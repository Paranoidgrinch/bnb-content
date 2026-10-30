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

    // Under the ration, with a granary standing, she takes a Grain — and eats it at the end of her own
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

        Assert.Equal(Math.Min(bled + 7, Lady(play).Health.Max), Lady(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Lady(play), ActFour.GrainId));
        play.Dispose();
    }

    // Over the ration, a standing granary answers with Burden.
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
        Assert.Equal(0, FightProbe.StacksOf(Lady(play), ActFour.GrainId));
        play.Dispose();
    }

}
