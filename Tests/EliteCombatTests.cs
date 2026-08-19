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

    private static CombatantState Enemy(RunPlayback play, CombatantId enemyId) =>
        play.CombatDriver!.Current!.State.GetCombatant(enemyId);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);
}
