using System.Collections.Concurrent;
using RogueDeck.Bot;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Converter.Playtest;

// ── THE ACT-I BENCH ──────────────────────────────────────────────────────────────────────────────────────────
// Balance from the fights up (user, 2026-09-27): take Act I apart into its eight stages, play every standard fight
// a stage can hand out against a stated deck, over many shuffles, with the clairvoyant planner looking five turns
// ahead — and read the HEALTH THE BEST FOUND LINE COSTS. A fresh body every fight: each number is that fight's own.
//
//   stage        the design's eight Act-I stages (Queue … Enforcement), read off the encounters' own
//                `stage_<name>` tags — and only the encounters the act's map can actually hand out
//   deck         the Bureaucrat's starter deck, plus whatever `--bench-extra` names; `--bench-cards` runs the
//                base deck once and then once per Act-I card added to it, and says what each card is worth
//   the number   per fight: won?, health lost by the planner's line. Per encounter and per stage: win rate,
//                mean / median / worst health lost.
//
// ⚠ The planner sees the draw pile, so every number here is what a player who could see the future would pay:
// a FLOOR on cost. A fight that is expensive here is expensive; one that is cheap here may still punish players.
public static class Act1Bench
{
    public static readonly string[] Stages =
        ["queue", "counter", "form", "seal", "ordinance", "delay", "appeal", "enforcement"];

    public sealed record Options(
        int Shuffles = 20, int Horizon = 5, int Beam = 16, int PerTurn = 300, int Jobs = 0,
        IReadOnlyList<string>? StagesWanted = null, IReadOnlyList<string>? Extra = null, bool Cards = false,
        int Health = 70);

    public sealed record Fight(string Stage, string Encounter, int Shuffle, PlannedFight Result);

    public static int Run(RunBlueprint game, Options options, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        var starter = game.ResolveStart().Deck.Select(c => c.value).ToList();
        var baseDeck = starter.Concat(options.Extra ?? []).ToList();
        var stages = (options.StagesWanted is { Count: > 0 } wanted ? wanted : Stages).ToList();
        var byStage = StageEncounters(game, stages);

        say($"act-1 bench · deck {baseDeck.Count} ({string.Join(", ", Tally(baseDeck))}) · {options.Health} HP · "
            + $"{options.Shuffles} shuffles · horizon {options.Horizon} · beam {options.Beam}");
        foreach (var (stage, encounters) in byStage)
            say($"  stage {stage}: {string.Join(", ", encounters)}");

        var baseline = Bench(game, byStage, baseDeck, options);
        Report(baseline, say);

        if (!options.Cards)
            return 0;

        // WHAT EACH CARD IS WORTH HERE: the same fights, the same shuffles, the deck with one Act-I card added.
        // Paired by (encounter, shuffle), so the difference is the card's and not the dice's.
        var candidates = Cards.FinalCards.RewardPool(1).Select(c => c.Id).ToList();
        say($"\ncard values over stages {string.Join(", ", stages)} — {candidates.Count} Act-I cards, each added once to the base deck");
        var baseKey = baseline.ToDictionary(f => (f.Encounter, f.Shuffle), f => f.Result);
        var rows = new List<(string Card, double Delta, int WinsDelta)>();
        foreach (var card in candidates)
        {
            var with = Bench(game, byStage, [.. baseDeck, card], options with { Jobs = options.Jobs });
            var delta = with.Average(f => (double)(baseKey[(f.Encounter, f.Shuffle)].HealthLost - f.Result.HealthLost));
            var wins = with.Count(f => f.Result.Won) - baseline.Count(f => f.Result.Won);
            rows.Add((card, delta, wins));
            say($"  {card,-28} health saved per fight {delta,6:+0.0;-0.0}   wins {wins,+3:+0;-0;0}");
        }
        say("\nranked:");
        foreach (var (card, delta, wins) in rows.OrderByDescending(r => r.WinsDelta).ThenByDescending(r => r.Delta))
            say($"  {delta,6:+0.0;-0.0}  {wins,+3:+0;-0;0}  {card}");
        return 0;
    }

    // Every (stage, encounter, shuffle) fought once, in parallel. Each fight is its own playback — nothing shared
    // but the immutable blueprint.
    private static List<Fight> Bench(
        RunBlueprint game, IReadOnlyList<(string Stage, List<string> Encounters)> byStage,
        IReadOnlyList<string> deck, Options options)
    {
        var jobs = byStage
            .SelectMany(s => s.Encounters.SelectMany(e => Enumerable.Range(1, options.Shuffles).Select(k => (s.Stage, e, k))))
            .ToList();
        var results = new ConcurrentBag<Fight>();
        Parallel.ForEach(jobs,
            new ParallelOptions { MaxDegreeOfParallelism = options.Jobs > 0 ? options.Jobs : Environment.ProcessorCount },
            job =>
            {
                var encounter = game.Encounters.First(e => e.Id.Value == job.e);
                var ring = SparringRing.OneFight(game, encounter, deck, maxHealth: options.Health);
                using var play = new RunPlayback(() => { });
                play.Start(ring, job.k, interactive: true);
                while (play.Session is { IsAwaitingInterlude: true } session)
                    session.Continue();
                if (play.CombatDriver?.Current is not { } combat)
                    return;
                var planner = new FightPlanner(options.Horizon, options.Beam, options.PerTurn);
                results.Add(new Fight(job.Stage, job.e, job.k, planner.Play(combat)));
            });
        return [.. results];
    }

    private static void Report(IReadOnlyList<Fight> fights, Action<string> say)
    {
        say("\n stage / encounter                         won    mean  median  worst   turns   sec/fight");
        foreach (var stage in fights.GroupBy(f => f.Stage).OrderBy(g => Array.IndexOf(Stages, g.Key)))
        {
            foreach (var encounter in stage.GroupBy(f => f.Encounter).OrderBy(g => g.Key, StringComparer.Ordinal))
                say(Line($"   {encounter.Key}", [.. encounter]));
            say(Line($" {stage.Key.ToUpperInvariant()}", [.. stage]));
        }
        say(Line(" ALL", [.. fights]));
    }

    private static string Line(string name, IReadOnlyList<Fight> fights)
    {
        var lost = fights.Select(f => f.Result.HealthLost).OrderBy(x => x).ToList();
        return $"{name,-42} {fights.Count(f => f.Result.Won),3}/{fights.Count,-3} {lost.Average(),6:0.0} "
            + $"{lost[lost.Count / 2],6} {lost[^1],6}   {fights.Average(f => f.Result.Turns),5:0.0}   "
            + $"{fights.Average(f => f.Result.Seconds),6:0.00}";
    }

    // The encounters a stage can hand out: tagged `stage_<name>`, and in the act's own Combat or MultiCombat pool.
    private static List<(string Stage, List<string> Encounters)> StageEncounters(RunBlueprint game, IReadOnlyList<string> stages)
    {
        var spec = game.Acts![0].MapGeneration!;
        var pool = spec.Encounters.For(MapNodeKind.Combat).Concat(spec.Encounters.For(MapNodeKind.MultiCombat))
            .Select(e => e.Encounter.Value).ToHashSet(StringComparer.Ordinal);
        return [.. stages.Select(stage => (stage, game.Encounters
            .Where(e => pool.Contains(e.Id.Value)
                && (game.Presentation.Encounters.GetValueOrDefault(e.Id.Value)?.Tags ?? []).Contains($"stage_{stage}"))
            .Select(e => e.Id.Value).OrderBy(id => id, StringComparer.Ordinal).ToList()))];
    }

    private static IEnumerable<string> Tally(IEnumerable<string> ids) =>
        ids.GroupBy(id => id).Select(g => g.Count() > 1 ? $"{g.Key} ×{g.Count()}" : g.Key);
}
