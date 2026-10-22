using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// The Lieutenant's cards, one rule each (lieutenant_master.md §3), in fights of the real document.
public class LieutenantCardTests
{
    private static int Standing(CombatantState c) => c.Health.Current + Block(c);
    private static int Energy(LieutenantFight f) =>
        Hero(f.Play).Resources[StandardCombatIds.EnergyResource].Current;

    internal sealed record LieutenantFight(RogueDeck.Sandbox.Composition.RunPlayback Play,
        RogueDeck.Sandbox.Run.InteractiveRunSession Session, CombatantId Enemy) : IDisposable
    {
        public void Play_(string card, int? choose = null) => LieutenantTests.Play(Play, Session, card, Enemy, choose);
        public void End() => EndTurn(Play, Session);
        public CombatantState Me => Hero(Play);
        public CombatantState It => Body(Play, Enemy);
        public void Dispose() => Play.Dispose();
    }

    private static LieutenantFight F(string[] deck, int energy = 9, string enemy = Quiet, string intent = QuietIntent,
        params (string Status, int Stacks)[] hero)
    {
        var (play, session, id) = Fight(deck, energy, enemy: enemy, intent: intent, hero: hero);
        return new LieutenantFight(play, session, id);
    }

    private static string[] Five(params string[] cards) =>
        [.. cards, .. Enumerable.Repeat("lt_buckler_up", Math.Max(0, 5 - cards.Length))];

    [Fact]
    public void Pike_butt_hits_twice_and_strength_counts_on_each()
    {
        using var f = F(Five("lt_pike_butt"), hero: (LieutenantKeywords.Strength, 1));
        var before = Standing(f.It);
        f.Play_("lt_pike_butt");
        Assert.Equal(before - 10, Standing(f.It));
    }

    [Fact]
    public void Heel_turn_strikes_twice_behind_open_guard()
    {
        using var f = F(Five("lt_heel_turn", "lt_heel_turn", "lt_new_stripes"));
        var before = Standing(f.It);
        f.Play_("lt_heel_turn");
        Assert.Equal(before - 3, Standing(f.It));
        f.Play_("lt_new_stripes", choose: 0);   // 1 Strength, 1 Open Guard
        before = Standing(f.It);
        f.Play_("lt_heel_turn");
        Assert.Equal(before - 8, Standing(f.It));
    }

    [Fact]
    public void Clear_the_square_gives_open_guard_for_each_enemy_it_hits()
    {
        using var f = F(Five("lt_clear_the_square"));
        f.Play_("lt_clear_the_square");
        Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.OpenGuard));
    }

    [Fact]
    public void Gatehouse_is_kept_when_an_attack_gets_nowhere_and_fails_when_nobody_attacks()
    {
        using (var f = F(Five("lt_gatehouse"), enemy: Biter, intent: BiteIntent))
        {
            f.Play_("lt_gatehouse");   // 8 Block against 6
            f.End();
            Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Authority));
            Assert.Equal(0, Stacks(f.Me, Promises.Gatehouse.Id));
        }
        using (var g = F(Five("lt_gatehouse")))
        {
            g.Play_("lt_gatehouse");
            g.End();
            Assert.Equal(0, Stacks(g.Me, LieutenantKeywords.Authority));
            Assert.Equal(0, Stacks(g.Me, Promises.Gatehouse.Id));
        }
    }

    [Fact]
    public void Last_bell_wants_the_turn_to_end_on_empty()
    {
        using (var f = F(Five("lt_last_bell"), energy: 1))
        {
            f.Play_("lt_last_bell");
            f.End();
            Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Authority));
        }
        using (var g = F(Five("lt_last_bell"), energy: 3))
        {
            g.Play_("lt_last_bell");
            g.End();
            Assert.Equal(0, Stacks(g.Me, LieutenantKeywords.Authority));
        }
    }

    [Fact]
    public void Scabbard_breaks_on_the_next_deed()
    {
        using var f = F(Five("lt_scabbard", "lt_watch_cut"));
        f.Play_("lt_scabbard");
        f.Play_("lt_watch_cut");
        Assert.Equal(0, Stacks(f.Me, Promises.Scabbard.Id));
        Assert.True(Counter(f.Me, Obligations.OrderFailedRound) > 0);
    }

    [Fact]
    public void Marked_man_is_kept_by_the_two_hits_of_one_pike_butt_and_witness_counts_one_hit_twice()
    {
        using (var f = F(Five("lt_marked_man", "lt_pike_butt")))
        {
            f.Play_("lt_marked_man");
            Assert.Equal(1, Stacks(f.It, Promises.MarkedMan.Id));
            f.Play_("lt_pike_butt");
            Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Authority));
        }
        using (var g = F(Five("lt_marked_man", "lt_witness", "lt_watch_cut")))
        {
            g.Play_("lt_marked_man");
            g.Play_("lt_witness");
            g.Play_("lt_watch_cut");
            Assert.Equal(1, Stacks(g.Me, LieutenantKeywords.Authority));
            Assert.Equal(0, Stacks(g.Me, LieutenantRules.Witness));
        }
    }

    [Fact]
    public void Braced_pike_pays_authority_and_strength_when_the_attack_takes_nothing()
    {
        using (var f = F(Five("lt_braced_pike"), enemy: Biter, intent: BiteIntent))
        {
            f.Play_("lt_braced_pike");
            Assert.Equal(1, Stacks(f.It, Promises.BracedPike.Id));
            f.End();
            Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Authority));
            Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Strength));
        }
        // Against an enemy that does not mean to attack, nothing is issued.
        using var g = F(Five("lt_braced_pike"));
        g.Play_("lt_braced_pike");
        Assert.Equal(0, Stacks(g.It, Promises.BracedPike.Id));
    }

    [Fact]
    public void Braced_pike_fails_when_the_attack_gets_through()
    {
        using var f = F(Five("lt_braced_pike", "lt_new_stripes"), enemy: Biter, intent: BiteIntent);
        f.Play_("lt_new_stripes", choose: 0);   // the Challenge it issues is replaced by Braced Pike's
        f.Play_("lt_braced_pike");   // 7 Block against 6 + 2 (Open Guard)
        f.End();
        Assert.Equal(0, Stacks(f.Me, LieutenantKeywords.Authority));
        Assert.Equal(0, Stacks(f.It, Promises.BracedPike.Id));
    }

    [Fact]
    public void Arrest_is_kept_by_the_kill_and_draws_a_card()
    {
        // Two bodies, so the fight goes on after the kill: 24 HP, felled by 6 + 2, then two Watch Cuts of
        // 6 + 2 (Challenged) + 2 Strength.
        var probe = FightProbe.RosterAgainstHero("lt_arrest", 9, [(LieutenantKeywords.Strength, 2)],
            (Biter, BiteIntent, null), (Quiet, QuietIntent, null));
        var (play, session, _) = FightProbe.Start(probe, ["lt_arrest", "lt_watch_cut", "lt_watch_cut", "lt_buckler_up", "lt_buckler_up"],
            character: LieutenantCharacter.Id);
        var biter = Now(play).State.Combatants.First(c => c.DefinitionId.value == Biter).Id;
        LieutenantTests.Play(play, session, "lt_arrest", biter);
        Assert.Equal(1, Stacks(Body(play, biter), Promises.Arrest.Id));
        LieutenantTests.Play(play, session, "lt_watch_cut", biter);
        var hand = Now(play).Hand.Count;
        LieutenantTests.Play(play, session, "lt_watch_cut", biter);
        Assert.False(Body(play, biter).IsAlive);
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.Authority));
        Assert.Equal(1, Counter(Hero(play), Obligations.ChallengesWon));
        Assert.Equal(hand, Now(play).Hand.Count);   // one played, one drawn
        play.Dispose();
    }

    [Fact]
    public void Second_rank_spends_authority_to_make_the_next_deed_free()
    {
        using var f = F(Five("lt_second_rank", "lt_watch_cut", "lt_watch_cut"), hero: (LieutenantKeywords.Authority, 1));
        f.Play_("lt_second_rank", choose: 0);
        Assert.Equal(0, Stacks(f.Me, LieutenantKeywords.Authority));
        var energy = Energy(f);
        f.Play_("lt_watch_cut");
        Assert.Equal(energy, Energy(f));
        f.Play_("lt_watch_cut");
        Assert.Equal(energy - 1, Energy(f));
    }

    [Fact]
    public void Headlong_makes_two_deeds_cheaper_and_exposes_her()
    {
        using var f = F(Five("lt_headlong", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"));
        f.Play_("lt_headlong");
        Assert.Equal(2, Stacks(f.Me, LieutenantKeywords.OpenGuard));
        var energy = Energy(f);
        f.Play_("lt_watch_cut");
        f.Play_("lt_watch_cut");
        Assert.Equal(energy, Energy(f));
        f.Play_("lt_watch_cut");
        Assert.Equal(energy - 1, Energy(f));
    }

    [Fact]
    public void Whetstone_draws_after_strength_was_spent_this_turn()
    {
        using var f = F(["lt_armguard", "lt_whetstone", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_watch_cut", "lt_watch_cut"],
            hero: (LieutenantKeywords.Strength, 1));
        f.Play_("lt_armguard", choose: 0);
        Assert.Equal(0, Stacks(f.Me, LieutenantKeywords.Strength));
        Assert.Equal(4, Stacks(f.Me, Converter.Cards.Keywords.WardWax));
        var hand = Now(f.Play).Hand.Count;
        f.Play_("lt_whetstone");
        Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Strength));
        Assert.Equal(hand, Now(f.Play).Hand.Count);   // one played, one drawn
    }

    [Fact]
    public void Buckle_takes_its_open_guard_back_when_an_order_is_kept()
    {
        using var f = F(Five("lt_buckle", "lt_first_orders", "lt_watch_cut"));
        f.Play_("lt_buckle");
        Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.OpenGuard));
        f.Play_("lt_first_orders");
        f.Play_("lt_watch_cut");
        Assert.Equal(1, Stacks(f.Me, LieutenantKeywords.Authority));
        Assert.Equal(0, Stacks(f.Me, LieutenantKeywords.OpenGuard));
    }

    [Fact]
    public void Clenched_jaw_answers_a_blow_that_got_through()
    {
        using (var f = F(Five("lt_clenched_jaw"), enemy: Biter, intent: BiteIntent))
        {
            f.Play_("lt_clenched_jaw");   // nothing has hit her yet
            Assert.Equal(0, Stacks(f.Me, LieutenantKeywords.Strength));
        }
        using var g = F(Five("lt_clenched_jaw"), enemy: Biter, intent: BiteIntent);
        g.End();                          // 6 damage, no Block
        Assert.True(g.Me.Health.Current < g.Me.Health.Max);
        g.Play_("lt_clenched_jaw");
        Assert.Equal(1, Stacks(g.Me, LieutenantKeywords.Strength));
    }
}
