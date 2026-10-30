using System.Text.Json;
using RogueDeck.Bot;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// ── THE MECHANIC AUDIT: DOES EVERY RULE OF EVERY FIGHT ACTUALLY DO SOMETHING? ────────────────────────────────
// (user, 2026-10-30) Before the act curve is tuned, every fight mechanic is checked: does it fire, does it do what
// its text says, and where does the health a fight costs come from. The act bench's planner plays each encounter
// against the snapshot decks; its chosen line is then REPLAYED on a traced fork (the planner searches on quiet forks,
// which write no receipts), and the receipts are counted:
//
//   triggers    every trigger evaluated: how often it fired, how often it enqueued something (acted), how often a
//               filter said no
//   hits        every point of health the hero lost, by what caused it: an enemy action, a status's own blow, a card
//   modifiers   every damage modifier step, by modifier id, summed for blows on the hero and on the enemies
//   statuses    every status application, by side, status and outcome (stacks requested)
//   enemy       block and healing the enemies gained, by the step that gave it
//
// One JSON line per fight into ~/Desktop/bnb-balance/audit/mechanics-<stamp>.jsonl, read by a script afterwards.
// ⚠ The replay answers prompts with the headless default, exactly as the planner's forks did.
public static class MechanicAudit
{
    public sealed record Options(
        string SnapshotsFile, IReadOnlyList<int> Acts, int PerGroup = 2, int Horizon = 5, int Beam = 6, int PerTurn = 100,
        int Jobs = 0, IReadOnlyList<string>? Only = null, string? Out = null);

    public static int Run(RunBlueprint game, Options options, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        // The first PerGroup snapshots of each character, act and point: the same decks the act bench reads.
        var snapshots = JsonSerializer.Deserialize<List<ActBench.Snapshot>>(File.ReadAllText(options.SnapshotsFile))!
            .Where(s => options.Acts.Contains(s.Act))
            .GroupBy(s => (s.Character, s.Act, s.Point))
            .SelectMany(g => g.Take(options.PerGroup))
            .ToList();

        var jobs = new List<(ActBench.Snapshot Snap, string Role, EncounterDefinition Encounter)>();
        foreach (var snap in snapshots)
            foreach (var (role, encounter) in ActBench.Pool(game, snap.Act, snap.Point))
                if (options.Only is not { Count: > 0 } only || only.Contains(encounter.Id.Value))
                    jobs.Add((snap, role, encounter));

        var cores = options.Jobs > 0 ? options.Jobs : Environment.ProcessorCount;
        var outFile = options.Out ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Desktop", "bnb-balance", "audit", $"mechanics-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
        say($"mechanic audit · acts {string.Join(",", options.Acts)} · {snapshots.Count} snapshots · {jobs.Count} fights "
            + $"on {cores} cores → {outFile}");

        var gate = new object();
        var done = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Parallel.ForEach(jobs.OrderBy(j => j.Role switch { "boss" => 0, "elite" => 1, "multi" => 2, _ => 3 }),
            new ParallelOptions { MaxDegreeOfParallelism = cores }, job =>
            {
                string line;
                try
                {
                    line = JsonSerializer.Serialize(Fight(game, job.Snap, job.Role, job.Encounter, options));
                }
                catch (Exception e)
                {
                    line = JsonSerializer.Serialize(new
                    {
                        run = job.Snap.Run,
                        act = job.Snap.Act,
                        role = job.Role,
                        encounter = job.Encounter.Id.Value,
                        error = e.GetType().Name + ": " + e.Message
                    });
                }
                lock (gate)
                    File.AppendAllText(outFile, line + "\n");
                if (Interlocked.Increment(ref done) % 25 == 0)
                    Console.Error.WriteLine($"  {done}/{jobs.Count} fights, {clock.Elapsed.TotalMinutes:0.0} min");
            });
        say($"done: {done} fights in {clock.Elapsed.TotalMinutes:0.0} min");
        return 0;
    }

    private static object Fight(RunBlueprint game, ActBench.Snapshot snap, string role, EncounterDefinition encounter,
        Options options)
    {
        var ring = SparringRing.OneFight(game, encounter, snap.Deck, maxHealth: snap.MaxHealth, relics: snap.Relics,
            character: snap.Character);
        using var play = new RunPlayback(() => { });
        play.Start(ring, 1, interactive: true);
        while (play.Session is { IsAwaitingInterlude: true } session)
            session.Continue();
        if (play.CombatDriver?.Current is not { } start)
            throw new InvalidOperationException("no fight");

        var planned = new FightPlanner(options.Horizon, options.Beam, options.PerTurn).Play(start);
        var traced = start.Fork();
        // ⚠ The steps do not hold every receipt: what happens between two enemies' turns (one's TurnEnded, the next
        // one's TurnStarted) lands after one step is recorded and before the next begins. So the whole stream is
        // tapped, and each receipt is given the step that recorded it, or "between-turns".
        var tap = new Tap(traced.State.TraceListener);
        traced.State.TraceListener = tap;
        var names = traced.State.Combatants.ToDictionary(c => c.Id, c => c.DisplayNameKey);
        var opening = traced.State.Combatants.Select(c => new
        {
            who = c.DisplayNameKey,
            hero = c.Id == traced.HeroId,
            statuses = c.Statuses.Select(s => $"{s.DefinitionId.value}:{s.Stacks}").ToList(),
        }).ToList();

        var replayProblems = new List<string>();
        var turnsPlayed = 0;
        // Counters write no receipt; their highest value at a turn boundary is read off the state instead.
        var counterMax = new Dictionary<string, int>(StringComparer.Ordinal);
        void ReadCounters()
        {
            foreach (var c in traced.State.Combatants)
                foreach (var (id, value) in c.Counters)
                {
                    var key = $"{(c.Id == traced.HeroId ? "hero" : "enemy")}:{id.value}";
                    if (value != 0 && (!counterMax.TryGetValue(key, out var was) || value > was))
                        counterMax[key] = value;
                }
        }
        foreach (var turn in planned.Lines)
        {
            while (!traced.IsOver && !traced.IsHeroTurn && turnsPlayed++ < 60)
                traced.EndTurn();
            if (traced.IsOver)
                break;
            if (turn != "(nothing)")
                foreach (var move in turn.Split(" + "))
                    if (Apply(traced, move) is { } problem)
                        replayProblems.Add(problem);
            ReadCounters();
            if (!traced.IsOver)
                traced.EndTurn();
            ReadCounters();
            turnsPlayed++;
        }

        var triggers = new Dictionary<string, int[]>(StringComparer.Ordinal); // fired, acted, rejected
        var hits = new Dictionary<string, int[]>(StringComparer.Ordinal);     // hp lost, blocked, count
        var modifiers = new Dictionary<string, int[]>(StringComparer.Ordinal); // on hero, on enemies, count
        var statuses = new Dictionary<string, int[]>(StringComparer.Ordinal); // stacks requested, count
        var enemy = new Dictionary<string, int[]>(StringComparer.Ordinal);    // block, heal, health set lost
        var actions = new Dictionary<string, int>(StringComparer.Ordinal);
        // What each trigger (or root step) actually did. A request's receipts come just before its EffectResolved, so they
        // are buffered and handed to the request's owner when it resolves. ⚠ The chain id is the EVENT's chain, shared by
        // every trigger that answered it: the owner is the first request still waiting with that chain and type (the
        // queue resolves in order).
        var effects = new Dictionary<string, int[]>(StringComparer.Ordinal); // amount, count
        var waiting = new List<(long Chain, string Type, string Owner)>();
        var pending = new List<(string What, int Amount)>();
        string Side(CombatantId id) => id == traced.HeroId ? "hero" : "enemy";
        int[] Of(Dictionary<string, int[]> d, string key, int n) => d.TryGetValue(key, out var v) ? v : d[key] = new int[n];

        var dump = Environment.GetEnvironmentVariable("AUDIT_DUMP") is { Length: > 0 } dumpPath
            ? new StreamWriter(dumpPath, append: true) : null;
        var stepOf = new Dictionary<CombatTraceEvent, string>(ReferenceEqualityComparer.Instance);
        foreach (var step in traced.Steps)
        {
            var key = step.Step switch
            {
                EnemyActs acts => $"action:{acts.ActionId}",
                HeroEndsTurn => "hero-ends-turn",
                AdvanceToNextRound => "round-start",
                _ => "hero:" + step.Step.GetType().Name,
            };
            if (step.Step is EnemyActs acted)
                actions[acted.ActionId] = actions.GetValueOrDefault(acted.ActionId) + 1;
            foreach (var evt in step.Trace)
                stepOf[evt] = key;
        }
        foreach (var evt in tap.Events)
        {
            var stepKey = stepOf.GetValueOrDefault(evt, "between-turns");
            dump?.WriteLine($"   [{stepKey}] " + evt.ToString()[..Math.Min(260, evt.ToString().Length)]);
            switch (evt)
            {
                case TriggerEvaluatedTraceEvent t:
                    var tv = Of(triggers, t.TriggerId, 3);
                    if (t.Outcome == TriggerEvaluationOutcome.Fired)
                        tv[0]++;
                    else if (t.Outcome == TriggerEvaluationOutcome.SkippedFilterRejected)
                        tv[2]++;
                    break;
                case EffectEnqueuedTraceEvent q:
                    if (q.TriggerId is { } tid)
                        Of(triggers, tid, 3)[1]++;
                    waiting.Add((q.ChainId, q.RequestType, q.TriggerId is { } owner ? $"trigger:{owner}" : stepKey));
                    break;
                case EffectResolvedTraceEvent r:
                    var at = waiting.FindIndex(w => w.Chain == r.ChainId && w.Type == r.RequestType);
                    var by = at < 0 ? stepKey : waiting[at].Owner;
                    if (at >= 0)
                        waiting.RemoveAt(at);
                    foreach (var (what, amount) in pending)
                    {
                        var ev = Of(effects, $"{by} | {what}", 2);
                        ev[0] += amount;
                        ev[1]++;
                    }
                    pending.Clear();
                    break;
                case DamageResolvedTraceEvent d:
                    pending.Add(($"damage>{Side(d.TargetCombatantId)}", d.HealthLost + d.BlockedAmount));
                    var onHero = d.TargetCombatantId == traced.HeroId;
                    foreach (var m in d.ModifierSteps)
                    {
                        var mv = Of(modifiers, $"{m.Stage}:{m.ModifierId}", 3);
                        mv[onHero ? 0 : 1] += m.After - m.Before;
                        mv[2]++;
                    }
                    if (onHero && (d.HealthLost > 0 || d.BlockedAmount > 0))
                    {
                        var cause = d.SourceStatusId is { } sid ? $"status:{sid.value}"
                            : d.SourceCardId is { } cid && !stepKey.StartsWith("action:", StringComparison.Ordinal) ? $"card:{cid.value}"
                            : stepKey;
                        var who = d.SourceCombatantId is { } src && names.TryGetValue(src, out var n) ? n : "-";
                        var hv = Of(hits, $"{who} | {cause}", 3);
                        hv[0] += d.HealthLost;
                        hv[1] += d.BlockedAmount;
                        hv[2]++;
                    }
                    break;
                case StatusApplicationResolvedTraceEvent s:
                    pending.Add(($"status>{Side(s.TargetCombatantId)}:{s.StatusDefinitionId.value}:{s.Outcome}", s.RequestedStacks));
                    var side = s.TargetCombatantId == traced.HeroId ? "hero" : "enemy";
                    var sv = Of(statuses, $"{side} | {s.StatusDefinitionId.value} | {s.Outcome}", 2);
                    sv[0] += s.RequestedStacks;
                    sv[1]++;
                    break;
                case HealResolvedTraceEvent h0 when h0.TargetCombatantId == traced.HeroId:
                    pending.Add(("heal>hero", h0.HealedAmount));
                    break;
                case BlockGainResolvedTraceEvent b0 when b0.TargetCombatantId == traced.HeroId:
                    pending.Add(("block>hero", b0.BlockAfter - b0.BlockBefore));
                    break;
                case ResourceChangeResolvedTraceEvent rc:
                    pending.Add(($"resource>{Side(rc.CombatantId)}:{rc.ResourceId.value}", rc.AppliedDelta));
                    break;
                case MaxHealthChangeResolvedTraceEvent mh:
                    pending.Add(($"maxhealth>{Side(mh.TargetCombatantId)}", 1));
                    break;
                case BlockGainResolvedTraceEvent b when b.TargetCombatantId != traced.HeroId:
                    pending.Add(("block>enemy", b.BlockAfter - b.BlockBefore));
                    Of(enemy, stepKey, 3)[0] += b.BlockAfter - b.BlockBefore;
                    break;
                case HealResolvedTraceEvent h when h.TargetCombatantId != traced.HeroId:
                    pending.Add(("heal>enemy", h.HealedAmount));
                    Of(enemy, stepKey, 3)[1] += h.HealedAmount;
                    break;
                case HealthSetResolvedTraceEvent hs:
                    pending.Add(($"healthset>{Side(hs.TargetCombatantId)}", hs.PreviousValue - hs.NewValue));
                    if (hs.TargetCombatantId == traced.HeroId)
                        break;
                    Of(enemy, stepKey, 3)[2] += hs.PreviousValue - hs.NewValue;
                    break;
            }
        }

        dump?.Dispose();
        var hero = traced.State.GetCombatant(traced.HeroId);
        // Every status anybody carried at some point: the opening ones and every one applied. A trigger of such a
        // status that never fired is what the audit is looking for, so only those and the triggers that did are kept.
        var present = opening.SelectMany(o => o.statuses).Select(s => s[..s.LastIndexOf(':')])
            .Concat(statuses.Keys.Select(k => k.Split(" | ")[1]))
            .ToHashSet(StringComparer.Ordinal);
        var kept = triggers.Where(t => t.Value[0] > 0 || t.Value[1] > 0
                || present.Contains(t.Key.LastIndexOf("_trigger", StringComparison.Ordinal) is var at and > 0 ? t.Key[..at] : t.Key))
            .ToDictionary(t => t.Key, t => t.Value, StringComparer.Ordinal);
        return new
        {
            run = snap.Run,
            character = snap.Character,
            act = snap.Act,
            role,
            encounter = encounter.Id.Value,
            won = planned.Won,
            lost = planned.HealthLost,
            replayLost = planned.HeroHealthAtStart - (traced.Result == CombatResult.Defeat ? 0 : traced.HeroHealth),
            replayResult = traced.Result.ToString(),
            turns = planned.Turns,
            replayProblems,
            opening,
            heroEnd = hero.Statuses.Select(s => $"{s.DefinitionId.value}:{s.Stacks}").ToList(),
            actions,
            present = present.Order(StringComparer.Ordinal).ToList(),
            triggers = kept,
            hits,
            modifiers,
            statuses,
            enemy,
            effects,
            counterMax,
        };
    }

    // One planner move on the traced fork: "card", "card->enemy", "!action", "!action[card]->enemy".
    private static string? Apply(InteractiveCombat combat, string move)
    {
        if (combat.IsOver || !combat.IsHeroTurn)
            return $"{move}: not the hero's turn";
        var arrow = move.LastIndexOf("->", StringComparison.Ordinal);
        var body = arrow < 0 ? move : move[..arrow];
        CombatantId? target = arrow < 0 ? null : new CombatantId(move[(arrow + 2)..]);
        if (body.StartsWith('!'))
        {
            var bracket = body.IndexOf('[', StringComparison.Ordinal);
            var action = new CardDefinitionId(bracket < 0 ? body[1..] : body[1..bracket]);
            var card = bracket < 0 ? null : body[(bracket + 1)..^1];
            combat.State.SetCardChooser(new Pinned(card));
            combat.UseAction(action, target);
            combat.State.SetCardChooser(null);
            return null;
        }
        var instance = combat.Hand.FirstOrDefault(c => c.DefinitionId.value == body);
        if (instance is null)
            return $"{move}: not in hand";
        combat.PlayCard(instance.Id, target);
        return null;
    }

    private sealed class Tap(ICombatTraceListener? inner) : ICombatTraceListener
    {
        public List<CombatTraceEvent> Events { get; } = [];

        public void OnTrace(CombatTraceEvent evt)
        {
            Events.Add(evt);
            inner?.OnTrace(evt);
        }
    }

    private sealed class Pinned(string? definition) : ICombatCardChooser
    {
        public IReadOnlyList<CardInstanceId> ChooseCards(IReadOnlyList<CardInstance> candidates, int count, string purpose)
        {
            if (candidates.Count == 0)
                return [];
            var first = candidates.FirstOrDefault(c => c.DefinitionId.value == definition) ?? candidates[0];
            return [first.Id, .. candidates.Where(c => c.Id != first.Id).Take(Math.Max(0, count - 1)).Select(c => c.Id)];
        }
    }
}
