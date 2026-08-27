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

    public static Report Walk(
        RunBlueprint blueprint, int seed, int stepBudget = 30000, int saveEvery = 0,
        Action<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        var rng = new Random(seed);
        var clock = System.Diagnostics.Stopwatch.StartNew();
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
            lastNode = stop.NodeId;
            progress?.Invoke(
                $"    [{clock.Elapsed.TotalSeconds,7:0.0}s, {steps,5} answers] act {stop.Act} "
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
            }
            else if (session.IsAwaitingEntities)
            {
                var request = session.PendingEntities!;
                var offered = request.Displays.Count;
                var take = Math.Min(request.Count, offered);
                var picks = Enumerable.Range(0, offered).OrderBy(_ => rng.Next()).Take(take).ToList();
                session.PickEntities(picks);
            }
            else if (play.CombatDriver?.Current is not null)
            {
                if (!Fight(play, session, rng, notes, lastNode))
                    break;
            }
            else if (session.IsAwaitingInterlude)
            {
                interludes++;
                if (saveEvery > 0 && interludes % saveEvery == 0 && !Reload(ref play, ref session, blueprint, notes))
                    break;
                session.Continue();
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
        RunPlayback play, InteractiveRunSession session, Random rng, List<string> notes, string node)
    {
        var driver = play.CombatDriver!;
        var refused = new HashSet<CardInstanceId>();
        for (var turn = 0; turn < 100; turn++)
        {
            refused.Clear();
            while (true)
            {
                if (driver.Current is null)
                    return true;
                if (Answer(driver, rng))
                    continue;
                if (session.Error is not null || play.Error is not null)
                    return true; // reported by the caller
                var combat = driver.Current;
                var hero = combat.State.GetCombatant(combat.HeroId);
                var energy = hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool)
                    ? pool.Current : 0;
                var candidates = combat.Hand
                    .Where(c => !refused.Contains(c.Id) && Affordable(play, combat, c, energy))
                    .ToList();
                if (candidates.Count == 0)
                    break;
                var card = candidates[rng.Next(candidates.Count)];
                var enemy = combat.State.Combatants
                    .FirstOrDefault(c => c.Id != combat.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive);
                var needsTarget = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var needs) && needs;
                var stepsBefore = combat.Steps.Count;
                driver.PlayCard(card.Id, needsTarget ? enemy?.Id : null);
                if (session.Error is not null || play.Error is not null)
                    return true;
                if (Refused(driver.Current, stepsBefore))
                    refused.Add(card.Id);
            }
            if (driver.Current is null)
                return true;
            if (Answer(driver, rng))
                continue;
            driver.EndTurn();
            if (session.Error is not null || play.Error is not null)
                return true;
        }
        notes.Add($"{node}: a fight did not end in 100 turns");
        return false;
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
