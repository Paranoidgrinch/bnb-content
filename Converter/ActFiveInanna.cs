using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V, the second god — INANNA, MISTRESS OF THE EANNA LEDGER. She claims, and what she claims still works.
//
// She does not take your cards. She ENTERS them: a claimed card is stamped PROPERTY OF EANNA and stays in your
// deck, stays playable, and is CHEAPER — the first time you play each copy each turn it costs one Energy less.
// Every use of it writes one TEMPLE DUE into the ledger. That is the whole fight in one line: the strongest
// thing you own has been made better, and using it is how the debt grows.
//
//   Temple Property   the copies she has entered — visible on the card and counted on your own chip
//   Temple Due        what this fight has run up so far
//   Procession        the turns until it is collected
//   Arrears           what you did not pay, for the rest of the fight
//
// You may settle it. OFFER THE SURPLUS spends one Energy for one Due — the pool is its own cap, so what you
// pay with is the turn you did not spend attacking. DEDICATE A WORK gives Eanna a card out of your hand for
// the rest of the fight: rubbish is worth nothing to her, an ordinary card one, and a card she has already
// claimed FOUR, because Eanna wants value and not garbage. What is dedicated comes back when she is dead.
//
// At the Procession the ledger is read. Everything unpaid becomes ARREARS, which do not decay, do not cleanse,
// and make Demand the Arrears worse every time it comes round.
//
// Three phases. WHAT YOU HOLD (100–70 %) is the ledger above, and she may hold no more than two claims at
// once. WHAT YOU ACCUMULATE (70–35 %) opens the storehouse: she stops claiming only cards and starts claiming
// SURPLUS — one claim a turn, rotating, each of them a threshold on something you produce (energy, hand,
// block, damage) above which the excess is Due. Her cap rises to four, and the first claimed card each turn
// is answered with SPLENDOR: Eanna makes its property magnificent. And at 35 %:
//
//   ALL THINGS ENTER EANNA — three turns in which every card you own is Temple Property.
//
// Everything is a Energy cheaper and everything writes Due. Play conservatively and she kills you with the
// turns; exploit the discount and the final Procession collects the whole of it at once, ignoring Block.
public static partial class ActFive
{
    public const string InannaEnemyId = "inanna_mistress_of_the_eanna_ledger";
    public const string InannaEncounterId = "act_5_inanna_mistress_of_the_eanna_ledger";

    // Her rule, worn from the first round; every trigger below hangs off it.
    public const string EannaLedgerId = "the_eanna_ledger";

    // The ledger's four rows. Temple Due and Arrears are NEUTRAL on purpose, and for the reason Nisaba's
    // Indelible is: a debt marked as a debuff is a debt any ordinary cleanse settles for free, and paying is
    // the whole of what this fight asks.
    public const string TempleDueId = "temple_due";
    public const string ArrearsId = "arrears";
    public const string TemplePropertyId = "property_of_eanna";
    public const string ProcessionId = "the_procession";
    public const string ProcessionCalledId = "the_procession_is_called";

    // The two later phases and the announcement that precedes each — the Scribe's idiom, one turn of warning
    // before the intent that carries it out.
    public const string StorehouseId = "the_open_storehouse";
    public const string StorehouseAnnouncedId = "the_storehouse_is_opened";
    public const string AllThingsId = "all_things_enter_eanna";
    public const string AllThingsAnnouncedId = "all_things_are_called";

    // What the player wears: the promise she made about their next card, the reward for using her property,
    // and the four surplus claims (only ever one at a time).
    public const string FirstGiftId = "the_first_gift";
    public const string SplendorId = "splendor_of_eanna";
    public const string ClaimOfGrainId = "claim_of_grain";
    public const string ClaimOfHandsId = "claim_of_hands";
    public const string ClaimOfWallsId = "claim_of_walls";
    public const string ClaimOfVictoryId = "claim_of_victory";

    // Her own sheets carry it, so the ledger never claims, counts or charges for its own paperwork.
    public const string EannaSheetTag = "eanna_sheet";

    public const string OfferSurplusCardId = "offer_the_surplus";
    public const string DedicateWorkCardId = "dedicate_a_work";

    // 760, and higher than Nisaba's 620 for a reason that is hers alone: HER OWN MECHANIC MAKES THE PLAYER
    // FASTER. Every claim is a discount, so the deck that is fighting her is cheaper than the deck that
    // fought anything else — the walker took her from 600 to 243 in FOUR turns, and a god whose third phase
    // is reached in the round she dies has two phases the player never sees. Nisaba is long because she
    // cannot be shortened (the Indelible); Inanna has no such floor, so her length has to be bought.
    public const int InannaMaxHealth = 760;
    private const int StorehouseAt = 532;  // 70 % of 760
    private const int AllThingsAt = 266;   // 35 % of 760
    private const int AllThingsTurns = 3;
    private const int ProcessionEvery = 3;
    private const int ClaimCap = 2;
    private const int StorehouseClaimCap = 4;

    // What the temple allows for a dedicated card. Eanna wants value, not garbage.
    private const int DedicatedClaimedWorth = 4;
    private const int DedicatedWorkWorth = 1;

    // THE STAMP ITSELF, on one copy of one card. Not a tag — a tag is what a card IS, and this is what has
    // been done to this copy; the discount below is read off the same card, by the engine's own per-instance
    // price. Beside it she keeps a use count on every card the player plays, which is her whole answer to
    // "the card you have played most often": she does not search a history, she writes in the margin.
    public static TagId ClaimMark { get; } = new("eanna_claim");
    private static TagId DedicateMark { get; } = new("eanna_dedicated");
    private static CounterId UseCounter => new("eanna_uses");

    // Counters. What this turn has already been given, where a claim pass got to, which surplus is next, and
    // whether a phase announcement has already been made.
    private static CounterId SplendorGiven => new("inanna_splendor_given");
    private static CounterId ClaimBest => new("inanna_claim_best");
    private static CounterId ClaimPlaced => new("inanna_claim_placed");
    private static CounterId SurplusTurn => new("inanna_surplus_turn");
    private static CounterId StorehouseTaken => new("inanna_storehouse_taken");
    private static CounterId AllThingsTaken => new("inanna_all_things_taken");

    // The player's running totals for the turn, one per surplus claim. Each is reset by the ledger at the
    // player's turn start, and each is the number its claim has ALREADY charged against.
    private static CounterId GrainCounter => new("eanna_grain");
    private static CounterId HandsCounter => new("eanna_hands");
    private static CounterId WallsCounter => new("eanna_walls");
    private static CounterId VictoryCounter => new("eanna_victory");

    // The thresholds. Generous by design (boss master §7.9: "she does not stop the player from producing
    // these resources; she only records sacred claim over the surplus"), so an ordinary turn is free and only
    // an engine pays.
    private static readonly (int Threshold, int Batch) Grain = (6, 1);
    private static readonly (int Threshold, int Batch) Hands = (7, 2);
    private static readonly (int Threshold, int Batch) Walls = (25, 5);
    private static readonly (int Threshold, int Batch) Victory = (40, 10);

    private static ICombatantTargetSelector Mistress => Bearer(EannaLedgerId);

    private static readonly CardZone[] DeckZones = [CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile];

    // HOW MANY CARDS A LOOP MAY WALK, and the walker is why this number is written down. The engine's default
    // ceiling is 64, which is a guard against a runaway program and not a statement about deck size — and a
    // deck five acts deep is bigger than that. Two of three whole-game walks died in Act V on
    // "resolved 78 cards which exceeds the configured maximum of 64", because every loop here is over the
    // PLAYER'S OWN DECK rather than over some bounded set the fight produced.
    private const int WholeDeck = 256;

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> InannaStatuses() =>
    [
        TheEannaLedger(),
        TempleDue(), Arrears(), TempleProperty(),
        Procession(), Announcement(ProcessionCalledId, "The Procession Is Called",
            "Her next act collects. Everything still owed becomes Arrears, and Arrears do not go away."),
        TheStorehouse(), Announcement(StorehouseAnnouncedId, "The Storehouse Opens",
            "She has stopped counting cups. She is about to start counting what overflows them."),
        AllThings(), Announcement(AllThingsAnnouncedId, "All Things Enter Eanna",
            "She is about to stop dividing the world into yours and hers."),
        FirstGift(), Splendor(),
        ClaimOfGrain(), ClaimOfHands(), ClaimOfWalls(), ClaimOfVictory(),
    ];

    public static IReadOnlyList<CardData> InannaLedgerCards() => [OfferTheSurplus(), DedicateAWork()];

    public static EffectProgram<EnemyActionContext>? InannaIntent(string enemyId, string intentId) =>
        enemyId != InannaEnemyId ? null : intentId switch
        {
            "raise_the_standard" => new EffectProgram<EnemyActionContext>(Seq(Hit(20), Guard(8))),
            // The finest work she can see, and the one she has watched you lean on. Both are HER reading of
            // your hand rather than a search of your deck: a claim the player cannot see coming is a claim
            // the player cannot plan around (§7.2, "claims should be predictable or clearly telegraphed").
            "claim_the_finest_work" => new EffectProgram<EnemyActionContext>(
                Seq(Hit(14), Claim(card => new CardInstanceBaseCostExpression<EnemyActionContext>(
                    card, StandardCombatIds.EnergyResource)))),
            "claim_the_favored_work" => new EffectProgram<EnemyActionContext>(
                Seq(Hit(14), Claim(card => new CardInstanceMarkCounterExpression<EnemyActionContext>(
                    card, UseCounter)))),
            "claim_the_first_gift" => ClaimTheFirstGift(),
            "demand_the_arrears" => DemandTheArrears(),
            // Her quiet turn, and the only one that costs no HP: the temple grows, and the supplicant is
            // invited to admire it.
            "adorn_eanna" => new EffectProgram<EnemyActionContext>(
                Seq(Hit(12), Debuff(Cards.Keywords.Doubt, 2))),
            "call_the_procession" => CallTheProcession(),
            "open_the_storehouse" => OpenTheStorehouse(),
            "all_things_enter_eanna" => AllThingsEnterEanna(),
            _ => null,
        };

    // ── the ledger, as faces ──────────────────────────────────────────────────────────────────────────────

    public static StatusData TheEannaLedger() => new()
    {
        Id = EannaLedgerId,
        NameKey = "The Eanna Ledger",
        DescriptionKey =
            "Cards she claims are stamped PROPERTY OF EANNA. They stay yours to play and their first play "
            + "each turn costs 1 Energy less — and every use of one writes 1 Temple Due. At the Procession "
            + "what is unpaid becomes Arrears.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(OpenTheLedger(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(EnterThePlays(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(CloseTheLedger(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheLedgerAnnouncements(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    private static StatusData TempleDue() => Face(
        TempleDueId, "Temple Due",
        "What this fight has run up. At the Procession every point still standing becomes Arrears. Offer "
        + "the Surplus pays 1 for 1 Energy; Dedicate a Work pays more.", stacks: true);

    private static StatusData Arrears() => Face(
        ArrearsId, "Arrears",
        "Due you did not pay. It does not decay and it does not cleanse — she carries it to the end of the "
        + "fight, and Demand the Arrears is worse for every point of it.", stacks: true);

    private static StatusData TempleProperty() => Face(
        TemplePropertyId, "Property of Eanna",
        "How many of your cards are entered in the ledger. Each is 1 Energy cheaper the first time you play "
        + "it each turn, and each use writes 1 Temple Due.", stacks: true);

    private static StatusData Procession() => Face(
        ProcessionId, "Procession",
        "Rounds until the ledger is collected. When it arrives, everything still owed becomes Arrears.",
        stacks: true);

    private static StatusData TheStorehouse() => Face(
        StorehouseId, "The Open Storehouse",
        "She claims surplus as well as cards: one claim a turn on something you produce, and everything you "
        + "make above its line is Due. She may now hold four claims, and pays the first property card you "
        + "play each turn with Splendor.", stacks: false);

    private static StatusData AllThings() => Face(
        AllThingsId, "All Things Enter Eanna",
        "Turns remaining. Every card you own is Temple Property: 1 Energy cheaper on its first play each "
        + "turn, and 1 Temple Due for every play. When the count runs out the whole ledger is collected at "
        + "once, and Block has nothing to say about it.", stacks: true);

    private static StatusData FirstGift() => Face(
        FirstGiftId, "The First Gift",
        "The next card you play becomes Property of Eanna. Which one is entirely your choice.", stacks: false);

    private static StatusData Splendor() => Worn(
        SplendorId, "Splendor of Eanna",
        "Eanna makes its property magnificent: your damage is 4 higher for the rest of this turn.",
        StatusPolarity.Buff,
        new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, 4,
            RestrictDamageKind: null));

    // ── the surplus claims ────────────────────────────────────────────────────────────────────────────────

    // Each is worn by the PLAYER and listens to the player's own events, which is what makes it readable: the
    // chip that charges you sits on the side that produced the surplus. All four are the same arithmetic —
    // a running total for the turn, a line it is free below, and one Due per batch above it — so a player who
    // has learnt one has learnt the other three.
    private static StatusData ClaimOfGrain() => SurplusClaim(
        ClaimOfGrainId, "Claim of Grain",
        $"Energy you gain beyond {Grain.Threshold} this turn is Eanna's: 1 Temple Due for each point above it.",
        nameof(TriggerEvent.ResourceGained), GrainCounter, Grain,
        Running<ResourceGainedTriggeredEffectContext>(GrainCounter, Sum: true));

    private static StatusData ClaimOfHands() => SurplusClaim(
        ClaimOfHandsId, "Claim of Hands",
        $"A hand held above {Hands.Threshold} cards is Eanna's: 1 Temple Due for every {Hands.Batch} cards above it.",
        nameof(TriggerEvent.CardsDrawn), HandsCounter, Hands,
        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, CardZone.Hand));

    private static StatusData ClaimOfWalls() => SurplusClaim(
        ClaimOfWallsId, "Claim of Walls",
        $"Block you gain beyond {Walls.Threshold} this turn is Eanna's: 1 Temple Due for every {Walls.Batch} above it.",
        nameof(TriggerEvent.BlockGained), WallsCounter, Walls,
        Running<BlockGainedTriggeredEffectContext>(WallsCounter, Sum: true));

    private static StatusData ClaimOfVictory() => SurplusClaim(
        ClaimOfVictoryId, "Claim of Victory",
        $"Damage you deal beyond {Victory.Threshold} this turn is Eanna's: 1 Temple Due for every {Victory.Batch} above it.",
        nameof(TriggerEvent.DamageDealt), VictoryCounter, Victory,
        Running<DamageDealtTriggeredEffectContext>(VictoryCounter, Sum: true));

    // "What the running total WILL be once this event is counted." For grain, walls and victory that is the
    // total so far plus what just happened; for hands it is simply the hand, which is a high-water mark
    // rather than a sum and needs no adding.
    private static ICombatExpression<TContext, int> Running<TContext>(CounterId counter, bool Sum)
        where TContext : class =>
        Sum
            ? new AddExpression<TContext>(
                new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter),
                new EventAmountExpression<TContext>())
            : new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter);

    private static StatusData SurplusClaim<TContext>(
        string id, string name, string description, string trigger,
        CounterId counter, (int Threshold, int Batch) line,
        ICombatExpression<TContext, int> running) where TContext : class
    {
        // Due for everything above the line that has not been charged yet: what the new total owes, less what
        // the old one already did. Written as a difference rather than as a remainder so a batch straddling
        // two events is charged exactly once.
        ICombatExpression<TContext, int> Owed(ICombatExpression<TContext, int> total) =>
            new DivideExpression<TContext>(
                new MaxExpression<TContext>(
                    Const<TContext>(0),
                    new SubtractExpression<TContext>(total, Const<TContext>(line.Threshold))),
                Const<TContext>(line.Batch));

        var already = new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter);
        var due = new SubtractExpression<TContext>(Owed(running), Owed(already));

        var program = new EffectProgram<TContext>(
            new CausalSequenceEffectNode<TContext>(
            [
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(due, ComparisonOperator.Greater, Const<TContext>(0)),
                    new ApplyStatusNode<TContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(TempleDueId), due,
                        sourceSelector: Mistress)),
                // The total only ever climbs: a hand that shrinks has already been claimed at its widest.
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(running, ComparisonOperator.Greater, already),
                    new SetCombatantCounterNode<TContext>(
                        CombatantTargetSelectors.Source, counter, running, relative: false)),
            ]));

        var face = Face(id, name, description, stacks: false);
        return face with { Triggers = [Trigger(program, trigger)] };
    }

    // ── the ledger's turn ─────────────────────────────────────────────────────────────────────────────────

    // The player's turn opens: the discount is written back onto every claimed copy, the ledger's own rows are
    // brought up to date, the surplus claim of the turn is posted, and the two ways of paying are laid in hand.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheLedger()
    {
        var owed = Stacks<TurnStartedTriggeredEffectContext>(Applicant, TempleDueId);

        IEffectNode<TurnStartedTriggeredEffectContext> Sheet(string cardId) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    owed, ComparisonOperator.GreaterOrEqual, Const<TurnStartedTriggeredEffectContext>(1)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(cardId), CardZone.Hand,
                    Const<TurnStartedTriggeredEffectContext>(1)));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Mistress, SplendorGiven, Const<TurnStartedTriggeredEffectContext>(0), relative: false),
                    .. new[] { GrainCounter, HandsCounter, WallsCounter, VictoryCounter }.Select(counter =>
                        (IEffectNode<TurnStartedTriggeredEffectContext>)
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            Applicant, counter, Const<TurnStartedTriggeredEffectContext>(0), relative: false)),
                    // The third phase, before the discount is written: a card claimed this instant is a card
                    // whose first play this turn is already a Energy cheaper.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Has<TurnStartedTriggeredEffectContext>(Mistress, AllThingsId),
                        ClaimEverything<TurnStartedTriggeredEffectContext>()),
                    RenewTheDiscount<TurnStartedTriggeredEffectContext>(),
                    Recount<TurnStartedTriggeredEffectContext>(),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Has<TurnStartedTriggeredEffectContext>(Mistress, StorehouseId),
                        PostTheSurplusClaim()),
                    Sheet(OfferSurplusCardId),
                    Sheet(DedicateWorkCardId),
                ])));
    }

    // THE DISCOUNT, written back onto each claimed copy once a turn. The engine's per-instance price is
    // CONSUMED by the play that uses it, which is exactly the audit rule §7.1 asks for and the reason a card
    // retrieved and replayed in the same turn does not get it twice: nothing here refreshes it mid-turn.
    private static IEffectNode<TContext> RenewTheDiscount<TContext>() where TContext : class =>
        new SequenceEffectNode<TContext>(
        [
            .. DeckZones.Select(zone => (IEffectNode<TContext>)new ForEachCardInZoneNode<TContext>(
                Applicant, zone,
                new SetCardInstanceMarkCounterNode<TContext>(
                    Applicant, new IteratedCardExpression<TContext>(),
                    StandardCombatIds.CardCostDeltaCounter, Const<TContext>(-1), relative: false),
                maxIterations: WholeDeck, markFilter: ClaimMark)),
        ]);

    // Every card in the deck, hers. Her own sheets are not cards the player owns and are skipped.
    private static IEffectNode<TContext> ClaimEverything<TContext>() where TContext : class =>
        new SequenceEffectNode<TContext>(
        [
            .. DeckZones.Select(zone => (IEffectNode<TContext>)new ForEachCardInZoneNode<TContext>(
                Applicant, zone,
                new ConditionalEffectNode<TContext>(
                    new NotExpression<TContext>(
                        new CardInstanceHasTagExpression<TContext>(
                            new IteratedCardExpression<TContext>(), new TagId(EannaSheetTag))),
                    new MarkCardInstanceNode<TContext>(
                        Applicant, new IteratedCardExpression<TContext>(), ClaimMark,
                        sourceSelector: Mistress)),
                maxIterations: WholeDeck)),
        ]);

    private static IEffectNode<TContext> StrikeEveryClaim<TContext>() where TContext : class =>
        new SequenceEffectNode<TContext>(
        [
            .. DeckZones.Select(zone => (IEffectNode<TContext>)new ForEachCardInZoneNode<TContext>(
                Applicant, zone,
                new CausalSequenceEffectNode<TContext>(
                [
                    new MarkCardInstanceNode<TContext>(
                        Applicant, new IteratedCardExpression<TContext>(), ClaimMark, remove: true),
                    // …and the price with the stamp. The discount is a promise that lives on the copy until
                    // something spends it, so a card released from the ledger that kept it would be a card
                    // wearing no stamp and still charging a temple price.
                    new SetCardInstanceMarkCounterNode<TContext>(
                        Applicant, new IteratedCardExpression<TContext>(),
                        StandardCombatIds.CardCostDeltaCounter, Const<TContext>(0), relative: false),
                ]),
                maxIterations: WholeDeck, markFilter: ClaimMark)),
        ]);

    // How many copies stand in the ledger, and the row that says so. A count kept as a status rather than
    // only as marks, because the marks are on the cards and the player is being asked to plan against a TOTAL.
    private static ICombatExpression<TContext, int> ClaimedCount<TContext>() where TContext : class =>
        DeckZones
            .Select(zone => (ICombatExpression<TContext, int>)new CombatantZoneCardCountExpression<TContext>(
                Applicant, zone, mark: ClaimMark))
            .Aggregate((a, b) => new AddExpression<TContext>(a, b));

    private static IEffectNode<TContext> Recount<TContext>() where TContext : class
    {
        var count = ClaimedCount<TContext>();
        return new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(Applicant, new StatusDefinitionId(TemplePropertyId)),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(count, ComparisonOperator.Greater, Const<TContext>(0)),
                new ApplyStatusNode<TContext>(
                    Applicant, new StatusDefinitionId(TemplePropertyId), count, sourceSelector: Mistress)),
        ]);
    }

    // ONE surplus claim at a time (§7.9), and a different one each turn, in a fixed order the player can
    // learn. Rotation by a counter rather than by a roll: what she claims next is knowable, which is what
    // makes an engine deck plannable against her at all.
    private static IEffectNode<TurnStartedTriggeredEffectContext> PostTheSurplusClaim()
    {
        string[] claims = [ClaimOfGrainId, ClaimOfHandsId, ClaimOfWallsId, ClaimOfVictoryId];

        var turn = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Mistress, SurplusTurn),
            Const<TurnStartedTriggeredEffectContext>(claims.Length));

        return new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            .. claims.Select(id => (IEffectNode<TurnStartedTriggeredEffectContext>)
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(id))),
            .. claims.Select((id, index) => (IEffectNode<TurnStartedTriggeredEffectContext>)
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        turn, ComparisonOperator.Equal, Const<TurnStartedTriggeredEffectContext>(index)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(id), Const<TurnStartedTriggeredEffectContext>(1),
                        sourceSelector: Mistress))),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Mistress, SurplusTurn, Const<TurnStartedTriggeredEffectContext>(1), relative: true),
        ]);
    }

    // ── a card is played ──────────────────────────────────────────────────────────────────────────────────

    // Her margin note on every card the player uses, the gift she promised being taken, the Due the use
    // writes, and — once the storehouse is open — the Splendor the first property card of the turn is paid
    // with. Her own sheets are none of her business.
    private static EffectProgram<CardPlayedTriggeredEffectContext> EnterThePlays()
    {
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var claimed = new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, ClaimMark);

        var theGift = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            Has<CardPlayedTriggeredEffectContext>(Applicant, FirstGiftId),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                    Applicant, played, ClaimMark, sourceSelector: Mistress),
                new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(FirstGiftId)),
                Recount<CardPlayedTriggeredEffectContext>(),
            ]));

        var theSplendor = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Has<CardPlayedTriggeredEffectContext>(Mistress, StorehouseId),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(Mistress, SplendorGiven),
                    ComparisonOperator.Equal, Const<CardPlayedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(SplendorId),
                    Const<CardPlayedTriggeredEffectContext>(1), sourceSelector: Mistress),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    Mistress, SplendorGiven, Const<CardPlayedTriggeredEffectContext>(1), relative: false),
            ]));

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(EannaSheetTag)))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                        Applicant, played, UseCounter, Const<CardPlayedTriggeredEffectContext>(1),
                        relative: true),
                    theGift,
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        claimed,
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(TempleDueId),
                                Const<CardPlayedTriggeredEffectContext>(1), sourceSelector: Mistress),
                            theSplendor,
                        ])),
                ])));
    }

    // ── the turn closes ───────────────────────────────────────────────────────────────────────────────────

    // Her window ends, and both counts move: the Procession walks towards collection, and the third phase
    // walks towards the collection that cannot be argued with.
    private static EffectProgram<TurnEndedTriggeredEffectContext> CloseTheLedger()
    {
        var procession = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Stacks<TurnEndedTriggeredEffectContext>(Mistress, ProcessionId),
                ComparisonOperator.GreaterOrEqual, Const<TurnEndedTriggeredEffectContext>(2)),
            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                Mistress, new StatusDefinitionId(ProcessionId), Const<TurnEndedTriggeredEffectContext>(-1)),
            // At one it is not decremented but CALLED, so the player gets a whole turn between the warning
            // and the reading — which is the turn the paying is meant to happen in.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Has<TurnEndedTriggeredEffectContext>(Mistress, ProcessionId),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Mistress, new StatusDefinitionId(ProcessionId)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        Mistress, new StatusDefinitionId(ProcessionCalledId),
                        Const<TurnEndedTriggeredEffectContext>(1), sourceSelector: Mistress),
                ])));

        var allThings = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Stacks<TurnEndedTriggeredEffectContext>(Mistress, AllThingsId),
                ComparisonOperator.GreaterOrEqual, Const<TurnEndedTriggeredEffectContext>(2)),
            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                Mistress, new StatusDefinitionId(AllThingsId), Const<TurnEndedTriggeredEffectContext>(-1)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Has<TurnEndedTriggeredEffectContext>(Mistress, AllThingsId),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    TheFinalProcession(),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Mistress, new StatusDefinitionId(AllThingsId)),
                ])));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>([]),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>([procession, allThings])));
    }

    // THE FINAL PROCESSION. Everything still owed becomes Arrears, and then the whole of the Arrears is
    // collected at once — not damage but a debt being taken, which is why no Block answers it. Afterwards
    // every seal is struck: the temple has what it came for, and the fight that is left is an ordinary one.
    private static IEffectNode<TurnEndedTriggeredEffectContext> TheFinalProcession()
    {
        var arrears = Stacks<TurnEndedTriggeredEffectContext>(Applicant, ArrearsId);

        return new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            Collect<TurnEndedTriggeredEffectContext>(),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    arrears, ComparisonOperator.GreaterOrEqual, Const<TurnEndedTriggeredEffectContext>(1)),
                new DealDamageNode<TurnEndedTriggeredEffectContext>(
                    Applicant, arrears, ignoresBlock: true, kind: DamageKind.DamageOverTime)),
            StrikeEveryClaim<TurnEndedTriggeredEffectContext>(),
            Recount<TurnEndedTriggeredEffectContext>(),
        ]);
    }

    // The reading itself: unpaid Due becomes Arrears one for one, and the ledger opens again at zero.
    private static IEffectNode<TContext> Collect<TContext>() where TContext : class
    {
        var owed = Stacks<TContext>(Applicant, TempleDueId);
        return new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(owed, ComparisonOperator.GreaterOrEqual, Const<TContext>(1)),
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(
                    Applicant, new StatusDefinitionId(ArrearsId), owed, sourceSelector: Mistress),
                new RemoveStatusNode<TContext>(Applicant, new StatusDefinitionId(TempleDueId)),
            ]));
    }

    // Her own blood announces both later phases, one turn before the intent that carries them out.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheLedgerAnnouncements()
    {
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(Mistress);

        IEffectNode<DamageReceivedTriggeredEffectContext> Announce(int band, CounterId taken, string marker) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        health, ComparisonOperator.LessOrEqual,
                        Const<DamageReceivedTriggeredEffectContext>(band)),
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(Mistress, taken),
                        ComparisonOperator.Equal, Const<DamageReceivedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        Mistress, taken, Const<DamageReceivedTriggeredEffectContext>(1), relative: false),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        Mistress, new StatusDefinitionId(marker),
                        Const<DamageReceivedTriggeredEffectContext>(1), sourceSelector: Mistress),
                ]));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                Announce(StorehouseAt, StorehouseTaken, StorehouseAnnouncedId),
                Announce(AllThingsAt, AllThingsTaken, AllThingsAnnouncedId),
            ]));
    }

    // ── her own hand ──────────────────────────────────────────────────────────────────────────────────────

    // A CLAIM, and it is TWO PASSES rather than a search: the first reads the hand and writes down the best
    // score it saw, the second enters the first card that matches it. That is the shape both claim moves
    // share — Finest reads a card's cost, Favored reads the use count she has been keeping in its margin —
    // and it is exact for any hand, where a ladder of "is there a 3, is there a 2" would only be exact for
    // the costs somebody thought of.
    private static IEffectNode<EnemyActionContext> Claim(
        Func<ICardInstanceExpression<EnemyActionContext>, ICombatExpression<EnemyActionContext, int>> score)
    {
        var card = new IteratedCardExpression<EnemyActionContext>();
        var best = new CombatantCounterExpression<EnemyActionContext>(Mistress, ClaimBest);

        // Free to be claimed: not already Eanna's, and not one of her own sheets.
        var claimable = new AndExpression<EnemyActionContext>(
            new NotExpression<EnemyActionContext>(
                new CardInstanceHasMarkExpression<EnemyActionContext>(card, ClaimMark)),
            new NotExpression<EnemyActionContext>(
                new CardInstanceHasTagExpression<EnemyActionContext>(card, new TagId(EannaSheetTag))));

        // ACROSS THE WHOLE DECK, and it has to be: the player's hand is discarded when their turn ends, so a
        // claim read off the hand would be a claim on an empty table every single time. A claim is on a CARD,
        // and the card is wherever the fight has left it.
        IEffectNode<EnemyActionContext> EveryZone(IEffectNode<EnemyActionContext> body) =>
            new SequenceEffectNode<EnemyActionContext>(
            [
                .. DeckZones.Select(zone => (IEffectNode<EnemyActionContext>)
                    new ForEachCardInZoneNode<EnemyActionContext>(
                        Applicant, zone, body, maxIterations: WholeDeck)),
            ]);

        var read = EveryZone(
            new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    claimable,
                    new ComparisonExpression<EnemyActionContext>(score(card), ComparisonOperator.Greater, best)),
                new SetCombatantCounterNode<EnemyActionContext>(
                    Mistress, ClaimBest, score(card), relative: false)));

        var write = EveryZone(
            new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    new AndExpression<EnemyActionContext>(
                        claimable,
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(Mistress, ClaimPlaced),
                            ComparisonOperator.Equal, Const<EnemyActionContext>(0))),
                    new ComparisonExpression<EnemyActionContext>(
                        score(card), ComparisonOperator.Equal, best)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new MarkCardInstanceNode<EnemyActionContext>(
                        Applicant, card, ClaimMark, sourceSelector: Mistress),
                    // Claimed the moment she takes it, not at the next turn's start: the discount is what
                    // makes a claim a temptation rather than a punishment, and it is offered at once.
                    new SetCardInstanceMarkCounterNode<EnemyActionContext>(
                        Applicant, card, StandardCombatIds.CardCostDeltaCounter,
                        Const<EnemyActionContext>(-1), relative: false),
                    new SetCombatantCounterNode<EnemyActionContext>(
                        Mistress, ClaimPlaced, Const<EnemyActionContext>(1), relative: false),
                ])));

        return new ConditionalEffectNode<EnemyActionContext>(
            UnderTheCap(),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new SetCombatantCounterNode<EnemyActionContext>(
                    Mistress, ClaimBest, Const<EnemyActionContext>(0), relative: false),
                new SetCombatantCounterNode<EnemyActionContext>(
                    Mistress, ClaimPlaced, Const<EnemyActionContext>(0), relative: false),
                read,
                write,
                Recount<EnemyActionContext>(),
            ]));
    }

    // Two seals to begin with (§7.8), four once the storehouse is open. A cap and not a rule about which
    // cards: the claims themselves are the interesting part, and a fight where every card is hers before the
    // third phase has nothing left to escalate into.
    private static ICombatExpression<EnemyActionContext, bool> UnderTheCap()
    {
        var count = ClaimedCount<EnemyActionContext>();
        return new OrExpression<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(Has<EnemyActionContext>(Mistress, StorehouseId)),
                new ComparisonExpression<EnemyActionContext>(
                    count, ComparisonOperator.Less, Const<EnemyActionContext>(ClaimCap))),
            new AndExpression<EnemyActionContext>(
                Has<EnemyActionContext>(Mistress, StorehouseId),
                new ComparisonExpression<EnemyActionContext>(
                    count, ComparisonOperator.Less, Const<EnemyActionContext>(StorehouseClaimCap))));
    }

    // "The first card played next turn becomes Temple Property." The one claim the PLAYER aims (§7.2), and
    // the reason it is worth aiming: the discount and the Splendor are real, and so is the Due.
    private static EffectProgram<EnemyActionContext> ClaimTheFirstGift() =>
        new(Seq(
            Hit(12),
            new ConditionalEffectNode<EnemyActionContext>(
                UnderTheCap(),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(FirstGiftId), Const<EnemyActionContext>(1),
                    sourceSelector: Mistress))));

    // "Demand the Arrears." What was refused, asked for again, with interest.
    private static EffectProgram<EnemyActionContext> DemandTheArrears() =>
        new(new DealDamageNode<EnemyActionContext>(
            Applicant,
            new AddExpression<EnemyActionContext>(
                Const<EnemyActionContext>(10),
                new MultiplyExpression<EnemyActionContext>(
                    Const<EnemyActionContext>(2),
                    Stacks<EnemyActionContext>(Applicant, ArrearsId)))));

    // "Call the Procession." The ledger is read aloud, the temple receives what it was given, and everything
    // it was not becomes Arrears. Then the count opens again.
    private static EffectProgram<EnemyActionContext> CallTheProcession() =>
        new(Seq(
            Hit(16),
            Collect<EnemyActionContext>(),
            // One MORE than the rounds it counts, because the ledger is re-entered inside the very window
            // its own countdown is read down at the end of: the player must open their turn looking at three.
            new ApplyStatusNode<EnemyActionContext>(
                Mistress, new StatusDefinitionId(ProcessionId), Const<EnemyActionContext>(ProcessionEvery + 1),
                sourceSelector: Mistress),
            new RemoveStatusNode<EnemyActionContext>(
                Mistress, new StatusDefinitionId(ProcessionCalledId))));

    // "You mistake the vessel for the wealth. I do not claim the cup. I claim what overflows it."
    private static EffectProgram<EnemyActionContext> OpenTheStorehouse() =>
        new(Seq(
            Hit(26),
            new ApplyStatusNode<EnemyActionContext>(
                Mistress, new StatusDefinitionId(StorehouseId), Const<EnemyActionContext>(1),
                sourceSelector: Mistress),
            new RemoveStatusNode<EnemyActionContext>(
                Mistress, new StatusDefinitionId(StorehouseAnnouncedId))));

    // "You still divide the world. Yours. Mine. A provincial distinction."
    private static EffectProgram<EnemyActionContext> AllThingsEnterEanna() =>
        new(Seq(
            Hit(24),
            ClaimEverything<EnemyActionContext>(),
            Recount<EnemyActionContext>(),
            // One more than the turns it lasts, because the count is read down at the end of the very window
            // it was written in: the player must open their next turn looking at three.
            new ApplyStatusNode<EnemyActionContext>(
                Mistress, new StatusDefinitionId(AllThingsId), Const<EnemyActionContext>(AllThingsTurns + 1),
                sourceSelector: Mistress),
            new RemoveStatusNode<EnemyActionContext>(
                Mistress, new StatusDefinitionId(AllThingsAnnouncedId))));

    // ── the two ways of paying ────────────────────────────────────────────────────────────────────────────

    // ENERGY OFFERING (§7.4). One Energy, one Due, and it re-offers itself while anything is still owed — so
    // the cap the master asks for is the pool itself: what you pay with is the attack you did not make.
    private static CardData OfferTheSurplus() => new()
    {
        Id = OfferSurplusCardId,
        NameKey = "Offer the Surplus",
        DescriptionKey = "Spend 1 Energy to settle 1 Temple Due.",
        Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
        Tags = [new TagId(EannaSheetTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    Stacks<CardPlayContext>(Applicant, TempleDueId),
                    ComparisonOperator.GreaterOrEqual, Const<CardPlayContext>(1)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new ModifyStatusStacksNode<CardPlayContext>(
                        Applicant, new StatusDefinitionId(TempleDueId), Const<CardPlayContext>(-1)),
                    new ConditionalEffectNode<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            Stacks<CardPlayContext>(Applicant, TempleDueId),
                            ComparisonOperator.GreaterOrEqual, Const<CardPlayContext>(1)),
                        new CreateCardInstanceNode<CardPlayContext>(
                            Applicant, new CardDefinitionId(OfferSurplusCardId), CardZone.Hand,
                            Const<CardPlayContext>(1))),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // DEDICATE A WORK (§7.4). A card out of the hand and into Eanna for the rest of the fight — the Banished
    // pile, which is the one place nothing fishes cards back out of, and which the run's own deck never sees:
    // what is dedicated is returned the moment she is dead, exactly as the master asks.
    //
    // The player is asked ONCE and the answer is written on the card as a mark; every step after reads the
    // mark, because a chooser expression consulted twice asks the player twice.
    private static CardData DedicateAWork() => new()
    {
        Id = DedicateWorkCardId,
        NameKey = "Dedicate a Work",
        DescriptionKey =
            $"Give Eanna a card from your hand for the rest of this fight. A card she has claimed settles "
            + $"{DedicatedClaimedWorth} Temple Due, an ordinary one {DedicatedWorkWorth}, and rubbish nothing "
            + "— Eanna wants value. Dedicated cards come back when she is dead.",
        Costs = [],
        Tags = [new TagId(EannaSheetTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
        Program = new EffectProgram<CardPlayContext>(Dedicate()),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    private static IEffectNode<CardPlayContext> Dedicate()
    {
        var card = new IteratedCardExpression<CardPlayContext>();

        IEffectNode<CardPlayContext> Settle(int amount) =>
            new ModifyStatusStacksNode<CardPlayContext>(
                Applicant, new StatusDefinitionId(TempleDueId), Const<CardPlayContext>(-amount));

        return new CausalSequenceEffectNode<CardPlayContext>(
        [
            new MarkCardInstanceNode<CardPlayContext>(
                Applicant,
                // Not her own sheets: this card is still in hand while its own program runs, and offering the
                // player their dedication sheet as the thing to dedicate is a trap rather than a decision.
                new ChosenCardInZoneExpression<CardPlayContext>(
                    CardZone.Hand, "dedicate a work to Eanna", excludeTag: new TagId(EannaSheetTag)),
                DedicateMark),
            new ForEachCardInZoneNode<CardPlayContext>(
                Applicant, CardZone.Hand,
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new MarkCardInstanceNode<CardPlayContext>(Applicant, card, DedicateMark, remove: true),
                    new ConditionalEffectNode<CardPlayContext>(
                        new NotExpression<CardPlayContext>(
                            new CardInstanceHasTagExpression<CardPlayContext>(
                                card, new TagId(Cards.CardAuthoring.JunkTag))),
                        new ConditionalEffectNode<CardPlayContext>(
                            new CardInstanceHasMarkExpression<CardPlayContext>(card, ClaimMark),
                            Settle(DedicatedClaimedWorth),
                            Settle(DedicatedWorkWorth))),
                    new MoveCardToZoneNode<CardPlayContext>(Applicant, card, CardZone.BanishedPile),
                ]),
                maxIterations: WholeDeck, markFilter: DedicateMark),
            Recount<CardPlayContext>(),
            // The sheet stands while anything is still owed. A Procession the player may answer with exactly
            // one card is not a decision about how much to give — it is a decision about which card, once.
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    Stacks<CardPlayContext>(Applicant, TempleDueId),
                    ComparisonOperator.GreaterOrEqual, Const<CardPlayContext>(1)),
                new CreateCardInstanceNode<CardPlayContext>(
                    Applicant, new CardDefinitionId(DedicateWorkCardId), CardZone.Hand,
                    Const<CardPlayContext>(1))),
        ]);
    }
}
