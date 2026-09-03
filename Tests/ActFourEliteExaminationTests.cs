using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, elites 9 and 10 — the act's final examination and its last word on discipline, proved in live
// fights. Between them they ask the two questions the whole act has been building to: have you LEARNED the
// five words, and can you keep to a schedule that is entirely visible?
public class ActFourEliteExaminationTests
{
    private const string OneCost = "paper_cut";  // Deed, 1: deal 6

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Keeper of the Thirty-Six Decans ───────────────────────────────────────────────────────────────

    // One watch a turn, the five words in order, and the sixth reads the stars.
    [Fact]
    public void The_watches_walk_the_five_words_in_order()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("keeper_of_the_thirty_six_decans", "move_the_star_peg"), health: 1200);

        // Watch I is set the moment the fight opens.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));

        // Watch III arrives at TWO, and that is the examination working: Watch II handed the player the
        // register and they did not spend it, so the next thing that happened to them landed one larger.
        // The act teaching itself is the whole design of this body.
        play.CombatDriver.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));

        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));

        // The sixth carries no burden of its own — it queues the reading, and the hours are shorter after it.
        play.CombatDriver.EndTurn();
        var keeper = Body(play, "keeper_of_the_thirty_six_decans");
        Assert.Equal(1, FightProbe.StacksOf(keeper, ActFour.HoursShortenedId));
        play.Dispose();
    }

    // A watch cleared before the turn ends costs the keeper 6. Watch III is the burden, and the way to clear
    // a burden is to pay the surcharge off — which is the act's whole lesson, asked as an examination.
    [Fact]
    public void Clearing_a_watch_costs_the_keeper_six()
    {
        // Black Horizon rather than the star peg: a keeper standing behind 24 Block would soak the cuts, and
        // what this test is about is the 6 the observation costs it.
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo("keeper_of_the_thirty_six_decans", "black_horizon", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 20)], health: 1200);

        play.CombatDriver!.EndTurn();  // Watch II — the register
        play.CombatDriver.EndTurn();   // Watch III — the burden. Here the keeper's own Black Horizon spent
                                       // the register a moment earlier, so the burden arrives at one.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        var before = Body(play, "keeper_of_the_thirty_six_decans").Health.Current;
        Play(play, session, OneCost, keeper);  // 2 Energy while burdened: the surcharge is actually paid
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        play.CombatDriver.EndTurn();

        // 6 for the correct observation, on top of the 6 the cut did.
        Assert.Equal(6 + 6, before - Body(play, "keeper_of_the_thirty_six_decans").Health.Current);
        play.Dispose();
    }

    // ── the Colossus of the Endless Procession ────────────────────────────────────────────────────────────

    // Nothing from outside makes this thing stronger, and the refusal is permanent: it puts itself straight
    // back up every time it is spent.
    [Fact]
    public void Stone_does_not_hurry_and_the_refusal_never_runs_out()
    {
        // Strength granted through the ordinary pipeline as the fight is dressed: refused entire, and the
        // refusal is still standing afterwards. That second half is the whole point — a prohibition with a
        // stock would be one grant away from running out, and this one puts itself straight back up.
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("colossus_of_the_endless_procession", "shadow_of_the_procession", energy: 3,
                ("strength", 2)),
            health: 1200);

        var stone = Body(play, "colossus_of_the_endless_procession");
        Assert.Equal(0, FightProbe.StacksOf(stone, "strength"));
        Assert.Equal(1, FightProbe.StacksOf(stone, ActFour.StoneDoesNotHurryId));
        play.Dispose();
    }

    // The foot's weight is the record: meet the measure and work the burden off and it comes down at 26.
    [Fact]
    public void Keeping_both_requirements_is_the_lightest_foot()
    {
        var (play, session, colossus) = FightProbe.Start(
            FightProbe.SoloCycle("colossus_of_the_endless_procession", 9,
                "processional_measure", "the_burden_advances", "the_foot_descends"),
            deck: [.. Enumerable.Repeat(OneCost, 20)], health: 1200);

        play.CombatDriver!.EndTurn();  // Step I — the measure: spend exactly 2
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        Play(play, session, OneCost, colossus);
        Play(play, session, OneCost, colossus);
        play.CombatDriver.EndTurn();   // met — Step II advances the burden

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Play(play, session, OneCost, colossus);  // a taxed card, actually paid for

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();   // Step III — the foot

        Assert.Equal(26, before - Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // …and doing neither is 40 and a burial.
    [Fact]
    public void Keeping_neither_requirement_is_the_heaviest_foot_and_a_burial()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle("colossus_of_the_endless_procession",
                "processional_measure", "the_burden_advances", "the_foot_descends"),
            health: 1200);

        play.CombatDriver!.EndTurn();  // the measure, unanswered
        play.CombatDriver.EndTurn();   // the burden, unpaid

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();

        Assert.Equal(40, before - Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }
}
