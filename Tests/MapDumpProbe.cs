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
//   MAP_DUMP_GENERATOR=v0.0.1 dotnet test …      (the strategic generator; the default is v0.0.0)
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
        var strategic = blueprint.Acts[index].StrategicMapGeneration;
        var generator = Environment.GetEnvironmentVariable("MAP_DUMP_GENERATOR") is { Length: > 0 } named
            ? named : MapGenerators.RuleBased;
        var balance = new BalanceCalculator(blueprint.Balance, blueprint.Encounters);

        // The act as the asked-for generator lays it out — and the gauntlet as the ONLY generator that has
        // anything to say about it, since Act V carries no strategic spec (MapSpecBuilder.Strategic).
        var strategicAct = MapGenerators.IsStrategic(generator) && strategic is not null
            ? StrategicMapGenerator.Generate(strategic, seed) : null;
        var generated = strategicAct is null
            ? RuleBasedMapGenerator.Generate(
                spec, seed, startingLoadout: 0, balance,
                (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef))
            : StrategicMapRealizer.Realize(
                strategicAct, spec, startingLoadout: 0, balance,
                (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));

        var diagnostic = MapDiagnostics.Of(generated, seed);
        output.WriteLine($"=== ACT {index + 1} · seed {seed} · generator "
            + $"{(strategicAct is null ? "v0.0.0 (rule-based)" : "v0.0.1 (strategic)")} ===");
        output.WriteLine(diagnostic.Render());

        // How the strategic generator got there: what it budgeted, what it had to repair, and what it still
        // owes. Nothing the rule-based one can answer, so it is printed only where it means something.
        if (strategicAct is not null)
            output.WriteLine(strategicAct.Render());

        // What one walk through this act asks of a player (map rework S8), against THIS act's own table —
        // authored in ActRules since S12, and the plan's worked defaults for an act that authors none.
        var pressure = strategic?.PathPressure ?? new PathPressureRules();
        output.WriteLine(StrategicPathPressure.Measure(generated, pressure).Render());

        // And whether the act's forks are choices (map rework S9). A hollow fork is one whose two ways lead into
        // the same expected future — the thing §1 was really counting when it found twelve forks in an Act I map.
        output.WriteLine(ForkQualityEvaluator.Measure(generated, strategic?.ForkQuality ?? new ForkQualityRules())
            .Render());

        output.WriteLine(diagnostic.Detail());
    }

    private static int Number(string variable, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(variable), out var value) ? value : fallback;
}
