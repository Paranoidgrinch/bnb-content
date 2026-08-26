using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The ACT SEAM: the run walks Act I and then Act II, and each act draws only from its own content.
//
// This pins the bug the audit found (ACT_I_II_COMPLETION_PLAN.md, A-2): the map rules used to group EVERY
// encounter that carried a role, so the city's boss row could draw the Grand Cross-Reference — the last boss
// of the next act — and its event nodes could open an archive door. Nothing in a run said which act it was in.
public class ActSeamTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260826);

    private static IEnumerable<string> PoolFor(RunAct act, MapNodeKind role) =>
        act.MapGeneration!.Encounters.For(role).Select(e => e.Encounter.Value);

    // The ids of the events this act AUTHORS (Act I's fifteen), built the way the assembler builds them.
    private static IReadOnlyList<string> AuthoredIds(int act) =>
        Converter.Events.AuthoredEvents
            .For(act, ConversionPools.Build(Data, Data.Relics.Select(RelicMapper.Map).ToList(), act), new Random(1))
            .Select(e => e.Id)
            .ToList();

    private static int ActOf(string encounterId) =>
        Data.Encounters.First(e => e.Id == encounterId).Act;

    private static int CardAct(string cardId) =>
        Converter.Cards.FinalCards.All().First(c => c.Id == cardId).Act;

    // The card ids a role's victory reward can hand out in this act.
    private static IReadOnlyList<string> RewardCards(RunAct act, MapNodeKind role) =>
        CardsIn(((FixedRewardSource)act.MapGeneration!.VictoryRewards[role].Source).Offers);

    private static IReadOnlyList<string> ShopCards(RunAct act) =>
        Game.Shops[act.MapGeneration!.NodeRefs[MapNodeKind.Shop]].Stock
            .SelectMany(shelf => shelf.Offers)
            .SelectMany(entry => entry.Payload)
            .OfType<AddCardToDeckRunEffect>()
            .Select(e => e.Card.value)
            .ToList();

    // Every card an offer (or a nested offer) can add to the deck.
    private static IReadOnlyList<string> CardsIn(IEnumerable<RewardOffer> offers)
    {
        var cards = new List<string>();
        foreach (var effect in offers.SelectMany(o => o.Grant))
            switch (effect)
            {
                case AddCardToDeckRunEffect add:
                    cards.Add(add.Card.value);
                    break;
                case OfferRewardRunEffect offer when offer.Source is PoolRewardSource pool:
                    cards.AddRange(CardsIn(pool.Pool.Entries.Select(e => e.Value)));
                    break;
                case OfferRewardRunEffect offer when offer.Source is FixedRewardSource fixedSource:
                    cards.AddRange(CardsIn(fixedSource.Offers));
                    break;
            }
        return cards;
    }

    [Fact]
    public void The_run_walks_both_acts_in_order()
    {
        Assert.Collection(Game.Acts!,
            first => Assert.Equal("act_1_city", first.Id),
            second => Assert.Equal("act_2_archives", second.Id));
        Assert.All(Game.Acts!, act => Assert.NotNull(act.MapGeneration));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void An_acts_encounter_pools_hold_only_that_acts_encounters(int index, int actNumber)
    {
        var act = Game.Acts![index];
        foreach (var role in new[]
                 { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Boss })
        {
            var pool = PoolFor(act, role).ToList();
            Assert.NotEmpty(pool);
            Assert.All(pool, id => Assert.Equal(actNumber, ActOf(id)));
        }
    }

    // The headline case: an Act-I run must not be able to end against an Act-II boss.
    [Fact]
    public void Each_act_ends_on_its_own_bosses()
    {
        var city = PoolFor(Game.Acts![0], MapNodeKind.Boss).ToList();
        var archives = PoolFor(Game.Acts![1], MapNodeKind.Boss).ToList();

        Assert.Equal(5, city.Count);
        Assert.Equal(5, archives.Count);
        Assert.All(city, id => Assert.StartsWith("city_boss_", id, StringComparison.Ordinal));
        Assert.DoesNotContain("archives_boss_grand_cross_reference", city);
        Assert.All(archives, id => Assert.StartsWith("archives_boss_", id, StringComparison.Ordinal));
        Assert.Empty(city.Intersect(archives));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void An_acts_event_nodes_open_only_that_acts_events(int index, int actNumber)
    {
        var spec = Game.Acts![index].MapGeneration!;
        var events = spec.NodeRefPools[MapNodeKind.Event];

        Assert.NotEmpty(events);
        Assert.All(events, id =>
        {
            // An act's door is either one it still converts or one it authors — and never the other act's.
            if (Data.Events.FirstOrDefault(e => e.Id == id) is { } ported)
                Assert.Equal(actNumber, ported.Act);
            else
                Assert.Contains(id, AuthoredIds(actNumber));
            Assert.True(Game.Events.ContainsKey(id), $"event '{id}' has no script");
        });
        Assert.Contains(spec.NodeRefs[MapNodeKind.Event], events);
    }

    // Shop, waiting room and treasure rooms are furniture, and each act keeps its own.
    [Fact]
    public void Each_act_brings_its_own_shop_rest_and_treasure_rooms()
    {
        var city = Game.Acts![0].MapGeneration!.NodeRefs;
        var archives = Game.Acts![1].MapGeneration!.NodeRefs;

        Assert.NotEqual(city[MapNodeKind.Shop], archives[MapNodeKind.Shop]);
        Assert.NotEqual(city[MapNodeKind.Rest], archives[MapNodeKind.Rest]);
        Assert.True(Game.Shops.ContainsKey(city[MapNodeKind.Shop]));
        Assert.True(Game.Shops.ContainsKey(archives[MapNodeKind.Shop]));
        Assert.True(Game.Events.ContainsKey(city[MapNodeKind.Rest]));
        Assert.True(Game.Events.ContainsKey(archives[MapNodeKind.Rest]));
        Assert.Empty(Game.Acts![0].MapGeneration!.NodeRefPools[MapNodeKind.Treasure]
            .Intersect(Game.Acts![1].MapGeneration!.NodeRefPools[MapNodeKind.Treasure]));
    }

    // ── What an act OFFERS (A-3 … A-6) ─────────────────────────────────────────────

    // The audit's per-path table (docs/bnb-act-map-specs.md): the archives ask for two multi-enemy fights and
    // two elites where the city asks for one of each, and guarantee one treasure instead of two.
    [Fact]
    public void Act_two_asks_more_of_a_route_than_act_one()
    {
        var city = Game.Acts![0].MapGeneration!;
        var archives = Game.Acts![1].MapGeneration!;

        Assert.Equal(8, archives.PerPathMinimums[MapNodeKind.Combat]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.MultiCombat]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.Elite]);
        Assert.Equal(3, archives.PerPathMinimums[MapNodeKind.Event]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.Rest]);
        Assert.Equal(1, archives.PerPathMinimums[MapNodeKind.Treasure]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.Shop]);

        Assert.True(archives.Rows > city.Rows, "the archives are the longer act");
        Assert.NotEqual(
            city.LaneProfiles.Select(l => l.Name).ToList(),
            archives.LaneProfiles.Select(l => l.Name).ToList());
    }

    // A treasure bites at the act's own rate, and what comes out of it is the act's own body.
    [Fact]
    public void Each_acts_treasure_bites_at_its_own_rate_with_its_own_mimic()
    {
        Assert.Equal(5, Game.Acts![0].MapGeneration!.TreasureMimicChancePercent);
        Assert.Equal(10, Game.Acts![1].MapGeneration!.TreasureMimicChancePercent);

        for (var index = 0; index < 2; index++)
        {
            var mimics = PoolFor(Game.Acts![index], MapNodeKind.Mimic).ToList();
            Assert.NotEmpty(mimics);
            Assert.All(mimics, id => Assert.Equal(index + 1, ActOf(id)));
        }
    }

    // A-5: the card a fight pays out is gated to the act it was won in. Act II opens the archives' cards
    // WITHOUT closing the city's — the design gates a card at "Act N or earlier".
    [Fact]
    public void An_acts_fights_pay_out_that_acts_cards()
    {
        var city = RewardCards(Game.Acts![0], MapNodeKind.Combat);
        var archives = RewardCards(Game.Acts![1], MapNodeKind.Combat);

        Assert.NotEmpty(city);
        Assert.ProperSubset(archives.ToHashSet(), city.ToHashSet());
        Assert.All(city, id => Assert.Equal(1, CardAct(id)));
        Assert.Contains(archives, id => CardAct(id) == 2);
    }

    // The shop shelves are stocked from the same act-gated pool, so the archives' shop is not the city's.
    [Fact]
    public void An_acts_shop_stocks_that_acts_cards()
    {
        var city = ShopCards(Game.Acts![0]);
        var archives = ShopCards(Game.Acts![1]);

        Assert.NotEmpty(city);
        Assert.NotEmpty(archives);
        Assert.All(city, id => Assert.Equal(1, CardAct(id)));
        Assert.All(archives, id => Assert.True(CardAct(id) <= 2, $"'{id}' is gated past Act II"));
        Assert.NotEqual(city, archives);
    }

    // A-4: each act's campfire and treasure room are its own rooms, not the city's text under another id.
    [Fact]
    public void Each_act_reads_as_its_own_place()
    {
        string TextOf(RunAct act, MapNodeKind kind)
        {
            var script = Game.Events[act.MapGeneration!.NodeRefs[kind]];
            return script.Situations[script.StartSituationId].TextKey;
        }

        Assert.NotEqual(TextOf(Game.Acts![0], MapNodeKind.Rest), TextOf(Game.Acts![1], MapNodeKind.Rest));
        Assert.NotEqual(TextOf(Game.Acts![0], MapNodeKind.Treasure), TextOf(Game.Acts![1], MapNodeKind.Treasure));
    }

    // A-6: the title is the GAME's; naming an act belongs to the act.
    [Fact]
    public void The_title_names_the_game_and_the_acts_name_themselves()
    {
        Assert.Equal("Bureaucrats & Broomsticks", BlueprintAssembler.GameTitle);
        Assert.DoesNotContain("Act I", BlueprintAssembler.GameTitle, StringComparison.Ordinal);
        Assert.Equal("Bureaucrats & Broomsticks", Game.Presentation.Game!.FlavorText);

        Assert.Equal("Act I: The Old City Offices", Game.Acts![0].NameKey);
        Assert.Equal("Act II: The Endless Archives", Game.Acts![1].NameKey);
    }

    // …and it holds where it counts: in the maps an actual run walks.
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20260826)]
    public void A_generated_run_meets_only_the_current_acts_encounters(int seed)
    {
        var plan = Game.BuildActPlan(seed, startingLoadout: 0);
        Assert.Equal(2, plan.Count);

        for (var index = 0; index < plan.Count; index++)
        {
            var fights = plan[index].Map.Nodes
                .Select(n => n.Payload).OfType<EncounterRef>()
                .Select(f => f.Id.Value).ToList();
            Assert.NotEmpty(fights);
            Assert.All(fights, id => Assert.Equal(index + 1, ActOf(id)));
        }
    }
}
