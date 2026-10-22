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

    public static readonly IReadOnlyList<OrderSpec> Orders =
        [FirstOrders, FirstOrdersPlus, RollCall, Gatehouse, LastBell, LastBellPlus, Scabbard];
    public static readonly IReadOnlyList<ChallengeSpec> Challenges = [NewStripes, MarkedMan, BracedPike, Arrest];
}
