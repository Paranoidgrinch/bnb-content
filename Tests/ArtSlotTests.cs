using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// THE PICTURES THE GAME WILL ASK FOR. Every card, every relic and every body the document ships names a file
// the frontend looks for, and none of those files exists yet — so nothing in the game can notice when the naming rule
// breaks. These are that notice.
//
// The rule they hold is the one that decides how much work there is: an upgraded card is drawn from its BASE
// card's picture, because an improvement changes what a card does and not what it is a picture of. Break that
// and 413 cards want 413 pictures instead of the 254 the table lists — and nobody would find out until 159
// cards turned up blank in a finished game.
public class ArtSlotTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260717);
    private static string Repo => Path.GetDirectoryName(TestData.Directory)!;

    [Fact]
    public void Every_card_and_every_relic_names_a_picture()
    {
        Assert.Equal(Game.Cards.Count, Game.Presentation.Cards.Count);
        Assert.Equal(Game.Relics.Count, Game.Presentation.Relics.Count);
        foreach (var (id, art) in Slots())
        {
            Assert.False(string.IsNullOrWhiteSpace(art), $"{id} names no picture");
            Assert.EndsWith(".png", art, StringComparison.Ordinal);
            // The file name is a file name on three operating systems and in a shell: no "+" (the upgrade
            // mark), no spaces, no capitals, nothing but the id.
            Assert.Matches(@"^(cards|relics|enemies)/[a-z0-9_]+\.png$", art);
        }
    }

    [Fact]
    public void An_upgraded_card_is_drawn_from_its_base_card()
    {
        var art = Game.Presentation.Cards;
        var upgrades = Game.Cards.Select(c => c.Id).Where(id => id.EndsWith('+')).ToList();
        Assert.NotEmpty(upgrades);
        foreach (var id in upgrades)
        {
            var basic = id.TrimEnd('+');
            Assert.True(art.ContainsKey(basic), $"{id} has no base card {basic}");
            Assert.Equal(art[basic].Art, art[id].Art);
        }
    }

    [Fact]
    public void The_document_asks_for_one_picture_per_base_card_and_per_relic()
    {
        var wanted = Slots().Select(s => s.Art).Distinct(StringComparer.Ordinal).Count();
        var basics = Game.Cards.Count(c => !c.Id.EndsWith('+'));
        Assert.Equal(basics + Game.Relics.Count + Game.Presentation.Enemies.Count, wanted);
    }

    // The table is generated, so it can go stale in one commit and mislead whoever is painting from it. This
    // regenerates it beside the checked-in one and compares; the message says the command that fixes it.
    [Fact]
    public void The_slot_table_says_what_the_document_says()
    {
        var current = Path.Combine(Repo, "ART_SLOTS.md");
        Assert.True(File.Exists(current), "ART_SLOTS.md is missing");
        var fresh = Path.Combine(Path.GetTempPath(), $"art-slots-{Guid.NewGuid():N}.md");
        try
        {
            ArtSlots.Write(Game, Data, TestData.Directory, fresh);
            Assert.True(File.ReadAllText(fresh) == File.ReadAllText(current),
                "ART_SLOTS.md is stale — regenerate it: "
                + "dotnet run --project Converter -- --data source-data --art-slots ART_SLOTS.md");
        }
        finally
        {
            File.Delete(fresh);
        }
    }

    private static IEnumerable<(string Id, string Art)> Slots() =>
        Game.Presentation.Cards.Select(e => (e.Key, e.Value.Art!))
            .Concat(Game.Presentation.Relics.Select(e => (e.Key, e.Value.Art!)))
            .Concat(Game.Presentation.Enemies.Select(e => (e.Key, e.Value.Art!)));
}
