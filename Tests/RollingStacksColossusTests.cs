using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Rolling Stacks Colossus: its Distance drops by one each of its turns and at 0 it crushes for 20, back at 3.
// Since 2026-11-08 (CONTENT_REVIEW E7) rolling it back costs a card: a Wheel Chock exhausts one from the hand.
public class RollingStacksColossusTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Colossus(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Distance(RunPlayback play) => FightProbe.StacksOf(Colossus(play), RollingStacksColossus.DistanceId);

    private static CardInstance? Chock(RunPlayback play) =>
        play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == RollingStacksColossus.WheelChockId);

    private static IReadOnlyList<CardInstance> Exhausted(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(CardZone.ExhaustPile);

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

    // At its starting distance there is nothing to roll back, so no chock is offered.
    [Fact]
    public void No_chock_while_it_stands_at_three()
    {
        using var play = Fight();
        Assert.Null(Chock(play));
        play.CombatDriver!.EndTurn();
        Assert.NotNull(Chock(play));
    }

    // The chock exhausts the card the player chooses and rolls it back one step; damage no longer does.
    [Fact]
    public void A_wheel_chock_exhausts_a_card_and_rolls_it_back()
    {
        using var play = Fight("displace_the_ladder", energy: 9);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Distance(play));

        var colossus = Colossus(play).Id;
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut").Id, colossus);
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut").Id, colossus);
        Assert.Equal(1, Distance(play));                 // 12 damage pushes nothing any more

        play.CombatDriver.PlayCard(Chock(play)!.Id, null);
        var offered = play.CombatDriver.PendingCardChoice;
        Assert.NotNull(offered);
        var spent = offered![0].Id;
        play.CombatDriver.SupplyCardChoice([spent]);
        Assert.Equal(2, Distance(play));
        Assert.Contains(Exhausted(play), c => c.Id == spent);
        Assert.Null(Chock(play));                        // one a turn

        play.CombatDriver.EndTurn();
        Assert.Equal(1, Distance(play));
    }
}
