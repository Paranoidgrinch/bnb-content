using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// Ghost Register — the Act-II Rite that gives back what you burned.
//
// "The first non-Junk, non-Temporary persistent card you Archive each turn is recorded. At the start of your
// next turn, add a Temporary copy of it to your hand; it costs 0 and Exhausts when played."
//
// Two things the engine cannot say, both recorded in ADAPTATIONS. It cannot point at the card that was just
// Archived from inside a rule that only hears "the archive count went up", so the copy is taken from the
// Exhaust pile by the player instead — which is the same bargain, and readable. And it cannot make one card
// instance cost 0, so the arrival comes with a free play instead; the player spends it on the copy, or on
// something better, which is their business.
public static class BureaucratArchive
{
    public const string GhostRegister = "ghost_register";

    // Whether anything was archived during the turn that just ended — the "recorded" half of the card.
    private static readonly CounterId Recorded = new("ghost_register_recorded");

    public static IReadOnlyList<StatusData> All() =>
    [
        Register(GhostRegister, "Ghost Register"),
        Register(GhostRegister + "+", "Ghost Register+"),
    ];

    private static StatusData Register(string id, string name)
    {
        // Something was Archived: remember that there is a copy owed.
        IEffectNode<TContext> Note<TContext>() where TContext : class =>
            new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Archived)),
                new SetCombatantCounterNode<TContext>(
                    CombatantTargetSelectors.Source, Recorded,
                    new ConstantExpression<TContext>(1), relative: false));

        // The turn turns over: pay what was recorded, then start listening again.
        var pay = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, Recorded),
                    ComparisonOperator.Greater, new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new CreateCardCopyNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ChosenCardInZoneExpression<TurnStartedTriggeredEffectContext>(
                            CardZone.ExhaustPile, "choose an Archived card to copy"),
                        CardZone.Hand,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    // The copy was meant to be free; the nearest the engine offers is one free play.
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.FreeNextCardStatus,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), charges: 1),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, Recorded,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                ])));

        return new StatusData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = "What you Archive comes back, once, at the start of your next turn.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers =
            [
                Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                    Note<StatusAppliedTriggeredEffectContext>()), nameof(TriggerEvent.StatusApplied)),
                Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                    Note<StatusMergedTriggeredEffectContext>()), nameof(TriggerEvent.StatusMerged)),
                Trigger(pay, nameof(TriggerEvent.TurnStarted)),
            ],
        };
    }

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
}
