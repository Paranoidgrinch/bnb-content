using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 7: the Exhausted pile (GENERAL_POOL_PLAN.md §4, added and approved 2026-11-08). Cards that
// fill it, cards that live off a full one, and cards that come back from it. The pile is called "Exhausted" in
// the game; "Archive" stays the Bureaucrat's word for putting a card there on purpose.
public static class GeneralExhausted
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BoneMeal = new(
        "bone_meal", "Bone Meal", WorkingTag, 0,
        "Exhaust up to 2 cards from your discard pile. Gain 5 Block for each.",
        Grind(2, 5));

    private static readonly BnbCard ColdHearth = new(
        "cold_hearth", "Cold Hearth", WorkingTag, 1,
        "Gain 6 Block, plus 3 for each card in your Exhausted pile. Count at most 8.",
        Block(Plus(Const(6), Times(AtMost(InZone(CardZone.ExhaustPile), 8), 3))));

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ReadByFirelight = new(
        "read_by_firelight", "Read by Firelight", WorkingTag, 1,
        "Exhaust the top 2 cards of your draw pile. You may play them this turn.",
        Firelight(2),
        Rarity: "uncommon");

    private static readonly BnbCard RakedFromTheAshes = new(
        "raked_from_the_ashes", "From the Ashes", WorkingTag, 1,
        "Put a card from your Exhausted pile into your hand. It Exhausts when played. Exhaust.",
        Rake(),
        Rarity: "uncommon", Act: 2, Tags: [ExhaustTag]);

    private static readonly BnbCard AshenEffigy = new(
        "ashen_effigy", "Ashen Effigy", DeedTag, 4,
        "Costs 1 less for each 2 cards in your Exhausted pile. Deal 18 damage.",
        Damage(18),
        Rarity: "uncommon", Tags: [GeneralPoolRules.EffigyTag]);

    private static readonly BnbCard SiftedAshes = new(
        "sifted_ashes", "Sifted Ashes", DeedTag, 1,
        "Deal 5 damage, plus 4 for each kind of card in your Exhausted pile (Deed, Working, Rite, Junk).",
        Damage(Plus(Const(5), Times(Kinds, 4))),
        Rarity: "uncommon", Act: 2);

    private static readonly BnbCard CorpseCandle = new(
        "corpse_candle", "Corpse Candle", DeedTag, 0,
        "Deal 5 damage. Exhaust. Whenever you Exhaust another card while this is Exhausted, return it to your hand.",
        Damage(5),
        Rarity: "uncommon", Tags: [ExhaustTag, GeneralPoolRules.CorpseCandleTag]);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard SalamanderInTheGrate = new(
        "salamander_in_the_grate", "Salamander", DeedTag, 2,
        "Deal 8 damage twice. Exhaust. At the start of your turn, if 6 or more cards are Exhausted, return this " +
        "to your hand.",
        Repeat(2, Damage(8)),
        Rarity: "rare", Act: 3, Tags: [ExhaustTag, GeneralPoolRules.SalamanderTag]);

    private static readonly BnbCard LastEmber = new(
        "last_ember", "Last Ember", WorkingTag, 1,
        "Play the card you Exhausted most recently again, for free. Exhaust.",
        Rekindle(),
        Rarity: "rare", Act: 2, Tags: [ExhaustTag, GeneralPoolRules.LastEmberTag]);

    private static readonly BnbCard Unburied = new(
        "unburied", "Unburied", WorkingTag, 2,
        "Shuffle every Exhausted card except Junk into your draw pile. Gain 3 Block for each. Exhaust.",
        Unbury(3),
        Rarity: "rare", Act: 4, Tags: [ExhaustTag]);

    public static IReadOnlyList<BnbCard> All() =>
    [
        BoneMeal, BoneMeal.Upgraded("Exhaust up to 3 cards from your discard pile. Gain 5 Block for each.",
            Grind(3, 5)),
        ColdHearth, ColdHearth.Upgraded(
            "Gain 8 Block, plus 5 for each card in your Exhausted pile. Count at most 8.",
            Block(Plus(Const(8), Times(AtMost(InZone(CardZone.ExhaustPile), 8), 5)))),

        ReadByFirelight, ReadByFirelight.Upgraded(
            "Exhaust the top 3 cards of your draw pile. You may play them this turn.", Firelight(3)),
        RakedFromTheAshes, RakedFromTheAshes.Upgraded(
            "Put a card from your Exhausted pile into your hand. It Exhausts when played. Exhaust.", cost: 0),
        AshenEffigy, AshenEffigy.Upgraded(
            "Costs 1 less for each 2 cards in your Exhausted pile. Deal 27 damage.", Damage(27)),
        SiftedAshes, SiftedAshes.Upgraded(
            "Deal 8 damage, plus 6 for each kind of card in your Exhausted pile (Deed, Working, Rite, Junk).",
            Damage(Plus(Const(8), Times(Kinds, 6)))),
        CorpseCandle, CorpseCandle.Upgraded(
            "Deal 8 damage. Exhaust. Whenever you Exhaust another card while this is Exhausted, return it to your " +
            "hand.", Damage(8)),

        SalamanderInTheGrate, SalamanderInTheGrate.Upgraded(
            "Deal 12 damage twice. Exhaust. At the start of your turn, if 6 or more cards are Exhausted, return " +
            "this to your hand.", Repeat(2, Damage(12))),
        LastEmber, LastEmber.Upgraded("Play the card you Exhausted most recently again, for free. Exhaust.", cost: 0),
        Unburied, Unburied.Upgraded(
            "Shuffle every Exhausted card except Junk into your draw pile. Gain 3 Block for each. Exhaust.", cost: 1),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // "Exhaust up to N cards from your discard pile. Gain B Block for each." One at a time, each time asking —
    // and only while there is something in the pile to grind.
    private static CombatNodeModel Grind(int left, int block) =>
        left <= 0
            ? Seq()
            : Seq(SetScratch(InZone(CardZone.DiscardPile)), IfScratch(ComparisonOperator.GreaterOrEqual, 1,
                CombatNodeModel.ChooseOptions(1,
                ["Exhaust a card from your discard pile", "Stop"],
                [
                    Seq(new CombatNodeModel("moveCardToZone", You,
                            Card: new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a card to exhaust"),
                            ToZone: CardZone.ExhaustPile),
                        Block(block),
                        Grind(left - 1, block)),
                    Seq(),
                ],
                "Bone Meal")));

    // "Exhaust the top N cards of your draw pile. You may play them this turn." Only a card in hand can be
    // played, so they come to the hand marked: played, they exhaust as a Temporary copy does; left over, the
    // end of the turn sweeps them from the discard pile into the Exhausted pile (GeneralPoolRules).
    private static CombatNodeModel Firelight(int top) =>
        CombatNodeModel.ForEachCard(You, CardZone.DrawPile, Seq(
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"),
                    ToTag: StandardCombatIds.TemporaryCopyMark.value),
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"),
                    ToTag: GeneralPoolRules.ReadByFirelightMark),
                new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"), ToZone: CardZone.Hand)),
            takeFirst: top);

    private const string Raking = "raked_from_the_ashes_pick";

    private static CombatNodeModel Rake() =>
        Seq(
            new CombatNodeModel("markCardInstance", You,
                Card: new CombatCardSpec("chosen", CardZone.ExhaustPile, Purpose: "choose a card to take back"),
                ToTag: Raking),
            CombatNodeModel.ForEachCard(You, CardZone.ExhaustPile, Seq(
                    new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"), ToTag: Raking,
                        Relative: true),
                    new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"),
                        ToTag: StandardCombatIds.TemporaryCopyMark.value),
                    new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"),
                        ToZone: CardZone.Hand)),
                mark: Raking));

    // How many of the four kinds lie in the Exhausted pile.
    private static CombatAmountSpec Kinds =>
        new[] { DeedTag, WorkingTag, RiteTag, JunkTag }
            .Select(tag => Once(CardsTagged(tag, CardZone.ExhaustPile)))
            .Aggregate(Plus);

    // The card exhausted most recently carries a mark the Applicant's rule keeps up to date.
    private static CombatNodeModel Rekindle() =>
        CombatNodeModel.ForEachCard(You, CardZone.ExhaustPile,
            new CombatNodeModel("replayCardProgram", Target, Card: new CombatCardSpec("iterated")),
            mark: GeneralPoolRules.LastExhaustedMark, takeFirst: 1);

    // "Shuffle every Exhausted card except Junk into your draw pile." Kind by kind, so Junk is never touched,
    // each card going in at a random place.
    private static CombatNodeModel Unbury(int blockEach) =>
        Seq(new[] { DeedTag, WorkingTag, RiteTag }
            .Select(tag => CombatNodeModel.ForEachCard(You, CardZone.ExhaustPile, Seq(
                    new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"),
                        ToZone: CardZone.DrawPile, Placement: GeneralPoolRules.RandomPlacement),
                    Block(blockEach)),
                tag: tag))
            .ToArray());
}
