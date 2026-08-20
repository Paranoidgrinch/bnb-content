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
// surcharge over as it is spent, so the debt cannot be dodged by simply not playing anything else that turn —
// it lasts the whole combat, as the card says.
public static class GeneralForgery
{
    public const string Discount = "false_signature_discount";
    public const string DiscountPlus = "false_signature_discount+";
    public const string Surcharge = "false_signature_surcharge";

    // The signature itself carries this, so the discount can tell "the card that wrote me" from "the next
    // card" — a card play cannot otherwise be identified from inside a trigger.
    public const string ForgeryTag = "forgery";

    public static IReadOnlyList<StatusData> All() =>
    [
        DiscountStatus(Discount, "False Signature", -1),
        DiscountStatus(DiscountPlus, "False Signature+", -2),
        SurchargeStatus(),
    ];

    private static StatusData DiscountStatus(string id, string name, int magnitude) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = $"Your next card costs {-magnitude} less Energy — and the one after it costs 1 more.",
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
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Surcharge),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
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
        DescriptionKey = "Your next card costs 1 more Energy.",
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
        ],
    };

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
}
