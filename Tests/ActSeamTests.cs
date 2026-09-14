using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The ACT SEAM: the run walks Act I, then Act II, then Act III, then Act IV, then Act V, and each act draws
// only from its own content. The fifth is the odd one: a gauntlet has no rooms, so the tests about furniture
// speak of the acts that HAVE any, and what Act V is checked for is that it has none.
//
// This pins the bug the audit found (ACT_I_II_COMPLETION_PLAN.md, A-2): the map rules used to group EVERY
// encounter that carried a role, so the city's boss row could draw the Grand Cross-Reference — the last boss
// of the next act — and its event nodes could open an archive door. Nothing in a run said which act it was in.
public class ActSeamTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260826);

    private static IEnumerable<string> PoolFor(RunAct act, MapNodeKind role) =>
        act.MapGeneration!.Encounters.For(role).Select(e => e.Encounter.Value);

    // The ids of the events this act AUTHORS (its fifteen), built the way the assembler builds them.
    private static IReadOnlyList<string> AuthoredIds(int act) =>
        Converter.Events.AuthoredEvents
            .For(act, ConversionPools.Build(act), new Random(1))
            .Select(e => e.Id)
            .ToList();

    // The acts that have rooms at all: everything but the gauntlet, which is three bosses and no furniture.
    private static IReadOnlyList<RunAct> Roomed =>
        [.. Game.Acts!.Where(act => act.MapGeneration!.Rows > 0)];

    private static int ActOf(string encounterId) =>
        Data.Encounters.First(e => e.Id == encounterId).Act;

    private static int CardAct(string cardId) =>
        Converter.Cards.FinalCards.All().First(c => c.Id == cardId).Act;

    // The card ids a role's victory reward can hand out in this act.
    private static IReadOnlyList<string> RewardCards(RunAct act, MapNodeKind role) =>
        CardsIn(((FixedRewardSource)act.MapGeneration!.VictoryRewards[role].Source).Offers);

    private static IReadOnlyList<string> ShopCards(RunAct act) =>
        Game.Shops[act.MapGeneration!.NodeRefs[MapNodeKind.Shop]].Stock
            .SelectMany(shelf => shelf.Offers)
            .SelectMany(entry => entry.Payload)
            .OfType<AddCardToDeckRunEffect>()
            .Select(e => e.Card.value)
            .ToList();

    // Every card an offer (or a nested offer) can add to the deck.
    private static IReadOnlyList<string> CardsIn(IEnumerable<RewardOffer> offers)
    {
        var cards = new List<string>();
        foreach (var effect in offers.SelectMany(o => o.Grant))
            switch (effect)
            {
                case AddCardToDeckRunEffect add:
                    cards.Add(add.Card.value);
                    break;
                case OfferRewardRunEffect offer when offer.Source is PoolRewardSource pool:
                    cards.AddRange(CardsIn(pool.Pool.Entries.Select(e => e.Value)));
                    break;
                case OfferRewardRunEffect offer when offer.Source is FixedRewardSource fixedSource:
                    cards.AddRange(CardsIn(fixedSource.Offers));
                    break;
            }
        return cards;
    }

    [Fact]
    public void The_run_walks_every_act_in_order()
    {
        Assert.Collection(Game.Acts!,
            first => Assert.Equal("act_1_city", first.Id),
            second => Assert.Equal("act_2_archives", second.Id),
            third => Assert.Equal("act_3_green_docket", third.Id),
            fourth => Assert.Equal("act_4_licensing_labyrinth", fourth.Id),
            fifth => Assert.Equal("act_5_divine_ledger", fifth.Id));
        Assert.All(Game.Acts!, act => Assert.NotNull(act.MapGeneration));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public void An_acts_encounter_pools_hold_only_that_acts_encounters(int index, int actNumber)
    {
        var act = Game.Acts![index];
        foreach (var role in new[]
                 { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Boss })
        {
            var pool = PoolFor(act, role).ToList();
            Assert.NotEmpty(pool);
            Assert.All(pool, id => Assert.Equal(actNumber, ActOf(id)));
        }
    }

    // The headline case: an Act-I run must not be able to end against an Act-II boss.
    [Fact]
    public void Each_act_ends_on_its_own_bosses()
    {
        var city = PoolFor(Game.Acts![0], MapNodeKind.Boss).ToList();
        var archives = PoolFor(Game.Acts![1], MapNodeKind.Boss).ToList();
        var road = PoolFor(Game.Acts![2], MapNodeKind.Boss).ToList();
        var labyrinth = PoolFor(Game.Acts![3], MapNodeKind.Boss).ToList();
        var ledger = PoolFor(Game.Acts![4], MapNodeKind.Boss).ToList();

        Assert.Equal(5, city.Count);
        Assert.Equal(5, archives.Count);
        Assert.Equal(5, road.Count);
        // …and the labyrinth ends on any of EIGHT, which is the act's own design (boss master §Act IV).
        Assert.Equal(8, labyrinth.Count);
        Assert.All(city, id => Assert.StartsWith("city_boss_", id, StringComparison.Ordinal));
        Assert.DoesNotContain("archives_boss_grand_cross_reference", city);
        Assert.All(archives, id => Assert.StartsWith("archives_boss_", id, StringComparison.Ordinal));
        Assert.All(road, id => Assert.StartsWith("green_docket_boss_", id, StringComparison.Ordinal));
        Assert.All(labyrinth, id => Assert.StartsWith("labyrinth_boss_", id, StringComparison.Ordinal));
        // …and the ledger on three of SIX gods, which is the whole of Act V (boss master §Act V §5).
        Assert.Equal(6, ledger.Count);
        Assert.All(ledger, id => Assert.StartsWith("act_5_", id, StringComparison.Ordinal));
        Assert.Empty(city.Intersect(archives));
        Assert.Empty(archives.Intersect(road));
        Assert.Empty(road.Intersect(labyrinth));
        Assert.Empty(labyrinth.Intersect(ledger));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public void An_acts_event_nodes_open_only_that_acts_events(int index, int actNumber)
    {
        var spec = Game.Acts![index].MapGeneration!;
        var events = spec.NodeRefPools[MapNodeKind.Event];

        Assert.NotEmpty(events);
        Assert.All(events, id =>
        {
            // Every door is one the act authors — and never the other act's.
            Assert.Contains(id, AuthoredIds(actNumber));
            Assert.True(Game.Events.ContainsKey(id), $"event '{id}' has no script");
        });
        Assert.Contains(spec.NodeRefs[MapNodeKind.Event], events);
    }

    // Shop, waiting room and treasure rooms are furniture, and each act keeps its own.
    [Fact]
    public void Each_act_brings_its_own_shop_rest_and_treasure_rooms()
    {
        var rooms = Roomed.Select(a => a.MapGeneration!.NodeRefs).ToList();

        // Nobody shares a counter or a chair with anybody.
        Assert.Equal(rooms.Count, rooms.Select(r => r[MapNodeKind.Shop]).Distinct().Count());
        Assert.Equal(rooms.Count, rooms.Select(r => r[MapNodeKind.Rest]).Distinct().Count());
        Assert.All(rooms, r => Assert.True(Game.Shops.ContainsKey(r[MapNodeKind.Shop])));
        Assert.All(rooms, r => Assert.True(Game.Events.ContainsKey(r[MapNodeKind.Rest])));

        var treasures = Roomed
            .Select(a => a.MapGeneration!.NodeRefPools[MapNodeKind.Treasure]).ToList();
        for (var a = 0; a < treasures.Count; a++)
            for (var b = a + 1; b < treasures.Count; b++)
                Assert.Empty(treasures[a].Intersect(treasures[b]));

        // …and the act that has no rooms references none: no shop, no waiting room, no jars, nothing.
        Assert.Empty(Game.Acts![4].MapGeneration!.NodeRefs);
        Assert.Empty(Game.Acts![4].MapGeneration!.NodeRefPools);
    }

    // ── What an act OFFERS (A-3 … A-6) ─────────────────────────────────────────────

    // EACH ACT ASKS MORE THAN THE ONE BEFORE IT — said about the ACT now, not about a route.
    //
    // The audit's per-path table used to say this ("the archives ask for two multi-enemy fights and two elites
    // where the city asks for one of each") and it is being retired: promising every route the same two elites
    // is what made every route the same walk (plan §1.2). The same design intent is now three sentences about
    // the act — it runs longer, it holds more trouble, and no walk through it is worth less than a walk through
    // the act before — and none of the three says what any particular route must contain.
    [Fact]
    public void Each_act_asks_more_of_the_player_than_the_one_before_it()
    {
        var acts = Game.Acts!.Take(4).Select(act => act.StrategicMapGeneration!).ToList();
        int Holds(StrategicActSpec spec, MapNodeKind kind) => spec.Rooms.RoomBudgets[kind].Target;

        for (var index = 1; index < acts.Count; index++)
        {
            var (before, after) = (acts[index - 1], acts[index]);
            Assert.True(after.Rows > before.Rows,
                $"act {index + 1} runs {after.Rows} rooms against act {index}'s {before.Rows}");
            Assert.True(after.PathPressure.Minimum > before.PathPressure.Minimum,
                $"act {index + 1} promises {after.PathPressure.Minimum} points a route against act {index}'s "
                + $"{before.PathPressure.Minimum}");
            Assert.True(Holds(after, MapNodeKind.Elite) > Holds(before, MapNodeKind.Elite),
                $"act {index + 1} holds {Holds(after, MapNodeKind.Elite)} elites against act {index}'s "
                + $"{Holds(before, MapNodeKind.Elite)}");
            Assert.True(Holds(after, MapNodeKind.MultiCombat) >= Holds(before, MapNodeKind.MultiCombat),
                $"act {index + 1} holds fewer crowded fights than act {index}");
            // …and each act is its own place: three ways across, and never the act before's three.
            Assert.NotEqual(
                before.LaneProfiles.Select(lane => lane.Name).ToList(),
                after.LaneProfiles.Select(lane => lane.Name).ToList());
        }

        // …while what an act KEEPS goes the other way once you leave the city: the archives hold one jar fewer
        // than the city and the road holds no more than the archives. Comfort is what the acts take away.
        Assert.True(Holds(acts[1], MapNodeKind.Treasure) < Holds(acts[0], MapNodeKind.Treasure));
        Assert.True(Holds(acts[2], MapNodeKind.Treasure) <= Holds(acts[1], MapNodeKind.Treasure));
    }

    // v0.0.0'S CONFIGURATION IS FROZEN, and this is the test that says so out loud. The rule-based generator
    // ships beside the strategic one so a playtester can walk the same act both ways (plan §4b), and a baseline
    // that quietly moved would be no baseline at all. The per-path table therefore still reads exactly as the
    // audit wrote it, and the act's backbone is still the five rows the retired `FreeRows` formula produced for
    // every act in the game. What those numbers PRODUCE is pinned in Tests/Golden/map-v0.0.0.txt.
    [Fact]
    public void The_old_generators_promises_are_the_ones_it_has_always_made()
    {
        var archives = Game.Acts![1].MapGeneration!;

        Assert.Equal(8, archives.PerPathMinimums[MapNodeKind.Combat]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.MultiCombat]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.Elite]);
        Assert.Equal(3, archives.PerPathMinimums[MapNodeKind.Event]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.Rest]);
        Assert.Equal(1, archives.PerPathMinimums[MapNodeKind.Treasure]);
        Assert.Equal(2, archives.PerPathMinimums[MapNodeKind.Shop]);

        Assert.All(Roomed, act => Assert.Equal(5, act.MapGeneration!.Rows));
    }

    // A treasure bites at the act's own rate, and what comes out of it is the act's own body.
    [Fact]
    public void Each_acts_treasure_bites_at_its_own_rate_with_its_own_mimic()
    {
        Assert.Equal(5, Game.Acts![0].MapGeneration!.TreasureMimicChancePercent);
        Assert.Equal(10, Game.Acts![1].MapGeneration!.TreasureMimicChancePercent);
        Assert.Equal(15, Game.Acts![2].MapGeneration!.TreasureMimicChancePercent);
        Assert.Equal(20, Game.Acts![3].MapGeneration!.TreasureMimicChancePercent);
        // The gauntlet has no treasure to bite: nothing to flip, and nothing to flip into.
        Assert.Equal(0, Game.Acts![4].MapGeneration!.TreasureMimicChancePercent);

        for (var index = 0; index < Roomed.Count; index++)
        {
            var mimics = PoolFor(Game.Acts![index], MapNodeKind.Mimic).ToList();
            Assert.NotEmpty(mimics);
            Assert.All(mimics, id => Assert.Equal(index + 1, ActOf(id)));
        }
    }

    // A-5: the card a fight pays out is gated to the act it was won in. Act II opens the archives' cards
    // WITHOUT closing the city's — the design gates a card at "Act N or earlier".
    [Fact]
    public void An_acts_fights_pay_out_that_acts_cards()
    {
        var city = RewardCards(Game.Acts![0], MapNodeKind.Combat);
        var archives = RewardCards(Game.Acts![1], MapNodeKind.Combat);

        Assert.NotEmpty(city);
        Assert.ProperSubset(archives.ToHashSet(), city.ToHashSet());
        Assert.All(city, id => Assert.Equal(1, CardAct(id)));
        Assert.Contains(archives, id => CardAct(id) == 2);

        // …and the road opens its own without closing either of theirs.
        var road = RewardCards(Game.Acts![2], MapNodeKind.Combat);
        Assert.ProperSubset(road.ToHashSet(), archives.ToHashSet());
        Assert.Contains(road, id => CardAct(id) == 3);

        // …and the labyrinth the same, one act further on.
        var labyrinth = RewardCards(Game.Acts![3], MapNodeKind.Combat);
        Assert.ProperSubset(labyrinth.ToHashSet(), road.ToHashSet());
        Assert.Contains(labyrinth, id => CardAct(id) == 4);
    }

    // The shop shelves are stocked from the same act-gated pool, so the archives' shop is not the city's.
    [Fact]
    public void An_acts_shop_stocks_that_acts_cards()
    {
        var city = ShopCards(Game.Acts![0]);
        var archives = ShopCards(Game.Acts![1]);

        Assert.NotEmpty(city);
        Assert.NotEmpty(archives);
        Assert.All(city, id => Assert.Equal(1, CardAct(id)));
        Assert.All(archives, id => Assert.True(CardAct(id) <= 2, $"'{id}' is gated past Act II"));
        Assert.NotEqual(city, archives);

        var road = ShopCards(Game.Acts![2]);
        Assert.NotEmpty(road);
        Assert.All(road, id => Assert.True(CardAct(id) <= 3, $"'{id}' is gated past Act III"));
        Assert.NotEqual(archives, road);

        var labyrinth = ShopCards(Game.Acts![3]);
        Assert.NotEmpty(labyrinth);
        Assert.All(labyrinth, id => Assert.True(CardAct(id) <= 4, $"'{id}' is gated past Act IV"));
        Assert.NotEqual(road, labyrinth);
    }

    // A-4: each act's campfire and treasure room are its own rooms, not the city's text under another id.
    [Fact]
    public void Each_act_reads_as_its_own_place()
    {
        string TextOf(RunAct act, MapNodeKind kind)
        {
            var script = Game.Events[act.MapGeneration!.NodeRefs[kind]];
            return script.Situations[script.StartSituationId].TextKey;
        }

        foreach (var kind in new[] { MapNodeKind.Rest, MapNodeKind.Treasure })
            Assert.Equal(Roomed.Count, Roomed.Select(act => TextOf(act, kind)).Distinct().Count());
    }

    // A-6: the title is the GAME's; naming an act belongs to the act.
    [Fact]
    public void The_title_names_the_game_and_the_acts_name_themselves()
    {
        Assert.Equal("Bureaucrats & Broomsticks", BlueprintAssembler.GameTitle);
        Assert.DoesNotContain("Act I", BlueprintAssembler.GameTitle, StringComparison.Ordinal);
        Assert.Equal("Bureaucrats & Broomsticks", Game.Presentation.Game!.FlavorText);

        Assert.Equal("Act I: The Old City Offices", Game.Acts![0].NameKey);
        Assert.Equal("Act II: The Endless Archives", Game.Acts![1].NameKey);
        Assert.Equal("Act III: The Green Docket", Game.Acts![2].NameKey);
        Assert.Equal("Act IV: The Licensing Labyrinth", Game.Acts![3].NameKey);
        Assert.Equal("Act V: The Divine Ledger", Game.Acts![4].NameKey);
    }

    // …and it holds where it counts: in the maps an actual run walks.
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20260826)]
    public void A_generated_run_meets_only_the_current_acts_encounters(int seed)
    {
        var plan = Game.BuildActPlan(seed, startingLoadout: 0);
        Assert.Equal(5, plan.Count);

        for (var index = 0; index < plan.Count; index++)
        {
            var fights = plan[index].Map.Nodes
                .Select(n => n.Payload).OfType<EncounterRef>()
                .Select(f => f.Id.Value).ToList();
            Assert.NotEmpty(fights);
            Assert.All(fights, id => Assert.Equal(index + 1, ActOf(id)));
        }
    }
}
