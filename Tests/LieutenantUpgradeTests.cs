using BnbContent.Converter.Lieutenant;
using static BnbContent.Tests.LieutenantDuel;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// The Lieutenant's upgrades whose facet is a rule of its own (LIEUTENANT_PLAN §1, §4).
public class LieutenantUpgradeTests
{
    [Fact]
    public void Witness_plus_lets_two_deeds_on_another_enemy_count()
    {
        using var d = Two(Five("lt_witness+", "lt_marked_man", "lt_watch_cut", "lt_watch_cut"));
        d.P("lt_marked_man");           // the biter is Challenged
        d.P("lt_witness+");
        Assert.Equal(6, Block(d.Me));
        d.P("lt_watch_cut", at: d.Other);
        Assert.Equal(0, Authority(d));
        d.P("lt_watch_cut", at: d.Other); // two Deeds on the tablet: kept on the biter
        Assert.Equal(1, Authority(d));
        Assert.Equal(0, Stacks(d.Me, LieutenantRules.Witness));
    }

    [Fact]
    public void Make_an_example_plus_spreads_the_whole_loss_for_authority()
    {
        using var d = Two(Five("lt_make_an_example+"), (LieutenantKeywords.Authority, 1));
        var other = Standing(Body(d.Play, d.Other!.Value));
        d.P("lt_make_an_example+", choose: 0); // 18: the biter's 24 lose 18, and so does the tablet
        Assert.Equal(other - 18, Standing(Body(d.Play, d.Other!.Value)));
        Assert.Equal(0, Authority(d));
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
    public void No_surrender_plus_still_wants_open_guard_and_pays_a_card_more()
    {
        string[] deck = [.. Five("lt_new_stripes", "lt_no_surrender+", "lt_buckler_up"), .. Enumerable.Repeat("lt_watch_cut", 10)];
        using (var d = One(deck, Biter, BiteIntent))
        {
            d.P("lt_new_stripes", choose: 0); // 1 Open Guard
            d.P("lt_no_surrender+");
            d.P("lt_buckler_up");            // 7 + 7 Block against 6 and a quarter
            d.End();
            Assert.Equal(2, Authority(d));
            Assert.Equal(1, Stacks(d.Me, Obligations.Reinforcements));
        }
        using var e = One(deck, Biter, BiteIntent);
        e.P("lt_no_surrender+");
        e.P("lt_buckler_up");
        e.End();                             // no Open Guard: failed
        Assert.Equal(0, Authority(e));
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

    [Fact]
    public void Headlong_plus_keeps_one_open_guard_and_draws_a_card()
    {
        using var d = One([.. Five("lt_headlong+", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"), "lt_watch_cut", "lt_watch_cut"]);
        var hand = d.Hand;
        d.P("lt_headlong+");
        Assert.Equal(1, Stacks(d.Me, LieutenantKeywords.OpenGuard));
        Assert.Equal(hand, d.Hand);              // one played, one drawn
        var energy = d.Energy;
        d.P("lt_watch_cut");
        d.P("lt_watch_cut");
        Assert.Equal(energy, d.Energy);
    }

    [Fact]
    public void Flank_plus_still_asks_authority_for_the_second_blow()
    {
        using (var d = Two(Five("lt_flank+"), (LieutenantKeywords.Authority, 1)))
        {
            var other = Standing(Body(d.Play, d.Other!.Value));
            d.P("lt_flank+", choose: 0);
            Assert.Equal(other - 13, Standing(Body(d.Play, d.Other!.Value)));
            Assert.Equal(0, Authority(d));
        }
        using var e = Two(Five("lt_flank+"));
        var untouched = Standing(Body(e.Play, e.Other!.Value));
        e.P("lt_flank+");                        // nothing to pay with: nothing asked
        Assert.Equal(untouched, Standing(Body(e.Play, e.Other!.Value)));
    }

    [Fact]
    public void Arms_drill_spends_up_to_two_strength_for_a_blow_each()
    {
        using (var d = One(Five("lt_arms_drill"), health: 100, hero: (LieutenantKeywords.Strength, 2)))
        {
            var before = Standing(d.It);
            d.P("lt_arms_drill", choose: 0);     // 7 + 7, then 2 Strength spent: 5 + 5
            Assert.Equal(before - 24, Standing(d.It));
            Assert.Equal(0, Strength(d));
        }
        using var e = One(Five("lt_arms_drill+"), health: 100, hero: (LieutenantKeywords.Strength, 1));
        var start = Standing(e.It);
        e.P("lt_arms_drill+", choose: 0);        // 7 + 7, then 1 Strength spent: 6
        Assert.Equal(start - 20, Standing(e.It));
    }

    [Fact]
    public void Broken_nose_plus_brings_authority_with_it()
    {
        using var d = One(Five("lt_broken_nose+"));
        d.P("lt_broken_nose+");
        Assert.Equal(1, Authority(d));
    }
}
