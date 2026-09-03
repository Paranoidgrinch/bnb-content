using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Architect of the Impossible Pyramid. A dead master builder before a pyramid whose upper
// courses continue in several mutually incompatible directions. He does not believe the plan is wrong.
//
//   > Reality has failed the blueprint.
//
// The fight is not a test. It is a SCHEDULE. The MONUMENT climbs one step at the end of every one of his
// turns, and at six the Capstone comes down — 42, two burials, and 24 of stone to stand behind. Nothing stops
// that. The only thing the player has is the brake.
//
// So after the draw he lays two BLUEPRINTS on the table, and the player picks the one that governs the turn:
//
//   MEASURED FOUNDATION   spend exactly 2.      met: Monument −1.  missed: Monument +1 and a burial.
//   EQUAL COURSES         play exactly 2 Deeds. met: Monument −1.  missed: Burdened 1.
//   ALTERNATING STONE     the first two cards you play must be of different kinds.
//                                               met: Monument −1.  missed: 2 Paperwork.
//   MEASURED COURSE       the fallback, and the only one whose figure moves: spend exactly as much as the
//                         turn can actually spend (§5.2), offered whenever an ordinary blueprint cannot be
//                         deterministically met.
//
// Every SECOND course laid true costs him 8 of his own blood and 8 of his cover — succeeding is not merely
// slowing the clock, it is the fight's only free damage.
//
// After the first Capstone (or at 320, whichever comes first) THE PLAN WAS ALWAYS CORRECT: from then on every
// blueprint that resolves adds one further step of its own, so a course laid true only holds the schedule and
// a course missed doubles it. And under 100, with four steps standing, he places the Eternal Capstone once —
// 36 and two per step, to a ceiling of 48.
public static partial class ActFour
{
    public const string ArchitectEnemyId = "architect_of_the_impossible_pyramid";

    public const string ImpossiblePyramidId = "the_impossible_pyramid";
    public const string MonumentId = "monument";
    public const string PlanAlwaysCorrectId = "the_plan_was_always_correct";
    public const string EternalCapstoneId = "the_eternal_capstone_is_placed";

    // The four blueprints, each a face on HIS table saying what this turn was accepted as. On his body and
    // not on the player's: see ADAPTATIONS — a neutral rule-marker applied to the player is an application
    // like any other, so the register enlarges it and eats an Inscribed doing it.
    public const string BlueprintFoundationId = "blueprint_measured_foundation";
    public const string BlueprintCoursesId = "blueprint_equal_courses";
    public const string BlueprintAlternatingId = "blueprint_alternating_stone";
    public const string BlueprintCourseId = "blueprint_measured_course";

    // …and the four cards that accept them. A boss's offer is CARDS: the choice is made by playing one.
    public const string FoundationCardId = "the_measured_foundation";
    public const string CoursesCardId = "the_equal_courses";
    public const string AlternatingCardId = "the_alternating_stone";
    public const string CourseCardId = "the_measured_course";

    public const string BlueprintTag = "blueprint";

    public const int MonumentCap = 6;
    public const int CapstoneResets = 3;
    public const int EternalResets = 2;
    private const int GreatCapstoneDamage = 42;
    private const int CapstoneBlock = 24;
    private const int CorrectedCourseLoss = 8;
    private const int TransitionBlock = 18;
    private const int PlanCorrectAt = 320;

    // How many courses have been laid true (every second one bleeds him), how the offer rotates, what the
    // Measured Course last demanded, and the first two kinds of card this turn — the only way "different
    // kinds" is answerable at a turn's end.
    public static CounterId CoursesLaid => new("courses_laid");
    public static CounterId BlueprintStep => new("blueprint_step");
    public static CounterId CourseDemand => new("measured_course_demand");
    public static CounterId CoursesThisTurn => new("courses_this_turn");
    public static CounterId FirstCourse => new("first_course_kind");
    public static CounterId SecondCourse => new("second_course_kind");

    private static readonly string[] Blueprints =
        [BlueprintFoundationId, BlueprintCoursesId, BlueprintAlternatingId, BlueprintCourseId];

    public static EffectProgram<EnemyActionContext>? ArchitectIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "architect_of_the_impossible_pyramid.night_shift_on_the_ramp" => NightShift(),
            "architect_of_the_impossible_pyramid.capstone_descends" => CapstoneDescends(),
            "architect_of_the_impossible_pyramid.place_the_eternal_capstone" => EternalCapstone(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> ArchitectStatuses() =>
    [
        TheImpossiblePyramid(),
        Monument(),
        PlanWasAlwaysCorrect(),
        Marker(EternalCapstoneId, "The Eternal Capstone Is Placed",
            "He has done the one thing he came to do. It does not happen twice."),
        Marker(BlueprintFoundationId, "Blueprint: Measured Foundation",
            "The course you accepted: spend exactly 2 Energy this turn."),
        Marker(BlueprintCoursesId, "Blueprint: Equal Courses",
            "The course you accepted: play exactly 2 Deeds this turn."),
        Marker(BlueprintAlternatingId, "Blueprint: Alternating Stone",
            "The course you accepted: the first two cards you play must be of different kinds."),
        Course(),
    ];

    public static IReadOnlyList<CardData> ArchitectCards() =>
    [
        BlueprintCard(FoundationCardId, "The Measured Foundation",
            "Accept this blueprint: spend exactly 2 Energy this turn. Met, the Monument falls a step; missed, "
            + "it climbs one and you are Entombed.", BlueprintFoundationId, demanded: false),
        BlueprintCard(CoursesCardId, "The Equal Courses",
            "Accept this blueprint: play exactly 2 Deeds this turn. Met, the Monument falls a step; missed, "
            + "you are Burdened.", BlueprintCoursesId, demanded: false),
        BlueprintCard(AlternatingCardId, "The Alternating Stone",
            "Accept this blueprint: the first two cards you play must be of different kinds. Met, the "
            + "Monument falls a step; missed, 2 Paperwork.", BlueprintAlternatingId, demanded: false),
        BlueprintCard(CourseCardId, "The Measured Course",
            "Accept the fallback course: spend exactly what the Architect has measured for this turn. Met, "
            + "the Monument falls a step; missed, it climbs one.", BlueprintCourseId, demanded: true),
    ];

    // ── the monument ──────────────────────────────────────────────────────────────────────────────────────

    // Six steps and the capstone comes down. It is a stacked status and not a counter on purpose: the whole
    // fight is planned around this number, so it has to be a thing the player can look at.
    public static StatusData Monument() => new()
    {
        Id = MonumentId,
        NameKey = "Monument",
        DescriptionKey =
            "The courses that stand. One more at the end of every Architect turn, and at six the Capstone "
            + "descends.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData PlanWasAlwaysCorrect() => new()
    {
        Id = PlanAlwaysCorrectId,
        NameKey = "The Plan Was Always Correct",
        DescriptionKey =
            "The second half. Every blueprint that resolves now adds a further step of its own — a course "
            + "laid true only holds the schedule, and a course missed doubles it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData Marker(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The one blueprint whose figure moves carries it in its stacks — the demand is the face.
    private static StatusData Course() => new()
    {
        Id = BlueprintCourseId,
        NameKey = "Blueprint: Measured Course",
        DescriptionKey =
            "The course you accepted, measured for this turn: spend exactly this much Energy. Met, the "
            + "Monument falls a step; missed, it climbs one.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheImpossiblePyramid() => new()
    {
        Id = ImpossiblePyramidId,
        NameKey = "The Impossible Pyramid",
        DescriptionKey =
            "Two blueprints after your draw, and you pick the one that governs the turn. Laid true the "
            + "Monument falls a step, and every second true course costs him 8 blood and 8 cover. Missed, it "
            + "climbs.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(LayTheBlueprints(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(CountTheCourses(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(JudgeTheBlueprint(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheFailsafe(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // ── the offer ─────────────────────────────────────────────────────────────────────────────────────────

    // After the draw: two different blueprints, both of which this turn can deterministically meet (§5.2, and
    // master 8.2 — "no Blueprint may be offered unless it is deterministically achievable"). What cannot be
    // met is replaced by the Measured Course, whose figure is chosen through the act's own filter.
    private static EffectProgram<TurnStartedTriggeredEffectContext> LayTheBlueprints()
    {
        var architect = Bearer(ImpossiblePyramidId);
        var step = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(architect, BlueprintStep);

        ICombatExpression<TurnStartedTriggeredEffectContext, int> InHand(string tag) =>
            new CombatantZoneCardCountExpression<TurnStartedTriggeredEffectContext>(
                Applicant, CardZone.Hand, new TagId(tag));

        var energy = new CombatantCurrentResourceExpression<TurnStartedTriggeredEffectContext>(
            Applicant, StandardCombatIds.EnergyResource);

        ICombatExpression<TurnStartedTriggeredEffectContext, bool> AtLeast(
            ICombatExpression<TurnStartedTriggeredEffectContext, int> value, int floor) =>
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                value, ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(floor));

        // Two Energy is what a turn opens with, so the Foundation is the ordinary case; the other two need
        // the hand to actually hold the cards they ask for.
        var canFound = AtLeast(energy, 2);
        var canCourse = new AndExpression<TurnStartedTriggeredEffectContext>(
            AtLeast(energy, 2), AtLeast(InHand(Cards.CardAuthoring.DeedTag), 2));
        var canAlternate = new AndExpression<TurnStartedTriggeredEffectContext>(
            AtLeast(energy, 2),
            new AndExpression<TurnStartedTriggeredEffectContext>(
                AtLeast(InHand(Cards.CardAuthoring.DeedTag), 1),
                AtLeast(InHand(Cards.CardAuthoring.WorkingTag), 1)));

        IEffectNode<TurnStartedTriggeredEffectContext> Lay(string cardId) =>
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                Applicant, new CardDefinitionId(cardId), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1));

        // Slot one takes the rotation's first choice, slot two its second; whichever cannot be met is laid as
        // the Measured Course instead, and the pair is never twice the same sheet.
        IEffectNode<TurnStartedTriggeredEffectContext> Pair(
            ICombatExpression<TurnStartedTriggeredEffectContext, bool> firstOk, string firstCard,
            ICombatExpression<TurnStartedTriggeredEffectContext, bool> secondOk, string secondCard,
            int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(
                        step, new ConstantExpression<TurnStartedTriggeredEffectContext>(3)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        firstOk, Lay(firstCard), Lay(CourseCardId)),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        secondOk, Lay(secondCard),
                        // The fallback is already on the table if the first could not be met; then the second
                        // sheet is the Foundation, which two Energy can always answer.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            firstOk, Lay(CourseCardId), Lay(FoundationCardId))),
                ]));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // Last turn's sheet is spent, whether it was laid true or not.
                    .. Blueprints.Select(id =>
                        (IEffectNode<TurnStartedTriggeredEffectContext>)
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            architect, new StatusDefinitionId(id))),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        architect, CoursesThisTurn,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        architect, FirstCourse,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        architect, SecondCourse,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

                    // What the fallback would ask for, measured against what the turn can actually spend.
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        architect, CourseDemand,
                        Achievable<TurnStartedTriggeredEffectContext>(2), relative: false),

                    Pair(canFound, FoundationCardId, canCourse, CoursesCardId, 0),
                    Pair(canCourse, CoursesCardId, canAlternate, AlternatingCardId, 1),
                    Pair(canAlternate, AlternatingCardId, canFound, FoundationCardId, 2),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        architect, BlueprintStep,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                ])));
    }

    // The first two kinds of card the player lays this turn. Blueprints and rubbish are not courses — a sheet
    // you accept is not a stone you set. 1 is a deed, 2 a working, 3 anything else: three kinds of stone, and
    // at the turn's end only whether the two numbers differ.
    private static EffectProgram<CardPlayedTriggeredEffectContext> CountTheCourses()
    {
        var architect = Bearer(ImpossiblePyramidId);
        var laid = new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(architect, CoursesThisTurn);

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Is(string tag) =>
            new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag));

        IEffectNode<CardPlayedTriggeredEffectContext> Note(CounterId slot, int kind) =>
            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                architect, slot, new ConstantExpression<CardPlayedTriggeredEffectContext>(kind),
                relative: false);

        IEffectNode<CardPlayedTriggeredEffectContext> Record(CounterId slot, int at) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    laid, ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(at)),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    Is(Cards.CardAuthoring.DeedTag),
                    Note(slot, 1),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Is(Cards.CardAuthoring.WorkingTag), Note(slot, 2), Note(slot, 3))));

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new OrExpression<CardPlayedTriggeredEffectContext>(
                            Is(Cards.CardAuthoring.JunkTag), Is(BlueprintTag)))),
                // Causal: the slot is chosen from the count this very card just made.
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        architect, CoursesThisTurn,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                    Record(FirstCourse, 1),
                    Record(SecondCourse, 2),
                ])));
    }

    // ── the judgment ──────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<TurnEndedTriggeredEffectContext> JudgeTheBlueprint()
    {
        var architect = Bearer(ImpossiblePyramidId);

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> Accepted(string blueprintId) =>
            new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                architect, new StatusDefinitionId(blueprintId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Counter(CounterId counter) =>
            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(architect, counter);

        var spent = new ResourceSpentThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant);

        var deeds = new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
            Applicant, new TagId(Cards.CardAuthoring.DeedTag));

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> Exactly(
            ICombatExpression<TurnEndedTriggeredEffectContext, int> value,
            ICombatExpression<TurnEndedTriggeredEffectContext, int> figure) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(value, ComparisonOperator.Equal, figure);

        // Alternating Stone: two courses laid, both of a known kind, and the kinds not the same.
        var alternated = new AndExpression<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Counter(SecondCourse), ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Counter(FirstCourse), ComparisonOperator.NotEqual, Counter(SecondCourse)));

        var laidTrue = new OrExpression<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                Accepted(BlueprintFoundationId),
                Exactly(spent, new ConstantExpression<TurnEndedTriggeredEffectContext>(2))),
            new OrExpression<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    Accepted(BlueprintCoursesId),
                    Exactly(deeds, new ConstantExpression<TurnEndedTriggeredEffectContext>(2))),
                new OrExpression<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Accepted(BlueprintAlternatingId), alternated),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Accepted(BlueprintCourseId),
                        Exactly(spent,
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                architect, new StatusDefinitionId(BlueprintCourseId)))))));

        // A course laid true: the monument falls a step, and every second one of them is 8 of his own blood
        // and 8 of his cover. Direct HP loss, not damage — succeeding does more than slow the clock.
        var trueCourse = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            StepDown<TurnEndedTriggeredEffectContext>(architect),

            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                architect, CoursesLaid,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        Counter(CoursesLaid), new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new DealDamageNode<TurnEndedTriggeredEffectContext>(
                        architect,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(CorrectedCourseLoss),
                        ignoresBlock: true, kind: DamageKind.DamageOverTime),
                    new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                        architect, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-CorrectedCourseLoss)),
                ])),
        ]);

        // …and a course missed, each sheet in its own coin.
        var missedCourse = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Accepted(BlueprintFoundationId),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    StepUp<TurnEndedTriggeredEffectContext>(architect),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(EntombedId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: architect),
                ])),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Accepted(BlueprintCoursesId),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(BurdenedId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: architect)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Accepted(BlueprintAlternatingId),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(2), sourceSelector: architect)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Accepted(BlueprintCourseId), StepUp<TurnEndedTriggeredEffectContext>(architect)),
        ]);

        var anyBlueprint = Blueprints
            .Select(id => (ICombatExpression<TurnEndedTriggeredEffectContext, bool>)Accepted(id))
            .Aggregate((a, b) => new OrExpression<TurnEndedTriggeredEffectContext>(a, b));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                // The player's turn: the sheet is answered.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        PlayersTurn<TurnEndedTriggeredEffectContext>(), anyBlueprint),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            laidTrue, trueCourse, missedCourse),

                        // …and in the second half the project takes a further step whatever was resolved.
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                                architect, new StatusDefinitionId(PlanAlwaysCorrectId)),
                            StepUp<TurnEndedTriggeredEffectContext>(architect)),
                    ])),

                // His own turn: the schedule, which nothing stops.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ImpossiblePyramidId)),
                    StepUp<TurnEndedTriggeredEffectContext>(architect)),
            ]));
    }

    private static IEffectNode<TContext> StepUp<TContext>(ICombatantTargetSelector architect)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(
                    architect, new StatusDefinitionId(MonumentId)),
                ComparisonOperator.Less, new ConstantExpression<TContext>(MonumentCap)),
            new ApplyStatusNode<TContext>(
                architect, new StatusDefinitionId(MonumentId), new ConstantExpression<TContext>(1),
                sourceSelector: architect));

    private static IEffectNode<TContext> StepDown<TContext>(ICombatantTargetSelector architect)
        where TContext : class =>
        new ModifyStatusStacksNode<TContext>(
            architect, new StatusDefinitionId(MonumentId), new ConstantExpression<TContext>(-1));

    private static IEffectNode<TContext> SetMonument<TContext>(ICombatantTargetSelector architect, int steps)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(architect, new StatusDefinitionId(MonumentId)),
            new ApplyStatusNode<TContext>(
                architect, new StatusDefinitionId(MonumentId), new ConstantExpression<TContext>(steps),
                sourceSelector: architect),
        ]);

    // ── the transition ────────────────────────────────────────────────────────────────────────────────────

    // The plan was always correct. It arrives either behind the first Capstone or at 320, whichever the
    // player reaches first — and it is not an attack: the monument is preserved, the pending sheet is cleared
    // off the table, and he stands behind 18.
    private static IEffectNode<TContext> ThePlanWasAlwaysCorrect<TContext>(ICombatantTargetSelector architect)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new NotExpression<TContext>(
                new TargetHasStatusExpression<TContext>(
                    architect, new StatusDefinitionId(PlanAlwaysCorrectId))),
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(
                    architect, new StatusDefinitionId(PlanAlwaysCorrectId),
                    new ConstantExpression<TContext>(1), sourceSelector: architect),

                .. Blueprints.Select(id =>
                    (IEffectNode<TContext>)
                    new RemoveStatusNode<TContext>(architect, new StatusDefinitionId(id))),

                new GainBlockNode<TContext>(architect, new ConstantExpression<TContext>(TransitionBlock)),
            ]));

    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheFailsafe()
    {
        var architect = Bearer(ImpossiblePyramidId);

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                    new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(architect),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(PlanCorrectAt)),
                ThePlanWasAlwaysCorrect<DamageReceivedTriggeredEffectContext>(architect)));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // Night Shift on the Ramp: he simply builds, and stands behind what he built.
    private static EffectProgram<EnemyActionContext> NightShift() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            StepUp<EnemyActionContext>(CombatantTargetSelectors.Source),
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(18)),
        ]));

    // Six steps stand, and the capstone comes down on the room. Nothing else resolves in that window — and
    // the first one of them is what makes the plan retroactively correct.
    private static EffectProgram<EnemyActionContext> CapstoneDescends()
    {
        var self = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(Applicant, Const(GreatCapstoneDamage)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(EntombedId), Const(2)),
                new GainBlockNode<EnemyActionContext>(self, Const(CapstoneBlock)),

                ThePlanWasAlwaysCorrect<EnemyActionContext>(self),

                SetMonument<EnemyActionContext>(self, CapstoneResets),
            ]));
    }

    // …and the one he came to place. 36, and two for every course standing, to a ceiling of 48.
    private static EffectProgram<EnemyActionContext> EternalCapstone()
    {
        var self = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new MinExpression<EnemyActionContext>(
                        Const(48),
                        new AddExpression<EnemyActionContext>(
                            Const(36),
                            new MultiplyExpression<EnemyActionContext>(
                                Const(2),
                                new CombatantStatusStacksExpression<EnemyActionContext>(
                                    self, new StatusDefinitionId(MonumentId)))))),

                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(EntombedId), Const(1)),

                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(EternalCapstoneId), Const(1)),

                SetMonument<EnemyActionContext>(self, EternalResets),
            ]));
    }

    // ── the sheets ────────────────────────────────────────────────────────────────────────────────────────

    // A blueprint card costs nothing and does nothing but ACCEPT — and only the first one accepted governs,
    // so laying both sheets does not buy two chances.
    private static CardData BlueprintCard(
        string id, string name, string text, string blueprintId, bool demanded)
    {
        var architect = Bearer(ImpossiblePyramidId);
        var standing = Blueprints
            .Select(other => (ICombatExpression<CardPlayContext, bool>)
                new TargetHasStatusExpression<CardPlayContext>(
                    architect, new StatusDefinitionId(other)))
            .Aggregate((a, b) => new OrExpression<CardPlayContext>(a, b));

        ICombatExpression<CardPlayContext, int> stacks = demanded
            ? new CombatantCounterExpression<CardPlayContext>(architect, CourseDemand)
            : new ConstantExpression<CardPlayContext>(1);

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId(BlueprintTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new NotExpression<CardPlayContext>(standing),
                    new ApplyStatusNode<CardPlayContext>(
                        architect, new StatusDefinitionId(blueprintId), stacks,
                        sourceSelector: architect))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
