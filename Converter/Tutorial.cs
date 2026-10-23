using RogueDeck.Run;

namespace BnbContent.Converter;

// THE TUTORIAL (user, 2026-09-26: "ein tutorial …, wo der spieler alles einmal gezeigt bekommt und erklaert
// bekommt"; 2026-10-23: one per character, offered the first time it is played, "nicht länger als 5 min").
// Not a second game: three rooms of the city, walked in a fixed order on the real content, each paying what its
// role pays in a real run — the payload of every room is realised by the same MapNodeRealizer, from the same
// Act-I spec, that a generated map uses. What is taught is the character and the fight:
//
//   1. an easy fight against one enemy          — the hand, Energy, an intent, Block, ending the turn
//   2. a fight against two                       — aiming a card, statuses, the reward
//   3. the waiting room                          — rest or improve
//
// Events, shops and elites were rooms of the first tutorial (six rooms, well over five minutes); they explain
// themselves on the map and are left to the first run.
//
// The coach's words for each moment ride in Presentation.Game.Extra under "tutorial:<moment>" (title) and
// "tutorial.text:<moment>" (text); the frontend decides WHEN a moment has come, the game says what it means.
public static class Tutorial
{
    private static readonly (MapNodeKind Kind, string? Encounter, string? Ref)[] Rooms =
    [
        (MapNodeKind.Combat, "city_easy_queue_01", null),
        (MapNodeKind.MultiCombat, "city_easy_counter_04", null),
        (MapNodeKind.Rest, null, null),
    ];

    public static RunTutorial Build(MapGenerationSpec cityRules, RunStart start)
    {
        var nodes = Rooms.Select((room, index) =>
        {
            var content = MapNodeRealizer.Realize(
                cityRules, room.Kind, room.Encounter is { } id ? new EncounterId(id) : null, room.Ref);
            return new Node(new NodeId($"t{index}"), content.Type, content.Payload, content.Tags);
        }).ToList();

        // A learner's body: enough health that a mistake teaches rather than ends the lesson.
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

    // Her learner's deck carries one Fortune card: her starter has none, and Misfortune is a third of her lesson.
    public static RunStart WitchLearner(RunStart her) =>
        her with { Deck = [.. her.Deck, new RogueDeck.Core.Combat.CardDefinitionId("black_cat")] };

    // The Lieutenant's carries Armguard: her starter never spends Strength, and spending it is half of her.
    public static RunStart LieutenantLearner(RunStart her) =>
        her with { Deck = [.. her.Deck, new RogueDeck.Core.Combat.CardDefinitionId("lt_armguard")] };

    // The coach's script, moment by moment, in the order a player meets them.
    public static readonly IReadOnlyList<(string Moment, string Title, string Text)> Steps =
    [
        ("welcome", "Welcome to the Old City Offices",
            "Three short rooms — two fights and a rest — show you how this character plays. Follow the notes; you " +
            "can skip the tutorial at any time."),
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
        ("combat.multiple", "More than one enemy",
            "Now there are two. Pick a card, then click the enemy you want it to hit. The arrow keys aim, Space " +
            "plays."),
        ("rest", "The waiting room",
            "Rest to heal, or improve one card for good. If nothing can be improved, the button says so."),
        ("compendium", "The compendium",
            "Every effect and status in the game is explained in the Compendium, with an example. Open it from " +
            "the title screen or with Esc during a run."),
        ("complete", "Ready for a real run",
            "That is how this character fights. A real run is five acts: you choose your path on the map, and meet " +
            "events (?), shops, elites and a boss at the end of every act — each explains itself. Good luck."),
    ];

    // THE HEDGE WITCH'S LESSON (user 2026-10-04, plan E9). The same six rooms, walked as her: everything above is
    // still true and is still told, and these are told beside it — the moments the frontend detects only while the
    // hero has a cauldron. What she does that he does not: cook instead of play, brew, shelter, and the three
    // things her families do to an enemy and to herself.
    public static readonly IReadOnlyList<(string Moment, string Title, string Text)> WitchSteps =
    [
        ("witch.cauldron", "The Cauldron",
            "Beside your cards you have two actions. Into the Pot puts a card from your hand into the cauldron " +
            "instead of playing it — the first each turn is free, every further one costs 1 Energy. A card in the " +
            "pot is not played."),
        ("witch.shelter", "Sheltering",
            "While the cauldron is empty, you shelter behind its lid: cards like Pot-Lid give more Block then. The " +
            "pot's label says its state — Sheltering, Brewing or Ready."),
        ("witch.families", "Ingredients",
            "Every one of your cards is an ingredient of one family: Fang (damage), Hex (Hexed), Husk (Block), " +
            "Hearth (healing) or Fortune (Misfortune). Its chip in the pot wears its family's colour."),
        ("witch.brew", "Brew",
            "With three in the pot, Brew (1 Energy). Three of one family make a strong concentrated brew; any other " +
            "mix gives what each ingredient gives. The pot shows what it is about to make. Then the ingredients go " +
            "to your discard pile and the pot is empty again."),
        ("witch.hexed", "Hexed",
            "Hexed does not fade. Every third enemy turn it bursts: the enemy loses 3 HP for each Hexed. Pile it on " +
            "before the third night."),
        ("witch.misfortune", "Misfortune",
            "Misfortune is a chance that the enemy's next action fails: 5 % per stack, at most 60 %. It is rolled " +
            "once, before that action, and then it is gone."),
        ("witch.hearth", "Hearth healing",
            "Your healing only gives back HP lost in THIS fight — never more than you began it with. Heal when it " +
            "hurts, not before."),
        ("witch.recipes", "The Recipe Book",
            "Some exact mixes of three cards are Hidden Recipes with effects of their own. Brew one and it is " +
            "written into your Recipe Book — the Recipes button beside the pot."),
    ];

    // THE SORCERER'S APPRENTICE'S LESSON: told only while the hero carries a wand, each the first time the fight
    // shows it.
    public static readonly IReadOnlyList<(string Moment, string Title, string Text)> ApprenticeSteps =
    [
        ("ap.wand", "The Wand",
            "Beside your cards are the wand's actions. Inscribe puts a card from your hand into a wand slot instead of " +
            "playing it; it stays there. Cast runs the whole wand, slot by slot, each card doing its Wand part."),
        ("ap.focus", "Focus",
            "The wand runs on Focus. Every card you play gives 1 Focus (at most 6), kept from turn to turn. A cast " +
            "costs 2 Focus for one or two wand cards, more for longer wands."),
        ("ap.cast", "Casting",
            "Your wand holds a card: Cast it. Modifiers like Heighten change the Speak after them, so the order of the " +
            "slots matters — the preview shows what the cast will do."),
        ("ap.exhaustion", "Exhaustion",
            "Each cast adds Exhaustion. At the limit the wand will not cast; it falls by 3 at the start of your turn. " +
            "Burn Out empties a slot when you need room."),
        ("ap.elements", "The four elements",
            "Singe on an enemy makes each direct hit deal more and is spent; Grit softens enemy hits; Flow refunds " +
            "Focus after a cast; Gust lets you draw and discard next turn."),
    ];

    // THE LIEUTENANT'S LESSON: told only while the hero carries her command, each the first time the fight shows it.
    public static readonly IReadOnlyList<(string Moment, string Title, string Text)> LieutenantSteps =
    [
        ("lt.authority", "Authority",
            "You earn Authority by keeping your word — each kept Order or Challenge gives 1. Cards like Signal spend " +
            "it. You hold at most 6, and it is gone when the fight ends."),
        ("lt.order", "An Order",
            "An Order is a promise about your own actions, shown on you: what it asks and by when. First Orders: play " +
            "a Deed later this turn. The card that issues it never counts."),
        ("lt.challenge", "A Challenge",
            "A Challenge is a promise about one enemy, shown on it — most must be kept before that enemy acts. Watch " +
            "Cut deals 2 more to a Challenged enemy."),
        ("lt.open_guard", "Open Guard",
            "Open Guard is risk you take on purpose: the next enemy hit deals 2 more per stack, before Block, and " +
            "spends it. Enough Block makes it cost nothing."),
        ("lt.strength", "Spending Strength",
            "Strength makes every hit stronger — and some of your cards can spend it for something else, like " +
            "Armguard's Ward Wax. Only Strength you really have can be spent."),
    ];

    public static IReadOnlyDictionary<string, string> Extra() =>
        Steps.Concat(WitchSteps).Concat(ApprenticeSteps).Concat(LieutenantSteps).SelectMany((step, index) => new[]
        {
            KeyValuePair.Create($"tutorial:{step.Moment}", step.Title),
            KeyValuePair.Create($"tutorial.text:{step.Moment}", step.Text),
            KeyValuePair.Create($"tutorial.order:{step.Moment}", index.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        }).ToDictionary(p => p.Key, p => p.Value);
}
