using static BnbContent.Converter.Lieutenant.ChallengeKind;
using static BnbContent.Converter.Lieutenant.OrderKind;

namespace BnbContent.Converter.Lieutenant;

// Every Order and Challenge a Lieutenant card can issue (lieutenant_master.md §3), in one place, so that each
// becomes its own status and "is this enemy Challenged?" can name them all. A card's text quotes its promise's.
public static class Promises
{
    // ── starters ─────────────────────────────────────────────────────────────────────────────────────────

    public static readonly OrderSpec FirstOrders = new("lt_o_first_orders", "First Orders", PlayLater,
        "play a Deed later this turn", new Reward());
    public static readonly OrderSpec FirstOrdersPlus = new("lt_o_first_orders_plus", "First Orders+", PlayLater,
        "play a Deed later this turn", new Reward(Block: 3));

    public static readonly ChallengeSpec NewStripes = new("lt_c_new_stripes", "New Stripes", Hits,
        "hit it with a Deed before it acts", new Reward());

    // ── commons ──────────────────────────────────────────────────────────────────────────────────────────

    public static readonly OrderSpec RollCall = new("lt_o_roll_call", "Roll Call", DeedAndWorking,
        "play a Deed and a Working later this turn", new Reward());
    public static readonly OrderSpec Gatehouse = new("lt_o_gatehouse", "Gatehouse", Withstand,
        "in the next enemy phase, an enemy attacks you and no Attack damage gets through to your HP", new Reward());
    public static readonly OrderSpec LastBell = new("lt_o_last_bell", "Last Bell", EndWithNoEnergy,
        "end this turn with exactly 0 Energy", new Reward());
    public static readonly OrderSpec LastBellPlus = new("lt_o_last_bell_plus", "Last Bell+", EndWithNoEnergy,
        "end this turn with exactly 0 Energy", new Reward(Block: 4));
    public static readonly OrderSpec Scabbard = new("lt_o_scabbard", "Scabbard", Restraint,
        "play no further Deed this turn and end it with Energy left", new Reward());

    public static readonly ChallengeSpec MarkedMan = new("lt_c_marked_man", "Marked Man", Hits,
        "hit it with Deeds twice more before it acts", new Reward(), N: 2);
    public static readonly ChallengeSpec BracedPike = new("lt_c_braced_pike", "Braced Pike", WithstandAttack,
        "its next Attack, within its next two actions, takes none of your HP", new Reward(Strength: 1));
    public static readonly ChallengeSpec Arrest = new("lt_c_arrest", "Arrest", Kill,
        "defeat it by the end of your next turn", new Reward(Draw: 1));

    // ── uncommons ────────────────────────────────────────────────────────────────────────────────────────

    public static readonly OrderSpec AgainstTheWall = new("lt_o_against_the_wall", "Against the Wall", EndWithBlock,
        "end this turn with at least 14 Block", new Reward(), N: 14);
    public static readonly OrderSpec DoubleFile = new("lt_o_double_file", "Double File", TwoEnemies,
        "hit two different enemies with Deeds later this turn", new Reward(Block: 4));
    public static readonly OrderSpec MindTheGap = new("lt_o_mind_the_gap", "Mind the Gap", EndExposed,
        "end this turn with Open Guard and at least 8 Block", new Reward(), N: 8);
    public static readonly OrderSpec Cadence = new("lt_o_cadence", "Cadence", ExactlyMore,
        "play exactly two more cards this turn", new Reward(), N: 2);
    public static readonly OrderSpec NoExcuses = new("lt_o_no_excuses", "No Excuses", OpenNextTurn,
        "the first card you play next turn is a Deed", new Reward(Block: 4));
    public static readonly OrderSpec NoSurrender = new("lt_o_no_surrender", "No Surrender", WithstandExposed,
        "go into the next enemy phase with Open Guard; an enemy attacks you and no Attack damage gets through to your HP",
        new Reward(Authority: 2));

    public static readonly ChallengeSpec Gauntlet = new("lt_c_gauntlet", "Gauntlet", BigHit,
        "take at least 12 of its HP with one later Deed hit before it acts", new Reward(Strength: 1), N: 12);
    public static readonly ChallengeSpec OnYourFeet = new("lt_c_on_your_feet", "On Your Feet", KeepBlock,
        "after its next Attack, within its next two actions, you still have Block", new Reward(NextTurnDraw: 1));
    public static readonly ChallengeSpec Pursuit = new("lt_c_pursuit", "Pursuit", ChallengeKind.Pursuit,
        "hit it with a Deed later this turn and again next turn", new Reward(Authority: 2));
    public static readonly ChallengeSpec NoRunning = new("lt_c_no_running", "No Running", BigHit,
        "a later Deed hit takes its HP before it acts", new Reward(Draw: 1), N: 1);
    public static readonly ChallengeSpec Dare = new("lt_c_dare", "Dare", TakeHit,
        "let its next Attack, within its next two actions, take your HP", new Reward(Authority: 2, Strength: 1));
    public static readonly ChallengeSpec SecondBlade = new("lt_c_second_blade", "Second Blade", DistinctDeeds,
        "hit it with two more Deeds before it acts", new Reward(Strength: 1), N: 2);
    public static readonly ChallengeSpec Testimony = new("lt_c_testimony", "Testimony", KeepOrder,
        "keep an Order before it acts", new Reward(Draw: 1));

    public static readonly IReadOnlyList<OrderSpec> Orders =
    [
        FirstOrders, FirstOrdersPlus, RollCall, Gatehouse, LastBell, LastBellPlus, Scabbard,
        AgainstTheWall, DoubleFile, MindTheGap, Cadence, NoExcuses, NoSurrender,
    ];

    public static readonly IReadOnlyList<ChallengeSpec> Challenges =
    [
        NewStripes, MarkedMan, BracedPike, Arrest,
        Gauntlet, OnYourFeet, Pursuit, NoRunning, Dare, SecondBlade, Testimony,
    ];
}
