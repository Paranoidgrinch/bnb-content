using RogueDeck.Run;

namespace BnbContent.Converter;

// An act's MAP RULES instead of a baked map: one MapGenerationSpec per act, which the engine generates a fresh
// layout from at the start of every run (RunSetup.CreateInitialRun / BuildActPlan), honouring the per-path
// minimums from docs/bnb-act-map-specs.md. What used to be decided once at conversion time — which fight sits
// where, which treasure is a mimic — is now decided per run, from the curated role pools (BabEncounter.role).
//
// Everything an act draws from is filtered to THAT act: its encounters, its events, its own shop, waiting room
// and treasure rooms. An act that pulled from the whole catalogue would end on another act's boss.
public sealed class ActMap
{
    public required MapGenerationSpec Spec { get; init; }
    public required Dictionary<string, EventScript> Events { get; init; }
    public required Dictionary<string, ShopDefinition> Shops { get; init; }
}

public static class MapSpecBuilder
{
    // Each act's furniture is its own: the city does not send you back to the archives' shop. The slug comes
    // from the manifest id ("act_2_archives" → "archives"), so a new act brings its own ids with it.
    public static string ShopId(BabActManifest act) => $"{Slug(act)}-shop";
    public static string RestEventId(BabActManifest act) => $"rest:{Slug(act)}-waiting-room";
    public static string TreasureId(BabActManifest act, int index) => $"treasure:{Slug(act)}-{index}";

    private static string Slug(BabActManifest act)
    {
        var parts = act.Id.Split('_', 3);
        return parts.Length == 3 ? parts[2] : act.Id;
    }

    // Per-path guarantees for Act I (docs/bnb-act-map-specs.md). Combat 8 counts ordinary fights; the duo is a
    // MultiCombat on top, and the enemy floor counts both plus the elite. Act II walks the same shape on a
    // longer backbone for now — its own rules are A-3 in ACT_I_II_COMPLETION_PLAN.md.
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

    public static ActMap Build(BabData data, ConversionPools pools, int seed, BabActManifest act)
    {
        var rng = new Random(seed);
        var events = new Dictionary<string, EventScript>
        {
            [RestEventId(act)] = EventTemplates.Rest(act.WaitingRoom?.HealPercent ?? 25),
        };

        // One treasure event per treasure a path can hold, so the pool can hand out distinct ones.
        var treasureIds = new List<string>();
        for (var i = 1; i <= PerPathMaximums[MapNodeKind.Treasure] + 1; i++)
        {
            var id = TreasureId(act, i);
            treasureIds.Add(id);
            events[id] = EventTemplates.Treasure(pools, id);
        }

        var actEvents = data.Events.Where(e => e.Act == act.Act).Select(e => e.Id).ToList();
        if (actEvents.Count == 0)
            throw new ConversionException($"act '{act.Id}'", "no event belongs to this act");

        var byRole = PoolsByRole(data, act);
        var spec = new MapGenerationSpec
        {
            // The act's own length: the original's steps_before_boss.
            Rows = act.Map.StepsBeforeBoss,
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
            Encounters = new EncounterDistribution { ByRole = byRole },
            VictoryRewards = VictoryRewards(pools),
            // A treasure only bites where the act HAS a mimic to field. Act I's chance is 5 % (10/15/20 in the
            // later acts); the archives name a mimic in their manifest but no encounter carries the role yet,
            // and a chance without a candidate would fail generation rather than surprise anyone.
            TreasureMimicChancePercent = byRole.ContainsKey(MapNodeKind.Mimic) ? MimicChance(act) : 0,
            NodeRefs = new Dictionary<MapNodeKind, string>
            {
                [MapNodeKind.Shop] = ShopId(act),
                [MapNodeKind.Rest] = RestEventId(act),
                [MapNodeKind.Treasure] = treasureIds[0],
                [MapNodeKind.Event] = actEvents[0],
            },
            NodeRefPools = new Dictionary<MapNodeKind, IReadOnlyList<string>>
            {
                [MapNodeKind.Event] = actEvents,
                [MapNodeKind.Treasure] = treasureIds,
            },
        };

        return new ActMap
        {
            Spec = spec,
            Events = events,
            Shops = new Dictionary<string, ShopDefinition>
            {
                [ShopId(act)] = ShopTemplate.Build(data, pools, rng),
            },
        };
    }

    private static int MimicChance(BabActManifest act) =>
        (int)Math.Round((act.Treasure?.MimicChance ?? 0.05) * 100);

    // The curated pools: every encounter of THIS act that carries a role, weighted as authored. The act filter
    // is the whole point — without it the city's boss row drew the archives' bosses too.
    private static Dictionary<MapNodeKind, IReadOnlyList<EncounterPoolEntry>> PoolsByRole(
        BabData data, BabActManifest act)
    {
        var byRole = new Dictionary<MapNodeKind, IReadOnlyList<EncounterPoolEntry>>();
        foreach (var group in data.Encounters
            .Where(e => e.Role is not null && e.Act == act.Act)
            .GroupBy(e => Role(e.Role!)))
        {
            byRole[group.Key] = group
                .Select(e => new EncounterPoolEntry(new EncounterId(e.Id), Math.Max(1, (int)(e.Weight ?? 1))))
                .ToList();
        }

        foreach (var required in new[] { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Boss })
            if (!byRole.ContainsKey(required))
                throw new ConversionException(
                    $"map generation for act '{act.Id}'", $"no encounter of this act carries the '{required}' role");

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
