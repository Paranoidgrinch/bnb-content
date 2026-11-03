using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── Black-Ink Oracle (Act II elite) ───────────────────────────────────────────────────────────────────────
//
// An oracle of lacquered plaques whose face is interrupted by strips of censoring ink. Every second turn, once
// your hand is drawn, it blacks out the cost of the first card in it and asks you what it is: guess right and you
// draw a card, guess wrong and you take 5 damage.
//
// The cost is graded against the card's printed cost; the frontend is what blacks it out (the mark
// `queried_by_the_oracle`), and the mark comes off once the question is answered.
//
// REWORKED 2026-11-03 (user, Act II trial: "die Idee ist cool, aber eher Kartenkosten raten: richtig kleiner
// Bonus, falsch 5 Schaden, jede zweite Runde"): Black Ink, the Devouring, Reveal, Decline and the citation are gone.
public static class BlackInkOracle
{
    public const string EnemyId = "black_ink_oracle";

    public const string TheOracleId = "the_black_ink_oracle";
    public const string OracleRulesId = "riddle_rules";
    public const string QueriedMark = "queried_by_the_oracle";

    public const int WrongDamage = 5;

    // Whether the last turn was a Riddle: they alternate, from the first.
    private static CounterId AskedCounter => new("oracle_asked");

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Oracles =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheOracleId));

    private const string RiddleText =
        "Every second turn it hides the cost of a card in your hand. Guess it: right, draw 1; wrong, take 5 damage.";

    public static IEnumerable<StatusData> Statuses() =>
    [
        Rule(TheOracleId, "The Black-Ink Oracle", RiddleText, []),
        Rules(),
    ];

    private static StatusData Rules()
    {
        var asked = new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, AskedCounter);
        var riddle = new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
            [Answer(0), Answer(1), Answer(2), AnswerThreeOrMore()],
            ["it costs 0", "it costs 1", "it costs 2", "it costs 3 or more"],
            count: 1, purpose: "the Oracle has blacked out the cost of a card in your hand: what does it cost?");
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CountTargetsExpression<CardsDrawnTriggeredEffectContext>(Oracles),
                    ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        asked, ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                            Self, AskedCounter, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand),
                                ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [
                                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                    Self, new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand, 0),
                                    new TagId(QueriedMark)),
                                riddle,
                                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                    Self, Queried(), new TagId(QueriedMark), remove: true),
                            ])),
                    ]),
                    @else: new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Self, AskedCounter, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false))));

        return Rule(OracleRulesId, "The Oracle's Riddle", RiddleText,
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    TheOpeningHand.Only(program), CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
            ]);
    }

    private static ICardInstanceExpression<CardsDrawnTriggeredEffectContext> Queried() =>
        new FirstMarkedCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand, new TagId(QueriedMark));

    private static ICombatExpression<CardsDrawnTriggeredEffectContext, int> QueriedCost() =>
        new CardInstanceBaseCostExpression<CardsDrawnTriggeredEffectContext>(Queried(), StandardCombatIds.EnergyResource);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Answer(int claimedCost) =>
        Graded(new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
            QueriedCost(), ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(claimedCost)));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> AnswerThreeOrMore() =>
        Graded(new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
            QueriedCost(), ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)));

    // Right: a card. Wrong: 5 damage, the Oracle's, which no Strength of the player's touches.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Graded(
        ICombatExpression<CardsDrawnTriggeredEffectContext, bool> correct) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            correct,
            new DrawCardsNode<CardsDrawnTriggeredEffectContext>(Self, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
            @else: new DealDamageNode<CardsDrawnTriggeredEffectContext>(
                Self, new ConstantExpression<CardsDrawnTriggeredEffectContext>(WrongDamage), kind: DamageKind.DamageOverTime));

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
