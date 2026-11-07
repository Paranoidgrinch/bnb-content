using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The learned player (PlayPolicy, PolicyTraining) and the draft model (DeckModel): the policy reads only what the
// table shows, its training is fixed by its seed, its weights survive the disk, its validation never meets a seed
// it was trained on — and the draft model finds a card's value where the data plants one.
public class PlayPolicyTests
{
    private static TrialPackages Packages { get; } = new(FightProbe.Shipped);

    private static (RogueDeck.Sandbox.Composition.RunPlayback Play, InteractiveCombat Real, Func<string, bool> Needs) Open(string encounter, int seed)
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, 1);
        var fight = walk.Fights.First(f => f.Encounter == encounter);
        using var path = new TrialRunner.Path(FightProbe.Shipped, walk, 3, Packages);
        path.Advance(fight.StopsBefore, new Dictionary<string, int>(), walk.Start);
        var (play, real) = TrialRunner.Open(TrialRunner.Ring(FightProbe.Shipped, fight, path.Loadout(walk.Start)), seed);
        var needs = play.CardNeedsTarget;
        return (play, real, id => needs.TryGetValue(id, out var n) && n);
    }

    private static PlayPolicy Skewed { get; } = new([.. PlayPolicy.Defaults.Select((w, i) => w + 0.37 * (i % 3 - 1))]);

    // ⚠ The swap test (TrialRunnerTests) for the learned player: alone and as the search's reading, the same table
    // with another hidden order and other dice behind it gets the same decision.
    [Theory]
    [InlineData("city_elite_appeal_01", 21, 0)]
    [InlineData("city_normal_seal_02", 22, 0)]
    [InlineData("city_elite_appeal_01", 23, 2)]
    public void A_policy_decision_does_not_change_when_the_hidden_order_and_dice_do(string encounter, int seed, int worlds)
    {
        var (play, real, needs) = Open(encounter, seed);
        using var _ = play;
        var options = new TrialBot.Options(Worlds: worlds, Horizon: 2, Beam: 2, PerTurn: 20, Policy: Skewed, RolloutPerTurn: 6);
        for (var turn = 0; turn < 3 && !real.IsOver; turn++)
        {
            var twin = Worlds.Sample(real, new Random(500 + turn));
            Assert.Equal(Worlds.Visible(real), Worlds.Visible(twin));
            var realDraw = real.State.GetCardZones(real.HeroId).DrawPile.Select(c => c.Id.value).ToList();
            var twinDraw = twin.State.GetCardZones(twin.HeroId).DrawPile.Select(c => c.Id.value).ToList();
            if (realDraw.Count > 3)
                Assert.NotEqual(realDraw, twinDraw);
            var before = real.State.CreateSnapshot();
            var decided = new TrialBot(options, 77, needs).Decide(real);
            var twinDecided = new TrialBot(options, 77, needs).Decide(twin);
            Assert.Equal(Describe(decided), Describe(twinDecided));
            Assert.Equal(CombatStateHasher.ComputeHash(before), CombatStateHasher.ComputeHash(real.State.CreateSnapshot()));
            foreach (var (move, _) in decided)
                if (!real.IsOver)
                    TrialBot.Apply(real, move, out var _);
            if (!real.IsOver)
                real.EndTurn();
        }
    }

    [Fact]
    public void The_policy_alone_plays_a_fight_to_its_end_with_moves_the_engine_accepts()
    {
        var (play, real, needs) = Open("city_easy_queue_04", 4);
        using var _ = play;
        var outcome = new TrialBot(new TrialBot.Options(Worlds: 0, PerTurn: 20, Policy: PlayPolicy.Default), 4, needs).Play(real);
        Assert.Null(outcome.Error);
        Assert.Equal(0, outcome.Refused);
        Assert.True(outcome.Ended);
    }

    // A fight still going never reads better than a fight won, however much the reading likes the buffs.
    [Fact]
    public void Winning_always_reads_better_than_going_on()
    {
        var (play, real, _) = Open("city_easy_01", 6);
        using var __ = play;
        var greedy = new PlayPolicy([.. PlayPolicy.Defaults.Select(_ => -50.0)]);
        Assert.True(greedy.Cost(real, real, real.HeroHealth) >= PlayPolicy.StillStanding);
    }

    // ── training ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Training_is_fixed_by_its_seed()
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, 1);
        var fight = walk.Fights.First(f => f.Encounter == "city_easy_01");
        var deck = new TrialRunner.Loadout([.. walk.Start.Deck.Select(c => c.value)], [.. walk.Start.StartingRelics], walk.Start.MaxHealth);
        var cases = new List<PolicyTraining.Case> { new(fight, deck, 101, 7), new(fight, deck, 202, 8) };
        var options = new PolicyTraining.Options([1], "unused", Pop: 4, Elite: 2, PerTurn: 10, Jobs: 4);
        (double[] W, double Fit) Train() =>
            PolicyTraining.Cem(FightProbe.Shipped, 1, "city_easy_01", PlayPolicy.Default, cases, options, 1, null, () => false, _ => { }) is var r
                ? (r.Policy.Weights, r.TrainFit) : default;
        var once = Train();
        var again = Train();
        Assert.Equal(once.W, again.W);
        Assert.Equal(once.Fit, again.Fit);
    }

    [Fact]
    public void Validation_seeds_never_meet_training_seeds_or_a_measured_seed()
    {
        var shipped = FightProbe.Shipped;
        var acts = new[] { 1, 2, 3, 4, 5 };
        var measured = TrialChoices.MeasuredSeeds(20261106, acts, a => TrialRunner.Walk(shipped, a).Fights.Count, 5, 20);
        var train = new HashSet<int>();
        var valid = new HashSet<int>();
        foreach (var act in acts)
            for (var f = 0; f < TrialRunner.Walk(shipped, act).Fights.Count; f++)
            {
                var (t, v) = PolicyTraining.Seeds(20261106, act, f, 12, 16, measured);
                Assert.Equal(12, t.Count);
                Assert.Equal(16, v.Count);
                Assert.Empty(t.Intersect(v));
                train.UnionWith(t);
                valid.UnionWith(v);
            }
        Assert.Empty(train.Intersect(measured));
        Assert.Empty(valid.Intersect(measured));
        // across encounters too: no validation case of one fight is a training seed of another
        Assert.Empty(train.Intersect(valid));
    }

    [Fact]
    public void Weights_are_written_and_read_back_and_the_book_falls_back()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bnb-policy-" + Guid.NewGuid().ToString("N"));
        try
        {
            var record = Skewed.ToRecord(1);
            record.Name = "city_easy_01";
            record.Done = true;
            record.ValidScore = 3.25;
            PlayPolicy.Save(PolicyBook.PathOf(dir, "city_easy_01"), record);
            var shared = new PlayPolicy([.. PlayPolicy.Defaults.Select(w => w * 2)]).ToRecord(1);
            shared.Name = PolicyBook.SharedName(1);
            PlayPolicy.Save(PolicyBook.PathOf(dir, PolicyBook.SharedName(1)), shared);

            var back = PlayPolicy.FromRecord(PlayPolicy.Load(PolicyBook.PathOf(dir, "city_easy_01"))!);
            Assert.Equal(Skewed.Weights.Select(w => Math.Round(w, 4)), back.Weights);
            Assert.Equal(3.25, PlayPolicy.Load(PolicyBook.PathOf(dir, "city_easy_01"))!.ValidScore);

            var book = new PolicyBook(dir);
            Assert.Equal(back.Weights, book.For("city_easy_01", 1).Weights);
            Assert.Equal(PlayPolicy.Defaults.Select(w => Math.Round(w * 2, 4)), book.For("city_boss_01", 1).Weights);
            Assert.Same(PlayPolicy.Default, book.For("city_boss_01", 2));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    // ── the draft model ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_draft_model_finds_a_planted_card_value_and_round_trips()
    {
        var reader = new DeckModel.Reader(FightProbe.Shipped, new DeckThemes(FightProbe.Shipped));
        var dice = new Random(5);
        var basics = new[] { "lt_watch_cut", "lt_buckler_up" };
        var rows = new List<DeckModel.Row>();
        for (var r = 0; r < 300; r++)
        {
            var deck = Enumerable.Range(0, 10).Select(_ => basics[dice.Next(2)]).ToList();
            var good = dice.Next(3);
            var bad = dice.Next(2);
            deck.AddRange(Enumerable.Repeat("lt_gauntlet", good));
            deck.AddRange(Enumerable.Repeat("lt_scabbard", bad));
            var score = 20 - 4.0 * good + 3.0 * bad + dice.NextDouble();
            rows.Add(new DeckModel.Row(1, 1, r, 2, [.. deck], ["trial_infirmary"], score, 4));
        }
        var model = DeckModel.Fit(rows, reader, lambda: 1.0);
        Assert.True(model.Beta["card:a1:lt_gauntlet"] < -2, $"gauntlet {model.Beta["card:a1:lt_gauntlet"]}");
        Assert.True(model.Beta["card:a1:lt_scabbard"] > 1, $"scabbard {model.Beta["card:a1:lt_scabbard"]}");
        var now = new TrialRunner.Loadout([.. basics, .. basics], ["trial_infirmary"], 80);
        var gain = model.Gain(reader, 1, 2, now.Deck, now.Relics, DeckModel.After(now, "card", "lt_gauntlet").Deck, now.Relics);
        Assert.True(gain > 0);

        var file = Path.Combine(Path.GetTempPath(), "bnb-model-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            model.Save(file);
            var back = DeckModel.Load(file)!;
            Assert.Equal(model.Beta, back.Beta);
            Assert.Contains("lt_gauntlet", File.ReadAllText(Path.ChangeExtension(file, ".txt")));
        }
        finally
        {
            File.Delete(file);
            File.Delete(Path.ChangeExtension(file, ".txt"));
        }
    }

    // An upgrade is what the "+" adds to its card, not a second card that replaces the first: before, the starters'
    // plain copies and their upgrades came out as mirror images of each other (2026-11-07).
    [Fact]
    public void The_draft_model_reads_an_upgrade_on_top_of_its_card_and_shares_values_across_acts()
    {
        var reader = new DeckModel.Reader(FightProbe.Shipped, new DeckThemes(FightProbe.Shipped));
        var dice = new Random(7);
        var rows = new List<DeckModel.Row>();
        for (var r = 0; r < 300; r++)
        {
            var act = 1 + r % 2;
            var upgraded = dice.Next(2) == 0;
            var deck = new List<string> { "lt_buckler_up", "lt_buckler_up", upgraded ? "lt_watch_cut+" : "lt_watch_cut", "lt_watch_cut" };
            var good = dice.Next(3);
            deck.AddRange(Enumerable.Repeat("lt_gauntlet", good));
            var score = 20 - 3.0 * good - (upgraded ? 5.0 : 0) + dice.NextDouble();
            rows.Add(new DeckModel.Row(1, act, r, 2, [.. deck], ["trial_infirmary"], score, 4));
        }
        var model = DeckModel.Fit(rows, reader, lambda: 1.0, synergies: false, pooled: true);
        Assert.True(model.Beta["up:a1:lt_watch_cut"] + model.Beta["up:*:lt_watch_cut"] < -3);
        // the plain card did not change with the upgrade, so it does not read as its mirror
        Assert.InRange(model.Beta["card:a1:lt_watch_cut"] + model.Beta["card:*:lt_watch_cut"], -1, 1);
        // the shared term carries part of what both acts agree on
        Assert.True(model.Beta["card:*:lt_gauntlet"] < -0.5, $"shared gauntlet {model.Beta["card:*:lt_gauntlet"]}");
        Assert.Equal(["lt_buckler_up", "lt_watch_cut"], model.Starters[1]);
        Assert.Contains("upgrades", model.Report());
    }

    private static List<string> Describe(IReadOnlyList<(TrialBot.Move Move, string Expected)> plan) =>
        [.. plan.Select(p => $"{p.Move.Card?.value}|{p.Move.Action?.value}|{p.Move.Target?.value}|{string.Join(";", p.Move.Script)}")];
}
