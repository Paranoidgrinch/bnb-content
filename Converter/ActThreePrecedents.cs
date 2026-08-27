using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 8 — The Old-Growth Precedents. The standard does not change arbitrarily; it becomes
// stricter because previous disputes hardened it. Three ways of remembering: a stump whose own law tightens
// with every dispute it has won, lichen that invents no law and only cites older authority, and a root
// network that keeps what the forest remembers long after the paperwork has moved on.
public static partial class ActThree
{
    public const string TheOldMeasureId = "the_old_measure";
    public const string MeasuredThisTurnId = "measured_this_turn";
    public const string CitedAuthorityId = "cited_authority";
    public const string CitedThisTurnId = "cited_this_turn";
    public const string DeepMemoryId = "deep_memory";
    public const string MemoryId = "memory";

    // How many player turns the Stump has been awake for. Its law says "beginning with the second".
    public static CounterId StumpTurnsCounter => new("old_measure_turns");

    // The last law anybody was found to have broken. The Precedent Lichen cites it; unlike the law being
    // filed at this instant, it is not cleared, because an authority outlives the moment it was invoked.
    public static CounterId LastLawBrokenCounter => new("last_law_broken");

    // Which law the Lichen is currently citing. Kept on the player with everything else the fight remembers.
    public static CounterId CitedLawCounter => new("cited_law");

    private const int MemoryCeiling = 4;

    // ── Sleeping Stump Auditor — The Old Measure ──────────────────────────────────────────────────────────

    // "From the second player turn on, play more cards than you played last turn and you owe the Stump."
    //
    // Rings of Precedent: every dispute the Stump has won hardens the same law. The design leaves the
    // numbers to the balance pass and states the intent — early Claim, stricter consequence — so the
    // placeholder is the plainest reading of it: the measure costs one Trespass, and one more for every
    // Claim the Stump holds.
    public static StatusData TheOldMeasure()
    {
        var player = CombatantTargetSelectors.Source;
        var stump = Lawgiver(TheOldMeasureId);

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    // The stump has to have been awake for a turn already to have a measure at all.
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                            player, StumpTurnsCounter),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(2)),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
                        ComparisonOperator.Greater,
                        new CardsPlayedLastTurnExpression<CardPlayedTriggeredEffectContext>(player))),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    // Asked before the law speaks, because the law's own answer is what closes the turn's
                    // measure — and the rings behind it are weight, not further violations.
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                            stump, new StatusDefinitionId(MeasuredThisTurnId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new RepeatEffectNode<CardPlayedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                                stump, new StatusDefinitionId(ClaimId)),
                            FileTrespass<CardPlayedTriggeredEffectContext>(stump)),
                        Violate<CardPlayedTriggeredEffectContext>(
                            stump, OldMeasureLaw, MeasuredThisTurnId),
                    ]))));

        var wake = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, StumpTurnsCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        stump, new StatusDefinitionId(MeasuredThisTurnId)),
                ])));

        return Rule(TheOldMeasureId, "The Old Measure",
            "From your second turn on, playing more cards than you played last turn owes the Sleeping Stump "
            + "Auditor 1 Trespass — and one more for every Claim it holds. The standard hardens.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    wake, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData MeasuredThisTurn() =>
        Marker(MeasuredThisTurnId, "Measured",
            "The Stump has already taken this turn's measure.");

    // ── Precedent Lichen — Cited Authority ────────────────────────────────────────────────────────────────

    // "When the Lichen gains a Claim it cites another party's Local Law — only the law, never their Claim
    // passives, their demands or their resources."
    //
    // It cites the last law anybody was found to have broken, which is the only authority a fight has
    // actually established. Afterwards, every breach of THAT law is a violation against the Lichen too: the
    // Stump's precedent becomes authority for two separate parties, which is exactly what Encounter 31 is
    // built to show. The Lichen invents nothing; it says "see older authority".
    public static StatusData CitedAuthority()
    {
        var lichen = Lawgiver(CitedAuthorityId);

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimId)),
                    new AndExpression<TContext>(
                        // …granted to the Lichen itself …
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(
                                CombatantTargetSelectors.EventTarget,
                                new StatusDefinitionId(CitedAuthorityId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TContext>(0)),
                        // …and there has to BE an older authority to cite.
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(Applicant, LastLawBrokenCounter),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TContext>(0)))),
                new SetCombatantCounterNode<TContext>(
                    Applicant, CitedLawCounter,
                    new CombatantCounterExpression<TContext>(Applicant, LastLawBrokenCounter),
                    relative: false)));

        var release = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    lichen, new StatusDefinitionId(CitedThisTurnId))));

        return Rule(CitedAuthorityId, "Cited Authority",
            "Granted a Claim, the Precedent Lichen cites the last law anybody was found to have broken. "
            + "Break that law again and you owe the Lichen 1 Trespass as well. Once a turn.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    release, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData CitedThisTurn() =>
        Marker(CitedThisTurnId, "Cited",
            "The Lichen has already cited its authority this turn.");

    // The other half, run from inside the violation itself — the same shape the Foxglove's testimony takes,
    // and for the same reason: a law's own answer is capped and a breach is not.
    public static IEffectNode<TContext> CitedAuthoritySpeaks<TContext>(int law)
        where TContext : class
    {
        var lichen = Lawgiver(CitedAuthorityId);

        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantCurrentHealthExpression<TContext>(lichen),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            lichen, new StatusDefinitionId(CitedThisTurnId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TContext>(0))),
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(Applicant, CitedLawCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(law))),
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(
                    lichen, new StatusDefinitionId(CitedThisTurnId), new ConstantExpression<TContext>(1)),
                FileTrespass<TContext>(lichen),
            ]));
    }

    // ── Footfall Root — Deep Memory ───────────────────────────────────────────────────────────────────────

    // "Every Claim the Root is granted becomes a Memory, to a maximum of 4. The Memory stays even if the
    // Claim later moves, is spent or is removed."
    //
    // Settlement may extinguish the Claim. It cannot extinguish what the forest remembers — which is the
    // whole reason the two are separate things here rather than one counter.
    public static StatusData DeepMemory()
    {
        var root = CombatantTargetSelectors.EventTarget;

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            root, new StatusDefinitionId(MemoryId)),
                        ComparisonOperator.Less,
                        new ConstantExpression<TContext>(MemoryCeiling))),
                new ApplyStatusNode<TContext>(
                    root, new StatusDefinitionId(MemoryId), new ConstantExpression<TContext>(1))));

        return Rule(DeepMemoryId, "Deep Memory",
            "Every Claim the Footfall Root is granted becomes a Memory, up to 4. The Memory outlasts the "
            + "Claim, whatever becomes of it.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    public static StatusData Memory() => new()
    {
        Id = MemoryId,
        NameKey = "Memory",
        DescriptionKey = "What the forest remembers of a footstep. Memory Crush hits 3 harder for each.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "Memory Crush — 16–20 damage, +3 per Memory, capped at +12."
    private static EffectProgram<EnemyActionContext> MemoryCrush(int damage) =>
        new(new DealDamageNode<EnemyActionContext>(
            Applicant,
            new AddExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(damage),
                new MultiplyExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(MemoryId)),
                    new ConstantExpression<EnemyActionContext>(3)))));

    public static EffectProgram<EnemyActionContext>? PrecedentsIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "footfall_root.memory_crush" => MemoryCrush(18),
            _ => null,
        };
}
