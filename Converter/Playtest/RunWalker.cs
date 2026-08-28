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

    public static Report Walk(
        RunBlueprint blueprint, int seed, int stepBudget = 30000, int saveEvery = 0,
        Action<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        var rng = new Random(seed);
        var meter = new Meter();
        var stops = new List<Stop>();
        var notes = new List<string>();
        var steps = 0;

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed, interactive: true);
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
                return new Report(seed, session.Run.Result, hostError, stops, notes, steps);
            if (session.Error is { } runError)
                return new Report(seed, session.Run.Result, runError, stops, notes, steps);
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
                    break;
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
                if (Answer(driver, rng))
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
                    if (TableState(combat) == tableBeforeThePlay)
                        barren.Add(finished);
                    lastPlayed = null;
                }

                var hero = combat.State.GetCombatant(combat.HeroId);
                var energy = hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool)
                    ? pool.Current : 0;
                var candidates = combat.Hand
                    .Where(c => !refused.Contains(c.Id) && !barren.Contains(c.DefinitionId.value)
                        && Affordable(play, combat, c, energy))
                    .ToList();
                if (candidates.Count == 0)
                    break;
                var card = candidates[rng.Next(candidates.Count)];
                var enemy = combat.State.Combatants
                    .FirstOrDefault(c => c.Id != combat.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive);
                var needsTarget = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var needs) && needs;
                var stepsBefore = combat.Steps.Count;
                tableBeforeThePlay = TableState(combat);
                lastPlayed = card.DefinitionId.value;
                driver.PlayCard(card.Id, needsTarget ? enemy?.Id : null);
                meter.Answered();
                // A turn that never runs out of affordable cards is a finding, not a slow fight — and it is
                // invisible from outside, because a turn only reports itself when it ends. Say what is being
                // played while it happens.
                // The backstop behind the barren rule: two cards that undo each other would still cycle.
                // Nothing in this game plays fifty cards in a turn, so hitting this is a finding.
                if (++playsThisTurn >= PlaysInATurnNobodyMakes)
                {
                    notes.Add($"{node}: a turn played {playsThisTurn} cards without ending — "
                        + $"last '{card.DefinitionId.value}'");
                    return false;
                }
                if (session.Error is not null || play.Error is not null)
                    return true;
                if (Refused(driver.Current, stepsBefore))
                    refused.Add(card.Id);
            }
            if (driver.Current is null)
                return true;
            if (Answer(driver, rng))
            {
                meter.Answered();
                continue;
            }
            driver.EndTurn();
            meter.Answered();
            // Only turns that COST something are worth a line. A walk prints a few hundred turns and almost
            // all of them are four answers long; the ones that matter are the ones that are not.
            if (meter.Seconds - turnSeconds > 1.0 || meter.Answers - turnAnswers > 15)
                progress?.Invoke($"        turn {turn + 1,3}: {meter.Since(turnSeconds, turnAnswers)}");
            if (session.Error is not null || play.Error is not null)
                return true;
        }
        notes.Add($"{node}: a fight did not end in 100 turns");
        return false;
    }

    private const int PlaysInATurnNobodyMakes = 50;

    // Everything about the table a play could visibly move. Two plays with the same reading either side of
    // them did nothing — which is the only way to tell a card that regenerates itself apart from one that
    // achieves something.
    //
    // The EXHAUST PILE is deliberately not in it. A card that burns itself and puts a fresh copy back in hand
    // grows that pile on every play, so counting it would make every such card look busy for ever — which is
    // exactly the loop this reading exists to find. Statuses are counted by their STACKS as well as their
    // number, because paying a debt down usually moves the stack and not the count.
    private static string TableState(InteractiveCombat combat)
    {
        var hero = combat.State.GetCombatant(combat.HeroId);
        var energy = hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool) ? pool.Current : 0;
        var enemies = combat.State.Combatants.Where(c => c.Id != combat.HeroId).ToList();
        var zones = combat.State.GetCardZones(combat.HeroId);
        int Count(CardZone zone) => zones.GetCardsInZone(zone).Count;
        static int Stacks(IEnumerable<StatusInstance> statuses) => statuses.Sum(status => status.Stacks);
        return $"{energy}/{hero.Health.Current}/{hero.Statuses.Count}/{Stacks(hero.Statuses)}/"
            + $"{Count(CardZone.Hand)}/{Count(CardZone.DiscardPile)}/{Count(CardZone.DrawPile)}/"
            + $"{enemies.Sum(e => e.Health.Current)}/{enemies.Sum(e => e.Statuses.Count)}/"
            + $"{enemies.Sum(e => Stacks(e.Statuses))}";
    }

    // Did the play the walker just made go through? The fight records every attempt as a step, and a refused
    // one carries the reason; nothing new at all means the driver dropped it (a prompt opened, say).
    private static bool Refused(InteractiveCombat? combat, int stepsBefore)
    {
        if (combat is null)
            return false;
        var steps = combat.Steps;
        return steps.Count <= stepsBefore || steps.Skip(stepsBefore).Any(step => step.HasProblems);
    }

    // A card (or an enemy) asked a question mid-resolution: answer it so the fight can go on. True = answered.
    private static bool Answer(InteractiveCombatDriver driver, Random rng)
    {
        if (driver.PendingCardChoice is { } cards)
        {
            var take = Math.Min(driver.PendingCardChoiceCount, cards.Count);
            driver.SupplyCardChoice([.. cards.OrderBy(_ => rng.Next()).Take(take).Select(c => c.Id)]);
            return true;
        }
        if (driver.PendingOptionChoice is { } options)
        {
            var take = Math.Min(driver.PendingOptionChoiceCount, options.Count);
            driver.SupplyOptionChoice([.. Enumerable.Range(0, options.Count).OrderBy(_ => rng.Next()).Take(take)]);
            return true;
        }
        return false;
    }

    // Whether the hero can pay for the card right now — its composed costs (shreds included), Energy only; a
    // card wanting a resource the hero has none of is simply not playable and the greedy player skips it.
    private static bool Affordable(RunPlayback play, InteractiveCombat combat, CardInstance card, int energy)
    {
        var hero = combat.State.GetCombatant(combat.HeroId);
        var costs = play.ComposedCostsFor(card.DefinitionId.value)
            ?? play.CardFullCosts.GetValueOrDefault(card.DefinitionId.value);
        if (costs is null)
            return play.CardCosts.GetValueOrDefault(card.DefinitionId.value) <= energy;
        foreach (var cost in costs)
        {
            var have = hero.Resources.TryGetValue(cost.ResourceId, out var pool) ? pool.Current : 0;
            if (have < cost.Amount)
                return false;
        }
        return true;
    }

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
