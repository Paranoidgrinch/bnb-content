using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 4 — **The Answering Hill** (374 HP).
//
// > The landscape stores unresolved Claims beneath its surface, and the player must decide when it is safe
// > to cross each HP threshold.
//
// At first there is no enemy. The road simply rises. Then boundary stones turn, roots pull free, and entire
// slopes shift, and the health bar reads THE HILL.
//
// The Hill never holds standing. Every Claim it would be granted goes straight into the ground as a
// **Buried Claim** — local memory, not standing, and nothing can transfer or spend it. What buried standing
// does is wait: at 251 HP and again at 123 the slope stirs, gives the player one full turn to answer, and
// then cashes out everything under it at once.
//
// So the whole fight is a question of timing. The player chooses when to cross each threshold, and the one
// thing that empties the ground between now and then is restitution: settling with the Hill in full takes
// one Buried Claim back out.
public static partial class ActThree
{
    public const string HillEnemyId = "the_answering_hill";
    public const string HillId = "the_landscape_has_standing";

    public const string BuriedClaimId = "buried_claim";
    public const string SurveyedFaceId = "the_surveyed_face";
    public const string CrownOfTheHillId = "the_crown_of_the_hill";
    public const string SlopeStirsPendingId = "the_slope_stirs";
    public const string SlopeAnswersPendingId = "the_slope_answers";
    public const string CrownStirsPendingId = "the_crown_stirs";
    public const string CrownBreaksPendingId = "the_crown_breaks_open";
    public const string HillDoubledId = "rezoned";
    public const string HillNotedId = "hill_noted";

    public const int FootpathLaw = 34;
    public const int MeasuredSlopeLaw = 35;

    private const int MaxBuriedClaims = 5;
    private const int FirstThresholdHealth = 251;
    private const int SecondThresholdHealth = 123;
    private const int HillSignatureHealth = 60;

    // Which law the crown is applying this turn: 0 the footpath, 1 the measure.
    public static CounterId CrownedLawCounter => new("crowned_law");

    private static ICombatantTargetSelector Hill { get; } = Elite(HillId);

    private static IEnumerable<StatusData> HillStatuses() =>
    [
        TheLandscapeHasStanding(),
        BuriedClaim(),
        Marker(SurveyedFaceId, "The Surveyed Face",
            "The slope has been measured. Two cards in a row of one price are the footing it objects to."),
        Marker(CrownOfTheHillId, "The Crown of the Hill",
            "Both of the hill's laws apply now, one each turn, and refusing either hardens the ground."),
        Marker(SlopeStirsPendingId, "The Slope Stirs",
            "The hill's next action is not a blow: it names a small price and shows you what is under it."),
        Marker(SlopeAnswersPendingId, "The Slope Answers",
            "Everything buried is answered at the hill's next action."),
        Marker(CrownStirsPendingId, "The Crown Stirs",
            "The hill's next action is not a blow. What is buried will be answered after it."),
        Marker(CrownBreaksPendingId, "The Crown Breaks Open",
            "Everything buried comes up through the road at the hill's next action."),
        Marker(HillDoubledId, "Rezoned",
            "The hill's next Trespass is 2 rather than 1. One licence still refuses the whole of it."),
        Marker(HillNotedId, "Footing Noted",
            "The hill has already answered one piece of footing this turn."),
    ];

    private static StatusData BuriedClaim() => new()
    {
        Id = BuriedClaimId,
        NameKey = "Buried Claim",
        DescriptionKey =
            "Standing the hill has taken into the ground. It is not a Claim: nothing transfers it and "
            + "nothing spends it. It waits until the slope stirs. At most 5.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, int> Buried<TContext>()
        where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(Hill, new StatusDefinitionId(BuriedClaimId));

    // One Trespass, or two off a rezoning. One application either way, so one licence refuses it whole.
    private static ICombatExpression<TContext, int> HillTrespass<TContext>()
        where TContext : class =>
        new AddExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantStatusStacksExpression<TContext>(Hill, new StatusDefinitionId(HillDoubledId))));

    // ── the hill itself ───────────────────────────────────────────────────────────────────────────────────
    private static StatusData TheLandscapeHasStanding()
    {
        var player = CombatantTargetSelectors.Source;
        var memory = CostMemory("the_hill");

        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        // Keep to the Footpath — the fourth real card. The Slope Has Been Measured — two in a row of one
        // price. Which is law depends on how far up the hill you have come, and in the crown it alternates.
        var footpath = new AndExpression<CardPlayedTriggeredEffectContext>(
            new OrExpression<CardPlayedTriggeredEffectContext>(
                new NotExpression<CardPlayedTriggeredEffectContext>(
                    Wears<CardPlayedTriggeredEffectContext>(Hill, SurveyedFaceId)),
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    Wears<CardPlayedTriggeredEffectContext>(Hill, CrownOfTheHillId),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                            Hill, CrownedLawCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0)))),
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(4)));

        var measured = new AndExpression<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wears<CardPlayedTriggeredEffectContext>(Hill, SurveyedFaceId),
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        Wears<CardPlayedTriggeredEffectContext>(Hill, CrownOfTheHillId)),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                            Hill, CrownedLawCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)))),
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                    ComparisonOperator.Equal, ThisCost())));

        var footing = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new OrExpression<CardPlayedTriggeredEffectContext>(footpath, measured),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        Violate<CardPlayedTriggeredEffectContext>(
                            Hill, FootpathLaw, HillNotedId,
                            stacks: HillTrespass<CardPlayedTriggeredEffectContext>()),
                        new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                            Hill, new StatusDefinitionId(HillDoubledId)),
                    ])),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, memory, new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                        relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Hill, new StatusDefinitionId(HillNotedId)),
                    // In the crown the two laws take it in turns, and which one is showing is on the board
                    // before the player acts.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Wears<TurnStartedTriggeredEffectContext>(Hill, CrownOfTheHillId),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            Hill, CrownedLawCounter,
                            new SubtractExpression<TurnStartedTriggeredEffectContext>(
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    Hill, CrownedLawCounter)),
                            relative: false)),
                    QueueTheThresholds<TurnStartedTriggeredEffectContext>(),
                ])));

        // "Whenever the Hill WOULD gain a newly created Claim: remove that Claim and bury it." The landscape
        // does not hold standing — it holds the memory of standing.
        EffectProgram<TContext> bury<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ModifySelectedStatusStacksNode<TContext>(
                        Hill,
                        new StatusSelectionSpec(StatusPolarityFilter.Any)
                        {
                            Definition = new StatusDefinitionId(ClaimId),
                        },
                        new ConstantExpression<TContext>(-1)),
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            Buried<TContext>(), ComparisonOperator.Less,
                            new ConstantExpression<TContext>(MaxBuriedClaims)),
                        new ApplyStatusNode<TContext>(
                            Hill, new StatusDefinitionId(BuriedClaimId), new ConstantExpression<TContext>(1))),
                ])));

        // "Crowned Jurisdiction: whenever Safe-Conduct prevents Hill Trespass, the Hill gains 6 Block."
        var crowned = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        Wears<StatusApplicationBlockedTriggeredEffectContext>(Hill, CrownOfTheHillId),
                        new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(HillId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0)))),
                new GainBlockNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Hill, new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(6))));

        return Rule(HillId, "The Landscape Has Standing",
            "On the lower slope the fourth real card of a turn is a Trespass; on the surveyed face it is two "
            + "cards in a row of one price; in the crown the two take it in turns. The hill holds no "
            + "standing at all — every Claim it is granted goes into the ground as a BURIED CLAIM, and at "
            + "251 and again at 123 HP the slope stirs, gives you one whole turn to answer, and then cashes "
            + "out everything under it. Settling with it in full is what takes one back out.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    footing, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    bury<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    bury<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    crowned, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // Thresholds are QUEUED, never taken as they are crossed, and they are resolved in order: the slope
    // before the crown, however fast the player comes up the hill.
    private static IEffectNode<TContext> QueueTheThresholds<TContext>()
        where TContext : class
    {
        ICombatExpression<TContext, bool> NothingPending() =>
            new NotExpression<TContext>(
                new OrExpression<TContext>(
                    new OrExpression<TContext>(
                        Wears<TContext>(Hill, SlopeStirsPendingId),
                        Wears<TContext>(Hill, SlopeAnswersPendingId)),
                    new OrExpression<TContext>(
                        Wears<TContext>(Hill, CrownStirsPendingId),
                        Wears<TContext>(Hill, CrownBreaksPendingId))));

        return new CausalSequenceEffectNode<TContext>(
        [
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(Hill, SurveyedFaceId)),
                    new AndExpression<TContext>(
                        NothingPending(),
                        new ComparisonExpression<TContext>(
                            new CombatantCurrentHealthExpression<TContext>(Hill),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TContext>(FirstThresholdHealth)))),
                new ApplyStatusNode<TContext>(
                    Hill, new StatusDefinitionId(SlopeStirsPendingId), new ConstantExpression<TContext>(1))),
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    Wears<TContext>(Hill, SurveyedFaceId),
                    new AndExpression<TContext>(
                        new NotExpression<TContext>(Wears<TContext>(Hill, CrownOfTheHillId)),
                        new AndExpression<TContext>(
                            NothingPending(),
                            new ComparisonExpression<TContext>(
                                new CombatantCurrentHealthExpression<TContext>(Hill),
                                ComparisonOperator.LessOrEqual,
                                new ConstantExpression<TContext>(SecondThresholdHealth))))),
                new ApplyStatusNode<TContext>(
                    Hill, new StatusDefinitionId(CrownStirsPendingId), new ConstantExpression<TContext>(1))),
        ]);
    }

    // ── Settle the Ground ─────────────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> SettleTheGround()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(HillId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Buried<TurnEndedTriggeredEffectContext>(), ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(BuriedClaimId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                // Nothing under the road to take back: the hill itself gives way a little.
                new SetHealthNode<TurnEndedTriggeredEffectContext>(
                    creditor,
                    new SubtractExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(creditor),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(6)))));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? HillIntent(string enemyId, string intentId)
    {
        if (enemyId != HillEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;
        var buried = Buried<EnemyActionContext>();
        var owed = new CombatantStatusStacksFromSourceExpression<EnemyActionContext>(
            Applicant, new StatusDefinitionId(WergildId), self);

        IEffectNode<EnemyActionContext>? ordinary = intentId switch
        {
            "loose_earth" => Slopes(
                Blow(15),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(17),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(2),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(MaxBuriedClaims), buried)))),
                Blow(22)),
            "raise_the_footpath" => Slopes(
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(20)),
                    DemandWergild<EnemyActionContext>(self, 1),
                ]),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(26))),
            "the_ground_remembers_weight" => Slopes(
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(14),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(2),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(MaxBuriedClaims), buried)))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(14),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId("doubt"),
                        new ConstantExpression<EnemyActionContext>(2)),
                ]),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(18),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(MaxBuriedClaims), buried))))),
            "mark_the_old_boundary" => Slopes(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    DemandWergild<EnemyActionContext>(self, 1),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(SafeConductId),
                        new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                ]),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(HillDoubledId),
                        new ConstantExpression<EnemyActionContext>(1)),
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
                ]),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(SafeConductId),
                        new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                    DemandWergild<EnemyActionContext>(self, 2),
                ])),
            "root_beneath_the_road" => Slopes(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(12),
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(HillDoubledId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ]),
                Blow(21),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(18),
                    DemandWergild<EnemyActionContext>(self, 1),
                ])),
            "the_hill_answers_entirely" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCurrentHealthExpression<EnemyActionContext>(self),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<EnemyActionContext>(HillSignatureHealth)),
                TheHillAnswersEntirely(buried, owed),
                Blow(15)),
            _ => null,
        };

        if (ordinary is null)
            return null;

        // A threshold takes the hill's next action, and the one after it. Nothing else happens on those two.
        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                Wears<EnemyActionContext>(self, CrownBreaksPendingId),
                TheCrownBreaksOpen(buried),
                new ConditionalEffectNode<EnemyActionContext>(
                    Wears<EnemyActionContext>(self, CrownStirsPendingId),
                    Stirs(CrownStirsPendingId, CrownBreaksPendingId),
                    new ConditionalEffectNode<EnemyActionContext>(
                        Wears<EnemyActionContext>(self, SlopeAnswersPendingId),
                        TheSlopeAnswers(buried),
                        new ConditionalEffectNode<EnemyActionContext>(
                            Wears<EnemyActionContext>(self, SlopeStirsPendingId),
                            Stirs(SlopeStirsPendingId, SlopeAnswersPendingId),
                            ordinary)))));
    }

    private static IEffectNode<EnemyActionContext> Slopes(
        IEffectNode<EnemyActionContext> lower,
        IEffectNode<EnemyActionContext> face,
        IEffectNode<EnemyActionContext> crown) =>
        new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(CombatantTargetSelectors.Source, CrownOfTheHillId),
            crown,
            new ConditionalEffectNode<EnemyActionContext>(
                Wears<EnemyActionContext>(CombatantTargetSelectors.Source, SurveyedFaceId),
                face, lower));

    // "A non-damaging boss-state action: name a small price, show what is under the road, and give the
    // player one whole turn." The showing is the marker itself, which the fight draws by name.
    private static IEffectNode<EnemyActionContext> Stirs(string from, string to)
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            DemandWergild<EnemyActionContext>(self, 1),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(to), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(from)),
        ]);
    }

    // "Wergild min(X, 3); 6 Block per Buried Claim answered, at most 30; then the ground is empty."
    private static IEffectNode<EnemyActionContext> TheSlopeAnswers(
        ICombatExpression<EnemyActionContext, int> buried)
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new RepeatEffectNode<EnemyActionContext>(
                new MinExpression<EnemyActionContext>(new ConstantExpression<EnemyActionContext>(3), buried),
                DemandWergild<EnemyActionContext>(self, 1)),
            new GainBlockNode<EnemyActionContext>(
                self,
                new MinExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(30),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(6), buried))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(BuriedClaimId)),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(SurveyedFaceId), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(SlopeAnswersPendingId)),
        ]);
    }

    // "4 direct HP per Buried Claim, at most 20; then the ground is empty, the player is licensed, and the
    // hill's guard is gone." Direct loss, so nothing recursive comes back out of it.
    private static IEffectNode<EnemyActionContext> TheCrownBreaksOpen(
        ICombatExpression<EnemyActionContext, int> buried)
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new SetHealthNode<EnemyActionContext>(
                Applicant,
                new SubtractExpression<EnemyActionContext>(
                    new CombatantCurrentHealthExpression<EnemyActionContext>(Applicant),
                    new MinExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(20),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(4), buried)))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(BuriedClaimId)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            new ModifyDefensivePoolNode<EnemyActionContext>(
                self, StandardCombatIds.BlockDefensivePool,
                new NegateExpression<EnemyActionContext>(
                    new CombatantDefensivePoolExpression<EnemyActionContext>(
                        self, StandardCombatIds.BlockDefensivePool))),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(CrownOfTheHillId), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(CrownBreaksPendingId)),
        ]);
    }

    // "24 +3 per Buried Claim +2 per open Wergild point, to a maximum of 34. Then the ground is empty and
    // the hill has nothing left to hide behind."
    private static IEffectNode<EnemyActionContext> TheHillAnswersEntirely(
        ICombatExpression<EnemyActionContext, int> buried,
        ICombatExpression<EnemyActionContext, int> owed)
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MinExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(34),
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(24),
                        new AddExpression<EnemyActionContext>(
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(3), buried),
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(2), owed))))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(BuriedClaimId)),
            new ModifyDefensivePoolNode<EnemyActionContext>(
                self, StandardCombatIds.BlockDefensivePool,
                new NegateExpression<EnemyActionContext>(
                    new CombatantDefensivePoolExpression<EnemyActionContext>(
                        self, StandardCombatIds.BlockDefensivePool))),
        ]);
    }
}
