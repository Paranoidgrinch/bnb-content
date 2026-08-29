using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Tests;

// THE SHELF IS A FIXED SHAPE. BnB_Run_Systems_Master §4.1 does not say "a shop sells some cards and some
// relics" — it says 3 General cards, 4 Character cards, 2 Shop relics, 2 Normal relics, and §2.5/§2.6 say an
// Event or Boss relic may never stand on any of them. That is a promise about a NUMBER and about a POOL, and
// both are invisible from inside the game: a shop that quietly showed five mixed cards and two relics drawn
// from everything non-boss looked exactly like a correct one, which is what it did until now.
//
// So these tests do two different things. The pool tests read the authored shelves — every card and relic the
// shop could ever show — because a wrong pool is a wrong shop even on the draw where it does not surface. The
// count tests DRAW the shelf the way a visit does, because the count is a property of the display, not of the
// pool behind it.
public class ShopShelfTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260829);

    public static TheoryData<int> Acts => [1, 2, 3];

    [Theory]
    [MemberData(nameof(Acts))]
    public void Every_regular_shop_shows_three_general_cards_and_four_character_cards(int act)
    {
        var slots = Drawn(act);

        Assert.Equal(3, slots.Count(slot => slot.GroupId == ShopRelics.GeneralCardShelf));
        Assert.Equal(4, slots.Count(slot => slot.GroupId == ShopRelics.CharacterCardShelf));
    }

    [Theory]
    [MemberData(nameof(Acts))]
    public void Every_regular_shop_shows_two_shop_relics_and_two_normal_relics(int act)
    {
        var slots = Drawn(act);

        Assert.Equal(2, slots.Count(slot => slot.GroupId == ShopRelics.ShopRelicShelf));
        Assert.Equal(2, slots.Count(slot => slot.GroupId == ShopRelics.NormalRelicShelf));
        // And the shelf says which is which, or no price rule could tell them apart.
        Assert.All(slots.Where(slot => slot.GroupId == ShopRelics.NormalRelicShelf),
            slot => Assert.Contains(ShopRelics.NormalRelic, slot.Entry.Tags!));
        Assert.All(slots.Where(slot => slot.GroupId == ShopRelics.ShopRelicShelf),
            slot => Assert.Contains(ShopRelics.ShopRelic, slot.Entry.Tags!));
    }

    // §4.2: each card shelf is generated from its own pool, under the act's gates. A General slot holding a
    // Bureaucrat card would still be a card — and would still be wrong, because the next character inherits
    // the General slots and would inherit a Bureaucrat's card with them.
    [Theory]
    [MemberData(nameof(Acts))]
    public void A_card_shelf_draws_only_from_its_own_pool(int act)
    {
        var general = FinalCards.GeneralPool(act).Select(c => c.Id).ToHashSet();
        var character = FinalCards.CharacterPool(act).Select(c => c.Id).ToHashSet();

        Assert.NotEmpty(Offered(act, ShopRelics.GeneralCardShelf));
        Assert.All(Offered(act, ShopRelics.GeneralCardShelf), id => Assert.Contains(id, general));
        Assert.NotEmpty(Offered(act, ShopRelics.CharacterCardShelf));
        Assert.All(Offered(act, ShopRelics.CharacterCardShelf), id => Assert.Contains(id, character));
    }

    // §4.3: the Shop slots draw only from the Shop-exclusive pool, the Normal slots only from the Normal pool.
    [Theory]
    [MemberData(nameof(Acts))]
    public void A_relic_shelf_draws_only_from_its_own_pool(int act)
    {
        var shop = FinalRelics.Pool(Pool.Shop).Select(r => r.Id).ToHashSet();
        var normal = FinalRelics.Pool(Pool.Normal).Select(r => r.Id).ToHashSet();

        Assert.NotEmpty(Offered(act, ShopRelics.ShopRelicShelf));
        Assert.All(Offered(act, ShopRelics.ShopRelicShelf), id => Assert.Contains(id, shop));
        Assert.NotEmpty(Offered(act, ShopRelics.NormalRelicShelf));
        Assert.All(Offered(act, ShopRelics.NormalRelicShelf), id => Assert.Contains(id, normal));
    }

    // §2.5 and §2.6, stated as the prohibition they are. This is the one that was actually broken: the relic
    // shelf drew from every relic whose rarity was not "boss", so the Event relics — each of them the payoff
    // of one named branch — were on sale.
    [Theory]
    [MemberData(nameof(Acts))]
    public void No_shelf_ever_holds_an_event_or_a_boss_relic(int act)
    {
        var forbidden = FinalRelics.All()
            .Where(relic => relic.Pool is Pool.Event or Pool.Boss)
            .Select(relic => relic.Id)
            .ToHashSet();

        Assert.NotEmpty(forbidden);
        foreach (var shelf in Shop(act).Stock!)
            Assert.All(shelf.Offers, entry => Assert.DoesNotContain(RelicIn(entry), forbidden));
    }

    // Every relic on a shelf is granted BY ID, so the game has to know that id — a shelf that sells a relic the
    // run cannot resolve is a purchase that takes the gold and hands over nothing.
    [Theory]
    [MemberData(nameof(Acts))]
    public void Every_relic_on_sale_is_a_relic_the_game_knows(int act)
    {
        var known = Game.Relics!.Select(relic => relic.Id).ToHashSet();

        foreach (var shelf in Shop(act).Stock!)
            foreach (var id in shelf.Offers.Select(RelicIn).Where(id => id is not null))
                Assert.Contains(id!, known);
    }

    // ── harness ────────────────────────────────────────────────────────────────

    private static ShopDefinition Shop(int act) =>
        Game.Shops[Game.Acts![act - 1].MapGeneration!.NodeRefs[MapNodeKind.Shop]];

    // What one visit actually puts out: the shelves drawn for a run carrying nothing, which is the plain shop
    // before any relic grants a slot.
    private static IReadOnlyList<ShopSlot> Drawn(int act)
    {
        var run = new RunState(new RunId("run"), new HealthState(40, 40), new RunMap([]));
        return new ShopShelf(run, Shop(act)).Slots;
    }

    // Every id one shelf could ever offer — the pool behind the display, not the four things on it today.
    private static IReadOnlyList<string> Offered(int act, string shelfId) =>
        Shop(act).Stock!.Single(shelf => shelf.Id == shelfId).Offers
            .Select(entry => CardIn(entry) ?? RelicIn(entry)!)
            .ToList();

    private static string? CardIn(ShopEntry entry) =>
        entry.Payload.OfType<AddCardToDeckRunEffect>().FirstOrDefault()?.Card.value;

    private static string? RelicIn(ShopEntry entry) =>
        entry.Payload.OfType<AddRelicByIdRunEffect>().FirstOrDefault()?.Relic.Value;
}
