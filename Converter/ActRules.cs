using RogueDeck.Run;

namespace BnbContent.Converter;

// What one act's MAP is like, as opposed to what its fights are: how much of each thing a route through it must
// hold, how much it may hold, which flavours its columns come in, and how its own stops read. The numbers are
// the audit's (docs/bnb-act-map-specs.md); the lanes and the room texts are this act's own voice.
//
// Everything else — gold per role, rest percentage, shop prices — is deliberately NOT here: the design leaves
// those to the balance pass (BnB_Run_Systems_Master.md), and inventing per-act numbers now would pre-empt it.
internal sealed record ActRules
{
    public required IReadOnlyDictionary<MapNodeKind, int> PerPathMinimums { get; init; }
    public required IReadOnlyDictionary<MapNodeKind, int> PerPathMaximums { get; init; }
    public required IReadOnlyList<MapLaneProfile> Lanes { get; init; }
    public required IReadOnlyDictionary<MapNodeKind, int> KindWeights { get; init; }

    // How deep into the act each kind of room may FIRST stand, as a percentage of the act's own depth. The
    // per-path table says how much of each thing a route holds; it says nothing about where, and without this
    // the answer is the gate order — which put a shop in the opening row (where nobody has any gold) and an
    // elite in the fourth (with the starting deck). Kinds not named here may stand anywhere.
    public required IReadOnlyDictionary<MapNodeKind, int> EarliestDepthPercent { get; init; }
    public required string RestText { get; init; }
    public required string RestChoiceText { get; init; }

    // The campfire's SECOND action (BnB_Run_Systems_Master §3: a waiting room offers Authorized Leave *or*
    // Submit an Amendment). Upgrading a card is the same act in every act; only the room's voice changes.
    public required string RestUpgradeChoiceText { get; init; }
    public required string TreasureText { get; init; }
    public required string TreasureOpenText { get; init; }
    public required string TreasureLeaveText { get; init; }

    public static ActRules For(BabActManifest act) => act.Act switch
    {
        1 => City,
        2 => Archives,
        3 => GreenDocket,
        var other => throw new ConversionException($"act '{act.Id}'", $"no map rules are authored for act {other}"),
    };

    // ── Act I: The Old City Offices ────────────────────────────────────────────────
    // Per-path guarantees from the audit. Combat 8 counts ordinary fights; the duo is a MultiCombat on top, and
    // the enemy floor counts both plus the elite.
    private static readonly ActRules City = new()
    {
        PerPathMinimums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 8,
            [MapNodeKind.MultiCombat] = 1,
            [MapNodeKind.Elite] = 1,
            [MapNodeKind.Event] = 3,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Treasure] = 2,
            [MapNodeKind.Shop] = 2,
        },
        // Ceilings: no single route may pile up the soft stuff. A path is guaranteed its two rests, two shops
        // and two treasures and may hold at most one more of each, so a "safe" route cannot be farmed — and at
        // most two elites, so a greedy one cannot stack them either.
        PerPathMaximums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Rest] = 3,
            [MapNodeKind.Treasure] = 3,
            [MapNodeKind.Shop] = 3,
            [MapNodeKind.Event] = 5,
            [MapNodeKind.Elite] = 2,
            [MapNodeKind.MultiCombat] = 2,
        },
        // The three flavours the act's columns are drawn from, so the routes actually feel different: the left
        // is a gauntlet of fights, the middle runs errands (events and shops), the right is the quiet, well-
        // stocked way round. Which column a path keeps to decides BOTH what it holds and the order it holds it.
        Lanes =
        [
            new("the long queue", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Combat] = 12,
                [MapNodeKind.MultiCombat] = 3,
                [MapNodeKind.Elite] = 2,
                [MapNodeKind.Event] = 2,
            }),
            new("errands", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Event] = 7,
                [MapNodeKind.Shop] = 4,
                [MapNodeKind.Combat] = 5,
                [MapNodeKind.MultiCombat] = 1,
            }),
            new("the quiet corridor", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Rest] = 6,
                [MapNodeKind.Treasure] = 5,
                [MapNodeKind.Combat] = 5,
                [MapNodeKind.Event] = 2,
            }),
        ],
        // Only used if the lanes above are ever cleared: the act's overall flavour in one table.
        KindWeights = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 10,
            [MapNodeKind.Event] = 4,
            [MapNodeKind.Treasure] = 2,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Shop] = 1,
            [MapNodeKind.Elite] = 1,
        },
        // The city eases you in: the first rooms are fights and doors, the shop opens once a fight or two has
        // paid for it, the duo and the elite wait until the deck has had a chance to become one.
        EarliestDepthPercent = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Shop] = 12,
            [MapNodeKind.Rest] = 10,
            [MapNodeKind.MultiCombat] = 20,
            [MapNodeKind.Elite] = 35,
        },
        RestText = "The waiting room. The chairs are terrible, but nobody can reach you here.",
        RestChoiceText = "Wait it out",
        RestUpgradeChoiceText = "Submit an amendment",
        TreasureText = "A sealed evidence crate, stamped in three colors of wax. Nobody has claimed it in decades.",
        TreasureOpenText = "Break the seals",
        TreasureLeaveText = "Leave it for the archivists",
    };

    // ── Act II: The Endless Archives ───────────────────────────────────────────────
    // The archives ask for more fighting and less comfort: two multi-enemy fights and two elites per route
    // against the city's one of each, and ONE guaranteed treasure instead of two.
    private static readonly ActRules Archives = new()
    {
        PerPathMinimums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 8,
            [MapNodeKind.MultiCombat] = 2,
            [MapNodeKind.Elite] = 2,
            [MapNodeKind.Event] = 3,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Treasure] = 1,
            [MapNodeKind.Shop] = 2,
        },
        // The ceilings move with the floors: one spare rest, shop and treasure as in the city, but a route may
        // take a third elite here — the greedy way through the stacks is a real option, not a rounding error.
        PerPathMaximums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Rest] = 3,
            [MapNodeKind.Treasure] = 2,
            [MapNodeKind.Shop] = 3,
            [MapNodeKind.Event] = 5,
            [MapNodeKind.Elite] = 3,
            [MapNodeKind.MultiCombat] = 3,
        },
        // The archives' three ways through: down the shelves themselves, along the desks where the staff still
        // pretend to work, or through the reading rooms nobody has swept in years.
        Lanes =
        [
            new("the deep stacks", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Combat] = 12,
                [MapNodeKind.MultiCombat] = 4,
                [MapNodeKind.Elite] = 3,
                [MapNodeKind.Event] = 2,
            }),
            new("the reference desks", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Event] = 7,
                [MapNodeKind.Shop] = 4,
                [MapNodeKind.Combat] = 5,
                [MapNodeKind.MultiCombat] = 2,
            }),
            new("the reading rooms", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Rest] = 6,
                [MapNodeKind.Treasure] = 3,
                [MapNodeKind.Combat] = 5,
                [MapNodeKind.Event] = 3,
            }),
        ],
        KindWeights = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 10,
            [MapNodeKind.Event] = 4,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Elite] = 2,
            [MapNodeKind.Treasure] = 1,
            [MapNodeKind.Shop] = 1,
        },
        // The archives are less patient than the city: the elites start earlier, because by Act II the deck is
        // a deck. The shop still waits for the first fight to pay for it.
        EarliestDepthPercent = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Shop] = 10,
            [MapNodeKind.Rest] = 10,
            [MapNodeKind.MultiCombat] = 12,
            [MapNodeKind.Elite] = 22,
        },
        RestText = "A reading alcove behind the returns desk. The lamp works, and the shelf above you has not "
            + "moved once in the hour you have been watching it.",
        RestChoiceText = "Sit until the shelf gives up",
        RestUpgradeChoiceText = "Amend a filing while nobody is looking",
        TreasureText = "A returns trolley nobody has emptied. The bottom shelf is still checked out to someone, "
            + "and the card says the loan period has not started yet.",
        TreasureOpenText = "Check the bottom shelf",
        TreasureLeaveText = "Push it back into the dark",
    };

    // ── Act III: The Green Docket ──────────────────────────────────────────────────
    // The road out of the archives. The audit asks for three elites a route rather than two and keeps
    // everything else where Act II left it: eight fights, two of them crowded, one treasure, two shops.
    private static readonly ActRules GreenDocket = new()
    {
        PerPathMinimums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 8,
            [MapNodeKind.MultiCombat] = 2,
            [MapNodeKind.Elite] = 3,
            [MapNodeKind.Event] = 3,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Treasure] = 1,
            [MapNodeKind.Shop] = 2,
        },
        // Out here the soft rooms are what is scarce: a route may find one spare rest and one spare shop and
        // no spare treasure at all, because there is nothing on this road that keeps anything. What it may
        // pile up instead is trouble — a fourth elite and a fourth crowded fight are both allowed.
        PerPathMaximums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Rest] = 3,
            [MapNodeKind.Treasure] = 2,
            [MapNodeKind.Shop] = 3,
            [MapNodeKind.Event] = 6,
            [MapNodeKind.Elite] = 4,
            [MapNodeKind.MultiCombat] = 4,
        },
        // Three ways across the same country: the old road that everything with a right to it is standing on,
        // the hedgeways where the doors are, and the long way round through the water meadows.
        Lanes =
        [
            new("the old road", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Combat] = 12,
                [MapNodeKind.MultiCombat] = 4,
                [MapNodeKind.Elite] = 4,
                [MapNodeKind.Event] = 2,
            }),
            new("the hedgeways", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Event] = 8,
                [MapNodeKind.Shop] = 3,
                [MapNodeKind.Combat] = 5,
                [MapNodeKind.MultiCombat] = 2,
            }),
            new("the water meadows", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Rest] = 6,
                [MapNodeKind.Treasure] = 3,
                [MapNodeKind.Combat] = 5,
                [MapNodeKind.Event] = 3,
            }),
        ],
        KindWeights = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 10,
            [MapNodeKind.Event] = 5,
            [MapNodeKind.Elite] = 3,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Treasure] = 1,
            [MapNodeKind.Shop] = 1,
        },
        // The road has no patience left at all: its elites stand almost from the start, and its shops are
        // carts, which are wherever they happen to be. What it does keep back is the crowded fight — being
        // surrounded on open ground is the act's own threat and it is not the first thing you meet.
        EarliestDepthPercent = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Shop] = 8,
            [MapNodeKind.Rest] = 10,
            [MapNodeKind.MultiCombat] = 15,
            [MapNodeKind.Elite] = 18,
        },
        RestText = "A hollow out of the wind, with a stone somebody has sat on often enough to wear it. "
            + "Nothing here has a right to you for as long as you stay off the road.",
        RestChoiceText = "Sit out of the wind",
        RestUpgradeChoiceText = "Put a filing in order by daylight",
        TreasureText = "A boundary cairn with a hollow in it, and something in the hollow that was left for "
            + "whoever came next. The stones around it have been counted recently.",
        TreasureOpenText = "Take what was left",
        TreasureLeaveText = "Add a stone and walk on",
    };
}
