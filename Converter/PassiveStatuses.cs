using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Reworked enemy PASSIVES, authored as engine statuses-with-triggers (a status the enemy carries from combat
// start via EncounterEnemy.StartingStatuses; see EnemyMapper). Unlike the six ported player statuses
// (StatusMapper), these reactions use the arc's richer effect-program expressions (card-play stats, counters,
// source-scoped reads) that CombatNodeModel does not expose — so each trigger is built as a RAW EffectProgram
// against the engine types and serialized through the CombatJson converters, exactly the path game.roguedeck.json
// uses. Ids here are referenced by enemy source-data `starting_statuses`.
public static class PassiveStatuses
{
    // Well-known content ids.
    public const string QueueAdvancesId = "queue_advances";
    public static CounterId QueuePositionCounter => new("queue_position");

    // A single-opponent selector usable inside an enemy's own status trigger and SERIALIZABLE into the export
    // (unlike FirstTarget, an escape node): the lowest-health enemy of the owner — in a solo fight, the hero.
    private static readonly ICombatantTargetSelector Opponent =
        CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // Unsigned Form Ghost: a WATCHER status that carries the toggle triggers and never leaves, plus the SHIELD
    // status that actually carries the damage reduction. Two statuses because a passive modifier cannot be made
    // conditional — presence is the condition — and a status that removed itself could never come back.
    public const string StillMissingASignatureId = "still_missing_a_signature";
    public const string SignaturePendingId = "signature_pending";
    private const int SignatureThreshold = 3;

    // Wax Notary: one status carrying both halves of "Paper Seals Wax".
    public const string PaperSealsWaxId = "paper_seals_wax";
    private static CounterId SeenPaperworkCounter => new("seen_paperwork");
    private static CounterId SealedThisTurnCounter => new("wax_sealed_this_turn");

    // Sealed Door Ward: the seal itself (carries the rules, and its absence IS the broken seal) plus the
    // per-hit dampener it re-arms each player turn.
    public const string OneRemainingSealId = "one_remaining_seal";
    public const string SealIntactId = "seal_intact";
    private static CounterId SealDamageThisTurnCounter => new("seal_damage_this_turn");
    private const int SealBreakThreshold = 18;

    // Oath Candle: a marker the Candle carries so a cross-combatant trigger can find it (EncounterPassives),
    // and the once-per-round latch it keeps.
    public const string WitnessTheSealId = "witness_the_seal";
    public static CounterId WitnessedThisRoundCounter => new("witnessed_this_round");

    public static IReadOnlyList<StatusData> All() =>
    [
        QueueAdvances(),
        StillMissingASignature(),
        SignaturePending(),
        PaperSealsWax(),
        OneRemainingSeal(),
        SealIntact(),
        WitnessTheSeal(),
        Marker(BothDirectionsMandatoryId, "Both Directions Mandatory",
            "The first card you play each turn picks the road: a Deed sends the Signpost down the dangerous "
            + "shortcut, anything else down the long administrative route. Play nothing and it lists no route."),
        Loophole(),
        // The applicant marker itself is built in Cards/Keywords.cs: besides marking the player it keeps the
        // record of what got through each round, which several cards ask about and which therefore has to be
        // kept whether or not the player is wearing anything else.
        StillInForce(),
        Marker(StolenSandId, "Stolen Sand",
            "Every time Fatigue actually takes Energy from you, the Hourglass banks a grain — up to three."),
        YourNumberIsFading(),
        Marker(StolenMinuteId, "Stolen Minute",
            "End a turn on exactly 0 Energy and the Moth keeps the minute, up to two. At two it spends them."),
        Counterclaim(),
        Sustained(),
        CorrectAgainstTheEvidence(),
        .. CardTypes.Select(Correction),
        Marker(OutstandingWarrantId, "Outstanding Warrant",
            "While you are carrying 4 or more Paperwork, the Bailiff's attacks hit for 5 more."),
        WarrantServed(),
        SeizeTheFiling(),
        BreakTheApproach(),
        Marker(YourNumberCameUpId, "Your Number Came Up",
            "Each time a stack of Panic fades from you on its own, the Wisp burns and you take 4 damage. "
            + "Cleansing Panic outright does not feed it."),
        CarbonCopies(),
        .. Appointments.Select(a => AppointmentDue(a.StatusId, a.Name, a.Description, a.Due, a.Expiry)),
        AppointmentsAccelerated(),
        OfficeHours(),
        Marker(LostTimeLedgerId, "Lost Time",
            "Every point of Energy you leave unspent at the end of your turn becomes Lost Time here, up to three."),
        Marker(PetitionId, "The Petition",
            "At the start of each of your turns the Chorus lays one of its three clauses into your hand. "
            + "They take turns."),
        Marker(PhantomId, "Remanded Case",
            "The case itself. While a living, unspent Remanding Writ still stands, downing this body sends the "
            + "case back instead of ending it."),
        RemandingWrit(),
        Remandable(),
        Marker(SpentWritId, "Spent Writ",
            "This Writ has already sent the case back once. It cannot do it again."),
        Marker(StepLowerId, "Lower Step",
            "The bottom of the staircase. A case held here climbs at the end of its holder's turn."),
        Marker(StepMiddleId, "Middle Step",
            "The middle of the staircase. A case held here climbs one step, or is remanded back down."),
        Marker(StepUpperId, "Upper Step",
            "The top of the staircase. A case that gets here has nowhere left to climb, and the ruling is "
            + "announced instead."),
        HoldsTheCase(),
        RulingPending(),
        Marker(IronWarrantId, "Iron Warrant",
            "At the start of each of your turns the Avatar issues one achievable demand. The orders take turns, "
            + "so the same one never comes twice running."),
        Contempt(),
        Marker(InventoryLanternId, "Inventory Lantern",
            "One card you draw each turn is marked as the Procession's property."),
        Marker(LockCartId, "Lock Cart",
            "A marked card still in your hand when your turn ends is taken away — and every seizure hardens "
            + "the Marshal."),
        Marker(SeizureMarshalId, "Seizure Marshal",
            "Grows stronger with every card the Lock Cart takes."),
        Marker(InventoryPendingId, "Inventoried",
            "The Lantern has marked a card of yours as property this turn."),
        FinalNotice(),
        EnforcementServed(),
        Marker(ServiceAcknowledgedId, "Service Acknowledged",
            "You signed for the notice: 2 Paperwork now, and a lighter enforcement when the deadline lands."),
        Marker(FinalNoticeKnightId, "Final Notice Knight",
            "The one serving the notice. The deadline runs on you, not on it."),
        Marker(SealedSpearId, "Sealed Spear",
            "Bound to the notice. What its death does depends on whether the deadline has already been served."),
        Marker(DeadlineCountingId, "Deadline Running",
            "The Final Notice is counting down at the end of each of your turns."),
        Marker(DeadlineServedId, "Deadline Served",
            "The deadline has run out. Enforcement lands on the Knight's next action, so you get one turn to "
            + "answer it."),
        Marker(GateShutId, "Gate: Shut",
            "The portcullis is down. Hit hard enough in a single turn and it grinds upward."),
        GateHalfRaised(),
        GateOpen(),
        Marker(HeldOpenId, "Held Open",
            "The gate has been forced and is being kept there. A turn that does not hit hard enough lets it "
            + "grind back down."),
        TheGatehouse(),
        .. ComplianceOrders.Select(o => Marker(o.StatusId, o.Name, o.Description)),
        .. DeputyUndersecretary.Statuses(),
        .. QueueCommissioner.Statuses(),
        .. LordSealkeeper.Statuses(),
        .. MunicipalDragon.Statuses(),
        .. LivingCharter.Statuses(),
        .. Elites.ReturnBell.Statuses(),
        .. Elites.RollingStacksColossus.Statuses(),
        .. Elites.CatalogueOfUnwiseNames.Statuses(),
        .. Elites.SilenceBetweenTwoWords.Statuses(),
        .. Elites.BlackInkOracle.Statuses(),
        .. Elites.VolumesOfCauseAndConsequence.Statuses(),
        .. Elites.DrawerOfInfiniteReturns.Statuses(),
        .. Elites.PresentlessClock.Statuses(),
        .. Elites.ObituaryWithThreeEndings.Statuses(),
        .. Bosses.WhisperingCatalogue.Statuses(),
        .. Bosses.WardenOfSealedVolumes.Statuses(),
        .. Bosses.CuratorOfMisplacedHours.Statuses(),
        .. Bosses.AuditorOfReturnedLives.Statuses(),
        .. Bosses.GrandCrossReference.Statuses(),
    ];

    // Iron Warrant Avatar: it issues a visible order each player turn. The orders are statuses ON THE PLAYER —
    // the only thing a combat UI shows by name — and the Avatar checks them when the turn ends.
    public const string IronWarrantId = "iron_warrant";
    public const string ContemptId = "contempt";
    public static CounterId OrderIndexCounter => new("compliance_order");
    public const int ContemptMaximum = 3;

    // The Seizure Procession: the Lantern marks, the Cart takes, the Marshal profits. Each body is found by
    // its own marker, and the confiscation itself rides on the engine's per-instance card marks.
    public const string InventoryLanternId = "inventory_lantern";
    public const string LockCartId = "lock_cart";
    public const string SeizureMarshalId = "seizure_marshal";
    public static readonly TagId InventoriedMark = new("inventoried");
    // The Lantern marks at most one card per player turn: a latch status on the PLAYER records "already marked
    // this turn" (it is cleared when the player's turn starts). A latch on the Lantern itself would have to be
    // read through an iteration target, which expressions cannot do reliably inside a loop.
    public const string InventoryPendingId = "inventory_pending";
    public static CounterId SeizedCardsCounter => new("seized_cards");
    public static CounterId MarshalStrengthCounter => new("marshal_strength");
    public const int SeizureCapacity = 2;
    public const int MarshalStrengthLimit = 4;

    // Portcullis Judicator: the gate's height is three visible marker statuses (the only state a combat UI
    // shows by name), the pressure tally lives in a counter, and the gatehouse status carries the machinery.
    public const string GateShutId = "gate_shut";
    public const string GateHalfId = "gate_half_raised";
    public const string GateOpenId = "gate_open";
    public const string HeldOpenId = "held_open";
    public const string GatehouseId = "the_gatehouse";
    public static CounterId GatePressureCounter => new("gate_pressure");
    public static CounterId GateBeatCounter => new("gate_beat");
    public const int GateThreshold = 12;

    // Final Notice Knight: the deadline is the PLAYER's — Final Notice, Enforcement Served and the
    // acknowledgement all sit on the applicant, where every program can address them with a single selector
    // (and where the player can read their own countdown). The Knight's two mirrors say which phase the
    // encounter is in for programs that run from the enemy side, e.g. the Spear's death.
    public const string FinalNoticeId = "final_notice";
    public const string EnforceQueuedId = "enforcement_served";
    public const string ServiceAcknowledgedId = "service_acknowledged";
    public const string FinalNoticeKnightId = "final_notice_knight";
    public const string SealedSpearId = "sealed_spear";
    public const string DeadlineCountingId = "deadline_running";
    public const string DeadlineServedId = "deadline_served";
    public const int FinalNoticeStart = 3;
    public const string AcknowledgeCardId = "acknowledge_service";

    // Enforcement Served counts the response window: 1 = served (the player's answer turn), 2 = the Knight's
    // next action IS the enforcement.
    private static StatusData EnforcementServed() => new()
    {
        Id = EnforceQueuedId,
        NameKey = "Enforcement Served",
        DescriptionKey = "The Knight will enforce the deadline on its next action.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData FinalNotice() => new()
    {
        Id = FinalNoticeId,
        NameKey = "Final Notice",
        DescriptionKey = "Counts down at the end of each of your turns. At 0 the Knight serves enforcement.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public sealed record ComplianceOrder(
        string StatusId, string Name, string Description,
        Func<ICombatExpression<TurnEndedTriggeredEffectContext, bool>> Fulfilled);

    public static readonly ComplianceOrder[] ComplianceOrders =
    [
        // "Spend at least 3 Energy this turn" — with a three-Energy hero that is an emptied pool.
        new("order_pay_the_fee", "Order: Pay the Fee",
            "Spend at least 3 Energy this turn. Obey and the Avatar takes 5; refuse and it takes it in Contempt.",
            () =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                ComparisonOperator.Equal,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),

        // "Play at least two different card types this turn."
        new("order_file_two_kinds", "Order: File Two Kinds",
            "Play at least two different kinds of card this turn. Obey and the Avatar takes 5; refuse and it "
            + "takes it in Contempt.",
            () =>
        {
            ICombatExpression<TurnEndedTriggeredEffectContext, int>? kinds = null;
            foreach (var type in CardTypes)
            {
                // 1 when at least one card of that type was played, 0 otherwise.
                var played = new MinExpression<TurnEndedTriggeredEffectContext>(
                    new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId(type)),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1));
                kinds = kinds is null ? played : new AddExpression<TurnEndedTriggeredEffectContext>(kinds, played);
            }
            return new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                kinds!, ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(2));
        }),

        // "Play a Skill before the first Attack" — the turn's opening card must not be an attack.
        new("order_observe_the_sequence", "Order: Observe the Sequence",
            "Do not open the turn with a Deed. Obey and the Avatar takes 5; refuse and it takes it in Contempt.",
            () =>
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new NotExpression<TurnEndedTriggeredEffectContext>(
                    new FirstCardPlayedThisTurnHasTagExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId(Cards.CardAuthoring.DeedTag))))),
    ];

    // Contempt: every point makes the Avatar's next direct attack hit 4 harder, and the attack spends it all.
    private static StatusData Contempt()
    {
        var program = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new ModifyStatusStacksNode<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ContemptId),
                new SubtractExpression<DamageDealtTriggeredEffectContext>(
                    new ConstantExpression<DamageDealtTriggeredEffectContext>(0),
                    new CombatantStatusStacksExpression<DamageDealtTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ContemptId)))));

        return new StatusData
        {
            Id = ContemptId,
            NameKey = "Contempt",
            DescriptionKey = "Every point makes the Avatar's next direct attack hit 4 harder — and that attack spends the lot.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 4, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // Appellate Staircase: the Case is a status exactly one Step holds. Which Step is which is a marker, so a
    // program can hand the Case one level down (a Remand) or up (the automatic ascent).
    public const string StepLowerId = "step_lower";
    public const string StepMiddleId = "step_middle";
    public const string StepUpperId = "step_upper";
    public const string HoldsTheCaseId = "holds_the_case";
    public const string RulingPendingId = "ruling_pending";
    public static CounterId CaseDamageThisTurnCounter => new("case_damage_this_turn");
    public static CounterId RemandedThisTurnCounter => new("remanded_this_turn");
    public static CounterId FinalRulingCounter => new("final_ruling");
    private static CounterId TriedAscentCounter => new("tried_ascent");
    private static CounterId CaseMovedThisRoundCounter => new("case_moved_this_round");
    private const int RemandThreshold = 12;

    // The Case itself: its holder hits 2 harder, takes the Remand threshold, and carries the ladder's movement.
    private static StatusData HoldsTheCase()
    {
        var self = CombatantTargetSelectors.Source;
        var hit = CombatantTargetSelectors.EventTarget;
        var stepBelow = CombatantTargetSelectors.IterationTarget;

        ICombatExpression<TContext, bool> Wears<TContext>(ICombatantTargetSelector who, string marker)
            where TContext : class =>
            new ComparisonExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(marker)),
                ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

        // Handing the Case on: apply it to the neighbour and drop it here — inside a loop over that neighbour,
        // so a dead one means the Case simply stays where it is.
        // The neighbour is found among ALL combatants, not among "allies of the source": a round-end program
        // runs with whoever acted last as its source, which may well be the player.
        IEffectNode<TContext> Hand<TContext>(ICombatantTargetSelector holder, string toMarker) where TContext : class =>
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(toMarker)),
                new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                {
                    new ApplyStatusNode<TContext>(CombatantTargetSelectors.IterationTarget,
                        new StatusDefinitionId(HoldsTheCaseId), new ConstantExpression<TContext>(1)),
                    new SetCombatantCounterNode<TContext>(CombatantTargetSelectors.IterationTarget,
                        CaseMovedThisRoundCounter, new ConstantExpression<TContext>(1), relative: false),
                    new ModifyStatusStacksNode<TContext>(holder, new StatusDefinitionId(HoldsTheCaseId),
                        new ConstantExpression<TContext>(-1)),
                }));

        // 12 HP of damage in one player turn remands the case one level down — once per turn, and it also
        // cancels a ruling this Step had announced.
        var onHit = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
            {
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    hit, CaseDamageThisTurnCounter,
                    new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new AddExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(hit, CaseDamageThisTurnCounter),
                                new EventAmountExpression<DamageReceivedTriggeredEffectContext>()),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(RemandThreshold)),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(hit, RemandedThisTurnCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                    new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                    {
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            hit, RemandedThisTurnCounter,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            hit, FinalRulingCounter,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0), relative: false),
                        // Down one level, from wherever this Step stands.
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            Wears<DamageReceivedTriggeredEffectContext>(hit, StepUpperId),
                            Hand<DamageReceivedTriggeredEffectContext>(hit, StepMiddleId),
                            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                                Wears<DamageReceivedTriggeredEffectContext>(hit, StepMiddleId),
                                Hand<DamageReceivedTriggeredEffectContext>(hit, StepLowerId))),
                    })),
            }));

        // The ladder moves at the holder's own turn end, but only ONCE per round: handing the Case on also
        // marks the receiver as "already moved this round", so a Case cannot climb two Steps in the round in
        // which each new holder takes its own turn. (Reading a counter off the iteration target works in a
        // node's target but not inside an expression here, which is why the movement is not a round-end loop.)
        var moved = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, CaseMovedThisRoundCounter);
        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        moved, ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    // It arrived here this round; the ladder has already moved.
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        self, CaseMovedThisRoundCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, RemandedThisTurnCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, TriedAscentCounter),
                                ComparisonOperator.Equal,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                            // Nowhere left to climb: the ruling is announced and the player gets one turn.
                            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                            {
                                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                                    self, FinalRulingCounter,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                    Opponent, new StatusDefinitionId(RulingPendingId),
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                            }),
                            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                            {
                                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                                    Wears<TurnEndedTriggeredEffectContext>(self, StepLowerId),
                                    Hand<TurnEndedTriggeredEffectContext>(self, StepMiddleId),
                                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                                        Wears<TurnEndedTriggeredEffectContext>(self, StepMiddleId),
                                        Hand<TurnEndedTriggeredEffectContext>(self, StepUpperId))),
                                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                                    self, TriedAscentCounter,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                            })))),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, CaseDamageThisTurnCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, RemandedThisTurnCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
            }));

        return new StatusData
        {
            Id = HoldsTheCaseId,
            NameKey = "Holds the Case",
            DescriptionKey = "Whoever holds the case hits 2 harder. Deal 12 damage to the holder inside one of your turns and the "
+ "case is remanded a step down; otherwise it climbs at the holder's turn end.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddFlat, 2, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    onHit, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    onTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // The announced ruling is a mark on the player for that round: the other Steps read it and stand aside.
    private static StatusData RulingPending()
    {
        var marked = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(RulingPendingId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new ModifyStatusStacksNode<RoundEndedTriggeredEffectContext>(
                marked, new StatusDefinitionId(RulingPendingId),
                new ConstantExpression<RoundEndedTriggeredEffectContext>(-1)));

        return Passive(RulingPendingId, "Ruling Pending",
            "A ruling has been announced against you. The other Steps stand aside for the round it takes to land.",
            "RoundEnded", program);
    }

    // The Remanded Case: two bodies and two legitimate kill orders. The Phantom carries the return rule, the
    // Writ the escalation; which one dies first decides how the fight ends.
    public const string PhantomId = "remanded_case";
    public const string RemandableId = "remandable";
    public const string RemandingWritId = "remanding_writ";
    public const string SpentWritId = "spent_writ";
    public static CounterId FinalityCounter => new("finality");

    // Route A — the Phantom would be downed while a living, unspent Writ still stands: the case is REMANDED
    // instead. Authored as death prevention (the engine's one-shot pre-down interceptor) rather than a revive:
    // reviving is impossible by construction — healing and status application refuse a downed target, and the
    // guard rejects the program outright. The prevention consumes this status, so it happens once; the Writ's
    // own death removes it too, which is what makes route B mutually exclusive.
    private static StatusData Remandable() => new()
    {
        Id = RemandableId,
        NameKey = "Remandable",
        DescriptionKey = "While a living, unspent Remanding Writ still stands, the first time this body would go down the case "
+ "is remanded instead — and it comes back angrier.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(24,
        [
            // The Writ pays FIRST: its 12 is a flat HP loss, and the interceptor's damage is credited to the
            // Phantom — so granting the Strength before it would let the case buff its own recoil.
            new InterceptorEffectData(nameof(EffectKind.DealDamage), nameof(EffectTarget.AllAllies), 12, "", 0,
                StatusPolarity.Debuff),
            // Then the case comes back angrier, and the Writ can never remand again.
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 2, "strength", 0,
                StatusPolarity.Buff),
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.AllAllies), 1,
                SpentWritId, 0, StatusPolarity.Neutral),
            new InterceptorEffectData(nameof(EffectKind.RemoveStatus), nameof(EffectTarget.AllAllies), 0,
                RemandingWritId, 0, StatusPolarity.Neutral),
        ]),
    };

    // Route B — the Writ dies before it ever caused a remand: the Phantom gains Finality (+2 Strength) and its
    // next intent becomes Final Judgment. Carried by the Writ, since that is the body being downed; losing the
    // marker (route A) is what makes a spent Writ silent here.
    private static StatusData RemandingWrit()
    {
        var phantom = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(PhantomId));
        var thatPhantom = CombatantTargetSelectors.IterationTarget;

        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(phantom,
                new SequenceEffectNode<CombatantDownedTriggeredEffectContext>(new IEffectNode<CombatantDownedTriggeredEffectContext>[]
                {
                    new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                        thatPhantom, new StatusDefinitionId("strength"),
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(2)),
                    new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(
                        thatPhantom, FinalityCounter,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(1), relative: false),
                    // No Writ, no remand: the case has nowhere left to be sent back to.
                    new ModifyStatusStacksNode<CombatantDownedTriggeredEffectContext>(
                        thatPhantom, new StatusDefinitionId(RemandableId),
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(-1)),
                })));

        return Passive(RemandingWritId, "Remanding Writ",
            "While this Writ lives, downing the case remands it instead of ending it. Kill the Writ first and "
            + "the case takes Finality — and its next move is Final Judgment.",
            "Downed", program);
    }

    // Living Petition Chorus: the marker the clause cards write their signatures and liabilities onto.
    public const string PetitionId = "the_petition";
    public static CounterId SignaturesCounter => new("signatures");
    public static CounterId ClauseIndexCounter => new("clause_index");

    // Devouring Waiting Room: the Room keeps the Lost Time ledger (so killing it erases the resource, while
    // killing the Moth Cloud leaves what was already lost). The marker is how the Moth and the encounter
    // trigger find the Room.
    public const string LostTimeLedgerId = "lost_time_ledger";
    public static CounterId LostTimeCounter => new("lost_time");
    public const int LostTimeMaximum = 3;

    // Reopening-Hours Monolith: the office's own schedule. Closed windows bank whatever the player does; the
    // open window processes the backlog in one go.
    public const string OfficeHoursId = "office_hours";
    public static CounterId PendingBusinessCounter => new("pending_business");
    public static CounterId OfficeOpenCounter => new("office_open");
    private static CounterId ClosedWindowsCounter => new("closed_windows");
    private static CounterId OpenWindowsCounter => new("open_windows");

    // "OFFICE CLOSED — Reopening in 2": while closed, HP loss the player causes is not removed but STORED as
    // Pending Business; two closed windows later the office opens and processes the lot at once, then shuts
    // again after its next action. Storing is done by healing the hit straight back and banking its size — the
    // engine has no "redirect this damage into a track", and the numbers work out identically (ADAPTATIONS.md).
    private static StatusData OfficeHours()
    {
        var self = CombatantTargetSelectors.Source;
        var hit = CombatantTargetSelectors.EventTarget; // the damaged combatant: the Monolith

        var store = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                    new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(hit, OfficeOpenCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                {
                    new HealNode<DamageReceivedTriggeredEffectContext>(
                        hit, new EventAmountExpression<DamageReceivedTriggeredEffectContext>()),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        hit, PendingBusinessCounter,
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                })));

        var pending = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, PendingBusinessCounter);
        var schedule = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, OfficeOpenCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                // The office was open for one action; the shutters come down again.
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, OfficeOpenCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    // One closed window has already passed, so this one completes the pair.
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, ClosedWindowsCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                    {
                        // Open FIRST: the backlog lands as ordinary damage, and by then the office no longer
                        // stores what it takes — otherwise it would bank its own processing.
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            self, OfficeOpenCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            self, ClosedWindowsCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            self, OpenWindowsCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                        new DealDamageNode<TurnEndedTriggeredEffectContext>(self, pending),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            self, PendingBusinessCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    }),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        self, ClosedWindowsCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true))));

        return new StatusData
        {
            Id = OfficeHoursId,
            NameKey = "Office Hours",
            DescriptionKey = "While the window is closed, the HP you take off is banked as Pending Business rather than lost. Two "
+ "closed windows later the office opens and processes the lot at once, then shuts after its next action.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    store, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    schedule, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // The Three Appointments (elite): each body carries its own visible countdown and its own consequence when
    // it runs out. The countdown ticks at the body's OWN turn end — once per round, exactly like the design's
    // "end of each player turn", only a beat later in the round (see ADAPTATIONS.md).
    public static CounterId AppointmentDueCounter => new("appointment_due");
    public const string AppointmentsAcceleratedId = "appointments_accelerated";

    private static CounterId AppointmentStartedCounter => new("appointment_started");

    public static readonly (string StatusId, string Name, string Description, int Due,
        Func<IEffectNode<TurnEndedTriggeredEffectContext>> Expiry)[]
        Appointments =
        [
            ("appointment_due_first", "Appointment Due (First)",
                "Counts down at the end of this body's turn. When it runs out you take 7 damage and 1 Fatigue.",
                2, () => Expiry(damage: 7, fatigue: 1)),
            ("appointment_due_second", "Appointment Due (Second)",
                "Counts down at the end of this body's turn. When it runs out you take 2 Paperwork and 1 Fatigue.",
                3, () => Expiry(paperwork: 2, fatigue: 1)),
            ("appointment_due_final", "Appointment Due (Final)",
                "Counts down at the end of this body's turn. When it runs out you take 15 damage and 1 Fatigue.",
                4, () => Expiry(damage: 15, fatigue: 1)),
        ];

    private static IEffectNode<TurnEndedTriggeredEffectContext> Expiry(
        int damage = 0, int paperwork = 0, int fatigue = 0)
    {
        var nodes = new List<IEffectNode<TurnEndedTriggeredEffectContext>>();
        if (damage > 0)
            nodes.Add(new DealDamageNode<TurnEndedTriggeredEffectContext>(
                Opponent, new ConstantExpression<TurnEndedTriggeredEffectContext>(damage)));
        if (paperwork > 0)
            nodes.Add(new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Opponent, new StatusDefinitionId("paperwork"),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(paperwork)));
        if (fatigue > 0)
            nodes.Add(new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Opponent, new StatusDefinitionId("fatigue"),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(fatigue)));
        return new SequenceEffectNode<TurnEndedTriggeredEffectContext>(nodes);
    }

    private static StatusData AppointmentDue(
        string id, string name, string description, int due,
        Func<IEffectNode<TurnEndedTriggeredEffectContext>> expiry)
    {
        var self = CombatantTargetSelectors.Source;
        var remaining = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, AppointmentDueCounter);

        // One step per own turn end. The "is it due NOW" test compares against 1 rather than reading the
        // counter it is about to write — within one program a node cannot see an earlier node's write.
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    remaining, ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    expiry(),
                    // Spent: no countdown runs again until a scheduling move sets a new one.
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        self, AppointmentDueCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                }),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        remaining, ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        self, AppointmentDueCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-1), relative: true))));

        // The countdown is ARMED at the body's first turn start: starting statuses cannot carry a counter
        // value, and "it is 0" cannot mean both "not started yet" and "already expired" — a separate started
        // flag says which. (A RoundStarted trigger would be the natural place, but the very first one fires
        // before any combatant is active, so its context does not resolve.)
        var arm = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, AppointmentStartedCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, AppointmentDueCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(due), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, AppointmentStartedCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false),
                })));

        var status = Passive(id, name, description, "TurnEnded", program);
        return status with
        {
            Triggers = [.. status.Triggers, new StatusTriggerData("TurnStarted",
                JsonSerializer.SerializeToElement(arm, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()))],
        };
    }

    // The anti-spike latch: whoever uses a scheduling move marks the player, and the other Appointments' intent
    // rules stand down while the mark is there. It clears at the end of the round — a status is the only handle
    // an intent CONDITION has on shared state (conditions read statuses, not counters, on the opponent).
    private static StatusData AppointmentsAccelerated()
    {
        var marked = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(AppointmentsAcceleratedId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new ModifyStatusStacksNode<RoundEndedTriggeredEffectContext>(
                marked, new StatusDefinitionId(AppointmentsAcceleratedId),
                new ConstantExpression<RoundEndedTriggeredEffectContext>(-1)));

        return Passive(AppointmentsAcceleratedId, "Appointments Accelerated",
            "One appointment has been moved forward. While this stands, the others hold off.",
            "RoundEnded", program);
    }

    // Duplicate Copy Mites: marker + the once-per-round latch, cleared at round end like its siblings.
    public const string CarbonCopiesId = "carbon_copies";
    public static CounterId CopiedThisRoundCounter => new("copied_this_round");

    private static StatusData CarbonCopies()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CarbonCopiesId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, CopiedThisRoundCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(CarbonCopiesId, "Carbon Copies",
            "The first time each round another enemy gains Bookworm, the Mites guard themselves for 4.",
            "RoundEnded", program);
    }

    // Number-Ticket Wisp: the marker its encounter trigger finds it by (the rule itself watches the PLAYER's
    // Panic, so it lives on the encounter).
    public const string YourNumberCameUpId = "your_number_came_up";

    // The Ward's marker also clears its once-per-round latch at round end (RoundEnded triggers have no bearer
    // filter, so the reset targets every carrier).
    private static StatusData SeizeTheFiling()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(SeizeTheFilingId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, SeizedThisRoundCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(SeizeTheFilingId, "Seize the Filing",
            "The first Paperwork you file on any enemy each round is turned against you: that enemy gains "
            + "1 Bookworm, and will eat the filing at its turn start.",
            "RoundEnded", program);
    }

    // Warrant Bailiff: the marker its watcher finds it by, and the buff the watcher switches on and off.
    public const string OutstandingWarrantId = "outstanding_warrant";
    public const string WarrantServedId = "warrant_served";

    // Threshold Seizure Ward: marker + the once-per-round latch its encounter trigger reads.
    public const string SeizeTheFilingId = "seize_the_filing";
    public static CounterId SeizedThisRoundCounter => new("seized_this_round");

    // Civic Battering Ram: Momentum, plus the two bits of bookkeeping "Break the Approach" needs.
    public const string BreakTheApproachId = "break_the_approach";
    public static CounterId MomentumCounter => new("momentum");
    private static CounterId HadBlockCounter => new("ram_had_block");
    private static CounterId ApproachBrokenCounter => new("approach_broken_this_turn");

    // "Outstanding Warrant" is a plain buff the Bailiff wears while the player is 4 Paperwork deep — the
    // watcher that switches it lives on the encounter (EncounterPassives), because the condition is about the
    // PLAYER while the buff belongs to the enemy.
    private static StatusData WarrantServed() => new()
    {
        Id = WarrantServedId,
        NameKey = "Warrant Served",
        DescriptionKey = "The warrant is out: this character's attacks hit for 5 more.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.AddFlat, 5, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    // "Break the Approach" (Civic Battering Ram): the first time each player turn a card strips the Ram's Block
    // away entirely, it loses a Momentum. "Entirely" needs to know the Block was there — the Ram remembers
    // gaining it (BlockGained on itself) and forgets once the guard is broken.
    private static StatusData BreakTheApproach()
    {
        var ram = CombatantTargetSelectors.EventTarget;

        var onBlockGained = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new SetCombatantCounterNode<BlockGainedTriggeredEffectContext>(
                ram, HadBlockCounter, new ConstantExpression<BlockGainedTriggeredEffectContext>(1), relative: false));

        var onHit = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        // A guard that was there and is now gone…
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(ram, HadBlockCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantDefensivePoolExpression<DamageReceivedTriggeredEffectContext>(
                                ram, StandardCombatIds.BlockDefensivePool),
                            ComparisonOperator.Equal,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                    // …once per player turn.
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(ram, ApproachBrokenCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                {
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        ram, MomentumCounter,
                        new MaxExpression<DamageReceivedTriggeredEffectContext>(
                            new SubtractExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(ram, MomentumCounter),
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        relative: false),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        ram, HadBlockCounter, new ConstantExpression<DamageReceivedTriggeredEffectContext>(0),
                        relative: false),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        ram, ApproachBrokenCounter, new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                        relative: false),
                })));

        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, ApproachBrokenCounter,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false));

        return new StatusData
        {
            Id = BreakTheApproachId,
            NameKey = "Break the Approach",
            DescriptionKey = "The first time each turn a card strips the Ram's Block away entirely, it loses a Momentum.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("BlockGained", JsonSerializer.SerializeToElement(
                    onBlockGained, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>())),
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    onHit, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    onTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // Self-Correcting Record: the card TYPES it can correct against (CardMapper emits a card's type as a
    // combat tag), the passive that arms a correction, and its once-per-player-turn latch.
    public static readonly string[] CardTypes =
        [Cards.CardAuthoring.DeedTag, Cards.CardAuthoring.WorkingTag, Cards.CardAuthoring.RiteTag];
    public const string CorrectAgainstTheEvidenceId = "correct_against_the_evidence";
    public static CounterId CorrectedThisTurnCounter => new("corrected_this_turn");
    public static string CorrectionId(string cardType) => $"correction_{cardType}";
    private const int CorrectionThreshold = 10;

    // "Correct Against the Evidence": the first card to deal 10+ HP damage to the Record each player turn is
    // studied; the next damaging card of THAT type deals 4 less, and the correction is spent. Everything sits
    // on the Record itself (it is the one being hit), so plain status triggers do it — the type is read off
    // the hit's own card, and each type has its own correction status because a passive modifier is data, not
    // a condition.
    private static StatusData CorrectAgainstTheEvidence()
    {
        var record = CombatantTargetSelectors.EventTarget; // the damaged combatant: the Record

        var arm = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(CorrectionThreshold)),
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
                            record, CorrectedThisTurnCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    .. CardTypes.Select(type => (IEffectNode<DamageReceivedTriggeredEffectContext>)
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            new TriggerEventSourceCardHasTagExpression<DamageReceivedTriggeredEffectContext>(new TagId(type)),
                            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                                record, new StatusDefinitionId(CorrectionId(type)),
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)))),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        record, CorrectedThisTurnCounter,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                ])));

        // At the Record's own turn end the correction lapses — it only holds for the player turn it was made in.
        var lapse = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                .. CardTypes.Select(type => (IEffectNode<TurnEndedTriggeredEffectContext>)
                    new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(CorrectionId(type)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-1))),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CorrectedThisTurnCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
            ]));

        return new StatusData
        {
            Id = CorrectAgainstTheEvidenceId,
            NameKey = "Correct Against the Evidence",
            DescriptionKey = "The first card to take 10 or more HP off the Record each turn is studied. The next damaging card of "
+ "that same kind deals 4 less.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    arm, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    lapse, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // One armed correction: 4 less from the next card of that type, and studying it spends the correction.
    private static StatusData Correction(string cardType) => new()
    {
        Id = CorrectionId(cardType),
        NameKey = $"Corrected ({cardType})",
        DescriptionKey = $"The Record has studied a {cardType}. The next damaging {cardType} deals 4 less, "
            + "and the correction is spent.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.AddFlat, -4,
                RestrictDamageKind: DamageKind.Direct, RestrictSourceCardTag: cardType),
        ],
        Triggers =
        [
            new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                new EffectProgram<DamageReceivedTriggeredEffectContext>(
                    new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<DamageReceivedTriggeredEffectContext>(
                            new TagId(cardType)),
                        new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(CorrectionId(cardType)),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)))),
                CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
        ],
    };

    // Counterclaim Imp: the passive itself is owner-scoped (the Imp is what gets filed on), so one status
    // carries both the reaction and the once-per-player-turn latch it clears at its own turn end.
    public const string CounterclaimId = "counterclaim";
    public static CounterId CounterclaimUsedCounter => new("counterclaim_used");

    private static StatusData Counterclaim()
    {
        var imp = CombatantTargetSelectors.EventTarget; // the status' recipient: the Imp
        var filer = CombatantTargetSelectors.Source;    // whoever applied it

        StatusTriggerData Reaction<TContext>(string trigger) where TContext : class
        {
            var program = new EffectProgram<TContext>(
                new ConditionalEffectNode<TContext>(
                    new AndExpression<TContext>(
                        // Only the player's own filings answer back, and only the first each turn.
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(filer, new StatusDefinitionId(ApplicantId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TContext>(0)),
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(imp, CounterclaimUsedCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TContext>(0))),
                    new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                    {
                        new ApplyStatusNode<TContext>(filer, new StatusDefinitionId("paperwork"),
                            new ConstantExpression<TContext>(1)),
                        new SetCombatantCounterNode<TContext>(imp, CounterclaimUsedCounter,
                            new ConstantExpression<TContext>(1), relative: false),
                    })));

            return new StatusTriggerData(trigger,
                JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
        }

        return new StatusData
        {
            Id = CounterclaimId,
            NameKey = "Counterclaim",
            DescriptionKey = "The first Paperwork you file on the Imp each turn is answered — you get 1 Paperwork back.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                Reaction<StatusAppliedTriggeredEffectContext>("StatusApplied"),
                Reaction<StatusMergedTriggeredEffectContext>("StatusMerged"),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    new EffectProgram<TurnEndedTriggeredEffectContext>(
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CounterclaimUsedCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false)),
                    CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // Sustaining Gavel: marker + the once-per-round latch its encounter trigger reads (EncounterPassives).
    public const string SustainedId = "sustained";
    public static CounterId SustainedThisRoundCounter => new("sustained_this_round");

    private static StatusData Sustained()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(SustainedId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, SustainedThisRoundCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(SustainedId, "Sustained",
            "The first time each round another living enemy gains Block, the Gavel copies half of it, rounded down.",
            "RoundEnded", program);
    }

    // Inverted Hourglass: the marker its encounter trigger finds it by; the sand itself is a counter.
    public const string StolenSandId = "stolen_sand_passive";
    public static CounterId StolenSandCounter => new("stolen_sand");

    // Minute Moth: same shape — marker + the counter its intent rule reads.
    public const string StolenMinuteId = "stolen_minute_passive";
    public static CounterId StolenMinuteCounter => new("stolen_minute");

    // "Your Number Is Fading" (Fading Number Token): at the end of each of its own turns the Token loses 3 HP
    // unless the player is carrying Fatigue — it only lasts as long as it can keep the queue waiting. Purely
    // owner-scoped, so an ordinary status trigger does it; the opponent (the hero in a solo party) is the
    // Token's lowest-health enemy.
    private static StatusData YourNumberIsFading()
    {
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        Opponent, new StatusDefinitionId("fatigue")),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new DealDamageNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(3))));

        return Passive("your_number_is_fading", "Your Number Is Fading",
            "At the end of each of its own turns the Token loses 3 HP unless you are carrying Fatigue. It only "
            + "lasts as long as it can keep the queue waiting.",
            "TurnEnded", program);
    }

    // The hero carries this in every fight (EncounterMapper) so a program can ask "did this happen to the
    // player?" — selectors are structural and cannot name a side.
    public const string ApplicantId = "the_applicant";

    // Old Statute Ghost: marker + the two tracks of "Still in Force".
    public const string StillInForceId = "still_in_force_passive";
    public static CounterId PrecedentCounter => new("precedent");
    public static CounterId PrecedentLatchCounter => new("precedent_this_round");

    private static StatusData StillInForce()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(StillInForceId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, PrecedentLatchCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(StillInForceId, "Still in Force",
            "The first time each round Panic, Doubt or Fatigue leaves you entirely, the Ghost gains 1 Precedent. "
            + "At 2 it re-files one stack of whatever just went, and clears its Precedent.",
            "RoundEnded", program);
    }

    // Exception Imp: the marker its encounter trigger finds it by, plus the once-per-round latch it clears at
    // round end (like the Oath Candle's, and for the same reason — RoundEnded triggers have no bearer filter).
    public const string LoopholeId = "loophole";
    public static CounterId LoopholeUsedCounter => new("loophole_used");

    private static StatusData Loophole()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(LoopholeId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, LoopholeUsedCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(LoopholeId, "Loophole",
            "The first time each round the enemy side would put a negative status on you, one stack of it is "
            + "struck out — a single stack is voided entirely — and the Imp gains 1 Strength for the exception.",
            "RoundEnded", program);
    }

    // Contradictory Signpost: a pure marker so its encounter trigger can write the route counter to the
    // Signpost and nobody else (see EncounterPassives.BothDirectionsMandatory).
    public const string BothDirectionsMandatoryId = "both_directions_mandatory";
    public static CounterId SignpostedRouteCounter => new("signposted_route");

    // A status that carries nothing but its own presence: the handle a cross-combatant trigger uses to find
    // one specific enemy, since selectors are structural and cannot name a combatant.
    // HALF-RAISED and OPEN are markers like SHUT; OPEN additionally makes the Judicator take 20 % more.
    private static StatusData GateHalfRaised() => Marker(GateHalfId, "Gate: Half-Raised",
        "The portcullis is halfway up. Another hard turn opens it; a soft one lets it fall.");

    private static StatusData GateOpen()
    {
        var open = Marker(GateOpenId, "Gate: Open",
            "The portcullis is up and the Judicator is exposed: while it stands open it takes 20 % more damage.");
        return open with
        {
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                    PassiveModifierOperation.ScalePercent, 120, RestrictDamageKind: null),
            ],
        };
    }

    // "The Gatehouse" (Portcullis Judicator): the player forces the portcullis up by hitting hard enough in
    // one turn, and it grinds back down whenever they do not. The gate rises THE MOMENT the threshold is
    // crossed, so the player sees the harsher band before deciding how to end the turn.
    private static StatusData TheGatehouse()
    {
        var bearer = CombatantTargetSelectors.EventTarget;
        var self = CombatantTargetSelectors.Source;

        ICombatExpression<DamageReceivedTriggeredEffectContext, int> Pressure() =>
            new AddExpression<DamageReceivedTriggeredEffectContext>(
                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(bearer, GatePressureCounter),
                new EventAmountExpression<DamageReceivedTriggeredEffectContext>());

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> Wearing(string statusId) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                    bearer, new StatusDefinitionId(statusId)),
                ComparisonOperator.Greater,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0));

        IEffectNode<DamageReceivedTriggeredEffectContext> Raise(string from, string to) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                Wearing(from),
                new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                {
                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(bearer, new StatusDefinitionId(from)),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        bearer, new StatusDefinitionId(to),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                }));

        // Every hit banks its damage; the hit that carries the tally over the threshold forces the gate.
        var onHit = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
            {
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    bearer, GatePressureCounter,
                    new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        // The tally above is an ENQUEUED write, so this hit has to add itself.
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            Pressure(), ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(GateThreshold)),
                        // Once per player turn: Held Open is both the latch and the visible "it stays up".
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                                bearer, new StatusDefinitionId(HeldOpenId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                    new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                    {
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            bearer, new StatusDefinitionId(HeldOpenId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                        // Half → Open is checked FIRST: both branches read the same pre-write state, so
                        // raising Shut → Half first would let one hit climb two levels.
                        Raise(GateHalfId, GateOpenId),
                        Raise(GateShutId, GateHalfId),
                    })),
            }));

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> WornBySelf(string statusId, ComparisonOperator op, int value) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    self, new StatusDefinitionId(statusId)),
                op, new ConstantExpression<TurnEndedTriggeredEffectContext>(value));

        IEffectNode<TurnEndedTriggeredEffectContext> Lower(string from, string to) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                WornBySelf(from, ComparisonOperator.Greater, 0),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(from)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(to),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                }));

        // At the Judicator's OWN turn end — its ruling has been handed down — the gate settles: it falls a
        // level unless the player forced it this round, and the round's bookkeeping resets.
        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    WornBySelf(HeldOpenId, ComparisonOperator.Equal, 0),
                    new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                    {
                        Lower(GateOpenId, GateHalfId),
                        Lower(GateHalfId, GateShutId),
                    })),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(HeldOpenId)),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, GatePressureCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                // The beat alternates between each band's two rulings. Both conditions read the same
                // pre-write value, so exactly one of them fires.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, GateBeatCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        self, GateBeatCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false)),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, GateBeatCounter),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        self, GateBeatCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false)),
            }));

        return new StatusData
        {
            Id = GatehouseId,
            NameKey = "The Gatehouse",
            DescriptionKey = "The portcullis answers how hard you hit in a single turn: enough forces it up the moment the threshold "
+ "is crossed, and a turn that falls short lets it grind back down.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    onHit, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    onTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // A marker with a player-facing description (boss state the UI should explain).
    public static StatusData NamedMarker(string id, string name, string description) =>
        Marker(id, name, description);

    // Every status the player can see owes them an explanation on hover, so a description is not optional
    // here — a marker with a name and nothing else is a riddle on a chip (Tests/StatusDescriptionTests).
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

    // "The Queue Advances" (A Very Official Line): if the player ended their turn having played 3+ cards, the
    // enemy gains 1 Queue Position (capped at 3). Read at the enemy's turn start (right after the player's turn)
    // via cardsPlayedLastTurn on the opponent. The cash-out ("at 3, replace the next intent with Everyone Moves
    // at Once, then Queue Position → 0") is the enemy's intent_rule (self_counter ≥ 3) + that action resetting
    // the counter — authored on the enemy in source-data.
    private static StatusData QueueAdvances()
    {
        var atLeastThree = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CardsPlayedLastTurnExpression<TurnStartedTriggeredEffectContext>(Opponent),
            ComparisonOperator.GreaterOrEqual,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(3));

        // queue_position = min(queue_position + 1, 3)
        var cappedIncrement = new MinExpression<TurnStartedTriggeredEffectContext>(
            new AddExpression<TurnStartedTriggeredEffectContext>(
                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, QueuePositionCounter),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(3));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                atLeastThree,
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, QueuePositionCounter, cappedIncrement, relative: false)));

        return Passive(QueueAdvancesId, "The Queue Advances",
            "End a turn having played 3 or more cards and the line moves: 1 Queue Position, up to 3. At 3 it "
            + "does Everyone Moves at Once and the count goes back to nothing.",
            "TurnStarted", program);
    }

    // "Still Missing a Signature" (Unsigned Form Ghost): while the Ghost carries fewer than 3 Paperwork it takes
    // 25% less direct damage; at 3+ the reduction is off; if Bookworm files it back below 3 the reduction
    // returns. The engine's passive modifiers cannot be conditional, so the reduction lives in its own status
    // (SignaturePending) and this watcher switches it on and off whenever the Ghost's statuses move.
    //
    // Every status event the Ghost is the SUBJECT of resolves the bearer as `eventTarget` (Applied/Merged/
    // Removed bind it to the affected combatant; StacksChanged binds both source and eventTarget to it), so one
    // program shape serves all four. All four are needed: a first Paperwork APPLIES, further ones MERGE, and
    // Bookworm only ADJUSTS the count. The program is idempotent — it adds a missing shield or drops a present
    // one — so the shield's own status events cannot make it loop.
    private static StatusData StillMissingASignature() => new()
    {
        Id = StillMissingASignatureId,
        NameKey = "Still Missing a Signature",
        DescriptionKey = "While the Ghost carries fewer than 3 Paperwork, it takes 25 % less damage from cards and attacks. "
+ "File enough on it and the form is finally signed.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            SignatureTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
            SignatureTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
            SignatureTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
            SignatureTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
        ],
    };

    private static StatusTriggerData SignatureTrigger<TContext>(string trigger) where TContext : class
    {
        var bearer = CombatantTargetSelectors.EventTarget;
        var paperwork = new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId("paperwork"));
        var shield = new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId(SignaturePendingId));

        var program = new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(paperwork, ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TContext>(SignatureThreshold)),
                // Filed often enough: drop the reduction (if it is still up).
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(shield, ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    new ModifyStatusStacksNode<TContext>(bearer, new StatusDefinitionId(SignaturePendingId),
                        new ConstantExpression<TContext>(-1))),
                // Still unsigned: put the reduction back (if it is not already up).
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(shield, ComparisonOperator.Equal,
                        new ConstantExpression<TContext>(0)),
                    new ApplyStatusNode<TContext>(bearer, new StatusDefinitionId(SignaturePendingId),
                        new ConstantExpression<TContext>(1)))));

        return new StatusTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // The reduction itself: 25% less DIRECT damage (card hits and attacks; Paperwork's own tick is
    // DamageOverTime and stays untouched). Carried only while the watcher says the form is still unsigned.
    private static StatusData SignaturePending() => new()
    {
        Id = SignaturePendingId,
        NameKey = "Signature Pending",
        DescriptionKey = "The form is unsigned: 25 % less damage from cards and attacks. Paperwork's own tick goes through it.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 75, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    // "Paper Seals Wax" (Wax Notary): the first time each player turn the Notary RECEIVES Paperwork it gains 5
    // Block; the Paperwork stays. "Receives" is read as "the count went up", by remembering the last seen count
    // in a counter — a plain status-event gate would also fire for any other status landing on it (its duo
    // partner hands out Bookworm). The once-per-turn latch resets at the Notary's own turn end, i.e. exactly
    // when the player's next turn is about to begin.
    private static StatusData PaperSealsWax() => new()
    {
        Id = PaperSealsWaxId,
        NameKey = "Paper Seals Wax",
        DescriptionKey = "The first time each of your turns the Notary receives Paperwork, it gains 5 Block. The Paperwork stays.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            SealTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
            SealTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
            SealTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
            SealTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
            new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                new EffectProgram<TurnEndedTriggeredEffectContext>(
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, SealedThisTurnCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false)),
                CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
        ],
    };

    private static StatusTriggerData SealTrigger<TContext>(string trigger) where TContext : class
    {
        var bearer = CombatantTargetSelectors.EventTarget;
        var paperwork = new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId("paperwork"));
        var seen = new CombatantCounterExpression<TContext>(bearer, SeenPaperworkCounter);

        var program = new EffectProgram<TContext>(
            new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
            {
                new ConditionalEffectNode<TContext>(
                    new AndExpression<TContext>(
                        new ComparisonExpression<TContext>(paperwork, ComparisonOperator.Greater, seen),
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(bearer, SealedThisTurnCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TContext>(0))),
                    new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                    {
                        new GainBlockNode<TContext>(bearer, new ConstantExpression<TContext>(5)),
                        new SetCombatantCounterNode<TContext>(bearer, SealedThisTurnCounter,
                            new ConstantExpression<TContext>(1), relative: false),
                    })),
                // Always resync, so a later filing counts as new and a cleanse doesn't leave a stale high-water mark.
                new SetCombatantCounterNode<TContext>(bearer, SeenPaperworkCounter, paperwork, relative: false),
            }));

        return new StatusTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // "One Remaining Seal" (Sealed Door Ward): while the seal holds, the FIRST card hit against the Ward each
    // player turn deals 4 less; take 18+ HP damage within one player turn and the seal breaks for good, taking
    // 6 direct damage with it. The seal's own presence is the "active" flag — once it is gone nothing re-arms
    // the dampener, which is exactly what "permanently" means here.
    private static StatusData OneRemainingSeal()
    {
        var bearer = CombatantTargetSelectors.EventTarget;

        // On every hit: bank it, spend the dampener, and check the break threshold.
        var onHit = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
            {
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    bearer, SealDamageThisTurnCounter,
                    new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                // Only the first hit of the turn is dampened.
                new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                    bearer, new StatusDefinitionId(SealIntactId),
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)),
                // The tally above is an ENQUEUED write, so it is not visible to this test yet — the threshold
                // has to add this hit itself: banked-so-far + this hit.
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new AddExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(bearer, SealDamageThisTurnCounter),
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>()),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(SealBreakThreshold)),
                    new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                    {
                        // Break FIRST, then take the recoil: with the seal already gone the recoil cannot
                        // re-enter this trigger at all.
                        new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                            bearer, new StatusDefinitionId(OneRemainingSealId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)),
                        new DealDamageNode<DamageReceivedTriggeredEffectContext>(
                            bearer, new ConstantExpression<DamageReceivedTriggeredEffectContext>(6)),
                    })),
            }));

        // At the Ward's own turn end — the player's turn is next — the dampener is re-armed and the tally resets.
        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(SealIntactId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(SealIntactId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, SealDamageThisTurnCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
            }));

        return new StatusData
        {
            Id = OneRemainingSealId,
            NameKey = "One Remaining Seal",
            DescriptionKey = "While the seal holds, the first card to hit the Ward each turn deals 4 less. Deal it 18 or more damage "
+ "inside a single turn and the seal breaks for good, taking 6 off the Ward with it.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("DamageTaken",
                    JsonSerializer.SerializeToElement(onHit, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded",
                    JsonSerializer.SerializeToElement(onTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // The dampener the seal re-arms: −4 on a DIRECT hit, spent by the first one each player turn.
    private static StatusData SealIntact() => new()
    {
        Id = SealIntactId,
        NameKey = "Seal Intact",
        DescriptionKey = "The seal's dampener is armed: the next card to hit the Ward this turn deals 4 less.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.AddFlat, -4, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    // "Witness the Seal" (Oath Candle): the marker that identifies the Candle to its encounter trigger (see
    // EncounterPassives.WitnessTheSeal) and resets its once-per-round latch. The reset targets every carrier of
    // the marker rather than `source`, because RoundEnded status triggers carry no bearer filter — in a fight
    // without a Candle the selector simply finds nobody.
    private static StatusData WitnessTheSeal()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(WitnessTheSealId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, WitnessedThisRoundCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(WitnessTheSealId, "Witness the Seal",
            "The first time each round another enemy gains Block, the Candle gives it 3 more.",
            "RoundEnded", program);
    }

    // Builds a hidden, non-stacking enemy status whose sole job is to carry one trigger program.
    private static StatusData Passive<TContext>(
        string id, string name, string description, string trigger, EffectProgram<TContext> program)
        where TContext : class => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [new StatusTriggerData(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()))],
    };
}
