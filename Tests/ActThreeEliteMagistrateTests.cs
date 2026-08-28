using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 9 — Magistrate of Thorns. You choose the custom you are judged under, and the one that
// matures into standing becomes Binding on top of whatever you choose next. The way back out is
// restitution: settling in full runs the oldest binding down a turn.
public class ActThreeEliteMagistrateTests
{
    private const string OneCost = "paper_cut";            // Deed, 1
    private const string OneCostWorking = "cower_behind_a_desk"; // Working, 1
    private const string TwoCost = "permit_a38";           // Working, 2

    private const string BindingConduct = "binding_judgment_conduct";
    private const string BindingMeasure = "binding_judgment_measure";
    private const string BindingStanding = "binding_judgment_standing";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Thorns(RunPlayback play) =>
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

    // The Magistrate's FIRST judgment is handed down rather than asked (the opening hand is dealt before
    // there is anybody in the dock), and it is always Conduct. From the next turn on, it asks.
    // (1 Conduct, 2 Measure, 3 Standing.)
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Thorns) Start(
        string intentId, IReadOnlyList<string> deck, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ActThree.MagistrateEnemyId, intentId, energy: 9, statuses),
            deck: deck, health: 700);

    private static void Accept(RunPlayback play, int judgment)
    {
        Assert.NotNull(play.CombatDriver!.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([judgment - 1]);
    }

    // End the turn and accept the custom offered for the turn that follows.
    private static void NextTurnUnder(RunPlayback play, int judgment)
    {
        play.CombatDriver!.EndTurn();
        Accept(play, judgment);
    }

    // ── the three judgments ───────────────────────────────────────────────────────────────────────────────

    // Only the custom you accepted is ordinary law. Judged under Conduct, a matched pair says nothing.
    [Fact]
    public void Only_the_accepted_judgment_is_law()
    {
        var (play, session, thorns) = Start("stay_of_judgment",
            [OneCostWorking, OneCostWorking, TwoCost, TwoCost, OneCost]);

        Play(play, session, OneCostWorking, thorns);
        Play(play, session, OneCostWorking, thorns); // a matched pair — but Measure was not accepted
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, TwoCost, thorns);
        Play(play, session, TwoCost, thorns);        // the fourth real card: Conduct answers
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // …and accepting the Measure makes the pair the breach instead.
    [Fact]
    public void Accepting_the_measure_makes_a_matched_pair_the_breach()
    {
        // Demand Redress with nothing to cash does nothing at all, so the licence the fight opened with is
        // the only one in play.
        var (play, session, thorns) = Start("demand_redress",
            [.. Enumerable.Repeat(OneCostWorking, 5)]);

        NextTurnUnder(play, judgment: 2);
        Play(play, session, OneCostWorking, thorns);
        Play(play, session, OneCostWorking, thorns);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // Judgment of Standing is about the licence, and it is asked as the turn ends.
    [Fact]
    public void Judgment_of_standing_reads_an_empty_licence_at_the_bell()
    {
        var (play, _, thorns) = Start("hear_the_trespass",
            [.. Enumerable.Repeat(TwoCost, 5)]);

        NextTurnUnder(play, judgment: 3);

        // A licence still in hand at the bell: nothing to answer.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, TrespassFrom(play, thorns));
        play.Dispose();
    }

    // ── Binding Judgment ──────────────────────────────────────────────────────────────────────────────────

    // Three breaches make standing, and the custom that made it binds itself for two of your turns —
    // whatever you accept next.
    [Fact]
    public void The_judgment_that_made_the_standing_becomes_binding()
    {
        var (play, session, thorns) = Start("demand_redress",
            [.. Enumerable.Repeat(OneCostWorking, 5)]);

        // Four turns judged under the Measure, playing a matched pair in each.
        for (var turn = 0; turn < 4; turn++)
        {
            NextTurnUnder(play, judgment: 2);
            Play(play, session, OneCostWorking, thorns);
            Play(play, session, OneCostWorking, thorns);
        }

        Assert.Equal(1, FightProbe.StacksOf(Thorns(play), ActThree.ClaimId));
        Assert.Equal(2, FightProbe.StacksOf(Thorns(play), BindingMeasure));
        play.Dispose();
    }

    // A Binding Judgment is law in ADDITION to what you accept — which is what makes the second one hurt.
    [Fact]
    public void A_binding_judgment_is_law_on_top_of_the_one_you_accept()
    {
        var (play, session, thorns) = Start("stay_of_judgment",
            [OneCostWorking, OneCostWorking, TwoCost, TwoCost, OneCost],
            (BindingMeasure, 2));

        Play(play, session, OneCostWorking, thorns);
        Play(play, session, OneCostWorking, thorns); // Conduct says nothing; the binding Measure does

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // Two turns and it lifts by itself.
    [Fact]
    public void A_binding_judgment_runs_down_on_its_own()
    {
        var (play, _, _) = Start("stay_of_judgment",
            [.. Enumerable.Repeat(TwoCost, 5)], (BindingStanding, 2));

        NextTurnUnder(play, judgment: 1);
        Assert.Equal(1, FightProbe.StacksOf(Thorns(play), BindingStanding));

        NextTurnUnder(play, judgment: 1);
        Assert.Equal(0, FightProbe.StacksOf(Thorns(play), BindingStanding));
        play.Dispose();
    }

    // ── Full Redress ──────────────────────────────────────────────────────────────────────────────────────

    // Settling in full strikes a Claim, runs the OLDEST binding down a turn and costs the Magistrate 7 HP.
    [Fact]
    public void Settling_in_full_runs_the_oldest_binding_down()
    {
        var (play, session, thorns) = Start("demand_redress",
            [.. Enumerable.Repeat(TwoCost, 5)],
            (ActThree.ClaimId, 2), (BindingMeasure, 2), (BindingStanding, 2));

        NextTurnUnder(play, judgment: 1); // a Claim is cashed for a demand of 2, and the bindings run down
        Assert.Equal(2, OwedTo(play, thorns));
        Assert.Equal(1, FightProbe.StacksOf(Thorns(play), BindingMeasure));

        for (var i = 0; i < 2; i++)
        {
            var card = play.CombatDriver.Current!.Hand
                .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
            play.CombatDriver.PlayCard(card.Id, thorns);
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]);
        }
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(0, OwedTo(play, thorns));

        var health = Thorns(play).Health.Current;
        var claims = FightProbe.StacksOf(Thorns(play), ActThree.ClaimId);
        play.CombatDriver!.EndTurn();

        // The oldest binding is the shortest-lived — the Measure, at one turn left — and it goes out.
        Assert.Equal(0, FightProbe.StacksOf(Thorns(play), BindingMeasure));
        Assert.Equal(claims - 1, FightProbe.StacksOf(Thorns(play), ActThree.ClaimId));
        Assert.True(Thorns(play).Health.Current <= health - 7, "the redress costs the Magistrate 7 HP");
        play.Dispose();
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // "Hear the Trespass — 15 +2 per current Magistrate-source Trespass, max 21."
    [Fact]
    public void Hearing_the_trespass_reads_what_is_on_the_record()
    {
        var (bare, _, _) = Start("hear_the_trespass",
            [.. Enumerable.Repeat(TwoCost, 5)]);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 15, Hero(bare).Health.Current);
        bare.Dispose();
    }

    // "Stay of Judgment — the player gains 1 Safe-Conduct and the Magistrate guards for 16." It hands back
    // exactly what its own law takes, which is why a fight where it came round every turn would be a fight
    // where nothing ever landed.
    [Fact]
    public void A_stay_of_judgment_hands_a_licence_back_and_guards()
    {
        var (play, session, thorns) = Start("stay_of_judgment",
            [.. Enumerable.Repeat(OneCostWorking, 5)]);

        NextTurnUnder(play, judgment: 2);
        Play(play, session, OneCostWorking, thorns);
        Play(play, session, OneCostWorking, thorns); // the pair is refused by the opening licence

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // and handed straight back
        Assert.Equal(0, TrespassFrom(play, thorns));                              // nothing on the record
        Assert.Equal(16, Block(Thorns(play)));
        play.Dispose();
    }

    // "Establish the Judgment": whatever you accept next turn is Binding for that turn as well.
    [Fact]
    public void Establishing_the_judgment_binds_whatever_you_accept()
    {
        var (play, _, _) = Start("establish_the_judgment",
            [.. Enumerable.Repeat(TwoCost, 5)]);

        NextTurnUnder(play, judgment: 3); // Standing, and it is binding for this turn too

        Assert.Equal(1, FightProbe.StacksOf(Thorns(play), BindingStanding));
        Assert.Equal(0, FightProbe.StacksOf(Thorns(play), ActThree.EstablishPendingId));
        play.Dispose();
    }

    // The signature: it spends everything, deals 30, names a price and takes a turn off every ruling.
    [Fact]
    public void The_judgment_of_the_green_docket_spends_everything()
    {
        var (play, _, thorns) = Start("judgment_of_the_green_docket",
            [.. Enumerable.Repeat(TwoCost, 5)],
            (ActThree.ClaimId, 3), (BindingConduct, 2), (BindingMeasure, 2));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 30, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Thorns(play), ActThree.ClaimId));
        Assert.Equal(2, OwedTo(play, thorns));
        // One turn from the signature, and one more from the bell that followed it.
        Assert.True(FightProbe.StacksOf(Thorns(play), BindingConduct) <= 1);
        Assert.True(FightProbe.StacksOf(Thorns(play), BindingMeasure) <= 1);
        play.Dispose();
    }
}
