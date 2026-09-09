using RogueDeck.Run;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// WHAT ONE FIGHT COST, AND WHAT TWENTY OF THEM SAY.
//
// The walk reports rooms because a run is rooms. A sparring match reports the two numbers a fight is actually
// about — how long it took and what it took out of you — and then reports them again nineteen times, because
// one fight is an anecdote. The same loadout shuffled differently is the whole spread, and the spread is the
// answer to "is this elite fair"; a single number is the answer to "did this elite crash".
//
// The player is `Greedy`, shared with the walk. That is a deliberate floor rather than a model of a human: a
// greedy player is roughly the worst competent play, so a fight it wins comfortably is not hard, and a fight
// it loses may still be fine. Read the numbers as a LOWER BOUND on the deck, and pin an intent when the
// question is about one of the enemy's moves rather than about the fight.
public static class SparringMatch
{
    public sealed record Round(int Turn, int HeroHealth, int EnemyHealth, int Plays);

    public sealed record Result(
        int Seed,
        bool Won,
        bool Ended,
        int Turns,
        int HeroHealth,
        int HeroMaxHealth,
        int HeroHealthLost,
        int EnemyHealthLost,
        int EnemyHealthLeft,
        string? Error,
        IReadOnlyList<Round> Rounds)
    {
        public string Line =>
            Error is { } broken ? $"seed {Seed,4}: BROKE — {broken}"
            : !Ended ? $"seed {Seed,4}: UNFINISHED after {Turns} turns — {EnemyHealthLeft} enemy HP still up"
            : Won ? $"seed {Seed,4}: won in {Turns,3} turns, hero {HeroHealth,4}/{HeroMaxHealth} (−{HeroHealthLost})"
            : $"seed {Seed,4}: DIED on turn {Turns,3}, with {EnemyHealthLeft} enemy HP still up";
    }

    // One fight, one seed. The blueprint is already the ring: the map is one room and the loadout is in Start.
    public static Result Fight(RunBlueprint ring, int seed, int turnBudget, Action<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(ring);

        var rng = new Random(seed);
        var rounds = new List<Round>();
        var play = new RunPlayback(() => { });
        using var _ = play;
        play.Start(ring, seed, interactive: true);
        if (play.Error is { } startError)
            return Broken(seed, startError, rounds);

        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        if (session.Error is { } interludeError)
            return Broken(seed, interludeError, rounds);
        if (play.CombatDriver?.Current is not { } opening)
            return Broken(seed, "the ring never started a fight", rounds);

        var driver = play.CombatDriver;
        var heroMax = opening.State.GetCombatant(opening.HeroId).Health.Max;
        var heroAtFirstBell = opening.State.GetCombatant(opening.HeroId).Health.Current;
        var enemiesAtFirstBell = EnemyHealth(opening);
        var heroNow = heroAtFirstBell;
        var enemiesNow = enemiesAtFirstBell;

        var refused = new HashSet<CardInstanceId>();
        var barren = new HashSet<string>(StringComparer.Ordinal);
        var turn = 0;

        for (; turn < turnBudget && driver.Current is not null; turn++)
        {
            refused.Clear();
            barren.Clear();
            var plays = 0;
            string? lastPlayed = null;
            var tableBeforeThePlay = "";

            while (driver.Current is { } combat)
            {
                if (Greedy.Answer(driver, rng))
                    continue;
                if (play.Error is { } midError)
                    return Broken(seed, midError, rounds, turn, heroNow, heroMax, heroAtFirstBell,
                        enemiesAtFirstBell, enemiesNow, play, driver.Current, progress);
                if (session.Error is { } midRunError)
                    return Broken(seed, midRunError, rounds, turn, heroNow, heroMax, heroAtFirstBell,
                        enemiesAtFirstBell, enemiesNow, play, driver.Current, progress);

                // Only HERE is the previous play finished: a card that asks a question parks halfway through
                // its own resolution, so a reading taken the moment PlayCard returns straddles an open one.
                if (lastPlayed is { } finished)
                {
                    if (Greedy.TableState(combat) == tableBeforeThePlay)
                        barren.Add(finished);
                    lastPlayed = null;
                }

                if (Greedy.Choose(play, combat, rng, refused, barren) is not { } card)
                    break;

                var needsTarget = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var needs) && needs;
                var stepsBefore = combat.Steps.Count;
                tableBeforeThePlay = Greedy.TableState(combat);
                lastPlayed = card.DefinitionId.value;
                driver.PlayCard(card.Id, needsTarget ? Greedy.Enemy(combat) : null);
                if (++plays >= Greedy.PlaysInATurnNobodyMakes)
                    return Broken(seed, $"a turn played {plays} cards without ending — "
                        + $"last '{card.DefinitionId.value}'", rounds, turn, heroNow, heroMax,
                        heroAtFirstBell, enemiesAtFirstBell, enemiesNow, play, driver.Current, progress);
                if (Greedy.Refused(driver.Current, stepsBefore))
                    refused.Add(card.Id);
            }

            if (driver.Current is null)
                break;
            if (Greedy.Answer(driver, rng))
                continue;

            driver.EndTurn();

            if (driver.Current is { } after)
            {
                heroNow = after.State.GetCombatant(after.HeroId).Health.Current;
                enemiesNow = EnemyHealth(after);
                rounds.Add(new Round(turn + 1, heroNow, enemiesNow, plays));
            }
            else
            {
                // ⚠ THE FIGHT ENDED IN THIS TURN, AND THAT SAYS NOTHING ABOUT WHO WON. Writing a zero here
                // for the enemies — "the fight is over, so they must be dead" — is what made every fight
                // that ended read as a victory, including twenty in a row the fighter did not survive. The
                // last honest number is the last one that was read.
                rounds.Add(new Round(turn + 1, heroNow, enemiesNow, plays));
                turn++;
                break;
            }
        }

        // ⚠ THE LAST SAMPLE IS NOT THE LAST STATE, and reading it as one is the trap this instrument sets for
        // itself. The table can only be read while the fight exists; the killing blow is the moment it stops
        // existing, so whatever was sampled last was taken BEFORE the blow that ended it. Read that way, a
        // clerk killed inside the first turn is "10 enemy HP still up" and the fight reads as lost.
        //
        // The run outlives the fight and knows both answers: whether the room was cleared, and what the
        // fighter walked out on. So the fight is sampled turn by turn for the SHAPE of it, and the verdict is
        // taken from the run.
        var ended = driver.Current is null;
        var survivor = session.Run.Primary.Health;
        if (ended)
        {
            heroNow = survivor.Current;
            if (session.Run.Result != RunResult.Defeat && heroNow > 0)
                enemiesNow = 0;
        }
        var won = ended && session.Run.Result != RunResult.Defeat && heroNow > 0;

        return new Result(
            seed, won, ended, turn, heroNow, heroMax,
            heroAtFirstBell - heroNow, enemiesAtFirstBell - enemiesNow, enemiesNow,
            Error: null, rounds);
    }

    // ── the spread ────────────────────────────────────────────────────────────────────────────────────────

    // The same loadout, `fights` times, shuffled differently each time. The report is the spread and not the
    // mean, because a fight that is lost one time in ten is a different design fact from one that costs ten
    // per cent more health, and an average hides exactly that difference.
    public static int Run(
        RunBlueprint game, SparringRing.Snapshot snapshot, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(say);

        RunBlueprint ring;
        try
        {
            ring = SparringRing.Assemble(game, snapshot);
        }
        catch (Exception assembly) when (assembly is InvalidOperationException or ArgumentException)
        {
            say($"the ring could not be set: {assembly.Message}");
            return 1;
        }

        say($"── {snapshot.Title} ──");
        say($"   {Loadout(snapshot)}");

        var results = new List<Result>();
        for (var i = 0; i < Math.Max(1, snapshot.Fights); i++)
        {
            var result = Fight(ring, snapshot.Seed + i, snapshot.TurnBudget, say);
            results.Add(result);
            // Every fight gets a line when there are few of them; over a spread the lines are noise and only
            // the ones that went wrong are worth reading.
            if (snapshot.Fights <= 5 || !result.Ended || result.Error is not null || !result.Won)
                say("   " + result.Line);
        }

        // A single fight is being LOOKED at, not sampled: print the turns.
        if (snapshot.Fights == 1 && results[0].Rounds.Count > 0)
            foreach (var round in results[0].Rounds)
                say($"      turn {round.Turn,3}: hero {round.HeroHealth,4}  enemies {round.EnemyHealth,5}"
                    + $"  ({round.Plays} played)");

        Summary(results, say);
        return results.Any(r => r.Error is not null) ? 1 : 0;
    }

    private static void Summary(IReadOnlyList<Result> results, Action<string> say)
    {
        var finished = results.Where(r => r.Error is null && r.Ended).ToList();
        var won = finished.Where(r => r.Won).ToList();
        var broke = results.Count(r => r.Error is not null);
        var unfinished = results.Count(r => r.Error is null && !r.Ended);

        say($"   {results.Count} fight(s): won {won.Count}, lost {finished.Count - won.Count}"
            + $", unfinished {unfinished}, broke {broke}");
        if (won.Count > 0)
        {
            say($"   turns    median {Median(won.Select(r => r.Turns))}"
                + $"   min {won.Min(r => r.Turns)}   max {won.Max(r => r.Turns)}");
            say($"   hero HP  median {Median(won.Select(r => r.HeroHealth))}/{won[0].HeroMaxHealth}"
                + $"   worst {won.Min(r => r.HeroHealth)}   best {won.Max(r => r.HeroHealth)}"
                + $"   (lost median {Median(won.Select(r => r.HeroHealthLost))})");
        }
        if (finished.Count > won.Count)
            say($"   when the fighter died: median turn {Median(finished.Where(r => !r.Won).Select(r => r.Turns))}"
                + $", median {Median(finished.Where(r => !r.Won).Select(r => r.EnemyHealthLeft))} enemy HP still up");
    }

    private static int Median(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
    }

    private static int EnemyHealth(InteractiveCombat combat) =>
        combat.State.Combatants.Where(c => c.Id != combat.HeroId).Sum(c => c.Health.Current);

    // ── when it goes wrong ────────────────────────────────────────────────────────────────────────────────

    private static Result Broken(int seed, string error, IReadOnlyList<Round> rounds) =>
        new(seed, false, false, rounds.Count, 0, 0, 0, 0, 0, error, rounds);

    // The V-3a lesson, in the small: a fight that breaks says what was ON THE TABLE when it did, because an
    // error string alone can only be answered by running it again with a debugger.
    private static Result Broken(
        int seed, string error, IReadOnlyList<Round> rounds, int turn,
        int heroNow, int heroMax, int heroAtFirstBell, int enemiesAtFirstBell, int enemiesNow,
        RunPlayback play, InteractiveCombat? combat, Action<string>? progress)
    {
        if (progress is not null && combat is not null)
        {
            var hero = combat.State.GetCombatant(combat.HeroId);
            progress($"      ✗ seed {seed} broke on turn {turn + 1}: {error}");
            progress($"        hero {hero.Health.Current}/{hero.Health.Max} — {Chips(hero)}");
            foreach (var body in combat.State.Combatants.Where(c => c.Id != combat.HeroId))
                progress($"        [{body.DefinitionId.value}] {body.Health.Current}/{body.Health.Max}"
                    + $" — {Chips(body)}");
            progress($"        hand: {string.Join(", ", combat.Hand.Select(c => c.DefinitionId.value))}");
            foreach (var entry in combat.State.CombatLog.TakeLast(12))
                progress($"        [log] {entry.Type}: {entry.Message}");
        }
        return new Result(seed, false, false, turn, heroNow, heroMax,
            heroAtFirstBell - heroNow, enemiesAtFirstBell - enemiesNow, enemiesNow, error, rounds);
    }

    private static string Chips(CombatantState body) =>
        body.Statuses.Count == 0
            ? "no statuses"
            : string.Join("  ", body.Statuses.Select(s => $"{s.DefinitionId.value} ×{s.Stacks}"));

    private static string Loadout(SparringRing.Snapshot snapshot)
    {
        var parts = new List<string>
        {
            $"{snapshot.Health ?? snapshot.MaxHealth}/{snapshot.MaxHealth} HP",
            $"{snapshot.Energy} Energy",
            snapshot.Deck.Count == 0 ? "the character's own deck" : $"{snapshot.Deck.Count} cards",
        };
        if (snapshot.Relics.Count > 0)
            parts.Add($"{snapshot.Relics.Count} relic(s): {string.Join(", ", snapshot.Relics)}");
        if (snapshot.Statuses.Count > 0)
            parts.Add(string.Join(", ", snapshot.Statuses.Select(s => $"{s.Key} ×{s.Value}")));
        if (snapshot.EnemyStatuses.Count > 0)
            parts.Add("enemy opens with " + string.Join(", ", snapshot.EnemyStatuses.Select(s => $"{s.Key} ×{s.Value}")));
        return string.Join(" · ", parts);
    }
}
