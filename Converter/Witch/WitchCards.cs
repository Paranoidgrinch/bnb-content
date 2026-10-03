using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's own cards (hedge_witch_master.md §10–§13). Every one carries exactly one ingredient family
// (§3) and a tag naming the card itself, which is how a Hidden Recipe (§9) asks for "these three cards" whatever
// their upgrade. Numbers are the canon's BALANCE DRAFTS.
public static class WitchCards
{
    public const string CharacterTag = "hedge_witch";
    public static string Ingredient(string cardId) => "ing_" + cardId.TrimEnd('+');

    // ── starter (§10) ─────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard AddersNip = Witch(
        "adders_nip", "Adder's Nip", DeedTag, 1, WitchActions.Fang,
        "Deal 6 damage.", Damage(6));

    private static readonly BnbCard PotLid = Witch(
        "pot_lid", "Pot-Lid", WorkingTag, 1, WitchActions.Husk,
        "Gain 5 Block. If the cauldron is Sheltering, gain 3 more.", Block(Sheltered(5, 3)));

    private static readonly BnbCard CrookedFinger = Witch(
        "crooked_finger", "Crooked Finger", WorkingTag, 1, WitchActions.Hex,
        "Apply 4 Hexed.", Apply(WitchKeywords.Hexed, 4));

    private static readonly BnbCard NettleTea = Witch(
        "nettle_tea", "Nettle Tea", WorkingTag, 1, WitchActions.Hearth,
        "Gain 4 Block. Heal 1 HP lost this combat.", Seq(Block(4), Heal(1)));

    public static IReadOnlyList<BnbCard> Starter() =>
    [
        AddersNip, AddersNip.Upgraded("Deal 9 damage.", Damage(9)),
        PotLid, PotLid.Upgraded("Gain 7 Block. If the cauldron is Sheltering, gain 3 more.", Block(Sheltered(7, 3))),
        CrookedFinger, CrookedFinger.Upgraded("Apply 5 Hexed.", Apply(WitchKeywords.Hexed, 5)),
        NettleTea, NettleTea.Upgraded("Gain 6 Block. Heal 1 HP lost this combat.", Seq(Block(6), Heal(1))),
    ];

    public static IReadOnlyList<string> StarterDeck =>
    [
        "adders_nip", "adders_nip", "adders_nip", "adders_nip",
        "pot_lid", "pot_lid", "pot_lid", "pot_lid",
        "crooked_finger", "nettle_tea",
    ];

    public static IReadOnlyList<BnbCard> All() => [.. Starter()];

    public static IReadOnlyList<CardData> Compile() => All().Select(c => c.Compile()).ToList();

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Witch(
        string id, string name, string type, int cost, string family, string text, CombatNodeModel program,
        string rarity = "starter") =>
        new(id, name, type, cost, text, program, Rarity: rarity, Tags: [family, Ingredient(id), CharacterTag]);

    // "+bonus while the cauldron is Sheltering" — empty — as arithmetic, base + bonus × (1 − min(1, cards in pot)).
    private static CombatAmountSpec Sheltered(int baseAmount, int bonus) =>
        Plus(CombatAmountSpec.FromConst(baseAmount),
            CombatAmountSpec.Binary("mul", CombatAmountSpec.FromConst(bonus),
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(1),
                    CombatAmountSpec.Binary("min", CombatAmountSpec.FromConst(1), CardsInZone(CardZone.SetAsidePile)))));

    // Hearth: "heal N HP lost during this combat" — never above the HP the fight began on (§3.4).
    public static CombatNodeModel Heal(int amount) =>
        new("heal", You,
            CombatAmountSpec.Binary("min", CombatAmountSpec.FromConst(amount),
                CombatAmountSpec.Binary("max", CombatAmountSpec.FromConst(0),
                    CombatAmountSpec.Binary("sub",
                        new CombatAmountSpec("counter", SelectorKey: You, CounterId: WitchKeywords.CombatStartHp.value),
                        new CombatAmountSpec("currentHealth", SelectorKey: You)))));
}
