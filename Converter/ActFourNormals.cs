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
//   Risen      the Half-Wrapped Mummy, the Natron Priest, the Pyramidion Golem, the Drowned Farmer, the Canopic
//              Keeper, the False-Door Ka, and whoever stands beside the Anubis Jackal.
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
            "Every Burdened surcharge you pay is a Stone. Its next Stone Blow deals 4 more per Stone and spends them.",
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

    // The Anubis Jackal: at the first bell every ally that cannot rise yet is watched, and rises once.
    private static StatusData GuardsTheWest()
    {
        var jackal = Bearer(JackalRuleId);
        var body = CombatantTargetSelectors.IterationTarget;
        var risenAlready = new[] { RisenId, RisenNatronId, RisenGolemId, RisenDoorId, RisenFarmerId, RisenKeeperId, RisenByJackalId }
            .Select(id => Wears<TurnStartedTriggeredEffectContext>(body, id))
            .Aggregate((a, b) => new OrExpression<TurnStartedTriggeredEffectContext>(a, b));
        var watch = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(jackal),
                Eq<TurnStartedTriggeredEffectContext>(new RoundNumberExpression<TurnStartedTriggeredEffectContext>(), N<TurnStartedTriggeredEffectContext>(1))),
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(JackalRuleId)),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new NotExpression<TurnStartedTriggeredEffectContext>(risenAlready),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(body, new StatusDefinitionId(RisenByJackalId), N<TurnStartedTriggeredEffectContext>(1)))));
        return Face(JackalRuleId, "Watched by Anubis",
            "While the Jackal lives, an ally that dies rises again with 40 % of its HP, once.",
            triggers: [On("TurnStarted", watch)]);
    }

    private static IEnumerable<StatusData> NormalStatuses() =>
    [
        Face(SobeksTitheId, "Sobek's Tithe", "Its attacks deal 4 more for every Burdened you carry."),
        StoneWork(),
        Face(StoneId, "Stone", "Stones it hauled from your surcharges. Its next Stone Blow deals 4 more per Stone.",
            StatusPolarity.Buff, stacks: true),
        GranaryThief(),
        Face(SnatchOwedId, "Snatching", "Cards it will take out of your next hand.", stacks: true),
        Face(SackId, "The Sack", "Cards it has stolen from you. Kill it and they come back to your hand.", stacks: true),
        TheSwarm(),
        Face(LootRuleId, "Tomb Gold",
            "Kill it and Tomb Gold joins your deck. Alone, it brings no other reward."),
        Face(KeeperRuleId, "The Canopic Keeper",
            "It always keeps two jars beside it."),
        Fleeing(),
        GuardsTheWest(),
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
