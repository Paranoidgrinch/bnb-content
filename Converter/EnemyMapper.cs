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
                // The scaling attacks telegraph their whole formula: base, per-stack bonus and its cap —
                // "7 dmg +3 per Panic (max +9)". Without the base the number the player must plan against
                // is missing from the intent.
                case "damage_per_status" when effect.Status is { } status:
                    var scaling = $"+{effect.AmountPerStack ?? 0} per {Capitalize(status)}";
                    if (effect.Cap is { } cap)
                        scaling += $" (max +{cap})";
                    parts.Add(effect.Amount is { } baseDamage && baseDamage != 0
                        ? $"{baseDamage} dmg {scaling}"
                        : $"dmg {scaling}");
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
                // The round-robin cycle excludes SPECIAL intents; they fire only via intent rules. All intents
                // (special or not) still get action definitions (EnemyMapper.MapActions), so rules can name them.
                enemy.Intents.Where(i => i.Special != true)
                    .Select(i => new EnemyActionDefinitionId(EnemyMapper.ActionId(enemyId, i.Id))).ToList(),
                StartingStatuses: MapStartingStatuses(enemy),
                DisplayName: enemy.Name,
                IntentRules: MapIntentRules(where, enemyId, enemy)));
        }
        // Cross-combatant enemy passives (reactions to player actions) become per-encounter triggered effects,
        // one set per distinct enemy present.
        var triggeredEffects = encounter.Enemies.Distinct()
            .SelectMany(EncounterPassives.ForEnemy)
            .ToList();

        return new EncounterDefinition(
            new EncounterId(encounter.Id),
            roster,
            [new ResourceSpec(StandardCombatIds.EnergyResource, startingEnergy, startingEnergy)],
            triggeredEffects: triggeredEffects);
    }

    // Passive signatures + standing buffs the enemy carries into the fight (a status with triggers).
    private static IReadOnlyList<StartingStatusSpec>? MapStartingStatuses(BabEnemy enemy) =>
        enemy.StartingStatuses is null || enemy.StartingStatuses.Count == 0
            ? null
            : enemy.StartingStatuses
                .Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Status), s.Stacks ?? 1))
                .ToList();

    // State-conditional intents → engine EnemyIntentRule. Action names an intent on this enemy.
    private static IReadOnlyList<EnemyIntentRule>? MapIntentRules(string where, string enemyId, BabEnemy enemy) =>
        enemy.IntentRules is null || enemy.IntentRules.Count == 0
            ? null
            : enemy.IntentRules
                .Select(r => new EnemyIntentRule(
                    MapCondition($"{where} intent rule", r.Condition),
                    new EnemyActionDefinitionId(EnemyMapper.ActionId(enemyId, r.Action)),
                    r.Priority ?? 0))
                .ToList();

    private static EnemyIntentCondition MapCondition(string where, BabIntentCondition c)
    {
        ComparisonOperator Op() => c.Op switch
        {
            "eq" => ComparisonOperator.Equal,
            "ne" => ComparisonOperator.NotEqual,
            "lt" => ComparisonOperator.Less,
            "le" => ComparisonOperator.LessOrEqual,
            "gt" => ComparisonOperator.Greater,
            "ge" => ComparisonOperator.GreaterOrEqual,
            var other => throw new ConversionException(where, $"unmapped comparison op '{other}'"),
        };
        int Val() => c.Value ?? throw new ConversionException(where, $"'{c.Kind}' condition missing value");
        string Str(string? s, string field) => s ?? throw new ConversionException(where, $"'{c.Kind}' missing {field}");
        IReadOnlyList<EnemyIntentCondition> Kids() => (c.Conditions ?? throw new ConversionException(where, $"'{c.Kind}' missing conditions"))
            .Select(k => MapCondition(where, k)).ToList();

        return c.Kind switch
        {
            "health_percent" => new EnemyHealthPercentCondition(Op(), c.Percent ?? Val()),
            "round" => new RoundCondition(Op(), Val()),
            "self_status" => new SelfHasStatusCondition(new StatusDefinitionId(Str(c.Status, "status")), c.MinStacks ?? 1),
            "opponent_status" => new OpponentHasStatusCondition(new StatusDefinitionId(Str(c.Status, "status")), c.MinStacks ?? 1),
            "self_counter" => new SelfHasCounterCondition(new CounterId(Str(c.Counter, "counter")), Op(), Val()),
            "self_resource" => new SelfResourceCondition(new ResourceId(Str(c.Resource, "resource")), Op(), Val()),
            "opponent_cards_played" => new OpponentCardsPlayedCondition(Op(), Val(), c.LastTurn ?? true),
            "all_of" => new AllOfCondition(Kids()),
            "any_of" => new AnyOfCondition(Kids()),
            "not" => new NotCondition(Kids()[0]),
            var other => throw new ConversionException(where, $"unmapped intent condition kind '{other}'"),
        };
    }
}
