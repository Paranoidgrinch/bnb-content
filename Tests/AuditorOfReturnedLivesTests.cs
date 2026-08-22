using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// "The damage may be real. The record may still be insufficient." Three Accounts come due as the Auditor's
// body goes, and each is closed with 2 Supporting Documentation or left unreconciled for a Discrepancy. Leave
// the Account of LIFE open and its death is not final. These tests answer an Account both ways and kill it
// both ways.
public class AuditorOfReturnedLivesTests
{
    private const string Deed = "paper_cut";

    private static CombatantState Auditor(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantState Me(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static bool Has(RunPlayback play, CombatantId id, string status) =>
        Auditor(play, id).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static int Discrepancy(RunPlayback play, CombatantId id) =>
        FightProbe.StacksOf(Auditor(play, id), AuditorOfReturnedLives.DiscrepancyId);

    private static int Documentation(RunPlayback play) =>
        FightProbe.StacksOf(Me(play), AuditorOfReturnedLives.DocumentationId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        int? bossHealth, params string[] intents)
    {
        var probe = FightProbe.Solo(AuditorOfReturnedLives.EnemyId, intents[0], 9);
        var body = probe.Enemies.Single() with
        {
            Actions = [.. intents.Select(i =>
                new EnemyActionDefinitionId($"{AuditorOfReturnedLives.EnemyId}.{i}"))],
            MaxHealth = bossHealth ?? probe.Enemies.Single().MaxHealth,
        };

        return FightProbe.Start(
            new EncounterDefinition(probe.Id, [body], probe.HeroResources, probe.HeroStartingStatuses,
                probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects),
            deck: [.. Enumerable.Repeat(Deed, 20)],
            health: 900);
    }

    private static void EndTurn(RunPlayback play, int option = 0)
    {
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
    }

    // Answer every citation the Auditor puts on the hand, which is how Documentation is earned at all.
    private static void AnswerCitations(RunPlayback play, CombatantId at)
    {
        while (Hand(play).FirstOrDefault(c =>
            c.Marks.Contains(new TagId(AuditorOfReturnedLives.AuditorReferenceMark))) is { } cited)
        {
            play.CombatDriver!.PlayCard(cited.Id, at);
        }
    }

    // §8.2: its own citations are the universal path to the audit resource — every deck can pay.
    [Fact]
    public void Answering_its_citations_is_what_pays_the_record()
    {
        var (play, _, auditor) = Fight(null, "open_the_account");

        Assert.Equal(0, Documentation(play));

        EndTurn(play);   // Request Supporting Documentation — the citation lands on the next hand.
        AnswerCitations(play, auditor);

        Assert.Equal(1, Documentation(play));
    }

    // §8.3: crossing a threshold queues the Account and gives the player one complete turn to answer it.
    [Fact]
    public void Crossing_a_threshold_puts_an_account_on_the_table()
    {
        // Just under the Account of Identity.
        var (play, _, auditor) = Fight(AuditorOfReturnedLives.IdentityHealth - 5, "open_the_account");

        Assert.False(Has(play, auditor, AuditorOfReturnedLives.AuditPendingId));

        EndTurn(play);

        Assert.True(Has(play, auditor, AuditorOfReturnedLives.AuditPendingId),
            "the Auditor never queued the Account it had already crossed");
    }

    // §8.4/§8.5: withholding costs a Discrepancy, and Discrepancy is what makes its case harder.
    [Fact]
    public void Withholding_leaves_the_account_unreconciled()
    {
        var (play, _, auditor) = Fight(AuditorOfReturnedLives.IdentityHealth - 5, "open_the_account");

        EndTurn(play);
        Assert.True(Has(play, auditor, AuditorOfReturnedLives.AuditPendingId));

        // With no Documentation there is nothing to submit; the Account goes unreconciled without asking.
        Assert.Equal(0, Documentation(play));

        EndTurn(play);   // The Auditor's next action IS the resolution.

        Assert.Equal(1, Discrepancy(play, auditor));
        Assert.False(Has(play, auditor, AuditorOfReturnedLives.AuditPendingId));
    }

    // …and submitting closes it for two lines of the record, with no Discrepancy at all.
    [Fact]
    public void Submitting_two_lines_closes_the_account()
    {
        // Two turns of citations before the threshold, so there is something to submit.
        var (play, _, auditor) = Fight(AuditorOfReturnedLives.IdentityHealth + 4, "open_the_account");

        EndTurn(play);
        AnswerCitations(play, auditor);
        EndTurn(play);
        AnswerCitations(play, auditor);
        Assert.Equal(2, Documentation(play));

        // Cross the threshold ourselves.
        while (Auditor(play, auditor).Health.Current > AuditorOfReturnedLives.IdentityHealth
               && Hand(play).Count > 0)
        {
            play.CombatDriver!.PlayCard(Hand(play)[0].Id, auditor);
        }

        EndTurn(play);   // The Account is queued at the Auditor's turn start.
        Assert.True(Has(play, auditor, AuditorOfReturnedLives.AuditPendingId));

        // Option 0 is SUBMIT — but the prompt is answered on the DRAW, which has already happened inside
        // EndTurn, so the answer was supplied there.
        EndTurn(play, option: 0);

        Assert.Equal(0, Discrepancy(play, auditor));
        Assert.Equal(0, Documentation(play));
    }

    // §8.11: an Account resolving is the boss's whole action — a threshold never costs a burst on top of it.
    [Fact]
    public void Resolving_an_account_replaces_the_attack()
    {
        var (play, _, auditor) = Fight(AuditorOfReturnedLives.IdentityHealth - 5, "final_reconciliation");

        EndTurn(play);   // queues the Account
        Assert.True(Has(play, auditor, AuditorOfReturnedLives.AuditPendingId));

        var before = Me(play).Health.Current;
        EndTurn(play);   // resolution instead of Preliminary Balance

        Assert.Equal(before, Me(play).Health.Current);
        Assert.Equal(1, Discrepancy(play, auditor));
    }

    // Walk the audit to the end: answer every Account by withholding, until the Account of Life has resolved.
    // A probe cannot start below the Life threshold and skip the other two — §8.12 queues only the NEXT
    // Account, so no burst turn can jump the audit, and the walk is what that rule looks like from outside.
    private static void WalkToTheLastAccount(RunPlayback play, CombatantId auditor, bool answerCitations)
    {
        for (var turn = 0; turn < 14; turn++)
        {
            if (Has(play, auditor, AuditorOfReturnedLives.DeathClauseId)
                || Discrepancy(play, auditor) >= AuditorOfReturnedLives.DiscrepancyMaximum)
            {
                return;
            }

            if (answerCitations)
                AnswerCitations(play, auditor);

            // Option 1 is WITHHOLD, which is what keeps the Documentation for the final receipt.
            EndTurn(play, option: 1);
        }
    }

    // §8.13: with the Account of Life unreconciled and fewer than 2 Documentation on the table, the death is
    // not final — it comes back at 72 HP into the Closing Audit.
    //
    // The probe body is the Life threshold itself, because death prevention CLAMPS its surviving health to
    // the combatant's maximum: a frailer probe would quietly turn "returns at 72" into "returns at 67".
    [Fact]
    public void An_undocumented_death_is_not_final()
    {
        var (play, _, auditor) = Fight(AuditorOfReturnedLives.LifeHealth, "final_reconciliation");

        WalkToTheLastAccount(play, auditor, answerCitations: false);

        Assert.Equal(0, Documentation(play));
        Assert.True(Has(play, auditor, AuditorOfReturnedLives.DeathClauseId),
            "the Account of Life went unreconciled without raising the clause");

        // Kill it — and stop the moment it comes back. A loop that kept swinging would beat the returned
        // body down past the number this test is about.
        for (var turn = 0; turn < 10 && play.CombatDriver?.Current is not null; turn++)
        {
            while (play.CombatDriver.Current is { } c && c.Hand.Count > 0
                   && !Has(play, auditor, AuditorOfReturnedLives.ClosingAuditId))
            {
                play.CombatDriver.PlayCard(c.Hand[0].Id, auditor);
            }
            if (play.CombatDriver.Current is null
                || Has(play, auditor, AuditorOfReturnedLives.ClosingAuditId))
                break;
            EndTurn(play, option: 1);
        }

        Assert.True(play.CombatDriver?.Current is not null, "the combat ended: the death was treated as final");
        Assert.True(Has(play, auditor, AuditorOfReturnedLives.ClosingAuditId),
            "the Auditor never came back to close the book");
        Assert.InRange(Auditor(play, auditor).Health.Current, 1, AuditorOfReturnedLives.LifeHealth);
    }

    // …and holding the two lines a final receipt costs makes it final after all. This is the design's own
    // counterplay, played out: withhold on the early Accounts to keep the Documentation for the last one.
    [Fact]
    public void A_documented_death_is_accepted()
    {
        var (play, _, auditor) = Fight(AuditorOfReturnedLives.LifeHealth, "open_the_account");

        WalkToTheLastAccount(play, auditor, answerCitations: true);

        // Its own citations paid for the receipt, and withholding kept it.
        for (var turn = 0; turn < 4 && Documentation(play) < AuditorOfReturnedLives.DocumentationPerAccount; turn++)
        {
            EndTurn(play, option: 1);
            AnswerCitations(play, auditor);
        }

        Assert.True(Documentation(play) >= AuditorOfReturnedLives.DocumentationPerAccount,
            "the probe never earned the two lines");
        Assert.False(Has(play, auditor, AuditorOfReturnedLives.DeathClauseId),
            "the clause stood even though the receipt was affordable");

        for (var turn = 0; turn < 10 && play.CombatDriver?.Current is not null; turn++)
        {
            while (play.CombatDriver.Current is { } c && c.Hand.Count > 0)
                play.CombatDriver.PlayCard(c.Hand[0].Id, auditor);
            if (play.CombatDriver.Current is null)
                break;
            EndTurn(play, option: 1);
        }

        Assert.True(play.CombatDriver?.Current is null,
            "the Auditor came back even though the death was documented");
    }
}
