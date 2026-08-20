using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The keyword substrate, proved in LIVE fights out of the real converted game: Paperwork's timing, Doubt's
// per-action spend, and the general statuses that were authorable without further engine work (Censure, Lien,
// Blood Ink, Ward Wax). Seal and Ratified are proved by the cards that apply them, once those exist.
//
// Most probes use the Ordinance Tablet's "Stone Precedent", which only guards the Tablet itself — so whatever
// the hero's HP does is the keyword under test and nothing else. The 10 Block it raises is also a free check
// that Paperwork really ignores Block.
public class KeywordStatusTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string FilesDoubt = "etched_subsection"; // 7 damage + 1 Doubt on the hero

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

    // "At the end of the affected enemy's turn, it loses HP equal to its current Paperwork. Ignores Block,
    // does not decay." Filed during the player's turn, it tolls when the ENEMY's turn ends — not at its start,
    // which is where this port used to put it.
    [Fact]
    public void Paperwork_tolls_at_the_end_of_its_bearers_turn_and_never_decays()
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 9);
        var (play, session, enemyId) = FightProbe.Start(probe, deck: Enumerable.Repeat("permit_a38", 12).ToList());

        var full = Enemy(play, enemyId).Health.Current;
        Play(play, session, "permit_a38", enemyId);
        Assert.Equal(5, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Paperwork));
        Assert.Equal(full, Enemy(play, enemyId).Health.Current); // the filing is not the toll

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // The Tablet's turn came and went: 5 HP through 10 Block, and the Paperwork is still 5 deep.
        Assert.Equal(10, Block(Enemy(play, enemyId)));
        Assert.Equal(full - 5, Enemy(play, enemyId).Health.Current);
        Assert.Equal(5, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Paperwork));

        // …and again the next turn, unabated.
        play.CombatDriver.EndTurn();
        Assert.Equal(full - 10, Enemy(play, enemyId).Health.Current);
        play.Dispose();
    }

    // Paperwork is HP LOSS, not an attack: the bearer's own Doubt cannot shrink it.
    [Fact]
    public void Paperwork_is_hp_loss_that_doubt_cannot_touch()
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 9, (Keywords.Doubt, 3));
        var (play, session, enemyId) = FightProbe.Start(probe, deck: Enumerable.Repeat("permit_a38", 12).ToList());

        var full = Enemy(play, enemyId).Health.Current;
        Play(play, session, "permit_a38", enemyId);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // 5, not 3 — a Direct-restricted modifier never sees a tick.
        Assert.Equal(full - 5, Enemy(play, enemyId).Health.Current);
        play.Dispose();
    }

    // "The next X Attack actions each deal 25% less damage. One stack per ACTION." A Paper Cut is one action,
    // so it spends exactly one Doubt.
    [Fact]
    public void Doubt_softens_a_card_and_is_spent_once_for_it()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9, (Keywords.Doubt, 2));
        var (play, session, enemyId) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        var full = Enemy(play, enemyId).Health.Current;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(full - 4, Enemy(play, enemyId).Health.Current); // 6 → 4
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));

        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(full - 8, Enemy(play, enemyId).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Doubt));

        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(full - 14, Enemy(play, enemyId).Health.Current); // Doubt gone: the full 6
        play.Dispose();
    }

    // "Censure X: prevent up to X stacks of a Status the bearer would not want, spending one stack per stack
    // prevented." On the hero that means debuffs — here the Doubt the Tablet's Etched Subsection files.
    [Fact]
    public void Censure_refuses_the_debuff_an_enemy_files_and_pays_for_it()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, FilesDoubt, energy: 9, (Keywords.Censure, 1));
        var (play, session, _) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // The 7 damage lands — Censure stops statuses, not hits — but the Doubt never arrives, and the
        // Censure that refused it is spent.
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Censure));
        play.Dispose();
    }

    [Fact]
    public void Without_censure_the_same_attack_files_its_doubt()
    {
        var probe = FightProbe.Solo(Quiet, FilesDoubt, energy: 9);
        var (play, session, _) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();
    }

    // "Lien X: at the end of the holder's turn, remove up to X remaining Block; the holder loses that much HP;
    // Lien is reduced by what it took."
    [Fact]
    public void Lien_turns_unspent_block_into_hp_loss_and_shrinks_by_what_it_took()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9, (Keywords.Lien, 5));
        var (play, session, enemyId) = FightProbe.Start(
            probe, deck: Enumerable.Repeat("cower_behind_a_desk", 12).ToList());

        var full = Hero(play).Health.Current;
        Play(play, session, "cower_behind_a_desk", enemyId); // 5 Block
        Assert.Equal(5, Block(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // The claim took all 5 Block and 5 HP with it, and cleared itself doing so.
        Assert.Equal(full - 5, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Lien));
        play.Dispose();
    }

    // "If the holder has no remaining Block, Lien does not decay."
    [Fact]
    public void Lien_does_not_decay_when_there_is_no_block_to_take()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9, (Keywords.Lien, 4));
        var (play, session, _) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        var full = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(full, Hero(play).Health.Current);
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.Lien));
        play.Dispose();
    }

    // Less Block than claim: only what there is, and the rest of the Lien stays outstanding.
    [Fact]
    public void Lien_takes_only_the_block_that_is_there_and_the_rest_stays_outstanding()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9, (Keywords.Lien, 8));
        var (play, session, enemyId) = FightProbe.Start(
            probe, deck: Enumerable.Repeat("cower_behind_a_desk", 12).ToList());

        var full = Hero(play).Health.Current;
        Play(play, session, "cower_behind_a_desk", enemyId); // 5 Block against an 8-deep claim

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(full - 5, Hero(play).Health.Current);
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.Lien));
        play.Dispose();
    }

    // "Blood Ink X: whenever ANOTHER status on the holder loses one or more stacks in a single event, the
    // holder loses X HP, then loses 1 Blood Ink." One event, one trigger — and never its own stacks.
    [Fact]
    public void Blood_ink_bleeds_when_another_status_loses_stacks_and_never_for_its_own()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9,
            (Keywords.BloodInk, 3), (Keywords.Doubt, 2));
        var (play, session, enemyId) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        var full = Hero(play).Health.Current;

        // Spending a Doubt is a stack loss on another status: 3 HP, and one Blood Ink gone. The Blood Ink's
        // OWN loss in that same breath must not bleed again.
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(full - 3, Hero(play).Health.Current);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Keywords.BloodInk));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));

        // The last Doubt going is an expiry, not a stack change — and it bleeds just the same.
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(full - 5, Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.BloodInk));
        play.Dispose();
    }

    // "Ward Wax X: at the start of your turn gain X Block. After the enemy turn lose 1 stack — or 2 if any
    // unblocked Attack damage got through."
    [Fact]
    public void Ward_wax_pays_block_each_turn_and_loses_one_stack_for_a_quiet_enemy_turn()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9, (Keywords.WardWax, 4));
        var (play, session, _) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // Nothing got through, so one stack: 4 → 3, and the new turn opens with 3 Block.
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(3, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Ward_wax_loses_two_stacks_when_an_attack_gets_through()
    {
        var probe = FightProbe.SoloAgainstHero(Quiet, FilesDoubt, energy: 9, (Keywords.WardWax, 5));
        var (play, session, _) = FightProbe.Start(probe, deck: Enumerable.Repeat("paper_cut", 12).ToList());

        // Turn 1 opens with 5 Block from the Wax; Etched Subsection's 7 breaks through it.
        Assert.Equal(5, Block(Hero(play)));
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }
}
