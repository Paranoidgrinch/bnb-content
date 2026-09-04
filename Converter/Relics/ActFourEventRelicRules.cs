using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules the five Act-IV Event relics install — the ones the Labyrinth's first ten doors hand
// over (the other four arrive with events 11–20).
//
// The Green Docket's prizes were about the SHAPE of a turn. The Labyrinth's are about the act's own five
// words: what a turn COST (the Cup), what fades and what is held in place (the Knot, the Jar), what the
// register does to the first thing entered in it (the Cartouche), and what a false measure is answered with
// (the Weight). Three of them are readings of keywords the act already owns rather than new machinery, which
// is the point: an Act-IV relic that did not speak Act IV would be an Act-III relic found in a tomb.
//
// ⚠ EVERYTHING here is a PROPERTY, ids and counters alike (the IV-21 lesson): a `static readonly` field
// declared below the status that names it is still null when that status is built, and nothing says so.
public static class ActFourEventRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
        [CupOfTheLowestMark, RedLinenKnot, BlankCartouche, JarOfBorrowedBreath, BrokenRoyalWeight];

    public const string CupId = "cup_of_the_lowest_mark";
    public const string KnotId = "red_linen_knot";
    public const string CartoucheId = "blank_cartouche";
    public const string JarId = "jar_of_borrowed_breath";
    public const string WeightId = "broken_royal_weight";

    public const int KnotBlock = 8;
    public const int WeightBlock = 10;

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;

    private static CounterId CupUsed => new("cup_of_the_lowest_mark_used");
    private static CounterId CupOwed => new("cup_of_the_lowest_mark_owed");
    private static CounterId KnotUsed => new("red_linen_knot_used");
    private static CounterId CartoucheUsed => new("blank_cartouche_used");
    private static CounterId JarUsed => new("jar_of_borrowed_breath_used");
    private static CounterId JarOwed => new("jar_of_borrowed_breath_owed");
    private static CounterId WeightUsed => new("broken_royal_weight_used");

    // ── Cup of the Lowest Mark ────────────────────────────────────────────────────────────────────────────

    // "First time each combat you end a turn with exactly 1 unspent Energy, heal 4 HP and draw +1 next turn."
    //
    // The nilometer reads the LOWEST mark, so the cup is interested in the turn you very nearly spent. The
    // "+1 next turn" is the act's LEDGER idiom rather than a status: what is owed is written at the turn's
    // end and paid at the next hand, because a one-turn draw status applied AT a turn end would have to
    // survive its own fading rule.
    public static StatusData CupOfTheLowestMark => Rule(
        CupId, "Cup of the Lowest Mark",
        "The first turn each fight you end with exactly one Energy unspent, the cup fills: heal 4, and draw "
        + "one more at your next hand.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Unspent<TurnEndedTriggeredEffectContext>(CupUsed),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                                You, StandardCombatIds.EnergyResource),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        Spend<TurnEndedTriggeredEffectContext>(CupUsed),
                        Set<TurnEndedTriggeredEffectContext>(CupOwed, 1),
                        new HealNode<TurnEndedTriggeredEffectContext>(
                            You, new ConstantExpression<TurnEndedTriggeredEffectContext>(4)),
                    ]))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, CupOwed),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Set<CardsDrawnTriggeredEffectContext>(CupOwed, 0),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Red Linen Knot ────────────────────────────────────────────────────────────────────────────────────

    // "Start combat with 8 Block. First time each combat a positive Status would naturally lose stacks or
    // duration, prevent that loss and gain 8 Block."
    //
    // NOT AN ADAPTATION, a reading: preventing one natural fade is EMBALMED, which this act already owns and
    // already writes down every time it spends itself (ActFour.DecaysPreserved). So the knot opens the fight
    // with 8 Block and one Embalmed, and pays its second 8 Block at the hand after the linen has held —
    // which is also the only place Block may be given, since Block expires at the start of its owner's turn.
    public static StatusData RedLinenKnot => Rule(
        KnotId, "Red Linen Knot",
        "The fight opens with 8 Block and one Embalmed. The first time the linen holds something in place "
        + "that would have faded, you are wrapped again: 8 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(KnotBlock)),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                You, new StatusDefinitionId(ActFour.EmbalmedId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            Unspent<CardsDrawnTriggeredEffectContext>(KnotUsed),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    You, ActFour.DecaysPreserved),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Spend<CardsDrawnTriggeredEffectContext>(KnotUsed),
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(KnotBlock)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Blank Cartouche ───────────────────────────────────────────────────────────────────────────────────

    // "Draw +1 on turn 1. First time each combat you gain Inscribed, remove 1 Inscribed."
    //
    // A cartouche with no name in it: the first thing entered on your behalf slides straight back off. Both
    // halves are the same object — an extra card for a register that will not hold the first entry.
    //
    // ⚠ In a status-APPLICATION event the Source is whoever APPLIED it, so every reach here is to the
    // combatant wearing the cartouche, addressed by the rule it carries.
    public static StatusData BlankCartouche => Rule(
        CartoucheId, "Blank Cartouche",
        "An extra card in your first hand. The first Inscribed you gain each fight finds no name to be "
        + "written under and comes straight off again.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                        You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(SlidesOff<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied)),
            Trigger(SlidesOff<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged)),
        ]);

    private static EffectProgram<TContext> SlidesOff<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(CartoucheId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ActFour.InscribedId)),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(wearer, CartoucheUsed),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        wearer, CartoucheUsed, new ConstantExpression<TContext>(1), relative: false),
                    new ModifyStatusStacksNode<TContext>(
                        wearer, new StatusDefinitionId(ActFour.InscribedId),
                        new ConstantExpression<TContext>(-1)),
                ])));
    }

    // ── Jar of Borrowed Breath ────────────────────────────────────────────────────────────────────────────

    // "First time each combat a temporary negative status leaves you completely, draw 1 and heal 3."
    //
    // A status whose last stack goes is an EXPIRY and one taken off whole is a REMOVAL, and the design's
    // "leaves you completely" is both — so the jar listens on both doors and answers on whichever comes
    // first. The design's "temporary" is dropped: nothing in a fight remembers whether an affliction was
    // meant to last.
    //
    // The heal is paid where it happens; the CARD is a ledger paid at the next hand, because the commonest
    // moment for an affliction to leave is the turn's end, and a card drawn into a hand that is about to be
    // discarded is not a card.
    public static StatusData JarOfBorrowedBreath => Rule(
        JarId, "Jar of Borrowed Breath",
        "The first affliction to leave you completely each fight is breath given back: heal 3, and one more "
        + "card at your next hand.",
        [
            // ⚠ Both doors are ANYWHERE, and StatusExpired is why: under Bearer scope that trigger does not
            // mean "on the wearer", it means "the wearer's own status is the one that ran out" — the jar
            // would only ever hear itself go. Anywhere hears every expiry in the fight, so the program's
            // first question is whether the thing that left left the jar's owner.
            Trigger(BreathReturned<StatusExpiredTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusExpired), StatusTriggerScope.Anywhere),
            Trigger(BreathReturned<StatusRemovedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusRemoved), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, JarOwed),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Set<CardsDrawnTriggeredEffectContext>(JarOwed, 0),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static EffectProgram<TContext> BreathReturned<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(JarId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new AndExpression<TContext>(
                        // It left the jar's owner, and it was an affliction.
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(JarId)),
                        new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff)),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(wearer, JarUsed),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        wearer, JarUsed, new ConstantExpression<TContext>(1), relative: false),
                    new SetCombatantCounterNode<TContext>(
                        wearer, JarOwed, new ConstantExpression<TContext>(1), relative: false),
                    new HealNode<TContext>(wearer, new ConstantExpression<TContext>(3)),
                ])));
    }

    // ── Broken Royal Weight ───────────────────────────────────────────────────────────────────────────────

    // "Start combat with 10 Block. Once per combat when Weighed is failed, prevent the direct HP loss and
    // gain Burdened 1 instead."
    //
    // ADAPTATION: a failed measure costs no HP in this port. The act answers a miss through the body that
    // SET the measure, by error band, so there is no direct loss to intercept — what the design is buying is
    // "the first miss does not hurt". The broken weight pays it the only way a relic can: the first failed
    // measure each fight is answered at the next hand with another 10 Block, and the false weight is heavier
    // for having been used — one Burdened. Block at the hand rather than at the miss, because Block given
    // during a turn that has already begun is swept away at the start of the next one.
    public static StatusData BrokenRoyalWeight => Rule(
        WeightId, "Broken Royal Weight",
        "The fight opens with 10 Block. The first measure you miss each fight is taken on the weight "
        + "instead: 10 Block at your next hand, and one Burdened for using a false weight.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(WeightBlock))),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            Unspent<CardsDrawnTriggeredEffectContext>(WeightUsed),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    You, ActFour.MeasuresFailed),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Spend<CardsDrawnTriggeredEffectContext>(WeightUsed),
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(WeightBlock)),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                You, new StatusDefinitionId(ActFour.BurdenedId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, bool> FirstRound<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            new RoundNumberExpression<TContext>(),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(1));

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(You, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        Set<TContext>(latch, 1);

    private static IEffectNode<TContext> Set<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            You, id, new ConstantExpression<TContext>(value), relative: false);

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

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
