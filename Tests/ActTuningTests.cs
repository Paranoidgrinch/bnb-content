using System.Text.Json;
using BnbContent.Converter;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// What ActTuning does to the document, pinned against the document as authored: the factor reaches the program
// AND the telegraph, health per encounter, damage per body — and nothing outside Acts III and IV moves.
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
        Assert.Equal("Reed Lash · 14 dmg", before.Intent.Label);
        var expected = (int)Math.Round(14 * factor / 100.0, MidpointRounding.AwayFromZero);
        Assert.Equal($"Reed Lash · {expected} dmg", after.Intent.Label);
        Assert.Contains($"\"Value\":{expected}", Json(after.Program!), StringComparison.Ordinal);
        Assert.DoesNotContain("\"Value\":14", Json(after.Program!), StringComparison.Ordinal);
    }

    [Fact]
    public void Health_follows_the_encounters_role_and_an_override_beats_it()
    {
        // A single normal body of Act IV, and one of its multi-fights fielding that same body.
        var normal = ActTuning.For(4, "normal").HealthPercent;
        var multi = ActTuning.For(4, "multi").HealthPercent;
        Assert.Equal([.. Health(Authored, "labyrinth_stelae_01").Select(h => Scale(h, normal))], Health(Shipped, "labyrinth_stelae_01"));
        Assert.Equal([.. Health(Authored, "labyrinth_stelae_duo_01").Select(h => Scale(h, multi))], Health(Shipped, "labyrinth_stelae_duo_01"));

        var vizier = ActTuning.Encounters["labyrinth_boss_vizier_of_the_kings_mouth"].HealthPercent;
        Assert.NotEqual(ActTuning.For(4, "boss").HealthPercent, vizier);
        Assert.Equal([.. Health(Authored, "labyrinth_boss_vizier_of_the_kings_mouth").Select(h => Scale(h, vizier))],
            Health(Shipped, "labyrinth_boss_vizier_of_the_kings_mouth"));
    }

    [Fact]
    public void An_act_three_boss_is_harder_than_authored()
    {
        Assert.True(Health(Shipped, "green_docket_boss_queen_under_the_hill")[0] > Health(Authored, "green_docket_boss_queen_under_the_hill")[0]);
        var changed = Authored.EnemyActions.Where(a => a.Id.StartsWith("queen_under_the_hill.", StringComparison.Ordinal))
            .Count(a => Json(a) != Json(Action(Shipped, a.Id)));
        Assert.True(changed > 0);
    }

    [Fact]
    public void Nothing_outside_acts_three_and_four_moves()
    {
        var data = BabData.Load(TestData.Directory);
        var outside = data.Encounters.Where(e => e.Act is not (3 or 4)).ToList();
        var bodies = outside.SelectMany(e => e.Enemies).ToHashSet(StringComparer.Ordinal);
        var inside = data.Encounters.Where(e => e.Act is 3 or 4).SelectMany(e => e.Enemies).ToHashSet(StringComparer.Ordinal);
        foreach (var encounter in outside)
            Assert.Equal(Json(Authored.Encounters.First(e => e.Id.Value == encounter.Id)),
                Json(Shipped.Encounters.First(e => e.Id.Value == encounter.Id)));
        foreach (var action in Authored.EnemyActions.Where(a => bodies.Contains(a.Id.Split('.')[0])
                     && !inside.Contains(a.Id.Split('.')[0])))
            Assert.Equal(Json(action), Json(Action(Shipped, action.Id)));
        Assert.Equal(Json(Authored.Cards), Json(Shipped.Cards));
        Assert.Equal(Json(Authored.Relics), Json(Shipped.Relics));
    }

    private static int Scale(int value, int percent) =>
        Math.Max(1, (int)Math.Round(value * percent / 100.0, MidpointRounding.AwayFromZero));
}
