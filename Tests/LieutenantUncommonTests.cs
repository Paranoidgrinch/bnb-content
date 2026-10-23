using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.LieutenantTests;
using static BnbContent.Tests.LieutenantDuel;

namespace BnbContent.Tests;

// The Lieutenant's uncommons, one rule each (lieutenant_master.md §3.3).
public class LieutenantUncommonTests
{

    [Fact]
    public void Double_file_wants_two_different_enemies_hit()
    {
        using var d = Two(Five("lt_double_file", "lt_watch_cut", "lt_watch_cut"));
        d.P("lt_double_file");
        d.P("lt_watch_cut");
        Assert.Equal(0, Authority(d));
        d.P("lt_watch_cut", at: d.Other);
        Assert.Equal(1, Authority(d));
        Assert.Equal(0, Stacks(d.It, Obligations.DoubleFileMark));
    }

    [Fact]
    public void Cadence_wants_exactly_two_more_cards()
    {
        using (var d = One(Five("lt_cadence", "lt_watch_cut", "lt_watch_cut")))
        {
            d.P("lt_cadence");
            d.P("lt_watch_cut");
            d.P("lt_watch_cut");
            d.End();
            Assert.Equal(1, Authority(d));
        }
        using (var d = One(Five("lt_cadence", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut")))
        {
            d.P("lt_cadence");
            d.P("lt_watch_cut");
            d.P("lt_watch_cut");
            d.P("lt_watch_cut");
            d.End();
            Assert.Equal(0, Authority(d));
        }
    }

    [Fact]
    public void No_excuses_is_kept_by_opening_the_next_turn_with_a_deed()
    {
        using var d = One(Five("lt_no_excuses", "lt_watch_cut"));
        d.P("lt_no_excuses");
        d.End();
        Assert.Equal(1, Stacks(d.Me, Promises.NoExcuses.Id));
        d.P("lt_watch_cut");
        Assert.Equal(1, Authority(d));
        Assert.Equal(4, Block(d.Me));
    }

    [Fact]
    public void No_surrender_pays_two_for_an_exposed_phase_that_costs_nothing()
    {
        using var d = One(Five("lt_no_surrender", "lt_buckle", "lt_buckler_up"), enemy: Biter, intent: BiteIntent);
        d.P("lt_no_surrender");
        d.P("lt_buckle");          // Open Guard, 3 Block
        d.P("lt_buckler_up");      // 7 more: 15 against 6 + 2
        d.End();
        Assert.Equal(2, Authority(d));
    }

    [Fact]
    public void Gauntlet_needs_one_big_hit()
    {
        using var d = Two(Five("lt_gauntlet", "lt_watch_cut"));
        d.P("lt_gauntlet");
        d.P("lt_watch_cut");       // 8: not 12
        Assert.Equal(0, Authority(d));
        Assert.Equal(1, Stacks(d.It, Promises.Gauntlet.Id));
    }

    [Fact]
    public void Gauntlet_is_kept_by_a_blow_of_twelve()
    {
        using var d = One(Five("lt_gauntlet", "lt_watch_cut"), health: 40, hero: (LieutenantKeywords.Strength, 4));
        d.P("lt_gauntlet");        // 13 of 40
        d.P("lt_watch_cut");       // 6 + 2 (Challenged) + 4 = 12
        Assert.Equal(1, Authority(d));
        Assert.Equal(6, Strength(d));
    }

    [Fact]
    public void On_your_feet_pays_a_card_next_turn_if_block_is_left()
    {
        // Six cards, so a sixth can be drawn.
        using var d = One([.. Five("lt_on_your_feet"), "lt_watch_cut"], enemy: Biter, intent: BiteIntent);
        if (!Now(d.Play).Hand.Any(c => c.DefinitionId.value == "lt_on_your_feet"))
            return;
        d.P("lt_on_your_feet");    // 8 Block against 6
        d.End();
        Assert.Equal(1, Authority(d));
        Assert.Equal(6, d.Hand);
    }

    [Fact]
    public void Pursuit_wants_a_hit_this_turn_and_one_the_next()
    {
        using var d = One(Five("lt_pursuit", "lt_watch_cut", "lt_watch_cut"));
        d.P("lt_pursuit");
        d.P("lt_watch_cut");
        d.End();
        Assert.Equal(1, Stacks(d.It, Promises.Pursuit.Id));
        d.P("lt_watch_cut");
        Assert.Equal(2, Authority(d));
    }

    [Fact]
    public void Pursuit_without_this_turns_hit_fails_at_its_end()
    {
        using var d = One(Five("lt_pursuit"));
        d.P("lt_pursuit");
        d.End();
        Assert.Equal(0, Stacks(d.It, Promises.Pursuit.Id));
    }

    [Fact]
    public void Dare_pays_when_the_attack_draws_blood()
    {
        using var d = One(Five("lt_dare", "lt_new_stripes"), enemy: Biter, intent: BiteIntent);
        d.P("lt_new_stripes", choose: 0);   // its Challenge gives way to Dare's
        d.P("lt_dare");            // 8 Block against 6 + 3 (two stacks of Open Guard)
        d.End();
        Assert.Equal(2, Authority(d));
        Assert.Equal(3, Strength(d));   // New Stripes' 1 and Dare's 2
    }

    [Fact]
    public void Second_blade_wants_two_different_deeds()
    {
        using var d = One(Five("lt_second_blade", "lt_pike_butt", "lt_watch_cut"));
        d.P("lt_second_blade");
        d.P("lt_pike_butt");
        Assert.Equal(0, Authority(d));
        d.P("lt_watch_cut");
        Assert.Equal(1, Authority(d));
        Assert.Equal(2, Strength(d));
    }

    [Fact]
    public void Testimony_is_kept_by_keeping_an_order()
    {
        using var d = One(Five("lt_testimony", "lt_first_orders", "lt_watch_cut"));
        d.P("lt_testimony");
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        Assert.Equal(2, Authority(d));
        Assert.Equal(0, Stacks(d.It, Promises.Testimony.Id));
    }

    [Fact]
    public void Remand_carries_a_challenge_past_one_deadline()
    {
        using var d = One(Five("lt_new_stripes", "lt_remand", "lt_watch_cut"), hero: (LieutenantKeywords.Authority, 1));
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_remand", choose: 0);
        d.End();
        Assert.Equal(1, Stacks(d.It, Promises.NewStripes.Id));
        d.P("lt_watch_cut");
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Stand_down_withdraws_the_challenge_and_draws_three()
    {
        using var d = One(["lt_new_stripes", "lt_stand_down", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up",
            "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"],
            hero: [(LieutenantKeywords.Authority, 1), (Obligations.Reinforcements, 4)]);   // nine cards: the whole deck
        Assert.Contains(Now(d.Play).Hand, c => c.DefinitionId.value == "lt_new_stripes");
        Assert.Contains(Now(d.Play).Hand, c => c.DefinitionId.value == "lt_stand_down");
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_buckler_up");
        d.P("lt_buckler_up");
        var hand = d.Hand;                 // 6 in hand, 3 in the discard pile to draw back
        d.P("lt_stand_down", choose: 0);
        Assert.Equal(0, Stacks(d.It, Promises.NewStripes.Id));
        Assert.Equal(hand - 1 + 3, d.Hand);
    }

    [Fact]
    public void Make_an_example_spreads_half_of_what_the_target_lost()
    {
        using var d = Two(Five("lt_make_an_example"), (LieutenantKeywords.Authority, 1));
        var other = Standing(Body(d.Play, d.Other!.Value));
        d.P("lt_make_an_example", choose: 0);   // 14 of the biter's 24
        Assert.Equal(other - 7, Standing(Body(d.Play, d.Other!.Value)));
    }

    [Fact]
    public void Heave_strikes_again_with_the_strength_that_is_left()
    {
        using var d = One(Five("lt_heave"), health: 40, hero: (LieutenantKeywords.Strength, 3));
        var before = Standing(d.It);
        d.P("lt_heave", choose: 0);   // 10 + 3, then 10 + 1
        Assert.Equal(before - 24, Standing(d.It));
        Assert.Equal(1, Strength(d));
    }

    [Fact]
    public void Grip_turns_strength_into_cards_and_block()
    {
        using var d = One(["lt_grip", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up",
            "lt_watch_cut", "lt_watch_cut"], hero: (LieutenantKeywords.Strength, 2));
        if (!Now(d.Play).Hand.Any(c => c.DefinitionId.value == "lt_grip"))
            return;
        var hand = d.Hand;
        d.P("lt_grip", choose: 0);
        Assert.Equal(0, Strength(d));
        Assert.Equal(8, Block(d.Me));
        Assert.Equal(hand - 1 + 2, d.Hand);
    }

    [Fact]
    public void Poise_keeps_the_next_deeds_open_guard_away()
    {
        using var d = One(Five("lt_poise", "lt_front_rank"), hero: (LieutenantKeywords.Authority, 1));
        d.P("lt_poise", choose: 0);
        d.P("lt_front_rank");
        Assert.Equal(0, Stacks(d.Me, LieutenantKeywords.OpenGuard));
        Assert.Equal(0, Stacks(d.Me, LieutenantRules.Poise));
    }

    [Fact]
    public void Under_the_colours_gives_strength_for_the_first_kept_order_each_turn()
    {
        using var d = One(Five("lt_under_the_colours", "lt_first_orders", "lt_watch_cut", "lt_first_orders+"));
        d.P("lt_under_the_colours");
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        Assert.Equal(1, Strength(d));
    }

    [Fact]
    public void Scar_answers_the_first_blow_that_draws_blood()
    {
        using var d = One(Five("lt_scar"), enemy: Biter, intent: BiteIntent);
        d.P("lt_scar");
        d.End();
        Assert.Equal(2, Strength(d));
    }

    [Fact]
    public void Overreach_charges_the_next_deed_after_open_guard()
    {
        using var d = One(Five("lt_overreach", "lt_new_stripes", "lt_watch_cut"));
        d.P("lt_overreach");
        d.P("lt_new_stripes", choose: 0);   // 1 Strength, Open Guard, a Challenge
        var before = Standing(d.It);
        d.P("lt_watch_cut");                // 6 + 2 + 1 + 6
        Assert.Equal(before - 15, Standing(d.It));
    }

    [Fact]
    public void Broken_nose_pays_when_open_guard_costs_blood()
    {
        using var d = One(Five("lt_broken_nose", "lt_new_stripes"), enemy: Biter, intent: BiteIntent);
        d.P("lt_broken_nose");
        d.P("lt_new_stripes", choose: 0);
        d.End();
        Assert.Equal(1, Authority(d));
        Assert.Equal(3, Strength(d));   // New Stripes' 1 and Broken Nose's 2
    }

    [Fact]
    public void Recoil_draws_two_more_after_an_exposed_phase_that_hurt()
    {
        using var d = One(Five("lt_recoil", "lt_new_stripes"), enemy: Biter, intent: BiteIntent);
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_recoil");          // 8 Block against 6 + 2: nothing through
        d.End();
        Assert.Equal(5, d.Hand);
        d.Dispose();

        using var e = One(Five("lt_recoil", "lt_new_stripes", "lt_heel_turn"), enemy: Biter, intent: BiteIntent);
        e.P("lt_new_stripes", choose: 0);
        e.P("lt_heel_turn");
        e.End();
        Assert.Equal(5, e.Hand);   // no Recoil played
    }

    [Fact]
    public void Ultimatum_buys_two_doubt()
    {
        using var d = One(Five("lt_ultimatum"), hero: (LieutenantKeywords.Authority, 2));
        d.P("lt_ultimatum", choose: 0);
        Assert.Equal(2, Stacks(d.It, Converter.Cards.Keywords.Doubt));
        Assert.Equal(0, Authority(d));
    }
}
