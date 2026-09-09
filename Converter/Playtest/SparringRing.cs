using System.Text.Json;
using System.Text.Json.Serialization;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Playtest;

// ONE FIGHT, ON PURPOSE, WITH A STATED LOADOUT.
//
// Everything else that drives this game without a human walks: `--playtest` walks whole runs, `--walk` walks
// one, the Godot probes walk to a room and look at it. Walking is right for finding what only a run can find,
// and wrong for every other question — because the thirty rooms before the fight are noise, they cost minutes,
// and they are not the same thirty rooms twice.
//
// The questions walking cannot answer:
//
//   BALANCE     what does this elite cost a deck of THIS shape? Twenty times, and the spread, not one number.
//   DIAGNOSTIC  reproduce a crash without the thirty rooms in front of it.
//   REGRESSION  a snapshot file IS a bug report: check it in, run it again next month.
//
// The trick is that almost nothing had to be built for it. A fight with a stated loadout is a RunBlueprint
// whose map is one room — which the test probe has been doing since Act I — and the loadout is `RunStart`,
// which already carries deck, relics, consumables, resources and health, and which `RunSetup` and
// `RunRunner.GrantStartingRelics` already honour. So a snapshot is a RunStart with an encounter named beside
// it, and the fight it starts is the real one, over the real host path, with the real relic rules attached.
//
// This file is the SHARED definition of that, deliberately: `Tests/FightProbe.cs` builds its probes through
// it too. Two definitions of "a blueprint that is one fight" would drift, and the drift would show up as the
// suite and the command line disagreeing about a fight — which is the least useful disagreement available.
public static class SparringRing
{
    // WHO IS IN THE RING. Exactly one of Encounter / Enemy is given: an encounter is the fight as the game
    // actually fields it (roster, per-encounter health, the passives the room carries), an enemy is one body
    // taken out of its encounter and stood on its own.
    public sealed record Snapshot
    {
        public string? Name { get; init; }

        // The authored encounter to fight, e.g. "act_4_elite_tombbreakers_three".
        public string? Encounter { get; init; }

        // …or a single authored enemy, e.g. "nanna_sin_lord_of_the_counted_moon", lifted out of whatever
        // encounter fields it and kept with its real health, passives and intent rules.
        public string? Enemy { get; init; }

        // Pin the enemy to ONE of its actions. This is the sharpest tool in the file: a boss that cycles
        // twelve intents answers a balance question with an average, and an average is not what a designer
        // wants to know about the move that kills people. Ignored unless Enemy is set.
        public string? Intent { get; init; }

        // Statuses the enemy opens with, by id → stacks — how a late-phase fight is reached without playing
        // the early one.
        public IReadOnlyDictionary<string, int> EnemyStatuses { get; init; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        // ── the player ────────────────────────────────────────────────────────────────────────────────────
        public int MaxHealth { get; init; } = 80;
        public int? Health { get; init; }              // null ⇒ full
        public int Energy { get; init; } = 3;
        public IReadOnlyList<string> Deck { get; init; } = [];
        public IReadOnlyList<string> Relics { get; init; } = [];
        public IReadOnlyList<string> Consumables { get; init; } = [];
        public IReadOnlyDictionary<string, int> Statuses { get; init; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        // ── how hard to look ──────────────────────────────────────────────────────────────────────────────
        // One fight tells you what happened once. The spread over many is the answer to a balance question,
        // and the seeds are what make the fights differ: the same loadout, shuffled differently.
        public int Fights { get; init; } = 1;
        public int Seed { get; init; } = 1;
        public int TurnBudget { get; init; } = 60;

        public string Title => Name
            ?? Encounter
            ?? (Intent is { } pinned ? $"{Enemy} · {pinned}" : Enemy)
            ?? "an unnamed fight";
    }

    public static readonly JsonSerializerOptions SnapshotJson = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Snapshot Read(string path) =>
        JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), SnapshotJson)
        ?? throw new InvalidOperationException($"'{path}' is not a fight snapshot.");

    // ── the ring ──────────────────────────────────────────────────────────────────────────────────────────

    // A blueprint that is ONE FIGHT: the whole authored game — same statuses, cards, relics, enemy actions —
    // with the map replaced by a single combat node pointing at the encounter given. Keeping the rest of the
    // document is the point: a missing status registration or a missing executor fails here exactly as it
    // would in Godot, which a hand-built encounter would hide.
    public static RunBlueprint OneFight(
        RunBlueprint game, EncounterDefinition fight,
        IReadOnlyList<string>? deck = null, int? health = null,
        int? maxHealth = null, int? energy = null,
        IReadOnlyList<string>? relics = null, IReadOnlyList<string>? consumables = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(fight);

        var blueprint = game with
        {
            Encounters = [fight],
            Map = new RunMap([new Node(new NodeId("ring"), StandardRunIds.CombatNode, new EncounterRef(fight.Id))]),
            // The real game GENERATES its map per run, act by act, which would replace the one node with a
            // whole act drawn from encounters this blueprint no longer holds. A ring is one fight.
            MapGeneration = null,
            Acts = null,
        };

        var start = blueprint.Start;
        var touched = false;

        if (deck is not null and not { Count: 0 })
        {
            var cards = deck.Select(id => new CardDefinitionId(id)).ToList();
            blueprint = blueprint with { Deck = cards };
            start = start with { Deck = cards };
            touched = true;
        }
        if (maxHealth is { } cap)
        {
            start = start with { MaxHealth = cap, StartingHealth = cap };
            touched = true;
        }
        // Health AFTER MaxHealth, so a snapshot may say "80 max, standing on 12" and mean it.
        if (health is { } hp)
        {
            start = start with { StartingHealth = hp, MaxHealth = Math.Max(hp, start.MaxHealth) };
            touched = true;
        }
        if (relics is not null and not { Count: 0 })
        {
            start = start with { StartingRelics = [.. relics] };
            touched = true;
        }
        if (consumables is not null and not { Count: 0 })
        {
            start = start with { StartingConsumables = [.. consumables] };
            touched = true;
        }
        if (energy is { } pool)
        {
            start = start with
            {
                Resources = new Dictionary<string, int>(start.Resources, StringComparer.Ordinal)
                {
                    [StandardCombatIds.EnergyResource.value] = pool,
                },
            };
            touched = true;
        }

        // ⚠ THE CHARACTER ROSTER OVERRIDES Start, so a loadout stated here is silently ignored while a roster
        // stands: RunSetup reads whichever the pick names. A ring is one stated fighter, so the roster goes.
        return touched ? blueprint with { Start = start, Characters = [] } : blueprint;
    }

    // A solo encounter with one AUTHORED enemy: its real roster entry (health, passives carried from the first
    // bell, intent rules) taken from the converted game and optionally narrowed to one intent, plus any extra
    // statuses it should open with. Hand-building the entry instead would quietly drop the enemy's own
    // starting statuses — the very passives most of these questions are about.
    public static EncounterDefinition Solo(
        RunBlueprint game, string enemyId, string? intentId = null, int energy = 3,
        IReadOnlyList<(string Status, int Stacks)>? enemyStatuses = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        var authored = game.Encounters.SelectMany(e => e.Enemies).FirstOrDefault(e => e.Id == enemyId)
            ?? throw new InvalidOperationException(
                $"no authored encounter fields '{enemyId}'."
                + Near(enemyId, game.Encounters.SelectMany(e => e.Enemies).Select(e => e.Id)));

        var body = authored with
        {
            // An enemy narrowed to one action still obeys its own INTENT RULES, which is usually what is
            // wanted (a boss whose whole fight is its rules) — and when it is not, the rules are what a
            // caller strips, not this.
            Actions = intentId is null
                ? authored.Actions
                : [new EnemyActionDefinitionId($"{enemyId}.{intentId}")],
            StartingStatuses =
            [
                .. authored.StartingStatuses ?? [],
                .. (enemyStatuses ?? []).Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Status), s.Stacks)),
            ],
        };

        return new EncounterDefinition(new EncounterId($"ring.{enemyId}"), [body],
            [new ResourceSpec(StandardCombatIds.EnergyResource, energy, energy)],
            heroStartingStatuses: HeroStatuses(enemyId),
            triggeredEffects: EncounterPassives.ForEnemy(enemyId));
    }

    // The REAL authored encounter, exactly as the game fields it (roster, per-encounter health, intents), with
    // the hero's pool raised when the question needs several cards inside one turn.
    public static EncounterDefinition Authored(RunBlueprint game, string encounterId, int? energy = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        var authored = game.Encounters.FirstOrDefault(e => e.Id.Value == encounterId)
            ?? throw new InvalidOperationException(
                $"no encounter '{encounterId}'." + Near(encounterId, game.Encounters.Select(e => e.Id.Value)));

        return energy is not { } pool
            ? authored
            : new EncounterDefinition(authored.Id, authored.Enemies,
                [new ResourceSpec(StandardCombatIds.EnergyResource, pool, pool)],
                authored.HeroStartingStatuses, authored.HeroDisplayName, authored.CardsDrawnPerTurn,
                authored.TriggeredEffects);
    }

    // WHAT THEY PROBABLY MEANT. The ids in this game are long and the one in a designer's head is usually a
    // fragment of the real one — "tombbreakers" for `labyrinth_elite_the_tombbreakers_three`. An instrument
    // that answers a near miss with "no" and nothing else sends its user grepping the source data, which is
    // the errand it exists to save.
    private static string Near(string wanted, IEnumerable<string> known)
    {
        var needle = wanted.Replace("_", "", StringComparison.Ordinal);
        var close = known
            .Where(id => id.Contains(wanted, StringComparison.OrdinalIgnoreCase)
                || id.Replace("_", "", StringComparison.Ordinal)
                     .Contains(needle, StringComparison.OrdinalIgnoreCase)
                || wanted.Split('_', StringSplitOptions.RemoveEmptyEntries)
                     .Where(word => word.Length > 3)
                     .Any(word => id.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.Ordinal)
            .Take(8)
            .ToList();
        return close.Count == 0 ? "" : $" Did you mean: {string.Join(", ", close)}?";
    }

    // Every ring marks the hero exactly as EncounterMapper does — the applicant marker a passive needs to tell
    // "this happened to the player" — plus whatever the roster serves ON the player at the first bell and
    // whatever the ACT serves once per fight.
    public static IReadOnlyList<StartingStatusSpec> HeroStatuses(params string[] enemyIds) =>
    [
        new StartingStatusSpec(new StatusDefinitionId(PassiveStatuses.ApplicantId), 1),
        .. enemyIds.Distinct().SelectMany(EncounterPassives.HeroOpeningStatuses),
        .. EncounterPassives.ActOpeningStatuses(enemyIds),
    ];

    // The hero's own opening statuses, when the snapshot asks for some: the applicant marker and the room's
    // own openers are already on the encounter, so these are ADDED to them rather than replacing them.
    public static EncounterDefinition AgainstHero(
        EncounterDefinition fight, IReadOnlyDictionary<string, int> statuses)
    {
        ArgumentNullException.ThrowIfNull(fight);
        ArgumentNullException.ThrowIfNull(statuses);
        if (statuses.Count == 0)
            return fight;

        return new EncounterDefinition(fight.Id, fight.Enemies, fight.HeroResources,
            [.. fight.HeroStartingStatuses ?? [],
             .. statuses.Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Key), s.Value))],
            fight.HeroDisplayName, fight.CardsDrawnPerTurn, fight.TriggeredEffects);
    }

    // The whole ring, assembled from a snapshot: who is in it, and what the fighter brought.
    public static RunBlueprint Assemble(RunBlueprint game, Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var named = (snapshot.Encounter is { Length: > 0 }, snapshot.Enemy is { Length: > 0 });
        var fight = named switch
        {
            (true, false) => Authored(game, snapshot.Encounter!, snapshot.Energy),
            (false, true) => Solo(game, snapshot.Enemy!, snapshot.Intent, snapshot.Energy,
                [.. snapshot.EnemyStatuses.Select(s => (s.Key, s.Value))]),
            (true, true) => throw new InvalidOperationException(
                "a snapshot names both an encounter and an enemy; it fields one or the other."),
            _ => throw new InvalidOperationException(
                "a snapshot must name an 'encounter' or an 'enemy' to fight."),
        };

        return OneFight(
            game, AgainstHero(fight, snapshot.Statuses),
            deck: snapshot.Deck, health: snapshot.Health, maxHealth: snapshot.MaxHealth,
            energy: snapshot.Energy, relics: snapshot.Relics, consumables: snapshot.Consumables);
    }
}
