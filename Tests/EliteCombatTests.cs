using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-I elites, driven in live fights like the standard identities.
public class EliteCombatTests
{
    // The Three Appointments: each body counts down on its own and resolves ITS consequence at zero. The First
    // is armed with 2 at its first turn start and steps once per turn end; "Called Too Early" opens its cycle,
    // so the countdown runs out at the end of its second turn.
    [Fact]
    public void The_first_appointment_comes_due_on_schedule_and_then_falls_silent()
    {
        var probe = FightProbe.Roster("appointments", ("first_appointment", "called_too_early", 24));
        var (play, session, appointmentId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn(); // armed to 2, steps to 1
        Assert.Null(session.Error);
        Assert.Equal(1, Enemy(play, appointmentId).GetCounter(PassiveStatuses.AppointmentDueCounter));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn(); // 6 from Called Too Early, then the expiry: 7 damage + 1 Fatigue
        Assert.Null(session.Error);
        Assert.Equal(before - 13, Hero(play).Health.Current);
        Assert.Equal(0, Enemy(play, appointmentId).GetCounter(PassiveStatuses.AppointmentDueCounter));

        // Spent: nothing is due any more, so the next round only carries the intent's own damage.
        var quiet = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(quiet - 6, Hero(play).Health.Current);
    }

    // A scheduling move brings its own date forward AND marks the encounter, so no second Appointment moves a
    // date in the same round (the design's anti-spike rule) — the other body stands down to a safe move.
    [Fact]
    public void Only_one_appointment_moves_a_date_per_round()
    {
        var probe = FightProbe.Roster("appointments_rush",
            ("first_appointment", "move_up_the_date", 24),
            ("second_appointment", "move_up_the_review", 28));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var firstId = combat.State.Combatants.First(c => c.Id.value.StartsWith("first_appointment")).Id;
        var secondId = combat.State.Combatants.First(c => c.Id.value.StartsWith("second_appointment")).Id;

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        // The First moved its date (2 → 1 by the move, then 1 → expiry at its turn end, leaving 0); the Second
        // was blocked and only took its ordinary step: armed 3 → 2. The mark itself is already gone — it lasts
        // exactly the round it was set in.
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), PassiveStatuses.AppointmentsAcceleratedId));
        Assert.Equal(2, Enemy(play, secondId).GetCounter(PassiveStatuses.AppointmentDueCounter));
        Assert.Equal(0, Enemy(play, firstId).GetCounter(PassiveStatuses.AppointmentDueCounter));
    }

    // Reopening-Hours Monolith: while the office is CLOSED nothing the player does comes off its HP — it is
    // banked as Pending Business. Two closed windows later the office opens and processes the lot at once.
    // (The probe cycles Close Without Warning so the Monolith guards nothing: Block would soak the hits before
    // they ever reach the ledger, which is correct but hides the mechanic.)
    [Fact]
    public void The_monolith_banks_two_closed_windows_and_then_processes_them_at_once()
    {
        var probe = FightProbe.Solo("reopening_hours_monolith", "close_without_warning", energy: 9);
        var (play, session, monolithId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        for (var i = 0; i < 3; i++)
            Cut(play, session, monolithId);
        Assert.Equal(92, Enemy(play, monolithId).Health.Current);
        Assert.Equal(18, Enemy(play, monolithId).GetCounter(PassiveStatuses.PendingBusinessCounter));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(92, Enemy(play, monolithId).Health.Current); // still closed after one window
        Assert.Equal(0, Enemy(play, monolithId).GetCounter(PassiveStatuses.OfficeOpenCounter));

        for (var i = 0; i < 3; i++)
            Cut(play, session, monolithId);
        Assert.Equal(92, Enemy(play, monolithId).Health.Current);
        Assert.Equal(36, Enemy(play, monolithId).GetCounter(PassiveStatuses.PendingBusinessCounter));

        play.CombatDriver.EndTurn(); // the office opens and settles its backlog in one go
        Assert.Null(session.Error);
        Assert.Equal(1, Enemy(play, monolithId).GetCounter(PassiveStatuses.OfficeOpenCounter));
        Assert.Equal(0, Enemy(play, monolithId).GetCounter(PassiveStatuses.PendingBusinessCounter));
        Assert.Equal(92 - 36, Enemy(play, monolithId).Health.Current);
    }

    // …and during the open window damage lands the ordinary way, before the shutters come down again.
    [Fact]
    public void An_open_window_takes_damage_normally_and_then_closes_again()
    {
        var probe = FightProbe.Solo("reopening_hours_monolith", "close_without_warning", energy: 9);
        var (play, session, monolithId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        play.CombatDriver.EndTurn(); // two closed windows passed → open
        Assert.Null(session.Error);
        Assert.Equal(1, Enemy(play, monolithId).GetCounter(PassiveStatuses.OfficeOpenCounter));

        var before = Enemy(play, monolithId).Health.Current;
        Cut(play, session, monolithId);
        Assert.Equal(before - 6, Enemy(play, monolithId).Health.Current); // straight to HP
        Assert.Equal(0, Enemy(play, monolithId).GetCounter(PassiveStatuses.PendingBusinessCounter));

        play.CombatDriver.EndTurn(); // one open action, then closed again
        Assert.Null(session.Error);
        Assert.Equal(0, Enemy(play, monolithId).GetCounter(PassiveStatuses.OfficeOpenCounter));
    }

    // Devouring Waiting Room: Energy the player leaves unspent at the end of their turn becomes Lost Time on
    // the Room (max 3), and Time Eats Back cashes the whole ledger in at 7 + 4 each.
    [Fact]
    public void The_waiting_room_turns_unspent_energy_into_lost_time_and_eats_it_back()
    {
        var probe = FightProbe.Roster("lost_time", ("devouring_waiting_room", "walls_hold_still", 68));
        var (play, session, roomId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn(); // three Energy left untouched → three Lost Time (the cap)
        Assert.Null(session.Error);
        Assert.Equal(3, Enemy(play, roomId).GetCounter(PassiveStatuses.LostTimeCounter));

        play.CombatDriver.EndTurn(); // already at the maximum
        Assert.Null(session.Error);
        Assert.Equal(3, Enemy(play, roomId).GetCounter(PassiveStatuses.LostTimeCounter));
    }

    [Fact]
    public void Time_eats_back_cashes_the_whole_ledger_in_one_hit()
    {
        var probe = FightProbe.Roster("time_eats", ("devouring_waiting_room", "time_eats_back", 68));
        var (play, session, roomId) = FightProbe.Start(probe);

        // The unspent Energy is banked at the player's turn end — before the Room acts in the same step — so
        // the very first Time Eats Back already carries a full ledger: 7 + 4 × 3, and it clears it.
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(before - 19, Hero(play).Health.Current);
        Assert.Equal(0, Enemy(play, roomId).GetCounter(PassiveStatuses.LostTimeCounter));

        var loaded = Hero(play).Health.Current;
        play.CombatDriver.EndTurn(); // the next turn refills the ledger and it eats again
        Assert.Null(session.Error);
        Assert.Equal(loaded - 19, Hero(play).Health.Current);
    }

    // The Moth Cloud feeds the Room's ledger rather than its own — killing the Room is what erases the resource.
    [Fact]
    public void The_moth_cloud_steals_a_minute_for_the_room()
    {
        var probe = FightProbe.Roster("stolen_minutes",
            ("minute_moth_cloud", "steal_a_minute", 24),
            ("devouring_waiting_room", "walls_hold_still", 68));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        var combat = play.CombatDriver!.Current!;
        var roomId = combat.State.Combatants.First(c => c.Id.value.StartsWith("devouring_waiting_room")).Id;
        var mothId = combat.State.Combatants.First(c => c.Id.value.StartsWith("minute_moth_cloud")).Id;

        // Spend every point of Energy, so the only Lost Time that appears is the Moth's theft.
        for (var i = 0; i < 3; i++)
            Cut(play, session, mothId);
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, Enemy(play, roomId).GetCounter(PassiveStatuses.LostTimeCounter));
        Assert.Equal(0, Enemy(play, mothId).GetCounter(PassiveStatuses.LostTimeCounter));
    }

    // Living Petition Chorus: each player turn opens with a clause on the table — a card in hand. Playing it
    // SIGNS (benefit now, liability later); leaving it there REFUSES, and the Petition takes its consolation.
    [Fact]
    public void The_petition_lays_a_clause_in_hand_each_turn_and_signing_records_it()
    {
        var probe = FightProbe.Roster("petition", ("living_petition_chorus", "amended_aloud", 90));
        var (play, session, petitionId) = FightProbe.Start(probe);

        var clause = play.CombatDriver!.Current!.Hand
            .FirstOrDefault(c => c.DefinitionId.value == ClauseCards.All[0].CardId);
        Assert.NotNull(clause); // the Extension Clause opens the cycle

        play.CombatDriver.PlayCard(clause!.Id, petitionId); // SIGN
        Assert.Null(session.Error);
        Assert.Equal(1, Enemy(play, petitionId).GetCounter(PassiveStatuses.SignaturesCounter));
        Assert.Equal(1, Enemy(play, petitionId).GetCounter(ClauseCards.All[0].Liability));
        Assert.DoesNotContain(play.CombatDriver.Current!.Hand, c => c.DefinitionId.value == ClauseCards.All[0].CardId);

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        // The next turn opens with the NEXT clause, so one reading cycle never repeats a clause.
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.DefinitionId.value == ClauseCards.All[1].CardId);
    }

    [Fact]
    public void A_refused_clause_pays_the_petition_instead()
    {
        var probe = FightProbe.Roster("petition_refused", ("living_petition_chorus", "chorus_cut", 90));
        var (play, session, petitionId) = FightProbe.Start(probe);

        // Refuse the first clause (its consolation is Block, which the Petition's own turn start would wipe
        // before we could look) and the second one, whose consolation is a Strength that sticks.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.DefinitionId.value == ClauseCards.All[1].CardId);

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, Enemy(play, petitionId).GetCounter(PassiveStatuses.SignaturesCounter));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, petitionId), "strength"));
    }

    // Three signatures and the Petition reads the record: 8 damage plus every liability signed for.
    [Fact]
    public void Three_signatures_bring_the_record_reading()
    {
        var probe = FightProbe.Roster("petition_read", ("living_petition_chorus", "amended_aloud", 90));
        var (play, session, petitionId) = FightProbe.Start(probe);

        for (var turn = 0; turn < 3; turn++)
        {
            var clause = play.CombatDriver!.Current!.Hand
                .First(c => ClauseCards.All.Any(x => x.CardId == c.DefinitionId.value));
            play.CombatDriver.PlayCard(clause.Id, petitionId);
            Assert.Null(session.Error);
            if (turn < 2)
            {
                play.CombatDriver.EndTurn();
                Assert.Null(session.Error);
            }
        }
        Assert.Equal(3, Enemy(play, petitionId).GetCounter(PassiveStatuses.SignaturesCounter));

        var before = Hero(play).Health.Current;
        var filedBefore = FightProbe.StacksOf(Hero(play), "paperwork"); // the Petition has been filing meanwhile
        play.CombatDriver!.EndTurn(); // READ INTO THE RECORD
        Assert.Null(session.Error);

        // 8 from the reading, then the three liabilities: 1 Fatigue, 2 Paperwork, 1 Doubt + 1 Paperwork — and
        // the whole Paperwork pile ticks at the hero's next turn start.
        Assert.Equal(filedBefore + 3, FightProbe.StacksOf(Hero(play), "paperwork"));
        Assert.Equal(before - 8 - (filedBefore + 3), Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "doubt"));
        Assert.Equal(0, Enemy(play, petitionId).GetCounter(PassiveStatuses.SignaturesCounter)); // a new cycle
    }

    // The Remanded Case, route A: down the Phantom while its Writ still stands and the case comes back —
    // 24 HP, 2 Strength — while the Writ pays 12 HP for it and can never do so again.
    [Fact]
    public void Downing_the_phantom_first_remands_the_case()
    {
        var probe = FightProbe.Roster("remand", energy: 9,
            ("remanded_case_phantom", "uncertain_remand", 30),
            ("escalation_writ", "higher_seal", 30));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("approved_for_disposal", 10).ToList());

        var combat = play.CombatDriver!.Current!;
        var phantomId = combat.State.Combatants.First(c => c.Id.value.StartsWith("remanded_case_phantom")).Id;
        var writId = combat.State.Combatants.First(c => c.Id.value.StartsWith("escalation_writ")).Id;

        // 30 HP of Phantom, three 12-damage forms.
        for (var i = 0; i < 3; i++)
            Disposal(play, session, phantomId);

        var phantom = Enemy(play, phantomId);
        Assert.Equal(24, phantom.Health.Current);                               // returned once
        Assert.Equal(2, FightProbe.StacksOf(phantom, "strength"));
        Assert.Equal(30 - 12, Enemy(play, writId).Health.Current);              // the Writ paid for it
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, writId), PassiveStatuses.SpentWritId));
    }

    // Route B: kill the Writ first and no remand is coming — the Phantom takes Finality instead and its next
    // intent is the Final Judgment.
    [Fact]
    public void Downing_the_writ_first_hands_the_phantom_its_finality()
    {
        var probe = FightProbe.Roster("finality", energy: 9,
            ("remanded_case_phantom", "uncertain_remand", 60),
            ("escalation_writ", "higher_seal", 24));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("approved_for_disposal", 10).ToList());

        var combat = play.CombatDriver!.Current!;
        var phantomId = combat.State.Combatants.First(c => c.Id.value.StartsWith("remanded_case_phantom")).Id;
        var writId = combat.State.Combatants.First(c => c.Id.value.StartsWith("escalation_writ")).Id;

        for (var i = 0; i < 2; i++)
            Disposal(play, session, writId);

        var phantom = Enemy(play, phantomId);
        Assert.Equal(2, FightProbe.StacksOf(phantom, "strength"));
        Assert.Equal(1, phantom.GetCounter(PassiveStatuses.FinalityCounter));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // Final Judgment: 17 damage + 1 Paperwork (and +2 Strength on top)
        Assert.Null(session.Error);
        Assert.True(Hero(play).Health.Current <= before - 17, "the Final Judgment should land at least 17");
        Assert.Equal(0, Enemy(play, phantomId).GetCounter(PassiveStatuses.FinalityCounter)); // spent
    }

    // Appellate Staircase: the Case starts on the Lower Step, which hits 2 harder for holding it, and climbs
    // one Step per round unless the player remands it with 12 damage in a turn.
    [Fact]
    public void The_case_climbs_the_staircase_unless_the_player_remands_it()
    {
        var probe = FightProbe.Roster("staircase", energy: 9,
            ("lower_appellate_step", "stone_step_cut", 24),
            ("middle_appellate_step", "staircase_strike", 30),
            ("upper_appellate_step", "final_step_falls", 36));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("approved_for_disposal", 10).ToList());

        var combat = play.CombatDriver!.Current!;
        var lowerId = combat.State.Combatants.First(c => c.Id.value.StartsWith("lower_appellate_step")).Id;
        var middleId = combat.State.Combatants.First(c => c.Id.value.StartsWith("middle_appellate_step")).Id;


        // The Lower Step's 8-damage cut arrives as 10 while it holds the Case.
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        // No remand happened, so the Case climbed to the Middle Step.
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, lowerId), PassiveStatuses.HoldsTheCaseId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, middleId), PassiveStatuses.HoldsTheCaseId));
        Assert.True(Hero(play).Health.Current <= before - 10, "the Case-holder hits 2 harder");
    }

    [Fact]
    public void Twelve_damage_in_a_turn_remands_the_case_one_step_down()
    {
        var probe = FightProbe.Roster("remand_ladder", energy: 9,
            ("lower_appellate_step", "procedural_step", 24),
            ("middle_appellate_step", "procedural_landing", 30),
            ("upper_appellate_step", "authority_above", 36));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("approved_for_disposal", 10).ToList());

        var combat = play.CombatDriver!.Current!;
        var lowerId = combat.State.Combatants.First(c => c.Id.value.StartsWith("lower_appellate_step")).Id;
        var middleId = combat.State.Combatants.First(c => c.Id.value.StartsWith("middle_appellate_step")).Id;

        play.CombatDriver.EndTurn(); // the Case climbs to the Middle Step
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, middleId), PassiveStatuses.HoldsTheCaseId));

        // 12 HP of ACTUAL damage in one turn sends the Case back down — the Step's own Block counts against
        // that, so the first 12-damage form only lands 9 through Procedural Landing's guard.
        Disposal(play, session, middleId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, middleId), PassiveStatuses.HoldsTheCaseId)); // not yet
        Disposal(play, session, middleId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, lowerId), PassiveStatuses.HoldsTheCaseId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, middleId), PassiveStatuses.HoldsTheCaseId));
    }

    private static void Disposal(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "approved_for_disposal");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static void Cut(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static CombatantState Enemy(RunPlayback play, CombatantId enemyId) =>
        play.CombatDriver!.Current!.State.GetCombatant(enemyId);

    private static int BlockOf(RunPlayback play, CombatantId enemyId) =>
        Enemy(play, enemyId).DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);
}
