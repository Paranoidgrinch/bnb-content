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
        int Health = 70, IReadOnlyList<string>? Only = null);

    public sealed record Fight(string Stage, string Encounter, int Shuffle, PlannedFight Result, string Order);

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

        // WHAT EACH CARD IS WORTH HERE, AS A PLAYER JUDGES IT: better than the basic card it would push out?
        //
        // ⚠⚠ ADDING A CARD MEASURED NOTHING (2026-09-27, discarded): one more card shuffles into a different
        // order, so "the same shuffle" was a different fight and the pairs were not pairs — and every card came
        // out a little negative, the deck diluted. So the card TAKES A SLOT: a Deed replaces a Paper Cut, anything
        // else a Cower Behind a Desk, at the same place in the deck list. The shuffle is a function of the seed
        // and the number of cards, not of which cards they are, so the new card is drawn exactly when the one it
        // replaced would have been — every pair is the same fight but for that card. The bench checks that
        // (`coupled`), fight by fight, rather than trusting it.
        var bnb = Cards.FinalCards.All().ToDictionary(c => c.Id);
        var candidates = Cards.FinalCards.RewardPool(1).Select(c => c.Id)
            .Where(id => options.Only is not { Count: > 0 } only || only.Contains(id)).ToList();
        say($"\ncard values over stages {string.Join(", ", stages)} — {candidates.Count} Act-I cards, each in place of a basic card");
        var baseKey = baseline.ToDictionary(f => (f.Encounter, f.Shuffle), f => f);
        var rows = new List<(string Card, string Replaced, double Saved, double Error, int Wins, int Coupled, int Pairs)>();
        // Every card's deck, all fought in ONE parallel pass: a pass per card left most cores idle behind its
        // slowest fight.
        var decks = candidates
            .Select(card => (Card: card, Replaced: bnb[card].Type == Cards.CardAuthoring.DeedTag ? "paper_cut" : "cower_behind_a_desk"))
            .Where(c => baseDeck.Contains(c.Replaced))
            .Select(c =>
            {
                var swapped = baseDeck.ToList();
                swapped[swapped.IndexOf(c.Replaced)] = c.Card;
                return (c.Card, c.Replaced, Deck: (IReadOnlyList<string>)swapped);
            })
            .ToList();
        // In batches, each reported as it lands: one pass over all 65 cards ran four hours and printed nothing
        // before the clock stopped it (2026-09-27). A batch of a dozen decks still keeps every core busy.
        foreach (var batch in decks.Chunk(12))
        {
            var fought = BenchMany(game, byStage, [.. batch.Select(d => (d.Card, d.Deck))], options);
            foreach (var (card, replaced, _) in batch)
            {
                var with = fought[card];
                var diffs = with.Select(f => (double)(baseKey[(f.Encounter, f.Shuffle)].Result.HealthLost - f.Result.HealthLost)).ToList();
                var mean = diffs.Average();
                var error = diffs.Count > 1
                    ? Math.Sqrt(diffs.Sum(d => (d - mean) * (d - mean)) / (diffs.Count - 1) / diffs.Count)
                    : 0;
                var wins = with.Count(f => f.Result.Won) - baseline.Count(f => f.Result.Won);
                // Coupled: the same card INSTANCES in the same order at the first draw. A card instance is numbered by
                // its place in the deck list, so the swapped card carries the replaced card's number.
                var coupled = with.Count(f => f.Order == baseKey[(f.Encounter, f.Shuffle)].Order);
                rows.Add((card, replaced, mean, error, wins, coupled, with.Count));
                say($"  {card,-28} {Family(bnb[card]),-12} {bnb[card].Cost}E  for {replaced,-20} saves {mean,6:+0.0;-0.0} ±{error:0.0} HP/fight   wins {wins,3:+0;-0;0}   coupled {coupled}/{with.Count}");
            }
        }
        say("\nranked (health saved per fight against the basic card it replaces; ± is one standard error):");
        foreach (var r in rows.OrderByDescending(r => r.Wins).ThenByDescending(r => r.Saved))
            say($"  {r.Saved,6:+0.0;-0.0} ±{r.Error:0.0}  {r.Wins,3:+0;-0;0}  {r.Card} (for {r.Replaced})");

        // THE FAMILIES (PLAYTEST_FEEDBACK_2 F2): each family's median health saved, and that median against
        // Paperwork's — the gate is every family within 15 % of it.
        var families = rows.GroupBy(r => Family(bnb[r.Card]))
            .Select(g => (Family: g.Key, Median: Median([.. g.Select(r => r.Saved)]), Cards: g.Count()))
            .OrderByDescending(f => f.Median).ToList();
        var paperwork = families.FirstOrDefault(f => f.Family == "Paperwork").Median;
        say("\nfamilies (median health saved per fight; share of Paperwork's median):");
        foreach (var f in families)
            say($"  {f.Family,-14} {f.Median,6:+0.0;-0.0}   {(paperwork > 0 ? $"{f.Median / paperwork,5:0%}" : "    –")}   ({f.Cards} cards)");
        return 0;
    }

    // Every (stage, encounter, shuffle) fought once, in parallel. Each fight is its own playback — nothing shared
    // but the immutable blueprint.
    private static List<Fight> Bench(
        RunBlueprint game, IReadOnlyList<(string Stage, List<string> Encounters)> byStage,
        IReadOnlyList<string> deck, Options options) =>
        BenchMany(game, byStage, [("base", deck)], options)["base"];

    // …and for several decks at once, every (deck, encounter, shuffle) in one parallel pass.
    private static Dictionary<string, List<Fight>> BenchMany(
        RunBlueprint game, IReadOnlyList<(string Stage, List<string> Encounters)> byStage,
        IReadOnlyList<(string Key, IReadOnlyList<string> Deck)> decks, Options options)
    {
        var jobs = decks
            .SelectMany(d => byStage.SelectMany(s => s.Encounters.SelectMany(e =>
                Enumerable.Range(1, options.Shuffles).Select(k => (d.Key, d.Deck, s.Stage, e, k)))))
            .ToList();
        var results = new ConcurrentBag<(string Key, Fight Fight)>();
        // Progress on stderr: a pass can run for an hour, and a slow fight should name itself rather than hide.
        var done = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Parallel.ForEach(jobs,
            new ParallelOptions { MaxDegreeOfParallelism = options.Jobs > 0 ? options.Jobs : Environment.ProcessorCount },
            job =>
            {
                var encounter = game.Encounters.First(e => e.Id.Value == job.e);
                var ring = SparringRing.OneFight(game, encounter, job.Deck, maxHealth: options.Health);
                using var play = new RunPlayback(() => { });
                play.Start(ring, job.k, interactive: true);
                while (play.Session is { IsAwaitingInterlude: true } session)
                    session.Continue();
                if (play.CombatDriver?.Current is not { } combat)
                    return;
                var zones = combat.State.GetCardZones(combat.HeroId);
                var order = string.Join(",", zones.Hand.Concat(zones.DrawPile).Select(c => c.Id.value));
                var planner = new FightPlanner(options.Horizon, options.Beam, options.PerTurn);
                var fight = new Fight(job.Stage, job.e, job.k, planner.Play(combat), order);
                results.Add((job.Key, fight));
                if (fight.Result.Seconds > 120)
                    Console.Error.WriteLine($"  slow: {job.Key} {job.e} #{job.k} {fight.Result.Seconds:0}s {fight.Result.Turns} turns");
                if (Interlocked.Increment(ref done) % 200 == 0)
                    Console.Error.WriteLine($"  {done}/{jobs.Count} fights, {clock.Elapsed.TotalMinutes:0.0} min");
            });
        return results.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.Select(r => r.Fight).ToList());
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

    // A card's family: the first keyword its rules text names — the one it is built around. A card naming none
    // is plain damage / Block.
    private static readonly (string Family, string[] Words)[] FamilyWords =
    [
        ("Paperwork", ["Paperwork"]), ("Doubt", ["Doubt"]), ("Queue", ["Queue"]), ("Lien", ["Lien"]),
        ("Citation", ["Citation"]), ("Blood Ink", ["Blood Ink"]), ("Seal", ["Seal", "Ratif"]),
        ("Ward Wax", ["Ward Wax"]), ("Censure", ["Censure"]), ("Archive/Junk", ["Archive", "Junk", "Misfiled Paper", "Duplicate Copy"]),
    ];

    public static string Family(Cards.CardAuthoring.BnbCard card)
    {
        var text = card.RulesText;
        var found = FamilyWords
            .Select(f => (f.Family, At: f.Words.Select(w => text.IndexOf(w, StringComparison.Ordinal)).Where(i => i >= 0).DefaultIfEmpty(int.MaxValue).Min()))
            .Where(f => f.At != int.MaxValue).OrderBy(f => f.At).FirstOrDefault();
        return found.Family ?? "plain";
    }

    public static int Pool(Action<string> say)
    {
        foreach (var family in Cards.FinalCards.RewardPool(5).GroupBy(Family).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            say($"\n== {family.Key} ({family.Count()})");
            foreach (var c in family.OrderBy(c => c.Act).ThenBy(c => c.Cost))
                say($"  A{c.Act} {c.Cost}E {c.Rarity,-8} {c.Type,-8} {c.Id,-26} {c.RulesText}");
        }
        return 0;
    }

    private static double Median(IReadOnlyList<double> xs)
    {
        var s = xs.OrderBy(x => x).ToList();
        return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    private static IEnumerable<string> Tally(IEnumerable<string> ids) =>
        ids.GroupBy(id => id).Select(g => g.Count() > 1 ? $"{g.Key} ×{g.Count()}" : g.Key);
}
