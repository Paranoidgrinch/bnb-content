using BnbContent.Converter;

namespace BnbContent.Tests;

// The curated Act-I pool after the content cut of 2026-10-29: four stages (Queue, Ordinance, Seal, Delay), 13
// identities in 13 solos and 6 duos, plus the elite / boss / mimic pools the generated map draws from. The few
// encounters without a role are test fixtures (quiet dummies) and must never reach a map.
public class ActOnePoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    // Every identity the cut Act I fields, stage by stage. The Oath Candle is support-first and never appears
    // alone, which is why 13 identities make 12 solos.
    private static readonly string[] FinalIdentities =
    [
        "a_very_official_line", "number_ticket_wisp", "queue_crier_homunculus",
        "contradictory_signpost", "wrong_window_scribe", "self_correcting_record", "exception_imp",
        "wax_notary", "sealed_door_ward", "oath_candle",
        "inverted_hourglass", "fading_number_token", "minute_moth",
    ];

    // Scoped to Act I: the catalogue holds every act's encounters, and these counts are Act I's.
    private static IEnumerable<BabEncounter> WithRole(string role) =>
        Data.Encounters.Where(e => e.Act == 1 && e.Role == role);

    [Fact]
    public void The_standard_pool_is_twelve_solos_and_six_duos()
    {
        Assert.Equal(12, WithRole("combat").Count());
        Assert.Equal(6, WithRole("multi_combat").Count());

        Assert.All(WithRole("combat"), e => Assert.Single(e.Enemies));
        Assert.All(WithRole("multi_combat"), e => Assert.True(e.Enemies.Count >= 2, e.Id));
    }

    [Fact]
    public void Every_final_identity_is_fielded_and_nothing_else_is()
    {
        var fielded = WithRole("combat").Concat(WithRole("multi_combat"))
            .SelectMany(e => e.Enemies).Distinct().ToList();

        Assert.Equal(FinalIdentities.OrderBy(id => id), fielded.OrderBy(id => id));
    }

    // A duo fields reduced bodies; the audit puts them at roughly two thirds of the solo HP.
    [Fact]
    public void Every_duo_states_its_own_reduced_health()
    {
        var enemies = Data.Enemies.ToDictionary(e => e.Id);

        foreach (var duo in WithRole("multi_combat"))
        {
            Assert.NotNull(duo.EnemyHealth);
            Assert.Equal(duo.Enemies.Count, duo.EnemyHealth!.Count);
            for (var slot = 0; slot < duo.Enemies.Count; slot++)
            {
                var solo = enemies[duo.Enemies[slot]].MaxHp;
                var fielded = duo.EnemyHealth[slot];
                Assert.NotNull(fielded);
                Assert.True(fielded < solo, $"{duo.Id}: {duo.Enemies[slot]} should be reduced from {solo}");
            }
        }
    }

    [Fact]
    public void The_map_pools_are_complete()
    {
        // Seven elites: three in Ordinance (the Queue's examiner stands there too), two in Seal, two in Delay.
        Assert.Equal(7, WithRole("elite").Count());
        Assert.Equal(5, WithRole("boss").Count());
        // The act's boss pool: the map draws one of them per run.
        Assert.NotEmpty(WithRole("boss"));
        Assert.NotEmpty(WithRole("mimic"));

        // Roles are a closed vocabulary — a typo would silently drop a template out of every pool.
        var known = new[] { "combat", "multi_combat", "elite", "boss", "mimic" };
        Assert.All(Data.Encounters.Where(e => e.Role is not null), e => Assert.Contains(e.Role, known));
    }
}
