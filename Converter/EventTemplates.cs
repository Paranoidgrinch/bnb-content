using RogueDeck.Core.Combat;
using RogueDeck.Run;

using BnbContent.Converter.Relics;

namespace BnbContent.Converter;

// The authored non-combat stops of an act: its campfire (rest), its treasure room and its shop. The SHAPE is
// shared — heal a percentage, open a container for a relic, two shelves and a reroll — while the room the
// player is standing in is the act's own (ActRules). Referenced by the generated map's NodeRefs.
public static class EventTemplates
{
    internal static EventScript Treasure(ConversionPools pools, string where, ActRules act) => new("start",
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

    internal static EventScript Rest(int healPercent, ActRules act) => new("start",
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

    // The act's shop: five rarity-weighted cards at the original's base prices, two relics (pickup effects
    // bundled), the card-removal service, and a paid reroll. Its STOCK is the act's own — the pools it is
    // built from are gated to the act the shop stands in.
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
