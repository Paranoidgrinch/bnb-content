using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 1 — The Road. The Hare posts a waymark each turn (2026-11-08; it wanted a Working first until then,
// which was the Queen Under the Hill's law), the Path's arrow names where your blows must go, and the Cairn throws
// back a stone for every hit it took.
public static partial class ActThree
{
    // ── Permit Hare — the Waymark ─────────────────────────────────────────────────────────────────────────

    public const string WaymarkId = "waymark";
    public const int WaymarkTop = 2;
    // The sign it shows this turn: one chip per cost, so the chip itself says the number.
    public static string WaymarkSignId(int cost) => $"waymark_{cost}";
    private const string WaymarkPickMark = "waymark_pick";

    public const string EveryDetourLeavesAStoneId = "every_detour_leaves_a_stone";
    public const string DetourStoneId = "detour_stone";

    public const string DestinationRuleId = "destination";
    public const string DestinationId = "the_destination";
    public const string AttendedId = "attended";
    public const string ArrowPointsElsewhereId = "arrow_points_elsewhere";


    // Whether the card just played was aimed at a party to the fight, and at which one.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> AimedAtAParty() =>
        new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(GreenDocketBodyId));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> AimedAt(string statusId) =>
        new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> Alive<TContext>(ICombatantTargetSelector who)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(who),
            ComparisonOperator.Greater,
            new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> ClearFromParties<TContext>(string statusId) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(
            Parties,
            new RemoveStatusNode<TContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(statusId)));

    // Since 2026-11-08 (content review E10): once your hand is dealt, the Hare reads the cost of one card in it at
    // random and posts it as the waymark — 0, 1 or 2 (2 meaning 2 or more). Your first card that turn must cost
    // exactly that, or you break its law. The sign is read off your own hand, so it can always be kept; the question
    // is whether you want to open with that card.
    public static StatusData Waymark()
    {
        var hare = Lawgiver(WaymarkId);
        var pick = FirstMarked<CardsDrawnTriggeredEffectContext>(CardZone.Hand, WaymarkPickMark);
        var picked = CardCost(pick);
        IEffectNode<CardsDrawnTriggeredEffectContext> Post(int cost) =>
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(hare, new StatusDefinitionId(WaymarkSignId(cost)),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1));
        var post = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            .. Enumerable.Range(0, WaymarkTop + 1).Select(cost => (IEffectNode<CardsDrawnTriggeredEffectContext>)
                new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(hare, new StatusDefinitionId(WaymarkSignId(cost)))),
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Is(new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand), ComparisonOperator.Greater, 0),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    MarkCard(new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand), WaymarkPickMark),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(Is(picked, ComparisonOperator.LessOrEqual, 0), Post(0),
                        @else: new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(Is(picked, ComparisonOperator.Equal, 1), Post(1),
                            @else: Post(2))),
                    MarkCard(pick, WaymarkPickMark, remove: true),
                ])),
        ]);

        // The first card of the turn, against the sign: a cost the sign does not show breaks the law.
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Misses(int cost) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wearing<CardPlayedTriggeredEffectContext>(hare, WaymarkSignId(cost)),
                cost < WaymarkTop
                    ? Is(PlayedCostValue, ComparisonOperator.NotEqual, cost)
                    : Is(PlayedCostValue, ComparisonOperator.Less, WaymarkTop));
        var offRoad = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Is(PlayedThisTurn, ComparisonOperator.Equal, 1),
                new OrExpression<CardPlayedTriggeredEffectContext>(Misses(0),
                    new OrExpression<CardPlayedTriggeredEffectContext>(Misses(1), Misses(2)))),
            Breach<CardPlayedTriggeredEffectContext>(hare));

        return Rule(WaymarkId, "Waymark",
            "Each turn it posts a cost from your hand: 0, 1 or 2+. Open the turn with a card of another cost and you break its law.",
            [AtTheOpeningHand(hare, post), WhenYouPlay(hare, offRoad)]);
    }

    public static IEnumerable<StatusData> WaymarkSigns() =>
    [
        .. Enumerable.Range(0, WaymarkTop + 1).Select(cost => Marker(WaymarkSignId(cost),
            cost < WaymarkTop ? $"Waymark: {cost} Energy" : $"Waymark: {WaymarkTop}+ Energy",
            cost < WaymarkTop
                ? $"Your first card this turn must cost {cost}, or you break the Hare's law."
                : $"Your first card this turn must cost {WaymarkTop} or more, or you break the Hare's law.")),
    ];

    // ── Elsewhere Path — the Arrow ────────────────────────────────────────────────────────────────────────
    //
    // "Each turn an arrow marks another enemy as Destination; ending the turn without hitting it is Trespass;
    // the Path is hard to hit while the arrow points elsewhere." Hitting the Destination turns the arrow back
    // to the Path, and the Path can be struck properly again until your next turn.
    public static StatusData Destination()
    {
        var path = Lawgiver(DestinationRuleId);
        var destination = Lawgiver(DestinationId);
        var candidate = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(DestinationRuleId)));

        var visit = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                AimedAt(DestinationId),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId(AttendedId),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        path, new StatusDefinitionId(ArrowPointsElsewhereId)),
                ])));

        var choose = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    Alive<TurnStartedTriggeredEffectContext>(path)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    ClearFromParties<TurnStartedTriggeredEffectContext>(DestinationId),
                    ClearFromParties<TurnStartedTriggeredEffectContext>(AttendedId),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Alive<TurnStartedTriggeredEffectContext>(candidate),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                candidate, new StatusDefinitionId(DestinationId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                path, new StatusDefinitionId(ArrowPointsElsewhereId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        ]),
                        // Nobody else left to point at: the arrow points at the Path itself.
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            path, new StatusDefinitionId(ArrowPointsElsewhereId))),
                ])));

        var reckon = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        PlayersTurn<TurnEndedTriggeredEffectContext>(),
                        Alive<TurnEndedTriggeredEffectContext>(path)),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Alive<TurnEndedTriggeredEffectContext>(destination),
                        new NotExpression<TurnEndedTriggeredEffectContext>(
                            Wears<TurnEndedTriggeredEffectContext>(destination, AttendedId)))),
                Violate<TurnEndedTriggeredEffectContext>(path, DestinationLaw)));

        return Rule(DestinationRuleId, "The Arrow",
            "Each turn the arrow names another enemy. Not hitting it this turn breaks the Path's law; until you do, the Path takes half damage.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    visit, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    choose, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    reckon, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData TheDestination() =>
        Marker(DestinationId, "Destination",
            "The Elsewhere Path's arrow points here. Hit it this turn, or you break the Path's law.");

    public static StatusData Attended() =>
        Marker(AttendedId, "Visited", "You went where the arrow pointed this turn.");

    public static StatusData ArrowPointsElsewhere() => new()
    {
        Id = ArrowPointsElsewhereId,
        NameKey = "The Arrow Points Elsewhere",
        DescriptionKey = "Takes half damage until you hit this turn's Destination.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 50),
        ],
        Triggers = [],
    };

    // ── Cairn of Stray Paths — the stones you knocked loose ──────────────────────────────────────────────
    //
    // Since 2026-11-03 (user): every hit you land on the Cairn knocks a stone loose, and on its turn it throws them
    // all back at you, 5 each. A turn nobody struck it, it braces for 10 instead; and every third turn of its own
    // it grows 2 Strength. The stones it holds are its own stacks, on its badge.
    // 3 as authored is the 5 asked for once Act III's damage factor (150 %) is laid over it.
    public const int StoneDamage = 3;
    public const int CairnBrace = 10;
    public const int CairnGrowth = 2;
    private static CounterId CairnTurnsCounter => new("cairn_turns");

    public static StatusData EveryDetourLeavesAStone()
    {
        // In a damage program the struck body is the event's target.
        var knock = new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(DetourStoneId),
            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1));
        return Rule(EveryDetourLeavesAStoneId, "Loose Stones",
            "Every hit on the Cairn knocks a stone loose; on its turn it throws them all at you, 5 each.",
            [On<DamageReceivedTriggeredEffectContext>("DamageTaken", knock)]);
    }

    public static StatusData DetourStone() => new()
    {
        Id = DetourStoneId,
        NameKey = "Loose Stones",
        DescriptionKey = "On its turn the Cairn throws each one at you for 5.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Its one action: the stones it holds, or a brace; and every third turn a little more weight.
    private static EffectProgram<EnemyActionContext> Stonefall()
    {
        var self = CombatantTargetSelectors.Source;
        var stones = new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(DetourStoneId));
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                Is(stones, ComparisonOperator.Greater, 0),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new RepeatEffectNode<EnemyActionContext>(stones,
                        new DealDamageNode<EnemyActionContext>(Applicant, new ConstantExpression<EnemyActionContext>(StoneDamage))),
                    new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(DetourStoneId)),
                ]),
                @else: new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(CairnBrace))),
            new SetCombatantCounterNode<EnemyActionContext>(self, CairnTurnsCounter, new ConstantExpression<EnemyActionContext>(1), relative: true),
            new ConditionalEffectNode<EnemyActionContext>(
                Is(new RemainderExpression<EnemyActionContext>(CounterOn<EnemyActionContext>(self, CairnTurnsCounter),
                    new ConstantExpression<EnemyActionContext>(3)), ComparisonOperator.Equal, 0),
                new ApplyStatusNode<EnemyActionContext>(self, new StatusDefinitionId("strength"),
                    new ConstantExpression<EnemyActionContext>(CairnGrowth))),
        ]));
    }

    // How many real cards have been played this turn — the Junk the fight hands you does not count as a use.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> NonJunkPlayedThisTurn(
        ICombatantTargetSelector player) =>
        new SubtractExpression<CardPlayedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
            new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                player, new TagId(Cards.CardAuthoring.JunkTag)));
}
