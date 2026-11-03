using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-III content cut of 2026-10-29: every standard law reworked to sit on a chip, and the reworked elites
// and bosses.
public class ActThreeCutTests
{
    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static IEnumerable<CombatantState> Enemies(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current.HeroId && c.IsAlive);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int Licences(RunPlayback play) => FightProbe.StacksOf(Hero(play), ActThree.SafeConductId);

    private const string Deed = "paper_cut";            // 1 Energy, Deed
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working
    private const string Heavy = "permit_a38";          // 2 Energy, Working

    private static int Claims(RunPlayback play, CombatantId id) => FightProbe.StacksOf(Body(play, id), ActThree.ClaimId);

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Solo(
        string enemy, string intent, IReadOnlyList<string> deck, int energy = 9) =>
        FightProbe.Start(FightProbe.Roster(enemy, energy, (enemy, intent, 200)), deck, health: 300);

    // ── Road ──────────────────────────────────────────────────────────────────────────────────────────────

    // A Deed before any Working breaks the Hare's law (2026-11-03); the opening licence forgives the first.
    [Fact]
    public void A_deed_before_any_working_breaks_the_hares_law()
    {
        var (play, session, hare) = Solo("permit_hare", "stamp_passage", [.. Enumerable.Repeat(Deed, 10)]);
        Play(play, session, Deed, hare);
        Assert.Equal(0, Licences(play));
        Assert.Equal(0, Claims(play, hare));
        play.CombatDriver!.EndTurn();
        Play(play, session, Deed, hare);
        Assert.Equal(1, Claims(play, hare));
        // Once a turn: a second Deed breaks nothing more.
        Play(play, session, Deed, hare);
        Assert.Equal(1, Claims(play, hare));
        play.Dispose();
    }

    [Fact]
    public void A_working_first_keeps_the_hares_law()
    {
        var (play, session, hare) = Solo("permit_hare", "stamp_passage", [Working, Deed, Deed, Deed, Deed, .. Enumerable.Repeat(Deed, 5)]);
        Play(play, session, Working, null);
        Play(play, session, Deed, hare);
        Assert.Equal(1, Licences(play));
        play.Dispose();
    }

    // The arrow names the other party; the Path is hard to hit until you go there.
    [Fact]
    public void The_paths_arrow_points_elsewhere_until_you_follow_it()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("arrow", energy: 9,
                ("elsewhere_path", "name_the_way", 200), ("permit_hare", "stamp_passage", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);
        var path = Find(play, "elsewhere_path");
        var hare = Find(play, "permit_hare");

        Assert.Equal(1, FightProbe.StacksOf(Body(play, hare), ActThree.DestinationId));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, path), ActThree.ArrowPointsElsewhereId));
        play.Dispose();
    }

    // Ignore the arrow and the Path's law is broken: the opening licence is spent forgiving it.
    [Fact]
    public void Ending_the_turn_without_visiting_the_destination_breaks_the_paths_law()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("unvisited", energy: 9,
                ("elsewhere_path", "name_the_way", 200), ("permit_hare", "stamp_passage", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // Two hits knock two stones loose, and the Cairn throws both back (3 each as authored); a turn without hits it braces.
    [Fact]
    public void The_cairn_throws_back_a_stone_for_every_hit()
    {
        var (play, session, cairn) = Solo("cairn_of_stray_paths", "loose_stone", [.. Enumerable.Repeat(Deed, 10)]);
        Play(play, session, Deed, cairn);
        Play(play, session, Deed, cairn);
        Assert.Equal(2, FightProbe.StacksOf(Body(play, cairn), ActThree.DetourStoneId));
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(before - 2 * ActThree.StoneDamage, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, cairn), ActThree.DetourStoneId));
        play.CombatDriver.EndTurn();
        Assert.Equal(ActThree.CairnBrace, Body(play, cairn).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        play.Dispose();
    }

    // ── Hedgerows ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Two_of_one_cost_in_a_row_break_the_hedges_law()
    {
        var (play, session, hedge) = Solo("reckoning_hedge", "close_the_hedge", [.. Enumerable.Repeat(Deed, 10)]);
        Play(play, session, Deed, hedge);
        Assert.Equal(1, Licences(play));
        Play(play, session, Deed, hedge);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // A turn that puts no status on the Tenant breaks its law; one Strong Binder (Doubt) keeps it.
    [Theory]
    [InlineData("strong_binder", 1)]
    [InlineData(Deed, 0)]
    public void The_tenant_wants_a_status_from_you_every_turn(string card, int licences)
    {
        var (play, session, tenant) = Solo("hawthorn_tenant", "thorn_lease", [.. Enumerable.Repeat(card, 10)]);
        Play(play, session, card, tenant);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(licences, Licences(play));
        play.Dispose();
    }

    [Fact]
    public void The_border_doubles_the_blow_and_breaks_the_stones_law()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("border", energy: 9,
                ("errant_boundary_stone", "settle_the_line", 200), ("permit_hare", "stamp_passage", 200)),
            [Working, Deed, Deed, Deed, Deed, .. Enumerable.Repeat(Deed, 5)], health: 300);
        var beyond = Enemies(play).First(e => FightProbe.StacksOf(e, ActThree.BeyondTheBorderId) > 0);
        var health = beyond.Health.Current;
        Play(play, session, Working, null);
        Play(play, session, Deed, beyond.Id);
        Assert.Equal(health - 12, Body(play, beyond.Id).Health.Current);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // ── Tollwater ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_third_card_breaks_the_snails_law()
    {
        var (play, session, snail) = Solo("charter_shell_snail", "shell_charter", [.. Enumerable.Repeat(Working, 10)]);
        Play(play, session, Working, null);
        Play(play, session, Working, null);
        Assert.Equal(1, Licences(play));
        Play(play, session, Working, null);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // Three turns kept — guarding (no Deeds), striking (Block 10 or less), guarding — and the fish leaves, defeated.
    [Fact]
    public void Keeping_the_fishs_law_three_turns_running_sends_it_away()
    {
        var (play, session, fish) = Solo("streamside_oath_fish", "river_shelter", [.. Enumerable.Repeat(Deed, 10)]);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, fish), ActThree.FishNoDeedsId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Body(play, fish), ActThree.OathKeptId));
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(2, FightProbe.StacksOf(Body(play, fish), ActThree.OathKeptId));
        play.CombatDriver.EndTurn();
        Assert.True(play.CombatDriver.Current is null || Body(play, fish).Health.Current <= 0);
        play.Dispose();
    }

    [Fact]
    public void A_deed_while_the_fish_guards_breaks_its_law_and_the_oath()
    {
        var (play, session, fish) = Solo("streamside_oath_fish", "river_shelter", [.. Enumerable.Repeat(Deed, 10)]);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, fish), ActThree.FishNoDeedsId));
        Play(play, session, Deed, fish);
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Body(play, fish), ActThree.OathKeptId));
        play.Dispose();
    }

    // The Ford's pressure intent: the first breach is forgiven, the second is a Claim — and every Claim is 6 Block.
    [Fact]
    public void Every_claim_the_ford_gains_is_six_block()
    {
        var (play, session, ford) = Solo("two_bank_toll_ford", "collect_both_banks", [.. Enumerable.Repeat(Working, 10)]);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, Claims(play, ford));
        Assert.Equal(ActThree.FordBlockPerClaim, Body(play, ford).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        play.Dispose();
    }

    // ── Covenants ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_witchlings_gift_left_in_hand_breaks_her_law()
    {
        var (play, session, _) = Solo("roadside_witchling", "warm_kettle", [.. Enumerable.Repeat(Working, 10)]);
        Assert.Single(play.CombatDriver!.Current!.Hand, c => ActThree.WitchlingGiftCards().Any(g => g.Id == c.DefinitionId.value));
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    [Fact]
    public void A_witchlings_gift_taken_keeps_her_law()
    {
        var (play, session, _) = Solo("roadside_witchling", "warm_kettle", [.. Enumerable.Repeat(Working, 10)]);
        var gift = play.CombatDriver!.Current!.Hand.First(c => ActThree.WitchlingGiftCards().Any(g => g.Id == c.DefinitionId.value));
        play.CombatDriver.PlayCard(gift.Id, null);
        if (play.CombatDriver.PendingCardChoice is { } offered)
            play.CombatDriver.SupplyCardChoice([offered[0].Id]);
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, Licences(play));
        play.Dispose();
    }

    [Fact]
    public void A_second_card_of_one_cost_breaks_the_brides_law()
    {
        var (play, session, bride) = Solo("blackthorn_bride", "veil_of_thorns", [.. Enumerable.Repeat(Deed, 10)]);
        Play(play, session, Deed, bride);
        Assert.Equal(1, Licences(play));
        Play(play, session, Deed, bride);
        Assert.Equal(0, Licences(play));
        // A new turn, a new set of prices.
        play.CombatDriver!.EndTurn();
        Play(play, session, Deed, bride);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, bride), ActThree.ClaimId));
        play.Dispose();
    }

    [Fact]
    public void The_cups_drink_is_energy_and_two_claims_for_the_cup()
    {
        var (play, session, cup) = FightProbe.Start(
            FightProbe.Roster("drink", energy: 3, ("crossroads_cup", "silver_rim", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);

        Play(play, session, Working, null);
        Play(play, session, ActThree.CrossroadsDrinkCardId, null);
        Assert.Equal(3, play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId)
            .Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(ActThree.CupClaims, FightProbe.StacksOf(Body(play, cup), ActThree.ClaimId));
        play.Dispose();
    }

    // ── bosses ────────────────────────────────────────────────────────────────────────────────────────────

    // The Hill forbids the kind of card you played most last turn.
    [Fact]
    public void The_hill_forbids_what_you_played_most()
    {
        var (play, session, hill) = FightProbe.Start(
            FightProbe.Roster("forbidden", energy: 9, ("the_answering_hill", "loose_earth", null)),
            [.. Enumerable.Repeat(Deed, 10)], health: 600);

        Play(play, session, Deed, hill);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, hill), ActThree.ForbiddenDeedId));
        play.Dispose();
    }
}
