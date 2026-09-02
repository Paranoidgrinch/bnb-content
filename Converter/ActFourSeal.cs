using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stages 9 and 10 — The Courts of the Royal Seal and The Processional Galleries. Three bodies that
// do nothing to the player directly and change everything about what the others do.
//
//   The Sun-Seal Bearer authorises. While its impression is intact — while it still holds Block — the first
//   original affliction its side lands each round carries one more stack, and the seal is pressed for it.
//   The False-Seal Forger counterfeits. After the first original affliction another body lands each round,
//   it adds exactly one more stack of THE SAME THING, and that stack is a forgery: it may be answered like
//   anything else, but it can never be the original a chain is measured from, nor feed another forgery.
//   The Kneeling Petitioners legitimise. The first affliction of the round makes every official act look
//   correct, and the whole procession braces.
//
// This is where the audit's §3.3 and §3.4 land, and where the last row of the plan's seam list was bought:
// an application can now say that it is a COPY (`ApplyStatusEffectRequest.Replicated`, carried as far as the
// applied/merged event), a rule can ask whether what it just heard was one (`eventIsReplicated`), and a rule
// can answer an application it did not make with an application of the same thing
// (`ApplyTriggerEventStatusNode`) — which no amount of content could express, because a program had no way
// to name a status it only learns at fire time.
public static partial class ActFour
{
    public const string SunSealEnemyId = "sun_seal_bearer";
    public const string ForgerEnemyId = "false_seal_forger";
    public const string PetitionersEnemyId = "kneeling_petitioners";

    public const string AuthorizedImpressionId = "authorized_impression";
    public const string CounterfeitAuthorizationId = "counterfeit_authorization";
    public const string ProcessionalApprovalId = "processional_approval";

    // What the seal costs to press, and what the procession's approval is worth to each body standing in it.
    private const int SealBlockSpent = 6;
    private const int ApprovalBlock = 7;

    // One of each a round, latched on the body that owns the rule.
    public static CounterId SealPressedThisRound => new("seal_pressed_this_round");
    public static CounterId ForgedThisRound => new("forged_this_round");
    public static CounterId ApprovedThisRound => new("approved_this_round");

    // ── the Sun-Seal Bearer ───────────────────────────────────────────────────────────────────────────────

    // "While the Bearer has Block: the first original negative status application by its side each round
    // gains +1 stack. Then consume part of the Bearer's Block."
    //
    // The extra stack arrives as a second application of the same status rather than by enlarging the first,
    // because the first has already landed by the time anything can answer it — and it is MARKED a copy, so
    // it cannot be the original the Forger standing beside it counterfeits (§3.3: a replicated application
    // never becomes the round's original).
    public static StatusData AuthorizedImpression() => new()
    {
        Id = AuthorizedImpressionId,
        NameKey = "Authorized Impression",
        DescriptionKey =
            "While this bearer still holds Block, the first affliction its side lands each round carries 1 "
            + "more stack — and pressing the seal costs it 6 Block.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(PressTheSeal<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(PressTheSeal<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(ClearLatch<RoundStartedTriggeredEffectContext>(AuthorizedImpressionId, SealPressedThisRound),
                nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<TContext> PressTheSeal<TContext>() where TContext : class
    {
        var bearer = Bearer(AuthorizedImpressionId);

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    OriginalAfflictionOnThePlayer<TContext>(),
                    new AndExpression<TContext>(
                        // …by its own side: the seal authorises this court's work, not the player's.
                        AppliedByThatSide<TContext>(bearer),
                        new AndExpression<TContext>(
                            NotYetThisRound<TContext>(bearer, SealPressedThisRound),
                            // The impression is only worth anything while it is intact.
                            new ComparisonExpression<TContext>(
                                new CombatantDefensivePoolExpression<TContext>(
                                    bearer, StandardCombatIds.BlockDefensivePool),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TContext>(SealBlockSpent))))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        bearer, SealPressedThisRound, new ConstantExpression<TContext>(1), relative: false),

                    new ApplyTriggerEventStatusNode<TContext>(
                        Applicant, new ConstantExpression<TContext>(1),
                        replicated: true, sourceSelector: bearer),

                    new ModifyDefensivePoolNode<TContext>(
                        bearer, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<TContext>(-SealBlockSpent)),
                ])));
    }

    // ── the False-Seal Forger ─────────────────────────────────────────────────────────────────────────────

    // "The first original negative status application by ANOTHER enemy each round: after resolving, apply +1
    // additional stack of that same status. That added stack is Replicated. It cannot trigger another
    // replication chain."
    //
    // Which is the whole of §3.4 in three questions: was it an affliction on the player, was it somebody
    // else's, and was it an original — a forgery of a forgery is exactly what the mark exists to prevent.
    public static StatusData CounterfeitAuthorization() => new()
    {
        Id = CounterfeitAuthorizationId,
        NameKey = "Counterfeit Authorization",
        DescriptionKey =
            "The first affliction another body lands on you each round is followed by 1 more stack of the "
            + "same thing — forged, and never itself worth forging.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(ForgeTheSeal<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(ForgeTheSeal<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(ClearLatch<RoundStartedTriggeredEffectContext>(CounterfeitAuthorizationId, ForgedThisRound),
                nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<TContext> ForgeTheSeal<TContext>() where TContext : class
    {
        var forger = Bearer(CounterfeitAuthorizationId);

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    OriginalAfflictionOnThePlayer<TContext>(),
                    new AndExpression<TContext>(
                        // …by ANOTHER body. A forger that fed on its own Doubt would need no forgery at all.
                        new NotExpression<TContext>(
                            new TargetHasStatusExpression<TContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId(CounterfeitAuthorizationId))),
                        NotYetThisRound<TContext>(forger, ForgedThisRound))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        forger, ForgedThisRound, new ConstantExpression<TContext>(1), relative: false),

                    new ApplyTriggerEventStatusNode<TContext>(
                        Applicant, new ConstantExpression<TContext>(1),
                        replicated: true, sourceSelector: forger),
                ])));
    }

    // ── the Kneeling Petitioners ──────────────────────────────────────────────────────────────────────────

    // "The first time each round another enemy successfully applies a negative status to the player: all
    // living enemies gain Block. A Replicated status CAN trigger this."
    //
    // So this rule deliberately does NOT ask whether what it heard was a forgery. Submission does not check
    // paperwork; it only has to look official.
    public static StatusData ProcessionalApproval() => new()
    {
        Id = ProcessionalApprovalId,
        NameKey = "Processional Approval",
        DescriptionKey =
            "The first affliction another body lands on you each round makes the whole procession look "
            + "correct: every enemy still standing gains 7 Block. A forged one counts too.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(Approve<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(Approve<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(ClearLatch<RoundStartedTriggeredEffectContext>(ProcessionalApprovalId, ApprovedThisRound),
                nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<TContext> Approve<TContext>() where TContext : class
    {
        var petitioners = Bearer(ProcessionalApprovalId);

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    // An affliction on the player, forged or not — the procession does not check.
                    new AndExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff)),
                    new AndExpression<TContext>(
                        new NotExpression<TContext>(
                            new TargetHasStatusExpression<TContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId(ProcessionalApprovalId))),
                        NotYetThisRound<TContext>(petitioners, ApprovedThisRound))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        petitioners, ApprovedThisRound, new ConstantExpression<TContext>(1), relative: false),

                    new ForEachTargetEffectNode<TContext>(
                        CombatantTargetSelectors.WithStatus(
                            CombatantTargetSelectors.AllAliveCombatants,
                            new StatusDefinitionId(LabyrinthBodyId)),
                        new GainBlockNode<TContext>(
                            CombatantTargetSelectors.IterationTarget,
                            new ConstantExpression<TContext>(ApprovalBlock))),
                ])));
    }

    // ── the questions all three ask ───────────────────────────────────────────────────────────────────────

    // An ORIGINAL affliction landing on the player: a debuff, on the applicant, that is not itself a copy.
    // In a status-application trigger "source" is whoever applied it and "eventTarget" is who received it.
    private static ICombatExpression<TContext, bool> OriginalAfflictionOnThePlayer<TContext>()
        where TContext : class =>
        new AndExpression<TContext>(
            new AndExpression<TContext>(
                new TargetHasStatusExpression<TContext>(
                    CombatantTargetSelectors.EventTarget, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff)),
            new NotExpression<TContext>(new TriggerEventIsReplicatedExpression<TContext>()));

    // "…and it was this court's work": the applier is a Licensing Labyrinth body, which the player never is.
    private static ICombatExpression<TContext, bool> AppliedByThatSide<TContext>(
        ICombatantTargetSelector bearer) where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(LabyrinthBodyId));

    private static ICombatExpression<TContext, bool> NotYetThisRound<TContext>(
        ICombatantTargetSelector body, CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(body, latch),
            ComparisonOperator.Equal,
            new ConstantExpression<TContext>(0));

    // A round turning is nobody's own event, so a latch is cleared for every body that carries the rule.
    private static EffectProgram<TContext> ClearLatch<TContext>(string ruleId, CounterId latch)
        where TContext : class =>
        new(new ForEachTargetEffectNode<TContext>(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)),
            new SetCombatantCounterNode<TContext>(
                CombatantTargetSelectors.IterationTarget, latch,
                new ConstantExpression<TContext>(0), relative: false)));
}
