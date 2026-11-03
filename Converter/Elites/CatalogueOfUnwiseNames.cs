using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Catalogue of Unwise Names (Act II elite) ──────────────────────────────────────────────────────────
//
// Three empty lines in a black book, and you have to fill one every turn. Once your hand is drawn you name a card
// of it, and the book enters it one of two ways, your choice:
//
//   CHEAPER — it costs 1 less and Exhausts when played.
//   DEARER  — it costs 1 more and stays in your hand until you play it.
//
// Either entry holds until the card is played — a cheaper card discarded unplayed is still cheaper the next time
// you draw it; a dearer one, once played, goes where it always goes and costs what it always cost.
//
// REWORKED 2026-11-03 (user, Act II trial): the Recognized and Established entries, the three Citations and the
// declined entry's Block are gone.
public static class CatalogueOfUnwiseNames
{
    public const string EnemyId = "catalogue_of_unwise_names";

    public const string TheCatalogueId = "the_black_catalogue";
    public const string CatalogueRulesId = "catalogue_rules";
    public const string CheaperMark = "named_cheaper";
    public const string DearerMark = "named_dearer";
    private const string NamingMark = "being_named";

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Catalogues =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheCatalogueId));

    private const string CatalogueText =
        "Each turn name a card in your hand: it costs 1 less and Exhausts, or 1 more and stays in hand, until played.";

    public static IEnumerable<StatusData> Statuses() =>
    [
        Rule(TheCatalogueId, "The Black Catalogue", CatalogueText, []),
        Rules(),
    ];

    private static StatusData Rules()
    {
        var naming = new FirstMarkedCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand, new TagId(NamingMark));
        IEffectNode<CardsDrawnTriggeredEffectContext> Mark(string mark, bool remove = false) =>
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Self, naming, new TagId(mark), remove);
        IEffectNode<CardsDrawnTriggeredEffectContext> Price(int delta) =>
            new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                Self, naming, StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(delta), relative: false);
        // A card named again is entered afresh: the other entry comes off it first.
        var cheaper = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Mark(DearerMark, remove: true),
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Self, naming, StandardCombatIds.RetainedCardMark, remove: true),
            Price(-1),
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Self, naming, StandardCombatIds.TemporaryCopyMark),
            Mark(CheaperMark),
        ]);
        var dearer = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardsDrawnTriggeredEffectContext>(naming, new TagId(CheaperMark)),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Mark(CheaperMark, remove: true),
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Self, naming, StandardCombatIds.TemporaryCopyMark, remove: true),
                ])),
            Price(+1),
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Self, naming, StandardCombatIds.RetainedCardMark),
            Mark(DearerMark),
        ]);

        // The card is chosen and marked ONCE; every later step reads the mark, because a chooser consulted twice
        // asks the player twice.
        var offer = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CountTargetsExpression<CardsDrawnTriggeredEffectContext>(Catalogues),
                        ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand),
                        ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self,
                        new ChosenCardInZoneExpression<CardsDrawnTriggeredEffectContext>(
                            CardZone.Hand, "name a card for the Catalogue"),
                        new TagId(NamingMark)),
                    new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                        [cheaper, dearer],
                        ["cheaper: it costs 1 less and Exhausts", "dearer: it costs 1 more and stays in hand"],
                        count: 1, purpose: "how does the Catalogue enter the card you named?"),
                    Mark(NamingMark, remove: true),
                ])));

        // Played, the entry is spent: the engine has already spent the price, and a dearer card stops being kept.
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var spend = new EffectProgram<CardPlayedTriggeredEffectContext>(new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, new TagId(DearerMark)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Self, played, new TagId(DearerMark), remove: true),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Self, played, StandardCombatIds.RetainedCardMark, remove: true),
                ])),
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Self, played, new TagId(CheaperMark), remove: true),
        ]));

        return Rule(CatalogueRulesId, "The Catalogue's Lines", CatalogueText,
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    TheOpeningHand.Only(offer), CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

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
