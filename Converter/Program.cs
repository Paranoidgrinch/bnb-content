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
string? poolTrial = null;
string? fightEnemy = null;
string? fightEncounter = null;
string? fightIntent = null;
string? fightDeck = null;
string? fightRelics = null;
int? fightHealth = null;
var fightEnergy = 3;
var fights = 1;
// THE ACT-I BENCH (Act1Bench): --act1-bench [--bench-shuffles n] [--bench-horizon n] [--bench-beam n]
//      [--bench-stages queue,counter] [--bench-extra card,card] [--bench-cards [--bench-only card,card]] [--bench-hp n]
// THE CARD RATING (CardRating, BALANCE_PLAN K1): --card-rating — every final card's effect per energy, read
// statically off its program. Seconds; no fight is played.
var act1Bench = args.Contains("--act1-bench");
var cardRating = args.Contains("--card-rating");
// THE FAMILY POOL: --family-pool — every reward card, by keyword family and act (the shelf the family decks are built from).
var familyPool = args.Contains("--family-pool");
// THE FAMILY BENCH (FamilyBench, PLAYTEST_FEEDBACK_2 F2): --family-decks FAMILY_DECKS.json [--family-acts 2,3]
//      [--family-relics none|only|both] [--family-normals n] [--bench-only deck,deck] [--family-lines] + the --bench-* knobs —
//      one built deck per keyword family, fought through a later act's normals, elites and bosses.
string? familyDecks = null;
string? familyActs = null;
var familyRelics = "both";
var familyNormals = 10;
// THE ACT BENCH (ActBench, act III/IV tuning 2026-10-08): --act-bench <snapshots.json> --bench-acts 3,4 [--yes]
//      + the --bench-* knobs — every encounter of an act against the decks real runs brought there.
string? actBench = null;
// THE MECHANIC AUDIT (MechanicAudit, 2026-10-30): --mechanic-audit <snapshots.json> --bench-acts 1,2,3,4 [--audit-per n]
//      [--bench-only id,id] — the planner's line through every encounter replayed on a traced fork, every rule counted.
string? mechanicAudit = null;
var auditPer = 2;
string? benchActs = null;
string? benchRuns = null;
// THE AUDIT (Audit, CONTENT_FIX_PLAN F5): --audit [--audit-only id,id] — every card and combat relic over four
// rounds against a quiet dummy and a striker, paired with the same fight without it; report to ~/Desktop/bnb-balance/audit.
var audit = args.Contains("--audit");
// THE CALCULATOR CHECK (CalcCheck, PLAYTEST_FEEDBACK_2 A3): --calc-check N — N real fights; every card preview,
// turn forecast and intent chip compared with what then happened; report to ~/Desktop/bnb-balance/calc.
var calcCheck = 0;
// THE HOT CAULDRON (Hedge Witch plan E6): --witch-hot builds the document with her post-Brew Hot state switched on.
var witchHot = args.Contains("--witch-hot");
string? auditOnly = null;
var benchCards = args.Contains("--bench-cards");
var benchShuffles = 20;
var benchHorizon = 5;
var benchBeam = 16;
var benchPerTurn = 300;
var benchHealth = 70;
string? benchStages = null;
string? benchExtra = null;
string? benchOnly = null;
// THE LIEUTENANT TRIAL RUNNER (TrialRunner, LT_RUNNER_PLAN.md): --lt-runner [--acts 1,2,3,4,5] [--runner-seeds n]
//      [--runner-paths n] [--runner-k n] [--runner-h n] [--runner-beam n] [--runner-perturn n] [--runner-out dir]
//      [--runner-seed n] [--runner-only id,id] [--compare <trial-log.jsonl>] [--jobs n] — the encounter trial played
//      by a determinized-search bot; output in ~/Desktop/bnb-balance/bots/. Package choices by play (TrialChoices):
//      [--choice-k n] [--choice-beam n] [--choice-perturn n] [--choice-seeds n] [--choice-top n] [--choice-skip-margin x]
//      [--runner-values <json>] (default bots/lt-card-values.json) [--no-learn] [--runner-static] (the old rating).
//      [--runner-human-decks <trial-log.jsonl>] (every fight with the deck the user fought it with)
//      [--runner-chain] (each act starts from the previous act's best ending decks instead of its preset deck)
var ltRunner = args.Contains("--lt-runner");
var runnerActs = "1";
var runnerSeeds = 20;
var runnerPaths = 5;
var runnerBot = new TrialBot.Options();
string? runnerOut = null;
string? runnerCompare = null;
string? runnerHumanDecks = null;
var runnerChain = args.Contains("--runner-chain");
string? runnerOnly = null;
var runnerSeed = 20261106;
// Package choices by play (TrialChoices), on by default; --runner-static for the old static rating.
var runnerChoices = new TrialChoices.Options(TrialChoices.CheapBot);
string? runnerValues = null;
// THE LEARNED PLAYER AND DRAFTER (PlayPolicy, PolicyTraining, DeckModel; overnight: tools/lt_overnight.sh):
//      --policy-dir <dir> (the runner plays with the learned policies there; default for training bots/lt-policy)
//      --deck-model <json> (the draft model orders the package candidates; default for training bots/lt-deck-model.json)
//      --runner-rollout-perturn n (lines per rollout turn with a policy)
//      --lt-train [--acts ..] [--runner-only ..] [--train-gens n] [--train-pop n] [--train-elite n] [--train-seeds n]
//          [--valid-seeds n] [--train-decks n] [--train-perturn n] [--train-minutes m] [--shared-gens n] [--train-warm]
//      --lt-decks [--acts ..] [--deck-progressions n] [--deck-seeds n] [--deck-epsilon x] [--deck-round n] [--deck-data <jsonl>] [--deck-lambda x] [--deck-synergies] [--deck-per-act]
var bots = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "bnb-balance", "bots");
string? policyDir = null;
string? deckModelPath = null;
var ltTrain = args.Contains("--lt-train");
var ltDecks = args.Contains("--lt-decks");
var train = new PolicyTraining.Options([1], Path.Combine(bots, "lt-policy"), Warm: args.Contains("--train-warm"));
var deckRound = new PolicyTraining.DeckOptions([1], 64, 2, 0.3, 1, Path.Combine(bots, "lt-deck-data.jsonl"),
    Path.Combine(bots, "lt-deck-model.json"), Path.Combine(bots, "lt-policy"));
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
        case "--pool-trial": poolTrial = args[i + 1]; break;
        case "--fight-enemy": fightEnemy = args[i + 1]; break;
        case "--fight-encounter": fightEncounter = args[i + 1]; break;
        case "--fight-intent": fightIntent = args[i + 1]; break;
        case "--fight-deck": fightDeck = args[i + 1]; break;
        case "--fight-relics": fightRelics = args[i + 1]; break;
        case "--fight-hp": fightHealth = int.Parse(args[i + 1]); break;
        case "--fight-energy": fightEnergy = int.Parse(args[i + 1]); break;
        case "--fights": fights = int.Parse(args[i + 1]); break;
        case "--calc-check": calcCheck = int.Parse(args[i + 1]); break;
        case "--bench-shuffles": benchShuffles = int.Parse(args[i + 1]); break;
        case "--bench-horizon": benchHorizon = int.Parse(args[i + 1]); break;
        case "--bench-beam": benchBeam = int.Parse(args[i + 1]); break;
        case "--bench-perturn": benchPerTurn = int.Parse(args[i + 1]); break;
        case "--bench-hp": benchHealth = int.Parse(args[i + 1]); break;
        case "--bench-stages": benchStages = args[i + 1]; break;
        case "--bench-extra": benchExtra = args[i + 1]; break;
        case "--bench-only": benchOnly = args[i + 1]; break;
        case "--family-decks": familyDecks = args[i + 1]; break;
        case "--family-acts": familyActs = args[i + 1]; break;
        case "--family-relics": familyRelics = args[i + 1]; break;
        case "--family-normals": familyNormals = int.Parse(args[i + 1]); break;
        case "--act-bench": actBench = args[i + 1]; break;
        case "--mechanic-audit": mechanicAudit = args[i + 1]; break;
        case "--audit-per": auditPer = int.Parse(args[i + 1]); break;
        case "--bench-acts": benchActs = args[i + 1]; break;
        case "--bench-runs": benchRuns = args[i + 1]; break;
        case "--tuning": ActTuning.Override(args[i + 1]); break;
        case "--apprentice-damage": ActTuning.ApprenticeDamagePercent = int.Parse(args[i + 1]); break;
        case "--apprentice-block": ActTuning.ApprenticeBlockPercent = int.Parse(args[i + 1]); break;
        case "--audit-only": auditOnly = args[i + 1]; break;
        case "--acts": runnerActs = args[i + 1]; break;
        case "--runner-seeds": runnerSeeds = int.Parse(args[i + 1]); break;
        case "--runner-paths": runnerPaths = int.Parse(args[i + 1]); break;
        case "--runner-k": runnerBot = runnerBot with { Worlds = int.Parse(args[i + 1]) }; break;
        case "--runner-h": runnerBot = runnerBot with { Horizon = int.Parse(args[i + 1]) }; break;
        case "--runner-beam": runnerBot = runnerBot with { Beam = int.Parse(args[i + 1]) }; break;
        case "--runner-perturn": runnerBot = runnerBot with { PerTurn = int.Parse(args[i + 1]) }; break;
        case "--runner-out": runnerOut = args[i + 1]; break;
        case "--runner-seed": runnerSeed = int.Parse(args[i + 1]); break;
        case "--runner-only": runnerOnly = args[i + 1]; break;
        case "--compare": runnerCompare = args[i + 1]; break;
        case "--runner-human-decks": runnerHumanDecks = args[i + 1]; break;
        case "--choice-k": runnerChoices = runnerChoices with { Bot = runnerChoices.Bot with { Worlds = int.Parse(args[i + 1]) } }; break;
        case "--choice-beam": runnerChoices = runnerChoices with { Bot = runnerChoices.Bot with { Beam = int.Parse(args[i + 1]) } }; break;
        case "--choice-perturn": runnerChoices = runnerChoices with { Bot = runnerChoices.Bot with { PerTurn = int.Parse(args[i + 1]) } }; break;
        case "--choice-seeds": runnerChoices = runnerChoices with { Seeds = int.Parse(args[i + 1]) }; break;
        case "--choice-top": runnerChoices = runnerChoices with { Top = int.Parse(args[i + 1]) }; break;
        case "--choice-skip-margin": runnerChoices = runnerChoices with { SkipMargin = double.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture) }; break;
        case "--runner-values": runnerValues = args[i + 1]; break;
        case "--policy-dir": policyDir = args[i + 1]; break;
        case "--deck-model": deckModelPath = args[i + 1]; break;
        case "--runner-rollout-perturn": runnerBot = runnerBot with { RolloutPerTurn = int.Parse(args[i + 1]) }; break;
        case "--train-gens": train = train with { Gens = int.Parse(args[i + 1]) }; break;
        case "--train-pop": train = train with { Pop = int.Parse(args[i + 1]) }; break;
        case "--train-elite": train = train with { Elite = int.Parse(args[i + 1]) }; break;
        case "--train-seeds": train = train with { TrainSeeds = int.Parse(args[i + 1]) }; break;
        case "--valid-seeds": train = train with { ValidSeeds = int.Parse(args[i + 1]) }; break;
        case "--train-decks": train = train with { Decks = int.Parse(args[i + 1]) }; break;
        case "--train-perturn": train = train with { PerTurn = int.Parse(args[i + 1]) }; deckRound = deckRound with { PerTurn = int.Parse(args[i + 1]) }; break;
        case "--train-minutes": train = train with { Minutes = double.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture) }; break;
        case "--shared-gens": train = train with { SharedGens = int.Parse(args[i + 1]) }; break;
        case "--train-mingens": train = train with { MinGens = int.Parse(args[i + 1]) }; break;
        case "--shared-cases": train = train with { SharedCases = int.Parse(args[i + 1]) }; break;
        case "--deck-progressions": deckRound = deckRound with { Progressions = int.Parse(args[i + 1]) }; break;
        case "--deck-synergies": deckRound = deckRound with { Synergies = true }; break;
        case "--deck-per-act": deckRound = deckRound with { Pooled = false }; break;
        case "--deck-lambda": deckRound = deckRound with { Lambda = double.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture) }; break;
        case "--deck-seeds": deckRound = deckRound with { Seeds = int.Parse(args[i + 1]) }; break;
        case "--deck-epsilon": deckRound = deckRound with { Epsilon = double.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture) }; break;
        case "--deck-round": deckRound = deckRound with { Round = int.Parse(args[i + 1]) }; break;
        case "--deck-data": deckRound = deckRound with { DataPath = args[i + 1] }; break;
    }
}

try
{
    var data = BabData.Load(dataDir);
    var blueprint = BlueprintAssembler.Build(data, seed, witchHot);

    if (poolTrial is not null)
        return BnbContent.Converter.Playtest.PoolTrial.Run(blueprint, poolTrial, Console.WriteLine);
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
    if (ltTrain)
        return PolicyTraining.TrainPolicies(blueprint, train with
        {
            Acts = [.. Split(runnerActs).Select(int.Parse)],
            Dir = policyDir ?? train.Dir,
            Only = Split(runnerOnly),
            Seed = runnerSeed,
            Jobs = jobs,
            ModelPath = deckModelPath ?? Path.Combine(bots, "lt-deck-model.json"),
            ValuesPath = runnerValues ?? Path.Combine(bots, "lt-card-values.json"),
            MeasuredPaths = runnerPaths,
            MeasuredSeeds = runnerSeeds,
            Compare = runnerCompare,
        }, Console.WriteLine);
    if (ltDecks)
        return PolicyTraining.DeckRounds(blueprint, deckRound with
        {
            Acts = [.. Split(runnerActs).Select(int.Parse)],
            PolicyDir = policyDir ?? deckRound.PolicyDir,
            ModelPath = deckModelPath ?? deckRound.ModelPath,
            Seed = runnerSeed,
            Jobs = jobs,
            ValuesPath = runnerValues ?? Path.Combine(bots, "lt-card-values.json"),
        }, Console.WriteLine);
    if (policyDir is not null || deckModelPath is not null)
        runnerChoices = runnerChoices with
        {
            Book = policyDir is null ? null : new PolicyBook(policyDir),
            Model = DeckModel.Load(deckModelPath),
        };
    if (ltRunner)
        return TrialRunner.Run(blueprint, new TrialRunner.Options(
            [.. Split(runnerActs).Select(int.Parse)], runnerSeeds, runnerPaths, runnerBot, jobs,
            runnerOut ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "bnb-balance", "bots"),
            runnerCompare, runnerSeed, Split(runnerOnly), args.Contains("--runner-verbose"),
            args.Contains("--runner-static") ? null : runnerChoices,
            args.Contains("--runner-static") || args.Contains("--no-learn") ? null
                : runnerValues ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "bnb-balance", "bots", "lt-card-values.json"),
            policyDir is null ? null : new PolicyBook(policyDir), DeckModel.Load(deckModelPath), HumanDecks: runnerHumanDecks, Chain: runnerChain),
            Console.WriteLine);
    if (calcCheck > 0)
        return CalcCheck.Run(blueprint, calcCheck, seed, Console.WriteLine, Split(auditOnly));
    if (audit)
        return Audit.Run(blueprint, Split(auditOnly) is { Count: > 0 } picked ? picked : null, jobs, Console.WriteLine);
    if (mechanicAudit is not null)
        return MechanicAudit.Run(blueprint, new MechanicAudit.Options(
            mechanicAudit, [.. Split(benchActs).Select(int.Parse)], auditPer, benchHorizon,
            args.Contains("--bench-beam") ? benchBeam : 6, args.Contains("--bench-perturn") ? benchPerTurn : 100, jobs,
            Split(benchOnly)), Console.WriteLine);
    if (actBench is not null)
        return ActBench.Run(blueprint, new ActBench.Options(
            actBench, [.. Split(benchActs).Select(int.Parse)], args.Contains("--bench-shuffles") ? benchShuffles : 1, benchHorizon,
            args.Contains("--bench-beam") ? benchBeam : 6, args.Contains("--bench-perturn") ? benchPerTurn : 100, jobs,
            Split(benchOnly), args.Contains("--yes"), Runs: Split(benchRuns)), Console.WriteLine);
    if (familyDecks is not null)
        return FamilyBench.Run(blueprint, new FamilyBench.Options(
            familyDecks, args.Contains("--bench-shuffles") ? benchShuffles : 4, benchHorizon,
            args.Contains("--bench-beam") ? benchBeam : 6, args.Contains("--bench-perturn") ? benchPerTurn : 100, jobs,
            familyNormals, benchHealth, familyRelics, Split(benchOnly),
            [.. Split(familyActs).Select(int.Parse)], args.Contains("--family-lines")), Console.WriteLine);
    if (familyPool)
        return Act1Bench.Pool(Console.WriteLine);
    if (cardRating)
        return CardRating.Run(blueprint, Console.WriteLine);
    if (act1Bench)
        return Act1Bench.Run(blueprint, new Act1Bench.Options(
            benchShuffles, benchHorizon, benchBeam, benchPerTurn, Jobs: jobs, StagesWanted: Split(benchStages),
            Extra: Split(benchExtra), Cards: benchCards, Health: benchHealth, Only: Split(benchOnly)), Console.WriteLine);
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
