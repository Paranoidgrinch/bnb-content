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
public class StrategicActReportTests(ITestOutputHelper output)
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260826);

    public static TheoryData<int> Roomed => [0, 1, 2, 3];

    [Theory]
    [MemberData(nameof(Roomed))]
    public void A_thousand_acts_come_out_clean(int index)
    {
        var report = StrategicActStatistics.Measure(
            Game.Acts![index].StrategicMapGeneration!, firstSeed: 1, seeds: 1000);
        output.WriteLine($"=== ACT {index + 1} ===");
        output.WriteLine(report.Render());

        Assert.True(report.Sound, report.Render());
    }

    // …and the gauntlet has no rules to report on, which is a fact worth a test rather than a silence: an act
    // missing from a report reads as a bug in the report.
    [Fact]
    public void The_gauntlet_has_nothing_to_report()
    {
        Assert.Null(Game.Acts![4].StrategicMapGeneration);
    }
}
