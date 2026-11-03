using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Whispering Catalogue (user, 2026-11-03): it mirrors the turn you just played — 3 damage for each Deed, 4 Block
// for each Working — and a turn with nothing played is answered with 10.
public class WhisperingCatalogueTests
{
    private const string Deed = "paper_cut";
    private const string Working = "strong_binder";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Catalogue(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static RunPlayback Fight(params string[] deck) =>
        FightProbe.Start(FightProbe.Solo(WhisperingCatalogue.EnemyId, "read_you_back", 9),
            deck: [.. deck], health: 600).Play;

    private static void Play(RunPlayback play, string card)
    {
        var instance = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == card);
        play.CombatDriver.PlayCard(instance.Id, Catalogue(play).Id);
    }

    [Fact]
    public void It_mirrors_your_deeds_as_damage_and_your_workings_as_block()
    {
        using var play = Fight(Deed, Deed, Working, Working, Working, Deed, Deed, Working, Working, Working);
        Play(play, Deed);
        Play(play, Deed);
        Play(play, Working);
        Assert.Equal(2 * WhisperingCatalogue.DamagePerDeed, FightProbe.StacksOf(Catalogue(play), WhisperingCatalogue.MirroredBlowsId));
        Assert.Equal(WhisperingCatalogue.BlockPerWorking, FightProbe.StacksOf(Catalogue(play), WhisperingCatalogue.MirroredGuardId));
        var health = Hero(play).Health.Current;
        var block = Hero(play).DefensivePools[StandardCombatIds.BlockDefensivePool].Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(health - Math.Max(0, 2 * WhisperingCatalogue.DamagePerDeed - block), Hero(play).Health.Current);
        Assert.Equal(WhisperingCatalogue.BlockPerWorking, Block(Catalogue(play)));
        // …and the page is blank for the next turn.
        Assert.Equal(0, FightProbe.StacksOf(Catalogue(play), WhisperingCatalogue.MirroredBlowsId));
    }

    [Fact]
    public void A_turn_with_nothing_played_is_answered_with_ten()
    {
        using var play = Fight([.. Enumerable.Repeat(Deed, 12)]);
        play.CombatDriver!.EndTurn();
        Assert.Equal(600 - WhisperingCatalogue.IdleDamage, Hero(play).Health.Current);
    }
}
