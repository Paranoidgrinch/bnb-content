using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Energy promised at a moment when there is no room for it.
//
// A combatant's Energy pool has a HARD ceiling: the engine clamps every gain to the pool's own max, and
// setting it higher throws — "the pool's OWN max is always the hard ceiling" (ResourceEffects). In this game
// that max is the 3 a turn refills to, and the refill happens BEFORE a turn's triggers run. So every promise
// of the form "at the start of your turn / of this combat, gain 1 Energy" lands on a full pool and does
// nothing whatsoever — silently, because nothing in the engine complains about a clamped gain.
//
// The answer is not to gain it but to HOLD it: the point waits as a status and arrives the moment its holder
// runs dry, which is the same one extra card the design's numbers are for, one moment later.
public static class HeldEnergy
{
    public const string Id = "held_energy";

    public static readonly StatusData Status = new()
    {
        Id = Id,
        NameKey = "Held Energy",
        DescriptionKey = "Energy kept in reserve. It arrives the moment you run out.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                        ComparisonOperator.Equal, new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new GainResourceNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                            new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(Id))),
                        new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Id)),
                    ]))),
                nameof(TriggerEvent.CardPlayed)),
        ],
    };

    // Hold `amount` Energy for the source.
    public static IEffectNode<TContext> Hold<TContext>(int amount) where TContext : class =>
        Hold<TContext>(new ConstantExpression<TContext>(amount));

    public static IEffectNode<TContext> Hold<TContext>(ICombatExpression<TContext, int> amount)
        where TContext : class =>
        new ApplyStatusNode<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(Id), amount);

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Bearer);
}
