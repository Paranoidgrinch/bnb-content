using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stages 13 and 14 — The Necropolis Warrens and The Chamber of Fixed Days, proved in live fights.
//
// Stage 13 is the one place in the act where Act III's law comes back, and §3.9 is emphatic about the terms:
// the player is never expected to arrive with it. So the first tests are about the room handing out the
// licence, spending it, and — when there is none left to spend — building three violations into the Finder's
// Claim. Stage 14 is two clocks and a memory: a table that measures 1, 2, 3 in that order, a moon that
// returns to the rite it last managed to lay (one stack, §3.7), and a procession with a fourth turn in it.
public class ActFourWarrensTests
{
    private const string OneCost = "paper_cut";  // Deed, 1

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Moon-Cycle Ibis ───────────────────────────────────────────────────────────────────────────────

    // The memory is a face on the body: whatever affliction the ibis last managed to lay is the Last Rite,
    // its cycle point repeats exactly ONE stack of it (§3.7), and the next rite it sets is the other one.
    [Fact]
    public void The_moon_repeats_one_stack_of_the_last_rite_and_then_sets_the_other()
    {
        var (play, _, ibis) = FightProbe.Start(
            FightProbe.SoloCycle("moon_cycle_ibis", "set_the_rite", "moon_peck", "wing_shelter"),
            health: 900);

        play.CombatDriver!.EndTurn();  // Set the Rite: with nothing remembered, the rite is weight
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, ibis), ActFour.LastRiteBurdenedId));

        play.CombatDriver.EndTurn();  // Moon Peck
        play.CombatDriver.EndTurn();  // Wing Shelter: the return, ONE stack

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        play.CombatDriver.EndTurn();  // Set the Rite again: the moon returns to what it has NOT lately done
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, ibis), ActFour.LastRiteEntombedId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, ibis), ActFour.LastRiteBurdenedId));

        play.CombatDriver.EndTurn();  // Moon Peck
        play.CombatDriver.EndTurn();  // Wing Shelter: now the burial is what returns

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // A fight in which the ibis has never landed a rite has nothing to return to: the cycle point shelters
    // and does nothing else. "Successfully applies" is the master's wording, and an empty memory is empty.
    [Fact]
    public void A_moon_with_no_rite_behind_it_repeats_nothing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("moon_cycle_ibis", "wing_shelter"), health: 900);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Eclipse Scarab ────────────────────────────────────────────────────────────────────────────────

    // The procession counts down where the player can see it, and the fourth turn is the one with no noon in
    // it. Then the count starts again — a calendar does not stop having fourth days.
    [Fact]
    public void Black_noon_arrives_on_the_fourth_turn_and_the_procession_begins_again()
    {
        var (play, _, scarab) = FightProbe.Start(
            FightProbe.Solo("eclipse_scarab", "solar_scar"), health: 900);

        foreach (var expected in new[] { 1, 2, 3 })
        {
            play.CombatDriver!.EndTurn();
            Assert.Equal(expected, FightProbe.StacksOf(Enemy(play, scarab), ActFour.ApproachOfNoonId));
        }

        play.CombatDriver!.EndTurn();  // the absence of noon
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, scarab), ActFour.ApproachOfNoonId));

        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, scarab), ActFour.ApproachOfNoonId));
        play.Dispose();
    }
}
