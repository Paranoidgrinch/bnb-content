using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// "The past is evidence. The future is already filed. The present is temporary." The Curator's dial turns
// after every one of its turns, and the same five moves mean three different things depending on where it
// stands. These tests read the record it keeps, watch a filed hour count down and come due, and push one back.
public class CuratorOfMisplacedHoursTests
{
    private const string Deed = "paper_cut";
    private const string Working = "strong_binder";

    private static CombatantState Curator(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static bool Has(RunPlayback play, CombatantId id, string status) =>
        Curator(play, id).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static int Countdown(RunPlayback play, CombatantId id, string hour) =>
        Curator(play, id).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(hour))
            .Select(s => s.DurationTurns).FirstOrDefault();

    private static int Filed(RunPlayback play, CombatantId id) =>
        CuratorOfMisplacedHours.Scheduled.Count(h => Has(play, id, h));

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static int MyHealth(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId).Health.Current;

    // A probe cycling the given intents. The Dial turns after EVERY Curator action, so a one-intent probe
    // would still walk PAST → PRESENT → FUTURE — which is exactly what these tests want to watch.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        int? bossHealth, params string[] intents)
    {
        var probe = FightProbe.Solo(CuratorOfMisplacedHours.EnemyId, intents[0], 9);
        var body = probe.Enemies.Single() with
        {
            Actions = [.. intents.Select(i =>
                new EnemyActionDefinitionId($"{CuratorOfMisplacedHours.EnemyId}.{i}"))],
            MaxHealth = bossHealth ?? probe.Enemies.Single().MaxHealth,
        };

        return FightProbe.Start(
            new EncounterDefinition(probe.Id, [body], probe.HeroResources, probe.HeroStartingStatuses,
                probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects),
            deck: [.. Enumerable.Repeat(Deed, 12), .. Enumerable.Repeat(Working, 12)],
            health: 900);
    }

    private static void PlayAny(RunPlayback play, CombatantId at)
    {
        var card = Hand(play).FirstOrDefault(c => c.DefinitionId.value != CuratorOfMisplacedHours.BorrowCardId);
        Assert.True(card is not null, "the probe hand held nothing but borrowed minutes");
        play.CombatDriver!.PlayCard(card!.Id, at);
    }

    private static void EndTurn(RunPlayback play, int option = 0)
    {
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
    }

    // §7.5: a FUTURE action does not hit — it files an hour with a countdown the player can read.
    [Fact]
    public void A_future_action_files_an_hour_instead_of_striking()
    {
        var (play, _, curator) = Fight(null, "immediate_correction");

        EndTurn(play);   // PRESENT: it simply hits.
        Assert.Equal(0, Filed(play, curator));

        var before = MyHealth(play);
        EndTurn(play);   // FUTURE: it files instead of striking.

        Assert.Equal(1, Filed(play, curator));
        Assert.True(Has(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId));
        Assert.Equal(before, MyHealth(play));
    }

    // …and the filed hour comes due on its own countdown, not on the Curator's next action.
    [Fact]
    public void A_filed_hour_counts_down_and_then_comes_due()
    {
        var (play, _, curator) = Fight(null, "immediate_correction");

        EndTurn(play);
        EndTurn(play);
        Assert.True(Has(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId));

        // Filed for two of its turns; one of them has already been counted at the turn it was filed on.
        var countdown = Countdown(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId);
        Assert.InRange(countdown, 1, CuratorOfMisplacedHours.MaximumCountdown);

        var before = MyHealth(play);
        for (var turn = 0; turn < countdown; turn++)
            EndTurn(play);

        Assert.False(Has(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId),
            "the filed hour never came due");
        Assert.True(MyHealth(play) < before, "the Collapse came due and did nothing");
    }

    // §7.6 Borrow One Minute: the player is handed one minute a turn, and spending it pushes a filed hour
    // back — the fight's central negotiation.
    [Fact]
    public void One_minute_a_turn_pushes_a_filed_hour_back()
    {
        var (play, _, curator) = Fight(null, "immediate_correction");

        Assert.Contains(Hand(play), c => c.DefinitionId.value == CuratorOfMisplacedHours.BorrowCardId);

        EndTurn(play);
        EndTurn(play);
        Assert.True(Has(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId));

        var before = Countdown(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId);
        Assert.True(before < CuratorOfMisplacedHours.MaximumCountdown,
            "nothing to prove: the hour was already at the ceiling");

        var minute = Hand(play).First(c => c.DefinitionId.value == CuratorOfMisplacedHours.BorrowCardId);
        play.CombatDriver!.PlayCard(minute.Id, curator);
        // Option 0 is the Collapse, which is the only hour on the timeline.
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([0]);

        Assert.Equal(before + 1, Countdown(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId));
    }

    // …and never past the ceiling. "Borrow One Minute cannot create countdown 4+."
    [Fact]
    public void A_borrowed_minute_never_pushes_past_three()
    {
        var (play, _, curator) = Fight(null, "immediate_correction");

        EndTurn(play);
        EndTurn(play);

        // Spend a minute every turn the hour is still standing.
        for (var turn = 0; turn < 4; turn++)
        {
            var minute = Hand(play).FirstOrDefault(c =>
                c.DefinitionId.value == CuratorOfMisplacedHours.BorrowCardId);
            if (minute is null || !Has(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId))
                break;

            play.CombatDriver!.PlayCard(minute.Id, curator);
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]);

            Assert.True(
                Countdown(play, curator, CuratorOfMisplacedHours.ScheduledCollapseId)
                    <= CuratorOfMisplacedHours.MaximumCountdown,
                "a borrowed minute pushed the hour past the ceiling");
            EndTurn(play);
        }
    }

    // §7.5: no more than three hours are ever on the timeline at once.
    [Fact]
    public void It_never_files_more_than_three_hours()
    {
        var (play, _, curator) = Fight(null,
            "immediate_correction", "seize_the_current_hour", "hold_the_present_open",
            "the_only_moment_that_hurts", "schedule_the_collapse");

        for (var turn = 0; turn < 14; turn++)
        {
            EndTurn(play);
            Assert.True(Filed(play, curator) <= CuratorOfMisplacedHours.TimelineCapacity,
                $"the timeline carried {Filed(play, curator)} hours");
        }
    }

    // §7.3 Reopen the First Procedure: a PAST action answers what the player OPENED with. Two runs that open
    // differently take different answers, which is the whole point of keeping a record.
    [Fact]
    public void What_the_past_does_depends_on_what_you_opened_with()
    {
        int Reopened(string opener)
        {
            // seize_the_current_hour on the PAST sector is Reopen the First Procedure.
            var (play, _, curator) = Fight(null, "seize_the_current_hour");

            // The record holds the last COMPLETED turn only, so the opening has to be repeated every turn or
            // the evidence the PAST action reads would be an empty turn instead.
            void Open()
            {
                var card = Hand(play).First(c => c.DefinitionId.value == opener);
                play.CombatDriver!.PlayCard(card.Id, curator);
            }

            // The dial starts on PRESENT, so it takes two turns to come round to PAST.
            Open();
            EndTurn(play);   // PRESENT
            Open();
            EndTurn(play);   // FUTURE
            Open();

            var before = MyHealth(play);
            EndTurn(play);   // PAST — reopens the recorded opening
            return before - MyHealth(play);
        }

        // A Deed opening is answered with 16; a Working opening with 11 and a guard for itself.
        Assert.NotEqual(Reopened(Deed), Reopened(Working));
    }

    // §Transition: at 139 HP it takes the present off its own clock, gains Block and does not attack. From
    // then on there is nothing but evidence and schedule.
    [Fact]
    public void At_half_its_body_it_removes_the_present_from_the_clock()
    {
        var (play, _, curator) = Fight(130, "immediate_correction");

        Assert.False(Has(play, curator, CuratorOfMisplacedHours.PresentRemovedId));
        var before = MyHealth(play);

        EndTurn(play);

        Assert.True(Has(play, curator, CuratorOfMisplacedHours.PresentRemovedId),
            "the Curator never removed the present");
        Assert.Equal(before, MyHealth(play));
    }

    // §Final Signature: at 70 HP it files all three of its last hours at once, and hands the player a minute
    // it does not have to pay for — the design's own compensation for the pressure.
    [Fact]
    public void The_last_hours_are_all_filed_at_once()
    {
        var (play, _, curator) = Fight(60, "immediate_correction");

        EndTurn(play);

        // The nearest hour was filed for ONE enemy turn, so it has already come due by the time that turn is
        // over; the other two are standing with their countdowns on the table.
        Assert.True(Has(play, curator, CuratorOfMisplacedHours.ScheduledMiddleId));
        Assert.True(Has(play, curator, CuratorOfMisplacedHours.ScheduledFarId));

        var me = play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);
        Assert.Contains(me.Statuses, s =>
            s.DefinitionId == new StatusDefinitionId(CuratorOfMisplacedHours.FreeAdjustmentId));
    }
}
