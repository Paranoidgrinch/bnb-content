using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 3 — Tollwater. Since 2026-11-03 (user): a snail that charges for a busy turn, a fish that leaves
// you be once you have kept its law three turns running, and (since 2026-11-08) a ford that asks a toll of you
// every turn and lets you choose which bank to pay.
public static partial class ActThree
{
    public const string PaymentAccordingToCharterId = "payment_according_to_charter";
    public const string OathAcceptedId = "oath_accepted";
    public const string FishNoDeedsId = "fish_law_no_deeds";
    public const string FishLowGuardId = "fish_law_low_guard";
    public const string OathKeptId = "oath_kept";
    public const string TollOnBothBanksId = "toll_on_both_banks";
    public const string TollPaidId = "toll_paid";
    public const string TollInKindId = "toll_in_kind";
    public const string TollInBloodId = "toll_in_blood";
    public const string FordTollTag = "ford_toll";

    public const int SnailCardLimit = 3;
    public const int OathTurnsToLeave = 3;
    public const int FishGuardLimit = 10;
    public const int TollInBloodHealth = 5;
    public const int TollPaidDamagePercent = 50;
    // The round in which a toll was last paid, on the player.
    public static CounterId FordTollRoundCounter => new("ford_toll_round");

    // ── Charter-Shell Snail ───────────────────────────────────────────────────────────────────────────────
    public static StatusData PaymentAccordingToCharter() =>
        LawOnPlay(PaymentAccordingToCharterId, "A Toll on Haste",
            "Playing a third card in a turn breaks its law.",
            Is(PlayedThisTurn, ComparisonOperator.GreaterOrEqual, SnailCardLimit));

    // ── Streamside Oath-Fish ──────────────────────────────────────────────────────────────────────────────
    //
    // Its law follows what it is about to do: while it means to guard, no Deeds; while it means to strike, keep your
    // Block to 10 or less. Each intent sets the law for the player's next turn (the two alternate, starting with
    // the guard). Keep its law three turns running and it leaves in peace, counted as defeated.
    public static StatusData OathAccepted()
    {
        var fish = Lawgiver(OathAcceptedId);
        var player = CombatantTargetSelectors.Source;

        var deed = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    Alive<CardPlayedTriggeredEffectContext>(fish),
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        Wearing<CardPlayedTriggeredEffectContext>(fish, FishNoDeedsId),
                        PlayedIs(Cards.CardAuthoring.DeedTag))),
                Breach<CardPlayedTriggeredEffectContext>(fish)));

        var reckon = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(), Alive<TurnEndedTriggeredEffectContext>(fish)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Wearing<TurnEndedTriggeredEffectContext>(fish, FishLowGuardId),
                        Is(new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                            player, StandardCombatIds.BlockDefensivePool), ComparisonOperator.Greater, FishGuardLimit)),
                    Breach<TurnEndedTriggeredEffectContext>(fish)),
                // The oath: a turn kept is a mark on the fish, a turn broken wipes them.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Wearing<TurnEndedTriggeredEffectContext>(fish, LawBrokenThisTurnId),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(fish, new StatusDefinitionId(OathKeptId)),
                    @else: new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            fish, new StatusDefinitionId(OathKeptId), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            Is(new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(fish, new StatusDefinitionId(OathKeptId)),
                                ComparisonOperator.GreaterOrEqual, OathTurnsToLeave),
                            new SetHealthNode<TurnEndedTriggeredEffectContext>(fish, new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                    ])),
            ]));

        return Rule(OathAcceptedId, "The Oath of the Stream",
            "While it guards: no Deeds. While it strikes: no more than 10 Block. Keep its law 3 turns running and it leaves, defeated.",
            [
                new StatusTriggerData("CardPlayed", System.Text.Json.JsonSerializer.SerializeToElement(
                    deed, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()), StatusTriggerScope.Anywhere),
                On("TurnEnded", reckon, StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData FishNoDeeds() =>
        Marker(FishNoDeedsId, "Law: No Deeds", "It is about to guard. Playing a Deed this turn breaks its law.");

    public static StatusData FishLowGuard() =>
        Marker(FishLowGuardId, "Law: Low Guard", "It is about to strike. Ending this turn on more than 10 Block breaks its law.");

    public static StatusData OathKept() => new()
    {
        Id = OathKeptId,
        NameKey = "Oath Kept",
        DescriptionKey = "Turns in a row you kept its law. At 3 it leaves in peace, counted as defeated.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Its two intents: the act, and the law for your next turn (the other one's).
    private static EffectProgram<EnemyActionContext> FishActs(IEffectNode<EnemyActionContext> act, string nextLaw, string lastLaw)
    {
        var self = CombatantTargetSelectors.Source;
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            act,
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(lastLaw)),
            new ApplyStatusNode<EnemyActionContext>(self, new StatusDefinitionId(nextLaw), new ConstantExpression<EnemyActionContext>(1)),
        ]));
    }

    // ── Two-Bank Toll Ford — the two tolls ────────────────────────────────────────────────────────────────
    //
    // Since 2026-11-08 (content review E13; MTG's Tribute): every turn of yours the Ford puts its two tolls in your
    // hand, Toll in Kind (discard a card) and Toll in Blood (lose 5 HP). Pay either and the other is waved through,
    // and its next attack lands at half; pay neither and your turn ends with its law broken. The Claim-into-Block
    // bank it had before is gone.
    private static ICombatantTargetSelector Ford => Lawgiver(TollOnBothBanksId);

    public static StatusData TollOnBothBanks()
    {
        IEffectNode<TurnStartedTriggeredEffectContext> Post(string toll) =>
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new CardDefinitionId(toll), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1));
        var unpaid = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                CounterOn<TurnEndedTriggeredEffectContext>(Applicant, FordTollRoundCounter), ComparisonOperator.NotEqual,
                new RoundNumberExpression<TurnEndedTriggeredEffectContext>()),
            Breach<TurnEndedTriggeredEffectContext>(Ford));
        return Rule(TollOnBothBanksId, "Toll on Both Banks",
            $"Each turn it puts two tolls in your hand: discard a card, or lose {TollInBloodHealth} HP. Pay one and its next attack is halved; pay neither and you break its law.",
            [
                AtYourTurnsStart(Ford, new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>([Post(TollInKindId), Post(TollInBloodId)])),
                AtYourTurnsEnd(Ford, unpaid),
            ]);
    }

    public static StatusData TollPaid() => new()
    {
        Id = TollPaidId,
        NameKey = "Toll Paid",
        DescriptionKey = "You paid its toll: its next attack deals half damage.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, TollPaidDamagePercent),
        ],
        Triggers = [],
    };

    // A toll paid: the other one is waved through, the price is taken, and the Ford is satisfied for this turn.
    private static CardData Toll(string id, string name, string price, string otherId, IEffectNode<CardPlayContext> pay) =>
        Gift(id, name, $"{price}. The Ford's next attack is halved. Pay one toll each turn, or you break its law.", FordTollTag,
            Seq(
                new ForEachCardInZoneNode<CardPlayContext>(CombatantTargetSelectors.Source, CardZone.Hand,
                    new MoveCardToZoneNode<CardPlayContext>(CombatantTargetSelectors.Source, new IteratedCardExpression<CardPlayContext>(), CardZone.ExhaustPile),
                    definitionFilter: new CardDefinitionId(otherId)),
                pay,
                new ApplyStatusNode<CardPlayContext>(Ford, new StatusDefinitionId(TollPaidId), N(1)),
                new SetCombatantCounterNode<CardPlayContext>(CombatantTargetSelectors.Source, FordTollRoundCounter,
                    new RoundNumberExpression<CardPlayContext>(), relative: false)));

    public static IEnumerable<CardData> FordTollCards() =>
    [
        Toll(TollInKindId, "Toll in Kind", "Discard a card", TollInBloodId,
            new MoveCardToZoneNode<CardPlayContext>(CombatantTargetSelectors.Source,
                new ChosenCardInZoneExpression<CardPlayContext>(CardZone.Hand, "discard a card", excludeTag: new TagId(FordTollTag)),
                CardZone.DiscardPile)),
        Toll(TollInBloodId, "Toll in Blood", $"Lose {TollInBloodHealth} HP", TollInKindId, LoseHealth(TollInBloodHealth)),
    ];

    // The Ford's two blows: each spends a paid toll, so the halving lasts until the next attack and no longer.
    private static EffectProgram<EnemyActionContext> FordStrike(int damage, bool trespass) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, new ConstantExpression<EnemyActionContext>(damage)),
            .. trespass ? new[] { FileTrespass<EnemyActionContext>(CombatantTargetSelectors.Source) } : [],
            new RemoveStatusNode<EnemyActionContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(TollPaidId)),
        ]));

    // The Act-III normal bodies whose intents are their own programs (the pressure intents are in ActThree.Intent).
    private static EffectProgram<EnemyActionContext>? NormalIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "streamside_oath_fish.river_shelter" => FishActs(
                new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, new ConstantExpression<EnemyActionContext>(12)),
                nextLaw: FishLowGuardId, lastLaw: FishNoDeedsId),
            "streamside_oath_fish.current_strike" => FishActs(
                new DealDamageNode<EnemyActionContext>(Applicant, new ConstantExpression<EnemyActionContext>(12)),
                nextLaw: FishNoDeedsId, lastLaw: FishLowGuardId),
            "cairn_of_stray_paths.loose_stone" => Stonefall(),
            "two_bank_toll_ford.collect_both_banks" => FordStrike(13, trespass: true),
            "two_bank_toll_ford.flood_toll" => FordStrike(18, trespass: false),
            _ => null,
        };
}
