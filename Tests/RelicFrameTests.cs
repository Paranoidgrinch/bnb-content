using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Run;

namespace BnbContent.Tests;

// WHICH POOL A RELIC CAME FROM HAS TO SURVIVE THE EXPORT. The visual canon fixes one frame per pool (§10.4:
// normal slate, shop copper, event pale violet, boss dark purple and antique gold; the elite canon §2 adds
// the plainer boss frame that the elites and the mimic wear), and a shelf of relics is read by frame before
// a single object on it is recognised. The pool lives in the authoring record, which the frontend never
// sees — so the only place it can be read from is `Presentation.Relics[id].Frame`, and these tests hold that
// line: every relic names its pool, the name is one of the six, and no pool loses a member on the way out.
public class RelicFrameTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260717);

    [Fact]
    public void Every_relic_names_the_pool_it_came_from()
    {
        var pools = Enum.GetNames<RelicAuthoring.Pool>().Select(p => p.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(Game.Relics.Count, Game.Presentation.Relics.Count);
        foreach (var relic in Game.Relics)
        {
            var frame = Game.Presentation.Relics[relic.Id].Frame;
            Assert.False(string.IsNullOrWhiteSpace(frame), $"{relic.Id} names no pool");
            Assert.Contains(frame, pools);
        }
    }

    // A count per pool, because the failure this catches is silent: a pool whose relics all export under
    // another pool's name still ships 210 relics and still passes every "is it there" test — the shelf just
    // draws them in the wrong frame.
    [Fact]
    public void The_shipped_frames_are_the_authored_pools()
    {
        var authored = FinalRelics.All()
            .GroupBy(r => r.Pool.ToString().ToLowerInvariant(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var shipped = Game.Presentation.Relics.Values
            .GroupBy(p => p.Frame!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        Assert.Equal(authored.OrderBy(p => p.Key, StringComparer.Ordinal), shipped.OrderBy(p => p.Key, StringComparer.Ordinal));
    }

    // The one pool the canon treats as a family rather than a list: one tooth in four grades, drawn as one
    // object at four sizes. If the mimic ever stops being its own pool the shelf silently files it under the
    // elites and the four grades lose the frame that says they belong together.
    [Fact]
    public void The_mimic_is_four_grades_of_one_pool()
    {
        var mimics = Game.Presentation.Relics.Where(p => p.Value.Frame == "mimic").Select(p => p.Key).ToList();
        Assert.Equal(4, mimics.Count);
    }
}
