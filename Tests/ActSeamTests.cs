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

    private static int ActOf(string encounterId) =>
        Data.Encounters.First(e => e.Id == encounterId).Act;

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
            Assert.Equal(actNumber, Data.Events.First(e => e.Id == id).Act);
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
