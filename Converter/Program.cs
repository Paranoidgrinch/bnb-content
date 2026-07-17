using BnbContent.Converter;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

// CLI: --data <dir> --out <file> --seed <int>
var dataDir = "source-data";
var outFile = "game.roguedeck.json";
var seed = 20260717;
for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--data": dataDir = args[i + 1]; break;
        case "--out": outFile = args[i + 1]; break;
        case "--seed": seed = int.Parse(args[i + 1]); break;
    }
}

try
{
    var data = BabData.Load(dataDir);
    var blueprint = BlueprintAssembler.Build(data, seed);

    var problems = RunDocumentValidator.ValidateForExport(blueprint).ToList();
    if (problems.Count > 0)
    {
        Console.Error.WriteLine($"Export validation failed with {problems.Count} problem(s):");
        foreach (var problem in problems)
            Console.Error.WriteLine($"  - {problem}");
        return 1;
    }

    var options = RunJson.CreateOptions();
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
