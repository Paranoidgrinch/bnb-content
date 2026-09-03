using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Surveyor of the Errant Cord. An ancient surveyor moves the same boundary stones and
// solemnly insists they were always there.
//
// Its whole identity is that the PLAYER picks how hard the survey is. Each of your turns it offers two exact
// values it will accept, and you take one:
//
//   NEAR BOUNDARY — the lower figure, easier to hit. Meeting it lets the Surveyor brace: 10 Block.
//   FAR BOUNDARY  — the higher figure. Meeting it costs the Surveyor 10 HP and strips what it was standing
//                   behind — which is the counterpressure the easier measurement does not buy you.
//
// A combat has no generic prompt, so the offer IS two cards in your hand (the Living Petition Chorus's
// idiom): playing one raises that measure and records which boundary you chose. Playing neither is a real
// answer too — no measure is raised, and the Surveyor gets nothing to answer.
//
// Missing the measure you chose is a Boundary Error, and the second one moves the boundary.
public static partial class ActFour
{
    public const string SurveyorEliteEnemyId = "surveyor_of_the_errant_cord";

    public const string ErrantCordId = "the_errant_cord";
    public const string ChoseTheFarBoundaryId = "chose_the_far_boundary";
    public const string CordSlackId = "cord_slack";

    public const string NearBoundaryCardId = "near_boundary";
    public const string FarBoundaryCardId = "far_boundary";

    // Two errors move the boundary (§7.2's maximum).
    public const int BoundaryErrorLimit = 2;

    private const int NearBrace = 10;
    private const int FarHealthLoss = 10;
    private const int FarBlockStripped = 10;
    private const int ReTensionedStrip = 14;

    // The offered figures, kept on the PLAYER because that is who has to act on them and read them — the
    // same reason the measure itself is a status and not a counter. The offer cards raise exactly these.
    public static CounterId SurveyNear => new("survey_near");
    public static CounterId SurveyFar => new("survey_far");

    // The Surveyor's own books: which pair comes next, how many errors stand, how much a Far success strips,
    // and its bookmark in the act's resolution tallies.
    public static CounterId SurveyPair => new("survey_pair");
    public static CounterId BoundaryError => new("boundary_error");
    public static CounterId FarStrip => new("far_strip");
    public static CounterId SurveyRead => new("survey_read");

    // The three pairs it rotates through, in an order in which no pair follows itself.
    private static readonly (int Near, int Far)[] Pairs = [(1, 2), (2, 3), (1, 3)];

    public static EffectProgram<EnemyActionContext>? SurveyorEliteIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "surveyor_of_the_errant_cord.strike_the_cord" => StrikeTheCord(11),
            "surveyor_of_the_errant_cord.re_tension_the_measure" => ReTensionTheMeasure(22),
            "surveyor_of_the_errant_cord.the_boundary_moves" => TheBoundaryMoves(31),
            _ => null,
        };

    public static IReadOnlyList<StatusData> SurveyorEliteStatuses() =>
        [TheErrantCord(), ChoseTheFarBoundary(), CordSlack()];

    // ── the cord ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheErrantCord() => new()
    {
        Id = ErrantCordId,
        NameKey = "The Errant Cord",
        DescriptionKey =
            "Each of your turns this surveyor offers two exact measures it will accept. Meet the near one and "
            + "it braces; meet the far one and it bleeds and loses its cover. Miss the one you chose and you "
            + "are buried 1 deeper — twice, and the boundary moves.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(OfferTheSurvey(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(AnswerTheSurvey(), nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // Which boundary the player took. A marker and not a counter: what the Surveyor asks afterwards is which
    // of two things happened, and the answer has to survive the card that set it being exhausted.
    public static StatusData ChoseTheFarBoundary() => new()
    {
        Id = ChoseTheFarBoundaryId,
        NameKey = "Far Boundary Taken",
        DescriptionKey = "You took the surveyor's harder figure. Meeting it costs it blood and cover.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The offer, at the player's turn start — after the refill, which is the only moment "what you can
    // realistically spend this turn" has an answer (§6.2). Both figures pass through the shared filter, so
    // the harder one is never a figure the turn cannot reach.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OfferTheSurvey()
    {
        var surveyor = Bearer(ErrantCordId);

        IEffectNode<TurnStartedTriggeredEffectContext> OfferPair(int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(surveyor, SurveyPair),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(Pairs.Length)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, SurveyFar,
                        Achievable<TurnStartedTriggeredEffectContext>(Pairs[index].Far), relative: false),

                    // The near figure is the lower of the two, and never higher than the far one after the
                    // filter has clamped it: at 1 Energy both boundaries are 1, and the choice is only about
                    // which answer you want.
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, SurveyNear,
                        new MinExpression<TurnStartedTriggeredEffectContext>(
                            Achievable<TurnStartedTriggeredEffectContext>(Pairs[index].Near),
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, SurveyFar)),
                        relative: false),
                ]));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // Last turn's answer is spent: a new survey is a new choice.
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(ChoseTheFarBoundaryId)),

                    .. Enumerable.Range(0, Pairs.Length).Select(OfferPair),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        surveyor, SurveyPair,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),

                    new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new CardDefinitionId(NearBoundaryCardId), CardZone.Hand,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new CardDefinitionId(FarBoundaryCardId), CardZone.Hand,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                ])));
    }

    // …and the answer, at the Surveyor's own turn start: the act's ordering-free idiom, one bookmark in the
    // resolution tallies, so a survey is answered exactly once however many bodies watched it resolve.
    private static EffectProgram<TurnStartedTriggeredEffectContext> AnswerTheSurvey()
    {
        var surveyor = CombatantTargetSelectors.Source;
        var unread = ResolutionsSinceLastLooked<TurnStartedTriggeredEffectContext>(surveyor, SurveyRead);

        var wasMet = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, MeasureResult),
            ComparisonOperator.Equal,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(1));

        var tookTheFar = new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
            Applicant, new StatusDefinitionId(ChoseTheFarBoundaryId));

        // The far success: blood and cover. The HP loss is not damage — it is not dealt, not blocked, and no
        // rule that answers a blow hears it, which is what the master means by "the HP Loss is not Damage".
        //
        // The cover is taken as SLACK IN THE CORD rather than as Block removed on the spot, and that is not a
        // softening: Block expires at its owner's turn start, so by the moment an answer at the Surveyor's
        // own turn could reach for it there is never any there. What a stripped brace actually costs a body
        // is the brace it does not get — so the next one it makes is that much weaker, and the slack is spent
        // making it.
        var farSuccess = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new DealDamageNode<TurnStartedTriggeredEffectContext>(
                surveyor, new ConstantExpression<TurnStartedTriggeredEffectContext>(FarHealthLoss),
                ignoresBlock: true, kind: DamageKind.DamageOverTime),

            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                surveyor, new StatusDefinitionId(CordSlackId),
                new MaxExpression<TurnStartedTriggeredEffectContext>(
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(FarBlockStripped),
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(surveyor, FarStrip)),
                sourceSelector: surveyor),

            // A re-tensioned cord strips harder ONCE: the next far success spends it.
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                surveyor, FarStrip,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
        ]);

        var failure = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(EntombedId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: surveyor),

            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                surveyor, BoundaryError,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
        ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    MoveTheResolutionBookmark<TurnStartedTriggeredEffectContext>(surveyor, SurveyRead),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        wasMet,
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            tookTheFar,
                            farSuccess,
                            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                surveyor,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(NearBrace))),
                        failure),
                ])));
    }

    // Cover taken off a body that has not put it up yet: the next brace it makes is this much weaker, and
    // the slack is spent making it.
    public static StatusData CordSlack() => new()
    {
        Id = CordSlackId,
        NameKey = "Slack in the Cord",
        DescriptionKey = "The next Block this body gains is reduced by this much, and the slack goes with it.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.BlockGain,
                PassiveModifierOperation.AddPerStack, -1, RestrictDamageKind: null),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<BlockGainedTriggeredEffectContext>(
                new RemoveStatusNode<BlockGainedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(CordSlackId))),
                nameof(TriggerEvent.BlockGained)),
        ],
    };

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // Eleven twice: two blows, so Block spent on the first still stands against the second.
    private static EffectProgram<EnemyActionContext> StrikeTheCord(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
        ]));

    // Brace, and re-tension the cord: the NEXT far success strips 14 rather than 10.
    private static EffectProgram<EnemyActionContext> ReTensionTheMeasure(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new SetCombatantCounterNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, FarStrip, Const(ReTensionedStrip), relative: false),
        ]));

    // The signature: the boundary is where the surveyor now says it is, and the record is wiped so the
    // dispute can start again. Queued by the error count and telegraphed like any other intent (§6.5).
    private static EffectProgram<EnemyActionContext> TheBoundaryMoves(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(BurdenedId), Const(1)),

            new SetCombatantCounterNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, BoundaryError, Const(0), relative: false),
        ]));

    // ── the offer ─────────────────────────────────────────────────────────────────────────────────────────

    // The two cards the offer IS. Free, and gone at the turn's end whether they are played or not: a survey
    // is offered once, and refusing is a real answer — no measure is raised, and the Surveyor is left with
    // nothing to answer either.
    public static IReadOnlyList<CardData> SurveyorOfferCards() =>
    [
        OfferCard(NearBoundaryCardId, "Near Boundary", SurveyNear,
            "Accept the surveyor's nearer figure as this turn's exact measure. Meeting it lets it brace.",
            far: false),
        OfferCard(FarBoundaryCardId, "Far Boundary", SurveyFar,
            "Accept the surveyor's further figure as this turn's exact measure. Meeting it costs it 10 HP "
            + "and strips its cover.",
            far: true),
    ];

    private static CardData OfferCard(string id, string name, CounterId figure, string text, bool far)
    {
        var play = new List<IEffectNode<CardPlayContext>>
        {
            // §3.1 still holds inside an elite: one measure stands at a time, and the offer never raises a
            // second one over somebody else's.
            new ConditionalEffectNode<CardPlayContext>(
                new NotExpression<CardPlayContext>(
                    new TargetHasStatusExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(WeighedId))),
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(WeighedId),
                    new CombatantCounterExpression<CardPlayContext>(CombatantTargetSelectors.Source, figure))),
        };

        if (far)
            play.Add(new ApplyStatusNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ChoseTheFarBoundaryId),
                new ConstantExpression<CardPlayContext>(1)));

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId("survey"), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(new SequenceEffectNode<CardPlayContext>(play)),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
