using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// The general pool's Act-III Rites and marks. Four of the six change what Ward Wax, Lien or a status tick
// does, so they are markers those keywords look for — the shape the whole pool uses for a rule that alters
// another rule.
public static class GeneralWax
{
    public const string WaxReliquary = "wax_reliquary";       // read by Ward Wax's decay
    public const string VotiveCovenant = "votive_covenant";   // read by Ward Wax's decay
    public const string ConsecratedTestament = "consecrated_testament";
    public const string MortgagedAegis = "mortgaged_aegis";
    public const string WaxIndemnity = "wax_indemnity";
    public const string DebtOuroboros = "debt_ouroboros";     // read by the Lien resolution
    public const string OathOfRefusal = "oath_of_refusal";

    public static readonly CounterId TestamentPaid = new("consecrated_testament_paid");
    public static readonly CounterId RefusalsRecorded = new("oath_of_refusal_recorded");

    public static IReadOnlyList<StatusData> All() =>
    [
        Marker(WaxReliquary, "Wax Reliquary"),
        Marker(VotiveCovenant, "Votive Covenant"),
        Marker(VotiveCovenant + "+", "Votive Covenant+"),
        Marker(DebtOuroboros, "Debt Ouroboros"),
        Marker(DebtOuroboros + "+", "Debt Ouroboros+"),
        Marker(WaxIndemnity, "Wax Indemnity"),

        Testament(ConsecratedTestament, "Consecrated Testament", 3),
        Testament(ConsecratedTestament + "+", "Consecrated Testament+", 4),
        Aegis(),
        Refusal(OathOfRefusal, "Oath of Refusal"),
        Refusal(OathOfRefusal + "+", "Oath of Refusal+"),
    ];

    // ── Consecrated Testament ─────────────────────────────────────────────────────────────────────────────
    // "The first N times each turn an enemy loses HP because of a Status effect, gain 1 Ward Wax."
    //
    // A status effect's HP loss is the DamageOverTime kind — the one every keyword in this game uses for its
    // tick, and the one no ordinary attack carries. The count is kept on the wearer and reset each turn.
    private static StatusData Testament(string id, string name, int times)
    {
        var wearer = CombatantTargetSelectors.IterationTarget;
        var loser = CombatantTargetSelectors.EventTarget;

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                // An enemy, and a loss that came from a lingering effect rather than a blow.
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new NotExpression<DamageReceivedTriggeredEffectContext>(
                        new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                            loser, new StatusDefinitionId(Keywords.ApplicantMarker))),
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                    new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(wearer, TestamentPaid),
                            ComparisonOperator.Less,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(times)),
                        new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                                wearer, new StatusDefinitionId(Keywords.WardWax),
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                            new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                                wearer, TestamentPaid,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: true),
                        ])))));

        return Rite(id, name, $"The first {times} times each turn a status costs an enemy HP, gain 1 Ward Wax.",
        [
            Trigger(program, nameof(TriggerEvent.DamageTaken), StatusTriggerScope.Anywhere),
            ClearLatch(TestamentPaid),
        ]);
    }

    // ── Mortgaged Aegis ───────────────────────────────────────────────────────────────────────────────────
    // "Gain N Block. At the start of your next turn, gain 8 Lien." The debt is the mark; it falls due once
    // and then goes.
    private static StatusData Aegis() => Rite(MortgagedAegis, "Mortgaged Aegis",
        "At the start of your next turn you take on 8 Lien.",
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Lien),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(8)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(MortgagedAegis)),
                ])), nameof(TriggerEvent.TurnStarted)),
        ]);

    // ── Oath of Refusal ───────────────────────────────────────────────────────────────────────────────────
    // "The first 2 times each turn Censure prevents one or more Status stacks, record 1 Refusal. At the start
    // of your next turn, draw 1 card per recorded Refusal, maximum 2; if at least 1 was recorded, gain 1
    // Energy. Then clear all recorded Refusal."
    private static StatusData Refusal(string id, string name)
    {
        var wearer = CombatantTargetSelectors.IterationTarget;

        var record = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ForEachTargetEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantCounterExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            wearer, RefusalsRecorded),
                        ComparisonOperator.Less,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(2)),
                    new SetCombatantCounterNode<StatusApplicationBlockedTriggeredEffectContext>(
                        wearer, RefusalsRecorded,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1),
                        relative: true))));

        var collect = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, RefusalsRecorded),
                    ComparisonOperator.Greater, new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new DrawCardsNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new MinExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, RefusalsRecorded),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(2))),
                    // Held, not gained: a turn's Energy is refilled before its triggers run, so a point
                    // added here would be clamped away (see HeldEnergy).
                    HeldEnergy.Hold<TurnStartedTriggeredEffectContext>(1),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, RefusalsRecorded,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                ])));

        return Rite(id, name,
            "Refusals are recorded, and paid out at the start of your next turn as cards and Energy.",
        [
            Trigger(record, nameof(TriggerEvent.StatusApplicationPrevented), StatusTriggerScope.Anywhere),
            Trigger(collect, nameof(TriggerEvent.TurnStarted)),
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
