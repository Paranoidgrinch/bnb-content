using BnbContent.Converter;

namespace BnbContent.Tests;

// Act IV as the master counts it, and the acceptance gate for the whole standard pool: **35 identities, 55
// encounter templates, 17 stages** — plus the balance appendix's HP band for every body the act fields.
//
// The count of 35 is a count of IDENTITIES, not of bodies. Stages 16 and 17 introduce no new ones: each of
// their five figures is an earlier identity in the office the labyrinth promoted it into, so the act fields
// 40 rosters and the master's roster still reads 35. That distinction is the reason this file exists — it is
// exactly the sort of thing a pool silently gets wrong.
public class ActFourPoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    // The five bodies of Stages 16 and 17, each mapped to the identity it is the final form OF. The master's
    // roster counts the identity once, at its debut stage.
    private static readonly IReadOnlyDictionary<string, string> FinalForms =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["feather_bearer"] = "crooked_rod_bearer",
            ["crocodile_beneath_the_balance"] = "crocodile_of_the_short_measure",
            ["golden_ushabti_captain"] = "stone_hauler_ushabti",
            ["eternal_reed_scribe"] = "palette_bearing_apprentice",
            ["oathbound_gate"] = "cornerstone_oath_stone",
        };

    // The balance appendix, stage by stage: the solo-HP band every body is priced into. Written out rather
    // than parsed back out of the document, because a test that re-derives its expectation from the same
    // prose it is checking proves only that the parser is consistent with itself.
    private static readonly IReadOnlyDictionary<string, (int Low, int High)> Bands =
        new Dictionary<string, (int, int)>(StringComparer.Ordinal)
        {
            ["reed_cord_surveyor"] = (80, 90),
            ["crooked_rod_bearer"] = (84, 94),
            ["uncounted_pilgrim"] = (86, 98),
            ["cobra_of_the_entry_mark"] = (88, 100),
            ["name_eating_baboon"] = (84, 96),
            ["crocodile_of_the_short_measure"] = (94, 108),
            ["jar_seal_scarab_swarm"] = (88, 100),
            ["hungry_grain_thief"] = (90, 102),
            ["flood_mark_reader"] = (98, 112),
            ["drowned_field_scribe"] = (100, 114),
            ["silt_buried_farmer_shade"] = (102, 116),
            ["foreign_tribute_shade"] = (108, 122),
            ["donkey_of_the_third_tally"] = (112, 126),
            ["empty_handed_envoy"] = (96, 108),
            ["rope_gang_wraith"] = (112, 128),
            ["runaway_laborer"] = (96, 108),
            ["stone_hauler_ushabti"] = (120, 136),
            ["fallen_capstone_golem"] = (136, 154),
            ["cornerstone_oath_stone"] = (128, 146),
            ["palette_bearing_apprentice"] = (112, 126),
            ["hieroglyphic_complaint_wall"] = (142, 160),
            ["sun_seal_bearer"] = (126, 142),
            ["false_seal_forger"] = (116, 132),
            ["kneeling_petitioners"] = (112, 128),
            ["natron_bearer"] = (136, 152),
            ["linen_wrapped_embalmer"] = (142, 160),
            ["unfinished_mummy"] = (150, 170),
            ["fourfold_vessel_guardian"] = (160, 180),
            ["false_door_finder"] = (142, 158),
            ["cursed_loot_bearer"] = (148, 166),
            ["star_table_scribe"] = (152, 170),
            ["moon_cycle_ibis"] = (146, 164),
            ["eclipse_scarab"] = (164, 184),
            ["name_erasing_chisel_spirit"] = (156, 176),
            ["royal_genealogy_wall"] = (176, 198),
            ["feather_bearer"] = (174, 194),
            ["crocodile_beneath_the_balance"] = (186, 208),
            ["golden_ushabti_captain"] = (182, 204),
            ["eternal_reed_scribe"] = (166, 188),
            ["oathbound_gate"] = (210, 238),
        };

    private static IEnumerable<BabEncounter> WithRole(string role) =>
        Data.Encounters.Where(e => e.Act == 4 && e.Role == role);

    private static IEnumerable<BabEncounter> Standard() =>
        WithRole("combat").Concat(WithRole("multi_combat"));

    // §4's roster: thirty-five identities, and not one of the final forms among them.
    [Fact]
    public void The_act_holds_thirty_five_identities()
    {
        var fielded = Standard().SelectMany(e => e.Enemies).Distinct().ToList();
        var identities = fielded.Select(id => FinalForms.GetValueOrDefault(id, id)).Distinct().ToList();

        Assert.Equal(40, fielded.Count);
        Assert.Equal(35, identities.Count);

        // Every final form's earlier self is fielded too — a promotion the player never met is not one.
        foreach (var earlier in FinalForms.Values)
            Assert.Contains(earlier, fielded);
    }

    // §5's stage table: 55 templates over 17 stages.
    [Fact]
    public void The_encounter_pool_is_the_shape_the_master_states()
    {
        Assert.Equal(55, Standard().Count());

        var stages = Standard()
            .SelectMany(e => e.Tags ?? [])
            .Where(t => t.StartsWith("stage_", StringComparison.Ordinal))
            .Distinct()
            .ToList();

        Assert.Equal(17, stages.Count);
        Assert.All(Standard(), e =>
            Assert.Single(e.Tags ?? [], t => t.StartsWith("stage_", StringComparison.Ordinal)));
    }

    // One body, two, and exactly one three — the Sealed Court capstone, which the master prices on its own.
    [Fact]
    public void Only_the_final_capstone_fields_three_bodies()
    {
        Assert.Equal(32, Standard().Count(e => e.Enemies.Count == 1));
        Assert.Equal(22, Standard().Count(e => e.Enemies.Count == 2));

        var trio = Assert.Single(Standard(), e => e.Enemies.Count == 3);
        Assert.Equal("labyrinth_sealed_court_trio_01", trio.Id);

        // 62–64% / 49–51% / 46–49% of solo HP, and 296–349 together.
        Assert.Equal([141, 97, 84], trio.EnemyHealth);
        Assert.InRange(trio.EnemyHealth!.Sum(hp => hp ?? 0), 296, 349);
    }

    // Every body the act fields is priced inside the balance appendix's band for it.
    [Fact]
    public void Every_body_is_priced_inside_its_band()
    {
        var fielded = Standard().SelectMany(e => e.Enemies).Distinct().ToList();
        var authored = Data.Enemies.ToDictionary(e => e.Id, StringComparer.Ordinal);

        Assert.Equal(Bands.Count, fielded.Count);

        foreach (var id in fielded)
        {
            Assert.True(Bands.TryGetValue(id, out var band), $"'{id}' has no band in the appendix");
            Assert.True(authored.TryGetValue(id, out var enemy), $"'{id}' is fielded but not authored");
            Assert.InRange(enemy!.MaxHp, band.Low, band.High);
        }
    }

    // …and every one of them is a body the act's own vocabulary knows about, or a fight against it would open
    // without the five words the whole act is written in.
    [Fact]
    public void Every_body_is_a_party_to_the_labyrinth()
    {
        foreach (var id in Standard().SelectMany(e => e.Enemies).Distinct())
        {
            Assert.Contains(id, ActFour.Identities);

            var enemy = Data.Enemies.Single(e => e.Id == id);
            Assert.Contains(enemy.StartingStatuses ?? [], s => s.Status == ActFour.LabyrinthBodyId);
        }
    }

    // The elite master's "Earliest depth/stage" table, as a percentage of the act's seventeen stages. It is
    // authored ON the encounters so the curve is data rather than prose; wiring it into generation waits for
    // Act IV becoming a walkable act, since elites are not drawn from a ref pool today.
    private static readonly IReadOnlyDictionary<string, int> EarliestDepth =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["surveyor_of_the_errant_cord"] = 18,
            ["scarab_host_of_the_sealed_granary"] = 18,
            ["rope_master_of_the_corvee"] = 24,
            ["keeper_of_the_living_cartouche"] = 35,
            ["mummified_overseer_of_the_linen_house"] = 41,
            ["treasury_of_the_two_pans"] = 41,
            ["sphinx_of_the_processional_measure"] = 47,
            ["pry_bar_veteran"] = 65,
            ["keeper_of_the_thirty_six_decans"] = 71,
            ["colossus_of_the_endless_procession"] = 71,
        };

    // The whole elite pool: ten encounters, the master's HP to the point, and the earliest-depth table
    // honoured — the act's difficulty curve as opposed to its contents.
    [Fact]
    public void The_elite_pool_is_complete_and_priced_to_the_point()
    {
        var bands = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["surveyor_of_the_errant_cord"] = 248,
            ["scarab_host_of_the_sealed_granary"] = 255,
            ["rope_master_of_the_corvee"] = 275,
            ["keeper_of_the_living_cartouche"] = 300,
            ["mummified_overseer_of_the_linen_house"] = 318,
            ["treasury_of_the_two_pans"] = 330,
            ["sphinx_of_the_processional_measure"] = 344,
            // The Tombbreakers are priced as three bodies that all act every round: 320 together.
            ["pry_bar_veteran"] = 112,
            ["lamp_thief"] = 100,
            ["curse_bearer"] = 108,
            ["keeper_of_the_thirty_six_decans"] = 365,
            ["colossus_of_the_endless_procession"] = 388,
        };

        var elites = WithRole("elite").ToList();
        Assert.Equal(10, elites.Count);

        foreach (var encounter in elites)
            foreach (var id in encounter.Enemies)
            {
                Assert.True(bands.TryGetValue(id, out var hp), $"'{id}' is not one of the priced elites");
                Assert.Equal(hp, Data.Enemies.Single(e => e.Id == id).MaxHp);
                Assert.Contains(id, ActFour.EliteIdentities);
            }

        // Only the Tombbreakers field more than one body, and they field exactly three.
        var many = Assert.Single(elites, e => e.Enemies.Count > 1);
        Assert.Equal("labyrinth_elite_the_tombbreakers_three", many.Id);
        Assert.Equal(320, many.Enemies.Sum(id => Data.Enemies.Single(e => e.Id == id).MaxHp));

        // …and every one of them carries the depth it may first stand at.
        foreach (var encounter in elites)
        {
            var lead = encounter.Enemies[0];
            Assert.True(EarliestDepth.TryGetValue(lead, out var depth), $"'{lead}' has no earliest depth");
            Assert.Equal(depth, encounter.EarliestDepthPercent);
        }

        // The curve only ever rises across the single-body elites: a deeper one is never a lighter one. The
        // Tombbreakers are the master's stated exception and are excluded — three bodies that all act every
        // round are worth more than their combined HP says, which is exactly why theirs is lower.
        var byDepth = elites
            .Where(e => e.Enemies.Count == 1)
            .OrderBy(e => e.EarliestDepthPercent)
            .Select(e => Data.Enemies.Single(x => x.Id == e.Enemies[0]).MaxHp)
            .ToList();
        Assert.Equal(byDepth.OrderBy(hp => hp), byDepth);
        Assert.True(many.EarliestDepthPercent > 47 && many.Enemies.Sum(
                id => Data.Enemies.Single(x => x.Id == id).MaxHp) < 344,
            "the Tombbreakers are supposed to be the exception: deeper than the Sphinx and lighter than it");

        // The Rope-Master's hands are summoned, never fielded — the roster never names them.
        Assert.DoesNotContain(ActFour.StoneHaulerSummonEnemyId, Data.Enemies.Select(e => e.Id));
        Assert.Contains(ActFour.StoneHaulerSummonEnemyId, ActFour.EliteIdentities);
    }

    // The bosses, as far as they are built: the master's HP to the point, one encounter each — and the phase
    // markers filed where a frontend puts them beside the telegraph, which is the only thing that makes one
    // rotating intent list read as a boss CHANGING rather than as a bug.
    [Fact]
    public void The_bosses_built_so_far_are_priced_and_their_phases_are_filed()
    {
        var bands = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["pharaoh_of_the_sealed_name"] = 630,
            ["weigher_of_the_unspoken_heart"] = 610,
            ["architect_of_the_impossible_pyramid"] = 640,
            ["lady_of_the_black_granaries"] = 600,
            ["first_scribe_of_the_house_of_life"] = 580,
            ["mother_of_natron_and_resin"] = 610,
            ["vizier_of_the_kings_mouth"] = 590,
            ["queen_of_the_flood_reckoning"] = 620,
        };

        // A boss encounter is named for ONE body; the Vizier's three Offices stand in the same room and are
        // priced separately, and nothing else in the act fields them.
        var offices = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["royal_seal_bearer"] = 110,
            ["keeper_of_tallies"] = 116,
            ["captain_of_the_inner_stair"] = 124,
        };

        var bosses = WithRole("boss").ToList();
        Assert.Equal(bands.Count, bosses.Count);

        foreach (var encounter in bosses)
        {
            var id = encounter.Enemies[0];
            Assert.True(bands.TryGetValue(id, out var hp), $"'{id}' is not one of the priced bosses");
            Assert.Equal(hp, Data.Enemies.Single(e => e.Id == id).MaxHp);
            Assert.Contains(id, ActFour.BossIdentities);

            foreach (var subordinate in encounter.Enemies.Skip(1))
            {
                Assert.True(offices.TryGetValue(subordinate, out var officeHp),
                    $"'{subordinate}' stands in a boss room without a price");
                Assert.Equal(officeHp, Data.Enemies.Single(e => e.Id == subordinate).MaxHp);
                Assert.Contains(subordinate, ActFour.BossOffices);
            }
        }

        foreach (var marker in new[]
                 {
                     ActFour.TwoLandsNameId, ActFour.EternalNameId, ActFour.NameExposedId,
                     ActFour.HeartRemembersId, ActFour.HeartDeclaredLightId,
                     ActFour.PlanAlwaysCorrectId, ActFour.GranariesOpenId, ActFour.FamineAccountingId,
                     ActFour.PalimpsestId, ActFour.TextIsCanonId,
                     ActFour.VesselsFullId, ActFour.ThreeJarsId, ActFour.LastPreparationId,
                     ActFour.MouthOpensNextId, ActFour.MouthHasOpenedId, ActFour.KingNotHereId,
                     ActFour.ActingOfficeId,
                     ActFour.FloodStirsId, ActFour.FloodDisobeysId, ActFour.FloodCountedId,
                     ActFour.WaterBlackId, ActFour.FloodDriftsId,
                 })
            Assert.Contains(marker, BossPhases.Markers);
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
