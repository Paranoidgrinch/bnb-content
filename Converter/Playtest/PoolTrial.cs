using System.Text;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// THE GENERAL POOL TRIAL (2026-11-08): does every general card do what its text says, in real fights?
//
// Each card goes into the Lieutenant's starting deck three times (plus what it needs around it) and is played
// in two real Act-I fights — one against a single enemy, one against a pair. The driver is deliberately simple:
// it plays the card under test whenever it can, answers every choice with the first offer, fills the rest of
// the turn with whatever else it can afford, and ends the turn. Every play of the card under test is written
// down with what changed and the combat log lines it raised, so a human can read the card against its text.
// `--pool-trial <dir>` writes one file per axis and a summary of anything that broke.
public static class PoolTrial
{
    private const string Character = Lieutenant.LieutenantCharacter.Id;
    private const int TurnBudget = 12;

    // What a card needs around it to show what it does: something to discard, exhaust, copy or count.
    private static readonly Dictionary<string, string[]> Helpers = new(StringComparer.Ordinal)
    {
        ["tallow_stub"] = ["scattered_notes"],
        ["spiteful_draft"] = ["scattered_notes", "hearth_kindling"],
        ["ashen_receipt"] = ["scattered_notes"],
        ["smouldering_correspondence"] = ["scattered_notes"],
        ["besom_guard"] = ["burnt_offering"],
        ["hares_mark"] = ["overlooking"],
        ["lingering_malison"] = ["overlooking", "named_in_the_roll"],
        ["spreading_rumour"] = ["overlooking"],
        ["compounded_grievance"] = ["seed_of_misgiving", "named_in_the_roll"],
        ["beadles_cudgel"] = ["named_in_the_roll"],
        ["third_and_final_writ"] = ["overlooking"],
        ["tallow_judgment"] = ["waxen_surety"],
        ["tallow_reserve"] = ["barred_door"],
        ["pyre_of_old_records"] = ["burnt_offering", "stolen_hour"],
        ["corpse_candle"] = ["burnt_offering", "crossed_sigil"],
        ["salamander_in_the_grate"] = ["bone_meal", "burnt_offering", "stolen_hour"],
        ["last_ember"] = ["stolen_hour", "burnt_offering"],
        ["ashen_effigy"] = ["bone_meal", "stolen_hour", "burnt_offering"],
        ["sifted_ashes"] = ["burnt_offering", "stolen_hour"],
        ["cold_hearth"] = ["bone_meal"],
        ["raked_from_the_ashes"] = ["stolen_hour"],
        ["unburied"] = ["stolen_hour", "burnt_offering"],
        ["whispering_gallery"] = ["scattered_notes"],
        ["vigil_lamp"] = [],
        ["misers_coffer"] = [],
    };

    // Cards whose point is what is left at the turn's end: once played, the turn ends at once.
    private static readonly HashSet<string> Hoard = new(StringComparer.Ordinal) { "vigil_lamp", "misers_coffer" };

    // Cards that act at a turn's edge: the whole end of the turn is written down after they are played.
    private static readonly HashSet<string> WatchTurnEnd = new(StringComparer.Ordinal)
    {
        "vigil_lamp", "misers_coffer", "dog_eared_almanac", "read_by_firelight", "salamander_in_the_grate",
        "whispering_gallery", "rowan_over_the_door", "standing_stones", "errand_rook", "wick_burnt_short",
        "coin_under_the_tongue", "stay_of_proceedings", "false_signature",
    };

    private static readonly (string Title, Func<IReadOnlyList<CardAuthoring.BnbCard>> Cards)[] Axes =
    [
        ("energy", GeneralEnergy.All), ("draw", GeneralDraw.All), ("discard", GeneralDiscard.All),
        ("debuffs", GeneralDebuffs.All), ("defence", GeneralDefence.All), ("damage", GeneralDamage.All),
        ("exhausted", GeneralExhausted.All),
    ];

    public static int Run(RunBlueprint game, string outDir, Action<string> log)
    {
        Directory.CreateDirectory(outDir);
        var single = PickEncounter(game, enemies: 1);
        var pair = PickEncounter(game, enemies: 2);
        log($"pool trial: single = {single}, pair = {pair}, as the {Character}");

        var starter = game.ResolveStart(Character).Deck.Select(c => c.value).ToList();
        var broken = new List<string>();
        foreach (var (title, cards) in Axes)
        {
            var text = new StringBuilder();
            foreach (var card in cards())
            {
                var id = card.Id;
                var helpers = Helpers.GetValueOrDefault(id.TrimEnd('+'), []).Where(h => game.Cards.Any(c => c.Id == h));
                List<string> deck = [.. starter, id, id, id, .. helpers];
                text.AppendLine($"══ {card.Name} — {card.RulesText}");
                foreach (var encounter in new[] { single, pair })
                {
                    var probe = SparringRing.Authored(game, encounter);
                    var ring = SparringRing.OneFight(game, probe, deck, character: Character);
                    var lines = Fight(ring, id, seed: 7);
                    foreach (var line in lines)
                        text.AppendLine(line);
                    broken.AddRange(lines.Where(l => l.Contains("BROKE", StringComparison.Ordinal))
                        .Select(l => $"{id}: {l.Trim()}"));
                    if (!lines.Any(l => l.StartsWith("  ▶", StringComparison.Ordinal)))
                        broken.Add($"{id}: never played against {encounter}");
                }
                text.AppendLine();
            }
            File.WriteAllText(Path.Combine(outDir, $"{title}.txt"), text.ToString());
            log($"  {title}: written");
        }
        File.WriteAllLines(Path.Combine(outDir, "summary.txt"), broken.Count == 0 ? ["nothing broke"] : broken);
        log(broken.Count == 0 ? "nothing broke" : $"{broken.Count} problem(s) — see summary.txt");
        return broken.Count == 0 ? 0 : 1;
    }

    private static string PickEncounter(RunBlueprint game, int enemies) =>
        game.Encounters.First(e => e.Id.Value.StartsWith("city_normal", StringComparison.Ordinal)
            && e.Enemies.Count == enemies).Id.Value;

    private static List<string> Fight(RunBlueprint ring, string cardId, int seed)
    {
        var lines = new List<string>();
        using var play = new RunPlayback(() => { });
        play.Start(ring, seed, interactive: true);
        if (play.Error is { } startError)
            return [$"  BROKE at the start: {startError}"];
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();

        var plays = 0;
        for (var turn = 1; turn <= TurnBudget; turn++)
        {
            var driver = play.CombatDriver;
            if (driver?.Current is not { } combat || session.Error is not null)
                break;

            var unaffordable = new HashSet<CardInstanceId>();
            for (var guard = 0; guard < 12; guard++)
            {
                combat = driver.Current!;
                var hero = combat.State.GetCombatant(combat.HeroId);
                var enemy = combat.State.Combatants.FirstOrDefault(c => c.Id != combat.HeroId && c.Health.Current > 0);
                if (enemy is null)
                    break;
                var energy = hero.Resources[StandardCombatIds.EnergyResource].Current;
                var playable = combat.Hand.Where(c => combat.CanPlay(c.Id) && !unaffordable.Contains(c.Id)).ToList();
                var pick = playable.FirstOrDefault(c => c.DefinitionId.value == cardId)
                    ?? playable.FirstOrDefault();
                if (pick is null)
                    break;

                var tested = pick.DefinitionId.value == cardId;
                var before = Snapshot(combat);
                var logBefore = combat.State.CombatLog.Count;
                driver.PlayCard(pick.Id, enemy.Id);
                Answer(driver);
                if (session.Error is { } error)
                {
                    lines.Add($"  BROKE on turn {turn} playing {pick.DefinitionId.value}: {error}");
                    return lines;
                }
                if (driver.Current is not { } after)
                    break;
                // CanPlay does not ask about Energy: a card that could not be paid for simply did not happen.
                if (!after.State.CombatLog.Skip(logBefore).Any(e => e.Type == "CardPlayed"))
                {
                    unaffordable.Add(pick.Id);
                    continue;
                }
                if (tested)
                {
                    plays++;
                    lines.Add($"  ▶ turn {turn}: {Describe(before, Snapshot(after))}");
                    foreach (var entry in after.State.CombatLog.Skip(logBefore).Take(14))
                        lines.Add($"      [{entry.Type}] {entry.Message}");
                }
                if (tested && Hoard.Contains(cardId.TrimEnd('+')))
                    break;
                if (energy == 0 && !tested && pick.DefinitionId.value != cardId)
                    break;
            }

            if (driver.Current is null || session.Error is not null)
                break;
            var logAtEnd = driver.Current.State.CombatLog.Count;
            var beforeEnd = Snapshot(driver.Current);
            driver.EndTurn();
            if (session.Error is { } endError)
            {
                lines.Add($"  BROKE ending turn {turn}: {endError}");
                return lines;
            }
            if (driver.Current is { } next && plays > 0 && turn <= 3)
            {
                var watched = WatchTurnEnd.Contains(cardId.TrimEnd('+'));
                var notes = next.State.CombatLog.Skip(logAtEnd)
                    .Where(e => watched || Mentions(e.Message, cardId)).Take(watched ? 30 : 6).ToList();
                if (notes.Count > 0)
                {
                    lines.Add($"  ◆ end of turn {turn}: {Describe(beforeEnd, Snapshot(next))}");
                    foreach (var entry in notes)
                        lines.Add($"      [{entry.Type}] {entry.Message}");
                }
            }
        }

        var outcome = play.CombatDriver?.Current is null ? "fight over" : "turn budget spent";
        lines.Add($"  — {plays} play(s), {outcome}");
        return lines;
    }

    private static bool Mentions(string message, string cardId) =>
        message.Contains(cardId.TrimEnd('+'), StringComparison.OrdinalIgnoreCase)
        || message.Contains("Liable", StringComparison.Ordinal) || message.Contains("Held Energy", StringComparison.Ordinal);

    // Whatever the card asks, the first offer.
    private static void Answer(InteractiveCombatDriver driver)
    {
        for (var guard = 0; guard < 24; guard++)
        {
            if (driver.PendingOptionChoice is not null)
                driver.SupplyOptionChoice([0]);
            else if (driver.PendingCardChoice is { Count: > 0 } offered)
                driver.SupplyCardChoice([offered[0].Id]);
            else
                return;
        }
    }

    private sealed record State(
        int Energy, int Held, int Block, int Hand, int Draw, int Discard, int Exhausted,
        IReadOnlyList<(string Id, int Health, int Block, string Statuses)> Enemies, string HeroStatuses);

    private static State Snapshot(InteractiveCombat combat)
    {
        var hero = combat.State.GetCombatant(combat.HeroId);
        var zones = combat.State.GetCardZones(combat.HeroId);
        return new State(
            hero.Resources[StandardCombatIds.EnergyResource].Current,
            hero.Statuses.Where(s => s.DefinitionId.value == HeldEnergy.Id).Sum(s => s.Stacks),
            BlockOf(hero),
            zones.GetCardsInZone(CardZone.Hand).Count,
            zones.GetCardsInZone(CardZone.DrawPile).Count,
            zones.GetCardsInZone(CardZone.DiscardPile).Count,
            zones.GetCardsInZone(CardZone.ExhaustPile).Count,
            [.. combat.State.Combatants.Where(c => c.Id != combat.HeroId)
                .Select(c => (c.DefinitionId.value, c.Health.Current, BlockOf(c), Statuses(c)))],
            Statuses(hero));
    }

    private static int BlockOf(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static string Statuses(CombatantState c) =>
        string.Join(" ", c.Statuses.Where(s => s.Stacks != 0 && !s.DefinitionId.value.Contains("applicant"))
            .Select(s => $"{s.DefinitionId.value}:{s.Stacks}").Order());

    private static string Describe(State a, State b)
    {
        var parts = new List<string>
        {
            $"energy {a.Energy}→{b.Energy}",
            $"held {a.Held}→{b.Held}",
            $"block {a.Block}→{b.Block}",
            $"hand {a.Hand}→{b.Hand}",
            $"draw {a.Draw}→{b.Draw}",
            $"discard {a.Discard}→{b.Discard}",
            $"exhausted {a.Exhausted}→{b.Exhausted}",
        };
        for (var i = 0; i < Math.Min(a.Enemies.Count, b.Enemies.Count); i++)
        {
            var (id, hp, block, statuses) = a.Enemies[i];
            var now = b.Enemies[i];
            parts.Add($"{id} hp {hp}→{now.Health} block {block}→{now.Block} [{statuses}]→[{now.Statuses}]");
        }
        if (a.HeroStatuses != b.HeroStatuses)
            parts.Add($"you [{a.HeroStatuses}]→[{b.HeroStatuses}]");
        return string.Join(" · ", parts);
    }
}
