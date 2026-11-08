using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Rolling Stacks Colossus (Act II elite) ────────────────────────────────────────────────────────────
//
// Shelving walls on stone rollers, coming at you. Its Distance counts down by one at each of its turns, and at 0
// it crushes you for 20 and rolls back to 3. Each turn it is closer than 3 the player gets a Wheel Chock: exhaust a
// card from the hand and it rolls back 1.
//
// REWORKED 2026-11-03 (user, Act II trial): the Compression from skipped misfilings, the Open Aisle cards and the
// displaced Ladder are gone — "Misfile an Entire Section" promised 23 and only misfiled two unseen cards.
// REWORKED 2026-11-08 (CONTENT_REVIEW E7): rolling it back costs a card instead of 12 damage in one turn — the
// burst answer was the Return Bell's countdown-and-delay again, right after it.
public static class RollingStacksColossus
{
    public const string EnemyId = "rolling_stacks_colossus";
    public const string DistanceId = "colossus_distance";

    public const int StartDistance = 3;
    public const int CrushDamage = 20;
    public const string WheelChockId = "wheel_chock";

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
        ]));

        return new StatusData
        {
            Id = DistanceId,
            NameKey = "Distance",
            DescriptionKey = "Drops by 1 each turn; at 0 it crushes you for 20. While it is closer than 3, each turn "
                + "brings a Wheel Chock: exhaust a card from your hand to roll it back 1.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    turn, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ],
        };
    }

    // The chock arrives with the turn's hand, whenever the Colossus is closer than where it starts.
    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        EnemyRules.Encounter("CardsDrawn",
            E<CardsDrawnTriggeredEffectContext>.If(
                E<CardsDrawnTriggeredEffectContext>.And(
                    E<CardsDrawnTriggeredEffectContext>.Applicant,
                    // Some Colossus stands closer than 3: its steps add up to less than three for each of them.
                    E<CardsDrawnTriggeredEffectContext>.Is(
                        new SumOverTargetsExpression<CardsDrawnTriggeredEffectContext>(
                            E<CardsDrawnTriggeredEffectContext>.Wearers(DistanceId),
                            new IterationTargetStatusStacksExpression<CardsDrawnTriggeredEffectContext>(new StatusDefinitionId(DistanceId))),
                        ComparisonOperator.Less,
                        E<CardsDrawnTriggeredEffectContext>.Times(
                            E<CardsDrawnTriggeredEffectContext>.CountOf(E<CardsDrawnTriggeredEffectContext>.Wearers(DistanceId)),
                            StartDistance))),
                E<CardsDrawnTriggeredEffectContext>.Give(WheelChockId))),
    ];

    // Exhaust a card from the hand: every Colossus closer than 3 rolls back one step.
    public static CardData WheelChock() => EnemyRules.GivenCard(WheelChockId, "Wheel Chock",
        "Exhaust a card from your hand. The Rolling Stacks Colossus rolls back 1 (up to 3).",
        E<CardPlayContext>.If(
            E<CardPlayContext>.Is(E<CardPlayContext>.InZone(E<CardPlayContext>.Self, CardZone.Hand), ComparisonOperator.Greater, 1),
            E<CardPlayContext>.Seq(
                E<CardPlayContext>.Move(new ChosenCardInZoneExpression<CardPlayContext>(CardZone.Hand,
                    "Wheel Chock: choose a card to exhaust"), CardZone.ExhaustPile),
                E<CardPlayContext>.Each(E<CardPlayContext>.Wearers(DistanceId),
                    E<CardPlayContext>.If(
                        E<CardPlayContext>.Is(
                            new IterationTargetStatusStacksExpression<CardPlayContext>(new StatusDefinitionId(DistanceId)),
                            ComparisonOperator.Less, StartDistance),
                        E<CardPlayContext>.AddStacks(E<CardPlayContext>.It, DistanceId, E<CardPlayContext>.C(1)))))));

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;
}
