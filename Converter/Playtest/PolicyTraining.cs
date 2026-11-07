using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using RogueDeck.Run;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Converter.Playtest;

// ── LEARNING TO PLAY EACH ENCOUNTER, AND TO BUILD THE DECK (the overnight training, LT_RUNNER_PLAN) ─────────────
//
// PLAY (TrainPolicies). Per act: a few trial decks per stage (ModelChooser walks the trial's own paths), then
//   1. a SHARED policy for the act: the cross-entropy method over PlayPolicy's weights, its fitness a few cases
//      of every encounter of the act;
//   2. per encounter, the same method started from the shared policy, on that encounter's training cases.
//   A case is (deck, real seed, bot seed). The fitness of weights is the mean over the cases of health lost +
//   100 for a loss + 0.1 per round, played by the policy alone (TrialBot with Worlds 0: each turn the line whose
//   end the weights price lowest, read on a world — never on the real fight). Training seeds, validation seeds
//   and every seed the runner measures are three disjoint sets; the numbers reported are validation numbers.
//   Every generation is a checkpoint (<dir>/ckpt/<name>.json); a finished policy is <dir>/<name>.json and is
//   skipped when its configuration matches. Generation g draws its samples from (seed, act, name, g) alone, so a
//   resumed run continues exactly where it stopped.
//
// DECKS (DeckRounds). Progressions walk the trial with ModelChooser (the model's best pick, or a random one with
// probability ε, so the data sees cards the model does not yet favour); after each stage's stops the deck plays
// that stage's fights with the learned policies; each (path, stage) is a row for DeckModel.
public static class PolicyTraining
{
    public sealed record Options(
        IReadOnlyList<int> Acts, string Dir, int Gens = 20, int Pop = 16, int Elite = 4, int TrainSeeds = 12,
        int ValidSeeds = 16, int Decks = 4, int PerTurn = 30, int Seed = 20261106, int Jobs = 8, double Minutes = 0,
        int SharedGens = 12, int SharedCases = 2, IReadOnlyList<string>? Only = null, bool Warm = false, int MinGens = 2,
        string? ModelPath = null, string? ValuesPath = null, int MeasuredPaths = 5, int MeasuredSeeds = 20, string? Compare = null);

    public sealed record Case(TrialRunner.TrialFight Fight, TrialRunner.Loadout Loadout, int Seed, int BotSeed);

    public static double Score(TrialRunner.FightRow row) =>
        row.Error is not null ? 200 : row.Lost + (row.Won ? 0 : 100) + 0.1 * row.Rounds;

    // ── the decks the training plays: the trial's own paths, chosen by the model ────────────────────────────

    public static Dictionary<int, TrialRunner.Loadout[]> Decks(RunBlueprint shipped, TrialRunner.TrialWalk walk, TrialPackages packages,
        ModelChooser chooser, int decks, int seed, int jobs)
    {
        var stages = walk.Fights.GroupBy(f => f.Stage).OrderBy(g => g.Key).Select(g => (Stage: g.Key, Stops: g.Min(f => f.StopsBefore))).ToList();
        var result = stages.ToDictionary(s => s.Stage, _ => new TrialRunner.Loadout[decks]);
        Parallel.For(0, decks, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, jobs) }, p =>
        {
            var dice = new Random(Mix(seed, walk.Act, p, 0xdec));
            using var path = new TrialRunner.Path(shipped, walk, TrialRunner.PathSeed(seed ^ 0x7a1, walk.Act, p), packages,
                (pa, d) => chooser.Choose(pa, d, walk, 0, dice));
            foreach (var (stage, stops) in stages)
            {
                path.Advance(stops, new Dictionary<string, int>(), walk.Start);
                result[stage][p] = path.Loadout(walk.Start);
            }
        });
        return result;
    }

    // Training and validation seeds: their own salts, never a seed the runner measures, never one another's.
    public static (List<int> Train, List<int> Valid) Seeds(int baseSeed, int act, int fight, int train, int valid, IReadOnlySet<int> measured)
    {
        ArgumentNullException.ThrowIfNull(measured);
        var taken = new HashSet<int>(measured);
        List<int> Draw(int count, int salt)
        {
            var list = new List<int>();
            for (var n = 0; list.Count < count; n++)
            {
                var s = Mix(baseSeed, act, fight, n, salt);
                if (taken.Add(s))
                    list.Add(s);
            }
            return list;
        }
        var t = Draw(train, 0x7ea1);
        var v = Draw(valid, 0x7a11d);
        return (t, v);
    }

    private static List<Case> Cases(TrialRunner.TrialFight fight, TrialRunner.Loadout[] decks, IReadOnlyList<int> seeds, int salt) =>
        [.. seeds.Select((s, i) => new Case(fight, decks[i % decks.Length], s, Mix(s, salt, i)))];

    // ── fitness: the policy alone, on every case ────────────────────────────────────────────────────────────

    public static double[] Evaluate(RunBlueprint shipped, int act, IReadOnlyList<PlayPolicy> policies, IReadOnlyList<Case> cases,
        int perTurn, int jobs, List<TrialRunner.FightRow>? rows = null)
    {
        var scores = new double[policies.Count * cases.Count];
        var kept = new TrialRunner.FightRow[scores.Length];
        Parallel.For(0, scores.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, jobs) }, j =>
        {
            var (p, c) = (j / cases.Count, j % cases.Count);
            var bot = new TrialBot.Options(Worlds: 0, PerTurn: perTurn, Policy: policies[p]);
            var row = TrialRunner.Fight(shipped, cases[c].Fight, cases[c].Loadout, act, 0, cases[c].Seed, cases[c].BotSeed, bot);
            scores[j] = Score(row);
            kept[j] = row;
        });
        rows?.AddRange(kept);
        return [.. Enumerable.Range(0, policies.Count).Select(p => Enumerable.Range(0, cases.Count).Average(c => scores[p * cases.Count + c]))];
    }

    // ── the cross-entropy method, one generation a checkpoint ───────────────────────────────────────────────

    public sealed class Checkpoint
    {
        public string Config { get; set; } = "";
        public int Gen { get; set; }
        public double[] Mean { get; set; } = [];
        public double[] Sd { get; set; } = [];
        public double[] Best { get; set; } = [];
        public double BestFit { get; set; } = double.MaxValue;
        public List<double> History { get; set; } = [];
    }

    public static (PlayPolicy Policy, double TrainFit, int Gens) Cem(RunBlueprint shipped, int act, string name, PlayPolicy start,
        IReadOnlyList<Case> cases, Options o, int gens, string? checkpoint, Func<bool> outOfTime, Action<string> say, Func<bool>? hardOut = null)
    {
        var config = $"{o.Seed}|{o.Pop}|{o.Elite}|{o.PerTurn}|{cases.Count}|{start.Weights.Sum():0.0000}";
        var d = PlayPolicy.Count;
        hardOut ??= () => false;
        Checkpoint ck;
        if (checkpoint is not null && File.Exists(checkpoint)
            && JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(checkpoint)) is { } saved && saved.Config == config)
        {
            ck = saved;
            say($"    {name}: resumed at generation {ck.Gen} (best {ck.BestFit:0.00})");
        }
        else
        {
            ck = new Checkpoint
            {
                Config = config,
                Mean = [.. start.Weights],
                Sd = [.. start.Weights.Select(w => Math.Max(0.5, 0.5 * Math.Abs(w)))],
                Best = [.. start.Weights],
            };
            ck.BestFit = Evaluate(shipped, act, [start], cases, o.PerTurn, o.Jobs)[0];
            ck.History.Add(ck.BestFit);
        }
        // Past its share of the budget an encounter stops after MinGens; past the whole budget (+10 %) at once.
        while (ck.Gen < gens && !(ck.Gen >= o.MinGens && outOfTime()) && !(o.Minutes > 0 && hardOut()))
        {
            var dice = new Random(Mix(o.Seed, act, Hash(name), ck.Gen, 0xce));
            var samples = new List<double[]> { ck.Mean.ToArray() };
            while (samples.Count < Math.Max(4, o.Pop))
            {
                var z = Enumerable.Range(0, d).Select(_ => Gauss(dice)).ToArray();
                samples.Add([.. Enumerable.Range(0, d).Select(i => ck.Mean[i] + ck.Sd[i] * z[i])]);
                if (samples.Count < o.Pop)
                    samples.Add([.. Enumerable.Range(0, d).Select(i => ck.Mean[i] - ck.Sd[i] * z[i])]);
            }
            var fit = Evaluate(shipped, act, [.. samples.Select(s => new PlayPolicy(s))], cases, o.PerTurn, o.Jobs);
            var order = Enumerable.Range(0, samples.Count).OrderBy(i => fit[i]).ThenBy(i => i).ToList();
            if (fit[order[0]] < ck.BestFit - 1e-9)
            {
                ck.BestFit = fit[order[0]];
                ck.Best = samples[order[0]];
            }
            var elite = order.Take(Math.Max(2, o.Elite)).Select(i => samples[i]).ToList();
            for (var i = 0; i < d; i++)
            {
                var m = elite.Average(e => e[i]);
                var sd = Math.Sqrt(elite.Average(e => (e[i] - m) * (e[i] - m)));
                ck.Mean[i] = 0.3 * ck.Mean[i] + 0.7 * m;
                ck.Sd[i] = Math.Max(0.05, 0.6 * ck.Sd[i] + 0.4 * sd);
            }
            ck.Gen++;
            ck.History.Add(fit[order[0]]);
            if (checkpoint is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(checkpoint))!);
                File.WriteAllText(checkpoint + ".tmp", JsonSerializer.Serialize(ck));
                File.Move(checkpoint + ".tmp", checkpoint, overwrite: true);
            }
            say($"    {name}: gen {ck.Gen}/{gens} · mean-of-gen {fit.Average():0.00} · best of gen {fit[order[0]]:0.00} · best {ck.BestFit:0.00}");
        }
        return (new PlayPolicy(ck.Best) { Name = name }, ck.BestFit, ck.Gen);
    }

    // ── the whole training of one or more acts ──────────────────────────────────────────────────────────────

    public static int TrainPolicies(RunBlueprint game, Options o, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(o);
        var json = RunJson.CreateOptions(indented: false);
        var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(game, json), json);
        var packages = new TrialPackages(shipped);
        var themes = new DeckThemes(shipped);
        var reader = new DeckModel.Reader(shipped, themes);
        var chooser = new ModelChooser(packages, reader, DeckModel.Load(o.ModelPath), CardValues.Load(o.ValuesPath), 0);
        Directory.CreateDirectory(o.Dir);
        var allActs = new[] { 1, 2, 3, 4, 5 };
        var measured = TrialChoices.MeasuredSeeds(o.Seed, allActs, a => TrialRunner.Walk(shipped, a).Fights.Count, o.MeasuredPaths, o.MeasuredSeeds);
        var human = o.Compare is { } log && File.Exists(log) ? TrialRunner.Human(log) : null;
        foreach (var act in o.Acts)
        {
            var clock = Stopwatch.StartNew();
            var walk = TrialRunner.Walk(shipped, act);
            var fights = walk.Fights.Where(f => o.Only is not { Count: > 0 } only || only.Contains(f.Encounter)).ToList();
            if (fights.Count == 0)
            {
                say($"act {act}: no encounter to train");
                continue;
            }
            say($"act {act}: {fights.Count} encounter(s) · decks by {(chooser.HasModel ? "the draft model" : "the learned table")} · "
                + $"pop {o.Pop} · elite {o.Elite} · gens {o.Gens} (shared {o.SharedGens}) · {o.TrainSeeds} train / {o.ValidSeeds} valid seeds · "
                + $"budget {(o.Minutes > 0 ? $"{o.Minutes:0} min" : "none")}");
            var decks = Decks(shipped, walk, packages, chooser, o.Decks, o.Seed, o.Jobs);
            var train = new Dictionary<string, List<Case>>(StringComparer.Ordinal);
            var valid = new Dictionary<string, List<Case>>(StringComparer.Ordinal);
            foreach (var f in fights)
            {
                var (t, v) = Seeds(o.Seed, act, f.Index, o.TrainSeeds, o.ValidSeeds, measured);
                train[f.Encounter] = Cases(f, decks[f.Stage], t, 0x71);
                valid[f.Encounter] = Cases(f, decks[f.Stage], v, 0x72);
            }
            bool Over(double share) => o.Minutes > 0 && clock.Elapsed.TotalMinutes > o.Minutes * share;

            // 1. the act's shared policy
            var sharedName = PolicyBook.SharedName(act);
            var sharedFile = PolicyBook.PathOf(o.Dir, sharedName);
            var sharedConfig = $"shared|{o.Seed}|{o.Pop}|{o.SharedGens}|{o.SharedCases}|{o.PerTurn}|{fights.Count}" + (o.Warm ? "|warm" : "");
            var sharedRecord = PlayPolicy.Load(sharedFile);
            PlayPolicy shared;
            if (sharedRecord is { Done: true } && sharedRecord.Config == sharedConfig)
            {
                shared = PlayPolicy.FromRecord(sharedRecord);
                say($"  {sharedName}: kept ({sharedRecord.Generations} gens, valid {sharedRecord.ValidScore:0.00})");
            }
            else
            {
                var start = sharedRecord is not null && o.Warm ? PlayPolicy.FromRecord(sharedRecord) : PlayPolicy.Default;
                var cases = fights.SelectMany(f => train[f.Encounter].Take(o.SharedCases)).ToList();
                var (p, fit, gens) = Cem(shipped, act, sharedName + (o.Warm ? "-warm" : ""), start, cases, o, o.SharedGens,
                    Path.Combine(o.Dir, "ckpt", sharedName + (o.Warm ? "-warm" : "") + ".json"), () => Over(0.2), say, () => Over(1.1));
                shared = new PlayPolicy(p.Weights) { Name = sharedName };
                var vcases = fights.SelectMany(f => valid[f.Encounter].Take(o.SharedCases)).ToList();
                var v = Evaluate(shipped, act, [PlayPolicy.Default, shared], vcases, o.PerTurn, o.Jobs);
                var record = shared.ToRecord(act);
                record.Name = sharedName;
                record.Done = true;
                record.Config = sharedConfig;
                record.Generations = gens;
                record.TrainScore = fit;
                record.ValidDefaultScore = v[0];
                record.ValidScore = v[1];
                record.ValidFights = vcases.Count;
                PlayPolicy.Save(sharedFile, record);
                say($"  {sharedName}: valid {v[1]:0.00} (built-in default {v[0]:0.00}) · {clock.Elapsed.TotalMinutes:0.0} min");
            }

            // 2. per encounter, from the shared policy (or, warm, from its own)
            var lines = new List<string>();
            for (var k = 0; k < fights.Count; k++)
            {
                var f = fights[k];
                var file = PolicyBook.PathOf(o.Dir, f.Encounter);
                var config = $"enc|{o.Seed}|{o.Pop}|{o.Gens}|{o.TrainSeeds}|{o.PerTurn}" + (o.Warm ? "|warm" : "");
                var old = PlayPolicy.Load(file);
                if (old is { Done: true } && old.Config == config)
                {
                    lines.Add(Line(f, old, human?.GetValueOrDefault((act, f.Encounter))));
                    continue;
                }
                var start = o.Warm && old is not null ? PlayPolicy.FromRecord(old) : shared;
                var left = fights.Count - k;
                var share = 0.2 + 0.75 * (k + 1) / fights.Count; // the budget left, shared out over what is left
                var tag = f.Encounter + (o.Warm ? "-warm" : "");
                var (p, fit, gens) = Cem(shipped, act, tag, start, train[f.Encounter], o, o.Gens,
                    Path.Combine(o.Dir, "ckpt", tag + ".json"), () => Over(share), say, () => Over(1.1));
                var rows = new List<TrialRunner.FightRow>();
                var v = Evaluate(shipped, act, [PlayPolicy.Default, start, p], valid[f.Encounter], o.PerTurn, o.Jobs, rows);
                var learned = rows.Skip(2 * valid[f.Encounter].Count).ToList();
                var record = p.ToRecord(act);
                record.Name = f.Encounter;
                record.Done = true;
                record.Config = config;
                record.Generations = gens + (old?.Generations ?? 0) * (o.Warm ? 1 : 0);
                record.TrainScore = fit;
                record.ValidDefaultScore = v[0];
                record.ValidStartScore = v[1];
                record.ValidScore = v[2];
                record.ValidLost = learned.Average(r => r.Lost);
                record.ValidWin = learned.Average(r => r.Won ? 1.0 : 0.0);
                record.ValidRounds = learned.Average(r => r.Rounds);
                record.ValidFights = learned.Count;
                PlayPolicy.Save(file, record);
                lines.Add(Line(f, record, human?.GetValueOrDefault((act, f.Encounter))));
                say($"  {f.Encounter}: valid {v[2]:0.00} (start {v[1]:0.00}, built-in {v[0]:0.00}) · lost {record.ValidLost:0.0} · "
                    + $"{gens} gens · {clock.Elapsed.TotalMinutes:0.0} min · {left - 1} left");
            }
            var report = new StringBuilder();
            report.AppendLine($"lieutenant play policies · act {act} · {DateTime.Now:yyyy-MM-dd HH:mm} · validation seeds only (never trained on)");
            report.AppendLine("score = lost + 100 per loss + 0.1 per round, the policy alone (no rollouts); start = the act's shared policy");
            report.AppendLine($"  {"st",2} {"kind",-6} {"encounter",-40} {"built-in",8} {"shared",8} {"learned",8} {"lost",6} {"win",5} {"rnd",5} {"gens",4}  human");
            lines.ForEach(l => report.AppendLine(l));
            File.WriteAllText(Path.Combine(o.Dir, $"validation-act{act}.txt"), report.ToString());
            say(report.ToString());
        }
        return 0;
    }

    private static string Line(TrialRunner.TrialFight f, PlayPolicy.Record r, (double Lost, double Rounds, string Won)? human) =>
        $"  {f.Stage,2} {f.Kind,-6} {f.Encounter,-40} {r.ValidDefaultScore,8:0.00} {r.ValidStartScore,8:0.00} {r.ValidScore,8:0.00} "
        + $"{r.ValidLost,6:0.0} {r.ValidWin * 100,4:0}% {r.ValidRounds,5:0.0} {r.Generations,4}  "
        + (human is { } h ? $"{h.Lost:0.0} lost / {h.Rounds:0} rnd" : "—");

    // ── deck rounds: progressions played by the learned bot, one row per stage ──────────────────────────────

    public sealed record DeckOptions(
        IReadOnlyList<int> Acts, int Progressions, int Seeds, double Epsilon, int Round, string DataPath, string ModelPath,
        string PolicyDir, int PerTurn = 30, int Seed = 20261106, int Jobs = 8, string? ValuesPath = null, double Lambda = 60,
        bool Synergies = false, bool Pooled = true);

    public static int DeckRounds(RunBlueprint game, DeckOptions o, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(o);
        var json = RunJson.CreateOptions(indented: false);
        var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(game, json), json);
        var packages = new TrialPackages(shipped);
        var themes = new DeckThemes(shipped);
        var reader = new DeckModel.Reader(shipped, themes);
        var book = new PolicyBook(o.PolicyDir);
        var chooser = new ModelChooser(packages, reader, DeckModel.Load(o.ModelPath), CardValues.Load(o.ValuesPath), o.Epsilon);
        var allActs = new[] { 1, 2, 3, 4, 5 };
        var measured = TrialChoices.MeasuredSeeds(o.Seed, allActs, a => TrialRunner.Walk(shipped, a).Fights.Count, 5, 20);
        var clock = Stopwatch.StartNew();
        foreach (var act in o.Acts)
        {
            var walk = TrialRunner.Walk(shipped, act);
            var done = DeckModel.ReadRows(o.DataPath).Where(r => r.Round == o.Round && r.Act == act).Select(r => r.Path).ToHashSet();
            var todo = Enumerable.Range(0, o.Progressions).Where(p => !done.Contains(p)).ToList();
            say($"deck round {o.Round} · act {act}: {todo.Count} of {o.Progressions} progressions to play · ε {o.Epsilon} · {book.Count} policies");
            var stages = walk.Fights.GroupBy(f => f.Stage).OrderBy(g => g.Key).ToList();
            // a batch at a time, so every batch is a checkpoint
            foreach (var batch in todo.Chunk(Math.Max(1, o.Jobs)))
            {
                var rows = new ConcurrentBag<DeckModel.Row>();
                Parallel.ForEach(batch, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, o.Jobs) }, p =>
                {
                    var dice = new Random(Mix(o.Seed, o.Round, act, p, 0xd3c));
                    using var path = new TrialRunner.Path(shipped, walk, Mix(o.Seed, o.Round, act, p, 0xa7), packages,
                        (pa, d) => chooser.Choose(pa, d, walk, act, dice));
                    foreach (var stage in stages)
                    {
                        path.Advance(stage.Min(f => f.StopsBefore), new Dictionary<string, int>(), walk.Start);
                        var loadout = path.Loadout(walk.Start);
                        var scores = new List<double>();
                        foreach (var f in stage)
                            for (var n = 0; n < o.Seeds; n++)
                            {
                                var seed = Mix(o.Seed, o.Round, act, p, f.Index, n, 0xda7a);
                                while (measured.Contains(seed))
                                    seed = Mix(seed, 1);
                                var bot = new TrialBot.Options(Worlds: 0, PerTurn: o.PerTurn);
                                scores.Add(Score(TrialRunner.Fight(shipped, f, loadout, act, p, seed, Mix(seed, 0xb0), bot, book: book)));
                            }
                        rows.Add(new DeckModel.Row(o.Round, act, p, stage.Key, [.. loadout.Deck], [.. loadout.Relics], scores.Average(), scores.Count));
                    }
                });
                DeckModel.AppendRows(o.DataPath, rows.OrderBy(r => r.Path).ThenBy(r => r.Stage));
                say($"  act {act}: progressions {batch.First()}–{batch.Last()} done · {clock.Elapsed.TotalMinutes:0.0} min");
            }
        }
        var all = DeckModel.ReadRows(o.DataPath);
        var model = DeckModel.Fit(all, reader, o.Lambda, o.Synergies, o.Pooled);
        model.Save(o.ModelPath);
        var cv = DeckModel.CrossValidate(all, reader, o.Lambda, o.Synergies, o.Pooled);
        say($"draft model: {all.Count} rows · rmse {model.Rmse:0.0} · held-out rmse {cv.Model:0.00} (stage means alone {cv.StageOnly:0.00}) · {o.ModelPath} (+ .txt)");
        return 0;
    }

    // ── helpers ─────────────────────────────────────────────────────────────────────────────────────────────

    private static double Gauss(Random dice)
    {
        var u1 = 1.0 - dice.NextDouble();
        var u2 = dice.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    public static int Hash(string text)
    {
        unchecked
        {
            var h = (int)2166136261;
            foreach (var ch in text)
                h = (h ^ ch) * 16777619;
            return h & int.MaxValue;
        }
    }

    public static int Mix(params int[] parts)
    {
        unchecked
        {
            var h = (int)2166136261;
            foreach (var p in parts)
                h = (h ^ p) * 16777619 ^ (h >> 13);
            return h & int.MaxValue;
        }
    }
}

// ── THE CHEAP DRAFTER: the model's best pick (or, with probability ε, any offer — exploration for the data) ────
// Card, relic and improvement offers are scored by the draft model in the deck as it stands (DeckModel.Gain); where
// the model has not seen an id, the learned table's mean stands in. Skip / none scores 0. A removal and anything
// else stays with the static heuristic (TrialPackages).
public sealed class ModelChooser
{
    private readonly TrialPackages _packages;
    private readonly DeckModel.Reader _reader;
    private readonly DeckModel? _model;
    private readonly CardValues _values;
    private readonly double _epsilon;

    public ModelChooser(TrialPackages packages, DeckModel.Reader reader, DeckModel? model, CardValues values, double epsilon)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _model = model;
        _values = values ?? new CardValues();
        _epsilon = epsilon;
    }

    public bool HasModel => _model is not null;

    public (IReadOnlyList<int> Picks, string? Note) Choose(TrialRunner.Path path, TrialRunner.Decision decision, TrialRunner.TrialWalk walk,
        int act, Random dice)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(walk);
        ArgumentNullException.ThrowIfNull(dice);
        var request = decision.Request;
        var kind = TrialPackages.KindOf(request);
        var heuristic = _packages.Pick(request, decision.Run, decision.Played);
        if (kind is not (TrialPackages.Kind.Card or TrialPackages.Kind.Relic or TrialPackages.Kind.Improve) || request.Displays.Count == 0)
            return (heuristic, null);
        var label = kind switch { TrialPackages.Kind.Card => "card", TrialPackages.Kind.Relic => "relic", _ => "upgrade" };
        var ids = Enumerable.Range(0, request.Displays.Count).Select(i => request.ArtAt(i) is { } art ? TrialPackages.Id(art) : "").ToList();
        var options = Enumerable.Range(0, ids.Count).Select(i => (Picks: (IReadOnlyList<int>)[i], Id: ids[i])).ToList();
        if (request.AllowSkip)
            options.Add(([], ""));
        // exploration draws first, always, so the dice move the same whichever way the choice goes
        var explore = dice.NextDouble() < _epsilon;
        var randomPick = dice.Next(options.Count);
        if (explore)
            return (options[randomPick].Picks, "explore");
        var effectiveAct = act <= 0 ? walk.Act : act;
        var now = path.Loadout(walk.Start);
        var stage = walk.Fights.FirstOrDefault(f => f.StopsBefore > decision.Stop)?.Stage ?? walk.Fights.Max(f => f.Stage);
        double Value(string id)
        {
            if (id.Length == 0)
                return 0;
            if (_model is { } model && model.Knows(effectiveAct, label, id))
            {
                var after = DeckModel.After(now, label, id);
                return model.Gain(_reader, effectiveAct, stage, now.Deck, now.Relics, after.Deck, after.Relics);
            }
            return _values.Mean(label, id) ?? (label == "card" ? 0.01 * _packages.Rate(id) : 0.01);
        }
        var best = options.Select((o, i) => (o, i, V: Value(o.Id))).OrderByDescending(x => x.V).ThenBy(x => x.i).First();
        return (best.o.Picks, $"model {best.V:0.0}");
    }
}
