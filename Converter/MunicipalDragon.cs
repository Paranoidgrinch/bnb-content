using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's fourth boss: "Steal civic authority from the hoard and use it against an increasingly illegal
// dragon."
//
// The hoard is a stack of Hoarded Permits on the Dragon: each one is 3 Block a turn and 2 damage on its
// Assessment. Hit hard enough in one turn and the player prises one loose — it becomes Authorization, a
// player-side currency spent on boss-context actions. When the Dragon burns what is left of the registry, every
// unburnt Permit becomes a Code Violation, and an unlicensed dragon hits harder for each one.
//
// Deviations: ADAPTATIONS.md.
public static class MunicipalDragon
{
    public const string DragonId = "the_municipal_dragon";
    public const string PermitId = "hoarded_permit";            // stacks = hoard size, on the Dragon
    public const string ViolationId = "code_violation";         // stacks = UNLICENSED, on the Dragon
    public const string AuthorizationId = "authorization";      // stacks, on the player
    public const string ActionUsedId = "authority_exercised";   // one action per turn, on the player
    public const string StealUsedId = "permit_prised_loose";    // one steal per player turn, on the Dragon
    public const string BurnPendingId = "burning_the_registry"; // telegraph, on the Dragon
    public const string UnlicensedId = "unlicensed";            // Phase II, on the Dragon
    public const string InfernoPendingId = "municipal_inferno_coming"; // telegraph, on the player
    public const string ObjectionId = "filed_objection";        // the inspection substitute, on the Dragon

    public const string ExpeditionCardId = "authorized_expedition";
    public const string EntryCardId = "authorized_entry";
    public const string ObjectionCardId = "file_an_objection";
    public const string CitationCardId = "issue_a_citation";

    public static CounterId StealDamageCounter => new("hoard_pressure");
    public static CounterId StolenPermitsCounter => new("permits_stolen");
    public static CounterId PermitChargesCounter => new("permit_charges");
    public static CounterId DragonBeatCounter => new("dragon_beat");

    public const int StartingPermits = 2;
    public const int PermitMaximum = 4;
    public const int PermitCharges = 2;
    public const int StealThreshold = 14;
    public const int AuthorizationMaximum = 3;
    public const int StolenForTransition = 4;
    public const int BurnHealth = 78;
    public const int DragonBeats = 5;

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheDragon(),
        Stacked(PermitId, "Hoarded Permit", "3 Block a turn, and 2 damage on the Dragon's Assessment."),
        CodeViolation(),
        Stacked(AuthorizationId, "Authorization", "Spend one on a boss-context action, once per turn."),
        PassiveStatuses.NamedMarker(ActionUsedId, "Authority Exercised", null),
        PassiveStatuses.NamedMarker(StealUsedId, "Permit Prised Loose", null),
        PassiveStatuses.NamedMarker(BurnPendingId, "Burning the Registry",
            "The Dragon's next action burns the hoard."),
        PassiveStatuses.NamedMarker(UnlicensedId, "Unlicensed", "Phase II."),
        PassiveStatuses.NamedMarker(InfernoPendingId, "Municipal Inferno Coming",
            "The Dragon's next action is its heaviest."),
        Objection(),
    ];

    public static IEnumerable<CardData> Cards() =>
        [Expedition(), Entry(), ObjectionCard(), Citation()];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheHoardGuards(),
        APermitIsPrisedLoose(),
    ];

    // ── The hoard ─────────────────────────────────────────────────────────────

    // The hoard's Block goes up as the player's turn begins — the design puts it at the Dragon's turn start,
    // but Block is cleared there right after the triggers run, so the guard would wipe itself. It also deals
    // out the Authorization actions the player can currently afford, and opens the per-turn latches.
    private static EncounterTriggerData TheHoardGuards()
    {
        var player = CombatantTargetSelectors.Source;
        var dragon = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(DragonId));
        var unlicensed = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(UnlicensedId));

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        IEffectNode<TurnStartedTriggeredEffectContext> Deal(string cardId) =>
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                player, new CardDefinitionId(cardId), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(dragon,
                        new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                        {
                            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget,
                                new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                                    new IterationTargetStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                        new StatusDefinitionId(PermitId)),
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(3))),
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(StealUsedId)),
                        })),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(ActionUsedId)),
                    // Authority in hand is authority that can be spent: the actions appear while Authorization
                    // is held, and are gone again when the turn ends.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            Stacks(AuthorizationId), ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                        {
                            Deal(ExpeditionCardId),
                            Deal(EntryCardId),
                            Deal(ObjectionCardId),
                            // The citation only exists once there is something to cite.
                            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                                unlicensed, Deal(CitationCardId)),
                        })),
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // Fourteen points of pressure in a single turn prise one Permit out of the hoard — once per player turn.
    // A player already holding three Authorizations still takes the Permit off the Dragon; the authority itself
    // is simply lost.
    private static EncounterTriggerData APermitIsPrisedLoose()
    {
        var dragon = CombatantTargetSelectors.EventTarget;
        var applicant = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId));

        ICombatExpression<DamageReceivedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                dragon, new StatusDefinitionId(statusId));

        var pressure = new AddExpression<DamageReceivedTriggeredEffectContext>(
            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(dragon, StealDamageCounter),
            new EventAmountExpression<DamageReceivedTriggeredEffectContext>());

        var steal = new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
        {
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                dragon, new StatusDefinitionId(StealUsedId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
            new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                dragon, new StatusDefinitionId(PermitId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)),
            new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                dragon, StolenPermitsCounter,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: true),
            // The player's own ceiling is read through the loop — three Authorizations is all anyone may hold.
            new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(applicant,
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new IterationTargetStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                            new StatusDefinitionId(AuthorizationId)),
                        ComparisonOperator.Less,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(AuthorizationMaximum)),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(AuthorizationId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)))),
        });

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        Stacks(DragonId), ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            Stacks(PermitId), ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        new AndExpression<DamageReceivedTriggeredEffectContext>(
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                Stacks(StealUsedId), ComparisonOperator.Equal,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                pressure, ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(StealThreshold))))),
                steal,
                // Below the threshold the hit is simply banked toward it.
                @else: new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    dragon, StealDamageCounter,
                    new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true)));

        return new EncounterTriggerData("DamageTaken",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>()));
    }

    // ── The Dragon's own machinery ────────────────────────────────────────────

    private static StatusData TheDragon()
    {
        var self = CombatantTargetSelectors.Source;
        var applicant = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.ApplicantId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        var beat = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, DragonBeatCounter);

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                // Wounded enough, or robbed often enough: the registry goes up in smoke next action.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(UnlicensedId), ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                Stacks(BurnPendingId), ComparisonOperator.Equal,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                            new OrExpression<TurnEndedTriggeredEffectContext>(
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(self),
                                    ComparisonOperator.LessOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(BurnHealth)),
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, StolenPermitsCounter),
                                    ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(StolenForTransition))))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(BurnPendingId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),

                // The turn's stolen-permit pressure resets with the player's next turn.
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, StealDamageCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),

                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, DragonBeatCounter,
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            beat, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(DragonBeats)),
                    relative: false),

                // The Inferno is announced a full player turn ahead.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(UnlicensedId), ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            beat, ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(DragonBeats - 2))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        applicant, new StatusDefinitionId(InfernoPendingId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    @else: new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        applicant, new StatusDefinitionId(InfernoPendingId))),
            }));

        return new StatusData
        {
            Id = DragonId,
            NameKey = "The Municipal Dragon",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // UNLICENSED: every Code Violation is 2 more damage on a direct attack, and four of them are the cap the
    // burning itself imposes.
    private static StatusData CodeViolation()
    {
        var violation = Stacked(ViolationId, "Code Violation", "The Dragon's attacks deal 2 more each.");
        return violation with
        {
            Polarity = StatusPolarity.Buff,
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 2, RestrictDamageKind: DamageKind.Direct),
            ],
        };
    }

    // The objection the player files instead of ordering an inspection: the Dragon's next blow lands lighter.
    private static StatusData Objection()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new RemoveStatusNode<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ObjectionId)));

        return new StatusData
        {
            Id = ObjectionId,
            NameKey = "Objection Filed",
            DescriptionKey = "Its next attack deals 5 less.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddFlat, -5, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // ── Authorization actions ─────────────────────────────────────────────────

    private static CardData Expedition() => Action(ExpeditionCardId, "Authorized Expedition",
        "Gain 1 Energy.",
        [new GainResourceNode<CardPlayContext>(
            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
            new ConstantExpression<CardPlayContext>(1))]);

    private static CardData Entry() => Action(EntryCardId, "Authorized Entry",
        "Remove up to 12 Block from the Municipal Dragon.",
        [new ModifyDefensivePoolNode<CardPlayContext>(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(DragonId)),
            StandardCombatIds.BlockDefensivePool, new ConstantExpression<CardPlayContext>(-12))]);

    private static CardData ObjectionCard() => Action(ObjectionCardId, "File an Objection",
        "The Dragon's next attack deals 5 less.",
        [new ApplyStatusNode<CardPlayContext>(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(DragonId)),
            new StatusDefinitionId(ObjectionId), new ConstantExpression<CardPlayContext>(1))]);

    private static CardData Citation() => Action(CitationCardId, "Issue a Citation",
        "Remove 1 Code Violation from the Dragon.",
        [new ModifyStatusStacksNode<CardPlayContext>(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(DragonId)),
            new StatusDefinitionId(ViolationId), new ConstantExpression<CardPlayContext>(-1))]);

    // Every Authorization action costs one Authorization, and only one may be exercised per turn.
    private static CardData Action(string id, string name, string text, IEffectNode<CardPlayContext>[] effects)
    {
        var player = CombatantTargetSelectors.Source;

        ICombatExpression<CardPlayContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<CardPlayContext>(player, new StatusDefinitionId(statusId));

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text + "\nCosts 1 Authorization; one authority per turn.",
            Costs = [],
            Tags = [new TagId("form"), new TagId("authorization")],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            Stacks(AuthorizationId), ComparisonOperator.Greater,
                            new ConstantExpression<CardPlayContext>(0)),
                        new ComparisonExpression<CardPlayContext>(
                            Stacks(ActionUsedId), ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayContext>(0))),
                    new SequenceEffectNode<CardPlayContext>(
                    [
                        new ModifyStatusStacksNode<CardPlayContext>(
                            player, new StatusDefinitionId(AuthorizationId),
                            new ConstantExpression<CardPlayContext>(-1)),
                        new ApplyStatusNode<CardPlayContext>(
                            player, new StatusDefinitionId(ActionUsedId),
                            new ConstantExpression<CardPlayContext>(1)),
                        .. effects,
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }

    // ── Raw intents ───────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "emergency_permit" => EmergencyPermit(),
        "burn_the_registry" => BurnTheRegistry(),
        "municipal_inferno" => MunicipalInferno(),
        _ => null,
    };

    // A fresh permit out of nothing — but only twice a fight, and never past a hoard of four.
    private static EffectProgram<EnemyActionContext> EmergencyPermit()
    {
        var self = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new ConditionalEffectNode<EnemyActionContext>(
                    new AndExpression<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(self, PermitChargesCounter),
                            ComparisonOperator.Less,
                            new ConstantExpression<EnemyActionContext>(PermitCharges)),
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                self, new StatusDefinitionId(PermitId)),
                            ComparisonOperator.Less,
                            new ConstantExpression<EnemyActionContext>(PermitMaximum))),
                    new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
                    {
                        new ApplyStatusNode<EnemyActionContext>(
                            self, new StatusDefinitionId(PermitId), new ConstantExpression<EnemyActionContext>(1)),
                        new SetCombatantCounterNode<EnemyActionContext>(
                            self, PermitChargesCounter,
                            new ConstantExpression<EnemyActionContext>(1), relative: true),
                    })),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
            }));
    }

    // The transition: what the player did not steal, the Dragon burns — and every burnt Permit is a Code
    // Violation it will fight the rest of the combat with.
    private static EffectProgram<EnemyActionContext> BurnTheRegistry()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        var permits = new CombatantStatusStacksExpression<EnemyActionContext>(
            self, new StatusDefinitionId(PermitId));

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(BurnPendingId)),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        permits, ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
                    new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
                    {
                        new ApplyStatusNode<EnemyActionContext>(
                            self, new StatusDefinitionId(ViolationId),
                            new MinExpression<EnemyActionContext>(
                                permits, new ConstantExpression<EnemyActionContext>(PermitMaximum))),
                        new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(PermitId)),
                        new ApplyStatusNode<EnemyActionContext>(
                            player, new StatusDefinitionId("paperwork"), new ConstantExpression<EnemyActionContext>(1)),
                    })),
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(8)),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(UnlicensedId), new ConstantExpression<EnemyActionContext>(1)),
            }));
    }

    // The signature. UNLICENSED rides on top through the Code Violation modifier, so 15 base is 23 at four
    // Violations; a heavily cited Dragon also files twice the Paperwork.
    private static EffectProgram<EnemyActionContext> MunicipalInferno()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(15)),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ViolationId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(3)),
                    new ApplyStatusNode<EnemyActionContext>(
                        player, new StatusDefinitionId("paperwork"), new ConstantExpression<EnemyActionContext>(2)),
                    @else: new ApplyStatusNode<EnemyActionContext>(
                        player, new StatusDefinitionId("paperwork"), new ConstantExpression<EnemyActionContext>(1))),
            }));
    }

    private static StatusData Stacked(string id, string name, string? description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
