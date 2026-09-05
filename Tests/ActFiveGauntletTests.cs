using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// ACT V IS NOT AN ACT. It is a gauntlet: three bosses back to back, drawn from six without repetition, with
// nothing whatever between them — no standards, no elites, no doors, no shop, no jars, no campfire, no
// healing, and no spoils of any kind (boss master §Act V §1). The build that enters is the build that wins.
//
// Every one of those is a thing that is easy to have by accident, because every OTHER act has it: a role
// weight left in a table, a reward inherited from a role, a treasure pool copied from the act above. So what
// this file mostly checks is absence — and, on the other side, that the six gods are really six (the Act-III
// relic finding: a pool nothing draws from looks exactly like a pool that works).
public class ActFiveGauntletTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260905);

    private static MapGenerationSpec Ledger => Game.Acts![4].MapGeneration!;

    private static IReadOnlyList<string> Gods =>
        [.. Ledger.Encounters.For(MapNodeKind.Boss).Select(e => e.Encounter.Value)];

    private static RunMap Walk(int seed) =>
        Game.BuildActPlan(seed, startingLoadout: 0)[4].Map;

    private static IReadOnlyList<EncounterRef> Fights(RunMap map) =>
        [.. map.Nodes.Select(n => n.Payload).OfType<EncounterRef>()];

    [Fact]
    public void The_act_is_three_boss_rooms_and_has_no_rooms_of_any_other_kind()
    {
        Assert.Equal(0, Ledger.Rows);
        Assert.Equal(3, Ledger.BossRooms);
        Assert.Empty(Ledger.PerPathMinimums);
        Assert.Empty(Ledger.PerPathMaximums);
        Assert.Empty(Ledger.KindWeights);
        Assert.Empty(Ledger.LaneProfiles);
        Assert.Equal(0, Ledger.MinEnemiesPerPath);

        // Nothing but bosses can be drawn, because nothing but bosses has anything to draw from.
        foreach (var role in new[]
                 {
                     MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Mimic,
                     MapNodeKind.Shop, MapNodeKind.Rest, MapNodeKind.Event, MapNodeKind.Treasure,
                 })
            Assert.Empty(Ledger.Encounters.For(role));
    }

    [Fact]
    public void The_gods_are_six_and_they_are_the_acts_own()
    {
        Assert.Equal(6, Gods.Count);
        Assert.All(Gods, id => Assert.Equal(5, Data.Encounters.First(e => e.Id == id).Act));
        Assert.Equal(Gods.Count, Gods.Distinct().Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20260905)]
    public void A_run_meets_three_different_gods_in_an_order_it_has_from_the_start(int seed)
    {
        var map = Walk(seed);
        var fought = Fights(map).Select(f => f.Id.Value).ToList();

        Assert.Equal(3, map.Nodes.Count);
        Assert.Equal(3, fought.Count);
        Assert.Equal(3, fought.Distinct().Count());
        Assert.All(fought, id => Assert.Contains(id, Gods));
        Assert.All(map.Nodes, node => Assert.Contains(MapNodeTags.Boss, node.Tags));

        // The order is a property of the MAP, and the whole run's maps are laid out when the run starts — so
        // the first room of Act V already knows which three gods it is, which is what lets it show them.
        Assert.Equal(fought, Fights(Walk(seed)).Select(f => f.Id.Value).ToList());
    }

    // The pool-that-nothing-draws-from finding (Act III's relics: 72 of 74 were unreachable and every test
    // passed). Six gods of which a run meets three is a pool of six only if all six can actually be met.
    [Fact]
    public void Every_one_of_the_six_gods_can_be_met()
    {
        var met = new HashSet<string>();
        for (var seed = 0; seed < 60; seed++)
            foreach (var fight in Fights(Walk(seed)))
                met.Add(fight.Id.Value);

        Assert.Equal([.. Gods.Order()], [.. met.Order()]);
    }

    // No gold, no card, no relic — not even the boss relic every other act's boss hands over. A god's fight
    // pays in the only currency the act has: the next god.
    [Fact]
    public void A_god_grants_nothing_for_being_beaten()
    {
        Assert.Empty(Ledger.VictoryRewards);
        Assert.Empty(Ledger.VictoryRewardsByEncounter);

        foreach (var fight in Fights(Walk(20260905)))
        {
            Assert.Null(fight.VictoryReward);
            Assert.Null(fight.VictoryRewardId);
        }
    }

    // Nothing heals between the gods either: the act has no waiting room to sit in and no jar to open.
    [Fact]
    public void There_is_nothing_between_the_gods()
    {
        Assert.Empty(Ledger.NodeRefs);
        Assert.Empty(Ledger.NodeRefPools);
        Assert.False(Game.Events.ContainsKey(MapSpecBuilder.RestEventId(Data.Acts[4])));
        Assert.False(Game.Shops.ContainsKey(MapSpecBuilder.ShopId(Data.Acts[4])));
    }

    // The act's one SHARED rule is a UI rule (boss master §Act V §4): every god owns a Divine Rule Area, and
    // an area with nothing to say is a panel the frontend cannot draw. So every god carries its own words —
    // and no other fight in the game does, or the panel would appear over an ordinary corridor brawl.
    [Fact]
    public void Every_god_names_its_own_divine_rule_area_and_nobody_else_has_one()
    {
        foreach (var id in Gods)
        {
            var extra = Game.Presentation.Encounters[id].Extra;
            Assert.False(string.IsNullOrWhiteSpace(extra.GetValueOrDefault(ActFive.RuleTitleKey)),
                $"'{id}' has no divine rule area to show");
            Assert.False(string.IsNullOrWhiteSpace(extra.GetValueOrDefault(ActFive.RuleTextKey)),
                $"'{id}' has a rule area that says nothing");
        }

        // Six areas, six different titles: "always in the same place, completely different by boss".
        Assert.Equal(6, Gods
            .Select(id => Game.Presentation.Encounters[id].Extra[ActFive.RuleTitleKey]).Distinct().Count());

        foreach (var (id, presentation) in Game.Presentation.Encounters)
            if (!Gods.Contains(id))
                Assert.DoesNotContain(ActFive.RuleTextKey, presentation.Extra.Keys);
    }

    // The map is an ORDER, not a choice: one way in, one way on, one way out.
    [Fact]
    public void The_act_offers_no_route_only_a_sequence()
    {
        var map = Walk(20260905);
        Assert.Single(map.EntryNodeIds);
        foreach (var node in map.Nodes)
            Assert.True(map.Edges.Count(e => e.From == node.Id) <= 1,
                $"'{node.Id}' offers a choice, and the gauntlet has none to offer");
    }
}
