using RogueDeck.Scenario.Authoring;
using RogueDeck.Core.Combat;

namespace BnbContent.Converter.Elites;

// ── After-Hours Return Bell (Act II elite) ────────────────────────────────────────────────────────────────
//
// The Bell is a debt engine with a receipt printer attached. Every Overdue it files also hands the player a
// Return Receipt; two of its Overdue collecting turns the debt into a Late Fee it keeps; and its signature
// cashes every Late Fee at once. The counterplay is the Receipt, which can pay down EITHER side — the debt
// you owe now, or the fee that will be charged later — but never both with one card.
//
// Everything here is one system, so it lives in one file: the fee, the collection, the receipt, and the toll.
public static class ReturnBell
{
    public const string EnemyId = "after_hours_return_bell";
    public const string LateFeeId = "late_fee";
    public const string DelinquencyId = "bell_delinquency";
    public const string ReceiptCardId = "return_receipt";

    private const int MaxLateFees = 3;
    private const int MaxReceipts = 3;
    private const int BellHpLoss = 5;
    private const int TollBase = 14;
    private const int TollPerFee = 4;

    private static readonly TagId ReceiptTag = new("return_receipt");

    // Inside the Bell's own programs the player is the one opponent; inside the player's card the Bell is.
    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    public static IEnumerable<StatusData> Statuses() => [LateFee(), BellDelinquency()];

    public static IEnumerable<CardData> Cards() => [ReturnReceipt()];

    // ── Late Fee ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // Stored enforcement. It does nothing on its own — it is read by the signature and spent by the Receipt.
    private static StatusData LateFee() => new()
    {
        Id = LateFeeId,
        NameKey = "Late Fee",
        DescriptionKey = "A charge the Bell has recorded and not yet collected.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── 5.3 Late Fee ──────────────────────────────────────────────────────────────────────────────────────
    //
    // The Bell's Delinquency is the standard Act II collection with one addition: collecting also books a fee,
    // up to three. The cap is a guard rather than a status maximum because a status that refused its fourth
    // stack would refuse it silently.
    private static StatusData BellDelinquency() =>
        ActTwo.Delinquency(DelinquencyId, "After-Hours Collection",
            "When two of the Bell's Overdue collect, the Bell also records a Late Fee (max 3).",
            lateConsequence: new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(LateFeeId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(MaxLateFees)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(LateFeeId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))));

    // ── 5.2 Proof of Return ───────────────────────────────────────────────────────────────────────────────
    //
    // "Whenever the Bell ITSELF creates 1 Overdue, it also creates 1 Return Receipt." The Bell creates Overdue
    // in exactly two intents, so the receipt is issued there rather than from a trigger watching status
    // applications: the two places are the whole rule, and writing it where the Overdue is written keeps the
    // pairing provable.
    //
    // "Maximum simultaneous Bell-generated Receipts: 3" is read off the cards themselves — hand, draw pile and
    // discard, the three zones a live Receipt can be in. A Receipt exhausts when played, so leaving combat
    // frees its slot without anyone counting.
    private static IEffectNode<EnemyActionContext> IssueReceipt() =>
        new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                LiveReceipts(), ComparisonOperator.Less,
                new ConstantExpression<EnemyActionContext>(MaxReceipts)),
            new CreateCardInstanceNode<EnemyActionContext>(
                Opponent, new CardDefinitionId(ReceiptCardId), CardZone.DiscardPile,
                new ConstantExpression<EnemyActionContext>(1)));

    private static ICombatExpression<EnemyActionContext, int> LiveReceipts() =>
        new AddExpression<EnemyActionContext>(
            new CombatantZoneCardCountExpression<EnemyActionContext>(Opponent, CardZone.Hand, ReceiptTag),
            new AddExpression<EnemyActionContext>(
                new CombatantZoneCardCountExpression<EnemyActionContext>(Opponent, CardZone.DrawPile, ReceiptTag),
                new CombatantZoneCardCountExpression<EnemyActionContext>(Opponent, CardZone.DiscardPile, ReceiptTag)));

    // ── Return Receipt ────────────────────────────────────────────────────────────────────────────────────
    //
    // 1 Energy · Retain · Exhaust. Retain is TurnEndHandDestinationZone = Hand (the card is never discarded at
    // the turn's end); Exhaust is PlayedCardDestinationZone = ExhaustPile.
    //
    // ADAPTATION: the design makes CONTEST THE FEE "unavailable" with no Late Fee on the field. An option list
    // has no per-option availability, so the option is always offered and does nothing when there is no fee to
    // contest — the same shape as a card played into an empty board.
    private static CardData ReturnReceipt() => new()
    {
        Id = ReceiptCardId,
        NameKey = "Return Receipt",
        DescriptionKey =
            "Choose one: FILE THE RECEIPT — remove 1 Overdue; the Bell loses 5 HP. "
            + "CONTEST THE FEE — remove 1 Late Fee; mark 1 draw-pile card Misfiled.",
        Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
        Tags = [ReceiptTag, new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new ChooseOptionsNode<CardPlayContext>(
                [FileTheReceipt(), ContestTheFee()],
                ["file the receipt", "contest the fee"],
                count: 1,
                purpose: "choose one")),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    // FILE THE RECEIPT: one Overdue goes away and the Bell pays for having filed it. The Bell fights alone, so
    // every Overdue on the player is Bell-issued and no source filter is needed to say so.
    //
    // "This is direct HP Loss, not a Damage event" — so it is written as a health SET, current minus five,
    // which no Block, no damage modifier and no damage-taken reaction can see.
    private static IEffectNode<CardPlayContext> FileTheReceipt() =>
        new CausalSequenceEffectNode<CardPlayContext>(
        [
            new ModifyStatusStacksNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ActTwo.OverdueId),
                new ConstantExpression<CardPlayContext>(-1)),
            new SetHealthNode<CardPlayContext>(
                Opponent,
                new SubtractExpression<CardPlayContext>(
                    new CombatantCurrentHealthExpression<CardPlayContext>(Opponent),
                    new ConstantExpression<CardPlayContext>(BellHpLoss))),
        ]);

    // CONTEST THE FEE: the fee is struck from the record, and the record is worse for it — one draw-pile card
    // comes back Misfiled. Without a fee to contest, nothing happens at all.
    private static IEffectNode<CardPlayContext> ContestTheFee() =>
        new ConditionalEffectNode<CardPlayContext>(
            new ComparisonExpression<CardPlayContext>(
                new CombatantStatusStacksExpression<CardPlayContext>(Opponent, new StatusDefinitionId(LateFeeId)),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayContext>(1)),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ModifyStatusStacksNode<CardPlayContext>(
                    Opponent, new StatusDefinitionId(LateFeeId),
                    new ConstantExpression<CardPlayContext>(-1)),
                new MarkCardInstanceNode<CardPlayContext>(
                    CombatantTargetSelectors.Source,
                    new CardInZoneExpression<CardPlayContext>(CardZone.DrawPile, 0),
                    new TagId(ActTwo.MisfiledMark)),
            ]));

    // ── 5.4 Signature — Toll for Every Unreturned Thing ───────────────────────────────────────────────────
    //
    // 14 + 4 per Late Fee, then the fees are cleared. The clearing is what makes the intent a decision rather
    // than a countdown: the player chooses which fees ever reach it.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "issue_the_closing_notice" => FileOverdue(damage: 0),
        "the_desk_is_now_closed" => FileOverdue(damage: 12),
        "toll_for_every_unreturned_thing" or "toll_for_every_unreturned_thing_again" => Toll(),
        _ => null,
    };

    private static EffectProgram<EnemyActionContext> FileOverdue(int damage)
    {
        var steps = new List<IEffectNode<EnemyActionContext>>();
        if (damage > 0)
            steps.Add(new DealDamageNode<EnemyActionContext>(
                Opponent, new ConstantExpression<EnemyActionContext>(damage)));
        steps.Add(new ApplyStatusNode<EnemyActionContext>(
            Opponent, new StatusDefinitionId(ActTwo.OverdueId),
            new ConstantExpression<EnemyActionContext>(1)));
        steps.Add(IssueReceipt());
        return new EffectProgram<EnemyActionContext>(new CausalSequenceEffectNode<EnemyActionContext>(steps));
    }

    private static EffectProgram<EnemyActionContext> Toll() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Opponent,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(TollBase),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(TollPerFee),
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            Self, new StatusDefinitionId(LateFeeId))))),
            new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(LateFeeId)),
        ]));
}
