using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III's laws in one shape (user, 2026-11-03): an enemy's law is a sentence about your cards, breaking it gives
// that enemy a Claim (+3 damage on its attacks, at most 3), and a law breaks at most once a turn. A Safe-Conduct
// still refuses the breach, because the breach still arrives as a Trespass — which the customs turn into the Claim
// the moment it lands (TrespassThreshold = 1).
public static partial class ActThree
{
    // Worn by a lawgiver whose law was broken this turn; cleared at the player's turn start (GreenDocketCustoms).
    public const string LawBrokenThisTurnId = "law_broken_this_turn";

    public static StatusData LawBrokenThisTurn() =>
        Marker(LawBrokenThisTurnId, "Law Broken", "You broke its law this turn: it gained a Claim. Its law does not break again until next turn.");

    // The breach itself: once a turn per lawgiver.
    public static IEffectNode<TContext> Breach<TContext>(ICombatantTargetSelector lawgiver) where TContext : class =>
        Violate<TContext>(lawgiver, NoLaw, LawBrokenThisTurnId);

    // A law about the card just played: `breaks` is asked of every card the player plays while the lawgiver lives.
    // `also` runs after the question, on every card — the place a law writes down what it remembers.
    private static StatusData LawOnPlay(
        string id, string name, string text,
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> breaks,
        IEffectNode<CardPlayedTriggeredEffectContext>? also = null,
        IEffectNode<TurnStartedTriggeredEffectContext>? atTurnStart = null)
    {
        var lawgiver = Lawgiver(id);
        var play = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                Alive<CardPlayedTriggeredEffectContext>(lawgiver),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        breaks, Breach<CardPlayedTriggeredEffectContext>(lawgiver)),
                    .. also is null ? [] : new[] { also },
                ])));
        var triggers = new List<StatusTriggerData>
        {
            new("CardPlayed", JsonSerializer.SerializeToElement(play, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere),
        };
        if (atTurnStart is not null)
            triggers.Add(new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                new EffectProgram<TurnStartedTriggeredEffectContext>(
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        PlayersTurn<TurnStartedTriggeredEffectContext>(), atTurnStart)),
                CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()), StatusTriggerScope.Anywhere));
        return Rule(id, name, text, triggers);
    }

    // ── what a law reads ──────────────────────────────────────────────────────────────────────────────────

    private static ICardInstanceExpression<CardPlayedTriggeredEffectContext> ThePlayedCard =>
        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedIs(string tag) =>
        new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(ThePlayedCard, new TagId(tag));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> PlayedCostValue =>
        new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(ThePlayedCard, StandardCombatIds.EnergyResource);

    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> PlayedThisTurn =>
        new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.Source);

    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> PlayedThisTurnTagged(string tag) =>
        new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.Source, new TagId(tag));

    private static ICombatExpression<TContext, bool> Is<TContext>(
        ICombatExpression<TContext, int> left, ComparisonOperator op, int right) where TContext : class =>
        new ComparisonExpression<TContext>(left, op, new ConstantExpression<TContext>(right));

    private static ICombatExpression<TContext, int> CounterOn<TContext>(ICombatantTargetSelector who, CounterId counter)
        where TContext : class =>
        new CombatantCounterExpression<TContext>(who, counter);

    private static IEffectNode<TContext> SetCounter<TContext>(ICombatantTargetSelector who, CounterId counter, int value)
        where TContext : class =>
        new SetCombatantCounterNode<TContext>(who, counter, new ConstantExpression<TContext>(value), relative: false);

    private static IEffectNode<TContext> SetCounter<TContext>(
        ICombatantTargetSelector who, CounterId counter, ICombatExpression<TContext, int> value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(who, counter, value, relative: false);


    // A body passes a share of its health (66 = two thirds): once, the moment a blow takes it there. In a damage
    // program the struck body is the event's target.
    private static IEffectNode<DamageReceivedTriggeredEffectContext> OnceBelow(
        int percent, string passedId, IEffectNode<DamageReceivedTriggeredEffectContext> then)
    {
        var body = CombatantTargetSelectors.EventTarget;
        return new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            new AndExpression<DamageReceivedTriggeredEffectContext>(
                new NotExpression<DamageReceivedTriggeredEffectContext>(Wearing<DamageReceivedTriggeredEffectContext>(body, passedId)),
                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                    new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(body),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(100)),
                    ComparisonOperator.LessOrEqual,
                    new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantMaxHealthExpression<DamageReceivedTriggeredEffectContext>(body),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(percent)))),
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(body, new StatusDefinitionId(passedId),
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                then,
            ]));
    }

    private static ICombatExpression<TContext, bool> Wearing<TContext>(ICombatantTargetSelector who, string statusId)
        where TContext : class =>
        new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(statusId));

    // Real cards — the Junk a fight hands you does not count — played this turn, and still in hand.
    public static ICombatExpression<TContext, int> RealCardsPlayed<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CardsPlayedThisTurnExpression<TContext>(Applicant),
            new CardsPlayedThisTurnWithTagExpression<TContext>(Applicant, new TagId(Cards.CardAuthoring.JunkTag)));

    public static ICombatExpression<TContext, int> RealCardsLeftInHand<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand),
            new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand, new TagId(Cards.CardAuthoring.JunkTag)));

    private static ICombatExpression<TContext, bool> Wears<TContext>(ICombatantTargetSelector who, string statusId)
        where TContext : class => Wearing<TContext>(who, statusId);

    private static StatusTriggerData On<TContext>(string trigger, IEffectNode<TContext> body,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(new EffectProgram<TContext>(body), CombatJson.CreateOptions<TContext>()), scope);
}
