using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

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
    // BnB's three primary card types, as the final design fixes them: Deed, Working, Rite. Every passive that
    // sequences on card TYPE reads these tags, which CardAuthoring puts on each card.
    private static readonly string[] CardTypes = { Cards.CardAuthoring.DeedTag, Cards.CardAuthoring.WorkingTag, Cards.CardAuthoring.RiteTag };

    public static IReadOnlyList<EncounterTriggerData> ForEnemy(string enemyId) => enemyId switch
    {
        "wrong_window_scribe" => [NotThisCounter(), NotThisCounterResets()],
        "oath_candle" => [WitnessTheSeal()],
        "contradictory_signpost" => [BothDirectionsMandatory(), TheSignpostIsLeftStanding()],
        // The Hourglass's two bottoms are pushed back by what the PLAYER plays, so the rule is about the
        // player's play and finds the glass by the marker it wears (ActTwo.DelayABottom).
        // The Footnote writes down what its SOURCE does, and the announcement is the Source's moment — so
        // the rule is found from the announcer's side (ActTwo.WriteInTheMargin).
        // Break the jar and the life it was holding is lost (ActTwo.BreakTheJar).
        "spare_life_jar" => [ActTwo.BreakTheJar()],
        "exception_imp" => Loophole(),
        "inverted_hourglass" => StolenSand(),
        "minute_moth" => [StolenMinute()],
        "queue_crier_homunculus" => [CuttingTheLine()],
        "number_ticket_wisp" => YourNumberCameUp(),
        Elites.WaitingRoom.EnemyId => Elites.WaitingRoom.Triggers(),
        Testator.EnemyId => Testator.Triggers(),
        Elites.DrawerOfInfiniteReturns.EnemyId => Elites.DrawerOfInfiniteReturns.Triggers(),
        Elites.ReturnBell.EnemyId => Elites.ReturnBell.Triggers(),
        Bosses.CuratorOfMisplacedHours.EnemyId => Bosses.CuratorOfMisplacedHours.Triggers(),
        Bosses.WhisperingCatalogue.EnemyId => Bosses.WhisperingCatalogue.Triggers(),
        // The court hears the case once, however many steps are standing: keyed on the bottom step alone.
        "lower_appellate_step" => [PassiveStatuses.HearTheCase()],
        "iron_warrant_avatar" => [IssueComplianceOrder(), JudgeCompliance()],
        "inventory_lantern" => [ClearTheInventoryLatch(), MarkTheGoods()],
        "lock_cart" => [SeizeTheGoods()],
        "deputy_undersecretary" => DeputyUndersecretary.Triggers(),
        "queue_commissioner" => QueueCommissioner.Triggers(),
        "lord_sealkeeper" => LordSealkeeper.Triggers(),
        "municipal_dragon" => MunicipalDragon.Triggers(),
        "living_charter" => LivingCharter.Triggers(),
        // Act IV: what falls with what — jars with their master, Anubis's watch with the Jackal, the sack with
        // the Thief (ActFour.LabyrinthTriggers). Every normal body brings it; it is idempotent.
        _ when ActFour.Identities.Contains(enemyId) => ActFour.LabyrinthTriggers(),
        // …and the elites' own: the Taskmaster's gang, Apep's coils, the Priest's jars, the brood, the robbers.
        _ when ActFour.EliteIdentities.Contains(enemyId) => ActFour.EliteTriggers(enemyId),
        // …and the bosses': the Pharaoh's traitors and Selket's vessels fall with them, her last vessel wraps her.
        _ when ActFour.BossIdentities.Contains(enemyId) => ActFour.BossTriggers(enemyId),
        // Act V: Nisaba's sentences leave with her, and the reed turns when the Final Line breaks.
        Gods.Nisaba.EnemyId => ActFive.Triggers(enemyId),
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

    // The applicant's opening statuses this enemy brings to the fight — the Final Notice Knight serves its
    // deadline ON THE PLAYER at the first bell. Encounter mapping and probes both read this, so a fight is
    // identical wherever it is built.
    // What the ACT serves on the player once per fight, whoever else is standing in it — Act III's customs
    // and its opening Safe-Conduct, and the same pair again wherever Act IV's False-Door Finder brings that
    // law back for a room (§3.9). Asked of the whole roster rather than of each body, or a duo would hand the
    // player two licences.
    // Act IV's jars open only a single normal fight, so the encounter's role is asked too (a probe has none).
    public static IReadOnlyList<StartingStatusSpec> ActOpeningStatuses(IEnumerable<string> enemyIds, string? role = null)
    {
        var roster = enemyIds as IReadOnlyList<string> ?? [.. enemyIds];
        return [.. ActThree.HeroOpening(roster), .. ActFour.HeroOpening(roster, role)];
    }

    public static IReadOnlyList<StartingStatusSpec> HeroOpeningStatuses(string enemyId) => enemyId switch
    {
        // The Deputy opens its Desk on the player: the phase mirror every Desk program gates on.
        "deputy_undersecretary" =>
            [new StartingStatusSpec(new StatusDefinitionId(DeputyUndersecretary.RoutineId), 1)],
        // The Commissioner puts the player in the queue: Position 3 of four.
        "queue_commissioner" =>
        [
            new StartingStatusSpec(new StatusDefinitionId(QueueCommissioner.PositionId), QueueCommissioner.StartPosition),
            new StartingStatusSpec(new StatusDefinitionId(QueueCommissioner.JustJoinedId), 1),
        ],
        // Act V: Nanshe's tablet is worn by the PLAYER, because what it does is let them READ her whole
        // Distribution — all three days of intents at once. A sight is a faculty of the one who looks.
        // Act V: what a god puts on the player (Nanshe's tablet and Day I's portion, Enlil's first decree).
        Gods.Nanshe.EnemyId or Gods.Enlil.EnemyId => ActFive.HeroOpening(enemyId),
        // Act II: the archive's rules live on the player, because it is the player's hand and the player's draw
        // they are about. Two misfiling enemies ask for the same marker and it merges, so a card is taken back
        // once however many shelves are in the room.
        "crabwise_shelf" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1)],
        // The Warden reaches into the player's hand and two of its three keys are about the player's own
        // turn, so the player carries the rules that seal and release.
        Bosses.WardenOfSealedVolumes.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Bosses.WardenOfSealedVolumes.WardenRulesId), 1)],
        // The Magistrate asks its question at the start of the player's turn, so the player carries it.
        ActThree.MagistrateEnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(ActThree.JudgmentOfferId), 1)],
        // The Oracle's riddle is asked of the player's hand, so the player carries it.
        Elites.BlackInkOracle.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Elites.BlackInkOracle.OracleRulesId), 1)],
        // The Catalogue's ledger is the player's cards and the player's plays, so the player carries the rules.
        Elites.CatalogueOfUnwiseNames.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Elites.CatalogueOfUnwiseNames.CatalogueRulesId), 1)],
        "mnemonic_chain" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.RememberedVolumeId), 1)],
        "corridor_in_the_wrong_edition" =>
        [
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.WrongEditionId), 1),
        ],
        _ => Array.Empty<StartingStatusSpec>(),
    };

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

    // …and judges it when the turn ends: compliance strips 2 (Block first, the rest off its HP), refusal gives it
    // 1 Strength.
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
                        // Compliance Credit 2 (5 until 2026-10-30, when obeying was simply the best card in the
                        // fight): it comes off the Avatar's guard first and its health after.
                        new DealDamageNode<TurnEndedTriggeredEffectContext>(
                            avatar, new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                        // Refusal makes it stronger: 1 Strength (until 2026-11-01 a Contempt of its own, which the
                        // player had to learn for this one fight — user: every rule in one clear sentence).
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            avatar, new StatusDefinitionId("strength"),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
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

    // "Stolen Minute" (Minute Moth): every player turn that ends on exactly 0 Energy gives the Moth 1 Strength and
    // 3 Block (user, 2026-11-01; it used to bank minutes for a Wingbeat). The Strength lands now; the Block is owed
    // (a counter) and paid at the Moth's own turn end, because Block is cleared at a combatant's turn start and
    // would be gone before it guarded anything. The turn's own combatant is the source here, so the applicant
    // marker says whether it was the player's turn that ended.
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
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        moth, new StatusDefinitionId("strength"), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        moth, PassiveStatuses.StolenMinuteCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                })));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // "Cutting the Line" (Queue-Crier Homunculus): the fourth card of a player turn cuts the line — 1 Panic. Exactly
    // the fourth, so it is once a turn on its own; the loop over the Crier's enemies-of-the-player is the gate that
    // it is on the table at all.
    private static EncounterTriggerData CuttingTheLine()
    {
        var player = CombatantTargetSelectors.Source;
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
                    ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(4)),
                new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.FirstTarget(CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                        new StatusDefinitionId(PassiveStatuses.CuttingTheLineId))),
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        player, new StatusDefinitionId("panic"), new ConstantExpression<CardPlayedTriggeredEffectContext>(1)))));
        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }

    // "Loophole" (Exception Imp): the first time each round the enemy side puts a debuff on the player, the Imp
    // gains 1 Strength. (Until 2026-11-01 it also struck a stack of that debuff out — half a gift to the player,
    // half to the Imp, which nobody could read; user: one clear sentence.) The loop over the Imp's allies of the
    // SOURCE finds it only when its own side did the filing; the event's target has to be the player.
    private static IReadOnlyList<EncounterTriggerData> Loophole() =>
    [
        LoopholeTrigger<StatusAppliedTriggeredEffectContext>("StatusApplied"),
        LoopholeTrigger<StatusMergedTriggeredEffectContext>("StatusMerged"),
    ];

    private static EncounterTriggerData LoopholeTrigger<TContext>(string trigger) where TContext : class
    {
        var imp = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.EventTarget;
        var program = new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0))),
                new ForEachTargetEffectNode<TContext>(
                    CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(PassiveStatuses.LoopholeId)),
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(imp, PassiveStatuses.LoopholeUsedCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TContext>(0)),
                        new SequenceEffectNode<TContext>(new IEffectNode<TContext>[]
                        {
                            new SetCombatantCounterNode<TContext>(imp, PassiveStatuses.LoopholeUsedCounter,
                                new ConstantExpression<TContext>(1), relative: false),
                            new ApplyStatusNode<TContext>(imp, new StatusDefinitionId("strength"),
                                new ConstantExpression<TContext>(1)),
                        })))));

        return new EncounterTriggerData(trigger,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // A body's mirror of one player status (the Wisp's Panic, the Hourglass's Fatigue).
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
                    // "an attack" from the Signpost's side of the fight is the player's offensive type: a Deed.
                    new FirstCardPlayedThisTurnHasTagExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId(Cards.CardAuthoring.DeedTag)),
                    Route(signpost, 1),
                    Route(signpost, 2))));

        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(Program(), CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }

    // THE SIGNPOST'S EASTER EGG (user, 2026-11-01): play nothing and it lists no route — a 1-damage shrug. Play
    // nothing for ten turns in a row and the Signpost, asked for directions by nobody, gives up: it falls over,
    // and the run is paid four times a fight's Gold for the patience (SignpostSpoilsProgram, read off the hero's
    // counter after the fight). The idle count lives on the Signpost and is read through a selector that names it,
    // since an iteration target cannot be read inside an expression.
    public const int SignpostPatience = 10;
    public static CounterId SignpostIdleCounter => new("signpost_idle_turns");
    public static CounterId SignpostConfusedCounter => new("signpost_confused");

    private static EncounterTriggerData TheSignpostIsLeftStanding()
    {
        var player = CombatantTargetSelectors.Source;
        var marker = new StatusDefinitionId(PassiveStatuses.BothDirectionsMandatoryId);
        var signposts = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(marker);
        var theSignpost = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, marker));
        var idle = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(theSignpost, SignpostIdleCounter);

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(player),
                        ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        // This idle turn is not counted yet (the write is enqueued), so the tenth is "nine so far".
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            idle, ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(SignpostPatience - 1)),
                        new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                        {
                            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                                player, SignpostConfusedCounter,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                            new DealDamageNode<TurnEndedTriggeredEffectContext>(
                                signposts, new ConstantExpression<TurnEndedTriggeredEffectContext>(9999), ignoresBlock: true),
                        }),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            signposts, SignpostIdleCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        signposts, SignpostIdleCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false))));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    public const string SignpostSpoilsProgramId = "signpost_spoils";
    public const int SignpostBonusGold = 96;   // three more of a fight's 25-40: four times the purse

    public static ITriggeredRunEffectDefinition SignpostSpoilsProgram() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Multiply(RunEventValues.CombatCounter(SignpostConfusedCounter.ToString()), RunExpr.Const(SignpostBonusGold))),
        ]);

    private static SetCombatantCounterNode<CardPlayedTriggeredEffectContext> Route(
        ICombatantTargetSelector signpost, int route) =>
        new(signpost, PassiveStatuses.SignpostedRouteCounter,
            new ConstantExpression<CardPlayedTriggeredEffectContext>(route), relative: false);

    // "Witness the Seal" (Oath Candle): the first time each round ANY enemy gains Block — the Candle itself
    // included since 2026-11-01 (user) — that enemy gains 3 more. Everything the program needs is expressed with
    // selectors, since an encounter trigger has no filters and cannot name a combatant:
    //   · `alliesWithStatus(witness_the_seal)` = the Candle, but only on the GAINER'S side — when the hero
    //     guards itself the loop finds nobody, so the hero is never witnessed;
    //   · the loop body runs once per Candle present, which is also the "is the Candle here" gate;
    //   · inside it, `iterationTarget` IS the Candle, so its once-per-round latch can be read and written —
    //     and it is closed BEFORE the 3 Block, so the Block it hands out is never witnessed again.
    // The latch is cleared at RoundEnded by the marker status (PassiveStatuses.WitnessTheSeal).
    private static EncounterTriggerData WitnessTheSeal()
    {
        var marker = new StatusDefinitionId(PassiveStatuses.WitnessTheSealId);
        var gainer = CombatantTargetSelectors.EventTarget;
        var candle = CombatantTargetSelectors.IterationTarget;

        var body = new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
            new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                new CombatantCounterExpression<BlockGainedTriggeredEffectContext>(
                    candle, PassiveStatuses.WitnessedThisRoundCounter),
                ComparisonOperator.Equal,
                new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
            new SequenceEffectNode<BlockGainedTriggeredEffectContext>(new IEffectNode<BlockGainedTriggeredEffectContext>[]
            {
                new SetCombatantCounterNode<BlockGainedTriggeredEffectContext>(
                    candle, PassiveStatuses.WitnessedThisRoundCounter,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(1), relative: false),
                new GainBlockNode<BlockGainedTriggeredEffectContext>(
                    gainer, new ConstantExpression<BlockGainedTriggeredEffectContext>(3)),
            }));

        var program = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ForEachTargetEffectNode<BlockGainedTriggeredEffectContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(marker), body));

        return new EncounterTriggerData("BlockGained",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>()));
    }

    // "Not This Counter" (Wrong-Window Scribe): the second card of any one type in a player turn makes the Scribe
    // gain 5 Block, once a turn (until 2026-11-01 only the type that OPENED the turn counted; user: one clear
    // sentence). The latch is a counter on the Scribe, cleared whenever a turn starts.
    private static EncounterTriggerData NotThisCounter()
    {
        var scribe = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PassiveStatuses.NotThisCounterId));
        var latched = CombatantTargetSelectors.FirstTarget(CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(PassiveStatuses.NotThisCounterId)));
        ICombatExpression<CardPlayedTriggeredEffectContext, bool>? pair = null;
        foreach (var type in CardTypes)
        {
            var two = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new TagId(type)),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(2));
            pair = pair is null ? two : new OrExpression<CardPlayedTriggeredEffectContext>(pair, two);
        }
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(pair!,
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(latched, PassiveStatuses.ScribeAnsweredCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0))),
                new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(scribe,
                    new SequenceEffectNode<CardPlayedTriggeredEffectContext>(new IEffectNode<CardPlayedTriggeredEffectContext>[]
                    {
                        new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget,
                            PassiveStatuses.ScribeAnsweredCounter, new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false),
                        new GainBlockNode<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(5)),
                    }))));
        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }

    // …and the Scribe's latch opens again whenever a turn starts.
    private static EncounterTriggerData NotThisCounterResets() =>
        new("TurnStarted", JsonSerializer.SerializeToElement(
            new EffectProgram<TurnStartedTriggeredEffectContext>(
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants,
                        new StatusDefinitionId(PassiveStatuses.NotThisCounterId)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget,
                        PassiveStatuses.ScribeAnsweredCounter, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false))),
            CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
}
