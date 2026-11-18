using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Sandbox.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// ── THE RUN MODIFIERS, AS RELICS (META_PROGRESSION_PLAN.md §3, user 2026-11-17) ───────────────────────────────
// Shells buy run modifiers in the manner of Risk of Rain 2's artifacts, and a run plays with the ones the player
// switched on. What a modifier DOES is written here, as a relic "mod_<id>" the run starts with: the frontend
// (bnb-godot Shells.ForRun) puts the armed ones among the starting relics, and the relic bar shows them — the
// run wears its own terms. A modifier whose relic is not defined here is shown in the shop but cannot be bought.
//
// The relics sit in the Event pool and nowhere else: no chest, shelf, boss or event ever offers one (like the
// trial's Infirmary). What happens "at the start of the run" rides on RunStarted, which a resumed run never
// raises again — a starting relic's pickup effects are not run by the engine.
public static class ModifierRelics
{
    public const string Prefix = "mod_";
    private const string Source = "Run modifier";

    public static IReadOnlyList<BnbRelic> All(IEnumerable<BnbRelic> normalRelics) =>
    [
        Overtime(),
        StatuteOfLimitations(),
        SpiteFiling(),
        StrictFiling(),
        InheritedDebt(),
        GlassSeal(),
        PaperTrail(),
        ClericalError(),
        SealedEnvelopes(),
        BureaucraticSacrifice(),
        RequisitionForm(),
        SecondOpinion(),
        SigningBonus(normalRelics),
    ];

    public static IReadOnlyList<StatusData> Statuses() => [GlassSealRule, StatuteRule, SpiteRule, ClericalRule];

    private static StatusTriggerData Trigger<TContext>(IEffectNode<TContext> body, TriggerEvent trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger.ToString(), JsonSerializer.SerializeToElement(
            TheOpeningHand.For(new EffectProgram<TContext>(body), trigger.ToString()), CombatJson.CreateOptions<TContext>()), scope);

    private static ICombatExpression<T, int> N<T>(int value) where T : class => new ConstantExpression<T>(value);

    private static readonly RewardMatch CardRewards = new(Kind: RewardKinds.Card);

    // Bad, +1: from round 8 of a fight the enemies grow angry, +3 Strength every round after.
    public const string StatuteRuleId = "mod_statute_rule";

    private static readonly StatusData StatuteRule = Rule(StatuteRuleId, "Statute of Limitations",
        "Run modifier: from round 8, enemies gain 3 Strength every round.",
        [Trigger(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(new RoundNumberExpression<TurnStartedTriggeredEffectContext>(),
                ComparisonOperator.GreaterOrEqual, N<TurnStartedTriggeredEffectContext>(8)),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.AllEnemiesOfSource,
                new StatusDefinitionId("strength"), N<TurnStartedTriggeredEffectContext>(3))), TriggerEvent.TurnStarted)]);

    private static BnbRelic StatuteOfLimitations() => Event(Prefix + "statute_of_limitations", "Statute of Limitations", Source,
        "Run modifier. From round 8 of a fight, enemies gain 3 Strength every round.", combatRule: StatuteRule);

    // Bad, +1: every enemy that falls strikes back once, 5 damage to the hero. Watched on anyone (the fallen is
    // the event's source); its enemies wearing this rule are the hero.
    public const string SpiteRuleId = "mod_spite_rule";

    private static readonly StatusData SpiteRule = Rule(SpiteRuleId, "Spite Filing",
        "Run modifier: every enemy that falls deals 5 damage to you.",
        [Trigger<CombatantDownedTriggeredEffectContext>(
            new DealDamageNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(SpiteRuleId)),
                N<CombatantDownedTriggeredEffectContext>(5)),
            TriggerEvent.Downed, StatusTriggerScope.Anywhere)]);

    private static BnbRelic SpiteFiling() => Event(Prefix + "spite_filing", "Spite Filing", Source,
        "Run modifier. Every enemy that falls deals 5 damage to you.", combatRule: SpiteRule);

    // Bad, +1: every card reward shows one card fewer.
    private static BnbRelic StrictFiling() => new(Prefix + "strict_filing", "Strict Filing",
        "Run modifier. Card rewards offer one card fewer.", Pool.Event, Rarity.Event, Source: Source,
        RewardRules: [new DropOffersRule(CardRewards, 1)]);

    // Different, ±0: twice the gold, twice the prices of what is for sale; services (a removal) keep their price.
    public const string PaperTrailEcho = "mod_paper_trail_echo";

    private static BnbRelic PaperTrail() => new(Prefix + "paper_trail", "Paper Trail",
        "Run modifier. You gain double gold; cards, relics and potions in shops cost double.", Pool.Event, Rarity.Event,
        Source: Source,
        RunPrograms:
        [
            // Every gain of gold is answered by the same again — once: the answer itself is a gain, and the echo
            // counter keeps it from being answered in turn.
            RunPrograms.When<ResourceChangedRunEvent>(
                new AndExpression(new AndExpression(
                        RunEventValues.ResourceIs(StandardRunIds.Gold),
                        new RunComparisonExpression(RunEventValues.ResourceDelta, RunComparisonOperator.GreaterThan, new RunConstantExpression(0))),
                    new RunComparisonExpression(new CounterValueExpression(new RunCounterId(PaperTrailEcho)), RunComparisonOperator.Equal, new RunConstantExpression(0))),
                // A template, not a queued effect reading the event: the queue drains after the event is gone.
                RunEffectTemplates.Literal(new SetCounterRunEffect(new RunCounterId(PaperTrailEcho), 1)),
                RunEffectTemplates.GainResource(StandardRunIds.Gold, RunEventValues.ResourceDelta),
                RunEffectTemplates.Literal(new SetCounterRunEffect(new RunCounterId(PaperTrailEcho), 0))),
        ],
        ShopPrices:
        [
            new ShopPriceRule(new ShopPriceMatch(Kind: "card"), PercentDelta: 100),
            new ShopPriceRule(new ShopPriceMatch(Kind: "relic"), PercentDelta: 100),
            new ShopPriceRule(new ShopPriceMatch(Kind: "consumable"), PercentDelta: 100),
        ]);

    // Different, ±0: after each draw one card in hand costs 0 this turn and another costs 1 more. The two copies
    // are marked so the turn's end can give their prices back.
    public const string ClericalRuleId = "mod_clerical_rule";
    private const string ClericalFree = "mod_clerical_free";
    private const string ClericalDear = "mod_clerical_dear";

    private static StatusData ClericalRule => _clerical ??= BuildClerical();
    private static StatusData? _clerical;

    private static StatusData BuildClerical()
    {
        var hero = CombatantTargetSelectors.Source;
        var free = new TagId(ClericalFree);
        var dear = new TagId(ClericalDear);
        IEffectNode<T> SetDelta<T>(int value, bool relative) where T : class =>
            new SetCardInstanceMarkCounterNode<T>(hero, new IteratedCardExpression<T>(), StandardCombatIds.CardCostDeltaCounter, N<T>(value), relative);
        IEffectNode<T> Unmark<T>(TagId mark) where T : class =>
            new MarkCardInstanceNode<T>(hero, new IteratedCardExpression<T>(), mark, remove: true);

        var drawn = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(hero, new RandomCardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand), free),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(hero, CardZone.Hand, SetDelta<CardsDrawnTriggeredEffectContext>(-9, relative: false), markFilter: free),
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(hero, new RandomCardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand), dear),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(hero, CardZone.Hand,
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new NotExpression<CardsDrawnTriggeredEffectContext>(new CardInstanceHasMarkExpression<CardsDrawnTriggeredEffectContext>(
                        new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), free)),
                    SetDelta<CardsDrawnTriggeredEffectContext>(1, relative: true),
                    @else: Unmark<CardsDrawnTriggeredEffectContext>(dear)),
                markFilter: dear),
        ]);
        var ended = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(hero, CardZone.Hand,
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>([Unmark<TurnEndedTriggeredEffectContext>(free), SetDelta<TurnEndedTriggeredEffectContext>(0, relative: false)]),
                markFilter: free),
            new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(hero, CardZone.Hand,
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>([Unmark<TurnEndedTriggeredEffectContext>(dear), SetDelta<TurnEndedTriggeredEffectContext>(0, relative: false)]),
                markFilter: dear),
        ]);
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var spent = new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(hero, played, free, remove: true),
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(hero, played, dear, remove: true),
        ]);
        return Rule(ClericalRuleId, "Clerical Error",
            "Run modifier: each turn one card in your hand costs 0 and another costs 1 more.",
            [
                Trigger(drawn, TriggerEvent.CardsDrawn),
                Trigger(ended, TriggerEvent.TurnEnded),
                Trigger(spent, TriggerEvent.CardPlayed),
            ]);
    }

    private static BnbRelic ClericalError() => Event(Prefix + "clerical_error", "Clerical Error", Source,
        "Run modifier. Each turn one random card in your hand costs 0 and another costs 1 more.", combatRule: ClericalRule);

    // Different, ±0: card rewards are face down, four instead of three.
    private static BnbRelic SealedEnvelopes() => new(Prefix + "sealed_envelopes", "Sealed Envelopes",
        "Run modifier. Card rewards are face down, but offer one card more.", Pool.Event, Rarity.Event, Source: Source,
        RewardRules:
        [
            new DrawMoreOffersRule(CardRewards, 1),
            new AppendOfferGrantRule(CardRewards, [], Count: 99, OfferTags: [RunEntityLabeler.FaceDownTag]),
        ]);

    // Different, ±0: no card rewards; instead every enemy that falls leaves a card behind with a chance of 1 in 4
    // (a pick of three, as a reward would have been). What it leaves is tagged so the rule lets it through, and
    // so is the Requisition Form's pick.
    public const string DroppedTag = "dropped";
    private const string FallenCounter = "mod_sacrifice_fallen";
    private const string RequisitionTag = "requisition";

    // Each act's own card pool (its rarity curve), for the cards these modifiers hand out: one program per act,
    // gated on the act the run is in.
    private static readonly int[] Acts = [1, 2, 3, 4, 5];
    // Handed over by the assembler once the act pools exist (BlueprintAssembler): building them asks for the relic
    // pools, so these two cannot build the act pools themselves. Before that they are listed without programs.
    private static IReadOnlyDictionary<int, ConversionPools>? _actPools;

    public static void UsePools(IReadOnlyDictionary<int, ConversionPools> pools) => _actPools = pools;

    private static IEnumerable<int> PooledActs => _actPools is { } pools ? Acts.Where(pools.ContainsKey) : [];

    private static ConversionPools PoolsOf(int act) => _actPools![act];

    private static IRunExpression<bool> InAct(int act) =>
        new RunComparisonExpression(new ActNumberExpression(), RunComparisonOperator.Equal, new RunConstantExpression(act));

    private static BnbRelic BureaucraticSacrifice() => new(Prefix + "bureaucratic_sacrifice", "Bureaucratic Sacrifice",
        "Run modifier. No card rewards; each enemy that falls leaves a card to choose with a chance of 1 in 4.", Pool.Event, Rarity.Event,
        Source: Source,
        RewardRules: [new DropOffersRule(new RewardMatch(Kind: RewardKinds.Card, NoneTag: [DroppedTag, RequisitionTag]), 0)],
        RunPrograms:
        [
            // How many fell is read off the event into a counter as the trigger fires (a template), and the roll
            // repeats over the counter when the queue drains, when the event is long gone.
            .. PooledActs.Select(act => RunPrograms.When<CombatResolvedRunEvent>(
                new AndExpression(new EventBoolValueExpression(RunEventFields.CombatVictory), InAct(act)),
                RunEffectTemplates.Literal(new SetCounterRunEffect(new RunCounterId(FallenCounter), 0)),
                RunEffectTemplates.ChangeCounter(new RunCounterId(FallenCounter), new EventIntValueExpression(RunEventFields.CombatFallenCount)),
                RunEffectTemplates.Literal(new RepeatRunEffect(new CounterValueExpression(new RunCounterId(FallenCounter)),
                    [new DrawEffectsRunEffect(new RunPool<IReadOnlyList<IRunEffectRequest>>(
                    [
                        new([new OfferRewardRunEffect(new RewardId("mod:dropped"), PoolsOf(act).CardRewardSource(), 1)
                            { Kind = RewardKinds.Card, Tags = [DroppedTag] }], 1),
                        new([], 3),
                    ]))])))),
        ]);

    // Good, −1: at the start of every act, a pick of ten cards from your own pool.
    private static BnbRelic RequisitionForm() => new(Prefix + "requisition_form", "Requisition Form",
        "Run modifier. At the start of every act, choose one of ten cards from your pool.", Pool.Event, Rarity.Event, Source: Source,
        RunPrograms:
        [
            .. PooledActs.Select(act => RunPrograms.When<ActStartedRunEvent>(InAct(act),
                new OfferRewardRunEffect(new RewardId("mod:requisition"), PoolsOf(act).CardRewardSource(10), 1)
                    { Kind = RewardKinds.Card, Tags = [RequisitionTag] })),
        ]);

    // Good, −1: every card reward may be drawn anew, once.
    private static BnbRelic SecondOpinion() => new(Prefix + "second_opinion", "Second Opinion",
        "Run modifier. Every card reward can be drawn anew once.", Pool.Event, Rarity.Event, Source: Source,
        RewardRules: [new RedrawOfferRule(CardRewards)]);

    private static ITriggeredRunEffectDefinition AtRunStart(params IRunEffectRequest[] effects) =>
        RunPrograms.On<RunStartedRunEvent>(effects);

    // Bad, +2: enemies start every fight with 1 Strength per act. One program per act, each gated on its act.
    private static BnbRelic Overtime() => Event(Prefix + "overtime", "Overtime", Source,
        "Run modifier. Enemies start every fight with 1 Strength per act.",
        runPrograms:
        [
            .. Enumerable.Range(1, 5).Select(act => RunPrograms.When<NodeEnteredRunEvent>(
                new AndExpression(
                    new EventBoolValueExpression(RunEventFields.NodeIsCombat),
                    new RunComparisonExpression(new ActNumberExpression(), RunComparisonOperator.Equal, new RunConstantExpression(act))),
                Openings.NextCombat(new CombatNodeModel("applyStatus", "allEnemies",
                    CombatAmountSpec.FromConst(act), StatusId: "strength")))),
        ]);

    // Bad, +1: the run opens with two curses in the deck (everyone starts with no gold already).
    public const string Curse = "tomb_curse";

    private static BnbRelic InheritedDebt() => Event(Prefix + "inherited_debt", "Inherited Debt", Source,
        "Run modifier. You started with two curses in your deck.",
        runPrograms: [AtRunStart(
            new AddCardToDeckRunEffect(new CardDefinitionId(Curse)),
            new AddCardToDeckRunEffect(new CardDefinitionId(Curse)))]);

    // Different, ±0: double damage dealt, half the max HP (taken off at the start, rounded so the hero keeps
    // the larger half).
    public const string GlassSealRuleId = "mod_glass_seal_rule";

    private static readonly StatusData GlassSealRule = Rule(GlassSealRuleId, "Glass Seal",
        "Run modifier: you deal double damage.", [],
        passives: [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, 200, RestrictDamageKind: null)]);

    private static BnbRelic GlassSeal() => Event(Prefix + "glass_seal", "Glass Seal", Source,
        "Run modifier. You deal double damage, but have half your max HP.",
        runPrograms: [AtRunStart(new RepeatRunEffect(
            new DivideExpression(new MaxHealthExpression(), new RunConstantExpression(2)),
            [new ChangeMaxHealthRunEffect(-1)]))],
        combatRule: GlassSealRule);

    // Good, −1: one random rare relic from the general pool, handed over as the run starts.
    private static BnbRelic SigningBonus(IEnumerable<BnbRelic> normalRelics)
    {
        var rares = normalRelics.Where(r => r.Rarity == Rarity.Rare && r.Eligibility == Eligibility.General).ToList();
        return Event(Prefix + "signing_bonus", "Signing Bonus", Source,
            "Run modifier. You started with a random rare relic.",
            runPrograms: [AtRunStart(new DrawEffectsRunEffect(new RunPool<IReadOnlyList<IRunEffectRequest>>(
                [.. rares.Select(r => new RunPool<IReadOnlyList<IRunEffectRequest>>.Entry(
                    [new AddRelicByIdRunEffect(new RelicId(r.Id)), .. r.Pickup ?? []], 1))])))]);
    }
}
