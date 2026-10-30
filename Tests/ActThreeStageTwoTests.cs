using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Surveyed Hedgerows. Stage 1 taught that a law has an author; Stage 2 teaches that standing
// CHANGES the law. A Claim reverses what the Hedge measures, decides where the Boundary Stone's title
// wanders, and cannot be prised off the Hawthorn Tenant at all.
public class ActThreeStageTwoTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // Two 1-cost cards in a row, and the hedge has measured you twice the same way.
    // (Paper Cut and Cower Behind a Desk both cost 1; Permit A38 costs 2.)
    private const string OneCost = "paper_cut";
    private const string AlsoOneCost = "cower_behind_a_desk";
    private const string TwoCost = "permit_a38";

    // ── Reckoning Hedge — Current Survey ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Two_cards_of_the_same_base_cost_in_a_row_are_a_trespass()
    {
        var (play, session, hedge) = FightProbe.Start(
            FightProbe.Solo("reckoning_hedge", "close_the_hedge", energy: 9),
            deck: [OneCost, AlsoOneCost, TwoCost, OneCost, AlsoOneCost]);

        Play(play, session, OneCost, hedge);      // nothing to be consecutive with yet
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, AlsoOneCost, hedge);  // 1 then 1 — the same measure twice

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // the licence paid for it
        play.Dispose();
    }

    [Fact]
    public void Changing_the_measure_says_nothing_to_an_unclaimed_hedge()
    {
        var (play, session, hedge) = FightProbe.Start(
            FightProbe.Solo("reckoning_hedge", "close_the_hedge", energy: 9),
            deck: [OneCost, TwoCost, OneCost, AlsoOneCost, TwoCost]);

        Play(play, session, OneCost, hedge);
        Play(play, session, TwoCost, hedge); // 1 then 2

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // The survey speaks once a turn, however many pairs the turn holds.
    [Fact]
    public void The_survey_measures_once_a_turn()
    {
        var (play, session, hedge) = FightProbe.Start(
            FightProbe.Solo("reckoning_hedge", "close_the_hedge", energy: 9),
            deck: [OneCost, AlsoOneCost, OneCost, AlsoOneCost, OneCost]);

        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // refused by the opening licence
        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // three more pairs, and none of them counts

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // A Claim reverses what the hedge is measuring for. Three Trespass make one, and the fourth pair — now of
    // DIFFERENT cost — is the violation.
    [Fact]
    public void A_claim_reverses_the_survey()
    {
        var (play, session, hedge) = FightProbe.Start(
            FightProbe.Solo("reckoning_hedge", "measure_back", energy: 9),
            deck: [OneCost, TwoCost, OneCost, TwoCost, OneCost]);

        // Measure Back files one Trespass a turn: the first is refused, the next three make the Claim.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));

        // Same cost twice — which the reversed survey has no objection to.
        Play(play, session, OneCost, hedge);
        Play(play, session, OneCost, hedge);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));

        // Different costs — which it now does.
        Play(play, session, TwoCost, hedge);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // ── Errant Boundary Stone — Wandering Title ───────────────────────────────────────────────────────────

    // ── The Hawthorn Tenant — Respect the Occupied Plot ───────────────────────────────────────────────────

    // Once a turn: the Tenant objects to being struck, not to each blow. The costs alternate on purpose —
    // the Hedge standing beside it is measuring, and two cards of the same Base Cost would be ITS violation,
    // which is exactly the pressure this pairing is built out of.
    [Fact]
    public void The_tenant_objects_once_a_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_hedgerows_duo_02", energy: 9),
            deck: [OneCost, TwoCost, OneCost, TwoCost, OneCost]);

        var tenant = Enemies(play)[1].Id;
        Play(play, session, OneCost, tenant);  // struck — the Tenant objects, and the licence pays
        Play(play, session, TwoCost, tenant);  // a Working: no blow, nothing to object to
        Play(play, session, OneCost, tenant);  // struck again, in the same turn

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId)); // one violation, and it was refused
        play.Dispose();
    }

    // ── Prior Possession ──────────────────────────────────────────────────────────────────────────────────

}
