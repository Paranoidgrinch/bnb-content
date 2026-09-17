using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — the Restricted Annex. The Checkout Codex keeps one of your cards behind the desk.
//
// ⚠ "In hand but unplayable" is not expressible (see ActTwo.BehindTheDesk), so the desk is a PLACE: the card
// leaves the hand for the top of the draw pile. That is what these tests read — the hand is one card lighter
// and something on the pile wears the desk's mark.
public class ActTwoStageSevenDeskTests
{
    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static int Marked(RunPlayback play) =>
        Enum.GetValues<CardZone>().SelectMany(zone => Zones(play).GetCardsInZone(zone))
            .Count(card => card.HasMark(new TagId(ActTwo.BehindTheDeskMark)));

    private static int Overdue(RunPlayback play) =>
        FightProbe.StacksOf(
            play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId),
            ActTwo.OverdueId);

    // "After normal draw, place one card visibly Behind the Desk. It is temporarily unavailable."
    [Fact]
    public void One_card_is_kept_behind_the_desk()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("checkout_codex", "check_you_out", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        Assert.Null(session.Error);
        Assert.Equal(1, Marked(play));                        // exactly one, and only one
        Assert.DoesNotContain(play.CombatDriver!.Current!.Hand,
            c => c.HasMark(new TagId(ActTwo.BehindTheDeskMark)));   // not in the hand: that is the point
        play.Dispose();
    }

    // "Wait Properly: play another card first. Then the Behind-the-Desk card returns normally to hand."
    [Fact]
    public void Playing_something_else_gets_the_card_handed_back()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("checkout_codex", "check_you_out", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        var before = play.CombatDriver!.Current!.Hand.Count;
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemyId);
        Assert.Null(session.Error);

        // One card left the hand to be played and one came back off the desk, so the hand is where it was.
        Assert.Equal(before, play.CombatDriver.Current!.Hand.Count);
        Assert.Equal(0, Marked(play));                        // the desk is empty; the mark is spent
        play.Dispose();
    }

    // "End the Turn Without Retrieval: apply 1 Overdue from the Checkout Codex." Checked at the Codex's own
    // turn start, which is the moment the hand is down and anything still marked was provably never asked for.
    [Fact]
    public void Leaving_without_asking_is_filed_against_you()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("checkout_codex", "check_you_out", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        var owed = Overdue(play);
        play.CombatDriver!.EndTurn();                         // nothing played: the desk keeps it
        Assert.Null(session.Error);

        Assert.True(Overdue(play) > owed, "the Codex should have filed the unasked-for card against you");
        play.Dispose();
    }
}
