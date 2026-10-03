using System.Collections.Concurrent;
using System.Text.Json;
using RogueDeck.Bot;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Converter.Playtest;

// ── THE FAMILY BENCH ─────────────────────────────────────────────────────────────────────────────────────────
// What a keyword family is worth, measured the way a player meets it (user, 2026-10-02): not one card swapped
// into the Act-I starter — a family lives on its synergies, and those only show in a deck BUILT around it, against
// the fights of a later act. So: one finished deck per family and act, every deck on the same budget (see
// FAMILY_DECKS.md), each fought through a sample of the act's normal fights, all its elites and all its bosses,
// with and without the relics a player of that family would have picked.
//
//   decks        a JSON list (`--family-decks <file>`): name, family, act, cards {id: copies}, relics [ids]
//   the number   per fight: won?, health lost by the clairvoyant planner's line (a death loses everything).
//                Per deck: win rate and mean health KEPT per kind (normal / elite / boss), the kinds weighted
//                equally; per act and relic setting: each family's health kept as a share of Paperwork's.
//
// ⚠ The planner sees the draw pile — every number is what a player who could see the future would pay. Compare
// decks with each other, never with players.
public static class FamilyBench
{
    public sealed record Options(
        string DecksFile, int Shuffles = 4, int Horizon = 5, int Beam = 6, int PerTurn = 100, int Jobs = 0,
        int Normals = 10, int Health = 70, string Relics = "both", IReadOnlyList<string>? Only = null,
        IReadOnlyList<int>? Acts = null);

    public sealed record Deck(string Name, string Family, int Act, Dictionary<string, int> Cards, List<string>? Relics);

    private sealed record Fight(string Deck, bool WithRelics, string Kind, string Encounter, int Shuffle, PlannedFight Result);

    public static int Run(RunBlueprint game, Options options, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        var decks = JsonSerializer.Deserialize<List<Deck>>(
            File.ReadAllText(options.DecksFile), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
            .Where(d => options.Only is not { Count: > 0 } only || only.Contains(d.Name))
            .Where(d => options.Acts is not { Count: > 0 } acts || acts.Contains(d.Act))
            .ToList();

        // A misspelt id would fight with a deck one card short and say nothing; refuse instead.
        var cards = game.Cards.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var relics = game.Relics.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var unknown = decks.SelectMany(d => d.Cards.Keys.Where(id => !cards.Contains(id)).Select(id => $"{d.Name}: card {id}"))
            .Concat(decks.SelectMany(d => (d.Relics ?? []).Where(id => !relics.Contains(id)).Select(id => $"{d.Name}: relic {id}")))
            .ToList();
        if (unknown.Count > 0)
        {
            say("unknown ids:\n  " + string.Join("\n  ", unknown));
            return 1;
        }

        var settings = options.Relics switch
        {
            "none" => new[] { false },
            "only" => [true],
            _ => [false, true],
        };
        var byAct = decks.Select(d => d.Act).Distinct().ToDictionary(a => a, a => Encounters(game, a, options.Normals));
        say($"family bench · {decks.Count} decks · {options.Health} HP · {options.Shuffles} shuffles · horizon {options.Horizon} "
            + $"· beam {options.Beam} · per turn {options.PerTurn} · relics {options.Relics}");
        foreach (var (act, encounters) in byAct.OrderBy(a => a.Key))
            foreach (var kind in encounters.GroupBy(e => e.Kind))
                say($"  act {act} {kind.Key}: {string.Join(", ", kind.Select(e => e.Id))}");

        var jobs = (from deck in decks
                    from withRelics in settings
                    where !withRelics || deck.Relics is { Count: > 0 }
                    from encounter in byAct[deck.Act]
                    from shuffle in Enumerable.Range(1, options.Shuffles)
                    select (deck, withRelics, encounter, shuffle)).ToList();
        say($"  {jobs.Count} fights\n");

        // Longest first: the bosses go out before the normals, so no core sits idle behind one at the end.
        jobs = [.. jobs.OrderBy(j => j.encounter.Kind switch { "boss" => 0, "elite" => 1, _ => 2 })];
        var results = new ConcurrentBag<Fight>();
        var done = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Parallel.ForEach(jobs,
            new ParallelOptions { MaxDegreeOfParallelism = options.Jobs > 0 ? options.Jobs : Environment.ProcessorCount },
            job =>
            {
                var deckList = job.deck.Cards.SelectMany(kv => Enumerable.Repeat(kv.Key, kv.Value)).ToList();
                var encounter = game.Encounters.First(e => e.Id.Value == job.encounter.Id);
                var ring = SparringRing.OneFight(game, encounter, deckList, maxHealth: options.Health,
                    relics: job.withRelics ? job.deck.Relics : null);
                using var play = new RunPlayback(() => { });
                play.Start(ring, job.shuffle, interactive: true);
                while (play.Session is { IsAwaitingInterlude: true } session)
                    session.Continue();
                if (play.CombatDriver?.Current is not { } combat)
                {
                    Console.Error.WriteLine($"  no fight: {job.deck.Name} {job.encounter.Id}");
                    return;
                }
                var planned = new FightPlanner(options.Horizon, options.Beam, options.PerTurn).Play(combat);
                results.Add(new Fight(job.deck.Name, job.withRelics, job.encounter.Kind, job.encounter.Id, job.shuffle, planned));
                if (planned.Seconds > 300)
                    Console.Error.WriteLine($"  slow: {job.deck.Name} {job.encounter.Id} #{job.shuffle} {planned.Seconds:0}s {planned.Turns} turns");
                if (Interlocked.Increment(ref done) % 100 == 0)
                    Console.Error.WriteLine($"  {done}/{jobs.Count} fights, {clock.Elapsed.TotalMinutes:0.0} min");
            });

        Report(decks, [.. results], settings, options.Health, say);
        return 0;
    }

    private static void Report(
        IReadOnlyList<Deck> decks, IReadOnlyList<Fight> fights, bool[] settings, int health, Action<string> say)
    {
        string[] kinds = ["normal", "elite", "boss"];
        foreach (var act in decks.Select(d => d.Act).Distinct().Order())
            foreach (var withRelics in settings)
            {
                var rows = decks.Where(d => d.Act == act)
                    .Select(d => (Deck: d, Fights: fights.Where(f => f.Deck == d.Name && f.WithRelics == withRelics).ToList()))
                    .Where(r => r.Fights.Count > 0)
                    .Select(r => (r.Deck, r.Fights, Kept: kinds
                        .Select(k => r.Fights.Where(f => f.Kind == k).ToList())
                        .Where(g => g.Count > 0)
                        .Average(g => g.Average(f => health - f.Result.HealthLost))))
                    .OrderByDescending(r => r.Kept)
                    .ToList();
                if (rows.Count == 0)
                    continue;
                var reference = rows.FirstOrDefault(r => r.Deck.Family == "Paperwork");
                say($"\n── act {act}, {(withRelics ? "with relics" : "no relics")} ──  per kind: health kept (of {health}), won, turns · "
                    + "share = kept / Paperwork's kept · lost× = health lost / Paperwork's");
                say($"  {"deck",-20} {"normal",-18} {"elite",-18} {"boss",-18}  {"kept",5} {"share",6} {"lost×",6}");
                foreach (var (deck, own, kept) in rows)
                {
                    var cells = kinds.Select(k => own.Where(f => f.Kind == k).ToList())
                        .Select(g => g.Count == 0 ? "–" : $"{g.Average(f => health - f.Result.HealthLost),4:0.0} {g.Count(f => f.Result.Won),2}/{g.Count,-2} {g.Average(f => f.Result.Turns),4:0.0}t");
                    var share = reference.Kept > 0 ? $"{kept / reference.Kept,6:0%}" : "     –";
                    var lost = reference.Kept < health ? $"{(health - kept) / (health - reference.Kept),6:0.00}" : "     –";
                    say($"  {deck.Name,-20} {string.Join(" ", cells.Select(c => $"{c,-18}"))}  {kept,5:0.0} {share} {lost}");
                }
            }
    }

    // An act's fights: an even spread of `normals` over its normal and multi-fights (sorted by id, so every deck
    // meets the same ones), and every elite and boss.
    private static List<(string Kind, string Id)> Encounters(RunBlueprint game, int act, int normals)
    {
        var pool = game.Acts![act - 1].MapGeneration!.Encounters;
        var normal = pool.For(MapNodeKind.Combat).Concat(pool.For(MapNodeKind.MultiCombat))
            .Select(e => e.Encounter.Value).Distinct().Order(StringComparer.Ordinal).ToList();
        var picked = normal.Count <= normals
            ? normal
            : [.. Enumerable.Range(0, normals).Select(i => normal[i * normal.Count / normals])];
        return
        [
            .. picked.Select(id => ("normal", id)),
            .. pool.For(MapNodeKind.Elite).Select(e => e.Encounter.Value).Distinct().Order(StringComparer.Ordinal).Select(id => ("elite", id)),
            .. pool.For(MapNodeKind.Boss).Select(e => e.Encounter.Value).Distinct().Order(StringComparer.Ordinal).Select(id => ("boss", id)),
        ];
    }
}
