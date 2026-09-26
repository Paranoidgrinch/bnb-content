using RogueDeck.Run;

namespace BnbContent.Converter;

// THE TUTORIAL (user, 2026-09-26: "ein tutorial …, wo der spieler alles einmal gezeigt bekommt und erklaert
// bekommt"). Not a second game: six rooms of the city, walked in a fixed order on the real content, each paying
// what its role pays in a real run — the payload of every room is realised by the same MapNodeRealizer, from the
// same Act-I spec, that a generated map uses. What is taught is what the player will meet:
//
//   1. an easy fight against one enemy          — the hand, Energy, an intent, Block, ending the turn
//   2. a simple event                            — reading a door and what each branch does
//   3. a fight against two                       — aiming a card, statuses, the reward
//   4. the shop                                  — buying, removing a card, right-click to see an upgrade
//   5. the waiting room                          — rest or improve
//   6. an elite with a rule of its own           — the rules plates, a relic as the prize
//
// The coach's words for each moment ride in Presentation.Game.Extra under "tutorial:<moment>" (title) and
// "tutorial.text:<moment>" (text); the frontend decides WHEN a moment has come, the game says what it means.
public static class Tutorial
{
    private static readonly (MapNodeKind Kind, string? Encounter, string? Ref)[] Rooms =
    [
        (MapNodeKind.Combat, "city_easy_queue_01", null),
        (MapNodeKind.Event, null, "clerks_tea_break"),
        (MapNodeKind.MultiCombat, "city_easy_counter_04", null),
        (MapNodeKind.Shop, null, null),
        (MapNodeKind.Rest, null, null),
        (MapNodeKind.Elite, "city_elite_enforcement_01", null),
    ];

    public static RunTutorial Build(MapGenerationSpec cityRules, RunStart start)
    {
        var nodes = Rooms.Select((room, index) =>
        {
            var content = MapNodeRealizer.Realize(
                cityRules, room.Kind, room.Encounter is { } id ? new EncounterId(id) : null, room.Ref);
            return new Node(new NodeId($"t{index}"), content.Type, content.Payload, content.Tags);
        }).ToList();

        // A learner's body and purse: enough health that the elite teaches rather than ends the lesson, and
        // gold for the shop to be a shop.
        var learner = start with
        {
            MaxHealth = 100,
            StartingHealth = 100,
            Resources = new Dictionary<string, int>(start.Resources) { ["gold"] = 120 },
            // One relic from the start, so the relics can be shown at work in the first fight: Black Salt Charm
            // guards you as the fight opens, and its tile lights up when it does.
            StartingRelics = [.. start.StartingRelics, "black_salt_charm"],
        };
        return new RunTutorial(new RunMap(nodes), learner);
    }

    // The coach's script, moment by moment, in the order a player meets them.
    public static readonly IReadOnlyList<(string Moment, string Title, string Text)> Steps =
    [
        ("welcome", "Welcome to the Old City Offices",
            "This short run shows you everything once. Follow the notes; you can skip the tutorial at any time."),
        ("map", "The map",
            "This is the act's map. Each room is a symbol — a fight, an event, a shop, a rest — and the legend says " +
            "which is which. In a real run you choose your own path between them; press M at any time to look."),
        ("combat.hand", "Your hand",
            "These are your cards. Click one to play it. A card that needs a target is aimed: click it, then click " +
            "an enemy. Right-click puts it back."),
        ("combat.energy", "Energy",
            "The number with the lightning bolt is your Energy. Each card costs what is printed in its top-left " +
            "corner. You get your Energy back every turn."),
        ("combat.intent", "What the enemy will do",
            "Under every enemy is its intent: the move's name, then what it does. ⚔ 6 means it will hit you for 6 " +
            "when you end your turn. Hover it for details."),
        ("combat.block", "Block",
            "Block cards give you a shield for this round: damage takes Block before HP. \"Incoming\" under your " +
            "health shows how much would get through right now."),
        ("combat.endturn", "Ending your turn",
            "When you have played what you want, press End turn (or E). The enemies act, then you draw a new hand."),
        ("combat.statuses", "Statuses",
            "The small chips under a body are statuses — Paperwork, Doubt and many more. The number is how much. " +
            "Hover any of them to read what it does; a chip lights up when it changes."),
        ("combat.relics", "Relics",
            "The little tiles above you are your relics. They work on their own; a relic lights up when it " +
            "triggers. Hover one to read it."),
        ("reward", "Your reward",
            "After a fight you get gold, and you may take one of three cards into your deck — or skip. A smaller " +
            "deck draws its best cards more often. Right-click a card to see its improved version."),
        ("event", "Events",
            "Rooms marked ? are events. Each choice says in plain words what it will do, under its own line."),
        ("combat.multiple", "More than one enemy",
            "Now there are two. Pick a card, then click the enemy you want it to hit. The arrow keys aim, Space " +
            "plays."),
        ("shop", "The shop",
            "Spend your gold on cards and relics, or pay to have a weak card struck from your deck. Right-click a " +
            "card to see its improved version before you buy."),
        ("rest", "The waiting room",
            "Rest to heal, or improve one card for good. If nothing can be improved, the button says so."),
        ("elite.rules", "An elite and its rules",
            "Elites and bosses have rules of their own. They stand in the middle of the room — read them before " +
            "you play. \"On you\" is a demand placed on you."),
        ("compendium", "The compendium",
            "Every effect and status in the game is explained in the Compendium, with an example. Open it from " +
            "the title screen or with Esc during a run."),
        ("complete", "That is the whole game",
            "You have seen fights, events, the shop, the rest and an elite. A real run is five acts of this. Good " +
            "luck with the paperwork."),
    ];

    public static IReadOnlyDictionary<string, string> Extra() =>
        Steps.SelectMany((step, index) => new[]
        {
            KeyValuePair.Create($"tutorial:{step.Moment}", step.Title),
            KeyValuePair.Create($"tutorial.text:{step.Moment}", step.Text),
            KeyValuePair.Create($"tutorial.order:{step.Moment}", index.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        }).ToDictionary(p => p.Key, p => p.Value);
}
