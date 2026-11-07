using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ── THE ACT TUNING TABLE: ONE PLACE FOR HOW HARD AN ACT HITS ─────────────────────────────────────────────────
// (user, 2026-10-08) The Act-III/IV bench (ActBench) measured the later acts far under the agreed cost curve —
// health lost per visit normal ~4 < multi ~10 < elite ~15 < boss ~20 — and an enemy's numbers live in TWO places:
// the source JSON (from which the telegraph is written) and the hand-written programs of Acts III and IV. A factor
// applied to the BUILT document reaches both, and is one line to change and one line to read back.
//
//   health   per ENCOUNTER role, on every body of that encounter's roster (an encounter fields its own health)
//   damage   per BODY role, on every constant inside the Amount of every dealDamage the body's actions hold —
//            base, per-stack bonus and cap alike, so a scaling attack keeps its shape. A body is "boss"/"elite"
//            by the strongest room it is met in (EnemyRole); a standard body met ONLY in multi-fights is "multi",
//            otherwise "normal" (an action is shared by every room the body stands in, so it has one factor).
//
// The telegraph's "N dmg" is scaled alongside (the screen prints the engine's own forecast and falls back to it).
// Health lost by rules (a status that costs HP, Restitution's 2 a point) is NOT touched: those are tuned by hand.
// 100 everywhere is the document as authored.
public static class ActTuning
{
    public sealed record Factor(int HealthPercent = 100, int DamagePercent = 100);

    // act → role (normal | multi | elite | boss) → factor. Missing = 100/100.
    public static readonly Dictionary<int, Dictionary<string, Factor>> Table = new()
    {
        // Act I, from the first hand-played encounter trial (user, 2026-11-01): "the normal enemies could take 10 %
        // more health, otherwise that fits".
        [1] = new Dictionary<string, Factor>
        {
            ["normal"] = new(110, 100),
        },
        // Act II, from the second encounter trial (user, 2026-11-03): the normal enemies were all a little too weak —
        // most single fights cost nothing and were over in two rounds.
        [2] = new Dictionary<string, Factor>
        {
            ["normal"] = new(140, 120),
            ["multi"] = new(130, 120),
        },
        // Measured 2026-10-08 (ActBench over the ten playthroughs' decks, beam 6): the Act-III and Act-IV single
        // bodies died before they struck. The multi-fights are made of the same normal bodies, so they take the
        // damage too. Act IV sits above Act III in every role (user, 2026-10-08) — and against a stronger deck.
        // Raised after the hand-played Act III trial of 2026-11-07 (user: the normal fights "can take more health and
        // damage anyway"): normals cost 0.6 (target 5) and multi-fights 1.6 (target 10). Was normal 170/150, multi 100/100.
        [3] = new Dictionary<string, Factor>
        {
            ["normal"] = new(195, 195),
            ["multi"] = new(115, 130),
            ["elite"] = new(150, 160),
            ["boss"] = new(220, 230),
        },
        // RESCALED in the content cut of 2026-10-30 to about Act III's level (user): a normal body was ~400 HP
        // and a boss ~1000; now a normal body is ~150-200 and a boss ~750-850.
        [4] = new Dictionary<string, Factor>
        {
            ["normal"] = new(125, 130),
            ["multi"] = new(70, 100),
            ["elite"] = new(115, 120),
            ["boss"] = new(130, 125),
        },
    };

    // `--tuning "4:normal=120/150,3:boss=150/130"` — health%/damage% laid over the table for one build, so a bench
    // can try a setting without an edit. What it settles on is written into the table above.
    public static void Override(string spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Regex.Match(part, @"^enc:([a-z0-9_]+)=(\d+)/(\d+)$") is { Success: true } one)
            {
                Encounters[one.Groups[1].Value] = new Factor(int.Parse(one.Groups[2].Value), int.Parse(one.Groups[3].Value));
                continue;
            }
            var m = Regex.Match(part, @"^(\d+):(normal|multi|elite|boss)=(\d+)/(\d+)$");
            if (!m.Success)
                throw new ArgumentException($"--tuning: '{part}' is not act:role=health/damage");
            var act = int.Parse(m.Groups[1].Value);
            if (!Table.TryGetValue(act, out var roles))
                Table[act] = roles = [];
            roles[m.Groups[2].Value] = new Factor(int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value));
        }
    }

    // encounter id → factor, in place of its act's role factor: for the fights a role's factor cannot fit (a boss that
    // already costs 30 next to one that costs 2). Health and damage both on that encounter alone — a body it shares
    // with other rooms fights here with its own copies of its actions.
    public static readonly Dictionary<string, Factor> Encounters = new(StringComparer.Ordinal)
    {
        // Act III — already in the band as authored (beam 6 reads multi-body fights about twice as dear as the strong
        // planner).
        ["green_docket_elite_ant_queen_of_the_proper_line"] = new(110, 110),
        // Act IV's per-encounter overrides went with the rescale of 2026-10-30: the role table alone sets it.
        // Act II: the Blank Death Certificate already cost 29 in the trial of 2026-11-02, so the act's buff passes it by.
        ["archives_necrology_01"] = new(100, 100),
        // Act I, from the Lieutenant trial bot (user, 2026-11-07: its numbers are the yardstick now; it plays a little
        // better than the hand-played trial). Measured 3 paths × 10 seeds per fight, bot HP lost before → after.
        // The bosses that cost nothing: 01 5 → 20 (still loses 7 %, long fights), 02 3 → 17, 05 6 → 19.
        ["city_boss_01"] = new(100, 145),
        ["city_boss_02"] = new(130, 220),
        ["city_boss_05"] = new(140, 200),
        // The Delay normals cost under 1; the act's normal factor is 110/100, so health stays at 110. → 2-3.
        ["city_normal_delay_04"] = new(110, 130),
        ["city_normal_delay_05"] = new(110, 130),
        ["city_normal_delay_07"] = new(110, 170),
        // The two elites that cost 33-36 (target 10) → 12-13, and the two dear multi-fights 15-18 (target 6) → 6-7.
        ["city_elite_appeal_01"] = new(90, 55),
        ["city_elite_delay_01"] = new(85, 45),
        ["city_normal_ordinance_08"] = new(85, 70),
        ["city_normal_seal_08"] = new(80, 65),
    };

    // ── A QUESTION, NOT A TUNING: `--apprentice-damage N`, `--apprentice-block N` ──────────────────────────────
    // Every damage (Block) constant on the Sorcerer's Apprentice's own cards — what they deal (give) when played and
    // what their wand Speaks deal (give) — scaled to N percent for one build, so a bench can ask how much he is short
    // of the others (APPRENTICE_PLAN.md §3). 100 is the document as authored; nothing ships with anything else.
    public static int ApprenticeDamagePercent { get; set; } = 100;
    public static int ApprenticeBlockPercent { get; set; } = 100;

    public static RunBlueprint ApplyApprenticeDamage(RunBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        if (ApprenticeDamagePercent == 100 && ApprenticeBlockPercent == 100)
            return blueprint;
        var options = RunJson.CreateOptions();
        var cards = blueprint.Cards.Select(card =>
        {
            if (!card.Id.StartsWith("ap_", StringComparison.Ordinal))
                return card;
            var node = JsonSerializer.SerializeToNode(card, options)!;
            ScaleDamage(node["Program"], ApprenticeDamagePercent);
            ScaleDamage(node["Program"], ApprenticeBlockPercent, "node.gainBlock");
            if (node["Wand"] is JsonObject wand)
                foreach (var list in new[] { wand["Atoms"], wand["Fallback"] })
                    foreach (var atom in (list as JsonArray ?? []).OfType<JsonObject>())
                        switch (atom["Kind"] is JsonValue kind ? kind.GetValue<string>() : null)
                        {
                            case "Damage": ScaleConstants(atom["Amount"], ApprenticeDamagePercent); break;
                            case "Block": ScaleConstants(atom["Amount"], ApprenticeBlockPercent); break;
                        }
            return node.Deserialize(card.GetType(), options) as CardData ?? card;
        }).ToList();
        return blueprint with { Cards = cards };
    }

    public static Factor For(int act, string role) =>
        Table.TryGetValue(act, out var roles) && roles.TryGetValue(role, out var factor) ? factor : new Factor();

    // An encounter's role, as the bench reads it: boss / elite by its difficulty, else multi when it fields more
    // than one body, else normal.
    public static string RoleOf(BabEncounter encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        return encounter.Difficulty.ToLowerInvariant() switch
        {
            "boss" => "boss",
            "elite" => "elite",
            "mimic" => "mimic",
            _ => encounter.Enemies.Count > 1 ? "multi" : "normal",
        };
    }

    public static RunBlueprint Apply(RunBlueprint blueprint, BabData data)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(data);
        if (Table.Values.SelectMany(r => r.Values).Concat(Encounters.Values).All(f => f is { HealthPercent: 100, DamagePercent: 100 }))
            return blueprint;

        var byId = data.Encounters.ToDictionary(e => e.Id, StringComparer.Ordinal);

        // Health: per encounter.
        var encounters = blueprint.Encounters.Select(encounter =>
        {
            if (!byId.TryGetValue(encounter.Id.Value, out var source)
                || (Encounters.TryGetValue(source.Id, out var own) ? own : For(source.Act, RoleOf(source))).HealthPercent is var percent
                    && percent == 100)
                return encounter;
            return new EncounterDefinition(
                encounter.Id,
                [.. encounter.Enemies.Select(e => e with { MaxHealth = Scale(e.MaxHealth, percent) })],
                encounter.HeroResources, encounter.HeroStartingStatuses, encounter.HeroDisplayName,
                encounter.CardsDrawnPerTurn, encounter.TriggeredEffects);
        }).ToList();

        // Damage: per body. A body belongs to the act of the rooms it is met in (the first, should it cross acts).
        var roles = EnemyRole.Of(data);
        var bodyAct = new Dictionary<string, int>(StringComparer.Ordinal);
        var onlyMulti = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var encounter in data.Encounters)
            foreach (var body in encounter.Enemies)
            {
                bodyAct.TryAdd(body, encounter.Act);
                onlyMulti[body] = onlyMulti.GetValueOrDefault(body, true) && encounter.Enemies.Count > 1;
            }
        int DamagePercent(string body)
        {
            if (!bodyAct.TryGetValue(body, out var act))
                return 100;
            var role = roles.GetValueOrDefault(body, EnemyRole.Standard) switch
            {
                EnemyRole.Boss => "boss",
                EnemyRole.Elite => "elite",
                EnemyRole.Mimic => "mimic",
                _ => onlyMulti.GetValueOrDefault(body) ? "multi" : "normal",
            };
            return For(act, role).DamagePercent;
        }

        var options = RunJson.CreateOptions();
        EnemyActionData Scaled(EnemyActionData action, int percent)
        {
            if (percent == 100)
                return action;
            var node = JsonSerializer.SerializeToNode(action, options)!;
            ScaleDamage(node["Program"], percent);
            var scaled = node.Deserialize<EnemyActionData>(options)!;
            return scaled with { Intent = new ActionIntent(ScaleLabel(scaled.Intent.Label, percent), scaled.Intent.Kind) };
        }
        var authored = blueprint.EnemyActions.ToDictionary(a => a.Id, StringComparer.Ordinal);
        var actions = blueprint.EnemyActions.Select(action => Scaled(action, DamagePercent(action.Id.Split('.')[0]))).ToList();

        // An encounter's own damage reaches only the bodies met in THAT encounter: where it differs from the body's
        // shared factor, the encounter's roster points at copies of the body's actions ("<action>@<encounter>"),
        // scaled from the authored action. Names and descriptions stay the authored action's.
        encounters = encounters.Select(encounter =>
        {
            if (!Encounters.TryGetValue(encounter.Id.Value, out var own))
                return encounter;
            var changed = false;
            var enemies = encounter.Enemies.Select(enemy =>
            {
                if (own.DamagePercent == DamagePercent(enemy.Id))
                    return enemy;
                changed = true;
                EnemyActionDefinitionId Copy(EnemyActionDefinitionId id)
                {
                    var copyId = $"{id.value}@{encounter.Id.Value}";
                    if (authored.TryGetValue(id.value, out var source) && authored.TryAdd(copyId, source))
                        actions.Add(Scaled(source, own.DamagePercent) with
                        {
                            Id = copyId,
                            NameKey = source.NameKey ?? $"enemy-action.{source.Id}.name",
                            DescriptionKey = source.DescriptionKey ?? $"enemy-action.{source.Id}.desc",
                        });
                    return authored.ContainsKey(copyId) ? new EnemyActionDefinitionId(copyId) : id;
                }
                return enemy with
                {
                    Actions = [.. enemy.Actions.Select(Copy)],
                    IntentRules = enemy.IntentRules is { } rules ? [.. rules.Select(r => r with { Action = Copy(r.Action) })] : null,
                };
            }).ToList();
            return changed
                ? new EncounterDefinition(encounter.Id, enemies, encounter.HeroResources, encounter.HeroStartingStatuses,
                    encounter.HeroDisplayName, encounter.CardsDrawnPerTurn, encounter.TriggeredEffects)
                : encounter;
        }).ToList();

        return blueprint with { Encounters = encounters, EnemyActions = actions };
    }

    private static int Scale(int value, int percent) =>
        value <= 0 ? value : Math.Max(1, (int)Math.Round(value * percent / 100.0, MidpointRounding.AwayFromZero));

    // Every constant under a dealDamage's (or the named node's) Amount; nothing else in the program.
    private static void ScaleDamage(JsonNode? node, int percent, string nodeKind = "node.dealDamage")
    {
        if (percent == 100)
            return;
        switch (node)
        {
            case JsonObject o when Kind(o) == nodeKind:
                ScaleConstants((o["value"] as JsonObject)?["Amount"], percent);
                break;
            case JsonObject o:
                foreach (var (_, child) in o.ToList())
                    ScaleDamage(child, percent, nodeKind);
                break;
            case JsonArray a:
                foreach (var child in a.ToList())
                    ScaleDamage(child, percent, nodeKind);
                break;
        }
    }

    private static void ScaleConstants(JsonNode? node, int percent)
    {
        if (node is JsonArray array)
        {
            foreach (var child in array.ToList())
                ScaleConstants(child, percent);
            return;
        }
        if (node is not JsonObject o)
            return;
        var value = (o["value"] as JsonObject)?["Value"] as JsonValue;
        if (Kind(o) == "const" && value is not null && value.GetValueKind() == JsonValueKind.Number)
        {
            ((JsonObject)o["value"]!)["Value"] = Scale(value.GetValue<int>(), percent);
            return;
        }
        foreach (var (_, child) in o.ToList())
            ScaleConstants(child, percent);
    }

    private static string? Kind(JsonObject o) =>
        o["kind"] is JsonValue kind && kind.GetValueKind() == JsonValueKind.String ? kind.GetValue<string>() : null;

    // The per-stack growth and its cap sit inside the same damage amount as the base, so they are scaled with it,
    // and the telegraph says so ("20 dmg +4 per Burdened", not the authored +3).
    private static string ScaleLabel(string label, int percent)
    {
        var scaled = Regex.Replace(label, @"(\d+) dmg", m => $"{Scale(int.Parse(m.Groups[1].Value), percent)} dmg");
        scaled = Regex.Replace(scaled, @"dmg \+(\d+) per", m => $"dmg +{Scale(int.Parse(m.Groups[1].Value), percent)} per");
        return Regex.Replace(scaled, @"\(max \+(\d+)\)", m => $"(max +{Scale(int.Parse(m.Groups[1].Value), percent)})");
    }
}
