using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The whole game, walked: a player who answers every question the run asks — which room, which door, which
// card, which enemy — gets from the first room of the city to the last of the archives without the run
// erroring, looping or parking in a state nobody can answer.
//
// ★ THE WALK COVERS EVERY ACT THE DOCUMENT HAS. It did not always: a run used to be replayed from its own
// answers for EVERY answer, so the cost of one more answer grew with the length of the run and the third act
// never returned. The baseline now moves at every interlude (InteractiveRunSession's checkpoint), and the
// walker no longer repeats a play that changes nothing — Act III's Make Amends puts a free copy of itself
// back in your hand for as long as anything is owed, which a greedy player will do for ever.
//
// This is the coverage net the individual tests cannot be: every other test builds the situation it wants,
// and the bugs that hurt are the ones that only appear after twenty rooms of real state (a fight that will
// not end, a door that asks for a card that is not there, a save that will not resume).
public class WholeRunTests
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260827);

    // Through the exported document, with a body that survives to the end: the walk is a COVERAGE
    // instrument, and a tester who dies in the city never sees the archives. ⚠ 99 999, not 9 999: once the
    // Rites exhaust (playtest 2026-09-26) this greedy walker grinds the First Scribe for ~270 answers, and the
    // Paperwork his tablet writes — which ignores Block and never decays — adds up past 9 999 on the way.
    private static RunBlueprint Shipped()
    {
        var options = RunJson.CreateOptions(indented: false);
        var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(Game, options), options);
        return RunWalker.WithHealth(shipped, 99999);
    }

    [Fact]
    public void A_whole_run_walks_every_act_from_the_first_room_to_the_last()
    {
        // Saving and resuming every fifth interlude, so the walk also proves the run can be put down and
        // picked up again at any point along it.
        var report = RunWalker.Walk(Shipped(), seed: 4711, saveEvery: 5);

        Assert.Null(report.Error);
        Assert.Empty(report.Notes);
        Assert.Equal(RunResult.Victory, report.Result);
        Assert.Equal(Game.Acts!.Count, report.ActsWalked);

        // Each act keeps its promises to the route actually walked (docs/bnb-act-map-specs.md).
        foreach (var (act, spec) in Game.Acts!
                     .Select((plan, index) => (index + 1, plan.MapGeneration!)))
        {
            var minimums = spec.PerPathMinimums;
            // One boss per act, and three for the gauntlet: Act V's whole length is its bosses.
            Assert.Equal(spec.BossRooms, report.Count(act, MapNodeTags.Boss));
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

    // …AND THE SAME WALK ON THE OTHER GENERATOR (map rework S12). Both ship and the player picks (plan §4b), so
    // "the game can be played end to end" is a claim that has to be made twice: a strategic act lays its rooms
    // out from budgets rather than from guaranteed rows, and a room that generation is happy with and the run
    // cannot realize would only ever show here. The walk goes through the EXPORTED document, so the act's
    // strategic rules have to survive serialization to be walked at all.
    [Fact]
    public void The_whole_run_walks_on_the_strategic_generator_too()
    {
        var report = RunWalker.Walk(Shipped(), seed: 4711, saveEvery: 5, mapGenerator: MapGenerators.Strategic);

        Assert.Null(report.Error);
        Assert.Empty(report.Notes);
        Assert.Equal(RunResult.Victory, report.Result);
        Assert.Equal(Game.Acts!.Count, report.ActsWalked);

        // What the route actually walked was worth, against what the act promised every route would be worth
        // (StrategicPathPressure). This is the per-path table's replacement, measured on a real walk rather
        // than on the plan: the promise is kept by the map, and the map is what the player is standing in.
        for (var act = 1; act <= 4; act++)
        {
            var rules = Game.Acts![act - 1].StrategicMapGeneration!.PathPressure;
            var walked = new[] { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite }
                .Sum(kind => report.Count(act, MapNodeTags.For(kind)) * rules.PressureOf(kind));
            Assert.True(walked >= rules.Minimum,
                $"act {act} promised every route {rules.Minimum} points of trouble, and the walk met {walked}");
        }

        // The gauntlet is drawn by the rule-based generator whichever generator the run asked for, because it
        // authors no strategic rules — three bosses, and its whole length is its bosses.
        Assert.Equal(3, report.Count(5, MapNodeTags.Boss));
    }

    // The act's length is what it promises, not what it promises plus a whole second act of filler: every
    // per-path minimum becomes a full row every route crosses, so a spec whose free rows were the manifest's
    // stage count made a nineteen-room act twenty-eight rooms long.
    // …and this one asks nothing of a WALK: it reads the spec straight off the document.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    // …the gauntlet included, whose promises are none and whose length is its three boss rooms.
    [InlineData(4)]
    public void An_act_is_as_long_as_it_promises_to_be(int act)
    {
        var spec = Game.Acts![act].MapGeneration!;
        var promised = spec.PerPathMinimums.Values.Sum();
        var generated = RuleBasedMapGenerator.Generate(spec, seed: 20260827, startingLoadout: 0,
            new BalanceCalculator(Game.Balance, Game.Encounters),
            (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));

        // Every row is one room on every route, so the row count IS the length of a walk through the act.
        var rows = generated.Map.Nodes.Select(n => n.Id.Value.Split('c')[0]).Distinct().Count();
        Assert.InRange(rows, promised + spec.BossRooms, promised + spec.Rows + spec.BossRooms + 1);
    }
}
