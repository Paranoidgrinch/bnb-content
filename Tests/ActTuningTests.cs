using System.Text.Json;
using BnbContent.Converter;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// What ActTuning does to the document, pinned against the document as authored: the factor reaches the program
// AND the telegraph, health per encounter, damage per body — and nothing outside Acts II-IV moves but the
// health of Act I's single fights.
public class ActTuningTests
{
    private static readonly RunBlueprint Authored = FightProbe.Game;
    private static readonly RunBlueprint Shipped = FightProbe.Shipped;

    private static EnemyActionData Action(RunBlueprint game, string id) => game.EnemyActions.First(a => a.Id == id);

    private static List<int> Health(RunBlueprint game, string encounter) =>
        [.. game.Encounters.First(e => e.Id.Value == encounter).Enemies.Select(e => e.MaxHealth)];

    private static string Json(object value) =>
        new([.. JsonSerializer.Serialize(value, RunJson.CreateOptions()).Where(c => !char.IsWhiteSpace(c))]);

    [Fact]
    public void An_act_four_normal_hits_by_its_factor_in_the_program_and_the_telegraph()
    {
        var factor = ActTuning.For(4, "normal").DamagePercent;
        Assert.NotEqual(100, factor);
        var before = Action(Authored, "reed_cord_surveyor.reed_lash");
        var after = Action(Shipped, "reed_cord_surveyor.reed_lash");
        Assert.Equal("Lash · 12 dmg", before.Intent.Label);
        var expected = (int)Math.Round(12 * factor / 100.0, MidpointRounding.AwayFromZero);
        Assert.Equal($"Lash · {expected} dmg", after.Intent.Label);
        Assert.Contains($"\"Value\":{expected}", Json(after.Program!), StringComparison.Ordinal);
        Assert.DoesNotContain("\"Value\":12", Json(after.Program!), StringComparison.Ordinal);
    }

    [Fact]
    public void Health_follows_the_encounters_role_and_an_override_beats_it()
    {
        // A single normal body of Act IV, and one of its multi-fights fielding that same body.
        var normal = ActTuning.For(4, "normal").HealthPercent;
        var multi = ActTuning.For(4, "multi").HealthPercent;
        Assert.Equal([.. Health(Authored, "labyrinth_stelae_01").Select(h => Scale(h, normal))], Health(Shipped, "labyrinth_stelae_01"));
        Assert.Equal([.. Health(Authored, "labyrinth_stelae_duo_01").Select(h => Scale(h, multi))], Health(Shipped, "labyrinth_stelae_duo_01"));

        const string queen = "green_docket_elite_ant_queen_of_the_proper_line";
        var own = ActTuning.Encounters[queen].HealthPercent;
        Assert.NotEqual(ActTuning.For(3, "elite").HealthPercent, own);
        Assert.Equal([.. Health(Authored, queen).Select(h => Scale(h, own))], Health(Shipped, queen));
    }

    [Fact]
    public void An_act_three_boss_is_harder_than_authored()
    {
        Assert.True(Health(Shipped, "green_docket_boss_queen_under_the_hill")[0] > Health(Authored, "green_docket_boss_queen_under_the_hill")[0]);
        var changed = Authored.EnemyActions.Where(a => a.Id.StartsWith("queen_under_the_hill.", StringComparison.Ordinal))
            .Count(a => Json(a) != Json(Action(Shipped, a.Id)));
        Assert.True(changed > 0);
    }

    // Act II's normal and multi-fights carry more health (the trial of 2026-11-02), all but the Blank Death
    // Certificate, which keeps its own.
    [Fact]
    public void Act_two_fields_its_normal_and_multi_fights_stronger()
    {
        var data = BabData.Load(TestData.Directory);
        foreach (var encounter in data.Encounters.Where(e => e.Act == 2 && ActTuning.RoleOf(e) is "normal" or "multi"))
        {
            var percent = ActTuning.Encounters.TryGetValue(encounter.Id, out var own)
                ? own.HealthPercent
                : ActTuning.For(2, ActTuning.RoleOf(encounter)).HealthPercent;
            Assert.Equal([.. Health(Authored, encounter.Id).Select(h => Scale(h, percent))], Health(Shipped, encounter.Id));
        }
        Assert.Equal(Health(Authored, "archives_necrology_01"), Health(Shipped, "archives_necrology_01"));
        Assert.True(Health(Shipped, "archives_catalogue_01")[0] > Health(Authored, "archives_catalogue_01")[0]);
    }

    [Fact]
    public void Nothing_outside_the_tuned_acts_moves_but_act_one_normal_health()
    {
        var data = BabData.Load(TestData.Directory);
        // Act I's single fights carry 10 % more health (the trial of 2026-11-01), and nothing else of Act I moves.
        var actOneNormals = data.Encounters.Where(e => e.Act == 1 && ActTuning.RoleOf(e) == "normal").ToList();
        Assert.NotEmpty(actOneNormals);
        foreach (var encounter in actOneNormals)
            Assert.Equal([.. Health(Authored, encounter.Id).Select(h => Scale(h, 110))], Health(Shipped, encounter.Id));
        var tuned = new[] { 2, 3, 4 };
        var outside = data.Encounters.Where(e => !tuned.Contains(e.Act)).Except(actOneNormals).ToList();
        var bodies = outside.SelectMany(e => e.Enemies).ToHashSet(StringComparer.Ordinal);
        var inside = data.Encounters.Where(e => tuned.Contains(e.Act)).SelectMany(e => e.Enemies).ToHashSet(StringComparer.Ordinal);
        foreach (var encounter in outside)
            Assert.Equal(Json(Authored.Encounters.First(e => e.Id.Value == encounter.Id)),
                Json(Shipped.Encounters.First(e => e.Id.Value == encounter.Id)));
        foreach (var action in Authored.EnemyActions.Where(a => bodies.Contains(a.Id.Split('.')[0])
                     && !inside.Contains(a.Id.Split('.')[0])))
            Assert.Equal(Json(action), Json(Action(Shipped, action.Id)));
        Assert.Equal(Json(Authored.Cards), Json(Shipped.Cards));
        Assert.Equal(Json(Authored.Relics), Json(Shipped.Relics));
    }

    [Fact]
    public void An_encounters_own_damage_reaches_only_that_encounter_through_copies()
    {
        var copies = Shipped.Encounters
            .SelectMany(e => e.Enemies.SelectMany(b => b.Actions).Select(a => (Encounter: e.Id.Value, Action: a.value)))
            .Where(x => x.Action.Contains('@', StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(copies);
        foreach (var (encounter, id) in copies)
        {
            var (authoredId, owner) = (id[..id.IndexOf('@', StringComparison.Ordinal)], id[(id.IndexOf('@', StringComparison.Ordinal) + 1)..]);
            Assert.Equal(encounter, owner);
            Assert.True(ActTuning.Encounters.ContainsKey(owner));
            var copy = Action(Shipped, id);
            var source = Action(Authored, authoredId);
            Assert.Equal(source.NameKey ?? $"enemy-action.{authoredId}.name", copy.NameKey);
            Assert.Equal(source.Intent.Kind, copy.Intent.Kind);
        }
        // The authored action stays in the document for the rooms that share the body.
        Assert.All(copies, x => Assert.Contains(Shipped.EnemyActions, a => a.Id == x.Action[..x.Action.IndexOf('@', StringComparison.Ordinal)]));
    }

    private static int Scale(int value, int percent) =>
        Math.Max(1, (int)Math.Round(value * percent / 100.0, MidpointRounding.AwayFromZero));
}
