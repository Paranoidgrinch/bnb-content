using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Mire of Appeals. Neither of these two creates standing. They move it, freeze it and
// occasionally extinguish it — and this is the first stage where the player can find themselves wanting a
// Claim to exist.
public class ActThreeStageSevenTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    private static void MakeAmends(RunPlayback play, InteractiveRunSession session, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        for (var guard = 0; guard < 4; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]); // pay in coin
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.True(session.Error is null, session.Error);
    }

    // ── Ditch Lamprey of Appeals ──────────────────────────────────────────────────────────────────────────

    // A grievance can temporarily belong to the appeal itself: the Lamprey takes a Claim granted to a party
    // holding more than it does, and remembers whose it was.
    [Fact]
    public void The_appeal_attaches_itself_to_somebody_elses_grievance()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_appeals_duo_03"), health: 300);

        var lamprey = Enemies(play)[0];
        var stone = Enemies(play)[1];

        // Prior Dispute grants the Stone a Claim before the first action; the Lamprey holds none, so the
        // appeal attaches to it at once.
        Assert.Equal(0, FightProbe.StacksOf(stone, ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(lamprey, ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(stone, ActThree.AppealRememberedId));
        play.Dispose();
    }

    // Nothing was created and nothing destroyed — a transfer is not a grant, and nothing that listens for a
    // grant hears any of this.
    [Fact]
    public void An_appeal_creates_no_new_standing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_appeals_duo_03"), health: 300);

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimCreatedId));
        play.Dispose();
    }

    // …and it can go home again.
    [Fact]
    public void The_appeal_hands_the_grievance_back()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("appeals",
                ("ditch_lamprey_of_appeals", "return_the_appeal", null),
                ("permit_hare", "check_the_permit", null)),
            health: 400);

        // Four turns: one refused, three that land, and the Hare's Claim is attached at once.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();

        var lamprey = Enemies(play)[0];
        var hare = Enemies(play)[1];
        Assert.Equal(1, FightProbe.StacksOf(lamprey, ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(hare, ActThree.ClaimId));

        play.CombatDriver!.EndTurn(); // Return the Appeal

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId));
        play.Dispose();
    }

    // ── The Sedge Bench ───────────────────────────────────────────────────────────────────────────────────

    // The Bench takes up whoever holds the most, and while a matter is before it nobody else may move it.
    [Fact]
    public void A_reviewed_claim_cannot_be_taken_by_anybody_else()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("appeals",
                ("the_sedge_bench", "hold_under_review", null),
                ("blackthorn_bride", "thorn_vow", null)),
            health: 500);

        // The Bride files a Trespass a turn: one refused, three that land, and her first Claim arrives.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId));

        play.CombatDriver!.EndTurn(); // the Bench's turn opens and takes the matter up

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.UnderReviewId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId)); // it still counts
        play.Dispose();
    }

    // "Call the Matter": the demand is the REVIEWED party's, not the Bench's — which is the only reason
    // settling it can extinguish the Claim.
    [Fact]
    public void Calling_the_matter_makes_the_reviewed_party_the_creditor()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("appeals",
                ("the_sedge_bench", "call_the_matter", null),
                ("blackthorn_bride", "thorn_vow", null)),
            health: 500);

        for (var turn = 0; turn < 5; turn++)
            play.CombatDriver!.EndTurn();

        var bench = Enemies(play)[0].Id;
        var bride = Enemies(play)[1].Id;
        Assert.True(OwedTo(play, bride) > 0, "the demand is the reviewed party's");
        Assert.Equal(0, OwedTo(play, bench));
        play.Dispose();
    }

    // An appeal does not erase ownership. It suspends the Claim long enough for settlement to extinguish it.
    [Fact]
    public void Settling_a_reviewed_partys_demand_extinguishes_the_claim()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("appeals", energy: 9,
                ("the_sedge_bench", "call_the_matter", null),
                ("blackthorn_bride", "thorn_vow", null)),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 500);

        for (var turn = 0; turn < 5; turn++)
            play.CombatDriver!.EndTurn();

        var bride = Enemies(play)[1];
        Assert.Equal(1, FightProbe.StacksOf(bride, ActThree.UnderReviewId));
        Assert.True(FightProbe.StacksOf(bride, ActThree.ClaimId) > 0);

        while (OwedTo(play, bride.Id) > 0)
            MakeAmends(play, session, bride.Id);

        play.CombatDriver!.EndTurn(); // the demand falls due settled, and the reviewed Claim goes with it

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[1], ActThree.UnderReviewId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId));
        play.Dispose();
    }
}
