using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Lieutenant trial runner (LT_RUNNER_PLAN.md): it walks the trial's own walk, its package choices are fixed by
// their seed, its bot plays only what the engine accepts — and it never reads the real draw order or a future roll.
public class TrialRunnerTests
{
    private static readonly TrialBot.Options Cheap = new(Worlds: 2, Horizon: 2, Beam: 2, PerTurn: 20);

    private static TrialPackages Packages { get; } = new(FightProbe.Shipped);

    // ── the walk ────────────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void The_walk_is_the_trials_own(int act)
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, act);
        var trial = TrialRunner.Trial(FightProbe.Shipped, act);
        var singles = TrialRunner.Singles(FightProbe.Shipped, act);
        // the stops are the trial's event nodes, in order; the fights its fights, each with the stops before it
        Assert.Equal(trial.Map.Nodes.Where(n => n.Payload is EventRef).Select(n => n.Id), walk.Stops.Select(n => n.Id));
        Assert.Equal(singles.Select(s => (s.Encounter, s.Stage, s.Kind)), walk.Fights.Select(f => (f.Encounter, f.Stage, f.Kind)));
        foreach (var (fight, single) in walk.Fights.Zip(singles))
        {
            Assert.Equal(single.Walk.Map.Nodes.Count - 1, fight.StopsBefore);
            Assert.Equal(single.Walk.Map.Nodes[^1].Id, fight.Node.Id);
            Assert.Equal(single.Walk.Map.Nodes[^1].Type, fight.Node.Type);
            Assert.Equal(single.Walk.Map.Nodes[^1].Tags, fight.Node.Tags);
            Assert.Equal(single.Walk.Map.Nodes.SkipLast(1).Select(n => n.Id), walk.Stops.Take(fight.StopsBefore).Select(n => n.Id));
        }
        Assert.Equal(trial.Start!.Deck, walk.Start.Deck);
        Assert.Equal(trial.Start.StartingRelics, walk.Start.StartingRelics);
    }

    // ── the packages ────────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void The_package_choices_are_fixed_by_their_seed(int act)
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, act);
        var played = new Dictionary<string, int> { ["lt_watch_cut"] = 5 };
        (IReadOnlyList<string> Picks, TrialRunner.Loadout Loadout) Walked(int seed)
        {
            using var path = new TrialRunner.Path(FightProbe.Shipped, walk, seed, Packages);
            path.Advance(walk.Stops.Count, played, walk.Start);
            Assert.Equal(walk.Stops.Count, path.StopsDone);
            return ([.. path.Picks], path.Loadout(walk.Start));
        }
        var once = Walked(17);
        var again = Walked(17);
        Assert.Equal(once.Picks, again.Picks);
        Assert.Equal(once.Loadout.Deck, again.Loadout.Deck);
        Assert.Equal(once.Loadout.Relics, again.Loadout.Relics);
        // every stop was opened and collected; the deck changed on the way
        Assert.Equal(walk.Stops.Count, once.Picks.Count(p => p.EndsWith(": collect", StringComparison.Ordinal)));
        Assert.NotEqual(walk.Start.Deck.Select(c => c.value), once.Loadout.Deck);
    }

    // A walk cut short stops before the next stop: Act I's second stage meets only the first package.
    [Fact]
    public void A_path_stops_before_the_stop_it_was_not_asked_for()
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, 1);
        using var path = new TrialRunner.Path(FightProbe.Shipped, walk, 5, Packages);
        path.Advance(1, new Dictionary<string, int>(), walk.Start);
        Assert.Equal(1, path.StopsDone);
        var deck = path.Loadout(walk.Start).Deck;
        // two card offers (each maybe skipped) and one improvement: at most two cards more than the start
        Assert.InRange(deck.Count, walk.Start.Deck.Count, walk.Start.Deck.Count + 2);
        Assert.Single(path.Picks, p => p.EndsWith(": collect", StringComparison.Ordinal));
    }

    // ── the bot ─────────────────────────────────────────────────────────────────────────────────────────────

    private static (RogueDeck.Sandbox.Composition.RunPlayback Play, InteractiveCombat Real, Func<string, bool> Needs) Open(
        string encounter, int seed, int stopsBefore = -1)
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, 1);
        var fight = walk.Fights.First(f => f.Encounter == encounter);
        using var path = new TrialRunner.Path(FightProbe.Shipped, walk, 3, Packages);
        path.Advance(stopsBefore < 0 ? fight.StopsBefore : stopsBefore, new Dictionary<string, int>(), walk.Start);
        var (play, real) = TrialRunner.Open(TrialRunner.Ring(FightProbe.Shipped, fight, path.Loadout(walk.Start)), seed);
        var needs = play.CardNeedsTarget;
        return (play, real, id => needs.TryGetValue(id, out var n) && n);
    }

    [Theory]
    [InlineData("city_easy_queue_06", 1)]
    [InlineData("city_easy_queue_04", 2)]
    [InlineData("city_elite_delay_01", 3)]
    public void The_search_plays_only_moves_the_engine_accepts(string encounter, int seed)
    {
        var (play, real, needs) = Open(encounter, seed);
        using var _ = play;
        var outcome = new TrialBot(Cheap, seed, needs).Play(real);
        Assert.Null(outcome.Error);
        Assert.Equal(0, outcome.Refused);
        Assert.True(outcome.Ended, $"{encounter}: not over after {outcome.Rounds} rounds");
        Assert.True(outcome.Played.Values.Sum() > 0);
    }

    // ⚠ THE POINT THE USER STRESSED MOST: the bot knows the piles' contents, never their order, never a future roll.
    // The same table with the draw pile in another order and other dice behind it must get the same decision —
    // and the real fight is never moved by deciding.
    [Theory]
    [InlineData("city_elite_appeal_01", 11)]
    [InlineData("city_boss_01", 12)]
    public void A_decision_does_not_change_when_the_hidden_order_and_dice_do(string encounter, int seed)
    {
        var (play, real, needs) = Open(encounter, seed);
        using var _ = play;
        for (var turn = 0; turn < 3 && !real.IsOver; turn++)
        {
            var twin = Worlds.Sample(real, new Random(1000 + turn));
            // the twin is the same table…
            Assert.Equal(Worlds.Visible(real), Worlds.Visible(twin));
            // …with another future behind it
            Assert.NotEqual(real.State.RandomSeed, twin.State.RandomSeed);
            var realDraw = real.State.GetCardZones(real.HeroId).DrawPile.Select(c => c.Id.value).ToList();
            var twinDraw = twin.State.GetCardZones(twin.HeroId).DrawPile.Select(c => c.Id.value).ToList();
            if (realDraw.Count > 3)
                Assert.NotEqual(realDraw, twinDraw);

            var before = real.State.CreateSnapshot();
            var bot = new TrialBot(Cheap, 77, needs);
            var decided = bot.Decide(real);
            var twinDecided = new TrialBot(Cheap, 77, needs).Decide(twin);
            Assert.Equal(Describe(decided), Describe(twinDecided));
            // deciding looked at copies only
            Assert.Equal(CombatStateHasher.ComputeHash(before), CombatStateHasher.ComputeHash(real.State.CreateSnapshot()));

            // play the turn on the real fight and look again next turn
            foreach (var (move, _) in decided)
                if (!real.IsOver)
                    TrialBot.Apply(real, move, out var _);
            if (!real.IsOver)
                real.EndTurn();
        }
    }

    // A world is a different future each time it is drawn — otherwise the test above proves nothing.
    [Fact]
    public void Worlds_differ_in_what_is_hidden_and_agree_on_what_is_seen()
    {
        var (play, real, _) = Open("city_boss_01", 5);
        using var __ = play;
        var a = Worlds.Sample(real, new Random(1));
        var b = Worlds.Sample(real, new Random(2));
        Assert.Equal(Worlds.Visible(a), Worlds.Visible(b));
        Assert.NotEqual(a.State.GetCardZones(a.HeroId).DrawPile.Select(c => c.Id.value),
            b.State.GetCardZones(b.HeroId).DrawPile.Select(c => c.Id.value));
        Assert.NotEqual(a.State.RandomSeed, b.State.RandomSeed);
        // the same dice draw the same world, whichever order the real pile was in
        var again = Worlds.Sample(Worlds.Sample(real, new Random(9)), new Random(1));
        Assert.Equal(a.State.GetCardZones(a.HeroId).DrawPile.Select(c => c.Id.value),
            again.State.GetCardZones(again.HeroId).DrawPile.Select(c => c.Id.value));
    }

    private static List<string> Describe(IReadOnlyList<(TrialBot.Move Move, string Expected)> plan) =>
        [.. plan.Select(p => $"{p.Move.Card?.value}|{p.Move.Action?.value}|{p.Move.Target?.value}|{string.Join(";", p.Move.Script)}")];
}
