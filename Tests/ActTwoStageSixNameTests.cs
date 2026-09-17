using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — Expunged Name. Say a card's name twice in one fight and the register strikes it through.
//
// ⚠ This one waited on the ENGINE: card-play records were per TURN, so "already played earlier in the COMBAT"
// had nothing to read. `cardPlaysThisCombat` is that record, and the play being made is already counted in it.
public class ActTwoStageSixNameTests
{
    private static int Redacted(RunPlayback play)
    {
        var zones = play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        return Enum.GetValues<CardZone>().SelectMany(zones.GetCardsInZone)
            .Count(card => card.HasMark(new TagId(ActTwo.RedactedMark)));
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Register() =>
        FightProbe.Start(
            FightProbe.Solo("expunged_name", "seal_the_register", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

    // The FIRST outing of a name is not a repeat: nothing is struck through.
    [Fact]
    public void The_first_time_a_name_is_said_it_stands()
    {
        var (play, session, enemy) = Register();

        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemy);
        Assert.Null(session.Error);

        Assert.Equal(0, Redacted(play));
        play.Dispose();
    }

    // "…a card whose name has already been played earlier in the combat: that card becomes Redacted."
    [Fact]
    public void Saying_the_same_name_twice_in_one_fight_strikes_it_through()
    {
        var (play, session, enemy) = Register();

        // Every card in this deck is a Paper Cut, so the second play is the same NAME as the first.
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemy);
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemy);
        Assert.Null(session.Error);

        Assert.Equal(1, Redacted(play));
        play.Dispose();
    }

    // "Maximum once per player turn." A third and fourth repeat in the same turn are not struck.
    [Fact]
    public void Only_the_first_repeat_of_a_turn_is_struck()
    {
        var (play, session, enemy) = Register();

        for (var i = 0; i < 4 && play.CombatDriver!.Current!.Hand.Count > 0; i++)
            play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemy);
        Assert.Null(session.Error);

        Assert.Equal(1, Redacted(play));
        play.Dispose();
    }

    // ⚠⚠ AND THE FIGHT REMEMBERS ACROSS THE TURN BOUNDARY, which is the whole point of the new record: a
    // name said once last turn and once this turn is a repeat, even though the per-turn tally was wiped in
    // between. Without that, this test is the one that fails.
    [Fact]
    public void A_name_said_last_turn_still_counts_as_said()
    {
        var (play, session, enemy) = Register();

        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemy);
        Assert.Equal(0, Redacted(play));

        play.CombatDriver.EndTurn();                       // the turn's own tally is wiped here
        Assert.Null(session.Error);

        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemy);
        Assert.Null(session.Error);

        Assert.Equal(1, Redacted(play));
        play.Dispose();
    }
}
