using System.Text;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Scripting;
using BnbContent.Converter.Relics;

namespace BnbContent.Converter.Playtest;

// THE RELIC AUDIT IN THE FIGHT (user, 2026-11-19: "kampfregel relikte muessen auch geprueft werden"). The run audit
// (RunAudit) sees what a relic does between rooms; this sees what it does inside a fight.
//
// For every relic: a handful of fights (Act I and II, normal, crowd and elite), played by a random player with the
// relic's own character and a deck of that character's starter cards plus cards from its pool, every trace event
// tapped (MechanicAudit's tap, on a fork, so no replay). Then the SAME fights with the same seeds without the relic.
// Per relic it writes:
//
//   • every trigger of the relic's rule: how often it was asked, fired, or rejected by its filter
//   • every consequence of an effect the relic's rule queued (damage, block, heal, status, energy, cards), with a
//     sample of the lines
//   • every damage modifier step named after the relic
//   • the difference to the fights without it: damage dealt and taken, block gained, cards played, turns, energy
//     spent — which is where a passive rule (a cost, a draw, a scaling) shows when it queues nothing
//
//   --relic-combat-audit <dir> [--audit-relics a,b] [--audit-fights 6] [--jobs n] [--seed s]
public static class RelicCombatAudit
{
    private sealed class Tap(ICombatTraceListener? inner) : ICombatTraceListener
    {
        public List<CombatTraceEvent> Events { get; } = [];

        public void OnTrace(CombatTraceEvent evt)
        {
            Events.Add(evt);
            inner?.OnTrace(evt);
        }
    }

    // What one fight showed, with and without the relic.
    private sealed class Tally
    {
        public int Fights, Won, Turns, CardsPlayed, DamageDealt, DamageTaken, BlockGained, EnergySpent, Healed, ActionsUsed;
        // Hand size and Energy as each of the first three hero turns begins, summed over the fights: a draw or an
        // Energy rule shows here even when it queues no effect of its own.
        public readonly int[] Hand = new int[3], Energy = new int[3], Seen = new int[3];
        public readonly Dictionary<string, int[]> Triggers = new(StringComparer.Ordinal); // asked, fired, rejected
        public readonly Dictionary<string, int> Consequences = new(StringComparer.Ordinal);
        public readonly Dictionary<string, int> Modifiers = new(StringComparer.Ordinal);
        public readonly Dictionary<string, int> Samples = new(StringComparer.Ordinal);
        // The table as the fight hands it over (the opening draw has already happened): statuses with stacks, block,
        // energy and hand size per side, summed over the fights. "At the start of each combat" lives only here.
        public readonly Dictionary<string, int> Opening = new(StringComparer.Ordinal);
    }

    public static int Run(RunBlueprint game, string outDir, IReadOnlyList<string>? only, int fights, int seed, int jobs)
    {
        Directory.CreateDirectory(outDir);
        var relics = FinalRelics.All()
            .Where(r => !r.Id.StartsWith("mod_", StringComparison.Ordinal))
            .Where(r => only is not { Count: > 0 } || only.Contains(r.Id))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();
        var encounters = Encounters(game);
        Console.WriteLine($"relic combat audit: {relics.Count} relic(s), {fights} fight(s) each over "
            + $"{encounters.Count} encounters -> {outDir}");

        var pages = new string[relics.Count];
        var done = 0;
        Parallel.For(0, relics.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, jobs) }, i =>
        {
            var relic = relics[i];
            var with = new Tally();
            var without = new Tally();
            var errors = new List<string>();
            for (var f = 0; f < fights; f++)
            {
                // A general relic is worn by every character in turn: its trigger may want another's cards.
                var character = relic.Eligibility == RelicAuthoring.Eligibility.General
                    ? Everyone[f % Everyone.Length]
                    : Character(relic.Eligibility);
                var encounter = encounters[(f + i) % encounters.Count];
                var deck = Deck(game, character, seed + f, relic.Text);
                try
                {
                    // The relic's own id and its combat rule's (an Apprentice rule is "oil_lamp_rule", not the relic's id).
                    string[] owners = relic.CombatRule?.Id is { } rule && rule != relic.Id ? [relic.Id, rule] : [relic.Id];
                    Fight(game, encounter, deck, character, [relic.Id], seed + f, owners, with);
                    Fight(game, encounter, deck, character, [], seed + f, owners, without);
                }
                catch (Exception e)
                {
                    errors.Add($"{encounter.Id.Value}: {e.GetType().Name}: {e.Message}");
                }
            }
            pages[i] = Page(relic, relic.Eligibility == RelicAuthoring.Eligibility.General ? "everyone in turn" : Character(relic.Eligibility),
                with, without, errors);
            Console.WriteLine($"  {Interlocked.Increment(ref done)}/{relics.Count} {relic.Id}");
        });

        File.WriteAllText(Path.Combine(outDir, "COMBAT_SUMMARY.md"),
            $"# Relic combat audit — {DateTime.Now:yyyy-MM-dd HH:mm}, {fights} fights per relic, seed {seed}\n\n"
            + string.Join("\n", pages));
        Console.WriteLine($"relic combat audit: {Path.Combine(outDir, "COMBAT_SUMMARY.md")}");
        return 0;
    }

    private static readonly string[] Everyone =
        [Lieutenant.LieutenantCharacter.Id, ConversionPools.BureaucratId, Witch.WitchCharacter.Id, Apprentice.ApprenticeCharacter.Id];

    private static string Character(RelicAuthoring.Eligibility eligibility) => eligibility switch
    {
        RelicAuthoring.Eligibility.Bureaucrat => ConversionPools.BureaucratId,
        RelicAuthoring.Eligibility.HedgeWitch => Witch.WitchCharacter.Id,
        RelicAuthoring.Eligibility.Apprentice => Apprentice.ApprenticeCharacter.Id,
        _ => Lieutenant.LieutenantCharacter.Id,
    };

    // Fights of every act and role but the boss, in a fixed order.
    private static List<EncounterDefinition> Encounters(RunBlueprint game)
    {
        // One normal, one crowd and one elite of every act I-IV: a relic that answers an act's own vocabulary
        // (Overdue, Claims, Burdened, Weighed) meets it only in that act's fights.
        var wanted = new[]
        {
            "city_easy_01", "city_normal_seal_08", "city_elite_appeal_01",
            "archives_returns_01", "archives_returns_duo_01", "archives_elite_after_hours_return_bell",
            "green_docket_road_01", "green_docket_road_duo_01", "green_docket_elite_grandmother_web",
            "labyrinth_stelae_01", "labyrinth_stelae_duo_01", "labyrinth_elite_scarab_host_of_the_sealed_granary",
        };
        var found = wanted.Select(id => game.Encounters.FirstOrDefault(e => e.Id.Value == id)).OfType<EncounterDefinition>().ToList();
        return found.Count > 0 ? found : [.. game.Encounters.Take(6)];
    }

    // The character's starter deck and twelve cards from its own pool and the open pool, drawn per seed.
    // ⚠ And the cards the relic's text is ABOUT: a relic that answers "Liable" or "a Form" does nothing in a deck
    // without one, so up to six cards whose text names a capitalised word of the relic's text are added first.
    private static readonly HashSet<string> Plain = new(StringComparer.Ordinal)
    {
        "The", "A", "An", "At", "After", "When", "Whenever", "Every", "Each", "Your", "You", "If", "Once", "Gain",
        "Deal", "Draw", "Heal", "Energy", "Block", "HP", "Max", "Gold", "Elite", "Boss", "Act", "It", "Its", "On",
        "Start", "First", "Then", "While", "Any", "All", "One", "Two", "Three", "Lose", "Apply", "Play", "Combat",
    };

    private static List<string> Deck(RunBlueprint game, string character, int seed, string relicText)
    {
        var words = System.Text.RegularExpressions.Regex.Matches(relicText, @"\b[A-Z][a-z]+\b")
            .Select(m => m.Value).Where(w => !Plain.Contains(w)).Distinct().ToList();
        var start = game.ResolveStart(character);
        var exclusive = game.Characters.SelectMany(c => c.Exclusive ?? []).ToHashSet(StringComparer.Ordinal);
        var own = game.Characters.FirstOrDefault(c => c.Id == character)?.Exclusive ?? [];
        var cards = game.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        bool Playable(string id) => cards.TryGetValue(id, out var c) && !c.Tags.Any(t => t.value is "curse" or "status" or "junk");
        var rng = new Random(seed);
        var mine = own.Where(Playable).OrderBy(_ => rng.Next()).Take(9);
        var open = game.Cards.Select(c => c.Id).Where(id => !exclusive.Contains(id) && Playable(id)
            && game.Presentation.Cards.ContainsKey(id)).OrderBy(_ => rng.Next()).Take(3);
        var allowed = new HashSet<string>(own, StringComparer.Ordinal);
        var about = words.Count == 0 ? [] : game.Cards
            .Where(c => (allowed.Contains(c.Id) || !exclusive.Contains(c.Id)) && Playable(c.Id)
                && words.Any(w => (c.DescriptionKey ?? "").Contains(w, StringComparison.Ordinal)))
            .Select(c => c.Id).OrderBy(_ => rng.Next()).Take(6).ToList();
        return [.. start.Deck.Select(d => d.value), .. about, .. mine, .. open];
    }

    private static void Fight(
        RunBlueprint game, EncounterDefinition encounter, IReadOnlyList<string> deck, string character,
        IReadOnlyList<string> relics, int seed, IReadOnlyList<string> relicId, Tally tally)
    {
        var ring = SparringRing.OneFight(game, encounter, deck, health: 300, maxHealth: 300, relics: relics,
            character: character);
        using var play = new RunPlayback(() => { });
        play.Start(ring, seed, interactive: true);
        while (play.Session is { IsAwaitingInterlude: true } session)
            session.Continue();
        // A question the fight asks before the first card (a relic's "at every opening hand, choose") is answered
        // as a player would, or the fork would be taken with it open and never answered.
        var answerer = new Random(seed);
        for (var asked = 0; asked < 10 && play.CombatDriver is { } driver && Greedy.Answer(driver, answerer); asked++)
        {
        }
        if (play.CombatDriver?.Current is not { } start)
            throw new InvalidOperationException("no fight");
        var combat = start.Fork();
        void Note(string key, int amount) => tally.Opening[key] = tally.Opening.GetValueOrDefault(key) + amount;
        foreach (var body in combat.State.Combatants.Where(c => c.IsAlive))
        {
            var side = body.Id == combat.HeroId ? "hero" : "enemy";
            foreach (var status in body.Statuses)
                Note($"{side} {status.DefinitionId.value}", Math.Max(1, status.Stacks));
        }
        Note("hero block", combat.HeroGuard);
        Note("hero energy", combat.HeroEnergy);
        Note("hero hand", combat.Hand.Count);
        var tap = new Tap(combat.State.TraceListener);
        combat.State.TraceListener = tap;
        var rng = new Random(seed);

        var turns = 0;
        while (!combat.IsOver && turns < 30)
        {
            while (!combat.IsOver && !combat.IsHeroTurn)
                combat.EndTurn();
            if (combat.IsOver)
                break;
            if (turns < 3)
            {
                tally.Hand[turns] += combat.Hand.Count;
                tally.Energy[turns] += combat.HeroEnergy;
                tally.Seen[turns]++;
            }
            turns++;
            // The hero's own actions (the Apprentice's wand: inscribe, cast, burn out; the Witch's cauldron), in a
            // random order, each tried once a turn before the cards and once after: a relic written for them
            // answers nothing in a fight played with cards alone.
            void Act()
            {
                foreach (var action in combat.Actions.OrderBy(_ => rng.Next()).ToList())
                {
                    if (combat.IsOver || !combat.IsHeroTurn)
                        return;
                    var target = Greedy.Enemy(combat);
                    if (!combat.CanUse(action, target) && !combat.CanUse(action, null))
                        continue;
                    var before = combat.Steps.Count;
                    try
                    {
                        combat.UseAction(action, combat.CanUse(action, target) ? target : null);
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                    if (!Greedy.Refused(combat, before))
                        tally.ActionsUsed++;
                }
            }
            Act();
            var refused = new HashSet<CardInstanceId>();
            for (var plays = 0; plays < 25 && !combat.IsOver && combat.IsHeroTurn; plays++)
            {
                var hand = combat.Hand.Where(c => !refused.Contains(c.Id) && combat.CanPlay(c.Id)
                    && !Greedy.Borrows(combat, c) && !Greedy.BurnsTheDeck(combat, c)).ToList();
                if (hand.Count == 0)
                    break;
                var card = hand[rng.Next(hand.Count)];
                var needs = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var n) && n;
                var before = combat.Steps.Count;
                combat.PlayCard(card.Id, needs ? Greedy.Enemy(combat) : null);
                if (Greedy.Refused(combat, before))
                    refused.Add(card.Id);
                else
                    tally.CardsPlayed++;
            }
            Act();
            if (!combat.IsOver && combat.IsHeroTurn)
                combat.EndTurn();
        }
        tally.Fights++;
        tally.Turns += turns;
        if (combat.Result == CombatResult.Victory)
            tally.Won++;
        if (Environment.GetEnvironmentVariable("RCA_DUMP") is { Length: > 0 } dump && relics.Count > 0)
            File.AppendAllLines(dump, tap.Events.Select(e => e.GetType().Name + " " + System.Text.Json.JsonSerializer.Serialize(e, e.GetType())));
        Read(tap.Events, combat, relicId, tally);
    }

    // A trigger, modifier or status belongs to the relic when its id is the relic's or starts with it
    // ("<relic>_trigger3", "<relic>_latch").
    private static bool Owned(string id, IReadOnlyList<string> owners) =>
        owners.Any(relic => id == relic || id.StartsWith(relic + "_", StringComparison.Ordinal));

    private static string Owner(string triggerId)
    {
        var cut = triggerId.LastIndexOf("_trigger", StringComparison.Ordinal);
        return cut > 0 ? triggerId[..cut] : triggerId;
    }

    private static void Read(List<CombatTraceEvent> events, InteractiveCombat combat, IReadOnlyList<string> relic, Tally tally)
    {
        var hero = combat.HeroId;
        // ⚠ A chain id is the EVENT's chain, shared by every trigger that answered it; the queue resolves in order, so
        // a resolved effect belongs to the first request still waiting with its chain and type (MechanicAudit).
        var waiting = new List<(long Chain, string Type, string? Owner)>();
        var pending = new List<string>();
        foreach (var evt in events)
        {
            switch (evt)
            {
                case TriggerEvaluatedTraceEvent t when Owned(Owner(t.TriggerId), relic):
                {
                    var key = $"{t.TriggerId} on {t.EventType}";
                    var counts = tally.Triggers.TryGetValue(key, out var c) ? c : tally.Triggers[key] = new int[3];
                    counts[0]++;
                    if (t.Outcome == TriggerEvaluationOutcome.Fired)
                        counts[1]++;
                    else if (t.Outcome == TriggerEvaluationOutcome.SkippedFilterRejected)
                        counts[2]++;
                    break;
                }
                case EffectEnqueuedTraceEvent q:
                    waiting.Add((q.ChainId, q.RequestType, q.TriggerId));
                    break;
                case EffectResolvedTraceEvent r:
                    var at = waiting.FindIndex(w => w.Chain == r.ChainId && w.Type == r.RequestType);
                    string? by = null;
                    if (at >= 0)
                    {
                        by = waiting[at].Owner;
                        waiting.RemoveAt(at);
                    }
                    if (by is not null && Owned(Owner(by), relic))
                        foreach (var line in pending)
                        {
                            tally.Consequences[Shape(line)] = tally.Consequences.GetValueOrDefault(Shape(line)) + 1;
                            if (tally.Samples.Count < 12)
                                tally.Samples[line] = tally.Samples.GetValueOrDefault(line) + 1;
                        }
                    pending.Clear();
                    break;
                case DamageResolvedTraceEvent d:
                    if (d.TargetCombatantId == hero)
                        tally.DamageTaken += d.HealthLost;
                    else
                        tally.DamageDealt += d.HealthLost + d.BlockedAmount;
                    foreach (var m in d.ModifierSteps.Where(m => m.Before != m.After && Owned(m.ModifierId, relic)))
                    {
                        var key = $"{m.ModifierId} on {(d.TargetCombatantId == hero ? "hero" : "enemy")}";
                        tally.Modifiers[key] = tally.Modifiers.GetValueOrDefault(key) + (m.After - m.Before);
                    }
                    if (Line(d, hero) is { } dl)
                        pending.Add(dl);
                    break;
                case CardCostResolvedTraceEvent cost:
                    if (cost.ResourceId == StandardCombatIds.EnergyResource)
                        tally.EnergySpent += Math.Max(0, cost.FinalAmount);
                    foreach (var m in cost.ModifierSteps.Where(m => m.Before != m.After && Owned(m.ModifierId, relic)))
                    {
                        var key = $"{m.ModifierId} on the cost of {cost.CardId.value}";
                        tally.Modifiers[$"{m.ModifierId} on card costs"] =
                            tally.Modifiers.GetValueOrDefault($"{m.ModifierId} on card costs") + (m.After - m.Before);
                        if (tally.Samples.Count < 12)
                            tally.Samples[$"{key}: {m.Before} -> {m.After}"] = 1;
                    }
                    break;
                case BlockGainResolvedTraceEvent b:
                    if (b.TargetCombatantId == hero)
                        tally.BlockGained += Math.Max(0, b.BlockAfter - b.BlockBefore);
                    if (b.BlockAfter != b.BlockBefore)
                        pending.Add($"{Who(b.TargetCombatantId, hero)} block {b.BlockAfter - b.BlockBefore:+#;-#;0}");
                    break;
                case HealResolvedTraceEvent h:
                    if (h.TargetCombatantId == hero)
                        tally.Healed += h.HealedAmount;
                    if (h.HealedAmount > 0)
                        pending.Add($"{Who(h.TargetCombatantId, hero)} heal {h.HealedAmount}");
                    break;
                case StatusApplicationResolvedTraceEvent s:
                    pending.Add($"{Who(s.TargetCombatantId, hero)} status {s.StatusDefinitionId.value} "
                        + $"{(s.RequestedStacks != 0 ? s.RequestedStacks : s.RequestedDurationTurns):+#;-#;0} ({s.Outcome})");
                    break;
                case ResourceChangeResolvedTraceEvent rc when rc.AppliedDelta != 0 && rc.Kind != ResourceChangeKind.Refilled:
                    pending.Add($"{Who(rc.CombatantId, hero)} {rc.ResourceId.value} {rc.AppliedDelta:+#;-#;0}");
                    break;
                case MaxHealthChangeResolvedTraceEvent mh when mh.AppliedDelta != 0:
                    pending.Add($"{Who(mh.TargetCombatantId, hero)} max HP {mh.AppliedDelta:+#;-#;0}");
                    break;
                // Cards moving (drawn, created, exhausted, discarded) write no receipt of their own, only the combat
                // event they raise on the effect's chain — which is enough to say what the relic's effect did.
                case CombatEventEnqueuedTraceEvent ce when ce.EventType.Contains("Card", StringComparison.Ordinal)
                    && !ce.EventType.Contains("Played", StringComparison.Ordinal)
                    && !ce.EventType.Contains("Cost", StringComparison.Ordinal):
                    pending.Add("event " + ce.EventType.Replace("CombatEvent", "", StringComparison.Ordinal));
                    break;
            }
        }
    }

    private static string? Line(DamageResolvedTraceEvent d, CombatantId hero) =>
        d.AmountAfterModifiers > 0 || d.HealthLost > 0 ? $"{Who(d.TargetCombatantId, hero)} damage {d.AmountAfterModifiers}" : null;

    private static string Who(CombatantId id, CombatantId hero) => id == hero ? "hero" : "enemy";

    private static string Shape(string line) =>
        System.Text.RegularExpressions.Regex.Replace(line, @"[+-]?\d+", "#");

    private static string Page(RelicAuthoring.BnbRelic relic, string character, Tally with, Tally without, List<string> errors)
    {
        var page = new StringBuilder();
        page.AppendLine($"## {relic.Id} ({relic.Pool}, {relic.Eligibility}, played as {character})");
        page.AppendLine($"text: {relic.Text}");
        var kinds = new List<string>();
        if (relic.CombatRule is not null) kinds.Add("combat rule");
        if (relic.RunPrograms is { Count: > 0 }) kinds.Add("run programs");
        if (relic.Pickup is { Count: > 0 }) kinds.Add("pickup");
        if (relic.ShopPrices is { Count: > 0 } || relic.ShopStock is { Count: > 0 } || relic.ShopServices is { Count: > 0 }
            || relic.ShopCredit is { Count: > 0 } || relic.ShopDebt is { Count: > 0 }) kinds.Add("shop");
        if (relic.RewardRules is { Count: > 0 }) kinds.Add("rewards");
        page.AppendLine($"made of: {(kinds.Count == 0 ? "nothing" : string.Join(", ", kinds))}");
        foreach (var error in errors)
            page.AppendLine($"**ERROR** {error}");
        foreach (var (key, c) in with.Triggers.OrderBy(t => t.Key, StringComparer.Ordinal))
            page.AppendLine($"trigger {key}: asked {c[0]}, fired {c[1]}, filtered {c[2]}");
        if (with.Consequences.Count > 0)
            page.AppendLine("did: " + string.Join("; ", with.Consequences.OrderByDescending(c => c.Value).Take(8)
                .Select(c => $"{c.Key} x{c.Value}")));
        if (with.Samples.Count > 0)
            page.AppendLine("e.g.: " + string.Join("; ", with.Samples.Keys.Take(6)));
        foreach (var (key, sum) in with.Modifiers)
            page.AppendLine($"modifier {key}: {sum:+#;-#;0} in all");
        string Delta(string what, int a, int b) => a == b ? "" : $"{what} {a} vs {b}";
        var deltas = new[]
        {
            Delta("won", with.Won, without.Won), Delta("turns", with.Turns, without.Turns),
            Delta("cards played", with.CardsPlayed, without.CardsPlayed),
            Delta("damage dealt", with.DamageDealt, without.DamageDealt),
            Delta("damage taken", with.DamageTaken, without.DamageTaken),
            Delta("block", with.BlockGained, without.BlockGained), Delta("energy spent", with.EnergySpent, without.EnergySpent),
            Delta("healed", with.Healed, without.Healed), Delta("actions used", with.ActionsUsed, without.ActionsUsed),
        }.Where(d => d.Length > 0).ToList();
        var openingDiff = with.Opening.Keys.Union(without.Opening.Keys)
            .Where(k => with.Opening.GetValueOrDefault(k) != without.Opening.GetValueOrDefault(k))
            .OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => $"{k} {with.Opening.GetValueOrDefault(k)} vs {without.Opening.GetValueOrDefault(k)}").ToList();
        if (openingDiff.Count > 0)
            page.AppendLine($"opening table (summed over the fights): {string.Join(", ", openingDiff)}");
        string Opening(Tally t) => string.Join(" / ", Enumerable.Range(0, 3).Select(k =>
            t.Seen[k] == 0 ? "-" : $"{(double)t.Hand[k] / t.Seen[k]:0.#}c {(double)t.Energy[k] / t.Seen[k]:0.#}e"));
        var opening = Opening(with) == Opening(without) ? "" : $"; turns 1/2/3 start with {Opening(with)} vs {Opening(without)} (cards, energy)";
        page.AppendLine($"with vs without ({with.Fights} fights): {(deltas.Count == 0 ? "no difference at all" : string.Join(", ", deltas))}{opening}");
        return page.ToString();
    }
}
