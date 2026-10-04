using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's own cards (hedge_witch_master.md §10–§13). Every one carries exactly one ingredient family
// (§3) and a tag naming the card itself, which is how a Hidden Recipe (§9) asks for "these three cards" whatever
// their upgrade. Numbers are the canon's BALANCE DRAFTS.
public static partial class WitchCards
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

    // ── commons (§11): twenty, four a family — the Witch's plain language. Numbers by the E7 rules ─────────

    private static readonly BnbCard BrambleSwitch = Witch(
        "bramble_switch", "Bramble Switch", DeedTag, 1, WitchActions.Fang,
        "Deal 7 damage. If the target is Hexed, deal 3 more.", HexedBonus(7, 3), "common");

    private static readonly BnbCard TwoTeeth = Witch(
        "two_teeth", "Two Teeth", DeedTag, 1, WitchActions.Fang,
        "Deal 4 damage twice.", Repeat(2, Damage(4)), "common");

    private static readonly BnbCard CrowsPeck = Witch(
        "crows_peck", "Crow's Peck", DeedTag, 0, WitchActions.Fang,
        "Deal 3 damage. If the target is at or below half its HP, deal 6 instead.", Peck(3, 6), "common");

    private static readonly BnbCard BriarSweep = Witch(
        "briar_sweep", "Briar Sweep", DeedTag, 2, WitchActions.Fang,
        "Deal 9 damage to ALL enemies.", Damage(9, AllEnemies), "common");

    private static readonly BnbCard EvilEye = Witch(
        "evil_eye", "Evil Eye", WorkingTag, 1, WitchActions.Hex,
        "Deal 3 damage. Apply 3 Hexed.", Seq(Damage(3), Apply(WitchKeywords.Hexed, 3)), "common");

    private static readonly BnbCard MutteredName = Witch(
        "muttered_name", "Muttered Name", WorkingTag, 1, WitchActions.Hex,
        "Apply 3 Hexed to ALL enemies.", Apply(WitchKeywords.Hexed, 3, AllEnemies), "common");

    private static readonly BnbCard ThirdKnock = Witch(
        "third_knock", "Third Knock", WorkingTag, 1, WitchActions.Hex,
        "Apply 3 Hexed. If its Hex bursts at the end of its next turn, apply 3 more.", Knock(3, 3), "common");

    private static readonly BnbCard OldGrudge = Witch(
        "old_grudge", "Old Grudge", WorkingTag, 2, WitchActions.Hex,
        "Apply 10 Hexed.", Apply(WitchKeywords.Hexed, 10), "common");

    private static readonly BnbCard BirchBarkWrap = Witch(
        "birch_bark_wrap", "Birch-Bark Wrap", WorkingTag, 1, WitchActions.Husk,
        "Gain 8 Block.", Block(8), "common");

    private static readonly BnbCard SnailShell = Witch(
        "snail_shell", "Snail Shell", WorkingTag, 1, WitchActions.Husk,
        "Gain 6 Block. If no Attack gets through this enemy turn, gain 3 Ward Wax.",
        Seq(Block(6), Apply(WitchRules.SnailShell, 3, You)), "common");

    private static readonly BnbCard ThornHedge = Witch(
        "thorn_hedge", "Thorn Hedge", WorkingTag, 1, WitchActions.Husk,
        "Gain 6 Block. The first enemy to attack you this turn takes 4 damage.",
        Seq(Block(6), Apply(WitchRules.ThornHedge, 4, You)), "common");

    private static readonly BnbCard ShedSkin = Witch(
        "shed_skin", "Shed Skin", WorkingTag, 1, WitchActions.Husk,
        "Gain 5 Block. Remove 1 stack of a negative Status from yourself.", Seq(Block(5), Cleanse(1)), "common");

    private static readonly BnbCard MugwortPoultice = Witch(
        "mugwort_poultice", "Mugwort Poultice", WorkingTag, 1, WitchActions.Hearth,
        "Heal 6 HP lost this combat. Exhaust.", Heal(6), "common", [ExhaustTag]);

    private static readonly BnbCard HotBroth = Witch(
        "hot_broth", "Hot Broth", WorkingTag, 1, WitchActions.Hearth,
        "Gain 6 Block. Heal 1 HP lost this combat.", Seq(Block(6), Heal(1)), "common");

    private static readonly BnbCard BitterTea = Witch(
        "bitter_tea", "Bitter Tea", WorkingTag, 1, WitchActions.Hearth,
        "Draw 1 card. Heal 1 HP lost this combat.", Seq(Draw(1), Heal(1)), "common");

    private static readonly BnbCard SetTheBone = Witch(
        "set_the_bone", "Set the Bone", WorkingTag, 2, WitchActions.Hearth,
        "Gain 11 Block. Heal 3 HP lost this combat.", Seq(Block(11), Heal(3)), "common");

    private static readonly BnbCard BlackCat = Witch(
        "black_cat", "Black Cat", DeedTag, 1, WitchActions.Fortune,
        "Deal 4 damage. Apply 5 Misfortune.", Seq(Damage(4), Apply(WitchKeywords.Misfortune, 5)), "common");

    private static readonly BnbCard SpilledSalt = Witch(
        "spilled_salt", "Spilled Salt", WorkingTag, 1, WitchActions.Fortune,
        "Gain 4 Block. Apply 4 Misfortune.", Seq(Block(4), Apply(WitchKeywords.Misfortune, 4)), "common");

    private static readonly BnbCard CrookedHorseshoe = Witch(
        "crooked_horseshoe", "Crooked Horseshoe", WorkingTag, 1, WitchActions.Fortune,
        "Apply 6 Misfortune.", Apply(WitchKeywords.Misfortune, 6), "common");

    private static readonly BnbCard KnockOnWood = Witch(
        "knock_on_wood", "Knock on Wood", WorkingTag, 0, WitchActions.Fortune,
        "Gain 3 Block. If any enemy has Misfortune, gain 3 more.", Block(IfAnyMisfortune(3, 3)), "common");

    public static IReadOnlyList<BnbCard> Commons() =>
    [
        BrambleSwitch, BrambleSwitch.Upgraded("Deal 9 damage. If the target is Hexed, deal 5 more.", HexedBonus(9, 5)),
        TwoTeeth, TwoTeeth.Upgraded("Deal 6 damage twice.", Repeat(2, Damage(6))),
        CrowsPeck, CrowsPeck.Upgraded(
            "Deal 4 damage. If the target is at or below half its HP, deal 9 instead.", Peck(4, 9)),
        BriarSweep, BriarSweep.Upgraded("Deal 13 damage to ALL enemies.", Damage(13, AllEnemies)),
        EvilEye, EvilEye.Upgraded("Deal 4 damage. Apply 5 Hexed.", Seq(Damage(4), Apply(WitchKeywords.Hexed, 5))),
        MutteredName, MutteredName.Upgraded("Apply 5 Hexed to ALL enemies.", Apply(WitchKeywords.Hexed, 5, AllEnemies)),
        ThirdKnock, ThirdKnock.Upgraded(
            "Apply 4 Hexed. If its Hex bursts at the end of its next turn, apply 5 more.", Knock(4, 5)),
        OldGrudge, OldGrudge.Upgraded("Apply 15 Hexed.", Apply(WitchKeywords.Hexed, 15)),
        BirchBarkWrap, BirchBarkWrap.Upgraded("Gain 12 Block.", Block(12)),
        SnailShell, SnailShell.Upgraded("Gain 8 Block. If no Attack gets through this enemy turn, gain 4 Ward Wax.",
            Seq(Block(8), Apply(WitchRules.SnailShell, 4, You))),
        ThornHedge, ThornHedge.Upgraded("Gain 8 Block. The first enemy to attack you this turn takes 6 damage.",
            Seq(Block(8), Apply(WitchRules.ThornHedge, 6, You))),
        ShedSkin, ShedSkin.Upgraded("Gain 8 Block. Remove 1 stack of a negative Status from yourself.",
            Seq(Block(8), Cleanse(1))),
        MugwortPoultice, MugwortPoultice.Upgraded("Heal 9 HP lost this combat. Exhaust.", Heal(9)),
        HotBroth, HotBroth.Upgraded("Gain 9 Block. Heal 1 HP lost this combat.", Seq(Block(9), Heal(1))),
        BitterTea, BitterTea.Upgraded("Draw 2 cards. Heal 1 HP lost this combat.", Seq(Draw(2), Heal(1))),
        SetTheBone, SetTheBone.Upgraded("Gain 15 Block. Heal 3 HP lost this combat.", Seq(Block(15), Heal(3))),
        BlackCat, BlackCat.Upgraded("Deal 6 damage. Apply 7 Misfortune.",
            Seq(Damage(6), Apply(WitchKeywords.Misfortune, 7))),
        SpilledSalt, SpilledSalt.Upgraded("Gain 6 Block. Apply 5 Misfortune.",
            Seq(Block(6), Apply(WitchKeywords.Misfortune, 5))),
        CrookedHorseshoe, CrookedHorseshoe.Upgraded("Apply 8 Misfortune.", Apply(WitchKeywords.Misfortune, 8)),
        KnockOnWood, KnockOnWood.Upgraded("Gain 4 Block. If any enemy has Misfortune, gain 4 more.",
            Block(IfAnyMisfortune(4, 4))),
    ];

    public static IReadOnlyList<string> StarterDeck =>
    [
        "adders_nip", "adders_nip", "adders_nip", "adders_nip",
        "pot_lid", "pot_lid", "pot_lid", "pot_lid",
        "crooked_finger", "nettle_tea",
    ];

    public static IReadOnlyList<BnbCard> All() =>
        [.. Starter(), .. Commons(), .. Uncommons(), .. Rares(), .. Junk(), .. SoupStones()];

    // Her reward cards gated to an act, as the Bureaucrat's are (FinalCards.Offerable): starters, Junk and the
    // upgraded twins are never offered.
    public static IReadOnlyList<BnbCard> RewardPool(int act) =>
        Cards.FinalCards.Offerable([.. Commons(), .. Uncommons(), .. Rares()], act);

    public static IReadOnlyList<CardData> Compile() => All().Select(c => c.Compile()).ToList();

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Witch(
        string id, string name, string type, int cost, string family, string text, CombatNodeModel program,
        string rarity = "starter", IReadOnlyList<string>? tags = null, int act = 1) =>
        new(id, name, type, cost, text, program, Rarity: rarity, Act: act,
            Tags: [family, Ingredient(id), CharacterTag, .. tags ?? []]);

    private static CombatConditionSpec IsHexed => HasStacks(WitchKeywords.Hexed);

    // "Deal N. If the target is Hexed, deal M more" — asked before the blow.
    private static CombatNodeModel HexedBonus(int damage, int bonus) =>
        If(IsHexed, Damage(damage + bonus), Damage(damage));

    // "If the target is at or below half its HP, deal M instead" — asked before the blow.
    private static CombatNodeModel Peck(int damage, int low) =>
        If(new CombatConditionSpec("compare", Target, ValueKind: "healthPercentage",
            Op: ComparisonOperator.LessOrEqual, Right: 50), Damage(low), Damage(damage));

    // "If its Hex bursts at the end of its next turn": its Threefold step already stands at two.
    private static CombatConditionSpec BurstsNext => new("compare", Target, ValueKind: "counter",
        Op: ComparisonOperator.GreaterOrEqual, Right: 2, Id: WitchKeywords.ThreefoldStep.value);

    private static CombatNodeModel Knock(int hexed, int more) =>
        Seq(Apply(WitchKeywords.Hexed, hexed), If(BurstsNext, Apply(WitchKeywords.Hexed, more)));

    // N stacks off negative statuses on herself, the engine picking which.
    private static CombatNodeModel Cleanse(int stacks) =>
        new("modifySelectedStatusStacks", You, CombatAmountSpec.FromConst(-stacks),
            Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff));

    // base, plus a bonus when any enemy carries Misfortune.
    private static CombatAmountSpec IfAnyMisfortune(int baseAmount, int bonus) =>
        Plus(CombatAmountSpec.FromConst(baseAmount), Times(Once(new CombatAmountSpec("countTargets",
            ReadSelector: new CombatSelectorSpec("withStatus", WitchKeywords.Misfortune,
                [new CombatSelectorSpec("allCombatants")]))), bonus));

    // "+bonus while the cauldron is Sheltering" — empty, or its lid shut (Shut the Lid), and not hot from a Brew
    // (E6, off unless switched on) — as arithmetic: base + bonus × max(1 − min(1, cards in pot), [lid shut]) × (1 − [hot]).
    private static CombatAmountSpec Sheltered(int baseAmount, int bonus) =>
        Plus(CombatAmountSpec.FromConst(baseAmount),
            CombatAmountSpec.Binary("mul", CombatAmountSpec.FromConst(bonus),
                CombatAmountSpec.Binary("mul",
                    CombatAmountSpec.Binary("max",
                        CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(1),
                            CombatAmountSpec.Binary("min", CombatAmountSpec.FromConst(1), CardsInZone(CardZone.SetAsidePile))),
                        Once(Stacks(WitchRules.ShutTheLid, You))),
                    CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(1), Once(Stacks(WitchRules.CauldronHot, You))))));

    // Hearth: "heal N HP lost during this combat" — never above the HP the fight began on (§3.4). What the cap
    // would waste may become Ward Wax first (Keep the Drippings), asked before the heal moves the numbers.
    public static CombatNodeModel Heal(int amount) => Heal(CombatAmountSpec.FromConst(amount));

    // Once a fight, Grandam's Ember lets a heal the cap would cut reach the HP lost before the fight — the same rule
    // WitchRules.Heal writes for brews and marks, here in the card's own vocabulary.
    public static CombatNodeModel Heal(CombatAmountSpec amount)
    {
        var capped = Seq(
            If(new CombatConditionSpec("hasStatus", You, Id: WitchRules.KeepTheDrippings),
                Apply(Cards.Keywords.WardWax, CombatAmountSpec.Binary("max", CombatAmountSpec.FromConst(0),
                    CombatAmountSpec.Binary("sub", amount, LostThisFight)), You)),
            new CombatNodeModel("heal", You, CombatAmountSpec.Binary("min", amount, LostThisFight)));
        var ember = Seq(
            new CombatNodeModel("setCombatantCounter", You, CombatAmountSpec.FromConst(1),
                CounterId: WitchRules.EmberLatch.value, Relative: false),
            new CombatNodeModel("heal", You, amount));
        return Seq(
            new CombatNodeModel("setCombatantCounter", You, CombatAmountSpec.Binary("sub", amount, LostThisFight),
                CounterId: HealReach.value, Relative: false),
            If(new CombatConditionSpec("hasStatus", You, Id: WitchRules.GrandamsEmber),
                If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Equal, Right: 0,
                        Id: WitchRules.EmberLatch.value),
                    If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Greater, Right: 0,
                            Id: HealReach.value),
                        ember, capped),
                    capped),
                capped));
    }

    // How far a heal reaches past the fight's cap — scratch, read at once.
    private static CounterId HealReach => new("hearth_heal_reach");

    private static CombatAmountSpec LostThisFight =>
        CombatAmountSpec.Binary("max", CombatAmountSpec.FromConst(0),
            CombatAmountSpec.Binary("sub",
                new CombatAmountSpec("counter", SelectorKey: You, CounterId: WitchKeywords.CombatStartHp.value),
                new CombatAmountSpec("currentHealth", SelectorKey: You)));
}
