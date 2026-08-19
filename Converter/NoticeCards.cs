using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// The Final Notice Knight's "Acknowledge Service" choice. A combat has no yes/no prompt, so the offer is a
// card the Knight puts in the player's hand on their response turn — the same device the Petition's clauses
// use. Playing it signs for the notice (2 Paperwork now, a far lighter enforcement later); leaving it in hand
// refuses (nothing now, the full physical enforcement later). Either way the card is gone afterwards, so the
// offer stands for exactly one turn.
public static class NoticeCards
{
    public static CardData Acknowledge() => new()
    {
        Id = PassiveStatuses.AcknowledgeCardId,
        NameKey = "Acknowledge Service",
        DescriptionKey =
            "Sign for the notice: gain 2 Paperwork. The Knight's enforcement deals 10 instead of 19 and 1 Paperwork.\n"
            + "Leave it in hand to refuse.",
        Costs = [],
        Tags = [new TagId("form"), new TagId("notice")],
        Program = new EffectProgram<CardPlayContext>(
            new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
            {
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId("paperwork"),
                    new ConstantExpression<CardPlayContext>(2)),
                // The acknowledgement is the player's own state: the Knight's intent rules read it as an
                // opponent status when it decides which enforcement to hand down.
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ServiceAcknowledgedId),
                    new ConstantExpression<CardPlayContext>(1)),
            })),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };
}
