using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Apprentice;

// The Sorcerer's Apprentice's own cards (sorcerers_apprentice_master.md §6–§9). Every card has TWO authored modes:
// what it does when PLAYED, and what it does in the WAND — written on the card as its text, a line break, and the
// "Wand:" line the screen shows apart. Its upgrade improves either or both. Numbers are the master's BALANCE DRAFT.
public static partial class ApprenticeCards
{
    public const string CharacterTag = "sorcerers_apprentice";

    // Where a card's words turn from its play text to its wand text — the frontend splits the card face here.
    public const string WandLine = "\nWand: ";

    // A tag naming the card itself, so "discard another card" can leave the card doing the asking out of the offer.
    public static string Self(string cardId) => "self_" + cardId.TrimEnd('+');

    private static BnbCard Card(
        string id, string name, string type, int cost, string play, CombatNodeModel program, string wandText,
        WandImprint wand, string rarity = "starter", IReadOnlyList<string>? tags = null, int act = 1) =>
        new(id, name, type, cost, play + WandLine + wandText, program, Rarity: rarity, Act: act,
            Tags: [CharacterTag, Self(id), .. tags ?? []], Wand: wand);

    private static BnbCard Up(this BnbCard card, string play, CombatNodeModel program, string wandText, WandImprint wand,
        int? cost = null, IReadOnlyList<string>? tags = null) =>
        card.Upgraded(play + WandLine + wandText, program, cost: cost, tags: tags, wand: wand);

    // ── starter (§6) ──────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard CandleStub = Card(
        "ap_candle_stub", "Candle Stub", DeedTag, 1,
        "Deal 6 damage. Apply 1 Singe.", Seq(Damage(6), Apply(ApprenticeKeywords.Singe, 1)),
        "Speak: deal 4 damage; in the first slot cast, also apply 1 Singe.",
        Speak(Hit(4), Put(ApprenticeKeywords.Singe, 1).Only(WandPosition.FirstExecuted)));

    private static readonly BnbCard ChalkLine = Card(
        "ap_chalk_line", "Chalk Line", WorkingTag, 1,
        "Gain 5 Block and 1 Grit.", Seq(Block(5), Apply(ApprenticeKeywords.Grit, 1, You)),
        "Speak: gain 4 Block; if another Speak follows, this cast's Exhaustion is 1 lower.",
        Speak(Guard(4)).GroundingIfFollowed(1));

    private static readonly BnbCard InkBlot = Card(
        "ap_ink_blot", "Ink Blot", WorkingTag, 1,
        "Draw 2 cards, then discard 1. Gain 1 Flow.", Seq(DrawDiscard("ap_ink_blot", 2, 1), Apply(ApprenticeKeywords.Flow, 1, You)),
        "Heighten: the next Speak deals or gives 50% more (a status-only Speak: 1 more stack).",
        Heighten(50));

    private static readonly BnbCard Flyleaf = Card(
        "ap_flyleaf", "Flyleaf", WorkingTag, 0,
        "Draw 1 card, then discard 1. Gain 1 Gust.", Seq(DrawDiscard("ap_flyleaf", 1, 1), Apply(ApprenticeKeywords.Gust, 1, You)),
        "Echo: repeat the last Speak at 50%, rounded up.",
        Echo(50));

    public static IReadOnlyList<BnbCard> Starter() =>
    [
        CandleStub, CandleStub.Up(
            "Deal 8 damage. Apply 1 Singe.", Seq(Damage(8), Apply(ApprenticeKeywords.Singe, 1)),
            "Speak: deal 6 damage; in the first slot cast, also apply 1 Singe.",
            Speak(Hit(6), Put(ApprenticeKeywords.Singe, 1).Only(WandPosition.FirstExecuted))),
        ChalkLine, ChalkLine.Up(
            "Gain 7 Block and 1 Grit.", Seq(Block(7), Apply(ApprenticeKeywords.Grit, 1, You)),
            "Speak: gain 6 Block; if another Speak follows, this cast's Exhaustion is 1 lower.",
            Speak(Guard(6)).GroundingIfFollowed(1)),
        InkBlot, InkBlot.Up(
            "Draw 3 cards, then discard 1. Gain 1 Flow.", Seq(DrawDiscard("ap_ink_blot", 3, 1), Apply(ApprenticeKeywords.Flow, 1, You)),
            "Heighten: the next Speak deals or gives 75% more (a status-only Speak: 1 more stack).",
            Heighten(75)),
        Flyleaf, Flyleaf.Up(
            "Draw 1 card, then discard 1. Gain 2 Gust.", Seq(DrawDiscard("ap_flyleaf", 1, 1), Apply(ApprenticeKeywords.Gust, 2, You)),
            "Echo: repeat the last Speak at 65%, rounded up.",
            Echo(65)),
    ];

    public static IReadOnlyList<string> StarterDeck =>
    [
        "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub",
        "ap_chalk_line", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line",
        "ap_ink_blot", "ap_flyleaf",
    ];

    public static IReadOnlyList<BnbCard> All() => [.. Starter(), .. Commons()];

    // His reward cards gated to an act, as the others' are: starters and the upgraded twins are never offered.
    public static IReadOnlyList<BnbCard> RewardPool(int act) => Cards.FinalCards.Offerable([.. Commons()], act);

    public static IReadOnlyList<CardData> Compile() => All().Select(c => c.Compile()).ToList();

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // "Draw N, then discard M" — the discard chosen from the hand, never the card doing the asking.
    private static CombatNodeModel DrawDiscard(string self, int draw, int discard) =>
        Seq(Draw(draw), Repeat(discard, Discard(self)));

    private static CombatNodeModel Discard(string self, string purpose = "choose a card to discard") =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: purpose, ExcludeTag: Self(self)),
            ToZone: CardZone.DiscardPile);
}
