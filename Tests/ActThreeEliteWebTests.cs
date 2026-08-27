using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 2 — Grandmother Web. Three visible customs, of which only the Taut ones are law. A licence
// spent here buys the removal of a RULE rather than a turn of safety, and the web mends what it loses.
public class ActThreeEliteWebTests
{
    private const string Deed = "paper_cut";           // 1 Energy
    private const string Working = "cower_behind_a_desk"; // 1 Energy
    private const string TwoCost = "permit_a38";       // 2 Energy

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Web(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int TrespassFrom(RunPlayback play, CombatantId filer) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)
                && s.SourceCombatantId == filer)
            .Sum(s => s.Stacks);

    private const string EntryCut = "web_thread_entry_cut";
    private const string MeasureCut = "web_thread_measure_cut";
    private const string EntryTaut = "web_thread_entry_taut";
    private const string MeasureTaut = "web_thread_measure_taut";
    private const string DepartureTaut = "web_thread_departure_taut";
    private const string EntryKnot = "web_thread_entry_knot";
    private const string MeasureKnot = "web_thread_measure_knot";

    // ── which courtesies are law ──────────────────────────────────────────────────────────────────────────

    // Two Threads begin Taut and one Slack, and only a Taut Thread is a rule at all: opening a turn with a
    // Deed is the Thread of Entry, and it costs the licence the fight opened with.
    [Fact]
    public void A_taut_thread_is_law()
    {
        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9),
            deck: [Deed, Working, Working, Working, Working], health: 400);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(1, FightProbe.StacksOf(Web(play), EntryTaut));
        Assert.Equal(0, FightProbe.StacksOf(Web(play), DepartureTaut));

        Play(play, session, Deed, web);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    // The Thread of Departure begins Slack, so ending a turn with an empty hand is no violation at all.
    [Fact]
    public void A_slack_thread_is_no_rule()
    {
        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9),
            deck: [Working, TwoCost, Working, TwoCost, Working], health: 400);

        // Alternating Base Costs and never a Deed first: neither Taut Thread has anything to say, and the
        // hand is empty at the bell — which is only the Departure's business, and the Departure is Slack.
        Play(play, session, Working, web);
        Play(play, session, TwoCost, web);
        Play(play, session, Working, web);
        Play(play, session, TwoCost, web);
        Play(play, session, Working, web);
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, TrespassFrom(play, web));
        play.Dispose();
    }

    // ── Cut the Thread ────────────────────────────────────────────────────────────────────────────────────

    // A licence spent against a Thread cuts THAT Thread — the act writes down which law is being filed, and
    // a refusal happens inside the filing.
    [Fact]
    public void A_refused_thread_is_the_one_that_gets_cut()
    {
        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9),
            deck: [Deed, Working, Working, Working, Working], health: 400);

        Play(play, session, Deed, web);

        Assert.Equal(2, FightProbe.StacksOf(Web(play), EntryCut));
        Assert.Equal(0, FightProbe.StacksOf(Web(play), MeasureCut)); // the Measure was never in question
        play.Dispose();
    }

    // "…for the rest of the current turn and the next full player turn." Two of the player's turn starts
    // pass, and the second is the mending — which the Web is paid 7 Block for.
    [Fact]
    public void A_cut_thread_mends_on_the_second_turn_and_pays_the_web()
    {
        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9),
            deck: [Deed, Working, Working, Working, Working], health: 400);

        Play(play, session, Deed, web);
        Assert.Equal(2, FightProbe.StacksOf(Web(play), EntryCut));

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Web(play), EntryCut)); // still cut through the next turn

        play.CombatDriver.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Web(play), EntryCut));
        Assert.True(Block(Web(play)) >= 7, $"expected the mend's Block, saw {Block(Web(play))}");
        play.Dispose();
    }

    // ── Knot the Claim ────────────────────────────────────────────────────────────────────────────────────

    // Standing lets the Web tie an older promise into a courtesy that is already law — the FIRST eligible
    // one, and only standing it was GRANTED. An unpaid demand is the act's other route to a new Claim, so
    // it is the one that proves the knot without a Thread having to be spent first.
    [Fact]
    public void A_newly_created_claim_knots_the_first_live_thread()
    {
        var (play, _, _) = FightProbe.Start(
            // Standing it merely HOLDS, with no grant announced: the knot answers the announcement alone.
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "hospitality_has_its_price", energy: 9,
                (ActThree.ClaimId, 1)),
            deck: [Working, TwoCost, Working, TwoCost, Working], health: 400);

        Assert.Equal(0, FightProbe.StacksOf(Web(play), EntryKnot));

        play.CombatDriver!.EndTurn(); // hospitality has its price: the Claim is spent for a demand of 2
        Assert.Equal(0, FightProbe.StacksOf(Web(play), ActThree.ClaimId));

        play.CombatDriver.EndTurn();  // left owing, so the demand becomes standing granted outright

        Assert.Equal(1, FightProbe.StacksOf(Web(play), EntryKnot));   // the first Thread that is law
        Assert.Equal(0, FightProbe.StacksOf(Web(play), MeasureKnot)); // and only one
        play.Dispose();
    }

    // A knotted Thread attempts 2 Trespass rather than 1 — one application, so one licence still refuses it,
    // and the refusal unties the knot as well as cutting the Thread.
    [Fact]
    public void A_knotted_thread_attempts_two_and_is_untied_by_a_refusal()
    {
        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9,
                (EntryKnot, 1)),
            deck: [Deed, Working, Working, Working, Working], health: 400);

        Assert.Equal(1, FightProbe.StacksOf(Web(play), EntryKnot));
        Play(play, session, Deed, web);

        var refusals = play.CombatDriver!.Current!.State.CombatLog
            .Where(e => e.Type == StandardCombatLogTypes.StatusApplicationBlocked)
            .Select(e => e.Message).ToList();
        Assert.Contains(refusals, m => m.Contains("prevented 2 stack(s)", StringComparison.Ordinal));

        Assert.Equal(0, FightProbe.StacksOf(Web(play), EntryKnot)); // untied
        Assert.Equal(2, FightProbe.StacksOf(Web(play), EntryCut));  // and cut
        play.Dispose();
    }

    // ── Thread rotation ───────────────────────────────────────────────────────────────────────────────────

    // "After every second Web action, one Taut Thread becomes Slack and the Slack Thread becomes Taut." The
    // slack courtesy walks the three in order, so what is coming is always readable off the board.
    [Fact]
    public void Every_second_web_action_turns_the_threads()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 400);

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Web(play), EntryTaut)); // one action: nothing turns yet

        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Web(play), DepartureTaut)); // the slack one is drawn tight…
        Assert.Equal(0, FightProbe.StacksOf(Web(play), EntryTaut));     // …and the next in order goes slack
        Assert.Equal(1, FightProbe.StacksOf(Web(play), MeasureTaut));
        play.Dispose();
    }

    // ── the Thread of Measure ─────────────────────────────────────────────────────────────────────────────

    // Two cards in a row of the same Base Cost, and once a turn only — the Measure answers a matched pair,
    // not every card after one.
    [Fact]
    public void The_measure_answers_one_matched_pair_a_turn()
    {
        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "needle_leg_courtesy", energy: 9),
            deck: [TwoCost, Working, Working, Working, Working], health: 400);

        Play(play, session, TwoCost, web);  // 2, and not a Deed: the Entry says nothing
        Play(play, session, Working, web);  // 1 — no match
        Play(play, session, Working, web);  // 1, 1 — the pair
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(2, FightProbe.StacksOf(Web(play), MeasureCut));

        Play(play, session, Working, web);  // another pair, but the Measure is cut now
        Assert.Equal(0, TrespassFrom(play, web));
        play.Dispose();
    }

    // ── Close the Web Around the Guest ────────────────────────────────────────────────────────────────────

    // "18 +4 per Knotted Thread, max 26" — two knots' worth, and no more however many are tied.
    [Fact]
    public void The_closing_reads_the_knots_up_to_two()
    {
        var (bare, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "close_the_web_around_the_guest", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 400);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 18, Hero(bare).Health.Current);
        bare.Dispose();

        var (knotted, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "close_the_web_around_the_guest", energy: 9,
                (EntryKnot, 1), (MeasureKnot, 1)),
            deck: [Working, Working, Working, Working, Working], health: 400);
        var start = Hero(knotted).Health.Current;
        knotted.CombatDriver!.EndTurn();
        Assert.Equal(start - 26, Hero(knotted).Health.Current);
        knotted.Dispose();
    }

    // ── Mend the Household Law ────────────────────────────────────────────────────────────────────────────

    // With nothing cut it is 20 Block; with something cut it is 14 plus the mending's own 7, and the rule
    // comes straight back.
    [Fact]
    public void Mending_restores_a_cut_thread_and_blocks_less_for_it()
    {
        var (idle, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "mend_the_household_law", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 400);
        idle.CombatDriver!.EndTurn();
        Assert.Equal(20, Block(Web(idle)));
        idle.Dispose();

        var (play, session, web) = FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherWebEnemyId, "mend_the_household_law", energy: 9),
            deck: [Deed, Working, Working, Working, Working], health: 400);

        Play(play, session, Deed, web); // cut the Entry
        Assert.Equal(2, FightProbe.StacksOf(Web(play), EntryCut));

        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Web(play), EntryCut)); // mended at once
        Assert.Equal(21, Block(Web(play)));                 // 14, and the mending's own 7
        play.Dispose();
    }
}
