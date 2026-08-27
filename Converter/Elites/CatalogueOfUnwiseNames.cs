using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── The Catalogue of Unwise Names (Act II elite) ──────────────────────────────────────────────────────────
//
// Three empty lines in a black book. The danger is not that the Archive already knows your deck — it is that
// you may voluntarily make something official. Naming a card cheapens it once; the Citation the naming incurs
// is chosen and SHOWN at that moment, so the price is known before the benefit is taken.
//
// The book's whole ledger is kept as counters on the PLAYER and marks on the player's cards, because both
// sides of the deal need to read it: the Catalogue's intents cash entries, and the player's own plays turn a
// Recognized entry into an Established one. A counter on the Catalogue could not be read by a player-side
// rule as a single number (a "the enemies with this status" selector is a set, not a combatant).
public static class CatalogueOfUnwiseNames
{
    public const string EnemyId = "catalogue_of_unwise_names";

    public const string TheBlackCatalogueId = "the_black_catalogue";
    public const string CatalogueRulesId = "catalogue_rules";
    public const string CatalogueReferenceId = "catalogue_citation_of_record";
    public const string NextNameStrikesId = "next_name_strikes";

    // The entry's own state, carried by the named card instance.
    public const string RecognizedMark = "recognized";
    public const string EstablishedMark = "established";
    public const string CitationOfCostMark = "citation_of_cost";
    public const string CitationOfFormMark = "citation_of_form";
    public const string CitationOfRecordMark = "citation_of_record";
    public const string CatalogueReferenceMark = "referenced_by_catalogue";

    private static CounterId EntriesCounter => new("catalogue_entries");
    private static CounterId NamingPreparedCounter => new("catalogue_naming_prepared");
    private static CounterId RotationCounter => new("catalogue_rotation");
    private static CounterId CashedCounter => new("catalogue_cashed");

    private const int MaxEntries = 3;
    private const int DeclineBlock = 8;
    private const int MaxCostCitationBlock = 17;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Catalogues =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheBlackCatalogueId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheBlackCatalogueId, "The Black Catalogue",
            "Three empty lines in a black book. Naming a card of yours makes it cheaper once — and the Citation "
            + "that naming incurs is chosen and shown before you take the benefit."),
        NextNameStrikes(),
        CatalogueRules(),
        // Citation of Record reuses the act's own Reference machinery: the tracked instance becomes Referenced
        // the next time it reaches the hand through normal draw, and an unfulfilled reference costs an Overdue
        // from the Catalogue. The `cite` override is what makes it that ONE card rather than any card.
        ActTwo.Reference(CatalogueReferenceId, "Citation of Record", CatalogueReferenceMark,
            "A named card the Catalogue has cashed for the record: it is cited the next time you draw it.",
            cite: CiteTheNamedCard()),
    ];

    // ── 7.2 Enter a Name ──────────────────────────────────────────────────────────────────────────────────
    //
    // Prepared by an intent, offered after the player's next normal draw. Both halves of the design's choice
    // survive: naming is voluntary, and declining while an eligible card exists pays the Catalogue 8 Block.
    // With no eligible card the prompt never appears, and nothing is owed.
    private static StatusData CatalogueRules()
    {
        var offer = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        Pending<CardsDrawnTriggeredEffectContext>(NamingPreparedCounter),
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            Count<CardsDrawnTriggeredEffectContext>(EntriesCounter),
                            ComparisonOperator.Less,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(MaxEntries))),
                    EligibleCardInHand()),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Self, NamingPreparedCounter,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                    new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                        [EnterAName(), Decline()],
                        ["enter a name in the Catalogue", "decline to be named"],
                        count: 1, purpose: "the Catalogue offers you a line"),
                ])));

        // 7.3: playing a Recognized card spends the benefit and turns the entry into an Established one — a
        // debt the Catalogue can cash at its leisure. Read immediately: the card is still in hand at the first
        // instant of the trigger, and the marks travel with the instance to wherever it lands.
        var establish = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    Played<CardPlayedTriggeredEffectContext>(), new TagId(RecognizedMark)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Self, Played<CardPlayedTriggeredEffectContext>(),
                        new TagId(RecognizedMark), remove: true),
                    // Nothing restores the cost here: a per-copy price is a promise the ENGINE keeps once and
                    // spends at the play itself (CardPlay), which is precisely the design's "after full
                    // resolution, Recognized is removed". Adding a +1 of our own would leave the card dearer
                    // than it was printed.
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Self, Played<CardPlayedTriggeredEffectContext>(), new TagId(EstablishedMark)),
                ])));

        return Rule(CatalogueRulesId, "The Catalogue's Lines",
            "Three lines remain in the black book. A named card costs 1 less the next time you play it, and "
            + "the Citation you incur is shown when you sign.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    offer, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    establish, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    // The player picks a card, and it is marked ONCE — every later step reads the mark instead of asking
    // again, because a chooser expression consulted twice asks the player twice.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> EnterAName() =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self,
                new ChosenCardInZoneExpression<CardsDrawnTriggeredEffectContext>(
                    CardZone.Hand, "name a card for the Catalogue"),
                new TagId(RecognizedMark)),
            new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                Self, Named<CardsDrawnTriggeredEffectContext>(CardZone.Hand),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
            // 7.4: the Citation is chosen and shown NOW. The three rotate, so the book is legible rather than
            // random — the player can read the next liability off the two already standing.
            AssignCitation(0, CitationOfCostMark),
            AssignCitation(1, CitationOfFormMark),
            AssignCitation(2, CitationOfRecordMark),
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                Self, RotationCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: true),
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                Self, EntriesCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: true),
        ]);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> AssignCitation(int slot, string mark) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                    Count<CardsDrawnTriggeredEffectContext>(RotationCounter),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                ComparisonOperator.Equal,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(slot)),
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self, Named<CardsDrawnTriggeredEffectContext>(CardZone.Hand), new TagId(mark)));

    // "If eligible cards exist but the player voluntarily declines: Catalogue gains 8 Block."
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Decline() =>
        new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Catalogues,
            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.IterationTarget,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(DeclineBlock)));

    // "One eligible non-Junk card instance from hand." Junk is the one thing the book will not dignify.
    private static ICombatExpression<CardsDrawnTriggeredEffectContext, bool> EligibleCardInHand() =>
        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
            new SubtractExpression<CardsDrawnTriggeredEffectContext>(
                new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand),
                new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                    Self, CardZone.Hand, new TagId(CardAuthoring.JunkTag))),
            ComparisonOperator.Greater,
            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0));

    // Citation of Record cites the named card itself, not the first card that happens to be in hand.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteTheNamedCard() =>
        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
            Self,
            new FirstMarkedCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(
                Self, CardZone.Hand, new TagId(CitationOfRecordMark)),
            new TagId(CatalogueReferenceMark));

    // ── Cashing an entry ──────────────────────────────────────────────────────────────────────────────────
    //
    // "Cash the oldest eligible Established Entry." Established cards are cards that have been PLAYED, so the
    // search runs discard → draw → hand, which is pile order and therefore deterministic. ADAPTATION: "oldest"
    // is read as first-in-pile-order rather than by a timestamp the engine does not keep.
    private static IEffectNode<EnemyActionContext> CashOldestEntry() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new SetCombatantCounterNode<EnemyActionContext>(
                Opponent, CashedCounter, new ConstantExpression<EnemyActionContext>(0), relative: false),
            CashFrom(CardZone.DiscardPile),
            CashFrom(CardZone.DrawPile),
            CashFrom(CardZone.Hand),
        ]);

    private static IEffectNode<EnemyActionContext> CashFrom(CardZone zone)
    {
        var card = new FirstMarkedCardInOwnerZoneExpression<EnemyActionContext>(
            Opponent, zone, new TagId(EstablishedMark));

        return new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCounterExpression<EnemyActionContext>(Opponent, CashedCounter),
                    ComparisonOperator.Equal, new ConstantExpression<EnemyActionContext>(0)),
                new CardInstanceHasMarkExpression<EnemyActionContext>(card, new TagId(EstablishedMark))),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, CashedCounter, new ConstantExpression<EnemyActionContext>(1), relative: false),
                CitationOfCost(card),
                CitationOfForm(card),
                CitationOfRecord(card),
                // The entry is spent: the line is struck and the book has room again.
                new MarkCardInstanceNode<EnemyActionContext>(Opponent, card, new TagId(EstablishedMark), remove: true),
                new MarkCardInstanceNode<EnemyActionContext>(Opponent, card, new TagId(CitationOfCostMark), remove: true),
                new MarkCardInstanceNode<EnemyActionContext>(Opponent, card, new TagId(CitationOfFormMark), remove: true),
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, EntriesCounter, new ConstantExpression<EnemyActionContext>(-1), relative: true),
            ]));
    }

    // 5 + 3 × the card's PRINTED cost, never past 17 — the base cost, so a card cheapened by anything else
    // still cites at what it was printed at.
    private static IEffectNode<EnemyActionContext> CitationOfCost(ICardInstanceExpression<EnemyActionContext> card) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new CardInstanceHasMarkExpression<EnemyActionContext>(card, new TagId(CitationOfCostMark)),
            new GainBlockNode<EnemyActionContext>(Self,
                new MinExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(MaxCostCitationBlock),
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(5),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3),
                            new CardInstanceBaseCostExpression<EnemyActionContext>(
                                card, StandardCombatIds.EnergyResource))))));

    // ADAPTATION: the design's Attack / Skill / Power are the generic engine's card types. B&B's own primary
    // types are Deed and Working, so the citation reads those — a Deed cites as an attack (the Catalogue's
    // next direct attack hits for 5 more), a Working cites as a skill (14 Block). Anything else takes the
    // design's own "predefined neutral fallback".
    private static IEffectNode<EnemyActionContext> CitationOfForm(ICardInstanceExpression<EnemyActionContext> card) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new CardInstanceHasMarkExpression<EnemyActionContext>(card, new TagId(CitationOfFormMark)),
            new ConditionalEffectNode<EnemyActionContext>(
                new CardInstanceHasTagExpression<EnemyActionContext>(card, new TagId(CardAuthoring.DeedTag)),
                new ApplyStatusNode<EnemyActionContext>(
                    Self, new StatusDefinitionId(NextNameStrikesId),
                    new ConstantExpression<EnemyActionContext>(1)),
                @else: new ConditionalEffectNode<EnemyActionContext>(
                    new CardInstanceHasTagExpression<EnemyActionContext>(card, new TagId(CardAuthoring.WorkingTag)),
                    new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(14)),
                    @else: new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(8)))));

    // The record keeps the card: it is cited the next time it reaches the hand, and the mark that says so is
    // what ActTwo.Reference watches for.
    private static IEffectNode<EnemyActionContext> CitationOfRecord(ICardInstanceExpression<EnemyActionContext> card) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new CardInstanceHasMarkExpression<EnemyActionContext>(card, new TagId(CitationOfRecordMark)),
            new MarkCardInstanceNode<EnemyActionContext>(Opponent, card, new TagId(CitationOfRecordMark)));

    // "+5 damage on the Catalogue's next direct attack", spent by that attack — the Return Parcel shape.
    private static StatusData NextNameStrikes()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new ModifyStatusStacksNode<DamageDealtTriggeredEffectContext>(
                Self, new StatusDefinitionId(NextNameStrikesId),
                new NegateExpression<DamageDealtTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<DamageDealtTriggeredEffectContext>(
                        Self, new StatusDefinitionId(NextNameStrikesId)))));

        return new StatusData
        {
            Id = NextNameStrikesId,
            NameKey = "The Name Strikes",
            DescriptionKey = "A cited Deed. The Catalogue's next direct attack hits for 5 more, and spends it.",
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

    // ── 7.6 Intents ───────────────────────────────────────────────────────────────────────────────────────
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        // Prepare Enter a Name for the next player turn, and gain 10 Block.
        "enter_the_name_in_black_salt" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new SetCombatantCounterNode<EnemyActionContext>(
                Opponent, NamingPreparedCounter,
                new ConstantExpression<EnemyActionContext>(1), relative: false),
            new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(10)),
        ])),
        "already_known" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
            [CashOldestEntry(), Damage(8)])),
        "close_the_catalogue" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(14),
            new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(10)),
        ])),
        "recitation_under_breath" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(9),
            // Reference one card in the current hand — the act's own citation, issued by the Catalogue.
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new CardInOwnerZoneExpression<EnemyActionContext>(Opponent, CardZone.Hand, 0),
                new TagId(CatalogueReferenceMark)),
        ])),
        "recite_an_unwise_name" or "recite_an_unwise_name_again" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            CashOldestEntry(),
            // 14, or 20 with all three lines full — the book at its loudest. Entries never pass 3, so
            // integer division by 3 is exactly the "and only then" the design asks for.
            new DealDamageNode<EnemyActionContext>(Opponent,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(14),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(6),
                        new DivideExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(Opponent, EntriesCounter),
                            new ConstantExpression<EnemyActionContext>(MaxEntries))))),
        ])),
        _ => null,
    };

    private static EffectProgram<EnemyActionContext> Program(IEffectNode<EnemyActionContext> body) => new(body);

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, new ConstantExpression<EnemyActionContext>(amount));

    // ── shared shapes ─────────────────────────────────────────────────────────────────────────────────────

    private static ICardInstanceExpression<TContext> Played<TContext>() where TContext : class =>
        new TriggerEventCardInstanceExpression<TContext>();

    private static ICardInstanceExpression<TContext> Named<TContext>(CardZone zone) where TContext : class =>
        new FirstMarkedCardInOwnerZoneExpression<TContext>(
            CombatantTargetSelectors.Source, zone, new TagId(RecognizedMark));

    private static ICombatExpression<TContext, int> Count<TContext>(CounterId counter) where TContext : class =>
        new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter);

    private static ICombatExpression<TContext, bool> Pending<TContext>(CounterId counter) where TContext : class =>
        new ComparisonExpression<TContext>(
            Count<TContext>(counter), ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // A marker still owes the player an explanation on hover: naming it twice explains nothing.
    private static StatusData Marker(string id, string name, string description) =>
        Rule(id, name, description, []);

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
