using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Converter;

// Bakes ONE Act-1 map from the original's staged_pilgrimage generator rules (bab/run/map.py), seeded —
// the engine's map is authored data, so the converter rolls the layout ONCE and freezes it. Same lane
// walk (start width 2–4, split chance 0.35 + 0.20·streak capped at 0.85, ≥2 lanes per row), same
// per-depth node-type weights with the act's caps (3 events / 1 treasure / 2 elites, elites from depth
// 6), depth 9 forced to the waiting room, boss at depth 10.
//
// Realization notes (ADAPTATIONS.md): an event roll converts to a plain combat with the act's
// event_combat_chance (baked, so the "surprise fight" is visible on the map); treasure rolls the mimic
// up front; ONE depth-5 combat becomes the city shop so the game exercises the shop machinery.
public sealed class BakedMap
{
    public required RunMap Map { get; init; }
    public required Dictionary<string, EventScript> Events { get; init; }
    public required Dictionary<string, ShopDefinition> Shops { get; init; }
}

public static class MapBaker
{
    private static readonly (string Kind, double Weight)[][] DepthWeights =
    [
        [("combat", 1.0)],                                                        // depth 1
        [("combat", 1.0)],                                                        // depth 2
        [("combat", 7.0), ("event", 3.0)],                                        // depth 3
        [("combat", 5.0), ("event", 2.0), ("treasure", 1.0)],                     // depth 4
        [("combat", 6.0), ("event", 2.0), ("treasure", 1.0)],                     // depth 5
        [("combat", 5.0), ("event", 2.0), ("treasure", 1.0), ("elite", 1.0)],     // depth 6
        [("combat", 5.0), ("event", 2.0), ("treasure", 1.0), ("elite", 1.0)],     // depth 7
        [("combat", 6.0), ("event", 1.0), ("treasure", 1.0), ("elite", 2.0)],     // depth 8
        [("waiting_room", 1.0)],                                                  // depth 9
    ];

    private static readonly IReadOnlyDictionary<string, (int Min, int Max)> GoldByDifficulty =
        new Dictionary<string, (int, int)>
        {
            ["easy"] = (15, 25),
            ["normal"] = (25, 40),
            ["mimic"] = (25, 40),
            ["elite"] = (60, 90),
            ["boss"] = (100, 140),
        };

    public static BakedMap Bake(BabData data, ConversionPools pools, int seed)
    {
        var rng = new Random(seed);
        var settings = data.Act.Map;
        var width = settings.Width;
        var steps = settings.StepsBeforeBoss;
        if (steps != 9)
            throw new ConversionException("act map", "staged pilgrimage expects exactly 9 steps before the boss");
        const int firstEliteDepth = 6;

        var encountersByDifficulty = data.Encounters
            .GroupBy(e => e.Difficulty)
            .ToDictionary(g => g.Key, g => g.ToList());
        var unusedByDifficulty = encountersByDifficulty.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        var unusedEvents = data.Events.ToList();

        var nodes = new List<Node>();
        var edges = new List<MapEdge>();
        var layout = new List<NodeLayout>();
        var events = new Dictionary<string, EventScript>();
        var shops = new Dictionary<string, ShopDefinition>();
        var counts = new Dictionary<string, int> { ["events"] = 0, ["treasures"] = 0, ["elites"] = 0 };
        string? shopNodeId = null;

        // ── the lane walk ────────────────────────────────────────────────────────────
        var startWidth = rng.Next(2, Math.Min(width, 4) + 1);
        var currentLanes = Enumerable.Range(0, width).OrderBy(_ => rng.Next()).Take(startWidth).OrderBy(l => l).ToList();
        var streak = currentLanes.ToDictionary(l => l, _ => 0);
        var rows = new List<List<int>> { currentLanes };
        var edgePairs = new List<(string From, string To)>();

        for (var depth = 2; depth <= steps; depth++)
        {
            var outgoing = new Dictionary<int, SortedSet<int>>();
            foreach (var lane in currentLanes)
            {
                var lanes = new SortedSet<int> { lane };
                var adjacent = new[] { lane - 1, lane + 1 }.Where(c => c >= 0 && c < width).ToList();
                var splitChance = Math.Min(0.35 + 0.20 * streak.GetValueOrDefault(lane), 0.85);
                if (adjacent.Count > 0 && rng.NextDouble() < splitChance)
                    lanes.Add(adjacent[rng.Next(adjacent.Count)]);
                outgoing[lane] = lanes;
            }
            var next = new SortedSet<int>(outgoing.Values.SelectMany(s => s));
            if (next.Count < 2)
            {
                var only = next.First();
                var adjacent = new[] { only - 1, only + 1 }.Where(c => c >= 0 && c < width).ToList();
                if (adjacent.Count > 0)
                {
                    var extra = adjacent[rng.Next(adjacent.Count)];
                    next.Add(extra);
                    outgoing[currentLanes[rng.Next(currentLanes.Count)]].Add(extra);
                }
            }
            foreach (var (from, lanes) in outgoing)
                foreach (var lane in lanes.Where(next.Contains))
                    edgePairs.Add((NodeName(depth - 1, from), NodeName(depth, lane)));

            var newStreak = new Dictionary<int, int>();
            foreach (var lane in next)
            {
                var predecessors = outgoing.Where(kv => kv.Value.Contains(lane)).Select(kv => kv.Key).ToList();
                newStreak[lane] = predecessors.Count == 1 && outgoing[predecessors[0]].Count == 1
                    ? streak.GetValueOrDefault(predecessors[0]) + 1
                    : 0;
            }
            streak = newStreak;
            currentLanes = next.ToList();
            rows.Add(currentLanes);
        }

        // ── realize every rolled node ────────────────────────────────────────────────
        for (var depth = 1; depth <= steps; depth++)
        {
            foreach (var lane in rows[depth - 1])
            {
                var id = NodeName(depth, lane);
                var kind = ChooseKind(rng, depth, firstEliteDepth, counts, settings);
                if (kind == "event" && rng.NextDouble() < settings.EventCombatChance)
                    kind = "combat"; // the original's hidden event-combat, baked visibly

                // ADAPTATION: the first depth-5 combat becomes the city shop (the staged map has no
                // shop node type, but the ported game should exercise the shop machinery).
                if (kind == "combat" && depth == 5 && shopNodeId is null)
                {
                    shopNodeId = id;
                    shops["city-shop"] = BuildShop(data, pools, rng);
                    nodes.Add(new Node(new NodeId(id), StandardRunIds.ShopNode, new ShopRef(new ShopId("city-shop"))));
                }
                else
                {
                    nodes.Add(Realize(id, kind, depth, data, pools, rng, unusedByDifficulty, encountersByDifficulty,
                        unusedEvents, events));
                }
                layout.Add(new NodeLayout(new NodeId(id), 40 + lane * 150, depth * 95));
            }
        }

        // ── the boss ─────────────────────────────────────────────────────────────────
        const string bossId = "act_1_boss";
        nodes.Add(new Node(new NodeId(bossId), StandardRunIds.CombatNode,
            CombatRef("boss", data, pools, rng, unusedByDifficulty, encountersByDifficulty, includeRelic: true)));
        layout.Add(new NodeLayout(new NodeId(bossId), 40 + (width - 1) * 75, (steps + 1) * 95));
        foreach (var lane in rows[^1])
            edgePairs.Add((NodeName(steps, lane), bossId));

        edges.AddRange(edgePairs.Select(p => new MapEdge(new NodeId(p.From), new NodeId(p.To))));
        var map = new RunMap(nodes)
        {
            Edges = edges,
            EntryNodeIds = rows[0].Select(l => new NodeId(NodeName(1, l))).ToList(),
            Layout = layout,
        };
        return new BakedMap { Map = map, Events = events, Shops = shops };
    }

    private static string NodeName(int depth, int lane) => $"act_1_d{depth:00}_n{lane:00}";

    private static string ChooseKind(
        Random rng, int depth, int firstEliteDepth, Dictionary<string, int> counts, BabMapSettings settings)
    {
        var caps = new Dictionary<string, (string Key, int Max)>
        {
            ["event"] = ("events", settings.MaxEvents),
            ["treasure"] = ("treasures", settings.MaxTreasures),
            ["elite"] = ("elites", settings.MaxElites),
        };
        var population = DepthWeights[depth - 1]
            .Where(entry => entry.Kind != "elite" || depth >= firstEliteDepth)
            .Where(entry => !caps.TryGetValue(entry.Kind, out var cap) || counts[cap.Key] < cap.Max)
            .ToList();
        var kind = population.Count == 0 ? "combat" : WeightedPick(rng, population);
        if (caps.TryGetValue(kind, out var chosen))
            counts[chosen.Key]++;
        return kind;
    }

    private static string WeightedPick(Random rng, List<(string Kind, double Weight)> population)
    {
        var roll = rng.NextDouble() * population.Sum(p => p.Weight);
        foreach (var (kind, weight) in population)
        {
            roll -= weight;
            if (roll <= 0)
                return kind;
        }
        return population[^1].Kind;
    }

    private static Node Realize(
        string id, string kind, int depth, BabData data, ConversionPools pools, Random rng,
        Dictionary<string, List<BabEncounter>> unused, Dictionary<string, List<BabEncounter>> all,
        List<BabEvent> unusedEvents, Dictionary<string, EventScript> events)
    {
        switch (kind)
        {
            case "combat":
                return new Node(new NodeId(id), StandardRunIds.CombatNode,
                    CombatRef(depth <= 3 ? "easy" : "normal", data, pools, rng, unused, all, includeRelic: false));
            case "elite":
                return new Node(new NodeId(id), StandardRunIds.CombatNode,
                    CombatRef("elite", data, pools, rng, unused, all, includeRelic: true));
            case "event":
            {
                var eventType = rng.NextDouble() * 15 < 8 ? "risk_reward" : "deck"; // 8 : 7
                var pool = unusedEvents.Where(e => e.EventType == eventType).ToList();
                if (pool.Count == 0)
                    pool = unusedEvents.ToList();
                if (pool.Count == 0)
                    pool = data.Events.ToList();
                var picked = WeightedPickEvent(rng, pool);
                unusedEvents.Remove(picked);
                return new Node(new NodeId(id), StandardRunIds.EventNode, new EventRef(new EventId(picked.Id)));
            }
            case "treasure":
            {
                if (data.Act.Treasure is { } treasure && rng.NextDouble() < treasure.MimicChance
                    && treasure.MimicEncounterId is { } mimic)
                    return new Node(new NodeId(id), StandardRunIds.CombatNode,
                        CombatRef("mimic", data, pools, rng, unused, all, includeRelic: true, exactId: mimic));
                var eventId = $"treasure:{id}";
                events[eventId] = TreasureEvent(pools, eventId);
                return new Node(new NodeId(id), StandardRunIds.EventNode, new EventRef(new EventId(eventId)));
            }
            case "waiting_room":
            {
                var eventId = $"rest:{id}";
                events[eventId] = RestEvent(data.Act.WaitingRoom?.HealPercent ?? 25);
                return new Node(new NodeId(id), StandardRunIds.EventNode, new EventRef(new EventId(eventId)));
            }
            default:
                throw new ConversionException($"map node '{id}'", $"unmapped baked node kind '{kind}'");
        }
    }

    private static EncounterRef CombatRef(
        string difficulty, BabData data, ConversionPools pools, Random rng,
        Dictionary<string, List<BabEncounter>> unused, Dictionary<string, List<BabEncounter>> all,
        bool includeRelic, string? exactId = null)
    {
        BabEncounter encounter;
        if (exactId is not null)
        {
            encounter = all.Values.SelectMany(v => v).FirstOrDefault(e => e.Id == exactId)
                ?? throw new ConversionException("map", $"references unknown encounter '{exactId}'");
        }
        else
        {
            if (!all.TryGetValue(difficulty, out var tier) || tier.Count == 0)
                throw new ConversionException("map", $"no encounters of difficulty '{difficulty}'");
            var pool = unused[difficulty];
            if (pool.Count == 0)
                pool.AddRange(tier); // every encounter used once → allow repeats
            var weights = pool.Select(e => (e, Math.Max(0.01, e.Weight ?? 1))).ToList();
            var roll = rng.NextDouble() * weights.Sum(w => w.Item2);
            encounter = weights[^1].e;
            foreach (var (candidate, weight) in weights)
            {
                roll -= weight;
                if (roll <= 0) { encounter = candidate; break; }
            }
            pool.Remove(encounter);
        }

        var (min, max) = GoldByDifficulty.TryGetValue(difficulty, out var range) ? range : (25, 40);
        var grant = new List<IRunEffectRequest>
        {
            new ChangeResourceRunEffect(StandardRunIds.Gold, rng.Next(min, max + 1)),
            new OfferRewardRunEffect(new RewardId($"cards:{encounter.Id}"), pools.CardRewardSource(), 1),
        };
        if (includeRelic)
            grant.Add(new OfferRewardRunEffect(new RewardId($"relic:{encounter.Id}"),
                pools.RelicGrantSource(null, $"encounter '{encounter.Id}' relic reward"), 1));

        return new EncounterRef(new EncounterId(encounter.Id),
            VictoryReward: new FixedRewardSource([new RewardOffer("spoils", grant)]),
            VictoryRewardId: new RewardId($"spoils:{encounter.Id}"));
    }

    private static BabEvent WeightedPickEvent(Random rng, List<BabEvent> pool)
    {
        var roll = rng.NextDouble() * pool.Sum(e => Math.Max(0.01, e.Weight ?? 1));
        foreach (var candidate in pool)
        {
            roll -= Math.Max(0.01, candidate.Weight ?? 1);
            if (roll <= 0)
                return candidate;
        }
        return pool[^1];
    }

    private static EventScript TreasureEvent(ConversionPools pools, string where) => new("start",
    [
        new EventSituation("start",
            "A sealed evidence crate, stamped in three colors of wax. Nobody has claimed it in decades.",
        [
            new EventChoice("open",
            [
                new OfferRewardRunEffect(new RewardId($"{where}:relic"), pools.RelicGrantSource(null, where), 1),
            ], TextKey: "Break the seals"),
            new EventChoice("leave", [], TextKey: "Leave it for the archivists"),
        ]),
    ]);

    private static EventScript RestEvent(int healPercent) => new("start",
    [
        new EventSituation("start",
            "The waiting room. The chairs are terrible, but nobody can reach you here.",
        [
            new EventChoice("rest",
            [
                new ComputedHealRunEffect(RunExpr.Divide(
                    RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(healPercent)), RunExpr.Const(99)),
                    RunExpr.Const(100))),
            ], TextKey: $"Wait it out (heal {healPercent}% of max HP)"),
            new EventChoice("leave", [], TextKey: "Skip the queue"),
        ]),
    ]);

    // The city shop: five rarity-weighted cards at the original's base prices, two relics (pickup
    // effects bundled), the card-removal service, and a paid reroll.
    private static ShopDefinition BuildShop(BabData data, ConversionPools pools, Random rng)
    {
        var cardPrices = new Dictionary<string, int> { ["common"] = 55, ["uncommon"] = 85, ["rare"] = 130 };
        var relicPrices = new Dictionary<string, int> { ["common"] = 130, ["uncommon"] = 190, ["rare"] = 260 };

        var cards = pools.RewardCards.OrderBy(_ => rng.Next()).Take(5).ToList();
        var relics = pools.Relics.OrderBy(_ => rng.Next()).Take(2).ToList();

        var entries = new List<ShopEntry>();
        foreach (var card in cards)
        {
            var id = CardMapper.MapCardId(card.Id);
            entries.Add(new ShopEntry($"buy-{id}", StandardRunIds.Gold,
                cardPrices.GetValueOrDefault(card.Rarity ?? "common", 85),
                [new AddCardToDeckRunEffect(new CardDefinitionId(id))], card.Name));
        }
        foreach (var relic in relics)
        {
            entries.Add(new ShopEntry($"buy-{relic.Relic.Id}", StandardRunIds.Gold,
                relicPrices.GetValueOrDefault(relic.Source.Rarity ?? "common", 190),
                ConversionPools.RelicOffer(relic).Grant, relic.Source.Name));
        }
        return new ShopDefinition(entries,
            OfferCount: entries.Count,
            Reroll: new ShopReroll(StandardRunIds.Gold, 25),
            Services: [ShopService.RemoveCard(StandardRunIds.Gold, 75)]);
    }
}
