using BnbContent.Converter;

namespace BnbContent.Tests;

// Act II as the master counts it: 25 unique identities across ten stages, and 35 encounter templates —
// 23 solo and 12 combination. The counts are pinned here so a stage cannot quietly go missing.
public class ActTwoPoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    private static IEnumerable<BabEncounter> ActTwoWithRole(string role) =>
        Data.Encounters.Where(e => e.Act == 2 && e.Role == role);

    // The STANDARD pool: elites and bosses are their own rosters and are counted separately.
    [Fact]
    public void The_act_fields_twenty_five_standard_identities()
    {
        var fielded = ActTwoWithRole("combat").Concat(ActTwoWithRole("multi_combat"))
            .SelectMany(e => e.Enemies).Distinct().ToList();

        Assert.Equal(25, fielded.Count);
    }

    // Four of the five Act-II bosses ship as single bodies; the Grand Cross-Reference is three volumes plus a
    // central body and is deliberately absent rather than flattened into one. See ADAPTATIONS.
    [Fact]
    public void The_act_fields_its_single_body_bosses()
    {
        var bosses = ActTwoWithRole("boss").ToList();

        Assert.Equal(4, bosses.Count);
        Assert.All(bosses, b => Assert.Single(b.Enemies));
    }

    // §1.1 of the elite master: nine elites, each its own encounter.
    [Fact]
    public void The_act_fields_nine_elites()
    {
        var elites = ActTwoWithRole("elite").ToList();

        Assert.Equal(9, elites.Count);
        Assert.All(elites, e => Assert.Single(e.Enemies));
    }

    // §3's stage table: 23 solo, 12 combination, 35 in all.
    [Fact]
    public void The_encounter_pool_is_the_shape_the_master_states()
    {
        Assert.Equal(23, ActTwoWithRole("combat").Count());
        Assert.Equal(12, ActTwoWithRole("multi_combat").Count());
    }

    // Every enemy an Act-II encounter fields must exist, or the fight cannot be built at all.
    [Fact]
    public void Every_enemy_an_encounter_fields_is_authored()
    {
        var authored = Data.Enemies.Select(e => e.Id).ToHashSet();

        foreach (var encounter in Data.Encounters.Where(e => e.Act == 2))
            foreach (var enemy in encounter.Enemies)
                Assert.True(authored.Contains(enemy), $"'{encounter.Id}' fields unauthored '{enemy}'");
    }

    // A combination encounter scales its bodies down; a solo is the identity at full strength.
    [Fact]
    public void Combination_encounters_field_more_than_one_body_and_scale_them()
    {
        foreach (var duo in ActTwoWithRole("multi_combat"))
        {
            Assert.True(duo.Enemies.Count >= 2, duo.Id);
            Assert.NotNull(duo.EnemyHealth);
            Assert.Equal(duo.Enemies.Count, duo.EnemyHealth!.Count);
        }
    }
}
