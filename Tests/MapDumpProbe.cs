using BnbContent.Converter;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using Xunit.Abstractions;

namespace BnbContent.Tests;

// PRINT ONE ACT'S MAP, ON DEMAND (map rework S1). Not an assertion — an eyepiece.
//
// The golden file (MapGoldenTests) pins topology and roles and deliberately holds no encounter or event ids.
// When a seed misbehaves, those ids are exactly what the question is about, so this prints the whole thing:
// summary, grid, and one line per room naming the fight or the door it drew.
//
//   dotnet test Tests/BnbContent.Tests.csproj --filter MapDumpProbe --logger "console;verbosity=detailed"
//   MAP_DUMP_ACT=4 MAP_DUMP_SEED=99 dotnet test … (act is 1-5; seed is any int)
//
// It always passes. A probe that can fail is a test, and a test that prints a 124-room act is noise in every
// run that was not asking about maps.
public class MapDumpProbe(ITestOutputHelper output)
{
    [Fact]
    public void Print()
    {
        var blueprint = BlueprintAssembler.Build(BabData.Load(TestData.Directory), 20260717);
        var act = Number("MAP_DUMP_ACT", 1);
        var seed = Number("MAP_DUMP_SEED", 1);
        var index = Math.Clamp(act, 1, blueprint.Acts!.Count) - 1;
        var spec = blueprint.Acts[index].MapGeneration!;

        var generated = RuleBasedMapGenerator.Generate(
            spec, seed, startingLoadout: 0,
            new BalanceCalculator(blueprint.Balance, blueprint.Encounters),
            (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));

        var diagnostic = MapDiagnostics.Of(generated, seed);
        output.WriteLine($"=== ACT {index + 1} · seed {seed} · generator v0.0.0 (rule-based) ===");
        output.WriteLine(diagnostic.Render());
        output.WriteLine(diagnostic.Detail());
    }

    private static int Number(string variable, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(variable), out var value) ? value : fallback;
}
