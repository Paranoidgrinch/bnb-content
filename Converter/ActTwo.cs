using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// Act II — The Endless Archives: the shared vocabulary, and the Stage-1 identities that introduce it.
//
// The archive's pressure is source-bound. Overdue is not one debt the player owes the room, it is a separate
// debt owed to each enemy that filed it, and each of them collects its own. That is why Overdue is applied one
// stack at a time as its OWN instance: two instances from the Brass Maw are the Maw's two, and the Ouroboros
// standing beside it cannot spend them.
//
// Each source's collection is its **Delinquency**: when it is owed 2, it takes them back, files a Paperwork,
// and does whatever that particular source does about being kept waiting.
public static class ActTwo
{
    // ── the vocabulary ────────────────────────────────────────────────────────────────────────────────────

    public const string OverdueId = "overdue";

    // One stack per filing, each its own instance carrying its own source. Merging would collapse the whole
    // point: the threshold is "2 from the same source", and merged stacks remember only the last source.
    public static StatusData Overdue() => new()
    {
        Id = OverdueId,
        NameKey = "Overdue",
        DescriptionKey = "A debt owed to whoever filed it. At 2 from one source, that source collects.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.CreateSeparateInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // A source's collection, checked at its OWN turn start — which is what makes "this source's" unambiguous:
    // the bearer is the acting source, so the stacks it spends are provably its own and never a neighbour's.
    // (The design collects the instant the second Overdue lands; here it collects on the next turn of the one
    // owed. See ADAPTATIONS.)
    public static StatusData Delinquency(
        string id, string name, string description,
        IEffectNode<TurnStartedTriggeredEffectContext>? lateConsequence = null)
    {
        var steps = new List<IEffectNode<TurnStartedTriggeredEffectContext>>
        {
            // Two instances of one stack each, so two picks — a single −2 would empty one filing and leave
            // the other standing.
            SpendOne(), SpendOne(),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Opponent, new StatusDefinitionId(Keywords.Paperwork),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
        };
        if (lateConsequence is not null)
            steps.Add(lateConsequence);

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksFromSourceExpression<TurnStartedTriggeredEffectContext>(
                        Opponent, new StatusDefinitionId(OverdueId), CombatantTargetSelectors.Source),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(steps)));

        return Rule(id, name, description,
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()))]);
    }

    private static IEffectNode<TurnStartedTriggeredEffectContext> SpendOne() =>
        new ModifySelectedStatusStacksNode<TurnStartedTriggeredEffectContext>(
            Opponent,
            new StatusSelectionSpec(StatusPolarityFilter.Debuff)
            {
                Definition = new StatusDefinitionId(OverdueId),
                FromActingSource = true,
            },
            new ConstantExpression<TurnStartedTriggeredEffectContext>(-1));

    // ── Misfiled ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // "A Misfiled card does not enter the hand on its next draw; it goes to discard, you draw a replacement,
    // and the mark clears." The engine hands the cards over BEFORE anything can object — CardsDrawn fires with
    // the hand already holding them — so the archive takes its card back a beat after it arrives. Invisible in
    // the numbers, visible in a combat log; the same beat-late shape Act I's Exception Imp uses.
    //
    // Which shelf a card was misfiled BY changes where it goes, so the destination is written into the mark
    // rather than looked up from the marker's source: a program cannot ask who put a mark there, and "the
    // Crabwise Shelf's misfilings go to the bottom of the draw pile" has to be answerable at the moment the
    // card is taken back. One rule owns that moment, exactly as one place owns the Paperwork tick.
    public const string MisfiledMark = "misfiled";
    public const string MisfiledSidewaysMark = "misfiled_sideways";
    public const string ArchiveRegulationsId = "archive_regulations";

    // The hero carries this in every fight where something can misfile. Idempotent by construction: two
    // misfiling enemies ask for the same marker and it merges, so the card is taken back once.
    public static StatusData ArchiveRegulations()
    {
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                TakeBack(MisfiledMark, CardZone.DiscardPile),
                TakeBack(MisfiledSidewaysMark, CardZone.DrawPile),
            ]));

        return Rule(ArchiveRegulationsId, "Archive Regulations",
            "A misfiled card is taken back as it reaches you, and something else is fetched in its place.",
            [new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()))]);
    }

    // Take back every card in hand carrying one kind of misfiling, and fetch a replacement for each. The mark
    // is cleared BEFORE the replacement is drawn, so a replacement that is itself misfiled waits for the next
    // draw rather than being swept up by the pass that fetched it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> TakeBack(string mark, CardZone destination) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, CardZone.Hand,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(mark), remove: true),
                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    destination),
                new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
            ]),
            markFilter: new TagId(mark));

    // ── Stage 2 — The Misfiled Stacks ─────────────────────────────────────────────────────────────────────

    public const string WrongEditionMark = "wrong_edition";
    public const string WrongEditionId = "wrong_edition_rule";

    // "Misfile 1 card": the mark goes on the draw pile, which is where a misfiling can still cost the player
    // something. The pile is already shuffled, so its top card IS the random one — the reading Act I's
    // Unclaimed Property Tag uses.
    public static IEffectNode<EnemyActionContext> MisfileOne(string mark) =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(), new TagId(mark)),
            takeFirst: 1);

    // "After normal draw, select one valid card in hand. If the player plays it this turn, it resolves normally
    // and THEN becomes Misfiled." The rule lives on the player because that is whose hand and whose play it is
    // about; it is only there when the Corridor is (see EncounterPassives.HeroOpeningStatuses).
    public static StatusData WrongEdition()
    {
        var mark = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, CardZone.Hand,
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(WrongEditionMark)),
                takeFirst: 1));

        // Played, so it resolved — and only now is it the wrong edition.
        var convert = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    new TagId(WrongEditionMark)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(WrongEditionMark), remove: true),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(MisfiledMark)),
                ])));

        return Rule(WrongEditionId, "Wrong Edition",
            "One card in your hand is from the wrong edition; playing it works, and then it is filed away.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    mark, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    convert, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    // ── Referenced ────────────────────────────────────────────────────────────────────────────────────────
    //
    // "A source-bound mark on a card. Play it and the reference is fulfilled; let it leave your hand unplayed
    // and the reference clears and costs you 1 Overdue from its source."
    //
    // The whole rule lives on the CITING ENEMY rather than on the player, for one reason: the Overdue an
    // unfulfilled reference costs has to come FROM that enemy, and a rule running on the player would file it
    // from the player. The enemy checking at its own turn start is also the moment that knows the answer — the
    // player's hand has just been put down, so anything still carrying the mark was not played.
    //
    // Each citer marks with its own tag, because a program cannot ask who put a mark on a card.
    public static StatusData Reference(string id, string name, string mark, string description,
        IEffectNode<CardsDrawnTriggeredEffectContext>? cite = null)
    {
        // Cite one card in the player's hand after the player's draw. `cite` overrides which one.
        var citing = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                cite ?? CiteFirst(mark)));

        // Played, so fulfilled — the mark simply goes.
        var fulfil = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    new TagId(mark)),
                Unmark<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), mark)));

        // My turn, and the player's hand is down: anything still marked was never played.
        var collect = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [Unfulfilled(mark, CardZone.DiscardPile), Unfulfilled(mark, CardZone.Hand)]));

        return Rule(id, name, description,
        [
            new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                citing, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere),
            new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                fulfil, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere),
            new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                collect, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
        ]);
    }

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteFirst(string mark) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, CardZone.Hand,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), new TagId(mark)),
            takeFirst: 1);

    private static IEffectNode<TurnStartedTriggeredEffectContext> Unfulfilled(string mark, CardZone zone) =>
        new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(
            Opponent, zone,
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Opponent, new IteratedCardExpression<TurnStartedTriggeredEffectContext>(),
                    new TagId(mark), remove: true),
                // Filed by ME — the bearer is the acting source, which is the whole reason this rule is here
                // and not on the player.
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Opponent, new StatusDefinitionId(OverdueId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            ]),
            markFilter: new TagId(mark));

    private static IEffectNode<TContext> Unmark<TContext>(ICardInstanceExpression<TContext> card, string mark)
        where TContext : class =>
        new MarkCardInstanceNode<TContext>(
            CombatantTargetSelectors.Source, card, new TagId(mark), remove: true);

    // "It was the player who drew / played", from inside a rule that watches the whole fight. Every fight
    // marks the hero as the applicant (Act I), which is the only structural handle on "the player".
    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    // ── Stage 1 — The Hall of Returns ─────────────────────────────────────────────────────────────────────

    public const string BrassMawDelinquencyId = "brass_maw_delinquency";
    public const string ReturnParcelId = "return_parcel";
    public const string OuroborosDelinquencyId = "ouroboros_delinquency";
    public const string OtherDelinquencyId = "other_delinquency";

    public static IReadOnlyList<StatusData> All() =>
    [
        Overdue(),
        ReturnParcel(),
        BrassMawDelinquency(),
        OuroborosDelinquency(),
        Delinquency(OtherDelinquencyId, "Improper Storage",
            "What is kept waiting is filed against you."),
        MiscellaneousClassification(),
        ArchiveRegulations(),
        WrongEdition(),
        SecondPersonReference(),
        LearnedLetter(),
        FangedAlphabetReference(),
        OrphanCitationReference(),
        ReservedSeat(),
        ShrinkingMargin(),
        LeaveOneWordUnspoken(),
        Voice(),
        BlankCertificateReference(),
    ];

    public const string CertificateReferenceMark = "referenced_certificate";
    public const string CertificateReferenceId = "blank_certificate_reference";

    // The Certificate cites you and then asks, when it dies, whether you answered.
    public static StatusData BlankCertificateReference() =>
        Reference(CertificateReferenceId, "Serve Certificate", CertificateReferenceMark,
            "A card is cited. Answer it, or owe the Certificate for it.");

    public const string MiscellaneousClassificationId = "miscellaneous_classification";

    // "The first non-Junk card type played each player turn becomes Recognized Category. Play a different type
    // that turn and the Object loses 5 Block (or takes 3 if it has none); play only the Recognized Category and
    // it gains 6."
    //
    // Read at the player's turn END, once, per type — which is the only moment that knows what the whole turn
    // turned out to be. "First non-Junk" is read as the literal first card, exactly as Act I reads it for the
    // Wrong-Window Scribe: the engine records ONE opening type per turn, and skipping Junk would need a second,
    // Junk-aware record. See ADAPTATIONS.
    private static StatusData MiscellaneousClassification()
    {
        IEffectNode<TurnEndedTriggeredEffectContext> Category(string recognized, string[] others)
        {
            ICombatExpression<TurnEndedTriggeredEffectContext, int> Played(string tag) =>
                new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                    Opponent, new TagId(tag));

            // "At least one different type" — the two categories that are not the recognized one.
            ICombatExpression<TurnEndedTriggeredEffectContext, int> strayed = Played(others[0]);
            for (var i = 1; i < others.Length; i++)
                strayed = new AddExpression<TurnEndedTriggeredEffectContext>(strayed, Played(others[i]));

            return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new FirstCardPlayedThisTurnHasTagExpression<TurnEndedTriggeredEffectContext>(
                        Opponent, new TagId(recognized)),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        strayed, ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                // Strayed: the classification is disturbed.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    // Losing Block is not gaining a negative amount of it — the pool is moved directly.
                    new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-5)),
                    new DealDamageNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(3),
                        ignoresBlock: true)),
                // Kept to the one category: properly stored.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new FirstCardPlayedThisTurnHasTagExpression<TurnEndedTriggeredEffectContext>(
                        Opponent, new TagId(recognized)),
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(6))));
        }

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                Category(CardAuthoring.DeedTag, [CardAuthoring.WorkingTag, CardAuthoring.RiteTag]),
                Category(CardAuthoring.WorkingTag, [CardAuthoring.DeedTag, CardAuthoring.RiteTag]),
                Category(CardAuthoring.RiteTag, [CardAuthoring.DeedTag, CardAuthoring.WorkingTag]),
            ]));

        return Rule(MiscellaneousClassificationId, "Miscellaneous Classification",
            "However you begin a turn is how the Object files you; stray from it and the filing suffers.",
            [new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()))]);
    }

    // "Whenever Brass Maw resolves its Delinquency: gain 1 Return Parcel, maximum 2." The parcels ride on the
    // Maw's next direct attack and are spent by it.
    private static StatusData BrassMawDelinquency() =>
        Delinquency(BrassMawDelinquencyId, "Return Intake",
            "What it is owed comes back as a parcel.",
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))));

    // "+5 damage per Return Parcel on the next direct attack; after that attack, Return Parcel → 0." The same
    // shape Act I's Contempt uses: the modifier is the parcel's presence, the trigger is what spends it.
    private static StatusData ReturnParcel()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new ModifyStatusStacksNode<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId),
                new SubtractExpression<DamageDealtTriggeredEffectContext>(
                    new ConstantExpression<DamageDealtTriggeredEffectContext>(0),
                    new CombatantStatusStacksExpression<DamageDealtTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId)))));

        return new StatusData
        {
            Id = ReturnParcelId,
            NameKey = "Return Parcel",
            DescriptionKey = "Returned lateness, waiting to be spat back. Spent by the next direct attack.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 5, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // "Whenever this enemy's Delinquency fully resolves: immediately apply 1 new Overdue from this same
    // source." 2 Overdue → collected → 0 → 1 again, so the loop never quite closes.
    private static StatusData OuroborosDelinquency() =>
        Delinquency(OuroborosDelinquencyId, "Return to Sender",
            "Nothing it is owed ever finishes being owed.",
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Opponent, new StatusDefinitionId(OverdueId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

    // ── Stage 3 — The Whispering Catalogue ────────────────────────────────────────────────────────────────

    public const string EntryReferenceMark = "referenced_entry";
    public const string AlphabetReferenceMark = "referenced_alphabet";
    public const string CitationReferenceMark = "referenced_citation";
    public const string EntryReferenceId = "second_person_reference";
    public const string AlphabetReferenceId = "fanged_alphabet_reference";
    public const string CitationReferenceId = "orphan_citation_reference";
    public const string AlphabetMemoryId = "learned_letter";

    private static readonly CounterId LastCostCounter = new("alphabet_last_cost");
    private static readonly CounterId LearnedCostCounter = new("alphabet_learned_cost");
    private static readonly CounterId CitedThisDrawCounter = new("alphabet_cited");

    // "Second-Person Entry cites you, again and again." The design chains the follow-up citation to the TYPE
    // of the card that fulfilled the last one; here every draw is simply cited afresh. See ADAPTATIONS.
    public static StatusData SecondPersonReference() =>
        Reference(EntryReferenceId, "You Are Cited Again", EntryReferenceMark,
            "One card in your hand is cited. Play it, or owe the Entry for it.");

    // "If the player plays two consecutive cards with the same Base Cost, remember that cost class; after the
    // next draw, cite a card of that cost." Two counters: what the last card cost, and what was learned.
    public static StatusData LearnedLetter()
    {
        var watch = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    // Two in a row at the same price is a class worth learning.
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            PlayedCost<CardPlayedTriggeredEffectContext>(),
                            ComparisonOperator.Equal,
                            OnWearer<CardPlayedTriggeredEffectContext>(LastCostCounter)),
                        SetOnWearer<CardPlayedTriggeredEffectContext>(
                            LearnedCostCounter, PlayedCost<CardPlayedTriggeredEffectContext>())),
                    SetOnWearer<CardPlayedTriggeredEffectContext>(
                        LastCostCounter, PlayedCost<CardPlayedTriggeredEffectContext>()),
                ])));

        return Rule(AlphabetMemoryId, "Learned Letter",
            "The alphabet listens for a price you pay twice in a row.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                watch, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // …and cites a card of the learned price, one per draw.
    public static StatusData FangedAlphabetReference() =>
        Reference(AlphabetReferenceId, "Bitten Letter", AlphabetReferenceMark,
            "A card of the price the alphabet learned is cited.",
            cite: new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    OnlyWearer, CitedThisDrawCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                IteratedCost<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.Equal,
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    OnlyWearer, LearnedCostCounter)),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    OnlyWearer, CitedThisDrawCounter),
                                ComparisonOperator.Equal,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                new TagId(AlphabetReferenceMark)),
                            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                                OnlyWearer, CitedThisDrawCounter,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
                        ]))),
            ]));

    // "The player may fulfil the citation by playing the exact card, OR another card of the same Base Cost and
    // the same type." The second path is answered where the citation is: compare what was played against the
    // card still carrying the mark.
    public static StatusData OrphanCitationReference()
    {
        var citation = Reference(CitationReferenceId, "Reconstruct the Source", CitationReferenceMark,
            "A card is cited; something enough like it will also do.");

        // Same price and same kind counts as the same citation.
        var standIn = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        PlayedCost<CardPlayedTriggeredEffectContext>(),
                        ComparisonOperator.Equal,
                        new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                            new FirstMarkedCardInOwnerZoneExpression<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, CardZone.Hand,
                                new TagId(CitationReferenceMark)),
                            StandardCombatIds.EnergyResource))),
                new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(CitationReferenceMark), remove: true),
                    markFilter: new TagId(CitationReferenceMark))));

        return citation with
        {
            Triggers =
            [
                .. citation.Triggers,
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    standIn, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ],
        };
    }

    // What the alphabet learned is kept on the PLAYER, not on the alphabet. Both moments it cares about — a
    // card played, a hand drawn — are the player's, and in a fight-wide trigger the player is the one
    // structural single target available ("who did this"): a "whoever wears the rule" selector can match
    // several combatants and so cannot be read as one counter at all.
    private static readonly ICombatantTargetSelector OnlyWearer = CombatantTargetSelectors.Source;

    private static ICombatExpression<TContext, int> PlayedCost<TContext>() where TContext : class =>
        new CardInstanceBaseCostExpression<TContext>(
            new TriggerEventCardInstanceExpression<TContext>(), StandardCombatIds.EnergyResource);

    private static ICombatExpression<TContext, int> IteratedCost<TContext>() where TContext : class =>
        new CardInstanceBaseCostExpression<TContext>(
            new IteratedCardExpression<TContext>(), StandardCombatIds.EnergyResource);

    private static ICombatExpression<TContext, int> OnWearer<TContext>(CounterId counter) where TContext : class =>
        new CombatantCounterExpression<TContext>(OnlyWearer, counter);

    private static IEffectNode<TContext> SetOnWearer<TContext>(
        CounterId counter, ICombatExpression<TContext, int> value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(OnlyWearer, counter, value, relative: false);

    // ── Redacted ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // "On its next play, positive numerical effects are halved, rounded down. Then Redacted clears." The engine
    // owns the halving: two reserved per-instance counters scale a card's next play and are consumed by it. The
    // content's part is the MARK beside them, which is what a rule can find and answer — an enemy that cares
    // "the player played a Redacted card" cannot see a scale factor, only a mark.
    public const string RedactedMark = "redacted";

    // The owner is a parameter and not `Source`: a redaction usually comes from an ENEMY's program, where
    // "source" is the enemy and the card belongs to the player. Getting that wrong marks a card nobody holds,
    // which is silent.
    public static IEffectNode<TContext> Redact<TContext>(
        ICombatantTargetSelector owner, ICardInstanceExpression<TContext> card) where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCardInstanceMarkCounterNode<TContext>(
                owner, card, StandardCombatIds.CardOutputScaleNumeratorCounter,
                new ConstantExpression<TContext>(1), relative: false),
            new SetCardInstanceMarkCounterNode<TContext>(
                owner, card, StandardCombatIds.CardOutputScaleDenominatorCounter,
                new ConstantExpression<TContext>(2), relative: false),
            new MarkCardInstanceNode<TContext>(owner, card, new TagId(RedactedMark)),
        ]);

    // "Redact one card": the top of the draw pile, the same reading of randomness Act I's Unclaimed Property
    // Tag uses — the pile is already shuffled, so its first card is the random one.
    public static IEffectNode<EnemyActionContext> RedactOne() =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            Redact<EnemyActionContext>(Opponent, new IteratedCardExpression<EnemyActionContext>()),
            takeFirst: 1);

    // ── Stage 5 — The Redaction Galleries ─────────────────────────────────────────────────────────────────

    // NOT BUILT: the Husk's "a played Redacted card becomes Misfiled" and the Portrait's "playing a Redacted
    // card opens the frame". Written three times — guarded, immediate, and with the rule on either side — and
    // never shown to work; the mark on the played card does not take. Not shipped unproven; see ADAPTATIONS.

    // ── Stage 4 — The Hushed Reading Room ─────────────────────────────────────────────────────────────────
    //
    // The Reading Room attacks the HAND: what you are still holding, how much of it you spent, what you never
    // got to say. All three rules live on the PLAYER (EncounterPassives.HeroOpeningStatuses) and watch the
    // whole fight, because the events they answer are the player's own.
    //
    // Three facts these rules are built on, all measured and all easy to get backwards:
    //   • the played-card count INCLUDES the card being played, so "the fourth card" is a count of four;
    //   • that card is already OUT of the hand when the rule runs, so reaching into the hand cannot catch it;
    //   • the played card is STILL IN THE HAND at the very first instant of a CardPlayed trigger, and gone a
    //     beat later — so a rule that reaches into the hand must let something run first or it takes the card
    //     that was just played, which is invisible because that card was on its way to the discard pile
    //     anyway. `Guarded` puts a no-op in front of every rule for exactly that reason. See
    //     RogueDeck.Sandbox.Tests/CardPlayedTriggerHandTimingTests.

    public const string ReservedSeatId = "reserved_seat";
    public const string ShrinkingMarginId = "shrinking_margin";
    public const string UnspokenWordId = "leave_one_word_unspoken";
    public const string VoiceId = "voice";

    private static readonly CounterId SeatTakenCounter = new("reserved_seat_taken");
    private static readonly CounterId MarginCounter = new("mute_margin");
    private static readonly CounterId MarginBittenCounter = new("mute_margin_bitten");
    private const int MarginStart = 5;
    private const int MarginFloor = 3;

    // A rule about the PLAYED card looks immediately: waiting a beat is what carries that card out of reach.
    private static EffectProgram<TContext> Immediate<TContext>(
        ICombatExpression<TContext, bool> when, IEffectNode<TContext> then) where TContext : class =>
        new(new ConditionalEffectNode<TContext>(when, then));

    // A guarded rule. The no-op is not decoration: it lets the played card leave the hand before anything
    // reaches in, which is the difference between taking a card the player still holds and taking the one they
    // just spent.
    private static EffectProgram<TContext> Guarded<TContext>(
        ICombatExpression<TContext, bool> when, IEffectNode<TContext> then) where TContext : class =>
        new(new CausalSequenceEffectNode<TContext>(
        [
            new NoOpEffectNode<TContext>(),
            new ConditionalEffectNode<TContext>(when, then),
        ]));

    // "After the player's fourth played card in a turn, the oldest remaining card in hand goes straight to
    // discard. Once per turn." A card taken this way was not played, so a Reference on it fails exactly as if
    // the hand had been put down holding it — the Stage-4 duo's whole lesson, and it needs no special case:
    // the citing enemy already collects from the discard pile.
    //
    // "The oldest remaining VALID non-Junk card" is read as the oldest card, full stop: a zone iteration can
    // require a tag but not refuse one. See ADAPTATIONS.
    public static StatusData ReservedSeat() =>
        Rule(ReservedSeatId, "Reserved Seat",
            "The table clears a place once a turn, whether or not you were using it.",
            [
                Watch("CardPlayed", Guarded(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                            PlayedThisTurn<CardPlayedTriggeredEffectContext>(
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                        Unspent<CardPlayedTriggeredEffectContext>(SeatTakenCounter)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new MoveCardToZoneNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                                CardZone.DiscardPile),
                            takeFirst: 1),
                        Spend<CardPlayedTriggeredEffectContext>(SeatTakenCounter),
                    ]))),
                ClearEachTurn(SeatTakenCounter),
            ]);

    // "A visible limit of 5. Exceed it and a card in your hand is Misfiled, and the limit shrinks by one to a
    // floor of 3; finish a turn inside it and it recovers by one, to a ceiling of 5."
    //
    // The limit lives on the player as a counter, and 0 means "not set yet": a starting status can put a
    // status on a combatant but not a number, so the margin introduces itself on the first draw.
    public static StatusData ShrinkingMargin() =>
        Rule(ShrinkingMarginId, "Shrinking Margin",
            "The margin allows five, and remembers every time you wrote past it.",
            [
                Watch("CardsDrawn", Guarded(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, MarginCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, MarginCounter,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(MarginStart), relative: false))),
                // The count includes the card in flight, so a count PAST the margin is the card that went past.
                Watch("CardPlayed", Guarded(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                            PlayedThisTurn<CardPlayedTriggeredEffectContext>(
                                ComparisonOperator.Greater,
                                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source, MarginCounter))),
                        Unspent<CardPlayedTriggeredEffectContext>(MarginBittenCounter)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand,
                            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                                new TagId(MisfiledMark)),
                            takeFirst: 1),
                        Step<CardPlayedTriggeredEffectContext>(-1, MarginFloor, ComparisonOperator.Greater),
                        Spend<CardPlayedTriggeredEffectContext>(MarginBittenCounter),
                    ]))),
                Watch("TurnEnded", Guarded(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        IsTheApplicant<TurnEndedTriggeredEffectContext>(),
                        Unspent<TurnEndedTriggeredEffectContext>(MarginBittenCounter)),
                    Step<TurnEndedTriggeredEffectContext>(1, MarginStart, ComparisonOperator.Less))),
                ClearEachTurn(MarginBittenCounter),
            ]);

    // "End a turn holding exactly one card and the Choir's guard breaks; end holding none and it finds a
    // Voice." The crescendo the design pays at two Voices is folded into the Voice status itself.
    public static StatusData LeaveOneWordUnspoken() =>
        Rule(UnspokenWordId, "Leave One Word Unspoken",
            "The choir listens to what you did not say.",
            [
                Watch("TurnEnded", Guarded(
                    IsTheApplicant<TurnEndedTriggeredEffectContext>(),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            HandHolds<TurnEndedTriggeredEffectContext>(1),
                            new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                                Choir, StandardCombatIds.BlockDefensivePool,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(-6))),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            HandHolds<TurnEndedTriggeredEffectContext>(0),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                Choir, new StatusDefinitionId(VoiceId),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                    ]))),
            ]);

    // Each Voice is +4 on the Choir's next direct attack, and the attack spends them all — two of them are the
    // design's +8, without a second status to hold the total.
    public static StatusData Voice()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new RemoveStatusNode<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(VoiceId)));

        return new StatusData
        {
            Id = VoiceId,
            NameKey = "Voice",
            DescriptionKey = "A word the choir kept, waiting on its next blow.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 4, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    private static readonly ICombatantTargetSelector Choir =
        CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static ICombatExpression<TContext, bool> PlayedThisTurn<TContext>(
        ComparisonOperator op, ICombatExpression<TContext, int> than) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CardsPlayedThisTurnExpression<TContext>(CombatantTargetSelectors.Source), op, than);

    private static ICombatExpression<TContext, bool> HandHolds<TContext>(int cards) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantZoneCardCountExpression<TContext>(CombatantTargetSelectors.Source, CardZone.Hand),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(cards));

    private static IEffectNode<TContext> Step<TContext>(int step, int stop, ComparisonOperator past)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, MarginCounter),
                past, new ConstantExpression<TContext>(stop)),
            new SetCombatantCounterNode<TContext>(
                CombatantTargetSelectors.Source, MarginCounter,
                new ConstantExpression<TContext>(step), relative: true));

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, latch,
            new ConstantExpression<TContext>(1), relative: false);

    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Anywhere);

    private static StatusTriggerData ClearEachTurn(CounterId latch) =>
        Watch("CardsDrawn", Guarded(
            IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, latch,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false)));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // The player, addressed from inside an enemy's own trigger and serializable into the export.
    private static readonly ICombatantTargetSelector Opponent =
        CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
