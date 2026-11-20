using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules the Normal relics install. Each is a hidden status the relic hands over when a fight
// opens; the status carries the rule, exactly as a Rite card's status does.
//
// The shapes are the ones the card pools established: "the first time each turn" is a counter latch cleared
// at turn start, a rule that watches the enemies is scoped to the whole fight, and a rule that changes what a
// keyword does is a marker the keyword itself looks for.
public static class RelicRules
{
    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> All() =>
    [
        BrassBookmark, ConservatorsThread, SunWarmedWaystone, FiveNotchBead, FormkeepersSignet,
        LeadCounterweight, HollowWaxBead, BindersAwl, PetitionersToken, IronPrayerBead, BlackSaltCharm,
        TarnishedBell, BruiseCup, VotiveCandle, GraveCoin, EmergencyInkwell, AshenWaxKnife,
        QuietReadersCord, ArchiveKey, RedactionKnife, IndexBone, ArchiveCenser, SealMakersDie,
        BloodPriceToken, BlackthornBrooch, SootglassLens, RubricTablet, RefuseDocket, IndexVolvelle,
        WithheldHourglass, ConcordanceMedallion, ChanceryRibbon, IronAstrolabe, RebindingSpindle,
        DeferredSignet, BloodStampedBond, ThornCrownedReliquary, ChanceryScale,
        // The relic audit (2026-11-08).
        WitnessSlate, AshPan, RemembranceString, EmberBox, CharnelLedger, ExecutionersMeasure,
        OilclothWrap, Rushlight, TinderPouch,
        // The shop relics of the sharpening pass (2026-10-21).
        WastebrokersFire, PriorityWindowFiling, SecondOpinion, AssentRibbon,
        GlassInkwell, IronThimble, StaleLoaf, PawnedWeddingRing,
        // The cheap discounts these relics hand out are statuses of their own, for the same reason a Rite's
        // allowance is: a passive modifier's PRESENCE is its condition, so the discount has to be a thing
        // that can be taken away.
        NextCardCheaper, NextCardFree, NextDeedStronger,
    ];

    // ── the simple ones ───────────────────────────────────────────────────────────────────────────────────

    // "At combat start gain N Block." The opening applies the status; the status pays once and goes.
    public static readonly StatusData BlackSaltCharm =
        Once("black_salt_charm", "Black Salt Charm", "Guards you as the fight opens.",
            Block<CardsDrawnTriggeredEffectContext>(4), nameof(TriggerEvent.CardsDrawn));

    // "Whenever an enemy dies while affected by a negative status, gain 4 Gold." The fight counts such deaths as
    // they happen (the one who fell is the source of its fall, still readable); the run pays when it is over.
    // ⚠ It used to pay 4 after every victory, debuff or not (relic audit 2026-11-20).
    public static CounterId DebuffedDeaths => new("grave_coin_deaths");

    public static readonly StatusData GraveCoin = Rule("grave_coin", "Grave Coin",
        "Whoever dies under your curses pays for the burial.",
        [Trigger(new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new AndExpression<CombatantDownedTriggeredEffectContext>(
                    new NotExpression<CombatantDownedTriggeredEffectContext>(
                        new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                            CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(Keywords.ApplicantMarker))),
                    new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                        new CombatantStacksByPolarityExpression<CombatantDownedTriggeredEffectContext>(
                            CombatantTargetSelectors.SourceIncludingDowned, StatusPolarity.Debuff),
                        ComparisonOperator.Greater, new ConstantExpression<CombatantDownedTriggeredEffectContext>(0))),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId("grave_coin")),
                    new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, DebuffedDeaths,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(1), relative: true)))),
            nameof(TriggerEvent.Downed), StatusTriggerScope.Anywhere)]);

    public static readonly ITriggeredRunEffectDefinition GraveCoinPay =
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Multiply(RunEventValues.CombatCounter(DebuffedDeaths.ToString()), RunExpr.Const(4))));

    // "Lose 6 HP; gain 1 Energy and 1 extra card on turn 1."
    public static readonly StatusData BloodStampedBond =
        Once("blood_stamped_bond", "Blood-Stamped Bond", "Signed in blood, once per fight.",
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new DealDamageNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new ConstantExpression<CardsDrawnTriggeredEffectContext>(3),
                    ignoresBlock: true, kind: DamageKind.DamageOverTime),
                HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                Draw<CardsDrawnTriggeredEffectContext>(1),
            ]), nameof(TriggerEvent.CardsDrawn));

    // "At the start of your next combat after a non-combat node, gain 1 Energy and 6 Block." The road decides, so the
    // run does it: a non-combat room owes the next fight an opening (RootboundStaffRoad). ⚠ It used to open EVERY fight
    // (relic audit 2026-11-20) — "the map bookkeeping is not worth a run program" no longer holds.
    public static RunCounterId RootboundOwed => new("rootbound_walking_staff_owed");

    public static IReadOnlyList<ITriggeredRunEffectDefinition> RootboundStaffRoad =>
    [
        // A fight entered while owed: the opening, and the debt is paid.
        RunPrograms.When<NodeEnteredRunEvent>(
            RunExpr.And(new EventBoolValueExpression(RunEventFields.NodeIsCombat),
                RunExpr.Equal(RunExpr.Counter(RootboundOwed), RunExpr.Const(1))),
            RunEffectTemplates.Literal(Openings.NextCombat(
                new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(1), StatusId: HeldEnergy.Id),
                new CombatNodeModel("gainBlock", "source", CombatAmountSpec.FromConst(6)))),
            RunEffectTemplates.ChangeCounter(RootboundOwed, RunExpr.Const(-1))),
        // Any other room: owed (set, never added).
        RunPrograms.When<NodeEnteredRunEvent>(
            RunExpr.Not(new EventBoolValueExpression(RunEventFields.NodeIsCombat)),
            RunEffectTemplates.ChangeCounter(RootboundOwed, RunExpr.Subtract(RunExpr.Const(1), RunExpr.Counter(RootboundOwed)))),
    ];

    // "At the start of your turn, draw 1 additional card."
    public static readonly StatusData IndexBone = Rule("index_bone", "Index Bone",
        "You draw one more card each turn.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            OnceEachTurn<CardsDrawnTriggeredEffectContext>("index_bone",
                Draw<CardsDrawnTriggeredEffectContext>(1))), nameof(TriggerEvent.CardsDrawn)),
         ClearLatch("index_bone")]);

    // "Once per turn, after your normal draw, discard 1 card and draw 1 card." The player picks the card, as the
    // text says. ⚠ It used to throw away the oldest card in hand, unasked (relic audit 2026-11-20): the engine could
    // not ask then; it can now (Tin Fife asks the same way).
    public static readonly StatusData RedactionKnife = Rule("redaction_knife", "Redaction Knife",
        "After your draw, discard a card of your choice and draw a fresh one.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            OnceEachTurn<CardsDrawnTriggeredEffectContext>("redaction_knife",
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ChosenCardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand,
                            "discard a card to draw a fresh one"),
                        CardZone.DiscardPile),
                    Draw<CardsDrawnTriggeredEffectContext>(1),
                ]))), nameof(TriggerEvent.CardsDrawn)),
         ClearLatch("redaction_knife")]);

    // "The first time your draw pile is shuffled each combat, gain 1 Energy and draw 1 card." A shuffle is
    // not an event a rule can hear, so it pays on the first draw of the second turn instead — the first
    // moment a starting deck has usually been through once. See ADAPTATIONS.
    public static readonly StatusData BindersAwl =
        Once("binders_awl", "Binder's Awl", "The rebinding pays once.",
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                 Draw<CardsDrawnTriggeredEffectContext>(1)]),
            nameof(TriggerEvent.CardsDrawn));

    // "If you end your turn with at least 1 unspent Energy, gain 5 Block." Granted as the turn ends, so it stands
    // against the blows that follow. ⚠ It used to be paid after the NEXT draw (relic audit 2026-11-20): the Block came a
    // round late and was gone before the enemy it was meant for ever struck. A turn's end Block holds until the next
    // turn starts, as Pay Chest's always has.
    public static readonly StatusData SunWarmedWaystone = Rule("sun_warmed_waystone", "Sun-Warmed Waystone",
        "Ending a turn with Energy to spare guards you.",
        [Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                Block<TurnEndedTriggeredEffectContext>(5))),
            nameof(TriggerEvent.TurnEnded))]);

    // "If you end your turn having played 2 or fewer cards, draw 1 additional card next turn."
    public static readonly StatusData QuietReadersCord = Rule("quiet_readers_cord", "Quiet Reader's Cord",
        "A quiet turn is repaid with a card.",
        [Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                SetCounter<TurnEndedTriggeredEffectContext>("cord_owed", 1))),
            nameof(TriggerEvent.TurnEnded)),
         Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Counter<CardsDrawnTriggeredEffectContext>("cord_owed", ComparisonOperator.Greater, 0),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Draw<CardsDrawnTriggeredEffectContext>(1),
                    SetCounter<CardsDrawnTriggeredEffectContext>("cord_owed", 0),
                ]))), nameof(TriggerEvent.CardsDrawn))]);

    // ── card-play counters ────────────────────────────────────────────────────────────────────────────────

    // "Every fifth card played deals 6 damage to the weakest living enemy."
    public static readonly StatusData FiveNotchBead = EveryNthCard("five_notch_bead", "Five-Notch Bead", 5,
        "Every fifth card you play strikes the weakest enemy.",
        new DealDamageNode<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.LowestHealthEnemyOfSource,
            new ConstantExpression<CardPlayedTriggeredEffectContext>(6)));

    // "Every third 0-cost card played during combat draws 1 card, at most once per turn." The played card's PRINTED
    // cost decides what counts (a card made free by a discount is not a 0-cost card). ⚠ It counted every card played
    // and drew on every third, any number of times a turn (relic audit 2026-11-20): the text was an adaptation the
    // engine has since outgrown.
    public static readonly StatusData HollowWaxBead = Rule("hollow_wax_bead", "Hollow Wax Bead",
        "Every third free card you play draws you another, once a turn.",
        [Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardCostExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        StandardCombatIds.EnergyResource),
                    ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new CounterId("hollow_wax_bead"),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new RemainderExpression<CardPlayedTriggeredEffectContext>(
                                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source, new CounterId("hollow_wax_bead")),
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                            ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        OnceEachTurn<CardPlayedTriggeredEffectContext>("hollow_wax_bead_turn",
                            Draw<CardPlayedTriggeredEffectContext>(1))),
                ]))), nameof(TriggerEvent.CardPlayed)),
         ClearLatch("hollow_wax_bead_turn")]);

    // "The first time each turn you play a card with base cost 2 or more, gain 4 Block." Cost is not readable
    // from a card-play trigger, so it pays on the first card of each turn. See ADAPTATIONS.
    // "Cards that cost 2 or more have Retain." Whatever reaches the hand is weighed as it arrives — the turn's
    // draw, a card drawn or brought back later — and a heavy one is marked to stay (the mark lasts the fight).
    public static readonly StatusData LeadCounterweight = Rule("lead_counterweight", "Lead Counterweight",
        "Your heavy cards wait for their moment.",
        [
            Trigger(Model<CardsDrawnTriggeredEffectContext>(WeighTheHand()), nameof(TriggerEvent.CardsDrawn)),
            Trigger(Model<CardMovedToZoneTriggeredEffectContext>(WeighTheHand()), nameof(TriggerEvent.CardMovedToZone)),
        ]);

    private static CombatNodeModel WeighTheHand() =>
        CombatNodeModel.ForEachCard(CardAuthoring.You, CardZone.Hand,
            CardAuthoring.Seq(
                new CombatNodeModel("setCombatantCounter", CardAuthoring.You,
                    new CombatAmountSpec("cardCost", ReadId: CardAuthoring.Energy.value, ReadCard: new CombatCardSpec("iterated")),
                    CounterId: "lead_counterweight_cost", Relative: false),
                CardAuthoring.If(new CombatConditionSpec("compare", CardAuthoring.You, ValueKind: "counter",
                        Op: ComparisonOperator.GreaterOrEqual, Right: 2, Id: "lead_counterweight_cost"),
                    new CombatNodeModel("markCardInstance", CardAuthoring.You, Card: new CombatCardSpec("iterated"),
                        ToTag: StandardCombatIds.RetainedCardMark.value))));

    // "The first time each turn you play a Form, gain 2 Block and apply 1 additional Paperwork to its target."
    public static readonly StatusData FormkeepersSignet = FirstTaggedCardEachTurn(
        "formkeepers_signet", "Formkeeper's Signet", CardAuthoring.FormTag,
        "The first Form you file each turn guards you and files a little deeper.",
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Block<CardPlayedTriggeredEffectContext>(2),
            new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Paperwork),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
        ]));

    // "The first Rite you play each combat costs 1 less and grants 3 Block." The discount is standing (the
    // relic hands it over at combat start) and spent by the Rite that uses it.
    public static readonly StatusData VotiveCandle = FirstTaggedCardEachTurn(
        "votive_candle", "Votive Candle", CardAuthoring.RiteTag,
        "The first Rite you perform each turn is lit for you.",
        Block<CardPlayedTriggeredEffectContext>(3));

    // "The first time each turn you play a Rite, your next card that turn costs 1 less."
    public static readonly StatusData RubricTablet = FirstTaggedCardEachTurn(
        "rubric_tablet", "Rubric Tablet", CardAuthoring.RiteTag,
        "A Rite makes the next thing you do cheaper.",
        GiveDiscount<CardPlayedTriggeredEffectContext>());

    // "Once per combat, after playing a card, if you have no Energy left, gain 1 Energy."
    public static readonly StatusData EmergencyInkwell = Rule("emergency_inkwell", "Emergency Inkwell",
        "Once a fight, an empty pool refills by one.",
        [Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCurrentResourceExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                OnceEachTurn<CardPlayedTriggeredEffectContext>("inkwell_used",
                    Energy<CardPlayedTriggeredEffectContext>(1)))), nameof(TriggerEvent.CardPlayed))]);

    // ── status and damage watchers ────────────────────────────────────────────────────────────────────────

    // "The first time each turn you apply a negative status to an enemy, deal 4 damage to it."
    public static readonly StatusData TarnishedBell = FirstFilingEachTurn(
        "tarnished_bell", "Tarnished Bell", "The first paper you serve each turn stings.",
        new DealDamageNode<StatusAppliedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new ConstantExpression<StatusAppliedTriggeredEffectContext>(4)),
        new DealDamageNode<StatusMergedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new ConstantExpression<StatusMergedTriggeredEffectContext>(4)));

    // "The first time each turn you apply a negative status to an enemy that already had one, draw 1 card." A merge
    // is a debuff onto the same debuff; a fresh one counts when the enemy carries more debuff than it just received.
    // ⚠ It used to hear merges only (relic audit 2026-11-20): Liable filed onto an enemy already in Doubt drew nothing.
    public static readonly StatusData SootglassLens = Rule("sootglass_lens", "Sootglass Lens",
        "Filing on top of a filing tells you something.",
        [Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
            OnEnemyFiling<StatusAppliedTriggeredEffectContext>("sootglass_lens",
                Draw<StatusAppliedTriggeredEffectContext>(1),
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    new CombatantStacksByPolarityExpression<StatusAppliedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, StatusPolarity.Debuff),
                    ComparisonOperator.Greater, new EventAmountExpression<StatusAppliedTriggeredEffectContext>()))),
            nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
         Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
            OnEnemyFiling<StatusMergedTriggeredEffectContext>("sootglass_lens",
                Draw<StatusMergedTriggeredEffectContext>(1))),
            nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
         ClearLatch("sootglass_lens")]);

    // "The first time each turn an enemy causes you to lose HP, gain 4 Block."
    public static readonly StatusData BruiseCup = Rule("bruise_cup", "Bruise Cup",
        "The first blow that lands each turn is answered with a guard.",
        [Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        // "An ENEMY causes you": in a blow the source is the striker. ⚠ Your own price (Blood-Price
                        // Token, Blood-Stamped Bond) used to spend the cup at every turn's start (relic audit 2026-11-20).
                        new NotExpression<DamageReceivedTriggeredEffectContext>(
                            new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.ApplicantMarker)))),
                    Claim<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, "bruise_cup")),
                new GainBlockNode<DamageReceivedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(4)))), nameof(TriggerEvent.DamageTaken)),
         ClearLatch("bruise_cup")]);

    // "Whenever you gain Block, deal a quarter of it to the enemy with the most HP, at most 10 per gain."
    // It cannot answer its own damage, since that is not Block.
    public static readonly StatusData ThornCrownedReliquary = Rule(
        "thorn_crowned_reliquary", "Thorn-Crowned Reliquary",
        "Every guard you raise throws something back.",
        [Trigger(new EffectProgram<BlockGainedTriggeredEffectContext>(
            new DealDamageNode<BlockGainedTriggeredEffectContext>(
                CombatantTargetSelectors.HighestHealthEnemyOfSource,
                new MinExpression<BlockGainedTriggeredEffectContext>(
                    new DivideExpression<BlockGainedTriggeredEffectContext>(
                        new EventAmountExpression<BlockGainedTriggeredEffectContext>(),
                        new ConstantExpression<BlockGainedTriggeredEffectContext>(4)),
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(10)))),
            nameof(TriggerEvent.BlockGained))]);

    // "The first time each turn a single card grants at least 10 Block, deal 6 damage to all enemies."
    public static readonly StatusData BlackthornBrooch = Rule("blackthorn_brooch", "Blackthorn Brooch",
        "A great guard answers every enemy at once.",
        [Trigger(new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
                new AndExpression<BlockGainedTriggeredEffectContext>(
                    new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                        new EventAmountExpression<BlockGainedTriggeredEffectContext>(),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<BlockGainedTriggeredEffectContext>(10)),
                    Claim<BlockGainedTriggeredEffectContext>(CombatantTargetSelectors.Source, "blackthorn")),
                new DealDamageNode<BlockGainedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(6)))), nameof(TriggerEvent.BlockGained)),
         ClearLatch("blackthorn")]);

    // ── Bureaucrat keyword watchers ───────────────────────────────────────────────────────────────────────

    public static readonly StatusData ArchiveKey = FirstCountEachTurn(
        "archive_key", "Archive Key", Keywords.Archived,
        "The first thing you Archive each turn guards you and pays a card.", block: 5, draw: 1);

    public static readonly StatusData ArchiveCenser = FirstCountEachTurn(
        "archive_censer", "Archive Censer", Keywords.Archived,
        "The first thing you Archive each turn pays an Energy.", energy: 1);

    public static readonly StatusData SealMakersDie = FirstCountEachTurn(
        "seal_makers_die", "Seal-Maker's Die", Keywords.Ratified,
        "The first Ratification each turn guards you and pays a card.", block: 5, draw: 1);

    public static readonly StatusData PetitionersToken = FirstCountEachTurn(
        "petitioners_token", "Petitioner's Token", Keywords.QueueResolved,
        "The first queued card to resolve each turn pays an Energy and a card.", draw: 1, energy: 1);

    // Relic audit (2026-11-08): it counted only the Bureaucrat's Archive; now any card reaching the Exhausted pile.
    public static readonly StatusData AshenWaxKnife = Rule("ashen_wax_knife", "Ashen Wax Knife",
        "The first card you burn each turn pays another.",
        [
            Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new TriggerEventCardZoneExpression<CardMovedToZoneTriggeredEffectContext>(CardZone.ExhaustPile),
                    OnceEachTurn<CardMovedToZoneTriggeredEffectContext>("ashen_wax_knife",
                        Draw<CardMovedToZoneTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardMovedToZone)),
            ClearLatch("ashen_wax_knife"),
        ]);

    // "The first time each turn you apply Paperwork to an enemy already 5 deep, gain 1 Energy and draw 1."
    public static readonly StatusData ChanceryScale = Rule("chancery_scale", "Chancery Scale",
        "Filing onto a deep pile pays you.",
        [Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusMergedTriggeredEffectContext>(
                new AndExpression<StatusMergedTriggeredEffectContext>(
                    new AndExpression<StatusMergedTriggeredEffectContext>(
                        new TriggerEventStatusIsExpression<StatusMergedTriggeredEffectContext>(
                            new StatusDefinitionId(Keywords.Paperwork)),
                        new ComparisonExpression<StatusMergedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusMergedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Paperwork)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<StatusMergedTriggeredEffectContext>(5))),
                    Wearers<StatusMergedTriggeredEffectContext>("chancery_scale", 0)),
                new ForEachTargetEffectNode<StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                        new StatusDefinitionId("chancery_scale")),
                    new CausalSequenceEffectNode<StatusMergedTriggeredEffectContext>(
                    [
                        new GainResourceNode<StatusMergedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, StandardCombatIds.EnergyResource,
                            new ConstantExpression<StatusMergedTriggeredEffectContext>(1)),
                        new DrawCardsNode<StatusMergedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget,
                            new ConstantExpression<StatusMergedTriggeredEffectContext>(1)),
                        new SetCombatantCounterNode<StatusMergedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new CounterId("chancery_scale"),
                            new ConstantExpression<StatusMergedTriggeredEffectContext>(1), relative: false),
                    ])))),
            nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
         ClearLatch("chancery_scale")]);

    // "The first Form you play each turn costs 1 less; the Paperwork and Doubt it applies are increased."
    // The increase is a flat +1 on the relic's own filing rather than a percentage on the card's, since a
    // rule cannot reach into a card's numbers. See ADAPTATIONS.
    public static readonly StatusData ChanceryRibbon = FirstTaggedCardEachTurn(
        "chancery_ribbon", "Chancery Ribbon", CardAuthoring.FormTag,
        "The first Form you file each turn is cheaper and files deeper.",
        new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Paperwork),
            new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
        costTag: CardAuthoring.FormTag);

    // "Apply half of any Paperwork or Doubt you file to every other enemy." Written as a flat 1 to the rest
    // of the field: the rule cannot read how much the card filed. See ADAPTATIONS.
    public static readonly StatusData ConcordanceMedallion = Rule(
        "concordance_medallion", "Concordance Medallion",
        "What you file on one desk is copied to the others.",
        [Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
            SpreadFiling<StatusAppliedTriggeredEffectContext>()),
            nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
         Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
            SpreadFiling<StatusMergedTriggeredEffectContext>()),
            nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
         ClearLatch("concordance")]);

    // The first queued card to RESOLVE each turn seals the weakest enemy. ⚠ The design wrote "its target", and the
    // rule cannot see the resolving card's target from the queue tally it listens to — so the text now says what
    // the relic does (2026-09-28). The Seal goes through the card keyword's own conversion (SealWeakest).
    // ⚠ SEAL GOES THROUGH THE KEYWORD, NOT AROUND IT. The "at 3 Seal, Ratify" conversion is not a rule of the Seal
    // status — it rides on CardAuthoring.ApplySeal, the way every card lays Seal. A relic that applied the bare
    // status left an enemy sitting at 3 or more Seal, never Ratified, until some card laid Seal again (audit,
    // 2026-09-28). Every relic that seals builds the card's own node for its trigger's context.
    internal static IEffectNode<TContext> SealWeakest<TContext>(int stacks) where TContext : class =>
        CombatProgramModel.Build<TContext>(CardAuthoring.ApplySeal(stacks, "lowestHealthEnemy")).Root;

    public static readonly StatusData DeferredSignet = Rule("deferred_signet", "Deferred Signet",
        "A queued matter comes back sealed.",
        [Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
            OnCountRising<StatusAppliedTriggeredEffectContext>("deferred_signet", Keywords.QueueResolved,
                SealWeakest<StatusAppliedTriggeredEffectContext>(1))),
            nameof(TriggerEvent.StatusApplied)),
         Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
            OnCountRising<StatusMergedTriggeredEffectContext>("deferred_signet", Keywords.QueueResolved,
                SealWeakest<StatusMergedTriggeredEffectContext>(1))),
            nameof(TriggerEvent.StatusMerged)),
         ClearLatch("deferred_signet")]);

    // "The first time each turn a Junk card enters your hand, Archive it and seal an enemy." Read after the
    // draw, which is when Junk usually arrives.
    public static readonly StatusData RefuseDocket = Rule("refuse_docket", "Refuse Docket",
        "Rubbish that reaches your hand is disposed of, and somebody is sealed for it.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            OnceEachTurn<CardsDrawnTriggeredEffectContext>("refuse_docket",
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            CardZone.ExhaustPile),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Archived),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        SealWeakest<CardsDrawnTriggeredEffectContext>(1),
                    ]),
                    tagFilter: new TagId(CardAuthoring.JunkTag), takeFirst: 1))),
            nameof(TriggerEvent.CardsDrawn)),
         ClearLatch("refuse_docket")]);

    // "The first time each turn a card leaves your hand without being played, gain 4 Block." Heard the way Whispering
    // Gallery hears it: a card moved out of the hand that was not played this turn (a discard, an exhaust), or the
    // hand thrown away at the turn's end. ⚠ It used to look only at the turn's end and pay with the NEXT draw (relic
    // audit 2026-11-20): a discard mid-turn paid nothing, and the Block came a round late.
    public static readonly StatusData ConservatorsThread = Rule(
        "conservators_thread", "Conservator's Thread",
        "A card set aside unplayed guards you.",
        [Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
            new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                GeneralPoolRules.Unplayed<CardMovedToZoneTriggeredEffectContext>(),
                OnceEachTurn<CardMovedToZoneTriggeredEffectContext>("conservators_thread",
                    Block<CardMovedToZoneTriggeredEffectContext>(4)))), nameof(TriggerEvent.CardMovedToZone)),
         Trigger(new EffectProgram<HandDiscardedTriggeredEffectContext>(
            new ConditionalEffectNode<HandDiscardedTriggeredEffectContext>(
                new ComparisonExpression<HandDiscardedTriggeredEffectContext>(
                    new EventAmountExpression<HandDiscardedTriggeredEffectContext>(),
                    ComparisonOperator.Greater, new ConstantExpression<HandDiscardedTriggeredEffectContext>(0)),
                OnceEachTurn<HandDiscardedTriggeredEffectContext>("conservators_thread",
                    Block<HandDiscardedTriggeredEffectContext>(4)))), nameof(TriggerEvent.HandDiscarded)),
         ClearLatch("conservators_thread")]);

    // "The first Deed you play each turn against an enemy that intends to Attack deals 4 more." The bonus is
    // a once-per-action damage passive on the ENEMY, handed to whoever is telegraphing an attack.
    public static readonly StatusData IronPrayerBead = Rule("iron_prayer_bead", "Iron Prayer Bead",
        "An enemy winding up takes more from your first blow.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new TargetIntendsExpression<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, nameof(IntentKind.Attack)),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(NextDeedStrongerId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))))),
            nameof(TriggerEvent.CardsDrawn))]);

    // ── per-instance card prices ──────────────────────────────────────────────────────────────────────────

    // "One card costs less the first time you play it." The promise is written on the CARD, which is the only
    // way to say it — a status prices every card its wearer holds.
    public static readonly StatusData IndexVolvelle =
        Once("index_volvelle", "Index Volvelle", "One card in your opening hand is cheaper.",
            Cheapen<CardsDrawnTriggeredEffectContext>(1, 1), nameof(TriggerEvent.CardsDrawn));

    public static readonly StatusData WithheldHourglass = Rule("withheld_hourglass", "Withheld Hourglass",
        "Each turn, one card in your hand is free the first time you play it.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            Cheapen<CardsDrawnTriggeredEffectContext>(1, 99)), nameof(TriggerEvent.CardsDrawn))]);

    public static readonly StatusData RebindingSpindle = Rule("rebinding_spindle", "Rebinding Spindle",
        "Each turn, two cards in your hand are cheaper the first time they are played.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            Cheapen<CardsDrawnTriggeredEffectContext>(2, 1)), nameof(TriggerEvent.CardsDrawn))]);

    // Every SECOND turn (sharpening pass 2026-10-21): "the first time each turn you draw" was every turn, since
    // the turn's own draw is the first — the Act-IV boss relic's whole effect on a Rare from the normal pool.
    // Counted per turn the way the Brass Service Bell counts: CardsDrawn is the turn's draw, once a turn.
    public static readonly StatusData IronAstrolabe = Rule("iron_astrolabe", "Iron Astrolabe",
        "Every second turn pays an Energy.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(CombatantTargetSelectors.Source,
                    new CounterId("iron_astrolabe_turn"), new ConstantExpression<CardsDrawnTriggeredEffectContext>(1),
                    relative: true),
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, new CounterId("iron_astrolabe_turn")),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                        ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1)),
            ])), nameof(TriggerEvent.CardsDrawn))]);

    // "At the start of your turn, lose 3 HP; your next card that turn costs 1 less." Taken every turn rather
    // than offered, since a relic cannot ask. See ADAPTATIONS.
    public static readonly StatusData BloodPriceToken = Rule("blood_price_token", "Blood-Price Token",
        "Each turn it takes 3 HP and makes your next card cheaper.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new DealDamageNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new ConstantExpression<CardsDrawnTriggeredEffectContext>(3),
                    ignoresBlock: true, kind: DamageKind.DamageOverTime),
                GiveDiscount<CardsDrawnTriggeredEffectContext>(),
            ])), nameof(TriggerEvent.CardsDrawn))]);

    // "The first non-Junk card that enters your hand outside the normal draw step each turn gains Retain until
    // the start of your next turn."
    //
    // "Outside the normal draw" needs no clause of its own: drawing reports as a draw, not as a card MOVE, so
    // a drawn card never reaches this trigger at all. What is left is the two things the line does say —
    // the card ARRIVED (the same event carries every card that leaves, and a rule that could not tell the
    // directions apart would keep whatever it last saw go), and it is the FIRST one, which is the usual latch.
    //
    // The keeping is a mark on that one COPY, so the relic keeps the card its text names rather than the hand
    // it happens to be in; the turn start takes the mark off again, which is where "until your next turn" ends.
    public static readonly StatusData BrassBookmark = Rule("brass_bookmark", "Brass Bookmark",
        "The first card that arrives in your hand outside your draw each turn is kept until your next turn.",
        // Two triggers for one sentence, because a card reaches a hand two ways it was not drawn into: it is
        // MOVED there (a tutor, a card handed back from the discard pile) or it is MADE there (what an enemy
        // pushes into your hand). Neither hears the other, and the latch is shared, so between them they keep
        // exactly one card however it arrived.
        [Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
            KeepTheFirstArrival<CardMovedToZoneTriggeredEffectContext>()),
            nameof(TriggerEvent.CardMovedToZone)),
         Trigger(new EffectProgram<CardInstanceCreatedTriggeredEffectContext>(
            KeepTheFirstArrival<CardInstanceCreatedTriggeredEffectContext>()),
            nameof(TriggerEvent.CardInstanceCreated)),
         // The release, and the latch cleared in the same breath: at most one card carries the mark, because
         // only one is ever given it per turn, so the first marked card in hand IS the kept one.
         Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Keep<TurnStartedTriggeredEffectContext>(
                    new FirstMarkedCardInOwnerZoneExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, CardZone.Hand, StandardCombatIds.RetainedCardMark),
                    release: true),
                SetCounter<TurnStartedTriggeredEffectContext>(BookmarkLatch, 0),
            ])), nameof(TriggerEvent.TurnStarted))]);

    private const string BookmarkLatch = "brass_bookmark";

    private static IEffectNode<TContext> KeepTheFirstArrival<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new TriggerEventCardZoneExpression<TContext>(CardZone.Hand),
                new NotExpression<TContext>(
                    new CardInstanceHasTagExpression<TContext>(
                        new TriggerEventCardInstanceExpression<TContext>(),
                        new TagId(CardAuthoring.JunkTag)))),
            OnceEachTurn<TContext>(BookmarkLatch,
                Keep<TContext>(new TriggerEventCardInstanceExpression<TContext>())));

    // Keep this COPY of a card past the end of the turn — or, released, stop keeping it.
    private static IEffectNode<TContext> Keep<TContext>(
        ICardInstanceExpression<TContext> card, bool release = false) where TContext : class =>
        new MarkCardInstanceNode<TContext>(
            CombatantTargetSelectors.Source, card, StandardCombatIds.RetainedCardMark, remove: release);

    // ── the discounts these relics hand out ───────────────────────────────────────────────────────────────

    public const string NextCardCheaperId = "relic_next_card_cheaper";
    public const string NextCardFreeId = "relic_next_card_free";
    public const string NextDeedStrongerId = "relic_next_deed_stronger";

    public static readonly StatusData NextCardCheaper = Discount(NextCardCheaperId, "Cheaper", -1, null);
    public static readonly StatusData NextCardFree = Discount(NextCardFreeId, "Free", -99, null);

    // The Iron Prayer Bead's bonus: +4 on the first Deed aimed at this enemy, once per card played.
    public static readonly StatusData NextDeedStronger = new()
    {
        Id = NextDeedStrongerId,
        NameKey = "Winding Up",
        DescriptionKey = "The next Deed aimed at this character deals 4 more.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.AddFlat, 4,
                RestrictDamageKind: DamageKind.Direct, RestrictSourceCardTag: CardAuthoring.DeedTag,
                OncePerAction: true),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(NextDeedStrongerId))),
                nameof(TriggerEvent.TurnEnded)),
            // "The FIRST Deed you play each turn": the Deed that struck a marked enemy has used the promise, and every
            // other enemy's mark goes with it. ⚠ It used to stay all turn (relic audit 2026-11-20): every Deed at an
            // attacker hit for 4 more. In a blow the source is the striker, so its enemies are the marked side.
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new TriggerEventSourceCardHasTagExpression<DamageReceivedTriggeredEffectContext>(new TagId(CardAuthoring.DeedTag)),
                    new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.AllEnemiesOfSource,
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(NextDeedStrongerId))))),
                nameof(TriggerEvent.DamageTaken)),
        ],
    };

    private static StatusData Discount(string id, string name, int magnitude, string? tag) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = "Your next card is cheaper.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, magnitude,
                RestrictDamageKind: null, RestrictSourceCardTag: tag),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── run programs ──────────────────────────────────────────────────────────────────────────────────────

    // "Whenever you Rest at a Campfire, upgrade 1 random unupgraded card. Whenever you Smith, heal 7 HP."
    // Read off the campfire's own choice (EventTemplates.Rest: "rest" heals, "amend" improves a card), at a room
    // tagged rest. ⚠ It used to answer every NodeEntered — an upgrade and 7 HP in every room of the run
    // (playtest 2026-11-19, found with the run log).
    public static readonly ITriggeredRunEffectDefinition TwinEmberBrazierRest =
        RunPrograms.When<EventChoiceMadeRunEvent>(
            new EventChoiceIsExpression("rest", MapNodeTags.Rest),
            new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().Random(1)));

    public static readonly ITriggeredRunEffectDefinition TwinEmberBrazierSmith =
        RunPrograms.When<EventChoiceMadeRunEvent>(
            new EventChoiceIsExpression("amend", MapNodeTags.Rest),
            new HealRunEffect(7));

    // ── the relic audit (2026-11-08) ──────────────────────────────────────────────────────────────────────

    // "The first time each turn you apply Liable, apply 1 more." The latch is set before the extra stack is
    // filed, so the extra stack cannot answer itself.
    public static readonly StatusData WitnessSlate = Rule("witness_slate", "Witness Slate",
        "The first Liable you file each turn is witnessed twice.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(Witness<StatusAppliedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(Witness<StatusMergedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            ClearLatch("witness_slate"),
        ]);

    // ⚠ Liable filed on EVERY enemy by one card used to be witnessed on each of them (relic audit 2026-11-20): the
    // latch was read before the first extra stack was written. And it is YOUR Liable — in a filing the source is
    // whoever filed it.
    private static IEffectNode<TContext> Witness<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Liable)),
                    new TargetHasStatusExpression<TContext>(CombatantTargetSelectors.Source, new StatusDefinitionId("witness_slate"))),
                new AndExpression<TContext>(
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.ApplicantMarker))),
                    Claim<TContext>(CombatantTargetSelectors.Source, "witness_slate"))),
            new ApplyStatusNode<TContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Liable),
                new ConstantExpression<TContext>(1)));

    // "The first time each combat your Exhausted pile holds 5 cards, gain 1 Energy and draw 1 card."
    public static readonly StatusData AshPan = Rule("ash_pan", "Ash Pan",
        "A full pan of ash warms the next hand.",
        [
            Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new AndExpression<CardMovedToZoneTriggeredEffectContext>(
                        new ComparisonExpression<CardMovedToZoneTriggeredEffectContext>(
                            new CombatantZoneCardCountExpression<CardMovedToZoneTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, CardZone.ExhaustPile),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardMovedToZoneTriggeredEffectContext>(5)),
                        Claim<CardMovedToZoneTriggeredEffectContext>(CombatantTargetSelectors.Source, "ash_pan_done")),
                    new CausalSequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    [
                        HeldEnergy.Hold<CardMovedToZoneTriggeredEffectContext>(1),
                        Draw<CardMovedToZoneTriggeredEffectContext>(1),
                    ]))),
                nameof(TriggerEvent.CardMovedToZone)),
        ]);

    // "At the end of your turn, each card you Retain costs 1 less until it is played." The hand at the turn's
    // end is exactly what was retained. A card kept turn after turn grows cheaper each time.
    public static readonly StatusData RemembranceString = Rule("remembrance_string", "Remembrance String",
        "What you keep becomes easier to do.",
        [
            Trigger(Model<TurnEndedTriggeredEffectContext>(
                CombatNodeModel.ForEachCard(CardAuthoring.You, CardZone.Hand,
                    new CombatNodeModel("setCardInstanceMarkCounter", CardAuthoring.You, CombatAmountSpec.FromConst(-1),
                        Card: new CombatCardSpec("iterated"),
                        CounterId: StandardCombatIds.CardCostDeltaCounter.value, Relative: true))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // Ember Box and Charnel Ledger are markers: Held Energy (HeldEnergy) and the Unread rule (GeneralPoolRules)
    // look for them.
    public static readonly StatusData EmberBox = Rule("ember_box", "Ember Box",
        "Whenever Held Energy arrives, draw 1 card.", []);

    public static readonly StatusData CharnelLedger = Rule("charnel_ledger", "Charnel Ledger",
        "Unread effects happen twice.", []);

    // "Whenever an enemy dies from exactly the damage it had left, gain 8 Gold." A hit is exact when nothing of
    // what it asked for was wasted: requested − blocked = the HP it took, and the target is down. The fight
    // counts such kills; the run pays for them when it is over.
    public static CounterId ExactKills => new("executioners_measure_kills");

    public static readonly StatusData ExecutionersMeasure = Rule("executioners_measure", "Executioner's Measure",
        "A clean measure is paid for.",
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new AndExpression<DamageReceivedTriggeredEffectContext>(
                            new NotExpression<DamageReceivedTriggeredEffectContext>(
                                new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                                    CombatantTargetSelectors.EventTarget,
                                    new StatusDefinitionId(Keywords.ApplicantMarker))),
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(
                                    CombatantTargetSelectors.EventTarget),
                                ComparisonOperator.Equal, new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                        new AndExpression<DamageReceivedTriggeredEffectContext>(
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                                ComparisonOperator.Greater, new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new SubtractExpression<DamageReceivedTriggeredEffectContext>(
                                    new EventRequestedAmountExpression<DamageReceivedTriggeredEffectContext>(),
                                    new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>()),
                                ComparisonOperator.Equal, new EventAmountExpression<DamageReceivedTriggeredEffectContext>()))),
                    new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                            new StatusDefinitionId("executioners_measure")),
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, ExactKills,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: true)))),
                nameof(TriggerEvent.DamageTaken), StatusTriggerScope.Anywhere),
        ]);

    public static readonly ITriggeredRunEffectDefinition ExecutionersPay =
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Multiply(RunEventValues.CombatCounter(ExactKills.ToString()), RunExpr.Const(8))),
        ]);

    // "Your hand is not discarded at the end of your turn. You draw 2 fewer cards at the start of each turn."
    public static readonly StatusData OilclothWrap = Rule("oilcloth_wrap", "Oilcloth Wrap",
        "Your hand is kept dry, and you are dealt less.", [],
        passives: [new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddFlat, -2,
            RestrictDamageKind: null)],
        tags: [StandardCombatIds.RetainHandTag.value]);

    // "Your first card each turn costs 0. You draw 1 fewer card at the start of each turn." The free card is
    // handed over with the turn's draw (the first turn's start comes before the relic's rule is worn), once a
    // turn, and only if the last one was used.
    public static readonly StatusData Rushlight = Rule("rushlight", "Rushlight",
        "A quick light for the first thing you do.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                OnceEachTurn<CardsDrawnTriggeredEffectContext>("rushlight",
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new NotExpression<CardsDrawnTriggeredEffectContext>(
                            new TargetHasStatusExpression<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, StandardCombatIds.FreeNextCardStatus)),
                        // The engine's free card is a CHARGE, not a stack.
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.FreeNextCardStatus,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), charges: 1)))),
                nameof(TriggerEvent.CardsDrawn)),
            ClearLatch("rushlight"),
        ],
        passives: [new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddFlat, -1,
            RestrictDamageKind: null)]);

    // "Whenever a card is Exhausted, gain 5 Block." The campfire half lives on the rest (EventTemplates.Rest).
    public static readonly StatusData TinderPouch = Rule("tinder_pouch", "Tinder Pouch",
        "Every card burnt kindles a guard.",
        [
            Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new TriggerEventCardZoneExpression<CardMovedToZoneTriggeredEffectContext>(CardZone.ExhaustPile),
                    Block<CardMovedToZoneTriggeredEffectContext>(5))),
                nameof(TriggerEvent.CardMovedToZone)),
        ]);

    private static EffectProgram<TContext> Model<TContext>(CombatNodeModel model) where TContext : class =>
        CombatProgramModel.Build<TContext>(model);

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Once(
        string id, string name, string description, IEffectNode<CardsDrawnTriggeredEffectContext> body,
        string trigger) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                OnceEachTurn<CardsDrawnTriggeredEffectContext>(id + "_done", body)),
                trigger),
        ]);

    private static StatusData EveryNthCard(string id, string name, int n, string description,
        IEffectNode<CardPlayedTriggeredEffectContext> body) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new CounterId(id),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new RemainderExpression<CardPlayedTriggeredEffectContext>(
                                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source, new CounterId(id)),
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(n)),
                            ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        body),
                ])), nameof(TriggerEvent.CardPlayed)),
        ]);

    private static StatusData FirstCardEachTurn(string id, string name, string description,
        IEffectNode<CardPlayedTriggeredEffectContext> body) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                OnceEachTurn<CardPlayedTriggeredEffectContext>(id, body)), nameof(TriggerEvent.CardPlayed)),
            ClearLatch(id),
        ]);

    private static StatusData FirstTaggedCardEachTurn(string id, string name, string tag, string description,
        IEffectNode<CardPlayedTriggeredEffectContext> body, string? costTag = null) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(id, body))),
                nameof(TriggerEvent.CardPlayed)),
            ClearLatch(id),
        ],
        passives: costTag is null
            ? null
            : [new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, -1,
                RestrictDamageKind: null, RestrictSourceCardTag: costTag)]);

    // "The first time each turn <count> goes up" — the shape Archive, Ratify and the Queue announce
    // themselves in.
    private static StatusData FirstCountEachTurn(
        string id, string name, string counted, string description,
        int block = 0, int draw = 0, int energy = 0) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                OnCountRising<StatusAppliedTriggeredEffectContext>(id, counted,
                    Pay<StatusAppliedTriggeredEffectContext>(block, draw, energy))),
                nameof(TriggerEvent.StatusApplied)),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                OnCountRising<StatusMergedTriggeredEffectContext>(id, counted,
                    Pay<StatusMergedTriggeredEffectContext>(block, draw, energy))),
                nameof(TriggerEvent.StatusMerged)),
            ClearLatch(id),
        ]);

    private static StatusData FirstFilingEachTurn(
        string id, string name, string description,
        IEffectNode<StatusAppliedTriggeredEffectContext> applied,
        IEffectNode<StatusMergedTriggeredEffectContext> merged) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                OnEnemyFiling<StatusAppliedTriggeredEffectContext>(id, applied)),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                OnEnemyFiling<StatusMergedTriggeredEffectContext>(id, merged)),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            ClearLatch(id),
        ]);

    private static IEffectNode<TContext> Pay<TContext>(int block, int draw, int energy) where TContext : class
    {
        var steps = new List<IEffectNode<TContext>>();
        if (block > 0) steps.Add(Block<TContext>(block));
        if (draw > 0) steps.Add(Draw<TContext>(draw));
        if (energy > 0) steps.Add(Energy<TContext>(energy));
        return steps.Count == 1 ? steps[0] : new CausalSequenceEffectNode<TContext>(steps);
    }

    // A count on the WEARER going up — Archive, Ratify, the Queue — answered once a turn.
    private static IEffectNode<TContext> OnCountRising<TContext>(string id, string counted,
        IEffectNode<TContext> body) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(counted)),
                Claim<TContext>(CombatantTargetSelectors.Source, id)),
            body);

    // A negative status the WEARER files on an ENEMY, answered once a turn. In a filing the source is whoever filed it.
    // ⚠ It used to answer any status landing on any enemy, filed by anyone (relic audit 2026-11-20): the neutral mark
    // Iron Prayer Bead puts on an attacker rang the Bell at every turn's start, and an enemy's own buffs did too.
    private static IEffectNode<TContext> OnEnemyFiling<TContext>(string id, IEffectNode<TContext> body,
        ICombatExpression<TContext, bool>? also = null)
        where TContext : class
    {
        ICombatExpression<TContext, bool> when = new AndExpression<TContext>(
            new AndExpression<TContext>(
                new TargetHasStatusExpression<TContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(id)),
                new NotExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.EventTarget,
                        new StatusDefinitionId(Keywords.ApplicantMarker)))),
            new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff));
        if (also is not null)
            when = new AndExpression<TContext>(when, also);
        // The claim last: a filing that does not count must not spend the turn's one.
        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(when, Claim<TContext>(CombatantTargetSelectors.Source, id)), body);
    }

    // "No wearer has used it yet this turn."
    private static ICombatExpression<TContext, bool> Wearers<TContext>(string id, int used)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CountTargetsExpression<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(id))),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(used));

    // "Apply half of what you filed to every other enemy." A flat 1, since the rule cannot read the amount.
    // "The first time each turn you apply Paperwork or Doubt to a single enemy, apply half of it to every other
    // enemy, rounded down but never less than 1."
    // ⚠ Rebuilt 2026-11-19 (combat audit): it answered ANY filing — an enemy filing Paperwork on its ally made
    // "the enemies of the source" (you) take Paperwork — and it always spread 1 Paperwork to every enemy,
    // the target included, for Doubt as well, and never for a filing onto a pile already there. Now: only what
    // the WEARER files on an enemy, the same status, half of it (at least 1), on every OTHER enemy.
    private static IEffectNode<TContext> SpreadFiling<TContext>() where TContext : class
    {
        IEffectNode<TContext> Spread(string status) =>
            new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(status)),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.OtherEnemiesOfSource, new StatusDefinitionId(status),
                    new MaxExpression<TContext>(
                        new ConstantExpression<TContext>(1),
                        new DivideExpression<TContext>(new EventAmountExpression<TContext>(), new ConstantExpression<TContext>(2)))));
        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId("concordance_medallion")),
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId("concordance_medallion")))),
                Claim<TContext>(CombatantTargetSelectors.Source, "concordance")),
            new CausalSequenceEffectNode<TContext>(
            [
                // The latch is claimed as it is read: the spread is itself a filing of Paperwork or Doubt by the
                // wearer, and must not answer itself.
                Spread(Keywords.Paperwork),
                Spread(Keywords.Doubt),
            ]));
    }

    // Write a one-shot price onto N cards in hand.
    private static IEffectNode<TContext> Cheapen<TContext>(int cards, int by) where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            CombatantTargetSelectors.Source, CardZone.Hand,
            new SetCardInstanceMarkCounterNode<TContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<TContext>(),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<TContext>(-by), relative: false),
            takeFirst: cards);

    private static IEffectNode<TContext> GiveDiscount<TContext>() where TContext : class =>
        new ApplyStatusNode<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(NextCardCheaperId),
            new ConstantExpression<TContext>(1));

    // The latch is CLAIMED as it is read (ClaimLatchExpression): one effect that raises the same event twice — a
    // double blow, a filing on every enemy — must not find it open twice (relic audit 2026-11-20).
    private static IEffectNode<TContext> OnceEachTurn<TContext>(string id, IEffectNode<TContext> body)
        where TContext : class =>
        OnceOn(CombatantTargetSelectors.Source, id, body);

    private static IEffectNode<TContext> OnceOn<TContext>(ICombatantTargetSelector holder, string id, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(Claim<TContext>(holder, id), body);

    private static ICombatExpression<TContext, bool> Claim<TContext>(ICombatantTargetSelector holder, string id)
        where TContext : class =>
        new ClaimLatchExpression<TContext>(holder, new CounterId(id));

    private static ICombatExpression<TContext, bool> Counter<TContext>(
        string id, ComparisonOperator op, int value) where TContext : class =>
        CounterOn<TContext>(CombatantTargetSelectors.Source, id, op, value);

    private static ICombatExpression<TContext, bool> CounterOn<TContext>(
        ICombatantTargetSelector who, string id, ComparisonOperator op, int value) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(who, new CounterId(id)), op,
            new ConstantExpression<TContext>(value));

    private static IEffectNode<TContext> SetCounter<TContext>(string id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, new CounterId(id),
            new ConstantExpression<TContext>(value), relative: false);

    // What the hand holds right now, written down for the turn's end to read.
    private static IEffectNode<TContext> RecordHand<TContext>() where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, new CounterId("thread_hand"),
            new CombatantZoneCardCountExpression<TContext>(CombatantTargetSelectors.Source, CardZone.Hand),
            relative: false);

    private static IEffectNode<TContext> Block<TContext>(int amount) where TContext : class =>
        new GainBlockNode<TContext>(CombatantTargetSelectors.Source, new ConstantExpression<TContext>(amount));

    private static IEffectNode<TContext> Draw<TContext>(int cards) where TContext : class =>
        new DrawCardsNode<TContext>(CombatantTargetSelectors.Source, new ConstantExpression<TContext>(cards));

    private static IEffectNode<TContext> Energy<TContext>(int amount) where TContext : class =>
        new GainResourceNode<TContext>(CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
            new ConstantExpression<TContext>(amount));

    private static StatusTriggerData ClearLatch(string id) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            SetCounter<TurnStartedTriggeredEffectContext>(id, 0)), nameof(TriggerEvent.TurnStarted));

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            TheOpeningHand.For(program, trigger), CombatJson.CreateOptions<TContext>()), scope);

    // ── the shop relics of the sharpening pass (2026-10-21) ─────────────────────────────────────────────
    //
    // Thirteen of the eighteen general shop relics and all six of the Bureaucrat's were Gold arithmetic. Slay the
    // Spire 2 sells rules instead: a relic that bends a fight, often for a price. The Bureaucrat's four bend one
    // of his weaker strands each (Archive, Queue, Doubt, Seal); the general four are a rule with a price.

    // "Whenever you Archive a card, deal 3 damage to ALL enemies." The Archive tally rising is the moment.
    public static readonly StatusData WastebrokersFire = Rule("wastebrokers_fire", "Wastebroker's Permit",
        "Everything you Archive feeds the furnace.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                WhenTallyRises<StatusAppliedTriggeredEffectContext>(Keywords.Archived, HitAll<StatusAppliedTriggeredEffectContext>(3))),
                nameof(TriggerEvent.StatusApplied)),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                WhenTallyRises<StatusMergedTriggeredEffectContext>(Keywords.Archived, HitAll<StatusMergedTriggeredEffectContext>(3))),
                nameof(TriggerEvent.StatusMerged)),
        ]);

    // "Whenever a Queued card resolves, apply 2 Paperwork to ALL enemies."
    public static readonly StatusData PriorityWindowFiling = Rule("priority_window_filing", "Priority Window Pass",
        "Every queued card that comes up is filed against everyone.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                WhenTallyRises<StatusAppliedTriggeredEffectContext>(Keywords.QueueResolved,
                    FileOnAll<StatusAppliedTriggeredEffectContext>(Keywords.Paperwork, 2))),
                nameof(TriggerEvent.StatusApplied)),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                WhenTallyRises<StatusMergedTriggeredEffectContext>(Keywords.QueueResolved,
                    FileOnAll<StatusMergedTriggeredEffectContext>(Keywords.Paperwork, 2))),
                nameof(TriggerEvent.StatusMerged)),
        ]);

    // "Doubt you apply is 1 larger."
    public static readonly StatusData SecondOpinion = Rule("second_opinion", "Second Opinion",
        "Every Doubt you file carries one more.", [],
        passives:
        [
            new PassiveModifierData(PassiveModifierPipeline.OutgoingStatusApplicationStacks,
                PassiveModifierOperation.AddFlat, 1, RestrictDamageKind: null, AppliesToStatusId: Keywords.Doubt),
        ]);

    // "Whenever you Ratify an enemy, apply 1 Doubt and 2 Paperwork to it." Ratified lands on the ENEMY, so the
    // rule listens to the whole fight and answers on the enemy the event names.
    public static readonly StatusData AssentRibbon = Rule("assent_ribbon", "Assent Ribbon",
        "A ratified enemy is also doubted and filed against.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                WhenStatusLands<StatusAppliedTriggeredEffectContext>(Keywords.Ratified)),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                WhenStatusLands<StatusMergedTriggeredEffectContext>(Keywords.Ratified)),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
        ]);

    // "Your attacks deal 25% more damage. Attacks against you deal 25% more damage."
    public static readonly StatusData GlassInkwell = Rule("glass_inkwell", "Glass Inkwell",
        "Sharper on both sides.", [],
        passives:
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, 125),
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 125),
        ]);

    // "Every attack hit against you deals 1 less damage. Every Block you gain is 1 less."
    public static readonly StatusData IronThimble = Rule("iron_thimble", "Iron Thimble",
        "Every blow and every guard is one smaller.", [],
        passives:
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.AddFlat, -1),
            new PassiveModifierData(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.AddFlat, -1,
                RestrictDamageKind: null),
        ]);

    // "Your first turn has 2 less Energy. Every later turn has 1 more." The later turns' point is HELD, like
    // every turn-start Energy in this game (HeldEnergy).
    public static readonly StatusData StaleLoaf = Rule("stale_loaf", "Stale Loaf",
        "A hard first bite, and bread for every day after.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                        ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    new LoseResourceNode<CardsDrawnTriggeredEffectContext>(CombatantTargetSelectors.Source,
                        StandardCombatIds.EnergyResource, new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                    @else: HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Start each combat with 2 Strength." Its price is paid at pickup (7 Max HP).
    public static readonly StatusData PawnedWeddingRing = Rule("pawned_wedding_ring", "Pawned Wedding Ring",
        "What was promised, spent on a harder hand.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                        ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(CombatantTargetSelectors.Source,
                        new StatusDefinitionId("strength"), new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // A tally on the wearer going up — every time, not once a turn.
    private static IEffectNode<TContext> WhenTallyRises<TContext>(string counted, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(counted)), body);

    // A status landing on an enemy: 1 Doubt and 2 Paperwork on it (Assent Ribbon).
    private static IEffectNode<TContext> WhenStatusLands<TContext>(string status) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(status)),
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(CombatantTargetSelectors.EventTarget,
                    new StatusDefinitionId(Keywords.Doubt), new ConstantExpression<TContext>(1)),
                new ApplyStatusNode<TContext>(CombatantTargetSelectors.EventTarget,
                    new StatusDefinitionId(Keywords.Paperwork), new ConstantExpression<TContext>(2)),
            ]));

    private static IEffectNode<TContext> HitAll<TContext>(int damage) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(CombatantTargetSelectors.AllEnemiesOfSource,
            new DealDamageNode<TContext>(CombatantTargetSelectors.IterationTarget, new ConstantExpression<TContext>(damage)));

    private static IEffectNode<TContext> FileOnAll<TContext>(string status, int stacks) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(CombatantTargetSelectors.AllEnemiesOfSource,
            new ApplyStatusNode<TContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(status),
                new ConstantExpression<TContext>(stacks)));
}
