using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Auditor of Returned Lives (user, 2026-11-03): every second turn is an Audit, and each card left in your hand
// when it ends costs you 3 HP.
public class AuditorOfReturnedLivesTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static bool Audited(RunPlayback play) =>
        Hero(play).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(AuditorOfReturnedLives.AuditDueId));

    // Its block intent: nothing else touches the hero's health.
    private static RunPlayback Fight() =>
        FightProbe.Start(FightProbe.Solo(AuditorOfReturnedLives.EnemyId, "open_the_account", 9),
            deck: [.. Enumerable.Repeat("paper_cut", 24)], health: 600).Play;

    [Fact]
    public void Every_second_turn_is_an_audit()
    {
        using var play = Fight();
        Assert.False(Audited(play));
        play.CombatDriver!.EndTurn();
        Assert.True(Audited(play));
        play.CombatDriver.EndTurn();
        Assert.False(Audited(play));
        play.CombatDriver.EndTurn();
        Assert.True(Audited(play));
    }

    // Two cards played of five: three left, nine health. An unaudited turn's hand costs nothing.
    [Fact]
    public void Each_card_left_in_hand_costs_three()
    {
        using var play = Fight();
        play.CombatDriver!.EndTurn();
        Assert.Equal(600, Hero(play).Health.Current);
        var auditor = play.CombatDriver.Current!.State.Combatants.First(c => c.Id != Hero(play).Id).Id;
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, auditor);
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, auditor);
        play.CombatDriver.EndTurn();
        Assert.Equal(600 - 3 * AuditorOfReturnedLives.CostPerCard, Hero(play).Health.Current);
    }
}
