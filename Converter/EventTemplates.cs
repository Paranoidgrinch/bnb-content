using RogueDeck.Core.Combat;
using RogueDeck.Run;

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

        var cards = pools.RewardCards.OrderBy(_ => rng.Next()).Take(5).ToList();
        var relics = pools.Relics.OrderBy(_ => rng.Next()).Take(2).ToList();

        var entries = new List<ShopEntry>();
        foreach (var card in cards)
        {
            var id = CardMapper.MapCardId(card.Id);
            entries.Add(new ShopEntry($"buy-{id}", StandardRunIds.Gold,
                cardPrices.GetValueOrDefault(card.Rarity ?? "common", 85),
                [new AddCardToDeckRunEffect(new CardDefinitionId(id))], card.Name));
        }
        foreach (var relic in relics)
        {
            entries.Add(new ShopEntry($"buy-{relic.Relic.Id}", StandardRunIds.Gold,
                relicPrices.GetValueOrDefault(relic.Source.Rarity ?? "common", 190),
                ConversionPools.RelicOffer(relic).Grant, relic.Source.Name));
        }
        return new ShopDefinition(entries,
            OfferCount: entries.Count,
            Reroll: new ShopReroll(StandardRunIds.Gold, 25),
            Services: [ShopService.RemoveCard(StandardRunIds.Gold, 75)]);
    }
}
