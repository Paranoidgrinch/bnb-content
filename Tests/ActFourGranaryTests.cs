using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 3 — The Granary Courts, proved in live fights.
//
// This is the stage where the act's two economic words are pushed into each other: the measure asks for an
// exact expenditure and the tax changes what an expenditure comes to, so meeting one is what stops you
// meeting the other. These tests are mostly about that collision, and about the three bodies that profit
// from it in three different ways.
public class ActFourGranaryTests
{
    private const string OneCost = "paper_cut";   // Deed, 1: 6 damage

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Crocodile of the Short Measure ────────────────────────────────────────────────────────────────

    // The unfair standard: it asks for the whole turn. Unburdened that is exactly meetable — three one-cost
    // Deeds — and the record comes to "exact".
    [Fact]
    public void The_short_measure_asks_for_the_whole_turn_and_can_be_met()
    {
        var (play, session, crocodile) = FightProbe.Start(
            FightProbe.Solo("crocodile_of_the_short_measure", "short_measure"),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 400);

        play.CombatDriver!.EndTurn();
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        Play(play, session, OneCost, crocodile);
        Play(play, session, OneCost, crocodile);
        Play(play, session, OneCost, crocodile);
        play.CombatDriver.EndTurn();

        Assert.Equal(1, Hero(play).GetCounter(ActFour.MeasureResult)); // 1 + 0: exact
        play.Dispose();
    }

    // …and the same three cards under one burden come to four, which is the stage in one line: the tax the
    // Crocodile's other jaw applies is what makes the Crocodile's own demand unmeetable.
    [Fact]
    public void A_burden_is_what_makes_the_short_measure_hard_to_meet()
    {
        var (play, session, crocodile) = FightProbe.Start(
            FightProbe.SoloAgainstHero("crocodile_of_the_short_measure", "short_measure", energy: 5,
                (ActFour.BurdenedId, 3)),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 400);

        play.CombatDriver!.EndTurn(); // spend exactly 3

        // Two Deeds at two Energy each: four, and there is no way back to three.
        Play(play, session, OneCost, crocodile);
        Play(play, session, OneCost, crocodile);
        play.CombatDriver.EndTurn();

        Assert.Equal(2, Hero(play).GetCounter(ActFour.MeasureResult)); // 1 + |4 − 3|
        play.Dispose();
    }

    // What the Crocodile does with a deficit: it bites, and the deficit itself is added to what the player
    // carries. An exact measure is bitten too — the bite is not the punishment, the burden is.
    [Fact]
    public void The_crocodile_snaps_and_adds_the_deficit_to_what_you_carry()
    {
        int BurdenAfterSnap(bool meetTheMeasure)
        {
            var (play, session, crocodile) = FightProbe.Start(
                FightProbe.Authored("labyrinth_granary_01"),
                deck: [.. Enumerable.Repeat(OneCost, 12)], health: 400);

            var body = Enemies(play)[0].Id;
            play.CombatDriver!.EndTurn();          // Short Measure: spend exactly 3
            if (meetTheMeasure)
                for (var i = 0; i < 3; i++)
                    Play(play, session, OneCost, body);
            play.CombatDriver.EndTurn();           // …the measure is taken, then Load the Scale
            var carried = FightProbe.StacksOf(Hero(play), ActFour.BurdenedId);
            play.CombatDriver.EndTurn();           // Snap at the Deficit

            var after = FightProbe.StacksOf(Hero(play), ActFour.BurdenedId) - carried;
            play.Dispose();
            return after;
        }

        Assert.Equal(1, BurdenAfterSnap(meetTheMeasure: false));
        Assert.Equal(0, BurdenAfterSnap(meetTheMeasure: true));
    }

    // ── the Jar-Seal Scarab Swarm ─────────────────────────────────────────────────────────────────────────

    // A seal is attached to whatever got through to flesh — once per swarm, however many of the three hits
    // landed.
    [Fact]
    public void A_swarm_that_reaches_flesh_attaches_one_seal()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("jar_seal_scarab_swarm", "seal_swarm"), health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 12, Hero(play).Health.Current);            // three hits of four
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // …and a swarm the player's Block eats has tagged nothing at all, however many times it hit.
    [Fact]
    public void A_swarm_that_never_reaches_flesh_attaches_nothing()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("jar_seal_scarab_swarm", "seal_swarm", energy: 4),
            deck: [.. Enumerable.Repeat("cower_behind_a_desk", 6)], health: 400);

        Play(play, session, "cower_behind_a_desk", null); //  5 Block
        Play(play, session, "cower_behind_a_desk", null); // 10 Block
        Play(play, session, "cower_behind_a_desk", null); // 15 Block — more than the swarm can spend

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // ── the Hungry Grain Thief ────────────────────────────────────────────────────────────────────────────

    // The Thief lives off the surcharge itself: one ration per card the player paid extra for, collected when
    // it comes round. A card played without a burden feeds it nothing.
    [Fact]
    public void The_thief_takes_one_ration_for_every_surcharge_paid()
    {
        var (play, session, thief) = FightProbe.Start(
            FightProbe.SoloAgainstHero("hungry_grain_thief", "hide_in_the_granary", energy: 6,
                (ActFour.BurdenedId, 2)),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 400);

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.RationId));

        Play(play, session, OneCost, thief); // taxed
        Play(play, session, OneCost, thief); // taxed
        Play(play, session, OneCost, thief); // …and this one is not: the burden is worked off
        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Enemies(play)[0], ActFour.RationId));
        play.Dispose();
    }

    // …and it never takes the same surcharge twice: the bookmark it keeps is what makes a second look at the
    // same tally worth nothing.
    [Fact]
    public void The_thief_never_collects_the_same_surcharge_twice()
    {
        var (play, session, thief) = FightProbe.Start(
            FightProbe.SoloAgainstHero("hungry_grain_thief", "hide_in_the_granary", energy: 6,
                (ActFour.BurdenedId, 1)),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 400);

        Play(play, session, OneCost, thief);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.RationId));

        Play(play, session, OneCost, thief); // untaxed: the burden is gone
        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.RationId));
        play.Dispose();
    }

    // A fat thief eats: the bite is always what the telegraph says, and three rations buy the healing on top.
    [Fact]
    public void Three_rations_buy_the_thief_a_meal()
    {
        var (play, session, thief) = FightProbe.Start(
            FightProbe.SoloAgainstHero("hungry_grain_thief", "feast_on_rations", energy: 9,
                (ActFour.BurdenedId, 3)),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 400);

        var body = Enemies(play)[0];
        for (var i = 0; i < 3; i++)
            Play(play, session, OneCost, body.Id); // three surcharges paid…

        var wounded = Enemies(play)[0].Health.Current;
        play.CombatDriver!.EndTurn(); // …collected at its turn start, and eaten in the same turn

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.RationId));
        Assert.Equal(wounded + 5, Enemies(play)[0].Health.Current);
        play.Dispose();
    }

    // With nothing in the larder the feast is only a bite, and the Thief heals nothing.
    [Fact]
    public void An_empty_larder_buys_nothing()
    {
        var (play, session, thief) = FightProbe.Start(
            FightProbe.Solo("hungry_grain_thief", "feast_on_rations"),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 400);

        var body = Enemies(play)[0];
        Play(play, session, OneCost, body.Id); // untaxed: nothing to collect
        var wounded = Enemies(play)[0].Health.Current;

        play.CombatDriver!.EndTurn();

        Assert.Equal(wounded, Enemies(play)[0].Health.Current);
        play.Dispose();
    }

    // Encounter 11 is the stage's own argument: the burden one body imposes is what the other body eats.
    [Fact]
    public void The_crocodile_feeds_the_thief()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("granary_duo", 6,
                ("crocodile_of_the_short_measure", "load_the_scale", null),
                ("hungry_grain_thief", "hide_in_the_granary", null)),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 400);

        play.CombatDriver!.EndTurn(); // the Crocodile loads the scale: 1 Burdened

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        Play(play, session, OneCost, Body(play, "crocodile").Id); // paid at the higher price
        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Body(play, "hungry_grain_thief"), ActFour.RationId));
        play.Dispose();
    }
}
