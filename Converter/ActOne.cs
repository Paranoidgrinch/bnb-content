using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.EnemyRules;

namespace BnbContent.Converter;

// Act I — the City: the four bodies reworked on 2026-11-08 (CONTENT_REVIEW.md E1-E4, approved as proposed). Each
// rule is one visible decision the player makes with the cards in hand:
//   · the Exception Imp's Loophole charges a Filing Fee on the first Deed of the turn — pay it and its next Block
//     lapses, dodge Deeds altogether and it grows;
//   · the Inverted Hourglass's sand runs out if the turn ends with 2 Energy unspent, the Fading Number Token fades
//     while nothing hits it;
//   · the Red Tape Golem wraps a card in your hand, and a Working cuts it free;
//   · the Queue-Crier Homunculus calls a cost, and the card you lead with answers it or earns Panic.
public static class ActOne
{
    // ── E1 Exception Imp: Loophole ──────────────────────────────────────────────────────────────────────────
    public const string ImpId = "exception_imp";
    public const string LoopholeId = PassiveStatuses.LoopholeId;
    public const string FilingFeeId = "filing_fee";
    public const string WaiverId = "waiver";

    // ── E2 Inverted Hourglass and Fading Number Token ───────────────────────────────────────────────────────
    public const string HourglassId = "inverted_hourglass";
    public const string SpiltSandId = "spilt_sand";
    public const int SandSpillEnergy = 2;
    public const int GlassBase = 8, GlassPerGrain = 4;
    public const string TokenId = "fading_number_token";
    public const string FadingInkId = "fading_ink";
    public const string FadedNumeralId = "faded_numeral";
    public const int FadedPercent = 75;
    private static CounterId TokenHitCounter => new("token_hit");

    // ── E3 Red Tape Golem ───────────────────────────────────────────────────────────────────────────────────
    public const string GolemId = "red_tape_golem";
    public const string RedTapeId = "red_tape_binding";
    public const string WrappedMark = "wrapped_in_red_tape";
    private static CounterId WrappedThisTurnCounter => new("red_tape_wrapped");

    // ── E4 Queue-Crier Homunculus ───────────────────────────────────────────────────────────────────────────
    public const string CrierId = "queue_crier_homunculus";
    public const string WaitingOrderId = "the_waiting_order";
    public const string CalledMark = "called_by_the_crier";
    public const int HighestCall = 5;
    public static CounterId CalledCostCounter => new("called_cost");
    public static string CalledNumberId(int cost) => $"called_number_{cost}";

    public static IEnumerable<StatusData> Statuses() =>
    [
        Rule(LoopholeId, "Loophole",
            "Your first Deed each turn that costs Energy costs 1 more. Pay it and its next Block lapses; play no Deed "
            + "at all and it gains 1 Strength."),
        Rule(FilingFeeId, "Filing Fee",
            "The Imp's Loophole: your next Deed that costs Energy costs 1 more this turn.",
            modifiers:
            [
                new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, 1,
                    RestrictDamageKind: null, RestrictSourceCardTag: CardAuthoring.DeedTag),
            ]),
        Rule(WaiverId, "Waiver", "The fee was paid: its next Block lapses."),

        Rule(SpiltSandId, "Spilt Sand", "The sand ran out: its next Turn the Glass falls empty.",
            [On("TurnEnded", E<TurnEndedTriggeredEffectContext>.Remove(E<TurnEndedTriggeredEffectContext>.Self, SpiltSandId))]),
        FadingInk(),
        Rule(FadedNumeralId, "Faded Numeral", "Faded: it takes 25 % less damage. The next hit restores it.",
            modifiers:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent,
                    FadedPercent, RestrictDamageKind: null),
            ]),

        Rule(RedTapeId, "Red Tape",
            "Each turn it wraps a card in your hand: that card costs 1 more. Play a Working to cut it free and take 1 "
            + "of its Strength."),

        Rule(WaitingOrderId, "The Waiting Order",
            "Each turn it calls an Energy cost. Lead with a card of that cost and it costs 0; lead with any other card "
            + "and gain 1 Panic."),
        .. Enumerable.Range(0, HighestCall + 1).Select(cost => Rule(CalledNumberId(cost),
            cost == HighestCall ? $"Called Number: {cost}+" : $"Called Number: {cost}",
            $"Lead with a card that costs {cost}{(cost == HighestCall ? " or more" : "")}: it costs 0. Lead with "
            + "anything else: gain 1 Panic.")),
    ];

    public static IReadOnlyList<EncounterTriggerData>? Triggers(string enemyId) => enemyId switch
    {
        ImpId => [ChargeTheFee(), PayTheFee(), CloseTheFile()],
        HourglassId => [SpillTheSand()],
        GolemId => [WrapACard(), CutTheTape(), UnwrapAtTurnEnd()],
        CrierId => [CallANumber(), AnswerTheCall(), CloseTheCall()],
        _ => null,
    };

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) => $"{enemyId}.{intentId}" switch
    {
        "exception_imp.hide_in_the_footnote" => new(E<EnemyActionContext>.Seq(
            E<EnemyActionContext>.Damage(E<EnemyActionContext>.Event, E<EnemyActionContext>.C(4)),
            Lapsing(E<EnemyActionContext>.Block(E<EnemyActionContext>.Self, E<EnemyActionContext>.C(6))))),
        "exception_imp.exception_to_the_exception" => new(
            Lapsing(E<EnemyActionContext>.Block(CombatantTargetSelectors.AllAlliesOfSource, E<EnemyActionContext>.C(7)))),
        "inverted_hourglass.turn_the_glass" => TurnTheGlass(),
        _ => null,
    };

    // ── E1 ───────────────────────────────────────────────────────────────────────────────────────────────────

    // A Block the Imp would raise, unless a Waiver stands — then the Waiver is spent instead.
    private static IEffectNode<EnemyActionContext> Lapsing(IEffectNode<EnemyActionContext> block)
    {
        var self = E<EnemyActionContext>.Self;
        return E<EnemyActionContext>.If(E<EnemyActionContext>.Wears(self, WaiverId),
            E<EnemyActionContext>.Remove(self, WaiverId), block);
    }

    // The fee is charged as the player's turn opens, while an Imp stands.
    private static EncounterTriggerData ChargeTheFee()
    {
        return Encounter("TurnStarted",
            E<TurnStartedTriggeredEffectContext>.If(
                E<TurnStartedTriggeredEffectContext>.And(
                    E<TurnStartedTriggeredEffectContext>.Applicant, E<TurnStartedTriggeredEffectContext>.Present(LoopholeId)),
                E<TurnStartedTriggeredEffectContext>.Apply(E<TurnStartedTriggeredEffectContext>.Self, FilingFeeId)));
    }

    // A Deed that cost Energy has paid it: the fee is gone and every Imp's next Block lapses. A free Deed slips
    // past the fee — the engine cannot raise a cost a card does not have — so it neither pays nor clears it.
    private static EncounterTriggerData PayTheFee()
    {
        var self = E<CardPlayedTriggeredEffectContext>.Self;
        var played = E<CardPlayedTriggeredEffectContext>.Played;
        return Encounter("CardPlayed",
            E<CardPlayedTriggeredEffectContext>.If(
                E<CardPlayedTriggeredEffectContext>.And(
                    E<CardPlayedTriggeredEffectContext>.Applicant,
                    E<CardPlayedTriggeredEffectContext>.Wears(self, FilingFeeId),
                    E<CardPlayedTriggeredEffectContext>.Tagged(played, CardAuthoring.DeedTag),
                    E<CardPlayedTriggeredEffectContext>.Is(
                        E<CardPlayedTriggeredEffectContext>.CostOf(played), ComparisonOperator.Greater, 0)),
                E<CardPlayedTriggeredEffectContext>.Seq(
                    E<CardPlayedTriggeredEffectContext>.Remove(self, FilingFeeId),
                    E<CardPlayedTriggeredEffectContext>.Apply(E<CardPlayedTriggeredEffectContext>.Wearers(LoopholeId), WaiverId))));
    }

    // The turn ends: an unpaid fee is withdrawn, and a turn without a single Deed was a loophole well used.
    private static EncounterTriggerData CloseTheFile()
    {
        var self = E<TurnEndedTriggeredEffectContext>.Self;
        return Encounter("TurnEnded",
            E<TurnEndedTriggeredEffectContext>.If(E<TurnEndedTriggeredEffectContext>.Applicant,
                E<TurnEndedTriggeredEffectContext>.Seq(
                    E<TurnEndedTriggeredEffectContext>.Remove(self, FilingFeeId),
                    E<TurnEndedTriggeredEffectContext>.If(
                        E<TurnEndedTriggeredEffectContext>.Is(
                            new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                                self, new TagId(CardAuthoring.DeedTag)),
                            ComparisonOperator.Equal, 0),
                        E<TurnEndedTriggeredEffectContext>.Apply(
                            E<TurnEndedTriggeredEffectContext>.Wearers(LoopholeId), "strength")))));
    }

    // ── E2 ───────────────────────────────────────────────────────────────────────────────────────────────────

    // A turn that ends with 2 Energy unspent runs the glass dry: the banked grains are gone and the next Turn the
    // Glass falls empty.
    private static EncounterTriggerData SpillTheSand()
    {
        var self = E<TurnEndedTriggeredEffectContext>.Self;
        var it = E<TurnEndedTriggeredEffectContext>.It;
        return Encounter("TurnEnded",
            E<TurnEndedTriggeredEffectContext>.If(
                E<TurnEndedTriggeredEffectContext>.And(
                    E<TurnEndedTriggeredEffectContext>.Applicant,
                    E<TurnEndedTriggeredEffectContext>.Is(
                        E<TurnEndedTriggeredEffectContext>.Energy(self), ComparisonOperator.GreaterOrEqual, SandSpillEnergy)),
                E<TurnEndedTriggeredEffectContext>.Each(E<TurnEndedTriggeredEffectContext>.Wearers(PassiveStatuses.StolenSandId),
                    E<TurnEndedTriggeredEffectContext>.Seq(
                        E<TurnEndedTriggeredEffectContext>.Set(it, PassiveStatuses.StolenSandCounter, E<TurnEndedTriggeredEffectContext>.C(0)),
                        E<TurnEndedTriggeredEffectContext>.Apply(it, SpiltSandId)))));
    }

    private static EffectProgram<EnemyActionContext> TurnTheGlass()
    {
        var self = E<EnemyActionContext>.Self;
        var sand = E<EnemyActionContext>.Counter(self, PassiveStatuses.StolenSandCounter);
        return new(E<EnemyActionContext>.If(E<EnemyActionContext>.Wears(self, SpiltSandId),
            E<EnemyActionContext>.Remove(self, SpiltSandId),
            E<EnemyActionContext>.Seq(
                E<EnemyActionContext>.Damage(E<EnemyActionContext>.Event,
                    E<EnemyActionContext>.Plus(E<EnemyActionContext>.C(GlassBase), E<EnemyActionContext>.Times(sand, GlassPerGrain))),
                E<EnemyActionContext>.Set(self, PassiveStatuses.StolenSandCounter, E<EnemyActionContext>.C(0)))));
    }

    // Fading Ink: a round in which nothing hits the Token fades it (25 % less damage), and the next hit — which the
    // fading still softens — restores it. "A round" runs from one of its turn ends to the next, so the player's
    // whole turn is in it.
    private static StatusData FadingInk()
    {
        var struck = E<DamageReceivedTriggeredEffectContext>.Event;
        var self = E<TurnEndedTriggeredEffectContext>.Self;
        return Rule(FadingInkId, "Fading Ink",
            "Each round nothing hits it, it fades: it takes 25 % less damage until it is hit again.",
            [
                On("DamageTaken", E<DamageReceivedTriggeredEffectContext>.Seq(
                    E<DamageReceivedTriggeredEffectContext>.Set(struck, TokenHitCounter, E<DamageReceivedTriggeredEffectContext>.C(1)),
                    E<DamageReceivedTriggeredEffectContext>.Remove(struck, FadedNumeralId))),
                On("TurnEnded", E<TurnEndedTriggeredEffectContext>.Seq(
                    E<TurnEndedTriggeredEffectContext>.If(
                        E<TurnEndedTriggeredEffectContext>.Is(
                            E<TurnEndedTriggeredEffectContext>.Counter(self, TokenHitCounter), ComparisonOperator.Equal, 0),
                        E<TurnEndedTriggeredEffectContext>.Apply(self, FadedNumeralId)),
                    E<TurnEndedTriggeredEffectContext>.Set(self, TokenHitCounter, E<TurnEndedTriggeredEffectContext>.C(0)))),
            ]);
    }

    // ── E3 ───────────────────────────────────────────────────────────────────────────────────────────────────

    // After the hand is dealt, the first card in it that costs Energy is wrapped: +1 on its price (the engine's
    // per-copy cost delta). A free card cannot be priced up, so it is passed over.
    private static EncounterTriggerData WrapACard()
    {
        var self = E<CardsDrawnTriggeredEffectContext>.Self;
        var card = E<CardsDrawnTriggeredEffectContext>.Iterated;
        var wrapped = E<CardsDrawnTriggeredEffectContext>.Counter(self, WrappedThisTurnCounter);
        return Encounter("CardsDrawn",
            E<CardsDrawnTriggeredEffectContext>.If(
                E<CardsDrawnTriggeredEffectContext>.And(
                    E<CardsDrawnTriggeredEffectContext>.Applicant, E<CardsDrawnTriggeredEffectContext>.Present(RedTapeId)),
                E<CardsDrawnTriggeredEffectContext>.Seq(
                    E<CardsDrawnTriggeredEffectContext>.Set(self, WrappedThisTurnCounter, E<CardsDrawnTriggeredEffectContext>.C(0)),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(self, CardZone.Hand,
                        E<CardsDrawnTriggeredEffectContext>.If(
                            E<CardsDrawnTriggeredEffectContext>.And(
                                E<CardsDrawnTriggeredEffectContext>.Is(
                                    E<CardsDrawnTriggeredEffectContext>.CostOf(card), ComparisonOperator.Greater, 0),
                                E<CardsDrawnTriggeredEffectContext>.Is(wrapped, ComparisonOperator.Equal, 0)),
                            E<CardsDrawnTriggeredEffectContext>.Seq(
                                E<CardsDrawnTriggeredEffectContext>.Mark(card, WrappedMark),
                                E<CardsDrawnTriggeredEffectContext>.Reprice(card, E<CardsDrawnTriggeredEffectContext>.C(1)),
                                E<CardsDrawnTriggeredEffectContext>.Set(self, WrappedThisTurnCounter,
                                    E<CardsDrawnTriggeredEffectContext>.C(1))))))));
    }

    // A wrapped card that is played has paid its way out (the play spends the delta). A Working played while a
    // wrapped card is still in hand cuts it free and takes 1 of the Golem's Strength.
    private static EncounterTriggerData CutTheTape()
    {
        var played = E<CardPlayedTriggeredEffectContext>.Played;
        var card = E<CardPlayedTriggeredEffectContext>.Iterated;
        var it = E<CardPlayedTriggeredEffectContext>.It;
        var self = E<CardPlayedTriggeredEffectContext>.Self;
        return Encounter("CardPlayed",
            E<CardPlayedTriggeredEffectContext>.If(E<CardPlayedTriggeredEffectContext>.Applicant,
                E<CardPlayedTriggeredEffectContext>.Seq(
                    E<CardPlayedTriggeredEffectContext>.If(E<CardPlayedTriggeredEffectContext>.Marked(played, WrappedMark),
                        E<CardPlayedTriggeredEffectContext>.Mark(played, WrappedMark, remove: true)),
                    E<CardPlayedTriggeredEffectContext>.If(
                        E<CardPlayedTriggeredEffectContext>.And(
                            E<CardPlayedTriggeredEffectContext>.Tagged(played, CardAuthoring.WorkingTag),
                            E<CardPlayedTriggeredEffectContext>.Is(
                                E<CardPlayedTriggeredEffectContext>.InZone(self, CardZone.Hand, WrappedMark),
                                ComparisonOperator.Greater, 0)),
                        E<CardPlayedTriggeredEffectContext>.Seq(
                            E<CardPlayedTriggeredEffectContext>.ForMarked(CardZone.Hand, WrappedMark,
                                E<CardPlayedTriggeredEffectContext>.Seq(
                                    E<CardPlayedTriggeredEffectContext>.Mark(card, WrappedMark, remove: true),
                                    E<CardPlayedTriggeredEffectContext>.Reprice(card, E<CardPlayedTriggeredEffectContext>.C(-1)))),
                            E<CardPlayedTriggeredEffectContext>.Each(E<CardPlayedTriggeredEffectContext>.Wearers(RedTapeId),
                                E<CardPlayedTriggeredEffectContext>.If(
                                    E<CardPlayedTriggeredEffectContext>.Is(
                                        new IterationTargetStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                                            new StatusDefinitionId("strength")),
                                        ComparisonOperator.Greater, 0),
                                    E<CardPlayedTriggeredEffectContext>.AddStacks(it, "strength",
                                        E<CardPlayedTriggeredEffectContext>.C(-1)))))))));
    }

    // The tape comes off with the turn: whatever is still wrapped, wherever it went, gets its price back.
    private static EncounterTriggerData UnwrapAtTurnEnd()
    {
        var card = E<TurnEndedTriggeredEffectContext>.Iterated;
        IEffectNode<TurnEndedTriggeredEffectContext> Unwrap(CardZone zone) =>
            E<TurnEndedTriggeredEffectContext>.ForMarked(zone, WrappedMark, E<TurnEndedTriggeredEffectContext>.Seq(
                E<TurnEndedTriggeredEffectContext>.Mark(card, WrappedMark, remove: true),
                E<TurnEndedTriggeredEffectContext>.Reprice(card, E<TurnEndedTriggeredEffectContext>.C(-1))));
        return Encounter("TurnEnded",
            E<TurnEndedTriggeredEffectContext>.If(E<TurnEndedTriggeredEffectContext>.Applicant,
                E<TurnEndedTriggeredEffectContext>.Seq(
                    Unwrap(CardZone.Hand), Unwrap(CardZone.DiscardPile), Unwrap(CardZone.DrawPile), Unwrap(CardZone.ExhaustPile))));
    }

    // ── E4 ───────────────────────────────────────────────────────────────────────────────────────────────────

    // After the hand is dealt the Crier calls the cost of one card in it, at random — so the call can always be
    // answered — and every card in hand at that cost is priced down to 0 until the turn's first card is played.
    private static EncounterTriggerData CallANumber()
    {
        var self = E<CardsDrawnTriggeredEffectContext>.Self;
        var card = E<CardsDrawnTriggeredEffectContext>.Iterated;
        var it = E<CardsDrawnTriggeredEffectContext>.It;
        var called = E<CardsDrawnTriggeredEffectContext>.Counter(self, CalledCostCounter);
        ICombatExpression<CardsDrawnTriggeredEffectContext, int> Call(ICardInstanceExpression<CardsDrawnTriggeredEffectContext> c) =>
            E<CardsDrawnTriggeredEffectContext>.Min(E<CardsDrawnTriggeredEffectContext>.CostOf(c),
                E<CardsDrawnTriggeredEffectContext>.C(HighestCall));

        return Encounter("CardsDrawn",
            E<CardsDrawnTriggeredEffectContext>.If(
                E<CardsDrawnTriggeredEffectContext>.And(
                    E<CardsDrawnTriggeredEffectContext>.Applicant,
                    E<CardsDrawnTriggeredEffectContext>.Present(WaitingOrderId),
                    E<CardsDrawnTriggeredEffectContext>.Is(
                        E<CardsDrawnTriggeredEffectContext>.InZone(self, CardZone.Hand), ComparisonOperator.Greater, 0)),
                E<CardsDrawnTriggeredEffectContext>.Seq(
                    E<CardsDrawnTriggeredEffectContext>.Set(self, CalledCostCounter,
                        Call(new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(self, CardZone.Hand))),
                    E<CardsDrawnTriggeredEffectContext>.Each(E<CardsDrawnTriggeredEffectContext>.Wearers(WaitingOrderId),
                        new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            .. Enumerable.Range(0, HighestCall + 1).Select(cost =>
                                E<CardsDrawnTriggeredEffectContext>.If(
                                    E<CardsDrawnTriggeredEffectContext>.Is(called, ComparisonOperator.Equal, cost),
                                    E<CardsDrawnTriggeredEffectContext>.Apply(it, CalledNumberId(cost)))),
                        ])),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(self, CardZone.Hand,
                        E<CardsDrawnTriggeredEffectContext>.If(
                            E<CardsDrawnTriggeredEffectContext>.Is(Call(card), ComparisonOperator.Equal, called),
                            E<CardsDrawnTriggeredEffectContext>.Seq(
                                E<CardsDrawnTriggeredEffectContext>.Mark(card, CalledMark),
                                E<CardsDrawnTriggeredEffectContext>.Reprice(card,
                                    E<CardsDrawnTriggeredEffectContext>.Minus(E<CardsDrawnTriggeredEffectContext>.C(0),
                                        E<CardsDrawnTriggeredEffectContext>.CostOf(card)))))))));
    }

    // The turn's first card answers the call — or does not, and that is 1 Panic. Either way the call is over.
    private static EncounterTriggerData AnswerTheCall()
    {
        var self = E<CardPlayedTriggeredEffectContext>.Self;
        var played = E<CardPlayedTriggeredEffectContext>.Played;
        return Encounter("CardPlayed",
            E<CardPlayedTriggeredEffectContext>.If(
                E<CardPlayedTriggeredEffectContext>.And(
                    E<CardPlayedTriggeredEffectContext>.Applicant,
                    E<CardPlayedTriggeredEffectContext>.Is(
                        new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(self), ComparisonOperator.Equal, 1)),
                E<CardPlayedTriggeredEffectContext>.Seq(
                    E<CardPlayedTriggeredEffectContext>.If(
                        E<CardPlayedTriggeredEffectContext>.And(
                            E<CardPlayedTriggeredEffectContext>.Present(WaitingOrderId),
                            E<CardPlayedTriggeredEffectContext>.Not(E<CardPlayedTriggeredEffectContext>.Marked(played, CalledMark))),
                        E<CardPlayedTriggeredEffectContext>.Apply(self, "panic")),
                    // The answering card has spent its discount by being played: only its mark comes off.
                    E<CardPlayedTriggeredEffectContext>.If(E<CardPlayedTriggeredEffectContext>.Marked(played, CalledMark),
                        E<CardPlayedTriggeredEffectContext>.Mark(played, CalledMark, remove: true)),
                    EndTheCall<CardPlayedTriggeredEffectContext>())));
    }

    private static EncounterTriggerData CloseTheCall() =>
        Encounter("TurnEnded",
            E<TurnEndedTriggeredEffectContext>.If(E<TurnEndedTriggeredEffectContext>.Applicant,
                EndTheCall<TurnEndedTriggeredEffectContext>()));

    // Every card still priced by the call gets its cost back, and the called number comes off the board.
    private static IEffectNode<T> EndTheCall<T>() where T : class
    {
        var card = E<T>.Iterated;
        IEffectNode<T> Restore(CardZone zone) =>
            E<T>.ForMarked(zone, CalledMark, E<T>.Seq(
                E<T>.Mark(card, CalledMark, remove: true),
                E<T>.Reprice(card, E<T>.CostOf(card))));
        return E<T>.Seq(
            Restore(CardZone.Hand), Restore(CardZone.DiscardPile), Restore(CardZone.DrawPile), Restore(CardZone.ExhaustPile),
            E<T>.Each(E<T>.Wearers(WaitingOrderId), new SequenceEffectNode<T>(
                [.. Enumerable.Range(0, HighestCall + 1).Select(cost => E<T>.Remove(E<T>.It, CalledNumberId(cost)))])));
    }
}
