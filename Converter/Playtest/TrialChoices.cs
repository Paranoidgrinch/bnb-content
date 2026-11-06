using System.Globalization;
using System.Text;
using System.Text.Json;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Converter.Playtest;

// ── THE TRIAL RUNNER'S PACKAGE CHOICES, BY PLAY ────────────────────────────────────────────────────────────────
// The static rating scores a status that is not a keyword as 0, so most Lieutenant cards rate 0 and a choice by
// rating skips most offers. So a choice is made by playing with it:
//
//   candidates    a card offer: every offered card, and skip. A relic offer: every relic, and none. An
//                 improvement: the top few by the old heuristic (TrialPackages). A removal stays heuristic.
//   a candidate   its deck is built EXACTLY: a fresh path on the same seed replays every answer so far plus this
//                 one and stops at the next question (relics that change the body, cards a pick brings along —
//                 all as the run really does it). "None" is the deck as it stands.
//   evaluation    each candidate deck plays the fights of the next stage the walk meets (the act's first stage for
//                 a catch-up stop, the bosses after the last stop), `Seeds` times each, with a cheap setting of the
//                 same search bot. The seeds are the EVALUATION's own (EvalSeed): never a seed a measured fight
//                 uses, so no measured fight's luck leaks into a choice. Every candidate meets the same seeds.
//   score         mean over those fights of health lost + a large penalty for a loss + a little per round.
//   choice        the best score; within `TieBand` of it, the learned table breaks the tie, then the offer order.
//
// LEARNING. For each candidate played, gain = score(none) − score(candidate): how much the deck with it beat the
// deck without it. It is recorded per card / relic / improvement id, with a light context (the act and the two
// statuses the deck leans on most), in a table kept across runs (CardValues). The table is a PRIOR only: it orders
// the candidates (with `Top` below all of them, only the best few by prior are played) and breaks ties; it never
// replaces the play-out of a candidate that is played.
public sealed class TrialChoices
{
    // Book: the learned play policies the evaluation's bot plays with. Model: the fitted draft model (DeckModel); with
    // it, candidates are ordered by what it predicts they gain in this deck, and only the best `Top` are played.
    public sealed record Options(TrialBot.Options Bot, int Seeds = 3, int Top = 99, int ImproveTop = 3,
        double LossPenalty = 100, double PerRound = 0.1, double TieBand = 0.5,
        [property: System.Text.Json.Serialization.JsonIgnore] PolicyBook? Book = null,
        [property: System.Text.Json.Serialization.JsonIgnore] DeckModel? Model = null);

    // The same cheap setting by default: one world, a beam of two, a short turn search.
    public static TrialBot.Options CheapBot { get; } = new(Worlds: 1, Horizon: 3, Beam: 2, PerTurn: 30);

    public sealed record Learned(string Kind, string Id, int Act, string Context, double Gain, int Fights);

    private readonly RunBlueprint _shipped;
    private readonly TrialRunner.TrialWalk _walk;
    private readonly TrialPackages _packages;
    private readonly CardValues _prior;
    private readonly Options _options;
    private readonly int _baseSeed;
    private readonly IReadOnlySet<int> _measured;
    private readonly ParallelOptions _parallel;
    private readonly DeckThemes _themes;

    public TrialChoices(RunBlueprint shipped, TrialRunner.TrialWalk walk, TrialPackages packages, CardValues prior,
        Options options, int baseSeed, IReadOnlySet<int> measured, ParallelOptions parallel, DeckThemes themes)
    {
        _shipped = shipped ?? throw new ArgumentNullException(nameof(shipped));
        _walk = walk ?? throw new ArgumentNullException(nameof(walk));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _prior = prior ?? throw new ArgumentNullException(nameof(prior));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _baseSeed = baseSeed;
        _measured = measured ?? throw new ArgumentNullException(nameof(measured));
        _parallel = parallel ?? throw new ArgumentNullException(nameof(parallel));
        _themes = themes ?? throw new ArgumentNullException(nameof(themes));
        _reader = options.Model is null ? null : new DeckModel.Reader(shipped, themes);
    }

    private readonly DeckModel.Reader? _reader;

    // What one path learned, in the order it learned it. Read once the path is done (one path, one thread).
    public Dictionary<int, List<Learned>> LearnedBy { get; } = [];

    public int EvalFights;
    public int Decisions;

    // The fights a choice at `stop` is judged on: the stage of the first fight after it; past the last fight, the
    // last stage (the bosses).
    public IReadOnlyList<TrialRunner.TrialFight> NextFights(int stop)
    {
        var next = _walk.Fights.FirstOrDefault(f => f.StopsBefore > stop);
        var stage = next?.Stage ?? _walk.Fights.Max(f => f.Stage);
        return [.. _walk.Fights.Where(f => f.Stage == stage)];
    }

    // A candidate: what to answer (empty = none), the id it is learned under, whether it may be chosen.
    private sealed record Candidate(IReadOnlyList<int> Picks, string Kind, string Id, bool Choosable, double Prior);

    public (IReadOnlyList<int> Picks, string? Note) Choose(TrialRunner.Path path, TrialRunner.Decision decision, int act, int pathIndex)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(decision);
        var request = decision.Request;
        var kind = TrialPackages.KindOf(request);
        var heuristic = _packages.Pick(request, decision.Run, decision.Played);
        if (kind is not (TrialPackages.Kind.Card or TrialPackages.Kind.Relic or TrialPackages.Kind.Improve) || request.Displays.Count == 0)
            return (heuristic, null);

        var ids = Enumerable.Range(0, request.Displays.Count).Select(i => request.ArtAt(i) is { } art ? TrialPackages.Id(art) : "").ToList();
        var label = kind switch { TrialPackages.Kind.Card => "card", TrialPackages.Kind.Relic => "relic", _ => "upgrade" };
        var now = path.Loadout(_walk.Start);
        var stage = NextFights(decision.Stop)[0].Stage;
        double Prior(string kind, string id) => this.Prior(kind, id, now, act, stage);
        var offered = new List<Candidate>();
        if (kind == TrialPackages.Kind.Improve)
        {
            // The top few by the old heuristic (rating gain, then the most-played attack), one per card.
            foreach (var i in _packages.ImproveOrder(request, decision.Played))
                if (offered.All(c => c.Id != ids[i]) && offered.Count < _options.ImproveTop)
                    offered.Add(new Candidate([i], label, ids[i], true, Prior(label, ids[i])));
        }
        else
            foreach (var i in Enumerable.Range(0, ids.Count))
                if (offered.All(c => c.Id != ids[i]))
                    offered.Add(new Candidate([i], label, ids[i], true, Prior(label, ids[i])));
        // The prior orders the candidates; with a short budget only the best few by it are played.
        var played = offered.OrderByDescending(c => c.Prior).ThenBy(c => c.Picks[0]).Take(Math.Max(1, _options.Top)).ToList();
        var none = new Candidate([], label, "", request.AllowSkip, 0);
        var all = new List<Candidate>(played) { none };

        var loadouts = played.Select(c => Probe(path, decision, c.Picks)).ToList();
        var baseline = request.AllowSkip ? Probe(path, decision, []) : Without(loadouts[0], label, played[0].Id);
        loadouts.Add(baseline);
        var fights = NextFights(decision.Stop);
        var mean = Scores(loadouts, fights, act, pathIndex, decision.Index);
        Interlocked.Increment(ref Decisions);
        if (!all.Any(c => c.Choosable))
            return (heuristic, null);
        var pick = Best(mean, [.. all.Select(c => c.Choosable)], [.. all.Select(c => c.Prior)], _options.TieBand);

        var context = _themes.Context(act, baseline.Deck);
        var noneScore = mean[^1];
        var learned = LearnedBy.TryGetValue(decision.Index, out var list) ? list : LearnedBy[decision.Index] = [];
        for (var c = 0; c < all.Count - 1; c++)
            learned.Add(new Learned(all[c].Kind, all[c].Id, act, context, noneScore - mean[c], fights.Count * _options.Seeds));
        var note = "play " + string.Join(", ", Enumerable.Range(0, all.Count)
            .Select(c => $"{(all[c].Picks.Count == 0 ? "none" : all[c].Id)} {mean[c].ToString("0.0", CultureInfo.InvariantCulture)}"))
            + $" on stage {fights[0].Stage} ×{_options.Seeds} · {context}";
        return (all[pick].Picks, note);
    }

    // Each loadout's mean score over `fights`, every fight `Seeds` times on the evaluation's own seeds — the same
    // seeds for every loadout, so they are compared on equal luck.
    public IReadOnlyList<double> Scores(IReadOnlyList<TrialRunner.Loadout> loadouts, IReadOnlyList<TrialRunner.TrialFight> fights,
        int act, int pathIndex, int decision)
    {
        ArgumentNullException.ThrowIfNull(loadouts);
        ArgumentNullException.ThrowIfNull(fights);
        var jobs = (from c in Enumerable.Range(0, loadouts.Count) from f in fights from n in Enumerable.Range(0, _options.Seeds)
                    select (C: c, Fight: f, N: n)).ToList();
        var scores = new double[jobs.Count];
        Parallel.For(0, jobs.Count, _parallel, j =>
        {
            var (c, fight, n) = jobs[j];
            var row = TrialRunner.Fight(_shipped, fight, loadouts[c], act, pathIndex,
                EvalSeed(_baseSeed, act, pathIndex, decision, fight.Index, n, _measured),
                EvalBotSeed(_baseSeed, act, pathIndex, decision, fight.Index, n), _options.Bot, book: _options.Book);
            scores[j] = row.Lost + (row.Won ? 0 : _options.LossPenalty) + _options.PerRound * row.Rounds;
        });
        Interlocked.Add(ref EvalFights, jobs.Count);
        return [.. Enumerable.Range(0, loadouts.Count).Select(c => Enumerable.Range(0, jobs.Count).Where(j => jobs[j].C == c).Average(j => scores[j]))];
    }

    // The best score among the choosable; within `tieBand` of it the higher prior, then the lower score, then the
    // earlier candidate.
    public static int Best(IReadOnlyList<double> scores, IReadOnlyList<bool> choosable, IReadOnlyList<double> prior, double tieBand)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(choosable);
        ArgumentNullException.ThrowIfNull(prior);
        var open = Enumerable.Range(0, scores.Count).Where(c => choosable[c]).ToList();
        var best = open.Min(c => scores[c]);
        return open.Where(c => scores[c] <= best + tieBand).OrderByDescending(c => prior[c]).ThenBy(c => scores[c]).ThenBy(c => c).First();
    }

    // What a candidate is expected to gain: the draft model's prediction in this deck where it knows the id, else
    // the learned table's mean, else a hundredth of the static rating.
    private double Prior(string kind, string id, TrialRunner.Loadout now, int act, int stage)
    {
        if (_options.Model is { } model && _reader is not null && model.Knows(act, kind, id))
        {
            var after = DeckModel.After(now, kind, id);
            return model.Gain(_reader, act, stage, now.Deck, now.Relics, after.Deck, after.Relics);
        }
        return _prior.Mean(kind, id) ?? (kind == "card" ? 0.01 * _packages.Rate(id) : 0);
    }

    // The deck a candidate answer leaves: the path replayed on its seed with every answer so far plus this one, to
    // the end of the stop (⚠ a stop's picks reach the deck only when the stop closes). The stop's later questions
    // get the same filler answer for every candidate: none where that is allowed, else the static heuristic.
    private TrialRunner.Loadout Probe(TrialRunner.Path path, TrialRunner.Decision decision, IReadOnlyList<int> picks)
    {
        using var probe = new TrialRunner.Path(_shipped, _walk, path.Seed, _packages,
            (_, d) => (d.Request.AllowSkip ? [] : _packages.Pick(d.Request, d.Run, d.Played), null),
            script: [.. path.Answers, picks]);
        probe.Advance(decision.Stop + 1, decision.Played, _walk.Start);
        return probe.Loadout(_walk.Start);
    }

    // "None" where the run does not allow it: the first candidate's deck with that candidate taken back out (an
    // improvement un-improved, a relic removed — what a relic did to the body on pickup stays).
    private static TrialRunner.Loadout Without(TrialRunner.Loadout with, string kind, string id)
    {
        if (kind == "upgrade")
        {
            var deck = with.Deck.ToList();
            var at = deck.IndexOf(id + "+");
            if (at >= 0)
                deck[at] = id;
            return with with { Deck = deck };
        }
        var relics = with.Relics.ToList();
        relics.Remove(id);
        return with with { Relics = relics };
    }

    // ── the evaluation's own seeds ───────────────────────────────────────────────────────────────────────────
    // A salt of their own, and if one ever falls on a measured fight's seed it is drawn again: no evaluation fight
    // ever plays a seed a measured fight plays.
    public static int EvalSeed(int baseSeed, int act, int path, int decision, int fight, int n, IReadOnlySet<int> measured)
    {
        ArgumentNullException.ThrowIfNull(measured);
        var salt = 0xe7a1;
        int seed;
        do
            seed = Mix(baseSeed, act, path, decision, fight, n, salt++);
        while (measured.Contains(seed));
        return seed;
    }

    public static int EvalBotSeed(int baseSeed, int act, int path, int decision, int fight, int n) =>
        Mix(baseSeed, act, path, decision, fight, n, 0xeb07);

    // Every real seed the run's measured fights use.
    public static HashSet<int> MeasuredSeeds(int baseSeed, IEnumerable<int> acts, Func<int, int> fightsInAct, int paths, int seeds)
    {
        ArgumentNullException.ThrowIfNull(acts);
        ArgumentNullException.ThrowIfNull(fightsInAct);
        var set = new HashSet<int>();
        foreach (var act in acts)
            for (var f = 0; f < fightsInAct(act); f++)
                for (var p = 0; p < paths; p++)
                    for (var n = 0; n < seeds; n++)
                        set.Add(TrialRunner.FightSeed(baseSeed, act, f, p, n));
        return set;
    }

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
}

// ── A DECK'S LEANING: THE STATUSES ITS CARDS WORK WITH ─────────────────────────────────────────────────────────
// A "theme" is a status that many cards in the document name (Strength, Open Guard, Authority, Brevet …); a card's
// own counters (named by one or two cards) are not. A deck's context is its two most-named themes.
public sealed class DeckThemes
{
    private readonly Dictionary<string, string[]> _byCard;

    public DeckThemes(RunBlueprint game, int minCards = 8)
    {
        ArgumentNullException.ThrowIfNull(game);
        using var document = JsonDocument.Parse(RunJson.ToJson(game, RunJson.CreateOptions(indented: false)));
        var named = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var card in document.RootElement.GetProperty("Cards").EnumerateArray())
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (card.TryGetProperty("Program", out var program))
                Statuses(program, set);
            named[card.GetProperty("Id").GetString()!] = set;
        }
        var counts = named.Values.SelectMany(s => s).GroupBy(s => s, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        _byCard = named.ToDictionary(kv => kv.Key, kv => kv.Value.Where(s => counts[s] >= minCards).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
    }

    private static void Statuses(JsonElement node, HashSet<string> into)
    {
        if (node.ValueKind == JsonValueKind.Array)
            foreach (var child in node.EnumerateArray())
                Statuses(child, into);
        else if (node.ValueKind == JsonValueKind.Object)
            foreach (var p in node.EnumerateObject())
                if (p.Name == "StatusDefinitionId" && p.Value.ValueKind == JsonValueKind.Object && p.Value.TryGetProperty("value", out var v))
                    into.Add(v.GetString()!);
                else
                    Statuses(p.Value, into);
    }

    // The themes one card works with (an improved copy reads as its base where it has no entry of its own).
    public IReadOnlyList<string> Of(string card) =>
        _byCard.GetValueOrDefault(card) ?? _byCard.GetValueOrDefault(card.TrimEnd('+')) ?? [];

    public string Context(int act, IEnumerable<string> deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        var top = deck.SelectMany(c => _byCard.GetValueOrDefault(c) ?? _byCard.GetValueOrDefault(c.TrimEnd('+')) ?? [])
            .GroupBy(s => s, StringComparer.Ordinal).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(2).Select(g => g.Key.StartsWith("lt_", StringComparison.Ordinal) ? g.Key[3..] : g.Key).ToList();
        return $"a{act}|{(top.Count == 0 ? "plain" : string.Join("+", top))}";
    }
}

// ── WHAT THE BOT HAS LEARNED ACROSS RUNS (~/Desktop/bnb-balance/bots/lt-card-values.json) ──────────────────────
// Per "<kind>:<id>" (card, relic, upgrade): how many play-outs, the summed gain over "none" and its square, and
// the same per context. A gain is in the score's units: health points, a loss costing the penalty on top.
public sealed class CardValues
{
    public sealed class Stat
    {
        public int N { get; set; }
        public double Sum { get; set; }
        public double SumSq { get; set; }
        public double Mean => N == 0 ? 0 : Sum / N;
        public double Sd => N < 2 ? 0 : Math.Sqrt(Math.Max(0, (SumSq - Sum * Sum / N) / (N - 1)));

        public void Add(double gain)
        {
            N++;
            Sum += gain;
            SumSq += gain * gain;
        }
    }

    public sealed class Entry
    {
        public Stat All { get; set; } = new();
        public SortedDictionary<string, Stat> Contexts { get; set; } = new(StringComparer.Ordinal);
    }

    public int Version { get; set; } = 1;
    public int Runs { get; set; }
    public SortedDictionary<string, Entry> Entries { get; set; } = new(StringComparer.Ordinal);

    public static string Key(string kind, string id) => $"{kind}:{id}";

    public double? Mean(string kind, string id) =>
        Entries.TryGetValue(Key(kind, id), out var e) && e.All.N > 0 ? e.All.Mean : null;

    public void Add(TrialChoices.Learned learned)
    {
        ArgumentNullException.ThrowIfNull(learned);
        var key = Key(learned.Kind, learned.Id);
        if (!Entries.TryGetValue(key, out var entry))
            Entries[key] = entry = new Entry();
        entry.All.Add(learned.Gain);
        if (!entry.Contexts.TryGetValue(learned.Context, out var stat))
            entry.Contexts[learned.Context] = stat = new Stat();
        stat.Add(learned.Gain);
    }

    public CardValues Copy() => Read(JsonSerializer.Serialize(this));

    private static CardValues Read(string json) => JsonSerializer.Deserialize<CardValues>(json) ?? new CardValues();

    public static CardValues Load(string? path) =>
        path is not null && File.Exists(path) ? Read(File.ReadAllText(path)) : new CardValues();

    public void Save(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, overwrite: true);
        File.WriteAllText(System.IO.Path.ChangeExtension(path, ".txt"), Report());
    }

    // The readable side: the best and worst by learned value, per kind, with how often each was played.
    public string Report(int top = 25)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"lieutenant trial runner · learned values · {Runs} run(s) · updated {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine("gain = how much less a stage cost with it than without it, by play-out (health lost, a loss counts");
        sb.AppendLine("everything plus 100, rounds a tenth each); n = play-outs; ± = standard error; best fit = the context with the");
        sb.AppendLine("highest mean among those with at least 2 play-outs (act | the two statuses the deck leaned on most)");
        foreach (var kind in new[] { "card", "relic", "upgrade" })
        {
            var rows = Entries.Where(e => e.Key.StartsWith(kind + ":", StringComparison.Ordinal))
                .Select(e => (Id: e.Key[(kind.Length + 1)..], e.Value)).ToList();
            if (rows.Count == 0)
                continue;
            string Line((string Id, Entry Value) r)
            {
                var s = r.Value.All;
                var fit = r.Value.Contexts.Where(c => c.Value.N >= 2).OrderByDescending(c => c.Value.Mean).ThenBy(c => c.Key, StringComparer.Ordinal)
                    .Select(c => $"{c.Key} {c.Value.Mean:+0.0;-0.0} (n {c.Value.N})").FirstOrDefault() ?? "—";
                var se = s.N < 2 ? 0 : s.Sd / Math.Sqrt(s.N);
                return $"  {r.Id,-34} {s.Mean,7:+0.0;-0.0} ±{se,5:0.0}  n {s.N,4}   best fit {fit}";
            }
            var ranked = rows.OrderByDescending(r => r.Value.All.Mean).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
            sb.AppendLine($"\n── {kind}s: {rows.Count} ──");
            if (ranked.Count <= 2 * top)
            {
                ranked.ForEach(r => sb.AppendLine(Line(r)));
                continue;
            }
            sb.AppendLine("  best");
            ranked.Take(top).ToList().ForEach(r => sb.AppendLine(Line(r)));
            sb.AppendLine("  worst");
            ranked.TakeLast(top).ToList().ForEach(r => sb.AppendLine(Line(r)));
        }
        return sb.ToString();
    }
}
