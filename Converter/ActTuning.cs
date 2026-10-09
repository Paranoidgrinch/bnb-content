using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        // Measured 2026-10-08 (ActBench over the ten playthroughs' decks, beam 6): the Act-III and Act-IV single
        // bodies died before they struck. The multi-fights are made of the same normal bodies, so they take the
        // damage too. Act IV sits above Act III in every role (user, 2026-10-08) — and against a stronger deck.
        [3] = new Dictionary<string, Factor>
        {
            ["normal"] = new(170, 150),
            ["multi"] = new(),
            ["elite"] = new(150, 160),
            ["boss"] = new(220, 230),
        },
        [4] = new Dictionary<string, Factor>
        {
            ["normal"] = new(230, 210),
            ["multi"] = new(70, 100),
            ["elite"] = new(170, 170),
            ["boss"] = new(160, 160),
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

    // encounter id → factor, over its act's role factor: for the fights a role's factor cannot fit (a boss that already
    // costs 30 next to one that costs 2). Health on that encounter; damage on every body met in it (a body met in more
    // than one overridden encounter takes the first).
    public static readonly Dictionary<string, Factor> Encounters = new(StringComparer.Ordinal)
    {
        // Act III — already in the band as authored (beam 6 reads multi-body fights about twice as dear as the strong
        // planner: three reeds 31 → 13).
        ["green_docket_elite_ant_queen_of_the_proper_line"] = new(110, 110),
        ["green_docket_elite_three_reeds_of_appeal"] = new(),
        // Act IV bosses: from 2.6 (Mother) to 60 (Vizier) as authored — one factor cannot fit them.
        ["labyrinth_boss_vizier_of_the_kings_mouth"] = new(85, 85),
        // The Scribe was 43.5 and lost 3/10 at authored numbers: a sheet of Paperwork per recorded Working grew
        // with the square of a Witch's long fight. With one sheet a tablet (ActFourBossScribe) and 85/80 he is
        // 28.9; the one loss left is w105, the weakest deck (57 HP), which loses him at any setting tried.
        ["labyrinth_boss_first_scribe_of_the_house_of_life"] = new(85, 80),
        ["labyrinth_boss_lady_of_the_black_granaries"] = new(120, 120),
        ["labyrinth_boss_pharaoh_of_the_sealed_name"] = new(130, 130),
        ["labyrinth_boss_queen_of_the_flood_reckoning"] = new(140, 140),
        ["labyrinth_boss_weigher_of_the_unspoken_heart"] = new(130, 130),
        ["labyrinth_boss_mother_of_natron_and_resin"] = new(180, 180),
        ["labyrinth_boss_architect_of_the_impossible_pyramid"] = new(105, 105),
        // Act IV multi-fights that hit at once from every side (over in three turns: health is not the lever). The
        // damage factor reaches every fight their bodies stand in — the court's and the cartouche's single fights
        // and duo with them, which were cheap.
        ["labyrinth_sealed_court_trio_01"] = new(85, 75),
        ["labyrinth_cartouche_duo_01"] = new(70, 110),
        // …and four duos the raised normal damage made too dear, decided by damage inside five turns: their bodies
        // keep the earlier normal factor (150) wherever they stand, single fights included.
        ["labyrinth_linen_duo_01"] = new(70, 150),
        ["labyrinth_vaults_duo_01"] = new(70, 150),
        ["labyrinth_vaults_duo_02"] = new(70, 150),
        ["labyrinth_monument_duo_01"] = new(70, 150),
        // …and the four the last raise (190 → 210) cost a planner fight: they keep 190.
        ["labyrinth_floodmark_duo_01"] = new(70, 190),
        ["labyrinth_balance_duo_01"] = new(70, 190),
        ["labyrinth_granary_duo_01"] = new(70, 190),
        ["labyrinth_yards_02"] = new(230, 190),
        // Act IV elites.
        ["labyrinth_elite_the_tombbreakers_three"] = new(),
        ["labyrinth_elite_treasury_of_the_two_pans"] = new(120, 120),
        ["labyrinth_elite_scarab_host_of_the_sealed_granary"] = new(130, 130),
        ["labyrinth_elite_rope_master_of_the_corvee"] = new(150, 150),
        ["labyrinth_elite_colossus_of_the_endless_procession"] = new(130, 130),
        ["labyrinth_elite_keeper_of_the_thirty_six_decans"] = new(150, 150),
        ["labyrinth_elite_sphinx_of_the_processional_measure"] = new(140, 140),
        ["labyrinth_elite_keeper_of_the_living_cartouche"] = new(210, 200),
        ["labyrinth_elite_mummified_overseer_of_the_linen_house"] = new(210, 200),
        ["labyrinth_elite_surveyor_of_the_errant_cord"] = new(180, 180),
    };

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
        var overridden = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var encounter in data.Encounters)
            if (Encounters.TryGetValue(encounter.Id, out var own))
                foreach (var body in encounter.Enemies)
                    overridden.TryAdd(body, own.DamagePercent);
        int DamagePercent(string body)
        {
            if (overridden.TryGetValue(body, out var forced))
                return forced;
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
        var actions = blueprint.EnemyActions.Select(action =>
        {
            var percent = DamagePercent(action.Id.Split('.')[0]);
            if (percent == 100)
                return action;
            var node = JsonSerializer.SerializeToNode(action, options)!;
            ScaleDamage(node["Program"], percent);
            var scaled = node.Deserialize<EnemyActionData>(options)!;
            return scaled with { Intent = new ActionIntent(ScaleLabel(scaled.Intent.Label, percent), scaled.Intent.Kind) };
        }).ToList();

        return blueprint with { Encounters = encounters, EnemyActions = actions };
    }

    private static int Scale(int value, int percent) =>
        value <= 0 ? value : Math.Max(1, (int)Math.Round(value * percent / 100.0, MidpointRounding.AwayFromZero));

    // Every constant under a dealDamage's Amount; nothing else in the program.
    private static void ScaleDamage(JsonNode? node, int percent)
    {
        switch (node)
        {
            case JsonObject o when Kind(o) == "node.dealDamage":
                ScaleConstants((o["value"] as JsonObject)?["Amount"], percent);
                break;
            case JsonObject o:
                foreach (var (_, child) in o.ToList())
                    ScaleDamage(child, percent);
                break;
            case JsonArray a:
                foreach (var child in a.ToList())
                    ScaleDamage(child, percent);
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

    private static string ScaleLabel(string label, int percent) =>
        Regex.Replace(label, @"(\d+) dmg", m => $"{Scale(int.Parse(m.Groups[1].Value), percent)} dmg");
}
