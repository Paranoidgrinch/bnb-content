using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Lieutenant.CardShapes;

namespace BnbContent.Converter.Lieutenant;

// The Lieutenant's own cards (lieutenant_master.md §3). The canon's names, rules and deliberate exceptions; its
// numbers are BALANCE DRAFTS, and its upgrades give way to the game's upgrade rules (LIEUTENANT_PLAN §1, §4).
public static partial class LieutenantCards
{
    public const string CharacterTag = "lieutenant";

    // A tag naming the card itself, so "discard another card" can leave the card doing the asking out of the offer.
    public static string Self(string cardId) => "self_" + cardId.TrimEnd('+');

    private static BnbCard Card(
        string id, string name, string type, int cost, string text, CombatNodeModel program,
        string rarity = "starter", int act = 1, IReadOnlyList<string>? tags = null) =>
        new(id, name, type, cost, text, program, Rarity: rarity, Act: act, Tags: [CharacterTag, Self(id), .. tags ?? []]);

    // ── starter (§3.1) ───────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel WatchCutHit(int damage, int more) =>
        IfChallenged(Damage(damage + more), Damage(damage));

    private static readonly BnbCard WatchCut = Card(
        "lt_watch_cut", "Watch Cut", DeedTag, 1,
        "Deal 6 damage. If the target is Challenged, deal 2 more.", WatchCutHit(6, 2));

    private static CombatNodeModel BucklerUpBlock(int block, int more) =>
        If(Has(OpenGuard), Block(block + more), Block(block));

    private static readonly BnbCard BucklerUp = Card(
        "lt_buckler_up", "Buckler Up", WorkingTag, 1,
        "Gain 5 Block. If you have Open Guard, gain 2 more.", BucklerUpBlock(5, 2));

    private static readonly BnbCard FirstOrders = Card(
        "lt_first_orders", "First Orders", WorkingTag, 1,
        $"Gain 4 Block. {Promises.FirstOrders.Text}", Seq(Block(4), Issue(Promises.FirstOrders)));

    // "You may gain 1 Open Guard to Challenge" — the fresh promotion's optional risk.
    private static CombatNodeModel NewStripesDare() =>
        If(new CombatConditionSpec("isAlive", Target),
            CombatNodeModel.ChooseOptions(1, ["Gain 1 Open Guard and Challenge the target", "Not now"],
                [Seq(Apply(OpenGuard, 1, You), Issue(Promises.NewStripes)), Seq()], "New Stripes"));

    private static readonly BnbCard NewStripes = Card(
        "lt_new_stripes", "New Stripes", WorkingTag, 1,
        $"Gain 1 Strength. You may gain 1 Open Guard to issue a {Promises.NewStripes.Text}",
        Seq(Apply(Strength, 1, You), NewStripesDare()));

    public static IReadOnlyList<BnbCard> Starter() =>
    [
        WatchCut, WatchCut.Upgraded("Deal 9 damage. If the target is Challenged, deal 3 more.", WatchCutHit(9, 3)),
        BucklerUp, BucklerUp.Upgraded("Gain 8 Block. If you have Open Guard, gain 3 more.", BucklerUpBlock(8, 3)),
        FirstOrders, FirstOrders.Upgraded($"Gain 6 Block. {Promises.FirstOrdersPlus.Text}",
            Seq(Block(6), Issue(Promises.FirstOrdersPlus))),
        NewStripes, NewStripes.Upgraded(
            $"Gain 1 Strength and 4 Block. You may gain 1 Open Guard to issue a {Promises.NewStripes.Text}",
            Seq(Apply(Strength, 1, You), Block(4), NewStripesDare())),
    ];

    // The starting deck (§1): 4 Watch Cut, 4 Buckler Up, First Orders, New Stripes.
    public static IReadOnlyList<string> StarterDeck =>
    [
        WatchCut.Id, WatchCut.Id, WatchCut.Id, WatchCut.Id,
        BucklerUp.Id, BucklerUp.Id, BucklerUp.Id, BucklerUp.Id,
        FirstOrders.Id, NewStripes.Id,
    ];

    public static IReadOnlyList<BnbCard> All() => [.. Starter(), .. Commons()];

    // Her reward cards gated to an act, as the others' are: starters and the upgraded twins are never offered.
    public static IReadOnlyList<BnbCard> RewardPool(int act) => Cards.FinalCards.Offerable([.. Commons()], act);

    public static IReadOnlyList<CardData> Compile() => All().Select(c => c.Compile()).ToList();
}
