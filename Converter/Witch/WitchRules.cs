using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The marks and Rites the Hedge Witch's cards leave on the table (hedge_witch_master.md §11–§13): a rule that
// outlives the card that played it lives on a status, as the Bureaucrat's and the general pool's do. Each
// status's stacks ARE its amount, so a second copy — or the upgrade — adds to the first rather than needing a
// status of its own.
public static partial class WitchRules
{
    // Snail Shell: "if you take no unblocked Attack damage during the enemy turn, gain Ward Wax".
    public const string SnailShell = "snail_shell";

    // Thorn Hedge: "the first time you are attacked this enemy turn, damage the attacker".
    public const string ThornHedge = "thorn_hedge";

    public static IReadOnlyList<StatusData> All() => [SnailShellMark(), ThornHedgeMark(), .. UncommonRules(), .. RareRules(), .. RelicRules()];

    private static ICombatantTargetSelector Self => CombatantTargetSelectors.Source;

    // Settled when the round closes — the first moment after the enemy turn — off the round's own count of
    // what got through (Keywords' struck-this-round, as Sealed Mantle reads it), and spent either way.
    private static StatusData SnailShellMark()
    {
        var wearer = CombatantTargetSelectors.IterationTarget;
        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(SnailShell)),
                new CausalSequenceEffectNode<RoundEndedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<RoundEndedTriggeredEffectContext>(
                        new ComparisonExpression<RoundEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<RoundEndedTriggeredEffectContext>(
                                wearer, Cards.Keywords.StruckThisRoundCounter),
                            ComparisonOperator.Equal, new ConstantExpression<RoundEndedTriggeredEffectContext>(0)),
                        new ApplyStatusNode<RoundEndedTriggeredEffectContext>(
                            wearer, new StatusDefinitionId(Cards.Keywords.WardWax),
                            new CombatantStatusStacksExpression<RoundEndedTriggeredEffectContext>(
                                wearer, new StatusDefinitionId(SnailShell)))),
                    new RemoveStatusNode<RoundEndedTriggeredEffectContext>(wearer, new StatusDefinitionId(SnailShell)),
                ])));

        return Mark(SnailShell, "Snail Shell",
            "If no Attack gets through this enemy turn, gain this much Ward Wax.",
            [Trigger(program, nameof(TriggerEvent.RoundEnded), StatusTriggerScope.Anywhere)]);
    }

    // An enemy action that struck announces itself when it closes, whether or not Block soaked it — that is
    // "attacked". The wearer answers the first one and the hedge is spent; her next turn clears what is left.
    private static StatusData ThornHedgeMark()
    {
        var wearer = CombatantTargetSelectors.IterationTarget;
        var answer = new EffectProgram<ActionResolvedTriggeredEffectContext>(
            new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                new AndExpression<ActionResolvedTriggeredEffectContext>(
                    new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(),
                    new NotExpression<ActionResolvedTriggeredEffectContext>(
                        new TargetHasStatusExpression<ActionResolvedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(Cards.Keywords.ApplicantMarker)))),
                new ForEachTargetEffectNode<ActionResolvedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(ThornHedge)),
                    new CausalSequenceEffectNode<ActionResolvedTriggeredEffectContext>(
                    [
                        new DealDamageNode<ActionResolvedTriggeredEffectContext>(
                            Self,
                            new CombatantStatusStacksExpression<ActionResolvedTriggeredEffectContext>(
                                wearer, new StatusDefinitionId(ThornHedge))),
                        new RemoveStatusNode<ActionResolvedTriggeredEffectContext>(
                            wearer, new StatusDefinitionId(ThornHedge)),
                    ]))));

        var wither = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Self, new StatusDefinitionId(ThornHedge)));

        return Mark(ThornHedge, "Thorn Hedge",
            "The first enemy to attack you this turn takes this much damage.",
            [
                Trigger(answer, nameof(TriggerEvent.ActionResolved), StatusTriggerScope.Anywhere),
                Trigger(wither, nameof(TriggerEvent.TurnStarted)),
            ]);
    }

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Mark(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            Triggers = triggers,
        };

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            TheOpeningHand.For(program, trigger), CombatJson.CreateOptions<TContext>()), scope);
}
