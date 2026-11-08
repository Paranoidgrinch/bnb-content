using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The character-unspecific Act-I cards in live fights. Each test is about a RULE the design states, not a
// number: that a Lien resolution takes only what is there, that Citation is cashed out before it is removed,
// that a card can read what an enemy means to do, that a Rite watches the other side of the fight.
public class GeneralCardTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero
    private const string AttackIntent = "heavy_impression";  // the Embossed Seal's plain attack

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    private static (RunPlayback, InteractiveRunSession, CombatantId) Fight(params string[] deck) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 9), deck.ToList());

    // "Requires at least 6 Block. Lose 6 Block. Gain 3 Ward Wax."
    [Fact]
    public void Tallow_reserve_only_trades_when_there_is_block_to_trade()
    {
        var (play, session, enemyId) = Fight(
            "tallow_reserve", "tallow_reserve", "deskward", "paper_cut", "paper_cut");

        // Nothing guarded yet: the trade does not happen and nothing is lost.
        Play(play, session, "tallow_reserve", enemyId);
        Assert.Equal(0, Block(Hero(play)));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.WardWax));

        Play(play, session, "deskward", enemyId); // 8 Block
        Play(play, session, "tallow_reserve", enemyId);
        Assert.Equal(2, Block(Hero(play)));
        Assert.Equal(6, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    // "Deal 16 damage. If this is the first card you play this turn, deal 10 additional damage."
    [Fact]
    public void Dawn_summons_hits_harder_when_nothing_came_before_it()
    {
        // Two fights rather than two plays: 26 and then 16 would kill the Tablet before it could be measured.
        var (play, session, enemyId) = Fight("dawn_summons", "paper_cut", "paper_cut");
        var health = Enemy(play, enemyId).Health.Current;
        Play(play, session, "dawn_summons", enemyId);
        Assert.Equal(health - 26, Enemy(play, enemyId).Health.Current);
        play.Dispose();

        var (play2, session2, enemy2) = Fight("paper_cut", "dawn_summons", "paper_cut");
        Play(play2, session2, "paper_cut", enemy2);
        var health2 = Enemy(play2, enemy2).Health.Current;
        Play(play2, session2, "dawn_summons", enemy2);
        Assert.Equal(health2 - 16, Enemy(play2, enemy2).Health.Current);
        play2.Dispose();
    }

    // "Your next card costs 1 less Energy. After it is played, the next card you play this combat costs 1 more."
    [Fact]
    public void False_signature_is_paid_for_by_the_card_after_it()
    {
        var (play, session, enemyId) = Fight("false_signature", "paper_cut", "paper_cut", "paper_cut");

        Play(play, session, "false_signature", enemyId);

        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(energy, Hero(play).Resources[StandardCombatIds.EnergyResource].Current); // 1 − 1

        energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(energy - 2, Hero(play).Resources[StandardCombatIds.EnergyResource].Current); // 1 + 1

        energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(energy - 1, Hero(play).Resources[StandardCombatIds.EnergyResource].Current); // back to normal
        play.Dispose();
    }

}
