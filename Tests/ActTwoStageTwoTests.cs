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

    // ★ Volume Q-Null's signature — Null Reference. "When a card Misfiled by Q-Null is skipped, inspect the
    // immediate replacement; if it has the same persistent Base Cost, it also becomes Misfiled."
    //
    // Every card in this deck costs the same, so the replacement always matches and the propagation always
    // fires. What is being read is the MARK LEFT IN HAND: a fresh misfiling sits in the draw pile wearing
    // Q-Null's own mark, and only a propagated one is a plain `misfiled` on a card that has just arrived.
    [Fact]
    public void Q_nulls_misfiling_spreads_to_a_replacement_that_costs_the_same()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("volume_q_null", "null_index"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn(); // Q-Null misfiles the top of the pile
        play.CombatDriver.EndTurn();  // …drawn, taken back, replacement fetched — and it costs the same
        Assert.Null(session.Error);

        Assert.True(
            play.CombatDriver.Current!.Hand.Any(c => c.HasMark(new TagId(ActTwo.MisfiledMark))),
            "the replacement should have caught the misfiling");
        play.Dispose();
    }

    // …and the plain shelf does NOT spread. The Crabwise Shelf misfiles on the same schedule and its
    // replacement costs exactly the same, so if the propagation were in the take-back rather than in the
    // MARK this would catch it — which is the whole reason Q-Null has a mark of its own.
    [Fact]
    public void An_ordinary_shelf_does_not_spread_its_misfiling()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("crabwise_shelf", "mis_shelve"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        Assert.DoesNotContain(
            play.CombatDriver.Current!.Hand, c => c.HasMark(new TagId(ActTwo.MisfiledMark)));
        play.Dispose();
    }

    // ⚠ AND THE COST IS ACTUALLY COMPARED. The two tests above would both pass if the condition were the
    // constant `true`, so this one hands Q-Null a deck of two price tiers: over several turns some
    // replacements match their skipped card and some do not, and the propagation has to be RARER than the
    // misfiling that caused it. Deterministic — the probe fights on a fixed seed.
    [Fact]
    public void A_replacement_that_costs_something_else_is_left_alone()
    {
        // ⚠ THE HERO HAS TO SURVIVE THE MEASUREMENT. Eight turns of Null Index is more than the probe's own
        // body holds, and a fight that has ended has no hand to read — which is how this test first failed,
        // with a NullReferenceException rather than a verdict about costs.
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("volume_q_null", "null_index"),
            deck: [.. Enumerable.Repeat("permit_a38", 8), .. Enumerable.Repeat("red_tape", 8)],
            health: 999);

        var misfilings = 0;
        var propagations = 0;
        for (var turn = 0; turn < 8 && play.CombatDriver!.Current is { } round; turn++)
        {
            play.CombatDriver.EndTurn();
            Assert.Null(session.Error);
            misfilings++;
            propagations += play.CombatDriver.Current?.Hand
                .Count(c => c.HasMark(new TagId(ActTwo.MisfiledMark))) ?? 0;
        }

        Assert.True(propagations < misfilings,
            $"the cost comparison should refuse some replacements: {propagations} of {misfilings} spread");
        play.Dispose();
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
