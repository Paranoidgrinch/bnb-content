using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Misfiled Stacks. A misfiled card does not reach your hand: it is taken back as it arrives and
// something else is fetched in its place. Which shelf misfiled it decides where it goes, which is why the
// destination is written into the mark rather than looked up from whoever put it there.
public class ActTwoStageTwoTests
{
    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static int Marked(RunPlayback play, string mark) =>
        Enum.GetValues<CardZone>()
            .SelectMany(zone => Zones(play).GetCardsInZone(zone))
            .Count(card => card.HasMark(new TagId(mark)));

    // The shelf marks a card in the pile it will be drawn from, and the mark is spent the moment that draw
    // happens: the card is taken straight back and a replacement fetched, so the hand is the size it should
    // have been and nothing is left marked. (There is no window in which the player holds it — which is the
    // whole point, and why the mark cannot be observed sitting in a hand.)
    [Fact]
    public void A_misfiled_card_never_reaches_the_hand_and_a_replacement_is_fetched()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("crabwise_shelf", "mis_shelve"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        var handSize = play.CombatDriver!.Current!.Hand.Count;
        play.CombatDriver.EndTurn(); // misfiled
        play.CombatDriver.EndTurn(); // …and drawn again: taken back, replacement fetched

        Assert.Equal(handSize, play.CombatDriver.Current!.Hand.Count);
        Assert.Equal(0, Marked(play, ActTwo.MisfiledSidewaysMark)); // the mark is spent by the taking-back
        play.Dispose();
    }

    // The Crabwise Shelf's misfilings go back to the DRAW pile, not to discard — the shelf puts things
    // sideways, not away. Volume Q-Null's go to discard, which is the plain rule.
    [Fact]
    public void Where_a_misfiled_card_goes_depends_on_the_shelf_that_misfiled_it()
    {
        var (sideways, _, _) = FightProbe.Start(
            FightProbe.Solo("crabwise_shelf", "mis_shelve"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);
        var (plain, _, _) = FightProbe.Start(
            FightProbe.Solo("volume_q_null", "null_index"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        sideways.CombatDriver!.EndTurn();
        sideways.CombatDriver.EndTurn();
        plain.CombatDriver!.EndTurn();
        plain.CombatDriver.EndTurn();

        // Both fights discard their hand at the player's turn end, so the two piles differ by exactly the ONE
        // taken-back card: Q-Null's went to discard, the Shelf's went sideways into the draw pile.
        Assert.True(
            Zones(plain).DiscardPile.Count > Zones(sideways).DiscardPile.Count,
            "the plain misfiling goes to discard, the sideways one back into the draw pile");
        sideways.Dispose();
        plain.Dispose();
    }

    // "If the player plays it this turn, it resolves normally and THEN becomes Misfiled." The card works; the
    // consequence is that you will not see it again next time it comes round.
    [Fact]
    public void A_wrong_edition_card_works_and_is_filed_away_afterwards()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("corridor_in_the_wrong_edition", "dead_end_turn"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Assert.Equal(1, Marked(play, ActTwo.WrongEditionMark)); // one card in hand is the wrong edition

        var before = play.CombatDriver!.Current!.State.GetCombatant(enemyId).Health.Current;
        var card = play.CombatDriver.Current!.Hand.First(c => c.HasMark(new TagId(ActTwo.WrongEditionMark)));
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);

        Assert.True(play.CombatDriver.Current!.State.GetCombatant(enemyId).Health.Current < before,
            "the card resolved normally");
        Assert.Equal(0, Marked(play, ActTwo.WrongEditionMark));
        Assert.Equal(1, Marked(play, ActTwo.MisfiledMark)); // and is filed away for next time
        play.Dispose();
    }
}
