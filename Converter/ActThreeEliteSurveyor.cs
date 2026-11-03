using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 6 — **Surveyor of Forgotten Paths** (198 HP).
//
// Two laws stand side by side on its map, and they trade places every round: one is Current, one is Former.
//
//   Opening the turn with a Deed.
//   Playing 4 or more cards in a turn.
//
// Breaking the Current law breaks its law — a Claim. Breaking the Former one is walking an old right of way:
// you gain 1 Energy next turn.
//
// REWORKED 2026-11-03 (user, Act III pass): the rotating three-law map, the Old Rights spent by a card, and the
// Boundary are gone.
public static partial class ActThree
{
    public const string SurveyorEnemyId = "surveyor_of_forgotten_paths";
    public const string SurveyorId = "the_survey_map";
    public const string CurrentDeedFirstId = "survey_current_deed_first";
    public const string CurrentFourCardsId = "survey_current_four_cards";
    public const string OldRightId = "old_right";

    public const int SurveyCardLimit = 4;

    private static ICombatantTargetSelector Surveyor { get; } = Elite(SurveyorId);

    private static IEnumerable<StatusData> SurveyorStatuses() =>
    [
        TheSurveyMap(),
        Marker(CurrentDeedFirstId, "Current: Deed First",
            "Opening your turn with a Deed breaks its law. Playing 4 or more cards is the Former law: 1 Energy next turn."),
        Marker(CurrentFourCardsId, "Current: Four Cards",
            "Playing 4 or more cards breaks its law. Opening with a Deed is the Former law: 1 Energy next turn."),
        OldRight(),
    ];

    // Earned this turn, paid at the start of the next.
    private static StatusData OldRight() => new()
    {
        Id = OldRightId,
        NameKey = "Old Right",
        DescriptionKey = "You walked an old right of way: 1 Energy at the start of your next turn.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            On("TurnStarted", new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new GainResourceNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource, new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(OldRightId)),
            ])),
        ],
    };

    private static StatusData TheSurveyMap()
    {
        var odd = Is(new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new RoundNumberExpression<TurnStartedTriggeredEffectContext>(), new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
            ComparisonOperator.Equal, 1);
        IEffectNode<TurnStartedTriggeredEffectContext> Current(string current, string former) =>
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Surveyor, new StatusDefinitionId(former)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(Surveyor, new StatusDefinitionId(current),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            ]);
        var turn = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(Surveyor)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(odd,
                Current(CurrentDeedFirstId, CurrentFourCardsId), @else: Current(CurrentFourCardsId, CurrentDeedFirstId)));

        var deedFirst = new AndExpression<CardPlayedTriggeredEffectContext>(
            Is(PlayedThisTurn, ComparisonOperator.Equal, 1), PlayedIs(Cards.CardAuthoring.DeedTag));
        var fourCards = Is(PlayedThisTurn, ComparisonOperator.Equal, SurveyCardLimit);
        IEffectNode<CardPlayedTriggeredEffectContext> Walked(ICombatExpression<CardPlayedTriggeredEffectContext, bool> broke, string currentWhenBroken) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(broke,
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    Wearing<CardPlayedTriggeredEffectContext>(Surveyor, currentWhenBroken),
                    Breach<CardPlayedTriggeredEffectContext>(Surveyor),
                    @else: new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(OldRightId), new ConstantExpression<CardPlayedTriggeredEffectContext>(1))));
        var play = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            Alive<CardPlayedTriggeredEffectContext>(Surveyor),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [Walked(deedFirst, CurrentDeedFirstId), Walked(fourCards, CurrentFourCardsId)]));

        return Rule(SurveyorId, "Current and Former",
            "Two laws trade places each round. Breaking the Current one breaks its law; breaking the Former one gives you 1 Energy next turn.",
            [On("TurnStarted", turn, StatusTriggerScope.Anywhere), On("CardPlayed", play, StatusTriggerScope.Anywhere)]);
    }

    private static EffectProgram<EnemyActionContext>? SurveyorIntent(string enemyId, string intentId) => null;
}
