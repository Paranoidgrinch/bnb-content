using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Original enemies → engine EnemyActionData (one per intent, id "<enemy>.<intent>") + encounter
// rosters. Intent cycles map 1:1 (the engine rotates the Actions list by round). The two
// weighted_random enemies fall back to that same cycle — see ADAPTATIONS.md. Attack intents carry
// their telegraphed number in the intent label ("Bite with Reservation (7)"), since the engine's
// intent is a label + kind, not a computed number.
public static class EnemyMapper
{
    public static string ActionId(string enemyId, string intentId) => $"{enemyId}.{intentId}";

    public static IReadOnlyList<EnemyActionData> MapActions(IReadOnlyList<BabEnemy> enemies) =>
        enemies.SelectMany(MapEnemy).ToList();

    private static IEnumerable<EnemyActionData> MapEnemy(BabEnemy enemy)
    {
        if (enemy.IntentPattern is not ("cycle" or "weighted_random"))
            throw new ConversionException($"enemy '{enemy.Id}'", $"unmapped intent_pattern '{enemy.IntentPattern}'");
        foreach (var intent in enemy.Intents)
            yield return MapIntent(enemy, intent);
    }

    private static EnemyActionData MapIntent(BabEnemy enemy, BabIntent intent)
    {
        var where = $"enemy '{enemy.Id}' intent '{intent.Id}'";
        var shapes = new[] { intent.Damage is not null, intent.Effects is not null, intent.Actions is not null };
        if (shapes.Count(s => s) != 1)
            throw new ConversionException(where, "expected exactly one payload shape (damage | effects | actions)");

        var program = intent.Damage is { } damage
            ? new CombatNodeModel("dealDamage", "eventTarget", CombatAmountSpec.FromConst(damage))
            : EffectMapper.MapAll(where, intent.Effects ?? intent.Actions, EffectMapper.EnemyTargets);

        return new EnemyActionData
        {
            Id = ActionId(enemy.Id, intent.Id),
            NameKey = intent.Name,
            Intent = new ActionIntent(Label(intent), Kind(where, intent.IntentType)),
            Program = CombatProgramModel.Build<EnemyActionContext>(program),
        };
    }

    // The telegraph: the intent name plus a plain-language summary of what it DOES, so the player can
    // read the meaning (not just a flavor name). "Bite with Reservation · 7 dmg", "File Complaint ·
    // Panic +1", "Brace · 4 block". Effects come from a bare damage value or the effect/action DSL list.
    private static string Label(BabIntent intent)
    {
        var effects = new List<BabEffect>();
        if (intent.Damage is { } bare)
            effects.Add(new BabEffect("deal_damage", null, bare, null, null, null, null, null, null, null));
        effects.AddRange(intent.Effects ?? intent.Actions ?? []);

        var parts = new List<string>();
        var damage = effects.Where(e => e.Type == "deal_damage").Sum(e => e.Amount ?? 0);
        if (damage > 0)
            parts.Add($"{damage} dmg");
        foreach (var effect in effects)
        {
            switch (effect.Type)
            {
                case "gain_block" when effect.Amount is { } block:
                    parts.Add($"{block} block");
                    break;
                case "apply_status" when effect.Status is { } status:
                    parts.Add($"{Capitalize(status)} +{effect.Amount ?? 1}");
                    break;
                case "gain_strength":
                    parts.Add($"Strength +{effect.Amount ?? 1}");
                    break;
                case "damage_per_status" when effect.Status is { } status:
                    parts.Add($"{effect.AmountPerStack ?? 0}× per {Capitalize(status)}");
                    break;
            }
        }
        return parts.Count > 0 ? $"{intent.Name} · {string.Join(", ", parts)}" : intent.Name;
    }

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static IntentKind Kind(string where, string intentType) => intentType switch
    {
        "attack" => IntentKind.Attack,
        "block" => IntentKind.Defend,
        "buff" => IntentKind.Buff,
        "debuff" => IntentKind.Debuff,
        "mixed" => IntentKind.Special,
        var other => throw new ConversionException(where, $"unmapped intent_type '{other}'"),
    };
}

// Original encounters → engine EncounterDefinition. Duplicate roster entries get "#n" instance ids
// (the fight needs unique combatant ids); their look rides on the DisplayName + presentation entries
// the assembler emits per instance id.
public static class EncounterMapper
{
    public static EncounterDefinition Map(
        BabEncounter encounter, IReadOnlyDictionary<string, BabEnemy> enemies, int startingEnergy)
    {
        var where = $"encounter '{encounter.Id}'";
        var seen = new Dictionary<string, int>();
        var roster = new List<EncounterEnemy>();
        foreach (var enemyId in encounter.Enemies)
        {
            if (!enemies.TryGetValue(enemyId, out var enemy))
                throw new ConversionException(where, $"references unknown enemy '{enemyId}'");
            var count = seen[enemyId] = seen.TryGetValue(enemyId, out var n) ? n + 1 : 1;
            roster.Add(new EncounterEnemy(
                count == 1 ? enemyId : $"{enemyId}#{count}",
                enemy.MaxHp,
                enemy.Intents.Select(i => new EnemyActionDefinitionId(EnemyMapper.ActionId(enemyId, i.Id))).ToList(),
                DisplayName: enemy.Name));
        }
        return new EncounterDefinition(
            new EncounterId(encounter.Id),
            roster,
            [new ResourceSpec(StandardCombatIds.EnergyResource, startingEnergy, startingEnergy)]);
    }
}
