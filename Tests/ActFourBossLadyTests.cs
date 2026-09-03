using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Lady of the Black Granaries, proved in live fights.
//
// She names a number of cards and the whole fight is what happens around it. The tests follow the ration:
// what counting exactly buys, what the seals still standing charge for a miss, and what is left of her once
// all four functions are gone.
public class ActFourBossLadyTests
{
    private const string Cut = "paper_cut";      // Deed, 1: deal 6
    private const string Wax = "waxen_surety";   // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Lady(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("lady", StringComparison.Ordinal));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static IReadOnlyList<string> InHand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand.Select(c => c.DefinitionId.value)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // The ration is announced after the draw, and counting it exactly hands the player the choice of which
    // of her four functions to take away.
    [Fact]
    public void An_exact_ration_buys_the_choice_of_a_seal()
    {
        var (play, session, lady) = FightProbe.Start(
            FightProbe.Solo(ActFour.LadyEnemyId, "black_granary_staff", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        // The ration is a face on HER: an announcement is not something that happens to the player.
        Assert.Equal(3, FightProbe.StacksOf(Lady(play), ActFour.LadyRationId));

        var whole = Hero(play).Health.Current;
        for (var i = 0; i < 3; i++)
            Play(play, session, Wax, null);
        play.CombatDriver!.EndTurn();

        // Her staff is 28 and her next blow was measured 5 softer, so it landed as 23…
        Assert.Equal(23, whole - Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Lady(play), ActFour.ShortMeasureId));

        // …and the four seals are in hand as a choice.
        var hand = InHand(play);
        Assert.Contains(ActFour.ReserveCardId, hand);
        Assert.Contains(ActFour.LaborCardId, hand);
        Assert.Contains(ActFour.RecordCardId, hand);
        Assert.Contains(ActFour.LadyRationCardId, hand);

        var wounded = Hero(play).Health.Current;
        Play(play, session, ActFour.ReserveCardId, null);

        Assert.False(Lady(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.ReserveSealId)));
        Assert.Equal(wounded + 5, Hero(play).Health.Current);

        // One break per turn: the other three sheets do nothing once the choice is spent.
        Play(play, session, ActFour.LaborCardId, null);
        Assert.True(Lady(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.LaborSealId)));
        play.Dispose();
    }

    // Under the ration, with the Reserve Seal intact, she takes a Grain — and eats it at the end of her own
    // turn, 7 a head.
    [Fact]
    public void An_under_ration_feeds_her()
    {
        var (play, session, lady) = FightProbe.Start(
            FightProbe.Solo(ActFour.LadyEnemyId, "black_granary_staff", energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        Play(play, session, Cut, lady);
        Play(play, session, Cut, lady);  // two of the three she counted
        var bled = Lady(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(bled + 7, Lady(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Lady(play), ActFour.GrainId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        play.Dispose();
    }

    // Over the ration, the Labor Seal answers with Burden and the Record Seal writes it up.
    [Fact]
    public void An_over_ration_burdens_and_is_written_up()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.LadyEnemyId, "black_granary_staff", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        for (var i = 0; i < 4; i++)
            Play(play, session, Wax, null);  // one more than she counted
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        Assert.Equal(0, FightProbe.StacksOf(Lady(play), ActFour.GrainId));
        play.Dispose();
    }

    // With the Ration Seal broken, counting correctly stops being merely correct: every exact ration is 10
    // of her blood and 10 of her cover.
    [Fact]
    public void With_the_ration_seal_broken_an_exact_ration_is_a_window()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.LadyEnemyId, "seal_the_storehouse", energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        for (var i = 0; i < 3; i++)
            Play(play, session, Wax, null);
        play.CombatDriver!.EndTurn();

        // She sealed the storehouse behind 28, and breaking the Ration Seal takes 10 of it at once.
        Play(play, session, ActFour.LadyRationCardId, null);
        Assert.Equal(18, Block(Lady(play)));

        var whole = Lady(play).Health.Current;
        for (var i = 0; i < 4; i++)
            Play(play, session, Wax, null);  // the second turn's ration is four
        play.CombatDriver.EndTurn();

        Assert.Equal(whole - 10, Lady(play).Health.Current + 0);
        play.Dispose();
    }

    // The failsafe: at 300 with seals still standing they are all struck off at once, nobody is paid for it,
    // and the stores stand open for two whole player turns before the books are closed.
    [Fact]
    public void The_emergency_opening_breaks_every_seal_and_opens_the_stores()
    {
        var (play, session, lady) = FightProbe.Start(
            FightProbe.Roster("lady_low", energy: 9, (ActFour.LadyEnemyId, "black_granary_staff", 302)),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        Play(play, session, Cut, lady);  // 302 → 296

        var opened = Lady(play);
        foreach (var seal in new[]
                 {
                     ActFour.ReserveSealId, ActFour.LaborSealId,
                     ActFour.RecordSealId, ActFour.LadyRationSealId,
                 })
            Assert.False(opened.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(seal)),
                $"'{seal}' survived the emergency opening");

        Assert.Equal(1, FightProbe.StacksOf(opened, ActFour.GranariesOpenId));

        // Two complete player turns of open stores…
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Lady(play), ActFour.GranariesOpenId));
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Lady(play), ActFour.GranariesOpenId));

        // …and then the books are closed: no cover, no grain, famine accounting.
        play.CombatDriver.EndTurn();
        var accounting = Lady(play);
        Assert.Equal(0, FightProbe.StacksOf(accounting, ActFour.GranariesOpenId));
        Assert.Equal(1, FightProbe.StacksOf(accounting, ActFour.FamineAccountingId));
        Assert.Equal(0, Block(accounting));
        play.Dispose();
    }

    // Famine accounting runs 2, 5, 2, 5 — and three rations counted wrong empty the storehouse.
    [Fact]
    public void Famine_runs_two_and_five_and_three_misses_empty_the_storehouse()
    {
        // The phase is a STARTING status: an interactive fight is a replay, so a state poked into a live
        // combat is thrown away by the next answer.
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.LadyEnemyId, "black_granary_staff", energy: 9,
                (ActFour.FamineAccountingId, 1)),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        Assert.Equal(2, FightProbe.StacksOf(Lady(play), ActFour.LadyRationId));
        play.CombatDriver!.EndTurn();

        Assert.Equal(5, FightProbe.StacksOf(Lady(play), ActFour.LadyRationId));
        Assert.Equal(1, FightProbe.StacksOf(Lady(play), ActFour.FamineId));

        play.CombatDriver.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Lady(play), ActFour.FamineId));

        play.CombatDriver.EndTurn();  // the third miss: three, and the storehouse answers
        Assert.Equal(0, FightProbe.StacksOf(Lady(play), ActFour.FamineId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }
}
