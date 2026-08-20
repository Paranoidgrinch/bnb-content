using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Bureaucrat's reward cards, in live fights out of the real converted game. The point of each test is a
// RULE, not a number: that a Seal converts at three, that a Queue card lands a turn later, that a conditional
// clause reads what it claims to read.
public class BureaucratCardTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

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

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Fight(
        params string[] deck) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 9), deck.ToList());

    // "Whenever an enemy reaches 3 Seal, remove exactly 3 Seal and trigger a Ratify event. Excess Seal
    // remains." Three applications of Seal of Concern get there; the third converts.
    [Fact]
    public void Three_seals_ratify_the_enemy_and_leave_the_excess()
    {
        var (play, session, enemyId) = Fight(Enumerable.Repeat("seal_of_concern", 10).ToArray());

        Play(play, session, "seal_of_concern", enemyId);
        Play(play, session, "seal_of_concern", enemyId);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));

        Play(play, session, "seal_of_concern", enemyId);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        play.Dispose();
    }

    // "Seal of Concern+ applies 2 Seal" — two of those is 4, which Ratifies and leaves 1 standing.
    [Fact]
    public void Seal_beyond_three_ratifies_and_the_rest_remains()
    {
        var (play, session, enemyId) = Fight(Enumerable.Repeat("seal_of_concern+", 10).ToArray());

        Play(play, session, "seal_of_concern+", enemyId);
        Play(play, session, "seal_of_concern+", enemyId);

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        play.Dispose();
    }

    // "Ratified: each Deed aimed at this enemy deals +3 total direct damage, until the end of your turn."
    [Fact]
    public void A_ratified_enemy_takes_three_more_from_every_deed_until_the_turn_ends()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9),
            ["seal_of_concern", "seal_of_concern", "seal_of_concern", "paper_cut", "paper_cut"]);

        for (var i = 0; i < 3; i++)
            Play(play, session, "seal_of_concern", enemyId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));

        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(before - 9, Enemy(play, enemyId).Health.Current); // 6 + 3

        // The window closes when the player's turn does.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        play.Dispose();
    }

    // "Notarial Press: apply 2 Seal. If this Ratifies the target, gain 5 Block." The bonus is owed only when
    // the Press is what carries the target over.
    [Fact]
    public void The_press_pays_its_block_only_when_it_is_the_one_that_ratifies()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9),
            ["notarial_press", "notarial_press", "notarial_press"]);

        Play(play, session, "notarial_press", enemyId); // 2 Seal — not there yet
        Assert.Equal(0, Block(Hero(play)));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));

        Play(play, session, "notarial_press", enemyId); // 4 → Ratify, 1 left over
        Assert.Equal(5, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        play.Dispose();
    }

    // "Queue: Deal 13 damage." Played now, felt at the start of the next turn.
    [Fact]
    public void A_queued_hex_lands_at_the_start_of_the_next_turn()
    {
        var (play, session, enemyId) = Fight("deferred_hex", "paper_cut", "paper_cut");

        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "deferred_hex", enemyId);
        Assert.Equal(before, Enemy(play, enemyId).Health.Current);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // The Tablet spent its turn raising 10 Block, and that Block is still standing when the Queue
        // resolves at the start of the player's turn — so 13 lands as 10 absorbed and 3 through.
        Assert.Equal(before - 3, Enemy(play, enemyId).Health.Current);
        Assert.Equal(0, Block(Enemy(play, enemyId)));
        play.Dispose();
    }

    // "Occult Precedent: gain 7 Block. If ANY enemy has Paperwork, gain 2 additional Block." — once, not once
    // per such enemy, and only when there is one.
    [Fact]
    public void Occult_precedent_pays_its_bonus_once_and_only_with_paperwork_on_the_table()
    {
        var (play, session, enemyId) = Fight("occult_precedent", "permit_a38", "occult_precedent");

        Play(play, session, "occult_precedent", enemyId);
        Assert.Equal(7, Block(Hero(play)));

        Play(play, session, "permit_a38", enemyId); // 5 Paperwork onto the Tablet
        Play(play, session, "occult_precedent", enemyId);
        Assert.Equal(7 + 9, Block(Hero(play)));
        play.Dispose();
    }

    // "Certified Kindling: Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional."
    // The Archive is recorded, which is what separates it from an ordinary exhaust.
    [Fact]
    public void Certified_kindling_takes_the_junk_and_records_the_archiving()
    {
        var (play, session, enemyId) = Fight("certified_kindling", "deskward", "paper_cut");

        Play(play, session, "deskward", enemyId); // 8 Block, and a Red Tape into the discard pile
        var afterDeskward = Block(Hero(play));

        // No Junk in HAND (the Red Tape went to the discard pile), so the Kindling pays only its base and
        // asks the player which card to Archive.
        Play(play, session, "certified_kindling", enemyId);
        Assert.Equal(afterDeskward + 4, Block(Hero(play)));

        var offered = play.CombatDriver!.PendingCardChoice;
        Assert.NotNull(offered);
        play.CombatDriver.SupplyCardChoice([offered![0].Id]);
        Assert.Null(session.Error);

        // Archiving is RECORDED — that record is what separates it from an ordinary exhaust.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Archived));
        Assert.Contains(play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId)
            .ExhaustPile, c => c.Id == offered[0].Id);
        play.Dispose();
    }
}
