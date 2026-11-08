using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV's normal bodies since the rework of 2026-11-04 (user): every rule one sentence, on the body, and every
// count a stack you can read. The Scale, the jars and Risen are the act's own words (ActFourLabyrinth); what is
// here is what each body adds.
//
//   Burdened   Sobek's Tithe (+4 a Burdened on its attacks), the Ushabti Hauler (a Stone for every surcharge
//              you pay), the Granary Thief (a hit through your Block steals a card), the Scarab Swarm (a hit
//              through your Block is 1 Burdened), the Tomb Robber (its loot is a card).
//   The Scale  the Rope-Stretcher, the Crooked Cubit, the Forty-Second Judge, the Eclipse Beetle, the Chiseller
//              and, in duos, Thoth's Ibis.
//   Risen      the Pyramidion Golem, the Shade of the Silted Field, the Keeper of the Four Vessels and the
//              False-Door Ka — four, and no more (content review E16, 2026-11-08).
//
// Since that review the Necropolis and the House of Linen speak their own words instead of rising:
//   the Half-Wrapped Mummy    Unfinished Wrapping: linen layers that swallow a hit each, one more each round
//                             unless you hit it three times.
//   the Bearer of the Natron  Drying of the Body: the Block you still have at the end of its turn becomes its own.
//   the Unlicensed Excavator  Spoils of the Tomb: hits through your Block steal Gold; after four turns it flees
//                             with them, killed first it gives them back.
//   the Keeper of the Western Gate  The Western Way: a fallen ally leaves a husk, and the Jackal raises it on its
//                             next turn unless the husk or the Jackal falls first.
public static partial class ActFour
{
    public const string SobeksTitheId = "sobeks_tithe";
    public const string ThiefRuleId = "the_granary_thief";
    public const string SackId = "the_sack";
    public const string SnatchOwedId = "snatch_owed";
    public const string InTheSackMark = "in_the_sack";
    public const int SackSize = 3;
    public const string SwarmRuleId = "the_swarm";
    public const string KeeperRuleId = "the_canopic_keeper";
    public const string FleeingId = "fleeing";
    public const string JackalRuleId = "guards_the_west";
    public const string LootRuleId = "every_object_requires_a_form";
    public const string StoneWorkId = "stone_work";
    public const string StoneId = "stone";
    public const string ApproachOfNoonId = "approach_of_noon";
    // Kept for Fade (ActFour.cs): no body preserves afflictions since 2026-11-04, so nothing is ever held.
    public const string DryWhatWouldDecayId = "dry_what_would_decay";

    // The Half-Wrapped Mummy (content review E16): linen instead of rising.
    public const string WrappingRuleId = "unfinished_wrapping";
    public const string LinenLayerId = "linen_layer";
    public const string UnwindingId = "unwinding";
    public const int LinenLayers = 4;
    public const int UnwindAt = 3;

    // The Bearer of the Natron (E16): your leftover Block dries into its own.
    public const string DryingRuleId = "drying_of_the_body";

    // The Unlicensed Excavator (E17): Gold for every hit through your Block, and a shaft to flee down.
    public const string EscapeShaftId = "escape_shaft";
    public const string StolenGoldId = "stolen_gold";
    public const string FledWithTheGoldId = "fled_with_the_gold";
    public const int RobberTurns = 4;
    public const int GoldPerHit = 20;
    public static CounterId GoldCarriedOff => new("tomb_gold_carried_off");

    // The Keeper of the Western Gate (E18): a fallen ally leaves a husk, and the Jackal guides it west.
    public const string HuskId = "western_husk";
    public const string HuskEnemyId = "western_husk";
    public const string HuskName = "Wrapped Husk";
    public const string GuidingWestId = "guiding_west";
    public const string GuidedWestId = "guided_west";
    public const string WestboundId = "westbound";
    public const int HuskHealth = 8;
    public const int WestwardRisePercent = 40;

    public const string FalseDoorEnemyId = "false_door";
    public const string TombGoldCardId = "tomb_gold";
    public const string TombRobberAloneEncounterId = "labyrinth_warrens_02";
    public const int TombGoldPay = 25;
    public const int RunawayRage = 4;

    private static CounterId StonesQuarried => new("stones_quarried");
    public static CounterId TombGoldPlayed => new("tomb_gold_played");

    public static EffectProgram<EnemyActionContext>? NormalIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "stone_hauler_ushabti.stone_blow" => StoneBlow(17, 3),
            "eclipse_scarab.black_noon" => BlackNoon(23),
            "fallen_capstone_golem.set_the_capstone" => Capstone(23),
            "runaway_laborer.hide_behind_the_gang" => DuckBehind(16, 8),
            "jackal_of_the_western_gate.westward_guidance" => WestwardGuidance(),
            _ when intentId == "rise" && Identities.Contains(enemyId) => RiseAction(),
            _ => null,
        };

    // ── intents ───────────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> StoneBlow(int damage, int perStone)
    {
        var self = CombatantTargetSelectors.Source;
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant,
                new AddExpression<EnemyActionContext>(Const(damage),
                    new MultiplyExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(StoneId)), Const(perStone)))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(StoneId)),
        ]));
    }

    private static EffectProgram<EnemyActionContext> BlackNoon(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ApplyStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(BurdenedId), Const(2),
                sourceSelector: CombatantTargetSelectors.Source),
            new RemoveStatusNode<EnemyActionContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(ApproachOfNoonId)),
        ]));

    private static EffectProgram<EnemyActionContext> Capstone(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new RemoveStatusNode<EnemyActionContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(CapstoneDueId)),
        ]));

    private static EffectProgram<EnemyActionContext> DuckBehind(int own, int ally) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(own)),
            new ForEachTargetEffectNode<EnemyActionContext>(
                CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(FleeingId)),
                new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(ally))),
        ]));

    // ── rules ─────────────────────────────────────────────────────────────────────────────────────────────

    // The Ushabti Hauler: every surcharge you pay is a Stone, quarried at its turn start.
    private static StatusData StoneWork()
    {
        var ushabti = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(ushabti, BurdenPaid, StonesQuarried);
        var quarry = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Above(unread, 0),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(ushabti, new StatusDefinitionId(StoneId), unread, sourceSelector: ushabti),
                MoveTheBookmark<TurnStartedTriggeredEffectContext>(ushabti, BurdenPaid, StonesQuarried),
            ]));
        return Face(StoneWorkId, "Compulsory Labour",
            "Every Burdened surcharge you pay is a Stone. Its next Dressed Stone deals 4 more per Stone and spends them.",
            triggers: [On("TurnStarted", quarry, StatusTriggerScope.Bearer)]);
    }

    // The Granary Thief: a hit of its that reaches you is a card owed, and it takes the card out of your next hand
    // (your hand is empty while it strikes). The card goes into its sack, which it gives back when it dies.
    private static StatusData GranaryThief()
    {
        var player = Applicant;
        var thief = Bearer(ThiefRuleId);
        var owed = new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
            Above(new EventAmountExpression<DamageDealtTriggeredEffectContext>(), 0),
            new ApplyStatusNode<DamageDealtTriggeredEffectContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(SnatchOwedId),
                N<DamageDealtTriggeredEffectContext>(1)));
        // The sack holds three at most: a thief that could take the whole deck would leave nothing to fight it with.
        var steal = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            AllOf(Above(new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.Hand), 0),
                new NotExpression<CardsDrawnTriggeredEffectContext>(
                    Above(StacksOn<CardsDrawnTriggeredEffectContext>(thief, SackId), SackSize - 1))),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    player, new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.Hand), new TagId(InTheSackMark)),
                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(player,
                    new FirstMarkedCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.Hand, new TagId(InTheSackMark)),
                    CardZone.BanishedPile),
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(thief, new StatusDefinitionId(SackId), N<CardsDrawnTriggeredEffectContext>(1)),
            ]));
        var draw = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            AllOf(IsPlayersTurn<CardsDrawnTriggeredEffectContext>(), Standing<CardsDrawnTriggeredEffectContext>(thief),
                Above(StacksOn<CardsDrawnTriggeredEffectContext>(thief, SnatchOwedId), 0)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new RepeatEffectNode<CardsDrawnTriggeredEffectContext>(StacksOn<CardsDrawnTriggeredEffectContext>(thief, SnatchOwedId), steal),
                new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(thief, new StatusDefinitionId(SnatchOwedId)),
            ]));
        return Face(ThiefRuleId, "Into the Sack",
            "Every hit that gets through your Block steals a card from your next hand, at most 3. Kill it to get them back.",
            triggers: [On("DamageDealt", owed, StatusTriggerScope.Bearer), On("CardsDrawn", draw)]);
    }

    // The sack opens: every stolen card back to the hand.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> OpenTheSack() =>
        new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(Applicant,
            new ForEachCardInZoneNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.IterationTarget, CardZone.BanishedPile,
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new IteratedCardExpression<CombatantDownedTriggeredEffectContext>(), new TagId(InTheSackMark), remove: true),
                    new MoveCardToZoneNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new IteratedCardExpression<CombatantDownedTriggeredEffectContext>(), CardZone.Hand),
                ]),
                markFilter: new TagId(InTheSackMark)));

    // The Scarab Swarm: every hit that reaches you weighs you down.
    private static StatusData TheSwarm() => Face(SwarmRuleId, "The Swarm",
        "Every hit of its that gets through your Block gives you 1 Burdened.",
        triggers:
        [
            On("DamageDealt", new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                Above(new EventAmountExpression<DamageDealtTriggeredEffectContext>(), 0),
                new ApplyStatusNode<DamageDealtTriggeredEffectContext>(Applicant, new StatusDefinitionId(BurdenedId),
                    N<DamageDealtTriggeredEffectContext>(1), sourceSelector: CombatantTargetSelectors.Source)),
                StatusTriggerScope.Bearer),
        ]);

    // The Runaway Ushabti: three turns of its own and it is gone, and its ally takes it badly.
    private static StatusData Fleeing()
    {
        var self = CombatantTargetSelectors.Source;
        var turn = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                StacksOn<TurnEndedTriggeredEffectContext>(self, FleeingId), ComparisonOperator.LessOrEqual, N<TurnEndedTriggeredEffectContext>(1)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(FleeingId)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget,
                        new StatusDefinitionId("strength"), N<TurnEndedTriggeredEffectContext>(RunawayRage))),
                new SetCombatantLifecycleStateNode<TurnEndedTriggeredEffectContext>(self, CombatantLifecycleState.Downed),
            ]),
            @else: new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(FleeingId), N<TurnEndedTriggeredEffectContext>(-1)));
        return Face(FleeingId, "Fleeing",
            "Turns until it flees. When it goes, its ally is enraged: +4 Strength.", stacks: true,
            triggers: [On("TurnEnded", turn, StatusTriggerScope.Bearer)]);
    }

    // ── the House of Linen and the Necropolis (content review E16–E18, 2026-11-08) ─────────────────────────

    // The Half-Wrapped Mummy: every layer of linen swallows one of your hits whole and is torn off doing it.
    // It wears its own layers, so the player counts what is left on the body. A hit the linen ate deals
    // nothing at all and is blocked by nothing — that is how the layer knows it was the one that took it.
    private static StatusData LinenLayer() => new()
    {
        Id = LinenLayerId,
        NameKey = "Linen Layer",
        DescriptionKey = "Each layer swallows one of your hits whole, and is torn off doing it.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 0,
                Priority: 1000),
        ],
        Triggers =
        [
            On("DamageTaken", new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                AllOf(
                    Wears<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.Source, PassiveStatuses.ApplicantId),
                    Eq(new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), N<DamageReceivedTriggeredEffectContext>(0)),
                    Eq(new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>(), N<DamageReceivedTriggeredEffectContext>(0)),
                    Above(new EventRequestedAmountExpression<DamageReceivedTriggeredEffectContext>(), 0)),
                new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                    new StatusDefinitionId(LinenLayerId), N<DamageReceivedTriggeredEffectContext>(-1))),
                StatusTriggerScope.Bearer),
        ],
    };

    // …and the rule that keeps wrapping it: each of your hits this round is a turn of Unwinding on the body; as its
    // own turn ends, fewer than three is one more layer (never more than four), and the count starts again.
    private static StatusData UnfinishedWrapping()
    {
        var self = CombatantTargetSelectors.Source;
        var hit = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            AllOf(
                Wears<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.Source, PassiveStatuses.ApplicantId),
                Above(new EventRequestedAmountExpression<DamageReceivedTriggeredEffectContext>(), 0)),
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                new StatusDefinitionId(UnwindingId), N<DamageReceivedTriggeredEffectContext>(1)));
        var wrap = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                AllOf(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(StacksOn<TurnEndedTriggeredEffectContext>(self, UnwindingId),
                        ComparisonOperator.Less, N<TurnEndedTriggeredEffectContext>(UnwindAt)),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(StacksOn<TurnEndedTriggeredEffectContext>(self, LinenLayerId),
                        ComparisonOperator.Less, N<TurnEndedTriggeredEffectContext>(LinenLayers))),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(LinenLayerId), N<TurnEndedTriggeredEffectContext>(1))),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(UnwindingId)),
        ]);
        return Face(WrappingRuleId, "Unfinished Wrapping",
            $"It starts with {LinenLayers} Linen Layers; each swallows one of your hits whole. At the end of its turn it "
            + $"wraps one more (at most {LinenLayers}), unless you hit it {UnwindAt} or more times this round.",
            triggers: [On("DamageTaken", hit, StatusTriggerScope.Bearer), On("TurnEnded", wrap, StatusTriggerScope.Bearer)]);
    }

    // The Bearer of the Natron: as its turn ends, the Block you still have — what its blow did not use up — dries
    // into its own. Copied, not taken: a duo partner acting after it still meets your Block. Paid at the end of
    // ITS turn because Block expires at the start of its owner's turn; gained any earlier it would be gone before
    // it stood between you and the Bearer.
    private static StatusData DryingOfTheBody()
    {
        var left = new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(Applicant, StandardCombatIds.BlockDefensivePool);
        return Face(DryingRuleId, "Drying of the Body",
            "At the end of its turn the Block you still have dries into its own: it gains that much Block.",
            triggers:
            [
                On("TurnEnded", new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Above(left, 0),
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, left)),
                    StatusTriggerScope.Bearer),
            ]);
    }

    // The Unlicensed Excavator: every hit of its that reaches you is Gold in its sack (Stolen Gold, on the body).
    // Combat cannot reach the run's purse, so the Gold only leaves you if it flees: the fight writes down what went
    // down the shaft and the run's ledger takes it after the fight (CutPrograms).
    private static StatusData SpoilsOfTheTomb() => Face(LootRuleId, "Spoils of the Tomb",
        $"Every hit of its that gets through your Block steals {GoldPerHit} Gold. Kill it before it flees and you keep "
        + "your Gold, and Tomb Gold joins your deck. Alone, it brings no other reward.",
        triggers:
        [
            On("DamageDealt", new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                AllOf(
                    Above(new EventAmountExpression<DamageDealtTriggeredEffectContext>(), 0),
                    Wears<DamageDealtTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, PassiveStatuses.ApplicantId)),
                new ApplyStatusNode<DamageDealtTriggeredEffectContext>(CombatantTargetSelectors.Source,
                    new StatusDefinitionId(StolenGoldId), N<DamageDealtTriggeredEffectContext>(GoldPerHit))),
                StatusTriggerScope.Bearer),
        ]);

    // Four of its turns, and down the shaft it goes with what it took (the fight's Downed rules settle the Gold).
    private static StatusData EscapeShaft()
    {
        var self = CombatantTargetSelectors.Source;
        var turn = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                StacksOn<TurnEndedTriggeredEffectContext>(self, EscapeShaftId), ComparisonOperator.LessOrEqual, N<TurnEndedTriggeredEffectContext>(1)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(EscapeShaftId)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(FledWithTheGoldId), N<TurnEndedTriggeredEffectContext>(1)),
                new SetCombatantLifecycleStateNode<TurnEndedTriggeredEffectContext>(self, CombatantLifecycleState.Downed),
            ]),
            @else: new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(EscapeShaftId), N<TurnEndedTriggeredEffectContext>(-1)));
        return Face(EscapeShaftId, "Escape Shaft",
            "Its turns until it slips down its shaft with the Gold it stole.", stacks: true,
            triggers: [On("TurnEnded", turn, StatusTriggerScope.Bearer)]);
    }

    // The Excavator goes down: killed, your Gold stays yours and its loot is a card after the fight; fled, the
    // Gold it carried is written down for the run to take, and there is no card.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> TombRobberDowned()
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        return new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            Wears<CombatantDownedTriggeredEffectContext>(downed, LootRuleId),
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                Wears<CombatantDownedTriggeredEffectContext>(downed, FledWithTheGoldId),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(Applicant, GoldCarriedOff,
                        StacksOn<CombatantDownedTriggeredEffectContext>(downed, StolenGoldId), relative: true),
                    new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(Applicant, LootClaimed,
                        N<CombatantDownedTriggeredEffectContext>(0), relative: false),
                ]),
                @else: new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(Applicant, LootClaimed,
                    N<CombatantDownedTriggeredEffectContext>(1), relative: true)));
    }

    // The Keeper of the Western Gate: the rule itself says it; the work is in the Downed rules and the Guidance.
    private static StatusData TheWesternWay() => Face(JackalRuleId, "The Western Way",
        $"When an ally falls it leaves a {HuskName} ({HuskHealth} HP). On its next turn the Jackal guides the husk "
        + $"west and the ally rises with {WestwardRisePercent} % of its HP. Destroy the husk first, or the Jackal.");

    private static ICombatantTargetSelector LivingHusks { get; } =
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(HuskId));

    private static ICombatantTargetSelector FallenWith(string status) =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(status), includeFallen: true);

    // A body the Jackal may still guide west: a fallen party that is not the Jackal, was not raised by it already,
    // and did not leave the fight on its own feet (fled down a shaft, or ran from the corvée).
    private static ICombatantTargetSelector GuidableFallen { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.Except(
                CombatantTargetSelectors.Downed(FallenWith(LabyrinthBodyId)),
                CombatantTargetSelectors.Union(
                    FallenWith(JackalRuleId), FallenWith(GuidedWestId), FallenWith(FledWithTheGoldId), FallenWith(FleeingId))));

    // What falls at the Western Gate. An ally (while the Jackal stands): a husk, and the Jackal turns to guide it.
    // A husk: if it was the last, the Jackal has nothing to guide. The Jackal: every husk crumbles with it.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> WesternGateDowned()
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        var jackal = Bearer(JackalRuleId);
        ICombatExpression<CombatantDownedTriggeredEffectContext, bool> Was(string status) =>
            Wears<CombatantDownedTriggeredEffectContext>(downed, status);
        ICombatExpression<CombatantDownedTriggeredEffectContext, bool> Not(ICombatExpression<CombatantDownedTriggeredEffectContext, bool> e) =>
            new NotExpression<CombatantDownedTriggeredEffectContext>(e);

        return new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                AllOf(Standing<CombatantDownedTriggeredEffectContext>(jackal), Was(LabyrinthBodyId), Not(Was(JackalRuleId)),
                    Not(Was(GuidedWestId)), Not(Was(FledWithTheGoldId)), Not(Was(FleeingId))),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    new SummonCombatantNode<CombatantDownedTriggeredEffectContext>(
                        StandardCombatIds.EnemyTeam, N<CombatantDownedTriggeredEffectContext>(HuskHealth),
                        new CombatantDefinitionId(HuskEnemyId), HuskName,
                        startingStatuses: [new StatusGrant(new StatusDefinitionId(HuskId), 1)]),
                    new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(jackal, new StatusDefinitionId(GuidingWestId),
                        N<CombatantDownedTriggeredEffectContext>(1)),
                ])),
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                AllOf(Was(HuskId), Eq(new CountTargetsExpression<CombatantDownedTriggeredEffectContext>(LivingHusks), N<CombatantDownedTriggeredEffectContext>(0))),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(GuidingWestId)),
                    new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(GuidingWestId)))),
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(Was(JackalRuleId),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(LivingHusks,
                    new SetHealthNode<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, N<CombatantDownedTriggeredEffectContext>(0)))),
        ]);
    }

    // Westward Guidance, the Jackal's whole turn while a husk waits: the fallen ally stands up (alive, at nothing
    // yet — which is how it is told from everyone else standing: it is the one at 0 HP), is marked, rises to 40 %
    // of its health free of debuffs, and the husk is spent. Raised once only (Guided West).
    private static EffectProgram<EnemyActionContext> WestwardGuidance()
    {
        var self = CombatantTargetSelectors.Source;
        var rising = Bearer(WestboundId);
        var raise = new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new SetCombatantLifecycleStateNode<EnemyActionContext>(GuidableFallen, CombatantLifecycleState.Alive),
            new ApplyStatusNode<EnemyActionContext>(CombatantTargetSelectors.LowestHealth(Parties), new StatusDefinitionId(WestboundId), Const(1)),
            new SetHealthNode<EnemyActionContext>(rising,
                new MaxExpression<EnemyActionContext>(Const(1),
                    new DivideExpression<EnemyActionContext>(
                        new MultiplyExpression<EnemyActionContext>(new CombatantMaxHealthExpression<EnemyActionContext>(rising), Const(WestwardRisePercent)),
                        Const(100)))),
            new RemoveStatusesByPolarityNode<EnemyActionContext>(rising, StatusPolarity.Debuff),
            new ApplyStatusNode<EnemyActionContext>(rising, new StatusDefinitionId(GuidedWestId), Const(1)),
            new RemoveStatusNode<EnemyActionContext>(rising, new StatusDefinitionId(WestboundId)),
            new ForEachTargetEffectNode<EnemyActionContext>(LivingHusks,
                new SetHealthNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(0))),
        ]);
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                AllOf(Above(new CountTargetsExpression<EnemyActionContext>(LivingHusks), 0),
                    Above(new CountTargetsExpression<EnemyActionContext>(GuidableFallen), 0)),
                raise),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(GuidingWestId)),
        ]));
    }

    private static IEnumerable<StatusData> NormalStatuses() =>
    [
        Face(SobeksTitheId, "Sobek's Tithe", "Its attacks deal 4 more for every Burdened you carry."),
        StoneWork(),
        Face(StoneId, "Stone", "Stones it hauled from your surcharges. Its next Dressed Stone deals 4 more per Stone.",
            StatusPolarity.Buff, stacks: true),
        GranaryThief(),
        Face(SnatchOwedId, "Snatching", "Cards it will take out of your next hand.", stacks: true),
        Face(SackId, "The Sack", "Cards it has stolen from you. Kill it and they come back to your hand.", stacks: true),
        TheSwarm(),
        SpoilsOfTheTomb(),
        EscapeShaft(),
        Face(StolenGoldId, "Stolen Gold",
            "Gold it has taken from you. Kill it and you keep it; if it flees, the Gold goes with it.", stacks: true),
        Face(FledWithTheGoldId, "Fled with the Gold", "It slipped down its shaft with the Gold it stole."),
        Face(KeeperRuleId, "Keeper of the Four Vessels",
            "It always keeps two jars beside it."),
        Fleeing(),
        TheWesternWay(),
        Face(GuidingWestId, "Guiding West",
            "A husk lies waiting: on its turn the Jackal guides it west and the fallen ally rises with 40 % of its HP. "
            + "Destroy the husk first, or the Jackal."),
        Face(GuidedWestId, "Guided West", "The Jackal raised it once already; if it falls again, it stays down."),
        Face(WestboundId, "Westbound", "Rising on the western road."),
        Face(HuskId, HuskName,
            "What a fallen ally left behind. Destroy it before the Jackal's turn, or the ally rises again."),
        UnfinishedWrapping(),
        LinenLayer(),
        Face(UnwindingId, "Unwinding",
            $"Your hits on it this round. At {UnwindAt} it gains no new linen layer this round.", stacks: true),
        DryingOfTheBody(),
        Face(ApproachOfNoonId, "Eclipse", "Crooked turns it has seen. At 3 comes Black Noon, and the count starts again.",
            stacks: true),
    ];

    // ── Tomb Gold ─────────────────────────────────────────────────────────────────────────────────────────

    // A card that pays: 25 Gold, 50 upgraded, counted in the fight and paid after it (combat cannot reach run
    // Gold). No Exhaust — looping it is a deck's reward in Act IV (user, 2026-11-04).
    public static IReadOnlyList<CardData> TombGoldCards() =>
    [
        TombGold(TombGoldCardId, "Tomb Gold", 1),
        TombGold(TombGoldCardId + "+", "Tomb Gold+", 2),
    ];

    private static CardData TombGold(string id, string name, int shares) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = $"Gain {shares * TombGoldPay} Gold (paid after the fight).",
        Costs = [],
        Tags = [new TagId(TombGoldCardId)],
        Program = new EffectProgram<CardPlayContext>(
            new SetCombatantCounterNode<CardPlayContext>(
                CombatantTargetSelectors.Source, TombGoldPlayed, new ConstantExpression<CardPlayContext>(shares), relative: true)),
    };
}
