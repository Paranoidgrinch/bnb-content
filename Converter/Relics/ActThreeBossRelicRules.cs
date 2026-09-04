using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules of the ACT III boss relics — the Green Docket's five courts, handed to the player.
//
// Each of them is a piece of its boss's own machinery: the Ombudsman's two Grounds, the Notary's three
// rings, Grandmother's courtesies (a gift with a clause attached — and here the clause is enforced against
// the HOLDER), the Hill's stored weight, and the Queen's reciprocity.
//
// The devices are the ones the other pools established: a LATCH is "the first time each turn", a LEDGER
// written at the turn's end and read at the next draw is "based on last turn", and a MARK on a card
// instance is "this copy, until it is played". Two Act-III shapes recur:
//
//   a GRACE — a gift the holder may take at the bell, which is a card the fight puts in their hand, because
//   a combat here has no free actions;
//   a CLAUSE — a promise checked when the turn ends, which costs the holder HP if it is broken.
public static partial class BossRelicRules
{
    // ── ids and counters ──────────────────────────────────────────────────────────────────────────────────

    private static CounterId GroundTurn => new("boundary_tally_turn");
    private static CounterId TwineUsed => new("counter_petition_twine");
    private static CounterId SettlementHealth => new("signed_settlement_health");
    private static CounterId RingCost => new("countersealed_ring_cost");
    private static CounterId RingMatched => new("countersealed_ring_matched");
    private static CounterId RestraintArmed => new("countersealed_restraint");
    private static CounterId HoneyUsed => new("honey_spoon");
    private static CounterId CushionUsed => new("better_chair_cushion");
    private static CounterId TinUsed => new("last_slice_tin");
    private static CounterId MilestoneStep => new("surveyed_milestone_step");
    private static CounterId CairnBuried => new("survey_cairn_buried");
    private static CounterId CairnUsed => new("survey_cairn_used");
    private static CounterId LoadstoneHealth => new("loadstone_health");
    private static CounterId LoadstoneWeight => new("loadstone_weight");
    private static CounterId GraceUsed => new("royal_grace_cup");
    private static CounterId CourtFavor => new("hollow_court_favor");
    private static CounterId CourtSpent => new("hollow_court_spent");
    private static CounterId TallyUsed => new("silver_name_tally");

    private const string ClauseBreachHealth = "6";

    public static IReadOnlyList<StatusData> ActThreeRules() =>
    [
        BoundaryTally, CounterPetitionTwine, SignedSettlement,
        CountersealedRingOfPassage, CountersealedRingOfRestraint, CountersealedRingOfKeeping,
        HoneySpoon, BetterChairCushion, LastSliceTin,
        SurveyedMilestone, SurveyCairn, LoadstoneCairn,
        RoyalGraceCup, HollowCourtToken, SilverNameTally,
    ];

    // ── The Ombudsman of Root and Road — the two Grounds ──────────────────────────────────────────────────

    // "At combat start choose Road or Root; alternate each turn. Road: first non-Junk card costs 1 less.
    // Root: start turn gain 10 Block."
    //
    // ADAPTATION: the opening choice is dropped and the alternation kept. A hearing that asks which Ground
    // it is being held on before the fight has begun has nobody to ask — the same reason the Ombudsman's own
    // hearings open on the Road rather than being put to the traveller.
    public static readonly StatusData BoundaryTally = Rule(
        "boundary_tally", "Boundary Tally",
        "The road and the root take it in turns. On a road turn your first real card costs 1 less; on a "
        + "root turn you open with 10 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    CountOwnTurn<CardsDrawnTriggeredEffectContext>(GroundTurn),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    Self, GroundTurn),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        CheapenTheFirstRealCard(),
                        Block<CardsDrawnTriggeredEffectContext>(10)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CheapenTheFirstRealCard() =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand,
            new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
            takeFirst: 1);

    // "Once/turn free action: discard 1 non-Junk card; draw 1 and gain 1 Energy."
    //
    // ADAPTATION: a combat here has no free actions, so the twine is a card the fight puts in the holder's
    // hand each turn — the same shape as Make Amends and every other player-side action in the act.
    public static readonly StatusData CounterPetitionTwine = Rule(
        "counter_petition_twine", "Counter-Petition Twine",
        "Once a turn you may re-argue a card: discard one, draw one, and gain 1 Energy.",
        GraceAtTheBell(TwineUsed, ActThreeBossRelicCards.TwineId, ActThreeBossRelicCards.TwineTag));

    // "Start turn: if no HP lost during the previous enemy turn, gain 1 Energy and draw 1; otherwise gain 8
    // Block. No turn-1 effect."
    public static readonly StatusData SignedSettlement = Rule(
        "signed_settlement", "Signed Settlement",
        "A settled night pays: come through an enemy turn untouched for 1 Energy and a card, and come "
        + "through it hurt for 8 Block.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, SettlementHealth,
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Self),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    // Nothing written down yet is the first turn, and the first turn is nobody's settlement.
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                            Self, SettlementHealth),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(Self),
                            ComparisonOperator.GreaterOrEqual,
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                Self, SettlementHealth)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Hold<CardsDrawnTriggeredEffectContext>(1),
                            Draw<CardsDrawnTriggeredEffectContext>(1),
                        ]),
                        Block<CardsDrawnTriggeredEffectContext>(8)))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Notary of Old Growth — the three rings ────────────────────────────────────────────────────────

    // "First non-Junk card each turn establishes base cost; the next non-Junk card of the same base cost
    // costs 0. If no match is played, gain 5 Block at the end of the turn."
    public static readonly StatusData CountersealedRingOfPassage = Rule(
        "countersealed_ring_of_passage", "Countersealed Ring of Passage",
        "Your first real card each turn sets a price. The next card at that price is free — and a turn with "
        + "no match ends in 5 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(CardAuthoring.JunkTag))),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Counter<CardPlayedTriggeredEffectContext>(RingCost, ComparisonOperator.Equal, 0),
                        // The first real card of the turn writes the price down, plus one so that a free
                        // card is still a price and not an empty cell.
                        new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                            Self, RingCost, PlayedCostPlusOne(), relative: false),
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                Counter<CardPlayedTriggeredEffectContext>(
                                    RingMatched, ComparisonOperator.Equal, 0),
                                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                        Self, RingCost),
                                    ComparisonOperator.Equal, PlayedCostPlusOne())),
                            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                            [
                                SetCounter<CardPlayedTriggeredEffectContext>(RingMatched, 1),
                                // The match is what is free, and the match has already been paid for — so
                                // the price comes back rather than being taken off in advance.
                                RefundThePrice(),
                            ]))))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Counter<TurnEndedTriggeredEffectContext>(RingMatched, ComparisonOperator.Equal, 0),
                    Block<TurnEndedTriggeredEffectContext>(5))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(RingCost, 0),
                    SetCounter<CardsDrawnTriggeredEffectContext>(RingMatched, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "After the third non-Junk card in a turn, the next non-Junk card costs 0 and draws 1 when played."
    //
    // ADAPTATION: the design's consolation half — retain a chosen card and cheapen it — is the Ring of
    // Keeping's own consolation, and giving the same one to two rings would make them one relic. Here a
    // quiet turn simply keeps its own reward: the ring stays armed into the next turn.
    public static readonly StatusData CountersealedRingOfRestraint = Rule(
        "countersealed_ring_of_restraint", "Countersealed Ring of Restraint",
        "Play three real cards in a turn and the fourth is free and draws you a card. A turn that never "
        + "reaches three keeps the ring armed for the next one.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(CardAuthoring.JunkTag))),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            Counter<CardPlayedTriggeredEffectContext>(
                                RestraintArmed, ComparisonOperator.Greater, 0),
                            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                NonJunkPlayedThisTurn<CardPlayedTriggeredEffectContext>(),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            SetCounter<CardPlayedTriggeredEffectContext>(RestraintArmed, 0),
                            RefundThePrice(),
                            Draw<CardPlayedTriggeredEffectContext>(1),
                        ])))),
                nameof(TriggerEvent.CardPlayed)),
            // Armed as the hand arrives rather than as the turn starts: a relic is put on at the first bell,
            // and a turn-start trigger installed by it has already been and gone.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                SetCounter<CardsDrawnTriggeredEffectContext>(RestraintArmed, 1)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "End a turn with no non-Junk cards in hand → next turn +1 Energy and +2 Draw; otherwise retain one
    // chosen card and reduce its next-turn cost by 1."
    //
    // The hand cannot be read where a turn's end is answered — the turn's end puts it away first — so what
    // is left is read off the act's own hand budget.
    public static readonly StatusData CountersealedRingOfKeeping = Rule(
        "countersealed_ring_of_keeping", "Countersealed Ring of Keeping",
        "Empty your hand of real cards and the next turn opens with 1 Energy and two extra cards. Keep "
        + "something back and one card stays with you, a little cheaper.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        ActThree.RealCardsLeftInHand<TurnEndedTriggeredEffectContext>(),
                        ComparisonOperator.LessOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    SetCounter<TurnEndedTriggeredEffectContext>(RingMatched, 2),
                    KeepOneBack())),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(RingMatched, ComparisonOperator.Equal, 2),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Hold<CardsDrawnTriggeredEffectContext>(1),
                        Draw<CardsDrawnTriggeredEffectContext>(2),
                        SetCounter<CardsDrawnTriggeredEffectContext>(RingMatched, 0),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static IEffectNode<TurnEndedTriggeredEffectContext> KeepOneBack() =>
        new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
            Self, CardZone.Hand,
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<TurnEndedTriggeredEffectContext>(
                    Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                    StandardCombatIds.RetainedCardMark),
                new SetCardInstanceMarkCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                    StandardCombatIds.CardCostDeltaCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1), relative: true),
            ]),
            takeFirst: 1);

    // The price of the card just played, plus one, so that a free card is a price and not an empty cell.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> PlayedCostPlusOne() =>
        new AddExpression<CardPlayedTriggeredEffectContext>(
            new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                StandardCombatIds.EnergyResource),
            new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

    // "Costs 0" for a card already paid for: the Energy comes back, which is what the holder feels.
    private static IEffectNode<CardPlayedTriggeredEffectContext> RefundThePrice() =>
        Converter.HeldEnergy.Hold(
            new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                StandardCombatIds.EnergyResource));

    // ── Grandmother Clause — a gift with a clause on it ───────────────────────────────────────────────────
    //
    // Each of the three is her own courtesy, kept by the holder now: the gift is taken at the bell, and the
    // promise is answered when the turn ends. Breaking it costs 6 HP, which is not damage.

    public static readonly StatusData HoneySpoon = Grandmothers(
        "honey_spoon", "Honey Spoon", HoneyUsed, ActThreeBossRelicCards.HoneyId, ActThreeBossRelicCards.HoneyTag,
        "Once a turn you may take 2 Energy. Then end the turn with at least 1 Energy, or it costs you 6 HP.",
        kept: new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                Self, StandardCombatIds.EnergyResource),
            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)));

    public static readonly StatusData BetterChairCushion = Grandmothers(
        "better_chair_cushion", "Better Chair Cushion", CushionUsed,
        ActThreeBossRelicCards.CushionId, ActThreeBossRelicCards.CushionTag,
        "Once a turn you may take 14 Block. Then end the turn holding a real card, or it costs you 6 HP.",
        kept: new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            ActThree.RealCardsLeftInHand<TurnEndedTriggeredEffectContext>(),
            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)));

    public static readonly StatusData LastSliceTin = Grandmothers(
        "last_slice_tin", "Last-Slice Tin", TinUsed, ActThreeBossRelicCards.TinId, ActThreeBossRelicCards.TinTag,
        "Once a turn you may draw 2. Then play no more than four real cards, or it costs you 6 HP.",
        kept: new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            ActThree.RealCardsPlayed<TurnEndedTriggeredEffectContext>(),
            ComparisonOperator.LessOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(4)));

    // One shape for all three: the offer at the bell, and the reckoning at the turn's end — and the clause
    // is only answered for on a turn where the gift was actually taken, because declining is always free.
    private static StatusData Grandmothers(
        string id, string name, CounterId used, string cardId, TagId tag, string text,
        ICombatExpression<TurnEndedTriggeredEffectContext, bool> kept) =>
        Rule(id, name, text,
        [
            .. GraceAtTheBell(used, cardId, tag),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Counter<TurnEndedTriggeredEffectContext>(used, ComparisonOperator.Greater, 0),
                        new NotExpression<TurnEndedTriggeredEffectContext>(kept)),
                    // Direct HP loss, which no Block and no reaction sees: it is a promise, not a fight.
                    new SetHealthNode<TurnEndedTriggeredEffectContext>(
                        Self,
                        new SubtractExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Self),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(6))))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // ── The Answering Hill — stored weight ────────────────────────────────────────────────────────────────

    // "Mark the highest-Max-HP enemy as Landmark. The first crossing of 75%, 50% and 25% of its health each
    // grants 1 Energy and a card; several crossed at once all resolve."
    public static readonly StatusData SurveyedMilestone = Rule(
        "surveyed_milestone", "Surveyed Milestone",
        "The largest thing on the field is a landmark. Each time you first bring it past three quarters, "
        + "half and a quarter of its health, take 1 Energy and a card.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    Milestone(75, 1), Milestone(50, 2), Milestone(25, 3),
                ])),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    private static IEffectNode<CardPlayedTriggeredEffectContext> Milestone(int percent, int step)
    {
        var landmark = CombatantTargetSelectors.HighestHealthEnemyOfSource;

        return new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Counter<CardPlayedTriggeredEffectContext>(MilestoneStep, ComparisonOperator.Less, step),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantHealthPercentageExpression<CardPlayedTriggeredEffectContext>(landmark),
                    ComparisonOperator.Less, new ConstantExpression<CardPlayedTriggeredEffectContext>(percent))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                SetCounter<CardPlayedTriggeredEffectContext>(MilestoneStep, step),
                Hold<CardPlayedTriggeredEffectContext>(1),
                Draw<CardPlayedTriggeredEffectContext>(1),
            ]));
    }

    // "End a turn with 12 or more Block: you may bury 12 of it. The next turn gain 1 Energy and draw 1.
    // Once a turn."
    //
    // ADAPTATION: the burying is not offered, it is done — a prompt at the turn's end has nobody to ask, and
    // the trade is the same either way: twelve Block you were about to lose for an Energy and a card.
    public static readonly StatusData SurveyCairn = Rule(
        "survey_cairn", "Survey Cairn",
        "End a turn with 12 Block or more and the cairn buries twelve of it. The next turn opens with 1 "
        + "Energy and a card.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Counter<TurnEndedTriggeredEffectContext>(CairnUsed, ComparisonOperator.Equal, 0),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                                Self, StandardCombatIds.BlockDefensivePool),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(12))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                            Self, StandardCombatIds.BlockDefensivePool,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(-12)),
                        SetCounter<TurnEndedTriggeredEffectContext>(CairnBuried, 1),
                        SetCounter<TurnEndedTriggeredEffectContext>(CairnUsed, 1),
                    ]))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(CairnUsed, 0),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(CairnBuried, ComparisonOperator.Greater, 0),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Hold<CardsDrawnTriggeredEffectContext>(1),
                            Draw<CardsDrawnTriggeredEffectContext>(1),
                            SetCounter<CardsDrawnTriggeredEffectContext>(CairnBuried, 0),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Enemy-caused HP loss records Weight, at most 12. The next turn gain Block equal to Weight and your
    // first Deed deals +Weight; then it resets."
    public static readonly StatusData LoadstoneCairn = Rule(
        "loadstone_cairn", "Loadstone Cairn",
        "What the enemies take out of you is weight in the stone: next turn it is Block, and it is on your "
        + "first Deed. Up to 12.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, LoadstoneHealth,
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Self),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Self, LoadstoneWeight,
                        new MinExpression<CardsDrawnTriggeredEffectContext>(
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(12),
                            new MaxExpression<CardsDrawnTriggeredEffectContext>(
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0),
                                new SubtractExpression<CardsDrawnTriggeredEffectContext>(
                                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                        Self, LoadstoneHealth),
                                    new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(
                                        Self)))),
                        relative: false),
                    new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                        Self,
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                            Self, LoadstoneWeight)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
            // The weight is on the first Deed of the turn, and then the stone is empty again.
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(CardAuthoring.DeedTag)),
                        Counter<CardPlayedTriggeredEffectContext>(
                            LoadstoneWeight, ComparisonOperator.Greater, 0)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new DealDamageNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.LowestHealthEnemyOfSource,
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                Self, LoadstoneWeight)),
                        SetCounter<CardPlayedTriggeredEffectContext>(LoadstoneWeight, 0),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // ── The Queen Under the Hill — reciprocity ────────────────────────────────────────────────────────────

    // "At the start of a turn you may accept one Grace: +1 Energy, draw 1, or +10 Block. Accepting makes
    // every enemy gain 6 Block."
    public static readonly StatusData RoyalGraceCup = Rule(
        "royal_grace_cup", "Royal Grace Cup",
        "Once a turn the cup offers: an Energy, a card, or 10 Block. Take it and every enemy guards for 6.",
        GraceAtTheBell(GraceUsed, ActThreeBossRelicCards.GraceId, ActThreeBossRelicCards.GraceTag));

    // "The first time each turn you spend your last Energy by playing a card: +1 Favor, max 3. Start a turn
    // at 3: consume all, gain 1 Energy, 2 Draw and 8 Block."
    public static readonly StatusData HollowCourtToken = Rule(
        "hollow_court_token", "Hollow-Court Token",
        "Spend your purse to the bottom and the court remembers it, up to three times. Open a turn owed all "
        + "three and it pays: 1 Energy, two cards and 8 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCurrentResourceExpression<CardPlayedTriggeredEffectContext>(
                                Self, StandardCombatIds.EnergyResource),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        Counter<CardPlayedTriggeredEffectContext>(CourtFavor, ComparisonOperator.Less, 3)),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(
                        CourtSpent, AddCounter<CardPlayedTriggeredEffectContext>(CourtFavor, 1)))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(CourtSpent, 0),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(
                            CourtFavor, ComparisonOperator.GreaterOrEqual, 3),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            SetCounter<CardsDrawnTriggeredEffectContext>(CourtFavor, 0),
                            Hold<CardsDrawnTriggeredEffectContext>(1),
                            Draw<CardsDrawnTriggeredEffectContext>(2),
                            Block<CardsDrawnTriggeredEffectContext>(8),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Once per combat, free action: choose an enemy, remove all its Block, its next attack deals 10 less,
    // and the next card you play that turn costs 0."
    public static readonly StatusData SilverNameTally = Rule(
        "silver_name_tally", "Silver Name-Tally",
        "Once a combat, name one: its guard is gone, its next blow is 10 lighter, and your next card that "
        + "turn is free.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(TallyUsed, ComparisonOperator.Equal, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActThreeBossRelicCards.TallyId, ActThreeBossRelicCards.TallyTag))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ★★ A GRACE at the bell, in two triggers, and it MUST be two.
    //
    // All of these once-a-turn gifts used to do both jobs on `CardsDrawn`: clear the counter, then offer the
    // card. That reads right and is wrong twice over, because CardsDrawn fires on EVERY draw and not only on
    // the hand a turn opens with. A grace whose card DRAWS therefore re-armed itself out of its own effect —
    // play the Last-Slice Tin, draw 2, the draw clears the counter and hands over a fresh Tin, for ever — and
    // even the graces that draw nothing lost their clause the moment anything else drew a card, because the
    // counter that remembers "the gift was taken" had been wiped before the turn ended to ask about it.
    //
    // So the RESET belongs to the turn, and only the OFFER belongs to the draw: cleared at TurnStarted, handed
    // over at any draw where it has not already been taken. (The Silver Name-Tally, once per COMBAT, always
    // had this shape and is the reason it never looped.)
    private static IReadOnlyList<StatusTriggerData> GraceAtTheBell(CounterId used, string cardId, TagId tag) =>
    [
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            SetCounter<TurnStartedTriggeredEffectContext>(used, 0)), nameof(TriggerEvent.TurnStarted)),
        Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Counter<CardsDrawnTriggeredEffectContext>(used, ComparisonOperator.Equal, 0),
                OfferTheCard<CardsDrawnTriggeredEffectContext>(cardId, tag))),
            nameof(TriggerEvent.CardsDrawn)),
    ];

    // The card a relic hands over, offered while it is not already in hand.
    private static IEffectNode<TContext> OfferTheCard<TContext>(string cardId, TagId tag)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantZoneCardCountExpression<TContext>(Self, CardZone.Hand, tag),
                ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
            new CreateCardInstanceNode<TContext>(
                Self, new CardDefinitionId(cardId), CardZone.Hand, new ConstantExpression<TContext>(1)));
}
