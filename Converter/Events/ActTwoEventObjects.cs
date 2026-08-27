using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// The shared Act-II event objects (BnB_Final_Events_Master_PostAudit.md, "ACT II"). Fifteen events are written
// out of this vocabulary, so it is built once, here — the archives' counterpart to ActOneEventObjects.
//
// The city's events wrote MARKINGS that said where a card starts. The archives write two different things:
//
//   · what an event does to a card for ONE fight, which is the archive's own vocabulary — a card can begin a
//     fight Misfiled or Redacted (ActTwo owns both marks and the rules that read them), or be held out of the
//     fight entirely and handed back part-way through;
//   · an INSCRIPTION, which is permanent. An inscription is a run card tag like Act I's Certified Original,
//     and the rule that reads it has to be in every later fight rather than the next one.
//
// Everything here is the HERO's rule. ActTwo's statuses are the archive's — they run on enemies and speak
// about the player; these run on the player and speak about their own cards.
public static class ActTwoEventObjects
{
    // ★ Static initializers run in DECLARATION order, so the ids and latches every rule below names are
    // declared first. A rule that reached backwards for one would find null.

    // Every mark an Act-II citer files. A Reference belongs to whoever filed it, so "a Reference" is these.
    private static readonly string[] References =
    [
        ActTwo.CertificateReferenceMark, ActTwo.EntryReferenceMark, ActTwo.AlphabetReferenceMark,
        ActTwo.CitationReferenceMark, ActTwo.ChainReferenceMark,
    ];

    private static readonly CounterId IlluminatedInitialSpent = new("illuminated_initial_spent");
    private static readonly CounterId ConcordantPairSpent = new("concordant_pair_spent");
    private static readonly CounterId TrueNameSpent = new("true_name_spent");
    private static readonly CounterId LateBoundSpent = new("late_bound_spent");
    private static readonly CounterId LeafSpent = new("redacted_leaf_spent");
    private static readonly CounterId RegisterReleased = new("act_two_register_released");
    private static readonly CounterId ReadersPassSpent = new("act_two_readers_pass_spent");
    private static readonly CounterId UnfinishedLifeSpent = new("act_two_unfinished_life_spent");

    // Two counters the RUN reads off the finished fight rather than the fight itself: whether the amendment was
    // played while it was still Redacted, and whether the Vow of Silent Scholarship held.
    //
    // ★ Both are written down as a ONE at the opening bell and only ever fall, because a fight that never had
    // the rule reads them as zero — which is exactly what a promise waiting on the WRONG fight has to see.
    public static readonly CounterId AmendmentPlayed = new("act_two_amendment_played");
    public static readonly CounterId VowHeld = new("act_two_vow_held");

    // ── Temporary cards ───────────────────────────────────────────────────────────────────────────────────

    // ADAPTATION: a Reference belongs to the enemy that filed it, so there is no one "Referenced" mark — each
    // citer of Act II marks with its own (ActTwo). The Citation therefore clears every kind, from the first
    // card in hand carrying that kind. A hand holding two differently-cited cards has both cleared, which is
    // marginally more generous than the design's "one card"; a hand normally holds at most one citation.
    public static readonly BnbCard UnfinishedCitation = new(
        "unfinished_citation", "Unfinished Citation", JunkTag, 1,
        "Retain. Exhaust. Clear a Reference from a card in your hand. If it is still in your hand at the end "
        + "of your turn, file 1 Paperwork.",
        Seq(),
        Rarity: "junk", Tags: [ExhaustTag, TemporaryTag, "unfinished_citation"], RetainInHand: true);

    // ADAPTATION: "the next playable card becomes Redacted immediately before resolution" needs a hook between
    // choosing a card and resolving it, which does not exist. The Leaf redacts as it is READ instead: while it
    // sits in your hand, the start of your turn redacts one card there and the Leaf is spent.
    public static readonly BnbCard RedactedLeaf = new(
        "redacted_leaf", "Redacted Leaf", JunkTag, 0,
        "Unplayable. Retain. At the start of your turn one card in your hand is Redacted, and the Leaf is "
        + "spent.",
        Seq(),
        Rarity: "junk", Tags: [UnplayableTag, ExhaustTag, TemporaryTag, "redacted_leaf"], RetainInHand: true);

    public static readonly BnbCard BorrowersClaim = new(
        "borrowers_claim", "Borrower's Claim", JunkTag, 0,
        "Retain. Exhaust. Put another card from your hand on the bottom of your draw pile, then draw 1. If it "
        + "is still in your hand at the end of your turn, file 1 Paperwork.",
        Seq(
            new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to file away"),
                ToZone: CardZone.DrawPile, Placement: ZonePlacement.Bottom),
            Draw(1)),
        Rarity: "junk", Tags: [ExhaustTag, TemporaryTag, "borrowers_claim"], RetainInHand: true);

    public static IReadOnlyList<BnbCard> Cards() => [UnfinishedCitation, RedactedLeaf, BorrowersClaim];

    // Two of them bite when a turn ends with them still in hand; the Leaf acts while it is held instead, and
    // its rule is the one below.
    public static IReadOnlyList<CardData> Compile() =>
    [
        .. Cards().Select(card => card.Compile()).Select(data => data.Id switch
        {
            // The Citation's own program is written in raw nodes: it has to find a card by the MARK on that
            // copy, and a mark filter is not something the curated card model can say.
            "unfinished_citation" => data with
            {
                Program = new EffectProgram<CardPlayContext>(
                    new CausalSequenceEffectNode<CardPlayContext>([.. References.Select(ClearReference)])),
                LifecyclePrograms = EndOfTurn(Keywords.Paperwork),
            },
            "borrowers_claim" => data with { LifecyclePrograms = EndOfTurn(Keywords.Paperwork) },
            _ => data,
        }),
    ];

    // ── What one fight has to honour ──────────────────────────────────────────────────────────────────────
    //
    // Misfiled and Redacted are the ARCHIVE's marks, and an event that writes one is writing the same thing an
    // enemy would. Carried across as a run card tag, they arrive as per-instance marks — but a mark is only
    // half of Redacted (the halving itself is two reserved counters beside it), so the rule below finishes
    // what the tag started. Borrower's Keeping and the Reservation are this act's own: a card lent away and
    // handed back part-way through the fight.
    public const string BorrowersKeeping = "borrowers_keeping"; // away; back in hand at round 2
    public const string Reservation = "reservation";            // away; back in hand at round 3

    // What is true for exactly one fight, and is therefore cleared once that fight is over.
    public static IReadOnlyList<string> SpentAfterOneFight() =>
        [ActTwo.MisfiledMark, ActTwo.RedactedMark, BorrowersKeeping, Reservation];

    // Everything the archive can write on one of your cards. A rule that undoes the archive's work — True
    // Name, the Blank Cameo — has to name them all, so they are named once, here.
    public static IReadOnlyList<string> ArchiveMarks() =>
        [ActTwo.MisfiledMark, ActTwo.RedactedMark, ActTwo.MisfiledSidewaysMark, .. References];

    public const string ArchiveMarkings = "act_two_markings";

    public static readonly StatusData ArchiveMarkingsRule = Rule(
        ArchiveMarkings, "As the Archive Left It",
        "What the archives did to your cards between fights is honoured here.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // Round 1: a redaction written between fights is finished (the mark rode across; the
                    // halving beside it did not), and what was lent out leaves the table.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(1),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Halve(CardZone.Hand), Halve(CardZone.DrawPile),
                            Lend(BorrowersKeeping), Lend(Reservation),
                        ])),
                    // The borrowed volume comes back at round 2, the reserved one at round 3 — each Retaining
                    // and free for that turn, which is what the loan was worth.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(2), HandBack(BorrowersKeeping)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(3), HandBack(Reservation)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The five permanent inscriptions ───────────────────────────────────────────────────────────────────
    //
    // An inscription is written on ONE card and never comes off. Like Act I's Certified Original it rides as a
    // run card tag and its rule is installed in every later fight (ActTwoEventPrograms), not just the next one.

    public const string AuthorizedRevision = "authorized_revision";
    public const string IlluminatedInitial = "illuminated_initial";
    public const string ConcordantPair = "concordant_pair";
    public const string TrueName = "true_name";
    public const string LateBound = "late_bound";

    public static IReadOnlyList<string> Inscriptions() =>
        [AuthorizedRevision, IlluminatedInitial, ConcordantPair, TrueName, LateBound];

    // "First play each combat costs +1; if payable, positive numerical effects +50%; then spent for combat."
    // Both halves are prices written on the COPY, and the play that pays them consumes them — which is exactly
    // "then spent", with no latch needed.
    public static readonly StatusData AuthorizedRevisionRule = Inscription(
        AuthorizedRevision, "Authorized Revision",
        "The revision is authorized, at a price: the first time you play it this fight it costs 1 more and "
        + "does half again as much.",
        Both(zone => new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Price(zone, AuthorizedRevision, 1),
            Scale(zone, AuthorizedRevision, 3, 2),
        ])));

    // "First play each combat draws 1 and gains 3 Block."
    public static readonly StatusData IlluminatedInitialRule = Rule(
        IlluminatedInitial, "Illuminated Initial",
        "The first time you play it each fight, the initial is worth a card and 3 Block.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                OncePerFight(IlluminatedInitialSpent, PlayedCarries(IlluminatedInitial),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                        new GainBlockNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "First partner played each combat moves the other from Draw/Discard to top of Draw; if already in hand
    // gain 3 Block."
    //
    // ADAPTATION: only the DRAW pile is fetched from. A played card has already reached the discard pile by the
    // time the play is announced, so a rule that reached in there would fetch the partner that was just played
    // back out of it — the pair would pull on itself.
    public static readonly StatusData ConcordantPairRule = Rule(
        ConcordantPair, "Concordant Pair",
        "Two arguments filed together. Play one and the other is fetched to the top of your draw pile, or is "
        + "worth 3 Block if you already hold it.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                OncePerFight(ConcordantPairSpent, PlayedCarries(ConcordantPair),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.DrawPile,
                            new MoveCardToZoneNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                                CardZone.DrawPile, placement: ZonePlacement.Top),
                            markFilter: new TagId(ConcordantPair), takeFirst: 1),
                        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new GainBlockNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                            markFilter: new TagId(ConcordantPair), takeFirst: 1),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "The first enemy Misfiled/Referenced/Redacted marker aimed at this card each combat is prevented."
    //
    // ADAPTATION: nothing hears a mark being PUT on a card, so the name is not a shield but a correction — the
    // start of the round after one lands, it is struck off, once per fight. The same beat-late shape the
    // Exception Imp's Loophole already uses in Act I.
    public static readonly StatusData TrueNameRule = Rule(
        TrueName, "True Name",
        "A card that knows its own name is written back the way it was: once each fight, the first mark the "
        + "archive puts on it is struck off again.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [Correct(CardZone.Hand), Correct(CardZone.DrawPile)])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "First time each combat the card ends turn unplayed in hand: Retain. Next turn cost −1 and positive
    // numerical effects +25%."
    //
    // ADAPTATION: a turn-end program cannot see the hand — the discard runs first — so the waiting is not
    // watched, it is granted. A late-bound card always Retains, and the first turn after the first on which it
    // is still being held, it is cheaper and does more.
    public static readonly StatusData LateBoundRule = Rule(
        LateBound, "Late-Bound",
        "It is never discarded, and the second turn you are still holding it, it costs 1 less and does a "
        + "quarter more.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(1),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [Hold(CardZone.Hand), Hold(CardZone.DrawPile)])),
                    // …from the SECOND turn on: round 1 is the turn you were given it, not a turn you spent
                    // holding it.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                            Unspent<CardsDrawnTriggeredEffectContext>(LateBoundSpent)),
                        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [
                                Discount<CardsDrawnTriggeredEffectContext>(1),
                                ScaleIterated<CardsDrawnTriggeredEffectContext>(5, 4),
                                Spend<CardsDrawnTriggeredEffectContext>(LateBoundSpent),
                            ]),
                            markFilter: new TagId(LateBound), takeFirst: 1)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Leaf's own rule: while it is held, the start of the turn redacts one other card and spends it.
    public static readonly StatusData RedactedLeafRule = Rule(
        "redacted_leaf_rule", "Redacted Leaf",
        "A loose leaf of black ink. What it touches in your hand comes out half-erased.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand, new TagId("redacted_leaf")),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        // Exactly ONE card, and not the Leaf: a zone filter names one tag at a time, so the
                        // three player types are tried in turn and a latch stops after the first that lands.
                        new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, LeafSpent,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                        RedactFirst(DeedTag), RedactFirst(WorkingTag), RedactFirst(RiteTag),
                        // …and the Leaf is gone, played or not.
                        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                CardZone.ExhaustPile),
                            tagFilter: new TagId("redacted_leaf")),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── The rules one or two doors need ───────────────────────────────────────────────────────────────────
    //
    // Everything above is shared by the whole act. These are the fights the individual events buy: a reading
    // room that reads over your shoulder, a name entered in the wrong register, a life that will not finish, a
    // pass that undoes the archive's first correction, an amendment worth watching, a vow, and an hour that was
    // borrowed from somewhere.

    public const string FourthCard = "act_two_fourth_card";
    public const string RegisteredElsewhere = "act_two_registered_elsewhere";
    public const string RegisteredMark = "registered_elsewhere";
    public const string UnfinishedLife = "act_two_unfinished_life";
    public const string UnfinishedReturn = "act_two_unfinished_return";
    public const string ReadersPass = "act_two_readers_pass";
    public const string AmendmentWatch = "act_two_amendment_watch";
    public const string WhisperedAmendment = "whispered_amendment";
    public const string Vow = "act_two_vow";
    public const string LostHour = "act_two_lost_hour";
    public const string ShortestPath = "act_two_shortest_path";

    // "The first fourth card each turn Redacts one random remaining hand card." The engine already tallies how
    // many cards a combatant has played this turn, and it tallies BEFORE the play's triggers run — so the play
    // that sees exactly 4 is the fourth one, and no card after it sees 4 again.
    public static readonly StatusData FourthCardRule = Rule(
        FourthCard, "Read Under Supervision",
        "Every fourth card you play in a turn is read over your shoulder, and one other card in your hand "
        + "comes back half-erased.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(4)),
                    ActTwo.Redact<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new RandomCardInZoneExpression<CardPlayedTriggeredEffectContext>(CardZone.Hand)))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "One random opening-hand card cannot be played until another card is played; if left unplayed it becomes
    // Misfiled."
    //
    // ADAPTATION: a copy cannot be made unplayable — there is no per-instance Unplayable, only the printed kind
    // — so the register writes a PRICE on it instead: 9 more Energy, which is nothing anyone can pay. The first
    // card played strikes the price off, which is the same permission read as a cost. And "left unplayed" is
    // read one beat later, at the next turn's draw: a card still carrying the register's mark then is misfiled.
    public static readonly StatusData RegisteredElsewhereRule = Rule(
        RegisteredElsewhere, "Entered Under Another Name",
        "One card in your opening hand is registered to somebody else. It cannot be filed until you have filed "
        + "something else, and if you never do, it is misfiled.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(1),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new RandomCardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand),
                                new TagId(RegisteredMark)),
                            Price(CardZone.Hand, RegisteredMark, 9),
                        ])),
                    // A later turn, with the register never released: the card goes where a card nobody could
                    // file goes.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                            Unspent<CardsDrawnTriggeredEffectContext>(RegisterReleased)),
                        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [
                                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source,
                                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                    new TagId(ActTwo.MisfiledMark)),
                                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source,
                                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                    CardZone.DiscardPile),
                                Spend<CardsDrawnTriggeredEffectContext>(RegisterReleased),
                            ]),
                            markFilter: new TagId(RegisteredMark))),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
            // Filing anything at all releases the register. The registered card cannot be that card — nobody can
            // pay nine — so this is exactly "until another card is played".
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    Unspent<CardPlayedTriggeredEffectContext>(RegisterReleased),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                            [
                                Discount<CardPlayedTriggeredEffectContext>(9),
                                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source,
                                    new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                                    new TagId(RegisteredMark), remove: true),
                            ]),
                            markFilter: new TagId(RegisteredMark)),
                        Spend<CardPlayedTriggeredEffectContext>(RegisterReleased),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "The primary enemy's first lethal event instead returns it once at 30% Max HP."
    //
    // ADAPTATION, in two halves. The engine's death prevention is authored data, but the health it survives at
    // is a CONSTANT — an act's enemies are drawn from a pool and do not share a maximum — so the prevention
    // catches the body at 1 HP and a second rule immediately writes it back up to 30% of its OWN maximum. And
    // "the primary enemy" is the largest body on the table: nothing else in a generated fight says which one
    // the encounter thinks of as primary.
    public static readonly StatusData UnfinishedLifeRule = new()
    {
        Id = UnfinishedLife,
        NameKey = "An Unfinished Life",
        DescriptionKey = "This one's file is still open. The first death it is dealt does not take.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        DeathPrevention = new StatusDeathPreventionData(1, []),
    };

    public static readonly StatusData UnfinishedReturnRule = Rule(
        UnfinishedReturn, "Borrowed Back",
        "What the window lends, it lends whole: a life caught at its end comes back at a third of itself.",
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                        Unspent<DamageReceivedTriggeredEffectContext>(UnfinishedLifeSpent)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new HealNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new MaxExpression<DamageReceivedTriggeredEffectContext>(
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0),
                                new SubtractExpression<DamageReceivedTriggeredEffectContext>(
                                    new DivideExpression<DamageReceivedTriggeredEffectContext>(
                                        new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
                                            new CombatantMaxHealthExpression<DamageReceivedTriggeredEffectContext>(
                                                CombatantTargetSelectors.Source),
                                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(30)),
                                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(100)),
                                    new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(
                                        CombatantTargetSelectors.Source)))),
                        Spend<DamageReceivedTriggeredEffectContext>(UnfinishedLifeSpent),
                    ]))),
                nameof(TriggerEvent.DamageTaken)),
        ]);

    // "Prevent the first enemy attempt to apply Misfiled/Referenced/Redacted to any player card."
    //
    // ADAPTATION: nothing hears a mark being put on a card, so the pass is a correction rather than a shield —
    // exactly the True Name's shape, but over ANY card rather than the one that knows its own name.
    public static readonly StatusData ReadersPassRule = Rule(
        ReadersPass, "Temporary Reader's Pass",
        "You are a reader here, for now: the first thing the archive writes on one of your cards is struck off "
        + "again at the start of your next turn.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [CorrectAny(CardZone.Hand), CorrectAny(CardZone.DrawPile)])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "If successfully played while still Redacted, permanently upgrade it after victory." The fight cannot
    // upgrade a persistent card — that is the run's — so it writes down that it happened and the run reads the
    // counter off the finished fight (Act I's Receipt of Prior Effort is the same trick).
    public static readonly StatusData AmendmentWatchRule = Rule(
        AmendmentWatch, "The Whispered Amendment",
        "File the amendment while it is still half-erased and the correction is made permanent afterwards.",
        [
            OpensAt(AmendmentPlayed, 0),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        PlayedCarries(WhisperedAmendment), PlayedCarries(ActTwo.RedactedMark)),
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, AmendmentPlayed,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "Never play more than 3 non-Junk cards in any player turn." Breaking it costs nothing but the bead, so
    // the rule does not stop anything — it only writes down that the vow was broken, and the run reads that.
    public static readonly StatusData VowRule = Rule(
        Vow, "The Vow of Silent Scholarship",
        "Three filings a turn, no more. Nothing stops a fourth; the table simply notices.",
        [
            OpensAt(VowHeld, 1),
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(), ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, VowHeld,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0), relative: false))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "Round 1 +1 Energy; Round 2 +1 Energy; Round 3 −2 available Energy, minimum 0; then ends." The gained
    // Energy is HELD — a full pool swallows a gain whole (Converter/HeldEnergy.cs).
    public static readonly StatusData LostHourRule = Rule(
        LostHour, "The Lost Hour",
        "An hour that belonged to somebody else: two rounds of it are yours, and the third is when it is "
        + "noticed missing.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(1),
                        HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(2),
                        HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(3),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new LoseResourceNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                            new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(LostHour)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // A line the fight wears because the RUN is going to act on it (ActTwoEventPrograms.ShortestPath).
    public static readonly StatusData ShortestPathNote = Note(
        ShortestPath, "The Shortest Path",
        "The Librarian sent you the short way: your opposition arrived diminished, this fight pays no Gold, "
        + "and there is an extra procedure waiting at the end of it.");

    public static IReadOnlyList<StatusData> Statuses() =>
    [
        ArchiveMarkingsRule, AuthorizedRevisionRule, IlluminatedInitialRule, ConcordantPairRule,
        TrueNameRule, LateBoundRule, RedactedLeafRule,
        FourthCardRule, RegisteredElsewhereRule, UnfinishedLifeRule, UnfinishedReturnRule, ReadersPassRule,
        AmendmentWatchRule, VowRule, LostHourRule, ShortestPathNote,
    ];

    // ── shorthands ────────────────────────────────────────────────────────────────────────────────────────


    // One redaction, on the first card in hand of the given type, and only while nothing has been redacted yet.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> RedactFirst(string type) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Unspent<CardsDrawnTriggeredEffectContext>(LeafSpent),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, CardZone.Hand,
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    ActTwo.Redact<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new IteratedCardExpression<CardsDrawnTriggeredEffectContext>()),
                    Spend<CardsDrawnTriggeredEffectContext>(LeafSpent),
                ]),
                tagFilter: new TagId(type), takeFirst: 1));

    private static IEffectNode<CardPlayContext> ClearReference(string mark) =>
        new ForEachCardInZoneNode<CardPlayContext>(
            CombatantTargetSelectors.Source, CardZone.Hand,
            new MarkCardInstanceNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new IteratedCardExpression<CardPlayContext>(),
                new TagId(mark), remove: true),
            markFilter: new TagId(mark), takeFirst: 1);

    private static IReadOnlyDictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>> EndOfTurn(
        string status) =>
        new Dictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>>
        {
            [CardLifecycleTrigger.TurnEndInHand] = new(new ApplyStatusNode<CardLifecycleContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(status),
                new ConstantExpression<CardLifecycleContext>(1))),
        };

    private static ICombatExpression<TContext, bool> OnRound<TContext>(int round) where TContext : class =>
        new ComparisonExpression<TContext>(
            new RoundNumberExpression<TContext>(), ComparisonOperator.Equal,
            new ConstantExpression<TContext>(round));

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, latch, new ConstantExpression<TContext>(1), relative: false);

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

    // A price written on the marked COPY, consumed by the play that pays it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Price(CardZone zone, string mark, int amount) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(amount), relative: true),
            markFilter: new TagId(mark));

    // What the marked copy's next play is worth, as the engine's own output scale.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Scale(
        CardZone zone, string mark, int numerator, int denominator) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            ScaleIterated<CardsDrawnTriggeredEffectContext>(numerator, denominator),
            markFilter: new TagId(mark));

    private static IEffectNode<TContext> ScaleIterated<TContext>(int numerator, int denominator)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCardInstanceMarkCounterNode<TContext>(
                CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(),
                StandardCombatIds.CardOutputScaleNumeratorCounter,
                new ConstantExpression<TContext>(numerator), relative: false),
            new SetCardInstanceMarkCounterNode<TContext>(
                CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(),
                StandardCombatIds.CardOutputScaleDenominatorCounter,
                new ConstantExpression<TContext>(denominator), relative: false),
        ]);

    private static IEffectNode<TContext> Discount<TContext>(int amount) where TContext : class =>
        new SetCardInstanceMarkCounterNode<TContext>(
            CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(),
            StandardCombatIds.CardCostDeltaCounter,
            new ConstantExpression<TContext>(-amount), relative: true);

    // Finish a redaction the run only half-wrote: the mark rode across as a card tag, the halving beside it
    // did not.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Halve(CardZone zone) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            ScaleIterated<CardsDrawnTriggeredEffectContext>(1, 2),
            markFilter: new TagId(ActTwo.RedactedMark));

    // A lent card leaves the table entirely; the fight is played without it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Lend(string mark) =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Move(CardZone.DrawPile, CardZone.BanishedPile, mark),
            Move(CardZone.Hand, CardZone.BanishedPile, mark),
        ]);

    // …and comes back Retaining and free for the turn it comes back on.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> HandBack(string mark) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, CardZone.BanishedPile,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), CardZone.Hand),
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    StandardCombatIds.RetainedCardMark),
                Discount<CardsDrawnTriggeredEffectContext>(9),
            ]),
            markFilter: new TagId(mark));

    // A late-bound card is never put down: the retain is a mark on the copy, so it travels with it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Hold(CardZone zone) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                StandardCombatIds.RetainedCardMark),
            markFilter: new TagId(LateBound));

    // The true name, struck back onto a card the archive wrote over — once per fight, and only on the card
    // that was actually written on.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Correct(CardZone zone) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Unspent<CardsDrawnTriggeredEffectContext>(TrueNameSpent),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, zone,
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Overwritten(),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        .. ArchiveMarks().Select(Unmark),
                        // A redaction is a mark AND the halving beside it; striking off only the mark would
                        // leave the card quietly worth half.
                        ScaleIterated<CardsDrawnTriggeredEffectContext>(1, 1),
                        Spend<CardsDrawnTriggeredEffectContext>(TrueNameSpent),
                    ])),
                markFilter: new TagId(TrueName)));

    // A counter the RUN is going to read off the finished fight is written down at the opening bell, so its
    // value is the rule's and not an accident of whether anything happened. A fight the rule was never in
    // reads zero, which is how a promise can tell "the fight I was about" from any other fight.
    private static StatusTriggerData OpensAt(CounterId counter, int value) =>
        Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                OnRound<CardsDrawnTriggeredEffectContext>(1),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, counter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(value), relative: false))),
            nameof(TriggerEvent.CardsDrawn));

    // The Reader's Pass' correction: the first card the archive has written on, anywhere the player can reach.
    // The True Name's shape without its markFilter — a name corrects itself, a pass corrects whatever it finds.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> CorrectAny(CardZone zone) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Unspent<CardsDrawnTriggeredEffectContext>(ReadersPassSpent),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, zone,
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Overwritten(),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        .. ArchiveMarks().Select(Unmark),
                        ScaleIterated<CardsDrawnTriggeredEffectContext>(1, 1),
                        Spend<CardsDrawnTriggeredEffectContext>(ReadersPassSpent),
                    ]))));

    // How many of the player's OWN cards were filed this turn — Junk is not scholarship, and the Vow is only
    // about the three player types.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> NonJunkPlayedThisTurn()
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, int> total =
            new ConstantExpression<CardPlayedTriggeredEffectContext>(0);
        foreach (var tag in new[] { DeedTag, WorkingTag, RiteTag })
        {
            total = new AddExpression<CardPlayedTriggeredEffectContext>(total,
                new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new TagId(tag)));
        }
        return total;
    }

    // A status that carries no rule at all: a line the fight shows because the RUN is going to act on it.
    private static StatusData Note(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
    };

    private static ICombatExpression<CardsDrawnTriggeredEffectContext, bool> Overwritten()
    {
        // "never", widened one mark at a time — the seed of an OR chain.
        ICombatExpression<CardsDrawnTriggeredEffectContext, bool> any =
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), ComparisonOperator.Equal,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1));
        foreach (var mark in ArchiveMarks())
        {
            any = new OrExpression<CardsDrawnTriggeredEffectContext>(any,
                new CardInstanceHasMarkExpression<CardsDrawnTriggeredEffectContext>(
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), new TagId(mark)));
        }
        return any;
    }

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Unmark(string mark) =>
        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source,
            new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), new TagId(mark), remove: true);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Move(CardZone from, CardZone to, string mark) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, from,
            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), to),
            markFilter: new TagId(mark));

    // An inscription whose whole rule is "at the opening, price this copy" — written wherever the copy is
    // standing when the fight begins.
    private static StatusData Inscription(
        string id, string name, string description,
        Func<CardZone, IEffectNode<CardsDrawnTriggeredEffectContext>> perZone) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    OnRound<CardsDrawnTriggeredEffectContext>(1),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [perZone(CardZone.Hand), perZone(CardZone.DrawPile)]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static Func<CardZone, IEffectNode<CardsDrawnTriggeredEffectContext>> Both(
        Func<CardZone, IEffectNode<CardsDrawnTriggeredEffectContext>> body) => body;

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
