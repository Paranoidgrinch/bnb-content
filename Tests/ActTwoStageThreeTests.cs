using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Whispering Catalogue. A Reference is a debt with a name on it: play the cited card and it is
// fulfilled, leave it in the hand you put down and it costs you an Overdue owed to the one who cited you.
// That is why the whole rule lives on the CITING ENEMY — the Overdue has to come from it.
public class ActTwoStageThreeTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Marked(RunPlayback play, string mark) =>
        Enum.GetValues<CardZone>()
            .SelectMany(zone => play.CombatDriver!.Current!.State
                .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(zone))
            .Count(card => card.HasMark(new TagId(mark)));

    [Fact]
    public void The_entry_cites_a_card_in_your_hand()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("second_person_entry", "you_are_here"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Assert.Equal(1, Marked(play, ActTwo.EntryReferenceMark));
        play.Dispose();
    }

    // Play what you were cited for and the citation is simply satisfied — no mark, nothing owed.
    [Fact]
    public void Playing_the_cited_card_fulfils_the_citation()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("second_person_entry", "you_are_here"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        var cited = play.CombatDriver!.Current!.Hand
            .First(c => c.HasMark(new TagId(ActTwo.EntryReferenceMark)));
        play.CombatDriver.PlayCard(cited.Id, enemyId);
        Assert.Null(session.Error);

        Assert.Equal(0, Marked(play, ActTwo.EntryReferenceMark));
        play.CombatDriver.EndTurn(); // the Entry's turn: nothing to collect

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        play.Dispose();
    }

    // Put the hand down with the citation unanswered and the Entry files it against you.
    [Fact]
    public void An_unanswered_citation_is_owed_to_the_one_who_cited_you()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("second_person_entry", "you_are_here"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn(); // the cited card goes down unplayed

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        var overdue = Hero(play).Statuses.Single(s => s.DefinitionId == new StatusDefinitionId(ActTwo.OverdueId));
        Assert.NotEqual(play.CombatDriver.Current!.HeroId, overdue.SourceCombatantId); // owed to the Entry
        play.Dispose();
    }

    // "…or another card with the same Base Cost." The Orphan Citation will take a stand-in.
    [Fact]
    public void The_orphan_citation_accepts_a_card_enough_like_the_one_it_cited()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("orphan_citation", "missing_source"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Assert.Equal(1, Marked(play, ActTwo.CitationReferenceMark));

        // Every card in this deck is the same kind, so any of them is a stand-in of the same price — and one
        // that is NOT the cited card proves the second path rather than the first.
        var standIn = play.CombatDriver!.Current!.Hand
            .First(c => !c.HasMark(new TagId(ActTwo.CitationReferenceMark)));
        play.CombatDriver.PlayCard(standIn.Id, enemyId);
        Assert.Null(session.Error);

        Assert.Equal(0, Marked(play, ActTwo.CitationReferenceMark));
        play.Dispose();
    }

    // "Two consecutive cards of the same Base Cost" — the alphabet learns the price.
    [Fact]
    public void The_alphabet_learns_a_price_paid_twice_in_a_row()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("fanged_alphabet", "re_index"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Play(play, session, enemyId);
        Play(play, session, enemyId);

        var learned = Hero(play).Counters
            .Single(c => c.Key == new CounterId("alphabet_learned_cost")).Value;
        Assert.True(learned > 0, "the alphabet should have learned the price it saw twice");
        play.Dispose();
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
