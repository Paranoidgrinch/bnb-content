using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, elites — the first three, proved in live fights.
//
// Each of them takes one of the act's five words past what a standard can do with it: the Surveyor makes the
// MEASURE a thing the player chooses the difficulty of, the Scarab Host makes its own armour something you
// decide how to dismantle, and the Rope-Master makes the TAX conscript.
public class ActFourEliteTests
{
    private const string OneCost = "paper_cut";  // Deed, 1: deal 6

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static bool Holds(RunPlayback play, string cardId) =>
        play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == cardId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    private static IReadOnlyList<string> Cuts => [.. Enumerable.Repeat(OneCost, 12)];

    // ── the Surveyor of the Errant Cord ───────────────────────────────────────────────────────────────────

    // The offer IS two cards, and the figures on them are the Surveyor's: a pair out of 1, 2 and 3, never
    // above what the turn can actually spend (§6.2).
    [Fact]
    public void The_survey_is_offered_as_two_achievable_figures()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("surveyor_of_the_errant_cord", "drive_the_first_stake"),
            deck: Cuts, health: 800);

        Assert.True(Holds(play, ActFour.NearBoundaryCardId), "the near boundary was not offered");
        Assert.True(Holds(play, ActFour.FarBoundaryCardId), "the far boundary was not offered");

        var near = Hero(play).GetCounter(ActFour.SurveyNear);
        var far = Hero(play).GetCounter(ActFour.SurveyFar);

        Assert.InRange(near, 1, 3);
        Assert.InRange(far, 1, 3);
        Assert.True(near <= far, $"the near figure {near} is not the nearer one");
        // §6.2: never a figure the turn cannot reach.
        Assert.True(far <= 3, $"the far figure {far} is above what 3 Energy can spend");
        play.Dispose();
    }

    // Take the near boundary and meet it: the surveyor braces.
    [Fact]
    public void Meeting_the_near_boundary_lets_the_surveyor_brace()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo("surveyor_of_the_errant_cord", "drive_the_first_stake"),
            deck: Cuts, health: 800);

        Play(play, session, ActFour.NearBoundaryCardId, null);
        var demanded = FightProbe.StacksOf(Hero(play), ActFour.WeighedId);
        Assert.Equal(Hero(play).GetCounter(ActFour.SurveyNear), demanded);

        for (var i = 0; i < demanded; i++)
            Play(play, session, OneCost, surveyor);
        play.CombatDriver!.EndTurn();

        Assert.True(BlockOf(Body(play, "surveyor_of_the_errant_cord")) >= 10,
            "the surveyor did not brace for a measure it was given exactly");
        play.Dispose();
    }

    // Take the far boundary and meet it and it costs the surveyor blood — an HP loss, not a blow, so no
    // Block of its own stands in the way of it.
    [Fact]
    public void Meeting_the_far_boundary_costs_the_surveyor_blood_and_cover()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.SoloCycle("surveyor_of_the_errant_cord",
                "re_tension_the_measure", "drive_the_first_stake"),
            deck: Cuts, health: 800);

        play.CombatDriver!.EndTurn();  // it braces for 22, and re-tensions the cord
        Assert.Equal(22, BlockOf(Body(play, "surveyor_of_the_errant_cord")));

        Play(play, session, ActFour.FarBoundaryCardId, null);
        var demanded = FightProbe.StacksOf(Hero(play), ActFour.WeighedId);
        Assert.Equal(Hero(play).GetCounter(ActFour.SurveyFar), demanded);

        var before = Body(play, "surveyor_of_the_errant_cord").Health.Current;
        for (var i = 0; i < demanded; i++)
            Play(play, session, OneCost, surveyor);
        var struck = before - Body(play, "surveyor_of_the_errant_cord").Health.Current;

        play.CombatDriver.EndTurn();

        // The blood is the measure's, on top of whatever the cards did — an HP loss and not a blow, so the
        // 22 Block it is standing behind does not stop a point of it. And a re-tensioned cord leaves 14 of
        // slack rather than 10.
        var after = Body(play, "surveyor_of_the_errant_cord");
        Assert.Equal(struck + 10, before - after.Health.Current);
        Assert.Equal(14, FightProbe.StacksOf(after, ActFour.CordSlackId));

        // …which is cover it does not get: the next brace comes to 22 less the slack, and the slack is gone.
        play.CombatDriver.EndTurn();
        var braced = Body(play, "surveyor_of_the_errant_cord");
        Assert.Equal(22 - 14, BlockOf(braced));
        Assert.Equal(0, FightProbe.StacksOf(braced, ActFour.CordSlackId));
        play.Dispose();
    }

    // Two missed surveys move the boundary — and a miss buries you a little deeper each time.
    [Fact]
    public void Two_missed_surveys_move_the_boundary()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("surveyor_of_the_errant_cord", "drive_the_first_stake"),
            deck: Cuts, health: 900);

        Play(play, session, ActFour.NearBoundaryCardId, null);
        play.CombatDriver!.EndTurn();  // nothing spent: one error

        var surveyor = Body(play, "surveyor_of_the_errant_cord");
        Assert.Equal(1, surveyor.GetCounter(ActFour.BoundaryError));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        Play(play, session, ActFour.NearBoundaryCardId, null);
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();  // a second error, and then the boundary moves

        Assert.Equal(0, Body(play, "surveyor_of_the_errant_cord").GetCounter(ActFour.BoundaryError));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.True(before - Hero(play).Health.Current >= 31,
            "the boundary moved for less than the signature is worth");
        play.Dispose();
    }

    // ── the Scarab Host of the Sealed Granary ─────────────────────────────────────────────────────────────

    // Three chambers, three seals, and 6 Block a turn for each one still intact.
    [Fact]
    public void The_colony_is_armoured_by_every_seal_it_still_holds()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("scarab_host_of_the_sealed_granary", "granary_wall"), deck: Cuts, health: 800);

        Assert.Equal(18, BlockOf(Body(play, "scarab_host_of_the_sealed_granary")));
        play.Dispose();
    }

    // Cut through the cover and into the colony and it offers its chambers — once a turn, however many times
    // you cut. Breaking the pest seal costs it 12 HP and puts what was living in there on you.
    [Fact]
    public void Reaching_the_colony_offers_a_chamber_to_break()
    {
        var (play, session, host) = FightProbe.Start(
            FightProbe.Solo("scarab_host_of_the_sealed_granary", "granary_wall", energy: 9),
            deck: Cuts, health: 800);

        // Three cuts to get through 18 Block, and the fourth reaches the colony.
        for (var i = 0; i < 4; i++)
            Play(play, session, OneCost, host);

        Assert.True(Holds(play, ActFour.BreakPestCardId), "no chamber was offered after cutting through");

        var before = Body(play, "scarab_host_of_the_sealed_granary").Health.Current;
        Play(play, session, ActFour.BreakPestCardId, null);

        Assert.Equal(12, before - Body(play, "scarab_host_of_the_sealed_granary").Health.Current);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "scarab_host_of_the_sealed_granary"),
            ActFour.PestSealId));
        play.Dispose();
    }

    // ── the Rope-Master of the Corvée ─────────────────────────────────────────────────────────────────────

    // Every surcharge actually paid is labour owed, and at three the missing hands are called: a Stone-Hauler
    // walks on at 72 HP wearing the rope.
    [Fact]
    public void Three_paid_surcharges_call_a_hand_onto_the_rope()
    {
        var (play, session, master) = FightProbe.Start(
            FightProbe.SoloAgainstHero("rope_master_of_the_corvee", "tie_to_the_gang", energy: 9,
                (ActFour.BurdenedId, 3)),
            deck: Cuts, health: 900);

        for (var i = 0; i < 3; i++)
            Play(play, session, OneCost, master);  // three surcharges paid
        Assert.Equal(3, Hero(play).GetCounter(ActFour.BurdenPaid));

        play.CombatDriver!.EndTurn();  // the Rope-Master counts the labour, then calls

        var hands = Enemies(play).Where(
            c => FightProbe.StacksOf(c, ActFour.AtTheRopeId) > 0).ToList();
        Assert.Single(hands);
        Assert.Equal(ActFour.HaulerHealth, hands[0].Health.Max);
        play.Dispose();
    }

    // …and the roll runs out. Two hands is all a rope holds, and two calls is all a fight gets.
    [Fact]
    public void The_roll_is_spent_after_two_calls_and_labour_stops_being_owed()
    {
        var (play, session, master) = FightProbe.Start(
            FightProbe.SoloAgainstHero("rope_master_of_the_corvee", "tie_to_the_gang", energy: 20,
                (ActFour.BurdenedId, 9)),
            deck: [.. Enumerable.Repeat(OneCost, 30)], health: 2000);

        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < 3; i++)
                Play(play, session, OneCost, master);
            play.CombatDriver!.EndTurn();
        }

        var rope = Body(play, "rope_master_of_the_corvee");
        Assert.Equal(2, rope.GetCounter(ActFour.CallsMade));
        Assert.True(Enemies(play).Count(c => FightProbe.StacksOf(c, ActFour.AtTheRopeId) > 0) <= 2,
            "more than two hands were on the rope");
        play.Dispose();
    }
}
