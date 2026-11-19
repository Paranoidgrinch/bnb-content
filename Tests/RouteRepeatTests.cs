using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// NO FIGHT TWICE ON ONE ROUTE (playtest 2026-11-19: "pro route auf einer map immernoch 2 mal der gleiche elite").
// Every act's map for many seeds, drawn as the game draws it, and every room checked against every room that leads
// to it: no route through an act may meet the same elite or boss twice. A normal fight may come back on a long route
// once every normal fight of the act stands on it — but never within MinimumNormalGap rows of itself.
public class RouteRepeatTests
{
    private const int Seeds = 300;
    private const int MinimumNormalGap = 2; // never straight after itself

    [Fact]
    public void No_route_meets_the_same_encounter_twice()
    {
        var blueprint = BlueprintAssembler.Build(BabData.Load(TestData.Directory), 20260717);
        var balance = new BalanceCalculator(blueprint.Balance, blueprint.Encounters);
        var repeats = new List<string>();
        var normals = new List<(int Gap, string Line)>();
        static int Row(NodeId id) => int.Parse(id.Value[1..id.Value.IndexOf('c')], System.Globalization.CultureInfo.InvariantCulture);
        for (var a = 0; a < blueprint.Acts!.Count; a++)
        {
            var spec = blueprint.Acts[a].MapGeneration!;
            if (blueprint.Acts[a].StrategicMapGeneration is not { } strategic)
                continue;
            for (var seed = 1; seed <= Seeds; seed++)
            {
                var act = StrategicMapGenerator.Generate(strategic, seed);
                var map = StrategicMapRealizer.Realize(act, spec, startingLoadout: 0, balance,
                    (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef)).Map;
                var fight = map.Nodes.Where(n => n.Payload is EncounterRef)
                    .ToDictionary(n => n.Id, n => ((EncounterRef)n.Payload).Id.Value);
                var parents = map.Edges.GroupBy(e => e.To).ToDictionary(g => g.Key, g => g.Select(e => e.From).ToList());
                var above = new Dictionary<NodeId, HashSet<NodeId>>();
                HashSet<NodeId> Above(NodeId id)
                {
                    if (above.TryGetValue(id, out var known))
                        return known;
                    var set = new HashSet<NodeId>();
                    foreach (var parent in parents.GetValueOrDefault(id) ?? [])
                    {
                        set.Add(parent);
                        set.UnionWith(Above(parent));
                    }
                    return above[id] = set;
                }
                var tags = map.Nodes.ToDictionary(n => n.Id, n => n.Tags);
                foreach (var (id, encounter) in fight)
                    foreach (var earlier in Above(id))
                        if (fight.TryGetValue(earlier, out var met) && met == encounter)
                        {
                            var line = $"act {a + 1} seed {seed}: {encounter} at {earlier.Value} and again at {id.Value}";
                            if (tags[id].Contains(MapNodeTags.Elite) || tags[id].Contains(MapNodeTags.Boss))
                                repeats.Add(line);
                            else
                                normals.Add((Row(id) - Row(earlier), line));
                        }
            }
        }
        Assert.True(repeats.Count == 0,
            $"{repeats.Count} elite/boss repeat(s) on a route, e.g.:\n{string.Join("\n", repeats.Take(15))}");

        // A normal fight may repeat on a long route once the act's whole pool is on it — never close together.
        var close = normals.Where(n => n.Gap < MinimumNormalGap).Select(n => $"{n.Line} ({n.Gap} rows apart)").ToList();
        Assert.True(close.Count == 0,
            $"{close.Count} normal fight(s) repeat within {MinimumNormalGap} rows of a route ({normals.Count} repeats in all), e.g.:\n"
            + string.Join("\n", close.Take(15)));
    }
}
