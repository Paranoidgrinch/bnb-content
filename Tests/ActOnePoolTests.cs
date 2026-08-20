using BnbContent.Converter;

namespace BnbContent.Tests;

// The curated Act-I pool: the FINAL_AUDIT lists exactly 32 standard templates — 23 solos and 9 duos — built
// from the 25 final identities, plus the elite / boss / mimic pools the generated map draws from. Encounters
// without a role are inert leftovers from the demo port and must never reach a map.
public class ActOnePoolTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    // Every identity the reworked Act I fields. Two of them (Oath Candle, Sustaining Gavel) are support-first
    // and never appear alone, which is why 25 identities make 23 solos.
    private static readonly string[] FinalIdentities =
    [
        "a_very_official_line", "number_ticket_wisp", "queue_crier_homunculus",
        "wrong_window_scribe", "receipt_eyed_clerk", "triplicate_examiner",
        "filing_beetle", "unsigned_form_ghost", "duplicate_copy_mite", "blank_line_leech",
        "wax_notary", "sealed_door_ward", "oath_candle",
        "contradictory_signpost", "exception_imp", "old_statute_ghost",
        "inverted_hourglass", "fading_number_token", "minute_moth",
        "counterclaim_imp", "self_correcting_record", "sustaining_gavel",
        "warrant_bailiff", "threshold_seizure_ward", "civic_battering_ram",
    ];

    private static IEnumerable<BabEncounter> WithRole(string role) =>
        Data.Encounters.Where(e => e.Role == role);

    [Fact]
    public void The_standard_pool_is_the_audits_twenty_three_solos_and_nine_duos()
    {
        Assert.Equal(23, WithRole("combat").Count());
        Assert.Equal(9, WithRole("multi_combat").Count());

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
        // The audit's ten elites: four enforcement, three delay, three appeal.
        Assert.Equal(10, WithRole("elite").Count());
        // The act's boss pool: the map draws one of them per run.
        Assert.NotEmpty(WithRole("boss"));
        Assert.NotEmpty(WithRole("mimic"));

        // Roles are a closed vocabulary — a typo would silently drop a template out of every pool.
        var known = new[] { "combat", "multi_combat", "elite", "boss", "mimic" };
        Assert.All(Data.Encounters.Where(e => e.Role is not null), e => Assert.Contains(e.Role, known));
    }
}
