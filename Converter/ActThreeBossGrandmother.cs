using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 3 — **Grandmother Clause** (350 HP).
//
// > Every gift is genuinely optional; accepting enough hospitality eventually makes the player subject to
// > household law.
//
// A very old woman sits before a small house that may also be a hollow tree, a stone circle, a cottage, or
// no structure at all. Tea, honey, bread and one empty chair wait on the table. She forces nothing. That is
// the danger.
//
// A **Courtesy** is a gift and a condition in the same breath, and DECLINING one is never a violation. Take
// the tea and you have promised to work with your hands this turn; take the chair and you have promised not
// to empty your hand; take the honey and you have promised to leave something in your purse. Keep the
// promise and it costs her 5 HP. Break it and it is two Trespass at once — which one licence still refuses
// whole, because that is what a licence is for.
//
// Three accepted courtesies and you are staying the night, and from then on she may set two places. Three
// remembered favours and she declares a HOUSE RULE: a courtesy's condition with no gift attached, standing
// for two of your turns whether you took anything or not.
public static partial class ActThree
{
    public const string GrandmotherEnemyId = "grandmother_clause";
    public const string GrandmotherId = "the_courtesies";

    public const string RememberedFavorId = "remembered_favor";
    public const string HouseholdLawId = "household_law";
    public const string StayLongerPendingId = "stay_a_little_longer";
    public const string SetAnotherPlaceId = "set_another_place";
    public const string YouReallyMustStayId = "you_really_must_stay";
    public const string CourtesyKeptId = "courtesy_kept";
    public const string HouseRuleNotedId = "house_rule_noted";
    public const string HospitalityOpenedId = "hospitality_opened";

    public const int CourtesyBreachLaw = 32;
    public const int HouseRuleLaw = 33;

    private const int MaxRememberedFavor = 3;
    private const int CourtesiesForStaying = 3;
    private const int GrandmotherTransitionHealth = 190;
    private const int GrandmotherSignatureHealth = 85;
    private const int CourtesiesForSignature = 6;

    public static CounterId HospitalityAcceptedCounter => new("hospitality_accepted");

    // The four courtesies: an accepted marker on the player, and a house rule the same condition can become.
    private static readonly (string Key, string Name, string Clause)[] Courtesies =
    [
        ("tea", "Warm Tea", "work with your hands at least once this turn"),
        ("chair", "The Better Chair", "end the turn with something real still in hand"),
        ("honey", "A Little Honey", "end the turn with at least 1 Energy"),
        ("slice", "Take Another Slice", "play no more than four real cards this turn"),
    ];

    private static string AcceptedId(string key) => $"courtesy_{key}";
    private static string HouseRuleId(string key) => $"house_rule_{key}";

    private static ICombatantTargetSelector Grandmother { get; } = Elite(GrandmotherId);

    private static IEnumerable<StatusData> GrandmotherStatuses() =>
    [
        TheCourtesies(),
        RememberedFavor(),
        Marker(HouseholdLawId, "Household Law",
            "You are staying the night. She may set two places, and what she remembers becomes rule."),
        Marker(StayLongerPendingId, "Stay a Little Longer",
            "Her next action is the invitation itself, and not a blow."),
        Marker(SetAnotherPlaceId, "A Place Set for You",
            "The next courtesy you accept is worth 2 more."),
        Marker(YouReallyMustStayId, "You Really Must Stay",
            "The next courtesy you break is worth 3 Trespass rather than 2. One licence still refuses all "
            + "of it."),
        Marker(CourtesyKeptId, "Courtesy Kept",
            "You have kept a promise made at this table this turn."),
        Marker(HouseRuleNotedId, "House Rule Noted",
            "The house rule has already been broken once this turn."),
        Marker(HospitalityOpenedId, "The Table Is Laid",
            "Grandmother has sat once. From the next turn she offers."),
        .. Courtesies.SelectMany(c => new[]
        {
            Marker(AcceptedId(c.Key), $"Accepted: {c.Name}",
                $"You took it, and promised to {c.Clause}."),
            HouseRule(c.Key, c.Name, c.Clause),
        }),
    ];

    private static StatusData RememberedFavor() => new()
    {
        Id = RememberedFavorId,
        NameKey = "Remembered Favour",
        DescriptionKey =
            "Something given that has not been given back. It is not standing and it is not a debt — it is "
            + "what she counts when she decides what the house rules are. At most 3.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData HouseRule(string key, string name, string clause) => new()
    {
        Id = HouseRuleId(key),
        NameKey = $"House Rule: {name}",
        DescriptionKey =
            $"Whether you took anything or not, you must {clause} — for this many more of your turns. "
            + "Breaking it is 2 Trespass, once a turn.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, bool> Accepted<TContext>(string key)
        where TContext : class =>
        Wears<TContext>(Applicant, AcceptedId(key));

    // ── the clauses ───────────────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: Warm Tea's clause is "play at least one Working this turn". The design adds a second half
    // about the order of a second Attack; the promise the player actually makes at the table is the first
    // half, and the second is unreadable at a glance — which is the one thing a courtesy must not be.
    private static ICombatExpression<TContext, bool> Kept<TContext>(string key)
        where TContext : class =>
        key switch
        {
            "tea" => new ComparisonExpression<TContext>(
                new CardsPlayedThisTurnWithTagExpression<TContext>(
                    Applicant, new TagId(Cards.CardAuthoring.WorkingTag)),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(1)),
            "chair" => new ComparisonExpression<TContext>(
                RealCardsLeftInHand<TContext>(),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(1)),
            "honey" => new ComparisonExpression<TContext>(
                new CombatantCurrentResourceExpression<TContext>(
                    Applicant, StandardCombatIds.EnergyResource),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(1)),
            _ => new ComparisonExpression<TContext>(
                RealCardsPlayed<TContext>(),
                ComparisonOperator.LessOrEqual, new ConstantExpression<TContext>(4)),
        };

    // ── the table ─────────────────────────────────────────────────────────────────────────────────────────
    private static StatusData TheCourtesies()
    {
        // The offering, at the bell. A courtesy is a gift and a promise together, and declining is free.
        IEffectNode<CardsDrawnTriggeredEffectContext> Take(string key) =>
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new NotExpression<CardsDrawnTriggeredEffectContext>(
                    Accepted<CardsDrawnTriggeredEffectContext>(key)),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(AcceptedId(key)),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    TheGift(key),
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Grandmother, HospitalityAcceptedCounter,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: true),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        Grandmother, new StatusDefinitionId(SetAnotherPlaceId)),
                ]));

        // Each place at the table is its own asking. Two places share no node, because a program is a tree
        // and the same branch offered twice is the same branch.
        ChooseOptionsNode<CardsDrawnTriggeredEffectContext> Offer(string purpose) =>
            new(
                [
                    new NoOpEffectNode<CardsDrawnTriggeredEffectContext>(),
                    .. Courtesies.Select(c => Take(c.Key)),
                ],
                [
                    "decline, politely",
                    .. Courtesies.Select(c => $"accept {c.Name} — and {c.Clause}"),
                ],
                count: 1, purpose: purpose);

        // ADAPTATION: the design has Grandmother choose which courtesy to offer, one in Phase I and two in
        // Phase II. An option list is a fixed list, so the whole table is laid out and the player takes what
        // they like — which keeps "only conditions that are achievable may be offered" true by construction,
        // and keeps DECLINING free, which is the rule the character is built on. In Household Law she sets a
        // second place, so the table is offered twice.
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(Grandmother),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<CardsDrawnTriggeredEffectContext>(Applicant),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Wears<CardsDrawnTriggeredEffectContext>(Grandmother, HospitalityOpenedId),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Offer("she offers"),
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            Wears<CardsDrawnTriggeredEffectContext>(Grandmother, HouseholdLawId),
                            Offer("and sets another place")),
                    ]),
                    // The table is being laid as the fight is handed over; she offers from the next turn.
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        Grandmother, new StatusDefinitionId(HospitalityOpenedId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))));

        // The reckoning, at the end of the player's turn: every promise made at this table is kept or broken.
        var reckoning = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(CourtesyKeptId)),
                    .. Courtesies.Select(c => Reckon(c.Key)),
                    // "If both are accepted and both fulfilled: the player gains 1 Safe-Conduct and
                    // Grandmother loses another 4 HP." Two promises kept at one table is a real evening.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(CourtesyKeptId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(SafeConductId),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                                sourceSelector: Grandmother),
                            LosesHeart(4),
                        ])),
                    .. Courtesies.Select(c => (IEffectNode<TurnEndedTriggeredEffectContext>)
                        BreakTheHouseRule(c.Key)),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Grandmother, new StatusDefinitionId(HouseRuleNotedId)),
                    // A rule runs down when the turn it governed is over, and not before it begins: a rule
                    // declared during her turn would otherwise be a turn short.
                    RunDownTheHouseRules<TurnEndedTriggeredEffectContext>(),
                    QueueStayALittleLonger<TurnEndedTriggeredEffectContext>(),
                ])));

        // "Whenever Grandmother gains a newly created Claim, she remembers a favour."
        EffectProgram<TContext> remember<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            Grandmother, new StatusDefinitionId(RememberedFavorId)),
                        ComparisonOperator.Less,
                        new ConstantExpression<TContext>(MaxRememberedFavor))),
                new ApplyStatusNode<TContext>(
                    Grandmother, new StatusDefinitionId(RememberedFavorId),
                    new ConstantExpression<TContext>(1))));

        return Rule(GrandmotherId, "The Courtesies",
            "She offers, and declining is never a violation. Each courtesy is a gift and a promise for that "
            + "turn; keep it and it costs her 5 HP, break it and it is 2 Trespass at once — which one "
            + "licence still refuses whole. Three courtesies accepted and you are staying the night; three "
            + "favours remembered and she declares a house rule that binds you whether you took anything or "
            + "not.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    reckoning, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    remember<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    remember<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // The gift itself, and the extra two where a place has been set for you.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> TheGift(string key)
    {
        var bonus = new MinExpression<CardsDrawnTriggeredEffectContext>(
            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2),
            new MultiplyExpression<CardsDrawnTriggeredEffectContext>(
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2),
                new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                    Grandmother, new StatusDefinitionId(SetAnotherPlaceId))));

        return key switch
        {
            "tea" => new HealNode<CardsDrawnTriggeredEffectContext>(
                Applicant,
                new AddExpression<CardsDrawnTriggeredEffectContext>(
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(6), bonus)),
            "chair" => new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                Applicant,
                new AddExpression<CardsDrawnTriggeredEffectContext>(
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(12), bonus)),
            // ADAPTATION: a purse has a hard ceiling here, and the honey is poured while it is still full —
            // so where it cannot hold another Energy the sweetness is Block instead. The promise is the same
            // either way, and that is the half of a courtesy that matters.
            "honey" => new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantMissingResourceExpression<CardsDrawnTriggeredEffectContext>(
                        Applicant, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                new GainResourceNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, StandardCombatIds.EnergyResource,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                    Applicant,
                    new AddExpression<CardsDrawnTriggeredEffectContext>(
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(5), bonus))),
            _ => new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                Applicant, new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
        };
    }

    // A promise kept costs her; a promise broken is two violations at once, or three if she has asked you
    // very sweetly to stay. Either way the courtesy is over.
    private static IEffectNode<TurnEndedTriggeredEffectContext> Reckon(string key) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Accepted<TurnEndedTriggeredEffectContext>(key),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Kept<TurnEndedTriggeredEffectContext>(key),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        LosesHeart(5),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(CourtesyKeptId),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    ]),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        Violate<TurnEndedTriggeredEffectContext>(
                            Grandmother, CourtesyBreachLaw,
                            stacks: new AddExpression<TurnEndedTriggeredEffectContext>(
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(2),
                                new MinExpression<TurnEndedTriggeredEffectContext>(
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                        Grandmother, new StatusDefinitionId(YouReallyMustStayId))))),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            Grandmother, new StatusDefinitionId(YouReallyMustStayId)),
                    ])),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(AcceptedId(key))),
            ]));

    // A house rule binds whether anything was taken or not — once a turn, whichever of them is standing.
    private static IEffectNode<TurnEndedTriggeredEffectContext> BreakTheHouseRule(string key) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                Wears<TurnEndedTriggeredEffectContext>(Grandmother, HouseRuleId(key)),
                new NotExpression<TurnEndedTriggeredEffectContext>(
                    Kept<TurnEndedTriggeredEffectContext>(key))),
            Violate<TurnEndedTriggeredEffectContext>(
                Grandmother, HouseRuleLaw, HouseRuleNotedId,
                stacks: new ConstantExpression<TurnEndedTriggeredEffectContext>(2)));

    private static IEffectNode<TContext> RunDownTheHouseRules<TContext>()
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
            [.. Courtesies.Select(c => (IEffectNode<TContext>)new ConditionalEffectNode<TContext>(
                Wears<TContext>(Grandmother, HouseRuleId(c.Key)),
                new ModifyStatusStacksNode<TContext>(
                    Grandmother, new StatusDefinitionId(HouseRuleId(c.Key)),
                    new ConstantExpression<TContext>(-1))))]);

    // Direct HP loss, which no Block and no reaction sees: she is not being fought, she is being repaid.
    private static IEffectNode<TurnEndedTriggeredEffectContext> LosesHeart(int amount) =>
        new SetHealthNode<TurnEndedTriggeredEffectContext>(
            Grandmother,
            new SubtractExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Grandmother),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(amount)));

    private static IEffectNode<TContext> QueueStayALittleLonger<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Wears<TContext>(Grandmother, HouseholdLawId)),
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(Grandmother, StayLongerPendingId)),
                    new OrExpression<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(
                                Grandmother, HospitalityAcceptedCounter),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TContext>(CourtesiesForStaying)),
                        new ComparisonExpression<TContext>(
                            new CombatantCurrentHealthExpression<TContext>(Grandmother),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TContext>(GrandmotherTransitionHealth))))),
            new ApplyStatusNode<TContext>(
                Grandmother, new StatusDefinitionId(StayLongerPendingId),
                new ConstantExpression<TContext>(1)));

    // ── A Debt Properly Settled ───────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> ADebtProperlySettled()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(GrandmotherId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(RememberedFavorId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                LosesHeart(6),
            ]));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? GrandmotherIntent(string enemyId, string intentId)
    {
        if (enemyId != GrandmotherEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        var favours = new CombatantStatusStacksExpression<EnemyActionContext>(
            self, new StatusDefinitionId(RememberedFavorId));
        var owed = new CombatantStatusStacksFromSourceExpression<EnemyActionContext>(
            Applicant, new StatusDefinitionId(WergildId), self);

        IEffectNode<EnemyActionContext>? ordinary = intentId switch
        {
            // Knitting-Needle Precedent: 8 twice, and 7 three times once you are staying.
            "knitting_needle_precedent" => Household(
                new CausalSequenceEffectNode<EnemyActionContext>([Blow(8), Blow(8)]),
                new CausalSequenceEffectNode<EnemyActionContext>([Blow(7), Blow(7), Blow(7)])),
            // Ask After Your Health → Close the Door Before Moonrise.
            "ask_after_your_health" => Household(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new HealNode<EnemyActionContext>(Applicant, new ConstantExpression<EnemyActionContext>(4)),
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(14)),
                ]),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(20),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(5),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(1), owed))))),
            // Call in a Small Favor → Call in What Was Given.
            "call_in_a_small_favor" => Household(
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        ConsumeClaim<EnemyActionContext>(self),
                        DemandWergild<EnemyActionContext>(self, 2),
                    ])),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new RepeatEffectNode<EnemyActionContext>(
                        new MinExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(2),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                self, new StatusDefinitionId(ClaimId))),
                        new CausalSequenceEffectNode<EnemyActionContext>(
                        [
                            ConsumeClaim<EnemyActionContext>(self),
                            DemandWergild<EnemyActionContext>(self, 1),
                        ])),
                    Blow(12),
                ])),
            // The Door Was Open for You → You Really Must Stay.
            "the_door_was_open_for_you" => Household(
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(16),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(MaxRememberedFavor), favours)))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(YouReallyMustStayId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ])),
            // Set Another Place → Settle at the Hearth.
            "set_another_place" => Household(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(16)),
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(SetAnotherPlaceId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ]),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    DemandWergild<EnemyActionContext>(self, 2),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(SafeConductId),
                        new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                ])),
            "you_accepted_the_hospitality" => new ConditionalEffectNode<EnemyActionContext>(
                new OrExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCurrentHealthExpression<EnemyActionContext>(self),
                        ComparisonOperator.LessOrEqual,
                        new ConstantExpression<EnemyActionContext>(GrandmotherSignatureHealth)),
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCounterExpression<EnemyActionContext>(self, HospitalityAcceptedCounter),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(CourtesiesForSignature))),
                YouAcceptedTheHospitality(favours, owed),
                Blow(16)),
            _ => null,
        };

        if (ordinary is null)
            return null;

        // "Because I Said So" is queued by three remembered favours and rides on whatever she was about to
        // do; the invitation to stay replaces it outright.
        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                Wears<EnemyActionContext>(self, StayLongerPendingId),
                StayALittleLonger(),
                new CausalSequenceEffectNode<EnemyActionContext>([ordinary, BecauseISaidSo()])));
    }

    private static IEffectNode<EnemyActionContext> Household(
        IEffectNode<EnemyActionContext> guest, IEffectNode<EnemyActionContext> household) =>
        new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(CombatantTargetSelectors.Source, HouseholdLawId),
            household, guest);

    // "Preserve Claims and Remembered Favour; the player gains 1 Safe-Conduct; no direct attack."
    private static IEffectNode<EnemyActionContext> StayALittleLonger()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(HouseholdLawId), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(StayLongerPendingId)),
        ]);
    }

    // "At 3 Remembered Favour, declare a House Rule for 2 player turns; then the favours are spent."
    // No gift accompanies it — that is the whole point of a house rule.
    private static IEffectNode<EnemyActionContext> BecauseISaidSo()
    {
        var self = CombatantTargetSelectors.Source;

        // The three House-Rule-safe conditions: the chair, the honey and the slice. Warm Tea is not among
        // them, because a rule that demands you play a particular kind of card can lock a hand out.
        var safe = new[] { "chair", "honey", "slice" };

        var steps = new List<IEffectNode<EnemyActionContext>>
        {
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(HouseRuleNotedId)),
        };
        foreach (var key in safe)
            steps.Add(new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    new NotExpression<EnemyActionContext>(
                        Wears<EnemyActionContext>(self, HouseRuleNotedId)),
                    new NotExpression<EnemyActionContext>(
                        Wears<EnemyActionContext>(self, HouseRuleId(key)))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(HouseRuleId(key)),
                        new ConstantExpression<EnemyActionContext>(2)),
                    new ApplyStatusNode<EnemyActionContext>(
                        self, new StatusDefinitionId(HouseRuleNotedId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ])));
        steps.Add(new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(HouseRuleNotedId)));
        steps.Add(new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(RememberedFavorId)));

        // Only one house rule at a time, and only once she has been given three things.
        return new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(RememberedFavorId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<EnemyActionContext>(MaxRememberedFavor)),
                new NotExpression<EnemyActionContext>(
                    safe.Select(k => (ICombatExpression<EnemyActionContext, bool>)
                            Wears<EnemyActionContext>(self, HouseRuleId(k)))
                        .Aggregate((a, b) => new OrExpression<EnemyActionContext>(a, b)))),
            new CausalSequenceEffectNode<EnemyActionContext>(steps));
    }

    // "18 +3 per Claim +2 per Remembered Favour +1 per open Wergild point, to a maximum of 34. Then the
    // Claims and the favours are gone, and the debt is not."
    private static IEffectNode<EnemyActionContext> YouAcceptedTheHospitality(
        ICombatExpression<EnemyActionContext, int> favours,
        ICombatExpression<EnemyActionContext, int> owed)
    {
        var self = CombatantTargetSelectors.Source;

        var claims = new CombatantStatusStacksExpression<EnemyActionContext>(
            self, new StatusDefinitionId(ClaimId));

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MinExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(34),
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(18),
                        new AddExpression<EnemyActionContext>(
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(3), claims),
                            new AddExpression<EnemyActionContext>(
                                new MultiplyExpression<EnemyActionContext>(
                                    new ConstantExpression<EnemyActionContext>(2), favours),
                                owed))))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(RememberedFavorId)),
        ]);
    }
}
