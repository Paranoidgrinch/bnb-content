using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Scarab Host of the Sealed Granary. A black scarab colony moving as one organism inside
// three sealed grain chambers.
//
// The Host is armoured by its own seals — 6 Block a turn for each one still intact — and the encounter is
// about deciding, deliberately, which of them to break. Cut through the swarm's cover and into the colony
// itself and you get to pick one:
//
//   RATION SEAL  — a ration for the road: 1 Energy on your next turn. The colony eats too: 1 Strength.
//   PEST SEAL    — the pests are in the grain: the Host loses 12 HP, and you take 2 Poison for opening it.
//   BURDEN SEAL  — the paperwork burns with the chaff: 1 Burdened or 1 Paperwork comes off you. The colony
//                  packs the breach: 12 Block when its turn comes.
//
// Break all three and the Stores Stand Open: no seal armour left, the swarm pours through every crack with an
// extra body in each wave, and it is 15% softer to everything you hit it with.
public static partial class ActFour
{
    public const string ScarabHostEnemyId = "scarab_host_of_the_sealed_granary";

    public const string SealedGranaryId = "the_sealed_granary";
    public const string RationSealId = "ration_seal";
    public const string PestSealId = "pest_seal";
    public const string BurdenSealId = "burden_seal";
    public const string StoresStandOpenId = "stores_stand_open";
    public const string ExtraRationId = "extra_ration";
    public const string PackTheBreachId = "pack_the_breach";

    public const string BreakRationCardId = "break_the_ration_seal";
    public const string BreakPestCardId = "break_the_pest_seal";
    public const string BreakBurdenCardId = "break_the_burden_seal";

    private const int BlockPerSeal = 6;
    private const int PestSealHealthLoss = 12;
    private const int BreachBlock = 12;
    private const int OpenStoresDamagePercent = 115;

    // The Host's one latch: a seal is offered once per player turn, however many times you cut into it.
    public static CounterId BreachOfferedThisTurn => new("breach_offered_this_turn");

    private static readonly string[] Seals = [RationSealId, PestSealId, BurdenSealId];

    public static EffectProgram<EnemyActionContext>? ScarabHostIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "scarab_host_of_the_sealed_granary.seal_the_jars" => SealTheJars(),
            "scarab_host_of_the_sealed_granary.black_swarm" => Swarm(hits: 5, each: 5),
            "scarab_host_of_the_sealed_granary.through_every_crack" => ThroughEveryCrack(hits: 4, each: 6),
            _ => null,
        };

    public static IReadOnlyList<StatusData> ScarabHostStatuses() =>
    [
        SealedGranary(),
        Seal(RationSealId, "Ration Seal", "Break it for a ration on your next turn — and the colony eats too."),
        Seal(PestSealId, "Pest Seal", "Break it and the pests are in the grain — and on you."),
        Seal(BurdenSealId, "Burden Seal", "Break it and the paperwork burns with the chaff."),
        StoresStandOpen(),
        ExtraRation(),
        PackTheBreach(),
    ];

    // ── the granary ───────────────────────────────────────────────────────────────────────────────────────

    public static StatusData SealedGranary() => new()
    {
        Id = SealedGranaryId,
        NameKey = "The Sealed Granary",
        DescriptionKey =
            "This colony is armoured by its own seals: 6 Block each turn for every one still intact. Cut "
            + "through its cover and into the colony itself and you may break one — once a turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(SealBlock(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(OfferTheBreach(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    private static StatusData Seal(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Everything the open stores change that is not simply the absence of a seal: the colony has no walls
    // left to hide behind, so what you hit it with goes 15% further.
    public static StatusData StoresStandOpen() => new()
    {
        Id = StoresStandOpenId,
        NameKey = "The Stores Stand Open",
        DescriptionKey =
            "Every chamber is broken. No seal armour, an extra body in every wave — and everything you land "
            + "on this colony goes 15% further.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, OpenStoresDamagePercent, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    // The ration is for the ROAD, not for the turn you opened the jar on: it arrives with your next turn.
    public static StatusData ExtraRation() => new()
    {
        Id = ExtraRationId,
        NameKey = "A Ration for the Road",
        DescriptionKey = "1 extra Energy at the start of your next turn.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new GainResourceNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ExtraRationId)),
                ])),
                nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // …and the colony packs the breach on its own next turn, which is the price of the burden seal.
    public static StatusData PackTheBreach() => new()
    {
        Id = PackTheBreachId,
        NameKey = "Packing the Breach",
        DescriptionKey = "This colony gains 12 Block when its turn comes.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new GainBlockNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(BreachBlock)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PackTheBreachId)),
                ])),
                nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // 6 Block per intact seal, at the player's turn start — so the armour is standing when the player decides
    // whether they can cut through it, which is the decision the encounter is made of.
    private static EffectProgram<TurnStartedTriggeredEffectContext> SealBlock()
    {
        var host = Bearer(SealedGranaryId);

        IEffectNode<TurnStartedTriggeredEffectContext> PerSeal(string seal) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                    host, new StatusDefinitionId(seal)),
                new GainBlockNode<TurnStartedTriggeredEffectContext>(
                    host, new ConstantExpression<TurnStartedTriggeredEffectContext>(BlockPerSeal)));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        host, BreachOfferedThisTurn,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

                    .. Seals.Select(PerSeal),
                ])));
    }

    // The break opportunity. The master states it as two steps — all the Block removed by the player's
    // damage, and THEN HP damage caused — but the second contains the first: damage only reaches a colony's
    // health once its cover is gone. So the whole condition is "the player got through to it", which is a
    // damage event that took health, and the offer is the three cards for the seals still intact.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> OfferTheBreach()
    {
        // In a damage-received trigger the SOURCE is whoever struck — the player — and the receiver is the
        // event's target. The colony is therefore addressed the way every Act-IV body addresses itself when
        // the acting side is not its own: by the rule it is the only one wearing.
        var host = Bearer(SealedGranaryId);

        IEffectNode<DamageReceivedTriggeredEffectContext> OfferSeal(string seal, string cardId) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                    host, new StatusDefinitionId(seal)),
                new CreateCardInstanceNode<DamageReceivedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(cardId), CardZone.Hand,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    // It reached the colony itself …
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    // … and it has not already been opened this turn.
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
                            host, BreachOfferedThisTurn),
                        ComparisonOperator.Equal,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        host, BreachOfferedThisTurn,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),

                    OfferSeal(RationSealId, BreakRationCardId),
                    OfferSeal(PestSealId, BreakPestCardId),
                    OfferSeal(BurdenSealId, BreakBurdenCardId),
                ])));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // "Seal the Jars — only while at least one Seal remains." With every chamber broken there is nothing left
    // to seal, so the colony walls up instead: the master's "becomes unavailable", spelled as the thing a
    // swarm with no jars left actually does.
    private static EffectProgram<EnemyActionContext> SealTheJars() =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            AnySealIntact<EnemyActionContext>(),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(20)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(BurdenedId), Const(1)),
            ]),
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(24))));

    // The waves. Every hit is its own blow, so Block spent on the first still stands against the rest — and
    // an open granary sends one more body than a sealed one.
    private static EffectProgram<EnemyActionContext> Swarm(int hits, int each) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            .. Enumerable.Range(0, hits).Select(_ =>
                (IEffectNode<EnemyActionContext>)new DealDamageNode<EnemyActionContext>(Applicant, Const(each))),
            ExtraBodyWhenOpen(each),
        ]));

    private static EffectProgram<EnemyActionContext> ThroughEveryCrack(int hits, int each) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            .. Enumerable.Range(0, hits).Select(_ =>
                (IEffectNode<EnemyActionContext>)new DealDamageNode<EnemyActionContext>(Applicant, Const(each))),
            ExtraBodyWhenOpen(each),
            new ApplyStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(BurdenedId), Const(1)),
        ]));

    private static IEffectNode<EnemyActionContext> ExtraBodyWhenOpen(int each) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(StoresStandOpenId)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(each)));

    private static ICombatExpression<TContext, bool> AnySealIntact<TContext>() where TContext : class =>
        new OrExpression<TContext>(
            new TargetHasStatusExpression<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(RationSealId)),
            new OrExpression<TContext>(
                new TargetHasStatusExpression<TContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(PestSealId)),
                new TargetHasStatusExpression<TContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(BurdenSealId))));

    // ── breaking a seal ───────────────────────────────────────────────────────────────────────────────────

    // One card per chamber, offered only while that chamber is intact, and gone at the turn's end whether it
    // was used or not: the opportunity is this turn's, and taking none of them is a decision too.
    public static IReadOnlyList<CardData> ScarabSealCards() =>
    [
        SealCard(BreakRationCardId, "Break the Ration Seal", RationSealId,
            "Break open the ration chamber: 1 Energy on your next turn. The colony eats too — it gains 1 "
            + "Strength.",
            host =>
            [
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(ExtraRationId),
                    new ConstantExpression<CardPlayContext>(1)),
                new ApplyStatusNode<CardPlayContext>(
                    host, new StatusDefinitionId("strength"), new ConstantExpression<CardPlayContext>(1)),
            ]),

        SealCard(BreakPestCardId, "Break the Pest Seal", PestSealId,
            "Break open the pest chamber: the colony loses 12 HP, and what was living in there gets on you — "
            + "2 Poison.",
            host =>
            [
                new DealDamageNode<CardPlayContext>(
                    host, new ConstantExpression<CardPlayContext>(PestSealHealthLoss),
                    ignoresBlock: true, kind: DamageKind.DamageOverTime),
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId("poison"),
                    new ConstantExpression<CardPlayContext>(2)),
            ]),

        SealCard(BreakBurdenCardId, "Break the Burden Seal", BurdenSealId,
            "Break open the burden chamber: 1 Burdened comes off you, or 1 Paperwork if you carry no burden. "
            + "The colony packs the breach — 12 Block when its turn comes.",
            host =>
            [
                // Burdened first, and Paperwork only if there was no burden to take: the master offers one or
                // the other, not both.
                new ConditionalEffectNode<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusStacksExpression<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(BurdenedId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayContext>(0)),
                    new ModifyStatusStacksNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(BurdenedId),
                        new ConstantExpression<CardPlayContext>(-1)),
                    new ModifyStatusStacksNode<CardPlayContext>(
                        CombatantTargetSelectors.Source,
                        new StatusDefinitionId(Cards.Keywords.Paperwork),
                        new ConstantExpression<CardPlayContext>(-1))),

                new ApplyStatusNode<CardPlayContext>(
                    host, new StatusDefinitionId(PackTheBreachId), new ConstantExpression<CardPlayContext>(1)),
            ]),
    ];

    private static CardData SealCard(
        string id, string name, string sealId, string text,
        Func<ICombatantTargetSelector, IReadOnlyList<IEffectNode<CardPlayContext>>> consequence)
    {
        var host = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(SealedGranaryId)));

        var play = new List<IEffectNode<CardPlayContext>>
        {
            new RemoveStatusNode<CardPlayContext>(host, new StatusDefinitionId(sealId)),
        };
        play.AddRange(consequence(host));
        play.AddRange(new IEffectNode<CardPlayContext>[]
        {
            // …and if that was the last chamber, the stores stand open.
            new ConditionalEffectNode<CardPlayContext>(
                new NotExpression<CardPlayContext>(AnySealIntactOn<CardPlayContext>(host)),
                new ApplyStatusNode<CardPlayContext>(
                    host, new StatusDefinitionId(StoresStandOpenId),
                    new ConstantExpression<CardPlayContext>(1))),
        });

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId("seal"), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(new SequenceEffectNode<CardPlayContext>(play)),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }

    private static ICombatExpression<TContext, bool> AnySealIntactOn<TContext>(ICombatantTargetSelector host)
        where TContext : class =>
        new OrExpression<TContext>(
            new TargetHasStatusExpression<TContext>(host, new StatusDefinitionId(RationSealId)),
            new OrExpression<TContext>(
                new TargetHasStatusExpression<TContext>(host, new StatusDefinitionId(PestSealId)),
                new TargetHasStatusExpression<TContext>(host, new StatusDefinitionId(BurdenSealId))));
}
