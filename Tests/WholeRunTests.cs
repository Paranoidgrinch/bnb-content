using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The whole game, walked: a player who answers every question the run asks — which room, which door, which
// card, which enemy — gets from the first room of the city to the last of the archives without the run
// erroring, looping or parking in a state nobody can answer.
//
// ★ THE WALK IS BOUNDED TO THE FIRST TWO ACTS, and that is a known limitation, not a decision about Act III.
// A run is replayed from its own answers for EVERY answer, so the cost of one more answer grows with the
// length of the run; by the third act a long fight spins for tens of minutes and the walk never returns
// (ACT_III_BUILD_PLAN.md §"Open findings" — the same growing replay latency that stopped `--smoke-marathon`).
// Act III is covered room by room instead (ActThreeEventLiveTests, ActThreeElite*/Boss* tests, ActSeamTests).
// **When the interlude checkpoint lands, walk every act here** — drop `TheActsThatCanBeWalked` and put the
// whole document back in.
//
// This is the coverage net the individual tests cannot be: every other test builds the situation it wants,
// and the bugs that hurt are the ones that only appear after twenty rooms of real state (a fight that will
// not end, a door that asks for a card that is not there, a save that will not resume).
public class WholeRunTests
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260827);

    // How many acts the walk can get through before the replay cost makes it hang. See the note above.
    private const int TheActsThatCanBeWalked = 2;

    // Through the exported document, with a body that survives to the end: the walk is a COVERAGE
    // instrument, and a tester who dies in the city never sees the archives.
    private static RunBlueprint Shipped()
    {
        var options = RunJson.CreateOptions(indented: false);
        var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(Game, options), options);
        return RunWalker.WithHealth(
            shipped with { Acts = [.. shipped.Acts!.Take(TheActsThatCanBeWalked)] }, 9999);
    }

    [Fact]
    public void A_whole_run_walks_every_act_it_can_from_the_first_room_to_the_last()
    {
        // Saving and resuming every fifth interlude, so the walk also proves the run can be put down and
        // picked up again at any point along it.
        var report = RunWalker.Walk(Shipped(), seed: 4711, saveEvery: 5);

        Assert.Null(report.Error);
        Assert.Empty(report.Notes);
        Assert.Equal(RunResult.Victory, report.Result);
        Assert.Equal(TheActsThatCanBeWalked, report.ActsWalked);

        // Each act keeps its promises to the route actually walked (docs/bnb-act-map-specs.md).
        foreach (var (act, minimums) in new[]
                 {
                     (1, Game.Acts![0].MapGeneration!.PerPathMinimums),
                     (2, Game.Acts![1].MapGeneration!.PerPathMinimums),
                 })
        {
            Assert.Equal(1, report.Count(act, MapNodeTags.Boss));
            foreach (var (kind, least) in minimums)
            {
                // A treasure may have bitten (a mimic is a fight), so treasures are counted at or above the
                // promise minus what could have flipped; everything else is exact-or-better.
                var walked = report.Count(act, MapNodeTags.For(kind)) + (kind == MapNodeKind.Treasure
                    ? report.Count(act, MapNodeTags.Mimic) : 0);
                Assert.True(walked >= least,
                    $"act {act} promised at least {least} {kind} room(s) on every route, but the walk met {walked}");
            }
        }
    }

    // The act's length is what it promises, not what it promises plus a whole second act of filler: every
    // per-path minimum becomes a full row every route crosses, so a spec whose free rows were the manifest's
    // stage count made a nineteen-room act twenty-eight rooms long.
    // …and this one asks nothing of a WALK, so it covers every act the document ships.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void An_act_is_as_long_as_it_promises_to_be(int act)
    {
        var spec = Game.Acts![act].MapGeneration!;
        var promised = spec.PerPathMinimums.Values.Sum();
        var generated = RuleBasedMapGenerator.Generate(spec, seed: 20260827, startingLoadout: 0,
            new BalanceCalculator(Game.Balance, Game.Encounters),
            (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));

        // Every row is one room on every route, so the row count IS the length of a walk through the act.
        var rows = generated.Map.Nodes.Select(n => n.Id.Value.Split('c')[0]).Distinct().Count();
        Assert.InRange(rows, promised + 1, promised + spec.Rows + 2);
    }
}
