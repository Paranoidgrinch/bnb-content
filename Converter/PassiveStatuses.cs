using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Reworked enemy PASSIVES, authored as engine statuses-with-triggers (a status the enemy carries from combat
// start via EncounterEnemy.StartingStatuses; see EnemyMapper). Unlike the six ported player statuses
// (StatusMapper), these reactions use the arc's richer effect-program expressions (card-play stats, counters,
// source-scoped reads) that CombatNodeModel does not expose — so each trigger is built as a RAW EffectProgram
// against the engine types and serialized through the CombatJson converters, exactly the path game.roguedeck.json
// uses. Ids here are referenced by enemy source-data `starting_statuses`.
public static class PassiveStatuses
{
    // Well-known content ids.
    public const string QueueAdvancesId = "queue_advances";
    public static readonly CounterId QueuePositionCounter = new("queue_position");

    // A single-opponent selector usable inside an enemy's own status trigger and SERIALIZABLE into the export
    // (unlike FirstTarget, an escape node): the lowest-health enemy of the owner — in a solo fight, the hero.
    private static readonly ICombatantTargetSelector Opponent =
        CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // Unsigned Form Ghost: a WATCHER status that carries the toggle triggers and never leaves, plus the SHIELD
    // status that actually carries the damage reduction. Two statuses because a passive modifier cannot be made
    // conditional — presence is the condition — and a status that removed itself could never come back.
    public const string StillMissingASignatureId = "still_missing_a_signature";
    public const string SignaturePendingId = "signature_pending";
    private const int SignatureThreshold = 3;

    public static IReadOnlyList<StatusData> All() =>
    [
        QueueAdvances(),
        StillMissingASignature(),
        SignaturePending(),
    ];

    // "The Queue Advances" (A Very Official Line): if the player ended their turn having played 3+ cards, the
    // enemy gains 1 Queue Position (capped at 3). Read at the enemy's turn start (right after the player's turn)
    // via cardsPlayedLastTurn on the opponent. The cash-out ("at 3, replace the next intent with Everyone Moves
    // at Once, then Queue Position → 0") is the enemy's intent_rule (self_counter ≥ 3) + that action resetting
    // the counter — authored on the enemy in source-data.
    private static StatusData QueueAdvances()
    {
        var atLeastThree = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CardsPlayedLastTurnExpression<TurnStartedTriggeredEffectContext>(Opponent),
            ComparisonOperator.GreaterOrEqual,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(3));

        // queue_position = min(queue_position + 1, 3)
        var cappedIncrement = new MinExpression<TurnStartedTriggeredEffectContext>(
            new AddExpression<TurnStartedTriggeredEffectContext>(
                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, QueuePositionCounter),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(3));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                atLeastThree,
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, QueuePositionCounter, cappedIncrement, relative: false)));

        return Passive(QueueAdvancesId, "The Queue Advances", "TurnStarted", program);
    }

    // "Still Missing a Signature" (Unsigned Form Ghost): while the Ghost carries fewer than 3 Paperwork it takes
    // 25% less direct damage; at 3+ the reduction is off; if Bookworm files it back below 3 the reduction
    // returns. The engine's passive modifiers cannot be conditional, so the reduction lives in its own status
    // (SignaturePending) and this watcher switches it on and off whenever the Ghost's statuses move.
    //
    // Every status event the Ghost is the SUBJECT of resolves the bearer as `eventTarget` (Applied/Merged/
    // Removed bind it to the affected combatant; StacksChanged binds both source and eventTarget to it), so one
    // program shape serves all four. All four are needed: a first Paperwork APPLIES, further ones MERGE, and
    // Bookworm only ADJUSTS the count. The program is idempotent — it adds a missing shield or drops a present
    // one — so the shield's own status events cannot make it loop.
    private static StatusData StillMissingASignature() => new()
    {
        Id = StillMissingASignatureId,
        NameKey = "Still Missing a Signature",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            SignatureTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
            SignatureTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
            SignatureTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
            SignatureTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
        ],
    };

    private static StatusTriggerData SignatureTrigger<TContext>(string trigger) where TContext : class
    {
        var bearer = CombatantTargetSelectors.EventTarget;
        var paperwork = new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId("paperwork"));
        var shield = new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId(SignaturePendingId));

        var program = new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(paperwork, ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TContext>(SignatureThreshold)),
                // Filed often enough: drop the reduction (if it is still up).
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(shield, ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    new ModifyStatusStacksNode<TContext>(bearer, new StatusDefinitionId(SignaturePendingId),
                        new ConstantExpression<TContext>(-1))),
                // Still unsigned: put the reduction back (if it is not already up).
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(shield, ComparisonOperator.Equal,
                        new ConstantExpression<TContext>(0)),
                    new ApplyStatusNode<TContext>(bearer, new StatusDefinitionId(SignaturePendingId),
                        new ConstantExpression<TContext>(1)))));

        return new StatusTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // The reduction itself: 25% less DIRECT damage (card hits and attacks; Paperwork's own tick is
    // DamageOverTime and stays untouched). Carried only while the watcher says the form is still unsigned.
    private static StatusData SignaturePending() => new()
    {
        Id = SignaturePendingId,
        NameKey = "Signature Pending",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 75, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    // Builds a hidden, non-stacking enemy status whose sole job is to carry one trigger program.
    private static StatusData Passive<TContext>(
        string id, string name, string trigger, EffectProgram<TContext> program) where TContext : class => new()
    {
        Id = id,
        NameKey = name,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [new StatusTriggerData(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()))],
    };
}
