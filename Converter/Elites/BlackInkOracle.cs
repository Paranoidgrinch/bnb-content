using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── Black-Ink Oracle (Act II elite) ───────────────────────────────────────────────────────────────────────
//
// An oracle of lacquered plaques whose face is interrupted by strips of censoring ink. It does not ask every
// turn; on a Riddle turn it picks one of your cards, blacks out a field, and asks about it. You may answer
// and be right or wrong, buy the certainty for an Energy, or refuse — and refusing costs the same as being
// wrong. Black Ink is what it keeps from every answer it did not get, and at three the next thing it does is
// devour the answer you never stated.
//
// 9.4 is the constraint that makes the riddle buildable at all: the hidden field must be DETERMINISTIC and
// part of the card definition. The one this asks about is the printed cost, which the engine can grade
// against the card itself.
public static class BlackInkOracle
{
    public const string EnemyId = "black_ink_oracle";

    public const string TheOracleId = "the_black_ink_oracle";
    public const string OracleRulesId = "riddle_rules";
    public const string BlackInkId = "black_ink";
    public const string QueriedMark = "queried_by_the_oracle";
    public const string OracleReferenceId = "oracle_reference";
    public const string OracleReferenceMark = "referenced_by_the_oracle";

    private static CounterId RiddlePreparedCounter => new("oracle_riddle_prepared");

    private const int MaxBlackInk = 3;
    private const int WrongAnswerCost = 8;
    private const int DevourBase = 14;
    private const int DevourPerInk = 4;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Oracles =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheOracleId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheOracleId, "The Black-Ink Oracle"),
        BlackInk(),
        Rules(),
        ActTwo.Reference(OracleReferenceId, "Blackened Citation", OracleReferenceMark,
            "A redacted card the Oracle has cited. Play it, or owe it.",
            cite: CiteARedactedCard()),
    ];

    // ── 9.2 Black Ink ─────────────────────────────────────────────────────────────────────────────────────
    //
    // Unlike the Silence's Echo this is NOT a damage modifier — the signature reads it and spends it, and
    // nothing else does. It is the Oracle's memory of every question you left unanswered.
    private static StatusData BlackInk() => new()
    {
        Id = BlackInkId,
        NameKey = "Black Ink",
        DescriptionKey = "Questions the Oracle asked and did not get answered. At 3 it stops asking.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── 9.3 / 9.5 The riddle ──────────────────────────────────────────────────────────────────────────────
    //
    // On a prepared Riddle turn, after the player's normal draw, the Oracle queries the first card in hand and
    // asks about its printed cost. Five responses, which are the design's three: three ANSWER claims that the
    // engine can grade against the card, plus REVEAL and DECLINE.
    //
    // ADAPTATION: the hiding itself is presentation. The engine poses the question and grades the answer; a
    // frontend is what can actually black the field out. Nothing about the exchange depends on the field being
    // invisible — a player who has memorised the deck simply always answers correctly, which 9.8 explicitly
    // allows ("the encounter never requires memorizing the deck to remain playable").
    private static StatusData Rules()
    {
        var riddle = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, RiddlePreparedCounter),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Self, RiddlePreparedCounter,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                    // The Oracle picks the card; the ink goes on it, and the question is about it.
                    ClearMark(QueriedMark),
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self, new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand, 0),
                        new TagId(QueriedMark)),
                    new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Answer(0), Answer(1), AnswerTwoOrMore(), Reveal(), Decline(),
                    ],
                    [
                        "answer: the blacked-out card costs 0",
                        "answer: the blacked-out card costs 1",
                        "answer: the blacked-out card costs 2 or more",
                        "reveal it (1 Energy, or 1 Overdue)",
                        "decline to answer",
                    ],
                    count: 1, purpose: "the Oracle asks about the first card in your hand"),
                ])));

        return Rule(OracleRulesId, "The Oracle's Riddle",
            "On a Riddle turn the Oracle blacks out a field of one of your cards and asks about it. Answer "
            + "and be right, buy the certainty, or refuse — refusing costs what being wrong costs.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    riddle, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
            ]);
    }

    private static ICardInstanceExpression<CardsDrawnTriggeredEffectContext> Queried() =>
        new FirstMarkedCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand, new TagId(QueriedMark));

    // "If correct: Oracle loses 8 HP, Black Ink −1, the field is revealed. If incorrect: Black Ink +1, and the
    // card becomes normally Redacted for its next play." The HP loss is direct, so a health SET.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Answer(int claimedCost) =>
        Graded(new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
            new CardInstanceBaseCostExpression<CardsDrawnTriggeredEffectContext>(
                Queried(), StandardCombatIds.EnergyResource),
            ComparisonOperator.Equal,
            new ConstantExpression<CardsDrawnTriggeredEffectContext>(claimedCost)));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> AnswerTwoOrMore() =>
        Graded(new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
            new CardInstanceBaseCostExpression<CardsDrawnTriggeredEffectContext>(
                Queried(), StandardCombatIds.EnergyResource),
            ComparisonOperator.GreaterOrEqual,
            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Graded(
        ICombatExpression<CardsDrawnTriggeredEffectContext, bool> correct) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            correct,
            new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Oracles,
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new SetHealthNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new SubtractExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(WrongAnswerCost))),
                    new ModifyStatusStacksNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BlackInkId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1)),
                ])),
            @else: WrongOrRefused());

    // Being wrong and refusing cost exactly the same, which is what makes DECLINE a real option rather than a
    // free out: an ink, and the card blacked out for its next play.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> WrongOrRefused() =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            GainInk(),
            ActTwo.Redact<CardsDrawnTriggeredEffectContext>(Self, Queried()),
        ]);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Decline() => WrongOrRefused();

    private static IEffectNode<CardsDrawnTriggeredEffectContext> GainInk() =>
        new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Oracles,
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BlackInkId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(MaxBlackInk)),
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BlackInkId),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))));

    // "REVEAL must never be presented as a supposedly safe option that is impossible to select." With an
    // Energy in hand it costs the Energy; without one it costs an Overdue owed to the Oracle. Either way it
    // buys certainty and gains the Oracle nothing — no ink, no redaction.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Reveal() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new CombatantCurrentResourceExpression<CardsDrawnTriggeredEffectContext>(
                    Self, StandardCombatIds.EnergyResource),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
            new ModifyResourceNode<CardsDrawnTriggeredEffectContext>(
                Self, StandardCombatIds.EnergyResource,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1)),
            @else: new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Oracles,
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    Self, new StatusDefinitionId(ActTwo.OverdueId),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))));

    // Blacken the Convenient Answer cites a card the Oracle has already redacted, not any card.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteARedactedCard() =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                new TagId(OracleReferenceMark)),
            markFilter: new TagId(ActTwo.RedactedMark), takeFirst: 1);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> ClearMark(string mark) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                new TagId(mark), remove: true),
            markFilter: new TagId(mark));

    // ── 9.6 Intents ───────────────────────────────────────────────────────────────────────────────────────
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "pose_the_missing_question" => new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, RiddlePreparedCounter,
                    new ConstantExpression<EnemyActionContext>(1), relative: false),
                new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(12)),
            ])),
        "seal_the_riddle" => Offensive(new CausalSequenceEffectNode<EnemyActionContext>(
            [Damage(16), ActTwo.RedactOne()])),
        "blacken_the_convenient_answer" => Offensive(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(11),
            // "If a Redacted card exists in hand, Reference 1 Redacted card" — and nothing if none does.
            new ForEachCardInZoneNode<EnemyActionContext>(
                Opponent, CardZone.Hand,
                new MarkCardInstanceNode<EnemyActionContext>(
                    Opponent, new IteratedCardExpression<EnemyActionContext>(),
                    new TagId(OracleReferenceMark)),
                markFilter: new TagId(ActTwo.RedactedMark), takeFirst: 1),
        ])),
        "stone_paw_of_omission" => Offensive(Damage(18)),
        "ink_wing_guard" => new EffectProgram<EnemyActionContext>(
            new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(22))),
        _ => null,
    };

    // Signature — Devour the Unstated Answer: 14 + 4 per Black Ink, then the ink is spent. Black Ink is not a
    // damage modifier, so the number is computed here rather than added by a passive.
    private static EffectProgram<EnemyActionContext> Offensive(IEffectNode<EnemyActionContext> body) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(BlackInkId)),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(MaxBlackInk)),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(Opponent,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(DevourBase),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(DevourPerInk),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                Self, new StatusDefinitionId(BlackInkId))))),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(BlackInkId)),
            ]),
            @else: body));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, new ConstantExpression<EnemyActionContext>(amount));

    private static StatusData Marker(string id, string name) => Rule(id, name, name, []);

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
