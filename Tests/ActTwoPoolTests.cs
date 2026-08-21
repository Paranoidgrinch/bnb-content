using BnbContent.Converter;

namespace BnbContent.Tests;

// Act II as the master counts it: 25 unique identities across ten stages, and 35 encounter templates —
// 23 solo and 12 combination. The counts are pinned here so a stage cannot quietly go missing.
public class ActTwoPoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    private static IEnumerable<BabEncounter> ActTwoWithRole(string role) =>
        Data.Encounters.Where(e => e.Act == 2 && e.Role == role);

    [Fact]
    public void The_act_fields_twenty_five_identities()
    {
        var fielded = Data.Encounters.Where(e => e.Act == 2).SelectMany(e => e.Enemies).Distinct().ToList();

        Assert.Equal(25, fielded.Count);
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
