using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Relics;

// The in-combat rules the five Act-III Event relics install.
//
// The Green Docket's prizes are all about the SHAPE of a turn rather than about one card: how many things
// you did, whether you did them in order, whether the third one was the third one. That is the act's own
// question — it is what every Local Law in it asks — and these are the first four answers the player owns.
// The fifth, the Guest-Right Brooch, is not a rule of a fight at all.
public static class ActThreeEventRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
        [Mootcap, DissentingSpore, AntwayMarker, ComplaintLeaf, Respondent, GuestRightBrooch];

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;

    private static CounterId MootcapUsed => new("mootcap_used");
    private static CounterId SporeCount => new("dissenting_spore");
    private static CounterId AntwayMemory => new("antway_last_cost");
    private static CounterId AntwayBroken => new("antway_broken");
    private static CounterId AntwayUsed => new("antway_used");
    private static CounterId LeafFound => new("complaint_leaf_found");

    public const string RespondentId = "respondent";

    // ── Mootcap ───────────────────────────────────────────────────────────────────────────────────────────

    // "First time each turn you play the third non-Junk card, choose: gain 10 Block; draw 1; or deal 7 damage
    // to all enemies."
    public static readonly StatusData Mootcap = Rule(
        "mootcap", "Mootcap",
        "A quorum is three. The third real card you play each turn is put to the circle, and the circle "
        + "answers: 10 Block, a card, or 7 damage to everything standing.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        Unspent<CardPlayedTriggeredEffectContext>(MootcapUsed),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            RealCardsPlayed<CardPlayedTriggeredEffectContext>(),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(3))),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        Spend<CardPlayedTriggeredEffectContext>(MootcapUsed),
                        new ChooseOptionsNode<CardPlayedTriggeredEffectContext>(
                        [
                            new GainBlockNode<CardPlayedTriggeredEffectContext>(
                                You, new ConstantExpression<CardPlayedTriggeredEffectContext>(10)),
                            new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                                You, new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                            new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.AllEnemiesOfSource,
                                new DealDamageNode<CardPlayedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    new ConstantExpression<CardPlayedTriggeredEffectContext>(7))),
                        ],
                        ["10 Block", "a card", "7 damage to everything standing"],
                        count: 1, purpose: "the circle answers"),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
            Clear(MootcapUsed),
        ]);

    // ── Dissenting Spore ──────────────────────────────────────────────────────────────────────────────────

    // "End turn with odd non-Junk count → +1 Spore, max 3; even → −1 Spore. Start turn at 3 Spores: consume
    // all, gain 1 Energy, draw +1, gain 6 Block."
    public static readonly StatusData DissentingSpore = Rule(
        "dissenting_spore", "Dissenting Spore",
        "The circle counts what you did: an odd turn grows a spore, an even one costs you one. Three spores "
        + "and the ring speaks — 1 Energy, an extra card and 6 Block.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new RemainderExpression<TurnEndedTriggeredEffectContext>(
                            RealCardsPlayed<TurnEndedTriggeredEffectContext>(),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(You, SporeCount),
                            ComparisonOperator.Less,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(3)),
                        Add<TurnEndedTriggeredEffectContext>(SporeCount, 1)),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(You, SporeCount),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        Add<TurnEndedTriggeredEffectContext>(SporeCount, -1)))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, SporeCount),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Set<CardsDrawnTriggeredEffectContext>(SporeCount, 0),
                        HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(6)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Antway Marker ─────────────────────────────────────────────────────────────────────────────────────

    // "If the first 3 non-Junk cards of a turn form a non-decreasing base-cost sequence, after card 3 gain 1
    // Energy and draw 1. If the sequence decreases before card 3, no trigger that turn."
    public static readonly StatusData AntwayMarker = Rule(
        "antway_marker", "Antway Marker",
        "Walk in the proper line: three real cards in a row, none cheaper than the one before it, and the "
        + "third is worth 1 Energy and a card. Step out of order and the line is broken for the turn.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(IsJunk()),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        // Cheaper than the one before it breaks the line for the rest of the turn.
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                PlayedCostPlusOne(), ComparisonOperator.Less,
                                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                    You, AntwayMemory)),
                            Set<CardPlayedTriggeredEffectContext>(AntwayBroken, 1)),
                        new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                            You, AntwayMemory, PlayedCostPlusOne(), relative: false),
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                Unspent<CardPlayedTriggeredEffectContext>(AntwayUsed),
                                new AndExpression<CardPlayedTriggeredEffectContext>(
                                    Unspent<CardPlayedTriggeredEffectContext>(AntwayBroken),
                                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                        RealCardsPlayed<CardPlayedTriggeredEffectContext>(),
                                        ComparisonOperator.Equal,
                                        new ConstantExpression<CardPlayedTriggeredEffectContext>(3)))),
                            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                            [
                                Spend<CardPlayedTriggeredEffectContext>(AntwayUsed),
                                HeldEnergy.Hold<CardPlayedTriggeredEffectContext>(1),
                                new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                                    You, new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                            ])),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    Set<TurnStartedTriggeredEffectContext>(AntwayMemory, 0),
                    Set<TurnStartedTriggeredEffectContext>(AntwayBroken, 0),
                    Set<TurnStartedTriggeredEffectContext>(AntwayUsed, 0),
                ])),
                nameof(TriggerEvent.TurnStarted)),
        ]);

    // ── Complaint Leaf ────────────────────────────────────────────────────────────────────────────────────

    // "The first enemy each combat that causes HP loss or directly applies a negative status becomes the
    // Respondent. While it lives, the first non-Junk card each turn targeting it costs 1 less."
    //
    // ADAPTATION: a discount cannot be conditioned on where a card will be aimed — a card's price is settled
    // before its target is. So while a Respondent is standing, ONE card in your hand each turn is a point
    // cheaper, which is the same discount for the same reason, given to the hand rather than to the aim.
    public static readonly StatusData ComplaintLeaf = Rule(
        "complaint_leaf", "Complaint Leaf",
        "The first party to lay a hand on you is named the Respondent. While it is standing, one card in "
        + "your hand each turn costs 1 less.",
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        Unspent<DamageReceivedTriggeredEffectContext>(LeafFound),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(RespondentId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                        Spend<DamageReceivedTriggeredEffectContext>(LeafFound),
                    ]))),
                nameof(TriggerEvent.DamageTaken)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.FirstTarget(
                                CombatantTargetSelectors.WithStatus(
                                    CombatantTargetSelectors.AllAliveCombatants,
                                    new StatusDefinitionId(RespondentId)))),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                        You, CardZone.Hand,
                        new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                            You, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
                        takeFirst: 1))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The mark itself: the party that answered for the complaint, so that anything else in the fight can
    // see who it was.
    public static readonly StatusData Respondent = new()
    {
        Id = RespondentId,
        NameKey = "Respondent",
        DescriptionKey = "The party your complaint names. It was the first to lay a hand on you.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
    };

    // ── Guest-Right Brooch ────────────────────────────────────────────────────────────────────────────────

    // ADAPTATION: "once per Event, reduce one explicit Gold/HP option cost by 25%" has no engine face — an
    // event's costs are settled by the door, not by what the traveller is carrying. The brooch is
    // guest-right instead, which is what it is: somebody who has been welcomed is looked after, and every
    // fight opens a little kinder.
    public static readonly StatusData GuestRightBrooch = Rule(
        "guest_right_brooch", "Guest-Right Brooch",
        "You have been welcomed on this road. Every fight opens with 8 Block and one Safe-Conduct.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(8)),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            You, new StatusDefinitionId(ActThree.SafeConductId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, int> RealCardsPlayed<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CardsPlayedThisTurnExpression<TContext>(You),
            new CardsPlayedThisTurnWithTagExpression<TContext>(
                You, new TagId(CardAuthoring.JunkTag)));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> IsJunk() =>
        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
            new TagId(CardAuthoring.JunkTag));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> PlayedCostPlusOne() =>
        new AddExpression<CardPlayedTriggeredEffectContext>(
            new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                StandardCombatIds.EnergyResource),
            new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(You, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        Set<TContext>(latch, 1);

    private static IEffectNode<TContext> Set<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            You, id, new ConstantExpression<TContext>(value), relative: false);

    private static IEffectNode<TContext> Add<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            You, id, new ConstantExpression<TContext>(value), relative: true);

    private static StatusTriggerData Clear(CounterId id) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            Set<TurnStartedTriggeredEffectContext>(id, 0)), nameof(TriggerEvent.TurnStarted));

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers = triggers,
        };

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Bearer);
}
