using System.Globalization;
using System.Text.Json;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using BnbCard = BnbContent.Converter.Cards.CardAuthoring.BnbCard;

namespace BnbContent.Converter.Playtest;

// ── WHAT A CARD DOES, PER POINT OF ENERGY (BALANCE_PLAN K1) ───────────────────────────────────────────────────
// The cheap half of valuing a card: no fight, no planner, seconds for the whole pool. The card's compiled
// program is walked and what it applies is added up — damage, block, status stacks, cards drawn, energy — and
// divided by what it costs. Low cost and big numbers are good; that is the whole idea, on purpose.
//
// ⚠⚠ THE KEYWORDS ARE COMPILED INTO EVERY CARD, AND THEIR MACHINERY IS NOT AN EFFECT. `ApplySeal` appends the
// "at 3 Seal, Ratify" conversion to each card that grants Seal; `IfRiteInForce` wraps a synergy in a scratch
// counter; `Archive` and `AddJunk` file a bookkeeping status. A walker that counted all of that would pay a Seal
// card for its own plumbing. So those shapes are recognised and skipped: Seal is valued as the stacks it lays
// down, and what the conversion does with them is the keyword's worth, not this card's.
//
// ⚠⚠ TWO NUMBERS, BECAUSE A SCALING CARD HAS TWO. "Deal 14, plus 8 for each different negative Status (at most
// 5)" is 14 on a clean target and 54 on a buried one. `safe` counts every fight-dependent amount as 0 — what the
// card does with no set-up — and is the rank. `max` counts it as ScalingTerm, which in practice runs into the
// card's own ceiling; the gap between the two is how much the card leans on set-up.
//
// ⚠ IT GUESSES IN NAMED PLACES AND SAYS SO ON EVERY ROW. A branch counts half (`if`), an amount that needs a
// fight in front of it is `scales` (see above), "for each card in a zone" counts CardsInAZone (`zone`). A
// status that is not a keyword carries its effect in its own rule (every Rite does) and scores nothing here
// (`rule:<id>`). A node kind this does not know is listed (`unread:<kind>`) — a card the walker cannot read is a
// finding, never a zero.
//
// The measured half is K2 (the planner). K3 holds the two against each other, and the weights below are the
// first thing that comparison will move.
public static class CardRating
{
    // ── the weights, in POINTS: one point of damage = one point of block = one stack of a keyword status ──
    public const double Stack = 1;
    public const double DrawnCard = 3;      // a card drawn: about half of what an average card does
    public const double EnergyPoint = 6;    // an energy: what Paper Cut does with one (6 damage)
    public const double FreeCard = 0.5;     // a 0-cost card divides by this, so it is never infinite

    // The three guesses, as in the runner's old evaluator.
    private const double MaybeRuns = 0.5;
    private const double ScalingTerm = 3;
    private const double CardsInAZone = 4;

    // The keywords whose stacks ARE the effect. Named rather than read off a polarity: Censure is neutral (it
    // protects whoever carries it) and still a keyword, while the neutral tallies are only records.
    private static readonly HashSet<string> Keyword = new(StringComparer.Ordinal)
    {
        Keywords.Paperwork, Keywords.Doubt, Keywords.Seal, Keywords.Censure, Keywords.Lien, Keywords.Citation,
        Keywords.BloodInk, Keywords.WardWax,
    };

    // Statuses that record that something happened — Archived, Junk filed, a Ratify — and do nothing themselves.
    private static readonly HashSet<string> Bookkeeping = new(StringComparer.Ordinal)
    {
        Keywords.Archived, Keywords.JunkFiled, Keywords.QueueResolved, Keywords.Ratified,
    };

    public sealed class Reading
    {
        public double Damage;
        public double Block;
        public double Draw;
        public double Energy;
        public readonly SortedDictionary<string, double> Statuses = new(StringComparer.Ordinal);
        public readonly SortedSet<string> Flags = new(StringComparer.Ordinal);

        public double StatusStacks => Statuses.Values.Sum();

        public double Points => Damage + Block + Stack * StatusStacks + DrawnCard * Draw + EnergyPoint * Energy;
    }

    public sealed record Row(
        BnbCard Card, Reading Read, double PerEnergy, double MaxPerEnergy, BnbCard? Upgrade, double? UpgradePerEnergy);

    public static double PerEnergy(double points, int cost) => points / (cost <= 0 ? FreeCard : cost);

    public static int Run(RunBlueprint game, Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(say);
        var rows = Rate(game);
        var byId = rows.ToDictionary(r => r.Card.Id, StringComparer.Ordinal);

        say($"card rating (static) · 1 dmg = 1 block = {Stack} per status stack · draw {DrawnCard} · energy "
            + $"{EnergyPoint} · 0-cost divides by {FreeCard} · branch ×{MaybeRuns} · scaling = {ScalingTerm} · "
            + $"zone = {CardsInAZone} cards · area × {EnemiesInAFight(game):0.00} enemies");
        say("  safe = points with no set-up, per energy (the rank) · max = with every scaling term fed · up = the "
            + "upgrade's safe · flags say where it had to guess");

        // The yardstick: what the deck starts with. Paper Cut is 6 per energy by construction.
        say("\nSTARTER — the yardstick");
        foreach (var r in BureaucratStarter.All().Where(c => !c.Id.EndsWith('+'))
                     .Select(c => byId.GetValueOrDefault(c.Id)).OfType<Row>())
            say(Line(r));

        for (var act = 1; act <= 4; act++)
        {
            var added = FinalCards.RewardPool(act).Where(c => c.Act == act).Select(c => byId[c.Id]).ToList();
            if (added.Count == 0)
                continue;
            say($"\nACT {act} — {added.Count} cards join the reward pool");
            say($"  {"card",-28} {"rarity",-8} {"type",-8} {"E",2} {"dmg",5} {"blk",5} {"stk",5} {"drw",4} "
                + $"{"nrg",4} {"pts",6} {"safe",6} {"max",6} {"up",6}  flags · statuses");
            foreach (var r in added.OrderByDescending(r => r.PerEnergy).ThenBy(r => r.Card.Id, StringComparer.Ordinal))
                say(Line(r));
        }

        var unread = rows.SelectMany(r => r.Read.Flags.Where(f => f.StartsWith("unread:", StringComparison.Ordinal))
                .Select(f => (f, r.Card.Id)))
            .GroupBy(x => x.f).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        var ruled = rows.Count(r => r.Read.Flags.Any(f => f.StartsWith("rule:", StringComparison.Ordinal)));
        say($"\n{rows.Count} cards rated · {ruled} carry (part of) their effect in a status rule, not scored here");
        if (unread.Count == 0)
            say("every node kind in every card was read");
        foreach (var g in unread)
            say($"  {g.Key}: {string.Join(", ", g.Select(x => x.Id))}");
        return 0;
    }

    private static string Line(Row r)
    {
        var x = r.Read;
        static string N(double v) => v == 0 ? "·" : v.ToString("0.#", CultureInfo.InvariantCulture);
        var statuses = string.Join(" ", x.Statuses.Select(s => $"{s.Key}×{N(s.Value)}"));
        var upgrade = r.UpgradePerEnergy is { } u ? u.ToString("0.0", CultureInfo.InvariantCulture) : "—";
        return $"  {r.Card.Id,-28} {r.Card.Rarity,-8} {r.Card.Type,-8} {r.Card.Cost,2} {N(x.Damage),5} "
            + $"{N(x.Block),5} {N(x.StatusStacks),5} {N(x.Draw),4} {N(x.Energy),4} {x.Points,6:0.0} "
            + $"{r.PerEnergy,6:0.0} {r.MaxPerEnergy,6:0.0} {upgrade,6}  {string.Join(" ", x.Flags)}"
            + (statuses.Length > 0 ? $" · {statuses}" : "");
    }

    // Every final card, base and upgrade, read off the program the game actually ships.
    public static IReadOnlyList<Row> Rate(RunBlueprint game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var enemies = EnemiesInAFight(game);

        using var document = JsonDocument.Parse(RunJson.ToJson(game, RunJson.CreateOptions(indented: false)));
        var programs = document.RootElement.GetProperty("Cards").EnumerateArray()
            .ToDictionary(c => c.GetProperty("Id").GetString()!, c => c.GetProperty("Program").Clone(), StringComparer.Ordinal);

        var cards = FinalCards.All().GroupBy(c => c.Id, StringComparer.Ordinal).Select(g => g.First())
            .ToDictionary(c => c.Id, StringComparer.Ordinal);
        Reading Read(BnbCard card, double scaling = 0)
        {
            var reading = new Reading();
            if (programs.TryGetValue(card.Id, out var program) && program.TryGetProperty("Root", out var root))
                new Walker(reading, enemies, scaling).Walk(root, 1);
            else
                reading.Flags.Add("unread:no-program");
            return reading;
        }

        var rows = new List<Row>();
        foreach (var card in cards.Values.Where(c => !c.Id.EndsWith('+')))
        {
            var reading = Read(card);
            var twin = cards.GetValueOrDefault(card.Id + "+");
            double? up = twin is null ? null : PerEnergy(Read(twin).Points, twin.Cost);
            var fed = Read(card, ScalingTerm).Points;
            rows.Add(new Row(card, reading, PerEnergy(reading.Points, card.Cost), PerEnergy(fed, card.Cost), twin, up));
        }
        return rows;
    }

    // How many enemies "every enemy" hits, asked of the encounters this game ships.
    private static double EnemiesInAFight(RunBlueprint game)
    {
        var sides = game.Encounters.Where(e => e.Enemies.Count > 0).Select(e => e.Enemies.Count).ToList();
        return sides.Count == 0 ? 1 : Math.Max(1, sides.Average());
    }

    private sealed class Walker(Reading into, double enemies, double scaling)
    {
        public void Walk(JsonElement node, double weight)
        {
            if (weight <= 0)
                return;
            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in node.EnumerateArray())
                    Walk(child, weight);
                return;
            }
            if (node.ValueKind != JsonValueKind.Object || Kind(node) is not { } kind
                || !node.TryGetProperty("value", out var body) || body.ValueKind != JsonValueKind.Object)
                return;

            switch (kind)
            {
                case "node.dealDamage":
                    into.Damage += weight * Amount(body, "Amount") * Targets(body, "TargetSelector");
                    return;
                case "node.gainBlock":
                    into.Block += weight * Amount(body, "Amount") * Targets(body, "TargetSelector");
                    return;
                // A pool moved either way is a pool fought over: stripping three guard does guard-work.
                case "node.modifyDefensivePool":
                    into.Block += weight * Math.Abs(Amount(body, "Delta")) * Targets(body, "TargetSelector");
                    return;
                case "node.applyStatus":
                    Status(Id(body, "StatusDefinitionId"), weight * Amount(body, "Stacks") * Targets(body, "TargetSelector"));
                    return;
                // Only what is ADDED is an effect; stacks taken off a target are a keyword being spent.
                case "node.modifyStatusStacks":
                case "node.modifySelectedStatusStacks":
                    if (Amount(body, "Delta") is > 0 and var delta)
                        Status(Id(body, "StatusDefinitionId"), weight * delta * Targets(body, "TargetSelector"));
                    return;
                case "node.modifyStatusDuration":
                case "node.removeStatus":
                    into.Flags.Add("status-edit");
                    return;
                case "node.drawCards":
                    into.Draw += weight * Amount(body, "Amount") + weight * Amount(body, "Count");
                    return;
                case "node.gainResource":
                    into.Energy += weight * Amount(body, "Amount");
                    return;
                case "node.modifyResource":
                    into.Energy += weight * Amount(body, "Delta");
                    return;
                case "node.loseResource":
                    into.Energy -= weight * Amount(body, "Amount");
                    return;
                case "node.heal":
                    into.Block += weight * Amount(body, "Amount");
                    into.Flags.Add("heal");
                    return;
                case "node.createCardInstance":
                case "node.createCardCopy":
                    into.Flags.Add($"makes:{Id(body, "CardDefinitionId") ?? "copy"}");
                    return;
                case "node.moveCardToZone":
                case "node.moveAllCardsFromZone":
                case "node.queueCard":
                case "node.resolveQueuedCards":
                case "node.markCardInstance":
                case "node.setCardInstanceMarkCounter":
                    into.Flags.Add("card-play");
                    return;
                case "node.setHealth":
                case "node.randomTargets":
                    into.Flags.Add(kind["node.".Length..]);
                    return;
                // Scratch state and nothing: the plumbing of other nodes.
                case "node.setCombatantCounter":
                case "node.noOp":
                    return;

                case "node.causalSequence":
                case "node.sequence":
                    Walk(Field(body, "Children"), weight);
                    return;
                case "node.repeat":
                    Walk(Field(body, "Body"), weight * Math.Max(1, Amount(body, "Count")));
                    return;
                case "node.forEachTarget":
                    into.Flags.Add("each-target");
                    Walk(Field(body, "Body"), weight * Targets(body, "CollectionSelector"));
                    return;
                case "node.forEachCardInZone":
                    {
                        var take = Field(body, "TakeFirst") is { ValueKind: JsonValueKind.Number } n ? n.GetInt32() : 0;
                        if (take <= 0)
                            into.Flags.Add("zone");
                        Walk(Field(body, "Body"), weight * (take > 0 ? take : CardsInAZone));
                        return;
                    }
                case "node.conditional":
                    {
                        // The keyword machinery, recognised by what it asks: the Seal conversion ("≥ 3 Seal") and
                        // a Rite synergy ("is the rite_in_force counter set"). Neither is this card's effect.
                        var condition = Field(body, "Condition");
                        if (IsSealConversion(condition))
                            return;
                        if (condition.GetRawText().Contains("\"rite_in_force\"", StringComparison.Ordinal))
                        {
                            into.Flags.Add("rite-synergy");
                            return;
                        }
                        into.Flags.Add("if");
                        Walk(Field(body, "Then"), weight * MaybeRuns);
                        Walk(Field(body, "Else"), weight * MaybeRuns);
                        return;
                    }
                case "node.chooseOptions":
                    {
                        var children = Field(body, "Children");
                        var offered = children.ValueKind == JsonValueKind.Array ? children.GetArrayLength() : 0;
                        var taken = Math.Max(1, Amount(body, "Count"));
                        into.Flags.Add("choose");
                        Walk(children, offered == 0 ? weight : weight * taken / offered);
                        return;
                    }
                default:
                    into.Flags.Add($"unread:{kind}");
                    return;
            }
        }

        // ConvertSeals asks exactly "the target has at least 3 Seal" (CardAuthoring.RatifyThreshold). A card
        // that asks anything else about Seal — Privy Seal's "at least 1" — is asking for itself.
        private static bool IsSealConversion(JsonElement condition) =>
            Kind(condition) == "compare"
            && condition.TryGetProperty("value", out var test)
            && Field(test, "Op").ValueKind == JsonValueKind.String && Field(test, "Op").GetString() == "GreaterOrEqual"
            && Kind(Field(test, "Left")) == "combatantStatusStacks"
            && Id(Field(Field(test, "Left"), "value"), "StatusId") == Keywords.Seal
            && Kind(Field(test, "Right")) == "const"
            && Field(Field(Field(test, "Right"), "value"), "Value") is { ValueKind: JsonValueKind.Number } n
            && n.GetDouble() == CardAuthoring.RatifyThreshold;

        private void Status(string? id, double stacks)
        {
            if (id is null || Bookkeeping.Contains(id) || stacks == 0)
                return;
            if (!Keyword.Contains(id))
            {
                into.Flags.Add($"rule:{id}");
                return;
            }
            into.Statuses[id] = into.Statuses.GetValueOrDefault(id) + stacks;
        }

        private double Amount(JsonElement body, string field) =>
            body.TryGetProperty(field, out var amount) ? Amount(amount) : 0;

        private double Amount(JsonElement amount)
        {
            if (amount.ValueKind == JsonValueKind.Number)
                return amount.GetDouble();
            if (amount.ValueKind != JsonValueKind.Object || Kind(amount) is not { } kind
                || !amount.TryGetProperty("value", out var body))
                return 0;
            switch (kind)
            {
                case "const": return Amount(body, "Value");
                case "add": return Amount(body, "Left") + Amount(body, "Right");
                case "subtract": return Amount(body, "Left") - Amount(body, "Right");
                case "multiply": return Amount(body, "Left") * Amount(body, "Right");
                case "min": return Math.Min(Amount(body, "Left"), Amount(body, "Right"));
                case "max": return Math.Max(Amount(body, "Left"), Amount(body, "Right"));
                case "negate": return -Amount(body, "Operand");
                case "divide":
                    return Amount(body, "Divisor") is var by && by != 0 ? Amount(body, "Dividend") / by : 0;
                default:
                    into.Flags.Add("scales");
                    return scaling;
            }
        }

        // How many a selector picks out. Only a CROWD answers more than one — and a crowd narrowed to its
        // first member (`sel.first`) is one.
        private double Targets(JsonElement body, string field)
        {
            if (!body.TryGetProperty(field, out var selector) || Kind(selector) is not { } kind)
                return 1;
            if (kind is "sel.allEnemies" or "sel.enemiesWithStatus")
            {
                into.Flags.Add("area");
                return enemies;
            }
            return 1;
        }

        private static string? Kind(JsonElement node) =>
            node.ValueKind == JsonValueKind.Object && node.TryGetProperty("kind", out var k) ? k.GetString() : null;

        private static JsonElement Field(JsonElement body, string field) =>
            body.TryGetProperty(field, out var child) ? child : default;

        private static string? Id(JsonElement body, string field) =>
            body.TryGetProperty(field, out var id) && id.ValueKind == JsonValueKind.Object
                && id.TryGetProperty("value", out var v) ? v.GetString() : null;
    }
}
