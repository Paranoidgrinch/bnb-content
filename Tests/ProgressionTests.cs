using BnbContent.Converter;
using BnbContent.Converter.Lieutenant;
using BnbContent.Converter.Relics;

namespace BnbContent.Tests;

// The Lieutenant's card progression (META_PROGRESSION_PLAN.md §1): the start set and the twelve boosters hold
// every card of her reward pool and every relic of hers, each exactly once, and a start of 30 cards and 10 relics.
public class ProgressionTests
{
    private static readonly Progression.Definition Lt = Progression.Lieutenant;

    [Fact]
    public void The_start_and_the_boosters_hold_every_card_of_her_pool_exactly_once()
    {
        var pool = LieutenantCards.Commons().Concat(LieutenantCards.Uncommons()).Concat(LieutenantCards.Rares())
            .Select(c => c.Id).Where(id => !id.EndsWith('+')).Order().ToList();
        var listed = Lt.StartCards.Concat(Lt.Boosters.SelectMany(b => b.Cards)).ToList();

        Assert.Equal(listed.Count, listed.Distinct().Count());
        Assert.Equal(pool, listed.Order().ToList());
        Assert.Equal(30, Lt.StartCards.Count);
    }

    [Fact]
    public void The_start_and_the_boosters_hold_every_relic_of_hers_exactly_once()
    {
        var hers = LieutenantRelics.All().Select(r => r.Id).Order().ToList();
        var listed = Lt.StartRelics.Concat(Lt.Boosters.SelectMany(b => b.Relics)).ToList();

        Assert.Equal(listed.Count, listed.Distinct().Count());
        Assert.Equal(hers, listed.Order().ToList());
        Assert.Equal(10, Lt.StartRelics.Count);
    }

    [Fact]
    public void Twelve_boosters_one_per_level_from_two_to_thirteen()
    {
        Assert.Equal(Enumerable.Range(2, 12), Lt.Boosters.Select(b => b.Level));
        Assert.Equal(12, Lt.LevelCosts.Count);
        Assert.Equal(30, Lt.LevelCosts[0]);
    }

    [Fact]
    public void The_modifier_relics_are_never_offered_anywhere()
    {
        foreach (var pool in Enum.GetValues<RelicAuthoring.Pool>().Where(p => p != RelicAuthoring.Pool.Event))
            Assert.DoesNotContain(FinalRelics.Pool(pool, RelicAuthoring.Eligibility.Lieutenant), r => r.Id.StartsWith(ModifierRelics.Prefix, StringComparison.Ordinal));
    }
}
