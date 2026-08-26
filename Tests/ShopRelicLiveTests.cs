using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The Shop relics are economy, and economy relics never "fire": a discount is simply true of the shelf while
// the relic is worn. That makes them exactly the kind of content that can quietly do nothing — a tag spelled
// one way on the relic and another way on the stock, and the price is just the price. So these tests walk the
// REAL city shop with the relic on, and check the till.
public class ShopRelicLiveTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly IReadOnlyList<MappedRelic> Relics = Data.Relics.Select(RelicMapper.Map).ToList();
    private static readonly ConversionPools Pools = ConversionPools.Build(Data, Relics, act: 1);
    private static readonly RunResourceId Gold = StandardRunIds.Gold;

    private static ShopDefinition CityShop() => ShopTemplate.Build(Data, Pools, new Random(7));

    // The shop's stock has to SAY what it is, or nothing the relics do can find it.
    [Fact]
    public void The_city_shop_labels_its_shelves_and_its_stock()
    {
        var shop = CityShop();
        var shelves = shop.Shelves().ToDictionary(shelf => shelf.Id);

        Assert.Equal(2, shelves.Count);
        Assert.All(shelves[ShopRelics.CardShelf].Offers,
            entry => Assert.Equal(ShopEntryKinds.Card, entry.Kind));
        Assert.All(shelves[ShopRelics.RelicShelf].Offers,
            entry => Assert.Contains(ShopRelics.NormalRelic, entry.Tags!));
        Assert.Contains(shop.Services!, service => service.Tags!.Contains(ShopRelics.Removal));
    }

    // Secondhand Reliquary: one Normal Relic is 30% off, and buying a Normal Relic costs 5 HP — once.
    [Fact]
    public void A_secondhand_relic_is_cheaper_and_costs_blood()
    {
        var shop = CityShop();
        var relic = OnOffer(shop, ShopRelics.RelicShelf, "secondhand_reliquary")[0];
        var run = Visit("secondhand_reliquary", shop, gold: 1000, relic.Entry.Id, "leave");

        Assert.Equal(1000 - relic.Price, run.GetResource(Gold));
        Assert.Equal((int)Math.Round(relic.Entry.Price * 0.7, MidpointRounding.AwayFromZero), relic.Price);
        Assert.Equal(25, run.Health.Current); // 30 − 5
    }

    // Crooked Display Case: a third relic on a two-relic shelf, and it is the dear one.
    [Fact]
    public void The_display_case_puts_out_one_more_relic_at_a_markup()
    {
        var shop = CityShop();
        var run = NewRun(1000);
        run.AddRelic(Wearing("crooked_display_case"));
        var shelf = new ShopShelf(run, shop);

        var relics = shelf.Slots.Where(slot => slot.GroupId == ShopRelics.RelicShelf).ToList();
        Assert.Equal(3, relics.Count);
        var extra = Assert.Single(relics, slot => slot.Entry.Tags!.Contains(ShopRelics.Extra));
        Assert.Equal((int)Math.Round(extra.Entry.Price * 1.2, MidpointRounding.AwayFromZero), extra.Price);
    }

    // Backroom Kettle: a service the player carries into a shop that never stocked one.
    [Fact]
    public void The_kettle_is_on_in_a_shop_that_never_sold_tea()
    {
        var run = Visit("backroom_kettle", CityShop(), gold: 100, "backroom-kettle", "leave");

        Assert.Equal(75, run.GetResource(Gold));
        Assert.Equal(38, run.Health.Current); // 30 + 8
    }

    // Scrivener's Shears: the removal desk is half price, which only works if the relic and the service agree
    // on what "removal" is called.
    [Fact]
    public void The_shears_halve_the_removal_desk()
    {
        var shop = CityShop();
        var removal = shop.Services!.Single(service => service.Id == "remove-card");
        var run = NewRun(1000);
        run.AddRelic(Wearing("scriveners_shears"));

        var shelf = new ShopShelf(run, shop);

        Assert.Equal((int)Math.Round(removal.Price * 0.5, MidpointRounding.AwayFromZero),
            shelf.PriceOf(removal));
    }

    // Debtor's Signet: a broke player walks out with the goods and a number owed.
    [Fact]
    public void The_signet_buys_what_the_purse_cannot()
    {
        var shop = CityShop();
        var card = OnOffer(shop, ShopRelics.CardShelf, "debtors_signet")[0];
        var run = Visit("debtors_signet", shop, gold: 10, card.Entry.Id, "leave");

        Assert.Equal(0, run.GetResource(Gold));
        Assert.Equal(card.Price - 10, run.GetCounter(ShopRelics.Debt));
    }

    // Archive Voucher Roll: vouchers settle part of a price without any Gold being spent.
    [Fact]
    public void Vouchers_pay_at_the_till_and_are_not_gold()
    {
        var shop = CityShop();
        var card = OnOffer(shop, ShopRelics.CardShelf, "archive_voucher_roll")[0];
        var run = NewRun(1000);
        run.SetResource(ShopRelics.ArchiveVoucher, 3);

        Resolve(run, "archive_voucher_roll", shop, card.Entry.Id, "leave");

        // Whole vouchers only, never overpaying: as many 10s as fit under the price.
        var spent = Math.Min(3, card.Price / 10);
        Assert.Equal(3 - spent, run.GetResource(ShopRelics.ArchiveVoucher));
        Assert.Equal(1000 - (card.Price - spent * 10), run.GetResource(Gold));
    }

    // Copper Receipt Roll: the third purchase pays, and the tally survives to start again.
    [Fact]
    public void Every_third_purchase_pays()
    {
        var shop = CityShop();
        var ids = OnOffer(shop, ShopRelics.CardShelf, "copper_receipt_roll").Take(3).ToList();
        var run = Visit("copper_receipt_roll", shop, gold: 1000, [.. ids.Select(s => s.Entry.Id), "leave"]);

        Assert.Equal(1000 - ids.Sum(s => s.Price) + 35, run.GetResource(Gold));
        Assert.Equal(0, run.GetCounter(ShopRelics.Receipts));
    }

    // ── harness ────────────────────────────────────────────────────────────────

    // What that shelf actually puts out for a player wearing that relic. A separate run with the same seed
    // draws the same stock, so the probe does not disturb the visit it is describing.
    private static IReadOnlyList<ShopSlot> OnOffer(ShopDefinition shop, string shelf, string relicId)
    {
        var probe = NewRun(1000);
        probe.AddRelic(Wearing(relicId));
        return new ShopShelf(probe, shop).Slots
            .Where(slot => slot.GroupId == shelf)
            .ToList();
    }

    private static RelicInstance Wearing(string id) =>
        new(ShopRelics.All().Single(relic => relic.Id == id).Compile().ToDefinition());

    // The shop's relic offers grant BY ID, so the run needs the catalog that knows what those ids are.
    private static readonly RunContentRegistry Content = BuildContent();

    private static RunContentRegistry BuildContent()
    {
        var builder = new RunContentRegistryBuilder();
        foreach (var relic in Relics)
            builder.RegisterRelic(relic.Relic.ToDefinition());
        return builder.Build();
    }

    private static RunState NewRun(int gold)
    {
        var run = new RunState(new RunId("run"), new HealthState(30, 40), new RunMap(Array.Empty<Node>()));
        run.SetContent(Content);
        run.SetResource(Gold, gold);
        return run;
    }

    private static RunState Visit(string relicId, ShopDefinition shop, int gold, params string[] choices)
    {
        var run = NewRun(gold);
        Resolve(run, relicId, shop, choices);
        return run;
    }

    private static void Resolve(RunState run, string relicId, ShopDefinition shop, params string[] choices)
    {
        run.AddRelic(Wearing(relicId));
        var builder = new RunDefinitionRegistryBuilder();
        new StandardRunPackage().RegisterDefinitions(builder);
        var provider = new ScriptedChoiceProvider(choices);
        run.SetEntityChooser(provider);
        var context = new NodeResolveContext(run, provider, builder.Build(), new RunEffectProcessor());
        new ShopNodeResolver().Resolve(context, new Node(new NodeId("shop"), StandardRunIds.ShopNode, shop));
    }
}
