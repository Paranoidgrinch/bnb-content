using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's first boss: "You cannot finish everything. Decide what may officially remain unresolved."
//
// The Desk holds up to three Matters. Each is a status ON THE PLAYER whose stacks are its Due count, so the
// player reads their own workload and every program that has to judge it runs from a player-turn trigger,
// where the player is the single-selector Source. An unresolved Matter goes Overdue and leaves Backlog in its
// category; four Backlog (or 58 HP) fills the Desk and the Deputy moves to Executive Disposition, where the
// Backlog becomes Executive Files that harden it for the rest of the fight.
//
// Deviations from the design doc are listed in ADAPTATIONS.md.
public static class DeputyUndersecretary
{
    public const string DeputyId = "the_deputy";                 // identity + machinery, on the boss
    public const string RoutineId = "routine_administration";    // Phase-I mirror, on the player
    public const string DeskFullId = "desk_is_full";             // telegraphed transition, on the boss
    public const string ExecutiveId = "executive_disposition";   // Phase II, on the boss
    public const string FileComplaintId = "file_unanswered_complaint";
    public const string FileDelayId = "file_accumulated_delay";
    public const string FileDefectiveId = "file_defective_filing";
    public const string FilingNotedId = "filing_noted";          // once-per-turn latch, on the player
    public const string ReviewCardId = "file_the_request";

    public static readonly CounterId OpenMattersCounter = new("open_matters");
    public static readonly CounterId MatterIndexCounter = new("matter_index");
    public static readonly CounterId BlockThisTurnCounter = new("block_this_turn");
    public static readonly CounterId DelayBeatCounter = new("delay_beat");
    public static readonly CounterId BossBeatCounter = new("boss_beat");
    public static readonly CounterId CloseFileUsesCounter = new("close_file_uses");
    public static readonly CounterId BacklogTotalCounter = new("backlog_total");

    public const int DeskCapacity = 3;
    public const int MatterDue = 2;
    public const int BacklogTransition = 4;
    public const int BacklogMaximum = 5;
    public const int FileIntensityMaximum = 2;
    public const int BossBeats = 5;

    // A Matter: what it is called, which Backlog it leaves, and how the player clears it within one turn.
    // `Resolved` is null for the Request for Additional Review — that one is cleared by playing its card.
    public sealed record Matter(
        string StatusId,
        string Name,
        string Description,
        CounterId Backlog,
        string BacklogFile,
        Func<ICombatExpression<TurnEndedTriggeredEffectContext, bool>>? Resolved);

    public static readonly CounterId PerformanceBacklog = new("backlog_performance");
    public static readonly CounterId ExpenditureBacklog = new("backlog_expenditure");
    public static readonly CounterId ProceduralBacklog = new("backlog_procedural");

    public static readonly Matter[] Matters =
    [
        new("matter_complaint", "Matter: Complaint of Insufficient Action",
            "Deal at least 12 damage this turn, or the complaint stands.",
            PerformanceBacklog, FileComplaintId,
            () => new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new DamageDealtThisTurnExpression<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(12))),

        new("matter_petition", "Matter: Petition for Immediate Relief",
            "Gain at least 10 Block this turn, or the petition goes unanswered.",
            PerformanceBacklog, FileComplaintId,
            () => new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, BlockThisTurnCounter),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(10))),

        new("matter_review", "Matter: Request for Additional Review",
            "File the Request (1 Energy) before the review lapses.",
            ExpenditureBacklog, FileDelayId, Resolved: null),

        new("matter_response", "Matter: Notice of Missing Response",
            "Play at least one Attack and one Form this turn.",
            ProceduralBacklog, FileDefectiveId,
            () => new AndExpression<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId("attack")),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId("form")),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)))),
    ];

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheDeputy(),
        PassiveStatuses.NamedMarker(RoutineId, "Routine Administration",
            "The Desk is open: new Matters arrive each turn."),
        PassiveStatuses.NamedMarker(DeskFullId, "The Desk Is Full",
            "The Deputy's next action declares the matter urgent."),
        PassiveStatuses.NamedMarker(ExecutiveId, "Executive Disposition", "Phase II."),
        PassiveStatuses.NamedMarker(FilingNotedId, "Filing Noted", null),
        Stacked(FileComplaintId, "Executive File: Unanswered Complaint",
            "The Deputy gains 4 Block per intensity when its turn starts."),
        Stacked(FileDelayId, "Executive File: Accumulated Delay",
            "Every second player turn: 1 Fatigue, and 1 Doubt at intensity 2."),
        Stacked(FileDefectiveId, "Executive File: Defective Filing",
            "The first status you receive each turn gives the Deputy 4 Block per intensity."),
        .. Matters.Select(m => Stacked(m.StatusId, m.Name, m.Description, StatusPolarity.Debuff)),
    ];

    // The Matter's own action: a card, because a combat has no boss-context button. Playing it files the
    // requested review — the Energy IS the cost.
    public static CardData ReviewCard() => new()
    {
        Id = ReviewCardId,
        NameKey = "File the Request",
        DescriptionKey = "Resolve the Request for Additional Review.",
        Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
        Tags = [new TagId("form"), new TagId("matter")],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId("matter_review")),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0)),
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    new RemoveStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId("matter_review")),
                    new SetCombatantCounterNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, OpenMattersCounter,
                        new ConstantExpression<CardPlayContext>(-1), relative: true),
                }))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheDeskOpens(),
        TheDeskCloses(),
        BlockIsRecorded(),
        DefectiveFilingNoted(),
    ];

    // ── Phase I: the Desk ─────────────────────────────────────────────────────

    // At the start of the player's turn the Desk files what it can, and the Executive Files that act on the
    // player's own turn do their work. A full Desk creates nothing — the Deputy simply guards itself.
    private static EncounterTriggerData TheDeskOpens()
    {
        var player = CombatantTargetSelectors.Source;
        var deputy = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(DeputyId));
        var delayFiles = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(FileDelayId));

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Counter(CounterId counter) =>
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(player, counter);

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        // The Desk files up to TWO Matters a turn, taken in rotation — the two matter-creating intents of the
        // design, folded into the Desk itself. A slot already holding that Matter is skipped: the Desk moves on
        // rather than doubling a demand. Both guards read the same pre-write state, hence the `capacity - k`.
        IEffectNode<TurnStartedTriggeredEffectContext> Open(Matter matter, int freeSlotsNeeded) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        Counter(OpenMattersCounter), ComparisonOperator.LessOrEqual,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(DeskCapacity - freeSlotsNeeded)),
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        Stacks(matter.StatusId), ComparisonOperator.Equal,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(matter.StatusId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(MatterDue)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        player, OpenMattersCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                    // The review is filed with its own action in hand.
                    .. (matter.Resolved is null
                        ? new IEffectNode<TurnStartedTriggeredEffectContext>[]
                        {
                            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                player, new CardDefinitionId(ReviewCardId), CardZone.Hand,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        }
                        : []),
                ]));

        IEffectNode<TurnStartedTriggeredEffectContext> File(int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Counter(MatterIndexCounter), ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    Open(Matters[index], freeSlotsNeeded: 1),
                    Open(Matters[(index + 1) % Matters.Length], freeSlotsNeeded: 2),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        player, MatterIndexCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>((index + 2) % Matters.Length),
                        relative: false),
                }));

        var desk = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                Stacks(RoutineId), ComparisonOperator.Greater,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Counter(OpenMattersCounter), ComparisonOperator.Less,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(DeskCapacity)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [.. Enumerable.Range(0, Matters.Length).Select(File)]),
                // Desk-full rule: no fourth Matter, the Deputy guards instead.
                @else: new GainBlockNode<TurnStartedTriggeredEffectContext>(
                    deputy, new ConstantExpression<TurnStartedTriggeredEffectContext>(6))));

        // Accumulated Delay bites every second player turn; intensity 2 adds Doubt.
        var delay = new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(delayFiles,
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Counter(DelayBeatCounter), ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId("fatigue"),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new IterationTargetStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                new StatusDefinitionId(FileDelayId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            player, new StatusDefinitionId("doubt"),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                })));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    desk,
                    delay,
                    // The delay beat and the "one filing noted" latch are per player turn.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            Counter(DelayBeatCounter), ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            player, DelayBeatCounter,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false),
                        @else: new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            player, DelayBeatCounter,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(FilingNotedId)),
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // At the end of the player's turn every open Matter is judged: cleared, or one Due closer to Overdue —
    // and an Overdue Matter leaves its category's Backlog behind. Every branch reads the same pre-write state.
    private static EncounterTriggerData TheDeskCloses()
    {
        var player = CombatantTargetSelectors.Source;
        var deputy = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(DeputyId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Counter(CounterId counter) =>
            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(player, counter);

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        // Backlog is kept on the player (where it can be read to cap it) and mirrored onto the Deputy (where
        // its own turn-start check and the Executive Files read it).
        IEffectNode<TurnEndedTriggeredEffectContext> Bank(CounterId category) =>
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        Counter(category), ComparisonOperator.Less,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(FileIntensityMaximum)),
                    new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                    {
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            player, category, new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            deputy, category, new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                    })),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        Counter(BacklogTotalCounter), ComparisonOperator.Less,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(BacklogMaximum)),
                    new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                    {
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            player, BacklogTotalCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            deputy, BacklogTotalCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                    })),
            });

        IEffectNode<TurnEndedTriggeredEffectContext> Close(Matter matter) =>
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    player, new StatusDefinitionId(matter.StatusId)),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    player, OpenMattersCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1), relative: true),
            });

        IEffectNode<TurnEndedTriggeredEffectContext> Judge(Matter matter)
        {
            var overdue = new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                Close(matter),
                Bank(matter.Backlog),
            });

            // Due 1 → this tick makes it Overdue; anything higher simply moves one closer.
            var tick = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Stacks(matter.StatusId), ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                overdue,
                @else: new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    player, new StatusDefinitionId(matter.StatusId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)));

            var body = matter.Resolved is { } resolved
                ? new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(resolved(), Close(matter), @else: tick)
                : tick;

            return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Stacks(matter.StatusId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                body);
        }

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    .. Matters.Select(Judge),
                    // The turn's Block tally starts fresh.
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        player, BlockThisTurnCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                ])));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // The Petition asks for Block "gained this turn" — an amount the engine tracks for damage but not for
    // Block, so the encounter keeps its own tally.
    private static EncounterTriggerData BlockIsRecorded()
    {
        var gainer = CombatantTargetSelectors.EventTarget;

        var program = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<BlockGainedTriggeredEffectContext>(
                        gainer, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                new SetCombatantCounterNode<BlockGainedTriggeredEffectContext>(
                    gainer, BlockThisTurnCounter,
                    new EventAmountExpression<BlockGainedTriggeredEffectContext>(), relative: true)));

        return new EncounterTriggerData("BlockGained",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>()));
    }

    // Defective Filing: the first status the player picks up each turn is filed away as the Deputy's guard.
    private static EncounterTriggerData DefectiveFilingNoted()
    {
        var receiver = CombatantTargetSelectors.EventTarget;
        var files = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(FileDefectiveId));

        ICombatExpression<StatusAppliedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                receiver, new StatusDefinitionId(statusId));

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                        Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                    new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                        Stacks(FilingNotedId), ComparisonOperator.Equal,
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(0))),
                new SequenceEffectNode<StatusAppliedTriggeredEffectContext>(new IEffectNode<StatusAppliedTriggeredEffectContext>[]
                {
                    new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                        receiver, new StatusDefinitionId(FilingNotedId),
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    new ForEachTargetEffectNode<StatusAppliedTriggeredEffectContext>(files,
                        new GainBlockNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget,
                            new MultiplyExpression<StatusAppliedTriggeredEffectContext>(
                                new MinExpression<StatusAppliedTriggeredEffectContext>(
                                    new IterationTargetStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                                        new StatusDefinitionId(FileDefectiveId)),
                                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(FileIntensityMaximum)),
                                new ConstantExpression<StatusAppliedTriggeredEffectContext>(4)))),
                })));

        return new EncounterTriggerData("StatusApplied",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()));
    }

    // ── The Deputy's own machinery ────────────────────────────────────────────

    // The boss reads its own Backlog and its own wounds; both the primary trigger (4 Backlog) and the failsafe
    // (58 HP) raise the SAME telegraph, so the transition always runs through one visible declaration.
    private static StatusData TheDeputy()
    {
        var self = CombatantTargetSelectors.Source;
        var applicant = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.ApplicantId));

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        var onTurnStart = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Stacks(ExecutiveId), ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                // Phase I: is the Desk full?
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            Stacks(DeskFullId), ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new OrExpression<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(self, BacklogTotalCounter),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(BacklogTransition)),
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(self),
                                ComparisonOperator.LessOrEqual,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(58)))),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        self, new StatusDefinitionId(DeskFullId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)))));

        // The declaration has been handed down this turn: the Desk is cleared, the Backlog becomes Executive
        // Files, and everything that belonged to Phase I is taken off the table.
        IEffectNode<TurnEndedTriggeredEffectContext> OpenFile(CounterId backlog, string fileId) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, backlog),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    self, new StatusDefinitionId(fileId),
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, backlog),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(FileIntensityMaximum))));

        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            self, new StatusDefinitionId(DeskFullId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new SequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(DeskFullId)),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            self, new StatusDefinitionId(ExecutiveId),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        OpenFile(PerformanceBacklog, FileComplaintId),
                        OpenFile(ExpenditureBacklog, FileDelayId),
                        OpenFile(ProceduralBacklog, FileDefectiveId),
                        // The Desk is closed: no more Matters, and the open ones are simply dropped.
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            applicant, new StatusDefinitionId(RoutineId)),
                        .. Matters.Select(m => (IEffectNode<TurnEndedTriggeredEffectContext>)
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                                applicant, new StatusDefinitionId(m.StatusId))),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            applicant, OpenMattersCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    ])),
                // Phase II: the Unanswered Complaint guards its author. It is granted at the Deputy's turn END
                // because Block is CLEARED at a combatant's turn start, after that turn's triggers have run —
                // guarding at the start would wipe itself. Here the guard stands through the player's turn,
                // which is the point of it.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            self, new StatusDefinitionId(FileComplaintId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(
                        self,
                        new MultiplyExpression<TurnEndedTriggeredEffectContext>(
                            new MinExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                    self, new StatusDefinitionId(FileComplaintId)),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(FileIntensityMaximum)),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(4)))),
                // The rotation Phase II picks its actions from.
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, BossBeatCounter,
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, BossBeatCounter),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(BossBeats)),
                    relative: false),
            }));

        return new StatusData
        {
            Id = DeputyId,
            NameKey = "The Deputy Undersecretary",
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

    // ── Raw intents ───────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "close_an_unanswered_file" => CloseAnUnansweredFile(),
        "everything_outstanding" => EverythingOutstanding(),
        _ => null,
    };

    // Spends ONE File's intensity — the first that has any — and turns the closure into armour.
    private static EffectProgram<EnemyActionContext> CloseAnUnansweredFile()
    {
        var self = CombatantTargetSelectors.Source;

        ICombatExpression<EnemyActionContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(statusId));

        IEffectNode<EnemyActionContext> Spend(string fileId, IEffectNode<EnemyActionContext>? next) =>
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    Stacks(fileId), ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
                new ModifyStatusStacksNode<EnemyActionContext>(
                    self, new StatusDefinitionId(fileId), new ConstantExpression<EnemyActionContext>(-1)),
                @else: next);

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                Spend(FileComplaintId, Spend(FileDelayId, Spend(FileDefectiveId, null))),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId("strength"), new ConstantExpression<EnemyActionContext>(1)),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, CloseFileUsesCounter, new ConstantExpression<EnemyActionContext>(1), relative: true),
            }));
    }

    // The signature: 14, plus 2 for every File intensity still outstanding, capped at 20. It reads the Files
    // but never spends them.
    private static EffectProgram<EnemyActionContext> EverythingOutstanding()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        ICombatExpression<EnemyActionContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(statusId));

        var outstanding = new AddExpression<EnemyActionContext>(
            Stacks(FileComplaintId),
            new AddExpression<EnemyActionContext>(Stacks(FileDelayId), Stacks(FileDefectiveId)));

        return new EffectProgram<EnemyActionContext>(
            new DealDamageNode<EnemyActionContext>(player,
                new MinExpression<EnemyActionContext>(
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(14),
                        new MultiplyExpression<EnemyActionContext>(
                            outstanding, new ConstantExpression<EnemyActionContext>(2))),
                    new ConstantExpression<EnemyActionContext>(20))));
    }

    private static StatusData Stacked(
        string id, string name, string? description, StatusPolarity polarity = StatusPolarity.Neutral) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = polarity,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
