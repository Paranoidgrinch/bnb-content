using System.Text.Json;
using BnbContent.Converter;
using BnbContent.Converter.Lieutenant;
using RogueDeck.Bot;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// The encounter trial (EncounterTrial): every fight of an act in stage order, the bosses last, a stage's spoils
// between the stages, and for a later act the earlier acts' spoils first — walked the way the frontend walks it,
// from the game document's own Extra.
public class EncounterTrialTests
{
    private static RunTutorial Trial(int act) =>
        JsonSerializer.Deserialize<RunTutorial>(
            FightProbe.Shipped.Presentation.Game!.Extra[$"{EncounterTrial.ExtraPrefix}{act}"], RunJson.CreateOptions())!;

    // What the frontend does with it (GameHost.StartTrial): the walk in place of the acts, the Lieutenant alone.
    private static RunBlueprint Walk(RunTutorial trial, int? health = null)
    {
        var start = health is { } hp ? trial.Start! with { MaxHealth = hp, StartingHealth = hp } : trial.Start!;
        return (FightProbe.Shipped with
        {
            Map = trial.Map,
            Acts = null,
            MapGeneration = null,
            StrategicMapGeneration = null,
            Tutorial = null,
        }).AsLoneFighter(start, LieutenantCharacter.Id);
    }

    [Theory]
    [InlineData(1, "city_boss_")]
    [InlineData(2, "archives_boss_")]
    [InlineData(3, "green_docket_boss_")]
    public void An_act_walks_every_stage_fight_then_every_boss(int act, string bossPrefix)
    {
        var trial = Trial(act);
        var fights = trial.Map.Nodes.Where(n => n.Payload is EncounterRef).Select(n => ((EncounterRef)n.Payload!).Id.Value).ToList();
        var pool = FightProbe.Shipped.Acts![act - 1].MapGeneration!.Encounters!;
        var expected = new[] { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Boss }
            .SelectMany(k => pool.For(k).Select(e => e.Encounter.Value)).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expected.Order(StringComparer.Ordinal), fights.Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(fights.Count, fights.Distinct().Count());
        // The bosses close it, and nothing pays: no fight carries a reward of its own.
        Assert.All(fights.TakeLast(5), id => Assert.StartsWith(bossPrefix, id, StringComparison.Ordinal));
        Assert.All(trial.Map.Nodes.Select(n => n.Payload).OfType<EncounterRef>(), r => Assert.Null(r.VictoryReward));
        // The earlier acts' packages and boss spoils before the first fight; then four stages, four packages, each
        // straight after its stage's last fight.
        var spoils = trial.Map.Nodes.Where(n => n.Payload is EventRef).Select(n => ((EventRef)n.Payload!).Id.Value).ToList();
        var catchUp = Enumerable.Range(1, act - 1)
            .SelectMany(earlier => Enumerable.Range(1, 4).Select(s => EncounterTrial.SpoilsId(earlier, s)).Append(EncounterTrial.BossSpoilsId(earlier)))
            .ToList();
        Assert.Equal([.. catchUp, .. Enumerable.Range(1, 4).Select(s => EncounterTrial.SpoilsId(act, s))], spoils);
        Assert.All(trial.Map.Nodes.Take(catchUp.Count), n => Assert.IsType<EventRef>(n.Payload));
        Assert.Contains(EncounterTrial.InfirmaryId, trial.Start!.StartingRelics);
        Assert.Equal(LieutenantCharacter.Start.Deck, trial.Start.Deck);
    }

    // A bot walks into it on a sturdy body: the first stage's fights, its package asked and answered, and on into
    // the next stage. (A body that cannot die is no help: the random bot then plays a fight
    // for hundreds of turns, until some card's own limits complain.)
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task A_bot_walks_into_the_second_stage(int act)
    {
        var trial = Trial(act);
        using var play = new RunPlayback(() => { });
        play.Start(Walk(trial, health: 400), seed: 4, interactive: true, characterId: LieutenantCharacter.Id);
        Assert.Null(play.Error);
        var result = await RunBot.Play(play, new BotOptions { Seed = 4, Budget = 3000 }, new DelegateBotLog(_ => { }));
        Assert.True(result.Error is "" or "none", result.Error);
        var stageOne = trial.Map.Nodes.SkipWhile(n => n.Payload is EventRef).TakeWhile(n => n.Payload is EncounterRef).Count();
        Assert.True(result.Fights > stageOne, $"{result.Fights} fights, {result.Reason}");
        // A later act opens on a deck the earlier acts have grown.
        if (act > 1)
            Assert.True(play.Session!.Run.Deck.Count > LieutenantCharacter.Start.Deck.Count + 4);
    }

    // The Infirmary: whatever a fight cost, the next one starts from full. (The run's history forgets a fight at
    // the next room, so it is read off the run as it stands: the relic is held, and the body is whole.)
    [Fact]
    public async Task Every_fight_starts_from_full_health()
    {
        using var play = new RunPlayback(() => { });
        play.Start(Walk(Trial(1), health: 200), seed: 9, interactive: true, characterId: LieutenantCharacter.Id);
        var result = await RunBot.Play(play, new BotOptions { Seed = 9, Budget = 600 }, new DelegateBotLog(_ => { }));
        var run = play.Session!.Run;
        Assert.True(result.Fights >= 3, $"{result.Fights} fights, {result.Reason}");
        Assert.Contains(run.Relics, r => r.Id.Value == EncounterTrial.InfirmaryId);
        if (run.Result == RunResult.Ongoing && play.CombatDriver?.Current is null)
            Assert.Equal(run.Health.Max, run.Health.Current);
    }
}
