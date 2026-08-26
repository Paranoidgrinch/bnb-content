using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// What Act I's events promise for AFTER the next fight.
//
// Most of an event's vocabulary is a rule of the fight — a status the fight opens with. These are the ones that
// are not: a fee taken out of the purse once the fight is over, a card that comes back upgraded, a purse that
// never arrives, a claim paid at a rate the fight decided. None of them belongs to a relic, so each is an
// authored RUN PROGRAM: the body lives once in the document (RunBlueprint.Programs) and an event installs it by
// name (fx.installProgramById). The instance id IS the name, so a promise made twice is still one promise, and
// a one-shot body names itself to step down once it has been kept.
public static class ActOneEventPrograms
{
    public const string MarkingsExpire = "act_one_markings_expire";
    public const string UnderReviewReturns = "act_one_under_review_returns";
    public const string CertifiedOriginal = "act_one_certified_original";
    public const string AuditNotice = "act_one_audit_notice";
    public const string GarnishedReward = "act_one_garnished_reward";
    public const string GarnishThePurse = "act_one_garnish_the_purse";
    public const string ExtraCardReward = "act_one_extra_card_reward";
    public const string ReceiptOfPriorEffort = "act_one_receipt_of_prior_effort";
    public const string WrongFormAgain = "act_one_wrong_form_again";

    public static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition> All(ConversionPools pools) =>
        new Dictionary<string, ITriggeredRunEffectDefinition>
        {
            [MarkingsExpire] = MarkingsExpireBody(),
            [UnderReviewReturns] = UnderReviewReturnsBody(),
            [CertifiedOriginal] = CertifiedOriginalBody(),
            [AuditNotice] = AuditNoticeBody(),
            [GarnishedReward] = GarnishedRewardBody(),
            [GarnishThePurse] = GarnishThePurseBody(),
            [ExtraCardReward] = ExtraCardRewardBody(pools),
            [ReceiptOfPriorEffort] = ReceiptOfPriorEffortBody(),
            [WrongFormAgain] = WrongFormAgainBody(),
        };

    // "Next combat" means exactly one combat. A marking is written between fights and read by the next one, so
    // the fight that read it is where it stops being true — otherwise every later fight would honour it too.
    private static ITriggeredRunEffectDefinition MarkingsExpireBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            .. ActOneEventObjects.SpentAfterOneFight().Select(Untag),
            Done(MarkingsExpire),
        ]);

    // Under Review is the marking that outlives its fight: the card was held back, and what comes back is
    // better than what was handed in. Upgrade first, then let the marking go — in that order, or the upgrade
    // finds nothing left to look for.
    private static ITriggeredRunEffectDefinition UnderReviewReturnsBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(new UpgradeCardsRunEffect(Tagged(ActOneEventObjects.UnderReview))),
            Untag(ActOneEventObjects.UnderReview),
            Done(UnderReviewReturns),
        ]);

    // The permanent marking's rule, and the only program here that never steps down: a card certified once is
    // certified for the rest of the run, so every later fight has to open knowing how to read the stamp.
    private static ITriggeredRunEffectDefinition CertifiedOriginalBody() =>
        Openings.EveryCombat(new CombatNodeModel("applyStatus", "source",
            CombatAmountSpec.FromConst(1), StatusId: ActOneEventObjects.CertifiedOriginalRuleId));

    // "After combat lose 4 Gold per HP lost, max 80."
    private static ITriggeredRunEffectDefinition AuditNoticeBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Negate(RunExpr.Min(
                    RunExpr.Multiply(RunEventValues.CombatDamageTaken, RunExpr.Const(4)), RunExpr.Const(80)))),
            Done(AuditNotice),
        ]);

    // "Combat grants no Gold." The purse is not the fight's to withhold — the map pays it out, and it is
    // enqueued AFTER the resolved event every program hears. So the garnishment is served in two beats: the
    // fight ending arms a bailiff, and the bailiff takes the very next Gold that arrives, which is that purse:
    // every generated fight pays one, and nothing else can change the purse in between.
    private static ITriggeredRunEffectDefinition GarnishedRewardBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(GarnishThePurse))),
            Done(GarnishedReward),
        ]);

    private static ITriggeredRunEffectDefinition GarnishThePurseBody() =>
        RunPrograms.When<ResourceChangedRunEvent>(
            RunExpr.And(
                RunEventValues.ResourceIs(StandardRunIds.Gold),
                RunExpr.GreaterThan(RunEventValues.ResourceDelta, RunExpr.Const(0))),
            [
                RunEffectTemplates.GainResource(
                    StandardRunIds.Gold, RunExpr.Negate(RunEventValues.ResourceDelta)),
                Done(GarnishThePurse),
            ]);

    // "Next combat grants one additional normal card reward" — the same three-card offer the fight itself pays,
    // a second time, out of the act's own pool.
    private static ITriggeredRunEffectDefinition ExtraCardRewardBody(ConversionPools pools) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            Literal(new OfferRewardRunEffect(
                new RewardId("event:sealed-back-door"), pools.CardRewardSource(), 1)),
            Done(ExtraCardReward),
        ]);

    // "125 Gold if won by end of round 3, otherwise 25." The fight wrote down how long it took
    // (ReceiptOfPriorEffortRule); this reads it off the result. There is no if-expression for a number, so the
    // lateness is arithmetic: min(1, max(0, rounds − 3)) is 1 exactly when the third round has passed.
    private static ITriggeredRunEffectDefinition ReceiptOfPriorEffortBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Subtract(RunExpr.Const(125), RunExpr.Multiply(RunExpr.Const(100),
                    RunExpr.Min(RunExpr.Const(1), RunExpr.Max(RunExpr.Const(0),
                        RunExpr.Subtract(
                            RunEventValues.CombatCounter(ActOneEventObjects.RoundsTaken.ToString()),
                            RunExpr.Const(3))))))),
            Done(ReceiptOfPriorEffort),
        ]);

    // "Shuffle Wrong Form into each of the next 2 combats." A combat opening is consumed by one fight, so the
    // second copy is queued again the moment the first fight is over.
    private static ITriggeredRunEffectDefinition WrongFormAgainBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(Openings.NextCombat(
                AddCard(ActOneEventObjects.WrongForm.Id, CardZone.DrawPile))),
            Done(WrongFormAgain),
        ]);

    // ── shorthands ────────────────────────────────────────────────────────────────────────────────────────

    private static IRunSelector<RunCardInstance> Tagged(string marking) =>
        RunSelectors.DeckCards.WithTag(new RunCardTagId(marking));

    private static IRunEffectTemplate Untag(string marking) =>
        Literal(new TagCardsRunEffect(Tagged(marking), new RunCardTagId(marking), false));

    private static IRunEffectTemplate Literal(IRunEffectRequest effect) => RunEffectTemplates.Literal(effect);

    // The last thing a one-shot promise does: name itself, and stop being pending.
    private static IRunEffectTemplate Done(string program) =>
        Literal(new UninstallRunProgramRunEffect(new RunProgramId(program)));
}
