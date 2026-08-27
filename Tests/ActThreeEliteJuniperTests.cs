using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 6 — Juniper Injunction. The only body in the act that attacks the player's ANSWERS. Its
// standing forbids a remedy; the fight is about keeping a way out open, and the design's two safety rules
// (never both payment routes at once; settling always frees one) are built as rules, not trusted to numbers.
public class ActThreeEliteJuniperTests
{
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Hedge(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    // The hedge's FIRST grant is made rather than asked (the opening hand is dealt before there is anybody
    // to put the question to), and it is always leave for Deeds. Every turn after that, it asks.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Hedge) Start(
        string intentId, IReadOnlyList<string> deck, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ActThree.JuniperEnemyId, intentId, energy: 9, statuses),
            deck: deck, health: 500);

    // End the turn and answer the hedge's question for the turn that follows.
    private static void NextTurnUnderLeave(RunPlayback play, int use)
    {
        play.CombatDriver!.EndTurn();
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([use - 1]);
    }

    private static void GrantLeave(RunPlayback play, int use)
    {
        if (play.CombatDriver!.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([use - 1]);
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static void MakeAmends(
        RunPlayback play, InteractiveRunSession session, CombatantId at, params int[] answers)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        var next = 0;
        for (var guard = 0; guard < 6; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([answers[Math.Min(next++, answers.Length - 1)]]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.True(session.Error is null, session.Error);
    }

    // ── Granted Use ───────────────────────────────────────────────────────────────────────────────────────

    // The hedge asks at the bell, and what you answer is the only way you may act that turn.
    [Fact]
    public void Stepping_off_the_granted_leave_is_a_trespass()
    {
        var (play, session, hedge) = Start("bind_the_path",
            [Deed, Working, Working, Working, Working]);

        Play(play, session, Deed, hedge);   // within the leave granted
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, Working, hedge); // off it
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // And the hedge answers one step off the path a turn, not every one.
    [Fact]
    public void The_hedge_answers_one_step_off_the_path_a_turn()
    {
        var (play, session, hedge) = Start("bind_the_path",
            [Working, Working, Working, Working, Working]);

        Play(play, session, Working, hedge);
        Play(play, session, Working, hedge);
        Play(play, session, Working, hedge);

        Assert.Equal(0, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));
        play.Dispose();
    }

    // The cost categories are leave as well: cheap cards, or dear ones.
    [Fact]
    public void Leave_may_be_granted_by_price_instead_of_by_kind()
    {
        var (play, session, hedge) = Start("bind_the_path",
            [.. Enumerable.Repeat(Deed, 5)]);

        NextTurnUnderLeave(play, use: 4); // leave to act with cards costing 2 or more
        Play(play, session, Deed, hedge); // a Deed, but a cheap one

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── the injunctions ───────────────────────────────────────────────────────────────────────────────────

    // Standing prepares an injunction, and safe passage is the first one the hedge reaches for.
    [Fact]
    public void Standing_prepares_the_injunction_against_safe_passage()
    {
        var (play, _, hedge) = Start("demand_relief_in_proper_form",
            [Working, Working, Working, Working, Working]);

        play.CombatDriver!.EndTurn(); // 2 demanded
        GrantLeave(play, 1);
        play.CombatDriver.EndTurn();  // left owing: standing is granted, and an injunction prepared

        Assert.Equal(1, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionSafePassageId));
        play.Dispose();
        Assert.NotEqual(default, hedge);
    }

    // Under it, the licence has no say — and it is still there afterwards, because an injunction forbids a
    // remedy rather than taking it away.
    [Fact]
    public void An_enjoined_licence_cannot_be_spent_and_is_not_lost()
    {
        var (play, session, hedge) = Start("bind_the_path",
            [Working, Working, Working, Working, Working],
            (ActThree.InjunctionSafePassageId, 1));

        Play(play, session, Working, hedge);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // untouched
        Assert.Equal(1, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));
        Assert.Equal(0, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionSafePassageId)); // and it lifts
        play.Dispose();
    }

    // The hard safety rule: never both ways of paying. With coin enjoined, a second grant prepares nothing.
    [Fact]
    public void The_hedge_never_enjoins_both_payment_routes()
    {
        var (play, _, _) = Start("demand_relief_in_proper_form",
            [Working, Working, Working, Working, Working],
            (ActThree.InjunctionSafePassageId, 1), (ActThree.InjunctionCoinId, 1));

        play.CombatDriver!.EndTurn();
        GrantLeave(play, 1);
        play.CombatDriver.EndTurn(); // a Claim is granted, and there is nothing legal left to enjoin

        Assert.Equal(0, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionOfferingId));
        play.Dispose();
    }

    // Against Coin closes the coin route and leaves the offering open.
    [Fact]
    public void An_injunction_against_coin_leaves_the_offering_open()
    {
        var (play, session, hedge) = Start("demand_relief_in_proper_form",
            [Working, Working, Working, Working, Working],
            (ActThree.InjunctionCoinId, 1));

        play.CombatDriver!.EndTurn(); // 2 demanded
        GrantLeave(play, 1);

        MakeAmends(play, session, hedge, 0);  // pay in coin — enjoined, so nothing moves
        Assert.Equal(2, OwedTo(play, hedge));

        MakeAmends(play, session, hedge, 1);  // an offering is still legal
        Assert.Equal(1, OwedTo(play, hedge));
        play.Dispose();
    }

    // ── Petition for Relief ───────────────────────────────────────────────────────────────────────────────

    // Settling in full always digs one way out again: an injunction lifts, a Claim is struck off, and the
    // hedge loses 6 HP that no Block sees.
    [Fact]
    public void Settling_in_full_lifts_an_injunction_and_costs_the_hedge_six()
    {
        var (play, session, hedge) = Start("demand_relief_in_proper_form",
            [Working, Working, Working, Working, Working],
            (ActThree.InjunctionSafePassageId, 1), (ActThree.InjunctionCoinId, 1),
            (ActThree.ClaimId, 1));

        // Leave to act with cards costing 0 or 1, so that paying is itself within the granted path — the
        // Make Amends card is free, and stepping off the path is a different question from settling.
        NextTurnUnderLeave(play, use: 3);

        MakeAmends(play, session, hedge, 1); // an offering — coin is enjoined
        MakeAmends(play, session, hedge, 1);
        Assert.Equal(0, OwedTo(play, hedge));

        var health = Hedge(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionSafePassageId)); // the first lifts
        Assert.Equal(1, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionCoinId));        // and only one
        Assert.Equal(0, FightProbe.StacksOf(Hedge(play), ActThree.ClaimId));
        Assert.True(Hedge(play).Health.Current <= health - 6, "the relief costs the hedge 6 HP");
        play.Dispose();
    }

    // Paying is itself a step off the path when the leave granted was for Deeds — and under the injunction
    // against safe passage the licence cannot be spent on it, so the violation lands and the injunction
    // lifts, which is the design's "then the Injunction expires" happening the moment it bites.
    [Fact]
    public void Even_settling_can_be_a_step_off_the_granted_path()
    {
        var (play, session, hedge) = Start("demand_relief_in_proper_form",
            [Working, Working, Working, Working, Working],
            (ActThree.InjunctionSafePassageId, 1));

        play.CombatDriver!.EndTurn(); // 2 demanded
        GrantLeave(play, 1);          // leave to act: Deeds

        MakeAmends(play, session, hedge, 0); // a free card, and not a Deed

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // the licence had no say
        Assert.Equal(1, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));
        Assert.Equal(0, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionSafePassageId));
        play.Dispose();
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // "Narrow the Granted Use": next turn there is one achievable use and nothing to choose.
    [Fact]
    public void Narrowing_the_path_leaves_nothing_to_choose()
    {
        var (play, session, hedge) = Start("narrow_the_granted_use",
            [Working, Working, Working, Working, Working]);

        play.CombatDriver!.EndTurn();

        Assert.Null(play.CombatDriver.PendingOptionChoice); // the hedge does not ask
        Play(play, session, Working, hedge);                // …and leave was granted for Deeds

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // "No Remedy Is Absolute — 15 damage; with 2 active Injunctions +7, max 22."
    [Fact]
    public void No_remedy_is_absolute_reads_a_full_pair_of_injunctions()
    {
        var (bare, _, _) = Start("no_remedy_is_absolute",
            [Working, Working, Working, Working, Working]);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 15, Hero(bare).Health.Current);
        bare.Dispose();

        var (play, _, _) = Start("no_remedy_is_absolute",
            [Working, Working, Working, Working, Working],
            (ActThree.InjunctionSafePassageId, 1), (ActThree.InjunctionCoinId, 1));
        var start = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(start - 22, Hero(play).Health.Current);
        play.Dispose();
    }

    // The Final Injunction spends everything the hedge has, forbids safe passage and one payment route, and
    // deals no damage at all.
    [Fact]
    public void The_final_injunction_forbids_rather_than_strikes()
    {
        var (play, _, hedge) = Start("the_final_injunction",
            [Working, Working, Working, Working, Working],
            (ActThree.ClaimId, 3));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hedge(play), ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionSafePassageId));
        Assert.Equal(1, FightProbe.StacksOf(Hedge(play), ActThree.InjunctionOfferingId));
        Assert.Equal(3, OwedTo(play, hedge));
        Assert.Equal(16, Block(Hedge(play)));
        play.Dispose();
    }
}
