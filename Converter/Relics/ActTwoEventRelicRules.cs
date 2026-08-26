using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Events;

namespace BnbContent.Converter.Relics;

// The in-combat rules the five Act-II Event relics install.
//
// The archives' prizes are all about ONE card — the one you did not get to play, the one you kept back, the
// one that keeps coming home. So each of these marks a copy and then answers for it, which is the machinery
// the inscriptions already use (ActTwoEventObjects) rather than anything new.
public static class ActTwoEventRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
        [UnreturnedLibraryCard, ReversibleShelfLabel, BlankCameo, VowBead, VowKept, InvertedSealstone];

    private const string Unreturned = "unreturned";
    private const string ShelfLabelled = "shelf_labelled";
    private const string Cameo = "blank_cameo_card";
    private const string Sealed = "inverted_seal";
    private const string VowKeptId = "vow_kept";

    // What each of the two remembering relics writes on the hand as it is dealt, so that "the card you did not
    // play" is still answerable once the hand has been put down. One mark each: a player may hold either relic
    // without the other, so neither may lean on the other's bookkeeping.
    private const string LibraryHeld = "library_card_held";
    private const string LabelHeld = "shelf_label_held";

    // "Once per combat, the first non-Junk card entering discard unplayed returns next turn, costs 0 that turn
    // and Exhausts on play."
    //
    // ★ A turn-end program cannot see the hand — the discard runs first — so "the card you did not play" has
    // to be written down while it is still true: the hand is marked as it is dealt, each played card loses the
    // mark as it is played, and what is still marked in the discard pile when the turn ends is exactly what
    // was held and never used. The Exhaust half is dropped: a card Exhausts because its DEFINITION says so.
    public static readonly StatusData UnreturnedLibraryCard = Rule(
        "unreturned_library_card_rule", "Unreturned Library Card",
        "Once each fight, a card you never got to play is waiting for you next turn, and it is free.",
        [
            NoteTheHand(LibraryHeld),
            ForgetWhatWasPlayed(LibraryHeld),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    Once<TurnEndedTriggeredEffectContext>("unreturned_library_card",
                        FirstUnplayed<TurnEndedTriggeredEffectContext>(
                            "unreturned_library_card_pick", LibraryHeld,
                            Mark<TurnEndedTriggeredEffectContext>(Unreturned))),
                    ClearHeld<TurnEndedTriggeredEffectContext>(LibraryHeld),
                ])),
                nameof(TriggerEvent.TurnEnded)),
            // Wherever the card ended up — put down, shuffled back in, or dealt again on its own — it is in
            // your hand at the start of the turn and it is free.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [Fetch(CardZone.DiscardPile), Fetch(CardZone.DrawPile), Fetch(CardZone.Hand)])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Once per combat, remember the first non-Junk card name moved from hand unplayed; the next same-name card
    // entering hand draws 1 and costs 1 less that turn."
    //
    // ADAPTATION: a rule cannot remember a NAME — there is nowhere to put one and no way to compare it later.
    // The label remembers the COPY instead: the card you put down is the card that comes back easier, whenever
    // it next reaches your hand.
    public static readonly StatusData ReversibleShelfLabel = Rule(
        "reversible_shelf_label_rule", "Reversible Shelf Label",
        "A label that reads the same either way. The card you put down is easier when it comes round again.",
        [
            NoteTheHand(LabelHeld),
            ForgetWhatWasPlayed(LabelHeld),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    Once<TurnEndedTriggeredEffectContext>("reversible_shelf_label",
                        FirstUnplayed<TurnEndedTriggeredEffectContext>(
                            "reversible_shelf_label_pick", LabelHeld,
                            Mark<TurnEndedTriggeredEffectContext>(ShelfLabelled))),
                    ClearHeld<TurnEndedTriggeredEffectContext>(LabelHeld),
                ])),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Discount<CardsDrawnTriggeredEffectContext>(1),
                        Unmark<CardsDrawnTriggeredEffectContext>(ShelfLabelled),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]),
                    markFilter: new TagId(ShelfLabelled), takeFirst: 1)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "After opening draw choose a non-Junk card: Retain, cost −1, protected from specific enemy card
    // targeting/markers until played."
    //
    // ADAPTATION: nothing hears a mark being put ON a card, so the protection is a correction rather than a
    // shield — whatever the archive writes on the cameo's card is struck off at the start of the next round,
    // for as long as the card is unplayed. The same beat-late shape True Name uses.
    public static readonly StatusData BlankCameo = Rule(
        "blank_cameo_rule", "Blank Cameo",
        "A portrait with the face left out. One card in your hand is kept, cheaper, and unmarkable.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Once<CardsDrawnTriggeredEffectContext>("blank_cameo",
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            SitFor(CardAuthoring.DeedTag), SitFor(CardAuthoring.WorkingTag),
                            SitFor(CardAuthoring.RiteTag),
                        ])),
                    // …and every round after that, whatever the archive wrote on it is taken off again.
                    Uncover(CardZone.Hand), Uncover(CardZone.DrawPile),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "At turn start optionally cap yourself at 3 non-Junk cards; playing exactly 3 grants next turn +1 Energy
    // and +1 Draw."
    //
    // ADAPTATION: the cap is dropped and only the reward is kept. A cap the player opts into would be a prompt
    // every single turn, and breaking the Vow costs nothing anyway — so the Bead simply notices restraint.
    public static readonly StatusData VowBead = Rule(
        "vow_bead_rule", "Vow Bead",
        "A bead counted at the end of a quiet turn: file exactly three things that matter and tomorrow is "
        + "easier.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        NonJunkPlayed<TurnEndedTriggeredEffectContext>(),
                        ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(3)),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(VowKeptId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // The Vow's promise, as a thing that is spent: applied at the end of the quiet turn, paid at the next
    // turn's draw, then gone.
    public static readonly StatusData VowKept = Rule(
        VowKeptId, "Vow Observed", "The vow was kept; tomorrow is easier.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // Held, not gained: the turn's refill has already filled the pool (see HeldEnergy).
                    HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                    new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(VowKeptId)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "After opening draw choose a Deed/Working; its first play returns that exact card to hand after
    // resolution instead of its normal post-play destination."
    public static readonly StatusData InvertedSealstone = Rule(
        "inverted_sealstone_rule", "Inverted Sealstone",
        "A seal pressed the wrong way round. The card it marks comes back to your hand the first time you "
        + "file it.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                Once<CardsDrawnTriggeredEffectContext>("inverted_sealstone",
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        SealFirst(CardAuthoring.DeedTag), SealFirst(CardAuthoring.WorkingTag),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
            // The card is put where it belongs after the play; taking it back at the next draw is the same
            // card in the same hand, one beat later.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [TakeBack(CardZone.DiscardPile), TakeBack(CardZone.ExhaustPile)])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // The first card of one type in the opening hand sits for the portrait — kept, cheaper, and unmarkable.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> SitFor(string type) =>
        PickFirst("blank_cameo_pick", type,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                Mark<CardsDrawnTriggeredEffectContext>(Cameo),
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    StandardCombatIds.RetainedCardMark),
                Discount<CardsDrawnTriggeredEffectContext>(1),
            ]));

    // The first card of one type in the opening hand, sealed — tried Deed first, then Working, with a latch so
    // only one is.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> SealFirst(string type) =>
        PickFirst("inverted_sealstone_pick", type, Mark<CardsDrawnTriggeredEffectContext>(Sealed));

    // One card out of the opening hand, of the named type, and only while nothing has been picked yet — the
    // three player types are offered in turn so exactly one card is chosen however the hand fell.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> PickFirst(
        string latch, string type, IEffectNode<CardsDrawnTriggeredEffectContext> body) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new CounterId(latch)),
                ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, CardZone.Hand,
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    body,
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new CounterId(latch),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
                ]),
                tagFilter: new TagId(type), takeFirst: 1));

    // The unreturned card, brought home from wherever the turn left it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Fetch(CardZone zone) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), CardZone.Hand),
                Discount<CardsDrawnTriggeredEffectContext>(9),
                Unmark<CardsDrawnTriggeredEffectContext>(Unreturned),
            ]),
            markFilter: new TagId(Unreturned));

    // The archive's marks, struck off the cameo's card wherever it is standing.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Uncover(CardZone zone) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                .. ActTwoEventObjects.ArchiveMarks().Select(Unmark<CardsDrawnTriggeredEffectContext>),
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    StandardCombatIds.CardOutputScaleNumeratorCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    StandardCombatIds.CardOutputScaleDenominatorCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
            ]),
            markFilter: new TagId(Cameo));

    // The sealed card, fetched home from wherever the play put it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> TakeBack(CardZone zone) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), CardZone.Hand),
                Unmark<CardsDrawnTriggeredEffectContext>(Sealed),
            ]),
            markFilter: new TagId(Sealed));

    // The hand, written down as it is dealt.
    private static StatusTriggerData NoteTheHand(string held) =>
        Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, CardZone.Hand,
                Mark<CardsDrawnTriggeredEffectContext>(held))),
            nameof(TriggerEvent.CardsDrawn));

    // …and struck off card by card as each one is used, so what is left is what was only held.
    private static StatusTriggerData ForgetWhatWasPlayed(string held) =>
        Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                new TagId(held), remove: true)),
            nameof(TriggerEvent.CardPlayed));

    // "The first non-Junk card you held and did not play." A zone filter names one tag at a time, so the three
    // player types are tried in turn and a latch stops after the first that lands. The hand itself is gone by
    // now, so the search is in the discard pile the hand was just put into.
    private static IEffectNode<TContext> FirstUnplayed<TContext>(
        string latch, string held, IEffectNode<TContext> body) where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(
                CombatantTargetSelectors.Source, new CounterId(latch),
                new ConstantExpression<TContext>(0), relative: false),
            Attempt(latch, held, CardAuthoring.DeedTag, body),
            Attempt(latch, held, CardAuthoring.WorkingTag, body),
            Attempt(latch, held, CardAuthoring.RiteTag, body),
        ]);

    private static IEffectNode<TContext> Attempt<TContext>(
        string latch, string held, string type, IEffectNode<TContext> body) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(
                    CombatantTargetSelectors.Source, new CounterId(latch)),
                ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
            new ForEachCardInZoneNode<TContext>(
                CombatantTargetSelectors.Source, CardZone.DiscardPile,
                new CausalSequenceEffectNode<TContext>(
                [
                    body,
                    new SetCombatantCounterNode<TContext>(
                        CombatantTargetSelectors.Source, new CounterId(latch),
                        new ConstantExpression<TContext>(1), relative: false),
                ]),
                tagFilter: new TagId(type), markFilter: new TagId(held), takeFirst: 1));

    // The note is torn up at the end of the turn it was about; the next draw writes a fresh one. A card still
    // in hand (Retained) keeps its mark, because it is still being held.
    private static IEffectNode<TContext> ClearHeld<TContext>(string held) where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            CombatantTargetSelectors.Source, CardZone.DiscardPile,
            Unmark<TContext>(held), markFilter: new TagId(held));

    private static IEffectNode<TContext> Mark<TContext>(string mark) where TContext : class =>
        new MarkCardInstanceNode<TContext>(
            CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(), new TagId(mark));

    private static IEffectNode<TContext> Unmark<TContext>(string mark) where TContext : class =>
        new MarkCardInstanceNode<TContext>(
            CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(), new TagId(mark),
            remove: true);

    private static IEffectNode<TContext> Discount<TContext>(int amount) where TContext : class =>
        new SetCardInstanceMarkCounterNode<TContext>(
            CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(),
            StandardCombatIds.CardCostDeltaCounter,
            new ConstantExpression<TContext>(-amount), relative: true);

    // Everything that was not Junk — the count the Vow is about.
    private static ICombatExpression<TContext, int> NonJunkPlayed<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new CardsPlayedThisTurnExpression<TContext>(CombatantTargetSelectors.Source),
            new CardsPlayedThisTurnWithTagExpression<TContext>(
                CombatantTargetSelectors.Source, new TagId(CardAuthoring.JunkTag)));

    private static IEffectNode<TContext> Once<TContext>(string id, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(
                    CombatantTargetSelectors.Source, new CounterId(id + "_done")),
                ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
            new CausalSequenceEffectNode<TContext>(
            [
                body,
                new SetCombatantCounterNode<TContext>(
                    CombatantTargetSelectors.Source, new CounterId(id + "_done"),
                    new ConstantExpression<TContext>(1), relative: false),
            ]));

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) =>
        RelicAuthoring.Rule(id, name, description, triggers);

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Bearer);
}
