using BnbContent.Converter;
using RogueDeck.Run;
using Xunit.Abstractions;

namespace BnbContent.Tests;

// WHAT THE ACTS ASK OF v0.0.1 (map rework S12).
//
// The rule-based generator is told what every route must hold and lays full rows to guarantee it; the strategic
// one is told what the ACT holds, where in its depth, and how much trouble one walk through it must be worth —
// and then the routes are allowed to differ, which is the whole point (plan §1.2: four of seven room types were
// identical on every single route).
//
// So these tests are written against INTENT rather than against per-path counts, because per-path counts are the
// thing being retired: an act is as long as it says, it holds what it budgets, it holds it where it says, no
// walk through it is a stroll, every fork is a question, and every room it lays down holds real content. What
// v0.0.0 does with the same acts is unchanged and pinned elsewhere (Tests/Golden/map-v0.0.0.txt).
public class StrategicMapTests(ITestOutputHelper output)
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260826);

    // The four acts that HAVE a map, with the act number the manifest knows them by.
    public static TheoryData<int> Roomed => [0, 1, 2, 3];

    private static StrategicActSpec Spec(int index) => Game.Acts![index].StrategicMapGeneration!;

    private static readonly int[] Seeds = [1, 7, 99, 4711, 20260914];

    [Fact]
    public void Every_act_but_the_gauntlet_is_authored_for_both_generators()
    {
        Assert.All(Game.Acts!, act => Assert.NotNull(act.MapGeneration));
        for (var index = 0; index < 4; index++)
            Assert.NotNull(Game.Acts![index].StrategicMapGeneration);

        // The Divine Ledger is three bosses and not one chosen room, so there is nothing to allocate and no
        // spec to author. A run on v0.0.1 therefore walks Act V on v0.0.0 — and the two agree about it exactly,
        // because the act's shape is its boss count and nothing else.
        Assert.Null(Game.Acts![4].StrategicMapGeneration);

        // The blueprint carries the first act's rules for both generators, the way it always has for one: a
        // document offering a generator choice it cannot honour is a document that fails on the title screen.
        Assert.Equal(Spec(0), Game.StrategicMapGeneration);
    }

    // Everything that can be said about an act before a seed is spent on it: the budgets fit the act, the depth
    // bands are reachable, the pressure floor is attainable and the fork threshold is not above what the act's
    // own roles could ever draw (StrategicActSpecValidator).
    [Theory]
    [MemberData(nameof(Roomed))]
    public void An_acts_own_rules_do_not_contradict_themselves(int index)
    {
        var report = StrategicActSpecValidator.Validate(Spec(index));
        output.WriteLine($"act {index + 1}: {report.Render()}");
        Assert.True(report.Clean, report.Render());
    }

    // §3 of the plan, as a test: the rework changes an act's SHAPE and not its size. The manifest's
    // `steps_before_boss` states the act's real length again (it described nothing at all while the per-path
    // table was the design), the spec is authored to it, and a route walks exactly that many rooms.
    [Theory]
    [MemberData(nameof(Roomed))]
    public void An_act_is_as_long_as_it_says_it_is(int index)
    {
        var manifest = Data.Acts.First(a => a.Id == Game.Acts![index].Id);
        Assert.Equal(manifest.Map.StepsBeforeBoss, Spec(index).Rows);

        foreach (var seed in Seeds)
        {
            var act = StrategicMapGenerator.Generate(Spec(index), seed);
            Assert.Equal(Spec(index).Rows, act.Plan.Topology.Rows.Count);
            // One room per row on every route, so the act's length is the length of a walk through it.
            Assert.Equal(Spec(index).Rows, act.Pressure.Thinnest.Rooms.Count);
            Assert.Equal(Spec(index).Rows, act.Pressure.Richest.Rooms.Count);
        }
    }

    // What the act HOLDS, which is the sentence that replaces the per-path table: the budgets are kept, and the
    // depth gates the acts have always authored still hold — an elite may not stand in the city's first third
    // whichever generator laid the city out.
    [Theory]
    [MemberData(nameof(Roomed))]
    public void An_act_holds_what_it_budgets_and_holds_it_where_it_says(int index)
    {
        var spec = Spec(index);
        foreach (var seed in Seeds)
        {
            var act = StrategicMapGenerator.Generate(spec, seed);
            Assert.True(act.Clean, $"act {index + 1} seed {seed} is not clean:\n{act.Render()}");

            foreach (var (kind, budget) in spec.Rooms.RoomBudgets)
            {
                var held = act.Plan.Count(kind);
                Assert.True(held >= budget.Min && held <= budget.Max,
                    $"act {index + 1} seed {seed} holds {held} {kind} room(s), outside its {budget.Min}..{budget.Max}");
            }

            foreach (var slot in act.Plan.Topology.Slots)
            {
                if (!act.Plan.TryKindOf(slot.Id, out var kind))
                    continue;
                var earliest = spec.Rooms.EarliestDepthOf(kind);
                var depth = MapDepth.Percent(slot.Row, spec.Rows);
                Assert.True(depth >= earliest,
                    $"act {index + 1} seed {seed}: a {kind} stands at {depth} %, before its gate at {earliest} %");
            }

            for (var band = 0; band < spec.Rooms.DepthBands.Count; band++)
                foreach (var (kind, budget) in spec.Rooms.DepthBands[band].Budgets)
                {
                    var held = act.Plan.CountInBand(band, kind);
                    Assert.True(held <= budget.Max,
                        $"act {index + 1} seed {seed}: the {spec.Rooms.DepthBands[band].Label} band holds "
                        + $"{held} {kind} room(s), more than the {budget.Max} it allows");
                }
        }
    }

    // …AND WHAT IT ASKS. The floor is a promise about every complete route and it is the thing the per-path
    // minimums were really for: a map where the elites sit on one side and the campfires on the other keeps
    // every act-wide budget ever authored and can still be strolled through.
    [Theory]
    [MemberData(nameof(Roomed))]
    public void No_walk_through_an_act_is_a_stroll_and_every_fork_is_a_question(int index)
    {
        var spec = Spec(index);
        foreach (var seed in Seeds)
        {
            var act = StrategicMapGenerator.Generate(spec, seed);
            Assert.True(act.Pressure.Held,
                $"act {index + 1} seed {seed}: the thinnest route is worth {act.Pressure.Thinnest.Pressure}, "
                + $"under the {spec.PathPressure.Minimum} the act promises — {act.Pressure.Thinnest.Letters}");

            // A hollow fork is one whose two ways lead into the same expected future. v0.0.0's are hollow
            // roughly three times in four (plan S9); an act that meets its contrast floor has none at all.
            Assert.All(act.Forks.Forks, fork => Assert.True(fork.Contrast >= spec.ForkQuality.MinimumContrast,
                $"act {index + 1} seed {seed}: a fork at {fork.Room.Value} is worth {fork.Contrast}, "
                + $"under the {spec.ForkQuality.MinimumContrast} the act promises"));
        }
    }

    // THE CLAIM THE WHOLE REWORK RESTS ON, said the way §1.2 said its opposite. Measured over ALL routes
    // through an act at once (the same O(V+E) reverse-topological DP the pressure floor uses, run once per role
    // with that role alone weighted), v0.0.0's Act I reads:
    //
    //     Elite 1..1   MultiCombat 1..1   Rest 2..2   Shop 2..2   Treasure 2..3   Event 3..5   Combat 9..12
    //
    // Four of seven room types identical on every single route — routes that are the same list of rooms in a
    // slightly different order. The strategic acts have to invert that: most room types differ by route, and a
    // route can lack one entirely.
    [Theory]
    [MemberData(nameof(Roomed))]
    public void A_route_can_hold_what_another_route_never_meets(int index)
    {
        MapNodeKind[] kinds =
        [
            MapNodeKind.Elite, MapNodeKind.MultiCombat, MapNodeKind.Rest, MapNodeKind.Shop,
            MapNodeKind.Treasure, MapNodeKind.Event, MapNodeKind.Combat,
        ];

        var identical = new List<int>();
        var absent = new List<int>();
        foreach (var seed in Seeds)
        {
            var act = StrategicMapGenerator.Generate(Spec(index), seed);
            var ranges = kinds.ToDictionary(kind => kind, kind => Range(act, kind));
            output.WriteLine($"act {index + 1} seed {seed}: "
                + string.Join("   ", kinds.Select(k => $"{k} {ranges[k].Least}..{ranges[k].Most}")));
            identical.Add(kinds.Count(k => ranges[k].Least == ranges[k].Most));
            absent.Add(kinds.Count(k => ranges[k].Least == 0 && ranges[k].Most > 0));
        }

        // At most one of the seven reads the same on every route — against v0.0.0's four. Measured over these
        // seeds it is none at all, and the margin is deliberate: this is a claim about the generator, and a
        // threshold at the measurement is a test that fails on the next tuning pass rather than on a defect.
        Assert.True(identical.Max() <= 1,
            $"act {index + 1} has {identical.Max()} room type(s) identical on every route, which is v0.0.0's defect");

        // …and the act CAN leave a room type off a route entirely (plan §7). Not every seed does — an act with
        // a campfire in every quarter and four routes will often put one on each — so this is asserted of the
        // act rather than of each of its seeds, which is exactly the claim: v0.0.0 could not do it at all.
        Assert.True(absent.Max() >= 1,
            $"act {index + 1} never leaves a room type off a route, on any of {Seeds.Length} seeds");
    }

    // How many rooms of one kind the thinnest and the richest route hold — the whole act measured at once, by
    // weighting that one role and reading the extremes the pressure DP reports.
    private static (int Least, int Most) Range(GeneratedAct act, MapNodeKind kind)
    {
        var report = StrategicPathPressure.Measure(act.Plan, new PathPressureRules
        {
            KindPressure = new Dictionary<MapNodeKind, int> { [kind] = 1 },
        });
        return (report.Thinnest.Pressure, report.Richest.Pressure);
    }

    // AND IT REACHES A RUN. The act plan is what RunSetup hands the player, so this is the seam where a spec
    // that generates beautifully and realizes nothing would show: every room the strategic generator lays down
    // has to hold a fight, a door, a counter or a chair that this act actually owns.
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20260914)]
    public void A_run_laid_out_by_v0_0_1_holds_nothing_but_real_content(int seed)
    {
        var plan = Game.BuildActPlan(seed, startingLoadout: 0, MapGenerators.Strategic);
        Assert.Equal(5, plan.Count);

        for (var index = 0; index < plan.Count; index++)
        {
            var nodes = plan[index].Map.Nodes;
            Assert.NotEmpty(nodes);
            foreach (var node in nodes)
                switch (node.Payload)
                {
                    case EncounterRef fight:
                        Assert.Equal(index + 1, Data.Encounters.First(e => e.Id == fight.Id.Value).Act);
                        break;
                    case EventRef door:
                        Assert.True(Game.Events.ContainsKey(door.Id.Value), $"event '{door.Id.Value}' has no script");
                        break;
                    case ShopRef counter:
                        Assert.True(Game.Shops.ContainsKey(counter.Id.Value), $"shop '{counter.Id.Value}' has no stock");
                        break;
                    default:
                        Assert.Fail($"act {index + 1} room {node.Id.Value} holds {node.Payload?.GetType().Name ?? "nothing"}");
                        break;
                }
        }
    }

    // The gauntlet is walked by v0.0.0 whichever generator the run asked for, so the two must produce the same
    // Act V — not "a similar one". Three gods, in one order, drawn from the same seed.
    [Theory]
    [InlineData(1)]
    [InlineData(20260914)]
    public void Both_generators_walk_the_same_gauntlet(int seed)
    {
        string Gods(string generator) => string.Join(" ", Game
            .BuildActPlan(seed, startingLoadout: 0, generator)[4].Map.Nodes
            .Select(n => n.Payload).OfType<EncounterRef>().Select(f => f.Id.Value));

        Assert.Equal(Gods(MapGenerators.RuleBased), Gods(MapGenerators.Strategic));
    }
}
