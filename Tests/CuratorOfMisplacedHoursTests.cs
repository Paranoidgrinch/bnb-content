using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Curator of Misplaced Hours (user, 2026-11-03): each card you play adds 2 damage to its next attack, which
// spends them.
public class CuratorOfMisplacedHoursTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Curator(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static RunPlayback Fight(string intent) =>
        FightProbe.Start(FightProbe.Solo(CuratorOfMisplacedHours.EnemyId, intent, 9),
            deck: [.. Enumerable.Repeat("paper_cut", 24)], health: 600).Play;

    [Fact]
    public void Every_card_played_is_two_more_on_its_next_attack_and_the_attack_spends_them()
    {
        using var play = Fight("immediate_correction");
        var curator = Curator(play).Id;
        for (var i = 0; i < 3; i++)
            play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, curator);
        Assert.Equal(3, FightProbe.StacksOf(Curator(play), CuratorOfMisplacedHours.KeptHoursId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(600 - (17 + 3 * CuratorOfMisplacedHours.DamagePerCard), Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Curator(play), CuratorOfMisplacedHours.KeptHoursId));
        // A turn without cards: the plain blow.
        play.CombatDriver.EndTurn();
        Assert.Equal(600 - (17 + 3 * CuratorOfMisplacedHours.DamagePerCard) - 17, Hero(play).Health.Current);
    }

    // A turn it only guards keeps the hours for the blow after.
    [Fact]
    public void A_guarding_turn_keeps_the_hours()
    {
        using var play = Fight("hold_the_present_open");
        var curator = Curator(play).Id;
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, curator);
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Curator(play), CuratorOfMisplacedHours.KeptHoursId));
    }
}
