using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// False Signature: a discount now, paid for by the card after it.
//
// The design has the player CHOOSE a card in hand and cheapen that one. The engine prices a card by what its
// owner is wearing, not by a mark on one card in hand, so the discount is on the next card played instead —
// which is the same bargain a turn later in the hand. Recorded in ADAPTATIONS.
//
// Three statuses, because a passive modifier's PRESENCE is its condition and cannot be made conditional: the
// discount, the bigger discount its upgrade gives, and the surcharge that follows. The discount hands the
// surcharge over as it is spent. Since the general pool rebuild (user, 2026-11-08) both halves last this turn
// only: a signature played as the second-to-last card is a clean saving.
//
// Charon's Obol borrows the same discount without the surcharge.
public static class GeneralForgery
{
    public const string Discount = "false_signature_discount";
    public const string DiscountPlus = "false_signature_discount+";
    public const string Surcharge = "false_signature_surcharge";
    public const string Coin = "coin_under_the_tongue_discount";

    // The signature itself carries this, so the discount can tell "the card that wrote me" from "the next
    // card" — a card play cannot otherwise be identified from inside a trigger.
    public const string ForgeryTag = "forgery";

    public static IReadOnlyList<StatusData> All() =>
    [
        DiscountStatus(Discount, "False Signature", -1),
        DiscountStatus(DiscountPlus, "False Signature+", -2),
        DiscountStatus(Coin, "Charon's Obol", -1, surcharge: false),
        SurchargeStatus(),
    ];

    private static StatusData DiscountStatus(string id, string name, int magnitude, bool surcharge = true) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = surcharge
            ? $"Your next card this turn costs {-magnitude} less Energy — and the one after it costs 1 more."
            : $"Your next card this turn costs {-magnitude} less Energy.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, magnitude),
        ],
        Triggers =
        [
            // Spent by the card that used it, which is also when the debt falls due — but never by the
            // signature that wrote it, which is played after the discount is already in force.
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(ForgeryTag))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(id)),
                    surcharge
                        ? new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Surcharge),
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1))
                        : new NoOpEffectNode<CardPlayedTriggeredEffectContext>(),
                ]))), nameof(TriggerEvent.CardPlayed)),

            // Unspent by the end of the turn, the signature is simply worthless: "this turn".
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    private static StatusData SurchargeStatus() => new()
    {
        Id = Surcharge,
        NameKey = "Countersigned",
        DescriptionKey = "Your next card this turn costs 1 more Energy.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, 1),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(ForgeryTag))),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Surcharge)))),
                nameof(TriggerEvent.CardPlayed)),

            // "this turn": an unpaid surcharge is forgiven when the turn ends.
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(Surcharge))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
}
