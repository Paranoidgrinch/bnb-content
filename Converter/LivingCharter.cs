using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's fifth boss: "Combat rules are law. Law can be reviewed, amended and interpreted."
//
// The Charter publishes an Article — a status on itself that rewrites how the fight works for BOTH sides. Every
// second action it calls a Judicial Review and hands the player the choice to uphold the standing Article or
// strike it down for the next one. (Until 2026-11-01 two Reviews brought an Emergency Amendment with a second
// Article and a second phase, and 34 HP a Crisis of Exception tokens; user: every rule in one clear sentence.)
//
// The Articles are authored as encounter triggers gated on the Charter still carrying the Article, so a struck
// Article stops working the moment it is removed. Deviations: ADAPTATIONS.md.
public static class LivingCharter
{
    public const string CharterId = "the_living_charter";
    public const string ContinuanceId = "article_of_continuance";
    public const string RedressId = "article_of_redress";
    public const string FullDisclosureId = "article_of_full_disclosure";
    public const string DisclosedId = "under_full_disclosure";   // the Article's mirror on the player

    public const string ReviewPendingId = "judicial_review_called";   // on the player: the choice is open
    public const string RemedyId = "remedy_due";                      // Redress, per side
    public const string ExceptionClaimedId = "exception_claimed";     // spent by the player for this turn

    public const string UpholdCardId = "uphold_the_article";
    public const string StrikeDownCardId = "strike_down_the_article";

    public static CounterId RetainedBlockCounter => new("retained_block");
    public static CounterId RedressDamageCounter => new("redress_damage");
    public static CounterId ActionsSinceReviewCounter => new("actions_since_review");
    public static CounterId ReviewsCounter => new("reviews_completed");
    public static CounterId ArticleIndexCounter => new("article_index");
    public static CounterId CharterBeatCounter => new("charter_beat");

    public const int RedressThreshold = 14;
    public const int PhaseOneActionsPerReview = 2;
    public const int CharterBeats = 5;

    // The Articles this fight reaches for, in order. One stands at a time; a strike-down publishes the next in this
    // rotation. Three, each one sentence a player reads at a glance (content cut 2026-10-29: Due Notice and
    // Reciprocal Burden are gone; 2026-11-01, user — one clear sentence a rule: Mutual Security is gone, and with
    // it the Emergency Amendment's second Article, its second phase and the Crisis's Exceptions).
    public static readonly string[] Articles =
        [ContinuanceId, RedressId, FullDisclosureId];

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheCharter(),
        PassiveStatuses.NamedMarker(ContinuanceId, "Article of Continuance",
            "Each side keeps half of its Block when its turn comes round again."),
        PassiveStatuses.NamedMarker(RedressId, "Article of Redress",
            "A side that loses 14 HP in a turn is owed 8 Block at the start of its next one."),
        // Full Disclosure is law that only widens the VIEW — the Charter shows its hand, and the design is
        // explicit that an article may favour the player.
        PassiveStatuses.NamedMarker(FullDisclosureId, "Article of Full Disclosure",
            "The player sees the top 2 cards of their draw pile and the Charter's next two intents."),
        Disclosing(DisclosedId, "Under Full Disclosure",
            "You see the top 2 cards of your draw pile and the Charter's next two intents."),
        PassiveStatuses.NamedMarker(ReviewPendingId, "Judicial Review",
            "Uphold the Article or strike it down before your turn ends."),
        PassiveStatuses.NamedMarker(RemedyId, "Remedy Due", "8 Block at the start of your next turn."),
        PassiveStatuses.NamedMarker(ExceptionClaimedId, "Exception Claimed",
            "The Articles do not touch you this turn."),
    ];

    public static IEnumerable<CardData> Cards() => [Uphold(), StrikeDown()];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheLawOpensThePlayersTurn(),
        TheLawAfterTheDraw(),
        TheLawClosesThePlayersTurn(),
        RedressIsRecorded(),
    ];

    // A status whose whole effect is what its bearer may SEE (the engine's disclosure rule).
    private static StatusData Disclosing(string id, string name, string description) =>
        PassiveStatuses.NamedMarker(id, name, description) with
        {
            Disclosure = new DisclosureData(DrawPileCards: 2, IntentLookahead: 1),
        };

    // ── The Articles, at the player's turn start ──────────────────────────────

    // Everything the law owes the CHARTER is paid here: the player's turn begins after the Charter's own turn
    // start has already cleared its Block, so this is where retained Block and Remedy can actually land on it.
    // The player's own dues wait for the draw (see below), because the player's Block is cleared after this.
    private static EncounterTriggerData TheLawOpensThePlayersTurn()
    {
        var player = CombatantTargetSelectors.Source;
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));

        IEffectNode<TurnStartedTriggeredEffectContext> ForCharter(IEffectNode<TurnStartedTriggeredEffectContext> body) =>
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(charter, body);

        ICombatExpression<TurnStartedTriggeredEffectContext, bool> CharterHas(string statusId) =>
            new IterationTargetHasStatusExpression<TurnStartedTriggeredEffectContext>(new StatusDefinitionId(statusId));

        var iterated = CombatantTargetSelectors.IterationTarget;

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    ForCharter(new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                    {
                        // Continuance: the half the Charter banked when its own turn began.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            CharterHas(ContinuanceId),
                            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                iterated,
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    iterated, RetainedBlockCounter))),
                        // Redress: the Charter's own remedy.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new AndExpression<TurnStartedTriggeredEffectContext>(
                                CharterHas(RedressId), CharterHas(RemedyId)),
                            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                            {
                                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                    iterated, new StatusDefinitionId(RemedyId)),
                                new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                    iterated, new ConstantExpression<TurnStartedTriggeredEffectContext>(8)),
                            })),
                        // Full Disclosure grants its sight to the PLAYER, so it too rides on a mirror.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            CharterHas(FullDisclosureId),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                player, new StatusDefinitionId(DisclosedId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            @else: new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                player, new StatusDefinitionId(DisclosedId))),
                        // The Judicial Review the Charter called is answered on this turn.
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                    player, new StatusDefinitionId(ReviewPendingId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                            {
                                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                    player, new CardDefinitionId(UpholdCardId), CardZone.Hand,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                    player, new CardDefinitionId(StrikeDownCardId), CardZone.Hand,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            })),
                    })),
                    // Continuance for the PLAYER: bank half of what is still standing, before it is cleared.
                    ForCharter(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        CharterHas(ContinuanceId),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            player, RetainedBlockCounter,
                            new DivideExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                                    player, StandardCombatIds.BlockDefensivePool),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                            relative: false))),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(ExceptionClaimedId)),
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // What the law owes the PLAYER lands after the draw: the turn-start Block clear resolves before this, so
    // Block granted here is Block the player actually keeps.
    private static EncounterTriggerData TheLawAfterTheDraw()
    {
        var player = CombatantTargetSelectors.Source;
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));

        ICombatExpression<CardsDrawnTriggeredEffectContext, bool> CharterHas(string statusId) =>
            new IterationTargetHasStatusExpression<CardsDrawnTriggeredEffectContext>(new StatusDefinitionId(statusId));

        ICombatExpression<CardsDrawnTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                player, new StatusDefinitionId(statusId));

        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    Stacks(PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(charter,
                    new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
                    {
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            CharterHas(ContinuanceId),
                            new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
                            {
                                new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                    player,
                                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                        player, RetainedBlockCounter)),
                                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                                    player, RetainedBlockCounter,
                                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                            })),
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            new AndExpression<CardsDrawnTriggeredEffectContext>(
                                CharterHas(RedressId),
                                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                    Stacks(RemedyId), ComparisonOperator.Greater,
                                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                            new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(new IEffectNode<CardsDrawnTriggeredEffectContext>[]
                            {
                                new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                                    player, new StatusDefinitionId(RemedyId)),
                                new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                    player, new ConstantExpression<CardsDrawnTriggeredEffectContext>(8)),
                            })),
                    }))));

        return new EncounterTriggerData("CardsDrawn",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()));
    }

    // At the player's turn end the Redress tallies start over, and an unanswered Review lapses into an uphold.
    private static EncounterTriggerData TheLawClosesThePlayersTurn()
    {
        var player = CombatantTargetSelectors.Source;
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        player, RedressDamageCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        charter, RedressDamageCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(ReviewPendingId)),
                })));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // Article of Redress: a side that loses 14 HP inside one turn is owed a remedy next turn.
    private static EncounterTriggerData RedressIsRecorded()
    {
        var hurt = CombatantTargetSelectors.EventTarget;
        var charter = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(CharterId));

        ICombatExpression<DamageReceivedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                hurt, new StatusDefinitionId(statusId));

        var tally = new AddExpression<DamageReceivedTriggeredEffectContext>(
            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(hurt, RedressDamageCounter),
            new EventAmountExpression<DamageReceivedTriggeredEffectContext>());

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(charter,
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new IterationTargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                        new StatusDefinitionId(RedressId)),
                    new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                    {
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            hurt, RedressDamageCounter,
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                        // The tally above is enqueued, so this blow has to count itself.
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            new AndExpression<DamageReceivedTriggeredEffectContext>(
                                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                    tally, ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(RedressThreshold)),
                                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                    Stacks(RemedyId), ComparisonOperator.Equal,
                                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                                hurt, new StatusDefinitionId(RemedyId),
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1))),
                    }))));

        return new EncounterTriggerData("DamageTaken",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>()));
    }

    // ── The Charter's own machinery ───────────────────────────────────────────

    private static StatusData TheCharter()
    {
        var self = CombatantTargetSelectors.Source;

        ICombatExpression<TurnStartedTriggeredEffectContext, int> StartStacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        // Continuance banks half of what is standing before the turn's clear takes it.
        var onTurnStart = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    StartStacks(ContinuanceId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    self, RetainedBlockCounter,
                    new DivideExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                            self, StandardCombatIds.BlockDefensivePool),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                    relative: false),
                @else: new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    self, RetainedBlockCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Counter(CounterId counter) =>
            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, counter);

        var onTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                // Every ordinary action counts toward the next Judicial Review.
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, ActionsSinceReviewCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),

                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, CharterBeatCounter,
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            Counter(CharterBeatCounter), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(CharterBeats)),
                    relative: false),
            }));

        return new StatusData
        {
            Id = CharterId,
            NameKey = "The Living Charter",
            DescriptionKey = "One Article changes the rules for both sides. Every second turn you may keep it or strike it down "
+ "for the next one.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    onTurnStart, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    onTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // ── The player's cards ────────────────────────────────────────────────────

    private static CardData Uphold() => Review(UpholdCardId, "Uphold the Article",
        "The Article stands. Gain 6 Block.",
        player =>
        [
            new GainBlockNode<CardPlayContext>(player, new ConstantExpression<CardPlayContext>(6)),
        ]);

    // Striking an Article down exchanges it for the next one in the Charter's own order — the law is replaced,
    // never simply deleted. The rotation index is kept on BOTH sides: a card can only read the player's
    // counter (its Source), the Charter's own actions only their own, so each writes both.
    private static CardData StrikeDown()
    {
        var charter = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(CharterId));
        var iterated = CombatantTargetSelectors.IterationTarget;
        var player = CombatantTargetSelectors.Source;

        IEffectNode<CardPlayContext> Publish(int index) =>
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantCounterExpression<CardPlayContext>(player, ArticleIndexCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayContext>(index)),
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    new ForEachTargetEffectNode<CardPlayContext>(charter,
                        new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                        {
                            // The standing Article goes; the next one is published in its place.
                            new RemoveStatusNode<CardPlayContext>(
                                iterated, new StatusDefinitionId(Articles[index])),
                            new ApplyStatusNode<CardPlayContext>(
                                iterated, new StatusDefinitionId(Articles[(index + 1) % Articles.Length]),
                                new ConstantExpression<CardPlayContext>(1)),
                            new GainBlockNode<CardPlayContext>(iterated, new ConstantExpression<CardPlayContext>(8)),
                            new SetCombatantCounterNode<CardPlayContext>(
                                iterated, ArticleIndexCounter,
                                new ConstantExpression<CardPlayContext>((index + 1) % Articles.Length),
                                relative: false),
                        })),
                    new SetCombatantCounterNode<CardPlayContext>(
                        player, ArticleIndexCounter,
                        new ConstantExpression<CardPlayContext>((index + 1) % Articles.Length), relative: false),
                }));

        return Review(StrikeDownCardId, "Strike Down the Article",
            "The Article is struck; the next prepared Article takes its place. The Charter gains 8 Block.",
            _ => [.. Enumerable.Range(0, Articles.Length).Select(Publish)]);
    }

    // Both answers are one-turn offers, and answering closes the Review.
    private static CardData Review(
        string id, string name, string text, Func<ICombatantTargetSelector, IEffectNode<CardPlayContext>[]> effects)
    {
        var player = CombatantTargetSelectors.Source;

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId("form"), new TagId("charter")],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusStacksExpression<CardPlayContext>(
                            player, new StatusDefinitionId(ReviewPendingId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayContext>(0)),
                    new SequenceEffectNode<CardPlayContext>(
                    [
                        new RemoveStatusNode<CardPlayContext>(player, new StatusDefinitionId(ReviewPendingId)),
                        .. effects(player),
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }

    // ── Raw intents ───────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "judicial_review" => JudicialReview(),
        _ => null,
    };

    // The Charter calls its own law into question: 6 damage, and the player answers on their next turn.
    private static EffectProgram<EnemyActionContext> JudicialReview()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(6)),
                new ApplyStatusNode<EnemyActionContext>(
                    player, new StatusDefinitionId(ReviewPendingId), new ConstantExpression<EnemyActionContext>(1)),
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, ActionsSinceReviewCounter,
                    new ConstantExpression<EnemyActionContext>(0), relative: false),
                new SetCombatantCounterNode<EnemyActionContext>(
                    self, ReviewsCounter, new ConstantExpression<EnemyActionContext>(1), relative: true),
            }));
    }

}
