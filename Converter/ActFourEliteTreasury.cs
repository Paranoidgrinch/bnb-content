using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Treasury of the Two Pans. A treasury chamber gone animate around an enormous double-pan
// balance, and the only body in the act that weighs your turn against ITSELF.
//
// Every other measure in this act asks for a figure. The Two Pans asks whether what you PAID matches what you
// DID: one pan holds the cards you played (junk excepted — the treasury does not dignify rubbish with a
// price), the other holds the Energy you actually spent on them.
//
//   BALANCED    — the books agree. The treasury bleeds 10, loses its cover, and writes you a Credit.
//   OVERVALUED  — you paid more than you did. That is waste, and waste is a burden.
//   UNDERPAID   — you did more than you paid for. That is a discrepancy, and a discrepancy is paperwork.
//
// A Credit is a line of credit against the treasury itself: once a turn it buys away its cover or a burden of
// yours. Keep them and Close the Accounts hits four harder for each unspent one, which is the second half of
// the same decision — the balance never stops weighing, even at the end.
public static partial class ActFour
{
    public const string TreasuryEnemyId = "treasury_of_the_two_pans";

    public const string TwoPansId = "the_two_pans";
    public const string TreasuryCreditId = "treasury_credit";
    public const string CreditDrawnThisTurnId = "credit_drawn_this_turn";

    public const string DrawAgainstTheTreasuryCardId = "draw_against_the_treasury";
    public const string SettleTheBurdenCardId = "settle_the_burden";

    public const int CreditCap = 2;
    private const int BalancedHealthLoss = 10;
    private const int BalancedBlockLost = 12;
    private const int OvervaluedBlock = 10;
    private const int CreditBlockDrawn = 12;
    private const int ClosingBase = 24;
    private const int ClosingPerCredit = 4;
    private const int ClosingCap = 32;

    public static CounterId CreditOfferedThisTurn => new("credit_offered_this_turn");

    public static EffectProgram<EnemyActionContext>? TreasuryIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "treasury_of_the_two_pans.inventory_of_copper" => InventoryOfCopper(13),
            "treasury_of_the_two_pans.close_the_accounts" => CloseTheAccounts(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> TreasuryStatuses() =>
        [TheTwoPans(), TreasuryCredit(), CreditDrawnThisTurn()];

    public static IReadOnlyList<CardData> TreasuryCreditCards() =>
    [
        CreditCard(DrawAgainstTheTreasuryCardId, "Draw Against the Treasury",
            "Spend a Treasury Credit: take up to 12 Block off the treasury. Once a turn.",
            treasury =>
            [
                new ModifyDefensivePoolNode<CardPlayContext>(
                    treasury, StandardCombatIds.BlockDefensivePool,
                    new ConstantExpression<CardPlayContext>(-CreditBlockDrawn)),
            ]),

        CreditCard(SettleTheBurdenCardId, "Settle the Burden",
            "Spend a Treasury Credit: 1 Burdened is settled and comes off you. Once a turn.",
            _ =>
            [
                new ModifyStatusStacksNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(BurdenedId),
                    new ConstantExpression<CardPlayContext>(-1)),
            ]),
    ];

    // ── the balance ───────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheTwoPans() => new()
    {
        Id = TwoPansId,
        NameKey = "The Two Pans",
        DescriptionKey =
            "One pan holds the cards you played, the other the Energy you actually spent on them. Balanced: "
            + "the treasury bleeds and writes you a Credit. Overpaid: a burden. Underpaid: paperwork.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(CopperReckoning(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(OfferTheCredit(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
        ],
    };

    public static StatusData TreasuryCredit() => new()
    {
        Id = TreasuryCreditId,
        NameKey = "Treasury Credit",
        DescriptionKey =
            "A line of credit against the treasury, at most 2. Once a turn one buys away its cover or a "
            + "burden of yours — and every one you do not spend makes the closing balance 4 harder.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "Once per player turn spend one Credit." Two offers stand in hand, and the first one drawn against
    // closes the window on the other — the treasury does not open its books twice in a day.
    public static StatusData CreditDrawnThisTurn() => new()
    {
        Id = CreditDrawnThisTurnId,
        NameKey = "Drawn Against Today",
        DescriptionKey = "You have already drawn against the treasury this turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The reckoning, at the player's turn end — which is the one moment both pans have a number in them, and
    // also the one moment the treasury's own cover is still standing to be taken off it.
    private static EffectProgram<TurnEndedTriggeredEffectContext> CopperReckoning()
    {
        var treasury = Bearer(TwoPansId);

        // Quantity: what you did, rubbish not counted. Value: what you actually paid for it — the same figure
        // the act's measure reads, so a turn is weighed by one number throughout the labyrinth.
        var quantity = new SubtractExpression<TurnEndedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant),
            new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                Applicant, new TagId(Cards.CardAuthoring.JunkTag)));
        var value = new ResourceSpentThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant);

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> Gap(
            ICombatExpression<TurnEndedTriggeredEffectContext, int> more,
            ICombatExpression<TurnEndedTriggeredEffectContext, int> less,
            int at) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new SubtractExpression<TurnEndedTriggeredEffectContext>(more, less),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(at));

        var balanced = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new DealDamageNode<TurnEndedTriggeredEffectContext>(
                treasury, new ConstantExpression<TurnEndedTriggeredEffectContext>(BalancedHealthLoss),
                ignoresBlock: true, kind: DamageKind.DamageOverTime),

            new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                treasury, StandardCombatIds.BlockDefensivePool,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(-BalancedBlockLost)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(TreasuryCreditId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(CreditCap)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(TreasuryCreditId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: treasury)),
        ]);

        var overvalued = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Gap(value, quantity, 2),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(BurdenedId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: treasury),
                new GainBlockNode<TurnEndedTriggeredEffectContext>(
                    treasury, new ConstantExpression<TurnEndedTriggeredEffectContext>(OvervaluedBlock)),
            ]),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(BurdenedId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: treasury));

        var underpaid = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Gap(quantity, value, 2),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(2), sourceSelector: treasury),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(InscribedId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: treasury),
            ]),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: treasury));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        value, ComparisonOperator.Equal, quantity),
                    balanced,
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            value, ComparisonOperator.Greater, quantity),
                        overvalued,
                        underpaid))));
    }

    // The offers, while a Credit stands. Both are put up each turn and the first one drawn against closes the
    // window on the other, which is the master's "once per player turn" spelled where the player can read it.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OfferTheCredit() =>
        new(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(TreasuryCreditId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(CreditDrawnThisTurnId)),

                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(DrawAgainstTheTreasuryCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(SettleTheBurdenCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            ])));

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> InventoryOfCopper(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
        ]));

    // The closing balance weighs what you did NOT spend: four harder for every credit still on the books,
    // and then the books are closed. Its cooldown is its place in the cycle — six intents round.
    private static EffectProgram<EnemyActionContext> CloseTheAccounts() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MinExpression<EnemyActionContext>(
                    Const(ClosingCap),
                    new AddExpression<EnemyActionContext>(
                        Const(ClosingBase),
                        new MultiplyExpression<EnemyActionContext>(
                            Const(ClosingPerCredit),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                Applicant, new StatusDefinitionId(TreasuryCreditId)))))),

            new RemoveStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(TreasuryCreditId)),
        ]));

    // ── drawing against the treasury ──────────────────────────────────────────────────────────────────────

    private static CardData CreditCard(
        string id, string name, string text,
        Func<ICombatantTargetSelector, IReadOnlyList<IEffectNode<CardPlayContext>>> spend)
    {
        var treasury = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TwoPansId)));

        var body = new List<IEffectNode<CardPlayContext>>
        {
            new ModifyStatusStacksNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(TreasuryCreditId),
                new ConstantExpression<CardPlayContext>(-1)),
            new ApplyStatusNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(CreditDrawnThisTurnId),
                new ConstantExpression<CardPlayContext>(1)),
        };
        body.AddRange(spend(treasury));

        // A credit and an unopened window, or the card does nothing at all: the treasury's counter is shut
        // for the day the moment one of these is used.
        var program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new AndExpression<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusStacksExpression<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(TreasuryCreditId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayContext>(0)),
                    new NotExpression<CardPlayContext>(
                        new TargetHasStatusExpression<CardPlayContext>(
                            CombatantTargetSelectors.Source,
                            new StatusDefinitionId(CreditDrawnThisTurnId)))),
                new SequenceEffectNode<CardPlayContext>(body)));

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId("credit"), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = program,
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
