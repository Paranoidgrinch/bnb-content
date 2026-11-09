using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 2: the draw engine (GENERAL_POOL_PLAN.md §4, approved 2026-11-08). Drawing, Retain,
// ordering the top of the draw pile, putting cards back; the Rares pay off a big hand.
public static class GeneralDraw
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard SieveAndShears = new(
        "sieve_and_shears", "Sieve and Shears", WorkingTag, 0,
        "Look at the top 3 cards of your draw pile. Put any of them on the bottom. Draw 1 card.",
        Seq(Sieve(3), Draw(1)));

    // Kept; Draw 3 instead of 2 (2026-11-08).
    private static readonly BnbCard BorrowedCandle = new(
        "borrowed_candle", "Borrowed Candle", WorkingTag, 0,
        "Draw 3 cards. Put a card from your hand on top of your draw pile. Exhaust.",
        Seq(Draw(3), PutBack()),
        Tags: [ExhaustTag]);

    // Content review (2026-11-08): was "Draw 2 cards. Gain 5 Block." — now it pays off a big hand.
    private static readonly BnbCard LamplitStudy = new(
        "lamplit_study", "Lamplit Study", WorkingTag, 1,
        "Draw 2 cards. Then gain 1 Block for each card in your hand.",
        Study(2));

    private static readonly BnbCard DogEaredAlmanac = new(
        "dog_eared_almanac", "Almanac", WorkingTag, 0,
        "Retain. Draw 1 card. If this was in your hand since last turn, draw 2 more.",
        Almanac(2),
        Tags: [GeneralPoolRules.AlmanacTag], RetainInHand: true);

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard NightScrivener = new(
        "night_scrivener", "Night Scrivener", WorkingTag, 1,
        "Draw 3 cards. Choose a card in your hand: it gains Retain this combat.",
        Seq(Draw(3), GrantRetain()),
        Rarity: "uncommon");

    // Kept (crossed_sigil); now costs 0 (2026-11-08).
    private const string FreshLedgerTag = "fresh_ledger";

    private static readonly BnbCard FreshLedger = new(
        "crossed_sigil", "Fresh Ledger", WorkingTag, 0,
        "Exhaust another card from your hand. Draw 2 cards.",
        Ledger(2),
        Rarity: "uncommon", Tags: [FreshLedgerTag]);

    private static readonly BnbCard WheatFromChaff = new(
        "wheat_from_chaff", "Wheat from Chaff", WorkingTag, 0,
        "Reveal the top 3 cards of your draw pile. Put either the Deeds or the rest into your hand; discard the others.",
        Winnow(3),
        Rarity: "uncommon", Act: 2);

    private static readonly BnbCard GatheredGossip = new(
        "gathered_gossip", "Gathered Gossip", WorkingTag, 0,
        "Draw 1 card, plus 1 for each enemy. Exhaust.",
        Gossip(1),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ErrandRook = new(
        "errand_rook", "Errand Rook", RiteTag, 1,
        "At the start of each of your turns, draw 1 more card. Whenever your hand is full, gain 3 Block. Exhaust.",
        InstallRite(GeneralPoolRules.ErrandRook),
        Rarity: "rare", Act: 2);

    // Kept; the original now stays in hand and the copy is free (2026-11-08).
    private static readonly BnbCard MoonlitCounterfeit = new(
        "moonlit_counterfeit", "Counterfeit", WorkingTag, 1,
        "Create a Temporary copy of a card in your hand. It costs 0. Exhaust.",
        Counterfeit(),
        Rarity: "rare", Act: 3, Tags: [ExhaustTag, CounterfeitTag]);

    public static IReadOnlyList<BnbCard> All() =>
    [
        SieveAndShears, SieveAndShears.Upgraded(
            "Look at the top 3 cards of your draw pile. Put any of them on the bottom. Draw 2 cards.",
            Seq(Sieve(3), Draw(2))),
        BorrowedCandle, BorrowedCandle.Upgraded(
            "Draw 4 cards. Put a card from your hand on top of your draw pile. Exhaust.", Seq(Draw(4), PutBack())),
        LamplitStudy, LamplitStudy.Upgraded("Draw 3 cards. Then gain 1 Block for each card in your hand.", Study(3)),
        DogEaredAlmanac, DogEaredAlmanac.Upgraded(
            "Retain. Draw 1 card. If this was in your hand since last turn, draw 3 more.", Almanac(3)),

        NightScrivener, NightScrivener.Upgraded(
            "Draw 4 cards. Choose a card in your hand: it gains Retain this combat.", Seq(Draw(4), GrantRetain())),
        FreshLedger, FreshLedger.Upgraded("Exhaust another card from your hand. Draw 3 cards.", Ledger(3)),
        WheatFromChaff, WheatFromChaff.Upgraded(
            "Reveal the top 4 cards of your draw pile. Put either the Deeds or the rest into your hand; discard " +
            "the others.", Winnow(4)),
        GatheredGossip, GatheredGossip.Upgraded("Draw 2 cards, plus 1 for each enemy. Exhaust.", Gossip(2)),

        ErrandRook, ErrandRook.Upgraded(
            "At the start of each of your turns, draw 1 more card. Whenever your hand is full, gain 3 Block. " +
            "Exhaust.", InstallRite(GeneralPoolRules.ErrandRook), cost: 0),
        MoonlitCounterfeit, MoonlitCounterfeit.Upgraded(
            "Create a Temporary copy of a card in your hand. It costs 0. Exhaust.", cost: 0),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // "Look at the top N cards of your draw pile. Put any of them on the bottom." One at a time, each time
    // asking whether to send another: a card sent down leaves the others it was shown with on top, so the
    // next choice looks at one card fewer and never at a card that was not shown.
    private static CombatNodeModel Sieve(int top) =>
        top <= 0
            ? Seq()
            : CombatNodeModel.ChooseOptions(1,
                ["Put one of them on the bottom", "Keep them as they are"],
                [
                    Seq(new CombatNodeModel("moveCardToZone", You,
                            Card: new CombatCardSpec("chosen", CardZone.DrawPile,
                                Purpose: "choose a card for the bottom of your draw pile", Top: top),
                            ToZone: CardZone.DrawPile, Placement: ZonePlacement.Bottom),
                        Sieve(top - 1)),
                    Seq(),
                ],
                "Sieve and Shears");

    // The Study is still in the hand while it resolves; it does not count itself.
    private static CombatNodeModel Study(int draw) =>
        Seq(Draw(draw), Block(Max(Const(0), Minus(InZone(CardZone.Hand), Const(1)))));

    private static CombatNodeModel PutBack() =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to put back"),
            ToZone: CardZone.DrawPile, Placement: ZonePlacement.Top);

    // "If this was in your hand since last turn": the turn's end counts the Almanacs still in hand (the hand
    // has been discarded by then, so only the retained ones are left), and each Almanac played spends one of
    // them. Which copy spends the credit does not matter; how many bonuses are paid does.
    private static CombatNodeModel Almanac(int more) =>
        Seq(
            Draw(1),
            SetScratch(new CombatAmountSpec("counter", SelectorKey: You, CounterId: GeneralPoolRules.AlmanacsHeld.value)),
            IfScratch(ComparisonOperator.GreaterOrEqual, 1, Seq(
                new CombatNodeModel("setCombatantCounter", You, Const(-1),
                    CounterId: GeneralPoolRules.AlmanacsHeld.value, Relative: true),
                Draw(more))));

    private static CombatNodeModel GrantRetain() =>
        new("markCardInstance", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to Retain"),
            ToTag: StandardCombatIds.RetainedCardMark.value);

    private static CombatNodeModel Ledger(int draw) =>
        Seq(new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to exhaust",
                    ExcludeTag: FreshLedgerTag),
                ToZone: CardZone.ExhaustPile),
            Draw(draw));

    // "Reveal the top N. Put either the Deeds or the rest into your hand; discard the others." The revealed
    // cards are marked first, so neither half can reach past them into the pile.
    private const string Revealed = "wheat_from_chaff_revealed";

    private static CombatNodeModel Winnow(int top) =>
        Seq(
            CombatNodeModel.ForEachCard(You, CardZone.DrawPile,
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"), ToTag: Revealed),
                takeFirst: top),
            CombatNodeModel.ChooseOptions(1,
                ["Take the Deeds", "Take the rest"],
                [
                    Seq(MoveRevealed(CardZone.Hand, DeedTag), MoveRevealed(CardZone.DiscardPile)),
                    Seq(MoveRevealed(CardZone.DiscardPile, DeedTag), MoveRevealed(CardZone.Hand)),
                ],
                "Wheat from Chaff"));

    private static CombatNodeModel MoveRevealed(CardZone to, string tag = "") =>
        CombatNodeModel.ForEachCard(You, CardZone.DrawPile, Seq(
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"), ToTag: Revealed,
                    Relative: true),
                new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"), ToZone: to)),
            tag: tag, mark: Revealed);

    private static CombatNodeModel Gossip(int baseDraw) =>
        new("drawCards", You, CombatAmountSpec.Binary("add", Const(baseDraw),
            new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec(AllEnemies))));

    // "Create a Temporary copy of a card in your hand. It costs 0." A program cannot name the card it has just
    // made, so the hand is marked first: afterwards every card is made free and every marked one put back as
    // it was — which leaves exactly the new copy free. Relative both ways, so another discount survives.
    private const string CounterfeitTag = "moonlit_counterfeit";
    private const string BeforeCopy = "moonlit_counterfeit_before";

    private static CombatNodeModel Counterfeit() =>
        Seq(
            CombatNodeModel.ForEachCard(You, CardZone.Hand,
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"), ToTag: BeforeCopy)),
            new CombatNodeModel("createCardCopy", You, Const(1),
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to copy",
                    ExcludeTag: CounterfeitTag),
                ToZone: CardZone.Hand, Temporary: true),
            CombatNodeModel.ForEachCard(You, CardZone.Hand, CostDelta(-FreeDelta)),
            CombatNodeModel.ForEachCard(You, CardZone.Hand, Seq(
                    CostDelta(FreeDelta),
                    new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"),
                        ToTag: BeforeCopy, Relative: true)),
                mark: BeforeCopy));

    private const int FreeDelta = 99;

    private static CombatNodeModel CostDelta(int delta) =>
        new("setCardInstanceMarkCounter", You, Const(delta), Card: new CombatCardSpec("iterated"),
            CounterId: StandardCombatIds.CardCostDeltaCounter.value, Relative: true);
}
