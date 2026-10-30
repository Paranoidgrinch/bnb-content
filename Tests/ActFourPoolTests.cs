using BnbContent.Converter;

namespace BnbContent.Tests;

// Act IV after the content cut of 2026-10-30, as the user approved it: twenty rows, six stages in a fixed order,
// nineteen standard bodies (three or four a stage), ten elites and seven bosses — a third more than Act I, so
// every pool is bigger. This file is the acceptance gate for that shape.
public class ActFourPoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    // The approved roster, stage by stage, in the order the act walks them.
    private static readonly (string Stage, string[] Bodies)[] Stages =
    [
        ("stage_stelae", ["crooked_rod_bearer", "reed_cord_surveyor", "silt_buried_farmer_shade"]),
        ("stage_granary", ["crocodile_of_the_short_measure", "hungry_grain_thief", "jar_seal_scarab_swarm"]),
        ("stage_works", ["stone_hauler_ushabti", "runaway_laborer", "fallen_capstone_golem"]),
        ("stage_linen", ["unfinished_mummy", "natron_bearer", "fourfold_vessel_guardian"]),
        ("stage_necropolis", ["cursed_loot_bearer", "false_door_finder", "jackal_of_the_western_gate"]),
        ("stage_two_truths", ["assessor_of_the_forty_two", "moon_cycle_ibis", "name_erasing_chisel_spirit", "eclipse_scarab"]),
    ];

    private static IEnumerable<BabEncounter> WithRole(string role) =>
        Data.Encounters.Where(e => e.Act == 4 && e.Role == role);

    private static IEnumerable<BabEncounter> Standard() =>
        WithRole("combat").Concat(WithRole("multi_combat"));

    private static string StageOf(BabEncounter e) =>
        Assert.Single(e.Tags ?? [], t => t.StartsWith("stage_", StringComparison.Ordinal));

    [Fact]
    public void The_act_holds_nineteen_bodies_and_they_are_the_approved_ones()
    {
        var fielded = Standard().SelectMany(e => e.Enemies).Distinct().Order().ToList();
        Assert.Equal(19, fielded.Count);
        Assert.Equal(Stages.SelectMany(s => s.Bodies).Order(), fielded);
        Assert.Equal(fielded, ActFour.Identities.Order());
    }

    [Fact]
    public void Six_stages_in_the_approved_order_each_fielding_only_its_own_bodies()
    {
        Assert.Equal(Stages.Select(s => s.Stage["stage_".Length..]), ActStages.Order[4]);

        foreach (var (stage, bodies) in Stages)
        {
            var rooms = Standard().Where(e => StageOf(e) == stage).ToList();
            Assert.InRange(rooms.Count, 3, 6);
            Assert.All(rooms, room => Assert.All(room.Enemies, id => Assert.Contains(id, bodies)));
            Assert.Equal(bodies.Order(), rooms.SelectMany(r => r.Enemies).Distinct().Order());
            // Every stage has at least one duo.
            Assert.Contains(rooms, r => r.Enemies.Count == 2);
        }
    }

    [Fact]
    public void Nobody_fights_three_at_once_outside_an_elite()
    {
        Assert.All(Standard(), e => Assert.InRange(e.Enemies.Count, 1, 2));
    }

    // …and every one of them is a body the act's own vocabulary knows about.
    [Fact]
    public void Every_body_is_a_party_to_the_labyrinth()
    {
        foreach (var id in Standard().SelectMany(e => e.Enemies).Distinct())
        {
            var enemy = Data.Enemies.Single(e => e.Id == id);
            Assert.Contains(enemy.StartingStatuses ?? [], s => s.Status == ActFour.LabyrinthBodyId);
        }
    }

    // Ten elites, each authored as one of the act's elite identities, each standing in a stage.
    [Fact]
    public void The_elite_pool_is_the_approved_ten()
    {
        string[] approved =
        [
            "labyrinth_elite_surveyor_of_the_errant_cord", "labyrinth_elite_sphinx_of_the_processional_measure",
            "labyrinth_elite_scarab_host_of_the_sealed_granary", "labyrinth_elite_rope_master_of_the_corvee",
            "labyrinth_elite_colossus_of_the_endless_procession",
            "labyrinth_elite_mummified_overseer_of_the_linen_house", "labyrinth_elite_the_tombbreakers_three",
            "labyrinth_elite_curse_of_the_opened_tomb", "labyrinth_elite_keeper_of_the_thirty_six_decans",
            "labyrinth_elite_ammit_devourer_of_the_unworthy",
        ];
        var elites = WithRole("elite").ToList();
        Assert.Equal(approved.Order(), elites.Select(e => e.Id).Order());

        foreach (var encounter in elites)
        {
            foreach (var id in encounter.Enemies)
                Assert.Contains(id, ActFour.EliteIdentities);
            Assert.NotNull(ActStages.EliteStage(4, encounter.Id, encounter.Tags, encounter.EarliestDepthPercent));
        }

        // Only the Tombbreakers field more than one body, and they field exactly three.
        var many = Assert.Single(elites, e => e.Enemies.Count > 1);
        Assert.Equal("labyrinth_elite_the_tombbreakers_three", many.Id);
        Assert.Equal(3, many.Enemies.Count);

        // The Rope-Master's hands are summoned, never fielded — the roster never names them.
        Assert.DoesNotContain(ActFour.StoneHaulerSummonEnemyId, Data.Enemies.Select(e => e.Id));
        Assert.Contains(ActFour.StoneHaulerSummonEnemyId, ActFour.EliteIdentities);
    }

    [Fact]
    public void Seven_bosses_and_the_vizier_is_gone()
    {
        var bosses = WithRole("boss").Select(e => e.Id).ToList();
        Assert.Equal(7, bosses.Count);
        Assert.DoesNotContain("labyrinth_boss_vizier_of_the_kings_mouth", bosses);
    }

    // Rescaled to roughly Act III: a normal body is no longer a 400-HP wall.
    [Fact]
    public void The_act_is_priced_near_act_three()
    {
        foreach (var id in Standard().SelectMany(e => e.Enemies).Distinct())
            Assert.InRange(Data.Enemies.Single(e => e.Id == id).MaxHp, 60, 200);
    }

    // Every intent an encounter can reach is authored, and every authored intent is reachable: a special one
    // only through an intent rule, everything else through the cycle.
    [Fact]
    public void Every_special_intent_is_reachable_through_a_rule()
    {
        foreach (var enemy in Data.Enemies.Where(e => Standard().Any(x => x.Enemies.Contains(e.Id))))
            foreach (var special in enemy.Intents.Where(i => i.Special == true))
                Assert.Contains(enemy.IntentRules ?? [], r => r.Action == special.Id);
    }
}
