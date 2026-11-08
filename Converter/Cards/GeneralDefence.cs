using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 5: defence (GENERAL_POOL_PLAN.md §4, approved 2026-11-08). Block with a condition, Ward Wax,
// Block that stays or turns into something else.
public static class GeneralDefence
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    // Content review (2026-11-08): its Unread was Ashen Receipt's twin; now the broom sweeps itself back.
    private static readonly BnbCard BesomGuard = new(
        "besom_guard", "Besom Guard", WorkingTag, 1,
        "Gain 8 Block. Unread: put this on top of your draw pile.",
        Block(8),
        Tags: Unread("besom_guard"));

    // Kept; now a Common.
    private static readonly BnbCard WaxenSurety = new(
        "waxen_surety", "Waxen Surety", WorkingTag, 1,
        "Gain 7 Ward Wax.",
        Apply(Keywords.WardWax, 7, You));

    private static readonly BnbCard SaltOnTheSill = new(
        "salt_on_the_sill", "Salt on the Sill", WorkingTag, 1,
        "Gain 6 Block. Every enemy that intends to attack gets 1 Doubt.",
        Seq(Block(6), Salt()));

    private static readonly BnbCard BarredDoor = new(
        "barred_door", "Barred Door", WorkingTag, 2,
        "Retain. Gain 14 Block.",
        Block(14),
        RetainInHand: true);

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    // Kept; 6 Ward Wax instead of 4 (2026-11-08).
    private static readonly BnbCard TallowReserve = new(
        "tallow_reserve", "Tallow Reserve", WorkingTag, 0,
        "Needs 6 Block. Lose 6 Block. Gain 6 Ward Wax. Exhaust.",
        SpendBlockForWax(6, 6),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    private static readonly BnbCard SealedMantle = new(
        "sealed_mantle", "Sealed Mantle", WorkingTag, 1,
        "Gain 8 Block. If enemies attack this turn and nothing gets through, gain 3 Ward Wax.",
        Seq(Block(8), Apply(GeneralRites.SealedMantle, 1, You)),
        Rarity: "uncommon");

    private static readonly BnbCard WaxReliquary = new(
        "wax_reliquary", "Wax Reliquary", WorkingTag, 1,
        "Gain 5 Ward Wax. Until your next turn, Ward Wax does not decay faster.",
        Seq(Apply(Keywords.WardWax, 5, You), Apply(GeneralWax.WaxReliquary, 1, You)),
        Rarity: "uncommon", Act: 2);

    private static readonly BnbCard RowanOverTheDoor = new(
        "rowan_over_the_door", "Rowan over the Door", WorkingTag, 1,
        "Gain 7 Block. Until your next turn, every enemy that attacks you gets 1 Doubt.",
        Seq(Block(7), Apply(GeneralPoolRules.Rowan, 1, You)),
        Rarity: "uncommon");

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard StayOfProceedings = new(
        "stay_of_proceedings", "Stay of Proceedings", WorkingTag, 3,
        "An enemy's next action does nothing. Exhaust.",
        Apply(GeneralPoolRules.Stayed, 1),
        Rarity: "rare", Act: 4, Tags: [ExhaustTag]);

    private static readonly BnbCard StandingStones = new(
        "standing_stones", "Standing Stones", WorkingTag, 2,
        "Your Block does not expire at the start of your next 2 turns. Exhaust.",
        new CombatNodeModel("applyStatus", You, Const(1), StatusId: GeneralPoolRules.StandingStones, DurationTurns: 3),
        Rarity: "rare", Act: 3, Tags: [ExhaustTag]);

    public static IReadOnlyList<BnbCard> All() =>
    [
        BesomGuard, BesomGuard.Upgraded("Gain 12 Block. Unread: put this on top of your draw pile.", Block(12),
            tags: Unread("besom_guard+")),
        WaxenSurety, WaxenSurety.Upgraded("Gain 10 Ward Wax.", Apply(Keywords.WardWax, 10, You)),
        SaltOnTheSill, SaltOnTheSill.Upgraded("Gain 9 Block. Every enemy that intends to attack gets 1 Doubt.",
            Seq(Block(9), Salt())),
        BarredDoor, BarredDoor.Upgraded("Retain. Gain 21 Block.", Block(21)),

        TallowReserve, TallowReserve.Upgraded("Needs 6 Block. Lose 6 Block. Gain 9 Ward Wax. Exhaust.",
            SpendBlockForWax(6, 9)),
        SealedMantle, SealedMantle.Upgraded(
            "Gain 12 Block. If enemies attack this turn and nothing gets through, gain 4 Ward Wax.",
            Seq(Block(12), Apply(GeneralRites.SealedMantle + "+", 1, You))),
        WaxReliquary, WaxReliquary.Upgraded(
            "Gain 8 Ward Wax. Until your next turn, Ward Wax does not decay faster.",
            Seq(Apply(Keywords.WardWax, 8, You), Apply(GeneralWax.WaxReliquary, 1, You))),
        RowanOverTheDoor, RowanOverTheDoor.Upgraded(
            "Gain 10 Block. Until your next turn, every enemy that attacks you gets 1 Doubt.",
            Seq(Block(10), Apply(GeneralPoolRules.Rowan, 1, You))),

        StayOfProceedings, StayOfProceedings.Upgraded("An enemy's next action does nothing. Exhaust.", cost: 2),
        StandingStones, StandingStones.Upgraded(
            "Your Block does not expire at the start of your next 2 turns. Exhaust.", cost: 1),
    ];

    // Besom Guard's Unread moves the card itself, which a card-effect model cannot name: the rule does it
    // (GeneralPoolRules, SweepsBack).
    public static IReadOnlyList<(string CardId, CombatNodeModel Effect)> Unreads() => [];

    public static readonly string[] SweepsBack = ["besom_guard", "besom_guard+"];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel Salt() =>
        CombatNodeModel.ForEach(AllEnemies,
            If(new CombatConditionSpec("intends", "iterationTarget", Id: nameof(IntentKind.Attack)),
                Apply(Keywords.Doubt, 1, "iterationTarget")));

    // "Needs N Block. Lose N Block. Gain M Ward Wax." Block is not a value a condition can read, so it goes
    // through the scratch counter.
    private static CombatNodeModel SpendBlockForWax(int price, int wax) =>
        Seq(
            SetScratch(new CombatAmountSpec("defensivePool", SelectorKey: You,
                ReadId: StandardCombatIds.BlockDefensivePool.value)),
            IfScratch(ComparisonOperator.GreaterOrEqual, price,
                Seq(
                    new CombatNodeModel("modifyDefensivePool", You, Const(-price),
                        PoolId: StandardCombatIds.BlockDefensivePool.value),
                    Apply(Keywords.WardWax, wax, You))));
}
