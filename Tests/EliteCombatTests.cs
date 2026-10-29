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

    // Appellate Staircase: the Case starts on the Lower Step, which hits 2 harder for holding it, and climbs
    // one Step per round unless the player remands it with 12 damage in a turn.
    [Fact]
    public void The_case_climbs_the_staircase_unless_the_player_remands_it()
    {
        var probe = FightProbe.Roster("staircase", energy: 9,
            ("lower_appellate_step", "stone_step_cut", 24),
            ("middle_appellate_step", "staircase_strike", 30),
            ("upper_appellate_step", "final_step_falls", 36));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat(Hammer, 10).ToList());

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
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat(Hammer, 10).ToList());

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

    // Iron Warrant Avatar: a visible order each turn. Obeying it strips 5 (Block first, health after);
    // refusing records Contempt, which makes its next attack hit 4 harder per count and then spends itself.
    [Fact]
    public void Obeying_the_warrants_order_costs_it_five()
    {
        // The opening order is "Pay the Fee": end the turn on empty. Three 1-cost Paper Cuts do exactly that.
        var probe = FightProbe.Roster("compliance", ("iron_warrant_avatar", "iron_authority", 94));
        var (play, session, avatarId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), PassiveStatuses.ComplianceOrders[0].StatusId));

        for (var i = 0; i < 3; i++)
            Cut(play, session, avatarId);
        var before = Enemy(play, avatarId).Health.Current;

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // Compliance Credit lands as 5 off the Avatar (its Iron Authority Block only guards from its turn on).
        Assert.Equal(before - 5, Enemy(play, avatarId).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, avatarId), PassiveStatuses.ContemptId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), PassiveStatuses.ComplianceOrders[0].StatusId)); // spent
    }

    [Fact]
    public void Refusing_the_order_is_answered_in_steel()
    {
        var probe = FightProbe.Roster("contempt", ("iron_warrant_avatar", "iron_authority", 94));
        var (play, session, avatarId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // the fee goes unpaid: three Energy left on the table

        Assert.Null(session.Error);
        // The refusal is recorded as Contempt and enforced in the same breath: Contempt forces the Avatar off
        // its defensive intent into Final Service, 17 + 4 per count — and the attack spends the whole record.
        Assert.Equal(before - 21, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, avatarId), PassiveStatuses.ContemptId));
    }

    // The Seizure Procession: the Lantern marks a card the player has just drawn, the Cart takes it away at the
    // turn's end if it is still in hand, and every seizure hardens the Marshal — until the Cart is full.
    [Fact]
    public void The_procession_marks_a_drawn_card_seizes_it_and_hardens_the_marshal()
    {
        var probe = FightProbe.Roster("seizure",
            ("seizure_marshal", "guard_the_list", 40),
            ("inventory_lantern", "seal_the_room", 24),
            ("lock_cart", "iron_storage", 32));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var heroId = combat.HeroId;
        var marshalId = combat.State.Combatants.First(c => c.Id.value.StartsWith("seizure_marshal")).Id;

        // Exactly one card of the opening hand carries the Lantern's mark.
        Assert.Equal(1, Marked(play, heroId, CardZone.Hand));

        play.CombatDriver.EndTurn(); // the marked card is carted off
        Assert.Null(session.Error);
        Assert.Equal(1, Hero(play).GetCounter(PassiveStatuses.SeizedCardsCounter));
        Assert.Equal(1, Marked(play, heroId, CardZone.ExhaustPile));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, marshalId), "strength"));

        play.CombatDriver.EndTurn(); // a second seizure fills the Cart
        Assert.Null(session.Error);
        Assert.Equal(2, Hero(play).GetCounter(PassiveStatuses.SeizedCardsCounter));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, marshalId), "strength"));

        // Full: the Lantern stops marking and nothing more is taken.
        Assert.Equal(0, Marked(play, play.CombatDriver.Current!.HeroId, CardZone.Hand));
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(2, Hero(play).GetCounter(PassiveStatuses.SeizedCardsCounter));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, marshalId), "strength"));
    }

    // A card the player PLAYS leaves the hand with its mark and is never seized.
    [Fact]
    public void A_played_card_escapes_the_cart()
    {
        var probe = FightProbe.Roster("seizure_play",
            ("lock_cart", "iron_storage", 32),
            ("inventory_lantern", "seal_the_room", 24));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var marked = combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.Hand)
            .First(c => c.HasMark(PassiveStatuses.InventoriedMark));
        play.CombatDriver.PlayCard(marked.Id, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
        Assert.Null(session.Error);

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, Hero(play).GetCounter(PassiveStatuses.SeizedCardsCounter));
    }

    // Portcullis Judicator: the player forces the gate up by hitting hard in one turn (12 HP damage), it grinds
    // back down whenever they do not, and an open gate means both harder rulings and 20 % more damage taken.
    [Fact]
    public void The_gate_rises_under_pressure_and_settles_back_down()
    {
        var (play, session, judicatorId) = FightProbe.Start(
            FightProbe.Authored("city_elite_enforcement_03"), Enumerable.Repeat("paper_cut", 10).ToList());

        // It opens the fight half-raised.
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateHalfId));

        // 12 damage in one turn forces it: two Paper Cuts are 12 exactly.
        Cut(play, session, judicatorId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateHalfId));
        Cut(play, session, judicatorId);

        // The gate is OPEN before the turn ends — the player sees the harsher band in time.
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateOpenId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateHalfId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.HeldOpenId));

        // A third hit cannot climb further this turn.
        Cut(play, session, judicatorId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateOpenId));

        var heroBefore = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // Falling Teeth: 18, the OPEN band's opening ruling. The gate stays up — the player forced it.
        Assert.Equal(heroBefore - 18, Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateOpenId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.HeldOpenId));

        // An open gate takes 20 % more: a 6-damage Paper Cut lands as 7.
        var gateBefore = Enemy(play, judicatorId).Health.Current;
        Cut(play, session, judicatorId);
        Assert.Equal(gateBefore - 7, Enemy(play, judicatorId).Health.Current);

        // Ending the turn below the threshold lets it grind down again — into the HALF-RAISED band.
        heroBefore = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateHalfId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateOpenId));
        // The ruling was still handed down from the open gate, and the band alternates: No-Exit Order, 12.
        Assert.Equal(heroBefore - 12, Hero(play).Health.Current);

        // Another quiet turn drops it all the way to SHUT.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, judicatorId), PassiveStatuses.GateShutId));
    }

    private static int Marked(RunPlayback play, CombatantId ownerId, CardZone zone) =>
        play.CombatDriver!.Current!.State.GetCardZones(ownerId).GetCardsInZone(zone)
            .Count(c => c.HasMark(PassiveStatuses.InventoriedMark));

    // The tests' hammer: a canon 1-Energy Deed that lands a flat 9. It used to be the ported v2 "Approved for
    // Disposal" (12 damage), which left the game with the rest of the leftovers on 2026-09-10 — the numbers
    // below are its 9, not that 12.
    private const string Hammer = "cauldron_copy";

    private static void Disposal(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Hammer);
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
