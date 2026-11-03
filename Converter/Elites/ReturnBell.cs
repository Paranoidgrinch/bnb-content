using System.Text.Json;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Converter.Elites;

// ── After-Hours Return Bell (Act II elite) ────────────────────────────────────────────────────────────────
//
// Closing time is counted on the Bell: 4, 3, 2, 1 at its turns, and at 0 it rings for 25 and starts again from 4.
// A busy desk keeps the doors open — every turn you play 4 or more cards puts the count back up by one (never
// past 4).
//
// REWORKED 2026-11-03 (user, Act II trial: "viel zu kompliziert"): the Overdue, the Late Fees and the Return
// Receipts are gone; four, not three, cards — "3 kann man fast immer spielen".
public static class ReturnBell
{
    public const string EnemyId = "after_hours_return_bell";
    public const string ClosingTimeId = "closing_time";

    public const int ClosingFrom = 4;
    public const int RingDamage = 25;
    public const int BusyDesk = 4;   // cards in one turn

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static ICombatantTargetSelector Bells =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ClosingTimeId));

    public static IEnumerable<StatusData> Statuses() => [ClosingTime()];

    // The count, ticked at the start of the Bell's own turn: the last tick rings, and the count starts over.
    private static StatusData ClosingTime()
    {
        var self = CombatantTargetSelectors.Source;
        var count = new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(ClosingTimeId));
        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    count, ComparisonOperator.LessOrEqual, new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new DealDamageNode<TurnStartedTriggeredEffectContext>(
                        Opponent, new ConstantExpression<TurnStartedTriggeredEffectContext>(RingDamage)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(ClosingTimeId)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        self, new StatusDefinitionId(ClosingTimeId), new ConstantExpression<TurnStartedTriggeredEffectContext>(ClosingFrom)),
                ]),
                @else: new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                    self, new StatusDefinitionId(ClosingTimeId), new ConstantExpression<TurnStartedTriggeredEffectContext>(-1))));
        return new StatusData
        {
            Id = ClosingTimeId,
            NameKey = "Closing Time",
            DescriptionKey = "Counts down each turn; at 0 the Bell rings for 25. A turn you play 4+ cards adds 1 back.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ],
        };
    }

    // The player's fourth card of a turn puts every Bell's count back by one, up to where it starts.
    public static IReadOnlyList<EncounterTriggerData> Triggers()
    {
        var player = CombatantTargetSelectors.Source;
        var bell = CombatantTargetSelectors.IterationTarget;
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
                    ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(BusyDesk)),
                new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(Bells,
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(bell, new StatusDefinitionId(ClosingTimeId)),
                            ComparisonOperator.Less, new ConstantExpression<CardPlayedTriggeredEffectContext>(ClosingFrom)),
                        new ModifyStatusStacksNode<CardPlayedTriggeredEffectContext>(
                            bell, new StatusDefinitionId(ClosingTimeId), new ConstantExpression<CardPlayedTriggeredEffectContext>(1))))));
        return [new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()))];
    }

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;
}
