using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Sphinx of the Processional Measure. It does not ask for knowledge. It asks which cost
// you will accept to keep walking.
//
// Every other action it sets a riddle for your next turn, and shows you two of its three answers:
//
//   THE ANSWER OF MEASURE — 2 Weighed. Walk to the figure.
//   THE ANSWER OF BURDEN  — 2 Burdened. Carry it.
//   THE ANSWER OF BURIAL  — 1 Entombed and a form. Be packed down and filed.
//
// There is no hidden right answer and no penalty for answering: what an answer buys is a MARK, and three of
// them force the procession open — the Sphinx loses its cover and takes a fifth more from you for a turn. The
// whole body is a price list, and the player is choosing which of the act's five words to owe.
public static partial class ActFour
{
    public const string SphinxEnemyId = "sphinx_of_the_processional_measure";

    public const string ProcessionalMeasureId = "the_processional_measure";
    public const string AnswerMarkId = "answer_mark";
    public const string ProcessionOpenedId = "procession_opened";

    public const string AnswerOfMeasureCardId = "the_answer_of_measure";
    public const string AnswerOfBurdenCardId = "the_answer_of_burden";
    public const string AnswerOfBurialCardId = "the_answer_of_burial";

    public const int AnswersToOpen = 3;

    private const int OpeningBlockLost = 18;
    private const int OpenedDamagePercent = 120;
    private const int HeardEnoughBase = 25;
    private const int HeardEnoughPerType = 3;
    private const int HeardEnoughCap = 37;

    // Which pair the next riddle offers, and how many player turns the procession has stood open.
    public static CounterId RiddleStep => new("riddle_step");
    public static CounterId OpenedTurns => new("opened_turns");

    // The three answers, paired so that no pair follows itself.
    private static readonly (string First, string Second)[] Riddles =
    [
        (AnswerOfMeasureCardId, AnswerOfBurdenCardId),
        (AnswerOfBurdenCardId, AnswerOfBurialCardId),
        (AnswerOfMeasureCardId, AnswerOfBurialCardId),
    ];

    public static EffectProgram<EnemyActionContext>? SphinxIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "sphinx_of_the_processional_measure.the_procession_has_heard_enough" => HeardEnough(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> SphinxStatuses() =>
        [TheProcessionalMeasure(), AnswerMark(), ProcessionOpened()];

    // ── the riddle ────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheProcessionalMeasure() => new()
    {
        Id = ProcessionalMeasureId,
        NameKey = "The Processional Measure",
        DescriptionKey =
            "Every other turn this sphinx sets a riddle and shows you two of its three answers. There is no "
            + "right one — answering is what buys a mark, and at 3 marks the procession opens: 18 Block off "
            + "the sphinx, and a fifth more from everything you land on it for a turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(SetTheRiddle(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere)],
    };

    public static StatusData AnswerMark() => new()
    {
        Id = AnswerMarkId,
        NameKey = "Answer Mark",
        DescriptionKey = "An answer this sphinx has heard. At 3 the procession opens.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData ProcessionOpened() => new()
    {
        Id = ProcessionOpenedId,
        NameKey = "The Procession Is Open",
        DescriptionKey = "Three answers heard. Everything you land on this sphinx goes 20% further.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, OpenedDamagePercent, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    // Every other player turn: two of the three answers, in hand. And the window an open procession leaves is
    // counted here too — announced the moment the third answer is chosen, and it stands for the rest of that
    // turn and the whole of the next, which is what "for the next player turn" comes to when the announcement
    // is immediate.
    private static EffectProgram<TurnStartedTriggeredEffectContext> SetTheRiddle()
    {
        var sphinx = Bearer(ProcessionalMeasureId);

        IEffectNode<TurnStartedTriggeredEffectContext> Offer(int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(sphinx, RiddleStep),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(Riddles.Length * 2)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index * 2)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new CardDefinitionId(Riddles[index].First), CardZone.Hand,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new CardDefinitionId(Riddles[index].Second), CardZone.Hand,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                ]));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // The open procession: one whole player turn of it, then it closes.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            sphinx, new StatusDefinitionId(ProcessionOpenedId)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    sphinx, OpenedTurns),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                            [
                                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                    sphinx, new StatusDefinitionId(ProcessionOpenedId)),
                                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                    sphinx, OpenedTurns,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                                    relative: false),
                            ]),
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                sphinx, OpenedTurns,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false))),

                    // …and the riddle itself, every other turn.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new RemainderExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    sphinx, RiddleStep),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                            [.. Enumerable.Range(0, Riddles.Length).Select(Offer)])),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        sphinx, RiddleStep,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                ])));
    }

    // ── the signature ─────────────────────────────────────────────────────────────────────────────────────

    // "3 per current Act-IV negative status TYPE on the player" — the act's three afflictions, counted as
    // KINDS and not as stacks, so a player buried five deep in one thing is answered more gently than one
    // carrying a little of everything. `min(stacks, 1)` is how "is this one present" is spelled as a number.
    // The master's ceiling of 37 is kept as written; with three kinds in the act it never binds.
    private static EffectProgram<EnemyActionContext> HeardEnough()
    {
        var kinds = NegativeKinds<EnemyActionContext>();

        return new EffectProgram<EnemyActionContext>(
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MinExpression<EnemyActionContext>(
                    Const(HeardEnoughCap),
                    new AddExpression<EnemyActionContext>(
                        Const(HeardEnoughBase),
                        new MultiplyExpression<EnemyActionContext>(Const(HeardEnoughPerType), kinds)))));
    }

    // ── the answers ───────────────────────────────────────────────────────────────────────────────────────

    // Three prices, and the mark each of them buys. The third mark opens the procession in the same breath —
    // "announced immediately when the third answer is chosen" is the card doing it, not a rule waiting for a
    // turn boundary.
    public static IReadOnlyList<CardData> SphinxAnswerCards() =>
    [
        Answer(AnswerOfMeasureCardId, "The Answer of Measure",
            "Answer the sphinx by walking to a figure: 2 Weighed. One Answer Mark.",
            player =>
            [
                new ApplyStatusNode<CardPlayContext>(
                    player, new StatusDefinitionId(WeighedId), new ConstantExpression<CardPlayContext>(2)),
            ]),

        Answer(AnswerOfBurdenCardId, "The Answer of Burden",
            "Answer the sphinx by carrying it: 2 Burdened. One Answer Mark.",
            player =>
            [
                new ApplyStatusNode<CardPlayContext>(
                    player, new StatusDefinitionId(BurdenedId), new ConstantExpression<CardPlayContext>(2)),
            ]),

        Answer(AnswerOfBurialCardId, "The Answer of Burial",
            "Answer the sphinx by being packed down and filed: 1 Entombed and 1 Paperwork. One Answer Mark.",
            player =>
            [
                new ApplyStatusNode<CardPlayContext>(
                    player, new StatusDefinitionId(EntombedId), new ConstantExpression<CardPlayContext>(1)),
                new ApplyStatusNode<CardPlayContext>(
                    player, new StatusDefinitionId(Cards.Keywords.Paperwork),
                    new ConstantExpression<CardPlayContext>(1)),
            ]),
    ];

    private static CardData Answer(
        string id, string name, string text,
        Func<ICombatantTargetSelector, IReadOnlyList<IEffectNode<CardPlayContext>>> price)
    {
        var player = CombatantTargetSelectors.Source;
        var sphinx = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                new StatusDefinitionId(ProcessionalMeasureId)));

        var body = new List<IEffectNode<CardPlayContext>>();
        body.AddRange(price(player));
        body.AddRange(new IEffectNode<CardPlayContext>[]
        {
            new ApplyStatusNode<CardPlayContext>(
                sphinx, new StatusDefinitionId(AnswerMarkId), new ConstantExpression<CardPlayContext>(1)),

            // The third answer forces the procession, and says so at once.
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        sphinx, new StatusDefinitionId(AnswerMarkId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<CardPlayContext>(AnswersToOpen)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new ModifyDefensivePoolNode<CardPlayContext>(
                        sphinx, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<CardPlayContext>(-OpeningBlockLost)),
                    new ApplyStatusNode<CardPlayContext>(
                        sphinx, new StatusDefinitionId(ProcessionOpenedId),
                        new ConstantExpression<CardPlayContext>(1)),
                    new SetCombatantCounterNode<CardPlayContext>(
                        sphinx, OpenedTurns, new ConstantExpression<CardPlayContext>(0), relative: false),
                    new RemoveStatusNode<CardPlayContext>(sphinx, new StatusDefinitionId(AnswerMarkId)),
                ])),
        });

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId("answer"), new TagId(Cards.CardAuthoring.TemporaryTag)],
            // Causal and not merely sequenced: the third answer has to SEE the mark it just left, or the
            // procession never opens on the answer that opened it.
            Program = new EffectProgram<CardPlayContext>(new CausalSequenceEffectNode<CardPlayContext>(body)),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
