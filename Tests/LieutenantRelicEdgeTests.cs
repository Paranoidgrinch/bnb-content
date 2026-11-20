using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.LieutenantDuel;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// THE LIEUTENANT'S RELICS AT THEIR EDGES (relic audit of 2026-11-20). LieutenantRelicTests shows each one doing its
// thing once; these hold each to the limits its text names — "the first time each turn", "you may", "a Challenged
// enemy", "an enemy attacks" — by doing the thing a second time, declining, or doing it where it should not count.
public class LieutenantRelicEdgeTests
{
    private static string[] Ten(string card) => [.. Enumerable.Repeat(card, 10)];

    [Fact]
    public void Tin_fife_answers_the_first_order_of_a_turn_only_and_again_the_next_turn()
    {
        using var d = Armed("lt_relic_tin_fife", Ten("lt_first_orders"));
        d.Play.CombatDriver!.PlayCard(Now(d.Play).Hand.First().Id, d.Enemy);
        Assert.NotNull(d.Play.CombatDriver.PendingCardChoice);
        d.Play.CombatDriver.SupplyCardChoice([d.Play.CombatDriver.PendingCardChoice![0].Id]);
        Assert.Equal(4, d.Hand);
        d.P("lt_first_orders");                    // a second Order this turn: nothing to put back
        Assert.Null(d.Play.CombatDriver.PendingCardChoice);
        Assert.Equal(3, d.Hand);
        d.End();
        d.Play.CombatDriver.PlayCard(Now(d.Play).Hand.First().Id, d.Enemy);
        Assert.NotNull(d.Play.CombatDriver.PendingCardChoice);
    }

    [Fact]
    public void Chipped_buckler_ignores_a_deed_at_an_enemy_without_a_challenge()
    {
        using var d = Armed("lt_relic_chipped_buckler", Five("lt_watch_cut", "lt_watch_cut"));
        d.P("lt_watch_cut");
        Assert.Equal(0, Block(d.Me));
    }

    [Fact]
    public void Sword_knot_pays_one_authority_a_turn_however_often_strength_is_spent()
    {
        using var d = Armed("lt_relic_sword_knot", Five("lt_armguard", "lt_armguard"), hero: (LieutenantKeywords.Strength, 2));
        d.P("lt_armguard", choose: 0);
        d.P("lt_armguard", choose: 0);
        Assert.Equal(0, Strength(d));
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Parade_spur_pays_once_for_an_attack_on_the_open_guard_even_a_double_one()
    {
        using var d = Armed("lt_relic_parade_spur", Five("lt_new_stripes"), Twice, TwiceIntent);
        d.P("lt_new_stripes", choose: 0);
        d.End();                                   // two blows: one Authority
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Parade_spur_pays_for_an_attack_the_block_stops()
    {
        // "The next time an enemy attacks you": a blow that is blocked still attacked.
        using var d = Armed("lt_relic_parade_spur", Five("lt_new_stripes", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"), Biter, BiteIntent);
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_buckler_up"); d.P("lt_buckler_up"); d.P("lt_buckler_up");
        var health = d.Me.Health.Current;
        d.End();
        Assert.Equal(health, d.Me.Health.Current);
        Assert.Equal(1, Authority(d));
    }

    [Fact]
    public void Pay_chest_pays_nothing_below_three_authority()
    {
        using var d = Armed("lt_relic_pay_chest", Five(), Biter, BiteIntent, hero: (LieutenantKeywords.Authority, 2));
        var health = d.Me.Health.Current;
        d.End();
        Assert.Equal(health - 6, d.Me.Health.Current);
    }

    [Fact]
    public void Dented_gorget_cheapens_one_deed_only_and_only_after_a_struck_guard()
    {
        using (var d = Armed("lt_relic_dented_gorget", [.. Five("lt_new_stripes"), .. Ten("lt_watch_cut")], Biter, BiteIntent))
        {
            d.P("lt_new_stripes", choose: 0);
            d.End();
            var energy = d.Energy;
            d.P("lt_watch_cut");
            Assert.Equal(energy, d.Energy);
            d.P("lt_watch_cut");
            Assert.Equal(energy - 1, d.Energy);
        }
        // Nobody struck the guard: full price.
        using var e = Armed("lt_relic_dented_gorget", [.. Five("lt_new_stripes"), .. Ten("lt_watch_cut")]);
        e.P("lt_new_stripes", choose: 0);
        e.End();
        var before = e.Energy;
        e.P("lt_watch_cut");
        Assert.Equal(before - 1, e.Energy);
    }

    [Fact]
    public void Full_tang_strikes_for_the_first_strength_spent_each_turn_only()
    {
        using var d = Armed("lt_relic_full_tang", Five("lt_armguard", "lt_armguard"), hero: (LieutenantKeywords.Strength, 2));
        var standing = Standing(d.It);
        d.P("lt_armguard", choose: 0);
        Assert.Equal(standing - 6, Standing(d.It));
        d.P("lt_armguard", choose: 0);
        Assert.Equal(standing - 6, Standing(d.It));
    }

    [Fact]
    public void Lead_knuckles_asks_only_with_open_guard_and_may_be_refused()
    {
        using (var d = Armed("lt_relic_lead_knuckles", Five("lt_watch_cut", "lt_watch_cut")))
        {
            d.P("lt_watch_cut");                   // no Open Guard: no question
            Assert.Null(d.Play.CombatDriver!.PendingOptionChoice);
        }
        using var e = Armed("lt_relic_lead_knuckles", Five("lt_new_stripes", "lt_watch_cut"));
        e.P("lt_new_stripes", choose: 0);
        var before = Standing(e.It);
        e.P("lt_watch_cut", choose: 1);            // keep the hand
        Assert.Equal(before - 9, Standing(e.It));
        Assert.Empty(Now(e.Play).State.GetCardZones(Now(e.Play).HeroId).ExhaustPile);
    }

    [Fact]
    public void Iron_shackle_may_be_refused_and_asks_once_a_turn()
    {
        using var d = Armed("lt_relic_iron_shackle", Five("lt_marked_man", "lt_marked_man"));
        d.P("lt_marked_man", choose: 1);           // not this time
        Assert.Equal(0, Stacks(d.It, Converter.Cards.Keywords.Doubt));
        Assert.Equal(0, Stacks(d.Me, LieutenantKeywords.OpenGuard));
        d.P("lt_marked_man");                      // the same turn: no second question
        Assert.Null(d.Play.CombatDriver!.PendingOptionChoice);
    }

    [Fact]
    public void Badge_chain_may_be_refused()
    {
        using var d = Armed("lt_relic_badge_chain", Five("lt_new_stripes", "lt_watch_cut"));
        d.P("lt_new_stripes", choose: 0);
        d.P("lt_watch_cut", choose: 1);
        Assert.Equal(1, Authority(d));
        Assert.Equal(2, Strength(d));
    }

    [Fact]
    public void Order_of_the_day_waits_into_the_next_turn_and_does_not_stack()
    {
        // Kept, and no Working played after it this turn: the discount waits for the next turn's first Working.
        using var d = Armed("lt_relic_worn_epaulette", ["lt_first_orders", "lt_watch_cut", "lt_buckler_up", "lt_buckler_up", "lt_watch_cut"]);
        d.P("lt_first_orders");
        d.P("lt_watch_cut");
        d.End();
        var energy = d.Energy;
        d.P("lt_buckler_up");                      // the next turn: 1 less
        Assert.Equal(energy, d.Energy);
        d.P("lt_buckler_up");                      // and only the one Working
        Assert.Equal(energy - 1, d.Energy);
    }

    [Fact]
    public void Promissory_note_may_be_refused_and_needs_strength_left()
    {
        using (var d = Armed("lt_relic_spur_buckle", Five("lt_armguard"), hero: (LieutenantKeywords.Strength, 2)))
        {
            d.Play.CombatDriver!.PlayCard(Now(d.Play).Hand.First(c => c.DefinitionId.value == "lt_armguard").Id, d.Enemy);
            d.Play.CombatDriver.SupplyOptionChoice([0]);
            d.Play.CombatDriver.SupplyOptionChoice([1]);   // keep the Strength
            Assert.Null(d.Session.Error);
            Assert.Equal(1, Strength(d));
        }
        using var e = Armed("lt_relic_spur_buckle", Five("lt_armguard"), hero: (LieutenantKeywords.Strength, 1));
        e.P("lt_armguard", choose: 0);             // the last Strength spent: nothing left to offer
        Assert.Null(e.Play.CombatDriver!.PendingOptionChoice);
        Assert.Equal(0, Strength(e));
    }
}
