using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules of the ACT IV boss relics — the Licensing Labyrinth's eight offices, handed to the
// player. Each is a piece of its boss's own machinery: the Pharaoh's names and his open audience, the
// Weigher's third-turn judgment and her two pans, the Architect's retention and repetition, the Lady's
// granary, the First Scribe's copies and erasures, the Mother's wrappings, the Vizier's three offices, and
// the Queen's gauge.
//
// The devices are the ones the earlier pools established, and they are used here without further comment:
//
//   a LATCH — a counter set to 1 and cleared when the hand arrives — is "the first time each turn";
//   a LEDGER — a counter written at the turn's end and read at the next draw — is "based on last turn";
//   a MARK on a card instance is "this copy, until it is played";
//   a GRACE — a card the fight puts in the holder's hand — is any "free action" the design writes, because
//   a combat here has no free actions (ActFourBossRelicCards).
//
// Two things about WHEN a rule may speak are load-bearing and have been paid for twice already:
//
//   Block gained at TurnStarted is swept away by the turn's own clear, so everything that opens a turn with
//   Block opens it at CardsDrawn instead — the moment after the hand arrives;
//   and Energy promised at a turn's start lands on a pool the refill has already filled, so it is HELD and
//   arrives when the holder runs dry (HeldEnergy).
public static partial class BossRelicRules
{
    // ── ids and counters ──────────────────────────────────────────────────────────────────────────────────

    public const string CartoucheSpentId = "eternal_cartouche_spent";
    public const string AcquittalJudgmentId = "acquittal_judgment";
    public const string ErasedLineId = "erased_line";
    public const string CanopicWardId = "canopic_ward";
    public const string TriuneStrikeId = "triune_strike";

    // What an erased line guards for instead of doing what it said.
    public const int ErasedLineBlock = 20;

    // What the fight tells the RUN about the cartouche: a combat counter is the only bridge between them,
    // and the relic is destroyed on the far side of it.
    public const string CartoucheSpentCounter = "eternal_cartouche_spent";

    // A quarter of a Max HP that the engine cannot read from inside a fight — a death-prevention spec takes
    // a NUMBER, not an expression. 18 is a quarter of the roster's middle (64–78 to start with).
    public const int CartoucheHealth = 18;

    private static CounterId CartoucheRecorded => new("eternal_cartouche_recorded");
    private static CounterId FeatherLed => new("feather_led_with");
    private static CounterId FeatherPaid => new("feather_answered");
    private static CounterId ScarabTurn => new("acquittal_scarab_turn");
    private static CounterId PansHealed => new("two_pans_healed");
    private static CounterId PansOwed => new("two_pans_owed");
    private static CounterId Capstone => new("impossible_capstone");
    private static CounterId PyramidionCount => new("pyramidion_count");
    private static CounterId PyramidionBusy => new("pyramidion_busy");
    private static CounterId PlumbLast => new("crooked_plumb_last");
    private static CounterId PlumbPaid => new("crooked_plumb_paid");
    private static CounterId GranaryKept => new("black_granary_kept");
    private static CounterId RationPaid => new("ration_seal_paid");
    private static CounterId ReedRecorded => new("palimpsest_recorded");
    private static CounterId ErasureUsed => new("erasure_tablet");
    private static CounterId CorrectionUsed => new("correction_reed");
    private static CounterId AudienceUsed => new("edict_of_the_open_audience");
    private static CounterId ShroudSpent => new("resin_shroud_spent");
    private static CounterId TriuneWorking => new("triune_office_working");
    private static CounterId StaffUsed => new("staff_of_the_kings_mouth");
    private static CounterId ThroneUsed => new("vacant_throne_decree");
    private static CounterId SluiceUsed => new("sluice_gate_of_the_two_lands");
    private static CounterId VesselUsed => new("black_flood_vessel");
    private static CounterId CrownEnergy => new("flood_reckoning_crown_energy");

    // The copy the Palimpsest Reed put by for tomorrow, marked so that tomorrow can find it.
    private static TagId ReedMark => new("palimpsest_copy");

    // ⚠ A PROPERTY, like every id above it, and for the reason the file header gives: a static FIELD declared
    // below the status that names it is still null when that status is built, and a create-node handed a null
    // result key records nothing — so the copy is made, and then nothing can find it to mark it.
    private static EffectResultKey<OrderedTargetOutcomes<CreateCardInstanceOutcome>> Copied =>
        new("palimpsest_reed.copied");

    public static IReadOnlyList<StatusData> ActFourRules() =>
    [
        CrownOfTheThreeNames, EdictOfTheOpenAudience, EternalCartouche, EternalCartoucheSpent,
        FeatherOfPerfectMeasure, AcquittalScarab, AcquittalJudgment, BalanceOfTheTwoPans,
        ImpossibleCapstone, PyramidionOfRepetition, CrookedPlumbLine,
        BlackGranaryKey, RationSeal,
        PalimpsestReed, ErasureTablet, ErasedLine, CorrectionReed,
        CanopicCabinet, CanopicWard, ResinShroud, BasinOfBlackNatron,
        TriuneOfficeSeal, TriuneStrike, StaffOfTheKingsMouth, VacantThroneDecree,
        SluiceGateOfTheTwoLands, FloodReckoningCrown, BlackFloodVessel,
    ];

    // ── The Pharaoh of the Sealed Name — the three names ──────────────────────────────────────────────────

    // "At start of each turn gain 1 Energy."
    public static readonly StatusData CrownOfTheThreeNames = Rule(
        "crown_of_the_three_names", "Crown of the Three Names",
        "Every turn is worth one more Energy.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                Hold<CardsDrawnTriggeredEffectContext>(1)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Once/combat after drawing hand: all cards currently in hand cost 0 for the rest of that turn."
    //
    // The edict is a GRACE — the holder decides which hand is worth hearing — and the price it takes off is
    // put back at the turn's end, on the marked cards and on nothing else. Without that, a cost mark rides
    // its card into the discard pile and comes back out of it free, three turns after the audience closed.
    public static readonly StatusData EdictOfTheOpenAudience = Rule(
        "edict_of_the_open_audience", "Edict of the Open Audience",
        "Once a combat, every card in your hand is heard for nothing.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(AudienceUsed, ComparisonOperator.Equal, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActFourBossRelicCards.AudienceId, ActFourBossRelicCards.AudienceTag))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
                    Self, CardZone.Hand,
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new SetCardInstanceMarkCounterNode<TurnEndedTriggeredEffectContext>(
                            Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(
                                -ActFourBossRelicCards.FreeEnough),
                            relative: true),
                        new MarkCardInstanceNode<TurnEndedTriggeredEffectContext>(
                            Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                            ActFourBossRelicCards.AudienceMark, remove: true),
                    ]),
                    markFilter: ActFourBossRelicCards.AudienceMark)),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // "Once after acquisition, if damage would reduce HP to 0: prevent it, set HP to 25% Max HP rounded up,
    // remove all negative statuses, permanently destroy this relic."
    //
    // Two ADAPTATIONS, both forced. The surviving health is a NUMBER — a death-prevention interceptor is
    // built before a fight and cannot read the holder's Max HP — so it is a quarter of the roster's middle.
    // And "destroy this relic" happens in the RUN, which never sees the moment: the fight records it in a
    // combat counter (below), and the relic's own run program reads that counter when the fight resolves and
    // takes itself off. See ADAPTATIONS.md.
    public static readonly StatusData EternalCartouche = new()
    {
        Id = "eternal_cartouche",
        NameKey = "Eternal Cartouche",
        DescriptionKey =
            $"The first blow that would end you does not: you stand at {CartoucheHealth} HP, clean of every "
            + "affliction, and the cartouche is spent for good.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        DeathPrevention = new StatusDeathPreventionData(CartoucheHealth,
        [
            new InterceptorEffectData(nameof(EffectKind.Cleanse), nameof(EffectTarget.Self), 0, "", 0,
                StatusPolarity.Debuff),
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                CartoucheSpentId, 0, StatusPolarity.Neutral),
        ]),
    };

    // The mark the spent cartouche leaves behind, and the only thing that can leave it: the interceptor takes
    // the cartouche off before its effects run, so the cartouche itself is not there to write anything down.
    // What it writes is a COMBAT COUNTER, which is what a fight can say to the run it is inside.
    public static readonly StatusData EternalCartoucheSpent = new()
    {
        Id = CartoucheSpentId,
        NameKey = "Cartouche Spent",
        DescriptionKey = "The cartouche has been read once, and will not be read again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                SetCounter<CardsDrawnTriggeredEffectContext>(CartoucheRecorded, 1)),
                nameof(TriggerEvent.CardsDrawn)),
            // Both ends of the turn, because a fight can end on either of them and the run has to hear it.
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                SetCounter<TurnEndedTriggeredEffectContext>(CartoucheRecorded, 1)),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── The Weigher of the Unspoken Heart — the scales ────────────────────────────────────────────────────

    // "First Deed or Working each turn costs 1 less. The first later play of the opposite category draws 1
    // and gains 8 Block."
    //
    // "Costs 1 less" for a card already paid for is the price coming back, which is what the holder feels and
    // is how every discount in this pool is written. It is capped at what was actually spent, so a card that
    // cost nothing does not pay the holder for playing it.
    public static readonly StatusData FeatherOfPerfectMeasure = Rule(
        "feather_of_perfect_measure", "Feather of Perfect Measure",
        "Whichever you lead with costs 1 less — and the first time you answer it with the other kind, draw "
        + "1 and gain 8 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Counter<CardPlayedTriggeredEffectContext>(FeatherLed, ComparisonOperator.Equal, 0),
                        // The lead: written down, and refunded up to the one point it is worth.
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            RecordPlayedType(FeatherLed),
                            Hold(RefundUpTo(1)),
                        ]),
                        // The answer: any other kind, once.
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                Counter<CardPlayedTriggeredEffectContext>(
                                    FeatherPaid, ComparisonOperator.Equal, 0),
                                new NotExpression<CardPlayedTriggeredEffectContext>(
                                    PlayedTypeIsCounter(FeatherLed))),
                            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                            [
                                SetCounter<CardPlayedTriggeredEffectContext>(FeatherPaid, 1),
                                Draw<CardPlayedTriggeredEffectContext>(1),
                                Block<CardPlayedTriggeredEffectContext>(8),
                            ]))))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(FeatherLed, 0),
                    SetCounter<CardsDrawnTriggeredEffectContext>(FeatherPaid, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Every third player turn remove all enemy Block; enemies take 30% increased player-caused HP damage for
    // the rest of the turn. Upcoming judgment is shown one turn ahead."
    //
    // The lookahead is not a rule at all — it is a DISCLOSURE, which the engine grants a bearer outright, so
    // nothing has to fire for the holder to see the enemy's next line but one.
    public static readonly StatusData AcquittalScarab = new()
    {
        Id = "acquittal_scarab",
        NameKey = "Acquittal Scarab",
        DescriptionKey =
            "Every third turn the court sits: enemy guards fall and you strike 30% harder until the turn "
            + "ends. You read one judgment further ahead than anyone else.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Disclosure = new DisclosureData(IntentLookahead: 1),
        Triggers =
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    CountOwnTurn<CardsDrawnTriggeredEffectContext>(ScarabTurn),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    Self, ScarabTurn),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.AllEnemiesOfSource,
                                new ModifyDefensivePoolNode<CardsDrawnTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    StandardCombatIds.BlockDefensivePool,
                                    new NegateExpression<CardsDrawnTriggeredEffectContext>(
                                        new CombatantDefensivePoolExpression<CardsDrawnTriggeredEffectContext>(
                                            CombatantTargetSelectors.IterationTarget,
                                            StandardCombatIds.BlockDefensivePool)))),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                Self, new StatusDefinitionId(AcquittalJudgmentId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ],
    };

    // The court's own sitting: 30 % on everything the holder does, for the one turn it lasts.
    public static readonly StatusData AcquittalJudgment = new()
    {
        Id = AcquittalJudgmentId,
        NameKey = "The Court Sits",
        DescriptionKey = "Everything you do this turn lands 30% harder.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.ScalePercent, 130, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(AcquittalJudgmentId))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // "End turn with equal numbers of Deeds and Workings, at least one each: heal 2 and gain 1 Energy next
    // turn; healing max 10/combat. Otherwise gain 12 Block."
    //
    // Nothing has to be counted by hand here: the engine already knows how many cards of a tag were played
    // this turn, and two of those readings are the two pans.
    public static readonly StatusData BalanceOfTheTwoPans = Rule(
        "balance_of_the_two_pans", "Balance of the Two Pans",
        "End a turn with as many Deeds as Workings, and at least one of each, to heal 2 and open the next "
        + "turn with an Energy. An unbalanced turn ends in 12 Block instead.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            PlayedThisTurn<TurnEndedTriggeredEffectContext>(CardAuthoring.DeedTag),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            PlayedThisTurn<TurnEndedTriggeredEffectContext>(CardAuthoring.DeedTag),
                            ComparisonOperator.Equal,
                            PlayedThisTurn<TurnEndedTriggeredEffectContext>(CardAuthoring.WorkingTag))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        SetCounter<TurnEndedTriggeredEffectContext>(PansOwed, 1),
                        // The healing has a ceiling per fight, and the ceiling is kept as what has been paid.
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            Counter<TurnEndedTriggeredEffectContext>(PansHealed, ComparisonOperator.Less, 10),
                            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                            [
                                new HealNode<TurnEndedTriggeredEffectContext>(
                                    Self, new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                                AddCounter<TurnEndedTriggeredEffectContext>(PansHealed, 2),
                            ])),
                    ]),
                    Block<TurnEndedTriggeredEffectContext>(12))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(PansOwed, ComparisonOperator.Greater, 0),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        SetCounter<CardsDrawnTriggeredEffectContext>(PansOwed, 0),
                        Hold<CardsDrawnTriggeredEffectContext>(1),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Architect of the Impossible Pyramid — what stands and what repeats ────────────────────────────

    // "At end of turn retain 50% of remaining Block, rounded down, no cap."
    public static readonly StatusData ImpossibleCapstone = Rule(
        "impossible_capstone", "Impossible Capstone",
        "Half of whatever Block you still have at the end of a turn is still there at the start of the next.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, Capstone,
                    new DivideExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                            Self, StandardCombatIds.BlockDefensivePool),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                        Self,
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, Capstone)),
                    SetCounter<CardsDrawnTriggeredEffectContext>(Capstone, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Every sixth non-Junk card is played twice; the repeat costs no Energy. The counter resets each combat."
    //
    // The repeat re-runs the card's own program rather than playing the card again, so it neither costs
    // anything nor tells the fight that a card was played — which is also what keeps a sixth card from being
    // its own seventh. The latch is belt and braces over that.
    public static readonly StatusData PyramidionOfRepetition = Rule(
        "pyramidion_of_repetition", "Pyramidion of Repetition",
        "Every sixth real card you play in a fight happens twice, and the second time is free.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        PlayedNonJunk(),
                        Counter<CardPlayedTriggeredEffectContext>(
                            PyramidionBusy, ComparisonOperator.Equal, 0)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        AddCounter<CardPlayedTriggeredEffectContext>(PyramidionCount, 1),
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                new RemainderExpression<CardPlayedTriggeredEffectContext>(
                                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                        Self, PyramidionCount),
                                    new ConstantExpression<CardPlayedTriggeredEffectContext>(6)),
                                ComparisonOperator.Equal,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                            [
                                SetCounter<CardPlayedTriggeredEffectContext>(PyramidionBusy, 1),
                                new ReplayCardProgramNode<CardPlayedTriggeredEffectContext>(
                                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                                    CombatantTargetSelectors.LowestHealthEnemyOfSource),
                                SetCounter<CardPlayedTriggeredEffectContext>(PyramidionBusy, 0),
                            ])),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "First time each turn two consecutive non-Junk cards have different types, refund up to 2 Energy
    // actually spent on the second. If never triggered that turn, gain 10 Block at end."
    public static readonly StatusData CrookedPlumbLine = Rule(
        "crooked_plumb_line", "Crooked Plumb Line",
        "The first time in a turn you follow a card with one of another kind, up to 2 Energy comes back. A "
        + "turn that never bends ends in 10 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(),
                    // The comparison comes FIRST and the record second: a sequence that wrote the new type
                    // before reading the old one would find every card following itself.
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                Counter<CardPlayedTriggeredEffectContext>(
                                    PlumbLast, ComparisonOperator.Greater, 0),
                                new NotExpression<CardPlayedTriggeredEffectContext>(
                                    PlayedTypeIsCounter(PlumbLast))),
                            OnceEachTurn<CardPlayedTriggeredEffectContext>(
                                PlumbPaid, Hold(RefundUpTo(2)))),
                        RecordPlayedType(PlumbLast),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Counter<TurnEndedTriggeredEffectContext>(PlumbPaid, ComparisonOperator.Equal, 0),
                    Block<TurnEndedTriggeredEffectContext>(10))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(PlumbLast, 0),
                    SetCounter<CardsDrawnTriggeredEffectContext>(PlumbPaid, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Lady of the Black Granaries — what is kept back ───────────────────────────────────────────────

    // "Unspent Energy is retained between turns with no cap."
    public static readonly StatusData BlackGranaryKey = Rule(
        "black_granary_key", "Black Granary Key",
        "Energy you do not spend is not lost: it is stored, and it comes back the moment you run out.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, GranaryKept,
                    new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                        Self, StandardCombatIds.EnergyResource),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Hold(new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, GranaryKept)),
                    SetCounter<CardsDrawnTriggeredEffectContext>(GranaryKept, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Fourth non-Junk card each turn costs 0 and draws 1 after resolving. If fewer than four are played,
    // gain 10 Block at end."
    // ⚠ The STATUS is `ration_seal_relic` and not `ration_seal`: the Scarab elite of this same act already
    // owns that status id, and a status registry is one flat namespace. The RELIC keeps the design's name.
    public static readonly StatusData RationSeal = Rule(
        "ration_seal_relic", "Ration Seal",
        "The fourth real card of a turn is free and draws you another. A turn that never gets there ends in "
        + "10 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        PlayedNonJunk(),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            NonJunkPlayedThisTurn<CardPlayedTriggeredEffectContext>(),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        SetCounter<CardPlayedTriggeredEffectContext>(RationPaid, 1),
                        Hold(RefundUpTo(9)),
                        Draw<CardPlayedTriggeredEffectContext>(1),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Counter<TurnEndedTriggeredEffectContext>(RationPaid, ComparisonOperator.Equal, 0),
                    Block<TurnEndedTriggeredEffectContext>(10))),
                nameof(TriggerEvent.TurnEnded)),
            ClearLatch(RationPaid),
        ]);

    // ── The First Scribe of the House of Life — the copy and the erasure ──────────────────────────────────

    // "First Deed or Working each turn is Recorded. Next turn add a temporary copy to hand; it costs 0 that
    // turn and Exhausts. Only one Recorded card at a time."
    //
    // The copy is made at once, into the discard pile, and MARKED; the next hand fetches whatever carries the
    // mark. That detour exists because a card put somewhere cannot be named again afterwards — but the
    // program that MAKES it can name what it made, and a mark outlives the moment.
    public static readonly StatusData PalimpsestReed = Rule(
        "palimpsest_reed", "Palimpsest Reed",
        "The first real card you play each turn is copied down; the copy is in your hand next turn, and it "
        + "is free.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(ReedRecorded, RecordACopy()))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(ReedRecorded, 0),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                        Self, CardZone.DiscardPile,
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                CardZone.Hand),
                            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                ReedMark, remove: true),
                        ]),
                        markFilter: ReedMark),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static IEffectNode<CardPlayedTriggeredEffectContext> RecordACopy() =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            new CreateCardCopyNode<CardPlayedTriggeredEffectContext>(
                Self,
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                CardZone.DiscardPile,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1),
                Copied),
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                Self, new CreateCardOutcomeExpression<CardPlayedTriggeredEffectContext>(Copied), ReedMark),
            new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                Self, new CreateCardOutcomeExpression<CardPlayedTriggeredEffectContext>(Copied),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(ActFourBossRelicCards.FreeEnough),
                relative: true),
        ]);

    // "Once/combat after enemy intent revealed: erase it." A GRACE, because the holder chooses which line is
    // worth erasing.
    public static readonly StatusData ErasureTablet = Rule(
        "erasure_tablet", "Erasure Tablet",
        "Once a combat you may erase what the enemies were about to do.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(ErasureUsed, ComparisonOperator.Equal, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActFourBossRelicCards.ErasureId, ActFourBossRelicCards.ErasureTag))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The erased line itself: whatever it was, it lands for nothing, and it is gone at the end of the turn
    // that used it.
    public static readonly StatusData ErasedLine = new()
    {
        Id = ErasedLineId,
        NameKey = "Erased",
        DescriptionKey = "This line has been rubbed out: what it was for deals no damage, and the hand that "
            + "wrote it guards instead.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.ScalePercent, 0, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers =
        [
            // ⚠ The 20 Block is paid HERE and not by the card that erased the line, because Block expires at
            // the start of its OWNER's turn: guard handed to an enemy during the player's turn is swept away
            // before the enemy has done anything with it. Paid as its turn ENDS, it stands through the
            // player's next turn, which is the turn it was meant to be worth something against.
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(
                        Self, new ConstantExpression<TurnEndedTriggeredEffectContext>(ErasedLineBlock)),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(ErasedLineId)),
                ])),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // "Once/turn after normal draw swap 1 hand card with 1 Discard card." Also a GRACE — a swap nobody is
    // asked about is a shuffle, not a correction.
    public static readonly StatusData CorrectionReed = Rule(
        "correction_reed", "Correction Reed",
        "Once a turn you may correct the record: a card away, a card back, and the one you take back comes "
        + "cheaper.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(CorrectionUsed, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActFourBossRelicCards.CorrectionId, ActFourBossRelicCards.CorrectionTag),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Mother of Natron and Resin — the wrappings ────────────────────────────────────────────────────

    // "At combat start gain 12 Block. First application of each distinct negative status to you each combat
    // is prevented."
    //
    // ADAPTATION: "each distinct" is a question about ids, and a prohibition counts applications rather than
    // kinds — nothing in the engine can hold a set of what has already been refused once. So the cabinet
    // carries TWO charges instead: the first two afflictions of a fight are refused outright, whatever they
    // are and however many stacks they carried. See ADAPTATIONS.md.
    public static readonly StatusData CanopicCabinet = Once(
        "canopic_cabinet", "Canopic Cabinet",
        "The fight opens with 12 Block, and the first two afflictions laid on you are refused outright.",
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Block<CardsDrawnTriggeredEffectContext>(12),
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                Self, new StatusDefinitionId(CanopicWardId),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
        ]),
        nameof(TriggerEvent.CardsDrawn));

    // The wrapping itself. It refuses a WHOLE application for one of its stacks — the shape a charge has, as
    // against the stack-for-stack toll Censure pays — and it stands aside for the Interdict, which is a
    // deliberate choice by the holder where this is simply worn.
    public static readonly StatusData CanopicWard = new()
    {
        Id = CanopicWardId,
        NameKey = "Canopic Wrapping",
        DescriptionKey = "The next affliction laid on you is refused, whatever it is and however large.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Prevention = new StatusPreventionData(
            StatusPreventionScope.UnwantedByBearer, Priority: 3, RefusesWholeApplication: true),
    };

    // "First time each combat an enemy turn ends while you are below 50% Max HP: remove all negative statuses
    // and gain 25 Block."
    //
    // ADAPTATION of the MOMENT and of nothing else: an enemy turn ending and the holder's turn beginning are
    // the same instant seen from two sides, and the second is the one where Block survives the turn's own
    // clear. See ADAPTATIONS.md.
    public static readonly StatusData ResinShroud = Rule(
        "resin_shroud", "Resin Shroud",
        "Once a fight, coming round below half your health strips every affliction and wraps you in 25 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(ShroudSpent, ComparisonOperator.Equal, 0),
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantHealthPercentageExpression<CardsDrawnTriggeredEffectContext>(Self),
                            ComparisonOperator.Less,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(50))),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        SetCounter<CardsDrawnTriggeredEffectContext>(ShroudSpent, 1),
                        new RemoveStatusesByPolarityNode<CardsDrawnTriggeredEffectContext>(
                            Self, StatusPolarity.Debuff),
                        Block<CardsDrawnTriggeredEffectContext>(25),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Start turn: if you have a negative status, remove 1 stack of one of your choice; otherwise gain 12
    // Block."
    //
    // ADAPTATION: the choice is not put to the holder — a prompt at every turn start for one stack of one
    // affliction is four clicks a fight for a decision that is nearly always the same one — so the basin
    // takes the first. See ADAPTATIONS.md.
    public static readonly StatusData BasinOfBlackNatron = Rule(
        "basin_of_black_natron", "Basin of Black Natron",
        "Each turn the basin washes a stack off one of your afflictions — or, if you have none, gives you 12 "
        + "Block instead.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantStacksByPolarityExpression<CardsDrawnTriggeredEffectContext>(
                            Self, StatusPolarity.Debuff),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ModifySelectedStatusStacksNode<CardsDrawnTriggeredEffectContext>(
                        Self,
                        new StatusSelectionSpec(StatusPolarityFilter.Debuff, StatusPick.First),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1)),
                    Block<CardsDrawnTriggeredEffectContext>(12))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Vizier of the King's Mouth — the three offices, kept by the holder ────────────────────────────

    // "Each turn: first Deed +8 total damage; draw +1; first Working gains 8 Block. All three offices active."
    public static readonly StatusData TriuneOfficeSeal = Rule(
        "triune_office_seal", "Triune Office Seal",
        "All three offices answer to you: an extra card each turn, 8 more on your first Deed, and 8 Block on "
        + "your first Working.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(TriuneWorking, 0),
                    Draw<CardsDrawnTriggeredEffectContext>(1),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        Self, new StatusDefinitionId(TriuneStrikeId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedHasTag(CardAuthoring.WorkingTag),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(
                        TriuneWorking, Block<CardPlayedTriggeredEffectContext>(8)))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // The office of the inner stair, as the holder wears it: +8 on one Deed, spent by that Deed.
    public static readonly StatusData TriuneStrike = new()
    {
        Id = TriuneStrikeId,
        NameKey = "Armed Authority",
        DescriptionKey = "Your next Deed deals 8 more damage.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, 8,
                RestrictDamageKind: DamageKind.Direct, RestrictSourceCardTag: CardAuthoring.DeedTag,
                OncePerAction: true),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedHasTag(CardAuthoring.DeedTag),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(TriuneStrikeId)))),
                nameof(TriggerEvent.CardPlayed)),
        ],
    };

    // "First non-Junk card each turn refunds Energy actually spent after resolving, maximum refund 2."
    public static readonly StatusData StaffOfTheKingsMouth = Rule(
        "staff_of_the_kings_mouth", "Staff of the King's Mouth",
        "The first real card of each turn is paid for out of the King's own purse, up to 2 Energy.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(
                        StaffUsed, Hold(RefundUpTo(2))))),
                nameof(TriggerEvent.CardPlayed)),
            ClearLatch(StaffUsed),
        ]);

    // "Once/combat free action: gain 3 Energy, draw 3, gain 20 Block."
    public static readonly StatusData VacantThroneDecree = Rule(
        "vacant_throne_decree", "Vacant-Throne Decree",
        "Once a combat, the empty throne pays: 3 Energy, three cards and 20 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(ThroneUsed, ComparisonOperator.Equal, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActFourBossRelicCards.ThroneId, ActFourBossRelicCards.ThroneTag))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Queen of the Flood Reckoning — the gauge ──────────────────────────────────────────────────────

    // "Once/turn free action choose: OPEN or CLOSE."
    public static readonly StatusData SluiceGateOfTheTwoLands = Rule(
        "sluice_gate_of_the_two_lands", "Sluice Gate of the Two Lands",
        "Once a turn you may work the gate: Block into Energy, or Energy into Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SetCounter<CardsDrawnTriggeredEffectContext>(SluiceUsed, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActFourBossRelicCards.SluiceId, ActFourBossRelicCards.SluiceTag),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Start turn based on previous end: ended at 0 Energy → +1 Energy and +1 Draw; ended with 1+ → +1 Energy
    // and 15 Block. Turn 1 gain 10 Block instead."
    //
    // The ledger is written as "what was left, plus one", so that a turn ended dry (1) and a turn that has
    // not happened yet (0) are different readings — a counter that has never been written and a counter
    // written with nothing in it look the same otherwise.
    public static readonly StatusData FloodReckoningCrown = Rule(
        "flood_reckoning_crown", "Flood-Reckoning Crown",
        "How you ended decides how you open: dry, and the crown pays an Energy and a card; with something "
        + "left, an Energy and 15 Block. The first turn of a fight opens with 10 Block.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, CrownEnergy,
                    new AddExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                            Self, StandardCombatIds.EnergyResource),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(CrownEnergy, ComparisonOperator.Equal, 0),
                    Block<CardsDrawnTriggeredEffectContext>(10),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Hold<CardsDrawnTriggeredEffectContext>(1),
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            Counter<CardsDrawnTriggeredEffectContext>(
                                CrownEnergy, ComparisonOperator.Equal, 1),
                            Draw<CardsDrawnTriggeredEffectContext>(1),
                            Block<CardsDrawnTriggeredEffectContext>(15)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Once/combat after normal draw discard entire hand, then draw 7 and gain 2 Energy."
    public static readonly StatusData BlackFloodVessel = Rule(
        "black_flood_vessel", "Black Flood Vessel",
        "Once a combat you may pour the whole hand away and draw seven fresh ones, with 2 Energy to spend "
        + "on them.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(VesselUsed, ComparisonOperator.Equal, 0),
                    OfferTheCard<CardsDrawnTriggeredEffectContext>(
                        ActFourBossRelicCards.VesselId, ActFourBossRelicCards.VesselTag))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── shared shorthands ─────────────────────────────────────────────────────────────────────────────────

    // "Costs N less" for a card that has already been paid for: the price comes back, capped both by N and by
    // what was actually spent, so a free card is not a source of Energy.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> RefundUpTo(int most) =>
        new MinExpression<CardPlayedTriggeredEffectContext>(
            new ConstantExpression<CardPlayedTriggeredEffectContext>(most),
            new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                StandardCombatIds.EnergyResource));

    // How many cards carrying a tag the bearer has played this turn.
    private static ICombatExpression<TContext, int> PlayedThisTurn<TContext>(string tag)
        where TContext : class =>
        new CardsPlayedThisTurnWithTagExpression<TContext>(Self, new TagId(tag));
}
