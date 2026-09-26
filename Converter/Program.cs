using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

// CLI: --data <dir> --out <file> --seed <int>
//      --playtest <n>   walk n whole runs instead of writing the document, and report what they met
//      --walk <seed>    walk exactly ONE run of that game, the one a --playtest report calls "seed <seed>"
//      --maps <n>       lay out every act's map for n seeds with v0.0.0 and report its shape, row by row.
//                       v0.0.1's equivalent is --map-report, which asks a different question of many more seeds
//      --map-report <n> what every act's STRATEGIC rules (v0.0.1) produce over n seeds: one line a measure,
//                       with the outlier seed named at each end. Exits non-zero only on an act that could not
//                       be generated clean; everything else is printed to be read.
//      --generator <id> which map generator --walk / --playtest lay their maps out with. Default "v0.0.1"
//      --jobs <n>       how many --playtest walks run at once (default: cores). One walk is single-threaded
//                       and the walks share nothing but the immutable blueprint.
//                       (strategic); pass "v0.0.0" for the rule-based maps every run used until 2026-09-14.
//      --art-slots <f>  write the picture list (ART_SLOTS.md) instead of the document: one row per file the
//                       frontend will look for, with the design canon's brief beside each relic
//      --fight <file>   THE SPARRING RING: one authored fight with a stated loadout, n times over n seeds.
//                       The file is a snapshot (see SparringRing.Snapshot); everything in it has a default,
//                       so the smallest useful one is {"enemy": "…"} or {"encounter": "…"}.
//      --fight-enemy <id> | --fight-encounter <id>
//                       the same ring without a file, for a quick look. Tunable with
//                       --fight-intent <id> --fight-hp <n> --fight-energy <n> --fights <n>
//                       --fight-deck a,b,c --fight-relics x,y
var dataDir = "source-data";
var outFile = "game.roguedeck.json";
var seed = 20260717;
var playtest = 0;
var jobs = Environment.ProcessorCount;
var walk = 0;
var maps = 0;
var mapReport = 0;
string? generator = null;
string? artSlots = null;
string? fightFile = null;
string? fightEnemy = null;
string? fightEncounter = null;
string? fightIntent = null;
string? fightDeck = null;
string? fightRelics = null;
int? fightHealth = null;
var fightEnergy = 3;
var fights = 1;
for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--data": dataDir = args[i + 1]; break;
        case "--out": outFile = args[i + 1]; break;
        case "--seed": seed = int.Parse(args[i + 1]); break;
        case "--playtest": playtest = int.Parse(args[i + 1]); break;
        case "--jobs": jobs = int.Parse(args[i + 1]); break;
        case "--walk": walk = int.Parse(args[i + 1]); break;
        // Which map generator the walked runs are laid out by: "v0.0.1" (strategic — what a BnB act now IS,
        // and the default here since 2026-09-14) or "v0.0.0" (rule-based, which still ships and is what a save
        // written before the choice existed resumes on). Both ship, and a walk that does not say which one it
        // used is a walk nobody can repeat, so every report names it.
        case "--generator": generator = args[i + 1]; break;
        case "--maps": maps = int.Parse(args[i + 1]); break;
        // How many seeds per act the strategic generator's statistical report reads (map rework S13). The
        // tests run a thousand; ten thousand is the number to reach for when a budget is being tuned.
        case "--map-report": mapReport = int.Parse(args[i + 1]); break;
        case "--art-slots": artSlots = args[i + 1]; break;
        case "--fight": fightFile = args[i + 1]; break;
        case "--fight-enemy": fightEnemy = args[i + 1]; break;
        case "--fight-encounter": fightEncounter = args[i + 1]; break;
        case "--fight-intent": fightIntent = args[i + 1]; break;
        case "--fight-deck": fightDeck = args[i + 1]; break;
        case "--fight-relics": fightRelics = args[i + 1]; break;
        case "--fight-hp": fightHealth = int.Parse(args[i + 1]); break;
        case "--fight-energy": fightEnergy = int.Parse(args[i + 1]); break;
        case "--fights": fights = int.Parse(args[i + 1]); break;
    }
}

try
{
    var data = BabData.Load(dataDir);
    var blueprint = BlueprintAssembler.Build(data, seed);

    if (fightFile is not null || fightEnemy is not null || fightEncounter is not null)
    {
        // A named file is the durable form — check it in beside the content and it is a regression test that
        // reads like a bug report. The flags are the ten-second form of the same thing.
        var snapshot = fightFile is not null
            ? SparringRing.Read(fightFile)
            : new SparringRing.Snapshot
            {
                Enemy = fightEnemy,
                Encounter = fightEncounter,
                Intent = fightIntent,
                Health = fightHealth,
                MaxHealth = Math.Max(fightHealth ?? 0, 80),
                Energy = fightEnergy,
                Deck = Split(fightDeck),
                Relics = Split(fightRelics),
                Fights = fights,
            };
        return SparringMatch.Run(blueprint, snapshot, Console.WriteLine);
    }
    if (artSlots is not null)
        return ArtSlots.Write(blueprint, data, dataDir, artSlots);
    if (maps > 0)
        return MapStats(blueprint, seed, maps);
    if (mapReport > 0)
        return MapReport(blueprint, seed, mapReport);
    if (walk != 0)
        return Playtest(blueprint, seed, 1, generator, jobs, walk);
    if (playtest > 0)
        return Playtest(blueprint, seed, playtest, generator, jobs);

    var problems = RunDocumentValidator.ValidateForExport(blueprint).ToList();
    if (problems.Count > 0)
    {
        Console.Error.WriteLine($"Export validation failed with {problems.Count} problem(s):");
        foreach (var problem in problems)
            Console.Error.WriteLine($"  - {problem}");
        return 1;
    }

    // The SHIPPED form: no indentation. Nobody reads this file by hand — the source data is what gets read —
    // and the whitespace was two thirds of it (13.6 MB indented against 4.1 MB, and 134 MB of heap at load
    // against 54 MB). The Studio's own JSON view keeps its indentation; that is a different document.
    var options = RunJson.CreateOptions(indented: false);
    var json = RunJson.ToJson(blueprint, options);

    // The document must survive its own round trip byte-for-byte.
    var reloaded = RunJson.BlueprintFromJson(json, options);
    if (RunJson.ToJson(reloaded, options) != json)
    {
        Console.Error.WriteLine("Round-trip mismatch: the serialized document does not reload identically.");
        return 1;
    }

    File.WriteAllText(outFile, json);
    Console.WriteLine($"Wrote {outFile}: {blueprint.Cards.Count} cards, {blueprint.Encounters.Count} encounters, "
    + $"{blueprint.EnemyActions.Count} enemy actions, {blueprint.Events.Count} events, "
    + $"{blueprint.Relics.Count} relics, {blueprint.Map.Nodes.Count} map nodes (seed {seed}).");
    return 0;
}
catch (ConversionException ex)
{
    Console.Error.WriteLine($"Conversion failed: {ex.Message}");
    return 2;
}

// A comma-separated list on the command line, e.g. --fight-deck paper_cut,paper_cut,strong_binder.
static IReadOnlyList<string> Split(string? list) =>
    string.IsNullOrWhiteSpace(list)
        ? []
        : [.. list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

// WHAT EVERY ACT'S STRATEGIC RULES PRODUCE OVER MANY SEEDS (map rework S13), as opposed to over the one seed
// somebody looked at. One line a measure, an outlier seed named at each end of it — and `MAP_DUMP_GENERATOR`
// on the dump probe is how you open the act a seed names.
//
// The exit code is the HARD constraint and nothing else: an act that could not be generated clean. Everything
// else is printed for a human to read, because a quality number that fails a build is a number nobody dares
// tune (source document §50).
static int MapReport(RunBlueprint blueprint, int seed, int seeds)
{
    var refused = 0;
    for (var act = 0; act < (blueprint.Acts?.Count ?? 0); act++)
    {
        if (blueprint.Acts![act].StrategicMapGeneration is not { } spec)
        {
            // The gauntlet, which authors no strategic rules and is drawn by v0.0.0 whichever generator the
            // run asked for. Said rather than skipped in silence: a report with a missing act reads as a bug.
            Console.WriteLine($"=== ACT {act + 1} — no strategic rules; this act is always laid out by "
                + $"{MapGenerators.RuleBased} ===\n");
            continue;
        }

        var report = StrategicActStatistics.Measure(spec, seed, seeds);
        Console.WriteLine($"=== ACT {act + 1} ===");
        Console.WriteLine(report.Render());
        refused += report.Refused.Count;
    }
    return refused == 0 ? 0 : 1;
}

// What the generated maps of every act actually hold: their shape, the rooms on them, and whether the routes
// through them honour the act's own rules. Cheap next to a walk, and it answers "is the act laid out as
// designed" without playing it.
static int MapStats(RunBlueprint blueprint, int seed, int runs)
{
    var balance = new BalanceCalculator(blueprint.Balance, blueprint.Encounters);
    var problems = 0;
    for (var i = 0; i < runs; i++)
    {
        for (var act = 0; act < (blueprint.Acts?.Count ?? 0); act++)
        {
            var spec = blueprint.Acts![act].MapGeneration!;
            // The same seed the run itself would use for this act (RunSetup's act stride), so what is printed
            // is the map that seed actually plays.
            var generated = RuleBasedMapGenerator.Generate(spec, seed + i + act * 7919, startingLoadout: 0, balance,
                (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));
            var rows = generated.Map.Nodes.Count == 0 ? 0
                : generated.Map.Nodes.Select(n => n.Id.Value.Split('c')[0]).Distinct().Count();
            var byRole = generated.Roles.Values.GroupBy(r => r).OrderBy(g => g.Key.ToString())
                .Select(g => $"{g.Key} {g.Count()}");
            Console.WriteLine($"seed {seed + i} act {act + 1}: {generated.Map.Nodes.Count} rooms in {rows} rows "
                + $"(spec asks {spec.Rows}) — {string.Join(", ", byRole)}");
            foreach (var row in generated.Map.Nodes.GroupBy(n => int.Parse(n.Id.Value.Split('c')[0][1..]))
                         .OrderBy(g => g.Key))
                Console.WriteLine($"       r{row.Key,-2} {string.Join(" ", row.Select(n => generated.Roles[n.Id]))}");
            var found = MapConstraintValidator.Validate(generated, spec).ToList();
            foreach (var problem in found)
                Console.WriteLine($"     PROBLEM: {problem}");
            problems += found.Count;
        }
    }
    return problems == 0 ? 0 : 1;
}

// Walk whole runs and print what each one met, act by act. A walk that errors, loops or never reaches the last
// act is a bug in the game, not in the walker — the report says which room it happened in.
static int Playtest(RunBlueprint blueprint, int seed, int runs, string? generator, int jobs, int? onlyWalk = null)
{
    // Through the exported document, not the in-memory one: what Godot loads is what gets walked.
    var options = RunJson.CreateOptions(indented: false);
    var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(blueprint, options), options);
    var tester = RunWalker.WithHealth(shipped, 99999); // see WholeRunTests.Shipped
    var failures = 0;

    // Which GAME is being walked, said once. The walk seeds run from the same number, so a walk reported as
    // "seed 20260909" is not reproducible on its own: --seed 20260909 builds a different game and walks it
    // once. To get that walk back, build the same game and name the walk: --walk 20260909.
    // The generator is named on every walk, always — from the moment two of them ship, a walk that does not
    // say which one laid its maps out is a report about an unknown act (plan §4b).
    // THE PLAYTESTER WALKS THE DESIGN, and the design is v0.0.1 (map rework S14). The engine's own default
    // stays v0.0.0 and must — a save written before the choice existed has nothing recorded, and nothing
    // recorded has to keep meaning the maps that save was laid out with. That is a statement about old runs.
    // What generator a NEW run should use is a different question, and this is BnB's answer to it.
    var laidOutBy = generator ?? MapGenerators.Strategic;
    Console.WriteLine(onlyWalk is { } one
        ? $"walking run {one} of the game built with seed {seed}, maps by {laidOutBy}"
        : $"walking {runs} run(s) of the game built with seed {seed}, maps by {laidOutBy}");

    // ⚠ THE WALKS RUN AT ONCE, AND EACH ONE STILL READS AS ITS OWN (S6). A walk is single-threaded, takes
    // minutes, and shares nothing with its neighbours but the immutable blueprint — so a `for` loop over
    // twelve of them was eleven idle cores. Each walk writes into its OWN buffer and the buffers are printed
    // in seed order afterwards: interleaved progress from a dozen walks would be a report nobody can read,
    // and a report that cannot be read is not one.
    var pages = new string[runs];
    var failed = new bool[runs];
    Parallel.For(0, runs, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, jobs) }, i =>
    {
        var page = new System.Text.StringBuilder();
        var walkSeed = onlyWalk ?? seed + i;
        var report = RunWalker.Walk(tester, walkSeed, saveEvery: 5, progress: line => page.AppendLine(line),
            mapGenerator: laidOutBy);
        var acts = shipped.Acts?.Count ?? 1;
        var reachedTheEnd = report.Result == RunResult.Victory;
        var ok = report.Error is null && report.Notes.Count == 0 && reachedTheEnd;
        failed[i] = !ok;
        // The generator on the RESULT line too, not only in the header above: a single line lifted out of a
        // long report (into a note, a commit message, a bug report) has to carry which act it is about.
        page.AppendLine($"{(ok ? "ok  " : "FAIL")} seed {walkSeed} maps {report.Maps}: {report.Result}, "
            + $"{report.Stops.Count} rooms over {report.ActsWalked}/{acts} acts, {report.Steps} steps");
        for (var act = 1; act <= report.ActsWalked; act++)
        {
            var rooms = report.InAct(act).ToList();
            var byRole = rooms.GroupBy(r => r.Role).OrderBy(g => g.Key)
                .Select(g => $"{g.Key} {g.Count()}");
            page.AppendLine($"     act {act}: {rooms.Count} rooms — {string.Join(", ", byRole)}");
            page.AppendLine($"       boss: {string.Join(", ", rooms.Where(r => r.Role == "boss").Select(r => r.Content))}");
            page.AppendLine($"       doors: {string.Join(", ", rooms.Where(r => r.Role == "event").Select(r => r.Content))}");
        }
        if (report.Error is { } error)
            page.AppendLine($"     ERROR: {error}");
        foreach (var note in report.Notes)
            page.AppendLine($"     NOTE: {note}");
        pages[i] = page.ToString();
    });
    foreach (var page in pages)
        Console.Write(page);
    failures = failed.Count(f => f);

    Console.WriteLine(failures == 0 ? $"all {runs} walk(s) finished" : $"{failures}/{runs} walk(s) had problems");
    return failures == 0 ? 0 : 1;
}
