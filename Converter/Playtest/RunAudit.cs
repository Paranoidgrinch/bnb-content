using System.Text.RegularExpressions;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Converter.Playtest;

// THE RELIC AUDIT THROUGH THE RUN LOG (user, 2026-11-19: "viele der events und relikte tun noch dinge die sie
// eigentlich nicht tun sollten"). One walk per relic, the relic worn from the first room, the engine's RunDiagnostics
// writing everything the run did: which events the relic answered, what each of its effects changed, what the shops
// and rewards offered. Then one summary line block per relic, to be read against the relic's text.
//
//   --run-audit <dir> [--audit-relics a,b,c] [--audit-acts 2] [--character lieutenant] [--jobs n] [--seed s]
//
// <dir>/<relic>.log is the full run log of that relic's walk; <dir>/SUMMARY.md the digest. A relic that answers no
// run event at all is a combat rule, a shop rule or a reward rule — its trace is in the shops and offers, or in the
// fight logs, and the summary says which of those it changed compared with a walk without it (same seed).
public static class RunAudit
{
    public static int Run(
        RunBlueprint shipped, string outDir, IReadOnlyList<string>? only, int acts, string character, int seed, int jobs)
    {
        Directory.CreateDirectory(outDir);
        var tester = RunWalker.WithHealth(shipped, 99999);
        var relics = (only is { Count: > 0 } ? only : shipped.Relics.Select(r => r.Id))
            .Where(id => !id.StartsWith("mod_", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var texts = shipped.Relics.ToDictionary(r => r.Id,
            r => $"{r.DisplayName}: {shipped.Presentation.Relics.GetValueOrDefault(r.Id)?.FlavorText}", StringComparer.Ordinal);
        Console.WriteLine($"run audit: {relics.Count} relic(s), acts 1-{acts}, as {character}, seed {seed} -> {outDir}");

        // The walk without any added relic: what a shop and a reward look like on this seed by default.
        var baseline = WalkWith(tester, null, Path.Combine(outDir, "_baseline.log"), acts, character, seed);
        var summaries = new string[relics.Count];
        Parallel.For(0, relics.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, jobs) }, i =>
        {
            var id = relics[i];
            var log = WalkWith(tester, id, Path.Combine(outDir, id + ".log"), acts, character, seed);
            summaries[i] = Summarize(id, texts.GetValueOrDefault(id, ""), log, baseline);
            Console.WriteLine($"  {i + 1}/{relics.Count} {id}");
        });

        var page = new System.Text.StringBuilder();
        page.AppendLine($"# Run audit — {DateTime.Now:yyyy-MM-dd HH:mm}, acts 1-{acts}, {character}, seed {seed}");
        page.AppendLine();
        page.AppendLine("Every relic worn from the first room on one immortal walk. \"reacts\" counts the run events it");
        page.AppendLine("answered; \"did\" lists its effects with the measured change; \"never changed anything\" means");
        page.AppendLine("it answered but its effects changed nothing. Full logs beside this file.");
        page.AppendLine();
        foreach (var summary in summaries)
            page.AppendLine(summary);
        File.WriteAllText(Path.Combine(outDir, "SUMMARY.md"), page.ToString());
        Console.WriteLine($"run audit: summary in {Path.Combine(outDir, "SUMMARY.md")}");
        return 0;
    }

    private static List<string> WalkWith(RunBlueprint tester, string? relic, string path, int acts, string character, int seed)
    {
        var blueprint = relic is null ? tester : WithRelic(tester, relic);
        var lines = new List<string>();
        var diagnostics = new RunDiagnostics(lines.Add);
        var report = RunWalker.Walk(blueprint, seed, characterId: character, mapGenerator: MapGenerators.Strategic,
            attach: play => diagnostics.Attach(play, $"audit walk · relic {relic ?? "(none)"} · seed {seed}"),
            maxActs: acts);
        diagnostics.Detach();
        if (report.Error is { } error)
            lines.Add($"=== WALK ERROR: {error}");
        foreach (var note in report.Notes)
            lines.Add($"=== WALK NOTE: {note}");
        File.WriteAllLines(path, lines);
        return lines;
    }

    private static RunBlueprint WithRelic(RunBlueprint blueprint, string relic)
    {
        RunStart Add(RunStart start) => start with { StartingRelics = [.. start.StartingRelics, relic] };
        return blueprint with
        {
            Start = Add(blueprint.Start),
            Characters = [.. blueprint.Characters.Select(c => c with { Start = Add(c.Start) })],
        };
    }

    private static string Summarize(string id, string text, List<string> log, List<string> baseline)
    {
        var me = $"relic {id}";
        // A combat relic is carried into each fight by an opening the engine installs on every room entered: that is
        // the relic's normal way in, not a reaction worth reading, so it is counted apart.
        const string CombatInstall = "InstallNextCombatOpening";
        var installs = log.Count(l => l.TrimStart().StartsWith($"[{me}] {CombatInstall}", StringComparison.Ordinal));
        var reacts = log.Select(l => Regex.Match(l, $@"^\s+{Regex.Escape(me)} reacts to (\w+): (.*)$"))
            .Where(m => m.Success && m.Groups[2].Value != CombatInstall).GroupBy(m => m.Groups[1].Value)
            .Select(g => $"{g.Key} x{g.Count()}").ToList();
        var did = log.Where(l => l.TrimStart().StartsWith($"[{me}]", StringComparison.Ordinal))
            .Select(l => l.Trim()[(me.Length + 3)..])
            .Where(l => !l.StartsWith(CombatInstall, StringComparison.Ordinal))
            .ToList();
        var changed = did.Where(l => !l.EndsWith("-> no change", StringComparison.Ordinal)).ToList();

        var block = new System.Text.StringBuilder();
        block.AppendLine($"## {id}");
        block.AppendLine($"text: {text.ReplaceLineEndings(" ")}");
        if (log.FirstOrDefault(l => l.StartsWith("=== WALK ERROR", StringComparison.Ordinal)) is { } error)
            block.AppendLine($"**{error}**");
        if (installs > 0)
            block.AppendLine($"combat rule carried into {installs} room(s) (its effect is in the fight logs)");
        if (log.FirstOrDefault(l => l.StartsWith("=== WALK NOTE", StringComparison.Ordinal)) is { } note)
            block.AppendLine($"walk cut short: {note[14..]}");
        if (reacts.Count == 0)
            block.AppendLine("reacts: nothing in the run layer");
        else
        {
            block.AppendLine($"reacts: {string.Join(", ", reacts)}");
            block.AppendLine(changed.Count == 0
                ? "did: never changed anything"
                : $"did ({changed.Count}x with a change, {did.Count - changed.Count}x without):");
            foreach (var sample in changed.GroupBy(Shape).Take(6))
                block.AppendLine($"  - {sample.First()}{(sample.Count() > 1 ? $"  (x{sample.Count()})" : "")}");
        }

        // What the relic changed about the shops and offers, compared with the walk without it.
        var shop = Distinct(log, "    service ").Except(Distinct(baseline, "    service ")).ToList();
        if (shop.Count > 0)
            block.AppendLine($"shop services new with it: {string.Join(" | ", shop.Take(4))}");
        var picks = PickSizes(log);
        var basePicks = PickSizes(baseline);
        if (picks != basePicks)
            block.AppendLine($"card offers: {picks} (without it: {basePicks})");
        var prices = ShopPriceAverage(log);
        var basePrices = ShopPriceAverage(baseline);
        if (prices is { } p && basePrices is { } b && Math.Abs(p - b) > 0.5)
            block.AppendLine($"shop prices average {p:0} (without it: {b:0})");
        return block.ToString();
    }

    // Two effect lines are the same shape when they differ only in numbers and card names.
    private static string Shape(string line) =>
        Regex.Replace(Regex.Replace(line, @"\d+", "#"), @"deck: \S+ upgraded to \S+", "deck: X upgraded");

    private static HashSet<string> Distinct(List<string> log, string prefix) =>
        log.Where(l => l.StartsWith(prefix, StringComparison.Ordinal))
            .Select(l => Regex.Replace(l.Trim(), @" \[used\]", ""))
            .ToHashSet(StringComparer.Ordinal);

    private static string PickSizes(List<string> log)
    {
        var sizes = new List<int>();
        for (var i = 0; i < log.Count; i++)
            if (log[i].Contains("asked: pick", StringComparison.Ordinal) && log[i].Contains("reward-card", StringComparison.Ordinal))
            {
                var n = 0;
                while (i + 1 + n < log.Count && log[i + 1 + n].TrimStart().StartsWith('['))
                    n++;
                sizes.Add(n);
            }
        return sizes.Count == 0 ? "none" : string.Join(",", sizes.GroupBy(s => s).OrderBy(g => g.Key).Select(g => $"{g.Count()}x{g.Key}"));
    }

    private static double? ShopPriceAverage(List<string> log)
    {
        var prices = log.Select(l => Regex.Match(l, @"^    shelf \S+ .* for (\d+) gold$"))
            .Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        return prices.Count == 0 ? null : prices.Average();
    }
}
