using RogueDeck.Run;

namespace BnbContent.Converter;

// The act's MAP RULES instead of a baked map: one MapGenerationSpec the engine generates a fresh Act-I layout
// from at the start of every run (RunSetup.CreateInitialRun), honouring the per-path minimums from
// docs/bnb-act-map-specs.md. What used to be decided once at conversion time — which fight sits where, which
// treasure is a mimic — is now decided per run, from the curated role pools (BabEncounter.role).
public sealed class ActMap
{
    public required MapGenerationSpec Spec { get; init; }
    public required Dictionary<string, EventScript> Events { get; init; }
    public required Dictionary<string, ShopDefinition> Shops { get; init; }
}

public static class MapSpecBuilder
{
    public const string ShopId = "city-shop";
    public const string RestEventId = "rest:waiting-room";

    // Per-path guarantees for Act I (docs/bnb-act-map-specs.md). Combat 8 counts ordinary fights; the duo is a
    // MultiCombat on top, and the enemy floor counts both plus the elite.
    private static readonly Dictionary<MapNodeKind, int> PerPathMinimums = new()
    {
        [MapNodeKind.Combat] = 8,
        [MapNodeKind.MultiCombat] = 1,
        [MapNodeKind.Elite] = 1,
        [MapNodeKind.Event] = 3,
        [MapNodeKind.Rest] = 2,
        [MapNodeKind.Treasure] = 2,
        [MapNodeKind.Shop] = 2,
    };

    // Ceilings: no single route may pile up the soft stuff. A path is guaranteed its two rests, two shops and
    // two treasures (above) and may hold at most one more of each, so a "safe" route cannot be farmed — and at
    // most two elites, so a greedy one cannot stack them either.
    private static readonly Dictionary<MapNodeKind, int> PerPathMaximums = new()
    {
        [MapNodeKind.Rest] = 3,
        [MapNodeKind.Treasure] = 3,
        [MapNodeKind.Shop] = 3,
        [MapNodeKind.Event] = 5,
        [MapNodeKind.Elite] = 2,
        [MapNodeKind.MultiCombat] = 2,
    };

    // The three flavours the act's columns are drawn from, so the routes actually feel different: the left is a
    // gauntlet of fights, the middle runs errands (events and shops), the right is the quiet, well-stocked way
    // round. Which column a path keeps to decides BOTH what it holds and the order it holds it in.
    private static readonly MapLaneProfile[] Lanes =
    [
        new("the long queue", new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 12,
            [MapNodeKind.MultiCombat] = 3,
            [MapNodeKind.Elite] = 2,
            [MapNodeKind.Event] = 2,
        }),
        new("errands", new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Event] = 7,
            [MapNodeKind.Shop] = 4,
            [MapNodeKind.Combat] = 5,
            [MapNodeKind.MultiCombat] = 1,
        }),
        new("the quiet corridor", new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Rest] = 6,
            [MapNodeKind.Treasure] = 5,
            [MapNodeKind.Combat] = 5,
            [MapNodeKind.Event] = 2,
        }),
    ];

    // Gold ranges per role, straight from the ported difficulty tiers.
    private static readonly Dictionary<MapNodeKind, (int Min, int Max)> Gold = new()
    {
        [MapNodeKind.Combat] = (25, 40),
        [MapNodeKind.MultiCombat] = (30, 45),
        [MapNodeKind.Mimic] = (35, 55),
        [MapNodeKind.Elite] = (45, 70),
        [MapNodeKind.Boss] = (90, 120),
    };

    public static ActMap Build(BabData data, ConversionPools pools, int seed)
    {
        var rng = new Random(seed);
        var events = new Dictionary<string, EventScript>
        {
            [RestEventId] = EventTemplates.Rest(data.Act.WaitingRoom?.HealPercent ?? 25),
        };

        // One treasure event per treasure a path can hold, so the pool can hand out distinct ones.
        var treasureIds = new List<string>();
        for (var i = 1; i <= PerPathMaximums[MapNodeKind.Treasure] + 1; i++)
        {
            var id = $"treasure:city-{i}";
            treasureIds.Add(id);
            events[id] = EventTemplates.Treasure(pools, id);
        }

        var spec = new MapGenerationSpec
        {
            Rows = 9,
            MinWidth = 2,
            MaxWidth = 4,
            PerPathMinimums = PerPathMinimums,
            PerPathMaximums = PerPathMaximums,
            LaneProfiles = Lanes,
            // Act I promises a lot per path (8 fights, the duo, the elite, 3 events, 2 rests, 2 treasures, 2
            // shops). As funnels those promises would be most of the map, and every route would read the same;
            // as full rows the city keeps its branches.
            WideGuaranteeRows = true,
            // Eight ordinary fights, the duo and the elite are all enemies the player must face.
            MinEnemiesPerPath = PerPathMinimums[MapNodeKind.Combat]
                + PerPathMinimums[MapNodeKind.MultiCombat] + PerPathMinimums[MapNodeKind.Elite],
            // Only used if the lanes above are ever cleared: the act's overall flavour in one table.
            KindWeights = new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Combat] = 10,
                [MapNodeKind.Event] = 4,
                [MapNodeKind.Treasure] = 2,
                [MapNodeKind.Rest] = 2,
                [MapNodeKind.Shop] = 1,
                [MapNodeKind.Elite] = 1,
            },
            Encounters = new EncounterDistribution { ByRole = PoolsByRole(data) },
            VictoryRewards = VictoryRewards(pools),
            TreasureMimicChancePercent = 5, // Act I; 10/15/20 in the later acts
            NodeRefs = new Dictionary<MapNodeKind, string>
            {
                [MapNodeKind.Shop] = ShopId,
                [MapNodeKind.Rest] = RestEventId,
                [MapNodeKind.Treasure] = treasureIds[0],
                [MapNodeKind.Event] = data.Events[0].Id,
            },
            NodeRefPools = new Dictionary<MapNodeKind, IReadOnlyList<string>>
            {
                [MapNodeKind.Event] = data.Events.Select(e => e.Id).ToList(),
                [MapNodeKind.Treasure] = treasureIds,
            },
        };

        return new ActMap
        {
            Spec = spec,
            Events = events,
            Shops = new Dictionary<string, ShopDefinition> { [ShopId] = ShopTemplate.Build(data, pools, rng) },
        };
    }

    // The curated pools: every encounter that carries a role, weighted as authored.
    private static Dictionary<MapNodeKind, IReadOnlyList<EncounterPoolEntry>> PoolsByRole(BabData data)
    {
        var byRole = new Dictionary<MapNodeKind, IReadOnlyList<EncounterPoolEntry>>();
        foreach (var group in data.Encounters.Where(e => e.Role is not null).GroupBy(e => Role(e.Role!)))
        {
            byRole[group.Key] = group
                .Select(e => new EncounterPoolEntry(new EncounterId(e.Id), Math.Max(1, (int)(e.Weight ?? 1))))
                .ToList();
        }

        foreach (var required in new[] { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Boss })
            if (!byRole.ContainsKey(required))
                throw new ConversionException("map generation", $"no encounter carries the '{required}' role");

        return byRole;
    }

    private static MapNodeKind Role(string role) => role switch
    {
        "combat" => MapNodeKind.Combat,
        "multi_combat" => MapNodeKind.MultiCombat,
        "elite" => MapNodeKind.Elite,
        "boss" => MapNodeKind.Boss,
        "mimic" => MapNodeKind.Mimic,
        var other => throw new ConversionException("map generation", $"unmapped encounter role '{other}'"),
    };

    // What a generated fight pays: gold plus a card offer, and a relic on top for the elite, the boss and the
    // mimic (which is tuned like a weak elite). The engine suffixes the id with the encounter.
    private static Dictionary<MapNodeKind, MapVictoryReward> VictoryRewards(ConversionPools pools)
    {
        var rewards = new Dictionary<MapNodeKind, MapVictoryReward>();
        foreach (var (role, (min, max)) in Gold)
        {
            var grant = new List<IRunEffectRequest>
            {
                // A SPREAD, rolled per fight from the run's own RNG — the same fight does not always pay the
                // same purse. (How much exactly is still open; the tiers are the ported difficulty bands.)
                new ChangeResourceRunEffect(StandardRunIds.Gold, min, max),
                new OfferRewardRunEffect(new RewardId($"cards:{role}"), pools.CardRewardSource(), 1),
            };
            if (role is MapNodeKind.Elite or MapNodeKind.Boss or MapNodeKind.Mimic)
                grant.Add(new OfferRewardRunEffect(new RewardId($"relic:{role}"),
                    pools.RelicGrantSource(null, $"{role} relic reward"), 1));

            rewards[role] = new MapVictoryReward(new FixedRewardSource([new RewardOffer("spoils", grant)]));
        }
        return rewards;
    }
}
