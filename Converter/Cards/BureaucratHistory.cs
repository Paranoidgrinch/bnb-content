using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// Act III's memory: what each enemy has done, and the Rites and marks that answer it.
//
// The design's Act-III cards ask about the past — "enemies that attacked during the previous enemy turn",
// "each time this enemy has attacked this combat" — and nothing in a fight remembers that on its own. The
// applicant marker keeps the record (see Keywords.ApplicantStatus), because it is the one status present in
// every encounter; these are the marks it writes and the cards that read them.
public static class BureaucratHistory
{
    // Written on an ENEMY by the applicant's bookkeeping.
    public const string AttackedThisRound = "attacked_this_round";
    public const string AttackedLastRound = "attacked_last_round";
    public static readonly CounterId AttacksCounter = new("attacks_this_combat");

    public const string HedgeHospitality = "hedge_hospitality";
    public const string WitnessKnot = "witness_knot";
    public const string GuestbookOath = "guestbook_oath";
    public const string HearthCompact = "hearth_compact";
    public const string HedgeCovenant = "hedge_covenant";
    public const string GuestRight = "guest_right";
    public static readonly CounterId GuestRightUsed = new("guest_right_used");

    public static IReadOnlyList<StatusData> All() =>
    [
        Marker(AttackedThisRound, "Struck This Round"),
        Marker(AttackedLastRound, "Struck Last Round"),

        Hospitality(HedgeHospitality, "Hedge Hospitality", 4),
        Hospitality(HedgeHospitality + "+", "Hedge Hospitality+", 5),
        Knot(WitnessKnot, "Witness Knot", 2),
        Knot(WitnessKnot + "+", "Witness Knot+", 3),
        Oath(GuestbookOath, "Guestbook Oath"),
        Oath(GuestbookOath + "+", "Guestbook Oath+"),

        // These three change what DOUBT does, so the Doubt status looks for them (see Keywords.DoubtStatus).
        Marker(HearthCompact, "Hearth Compact"),
        Marker(HearthCompact + "+", "Hearth Compact+"),
        Marker(HedgeCovenant, "Hedge Covenant"),
        Marker(HedgeCovenant + "+", "Hedge Covenant+"),
        Marker(GuestRight, "Guest Right"),
        Marker(GuestRight + "+", "Guest Right+"),
    ];

    // ── Hedge Hospitality ─────────────────────────────────────────────────────────────────────────────────
    // "Until your next turn, the first enemy that deals unblocked damage to you gains N Paperwork." A mark on
    // the PLAYER, answered by whoever got through, and spent by the answering.
    private static StatusData Hospitality(string id, string name, int paperwork) => Marked(id, name,
        $"The next enemy to get through you takes {paperwork} Paperwork.",
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        // In a damage event "source" is whoever swung — which is exactly who owes the paper.
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Paperwork),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(paperwork)),
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(id)),
                    ]))), nameof(TriggerEvent.DamageTaken)),
            ExpireAtTurnStart(id),
        ]);

    // ── Witness Knot ──────────────────────────────────────────────────────────────────────────────────────
    // "If it attacks before your next turn, apply N Paperwork to all OTHER enemies." A mark on the enemy,
    // answered by its own action. "All other" is its whole side minus itself, which is a spread and a
    // subtraction rather than a selector.
    private static StatusData Knot(string id, string name, int paperwork) => Marked(id, name,
        $"If this character attacks, every other enemy takes {paperwork} Paperwork.",
        [
            Trigger(new EffectProgram<ActionResolvedTriggeredEffectContext>(
                new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                    new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(),
                    new CausalSequenceEffectNode<ActionResolvedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.AllAlliesOfSource, new StatusDefinitionId(Keywords.Paperwork),
                            new ConstantExpression<ActionResolvedTriggeredEffectContext>(paperwork)),
                        new ModifyStatusStacksNode<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Paperwork),
                            new ConstantExpression<ActionResolvedTriggeredEffectContext>(-paperwork)),
                        new RemoveStatusNode<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(id)),
                    ]))), nameof(TriggerEvent.ActionResolved)),
        ]);

    // ── Guestbook Oath ────────────────────────────────────────────────────────────────────────────────────
    // "At the end of your turn, if you have any Block, apply 1 Doubt to every enemy that intends to Attack."
    // Which enemies mean to attack cannot be asked of a selector, so the rule walks them and asks each.
    private static StatusData Oath(string id, string name)
    {
        var enemy = CombatantTargetSelectors.IterationTarget;

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new TargetIntendsExpression<TurnEndedTriggeredEffectContext>(
                            enemy, nameof(IntentKind.Attack)),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            enemy, new StatusDefinitionId(Keywords.Doubt),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1))))));

        return Rite(id, name, "At the end of your turn, if you are guarded, every enemy meaning to attack " +
            "gains 1 Doubt.", [Trigger(program, nameof(TriggerEvent.TurnEnded))]);
    }

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusTriggerData ExpireAtTurnStart(string id) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
            nameof(TriggerEvent.TurnStarted));

    private static StatusData Marker(string id, string name) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = "A standing rule of this fight.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
    };

    private static StatusData Marked(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) =>
        Rite(id, name, description, triggers);

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

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
