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
        "wrong_window_scribe" => [NotThisCounter()],
        "oath_candle" => [WitnessTheSeal()],
        "contradictory_signpost" => [BothDirectionsMandatory()],
        // The Hourglass's two bottoms are pushed back by what the PLAYER plays, so the rule is about the
        // player's play and finds the glass by the marker it wears (ActTwo.DelayABottom).
        "hourglass_with_two_bottoms" => [ActTwo.DelayABottom()],
        // The Footnote writes down what its SOURCE does, and the announcement is the Source's moment — so
        // the rule is found from the announcer's side (ActTwo.WriteInTheMargin).
        "detached_footnote" => [ActTwo.WriteInTheMargin()],
        // Break the jar and the life it was holding is lost (ActTwo.BreakTheJar).
        "spare_life_jar" => [ActTwo.BreakTheJar()],
        "exception_imp" => Loophole(),
        "inverted_hourglass" => StolenSand(),
        "minute_moth" => [StolenMinute()],
        "number_ticket_wisp" => YourNumberCameUp(),
        "devouring_waiting_room" => [LostTime()],
        "iron_warrant_avatar" => [IssueComplianceOrder(), JudgeCompliance()],
        "inventory_lantern" => [ClearTheInventoryLatch(), MarkTheGoods()],
        "lock_cart" => [SeizeTheGoods()],
        "deputy_undersecretary" => DeputyUndersecretary.Triggers(),
        "queue_commissioner" => QueueCommissioner.Triggers(),
        "lord_sealkeeper" => LordSealkeeper.Triggers(),
        "municipal_dragon" => MunicipalDragon.Triggers(),
        "living_charter" => LivingCharter.Triggers(),
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
    public static IReadOnlyList<StartingStatusSpec> ActOpeningStatuses(IEnumerable<string> enemyIds)
    {
        var roster = enemyIds as IReadOnlyList<string> ?? [.. enemyIds];
        return [.. ActThree.HeroOpening(roster), .. ActFour.NecropolisOpening(roster)];
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
        ActFive.NansheEnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(ActFive.RationTabletId), 1)],
        // Act IV: the Chisel Spirit sets its chisel against the player's name from the first bell — a round
        // top-up alone would miss the opening round, which starts before the bodies in it are dressed.
        ActFour.ChiselSpiritEnemyId => ActFour.CartoucheOpening(ActFour.ChiselSpiritEnemyId),
        // Act II: the archive's rules live on the player, because it is the player's hand and the player's draw
        // they are about. Two misfiling enemies ask for the same marker and it merges, so a card is taken back
        // once however many shelves are in the room.
        "crabwise_shelf" or "volume_q_null" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1)],
        // The Cross-Reference's citations land on the player's hand, its Binding Guard is broken by what the
        // player plays, and its misfilings need the act's own take-back rule.
        Bosses.GrandCrossReference.PremiseId
            or Bosses.GrandCrossReference.AuthorityId
            or Bosses.GrandCrossReference.ConclusionId
            or Bosses.GrandCrossReference.EngineId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Bosses.GrandCrossReference.CrossReferenceRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Auditor's Documentation is the player's, its citations are answered from the player's hand,
        // and the answer to an Account is the player's to give — so the player carries the record. It
        // redacts on an unreconciled Identity, so the act's own rules ride along.
        Bosses.AuditorOfReturnedLives.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Bosses.AuditorOfReturnedLives.AuditorRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Curator's evidence is the player's own last turn, and Borrow One Minute is a card in the
        // player's hand — so the player carries the record and the minute. It misfiles too.
        Bosses.CuratorOfMisplacedHours.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Bosses.CuratorOfMisplacedHours.CuratorRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Warden reaches into the player's hand and two of its three keys are about the player's own
        // turn, so the player carries the rules that seal and release.
        Bosses.WardenOfSealedVolumes.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Bosses.WardenOfSealedVolumes.WardenRulesId), 1)],
        // The Catalogue compiles a record of the PLAYER, so the player carries the ledger the whole fight
        // writes into — and the act's take-back rule too, because an Established Opening redacts a card.
        Bosses.WhisperingCatalogue.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Bosses.WhisperingCatalogue.CatalogueRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Obituary's death clauses are kept in step by the player's own record and plays.
        Elites.ObituaryWithThreeEndings.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Elites.ObituaryWithThreeEndings.ObituaryRulesId), 1)],
        // The Clock changes when the PLAYER's effects happened, so the player carries the attribution.
        Elites.PresentlessClock.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Elites.PresentlessClock.ClockRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Drawer's nesting is about the player's card and the player's turn, and it misfiles too.
        Elites.DrawerOfInfiniteReturns.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Elites.DrawerOfInfiniteReturns.DrawerRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Magistrate asks its question at the start of the player's turn, so the player carries it.
        ActThree.MagistrateEnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(ActThree.JudgmentOfferId), 1)],
        // The tribunal's chain has to outlive any of its three reeds, so the player carries it: a rule kept
        // on a reed would stop working the moment that reed was cut.
        ActThree.HearingReedId or ActThree.RemandReedId or ActThree.RefusalReedId =>
            [new StartingStatusSpec(new StatusDefinitionId(ActThree.AppealChainId), 1)],
        // The Juniper grants its leave at the start of the player's turn, of the player's own hand, so the
        // player carries the asking.
        ActThree.JuniperEnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(ActThree.GrantedUseId), 1)],
        // The Oracle's riddle is asked of the player's hand, so the player carries it.
        Elites.BlackInkOracle.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Elites.BlackInkOracle.OracleRulesId), 1)],
        // The Silence's pair is about the player's hand and the player's plays, so the player carries it —
        // and the act's take-back rule too, because a single Word spoken misfiles a card.
        Elites.SilenceBetweenTwoWords.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(Elites.SilenceBetweenTwoWords.SilenceRulesId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
        ],
        // The Catalogue's ledger is the player's cards and the player's plays, so the player carries the rules.
        Elites.CatalogueOfUnwiseNames.EnemyId =>
            [new StartingStatusSpec(new StatusDefinitionId(Elites.CatalogueOfUnwiseNames.CatalogueRulesId), 1)],
        // The Colossus misfiles too, so the player carries the same take-back rule — plus the other half of
        // Open Aisle, which is about the player's hand and the player's plays.
        Elites.RollingStacksColossus.EnemyId =>
        [
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
            new StartingStatusSpec(new StatusDefinitionId(Elites.RollingStacksColossus.OpenAisleRulesId), 1),
        ],
        // The Reading Room's rules are about the player's hand and the player's plays, so the player carries
        // them: they watch the whole fight and answer what the player did.
        "unclaimed_reading_table" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.ReservedSeatId), 1)],
        "mute_margin" =>
        [
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ShrinkingMarginId), 1),
        ],
        "choir_of_unspoken_words" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.UnspokenWordId), 1)],
        // The Redaction Galleries answer what the PLAYER plays, so the player carries the rules.
        "palimpsest_husk" =>
        [
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.ArchiveRegulationsId), 1),
            new StartingStatusSpec(new StatusDefinitionId(ActTwo.OlderTextBeneathId), 1),
        ],
        "vacant_portrait" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.AbsenceBecomesVisibleId), 1)],
        // The register is about the player's own play history, so the player carries it — and it is only
        // there when the Expunged Name is.
        "expunged_name" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.NoLongerRecognizedId), 1)],
        "miscellany_index" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.ResidueId), 1)],
        "mnemonic_chain" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.RememberedVolumeId), 1)],
        "fatal_comma" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.ClausesId), 1)],
        "errata_doppelganger" =>
            [new StartingStatusSpec(new StatusDefinitionId(ActTwo.RevisionPassId), 1)],
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
                    // "an attack" from the Signpost's side of the fight is the player's offensive type: a Deed.
                    new FirstCardPlayedThisTurnHasTagExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId(Cards.CardAuthoring.DeedTag)),
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
