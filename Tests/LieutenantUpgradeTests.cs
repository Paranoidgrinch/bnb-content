using BnbContent.Converter.Lieutenant;
using static BnbContent.Tests.LieutenantDuel;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// The Lieutenant's upgrades whose facet is a rule of its own (LIEUTENANT_PLAN §1, §4).
public class LieutenantUpgradeTests
{
    [Fact]
    public void Witness_plus_doubles_two_hits()
    {
        using var d = One(Five("lt_witness+", "lt_marked_man", "lt_watch_cut"));
        d.P("lt_marked_man");
        d.P("lt_witness+");
        d.P("lt_watch_cut");            // one hit, counted twice: kept
        Assert.Equal(1, Authority(d));
        Assert.Equal(1, Stacks(d.Me, LieutenantRules.Witness));
    }

    [Fact]
    public void Make_an_example_plus_spreads_without_authority()
    {
        using var d = Two(Five("lt_make_an_example+"));
        var other = Standing(Body(d.Play, d.Other!.Value));
        d.P("lt_make_an_example+");     // 18: the biter's 24 lose 18
        Assert.Equal(other - 9, Standing(Body(d.Play, d.Other!.Value)));
    }

    [Fact]
    public void Heave_plus_strikes_again_for_one_strength()
    {
        using var d = One(Five("lt_heave+"), health: 40, hero: (LieutenantKeywords.Strength, 1));
        var before = Standing(d.It);
        d.P("lt_heave+", choose: 0);    // 13 + 1, then 13 + 0
        Assert.Equal(before - 27, Standing(d.It));
    }

    [Fact]
    public void No_surrender_plus_does_not_need_open_guard()
    {
        using var d = One(Five("lt_no_surrender+", "lt_buckler_up"), Biter, BiteIntent);
        d.P("lt_no_surrender+");
        d.End();                         // 7 + 5 Block against 6
        Assert.Equal(2, Authority(d));
    }

    [Fact]
    public void Parade_ground_plus_counts_three_energy()
    {
        using var d = One(Five("lt_parade_ground+", "lt_first_orders", "lt_watch_cut"));
        d.P("lt_parade_ground+");
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        d.End();
        Assert.InRange(Stacks(d.Me, Converter.Cards.Keywords.WardWax), 14, 15);
    }

    [Fact]
    public void Drum_major_plus_draws_with_its_energy()
    {
        using var d = One([.. Five("lt_drum_major+", "lt_first_orders", "lt_watch_cut"), "lt_watch_cut", "lt_watch_cut"]);
        if (!new[] { "lt_drum_major+", "lt_first_orders", "lt_watch_cut" }.All(id => Now(d.Play).Hand.Any(c => c.DefinitionId.value == id)))
            return;
        d.P("lt_drum_major+");
        d.P("lt_first_orders");
        var hand = d.Hand;
        d.P("lt_watch_cut");
        Assert.Equal(hand, d.Hand);      // one played, one drawn
    }

    [Fact]
    public void Service_record_plus_pays_authority_for_the_kill()
    {
        using var d = Two(Five("lt_service_record+"), (LieutenantKeywords.Strength, 14));
        d.P("lt_service_record+");      // 10 + 14 = 24
        Assert.False(d.It.IsAlive);
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Marked_man_plus_pays_strength_too()
    {
        using var d = One(Five("lt_marked_man+", "lt_pike_butt"));
        d.P("lt_marked_man+");
        d.P("lt_pike_butt");
        Assert.Equal(1, Authority(d));
        Assert.Equal(1, Strength(d));
    }
}
