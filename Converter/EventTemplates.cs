using RogueDeck.Core.Combat;
using RogueDeck.Run;

using BnbContent.Converter.Relics;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter;

// The authored non-combat stops of an act: its campfire (rest), its treasure room and its shop. The SHAPE is
// shared — heal a percentage, open a container for a relic, two shelves and a reroll — while the room the
// player is standing in is the act's own (ActRules.Rooms). Referenced by the generated map's NodeRefs.
public static class EventTemplates
{
    internal static EventScript Treasure(ConversionPools pools, string where, ActRooms act) => new("start",
    [
        new EventSituation("start", act.TreasureText,
        [
            new EventChoice("open",
            [
                // The canonical Normal pool on the act's own rarity curve. Until 2026-09-10 this was
                // RelicGrantSource, which drew from the PORTED v2 list, so a chest in the finished game handed
                // out demo relics; that source and the list behind it have both been deleted since. The master is explicit (§1): a Treasure relic reward is a Normal relic, and Shop
                // relics stay shop-exclusive.
                new OfferRewardRunEffect(
                    new RewardId($"{where}:relic"), pools.NormalRelicOnTheCurve(where), 1)
                    { Kind = RewardKinds.Relic },
            ], TextKey: act.TreasureOpenText),
            new EventChoice("leave", [], TextKey: act.TreasureLeaveText),
        ]),
    ]);

    internal static EventScript Rest(int healPercent, ActRooms act) => new("start",
    [
        new EventSituation("start", act.RestText,
        [
            new EventChoice("rest",
            [
                new ComputedHealRunEffect(RunExpr.Divide(
                    RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(healPercent)), RunExpr.Const(99)),
                    RunExpr.Const(100))),
            ], TextKey: $"{act.RestChoiceText} (heal {healPercent}% of max HP)"),
            // The campfire's other half: the design gives a waiting room two actions (BnB_Run_Systems_Master
            // §3 — Authorized Leave *or* Submit an Amendment) and only the heal was ever built.
            //
            // ⚠ It used to be offered unconditionally, on the theory that a choice with nothing to improve
            // "picks nothing and does nothing". To a player it did something: the rest was spent and the run
            // went straight back to the map (playtest 2026-09-26). It now requires an improvable card, and
            // while there is none it stays on screen greyed, saying why, instead of vanishing.
            new EventChoice("amend",
            [
                new UpgradeCardsRunEffect(
                    RunSelectors.DeckCards.Upgradable().ChooseByPlayer(1, "improve one card, permanently")),
            ], TextKey: $"{act.RestUpgradeChoiceText} (improve a card)",
                Requirement: RunExpr.GreaterThan(
                    RunExpr.Count(RunSelectors.DeckCards.Upgradable()), RunExpr.Const(0)),
                DisabledText: "There's nothing to improve."),
            new EventChoice("leave", [], TextKey: "Move on"),
        ]),
    ]);

    // The act's shop: the fixed inventory BnB_Run_Systems_Master §4.1 gives every regular shop — 3 General
    // cards, 4 Character cards, 2 Shop relics, 2 Normal relics — at the original's base prices, with the
    // card-removal service and a paid reroll. Its STOCK is the act's own: the card pools are gated to the act
    // the shop stands in.
}

public static class ShopTemplate
{
    private static readonly Dictionary<string, int> CardPrices =
        new() { ["common"] = 55, ["uncommon"] = 85, ["rare"] = 130 };

    // §4.5 declares prices balance variables, not content, so these are exactly what they were. A Shop relic
    // has no Common/Uncommon/Rare — its pool is its rarity — so it is priced at what an unlabelled relic
    // already cost on this shelf.
    //
    // Playtest 2026-09-26: "shop relics sind zu teuer bzw haben zu schlechte effekte — im zweifel einfach 20%
    // billiger". Every relic on the shelf came down by a fifth (130/190/260 → 104/152/208); the cards did not.
    private const int DefaultRelicPrice = 152;

    private static readonly Dictionary<Rarity, int> RelicPrices = new()
    {
        [Rarity.Common] = 104,
        [Rarity.Uncommon] = 152,
        [Rarity.Rare] = 208,
        [Rarity.Shop] = DefaultRelicPrice,
    };

    // What a relic costs on ANY shelf — the shop node's and the markets an event builds (the Licensed Vendor,
    // the Act-III fairs). They used to keep a copy of the table each, so a price change reached only one.
    public static int RelicPrice(BnbRelic relic) => RelicPrices.GetValueOrDefault(relic.Rarity, DefaultRelicPrice);

    public static ShopDefinition Build(ConversionPools pools, Random rng)
    {
        // Each shelf's POOL is deeper than what it shows, so a reroll can actually turn the stock over and a
        // relic that adds a slot has something to put in it.
        var general = Cards(pools, pools.GeneralCards, rng, depth: 8);
        var character = Cards(pools, pools.CharacterCards, rng, depth: 10);
        // THE WHOLE POOL, not a sample of it. A shelf's stock is drawn at CONVERSION time and shipped in the
        // document, so a depth of 5 meant five of the twenty-four Shop relics existed in a given build of the
        // game and the other nineteen could never be bought by anyone — twelve of them were named nowhere in
        // the shipped document at all (found 2026-09-10). Storing the pool whole costs a few hundred entries
        // and makes the master's "eligible in standard Shop relic inventory" true again; what the player SEES
        // is still the two the group shows, and a reroll now turns over the real pool.
        var shopRelics = Relics(pools.ShopRelicStock, rng, depth: pools.ShopRelicStock.Count);
        var normalRelics = Relics(pools.NormalRelicStock, rng, depth: pools.NormalRelicStock.Count);

        // FOUR SHELVES rather than one bag, and every entry says what it is. A relic that makes "one Normal
        // Relic" cheaper, or adds a slot to the normal relic shelf, or replaces the unsold cards, finds
        // nothing unless the stock is labelled — the effects behind a purchase are opaque. The shelf stamps
        // what the whole shelf is (ShopStockGroup.Tags); the entry says what the thing itself is.
        return new ShopDefinition([], OfferCount: 0,
            Reroll: new ShopReroll(StandardRunIds.Gold, 25),
            Services: [ShopService.RemoveCard(StandardRunIds.Gold, 75)],
            Stock:
            [
                new ShopStockGroup(ShopRelics.GeneralCardShelf, general, 3),
                new ShopStockGroup(ShopRelics.CharacterCardShelf, character, 4),
                new ShopStockGroup(ShopRelics.ShopRelicShelf, shopRelics, 2, [ShopRelics.ShopRelic]),
                new ShopStockGroup(ShopRelics.NormalRelicShelf, normalRelics, 2, [ShopRelics.NormalRelic]),
            ]);
    }

    // The shelf's stock is dealt on the act curve (ConversionPools.ActShare): a weighted shuffle — each card's key
    // is -ln(u)/weight, and the smallest keys are dealt — so an older act's card still turns up, less often.
    private static IReadOnlyList<ShopEntry> Cards(
        ConversionPools pools, IReadOnlyList<Cards.CardAuthoring.BnbCard> pool, Random rng, int depth) =>
        pool.Select(card => (Card: card, Key: -Math.Log(1 - rng.NextDouble()) / pools.ShelfWeight(card, pool)))
            .OrderBy(k => k.Key).Select(k => k.Card)
            .Take(depth).Select(card => new ShopEntry(
            $"buy-{card.Id}", StandardRunIds.Gold,
            CardPrices.GetValueOrDefault(card.Rarity, 85),
            [new AddCardToDeckRunEffect(new CardDefinitionId(card.Id))], card.Name,
            Kind: ShopEntryKinds.Card,
            // The card's own vocabulary — its type (Deed/Working/Rite) and whatever else it carries — plus its
            // rarity, so a rule that discounts "the first Form or Queue card" can find one.
            Tags: [card.Rarity, .. card.AllTags])).ToList();

    private static IReadOnlyList<ShopEntry> Relics(
        IReadOnlyList<BnbRelic> pool, Random rng, int depth) =>
        pool.OrderBy(_ => rng.Next()).Take(depth).Select(relic => new ShopEntry(
            $"buy-{relic.Id}", StandardRunIds.Gold,
            RelicPrice(relic),
            ConversionPools.Grant(relic), relic.Name,
            Kind: ShopEntryKinds.Relic)).ToList();
}
