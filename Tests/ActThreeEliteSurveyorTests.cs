using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 7 — Surveyor of Forgotten Paths. Three laws stand at once and only the CURRENT one is law;
// breaking the FORMER one is where an Old Right comes from. Every Claim it is granted re-surveys, so the
// standing you hand it turns the law you were breaking on purpose into the law you are punished under.
public class ActThreeEliteSurveyorTests
{
    private const string OneCost = "paper_cut";           // Deed, 1
    private const string OneCostWorking = "cower_behind_a_desk"; // Working, 1
    private const string TwoCost = "permit_a38";          // Working, 2

    private const string CurrentFootfall = "survey_current_footfall";
    private const string FormerFootfall = "survey_former_footfall";
    private const string CurrentMeasure = "survey_current_measure";
    private const string FormerMeasure = "survey_former_measure";
    private const string CurrentMargin = "survey_current_margin";
    private const string FormerMargin = "survey_former_margin";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Surveyor(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // Old Rights and the citations that spend them are the PLAYER's, so a probe that wants one has to open
    // the hero with it.
    private static EncounterDefinition WithHero(EncounterDefinition probe, params (string, int)[] statuses) =>
        new(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             .. statuses.Select(x => new StartingStatusSpec(new StatusDefinitionId(x.Item1), x.Item2))],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);

    private static void Cite(RunPlayback play, InteractiveRunSession session, int option, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.CiteTheOldSurveyCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the map ───────────────────────────────────────────────────────────────────────────────────────────

    // At the first bell the map is drawn: Footfall current, Margin former, Measure unsurveyed.
    [Fact]
    public void The_map_stands_before_the_first_card()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "drive_the_first_stake", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        Assert.Equal(1, FightProbe.StacksOf(Surveyor(play), CurrentFootfall));
        Assert.Equal(1, FightProbe.StacksOf(Surveyor(play), FormerMargin));
        Assert.Equal(0, FightProbe.StacksOf(Surveyor(play), CurrentMeasure));
        Assert.Equal(0, FightProbe.StacksOf(Surveyor(play), FormerMeasure));
        play.Dispose();
    }

    // The Current survey files: a third real card in a turn is a Trespass.
    [Fact]
    public void Breaking_the_current_survey_is_a_trespass()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "declare_the_new_boundary", energy: 9),
            deck: [OneCost, TwoCost, OneCostWorking, TwoCost, OneCost], health: 500);

        Play(play, session, OneCost, surveyor);
        Play(play, session, TwoCost, surveyor);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, OneCostWorking, surveyor); // the third
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // The Former survey pays instead: ending a turn with nothing left to spend is obsolete law, and walking
    // a forgotten path is worth an Old Right — with the card to cite it.
    [Fact]
    public void Breaking_the_former_survey_earns_an_old_right()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "declare_the_new_boundary", energy: 2),
            deck: [OneCost, TwoCost, OneCostWorking, TwoCost, OneCost], health: 500);

        Play(play, session, OneCost, surveyor);
        Play(play, session, OneCostWorking, surveyor); // two cards, and the purse is empty
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        Assert.Contains(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == ActThree.CiteTheOldSurveyCardId);
        play.Dispose();
    }

    // One a turn, and at most two: the forgotten paths are a resource, not an engine.
    [Fact]
    public void Old_rights_stop_at_two()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "declare_the_new_boundary", energy: 0),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn(); // every turn ends on an empty purse

        Assert.Equal(ActThree.MaxOldRights, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        play.Dispose();
    }

    // ── Re-Survey ─────────────────────────────────────────────────────────────────────────────────────────

    // "Current → Former → Unsurveyed → Current." The stake drives the whole map round one step.
    [Fact]
    public void Driving_a_stake_re_surveys_the_map()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "drive_the_first_stake", energy: 9),
            deck: [.. Enumerable.Repeat(OneCostWorking, 5)], health: 500);

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Surveyor(play), FormerFootfall)); // was current
        Assert.Equal(1, FightProbe.StacksOf(Surveyor(play), CurrentMeasure)); // was unsurveyed
        Assert.Equal(0, FightProbe.StacksOf(Surveyor(play), CurrentMargin));  // was former
        Assert.Equal(0, FightProbe.StacksOf(Surveyor(play), FormerMargin));
        Assert.Equal(16, Block(Surveyor(play)));
        play.Dispose();
    }

    // ── Cite the Old Survey ───────────────────────────────────────────────────────────────────────────────

    // OLD BOUNDARY swaps the two surveys for the rest of the turn — so the law you were being punished
    // under becomes the one that pays you.
    [Fact]
    public void Old_boundary_swaps_which_law_is_in_force()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "declare_the_new_boundary", energy: 0),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        play.CombatDriver!.EndTurn(); // an empty purse breaks the Former survey: 1 Old Right

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        Cite(play, session, option: 0, at: surveyor); // OLD BOUNDARY

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.OldBoundaryId));

        // Ending on nothing is now the CURRENT law: it files instead of paying, so no right is earned.
        play.CombatDriver.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        play.Dispose();
    }

    // OLD MEASURE takes 8 Block off the map.
    [Fact]
    public void Old_measure_takes_eight_block_off_the_surveyor()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "drive_the_first_stake", energy: 0),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        play.CombatDriver!.EndTurn(); // 16 Block, and an Old Right for the empty purse
        Assert.Equal(16, Block(Surveyor(play)));

        Cite(play, session, option: 2, at: surveyor); // OLD MEASURE

        Assert.Equal(8, Block(Surveyor(play)));
        play.Dispose();
    }

    // OLD RIGHT OF PASSAGE stops the Surveyor cashing a Claim — and the Claim remains.
    [Fact]
    public void Old_right_of_passage_stops_the_cashing_and_leaves_the_claim()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "stake_through_the_map", energy: 9,
                (ActThree.ClaimId, 1)),
            deck: [.. Enumerable.Repeat(OneCostWorking, 5)], health: 500);

        play.CombatDriver!.EndTurn(); // 20 damage, the Claim cashed, 12 Block
        Assert.Equal(0, FightProbe.StacksOf(Surveyor(play), ActThree.ClaimId));
        Assert.Equal(12, Block(Surveyor(play)));
        play.Dispose();

        // A citation is spent on the turn AFTER it is made, so the right has to be earned first: an empty
        // purse breaks the Former survey, and the passage is cited before the stake comes down again.
        var (held, session, second) = FightProbe.Start(
            // Standing it merely HOLDS: an announced grant would re-survey the map, and the Margin law the
            // empty purse breaks would stop being the Former one.
            FightProbe.Solo(ActThree.SurveyorEnemyId, "stake_through_the_map", energy: 0,
                (ActThree.ClaimId, 2)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        held.CombatDriver!.EndTurn();  // a right earned, and the stake cashes one Claim
        Assert.Equal(1, FightProbe.StacksOf(Surveyor(held), ActThree.ClaimId));

        Cite(held, session, option: 1, at: second); // OLD RIGHT OF PASSAGE
        Assert.Equal(1, FightProbe.StacksOf(Hero(held), ActThree.OldRightOfPassageId));

        held.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Surveyor(held), ActThree.ClaimId)); // the Claim remains
        Assert.Equal(0, FightProbe.StacksOf(Hero(held), ActThree.OldRightOfPassageId)); // the citation spent
        held.Dispose();
    }

    // Once a turn: a second citation in the same turn spends nothing and does nothing.
    [Fact]
    public void The_old_survey_may_be_cited_once_a_turn()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "declare_the_new_boundary", energy: 0),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two empty-purse turns: two Old Rights

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        Cite(play, session, option: 2, at: surveyor);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));

        Cite(play, session, option: 2, at: surveyor); // …and the second citation is refused
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        play.Dispose();
    }

    // ── the intents that read the rights ──────────────────────────────────────────────────────────────────

    // "Measure What Was Forgotten — 14 +5 per Old Right, max 24."
    [Fact]
    public void The_measure_reads_what_you_have_kept()
    {
        var (bare, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "measure_what_was_forgotten", energy: 9),
            deck: [.. Enumerable.Repeat(OneCostWorking, 5)], health: 500);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 14, Hero(bare).Health.Current);
        bare.Dispose();

        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "measure_what_was_forgotten", energy: 0),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);
        play.CombatDriver!.EndTurn(); // one Old Right earned, and 14+5
        play.CombatDriver.EndTurn();  // two, and 14+10

        Assert.Equal(ActThree.MaxOldRights, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));
        play.Dispose();
    }

    // "Close the Survey — consume all Old Rights; 16 +6 per consumed, max 28."
    [Fact]
    public void Closing_the_survey_cashes_every_old_right()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.SurveyorEnemyId, "close_the_survey", energy: 0),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        play.CombatDriver!.EndTurn(); // an Old Right earned as the turn ends, then 16+6 and it is gone
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.OldRightId));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(before - 22, Hero(play).Health.Current); // one right earned this turn, cashed at once
        play.Dispose();
    }
}
