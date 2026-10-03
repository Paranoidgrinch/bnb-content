using System.Text.Json;
using RogueDeck.Run;

namespace BnbContent.Converter;

// WHERE EVERY CARD AND RELIC IS SHELVED IN THE ARCHIVE (user, 2026-10-03): the cards a player can own apart from
// the cards a fight forces on them, the Bureaucrat's own apart from the general pool, and every relic by the pool
// it is won from. The game files know this; the archive does not, so it is written into each entry's
// presentation (`Extra["archiveSection"]`), and a fight card also says what puts it in a hand
// (`Extra["madeBy"]`). The engine never reads either.
public static class ArchiveSections
{
    public const string SectionKey = "archiveSection";
    public const string MadeByKey = "madeBy";

    // The order the archive shows them in.
    public const string Bureaucrat = "Bureaucrat";
    public const string GeneralPool = "General pool";
    public const string Junk = "Junk & curses";
    public const string RelicCards = "Relic cards";
    public const string Fight = "Fight cards";

    private const string EventPrefix = "event: ";
    private const string RelicPrefix = "relic: ";
    private const string CardPrefix = "card: ";

    public static PresentationManifest Annotate(RunBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        var manifest = blueprint.Presentation;

        // Every offerable card and starter by its own id; an upgrade is shelved with the card it improves.
        var bnb = Cards.FinalCards.All().Where(c => !c.Id.EndsWith('+')).ToDictionary(c => c.Id, StringComparer.Ordinal);
        var character = Cards.FinalCards.CharacterPool(5).Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var general = Cards.FinalCards.GeneralPool(5).Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        string CardSection(string id)
        {
            var key = id.TrimEnd('+');
            if (character.Contains(key) || bnb.GetValueOrDefault(key)?.Rarity == "starter")
                return Bureaucrat;
            if (general.Contains(key))
                return GeneralPool;
            return bnb.GetValueOrDefault(key)?.Rarity == "junk" ? Junk : Fight;
        }

        var madeBy = MadeBy(blueprint);
        var cards = blueprint.Cards.ToDictionary(c => c.Id, c =>
        {
            var look = manifest.Cards.GetValueOrDefault(c.Id) ?? new EntityPresentation();
            var section = CardSection(c.Id);
            var makers = madeBy.GetValueOrDefault(c.Id.TrimEnd('+')) ?? [];
            // A card no pool offers is a FIGHT card when an enemy or a fight writes it — and otherwise what
            // gives it: only events (a curse they leave in the deck) or a relic (its own card).
            if (section == Fight && makers.Count > 0)
            {
                if (makers.All(m => m.StartsWith(EventPrefix, StringComparison.Ordinal)))
                    section = Junk;
                else if (makers.All(m => m.StartsWith(RelicPrefix, StringComparison.Ordinal)
                                         || m.StartsWith(CardPrefix, StringComparison.Ordinal))
                         && makers.Any(m => m.StartsWith(RelicPrefix, StringComparison.Ordinal)))
                    section = RelicCards;
            }
            var extra = new Dictionary<string, string>(look.Extra, StringComparer.Ordinal) { [SectionKey] = section };
            if (section is Fight or Junk or RelicCards && makers.Count > 0)
                extra[MadeByKey] = string.Join(" · ", makers);
            return look with { Extra = extra };
        }, StringComparer.Ordinal);

        var relicPool = Relics.FinalRelics.All().ToDictionary(r => r.Id, StringComparer.Ordinal);
        var relics = manifest.Relics.ToDictionary(p => p.Key, p =>
        {
            var section = relicPool.GetValueOrDefault(p.Key) is { } relic
                ? relic.Eligibility == Relics.RelicAuthoring.Eligibility.Bureaucrat
                    ? Bureaucrat
                    : relic.Pool.ToString()
                : "Other";
            return p.Value with
            {
                Extra = new Dictionary<string, string>(p.Value.Extra, StringComparer.Ordinal) { [SectionKey] = section },
            };
        }, StringComparer.Ordinal);

        return manifest with { Cards = cards, Relics = relics };
    }

    // Who puts each card into a fight, by name: an enemy whose move or opening status writes it, a fight whose
    // own rules do, an event, a relic, or another card. Read off the shipped document itself — every place a
    // card id is written as a value — so nothing here has to be kept in step with the content by hand.
    private static Dictionary<string, List<string>> MadeBy(RunBlueprint blueprint)
    {
        var options = RunJson.CreateOptions(indented: false);
        using var doc = JsonDocument.Parse(RunJson.ToJson(blueprint, options));
        var root = doc.RootElement;
        var cardIds = blueprint.Cards.Select(c => c.Id.TrimEnd('+')).ToHashSet(StringComparer.Ordinal);

        // Every card id named anywhere inside one element.
        HashSet<string> Named(JsonElement element)
        {
            var found = new HashSet<string>(StringComparer.Ordinal);
            void Walk(JsonElement e)
            {
                switch (e.ValueKind)
                {
                    case JsonValueKind.String:
                        if (e.GetString() is { } s && cardIds.Contains(s.TrimEnd('+')))
                            found.Add(s.TrimEnd('+'));
                        break;
                    case JsonValueKind.Object:
                        // An element's own id is not a mention of anything (a Rite's status shares its card's id).
                        foreach (var p in e.EnumerateObject().Where(p => p.Name != "Id"))
                            Walk(p.Value);
                        break;
                    case JsonValueKind.Array:
                        foreach (var item in e.EnumerateArray())
                            Walk(item);
                        break;
                }
            }
            Walk(element);
            return found;
        }

        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Add(string card, string who)
        {
            if (!result.TryGetValue(card, out var list))
                result[card] = list = [];
            if (!list.Contains(who))
                list.Add(who);
        }

        // Enemy moves and enemy statuses → the enemies that carry them.
        var byAction = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var byStatus = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var encounter in blueprint.Encounters)
            foreach (var enemy in encounter.Enemies)
            {
                var name = enemy.DisplayName ?? enemy.Id;
                foreach (var action in enemy.Actions)
                    (byAction.TryGetValue(action.value, out var a) ? a : byAction[action.value] = []).Add(name);
                foreach (var status in enemy.StartingStatuses ?? [])
                    (byStatus.TryGetValue(status.Status.value, out var s) ? s : byStatus[status.Status.value] = []).Add(name);
            }

        foreach (var action in root.GetProperty("EnemyActions").EnumerateArray())
        {
            var id = action.GetProperty("Id").GetString()!;
            foreach (var card in Named(action))
                foreach (var enemy in byAction.GetValueOrDefault(id) ?? [])
                    Add(card, enemy);
        }

        var relicNames = blueprint.Relics.ToDictionary(r => r.Id, r => r.DisplayName, StringComparer.Ordinal);
        foreach (var status in root.GetProperty("Statuses").EnumerateArray())
        {
            var id = status.GetProperty("Id").GetString()!;
            var named = Named(status);
            if (named.Count == 0)
                continue;
            var who = byStatus.GetValueOrDefault(id) is { Count: > 0 } enemies
                ? enemies
                : relicNames.TryGetValue(id, out var relic) ? [RelicPrefix + relic] : (IEnumerable<string>)[];
            foreach (var card in named)
                foreach (var name in who)
                    Add(card, name);
        }

        // A fight's own rules (its opening hand, its triggers): every body standing in it.
        foreach (var encounter in root.GetProperty("Encounters").EnumerateArray())
        {
            var bodies = encounter.GetProperty("Enemies").EnumerateArray()
                .Select(e => e.TryGetProperty("DisplayName", out var n) && n.ValueKind == JsonValueKind.String
                    ? n.GetString()! : e.GetProperty("Id").GetString()!)
                .ToList();
            foreach (var property in encounter.EnumerateObject().Where(p => p.Name != "Enemies"))
                foreach (var card in Named(property.Value))
                    foreach (var body in bodies)
                        Add(card, body);
        }

        foreach (var relic in root.GetProperty("Relics").EnumerateArray())
            foreach (var card in Named(relic))
                Add(card, RelicPrefix + relicNames.GetValueOrDefault(relic.GetProperty("Id").GetString()!, "a relic"));

        var eventNames = blueprint.Presentation.Events.ToDictionary(
            e => e.Key, e => e.Value.FlavorText ?? e.Key, StringComparer.Ordinal);
        foreach (var ev in root.GetProperty("Events").EnumerateObject())
            foreach (var card in Named(ev.Value))
                Add(card, $"{EventPrefix}{eventNames.GetValueOrDefault(ev.Name, ev.Name)}");

        // Cards that make other cards, by name — but a card does not "make" itself.
        var cardNames = blueprint.Cards.ToDictionary(c => c.Id, c => c.NameKey ?? c.Id, StringComparer.Ordinal);
        foreach (var card in root.GetProperty("Cards").EnumerateArray())
        {
            var id = card.GetProperty("Id").GetString()!;
            foreach (var made in Named(card).Where(m => m != id.TrimEnd('+')))
                Add(made, $"{CardPrefix}{cardNames.GetValueOrDefault(id.TrimEnd('+'), id)}");
        }
        return result;
    }
}
