using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// What the first alpha's players asked for (bnb-godot PLAYTEST_FEEDBACK_PLAN.md, phase P0), each pinned where
// it lands: the compiled card, the shop's till, the waiting room as the exported document carries it.
public class PlaytestFeedbackTests
{
    private static readonly RunBlueprint Game = FightProbe.Game;

    // P0-1 — "power karten … sollten exhausten, das man das ganze sonst stacken kann".
    [Fact]
    public void Every_rite_exhausts_and_says_so()
    {
        var rites = FinalCards.All().Where(c => c.Type == CardAuthoring.RiteTag).ToList();
        Assert.NotEmpty(rites);
        foreach (var rite in rites)
        {
            var compiled = rite.Compile();
            Assert.Equal(CardZone.ExhaustPile, compiled.PlayedCardDestinationZone);
            Assert.Contains("Exhaust", compiled.DescriptionKey, StringComparison.Ordinal);
            Assert.Contains(compiled.Tags, t => t.value == CardAuthoring.ExhaustTag);
        }
        // …and a card that is not a Rite is untouched.
        var paperCut = FinalCards.All().First(c => c.Id == "paper_cut").Compile();
        Assert.Equal(CardZone.DiscardPile, paperCut.PlayedCardDestinationZone);
    }

    // P0-2 — "shop relics sind zu teuer … im zweifel einfach 20% billiger". Measured on the shelf itself.
    [Fact]
    public void Every_relic_on_a_shelf_costs_a_fifth_less_than_it_did()
    {
        Assert.Equal(104, ShopTemplate.RelicPrice(FinalRelics.Pool(RelicAuthoring.Pool.Normal)
            .First(r => r.Rarity == RelicAuthoring.Rarity.Common)));
        Assert.Equal(152, ShopTemplate.RelicPrice(FinalRelics.Pool(RelicAuthoring.Pool.Normal)
            .First(r => r.Rarity == RelicAuthoring.Rarity.Uncommon)));
        Assert.Equal(208, ShopTemplate.RelicPrice(FinalRelics.Pool(RelicAuthoring.Pool.Normal)
            .First(r => r.Rarity == RelicAuthoring.Rarity.Rare)));
        Assert.Equal(152, ShopTemplate.RelicPrice(FinalRelics.Pool(RelicAuthoring.Pool.Shop).First()));
    }

    // P0-3 — the waiting room with nothing left to improve used to spend the rest and send the run back to the
    // map. Now the choice is shut while no card can be improved, and it says why.
    [Fact]
    public void The_waiting_room_shuts_its_amendment_when_nothing_can_be_improved()
    {
        var rests = Game.Events.Where(e => e.Key.StartsWith("rest:", StringComparison.Ordinal)).ToList();
        Assert.Equal(4, rests.Count); // Acts I–IV; the Divine Ledger has no waiting room
        foreach (var (id, script) in rests)
        {
            var amend = script.Situations.Values.SelectMany(s => s.Choices).Single(c => c.Id == "amend");

            var done = NewRun();
            done.AddDeckCard(new CardDefinitionId("paper_cut")).Upgrade();
            Assert.False(amend.IsAvailable(done), $"'{id}' offers an amendment with nothing to amend");
            Assert.Equal("There's nothing to improve.", amend.ShownDisabledReason(done));

            var open = NewRun();
            open.AddDeckCard(new CardDefinitionId("paper_cut")).Upgrade();
            open.AddDeckCard(new CardDefinitionId("paper_cut"));
            Assert.True(amend.IsAvailable(open), $"'{id}' refuses an amendment with a card to amend");
            Assert.Null(amend.ShownDisabledReason(open));
        }
    }

    private static RunState NewRun() =>
        new(new RunId("run"), new HealthState(30, 40), new RunMap(Array.Empty<Node>()));
}
