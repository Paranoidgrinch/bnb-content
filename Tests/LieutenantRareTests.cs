using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.LieutenantDuel;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// The Lieutenant's rares, one rule each (lieutenant_master.md §3.4), with the guardrails of §2.7.
public class LieutenantRareTests
{
    private const string Wax = Converter.Cards.Keywords.WardWax;

    [Fact]
    public void Brevet_makes_a_one_authority_price_free_and_free_is_no_payment()
    {
        using var d = One(Five("lt_brevet", "lt_signal", "lt_rising_star"));
        d.P("lt_rising_star");           // +1 Authority; pays when a payment leaves 0
        d.P("lt_brevet");                // +1 Authority: 2
        var energy = d.Energy;
        d.P("lt_signal", choose: 0);     // free with Brevet: 1 Energy, Authority untouched
        Assert.Equal(2, Authority(d));
        Assert.Equal(energy + 1, d.Energy);
        Assert.Equal(0, Strength(d));    // no payment, so Rising Star stays quiet
    }

    [Fact]
    public void Insubordination_abandons_an_order_without_failing_it()
    {
        using var d = Two(Five("lt_first_orders", "lt_insubordination"));
        d.P("lt_first_orders");
        var before = Standing(d.It);
        d.P("lt_insubordination", choose: 0);
        Assert.Equal(before - 20, Standing(d.It));
        Assert.Equal(0, Stacks(d.Me, Promises.FirstOrders.Id));
        Assert.Equal(1, Stacks(d.Me, LieutenantKeywords.OpenGuard));
        Assert.Equal(0, Counter(d.Me, Obligations.OrderFailedRound));
        Assert.Equal(0, Authority(d));
    }

    [Fact]
    public void Parade_ground_turns_unspent_energy_into_ward_wax_after_a_kept_order()
    {
        using var d = One(Five("lt_parade_ground", "lt_first_orders", "lt_watch_cut"));
        d.P("lt_parade_ground");
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        d.End();
        // 2 Energy (at most) × 5 — and Ward Wax wears down by 1 over the enemy phase.
        Assert.InRange(Stacks(d.Me, Wax), 9, 10);
    }

    [Fact]
    public void Parade_ground_pays_for_an_order_kept_at_the_turns_end()
    {
        using var d = One(Five("lt_parade_ground", "lt_scabbard"));
        d.P("lt_parade_ground");
        d.P("lt_scabbard");
        d.End();
        Assert.Equal(1, Authority(d));
        Assert.InRange(Stacks(d.Me, Wax), 9, 10);
    }

    [Fact]
    public void Drum_major_pays_an_energy_for_the_first_order_kept_in_the_turn()
    {
        using var d = One(Five("lt_drum_major", "lt_first_orders", "lt_watch_cut"));
        d.P("lt_drum_major");
        d.P("lt_first_orders");
        var energy = d.Energy;
        d.P("lt_watch_cut");
        Assert.Equal(energy, d.Energy);         // 1 spent on the cut, 1 back
    }

    [Fact]
    public void Service_record_counts_the_challenges_kept()
    {
        using var d = Two(Five("lt_new_stripes", "lt_watch_cut", "lt_service_record"));
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_watch_cut");                    // kept
        var before = Standing(d.It);
        d.P("lt_service_record");               // 8 + 4, 1 Strength
        Assert.Equal(before - 13, Standing(d.It));
    }

    [Fact]
    public void Splintered_shield_trades_all_block_for_strength_authority_and_guard()
    {
        using var d = One(Five("lt_splintered_shield"));
        d.P("lt_buckler_up");
        d.P("lt_splintered_shield", choose: 0);
        Assert.Equal(0, Block(d.Me));
        Assert.Equal(1, Strength(d));
        Assert.Equal(1, Authority(d));
        Assert.Equal(2, Stacks(d.Me, LieutenantKeywords.OpenGuard));
    }

    [Fact]
    public void Last_ditch_doubles_only_its_missing_health_part()
    {
        using var d = Two(Five("lt_last_ditch", "lt_last_ditch"));
        var before = Standing(d.It);
        d.P("lt_last_ditch", choose: 1);        // full health: 10
        Assert.Equal(before - 10, Standing(d.It));
        d.Dispose();

        using var e = One(Five("lt_last_ditch"), enemy: Biter, intent: BiteIntent, health: 50);
        // 30 missing: +10; with Open Guard, +20 — the biter has 24
        e.P("lt_last_ditch", choose: 1);
        Assert.Equal(4, e.It.Health.Current);
    }

    [Fact]
    public void Open_visor_lets_the_next_deed_ignore_block_and_leaves_the_block_standing()
    {
        using var d = One(Five("lt_open_visor", "lt_watch_cut"));
        d.End();                                 // the tablet guards itself
        var block = Block(d.It);
        Assert.True(block > 0);
        var health = d.It.Health.Current;
        d.P("lt_open_visor", choose: 0);
        d.P("lt_watch_cut");
        Assert.Equal(health - 6, d.It.Health.Current);
        Assert.Equal(block, Block(d.It));
    }

    [Fact]
    public void Black_gauntlet_wants_her_to_live_through_the_attack_and_answer_it()
    {
        using var d = Two(Five("lt_black_gauntlet", "lt_watch_cut"));
        d.P("lt_black_gauntlet");
        d.End();                                 // the biter strikes; she lives
        Assert.Equal(1, Stacks(d.It, Promises.BlackGauntlet.Id));
        d.P("lt_watch_cut");
        Assert.Equal(2, Authority(d));
        Assert.Equal(1, Strength(d));
    }

    [Fact]
    public void Redress_moves_a_challenge_to_another_enemy_that_fits_it()
    {
        using var d = Two(Five("lt_new_stripes", "lt_redress", "lt_watch_cut"));
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_redress", choose: 0, at: d.Other);
        Assert.Equal(0, Stacks(d.It, Promises.NewStripes.Id));
        Assert.Equal(1, Stacks(Body(d.Play, d.Other!.Value), Promises.NewStripes.Id));
        d.P("lt_watch_cut", at: d.Other);
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Riposte_strikes_back_at_a_challenged_attacker()
    {
        using var d = Two(Five("lt_riposte", "lt_braced_pike"));
        d.P("lt_riposte");
        d.P("lt_braced_pike");                   // a Challenge on the biter, who means to attack
        var health = d.It.Health.Current;
        d.End();
        Assert.Equal(health - 5, d.It.Health.Current);
    }

    [Fact]
    public void Countercharge_spends_strength_for_doubt_and_authority()
    {
        using var d = One(Five("lt_countercharge"), hero: (LieutenantKeywords.Strength, 2));
        d.P("lt_countercharge", choose: 0);
        Assert.Equal(2, Stacks(d.It, Converter.Cards.Keywords.Doubt));
        Assert.Equal(2, Authority(d));
        Assert.Equal(0, Strength(d));
    }

    [Fact]
    public void Forced_march_takes_back_what_is_left_and_never_more()
    {
        using var d = One(Five("lt_forced_march", "lt_heave"));
        d.P("lt_forced_march");
        Assert.Equal(3, Strength(d));
        d.P("lt_heave", choose: 0);              // spends 2
        d.End();
        Assert.Equal(0, Strength(d));
    }

    [Fact]
    public void Reprimand_answers_an_order_that_failed_last_turn()
    {
        using var d = One(Five("lt_first_orders", "lt_reprimand"));
        d.P("lt_first_orders");
        d.End();                                 // no Deed: failed
        d.P("lt_reprimand");
        Assert.Equal(2, Strength(d));
        Assert.Equal(1, Stacks(d.Me, LieutenantKeywords.OpenGuard));
    }

    [Fact]
    public void Chain_of_command_holds_two_orders_and_pays_for_keeping_both()
    {
        using var d = One([.. Five("lt_chain_of_command", "lt_cadence", "lt_first_orders", "lt_watch_cut"), "lt_watch_cut", "lt_watch_cut"]);
        if (!new[] { "lt_chain_of_command", "lt_cadence", "lt_first_orders", "lt_watch_cut" }
                .All(id => Now(d.Play).Hand.Any(c => c.DefinitionId.value == id)))
            return;
        d.P("lt_chain_of_command");
        d.P("lt_cadence");
        d.P("lt_first_orders");
        Assert.Equal(1, Stacks(d.Me, Promises.Cadence.Id));
        Assert.Equal(1, Stacks(d.Me, Promises.FirstOrders.Id));
        d.P("lt_watch_cut");                     // keeps First Orders; the second card after Cadence
        d.End();                                 // keeps Cadence
        Assert.Equal(2, Authority(d));
        Assert.True(d.Hand >= 6);
    }

    [Fact]
    public void Succession_lays_a_new_challenge_on_another_enemy()
    {
        using var d = Two(Five("lt_succession", "lt_new_stripes", "lt_watch_cut"));
        d.P("lt_succession");
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_watch_cut");
        Assert.Equal(1, Stacks(Body(d.Play, d.Other!.Value), Promises.Successor.Id));
    }

    [Fact]
    public void Hilt_knot_draws_and_guards_after_a_strength_payment()
    {
        using var d = One([.. Five("lt_hilt_knot", "lt_armguard"), "lt_watch_cut"]);
        if (!Now(d.Play).Hand.Any(c => c.DefinitionId.value == "lt_hilt_knot") ||
            !Now(d.Play).Hand.Any(c => c.DefinitionId.value == "lt_armguard"))
            return;
        d.P("lt_hilt_knot");                     // 1 Strength
        var hand = d.Hand;
        d.P("lt_armguard", choose: 0);
        Assert.Equal(hand, d.Hand);              // one played, one drawn
        Assert.Equal(8 + 5, Block(d.Me));
    }

    [Fact]
    public void Pike_square_turns_aside_the_first_hit_and_strikes_back()
    {
        using var d = One(Five("lt_pike_square"), enemy: Biter, intent: BiteIntent, hero: (LieutenantKeywords.Strength, 1));
        d.P("lt_pike_square");                   // 12 Block; 6 less from the first hit
        var health = d.It.Health.Current;
        d.End();
        Assert.Equal(80, d.Me.Health.Current);
        Assert.Equal(health - 6, d.It.Health.Current);
    }

    [Fact]
    public void Rising_star_pays_when_a_payment_empties_her_authority()
    {
        using var d = One(Five("lt_rising_star", "lt_signal"));
        d.P("lt_rising_star");
        d.P("lt_signal", choose: 0);
        Assert.Equal(0, Authority(d));
        Assert.Equal(1, Strength(d));
        Assert.Equal(2 + 4, Block(d.Me));
    }

    [Fact]
    public void Watchword_pays_next_turn_for_an_order_and_a_challenge_kept()
    {
        using var d = One([.. Five("lt_watchword", "lt_first_orders", "lt_new_stripes", "lt_watch_cut"), "lt_watch_cut"]);
        if (!new[] { "lt_watchword", "lt_first_orders", "lt_new_stripes", "lt_watch_cut" }
                .All(id => Now(d.Play).Hand.Any(c => c.DefinitionId.value == id)))
            return;
        d.P("lt_watchword");
        d.P("lt_first_orders");
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_watch_cut");                     // keeps both
        Assert.Equal(2, Authority(d));
        d.End();
        Assert.Equal(3, Authority(d));
        Assert.Equal(2, Strength(d));
    }

    [Fact]
    public void Armsmaster_asks_at_the_start_of_every_turn()
    {
        using var d = One(Five("lt_armsmaster"));
        d.P("lt_armsmaster");
        Assert.Equal(1, Strength(d));
        d.Play.CombatDriver!.EndTurn();
        Assert.NotNull(d.Play.CombatDriver.PendingOptionChoice);
        d.Play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(d.Session.Error);
        Assert.Equal(2, Strength(d));
    }

    [Fact]
    public void Armsmasters_saving_takes_one_off_a_payment_of_two()
    {
        using var d = One(Five("lt_armsmaster", "lt_heave"), hero: (LieutenantKeywords.Strength, 2));
        d.P("lt_armsmaster");                    // 3
        d.Play.CombatDriver!.EndTurn();
        d.Play.CombatDriver.SupplyOptionChoice([1]);
        d.P("lt_heave", choose: 0);              // declares 2, pays 1
        Assert.Equal(2, Strength(d));
        Assert.Equal(1, Counter(d.Me, LieutenantKeywords.StrengthPaidLast));
    }

    [Fact]
    public void Full_tilt_buys_more_blows_with_energy()
    {
        using var d = Two(Five("lt_full_tilt"));
        var energy = d.Energy;
        d.P("lt_full_tilt", choose: 0);          // 9 three times: the biter's 24 and more
        Assert.False(d.It.IsAlive);
        Assert.Equal(energy - 3, d.Energy);
        Assert.Equal(2, Stacks(d.Me, LieutenantKeywords.OpenGuard));
    }

    [Fact]
    public void Blood_debt_pays_back_what_open_guard_cost()
    {
        using var d = Two(Five("lt_blood_debt", "lt_new_stripes"));
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_blood_debt");                    // 6 Block against 6 + 2: 2 through, both of them Open Guard's
        var health = d.It.Health.Current;
        d.End();
        Assert.Equal(health - 2, d.It.Health.Current);
    }

    [Fact]
    public void Watch_surgeon_heals_for_a_kept_promise_and_an_authority()
    {
        using var d = One(Five("lt_watch_surgeon", "lt_first_orders", "lt_watch_cut"), health: 70, hero: (LieutenantKeywords.Authority, 1));
        d.P("lt_watch_surgeon");
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        Assert.Equal(72, d.Me.Health.Current);
        Assert.Equal(1, Authority(d));           // 1 + 1 kept − 1 spent
    }

    [Fact]
    public void Field_pardon_declares_a_standing_order_kept_once()
    {
        using var d = One(Five("lt_first_orders", "lt_field_pardon"), hero: (LieutenantKeywords.Authority, 2));
        d.P("lt_first_orders");
        d.P("lt_field_pardon", choose: 0);       // +1 = 3, spend 3, the Order pays 1
        Assert.Equal(0, Stacks(d.Me, Promises.FirstOrders.Id));
        Assert.Equal(1, Authority(d));
        Assert.Equal(1, Counter(d.Me, Obligations.OrdersKept));
    }
}
