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

    public static readonly IReadOnlyList<OrderSpec> Orders = [FirstOrders, FirstOrdersPlus];
    public static readonly IReadOnlyList<ChallengeSpec> Challenges = [NewStripes];
}
