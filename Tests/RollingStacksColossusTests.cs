using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Rolling Stacks Colossus (user, 2026-11-03): its Distance drops by one each of its turns and at 0 it crushes
// for 20, back at 3; 12 or more damage in one turn pushes it back one.
public class RollingStacksColossusTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Colossus(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Distance(RunPlayback play) => FightProbe.StacksOf(Colossus(play), RollingStacksColossus.DistanceId);

    private static RunPlayback Fight(string intent = "close_the_remaining_passage", int energy = 3) =>
        FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, intent, energy),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400).Play;

    [Fact]
    public void It_rolls_closer_and_crushes_at_zero()
    {
        using var play = Fight();
        Assert.Equal(3, Distance(play));
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Distance(play));
        Assert.Equal(400, Hero(play).Health.Current);
        play.CombatDriver.EndTurn();
        Assert.Equal(400 - RollingStacksColossus.CrushDamage, Hero(play).Health.Current);
        Assert.Equal(RollingStacksColossus.StartDistance, Distance(play));
    }

    // Two Paper Cuts (12 health) push it back one step — once a turn, however much more follows; one alone does not.
    [Fact]
    public void Twelve_damage_in_a_turn_pushes_it_back_once()
    {
        using var play = Fight("displace_the_ladder", energy: 9);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Distance(play));
        var colossus = Colossus(play).Id;
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, colossus);
        Assert.Equal(1, Distance(play));
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, colossus);
        Assert.Equal(2, Distance(play));
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, colossus);
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, colossus);
        Assert.Equal(2, Distance(play));
        // …and the count starts over with its turn.
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Distance(play));
    }

    [Fact]
    public void It_is_never_pushed_past_where_it_starts()
    {
        using var play = Fight("displace_the_ladder", energy: 9);
        var colossus = Colossus(play).Id;
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, colossus);
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, colossus);
        Assert.Equal(RollingStacksColossus.StartDistance, Distance(play));
    }
}
