using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 2 — **The Notary of Old Growth** (360 HP).
//
// > A violation can stop being an incident and become durable PRECEDENT, unless the player earns and spends
// > legal counterauthority.
//
// An ancient oak has grown around a stone legal seat. Ownership marks, old cuts, pawprints and names appear
// as annual rings, and dark resin seals whatever the tree has witnessed often enough to call law.
//
// Three Rings, of which exactly one is ordinary law and it rotates every turn:
//
//   Passage   — two cards in a row of the same Base Cost.
//   Restraint — the fourth real card of a turn.
//   Keeping   — ending a turn with nothing real left in hand.
//
// The escalation is not the clock and not the HP bar: it is the player's own record. Standing made under
// the ring that happens to be governing PROPOSES that ring as precedent, and the Notary's next sealing makes
// it permanent — law in addition to whatever is rotating. Two seals is as many as the tree will hold; a
// third becomes WEIGHT OF PRECEDENT, which is worth damage rather than law.
//
// The counterauthority is bought with restitution: settling in full hands the player a **Counterseal**, and
// a Counterseal prises one Notarial Seal back out of the wood.
public static partial class ActThree
{
    public const string NotaryEnemyId = "notary_of_old_growth";
    public const string NotaryId = "three_rings_of_law";

    public const string WeightOfPrecedentId = "weight_of_precedent";
    public const string CounterSealId = "counterseal";
    public const string CounterSealCardId = "spend_a_counterseal";
    public const string HeartwoodId = "the_heartwood";
    public const string HeartwoodPendingId = "the_heartwood_bears_witness";
    public const string RevisionRefusedId = "heartwood_refuses_revision";
    public const string RingsOpenedId = "rings_opened";
    public const string RingRestoredId = "ring_restored";

    public const int RingOfPassageLaw = 29;
    public const int RingOfRestraintLaw = 30;
    public const int RingOfKeepingLaw = 31;

    private const int MaxSeals = 2;
    private const int MaxWeight = 2;
    private const int MaxCounterseals = 2;
    private const int PrecedentsForHeartwood = 2;
    private const int NotaryTransitionHealth = 180;
    private const int NotarySignatureHealth = 90;

    public static readonly TagId CounterSealTag = new("counterseal");

    // Which Ring is rotating: 0 Passage, 1 Restraint, 2 Keeping. And which one the record has proposed as
    // precedent: 0 none, otherwise the ring's index plus one.
    public static CounterId NotaryRingCounter => new("notary_ring");
    public static CounterId ProposedPrecedentCounter => new("proposed_precedent");
    public static CounterId PrecedentsEstablishedCounter => new("precedents_established");

    private static readonly (string Key, string Name, int Law)[] Rings =
    [
        ("passage", "Passage", RingOfPassageLaw),
        ("restraint", "Restraint", RingOfRestraintLaw),
        ("keeping", "Keeping", RingOfKeepingLaw),
    ];

    private static string RotatingId(string key) => $"ring_rotating_{key}";
    private static string SealId(string key) => $"notarial_seal_{key}";
    private static string BrokenSealId(string key) => $"seal_broken_{key}";
    private static string RingNotedId(string key) => $"ring_noted_{key}";

    private static ICombatantTargetSelector Notary { get; } = Elite(NotaryId);

    private static IEnumerable<StatusData> NotaryStatuses() =>
    [
        ThreeRingsOfLaw(),
        WeightOfPrecedent(),
        CounterSeal(),
        Marker(HeartwoodId, "The Heartwood",
            "The tree has heard enough. A sealed ring now takes 2 Trespass at its first breach each turn."),
        Marker(HeartwoodPendingId, "The Heartwood Bears Witness",
            "The Notary's next action is the tree's own testimony, and not a blow."),
        Marker(RevisionRefusedId, "Refuses Revision",
            "The next Counterseal still works — and the wood closes over it for 8 Block."),
        Marker(RingsOpenedId, "Rings Opened",
            "The Notary has sat once. From the next bell, the rings turn."),
        Marker(RingRestoredId, "Ring Restored",
            "One broken seal has already been declared unbroken."),
        .. Rings.SelectMany(r => new[]
        {
            Marker(RotatingId(r.Key), $"Ring of {r.Name}",
                $"The Ring of {r.Name} is the rotating law this turn."),
            Marker(SealId(r.Key), $"Sealed: {r.Name}",
                $"The Ring of {r.Name} has been sealed in sap. It is law whatever is rotating."),
            Marker(BrokenSealId(r.Key), $"Seal Broken: {r.Name}",
                $"A Counterseal prised the Ring of {r.Name} back out of the wood. The Notary remembers."),
            Marker(RingNotedId(r.Key), $"{r.Name} Noted",
                $"The Ring of {r.Name} has already answered a breach this turn."),
        }),
    ];

    private static StatusData WeightOfPrecedent() => new()
    {
        Id = WeightOfPrecedentId,
        NameKey = "Weight of Precedent",
        DescriptionKey =
            "A precedent the wood had no room to seal. It is not law — it is worth 3 damage to whatever the "
            + "Notary signs. At most 2.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData CounterSeal() => new()
    {
        Id = CounterSealId,
        NameKey = "Counterseal",
        DescriptionKey =
            "Legal counterauthority, bought by settling with the Notary in full. Spend one to prise a "
            + "Notarial Seal back out of the wood. At most 2.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, bool> Sealed<TContext>(string key)
        where TContext : class =>
        Wears<TContext>(Notary, SealId(key));

    // A Ring is law if it is the one rotating, or if the wood has sealed it.
    private static ICombatExpression<TContext, bool> RingIsLaw<TContext>(string key)
        where TContext : class =>
        new OrExpression<TContext>(Wears<TContext>(Notary, RotatingId(key)), Sealed<TContext>(key));

    // In the heartwood a sealed ring's first breach each turn is worth two.
    private static ICombatExpression<TContext, int> RingWeight<TContext>(string key)
        where TContext : class =>
        new AddExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new MultiplyExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        Notary, new StatusDefinitionId(HeartwoodId)),
                    new CombatantStatusStacksExpression<TContext>(
                        Notary, new StatusDefinitionId(SealId(key))))));

    // ── the three rings ───────────────────────────────────────────────────────────────────────────────────
    private static StatusData ThreeRingsOfLaw()
    {
        var player = CombatantTargetSelectors.Source;
        var memory = CostMemory("old_growth");

        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var rings = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                // Passage — two cards in a row of one price.
                Breach("passage", RingOfPassageLaw,
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                            ComparisonOperator.Equal, ThisCost()))),
                // Restraint — the fourth real card.
                Breach("restraint", RingOfRestraintLaw,
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        // Keeping — ending a turn with nothing real left in hand.
        var keeping = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                Breach("keeping", RingOfKeepingLaw,
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        RealCardsLeftInHand<TurnEndedTriggeredEffectContext>(),
                        ComparisonOperator.LessOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))));

        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, memory, new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                        relative: false),
                    .. Rings.Select(r => (IEffectNode<TurnStartedTriggeredEffectContext>)
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            Notary, new StatusDefinitionId(RingNotedId(r.Key)))),
                    // The first bell of the fight opens the rings on Passage rather than turning them.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            Wears<TurnStartedTriggeredEffectContext>(Notary, RingsOpenedId)),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Notary, new StatusDefinitionId(RingsOpenedId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            Notary, NotaryRingCounter,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true)),
                    DrawTheRings<TurnStartedTriggeredEffectContext>(),
                    OfferACounterseal<TurnStartedTriggeredEffectContext>(),
                    QueueTheHeartwood<TurnStartedTriggeredEffectContext>(),
                ])));

        // "When the Notary gains a newly created Claim from the currently GOVERNING Ring, record that Ring
        // as Proposed Precedent." Which ring that was is on the record already — the act writes the law
        // down as the violation goes past, and the standing is made inside that filing.
        EffectProgram<TContext> propose<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new CausalSequenceEffectNode<TContext>(
                    [.. Rings.Select((r, i) => (IEffectNode<TContext>)new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(Applicant, LawBeingFiledCounter),
                            ComparisonOperator.Equal, new ConstantExpression<TContext>(r.Law)),
                        new SetCombatantCounterNode<TContext>(
                            Notary, ProposedPrecedentCounter,
                            new ConstantExpression<TContext>(i + 1), relative: false)))])));

        return Rule(NotaryId, "Three Rings of Law",
            "Three rings, of which one is law and it turns every turn: Passage (two cards in a row of one "
            + "price), Restraint (a fourth real card) and Keeping (ending a turn with nothing real in hand). "
            + "Standing made under the ring that is governing PROPOSES that ring as precedent, and the "
            + "Notary's sealing makes it permanent — law on top of whatever is turning. Settle its demand "
            + "in full and you are handed a COUNTERSEAL, which prises one seal back out of the wood.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    rings, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    keeping, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    propose<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    propose<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // One ring answering one breach: once a turn each, and doubled off a seal once the heartwood is awake.
    private static IEffectNode<TContext> Breach<TContext>(
        string key, int law, ICombatExpression<TContext, bool> broken)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(RingIsLaw<TContext>(key), broken),
            Violate<TContext>(Notary, law, RingNotedId(key), stacks: RingWeight<TContext>(key)));

    // The rotation, written out of the one number that holds it and redrawn rather than added to.
    private static IEffectNode<TContext> DrawTheRings<TContext>()
        where TContext : class
    {
        var steps = new List<IEffectNode<TContext>>();
        foreach (var ring in Rings)
            steps.Add(new RemoveStatusNode<TContext>(Notary, new StatusDefinitionId(RotatingId(ring.Key))));

        for (var position = 0; position < Rings.Length; position++)
            steps.Add(new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new RemainderExpression<TContext>(
                        new CombatantCounterExpression<TContext>(Notary, NotaryRingCounter),
                        new ConstantExpression<TContext>(Rings.Length)),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(position)),
                new ApplyStatusNode<TContext>(
                    Notary, new StatusDefinitionId(RotatingId(Rings[position].Key)),
                    new ConstantExpression<TContext>(1))));

        return new CausalSequenceEffectNode<TContext>(steps);
    }

    // "At 2 established precedents queue The Heartwood Bears Witness. Failsafe: 180 HP."
    private static IEffectNode<TContext> QueueTheHeartwood<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Wears<TContext>(Notary, HeartwoodId)),
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(Notary, HeartwoodPendingId)),
                    new OrExpression<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(Notary, PrecedentsEstablishedCounter),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TContext>(PrecedentsForHeartwood)),
                        new ComparisonExpression<TContext>(
                            new CombatantCurrentHealthExpression<TContext>(Notary),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TContext>(NotaryTransitionHealth))))),
            new ApplyStatusNode<TContext>(
                Notary, new StatusDefinitionId(HeartwoodPendingId), new ConstantExpression<TContext>(1)));

    // ── the Counterseal ───────────────────────────────────────────────────────────────────────────────────
    public static CardData SpendACounterseal() => new()
    {
        Id = CounterSealCardId,
        NameKey = "Spend a Counterseal",
        DescriptionKey =
            "Spend 1 Counterseal to prise one Notarial Seal back out of the wood. The ring returns to "
            + "ordinary rotation; the Weight of Precedent already earned stays where it is.",
        Costs = [],
        Tags = [CounterSealTag, new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantStatusStacksExpression<CardPlayContext>(
                                Applicant, new StatusDefinitionId(CounterSealId)),
                            ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardPlayContext>(1)),
                        Rings.Select(r => (ICombatExpression<CardPlayContext, bool>)
                                Sealed<CardPlayContext>(r.Key))
                            .Aggregate((a, b) => new OrExpression<CardPlayContext>(a, b))),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new ModifyStatusStacksNode<CardPlayContext>(
                            Applicant, new StatusDefinitionId(CounterSealId),
                            new ConstantExpression<CardPlayContext>(-1)),
                        new ChooseOptionsNode<CardPlayContext>(
                            [.. Rings.Select(r => PriseOut(r.Key))],
                            [.. Rings.Select(r => $"prise the Ring of {r.Name} back out")],
                            count: 1, purpose: "which seal"),
                        // "Heartwood Refuses Revision: the Counterseal still works, and the wood closes
                        // over it."
                        new ConditionalEffectNode<CardPlayContext>(
                            Wears<CardPlayContext>(Notary, RevisionRefusedId),
                            new CausalSequenceEffectNode<CardPlayContext>(
                            [
                                new GainBlockNode<CardPlayContext>(
                                    Notary, new ConstantExpression<CardPlayContext>(8)),
                                new RemoveStatusNode<CardPlayContext>(
                                    Notary, new StatusDefinitionId(RevisionRefusedId)),
                            ])),
                    ])),
                AnotherCounterseal(),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    private static IEffectNode<CardPlayContext> PriseOut(string key) =>
        new ConditionalEffectNode<CardPlayContext>(
            Sealed<CardPlayContext>(key),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new RemoveStatusNode<CardPlayContext>(Notary, new StatusDefinitionId(SealId(key))),
                // The wood remembers what was taken out of it.
                new ApplyStatusNode<CardPlayContext>(
                    Notary, new StatusDefinitionId(BrokenSealId(key)),
                    new ConstantExpression<CardPlayContext>(1)),
            ]));

    private static IEffectNode<TContext> OfferACounterseal<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        Applicant, new StatusDefinitionId(CounterSealId)),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand, CounterSealTag),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
            new CreateCardInstanceNode<TContext>(
                Applicant, new CardDefinitionId(CounterSealCardId), CardZone.Hand,
                new ConstantExpression<TContext>(1)));

    private static IEffectNode<CardPlayContext> AnotherCounterseal() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        Applicant, new StatusDefinitionId(CounterSealId)),
                    ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantZoneCardCountExpression<CardPlayContext>(
                        Applicant, CardZone.Hand, CounterSealTag),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<CardPlayContext>(1))),
            new CreateCardInstanceNode<CardPlayContext>(
                Applicant, new CardDefinitionId(CounterSealCardId), CardZone.Hand,
                new ConstantExpression<CardPlayContext>(1)));

    // ── settling with the Notary ──────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> TheCounterseal()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        creditor, new StatusDefinitionId(NotaryId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(CounterSealId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(MaxCounterseals))),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(CounterSealId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                OfferACounterseal<TurnEndedTriggeredEffectContext>(),
            ]));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? NotaryIntent(string enemyId, string intentId)
    {
        if (enemyId != NotaryEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        var seals = Rings
            .Select(r => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    self, new StatusDefinitionId(SealId(r.Key)))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        var weight = new CombatantStatusStacksExpression<EnemyActionContext>(
            self, new StatusDefinitionId(WeightOfPrecedentId));

        // Distinct active laws: a sealed ring and the rotating ring count separately only if they differ.
        var activeRings = Rings
            .Select(r => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new AddExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(RotatingId(r.Key))),
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(SealId(r.Key))))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        IEffectNode<EnemyActionContext>? ordinary = intentId switch
        {
            // Read the Annual Clause → Stamp with Centuries.
            "read_the_annual_clause" => Heartwood(
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(15),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3), seals))),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(18),
                        new AddExpression<EnemyActionContext>(
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(4), seals),
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(3), weight))))),
            // Seal It in Sap → Declare the Ring Unbroken.
            "seal_it_in_sap" => Heartwood(SealItInSap(), DeclareTheRingUnbroken()),
            // Witnessed by Every Branch → Heartwood Refuses Revision.
            "witnessed_by_every_branch" => Heartwood(
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(12),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(4), activeRings))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(24)),
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(RevisionRefusedId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ])),
            // Demand the Countermark → Sap-Sealed Redress.
            "demand_the_countermark" => Heartwood(
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        ConsumeClaim<EnemyActionContext>(self),
                        DemandWergild<EnemyActionContext>(self, 2),
                    ]),
                    DemandWergild<EnemyActionContext>(self, 1)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    DemandWergild<EnemyActionContext>(self, 2),
                    new GainBlockNode<EnemyActionContext>(
                        self,
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(6), seals)),
                ])),
            // The Old Growth's own gift, in both phases: a licence, and the law turns past the sealed rings.
            "old_growth_signature" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        activeRings, ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(Rings.Length)),
                    Blow(16),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        new SetCombatantCounterNode<EnemyActionContext>(
                            self, NotaryRingCounter, new ConstantExpression<EnemyActionContext>(1),
                            relative: true),
                        DrawTheRings<EnemyActionContext>(),
                    ])),
            ]),
            "every_ring_is_evidence" => new ConditionalEffectNode<EnemyActionContext>(
                new OrExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCurrentHealthExpression<EnemyActionContext>(self),
                        ComparisonOperator.LessOrEqual,
                        new ConstantExpression<EnemyActionContext>(NotarySignatureHealth)),
                    new AndExpression<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            seals, ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<EnemyActionContext>(MaxSeals)),
                        new ComparisonExpression<EnemyActionContext>(
                            weight, ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<EnemyActionContext>(MaxWeight)))),
                EveryRingIsEvidence(activeRings, seals, weight),
                Blow(16)),
            _ => null,
        };

        return ordinary is null
            ? null
            : new EffectProgram<EnemyActionContext>(
                new ConditionalEffectNode<EnemyActionContext>(
                    Wears<EnemyActionContext>(self, HeartwoodPendingId),
                    TheHeartwoodBearsWitness(), ordinary));
    }

    private static IEffectNode<EnemyActionContext> Heartwood(
        IEffectNode<EnemyActionContext> younger, IEffectNode<EnemyActionContext> heartwood) =>
        new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(CombatantTargetSelectors.Source, HeartwoodId),
            heartwood, younger);

    // "Consume 1 Claim, place a Notarial Seal on the Proposed Ring, gain 12 Block. Otherwise gain 20."
    // A third distinct seal becomes Weight of Precedent instead — bounded information, not a third law.
    private static IEffectNode<EnemyActionContext> SealItInSap()
    {
        var self = CombatantTargetSelectors.Source;

        var seals = Rings
            .Select(r => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    self, new StatusDefinitionId(SealId(r.Key)))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        var steps = new List<IEffectNode<EnemyActionContext>>();
        for (var i = 0; i < Rings.Length; i++)
            steps.Add(new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCounterExpression<EnemyActionContext>(self, ProposedPrecedentCounter),
                        ComparisonOperator.Equal, new ConstantExpression<EnemyActionContext>(i + 1)),
                    new NotExpression<EnemyActionContext>(Sealed<EnemyActionContext>(Rings[i].Key))),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        seals, ComparisonOperator.Less, new ConstantExpression<EnemyActionContext>(MaxSeals)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        new ApplyStatusNode<EnemyActionContext>(
                            self, new StatusDefinitionId(SealId(Rings[i].Key)),
                            new ConstantExpression<EnemyActionContext>(1)),
                        new SetCombatantCounterNode<EnemyActionContext>(
                            self, PrecedentsEstablishedCounter,
                            new ConstantExpression<EnemyActionContext>(1), relative: true),
                    ]),
                    // No room in the wood: the precedent is carried as weight instead.
                    new ConditionalEffectNode<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                self, new StatusDefinitionId(WeightOfPrecedentId)),
                            ComparisonOperator.Less, new ConstantExpression<EnemyActionContext>(MaxWeight)),
                        new ApplyStatusNode<EnemyActionContext>(
                            self, new StatusDefinitionId(WeightOfPrecedentId),
                            new ConstantExpression<EnemyActionContext>(1))))));

        var proposal = new AndExpression<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new CombatantCounterExpression<EnemyActionContext>(self, ProposedPrecedentCounter),
                ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
            new ComparisonExpression<EnemyActionContext>(
                new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)));

        return new ConditionalEffectNode<EnemyActionContext>(
            proposal,
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                ConsumeClaim<EnemyActionContext>(self),
                .. steps,
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, ProposedPrecedentCounter, new ConstantExpression<EnemyActionContext>(0),
                    relative: false),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(12)),
            ]),
            new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(20)));
    }

    // "If a removed Seal exists and the Notary has 2 Claims, consume 2 and restore one. Never a third."
    private static IEffectNode<EnemyActionContext> DeclareTheRingUnbroken()
    {
        var self = CombatantTargetSelectors.Source;

        var seals = Rings
            .Select(r => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    self, new StatusDefinitionId(SealId(r.Key)))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        var anyBroken = Rings
            .Select(r => (ICombatExpression<EnemyActionContext, bool>)
                Wears<EnemyActionContext>(self, BrokenSealId(r.Key)))
            .Aggregate((a, b) => new OrExpression<EnemyActionContext>(a, b));

        var steps = new List<IEffectNode<EnemyActionContext>>
        {
            ConsumeClaim<EnemyActionContext>(self),
            ConsumeClaim<EnemyActionContext>(self),
        };
        // The first ring the wood remembers losing, and only one. Its OWN cell: the pending transition is
        // a different fact, and a rule that borrowed it would cancel the tree's own testimony.
        steps.Add(new RemoveStatusNode<EnemyActionContext>(
            self, new StatusDefinitionId(RingRestoredId)));
        foreach (var ring in Rings)
            steps.Add(new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    new NotExpression<EnemyActionContext>(
                        Wears<EnemyActionContext>(self, RingRestoredId)),
                    Wears<EnemyActionContext>(self, BrokenSealId(ring.Key))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(SealId(ring.Key)),
                        new ConstantExpression<EnemyActionContext>(1)),
                    new RemoveStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(BrokenSealId(ring.Key))),
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(RingRestoredId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ])));
        steps.Add(new RemoveStatusNode<EnemyActionContext>(
            self, new StatusDefinitionId(RingRestoredId)));

        return new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                anyBroken,
                new AndExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(2)),
                    new ComparisonExpression<EnemyActionContext>(
                        seals, ComparisonOperator.Less,
                        new ConstantExpression<EnemyActionContext>(MaxSeals)))),
            new CausalSequenceEffectNode<EnemyActionContext>(steps),
            new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(20)));
    }

    // "Preserve Claims, Seals and Weight; the player gains 1 Safe-Conduct; the Notary gains 18 Block; no
    // direct attack."
    private static IEffectNode<EnemyActionContext> TheHeartwoodBearsWitness()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(HeartwoodId), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(HeartwoodPendingId)),
        ]);
    }

    // "16 damage, +4 per distinct active Ring, +2 per Weight; a demand for 1 per Notarial Seal."
    private static IEffectNode<EnemyActionContext> EveryRingIsEvidence(
        ICombatExpression<EnemyActionContext, int> activeRings,
        ICombatExpression<EnemyActionContext, int> seals,
        ICombatExpression<EnemyActionContext, int> weight)
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(16),
                    new AddExpression<EnemyActionContext>(
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(4), activeRings),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(2), weight)))),
            new RepeatEffectNode<EnemyActionContext>(seals, DemandWergild<EnemyActionContext>(self, 1)),
        ]);
    }
}
