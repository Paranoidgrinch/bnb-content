using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// ── THE LIEUTENANT TRIAL RUNNER (LT_RUNNER_PLAN.md, agreed 2026-11-06) ────────────────────────────────────────
// The encounter trial (EncounterTrial), walked by a bot that plays each fight close to a strong human: the
// Lieutenant's start, the stages in order, the packages between them, the earlier acts' catch-up for a later act.
//
//   the walk      exactly the trial's: its stops (the packages) in order, read from the shipped document's
//                 Extra["trial:<act>"], each fight met with the deck the stops before it built (Singles' cut).
//   the packages  chosen by play (TrialChoices): each candidate deck plays the next stage with a cheap bot on the
//                 evaluation's own seeds, and what it gained is learned across runs (CardValues). Without
//                 Choices, the old static card rating (TrialPackages).
//   a fight       played by TrialBot (determinized search; it never sees the draw order or a future roll) on N
//                 fresh seeds with the same deck, each from full health (the Infirmary).
//   variance      P progressions with different reward seeds; each plays every stage on the deck it built.
//
// Output: a JSON file and a table per run in ~/Desktop/bnb-balance/bots/; with --compare, the user's hand-played
// trial log sits beside the bot's numbers.
public static class TrialRunner
{
    public sealed record Options(
        IReadOnlyList<int> Acts, int Seeds, int Paths, TrialBot.Options Bot, int Jobs, string OutDir,
        string? Compare = null, int Seed = 20261106, IReadOnlyList<string>? Only = null, bool Verbose = false,
        TrialChoices.Options? Choices = null, string? ValuesPath = null, PolicyBook? Policies = null, DeckModel? Model = null,
        string? DeckData = null, string? HumanDecks = null);

    // One fight of the walk: where it stands (stage, kind), the node the trial fields it at, and how many of the
    // walk's stops come before it.
    public sealed record TrialFight(int Index, int Stage, string Kind, string Encounter, int StopsBefore, Node Node);

    public sealed record TrialWalk(int Act, RunStart Start, IReadOnlyList<Node> Stops, IReadOnlyList<TrialFight> Fights);

    public sealed record Loadout(IReadOnlyList<string> Deck, IReadOnlyList<string> Relics, int MaxHealth);

    // A package question as a path meets it: the stop it stands in (0-based), its place among the path's answers,
    // the question, the run as it stands, what the bot has played so far on this path.
    public sealed record Decision(int Stop, int Index, EntitySelectionRequest Request, RunState Run, IReadOnlyDictionary<string, int> Played);

    // The answer and a note for the picks log.
    public delegate (IReadOnlyList<int> Picks, string? Note) Chooser(Path path, Decision decision);

    public sealed record FightRow(
        int Act, int Stage, string Kind, string Encounter, int Path, int Seed, bool Won, bool Ended, int Lost, int Rounds,
        int Refused, int Replans, double Seconds, string? Error, IReadOnlyDictionary<string, int> Played, long Positions = 0);

    // ── the walk ────────────────────────────────────────────────────────────────────────────────────────────

    public static RunTutorial Trial(RunBlueprint shipped, int act) =>
        JsonSerializer.Deserialize<RunTutorial>(shipped.Presentation.Game!.Extra[$"{EncounterTrial.ExtraPrefix}{act}"],
            RunJson.CreateOptions())!;

    public static IReadOnlyList<EncounterTrial.Single> Singles(RunBlueprint shipped, int act) =>
        JsonSerializer.Deserialize<List<EncounterTrial.Single>>(shipped.Presentation.Game!.Extra[EncounterTrial.EncountersKey(act)],
            RunJson.CreateOptions())!;

    public static TrialWalk Walk(RunBlueprint shipped, int act)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        var trial = Trial(shipped, act);
        var singles = Singles(shipped, act);
        var stops = new List<Node>();
        var fights = new List<TrialFight>();
        foreach (var node in trial.Map.Nodes)
        {
            if (node.Payload is EventRef)
            {
                stops.Add(node);
                continue;
            }
            if (node.Payload is not EncounterRef fight)
                throw new InvalidOperationException($"trial {act}: node {node.Id.Value} is neither a stop nor a fight");
            var single = singles[fights.Count];
            if (single.Encounter != fight.Id.Value)
                throw new InvalidOperationException($"trial {act}: fight {fights.Count} is {fight.Id.Value}, its single {single.Encounter}");
            fights.Add(new TrialFight(fights.Count, single.Stage, single.Kind, fight.Id.Value, stops.Count, node));
        }
        return new TrialWalk(act, trial.Start!, stops, fights);
    }

    // What the frontend does with a walk (GameHost.StartTrial): the walk in place of the acts, the Lieutenant alone.
    private static RunBlueprint WalkBlueprint(RunBlueprint shipped, RunStart start, IReadOnlyList<Node> nodes) =>
        (shipped with
        {
            Map = new RunMap([.. nodes]),
            Acts = null,
            MapGeneration = null,
            StrategicMapGeneration = null,
            Tutorial = null,
        }).AsLoneFighter(start, LieutenantCharacter.Id);

    // ── one progression: the trial's stops, walked with the package choices ─────────────────────────────────
    public sealed class Path : IDisposable
    {
        private readonly RunPlayback _play;
        private readonly IReadOnlyList<Node> _stops;
        private readonly TrialPackages _packages;
        private readonly HashSet<string> _known;
        private readonly Chooser? _choose;
        private readonly IReadOnlyList<IReadOnlyList<int>>? _script;
        private int _started = -1;

        public List<string> Picks { get; } = [];
        public int StopsDone { get; private set; }
        public int Seed { get; }

        // Every answer given to a package question, in order: the path's whole history of choices. Replayed on a
        // fresh path with the same seed, it walks the same run (TrialChoices builds a candidate's deck that way).
        public List<IReadOnlyList<int>> Answers { get; } = [];

        // The stop being opened or answered (0-based), -1 before the first.
        public int Stop => _started;

        // `choose` answers the package questions (null: the static rating, TrialPackages). `script` answers the
        // first questions instead, in order.
        public Path(RunBlueprint shipped, TrialWalk walk, int seed, TrialPackages packages, Chooser? choose = null,
            IReadOnlyList<IReadOnlyList<int>>? script = null)
        {
            ArgumentNullException.ThrowIfNull(shipped);
            ArgumentNullException.ThrowIfNull(walk);
            _stops = walk.Stops;
            _packages = packages;
            _choose = choose;
            _script = script;
            Seed = seed;
            _known = shipped.Cards.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            _play = new RunPlayback(() => { });
            if (_stops.Count == 0)
                return;
            _play.Start(WalkBlueprint(shipped, walk.Start, _stops), seed, interactive: true, characterId: LieutenantCharacter.Id);
            if (_play.Error is { } error)
                throw new InvalidOperationException($"the trial walk would not start: {error}");
        }

        private RunState? Run => _play.Session?.Run;

        // Walk on until `stops` stops are behind; the next stop is not begun. `played` is what the bot has played
        // so far on this progression (for an improvement nothing rates).
        public void Advance(int stops, IReadOnlyDictionary<string, int> played, RunStart start)
        {
            if (_stops.Count == 0 || StopsDone >= stops)
                return;
            var session = _play.Session!;
            for (var step = 0; step < 10000; step++)
            {
                if (_play.Error is { } hostError)
                    throw new InvalidOperationException($"the trial walk broke: {hostError}");
                if (session.Error is { } runError)
                    throw new InvalidOperationException($"the trial walk broke: {runError}");
                if (session.IsComplete)
                {
                    StopsDone = _stops.Count;
                    return;
                }
                if (session.IsAwaitingNodeChoice)
                {
                    session.PickNode(session.PendingNodeChoices[0].Id.Value);
                    continue;
                }
                if (session.IsAwaitingInterlude)
                {
                    session.Continue();
                    continue;
                }
                // Every stop opens with its one choice ("collect"), and the run names no node while an event
                // stands — so the stops are counted by their openings.
                if (session.IsAwaitingChoice)
                {
                    StopsDone = _started + 1;
                    if (StopsDone >= stops)
                        return;
                    _started = StopsDone;
                }
                var at = Math.Max(0, _started);
                if (session.IsAwaitingChoice)
                {
                    var choice = session.PendingChoices[0];
                    Picks.Add($"{_stops[at].Id.Value}: {choice.Id}");
                    session.Pick(choice.Id);
                }
                else if (session.IsAwaitingEntities && session.PendingEntities is { } request)
                {
                    string? note = null;
                    IReadOnlyList<int> picks;
                    if (_script is not null && Answers.Count < _script.Count)
                        picks = _script[Answers.Count];
                    else if (_choose is not null)
                        (picks, note) = _choose(this, new Decision(at, Answers.Count, request, session.Run, played));
                    else
                        picks = _packages.Pick(request, session.Run, played);
                    Answers.Add([.. picks]);
                    Picks.Add($"{_stops[at].Id.Value}: {_packages.Describe(request, picks)}{(note is null ? "" : " · " + note)}");
                    session.PickEntities(picks);
                }
                else
                    throw new InvalidOperationException($"the trial walk parked at {session.Run.CurrentNodeId?.Value} with nothing to answer");
            }
            throw new InvalidOperationException("the trial walk ran out of steps");
        }

        // The deck as a fight deals it (an improved copy is "<id>+" where that card exists), the relics held, the
        // body's maximum.
        public Loadout Loadout(RunStart start)
        {
            if (Run is not { } run)
                return new Loadout([.. start.Deck.Select(c => c.value)], [.. start.StartingRelics], start.MaxHealth);
            return new Loadout(
                [.. run.Deck.Select(c => c.UpgradeLevel > 0 && _known.Contains(c.DefinitionId.value + "+") ? c.DefinitionId.value + "+" : c.DefinitionId.value)],
                [.. run.Relics.Where(r => r.Enabled).Select(r => r.Id.Value)],
                run.Primary.Health.Max);
        }

        public void Dispose() => _play.Dispose();
    }

    // ── one fight on a stated loadout and a real seed ───────────────────────────────────────────────────────

    public static RunBlueprint Ring(RunBlueprint shipped, TrialFight fight, Loadout loadout)
    {
        var encounter = shipped.Encounters.First(e => e.Id.Value == fight.Encounter);
        var ring = SparringRing.OneFight(shipped, encounter, loadout.Deck, maxHealth: loadout.MaxHealth,
            relics: loadout.Relics, character: LieutenantCharacter.Id);
        // The room as the trial fields it: its own node (type and tags), not the ring's plain combat node.
        return ring with { Map = new RunMap([fight.Node]) };
    }

    // The real fight, started: the ring's playback (kept alive by the caller) and the fight as it opens.
    public static (RunPlayback Play, InteractiveCombat Real) Open(RunBlueprint ring, int seed)
    {
        var play = new RunPlayback(() => { });
        play.Start(ring, seed, interactive: true);
        if (play.Error is { } error)
            throw new InvalidOperationException($"the ring would not start: {error}");
        while (play.Session is { IsAwaitingInterlude: true } session)
            session.Continue();
        var driver = play.CombatDriver ?? throw new InvalidOperationException("the ring never started a fight");
        // A question asked as the opening hand is dealt is answered with the first option (a world could not be
        // forked halfway through the deal); after that the fight is the bot's alone.
        var rng = new Random(seed);
        for (var guard = 0; guard < 20 && Greedy.Answer(driver, rng); guard++)
        {
        }
        var opening = driver.Current ?? throw new InvalidOperationException("the ring never started a fight");
        return (play, opening.ForkQuiet());
    }

    public static FightRow Fight(RunBlueprint shipped, TrialFight fight, Loadout loadout, int act, int path, int seed,
        int botSeed, TrialBot.Options bot, Action<string>? log = null, PolicyBook? book = null)
    {
        var clock = Stopwatch.StartNew();
        if (book is not null)
            bot = bot with { Policy = book.For(fight.Encounter, act) };
        try
        {
            var (play, real) = Open(Ring(shipped, fight, loadout), seed);
            using var _ = play;
            var needs = play.CardNeedsTarget;
            var outcome = new TrialBot(bot, botSeed, id => needs.TryGetValue(id, out var n) && n).Play(real, log);
            return new FightRow(act, fight.Stage, fight.Kind, fight.Encounter, path, seed, outcome.Won, outcome.Ended,
                outcome.Lost, outcome.Rounds, outcome.Refused, outcome.Replans, clock.Elapsed.TotalSeconds, outcome.Error, outcome.Played,
                outcome.Positions);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
        {
            return new FightRow(act, fight.Stage, fight.Kind, fight.Encounter, path, seed, false, false, loadout.MaxHealth, 0, 0, 0,
                clock.Elapsed.TotalSeconds, ex.Message, new Dictionary<string, int>());
        }
    }

    // A fight's real seed and the bot's own seed, from where the fight stands — never one from the other.
    public static int FightSeed(int baseSeed, int act, int fight, int path, int n) =>
        Mix(baseSeed, act, fight, path, n, 0x5eed);

    public static int BotSeed(int baseSeed, int act, int fight, int path, int n) =>
        Mix(baseSeed, act, fight, path, n, 0xb07);

    public static int PathSeed(int baseSeed, int act, int path) => Mix(baseSeed, act, -1, path, -1, 0x9a7);

    private static int Mix(params int[] parts)
    {
        unchecked
        {
            var h = (int)2166136261;
            foreach (var p in parts)
                h = (h ^ p) * 16777619 ^ (h >> 13);
            return h & int.MaxValue;
        }
    }

    // ── the run ─────────────────────────────────────────────────────────────────────────────────────────────

    public static int Run(RunBlueprint game, Options options, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        // Through the exported document, as the frontend loads it.
        var json = RunJson.CreateOptions(indented: false);
        var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(game, json), json);
        var packages = new TrialPackages(shipped);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        Directory.CreateDirectory(options.OutDir);
        var name = $"lt-runner-act{string.Join("", options.Acts)}-{stamp}";
        var b = options.Bot;
        var c = options.Choices;
        say($"lieutenant trial runner · acts {string.Join(",", options.Acts)} · {options.Paths} progression(s) × {options.Seeds} seed(s) "
            + $"per encounter · K {b.Worlds} worlds · H {b.Horizon} turns · beam {b.Beam} · {b.PerTurn} positions/turn · {options.Jobs} jobs");
        say(c is null ? "packages: static card rating"
            : $"packages: by play · K {c.Bot.Worlds} · H {c.Bot.Horizon} · beam {c.Bot.Beam} · perTurn {c.Bot.PerTurn} · {c.Seeds} seed(s) per fight · "
              + $"top {c.Top} · learned values {options.ValuesPath ?? "off"}");
        var values = CardValues.Load(options.ValuesPath);
        var themes = c is null ? null : new DeckThemes(shipped);
        var measured = TrialChoices.MeasuredSeeds(options.Seed, options.Acts, a => Walk(shipped, a).Fights.Count, options.Paths, options.Seeds);
        var evalScheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Math.Max(1, options.Jobs)).ConcurrentScheduler;
        var evalParallel = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.Jobs), TaskScheduler = evalScheduler };

        var humanDecks = options.HumanDecks is { } deckLog ? HumanLoadouts(deckLog) : null;
        var rows = new ConcurrentBag<FightRow>();
        var picks = new Dictionary<string, List<string>>();
        var timings = new List<(int Act, double Minutes, int Fights, double ChoiceMinutes, int Decisions, int EvalFights)>();
        var total = Stopwatch.StartNew();
        foreach (var act in options.Acts)
        {
            var clock = Stopwatch.StartNew();
            var walk = Walk(shipped, act);
            var fights = walk.Fights.Where(f => options.Only is not { Count: > 0 } only || only.Contains(f.Encounter)).ToList();
            // The prior is the table as the act begins: every path reads the same, whatever the others learn meanwhile.
            var prior = values.Copy();
            var choices = Enumerable.Range(0, options.Paths)
                .Select(p => c is null ? null : new TrialChoices(shipped, walk, packages, prior, c, options.Seed, measured, evalParallel, themes!)).ToList();
            var paths = Enumerable.Range(0, options.Paths)
                .Select(p => new Path(shipped, walk, PathSeed(options.Seed, act, p), packages,
                    choices[p] is { } chooser ? (path, d) => chooser.Choose(path, d, act, p) : null)).ToList();
            var choiceClock = new Stopwatch();
            var played = Enumerable.Range(0, options.Paths).Select(_ => new Dictionary<string, int>(StringComparer.Ordinal)).ToList();
            var count = 0;
            foreach (var stage in fights.GroupBy(f => f.Stage).OrderBy(g => g.Key))
            {
                // Each progression walks on to this stage: the stops before its fights, with what it has played.
                var loadouts = new Loadout[options.Paths];
                choiceClock.Start();
                Parallel.For(0, options.Paths, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.Paths) }, p =>
                {
                    paths[p].Advance(stage.Min(f => f.StopsBefore), played[p], walk.Start);
                    loadouts[p] = paths[p].Loadout(walk.Start);
                });
                choiceClock.Stop();
                var jobs = (from f in stage from p in Enumerable.Range(0, options.Paths) from n in Enumerable.Range(0, options.Seeds)
                            select (Fight: f, Path: p, N: n))
                    .OrderBy(j => j.Fight.Kind switch { "boss" => 0, "elite" => 1, "multi" => 2, _ => 3 }).ToList();
                var stageRows = new ConcurrentBag<FightRow>();
                Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.Jobs) }, job =>
                {
                    var lines = options.Verbose ? new List<string>() : null;
                    var loadout = humanDecks?.GetValueOrDefault((act, job.Fight.Encounter)) ?? loadouts[job.Path];
                    var row = Fight(shipped, job.Fight, loadout, act, job.Path,
                        FightSeed(options.Seed, act, job.Fight.Index, job.Path, job.N),
                        BotSeed(options.Seed, act, job.Fight.Index, job.Path, job.N), options.Bot, lines is null ? null : lines.Add, options.Policies);
                    if (lines is not null)
                        lock (stageRows)
                        {
                            say($"── {job.Fight.Encounter} path {job.Path} seed {row.Seed}: deck {string.Join(",", loadouts[job.Path].Deck)} · relics {string.Join(",", loadouts[job.Path].Relics)}");
                            lines.ForEach(say);
                            say($"   => won {row.Won} lost {row.Lost} rounds {row.Rounds} {row.Error}");
                        }
                    stageRows.Add(row);
                });
                foreach (var row in stageRows)
                {
                    rows.Add(row);
                    foreach (var (card, times) in row.Played)
                        played[row.Path][card] = played[row.Path].GetValueOrDefault(card) + times;
                }
                count += jobs.Count;
                var broke = stageRows.Count(r => r.Error is not null);
                say($"  act {act} stage {stage.Key}: {stage.Count()} encounter(s), {jobs.Count} fights, "
                    + $"{clock.Elapsed.TotalMinutes:0.0} min so far ({choiceClock.Elapsed.TotalMinutes:0.0} choosing, "
                    + $"{choices.Sum(x => x?.Decisions ?? 0)} decisions, {choices.Sum(x => x?.EvalFights ?? 0)} play-out fights)"
                    + $"{(broke > 0 ? $", {broke} BROKE" : "")}");
            }
            for (var p = 0; p < options.Paths; p++)
            {
                picks[$"act{act}/path{p}"] = paths[p].Picks;
                paths[p].Dispose();
            }
            // What the act's paths learned, merged in a fixed order (path, then decision) and kept.
            if (options.ValuesPath is { } valuesPath && c is not null)
            {
                foreach (var chooser in choices)
                    foreach (var (_, learned) in chooser!.LearnedBy.OrderBy(kv => kv.Key))
                        learned.ForEach(values.Add);
                values.Save(valuesPath);
            }
            timings.Add((act, clock.Elapsed.TotalMinutes, count, choiceClock.Elapsed.TotalMinutes,
                choices.Sum(x => x?.Decisions ?? 0), choices.Sum(x => x?.EvalFights ?? 0)));
            say($"act {act}: {count} fights in {clock.Elapsed.TotalMinutes:0.0} min ({choiceClock.Elapsed.TotalMinutes:0.0} min choosing)");
        }

        var human = options.Compare is { } log ? Human(log) : null;
        var table = Table(rows.ToList(), human, options);
        var text = new StringBuilder();
        text.AppendLine($"lieutenant trial runner · {DateTime.Now:yyyy-MM-dd HH:mm} · acts {string.Join(",", options.Acts)}");
        text.AppendLine($"{options.Paths} progression(s) × {options.Seeds} seed(s) · K {b.Worlds} · H {b.Horizon} · beam {b.Beam} · "
            + $"perTurn {b.PerTurn} · seed {options.Seed}");
        if (c is not null)
            text.AppendLine($"packages by play · K {c.Bot.Worlds} · H {c.Bot.Horizon} · beam {c.Bot.Beam} · perTurn {c.Bot.PerTurn} · "
                + $"{c.Seeds} seed(s) per fight · top {c.Top} · learned values {options.ValuesPath ?? "off"}");
        foreach (var (act, minutes, n, choosing, decisions, evalFights) in timings)
            text.AppendLine($"act {act}: {n} fights, {minutes:0.0} min ({choosing:0.0} min choosing: {decisions} decisions, {evalFights} play-out fights)");
        text.AppendLine();
        text.Append(table);
        File.WriteAllText(System.IO.Path.Combine(options.OutDir, name + ".txt"), text.ToString());
        File.WriteAllText(System.IO.Path.Combine(options.OutDir, name + ".json"), JsonSerializer.Serialize(new
        {
            Options = new { options.Acts, options.Seeds, options.Paths, options.Bot, options.Seed, options.Compare, options.Choices, options.ValuesPath },
            Timings = timings.Select(t => new { t.Act, t.Minutes, t.Fights, t.ChoiceMinutes, t.Decisions, t.EvalFights }),
            Encounters = Summaries(rows.ToList(), human),
            Picks = picks,
            Fights = rows.OrderBy(r => r.Act).ThenBy(r => r.Stage).ThenBy(r => r.Encounter, StringComparer.Ordinal)
                .ThenBy(r => r.Path).ThenBy(r => r.Seed),
        }, new JsonSerializerOptions { WriteIndented = true }));
        say(text.ToString());
        if (options.ValuesPath is { } kept && c is not null)
        {
            values.Runs++;
            values.Save(kept);
            say($"learned values: {kept} (+ .txt)");
        }
        say($"wrote {System.IO.Path.Combine(options.OutDir, name)}.txt/.json · {total.Elapsed.TotalMinutes:0.0} min in all");
        return rows.Any(r => r.Error is not null) ? 1 : 0;
    }

    // ── targets (bnb-encounter-trial memory, tools/trial_report.py) ─────────────────────────────────────────

    private static readonly Dictionary<int, Dictionary<string, int>> LostTarget = new()
    {
        [1] = new() { ["normal"] = 3, ["multi"] = 6, ["elite"] = 10, ["boss"] = 20 },
        [2] = new() { ["normal"] = 4, ["multi"] = 8, ["elite"] = 12, ["boss"] = 30 },
        [3] = new() { ["normal"] = 5, ["multi"] = 10, ["elite"] = 14, ["boss"] = 40 },
        [4] = new() { ["normal"] = 6, ["multi"] = 12, ["elite"] = 16, ["boss"] = 50 },
    };

    private static readonly Dictionary<string, (int Low, int High)> RoundsTarget = new()
    {
        ["normal"] = (3, 4), ["multi"] = (5, 6), ["elite"] = (7, 8), ["boss"] = (9, 10),
    };

    public sealed record Summary(
        int Act, int Stage, string Kind, string Encounter, int Fights, double WinRate, double LostMean, double LostMedian,
        int LostMax, double LostSd, double RoundsMean, int RoundsMax, int? LostTargetValue, bool? LostMet, bool? SpreadMet,
        string RoundsTargetText, bool? RoundsMet, double? HumanLost, double? HumanRounds, string? HumanWon, int Broke);

    // Lost "met": a normal/multi/elite within ±25 % of its target (at least ±1), a boss within ±20 (user's boss
    // rule). Spread "met": max ≤ 2 × mean and sd ≤ 50 % of the mean; a boss sd ≤ 10. Rounds "met": the mean
    // within the target span ± 0.5. A lost fight counts as everything the Lieutenant had (as trial_report.py).
    public static List<Summary> Summaries(IReadOnlyList<FightRow> rows, IReadOnlyDictionary<(int, string), (double Lost, double Rounds, string Won)>? human)
    {
        var list = new List<Summary>();
        foreach (var g in rows.GroupBy(r => (r.Act, r.Stage, r.Kind, r.Encounter)).OrderBy(g => g.Key.Act).ThenBy(g => g.Key.Stage)
                     .ThenBy(g => Array.IndexOf(["normal", "multi", "elite", "boss"], g.Key.Kind)).ThenBy(g => g.Key.Encounter, StringComparer.Ordinal))
        {
            var lost = g.Select(r => r.Lost).Order().ToList();
            var mean = lost.Average();
            var median = lost.Count % 2 == 1 ? lost[lost.Count / 2] : (lost[lost.Count / 2 - 1] + lost[lost.Count / 2]) / 2.0;
            var sd = Math.Sqrt(lost.Sum(x => (x - mean) * (x - mean)) / Math.Max(1, lost.Count - 1));
            var rounds = g.Select(r => r.Rounds).ToList();
            int? target = LostTarget.TryGetValue(g.Key.Act, out var t) ? t[g.Key.Kind] : null;
            bool? lostMet = target is { } tv ? g.Key.Kind == "boss" ? Math.Abs(mean - tv) <= 20 : Math.Abs(mean - tv) <= Math.Max(1, 0.25 * tv) : null;
            bool? spreadMet = target is null ? null : g.Key.Kind == "boss" ? sd <= 10 : lost.Max() <= 2 * Math.Max(mean, 0.5) && sd <= 0.5 * Math.Max(mean, 1);
            var (low, high) = RoundsTarget[g.Key.Kind];
            var roundsMean = rounds.Average();
            bool? roundsMet = g.Key.Act <= 4 ? roundsMean >= low - 0.5 && roundsMean <= high + 0.5 : null;
            var h = human is not null && human.TryGetValue((g.Key.Act, g.Key.Encounter), out var hv) ? hv : ((double, double, string)?)null;
            list.Add(new Summary(g.Key.Act, g.Key.Stage, g.Key.Kind, g.Key.Encounter, lost.Count, g.Count(r => r.Won) / (double)lost.Count,
                mean, median, lost.Max(), sd, roundsMean, rounds.Max(), target, lostMet, spreadMet, $"{low}-{high}", roundsMet,
                h?.Item1, h?.Item2, h?.Item3, g.Count(r => r.Error is not null)));
        }
        return list;
    }

    private static string Table(IReadOnlyList<FightRow> rows, IReadOnlyDictionary<(int, string), (double Lost, double Rounds, string Won)>? human, Options options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("lost: mean/median/max (sd) · target, ok = within ±25 % (boss ±20) · spread ok = max ≤ 2×mean, sd ≤ 50 % (boss sd ≤ 10)");
        sb.AppendLine("rounds: mean/max · target span, ok = within ±0.5 · a lost fight costs everything the Lieutenant had");
        if (human is not null)
            sb.AppendLine($"human: {options.Compare} (lost / rounds / won-of-fought)");
        foreach (var act in rows.GroupBy(s => s.Act).OrderBy(g => g.Key))
        {
            sb.AppendLine($"\n── Act {act.Key} ──");
            sb.AppendLine($"  {"st",2} {"kind",-6} {"encounter",-44} {"win",5} {"lost mean/med/max (sd)",-24} {"tgt",3} {"ok",-3} {"spr",-3} "
                + $"{"rounds",-9} {"tgt",-5} {"ok",-3}{(human is not null ? "  human lost/rnd/won" : "")}");
            foreach (var s in Summaries([.. act], human))
            {
                static string Yes(bool? b) => b is null ? "—" : b.Value ? "ok" : "NO";
                var lostText = $"{s.LostMean,5:0.0}/{s.LostMedian,4:0.#}/{s.LostMax,3} ({s.LostSd,4:0.0})";
                sb.AppendLine($"  {s.Stage,2} {s.Kind,-6} {s.Encounter,-44} {s.WinRate * 100,4:0}% {lostText,-24} {s.LostTargetValue?.ToString() ?? "—",3} "
                    + $"{Yes(s.LostMet),-3} {Yes(s.SpreadMet),-3} {s.RoundsMean,4:0.0}/{s.RoundsMax,-3} {s.RoundsTargetText,-5} {Yes(s.RoundsMet),-3}"
                    + (human is not null ? s.HumanLost is { } hl ? $"  {hl,5:0.0} / {s.HumanRounds,4:0.0} / {s.HumanWon}" : "  —" : "")
                    + (s.Broke > 0 ? $"  BROKE {s.Broke}" : ""));
            }
            foreach (var kind in new[] { "normal", "multi", "elite", "boss" })
            {
                var of = Summaries([.. act], human).Where(s => s.Kind == kind).ToList();
                if (of.Count == 0)
                    continue;
                sb.AppendLine($"     {kind,-6} mean over encounters {of.Average(s => s.LostMean),5:0.0} lost, {of.Average(s => s.RoundsMean),4:0.0} rounds, "
                    + $"win {of.Average(s => s.WinRate) * 100:0}%"
                    + (of.Any(s => s.HumanLost is not null) ? $" · human {of.Where(s => s.HumanLost is not null).Average(s => s.HumanLost!.Value),5:0.0} lost, "
                        + $"{of.Where(s => s.HumanRounds is not null).Average(s => s.HumanRounds!.Value),4:0.0} rounds" : ""));
            }
        }
        return sb.ToString();
    }

    // `--runner-human-decks <log>`: the deck, relics and max health the user fought each encounter with, so the bot
    // plays the very same fight — a bot that loses where the user lost nothing is told apart from a bot that drafted
    // worse. The last entry wins where an encounter was fought twice.
    public static Dictionary<(int, string), Loadout> HumanLoadouts(string path) =>
        File.ReadAllLines(path).Where(l => l.Trim().Length > 0).Select(l => JsonDocument.Parse(l).RootElement.Clone())
            .GroupBy(r => (r.GetProperty("act").GetInt32(), r.GetProperty("encounter").GetString()!))
            .ToDictionary(g => g.Key, g => g.Last() is var r
                ? new Loadout([.. r.GetProperty("deck").EnumerateArray().Select(c => c.GetString()!)],
                    [.. r.GetProperty("relics").EnumerateArray().Select(c => c.GetString()!)], r.GetProperty("max").GetInt32())
                : null!);

    // The user's hand-played trial log: per (act, encounter) mean lost (a lost fight costs everything), rounds, won/fought.
    public static Dictionary<(int, string), (double Lost, double Rounds, string Won)> Human(string path)
    {
        var lines = File.ReadAllLines(path).Where(l => l.Trim().Length > 0)
            .Select(l => JsonDocument.Parse(l).RootElement.Clone()).ToList();
        return lines.GroupBy(r => (r.GetProperty("act").GetInt32(), r.GetProperty("encounter").GetString()!))
            .ToDictionary(g => g.Key, g => (
                g.Average(r => r.GetProperty("lost").GetDouble()),
                g.Average(r => r.GetProperty("rounds").GetDouble()),
                $"{g.Count(r => r.GetProperty("won").GetBoolean())}/{g.Count()}"));
    }
}
