using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Lieutenant in live fights of the real document (lieutenant_master.md): her start, Authority, Open Guard, and
// the life of an Order and a Challenge. The numbers are the canon's BALANCE DRAFTS.
public class LieutenantTests
{
    internal const string Quiet = "ordinance_tablet";
    internal const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero
    internal const string Biter = "embossed_seal";           // 6 damage, one hit
    internal const string BiteIntent = "heavy_impression";
    internal const string Twice = "levy_constable";          // 5 damage, twice
    internal const string TwiceIntent = "double_assessment";

    internal static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Fight(
        string[] deck, int energy = 9, int? health = null, string enemy = Quiet, string intent = QuietIntent,
        params (string Status, int Stacks)[] hero) =>
        FightProbe.Start(FightProbe.SoloAgainstHero(enemy, intent, energy, hero), deck, health,
            character: LieutenantCharacter.Id);

    internal static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;
    internal static CombatantState Hero(RunPlayback play) => Now(play).State.GetCombatant(Now(play).HeroId);
    internal static CombatantState Body(RunPlayback play, CombatantId id) => Now(play).State.GetCombatant(id);
    internal static int Stacks(CombatantState c, string status) => FightProbe.StacksOf(c, status);
    internal static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;
    internal static int Counter(CombatantState c, string counter) => c.GetCounter(new CounterId(counter));

    // Play the first card of this kind in hand; `choose` answers the option the card asks, if it asks one.
    internal static void Play(RunPlayback play, InteractiveRunSession session, string card, CombatantId target, int? choose = null)
    {
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == card).Id, target);
        if (choose is { } option)
        {
            Assert.NotNull(play.CombatDriver.PendingOptionChoice);
            play.CombatDriver.SupplyOptionChoice([option]);
        }
        Assert.Null(session.Error);
    }

    internal static void EndTurn(RunPlayback play, InteractiveRunSession session)
    {
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
    }

    [Fact]
    public void She_starts_with_eighty_health_ten_cards_and_her_command()
    {
        var start = LieutenantCharacter.Start;
        Assert.Equal(80, start.MaxHealth);
        Assert.Equal(10, start.Deck.Count);
        Assert.Equal(4, start.Deck.Count(c => c.value == "lt_watch_cut"));
        Assert.Equal(4, start.Deck.Count(c => c.value == "lt_buckler_up"));
        Assert.Single(start.Deck, c => c.value == "lt_first_orders");
        Assert.Single(start.Deck, c => c.value == "lt_new_stripes");

        var (play, _, _) = Fight(["lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"]);
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.Command));
        Assert.Equal(0, Stacks(Hero(play), LieutenantKeywords.Authority));
        play.Dispose();
    }

    [Fact]
    public void An_order_is_kept_by_a_later_deed_and_pays_one_authority()
    {
        var (play, session, enemy) = Fight(["lt_first_orders", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"]);
        Play(play, session, "lt_first_orders", enemy);
        Assert.Equal(1, Stacks(Hero(play), Promises.FirstOrders.Id));
        Assert.Equal(4, Block(Hero(play)));

        Play(play, session, "lt_watch_cut", enemy);
        Assert.Equal(0, Stacks(Hero(play), Promises.FirstOrders.Id));
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.Authority));
        Assert.Equal(1, Counter(Hero(play), Obligations.OrdersKept));
        play.Dispose();
    }

    [Fact]
    public void An_order_still_standing_when_the_turn_ends_has_failed()
    {
        var (play, session, enemy) = Fight(["lt_first_orders", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"]);
        Play(play, session, "lt_first_orders", enemy);
        Play(play, session, "lt_buckler_up", enemy);   // a Working is not a Deed
        Assert.Equal(1, Stacks(Hero(play), Promises.FirstOrders.Id));
        EndTurn(play, session);
        Assert.Equal(0, Stacks(Hero(play), Promises.FirstOrders.Id));
        Assert.Equal(0, Stacks(Hero(play), LieutenantKeywords.Authority));
        Assert.True(Counter(Hero(play), Obligations.OrderFailedRound) > 0);
        play.Dispose();
    }

    [Fact]
    public void A_second_order_replaces_the_first_without_failing_it()
    {
        var (play, session, enemy) = Fight(["lt_first_orders", "lt_first_orders+", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"]);
        Play(play, session, "lt_first_orders", enemy);
        Play(play, session, "lt_first_orders+", enemy);
        Assert.Equal(0, Stacks(Hero(play), Promises.FirstOrders.Id));
        Assert.Equal(1, Stacks(Hero(play), Promises.FirstOrdersPlus.Id));
        Assert.Equal(0, Counter(Hero(play), Obligations.OrderFailedRound));
        play.Dispose();
    }

    [Fact]
    public void Authority_stops_at_six()
    {
        var (play, session, enemy) = Fight(["lt_first_orders", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"],
            hero: (LieutenantKeywords.Authority, 6));
        Play(play, session, "lt_first_orders", enemy);
        Play(play, session, "lt_watch_cut", enemy);
        Assert.Equal(1, Counter(Hero(play), Obligations.OrdersKept));
        Assert.Equal(6, Stacks(Hero(play), LieutenantKeywords.Authority));
        play.Dispose();
    }

    [Fact]
    public void New_stripes_challenges_the_target_and_a_later_deed_keeps_it()
    {
        var (play, session, enemy) = Fight(["lt_new_stripes", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"]);
        Play(play, session, "lt_new_stripes", enemy, choose: 0);
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.Strength));
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.OpenGuard));
        Assert.Equal(1, Stacks(Body(play, enemy), Promises.NewStripes.Id));

        var before = Body(play, enemy).Health.Current + Block(Body(play, enemy));
        Play(play, session, "lt_watch_cut", enemy);
        // 6, 2 more for the Challenge, 1 Strength — the Challenge is read as the hit lands, before it is kept.
        Assert.Equal(before - 9, Body(play, enemy).Health.Current + Block(Body(play, enemy)));
        Assert.Equal(0, Stacks(Body(play, enemy), Promises.NewStripes.Id));
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.Authority));
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.OpenGuard));   // only an enemy hit spends it
        play.Dispose();
    }

    [Fact]
    public void A_challenge_not_kept_before_the_enemy_acts_has_failed()
    {
        var (play, session, enemy) = Fight(["lt_new_stripes", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"]);
        Play(play, session, "lt_new_stripes", enemy, choose: 0);
        EndTurn(play, session);
        Assert.Equal(0, Stacks(Body(play, enemy), Promises.NewStripes.Id));
        Assert.Equal(0, Stacks(Hero(play), LieutenantKeywords.Authority));
        play.Dispose();
    }

    [Fact]
    public void Declining_new_stripes_dare_issues_nothing()
    {
        var (play, session, enemy) = Fight(["lt_new_stripes", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"]);
        Play(play, session, "lt_new_stripes", enemy, choose: 1);
        Assert.Equal(0, Stacks(Hero(play), LieutenantKeywords.OpenGuard));
        Assert.Equal(0, Stacks(Body(play, enemy), Promises.NewStripes.Id));
        play.Dispose();
    }

    // Open Guard: a quarter more a stack (at least 1) on the first enemy hit, before Block, and spent by it (LT-027).
    [Fact]
    public void Open_guard_adds_a_quarter_a_stack_to_the_first_enemy_hit_only()
    {
        int Lost(bool exposed)
        {
            var (play, session, enemy) = Fight(["lt_new_stripes", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"],
                enemy: Twice, intent: TwiceIntent);
            Play(play, session, "lt_new_stripes", enemy, choose: exposed ? 0 : 1);
            EndTurn(play, session);
            var hero = Hero(play);
            Assert.Equal(0, Stacks(hero, LieutenantKeywords.OpenGuard));
            var lost = hero.Health.Max - hero.Health.Current;
            if (exposed)
                Assert.Equal(1, Counter(hero, LieutenantKeywords.GuardExtra));
            play.Dispose();
            return lost;
        }

        Assert.Equal(Lost(exposed: false) + 1, Lost(exposed: true));
    }

    // Absorbed by Block, the extra damage costs no HP — and is recorded as none (§2.5).
    [Fact]
    public void Open_guard_behind_enough_block_costs_no_health_and_still_lapses()
    {
        var (play, session, enemy) = Fight(["lt_new_stripes", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"],
            enemy: Biter, intent: BiteIntent);
        Play(play, session, "lt_new_stripes", enemy, choose: 0);
        Play(play, session, "lt_buckler_up", enemy);   // 5 + 2 with Open Guard
        Play(play, session, "lt_buckler_up", enemy);
        EndTurn(play, session);
        var hero = Hero(play);
        Assert.Equal(hero.Health.Max, hero.Health.Current);
        Assert.Equal(0, Stacks(hero, LieutenantKeywords.OpenGuard));
        Assert.Equal(0, Counter(hero, LieutenantKeywords.GuardExtra));
        play.Dispose();
    }

    [Fact]
    public void Open_guard_no_enemy_hit_lapses_at_the_start_of_her_turn()
    {
        var (play, session, enemy) = Fight(["lt_new_stripes", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up", "lt_buckler_up"]);
        Play(play, session, "lt_new_stripes", enemy, choose: 0);
        EndTurn(play, session);
        Assert.Equal(0, Stacks(Hero(play), LieutenantKeywords.OpenGuard));
        play.Dispose();
    }
}
