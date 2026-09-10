using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// EVERY BODY IN THE GAME IS A PICTURE. A card asks for `cards/<id>.png` and a relic for `relics/<id>.png`;
// an enemy asks for `enemies/<id>.png` on exactly the same terms, and the frontend resolves the path the
// document declares rather than inventing a scheme of its own.
//
// Two of the three fields cannot be worked out by whoever is drawing the picture, and these tests are what
// keeps them true:
//   • the NAME — a body's display name lives inside the encounters that use it, not on the enemy record the
//     art table is written from;
//   • the ROLE (`Frame`) — a mook, an elite, a mimic and a god are different pictures, and NONE of that is a
//     property of the enemy. It is a property of the fights it is met in, so it is derived once at assembly.
//     The failure this guards is silent: a boss exported as "standard" still ships, still fights, still has
//     a picture — it is simply drawn as filler, and nothing in the game would ever say so.
public class EnemyArtTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260717);

    [Fact]
    public void Every_enemy_asks_for_a_picture_named_after_its_id()
    {
        Assert.NotEmpty(Game.Presentation.Enemies);
        foreach (var (id, look) in Game.Presentation.Enemies)
            Assert.Equal($"enemies/{id}.png", look.Art);
    }

    [Fact]
    public void Every_enemy_carries_the_name_the_screen_prints()
    {
        foreach (var (id, look) in Game.Presentation.Enemies)
            Assert.False(string.IsNullOrWhiteSpace(look.FlavorText), $"{id} has no name");
    }

    [Fact]
    public void Every_enemy_says_which_kind_of_body_it_is()
    {
        var roles = new[] { EnemyRole.Standard, EnemyRole.Elite, EnemyRole.Boss, EnemyRole.Mimic };
        foreach (var (id, look) in Game.Presentation.Enemies)
            Assert.Contains(look.Frame ?? $"{id} names no role", roles);
    }

    // The strongest room a body is met in is what it is. Counted per role, because that is the shape the art
    // list is worked down in — and because a rank that quietly stopped ranking would leave every body a mook.
    [Fact]
    public void The_role_is_the_hardest_fight_the_body_is_met_in()
    {
        var shipped = Game.Presentation.Enemies.Values
            .GroupBy(look => look.Frame!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        Assert.Equal(35, shipped[EnemyRole.Boss]);
        Assert.Equal(66, shipped[EnemyRole.Elite]);
        Assert.Equal(4, shipped[EnemyRole.Mimic]);
        Assert.Equal(164, shipped[EnemyRole.Standard]);
        Assert.Equal(Game.Presentation.Enemies.Count, shipped.Values.Sum());
    }

    // A body met in a boss room is a boss wherever else it also stands. Checked against the source data
    // directly, so the rank is proved rather than restated.
    [Fact]
    public void A_body_met_in_a_boss_room_is_never_filed_as_filler()
    {
        var inBossRooms = Data.Encounters
            .Where(e => string.Equals(e.Difficulty, "boss", StringComparison.OrdinalIgnoreCase))
            .SelectMany(e => e.Enemies)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(inBossRooms);
        foreach (var id in inBossRooms.Where(Game.Presentation.Enemies.ContainsKey))
            Assert.Equal(EnemyRole.Boss, Game.Presentation.Enemies[id].Frame);
    }
}
