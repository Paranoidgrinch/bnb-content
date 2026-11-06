using System.Globalization;
using System.Text;
using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Converter.Playtest;

// ── THE DRAFT MODEL: WHAT A DECK IS WORTH ON THE STAGE IT MEETS (ridge regression, inspectable) ─────────────────
// Data: trial progressions played by the learned bot (DeckRounds), one row per (path, stage): the deck, the relics
// and the mean score of the stage's fights (health lost + 100 for a loss + 0.1 per round; lower is better).
//
//   score ≈ stage intercept                                   (act, stage: the fights themselves)
//         + Σ cards   β[act, card] · copies                  (a card's value by act; "+" is its own card)
//         + Σ relics  γ[act, relic]
//         + Σ themes  δ[tᵢ·tⱼ] · nᵢ·nⱼ / size                  (synergy over the statuses cards share: Open Guard,
//                                                              Strength, Authority …, DeckThemes)
//         + size and curve terms                              (deck size, its square, mean Energy cost, attack share)
//
// A card's VALUE is −β (health saved per copy). The model orders and prunes the package candidates (TrialChoices)
// and makes the training decks (ModelChooser); the play-out stays the final judge where it is run.
public sealed class DeckModel
{
    public sealed record Row(int Round, int Act, int Path, int Stage, string[] Deck, string[] Relics, double Score, int Fights);

    public SortedDictionary<string, double> Beta { get; set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, int> Support { get; set; } = new(StringComparer.Ordinal);
    public int Rows { get; set; }
    public double Lambda { get; set; }
    public double Rmse { get; set; }
    public double Mean { get; set; }
    public string Updated { get; set; } = "";

    // ── the features of one deck ────────────────────────────────────────────────────────────────────────────

    public sealed class Reader
    {
        private readonly DeckThemes _themes;
        private readonly Dictionary<string, (int Cost, bool Attack)> _cards;

        public Reader(RunBlueprint game, DeckThemes themes)
        {
            ArgumentNullException.ThrowIfNull(game);
            _themes = themes ?? throw new ArgumentNullException(nameof(themes));
            var rating = CardRating.PerEnergyById(game);
            _cards = new Dictionary<string, (int, bool)>(StringComparer.Ordinal);
            using var document = JsonDocument.Parse(RunJson.ToJson(game, RunJson.CreateOptions(indented: false)));
            foreach (var card in document.RootElement.GetProperty("Cards").EnumerateArray())
            {
                var id = card.GetProperty("Id").GetString()!;
                var cost = 0;
                if (card.TryGetProperty("Costs", out var costs) && costs.ValueKind == JsonValueKind.Array)
                    foreach (var c in costs.EnumerateArray())
                        if (c.TryGetProperty("ResourceId", out var resource) && resource.TryGetProperty("value", out var name)
                            && name.GetString() == StandardCombatIds.EnergyResource.value && c.TryGetProperty("Amount", out var amount)
                            && amount.ValueKind == JsonValueKind.Number)
                            cost += amount.GetInt32();
                _cards[id] = (cost, rating.TryGetValue(id, out var v) && v.Attack);
            }
        }

        public Dictionary<string, double> Features(int act, int stage, IReadOnlyList<string> deck, IReadOnlyList<string> relics)
        {
            var x = new Dictionary<string, double>(StringComparer.Ordinal) { [$"stage:a{act}s{stage}"] = 1 };
            foreach (var card in deck)
                x[$"card:a{act}:{card}"] = x.GetValueOrDefault($"card:a{act}:{card}") + 1;
            foreach (var relic in relics.Distinct(StringComparer.Ordinal))
                x[$"relic:a{act}:{relic}"] = 1;
            var size = Math.Max(1, deck.Count);
            var themes = deck.SelectMany(c => _themes.Of(c)).GroupBy(t => t, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => (double)g.Count(), StringComparer.Ordinal);
            var names = themes.Keys.Order(StringComparer.Ordinal).ToList();
            for (var i = 0; i < names.Count; i++)
                for (var j = i; j < names.Count; j++)
                    x[$"syn:{names[i]}*{names[j]}"] = themes[names[i]] * themes[names[j]] / size;
            x["size"] = size - 12;
            x["size2"] = (size - 12) * (size - 12) / 10.0;
            x["cost_mean"] = deck.Average(c => (double)_cards.GetValueOrDefault(c).Cost) - 1;
            x["attack_share"] = deck.Count(c => _cards.GetValueOrDefault(c).Attack) / (double)size - 0.4;
            return x;
        }
    }

    // ── fit ─────────────────────────────────────────────────────────────────────────────────────────────────

    // Ridge, solved exactly: (XᵀX + Λ)β = Xᵀ(y − ȳ). The stage intercepts are barely held (they are the fights
    // themselves); everything else is pulled toward 0 by `lambda`, so a card seen twice says little.
    public static DeckModel Fit(IReadOnlyList<Row> rows, Reader reader, double lambda = 3.0)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(reader);
        var xs = rows.Select(r => reader.Features(r.Act, r.Stage, r.Deck, r.Relics)).ToList();
        var names = xs.SelectMany(x => x.Keys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var index = names.Select((n, i) => (n, i)).ToDictionary(t => t.n, t => t.i, StringComparer.Ordinal);
        var d = names.Count;
        var mean = rows.Count == 0 ? 0 : rows.Average(r => r.Score);
        var a = new double[d, d];
        var b = new double[d];
        var support = new int[d];
        for (var r = 0; r < rows.Count; r++)
        {
            var w = Math.Sqrt(Math.Max(1, rows[r].Fights));
            var entries = xs[r].Select(kv => (I: index[kv.Key], V: kv.Value)).ToArray();
            foreach (var (i, v) in entries)
            {
                if (v != 0)
                    support[i]++;
                b[i] += w * v * (rows[r].Score - mean);
                foreach (var (j, u) in entries)
                    a[i, j] += w * v * u;
            }
        }
        for (var i = 0; i < d; i++)
            a[i, i] += names[i].StartsWith("stage:", StringComparison.Ordinal) ? 0.01 : lambda;
        var beta = Solve(a, b);
        var model = new DeckModel
        {
            Rows = rows.Count,
            Lambda = lambda,
            Mean = mean,
            Updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        };
        for (var i = 0; i < d; i++)
        {
            model.Beta[names[i]] = Math.Round(beta[i], 5);
            model.Support[names[i]] = support[i];
        }
        model.Rmse = rows.Count == 0 ? 0 : Math.Sqrt(rows.Select((r, k) => Math.Pow(model.Predict(xs[k]) - r.Score, 2)).Average());
        return model;
    }

    private static double[] Solve(double[,] a, double[] b)
    {
        var n = b.Length;
        var m = (double[,])a.Clone();
        var x = (double[])b.Clone();
        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var r = col + 1; r < n; r++)
                if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col]))
                    pivot = r;
            if (Math.Abs(m[pivot, col]) < 1e-12)
                continue;
            if (pivot != col)
            {
                for (var c = 0; c < n; c++)
                    (m[col, c], m[pivot, c]) = (m[pivot, c], m[col, c]);
                (x[col], x[pivot]) = (x[pivot], x[col]);
            }
            for (var r = 0; r < n; r++)
            {
                if (r == col || m[r, col] == 0)
                    continue;
                var f = m[r, col] / m[col, col];
                for (var c = col; c < n; c++)
                    m[r, c] -= f * m[col, c];
                x[r] -= f * x[col];
            }
        }
        for (var i = 0; i < n; i++)
            x[i] = Math.Abs(m[i, i]) < 1e-12 ? 0 : x[i] / m[i, i];
        return x;
    }

    public double Predict(IReadOnlyDictionary<string, double> x) =>
        Mean + x.Sum(kv => Beta.TryGetValue(kv.Key, out var beta) ? beta * kv.Value : 0);

    // How much less the stage is predicted to cost with `deckAfter`/`relicsAfter` than with the deck as it stands.
    public double Gain(Reader reader, int act, int stage, IReadOnlyList<string> deck, IReadOnlyList<string> relics,
        IReadOnlyList<string> deckAfter, IReadOnlyList<string> relicsAfter)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return Predict(reader.Features(act, stage, deck, relics)) - Predict(reader.Features(act, stage, deckAfter, relicsAfter));
    }

    // The loadout with one candidate taken: a card added, a relic added, a card improved ("upgrade").
    public static TrialRunner.Loadout After(TrialRunner.Loadout now, string kind, string id)
    {
        ArgumentNullException.ThrowIfNull(now);
        switch (kind)
        {
            case "card":
                return now with { Deck = [.. now.Deck, id] };
            case "relic":
                return now with { Relics = [.. now.Relics, id] };
            default:
            {
                var deck = now.Deck.ToList();
                var at = deck.IndexOf(id.TrimEnd('+'));
                if (at >= 0)
                    deck[at] = id.TrimEnd('+') + "+";
                return now with { Deck = deck };
            }
        }
    }

    public bool Knows(int act, string kind, string id) =>
        kind == "upgrade"
            ? Support.GetValueOrDefault($"card:a{act}:{id.TrimEnd('+')}+") > 0 || Support.GetValueOrDefault($"card:a{act}:{id.TrimEnd('+')}") > 0
            : Support.GetValueOrDefault($"{(kind == "relic" ? "relic" : "card")}:a{act}:{id}") > 0;

    // ── kept on disk ────────────────────────────────────────────────────────────────────────────────────────

    public void Save(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, overwrite: true);
        File.WriteAllText(Path.ChangeExtension(path, ".txt"), Report());
    }

    public static DeckModel? Load(string? path) =>
        path is not null && File.Exists(path) ? JsonSerializer.Deserialize<DeckModel>(File.ReadAllText(path)) : null;

    public static List<Row> ReadRows(string path) =>
        !File.Exists(path) ? [] : [.. File.ReadAllLines(path).Where(l => l.Trim().Length > 0).Select(l => JsonSerializer.Deserialize<Row>(l)!)];

    public static void AppendRows(string path, IEnumerable<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.AppendAllLines(path, rows.Select(r => JsonSerializer.Serialize(r)));
    }

    // The readable side: per act the best and worst cards and relics by value (−β, health per copy), the synergies,
    // the size and curve terms. n = rows the feature appeared in.
    public string Report(int top = 20)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"lieutenant draft model · ridge λ {Lambda} · {Rows} stage rows · rmse {Rmse:0.0} · updated {Updated}");
        sb.AppendLine("value = health a stage costs less per copy (card) or with it (relic), negative = it costs more; n = rows seen in");
        string Line(string key, string name) => $"  {name,-36} {-Beta[key],+7:+0.00;-0.00}  n {Support.GetValueOrDefault(key),5}";
        foreach (var act in Beta.Keys.Where(k => k.StartsWith("card:a", StringComparison.Ordinal) || k.StartsWith("relic:a", StringComparison.Ordinal))
                     .Select(k => k.Split(':')[1]).Distinct().Order(StringComparer.Ordinal))
        {
            foreach (var kind in new[] { "card", "relic" })
            {
                var keys = Beta.Keys.Where(k => k.StartsWith($"{kind}:{act}:", StringComparison.Ordinal) && Support.GetValueOrDefault(k) >= 3)
                    .OrderBy(k => Beta[k]).ThenBy(k => k, StringComparer.Ordinal).ToList();
                if (keys.Count == 0)
                    continue;
                sb.AppendLine($"\n── act {act[1..]} · {kind}s ({keys.Count} seen in 3+ rows) ──");
                if (keys.Count <= 2 * top)
                    keys.ForEach(k => sb.AppendLine(Line(k, k.Split(':')[2])));
                else
                {
                    sb.AppendLine("  best");
                    keys.Take(top).ToList().ForEach(k => sb.AppendLine(Line(k, k.Split(':')[2])));
                    sb.AppendLine("  worst");
                    keys.TakeLast(top).ToList().ForEach(k => sb.AppendLine(Line(k, k.Split(':')[2])));
                }
            }
        }
        var syn = Beta.Keys.Where(k => k.StartsWith("syn:", StringComparison.Ordinal) && Support.GetValueOrDefault(k) >= 5)
            .OrderBy(k => Beta[k]).ToList();
        if (syn.Count > 0)
        {
            sb.AppendLine("\n── synergies (per nᵢ·nⱼ/size; value as above) ──");
            syn.ForEach(k => sb.AppendLine(Line(k, k[4..])));
        }
        sb.AppendLine("\n── size and curve ──");
        foreach (var k in new[] { "size", "size2", "cost_mean", "attack_share" })
            if (Beta.ContainsKey(k))
                sb.AppendLine(Line(k, k));
        return sb.ToString();
    }
}
