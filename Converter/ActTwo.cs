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
    ];

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
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
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
