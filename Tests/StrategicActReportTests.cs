using BnbContent.Converter;
using RogueDeck.Run;
using Xunit.Abstractions;

namespace BnbContent.Tests;

// WHAT THE FOUR ACTS PRODUCE OVER A THOUSAND SEEDS EACH (map rework S13).
//
// Every other map test in this repo asks about a handful of seeds, which is enough to catch a rule that never
// holds and useless against the ones that hold nine times in ten: a pressure floor the repair only just
// reaches, a band that quietly empties on the wide acts, a fork threshold that costs a whole-act retry once in
// six. Those are tuning facts about the numbers in ActRules, they are invisible on any single map, and this is
// where they are visible.
//
// ONE THING HERE MAY FAIL A BUILD and it is the act's own promise: the seed produced an act with no defect left
// on it. Everything else — how wide, how forked, how much trouble, how much repairing — is written to the test
// output for a human to read, because a quality number that fails a build is a number nobody dares tune.
//
// The deeper look is the converter's own: `dotnet run --project Converter -- --map-report 10000`.
//
// ONE CLASS PER QUARTER OF AN ACT, and that is about wall time, not taste: xUnit runs a class's tests one after
// another, and the four acts as rows of one Theory were a nine-minute strand the rest of the suite had long
// finished waiting on; as one class per act, act four alone was still eight. The thousand seeds are the same, split
// into four runs of 250 — the build-failing promise (no seed leaves a defect) holds per seed, so it is kept whole;
// only the printed spread is per quarter. `dotnet run --project Converter -- --map-report 1000` prints it whole.
public abstract class StrategicActReportTests(int index, int quarter, ITestOutputHelper output)
{
    internal static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260826);

    private const int Seeds = 1000, Quarter = Seeds / 4;

    [Fact]
    public void A_thousand_acts_come_out_clean()
    {
        var report = StrategicActStatistics.Measure(
            Game.Acts![index].StrategicMapGeneration!, firstSeed: 1 + quarter * Quarter, seeds: Quarter, parallelism: 1);
        output.WriteLine($"=== ACT {index + 1}, seeds {1 + quarter * Quarter}..{(quarter + 1) * Quarter} ===");
        output.WriteLine(report.Render());

        Assert.True(report.Sound, report.Render());
    }
}

public class ActOneStrategicReportQ1Tests(ITestOutputHelper output) : StrategicActReportTests(0, 0, output);

public class ActOneStrategicReportQ2Tests(ITestOutputHelper output) : StrategicActReportTests(0, 1, output);

public class ActOneStrategicReportQ3Tests(ITestOutputHelper output) : StrategicActReportTests(0, 2, output);

public class ActOneStrategicReportQ4Tests(ITestOutputHelper output) : StrategicActReportTests(0, 3, output);

public class ActTwoStrategicReportQ1Tests(ITestOutputHelper output) : StrategicActReportTests(1, 0, output);

public class ActTwoStrategicReportQ2Tests(ITestOutputHelper output) : StrategicActReportTests(1, 1, output);

public class ActTwoStrategicReportQ3Tests(ITestOutputHelper output) : StrategicActReportTests(1, 2, output);

public class ActTwoStrategicReportQ4Tests(ITestOutputHelper output) : StrategicActReportTests(1, 3, output);

public class ActThreeStrategicReportQ1Tests(ITestOutputHelper output) : StrategicActReportTests(2, 0, output);

public class ActThreeStrategicReportQ2Tests(ITestOutputHelper output) : StrategicActReportTests(2, 1, output);

public class ActThreeStrategicReportQ3Tests(ITestOutputHelper output) : StrategicActReportTests(2, 2, output);

public class ActThreeStrategicReportQ4Tests(ITestOutputHelper output) : StrategicActReportTests(2, 3, output);

public class ActFourStrategicReportQ1Tests(ITestOutputHelper output) : StrategicActReportTests(3, 0, output);

public class ActFourStrategicReportQ2Tests(ITestOutputHelper output) : StrategicActReportTests(3, 1, output);

public class ActFourStrategicReportQ3Tests(ITestOutputHelper output) : StrategicActReportTests(3, 2, output);

public class ActFourStrategicReportQ4Tests(ITestOutputHelper output) : StrategicActReportTests(3, 3, output)
{
    // …and the gauntlet has no rules to report on, which is a fact worth a test rather than a silence: an act
    // missing from a report reads as a bug in the report.
    [Fact]
    public void The_gauntlet_has_nothing_to_report()
    {
        Assert.Null(Game.Acts![4].StrategicMapGeneration);
    }
}
