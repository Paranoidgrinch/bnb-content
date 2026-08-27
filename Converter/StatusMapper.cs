using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// The six original statuses, hand-ported: their trigger semantics live in the original's combat code
// (bab/combat/turns.py), not its data files, so each is authored here as engine StatusData and the
// mapper VERIFIES the source data still lists exactly the statuses this table knows — a new or renamed
// status in source-data fails the conversion instead of silently keeping stale behaviour.
//
// Faithfulness notes (from turns.py; deliberate deviations in ADAPTATIONS.md):
// - paperwork: bearer loses HP = stacks each turn, no decay. Ported via the engine's damage_over_time
//   status tag: it ticks at the bearer's TURN START (original: turn end) with DamageKind.DamageOverTime,
//   so doubt's attack penalty can never touch it; the bearer's block was just cleared, so it lands on HP.
// - poison: like paperwork, plus one stack fades at the bearer's turn end (tick full, then decay).
// - doubt: the bearer's attacks (Direct damage) deal 25% less; one stack fades per damaging attack.
// - panic: bearer draws min(stacks, draw) fewer cards at its turn start; one stack fades per turn
//   (authored at the bearer's turn END so the turn-start draw sees the full stack count).
// - fatigue: bearer loses 1 energy at its turn start (after the refill); one stack fades.
// - strength: attacks deal +stacks damage.
public static class StatusMapper
{
    public static IReadOnlyList<StatusData> Map(string where, IReadOnlyList<BabStatus> source)
    {
        var known = new[] { "paperwork", "doubt", "panic", "fatigue", "strength", "poison", "bookworm" };
        var listed = source.Select(s => s.Id).ToHashSet();
        var unknown = listed.Except(known).ToList();
        if (unknown.Count > 0)
            throw new ConversionException(where, $"source lists statuses this port doesn't know: {string.Join(", ", unknown)}");
        var missing = known.Except(listed).ToList();
        if (missing.Count > 0)
            throw new ConversionException(where, $"source no longer lists: {string.Join(", ", missing)}");

        var byId = source.ToDictionary(s => s.Id);
        return
        [
            // paperwork and doubt are NOT built here any more. The final card design gives both a rule this
            // port had approximated (Paperwork ticks at the bearer's turn END; Doubt is spent once per Attack
            // ACTION, not once per hit), so they are authored in Cards/Keywords.cs alongside the rest of the
            // keyword substrate. The source data still lists them, and the completeness check below still
            // insists it does, because the enemies reference them by id.
            Status(byId["panic"], StatusPolarity.Debuff,
                "At the start of its turn, this character draws 1 fewer card per stack. One stack fades each turn.",
                passives: [new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw,
                    PassiveModifierOperation.AddPerStack, -1, RestrictDamageKind: null)],
                triggers: [TurnEnded(ConsumeOneStack("panic"))]),

            Status(byId["fatigue"], StatusPolarity.Debuff,
                "At the start of its turn, this character loses 1 Energy. One stack fades each turn.",
                triggers: [Trigger("TurnStarted", CombatProgramModel.Build<TurnStartedTriggeredEffectContext>(
                    CombatNodeModel.Sequence(
                    [
                        new CombatNodeModel("loseResource", "source", CombatAmountSpec.FromConst(1),
                            StandardCombatIds.EnergyResource.value),
                        ConsumeOneStackModel("fatigue"),
                    ])))]),

            Status(byId["strength"], StatusPolarity.Buff,
                "This character's attacks deal 1 more damage per stack.",
                passives: [new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 1)]),

            Status(byId["poison"], StatusPolarity.Debuff,
                "At the start of its turn, this character loses HP equal to its Poison, ignoring Block. "
                + "Then one stack fades.",
                tags: [StandardCombatIds.DamageOverTimeTag.value],
                triggers: [TurnEnded(ConsumeOneStack("poison"))]),

            Status(byId["bookworm"], StatusPolarity.Buff,
                "Just before this character's Paperwork resolves, that much Paperwork is eaten instead — one "
                + "stack of Bookworm per stack of Paperwork, and both are spent together. What is not eaten stays.",
                triggers: [Trigger("TurnStarted", Bookworm())]),
        ];
    }

    // Bookworm X (the reworked Act-I anti-Paperwork status): immediately before the bearer's Paperwork
    // resolves, remove min(Paperwork, Bookworm) of EACH. It stays on the bearer's TurnStarted trigger now
    // that Paperwork ticks at the bearer's turn END: start and end of the same turn is the cleanest possible
    // reading of "immediately before it resolves", and it needs no ordering agreement between two statuses
    // firing on one event (5 Paperwork + 2 Bookworm → the turn ends with a 3-point tick).
    //
    // min() without a scratch value: the naive sequence fails because the second removal re-reads a value
    // the first one already changed. Branching on which side is smaller keeps every read on the status that
    // has NOT been touched yet, so both removals see their original stack counts.
    private static EffectProgram<TurnStartedTriggeredEffectContext> Bookworm()
    {
        var paperwork = Stacks("paperwork");
        var bookworm = Stacks("bookworm");

        var program = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                paperwork, ComparisonOperator.GreaterOrEqual, bookworm),
            // Paperwork covers every Bookworm stack: spend them all (Bookworm is read before it changes).
            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
            {
                Remove("paperwork", bookworm),
                Remove("bookworm", bookworm),
            }),
            // Less Paperwork than Bookworm: spend only as much Bookworm as there is Paperwork, the rest remains.
            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
            {
                Remove("bookworm", paperwork),
                Remove("paperwork", paperwork),
            }));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(program);
    }

    private static CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext> Stacks(string statusId) =>
        new(CombatantTargetSelectors.Source, new StatusDefinitionId(statusId));

    private static ModifyStatusStacksNode<TurnStartedTriggeredEffectContext> Remove(
        string statusId, ICombatExpression<TurnStartedTriggeredEffectContext, int> amount) =>
        new(CombatantTargetSelectors.Source, new StatusDefinitionId(statusId),
            new SubtractExpression<TurnStartedTriggeredEffectContext>(
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0), amount));

    // `rules` is the description the PLAYER reads on hover, and it is written here rather than copied from
    // the source data because this port deviates: the original's text says "the player's turn" for statuses
    // the engine ticks on whoever is carrying them, and a description that describes another game's rule is
    // worse than none. Every status the player can see owes them one (Tests/StatusDescriptionTests).
    private static StatusData Status(
        BabStatus source, StatusPolarity polarity, string rules,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<PassiveModifierData>? passives = null,
        IReadOnlyList<StatusTriggerData>? triggers = null) => new()
        {
            Id = source.Id,
            NameKey = source.Name,
            DescriptionKey = rules,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = tags ?? [],
            PassiveModifiers = passives ?? [],
            Triggers = triggers ?? [],
        };

    private static CombatNodeModel ConsumeOneStackModel(string statusId) =>
        new("modifyStatusStacks", "source", CombatAmountSpec.FromConst(-1), StatusId: statusId);

    // Status triggers store their program as context-free CombatJson; any context type serializes the
    // same document, so everything builds under the turn-ended context for uniformity.
    private static EffectProgram<TurnEndedTriggeredEffectContext> ConsumeOneStack(string statusId) =>
        CombatProgramModel.Build<TurnEndedTriggeredEffectContext>(ConsumeOneStackModel(statusId));

    private static StatusTriggerData TurnEnded(EffectProgram<TurnEndedTriggeredEffectContext> program) =>
        Trigger("TurnEnded", program);

    // Trigger names are the Studio composer's TriggerEvent member names; a wrong name would author a
    // trigger that never fires, which the end-to-end smoke run would catch.
    private static StatusTriggerData Trigger<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));
}
