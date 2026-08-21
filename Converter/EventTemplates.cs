using RogueDeck.Core.Combat;
using RogueDeck.Run;

using BnbContent.Converter.Relics;

namespace BnbContent.Converter;

// The authored non-combat stops of Act I: the waiting room (rest), the sealed evidence crate (treasure) and
// the city shop. Written once here and referenced by the generated map's NodeRefs — they used to be created
// inline while baking a fixed map.
public static class EventTemplates
{
    public static EventScript Treasure(ConversionPools pools, string where) => new("start",
    [
        new EventSituation("start",
            "A sealed evidence crate, stamped in three colors of wax. Nobody has claimed it in decades.",
        [
            new EventChoice("open",
            [
                new OfferRewardRunEffect(new RewardId($"{where}:relic"), pools.RelicGrantSource(null, where), 1),
            ], TextKey: "Break the seals"),
            new EventChoice("leave", [], TextKey: "Leave it for the archivists"),
        ]),
    ]);

    public static EventScript Rest(int healPercent) => new("start",
    [
        new EventSituation("start",
            "The waiting room. The chairs are terrible, but nobody can reach you here.",
        [
            new EventChoice("rest",
            [
                new ComputedHealRunEffect(RunExpr.Divide(
                    RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(healPercent)), RunExpr.Const(99)),
                    RunExpr.Const(100))),
            ], TextKey: $"Wait it out (heal {healPercent}% of max HP)"),
            new EventChoice("leave", [], TextKey: "Skip the queue"),
        ]),
    ]);

    // The city shop: five rarity-weighted cards at the original's base prices, two relics (pickup
    // effects bundled), the card-removal service, and a paid reroll.
}

public static class ShopTemplate
{
    public static ShopDefinition Build(BabData data, ConversionPools pools, Random rng)
    {
        var cardPrices = new Dictionary<string, int> { ["common"] = 55, ["uncommon"] = 85, ["rare"] = 130 };
        var relicPrices = new Dictionary<string, int> { ["common"] = 130, ["uncommon"] = 190, ["rare"] = 260 };

        // Each shelf's POOL is deeper than what it shows, so a reroll can actually turn the stock over and a
        // relic that adds a slot has something to put in it.
        var cards = pools.RewardCards.OrderBy(_ => rng.Next()).Take(12).ToList();
        var relics = pools.Relics.OrderBy(_ => rng.Next()).Take(5).ToList();

        // Two SHELVES rather than one bag, and every entry says what it is. A relic that makes "one Normal
        // Relic" cheaper, or adds a slot to the relic shelf, or replaces the unsold cards, finds nothing
        // unless the stock is labelled — the effects behind a purchase are opaque.
        var cardEntries = cards.Select(card => new ShopEntry(
            $"buy-{card.Id}", StandardRunIds.Gold,
            cardPrices.GetValueOrDefault(card.Rarity, 85),
            [new AddCardToDeckRunEffect(new CardDefinitionId(card.Id))], card.Name,
            Kind: ShopEntryKinds.Card,
            // The card's own vocabulary — its type (Deed/Working/Rite) and whatever else it carries — plus its
            // rarity, so a rule that discounts "the first Form or Queue card" can find one.
            Tags: [card.Rarity, .. card.AllTags])).ToList();

        var relicEntries = relics.Select(relic => new ShopEntry(
            $"buy-{relic.Relic.Id}", StandardRunIds.Gold,
            relicPrices.GetValueOrDefault(relic.Source.Rarity ?? "common", 190),
            ConversionPools.RelicOffer(relic).Grant, relic.Source.Name,
            Kind: ShopEntryKinds.Relic,
            Tags: [ShopRelics.NormalRelic])).ToList();

        return new ShopDefinition([], OfferCount: 0,
            Reroll: new ShopReroll(StandardRunIds.Gold, 25),
            Services: [ShopService.RemoveCard(StandardRunIds.Gold, 75)],
            Stock:
            [
                new ShopStockGroup(ShopRelics.CardShelf, cardEntries, 5),
                new ShopStockGroup(ShopRelics.RelicShelf, relicEntries, 2),
            ]);
    }
}
