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

    private static void Cut(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static CombatantState Enemy(RunPlayback play, CombatantId enemyId) =>
        play.CombatDriver!.Current!.State.GetCombatant(enemyId);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);
}
