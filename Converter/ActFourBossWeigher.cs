using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Weigher of the Unspoken Heart. A nearly motionless figure beside a balance. The feather
// needs no hand to hold it.
//
// This one does not ask for a number. It weighs the COMPOSITION of your turn: every Deed tips the scale
// toward the Heart, every Working toward the Feather, and what it judges at the end is where the pan came to
// rest — not how much you did, not how hard.
//
//   TRUE BALANCE (0)   the only kindness in the fight: its cover comes off, a burial comes off you, its next
//                      blow lands softer, and you take a Feather.
//   ACCEPTABLE (±1)    nothing. This is the width of the road.
//   HEAVY / HOLLOW (±2) two forms.
//   CONDEMNED (±3)     two burials, a point of Strength for it, and the scale is reset by force.
//
// Three Feathers and the heart is DECLARED LIGHT: the Weigher bleeds 22, loses everything it was standing
// behind, and takes a fifth more from you for a whole turn. Twice declared and it remembers — and in the
// second half the first card of every turn moves the pan TWO steps, so the road narrows to a line.
public static partial class ActFour
{
    public const string WeigherEnemyId = "weigher_of_the_unspoken_heart";

    public const string UnspokenHeartId = "the_unspoken_heart";
    public const string TowardTheHeartId = "toward_the_heart";
    public const string TowardTheFeatherId = "toward_the_feather";
    public const string FeatherId = "feather";
    public const string HeartDeclaredLightId = "heart_declared_light";
    public const string HeartRemembersId = "the_heart_remembers";
    public const string DeviationForgivenId = "deviation_forgiven";

    public const int BalanceLimit = 3;
    public const int FeathersToDeclare = 3;
    private const int DeclarationHealthLoss = 22;
    private const int RemembersAt = 305;
    private const int DeclarationsToRemember = 2;

    // The pan itself, the declarations it has cost, and the once-a-turn latch the second half runs on.
    public static CounterId Balance => new("balance");
    public static CounterId LightDeclarations => new("light_declarations");
    public static CounterId FirstCardThisTurn => new("first_card_this_turn");
    public static CounterId DeclaredTurns => new("declared_turns");

    public static EffectProgram<EnemyActionContext>? WeigherIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "weigher_of_the_unspoken_heart.read_the_deviation" => ReadTheDeviation(),
            "weigher_of_the_unspoken_heart.the_heart_has_spoken" => TheHeartHasSpoken(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> WeigherStatuses() =>
    [
        TheUnspokenHeart(),
        Pan(TowardTheHeartId, "Toward the Heart",
            "Your deeds have weighed the pan down. At 3 the heart is condemned."),
        Pan(TowardTheFeatherId, "Toward the Feather",
            "Your workings have lifted the pan. At 3 the heart is condemned for being hollow."),
        Feather(),
        HeartDeclaredLight(),
        HeartRemembers(),
        DeviationForgiven(),
    ];

    // ── the balance ───────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheUnspokenHeart() => new()
    {
        Id = UnspokenHeartId,
        NameKey = "The Unspoken Heart",
        DescriptionKey =
            "A Deed tips the pan toward the Heart, a Working toward the Feather, and at the end of your turn "
            + "it judges where the pan came to rest. Nought is true balance and worth a Feather; three either "
            + "way is condemnation.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TipThePan(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(Judge(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(OpenTheTurn(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(TheHeartRemembers(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    private static StatusData Pan(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData Feather() => new()
    {
        Id = FeatherId,
        NameKey = "Feather",
        DescriptionKey = "A turn weighed true. At 3 the heart is declared light.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData HeartDeclaredLight() => new()
    {
        Id = HeartDeclaredLightId,
        NameKey = "The Heart Is Declared Light",
        DescriptionKey = "Judged and found weightless. Everything you land on this figure goes 20% further.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 120, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    public static StatusData HeartRemembers() => new()
    {
        Id = HeartRemembersId,
        NameKey = "The Heart Remembers",
        DescriptionKey =
            "The second half. The first Deed or Working you play each turn moves the pan two steps instead "
            + "of one — the road is a line now.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData DeviationForgiven() => new()
    {
        Id = DeviationForgivenId,
        NameKey = "Weighed True",
        DescriptionKey = "This figure's next blow lands 6 softer.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.AddFlat, -6, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<DamageDealtTriggeredEffectContext>(
                new RemoveStatusNode<DamageDealtTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(DeviationForgivenId))),
                nameof(TriggerEvent.DamageDealt)),
        ],
    };

    // A card tips the pan by its KIND and never by its size — which is the whole identity: this figure does
    // not care how hard you hit, only what sort of turn you had.
    private static EffectProgram<CardPlayedTriggeredEffectContext> TipThePan()
    {
        var weigher = Bearer(UnspokenHeartId);

        // Two steps for the first card of a turn once the heart remembers, one for everything after.
        var step = new ConditionalValue(weigher);

        IEffectNode<CardPlayedTriggeredEffectContext> Tip(string tag, bool towardHeart) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        weigher, Balance,
                        new MaxExpression<CardPlayedTriggeredEffectContext>(
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(-BalanceLimit),
                            new MinExpression<CardPlayedTriggeredEffectContext>(
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(BalanceLimit),
                                new AddExpression<CardPlayedTriggeredEffectContext>(
                                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                        weigher, Balance),
                                    towardHeart ? step.Forward : step.Back))),
                        relative: false),

                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        weigher, FirstCardThisTurn,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false),

                    ShowThePan<CardPlayedTriggeredEffectContext>(weigher),
                ]));

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    Tip(Cards.CardAuthoring.DeedTag, towardHeart: true),
                    Tip(Cards.CardAuthoring.WorkingTag, towardHeart: false),
                ])));
    }

    // How far one card moves the pan: two for the first of a turn once the heart remembers, one otherwise.
    private sealed class ConditionalValue
    {
        private readonly ICombatantTargetSelector _weigher;

        public ConditionalValue(ICombatantTargetSelector weigher) => _weigher = weigher;

        private ICombatExpression<CardPlayedTriggeredEffectContext, int> Size =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1),
                new MultiplyExpression<CardPlayedTriggeredEffectContext>(
                    new MinExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                            _weigher, new StatusDefinitionId(HeartRemembersId)),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    new SubtractExpression<CardPlayedTriggeredEffectContext>(
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1),
                        new MinExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                _weigher, FirstCardThisTurn),
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1)))));

        public ICombatExpression<CardPlayedTriggeredEffectContext, int> Forward => Size;

        public ICombatExpression<CardPlayedTriggeredEffectContext, int> Back =>
            new SubtractExpression<CardPlayedTriggeredEffectContext>(
                new ConstantExpression<CardPlayedTriggeredEffectContext>(0), Size);
    }

    // The pan is a signed number, and a signed number is not a thing a player can look at. So it is also two
    // faces, kept in step with it: which way it leans, and how far.
    private static IEffectNode<TContext> ShowThePan<TContext>(ICombatantTargetSelector weigher)
        where TContext : class
    {
        var balance = new CombatantCounterExpression<TContext>(weigher, Balance);

        return new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(weigher, new StatusDefinitionId(TowardTheHeartId)),
            new RemoveStatusNode<TContext>(weigher, new StatusDefinitionId(TowardTheFeatherId)),

            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    balance, ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new ApplyStatusNode<TContext>(
                    weigher, new StatusDefinitionId(TowardTheHeartId), balance, sourceSelector: weigher)),

            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    balance, ComparisonOperator.Less, new ConstantExpression<TContext>(0)),
                new ApplyStatusNode<TContext>(
                    weigher, new StatusDefinitionId(TowardTheFeatherId),
                    new SubtractExpression<TContext>(new ConstantExpression<TContext>(0), balance),
                    sourceSelector: weigher)),
        ]);
    }

    // ── the judgment ──────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<TurnEndedTriggeredEffectContext> Judge()
    {
        var weigher = Bearer(UnspokenHeartId);
        var balance = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(weigher, Balance);
        var deviation = new AbsExpression<TurnEndedTriggeredEffectContext>(balance);

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> At(int step) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                deviation, ComparisonOperator.Equal,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(step));

        var trueBalance = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                weigher, StandardCombatIds.BlockDefensivePool,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(-18)),

            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(EntombedId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),

            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                weigher, new StatusDefinitionId(DeviationForgivenId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: weigher),

            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                weigher, new StatusDefinitionId(FeatherId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: weigher),

            // Three of them and the heart is declared light.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        weigher, new StatusDefinitionId(FeatherId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(FeathersToDeclare)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        weigher, new StatusDefinitionId(FeatherId)),
                    new DealDamageNode<TurnEndedTriggeredEffectContext>(
                        weigher,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(DeclarationHealthLoss),
                        ignoresBlock: true, kind: DamageKind.DamageOverTime),
                    new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                        weigher, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-999)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        weigher, new StatusDefinitionId(HeartDeclaredLightId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: weigher),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        weigher, DeclaredTurns,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        weigher, LightDeclarations,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                ])),
        ]);

        var condemned = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(EntombedId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(2), sourceSelector: weigher),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                weigher, new StatusDefinitionId("strength"),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: weigher),
            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                weigher, Balance, new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
            ShowThePan<TurnEndedTriggeredEffectContext>(weigher),
        ]);

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    At(0),
                    trueBalance,
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        At(2),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(2),
                            sourceSelector: weigher),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            At(BalanceLimit), condemned)))));
    }

    // The declaration lasts one whole player turn, and the once-a-turn latch the second half runs on is
    // cleared here as well.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheTurn()
    {
        var weigher = Bearer(UnspokenHeartId);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        weigher, FirstCardThisTurn,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            weigher, new StatusDefinitionId(HeartDeclaredLightId)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    weigher, DeclaredTurns),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                weigher, new StatusDefinitionId(HeartDeclaredLightId)),
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                weigher, DeclaredTurns,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                relative: false))),
                ])));
    }

    // The transition: two declarations, or half its blood — whichever comes first. It is not an attack. The
    // scale is levelled, the feathers are gone, its cover is gone, and a burial comes off the player.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheHeartRemembers()
    {
        var weigher = Bearer(UnspokenHeartId);

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new NotExpression<DamageReceivedTriggeredEffectContext>(
                        new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                            weigher, new StatusDefinitionId(HeartRemembersId))),
                    new OrExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
                                weigher, LightDeclarations),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(
                                DeclarationsToRemember)),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(weigher),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(RemembersAt)))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        weigher, new StatusDefinitionId(HeartRemembersId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                        sourceSelector: weigher),

                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        weigher, Balance,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0), relative: false),
                    ShowThePan<DamageReceivedTriggeredEffectContext>(weigher),

                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                        weigher, new StatusDefinitionId(FeatherId)),
                    new ModifyDefensivePoolNode<DamageReceivedTriggeredEffectContext>(
                        weigher, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(-999)),

                    new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(EntombedId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)),
                ])));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // It reads the deviation and answers it: the further the pan is from true, the harder. It does not move
    // the pan doing so — reading is not weighing.
    private static EffectProgram<EnemyActionContext> ReadTheDeviation() =>
        new(new DealDamageNode<EnemyActionContext>(
            Applicant,
            new MinExpression<EnemyActionContext>(
                Const(34),
                new AddExpression<EnemyActionContext>(
                    Const(22),
                    new MultiplyExpression<EnemyActionContext>(
                        Const(4),
                        new AbsExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(
                                CombatantTargetSelectors.Source, Balance)))))));

    // …and when the heart has spoken, what it says is where the pan was standing. Then the scale is levelled.
    private static EffectProgram<EnemyActionContext> TheHeartHasSpoken()
    {
        var self = CombatantTargetSelectors.Source;
        var deviation = new AbsExpression<EnemyActionContext>(
            new CombatantCounterExpression<EnemyActionContext>(self, Balance));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        deviation, ComparisonOperator.GreaterOrEqual, Const(BalanceLimit)),
                    new DealDamageNode<EnemyActionContext>(Applicant, Const(40)),
                    new ConditionalEffectNode<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            deviation, ComparisonOperator.Equal, Const(2)),
                        new DealDamageNode<EnemyActionContext>(Applicant, Const(36)),
                        new DealDamageNode<EnemyActionContext>(Applicant, Const(32)))),

                new SetCombatantCounterNode<EnemyActionContext>(self, Balance, Const(0), relative: false),
                ShowThePan<EnemyActionContext>(self),
            ]));
    }
}
