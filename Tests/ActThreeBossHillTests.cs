using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III boss 4 — The Answering Hill. The landscape holds no standing: every Claim it is granted goes into
// the ground and waits. At 251 and again at 123 the slope stirs, hands the player one whole turn, and then
// cashes out everything under it — so the whole fight is a question of when it is safe to cross.
public class ActThreeBossHillTests
{
    private const string OneCost = "paper_cut";
    private const string Working = "cower_behind_a_desk";
    private const string TwoCost = "permit_a38";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState TheHill(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Hill) Start(
        string intentId, IReadOnlyList<string> deck, int energy = 9, int? health = null,
        params (string, int)[] statuses)
    {
        var probe = FightProbe.Solo(ActThree.HillEnemyId, intentId, energy, statuses);
        if (health is { } hp)
            probe = new EncounterDefinition(probe.Id,
                [probe.Enemies[0] with { MaxHealth = hp }],
                probe.HeroResources, probe.HeroStartingStatuses, probe.HeroDisplayName,
                probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        return FightProbe.Start(probe, deck: deck, health: 900);
    }

    // ── Keep to the Footpath ──────────────────────────────────────────────────────────────────────────────

    // On the lower slope the fourth real card of a turn is the footing the hill objects to.
    [Fact]
    public void The_lower_slope_answers_a_fourth_card()
    {
        var (play, session, hill) = Start("raise_the_footpath", [.. Enumerable.Repeat(Working, 5)], energy: 4);

        Play(play, session, Working, hill);
        Play(play, session, Working, hill);
        Play(play, session, Working, hill);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, Working, hill);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Buried Claims ─────────────────────────────────────────────────────────────────────────────────────

    // The hill never holds standing. What it is granted goes into the ground, where nothing can transfer it
    // and nothing can spend it.
    [Fact]
    public void Standing_granted_to_the_hill_goes_into_the_ground()
    {
        var (play, _, _) = Start("mark_the_old_boundary", [.. Enumerable.Repeat(Working, 5)], energy: 0);

        play.CombatDriver!.EndTurn(); // a demand for 1, and a licence
        play.CombatDriver.EndTurn();  // left owing: the standing it earns is buried at once

        Assert.Equal(0, FightProbe.StacksOf(TheHill(play), ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId));
        play.Dispose();
    }

    // "The Ground Remembers Weight — 14 +2 per Buried Claim."
    [Fact]
    public void What_is_buried_is_what_the_ground_remembers()
    {
        var (bare, _, _) = Start("the_ground_remembers_weight",
            [.. Enumerable.Repeat(Working, 5)], energy: 0);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 14, Hero(bare).Health.Current);
        bare.Dispose();

        var (play, _, _) = Start("the_ground_remembers_weight",
            [.. Enumerable.Repeat(Working, 5)], energy: 0, health: null,
            (ActThree.BuriedClaimId, 5));
        var start = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(start - 24, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── Settle the Ground ─────────────────────────────────────────────────────────────────────────────────

    // Settling with the hill in full takes one Buried Claim back out of the road — and where there is
    // nothing under it, the hill itself gives way.
    [Fact]
    public void Settling_in_full_digs_one_back_out()
    {
        var (play, session, hill) = Start("mark_the_old_boundary",
            [.. Enumerable.Repeat(Working, 5)], energy: 9, health: null,
            (ActThree.BuriedClaimId, 2));

        play.CombatDriver!.EndTurn(); // a demand for 1
        Assert.Equal(1, OwedTo(play, hill));

        var card = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, hill);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.True(session.Error is null, session.Error);

        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId));
        play.Dispose();
    }

    // ── the first threshold ───────────────────────────────────────────────────────────────────────────────

    // Crossing 251 does not cash anything out. The slope STIRS — a small price, and one whole turn to
    // answer — and only then does it answer.
    [Fact]
    public void The_slope_stirs_before_it_answers()
    {
        var (play, _, hill) = Start("loose_earth", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            health: 240, statuses: [(ActThree.BuriedClaimId, 2)]);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // the bell queues the stirring, and it takes the hill's action

        Assert.Equal(before, Hero(play).Health.Current); // not a blow
        Assert.Equal(1, OwedTo(play, hill));
        Assert.Equal(2, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId)); // nothing cashed yet
        Assert.Equal(1, FightProbe.StacksOf(TheHill(play), ActThree.SlopeAnswersPendingId));

        play.CombatDriver.EndTurn(); // and now the slope answers everything under it

        Assert.Equal(0, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId));
        // Three under the road by then: the two that were there, and the small price of the stirring left
        // unpaid, which is standing the hill buries like any other.
        Assert.Equal(18, Block(TheHill(play)));                 // 6 a claim
        Assert.Equal(1, FightProbe.StacksOf(TheHill(play), ActThree.SurveyedFaceId));
        play.Dispose();
    }

    // The turn between is a real turn: a demand settled in it takes one Buried Claim out before the cash-out.
    [Fact]
    public void The_turn_between_is_a_turn_to_answer_in()
    {
        var (play, session, hill) = Start("loose_earth", [.. Enumerable.Repeat(Working, 5)], energy: 9,
            health: 240, statuses: [(ActThree.BuriedClaimId, 3)]);

        play.CombatDriver!.EndTurn(); // the slope stirs and names a price of 1

        var card = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, hill);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.True(session.Error is null, session.Error);

        play.CombatDriver.EndTurn(); // settled in full: one comes out, and then the rest are answered

        Assert.Equal(0, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId));
        Assert.Equal(12, Block(TheHill(play))); // two were left under the road, not three
        play.Dispose();
    }

    // ── the surveyed face ─────────────────────────────────────────────────────────────────────────────────

    // Above the first threshold the law is the measure, not the footpath.
    [Fact]
    public void The_surveyed_face_measures_instead_of_counting()
    {
        var (play, session, hill) = Start("raise_the_footpath",
            [Working, TwoCost, Working, TwoCost, OneCost], energy: 9, health: null,
            (ActThree.SurveyedFaceId, 1));

        Play(play, session, Working, hill);
        Play(play, session, TwoCost, hill);
        Play(play, session, Working, hill);
        Play(play, session, TwoCost, hill); // four real cards, no matched pair
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, OneCost, hill); // 1 after 2 — still no pair
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── the crown ─────────────────────────────────────────────────────────────────────────────────────────

    // "4 direct HP per Buried Claim, at most 20" — direct loss, which no Block and no reaction sees.
    [Fact]
    public void The_crown_breaks_open_through_the_road()
    {
        var (play, _, _) = Start("loose_earth", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            health: 120,
            statuses: [(ActThree.SurveyedFaceId, 1), (ActThree.BuriedClaimId, 5)]);

        play.CombatDriver!.EndTurn(); // the crown stirs
        Assert.Equal(1, FightProbe.StacksOf(TheHill(play), ActThree.CrownBreaksPendingId));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn(); // and breaks open

        // Twenty through the road — the ground was full — and 2 for the stirring's price left unpaid.
        Assert.Equal(before - 22, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId));
        Assert.Equal(0, Block(TheHill(play)));
        Assert.Equal(1, FightProbe.StacksOf(TheHill(play), ActThree.CrownOfTheHillId));
        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.SafeConductId) >= 1);
        play.Dispose();
    }

    // In the crown, refusing the hill hardens the ground — and the licence still refuses it entirely.
    [Fact]
    public void Refusing_the_crowned_hill_hardens_the_ground()
    {
        var (play, session, hill) = Start("raise_the_footpath",
            [.. Enumerable.Repeat(Working, 5)], energy: 4, health: null,
            (ActThree.CrownOfTheHillId, 1), (ActThree.SurveyedFaceId, 1));

        // The crown alternates, and the first bell of the fight sets it to the measure.
        Play(play, session, Working, hill);
        Play(play, session, Working, hill); // a matched pair

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(6, Block(TheHill(play)));
        play.Dispose();
    }

    // ── the signature ─────────────────────────────────────────────────────────────────────────────────────

    // "24 +3 per Buried Claim +2 per open Wergild point, to a maximum of 34."
    [Fact]
    public void The_hill_answers_entirely_at_the_end()
    {
        var (play, _, _) = Start("the_hill_answers_entirely", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            health: 55,
            statuses:
            [
                (ActThree.SurveyedFaceId, 1), (ActThree.CrownOfTheHillId, 1), (ActThree.BuriedClaimId, 3),
            ]);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 33, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(TheHill(play), ActThree.BuriedClaimId));
        play.Dispose();
    }

    // Until the hill is nearly spent, the slot is an ordinary blow.
    [Fact]
    public void The_signature_waits_for_the_hill_to_be_nearly_spent()
    {
        var (play, _, _) = Start("the_hill_answers_entirely",
            [.. Enumerable.Repeat(Working, 5)], energy: 0);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 15, Hero(play).Health.Current);
        play.Dispose();
    }
}
