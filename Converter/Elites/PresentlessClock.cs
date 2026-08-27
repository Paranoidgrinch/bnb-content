using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Presentless Clock (Act II elite) ──────────────────────────────────────────────────────────────────
//
// A clock with hands for the Past and the Future and nothing at all for the Present. It does not schedule its
// own attacks — it changes WHEN your actions are considered to have happened. File an effect to the Past and
// it happens now and echoes at half strength next turn; file it to the Future and half of it happens now and
// the rest arrives late. Either way the Clock is holding a record of yours, and it reacts to that.
//
// The two hands pull in opposite directions: an unresolved Past record guards it, an unresolved Future record
// costs it. Which is why filing everything to one hand is never right.
public static class PresentlessClock
{
    public const string EnemyId = "presentless_clock";

    public const string TheClockId = "the_presentless_clock";
    public const string ClockRulesId = "temporal_attribution";
    public const string PastArmedId = "filed_to_the_past";
    public const string FutureArmedId = "filed_to_the_future";
    public const string ClockReferenceId = "clock_reference";
    public const string ClockReferenceMark = "referenced_by_the_clock";
    public const string ClockDelinquencyId = "clock_delinquency";

    // A record is a kind and an amount: 1 = direct card damage, 2 = card-generated Block.
    private const int KindDamage = 1;
    private const int KindBlock = 2;

    private static CounterId PastKind => new("clock_past_kind");
    private static CounterId PastAmount => new("clock_past_amount");
    private static CounterId FutureKind => new("clock_future_kind");
    private static CounterId FutureAmount => new("clock_future_amount");
    // Signature state: the Past echo can be made to repeat at 75 %, and a Future record can be held back one
    // extra turn — once.
    private static CounterId PastScale => new("clock_past_scale");
    private static CounterId FutureDelay => new("clock_future_delay");
    // "Maximum once per enemy turn" for each of the Clock's two reactions.
    private static CounterId ArchivedCounter => new("clock_archived");

    private const int DefaultEchoPercent = 50;
    private const int RepeatedEchoPercent = 75;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Clocks =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheClockId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheClock(),
        Rules(),
        PastArmed(),
        FutureArmed(),
        ActTwo.Delinquency(ClockDelinquencyId, "Overdue by an Hour", "The Clock collects what it is owed."),
        ActTwo.Reference(ClockReferenceId, "No Present Tense", ClockReferenceMark,
            "A card the Clock has cited. Play it, or owe it.",
            cite: new NoOpEffectNode<CardsDrawnTriggeredEffectContext>()),
    ];

    // ── 12.6 Clock reaction ───────────────────────────────────────────────────────────────────────────────
    //
    // At its own turn start the Clock reads what it is holding: an unresolved Past record guards it by 10, an
    // unresolved Future record costs it 6 Block — or 6 HP if it has no Block to give, which is direct loss and
    // not a Damage event. Both at most once per enemy turn, which is what one turn-start pass is.
    private static StatusData TheClock()
    {
        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    // The records live on the PLAYER — they are the player's effects — so the Clock reads
                    // across at its own turn start rather than looking at itself.
                    HeldByOpponent<TurnStartedTriggeredEffectContext>(PastKind),
                    new GainBlockNode<TurnStartedTriggeredEffectContext>(
                        Self, new ConstantExpression<TurnStartedTriggeredEffectContext>(10))),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    HeldByOpponent<TurnStartedTriggeredEffectContext>(FutureKind),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                                Self, StandardCombatIds.BlockDefensivePool),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new ModifyDefensivePoolNode<TurnStartedTriggeredEffectContext>(
                            Self, StandardCombatIds.BlockDefensivePool,
                            new NegateExpression<TurnStartedTriggeredEffectContext>(
                                new MinExpression<TurnStartedTriggeredEffectContext>(
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(6),
                                    new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                                        Self, StandardCombatIds.BlockDefensivePool)))),
                        @else: new SetHealthNode<TurnStartedTriggeredEffectContext>(Self,
                            new SubtractExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(Self),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(6))))),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Opponent, ArchivedCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
            ]));

        return Rule(TheClockId, "The Presentless Clock",
            "It holds your past and your future. The past it archives; the future it forecloses.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ]);
    }

    // ── 12.2 Temporal Attribution ─────────────────────────────────────────────────────────────────────────
    //
    // After the normal draw the player files the turn to one hand or the other, and the FIRST eligible primary
    // effect that turn is what gets recorded. Two moments carry it: this offer, and the turn start where the
    // records come due.
    //
    // ADAPTATION: 12.5 says an occupied slot makes that mode "unavailable". An option list cannot hide an
    // option, so the mode is offered and does nothing when its slot is full — the record is never overwritten,
    // which is the rule that actually matters.
    private static StatusData Rules()
    {
        var offer = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
            [
                Arm(PastArmedId, PastKind), Arm(FutureArmedId, FutureKind),
                new NoOpEffectNode<CardsDrawnTriggeredEffectContext>(),
            ],
            ["file this turn to the Past", "file this turn to the Future", "let it happen now"],
            count: 1, purpose: "the Clock has no hand for the present"));

        var due = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // 12.3: the Past echoes at half — or at three quarters, if the Clock made history repeat.
                Resolve(PastKind, PastAmount),
                // 12.4: the Future's remainder arrives — unless the Clock borrowed it another turn.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        Count<TurnStartedTriggeredEffectContext>(FutureDelay),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Self, FutureDelay,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    @else: Resolve(FutureKind, FutureAmount)),
                // A fresh turn: the echo is back to half unless the Clock says otherwise again…
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Self, PastScale,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(DefaultEchoPercent),
                    relative: false),

            ]));

        // An arming that caught nothing expires with the turn it belonged to. It has to be the turn's END:
        // a turn-start program is still draining when the draw happens, so a clearing step written there
        // would arrive AFTER the new arming and take it straight back off.
        var expire = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(PastArmedId)),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(FutureArmedId)),
            ]));

        return Rule(ClockRulesId, "Temporal Attribution",
            "Each turn you may file your first effect to the Past — it happens now and echoes at half next "
            + "turn — or to the Future, where half of it happens now and the rest arrives late.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    offer, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    due, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    expire, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ]);
    }

    // Arming a hand is only possible while its slot is empty — a record is never overwritten.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Arm(string status, CounterId slot) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new NotExpression<CardsDrawnTriggeredEffectContext>(Holding<CardsDrawnTriggeredEffectContext>(slot)),
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                Self, new StatusDefinitionId(status),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)));

    // A record comes due: damage lands on the Clock, Block lands on the player. Then the slot is free again.
    private static IEffectNode<TurnStartedTriggeredEffectContext> Resolve(CounterId kind, CounterId amount) =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Holding<TurnStartedTriggeredEffectContext>(kind),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        Count<TurnStartedTriggeredEffectContext>(kind),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(KindDamage)),
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Clocks,
                        new DealDamageNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget,
                            Count<TurnStartedTriggeredEffectContext>(amount))),
                    @else: new GainBlockNode<TurnStartedTriggeredEffectContext>(
                        Self, Count<TurnStartedTriggeredEffectContext>(amount))),
                Set<TurnStartedTriggeredEffectContext>(kind, 0),
                Set<TurnStartedTriggeredEffectContext>(amount, 0),
            ]));

    // ── 12.3 Past ─────────────────────────────────────────────────────────────────────────────────────────
    //
    // The effect resolves in full now; what is stored is a fraction of it. The status is the arming, and it
    // takes itself off the moment it has caught something — that is what "the FIRST eligible primary effect"
    // means.
    private static StatusData PastArmed() =>
        Rule(PastArmedId, "Filed to the Past",
            "The first effect you make this turn will echo at half strength next turn.",
            [
                Catch<DamageDealtTriggeredEffectContext>(
                    "DamageDealt", PastArmedId, PastKind, PastAmount, KindDamage, scaled: true),
                Catch<BlockGainedTriggeredEffectContext>(
                    "BlockGained", PastArmedId, PastKind, PastAmount, KindBlock, scaled: true),
            ]);

    // ── 12.4 Future ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Here the status does two jobs: it HALVES the effect as it happens (a passive modifier on the two
    // pipelines the design calls eligible), and then records what was dealt as the remainder still owed.
    // Recording the dealt amount — rather than the original minus the dealt — is the design's "approximately
    // 50 %", and it keeps the two halves equal without the original ever being knowable after the fact.
    private static StatusData FutureArmed()
    {
        var status = Rule(FutureArmedId, "Filed to the Future",
            "Half of the first effect you make this turn happens now; the rest arrives next turn.",
            [
                Catch<DamageDealtTriggeredEffectContext>(
                    "DamageDealt", FutureArmedId, FutureKind, FutureAmount, KindDamage, scaled: false),
                Catch<BlockGainedTriggeredEffectContext>(
                    "BlockGained", FutureArmedId, FutureKind, FutureAmount, KindBlock, scaled: false),
            ]);

        return status with
        {
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.ScalePercent, 50, RestrictDamageKind: DamageKind.Direct),
                new PassiveModifierData(PassiveModifierPipeline.BlockGain,
                    PassiveModifierOperation.ScalePercent, 50),
            ],
        };
    }

    // One catch: record the kind and the amount, then disarm. `scaled` is the Past's echo fraction — the
    // Future's remainder is the amount itself, because the modifier already took the other half.
    private static StatusTriggerData Catch<TContext>(
        string eventName, string armed, CounterId kind, CounterId amount, int kindValue, bool scaled)
        where TContext : class
    {
        ICombatExpression<TContext, int> recorded = scaled
            ? new DivideExpression<TContext>(
                new MultiplyExpression<TContext>(
                    new EventAmountExpression<TContext>(), Count<TContext>(PastScale)),
                new ConstantExpression<TContext>(100))
            : new EventAmountExpression<TContext>();

        var program = new EffectProgram<TContext>(
            new CausalSequenceEffectNode<TContext>(
            [
                Set<TContext>(kind, kindValue),
                new SetCombatantCounterNode<TContext>(
                    CombatantTargetSelectors.Source, amount, recorded, relative: false),
                new RemoveStatusNode<TContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(armed)),
            ]));

        return new StatusTriggerData(eventName,
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
    }

    // ── 12.7 Intents ──────────────────────────────────────────────────────────────────────────────────────
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "history_refuses_revision" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(15),
            new ConditionalEffectNode<EnemyActionContext>(
                HoldingOnOpponent(PastKind),
                new GainBlockNode<EnemyActionContext>(Self, Const(10))),
        ])),
        "tomorrow_has_already_been_filed" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(13),
            new ConditionalEffectNode<EnemyActionContext>(HoldingOnOpponent(FutureKind), MisfileOne()),
        ])),
        "no_present_tense" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(10),
            new ForEachCardInZoneNode<EnemyActionContext>(
                Opponent, CardZone.Hand,
                new MarkCardInstanceNode<EnemyActionContext>(
                    Opponent, new IteratedCardExpression<EnemyActionContext>(),
                    new TagId(ClockReferenceMark)),
                takeFirst: 1),
        ])),
        "second_hand_no_first" or "second_hand_no_first_again" => Program(Damage(18)),
        "chronology_closed" => Program(new GainBlockNode<EnemyActionContext>(Self, Const(24))),

        // Signature — Borrowed Tomorrow: it holds your future back one more turn. Only ever once, because the
        // delay counter is cleared by the turn that honours it.
        "borrowed_tomorrow" => Program(new ConditionalEffectNode<EnemyActionContext>(
            HoldingOnOpponent(FutureKind),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(11),
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, FutureDelay, Const(1), relative: false),
            ]),
            @else: Damage(11))),

        // Signature — History Repeats Incorrectly: the echo comes back louder than it left.
        "history_repeats_incorrectly" => Program(new ConditionalEffectNode<EnemyActionContext>(
            HoldingOnOpponent(PastKind),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(19),
                // The record was stored at 50 %; three quarters of the original is half as much again.
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, PastAmount,
                    new DivideExpression<EnemyActionContext>(
                        new MultiplyExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(Opponent, PastAmount),
                            Const(RepeatedEchoPercent)),
                        Const(DefaultEchoPercent)),
                    relative: false),
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, PastScale, Const(RepeatedEchoPercent), relative: false),
            ]),
            @else: Damage(19))),
        _ => null,
    };

    private static IEffectNode<EnemyActionContext> MisfileOne() =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(),
                new TagId(ActTwo.MisfiledMark)),
            takeFirst: 1);

    private static EffectProgram<EnemyActionContext> Program(IEffectNode<EnemyActionContext> body) => new(body);

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // ── shared shapes ─────────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, int> Count<TContext>(CounterId counter) where TContext : class =>
        new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter);

    private static ICombatExpression<TContext, bool> Holding<TContext>(CounterId kind) where TContext : class =>
        new ComparisonExpression<TContext>(
            Count<TContext>(kind), ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> HeldByOpponent<TContext>(CounterId kind)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(Opponent, kind),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<EnemyActionContext, bool> HoldingOnOpponent(CounterId kind) =>
        HeldByOpponent<EnemyActionContext>(kind);

    private static IEffectNode<TContext> Set<TContext>(CounterId counter, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, counter, new ConstantExpression<TContext>(value), relative: false);

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
