using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 7 — **Magistrate of Thorns** (220 HP).
//
// Once your hand is drawn, you choose the Judgment you will be held to this turn: no Deeds, no Workings, or no
// more than 3 cards. Keep it to the end of the turn and the Magistrate loses 8 HP; break it and it gains a Claim
// and 10 Block. The Judgment you chose stands on you for the turn.
//
// REWORKED 2026-11-03 (user, Act III pass): the three customs as standing law, the Binding Judgments, the
// hearing's bell and the Redress in Wergild are gone.
public static partial class ActThree
{
    public const string MagistrateEnemyId = "magistrate_of_thorns";
    public const string MagistrateId = "the_magistrate_of_thorns";
    public const string JudgmentOfferId = "three_judgments";

    public const int JudgmentKeptHealth = 8;
    public const int JudgmentBrokenBlock = 10;
    public const int JudgmentCardLimit = 3;

    private static readonly (string Key, string Name, string Option, string Text)[] Judgments =
    [
        ("no_deeds", "Judgment: No Deeds", "no Deeds this turn", "You chose: no Deeds this turn."),
        ("no_workings", "Judgment: No Workings", "no Workings this turn", "You chose: no Workings this turn."),
        ("three_cards", "Judgment: Three Cards", "no more than 3 cards this turn", "You chose: no more than 3 cards this turn."),
    ];

    private static string JudgmentId(string key) => $"judgment_{key}";

    private static ICombatantTargetSelector Magistrate { get; } = Elite(MagistrateId);

    private static IEnumerable<StatusData> MagistrateStatuses() =>
    [
        TheMagistrateOfThorns(),
        ThreeJudgments(),
        .. Judgments.Select(j => Marker(JudgmentId(j.Key), j.Name,
            $"{j.Text} Keep it and the Magistrate loses 8 HP; break it and it gains a Claim and 10 Block.")),
    ];

    // The player's side: the question, once the hand is drawn.
    private static StatusData ThreeJudgments()
    {
        var player = CombatantTargetSelectors.Source;
        IEffectNode<CardsDrawnTriggeredEffectContext> Accept(string key) =>
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(player, new StatusDefinitionId(JudgmentId(key)),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1));
        var ask = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Alive<CardsDrawnTriggeredEffectContext>(Magistrate),
            new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                [.. Judgments.Select(j => Accept(j.Key))],
                [.. Judgments.Select(j => j.Option)],
                count: 1, purpose: "the Magistrate asks which Judgment you will be held to"));
        return Rule(JudgmentOfferId, "Three Judgments",
            "Each turn choose a Judgment: no Deeds, no Workings, or at most 3 cards.",
            [new StatusTriggerData("CardsDrawn", System.Text.Json.JsonSerializer.SerializeToElement(
                TheOpeningHand.Only(new EffectProgram<CardsDrawnTriggeredEffectContext>(ask)),
                CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()))]);
    }

    private static StatusData TheMagistrateOfThorns()
    {
        var player = CombatantTargetSelectors.Source;
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Under(string key) =>
            Wearing<CardPlayedTriggeredEffectContext>(player, JudgmentId(key));
        var broken = new OrExpression<CardPlayedTriggeredEffectContext>(
            new OrExpression<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(Under("no_deeds"), PlayedIs(Cards.CardAuthoring.DeedTag)),
                new AndExpression<CardPlayedTriggeredEffectContext>(Under("no_workings"), PlayedIs(Cards.CardAuthoring.WorkingTag))),
            new AndExpression<CardPlayedTriggeredEffectContext>(Under("three_cards"),
                Is(PlayedThisTurn, ComparisonOperator.Greater, JudgmentCardLimit)));

        var play = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Alive<CardPlayedTriggeredEffectContext>(Magistrate),
                new AndExpression<CardPlayedTriggeredEffectContext>(broken,
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        Wearing<CardPlayedTriggeredEffectContext>(Magistrate, LawBrokenThisTurnId)))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new GainBlockNode<CardPlayedTriggeredEffectContext>(Magistrate,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(JudgmentBrokenBlock)),
                Breach<CardPlayedTriggeredEffectContext>(Magistrate),
            ]));

        // The turn ends: a Judgment kept costs the Magistrate 8, and the Judgment is lifted either way.
        var reckon = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            PlayersTurn<TurnEndedTriggeredEffectContext>(),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Alive<TurnEndedTriggeredEffectContext>(Magistrate),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            Judgments.Select(j => Wearing<TurnEndedTriggeredEffectContext>(Applicant, JudgmentId(j.Key)))
                                .Aggregate((a, b) => new OrExpression<TurnEndedTriggeredEffectContext>(a, b)),
                            new NotExpression<TurnEndedTriggeredEffectContext>(
                                Wearing<TurnEndedTriggeredEffectContext>(Magistrate, LawBrokenThisTurnId)))),
                    new SetHealthNode<TurnEndedTriggeredEffectContext>(Magistrate,
                        new SubtractExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Magistrate),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(JudgmentKeptHealth)))),
                .. Judgments.Select(j => (IEffectNode<TurnEndedTriggeredEffectContext>)
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Applicant, new StatusDefinitionId(JudgmentId(j.Key)))),
            ]));

        return Rule(MagistrateId, "The Magistrate of Thorns",
            "Keep the Judgment you chose and it loses 8 HP. Break it and it gains a Claim and 10 Block.",
            [On("CardPlayed", play, StatusTriggerScope.Anywhere), On("TurnEnded", reckon, StatusTriggerScope.Anywhere)]);
    }

    private static EffectProgram<EnemyActionContext>? MagistrateIntent(string enemyId, string intentId) => null;
}
