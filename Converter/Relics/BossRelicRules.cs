using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules the BOSS relics install (BnB_Final_Relics_Master_PostAudit.md §6). One hidden status per
// relic, handed over when a fight opens, exactly like the other pools.
//
// What makes this pool different is its subject. A Normal relic is usually a flat bonus; a boss relic is a
// piece of ITS BOSS's mechanic, handed to the player: the Undersecretary's backlog, the Commissioner's queue,
// the Sealkeeper's seals, the Catalogue's record of what you do, the Warden's custody, the Curator's borrowed
// time. So the shapes here are the boss shapes — a ledger kept across turns, a debt paid next turn, a card
// held back and returned cheaper.
//
// Three recurring devices, all established by the card pools:
//   · a LATCH — a counter set to 1 and cleared at turn start — is "the first time each turn";
//   · a LEDGER — a counter written at turn end and read at the next turn's draw — is "based on last turn";
//   · a MARK on a card instance is "this copy, until it is played".
public static partial class BossRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
    [
        // The Deputy Undersecretary
        UnfinishedDocket, RedRibbonedMatter, BacklogCounterseal,
        // The Queue Commissioner
        BrassServiceBell, PrioritySash, IvoryNumberDisc,
        // The Lord Sealkeeper
        AccessSealShard, TestimonySealShard, ExecutionSealShard,
        // The Municipal Dragon
        StampedExpeditionWrit, CivicEntryWarrant, InspectorsBrassCharter,
        // The Living Charter
        ContinuanceFragment, RightOfRedress, MarginOfAppeal,
        // The Whispering Catalogue
        ErrataRibbon, IndexOfContradictions, RegistryTab,
        // The Warden of Sealed Volumes
        CustodyShackle, MasterReleaseKey, ReleaseTag,
        // The Curator of Misplaced Hours
        MisdatedPocketWatch, BorrowedMinute, DeferredAppointmentBook,
        // The Auditor of Returned Lives
        IdentityWrit, SettledLedger,
        // The Grand Cross-Reference
        PremiseSlip, ConcordanceThread, ConclusionLeaf,
        // Act III — the Green Docket's five courts
        .. ActThreeRules(),
        // Act IV — the Licensing Labyrinth's eight offices
        .. ActFourRules(),
        // The discounts and bonuses these relics hand out are statuses of their own, for the same reason the
        // other pools' are: a passive modifier's PRESENCE is its condition, so it has to be removable.
        SealedStrike, DefusedIntent, TestimonySeal, ConcludedStrike,
        // Six of these promise Energy at a moment the pool is full; they all hold it instead (HeldEnergy).
        Converter.HeldEnergy.Status,
    ];

    // ── ids, counters and shorthands ──────────────────────────────────────────────────────────────────────
    //
    // These come BEFORE the rules that use them on purpose: static field initializers run in declaration
    // order, so a counter declared below the status that names it would still be its default when that status
    // is built — an id of nothing, silently.

    public const string SealedStrikeId = "sealed_strike";
    public const string TestimonySealId = "testimony_seal";
    public const string ConcludedStrikeId = "concluded_strike";
    public const string DefusedIntentId = "defused_intent";

    private static readonly TagId RibbonedMark = new("ribboned");
    private static readonly TagId CustodyMark = new("in_custody");
    private static readonly TagId EvidenceMark = new("evidence");
    private static readonly TagId ThreadMark = new("threaded");

    // The thread's draw names its own card, because the relic cares WHICH card came in.
    private static readonly EffectResultKey<OrderedTargetOutcomes<DrawCardsOutcome>> Referenced =
        new("concordance_thread.referenced");
    private static readonly TagId PremiseDiscountMark = new("premise_discount");

    private static CounterId Docket => new("unfinished_docket");
    private static CounterId Sash => new("priority_sash");
    private static CounterId Disc => new("ivory_number_disc");
    private static CounterId DiscHealth => new("ivory_number_disc_hp");
    private static CounterId ExpeditionDone => new("expedition_writ_done");
    private static CounterId WarrantDone => new("civic_warrant_done");
    private static CounterId Continuance => new("continuance_fragment");
    private static CounterId RedressStart => new("redress_opening_hp");
    private static CounterId RedressPaid => new("redress_owed");
    private static CounterId AppealDone => new("margin_of_appeal_done");
    private static CounterId ErrataNow => new("errata_now");
    private static CounterId ErrataLast => new("errata_last");
    private static CounterId ErrataOwed => new("errata_owed");
    private static CounterId Index => new("index_of_contradictions");
    private static CounterId IndexLast => new("index_last_type");
    private static CounterId Registered => new("registry_tab_type");
    private static CounterId MinuteDebt => new("borrowed_minute_debt");
    private static CounterId Writ => new("identity_writ");
    private static CounterId Ledger => new("settled_ledger");
    private static CounterId Premise => new("premise_slip_type");
    private static CounterId Conclusion => new("conclusion_leaf_type");
    private static CounterId Backlog => new("backlog_in_hand");
    private static CounterId BellTurn => new("brass_bell_turn");
    private static CounterId AppointmentTurn => new("appointment_turn");
    private static CounterId KeyUsed => new("master_release_key_used");

    private static ICombatantTargetSelector Self => CombatantTargetSelectors.Source;

    // ── the vocabulary the Act-II relics share ────────────────────────────────────────────────────────────

    // The three card types as the counters spell them: 1 Deed, 2 Working, 3 Rite. A card with no taxonomy tag
    // at all is not a type, and nothing here counts it.
    private static (string Tag, int Value)[] Types =>
    [
        (CardAuthoring.DeedTag, 1), (CardAuthoring.WorkingTag, 2), (CardAuthoring.RiteTag, 3),
    ];

    private static ICombatExpression<TContext, int> NonJunkPlayedThisTurn<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CardsPlayedThisTurnExpression<TContext>(Self),
            new CardsPlayedThisTurnWithTagExpression<TContext>(Self, new TagId(CardAuthoring.JunkTag)));

    private static ICombatExpression<TContext, int> NonJunkPlayedLastTurn<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CardsPlayedLastTurnExpression<TContext>(Self),
            new CardsPlayedLastTurnWithTagExpression<TContext>(Self, new TagId(CardAuthoring.JunkTag)));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedNonJunk() =>
        new NotExpression<CardPlayedTriggeredEffectContext>(PlayedHasTag(CardAuthoring.JunkTag));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedHasTag(string tag) =>
        new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(tag));

    // Write the type of the card just played into a counter.
    private static IEffectNode<CardPlayedTriggeredEffectContext> RecordPlayedType(CounterId counter) =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [.. Types.Select(type => new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                PlayedHasTag(type.Tag), SetCounter<CardPlayedTriggeredEffectContext>(counter, type.Value)))]);

    // "The card just played is of the type this counter holds."
    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedTypeIsCounter(CounterId counter)
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> matches = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
            new ConstantExpression<CardPlayedTriggeredEffectContext>(0),
            ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(1)); // never
        foreach (var (tag, value) in Types)
            matches = new OrExpression<CardPlayedTriggeredEffectContext>(matches,
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    PlayedHasTag(tag),
                    Counter<CardPlayedTriggeredEffectContext>(counter, ComparisonOperator.Equal, value)));
        return matches;
    }

    // "This is the second card of its type this turn" — the play the Identity Writ is watching for.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedTypeRepeats()
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> repeats = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
            new ConstantExpression<CardPlayedTriggeredEffectContext>(0),
            ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(1)); // never
        foreach (var (tag, _) in Types)
            repeats = new OrExpression<CardPlayedTriggeredEffectContext>(repeats,
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    PlayedHasTag(tag),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                            Self, new TagId(tag)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(2))));
        return repeats;
    }

    // Register `value` if no later type beat this one on turn 1. Written in descending precedence, so the last
    // write wins a tie — Deed over Working over Rite.
    private static IEffectNode<TurnEndedTriggeredEffectContext> RegisterIfHighest(string tag, int value) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(Self, new TagId(tag)),
                ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Beats(tag), SetCounter<TurnEndedTriggeredEffectContext>(Registered, value)));

    // "No other type was played more often this turn than this one."
    private static ICombatExpression<TurnEndedTriggeredEffectContext, bool> Beats(string tag)
    {
        ICombatExpression<TurnEndedTriggeredEffectContext, bool> holds = new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
            ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)); // always
        foreach (var (other, _) in Types.Where(t => t.Tag != tag))
            holds = new AndExpression<TurnEndedTriggeredEffectContext>(holds,
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(Self, new TagId(tag)),
                    ComparisonOperator.GreaterOrEqual,
                    new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(Self, new TagId(other))));
        return holds;
    }

    // The Registry Tab's discount: the first card in hand of the registered type.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> CheapenFirstOfType(int registered, string tag) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Counter<CardsDrawnTriggeredEffectContext>(Registered, ComparisonOperator.Equal, registered),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, CardZone.Hand,
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    StandardCombatIds.CardCostDeltaCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
                tagFilter: new TagId(tag), takeFirst: 1));

    // The Premise Slip's discount: every card in hand that is NOT the premise's type, marked so it can be
    // taken back when the premise expires.
    private static IEffectNode<CardPlayedTriggeredEffectContext> CheapenOtherTypes(string tag, int value) =>
        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new NotExpression<CardPlayedTriggeredEffectContext>(
                Counter<CardPlayedTriggeredEffectContext>(Premise, ComparisonOperator.Equal, value)),
            new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                Self, CardZone.Hand,
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                        Self, new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                        StandardCombatIds.CardCostDeltaCounter,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(-1), relative: true),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Self, new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                        PremiseDiscountMark),
                ]),
                tagFilter: new TagId(tag)));

    private static IEffectNode<CardPlayedTriggeredEffectContext> ExpirePremise() =>
        ExpirePremiseAt<CardPlayedTriggeredEffectContext>();

    // The premise is spent: every card it marked down goes back to its own price.
    private static IEffectNode<TContext> ExpirePremiseAt<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.Hand,
            new CausalSequenceEffectNode<TContext>(
            [
                new SetCardInstanceMarkCounterNode<TContext>(
                    Self, new IteratedCardExpression<TContext>(),
                    StandardCombatIds.CardCostDeltaCounter,
                    new ConstantExpression<TContext>(1), relative: true),
                new MarkCardInstanceNode<TContext>(
                    Self, new IteratedCardExpression<TContext>(), PremiseDiscountMark, remove: true),
            ]),
            markFilter: PremiseDiscountMark);

    // The Warden's two: one card held back in hand, and returned free the turn after.
    private static IEffectNode<TContext> TakeIntoCustody<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.Hand,
            new CausalSequenceEffectNode<TContext>(
            [
                new MarkCardInstanceNode<TContext>(
                    Self, new IteratedCardExpression<TContext>(), CustodyMark),
                new MarkCardInstanceNode<TContext>(
                    Self, new IteratedCardExpression<TContext>(), StandardCombatIds.RetainedCardMark),
            ]),
            takeFirst: 1);

    private static StatusTriggerData ReleaseFromCustody() =>
        Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, CardZone.Hand,
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // Free for this turn: exactly its own cost taken off.
                    new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                        Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                        StandardCombatIds.CardCostDeltaCounter,
                        new NegateExpression<CardsDrawnTriggeredEffectContext>(
                            new CardInstanceBaseCostExpression<CardsDrawnTriggeredEffectContext>(
                                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                StandardCombatIds.EnergyResource)),
                        relative: false),
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                        CustodyMark, remove: true),
                ]),
                markFilter: CustodyMark)),
            nameof(TriggerEvent.CardsDrawn));

    private static ICardInstanceExpression<TContext> InCustody<TContext>() where TContext : class =>
        new FirstMarkedCardInOwnerZoneExpression<TContext>(Self, CardZone.Hand, CustodyMark);

    private static ICardInstanceExpression<TContext> Evidence<TContext>() where TContext : class =>
        new FirstMarkedCardInOwnerZoneExpression<TContext>(Self, CardZone.Hand, EvidenceMark);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> OnOwnTurn(
        int turn, IEffectNode<CardsDrawnTriggeredEffectContext> body) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Counter<CardsDrawnTriggeredEffectContext>(AppointmentTurn, ComparisonOperator.Equal, turn), body);

    // What the hand holds right now, written down for the turn's end to read.
    private static IEffectNode<TContext> RecordHand<TContext>() where TContext : class =>
        new SetCombatantCounterNode<TContext>(Self, Backlog, NonJunkInHand<TContext>(), relative: false);

    // Custody is let go: the card stays a card like any other.
    private static IEffectNode<TContext> ReleaseCustody<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.Hand,
            new CausalSequenceEffectNode<TContext>(
            [
                new MarkCardInstanceNode<TContext>(
                    Self, new IteratedCardExpression<TContext>(), CustodyMark, remove: true),
                new MarkCardInstanceNode<TContext>(
                    Self, new IteratedCardExpression<TContext>(), StandardCombatIds.RetainedCardMark,
                    remove: true),
            ]),
            markFilter: CustodyMark);


    // The card the Red-Ribboned Matter picked out this turn.
    private static ICardInstanceExpression<TContext> Ribboned<TContext>() where TContext : class =>
        new FirstMarkedCardInOwnerZoneExpression<TContext>(Self, CardZone.Hand, RibbonedMark);

    private static ICombatExpression<TContext, bool> HoldsACard<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantZoneCardCountExpression<TContext>(Self, CardZone.Hand),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, int> NonJunkInHand<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CombatantZoneCardCountExpression<TContext>(Self, CardZone.Hand),
            new CombatantZoneCardCountExpression<TContext>(
                Self, CardZone.Hand, new TagId(CardAuthoring.JunkTag)));

    // "Out of Energy with cards still in hand, and this relic has not fired yet in this fight."
    private static ICombatExpression<TContext, bool> SpentOut<TContext>(CounterId done) where TContext : class =>
        new AndExpression<TContext>(
            Counter<TContext>(done, ComparisonOperator.Equal, 0),
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantCurrentResourceExpression<TContext>(Self, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
                HoldsACard<TContext>()));

    private static StatusData Once(
        string id, string name, string description, IEffectNode<CardsDrawnTriggeredEffectContext> body,
        string trigger)
    {
        var done = new CounterId(id + "_done");
        return Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(done, ComparisonOperator.Equal, 0),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [body, SetCounter<CardsDrawnTriggeredEffectContext>(done, 1)]))),
                trigger),
        ]);
    }

    private static IEffectNode<TContext> OnceEachTurn<TContext>(CounterId id, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            Counter<TContext>(id, ComparisonOperator.Equal, 0),
            new CausalSequenceEffectNode<TContext>([body, SetCounter<TContext>(id, 1)]));

    private static ICombatExpression<TContext, bool> Counter<TContext>(
        CounterId id, ComparisonOperator op, int value) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(Self, id), op, new ConstantExpression<TContext>(value));

    private static IEffectNode<TContext> SetCounter<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            Self, id, new ConstantExpression<TContext>(value), relative: false);

    private static IEffectNode<TContext> AddCounter<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            Self, id, new ConstantExpression<TContext>(value), relative: true);

    private static IEffectNode<TContext> Block<TContext>(int amount) where TContext : class =>
        new GainBlockNode<TContext>(Self, new ConstantExpression<TContext>(amount));

    private static IEffectNode<TContext> Draw<TContext>(int cards) where TContext : class =>
        new DrawCardsNode<TContext>(Self, new ConstantExpression<TContext>(cards));

    // Energy asked for while the pool may be full: held, and paid when the holder runs dry (see HeldEnergy).
    private static IEffectNode<TContext> Hold<TContext>(int amount) where TContext : class =>
        Converter.HeldEnergy.Hold<TContext>(amount);

    private static IEffectNode<TContext> Hold<TContext>(ICombatExpression<TContext, int> amount)
        where TContext : class => Converter.HeldEnergy.Hold(amount);

    // Energy asked for at a moment the pool is known to have room — the holder has just run out.
    private static IEffectNode<TContext> Energy<TContext>(int amount) where TContext : class =>
        new GainResourceNode<TContext>(
            Self, StandardCombatIds.EnergyResource, new ConstantExpression<TContext>(amount));

    // How many turns the BEARER has taken. `TurnNumberExpression` counts turns within a round — in a duel the
    // player's turn is always turn 1 — so a relic that speaks of "turn 2" has to count its own.
    private static IEffectNode<TContext> CountOwnTurn<TContext>(CounterId counter) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            Self, counter, new ConstantExpression<TContext>(1), relative: true);

    private static StatusTriggerData ClearLatch(CounterId id) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            SetCounter<TurnStartedTriggeredEffectContext>(id, 0)), nameof(TriggerEvent.TurnStarted));


    // ── The Deputy Undersecretary — the backlog ────────────────────────────────────────────────────────────

    // "At end of turn store up to 1 unspent Energy; gain stored Energy next turn."
    public static readonly StatusData UnfinishedDocket = Rule(
        "unfinished_docket", "Unfinished Docket",
        "One unspent Energy is carried into your next turn.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, Docket,
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                        new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                            Self, StandardCombatIds.EnergyResource)),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            // Carried into the next turn as HELD Energy: the turn's refill has already filled the pool, so
            // the stored point waits for the moment it can be spent.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(Docket, ComparisonOperator.Greater, 0),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Hold<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, Docket)),
                        SetCounter<CardsDrawnTriggeredEffectContext>(Docket, 0),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "At end of turn choose 1 non-Junk card to Retain; it costs 1 less next turn."
    //
    // ADAPTATION: the choice happens after the DRAW rather than at the turn's end, because by the time a
    // turn-end program runs there is no hand left to choose from (see the Backlog Counterseal). The card is
    // ribboned for the whole turn: play it and the ribbon is spent, keep it and it stays, one cheaper.
    public static readonly StatusData RedRibbonedMatter = Rule(
        "red_ribboned_matter", "Red-Ribboned Matter",
        "After your draw, keep one card in hand; it costs 1 less.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        HoldsACard<CardsDrawnTriggeredEffectContext>(),
                        // Only one card is under the ribbon at a time.
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                                Self, CardZone.Hand, mark: RibbonedMark),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        // Chosen ONCE and marked; every later step reads the mark, because asking a chooser
                        // twice asks the player twice.
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self,
                            new ChosenCardInZoneExpression<CardsDrawnTriggeredEffectContext>(
                                CardZone.Hand, "keep one card under the Red Ribbon"),
                            RibbonedMark),
                        new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                            Self, Ribboned<CardsDrawnTriggeredEffectContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self, Ribboned<CardsDrawnTriggeredEffectContext>(),
                            StandardCombatIds.RetainedCardMark),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "At end of turn gain 4 Block per unplayed non-Junk card, max 8."
    //
    // ADAPTATION: a turn-end program cannot see the hand — the engine discards it before the turn-end triggers
    // run (DiscardHandOnTurnEndedHandler is registered ahead of them). So the hand is written down as it
    // stands, at the draw and after every card played, and the turn's end pays out what was last written.
    public static readonly StatusData BacklogCounterseal = Rule(
        "backlog_counterseal", "Backlog Counterseal",
        "What you did not get to defends you: 4 Block per card left in hand, up to 8.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                RecordHand<CardsDrawnTriggeredEffectContext>()), nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                RecordHand<CardPlayedTriggeredEffectContext>()), nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new GainBlockNode<TurnEndedTriggeredEffectContext>(
                    Self,
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(8),
                        new MultiplyExpression<TurnEndedTriggeredEffectContext>(
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(4),
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, Backlog))))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // ── The Queue Commissioner — the queue ─────────────────────────────────────────────────────────────────

    // "At start of every third player turn: gain 1 Energy and draw 1."
    public static readonly StatusData BrassServiceBell = Rule(
        "brass_service_bell", "Brass Service Bell",
        "Every third turn the bell rings: 1 Energy and a card.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    CountOwnTurn<CardsDrawnTriggeredEffectContext>(BellTurn),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, BellTurn),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [Hold<CardsDrawnTriggeredEffectContext>(1), Draw<CardsDrawnTriggeredEffectContext>(1)])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "First time each turn total player-caused HP damage reaches 15+: gain 8 Block."
    public static readonly StatusData PrioritySash = Rule(
        "priority_sash", "Priority Sash",
        "The first time you deal 15 damage in a turn, you gain 8 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new DamageDealtThisTurnExpression<CardPlayedTriggeredEffectContext>(Self),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(15)),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(
                        Sash, Block<CardPlayedTriggeredEffectContext>(8)))),
                nameof(TriggerEvent.CardPlayed)),
            ClearLatch(Sash),
        ]);

    // "Enemy turn ends with no HP loss → advance. At 2, reset; next turn gain 1 Energy and draw 1."
    //
    // "No HP loss" is read the way the fight can read it: the health the player had when the enemies began is
    // written down, and compared when they are done.
    public static readonly StatusData IvoryNumberDisc = Rule(
        "ivory_number_disc", "Ivory Number Disc",
        "Two untouched enemy turns in a row and your number is called: 1 Energy and a card.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, DiscHealth,
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Self),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            // Read at the next draw: everything the enemies did lies between the two moments.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // The queue advances only if nothing got through; any loss sends you back to the end of it.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, DiscHealth),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(Self),
                                ComparisonOperator.GreaterOrEqual,
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, DiscHealth))),
                        AddCounter<CardsDrawnTriggeredEffectContext>(Disc, 1),
                        @else: SetCounter<CardsDrawnTriggeredEffectContext>(Disc, 0)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, Disc),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Hold<CardsDrawnTriggeredEffectContext>(1),
                            Draw<CardsDrawnTriggeredEffectContext>(1),
                            SetCounter<CardsDrawnTriggeredEffectContext>(Disc, 0),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Lord Sealkeeper — the three seals ──────────────────────────────────────────────────────────────

    // "At combat start gain 1 Energy and draw +1."
    public static readonly StatusData AccessSealShard = Once(
        "access_seal_shard", "Access Seal-Shard", "The door is already open: 1 Energy and a card.",
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [Hold<CardsDrawnTriggeredEffectContext>(1), Draw<CardsDrawnTriggeredEffectContext>(1)]),
        nameof(TriggerEvent.CardsDrawn));

    // "At combat start gain 8 Block; prevent first negative-status application each combat."
    public static readonly StatusData TestimonySealShard = Once(
        "testimony_seal_shard", "Testimony Seal-Shard",
        "8 Block as the fight opens, and the first thing done to you is refused.",
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Block<CardsDrawnTriggeredEffectContext>(8),
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                Self, new StatusDefinitionId(TestimonySealId),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
        ]), nameof(TriggerEvent.CardsDrawn));

    // The seal itself, standing between the player and the first debuff of the fight. The engine's own
    // debuff-block interceptor consumes the status that carries it, which is exactly "the FIRST one" — so the
    // guard is a status of its own rather than the relic's rule, which has to keep working afterwards.
    public static readonly StatusData TestimonySeal = new()
    {
        Id = TestimonySealId,
        NameKey = "Testimony Seal",
        DescriptionKey = "The next thing done to you is refused.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        DebuffBlock = new StatusDebuffBlockData([]),
    };

    // "First Attack/Deed-style damaging play each turn deals 4 additional total damage; multi-hit receives it
    // once." The bonus is a status the first Deed of the turn consumes — the same shape the card pools use for
    // "your next Deed hits harder", so a multi-hit card is buffed once rather than per hit.
    public static readonly StatusData ExecutionSealShard = Rule(
        "execution_seal_shard", "Execution Seal-Shard",
        "Your first Deed each turn deals 4 more damage.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    Self, new StatusDefinitionId(SealedStrikeId),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The sealed strike itself: +4 damage while it stands, and it stands until the first Deed spends it.
    public static readonly StatusData SealedStrike = new()
    {
        Id = SealedStrikeId,
        NameKey = "Sealed Strike",
        DescriptionKey = "Your next Deed deals 4 more damage.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            // Once per ACTION, so a multi-hit Deed collects the seal once, as the design says.
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, 4,
                RestrictDamageKind: DamageKind.Direct, RestrictSourceCardTag: CardAuthoring.DeedTag,
                OncePerAction: true),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(CardAuthoring.DeedTag)),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(SealedStrikeId)))),
                nameof(TriggerEvent.CardPlayed)),
        ],
    };

    // ── The Municipal Dragon — the writs ───────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: the design gives these two a FREE ACTION the player spends when they choose. The engine has
    // no player-activated relic, so each fires itself at the moment it would have been spent — the first time
    // in a fight the player runs out of Energy with cards still in hand. See ADAPTATIONS.md.

    // "Once per combat, free action: gain 2 Energy this turn."
    public static readonly StatusData StampedExpeditionWrit = Rule(
        "stamped_expedition_writ", "Stamped Expedition Writ",
        "The first time a fight leaves you out of Energy with cards in hand, the writ pays 2.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    SpentOut<CardPlayedTriggeredEffectContext>(ExpeditionDone),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        Energy<CardPlayedTriggeredEffectContext>(2),
                        SetCounter<CardPlayedTriggeredEffectContext>(ExpeditionDone, 1),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "Once per combat, free action: gain 1 Energy; your Attacks/Deeds ignore enemy Block for the rest of the
    // turn." Ignoring Block is not a modifier a status can carry, so the warrant strips the Block that is
    // standing instead — the same outcome for the turn it is spent in. See ADAPTATIONS.md.
    public static readonly StatusData CivicEntryWarrant = Rule(
        "civic_entry_warrant", "Civic Entry Warrant",
        "The first time a fight leaves you out of Energy with cards in hand: 1 Energy, and every enemy's guard drops.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    SpentOut<CardPlayedTriggeredEffectContext>(WarrantDone),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        Energy<CardPlayedTriggeredEffectContext>(1),
                        new ModifyDefensivePoolNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.AllEnemiesOfSource,
                            StandardCombatIds.BlockDefensivePool,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(-99)),
                        SetCounter<CardPlayedTriggeredEffectContext>(WarrantDone, 1),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "At combat start gain 8 Block; enemies reveal their following intent in addition to current intent."
    // The second intent is a frontend affordance the engine does not carry, so the charter ships as its Block
    // half. See ADAPTATIONS.md.
    public static readonly StatusData InspectorsBrassCharter = Once(
        "inspectors_brass_charter", "Inspector's Brass Charter",
        "8 Block as the fight opens.",
        Block<CardsDrawnTriggeredEffectContext>(8), nameof(TriggerEvent.CardsDrawn));

    // ── The Living Charter — what carries over ─────────────────────────────────────────────────────────────

    // "At end of turn retain up to 8 remaining Block for next turn."
    public static readonly StatusData ContinuanceFragment = Rule(
        "continuance_fragment", "Continuance Fragment",
        "Up to 8 Block survives into your next turn.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, Continuance,
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(8),
                        new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                            Self, StandardCombatIds.BlockDefensivePool)),
                    relative: false)),
                nameof(TriggerEvent.TurnEnded)),
            // Paid after the turn-start clear, which is what makes it a retention rather than a doubling.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                        Self,
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, Continuance)),
                    SetCounter<CardsDrawnTriggeredEffectContext>(Continuance, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "First time cumulative HP loss in a combat reaches 12: next turn gain 15 Block and draw 2."
    // The fight writes down the health it opened with, so "lost in this combat" is a subtraction.
    public static readonly StatusData RightOfRedress = Rule(
        "right_of_redress", "Right of Redress",
        "Once you have lost 12 HP in a fight, your next turn opens with 15 Block and two cards.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // The opening health, written once.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(RedressStart, ComparisonOperator.Equal, 0),
                        new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                            Self, RedressStart,
                            new CombatantCurrentHealthExpression<CardsDrawnTriggeredEffectContext>(Self),
                            relative: false)),
                    // The redress owed from what happened since.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(RedressPaid, ComparisonOperator.Equal, 1),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Block<CardsDrawnTriggeredEffectContext>(15),
                            Draw<CardsDrawnTriggeredEffectContext>(2),
                            SetCounter<CardsDrawnTriggeredEffectContext>(RedressPaid, 2),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Counter<TurnEndedTriggeredEffectContext>(RedressPaid, ComparisonOperator.Equal, 0),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new SubtractExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, RedressStart),
                                new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Self)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(12))),
                    SetCounter<TurnEndedTriggeredEffectContext>(RedressPaid, 1))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // "Once per combat after an enemy intent is revealed, replace it with another legal non-identical intent."
    // Choosing what an enemy does instead is not something a relic can reach — intents are the enemy's own
    // rotation. What the appeal keeps is its EFFECT on the fight: once per combat, the first enemy turn after
    // the opening lands defused. See ADAPTATIONS.md.
    public static readonly StatusData MarginOfAppeal = Rule(
        "margin_of_appeal", "Margin of Appeal",
        "Once a fight, the enemies' next turn is appealed: they deal half damage.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Counter<TurnEndedTriggeredEffectContext>(AppealDone, ComparisonOperator.Equal, 0),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.AllEnemiesOfSource,
                            new StatusDefinitionId(DefusedIntentId),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        SetCounter<TurnEndedTriggeredEffectContext>(AppealDone, 1),
                    ]))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // The appealed intent: halved output for the one turn it stands.
    public static readonly StatusData DefusedIntent = new()
    {
        Id = DefusedIntentId,
        NameKey = "Under Appeal",
        DescriptionKey = "This turn's action was appealed: it deals half damage.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesDuration = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.ScalePercent, 50, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(DefusedIntentId))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };


    // ── The Whispering Catalogue — the record it keeps of you ──────────────────────────────────────────────

    // "End turn classify Sparse (0–2 non-Junk) or Busy (3+). From turn 2: changed classification → next turn
    // +1 Energy; same → next turn +6 Block."
    public static readonly StatusData ErrataRibbon = Rule(
        "errata_ribbon", "Errata Ribbon",
        "Change your pace between turns for Energy; keep it for Block.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    // This turn's classification: 1 sparse, 2 busy.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            NonJunkPlayedThisTurn<TurnEndedTriggeredEffectContext>(),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(3)),
                        SetCounter<TurnEndedTriggeredEffectContext>(ErrataNow, 2),
                        @else: SetCounter<TurnEndedTriggeredEffectContext>(ErrataNow, 1)),
                    // Compared with the turn before — there is nothing to compare on turn 1.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, ErrataLast),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, ErrataNow),
                                ComparisonOperator.Equal,
                                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, ErrataLast)),
                            SetCounter<TurnEndedTriggeredEffectContext>(ErrataOwed, 2),
                            @else: SetCounter<TurnEndedTriggeredEffectContext>(ErrataOwed, 1))),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        Self, ErrataLast,
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, ErrataNow),
                        relative: false),
                ])),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(ErrataOwed, ComparisonOperator.Equal, 1),
                        Hold<CardsDrawnTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(ErrataOwed, ComparisonOperator.Equal, 2),
                        Block<CardsDrawnTriggeredEffectContext>(6)),
                    SetCounter<CardsDrawnTriggeredEffectContext>(ErrataOwed, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "First time each turn a non-Junk card differs in type from previous non-Junk card: draw 1 and gain 3
    // Block."
    public static readonly StatusData IndexOfContradictions = Rule(
        "index_of_contradictions", "Index of Contradictions",
        "The first time each turn you follow a card with one of another type: draw 1 and gain 3 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(Self, IndexLast),
                                    ComparisonOperator.Greater,
                                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                                new NotExpression<CardPlayedTriggeredEffectContext>(
                                    PlayedTypeIsCounter(IndexLast))),
                            OnceEachTurn<CardPlayedTriggeredEffectContext>(Index,
                                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                                [
                                    Draw<CardPlayedTriggeredEffectContext>(1),
                                    Block<CardPlayedTriggeredEffectContext>(3),
                                ]))),
                        RecordPlayedType(IndexLast),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
            ClearLatch(Index),
        ]);

    // "End turn 1 determine most-played non-Junk card type. From turn 2, first card of that Registered Type
    // each turn costs 1 less."
    public static readonly StatusData RegistryTab = Rule(
        "registry_tab", "Registry Tab",
        "What you played most on turn 1 is registered; its first card each turn costs 1 less.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Counter<TurnEndedTriggeredEffectContext>(Registered, ComparisonOperator.Equal, 0),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            NonJunkPlayedThisTurn<TurnEndedTriggeredEffectContext>(),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                    // Deed, then Working, then Rite: the first that is not beaten by a later one wins, and a
                    // tie goes to the earlier — the design lets the holder choose, and this chooses for them.
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        RegisterIfHighest(CardAuthoring.RiteTag, 3),
                        RegisterIfHighest(CardAuthoring.WorkingTag, 2),
                        RegisterIfHighest(CardAuthoring.DeedTag, 1),
                    ]))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    CheapenFirstOfType(1, CardAuthoring.DeedTag),
                    CheapenFirstOfType(2, CardAuthoring.WorkingTag),
                    CheapenFirstOfType(3, CardAuthoring.RiteTag),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Warden of Sealed Volumes — what is held back ───────────────────────────────────────────────────

    // "End turn with ≤2 non-Junk cards played: Seal the highest-base-cost non-Junk card remaining in hand
    // instead of discarding. Next turn return it; cost 0 that turn."
    //
    // ADAPTATION: "highest base cost" is not a question a rule can ask a zone — there is no "the dearest card
    // in hand" expression — so the first non-Junk card left in hand goes into custody. See ADAPTATIONS.md.
    public static readonly StatusData CustodyShackle = Rule(
        "custody_shackle", "Custody Shackle",
        "End a quiet turn and one card stays in hand, free next turn.",
        [
            // ADAPTATION: a turn-end program has no hand left to take a card from (see the Backlog
            // Counterseal), so the candidate goes into custody at the DRAW — and is let go again the moment
            // the turn turns out to be a busy one, which is the condition the design states.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    HoldsACard<CardsDrawnTriggeredEffectContext>(),
                    TakeIntoCustody<CardsDrawnTriggeredEffectContext>())),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn<CardPlayedTriggeredEffectContext>(),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                    ReleaseCustody<CardPlayedTriggeredEffectContext>())),
                nameof(TriggerEvent.CardPlayed)),
            ReleaseFromCustody(),
        ]);

    // "After opening draw choose 1 non-Junk card and Seal it; turn 2 return it to hand costing 0 that turn."
    public static readonly StatusData MasterReleaseKey = Rule(
        "master_release_key", "Master Release Key",
        "On the first turn, one card of your choice is held back — and comes back free.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(KeyUsed, ComparisonOperator.Equal, 0),
                        HoldsACard<CardsDrawnTriggeredEffectContext>()),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self,
                            new ChosenCardInZoneExpression<CardsDrawnTriggeredEffectContext>(
                                CardZone.Hand, "seal one card with the Master Release Key"),
                            CustodyMark),
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self, InCustody<CardsDrawnTriggeredEffectContext>(),
                            StandardCombatIds.RetainedCardMark),
                        SetCounter<CardsDrawnTriggeredEffectContext>(KeyUsed, 1),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
            ReleaseFromCustody(),
        ]);

    // "After normal draw mark a random playable base-cost-1+ non-Junk card as Evidence: cost −1 that turn and
    // gain 4 Block when played."
    public static readonly StatusData ReleaseTag = Rule(
        "release_tag", "Release Tag",
        "Each turn one card in hand is tagged: it costs 1 less and pays 4 Block when played.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    HoldsACard<CardsDrawnTriggeredEffectContext>(),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self,
                            new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(
                                Self, CardZone.Hand),
                            EvidenceMark),
                        new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                            Self, Evidence<CardsDrawnTriggeredEffectContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), EvidenceMark),
                    Block<CardPlayedTriggeredEffectContext>(4))),
                nameof(TriggerEvent.CardPlayed)),
            // The tag is this turn's: what was not played goes back to its own price.
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(
                    Self, CardZone.Hand,
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new SetCardInstanceMarkCounterNode<TurnStartedTriggeredEffectContext>(
                            Self, new IteratedCardExpression<TurnStartedTriggeredEffectContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                        new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                            Self, new IteratedCardExpression<TurnStartedTriggeredEffectContext>(),
                            EvidenceMark, remove: true),
                    ]),
                    markFilter: EvidenceMark)),
                nameof(TriggerEvent.TurnStarted)),
        ]);

    // ── The Curator of Misplaced Hours — time borrowed and owed ────────────────────────────────────────────

    // "Start turn based on previous turn: 0 non-Junk → 8 Block; 1–2 → +1 Energy; 3+ → draw 1. No turn-1
    // effect."
    public static readonly StatusData MisdatedPocketWatch = Rule(
        "misdated_pocket_watch", "Misdated Pocket Watch",
        "Your last turn decides this one: idle pays Block, measured pays Energy, busy pays a card.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new TurnNumberExpression<CardsDrawnTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            NonJunkPlayedLastTurn<CardsDrawnTriggeredEffectContext>(),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        Block<CardsDrawnTriggeredEffectContext>(8),
                        @else: new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                NonJunkPlayedLastTurn<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.LessOrEqual,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                            Hold<CardsDrawnTriggeredEffectContext>(1),
                            @else: Draw<CardsDrawnTriggeredEffectContext>(1))))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Once/turn with no outstanding debt: gain 1 Energy now. Next turn start with 1 less Energy, minimum 0,
    // and gain 4 Block; debt then clears."
    //
    // ADAPTATION: the design lets the holder decide when to borrow. Without a player-activated relic the
    // minute is taken as soon as it is available — at the draw, which is where the Energy is worth most.
    public static readonly StatusData BorrowedMinute = Rule(
        "borrowed_minute", "Borrowed Minute",
        "Borrow a minute of Energy each turn; the next turn pays it back with interest in Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Counter<CardsDrawnTriggeredEffectContext>(MinuteDebt, ComparisonOperator.Equal, 0),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Hold<CardsDrawnTriggeredEffectContext>(1),
                        SetCounter<CardsDrawnTriggeredEffectContext>(MinuteDebt, 1),
                    ]),
                    @else: new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new LoseResourceNode<CardsDrawnTriggeredEffectContext>(
                            Self, StandardCombatIds.EnergyResource,
                            new MinExpression<CardsDrawnTriggeredEffectContext>(
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1),
                                new CombatantCurrentResourceExpression<CardsDrawnTriggeredEffectContext>(
                                    Self, StandardCombatIds.EnergyResource))),
                        Block<CardsDrawnTriggeredEffectContext>(4),
                        SetCounter<CardsDrawnTriggeredEffectContext>(MinuteDebt, 0),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Turn 2 draw +2; Turn 3 gain 2 Energy; Turn 4 gain 15 Block."
    public static readonly StatusData DeferredAppointmentBook = Rule(
        "deferred_appointment_book", "Deferred Appointment Book",
        "Appointments come due on turns two, three and four.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    CountOwnTurn<CardsDrawnTriggeredEffectContext>(AppointmentTurn),
                    OnOwnTurn(2, Draw<CardsDrawnTriggeredEffectContext>(2)),
                    OnOwnTurn(3, Hold<CardsDrawnTriggeredEffectContext>(2)),
                    OnOwnTurn(4, Block<CardsDrawnTriggeredEffectContext>(15)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The Auditor of Returned Lives — what is on the record ──────────────────────────────────────────────

    // "First time each turn you play a card name already played earlier this combat: draw 1. If not triggered
    // that turn, gain 5 Block at end."
    //
    // ADAPTATION: nothing in a fight remembers which card NAMES have been played — the stats it keeps are per
    // turn and per type. So the repeat that counts is the second card of the same TYPE in a turn, which is the
    // same shape of play the design is rewarding. See ADAPTATIONS.md.
    public static readonly StatusData IdentityWrit = Rule(
        "identity_writ", "Identity Writ",
        "Repeat yourself once a turn and draw a card; go without and take 5 Block instead.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(PlayedNonJunk(), PlayedTypeRepeats()),
                    OnceEachTurn<CardPlayedTriggeredEffectContext>(Writ,
                        Draw<CardPlayedTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Counter<TurnEndedTriggeredEffectContext>(Writ, ComparisonOperator.Equal, 0),
                    Block<TurnEndedTriggeredEffectContext>(5))),
                nameof(TriggerEvent.TurnEnded)),
            ClearLatch(Writ),
        ]);

    // "Track Energy actually spent on cards. Every 4 spent → gain 1 Energy; excess carries within combat."
    public static readonly StatusData SettledLedger = Rule(
        "settled_ledger", "Settled Ledger",
        "Every 4 Energy you spend, the ledger returns 1.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        Self, Ledger,
                        new CardCostExpression<CardPlayedTriggeredEffectContext>(
                            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                            StandardCombatIds.EnergyResource),
                        relative: true),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Counter<CardPlayedTriggeredEffectContext>(Ledger, ComparisonOperator.GreaterOrEqual, 4),
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            AddCounter<CardPlayedTriggeredEffectContext>(Ledger, -4),
                            Energy<CardPlayedTriggeredEffectContext>(1),
                        ])),
                ])),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // ── The Grand Cross-Reference — one card read against the next ─────────────────────────────────────────

    // "First non-Junk card each turn is Premise. Next non-Junk: different type → costs 1 less; same type →
    // gain 6 Block when played. Then Premise expires."
    //
    // The discount cannot wait to see which card comes next, so the moment the Premise is played every card in
    // hand of ANOTHER type is marked down — which is exactly the condition the design states, applied to the
    // hand instead of to the choice.
    public static readonly StatusData PremiseSlip = Rule(
        "premise_slip", "Premise Slip",
        "Your first card each turn is a premise: follow it with another type for a discount, or the same type for Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Counter<CardPlayedTriggeredEffectContext>(Premise, ComparisonOperator.Equal, 0),
                        // The premise itself: recorded, and the hand is marked against it.
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            RecordPlayedType(Premise),
                            CheapenOtherTypes(CardAuthoring.DeedTag, 1),
                            CheapenOtherTypes(CardAuthoring.WorkingTag, 2),
                            CheapenOtherTypes(CardAuthoring.RiteTag, 3),
                        ]),
                        // The card that answers it.
                        @else: new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                                PlayedTypeIsCounter(Premise),
                                Block<CardPlayedTriggeredEffectContext>(6)),
                            ExpirePremise(),
                            SetCounter<CardPlayedTriggeredEffectContext>(Premise, -1),
                        ])))),
                nameof(TriggerEvent.CardPlayed)),
            // A new turn, a new premise.
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    ExpirePremiseAt<TurnStartedTriggeredEffectContext>(),
                    SetCounter<TurnStartedTriggeredEffectContext>(Premise, 0),
                ])),
                nameof(TriggerEvent.TurnStarted)),
        ]);

    // "After normal draw link a random playable non-Junk hand card to the next non-Junk draw-pile card.
    // Playing the linked hand card draws the referenced card; it costs 1 less that turn. One link per turn."
    public static readonly StatusData ConcordanceThread = Rule(
        "concordance_thread", "Concordance Thread",
        "One card in hand is threaded to the deck: playing it draws its reference, one cheaper.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    HoldsACard<CardsDrawnTriggeredEffectContext>(),
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self,
                        new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(
                            Self, CardZone.Hand),
                        ThreadMark))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), ThreadMark),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                            Self, new ConstantExpression<CardPlayedTriggeredEffectContext>(1),
                            resultKey: Referenced),
                        // The card the thread pulled in, one cheaper for this turn.
                        new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                            Self, new DrawCardOutcomeExpression<CardPlayedTriggeredEffectContext>(Referenced),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(-1), relative: true),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "End turn record last non-Junk type. Next turn: Deed → first damaging Deed +8 total damage; Working →
    // +8 Block; Rite → draw +1. No card → no bonus."
    public static readonly StatusData ConclusionLeaf = Rule(
        "conclusion_leaf", "Conclusion Leaf",
        "How a turn ended decides how the next one opens.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedNonJunk(), RecordPlayedType(Conclusion))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(Conclusion, ComparisonOperator.Equal, 1),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            Self, new StatusDefinitionId(ConcludedStrikeId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(Conclusion, ComparisonOperator.Equal, 2),
                        Block<CardsDrawnTriggeredEffectContext>(8)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        Counter<CardsDrawnTriggeredEffectContext>(Conclusion, ComparisonOperator.Equal, 3),
                        Draw<CardsDrawnTriggeredEffectContext>(1)),
                    SetCounter<CardsDrawnTriggeredEffectContext>(Conclusion, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The conclusion's strike: +8 on one Deed, spent by the first one played.
    public static readonly StatusData ConcludedStrike = new()
    {
        Id = ConcludedStrikeId,
        NameKey = "Concluded",
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
                    new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(CardAuthoring.DeedTag)),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(ConcludedStrikeId)))),
                nameof(TriggerEvent.CardPlayed)),
        ],
    };

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
