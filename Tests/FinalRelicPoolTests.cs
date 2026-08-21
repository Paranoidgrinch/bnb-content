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

    // §4: 24 Shop relics — 18 General and 6 Bureaucrat-specific.
    [Fact]
    public void The_shop_pool_is_the_shape_the_master_states()
    {
        var pool = ShopRelics.All();
        Assert.Equal(24, pool.Count);
        Assert.Equal(6, pool.Count(r => r.Eligibility == Eligibility.Bureaucrat));
        Assert.Equal(18, pool.Count(r => r.Eligibility == Eligibility.General));
        Assert.All(pool, r => Assert.Equal(Pool.Shop, r.Pool));
    }

    // Most of the Shop pool is economy, and economy is not a reaction: a discount is simply true of the shelf
    // while the relic is worn. If these stopped compiling into the relic's shop faces they would silently do
    // nothing, since nothing ever "fires".
    [Fact]
    public void The_economy_relics_compile_into_standing_facts_about_a_shop()
    {
        var compiled = ShopRelics.All().ToDictionary(r => r.Id, r => r.Compile());

        Assert.NotNull(compiled["secondhand_reliquary"].ShopPriceRules);
        Assert.NotNull(compiled["crooked_display_case"].ShopStockGrants);
        Assert.NotNull(compiled["backroom_kettle"].ShopServices);
        Assert.NotNull(compiled["archive_voucher_roll"].ShopCreditSources);
        Assert.NotNull(compiled["debtors_signet"].ShopDebtTerms);
        Assert.NotNull(compiled["bent_auction_gavel"].RewardRules);
    }

    // §5: 25 Event relics, 6 of them in Act I. Only the Act-I six are built so far — the rest are named by
    // events that do not exist yet (Phase D).
    [Fact]
    public void The_act_one_event_relics_are_all_six_of_them()
    {
        var pool = EventRelics.ActI;
        Assert.Equal(6, pool.Count);
        Assert.All(pool, r => Assert.Equal(Pool.Event, r.Pool));
    }

    // An Event relic has exactly ONE source, and saying which is the whole point: it is what Phase D wires the
    // grant to, and a relic with no source could never be won.
    [Fact]
    public void Every_event_relic_names_the_branch_that_grants_it()
    {
        foreach (var relic in FinalRelics.Pool(Pool.Event))
            Assert.False(string.IsNullOrWhiteSpace(relic.Source), $"'{relic.Id}' names no source");
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
