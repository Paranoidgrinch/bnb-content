using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// ── A LEARNED WAY TO PLAY ONE FIGHT: A WEIGHTED READING OF THE TABLE ───────────────────────────────────────────
// The trial bot's weak point was what it thought a position was worth: a rollout that lost no health in its few
// turns priced the enemies still standing at a tenth of a point per round, and nothing priced Strength, Authority,
// Open Guard or a Challenge in flight. A PlayPolicy is that reading made explicit and learnable:
//
//   cost(position) = health lost since the decision + Σ wᵢ · featureᵢ(position)            (lower is better)
//
// in health points, read at the end of a turn on a WORLD (Worlds.Sample: the hidden order and the dice redrawn)
// after the enemies have answered it. The features are what a player reads off the table (the list below); the
// weights are a strategy, learned per encounter by an evolution strategy (PolicyTraining) — never a seed's
// knowledge, since every reading happens on a world.
//
// The same reading does three jobs in the bot (TrialBot): with Worlds = 0 it IS the player (each turn, the line
// whose end-of-turn position costs least); with rollouts it ranks the beam, plays the rollout turns, and prices a
// rollout still going at the horizon.
public sealed class PlayPolicy
{
    // The features, in order. "x10" ones are read in tens of points so every weight sits near 1.
    public static readonly string[] Names =
    [
        "enemy_hp_x10",        // health the enemies still have
        "bodies",              // enemies still standing (a kill is worth this much more than its health)
        "threat_x10",          // the damage the living enemies' next intents show
        "threat_body_hp_x10",  // health of the most threatening body (target priority: the dangerous one first)
        "weakest_hp_x10",      // health of the weakest body (finish what can be finished)
        "enemy_block_x10",     // Block the enemies carry into the hero's turn
        "strength",            // hero Strength
        "open_guard",          // hero Open Guard
        "authority",           // hero Authority
        "hero_buffs",          // the hero's other buff stacks
        "hero_debuffs",        // the hero's debuff stacks (Doubt, …)
        "enemy_debuffs",       // debuff stacks on enemies
        "enemy_buffs",         // buff stacks on enemies (their Strength, …)
        "challenges",          // Challenge marks on enemies (lt_mark_*)
        "energy_unspent",      // Energy left when the turn ended
        "cards_kept",          // cards left in hand when the turn ended
        "block_short_x10",     // the intents' damage the turn's Block did not cover
        "block_excess_x10",    // Block beyond the intents' damage
        "danger_x10",          // how far health is below a third of the maximum
    ];

    public static int Count => Names.Length;

    // A hand-set start: an enemy point costs a third of a health point, a body four, the threat half; buffs help.
    public static readonly double[] Defaults =
    [
        3.0, 4.0, 5.0, 1.0, 0.5, 1.0,
        -2.0, -0.5, -1.5, -1.0, 1.5, -1.0, 1.0, -1.0,
        1.0, -0.2, 2.0, 0.3, 2.0,
    ];

    public double[] Weights { get; }

    public string Name { get; init; } = "default";

    public PlayPolicy(double[] weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != Count)
            throw new ArgumentException($"a play policy has {Count} weights, not {weights.Length}", nameof(weights));
        Weights = [.. weights];
    }

    public static PlayPolicy Default { get; } = new(Defaults);

    public const double LossCost = 1000;
    public const double PerRound = 0.1;
    public const double StillStanding = 3;

    // ── the reading ─────────────────────────────────────────────────────────────────────────────────────────

    // `ended` is the position the turn ended in (before the enemies; null when the turn's own end is not at hand),
    // `after` the position the enemies left. Cost from `rootHealth` on.
    public double Cost(InteractiveCombat? ended, InteractiveCombat after, int rootHealth)
    {
        ArgumentNullException.ThrowIfNull(after);
        if (after.Result == CombatResult.Defeat || after.HeroHealth <= 0)
            return LossCost + rootHealth;
        var lost = Math.Max(0, rootHealth - after.HeroHealth);
        if (after.Result == CombatResult.Victory)
            return lost + PerRound * after.Round;
        var f = Features(ended, after);
        var read = 0.0;
        for (var i = 0; i < f.Length; i++)
            read += Weights[i] * f[i];
        // ⚠ A fight still going never reads better than one won: with buffs weighed in, a reading can go below
        // zero, and a policy that may farm Strength forever instead of finishing a body at 1 HP will (it did).
        return lost + PerRound * after.Round + StillStanding + Math.Max(0, read);
    }

    public static double[] Features(InteractiveCombat? ended, InteractiveCombat after)
    {
        ArgumentNullException.ThrowIfNull(after);
        var f = new double[Count];
        var enemies = after.State.Combatants
            .Where(c => c.Id != after.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive).ToList();
        var threats = enemies.Select(e => (Body: e, Damage: IntentDamage(after, e.Id))).ToList();
        f[0] = enemies.Sum(e => e.Health.Current) / 10.0;
        f[1] = enemies.Count;
        f[2] = threats.Sum(t => t.Damage) / 10.0;
        f[3] = threats.Count == 0 ? 0 : threats.OrderByDescending(t => t.Damage).ThenBy(t => t.Body.Id.value, StringComparer.Ordinal)
            .First().Body.Health.Current / 10.0;
        f[4] = enemies.Count == 0 ? 0 : enemies.Min(e => e.Health.Current) / 10.0;
        f[5] = enemies.Sum(e => e.DefensivePools.Values.Sum(p => p.Current)) / 10.0;
        var hero = after.State.GetCombatant(after.HeroId);
        double Has(string id) => hero.Statuses.Where(s => s.DefinitionId.value == id).Sum(s => Math.Max(1, s.Stacks));
        f[6] = Has("strength");
        f[7] = Has("lt_open_guard");
        f[8] = Has("lt_authority");
        f[9] = hero.Statuses.Where(s => s.Polarity == StatusPolarity.Buff && s.DefinitionId.value is not ("strength" or "lt_open_guard" or "lt_authority"))
            .Sum(s => Math.Max(1, s.Stacks));
        f[10] = hero.Statuses.Where(s => s.Polarity == StatusPolarity.Debuff).Sum(s => Math.Max(1, s.Stacks));
        f[11] = enemies.Sum(e => e.Statuses.Where(s => s.Polarity == StatusPolarity.Debuff).Sum(s => Math.Max(1, s.Stacks)));
        f[12] = enemies.Sum(e => e.Statuses.Where(s => s.Polarity == StatusPolarity.Buff).Sum(s => Math.Max(1, s.Stacks)));
        f[13] = enemies.Sum(e => e.Statuses.Count(s => s.DefinitionId.value.StartsWith("lt_mark_", StringComparison.Ordinal)));
        if (ended is not null && !ended.IsOver)
        {
            f[14] = ended.HeroEnergy;
            f[15] = ended.Hand.Count;
            var incoming = ended.State.Combatants
                .Where(c => c.Id != ended.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive)
                .Sum(c => IntentDamage(ended, c.Id));
            f[16] = Math.Max(0, incoming - ended.HeroGuard) / 10.0;
            f[17] = Math.Max(0, ended.HeroGuard - incoming) / 10.0;
        }
        f[18] = Math.Max(0, hero.Health.Max / 3.0 - hero.Health.Current) / 10.0;
        return f;
    }

    // "Lawful Strike · 12 dmg", "6 dmg x2", "Cut · 3 dmg ×3": the damage an intent shows, hits multiplied.
    private static readonly Regex Damage = new(@"(\d+)\s*dmg(?:\s*[x×]\s*(\d+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static int IntentDamage(InteractiveCombat at, CombatantId enemy)
    {
        ArgumentNullException.ThrowIfNull(at);
        if (at.UpcomingIntentFor(enemy)?.Label is not { } label)
            return 0;
        var total = 0;
        foreach (Match m in Damage.Matches(label))
            total += int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)
                * (m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 1);
        return total;
    }

    // ── kept on disk: ~/Desktop/bnb-balance/bots/lt-policy/<encounter>.json ────────────────────────────────

    public sealed class Record
    {
        public string Name { get; set; } = "";
        public int Act { get; set; }
        public SortedDictionary<string, double> Weights { get; set; } = new(StringComparer.Ordinal);
        public bool Done { get; set; }
        public string Config { get; set; } = "";
        public int Generations { get; set; }
        public double? TrainScore { get; set; }
        public double? ValidScore { get; set; }
        public double? ValidDefaultScore { get; set; }
        public double? ValidStartScore { get; set; }
        public double? ValidLost { get; set; }
        public double? ValidWin { get; set; }
        public double? ValidRounds { get; set; }
        public int ValidFights { get; set; }
        public string Updated { get; set; } = "";
    }

    public Record ToRecord(int act) => new()
    {
        Name = Name,
        Act = act,
        Weights = new SortedDictionary<string, double>(Enumerable.Range(0, Count).ToDictionary(i => Names[i], i => Math.Round(Weights[i], 4)), StringComparer.Ordinal),
        Updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
    };

    // A weight the file does not name keeps its default (a feature added later reads old files).
    public static PlayPolicy FromRecord(Record record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var w = Enumerable.Range(0, Count).Select(i => record.Weights.TryGetValue(Names[i], out var v) ? v : Defaults[i]).ToArray();
        return new PlayPolicy(w) { Name = record.Name };
    }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static void Save(string path, Record record)
    {
        ArgumentNullException.ThrowIfNull(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(record, Json));
        File.Move(tmp, path, overwrite: true);
    }

    public static Record? Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<Record>(File.ReadAllText(path)) : null;
}

// ── WHICH POLICY PLAYS WHICH FIGHT: the encounter's own, else the act's shared default, else the built-in one ──
public sealed class PolicyBook
{
    private readonly Dictionary<string, PlayPolicy> _byName = new(StringComparer.Ordinal);

    public string Dir { get; }

    public PolicyBook(string dir)
    {
        Dir = dir ?? throw new ArgumentNullException(nameof(dir));
        if (!Directory.Exists(dir))
            return;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
            if (PlayPolicy.Load(file) is { } record)
                _byName[Path.GetFileNameWithoutExtension(file)] = PlayPolicy.FromRecord(record);
    }

    public static string SharedName(int act) => $"_act{act}";

    public static string PathOf(string dir, string name) => Path.Combine(dir, name + ".json");

    public PlayPolicy For(string encounter, int act) =>
        _byName.GetValueOrDefault(encounter) ?? _byName.GetValueOrDefault(SharedName(act)) ?? PlayPolicy.Default;

    public bool Has(string name) => _byName.ContainsKey(name);

    public int Count => _byName.Count;
}
