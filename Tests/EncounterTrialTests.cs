using System.Text.Json;
using BnbContent.Converter;
using BnbContent.Converter.Lieutenant;
using RogueDeck.Bot;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// The encounter trial (EncounterTrial): every fight of Act I in stage order, the bosses last, a stage's spoils
// between the stages — walked the way the frontend walks it, from the game document's own Extra.
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

    [Fact]
    public void Act_one_walks_every_stage_fight_then_every_boss()
    {
        var trial = Trial(1);
        var fights = trial.Map.Nodes.Where(n => n.Payload is EncounterRef).Select(n => ((EncounterRef)n.Payload!).Id.Value).ToList();
        var pool = FightProbe.Shipped.Acts![0].MapGeneration!.Encounters!;
        var expected = new[] { MapNodeKind.Combat, MapNodeKind.MultiCombat, MapNodeKind.Elite, MapNodeKind.Boss }
            .SelectMany(k => pool.For(k).Select(e => e.Encounter.Value)).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expected.Order(StringComparer.Ordinal), fights.Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(fights.Count, fights.Distinct().Count());
        // The bosses close it, and nothing pays: no fight carries a reward of its own.
        Assert.All(fights.TakeLast(5), id => Assert.StartsWith("city_boss_", id, StringComparison.Ordinal));
        Assert.All(trial.Map.Nodes.Select(n => n.Payload).OfType<EncounterRef>(), r => Assert.Null(r.VictoryReward));
        // Four stages, four packages, each straight after its stage's last fight.
        var spoils = trial.Map.Nodes.Where(n => n.Payload is EventRef).Select(n => ((EventRef)n.Payload!).Id.Value).ToList();
        Assert.Equal([.. Enumerable.Range(1, 4).Select(s => EncounterTrial.SpoilsId(1, s))], spoils);
        Assert.Contains(EncounterTrial.InfirmaryId, trial.Start!.StartingRelics);
        Assert.Equal(LieutenantCharacter.Start.Deck, trial.Start.Deck);
    }

    // A bot walks into it on a sturdy body: the first stage's fights, its package asked and answered, and on into
    // the next stage. (A body that cannot die is no help: the random bot then plays a fight
    // for hundreds of turns, until some card's own limits complain.)
    [Fact]
    public async Task A_bot_walks_the_whole_trial()
    {
        using var play = new RunPlayback(() => { });
        play.Start(Walk(Trial(1), health: 400), seed: 4, interactive: true, characterId: LieutenantCharacter.Id);
        Assert.Null(play.Error);
        var result = await RunBot.Play(play, new BotOptions { Seed = 4, Budget = 3000 }, new DelegateBotLog(_ => { }));
        Assert.True(result.Error is "" or "none", result.Error);
        Assert.True(result.Fights > Trial(1).Map.Nodes.TakeWhile(n => n.Payload is EncounterRef).Count(), $"{result.Fights} fights, {result.Reason}");
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
