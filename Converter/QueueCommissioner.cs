using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's second boss: "You are inside the queue."
//
// The Queue Track is a status ON THE PLAYER whose stacks are the position (4 → 1, then the Counter). It ticks
// one closer at the start of each player turn — the beat right after the Commissioner's turn, which is where
// the design puts it — unless the Counter was closed. Reaching the Counter opens a Service Window: for that
// one turn the Commissioner is stripped of Block and takes more damage. Two served Windows (or 60 HP) open the
// Counter of Final Appeal and Phase II, where the Window itself becomes a choice.
//
// Everything the Queue has to READ lives on the player, because a player-turn trigger addresses the player
// with a single selector while a boss program can only write to them. Deviations: ADAPTATIONS.md.
public static class QueueCommissioner
{
    public const string CommissionerId = "the_commissioner";     // identity + machinery, on the boss
    public const string PositionId = "queue_position";           // stacks = position, on the player
    public const string PriorityId = "priority";                 // on the player, at most 1
    public const string ServiceId = "serving_you";               // the Window, on the player
    public const string BeingServedId = "being_served";          // damage amplifier, on the boss
    public const string ExpeditedId = "expedited_service";       // Phase-II choice, on the boss
    public const string CounterClosedId = "counter_closed";      // "no advance next turn", on the player
    public const string PushedBackId = "sent_to_the_back";       // pending backward move, on the player
    public const string ChoiceMadeId = "administrative_choice";  // one choice per turn, on the player
    public const string FinalCounterId = "counter_of_final_appeal"; // telegraphed transition, on the boss
    public const string PriorityQueueId = "priority_queue";      // Phase II, on the boss
    public const string LastNumberId = "last_number_called";     // telegraph, on the player
    public const string JustJoinedId = "just_joined_the_queue";  // skips the first advance, on the player

    public const string PetitionCardId = "petition_for_priority";
    public const string YieldCardId = "yield_your_place";
    public const string ExpediteCardId = "ask_for_expedited_service";

    public static readonly CounterId PriorityDamageCounter = new("priority_damage");
    public static readonly CounterId ServicesCounter = new("services_completed");
    public static readonly CounterId QueueBeatCounter = new("queue_beat");

    public const int StartPosition = 3;
    public const int PhaseTwoPosition = 2;
    public const int BackOfQueue = 4;
    public const int PhaseTwoBackOfQueue = 3;
    public const int PriorityThreshold = 14;
    public const int ServicesForTransition = 2;
    public const int QueueBeats = 5;

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheCommissioner(),
        Stacked(PositionId, "Queue Position", "How far you still are from the Counter."),
        PassiveStatuses.NamedMarker(PriorityId, "Priority", "Prevents the next backward move in the queue."),
        PassiveStatuses.NamedMarker(ServiceId, "Serving You", "The Counter is yours this turn."),
        PassiveStatuses.NamedMarker(CounterClosedId, "Counter Closed", "The queue does not move this turn."),
        PassiveStatuses.NamedMarker(PushedBackId, "Sent to the Back", "You lose a place when your turn begins."),
        PassiveStatuses.NamedMarker(ChoiceMadeId, "Administrative Choice Made", null),
        PassiveStatuses.NamedMarker(FinalCounterId, "Counter of Final Appeal",
            "The Commissioner's next action opens the final counter."),
        PassiveStatuses.NamedMarker(PriorityQueueId, "Priority Queue", "Phase II."),
        PassiveStatuses.NamedMarker(LastNumberId, "Last Number of the Day",
            "The Commissioner's next action is its heaviest."),
        PassiveStatuses.NamedMarker(JustJoinedId, "Just Joined the Queue", null),
        Amplifier(BeingServedId, "Being Served", 125),
        Amplifier(ExpeditedId, "Expedited Service", 115),
    ];

    public static IEnumerable<CardData> Cards() => [PetitionCard(), YieldCard(), ExpediteCard()];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheQueueMoves(),
        TheWindowCloses(),
        PriorityIsEarned(),
    ];

    // ── The player's side of the queue ────────────────────────────────────────

    // At the start of the player's turn the queue resolves: a pending push-back (unless Priority stops it),
    // then one step toward the Counter, and the Service Window when the Counter is reached. The Administrative
    // Choice is dealt out as cards for any ordinary turn.
    private static EncounterTriggerData TheQueueMoves()
    {
        var player = CombatantTargetSelectors.Source;
        var boss = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CommissionerId));

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        IEffectNode<TurnStartedTriggeredEffectContext> Apply(
            ICombatantTargetSelector target, string statusId, int stacks) =>
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                target, new StatusDefinitionId(statusId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(stacks));

        IEffectNode<TurnStartedTriggeredEffectContext> Remove(ICombatantTargetSelector target, string statusId) =>
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(target, new StatusDefinitionId(statusId));

        ICombatExpression<TurnStartedTriggeredEffectContext, bool> Wearing(string statusId) =>
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                Stacks(statusId), ComparisonOperator.Greater,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0));

        // A push-back the Commissioner ordered last turn: Priority spends itself to stop it. The back of the
        // queue is one shorter once the final counter opens.
        var pushBack = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Wearing(PushedBackId),
            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
            {
                Remove(player, PushedBackId),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    Wearing(PriorityId),
                    Remove(player, PriorityId),
                    @else: new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            Stacks(PositionId), ComparisonOperator.Less,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(BackOfQueue)),
                        Apply(player, PositionId, 1))),
            }));

        // The queue itself. At Position 1 the next step IS the Counter: the Window opens instead of a move.
        // The advance belongs to the END of the Commissioner's turn; running it at the player's turn start is
        // the same beat, EXCEPT on the first turn of the fight — which the joining marker skips.
        var advance = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Wearing(JustJoinedId),
            Remove(player, JustJoinedId),
            @else: new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Wearing(CounterClosedId),
            Remove(player, CounterClosedId),
            @else: new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Stacks(PositionId), ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    Remove(player, PositionId),
                    Apply(player, ServiceId, 1),
                    // The Window strips the Commissioner's guard and lays him open.
                    new ModifyDefensivePoolNode<TurnStartedTriggeredEffectContext>(
                        boss, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(-10)),
                    Apply(boss, BeingServedId, 1),
                    // Phase II lets the player ask for a faster, shallower window instead.
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PriorityQueueId)),
                        new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                            player, new CardDefinitionId(ExpediteCardId), CardZone.Hand,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                }),
                @else: new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        Stacks(PositionId), ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PositionId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(-1))))));

        // Outside a Window the player may take one administrative step. Both offers exhaust at the turn's end.
        var choice = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new NotExpression<TurnStartedTriggeredEffectContext>(Wearing(ServiceId)),
            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
            {
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    player, new CardDefinitionId(PetitionCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    player, new CardDefinitionId(YieldCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            }));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                Wearing(PassiveStatuses.ApplicantId),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    Remove(player, ChoiceMadeId),
                    pushBack,
                    advance,
                    choice,
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // The Window lasts exactly one player turn: at its end the player is sent back into the queue, the
    // Commissioner closes up again, and the served Window is counted toward the Counter of Final Appeal.
    private static EncounterTriggerData TheWindowCloses()
    {
        var player = CombatantTargetSelectors.Source;
        var boss = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CommissionerId));
        var phaseTwo = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PriorityQueueId));
        var expedited = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(ExpeditedId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        IEffectNode<TurnEndedTriggeredEffectContext> Remove(ICombatantTargetSelector target, string statusId) =>
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(target, new StatusDefinitionId(statusId));

        IEffectNode<TurnEndedTriggeredEffectContext> Apply(
            ICombatantTargetSelector target, string statusId, int stacks) =>
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                target, new StatusDefinitionId(statusId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(stacks));

        // Where the player lands afterwards: back to the standard place, or — with expedited service — right
        // behind the Counter. The Phase-II standard service also costs a Paperwork.
        var afterwards = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CountTargetsExpression<TurnEndedTriggeredEffectContext>(expedited),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                Remove(expedited, ExpeditedId),
                Apply(player, PositionId, 1),
            }),
            @else: new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CountTargetsExpression<TurnEndedTriggeredEffectContext>(phaseTwo),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    Apply(player, PositionId, PhaseTwoPosition),
                    Apply(player, new StatusDefinitionId("paperwork").value, 1),
                }),
                @else: Apply(player, PositionId, StartPosition)));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        Stacks(ServiceId), ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    Remove(player, ServiceId),
                    Remove(boss, BeingServedId),
                    afterwards,
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        boss, ServicesCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                })));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // Priority is bought with pressure: the first 14 HP the Commissioner loses in a player turn earn one, and
    // it is spent on the next backward move. The tally is kept on the Commissioner, where the damage lands.
    private static EncounterTriggerData PriorityIsEarned()
    {
        var boss = CombatantTargetSelectors.EventTarget;
        var applicant = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId));

        var tally = new AddExpression<DamageReceivedTriggeredEffectContext>(
            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(boss, PriorityDamageCounter),
            new EventAmountExpression<DamageReceivedTriggeredEffectContext>());

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                        boss, new StatusDefinitionId(CommissionerId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                {
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        boss, PriorityDamageCounter,
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                    // The tally above is an ENQUEUED write, so this hit has to add itself.
                    new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                        new AndExpression<DamageReceivedTriggeredEffectContext>(
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                tally, ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(PriorityThreshold)),
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
                                    boss, PriorityDamageCounter),
                                ComparisonOperator.Less,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(PriorityThreshold))),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            applicant, new StatusDefinitionId(PriorityId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1))),
                })));

        return new EncounterTriggerData("DamageTaken",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>()));
    }

    // ── The Commissioner's own machinery ──────────────────────────────────────

    private static StatusData TheCommissioner()
    {
        var self = CombatantTargetSelectors.Source;
        var applicant = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.ApplicantId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        var beat = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, QueueBeatCounter);

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                // The transition is telegraphed one action ahead: two Windows served, or 60 HP left.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(PriorityQueueId), ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                Stacks(FinalCounterId), ComparisonOperator.Equal,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                            new OrExpression<TurnEndedTriggeredEffectContext>(
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, ServicesCounter),
                                    ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(ServicesForTransition)),
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(self),
                                    ComparisonOperator.LessOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(60))))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(FinalCounterId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    // The telegraphed action has been handed down: the final counter opens behind it.
                    @else: new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(FinalCounterId), ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                        {
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                                self, new StatusDefinitionId(FinalCounterId)),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                self, new StatusDefinitionId(PriorityQueueId),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                            // A shorter queue: the player is placed at Position 2 of three.
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                                applicant, new StatusDefinitionId(PositionId)),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                applicant, new StatusDefinitionId(PositionId),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(PhaseTwoPosition)),
                        }))),

                // The per-turn damage tally that buys Priority starts fresh for the player's next turn.
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, PriorityDamageCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),

                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, QueueBeatCounter,
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            beat, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(QueueBeats)),
                    relative: false),

                // "Last Number of the Day" is always announced a full turn ahead: the beat the rotation is
                // about to reach decides whether the sign goes up or comes down.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(PriorityQueueId), ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            beat, ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(QueueBeats - 2))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        applicant, new StatusDefinitionId(LastNumberId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    @else: new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        applicant, new StatusDefinitionId(LastNumberId))),
            }));

        return new StatusData
        {
            Id = CommissionerId,
            NameKey = "The Queue Commissioner",
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

    // ── The player's cards ────────────────────────────────────────────────────

    // The Administrative Choice: two offers, one step. A latch keeps the turn to a single administrative act.
    private static CardData PetitionCard() => Choice(PetitionCardId, "Petition for Priority",
        "Move one place toward the Counter. Gain 1 Paperwork.",
        new IEffectNode<CardPlayContext>[]
        {
            new ApplyStatusNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId("paperwork"),
                new ConstantExpression<CardPlayContext>(1)),
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PositionId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(1)),
                new ModifyStatusStacksNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(PositionId),
                    new ConstantExpression<CardPlayContext>(-1))),
        });

    private static CardData YieldCard() => Choice(YieldCardId, "Yield Your Place",
        "Move one place away from the Counter. Gain 6 Block.",
        new IEffectNode<CardPlayContext>[]
        {
            new GainBlockNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(6)),
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PositionId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<CardPlayContext>(BackOfQueue)),
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(PositionId),
                    new ConstantExpression<CardPlayContext>(1))),
        });

    private static CardData Choice(string id, string name, string text, IEffectNode<CardPlayContext>[] effects) =>
        new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text + "\nOnly one administrative choice per turn.",
            Costs = [],
            Tags = [new TagId("form"), new TagId("queue")],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusStacksExpression<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(ChoiceMadeId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayContext>(0)),
                    new SequenceEffectNode<CardPlayContext>(
                    [
                        new ApplyStatusNode<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(ChoiceMadeId),
                            new ConstantExpression<CardPlayContext>(1)),
                        .. effects,
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };

    // Phase II's Window choice: a shallower opening in exchange for keeping the Counter within reach.
    private static CardData ExpediteCard() => new()
    {
        Id = ExpediteCardId,
        NameKey = "Ask for Expedited Service",
        DescriptionKey =
            "This Service Window opens the Commissioner by 15 % instead of 25 %, but afterwards you stand at "
            + "Position 1 instead of going back into the queue.",
        Costs = [],
        Tags = [new TagId("form"), new TagId("queue")],
        Program = new EffectProgram<CardPlayContext>(
            new ForEachTargetEffectNode<CardPlayContext>(
                CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(BeingServedId)),
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    new RemoveStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BeingServedId)),
                    new ApplyStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(ExpeditedId),
                        new ConstantExpression<CardPlayContext>(1)),
                }))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // ── Raw intents ───────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "reorder_the_line" => PushBack(8, "panic"),
        "reassign_the_queue" => PushBack(9, "panic"),
        _ => null,
    };

    // The push itself waits for the player's turn to begin — that is where Priority can be read and spent.
    private static EffectProgram<EnemyActionContext> PushBack(int damage, string status)
    {
        var player = CombatantTargetSelectors.EventTarget;

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(damage)),
                new ApplyStatusNode<EnemyActionContext>(
                    player, new StatusDefinitionId(status), new ConstantExpression<EnemyActionContext>(1)),
                new ApplyStatusNode<EnemyActionContext>(
                    player, new StatusDefinitionId(PushedBackId), new ConstantExpression<EnemyActionContext>(1)),
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

    // A window is an opening: while it is on the Commissioner, everything hits it harder.
    private static StatusData Amplifier(string id, string name, int percent) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = $"Takes {percent - 100} % more damage.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, percent, RestrictDamageKind: null),
        ],
        Triggers = [],
    };
}
