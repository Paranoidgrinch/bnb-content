namespace BnbContent.Converter;

// THE COMPENDIUM'S WORDS. The player asked for a wiki in the pause and main menu "where every effect and status in
// the game is explained in simple language, with an example" (playtest 2026-09-26). A status's RULE is already in
// the document (StatusData.Description, which every hover shows); what a rule does not say is how it FEELS in a
// turn. That is authored here, once per term, and travels in `Presentation.Game.Extra` under three keys a frontend
// reads without knowing anything about this game:
//
//   compendium:<id>          the term's name (the id is a status id, or a concept's own id)
//   compendium.plain:<id>    one sentence of plain words
//   compendium.example:<id>  one worked example, with numbers
//
// A CONCEPT is a rule of the game that is not a status (Block, Energy, Exhaust, a Queue card…) and is listed only
// here. A status the compendium has no words for still gets an entry in the frontend from its own name and rule —
// the words here are what the common ones deserve on top of that, not a gate on being listed.
public static class Compendium
{
    public sealed record Entry(string Id, string Name, string Plain, string Example, bool Concept = false);

    public static readonly IReadOnlyList<Entry> Entries =
    [
        // ── the ground rules ────────────────────────────────────────────────────────────────────────────────
        new("energy", "Energy",
            "What you spend to play cards. You get it back at the start of every turn.",
            "You have 3 Energy. Paper Cut costs 1 and Permit A38 costs 2: you can play both this turn, and then nothing else.",
            Concept: true),
        new("block", "Block",
            "Armour for one round. Damage takes Block away before it takes HP, and Block you did not use is gone at the start of your next turn.",
            "You have 7 Block and an enemy attacks for 10: the Block is used up and you lose 3 HP.",
            Concept: true),
        new("intent", "Intent",
            "What an enemy will do on its next turn, shown under its health bar: the move's name, then what it does.",
            "\"Enter the Premises · ⚔ 9\" means that enemy will attack you for 9 when you end your turn.",
            Concept: true),
        new("exhaust", "Exhaust",
            "A card that Exhausts leaves your deck for the rest of the fight after it is played. It comes back next fight.",
            "Candle Allowance says Exhaust: play it once, and it will not be drawn again until the next combat.",
            Concept: true),
        new("rite", "Rite",
            "A card that sets up a lasting effect for the rest of the fight. Rites Exhaust, so each one works once per fight.",
            "Play Ash Register: from now on, the first card you Archive each turn draws you a card — until the fight ends.",
            Concept: true),
        new("deed", "Deed",
            "A card whose main job is to hurt an enemy. Some effects only care about Deeds.",
            "Paper Cut is a Deed. Against a Ratified enemy, each Deed deals 3 more damage.",
            Concept: true),
        new("working", "Working",
            "A card that defends, sets things up or bends the rules, rather than dealing damage directly.",
            "Cower Behind a Desk is a Working: it gives you Block and never touches the enemy.",
            Concept: true),
        new("queue", "Queue",
            "A Queue card does not happen when you play it — it is filed, and resolves later, in the order it was filed.",
            "Deferred Hex says \"Queue: Deal 13 damage.\" You play it now for 1 Energy; the 13 damage lands when the queue reaches it.",
            Concept: true),
        new("archive", "Archive",
            "To Archive a card is to take it out of your hand and put it away for the fight, on purpose. Some cards and relics reward you for it.",
            "Certified Kindling: Archive a card from your hand and gain 4 Block. Archive a Junk card and you gain 4 more.",
            Concept: true),
        new("junk", "Junk",
            "Useless paperwork that clogs your deck. Some enemies and events add it; Archiving it is often the best use.",
            "An enemy adds Red Tape to your discard pile. When you draw it, it takes a slot in your hand and cannot be played.",
            Concept: true),
        new("retain", "Retain",
            "A retained card stays in your hand at the end of your turn instead of being discarded.",
            "Keep a Retain card for the turn the big attack comes, and play it then.",
            Concept: true),
        new("upgrade", "Improved cards",
            "An improved card (a \"+\" after its name) is a stronger version of itself. Right-click a card in a shop, a reward or your deck to see its improved form.",
            "Form of Ill Intent applies 3 Paperwork; Form of Ill Intent+ applies 4.",
            Concept: true),

        // ── the common statuses ─────────────────────────────────────────────────────────────────────────────
        new("paperwork", "Paperwork",
            "Damage over time that goes straight past Block and never wears off on its own.",
            "An enemy with 5 Paperwork loses 5 HP at the end of each of its turns — every turn, until the fight ends.", false),
        new("doubt", "Doubt",
            "Makes attacks weaker: each attack with Doubt deals a quarter less damage and uses up one Doubt.",
            "An enemy with 2 Doubt attacks for 12: it deals 9, and has 1 Doubt left for its next attack."),
        new("seal", "Seal",
            "Collects towards Ratified: at 3 Seal, 3 are used up and the enemy becomes Ratified.",
            "An enemy has 2 Seal and you apply 2 more: it is Ratified, and 1 Seal is left over for next time."),
        new("ratified", "Ratified",
            "Until the end of your turn, every Deed aimed at a Ratified enemy deals 3 more damage.",
            "Ratify an enemy, then play three Paper Cuts at it: each deals 6 + 3 = 9 instead of 6."),
        new("citation", "Citation",
            "Punishes an enemy for doing anything that is not an attack: it loses HP equal to its Citation, then one Citation.",
            "An enemy with 3 Citation blocks instead of attacking: it loses 3 HP and drops to 2 Citation."),
        new("lien", "Lien",
            "Eats an enemy's leftover Block at the end of its turn, and takes the same amount of HP.",
            "An enemy has 4 Lien and ends its turn with 6 Block: it loses 4 Block and 4 HP, and its Lien falls to 0."),
        new("strength", "Strength",
            "Each stack makes every attack of that character deal 1 more damage.",
            "An enemy with 2 Strength whose move says ⚔ 6 hits you for 8."),
        new("panic", "Panic",
            "You draw fewer cards: one fewer per stack at the start of your turn. One stack fades each turn.",
            "With 2 Panic you draw 3 cards instead of 5 next turn, and 4 the turn after."),
        new("burdened", "Burdened",
            "Every card costs 1 more Energy. Each time you pay that extra, one stack is worked off.",
            "With 2 Burdened, your first two cards each cost 1 more; after that, cards cost their normal price again."),
        new("fatigue", "Fatigue",
            "You start your turn with 1 less Energy. One stack fades each turn.",
            "With 1 Fatigue you have 2 Energy next turn instead of 3."),
        new("poison", "Poison",
            "Damage over time that ignores Block, then shrinks by one each turn.",
            "5 Poison: you lose 5 HP, then 4, then 3 … over the next turns."),
        new("inscribed", "Inscribed",
            "The next status put on you — good or bad — lands one stack bigger, and one Inscribed is used up.",
            "You have 1 Inscribed and an enemy gives you 2 Doubt: you get 3 Doubt instead."),
        new("trespass", "Trespass",
            "A tally of rules you broke. It does no damage by itself, but at 3 from the same enemy it gives that enemy a Claim.",
            "You play a card the law forbids three times: that enemy takes your 3 Trespass and gains a Claim."),
        new("safe_conduct", "Safe-Conduct",
            "A pass that cancels one Trespass before it lands.",
            "With 1 Safe-Conduct, the next Trespass put on you is refused and the Safe-Conduct is spent."),
        new("claim", "Claim",
            "Standing that a side has earned. What a Claim does depends on who holds it — read that enemy's rules.",
            "An enemy with 2 Claims may hit harder or protect itself, as its own rules say. Nobody holds more than 3."),
        new("overdue", "Overdue",
            "A debt to whoever gave it to you. At 2 from the same source, that source collects its price.",
            "An enemy gives you Overdue twice: at 2 it collects, as its rules describe."),
        new("wergild", "Wergild",
            "A debt with a deadline: pay it off by the end of your next turn, or each point left costs you 2 HP.",
            "You owe 3 Wergild and pay 1 with Make Amends: the 2 you still owe cost you 4 HP when they fall due."),
        new("entombed", "Entombed",
            "Burial pressure that builds up. At 5 you lose your whole next turn, and 5 are used up.",
            "You are at 4 Entombed and an enemy adds 1: your next turn is skipped."),
        new("weighed", "Weighed",
            "This turn you must spend exactly this much Energy. The further you are from it, the worse the answer.",
            "Weighed 3 and you spend only 2 Energy: you missed the measure by 1."),
        new("ward_wax", "Ward Wax",
            "Gives you Block at the start of each turn. It shrinks after the enemy turn — faster if an attack got through.",
            "With 4 Ward Wax you start your turn with 4 extra Block."),
        new("blood_ink", "Blood Ink",
            "Whenever any other status on this character shrinks, it loses HP equal to its Blood Ink, then one Blood Ink.",
            "An enemy with 3 Blood Ink uses up a Doubt: it loses 3 HP, and has 2 Blood Ink left."),
        new("embalmed", "Embalmed",
            "Protects the other statuses: when one would fade on its own, an Embalmed is spent instead and the status stays.",
            "An enemy with 1 Embalmed and 2 Panic keeps both Panic at the end of the turn, and loses the Embalmed."),
        new("archived", "Archived",
            "Counts how many cards you have Archived this fight. Some cards grow stronger with it.",
            "You have Archived 3 cards: a card that deals \"5 damage for each card you have Archived\" deals 15."),
    ];

    public static IReadOnlyDictionary<string, string> Extra() =>
        Entries.SelectMany(e => new[]
            {
                KeyValuePair.Create($"compendium:{e.Id}", e.Name),
                KeyValuePair.Create($"compendium.plain:{e.Id}", e.Plain),
                KeyValuePair.Create($"compendium.example:{e.Id}", e.Example),
            }.Concat(e.Concept ? [KeyValuePair.Create($"compendium.concept:{e.Id}", "true")] : []))
            .ToDictionary(p => p.Key, p => p.Value);
}
