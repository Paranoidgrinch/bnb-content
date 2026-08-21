using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — Scriptorium of Errata. The Comma marks two clauses and cares which order you read them in; the
// Doppelgänger will not let a redaction rest where it landed.
public class ActTwoStageSixTests
{
    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static int Health(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id).Health.Current;

    private static int Marked(RunPlayback play, string mark) =>
        Enum.GetValues<CardZone>().SelectMany(zone => Zones(play).GetCardsInZone(zone))
            .Count(card => card.HasMark(new TagId(mark)));

    // "Mark two different cards as Clause A and Clause B."
    [Fact]
    public void The_comma_marks_two_different_clauses()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("fatal_comma", "editorial_stay", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Assert.Equal(1, Marked(play, ActTwo.ClauseAMark));
        Assert.Equal(1, Marked(play, ActTwo.ClauseBMark));

        var a = play.CombatDriver!.Current!.Hand.Single(c => c.HasMark(new TagId(ActTwo.ClauseAMark)));
        var b = play.CombatDriver.Current!.Hand.Single(c => c.HasMark(new TagId(ActTwo.ClauseBMark)));
        Assert.NotEqual(a.Id, b.Id); // two DIFFERENT cards, which is the whole puzzle
        play.Dispose();
    }

    // "A before B: Fatal Comma takes 8 direct damage." The reward lands as the pair completes.
    [Fact]
    public void Reading_the_clauses_in_order_cuts_the_comma()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("fatal_comma", "editorial_stay", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        var a = play.CombatDriver!.Current!.Hand.Single(c => c.HasMark(new TagId(ActTwo.ClauseAMark)));
        var b = play.CombatDriver.Current!.Hand.Single(c => c.HasMark(new TagId(ActTwo.ClauseBMark)));

        play.CombatDriver.PlayCard(a.Id, enemyId);
        var afterA = Health(play, enemyId);
        play.CombatDriver.PlayCard(b.Id, enemyId);
        var afterB = Health(play, enemyId);
        Assert.Null(session.Error);

        // The second clause lands its own damage AND the Comma's 8 on top.
        Assert.True(afterA - afterB >= 8 + 1, "reading A then B should have cut the Comma for 8 extra");
        play.Dispose();
    }

    // …and the wrong order does not. B first is simply a missed reward.
    [Fact]
    public void Reading_them_backwards_earns_nothing()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("fatal_comma", "editorial_stay", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        var a = play.CombatDriver!.Current!.Hand.Single(c => c.HasMark(new TagId(ActTwo.ClauseAMark)));
        var b = play.CombatDriver.Current!.Hand.Single(c => c.HasMark(new TagId(ActTwo.ClauseBMark)));

        var before = Health(play, enemyId);
        play.CombatDriver.PlayCard(b.Id, enemyId);
        var afterB = Health(play, enemyId);
        play.CombatDriver.PlayCard(a.Id, enemyId);
        var afterA = Health(play, enemyId);
        Assert.Null(session.Error);

        // Both plays cost the same: no bonus either way round.
        Assert.Equal(before - afterB, afterB - afterA);
        play.Dispose();
    }

    // "If neither clause is played: 1 Overdue from Fatal Comma."
    [Fact]
    public void Leaving_both_clauses_unread_is_owed_to_the_comma()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("fatal_comma", "editorial_stay", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn(); // neither clause read

        Assert.Equal(1, FightProbe.StacksOf(
            play.CombatDriver.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId),
            ActTwo.OverdueId));
        play.Dispose();
    }

    // "The first Redacted card played each turn: the redaction moves on to another card in hand."
    [Fact]
    public void The_doppelganger_moves_a_redaction_along()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("errata_doppelganger", "errata_transfer", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();  // the Doppelgänger redacts a card in the draw pile
        play.CombatDriver.EndTurn();   // …and it is drawn

        var redacted = play.CombatDriver.Current!.Hand
            .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.NotNull(redacted);
        var markedBefore = Marked(play, ActTwo.RedactedMark);

        play.CombatDriver.PlayCard(redacted!.Id, enemyId);
        Assert.Null(session.Error);

        // The played card is clean, and the redaction has landed on something else instead.
        Assert.False(Zones(play).GetCard(redacted.Id).HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.Equal(markedBefore, Marked(play, ActTwo.RedactedMark));
        play.Dispose();
    }
}
