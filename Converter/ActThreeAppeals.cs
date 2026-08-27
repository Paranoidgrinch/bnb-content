using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 7 — The Mire of Appeals. No solo encounters either: an appeal needs an existing grievance,
// another party, and a procedure that changes its legal status. Neither of these two creates standing. They
// move it, freeze it, and occasionally extinguish it — which is a different kind of pressure entirely, and
// the first place in the act where the player can want a Claim to exist.
public static partial class ActThree
{
    public const string AttachToTheAppealId = "attach_to_the_appeal";
    public const string AppealRememberedId = "appeal_remembered";
    public const string AppealTakenThisTurnId = "appeal_taken_this_turn";
    public const string UnderReviewId = "under_review";
    public const string SedgeBenchId = "under_review_bench";

    // ── Ditch Lamprey of Appeals ──────────────────────────────────────────────────────────────────────────

    // "The first time each round another party is granted a Claim, and the Lamprey holds fewer than they do,
    // the Lamprey may take that Claim onto itself. Remember whose it was; it may hand it back later."
    //
    // A grievance can temporarily belong to the appeal itself. Nothing is created and nothing is destroyed —
    // which is why the Moot standing next to it hears none of this.
    public static StatusData AttachToTheAppeal()
    {
        var lamprey = Lawgiver(AttachToTheAppealId);
        var claimant = CombatantTargetSelectors.EventTarget;

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                    new AndExpression<TContext>(
                        // Somebody else's grievance, and one the Lamprey is allowed to reach.
                        new AndExpression<TContext>(
                            new ComparisonExpression<TContext>(
                                new CombatantStatusStacksExpression<TContext>(
                                    claimant, new StatusDefinitionId(AttachToTheAppealId)),
                                ComparisonOperator.Equal,
                                new ConstantExpression<TContext>(0)),
                            new ComparisonExpression<TContext>(
                                new CombatantStatusStacksExpression<TContext>(
                                    lamprey, new StatusDefinitionId(AppealTakenThisTurnId)),
                                ComparisonOperator.Equal,
                                new ConstantExpression<TContext>(0))),
                        // …travelling upstream: the appeal only attaches to standing above its own.
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(
                                lamprey, new StatusDefinitionId(ClaimId)),
                            ComparisonOperator.Less,
                            new CombatantStatusStacksExpression<TContext>(
                                claimant, new StatusDefinitionId(ClaimId))))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(
                        lamprey, new StatusDefinitionId(AppealTakenThisTurnId),
                        new ConstantExpression<TContext>(1)),
                    // Whose it was, so it can go back.
                    new ApplyStatusNode<TContext>(
                        claimant, new StatusDefinitionId(AppealRememberedId),
                        new ConstantExpression<TContext>(1)),
                    TransferClaim<TContext>(ClaimsOthersMayTake(claimant), lamprey),
                ])));

        var release = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    lamprey, new StatusDefinitionId(AppealTakenThisTurnId))));

        return Rule(AttachToTheAppealId, "Attach to the Appeal",
            "Once a turn, a Claim granted to a party holding more than the Ditch Lamprey is taken onto the "
            + "Lamprey instead. It remembers whose it was, and may hand it back.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    release, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData AppealRemembered() =>
        Marker(AppealRememberedId, "Under Appeal",
            "This party's grievance is being argued by the Ditch Lamprey. It may come back.");

    public static StatusData AppealTakenThisTurn() =>
        Marker(AppealTakenThisTurnId, "Attached",
            "The Lamprey has already taken up one appeal this turn.");

    // The Lamprey's own action: give it back. Nothing is created — the same Claim goes home.
    private static EffectProgram<EnemyActionContext> ReturnTheAppeal()
    {
        var lamprey = CombatantTargetSelectors.Source;
        var claimant = Lawgiver(AppealRememberedId);

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCurrentHealthExpression<EnemyActionContext>(claimant),
                        ComparisonOperator.Greater,
                        new ConstantExpression<EnemyActionContext>(0)),
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            lamprey, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<EnemyActionContext>(0))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    TransferClaim<EnemyActionContext>(lamprey, claimant),
                    new RemoveStatusNode<EnemyActionContext>(
                        claimant, new StatusDefinitionId(AppealRememberedId)),
                ])));
    }

    // ── The Sedge Bench ───────────────────────────────────────────────────────────────────────────────────

    // "At the start of its turn, mark another party's oldest eligible Claim as Under Review. While reviewed
    // it still exists and still counts, but cannot be transferred or consumed."
    //
    // An appeal does not erase ownership. It suspends the Claim long enough for settlement to extinguish it,
    // which is the other half of the Bench and lives in the Wergild settlement itself.
    public static StatusData UnderReviewBench()
    {
        var bench = CombatantTargetSelectors.Source;
        var matter = CombatantTargetSelectors.HighestStatusStacks(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(SedgeBenchId)),
            new StatusDefinitionId(ClaimId));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    // The Bench's own turn …
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            bench, new StatusDefinitionId(SedgeBenchId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    // … and a matter to hear.
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            matter, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    matter, new StatusDefinitionId(UnderReviewId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))));

        return Rule(SedgeBenchId, "Under Review",
            "At the start of its turn the Sedge Bench takes up whichever party holds the most Claims. A "
            + "reviewed Claim still counts, but nobody else may move or spend it.",
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()))]);
    }

    public static StatusData UnderReview() =>
        Marker(UnderReviewId, "Under Review",
            "This party's standing is before the Sedge Bench: it still counts, but nobody else may move or "
            + "spend it, and settling its demand in full extinguishes it.");

    // "Call the Matter": a small demand from the REVIEWED party, without touching the Claim being reviewed.
    // The Bench does not collect it — the reviewed party does — which is what makes settlement able to
    // extinguish the Claim.
    private static EffectProgram<EnemyActionContext> CallTheMatter()
    {
        var reviewed = Lawgiver(UnderReviewId);

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCurrentHealthExpression<EnemyActionContext>(reviewed),
                    ComparisonOperator.Greater,
                    new ConstantExpression<EnemyActionContext>(0)),
                DemandWergild<EnemyActionContext>(reviewed, 1)));
    }

    // The Bench's intents, which need a party the flat effect list has no name for.
    public static EffectProgram<EnemyActionContext>? AppealsIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "the_sedge_bench.call_the_matter" => CallTheMatter(),
            "ditch_lamprey_of_appeals.return_the_appeal" => ReturnTheAppeal(),
            _ => null,
        };
}
