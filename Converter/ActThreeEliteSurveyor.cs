using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 7 — **Surveyor of Forgotten Paths** (198 HP).
//
// > Yesterday's invalid boundary can become today's legal defence if the player knows when to cite it.
//
// A figure made of survey stakes, measuring cord, boundary stones and old maps, walking backward through
// its own measurements. Three Survey Laws stand at once and only one of them is CURRENT: the Current Survey
// files Trespass, the Former Survey is obsolete — and breaking an obsolete law is where an **Old Right**
// comes from. The Unsurveyed law does nothing at all.
//
//   Footfall — the third real card of a turn.
//   Measure  — two cards in a row of the same Base Cost.
//   Margin   — ending a turn with no Energy left.
//
// Every Claim it is granted RE-SURVEYS: Current becomes Former, Former becomes Unsurveyed, Unsurveyed
// becomes Current. So the standing you hand it is what turns the law you were breaking on purpose into the
// law you are being punished under.
//
// The citation is a card the fight puts in your hand, for the same reason Make Amends is one: a combat here
// has no free actions, only cards.
public static partial class ActThree
{
    public const string SurveyorEnemyId = "surveyor_of_forgotten_paths";
    public const string SurveyorId = "the_survey_map";
    public const string OldRightId = "old_right";
    public const string CiteTheOldSurveyCardId = "cite_the_old_survey";
    public const string OldBoundaryId = "old_boundary";
    public const string OldRightOfPassageId = "old_right_of_passage";
    public const string OldRightTakenId = "old_right_taken";
    public const string SurveyCitedId = "survey_cited";

    public const int FootfallLaw = 20;
    public const int MeasureLaw = 21;
    public const int MarginLaw = 22;

    public const int MaxOldRights = 2;

    public static readonly TagId CiteTag = new("cite_the_old_survey");

    // Which of the three laws is CURRENT: 0 Footfall, 1 Measure, 2 Margin. Former is the one before it and
    // Unsurveyed the one after, so a single number is the whole map — and a Re-Survey is +1.
    public static CounterId SurveyPositionCounter => new("survey_position");

    private static readonly (string Key, string Name, int Law)[] Surveys =
    [
        ("footfall", "Footfall", FootfallLaw),
        ("measure", "Measure", MeasureLaw),
        ("margin", "Margin", MarginLaw),
    ];

    private static string CurrentId(string key) => $"survey_current_{key}";
    private static string FormerId(string key) => $"survey_former_{key}";

    private static ICombatantTargetSelector Surveyor { get; } = Elite(SurveyorId);

    private static IEnumerable<StatusData> SurveyorStatuses() =>
    [
        TheSurveyMap(),
        OldRight(),
        Marker(OldBoundaryId, "Old Boundary Cited",
            "Current and Former Survey are swapped for the rest of this turn."),
        Marker(OldRightOfPassageId, "Old Right of Passage",
            "The Surveyor's next attempt to cash a Claim this turn comes to nothing. The Claim remains."),
        Marker(OldRightTakenId, "Forgotten Path Walked",
            "You have already earned an Old Right this turn."),
        Marker(SurveyCitedId, "Survey Cited",
            "You have already cited the old survey this turn."),
        .. Surveys.SelectMany(s => new[]
        {
            Marker(CurrentId(s.Key), $"Current Survey: {s.Name}",
                $"The {s.Name} law is in force. Breaking it is a Trespass owed to the Surveyor."),
            Marker(FormerId(s.Key), $"Former Survey: {s.Name}",
                $"The {s.Name} law is obsolete. Breaking it is worth an Old Right instead."),
        }),
    ];

    private static StatusData OldRight() => new()
    {
        Id = OldRightId,
        NameKey = "Old Right",
        DescriptionKey =
            "A right earned by doing what yesterday's map forbade. Spend one to cite the old survey. At most 2.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, bool> Wearing<TContext>(
        ICombatantTargetSelector who, string statusId)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(statusId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // Which law is in force, read through the citation: OLD BOUNDARY swaps the two for a turn, so "current"
    // is a question about the map AND about what the player has argued today.
    private static ICombatExpression<TContext, bool> IsCurrent<TContext>(string key)
        where TContext : class =>
        new OrExpression<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Wearing<TContext>(Applicant, OldBoundaryId)),
                Wearing<TContext>(Surveyor, CurrentId(key))),
            new AndExpression<TContext>(
                Wearing<TContext>(Applicant, OldBoundaryId),
                Wearing<TContext>(Surveyor, FormerId(key))));

    private static ICombatExpression<TContext, bool> IsFormer<TContext>(string key)
        where TContext : class =>
        new OrExpression<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Wearing<TContext>(Applicant, OldBoundaryId)),
                Wearing<TContext>(Surveyor, FormerId(key))),
            new AndExpression<TContext>(
                Wearing<TContext>(Applicant, OldBoundaryId),
                Wearing<TContext>(Surveyor, CurrentId(key))));

    // ── the map ───────────────────────────────────────────────────────────────────────────────────────────
    private static StatusData TheSurveyMap()
    {
        var player = CombatantTargetSelectors.Source;
        var memory = CostMemory("survey_map");

        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        // Footfall — the third real card of the turn. Measure — two in a row of the same Base Cost.
        var walked = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                Surveyed("footfall", FootfallLaw,
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(3))),
                Surveyed("measure", MeasureLaw,
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                            ComparisonOperator.Equal, ThisCost()))),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        // Margin — ending a turn with nothing left to spend.
        var margin = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                Surveyed("margin", MarginLaw,
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                            Applicant, StandardCombatIds.EnergyResource),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))));

        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, memory, new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                        relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(OldBoundaryId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(OldRightTakenId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(SurveyCitedId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(OldRightOfPassageId)),
                    // The map has to say something even on the first bell of the fight.
                    DrawTheMap<TurnStartedTriggeredEffectContext>(),
                    // A right earned as a turn ENDED has no card to cite it with — the hand it was dealt
                    // into is put away in the same breath — so the citation is offered again at the bell.
                    OfferACitation<TurnStartedTriggeredEffectContext>(),
                ])));

        EffectProgram<TContext> resurvey<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        Surveyor, SurveyPositionCounter, new ConstantExpression<TContext>(1), relative: true),
                    DrawTheMap<TContext>(),
                ])));

        return Rule(SurveyorId, "The Survey Map",
            "Three laws stand at once and only the CURRENT one is law: Footfall (the third real card of a "
            + "turn), Measure (two in a row of the same Base Cost) and Margin (ending a turn with no "
            + "Energy). Breaking the FORMER survey is worth 1 Old Right instead, once a turn and at most 2. "
            + "Every Claim the Surveyor is granted re-surveys: Current becomes Former, Former becomes "
            + "Unsurveyed, Unsurveyed becomes Current.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    walked, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    margin, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    resurvey<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    resurvey<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // What one law does when it is broken: the Current survey files, the Former survey pays, and the
    // Unsurveyed one says nothing at all.
    private static IEffectNode<TContext> Surveyed<TContext>(
        string key, int law, ICombatExpression<TContext, bool> broken)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            broken,
            new ConditionalEffectNode<TContext>(
                IsCurrent<TContext>(key),
                Violate<TContext>(Surveyor, law),
                new ConditionalEffectNode<TContext>(
                    new AndExpression<TContext>(
                        IsFormer<TContext>(key),
                        new AndExpression<TContext>(
                            new NotExpression<TContext>(Wearing<TContext>(Applicant, OldRightTakenId)),
                            new ComparisonExpression<TContext>(
                                new CombatantStatusStacksExpression<TContext>(
                                    Applicant, new StatusDefinitionId(OldRightId)),
                                ComparisonOperator.Less,
                                new ConstantExpression<TContext>(MaxOldRights)))),
                    EarnAnOldRight<TContext>())));

    private static IEffectNode<TContext> EarnAnOldRight<TContext>()
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new ApplyStatusNode<TContext>(
                Applicant, new StatusDefinitionId(OldRightId), new ConstantExpression<TContext>(1)),
            new ApplyStatusNode<TContext>(
                Applicant, new StatusDefinitionId(OldRightTakenId), new ConstantExpression<TContext>(1)),
            // The right is no use without something to cite it with.
            OfferACitation<TContext>(),
        ]);

    private static IEffectNode<TContext> OfferACitation<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        Applicant, new StatusDefinitionId(OldRightId)),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand, CiteTag),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
            new CreateCardInstanceNode<TContext>(
                Applicant, new CardDefinitionId(CiteTheOldSurveyCardId), CardZone.Hand,
                new ConstantExpression<TContext>(1)));

    // The map, written out of the one number that holds it. Current is the law at the position, Former the
    // one before it, Unsurveyed the one after — which is why a Re-Survey is a single increment.
    private static IEffectNode<TContext> DrawTheMap<TContext>()
        where TContext : class
    {
        var steps = new List<IEffectNode<TContext>>();

        // Wiped and redrawn, rather than added to: a map is a statement about where the boundary is now.
        foreach (var survey in Surveys)
        {
            steps.Add(new RemoveStatusNode<TContext>(Surveyor, new StatusDefinitionId(CurrentId(survey.Key))));
            steps.Add(new RemoveStatusNode<TContext>(Surveyor, new StatusDefinitionId(FormerId(survey.Key))));
        }

        for (var position = 0; position < Surveys.Length; position++)
        {
            var current = Surveys[position].Key;
            var former = Surveys[(position + 2) % Surveys.Length].Key;

            steps.Add(new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new RemainderExpression<TContext>(
                        new CombatantCounterExpression<TContext>(Surveyor, SurveyPositionCounter),
                        new ConstantExpression<TContext>(Surveys.Length)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(position)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(
                        Surveyor, new StatusDefinitionId(CurrentId(current)),
                        new ConstantExpression<TContext>(1)),
                    new ApplyStatusNode<TContext>(
                        Surveyor, new StatusDefinitionId(FormerId(former)),
                        new ConstantExpression<TContext>(1)),
                ])));
        }

        return new CausalSequenceEffectNode<TContext>(steps);
    }

    // ── Cite the Old Survey ───────────────────────────────────────────────────────────────────────────────
    //
    // Once a player turn, and it costs an Old Right. Three citations, and which one is worth spending is the
    // whole of the encounter's counterplay.
    public static CardData CiteTheOldSurvey() => new()
    {
        Id = CiteTheOldSurveyCardId,
        NameKey = "Cite the Old Survey",
        DescriptionKey =
            "Spend 1 Old Right, once a turn. OLD BOUNDARY — Current and Former Survey are swapped for the "
            + "rest of this turn. OLD RIGHT OF PASSAGE — the Surveyor's next attempt to cash a Claim comes "
            + "to nothing, and the Claim remains. OLD MEASURE — remove up to 8 of the Surveyor's Block.",
        Costs = [],
        Tags = [CiteTag, new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
            new ConditionalEffectNode<CardPlayContext>(
                new AndExpression<CardPlayContext>(
                    new NotExpression<CardPlayContext>(Wearing<CardPlayContext>(Applicant, SurveyCitedId)),
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusStacksExpression<CardPlayContext>(
                            Applicant, new StatusDefinitionId(OldRightId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardPlayContext>(1))),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new ModifyStatusStacksNode<CardPlayContext>(
                        Applicant, new StatusDefinitionId(OldRightId),
                        new ConstantExpression<CardPlayContext>(-1)),
                    new ApplyStatusNode<CardPlayContext>(
                        Applicant, new StatusDefinitionId(SurveyCitedId),
                        new ConstantExpression<CardPlayContext>(1)),
                    new ChooseOptionsNode<CardPlayContext>(
                    [
                        new ApplyStatusNode<CardPlayContext>(
                            Applicant, new StatusDefinitionId(OldBoundaryId),
                            new ConstantExpression<CardPlayContext>(1)),
                        new ApplyStatusNode<CardPlayContext>(
                            Applicant, new StatusDefinitionId(OldRightOfPassageId),
                            new ConstantExpression<CardPlayContext>(1)),
                        new ModifyDefensivePoolNode<CardPlayContext>(
                            Surveyor, StandardCombatIds.BlockDefensivePool,
                            new ConstantExpression<CardPlayContext>(-8)),
                    ],
                    ["old boundary", "old right of passage", "old measure"],
                    count: 1, purpose: "cite the old survey"),
                ])),
            // The card exhausts when it is played and comes back while a right is still held — the copy
            // being played is still counted in hand, so the threshold is one rather than none.
            AnotherCitation(),
        ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    private static IEffectNode<CardPlayContext> AnotherCitation() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        Applicant, new StatusDefinitionId(OldRightId)),
                    ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantZoneCardCountExpression<CardPlayContext>(Applicant, CardZone.Hand, CiteTag),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<CardPlayContext>(1))),
            new CreateCardInstanceNode<CardPlayContext>(
                Applicant, new CardDefinitionId(CiteTheOldSurveyCardId), CardZone.Hand,
                new ConstantExpression<CardPlayContext>(1)));

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? SurveyorIntent(string enemyId, string intentId)
    {
        if (enemyId != SurveyorEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        var oldRights = new CombatantStatusStacksExpression<EnemyActionContext>(
            Applicant, new StatusDefinitionId(OldRightId));

        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            "drive_the_first_stake" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, SurveyPositionCounter, new ConstantExpression<EnemyActionContext>(1),
                    relative: true),
                DrawTheMap<EnemyActionContext>(),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(16)),
            ]),
            // "14 +5 per Old Right, max 24" — two rights' worth.
            "measure_what_was_forgotten" => new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(14),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(5),
                        new MinExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(MaxOldRights), oldRights)))),
            // The Surveyor hands you a licence and shows you what it is about to measure you against.
            "declare_the_new_boundary" => new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            // "Consume all Old Rights; 16 +6 per consumed, max 28."
            "close_the_survey" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(16),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(6),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(MaxOldRights), oldRights)))),
                new RemoveStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(OldRightId)),
            ]),
            // "20 damage; if the Surveyor has a Claim, consume 1 and gain 12 Block." An Old Right of
            // Passage stops the cashing and leaves the Claim standing.
            "stake_through_the_map" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Blow(20),
                new ConditionalEffectNode<EnemyActionContext>(
                    new AndExpression<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                self, new StatusDefinitionId(ClaimId)),
                            ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                        new NotExpression<EnemyActionContext>(
                            Wearing<EnemyActionContext>(Applicant, OldRightOfPassageId))),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        ConsumeClaim<EnemyActionContext>(self),
                        new GainBlockNode<EnemyActionContext>(
                            self, new ConstantExpression<EnemyActionContext>(12)),
                    ]),
                    // The citation is spent stopping it, and the standing stays where it is.
                    new RemoveStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(OldRightOfPassageId))),
            ]),
            _ => null,
        };

        return act is null ? null : new EffectProgram<EnemyActionContext>(act);
    }
}
