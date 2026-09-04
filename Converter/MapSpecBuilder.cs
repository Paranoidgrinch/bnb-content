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

    // The rows the act keeps for itself, once its per-path promises have taken theirs. The floor is not a
    // taste: every promise is a full row EVERY route crosses, so the free rows are the only ones where two
    // routes can hold different kinds of room, and below five the fightiest and the quietest way through the
    // city stop differing at all (EndToEndSmokeTests.The_routes_through_the_act_differ…).
    private const int MinimumFreeRows = 5;

    private static int FreeRows(BabActManifest act, ActRules rules) =>
        Math.Max(MinimumFreeRows, act.Map.StepsBeforeBoss - rules.PerPathMinimums.Values.Sum());

    private static string Slug(BabActManifest act)
    {
        var parts = act.Id.Split('_', 3);
        return parts.Length == 3 ? parts[2] : act.Id;
    }

    // Gold ranges per role, straight from the ported difficulty tiers.
    private static readonly Dictionary<MapNodeKind, (int Min, int Max)> Gold = new()
    {
        [MapNodeKind.Combat] = (25, 40),
        [MapNodeKind.MultiCombat] = (30, 45),
        [MapNodeKind.Mimic] = (35, 55),
        [MapNodeKind.Elite] = (45, 70),
        [MapNodeKind.Boss] = (90, 120),
    };

    public static ActMap Build(
        BabData data, ConversionPools pools, int seed, BabActManifest act,
        IReadOnlyList<Events.BnbEvent> authoredEvents)
    {
        var rng = new Random(seed);
        var rules = ActRules.For(act);
        var events = new Dictionary<string, EventScript>
        {
            [RestEventId(act)] = EventTemplates.Rest(act.WaitingRoom?.HealPercent ?? 25, rules),
        };

        // One treasure event per treasure a path can hold, so the pool can hand out distinct ones.
        var treasureIds = new List<string>();
        for (var i = 1; i <= rules.PerPathMaximums[MapNodeKind.Treasure] + 1; i++)
        {
            var id = TreasureId(act, i);
            treasureIds.Add(id);
            events[id] = EventTemplates.Treasure(pools, id, rules);
        }

        // The act's doors, all fifteen of them authored (Events/AuthoredEvents).
        var actEvents = authoredEvents.Select(e => e.Id).ToList();
        if (actEvents.Count == 0)
            throw new ConversionException($"act '{act.Id}'", "no event belongs to this act");

        var byRole = PoolsByRole(data, act);
        var spec = new MapGenerationSpec
        {
            // How many FREE rows the act gets on top of its promises.
            //
            // Two prescriptions disagree here and the newer one wins. The audit's per-path table
            // (docs/bnb-act-map-specs.md) already fixes an act's length: every promise becomes a full row every
            // route crosses, so Act I's nineteen promises ARE nineteen rooms. The manifest's `steps_before_boss`
            // is the ported v2 number from a map model that had no such promises, and adding it on top made a
            // nine-stage act twenty-eight rooms long. So it counts toward the promises rather than after them,
            // and what is left over (never less than a few, or no route could differ from another) is the part
            // of the act the player's chosen lane actually decides.
            Rows = FreeRows(act, rules),
            MinWidth = 2,
            MaxWidth = 4,
            PerPathMinimums = rules.PerPathMinimums,
            PerPathMaximums = rules.PerPathMaximums,
            LaneProfiles = [.. rules.Lanes],
            // An act promises a lot per path (Act I: 8 fights, the duo, the elite, 3 events, 2 rests, 2
            // treasures, 2 shops). As funnels those promises would be most of the map and every route would
            // read the same; as full rows the act keeps its branches.
            WideGuaranteeRows = true,
            // The ordinary fights, the multi-enemy ones and the elites are all enemies the player must face.
            MinEnemiesPerPath = rules.PerPathMinimums[MapNodeKind.Combat]
                + rules.PerPathMinimums[MapNodeKind.MultiCombat] + rules.PerPathMinimums[MapNodeKind.Elite],
            KindWeights = rules.KindWeights,
            Encounters = new EncounterDistribution { ByRole = byRole },
            VictoryRewards = VictoryRewards(pools),
            // A boss pays out what its ROLE pays plus one of its own three relics — so the payout has to be
            // stated per boss rather than per role (docs: BnB_Final_Relics_Master_PostAudit.md §6).
            VictoryRewardsByEncounter = BossRewards(data, pools, act),
            // A treasure only bites where the act HAS a mimic to field (5 / 10 / 15 / 20 % across the acts,
            // from the act's own manifest); a chance without a candidate would fail generation.
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
            // The design's "Earliest Stage N", act by act: a door the archives only open at the far end of the
            // aisle is filtered out of the shallow rows. An act whose events name no stage gates nothing.
            // Where each KIND of room may first stand (ActRules.EarliestDepthPercent) — the act's difficulty
            // curve, as opposed to its contents.
            RoleMinimumDepthPercent = rules.EarliestDepthPercent,
            NodeRefMinimumDepthPercent = authoredEvents
                .Where(e => e.EarliestDepthPercent > 0)
                .ToDictionary(e => e.Id, e => e.EarliestDepthPercent),
            // And which FIGHT may stand where: the elite masters' "earliest depth/stage" tables, carried on
            // the encounters themselves. The role gate above says an elite waits until a fifth of the way in;
            // this says which elite the act opens with and which one it keeps for the last quarter.
            EncounterMinimumDepthPercent = data.Encounters
                .Where(e => e.Act == act.Act && e.Role is not null && e.EarliestDepthPercent > 0)
                .ToDictionary(e => e.Id, e => e.EarliestDepthPercent!.Value),
        };

        return new ActMap
        {
            Spec = spec,
            Events = events,
            Shops = new Dictionary<string, ShopDefinition>
            {
                [ShopId(act)] = ShopTemplate.Build(pools, rng),
            },
        };
    }

    // Each boss of this act: its role's gold and card offer, and then ONE of its own three relics at random,
    // taken without a choice screen — a single-offer reward the player does not pick from.
    private static Dictionary<string, MapVictoryReward> BossRewards(
        BabData data, ConversionPools pools, BabActManifest act)
    {
        var rewards = new Dictionary<string, MapVictoryReward>();
        foreach (var boss in data.Encounters.Where(e => e.Act == act.Act && e.Role == "boss"))
        {
            var three = Relics.BossRelics.For(boss.Name);
            if (three.Count == 0)
                throw new ConversionException($"boss '{boss.Id}'", $"no boss relics are authored for '{boss.Name}'");

            var (min, max) = Gold[MapNodeKind.Boss];
            rewards[boss.Id] = new MapVictoryReward(new FixedRewardSource(
            [
                new RewardOffer("spoils",
                [
                    new ChangeResourceRunEffect(StandardRunIds.Gold, min, max),
                    // Both nested rewards SAY what they are. A reward that does not is announced as a card,
                    // which is how the one thing a boss is fought for — its relic — reached the player behind
                    // the words "a card reward", twice over and under the same heading as the card.
                    new OfferRewardRunEffect(new RewardId($"cards:{boss.Id}"), pools.CardRewardSource(), 1)
                        { Kind = RewardKinds.Card },
                    new OfferRewardRunEffect(
                        new RewardId($"relic:{boss.Id}"),
                        new PoolRewardSource(
                            new RunPool<RewardOffer>(three
                                .Select(relic => new RunPool<RewardOffer>.Entry(
                                    new RewardOffer($"relic-{relic.Id}",
                                        [new AddRelicByIdRunEffect(new RelicId(relic.Id))]), 1))
                                .ToList()),
                            1),
                        1)
                        { Kind = RewardKinds.Relic },
                ]),
            ]));
        }
        return rewards;
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
                new OfferRewardRunEffect(new RewardId($"cards:{role}"), pools.CardRewardSource(), 1)
                    { Kind = RewardKinds.Card },
            };
            if (role is MapNodeKind.Elite or MapNodeKind.Boss or MapNodeKind.Mimic)
                grant.Add(new OfferRewardRunEffect(new RewardId($"relic:{role}"),
                    pools.RelicGrantSource(null, $"{role} relic reward"), 1)
                    { Kind = RewardKinds.Relic });

            rewards[role] = new MapVictoryReward(new FixedRewardSource([new RewardOffer("spoils", grant)]));
        }
        return rewards;
    }
}
