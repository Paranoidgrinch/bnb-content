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
                new OfferRewardRunEffect(new RewardId($"{where}:relic"), pools.RelicGrantSource(null, where), 1),
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
            // §3 — Authorized Leave *or* Submit an Amendment) and only the heal was ever built. Offered
            // unconditionally: a "how many cards could be improved" guard is not expressible as DATA (a count
            // over a selector is an escape node and would not serialize), and it is not needed — a choice with
            // nothing to improve picks nothing and does nothing.
            new EventChoice("amend",
            [
                new UpgradeCardsRunEffect(
                    RunSelectors.DeckCards.Upgradable().ChooseByPlayer(1, "improve one card, permanently")),
            ], TextKey: $"{act.RestUpgradeChoiceText} (improve a card)"),
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
    private const int DefaultRelicPrice = 190;

    private static readonly Dictionary<Rarity, int> RelicPrices = new()
    {
        [Rarity.Common] = 130,
        [Rarity.Uncommon] = 190,
        [Rarity.Rare] = 260,
        [Rarity.Shop] = DefaultRelicPrice,
    };

    public static ShopDefinition Build(ConversionPools pools, Random rng)
    {
        // Each shelf's POOL is deeper than what it shows, so a reroll can actually turn the stock over and a
        // relic that adds a slot has something to put in it.
        var general = Cards(pools.GeneralCards, rng, depth: 8);
        var character = Cards(pools.CharacterCards, rng, depth: 10);
        var shopRelics = Relics(pools.ShopRelicStock, rng, depth: 5);
        var normalRelics = Relics(pools.NormalRelicStock, rng, depth: 5);

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

    private static IReadOnlyList<ShopEntry> Cards(
        IReadOnlyList<Cards.CardAuthoring.BnbCard> pool, Random rng, int depth) =>
        pool.OrderBy(_ => rng.Next()).Take(depth).Select(card => new ShopEntry(
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
            RelicPrices.GetValueOrDefault(relic.Rarity, DefaultRelicPrice),
            ConversionPools.Grant(relic), relic.Name,
            Kind: ShopEntryKinds.Relic)).ToList();
}
