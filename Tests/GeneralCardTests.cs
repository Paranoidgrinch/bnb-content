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

    // "Choose one: gain 2 Censure; or apply 2 Censure to an enemy." One card, both sides of a status that
    // reads differently depending on who wears it.
    [Fact]
    public void Malediction_review_offers_the_censure_to_either_side()
    {
        var (play, session, enemyId) = Fight("malediction_review", "paper_cut", "paper_cut");

        Play(play, session, "malediction_review", enemyId);
        Assert.Equal(["gain 2 Censure", "apply 2 Censure to an enemy"], play.CombatDriver!.PendingOptionChoice);

        play.CombatDriver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);

        Assert.Equal(6, Block(Hero(play)));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Censure));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Censure));
        play.Dispose();
    }

    // "Deal 6 damage. Then immediately resolve up to 5 Lien on the target." The resolution takes Block and
    // the same in HP, and reduces the Lien by exactly what it took — capped at 5 however deep the Lien is.
    [Fact]
    public void Foreclosure_calls_in_the_lien_it_can_reach_and_no_more()
    {
        var (play, session, enemyId) = Fight("mortgage_sigil", "foreclosure", "paper_cut");

        // Let the Tablet raise its 10 Block first, so there is something for the claim to take. The Lien is
        // filed AFTER that, by a card that does no damage of its own.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(10, Block(Enemy(play, enemyId)));

        Play(play, session, "mortgage_sigil", enemyId); // 3 Lien, no damage
        var health = Enemy(play, enemyId).Health.Current;
        Play(play, session, "foreclosure", enemyId);

        // 6 struck at the guard (10 → 4), then the claim takes what it can: 3 Block, 3 HP, and the Lien clears.
        Assert.Equal(1, Block(Enemy(play, enemyId)));
        Assert.Equal(health - 3, Enemy(play, enemyId).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Lien));
        play.Dispose();
    }

    // "Remove all Citation from an enemy. Gain 2 Block per Citation removed." How much was there has to be
    // read before it is taken away.
    [Fact]
    public void Contempt_finding_pays_for_what_it_erases()
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 9, (Keywords.Citation, 4));
        var (play, session, enemyId) = FightProbe.Start(probe, ["contempt_finding", "paper_cut", "paper_cut"]);

        Play(play, session, "contempt_finding", enemyId);
        Assert.Equal(8, Block(Hero(play)));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Citation));
        play.Dispose();
    }

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
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    // "Deal 7 damage. If the target still has Block after this attack, apply 4 Lien."
    [Fact]
    public void Forfeit_seal_files_its_lien_only_against_a_guard_that_held()
    {
        var (play, session, enemyId) = Fight("forfeit_seal", "forfeit_seal", "paper_cut");

        // Turn 1: the Tablet has no Block yet, so nothing is owed.
        Play(play, session, "forfeit_seal", enemyId);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Lien));

        // After its turn it stands behind 10: the attack does not break it, so the Lien is filed.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Play(play, session, "forfeit_seal", enemyId);
        Assert.Equal(4, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Lien));
        play.Dispose();
    }

    // "Apply 3 Citation. If the target currently intends a non-damaging action, draw 1 card."
    [Fact]
    public void Witchmark_citation_reads_what_the_enemy_means_to_do()
    {
        // Seven cards, so the draw pile still holds something when the card asks for one.
        string[] deck = ["witchmark_citation", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"];

        // Guarding is not a damaging action: the card is drawn, so the hand is where it started.
        var (play, session, enemyId) = Fight(deck);
        var hand = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, "witchmark_citation", enemyId);
        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Citation));
        Assert.Equal(hand, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();

        // An attack is a damaging action: no card, so the hand is one down.
        var (play2, session2, enemy2) = FightProbe.Start(
            FightProbe.Solo("embossed_seal", AttackIntent, energy: 9), deck.ToList());
        var hand2 = play2.CombatDriver!.Current!.Hand.Count;
        Play(play2, session2, "witchmark_citation", enemy2);
        Assert.Equal(hand2 - 1, play2.CombatDriver.Current!.Hand.Count);
        play2.Dispose();
    }

    // "Until your next turn, if the target performs a damaging action, gain 7 Block." A mark on the ENEMY
    // that pays the PLAYER — a rule reaching across the fight.
    [Fact]
    public void Silent_hearing_pays_when_the_enemy_strikes()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("embossed_seal", AttackIntent, energy: 9),
            ["silent_hearing", "paper_cut", "paper_cut"]);

        Play(play, session, "silent_hearing", enemyId);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Citation));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // The Seal struck, so the hearing paid — after the player's own turn start swept the board, which is
        // why the debt waits for the draw rather than granting Block mid-enemy-turn.
        Assert.Equal(7, Block(Hero(play)));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), GeneralRites.SilentHearing));
        play.Dispose();
    }

    // "The next time the target gains Block before the end of its next turn, apply 3 additional Lien."
    [Fact]
    public void Mortgage_sigil_charges_the_next_guard_the_enemy_raises()
    {
        var (play, session, enemyId) = Fight("mortgage_sigil", "paper_cut", "paper_cut");

        Play(play, session, "mortgage_sigil", enemyId);
        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Lien));

        play.CombatDriver!.EndTurn(); // the Tablet guards itself — and pays for it
        Assert.Null(session.Error);

        // 3 more Lien filed, then the Lien resolved against the very Block that triggered it.
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), GeneralRites.MortgageSigil));
        Assert.True(FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Lien) < 6,
            "the claim resolved against the Block it charged for");
        play.Dispose();
    }

    // "The first time each turn you apply a negative Status to an enemy that does not already have that
    // Status, apply 1 additional stack of it." New to the enemy, and once a turn.
    [Fact]
    public void Notary_beetle_seeds_a_status_that_was_not_there_and_only_the_first_one()
    {
        var (play, session, enemyId) = Fight(
            "notary_beetle", "cursed_addendum", "cursed_addendum", "seal_of_concern", "paper_cut");

        Play(play, session, "notary_beetle", enemyId);

        // Paperwork is new to the Tablet: 2 filed, 1 seeded.
        Play(play, session, "cursed_addendum", enemyId);
        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Paperwork));

        // A second filing the same turn is neither new nor first: 2 more, no seed.
        Play(play, session, "cursed_addendum", enemyId);
        Assert.Equal(5, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Paperwork));
        play.Dispose();
    }

    // "Whenever Lien removes Block from an enemy, apply 1 Citation for every 3 Block removed, maximum 3."
    [Fact]
    public void Usurers_moon_files_citation_for_the_block_a_lien_takes()
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 9, (Keywords.Lien, 9));
        var (play, session, enemyId) = FightProbe.Start(probe, ["usurers_moon", "paper_cut", "paper_cut"]);

        Play(play, session, "usurers_moon", enemyId);
        play.CombatDriver!.EndTurn(); // the Tablet guards for 10, then its Lien takes 9 of it
        Assert.Null(session.Error);

        // 9 Block taken → 3 Citation, which is also the ceiling.
        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Citation));
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
