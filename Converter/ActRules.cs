using RogueDeck.Run;

namespace BnbContent.Converter;

// What one act's MAP is like, as opposed to what its fights are: how long it runs, how much of each thing it
// holds, where in its depth each of those may stand, what a single walk through it must ask of the player,
// which flavours its columns come in, and how its own stops read. The lanes and the room texts are the act's
// own voice; the numbers are measured (docs/strategic-map-generator-plan.md §1, §6).
//
// TWO GENERATORS READ THIS RECORD AND THEY READ DIFFERENT HALVES OF IT (plan §4b). v0.0.0 — the rule-based
// generator, which every run has used so far and which still ships — reads the per-path table and the free-row
// count, and nothing in that half moves any more. v0.0.1 — the strategic generator — reads the act's length,
// its budgets, its depth bands, its path pressure and its fork threshold. The lanes, the weights, the depth
// gates, the boss count and the rooms are read by BOTH, because they are facts about the act rather than about
// a way of laying it out.
//
// Everything else — gold per role, rest percentage, shop prices — is deliberately NOT here: the design leaves
// those to the balance pass (BnB_Run_Systems_Master.md), and inventing per-act numbers now would pre-empt it.
internal sealed record ActRules
{
    // ── What v0.0.0 reads (frozen) ─────────────────────────────────────────────────
    //
    // The per-path table the audit wrote, and the ONLY thing it still configures: the rule-based generator,
    // which ships beside the strategic one so a playtester can walk the same act both ways (plan §4b). These
    // numbers are not the design any more — the budgets below are — but a baseline that quietly changed would
    // be no baseline at all, so nothing here moves, and `Tests/Golden/map-v0.0.0.txt` is what says so.
    public required IReadOnlyDictionary<MapNodeKind, int> PerPathMinimums { get; init; }
    public required IReadOnlyDictionary<MapNodeKind, int> PerPathMaximums { get; init; }

    // The BACKBONE v0.0.0 grows its guarantee rows onto, as opposed to the act's length. Until now this was
    // computed (`Math.Max(5, steps_before_boss − promises.Sum())`) and the floor won for every act in the game
    // — the subtraction has been negative since the per-path table was authored — so the formula described
    // nothing and hid what it produced. It is five, written down, and it stays five.
    public int RuleBasedFreeRows { get; init; } = 5;

    public required IReadOnlyList<MapLaneProfile> Lanes { get; init; }
    public required IReadOnlyDictionary<MapNodeKind, int> KindWeights { get; init; }

    // How deep into the act each kind of room may FIRST stand, as a percentage of the act's own depth. The
    // per-path table says how much of each thing a route holds; it says nothing about where, and without this
    // the answer is the gate order — which put a shop in the opening row (where nobody has any gold) and an
    // elite in the fourth (with the starting deck). Kinds not named here may stand anywhere.
    public required IReadOnlyDictionary<MapNodeKind, int> EarliestDepthPercent { get; init; }

    // ── What v0.0.1 reads ──────────────────────────────────────────────────────────
    //
    // The same act, said the other way round. v0.0.0 is told what EVERY ROUTE must hold and lays full rows to
    // guarantee it; the strategic generator is told what the ACT holds, where in its depth, and how much a
    // single walk through it must ask of the player — and then the routes are allowed to differ, which is the
    // entire point of the rework (docs/strategic-map-generator-plan.md §1.2).

    // EVERY row the act has, its boss rooms included: the number of rooms a route actually walks. The four acts
    // are authored at the lengths they already have (23 / 24 / 25 / 35 — plan §3, "aktlänge beibehalten"), so
    // the rework changes the SHAPE of an act and not its size, and a playtest measures one variable.
    public int Rows { get; init; }

    public StrategicTopologyRules Topology { get; init; } = new();

    // HOW MUCH OF EACH THING THE WHOLE ACT HOLDS. `Target` is what the act aims at, `Min` what it may not fall
    // below, `Max` what it may not exceed; Combat is deliberately absent, because the filler is whatever is
    // left over once everything that was asked for has been placed.
    public IReadOnlyDictionary<MapNodeKind, RoomBudget> RoomBudgets { get; init; } =
        new Dictionary<MapNodeKind, RoomBudget>();

    // …AND WHERE IN THE ACT. Four quarters, and what each may and must hold: the budgets alone would happily
    // put every elite in the last third and every campfire in the first, which is an act with a shape nobody
    // authored. A band never relaxes a depth gate — it only narrows.
    public IReadOnlyList<DepthBandBudget> DepthBands { get; init; } = [];

    // WHAT ONE ROUTE MUST ASK OF THE PLAYER, in points (Combat 10, MultiCombat 15, Elite 25 — the engine's
    // default table is BnB's). This is what replaces the per-path minimums: not "every route holds two elites"
    // but "every route is worth this much trouble, however it comes by it".
    public PathPressureRules PathPressure { get; init; } = new();

    // WHETHER THE ACT'S FORKS ARE CHOICES — the measurement the whole rework exists for (plan S9: 72–80 % of
    // v0.0.0's forks lead into the same future twice).
    public ForkQualityRules ForkQuality { get; init; } = new();

    // The act's own quiet rooms, in its own voice. NULL for an act that has none: Act V is three bosses back
    // to back with no campfire, no jar and no shop between them, and an empty waiting-room text would be a
    // room that exists and says nothing rather than a room the act does not have.
    public ActRooms? Rooms { get; init; }

    // How many BOSS rooms the act ends on. One is what a boss is; the Divine Ledger is three.
    public int BossRooms { get; init; } = 1;

    // An act with no rooms is a GAUNTLET: nothing but its bosses. Everything MapSpecBuilder normally assembles
    // — the waiting room, the jars, the act's doors, the shop, the spoils — is a thing such an act does not
    // have, so it is built by its own path rather than by giving each of those an "unless act five" clause.
    public bool IsGauntlet => Rooms is null;

    // The act's budgets, one line each: how few, how many, how many at most. Written as a helper because six
    // `new RoomBudget { … }` literals per act would bury the only thing that differs between the acts — the
    // numbers — under punctuation.
    private static Dictionary<MapNodeKind, RoomBudget> Budgets(
        params (MapNodeKind Kind, int Min, int Target, int Max)[] budgets) =>
        budgets.ToDictionary(b => b.Kind, b => new RoomBudget { Min = b.Min, Target = b.Target, Max = b.Max });

    // The act in four quarters, each said the same way: `(kind, at least, at most)`. A quarter that asks for a
    // minimum also TARGETS it — a band aims at exactly what it promises and no further, because wanting more of
    // a role in one quarter than the quarter promises is what the act-wide target is for.
    private static IReadOnlyList<DepthBandBudget> Quarters(
        params (MapNodeKind Kind, int Min, int Max)[][] quarters) =>
        [.. quarters.Select((band, index) => new DepthBandBudget
        {
            StartPercent = index * 25,
            EndPercent = (index + 1) * 25,
            Budgets = band.ToDictionary(
                rule => rule.Kind,
                rule => new RoomBudget { Min = rule.Min, Target = rule.Min, Max = rule.Max }),
        })];

    public static ActRules For(BabActManifest act) => act.Act switch
    {
        1 => City,
        2 => Archives,
        3 => GreenDocket,
        4 => Labyrinth,
        5 => DivineLedger,
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
        // The act is twenty-three rooms long and holds this much of each thing — the totals §6 of the plan
        // derived from what v0.0.0's twenty-three rows actually contain, not from multiplying the per-path
        // table (routes share rooms, so that multiplication means nothing). Combat is absent on purpose: it is
        // the filler, and what it amounts to is whatever the other budgets leave.
        Rows = 23,
        RoomBudgets = Budgets(
            (MapNodeKind.Elite, 3, 5, 8),
            (MapNodeKind.MultiCombat, 2, 4, 7),
            (MapNodeKind.Event, 8, 11, 15),
            (MapNodeKind.Rest, 4, 6, 9),
            (MapNodeKind.Treasure, 4, 6, 9),
            (MapNodeKind.Shop, 4, 5, 7)),
        // A campfire in every quarter, so no stretch of the city is unsurvivable; the elites spread out rather
        // than queue up at the end; the crowded fight stays rare while the deck is still a handful of cards.
        // The opening quarter budgets no elite at all because the act already gates one out of it (35 %) — a
        // band that repeats a gate is a sentence about nothing.
        DepthBands = Quarters(
            [(MapNodeKind.Rest, 1, 2), (MapNodeKind.MultiCombat, 0, 1)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 2), (MapNodeKind.Shop, 1, 3)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Shop, 1, 3)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Treasure, 1, 4)]),
        // THE FLOOR IS v0.0.0'S OWN THINNEST ROUTE, measured over 100 seeds (plan S8: Act I 120..150). The
        // strategic act may lay its trouble out differently, but no walk through it may ask less than the
        // easiest walk through the old one — which is precisely what the retired per-path minimums bought.
        // The ceiling is a remark, not a promise: v0.0.0's richest route measured 190.
        PathPressure = new PathPressureRules { Minimum = 120, Maximum = 200 },
        // v0.0.0's forks average a contrast of 10 and three quarters of them are hollow. Thirty is a fork whose
        // two ways differ by an elite, or by a campfire against a shop — a question rather than a formality.
        ForkQuality = new ForkQualityRules { MinimumContrast = 30 },
        Rooms = new ActRooms
        {
            RestText = "The waiting room. The chairs are terrible, but nobody can reach you here.",
            RestChoiceText = "Wait it out",
            RestUpgradeChoiceText = "Submit an amendment",
            TreasureText = "A sealed evidence crate, stamped in three colors of wax. Nobody has claimed it in decades.",
            TreasureOpenText = "Break the seals",
            TreasureLeaveText = "Leave it for the archivists",
        },
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
        // Twenty-four rooms, and more of them dangerous than the city's: an elite more, a crowded fight more,
        // one treasure fewer. v0.0.0's thinnest route through the archives measured 160.
        Rows = 24,
        RoomBudgets = Budgets(
            (MapNodeKind.Elite, 4, 6, 9),
            (MapNodeKind.MultiCombat, 3, 5, 8),
            (MapNodeKind.Event, 8, 11, 15),
            (MapNodeKind.Rest, 4, 6, 9),
            (MapNodeKind.Treasure, 3, 5, 8),
            (MapNodeKind.Shop, 4, 5, 7)),
        DepthBands = Quarters(
            [(MapNodeKind.Rest, 1, 2), (MapNodeKind.MultiCombat, 0, 2)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Shop, 1, 3)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Shop, 1, 3)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Treasure, 1, 4)]),
        PathPressure = new PathPressureRules { Minimum = 160, Maximum = 240 },
        ForkQuality = new ForkQualityRules { MinimumContrast = 30 },
        Rooms = new ActRooms
        {
            RestText = "A reading alcove behind the returns desk. The lamp works, and the shelf above you has not "
                + "moved once in the hour you have been watching it.",
            RestChoiceText = "Sit until the shelf gives up",
            RestUpgradeChoiceText = "Amend a filing while nobody is looking",
            TreasureText = "A returns trolley nobody has emptied. The bottom shelf is still checked out to someone, "
                + "and the card says the loan period has not started yet.",
            TreasureOpenText = "Check the bottom shelf",
            TreasureLeaveText = "Push it back into the dark",
        },
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
        // Twenty-five rooms of open country, and the road's own bargain: more elites than the archives and
        // less of everything that keeps you. v0.0.0's thinnest route measured 185.
        Rows = 25,
        RoomBudgets = Budgets(
            (MapNodeKind.Elite, 5, 7, 10),
            (MapNodeKind.MultiCombat, 3, 5, 8),
            (MapNodeKind.Event, 9, 12, 16),
            (MapNodeKind.Rest, 4, 6, 9),
            (MapNodeKind.Treasure, 3, 5, 8),
            (MapNodeKind.Shop, 4, 5, 8)),
        // The road's elites stand from a fifth of the way in, so its opening quarter is the one quarter in the
        // game that budgets an elite of its own — one, and only one.
        DepthBands = Quarters(
            [(MapNodeKind.Rest, 1, 2), (MapNodeKind.MultiCombat, 0, 2), (MapNodeKind.Elite, 0, 1)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Shop, 1, 3)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Shop, 1, 3)],
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.Elite, 0, 3), (MapNodeKind.Treasure, 1, 4)]),
        PathPressure = new PathPressureRules { Minimum = 185, Maximum = 270 },
        ForkQuality = new ForkQualityRules { MinimumContrast = 30 },
        Rooms = new ActRooms
        {
            RestText = "A hollow out of the wind, with a stone somebody has sat on often enough to wear it. "
                + "Nothing here has a right to you for as long as you stay off the road.",
            RestChoiceText = "Sit out of the wind",
            RestUpgradeChoiceText = "Put a filing in order by daylight",
            TreasureText = "A boundary cairn with a hollow in it, and something in the hollow that was left for "
                + "whoever came next. The stones around it have been counted recently.",
            TreasureOpenText = "Take what was left",
            TreasureLeaveText = "Add a stone and walk on",
        },
    };

    // ── Act IV: The Licensing Labyrinth ────────────────────────────────────────────
    // The act the design calls "substantially longer and mechanically denser than the earlier acts", and the
    // per-path table says so in numbers: twelve fights a route instead of eight, three of them crowded, four
    // elites, four doors. The soft rooms grow with it — three campfires, three shops, two treasures — because
    // an act half again as long with Act III's comforts is not harder, only meaner.
    private static readonly ActRules Labyrinth = new()
    {
        PerPathMinimums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 12,
            [MapNodeKind.MultiCombat] = 3,
            [MapNodeKind.Elite] = 4,
            [MapNodeKind.Event] = 4,
            [MapNodeKind.Rest] = 3,
            [MapNodeKind.Treasure] = 2,
            [MapNodeKind.Shop] = 3,
        },
        // One spare of each comfort, as everywhere else — and the labyrinth's own indulgence, a sixth elite,
        // because by Act IV a route that goes looking for elites is a build rather than a mistake.
        PerPathMaximums = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Rest] = 4,
            [MapNodeKind.Treasure] = 3,
            [MapNodeKind.Shop] = 4,
            [MapNodeKind.Event] = 7,
            [MapNodeKind.Elite] = 6,
            [MapNodeKind.MultiCombat] = 5,
        },
        // Three ways through the same institution: up the ramp with everything that has been licensed to walk
        // it, along the counters where the licences are actually issued, or down through the storerooms where
        // what has been licensed is kept. The labyrinth has no quiet way — the third lane still fights.
        Lanes =
        [
            new("the processional ramp", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Combat] = 12,
                [MapNodeKind.MultiCombat] = 5,
                [MapNodeKind.Elite] = 5,
                [MapNodeKind.Event] = 2,
            }),
            new("the licence counters", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Event] = 8,
                [MapNodeKind.Shop] = 4,
                [MapNodeKind.Combat] = 6,
                [MapNodeKind.MultiCombat] = 2,
            }),
            new("the sealed storerooms", new Dictionary<MapNodeKind, int>
            {
                [MapNodeKind.Rest] = 5,
                [MapNodeKind.Treasure] = 4,
                [MapNodeKind.Combat] = 6,
                [MapNodeKind.Event] = 3,
            }),
        ],
        KindWeights = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Combat] = 10,
            [MapNodeKind.Event] = 5,
            [MapNodeKind.Elite] = 4,
            [MapNodeKind.Rest] = 2,
            [MapNodeKind.Treasure] = 2,
            [MapNodeKind.Shop] = 1,
        },
        // The elite table's own earliest column (elite master §5: depths 3 to 12 of seventeen stages), read as
        // the shallowest of them — an elite may stand from a fifth of the way in, and WHICH elite stands there
        // is the encounters' own gate (BabEncounter.EarliestDepthPercent → EncounterMinimumDepthPercent).
        // The rest is the labyrinth's temper: it fills a room before it sells you anything, and its crowds
        // form early, because being outnumbered in a corridor is what the act is.
        EarliestDepthPercent = new Dictionary<MapNodeKind, int>
        {
            [MapNodeKind.Shop] = 8,
            [MapNodeKind.Rest] = 8,
            [MapNodeKind.MultiCombat] = 12,
            [MapNodeKind.Elite] = 18,
        },
        // Half again as long as the acts before it — thirty-five rooms — and everything in it scales with the
        // length, comforts included: an act of Act IV's size on Act III's supplies is not harder, only meaner
        // (the same argument the per-path table made, now made about the act instead of about a route).
        // v0.0.0's thinnest route measured 265.
        Rows = 35,
        RoomBudgets = Budgets(
            (MapNodeKind.Elite, 7, 10, 14),
            (MapNodeKind.MultiCombat, 5, 8, 12),
            (MapNodeKind.Event, 12, 16, 21),
            (MapNodeKind.Rest, 7, 9, 12),
            (MapNodeKind.Treasure, 5, 7, 10),
            (MapNodeKind.Shop, 5, 7, 10)),
        DepthBands = Quarters(
            [(MapNodeKind.Rest, 1, 3), (MapNodeKind.MultiCombat, 0, 3), (MapNodeKind.Elite, 0, 2)],
            [(MapNodeKind.Rest, 2, 4), (MapNodeKind.Elite, 0, 4), (MapNodeKind.Shop, 1, 4)],
            [(MapNodeKind.Rest, 2, 4), (MapNodeKind.Elite, 0, 4), (MapNodeKind.Shop, 1, 4)],
            [(MapNodeKind.Rest, 2, 4), (MapNodeKind.Elite, 0, 4), (MapNodeKind.Treasure, 2, 5)]),
        PathPressure = new PathPressureRules { Minimum = 265, Maximum = 360 },
        ForkQuality = new ForkQualityRules { MinimumContrast = 30 },
        Rooms = new ActRooms
        {
            RestText = "A niche off the ramp with a water jar in it, left for the workmen and never collected. "
                + "Nothing here is licensed to notice you, which in this building is the same as being alone.",
            RestChoiceText = "Sit in the niche until the shift changes",
            RestUpgradeChoiceText = "Recut one of your own procedures while the stone is soft",
            TreasureText = "A sealed jar in a wall-slot, with the impression of a seal that has not been valid "
                + "for four reigns. Whatever it was licensed to hold is still inside.",
            TreasureOpenText = "Break the old seal",
            TreasureLeaveText = "Press the slot shut and log nothing",
        },
    };

    // ── Act V: The Divine Ledger ───────────────────────────────────────────────────
    // Not an act. Three bosses, back to back, drawn from six without repetition, with nothing whatever
    // between them: no standards, no elites, no doors, no shop, no jars, no campfire, no healing, and no
    // spoils of any kind (BnB Boss Master §Act V, §1). The build that enters is the build that must win.
    //
    // So this record says almost nothing, and that is the design: no per-path promises (there is no path —
    // there is an order), no lanes (there are no columns), no weights (nothing is drawn but bosses), no
    // depth gates (a god may open the act as readily as close it), and no rooms at all.
    private static readonly ActRules DivineLedger = new()
    {
        BossRooms = 3,
        // The act's length, and the whole of it. No budgets, no bands, no path pressure and no forks: a gauntlet
        // has not one chosen room, so there is nothing for the strategic generator to allocate and MapSpecBuilder
        // builds it no spec at all — a run on v0.0.1 walks THIS act on v0.0.0, because the two agree about it.
        Rows = 3,
        PerPathMinimums = new Dictionary<MapNodeKind, int>(),
        PerPathMaximums = new Dictionary<MapNodeKind, int>(),
        Lanes = [],
        KindWeights = new Dictionary<MapNodeKind, int>(),
        EarliestDepthPercent = new Dictionary<MapNodeKind, int>(),
    };
}

// The words an act's own quiet rooms are told in. Shared shape, act-specific voice — a waiting room heals a
// percentage and offers an amendment wherever it stands, and only the furniture differs.
internal sealed record ActRooms
{
    public required string RestText { get; init; }
    public required string RestChoiceText { get; init; }

    // The campfire's SECOND action (BnB_Run_Systems_Master §3: a waiting room offers Authorized Leave *or*
    // Submit an Amendment). Upgrading a card is the same act in every act; only the room's voice changes.
    public required string RestUpgradeChoiceText { get; init; }
    public required string TreasureText { get; init; }
    public required string TreasureOpenText { get; init; }
    public required string TreasureLeaveText { get; init; }
}
