using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 9 — **Magistrate of Thorns** (220 HP). The hardest conventional fight in the act.
//
// > The player chooses the least harmful rule to be judged under; Claims temporarily make that ruling
// > binding.
//
// A thorn-crowned magistrate asks the traveller under which known custom they would prefer to be judged —
// and then makes the answer binding. Three Judgments, one chosen each turn:
//
//   Conduct  — the fourth real card of a turn.
//   Measure  — two cards in a row of the same Base Cost.
//   Standing — ending a turn with no Safe-Conduct.
//
// Only the chosen one is ordinary law. But a Judgment that MATURES into standing becomes **Binding** for
// two of your turns — in addition to whatever you choose next — so the law you accepted as the least bad
// is the one that follows you. Two may bind at once, and at that point every turn is judged three ways.
//
// The way back out is restitution: settling in full strikes a Claim, takes a turn off the oldest Binding
// Judgment, and costs the Magistrate 7 HP.
public static partial class ActThree
{
    public const string MagistrateEnemyId = "magistrate_of_thorns";
    public const string MagistrateId = "the_magistrate_of_thorns";
    public const string JudgmentOfferId = "three_judgments";
    public const string MagistrateNotedId = "judgment_noted";
    public const string EstablishPendingId = "judgment_to_be_established";
    public const string HearingOpenedId = "hearing_opened";
    public const string BellRungId = "thorn_bell_rung";

    public const int JudgmentOfConductLaw = 24;
    public const int JudgmentOfMeasureLaw = 25;
    public const int JudgmentOfStandingLaw = 26;

    private const int BindingTurns = 2;
    private const int MaxBindingJudgments = 2;
    private const int MagistrateRedressHealth = 7;

    // Which Judgment the traveller accepted this turn: 1 Conduct, 2 Measure, 3 Standing.
    public static CounterId ChosenJudgmentCounter => new("chosen_judgment");

    private static readonly (string Key, string Name, int Law)[] Judgments =
    [
        ("conduct", "Conduct", JudgmentOfConductLaw),
        ("measure", "Measure", JudgmentOfMeasureLaw),
        ("standing", "Standing", JudgmentOfStandingLaw),
    ];

    private static string BindingId(string key) => $"binding_judgment_{key}";

    private static ICombatantTargetSelector Magistrate { get; } = Elite(MagistrateId);

    private static IEnumerable<StatusData> MagistrateStatuses() =>
    [
        TheMagistrateOfThorns(),
        ThreeJudgments(),
        Marker(MagistrateNotedId, "Judgment Noted",
            "The Magistrate has already answered one breach this turn."),
        Marker(BellRungId, "Court in Session",
            "The Magistrate has sat once. From the next bell, its rulings run down a turn each."),
        Marker(HearingOpenedId, "Hearing Opened",
            "The Magistrate has begun asking. Its first judgment was handed down before you spoke."),
        Marker(EstablishPendingId, "Judgment to Be Established",
            "Whatever you accept next turn is Binding for that turn as well."),
        .. Judgments.Select(j => BindingJudgment(j.Key, j.Name)),
    ];

    private static StatusData BindingJudgment(string key, string name) => new()
    {
        Id = BindingId(key),
        NameKey = $"Binding: {name}",
        DescriptionKey =
            $"The Judgment of {name} is law whatever you accept, for this many more of your turns.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, bool> Binding<TContext>(string key)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Magistrate, new StatusDefinitionId(BindingId(key))),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // A Judgment is law this turn if it was accepted, or if it is binding whatever you accepted.
    private static ICombatExpression<TContext, bool> InForce<TContext>(string key, int index)
        where TContext : class =>
        new OrExpression<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(Applicant, ChosenJudgmentCounter),
                ComparisonOperator.Equal, new ConstantExpression<TContext>(index + 1)),
            Binding<TContext>(key));

    // ── the asking ────────────────────────────────────────────────────────────────────────────────────────
    //
    // The prompt lives on the PLAYER: it is asked of the player's own turn, in the place Act II put every
    // "at the start of your turn, choose".
    //
    // ADAPTATION: all three Judgments are offered rather than two of them — an option list is a fixed list,
    // and the design's guard ("no pair may be offered if both are deterministically impossible to respect")
    // then holds trivially, because the traveller picks the one they can live under.
    private static StatusData ThreeJudgments()
    {
        IEffectNode<CardsDrawnTriggeredEffectContext> Accept(int index) =>
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, ChosenJudgmentCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(index + 1), relative: false),
                // "Establish the Judgment": what you accept is Binding for this turn as well.
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                            Magistrate, new StatusDefinitionId(EstablishPendingId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            Magistrate, new StatusDefinitionId(BindingId(Judgments[index].Key)),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                            Magistrate, new StatusDefinitionId(EstablishPendingId)),
                    ])),
            ]);

        // Everything the bell does is written HERE rather than on the turn's start, and in this order: the
        // rulings run down, THEN the question is put. A parked question holds the turn's other triggers
        // behind it, so a ruling established by the answer would otherwise be run down by the same bell
        // that established it.
        var runDown = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                    Magistrate, new StatusDefinitionId(BellRungId)),
                ComparisonOperator.Greater,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [.. Judgments.Select(j => (IEffectNode<CardsDrawnTriggeredEffectContext>)
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Binding<CardsDrawnTriggeredEffectContext>(j.Key),
                        new ModifyStatusStacksNode<CardsDrawnTriggeredEffectContext>(
                            Magistrate, new StatusDefinitionId(BindingId(j.Key)),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1))))]),
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                Magistrate, new StatusDefinitionId(BellRungId),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)));

        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(Magistrate),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    // Only the hand a turn OPENS with: a card that draws mid-turn does not reopen the
                    // hearing.
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<CardsDrawnTriggeredEffectContext>(Applicant),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    runDown,
                // ADAPTATION: the FIRST judgment is handed down rather than asked. The opening hand is
                // dealt as the fight is handed over, before there is anybody in the dock to put the
                // question to — so the Magistrate opens under Conduct and starts asking next turn.
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                            Magistrate, new StatusDefinitionId(HearingOpenedId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                        [Accept(0), Accept(1), Accept(2)],
                        [
                            "judged under Conduct: a fourth real card is a breach",
                            "judged under Measure: two cards of one price in a row are a breach",
                            "judged under Standing: ending the turn with no licence is a breach",
                        ],
                        count: 1, purpose: "under which custom would you be judged"),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Accept(0),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            Magistrate, new StatusDefinitionId(HearingOpenedId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ])),
                ])));

        return Rule(JudgmentOfferId, "Three Judgments",
            "Each turn the Magistrate asks under which custom you would rather be judged, and only that one "
            + "is ordinary law — until one of them matures into standing, and binds itself to you for two "
            + "turns on top of whatever you accept.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
            ]);
    }

    // ── the Magistrate ────────────────────────────────────────────────────────────────────────────────────
    private static StatusData TheMagistrateOfThorns()
    {
        var player = CombatantTargetSelectors.Source;
        var memory = CostMemory("thorns");

        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var judged = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                // Conduct — the fourth real card of the turn.
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        InForce<CardPlayedTriggeredEffectContext>("conduct", 0),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                    Violate<CardPlayedTriggeredEffectContext>(
                        Magistrate, JudgmentOfConductLaw, MagistrateNotedId)),
                // Measure — two in a row of one price.
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        InForce<CardPlayedTriggeredEffectContext>("measure", 1),
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                                ComparisonOperator.Equal, ThisCost()))),
                    Violate<CardPlayedTriggeredEffectContext>(
                        Magistrate, JudgmentOfMeasureLaw, MagistrateNotedId)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        // Standing — ending a turn with no licence left at all.
        var standing = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        InForce<TurnEndedTriggeredEffectContext>("standing", 2),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(SafeConductId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))),
                Violate<TurnEndedTriggeredEffectContext>(
                    Magistrate, JudgmentOfStandingLaw, MagistrateNotedId)));

        // The bell: the latch clears, the measure forgets, and every Binding Judgment runs down a turn.
        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Magistrate, new StatusDefinitionId(MagistrateNotedId)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, memory, new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                        relative: false),
                ])));

        // "When the Magistrate gains a newly created Claim, the Judgment that caused the final Trespass
        // becomes Binding." Which one that was is written down already: the act records the law being filed
        // for exactly as long as the filing lasts, and the standing is made inside it.
        EffectProgram<TContext> bind<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new CausalSequenceEffectNode<TContext>(
                    [.. Judgments.Select(BindIfItWas<TContext>)])));

        return Rule(MagistrateId, "The Magistrate of Thorns",
            "Break the custom you accepted and you owe the Magistrate 1 Trespass, once a turn. Three of them "
            + "make standing, and the custom that made it becomes BINDING for two of your turns whatever you "
            + "accept next — two may bind at once. Settle its demand in full and a Claim is struck off, the "
            + "oldest binding runs down a turn, and the Magistrate loses 7 HP.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    judged, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    standing, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    bind<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    bind<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // One Judgment binds if it was the one that filed the violation that matured. A copy that binds again
    // refreshes rather than stacking, and a third one binds only if there is room.
    private static IEffectNode<TContext> BindIfItWas<TContext>((string Key, string Name, int Law) judgment)
        where TContext : class
    {
        var active = Judgments
            .Select(j => (ICombatExpression<TContext, int>)new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantStatusStacksExpression<TContext>(
                    Magistrate, new StatusDefinitionId(BindingId(j.Key)))))
            .Aggregate((a, b) => new AddExpression<TContext>(a, b));

        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(Applicant, LawBeingFiledCounter),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(judgment.Law)),
                new OrExpression<TContext>(
                    Binding<TContext>(judgment.Key),
                    new ComparisonExpression<TContext>(
                        active, ComparisonOperator.Less,
                        new ConstantExpression<TContext>(MaxBindingJudgments)))),
            new CausalSequenceEffectNode<TContext>(
            [
                // Refresh to two rather than stack: a ruling that binds twice is still one ruling.
                new RemoveStatusNode<TContext>(Magistrate, new StatusDefinitionId(BindingId(judgment.Key))),
                new ApplyStatusNode<TContext>(
                    Magistrate, new StatusDefinitionId(BindingId(judgment.Key)),
                    new ConstantExpression<TContext>(BindingTurns)),
            ]));
    }

    // ── Full Redress ──────────────────────────────────────────────────────────────────────────────────────
    //
    // "Remove 1 turn from the OLDEST Binding Judgment" — the oldest is the one with the fewest turns left,
    // because every binding starts at two and runs down together.
    private static IEffectNode<TurnEndedTriggeredEffectContext> FullRedress()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        var steps = new List<IEffectNode<TurnEndedTriggeredEffectContext>>
        {
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                creditor, new StatusDefinitionId(MagistrateNotedId)),
        };

        // One turn off one binding: the shortest-lived first, and the latch is what makes "one" mean one.
        for (var remaining = 1; remaining <= BindingTurns; remaining++)
            foreach (var judgment in Judgments)
                steps.Add(new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new NotExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                    creditor, new StatusDefinitionId(MagistrateNotedId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                creditor, new StatusDefinitionId(BindingId(judgment.Key))),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(remaining))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(BindingId(judgment.Key)),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(MagistrateNotedId),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    ])));

        steps.Add(new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
            creditor, new StatusDefinitionId(MagistrateNotedId)));
        steps.Add(new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
            creditor,
            new StatusSelectionSpec(StatusPolarityFilter.Any) { Definition = new StatusDefinitionId(ClaimId) },
            new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)));
        steps.Add(new SetHealthNode<TurnEndedTriggeredEffectContext>(
            creditor,
            new SubtractExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(creditor),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(MagistrateRedressHealth))));

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(MagistrateId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(steps));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? MagistrateIntent(string enemyId, string intentId)
    {
        if (enemyId != MagistrateEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        var bindings = Judgments
            .Select(j => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    self, new StatusDefinitionId(BindingId(j.Key)))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        var owedTrespass = new CombatantStatusStacksFromSourceExpression<EnemyActionContext>(
            Applicant, new StatusDefinitionId(TrespassId), self);

        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            // "15 +2 per current Magistrate-source Trespass, max 21."
            "hear_the_trespass" => new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(15),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(2),
                        new MinExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3), owedTrespass)))),
            "establish_the_judgment" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(EstablishPendingId),
                    new ConstantExpression<EnemyActionContext>(1)),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(12)),
            ]),
            "demand_redress" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ConsumeClaim<EnemyActionContext>(self),
                    DemandWergild<EnemyActionContext>(self, 2),
                ])),
            "thorn_gavel" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Blow(21),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        bindings, ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(MaxBindingJudgments)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        new ApplyStatusNode<EnemyActionContext>(
                            Applicant, new StatusDefinitionId("paperwork"),
                            new ConstantExpression<EnemyActionContext>(1)),
                        new ApplyStatusNode<EnemyActionContext>(
                            Applicant, new StatusDefinitionId("doubt"),
                            new ConstantExpression<EnemyActionContext>(1)),
                    ])),
            ]),
            "stay_of_judgment" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                new ModifySelectedStatusStacksNode<EnemyActionContext>(
                    Applicant,
                    new StatusSelectionSpec(StatusPolarityFilter.Debuff)
                    {
                        Definition = new StatusDefinitionId(TrespassId),
                    },
                    new ConstantExpression<EnemyActionContext>(-1), sourceSelector: self),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(16)),
            ]),
            "judgment_of_the_green_docket" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(ClaimCeiling)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new RepeatEffectNode<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(ClaimCeiling),
                        ConsumeClaim<EnemyActionContext>(self)),
                    Blow(30),
                    DemandWergild<EnemyActionContext>(self, 2),
                    // The docket is cleared by the judgment it hands down.
                    .. Judgments.Select(j => (IEffectNode<EnemyActionContext>)
                        new ConditionalEffectNode<EnemyActionContext>(
                            Binding<EnemyActionContext>(j.Key),
                            new ModifyStatusStacksNode<EnemyActionContext>(
                                self, new StatusDefinitionId(BindingId(j.Key)),
                                new ConstantExpression<EnemyActionContext>(-1)))),
                ]),
                Blow(21)),
            _ => null,
        };

        return act is null ? null : new EffectProgram<EnemyActionContext>(act);
    }
}
