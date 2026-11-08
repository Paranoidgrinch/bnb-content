using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 4: enemy debuffs (GENERAL_POOL_PLAN.md §4, approved 2026-11-08). Doubt and Liable spread,
// stretched and doubled; the Rares reward different debuffs on the same enemy.
public static class GeneralDebuffs
{
    private const string Doubt = Keywords.Doubt;
    private const string Liable = Keywords.Liable;

    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard SeedOfMisgiving = new(
        "seed_of_misgiving", "Seed of Misgiving", WorkingTag, 1,
        "Apply 2 Doubt. Gain 4 Block.",
        Seq(Apply(Doubt, 2), Block(4)));

    private static readonly BnbCard NamedInTheRoll = new(
        "named_in_the_roll", "Named in the Roll", WorkingTag, 1,
        "Apply 2 Liable. Draw 1 card.",
        Seq(Apply(Liable, 2), Draw(1)));

    private static readonly BnbCard Overlooking = new(
        "overlooking", "Overlooking", WorkingTag, 0,
        "Apply 1 Doubt and 1 Liable.",
        Seq(Apply(Doubt, 1), Apply(Liable, 1)));

    private static readonly BnbCard CurdledMilk = new(
        "curdled_milk", "Curdled Milk", WorkingTag, 1,
        "Apply 3 Doubt to ALL enemies.",
        ApplyAll(Doubt, 3));

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard HaresMark = new(
        "hares_mark", "Hare's Mark", WorkingTag, 1,
        "Apply 2 Liable. If the enemy has Doubt, apply 2 more.",
        Mark(2),
        Rarity: "uncommon");

    private static readonly BnbCard LingeringMalison = new(
        "lingering_malison", "Lingering Malison", WorkingTag, 0,
        "Every enemy with Doubt gets 1 more Doubt; every enemy with Liable gets 1 more Liable. Draw 1 card.",
        Seq(Proliferate(), Draw(1)),
        Rarity: "uncommon", Act: 2);

    private static readonly BnbCard SpreadingRumour = new(
        "spreading_rumour", "Spreading Rumour", WorkingTag, 1,
        "Apply 1 Doubt and 1 Liable. Then every other enemy gets the Doubt and Liable this enemy has.",
        Rumour(1),
        Rarity: "uncommon");

    private static readonly BnbCard IllWind = new(
        "ill_wind", "Ill Wind", DeedTag, 2,
        "Apply 1 Liable to ALL enemies, then deal 8 damage to ALL enemies.",
        Seq(ApplyAll(Liable, 1), DamageAll(8)),
        Rarity: "uncommon", Act: 2);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard CompoundedGrievance = new(
        "compounded_grievance", "Compounded Grievance", WorkingTag, 1,
        "Double every debuff on an enemy. Exhaust.",
        DoubleDebuffs(),
        Rarity: "rare", Act: 4, Tags: [ExhaustTag]);

    private static readonly BnbCard BillOfAttainder = new(
        "bill_of_attainder", "Bill of Attainder", RiteTag, 1,
        "Whenever you apply Doubt or Liable, apply 1 more. Exhaust.",
        InstallRite(GeneralPoolRules.BillOfAttainder),
        Rarity: "rare", Act: 2);

    public static IReadOnlyList<BnbCard> All() =>
    [
        SeedOfMisgiving, SeedOfMisgiving.Upgraded("Apply 3 Doubt. Gain 6 Block.", Seq(Apply(Doubt, 3), Block(6))),
        NamedInTheRoll, NamedInTheRoll.Upgraded("Apply 3 Liable. Draw 1 card.", Seq(Apply(Liable, 3), Draw(1))),
        Overlooking, Overlooking.Upgraded("Apply 2 Doubt and 1 Liable.", Seq(Apply(Doubt, 2), Apply(Liable, 1))),
        CurdledMilk, CurdledMilk.Upgraded("Apply 4 Doubt to ALL enemies.", ApplyAll(Doubt, 4)),

        HaresMark, HaresMark.Upgraded("Apply 3 Liable. If the enemy has Doubt, apply 3 more.", Mark(3)),
        LingeringMalison, LingeringMalison.Upgraded(
            "Every enemy with Doubt gets 1 more Doubt; every enemy with Liable gets 1 more Liable. Draw 2 cards.",
            Seq(Proliferate(), Draw(2))),
        SpreadingRumour, SpreadingRumour.Upgraded(
            "Apply 2 Doubt and 2 Liable. Then every other enemy gets the Doubt and Liable this enemy has.", Rumour(2)),
        IllWind, IllWind.Upgraded("Apply 1 Liable to ALL enemies, then deal 12 damage to ALL enemies.",
            Seq(ApplyAll(Liable, 1), DamageAll(12))),

        CompoundedGrievance, CompoundedGrievance.Upgraded("Double every debuff on an enemy. Exhaust.", cost: 0),
        BillOfAttainder, BillOfAttainder.Upgraded("Whenever you apply Doubt or Liable, apply 1 more. Exhaust.",
            InstallRite(GeneralPoolRules.BillOfAttainder), cost: 0),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel Mark(int stacks) =>
        Seq(Apply(Liable, stacks), If(HasStacks(Doubt), Apply(Liable, stacks)));

    private static CombatNodeModel Proliferate() =>
        Seq(EachEnemyWith(Doubt, Apply(Doubt, 1, "iterationTarget")),
            EachEnemyWith(Liable, Apply(Liable, 1, "iterationTarget")));

    private static CombatNodeModel EachEnemyWith(string status, CombatNodeModel body) =>
        new("forEachTarget", SelectorKey: "enemiesWithStatus", SelectorStatusId: status, Children: [body]);

    // "Then every other enemy gets the Doubt and Liable this enemy has." Read off the target after its own
    // stacks are applied, and given to the others — never taken from it.
    private static CombatNodeModel Rumour(int stacks) =>
        Seq(
            Apply(Doubt, stacks),
            Apply(Liable, stacks),
            If(HasStacks(Doubt), Apply(Doubt, Stacks(Doubt), "otherEnemies")),
            If(HasStacks(Liable), Apply(Liable, Stacks(Liable), "otherEnemies")));

    // "Double every debuff on an enemy": each negative status the game files, topped up by what it already has.
    private static CombatNodeModel DoubleDebuffs() =>
        Seq(NegativeStatuses.Select(status => If(HasStacks(status), Apply(status, Stacks(status)))).ToArray());
}
