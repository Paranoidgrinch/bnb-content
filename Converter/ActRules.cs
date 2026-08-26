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
    public required string RestText { get; init; }
    public required string RestChoiceText { get; init; }
    public required string TreasureText { get; init; }
    public required string TreasureOpenText { get; init; }
    public required string TreasureLeaveText { get; init; }

    public static ActRules For(BabActManifest act) => act.Act switch
    {
        1 => City,
        2 => Archives,
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
        RestText = "The waiting room. The chairs are terrible, but nobody can reach you here.",
        RestChoiceText = "Wait it out",
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
        RestText = "A reading alcove behind the returns desk. The lamp works, and the shelf above you has not "
            + "moved once in the hour you have been watching it.",
        RestChoiceText = "Sit until the shelf gives up",
        TreasureText = "A returns trolley nobody has emptied. The bottom shelf is still checked out to someone, "
            + "and the card says the loan period has not started yet.",
        TreasureOpenText = "Check the bottom shelf",
        TreasureLeaveText = "Push it back into the dark",
    };
}
