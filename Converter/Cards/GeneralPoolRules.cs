using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// The rules behind the general pool (GENERAL_POOL_PLAN.md, 2026-11-08): the Rites' statuses, the marks some cards
// leave on enemies or on you, and the listeners every hero carries.
//
// A card program only runs while the card is played, and a card can watch nothing from its pile. Whatever
// "Unread:", Corpse Candle, the Salamander, Last Ember, Ashen Effigy or the Almanac need to hear is therefore
// heard by the Applicant — the status the hero wears in every fight — and those triggers are written here and
// added to it (Keywords).
public static class GeneralPoolRules
{
    public const string VigilLamp = "vigil_lamp";
    public const string MisersCoffer = "misers_coffer";
    public const string ErrandRook = "errand_rook";
    public const string WhisperingGallery = "whispering_gallery";
    public const string PyreOfOldRecords = "pyre_of_old_records";
    public const string BillOfAttainder = "bill_of_attainder";
    public const string Rowan = "rowan_over_the_door";
    public const string Stayed = "stay_of_proceedings";
    public const string StandingStones = "standing_stones";

    public const string AlmanacTag = "dog_eared_almanac";
    public const string EffigyTag = "ashen_effigy";
    public const string CorpseCandleTag = "corpse_candle";
    public const string SalamanderTag = "salamander_in_the_grate";
    public const string LastEmberTag = "last_ember";

    public const string LastExhaustedMark = "last_exhausted";

    // The Charnel Ledger relic (relic audit 2026-11-08) is a marker this rule looks for.
    public const string CharnelLedgerId = "charnel_ledger";
    public const string ReadByFirelightMark = "read_by_firelight";

    public static CounterId AlmanacsHeld => new("almanacs_held");

    // The turn a copy was last played on, so a move out of the hand can tell "played" from "thrown away".
    private static CounterId PlayedOn => new("general_played_on");

    public static readonly ZonePlacement RandomPlacement = ZonePlacement.Random;

    // A hand of this many cards is "full" for Errand Rook. The game itself sets no hand limit.
    public const int FullHand = 10;

    private const int SalamanderThreshold = 6;

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;

    public static IReadOnlyList<StatusData> All() =>
    [
        Rite(VigilLamp, "Vigil Lamp", "At the end of your turn, gain 8 Block for each Energy you have left.",
            [Trigger(LeftoverToBlock(VigilLamp, 8), nameof(TriggerEvent.TurnEnded))]),
        Rite(VigilLamp + "+", "Vigil Lamp+", "At the end of your turn, gain 12 Block for each Energy you have left.",
            [Trigger(LeftoverToBlock(VigilLamp + "+", 12), nameof(TriggerEvent.TurnEnded))]),
        Rite(MisersCoffer, "Miser's Coffer", "At the end of your turn, keep up to 2 unspent Energy as Held Energy.",
            [Trigger(LeftoverToHeld(MisersCoffer, 2), nameof(TriggerEvent.TurnEnded))]),
        Rite(MisersCoffer + "+", "Miser's Coffer+",
            "At the end of your turn, keep up to 3 unspent Energy as Held Energy.",
            [Trigger(LeftoverToHeld(MisersCoffer + "+", 3), nameof(TriggerEvent.TurnEnded))]),
        Rite(ErrandRook, "Errand Rook",
            $"At the start of your turn, draw 1 more card. Whenever your hand is full ({FullHand} cards), gain 3 Block.",
            [Trigger(FullHandBlock(), nameof(TriggerEvent.CardsDrawn))],
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, 1,
                    RestrictDamageKind: null),
            ]),
        Rite(WhisperingGallery, "Whispering Gallery",
            "Whenever a card leaves your hand without being played, a random enemy gets 1 Doubt.",
            [
                Trigger(Model<CardMovedToZoneTriggeredEffectContext>(Whisper(), when: Unplayed<CardMovedToZoneTriggeredEffectContext>()),
                    nameof(TriggerEvent.CardMovedToZone)),
                Trigger(WhisperEach(), nameof(TriggerEvent.HandDiscarded)),
            ]),
        Rite(PyreOfOldRecords, "Pyre of Old Records",
            "Whenever a card is exhausted, deal 7 damage to a random enemy and gain 7 Block.",
            [Trigger(Model<CardMovedToZoneTriggeredEffectContext>(Pyre(),
                when: new TriggerEventCardZoneExpression<CardMovedToZoneTriggeredEffectContext>(CardZone.ExhaustPile)),
                nameof(TriggerEvent.CardMovedToZone))]),
        Rite(BillOfAttainder, "Bill of Attainder", "Whenever you apply Doubt or Liable, apply 1 more.", [],
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.OutgoingStatusApplicationStacks,
                    PassiveModifierOperation.AddPerStack, 1, RestrictDamageKind: null, AppliesToStatusId: Keywords.Doubt),
                new PassiveModifierData(PassiveModifierPipeline.OutgoingStatusApplicationStacks,
                    PassiveModifierOperation.AddPerStack, 1, RestrictDamageKind: null, AppliesToStatusId: Keywords.Liable),
            ]),
        RowanStatus(),
        StayedStatus(),
        StandingStonesStatus(),
    ];

    // ── the Applicant's listeners ─────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<StatusTriggerData> ApplicantTriggers() =>
    [
        // Stamp each played copy with its round: a copy leaving the hand in the round of its stamp was played.
    // (The round, not CurrentTurn, which does not count up from one player turn to the next.)
        Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
            new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                You, new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                PlayedOn, new RoundNumberExpression<CardPlayedTriggeredEffectContext>())),
            nameof(TriggerEvent.CardPlayed)),

        Trigger(Moved(), nameof(TriggerEvent.CardMovedToZone)),
        Trigger(Model<CardsDrawnTriggeredEffectContext>(PriceEffigies()), nameof(TriggerEvent.CardsDrawn)),
        Trigger(Model<TurnEndedTriggeredEffectContext>(TurnEnds()), nameof(TriggerEvent.TurnEnded)),
        Trigger(Model<TurnStartedTriggeredEffectContext>(SalamanderReturns()), nameof(TriggerEvent.TurnStarted)),
    ];

    // Every card that has an "Unread:", with what it does.
    private static IReadOnlyList<(string CardId, CombatNodeModel Effect)> Unreads() =>
    [
        .. GeneralEnergy.Unreads(), .. GeneralDiscard.Unreads(), .. GeneralDefence.Unreads(),
    ];

    private static EffectProgram<CardMovedToZoneTriggeredEffectContext> Moved()
    {
        var card = new TriggerEventCardInstanceExpression<CardMovedToZoneTriggeredEffectContext>();
        var toExhaust = new TriggerEventCardZoneExpression<CardMovedToZoneTriggeredEffectContext>(CardZone.ExhaustPile);

        IEffectNode<CardMovedToZoneTriggeredEffectContext> unread = new SequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
        [
            .. Unreads().Select(u => (IEffectNode<CardMovedToZoneTriggeredEffectContext>)
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new CardInstanceHasTagExpression<CardMovedToZoneTriggeredEffectContext>(
                        card, new TagId(GeneralPoolShapes.UnreadIdTag(u.CardId))),
                    Node<CardMovedToZoneTriggeredEffectContext>(u.Effect))),
            // Besom Guard: the broom goes back on top of the draw pile.
            .. GeneralDefence.SweepsBack.Select(id => (IEffectNode<CardMovedToZoneTriggeredEffectContext>)
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new CardInstanceHasTagExpression<CardMovedToZoneTriggeredEffectContext>(
                        card, new TagId(GeneralPoolShapes.UnreadIdTag(id))),
                    new MoveCardToZoneNode<CardMovedToZoneTriggeredEffectContext>(
                        You, card, CardZone.DrawPile, placement: ZonePlacement.Top))),
        ]);

        return new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
            new SequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
            [
                // Unread: thrown away from the hand by an effect, not played — twice with the Charnel Ledger.
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new AndExpression<CardMovedToZoneTriggeredEffectContext>(
                        Unplayed<CardMovedToZoneTriggeredEffectContext>(),
                        new CardInstanceHasTagExpression<CardMovedToZoneTriggeredEffectContext>(
                            card, new TagId(GeneralPoolShapes.UnreadTag))),
                    new SequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    [
                        unread,
                        new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                            new TargetHasStatusExpression<CardMovedToZoneTriggeredEffectContext>(
                                You, new StatusDefinitionId(CharnelLedgerId)),
                            unread),
                    ])),

                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(toExhaust,
                    new SequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    [
                        // Corpse Candle: another card exhausted calls every Exhausted Candle back.
                        new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                            new NotExpression<CardMovedToZoneTriggeredEffectContext>(
                                new CardInstanceHasTagExpression<CardMovedToZoneTriggeredEffectContext>(
                                    card, new TagId(CorpseCandleTag))),
                            Node<CardMovedToZoneTriggeredEffectContext>(CombatNodeModel.ForEachCard(
                                CardAuthoring.You, CardZone.ExhaustPile,
                                new CombatNodeModel("moveCardToZone", CardAuthoring.You,
                                    Card: new CombatCardSpec("iterated"), ToZone: CardZone.Hand),
                                tag: CorpseCandleTag))),

                        // Last Ember: the newest exhausted card carries the mark — never a Last Ember itself,
                        // which would only replay the search for the one before it.
                        new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                            new NotExpression<CardMovedToZoneTriggeredEffectContext>(
                                new CardInstanceHasTagExpression<CardMovedToZoneTriggeredEffectContext>(
                                    card, new TagId(LastEmberTag))),
                            new SequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
                            [
                                Node<CardMovedToZoneTriggeredEffectContext>(CombatNodeModel.ForEachCard(
                                    CardAuthoring.You, CardZone.ExhaustPile,
                                    new CombatNodeModel("markCardInstance", CardAuthoring.You,
                                        Card: new CombatCardSpec("iterated"), ToTag: LastExhaustedMark, Relative: true),
                                    mark: LastExhaustedMark)),
                                new MarkCardInstanceNode<CardMovedToZoneTriggeredEffectContext>(
                                    You, card, new TagId(LastExhaustedMark)),
                            ])),
                    ])),

                // Ashen Effigy: whatever moved, the pile may have changed.
                Node<CardMovedToZoneTriggeredEffectContext>(PriceEffigies()),
            ]));
    }

    private static ICombatExpression<TContext, bool> Unplayed<TContext>() where TContext : class
    {
        var card = new TriggerEventCardInstanceExpression<TContext>();
        return new AndExpression<TContext>(
            new TriggerEventCardZoneExpression<TContext>(CardZone.Hand, from: true),
            new AndExpression<TContext>(
                new OrExpression<TContext>(
                    new TriggerEventCardZoneExpression<TContext>(CardZone.DiscardPile),
                    new TriggerEventCardZoneExpression<TContext>(CardZone.ExhaustPile)),
                new NotExpression<TContext>(
                    new ComparisonExpression<TContext>(
                        new CardInstanceMarkCounterExpression<TContext>(card, PlayedOn),
                        ComparisonOperator.Equal, new RoundNumberExpression<TContext>()))));
    }

    // "Costs 1 less for each 2 cards in your Exhausted pile": every Effigy in hand is re-priced from scratch.
    private static CombatNodeModel PriceEffigies() =>
        CombatNodeModel.ForEachCard(CardAuthoring.You, CardZone.Hand,
            new CombatNodeModel("setCardInstanceMarkCounter", CardAuthoring.You,
                CombatAmountSpec.Unary("neg", CombatAmountSpec.Binary("div", CardsInZone(CardZone.ExhaustPile),
                    CombatAmountSpec.FromConst(2))),
                Card: new CombatCardSpec("iterated"), CounterId: StandardCombatIds.CardCostDeltaCounter.value,
                Relative: false),
            tag: EffigyTag);

    // The turn's end: the hand has been discarded, so what is left in it was retained.
    private static CombatNodeModel TurnEnds() =>
        Seq(
            // Dog-Eared Almanac: how many are waiting for next turn.
            new CombatNodeModel("setCombatantCounter", CardAuthoring.You, CardsTagged(AlmanacTag),
                CounterId: AlmanacsHeld.value, Relative: false),
            // Read by Firelight: what was not played goes where it was promised.
            CombatNodeModel.ForEachCard(CardAuthoring.You, CardZone.DiscardPile, Seq(
                    new CombatNodeModel("markCardInstance", CardAuthoring.You, Card: new CombatCardSpec("iterated"),
                        ToTag: ReadByFirelightMark, Relative: true),
                    new CombatNodeModel("markCardInstance", CardAuthoring.You, Card: new CombatCardSpec("iterated"),
                        ToTag: StandardCombatIds.TemporaryCopyMark.value, Relative: true),
                    new CombatNodeModel("moveCardToZone", CardAuthoring.You, Card: new CombatCardSpec("iterated"),
                        ToZone: CardZone.ExhaustPile)),
                mark: ReadByFirelightMark));

    private static CombatNodeModel SalamanderReturns() =>
        Seq(
            new CombatNodeModel("setCombatantCounter", CardAuthoring.You, CardsInZone(CardZone.ExhaustPile),
                CounterId: GeneralPoolShapes.ScratchId.value, Relative: false),
            GeneralPoolShapes.IfScratch(ComparisonOperator.GreaterOrEqual, SalamanderThreshold,
                CombatNodeModel.ForEachCard(CardAuthoring.You, CardZone.ExhaustPile,
                    new CombatNodeModel("moveCardToZone", CardAuthoring.You, Card: new CombatCardSpec("iterated"),
                        ToZone: CardZone.Hand),
                    tag: SalamanderTag)));

    // ── Rites ─────────────────────────────────────────────────────────────────────────────────────────────

    private static CombatAmountSpec LeftoverEnergy =>
        new("currentResource", SelectorKey: CardAuthoring.You, ReadId: Energy.value);

    // Two Lamps burn twice as bright.
    private static EffectProgram<TurnEndedTriggeredEffectContext> LeftoverToBlock(string self, int per) =>
        Model<TurnEndedTriggeredEffectContext>(Block(Times(
            CombatAmountSpec.Binary("mul", LeftoverEnergy, Stacks(self, CardAuthoring.You)), per)));

    private static EffectProgram<TurnEndedTriggeredEffectContext> LeftoverToHeld(string self, int most) =>
        Model<TurnEndedTriggeredEffectContext>(
            Seq(
                new CombatNodeModel("setCombatantCounter", CardAuthoring.You,
                    CombatAmountSpec.Binary("min", LeftoverEnergy, Times(Stacks(self, CardAuthoring.You), most)),
                    CounterId: GeneralPoolShapes.ScratchId.value, Relative: false),
                GeneralPoolShapes.IfScratch(ComparisonOperator.GreaterOrEqual, 1,
                    Apply(HeldEnergy.Id, GeneralPoolShapes.Scratch, CardAuthoring.You))));

    private static EffectProgram<CardsDrawnTriggeredEffectContext> FullHandBlock() =>
        Model<CardsDrawnTriggeredEffectContext>(
            Seq(
                new CombatNodeModel("setCombatantCounter", CardAuthoring.You, CardsInZone(CardZone.Hand),
                    CounterId: GeneralPoolShapes.ScratchId.value, Relative: false),
                GeneralPoolShapes.IfScratch(ComparisonOperator.GreaterOrEqual, FullHand,
                    Block(Times(Stacks(ErrandRook, CardAuthoring.You), 3)))));

    private static CombatNodeModel Whisper() =>
        new CombatNodeModel("repeat", Amount: Stacks(WhisperingGallery, CardAuthoring.You),
            Children: [new CombatNodeModel("randomTargets", AllEnemies, CombatAmountSpec.FromConst(1),
                Children: [Apply(Keywords.Doubt, 1, "iterationTarget")])]);

    // The end-of-turn discard: one whisper for each card it took.
    private static EffectProgram<HandDiscardedTriggeredEffectContext> WhisperEach() =>
        Model<HandDiscardedTriggeredEffectContext>(
            new CombatNodeModel("repeat", Amount: CombatAmountSpec.Event, Children: [Whisper()]));

    private static CombatNodeModel Pyre() =>
        new CombatNodeModel("repeat", Amount: Stacks(PyreOfOldRecords, CardAuthoring.You),
            Children: [Seq(GeneralPoolShapes.DamageRandomEnemy(7), Block(7))]);

    // "Until your next turn, every enemy that attacks you gets 1 Doubt." In a damage event the source is whoever
    // swung; the hero's own HP losses are left alone.
    private static StatusData RowanStatus() => new()
    {
        Id = Rowan,
        NameKey = "Rowan over the Door",
        DescriptionKey = "Until your next turn, every enemy that attacks you gets 1 Doubt.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new NotExpression<DamageReceivedTriggeredEffectContext>(
                        new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId))),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Doubt),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.DamageTaken)),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(You, new StatusDefinitionId(Rowan))),
                nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // "An enemy's next action does nothing": consumed as it starts, like a landed Misfortune.
    private static StatusData StayedStatus() => new()
    {
        Id = Stayed,
        NameKey = "Stay of Proceedings",
        DescriptionKey = "This character's next action does nothing.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<ActionStartingTriggeredEffectContext>(
                new SequenceEffectNode<ActionStartingTriggeredEffectContext>(
                [
                    new ApplyStatusNode<ActionStartingTriggeredEffectContext>(
                        You, StandardCombatIds.ActionFailsStatus,
                        new ConstantExpression<ActionStartingTriggeredEffectContext>(1)),
                    new ModifyStatusStacksNode<ActionStartingTriggeredEffectContext>(
                        You, new StatusDefinitionId(Stayed),
                        new ConstantExpression<ActionStartingTriggeredEffectContext>(-1)),
                ])),
                nameof(TriggerEvent.ActionStarting)),
        ],
    };

    // "Your Block does not expire at the start of your next 2 turns": applied with a duration of 3, which ticks
    // down at the end of each of your turns — this one, then the next two.
    private static StatusData StandingStonesStatus() => new()
    {
        Id = StandingStones,
        NameKey = "Standing Stones",
        DescriptionKey = "Your Block does not expire at the start of your turn.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        UsesDuration = true,
        Tags = [StandardCombatIds.RetainBlockTag.value],
    };

    // ── plumbing ──────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Rite(string id, string name, string description,
        IReadOnlyList<StatusTriggerData> triggers, IReadOnlyList<PassiveModifierData>? passives = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers = triggers,
            PassiveModifiers = passives ?? [],
        };

    private static IEffectNode<TContext> Node<TContext>(CombatNodeModel model) where TContext : class =>
        CombatProgramModel.Build<TContext>(model).Root;

    private static EffectProgram<TContext> Model<TContext>(CombatNodeModel model,
        ICombatExpression<TContext, bool>? when = null) where TContext : class =>
        new(when is null ? Node<TContext>(model) : new ConditionalEffectNode<TContext>(when, Node<TContext>(model)));

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Bearer);
}
