using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Converter;

// Cross-combatant enemy passives — reactions to PLAYER actions — authored as per-ENCOUNTER triggered effects
// (EncounterTriggerData), not owner-scoped status triggers (which fire only when the bearer is the event
// subject). Keyed by enemy id; EncounterMapper aggregates the triggers of an encounter's enemies onto the
// EncounterDefinition, so a trigger is active exactly when its enemy is in the fight. Programs are RAW
// EffectPrograms (the arc's richer expressions aren't in CombatNodeModel), serialized via CombatJson; they
// self-gate and target the enemy via AllEnemiesOfSource (source = the acting hero).
public static class EncounterPassives
{
    // The B&B card TYPES used for card-type sequencing (emitted as combat tags by CardMapper).
    private static readonly string[] CardTypes = { "action", "spell", "form" };

    public static IReadOnlyList<EncounterTriggerData> ForEnemy(string enemyId) => enemyId switch
    {
        "wrong_window_scribe" => [NotThisCounter()],
        "triplicate_examiner" => [ThreeCopiesRequired()],
        "oath_candle" => [WitnessTheSeal()],
        "contradictory_signpost" => [BothDirectionsMandatory()],
        "exception_imp" => Loophole(),
        "old_statute_ghost" => StillInForce(),
        "inverted_hourglass" => StolenSand(),
        "minute_moth" => [StolenMinute()],
        "sustaining_gavel" => [Sustained()],
        "warrant_bailiff" => OutstandingWarrant(),
        "threshold_seizure_ward" => SeizeTheFiling(),
        "number_ticket_wisp" => YourNumberCameUp(),
        "duplicate_copy_mite" => CarbonCopies(),
        "devouring_waiting_room" => [LostTime()],
        "living_petition_chorus" => [ClauseOffer()],
        "iron_warrant_avatar" => [IssueComplianceOrder(), JudgeCompliance()],
        "inventory_lantern" => [ClearTheInventoryLatch(), MarkTheGoods()],
        "lock_cart" => [SeizeTheGoods()],
        _ => Array.Empty<EncounterTriggerData>(),
    };

    // "Inventoried" (Inventory Lantern): a card the player has just drawn is marked as property. It rides on
    // CardsDrawn rather than the turn's start because the turn-start draw happens AFTER turn-start triggers —
    // at that moment the hand is still empty. A latch status on the player keeps it to one card per turn.
    private static EncounterTriggerData MarkTheGoods()
    {
        var player = CombatantTargetSelectors.Source;

        var mark = new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(
            // Only while a Cart is on the field to store the goods in.
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PassiveStatuses.LockCartId)),
            new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
            {
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    player,
                    new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.Hand),
                    PassiveStatuses.InventoriedMark),
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    player, new StatusDefinitionId(PassiveStatuses.InventoryPendingId),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
            }));

        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                            player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        // Not yet marked this turn …
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                                player, new StatusDefinitionId(PassiveStatuses.InventoryPendingId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        // … and the Cart still has room. The tally is kept on the player, the one combatant
                        // every part of this program can address by a single selector.
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                player, PassiveStatuses.SeizedCardsCounter),
                            ComparisonOperator.Less,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(PassiveStatuses.SeizureCapacity)))),
                mark));

        return new EncounterTriggerData("CardsDrawn",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()));
    }

    // The latch is released when the player's turn starts — turn-start triggers run before the draw, so the
    // turn's first draw finds it clear.
    private static EncounterTriggerData ClearTheInventoryLatch()
    {
        var player = CombatantTargetSelectors.Source;

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    player, new StatusDefinitionId(PassiveStatuses.InventoryPendingId))));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // "Seized" (Lock Cart): a marked card still in hand when the turn ends is taken away, and every seizure
    // hardens the Marshal. Cart and Marshal are addressed through their markers rather than an enclosing loop:
    // inside a card loop the iteration slot holds a CARD, so an outer combatant iteration target is gone.
    private static EncounterTriggerData SeizeTheGoods()
    {
        var player = CombatantTargetSelectors.Source;
        var marshals = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(PassiveStatuses.SeizureMarshalId));
        var card = new IteratedCardExpression<TurnEndedTriggeredEffectContext>();

        var seize = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new CardInstanceHasMarkExpression<TurnEndedTriggeredEffectContext>(card, PassiveStatuses.InventoriedMark),
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                new MoveCardToZoneNode<TurnEndedTriggeredEffectContext>(player, card, CardZone.ExhaustPile),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    player, PassiveStatuses.SeizedCardsCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                // The Marshal turns a successful seizure into force. The Cart's capacity of 2 keeps this well
                // under the design's cap of +4 without a second tally.
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    marshals, new StatusDefinitionId("strength"),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
            }));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
                            player, PassiveStatuses.SeizedCardsCounter),
                        ComparisonOperator.Less,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(PassiveStatuses.SeizureCapacity))),
                // A living Cart has to be there to take it.
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                        new StatusDefinitionId(PassiveStatuses.LockCartId)),
                    new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(player, CardZone.Hand, seize))));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // "Compliance Order" (Iron Warrant Avatar): the Avatar issues one visible, achievable demand at the start
    // of the player's turn. The orders take turns, so the same one never comes twice in a row.
    private static EncounterTriggerData IssueComplianceOrder()
    {
        var player = CombatantTargetSelectors.Source;
        var avatar = CombatantTargetSelectors.IterationTarget;
        var orders = PassiveStatuses.ComplianceOrders;

        IEffectNode<TurnStartedTriggeredEffectContext> Issue(int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                        avatar, PassiveStatuses.OrderIndexCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(orders[index].StatusId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        avatar, PassiveStatuses.OrderIndexCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>((index + 1) % orders.Length),
                        relative: false),
                }));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                        new StatusDefinitionId(PassiveStatuses.IronWarrantId)),
                    new SequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [.. Enumerable.Range(0, orders.Length).Select(Issue)]))));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // …and judges it when the turn ends: compliance strips 5 (Block first, the rest off its HP), refusal is
    // recorded as Contempt, up to 3.
    private static EncounterTriggerData JudgeCompliance()
    {
        var player = CombatantTargetSelectors.Source;
        var avatar = CombatantTargetSelectors.IterationTarget;
        var orders = PassiveStatuses.ComplianceOrders;

        IEffectNode<TurnEndedTriggeredEffectContext> Judge(PassiveStatuses.ComplianceOrder order) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(order.StatusId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        order.Fulfilled(),
                        // Compliance Credit 5: it comes off the Avatar's guard first and its health after.
                        new DealDamageNode<TurnEndedTriggeredEffectContext>(
                            avatar, new ConstantExpression<TurnEndedTriggeredEffectContext>(5)),
                        // Refusal is recorded, up to three counts.
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                    avatar, new StatusDefinitionId(PassiveStatuses.ContemptId)),
                                ComparisonOperator.Less,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(PassiveStatuses.ContemptMaximum)),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                avatar, new StatusDefinitionId(PassiveStatuses.ContemptId),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)))),
                    // The order is spent either way.
                    new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(order.StatusId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                }));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                        new StatusDefinitionId(PassiveStatuses.IronWarrantId)),
                    new SequenceEffectNode<TurnEndedTriggeredEffectContext>([.. orders.Select(o => Judge(o))]))));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // "Clause offer" (Living Petition Chorus): at the start of each player turn the Petition lays one clause on
    // the table — as a card in the player's hand, since a combat has no yes/no prompt. The three clauses take
    // turns, so each appears once per reading cycle.
    private static EncounterTriggerData ClauseOffer()
    {
        var player = CombatantTargetSelectors.Source; // the combatant whose turn started
        var petition = CombatantTargetSelectors.IterationTarget;
        var clauses = ClauseCards.All;

        IEffectNode<TurnStartedTriggeredEffectContext> Offer(int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                        petition, PassiveStatuses.ClauseIndexCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                        player, new CardDefinitionId(clauses[index].CardId), CardZone.Hand,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        petition, PassiveStatuses.ClauseIndexCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>((index + 1) % clauses.Length),
                        relative: false),
                }));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                        new StatusDefinitionId(PassiveStatuses.PetitionId)),
                    new SequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [.. Enumerable.Range(0, clauses.Length).Select(Offer)]))));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // "Lost Time" (Devouring Waiting Room): every point of Energy the player leaves unspent at the end of their
    // turn becomes Lost Time on the Room, up to 3. Energy that Fatigue took is simply not there to count — it
    // was removed at the turn's start — so the design's "freely unspent" needs no extra bookkeeping.
    private static EncounterTriggerData LostTime()
    {
        var player = CombatantTargetSelectors.Source; // the combatant whose turn ended
        var room = CombatantTargetSelectors.IterationTarget;
        var ledger = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
            room, PassiveStatuses.LostTimeCounter);

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                        new StatusDefinitionId(PassiveStatuses.LostTimeLedgerId)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        room, PassiveStatuses.LostTimeCounter,
                        new MinExpression<TurnEndedTriggeredEffectContext>(
                            new AddExpression<TurnEndedTriggeredEffectContext>(
                                ledger,
                                new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                                    player, StandardCombatIds.EnergyResource)),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(PassiveStatuses.LostTimeMaximum)),
                        relative: false))));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // "Carbon Copies" (Duplicate Copy Mites): the first time each round another enemy gains Bookworm, the Mites
    // guard themselves for 4. Same shape as the Oath Candle's Witness the Seal — the loop over
    // `alliesWithStatus(carbon_copies)` finds the Mites on the GAINER's side, the gainer carrying the marker is
    // the "another enemy" clause, and `iterationTarget` holds the once-per-round latch. Reading: "gained
    // Bookworm" is "the affected enemy now carries Bookworm", since a program cannot read the event's status.
    private static IReadOnlyList<EncounterTriggerData> CarbonCopies() =>
    [
        CarbonCopiesTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        CarbonCopiesTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
    ];

    private static EncounterTriggerData CarbonCopiesTrigger<TContext>(string trigger) where TContext : class
    {
        var mites = CombatantTargetSelectors.IterationTarget;
        var gainer = CombatantTargetSelectors.EventTarget;
        var marker = new StatusDefinitionId(PassiveStatuses.CarbonCopiesId);

        var body = new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(gainer, marker),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TContext>(0)),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(gainer, new StatusDefinitionId("bookworm")),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0))),
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(mites, PassiveStatuses.CopiedThisRoundCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0))),
            new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
            {
                new GainBlockNode<TContext>(mites, new ConstantExpression<TContext>(4)),
                new SetCombatantCounterNode<TContext>(mites, PassiveStatuses.CopiedThisRoundCounter,
                    new ConstantExpression<TContext>(1), relative: false),
            }));

        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(marker), body));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // "Your Number Came Up" (Number-Ticket Wisp): the Wisp burns out with the Panic it hands out — whenever
    // Panic leaves the player through its own DECAY it takes 4 direct damage; a cleanse does not feed it.
    // Decay is what the mirror can tell apart: Panic sheds exactly one stack per turn end, so a drop of
    // exactly 1 is the decay while a cleanse takes the whole pile at once.
    private static IReadOnlyList<EncounterTriggerData> YourNumberCameUp() =>
    [
        WispTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
        WispTrigger<StatusExpiredTriggeredEffectContext>("StatusExpired"),
        WispTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
        WispTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        WispTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
    ];

    private static EncounterTriggerData WispTrigger<TContext>(string trigger) where TContext : class
    {
        var wisp = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.EventTarget;
        var panic = new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId("panic"));
        var seen = new CombatantCounterExpression<TContext>(wisp, SeenCounter("panic"));

        var body = new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
        {
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new SubtractExpression<TContext>(seen, panic),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(1)),
                new DealDamageNode<TContext>(wisp, new ConstantExpression<TContext>(4))),
            new SetCombatantCounterNode<TContext>(wisp, SeenCounter("panic"), panic, relative: false),
        });

        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(PassiveStatuses.YourNumberCameUpId)),
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(player,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    body)));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // "Outstanding Warrant" (Warrant Bailiff): while the player is 4 Paperwork deep the Bailiff's attacks hit
    // for 5 more. A passive modifier cannot be conditional, so — as with the Unsigned Form Ghost — presence IS
    // the condition: this watcher switches the buff on and off whenever the player's statuses move. The
    // condition is about the PLAYER while the buff belongs to the enemy, hence an encounter trigger.
    private static IReadOnlyList<EncounterTriggerData> OutstandingWarrant() =>
    [
        WarrantTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        WarrantTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
        WarrantTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
        WarrantTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
        WarrantTrigger<StatusExpiredTriggeredEffectContext>("StatusExpired"),
    ];

    private static EncounterTriggerData WarrantTrigger<TContext>(string trigger) where TContext : class
    {
        var bailiff = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.EventTarget;
        var served = new StatusDefinitionId(PassiveStatuses.WarrantServedId);
        var paperwork = new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId("paperwork"));
        var buff = new CombatantStatusStacksExpression<TContext>(bailiff, served);

        var body = new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(paperwork, ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TContext>(4)),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(buff, ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
                new ApplyStatusNode<TContext>(bailiff, served, new ConstantExpression<TContext>(1))),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(buff, ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new ModifyStatusStacksNode<TContext>(bailiff, served, new ConstantExpression<TContext>(-1))));

        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(PassiveStatuses.OutstandingWarrantId)),
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(player,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    body)));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // "Seize the Filing" (Threshold Seizure Ward): the first Paperwork the PLAYER files on any enemy each round
    // is turned against them — that enemy gains 1 Bookworm, which will erase the filing at its turn start.
    // Reading: the enemy-facing status the Bureaucrat files is Paperwork, so "the target now carries Paperwork
    // and the filer is the player" stands in for "Paperwork was applied" (a program cannot read the event's
    // status id).
    private static IReadOnlyList<EncounterTriggerData> SeizeTheFiling() =>
    [
        SeizeTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        SeizeTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
    ];

    private static EncounterTriggerData SeizeTrigger<TContext>(string trigger) where TContext : class
    {
        var ward = CombatantTargetSelectors.IterationTarget;
        var filedOn = CombatantTargetSelectors.EventTarget;
        var filer = CombatantTargetSelectors.Source;

        var body = new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    // The filer is the player…
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(filer,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    // …the filing landed on an enemy (never on the player themselves)…
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(filedOn, new StatusDefinitionId("paperwork")),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0))),
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(ward, PassiveStatuses.SeizedThisRoundCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0))),
            new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
            {
                new ApplyStatusNode<TContext>(filedOn, new StatusDefinitionId("bookworm"),
                    new ConstantExpression<TContext>(1)),
                new SetCombatantCounterNode<TContext>(ward, PassiveStatuses.SeizedThisRoundCounter,
                    new ConstantExpression<TContext>(1), relative: false),
            }));

        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                    new StatusDefinitionId(PassiveStatuses.SeizeTheFilingId)),
                body));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // "Sustained" (Sustaining Gavel): the first time each round ANOTHER living enemy gains Block, the Gavel
    // copies half of it, rounded down. Same shape as the Oath Candle's Witness the Seal — the loop over
    // `alliesWithStatus(sustained)` is the "is the Gavel here / is the gainer on its side" gate and the handle
    // on the latch holder — but the Gavel guards ITSELF instead of topping the other body up, and the gainer
    // carrying the marker is the "no recursion" clause.
    private static EncounterTriggerData Sustained()
    {
        var marker = new StatusDefinitionId(PassiveStatuses.SustainedId);
        var gainer = CombatantTargetSelectors.EventTarget;
        var gavel = CombatantTargetSelectors.IterationTarget;

        var body = new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
            new AndExpression<BlockGainedTriggeredEffectContext>(
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<BlockGainedTriggeredEffectContext>(gainer, marker),
                    ComparisonOperator.Equal,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantCounterExpression<BlockGainedTriggeredEffectContext>(
                        gavel, PassiveStatuses.SustainedThisRoundCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0))),
            new SequenceEffectNode<BlockGainedTriggeredEffectContext>(new IEffectNode<BlockGainedTriggeredEffectContext>[]
            {
                new GainBlockNode<BlockGainedTriggeredEffectContext>(gavel,
                    new DivideExpression<BlockGainedTriggeredEffectContext>(
                        new EventAmountExpression<BlockGainedTriggeredEffectContext>(),
                        new ConstantExpression<BlockGainedTriggeredEffectContext>(2))),
                new SetCombatantCounterNode<BlockGainedTriggeredEffectContext>(
                    gavel, PassiveStatuses.SustainedThisRoundCounter,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(1), relative: false),
            }));

        var program = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ForEachTargetEffectNode<BlockGainedTriggeredEffectContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(marker), body));

        return new EncounterTriggerData("BlockGained",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>()));
    }

    // "Stolen Sand" (Inverted Hourglass): whenever Fatigue actually costs the player Energy, the Hourglass
    // banks a grain, up to 3. Fatigue spends exactly one stack when it fires (StatusMapper), so a DROP in the
    // player's Fatigue IS the moment the Energy went — the mirror that tells the Imp which status moved tells
    // the Hourglass when its own fired. NOTE: a status whose LAST stack is spent raises StatusExpired, not
    // StatusRemoved or StatusStacksChanged — the "it is finally gone" moment every mirror passive must listen for.
    private static IReadOnlyList<EncounterTriggerData> StolenSand() =>
    [
        StolenSandTrigger<StatusExpiredTriggeredEffectContext>("StatusExpired"),
        StolenSandTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
        StolenSandTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
        StolenSandTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        StolenSandTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
    ];

    private static EncounterTriggerData StolenSandTrigger<TContext>(string trigger) where TContext : class
    {
        var hourglass = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.EventTarget;
        var fatigue = new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId("fatigue"));
        var seen = new CombatantCounterExpression<TContext>(hourglass, SeenCounter("fatigue"));

        var body = new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
        {
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(fatigue, ComparisonOperator.Less, seen),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(hourglass, PassiveStatuses.StolenSandCounter),
                        ComparisonOperator.Less,
                        new ConstantExpression<TContext>(3))),
                new SetCombatantCounterNode<TContext>(hourglass, PassiveStatuses.StolenSandCounter,
                    new ConstantExpression<TContext>(1), relative: true)),
            new SetCombatantCounterNode<TContext>(hourglass, SeenCounter("fatigue"), fatigue, relative: false),
        });

        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(PassiveStatuses.StolenSandId)),
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(player,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    body)));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // "Stolen Minute" (Minute Moth): a player turn that ends on exactly 0 Energy hands the Moth a minute, up to
    // 2 — at which point its intent rule swaps in Wingbeat Delay (which spends them). The turn's own combatant
    // is the source here, so the applicant marker says whether it was the player's turn that ended.
    private static EncounterTriggerData StolenMinute()
    {
        var player = CombatantTargetSelectors.Source;
        var moth = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.StolenMinuteId));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                            player, StandardCombatIds.EnergyResource),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    moth, PassiveStatuses.StolenMinuteCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true)));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // The statuses the Old Statute Ghost keeps in force. Paperwork is deliberately not among them — it is the
    // bureaucracy's own instrument, not an expired penalty.
    private static readonly string[] ExpiringPenalties = { "panic", "doubt", "fatigue" };

    // "Still in Force" (Old Statute Ghost): the first time each round one of Panic / Doubt / Fatigue vanishes
    // from the player entirely, the Ghost gains 1 Precedent; at 2 it re-files one stack of the status that just
    // went and clears its Precedent. Like the Imp's Loophole this reads "which status" from a mirror of the
    // player's counts — and because the cash-out happens in the branch of the status that vanished, "the most
    // recently disappeared one" needs no extra memory at all.
    private static IReadOnlyList<EncounterTriggerData> StillInForce() =>
    [
        StillInForceTrigger<StatusExpiredTriggeredEffectContext>("StatusExpired"),
        StillInForceTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved"),
        StillInForceTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged"),
        StillInForceTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        StillInForceTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
    ];

    private static EncounterTriggerData StillInForceTrigger<TContext>(string trigger) where TContext : class
    {
        var ghost = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.EventTarget;

        ICombatExpression<TContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId(statusId));
        ICombatExpression<TContext, int> Counter(CounterId counter) =>
            new CombatantCounterExpression<TContext>(ghost, counter);

        var detections = ExpiringPenalties.Select(penalty => (IEffectNode<TContext>)
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(Counter(SeenCounter(penalty)),
                        ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                    new ComparisonExpression<TContext>(Stacks(penalty),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                {
                    new SetCombatantCounterNode<TContext>(ghost, PassiveStatuses.PrecedentLatchCounter,
                        new ConstantExpression<TContext>(1), relative: false),
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(Counter(PassiveStatuses.PrecedentCounter),
                            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(1)),
                        // Second precedent: the statute is re-imposed and the tally starts over.
                        new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                        {
                            new ApplyStatusNode<TContext>(player, new StatusDefinitionId(penalty),
                                new ConstantExpression<TContext>(1)),
                            new SetCombatantCounterNode<TContext>(ghost, PassiveStatuses.PrecedentCounter,
                                new ConstantExpression<TContext>(0), relative: false),
                        }),
                        new SetCombatantCounterNode<TContext>(ghost, PassiveStatuses.PrecedentCounter,
                            new ConstantExpression<TContext>(1), relative: false)),
                })));

        var body = new List<IEffectNode<TContext>>
        {
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(Counter(PassiveStatuses.PrecedentLatchCounter),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
                new SequenceEffectNode<TContext>(detections.ToList())),
        };
        body.AddRange(ExpiringPenalties.Select(penalty => (IEffectNode<TContext>)
            new SetCombatantCounterNode<TContext>(ghost, SeenCounter(penalty), Stacks(penalty), relative: false)));

        // Only what happens to the APPLICANT counts — the Ghost is deaf to statuses moving on its own side.
        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(PassiveStatuses.StillInForceId)),
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(player,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    new SequenceEffectNode<TContext>(body))));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // The negative statuses the enemy side files on the player in Act I. Loophole watches all of them.
    private static readonly string[] PlayerDebuffs = { "panic", "doubt", "paperwork", "fatigue" };

    // "Loophole" (Exception Imp): the first time each round the enemy side would apply a negative status to the
    // player, one stack of it is struck — a single-stack application is voided entirely — and the Imp gains 1
    // Strength for finding the exception.
    //
    // No interceptor: the engine's data-authored ones read the TARGET's statuses, and here the exception belongs
    // to an ENEMY. So the application is undone a beat later instead, which needs a way to tell WHICH status
    // arrived — a trigger program cannot read the event's status id. The Imp therefore mirrors the player's
    // debuff counts in its own counters: the status whose count is now HIGHER than the mirror is the one that
    // just landed. Every relevant event resyncs the mirror, including the reduction's own stack change, so it
    // converges even when a status is struck down to nothing.
    private static IReadOnlyList<EncounterTriggerData> Loophole() =>
    [
        LoopholeTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied", strike: true),
        LoopholeTrigger<StatusMergedTriggeredEffectContext>("StatusMerged", strike: true),
        LoopholeTrigger<StatusStacksChangedTriggeredEffectContext>("StatusStacksChanged", strike: false),
        LoopholeTrigger<StatusRemovedTriggeredEffectContext>("StatusRemoved", strike: false),
        LoopholeTrigger<StatusExpiredTriggeredEffectContext>("StatusExpired", strike: false),
    ];

    private static EncounterTriggerData LoopholeTrigger<TContext>(string trigger, bool strike) where TContext : class
    {
        var imp = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.EventTarget;
        var body = new List<IEffectNode<TContext>>();

        if (strike)
        {
            var strikes = PlayerDebuffs.Select(debuff => (IEffectNode<TContext>)
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId(debuff)),
                        ComparisonOperator.Greater,
                        new CombatantCounterExpression<TContext>(imp, SeenCounter(debuff))),
                    new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                    {
                        new ModifyStatusStacksNode<TContext>(player, new StatusDefinitionId(debuff),
                            new ConstantExpression<TContext>(-1)),
                        new SetCombatantCounterNode<TContext>(imp, PassiveStatuses.LoopholeUsedCounter,
                            new ConstantExpression<TContext>(1), relative: false),
                        new ApplyStatusNode<TContext>(imp, new StatusDefinitionId("strength"),
                            new ConstantExpression<TContext>(1)),
                    })));

            body.Add(new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(imp, PassiveStatuses.LoopholeUsedCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0)),
                new SequenceEffectNode<TContext>(strikes.ToList())));
        }

        // Resync the mirror last: whatever the player carries now is what the Imp remembers.
        body.AddRange(PlayerDebuffs.Select(debuff => (IEffectNode<TContext>)
            new SetCombatantCounterNode<TContext>(imp, SeenCounter(debuff),
                new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId(debuff)),
                relative: false)));

        var program = new EffectProgram<TContext>(
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
                    new StatusDefinitionId(PassiveStatuses.LoopholeId)),
                new SequenceEffectNode<TContext>(body)));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // The Imp's mirror of one player debuff.
    public static CounterId SeenCounter(string statusId) => new($"seen_{statusId}");

    // "Both Directions Mandatory" (Contradictory Signpost): the FIRST card the player plays each turn picks the
    // direction — an Attack takes the LEFT road (Dangerous Shortcut), anything else the RIGHT one (Long
    // Administrative Route). The choice is stored as a counter on the Signpost, which its intent rules read;
    // playing no card at all leaves the counter at 0 and it posts "No Route Listed" instead.
    private static EncounterTriggerData BothDirectionsMandatory()
    {
        var signpost = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.BothDirectionsMandatoryId));

        EffectProgram<CardPlayedTriggeredEffectContext> Program() =>
            new(new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new FirstCardPlayedThisTurnHasTagExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId("attack")),
                    Route(signpost, 1),
                    Route(signpost, 2))));

        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(Program(), CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }

    private static SetCombatantCounterNode<CardPlayedTriggeredEffectContext> Route(
        ICombatantTargetSelector signpost, int route) =>
        new(signpost, PassiveStatuses.SignpostedRouteCounter,
            new ConstantExpression<CardPlayedTriggeredEffectContext>(route), relative: false);

    // "Witness the Seal" (Oath Candle): the first time each round ANOTHER enemy gains Block, that enemy gains 3
    // more. Everything the program needs is expressed with selectors, since an encounter trigger has no filters
    // and cannot name a combatant:
    //   · `alliesWithStatus(witness_the_seal)` = the Candle, but only on the GAINER'S side — when the hero
    //     guards itself the loop finds nobody, so the hero is never witnessed;
    //   · the loop body runs once per Candle present, which is also the "is the Candle here" gate;
    //   · inside it, `iterationTarget` IS the Candle, so its once-per-round latch can be read and written;
    //   · the gainer carrying the marker means the Candle witnessed itself — the design's "no recursion".
    // The latch is cleared at RoundEnded by the marker status (PassiveStatuses.WitnessTheSeal).
    private static EncounterTriggerData WitnessTheSeal()
    {
        var marker = new StatusDefinitionId(PassiveStatuses.WitnessTheSealId);
        var gainer = CombatantTargetSelectors.EventTarget;
        var candle = CombatantTargetSelectors.IterationTarget;

        var body = new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
            new AndExpression<BlockGainedTriggeredEffectContext>(
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<BlockGainedTriggeredEffectContext>(gainer, marker),
                    ComparisonOperator.Equal,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantCounterExpression<BlockGainedTriggeredEffectContext>(
                        candle, PassiveStatuses.WitnessedThisRoundCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0))),
            new SequenceEffectNode<BlockGainedTriggeredEffectContext>(new IEffectNode<BlockGainedTriggeredEffectContext>[]
            {
                new GainBlockNode<BlockGainedTriggeredEffectContext>(
                    gainer, new ConstantExpression<BlockGainedTriggeredEffectContext>(3)),
                new SetCombatantCounterNode<BlockGainedTriggeredEffectContext>(
                    candle, PassiveStatuses.WitnessedThisRoundCounter,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(1), relative: false),
            }));

        var program = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ForEachTargetEffectNode<BlockGainedTriggeredEffectContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(marker), body));

        return new EncounterTriggerData("BlockGained",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>()));
    }

    // "Not This Counter": the first non-Junk card TYPE each turn is the "Wrong Window"; the first LATER card of
    // that same type makes the Scribe gain 5 Block. Encoded as: on the player's 2nd card of the turn's OPENING
    // type, the Scribe (all enemies of the card's source) gains 5 Block. Faithful simplification: the opening
    // type is literally the first card's type (Junk not skipped) — see ADAPTATIONS.md.
    private static EncounterTriggerData NotThisCounter() =>
        OnNthCardOfTheOpeningType(2,
            new GainBlockNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(5)));

    // "Three Copies Required": the turn's opening card TYPE is what the Examiner demands in triplicate — the
    // player's THIRD card of that type gives the Examiner 8 Block and the player 1 Doubt. Same simplification
    // as Not This Counter (opening type = literally the first card's type).
    private static EncounterTriggerData ThreeCopiesRequired() =>
        OnNthCardOfTheOpeningType(3,
            new SequenceEffectNode<CardPlayedTriggeredEffectContext>(new IEffectNode<CardPlayedTriggeredEffectContext>[]
            {
                new GainBlockNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(8)),
                // The card's player — the hero in a solo fight — takes the Doubt.
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new StatusDefinitionId("doubt"),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
            }));

    // The shared shape of the counter passives: fire `effect` when the player plays their Nth card of the type
    // that OPENED the turn. Exactly-N (not ≥N) makes it once per player turn on its own — no cooldown state.
    // The opening type isn't readable as a value, so the program ORs the per-type cases.
    private static EncounterTriggerData OnNthCardOfTheOpeningType(int n, IEffectNode<CardPlayedTriggeredEffectContext> effect)
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool>? condition = null;
        foreach (var type in CardTypes)
        {
            var tag = new TagId(type);
            var nthOfOpeningType = new AndExpression<CardPlayedTriggeredEffectContext>(
                new FirstCardPlayedThisTurnHasTagExpression<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, tag),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, tag),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(n)));
            condition = condition is null
                ? nthOfOpeningType
                : new OrExpression<CardPlayedTriggeredEffectContext>(condition, nthOfOpeningType);
        }

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(condition!, effect));

        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }
}
