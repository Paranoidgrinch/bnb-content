using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III boss 1 — The Ombudsman of Root and Road. The player does not only decide whether standing exists;
// they decide which legal Ground it belongs to. Road Claims cost money, Root Claims cost blood, and a
// licence spent on a Counter-Petition moves one from the one to the other without creating anything.
public class ActThreeBossOmbudsmanTests
{
    private const string OneCost = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Boss(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    private static int TrespassFrom(RunPlayback play, CombatantId filer) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)
                && s.SourceCombatantId == filer)
            .Sum(s => s.Stacks);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Boss) Start(
        string intentId, IReadOnlyList<string> deck, int energy = 9, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ActThree.OmbudsmanEnemyId, intentId, energy, statuses),
            deck: deck, health: 900);

    // ── the two Grounds ───────────────────────────────────────────────────────────────────────────────────

    // The hearings open on the Road, so the fourth real card of the first turn is the breach — and ending
    // that turn with nothing left to spend is not, because the Root is not being heard.
    [Fact]
    public void Only_the_ground_being_heard_is_law()
    {
        var (play, session, boss) = Start("hear_both_parties", [.. Enumerable.Repeat(Working, 5)], energy: 4);

        Play(play, session, Working, boss);
        Play(play, session, Working, boss);
        Play(play, session, Working, boss);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, Working, boss); // the fourth: the Right of the Road
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn(); // the purse is empty, but the Root is not being heard
        Assert.Equal(0, TrespassFrom(play, boss));
        play.Dispose();
    }

    // …and the hearing moves to the other Ground every turn while they are still separate.
    [Fact]
    public void The_hearing_moves_to_the_other_ground_each_turn()
    {
        var (play, _, boss) = Start("hear_both_parties", [.. Enumerable.Repeat(Working, 5)], energy: 0);

        play.CombatDriver!.EndTurn(); // turn 1 is the Road: an empty purse says nothing
        Assert.Equal(0, TrespassFrom(play, boss));

        play.CombatDriver.EndTurn();  // turn 2 is the Root, and the purse was empty
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Grounds of Complaint ──────────────────────────────────────────────────────────────────────────────

    // Standing remembers which right made it: a breach of the Road is a Road Claim.
    [Fact]
    public void Standing_remembers_the_ground_that_made_it()
    {
        var (play, session, boss) = Start("hear_both_parties",
            [.. Enumerable.Repeat(Working, 5)], energy: 9);

        // Three breaches of the Road, one a turn, and the first is refused by the opening licence.
        for (var turn = 0; turn < 4; turn++)
        {
            if (turn > 0)
            {
                play.CombatDriver!.EndTurn(); // the hearing moves to the Root …
                play.CombatDriver.EndTurn();  // … and back to the Road
            }
            for (var card = 0; card < 4; card++)
                Play(play, session, Working, boss);
        }

        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RoadClaimId));
        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.RootClaimId));
        play.Dispose();
    }

    // ── Counter-Petition ──────────────────────────────────────────────────────────────────────────────────

    // A licence re-argues one complaint under the other Ground. Nothing is created and nothing changes
    // hands: the Ombudsman holds exactly what it held.
    [Fact]
    public void A_counter_petition_changes_the_ground_and_nothing_else()
    {
        var (play, session, boss) = Start("hear_both_parties",
            [.. Enumerable.Repeat(Working, 5)], energy: 9,
            (ActThree.ClaimId, 1), (ActThree.RoadClaimId, 1));

        Assert.Contains(play.CombatDriver!.Current!.Hand,
            c => c.DefinitionId.value == ActThree.CounterPetitionCardId);

        var card = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.CounterPetitionCardId);
        play.CombatDriver.PlayCard(card.Id, boss);
        play.CombatDriver.SupplyOptionChoice([0]); // argue it as a matter of the root
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.RoadClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RootClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.ClaimId));         // unchanged
        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.ClaimCreatedId));  // and not announced
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));   // the licence went
        play.Dispose();
    }

    // Once a turn, and only with a licence to spend.
    [Fact]
    public void A_counter_petition_is_made_once_a_turn()
    {
        var (play, session, boss) = Start("hear_both_parties",
            [.. Enumerable.Repeat(Working, 5)], energy: 9,
            (ActThree.ClaimId, 2), (ActThree.RoadClaimId, 2));

        void Petition(int option)
        {
            var card = play.CombatDriver!.Current!.Hand
                .First(c => c.DefinitionId.value == ActThree.CounterPetitionCardId);
            play.CombatDriver.PlayCard(card.Id, boss);
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([option]);
            Assert.True(session.Error is null, session.Error);
        }

        Petition(0);
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RootClaimId));

        Petition(0); // refused: one hearing a turn
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RootClaimId));
        play.Dispose();
    }

    // ── the hearings ──────────────────────────────────────────────────────────────────────────────────────

    // "Hear the Road: consume 1 Road Claim, create Wergild 2, deal 10." Without one it is a flat 16.
    [Fact]
    public void Hearing_the_road_charges_and_hearing_nothing_hits()
    {
        var (bare, _, boss) = Start("hear_the_road", [.. Enumerable.Repeat(Working, 5)], energy: 0);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 16, Hero(bare).Health.Current);
        Assert.Equal(0, OwedTo(bare, boss));
        bare.Dispose();

        var (play, _, ombudsman) = Start("hear_the_road", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.ClaimId, 1), (ActThree.RoadClaimId, 1));
        var start = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(start - 10, Hero(play).Health.Current);
        Assert.Equal(2, OwedTo(play, ombudsman));
        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.RoadClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RoadHeardId));
        play.Dispose();
    }

    // "Hear the Root: consume 1 Root Claim, deal 20, gain 10 Block."
    [Fact]
    public void Hearing_the_root_strikes_and_guards()
    {
        var (play, _, _) = Start("hear_the_root", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.ClaimId, 1), (ActThree.RootClaimId, 1));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 20, Hero(play).Health.Current);
        Assert.Equal(10, Block(Boss(play)));
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RootHeardId));
        play.Dispose();
    }

    // ── the transition ────────────────────────────────────────────────────────────────────────────────────

    // Once both Grounds have been heard, the Ombudsman's NEXT action is the joining of the two
    // jurisdictions — and it is not a blow.
    [Fact]
    public void Hearing_both_grounds_joins_the_jurisdictions()
    {
        var (play, _, _) = Start("hear_the_road", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.ClaimId, 1), (ActThree.RoadClaimId, 1),
            (ActThree.RootHeardId, 1)); // the Root has already been heard

        play.CombatDriver!.EndTurn(); // the Road is heard: both grounds now stand heard
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.RoadHeardId));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn(); // the bell queues the joining, and it replaces the next action

        // No direct attack occurs. What the traveller still takes is the demand the hearing raised, falling
        // due unpaid — which is its own bill and not a blow from the Ombudsman.
        Assert.Equal(before - 4, Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Boss(play), ActThree.CombinedJurisdictionId));
        Assert.Equal(0, Block(Boss(play)));
        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.SafeConductId) >= 1);
        play.Dispose();
    }

    // Under combined jurisdiction both rights are law at once.
    [Fact]
    public void Combined_jurisdiction_makes_both_rights_law()
    {
        var (play, session, boss) = Start("hear_both_parties",
            [.. Enumerable.Repeat(Working, 5)], energy: 4,
            (ActThree.CombinedJurisdictionId, 1));

        for (var card = 0; card < 4; card++)
            Play(play, session, Working, boss); // the Road answers the fourth card …
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn();           // … and the Root answers the empty purse
        Assert.Equal(1, TrespassFrom(play, boss));
        play.Dispose();
    }

    // ── Settlement Has Weight ─────────────────────────────────────────────────────────────────────────────

    // Settling in full strikes one Ombudsman Trespass off the record — and where there is nothing on it,
    // costs the Ombudsman 6 HP instead.
    [Fact]
    public void Settling_in_full_clears_the_record_or_wounds_the_ombudsman()
    {
        var (play, session, boss) = Start("recommend_amends", [.. Enumerable.Repeat(Working, 5)], energy: 9);

        play.CombatDriver!.EndTurn(); // a demand for 1, and a licence handed over
        Assert.Equal(1, OwedTo(play, boss));

        var health = Boss(play).Health.Current;
        var card = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, boss);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(0, OwedTo(play, boss));

        play.CombatDriver.EndTurn();

        Assert.True(Boss(play).Health.Current <= health - 6,
            "with a clean record the settlement costs the Ombudsman 6 HP");
        play.Dispose();
    }

    // ── Hear Every Complaint ──────────────────────────────────────────────────────────────────────────────

    // "18 damage, +5 per Root Claim; a demand for 1 per Road Claim; then every complaint is struck out."
    [Fact]
    public void The_signature_hears_everything_and_strikes_it_out()
    {
        var (play, _, boss) = Start("hear_every_complaint", [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.ClaimId, 3), (ActThree.RootClaimId, 2), (ActThree.RoadClaimId, 1));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 28, Hero(play).Health.Current); // 18 and two roots
        Assert.Equal(1, OwedTo(play, boss));                  // one road
        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.RoadClaimId));
        Assert.Equal(0, FightProbe.StacksOf(Boss(play), ActThree.RootClaimId));
        play.Dispose();
    }

    // Without the standing or the wounds to unlock it, the slot is an ordinary blow.
    [Fact]
    public void The_signature_waits_for_standing_or_wounds()
    {
        var (play, _, _) = Start("hear_every_complaint", [.. Enumerable.Repeat(Working, 5)], energy: 0);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 16, Hero(play).Health.Current);
        play.Dispose();
    }
}
