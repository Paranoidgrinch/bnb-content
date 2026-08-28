using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

// CLI: --data <dir> --out <file> --seed <int>
//      --playtest <n>   walk n whole runs instead of writing the document, and report what they met
//      --maps <n>       lay out every act's map for n seeds and report its shape, row by row
var dataDir = "source-data";
var outFile = "game.roguedeck.json";
var seed = 20260717;
var playtest = 0;
var maps = 0;
for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--data": dataDir = args[i + 1]; break;
        case "--out": outFile = args[i + 1]; break;
        case "--seed": seed = int.Parse(args[i + 1]); break;
        case "--playtest": playtest = int.Parse(args[i + 1]); break;
        case "--maps": maps = int.Parse(args[i + 1]); break;
    }
}

try
{
    var data = BabData.Load(dataDir);
    var blueprint = BlueprintAssembler.Build(data, seed);

    if (maps > 0)
        return MapStats(blueprint, seed, maps);
    if (playtest > 0)
        return Playtest(blueprint, seed, playtest);

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
static int Playtest(RunBlueprint blueprint, int seed, int runs)
{
    // Through the exported document, not the in-memory one: what Godot loads is what gets walked.
    var options = RunJson.CreateOptions(indented: false);
    var shipped = RunJson.BlueprintFromJson(RunJson.ToJson(blueprint, options), options);
    var tester = RunWalker.WithHealth(shipped, 9999);
    var failures = 0;

    // Which GAME is being walked, said once. The walk seeds run from the same number, so a walk reported as
    // "seed 20260909" is not reproducible on its own: --seed 20260909 builds a different game and walks it
    // once. To get that walk back, walk the same game far enough to reach it.
    Console.WriteLine($"walking {runs} run(s) of the game built with seed {seed}");

    for (var i = 0; i < runs; i++)
    {
        var walkSeed = seed + i;
        var report = RunWalker.Walk(tester, walkSeed, saveEvery: 5, progress: Console.WriteLine);
        var acts = shipped.Acts?.Count ?? 1;
        var reachedTheEnd = report.Result == RunResult.Victory;
        var ok = report.Error is null && report.Notes.Count == 0 && reachedTheEnd;
        if (!ok)
            failures++;
        Console.WriteLine($"{(ok ? "ok  " : "FAIL")} seed {walkSeed}: {report.Result}, "
            + $"{report.Stops.Count} rooms over {report.ActsWalked}/{acts} acts, {report.Steps} steps");
        for (var act = 1; act <= report.ActsWalked; act++)
        {
            var rooms = report.InAct(act).ToList();
            var byRole = rooms.GroupBy(r => r.Role).OrderBy(g => g.Key)
                .Select(g => $"{g.Key} {g.Count()}");
            Console.WriteLine($"     act {act}: {rooms.Count} rooms — {string.Join(", ", byRole)}");
            Console.WriteLine($"       boss: {string.Join(", ", rooms.Where(r => r.Role == "boss").Select(r => r.Content))}");
            Console.WriteLine($"       doors: {string.Join(", ", rooms.Where(r => r.Role == "event").Select(r => r.Content))}");
        }
        if (report.Error is { } error)
            Console.WriteLine($"     ERROR: {error}");
        foreach (var note in report.Notes)
            Console.WriteLine($"     NOTE: {note}");
    }

    Console.WriteLine(failures == 0 ? $"all {runs} walk(s) finished" : $"{failures}/{runs} walk(s) had problems");
    return failures == 0 ? 0 : 1;
}
