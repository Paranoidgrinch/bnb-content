using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III's elites and bosses as reworked with the user on 2026-11-03: every rule a law, every broken law a Claim.
// Every fight opens with one Safe-Conduct, which forgives the first broken law — so "the licence is spent" is how
// these tests see a law broken without having to break it twice.
public class ActThreeElitesAndBossesTests
{
    private const string Deed = "paper_cut";            // 1 Energy, Deed, 6 damage
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working, 5 Block

    private static CombatantState Body(RunPlayback play, CombatantId id) => play.CombatDriver!.Current!.State.GetCombatant(id);
    private static CombatantState Hero(RunPlayback play) => Body(play, play.CombatDriver!.Current!.HeroId);
    private static int Licences(RunPlayback play) => FightProbe.StacksOf(Hero(play), ActThree.SafeConductId);
    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        string enemy, string intent, IReadOnlyList<string> deck, int? health = 200, int energy = 9) =>
        FightProbe.Start(FightProbe.Roster(enemy, energy, (enemy, intent, health)), deck, health: 400);

    private static void Play(RunPlayback play, InteractiveRunSession session, string card, CombatantId? target)
    {
        var instance = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == card);
        play.CombatDriver.PlayCard(instance.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── Stag ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void In_the_open_season_your_deeds_deal_half_again_and_in_the_closed_one_they_break_its_law()
    {
        var (play, session, stag) = Fight("stag_of_pre_approved_violence", "mark_the_verge", [.. Enumerable.Repeat(Deed, 12)]);
        Assert.True(FightProbe.StacksOf(Body(play, stag), ActThree.SeasonOpenId) > 0);
        var health = Body(play, stag).Health.Current;
        Play(play, session, Deed, stag);
        Assert.Equal(health - 9, Body(play, stag).Health.Current);
        Assert.Equal(1, Licences(play));
        play.CombatDriver!.EndTurn();
        Assert.True(FightProbe.StacksOf(Body(play, stag), ActThree.SeasonClosedId) > 0);
        Play(play, session, Deed, stag);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // ── Old Web ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_card_spins_a_thread_workings_cut_two_and_each_thread_hits_for_three()
    {
        var (play, session, web) = Fight("grandmother_web", "knot_an_older_promise", [.. Enumerable.Repeat(Deed, 12)]);
        Play(play, session, Deed, web);
        Play(play, session, Deed, web);
        Play(play, session, Deed, web);
        Assert.Equal(3, FightProbe.StacksOf(Body(play, web), ActThree.WebThreadId));
        var health = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(health - 3 * ActThree.WebThreadDamage, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, web), ActThree.WebThreadId));
        play.Dispose();
    }

    [Fact]
    public void A_working_cuts_two_threads()
    {
        var (play, session, web) = Fight("grandmother_web", "knot_an_older_promise", [.. Enumerable.Repeat(Working, 12)]);
        Play(play, session, Working, null);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, web), ActThree.WebThreadId));
        play.Dispose();
    }

    // ── Wrong Bridge ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_first_hit_is_the_toll_and_past_half_the_first_hit_is_double()
    {
        var (play, session, bridge) = Fight("the_wrong_bridge_in_person", "raise_the_tollgate", [.. Enumerable.Repeat(Deed, 12)], health: 20);
        Play(play, session, Deed, bridge);          // 20 → 14: the toll, forgiven by the licence
        Assert.Equal(0, Licences(play));
        Play(play, session, Deed, bridge);          // 14 → 8: past half, it turns
        Assert.True(FightProbe.StacksOf(Body(play, bridge), ActThree.TheOtherBankId) > 0);
        play.CombatDriver!.EndTurn();
        var block = Block(Body(play, bridge));
        var health = Body(play, bridge).Health.Current;
        Play(play, session, Deed, bridge);
        Assert.Equal(health - Math.Max(0, 12 - block), Body(play, bridge).Health.Current);
        play.Dispose();
    }

    // ── Great Toll Frog ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_frog_swallows_five_gold_a_turn_and_grows_heavy_with_it()
    {
        var (play, _, frog) = Fight("great_toll_frog", "swallow_the_offering", [.. Enumerable.Repeat(Working, 12)]);
        play.CombatDriver!.EndTurn();
        Assert.Equal(5, FightProbe.StacksOf(Body(play, frog), ActThree.SwallowedGoldId));
        play.CombatDriver.EndTurn();
        Assert.Equal(10, Hero(play).Counters[ActThree.FrogBellyCounter]);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, frog), ActThree.TollWeightId));
        play.Dispose();
    }

    [Fact]
    public void Twenty_five_in_one_turn_and_it_spits_everything_back()
    {
        var (play, session, frog) = Fight("great_toll_frog", "tongue_of_collection", [.. Enumerable.Repeat(Deed, 12)]);
        play.CombatDriver!.EndTurn();
        Assert.Equal(5, Hero(play).Counters[ActThree.FrogBellyCounter]);
        for (var i = 0; i < 5; i++)
            Play(play, session, Deed, frog);
        Assert.Equal(0, Hero(play).Counters[ActThree.FrogBellyCounter]);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, frog), ActThree.SwallowedGoldId));
        play.Dispose();
    }

    // ── Ant Queen ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_queen_takes_half_while_her_line_stands_and_out_of_order_breaks_her_law()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_elite_ant_queen_of_the_proper_line", energy: 9),
            [.. Enumerable.Repeat(Deed, 12)], health: 400);
        var queen = Find(play, "ant_queen_of_the_proper_line");
        var second = Find(play, "second_line_bearer");
        var health = Body(play, queen).Health.Current;
        Play(play, session, Deed, queen);
        Assert.Equal(health - 3, Body(play, queen).Health.Current);
        Assert.Equal(1, Licences(play));
        Play(play, session, Deed, second);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // ── Surveyor ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Breaking_the_current_law_breaks_its_law_and_the_former_one_is_an_old_right()
    {
        var (play, session, surveyor) = Fight("surveyor_of_forgotten_paths", "drive_the_first_stake", [.. Enumerable.Repeat(Deed, 12)]);
        // Round 1: Deed First is Current — and four cards is the Former law.
        Play(play, session, Deed, surveyor);
        Assert.Equal(0, Licences(play));
        Play(play, session, Deed, surveyor);
        Play(play, session, Deed, surveyor);
        Play(play, session, Deed, surveyor);
        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.OldRightId) > 0);
        play.Dispose();
    }

    // ── Magistrate ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_judgment_kept_costs_the_magistrate_eight()
    {
        var (play, session, magistrate) = Fight("magistrate_of_thorns", "establish_the_judgment", [.. Enumerable.Repeat(Working, 12)]);
        play.CombatDriver!.SupplyOptionChoice([0]); // no Deeds
        Play(play, session, Working, null);
        var health = Body(play, magistrate).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(health - ActThree.JudgmentKeptHealth, Body(play, magistrate).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void A_judgment_broken_is_a_claim_and_ten_block()
    {
        var (play, session, magistrate) = Fight("magistrate_of_thorns", "hear_the_trespass", [.. Enumerable.Repeat(Working, 12)]);
        play.CombatDriver!.SupplyOptionChoice([1]); // no Workings
        Play(play, session, Working, null);
        Assert.Equal(0, Licences(play));
        Assert.Equal(ActThree.JudgmentBrokenBlock, Block(Body(play, magistrate)));
        play.Dispose();
    }

    // ── Ombudsman ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_road_and_the_root_take_turns()
    {
        var (play, session, ombudsman) = Fight("ombudsman_of_root_and_road", "hear_both_parties", [.. Enumerable.Repeat(Deed, 12)]);
        Play(play, session, Deed, ombudsman);
        Assert.Equal(1, Licences(play));
        Play(play, session, Deed, ombudsman);       // the Road: two of one cost
        Assert.Equal(0, Licences(play));
        Assert.True(FightProbe.StacksOf(Body(play, ombudsman), ActThree.RoadHeardId) > 0);
        play.CombatDriver!.EndTurn();
        Assert.True(FightProbe.StacksOf(Body(play, ombudsman), ActThree.RootHeardId) > 0);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, ombudsman), ActThree.RoadHeardId));
        play.Dispose();
    }

    // ── Notary ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_law_broken_under_the_notary_is_written_into_the_wood()
    {
        var (play, session, notary) = Fight("notary_of_old_growth", "seal_it_in_sap", [.. Enumerable.Repeat(Deed, 12)]);
        Play(play, session, Deed, notary);          // round 1 reads Deed First
        Assert.Equal(0, Licences(play));
        Assert.True(FightProbe.StacksOf(Body(play, notary), "notary_ring_deed_first") > 0);
        play.Dispose();
    }

    // ── Grandmother ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Her_gift_played_costs_her_six_and_her_gift_kept_breaks_her_law()
    {
        var (play, session, grandmother) = Fight("grandmother_clause", "ask_after_your_health", [.. Enumerable.Repeat(Working, 12)]);
        var gift = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value is "thorned_shawl" or "heavy_pie" or "lavender_sachet");
        var health = Body(play, grandmother).Health.Current;
        play.CombatDriver.PlayCard(gift.Id, null);
        while (play.CombatDriver.PendingCardChoice is { } offered)
            play.CombatDriver.SupplyCardChoice([offered[0].Id]);
        Assert.Equal(health - ActThree.GiftPlayedHealth, Body(play, grandmother).Health.Current);
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Licences(play));
        play.CombatDriver.EndTurn();                // the second gift, left in hand
        Assert.Null(session.Error);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // ── Queen Under the Hill ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_first_card_that_is_not_a_working_breaks_her_law()
    {
        var (play, session, queen) = Fight("queen_under_the_hill", "count_every_buried_name", [.. Enumerable.Repeat(Deed, 12)]);
        Play(play, session, Deed, queen);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    [Fact]
    public void Three_turns_of_her_law_kept_and_she_sits_out_a_turn()
    {
        var (play, _, queen) = Fight("queen_under_the_hill", "open_the_hill_registry", [.. Enumerable.Repeat(Working, 12)]);
        for (var turn = 0; turn < 3; turn++)
        {
            play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, null);
            if (turn < 2)
                play.CombatDriver.EndTurn();
        }
        var health = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();                // the third turn kept: she is satisfied, and sits this one out
        Assert.Equal(health, Hero(play).Health.Current);
        play.Dispose();
    }
}
