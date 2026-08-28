using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Relics;

// The cards six of the Act-III boss relics hand over. A combat in the Green Docket has no free actions, only
// cards — the act settled that when Make Amends was built — so a relic's "once per turn, free action" is a
// card the fight puts in the holder's hand at the bell. Each of them is free, comes back while its relic is
// worn, and does nothing at all once it has been used this turn.
//
// The counters they set are the relics' own: the card is the hand the player reaches out with, and the
// relic is what remembers.
public static class ActThreeBossRelicCards
{
    public const string TwineId = "counter_petition_twine_action";
    public const string HoneyId = "honey_spoon_action";
    public const string CushionId = "better_chair_cushion_action";
    public const string TinId = "last_slice_tin_action";
    public const string GraceId = "royal_grace_cup_action";
    public const string TallyId = "silver_name_tally_action";

    public static readonly TagId TwineTag = new("counter_petition_twine_action");
    public static readonly TagId HoneyTag = new("honey_spoon_action");
    public static readonly TagId CushionTag = new("better_chair_cushion_action");
    public static readonly TagId TinTag = new("last_slice_tin_action");
    public static readonly TagId GraceTag = new("royal_grace_cup_action");
    public static readonly TagId TallyTag = new("silver_name_tally_action");

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    public static IReadOnlyList<CardData> All() =>
    [
        Twine(), HoneySpoon(), BetterChair(), LastSlice(), RoyalGrace(), SilverName(),
    ];

    // A free card that is not spent by being played: it exhausts, survives the turn boundary, and the relic
    // that offered it puts another in the hand at the next bell.
    private static CardData Action(
        string id, string name, string text, TagId tag, IEffectNode<CardPlayContext> program) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [tag, new TagId(CardAuthoring.FormTag)],
            Program = new EffectProgram<CardPlayContext>(program),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.Hand,
        };

    private static ICombatExpression<CardPlayContext, bool> NotYet(string counter) =>
        new ComparisonExpression<CardPlayContext>(
            new CombatantCounterExpression<CardPlayContext>(Self, new CounterId(counter)),
            ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(0));

    private static IEffectNode<CardPlayContext> Once(string counter, IEffectNode<CardPlayContext> body) =>
        new ConditionalEffectNode<CardPlayContext>(
            NotYet(counter),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new SetCombatantCounterNode<CardPlayContext>(
                    Self, new CounterId(counter), new ConstantExpression<CardPlayContext>(1),
                    relative: false),
                body,
            ]));

    // ── The Ombudsman's twine ─────────────────────────────────────────────────────────────────────────────
    private static CardData Twine() =>
        Action(TwineId, "Counter-Petition", "Discard a card, draw a card, and gain 1 Energy. Once a turn.",
            TwineTag,
            Once("counter_petition_twine",
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new MoveCardToZoneNode<CardPlayContext>(
                        Self,
                        new ChosenCardInZoneExpression<CardPlayContext>(
                            CardZone.Hand, "re-argue a card", excludeTag: TwineTag),
                        CardZone.DiscardPile),
                    new DrawCardsNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(1)),
                    HeldEnergy.Hold<CardPlayContext>(1),
                ])));

    // ── Grandmother's three courtesies, kept by the holder ────────────────────────────────────────────────
    private static CardData HoneySpoon() =>
        Action(HoneyId, "A Little Honey",
            "Gain 2 Energy. Then end the turn with at least 1 Energy, or the spoon costs you 6 HP.",
            HoneyTag,
            Once("honey_spoon", HeldEnergy.Hold<CardPlayContext>(2)));

    private static CardData BetterChair() =>
        Action(CushionId, "The Better Chair",
            "Gain 14 Block. Then end the turn holding a real card, or the cushion costs you 6 HP.",
            CushionTag,
            Once("better_chair_cushion",
                new GainBlockNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(14))));

    private static CardData LastSlice() =>
        Action(TinId, "Take Another Slice",
            "Draw 2. Then play no more than four real cards this turn, or the tin costs you 6 HP.",
            TinTag,
            Once("last_slice_tin",
                new DrawCardsNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(2))));

    // ── The Queen's cup, and her tally ────────────────────────────────────────────────────────────────────
    private static CardData RoyalGrace() =>
        Action(GraceId, "Royal Grace",
            "Choose one: 1 Energy, a card, or 10 Block. Every enemy guards for 6. Once a turn.",
            GraceTag,
            Once("royal_grace_cup",
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new ChooseOptionsNode<CardPlayContext>(
                    [
                        HeldEnergy.Hold<CardPlayContext>(1),
                        new DrawCardsNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(1)),
                        new GainBlockNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(10)),
                    ],
                    ["an Energy", "a card", "10 Block"],
                    count: 1, purpose: "the cup offers"),
                    // The court is generous, and the court's other guests are grateful.
                    new ForEachTargetEffectNode<CardPlayContext>(
                        CombatantTargetSelectors.AllEnemiesOfSource,
                        new GainBlockNode<CardPlayContext>(
                            CombatantTargetSelectors.IterationTarget,
                            new ConstantExpression<CardPlayContext>(6))),
                ])));

    // "Remove all its Block; its next attack deals 10 less; the next card you play this turn costs 0."
    //
    // ADAPTATION: "its next attack deals 10 less" is written as 10 Block for the holder, taken now — the
    // engine has no per-enemy outgoing-damage reduction, and what the holder feels is the same ten points.
    // The free card is the price of the next card refunded, which is how every "costs 0" in this pool works.
    private static CardData SilverName() =>
        Action(TallyId, "Speak the Silver Name",
            "Once a combat: one enemy's guard is gone, you gain 10 Block against what it was about to do, "
            + "and the next card you play this turn is refunded.",
            TallyTag,
            Once("silver_name_tally",
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new ModifyDefensivePoolNode<CardPlayContext>(
                        CombatantTargetSelectors.LowestHealthEnemyOfSource,
                        StandardCombatIds.BlockDefensivePool,
                        new NegateExpression<CardPlayContext>(
                            new CombatantDefensivePoolExpression<CardPlayContext>(
                                CombatantTargetSelectors.LowestHealthEnemyOfSource,
                                StandardCombatIds.BlockDefensivePool))),
                    new GainBlockNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(10)),
                    new ForEachCardInZoneNode<CardPlayContext>(
                        Self, CardZone.Hand,
                        new SetCardInstanceMarkCounterNode<CardPlayContext>(
                            Self, new IteratedCardExpression<CardPlayContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardPlayContext>(-3), relative: true),
                        takeFirst: 1),
                ])));
}
