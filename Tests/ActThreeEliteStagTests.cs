using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 1 — The Stag of Pre-Approved Violence. Its whole argument is that the licence and the
// offence come from the same animal: it hands you a Safe-Conduct every turn, makes your first Deed need one,
// and counts every one you spend against you.
public class ActThreeEliteStagTests
{
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Stag(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

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

    private static void MakeAmends(RunPlayback play, InteractiveRunSession session, int option, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        for (var guard = 0; guard < 4; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([option]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.True(session.Error is null, session.Error);
    }

    // ── Violence Requires Leave ───────────────────────────────────────────────────────────────────────────

    // The first Deed of the turn needs leave. The fight opens the player with one Safe-Conduct and the Stag
    // hands over a second at the bell, so the first two violations are simply refused.
    [Fact]
    public void The_first_deed_of_a_turn_needs_leave()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9),
            deck: [Deed, Deed, Working, Working, Working], health: 400);

        // The act's own licence, plus the one the Stag grants at the start of the player's turn.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, Deed, stag);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        Assert.Equal(0, TrespassFrom(play, stag));
        play.Dispose();
    }

    // A Working is not violence, and the second Deed of a turn is not a second offence — the road answers
    // one act of violence a turn.
    [Fact]
    public void Only_the_first_deed_of_a_turn_is_an_offence()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9),
            deck: [Working, Deed, Deed, Deed, Deed], health: 400);

        Play(play, session, Working, stag);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // nothing to answer

        Play(play, session, Deed, stag);
        Play(play, session, Deed, stag);
        Play(play, session, Deed, stag);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // exactly one licence spent
        play.Dispose();
    }

    // ── Sanction ──────────────────────────────────────────────────────────────────────────────────────────

    // Every licence spent refusing the Stag is a Sanction, once a turn — and because the Stag refills the
    // licence at every bell, a turn that opens with a Deed always costs one.
    [Fact]
    public void Refusing_the_stag_sanctions_you_once_a_turn()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 400);

        Play(play, session, Deed, stag);
        Play(play, session, Deed, stag);
        Assert.Equal(1, FightProbe.StacksOf(Stag(play), ActThree.StagSanctionId));

        play.CombatDriver!.EndTurn();
        Play(play, session, Deed, stag);
        Assert.Equal(2, FightProbe.StacksOf(Stag(play), ActThree.StagSanctionId));
        play.Dispose();
    }

    // The one lever the player has over the Charge: violence is what the road answers, so a turn with no
    // Deed in it records nothing at all.
    [Fact]
    public void A_turn_without_violence_records_nothing()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 400);

        for (var turn = 0; turn < 3; turn++)
        {
            Play(play, session, Working, stag);
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal(0, FightProbe.StacksOf(Stag(play), ActThree.StagSanctionId));
        play.Dispose();
    }

    // "If below the normal Safe-Conduct cap": the grant stops at three, so a player who never spends one is
    // not handed an endless supply.
    [Fact]
    public void The_grant_stops_at_the_cap()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 400);

        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(ActThree.SafeConductCeiling, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Authorized Charge ─────────────────────────────────────────────────────────────────────────────────

    // Three Sanctions replace whatever the Stag was about to do. With no standing to cash in it is a flat 22
    // — the Charge is only worth more than an ordinary blow because of the Claims the player let it keep.
    [Fact]
    public void Three_sanctions_replace_the_stags_next_action()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9,
                (ActThree.StagSanctionId, 3)),
            deck: [Working, Working, Working, Working, Working], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // Stamp the Hoof would be 15

        Assert.Equal(before - 22, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Stag(play), ActThree.StagSanctionId));
        play.Dispose();
    }

    // "+4 per Claim consumed, up to 2." Two Claims are cashed and gone; a third would be left standing.
    [Fact]
    public void The_charge_cashes_at_most_two_claims()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "stamp_the_hoof", energy: 9,
                (ActThree.StagSanctionId, 3), (ActThree.ClaimId, 3)),
            deck: [Working, Working, Working, Working, Working], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 30, Hero(play).Health.Current);          // the stated maximum
        Assert.Equal(1, FightProbe.StacksOf(Stag(play), ActThree.ClaimId));
        Assert.Equal(2, FightProbe.StacksOf(Stag(play), ActThree.ClaimConsumedId));
        play.Dispose();
    }

    // ── Mark the Verge ────────────────────────────────────────────────────────────────────────────────────

    // A staked verge doubles the next Trespass — and §5.2 holds: one licence still refuses the whole of it.
    [Fact]
    public void One_licence_refuses_a_doubled_trespass_whole()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "mark_the_verge", energy: 9),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 400);

        play.CombatDriver!.EndTurn(); // the Stag stakes the verge

        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Play(play, session, Deed, stag);

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // one licence, not two
        Assert.Equal(0, TrespassFrom(play, stag));
        Assert.Equal(1, FightProbe.StacksOf(Stag(play), ActThree.StagSanctionId));
        play.Dispose();
    }

    // …and what the licence refused really was two: the staked verge doubles the ATTEMPT, so the whole of a
    // two-stack application is turned away by one grant. (A player with no licence left takes both.)
    [Fact]
    public void The_verge_doubles_the_attempt_and_is_spent_by_it()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "mark_the_verge", energy: 9),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 400);

        play.CombatDriver!.EndTurn(); // the Stag stakes the verge
        Assert.Equal(1, FightProbe.StacksOf(Stag(play), ActThree.StagVergeMarkedId));

        Play(play, session, Deed, stag);

        var refusals = play.CombatDriver.Current!.State.CombatLog
            .Where(e => e.Type == StandardCombatLogTypes.StatusApplicationBlocked)
            .Select(e => e.Message)
            .ToList();
        Assert.Contains(refusals, m => m.Contains("prevented 2 stack(s)", StringComparison.Ordinal)
            && m.Contains($"'{ActThree.TrespassId}'", StringComparison.Ordinal));

        // A stake marks ONE crossing: refused or not, the verge is gone afterwards.
        Assert.Equal(0, FightProbe.StacksOf(Stag(play), ActThree.StagVergeMarkedId));
        play.Dispose();
    }

    // ── A Clean Fight ─────────────────────────────────────────────────────────────────────────────────────

    // Settling in full costs the Stag 7 HP and bleeds a Sanction back off — the one route that answers the
    // Charge without ever refusing the Stag a thing.
    [Fact]
    public void Settling_in_full_bleeds_a_sanction_and_seven_health()
    {
        var (play, session, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "antlered_enforcement", energy: 9,
                (ActThree.ClaimId, 1), (ActThree.StagSanctionId, 2)),
            deck: [Working, Working, Working, Working, Working], health: 400);

        play.CombatDriver!.EndTurn(); // 18 damage and a bill for one
        Assert.Equal(1, OwedTo(play, stag));

        var health = Stag(play).Health.Current;
        MakeAmends(play, session, option: 0, at: stag); // pay it in coin
        Assert.Equal(0, OwedTo(play, stag));

        play.CombatDriver.EndTurn();                    // and the demand falls due settled

        Assert.Equal(health - 7, Stag(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Stag(play), ActThree.StagSanctionId));
        play.Dispose();
    }

    // Without standing the Stag does not bill at all: Antlered Enforcement is 18 and nothing else.
    [Fact]
    public void Enforcement_bills_only_where_the_stag_has_standing()
    {
        var (play, _, stag) = FightProbe.Start(
            FightProbe.Solo(ActThree.StagEnemyId, "antlered_enforcement", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 400);

        play.CombatDriver!.EndTurn();

        Assert.Equal(0, OwedTo(play, stag));
        play.Dispose();
    }
}
