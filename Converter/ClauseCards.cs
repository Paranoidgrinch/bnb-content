using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// The Living Petition Chorus asks the player to SIGN or REFUSE a clause each turn. A combat has no generic
// yes/no prompt — but it has cards, so each clause IS a card the Petition puts in the player's hand: playing
// it signs (immediate benefit, and the liability is recorded on the Petition), leaving it there refuses (the
// Petition takes its consolation instead). Either way the card exhausts, so a clause is offered once.
public static class ClauseCards
{
    public sealed record Clause(
        string CardId,
        string Name,
        string Text,
        CounterId Liability,
        Func<IEffectNode<CardPlayContext>> Benefit,
        Func<ICombatantTargetSelector, IEffectNode<CardLifecycleContext>> Refusal);

    public static readonly Clause[] All =
    [
        new("clause_extension", "Extension Clause",
            "Sign: gain 1 Energy. Liability: 1 Fatigue when the record is read.\nRefuse: the Petition gains 8 Block.",
            new CounterId("liability_extension"),
            () => new GainResourceNode<CardPlayContext>(
                CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                new ConstantExpression<CardPlayContext>(1)),
            petition => new GainBlockNode<CardLifecycleContext>(
                petition, new ConstantExpression<CardLifecycleContext>(8))),

        new("clause_protective", "Protective Clause",
            "Sign: gain 10 Block. Liability: 2 Paperwork when the record is read.\nRefuse: the Petition gains 1 Strength.",
            new CounterId("liability_protective"),
            () => new GainBlockNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(10)),
            // The design's "+3 on its next direct attack" needs a one-shot damage buff; a Strength point is the
            // same shape in this vocabulary and is what the Evidentiary refusal already grants (ADAPTATIONS.md).
            petition => new ApplyStatusNode<CardLifecycleContext>(
                petition, new StatusDefinitionId("strength"), new ConstantExpression<CardLifecycleContext>(1))),

        new("clause_evidentiary", "Evidentiary Clause",
            "Sign: draw 2 cards. Liability: 1 Doubt and 1 Paperwork when the record is read.\nRefuse: the Petition gains 1 Strength.",
            new CounterId("liability_evidentiary"),
            () => new DrawCardsNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(2)),
            petition => new ApplyStatusNode<CardLifecycleContext>(
                petition, new StatusDefinitionId("strength"), new ConstantExpression<CardLifecycleContext>(1))),
    ];

    public static IReadOnlyList<CardData> Cards() => All.Select(Card).ToList();

    private static CardData Card(Clause clause)
    {
        var petitionPlay = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.PetitionId));
        var petitionRefuse = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.PetitionId));

        // SIGN: take the benefit, and let the Petition write both the signature and this clause's liability.
        var sign = new EffectProgram<CardPlayContext>(
            new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
            {
                clause.Benefit(),
                new ForEachTargetEffectNode<CardPlayContext>(petitionPlay,
                    new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                    {
                        new SetCombatantCounterNode<CardPlayContext>(
                            CombatantTargetSelectors.IterationTarget, PassiveStatuses.SignaturesCounter,
                            new ConstantExpression<CardPlayContext>(1), relative: true),
                        new SetCombatantCounterNode<CardPlayContext>(
                            CombatantTargetSelectors.IterationTarget, clause.Liability,
                            new ConstantExpression<CardPlayContext>(1), relative: false),
                    })),
            }));

        // REFUSE: still in hand when the turn ends — the Petition takes its consolation and the clause is gone.
        var refuse = new EffectProgram<CardLifecycleContext>(clause.Refusal(petitionRefuse));

        return new CardData
        {
            Id = clause.CardId,
            NameKey = clause.Name,
            DescriptionKey = clause.Text,
            Costs = [],
            Tags = [new TagId("clause"), new TagId("form")],
            Program = sign,
            LifecyclePrograms = new Dictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>>
            {
                [CardLifecycleTrigger.TurnEndInHand] = refuse,
            },
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
