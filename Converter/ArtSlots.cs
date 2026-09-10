using System.Text;
using System.Text.RegularExpressions;

using RogueDeck.Run;

using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;

using BnbCard = BnbContent.Converter.Cards.CardAuthoring.BnbCard;

namespace BnbContent.Converter;

// ART_SLOTS.md — the table the pictures get worked down from.
//
// Every card and every relic the document ships names a file the frontend will look for; none of those files
// exists yet. This writes the list of them: one row per PICTURE, not per entity, with everything the person
// drawing it needs in the row — the file name, the title, where it comes from, and for a relic the catalogue
// number and the visual brief the design canon already wrote for it.
//
// THE ROW IS THE FILE NAME. A slot is filled by dropping a PNG in; nothing here has to be edited, no registry
// exists to add it to, and a name that has to be looked up would have made a second registry out of this
// document. Because an upgraded card shares its base card's slot ("levy_stamp+" is drawn from
// "levy_stamp.png"), 413 cards ask for 254 pictures.
public static partial class ArtSlots
{
    public static int Write(RunBlueprint blueprint, BabData data, string dataDir, string outFile)
    {
        var canon = Canon(dataDir);
        var page = new StringBuilder();
        var relics = FinalRelics.All();
        var cardSlots = blueprint.Cards.Select(c => c.Id).Where(id => !id.EndsWith('+')).ToHashSet(StringComparer.Ordinal);

        // The remnants are counted for the head line: they are slots, but they are not work worth doing.
        // A remnant is a ported card that no authored sheet replaced — the ones that reach the last table.
        var ported = data.Cards.Select(c => CardMapper.MapCardId(c.Id)).ToHashSet(StringComparer.Ordinal);
        var authored = FinalCards.Ids();
        var bodies = EnemySlots(data);
        Head(page, cardSlots.Count, relics.Count, bodies.Count,
            cardSlots.Count(id => ported.Contains(id) && !authored.Contains(id)));
        RelicTables(page, relics, canon);
        EnemyTables(page, blueprint, bodies);
        CardTables(page, blueprint, data, cardSlots);

        File.WriteAllText(outFile, page.ToString());
        var briefless = relics.Count(r => !canon.ContainsKey(r.Name));
        Console.WriteLine($"Wrote {outFile}: {cardSlots.Count} card slots, {relics.Count} relic slots, "
            + $"{bodies.Count} enemy slots, {cardSlots.Count + relics.Count + bodies.Count} pictures in all"
            + (briefless == 0 ? " (every relic has its canon brief)." : $" — {briefless} relic(s) WITHOUT a canon brief."));
        return 0;
    }

    private static void Head(StringBuilder page, int cards, int relics, int enemies, int remnants) => page.Append($"""
        # Art slots — every picture the game looks for

        Generated: `dotnet run --project Converter -- --art-slots ART_SLOTS.md`. **Do not edit by hand.**

        **{cards + relics + enemies} pictures**: {cards} cards, {relics} relics and {enemies} bodies (every
        enemy, elite and boss). Every one of them is filled today by a PLACEHOLDER — a plate with the file's own
        name and the word placeholder on it, written by `bnb-godot/tools/make-card-art.py`,
        `make-relic-art.py` and `make-enemy-art.py`. A painted picture replaces one by being saved over it, so
        the list below can be worked down in any order and nothing has to be registered anywhere. A slot with no
        file at all is a normal state as well: a card draws an empty socket with its own code in it, a relic
        draws its code, and a body draws a stick figure. {remnants} of the card slots belong to the demo game's
        leftovers and are marked "Ported v2 remnants" at the end: **{cards + relics + enemies - remnants}
        pictures** are the real list.

        ## How a slot is filled

        1. Name the file after the **code** in the table and drop it in:
           `bnb-godot/assets/art/cards/<code>.png` · `bnb-godot/assets/art/relics/<code>.png` ·
           `bnb-godot/assets/art/enemies/<code>.png`
        2. Run `bnb-godot/tools/import-art.sh` once afterwards. Godot only reads textures it has imported, so a
           file that was merely copied in is invisible to the game until that runs (the editor does it by itself
           on focus; the headless probes do not).

        The code is the entity's id, which is also what the export contract's `Presentation.Art` already says.
        **An upgraded card has no slot of its own**: `levy_stamp+` is drawn from `levy_stamp.png`, so an
        improvement never changes the picture. Dropping `levy_stamp+.png` in does nothing — the `+` is not part
        of any file name in this game.

        ## What the picture has to be

        - **Card art** is drawn into the frame's window at **122 × 110** points (11:10 landscape), cropped to
          fill. Draw it larger — 488 × 440 or 976 × 880 — and let the mipmaps do the reduction; the window is
          the only part of the card the picture may occupy, and the frame is printed over everything else.
        - **Relic art** will be drawn (D4) as a **small square** in the right-hand strip, roughly 34 × 34
          points, larger on hover. It has to survive being that small: one object, a clear silhouette, no fine
          text. Square source, 512 × 512 or more.
        - **A body** stands in its column in the arena, in a window as wide as that column and **150 points
          tall**, its aspect KEPT (never cropped, never squashed): a portrait ends up about 110 points wide
          with the health bar directly under its feet. Draw it portrait on transparency — 600 × 900 or more —
          and let the mipmaps reduce it. A crowd of four narrows every column to about 110 points, so the
          silhouette has to read at half the room a duel gives it. **Draw the body facing LEFT**, towards the
          player, who stands on the left of the arena: a picture is never mirrored by the game (a flipped body
          wears its sash on the wrong side), so the direction it faces is the direction it was drawn in.
        - PNG, RGBA. Transparency is welcome — a card's socket is a dark recess and a relic's square carries
          its pool's frame colour, and both are meant to show through.

        The relic briefs below are the design canon's own words
        (`source-data/design/BnB_Final_Relics_Master_PostAudit_VISUAL_DESIGN_CANON.md` for #1–168,
        `BnB_Elite_Relics_MASTER_AND_VISUAL_CANON.md` for #169–210). Frames, palettes and the label rule live
        there in full; only the object line is repeated here, because that is the line that differs per relic.


        """);

    private static void RelicTables(
        StringBuilder page, IReadOnlyList<RelicAuthoring.BnbRelic> relics,
        IReadOnlyDictionary<string, (int Number, string Brief)> canon)
    {
        page.AppendLine($"## Relics — {relics.Count} pictures");
        page.AppendLine();
        page.AppendLine("The catalogue number is the design canon's, and it is not part of the file name: the id is.");
        page.AppendLine();
        foreach (var pool in new[]
                 {
                     RelicAuthoring.Pool.Normal, RelicAuthoring.Pool.Shop, RelicAuthoring.Pool.Event,
                     RelicAuthoring.Pool.Boss, RelicAuthoring.Pool.Elite, RelicAuthoring.Pool.Mimic,
                 })
        {
            var members = relics.Where(r => r.Pool == pool)
                .OrderBy(r => canon.TryGetValue(r.Name, out var e) ? e.Number : int.MaxValue)
                .ToList();
            if (members.Count == 0)
                continue;
            page.AppendLine($"### {pool} relics — {members.Count}");
            page.AppendLine();
            page.AppendLine("| # | code | title | rarity | the object |");
            page.AppendLine("|---:|---|---|---|---|");
            foreach (var relic in members)
            {
                var entry = canon.TryGetValue(relic.Name, out var found) ? found : (Number: 0, Brief: "");
                page.AppendLine($"| {(entry.Number == 0 ? "—" : entry.Number.ToString())} | `{relic.Id}` | "
                    + $"{Cell(relic.Name)} | {relic.Rarity.ToString().ToLowerInvariant()} | "
                    + $"{Cell(entry.Brief.Length > 0 ? entry.Brief : relic.Text)} |");
            }
            page.AppendLine();
        }
    }

    // Every body the document ships, with the kind of room it is met in. The ROLE is the thing to draw to:
    // a mook, an elite and a god are not the same picture at three sizes. It is worked out from the fights an
    // id appears in (BlueprintAssembler.EnemyRole) and travels in the document as `Presentation.Frame`, so
    // this table and the game agree about it by construction rather than by being kept in step.
    private static IReadOnlyList<(string Id, string Name, string Role, int Health, int Fights, string Example)>
        EnemySlots(BabData data)
    {
        var roles = EnemyRole.Of(data);
        var fights = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var encounter in data.Encounters)
        {
            foreach (var id in encounter.Enemies.Distinct(StringComparer.Ordinal))
            {
                if (!fights.TryGetValue(id, out var list))
                    fights[id] = list = [];
                list.Add(encounter.Name);
            }
        }
        return data.Enemies
            .Where(e => fights.ContainsKey(e.Id))
            .Select(e => (
                e.Id,
                e.Name,
                Role: roles.GetValueOrDefault(e.Id, EnemyRole.Standard),
                e.MaxHp,
                Fights: fights[e.Id].Count,
                Example: fights[e.Id][0]))
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static void EnemyTables(
        StringBuilder page, RunBlueprint blueprint,
        IReadOnlyList<(string Id, string Name, string Role, int Health, int Fights, string Example)> bodies)
    {
        page.AppendLine($"## Bodies — {bodies.Count} pictures");
        page.AppendLine();
        page.AppendLine("Every enemy, elite and boss in the game. No visual canon was written for the bodies, so");
        page.AppendLine("the row carries what the fight itself says: how much it can take, how many rooms use it,");
        page.AppendLine("and one of those rooms by name. The role is what the picture has to BE — a body that is");
        page.AppendLine("ever met in a boss room is drawn as a boss, even if it also turns up as filler.");
        page.AppendLine();
        foreach (var role in new[] { EnemyRole.Boss, EnemyRole.Elite, EnemyRole.Mimic, EnemyRole.Standard })
        {
            var members = bodies.Where(b => b.Role == role).ToList();
            if (members.Count == 0)
                continue;
            page.AppendLine($"### {char.ToUpperInvariant(role[0])}{role[1..]} bodies — {members.Count}");
            page.AppendLine();
            page.AppendLine("| code | title | HP | rooms | met in |");
            page.AppendLine("|---|---|---:|---:|---|");
            foreach (var body in members)
            {
                // The name the FRONTEND will print over the picture, which is the document's, not the
                // source data's — they are the same string today and this is where it would show if they
                // ever stopped being.
                var shown = blueprint.Presentation.Enemies.GetValueOrDefault(body.Id)?.FlavorText ?? body.Name;
                page.AppendLine($"| `{body.Id}` | {Cell(shown)} | {body.Health} | {body.Fights} | {Cell(body.Example)} |");
            }
            page.AppendLine();
        }
    }

    private static void CardTables(
        StringBuilder page, RunBlueprint blueprint, BabData data, IReadOnlySet<string> slots)
    {
        page.AppendLine($"## Cards — {slots.Count} pictures");
        page.AppendLine();
        page.AppendLine("No visual canon was ever written for the cards, so the row carries the card's own rules");
        page.AppendLine("text instead of a brief. Act = the act that unlocks it; the starters, the Junk and the");
        page.AppendLine("cards a boss or a door hands over are never offered and have no act.");
        page.AppendLine();

        var printed = new HashSet<string>(StringComparer.Ordinal);
        var groups = new (string Heading, IReadOnlyList<BnbCard> Cards)[]
        {
            ("Starters and Junk", [.. BureaucratStarter.All()]),
            ("Bureaucrat — Act I", [.. BureaucratActI.All()]),
            ("Bureaucrat — Act II", [.. BureaucratActII.All()]),
            ("Bureaucrat — Act III", [.. BureaucratActIII.All()]),
            ("Bureaucrat — Act IV", ActIVCards.Bureaucrat()),
            ("General — Act I", [.. GeneralActI.All()]),
            ("General — Act II", [.. GeneralActII.All()]),
            ("General — Act III", [.. GeneralActIII.All()]),
            ("General — Act IV", ActIVCards.General()),
        };

        foreach (var (heading, cards) in groups)
        {
            var members = cards.Where(c => slots.Contains(c.Id)).ToList();
            if (members.Count == 0)
                continue;
            page.AppendLine($"### {heading} — {members.Count}");
            page.AppendLine();
            page.AppendLine("| code | title | type | rarity | what it does |");
            page.AppendLine("|---|---|---|---|---|");
            foreach (var card in members.OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                printed.Add(card.Id);
                page.AppendLine($"| `{card.Id}` | {Cell(card.Name)} | {card.Type} | {card.Rarity} | {Cell(card.Text)} |");
            }
            page.AppendLine();
        }

        // Whatever the document ships that no reward sheet wrote: the cards a boss's clause or a door's
        // consequence puts into the deck mid-run. They are seen in hand like any other card, so they need a
        // picture like any other card — with one exception, split off below.
        var ported = data.Cards.Select(c => CardMapper.MapCardId(c.Id)).ToHashSet(StringComparer.Ordinal);
        var rest = blueprint.Cards.Where(c => slots.Contains(c.Id) && !printed.Contains(c.Id))
            .OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        Rest(page, blueprint, "Given in play", [.. rest.Where(c => !ported.Contains(c.Id))],
            "Handed over by a boss, a door or an event rather than offered; never in a reward pool.");
        Rest(page, blueprint, "Ported v2 remnants", [.. rest.Where(c => ported.Contains(c.Id))],
            "⚠ **Paint these last, or not at all.** They are the demo game's cards, still shipped only because "
            + "ported events name them; they leave when those events are replaced.");
    }

    private static void Rest(
        StringBuilder page, RunBlueprint blueprint, string heading,
        IReadOnlyList<RogueDeck.Scenario.Authoring.CardData> rest, string note)
    {
        if (rest.Count == 0)
            return;
        page.AppendLine($"### {heading} — {rest.Count}");
        page.AppendLine();
        page.AppendLine(note);
        page.AppendLine();
        page.AppendLine("| code | title | what it does |");
        page.AppendLine("|---|---|---|");
        foreach (var card in rest)
        {
            // A boss's clause carries its rules text on the card; a ported one carries it only in the
            // presentation manifest, which is exactly where the frontend reads it from too.
            var text = string.IsNullOrWhiteSpace(card.DescriptionKey)
                ? blueprint.Presentation.Cards.GetValueOrDefault(card.Id)?.FlavorText ?? ""
                : card.DescriptionKey;
            page.AppendLine($"| `{card.Id}` | {Cell(card.NameKey ?? card.Id)} | {Cell(text)} |");
        }
        page.AppendLine();
    }

    // The two visual-design canons, read as one catalogue: "### 12. Archive Key" opens an entry and
    // "- **Object / silhouette:** …" is the line that says what to draw. Matched to the relics by TITLE,
    // because the catalogue numbers the objects and the code names the file — the title is the only thing
    // both of them carry.
    private static IReadOnlyDictionary<string, (int Number, string Brief)> Canon(string dataDir)
    {
        var found = new Dictionary<string, (int, string)>(StringComparer.Ordinal);
        foreach (var doc in new[]
                 {
                     "BnB_Final_Relics_Master_PostAudit_VISUAL_DESIGN_CANON.md",
                     "BnB_Elite_Relics_MASTER_AND_VISUAL_CANON.md",
                 })
        {
            var path = Path.Combine(dataDir, "design", doc);
            if (!File.Exists(path))
                continue;
            var number = 0;
            var title = "";
            foreach (var line in File.ReadLines(path))
            {
                if (Entry().Match(line) is { Success: true } entry)
                {
                    number = int.Parse(entry.Groups[1].Value);
                    title = entry.Groups[2].Value.Trim();
                    found[title] = (number, "");
                }
                else if (title.Length > 0 && Object().Match(line) is { Success: true } brief)
                {
                    found[title] = (number, brief.Groups[1].Value.Trim());
                }
            }
        }
        return found;
    }

    private static string Cell(string text) => text.Replace("|", "\\|").Replace("\n", " ");

    [GeneratedRegex(@"^### (\d+)\. (.+)$")]
    private static partial Regex Entry();

    [GeneratedRegex(@"^- \*\*Object / silhouette:\*\* (.+)$")]
    private static partial Regex Object();
}
