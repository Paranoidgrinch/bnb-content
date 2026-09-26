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

    // P2-4 — the compendium's words reach the document, and every status it explains is a status the game has.
    [Fact]
    public void The_compendium_explains_real_statuses_in_plain_words()
    {
        var extra = Game.Presentation.Game.Extra;
        var statuses = Game.Statuses.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(Compendium.Entries);
        foreach (var entry in Compendium.Entries)
        {
            Assert.Equal(entry.Name, extra[$"compendium:{entry.Id}"]);
            Assert.False(string.IsNullOrWhiteSpace(extra[$"compendium.plain:{entry.Id}"]));
            Assert.False(string.IsNullOrWhiteSpace(extra[$"compendium.example:{entry.Id}"]));
            if (!entry.Concept)
                Assert.True(statuses.Contains(entry.Id), $"the compendium explains '{entry.Id}', which is no status");
        }
    }

    // P5-3 — "later acts can still offer earlier cards, but less and less the further you get". Read off the
    // real card reward's pool weights: the current act takes its share, Act I's share shrinks act by act, and
    // the rarity curve still holds exactly.
    [Fact]
    public void Older_acts_cards_come_less_often_the_further_the_run_goes()
    {
        double? lastActOneShare = null;
        foreach (var act in new[] { 2, 3, 4 })
        {
            var pools = ConversionPools.Build(act);
            var source = Assert.IsType<PoolRewardSource>(pools.CardRewardSource());
            var byId = pools.RewardCards.ToDictionary(c => c.Id);
            double total = source.Pool.Entries.Sum(e => e.Weight);
            double Share(Func<CardAuthoring.BnbCard, bool> which) => source.Pool.Entries
                .Where(e => which(byId[e.Value.Id["card-".Length..]])).Sum(e => e.Weight) / total;

            Assert.Equal(ConversionPools.CurrentActShare[act] / 100.0, Share(c => c.Act == act), 2);
            var actOne = Share(c => c.Act == 1);
            if (lastActOneShare is { } before)
                Assert.True(actOne < before, $"Act I cards take {actOne:P1} in act {act}, not less than {before:P1}");
            lastActOneShare = actOne;

            var (common, uncommon, rare) = ConversionPools.RarityCurve[act];
            Assert.Equal(rare / 100.0, Share(c => c.Rarity == "rare"), 2);
            Assert.Equal(common / 100.0, Share(c => c.Rarity == "common" || c.Rarity is not ("uncommon" or "rare")), 2);
            _ = uncommon;
        }
    }

    // P5-1 — an upgrade improves what the card is about, not only its damage.
    [Fact]
    public void Upgrades_grow_the_thing_the_card_is_about()
    {
        var text = FinalCards.All().ToDictionary(c => c.Id, c => c.RulesText);
        Assert.Equal("Deal 7 damage. Apply 3 Paperwork.", text["cursed_addendum+"]);
        Assert.Equal("Deal 6 damage. Apply 2 Seal.", text["waxing_authority+"]);
        Assert.Equal("Gain 6 Block. Apply 2 Doubt.", text["petty_objection+"]);
        // The "+" of Skeleton Staff used to be the base card word for word.
        var staff = FinalCards.All().Single(c => c.Id == "skeleton_staff");
        var staffPlus = FinalCards.All().Single(c => c.Id == "skeleton_staff+");
        Assert.True(staffPlus.Cost < staff.Cost);
        // No card's "+" is its base card again.
        foreach (var card in FinalCards.All().Where(c => !c.Id.EndsWith('+')))
            if (FinalCards.All().FirstOrDefault(c => c.Id == card.Id + "+") is { } plus)
                Assert.False(plus.RulesText == card.RulesText && plus.Cost == card.Cost,
                    $"{card.Id}+ is {card.Id} again");
    }

    // The tutorial: six city rooms in a fixed order on the real content, through the exported document, walked
    // to its end by a player who answers every question — and every moment the coach names has words.
    [Fact]
    public void The_tutorial_is_six_city_rooms_and_can_be_walked_to_the_end()
    {
        var options = RunJson.CreateOptions(indented: false);
        var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(Game, options), options);
        Assert.NotNull(shipped.Tutorial);
        var tutorial = shipped.ForTutorial();
        Assert.Equal(["combat", "event", "multi-combat", "shop", "rest", "elite"],
            tutorial.Map.Nodes.Select(n => n.Tags.Single()));

        var report = BnbContent.Converter.Playtest.RunWalker.Walk(tutorial, seed: 1);
        Assert.Null(report.Error);
        Assert.Equal(RunResult.Victory, report.Result);
        Assert.True(report.Steps > 6, "the tutorial ended before its rooms were played");

        var extra = Game.Presentation.Game.Extra;
        foreach (var (moment, _, _) in Tutorial.Steps)
            Assert.False(string.IsNullOrWhiteSpace(extra[$"tutorial.text:{moment}"]), moment);
    }
}
