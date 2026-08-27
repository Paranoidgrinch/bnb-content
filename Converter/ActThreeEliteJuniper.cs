using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 6 — **Juniper Injunction** (188 HP).
//
// > Claims do not merely punish the player; they let the hedge temporarily forbid one legal remedy.
//
// A huge juniper has grown around an old path, with bone tags from earlier travellers hanging in it. It is
// the only body in the act that attacks the player's ANSWERS rather than the player: standing lets it
// enjoin a remedy — the licence, or one of the two ways of paying restitution — and the fight is about
// keeping a way out open.
//
// Two hard safety rules from the design are built as rules rather than trusted to numbers:
//   the two payment routes may never be enjoined at the same time (so a demand is always payable);
//   settling in full always frees one Injunction, so the player can always dig back out.
//
// ADAPTATIONS, both forced by an option list being a fixed list:
//   All four Granted Uses are offered each turn rather than two of them. The design's "no impossible
//   category may be offered" then holds trivially, because the player picks the one they can respect.
//   "Narrow the Granted Use" is what removes the choice: on the turn after it, the hedge grants leave for
//   Deeds and asks nothing — which is exactly "only one achievable Granted Use is offered".
public static partial class ActThree
{
    public const string JuniperEnemyId = "juniper_injunction";
    public const string JuniperId = "the_juniper_injunction";
    public const string GrantedUseId = "granted_use";
    public const string JuniperNotedId = "juniper_noted";
    public const string GrantedUseNarrowedId = "granted_use_narrowed";
    public const string GrantedUseAskedId = "granted_use_asked";
    public const string JuniperReliefGivenId = "juniper_relief_given";

    public const string InjunctionSafePassageId = "injunction_against_safe_passage";
    public const string InjunctionCoinId = "injunction_against_coin";
    public const string InjunctionOfferingId = "injunction_against_offering";

    public const int GrantedUseLaw = 19;
    private const int JuniperReliefHealth = 6;
    private const int MaxInjunctions = 2;

    // Which leave the player took this turn: 1 Deed, 2 Working, 3 Base Cost 0–1, 4 Base Cost 2 or more.
    // Kept on the player, the one combatant every part of the rule can address.
    public static CounterId GrantedUseCounter => new("granted_use");

    private static ICombatantTargetSelector Juniper { get; } = Elite(JuniperId);

    private static readonly string[] Injunctions =
        [InjunctionSafePassageId, InjunctionCoinId, InjunctionOfferingId];

    private static IEnumerable<StatusData> JuniperStatuses() =>
    [
        TheJuniperInjunction(),
        GrantedUseOffer(),
        Marker(JuniperNotedId, "Leave Noted",
            "The hedge has already answered one step off the granted path this turn."),
        Marker(GrantedUseNarrowedId, "Path Narrowed",
            "Next turn the hedge grants leave for Deeds and asks nothing: there is no choice to make."),
        Marker(GrantedUseAskedId, "Leave Asked",
            "The hedge has begun asking. Its first grant was made before you were on the path."),
        Marker(JuniperReliefGivenId, "Relief Granted",
            "One injunction has already lifted for this settlement."),
        Marker(InjunctionSafePassageId, "Enjoined: Safe Passage",
            "The hedge's next Trespass cannot be refused by a licence. Then the injunction lifts."),
        Marker(InjunctionCoinId, "Enjoined: Coin",
            "The Juniper's Wergild cannot be paid with Energy. Offerings remain legal."),
        Marker(InjunctionOfferingId, "Enjoined: Offering",
            "The Juniper's Wergild cannot be paid with a discarded card. Coin remains legal."),
    ];

    private static ICombatExpression<TContext, bool> Enjoined<TContext>(string injunction)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Juniper, new StatusDefinitionId(injunction)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // "The Juniper's Wergild cannot be paid this way." Read by the act's own payment card, and true only
    // while a living hedge is holding the injunction.
    public static ICombatExpression<TContext, bool> PaymentEnjoined<TContext>(string injunction)
        where TContext : class =>
        new AndExpression<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCurrentHealthExpression<TContext>(Juniper),
                ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
            Enjoined<TContext>(injunction));

    // ── Granted Use ───────────────────────────────────────────────────────────────────────────────────────
    //
    // The prompt lives on the PLAYER, because it is asked of the player's own turn and the player's own
    // hand — the same place Act II put every "at the start of your turn, choose" rule.
    private static StatusData GrantedUseOffer()
    {
        IEffectNode<CardsDrawnTriggeredEffectContext> Take(int use) =>
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                Applicant, GrantedUseCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(use), relative: false);

        var ask = new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
            [Take(1), Take(2), Take(3), Take(4)],
            [
                "leave to act: Deeds",
                "leave to act: Workings",
                "leave to act: cards costing 0 or 1",
                "leave to act: cards costing 2 or more",
            ],
            count: 1, purpose: "the hedge grants one use of the path");

        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(Juniper),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Enjoined<CardsDrawnTriggeredEffectContext>(GrantedUseNarrowedId),
                    // Narrowed: there is one achievable use and no choice about it.
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Take(1),
                        new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                            Juniper, new StatusDefinitionId(GrantedUseNarrowedId)),
                    ]),
                    // ADAPTATION: the FIRST grant is made rather than asked. The opening hand is dealt as
                    // the fight is being handed over, before there is anybody standing on the path to put
                    // the question to — so the hedge grants leave for Deeds and starts asking next turn.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Enjoined<CardsDrawnTriggeredEffectContext>(GrantedUseAskedId),
                        ask,
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Take(1),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                Juniper, new StatusDefinitionId(GrantedUseAskedId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])))));

        return Rule(GrantedUseId, "Granted Use",
            "Each turn the juniper grants you leave to act one way — Deeds, Workings, cheap cards or dear "
            + "ones. The first real card you play outside it is 1 Trespass owed to the hedge.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
            ]);
    }

    // ── the hedge itself ──────────────────────────────────────────────────────────────────────────────────
    private static StatusData TheJuniperInjunction()
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Chose(int use) =>
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                    Applicant, GrantedUseCounter),
                ComparisonOperator.Equal,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(use));

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> IsA(string tag) =>
            new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag));

        ICombatExpression<CardPlayedTriggeredEffectContext, int> Cost() =>
            new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                StandardCombatIds.EnergyResource);

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Outside(
            int use, ICombatExpression<CardPlayedTriggeredEffectContext, bool> within) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Chose(use), new NotExpression<CardPlayedTriggeredEffectContext>(within));

        var offThePath = new OrExpression<CardPlayedTriggeredEffectContext>(
            Outside(1, IsA(Cards.CardAuthoring.DeedTag)),
            new OrExpression<CardPlayedTriggeredEffectContext>(
                Outside(2, IsA(Cards.CardAuthoring.WorkingTag)),
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    Outside(3, new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        Cost(), ComparisonOperator.LessOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                    Outside(4, new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        Cost(), ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(2))))));

        // Under an injunction against safe passage the violation names the licence that may not be spent on
        // it — and the injunction lifts once it has done that once.
        var law = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        IsA(Cards.CardAuthoring.JunkTag)),
                    offThePath),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    Enjoined<CardPlayedTriggeredEffectContext>(InjunctionSafePassageId),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        Violate<CardPlayedTriggeredEffectContext>(
                            Juniper, GrantedUseLaw, JuniperNotedId, unrefusableBy: SafeConductId),
                        new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                            Juniper, new StatusDefinitionId(InjunctionSafePassageId)),
                    ]),
                    Violate<CardPlayedTriggeredEffectContext>(Juniper, GrantedUseLaw, JuniperNotedId))));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Juniper, new StatusDefinitionId(JuniperNotedId))));

        EffectProgram<TContext> enjoin<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                PrepareAnInjunction<TContext>()));

        return Rule(JuniperId, "The Juniper Injunction",
            "Step off the leave the hedge granted you and you owe it 1 Trespass, once a turn. Every Claim it "
            + "is granted prepares an INJUNCTION against one of your remedies — the licence, coin, or an "
            + "offering — and it may hold two at once, but never both ways of paying. Settle its demand in "
            + "full and one injunction lifts, a Claim is struck off and the hedge loses 6 HP.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    law, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    enjoin<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    enjoin<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // ── preparing an injunction ───────────────────────────────────────────────────────────────────────────
    //
    // Safe passage first, because it is the one that expires by itself. After that a payment route — and
    // only ever ONE of the two, whichever way round they are asked for, which is the design's hard rule
    // that a demand must always remain payable.
    private static IEffectNode<TContext> PrepareAnInjunction<TContext>()
        where TContext : class
    {
        var active = Injunctions
            .Select(i => (ICombatExpression<TContext, int>)new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantStatusStacksExpression<TContext>(Juniper, new StatusDefinitionId(i))))
            .Aggregate((a, b) => new AddExpression<TContext>(a, b));

        var room = new ComparisonExpression<TContext>(
            active, ComparisonOperator.Less, new ConstantExpression<TContext>(MaxInjunctions));

        var eitherRouteEnjoined = new OrExpression<TContext>(
            Enjoined<TContext>(InjunctionCoinId), Enjoined<TContext>(InjunctionOfferingId));

        return new ConditionalEffectNode<TContext>(
            room,
            new ConditionalEffectNode<TContext>(
                new NotExpression<TContext>(Enjoined<TContext>(InjunctionSafePassageId)),
                new ApplyStatusNode<TContext>(
                    Juniper, new StatusDefinitionId(InjunctionSafePassageId),
                    new ConstantExpression<TContext>(1)),
                new ConditionalEffectNode<TContext>(
                    new NotExpression<TContext>(eitherRouteEnjoined),
                    new ApplyStatusNode<TContext>(
                        Juniper, new StatusDefinitionId(InjunctionCoinId),
                        new ConstantExpression<TContext>(1)))));
    }

    // ── Petition for Relief ───────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: the design lets the PLAYER choose which injunction to lift. A settlement resolves as a
    // turn ends, where there is nobody to ask, so the first one in order lifts — and safe passage is first,
    // which is the one worth having back.
    private static IEffectNode<TurnEndedTriggeredEffectContext> PetitionForRelief()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        var steps = new List<IEffectNode<TurnEndedTriggeredEffectContext>>();
        foreach (var injunction in Injunctions)
        {
            steps.Add(new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    Enjoined<TurnEndedTriggeredEffectContext>(injunction),
                    new NotExpression<TurnEndedTriggeredEffectContext>(
                        Enjoined<TurnEndedTriggeredEffectContext>(JuniperReliefGivenId))),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        creditor, new StatusDefinitionId(injunction)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        creditor, new StatusDefinitionId(JuniperReliefGivenId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                ])));
        }

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(JuniperId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                // Its own cell, cleared on both sides of the lifting: the law's per-turn latch is a
                // different fact, and two rules sharing one memory would race.
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(JuniperReliefGivenId)),
                .. steps,
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(JuniperReliefGivenId)),
                new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    creditor,
                    new StatusSelectionSpec(StatusPolarityFilter.Any)
                    {
                        Definition = new StatusDefinitionId(ClaimId),
                    },
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                new SetHealthNode<TurnEndedTriggeredEffectContext>(
                    creditor,
                    new SubtractExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(creditor),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(JuniperReliefHealth))),
            ]));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? JuniperIntent(string enemyId, string intentId)
    {
        if (enemyId != JuniperEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        var activeInjunctions = Injunctions
            .Select(i => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(i))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            "narrow_the_granted_use" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(GrantedUseNarrowedId),
                    new ConstantExpression<EnemyActionContext>(1)),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(14)),
            ]),
            "bind_the_path" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Blow(17),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        activeInjunctions, ComparisonOperator.Greater,
                        new ConstantExpression<EnemyActionContext>(0)),
                    DemandWergild<EnemyActionContext>(self, 1)),
            ]),
            "thorned_enforcement" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                    ConsumeClaim<EnemyActionContext>(self)),
                Blow(20),
                PrepareAnInjunction<EnemyActionContext>(),
            ]),
            "demand_relief_in_proper_form" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                DemandWergild<EnemyActionContext>(self, 2),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
            ]),
            // "15 damage; with 2 active Injunctions +7, max 22."
            "no_remedy_is_absolute" => new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(15),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(7),
                        new DivideExpression<EnemyActionContext>(
                            activeInjunctions, new ConstantExpression<EnemyActionContext>(2))))),
            "the_final_injunction" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(ClaimCeiling)),
                TheFinalInjunction(), Blow(15)),
            _ => null,
        };

        return act is null ? null : new EffectProgram<EnemyActionContext>(act);
    }

    // "Consume all 3 Claims; prepare one Against Safe Passage and one payment-route injunction that cannot
    // hardlock payment; create Wergild 3; gain 16 Block. No direct damage."
    private static IEffectNode<EnemyActionContext> TheFinalInjunction()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new RepeatEffectNode<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(ClaimCeiling),
                ConsumeClaim<EnemyActionContext>(self)),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(InjunctionSafePassageId),
                new ConstantExpression<EnemyActionContext>(1)),
            // The second one still obeys the hard rule: never both ways of paying.
            new ConditionalEffectNode<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(
                    new OrExpression<EnemyActionContext>(
                        Enjoined<EnemyActionContext>(InjunctionCoinId),
                        Enjoined<EnemyActionContext>(InjunctionOfferingId))),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(InjunctionOfferingId),
                    new ConstantExpression<EnemyActionContext>(1))),
            DemandWergild<EnemyActionContext>(self, 3),
            new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(16)),
        ]);
    }
}
