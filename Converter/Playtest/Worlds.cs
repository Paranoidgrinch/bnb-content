using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// ── WHAT THE PLAYER KNOWS, AND A FUTURE THAT FITS IT (LT_RUNNER_PLAN, "What the bot may know") ───────────────
// A fight's state holds two things a player at the table does not have: the ORDER of the draw pile and the dice
// still to be rolled (the engine rolls from RandomSeed + RandomStep, so the seed IS every future roll). The
// trial runner never searches the real fight. Everything it looks ahead on is a WORLD:
//
//   a copy of the real fight, with the hero's draw pile shuffled anew (the cards the player can see on top — a
//   disclosure such as the Article of Full Disclosure — stay where they are) and a fresh RandomSeed.
//
// So a world keeps everything a player can read off the table (hand, Energy, piles as contents, statuses,
// counters, intents) and replaces exactly what the player cannot.
//
// ⚠ THE ENGINE HAS NO "FORK WITH ANOTHER SEED". InteractiveCombat.Fork() restores its own snapshot with its own
// seed; a world needs the snapshot EDITED first. The snapshot is a public record, CombatState.Restore is public,
// and so is the InteractiveCombat constructor that resumes a restored state — only the compiled scenario and the
// enemy-intent selector the fight was built with are private. Those two (immutable content) are read by
// reflection, once, which is the least invasive way to build the world without changing RogueDeck-Core.
public static class Worlds
{
    private static readonly FieldInfo Compiled =
        typeof(InteractiveCombat).GetField("_compiled", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("InteractiveCombat no longer has a _compiled field");

    private static readonly FieldInfo Intent =
        typeof(InteractiveCombat).GetField("_enemyIntent", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("InteractiveCombat no longer has an _enemyIntent field");

    // A future the player cannot tell apart from the real one: the draw pile reshuffled (the visible top kept),
    // every roll to come redrawn. Quiet: a world keeps no trace.
    public static InteractiveCombat Sample(InteractiveCombat from, Random dice)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(dice);
        var snapshot = from.State.CreateSnapshot();
        var seen = from.IsOver ? 0 : Math.Max(0, from.HeroDisclosure.DrawPileCards);
        var zones = snapshot.CardZones.Select(entry => entry.CombatantId == from.HeroId
            ? entry with { Zones = entry.Zones with { DrawPile = Shuffle(entry.Zones.DrawPile, seen, dice) } }
            : entry).ToImmutableArray();
        snapshot = snapshot with { RandomSeed = dice.Next(), CardZones = zones };
        return Build(from, snapshot);
    }

    // The draw takes from the END of the pile: the last `seen` cards are the visible top and stay put.
    private static ImmutableArray<CardInstanceSnapshot> Shuffle(ImmutableArray<CardInstanceSnapshot> pile, int seen, Random dice)
    {
        if (pile.IsDefaultOrEmpty)
            return pile;
        var hidden = Math.Max(0, pile.Length - seen);
        // The hidden part is first put in a CANONICAL order (by instance id: the same set of cards whatever
        // order the real pile holds them in), so the world drawn from it depends on the dice alone — never on
        // the real order it was taken from.
        var cards = pile.Take(hidden).OrderBy(c => c.Id.value, StringComparer.Ordinal).Concat(pile.Skip(hidden)).ToArray();
        for (var i = hidden - 1; i > 0; i--)
        {
            var j = dice.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        return [.. cards];
    }

    private static InteractiveCombat Build(InteractiveCombat from, CombatStateSnapshot snapshot)
    {
        var compiled = (CompiledScenario)Compiled.GetValue(from)!;
        var intent = (Func<CombatState, CombatantId, int, EnemyActionDefinitionId?>)Intent.GetValue(from)!;
        var state = CombatState.Restore(snapshot, from.Registry);
        var world = new InteractiveCombat(compiled, state, intent, startOpeningTurn: false);
        world.State.TraceListener = null;
        return world;
    }

    // ── what the player sees, as one string ─────────────────────────────────────────────────────────────────
    // Everything on the table and nothing behind it: the piles by CONTENT (sorted), never by order; no seed, no
    // roll counter. Two states with the same reading are the same decision to a player — so the bot's own dice
    // are seeded from this reading (plus the bot's seed), and swapping the hidden order cannot change a choice.
    public static string Visible(InteractiveCombat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        var state = combat.State;
        var sb = new StringBuilder(512);
        sb.Append(state.CurrentRound).Append('|').Append(state.CurrentTurn).Append('|').Append((int)state.TurnPhase)
            .Append('|').Append((int)state.Result).Append('|').Append(state.ActiveCombatantId?.value).Append('\n');
        foreach (var c in state.Combatants)
        {
            sb.Append(c.Id.value).Append(' ').Append(c.DefinitionId.value).Append(' ').Append((int)c.LifecycleState)
                .Append(' ').Append(c.Health.Current).Append('/').Append(c.Health.Max);
            foreach (var (key, pool) in c.Resources.OrderBy(r => r.Key.value, StringComparer.Ordinal))
                sb.Append(" r:").Append(key.value).Append('=').Append(pool.Current);
            foreach (var (key, pool) in c.DefensivePools.OrderBy(r => r.Key.value, StringComparer.Ordinal))
                sb.Append(" d:").Append(key.value).Append('=').Append(pool.Current);
            foreach (var s in c.Statuses.OrderBy(s => s.DefinitionId.value, StringComparer.Ordinal).ThenBy(s => s.Stacks))
                sb.Append(" s:").Append(s.DefinitionId.value).Append('=').Append(s.Stacks).Append('/').Append(s.DurationTurns);
            foreach (var (key, value) in c.Counters.OrderBy(k => k.Key.value, StringComparer.Ordinal))
                sb.Append(" c:").Append(key.value).Append('=').Append(value);
            if (c.Id != combat.HeroId)
                sb.Append(" i:").Append(combat.UpcomingIntentFor(c.Id)?.Label);
            sb.Append('\n');
        }
        var zones = state.GetCardZones(combat.HeroId);
        void Pile(string name, CardZone zone, bool ordered)
        {
            var cards = zones.GetCardsInZone(zone).Select(Card);
            sb.Append(name).Append(':').Append(string.Join(',', ordered ? cards : cards.Order(StringComparer.Ordinal))).Append('\n');
        }
        Pile("hand", CardZone.Hand, ordered: true);
        Pile("draw", CardZone.DrawPile, ordered: false);
        Pile("disc", CardZone.DiscardPile, ordered: false);
        Pile("exh", CardZone.ExhaustPile, ordered: false);
        var top = combat.RevealedDrawPile;
        if (top.Count > 0)
            sb.Append("top:").Append(string.Join(',', top.Select(Card))).Append('\n');
        return sb.ToString();
    }

    private static string Card(CardInstance card) =>
        card.Marks.Count == 0 ? card.DefinitionId.value : card.DefinitionId.value + "#" + string.Join('#', card.Marks.Select(m => m.value).Order(StringComparer.Ordinal));

    // The bot's dice for one decision: its own seed and what it can see, nothing else.
    public static int Seed(int botSeed, InteractiveCombat combat, string salt = "")
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{botSeed}|{salt}|{Visible(combat)}"));
        return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
    }
}
