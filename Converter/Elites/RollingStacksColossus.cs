using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Rolling Stacks Colossus (Act II elite) ────────────────────────────────────────────────────────────
//
// Shelving walls on stone rollers, coming at you. Its Distance counts down by one at each of its turns, and at 0
// it crushes you for 20 and rolls back to 3. Hit it for 12 or more in one turn and you push it back one.
//
// REWORKED 2026-11-03 (user, Act II trial): the Compression from skipped misfilings, the Open Aisle cards and the
// displaced Ladder are gone — "Misfile an Entire Section" promised 23 and only misfiled two unseen cards.
public static class RollingStacksColossus
{
    public const string EnemyId = "rolling_stacks_colossus";
    public const string DistanceId = "colossus_distance";

    public const int StartDistance = 3;
    public const int CrushDamage = 20;
    public const int PushBack = 12;   // damage in one turn

    // The health it has lost since its last turn, and whether that has pushed it back already.
    private static CounterId TakenCounter => new("colossus_taken");
    private static CounterId PushedCounter => new("colossus_pushed");

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    public static IEnumerable<StatusData> Statuses() => [Distance()];

    private static StatusData Distance()
    {
        var self = CombatantTargetSelectors.Source;
        var distance = new StatusDefinitionId(DistanceId);

        // Its turn: one step closer — the last step crushes — and a fresh turn for pushing it back.
        var turn = new EffectProgram<TurnStartedTriggeredEffectContext>(new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(self, distance),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new DealDamageNode<TurnStartedTriggeredEffectContext>(
                        Opponent, new ConstantExpression<TurnStartedTriggeredEffectContext>(CrushDamage)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(self, distance),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        self, distance, new ConstantExpression<TurnStartedTriggeredEffectContext>(StartDistance)),
                ]),
                @else: new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                    self, distance, new ConstantExpression<TurnStartedTriggeredEffectContext>(-1))),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                self, TakenCounter, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                self, PushedCounter, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
        ]));

        // A blow that draws blood counts toward the push; the twelfth point pushes it back once (never past 3).
        // In a damage program the struck body is the event's target.
        var struck = CombatantTargetSelectors.EventTarget;
        ComparisonExpression<DamageReceivedTriggeredEffectContext> Is(
            ICombatExpression<DamageReceivedTriggeredEffectContext, int> left, ComparisonOperator op, int right) =>
            new(left, op, new ConstantExpression<DamageReceivedTriggeredEffectContext>(right));
        var blow = new EffectProgram<DamageReceivedTriggeredEffectContext>(new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
        [
            new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                struck, TakenCounter, new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    Is(new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(struck, TakenCounter),
                        ComparisonOperator.GreaterOrEqual, PushBack),
                    Is(new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(struck, PushedCounter),
                        ComparisonOperator.Equal, 0)),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        struck, PushedCounter, new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                    new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                        Is(new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(struck, distance),
                            ComparisonOperator.Less, StartDistance),
                        new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                            struck, distance, new ConstantExpression<DamageReceivedTriggeredEffectContext>(1))),
                ])),
        ]));

        return new StatusData
        {
            Id = DistanceId,
            NameKey = "Distance",
            DescriptionKey = "Drops by 1 each turn; at 0 it crushes you for 20. Deal 12+ damage in one turn to push it back 1.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    turn, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    blow, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
            ],
        };
    }

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;
}
