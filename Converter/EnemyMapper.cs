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

    // The telegraph: the intent name, plus the summed flat damage when the intent attacks.
    private static string Label(BabIntent intent)
    {
        var damage = (intent.Damage ?? 0)
            + (intent.Effects ?? intent.Actions ?? []).Where(e => e.Type == "deal_damage").Sum(e => e.Amount ?? 0);
        return damage > 0 ? $"{intent.Name} ({damage})" : intent.Name;
    }

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
