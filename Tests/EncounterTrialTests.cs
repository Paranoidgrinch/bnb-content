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

    private static IReadOnlyList<EncounterTrial.Single> Singles(int act) =>
        JsonSerializer.Deserialize<List<EncounterTrial.Single>>(
            FightProbe.Shipped.Presentation.Game!.Extra[EncounterTrial.EncountersKey(act)], RunJson.CreateOptions())!;

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
    [InlineData(4, "labyrinth_boss_")]
    [InlineData(5, "act_5_")]
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
        // The earlier acts' packages and boss spoils before the first fight; then each stage and its package,
        // straight after its stage's last fight.
        var spoils = trial.Map.Nodes.Where(n => n.Payload is EventRef).Select(n => ((EventRef)n.Payload!).Id.Value).ToList();
        var catchUp = Enumerable.Range(1, act - 1)
            .SelectMany(earlier => Enumerable.Range(1, EncounterTrial.Stages(earlier)).Select(s => EncounterTrial.SpoilsId(earlier, s)).Append(EncounterTrial.BossSpoilsId(earlier)))
            .ToList();
        Assert.Equal([.. catchUp, .. Enumerable.Range(1, EncounterTrial.Stages(act)).Select(s => EncounterTrial.SpoilsId(act, s))], spoils);
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
    [InlineData(4)]
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

    // ── ONE FIGHT ON ITS OWN (EncounterTrial.Singles) ──────────────────────────────────────────────────────────

    // The list is the full walk's fights, every one of them, in its order, each with its name, stage and kind.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void An_act_lists_every_fight_of_its_trial(int act)
    {
        var trial = Trial(act);
        var singles = Singles(act);
        var fights = trial.Map.Nodes.Where(n => n.Payload is EncounterRef).ToList();
        Assert.Equal(fights.Select(n => ((EncounterRef)n.Payload!).Id.Value), singles.Select(s => s.Encounter));
        var stages = EncounterTrial.Stages(act);
        var pool = FightProbe.Shipped.Acts![act - 1].MapGeneration!.Encounters!;
        foreach (var single in singles)
        {
            Assert.Equal(FightProbe.Shipped.Presentation.Encounters[single.Encounter].FlavorText, single.Name);
            Assert.False(string.IsNullOrWhiteSpace(single.Name));
            var kind = single.Kind switch
            {
                "normal" => MapNodeKind.Combat,
                "multi" => MapNodeKind.MultiCombat,
                "elite" => MapNodeKind.Elite,
                "boss" => MapNodeKind.Boss,
                _ => throw new Xunit.Sdk.XunitException($"{single.Encounter}: kind {single.Kind}"),
            };
            Assert.Contains(pool.For(kind), e => e.Encounter.Value == single.Encounter);
            Assert.InRange(single.Stage, 1, stages + 1);
            Assert.Equal(kind == MapNodeKind.Boss, single.Stage == stages + 1);
        }
        // Stage by stage, and within a stage normal, multi, elite — the walk's own order.
        Assert.Equal(singles.Select(s => s.Stage).Order(), singles.Select(s => s.Stage));
        foreach (var stage in singles.GroupBy(s => s.Stage).Where(g => g.Key <= stages))
            Assert.Equal(stage.Select(s => s.Kind).OrderBy(k => Array.IndexOf(["normal", "multi", "elite"], k)), stage.Select(s => s.Kind));
        Assert.Equal(pool.For(MapNodeKind.Boss).Select(e => e.Encounter.Value).Distinct().Count(), singles.Count(s => s.Kind == "boss"));
    }

    // Before a fight of stage k: the earlier acts' catch-up and this act's packages 1 … k−1, from the very stops
    // of the full walk; then the fight itself, as the full walk fields it, and nothing after it.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void A_single_fight_opens_on_the_spoils_before_its_stage(int act)
    {
        var trial = Trial(act);
        var catchUp = Enumerable.Range(1, act - 1)
            .SelectMany(earlier => Enumerable.Range(1, EncounterTrial.Stages(earlier)).Select(s => EncounterTrial.SpoilsId(earlier, s)).Append(EncounterTrial.BossSpoilsId(earlier)))
            .ToList();
        foreach (var single in Singles(act))
        {
            var nodes = single.Walk.Map.Nodes;
            var spoils = nodes.SkipLast(1).Select(n => Assert.IsType<EventRef>(n.Payload).Id.Value).ToList();
            Assert.Equal([.. catchUp, .. Enumerable.Range(1, single.Stage - 1).Select(s => EncounterTrial.SpoilsId(act, s))], spoils);
            var fight = trial.Map.Nodes.Single(n => n.Payload is EncounterRef r && r.Id.Value == single.Encounter);
            Assert.Equal(fight.Id, nodes[^1].Id);
            Assert.Equal(fight.Type, nodes[^1].Type);
            Assert.Equal(fight.Tags, nodes[^1].Tags);
            Assert.Null(Assert.IsType<EncounterRef>(nodes[^1].Payload).VictoryReward);
            // The very stops of the full walk, by their own ids, and the same start with the Infirmary.
            Assert.All(nodes.SkipLast(1), n => Assert.Contains(trial.Map.Nodes, t => t.Id == n.Id && t.Payload!.Equals(n.Payload)));
            Assert.Equal(trial.Start!.Deck, single.Walk.Start!.Deck);
            Assert.Equal(trial.Start.StartingRelics, single.Walk.Start.StartingRelics);
        }
        // Act I's first stage opens on nothing at all; a boss on every package of its act.
        if (act == 1)
            Assert.Single(Singles(act).First(s => s.Stage == 1).Walk.Map.Nodes);
        Assert.Equal(catchUp.Count + EncounterTrial.Stages(act) + 1, Singles(act).First(s => s.Kind == "boss").Walk.Map.Nodes.Count);
    }

    // Walked by a bot: the packages, then the one fight, and the run is over — won or lost.
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task A_bot_walks_a_single_fight_to_its_end(int act)
    {
        var single = Singles(act).First(s => s.Stage == 2 && s.Kind == "normal");
        using var play = new RunPlayback(() => { });
        play.Start(Walk(single.Walk, health: 400), seed: 5, interactive: true, characterId: LieutenantCharacter.Id);
        Assert.Null(play.Error);
        var result = await RunBot.Play(play, new BotOptions { Seed = 5, Budget = 3000 }, new DelegateBotLog(_ => { }));
        Assert.True(result.Error is "" or "none", result.Error);
        Assert.Equal(1, result.Fights);
        Assert.NotEqual(RunResult.Ongoing, play.Session!.Run.Result);
        Assert.True(play.Session.Run.Deck.Count > LieutenantCharacter.Start.Deck.Count);
    }

    // ── Act V: no stages, the six gods ────────────────────────────────────────────────────────────────────

    // The gauntlet has no stages: the catch-up of Acts I–IV, and then the six gods one after another, the
    // Infirmary between them. Every god can also be met on its own, after the same packages.
    [Fact]
    public void Act_five_is_the_catch_up_and_then_all_six_gods()
    {
        Assert.Equal(0, EncounterTrial.Stages(5));
        var trial = Trial(5);
        var gods = FightProbe.Shipped.Acts![4].MapGeneration!.Encounters!.For(MapNodeKind.Boss)
            .Select(e => e.Encounter.Value).Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.Equal(6, gods.Count);
        var fights = trial.Map.Nodes.Where(n => n.Payload is EncounterRef).Select(n => ((EncounterRef)n.Payload!).Id.Value).ToList();
        Assert.Equal(gods, fights);
        // the catch-up first, all of it, then nothing but the gods
        Assert.All(trial.Map.Nodes.Take(trial.Map.Nodes.Count - 6), n => Assert.IsType<EventRef>(n.Payload));
        Assert.Equal(Enumerable.Range(1, 4).Sum(earlier => EncounterTrial.Stages(earlier) + 1),
            trial.Map.Nodes.Count(n => n.Payload is EventRef));
        Assert.All(Singles(5), single =>
        {
            Assert.Equal("boss", single.Kind);
            Assert.Equal(1, single.Stage);
            Assert.Equal(trial.Map.Nodes.Count(n => n.Payload is EventRef) + 1, single.Walk.Map.Nodes.Count);
        });
        Assert.Equal(gods, Singles(5).Select(s => s.Encounter));
    }

    // A bot walks the catch-up and into the first god (and the run ends there or later: a god at 400 HP of the
    // Lieutenant's is not a fight the random bot is expected to win).
    [Fact]
    public async Task A_bot_walks_act_fives_catch_up_into_the_gods()
    {
        using var play = new RunPlayback(() => { });
        play.Start(Walk(Trial(5), health: 400), seed: 4, interactive: true, characterId: LieutenantCharacter.Id);
        Assert.Null(play.Error);
        var result = await RunBot.Play(play, new BotOptions { Seed = 4, Budget = 4000 }, new DelegateBotLog(_ => { }));
        Assert.True(result.Error is "" or "none", result.Error);
        Assert.True(result.Fights >= 1, $"{result.Fights} fights, {result.Reason}");
        Assert.True(play.Session!.Run.Deck.Count > LieutenantCharacter.Start.Deck.Count + 4);
    }

    // …and every god on its own, to the end of its fight: no god holds the bot in a turn or a prompt for ever.
    [Theory]
    [InlineData("act_5_nisaba_keeper_of_the_first_tablet")]
    [InlineData("act_5_inanna_mistress_of_the_eanna_ledger")]
    [InlineData("act_5_nanshe_keeper_of_the_just_ration")]
    [InlineData("act_5_nanna_sin_lord_of_the_counted_moon")]
    [InlineData("act_5_utu_witness_of_every_oath")]
    [InlineData("act_5_enlil_voice_of_the_unalterable_decree")]
    public async Task A_bot_walks_a_single_god_to_its_end(string god)
    {
        var single = Singles(5).Single(s => s.Encounter == god);
        using var play = new RunPlayback(() => { });
        play.Start(Walk(single.Walk, health: 400), seed: 5, interactive: true, characterId: LieutenantCharacter.Id);
        Assert.Null(play.Error);
        var result = await RunBot.Play(play, new BotOptions { Seed = 5, Budget = 4000 }, new DelegateBotLog(_ => { }));
        Assert.True(result.Error is "" or "none", result.Error);
        Assert.Equal(1, result.Fights);
        Assert.NotEqual(RunResult.Ongoing, play.Session!.Run.Result);
    }
}
