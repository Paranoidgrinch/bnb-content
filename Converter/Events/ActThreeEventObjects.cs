using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// The shared Act-III event objects (BnB_Final_Events_Master_PostAudit.md, "ACT III"). The Green Docket's
// fifteen doors are written out of this vocabulary, the way the city's and the archives' were.
//
// The city wrote MARKINGS on a card for one fight. The archives wrote INSCRIPTIONS that never come off. The
// Green Docket writes inscriptions too — five of them — but its own subject is different: an inscription
// here is a courtesy the road extends to one card, and it is worded the way the act words everything, in
// terms of what you did FIRST, what you did BEFORE, and what you are still holding.
//
//   Rowan-Blessed   — the card that opens a turn is sheltered.
//   Way-Knotted     — a card that changes the price is worth an Energy.
//   Hearth-Kept     — a card you keep is never put away, and comes cheaper.
//   Stone-Witnessed — a card aimed where somebody has already been aimed does more.
//   Old Right       — a card that would burn itself out is only discarded.
//
// Beside them the act's doors write one thing on the FIGHT rather than on a card: an environmental demand,
// which is a Wergild owed to nobody. It is settled through the same Make Amends as every other demand, and
// leaving it owing costs the ordinary 2 HP a point and creates no standing, because there is nobody for the
// standing to belong to.
public static class ActThreeEventObjects
{
    // ★ Static initializers run in DECLARATION order: every id and latch a rule below names is declared here.

    public const string RowanBlessed = "rowan_blessed";
    public const string WayKnotted = "way_knotted";
    public const string HearthKept = "hearth_kept";
    public const string StoneWitnessed = "stone_witnessed";
    public const string OldRightInscription = "old_right_inscription";

    public const string AttendedByYou = "attended_by_you";

    private static CounterId RowanSpent => new("rowan_blessed_spent");
    private static CounterId KnotSpent => new("way_knotted_spent");
    private static CounterId HearthSpent => new("hearth_kept_spent");
    private static CounterId WitnessSpent => new("stone_witnessed_spent");
    private static CounterId OldRightSpent => new("old_right_spent");
    private static CounterId KnotMemory => new("way_knotted_last_cost");

    public static IReadOnlyList<string> Inscriptions() =>
        [RowanBlessed, WayKnotted, HearthKept, StoneWitnessed, OldRightInscription];

    public static IReadOnlyList<StatusData> Statuses() =>
    [
        RowanBlessedRule, WayKnottedRule, HearthKeptRule, StoneWitnessedRule, OldRightRule,
        Marker(AttendedByYou, "Attended To",
            "You have aimed something at this party this turn."),
        EnvironmentalWergild(), EnvironmentalDue(),
    ];

    // ── Rowan-Blessed ─────────────────────────────────────────────────────────────────────────────────────

    // "First time each combat this card is the first card played in a turn: gain 5 Block."
    public static readonly StatusData RowanBlessedRule = Rule(
        RowanBlessed, "Rowan-Blessed",
        "A card the rowan sheltered. The first time each fight you open a turn with it, you gain 5 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                OncePerFight(RowanSpent,
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        PlayedCarries(RowanBlessed),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(You),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                    new GainBlockNode<CardPlayedTriggeredEffectContext>(
                        You, new ConstantExpression<CardPlayedTriggeredEffectContext>(5)))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // ── Way-Knotted ───────────────────────────────────────────────────────────────────────────────────────

    // "First time each combat this card follows a card with a different base cost: gain 1 Energy." The price
    // of the card before is written down plus one, so that a free card is a price and not an empty cell —
    // the same cell every "two in a row" rule in the act keeps, and this one keeps its OWN.
    public static readonly StatusData WayKnottedRule = Rule(
        WayKnotted, "Way-Knotted",
        "A knot tied where the road changes. The first time each fight you play it straight after a card of "
        + "a different price, you gain 1 Energy.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    OncePerFight(KnotSpent,
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            PlayedCarries(WayKnotted),
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                        You, KnotMemory),
                                    ComparisonOperator.Greater,
                                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                                new NotExpression<CardPlayedTriggeredEffectContext>(
                                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                            You, KnotMemory),
                                        ComparisonOperator.Equal, PlayedCostPlusOne())))),
                        HeldEnergy.Hold<CardPlayedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        You, KnotMemory, PlayedCostPlusOne(), relative: false),
                ])),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    You, KnotMemory, new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                    relative: false)),
                nameof(TriggerEvent.TurnStarted)),
        ]);

    // ── Hearth-Kept ───────────────────────────────────────────────────────────────────────────────────────

    // "First time each combat the card remains in hand at turn end: Retain; next-turn cost −1."
    //
    // ADAPTATION: a turn-end program cannot see the hand — the discard runs first — so the keeping is not
    // watched, it is granted, exactly as Act II's Late-Bound is. A hearth-kept card always Retains, and the
    // first turn after the first on which you are still holding it, it is a point cheaper.
    public static readonly StatusData HearthKeptRule = Rule(
        HearthKept, "Hearth-Kept",
        "A card somebody kept warm for you. It is never put away at the end of a turn, and the second turn "
        + "you are still holding it, it costs 1 less.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Keep(CardZone.Hand), Keep(CardZone.DrawPile),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                            Unspent<CardsDrawnTriggeredEffectContext>(HearthSpent)),
                        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                            You, CardZone.Hand,
                            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [
                                Discount<CardsDrawnTriggeredEffectContext>(1),
                                Spend<CardsDrawnTriggeredEffectContext>(HearthSpent),
                            ]),
                            markFilter: new TagId(HearthKept), takeFirst: 1)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Stone-Witnessed ───────────────────────────────────────────────────────────────────────────────────

    // "First time each combat the card targets an enemy already targeted by another player card that turn:
    // positive numerical effects +25%."
    //
    // ADAPTATION: a card's output cannot be scaled between choosing it and resolving it, so the witnessing
    // arms the copy rather than the play. The first time each fight you aim anything at a party you have
    // already aimed at this turn, every witnessed copy you are carrying does a quarter more from then on.
    public static readonly StatusData StoneWitnessedRule = Rule(
        StoneWitnessed, "Stone-Witnessed",
        "A card the old stones watched. Once each fight, striking where you have already struck this turn "
        + "makes it worth a quarter more for the rest of the fight.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    OncePerFight(WitnessSpent,
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget,
                                new StatusDefinitionId(AttendedByYou)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            Witness(CardZone.Hand), Witness(CardZone.DrawPile), Witness(CardZone.DiscardPile),
                        ])),
                    // Whatever it did, this is now a party you have been aimed at.
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId(AttendedByYou),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                ])),
                nameof(TriggerEvent.CardPlayed)),
            // A new turn is a new road: nobody has been attended to yet.
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllAliveCombatants,
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new StatusDefinitionId(AttendedByYou)))),
                nameof(TriggerEvent.TurnStarted)),
        ]);

    // ── Old Right ─────────────────────────────────────────────────────────────────────────────────────────

    // "First time each combat the card would enter Exhaust because of its own normal post-play Exhaust: put
    // it in Discard instead."
    //
    // ADAPTATION: nothing can stand between a played card and where it goes, so the old right is not a
    // refusal but a RECOVERY, a beat late: once each fight, at your next bell, a card of yours that burned
    // itself out is found in the ashes and filed with the rest. The same shape Act II's True Name uses.
    public static readonly StatusData OldRightRule = Rule(
        OldRightInscription, "Old Right",
        "An older right than the one that burns a card. Once each fight, a card of yours that exhausted "
        + "itself is back in your discard pile at the next bell.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Unspent<CardsDrawnTriggeredEffectContext>(OldRightSpent),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                        You, CardZone.ExhaustPile,
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                                You, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                CardZone.DiscardPile),
                            Spend<CardsDrawnTriggeredEffectContext>(OldRightSpent),
                        ]),
                        markFilter: new TagId(OldRightInscription), takeFirst: 1))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── An environmental demand ───────────────────────────────────────────────────────────────────────────
    //
    // A Wergild owed to nobody: the hedge's, the bridge's, the road's. It is settled through the act's one
    // payment like every other demand, and left owing it costs the ordinary 2 HP a point — and creates no
    // standing, because there is no party for standing to belong to.
    public const string EnvironmentalWergildId = "environmental_wergild";
    public const string EnvironmentalDueId = "environmental_wergild_due";

    public static StatusData EnvironmentalWergild() => new()
    {
        Id = EnvironmentalWergildId,
        NameKey = "A Demand of the Road",
        DescriptionKey =
            "Something here is owed, and there is nobody to owe it to. Make Amends settles it like any other "
            + "demand; left owing at the end of your next turn it costs you 2 HP a point, and nothing else.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData EnvironmentalDue() => new()
    {
        Id = EnvironmentalDueId,
        NameKey = "Due at the Bell",
        DescriptionKey = "The road's demand is settled at the end of this turn, paid or not.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;

    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> PlayedCostPlusOne() =>
        new AddExpression<CardPlayedTriggeredEffectContext>(
            new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                StandardCombatIds.EnergyResource),
            new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Keep(CardZone zone) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            You, zone,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                You, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                StandardCombatIds.RetainedCardMark),
            markFilter: new TagId(HearthKept));

    private static IEffectNode<CardPlayedTriggeredEffectContext> Witness(CardZone zone) =>
        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
            You, zone,
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                    You, new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.CardOutputScaleNumeratorCounter,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(5), relative: false),
                new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                    You, new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.CardOutputScaleDenominatorCounter,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(4), relative: false),
            ]),
            markFilter: new TagId(StoneWitnessed));

    private static IEffectNode<TContext> Discount<TContext>(int amount) where TContext : class =>
        new SetCardInstanceMarkCounterNode<TContext>(
            You, new IteratedCardExpression<TContext>(), StandardCombatIds.CardCostDeltaCounter,
            new ConstantExpression<TContext>(-amount), relative: true);

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(You, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            You, latch, new ConstantExpression<TContext>(1), relative: false);

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedCarries(string mark) =>
        new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(mark));

    private static IEffectNode<CardPlayedTriggeredEffectContext> OncePerFight(
        CounterId latch, ICombatExpression<CardPlayedTriggeredEffectContext, bool> when,
        IEffectNode<CardPlayedTriggeredEffectContext> body) =>
        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                when, Unspent<CardPlayedTriggeredEffectContext>(latch)),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [body, Spend<CardPlayedTriggeredEffectContext>(latch)]));

    private static StatusData Marker(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
    };

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers = triggers,
        };

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Bearer);
}
