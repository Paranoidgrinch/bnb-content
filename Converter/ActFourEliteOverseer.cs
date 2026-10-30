using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Mummified Overseer of the Linen House. REWORKED in the content cut of 2026-10-30: the
// Overseer wraps your hand.
//
// Each turn, once the hand is drawn, it wraps two cards of it in linen (cards already wrapped, and cards that
// cannot be played, are passed over). A wrapped card costs 0 — the price is written on that copy and spent by
// the play — but playing it buries you a little: 1 Entombed. A wrapped card stays wrapped until it is played,
// so the linen piles up in the deck if it is left alone. The offer is good, and five Entombed lose the turn:
// the fight is deciding how much free play the burial is worth.
//
// Its own intents tax the hand (Burdened makes every unwrapped card dearer, which makes the free ones dearer
// to refuse) rather than burying, so the Entombed on the player is the player's own doing.
public static partial class ActFour
{
    public const string LinenOverseerEnemyId = "mummified_overseer_of_the_linen_house";

    public const string LinenHouseId = "the_linen_house";
    public const string LinenWrappedMark = "linen_wrapped";

    public const int WrapsPerTurn = 2;

    // A discount larger than any printed cost: the per-copy price clamps at 0 (CardInstanceCostModifier).
    private const int FreeOfCharge = -9;

    private static CounterId WrappedThisTurn => new("linen_wrapped_this_turn");

    public static EffectProgram<EnemyActionContext>? LinenOverseerIntent(string enemyId, string intentId) => null;

    public static IReadOnlyList<StatusData> LinenOverseerStatuses() => [TheLinenHouse()];

    public static StatusData TheLinenHouse()
    {
        var overseer = Bearer(LinenHouseId);
        var mark = new TagId(LinenWrappedMark);
        var card = new IteratedCardExpression<CardsDrawnTriggeredEffectContext>();

        var wrapOne = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(overseer, WrappedThisTurn),
                    ComparisonOperator.Less, N<CardsDrawnTriggeredEffectContext>(WrapsPerTurn)),
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new NotExpression<CardsDrawnTriggeredEffectContext>(
                        new CardInstanceHasMarkExpression<CardsDrawnTriggeredEffectContext>(card, mark)),
                    new NotExpression<CardsDrawnTriggeredEffectContext>(
                        new CardInstanceHasTagExpression<CardsDrawnTriggeredEffectContext>(
                            card, StandardCombatIds.UnplayableTag)))),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, card, mark, sourceSelector: overseer),
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, card, StandardCombatIds.CardCostDeltaCounter,
                    N<CardsDrawnTriggeredEffectContext>(FreeOfCharge), relative: false),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    overseer, WrappedThisTurn, N<CardsDrawnTriggeredEffectContext>(1), relative: true),
            ]));

        var wrap = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                IsPlayersTurn<CardsDrawnTriggeredEffectContext>(), Standing<CardsDrawnTriggeredEffectContext>(overseer)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    overseer, WrappedThisTurn, N<CardsDrawnTriggeredEffectContext>(0), relative: false),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand, wrapOne),
            ]));

        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var unwrap = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, mark),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Applicant, played, mark, remove: true),
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(EntombedId), N<CardPlayedTriggeredEffectContext>(1),
                    sourceSelector: overseer),
            ]));

        return Face(LinenHouseId, "The Linen House",
            "Each turn it wraps 2 cards in your hand in linen. A wrapped card costs 0, but playing it gives you "
            + "1 Entombed.",
            triggers: [On("CardsDrawn", wrap), On("CardPlayed", unwrap)]);
    }
}
