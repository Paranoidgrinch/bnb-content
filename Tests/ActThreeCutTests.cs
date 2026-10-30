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
    private static int Trespass(RunPlayback play) => FightProbe.StacksOf(Hero(play), ActThree.TrespassId);

    private const string Deed = "paper_cut";            // 1 Energy, Deed
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working
    private const string Heavy = "permit_a38";          // 2 Energy, Working

    // ── Road ──────────────────────────────────────────────────────────────────────────────────────────────

    // Switching quarry within a turn is the Hare's breach: the opening licence pays for it.
    [Fact]
    public void The_hare_files_when_you_change_your_quarry()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("quarry", energy: 9,
                ("permit_hare", "stamp_passage", 200), ("cairn_of_stray_paths", "brace_cairn", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var hare = Find(play, "permit_hare");
        var cairn = Find(play, "cairn_of_stray_paths");

        Play(play, session, Deed, hare);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, hare), ActThree.QuarryId));
        Assert.Equal(1, Licences(play));
        Play(play, session, Deed, cairn);
        Assert.Equal(0, Licences(play));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, cairn), ActThree.QuarryId));
        play.Dispose();
    }

    // A turn spent on one quarry is lawful, and the Hare stamps it with a licence.
    [Fact]
    public void A_lawful_turn_earns_a_licence_from_the_hare()
    {
        var (play, session, hare) = FightProbe.Start(
            FightProbe.Roster("permit", energy: 9, ("permit_hare", "stamp_passage", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);

        Play(play, session, Deed, hare);
        Play(play, session, Deed, hare);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(2, Licences(play));
        play.Dispose();
    }

    // The arrow names the other party; the Path is hard to hit until you go there.
    [Fact]
    public void The_paths_arrow_points_elsewhere_until_you_follow_it()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("arrow", energy: 9,
                ("elsewhere_path", "name_the_way", 200), ("permit_hare", "stamp_passage", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var path = Find(play, "elsewhere_path");
        var hare = Find(play, "permit_hare");

        Assert.Equal(1, FightProbe.StacksOf(Body(play, hare), ActThree.DestinationId));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, path), ActThree.ArrowPointsElsewhereId));
        Play(play, session, Deed, hare);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, path), ActThree.ArrowPointsElsewhereId));
        play.Dispose();
    }

    // Ignore the arrow and the Path is owed: the opening licence is spent refusing it.
    [Fact]
    public void Ending_the_turn_without_visiting_the_destination_is_a_trespass()
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

    // A Trespass that lands leaves a stone; a hit on the Cairn knocks it loose.
    [Fact]
    public void The_cairn_gathers_stones_from_trespass_and_loses_them_to_blows()
    {
        var (play, session, cairn) = FightProbe.Start(
            FightProbe.Roster("stones", energy: 9, ("cairn_of_stray_paths", "loose_stone", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);

        play.CombatDriver!.EndTurn(); // the opening licence refuses the first
        play.CombatDriver.EndTurn();  // the second lands
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, cairn), ActThree.DetourStoneId));
        Play(play, session, Deed, cairn);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, cairn), ActThree.DetourStoneId));
        play.Dispose();
    }

    // ── Hedgerows ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_hedges_sign_names_the_law_and_two_of_one_cost_break_it()
    {
        var (play, session, hedge) = FightProbe.Start(
            FightProbe.Roster("sign", energy: 9, ("reckoning_hedge", "close_the_hedge", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);

        Assert.Equal(1, FightProbe.StacksOf(Body(play, hedge), ActThree.SignSameCostId));
        Play(play, session, Deed, hedge);
        Assert.Equal(1, Licences(play));
        Play(play, session, Deed, hedge);
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // The first card of the turn belongs to the Tenant.
    [Fact]
    public void Opening_on_anyone_but_the_tenant_is_a_trespass()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("plot", energy: 9,
                ("hawthorn_tenant", "thorn_lease", 200), ("reckoning_hedge", "close_the_hedge", 200)),
            [.. Enumerable.Repeat(Heavy, 10)], health: 300);

        Play(play, session, Heavy, Find(play, "reckoning_hedge"));
        Assert.Equal(0, Licences(play));
        play.Dispose();
    }

    // The border stands on one party; striking past it is a Trespass, and the blow lands double.
    [Fact]
    public void The_border_doubles_the_blow_and_files_for_it()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("border", energy: 9,
                ("errant_boundary_stone", "settle_the_line", 200), ("reckoning_hedge", "close_the_hedge", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);

        var beyond = Enemies(play).Single(e => FightProbe.StacksOf(e, ActThree.BeyondTheBorderId) > 0);
        var before = beyond.Health.Current;
        Play(play, session, Deed, beyond.Id);
        Assert.Equal(0, Licences(play));
        var dealt = before - Body(play, beyond.Id).Health.Current;
        Assert.True(dealt >= 8, $"dealt {dealt}");
        play.Dispose();
    }

    // ── Tollwater ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_snails_shell_turns_each_turn()
    {
        var (play, session, snail) = FightProbe.Start(
            FightProbe.Roster("shell", energy: 9, ("charter_shell_snail", "shell_charter", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);

        Assert.Equal(1, FightProbe.StacksOf(Body(play, snail), ActThree.ShellEnergyId));
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, snail), ActThree.ShellDeedId));
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Body(play, snail), ActThree.ShellWorkingId));
        play.Dispose();
    }

    // ── Covenants ─────────────────────────────────────────────────────────────────────────────────────────

    // Her gift waits in your hand; accepted and left unused for two turns, it is a Trespass.
    [Fact]
    public void The_witchlings_gift_unused_for_two_turns_is_a_trespass()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("gift", energy: 9, ("roadside_witchling", "warm_kettle", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);

        Play(play, session, ActThree.WitchlingGiftCardId, null);
        play.CombatDriver!.SupplyOptionChoice([1]); // her charm
        Assert.Null(session.Error);
        Assert.Equal(ActThree.GiftPatienceTurns, FightProbe.StacksOf(Hero(play), ActThree.GiftOutstandingId));
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.GiftOutstandingId));
        Assert.Equal(0, Licences(play)); // the opening licence refused her grievance
        play.Dispose();
    }

    [Fact]
    public void The_cups_drink_is_energy_and_somebody_elses_claim()
    {
        var (play, session, cup) = FightProbe.Start(
            FightProbe.Roster("drink", energy: 3, ("crossroads_cup", "silver_rim", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);

        Play(play, session, Working, null);
        Play(play, session, ActThree.CrossroadsDrinkCardId, null);
        Assert.Equal(3, play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId)
            .Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, cup), ActThree.ClaimId));
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
