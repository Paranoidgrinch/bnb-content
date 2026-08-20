using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// The general pool's Act-II Rites: three rules about refusals, status decay and Citation.
public static class GeneralPrevention
{
    public const string CountermandedGrace = "countermanded_grace";
    public const string VeinRegister = "vein_register";
    public const string StandingCitation = "standing_citation";

    public static IReadOnlyList<StatusData> All() =>
    [
        Grace(CountermandedGrace, "Countermanded Grace", 2),
        Grace(CountermandedGrace + "+", "Countermanded Grace+", 3),
        Vein(VeinRegister, "Vein Register"),
        Vein(VeinRegister + "+", "Vein Register+"),
        // Standing Citation has no rules of its own: the Citation status looks for it, because only the
        // Citation trigger knows it is about to spend a stack.
        Marker(StandingCitation, "Standing Citation"),
        Marker(StandingCitation + "+", "Standing Citation+"),
    ];

    // ── Countermanded Grace ───────────────────────────────────────────────────────────────────────────────
    // "The first time each turn Censure prevents any Status stack, gain N Ward Wax. This may trigger from
    // Censure on you or on an enemy." Either side of the fight, so the rule watches the whole of it.
    private static StatusData Grace(string id, string name, int wax)
    {
        var latch = new CounterId($"{id}_paid");
        var wearer = CombatantTargetSelectors.IterationTarget;

        var program = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ForEachTargetEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantCounterExpression<StatusApplicationBlockedTriggeredEffectContext>(wearer, latch),
                        ComparisonOperator.Equal,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                            wearer, new StatusDefinitionId(Keywords.WardWax),
                            new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(wax)),
                        new SetCombatantCounterNode<StatusApplicationBlockedTriggeredEffectContext>(
                            wearer, latch,
                            new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1),
                            relative: false),
                    ]))));

        return Rite(id, name, $"The first refusal each turn, wherever it happens, gains you {wax} Ward Wax.",
        [
            Trigger(program, nameof(TriggerEvent.StatusApplicationPrevented), StatusTriggerScope.Anywhere),
            ClearLatch(latch),
        ]);
    }

    // ── Vein Register ─────────────────────────────────────────────────────────────────────────────────────
    // "The first time each turn another Status on an enemy loses a stack, apply 1 Blood Ink to it." Both the
    // stack change and the last stack going (which is an expiry, not a change) count.
    private static StatusData Vein(string id, string name)
    {
        var latch = new CounterId($"{id}_paid");
        var wearer = CombatantTargetSelectors.IterationTarget;

        IEffectNode<TContext> Body<TContext>(ICombatantTargetSelector loser, bool onlyLosses)
            where TContext : class
        {
            ICombatExpression<TContext, bool> gate = new AndExpression<TContext>(
                // Never its own doing, and never the player's own misfortune.
                new NotExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.BloodInk))),
                new NotExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(loser, new StatusDefinitionId(Keywords.ApplicantMarker))));

            if (onlyLosses)
                gate = new AndExpression<TContext>(gate,
                    new ComparisonExpression<TContext>(
                        new EventAmountExpression<TContext>(), ComparisonOperator.Less,
                        new ConstantExpression<TContext>(0)));

            return new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                new ConditionalEffectNode<TContext>(
                    new AndExpression<TContext>(gate,
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(wearer, latch),
                            ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new ApplyStatusNode<TContext>(loser, new StatusDefinitionId(Keywords.BloodInk),
                            new ConstantExpression<TContext>(1)),
                        new SetCombatantCounterNode<TContext>(
                            wearer, latch, new ConstantExpression<TContext>(1), relative: false),
                    ])));
        }

        return Rite(id, name, "The first status an enemy loses each turn is answered with Blood Ink.",
        [
            Trigger(new EffectProgram<StatusStacksChangedTriggeredEffectContext>(
                    Body<StatusStacksChangedTriggeredEffectContext>(CombatantTargetSelectors.Source, onlyLosses: true)),
                nameof(TriggerEvent.StatusStacksChanged), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusExpiredTriggeredEffectContext>(
                    Body<StatusExpiredTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, onlyLosses: false)),
                nameof(TriggerEvent.StatusExpired), StatusTriggerScope.Anywhere),
            ClearLatch(latch),
        ]);
    }

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Marker(string id, string name) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = "A standing rule of this fight.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
    };

    private static StatusData Rite(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers = triggers,
        };

    private static StatusTriggerData ClearLatch(CounterId latch) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, latch,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
            nameof(TriggerEvent.TurnStarted));

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
