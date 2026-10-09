using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RogueDeck.Bot;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Converter.Playtest;

// ── THE ACT BENCH: EVERY FIGHT OF AN ACT, AGAINST THE DECKS REAL RUNS HAD THERE ──────────────────────────────
// The tuning of Acts III and IV (user, 2026-10-08): the clairvoyant planner's ten playthroughs said the later acts
// cost almost nothing, elites less than multi-fights. To tune that, each encounter of the act's pools is fought
// against the hero a run actually brought there — deck (upgrades as "<id>+"), relics, max HP, character — taken
// from the saves by `roguedeck-bot --play <save> --snapshots`:
//
//   entry snapshot   the state at the act's first fight → every normal, multi and elite encounter of the act
//   boss snapshot    the state at the act's boss → every boss encounter of the act
//
// Full health every fight (each act opens with a full heal; a mid-act fight is read on its own).
//
//   the store    every fight is one JSONL line in ~/Desktop/bnb-balance/store/act-bench.jsonl, WRITTEN AT ONCE.
//                Its key holds a hash of the encounter (its enemies' actions and every status they name) and of
//                the deck's cards, so a tuned enemy re-fights only its own encounters and a re-run computes nothing.
//   the target   (agreed 2026-10-08) health lost per visit rising normal ~4 < multi ~10 < elite ~15 < boss ~20.
//
// ⚠ The planner sees the draw pile: every number is a FLOOR on what the fight costs.
public static class ActBench
{
    public sealed record Options(
        string SnapshotsFile, IReadOnlyList<int> Acts, int Shuffles = 1, int Horizon = 5, int Beam = 6, int PerTurn = 100,
        int Jobs = 0, IReadOnlyList<string>? Only = null, bool Yes = false, string? Store = null, IReadOnlyList<string>? Runs = null);

    public sealed record Snapshot(string Run, string? Character, int Act, string Point, int MaxHealth,
        List<string> Deck, List<string> Relics);

    public sealed record Row(string Key, string Run, string? Character, int Act, string Role, string Encounter, int Shuffle,
        bool Won, int Lost, int Turns, double Seconds);

    private static readonly JsonSerializerOptions Line = new();

    public static int Run(RunBlueprint game, Options options, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        var snapshots = JsonSerializer.Deserialize<List<Snapshot>>(File.ReadAllText(options.SnapshotsFile))!
            .Where(s => options.Acts.Contains(s.Act))
            .Where(s => options.Runs is not { Count: > 0 } runs || runs.Contains(s.Run))
            .ToList();
        var cards = game.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var missing = snapshots.SelectMany(s => s.Deck.Where(id => !cards.ContainsKey(id)).Select(id => $"{s.Run} act {s.Act}: card {id}"))
            .Distinct().ToList();
        if (missing.Count > 0)
        {
            say("unknown cards (fix before measuring):\n  " + string.Join("\n  ", missing));
            return 1;
        }

        var store = options.Store ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "bnb-balance", "store", "act-bench.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(store)!);
        var known = Read(store).ToDictionary(r => r.Key, StringComparer.Ordinal);

        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        string EncounterHash(EncounterDefinition e) =>
            hashes.TryGetValue(e.Id.Value, out var h) ? h : hashes[e.Id.Value] = Hash(Describe(game, e));
        string DeckHash(Snapshot s) => Hash(string.Join("|", s.Deck.Order(StringComparer.Ordinal)
            .Select(id => JsonSerializer.Serialize(cards[id].Program, RunJson.CreateOptions()))) + "|" + string.Join(",", s.Relics));

        var all = new List<(Snapshot Snap, string Role, EncounterDefinition Encounter, int Shuffle, string Key)>();
        foreach (var snap in snapshots)
        {
            var deckHash = DeckHash(snap);
            foreach (var (role, encounter) in Pool(game, snap.Act, snap.Point))
            {
                if (options.Only is { Count: > 0 } only && !only.Contains(encounter.Id.Value))
                    continue;
                for (var shuffle = 1; shuffle <= options.Shuffles; shuffle++)
                    all.Add((snap, role, encounter, shuffle,
                        $"{snap.Run}|{snap.Act}|{snap.Point}|{encounter.Id.Value}|{EncounterHash(encounter)}|{deckHash}|{shuffle}|{options.Horizon},{options.Beam},{options.PerTurn}"));
            }
        }
        var todo = all.Where(j => !known.ContainsKey(j.Key)).ToList();

        // THE ESTIMATE, BEFORE ANYTHING RUNS: seconds per fight from the store's own rows of that role, else a guess.
        var cores = options.Jobs > 0 ? options.Jobs : Environment.ProcessorCount;
        double Guess(string role) => known.Values.Where(r => r.Role == role).Select(r => r.Seconds).DefaultIfEmpty(
            role switch { "boss" => 60, "elite" => 30, "multi" => 25, _ => 15 }).Average();
        var minutes = todo.Sum(j => Guess(j.Role)) / cores / 60;
        say($"act bench · acts {string.Join(",", options.Acts)} · {snapshots.Count} snapshots · {all.Count} fights, "
            + $"{all.Count - todo.Count} in the store, {todo.Count} to fight · ~{minutes:0} min on {cores} cores · "
            + $"horizon {options.Horizon} beam {options.Beam} per turn {options.PerTurn}");
        if (minutes > 30 && !options.Yes)
        {
            say("over 30 minutes: start again with --yes to run it");
            return 1;
        }

        // Longest first, so no core sits idle behind one boss at the end.
        todo = [.. todo.OrderBy(j => j.Role switch { "boss" => 0, "elite" => 1, "multi" => 2, _ => 3 })];
        var gate = new object();
        var done = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Parallel.ForEach(todo, new ParallelOptions { MaxDegreeOfParallelism = cores }, job =>
        {
            var ring = SparringRing.OneFight(game, job.Encounter, job.Snap.Deck, maxHealth: job.Snap.MaxHealth,
                relics: job.Snap.Relics, character: job.Snap.Character);
            using var play = new RunPlayback(() => { });
            play.Start(ring, job.Shuffle, interactive: true);
            while (play.Session is { IsAwaitingInterlude: true } session)
                session.Continue();
            if (play.CombatDriver?.Current is not { } combat)
            {
                Console.Error.WriteLine($"  no fight: {job.Snap.Run} {job.Encounter.Id.Value}");
                return;
            }
            var planned = new FightPlanner(options.Horizon, options.Beam, options.PerTurn).Play(combat);
            var row = new Row(job.Key, job.Snap.Run, job.Snap.Character, job.Snap.Act, job.Role, job.Encounter.Id.Value,
                job.Shuffle, planned.Won, planned.Won ? planned.HealthLost : planned.HeroHealthAtStart, planned.Turns, planned.Seconds);
            lock (gate)
            {
                File.AppendAllText(store, JsonSerializer.Serialize(row, Line) + "\n");
                known[row.Key] = row;
            }
            if (Interlocked.Increment(ref done) % 50 == 0)
                Console.Error.WriteLine($"  {done}/{todo.Count} fights, {clock.Elapsed.TotalMinutes:0.0} min");
        });

        Report([.. all.Where(j => known.ContainsKey(j.Key)).Select(j => known[j.Key])], say);
        return 0;
    }

    // An act's encounters by role, as the act's own pools hand them out (a mimic is a treasure, not a fight here).
    private static IEnumerable<(string Role, EncounterDefinition Encounter)> Pool(RunBlueprint game, int act, string point)
    {
        var pool = game.Acts![act - 1].MapGeneration!.Encounters;
        var byId = game.Encounters.ToDictionary(e => e.Id.Value, StringComparer.Ordinal);
        IEnumerable<(string, MapNodeKind)> roles = point == "boss"
            ? [("boss", MapNodeKind.Boss)]
            : [("normal", MapNodeKind.Combat), ("multi", MapNodeKind.MultiCombat), ("elite", MapNodeKind.Elite)];
        foreach (var (role, kind) in roles)
            foreach (var id in pool.For(kind).Select(e => e.Encounter.Value).Distinct().Order(StringComparer.Ordinal))
                if (byId.TryGetValue(id, out var encounter))
                    yield return (role, encounter);
    }

    // What an encounter IS, for its hash: itself, its enemies' actions, and every status any of that names.
    private static string Describe(RunBlueprint game, EncounterDefinition encounter)
    {
        var options = RunJson.CreateOptions();
        var text = new StringBuilder(JsonSerializer.Serialize(encounter, options));
        foreach (var action in encounter.Enemies.SelectMany(e => e.Actions).Distinct())
            if (game.EnemyActions.FirstOrDefault(a => a.Id == action.value) is { } found)
                text.Append(JsonSerializer.Serialize(found, options));
        var named = text.ToString();
        foreach (var status in game.Statuses.Where(s => named.Contains($"\"{s.Id}\"", StringComparison.Ordinal)))
            text.Append(JsonSerializer.Serialize(status, options));
        return text.ToString();
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..12];

    private static IEnumerable<Row> Read(string store) =>
        File.Exists(store)
            ? File.ReadLines(store).Where(l => l.Length > 0).Select(l => JsonSerializer.Deserialize<Row>(l, Line)!)
            : [];

    private static void Report(IReadOnlyList<Row> rows, Action<string> say)
    {
        string[] roles = ["normal", "multi", "elite", "boss"];
        static double Median(IEnumerable<int> v)
        {
            var s = v.Order().ToList();
            return s.Count == 0 ? 0 : s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2.0;
        }
        foreach (var act in rows.Select(r => r.Act).Distinct().Order())
        {
            say($"\n── act {act} ── health lost per fight (planner, full HP) · target normal ~4 < multi ~10 < elite ~15 < boss ~20");
            say($"  {"role",-7} {"who",-12} {"fights",6} {"mean",6} {"median",6} {"worst",6} {"lost",5} {"turns",6}");
            foreach (var role in roles)
                foreach (var who in new[] { "all", "bureaucrat", "hedge_witch" })
                {
                    var g = rows.Where(r => r.Act == act && r.Role == role && (who == "all" || r.Character == who)).ToList();
                    if (g.Count == 0)
                        continue;
                    say($"  {role,-7} {who,-12} {g.Count,6} {g.Average(r => r.Lost),6:0.0} {Median(g.Select(r => r.Lost)),6:0.0} "
                        + $"{g.Max(r => r.Lost),6} {g.Count(r => !r.Won),5} {g.Average(r => r.Turns),6:0.0}");
                }
            say($"\n  per encounter (mean over decks · bureaucrat / witch · worst · turns):");
            foreach (var role in roles)
                foreach (var e in rows.Where(r => r.Act == act && r.Role == role).GroupBy(r => r.Encounter)
                             .OrderByDescending(g => g.Average(r => r.Lost)))
                {
                    string Of(string who) => e.Where(r => r.Character == who).Select(r => r.Lost).DefaultIfEmpty().Average().ToString("0.0");
                    say($"  {role,-7} {e.Key,-46} {e.Average(r => r.Lost),5:0.0}  B {Of("bureaucrat"),5} W {Of("hedge_witch"),5}  "
                        + $"worst {e.Max(r => r.Lost),3}  {e.Average(r => r.Turns),4:0.0}t{(e.Any(r => !r.Won) ? $"  LOST {e.Count(r => !r.Won)}" : "")}");
                }
        }
    }
}
