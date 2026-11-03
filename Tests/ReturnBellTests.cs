using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The After-Hours Return Bell (user, 2026-11-03): Closing Time counts down at its turns and rings for 25 at 0;
// every turn you play 4 or more cards puts it back by one, never past 4.
public class ReturnBellTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Bell(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    // The Bell as authored: Closing Time 4.
    private static RunPlayback Fight(int energy = 3)
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "reopen_for_one_final_minute", energy),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);
        return play;
    }

    [Fact]
    public void The_count_ticks_down_at_the_bells_turn()
    {
        using var play = Fight();
        Assert.Equal(ReturnBell.ClosingFrom, FightProbe.StacksOf(Bell(play), ReturnBell.ClosingTimeId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, FightProbe.StacksOf(Bell(play), ReturnBell.ClosingTimeId));
        Assert.Equal(400, Hero(play).Health.Current);
    }

    [Fact]
    public void At_zero_it_rings_and_starts_again()
    {
        using var play = Fight();
        for (var turn = 0; turn < 3; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(400, Hero(play).Health.Current);
        play.CombatDriver!.EndTurn();
        Assert.Equal(400 - ReturnBell.RingDamage, Hero(play).Health.Current);
        Assert.Equal(ReturnBell.ClosingFrom, FightProbe.StacksOf(Bell(play), ReturnBell.ClosingTimeId));
    }

    [Theory]
    [InlineData(1, 3, 4)]
    [InlineData(0, 4, 4)] // never past where it starts
    public void A_turn_of_four_cards_puts_the_count_back_by_one(int turns, int before, int after)
    {
        using var play = Fight(energy: 9);
        for (var turn = 0; turn < turns; turn++)
            play.CombatDriver!.EndTurn();
        var bell = Bell(play).Id;
        for (var i = 0; i < 3; i++)
            play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, bell);
        Assert.Equal(before, FightProbe.StacksOf(Bell(play), ReturnBell.ClosingTimeId));
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, bell);
        Assert.Equal(after, FightProbe.StacksOf(Bell(play), ReturnBell.ClosingTimeId));
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, bell);
        Assert.Equal(after, FightProbe.StacksOf(Bell(play), ReturnBell.ClosingTimeId));
    }
}
