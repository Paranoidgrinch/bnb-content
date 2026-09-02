using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 5 — The Tribute Causeway, proved in live fights.
//
// Stage 4 asked what a missed measure costs. This one asks what a MET one costs, and the answer is the act's
// whole joke: the tribute was correct, processing was not included. So these tests are mostly about
// SUCCESS — that meeting a measure still costs a sheet, that being counted correctly is still being counted,
// and that a turn which ends with nothing left in hand means two opposite things depending on the measure.
public class ActFourCausewayTests
{
    private const string OneCost = "paper_cut";  // Deed, 1

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

    // Spends exactly two Energy on the body in front of the player: the measure every Causeway demand asks
    // for, met to the Energy.
    private static void MeetTheMeasure(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        Play(play, session, OneCost, target);
        Play(play, session, OneCost, target);
    }

    // ── the Foreign Tribute Shade ─────────────────────────────────────────────────────────────────────────

    // Correctness is not a discount: the measure is met to the Energy, and the processing is still a sheet.
    [Fact]
    public void A_measure_that_is_MET_still_costs_a_sheet_of_paperwork()
    {
        var (play, session, shade) = FightProbe.Start(
            FightProbe.Solo("foreign_tribute_shade", "assess_tribute"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 500);

        play.CombatDriver!.EndTurn();  // Assess Tribute: spend exactly 2
        MeetTheMeasure(play, session, shade);
        play.CombatDriver.EndTurn();   // met — and filed

        Assert.Equal(1, Hero(play).GetCounter(ActFour.MeasureResult)); // exact
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }

    // …and a measure MISSED costs the Shade nothing to process: it charges for filing tributes, not for
    // chasing them.
    [Fact]
    public void A_measure_that_is_missed_is_not_processed_at_all()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("foreign_tribute_shade", "assess_tribute"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 500);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();  // nothing spent: missed

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }

    // ── the Donkey of the Third Tally ─────────────────────────────────────────────────────────────────────

    // Every resolution is another entry against the same animal, right or wrong — and the third is the one
    // you carry. A wrong third entry weighs two.
    [Fact]
    public void The_third_entry_is_the_one_you_carry()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("causeway_counted", 3,
                ("foreign_tribute_shade", "assess_tribute", null),
                ("donkey_of_the_third_tally", "brace_the_load", null)),
            health: 600);

        var donkey = Body(play, "donkey_of_the_third_tally").Id;

        play.CombatDriver!.EndTurn();  // the Shade demands
        play.CombatDriver.EndTurn();   // first resolution (missed) — entry 1
        Assert.Equal(1, FightProbe.StacksOf(Body(play, "donkey_of_the_third_tally"), ActFour.TallyId));

        play.CombatDriver.EndTurn();   // entry 2
        Assert.Equal(2, FightProbe.StacksOf(Body(play, "donkey_of_the_third_tally"), ActFour.TallyId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        play.CombatDriver.EndTurn();   // entry 3: settled

        Assert.Equal(0, FightProbe.StacksOf(Body(play, "donkey_of_the_third_tally"), ActFour.TallyId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // A third entry that was CORRECT weighs one instead of two. Being counted right is still being counted.
    [Fact]
    public void A_correct_third_entry_still_weighs_something()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("causeway_counted", 3,
                ("foreign_tribute_shade", "assess_tribute", null),
                ("donkey_of_the_third_tally", "brace_the_load", null)),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 600);

        var shade = Body(play, "foreign_tribute_shade").Id;

        play.CombatDriver!.EndTurn();  // demanded
        play.CombatDriver.EndTurn();   // entry 1, missed
        play.CombatDriver.EndTurn();   // entry 2, missed

        MeetTheMeasure(play, session, shade);
        play.CombatDriver.EndTurn();   // entry 3 — and this one was met

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // ── the Empty-Handed Envoy ────────────────────────────────────────────────────────────────────────────

    // Empty hands and a measure MET: everything was presented, and there is nothing left of the envoy to hide
    // behind — it takes half again as much until its next turn.
    [Fact]
    public void Empty_hands_and_a_measure_met_leave_the_envoy_exposed()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_causeway_duo_01"),
            deck: [OneCost, OneCost], health: 600);

        var shade = Body(play, "foreign_tribute_shade").Id;

        play.CombatDriver!.EndTurn();          // the Shade demands two
        MeetTheMeasure(play, session, shade);  // …met, and the hand is empty doing it
        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Body(play, "empty_handed_envoy"), ActFour.PresentedInFullId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // Empty hands and a measure MISSED means the opposite: nothing was presented at all, and the register
    // says so.
    [Fact]
    public void Empty_hands_and_a_measure_missed_write_you_into_the_register()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_causeway_duo_01"),
            deck: [OneCost, OneCost, OneCost], health: 600);

        var shade = Body(play, "foreign_tribute_shade").Id;

        play.CombatDriver!.EndTurn();  // the Shade demands two
        // …and the player empties their hand spending THREE: nothing left to present, and nothing correct
        // about what was.
        Play(play, session, OneCost, shade);
        Play(play, session, OneCost, shade);
        Play(play, session, OneCost, shade);
        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "empty_handed_envoy"), ActFour.PresentedInFullId));
        play.Dispose();
    }

    // A hand that still holds something is no answer at all: the envoy reads nothing either way.
    [Fact]
    public void A_hand_that_still_holds_something_tells_the_envoy_nothing()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_causeway_duo_01"),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 600);

        var shade = Body(play, "foreign_tribute_shade").Id;

        play.CombatDriver!.EndTurn();
        MeetTheMeasure(play, session, shade);  // met, but cards are left in hand
        play.CombatDriver.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Body(play, "empty_handed_envoy"), ActFour.PresentedInFullId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // The exposure lasts exactly as long as the player's turn: it is there to be used, and gone by the time
    // the envoy acts again.
    [Fact]
    public void The_exposure_lasts_one_turn_and_no_longer()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_causeway_duo_01"),
            deck: [OneCost, OneCost], health: 600);

        var shade = Body(play, "foreign_tribute_shade").Id;

        play.CombatDriver!.EndTurn();
        MeetTheMeasure(play, session, shade);
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Body(play, "empty_handed_envoy"), ActFour.PresentedInFullId));

        play.CombatDriver.EndTurn(); // …the envoy's next turn comes round

        Assert.Equal(0, FightProbe.StacksOf(Body(play, "empty_handed_envoy"), ActFour.PresentedInFullId));
        play.Dispose();
    }
}
