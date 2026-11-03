using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 2 — **The Notary of Old Growth** (360 HP).
//
// One of three laws is read each round, in turn:
//
//   Deed First   — opening the turn with a Deed.
//   Twice Priced — two cards of the same cost in a row.
//   Long Hand    — playing a fourth card in a turn.
//
// A law you break is written into the wood as a ring: from then on it stands every round on top of the one being
// read, and each ring adds 3 to the Notary's attacks.
//
// REWORKED 2026-11-03 (user, Act III pass): the Notarial Seals, the Counterseals and the Wergild are gone.
public static partial class ActThree
{
    public const string NotaryEnemyId = "notary_of_old_growth";
    public const string NotaryId = "three_rings_of_law";

    public const int NotaryCardLimit = 4;
    public const int RingDamage = 3;

    private static readonly (string Key, string Name, string Text)[] NotaryLaws =
    [
        ("deed_first", "Deed First", "opening your turn with a Deed"),
        ("twice_priced", "Twice Priced", "two cards of the same cost in a row"),
        ("long_hand", "Long Hand", "playing a fourth card in a turn"),
    ];

    private static string ReadId(string key) => $"notary_reads_{key}";
    private static string RingId(string key) => $"notary_ring_{key}";

    private static ICombatantTargetSelector Notary { get; } = Elite(NotaryId);
    private static CounterId NotaryMemory => CostMemory("notary");

    private static IEnumerable<StatusData> NotaryStatuses() =>
    [
        ThreeRingsOfLaw(),
        .. NotaryLaws.Select(l => Marker(ReadId(l.Key), $"Read: {l.Name}", $"This round {l.Text} breaks its law.")),
        .. NotaryLaws.Select(l => new StatusData
        {
            Id = RingId(l.Key),
            NameKey = $"Ring: {l.Name}",
            DescriptionKey = $"Written into the wood: {l.Text} breaks its law every round. Its attacks deal 3 more.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, RingDamage,
                    RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers = [],
        }),
    ];

    private static StatusData ThreeRingsOfLaw()
    {
        var player = CombatantTargetSelectors.Source;
        var reading = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new RoundNumberExpression<TurnStartedTriggeredEffectContext>(), new ConstantExpression<TurnStartedTriggeredEffectContext>(NotaryLaws.Length));
        // Round 1 reads the first law, round 2 the second, and so on.
        var turn = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(Notary)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                .. NotaryLaws.Select((l, i) => (IEffectNode<TurnStartedTriggeredEffectContext>)
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Is(reading, ComparisonOperator.Equal, (i + 1) % NotaryLaws.Length),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(Notary, new StatusDefinitionId(ReadId(l.Key)),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        @else: new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Notary, new StatusDefinitionId(ReadId(l.Key))))),
                SetCounter<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.Source, NotaryMemory, 0),
            ]));

        var costPlusOne = new AddExpression<CardPlayedTriggeredEffectContext>(PlayedCostValue, new ConstantExpression<CardPlayedTriggeredEffectContext>(1));
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Broke(string key) => key switch
        {
            "deed_first" => new AndExpression<CardPlayedTriggeredEffectContext>(
                Is(PlayedThisTurn, ComparisonOperator.Equal, 1), PlayedIs(Cards.CardAuthoring.DeedTag)),
            "twice_priced" => new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                CounterOn<CardPlayedTriggeredEffectContext>(player, NotaryMemory), ComparisonOperator.Equal, costPlusOne),
            _ => Is(PlayedThisTurn, ComparisonOperator.Equal, NotaryCardLimit),
        };
        // A law in force — read this round, or a ring — that is broken: a Claim, and the law becomes a ring.
        IEffectNode<CardPlayedTriggeredEffectContext> Judge((string Key, string Name, string Text) law) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(Broke(law.Key),
                    new OrExpression<CardPlayedTriggeredEffectContext>(
                        Wearing<CardPlayedTriggeredEffectContext>(Notary, ReadId(law.Key)),
                        Wearing<CardPlayedTriggeredEffectContext>(Notary, RingId(law.Key)))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    Breach<CardPlayedTriggeredEffectContext>(Notary),
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(Notary, new StatusDefinitionId(RingId(law.Key)),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                ]));
        var play = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            Alive<CardPlayedTriggeredEffectContext>(Notary),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                .. NotaryLaws.Select(Judge),
                SetCounter<CardPlayedTriggeredEffectContext>(player, NotaryMemory, costPlusOne),
            ]));

        return Rule(NotaryId, "Three Rings of Law",
            "One of three laws is read each round. A law you break becomes a ring: it stands every round, and adds 3 to its attacks.",
            [On("TurnStarted", turn, StatusTriggerScope.Anywhere), On("CardPlayed", play, StatusTriggerScope.Anywhere)]);
    }

    private static EffectProgram<EnemyActionContext>? NotaryIntent(string enemyId, string intentId) => null;
}
