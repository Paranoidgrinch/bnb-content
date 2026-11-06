using System.Diagnostics;
using System.Text;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// ── THE TRIAL BOT: ONE FIGHT, PLAYED BY DETERMINIZED SEARCH (LT_RUNNER_PLAN, "How it plays") ──────────────────
// At each of its turns the hand is known; what is not known is the order of the draw pile and every roll to come.
// So a turn is decided in three steps, and none of them looks at the real fight:
//
//   1. WITHIN THE TURN. A world (Worlds.Sample) is drawn and every way of playing this turn is laid out on it —
//      which card, at whom, in which order, what to answer when a card asks, when to stop — merged by position.
//      Up to `PerTurn` end-of-turn positions; the best `Beam` of them by a cheap reading (the turn ended on a
//      copy: health kept against enemy health taken) go on.
//   2. LEAF EVALUATION. Each of those is played on in `Worlds` (K) fresh worlds — its draw pile reshuffled and its
//      dice redrawn again — by a cheap greedy player for `Horizon` (H) turns or to the end of the fight. Score:
//      health lost, plus a large penalty for a loss, plus a little for each round (a faster win breaks a tie).
//      The worlds are the same K for every leaf (common random numbers), so leaves are compared on equal luck.
//   3. CHOICE. The line with the best mean is played on the real fight, one move at a time. When the real fight
//      stops looking like the world it was planned on (a card drew something else, a roll fell differently),
//      the turn is planned again from where it really stands.
//
// The bot's own dice are seeded from its seed and what it can SEE (Worlds.Seed): two fights that differ only in
// what is hidden get the same decisions. That is tested (TrialRunnerTests).
public sealed class TrialBot
{
    public sealed record Options(
        int Worlds = 3, int Horizon = 3, int Beam = 4, int PerTurn = 50, int PlaysDeep = 10, int TurnCap = 40,
        int ChoiceCap = 4, int Replans = 6);

    public sealed record Outcome(
        bool Won, bool Ended, int Lost, int Rounds, int Refused, int Replans, long Positions, double Seconds,
        IReadOnlyDictionary<string, int> Played, string? Error);

    // A card's (or an action's) question, answered: by card DEFINITION (a world holds other copies than the real
    // fight) or by option index.
    public sealed record Answer(IReadOnlyList<string>? Cards, IReadOnlyList<int>? Options)
    {
        public override string ToString() => Cards is { } c ? $"[{string.Join(",", c)}]" : $"[#{string.Join(",", Options ?? [])}]";
    }

    public sealed record Move(CardInstanceId? Card, CardDefinitionId? Action, CombatantId? Target, IReadOnlyList<Answer> Script, string Label);

    private readonly Options _options;
    private readonly int _seed;
    private readonly Func<string, bool> _needsTarget;
    private long _positions;

    public TrialBot(Options options, int seed, Func<string, bool> needsTarget)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _seed = seed;
        _needsTarget = needsTarget ?? throw new ArgumentNullException(nameof(needsTarget));
    }

    // ── the real fight ──────────────────────────────────────────────────────────────────────────────────────

    public Outcome Play(InteractiveCombat real, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(real);
        var clock = Stopwatch.StartNew();
        _positions = 0;
        var start = real.HeroHealth;
        var played = new Dictionary<string, int>(StringComparer.Ordinal);
        var refused = 0;
        var replans = 0;
        var turns = 0;
        try
        {
            while (!real.IsOver && turns < _options.TurnCap)
            {
                if (!real.IsHeroTurn)
                {
                    real.EndTurn();
                    continue;
                }
                turns++;
                log?.Invoke($"turn {turns}: hero {real.HeroHealth} block {real.HeroGuard} energy {real.HeroEnergy} · hand "
                    + string.Join(", ", real.Hand.Select(c => c.DefinitionId.value)) + " · "
                    + string.Join("; ", real.State.Combatants.Where(c => c.Id != real.HeroId && c.IsAlive)
                        .Select(c => $"{c.Id.value} {c.Health.Current}/{c.Health.Max} [{real.UpcomingIntentFor(c.Id)?.Label}] "
                            + string.Join(" ", c.Statuses.Select(st => $"{st.DefinitionId.value}x{st.Stacks}")))));
                var plan = Decide(real);
                var i = 0;
                var again = 0;
                while (i < plan.Count && !real.IsOver && real.IsHeroTurn)
                {
                    var (move, expected) = plan[i];
                    var label = MoveName(real, move);
                    if (!Apply(real, move, out _))
                    {
                        refused++;
                        break;
                    }
                    played[label] = played.GetValueOrDefault(label) + 1;
                    log?.Invoke($"   play {label}{(move.Target is { } aim ? " -> " + aim.value : "")}{string.Concat(move.Script.Select(a => " " + a))}");
                    i++;
                    if (real.IsOver || i >= plan.Count)
                        break;
                    if (Worlds.Visible(real) != expected && again < _options.Replans)
                    {
                        again++;
                        replans++;
                        plan = Decide(real);
                        i = 0;
                    }
                }
                if (real.IsOver)
                    break;
                EndTurn(real);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
        {
            return new Outcome(false, false, start, turns, refused, replans, _positions, clock.Elapsed.TotalSeconds, played, ex.Message);
        }
        var won = real.Result == CombatResult.Victory;
        return new Outcome(won, real.IsOver, won ? start - real.HeroHealth : start, turns, refused, replans, _positions,
            clock.Elapsed.TotalSeconds, played, null);
    }

    private static string MoveName(InteractiveCombat at, Move move) =>
        move.Card is { } id
            ? at.Hand.FirstOrDefault(c => c.Id == id)?.DefinitionId.value ?? id.value
            : "!" + move.Action?.value;

    // ── one turn's decision: the moves, each with the table it should leave behind ──────────────────────────

    public IReadOnlyList<(Move Move, string Expected)> Decide(InteractiveCombat real)
    {
        ArgumentNullException.ThrowIfNull(real);
        var dice = new Random(Worlds.Seed(_seed, real, "plan"));
        var world = Worlds.Sample(real, dice);
        var leaves = Leaves(world);
        if (leaves.Count == 0)
            return [];

        var heroMax = Math.Max(1, world.State.GetCombatant(world.HeroId).Health.Max);
        var enemyMax = Math.Max(1, EnemyMax(world));
        // The cheap reading: the turn ended on a copy of the planning world.
        var ranked = leaves
            .Select(leaf => (Leaf: leaf, Quick: Quick(leaf.State, heroMax, enemyMax)))
            .OrderByDescending(x => x.Quick)
            .Take(Math.Max(1, _options.Beam))
            .Select(x => x.Leaf)
            .ToList();

        var worldSeeds = Enumerable.Range(0, Math.Max(1, _options.Worlds)).Select(_ => dice.Next()).ToArray();
        var rootHealth = real.HeroHealth;
        var rootEnemies = EnemyHealth(real);
        Leaf? best = null;
        var bestScore = double.MaxValue;
        foreach (var leaf in ranked)
        {
            var total = 0.0;
            foreach (var s in worldSeeds)
                total += Rollout(leaf.State, new Random(s), rootHealth, rootEnemies, endScript: null);
            var mean = total / worldSeeds.Length;
            if (mean < bestScore - 1e-9)
            {
                bestScore = mean;
                best = leaf;
            }
        }
        return best!.Path;
    }

    private sealed record Leaf(InteractiveCombat State, IReadOnlyList<(Move Move, string Expected)> Path);

    // Every way this turn can be played on the planning world, as the position it stops in (before ending).
    private List<Leaf> Leaves(InteractiveCombat world)
    {
        var leaves = new List<Leaf>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        seen.Add(Shape(world));
        Walk(world, [], 0, leaves, seen, _options.PerTurn);
        return leaves;
    }

    private void Walk(InteractiveCombat node, IReadOnlyList<(Move, string)> path, int depth, List<Leaf> leaves,
        HashSet<string> seen, int cap)
    {
        if (leaves.Count >= cap)
            return;
        leaves.Add(new Leaf(node, path));
        if (node.IsOver || !node.IsHeroTurn || depth >= _options.PlaysDeep)
            return;

        var moves = Candidates(node);
        // THE BUDGET IS SHARED OUT AT THE ROOT (FightPlanner's lesson): depth-first against one budget, the first
        // card's lines would use all of it.
        var share = depth == 0 ? Math.Max(1, (cap - leaves.Count) / Math.Max(1, moves.Count)) : 0;
        foreach (var move in moves)
        {
            foreach (var (child, done) in Expand(node, move))
            {
                if (leaves.Count >= cap)
                    return;
                if (!seen.Add(Shape(child)))
                    continue;
                var limit = depth == 0 ? Math.Min(cap, leaves.Count + share) : cap;
                Walk(child, [.. path, (done, Worlds.Visible(child))], depth + 1, leaves, seen, limit);
            }
        }
    }

    // What may be tried from here: each card in hand once per definition, at each enemy it can be aimed at; the
    // hero's own actions likewise. Whether it is actually allowed is the engine's to say (a refused play is dropped).
    private List<Move> Candidates(InteractiveCombat node, bool oneTarget = false)
    {
        var enemies = Living(node);
        // The cheap player aims at one body only: the weakest still standing (finish what can be finished).
        if (oneTarget && enemies.Count > 1)
            enemies = [enemies.OrderBy(e => node.State.GetCombatant(e).Health.Current).ThenBy(e => e.value, StringComparer.Ordinal).First()];
        var targets = enemies.Count == 0 ? [null] : enemies.Select(e => (CombatantId?)e).ToList();
        var moves = new List<Move>();
        foreach (var card in node.Hand.GroupBy(c => c.DefinitionId).Select(g => g.First()))
        {
            if (!node.CanPlay(card.Id) || Greedy.Borrows(node, card))
                continue;
            foreach (var target in _needsTarget(card.DefinitionId.value) ? targets : [null])
                moves.Add(new Move(card.Id, null, target, [], card.DefinitionId.value));
        }
        foreach (var action in node.Actions)
            foreach (var target in targets)
                if (node.CanUse(action, target))
                    moves.Add(new Move(null, action, target, [], "!" + action.value));
        return moves;
    }

    // A move, played on a copy — and when the card asks something nobody scripted, once per answer worth trying.
    private List<(InteractiveCombat Child, Move Done)> Expand(InteractiveCombat node, Move move)
    {
        var results = new List<(InteractiveCombat, Move)>();
        var queue = new Queue<Move>();
        queue.Enqueue(move);
        while (queue.Count > 0)
        {
            var m = queue.Dequeue();
            var child = node.ForkQuiet();
            _positions++;
            if (!Apply(child, m, out var asks))
                continue;
            if (asks.Count > 0 && m.Script.Count < 2)
                foreach (var answer in Alternatives(asks[0]))
                    queue.Enqueue(m with { Script = [.. m.Script, answer] });
            else
                results.Add((child, m));
        }
        return results;
    }

    private IEnumerable<Answer> Alternatives(Answers.Ask ask)
    {
        if (ask.Cards is { } cards)
            return cards.Distinct(StringComparer.Ordinal).Take(_options.ChoiceCap).Select(c => new Answer([c], null));
        var options = ask.Options ?? [];
        if (ask.Count <= 1)
            return Enumerable.Range(0, options.Count).Take(_options.ChoiceCap).Select(i => new Answer(null, [i]));
        return Combinations(options.Count, ask.Count).Take(_options.ChoiceCap).Select(c => new Answer(null, c));
    }

    private static IEnumerable<IReadOnlyList<int>> Combinations(int n, int k)
    {
        if (k <= 0 || k > n)
        {
            yield return Enumerable.Range(0, Math.Min(n, Math.Max(0, k))).ToList();
            yield break;
        }
        var pick = Enumerable.Range(0, k).ToArray();
        while (true)
        {
            yield return [.. pick];
            var i = k - 1;
            while (i >= 0 && pick[i] == n - k + i)
                i--;
            if (i < 0)
                yield break;
            pick[i]++;
            for (var j = i + 1; j < k; j++)
                pick[j] = pick[j - 1] + 1;
        }
    }

    // Plays a move with its scripted answers. False when the engine refused it (or the card is not in hand).
    public static bool Apply(InteractiveCombat at, Move move, out IReadOnlyList<Answers.Ask> unscripted)
    {
        var answers = new Answers(move.Script);
        unscripted = answers.Unscripted;
        if (move.Card is { } id && at.Hand.All(c => c.Id != id))
            return false;
        at.State.SetCardChooser(answers);
        at.State.SetOptionChooser(answers);
        var steps = at.Steps.Count;
        try
        {
            if (move.Card is { } card)
                at.PlayCard(card, move.Target);
            else if (move.Action is { } action)
                at.UseAction(action, move.Target);
        }
        finally
        {
            at.State.SetCardChooser(null);
            at.State.SetOptionChooser(null);
        }
        return !Greedy.Refused(at, steps);
    }

    // ── ending the real turn: a question asked at the turn's end is decided like a move ─────────────────────

    private void EndTurn(InteractiveCombat real)
    {
        var dice = new Random(Worlds.Seed(_seed, real, "end"));
        var probe = Worlds.Sample(real, dice);
        var recorder = new Answers([]);
        probe.State.SetCardChooser(recorder);
        probe.State.SetOptionChooser(recorder);
        probe.EndTurn();
        Answer? chosen = null;
        if (recorder.Unscripted.Count > 0)
        {
            var seeds = Enumerable.Range(0, Math.Max(1, _options.Worlds)).Select(_ => dice.Next()).ToArray();
            var best = double.MaxValue;
            foreach (var answer in Alternatives(recorder.Unscripted[0]))
            {
                var total = seeds.Sum(s => Rollout(real, new Random(s), real.HeroHealth, EnemyHealth(real), [answer]));
                if (total < best)
                {
                    best = total;
                    chosen = answer;
                }
            }
        }
        var answers = new Answers(chosen is null ? [] : [chosen]);
        real.State.SetCardChooser(answers);
        real.State.SetOptionChooser(answers);
        try
        {
            real.EndTurn();
        }
        finally
        {
            real.State.SetCardChooser(null);
            real.State.SetOptionChooser(null);
        }
    }

    // ── a sampled future, played on by the cheap player ─────────────────────────────────────────────────────

    // Lower is better: health lost from the decision on, a loss as everything plus a large penalty, a round as a
    // tenth of a point. A fight still going at the horizon is finished on paper: at the rate the rollout traded
    // blows, how long until the enemies fall and what that costs.
    private const double LossPenalty = 1000;
    private const double PerRound = 0.1;

    private double Rollout(InteractiveCombat leaf, Random dice, int rootHealth, int rootEnemies, IReadOnlyList<Answer>? endScript)
    {
        var w = Worlds.Sample(leaf, dice);
        _positions++;
        var turns = 1;
        if (!w.IsOver && w.IsHeroTurn)
            EndWith(w, endScript ?? []);
        while (!w.IsOver && turns < _options.Horizon)
        {
            if (!w.IsHeroTurn)
            {
                w.EndTurn();
                continue;
            }
            turns++;
            w = GreedyTurn(w);
            if (w.IsOver)
                break;
            EndWith(w, []);
        }
        if (w.Result == CombatResult.Defeat || w.HeroHealth <= 0)
            return LossPenalty + rootHealth + PerRound * turns;
        var lost = Math.Max(0, rootHealth - w.HeroHealth);
        if (w.Result == CombatResult.Victory)
            return lost + PerRound * turns;
        var enemies = EnemyHealth(w);
        var dealt = Math.Max(1.0, (rootEnemies - enemies) / (double)turns);
        var rate = lost / (double)turns;
        var more = Math.Min(30, Math.Ceiling(enemies / dealt));
        var tail = rate * more;
        if (tail >= w.HeroHealth)
            return LossPenalty * 0.5 + rootHealth + PerRound * (turns + more);
        return lost + tail + PerRound * (turns + more);
    }

    private void EndWith(InteractiveCombat w, IReadOnlyList<Answer> script)
    {
        var answers = new Answers(script);
        w.State.SetCardChooser(answers);
        w.State.SetOptionChooser(answers);
        w.EndTurn();
        w.State.SetCardChooser(null);
        w.State.SetOptionChooser(null);
    }

    // THE CHEAP PLAYER: card by card, the play that does the most right now — damage (a kill counts extra), Block
    // up to what is coming, statuses on either side, cards drawn and Energy gained — until nothing pays.
    private InteractiveCombat GreedyTurn(InteractiveCombat w)
    {
        var incoming = Incoming(w);
        for (var plays = 0; plays < _options.PlaysDeep && !w.IsOver && w.IsHeroTurn; plays++)
        {
            InteractiveCombat? bestChild = null;
            var bestGain = 0.0;
            foreach (var move in Candidates(w, oneTarget: true))
            {
                var child = w.ForkQuiet();
                _positions++;
                if (!Apply(child, move, out _))
                    continue;
                var gain = Gain(w, child, incoming);
                if (gain > bestGain)
                {
                    bestGain = gain;
                    bestChild = child;
                }
            }
            if (bestChild is null)
                break;
            // The child IS the next position: the rollout carries on from the newest copy.
            w = bestChild;
        }
        return w;
    }

    private static int Incoming(InteractiveCombat w)
    {
        var f = w.ForkQuiet();
        var before = f.HeroHealth;
        f.EndTurn();
        return Math.Max(0, before - f.HeroHealth) + w.HeroGuard;
    }

    private static double Gain(InteractiveCombat before, InteractiveCombat after, int incoming)
    {
        if (after.Result == CombatResult.Victory)
            return 10000;
        var hero0 = before.State.GetCombatant(before.HeroId);
        var hero1 = after.State.GetCombatant(after.HeroId);
        var damage = EnemyHealth(before) - EnemyHealth(after);
        var kills = Living(before).Count - Living(after).Count;
        var guard = Math.Min(after.HeroGuard, incoming) - Math.Min(before.HeroGuard, incoming);
        var debuffs = EnemyDebuffs(after) - EnemyDebuffs(before);
        var buffs = Stacks(hero1, StatusPolarity.Buff) - Stacks(hero0, StatusPolarity.Buff);
        var curses = Stacks(hero1, StatusPolarity.Debuff) - Stacks(hero0, StatusPolarity.Debuff);
        var health = after.HeroHealth - before.HeroHealth;
        var drawn = after.Hand.Count - before.Hand.Count + 1; // the card played left the hand
        var energy = after.HeroEnergy - before.HeroEnergy;    // negative: what it cost
        return damage + 8 * kills + guard + 1.5 * debuffs + 1.5 * buffs - 1.5 * curses + 2 * health
            + 3 * Math.Max(0, drawn) + 2 * energy + 0.5;
    }

    // ── readings ────────────────────────────────────────────────────────────────────────────────────────────

    private double Quick(InteractiveCombat leaf, int heroMax, int enemyMax)
    {
        var f = leaf.ForkQuiet();
        _positions++;
        if (!f.IsOver && f.IsHeroTurn)
            f.EndTurn();
        return f.Result switch
        {
            CombatResult.Victory => 1000.0 + f.HeroHealth,
            CombatResult.Defeat => -1000.0,
            _ => (double)f.HeroHealth / heroMax - (double)EnemyHealth(f) / enemyMax + 0.002 * EnemyDebuffs(f),
        };
    }

    private static List<CombatantId> Living(InteractiveCombat at)
    {
        var standing = at.State.Combatants
            .Where(c => c.Id != at.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive).ToList();
        // A body a rule holds at 1 HP is aimed at only when nothing else stands (Greedy.HeldUp).
        var open = standing.Where(c => !Greedy.HeldUp(c)).ToList();
        return [.. (open.Count > 0 ? open : standing).Select(c => c.Id)];
    }

    private static int EnemyHealth(InteractiveCombat at) => at.State.Combatants
        .Where(c => c.Id != at.HeroId && c.IsAlive && c.TeamId == StandardCombatIds.EnemyTeam)
        .Sum(c => c.Health.Current);

    private static int EnemyMax(InteractiveCombat at) => at.State.Combatants
        .Where(c => c.Id != at.HeroId && c.TeamId == StandardCombatIds.EnemyTeam)
        .Sum(c => c.Health.Max);

    private static int EnemyDebuffs(InteractiveCombat at) => at.State.Combatants
        .Where(c => c.Id != at.HeroId && c.IsAlive && c.TeamId == StandardCombatIds.EnemyTeam)
        .Sum(c => Stacks(c, StatusPolarity.Debuff));

    private static int Stacks(CombatantState body, StatusPolarity polarity) =>
        body.Statuses.Where(s => s.Polarity == polarity).Sum(s => Math.Max(1, s.Stacks));

    // A position, for merging within one world: every pile IN ORDER and by definition (two copies of a card are
    // the same move), every status with its clocks, the roll counter. (FightPlanner.Shape, which is internal.)
    private static string Shape(InteractiveCombat combat)
    {
        var snapshot = combat.State.CreateSnapshot();
        var sb = new StringBuilder(512);
        sb.Append(snapshot.RandomStep).Append('|').Append(snapshot.CurrentRound).Append('|').Append(snapshot.CurrentTurn)
            .Append('|').Append((int)snapshot.TurnPhase).Append('|').Append((int)snapshot.Result).Append('|')
            .Append(snapshot.ActiveCombatantId?.value).Append('\n');
        foreach (var c in snapshot.Combatants)
        {
            sb.Append(c.Id.value).Append(' ').Append((int)c.LifecycleState).Append(' ').Append(c.HealthCurrent).Append('/').Append(c.HealthMax);
            foreach (var (key, pool) in c.Resources)
                sb.Append(" r:").Append(key.value).Append('=').Append(pool.Current);
            foreach (var (key, pool) in c.DefensivePools)
                sb.Append(" d:").Append(key.value).Append('=').Append(pool.Current);
            foreach (var s in c.Statuses)
                sb.Append(" s:").Append(s.DefinitionId.value).Append('=').Append(s.Stacks).Append('/').Append(s.DurationTurns)
                    .Append('/').Append(s.Charges).Append('/').Append(s.PendingTurns);
            foreach (var (key, value) in c.Counters)
                sb.Append(" c:").Append(key.value).Append('=').Append(value);
            sb.Append('\n');
        }
        foreach (var (owner, zones) in snapshot.CardZones)
        {
            sb.Append(owner.value);
            void Pile(string name, System.Collections.Immutable.ImmutableArray<CardInstanceSnapshot> cards)
            {
                sb.Append(' ').Append(name).Append(':');
                if (cards.IsDefaultOrEmpty)
                    return;
                foreach (var card in cards)
                {
                    sb.Append(card.DefinitionId.value);
                    if (!card.Marks.IsDefaultOrEmpty)
                        foreach (var mark in card.Marks)
                            sb.Append('#').Append(mark.value);
                    sb.Append(',');
                }
            }
            Pile("draw", zones.DrawPile);
            Pile("hand", zones.Hand);
            Pile("disc", zones.DiscardPile);
            Pile("exh", zones.ExhaustPile);
            Pile("ban", zones.BanishedPile);
            Pile("queue", zones.QueuePile);
            Pile("aside", zones.SetAsidePile);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // ── the chooser on every copy: scripted answers first, then the first offered (and a note that it was asked) ──
    public sealed class Answers(IReadOnlyList<Answer> script) : ICombatCardChooser, ICombatOptionChooser
    {
        public sealed record Ask(IReadOnlyList<string>? Cards, IReadOnlyList<string>? Options, int Count);

        private int _next;
        public List<Ask> Unscripted { get; } = [];

        public IReadOnlyList<CardInstanceId> ChooseCards(IReadOnlyList<CardInstance> candidates, int count, string purpose)
        {
            var take = Math.Min(count, candidates.Count);
            var answer = _next < script.Count ? script[_next++] : null;
            if (answer?.Cards is not { } wanted)
            {
                if (answer is null && candidates.Count > 1)
                    Unscripted.Add(new Ask([.. candidates.Select(c => c.DefinitionId.value)], null, count));
                return [.. candidates.Take(take).Select(c => c.Id)];
            }
            var picked = new List<CardInstanceId>();
            foreach (var definition in wanted)
                if (candidates.FirstOrDefault(c => c.DefinitionId.value == definition && !picked.Contains(c.Id)) is { } hit && picked.Count < take)
                    picked.Add(hit.Id);
            foreach (var c in candidates)
                if (picked.Count < take && !picked.Contains(c.Id))
                    picked.Add(c.Id);
            return picked;
        }

        public IReadOnlyList<int> ChooseOptions(IReadOnlyList<string> options, int count, string purpose)
        {
            var take = Math.Min(count, options.Count);
            var answer = _next < script.Count ? script[_next++] : null;
            if (answer?.Options is not { } wanted)
            {
                if (answer is null && options.Count > 1)
                    Unscripted.Add(new Ask(null, [.. options], count));
                return [.. Enumerable.Range(0, take)];
            }
            var picked = wanted.Where(i => i >= 0 && i < options.Count).Distinct().Take(take).ToList();
            for (var i = 0; picked.Count < take && i < options.Count; i++)
                if (!picked.Contains(i))
                    picked.Add(i);
            return picked;
        }
    }
}
