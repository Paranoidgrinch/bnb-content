using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's thirty-five uncommons (hedge_witch_master.md §12): build direction, cauldron handling, the
// third night's timing and the roll's. Numbers by the approved E7 rules; two thirds open in Act I, the rest in
// Act II (plan E2). A rule that outlives the card lives on a WitchRules status.
public static partial class WitchCards
{
    // ── Fang ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BadgersTemper = Witch(
        "badgers_temper", "Badger's Temper", DeedTag, 1, WitchActions.Fang,
        "Deal 7 damage. If the target intends to Attack, deal 4 more.", Temper(7, 4), "uncommon");

    private static readonly BnbCard AdderInTheSleeve = Witch(
        "adder_in_the_sleeve", "Adder in the Sleeve", DeedTag, 1, WitchActions.Fang,
        "Retain. Deal 5 damage, and 3 more for each turn it has waited in your hand (at most 9 more).",
        Sleeve(5, 3), "uncommon", act: 2) with { RetainInHand = true };

    private static readonly BnbCard MurderOfCrows = Witch(
        "murder_of_crows", "Murder of Crows", DeedTag, 1, WitchActions.Fang,
        "Deal 2 damage to a random enemy 4 times.", Crows(4, 2), "uncommon");

    private static readonly BnbCard HawthornSwitch = Witch(
        "hawthorn_switch", "Hawthorn Switch", DeedTag, 1, WitchActions.Fang,
        "Deal 8 damage. If the target is Hexed, its Threefold count moves on by 1.",
        Seq(Damage(8), If(IsHexed, Step(1))), "uncommon");

    private static readonly BnbCard BiteTheHand = Witch(
        "bite_the_hand", "Bite the Hand", DeedTag, 1, WitchActions.Fang,
        "Deal 6 damage. If the target has any positive Status, deal 6 more.", Bite(6, 6), "uncommon", act: 2);

    private static readonly BnbCard FamiliarsSupper = Witch(
        "familiars_supper", "Familiar's Supper", DeedTag, 1, WitchActions.Fang,
        "Deal 8 damage. If this kills, your next card into the cauldron is free.", Supper(8), "uncommon");

    private static readonly BnbCard HedgeThing = Witch(
        "hedge_thing", "Hedge-Thing", DeedTag, 1, WitchActions.Fang,
        "Deal 7 damage. If the cauldron is Sheltering, deal 4 more.", Damage(Sheltered(7, 4)), "uncommon");

    // ── Hex ───────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ThriceSpokenName = Witch(
        "thrice_spoken_name", "Thrice-Spoken Name", WorkingTag, 1, WitchActions.Hex,
        "Apply 4 Hexed. If its Hex bursts at the end of its next turn, apply 6 more.", Knock(4, 6), "uncommon");

    private static readonly BnbCard CharmBackwards = Witch(
        "charm_backwards", "Charm Backwards", WorkingTag, 1, WitchActions.Hex,
        "Apply 8 Hexed. Its Threefold count goes back by 1.",
        Seq(Apply(WitchKeywords.Hexed, 8), Step(-1)), "uncommon", act: 2);

    private static readonly BnbCard DeadMansHex = Witch(
        "dead_mans_hex", "Dead Man's Hex", WorkingTag, 1, WitchActions.Hex,
        "Apply 5 Hexed. When the target dies, the healthiest other enemy takes on half its Hexed.",
        Seq(Apply(WitchKeywords.Hexed, 5), Apply(WitchRules.DeadMansHex, 1)), "uncommon", act: 2);

    private static readonly BnbCard SourTheMilk = Witch(
        "sour_the_milk", "Sour the Milk", WorkingTag, 1, WitchActions.Hex,
        "Apply 4 Hexed. Another negative Status on the target gains 1 stack.", Sour(4, 1), "uncommon");

    private static readonly BnbCard NailInTheDoorpost = Witch(
        "nail_in_the_doorpost", "Nail in the Doorpost", RiteTag, 1, WitchActions.Hex,
        "The first time each turn you apply Hexed to an enemy, the healthiest other enemy gains 2 Hexed.",
        Apply(WitchRules.NailInTheDoorpost, 2, You), "uncommon", act: 2);

    private static readonly BnbCard ThirdBell = Witch(
        "third_bell", "Third Bell", WorkingTag, 1, WitchActions.Hex,
        "Apply 3 Hexed. If its Hex bursts at the end of its next turn, every other enemy gains 3 Hexed.",
        Seq(Apply(WitchKeywords.Hexed, 3), Apply(WitchRules.ThirdBell, 3)), "uncommon");

    // A special ingredient (R8): a plain card, a strong one in the pot.
    private static readonly BnbCard KnottedCord = Witch(
        "knotted_cord", "Knotted Cord", WorkingTag, 1, WitchActions.Hex,
        "Apply 2 Hexed. In the cauldron, it counts as two Hex.", Apply(WitchKeywords.Hexed, 2), "uncommon",
        tags: [WitchActions.Double(WitchActions.Hex)]);

    // ── Husk ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard Oakskin = Witch(
        "oakskin", "Oakskin", WorkingTag, 1, WitchActions.Husk,
        "Gain 6 Block. If the cauldron is Brewing, gain 4 more.", Block(Brewing(6, 4)), "uncommon");

    private static readonly BnbCard ClampTheLid = Witch(
        "clamp_the_lid", "Clamp the Lid", WorkingTag, 1, WitchActions.Husk,
        "Gain 7 Block. If the cauldron is Sheltering, gain 4 Ward Wax.",
        Seq(Block(7), Apply(Cards.Keywords.WardWax, Sheltered(0, 4), You)), "uncommon");

    private static readonly BnbCard HedgehogCurl = Witch(
        "hedgehog_curl", "Hedgehog Curl", WorkingTag, 1, WitchActions.Husk,
        "Gain 5 Block. Every enemy attack on you this turn costs the attacker 2 damage.",
        Seq(Block(5), Apply(WitchRules.HedgehogCurl, 2, You)), "uncommon");

    private static readonly BnbCard SloughOff = Witch(
        "slough_off", "Slough Off", WorkingTag, 1, WitchActions.Husk,
        "Gain 6 Block. Remove 1 stack of a negative Status from yourself; if you did, draw 1 card.",
        Slough(6), "uncommon");

    private static readonly BnbCard SnailsPatience = Witch(
        "snails_patience", "Snail's Patience", WorkingTag, 1, WitchActions.Husk,
        "Gain 6 Block. If this is your first or second card this turn, gain 6 Block next turn.",
        Patience(6, 6), "uncommon", act: 2);

    private static readonly BnbCard HawthornWall = Witch(
        "hawthorn_wall", "Hawthorn Wall", WorkingTag, 2, WitchActions.Husk,
        "Gain 12 Block. Every enemy that attacks you this turn gains 2 Hexed.",
        Seq(Block(12), Apply(WitchRules.HawthornWall, 2, You)), "uncommon", act: 2);

    private static readonly BnbCard HornSpoon = Witch(
        "horn_spoon", "Horn Spoon", WorkingTag, 1, WitchActions.Husk,
        $"Gain 6 Block. In the cauldron, whatever is brewed also gives {WitchActions.WaxRiderStacks} Ward Wax.",
        Block(6), "uncommon", tags: [WitchActions.WaxRider]);

    // ── Hearth ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ComfreyPoultice = Witch(
        "comfrey_poultice", "Comfrey Poultice", WorkingTag, 1, WitchActions.Hearth,
        "Heal 3 HP lost this combat. If you are at or below half your HP, heal 7 instead.", Comfrey(3, 7), "uncommon");

    private static readonly BnbCard HoneyAndOnion = Witch(
        "honey_and_onion", "Honey and Onion", WorkingTag, 1, WitchActions.Hearth,
        "Heal 2 HP lost this combat. Remove 2 stacks of negative Statuses from yourself.",
        Seq(Heal(2), Cleanse(2)), "uncommon");

    private static readonly BnbCard ProperSupper = Witch(
        "proper_supper", "Proper Supper", WorkingTag, 1, WitchActions.Hearth,
        "Gain 5 Block. At the start of your next turn, heal 3 HP lost this combat.",
        Seq(Block(5), Apply(WitchRules.ProperSupper, 3, You)), "uncommon");

    private static readonly BnbCard MidwifesHands = Witch(
        "midwifes_hands", "Midwife's Hands", RiteTag, 1, WitchActions.Hearth,
        "The first time you fall to a third of your HP or below this combat, heal 8 HP lost this combat.",
        Apply(WitchRules.MidwifesHands, 8, You), "uncommon", act: 2);

    private static readonly BnbCard TellTheBees = Witch(
        "tell_the_bees", "Tell the Bees", RiteTag, 1, WitchActions.Hearth,
        "Once each turn, when an enemy dies, heal 3 HP lost this combat.",
        Apply(WitchRules.TellTheBees, 3, You), "uncommon", act: 2);

    private static readonly BnbCard TasteTheBroth = Witch(
        "taste_the_broth", "Taste the Broth", WorkingTag, 0, WitchActions.Hearth,
        "Take a card from the cauldron back into your hand. Heal 2 HP lost this combat.",
        Seq(TakeBack(), Heal(2)), "uncommon");

    private static readonly BnbCard PutTheKettleOn = Witch(
        "put_the_kettle_on", "Put the Kettle On", WorkingTag, 1, WitchActions.Hearth,
        "Gain 4 Block. Heal 1 HP lost this combat. Your next card into the cauldron is free.",
        Seq(Block(4), Heal(1), FreeIngredient()), "uncommon");

    // ── Fortune ───────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard CrossYourFingers = Witch(
        "cross_your_fingers", "Cross Your Fingers", WorkingTag, 1, WitchActions.Fortune,
        "Apply 5 Misfortune. If its roll fails, you gain 6 Block before its action.",
        Seq(Apply(WitchKeywords.Misfortune, 5), Apply(WitchRules.CrossedFingers, 6)), "uncommon");

    private static readonly BnbCard BadPenny = Witch(
        "bad_penny", "Bad Penny", WorkingTag, 1, WitchActions.Fortune,
        "Apply 5 Misfortune. If its roll fails, 3 Misfortune stays for its next action.",
        Seq(Apply(WitchKeywords.Misfortune, 5), Apply(WitchRules.BadPenny, 3)), "uncommon");

    private static readonly BnbCard CastTheKnucklebones = Witch(
        "cast_the_knucklebones", "Cast the Knucklebones", WorkingTag, 1, WitchActions.Fortune,
        "Choose one: apply 5 Misfortune; or apply 0 to 12 Misfortune, at random.", Knucklebones(5, 12), "uncommon");

    private static readonly BnbCard HorseshoeOverTheDoor = Witch(
        "horseshoe_over_the_door", "Horseshoe Over the Door", RiteTag, 1, WitchActions.Fortune,
        "The first time each turn you apply Misfortune to an enemy, it gains 2 more.",
        Apply(WitchRules.HorseshoeOverTheDoor, 2, You), "uncommon", act: 2);

    private static readonly BnbCard MagpiesLuck = Witch(
        "magpies_luck", "Magpie's Luck", WorkingTag, 1, WitchActions.Fortune,
        "Apply 5 Misfortune. If it makes the target's action fail, draw 2 more cards next turn.",
        Seq(Apply(WitchKeywords.Misfortune, 5), Apply(WitchRules.MagpiesLuck, 2)), "uncommon", act: 2);

    private static readonly BnbCard BrokenMirror = Witch(
        "broken_mirror", "Broken Mirror", WorkingTag, 1, WitchActions.Fortune,
        "Apply 10 Misfortune. Shuffle a Mirror Shard into your draw pile.",
        Seq(Apply(WitchKeywords.Misfortune, 10), AddJunk(MirrorShardId, CardZone.DrawPile)), "uncommon", act: 2);

    private static readonly BnbCard SevenMagpies = Witch(
        "seven_magpies", "Seven Magpies", WorkingTag, 1, WitchActions.Fortune,
        "Apply 3 Misfortune to ALL enemies. If only one enemy stands, it gains 3 more.", Magpies(3, 3), "uncommon");

    public static IReadOnlyList<BnbCard> Uncommons() =>
    [
        BadgersTemper, BadgersTemper.Upgraded("Deal 9 damage. If the target intends to Attack, deal 6 more.", Temper(9, 6)),
        AdderInTheSleeve, AdderInTheSleeve.Upgraded(
            "Retain. Deal 7 damage, and 4 more for each turn it has waited in your hand (at most 12 more).", Sleeve(7, 4)),
        MurderOfCrows, MurderOfCrows.Upgraded("Deal 3 damage to a random enemy 4 times.", Crows(4, 3)),
        HawthornSwitch, HawthornSwitch.Upgraded(
            "Deal 11 damage. If the target is Hexed, its Threefold count moves on by 1.",
            Seq(Damage(11), If(IsHexed, Step(1)))),
        BiteTheHand, BiteTheHand.Upgraded("Deal 8 damage. If the target has any positive Status, deal 9 more.", Bite(8, 9)),
        FamiliarsSupper, FamiliarsSupper.Upgraded(
            "Deal 11 damage. If this kills, your next card into the cauldron is free.", Supper(11)),
        HedgeThing, HedgeThing.Upgraded("Deal 10 damage. If the cauldron is Sheltering, deal 5 more.",
            Damage(Sheltered(10, 5))),

        ThriceSpokenName, ThriceSpokenName.Upgraded(
            "Apply 5 Hexed. If its Hex bursts at the end of its next turn, apply 9 more.", Knock(5, 9)),
        CharmBackwards, CharmBackwards.Upgraded("Apply 12 Hexed. Its Threefold count goes back by 1.",
            Seq(Apply(WitchKeywords.Hexed, 12), Step(-1))),
        DeadMansHex, DeadMansHex.Upgraded(
            "Apply 7 Hexed. When the target dies, the healthiest other enemy takes on all its Hexed.",
            Seq(Apply(WitchKeywords.Hexed, 7), Apply(WitchRules.DeadMansHex, 2))),
        SourTheMilk, SourTheMilk.Upgraded("Apply 6 Hexed. Another negative Status on the target gains 2 stacks.",
            Sour(6, 2)),
        NailInTheDoorpost, NailInTheDoorpost.Upgraded(
            "The first time each turn you apply Hexed to an enemy, the healthiest other enemy gains 3 Hexed.",
            Apply(WitchRules.NailInTheDoorpost, 3, You)),
        ThirdBell, ThirdBell.Upgraded(
            "Apply 5 Hexed. If its Hex bursts at the end of its next turn, every other enemy gains 5 Hexed.",
            Seq(Apply(WitchKeywords.Hexed, 5), Apply(WitchRules.ThirdBell, 5))),
        KnottedCord, KnottedCord.Upgraded("Apply 3 Hexed. Draw 1 card. In the cauldron, it counts as two Hex.",
            Seq(Apply(WitchKeywords.Hexed, 3), Draw(1))),

        Oakskin, Oakskin.Upgraded("Gain 8 Block. If the cauldron is Brewing, gain 6 more.", Block(Brewing(8, 6))),
        ClampTheLid, ClampTheLid.Upgraded("Gain 10 Block. If the cauldron is Sheltering, gain 5 Ward Wax.",
            Seq(Block(10), Apply(Cards.Keywords.WardWax, Sheltered(0, 5), You))),
        HedgehogCurl, HedgehogCurl.Upgraded(
            "Gain 7 Block. Every enemy attack on you this turn costs the attacker 3 damage.",
            Seq(Block(7), Apply(WitchRules.HedgehogCurl, 3, You))),
        SloughOff, SloughOff.Upgraded(
            "Gain 9 Block. Remove 1 stack of a negative Status from yourself; if you did, draw 1 card.", Slough(9)),
        SnailsPatience, SnailsPatience.Upgraded(
            "Gain 8 Block. If this is your first or second card this turn, gain 8 Block next turn.", Patience(8, 8)),
        HawthornWall, HawthornWall.Upgraded("Gain 16 Block. Every enemy that attacks you this turn gains 3 Hexed.",
            Seq(Block(16), Apply(WitchRules.HawthornWall, 3, You))),
        HornSpoon, HornSpoon.Upgraded(
            $"Gain 9 Block. In the cauldron, whatever is brewed also gives {WitchActions.WaxRiderStacks} Ward Wax.",
            Block(9)),

        ComfreyPoultice, ComfreyPoultice.Upgraded(
            "Heal 4 HP lost this combat. If you are at or below half your HP, heal 10 instead.", Comfrey(4, 10)),
        HoneyAndOnion, HoneyAndOnion.Upgraded(
            "Heal 3 HP lost this combat. Remove 3 stacks of negative Statuses from yourself.", Seq(Heal(3), Cleanse(3))),
        ProperSupper, ProperSupper.Upgraded("Gain 7 Block. At the start of your next turn, heal 4 HP lost this combat.",
            Seq(Block(7), Apply(WitchRules.ProperSupper, 4, You))),
        MidwifesHands, MidwifesHands.Upgraded(
            "The first time you fall to a third of your HP or below this combat, heal 12 HP lost this combat.",
            Apply(WitchRules.MidwifesHands, 12, You)),
        TellTheBees, TellTheBees.Upgraded("Once each turn, when an enemy dies, heal 5 HP lost this combat.",
            Apply(WitchRules.TellTheBees, 5, You)),
        TasteTheBroth, TasteTheBroth.Upgraded(
            "Take a card from the cauldron back into your hand. Heal 4 HP lost this combat.", Seq(TakeBack(), Heal(4))),
        PutTheKettleOn, PutTheKettleOn.Upgraded(
            "Gain 7 Block. Heal 1 HP lost this combat. Your next card into the cauldron is free.",
            Seq(Block(7), Heal(1), FreeIngredient())),

        CrossYourFingers, CrossYourFingers.Upgraded("Apply 6 Misfortune. If its roll fails, you gain 9 Block before its action.",
            Seq(Apply(WitchKeywords.Misfortune, 6), Apply(WitchRules.CrossedFingers, 9))),
        BadPenny, BadPenny.Upgraded("Apply 6 Misfortune. If its roll fails, 5 Misfortune stays for its next action.",
            Seq(Apply(WitchKeywords.Misfortune, 6), Apply(WitchRules.BadPenny, 5))),
        CastTheKnucklebones, CastTheKnucklebones.Upgraded(
            "Choose one: apply 7 Misfortune; or apply 0 to 16 Misfortune, at random.", Knucklebones(7, 16)),
        HorseshoeOverTheDoor, HorseshoeOverTheDoor.Upgraded(
            "The first time each turn you apply Misfortune to an enemy, it gains 3 more.",
            Apply(WitchRules.HorseshoeOverTheDoor, 3, You)),
        MagpiesLuck, MagpiesLuck.Upgraded("Apply 7 Misfortune. If it makes the target's action fail, draw 2 more cards next turn.",
            Seq(Apply(WitchKeywords.Misfortune, 7), Apply(WitchRules.MagpiesLuck, 2))),
        BrokenMirror, BrokenMirror.Upgraded("Apply 13 Misfortune. Shuffle a Mirror Shard into your draw pile.",
            Seq(Apply(WitchKeywords.Misfortune, 13), AddJunk(MirrorShardId, CardZone.DrawPile))),
        SevenMagpies, SevenMagpies.Upgraded(
            "Apply 4 Misfortune to ALL enemies. If only one enemy stands, it gains 4 more.", Magpies(4, 4)),
    ];

    // ── the Witch's own Junk ──────────────────────────────────────────────────────────────────────────────

    // No family: in the pot it is Dregs (E5).
    public const string MirrorShardId = "mirror_shard";

    private static readonly BnbCard MirrorShard = new(
        MirrorShardId, "Mirror Shard", JunkTag, 0, "Unplayable. Seven years, it says.", Seq(),
        Rarity: "junk", Tags: [UnplayableTag, MirrorShardId, CharacterTag]);

    public static IReadOnlyList<BnbCard> Junk() => [MirrorShard];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel Temper(int damage, int more) =>
        Seq(Damage(damage), If(new CombatConditionSpec("intends", Target, Id: nameof(IntentKind.Attack)), Damage(more)));

    // Grows with every turn it has waited (WitchRules' Habits count it), at most three; playing it starts again.
    private static CombatNodeModel Sleeve(int damage, int per) =>
        Seq(
            Damage(Plus(CombatAmountSpec.FromConst(damage),
                Times(AtMost(CombatAmountSpec.Counter(You, WitchRules.SleeveWaited.value), 3), per))),
            new CombatNodeModel("setCombatantCounter", You, CombatAmountSpec.FromConst(0),
                CounterId: WitchRules.SleeveWaited.value, Relative: false));

    private static CombatNodeModel Crows(int hits, int damage) =>
        CombatNodeModel.RandomTargets(AllEnemies, CombatAmountSpec.FromConst(hits), Damage(damage, "iterationTarget"));

    // The target's Threefold count, moved on or back.
    private static CombatNodeModel Step(int by) =>
        new("setCombatantCounter", Target, CombatAmountSpec.FromConst(by),
            CounterId: WitchKeywords.ThreefoldStep.value, Relative: true);

    private static CombatNodeModel Bite(int damage, int more) =>
        Damage(Plus(CombatAmountSpec.FromConst(damage), Times(Once(
            new CombatAmountSpec("stacksByPolarity", SelectorKey: Target, Polarity: StatusPolarity.Buff)), more)));

    private static CombatNodeModel FreeIngredient() => Apply(WitchKeywords.FreeIngredient, 1, You);

    // "If this kills": the enemies standing are counted before the blow and again after it — a target that has
    // fallen is no longer a target anything can ask about.
    private static CounterId Standing => new("familiars_supper_standing");

    private static CombatAmountSpec EnemiesStanding => new("countTargets", ReadSelector: new CombatSelectorSpec(AllEnemies));

    private static CombatNodeModel Supper(int damage) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, EnemiesStanding, CounterId: Standing.value, Relative: false),
            Damage(damage),
            Apply(WitchKeywords.FreeIngredient, Once(CombatAmountSpec.Binary("max", CombatAmountSpec.FromConst(0),
                CombatAmountSpec.Binary("sub", CombatAmountSpec.Counter(You, Standing.value), EnemiesStanding))), You));

    private static CombatNodeModel Sour(int hexed, int more) =>
        Seq(
            Apply(WitchKeywords.Hexed, hexed),
            new CombatNodeModel("modifySelectedStatusStacks", Target, CombatAmountSpec.FromConst(more),
                Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff)
                    { Except = new StatusDefinitionId(WitchKeywords.Hexed) }));

    // base, plus a bonus while the pot holds anything.
    private static CombatAmountSpec Brewing(int baseAmount, int bonus) =>
        Plus(CombatAmountSpec.FromConst(baseAmount), Times(Once(CardsInZone(CardZone.SetAsidePile)), bonus));

    // "If you did, draw 1": what she carried is read before anything is taken, and the draw comes after the
    // shedding — so shedding Panic lets the card be drawn.
    private static CounterId Carried => new("slough_off_carried");

    private static CombatNodeModel Slough(int block) =>
        Seq(
            Block(block),
            new CombatNodeModel("setCombatantCounter", You,
                new CombatAmountSpec("stacksByPolarity", SelectorKey: You, Polarity: StatusPolarity.Debuff),
                CounterId: Carried.value, Relative: false),
            Cleanse(1),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                Op: ComparisonOperator.Greater, Right: 0, Id: Carried.value), Draw(1)));

    private static CounterId PlayedSoFar => new("snails_patience_played");

    private static CombatNodeModel Patience(int block, int later) =>
        Seq(
            Block(block),
            new CombatNodeModel("setCombatantCounter", You, new CombatAmountSpec("cardsPlayedThisTurn", SelectorKey: You),
                CounterId: PlayedSoFar.value, Relative: false),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.LessOrEqual, Right: 2, Id: PlayedSoFar.value),
                Apply(Cards.Keywords.PromisedBlock, later, You)));

    private static CombatNodeModel Comfrey(int heal, int low) =>
        If(new CombatConditionSpec("compare", You, ValueKind: "healthPercentage",
            Op: ComparisonOperator.LessOrEqual, Right: 50), Heal(low), Heal(heal));

    // One card back out of the pot — if there is one to take.
    private static CombatNodeModel TakeBack() =>
        CombatNodeModel.Repeat(Once(CardsInZone(CardZone.SetAsidePile)),
            new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.SetAsidePile, Purpose: "take a card back from the cauldron"),
                ToZone: CardZone.Hand));

    private static CombatNodeModel Knucklebones(int sure, int ceiling) =>
        CombatNodeModel.ChooseOptions(
            1, [$"apply {sure} Misfortune", $"apply 0 to {ceiling} Misfortune, at random"],
            [Apply(WitchKeywords.Misfortune, sure),
             Apply(WitchKeywords.Misfortune, new CombatAmountSpec("randomBelow", ceiling + 1))],
            "choose one");

    // Spread over every enemy; against a lone one, the rest of the flock comes down on it too.
    private static CombatNodeModel Magpies(int each, int lone) =>
        Seq(
            Apply(WitchKeywords.Misfortune, each, AllEnemies),
            Apply(WitchKeywords.Misfortune, Times(CombatAmountSpec.Binary("max", CombatAmountSpec.FromConst(0),
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(2),
                    new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec(AllEnemies)))), lone)));
}
