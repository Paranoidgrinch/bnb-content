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
        var known = new[] { "paperwork", "doubt", "panic", "fatigue", "strength", "poison" };
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
            Status(byId["paperwork"], StatusPolarity.Debuff,
                tags: [StandardCombatIds.DamageOverTimeTag.value]),

            Status(byId["doubt"], StatusPolarity.Debuff,
                passives: [new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.ScalePercent, 75)],
                triggers: [Trigger("DamageDealt", ConsumeOneStack("doubt"))]),

            Status(byId["panic"], StatusPolarity.Debuff,
                passives: [new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw,
                    PassiveModifierOperation.AddPerStack, -1, RestrictDamageKind: null)],
                triggers: [TurnEnded(ConsumeOneStack("panic"))]),

            Status(byId["fatigue"], StatusPolarity.Debuff,
                triggers: [Trigger("TurnStarted", CombatProgramModel.Build<TurnStartedTriggeredEffectContext>(
                    CombatNodeModel.Sequence(
                    [
                        new CombatNodeModel("loseResource", "source", CombatAmountSpec.FromConst(1),
                            StandardCombatIds.EnergyResource.value),
                        ConsumeOneStackModel("fatigue"),
                    ])))]),

            Status(byId["strength"], StatusPolarity.Buff,
                passives: [new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 1)]),

            Status(byId["poison"], StatusPolarity.Debuff,
                tags: [StandardCombatIds.DamageOverTimeTag.value],
                triggers: [TurnEnded(ConsumeOneStack("poison"))]),
        ];
    }

    private static StatusData Status(
        BabStatus source, StatusPolarity polarity,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<PassiveModifierData>? passives = null,
        IReadOnlyList<StatusTriggerData>? triggers = null) => new()
        {
            Id = source.Id,
            NameKey = source.Name,
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
