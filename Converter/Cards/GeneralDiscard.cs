using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 3: discarding and exhausting (GENERAL_POOL_PLAN.md §4, approved 2026-11-08). Unread cards,
// cards that make you throw cards away, and rewards for cards that vanish.
public static class GeneralDiscard
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard SpitefulDraft = new(
        "spiteful_draft", "Spiteful Draft", DeedTag, 1,
        "Deal 7 damage. Unread: deal 7 damage to a random enemy.",
        Damage(7),
        Tags: Unread("spiteful_draft"));

    private static readonly BnbCard ScatteredNotes = new(
        "scattered_notes", "Scattered Notes", WorkingTag, 0,
        "Discard 1 card. Draw 2 cards.",
        Seq(DiscardChosen(), Draw(2)));

    private static readonly BnbCard AshenReceipt = new(
        "ashen_receipt", "Ashen Receipt", WorkingTag, 1,
        "Gain 7 Block. Unread: draw 1 card.",
        Block(7),
        Tags: Unread("ashen_receipt"));

    private static readonly BnbCard SmoulderingCorrespondence = new(
        "smouldering_correspondence", "Burning Letter", DeedTag, 1,
        "Deal 5 damage to ALL enemies. Unread: deal 5 damage to ALL enemies.",
        DamageAll(5),
        Tags: Unread("smouldering_correspondence"));

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard HearthKindling = new(
        "hearth_kindling", "Hearth Kindling", WorkingTag, 0,
        "Discard any number of cards. Draw that many. Gain 3 Block for each.",
        Kindle(3),
        Rarity: "uncommon");

    // ADAPTATION: "It costs 0 this turn" — a per-copy discount lasts until the copy is played, so a card not
    // played this turn keeps it.
    private static readonly BnbCard AshRemembers = new(
        "ash_remembers", "Ash Remembers", WorkingTag, 1,
        "Put a card from your discard pile into your hand. It costs 0 this turn. Exhaust.",
        Remember(),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    private static readonly BnbCard WeightOfPaper = new(
        "weight_of_paper", "Weight of Paper", DeedTag, 2,
        "Deal 6 damage, plus 2 for each card in your discard pile. Count at most 12.",
        Damage(Plus(Const(6), Times(AtMost(InZone(CardZone.DiscardPile), 12), 2))),
        Rarity: "uncommon", Act: 2);

    private static readonly BnbCard BurntOffering = new(
        "burnt_offering", "Burnt Offering", WorkingTag, 0,
        "Exhaust a card from your hand. Gain 8 Block. Draw 1 card.",
        Offering(8),
        Rarity: "uncommon");

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard WhisperingGallery = new(
        "whispering_gallery", "Whispers", RiteTag, 1,
        "Whenever a card leaves your hand without being played, a random enemy gets 1 Doubt. Exhaust.",
        InstallRite(GeneralPoolRules.WhisperingGallery),
        Rarity: "rare", Act: 3);

    private static readonly BnbCard PyreOfOldRecords = new(
        "pyre_of_old_records", "Record Pyre", RiteTag, 1,
        "Whenever a card is exhausted, deal 7 damage to a random enemy and gain 7 Block. Exhaust.",
        InstallRite(GeneralPoolRules.PyreOfOldRecords),
        Rarity: "rare", Act: 2);

    public static IReadOnlyList<BnbCard> All() =>
    [
        SpitefulDraft, SpitefulDraft.Upgraded("Deal 10 damage. Unread: deal 10 damage to a random enemy.",
            Damage(10), tags: Unread("spiteful_draft+")),
        ScatteredNotes, ScatteredNotes.Upgraded("Discard 1 card. Draw 3 cards.", Seq(DiscardChosen(), Draw(3))),
        AshenReceipt, AshenReceipt.Upgraded("Gain 10 Block. Unread: draw 1 card.", Block(10),
            tags: Unread("ashen_receipt+")),
        SmoulderingCorrespondence, SmoulderingCorrespondence.Upgraded(
            "Deal 8 damage to ALL enemies. Unread: deal 8 damage to ALL enemies.", DamageAll(8),
            tags: Unread("smouldering_correspondence+")),

        HearthKindling, HearthKindling.Upgraded(
            "Discard any number of cards. Draw that many. Gain 4 Block for each.", Kindle(4)),
        AshRemembers, AshRemembers.Upgraded(
            "Put a card from your discard pile into your hand. It costs 0 this turn. Exhaust.", cost: 0),
        WeightOfPaper, WeightOfPaper.Upgraded(
            "Deal 9 damage, plus 3 for each card in your discard pile. Count at most 12.",
            Damage(Plus(Const(9), Times(AtMost(InZone(CardZone.DiscardPile), 12), 3)))),
        BurntOffering, BurntOffering.Upgraded("Exhaust a card from your hand. Gain 12 Block. Draw 1 card.",
            Offering(12)),

        WhisperingGallery, WhisperingGallery.Upgraded(
            "Whenever a card leaves your hand without being played, a random enemy gets 1 Doubt. Exhaust.",
            InstallRite(GeneralPoolRules.WhisperingGallery), cost: 0),
        PyreOfOldRecords, PyreOfOldRecords.Upgraded(
            "Whenever a card is exhausted, deal 7 damage to a random enemy and gain 7 Block. Exhaust.",
            InstallRite(GeneralPoolRules.PyreOfOldRecords), cost: 0),
    ];

    public static IReadOnlyList<(string CardId, CombatNodeModel Effect)> Unreads() =>
    [
        ("spiteful_draft", DamageRandomEnemy(7)),
        ("spiteful_draft+", DamageRandomEnemy(10)),
        ("ashen_receipt", Draw(1)),
        ("ashen_receipt+", Draw(1)),
        ("smouldering_correspondence", DamageAll(5)),
        ("smouldering_correspondence+", DamageAll(8)),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // A discard by a card effect: one card at a time, through moveCardToZone, so the move is heard (Unread,
    // Whispers) — the end-of-turn discard is a different event.
    public static CombatNodeModel DiscardChosen() =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to discard"),
            ToZone: CardZone.DiscardPile);

    // "Discard any number of cards. Draw that many. Gain N Block for each." Asked one card at a time while the
    // hand holds any; what was thrown away is counted off the hand itself, and only then drawn, so the new
    // cards cannot be thrown after them.
    private static CounterId Kindled => new("hearth_kindling_count");
    private static CounterId KindlingDone => new("hearth_kindling_done");

    private const int MostKindled = 10;

    private static CombatAmountSpec HandSize => InZone(CardZone.Hand);

    private static CombatNodeModel Kindle(int blockEach) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, HandSize, CounterId: Kindled.value, Relative: false),
            new CombatNodeModel("setCombatantCounter", You, Const(0), CounterId: KindlingDone.value, Relative: false),
            Repeat(MostKindled, Seq(
                // The Kindling itself is still in hand while it resolves.
                SetScratch(HandSize),
                IfScratch(ComparisonOperator.GreaterOrEqual, 2,
                    If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Equal,
                            Right: 0, Id: KindlingDone.value),
                        CombatNodeModel.ChooseOptions(1,
                            ["Discard a card", "Stop"],
                            [
                                DiscardChosen(),
                                new CombatNodeModel("setCombatantCounter", You, Const(1), CounterId: KindlingDone.value,
                                    Relative: false),
                            ],
                            "Hearth Kindling"))))),
            // An Unread that draws (Ashen Receipt) can grow the hand while it is being thrown: never below none.
            new CombatNodeModel("setCombatantCounter", You, Max(Const(0), Minus(KindledCount, HandSize)),
                CounterId: Kindled.value, Relative: false),
            new CombatNodeModel("drawCards", You, KindledCount),
            Block(Times(KindledCount, blockEach)));

    private static CombatAmountSpec KindledCount => new("counter", SelectorKey: You, CounterId: Kindled.value);

    private const string Remembered = "ash_remembers_pick";

    private static CombatNodeModel Remember() =>
        Seq(
            new CombatNodeModel("markCardInstance", You,
                Card: new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a card to take back"),
                ToTag: Remembered),
            CombatNodeModel.ForEachCard(You, CardZone.DiscardPile, Seq(
                    new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"),
                        ToTag: Remembered, Relative: true),
                    new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"),
                        ToZone: CardZone.Hand),
                    new CombatNodeModel("setCardInstanceMarkCounter", You, Const(-99),
                        Card: new CombatCardSpec("iterated"), CounterId: StandardCombatIds.CardCostDeltaCounter.value,
                        Relative: true)),
                mark: Remembered));

    private static CombatNodeModel Offering(int block) =>
        Seq(new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to exhaust"),
                ToZone: CardZone.ExhaustPile),
            Block(block),
            Draw(1));
}
