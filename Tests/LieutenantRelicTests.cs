using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.LieutenantDuel;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// The Lieutenant's relics, one rule each (lieutenant_master.md §4).
public class LieutenantRelicTests
{
    [Fact]
    public void Her_nineteen_relics_are_three_five_four_and_seven_and_hers_alone()
    {
        var all = LieutenantRelics.All();
        Assert.Equal(19, all.Count);
        Assert.Equal(3, all.Count(r => r.Rarity == Converter.Relics.RelicAuthoring.Rarity.Common));
        Assert.Equal(5, all.Count(r => r.Rarity == Converter.Relics.RelicAuthoring.Rarity.Uncommon));
        Assert.Equal(4, all.Count(r => r.Rarity == Converter.Relics.RelicAuthoring.Rarity.Rare));
        Assert.Equal(7, all.Count(r => r.Pool == Converter.Relics.RelicAuthoring.Pool.Shop));
        Assert.All(all, r => Assert.Equal(Converter.Relics.RelicAuthoring.Eligibility.Lieutenant, r.Eligibility));
        Assert.All(all, r => Assert.NotNull(r.CombatRule));
    }

    [Fact]
    public void Tin_fife_draws_one_and_puts_one_back_when_an_order_is_issued()
    {
        using var d = Armed("lt_relic_tin_fife", [.. Five("lt_first_orders"), "lt_watch_cut"]);
        var hand = d.Hand;
        d.Play.CombatDriver!.PlayCard(Now(d.Play).Hand.First(c => c.DefinitionId.value == "lt_first_orders").Id, d.Enemy);
        var offered = d.Play.CombatDriver.PendingCardChoice;
        Assert.NotNull(offered);
        d.Play.CombatDriver.SupplyCardChoice([offered![0].Id]);
        Assert.Null(d.Session.Error);
        Assert.Equal(hand - 1, d.Hand);           // played one, drew one, put one back
    }

    [Fact]
    public void Chipped_buckler_guards_once_a_turn_for_hitting_the_challenged_enemy()
    {
        using var d = Armed("lt_relic_chipped_buckler", Five("lt_marked_man", "lt_pike_butt", "lt_watch_cut"));
        d.P("lt_marked_man");
        d.P("lt_pike_butt");                      // two hits, one card: 4
        Assert.Equal(4, Block(d.Me));
    }

    [Fact]
    public void Split_chinstrap_draws_when_open_guard_lapses_unspent()
    {
        string[] deck = [.. Five("lt_new_stripes"), .. Enumerable.Repeat("lt_watch_cut", 10)];
        // Nobody strikes: the Open Guard lapses unspent, and the next hand has a card more.
        using (var d = Armed("lt_relic_split_chinstrap", deck))
        {
            d.P("lt_new_stripes", choose: 0);
            d.End();
            Assert.Equal(6, d.Hand);
        }
        // Struck: the Open Guard was spent, and the hand is the usual five.
        using var e = Armed("lt_relic_split_chinstrap", deck, Biter, BiteIntent);
        e.P("lt_new_stripes", choose: 0);
        e.End();
        Assert.Equal(5, e.Hand);
    }

    [Fact]
    public void Sword_knot_pays_authority_for_the_first_strength_spent()
    {
        using var d = Armed("lt_relic_sword_knot", Five("lt_armguard"), hero: (LieutenantKeywords.Strength, 1));
        d.P("lt_armguard", choose: 0);
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Warrant_cord_draws_a_card_for_the_first_kept_challenge_each_turn()
    {
        using var d = Armed("lt_relic_warrant_cord", [.. Five("lt_new_stripes", "lt_watch_cut", "lt_new_stripes", "lt_watch_cut"), "lt_buckler_up", "lt_buckler_up"]);
        d.P("lt_new_stripes", choose: 0);
        var hand = d.Hand;
        d.P("lt_watch_cut");                       // kept: one played, one drawn
        Assert.Equal(1, Authority(d));
        Assert.Equal(hand, d.Hand);
        d.P("lt_new_stripes", choose: 0);
        hand = d.Hand;
        d.P("lt_watch_cut");                       // kept again, the same turn: no second card
        Assert.Equal(hand - 1, d.Hand);
    }

    [Fact]
    public void Parade_spur_pays_authority_when_an_attack_meets_the_open_guard()
    {
        using (var d = Armed("lt_relic_parade_spur", Five("lt_new_stripes"), Biter, BiteIntent))
        {
            d.P("lt_new_stripes", choose: 0);
            d.End();                               // nothing to choose: the bite pays as it lands
            Assert.Equal(1, Authority(d));
        }
        // No Open Guard at the end of the turn: nothing.
        using var e = Armed("lt_relic_parade_spur", Five(), Biter, BiteIntent);
        e.End();
        Assert.Equal(0, Authority(e));
    }

    [Fact]
    public void Pay_chest_guards_a_full_purse_at_the_end_of_the_turn()
    {
        using var d = Armed("lt_relic_pay_chest", Five(), Biter, BiteIntent, hero: (LieutenantKeywords.Authority, 3));
        d.End();                                   // 5 Block against 6
        Assert.Equal(79, d.Me.Health.Current);
    }

    [Fact]
    public void Dented_gorget_makes_the_first_deed_after_a_struck_guard_cheaper()
    {
        using var d = Armed("lt_relic_dented_gorget", Five("lt_new_stripes", "lt_watch_cut"), Biter, BiteIntent);
        d.P("lt_new_stripes", choose: 0);
        d.End();
        var energy = d.Energy;
        d.P("lt_watch_cut");
        Assert.Equal(energy, d.Energy);
    }

    [Fact]
    public void Torn_standard_hardens_a_failed_order_and_pays_the_first_kept_one()
    {
        using (var d = Armed("lt_relic_torn_standard", Five("lt_first_orders", "lt_watch_cut", "lt_first_orders", "lt_watch_cut")))
        {
            d.P("lt_first_orders");
            d.P("lt_watch_cut");                   // kept: 1 Authority, and 1 more for the first this turn
            Assert.Equal(2, Authority(d));
            d.P("lt_first_orders");
            d.P("lt_watch_cut");                   // kept again: only the Order's own
            Assert.Equal(3, Authority(d));
            Assert.Equal(0, Strength(d));
        }
        using var e = Armed("lt_relic_torn_standard", Five("lt_first_orders"));
        e.P("lt_first_orders");
        e.End();                                   // failed: 1 Strength
        Assert.Equal(1, Strength(e));
    }

    [Fact]
    public void Full_tang_strikes_for_the_strength_spent()
    {
        using var d = Armed("lt_relic_full_tang", Five("lt_armguard"), hero: (LieutenantKeywords.Strength, 1));
        var before = Standing(d.It);
        d.P("lt_armguard", choose: 0);
        Assert.Equal(before - 6, Standing(d.It));
    }

    [Fact]
    public void Scarlet_epaulette_adds_a_half_blow_after_open_guard()
    {
        using var d = Armed("lt_relic_scarlet_epaulette", Five("lt_new_stripes", "lt_watch_cut"));
        d.P("lt_new_stripes", choose: 0);          // 1 Strength, Open Guard, Challenged
        var before = Standing(d.It);
        d.P("lt_watch_cut");                       // 9, then half of (9 − 1) + 1
        Assert.Equal(before - 9 - 5, Standing(d.It));
    }

    [Fact]
    public void Commission_raises_the_cap_to_eight_and_counts_what_is_left()
    {
        using (var d = Armed("lt_relic_commission", Five("lt_signal"), hero: (LieutenantKeywords.Authority, 1)))
        {
            Assert.Equal(1, Authority(d));        // no Authority of its own any more
            d.P("lt_signal", choose: 0);
            Assert.Equal(0, Counter(d.Me, LieutenantRules.AuthorityLeft));
        }
        // 7 and a kept Challenge's 1: 8, past the usual 6.
        using (var e = Armed("lt_relic_commission", Five("lt_new_stripes", "lt_watch_cut"), hero: (LieutenantKeywords.Authority, 7)))
        {
            e.P("lt_new_stripes", choose: 0);
            e.P("lt_watch_cut");
            Assert.Equal(8, Authority(e));
        }
        // Without it, 6 is the most.
        using var f = One(Five("lt_new_stripes", "lt_watch_cut"), hero: (LieutenantKeywords.Authority, 6));
        f.P("lt_new_stripes", choose: 0);
        f.P("lt_watch_cut");
        Assert.Equal(6, Authority(f));
    }

    [Fact]
    public void Lead_knuckles_burns_a_card_for_eight_more()
    {
        using var d = Armed("lt_relic_lead_knuckles", Five("lt_new_stripes", "lt_watch_cut"));
        d.P("lt_new_stripes", choose: 0);
        var before = Standing(d.It);
        d.Play.CombatDriver!.PlayCard(Now(d.Play).Hand.First(c => c.DefinitionId.value == "lt_watch_cut").Id, d.Enemy);
        Assert.NotNull(d.Play.CombatDriver.PendingOptionChoice);
        d.Play.CombatDriver.SupplyOptionChoice([0]);
        var offered = d.Play.CombatDriver.PendingCardChoice;
        Assert.NotNull(offered);
        d.Play.CombatDriver.SupplyCardChoice([offered![0].Id]);
        Assert.Null(d.Session.Error);
        Assert.Equal(before - 9 - 8, Standing(d.It));
        Assert.Single(Now(d.Play).State.GetCardZones(Now(d.Play).HeroId).ExhaustPile);
    }

    [Fact]
    public void Iron_shackle_doubts_the_challenged_enemy_for_an_open_guard()
    {
        using var d = Armed("lt_relic_iron_shackle", Five("lt_marked_man"));
        d.Play.CombatDriver!.PlayCard(Now(d.Play).Hand.First(c => c.DefinitionId.value == "lt_marked_man").Id, d.Enemy);
        Assert.NotNull(d.Play.CombatDriver.PendingOptionChoice);
        d.Play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(d.Session.Error);
        Assert.Equal(2, Stacks(d.It, Converter.Cards.Keywords.Doubt));
        Assert.Equal(1, Stacks(d.Me, LieutenantKeywords.OpenGuard));
    }

    [Fact]
    public void Brandy_flask_has_the_fight_count_what_open_guard_cost()
    {
        using var d = Armed("lt_relic_brandy_flask", Five("lt_new_stripes"), Biter, BiteIntent);
        d.P("lt_new_stripes", choose: 0);
        d.End();                                   // 6 + a quarter of 6, no Block: Open Guard cost 1
        Assert.Equal(1, Counter(d.Me, LieutenantKeywords.GuardExtraTotal));
    }

    [Fact]
    public void Badge_chain_turns_a_kept_challenges_authority_into_strength()
    {
        using var d = Armed("lt_relic_badge_chain", Five("lt_new_stripes", "lt_watch_cut"));
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_watch_cut", choose: 0);
        Assert.Equal(0, Authority(d));
        Assert.Equal(3, Strength(d));   // the card's 1, the Challenge's 1 and its Authority's 1
    }

    [Fact]
    public void Order_of_the_day_makes_the_next_working_cheaper()
    {
        using var d = Armed("lt_relic_worn_epaulette", Five("lt_first_orders", "lt_watch_cut", "lt_buckler_up"));
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        var energy = d.Energy;
        d.P("lt_buckler_up");
        Assert.Equal(energy, d.Energy);
    }

    [Fact]
    public void Promissory_note_trades_one_more_strength_for_an_energy()
    {
        using var d = Armed("lt_relic_spur_buckle", Five("lt_armguard"), hero: (LieutenantKeywords.Strength, 2));
        var energy = d.Energy;
        d.Play.CombatDriver!.PlayCard(Now(d.Play).Hand.First(c => c.DefinitionId.value == "lt_armguard").Id, d.Enemy);
        d.Play.CombatDriver.SupplyOptionChoice([0]);     // spend 1 Strength for Ward Wax
        Assert.NotNull(d.Play.CombatDriver.PendingOptionChoice);
        d.Play.CombatDriver.SupplyOptionChoice([0]);     // and 1 more for Energy
        Assert.Null(d.Session.Error);
        Assert.Equal(0, Strength(d));
        Assert.Equal(energy, d.Energy);                  // 1 paid for the card, 1 back
    }

    [Fact]
    public void Regimental_drum_opens_the_fight_with_open_guard_authority_and_strength()
    {
        using var d = Armed("lt_relic_regimental_drum", Five());
        Assert.Equal(1, Stacks(d.Me, LieutenantKeywords.OpenGuard));
        Assert.Equal(1, Authority(d));
        Assert.Equal(1, Strength(d));
        d.End();                                   // once a fight: the next turn brings nothing more
        Assert.Equal(0, Stacks(d.Me, LieutenantKeywords.OpenGuard));
        Assert.Equal(1, Authority(d));
        Assert.Equal(1, Strength(d));
    }
}
