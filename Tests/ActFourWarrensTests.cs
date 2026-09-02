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

    // ── the law is local ──────────────────────────────────────────────────────────────────────────────────

    // §3.9 exactly: a room with the Finder in it opens under the Green Docket's customs and hands the player
    // the one Safe-Conduct those customs are worth having — and a room without it does not, because the
    // return of Act-III law is the Finder's and nobody else's.
    [Fact]
    public void The_finders_room_opens_under_act_three_law_and_no_other_room_does()
    {
        var (warrens, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_warrens_01"), health: 400);
        Assert.Equal(1, FightProbe.StacksOf(Hero(warrens), ActThree.SafeConductId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(warrens), ActThree.GreenDocketCustomsId));
        warrens.Dispose();

        var (days, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_fixed_days_01"), health: 400);
        Assert.Equal(0, FightProbe.StacksOf(Hero(days), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(days), ActThree.GreenDocketCustomsId));
        days.Dispose();
    }

    // A duo hands out ONE licence, not one per body — the opening belongs to the act, and Safe-Conduct is
    // kept as per-grant instances, so asking twice would not even merge into a single one.
    [Fact]
    public void A_duo_still_opens_with_a_single_licence()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_warrens_duo_01"), health: 400);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── the False-Door Finder ─────────────────────────────────────────────────────────────────────────────

    // The passage check is the act's own measure, and the licence the room gave you refuses the first
    // violation outright — which is what the licence is FOR, and the only thing it refuses.
    [Fact]
    public void A_missed_passage_is_refused_by_the_licence_the_room_gave_you()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("false_door_finder", "certify_passage"), health: 800);

        play.CombatDriver!.EndTurn();  // Certify Passage: 13 damage, and spend exactly 2 this turn
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver.EndTurn();  // nothing spent: the check is missed, and the Finder files
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver.EndTurn();  // no licence left: the second violation stands
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // …and three violations owed to one party are that party's Claim. The customs that do it are Act III's,
    // unforked: the Finder files in its own name, so the standing it builds is provably its own.
    [Fact]
    public void Three_missed_passages_are_the_finders_claim()
    {
        var (play, _, finder) = FightProbe.Start(
            FightProbe.Solo("false_door_finder", "certify_passage"), health: 800);

        for (var turn = 0; turn < 5; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, finder), ActThree.ClaimId));
        play.Dispose();
    }

    // Compliance is worth a licence back — and the local cap holds, so meeting every check in a long fight
    // does not stockpile a way out of the stage.
    [Fact]
    public void A_met_passage_earns_a_licence_and_the_cap_holds()
    {
        var (play, session, finder) = FightProbe.Start(
            FightProbe.Solo("false_door_finder", "certify_passage"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 800);

        for (var turn = 0; turn < 3; turn++)
        {
            play.CombatDriver!.EndTurn();  // the check is raised
            Play(play, session, OneCost, finder);
            Play(play, session, OneCost, finder);  // exactly 2 Energy
        }

        play.CombatDriver!.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // ── the Cursed Loot Bearer ────────────────────────────────────────────────────────────────────────────

    // "Whenever Burdened actually increases the Energy cost paid for a card: apply Paperwork." One form per
    // card whose surcharge was PAID — and a turn can only pay as many surcharges as it had Burdened for, so
    // the tax caps its own paperwork.
    [Fact]
    public void Every_object_the_burden_made_dearer_requires_a_form()
    {
        var (play, session, bearer) = FightProbe.Start(
            FightProbe.SoloAgainstHero("cursed_loot_bearer", "loot_swing", energy: 6,
                (ActFour.BurdenedId, 2)),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 800);

        Play(play, session, OneCost, bearer);
        Play(play, session, OneCost, bearer);  // two surcharges paid, and the burden is worked off
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Converter.Cards.Keywords.Paperwork));

        // Cards played with no burden on them pay no surcharge, and file nothing.
        Play(play, session, OneCost, bearer);
        Play(play, session, OneCost, bearer);
        play.CombatDriver.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Converter.Cards.Keywords.Paperwork));
        play.Dispose();
    }

    // ── the Star-Table Scribe ─────────────────────────────────────────────────────────────────────────────

    // The table is fixed: 1, then 2, then 3, then round again. No random order — a player who has read the
    // table knows what the appointed day asks for.
    [Fact]
    public void The_decan_table_measures_one_then_two_then_three()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("star_table_scribe", "fixed_decan_measure"), health: 900);

        foreach (var expected in new[] { 1, 2, 3, 1 })
        {
            play.CombatDriver!.EndTurn();
            Assert.Equal(expected, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        }

        play.Dispose();
    }

    // A day measured wrong is a day written into the register — and one measured right is not. The Scribe
    // does not care HOW wrong: error by band is the Reed-Cord Surveyor's office, not the astronomer's.
    [Fact]
    public void A_day_measured_wrong_goes_into_the_register()
    {
        var (missed, _, _) = FightProbe.Start(
            FightProbe.SoloCycle("star_table_scribe", "fixed_decan_measure", "table_cover"), health: 900);

        missed.CombatDriver!.EndTurn();  // the day is measured: spend exactly 1
        missed.CombatDriver.EndTurn();   // nothing spent — and the Table Cover writes it up
        Assert.Equal(1, FightProbe.StacksOf(Hero(missed), ActFour.InscribedId));
        missed.Dispose();

        var (met, session, scribe) = FightProbe.Start(
            FightProbe.SoloCycle("star_table_scribe", "fixed_decan_measure", "table_cover"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 900);

        met.CombatDriver!.EndTurn();
        Play(met, session, OneCost, scribe);  // exactly 1 Energy against a measure of 1
        met.CombatDriver.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hero(met), ActFour.InscribedId));
        met.Dispose();
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
