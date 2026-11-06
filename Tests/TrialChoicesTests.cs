using BnbContent.Converter.Playtest;

namespace BnbContent.Tests;

// The trial runner's package choices by play (TrialChoices) and what it learns across runs (CardValues): fixed by
// their seed, judged on seeds no measured fight uses, kept and read back, and able to tell a good card from none.
public class TrialChoicesTests
{
    private static readonly TrialChoices.Options Tiny = new(new TrialBot.Options(Worlds: 1, Horizon: 2, Beam: 2, PerTurn: 15), Seeds: 1);

    private static TrialPackages Packages { get; } = new(FightProbe.Shipped);

    private static DeckThemes Themes { get; } = new(FightProbe.Shipped);

    private static readonly ParallelOptions Parallel = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

    private static TrialChoices Choices(TrialRunner.TrialWalk walk, TrialChoices.Options options, CardValues? prior = null) =>
        new(FightProbe.Shipped, walk, Packages, prior ?? new CardValues(), options, 20261106,
            TrialChoices.MeasuredSeeds(20261106, [walk.Act], _ => walk.Fights.Count, 5, 20), Parallel, Themes);

    [Fact]
    public void Choices_by_play_are_fixed_by_their_seed()
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, 1);
        (List<string> Picks, List<IReadOnlyList<int>> Answers, List<TrialChoices.Learned> Learned) Walked()
        {
            var choices = Choices(walk, Tiny);
            using var path = new TrialRunner.Path(FightProbe.Shipped, walk, 17, Packages, (p, d) => choices.Choose(p, d, 1, 0));
            path.Advance(1, new Dictionary<string, int>(), walk.Start);
            return ([.. path.Picks], [.. path.Answers], [.. choices.LearnedBy.OrderBy(kv => kv.Key).SelectMany(kv => kv.Value)]);
        }
        var once = Walked();
        var again = Walked();
        Assert.Equal(once.Picks, again.Picks);
        Assert.Equal(once.Answers.Select(a => string.Join(",", a)), again.Answers.Select(a => string.Join(",", a)));
        Assert.Equal(once.Learned, again.Learned);
        // the first stop's two card offers were played out (three cards each), and its improvement
        Assert.Contains(once.Picks, p => p.Contains(" · play ", StringComparison.Ordinal));
        Assert.True(once.Learned.Count(l => l.Kind == "card") >= 4);
    }

    // ⚠ No evaluation fight plays a seed a measured fight plays — over every act, at the full defaults and beyond.
    [Fact]
    public void Evaluation_seeds_never_meet_a_measured_seed()
    {
        var shipped = FightProbe.Shipped;
        var acts = new[] { 1, 2, 3, 4, 5 };
        var counts = acts.ToDictionary(a => a, a => TrialRunner.Walk(shipped, a).Fights.Count);
        var measured = TrialChoices.MeasuredSeeds(20261106, acts, a => counts[a], 5, 20);
        Assert.InRange(measured.Count, acts.Sum(a => counts[a]) * 5 * 20 - 50, acts.Sum(a => counts[a]) * 5 * 20);
        var eval = new HashSet<int>();
        foreach (var act in acts)
            for (var p = 0; p < 5; p++)
                for (var d = 0; d < 100; d += 7)
                    for (var f = 0; f < counts[act]; f++)
                        for (var n = 0; n < 3; n++)
                            eval.Add(TrialChoices.EvalSeed(20261106, act, p, d, f, n, measured));
        Assert.Empty(eval.Intersect(measured));
        // a seed that would fall on a measured one is drawn again
        var first = TrialChoices.EvalSeed(1, 1, 0, 0, 0, 0, new HashSet<int>());
        var moved = TrialChoices.EvalSeed(1, 1, 0, 0, 0, 0, new HashSet<int> { first });
        Assert.NotEqual(first, moved);
    }

    [Fact]
    public void The_learned_table_is_written_and_read_back()
    {
        var dir = Directory.CreateTempSubdirectory("lt-values-");
        try
        {
            var file = Path.Combine(dir.FullName, "lt-card-values.json");
            var values = new CardValues { Runs = 2 };
            values.Add(new TrialChoices.Learned("card", "lt_dare", 1, "a1|open_guard+strength", 6, 18));
            values.Add(new TrialChoices.Learned("card", "lt_dare", 2, "a2|authority+open_guard", 2, 18));
            values.Add(new TrialChoices.Learned("relic", "tarnished_bell", 1, "a1|open_guard+strength", -3, 15));
            values.Save(file);

            var back = CardValues.Load(file);
            Assert.Equal(2, back.Runs);
            Assert.Equal(4, back.Mean("card", "lt_dare"));
            Assert.Equal(-3, back.Mean("relic", "tarnished_bell"));
            Assert.Null(back.Mean("card", "lt_whistle"));
            Assert.Equal(2, back.Entries["card:lt_dare"].All.N);
            Assert.Equal(6, back.Entries["card:lt_dare"].Contexts["a1|open_guard+strength"].Mean);
            var report = File.ReadAllText(Path.ChangeExtension(file, ".txt"));
            Assert.Contains("lt_dare", report, StringComparison.Ordinal);
            Assert.Contains("tarnished_bell", report, StringComparison.Ordinal);
            // a missing table is an empty one
            Assert.Empty(CardValues.Load(Path.Combine(dir.FullName, "none.json")).Entries);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    // A clearly better card wins over none: the start deck with an upgraded strong attack against the start deck
    // with one more Watch Cut (the Strike), and against the start deck as it is.
    [Fact]
    public void A_clearly_better_card_is_picked_over_none()
    {
        var walk = TrialRunner.Walk(FightProbe.Shipped, 1);
        var start = new TrialRunner.Loadout([.. walk.Start.Deck.Select(c => c.value)], [.. walk.Start.StartingRelics], walk.Start.MaxHealth);
        TrialRunner.Loadout With(string card) => start with { Deck = [.. start.Deck, card] };
        var choices = Choices(walk, Tiny with { Seeds = 2 });
        var fights = walk.Fights.Where(f => f.Stage == 2 && f.Kind is "elite" or "multi").ToList();
        var scores = choices.Scores([With("lt_shield_punch+"), With("lt_watch_cut"), start], fights, 1, 0, 0);
        var pick = TrialChoices.Best(scores, [true, true, true], [0, 0, 0], 0.5);
        Assert.True(pick == 0, $"scores {string.Join(", ", scores.Select(s => s.ToString("0.0")))}");
    }

    [Fact]
    public void Within_the_tie_band_the_prior_decides_and_beyond_it_the_play()
    {
        Assert.Equal(1, TrialChoices.Best([10.0, 10.3, 12], [true, true, true], [0, 5, 9], 0.5));
        Assert.Equal(0, TrialChoices.Best([10.0, 11, 12], [true, true, true], [0, 5, 9], 0.5));
        // none cannot be chosen where the run does not allow it
        Assert.Equal(1, TrialChoices.Best([10.0, 11], [false, true], [0, 0], 0.5));
    }
}
