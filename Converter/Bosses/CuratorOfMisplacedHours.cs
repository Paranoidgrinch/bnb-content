using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Bosses;

// ── The Curator of Misplaced Hours (Act II boss, 278 HP) ──────────────────────────────────────────────────
//
// "The past is evidence. The future is already filed. The present is temporary."
//
// Every card you play is an hour the Curator keeps: each one adds 2 damage to its next attack, on its badge where
// you can count them, and the attack spends them all. A long turn now is a heavy blow later.
//
// REWORKED 2026-11-03 (user, Act II trial: "irgendwas mit mehr dps für mehr gespielte Karten? eigentlich eine
// coole Idee aber kompliziert umgesetzt"): the dial of Past, Present and Future, the scheduled hours, Borrow One
// Minute and the citation are gone.
public static class CuratorOfMisplacedHours
{
    public const string EnemyId = "curator_of_misplaced_hours";

    public const string TheCuratorId = "the_curator_of_misplaced_hours";
    public const string KeptHoursId = "kept_hours";

    public const int DamagePerCard = 2;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    public static IEnumerable<StatusData> Statuses() =>
    [
        new StatusData
        {
            Id = TheCuratorId,
            NameKey = "The Curator of Misplaced Hours",
            DescriptionKey = "Each card you play adds 2 damage to its next attack.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
        },
        KeptHours(),
    ];

    // The hours, counted on the Curator: 2 damage each on its next blow, and that blow spends them.
    private static StatusData KeptHours()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new RemoveStatusNode<DamageDealtTriggeredEffectContext>(Self, new StatusDefinitionId(KeptHoursId)));
        return new StatusData
        {
            Id = KeptHoursId,
            NameKey = "Kept Hours",
            DescriptionKey = "Its next attack deals 2 more damage for each.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, DamagePerCard,
                    RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // Every card the player plays is one more hour on every Curator standing.
    public static IReadOnlyList<EncounterTriggerData> Triggers()
    {
        var curators = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(TheCuratorId));
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(curators,
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(KeptHoursId),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1))));
        return [new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()))];
    }

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;
}
