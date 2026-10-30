using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 4 — The Covenants. The stage where the act starts being NICE to you, and where the
// player learns that hospitality is a debt engine. All three parties hand out Safe-Conduct, and all three
// want something back for it: the Witchling wants the gift used, the Bride wants the relationship to
// progress, and the Cup wants its generosity to create obligation somewhere in the social network.
public static partial class ActThree
{
    public const string CourtesySafeConductId = "courtesy_safe_conduct";
    public const string GiftOutstandingId = "witchling_gift_outstanding";
    public const string PromiseMustBePairedId = "a_promise_must_be_paired";
    public const string PromiseOpenId = "promise_open";
    public const string BetrothalClaimId = "betrothal_claim";
    public const string BetrothalGiftGivenId = "betrothal_gift_given";
    public const string DrinkBeforeChoosingId = "drink_before_choosing";

    public const string WitchlingGiftCardId = "witchlings_gift";
    public const string WitchlingCharmCardId = "witchlings_charm";
    public const string CrossroadsDrinkCardId = "drink_from_the_cup";

    // Turns an accepted gift may go unused before it is an insult.
    public const int GiftPatienceTurns = 2;

    // ── Roadside Witchling — the gift with strings ────────────────────────────────────────────────────────
    //
    // "Offers a gift each turn (Safe-Conduct or a card); an accepted gift not used within 2 turns becomes
    // Trespass; declining is free." The offer is a free card in your hand that vanishes at the end of the turn
    // — leaving it is declining. Accepting puts a visible chip on you that counts the turns down.
    public static StatusData CourtesySafeConduct()
    {
        var witchling = Lawgiver(CourtesySafeConductId);

        var offer = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    Alive<TurnStartedTriggeredEffectContext>(witchling)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new CardDefinitionId(WitchlingGiftCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))));

        // A licence spent is a gift used.
        var used = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new StatusDefinitionId(TrespassId)),
                new RemoveStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(GiftOutstandingId))));

        var outstanding = new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
            Applicant, new StatusDefinitionId(GiftOutstandingId));

        var reckon = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        outstanding, ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        outstanding, ComparisonOperator.LessOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(GiftOutstandingId)),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            Alive<TurnEndedTriggeredEffectContext>(witchling),
                            FileTrespass<TurnEndedTriggeredEffectContext>(witchling)),
                    ]),
                    new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(GiftOutstandingId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)))));

        return Rule(CourtesySafeConductId, "A Gift with Strings",
            "Each turn she offers a gift. Leaving it is free. Accept it and use it within 2 turns, or owe her "
            + "1 Trespass.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    offer, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    used, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    reckon, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData GiftOutstanding() => new()
    {
        Id = GiftOutstandingId,
        NameKey = "A Gift Unused",
        DescriptionKey = "The Witchling's gift. Spend a Safe-Conduct or play her Charm before this runs out, or owe her 1 Trespass.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static IEffectNode<CardPlayContext> GiftAccepted() =>
        new CausalSequenceEffectNode<CardPlayContext>(
        [
            new RemoveStatusNode<CardPlayContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(GiftOutstandingId)),
            new ApplyStatusNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(GiftOutstandingId),
                new ConstantExpression<CardPlayContext>(GiftPatienceTurns)),
        ]);

    public static CardData WitchlingsGift() => new()
    {
        Id = WitchlingGiftCardId,
        NameKey = "The Witchling's Gift",
        DescriptionKey = "Accept: 1 Safe-Conduct, or her Charm. Use it within 2 turns or owe her 1 Trespass. Unplayed, it is declined.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new ChooseOptionsNode<CardPlayContext>(
                [
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new ApplyStatusNode<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(SafeConductId),
                            new ConstantExpression<CardPlayContext>(1),
                            sourceSelector: Lawgiver(CourtesySafeConductId)),
                        GiftAccepted(),
                    ]),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new CreateCardInstanceNode<CardPlayContext>(
                            CombatantTargetSelectors.Source, new CardDefinitionId(WitchlingCharmCardId), CardZone.Hand,
                            new ConstantExpression<CardPlayContext>(1)),
                        GiftAccepted(),
                    ]),
                ],
                ["a safe-conduct", "her charm"],
                count: 1,
                purpose: "accept the gift")),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    public static CardData WitchlingsCharm() => new()
    {
        Id = WitchlingCharmCardId,
        NameKey = "The Witchling's Charm",
        DescriptionKey = "Gain 8 Block. Draw 1. Using it settles her gift.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new GainBlockNode<CardPlayContext>(CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(8)),
                new DrawCardsNode<CardPlayContext>(CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(1)),
                new RemoveStatusNode<CardPlayContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(GiftOutstandingId)),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    // ── The Blackthorn Bride ──────────────────────────────────────────────────────────────────────────────

    // "After a card of Base Cost 2 or more, the next card should cost 0 or 1. If it also costs 2 or more:
    // 1 Trespass." A promise must be paired.
    public static StatusData APromiseMustBePaired()
    {
        var player = CombatantTargetSelectors.Source;
        var bride = Lawgiver(PromiseMustBePairedId);
        var memory = CostMemory("betrothal");

        // The Base Cost of the card just played, plus one, so that "nothing yet" can be zero.
        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var bothHeavy = new AndExpression<CardPlayedTriggeredEffectContext>(
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                ThisCost(), ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(3)));

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    bothHeavy,
                    Violate<CardPlayedTriggeredEffectContext>(bride, PairedPromiseLaw)),
                // The open promise is worn by the Bride, so the next card's limit is on screen.
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        ThisCost(), ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        bride, new StatusDefinitionId(PromiseOpenId),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        bride, new StatusDefinitionId(PromiseOpenId))),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, memory,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Lawgiver(PromiseMustBePairedId), new StatusDefinitionId(PromiseOpenId)),
                ])));

        return Rule(PromiseMustBePairedId, "A Promise Must Be Paired",
            "After a card costing 2 or more, your next card must cost 0 or 1, or you owe the Bride 1 "
            + "Trespass.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // "At 1 Claim the player gains 1 Safe-Conduct. At 2 Claims, create Wergild 2 from the Bride." Welcome,
    // then commitment, then obligation — the relationship progresses whether the player wants it to or not.
    public static StatusData BetrothalClaim()
    {
        var bride = CombatantTargetSelectors.EventTarget;

        EffectProgram<TContext> Program<TContext>() where TContext : class
        {
            var claims = new CombatantStatusStacksExpression<TContext>(
                bride, new StatusDefinitionId(ClaimId));

            return new EffectProgram<TContext>(
                new ConditionalEffectNode<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimId)),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        // The welcome, once.
                        new ConditionalEffectNode<TContext>(
                            new AndExpression<TContext>(
                                new ComparisonExpression<TContext>(
                                    claims, ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<TContext>(1)),
                                new ComparisonExpression<TContext>(
                                    new CombatantStatusStacksExpression<TContext>(
                                        bride, new StatusDefinitionId(BetrothalGiftGivenId)),
                                    ComparisonOperator.Equal,
                                    new ConstantExpression<TContext>(0))),
                            new CausalSequenceEffectNode<TContext>(
                            [
                                new ApplyStatusNode<TContext>(
                                    Applicant, new StatusDefinitionId(SafeConductId),
                                    new ConstantExpression<TContext>(1), sourceSelector: bride),
                                new ApplyStatusNode<TContext>(
                                    bride, new StatusDefinitionId(BetrothalGiftGivenId),
                                    new ConstantExpression<TContext>(1)),
                            ])),
                        // …and then the bill.
                        new ConditionalEffectNode<TContext>(
                            new ComparisonExpression<TContext>(
                                claims, ComparisonOperator.Equal, new ConstantExpression<TContext>(2)),
                            DemandWergild<TContext>(bride, 2)),
                    ])));
        }

        return Rule(BetrothalClaimId, "Betrothal Claim",
            "The Bride's first Claim is welcomed with 1 Safe-Conduct for you. Her second is a demand for "
            + "2 Wergild.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    public static StatusData PromiseOpen() =>
        Marker(PromiseOpenId, "A Promise Open",
            "You played a card costing 2 or more. Your next card must cost 0 or 1, or owe the Bride 1 Trespass.");

    public static StatusData BetrothalGiftGiven() =>
        Marker(BetrothalGiftGivenId, "Welcomed",
            "The Bride has already welcomed you once.");

    // ── Crossroads Cup — Drink Before Choosing ────────────────────────────────────────────────────────────
    //
    // "At your turn start it offers a drink, +1 Energy this turn, but the enemy with the fewest Claims gains
    // one." The drink is a free card that vanishes at the end of the turn; leaving it costs nothing.
    public static StatusData DrinkBeforeChoosing()
    {
        var cup = Lawgiver(DrinkBeforeChoosingId);

        var pour = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    Alive<TurnStartedTriggeredEffectContext>(cup)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new CardDefinitionId(CrossroadsDrinkCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))));

        return Rule(DrinkBeforeChoosingId, "Drink Before Choosing",
            "Each turn the Cup offers you a drink: +1 Energy, but the party holding the fewest Claims gains one.",
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                pour, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    public static CardData DrinkFromTheCup() => new()
    {
        Id = CrossroadsDrinkCardId,
        NameKey = "Drink from the Cup",
        DescriptionKey = "Gain 1 Energy. The enemy with the fewest Claims gains a Claim. Unplayed, it is gone at the end of the turn.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new GainResourceNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                    new ConstantExpression<CardPlayContext>(1)),
                CreateClaim<CardPlayContext>(
                    CombatantTargetSelectors.FirstTarget(
                        CombatantTargetSelectors.LowestStatusStacks(Parties, new StatusDefinitionId(ClaimId)))),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };
}
