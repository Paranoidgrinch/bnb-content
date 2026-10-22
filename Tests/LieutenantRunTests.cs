using BnbContent.Converter.Lieutenant;
using RogueDeck.Bot;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// The Lieutenant through the shipped game, played by the bot: rooms, rewards, shops and fights as her, her
// promises issued in them — and nothing in any of it throws.
public class LieutenantRunTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public async Task The_bot_walks_her_run_and_keeps_her_promises(int seed)
    {
        using var play = new RunPlayback(() => { });
        play.Start(FightProbe.Shipped, seed, interactive: true, characterId: LieutenantCharacter.Id);
        Assert.Null(play.Error);
        var log = new DelegateBotLog(_ => { });
        var result = await RunBot.Play(play, new BotOptions { Seed = seed, Budget = 1500 }, log);
        Assert.True(result.Error is "" or "none", result.Error);
        Assert.True(result.Fights >= 2, $"{result.Fights} fights, {result.Reason}");
        Assert.Equal(LieutenantCharacter.Id, play.Session!.Run.CharacterId);
    }
}
