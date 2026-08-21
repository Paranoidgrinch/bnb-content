using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Hall of Returns, proved in live fights. The act's pressure is SOURCE-BOUND: Overdue is not one
// debt the player owes the room, it is a separate debt owed to each enemy that filed it, and each collects its
// own. These tests are mostly about that word "own".
public class ActTwoStageOneTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // Each filing is its own instance, because the threshold is "2 from the same source" and merged stacks
    // remember only the last source.
    [Fact]
    public void Every_filing_of_overdue_is_its_own_debt()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("brass_maw_of_returns", "return_intake"));

        play.CombatDriver!.EndTurn(); // Return Intake: 9 damage + 1 Overdue
        play.CombatDriver.EndTurn();  // again

        var overdue = Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActTwo.OverdueId))
            .ToList();
        Assert.Equal(2, overdue.Count);
        Assert.All(overdue, s => Assert.Equal(1, s.Stacks));
        play.Dispose();
    }

    // At 2 owed, the source collects: it takes back what it is owed and files a Paperwork for the trouble.
    [Fact]
    public void At_two_owed_the_source_collects_and_files_paperwork()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("brass_maw_of_returns", "return_intake"));

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two Overdue standing
        play.CombatDriver.EndTurn(); // the Maw's next turn opens: Delinquency

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }

    // "Whenever Brass Maw resolves its Delinquency: gain 1 Return Parcel." The parcels ride on its next direct
    // attack, +5 each, and that attack spends them all.
    [Fact]
    public void The_maw_spits_back_what_it_collected()
    {
        var (play, _, enemyId) = FightProbe.Start(
            FightProbe.Solo("brass_maw_of_returns", "return_intake"));

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two Overdue standing

        var before = Hero(play).Health.Current;
        // The Maw's turn: Delinquency collects and hands it a parcel, and the attack that follows in the SAME
        // turn is the "next direct attack" the parcel was waiting for.
        play.CombatDriver.EndTurn();

        Assert.Equal(before - 14, Hero(play).Health.Current); // 9 base + 5 for the parcel
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), ActTwo.ReturnParcelId)); // and spent by it
        play.Dispose();
    }

    // "Whenever this enemy's Delinquency fully resolves: immediately apply 1 new Overdue from this same
    // source." The loop never quite closes.
    [Fact]
    public void The_ouroboros_re_owes_itself_the_moment_it_collects()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("dead_letter_ouroboros", "forwarding_loop"));

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two Overdue
        play.CombatDriver.EndTurn(); // collected — and immediately re-owed

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId)); // 1 re-owed + 1 from this turn's intent
        play.Dispose();
    }

    // The Object files you by however you OPEN a turn; keep to that category and it stores itself away.
    [Fact]
    public void Keeping_to_one_category_lets_the_object_file_itself_safely()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("object_listed_as_other", "miscellaneous_storage"),
            deck: [.. Enumerable.Repeat("paper_cut", 10)]);

        Play(play, session, "paper_cut", enemyId); // a Deed opens the turn
        Play(play, session, "paper_cut", enemyId); // …and only Deeds follow
        var before = Block(Enemy(play, enemyId));
        play.CombatDriver!.EndTurn();

        Assert.True(Block(Enemy(play, enemyId)) > before, "the Object should have stored itself away");
        play.Dispose();
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
