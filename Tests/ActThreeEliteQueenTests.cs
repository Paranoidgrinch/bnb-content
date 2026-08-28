using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 5 — Ant Queen of the Proper Line. One question, asked four ways: may you choose your own
// target? Striking out of order is the QUEEN's Trespass, and the standing you hand her is what pays for the
// Bearers you killed to walk back into the procession.
public class ActThreeEliteQueenTests
{
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private const string First = "line_bearer_first";
    private const string Second = "line_bearer_second";
    private const string Third = "line_bearer_third";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Body(RunPlayback play, string status) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status)));

    private static CombatantState Queen(RunPlayback play) => Body(play, ActThree.AntQueenId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // The whole procession, as the game fields it: the Queen and her three Bearers, each narrowed to one
    // intent so a test can say what the line was doing.
    private static EncounterDefinition Procession(
        string queenIntent, string bearerIntent = "carry_forward", int? bearerHealth = null) =>
        FightProbe.Roster("ant_queen", energy: 9,
            (ActThree.AntQueenEnemyId, queenIntent, null),
            ("first_line_bearer", bearerIntent, bearerHealth),
            ("second_line_bearer", bearerIntent, bearerHealth),
            ("third_line_bearer", bearerIntent, bearerHealth));

    private static EncounterDefinition ProcessionWithStanding(
        string queenIntent, string bearerIntent, int claims)
    {
        var roster = FightProbe.Roster("ant_queen_standing", energy: 9,
            (ActThree.AntQueenEnemyId, queenIntent, null),
            ("first_line_bearer", bearerIntent, null),
            ("second_line_bearer", bearerIntent, null),
            ("third_line_bearer", bearerIntent, null));
        var queen = roster.Enemies[0] with
        {
            StartingStatuses =
            [
                .. roster.Enemies[0].StartingStatuses ?? [],
                new StartingStatusSpec(new StatusDefinitionId(ActThree.ClaimId), claims),
            ],
        };
        return new EncounterDefinition(roster.Id, [queen, .. roster.Enemies.Skip(1)],
            roster.HeroResources, roster.HeroStartingStatuses, roster.HeroDisplayName,
            roster.CardsDrawnPerTurn, roster.TriggeredEffects);
    }

    // ── Do Not Break the Line ─────────────────────────────────────────────────────────────────────────────

    // Striking the front of the procession is in order, and costs nothing.
    [Fact]
    public void Striking_the_front_of_the_line_is_in_order()
    {
        var (play, session, _) = FightProbe.Start(
            Procession("royal_survey_of_the_line"),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        Play(play, session, Deed, Body(play, First).Id);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // Striking past a living Bearer is a Trespass — and it is the QUEEN's, because the Bearers hold no
    // standing of their own.
    [Fact]
    public void Striking_out_of_order_is_owed_to_the_queen()
    {
        var (play, session, _) = FightProbe.Start(
            Procession("royal_survey_of_the_line"),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        Play(play, session, Deed, Body(play, Third).Id);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        Assert.Equal(1, FightProbe.StacksOf(Body(play, Third), ActThree.PermittedExceptionId));
        play.Dispose();
    }

    // Once a turn: the Queen answers one broken line, not every card in it.
    [Fact]
    public void The_queen_answers_one_broken_line_a_turn()
    {
        var (play, session, _) = FightProbe.Start(
            Procession("royal_survey_of_the_line"),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        Play(play, session, Deed, Body(play, Third).Id); // refused
        Play(play, session, Deed, Body(play, Third).Id);
        Play(play, session, Deed, Body(play, Second).Id);

        Assert.Equal(0, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));
        play.Dispose();
    }

    // Once the line ahead of it is gone, the position is the front, and striking it is in order again.
    [Fact]
    public void A_bearer_with_nobody_ahead_of_it_is_the_front()
    {
        var (play, session, _) = FightProbe.Start(
            // Bearers of 5 HP, so one Deed clears a position.
            Procession("royal_survey_of_the_line", bearerHealth: 5),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        Play(play, session, Deed, Body(play, First).Id);   // in order — the first falls
        Play(play, session, Deed, Body(play, Second).Id);  // now the front — still in order
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, Deed, Body(play, Third).Id);   // the front, with both ahead of it gone
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Permitted Exception ───────────────────────────────────────────────────────────────────────────────

    // The Queen's own licence spent on a Bearer strikes up to 6 of its Block off with it — which is what a
    // closed formation's guard is worth, so the exception takes the whole of it.
    [Fact]
    public void A_permitted_exception_takes_the_bearers_guard_off()
    {
        var (play, session, _) = FightProbe.Start(
            ProcessionWithStanding("royal_survey_of_the_line", "hold_the_line", claims: 2),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        play.CombatDriver!.EndTurn(); // the formation closes: 4 Block on every living Bearer

        Assert.Equal(4, Block(Body(play, Third)));
        Play(play, session, Deed, Body(play, Third).Id); // struck out of order, and the licence is spent

        Assert.Equal(1, FightProbe.StacksOf(Body(play, Third), ActThree.PermittedExceptionId));
        Assert.Equal(0, Block(Body(play, Third)));
        play.Dispose();
    }

    // ── Claim thresholds ──────────────────────────────────────────────────────────────────────────────────

    // One Claim, and the acting Bearer hits for 12 instead of 9.
    [Fact]
    public void One_claim_tightens_the_line()
    {
        var (bare, _, _) = FightProbe.Start(
            Procession("royal_survey_of_the_line"),
            deck: [Working, Working, Working, Working, Working], health: 500);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 9, Hero(bare).Health.Current); // only the frontmost Bearer acts
        bare.Dispose();

        var (play, _, _) = FightProbe.Start(
            ProcessionWithStanding("royal_survey_of_the_line", "carry_forward", claims: 1),
            deck: [Working, Working, Working, Working, Working], health: 500);

        var start = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(start - 12, Hero(play).Health.Current);
        play.Dispose();
    }

    // Two Claims and every living Bearer guards at the end of the Queen's turn.
    [Fact]
    public void Two_claims_close_the_formation()
    {
        var (play, _, _) = FightProbe.Start(
            ProcessionWithStanding("royal_survey_of_the_line", "hold_the_line", claims: 2),
            deck: [Working, Working, Working, Working, Working], health: 500);

        play.CombatDriver!.EndTurn();

        Assert.Equal(4, Block(Body(play, Second))); // a positional body that never acted
        Assert.Equal(4, Block(Body(play, Third)));
        play.Dispose();
    }

    // ── Count the Proper Order ────────────────────────────────────────────────────────────────────────────

    // "12 +4 per living Bearer, max 24" — a full procession is the maximum, and a broken one is worth less.
    [Fact]
    public void Counting_the_order_reads_the_living_line()
    {
        var (play, session, _) = FightProbe.Start(
            Procession("count_the_proper_order", bearerHealth: 5),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(before - 24 - 9, Hero(play).Health.Current); // 12 + 4×3, and the front Bearer's 9

        Play(play, session, Deed, Body(play, First).Id); // one position emptied
        var start = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(start - 20 - 9, Hero(play).Health.Current); // 12 + 4×2
        play.Dispose();
    }

    // ── Replace the Fallen ────────────────────────────────────────────────────────────────────────────────

    // Standing plus a charge calls the lowest-numbered missing Bearer back at 18 HP — and spends both.
    [Fact]
    public void The_queen_calls_the_lowest_fallen_bearer_back()
    {
        // Bearers of 24, so the 18 the Queen calls one back at is under its own strength and readable.
        var roster = FightProbe.Roster("ant_queen_rebuild", energy: 9,
            (ActThree.AntQueenEnemyId, "replace_the_fallen", null),
            ("first_line_bearer", "hold_the_line", 24),
            ("second_line_bearer", "hold_the_line", 24),
            ("third_line_bearer", "hold_the_line", 24));
        var queen = roster.Enemies[0] with
        {
            StartingStatuses =
            [
                .. roster.Enemies[0].StartingStatuses ?? [],
                new StartingStatusSpec(new StatusDefinitionId(ActThree.ClaimId), 1),
            ],
        };

        var (play, session, _) = FightProbe.Start(
            new EncounterDefinition(roster.Id, [queen, .. roster.Enemies.Skip(1)],
                roster.HeroResources, roster.HeroStartingStatuses, roster.HeroDisplayName,
                roster.CardsDrawnPerTurn, roster.TriggeredEffects),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        for (var i = 0; i < 4; i++)
            Play(play, session, Deed, Body(play, First).Id); // 6 a card: the first position falls
        Assert.Equal(0, Body(play, First).Health.Current);

        play.CombatDriver!.EndTurn();

        Assert.Equal(18, Body(play, First).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Queen(play), ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Queen(play), ActThree.ReconstructionChargeId));
        play.Dispose();
    }

    // ── the Queen's death ─────────────────────────────────────────────────────────────────────────────────

    // "Queen death: all surviving Bearers collapse." The procession is hers and has nothing to carry.
    [Fact]
    public void The_line_collapses_with_the_queen()
    {
        var roster = FightProbe.Roster("ant_queen_death", energy: 9,
            (ActThree.AntQueenEnemyId, "royal_survey_of_the_line", 5),
            ("first_line_bearer", "hold_the_line", null),
            ("second_line_bearer", "hold_the_line", null),
            ("third_line_bearer", "hold_the_line", null));

        var (play, session, _) = FightProbe.Start(roster, deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        Play(play, session, Deed, Queen(play).Id);

        // "…and combat ends", which is the whole claim: the procession is hers and has nothing to carry once
        // she is gone. It used to assert three bodies at 0 health instead, because that is all that happened —
        // a Bearer set to zero was not DOWNED unless damage put it there, so the three of them stood at zero
        // and the elite could not be finished. See ADAPTATIONS.md §"The boss that would not end, and the crash
        // that was actually killing the walk".
        Assert.Null(session.Error);
        Assert.Null(play.CombatDriver!.Current);
        play.Dispose();
    }
}
