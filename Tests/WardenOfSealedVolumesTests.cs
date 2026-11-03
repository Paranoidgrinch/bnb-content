using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Warden of Sealed Volumes (user, 2026-11-03): every turn it seals a card of your hand, three at most; each
// sealed card adds 3 to its attacks; 20 damage to it in one turn frees them all.
public class WardenOfSealedVolumesTests
{
    private static CombatantState Warden(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Custody(RunPlayback play) => FightProbe.StacksOf(Warden(play), WardenOfSealedVolumes.CustodyId);

    private static int Sealed(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current.HeroId)
            .GetCardsInZone(CardZone.BanishedPile).Count(c => c.HasMark(new TagId(WardenOfSealedVolumes.SealedMark)));

    private static RunPlayback Fight(string intent) =>
        FightProbe.Start(FightProbe.Solo(WardenOfSealedVolumes.EnemyId, intent, 9),
            deck: [.. Enumerable.Repeat("paper_cut", 24)], health: 600).Play;

    [Fact]
    public void It_seals_one_card_a_turn_and_three_at_most()
    {
        using var play = Fight("inspect_the_claim");
        Assert.Equal(1, Sealed(play));
        Assert.Equal(1, Custody(play));
        Assert.Equal(4, play.CombatDriver!.Current!.Hand.Count);
        for (var turn = 0; turn < 3; turn++)
            play.CombatDriver.EndTurn();
        Assert.Equal(WardenOfSealedVolumes.CustodyMaximum, Sealed(play));
        Assert.Equal(WardenOfSealedVolumes.CustodyMaximum, Custody(play));
        Assert.Equal(5, play.CombatDriver.Current!.Hand.Count);
    }

    [Fact]
    public void Every_sealed_card_adds_three_to_its_attacks()
    {
        using var play = Fight("deny_immediate_access");
        play.CombatDriver!.EndTurn();
        Assert.Equal(600 - (15 + WardenOfSealedVolumes.StrengthPerCard), Hero(play).Health.Current);
    }

    // Three Paper Cuts are 18 and free nothing; the fourth makes 24 and every sealed card comes home.
    [Fact]
    public void Twenty_damage_in_one_turn_frees_every_card()
    {
        using var play = Fight("inspect_the_claim");
        var warden = Warden(play).Id;
        for (var i = 0; i < 3; i++)
            play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, warden);
        Assert.Equal(1, Sealed(play));
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, warden);
        Assert.Equal(0, Sealed(play));
        Assert.Equal(0, Custody(play));
        Assert.Single(play.CombatDriver.Current!.Hand);
    }
}
