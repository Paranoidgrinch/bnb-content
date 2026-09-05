using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// ACT V, the third god — Nanshe, Keeper of the Just Ration, proved in live fights.
//
// The tests follow the tablet: what a day's portion comes to, what taking from a later day costs, what an
// unused portion gives back, what changes when she starts counting the measures she did not allot, and what
// the last four days are.
public class ActFiveBossNansheTests
{
    private const string Cut = "paper_cut";              // Deed, 1 Energy: deal 6
    private const string Penalty = "compounded_penalty"; // Attack, 2 Energy

    // A day on which she does nothing but stand there, so a test about one rule is about that rule.
    private const string Quiet = "the_quiet_measure";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Keeper(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("nanshe", StringComparison.Ordinal));

    private static int Stacks(CombatantState body, string status) => FightProbe.StacksOf(body, status);

    private static int Energy(RunPlayback play) =>
        Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static int HandSize(RunPlayback play) => play.CombatDriver!.Current!.Hand.Count;

    private static IReadOnlyList<string> InHand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand.Select(c => c.DefinitionId.value)];

    private static void Play(RunPlayback play, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
    }

    // Spend the day's portion down to nothing, so what the day's END does is about the debt and not about
    // an unspent point paying it back. Bounded by the pool rather than by the hand: a card the pool can no
    // longer pay for stays in hand, and a loop that waits for it to leave waits for ever.
    private static void Spend(RunPlayback play, CombatantId enemy)
    {
        for (var guard = 0; guard < 20 && Energy(play) > 0; guard++)
        {
            var card = play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == Cut);
            if (card is null)
                return;
            play.CombatDriver.PlayCard(card.Id, enemy);
        }
    }

    // Her whole calendar is the round, so a probe that keeps only ONE of her actions still walks the days and
    // the Distributions exactly as the real fight does.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Ration(
        string intent = Quiet, int energy = 3, int deck = 14, int health = 900,
        (string Status, int Stacks)[]? hers = null, int? drawn = null)
    {
        var probe = FightProbe.Solo(ActFive.NansheEnemyId, intent, energy, hers ?? []);
        if (drawn is { } wide)
            probe = new EncounterDefinition(
                probe.Id, probe.Enemies, probe.HeroResources, probe.HeroStartingStatuses,
                probe.HeroDisplayName, wide, probe.TriggeredEffects);
        return FightProbe.Start(probe, deck: [.. Enumerable.Repeat(Cut, deck)], health: health);
    }

    // A transition is something she says ONCE. A one-action probe would say it every round — which for the
    // Final Distribution means pouring a fresh store on top of the one being spent, every day, for ever.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) After(
        string transition, int deck = 20, int energy = 3)
    {
        var probe = FightProbe.SoloCycle(
            ActFive.NansheEnemyId, energy,
            [transition, Quiet, Quiet, Quiet, Quiet, Quiet, Quiet, Quiet]);
        return FightProbe.Start(probe, deck: [.. Enumerable.Repeat(Cut, deck)], health: 2000);
    }

    // ── the tablet ────────────────────────────────────────────────────────────────────────────────────────

    // The first day takes nothing at all: she has allotted the build its own share and nothing has been taken
    // out of any later day yet. And the tablet is in front of the player from the first bell.
    [Fact]
    public void The_first_day_is_the_builds_own_share()
    {
        var (play, _, _) = Ration();

        Assert.Equal(1, Stacks(Hero(play), ActFive.RationTabletId));
        Assert.Equal(1, Stacks(Hero(play), ActFive.DayOfDistributionId));
        Assert.Equal(1, Stacks(Keeper(play), ActFive.ShelterId));
        Assert.Equal(3, Energy(play));
        Assert.Equal(0, Stacks(Hero(play), ActFive.WithheldEnergyId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        // And both levers are in hand on an unsealed day.
        Assert.Contains(ActFive.TakeAheadCardId, InHand(play));
        Assert.Contains(ActFive.DrawAheadCardId, InHand(play));
        play.Dispose();
    }

    // §8.1: she does not hide her future intents within a Distribution. The engine's own sight, granted by
    // the tablet — three days read at once, which is the whole tablet.
    [Fact]
    public void The_player_reads_all_three_days_before_the_first_one()
    {
        var (play, _, enemy) = Ration();

        var days = play.CombatDriver!.Current!.UpcomingIntentsFor(enemy);

        Assert.Equal(3, days.Count);
        play.Dispose();
    }

    // The day and the pattern are a function of the round, which is why the three days above can be read at
    // all: her calendar is not a hidden counter, it is the clock everybody can see.
    [Fact]
    public void The_days_run_one_two_three_and_then_the_pattern_turns()
    {
        var (play, _, _) = Ration();

        var days = new List<int>();
        var patterns = new List<bool>();
        for (var round = 0; round < 4; round++)
        {
            days.Add(Stacks(Hero(play), ActFive.DayOfDistributionId));
            patterns.Add(Stacks(Keeper(play), ActFive.ShelterId) == 1);
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal([1, 2, 3, 1], days);
        Assert.Equal([true, true, true, false], patterns);
        Assert.Equal(1, Stacks(Keeper(play), ActFive.LabourId));
        play.Dispose();
    }

    // ── taking from a later day ───────────────────────────────────────────────────────────────────────────

    // TAKE AHEAD. The point is HELD rather than gained, because the pool she just filled has no room in it —
    // and it arrives at exactly the moment the player runs out, which is the moment it was taken for.
    [Fact]
    public void Take_ahead_holds_a_point_that_arrives_when_the_pool_runs_dry()
    {
        var (play, session, enemy) = Ration(deck: 6);

        Play(play, ActFive.TakeAheadCardId, null);
        Assert.True(session.Error is null, session.Error);

        // Nothing has been added to a full pool — it is waiting.
        Assert.Equal(3, Energy(play));
        Assert.Equal(1, Stacks(Hero(play), ActFive.BorrowedEnergyId));

        for (var i = 0; i < 3; i++)
            Play(play, Cut, enemy);

        // Three cards paid for out of three Energy, and the fourth point is there.
        Assert.Equal(1, Energy(play));
        play.Dispose();
    }

    // …and tomorrow is simply smaller. What was taken comes off the next day's portion, and the Withheld row
    // says so rather than leaving the player to work out why the pool is short.
    [Fact]
    public void What_was_taken_comes_off_the_next_day()
    {
        var (play, _, enemy) = Ration(deck: 6);

        Play(play, ActFive.TakeAheadCardId, null);
        Play(play, ActFive.TakeAheadCardId, null);
        Assert.Equal(2, Stacks(Hero(play), ActFive.BorrowedEnergyId));

        // Spend the day out, so nothing is returned at its end.
        Spend(play, enemy);
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, Energy(play));
        Assert.Equal(2, Stacks(Hero(play), ActFive.WithheldEnergyId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        play.Dispose();
    }

    // §8.7. A day outside the Final Distribution never falls below one natural Energy — and the debt the
    // minimum protects is not forgiven, it waits for a day that can carry it.
    [Fact]
    public void No_day_falls_below_one_energy_and_the_rest_of_the_debt_waits()
    {
        // A wide hand, because the point of this test is a day SPENT OUT: energy taken ahead is energy
        // HELD, and five one-cost cards cannot consume seven points however willing the player is.
        var (play, _, enemy) = Ration(deck: 20, drawn: 10);

        for (var i = 0; i < 4; i++)
            Play(play, ActFive.TakeAheadCardId, null);
        Assert.Equal(4, Stacks(Hero(play), ActFive.BorrowedEnergyId));

        Spend(play, enemy);
        play.CombatDriver!.EndTurn();

        // Three natural, two taken, one left standing — and two of the four still owed.
        Assert.Equal(1, Energy(play));
        Assert.Equal(2, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        play.Dispose();
    }

    // §8.5, RETURN THE PORTION. She does not confiscate what was not used: an unspent point pays a borrowed
    // one back, which is the whole reason a day of restraint is worth playing.
    [Fact]
    public void An_unspent_point_returns_the_portion()
    {
        var (play, _, _) = Ration(deck: 6);

        Play(play, ActFive.TakeAheadCardId, null);
        Play(play, ActFive.TakeAheadCardId, null);
        Assert.Equal(2, Stacks(Hero(play), ActFive.BorrowedEnergyId));

        // Nothing is spent, so all three points of the portion come back against the debt.
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.WithheldEnergyId));
        Assert.Equal(3, Energy(play));
        play.Dispose();
    }

    // DRAW AHEAD moves quantity, not cards: one card now, one fewer at the start of a later day.
    [Fact]
    public void Draw_ahead_costs_a_card_from_a_later_day()
    {
        var (play, _, enemy) = Ration(deck: 20);

        var before = HandSize(play);
        Play(play, ActFive.DrawAheadCardId, null);
        // One sheet left the hand, one card came in, and the sheet re-offered itself.
        Assert.Equal(before + 1, HandSize(play));
        Assert.Equal(1, Stacks(Hero(play), ActFive.BorrowedDrawId));

        // Empty the pool so nothing is returned at the day's end.
        Spend(play, enemy);
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, Stacks(Hero(play), ActFive.WithheldDrawId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedDrawId));
        play.Dispose();
    }

    // §8.12. A Distribution closed owing nothing is answered — modestly, and in Block, at the one moment
    // Block given to the player is worth having.
    [Fact]
    public void The_measure_holds_when_a_distribution_closes_owing_nothing()
    {
        var (play, _, _) = Ration();

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(3, Stacks(Hero(play), ActFive.DayOfDistributionId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.MeasureHoldsId));

        play.CombatDriver.EndTurn();

        // The chip is struck at the next day's start, but it was on the table for the turn it described.
        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        play.Dispose();
    }

    // ── count every measure ───────────────────────────────────────────────────────────────────────────────

    // §8.8. In the first phase what a card makes is the player's own: she counts her portions and nothing
    // else, and the player learns the system on a fight that is not yet punishing them for it.
    [Fact]
    public void Energy_a_card_makes_is_free_while_only_her_portions_are_counted()
    {
        var (play, session, _) = Ration(deck: 6);

        // Her own hand-out is absorbed by the credit she wrote when the sheet was played…
        Play(play, ActFive.TakeAheadCardId, null);
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(1, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        play.Dispose();
    }

    // §8.10. "Three measures entered. They were not allotted. That does not make them uncounted." From here
    // a point of Energy the player makes is charged exactly as one she was given.
    [Fact]
    public void Once_every_measure_is_counted_a_made_point_is_charged_too()
    {
        var (play, _, _) = Ration(hers: [(ActFive.CountEveryMeasureId, 1)], deck: 6);

        var before = Stacks(Hero(play), ActFive.BorrowedEnergyId);
        // One sheet, one point: the credit absorbs her own hand-out, and the sheet's own charge stands alone.
        Play(play, ActFive.TakeAheadCardId, null);
        Assert.Equal(before + 1, Stacks(Hero(play), ActFive.BorrowedEnergyId));

        // A card that draws is a measure now as well.
        Play(play, ActFive.DrawAheadCardId, null);
        Assert.Equal(1, Stacks(Hero(play), ActFive.BorrowedDrawId));
        play.Dispose();
    }

    // §8.11, SEAL THE BASKET. Announced on Day I, and on the day itself nothing may be moved between days at
    // all — the levers are simply not laid out.
    [Fact]
    public void A_sealed_day_is_named_in_advance_and_hands_over_no_levers()
    {
        var (play, _, _) = Ration(hers: [(ActFive.CountEveryMeasureId, 1)]);

        var sealedDay = Stacks(Hero(play), ActFive.BasketSealedId);
        Assert.Equal(2, sealedDay);
        Assert.Contains(ActFive.TakeAheadCardId, InHand(play));

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, Stacks(Hero(play), ActFive.DayOfDistributionId));
        Assert.DoesNotContain(ActFive.TakeAheadCardId, InHand(play));
        Assert.DoesNotContain(ActFive.DrawAheadCardId, InHand(play));

        // Day III has no lever either, and for the other reason: there is no later day IN this Distribution
        // to take from. The seal lifts with the Distribution, and the next one names a different day.
        play.CombatDriver.EndTurn();
        Assert.DoesNotContain(ActFive.TakeAheadCardId, InHand(play));

        play.CombatDriver.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFive.DayOfDistributionId));
        Assert.Equal(3, Stacks(Hero(play), ActFive.BasketSealedId));
        Assert.Contains(ActFive.TakeAheadCardId, InHand(play));
        play.Dispose();
    }

    // ── the final distribution ────────────────────────────────────────────────────────────────────────────

    // §8.13. "There will be four more. There is no fifth." Every account is closed, because there is no later
    // day for a debt to come out of — what is left is one store and the four days it has to cover.
    [Fact]
    public void The_final_distribution_closes_every_account_and_opens_one_store()
    {
        var (play, _, _) = After("there_will_be_four_more", deck: 8);

        Play(play, ActFive.TakeAheadCardId, null);
        Assert.Equal(1, Stacks(Hero(play), ActFive.BorrowedEnergyId));

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, Stacks(Keeper(play), ActFive.FinalDistributionId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedEnergyId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.BorrowedDrawId));
        // Four days on the tablet — none of them spent yet, because she speaks after the day that is over —
        // and the store already one day lighter for the day that has just begun.
        Assert.Equal(4, Stacks(Hero(play), ActFive.DaysRemainId));
        Assert.Equal(13, Stacks(Hero(play), ActFive.FinalEnergyId));
        Assert.Equal(3, Energy(play));
        play.Dispose();
    }

    // The store pays each day and shrinks by exactly what it paid. No minimum protects it: when it is empty
    // the day's portion is empty too.
    [Fact]
    public void The_final_store_pays_each_day_until_there_is_no_fifth_portion()
    {
        var (play, _, _) = After("there_will_be_four_more", deck: 30);

        play.CombatDriver!.EndTurn();      // she speaks; day one of four opens

        var store = new List<int>();
        var energy = new List<int>();
        for (var day = 0; day < 5; day++)
        {
            store.Add(Stacks(Hero(play), ActFive.FinalEnergyId));
            energy.Add(Energy(play));
            play.CombatDriver.EndTurn();
        }

        // 16 → 13 → 10 → 7 → 4, four days paid, and the fifth day receives nothing at all.
        Assert.Equal([13, 10, 7, 4, 4], store);
        Assert.Equal([3, 3, 3, 3, 0], energy);
        Assert.Equal(0, Stacks(Hero(play), ActFive.DaysRemainId));
        play.Dispose();
    }

    // …and the levers stop being laid out the moment the store cannot cover them, which is what makes an
    // empty bowl legible rather than a card that silently does nothing.
    [Fact]
    public void An_empty_store_hands_over_no_levers()
    {
        var (play, _, _) = Ration(deck: 30, hers: [(ActFive.FinalDistributionId, 1)]);

        Assert.DoesNotContain(ActFive.TakeAheadCardId, InHand(play));
        Assert.DoesNotContain(ActFive.DrawAheadCardId, InHand(play));
        // With no store and no days, the portion is nothing — and the Withheld row says the whole of it.
        Assert.Equal(0, Energy(play));
        play.Dispose();
    }

    // §8.2, and the reason the ration never has to be told what the build is: the share IS the pool she has
    // just refilled, so a build that has earned five Energy is rationed against five.
    [Fact]
    public void The_share_is_whatever_the_build_itself_receives()
    {
        var (play, _, enemy) = Ration(energy: 5, deck: 20, drawn: 10);

        Assert.Equal(5, Energy(play));

        for (var i = 0; i < 3; i++)
            Play(play, ActFive.TakeAheadCardId, null);
        Spend(play, enemy);
        play.CombatDriver!.EndTurn();

        // Three taken out of a five-point day leaves two, where a three-point day would have kept only one.
        Assert.Equal(2, Energy(play));
        Assert.Equal(3, Stacks(Hero(play), ActFive.WithheldEnergyId));
        play.Dispose();
    }
}
