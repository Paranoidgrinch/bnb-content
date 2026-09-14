using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// An auto-player that walks a WHOLE run — every act, through the real host path (RunPlayback + the interactive
// drivers), answering exactly the questions a player answers: which room, which door, which card, which enemy.
//
// It exists to find bugs that only a full walk can find: a room that parks forever, a door that asks a question
// nobody can answer, a boss that cannot be reached, a save that will not resume. It is deliberately dumb — a
// greedy, random player — because a smart one would avoid the corners we are looking for. The tester walks in
// with as much health as the caller gives it (`health`), so that dying does not cut the walk short before the
// last act; that makes it a COVERAGE instrument, not a balance one.
public static class RunWalker
{
    // One stop the walk made, as the report reads it back: which act, which room, what the room WAS (its role
    // tag) and what it held (the encounter/event/shop id).
    public sealed record Stop(int Act, string NodeId, string Role, string Content);

    public sealed record Report(
        int Seed,
        RunResult Result,
        string? Error,
        IReadOnlyList<Stop> Stops,
        IReadOnlyList<string> Notes,
        int Steps)
    {
        public bool Finished => Error is null && Result != RunResult.Ongoing;
        public int ActsWalked => Stops.Count == 0 ? 0 : Stops.Max(s => s.Act);
        public IEnumerable<Stop> InAct(int act) => Stops.Where(s => s.Act == act);
        public int Count(int act, string role) => InAct(act).Count(s => s.Role == role);
    }

    // Give the walking tester a body that survives the whole run, so the walk reaches the last act. Both the
    // blueprint's own Start and every character's are raised — RunSetup reads whichever the pick names.
    public static RunBlueprint WithHealth(RunBlueprint blueprint, int health)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        RunStart Raise(RunStart start) => start with { MaxHealth = health, StartingHealth = health };
        return blueprint with
        {
            Start = Raise(blueprint.Start),
            Characters = [.. blueprint.Characters.Select(c => c with { Start = Raise(c.Start) })],
        };
    }

    // What a walk COSTS, as the instrument that finds the replay baseline. Every answer re-executes the run
    // from that baseline up to the first unanswered prompt, so the price of one answer grows with the number
    // of answers behind it — and where the curve bends is where the baseline is too far back. This counts the
    // HOST's replay model, not the run: an answer is anything a player clicks.
    private sealed class Meter
    {
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

        public int Answers { get; private set; }
        public double Seconds => _clock.Elapsed.TotalSeconds;

        public void Answered() => Answers++;

        // "3.1s over 48 answers (65 ms/answer)" — the cost of everything since a mark was taken.
        public string Since(double seconds, int answers)
        {
            var dt = Seconds - seconds;
            var da = Answers - answers;
            return $"{dt,5:0.0}s over {da,4} answers ({(da == 0 ? 0 : dt * 1000 / da),6:0} ms/answer)";
        }
    }

    // `mapGenerator` is MapGenerators.RuleBased or .Strategic, and null means the run's own default, which is
    // the rule-based one. It is only passed at the START: a save carries the generator it was laid out with, so
    // the resume below is already walking the right maps without being told (plan §4b).
    public static Report Walk(
        RunBlueprint blueprint, int seed, int stepBudget = 30000, int saveEvery = 0,
        Action<string>? progress = null, string? mapGenerator = null)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        var rng = new Random(seed);
        var meter = new Meter();
        var stops = new List<Stop>();
        var notes = new List<string>();
        var steps = 0;

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed, interactive: true, mapGenerator: mapGenerator);
        using var _ = play;
        if (play.Error is { } startError)
            return new Report(seed, RunResult.Ongoing, startError, stops, notes, steps);

        var session = play.Session!;
        var interludes = 0;
        var lastNode = "";
        var roomSeconds = 0.0;
        var roomAnswers = 0;
        var clockedRooms = new HashSet<string>(StringComparer.Ordinal);

        // Every room the run STANDS in, in the order it is walked. Not every room is chosen: where a row offers
        // one way on, the engine walks it without asking, so counting only the answered forks misses most of
        // the act (and every boss, which never forks).
        void Note()
        {
            // Keyed by ACT and room: every act's map numbers its rooms from r0c0, so a bare id would count
            // the second act's opening as the first act's and drop it from the report.
            if (session.Run.CurrentNodeId is not { } id
                || !clockedRooms.Add($"{session.Run.ActNumber}/{id.Value}"))
                return;
            var node = session.Run.Map.Nodes.FirstOrDefault(n => n.Id.Value == id.Value);
            if (node is null)
                return;
            var stop = Describe(session.Run, node);
            stops.Add(stop);
            // The room we are leaving is the one whose price is now known — a room is only as expensive as
            // the answers it took, and that number is not in until the next room begins.
            if (lastNode.Length > 0)
                progress?.Invoke($"      ^ {lastNode} cost {meter.Since(roomSeconds, roomAnswers)}");
            roomSeconds = meter.Seconds;
            roomAnswers = meter.Answers;
            lastNode = stop.NodeId;
            progress?.Invoke(
                $"    [{meter.Seconds,7:0.0}s, {meter.Answers,5} answers] act {stop.Act} "
                + $"{stop.NodeId} {stop.Role} — {stop.Content}");
        }

        while (steps++ < stepBudget)
        {
            if (play.Error is { } hostError)
            {
                Inventory(play, session, progress);
                return new Report(seed, session.Run.Result, hostError, stops, notes, steps);
            }
            if (session.Error is { } runError)
            {
                Inventory(play, session, progress);
                return new Report(seed, session.Run.Result, runError, stops, notes, steps);
            }
            if (session.IsComplete)
                break;
            Note();

            if (session.IsAwaitingNodeChoice)
            {
                var pick = session.PendingNodeChoices[rng.Next(session.PendingNodeChoices.Count)];
                session.PickNode(pick.Id.Value);
                meter.Answered();
            }
            else if (session.IsAwaitingChoice)
            {
                var choices = session.PendingChoices;
                if (choices.Count == 0)
                {
                    notes.Add($"{lastNode}: an event parked with no answerable choice");
                    break;
                }
                session.Pick(choices[rng.Next(choices.Count)].Id);
                meter.Answered();
            }
            else if (session.IsAwaitingEntities)
            {
                var request = session.PendingEntities!;
                var offered = request.Displays.Count;
                var take = Math.Min(request.Count, offered);
                var picks = Enumerable.Range(0, offered).OrderBy(_ => rng.Next()).Take(take).ToList();
                session.PickEntities(picks);
                meter.Answered();
            }
            else if (play.CombatDriver?.Current is not null)
            {
                if (!Fight(play, session, rng, notes, lastNode, meter, progress))
                {
                    Inventory(play, session, progress);
                    break;
                }
            }
            else if (session.IsAwaitingInterlude)
            {
                interludes++;
                if (saveEvery > 0 && interludes % saveEvery == 0 && !Reload(ref play, ref session, blueprint, notes))
                    break;
                session.Continue();
                meter.Answered();
            }
            else
            {
                notes.Add($"{lastNode}: the run parked in no state at all (nothing to answer, not complete)");
                break;
            }
        }

        if (steps >= stepBudget)
            notes.Add($"the walk ran out of steps after {stepBudget} — something is looping");
        return new Report(seed, session.Run.Result, session.Error ?? play.Error, stops, notes, steps);
    }

    // Save the live run at its interlude and resume it from that save — the exact round trip the player makes
    // by quitting and continuing. A walk that survives this proves the save covers what the walk has done.
    private static bool Reload(
        ref RunPlayback play, ref InteractiveRunSession session, RunBlueprint blueprint, List<string> notes)
    {
        var json = play.SaveJson();
        if (json is null)
        {
            notes.Add($"the run would not save at an interlude: {play.Error ?? session.Error}");
            return false;
        }
        var resumed = new RunPlayback(() => { });
        resumed.Resume(blueprint, RunSaveJson.FromJson(json), interactive: true);
        if (resumed.Error is { } error)
        {
            notes.Add($"the saved run would not resume: {error}");
            return false;
        }
        play.Dispose();
        play = resumed;
        session = resumed.Session!;
        return true;
    }

    // One fight, played greedily: whatever is affordable at the first living enemy, then end the turn.
    //
    // A play the engine REFUSES (an unplayable Junk card, a validator saying no) is still recorded into the
    // replay script — so a walker that keeps retrying one refused card makes every later replay longer and the
    // walk grinds to a halt. Refusals are read back off the fight's own step report and that card is not
    // offered again this turn. Returns false when the fight is stuck (then the note says so).
    private static bool Fight(
        RunPlayback play, InteractiveRunSession session, Random rng, List<string> notes, string node,
        Meter meter, Action<string>? progress)
    {
        var driver = play.CombatDriver!;
        var refused = new HashSet<CardInstanceId>();
        // Cards that CHANGED NOTHING when they were played this turn, by definition rather than by copy. A
        // card is allowed to put a fresh copy of itself back in your hand — Act III's Make Amends does it on
        // purpose, so that a payment which could not go through can be tried again — and a greedy player will
        // then play it for ever: the copy is new, so refusing the instance does not help, and nothing about
        // the table moves. A human ends the turn; the walker is told to by this.
        var barren = new HashSet<string>(StringComparer.Ordinal);
        for (var turn = 0; turn < 100; turn++)
        {
            // A fight is where the answers pile up fastest — one per card played, one per turn ended, one per
            // question a card asks — so the per-TURN price is what says whether a fight is the thing to cap.
            var turnSeconds = meter.Seconds;
            var turnAnswers = meter.Answers;
            var playsThisTurn = 0;
            var lastPlayed = (string?)null;
            var tableBeforeThePlay = "";
            refused.Clear();
            barren.Clear();
            while (true)
            {
                if (driver.Current is null)
                    return true;
                if (Greedy.Answer(driver, rng))
                {
                    meter.Answered();
                    continue;
                }
                if (session.Error is not null || play.Error is not null)
                    return true; // reported by the caller
                var combat = driver.Current;

                // Only HERE is the previous play finished. A card that asks a question parks halfway through
                // its own resolution, so a reading taken the moment PlayCard returns straddles an open
                // question and always differs; this is the first point at which nothing is pending.
                if (lastPlayed is { } finished)
                {
                    if (Greedy.TableState(combat) == tableBeforeThePlay)
                        barren.Add(finished);
                    lastPlayed = null;
                }

                if (Greedy.Choose(play, combat, rng, refused, barren) is not { } card)
                    break;
                var enemy = Greedy.Enemy(combat);
                var needsTarget = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var needs) && needs;
                var stepsBefore = combat.Steps.Count;
                tableBeforeThePlay = Greedy.TableState(combat);
                lastPlayed = card.DefinitionId.value;
                driver.PlayCard(card.Id, needsTarget ? enemy : null);
                meter.Answered();
                // A turn that never runs out of affordable cards is a finding, not a slow fight — and it is
                // invisible from outside, because a turn only reports itself when it ends. Say what is being
                // played while it happens.
                // The backstop behind the barren rule: two cards that undo each other would still cycle.
                // Nothing in this game plays fifty cards in a turn, so hitting this is a finding.
                if (++playsThisTurn >= Greedy.PlaysInATurnNobodyMakes)
                {
                    notes.Add($"{node}: a turn played {playsThisTurn} cards without ending — "
                        + $"last '{card.DefinitionId.value}'");
                    return false;
                }
                if (session.Error is not null || play.Error is not null)
                    return true;
                if (Greedy.Refused(driver.Current, stepsBefore))
                    refused.Add(card.Id);
            }
            if (driver.Current is null)
                return true;
            if (Greedy.Answer(driver, rng))
            {
                meter.Answered();
                continue;
            }
            driver.EndTurn();
            meter.Answered();
            // Only turns that COST something are worth a line. A walk prints a few hundred turns and almost
            // all of them are four answers long; the ones that matter are the ones that are not.
            //
            // The line says who is still standing, because that is the whole question about a long fight and
            // the walk is the only place it can be asked: a fight at turn 80 whose enemy is losing 6 HP a turn
            // is slow, and one whose enemy has not moved since turn 12 is stuck, and a timing alone cannot
            // tell them apart. The Warden of Sealed Volumes was carried for a week as "does not end" for want
            // of exactly this.
            if (meter.Seconds - turnSeconds > 1.0 || meter.Answers - turnAnswers > 15)
                progress?.Invoke($"        turn {turn + 1,3}: {meter.Since(turnSeconds, turnAnswers)}"
                    + $" — {Greedy.Standing(driver.Current)}");
            if (session.Error is not null || play.Error is not null)
                return true;
        }
        notes.Add($"{node}: a fight did not end in 100 turns");
        return false;
    }

    // WHAT THE RUN WAS CARRYING when it broke. A crash inside a fight is almost never about the fight alone —
    // a boss's own rules are the same in every walk — so the first question is always what THIS walk had
    // picked up on the way to it, and the second is where the table stood. Printed once, at the moment the
    // walk gives up, because a walk that ends in an error string alone can only be answered by walking again.
    private static void Inventory(
        RunPlayback play, InteractiveRunSession session, Action<string>? progress)
    {
        if (progress is null)
            return;

        var run = session.Run;
        progress($"      --- what the run was carrying at act {run.ActNumber} "
            + $"{run.CurrentNodeId?.Value ?? "(nowhere)"} ---");
        progress($"      relics ({run.Relics.Count}): "
            + Tally(run.Relics.Select(r => r.Enabled ? r.Id.Value : $"{r.Id.Value}(off)")));
        progress($"      deck ({run.Deck.Count}): "
            + Tally(run.Deck.Select(c => c.UpgradeLevel > 0
                ? $"{c.DefinitionId.value}+{c.UpgradeLevel}" : c.DefinitionId.value)));

        if (play.CombatDriver?.Current is not { } combat)
            return;

        foreach (var combatant in combat.State.Combatants)
        {
            var who = combatant.Id == combat.HeroId ? "hero" : "enemy";
            var statuses = string.Join(", ", combatant.Statuses.Select(s => $"{s.DefinitionId.value} x{s.Stacks}"));
            var counters = string.Join(", ", combatant.Counters.Select(c => $"{c.Key.value}={c.Value}"));
            progress($"      {who} {combatant.Id.value} {combatant.Health.Current}/{combatant.Health.Max}"
                + $" — statuses [{statuses}] counters [{counters}]");
        }
        var zones = combat.State.GetCardZones(combat.HeroId);
        progress($"      hand: {Tally(zones.GetCardsInZone(CardZone.Hand).Select(c => c.DefinitionId.value))}");
        foreach (var step in combat.Steps.TakeLast(LastStepsShown))
            progress($"      step {step.Index}: r{step.Round}t{step.Turn} {step.Step}"
                + (step.HasProblems ? $" — REFUSED: {string.Join("; ", step.Problems)}" : ""));

        // The fight's WHOLE log, written out beside the walk. A loop says nothing in ten lines — what tells
        // the two apart (a program that repeats for ever and one that is merely long) is where the repetition
        // starts, and that is hundreds of lines back.
        var path = Path.Combine(Path.GetTempPath(), $"walk-{run.ActNumber}-{run.CurrentNodeId?.Value}-fight.log");
        File.WriteAllLines(path, combat.State.CombatLog.Select(e => $"r{e.Round}t{e.Turn} {e.Type}: {e.Message}"));
        progress($"      the whole fight log ({combat.State.CombatLog.Count} lines): {path}");
    }

    private const int LastStepsShown = 8;

    // "a, b x3, c" — a list of ids that says how many of each without printing forty lines of deck.
    private static string Tally(IEnumerable<string> ids) =>
        string.Join(", ", ids.GroupBy(id => id, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.Count() == 1 ? g.Key : $"{g.Key} x{g.Count()}"));

    private static Stop Describe(RunState run, Node node)
    {
        var role = node.Tags.Count > 0 ? node.Tags[0] : node.Type.Value;
        var content = node.Payload switch
        {
            EncounterRef fight => fight.Id.Value,
            EventRef door => door.Id.Value,
            ShopRef shop => shop.Id.Value,
            _ => node.Payload.GetType().Name,
        };
        return new Stop(run.ActNumber, node.Id.Value, role, content);
    }
}
