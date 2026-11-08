using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// Act IV's words since the rework of 2026-11-04 (user). Burdened stays; Weighed, Paperwork and Entombed leave the
// normal bodies; three new words carry them:
//
//   The Scale   at the end of your turn a weighing body counts your Deeds against your Workings (Rites do not
//               count). Exactly as many, at least one of each: a Feather (your next attack deals 5 more).
//               Anything else: 1 Burdened. The pans fill live on the body as you play.
//   The Jars    every single normal fight opens with one or two canopic jars, drawn at random from eight. A jar
//               is its own small body; it gives its master a power while it stands and falls with its master.
//   Risen       at its first death a body is Downed instead (the engine's death prevention, at 1 HP, so no
//               victory is ever called early): it takes no damage, and its next action is to rise — 40 % of
//               its health, every debuff gone — and nothing else that turn (user, 2026-11-04).
public static partial class ActFour
{
    // ── ids ───────────────────────────────────────────────────────────────────────────────────────────────

    // The Scale, and the variants that weigh differently. Every weighing body wears WeighsYourTurnId.
    public const string WeighsYourTurnId = "weighs_your_turn";
    public const string TheScaleId = "the_scale";
    public const string CrookedScaleId = "the_crooked_cubit";
    public const string JudgesScaleId = "the_forty_second_question";
    public const string EclipseScaleId = "the_eclipse";
    public const string ChiselScaleId = "the_chisel";
    public const string ThothsScaleId = "thoths_ledger";
    public const string DeedsPanId = "deeds_on_the_scale";
    public const string WorkingsPanId = "workings_on_the_scale";
    public const string WantsDeedsId = "cubit_wants_deeds";
    public const string WantsWorkingsId = "cubit_wants_workings";
    public const string ChiselDeedsId = "chisel_on_deeds";
    public const string ChiselWorkingsId = "chisel_on_workings";
    public const string AcquittalId = "acquittal";
    public const string InkShelterId = "ink_shelter";
    public const string ScaleFeatherId = "scale_feather";
    public const string ScaleProcedureId = "weighed_at_turn_end";

    // Risen.
    public const string RisingId = "rising";
    public const string RisenDownedId = "risen_downed";
    public const string CapstonePendingId = "capstone_pending";
    public const string DoorPendingId = "door_pending";
    public const string RisenId = "risen";
    public const string RisenGolemId = "risen_golem";
    public const string RisenDoorId = "risen_door";
    public const string RisenFarmerId = "risen_farmer";
    public const string RisenFarmerLastId = "risen_farmer_last";
    public const string RisenByKaId = "risen_by_ka";
    public const string RisenKeeperId = "risen_keeper";
    public const string CapstoneDueId = "capstone_due";
    public const string ThroughTheDoorId = "through_the_door";

    public const int RisePercent = 40;
    public const int FarmerRisePercent = 25;
    public const int FeatherDamage = 5;

    // The jars.
    public const string CanopicRiteId = "canopic_rite";
    public const string CanopicJarId = "canopic_jar";
    public const string CanopicJarEnemyId = "canopic_jar";
    public const int JarHealth = 14;

    public sealed record Jar(string Key, string Name, string Text);

    // The eight, in the order the dice count them. Each jar body wears jar_<key>; its master wears jar_power_<key>.
    public static readonly IReadOnlyList<Jar> Jars =
    [
        new("liver", "Liver Jar", "While it stands, its master heals 4 at the end of its turn."),
        new("blood", "Blood Jar", "While it stands, its master's attacks deal 3 more."),
        new("lung", "Lung Jar", "While it stands, its master gains 6 Block at the end of its turn."),
        new("breath", "Breath Jar", "While it stands, your first hit on its master each turn deals 5 less."),
        new("stomach", "Stomach Jar", "While it stands, each attack of its master gives you 1 Burdened."),
        new("hunger", "Hunger Jar", "While it stands, each Burdened surcharge you pay heals its master 3, on its turn."),
        new("gut", "Gut Jar", "While it stands, a crooked scale gives you 2 Burdened instead of 1."),
        new("ka", "Ka Jar", "While it stands, its master rises once more: at death it sheds its debuffs and returns with 40 % HP."),
    ];

    public static string JarBodyId(string key) => $"jar_{key}";
    public static string JarPowerId(string key) => key == "ka" ? RisenByKaId : $"jar_power_{key}";

    private static CounterId JarRollFirst => new("jar_roll_first");
    private static CounterId JarRollSecond => new("jar_roll_second");
    private static CounterId JarRollCount => new("jar_roll_count");
    private static CounterId JarsSet => new("jars_set");
    private static CounterId HungerRead => new("hunger_read");

    // The bodies that weigh, and the single-fight bodies that open with jars.
    public static readonly IReadOnlySet<string> Weighers = new HashSet<string>(StringComparer.Ordinal)
    {
        "reed_cord_surveyor", "crooked_rod_bearer", "assessor_of_the_forty_two", "eclipse_scarab",
        "name_erasing_chisel_spirit", "moon_cycle_ibis",
    };

    // ── what the act serves on the player ─────────────────────────────────────────────────────────────────

    // Asked of the whole roster: a weighing body anywhere puts the procedure on the player once, and a single
    // normal fight (never a multi-fight, a mimic, an elite or a boss) opens with the jars.
    public static IReadOnlyList<StartingStatusSpec> HeroOpening(IReadOnlyList<string> roster, string? role) =>
    [
        .. roster.Any(Weighers.Contains)
            ? [new StartingStatusSpec(new StatusDefinitionId(ScaleProcedureId), 1)]
            : Array.Empty<StartingStatusSpec>(),
        .. role == "combat" && roster.Count == 1 && Identities.Contains(roster[0])
            ? [new StartingStatusSpec(new StatusDefinitionId(CanopicRiteId), 1)]
            : Array.Empty<StartingStatusSpec>(),
        // Ammit lays one Feather of Truth down herself, so even a player who carries none has one answer to her.
        .. roster.Contains(AmmitEnemyId)
            ? [new StartingStatusSpec(new StatusDefinitionId(FeatherOfTruthId), 1)]
            : Array.Empty<StartingStatusSpec>(),
    ];

    private static ICombatantTargetSelector WeighingBodies { get; } =
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(WeighsYourTurnId));

    private static ICombatantTargetSelector LivingJars { get; } =
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(CanopicJarId));

    private static ICombatExpression<TContext, int> StacksOn<TContext>(ICombatantTargetSelector who, string status)
        where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(status));

    private static ICombatExpression<TContext, bool> Wears<TContext>(ICombatantTargetSelector who, string status)
        where TContext : class =>
        new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(status));

    private static ICombatExpression<TContext, bool> Eq<TContext>(ICombatExpression<TContext, int> a, ICombatExpression<TContext, int> b)
        where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Equal, b);

    private static ICombatExpression<TContext, bool> Above<TContext>(ICombatExpression<TContext, int> a, int b)
        where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Greater, N<TContext>(b));

    private static ICombatExpression<TContext, bool> AllOf<TContext>(params ICombatExpression<TContext, bool>[] terms)
        where TContext : class =>
        terms.Aggregate((a, b) => new AndExpression<TContext>(a, b));

    // ── the Scale ─────────────────────────────────────────────────────────────────────────────────────────

    // The player's side of every weighing: the pans fill as cards are played, the weighing happens as the turn
    // ends, and the pans empty (and the Cubit and the Chisel turn) as the next one begins. One procedure on the
    // player, walking every weighing body, so two scales in one room each weigh the same turn.
    private static StatusData ScaleProcedure()
    {
        var body = CombatantTargetSelectors.IterationTarget;

        IEffectNode<CardPlayedTriggeredEffectContext> Pan(string tag, string pan, string chisel) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)),
                new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(WeighingBodies,
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Wears<CardPlayedTriggeredEffectContext>(body, chisel),
                        new ApplyStatusNode<CardPlayedTriggeredEffectContext>(body, new StatusDefinitionId(pan), N<CardPlayedTriggeredEffectContext>(2)),
                        @else: new ApplyStatusNode<CardPlayedTriggeredEffectContext>(body, new StatusDefinitionId(pan), N<CardPlayedTriggeredEffectContext>(1)))));
        var play = new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Pan(CardAuthoring.DeedTag, DeedsPanId, ChiselDeedsId),
            Pan(CardAuthoring.WorkingTag, WorkingsPanId, ChiselWorkingsId),
        ]);

        // Odd rounds the Cubit asks for one Deed more and the Chisel bites the Deeds; even rounds the Workings.
        var odd = Eq<TurnStartedTriggeredEffectContext>(
            new RemainderExpression<TurnStartedTriggeredEffectContext>(
                new RoundNumberExpression<TurnStartedTriggeredEffectContext>(), N<TurnStartedTriggeredEffectContext>(2)),
            N<TurnStartedTriggeredEffectContext>(1));
        IEffectNode<TurnStartedTriggeredEffectContext> Turn(string rule, string oddMark, string evenMark) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                Wears<TurnStartedTriggeredEffectContext>(body, rule),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(oddMark)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(evenMark)),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(odd,
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(oddMark), N<TurnStartedTriggeredEffectContext>(1)),
                        @else: new ApplyStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(evenMark), N<TurnStartedTriggeredEffectContext>(1))),
                ]));
        var morning = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            IsPlayersTurn<TurnStartedTriggeredEffectContext>(),
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(WeighingBodies,
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(DeedsPanId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(WorkingsPanId)),
                    Turn(CrookedScaleId, WantsDeedsId, WantsWorkingsId),
                    Turn(ChiselScaleId, ChiselDeedsId, ChiselWorkingsId),
                ])));

        // The weighing.
        var deeds = StacksOn<TurnEndedTriggeredEffectContext>(body, DeedsPanId);
        var workings = StacksOn<TurnEndedTriggeredEffectContext>(body, WorkingsPanId);
        var one = N<TurnEndedTriggeredEffectContext>(1);
        var even = AllOf(Eq(deeds, workings), Above(deeds, 0));
        var crookedTrue = new OrExpression<TurnEndedTriggeredEffectContext>(
            AllOf(Wears<TurnEndedTriggeredEffectContext>(body, WantsDeedsId),
                Eq<TurnEndedTriggeredEffectContext>(deeds, new AddExpression<TurnEndedTriggeredEffectContext>(workings, one))),
            AllOf(Wears<TurnEndedTriggeredEffectContext>(body, WantsWorkingsId),
                Eq<TurnEndedTriggeredEffectContext>(workings, new AddExpression<TurnEndedTriggeredEffectContext>(deeds, one))));
        var crooked = Wears<TurnEndedTriggeredEffectContext>(body, CrookedScaleId);
        var balanced = new OrExpression<TurnEndedTriggeredEffectContext>(
            AllOf(crooked, crookedTrue),
            AllOf<TurnEndedTriggeredEffectContext>(new NotExpression<TurnEndedTriggeredEffectContext>(crooked), even));

        var gut = Above(new CountTargetsExpression<TurnEndedTriggeredEffectContext>(
            CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(JarPowerId("gut")))), 0);
        var weightOnYou = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(gut,
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Applicant, new StatusDefinitionId(BurdenedId), N<TurnEndedTriggeredEffectContext>(2), sourceSelector: body),
            @else: new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Applicant, new StatusDefinitionId(BurdenedId), one, sourceSelector: body));

        IEffectNode<TurnEndedTriggeredEffectContext> If(string rule, IEffectNode<TurnEndedTriggeredEffectContext> then) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Wears<TurnEndedTriggeredEffectContext>(body, rule), then);

        var trueTurn = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Applicant, new StatusDefinitionId(ScaleFeatherId), one),
            If(JudgesScaleId, new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(body, new StatusDefinitionId(AcquittalId), one),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        StacksOn<TurnEndedTriggeredEffectContext>(body, AcquittalId), ComparisonOperator.GreaterOrEqual, N<TurnEndedTriggeredEffectContext>(JudgeAcquits)),
                    new SetCombatantLifecycleStateNode<TurnEndedTriggeredEffectContext>(body, CombatantLifecycleState.Downed)),
            ])),
        ]);
        var crookedTurn = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            weightOnYou,
            If(JudgesScaleId, new RemoveStatusNode<TurnEndedTriggeredEffectContext>(body, new StatusDefinitionId(AcquittalId))),
            If(EclipseScaleId, new ApplyStatusNode<TurnEndedTriggeredEffectContext>(body, new StatusDefinitionId(ApproachOfNoonId), one)),
            If(ThothsScaleId, new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Parties,
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(InkShelterId), one))),
        ]);
        var weigh = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            IsPlayersTurn<TurnEndedTriggeredEffectContext>(),
            new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(WeighingBodies,
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(balanced, trueTurn, @else: crookedTurn)));

        return Face(ScaleProcedureId, "Weighed",
            "At the end of your turn each Scale counts your Deeds and Workings. Equal: a Feather. Otherwise: 1 Burdened.",
            triggers:
            [
                On("CardPlayed", play, StatusTriggerScope.Bearer),
                On("TurnStarted", morning, StatusTriggerScope.Bearer),
                On("TurnEnded", weigh, StatusTriggerScope.Bearer),
            ]);
    }

    public const int JudgeAcquits = 3;

    // The token a true turn earns: the next attack is 5 heavier, and one is spent per attack.
    private static StatusData ScaleFeather() => new()
    {
        Id = ScaleFeatherId,
        NameKey = "Feather",
        DescriptionKey = "Your next attack deals 5 more damage. One Feather is spent per attack.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, FeatherDamage,
                RestrictDamageKind: DamageKind.Direct, OncePerAction: true),
        ],
        Triggers =
        [
            On("DamageDealt", new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                new ClaimOnceThisActionExpression<DamageDealtTriggeredEffectContext>("scale_feather.spent"),
                new ModifyStatusStacksNode<DamageDealtTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(ScaleFeatherId), N<DamageDealtTriggeredEffectContext>(-1))),
                StatusTriggerScope.Bearer),
        ],
    };

    // Thoth's ink: a crooked turn shelters every body: 6 Block at the end of its own turn (Block gained on your
    // turn would be cleared before it ever stood between you and them).
    private static StatusData InkShelter() => Face(InkShelterId, "Ink Shelter",
        "Thoth's Ibis saw a crooked scale: 6 Block at the end of this body's turn.", StatusPolarity.Buff,
        triggers:
        [
            On("TurnEnded", new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new GainBlockNode<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, N<TurnEndedTriggeredEffectContext>(6)),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(InkShelterId)),
            ]), StatusTriggerScope.Bearer),
        ]);

    private static IEnumerable<StatusData> ScaleStatuses() =>
    [
        ScaleProcedure(),
        ScaleFeather(),
        InkShelter(),
        Face(WeighsYourTurnId, "Weighs Your Turn", "This body weighs your turn on its Scale."),
        Face(TheScaleId, "The Scale",
            "At the end of your turn: as many Deeds as Workings (one each at least) is a Feather; anything else is 1 Burdened."),
        Face(CrookedScaleId, "The Crooked Cubit",
            "Its scale is crooked: it wants one Deed more, or one Working more, as its mark says. Anything else is 1 Burdened."),
        Face(JudgesScaleId, "The Forty-Second Question",
            "Weighs like the Scale. Three balanced turns in a row and it acquits you and leaves."),
        Face(EclipseScaleId, "The Eclipse",
            "Weighs like the Scale. Every crooked turn moves the eclipse 1; at 3 comes Black Noon."),
        Face(ChiselScaleId, "The Chisel",
            "Weighs like the Scale, but each round it chisels one pan: those cards count double."),
        Face(ThothsScaleId, "Thoth's Ledger",
            "Weighs like the Scale. A crooked turn gives every enemy 6 Block at the end of its turn."),
        Face(DeedsPanId, "Deeds on the Scale", "The Deeds you played this turn, as this Scale counts them.", stacks: true),
        Face(WorkingsPanId, "Workings on the Scale", "The Workings you played this turn, as this Scale counts them.", stacks: true),
        Face(WantsDeedsId, "Wants One Deed More", "This turn the Cubit is satisfied by exactly one Deed more than Workings."),
        Face(WantsWorkingsId, "Wants One Working More", "This turn the Cubit is satisfied by exactly one Working more than Deeds."),
        Face(ChiselDeedsId, "Chisel on the Deeds", "This round each Deed counts twice on its Scale."),
        Face(ChiselWorkingsId, "Chisel on the Workings", "This round each Working counts twice on its Scale."),
        Face(AcquittalId, "Acquittal", "Balanced turns in a row. At 3 it acquits you and leaves.", stacks: true),
    ];

    // ── Risen ─────────────────────────────────────────────────────────────────────────────────────────────

    private static InterceptorEffectData Put(string status, int stacks = 1) =>
        new(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), stacks, status, 0, StatusPolarity.Neutral);

    private static StatusData RisenStatus(string id, string name, string text, IReadOnlyList<InterceptorEffectData> effects,
        bool repeating = false) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
            DeathPrevention = new StatusDeathPreventionData(1, effects, Repeating: repeating),
        };

    // Every prevention leaves the body Downed at 1 HP, with how far it will rise (Rising, in percent) and what it
    // rises with (a pending mark); the rise itself is the body's next action (RiseAction).
    private static InterceptorEffectData[] Fall(int percent, params string[] pending) =>
        [Put(RisenDownedId), Put(RisingId, percent), .. pending.Select(p => Put(p))];

    private static IEnumerable<StatusData> RisenStatuses() =>
    [
        RisenStatus(RisenId, "Rises Again",
            "At its first death it is Downed; on its turn it rises with 40 % of its HP, free of debuffs.", Fall(RisePercent)),
        RisenStatus(RisenGolemId, "Rises Under the Capstone",
            "At its first death it is Downed; on its turn it rises with 40 % of its HP, and its next action is the Fall of the Pyramidion.",
            Fall(RisePercent, CapstonePendingId)),
        RisenStatus(RisenDoorId, "Through the False Door",
            "At its first death it is Downed; on its turn it returns with 40 % of its HP, 20 Block and 3 more on its attacks.",
            Fall(RisePercent, DoorPendingId)),
        // Not "Rises Twice": that is the Restless King's, and two bodies of one act must not share a rule's name.
        RisenStatus(RisenFarmerId, "Twice from the Silt",
            "Twice it is Downed and rises again from the silt on its turn, each time with 25 % of its HP.",
            [.. Fall(FarmerRisePercent), Put(RisenFarmerLastId)]),
        RisenStatus(RisenFarmerLastId, "Once More from the Silt",
            "Downed once more, it rises from the silt one last time on its turn, with 25 % of its HP.", Fall(FarmerRisePercent)),
        RisenStatus(RisenByKaId, "Ka Jar",
            "While its Ka Jar stands it rises once more: Downed at death, it rises on its turn with 40 % of its HP.",
            Fall(RisePercent)),
        RisenStatus(RisenKeeperId, "Keeper of the Jars",
            "While one of its jars stands (in a duo: while its ally lives) every death only Downs it; it rises on its turn with 40 %.",
            Fall(RisePercent), repeating: true),
        new StatusData
        {
            Id = RisenDownedId,
            NameKey = "Downed",
            DescriptionKey = "Downed: takes no damage. Rises at the start of its turn.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 0,
                    Priority: 1000, RestrictDamageKind: null),
            ],
            Triggers = [],
        },
        Face(RisingId, "Rising", "How much of its HP it rises with, in percent.", stacks: true),
        Face(CapstonePendingId, "Capstone Waiting", "After it rises, its next action is the Fall of the Pyramidion."),
        Face(DoorPendingId, "Door Waiting", "It returns with 20 Block and 3 more on its attacks."),
        Face(CapstoneDueId, "Capstone Due", "It has risen: its next action is the Fall of the Pyramidion."),
        Face(ThroughTheDoorId, "Through the Door", "It came back through the false door: its attacks deal 3 more.",
            StatusPolarity.Buff, modifiers:
            [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, 3, RestrictDamageKind: DamageKind.Direct)]),
    ];

    // The rise, as the Downed body's whole action that turn (an intent rule above every other picks it while it
    // is Downed): health to its share of the maximum, every debuff gone, Downed gone, and what it rises with.
    // It does nothing else that turn; its next action comes after your following turn.
    public static EffectProgram<EnemyActionContext> RiseAction()
    {
        var self = CombatantTargetSelectors.Source;
        IEffectNode<EnemyActionContext> Pending(string mark, params IEffectNode<EnemyActionContext>[] then) =>
            new ConditionalEffectNode<EnemyActionContext>(Wears<EnemyActionContext>(self, mark),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    .. then,
                    new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(mark)),
                ]));
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new SetHealthNode<EnemyActionContext>(self,
                new MaxExpression<EnemyActionContext>(Const(1),
                    new DivideExpression<EnemyActionContext>(
                        new MultiplyExpression<EnemyActionContext>(
                            new CombatantMaxHealthExpression<EnemyActionContext>(self),
                            StacksOn<EnemyActionContext>(self, RisingId)),
                        Const(100)))),
            new RemoveStatusesByPolarityNode<EnemyActionContext>(self, StatusPolarity.Debuff),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(RisingId)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(RisenDownedId)),
            Pending(CapstonePendingId, new ApplyStatusNode<EnemyActionContext>(self, new StatusDefinitionId(CapstoneDueId), Const(1))),
            Pending(DoorPendingId,
                new ApplyStatusNode<EnemyActionContext>(self, new StatusDefinitionId(ThroughTheDoorId), Const(1)),
                new GainBlockNode<EnemyActionContext>(self, Const(20))),
            // The Restless King (elite) rises with a random jar's power.
            Pending(KingPendingId, KingTakesAJar()),
        ]));
    }

    // ── the jars ──────────────────────────────────────────────────────────────────────────────────────────

    // The rite on the player: at the first hand of the fight one or two jars, drawn at random and never the same
    // twice, are set beside the one body in the room (the Canopic Keeper always takes two).
    private static StatusData CanopicRite()
    {
        var player = CombatantTargetSelectors.Source;
        var master = CombatantTargetSelectors.FirstTarget(Parties);
        ICombatExpression<CardsDrawnTriggeredEffectContext, int> Counter(CounterId c) =>
            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(player, c);
        IEffectNode<CardsDrawnTriggeredEffectContext> Roll(CounterId c, ICombatExpression<CardsDrawnTriggeredEffectContext, int> value) =>
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(player, c, value, relative: false);

        IEffectNode<CardsDrawnTriggeredEffectContext> Summon(int index)
        {
            var jar = Jars[index];
            return new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SummonCombatantNode<CardsDrawnTriggeredEffectContext>(
                    StandardCombatIds.EnemyTeam, N<CardsDrawnTriggeredEffectContext>(JarHealth),
                    new CombatantDefinitionId(CanopicJarEnemyId), jar.Name,
                    startingStatuses:
                    [
                        new StatusGrant(new StatusDefinitionId(CanopicJarId), 1),
                        new StatusGrant(new StatusDefinitionId(JarBodyId(jar.Key)), 1),
                    ]),
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(master, new StatusDefinitionId(JarPowerId(jar.Key)),
                    N<CardsDrawnTriggeredEffectContext>(1)),
            ]);
        }

        var keeper = Wears<CardsDrawnTriggeredEffectContext>(master, KeeperRuleId);
        var set = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            AllOf(Eq(Counter(JarsSet), N<CardsDrawnTriggeredEffectContext>(0)), Standing<CardsDrawnTriggeredEffectContext>(master)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                Roll(JarsSet, N<CardsDrawnTriggeredEffectContext>(1)),
                Roll(JarRollFirst, new RandomBelowExpression<CardsDrawnTriggeredEffectContext>(Jars.Count)),
                // A second, different jar: the first plus 1 to 7, around the shelf.
                Roll(JarRollSecond, new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                    new AddExpression<CardsDrawnTriggeredEffectContext>(
                        new AddExpression<CardsDrawnTriggeredEffectContext>(Counter(JarRollFirst), N<CardsDrawnTriggeredEffectContext>(1)),
                        new RandomBelowExpression<CardsDrawnTriggeredEffectContext>(Jars.Count - 1)),
                    N<CardsDrawnTriggeredEffectContext>(Jars.Count))),
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(keeper,
                    Roll(JarRollCount, N<CardsDrawnTriggeredEffectContext>(1)),
                    @else: Roll(JarRollCount, new RandomBelowExpression<CardsDrawnTriggeredEffectContext>(2))),
                .. Enumerable.Range(0, Jars.Count).Select(i => (IEffectNode<CardsDrawnTriggeredEffectContext>)
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Eq(Counter(JarRollFirst), N<CardsDrawnTriggeredEffectContext>(i)), Summon(i))),
                .. Enumerable.Range(0, Jars.Count).Select(i => (IEffectNode<CardsDrawnTriggeredEffectContext>)
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        AllOf(Eq(Counter(JarRollSecond), N<CardsDrawnTriggeredEffectContext>(i)),
                            Eq(Counter(JarRollCount), N<CardsDrawnTriggeredEffectContext>(1))),
                        Summon(i))),
            ]));

        return Face(CanopicRiteId, "Canopic Jars",
            "One or two jars stand beside it, each giving it a power. Break a jar and its power is gone.",
            triggers: [On("CardsDrawn", set, StatusTriggerScope.Bearer)]);
    }

    // What the master wears, one per jar — the Ka's is its own Risen status.
    private static IEnumerable<StatusData> JarStatuses()
    {
        yield return CanopicRite();
        yield return Face(CanopicJarId, "Canopic Jar", "A jar beside its master. It does not act; it falls with its master.");
        foreach (var jar in Jars)
            yield return Face(JarBodyId(jar.Key), jar.Name, jar.Text);

        var self = CombatantTargetSelectors.Source;
        yield return Face(JarPowerId("liver"), "Liver Jar", "Its Liver Jar stands: it heals 4 at the end of its turn.", StatusPolarity.Buff,
            triggers: [On("TurnEnded", new HealNode<TurnEndedTriggeredEffectContext>(self, N<TurnEndedTriggeredEffectContext>(4)), StatusTriggerScope.Bearer)]);
        yield return Face(JarPowerId("blood"), "Blood Jar", "Its Blood Jar stands: its attacks deal 3 more.", StatusPolarity.Buff,
            modifiers: [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, 3, RestrictDamageKind: DamageKind.Direct)]);
        yield return Face(JarPowerId("lung"), "Lung Jar", "Its Lung Jar stands: it gains 6 Block at the end of its turn.", StatusPolarity.Buff,
            triggers: [On("TurnEnded", new GainBlockNode<TurnEndedTriggeredEffectContext>(self, N<TurnEndedTriggeredEffectContext>(6)), StatusTriggerScope.Bearer)]);
        yield return Face(JarPowerId("breath"), "Breath Jar", "Its Breath Jar stands: your first hit on it each turn deals 5 less.", StatusPolarity.Buff,
            modifiers: [new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.AddFlat, -5, OncePerTurn: true)]);
        yield return Face(JarPowerId("stomach"), "Stomach Jar", "Its Stomach Jar stands: each of its attacks gives you 1 Burdened.", StatusPolarity.Buff,
            triggers:
            [
                On("DamageDealt", new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                    new ClaimOnceThisActionExpression<DamageDealtTriggeredEffectContext>("jar_power_stomach.fed"),
                    new ApplyStatusNode<DamageDealtTriggeredEffectContext>(Applicant, new StatusDefinitionId(BurdenedId),
                        N<DamageDealtTriggeredEffectContext>(1), sourceSelector: self)), StatusTriggerScope.Bearer),
            ]);
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(self, BurdenPaid, HungerRead);
        yield return Face(JarPowerId("hunger"), "Hunger Jar",
            "Its Hunger Jar stands: on its turn it heals 3 for each Burdened surcharge you paid since.", StatusPolarity.Buff,
            triggers:
            [
                On("TurnStarted", new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(Above(unread, 0),
                        new HealNode<TurnStartedTriggeredEffectContext>(self,
                            new MultiplyExpression<TurnStartedTriggeredEffectContext>(unread, N<TurnStartedTriggeredEffectContext>(3)))),
                    MoveTheBookmark<TurnStartedTriggeredEffectContext>(self, BurdenPaid, HungerRead),
                ]), StatusTriggerScope.Bearer),
            ]);
        yield return Face(JarPowerId("gut"), "Gut Jar", "Its Gut Jar stands: a crooked scale gives you 2 Burdened instead of 1.", StatusPolarity.Buff);
    }

    // ── what falls with what ──────────────────────────────────────────────────────────────────────────────
    //
    // Downings are heard as ENCOUNTER triggers (a bearer-scoped Downed on the downed body's own status is not
    // reliably heard, ActTwo.BreakTheJar), and the guards read `sourceIncludingDowned`. Every Act-IV normal body
    // brings the same set; each is idempotent, so a duo carrying them twice changes nothing.
    //
    // The Western Gate's and the Excavator's rules are NOT idempotent (a husk is summoned, Gold is counted), so they
    // come only with their own body: a duo carrying the shared set twice must not summon two husks.
    public static IReadOnlyList<EncounterTriggerData> LabyrinthTriggers(string? enemyId = null)
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        ICombatExpression<CombatantDownedTriggeredEffectContext, bool> Was(string status) =>
            Wears<CombatantDownedTriggeredEffectContext>(downed, status);
        IEffectNode<CombatantDownedTriggeredEffectContext> TakeFromAll(string status) =>
            new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(status)),
                new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(status)));

        var keepers = CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(KeeperRuleId));
        var othersBesideTheKeeper = CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(KeeperRuleId));

        var program = new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
        [
            // A jar breaks: its power leaves its master.
            .. Jars.Select(jar => (IEffectNode<CombatantDownedTriggeredEffectContext>)
                new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(Was(JarBodyId(jar.Key)), TakeFromAll(JarPowerId(jar.Key)))),
            // The master falls: its jars fall with it.
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(Was(LabyrinthBodyId),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(LivingJars,
                    new SetHealthNode<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, N<CombatantDownedTriggeredEffectContext>(0)))),
            // The Western Gate: a fallen ally leaves a husk for the Jackal to guide west; the husk or the Jackal
            // falling ends it (ActFourNormals).
            .. enemyId == "jackal_of_the_western_gate" ? [WesternGateDowned()] : Array.Empty<IEffectNode<CombatantDownedTriggeredEffectContext>>(),
            // The Robber falls: killed, its loot is yours (as a card, after the fight); fled, your Gold goes with it.
            .. enemyId == "cursed_loot_bearer" ? [TombRobberDowned()] : Array.Empty<IEffectNode<CombatantDownedTriggeredEffectContext>>(),
            // The Thief falls: the sack opens.
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(Was(ThiefRuleId), OpenTheSack()),
            // The Keeper rises only while a jar stands or, in a duo, while its ally lives.
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                AllOf(Eq(new CountTargetsExpression<CombatantDownedTriggeredEffectContext>(LivingJars), N<CombatantDownedTriggeredEffectContext>(0)),
                    Eq(new CountTargetsExpression<CombatantDownedTriggeredEffectContext>(othersBesideTheKeeper), N<CombatantDownedTriggeredEffectContext>(0))),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(keepers,
                    new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(RisenKeeperId)))),
        ]);

        return
        [
            new EncounterTriggerData("Downed", JsonSerializer.SerializeToElement(
                new EffectProgram<CombatantDownedTriggeredEffectContext>(program),
                CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>())),
        ];
    }

    public static IEnumerable<StatusData> LabyrinthStatuses() =>
    [
        .. ScaleStatuses(),
        .. RisenStatuses(),
        .. JarStatuses(),
    ];
}
