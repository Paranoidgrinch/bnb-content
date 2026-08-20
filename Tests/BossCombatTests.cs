using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act-I bosses through the real host path. Each test drives the ACTUAL authored boss encounter, so a boss's
// whole machinery — its statuses, its intent rules, the encounter passives that carry its systems — has to be
// wired exactly as the game ships it.
public class BossCombatTests
{
    // ── The Deputy Undersecretary ─────────────────────────────────────────────

    // The Desk files one Matter per turn while it has room, and each Matter opens at Due 2.
    [Fact]
    public void The_desk_files_a_matter_each_turn_up_to_three()
    {
        var (play, session, _) = Deputy();

        // The Desk opens with two Matters, both at Due 2.
        Assert.Equal(2, Open(play));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.Matters[0].StatusId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.Matters[1].StatusId));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        // A third arrives while the first two age — the Desk is full.
        Assert.Equal(DeputyUndersecretary.DeskCapacity, Open(play));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.Matters[0].StatusId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.Matters[2].StatusId));
    }

    // An unresolved Matter goes Overdue and leaves its category's Backlog on the Deputy.
    [Fact]
    public void An_overdue_matter_leaves_backlog()
    {
        var (play, session, deputyId) = Deputy();

        play.CombatDriver!.EndTurn(); // the two opening Matters age: Due 2 → 1
        play.CombatDriver.EndTurn();  // Due 1 → Overdue
        Assert.Null(session.Error);

        // Both opening Matters are Performance ones, and both lapsed: two Backlog in that category.
        Assert.Equal(2, Enemy(play, deputyId).GetCounter(DeputyUndersecretary.PerformanceBacklog));
        Assert.Equal(2, Enemy(play, deputyId).GetCounter(DeputyUndersecretary.BacklogTotalCounter));
        Assert.Equal(0, Enemy(play, deputyId).GetCounter(DeputyUndersecretary.ProceduralBacklog));
    }

    // Resolving a Matter clears it without Backlog: the Complaint asks for 12 damage in one turn.
    [Fact]
    public void A_resolved_matter_leaves_nothing_behind()
    {
        var (play, session, deputyId) = Deputy(Enumerable.Repeat("paper_cut", 12).ToList());

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.Matters[0].StatusId));
        for (var i = 0; i < 2; i++) // 12 damage exactly
            Cut(play, session, deputyId);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.Matters[0].StatusId));
        Assert.Equal(0, Enemy(play, deputyId).GetCounter(DeputyUndersecretary.BacklogTotalCounter));
    }

    // Four Backlog fill the Desk: the Deputy telegraphs the declaration, hands it down, and Executive
    // Disposition begins with one File per Backlog category.
    [Fact]
    public void A_full_desk_declares_the_matter_urgent_and_opens_the_executive_files()
    {
        var (play, session, deputyId) = Deputy();

        // Six quiet turns let four Matters lapse (the Desk refills as slots free up).
        for (var i = 0; i < 6 && Enemy(play, deputyId).GetCounter(DeputyUndersecretary.BacklogTotalCounter) < 4; i++)
            play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.True(Enemy(play, deputyId).GetCounter(DeputyUndersecretary.BacklogTotalCounter) >= 4);

        // The declaration is telegraphed at the Deputy's turn start and resolved in the same turn.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.ExecutiveId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.DeskFullId));

        // Phase II: the Desk is gone from the player's side …
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), DeputyUndersecretary.RoutineId));
        Assert.All(DeputyUndersecretary.Matters,
            m => Assert.Equal(0, FightProbe.StacksOf(Hero(play), m.StatusId)));

        // … and every Backlog category that was recorded is now an Executive File.
        var files = FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.FileComplaintId)
            + FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.FileDelayId)
            + FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.FileDefectiveId);
        Assert.True(files >= 1, "the Backlog must survive as Executive Files");
    }

    // The Request for Additional Review comes with its own action: a card that costs the Energy the design
    // asks for. Playing it closes the Matter; ignoring it lets the review lapse into Expenditure Backlog.
    [Fact]
    public void Filing_the_request_resolves_the_review()
    {
        var (play, session, deputyId) = Deputy();

        play.CombatDriver!.EndTurn(); // the Desk moves on to the Review and the Missing Response
        Assert.Null(session.Error);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "matter_review"));

        var request = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == DeputyUndersecretary.ReviewCardId);
        play.CombatDriver.PlayCard(request.Id, deputyId);
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "matter_review"));

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, Enemy(play, deputyId).GetCounter(DeputyUndersecretary.ExpenditureBacklog));
    }

    // In Executive Disposition the Files pay out: an Unanswered Complaint hardens the Deputy every turn.
    [Fact]
    public void An_executive_file_guards_the_deputy_every_turn()
    {
        var (play, session, deputyId) = Deputy();

        for (var i = 0; i < 8 && FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.ExecutiveId) == 0; i++)
            play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.ExecutiveId));

        var intensity = FightProbe.StacksOf(Enemy(play, deputyId), DeputyUndersecretary.FileComplaintId);
        Assert.True(intensity > 0, "the lapsed Performance Matters must have become an Unanswered Complaint");

        // The Deputy opens its next turn behind 4 Block per intensity — visible when the turn comes back.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.True(BlockOf(Enemy(play, deputyId)) >= 4 * Math.Min(intensity, DeputyUndersecretary.FileIntensityMaximum),
            "the Unanswered Complaint must guard its author");
    }

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId DeputyId) Deputy(
        IReadOnlyList<string>? deck = null) =>
        FightProbe.Start(FightProbe.Authored("city_boss_01"), deck, health: 400);

    private static void Cut(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static int Open(RunPlayback play) => Hero(play).GetCounter(DeputyUndersecretary.OpenMattersCounter);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id == play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);
}
