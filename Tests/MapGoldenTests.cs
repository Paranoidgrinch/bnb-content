using System.Text;
using BnbContent.Converter;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// v0.0.0 IS NOW A SHIPPING FEATURE, SO ITS MAPS ARE A CONTRACT (map rework S1).
//
// Until 2026-09-11 the rule-based generator was simply "the" generator, and nothing pinned its output: if a
// refactor shifted a seed's map by one room, every suite stayed green and nobody could have known. The user has
// since decided that it stays in the game as a player-selectable option beside the strategic generator
// ("v0.0.0"), which turns that into a promise: a run started on v0.0.0 regenerates its map on every resume, so
// the same seed must keep producing the same act for as long as the option exists.
//
// WHAT IS PINNED, and what deliberately is not. The golden file holds TOPOLOGY AND ROLES — row widths, the room
// kind in every column, and every edge. It does NOT hold which fight or which door each room drew, because that
// depends on the act's content pools: authoring one more Act I elite would rewrite the whole file and train
// everybody to re-bless it unread. Content has its own guards (EndToEndSmokeTests). So a failure here means one
// of exactly two things — the generator changed, or an act's map RULES changed — and both are worth a look.
//
// To re-bless after a deliberate change:  UPDATE_MAP_GOLDEN=1 dotnet test Tests/BnbContent.Tests.csproj
public class MapGoldenTests
{
    private const string BlueprintSeed = "20260717";
    private static readonly int[] Seeds = [1, 7, 20260820];

    private static readonly RunBlueprint Blueprint =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), int.Parse(BlueprintSeed));

    private static string GoldenDirectory =>
        Path.Combine(Directory.GetParent(TestData.Directory)!.FullName, "Tests", "Golden");

    private static string GoldenPath => Path.Combine(GoldenDirectory, "map-v0.0.0.txt");

    [Fact]
    public void The_rule_based_generator_still_builds_the_maps_it_built_on_2026_09_11()
    {
        var actual = Render();

        if (Environment.GetEnvironmentVariable("UPDATE_MAP_GOLDEN") == "1")
        {
            Directory.CreateDirectory(GoldenDirectory);
            File.WriteAllText(GoldenPath, actual);
            return;
        }

        Assert.True(File.Exists(GoldenPath),
            $"the golden map file is missing — write it with UPDATE_MAP_GOLDEN=1 ({GoldenPath})");

        var expected = File.ReadAllText(GoldenPath);
        if (expected == actual)
            return;

        // A diff is worth more than an assertion message: the actual output is written beside the golden so the
        // change can be read as a change rather than guessed at from one line of xunit output.
        var sideBySide = Path.Combine(GoldenDirectory, "map-v0.0.0.actual.txt");
        File.WriteAllText(sideBySide, actual);
        Assert.Fail(
            $"the rule-based generator's maps have changed: {FirstDifference(expected, actual)}\n"
            + $"  golden: {GoldenPath}\n  actual: {sideBySide}\n"
            + "  diff them. If the change is deliberate, re-bless with UPDATE_MAP_GOLDEN=1.");
    }

    // Every act at every seed, as topology and roles.
    private static string Render()
    {
        var text = new StringBuilder();
        text.AppendLine("# Bureaucrats & Broomsticks — map generator v0.0.0 (RuleBasedMapGenerator), golden output");
        text.AppendLine("#");
        text.AppendLine("# Topology and roles only — no encounter or event ids (see MapGoldenTests for why).");
        text.AppendLine("# C combat · M multi-combat · E elite · B boss · $ shop · R rest · ? event · T treasure");
        text.AppendLine($"# blueprint seed {BlueprintSeed}");

        for (var act = 0; act < Blueprint.Acts!.Count; act++)
        {
            var spec = Blueprint.Acts[act].MapGeneration!;
            text.AppendLine();
            text.AppendLine($"=== ACT {act + 1} ===");
            text.AppendLine($"spec: rows {spec.Rows} · boss rooms {spec.BossRooms} · width {spec.MinWidth}-{spec.MaxWidth}"
                + $" · wide guarantee rows {spec.WideGuaranteeRows} · lanes {spec.LaneProfiles.Count}");

            foreach (var seed in Seeds)
            {
                var generated = RuleBasedMapGenerator.Generate(
                    spec, seed, startingLoadout: 0,
                    new BalanceCalculator(Blueprint.Balance, Blueprint.Encounters),
                    (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));

                text.AppendLine();
                text.AppendLine($"--- seed {seed} ---");
                text.Append(MapDiagnostics.Of(generated, seed).Render());
            }
        }
        return text.ToString().ReplaceLineEndings("\n");
    }

    private static string FirstDifference(string expected, string actual)
    {
        var left = expected.ReplaceLineEndings("\n").Split('\n');
        var right = actual.ReplaceLineEndings("\n").Split('\n');
        for (var line = 0; line < Math.Max(left.Length, right.Length); line++)
        {
            var was = line < left.Length ? left[line] : "(end of file)";
            var now = line < right.Length ? right[line] : "(end of file)";
            if (was != now)
                return $"line {line + 1} was \"{was}\" and is now \"{now}\"";
        }
        return "the files differ only in trailing whitespace";
    }
}
