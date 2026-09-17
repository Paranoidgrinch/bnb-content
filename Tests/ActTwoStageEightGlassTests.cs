using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — the Archive of Misplaced Hours. The Hourglass With Two Bottoms runs two clocks at once, and what
// the player plays pushes one of them back: a Deed the left bottom, anything else the right.
//
// ⚠ The counters count UP to a fuse of three rather than down, so a delay is a DECREMENT — see
// ActTwo.TwoFuturesAtOnce for why (a down-counter would have to be initialised, and a counter defaults to 0).
public class ActTwoStageEightGlassTests
{
    private static int Sand(RunPlayback play, CombatantId glass, CounterId counter) =>
        play.CombatDriver!.Current!.State.GetCombatant(glass).GetCounter(counter);

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Glass) Glass(string deckCard) =>
        FightProbe.Start(
            FightProbe.Solo("hourglass_with_two_bottoms", "flip_the_glass", energy: 9),
            deck: [.. Enumerable.Repeat(deckCard, 12)],
            health: 999);

    // "A turn of the Hourglass's own passes for both bottoms."
    [Fact]
    public void Both_bottoms_run_at_once()
    {
        var (play, session, glass) = Glass("paper_cut");

        Assert.Equal(0, Sand(play, glass, ActTwo.LeftElapsedCounter));
        Assert.Equal(0, Sand(play, glass, ActTwo.RightElapsedCounter));

        play.CombatDriver!.EndTurn();                    // one turn of the glass
        Assert.Null(session.Error);

        Assert.Equal(1, Sand(play, glass, ActTwo.LeftElapsedCounter));
        Assert.Equal(1, Sand(play, glass, ActTwo.RightElapsedCounter));
        play.Dispose();
    }

    // "The first Attack each player turn increases Left Bottom's countdown by 1" — a Deed pushes the LEFT
    // bottom back and leaves the right one alone, which is the whole choice the Hourglass offers.
    [Fact]
    public void A_deed_pushes_the_left_bottom_back_and_only_the_left()
    {
        var (play, session, glass) = Glass("paper_cut");     // paper_cut is a Deed

        play.CombatDriver!.EndTurn();                       // sand: 1 / 1
        var card = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, glass);
        Assert.Null(session.Error);

        Assert.Equal(0, Sand(play, glass, ActTwo.LeftElapsedCounter));   // pushed back
        Assert.Equal(1, Sand(play, glass, ActTwo.RightElapsedCounter));  // untouched
        play.Dispose();
    }

    // ⚠ AND THE OTHER BRANCH. Without this the `else` is unproven: a rule that pushed the LEFT bottom for
    // every card would pass every test above. `permit_a38` is a Form — B&B has no Skill type, so "anything
    // that is not a Deed" is what the design's Skill means here.
    [Fact]
    public void Anything_that_is_not_a_deed_pushes_the_right_bottom()
    {
        var (play, session, glass) = Glass("permit_a38");

        play.CombatDriver!.EndTurn();                       // sand: 1 / 1
        var card = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "permit_a38");
        play.CombatDriver.PlayCard(card.Id, glass);
        Assert.Null(session.Error);

        Assert.Equal(1, Sand(play, glass, ActTwo.LeftElapsedCounter));   // untouched
        Assert.Equal(0, Sand(play, glass, ActTwo.RightElapsedCounter));  // pushed back
        play.Dispose();
    }

    // "…and each side may be delayed at most once per player turn." A second Deed in the same turn does not
    // push again.
    [Fact]
    public void One_delay_per_side_per_turn()
    {
        var (play, session, glass) = Glass("paper_cut");

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();                        // sand: 2 / 2

        foreach (var card in play.CombatDriver.Current!.Hand.Take(3).ToList())
            play.CombatDriver.PlayCard(card.Id, glass);
        Assert.Null(session.Error);

        Assert.Equal(1, Sand(play, glass, ActTwo.LeftElapsedCounter));   // three deeds, ONE push
        play.Dispose();
    }

    // "Maximum 3" is the fuse itself: with no sand run, there is nothing left to push back.
    [Fact]
    public void A_bottom_cannot_be_pushed_past_its_ceiling()
    {
        var (play, session, glass) = Glass("paper_cut");

        var card = play.CombatDriver!.Current!.Hand.First();
        play.CombatDriver.PlayCard(card.Id, glass);          // on turn one, before any sand has run
        Assert.Null(session.Error);

        Assert.Equal(0, Sand(play, glass, ActTwo.LeftElapsedCounter));   // still nothing, not −1
        play.Dispose();
    }

    // The future comes due and empties that bottom: at a full three turns of sand the Hourglass posts its
    // Left Future, and the action turns its own glass over.
    [Fact]
    public void A_full_bottom_empties_and_starts_over()
    {
        var (play, session, glass) = Glass("paper_cut");

        for (var turn = 0; turn < 3; turn++)
            play.CombatDriver!.EndTurn();                   // three turns of the glass, nothing played
        Assert.Null(session.Error);

        // The third tick brought the left bottom due; its action reset the sand as it landed.
        Assert.True(Sand(play, glass, ActTwo.LeftElapsedCounter) < 3,
            "the left bottom should have emptied and started over");
        play.Dispose();
    }
}
