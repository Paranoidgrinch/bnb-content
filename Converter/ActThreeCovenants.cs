using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 4 — The Covenants. Since 2026-11-03 (user): a witch whose gifts must be taken, a bride who wants
// one of each, and a cup whose every drink is a debt.
public static partial class ActThree
{
    public const string CourtesySafeConductId = "courtesy_safe_conduct";
    public const string PromiseMustBePairedId = "a_promise_must_be_paired";
    public const string DrinkBeforeChoosingId = "drink_before_choosing";
    public const string CrossroadsDrinkCardId = "drink_from_the_cup";

    // The gifts a hedge-witch gives (user, 2026-11-03): free, small, and each with a little sting in it.
    public const string GiftTag = "roadside_gift";
    public const string CrookedKindnessId = "crooked_kindness";
    public const string BentPennyId = "bent_penny";
    public const string WarmKettleId = "warm_kettle_gift";
    private static readonly string[] WitchlingGifts = [CrookedKindnessId, BentPennyId, WarmKettleId];

    public const int CupClaims = 2;

    // ── Roadside Witchling — the gift with strings ────────────────────────────────────────────────────────
    //
    // Every turn of yours she puts one of her three gifts in your hand, in turn. A gift still in your hand when
    // the turn ends is an insult: her law is broken, and the gift is gone either way.
    public static StatusData CourtesySafeConduct() =>
        GiftGiver(CourtesySafeConductId, "A Gift with Strings",
            "Each turn she gives you a gift. Still holding it at turn end breaks her law.",
            WitchlingGifts, GiftTag);

    // A giver of gifts: the gift of this round, in rotation, and the insult of a gift left in hand.
    public static StatusData GiftGiver(string id, string name, string text, IReadOnlyList<string> gifts, string tag) =>
        Rule(id, name, text,
        [
            On("TurnStarted", GiveAGift(Lawgiver(id), gifts), StatusTriggerScope.Anywhere),
            On("TurnEnded", AGiftRefused(Lawgiver(id), tag), StatusTriggerScope.Anywhere),
        ]);

    // The player's turn opens: the gift of this round, in turn.
    private static IEffectNode<TurnStartedTriggeredEffectContext> GiveAGift(ICombatantTargetSelector giver, IReadOnlyList<string> gifts)
    {
        var round = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new RoundNumberExpression<TurnStartedTriggeredEffectContext>(),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(gifts.Count));
        IEffectNode<TurnStartedTriggeredEffectContext> Create(string gift) =>
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new CardDefinitionId(gift), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1));
        IEffectNode<TurnStartedTriggeredEffectContext> Give(int index) =>
            index == gifts.Count - 1
                ? Create(gifts[index])
                : new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    Is(round, ComparisonOperator.Equal, (index + 1) % gifts.Count), Create(gifts[index]), @else: Give(index + 1));
        return new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(giver)),
            Give(0));
    }

    // The player's turn ends with a gift still in hand: the giver's law is broken.
    private static IEffectNode<TurnEndedTriggeredEffectContext> AGiftRefused(ICombatantTargetSelector giver, string tag) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(), Alive<TurnEndedTriggeredEffectContext>(giver)),
                Is(new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand, new TagId(tag)), ComparisonOperator.Greater, 0)),
            Breach<TurnEndedTriggeredEffectContext>(giver));

    // A gift: free, gone when played, gone at the end of the turn if not.
    public static CardData Gift(string id, string name, string text, string tag, IEffectNode<CardPlayContext> body, int cost = 0) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = text,
        Costs = cost > 0 ? [new ResourceCost(StandardCombatIds.EnergyResource, cost)] : [],
        Tags = [new TagId("form"), new TagId(tag)],
        Program = new EffectProgram<CardPlayContext>(body),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    private static IEffectNode<CardPlayContext> LoseHealth(int amount) =>
        new SetHealthNode<CardPlayContext>(CombatantTargetSelectors.Source,
            new SubtractExpression<CardPlayContext>(
                new CombatantCurrentHealthExpression<CardPlayContext>(CombatantTargetSelectors.Source),
                new ConstantExpression<CardPlayContext>(amount)));

    private static IEffectNode<CardPlayContext> DiscardChosen(int count) =>
        new RepeatEffectNode<CardPlayContext>(new ConstantExpression<CardPlayContext>(count),
            new MoveCardToZoneNode<CardPlayContext>(CombatantTargetSelectors.Source,
                new ChosenCardInZoneExpression<CardPlayContext>(CardZone.Hand, "discard a card"), CardZone.DiscardPile));

    private static IEffectNode<CardPlayContext> Seq(params IEffectNode<CardPlayContext>[] steps) =>
        new CausalSequenceEffectNode<CardPlayContext>(steps);

    private static ConstantExpression<CardPlayContext> N(int value) => new(value);

    public static IEnumerable<CardData> WitchlingGiftCards() =>
    [
        Gift(CrookedKindnessId, "Crooked Kindness", "Lose 2 HP. Draw 2 cards.", GiftTag,
            Seq(LoseHealth(2), new DrawCardsNode<CardPlayContext>(CombatantTargetSelectors.Source, N(2)))),
        Gift(BentPennyId, "Bent Penny", "Gain 1 Energy. Gain 1 Doubt.", GiftTag,
            Seq(new GainResourceNode<CardPlayContext>(CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource, N(1)),
                new ApplyStatusNode<CardPlayContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(Cards.Keywords.Doubt), N(1)))),
        Gift(WarmKettleId, "Warm Kettle", "Gain 8 Block. Discard a card.", GiftTag,
            Seq(new GainBlockNode<CardPlayContext>(CombatantTargetSelectors.Source, N(8)), DiscardChosen(1))),
    ];

    // ── The Blackthorn Bride — one of each ────────────────────────────────────────────────────────────────
    //
    // Since 2026-11-03 (user): each turn at most one card of each cost — a second card of a cost already played
    // this turn breaks her law. What has been played is remembered on the player, one counter per cost (3 and
    // more are one cost).
    private static CounterId BrideCost(int cost) => new($"bride_cost_{cost}");

    public static StatusData APromiseMustBePaired()
    {
        var player = CombatantTargetSelectors.Source;
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> CostIs(int cost) =>
            cost < 3 ? Is(PlayedCostValue, ComparisonOperator.Equal, cost) : Is(PlayedCostValue, ComparisonOperator.GreaterOrEqual, 3);
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Again(int cost) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(CostIs(cost),
                Is(CounterOn<CardPlayedTriggeredEffectContext>(player, BrideCost(cost)), ComparisonOperator.Greater, 0));
        var again = new OrExpression<CardPlayedTriggeredEffectContext>(
            new OrExpression<CardPlayedTriggeredEffectContext>(Again(0), Again(1)),
            new OrExpression<CardPlayedTriggeredEffectContext>(Again(2), Again(3)));
        var remember = new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [.. Enumerable.Range(0, 4).Select(cost => (IEffectNode<CardPlayedTriggeredEffectContext>)
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(CostIs(cost),
                    SetCounter<CardPlayedTriggeredEffectContext>(player, BrideCost(cost), 1)))]);
        var forget = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [.. Enumerable.Range(0, 4).Select(cost => SetCounter<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.Source, BrideCost(cost), 0))]);
        return LawOnPlay(PromiseMustBePairedId, "One of Each",
            "Each turn, play at most one card of each cost. A second card of the same cost breaks her law.",
            again, also: remember, atTurnStart: forget);
    }

    // ── Crossroads Cup — Drink Before Choosing ────────────────────────────────────────────────────────────
    //
    // Each turn of yours a drink stands in your hand: +1 Energy, and the Cup gains 2 Claims (user, 2026-11-03).
    public static StatusData DrinkBeforeChoosing()
    {
        var cup = Lawgiver(DrinkBeforeChoosingId);
        var pour = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(cup)),
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new CardDefinitionId(CrossroadsDrinkCardId), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));
        return Rule(DrinkBeforeChoosingId, "Drink Before Choosing",
            "Each turn the Cup offers you a drink: +1 Energy, but the Cup gains 2 Claims.",
            [On("TurnStarted", pour, StatusTriggerScope.Anywhere)]);
    }

    public static CardData DrinkFromTheCup() => new()
    {
        Id = CrossroadsDrinkCardId,
        NameKey = "Drink from the Cup",
        DescriptionKey = "Gain 1 Energy. The Cup gains 2 Claims. Unplayed, it is gone at the end of the turn.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new GainResourceNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                    new ConstantExpression<CardPlayContext>(1)),
                CreateClaim<CardPlayContext>(Lawgiver(DrinkBeforeChoosingId)),
                CreateClaim<CardPlayContext>(Lawgiver(DrinkBeforeChoosingId)),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };
}
