using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Meadow of Living Testimony. Neither of these two presses the player; they argue about what
// happened to them. The Foxglove acquires standing from a law it does not own; the Magpie takes standing off
// whoever earned it. Both live inside the moment a Trespass is filed, so both are tested there.
public class ActThreeStageThreeTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static IReadOnlyList<StatusInstance> Trespasses(RunPlayback play) =>
        [.. Hero(play).Statuses.Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private const string OneCost = "paper_cut";
    private const string AlsoOneCost = "cower_behind_a_desk";
    private const string TwoCost = "permit_a38";

    // ── Foxglove Witness — I Saw That Too ─────────────────────────────────────────────────────────────────

    // A violation the licence refuses is not something a witness saw happen, so the meadow remembers
    // nothing and a second breach in the same turn passes unremarked.
    [Fact]
    public void A_refused_violation_is_not_witnessed()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("testimony", energy: 9,
                ("reckoning_hedge", "close_the_hedge", null),
                ("foxglove_witness", "witness_shelter", null)),
            deck: [OneCost, AlsoOneCost, OneCost, AlsoOneCost, OneCost]);

        var hedge = Enemies(play)[0].Id;
        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // the Hedge's law — refused by the licence
        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // the same law again, and nobody saw the first one

        Assert.Empty(Trespasses(play));
        play.Dispose();
    }

    // Break the SAME law twice in one turn and the Foxglove testifies to a violation of a law that is not
    // its own — and that the Hedge itself no longer answers, having already spoken this turn.
    [Fact]
    public void Breaking_the_same_law_twice_in_a_turn_brings_a_second_witness()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("testimony", energy: 9,
                ("reckoning_hedge", "close_the_hedge", null),
                ("foxglove_witness", "witness_shelter", null)),
            deck: [OneCost, AlsoOneCost, OneCost, AlsoOneCost, OneCost]);

        var hedge = Enemies(play)[0].Id;
        var foxglove = Enemies(play)[1].Id;

        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // the Hedge's law — refused by the opening licence
        Assert.Empty(Trespasses(play));

        play.CombatDriver!.EndTurn(); // a new turn: the meadow forgets, and the licence is gone
        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // broken — this one lands, and is heard
        Assert.Single(Trespasses(play));
        Assert.Equal(hedge, Trespasses(play)[0].SourceCombatantId);

        Play(play, session, OneCost, hedge);     // …and broken again, in the same turn
        Play(play, session, AlsoOneCost, hedge);

        Assert.Contains(Trespasses(play), t => t.SourceCombatantId == foxglove);
        // The Hedge has already spoken this turn, so the second breach is the witness's alone.
        Assert.Equal(1, Trespasses(play).Count(t => t.SourceCombatantId == hedge));
        play.Dispose();
    }

    // Once a turn: the Foxglove gives one account, not one per breach. (The licence is burned first, because
    // a refused breach spends the Hedge's own once-a-turn answer without anything landing — so a turn that
    // opens with a licence in hand is a turn the meadow hears nothing at all.)
    [Fact]
    public void The_witness_gives_one_account_a_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("testimony", energy: 9,
                ("reckoning_hedge", "close_the_hedge", null),
                ("foxglove_witness", "witness_shelter", null)),
            deck: [OneCost, AlsoOneCost, OneCost, AlsoOneCost, OneCost]);

        var hedge = Enemies(play)[0].Id;
        var foxglove = Enemies(play)[1].Id;

        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // refused — and the licence is gone
        play.CombatDriver!.EndTurn();

        Play(play, session, OneCost, hedge);
        Play(play, session, AlsoOneCost, hedge); // lands, and is heard
        Play(play, session, OneCost, hedge);     // the witness speaks
        Play(play, session, AlsoOneCost, hedge); // …and has nothing more to say this turn
        Play(play, session, OneCost, hedge);

        Assert.Equal(1, Trespasses(play).Count(t => t.SourceCombatantId == foxglove));
        play.Dispose();
    }

    // ── Contrary Magpie — Contrary Testimony ──────────────────────────────────────────────────────────────

    // The Hare opens holding standing the Magpie does not have, so the Hare's violation is contested and ends
    // up owed to the bird that had nothing to do with it. The first round is spent burning the opening
    // licence; the violation that matters is the Hare's own Local Law in the round after, which lands — and
    // lands in the Magpie's name.
    [Fact]
    public void A_violation_is_owed_to_whoever_argues_they_saw_it()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_testimony_duo_02", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)]);

        var hare = Enemies(play)[0];
        var magpie = Enemies(play)[1].Id;
        Assert.Equal(1, FightProbe.StacksOf(hare, ActThree.ClaimId));

        play.CombatDriver!.EndTurn(); // both file; the Hare's is contested and refused, the Magpie's lands
        Assert.Single(Trespasses(play));

        Play(play, session, OneCost, hare.Id);
        Play(play, session, OneCost, hare.Id);
        Play(play, session, OneCost, hare.Id); // the Hare's own Local Law — and the Magpie speaks over it

        Assert.DoesNotContain(Trespasses(play), t => t.SourceCombatantId == hare.Id);
        Assert.Equal(2, Trespasses(play).Count(t => t.SourceCombatantId == magpie));
        play.Dispose();
    }

    // Once a window. Having spoken over the Hare's Local Law, the Magpie has nothing left to say about the
    // Hare's intent later in the same round.
    [Fact]
    public void The_magpie_contradicts_once_a_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_testimony_duo_02", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)]);

        var hare = Enemies(play)[0].Id;
        var magpie = Enemies(play)[1].Id;

        Play(play, session, OneCost, hare);
        Play(play, session, OneCost, hare);
        Play(play, session, OneCost, hare); // the Hare's Local Law: contested, and the licence pays
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.ContestedThisTurnId));
        Assert.Empty(Trespasses(play));

        play.CombatDriver!.EndTurn(); // the same window: the Hare's intent files, and it keeps this one

        Assert.Contains(Trespasses(play), t => t.SourceCombatantId == hare);
        play.Dispose();
    }

    // It contests parties with MORE standing than itself, which is the whole shape of the identity: it is
    // arguing its way up, not defending what it has.
    [Fact]
    public void The_magpie_does_not_contradict_a_party_with_no_more_standing_than_itself()
    {
        // Here the Hedge holds nothing, so there is nothing for the Magpie to take that it does not have.
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("testimony", energy: 9,
                ("reckoning_hedge", "measure_back", null),
                ("contrary_magpie", "bright_evidence", null)));

        play.CombatDriver!.EndTurn(); // the Hedge files; the licence pays
        play.CombatDriver.EndTurn();  // and this one lands

        var hedge = Enemies(play)[0].Id;
        Assert.Contains(Trespasses(play), t => t.SourceCombatantId == hedge);
        play.Dispose();
    }

    // ── Two Witnesses, One Account ────────────────────────────────────────────────────────────────────────

    // The stage's capstone. The Hedge opens holding a Claim, so its survey is already reversed; the Magpie
    // rewrites who the resulting violation is owed to; and the Foxglove still remembers the LAW that was
    // broken rather than the name the argument left on it — which is the only reason its testimony can
    // arrive at all.
    [Fact]
    public void The_witness_remembers_the_law_and_not_the_name_the_argument_left_on_it()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_testimony_trio_01", energy: 9),
            deck: [OneCost, TwoCost, OneCost, TwoCost, OneCost],
            // Three bodies swinging: the probe is a mechanism test, not a balance sample.
            health: 400);

        var hedge = Enemies(play)[0].Id;
        var foxglove = Enemies(play)[1].Id;
        var magpie = Enemies(play)[2].Id;
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId)); // the survey opens reversed

        // Different costs in a row is the reversed survey's violation. It is refused, and the licence goes.
        Play(play, session, OneCost, hedge);
        Play(play, session, TwoCost, hedge);
        Assert.Empty(Trespasses(play));

        play.CombatDriver!.EndTurn();

        Play(play, session, OneCost, hedge);
        Play(play, session, TwoCost, hedge); // the survey again — it lands, and the Magpie owns the account
        Play(play, session, OneCost, hedge);
        Play(play, session, TwoCost, hedge); // the same law a second time this turn

        Assert.Contains(Trespasses(play), t => t.SourceCombatantId == foxglove);
        // The witness heard a law, not a party: what it testified to was owed to the Magpie all along.
        Assert.Contains(Trespasses(play), t => t.SourceCombatantId == magpie);
        play.Dispose();
    }
}
