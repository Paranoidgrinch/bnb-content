using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Quorum Ring. No solo encounters, and that is the stage's argument: a quorum requires more
// than one party. The Mushroom Circle owns a mandate somebody else may act under; the Bracken Moot hears
// every grievance whether or not anybody asked, and turns isolated claims into communal pressure.
public class ActThreeStageSixTests
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

    private const string Deed = "paper_cut";              // a Deed, Base Cost 1
    private const string Working = "cower_behind_a_desk"; // a Working, Base Cost 1

    // ── Mandated Mushroom Circle — Quorum Requires Dissent ────────────────────────────────────────────────

    [Fact]
    public void A_turn_of_one_kind_of_card_fails_the_quorum()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("quorum", energy: 9,
                ("mandated_mushroom_circle", "ring_of_caps", null),
                ("permit_hare", "stamp_passage", null)),
            deck: [Deed, Deed, Working, Deed, Working], health: 300);

        var circle = Enemies(play)[0].Id;
        Play(play, session, Deed, circle);
        Play(play, session, Deed, circle); // two real cards, both Deeds
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn(); // the Circle records the turn as it closes

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    [Fact]
    public void A_turn_with_dissent_in_it_satisfies_the_quorum()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("quorum", energy: 9,
                ("mandated_mushroom_circle", "ring_of_caps", null),
                ("permit_hare", "stamp_passage", null)),
            deck: [Deed, Working, Deed, Working, Deed], health: 300);

        var circle = Enemies(play)[0].Id;
        Play(play, session, Deed, circle);
        Play(play, session, Working, circle); // two kinds: the procedure is satisfied

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // One card is not a quorum question at all — the Circle wants two before it has an opinion.
    [Fact]
    public void One_card_is_not_a_quorum_question()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("quorum", energy: 9,
                ("mandated_mushroom_circle", "ring_of_caps", null),
                ("permit_hare", "stamp_passage", null)),
            deck: [.. Enumerable.Repeat(Deed, 5)], health: 300);

        Play(play, session, Deed, Enemies(play)[0].Id);
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Common Mandate ────────────────────────────────────────────────────────────────────────────────────

    // The Circle spends its own standing so that a neighbour holding none may exercise a right — and the
    // Ford's right is to charge for it. The Claim is SPENT, not handed over: the Ford never holds one.
    [Fact]
    public void The_circle_lends_its_standing_and_the_ford_charges_for_it()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_quorum_duo_03"), health: 400);

        var circle = Enemies(play)[0];
        var ford = Enemies(play)[1].Id;
        Assert.Equal(1, FightProbe.StacksOf(circle, ActThree.ClaimId));

        play.CombatDriver!.EndTurn(); // the Circle's turn opens: it spends the Claim and mandates the Ford

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId)); // spent
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimConsumedId)); // and counted as spent
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId)); // never the Ford's own
        Assert.Contains(Hero(play).Statuses,
            s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId) && s.SourceCombatantId == ford);
        play.Dispose();
    }

    // ── The Bracken Moot — Claims Are Heard Together ──────────────────────────────────────────────────────

    // Two grievances brought by somebody else, and the Moot finds for whoever already holds the most.
    [Fact]
    public void Two_hearings_become_a_finding()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("quorum",
                ("bracken_moot", "adjourn", null),
                ("permit_hare", "check_the_permit", null)),
            health: 400);

        var moot = Enemies(play)[0];
        // The Hare files one a turn: the first is refused, and three more make its Claim — one Hearing.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.HearingId));

        // Three more Trespass make a second Claim, a second Hearing — and the finding.
        for (var turn = 0; turn < 3; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActThree.HearingId)); // spent on the finding
        Assert.True(Enemies(play)[1].Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.ClaimId)).Sum(s => s.Stacks) >= 3,
            "the finding goes to whoever already holds the most");
        play.Dispose();
    }

    // A Claim that merely changes hands is not a grievance anybody brought, and the Moot does not hear it —
    // which is the loop the design's own §3 is written to prevent.
    [Fact]
    public void A_title_that_changes_hands_is_no_hearing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_quorum_duo_02"), health: 300);

        var moot = Enemies(play)[0];
        var stone = Enemies(play)[1];

        // Prior Dispute grants the Stone a Claim before the first action, and Wandering Title passes it on
        // to the neighbour holding fewer — the Moot itself.
        Assert.Equal(0, FightProbe.StacksOf(stone, ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(moot, ActThree.ClaimId));

        // One grant was heard (the Stone's own); the transfer onto the Moot was not, and the Moot never
        // hears its own business in any case.
        Assert.Equal(1, FightProbe.StacksOf(moot, ActThree.HearingId));
        play.Dispose();
    }
}
