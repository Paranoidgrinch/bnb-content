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

    // ── The Queue Commissioner ────────────────────────────────────────────────

    // The queue moves one place toward the Counter each turn; reaching it opens a Service Window that strips
    // the Commissioner's guard, opens him up, and sends the player back into the line afterwards.
    [Fact]
    public void The_queue_advances_and_the_counter_serves_the_player()
    {
        var (play, session, bossId) = Commissioner(Enumerable.Repeat("paper_cut", 20).ToList());

        Assert.Equal(QueueCommissioner.StartPosition, Position(play));

        // The queue starts moving once the Commissioner has had its turn.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(2, Position(play));

        // Sooner or later the Counter is reached — Reorder the Line can push the player back on the way.
        for (var i = 0; i < 8 && FightProbe.StacksOf(Hero(play), QueueCommissioner.ServiceId) == 0; i++)
            play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        // At the Counter: no position left, the Window open, the Commissioner exposed.
        Assert.Equal(0, Position(play));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), QueueCommissioner.ServiceId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, bossId), QueueCommissioner.BeingServedId));

        // 25 % more, after whatever Doubt the queue has piled on the player (one stack, −25 %, spends itself):
        // a 6-damage Paper Cut lands as 7, or as 5 through Doubt.
        var doubt = FightProbe.StacksOf(Hero(play), "doubt");
        var expected = (doubt > 0 ? 6 * 75 / 100 : 6) * 125 / 100;
        var before = Enemy(play, bossId).Health.Current;
        Cut(play, session, bossId);
        Assert.Equal(before - expected, Enemy(play, bossId).Health.Current);

        // The Window lasts one turn; afterwards the player is back in the queue and it counts as served.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), QueueCommissioner.ServiceId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, bossId), QueueCommissioner.BeingServedId));
        Assert.True(Position(play) > 0, "the player rejoins the queue after being served");
        Assert.Equal(1, Enemy(play, bossId).GetCounter(QueueCommissioner.ServicesCounter));
    }

    // The Administrative Choice is dealt as two offers, and only one of them counts per turn.
    [Fact]
    public void Only_one_administrative_choice_counts_per_turn()
    {
        var (play, session, _) = Commissioner();

        var hand = play.CombatDriver!.Current!.Hand;
        Assert.Contains(hand, c => c.DefinitionId.value == QueueCommissioner.PetitionCardId);
        Assert.Contains(hand, c => c.DefinitionId.value == QueueCommissioner.YieldCardId);

        Play(play, session, QueueCommissioner.YieldCardId);
        Assert.Equal(QueueCommissioner.BackOfQueue, Position(play));

        // The second offer is refused: one step per turn.
        Play(play, session, QueueCommissioner.PetitionCardId);
        Assert.Equal(QueueCommissioner.BackOfQueue, Position(play));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "paperwork"));
    }

    // Hitting the Commissioner hard enough in one turn earns Priority, which then eats the next push-back.
    [Fact]
    public void Priority_is_earned_by_pressure_and_spent_on_the_next_push_back()
    {
        var (play, session, bossId) = Commissioner(Enumerable.Repeat("paper_cut", 20).ToList(), energy: 9);

        for (var i = 0; i < 3; i++) // 18 damage: past the 14 the Commissioner has to suffer
            Cut(play, session, bossId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), QueueCommissioner.PriorityId));

        play.CombatDriver!.EndTurn(); // Next, Please — the queue simply advances
        Assert.Equal(2, Position(play));

        // Reorder the Line pushes the player back — Priority spends itself instead, and only the ordinary
        // advance remains.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), QueueCommissioner.PriorityId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), QueueCommissioner.PushedBackId));
        Assert.Equal(1, Position(play));
    }

    // Two served Windows open the Counter of Final Appeal: the transition is telegraphed, handed down, and
    // leaves the player in a shorter queue.
    [Fact]
    public void Two_service_windows_open_the_counter_of_final_appeal()
    {
        var (play, session, bossId) = Commissioner();

        for (var i = 0; i < 12 && FightProbe.StacksOf(Enemy(play, bossId), QueueCommissioner.PriorityQueueId) == 0; i++)
            play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, bossId), QueueCommissioner.PriorityQueueId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, bossId), QueueCommissioner.FinalCounterId));
        Assert.True(Enemy(play, bossId).GetCounter(QueueCommissioner.ServicesCounter) >= 2
            || Enemy(play, bossId).Health.Current <= 60);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId BossId) Commissioner(
        IReadOnlyList<string>? deck = null, int? energy = null) =>
        FightProbe.Start(FightProbe.Authored("city_boss_02", energy), deck, health: 400);

    private static int Position(RunPlayback play) =>
        FightProbe.StacksOf(Hero(play), QueueCommissioner.PositionId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, play.CombatDriver.Current!.State.Combatants
            .First(c => c.Id != play.CombatDriver.Current!.HeroId).Id);
        Assert.Null(session.Error);
    }

    // ── The Lord Sealkeeper ───────────────────────────────────────────────────

    // Three Seals raise a 16-Block Ward every player turn; the Seal of Access is 4 of it.
    [Fact]
    public void The_seal_ward_rises_with_every_standing_seal()
    {
        var (play, _, keeperId) = Sealkeeper();

        Assert.Equal(16, BlockOf(Enemy(play, keeperId)));
        Assert.All(LordSealkeeper.Seals,
            seal => Assert.Equal(1, FightProbe.StacksOf(Enemy(play, keeperId), seal.SealId)));
    }

    // Strip the Ward, draw blood, and a Seal may be broken — the player chooses which, and keeps its Fragment.
    [Fact]
    public void Breaking_a_seal_cracks_the_keeper_and_leaves_a_fragment()
    {
        var (play, session, keeperId) = Sealkeeper(Enumerable.Repeat("paper_cut", 30).ToList(), energy: 9);

        // 16 Block, then blood: the offer only appears once the Ward is gone. Two Paper Cuts are 12 — not
        // enough; the third goes through it.
        for (var i = 0; i < 2; i++)
            Cut(play, session, keeperId);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), LordSealkeeper.BreakReadyId));
        Cut(play, session, keeperId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), LordSealkeeper.BreakReadyId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, keeperId), LordSealkeeper.CrackedId));

        // Three offers on the table; taking one shatters that Seal and hands over its Fragment.
        var access = LordSealkeeper.Seals[0];
        Assert.All(LordSealkeeper.Seals,
            seal => Assert.Contains(play.CombatDriver!.Current!.Hand, c => c.DefinitionId.value == seal.BreakCardId));

        Play(play, session, access.BreakCardId);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, keeperId), access.SealId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, keeperId), access.OutstandingId));
        Assert.Contains(play.CombatDriver!.Current!.Hand, c => c.DefinitionId.value == access.FragmentCardId);

        // Only one Seal per turn: a second offer does nothing.
        Play(play, session, LordSealkeeper.Seals[1].BreakCardId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, keeperId), LordSealkeeper.Seals[1].SealId));

        // The Fragment of Access takes 12 Block off the Keeper — and is a one-use piece of stolen authority.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        var guarded = BlockOf(Enemy(play, keeperId));
        Assert.True(guarded > 0, "the Ward rises again on the surviving Seals");
        Play(play, session, access.FragmentCardId);
        Assert.Equal(Math.Max(0, guarded - 12), BlockOf(Enemy(play, keeperId)));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, keeperId), access.OutstandingId));
    }

    // A Fragment stays in hand across turns: it is the boss's authority, not a card of the deck.
    [Fact]
    public void A_fragment_is_kept_until_it_is_spent()
    {
        var (play, session, keeperId) = Sealkeeper(Enumerable.Repeat("paper_cut", 30).ToList(), energy: 9);

        for (var i = 0; i < 4; i++)
            Cut(play, session, keeperId);
        Play(play, session, LordSealkeeper.Seals[2].BreakCardId); // Execution

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Contains(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == LordSealkeeper.Seals[2].FragmentCardId);
    }

    // Wearing the Keeper down unseals it: the Seals shatter, the Ward falls, and Phase II begins.
    [Fact]
    public void The_keeper_unseals_itself_when_the_seals_are_gone()
    {
        var (play, session, keeperId) = Sealkeeper(Enumerable.Repeat("paper_cut", 60).ToList(), energy: 9);

        for (var turn = 0; turn < 12 && FightProbe.StacksOf(Enemy(play, keeperId), LordSealkeeper.UnsealedId) == 0; turn++)
        {
            while (play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == "paper_cut")
                   && Hero(play).Resources[StandardCombatIds.EnergyResource].Current > 0)
                Cut(play, session, keeperId);

            // Whatever Seal is offered, take it.
            var offer = play.CombatDriver.Current!.Hand
                .FirstOrDefault(c => LordSealkeeper.Seals.Any(s => s.BreakCardId == c.DefinitionId.value));
            if (offer is not null)
                play.CombatDriver.PlayCard(offer.Id, keeperId);

            play.CombatDriver.EndTurn();
            Assert.Null(session.Error);
        }

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, keeperId), LordSealkeeper.UnsealedId));
        Assert.All(LordSealkeeper.Seals,
            seal => Assert.Equal(0, FightProbe.StacksOf(Enemy(play, keeperId), seal.SealId)));
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId KeeperId) Sealkeeper(
        IReadOnlyList<string>? deck = null, int? energy = null) =>
        FightProbe.Start(FightProbe.Authored("city_boss_03", energy), deck, health: 400);

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
