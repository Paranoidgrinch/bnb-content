using BnbContent.Converter.Apprentice;
using RogueDeck.Bot;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// The Sorcerer's Apprentice through the shipped game, played by the bot: rooms, rewards, shops and fights as him,
// his wand used in them — and nothing in any of it throws.
public class ApprenticeRunTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public async Task The_bot_walks_his_run_and_casts_his_wand(int seed)
    {
        using var play = new RunPlayback(() => { });
        play.Start(FightProbe.Shipped, seed, interactive: true, characterId: ApprenticeCharacter.Id);
        Assert.Null(play.Error);
        var casts = 0;
        var inscriptions = 0;
        play.Script!.Answered += entry =>
        {
            if (entry is RogueDeck.Sandbox.Run.CombatActionEntry { Action.value: ApprenticeWand.Cast })
                casts++;
            if (entry is RogueDeck.Sandbox.Run.CombatActionEntry { Action.value: ApprenticeWand.Inscribe })
                inscriptions++;
        };
        var log = new DelegateBotLog(_ => { });
        var result = await RunBot.Play(play, new BotOptions { Seed = seed, Budget = 1500 }, log);
        Assert.True(result.Error is "" or "none", result.Error);
        Assert.True(result.Fights >= 2, $"{result.Fights} fights, {result.Reason}");
        Assert.Equal(ApprenticeCharacter.Id, play.Session!.Run.CharacterId);
        Assert.True(inscriptions > 0, "he never inscribed a card");
        Assert.True(casts > 0, "he never cast the wand");
    }
}
