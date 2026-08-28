using BnbContent.Converter;

namespace BnbContent.Tests;

// Act III as the master counts it: 25 unique identities across ten stages, and 40 encounter templates — 12
// solo and 28 combination. The act deliberately holds fewer bodies and more recombination than Act II,
// because customary law only becomes interesting when several parties hold competing rights.
public class ActThreePoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    private static IEnumerable<BabEncounter> ActThreeWithRole(string role) =>
        Data.Encounters.Where(e => e.Act == 3 && e.Role == role);

    private static IEnumerable<BabEncounter> Standard() =>
        ActThreeWithRole("combat").Concat(ActThreeWithRole("multi_combat"));

    [Fact]
    public void The_act_fields_twenty_five_standard_identities()
    {
        var fielded = Standard().SelectMany(e => e.Enemies).Distinct().ToList();

        Assert.Equal(25, fielded.Count);
    }

    // §5's stage table: 12 solo, 28 combination, 40 in all.
    [Fact]
    public void The_encounter_pool_is_the_shape_the_master_states()
    {
        Assert.Equal(12, ActThreeWithRole("combat").Count());
        Assert.Equal(28, ActThreeWithRole("multi_combat").Count());
    }

    // Ten stages, four encounters each — the act is a curriculum, and every stage teaches its four.
    [Fact]
    public void Every_stage_fields_four_encounters()
    {
        var stages = Standard()
            .SelectMany(e => e.Tags ?? [])
            .Where(t => t.StartsWith("stage_", StringComparison.Ordinal))
            .GroupBy(t => t)
            .ToList();

        Assert.Equal(10, stages.Count);
        Assert.All(stages, stage => Assert.Equal(4, stage.Count()));
    }

    // Every identity the act fields is authored, or the fight cannot be built at all.
    [Fact]
    public void Every_enemy_an_encounter_fields_is_authored()
    {
        var authored = Data.Enemies.Select(e => e.Id).ToHashSet();

        foreach (var encounter in Data.Encounters.Where(e => e.Act == 3))
            foreach (var enemy in encounter.Enemies)
                Assert.True(authored.Contains(enemy), $"'{encounter.Id}' fields unauthored '{enemy}'");
    }

    // …and every identity the act fields is one the act's own customs know about, or a fight against it
    // would open without the vocabulary the whole act is written in.
    [Fact]
    public void Every_identity_is_a_party_to_the_docket()
    {
        foreach (var enemy in Standard().SelectMany(e => e.Enemies).Distinct())
            Assert.Contains(enemy, ActThree.Identities);
    }

    // Two of the forty are three-body fights, and the master says which: the Stage-3 testimony capstone and
    // the Stage-10 reckoning. Everything else is one body or two.
    [Fact]
    public void Exactly_two_encounters_field_three_bodies()
    {
        var trios = Standard().Where(e => e.Enemies.Count == 3).Select(e => e.Id).ToList();

        Assert.Equal(2, trios.Count);
        Assert.Contains("green_docket_testimony_trio_01", trios);
        Assert.Contains("green_docket_court_trio_01", trios);
    }

    // A combination scales its bodies down; a solo is the identity at full strength.
    [Fact]
    public void Combination_encounters_field_more_than_one_body_and_scale_them()
    {
        foreach (var many in ActThreeWithRole("multi_combat"))
        {
            Assert.True(many.Enemies.Count >= 2, many.Id);
            Assert.NotNull(many.EnemyHealth);
            Assert.Equal(many.Enemies.Count, many.EnemyHealth!.Count);
        }
    }

    // Every party carries the act's own marker, which is how a rule says "the parties in this fight" without
    // knowing which side it is looking from.
    [Fact]
    public void Every_body_is_marked_as_a_party()
    {
        foreach (var id in ActThree.Identities)
        {
            var enemy = Data.Enemies.Single(e => e.Id == id);
            Assert.Contains(enemy.StartingStatuses ?? [],
                s => s.Status == ActThree.GreenDocketBodyId);
        }
    }

    // ── the elite layer ───────────────────────────────────────────────────────────────────────────────────

    // Nine elites, and the master's own roster: permission, custom, crossing, restitution, formation,
    // injunction, obsolete right, appeal, judgment. Act III requires three elites on every valid path, so a
    // nine-encounter pool is what gives a run variance without padding.
    [Fact]
    public void The_act_fields_nine_elite_encounters()
    {
        var elites = Data.Encounters.Where(e => e.Act == 3 && e.Role == "elite").ToList();

        Assert.Equal(9, elites.Count);
    }

    // An elite is a Green Docket body like any other — the act's customs open on it — but it is never a
    // standard identity, so the two rosters are counted apart.
    [Fact]
    public void Every_elite_body_is_a_party_and_never_a_standard()
    {
        foreach (var encounter in Data.Encounters.Where(e => e.Act == 3 && e.Role == "elite"))
            foreach (var id in encounter.Enemies)
            {
                var enemy = Data.Enemies.Single(e => e.Id == id);
                Assert.Contains(enemy.StartingStatuses ?? [], s => s.Status == ActThree.GreenDocketBodyId);
                Assert.Contains(id, ActThree.EliteIdentities);
                Assert.DoesNotContain(id, ActThree.Identities);
            }
    }

    // The master's HP targets, body for body.
    [Fact]
    public void The_elites_are_the_size_the_master_states()
    {
        (string Id, int Health)[] expected =
        [
            (ActThree.StagEnemyId, 138),
            (ActThree.GrandmotherWebEnemyId, 154),
            // ADAPTATION: one 200-HP body rather than 96 + 104, turning around at the far bank.
            (ActThree.WrongBridgeEnemyId, 200),
            (ActThree.GreatTollFrogEnemyId, 176),
            (ActThree.AntQueenEnemyId, 160),
            ("first_line_bearer", 27), ("second_line_bearer", 27), ("third_line_bearer", 27),
            (ActThree.JuniperEnemyId, 188),
            (ActThree.SurveyorEnemyId, 198),
            (ActThree.HearingReedId, 78), (ActThree.RemandReedId, 84), (ActThree.RefusalReedId, 90),
            (ActThree.MagistrateEnemyId, 220),
        ];

        foreach (var (id, health) in expected)
            Assert.Equal(health, Data.Enemies.Single(e => e.Id == id).MaxHp);
    }
}
