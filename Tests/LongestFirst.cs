using Xunit.Abstractions;

[assembly: TestCollectionOrderer("BnbContent.Tests.LongestFirst", "BnbContent.Tests")]

namespace BnbContent.Tests;

// THE LONG STRANDS START FIRST. xUnit runs the classes side by side but starts them in the order it is given, and
// by default that put the whole-run walk and the thousand-act report at minute three to five — so the suite's wall
// time was "everything else, THEN the longest test". Started first, the longest test runs while everything else
// fills the remaining threads, and the wall time comes down to the larger of the longest test and the total work
// over the cores. (Measured 2026-10-04, ~/Desktop/bnb-diagnostics/testtime-*.)
//
// ⚠ DO NOT PARALLELIZE INSIDE A TEST to shorten it: a Parallel.ForEach in a test competes with xUnit's own threads,
// and the suite got no faster while the test it was meant to help took twice as long. A long test that can be
// split is split into CLASSES (see StrategicActReportTests); one that cannot is named here.
//
// The list is by measured length, longest first; a class not on it keeps xUnit's order after these.
public sealed class LongestFirst : ITestCollectionOrderer
{
    private static readonly string[] Longest =
    [
        nameof(WholeRunStrategicTests),
        nameof(WholeRunTests),
        nameof(ActFourStrategicReportQ1Tests), nameof(ActFourStrategicReportQ2Tests),
        nameof(ActFourStrategicReportQ3Tests), nameof(ActFourStrategicReportQ4Tests),
        nameof(ActThreeStrategicReportQ1Tests), nameof(ActThreeStrategicReportQ2Tests),
        nameof(ActThreeStrategicReportQ3Tests), nameof(ActThreeStrategicReportQ4Tests),
        nameof(WitchPotEverywhereTests),
        nameof(ActTwoStrategicReportQ1Tests), nameof(ActTwoStrategicReportQ2Tests),
        nameof(ActTwoStrategicReportQ3Tests), nameof(ActTwoStrategicReportQ4Tests),
        nameof(ActOneStrategicReportQ1Tests), nameof(ActOneStrategicReportQ2Tests),
        nameof(ActOneStrategicReportQ3Tests), nameof(ActOneStrategicReportQ4Tests),
        nameof(PlaytestFeedback2Tests),
        nameof(EliteRelicTests),
        nameof(BossLengthTests),
    ];

    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        testCollections.OrderBy(Rank);

    private static int Rank(ITestCollection collection)
    {
        // A class's own collection is named "Test collection for <full class name>".
        var name = collection.DisplayName[(collection.DisplayName.LastIndexOf('.') + 1)..];
        var rank = Array.IndexOf(Longest, name);
        return rank < 0 ? Longest.Length : rank;
    }
}
