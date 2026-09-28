using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// ── THE AUDIT (CONTENT_FIX_PLAN F5) ──────────────────────────────────────────────────────────────────────────
// Does every card and every relic do what its text says — and is what it does any use to a player?
//
// ⚠ OVER FOUR ROUNDS, NOT ONE PLAY (user, 2026-09-28). Paperwork ticks on every enemy turn and never decays, a
// Queued card resolves next turn, Doubt waits for the next attack, a Rite installs a rule. So every measurement is
// a whole short fight — the card played once on turn 1, then four rounds watched — held against the SAME fight
// (same seed, same deck) in which it was not played. Two dummies: a quiet one that does nothing (what does the
// card do to the enemy and to me?) and one that hits for 10 a turn (what does it save me?).
//
// ⚠ SYNERGIES ARE MEASURED APART: a pair played together, against each of its halves alone.
//
// ⚠ RELICS ALONE AND WITH A COMPANION: the Remittitur Seal bug showed only when a second relic's opening rule
// landed on the hero, so every relic is also run beside one.
//
// What it cannot know is named rather than guessed: a text with a condition ("If …", "for each …") whose number
// did not show is CONDITION, not BUG — the dummy may simply not have met it.
public static class Audit
{
    public const int Rounds = 4;
    private const int Seed = 11;
    private const string QuietEnemy = "elsewhere_path";
    private const string QuietAction = "elsewhere_path.name_the_way";   // an empty sequence: does nothing at all
    private const string Striker = "filing_beetle";
    private const string StrikeAction = "filing_beetle.mandible_stamp"; // 10 damage, nothing else
    private const string Companion = "petitioners_token";

    private static readonly string[] Starters = ["paper_cut", "cower_behind_a_desk", "strong_binder", "permit_a38"];

    // ── what one fight looked like ────────────────────────────────────────────────────────────────────────

    public sealed record Snap(
        int HeroHp, int HeroBlock, IReadOnlyDictionary<string, int> HeroStatuses,
        int EnemyHp, int EnemyBlock, IReadOnlyDictionary<string, int> EnemyStatuses,
        int Hand, int Draw, int Discard, int Exhaust, int Queue, int Energy)
    {
        public int Cards => Hand + Draw + Discard + Exhaust + Queue;

        // Everything that can differ, as one string — two fights "did the same" when every snap matches.
        public string Key =>
            $"{HeroHp}|{HeroBlock}|{EnemyHp}|{EnemyBlock}|{Hand}|{Draw}|{Discard}|{Exhaust}|{Queue}|{Energy}|"
            + string.Join(",", HeroStatuses.Where(s => s.Key != "the_applicant").OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={s.Value}"))
            + "|" + string.Join(",", EnemyStatuses.OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={s.Value}"));
    }

    // Start of turn 1, straight after the turn-1 plays, then the start of turns 2 … Rounds+1.
    public sealed record Trace(IReadOnlyList<Snap> Snaps, string? Error)
    {
        public Snap Start => Snaps[0];
        public Snap AfterPlay => Snaps.Count > 1 ? Snaps[1] : Snaps[0];
        public Snap End => Snaps[^1];
        public string Key => string.Join(" / ", Snaps.Select(s => s.Key));
    }

    public sealed record Finding(string Severity, string Subject, string What, string Repro);

    // ── the fights ────────────────────────────────────────────────────────────────────────────────────────

    public static EncounterDefinition Dummy(RunBlueprint game, bool strikes, int energy)
    {
        var template = game.Encounters.SelectMany(e => e.Enemies).FirstOrDefault(e => e.Id == (strikes ? Striker : QuietEnemy))
            ?? throw new InvalidOperationException("the audit's dummy enemy is not in the game");
        var body = template with
        {
            MaxHealth = 999,
            Actions = [new EnemyActionDefinitionId(strikes ? StrikeAction : QuietAction)],
            StartingStatuses = [],
            IntentRules = null,
        };
        return new EncounterDefinition(new EncounterId(strikes ? "audit.striker" : "audit.quiet"), [body],
            [new ResourceSpec(StandardCombatIds.EnergyResource, energy, energy, CanExceedMax: true)],
            heroStartingStatuses: SparringRing.HeroStatuses(body.Id));
    }

    // One fight: `turnOne` plays whatever the question needs on turn 1 (and may play on later turns through
    // `everyTurn`); the rest of the time the hero only ends its turn.
    public static Trace Fight(
        RunBlueprint game, EncounterDefinition dummy, IReadOnlyList<string> deck, IReadOnlyList<string>? relics,
        Action<RunPlayback, InteractiveCombatDriver, Random> turnOne,
        Action<RunPlayback, InteractiveCombatDriver, Random>? everyTurn = null, int rounds = Rounds)
    {
        var ring = SparringRing.OneFight(game, dummy, deck, maxHealth: 200, relics: relics);
        var snaps = new List<Snap>();
        using var play = new RunPlayback(() => { });
        try
        {
            play.Start(ring, Seed, interactive: true);
            var session = play.Session;
            if (play.Error is { } startError || session is null)
                return new Trace(snaps, play.Error ?? "no session");
            while (session.IsAwaitingInterlude)
                session.Continue();
            if (play.CombatDriver is not { Current: not null } driver)
                return new Trace(snaps, "no fight");

            var rng = new Random(Seed);
            snaps.Add(Take(driver.Current!));
            turnOne(play, driver, rng);
            Settle(driver, rng);
            if (driver.Current is null)
                return new Trace(snaps, null);
            snaps.Add(Take(driver.Current));
            for (var round = 0; round < rounds; round++)
            {
                if (driver.Current is null)
                    break;
                if (round > 0 && everyTurn is not null)
                {
                    everyTurn(play, driver, rng);
                    Settle(driver, rng);
                    if (driver.Current is null)
                        break;
                }
                driver.EndTurn();
                Settle(driver, rng);
                if (driver.Current is null)
                    break;
                snaps.Add(Take(driver.Current));
            }
            return new Trace(snaps, play.Error ?? session.Error);
        }
        catch (Exception ex)
        {
            return new Trace(snaps, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Settle(InteractiveCombatDriver driver, Random rng)
    {
        for (var guard = 0; guard < 20 && Greedy.Answer(driver, rng); guard++)
        {
        }
    }

    // Play the first copy in hand of each card named, in order, at the enemy.
    public static Action<RunPlayback, InteractiveCombatDriver, Random> Plays(params string[] cards) =>
        (play, driver, rng) =>
        {
            foreach (var id in cards)
            {
                Settle(driver, rng);
                if (driver.Current is not { } combat)
                    return;
                var card = combat.Hand.FirstOrDefault(c => c.DefinitionId.value == id);
                if (card is null)
                    continue;
                var needsTarget = play.CardNeedsTarget.TryGetValue(id, out var needs) && needs;
                driver.PlayCard(card.Id, needsTarget ? Greedy.Enemy(combat) : null);
            }
        };

    // A plain player for the relic fights: play what can be played, in a seeded order.
    private static void Greedily(RunPlayback play, InteractiveCombatDriver driver, Random rng)
    {
        var refused = new HashSet<CardInstanceId>();
        var barren = new HashSet<string>(StringComparer.Ordinal);
        for (var plays = 0; plays < 12; plays++)
        {
            Settle(driver, rng);
            if (driver.Current is not { } combat || Greedy.Choose(play, combat, rng, refused, barren) is not { } card)
                return;
            var needsTarget = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var needs) && needs;
            var before = combat.Steps.Count;
            driver.PlayCard(card.Id, needsTarget ? Greedy.Enemy(combat) : null);
            if (Greedy.Refused(driver.Current, before))
                refused.Add(card.Id);
        }
    }

    public static Snap Take(InteractiveCombat combat)
    {
        var hero = combat.State.GetCombatant(combat.HeroId);
        var enemy = combat.State.Combatants.First(c => c.Id != combat.HeroId);
        var zones = combat.State.GetCardZones(combat.HeroId);
        static Dictionary<string, int> Statuses(CombatantState c) =>
            c.AllStatuses.GroupBy(s => s.DefinitionId.value).ToDictionary(g => g.Key, g => g.Sum(s => s.Stacks), StringComparer.Ordinal);
        return new Snap(
            hero.Health.Current, hero.DefensivePools.Values.Sum(p => p.Current), Statuses(hero),
            enemy.Health.Current, enemy.DefensivePools.Values.Sum(p => p.Current), Statuses(enemy),
            zones.GetCardsInZone(CardZone.Hand).Count, zones.GetCardsInZone(CardZone.DrawPile).Count,
            zones.GetCardsInZone(CardZone.DiscardPile).Count, zones.GetCardsInZone(CardZone.ExhaustPile).Count,
            zones.GetCardsInZone(CardZone.QueuePile).Count,
            hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool) ? pool.Current : 0);
    }

    // ── cards ─────────────────────────────────────────────────────────────────────────────────────────────

    public sealed record CardAudit(
        string Id, string Text, int Cost, Trace Quiet, Trace QuietBase, Trace Strike, Trace StrikeBase)
    {
        // What four rounds of it were worth: damage done in the quiet fight, health kept in the striking one.
        // Damage in whichever fight showed more of it: some effects wait for the enemy to attack (Doubt consumed →
        // Paperwork), and the quiet dummy never does.
        public int DamageDone => Math.Max(QuietBase.End.EnemyHp - Quiet.End.EnemyHp, StrikeBase.End.EnemyHp - Strike.End.EnemyHp);
        public int HealthKept => (Strike.End.HeroHp - StrikeBase.End.HeroHp);
        public int Value => DamageDone + HealthKept;
        public double PerEnergy => Value / (Cost <= 0 ? 0.5 : Cost);
    }

    // A card the audit never plays: it only fills the draw pile.
    private const string Filler = "cower_behind_a_desk";

    public static CardAudit AuditCard(RunBlueprint game, string id, int energy = 6)
    {
        // Eight copies and four fillers: five dealt, so at least one copy is always in hand, and seven left to draw
        // — the first audit dealt five of six, and a card that archives from the draw pile then drew from nothing.
        var deck = Enumerable.Repeat(id, 8).Concat(Enumerable.Repeat(Filler, 4)).ToList();
        var text = game.Cards.FirstOrDefault(c => c.Id == id)?.DescriptionKey ?? "";
        var cost = game.Cards.FirstOrDefault(c => c.Id == id)?.Costs.Sum(c => c.Amount) ?? 0;
        var quiet = Dummy(game, strikes: false, energy);
        var strike = Dummy(game, strikes: true, energy);
        var none = (Action<RunPlayback, InteractiveCombatDriver, Random>)((_, _, _) => { });
        return new CardAudit(id, text, cost,
            Fight(game, quiet, deck, null, Plays(id)), Fight(game, quiet, deck, null, none),
            Fight(game, strike, deck, null, Plays(id)), Fight(game, strike, deck, null, none));
    }

    // "Deal 4 damage 3 times" is 12: the count after it multiplies (the first audit read it as 4).
    private static readonly Regex Deal = new(@"Deal (\d+) damage(?: (\d+) times)?", RegexOptions.Compiled);
    private static readonly Regex Block = new(@"Gain (\d+) Block", RegexOptions.Compiled);
    private static readonly Regex Apply = new(@"(Apply|Gain) (\d+) ([A-Z][A-Za-z' ]+?)(?= to|[.,]| and|$)", RegexOptions.Compiled);
    private static readonly Regex DrawN = new(@"Draw (\d+) card", RegexOptions.Compiled);
    private static readonly Regex EnergyN = new(@"Gain (\d+) Energy", RegexOptions.Compiled);

    // Words that make a number conditional or scaling: the dummy may not have met the condition.
    private static readonly Regex Conditional = new(
        @"\b(If|if|unless|for each|per |plus|equal to|additional|instead|random|Choose|choose|Requires|At the|When|Whenever|next|X\b|up to|Count)",
        RegexOptions.Compiled);

    public static IEnumerable<Finding> Judge(RunBlueprint game, CardAudit a, double starterFloor)
    {
        var repro = $"--audit --audit-only {a.Id}";
        if ((a.Quiet.Error ?? a.Strike.Error) is { } broke)
        {
            yield return new Finding("BUG", a.Id, $"the fight broke: {broke}", repro);
            yield break;
        }
        var text = a.Text;
        var conditional = Conditional.IsMatch(text);
        var queued = text.StartsWith("Queue", StringComparison.Ordinal);
        // A Queued card's effect lands at the start of the next turn; everything else straight after the play.
        var before = a.Quiet.Start;
        var after = queued && a.Quiet.Snaps.Count > 2 ? a.Quiet.Snaps[2] : a.Quiet.AfterPlay;
        var baseAfter = queued && a.QuietBase.Snaps.Count > 2 ? a.QuietBase.Snaps[2] : a.QuietBase.AfterPlay;

        var played = a.Quiet.AfterPlay.Key != a.QuietBase.AfterPlay.Key || a.Quiet.Key != a.QuietBase.Key;
        if (!played && a.Strike.Key == a.StrikeBase.Key)
        {
            yield return new Finding("UNUSED", a.Id,
                "nothing differed from not playing it, in either fight, over four rounds (unplayable here, or no effect)", repro);
            yield break;
        }

        string Tag(string severity) => conditional && severity == "BUG" ? "CONDITION" : severity;

        var dealt = Deal.Matches(text).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)
            * (m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 1)).ToList();
        if (dealt.Count > 0)
        {
            var seen = (baseAfter.EnemyHp - after.EnemyHp) + Math.Max(0, baseAfter.EnemyBlock - after.EnemyBlock);
            var promised = dealt.Sum();
            if (seen < dealt[0] || (!conditional && seen != promised))
                yield return new Finding(Tag("BUG"), a.Id, $"text deals {promised}, the dummy lost {seen}", repro);
        }

        var blocks = Block.Matches(text).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
        if (blocks.Count > 0)
        {
            var seen = after.HeroBlock - baseAfter.HeroBlock;
            if (seen < blocks[0] || (!conditional && seen != blocks.Sum()))
                yield return new Finding(Tag("BUG"), a.Id, $"text gains {blocks.Sum()} Block, the hero gained {seen}", repro);
        }

        foreach (Match m in Apply.Matches(text))
        {
            var n = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var name = m.Groups[3].Value.Trim();
            if (name is "Block" or "Energy")
                continue;
            var status = game.Statuses.FirstOrDefault(s => string.Equals(s.NameKey, name, StringComparison.OrdinalIgnoreCase))?.Id;
            if (status is null)
                continue;
            var onHero = m.Groups[1].Value == "Gain" || m.Value.Contains("to you", StringComparison.Ordinal);
            int Stacks(Snap s) => (onHero ? s.HeroStatuses : s.EnemyStatuses).GetValueOrDefault(status);
            var seen = Stacks(after) - Stacks(baseAfter);
            // Seal converts at three: three or more laid may show as fewer Seal and a Ratified.
            var ratified = status == "seal" && after.EnemyStatuses.GetValueOrDefault("ratified") > baseAfter.EnemyStatuses.GetValueOrDefault("ratified");
            if (seen != n && !(ratified && seen >= n - 3) && !(onHero && seen > 0 && conditional))
                yield return new Finding(Tag("BUG"), a.Id, $"text {m.Groups[1].Value.ToLowerInvariant()}s {n} {name}, {(onHero ? "the hero" : "the dummy")} got {seen}", repro);
        }

        foreach (Match m in DrawN.Matches(text))
        {
            var n = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var drawn = (a.Quiet.AfterPlay.Hand - a.QuietBase.AfterPlay.Hand) + 1;
            if (a.QuietBase.AfterPlay.Draw >= n && drawn < n)
                yield return new Finding(Tag("BUG"), a.Id, $"text draws {n}, the hand grew by {drawn}", repro);
        }

        foreach (Match m in EnergyN.Matches(text))
        {
            var n = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var gained = a.Quiet.AfterPlay.Energy - a.QuietBase.AfterPlay.Energy + a.Cost;
            if (!queued && gained < n)
                yield return new Finding(Tag("BUG"), a.Id, $"text gains {n} Energy, the hero gained {gained}", repro);
        }

        if (text.Contains("Exhaust.", StringComparison.Ordinal) && !queued
            && a.Quiet.AfterPlay.Exhaust <= a.QuietBase.AfterPlay.Exhaust)
            yield return new Finding(Tag("BUG"), a.Id, "text says Exhaust, the card did not go to the exhaust pile", repro);

        // WHAT IT COSTS ME that the text does not say: health lost to a dummy that never attacks, or a debuff the
        // hero carries at the end that it would not have carried.
        var selfHarm = a.QuietBase.End.HeroHp - a.Quiet.End.HeroHp;
        var mentionsCost = Regex.IsMatch(text, @"\b(lose|Lose|take|Take|pay|Pay|costs|HP)\b");
        if (selfHarm > 0 && !mentionsCost)
            yield return new Finding("SIDE", a.Id, $"the hero lost {selfHarm} HP to its own card against a dummy that never attacks", repro);
        var debuffs = HeroDebuffs(game, a.Quiet.End) - HeroDebuffs(game, a.QuietBase.End);
        // A debuff the text NAMES is not a hidden cost ("…, gain 8 Lien" — Mortgaged Aegis, flagged by the first run).
        var named = GainedHeroDebuffs(game, a.Quiet.End, a.QuietBase.End)
            .Any(id => game.Statuses.FirstOrDefault(d => d.Id == id)?.NameKey is { } name
                && text.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (debuffs > 0 && !mentionsCost && !named && !Regex.IsMatch(text, @"\b(Paperwork|Doubt|Lien|Citation|Blood Ink|Fatigue|Panic)\b.*\byou\b"))
            yield return new Finding("SIDE", a.Id, $"the hero ends four rounds carrying {debuffs} more debuff stacks, which the text does not mention", repro);

        // ⚠ WEAK ONLY WHERE THE WORTH IS MEASURED. Value here is damage + health kept over four rounds, and nothing
        // else: energy gained, cards drawn, a Rite's rule, a trigger waiting for other cards — all read as zero. The
        // first run called 141 cards weak, most of them for exactly that. Those are listed as UNMEASURED instead.
        var rite = (game.Cards.FirstOrDefault(c => c.Id == a.Id)?.Tags ?? []).Any(t => t.value == "rite");
        var unmeasured = rite || conditional
            || Regex.IsMatch(text, @"\b(Energy|Draw|draw|Retain|Archive|Queue|Queued|card|cards|Whenever|whenever|first time|next|cost|Junk|Ward Wax)\b");
        if (a.PerEnergy < starterFloor && a.Value >= 0 && unmeasured)
            yield return new Finding("UNMEASURED", a.Id,
                $"worth {a.Value} over four rounds on damage + health alone — its value is in what this cannot count "
                + (rite ? "(a Rite's rule)" : "(energy, draw, a condition or a trigger)"), repro);
        else if (a.PerEnergy < starterFloor && a.Value >= 0)
            yield return new Finding("WEAK", a.Id,
                $"four rounds were worth {a.Value} (damage {a.DamageDone} + health kept {a.HealthKept}) for {a.Cost} energy — "
                + $"{a.PerEnergy:0.0}/energy, below every starter card ({starterFloor:0.0})", repro);
    }

    private static IEnumerable<string> GainedHeroDebuffs(RunBlueprint game, Snap with, Snap without) =>
        with.HeroStatuses
            .Where(s => s.Value > without.HeroStatuses.GetValueOrDefault(s.Key)
                && game.Statuses.FirstOrDefault(d => d.Id == s.Key)?.Polarity == StatusPolarity.Debuff)
            .Select(s => s.Key);

    private static int HeroDebuffs(RunBlueprint game, Snap snap) =>
        snap.HeroStatuses.Where(s => game.Statuses.FirstOrDefault(d => d.Id == s.Key)?.Polarity == StatusPolarity.Debuff)
            .Sum(s => s.Value);

    // ── relics ────────────────────────────────────────────────────────────────────────────────────────────

    public sealed record RelicAudit(string Id, string Text, Trace Quiet, Trace QuietBase, Trace Strike, Trace StrikeBase,
        Trace WithCompanion, Trace CompanionOnly);

    public static RelicAudit AuditRelic(RunBlueprint game, string id)
    {
        var deck = game.Start.Deck.Select(c => c.value).ToList();
        var text = game.Presentation.Relics.GetValueOrDefault(id)?.FlavorText ?? "";
        var quiet = Dummy(game, strikes: false, energy: 3);
        var strike = Dummy(game, strikes: true, energy: 3);
        Trace Run(EncounterDefinition d, IReadOnlyList<string>? relics) => Fight(game, d, deck, relics, Greedily, Greedily);
        return new RelicAudit(id, text,
            Run(quiet, [id]), Run(quiet, null), Run(strike, [id]), Run(strike, null),
            Run(quiet, [Companion, id]), Run(quiet, [Companion]));
    }

    public static IEnumerable<Finding> JudgeRelic(RunBlueprint game, RelicAudit a)
    {
        var repro = $"--audit --audit-only {a.Id}";
        if ((a.Quiet.Error ?? a.Strike.Error ?? a.WithCompanion.Error) is { } broke)
        {
            yield return new Finding("BUG", a.Id, $"the fight broke: {broke}", repro);
            yield break;
        }
        var mentionsCost = Regex.IsMatch(a.Text, @"\b(lose|Lose|take|Take|pay|Pay|cost|costs|HP|Burdened|curse|Curse)\b");
        foreach (var (label, with, without) in new[]
        {
            ("alone", a.Quiet, a.QuietBase), ("beside Petitioner's Token", a.WithCompanion, a.CompanionOnly),
        })
        {
            var harm = without.End.HeroHp - with.End.HeroHp;
            if (harm > 0 && !mentionsCost)
                yield return new Finding("BUG", a.Id, $"{label}: the hero lost {harm} HP to a dummy that never attacks", repro);
            var debuffs = with.Snaps.Max(s => HeroDebuffs(game, s)) - without.Snaps.Max(s => HeroDebuffs(game, s));
            if (debuffs > 0 && !mentionsCost)
                yield return new Finding("BUG", a.Id, $"{label}: the hero carried {debuffs} more debuff stacks than without the relic", repro);
        }
        if (a.Quiet.Key == a.QuietBase.Key && a.Strike.Key == a.StrikeBase.Key)
            yield return new Finding("UNUSED", a.Id,
                "four plain rounds against either dummy went exactly as without it (a condition never met, or no effect)", repro);
    }

    // ── synergies ─────────────────────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<(string A, string B, string Why)> Pairs =
    [
        ("waxing_authority", "conditional_approval", "Seal towards a Ratify"),
        ("notarial_press", "waxing_authority", "Seal towards a Ratify"),
        ("strong_binder", "dubious_authority", "Doubt consumed → Paperwork"),
        ("form_of_ill_intent", "dubious_authority", "Doubt consumed → Paperwork"),
        ("permit_a38", "inkblot_verdict", "Paperwork payoff"),
        ("secure_misfiling", "certified_kindling", "Junk made → Junk archived"),
        ("waxen_surety", "tallow_judgment", "Ward Wax made → Ward Wax consumed"),
        ("sealed_mantle", "tallow_judgment", "Ward Wax made → Ward Wax consumed"),
        ("deferred_hex", "protective_adjournment", "two Queued cards"),
    ];

    public sealed record PairAudit(string A, string B, string Why, int Both, int OnlyA, int OnlyB, string? Error);

    // What a pair's two fights looked like, round by round — the reproduction behind a synergy line.
    public static IEnumerable<string> TracePair(RunBlueprint game, string a, string b)
    {
        var deck = new[] { a, b, a, b, a, b };
        foreach (var strikes in new[] { false, true })
            foreach (var cards in new[] { new[] { a, b }, new[] { a }, new[] { b }, Array.Empty<string>() })
            {
                var t = Fight(game, Dummy(game, strikes, 6), deck, null, Plays(cards));
                yield return $"{(strikes ? "striker" : "quiet  ")} {(cards.Length == 0 ? "(nothing)" : string.Join("+", cards)),-44} "
                    + string.Join("  →  ", t.Snaps.Select(x => $"hero {x.HeroHp}/b{x.HeroBlock} hand {x.Hand} draw {x.Draw} exh {x.Exhaust} enemy {x.EnemyHp} "
                        + $"[{string.Join(",", x.EnemyStatuses.Select(kv => $"{kv.Key}={kv.Value}"))}]"))
                    + (t.Error is { } e ? $"  BROKE {e}" : "");
            }
    }

    public static PairAudit AuditPair(RunBlueprint game, string a, string b)
    {
        var deck = new[] { a, b, a, b, a, b };
        int Value(params string[] cards)
        {
            var q = Fight(game, Dummy(game, false, 6), deck, null, Plays(cards));
            var qb = Fight(game, Dummy(game, false, 6), deck, null, (_, _, _) => { });
            var s = Fight(game, Dummy(game, true, 6), deck, null, Plays(cards));
            var sb = Fight(game, Dummy(game, true, 6), deck, null, (_, _, _) => { });
            if ((q.Error ?? s.Error) is { } e)
                throw new InvalidOperationException(e);
            return Math.Max(qb.End.EnemyHp - q.End.EnemyHp, sb.End.EnemyHp - s.End.EnemyHp) + (s.End.HeroHp - sb.End.HeroHp);
        }
        try
        {
            return new PairAudit(a, b, "", Value(a, b), Value(a), Value(b), null);
        }
        catch (Exception ex)
        {
            return new PairAudit(a, b, "", 0, 0, 0, ex.Message);
        }
    }

    // ── the run ───────────────────────────────────────────────────────────────────────────────────────────

    public static int Run(RunBlueprint game, IReadOnlyList<string>? only, int jobs, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        // `--audit-only a&b` traces that pair round by round instead of auditing. ⚠ Not '+': an upgraded card's
        // id ends in '+', and `--audit-only smudged_index+` was read as a pair with an empty second card.
        if (only is { Count: 1 } one && one[0].Contains('&', StringComparison.Ordinal))
        {
            var parts = one[0].Split('&');
            foreach (var line in TracePair(game, parts[0], parts[1]))
                say(line);
            return 0;
        }
        var cards = Cards.FinalCards.All()
            .Where(c => c.Rarity is not "junk" && !(c.Tags ?? []).Contains("unplayable"))
            .Select(c => c.Id).Distinct().Where(id => only is null || only.Contains(id)).ToList();
        // A COMBAT relic is one whose programs answer a fight: its rule is handed over when a combat room is
        // entered (node.isCombat), or it carries combat rules of its own. The rest act between rooms (gold,
        // shops, rests) and cannot be judged in a fight — they are listed, not audited here.
        using var exported = System.Text.Json.JsonDocument.Parse(RunJson.ToJson(game, RunJson.CreateOptions(indented: false)));
        var combatRelics = exported.RootElement.GetProperty("Relics").EnumerateArray()
            .Where(r => r.GetRawText().Contains("isCombat", StringComparison.Ordinal)
                || (r.TryGetProperty("CombatRules", out var rules) && rules.GetArrayLength() > 0))
            .Select(r => r.GetProperty("Id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var relics = game.Relics.Select(r => r.Id).Where(combatRelics.Contains)
            .Where(id => only is null || only.Contains(id)).ToList();
        var outOfCombat = game.Relics.Count - combatRelics.Count;
        var pairs = Pairs.Where(p => only is null || only.Contains(p.A) || only.Contains(p.B))
            .Where(p => game.Cards.Any(c => c.Id == p.A) && game.Cards.Any(c => c.Id == p.B)).ToList();

        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "bnb-balance", "audit");
        Directory.CreateDirectory(dir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        var csvPath = Path.Combine(dir, $"{stamp}-audit.csv");
        var mdPath = Path.Combine(dir, $"{stamp}-audit.md");

        // THE ESTIMATE FIRST (BALANCE_PLAN rule 1): time one card, then say what the whole will cost.
        var clock = Stopwatch.StartNew();
        var starterAudits = Starters.Where(s => game.Cards.Any(c => c.Id == s)).Select(s => AuditCard(game, s)).ToList();
        var perCard = clock.Elapsed.TotalSeconds / Math.Max(1, starterAudits.Count);
        var workers = jobs > 0 ? jobs : Environment.ProcessorCount;
        var estimate = (cards.Count * perCard + relics.Count * perCard * 1.5 + pairs.Count * perCard * 3) / workers;
        say($"audit: {cards.Count} cards, {relics.Count} combat relics, {pairs.Count} synergy pairs · {Rounds} rounds each · "
            + $"~{perCard:0.00} s per card · estimate {estimate / 60:0.0} min on {workers} workers");
        var starterFloor = starterAudits.Min(s => s.PerEnergy);
        say($"  starter floor: {string.Join(", ", starterAudits.Select(s => $"{s.Id} {s.PerEnergy:0.0}/E"))}");

        var findings = new ConcurrentBag<Finding>();
        var rows = new ConcurrentBag<string>();
        var sink = new object();
        using var csv = new StreamWriter(csvPath) { AutoFlush = true };
        csv.WriteLine("kind,id,cost,value4,damage4,healthkept4,per_energy,severity,finding");
        void Write(string line)
        {
            lock (sink)
                csv.WriteLine(line);
        }
        static string Q(string s) => "\"" + s.Replace("\"", "'", StringComparison.Ordinal) + "\"";

        var options = new ParallelOptions { MaxDegreeOfParallelism = workers };
        var done = 0;
        Parallel.ForEach(cards, options, id =>
        {
            var audit = AuditCard(game, id);
            var found = Judge(game, audit, starterFloor).ToList();
            foreach (var f in found)
                findings.Add(f);
            Write(string.Join(",", "card", id, audit.Cost, audit.Value, audit.DamageDone, audit.HealthKept,
                audit.PerEnergy.ToString("0.0", CultureInfo.InvariantCulture),
                found.Count == 0 ? "OK" : string.Join("+", found.Select(f => f.Severity).Distinct()),
                Q(string.Join(" | ", found.Select(f => f.What)))));
            if (Interlocked.Increment(ref done) % 50 == 0)
                say($"  … {done}/{cards.Count} cards");
        });
        Parallel.ForEach(relics, options, id =>
        {
            var audit = AuditRelic(game, id);
            var found = JudgeRelic(game, audit).ToList();
            foreach (var f in found)
                findings.Add(f);
            var dmg = audit.QuietBase.End.EnemyHp - audit.Quiet.End.EnemyHp;
            var kept = audit.Strike.End.HeroHp - audit.StrikeBase.End.HeroHp;
            Write(string.Join(",", "relic", id, 0, dmg + kept, dmg, kept, "",
                found.Count == 0 ? "OK" : string.Join("+", found.Select(f => f.Severity).Distinct()),
                Q(string.Join(" | ", found.Select(f => f.What)))));
        });
        var pairResults = new ConcurrentBag<PairAudit>();
        Parallel.ForEach(pairs, options, p =>
        {
            var r = AuditPair(game, p.A, p.B) with { Why = p.Why };
            pairResults.Add(r);
            // A pair that is on the list BECAUSE it is meant to combine, and does nothing more together than
            // apart: the payoff never came.
            if (r.Error is null && r.Both <= r.OnlyA + r.OnlyB)
                findings.Add(new Finding("SYNERGY-MISSING", $"{p.A} + {p.B}",
                    $"{p.Why}: together {r.Both}, apart {r.OnlyA} + {r.OnlyB} — the payoff did not happen in four rounds",
                    $"--audit --audit-only '{p.A}&{p.B}'"));
            Write(string.Join(",", "pair", $"{p.A}+{p.B}", "", r.Both, "", "", "",
                r.Error is null ? "SYNERGY" : "BUG", Q(r.Error ?? $"together {r.Both} vs {r.OnlyA} + {r.OnlyB} apart")));
        });

        // ── the report, most severe first ──
        var order = new[] { "BUG", "SYNERGY-MISSING", "SIDE", "UNUSED", "CONDITION", "WEAK", "UNMEASURED" };
        var md = new StringBuilder();
        md.AppendLine(CultureInfo.InvariantCulture, $"# Audit {stamp} — {cards.Count} cards, {relics.Count} combat relics, {pairs.Count} pairs, {Rounds} rounds");
        md.AppendLine();
        md.AppendLine(CultureInfo.InvariantCulture, $"Wall clock {clock.Elapsed.TotalMinutes:0.0} min. Reproduce one line: `dotnet run --project Converter -- <repro>`. CSV: `{Path.GetFileName(csvPath)}`.");
        md.AppendLine();
        md.AppendLine("Method: each card played once on turn 1 (deck: eight copies and four fillers), then four rounds, against the same fight without it (seed 11), "
            + "against a quiet dummy (does nothing, 999 HP) and a striker (10 damage a turn). Relics: a plain greedy player with "
            + "the starter deck for four rounds, with and without the relic, alone and beside Petitioner's Token. "
            + "BUG = text and effect disagree; SIDE = the card costs the hero something its text does not say; UNUSED = no "
            + "difference at all in four rounds; CONDITION = a conditional number that did not show (the dummy may not meet "
            + "the condition — read, don't fix); WEAK = worth less per energy over four rounds than every starter card, for a "
            + "card whose whole worth is damage, block or a status; UNMEASURED = below the starters on damage + health alone, "
            + "but its worth is energy, draw, a Rite's rule or a trigger this audit does not count — judge it by hand.");
        md.AppendLine();
        md.AppendLine(CultureInfo.InvariantCulture, $"Relics that act only between rooms ({outOfCombat}) are not judged in a fight.");
        md.AppendLine(CultureInfo.InvariantCulture, $"Starter floor: {string.Join(", ", starterAudits.Select(s => $"{s.Id} {s.PerEnergy:0.0}/E"))}.");
        foreach (var severity in order)
        {
            var these = findings.Where(f => f.Severity == severity).OrderBy(f => f.Subject, StringComparer.Ordinal).ToList();
            md.AppendLine();
            md.AppendLine(CultureInfo.InvariantCulture, $"## {severity} ({these.Count})");
            foreach (var f in these)
                md.AppendLine(CultureInfo.InvariantCulture, $"- **{f.Subject}** — {f.What} · `{f.Repro}`");
        }
        md.AppendLine();
        md.AppendLine("## Synergy pairs (value over four rounds: together vs each alone)");
        foreach (var p in pairResults.OrderBy(p => p.A, StringComparer.Ordinal))
            md.AppendLine(p.Error is { } e
                ? $"- **{p.A} + {p.B}** ({p.Why}) — BROKE: {e}"
                : $"- **{p.A} + {p.B}** ({p.Why}) — together {p.Both}, apart {p.OnlyA} + {p.OnlyB} = {p.OnlyA + p.OnlyB} → "
                  + $"{(p.Both > p.OnlyA + p.OnlyB ? $"synergy +{p.Both - p.OnlyA - p.OnlyB}" : p.Both == p.OnlyA + p.OnlyB ? "no synergy" : $"WORSE together ({p.Both - p.OnlyA - p.OnlyB})")}");
        File.WriteAllText(mdPath, md.ToString());

        say($"\naudit done in {clock.Elapsed.TotalMinutes:0.0} min → {mdPath}");
        foreach (var severity in order)
            say($"  {severity,-10} {findings.Count(f => f.Severity == severity)}");
        return 0;
    }
}
