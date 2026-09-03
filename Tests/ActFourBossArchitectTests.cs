using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Architect of the Impossible Pyramid, proved in live fights.
//
// The fight is a schedule, and the blueprints are the brake. The tests follow the Monument: what a course
// laid true takes off it, what a course missed puts on, and what happens when nothing has held it back.
public class ActFourBossArchitectTests
{
    private const string Cut = "paper_cut";      // Deed, 1: deal 6
    private const string Wax = "waxen_surety";   // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Builder(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("architect", StringComparison.Ordinal));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Monument(RunPlayback play) =>
        FightProbe.StacksOf(Builder(play), ActFour.MonumentId);

    private static IReadOnlyList<string> InHand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand.Select(c => c.DefinitionId.value)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // After the draw there are two sheets on the table, and they are never the same sheet twice.
    [Fact]
    public void Two_different_blueprints_are_laid_after_the_draw()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "stone_sledge", energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        var sheets = InHand(play).Where(id => id.StartsWith("the_", StringComparison.Ordinal)).ToList();
        Assert.Contains(ActFour.FoundationCardId, sheets);
        Assert.Contains(ActFour.CoursesCardId, sheets);
        Assert.Equal(sheets.Count, sheets.Distinct().Count());

        // And the Monument opens at two, four steps below the capstone.
        Assert.Equal(2, Monument(play));
        play.Dispose();
    }

    // A course laid true takes a step off the Monument — and his own turn puts one back, so holding the
    // schedule steady is exactly what a perfect turn buys in the first half.
    [Fact]
    public void A_measured_foundation_met_takes_a_step_off_the_monument()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "stone_sledge", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        Play(play, session, ActFour.FoundationCardId, null);
        Play(play, session, Wax, null);
        Play(play, session, Wax, null);  // exactly 2 spent
        play.CombatDriver!.EndTurn();

        // 2 − 1 for the course, + 1 for the end of his own turn.
        Assert.Equal(2, Monument(play));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // …and a course missed puts one on instead, and buries the player for the trouble.
    [Fact]
    public void A_measured_foundation_missed_climbs_the_monument_and_buries_you()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "stone_sledge", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        Play(play, session, ActFour.FoundationCardId, null);
        Play(play, session, Wax, null);  // one spent, not two
        play.CombatDriver!.EndTurn();

        // 2 + 1 for the missed course, + 1 for the end of his own turn.
        Assert.Equal(4, Monument(play));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // Every SECOND course laid true is 8 of his own blood — direct loss, not damage, so succeeding is not
    // merely slowing the clock. The deck here does no damage of its own, so the figure is his alone.
    [Fact]
    public void Every_second_true_course_costs_him_eight_of_his_own()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "stone_sledge", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        var whole = Builder(play).Health.Current;

        for (var turn = 0; turn < 2; turn++)
        {
            // Whichever sheet the rotation lays, the Foundation is always one of the two: two Energy is what
            // a turn opens with, so it is the one blueprint that can always be met.
            Play(play, session, ActFour.FoundationCardId, null);
            Play(play, session, Wax, null);
            Play(play, session, Wax, null);
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal(whole - 8, Builder(play).Health.Current);
        play.Dispose();
    }

    // Alternating Stone reads the first two stones you set and asks only that they be of different kinds.
    [Fact]
    public void Alternating_stone_reads_the_first_two_kinds_of_card()
    {
        // Two deeds and a working in hand: the pair the rotation lays on the second turn is the fallback
        // Course and the Alternating Stone.
        IReadOnlyList<string> deck = [Cut, Cut, Wax];

        var (met, metSession, metTarget) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "stone_sledge", energy: 9), deck, health: 900);
        met.CombatDriver!.EndTurn();

        Assert.Contains(ActFour.AlternatingCardId, InHand(met));
        Play(met, metSession, ActFour.AlternatingCardId, null);
        Play(met, metSession, Cut, metTarget);
        Play(met, metSession, Wax, null);
        met.CombatDriver.EndTurn();

        // 2, + 1 for the first turn's end, − 1 for the course, + 1 for the second: three.
        Assert.Equal(3, Monument(met));
        Assert.Equal(0, FightProbe.StacksOf(Hero(met), BnbContent.Converter.Cards.Keywords.Paperwork));
        met.Dispose();

        var (missed, missedSession, missedTarget) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "stone_sledge", energy: 9), deck, health: 900);
        missed.CombatDriver!.EndTurn();

        Play(missed, missedSession, ActFour.AlternatingCardId, null);
        Play(missed, missedSession, Cut, missedTarget);
        Play(missed, missedSession, Cut, missedTarget);  // the same kind twice
        missed.CombatDriver.EndTurn();

        Assert.Equal(4, Monument(missed));
        Assert.Equal(2, FightProbe.StacksOf(Hero(missed), BnbContent.Converter.Cards.Keywords.Paperwork));
        missed.Dispose();
    }

    // Six steps and the capstone comes down on the room — and the first one of them is what makes the plan
    // retroactively correct. Nothing here holds the schedule back, which is the point: the clock is his.
    [Fact]
    public void Six_steps_bring_the_capstone_down_and_the_plan_becomes_correct()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ArchitectEnemyId, "night_shift_on_the_ramp", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        var whole = Hero(play).Health.Current;

        play.CombatDriver!.EndTurn();  // +1 night shift, +1 his turn end: 4
        Assert.Equal(4, Monument(play));

        play.CombatDriver.EndTurn();   // …6, and the capstone is queued
        Assert.Equal(ActFour.MonumentCap, Monument(play));
        Assert.False(FightProbe.StacksOf(Builder(play), ActFour.PlanAlwaysCorrectId) > 0);

        play.CombatDriver.EndTurn();
        var architect = Builder(play);

        Assert.Equal(1, FightProbe.StacksOf(architect, ActFour.PlanAlwaysCorrectId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        // Reset to three, and his own turn end puts the fourth on.
        Assert.Equal(ActFour.CapstoneResets + 1, Monument(play));
        Assert.True(whole - Hero(play).Health.Current >= 42, "the capstone did not land");
        play.Dispose();
    }

    // The failsafe: half his blood and the plan is correct anyway, whether a capstone ever fell or not. It is
    // not an attack — he simply stands behind 18 and goes on building.
    [Fact]
    public void Half_his_blood_makes_the_plan_correct_without_a_capstone()
    {
        var (play, session, target) = FightProbe.Start(
            FightProbe.Roster("architect_half", energy: 9,
                (ActFour.ArchitectEnemyId, "stone_sledge", 322)),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        Assert.Equal(0, FightProbe.StacksOf(Builder(play), ActFour.PlanAlwaysCorrectId));

        Play(play, session, Cut, target);  // 322 → 316
        var architect = Builder(play);

        Assert.Equal(1, FightProbe.StacksOf(architect, ActFour.PlanAlwaysCorrectId));
        Assert.Equal(18, Block(architect));
        play.Dispose();
    }
}
