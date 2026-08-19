using System.Text.Json;
using RogueDeck.Core.Combat;
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
    public static readonly CounterId QueuePositionCounter = new("queue_position");

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
    private static readonly CounterId SeenPaperworkCounter = new("seen_paperwork");
    private static readonly CounterId SealedThisTurnCounter = new("wax_sealed_this_turn");

    // Sealed Door Ward: the seal itself (carries the rules, and its absence IS the broken seal) plus the
    // per-hit dampener it re-arms each player turn.
    public const string OneRemainingSealId = "one_remaining_seal";
    public const string SealIntactId = "seal_intact";
    private static readonly CounterId SealDamageThisTurnCounter = new("seal_damage_this_turn");
    private const int SealBreakThreshold = 18;

    // Oath Candle: a marker the Candle carries so a cross-combatant trigger can find it (EncounterPassives),
    // and the once-per-round latch it keeps.
    public const string WitnessTheSealId = "witness_the_seal";
    public static readonly CounterId WitnessedThisRoundCounter = new("witnessed_this_round");

    public static IReadOnlyList<StatusData> All() =>
    [
        QueueAdvances(),
        StillMissingASignature(),
        SignaturePending(),
        PaperSealsWax(),
        OneRemainingSeal(),
        SealIntact(),
        WitnessTheSeal(),
        Marker(BothDirectionsMandatoryId, "Both Directions Mandatory"),
        Loophole(),
        Marker(ApplicantId, "The Applicant"),
        StillInForce(),
        Marker(StolenSandId, "Stolen Sand"),
        YourNumberIsFading(),
        Marker(StolenMinuteId, "Stolen Minute"),
        Counterclaim(),
        Sustained(),
        CorrectAgainstTheEvidence(),
        .. CardTypes.Select(Correction),
        Marker(OutstandingWarrantId, "Outstanding Warrant"),
        WarrantServed(),
        SeizeTheFiling(),
        BreakTheApproach(),
        Marker(YourNumberCameUpId, "Your Number Came Up"),
        CarbonCopies(),
        .. Appointments.Select(a => AppointmentDue(a.StatusId, a.Name, a.Due, a.Expiry)),
        AppointmentsAccelerated(),
        OfficeHours(),
        Marker(LostTimeLedgerId, "Lost Time"),
    ];

    // Devouring Waiting Room: the Room keeps the Lost Time ledger (so killing it erases the resource, while
    // killing the Moth Cloud leaves what was already lost). The marker is how the Moth and the encounter
    // trigger find the Room.
    public const string LostTimeLedgerId = "lost_time_ledger";
    public static readonly CounterId LostTimeCounter = new("lost_time");
    public const int LostTimeMaximum = 3;

    // Reopening-Hours Monolith: the office's own schedule. Closed windows bank whatever the player does; the
    // open window processes the backlog in one go.
    public const string OfficeHoursId = "office_hours";
    public static readonly CounterId PendingBusinessCounter = new("pending_business");
    public static readonly CounterId OfficeOpenCounter = new("office_open");
    private static readonly CounterId ClosedWindowsCounter = new("closed_windows");
    private static readonly CounterId OpenWindowsCounter = new("open_windows");

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
    public static readonly CounterId AppointmentDueCounter = new("appointment_due");
    public const string AppointmentsAcceleratedId = "appointments_accelerated";

    private static readonly CounterId AppointmentStartedCounter = new("appointment_started");

    public static readonly (string StatusId, string Name, int Due, Func<IEffectNode<TurnEndedTriggeredEffectContext>> Expiry)[]
        Appointments =
        [
            ("appointment_due_first", "Appointment Due (First)", 2, () => Expiry(damage: 7, fatigue: 1)),
            ("appointment_due_second", "Appointment Due (Second)", 3, () => Expiry(paperwork: 2, fatigue: 1)),
            ("appointment_due_final", "Appointment Due (Final)", 4, () => Expiry(damage: 15, fatigue: 1)),
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
        string id, string name, int due, Func<IEffectNode<TurnEndedTriggeredEffectContext>> expiry)
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

        var status = Passive(id, name, "TurnEnded", program);
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

        return Passive(AppointmentsAcceleratedId, "Appointments Accelerated", "RoundEnded", program);
    }

    // Duplicate Copy Mites: marker + the once-per-round latch, cleared at round end like its siblings.
    public const string CarbonCopiesId = "carbon_copies";
    public static readonly CounterId CopiedThisRoundCounter = new("copied_this_round");

    private static StatusData CarbonCopies()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CarbonCopiesId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, CopiedThisRoundCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(CarbonCopiesId, "Carbon Copies", "RoundEnded", program);
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

        return Passive(SeizeTheFilingId, "Seize the Filing", "RoundEnded", program);
    }

    // Warrant Bailiff: the marker its watcher finds it by, and the buff the watcher switches on and off.
    public const string OutstandingWarrantId = "outstanding_warrant";
    public const string WarrantServedId = "warrant_served";

    // Threshold Seizure Ward: marker + the once-per-round latch its encounter trigger reads.
    public const string SeizeTheFilingId = "seize_the_filing";
    public static readonly CounterId SeizedThisRoundCounter = new("seized_this_round");

    // Civic Battering Ram: Momentum, plus the two bits of bookkeeping "Break the Approach" needs.
    public const string BreakTheApproachId = "break_the_approach";
    public static readonly CounterId MomentumCounter = new("momentum");
    private static readonly CounterId HadBlockCounter = new("ram_had_block");
    private static readonly CounterId ApproachBrokenCounter = new("approach_broken_this_turn");

    // "Outstanding Warrant" is a plain buff the Bailiff wears while the player is 4 Paperwork deep — the
    // watcher that switches it lives on the encounter (EncounterPassives), because the condition is about the
    // PLAYER while the buff belongs to the enemy.
    private static StatusData WarrantServed() => new()
    {
        Id = WarrantServedId,
        NameKey = "Warrant Served",
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
    public static readonly string[] CardTypes = ["action", "form", "argument"];
    public const string CorrectAgainstTheEvidenceId = "correct_against_the_evidence";
    public static readonly CounterId CorrectedThisTurnCounter = new("corrected_this_turn");
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
    public static readonly CounterId CounterclaimUsedCounter = new("counterclaim_used");

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
    public static readonly CounterId SustainedThisRoundCounter = new("sustained_this_round");

    private static StatusData Sustained()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(SustainedId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, SustainedThisRoundCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(SustainedId, "Sustained", "RoundEnded", program);
    }

    // Inverted Hourglass: the marker its encounter trigger finds it by; the sand itself is a counter.
    public const string StolenSandId = "stolen_sand_passive";
    public static readonly CounterId StolenSandCounter = new("stolen_sand");

    // Minute Moth: same shape — marker + the counter its intent rule reads.
    public const string StolenMinuteId = "stolen_minute_passive";
    public static readonly CounterId StolenMinuteCounter = new("stolen_minute");

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

        return Passive("your_number_is_fading", "Your Number Is Fading", "TurnEnded", program);
    }

    // The hero carries this in every fight (EncounterMapper) so a program can ask "did this happen to the
    // player?" — selectors are structural and cannot name a side.
    public const string ApplicantId = "the_applicant";

    // Old Statute Ghost: marker + the two tracks of "Still in Force".
    public const string StillInForceId = "still_in_force_passive";
    public static readonly CounterId PrecedentCounter = new("precedent");
    public static readonly CounterId PrecedentLatchCounter = new("precedent_this_round");

    private static StatusData StillInForce()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(StillInForceId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, PrecedentLatchCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(StillInForceId, "Still in Force", "RoundEnded", program);
    }

    // Exception Imp: the marker its encounter trigger finds it by, plus the once-per-round latch it clears at
    // round end (like the Oath Candle's, and for the same reason — RoundEnded triggers have no bearer filter).
    public const string LoopholeId = "loophole";
    public static readonly CounterId LoopholeUsedCounter = new("loophole_used");

    private static StatusData Loophole()
    {
        var carriers = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(LoopholeId));

        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                carriers, LoopholeUsedCounter,
                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false));

        return Passive(LoopholeId, "Loophole", "RoundEnded", program);
    }

    // Contradictory Signpost: a pure marker so its encounter trigger can write the route counter to the
    // Signpost and nobody else (see EncounterPassives.BothDirectionsMandatory).
    public const string BothDirectionsMandatoryId = "both_directions_mandatory";
    public static readonly CounterId SignpostedRouteCounter = new("signposted_route");

    // A status that carries nothing but its own presence: the handle a cross-combatant trigger uses to find
    // one specific enemy, since selectors are structural and cannot name a combatant.
    private static StatusData Marker(string id, string name) => new()
    {
        Id = id,
        NameKey = name,
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

        return Passive(QueueAdvancesId, "The Queue Advances", "TurnStarted", program);
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

        return Passive(WitnessTheSealId, "Witness the Seal", "RoundEnded", program);
    }

    // Builds a hidden, non-stacking enemy status whose sole job is to carry one trigger program.
    private static StatusData Passive<TContext>(
        string id, string name, string trigger, EffectProgram<TContext> program) where TContext : class => new()
    {
        Id = id,
        NameKey = name,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [new StatusTriggerData(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()))],
    };
}
