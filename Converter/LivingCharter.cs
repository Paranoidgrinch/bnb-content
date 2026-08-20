using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's fifth boss: "Combat rules are law. Law can be reviewed, amended and interpreted."
//
// The Charter publishes Articles — statuses on itself that rewrite how the fight works for BOTH sides. Every
// second action it calls a Judicial Review and hands the player the choice to uphold the standing Article or
// strike it down for the next one. Two Reviews (or 67 HP) bring the Emergency Amendment and a second
// simultaneous Article; at 34 HP the Constitutional Crisis hands each side an Exception token per round.
//
// The Articles are authored as encounter triggers gated on the Charter still carrying the Article, so a struck
// Article stops working the moment it is removed. Deviations: ADAPTATIONS.md.
public static class LivingCharter
{
    public const string CharterId = "the_living_charter";
    public const string ContinuanceId = "article_of_continuance";
    public const string RedressId = "article_of_redress";
    public const string MutualSecurityId = "article_of_mutual_security";
    public const string ReciprocalBurdenId = "article_of_reciprocal_burden";

    public const string ReviewPendingId = "judicial_review_called";   // on the player: the choice is open
    public const string AmendmentPendingId = "emergency_amendment";   // telegraph, on the Charter
    public const string ContradictoryId = "contradictory_constitution"; // Phase II, on the Charter
    public const string CrisisId = "constitutional_crisis";           // final state, on the Charter
    public const string RemedyId = "remedy_due";                      // Redress, per side
    public const string SecurityUsedId = "security_invoked";          // Mutual Security latch, per side
    public const string BurdenUsedId = "burden_borne";                // Reciprocal Burden latch, per side
    public const string ExceptionId = "exception";                    // Crisis token, per side
    public const string ExceptionClaimedId = "exception_claimed";     // spent by the player for this turn

    public const string UpholdCardId = "uphold_the_article";
    public const string StrikeDownCardId = "strike_down_the_article";
    public const string ExceptionCardId = "claim_an_exception";

    public static readonly CounterId RetainedBlockCounter = new("retained_block");
    public static readonly CounterId RedressDamageCounter = new("redress_damage");
    public static readonly CounterId ActionsSinceReviewCounter = new("actions_since_review");
    public static readonly CounterId ReviewsCounter = new("reviews_completed");
    public static readonly CounterId ArticleIndexCounter = new("article_index");
    public static readonly CounterId CharterBeatCounter = new("charter_beat");

    public const int RedressThreshold = 14;
    public const int PhaseOneActionsPerReview = 2;
    public const int PhaseTwoActionsPerReview = 3;
    public const int ReviewsForAmendment = 2;
    public const int AmendmentHealth = 67;
    public const int CrisisHealth = 34;
    public const int CharterBeats = 5;

    // The three Articles this fight publishes, in the order the Charter reaches for them.
    public static readonly string[] Articles = [ContinuanceId, RedressId, MutualSecurityId];

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheCharter(),
        PassiveStatuses.NamedMarker(ContinuanceId, "Article of Continuance",
            "Each side keeps half of its Block when its turn comes round again."),
        PassiveStatuses.NamedMarker(RedressId, "Article of Redress",
            "A side that loses 14 HP in a turn is owed 8 Block at the start of its next one."),
        PassiveStatuses.NamedMarker(MutualSecurityId, "Article of Mutual Security",
            "The first Block a side gains each turn gives the other side 4 Block."),
        PassiveStatuses.NamedMarker(ReciprocalBurdenId, "Article of Reciprocal Burden",
            "The first status a side lands on its opponent each turn costs the applier 1 Doubt."),
        PassiveStatuses.NamedMarker(ReviewPendingId, "Judicial Review",
            "Uphold the Article or strike it down before your turn ends."),
        PassiveStatuses.NamedMarker(AmendmentPendingId, "Emergency Amendment",
            "The Charter's next action amends itself."),
        PassiveStatuses.NamedMarker(ContradictoryId, "Contradictory Constitution", "Phase II."),
        PassiveStatuses.NamedMarker(CrisisId, "Constitutional Crisis",
            "Each round both sides receive an Exception."),
        PassiveStatuses.NamedMarker(RemedyId, "Remedy Due", "8 Block at the start of your next turn."),
        PassiveStatuses.NamedMarker(SecurityUsedId, "Security Invoked", null),
        PassiveStatuses.NamedMarker(BurdenUsedId, "Burden Borne", null),
        PassiveStatuses.NamedMarker(ExceptionId, "Exception", "May be claimed to ignore an Article this turn."),
        PassiveStatuses.NamedMarker(ExceptionClaimedId, "Exception Claimed",
            "The Articles do not touch you this turn."),
    ];

    public static IEnumerable<CardData> Cards() => [Uphold(), StrikeDown(), ClaimException()];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheLawOpensThePlayersTurn(),
        TheLawAfterTheDraw(),
        TheLawClosesThePlayersTurn(),
        RedressIsRecorded(),
        MutualSecurityIsInvoked(),
        ReciprocalBurdenIsBorne(),
        ExceptionsAreIssued(),
    ];

    // ── The Articles, at the player's turn start ──────────────────────────────

    // Everything the law owes the CHARTER is paid here: the player's turn begins after the Charter's own turn
    // start has already cleared its Block, so this is where retained Block and Remedy can actually land on it.
    // The player's own dues wait for the draw (see below), because the player's Block is cleared after this.
    private static EncounterTriggerData TheLawOpensThePlayersTurn()
    {
        var player = CombatantTargetSelectors.Source;
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));

        IEffectNode<TurnStartedTriggeredEffectContext> ForCharter(IEffectNode<TurnStartedTriggeredEffectContext> body) =>
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(charter, body);

        ICombatExpression<TurnStartedTriggeredEffectContext, bool> CharterHas(string statusId) =>
            new IterationTargetHasStatusExpression<TurnStartedTriggeredEffectContext>(new StatusDefinitionId(statusId));

        var iterated = CombatantTargetSelectors.IterationTarget;

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    ForCharter(new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                    {
                        // Continuance: the half the Charter banked when its own turn began.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            CharterHas(ContinuanceId),
                            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                iterated,
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    iterated, RetainedBlockCounter))),
                        // Redress: the Charter's own remedy.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new AndExpression<TurnStartedTriggeredEffectContext>(
                                CharterHas(RedressId), CharterHas(RemedyId)),
                            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                            {
                                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                    iterated, new StatusDefinitionId(RemedyId)),
                                new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                    iterated, new ConstantExpression<TurnStartedTriggeredEffectContext>(8)),
                            })),
                        // The per-turn latches of the Articles open again for both sides.
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            iterated, new StatusDefinitionId(SecurityUsedId)),
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            iterated, new StatusDefinitionId(BurdenUsedId)),
                        // The Judicial Review the Charter called is answered on this turn.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                    player, new StatusDefinitionId(ReviewPendingId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                            {
                                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                    player, new CardDefinitionId(UpholdCardId), CardZone.Hand,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                    player, new CardDefinitionId(StrikeDownCardId), CardZone.Hand,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            })),
                        // In the Crisis, an unspent Exception can be claimed.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                    player, new StatusDefinitionId(ExceptionId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                player, new CardDefinitionId(ExceptionCardId), CardZone.Hand,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                    })),
                    // Continuance for the PLAYER: bank half of what is still standing, before it is cleared.
                    ForCharter(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        CharterHas(ContinuanceId),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            player, RetainedBlockCounter,
                            new DivideExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                                    player, StandardCombatIds.BlockDefensivePool),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                            relative: false))),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(SecurityUsedId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(BurdenUsedId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(ExceptionClaimedId)),
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // What the law owes the PLAYER lands after the draw: the turn-start Block clear resolves before this, so
    // Block granted here is Block the player actually keeps.
    private static EncounterTriggerData TheLawAfterTheDraw()
    {
        var player = CombatantTargetSelectors.Source;
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));

        ICombatExpression<CardsDrawnTriggeredEffectContext, bool> CharterHas(string statusId) =>
            new IterationTargetHasStatusExpression<CardsDrawnTriggeredEffectContext>(new StatusDefinitionId(statusId));

        ICombatExpression<CardsDrawnTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(charter,
                    new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
                    {
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            CharterHas(ContinuanceId),
                            new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
                            {
                                new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                    player,
                                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                        player, RetainedBlockCounter)),
                                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                                    player, RetainedBlockCounter,
                                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                            })),
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            new AndExpression<CardsDrawnTriggeredEffectContext>(
                                CharterHas(RedressId),
                                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                    Stacks(RemedyId), ComparisonOperator.Greater,
                                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                            new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
                            {
                                new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                                    player, new StatusDefinitionId(RemedyId)),
                                new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                    player, new ConstantExpression<CardsDrawnTriggeredEffectContext>(8)),
                            })),
                    }))));

        return new EncounterTriggerData("CardsDrawn",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()));
    }

    // At the player's turn end the Redress tallies start over, and an unanswered Review lapses into an uphold.
    private static EncounterTriggerData TheLawClosesThePlayersTurn()
    {
        var player = CombatantTargetSelectors.Source;
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        player, RedressDamageCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        charter, RedressDamageCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(ReviewPendingId)),
                })));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // Article of Redress: a side that loses 14 HP inside one turn is owed a remedy next turn.
    private static EncounterTriggerData RedressIsRecorded()
    {
        var hurt = CombatantTargetSelectors.EventTarget;
        var charter = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CharterId));

        ICombatExpression<DamageReceivedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                hurt, new StatusDefinitionId(statusId));

        var tally = new AddExpression<DamageReceivedTriggeredEffectContext>(
            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(hurt, RedressDamageCounter),
            new EventAmountExpression<DamageReceivedTriggeredEffectContext>());

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(charter,
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new IterationTargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                        new StatusDefinitionId(RedressId)),
                    new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                    {
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            hurt, RedressDamageCounter,
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                        // The tally above is enqueued, so this blow has to count itself.
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            new AndExpression<DamageReceivedTriggeredEffectContext>(
                                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                    tally, ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(RedressThreshold)),
                                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                    Stacks(RemedyId), ComparisonOperator.Equal,
                                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                                hurt, new StatusDefinitionId(RemedyId),
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1))),
                    }))));

        return new EncounterTriggerData("DamageTaken",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>()));
    }

    // Article of Mutual Security: the first Block a side raises each turn arms the other side too. The gift
    // itself sets the recipient's latch, so security never answers security.
    private static EncounterTriggerData MutualSecurityIsInvoked()
    {
        var gainer = CombatantTargetSelectors.EventTarget;
        var charter = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CharterId));
        var applicant = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId));

        ICombatExpression<BlockGainedTriggeredEffectContext, int> Stacks(
            ICombatantTargetSelector who, string statusId) =>
            new CombatantStatusStacksExpression<BlockGainedTriggeredEffectContext>(
                who, new StatusDefinitionId(statusId));

        IEffectNode<BlockGainedTriggeredEffectContext> Answer(ICombatantTargetSelector other) =>
            new SequenceEffectNode<BlockGainedTriggeredEffectContext>(new IEffectNode<BlockGainedTriggeredEffectContext>[]
            {
                new ApplyStatusNode<BlockGainedTriggeredEffectContext>(
                    gainer, new StatusDefinitionId(SecurityUsedId),
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(1)),
                new ApplyStatusNode<BlockGainedTriggeredEffectContext>(
                    other, new StatusDefinitionId(SecurityUsedId),
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(1)),
                new GainBlockNode<BlockGainedTriggeredEffectContext>(
                    other, new ConstantExpression<BlockGainedTriggeredEffectContext>(4)),
            });

        var program = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ForEachTargetEffectNode<BlockGainedTriggeredEffectContext>(charter,
                new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
                    new AndExpression<BlockGainedTriggeredEffectContext>(
                        new IterationTargetHasStatusExpression<BlockGainedTriggeredEffectContext>(
                            new StatusDefinitionId(MutualSecurityId)),
                        new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                            Stacks(gainer, SecurityUsedId), ComparisonOperator.Equal,
                            new ConstantExpression<BlockGainedTriggeredEffectContext>(0))),
                    new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
                        // Who gained? The player's guard arms the Charter, and the Charter's arms the player —
                        // unless the Charter spends its Exception to refuse the disadvantage.
                        new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                            Stacks(gainer, PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                            new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                        Answer(charter),
                        @else: new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
                            new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                                Stacks(gainer, CharterId), ComparisonOperator.Greater,
                                new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                            new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
                                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                                    Stacks(gainer, ExceptionId), ComparisonOperator.Greater,
                                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                                new SequenceEffectNode<BlockGainedTriggeredEffectContext>(new IEffectNode<BlockGainedTriggeredEffectContext>[]
                                {
                                    new RemoveStatusNode<BlockGainedTriggeredEffectContext>(
                                        gainer, new StatusDefinitionId(ExceptionId)),
                                    new ApplyStatusNode<BlockGainedTriggeredEffectContext>(
                                        gainer, new StatusDefinitionId(SecurityUsedId),
                                        new ConstantExpression<BlockGainedTriggeredEffectContext>(1)),
                                }),
                                @else: Answer(applicant)))))));

        return new EncounterTriggerData("BlockGained",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>()));
    }

    // Article of Reciprocal Burden: the first status a side lands on its opponent each turn is answered with a
    // Doubt on the applier — the Charter included. A player holding a claimed Exception is spared.
    private static EncounterTriggerData ReciprocalBurdenIsBorne()
    {
        var applier = CombatantTargetSelectors.Source;
        var recipient = CombatantTargetSelectors.EventTarget;
        var charter = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CharterId));

        ICombatExpression<StatusAppliedTriggeredEffectContext, int> Stacks(
            ICombatantTargetSelector who, string statusId) =>
            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                who, new StatusDefinitionId(statusId));

        // Only cross-side applications count: a side dressing itself is not a burden on anybody.
        var crossSide = new OrExpression<StatusAppliedTriggeredEffectContext>(
            new AndExpression<StatusAppliedTriggeredEffectContext>(
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    Stacks(applier, PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    Stacks(recipient, CharterId), ComparisonOperator.Greater,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(0))),
            new AndExpression<StatusAppliedTriggeredEffectContext>(
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    Stacks(applier, CharterId), ComparisonOperator.Greater,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    Stacks(recipient, PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(0))));

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ForEachTargetEffectNode<StatusAppliedTriggeredEffectContext>(charter,
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        new IterationTargetHasStatusExpression<StatusAppliedTriggeredEffectContext>(
                            new StatusDefinitionId(ReciprocalBurdenId)),
                        new AndExpression<StatusAppliedTriggeredEffectContext>(
                            crossSide,
                            new AndExpression<StatusAppliedTriggeredEffectContext>(
                                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                                    Stacks(applier, BurdenUsedId), ComparisonOperator.Equal,
                                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                                    Stacks(applier, ExceptionClaimedId), ComparisonOperator.Equal,
                                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(0))))),
                    new SequenceEffectNode<StatusAppliedTriggeredEffectContext>(new IEffectNode<StatusAppliedTriggeredEffectContext>[]
                    {
                        new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                            applier, new StatusDefinitionId(BurdenUsedId),
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                        new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                            applier, new StatusDefinitionId("doubt"),
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    }))));

        return new EncounterTriggerData("StatusApplied",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()));
    }

    // Constitutional Crisis: one Exception each per round, never more than one held.
    private static EncounterTriggerData ExceptionsAreIssued()
    {
        var charter = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CharterId));
        var applicant = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId));

        IEffectNode<RoundStartedTriggeredEffectContext> Issue(ICombatantTargetSelector who) =>
            new ForEachTargetEffectNode<RoundStartedTriggeredEffectContext>(who,
                new ConditionalEffectNode<RoundStartedTriggeredEffectContext>(
                    new NotExpression<RoundStartedTriggeredEffectContext>(
                        new IterationTargetHasStatusExpression<RoundStartedTriggeredEffectContext>(
                            new StatusDefinitionId(ExceptionId))),
                    new ApplyStatusNode<RoundStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(ExceptionId),
                        new ConstantExpression<RoundStartedTriggeredEffectContext>(1))));

        var program = new EffectProgram<RoundStartedTriggeredEffectContext>(
            new ForEachTargetEffectNode<RoundStartedTriggeredEffectContext>(charter,
                new ConditionalEffectNode<RoundStartedTriggeredEffectContext>(
                    new IterationTargetHasStatusExpression<RoundStartedTriggeredEffectContext>(
                        new StatusDefinitionId(CrisisId)),
                    new SequenceEffectNode<RoundStartedTriggeredEffectContext>(new IEffectNode<RoundStartedTriggeredEffectContext>[]
                    {
                        Issue(charter),
                        Issue(applicant),
                    }))));

        return new EncounterTriggerData("RoundStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<RoundStartedTriggeredEffectContext>()));
    }

    // ── The Charter's own machinery ───────────────────────────────────────────

    private static StatusData TheCharter()
    {
        var self = CombatantTargetSelectors.Source;

        ICombatExpression<TurnStartedTriggeredEffectContext, int> StartStacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        // Continuance banks half of what is standing before the turn's clear takes it.
        var onTurnStart = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    StartStacks(ContinuanceId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    self, RetainedBlockCounter,
                    new DivideExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                            self, StandardCombatIds.BlockDefensivePool),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                    relative: false),
                @else: new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    self, RetainedBlockCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Counter(CounterId counter) =>
            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, counter);

        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                // Every ordinary action counts toward the next Judicial Review.
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, ActionsSinceReviewCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),

                // Two Reviews (or 67 HP) call the Emergency Amendment; 34 HP calls the Crisis.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(ContradictoryId), ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                Stacks(AmendmentPendingId), ComparisonOperator.Equal,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                            new OrExpression<TurnEndedTriggeredEffectContext>(
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    Counter(ReviewsCounter), ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(ReviewsForAmendment)),
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(self),
                                    ComparisonOperator.LessOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(AmendmentHealth))))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(AmendmentPendingId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),

                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(CrisisId), ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(self),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(CrisisHealth))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(CrisisId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),

                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, CharterBeatCounter,
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            Counter(CharterBeatCounter), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(CharterBeats)),
                    relative: false),
            }));

        return new StatusData
        {
            Id = CharterId,
            NameKey = "The Living Charter",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    onTurnStart, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    onTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // ── The player's cards ────────────────────────────────────────────────────

    private static CardData Uphold() => Review(UpholdCardId, "Uphold the Article",
        "The Article stands. Gain 6 Block.",
        player =>
        [
            new GainBlockNode<CardPlayContext>(player, new ConstantExpression<CardPlayContext>(6)),
        ]);

    // Striking an Article down exchanges it for the next one in the Charter's own order — the law is replaced,
    // never simply deleted.
    private static CardData StrikeDown()
    {
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));
        var iterated = CombatantTargetSelectors.IterationTarget;

        IEffectNode<CardPlayContext> Publish(int index) =>
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantCounterExpression<CardPlayContext>(iterated, ArticleIndexCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayContext>(index)),
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    // The standing Article goes; the next one is published in its place.
                    new RemoveStatusNode<CardPlayContext>(
                        iterated, new StatusDefinitionId(Articles[index])),
                    new ApplyStatusNode<CardPlayContext>(
                        iterated, new StatusDefinitionId(Articles[(index + 1) % Articles.Length]),
                        new ConstantExpression<CardPlayContext>(1)),
                    new SetCombatantCounterNode<CardPlayContext>(
                        iterated, ArticleIndexCounter,
                        new ConstantExpression<CardPlayContext>((index + 1) % Articles.Length), relative: false),
                }));

        return Review(StrikeDownCardId, "Strike Down the Article",
            "The Article is struck; the next prepared Article takes its place. The Charter gains 8 Block.",
            _ =>
            [
                new ForEachTargetEffectNode<CardPlayContext>(charter,
                    new SequenceEffectNode<CardPlayContext>(
                    [
                        .. Enumerable.Range(0, Articles.Length).Select(Publish),
                        new GainBlockNode<CardPlayContext>(iterated, new ConstantExpression<CardPlayContext>(8)),
                    ])),
            ]);
    }

    // Both answers are one-turn offers, and answering closes the Review.
    private static CardData Review(
        string id, string name, string text, Func<ICombatantTargetSelector, IEffectNode<CardPlayContext>[]> effects)
    {
        var player = CombatantTargetSelectors.Source;

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId("form"), new TagId("charter")],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusStacksExpression<CardPlayContext>(
                            player, new StatusDefinitionId(ReviewPendingId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayContext>(0)),
                    new SequenceEffectNode<CardPlayContext>(
                    [
                        new RemoveStatusNode<CardPlayContext>(player, new StatusDefinitionId(ReviewPendingId)),
                        .. effects(player),
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }

    // The Crisis Exception: for this turn, the Articles leave the player alone.
    private static CardData ClaimException() => new()
    {
        Id = ExceptionCardId,
        NameKey = "Claim an Exception",
        DescriptionKey = "The Articles do not touch you for the rest of this turn.",
        Costs = [],
        Tags = [new TagId("form"), new TagId("charter")],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ExceptionId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0)),
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    new RemoveStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ExceptionId)),
                    new ApplyStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ExceptionClaimedId),
                        new ConstantExpression<CardPlayContext>(1)),
                }))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // ── Raw intents ───────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "judicial_review" => JudicialReview(),
        "emergency_amendment" => EmergencyAmendment(),
        _ => null,
    };

    // The Charter calls its own law into question: 6 damage, and the player answers on their next turn.
    private static EffectProgram<EnemyActionContext> JudicialReview()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(6)),
                new ApplyStatusNode<EnemyActionContext>(
                    player, new StatusDefinitionId(ReviewPendingId), new ConstantExpression<EnemyActionContext>(1)),
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, ActionsSinceReviewCounter,
                    new ConstantExpression<EnemyActionContext>(0), relative: false),
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, ReviewsCounter, new ConstantExpression<EnemyActionContext>(1), relative: true),
            }));
    }

    // The amendment: 8 damage, 1 Paperwork, 8 Block — and a second Article published beside the first.
    private static EffectProgram<EnemyActionContext> EmergencyAmendment()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        IEffectNode<EnemyActionContext> Publish(int index) =>
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCounterExpression<EnemyActionContext>(self, ArticleIndexCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<EnemyActionContext>(index)),
                new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
                {
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(Articles[(index + 1) % Articles.Length]),
                        new ConstantExpression<EnemyActionContext>(1)),
                    new SetCombatantCounterNode<EnemyActionContext>(
                        self, ArticleIndexCounter,
                        new ConstantExpression<EnemyActionContext>((index + 1) % Articles.Length), relative: false),
                }));

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(
            [
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(AmendmentPendingId)),
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(8)),
                new ApplyStatusNode<EnemyActionContext>(
                    player, new StatusDefinitionId("paperwork"), new ConstantExpression<EnemyActionContext>(1)),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(8)),
                .. Enumerable.Range(0, Articles.Length).Select(Publish),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(ContradictoryId), new ConstantExpression<EnemyActionContext>(1)),
            ]));
    }
}
