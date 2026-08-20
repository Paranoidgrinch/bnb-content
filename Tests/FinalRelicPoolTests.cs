using BnbContent.Converter.Relics;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Tests;

// The relic pools as the master counts them, and the rule that keeps them apart: a Boss or Event relic must
// never turn up in a shop or a treasure chest.
public class FinalRelicPoolTests
{
    // BnB_Final_Relics_Master_PostAudit.md §3: 18 Common, 18 Uncommon, 14 Rare.
    [Theory]
    [InlineData(Rarity.Common, 18)]
    [InlineData(Rarity.Uncommon, 18)]
    [InlineData(Rarity.Rare, 14)]
    public void The_normal_pool_is_the_shape_the_master_states(Rarity rarity, int expected) =>
        Assert.Equal(expected, NormalRelics.All().Count(r => r.Rarity == rarity));

    // "50 Normal relics — 38 General and 12 Bureaucrat-specific."
    [Fact]
    public void The_normal_pool_is_fifty_relics_split_as_the_master_states()
    {
        var pool = NormalRelics.All();
        Assert.Equal(50, pool.Count);
        Assert.Equal(12, pool.Count(r => r.Eligibility == Eligibility.Bureaucrat));
        Assert.Equal(38, pool.Count(r => r.Eligibility == Eligibility.General));
    }

    [Fact]
    public void No_two_relics_share_an_id()
    {
        var ids = FinalRelics.All().Select(r => r.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // Every relic says what it does — the engine renders no relic text, so this is what both UIs show.
    [Fact]
    public void Every_relic_says_what_it_does()
    {
        foreach (var relic in FinalRelics.All())
            Assert.False(string.IsNullOrWhiteSpace(relic.Text), $"'{relic.Id}' has no text");
    }

    // A relic's in-combat rule is a status it hands over when a fight opens; that status has to exist, or the
    // relic would quietly do nothing and no fight would complain.
    [Fact]
    public void Every_combat_rule_a_relic_installs_is_registered()
    {
        var statuses = FinalRelics.Statuses().Select(s => s.Id).ToHashSet();

        foreach (var relic in FinalRelics.All().Where(r => r.CombatRule is not null))
            Assert.True(statuses.Contains(relic.CombatRule!.Id),
                $"relic '{relic.Id}' installs unregistered status '{relic.CombatRule.Id}'");
    }

    // The pools never mix: a Bureaucrat pool draw offers General plus Bureaucrat relics and nothing else.
    [Fact]
    public void A_pool_draw_offers_only_that_pool()
    {
        var normal = FinalRelics.Pool(Pool.Normal);
        Assert.NotEmpty(normal);
        Assert.All(normal, r => Assert.Equal(Pool.Normal, r.Pool));
        Assert.DoesNotContain(normal, r => r.Rarity is Rarity.Boss or Rarity.Event or Rarity.Shop);
    }
}
