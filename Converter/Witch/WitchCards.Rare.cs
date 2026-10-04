using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's twenty-five rares (hedge_witch_master.md §13): allowed to bend her own rules — the third
// night, the roll, the pot, the heal cap — and to define whole runs. Numbers by the approved E7 rules; spread over
// the acts as the Bureaucrat's are (plan E2): eight in Act I, six in II, six in III, five in IV.
public static partial class WitchCards
{
    // ── Fang ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard WolfAtTheDoor = Witch(
        "wolf_at_the_door", "Wolf at the Door", DeedTag, 2, WitchActions.Fang,
        "Retain. Deal 20 damage. Each turn it waits in your hand, it costs 1 less.",
        Wolf(20), "rare", act: 3) with { RetainInHand = true };

    private static readonly BnbCard CarrionFlight = Witch(
        "carrion_flight", "Carrion Flight", DeedTag, 2, WitchActions.Fang,
        "Deal 3 damage 6 times. Once the target has fallen, the rest strike a random enemy.", Carrion(6, 3), "rare", act: 2);

    private static readonly BnbCard TeethInTheDark = Witch(
        "teeth_in_the_dark", "Teeth in the Dark", DeedTag, 1, WitchActions.Fang,
        "Deal 6 damage, and 4 more for each card in the cauldron.", Damage(PerInPot(6, 4)), "rare");

    private static readonly BnbCard Turnskin = Witch(
        "turnskin", "Turnskin", DeedTag, 1, WitchActions.Fang,
        "A Husk card in the cauldron turns into an Adder's Nip. Deal 9 damage.", Seq(Turn(1), Damage(9)), "rare", act: 3);

    private static readonly BnbCard TheHareRunsLast = Witch(
        "the_hare_runs_last", "The Hare Runs Last", DeedTag, 1, WitchActions.Fang,
        "Deal 8 damage. If its Hex bursts at the end of its next turn, it takes 8 damage again after the burst.",
        Seq(Damage(8), Apply(WitchRules.HareRunsLast, 8)), "rare");

    // ── Hex ───────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard CallTheThirdNight = Witch(
        "call_the_third_night", "Call the Third Night", WorkingTag, 2, WitchActions.Hex,
        "The target's Hex bursts now: it loses 3 HP for each Hexed. Its Threefold count starts again.",
        ThirdNightNow(), "rare");

    private static readonly BnbCard NameWrittenBackwards = Witch(
        "name_written_backwards", "Name Written Backwards", WorkingTag, 1, WitchActions.Hex,
        "Double the target's Hexed. Its Threefold count starts again.", Backwards(0), "rare", act: 2);

    private static readonly BnbCard HexInTheRafters = Witch(
        "hex_in_the_rafters", "Hex in the Rafters", RiteTag, 2, WitchActions.Hex,
        "The first Hex burst each enemy turn gives every other enemy 3 Hexed.",
        Apply(WitchRules.HexInTheRafters, 3, You), "rare", act: 4);

    private static readonly BnbCard LastBreathHex = Witch(
        "last_breath_hex", "Last-Breath Hex", WorkingTag, 1, WitchActions.Hex,
        "Apply 6 Hexed. When the target dies, the healthiest other enemy takes on all its Hexed and its Threefold count.",
        Seq(Apply(WitchKeywords.Hexed, 6), Apply(WitchRules.LastBreathHex, 1)), "rare", act: 2);

    private static readonly BnbCard BaneRoot = Witch(
        "bane_root", "Bane-Root", RiteTag, 1, WitchActions.Hex,
        "After a Hex bursts, that enemy gains 2 Hexed.", Apply(WitchRules.BaneRoot, 2, You), "rare", act: 3);

    // ── Husk ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ShutTheLid = Witch(
        "shut_the_lid", "Shut the Lid", WorkingTag, 1, WitchActions.Husk,
        "Gain 8 Block. Until your next turn the cauldron shelters you however full it is, and nothing goes in or out.",
        Seq(Block(8), Apply(WitchRules.ShutTheLid, 1, You)), "rare", act: 2);

    private static readonly BnbCard ScarBark = Witch(
        "scar_bark", "Scar-Bark", WorkingTag, 1, WitchActions.Husk,
        "Gain 5 Block, and 1 more for each HP attacks took from you last enemy turn (at most 15 more).",
        Block(Scarred(5, 15)), "rare");

    private static readonly BnbCard FullPot = Witch(
        "full_pot", "Full Pot", WorkingTag, 1, WitchActions.Husk,
        "Gain 4 Block, and 3 more for each card in the cauldron. If it is Ready, gain 6 more.",
        Block(Plus(PerInPot(4, 3), Times(PotReady, 6))), "rare");

    private static readonly BnbCard WinterBark = Witch(
        "winter_bark", "Winter Bark", WorkingTag, 2, WitchActions.Husk,
        "Gain 16 Block. After the enemy turn, up to 6 of your unused Block becomes Ward Wax.",
        Seq(Block(16), Apply(WitchRules.WinterBark, 6, You)), "rare", act: 3);

    private static readonly BnbCard Ironwood = Witch(
        "ironwood", "Ironwood", RiteTag, 2, WitchActions.Husk,
        "The first time each turn an attack gets through your Block, gain 4 Ward Wax.",
        Apply(WitchRules.Ironwood, 4, You), "rare", act: 4);

    // ── Hearth ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard GrannysPhysic = Witch(
        "grannys_physic", "Granny's Physic", WorkingTag, 2, WitchActions.Hearth,
        "Remove every negative Status from yourself. Heal 3 HP lost this combat, and 2 more for each stack removed " +
        "(at most 10 more). Exhaust.", Physic(), "rare", tags: [ExhaustTag]);

    private static readonly BnbCard StoneSoup = Witch(
        "stone_soup", "Stone Soup", WorkingTag, 0, WitchActions.Hearth,
        "Name a family: the first Junk card in your hand becomes a Soup Stone of it and goes into the cauldron. Draw 1 card.",
        Seq(Soup(), Draw(1)), "rare", act: 3);

    private static readonly BnbCard KeepTheDrippings = Witch(
        "keep_the_drippings", "Keep the Drippings", RiteTag, 1, WitchActions.Hearth,
        "Hearth healing that the combat-heal cap would waste becomes Ward Wax instead.",
        Apply(WitchRules.KeepTheDrippings, 1, You), "rare", act: 4);

    private static readonly BnbCard NeverWashThePot = Witch(
        "never_wash_the_pot", "Never Wash the Pot", RiteTag, 2, WitchActions.Hearth,
        "After each Brew, keep one of its ingredients in the cauldron.",
        Apply(WitchRules.NeverWashThePot, 1, You), "rare", act: 4);

    private static readonly BnbCard ForWhatAilsYou = Witch(
        "for_what_ails_you", "For What Ails You", WorkingTag, 1, WitchActions.Hearth,
        "Choose one: heal 4 HP lost this combat; remove 2 stacks of negative Statuses from yourself; draw 2 cards; " +
        "or your next card into the cauldron is free.", Ails(1), "rare", act: 2);

    // ── Fortune ───────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard LoadedKnucklebones = Witch(
        "loaded_knucklebones", "Loaded Knucklebones", WorkingTag, 1, WitchActions.Fortune,
        "Apply 3 Misfortune. Its next roll is rolled twice; either may land.",
        Seq(Apply(WitchKeywords.Misfortune, 3), Apply(WitchRules.Loaded, 1)), "rare");

    private static readonly BnbCard SevenYearsBadLuck = Witch(
        "seven_years_bad_luck", "Seven Years' Bad Luck", WorkingTag, 2, WitchActions.Fortune,
        "Apply 8 Misfortune. Until it lands, every roll the target wins leaves half the Misfortune behind.",
        Seq(Apply(WitchKeywords.Misfortune, 8), Apply(WitchRules.SevenYears, 1)), "rare", act: 2);

    private static readonly BnbCard NinthLife = Witch(
        "ninth_life", "Ninth Life", RiteTag, 1, WitchActions.Fortune,
        "After the first Misfortune that lands this combat, that enemy is left 4 Misfortune.",
        Apply(WitchRules.NinthLife, 4, You), "rare", act: 3);

    private static readonly BnbCard BorrowedLuck = Witch(
        "borrowed_luck", "Borrowed Luck", WorkingTag, 1, WitchActions.Fortune,
        "Remove all Misfortune from an enemy. Gain 2 Block for each, and draw 1 card for every 4.", Borrow(2), "rare");

    private static readonly BnbCard TheBlackCatSatDown = Witch(
        "the_black_cat_sat_down", "The Black Cat Sat Down", WorkingTag, 2, WitchActions.Fortune,
        "The target's next action fails. Lose 4 HP. Shuffle 2 Mirror Shards into your draw pile. Exhaust.",
        SatDown(), "rare", tags: [ExhaustTag], act: 4);

    public static IReadOnlyList<BnbCard> Rares() =>
    [
        WolfAtTheDoor, WolfAtTheDoor.Upgraded("Retain. Deal 26 damage. Each turn it waits in your hand, it costs 1 less.", Wolf(26)),
        CarrionFlight, CarrionFlight.Upgraded(
            "Deal 3 damage 8 times. Once the target has fallen, the rest strike a random enemy.", Carrion(8, 3)),
        TeethInTheDark, TeethInTheDark.Upgraded("Deal 8 damage, and 5 more for each card in the cauldron.",
            Damage(PerInPot(8, 5))),
        Turnskin, Turnskin.Upgraded("Every Husk card in the cauldron turns into an Adder's Nip. Deal 12 damage.",
            Seq(Turn(null), Damage(12))),
        TheHareRunsLast, TheHareRunsLast.Upgraded(
            "Deal 11 damage. If its Hex bursts at the end of its next turn, it takes 11 damage again after the burst.",
            Seq(Damage(11), Apply(WitchRules.HareRunsLast, 11))),

        CallTheThirdNight, CallTheThirdNight.Upgraded(
            "The target's Hex bursts now: it loses 3 HP for each Hexed. Its Threefold count starts again.", cost: 1),
        NameWrittenBackwards, NameWrittenBackwards.Upgraded(
            "Double the target's Hexed, then apply 3 more. Its Threefold count starts again.", Backwards(3)),
        HexInTheRafters, HexInTheRafters.Upgraded(
            "The first Hex burst each enemy turn gives every other enemy 5 Hexed.", Apply(WitchRules.HexInTheRafters, 5, You)),
        LastBreathHex, LastBreathHex.Upgraded(
            "Apply 9 Hexed. When the target dies, the healthiest other enemy takes on all its Hexed and its Threefold count.",
            Seq(Apply(WitchKeywords.Hexed, 9), Apply(WitchRules.LastBreathHex, 1))),
        BaneRoot, BaneRoot.Upgraded("After a Hex bursts, that enemy gains 3 Hexed.", Apply(WitchRules.BaneRoot, 3, You)),

        ShutTheLid, ShutTheLid.Upgraded(
            "Gain 12 Block. Until your next turn the cauldron shelters you however full it is, and nothing goes in or out.",
            Seq(Block(12), Apply(WitchRules.ShutTheLid, 1, You))),
        ScarBark, ScarBark.Upgraded(
            "Gain 8 Block, and 1 more for each HP attacks took from you last enemy turn (at most 25 more).",
            Block(Scarred(8, 25))),
        FullPot, FullPot.Upgraded("Gain 6 Block, and 4 more for each card in the cauldron. If it is Ready, gain 8 more.",
            Block(Plus(PerInPot(6, 4), Times(PotReady, 8)))),
        WinterBark, WinterBark.Upgraded("Gain 20 Block. After the enemy turn, up to 8 of your unused Block becomes Ward Wax.",
            Seq(Block(20), Apply(WitchRules.WinterBark, 8, You))),
        Ironwood, Ironwood.Upgraded("The first time each turn an attack gets through your Block, gain 6 Ward Wax.",
            Apply(WitchRules.Ironwood, 6, You)),

        GrannysPhysic, GrannysPhysic.Upgraded(
            "Remove every negative Status from yourself. Heal 3 HP lost this combat, and 2 more for each stack removed " +
            "(at most 10 more). Exhaust.", cost: 1),
        StoneSoup, StoneSoup.Upgraded(
            "Name a family: the first Junk card in your hand becomes a Soup Stone of it and goes into the cauldron. Draw 2 cards.",
            Seq(Soup(), Draw(2))),
        KeepTheDrippings, KeepTheDrippings.Upgraded(
            "Hearth healing that the combat-heal cap would waste becomes Ward Wax instead.", cost: 0),
        NeverWashThePot, NeverWashThePot.Upgraded("After each Brew, keep one of its ingredients in the cauldron.", cost: 1),
        ForWhatAilsYou, ForWhatAilsYou.Upgraded(
            "Choose two: heal 4 HP lost this combat; remove 2 stacks of negative Statuses from yourself; draw 2 cards; " +
            "or your next card into the cauldron is free.", Ails(2)),

        LoadedKnucklebones, LoadedKnucklebones.Upgraded("Apply 6 Misfortune. Its next roll is rolled twice; either may land.",
            Seq(Apply(WitchKeywords.Misfortune, 6), Apply(WitchRules.Loaded, 1))),
        SevenYearsBadLuck, SevenYearsBadLuck.Upgraded(
            "Apply 11 Misfortune. Until it lands, every roll the target wins leaves half the Misfortune behind.",
            Seq(Apply(WitchKeywords.Misfortune, 11), Apply(WitchRules.SevenYears, 1))),
        NinthLife, NinthLife.Upgraded("After the first Misfortune that lands this combat, that enemy is left 7 Misfortune.",
            Apply(WitchRules.NinthLife, 7, You)),
        BorrowedLuck, BorrowedLuck.Upgraded(
            "Remove all Misfortune from an enemy. Gain 3 Block for each, and draw 1 card for every 4.", Borrow(3)),
        TheBlackCatSatDown, TheBlackCatSatDown.Upgraded(
            "The target's next action fails. Lose 4 HP. Shuffle 2 Mirror Shards into your draw pile. Exhaust.", cost: 1),
    ];

    // ── Soup Stones: what Stone Soup makes of a Junk card — an ingredient of the family she named ─────────

    public static string SoupStoneId(string family) => "soup_stone_" + family["fam_".Length..];

    private static BnbCard SoupStone(string family, string familyName) => new(
        SoupStoneId(family), $"Soup Stone ({familyName})", JunkTag, 0,
        $"Unplayable. In the cauldron, it is {familyName}.", Seq(),
        Rarity: "junk", Tags: [UnplayableTag, family, CharacterTag]);

    // A property, not a field: the cards above are built during type initialisation and read it first.
    private static (string Family, string Name)[] FamilyNames =>
    [
        (WitchActions.Fang, "Fang"), (WitchActions.Hex, "Hex"), (WitchActions.Husk, "Husk"),
        (WitchActions.Hearth, "Hearth"), (WitchActions.Fortune, "Fortune"),
    ];

    public static IReadOnlyList<BnbCard> SoupStones() => [.. FamilyNames.Select(f => SoupStone(f.Family, f.Name))];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // The wolf's price falls while it waits (WitchRules' Habits feed the discount); played, the waiting is over.
    private static CombatNodeModel Wolf(int damage) =>
        Seq(Damage(damage), new CombatNodeModel("removeStatus", You, StatusId: WitchRules.WolfWaits));

    // Each strike asks whether the target still stands; once it has fallen, a random enemy takes the rest.
    private static CombatNodeModel Carrion(int hits, int damage) =>
        Repeat(hits, If(new CombatConditionSpec("isAlive", Target),
            Damage(damage),
            CombatNodeModel.RandomTargets(AllEnemies, CombatAmountSpec.FromConst(1), Damage(damage, "iterationTarget"))));

    private static CombatAmountSpec PerInPot(int baseAmount, int per) =>
        Plus(CombatAmountSpec.FromConst(baseAmount), Times(CardsInZone(CardZone.SetAsidePile), per));

    // 1 when the pot is Ready — three in it — and 0 otherwise.
    private static CombatAmountSpec PotReady =>
        Once(CombatAmountSpec.Binary("max", CombatAmountSpec.FromConst(0),
            CombatAmountSpec.Binary("sub", CardsInZone(CardZone.SetAsidePile), CombatAmountSpec.FromConst(WitchActions.Slots - 1))));

    // A Husk card in the pot becomes an Adder's Nip — the first one, or every one (null).
    private static CombatNodeModel Turn(int? first) =>
        CombatNodeModel.ForEachCard(You, CardZone.SetAsidePile,
            new CombatNodeModel("transformCard", You, Card: new CombatCardSpec("iterated"), ToDefinition: "adders_nip"),
            tag: WitchActions.Husk, takeFirst: first);

    // The third night, called: the burst's own HP loss, and the count starts again.
    private static CombatNodeModel ThirdNightNow() =>
        Seq(
            new CombatNodeModel("dealDamage", Target,
                Times(Stacks(WitchKeywords.Hexed), WitchKeywords.ThreefoldMultiplier),
                IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
            ResetCount());

    private static CombatNodeModel ResetCount() =>
        new("setCombatantCounter", Target, CombatAmountSpec.FromConst(0),
            CounterId: WitchKeywords.ThreefoldStep.value, Relative: false);

    private static CombatNodeModel Backwards(int more) =>
        Seq(Apply(WitchKeywords.Hexed, Plus(Stacks(WitchKeywords.Hexed), CombatAmountSpec.FromConst(more))), ResetCount());

    private static CombatAmountSpec Scarred(int baseAmount, int ceiling) =>
        Plus(CombatAmountSpec.FromConst(baseAmount),
            AtMost(CombatAmountSpec.Counter(You, Cards.Keywords.StruckLastRoundCounter.value), ceiling));

    private static CounterId Treated => new("grannys_physic_treated");

    private static CombatNodeModel Physic() =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You,
                new CombatAmountSpec("stacksByPolarity", SelectorKey: You, Polarity: StatusPolarity.Debuff),
                CounterId: Treated.value, Relative: false),
            new CombatNodeModel("cleanse", You, Polarity: StatusPolarity.Debuff),
            Heal(Plus(CombatAmountSpec.FromConst(3), AtMost(Times(CombatAmountSpec.Counter(You, Treated.value), 2), 10))));

    // Name a family; the first Junk in hand becomes that family's Soup Stone, and into the pot it goes.
    private static CombatNodeModel Soup() =>
        CombatNodeModel.ChooseOptions(1,
            [.. FamilyNames.Select(f => f.Name)],
            [.. FamilyNames.Select(f => CombatNodeModel.ForEachCard(You, CardZone.Hand,
                Seq(
                    new CombatNodeModel("transformCard", You, Card: new CombatCardSpec("iterated"),
                        ToDefinition: SoupStoneId(f.Family)),
                    new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"),
                        ToZone: CardZone.SetAsidePile)),
                tag: JunkTag, takeFirst: 1))],
            "name a family");

    private static CombatNodeModel Ails(int picks) =>
        CombatNodeModel.ChooseOptions(picks,
            ["heal 4 HP lost this combat", "remove 2 stacks of negative Statuses", "draw 2 cards",
             "your next card into the cauldron is free"],
            [Heal(4), Cleanse(2), Draw(2), FreeIngredient()],
            picks == 1 ? "choose one" : "choose two");

    private static CounterId Borrowed => new("borrowed_luck_taken");

    private static CombatNodeModel Borrow(int blockEach) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, Stacks(WitchKeywords.Misfortune),
                CounterId: Borrowed.value, Relative: false),
            new CombatNodeModel("removeStatus", Target, StatusId: WitchKeywords.Misfortune),
            Block(Times(CombatAmountSpec.Counter(You, Borrowed.value), blockEach)),
            new CombatNodeModel("drawCards", You,
                CombatAmountSpec.Binary("div", CombatAmountSpec.Counter(You, Borrowed.value), CombatAmountSpec.FromConst(4))));

    // The cat sits: a roll that cannot fail (it needs a Misfortune to be rolled at all), and what it costs her.
    private static CombatNodeModel SatDown() =>
        Seq(
            Apply(WitchRules.BlackCatSatDown, 1),
            Apply(WitchKeywords.Misfortune, 1),
            new CombatNodeModel("dealDamage", You, CombatAmountSpec.FromConst(4),
                IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
            AddJunk(MirrorShardId, CardZone.DrawPile, 2));
}
