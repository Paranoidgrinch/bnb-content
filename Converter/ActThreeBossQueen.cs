using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 5 — **The Queen Under the Hill** (392 HP). The act's complete examination.
//
// > The player can deliberately enter a cycle of gift, Claim, Wergild and Favor, and then use that legal
// > reciprocity against sovereign authority.
//
// The Queen wears no metal crown; the roots above her head form one. Her court holds white stones, dark
// cloth, old bones, cups, names, silver, and things that were once promised. She does not threaten anybody.
// Everything under the hill already belongs to some right.
//
// The whole boss is one cycle, and the player chooses how far into it to go:
//
//   **Gift → Claim → Wergild → Favor → Right of Audience**
//
// Royal Grace is a real gift and it is always optional; accepting one hands the Queen standing. Standing
// becomes a demand, a demand settled in full becomes FAVOUR, and favour is the only currency that buys an
// audience: one removes a Claim, two suspend her law for the turn, three strike her guard away and prepare
// the GRANTED NAME, which takes eight off her final order.
//
// Declining everything is legal to the end. It is simply a longer road.
public static partial class ActThree
{
    public const string QueenEnemyId = "queen_under_the_hill";
    public const string QueenId = "the_hill_court";

    public const string FavorId = "favor";
    public const string GrantedNamePreparedId = "granted_name_prepared";
    public const string CourtInSessionId = "court_in_session";
    public const string SovereignReciprocityId = "sovereign_reciprocity";
    public const string CourtSessionPendingId = "the_court_is_now_in_session";
    public const string GrantedNamePendingId = "the_granted_name_is_spoken";
    public const string NoFurtherGiftId = "no_further_gift";
    public const string AudienceUsedId = "audience_taken";
    public const string RoyalLawSuspendedId = "reciprocity_invoked";
    public const string QueenAddressedId = "queen_addressed";
    public const string CourtOpenedId = "court_opened";
    public const string ReciprocityPaidId = "reciprocity_paid";
    public const string RightOfAudienceCardId = "right_of_audience";

    public const int DoNotSpeakLaw = 36;

    private const int MaxFavor = 3;
    private const int FavorForSession = 2;
    private const int QueenSessionHealth = 255;
    private const int QueenNameHealth = 130;
    private const int QueenSignatureHealth = 60;

    public static readonly TagId AudienceTag = new("right_of_audience");

    public static CounterId TotalFavorEarnedCounter => new("total_favor_earned");

    private static readonly (string Key, string Name)[] Graces =
    [
        ("passage", "Grace of Passage"),
        ("plenty", "Grace of Plenty"),
        ("shelter", "Grace of Shelter"),
        ("recall", "Grace of Recall"),
    ];

    private static ICombatantTargetSelector HillQueen { get; } = Elite(QueenId);

    private static IEnumerable<StatusData> HillQueenStatuses() =>
    [
        TheHillCourt(),
        Favor(),
        Marker(GrantedNamePreparedId, "Granted Name Prepared",
            "Her guard is gone and her name is in your mouth. Her next final order is 8 lighter."),
        Marker(CourtInSessionId, "Court in Session",
            "The court is sitting. Standing she holds while you hold no favour hardens her."),
        Marker(SovereignReciprocityId, "Sovereign Reciprocity",
            "The legal economy cuts both ways now: favour spent costs her, and standing spent guards you."),
        Marker(CourtSessionPendingId, "The Court Is Now in Session",
            "Her next action is the convening of the court, and not a blow."),
        Marker(GrantedNamePendingId, "The Granted Name Is Spoken",
            "Her next action is the hearing of the name, and not a blow."),
        Marker(NoFurtherGiftId, "No Further Gift",
            "The next demand you settle in full still earns favour — and no licence with it."),
        Marker(AudienceUsedId, "Audience Taken",
            "You have already been heard this turn."),
        Marker(RoyalLawSuspendedId, "Reciprocity Invoked",
            "Her law is suspended for the rest of this turn."),
        Marker(QueenAddressedId, "Addressed",
            "The court has already answered one word spoken out of turn."),
        Marker(CourtOpenedId, "The Court Is Open",
            "The Queen has sat once. From the next turn she offers grace."),
        Marker(ReciprocityPaidId, "Reciprocity Answered",
            "The court has already given back for favour spent this turn."),
    ];

    private static StatusData Favor() => new()
    {
        Id = FavorId,
        NameKey = "Favour",
        DescriptionKey =
            "What the court owes YOU: earned by settling a royal demand in full. Spend it on an audience — "
            + "one to strike a Claim, two to suspend her law for a turn, three to speak the granted name. "
            + "At most 3.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, int> HillQueenClaims<TContext>()
        where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(HillQueen, new StatusDefinitionId(ClaimId));

    private static ICombatExpression<TContext, int> HeldFavor<TContext>()
        where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(Applicant, new StatusDefinitionId(FavorId));

    // ── the court ─────────────────────────────────────────────────────────────────────────────────────────
    private static StatusData TheHillCourt()
    {
        // "Do Not Speak Before Addressed": the turn's first real card.
        //
        // ADAPTATION: the design gates the law on the player holding no licence. Written that way it could
        // never fire here — the act opens every Green Docket fight with one licence, the Queen's own graces
        // hand out more, and in a solo court nothing else spends them, so the condition is never met. Filed
        // unconditionally it behaves exactly as the design describes: leave to speak is what a licence IS,
        // so while you hold one the court takes it and says nothing, and the first word you say without one
        // is the violation.
        var law = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        Wears<CardPlayedTriggeredEffectContext>(Applicant, RoyalLawSuspendedId)),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(CombatantTargetSelectors.Source),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                Violate<CardPlayedTriggeredEffectContext>(HillQueen, DoNotSpeakLaw, QueenAddressedId)));

        // The bell: the latches clear, grace is offered while there is room for standing, and the audience
        // is offered while there is favour to spend at it.
        var bell = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(HillQueen),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<CardsDrawnTriggeredEffectContext>(Applicant),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(AudienceUsedId)),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(RoyalLawSuspendedId)),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(ReciprocityPaidId)),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        HillQueen, new StatusDefinitionId(QueenAddressedId)),
                    OfferAnAudience<CardsDrawnTriggeredEffectContext>(),
                    // ADAPTATION: the design shows 2 of 4 graces; an option list is a fixed list, so the
                    // whole court is laid out and the player takes at most one, which is the rule that
                    // matters. And the first bell is the court settling: she offers from the next turn.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Wears<CardsDrawnTriggeredEffectContext>(HillQueen, CourtOpenedId),
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                HillQueenClaims<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.Less,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(ClaimCeiling)),
                            RoyalGrace()),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            HillQueen, new StatusDefinitionId(CourtOpenedId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                ])));

        var thresholds = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                QueueTheCourt<TurnStartedTriggeredEffectContext>()));

        // "Court Standing": at the start of her own turn, standing she holds while you hold no favour is
        // worth guarding.
        var standing = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    new NotExpression<TurnStartedTriggeredEffectContext>(
                        PlayersTurn<TurnStartedTriggeredEffectContext>()),
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        Wears<TurnStartedTriggeredEffectContext>(HillQueen, CourtInSessionId),
                        new AndExpression<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                HillQueenClaims<TurnStartedTriggeredEffectContext>(),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                HeldFavor<TurnStartedTriggeredEffectContext>(),
                                ComparisonOperator.Equal,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0))))),
                new GainBlockNode<TurnStartedTriggeredEffectContext>(
                    HillQueen, new ConstantExpression<TurnStartedTriggeredEffectContext>(8))));

        return Rule(QueenId, "The Hill Court",
            "The first word you say each turn needs leave: a licence answers for it, and without one you owe "
            + "the Queen 1 Trespass. Everything else "
            + "is a cycle you may enter or refuse: ROYAL GRACE is a real gift that hands her standing, "
            + "standing becomes a demand, a demand settled in full earns you FAVOUR, and favour buys an "
            + "AUDIENCE — one to strike a Claim off, two to suspend her law, three to speak the granted "
            + "name. Declining everything is legal to the end.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    law, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    thresholds, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    standing, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // ── Royal Grace ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Every gift is real, and every one of them hands her a Claim she did not have. Declining costs nothing.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> RoyalGrace()
    {
        IEffectNode<CardsDrawnTriggeredEffectContext> Accept(
            IEffectNode<CardsDrawnTriggeredEffectContext> gift) =>
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                gift,
                CreateClaim<CardsDrawnTriggeredEffectContext>(HillQueen),
            ]);

        return new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
            [
                new NoOpEffectNode<CardsDrawnTriggeredEffectContext>(),
                Accept(new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), sourceSelector: HillQueen)),
                Accept(new GainResourceNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, StandardCombatIds.EnergyResource,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                Accept(new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, new ConstantExpression<CardsDrawnTriggeredEffectContext>(12))),
                Accept(new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, new ConstantExpression<CardsDrawnTriggeredEffectContext>(2))),
            ],
            [
                "decline the court's grace",
                "Grace of Passage — a licence, and she gains standing",
                "Grace of Plenty — an Energy, and she gains standing",
                "Grace of Shelter — 12 Block, and she gains standing",
                "Grace of Recall — two cards, and she gains standing",
            ],
            count: 1, purpose: "the court offers");
    }

    // ── Right of Audience ─────────────────────────────────────────────────────────────────────────────────
    public static CardData RightOfAudience() => new()
    {
        Id = RightOfAudienceCardId,
        NameKey = "Right of Audience",
        DescriptionKey =
            "Once a turn, spend Favour. ONE — strike one of the Queen's Claims off. TWO — her law is "
            + "suspended for the rest of this turn. THREE — her guard is struck away and her granted name "
            + "is prepared, which takes 8 off her final order.",
        Costs = [],
        Tags = [AudienceTag, new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new NotExpression<CardPlayContext>(
                            Wears<CardPlayContext>(Applicant, AudienceUsedId)),
                        new ComparisonExpression<CardPlayContext>(
                            HeldFavor<CardPlayContext>(), ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardPlayContext>(1))),
                    new ChooseOptionsNode<CardPlayContext>(
                        [AskForRedress(), InvokeReciprocity(), SpeakTheGrantedName()],
                        [
                            "ask for redress — 1 Favour, and a Claim is struck off",
                            "invoke reciprocity — 2 Favour, and her law is suspended this turn",
                            "speak the granted name — 3 Favour, and her guard and her order are lighter",
                        ],
                        count: 1, purpose: "how you would be heard")),
                AnotherAudience(),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    // Favour spent is favour the court answers for, once a turn, once she is reciprocating.
    private static IEffectNode<CardPlayContext> SpendFavor(int favour, IEffectNode<CardPlayContext> heard) =>
        new ConditionalEffectNode<CardPlayContext>(
            new ComparisonExpression<CardPlayContext>(
                HeldFavor<CardPlayContext>(), ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayContext>(favour)),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ModifyStatusStacksNode<CardPlayContext>(
                    Applicant, new StatusDefinitionId(FavorId),
                    new ConstantExpression<CardPlayContext>(-favour)),
                new ApplyStatusNode<CardPlayContext>(
                    Applicant, new StatusDefinitionId(AudienceUsedId),
                    new ConstantExpression<CardPlayContext>(1)),
                heard,
                RoyalReciprocity(),
            ]));

    private static IEffectNode<CardPlayContext> AskForRedress() =>
        SpendFavor(1,
            new ModifySelectedStatusStacksNode<CardPlayContext>(
                HillQueen,
                new StatusSelectionSpec(StatusPolarityFilter.Any) { Definition = new StatusDefinitionId(ClaimId) },
                new ConstantExpression<CardPlayContext>(-1)));

    private static IEffectNode<CardPlayContext> InvokeReciprocity() =>
        SpendFavor(2,
            new ApplyStatusNode<CardPlayContext>(
                Applicant, new StatusDefinitionId(RoyalLawSuspendedId),
                new ConstantExpression<CardPlayContext>(1)));

    private static IEffectNode<CardPlayContext> SpeakTheGrantedName() =>
        SpendFavor(3,
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ModifyDefensivePoolNode<CardPlayContext>(
                    HillQueen, StandardCombatIds.BlockDefensivePool,
                    new NegateExpression<CardPlayContext>(
                        new CombatantDefensivePoolExpression<CardPlayContext>(
                            HillQueen, StandardCombatIds.BlockDefensivePool))),
                new ApplyStatusNode<CardPlayContext>(
                    HillQueen, new StatusDefinitionId(GrantedNamePreparedId),
                    new ConstantExpression<CardPlayContext>(1)),
                // Speaking the name is the second transition, whatever her health says.
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new NotExpression<CardPlayContext>(
                            Wears<CardPlayContext>(HillQueen, SovereignReciprocityId)),
                        new NotExpression<CardPlayContext>(
                            Wears<CardPlayContext>(HillQueen, GrantedNamePendingId))),
                    new ApplyStatusNode<CardPlayContext>(
                        HillQueen, new StatusDefinitionId(GrantedNamePendingId),
                        new ConstantExpression<CardPlayContext>(1))),
            ]));

    // "Whenever the player spends Favour, the Queen loses 6 HP. Once per player turn." Direct loss.
    private static IEffectNode<CardPlayContext> RoyalReciprocity() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                Wears<CardPlayContext>(HillQueen, SovereignReciprocityId),
                new NotExpression<CardPlayContext>(Wears<CardPlayContext>(Applicant, ReciprocityPaidId))),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new SetHealthNode<CardPlayContext>(
                    HillQueen,
                    new SubtractExpression<CardPlayContext>(
                        new CombatantCurrentHealthExpression<CardPlayContext>(HillQueen),
                        new ConstantExpression<CardPlayContext>(6))),
                new ApplyStatusNode<CardPlayContext>(
                    Applicant, new StatusDefinitionId(ReciprocityPaidId),
                    new ConstantExpression<CardPlayContext>(1)),
            ]));

    private static IEffectNode<TContext> OfferAnAudience<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    HeldFavor<TContext>(), ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand, AudienceTag),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
            new CreateCardInstanceNode<TContext>(
                Applicant, new CardDefinitionId(RightOfAudienceCardId), CardZone.Hand,
                new ConstantExpression<TContext>(1)));

    private static IEffectNode<CardPlayContext> AnotherAudience() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    HeldFavor<CardPlayContext>(), ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0)),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantZoneCardCountExpression<CardPlayContext>(
                        Applicant, CardZone.Hand, AudienceTag),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<CardPlayContext>(1))),
            new CreateCardInstanceNode<CardPlayContext>(
                Applicant, new CardDefinitionId(RightOfAudienceCardId), CardZone.Hand,
                new ConstantExpression<CardPlayContext>(1)));

    // ── Favour, earned ────────────────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> TheCourtRemembersPayment()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(QueenId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        HeldFavor<TurnEndedTriggeredEffectContext>(), ComparisonOperator.Less,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(MaxFavor)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(FavorId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    creditor, TotalFavorEarnedCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                // "No Further Gift": the favour is still earned, and the licence is not. The act's own
                // settlement has already granted it, so it is taken back here.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Wears<TurnEndedTriggeredEffectContext>(creditor, NoFurtherGiftId),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
                            Applicant,
                            new StatusSelectionSpec(StatusPolarityFilter.Any)
                            {
                                Definition = new StatusDefinitionId(SafeConductId),
                            },
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(NoFurtherGiftId)),
                    ])),
                OfferAnAudience<TurnEndedTriggeredEffectContext>(),
            ]));
    }

    // ── the two transitions ───────────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TContext> QueueTheCourt<TContext>()
        where TContext : class
    {
        ICombatExpression<TContext, bool> NothingPending() =>
            new NotExpression<TContext>(
                new OrExpression<TContext>(
                    Wears<TContext>(HillQueen, CourtSessionPendingId),
                    Wears<TContext>(HillQueen, GrantedNamePendingId)));

        return new CausalSequenceEffectNode<TContext>(
        [
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(HillQueen, CourtInSessionId)),
                    new AndExpression<TContext>(
                        NothingPending(),
                        new OrExpression<TContext>(
                            new ComparisonExpression<TContext>(
                                new CombatantCounterExpression<TContext>(HillQueen, TotalFavorEarnedCounter),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TContext>(FavorForSession)),
                            new ComparisonExpression<TContext>(
                                new CombatantCurrentHealthExpression<TContext>(HillQueen),
                                ComparisonOperator.LessOrEqual,
                                new ConstantExpression<TContext>(QueenSessionHealth))))),
                new ApplyStatusNode<TContext>(
                    HillQueen, new StatusDefinitionId(CourtSessionPendingId),
                    new ConstantExpression<TContext>(1))),
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    Wears<TContext>(HillQueen, CourtInSessionId),
                    new AndExpression<TContext>(
                        new NotExpression<TContext>(Wears<TContext>(HillQueen, SovereignReciprocityId)),
                        new AndExpression<TContext>(
                            NothingPending(),
                            new ComparisonExpression<TContext>(
                                new CombatantCurrentHealthExpression<TContext>(HillQueen),
                                ComparisonOperator.LessOrEqual,
                                new ConstantExpression<TContext>(QueenNameHealth))))),
                new ApplyStatusNode<TContext>(
                    HillQueen, new StatusDefinitionId(GrantedNamePendingId),
                    new ConstantExpression<TContext>(1))),
        ]);
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? HillQueenIntent(string enemyId, string intentId)
    {
        if (enemyId != QueenEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;
        var claims = HillQueenClaims<EnemyActionContext>();
        var favour = HeldFavor<EnemyActionContext>();
        var owed = new CombatantStatusStacksFromSourceExpression<EnemyActionContext>(
            Applicant, new StatusDefinitionId(WergildId), self);

        IEffectNode<EnemyActionContext>? ordinary = intentId switch
        {
            "open_the_hill_registry" => Courts(
                Blow(16),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(20),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(5),
                            new SubtractExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(1),
                                new MinExpression<EnemyActionContext>(
                                    new ConstantExpression<EnemyActionContext>(1), favour))))),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new MinExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(25),
                        new AddExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(16),
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(3), favour))))),
            "call_in_the_gift" => Courts(
                CashClaims(1, 2, damage: 0),
                CashClaims(2, 1, damage: 12),
                CashClaims(1, 2, damage: 0)),
            "royal_subterranean_correction" => Courts(
                Correction(14, 19), Correction(14, 19), Correction(18, 23)),
            "count_every_buried_name" => Courts(
                Guard(14, 4, 26), Guard(16, 4, 28), Guard(16, 5, 31)),
            "a_gift_in_return" => Courts(AGiftInReturn(18), AGiftInReturn(18), AGiftInReturn(21)),
            "no_further_gift" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(NoFurtherGiftId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ]),
            "hill_court_final_order" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCurrentHealthExpression<EnemyActionContext>(self),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<EnemyActionContext>(QueenSignatureHealth)),
                HillCourtFinalOrder(claims, owed),
                Blow(18)),
            _ => null,
        };

        if (ordinary is null)
            return null;

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                Wears<EnemyActionContext>(self, GrantedNamePendingId),
                TheGrantedNameIsSpoken(),
                new ConditionalEffectNode<EnemyActionContext>(
                    Wears<EnemyActionContext>(self, CourtSessionPendingId),
                    TheCourtIsNowInSession(),
                    ordinary)));
    }

    private static IEffectNode<EnemyActionContext> Courts(
        IEffectNode<EnemyActionContext> audience,
        IEffectNode<EnemyActionContext> session,
        IEffectNode<EnemyActionContext> sovereign) =>
        new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(CombatantTargetSelectors.Source, SovereignReciprocityId),
            sovereign,
            new ConditionalEffectNode<EnemyActionContext>(
                Wears<EnemyActionContext>(CombatantTargetSelectors.Source, CourtInSessionId),
                session, audience));

    // Standing cashed for a demand — and once she is reciprocating, standing she spends guards YOU.
    private static IEffectNode<EnemyActionContext> CashClaims(int count, int points, int damage)
    {
        var self = CombatantTargetSelectors.Source;

        var steps = new List<IEffectNode<EnemyActionContext>>
        {
            new RepeatEffectNode<EnemyActionContext>(
                new MinExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(count), HillQueenClaims<EnemyActionContext>()),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ConsumeClaim<EnemyActionContext>(self),
                    DemandWergild<EnemyActionContext>(self, points),
                ])),
        };
        if (damage > 0)
            steps.Add(Blow(damage));

        // "Whenever the Queen consumes at least one Claim during an action, the player gains 4 Block."
        steps.Add(new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                Wears<EnemyActionContext>(self, SovereignReciprocityId),
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(ClaimConsumedId)),
                    ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0))),
            new GainBlockNode<EnemyActionContext>(
                Applicant, new ConstantExpression<EnemyActionContext>(4))));

        return new CausalSequenceEffectNode<EnemyActionContext>(steps);
    }

    private static IEffectNode<EnemyActionContext> Correction(int plain, int withStanding) =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    HillQueenClaims<EnemyActionContext>(), ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<EnemyActionContext>(2)),
                Blow(withStanding), Blow(plain)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId("doubt"), new ConstantExpression<EnemyActionContext>(1)),
        ]);

    private static IEffectNode<EnemyActionContext> Guard(int flat, int perClaim, int ceiling) =>
        new GainBlockNode<EnemyActionContext>(
            CombatantTargetSelectors.Source,
            new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(ceiling),
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(flat),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(perClaim),
                        HillQueenClaims<EnemyActionContext>()))));

    private static IEffectNode<EnemyActionContext> AGiftInReturn(int refused)
    {
        var self = CombatantTargetSelectors.Source;

        return new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                HillQueenClaims<EnemyActionContext>(), ComparisonOperator.Less,
                new ConstantExpression<EnemyActionContext>(ClaimCeiling)),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                CreateClaim<EnemyActionContext>(self),
            ]),
            Blow(refused));
    }

    // "Preserve everything; the player gains a licence; the Queen guards for 14; no direct attack."
    private static IEffectNode<EnemyActionContext> TheCourtIsNowInSession()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(14)),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(CourtInSessionId), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(CourtSessionPendingId)),
        ]);
    }

    // "Preserve Claims, Favour and open Wergild; remove her Block; the player gains 1 Favour; no direct
    // attack." A name already prepared stays prepared.
    private static IEffectNode<EnemyActionContext> TheGrantedNameIsSpoken()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ModifyDefensivePoolNode<EnemyActionContext>(
                self, StandardCombatIds.BlockDefensivePool,
                new NegateExpression<EnemyActionContext>(
                    new CombatantDefensivePoolExpression<EnemyActionContext>(
                        self, StandardCombatIds.BlockDefensivePool))),
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    HeldFavor<EnemyActionContext>(), ComparisonOperator.Less,
                    new ConstantExpression<EnemyActionContext>(MaxFavor)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(FavorId),
                    new ConstantExpression<EnemyActionContext>(1))),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(SovereignReciprocityId),
                new ConstantExpression<EnemyActionContext>(1)),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(CourtInSessionId), new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(GrantedNamePendingId)),
        ]);
    }

    // "22 +4 per Claim +2 per open Wergild point, to a maximum of 34; 8 less if her granted name has been
    // spoken. Then every Claim is struck off."
    private static IEffectNode<EnemyActionContext> HillCourtFinalOrder(
        ICombatExpression<EnemyActionContext, int> claims,
        ICombatExpression<EnemyActionContext, int> owed)
    {
        var self = CombatantTargetSelectors.Source;

        var order = new MinExpression<EnemyActionContext>(
            new ConstantExpression<EnemyActionContext>(34),
            new AddExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(22),
                new AddExpression<EnemyActionContext>(
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(4), claims),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(2), owed))));

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MaxExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(0),
                    new SubtractExpression<EnemyActionContext>(
                        order,
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(8),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(1),
                                new CombatantStatusStacksExpression<EnemyActionContext>(
                                    self, new StatusDefinitionId(GrantedNamePreparedId))))))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(GrantedNamePreparedId)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
        ]);
    }
}
