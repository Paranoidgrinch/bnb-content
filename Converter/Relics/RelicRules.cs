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
        TarnishedBell, BruiseCup, VotiveCandle, RootboundStaff, EmergencyInkwell, AshenWaxKnife,
        QuietReadersCord, ArchiveKey, RedactionKnife, IndexBone, ArchiveCenser, SealMakersDie,
        BloodPriceToken, BlackthornBrooch, SootglassLens, RubricTablet, RefuseDocket, IndexVolvelle,
        WithheldHourglass, ConcordanceMedallion, ChanceryRibbon, IronAstrolabe, RebindingSpindle,
        DeferredSignet, BloodStampedBond, ThornCrownedReliquary, ChanceryScale,
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

    // "Lose 6 HP; gain 1 Energy and 1 extra card on turn 1."
    public static readonly StatusData BloodStampedBond =
        Once("blood_stamped_bond", "Blood-Stamped Bond", "Signed in blood, once per fight.",
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new DealDamageNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new ConstantExpression<CardsDrawnTriggeredEffectContext>(6),
                    ignoresBlock: true, kind: DamageKind.DamageOverTime),
                Energy<CardsDrawnTriggeredEffectContext>(1),
                Draw<CardsDrawnTriggeredEffectContext>(1),
            ]), nameof(TriggerEvent.CardsDrawn));

    // "At the start of your next combat after a non-combat node, gain 1 Energy and 6 Block." Every combat
    // gets it — the map bookkeeping that would restrict it to combats following a rest is not worth a run
    // program of its own. See ADAPTATIONS.
    public static readonly StatusData RootboundStaff =
        Once("rootbound_walking_staff", "Rootbound Walking Staff", "The road rested you.",
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [Energy<CardsDrawnTriggeredEffectContext>(1), Block<CardsDrawnTriggeredEffectContext>(6)]),
            nameof(TriggerEvent.CardsDrawn));

    // "At the start of your turn, draw 1 additional card."
    public static readonly StatusData IndexBone = Rule("index_bone", "Index Bone",
        "You draw one more card each turn.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            OnceEachTurn<CardsDrawnTriggeredEffectContext>("index_bone",
                Draw<CardsDrawnTriggeredEffectContext>(1))), nameof(TriggerEvent.CardsDrawn)),
         ClearLatch("index_bone")]);

    // "Once per turn, after your normal draw, discard 1 card and draw 1 card." Discarding a card the player
    // would have picked is not something a rule can ask for, so the oldest card in hand goes. See ADAPTATIONS.
    public static readonly StatusData RedactionKnife = Rule("redaction_knife", "Redaction Knife",
        "After your draw, the oldest card in your hand is traded for a fresh one.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            OnceEachTurn<CardsDrawnTriggeredEffectContext>("redaction_knife",
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand, 0),
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
                [Energy<CardsDrawnTriggeredEffectContext>(1), Draw<CardsDrawnTriggeredEffectContext>(1)]),
            nameof(TriggerEvent.CardsDrawn));

    // "If you end your turn with at least 1 unspent Energy, gain 5 Block." The Block is granted after the
    // next draw, since Block gained at a turn's end is swept away by that turn's own end.
    public static readonly StatusData SunWarmedWaystone = Rule("sun_warmed_waystone", "Sun-Warmed Waystone",
        "Ending a turn with Energy to spare guards you next turn.",
        [Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                SetCounter<TurnEndedTriggeredEffectContext>("waystone_owed", 1))),
            nameof(TriggerEvent.TurnEnded)),
         Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Counter<CardsDrawnTriggeredEffectContext>("waystone_owed", ComparisonOperator.Greater, 0),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Block<CardsDrawnTriggeredEffectContext>(5),
                    SetCounter<CardsDrawnTriggeredEffectContext>("waystone_owed", 0),
                ]))), nameof(TriggerEvent.CardsDrawn))]);

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

    // "Every third 0-cost card played draws 1 card." Counted over every card rather than only free ones —
    // a trigger cannot read the cost of the card that fired it. See ADAPTATIONS.
    public static readonly StatusData HollowWaxBead = EveryNthCard("hollow_wax_bead", "Hollow Wax Bead", 3,
        "Every third card you play draws you another.",
        Draw<CardPlayedTriggeredEffectContext>(1));

    // "The first time each turn you play a card with base cost 2 or more, gain 4 Block." Cost is not readable
    // from a card-play trigger, so it pays on the first card of each turn. See ADAPTATIONS.
    public static readonly StatusData LeadCounterweight = FirstCardEachTurn(
        "lead_counterweight", "Lead Counterweight", "The first card you play each turn guards you.",
        Block<CardPlayedTriggeredEffectContext>(4));

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
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    Counter<CardPlayedTriggeredEffectContext>("inkwell_used", ComparisonOperator.Equal, 0),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                        ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    Energy<CardPlayedTriggeredEffectContext>(1),
                    SetCounter<CardPlayedTriggeredEffectContext>("inkwell_used", 1),
                ]))), nameof(TriggerEvent.CardPlayed))]);

    // ── status and damage watchers ────────────────────────────────────────────────────────────────────────

    // "The first time each turn you apply a negative status to an enemy, deal 4 damage to it."
    public static readonly StatusData TarnishedBell = FirstFilingEachTurn(
        "tarnished_bell", "Tarnished Bell", "The first paper you serve each turn stings.",
        new DealDamageNode<StatusAppliedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new ConstantExpression<StatusAppliedTriggeredEffectContext>(4)),
        new DealDamageNode<StatusMergedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new ConstantExpression<StatusMergedTriggeredEffectContext>(4)));

    // "The first time each turn you apply a negative status to an enemy that already had one, draw 1 card."
    // Told apart from the Bell by watching MERGES only: a status landing on one already there.
    public static readonly StatusData SootglassLens = Rule("sootglass_lens", "Sootglass Lens",
        "Filing on top of a filing tells you something.",
        [Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
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
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    CounterOn<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, "bruise_cup", ComparisonOperator.Equal, 0)),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new GainBlockNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(4)),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, new CounterId("bruise_cup"),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                ]))), nameof(TriggerEvent.DamageTaken)),
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
                    Counter<BlockGainedTriggeredEffectContext>("blackthorn", ComparisonOperator.Equal, 0)),
                new CausalSequenceEffectNode<BlockGainedTriggeredEffectContext>(
                [
                    new DealDamageNode<BlockGainedTriggeredEffectContext>(
                        CombatantTargetSelectors.AllEnemiesOfSource,
                        new ConstantExpression<BlockGainedTriggeredEffectContext>(6)),
                    SetCounter<BlockGainedTriggeredEffectContext>("blackthorn", 1),
                ]))), nameof(TriggerEvent.BlockGained)),
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

    public static readonly StatusData AshenWaxKnife = FirstCountEachTurn(
        "ashen_wax_knife", "Ashen Wax Knife", Keywords.Archived,
        "The first card you burn each turn pays another.", draw: 1);

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
         ClearLatch("concordance")]);

    // "The first card you Queue each turn applies 1 Seal to its target when it resolves." Read as: the first
    // queued card to RESOLVE each turn seals what it was aimed at.
    public static readonly StatusData DeferredSignet = Rule("deferred_signet", "Deferred Signet",
        "A queued matter comes back sealed.",
        [Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
            OnCountRising<StatusAppliedTriggeredEffectContext>("deferred_signet", Keywords.QueueResolved,
                new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.LowestHealthEnemyOfSource, new StatusDefinitionId(Keywords.Seal),
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)))),
            nameof(TriggerEvent.StatusApplied)),
         Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
            OnCountRising<StatusMergedTriggeredEffectContext>("deferred_signet", Keywords.QueueResolved,
                new ApplyStatusNode<StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.LowestHealthEnemyOfSource, new StatusDefinitionId(Keywords.Seal),
                    new ConstantExpression<StatusMergedTriggeredEffectContext>(1)))),
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
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.LowestHealthEnemyOfSource,
                            new StatusDefinitionId(Keywords.Seal),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]),
                    tagFilter: new TagId(CardAuthoring.JunkTag), takeFirst: 1))),
            nameof(TriggerEvent.CardsDrawn)),
         ClearLatch("refuse_docket")]);

    // "The first time each turn a card leaves your hand without being played, gain 4 Block." A card leaving
    // unplayed is not an event a rule can hear, so it pays at the turn's end when the hand is discarded —
    // the moment that is almost always about. See ADAPTATIONS.
    public static readonly StatusData ConservatorsThread = Rule(
        "conservators_thread", "Conservator's Thread",
        "What you set aside at the end of a turn guards you next turn.",
        [Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, CardZone.Hand),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                SetCounter<TurnEndedTriggeredEffectContext>("thread_owed", 1))),
            nameof(TriggerEvent.TurnEnded)),
         Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Counter<CardsDrawnTriggeredEffectContext>("thread_owed", ComparisonOperator.Greater, 0),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Block<CardsDrawnTriggeredEffectContext>(4),
                    SetCounter<CardsDrawnTriggeredEffectContext>("thread_owed", 0),
                ]))), nameof(TriggerEvent.CardsDrawn))]);

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

    public static readonly StatusData IronAstrolabe = Rule("iron_astrolabe", "Iron Astrolabe",
        "The first draw of each turn pays an Energy.",
        [Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            OnceEachTurn<CardsDrawnTriggeredEffectContext>("iron_astrolabe",
                Energy<CardsDrawnTriggeredEffectContext>(1))), nameof(TriggerEvent.CardsDrawn)),
         ClearLatch("iron_astrolabe")]);

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

    // "Retain the hand for one turn" — the nearest the engine has to keeping one card. See ADAPTATIONS.
    public static readonly StatusData BrassBookmark = Rule("brass_bookmark", "Brass Bookmark",
        "Your hand is kept through the first turn of a fight.",
        [], tags: [StandardCombatIds.RetainHandTag.value]);

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
    // Both halves happen on entering a rest node, since which action was taken is the node's own business.
    public static readonly ITriggeredRunEffectDefinition TwinEmberBrazier =
        RunPrograms.On<NodeEnteredRunEvent>(
            new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().Random(1)),
            new HealRunEffect(7));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Once(
        string id, string name, string description, IEffectNode<CardsDrawnTriggeredEffectContext> body,
        string trigger) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(id + "_done", ComparisonOperator.Equal, 0),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [body, SetCounter<CardsDrawnTriggeredEffectContext>(id + "_done", 1)]))),
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
                Counter<TContext>(id, ComparisonOperator.Equal, 0)),
            new CausalSequenceEffectNode<TContext>([body, SetCounter<TContext>(id, 1)]));

    // A negative status landing on an ENEMY, answered once a turn by whoever wears the relic.
    private static IEffectNode<TContext> OnEnemyFiling<TContext>(string id, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.EventTarget,
                        new StatusDefinitionId(Keywords.ApplicantMarker))),
                Wearers<TContext>(id, 0)),
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(id)),
                // The latch is read AND written on the wearer itself: who "source" means differs from event to
                // event, but the wearer is unambiguous, and it is the wearer whose once-a-turn promise this is.
                new ConditionalEffectNode<TContext>(
                    CounterOn<TContext>(CombatantTargetSelectors.IterationTarget, id, ComparisonOperator.Equal, 0),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        body,
                        new SetCombatantCounterNode<TContext>(
                            CombatantTargetSelectors.IterationTarget, new CounterId(id),
                            new ConstantExpression<TContext>(1), relative: false),
                    ]))));

    // "No wearer has used it yet this turn."
    private static ICombatExpression<TContext, bool> Wearers<TContext>(string id, int used)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CountTargetsExpression<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(id))),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(used));

    // "Apply half of what you filed to every other enemy." A flat 1, since the rule cannot read the amount.
    private static IEffectNode<TContext> SpreadFiling<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(Keywords.ApplicantMarker))),
                    new OrExpression<TContext>(
                        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Paperwork)),
                        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Doubt)))),
                Counter<TContext>("concordance", ComparisonOperator.Equal, 0)),
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource, new StatusDefinitionId(Keywords.Paperwork),
                    new ConstantExpression<TContext>(1)),
                SetCounter<TContext>("concordance", 1),
            ]));

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

    private static IEffectNode<TContext> OnceEachTurn<TContext>(string id, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            Counter<TContext>(id, ComparisonOperator.Equal, 0),
            new CausalSequenceEffectNode<TContext>([body, SetCounter<TContext>(id, 1)]));

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
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
