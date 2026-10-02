using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Converter.Playtest;

// IS THE DAMAGE CALCULATOR TELLING THE TRUTH? (playtest feedback 2, A3)
//
// The screen promises three numbers before they happen: what a hovered card will do (SessionPreview — the fight
// forked and the card played on the copy), what ending the turn will cost (the "Incoming" line —
// InteractiveCombat.Foresee), and what each enemy is about to hit for (the chips on its intent plate). This
// plays many real fights with the greedy player and checks every one of those promises against what then
// actually happened, at the moment it happened.
//
// A play or an enemy turn that ASKED something is not compared: the fork answers a question with the headless
// default and the real fight with whatever the player picked, so a difference there is the question and not a
// fault. Everything else should match to the point.
public static class CalcCheck
{
    private static readonly string[] Starters = ["paper_cut", "cower_behind_a_desk", "strong_binder", "permit_a38"];

    private sealed record Stand(int Health, int Block, bool Alive);

    private sealed class Tally
    {
        public int CardsChecked;
        public int CardsAsked;
        public int TurnsChecked;
        public int TurnsAsked;
        public int ChipsChecked;
        public readonly Dictionary<string, List<string>> Misses = new(StringComparer.Ordinal);

        public void Miss(string kind, string line)
        {
            if (!Misses.TryGetValue(kind, out var list))
                Misses[kind] = list = [];
            list.Add(line);
        }
    }

    public static int Run(RunBlueprint game, int fights, int seed, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(say);

        var rng = new Random(seed);
        var pool = Cards.FinalCards.All()
            .Where(c => c.Rarity is not "junk" && !(c.Tags ?? []).Contains("unplayable"))
            .Where(c => game.Cards.Any(d => d.Id == c.Id))
            .ToList();
        var queued = pool.Where(c => c.Queued).Select(c => c.Id).ToList();
        var others = pool.Where(c => !c.Queued).Select(c => c.Id).ToList();
        var encounters = game.Encounters
            .Where(e => !e.Id.Value.StartsWith("tutorial", StringComparison.Ordinal))
            .Select(e => e.Id.Value).OrderBy(id => id, StringComparer.Ordinal).ToList();

        var tally = new Tally();
        for (var i = 0; i < fights; i++)
        {
            var encounter = encounters[rng.Next(encounters.Count)];
            var deck = new List<string>();
            deck.AddRange(Enumerable.Repeat(Starters[0], 4));
            deck.AddRange(Enumerable.Repeat(Starters[1], 4));
            deck.Add(Starters[2]);
            deck.Add(Starters[3]);
            for (var k = 0; k < 4; k++)
                deck.Add(queued[rng.Next(queued.Count)]);
            for (var k = 0; k < 6; k++)
                deck.Add(others[rng.Next(others.Count)]);

            var ring = SparringRing.OneFight(game, SparringRing.Authored(game, encounter), deck,
                maxHealth: 400, energy: 4);
            try
            {
                Fight(ring, seed + i, encounter, tally);
            }
            catch (Exception ex)
            {
                tally.Miss("broke", $"{encounter}: {ex.GetType().Name}: {ex.Message}");
            }
            if ((i + 1) % 25 == 0)
                say($"   … {i + 1}/{fights} fights");
        }

        var md = new StringBuilder();
        md.AppendLine(CultureInfo.InvariantCulture, $"# Calculator check — {fights} fights, seed {seed}");
        md.AppendLine();
        md.AppendLine(CultureInfo.InvariantCulture,
            $"Card previews checked: {tally.CardsChecked} (+{tally.CardsAsked} that asked). "
            + $"Turn forecasts checked: {tally.TurnsChecked} (+{tally.TurnsAsked} that asked). "
            + $"Intent chips checked: {tally.ChipsChecked}.");
        foreach (var (kind, lines) in tally.Misses.OrderByDescending(m => m.Value.Count))
        {
            md.AppendLine();
            md.AppendLine(CultureInfo.InvariantCulture, $"## {kind} — {lines.Count}");
            foreach (var group in lines.GroupBy(l => l.Split(" | ")[0]).OrderByDescending(g => g.Count()).Take(40))
                md.AppendLine(CultureInfo.InvariantCulture, $"- ×{group.Count()} {group.First()}");
        }

        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Desktop", "bnb-balance", "calc");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}-calc.md");
        File.WriteAllText(path, md.ToString());
        say(md.ToString());
        say($"written: {path}");
        return 0;
    }

    private static void Fight(RunBlueprint ring, int seed, string encounter, Tally tally)
    {
        var rng = new Random(seed);
        using var play = new RunPlayback(() => { });
        play.Start(ring, seed, interactive: true);
        if (play.Error is { } startError)
        {
            tally.Miss("broke", $"{encounter}: {startError}");
            return;
        }
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        var driver = play.CombatDriver!;

        for (var turn = 0; turn < 30 && driver.Current is not null; turn++)
        {
            var refused = new HashSet<CardInstanceId>();
            var barren = new HashSet<string>(StringComparer.Ordinal);
            for (var plays = 0; plays < Greedy.PlaysInATurnNobodyMakes && driver.Current is { } combat; plays++)
            {
                if (Greedy.Answer(driver, rng))
                    continue;
                if (play.Error is not null || session.Error is not null)
                    return;
                if (Greedy.Choose(play, combat, rng, refused, barren) is not { } card)
                    break;

                var needsTarget = play.CardNeedsTarget.TryGetValue(card.DefinitionId.value, out var needs) && needs;
                var target = needsTarget ? Greedy.Enemy(combat) : null;
                var promised = Promise(combat, card.Id, target ?? Greedy.Enemy(combat));
                var steps = combat.Steps.Count;
                var before = Greedy.TableState(combat);
                driver.PlayCard(card.Id, target);
                if (Greedy.Refused(driver.Current, steps))
                {
                    refused.Add(card.Id);
                    continue;
                }
                if (driver.PendingCardChoice is not null || driver.PendingOptionChoice is not null)
                {
                    tally.CardsAsked++;
                    continue;
                }
                if (driver.Current is { } after && promised is not null)
                {
                    tally.CardsChecked++;
                    var actual = Standing(after);
                    foreach (var (id, then) in promised)
                        if (actual.TryGetValue(id, out var now) && now != then)
                            tally.Miss("card preview ≠ play",
                                $"{card.DefinitionId.value} | {encounter} turn {turn + 1}: {id} preview "
                                + $"{then.Health}hp/{then.Block}bl, played {now.Health}hp/{now.Block}bl");
                    if (Greedy.TableState(after) == before)
                        barren.Add(card.DefinitionId.value);
                }
            }

            if (driver.Current is not { } ending)
                break;
            if (Greedy.Answer(driver, rng))
                continue;

            var foresight = ending.Foresee();
            var heroId = ending.HeroId;
            // The whole table after the hand-back, queue resolution included — what a fork says it will be.
            Dictionary<string, Stand>? table = null;
            try
            {
                var fork = ending.Fork();
                fork.EndTurn();
                table = Standing(fork);
            }
            catch (InvalidOperationException)
            {
            }
            var queuedBefore = ending.State.GetCardZones(heroId).GetCardsInZone(CardZone.QueuePile).Count;
            CheckChips(ending, foresight, encounter, turn, tally);
            driver.EndTurn();
            if (driver.PendingCardChoice is not null || driver.PendingOptionChoice is not null)
            {
                tally.TurnsAsked++;
                continue;
            }
            if (foresight is null || driver.Current is not { } next)
                continue;
            tally.TurnsChecked++;
            if (table is not null)
            {
                var actualTable = Standing(next);
                foreach (var (id, then) in table)
                    if (id != heroId.value && actualTable.TryGetValue(id, out var now) && now.Health != then.Health)
                        tally.Miss("fork ≠ hand-back (enemies)",
                            $"{encounter} | turn {turn + 1}: {id} fork {then.Health}hp, actual {now.Health}hp, "
                            + $"queued {queuedBefore}");
            }
            var hp = next.State.GetCombatant(heroId).Health.Current;
            if (hp != foresight.HeroHealthAfter)
                tally.Miss("turn forecast ≠ enemy turn",
                    $"{encounter} | turn {turn + 1}: forecast hero {foresight.HeroHealthAfter}, actual {hp} "
                    + $"(blows {string.Join("+", foresight.Blows.Select(b => b.Amount))})");
        }
    }

    // What the hover would show: the card played on a fork, read back as each body's health and block.
    private static Dictionary<string, Stand>? Promise(InteractiveCombat combat, CardInstanceId card, CombatantId? target)
    {
        try
        {
            var fork = combat.Fork();
            fork.PlayCard(card, target);
            return Standing(fork);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static Dictionary<string, Stand> Standing(InteractiveCombat combat) =>
        combat.State.Combatants.ToDictionary(c => c.Id.value,
            c => new Stand(c.Health.Current,
                c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var p) ? p.Current : 0,
                c.IsAlive),
            StringComparer.Ordinal);

    // The intent plate prints the AUTHORED number ("11 dmg"). Compare it with what the blow will actually take
    // (health + block) on the forecast — only when the hero lives through it, so a blow is not cut short by death.
    private static void CheckChips(InteractiveCombat combat, Foresight? foresight, string encounter, int turn, Tally tally)
    {
        if (foresight is null || foresight.HeroDies)
            return;
        foreach (var blow in foresight.Blows)
        {
            if (blow.Intent is not { } intent)
                continue;
            var printed = Regex.Matches(intent.Label, @"(\d+) dmg").Sum(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            var hits = Regex.Match(intent.Label, @"(\d+)\s*[x×]\s*(\d+) dmg");
            if (hits.Success)
                printed = int.Parse(hits.Groups[1].Value, CultureInfo.InvariantCulture)
                    * int.Parse(hits.Groups[2].Value, CultureInfo.InvariantCulture);
            if (printed == 0 && blow.Amount == 0)
                continue;
            tally.ChipsChecked++;
            if (printed != blow.Amount)
                tally.Miss("intent chip ≠ blow",
                    $"{intent.Label} | {encounter} turn {turn + 1}: chip {printed}, blow {blow.Amount}");
        }
    }
}
