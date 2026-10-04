using RogueDeck.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's relics (hedge_witch_master.md §14; plan W5): three Common, five Uncommon, four Rare and six
// Shop, like the Bureaucrat's own. Offered to her alone (Eligibility.HedgeWitch, and her roster's Exclusive). Their
// in-combat rules are WitchRules statuses; their numbers are drafts. Black Spoon, which rewards a discovered Hidden
// Recipe, comes with the recipes themselves.
public static class WitchRelics
{
    public static IReadOnlyList<BnbRelic> All() =>
    [
        // ── Common ────────────────────────────────────────────────────────────────────────────────────────
        Normal("rowan_pin", "Rowan Pin", Rarity.Common,
            $"The first time each turn a card goes into the cauldron, gain {WitchRules.RowanPinBlock} Block.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.RowanPin)),
        Normal("three_knot_cord", "Three-Knot Cord", Rarity.Common,
            $"The first time each combat a Hex bursts, that enemy gains {WitchRules.ThreeKnotHexed} Hexed.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.ThreeKnotCord)),
        Normal("found_button", "Found Button", Rarity.Common,
            $"The first time each combat a Misfortune roll fails, gain {WitchRules.FoundButtonBlock} Block.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.FoundButton)),

        // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────
        Normal("greasy_ladle", "Greasy Ladle", Rarity.Uncommon,
            "Your first Brew each turn draws 1 card.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.GreasyLadle)),
        Normal("iron_trivet", "Iron Trivet", Rarity.Uncommon,
            $"If you end your turn with the cauldron Ready, gain {WitchRules.IronTrivetBlock} Block.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.IronTrivet)),
        Normal("crows_toe", "Crow's Toe", Rarity.Uncommon,
            $"The first time each turn you hit an enemy whose Hex bursts at the end of its next turn, deal " +
            $"{WitchRules.CrowsToeDamage} more damage.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.CrowsToe)),
        Normal("beeswax_cup", "Beeswax Cup", Rarity.Uncommon,
            $"The first time each turn you are healed, gain {WitchRules.BeeswaxWax} Ward Wax.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.BeeswaxCup)),
        Normal("knucklebone_pair", "Knucklebone Pair", Rarity.Uncommon,
            $"The first time each turn a Misfortune roll fails, {WitchRules.KnucklebonePairStacks} Misfortune stays on " +
            "that enemy.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.KnucklebonePair)),

        // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────
        Normal("false_bottom_pot", "False-Bottom Pot", Rarity.Rare,
            "The cauldron takes a fourth card in reserve. It is not part of the Brew, and stays for the next one.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.FalseBottomPot)),
        Normal("first_bell", "First Bell", Rarity.Rare,
            "The first enemy you Hex each combat starts its Threefold count a step ahead.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.FirstBell)),
        Normal("black_cats_collar", "Black Cat's Collar", Rarity.Rare,
            "The first Misfortune roll that fails each combat is rolled again.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.BlackCatsCollar)),
        Normal("grandams_ember", "Grandam's Ember", Rarity.Rare,
            "Once each combat, Hearth healing may restore HP you lost before the fight.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.GrandamsEmber)),

        // ── Shop ──────────────────────────────────────────────────────────────────────────────────────────
        Shop("bone_strainer", "Bone Strainer",
            "Dregs in a mixed Brew count as a family you name. They never make a concentrated one.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.BoneStrainer)),
        Shop("apothecarys_scale", "Apothecary's Scale",
            "Your first Brew each combat of three different families costs no Energy.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.ApothecarysScale)),
        Shop("yesterdays_jar", "Yesterday's Jar",
            "After your first Brew each combat, the last ingredient stays in the cauldron.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.YesterdaysJar)),
        Shop("cats_eye_coin", "Cat's-Eye Coin",
            $"After winning a combat, gain {WitchRules.CatsEyeGold} Gold for each Misfortune that landed in it, at " +
            $"most {WitchRules.CatsEyeCap}.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.CatsEyeCoin),
            runPrograms:
            [
                RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
                    RunEffectTemplates.GainResource(StandardRunIds.Gold,
                        RunExpr.Min(
                            RunExpr.Multiply(RunEventValues.CombatCounter(WitchRules.CatsEyeTally.ToString()),
                                RunExpr.Const(WitchRules.CatsEyeGold)),
                            RunExpr.Const(WitchRules.CatsEyeCap)))),
            ]),
        // ADAPTATION: the canon rewards an ALREADY-DISCOVERED recipe; discovery is the player's knowledge, kept in
        // the frontend's profile and never in the run, so the spoon rewards the first Hidden Recipe brewed each
        // combat — which reveals nothing either: the pot only knows a recipe once its three cards are in it.
        Shop("black_spoon", "Black Spoon",
            "The first time each combat you brew a Hidden Recipe, gain 1 Energy and draw 1 card.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.BlackSpoon)),
        Shop("herb_wifes_rack", "Herb-Wife's Rack",
            $"Your first Brew each combat with a Hearth ingredient also heals {WitchRules.HerbWifeHeal} HP lost this combat.",
            eligibility: Eligibility.HedgeWitch, combatRule: Rule(WitchRules.HerbWifesRack)),
    ];

    private static RogueDeck.Scenario.Authoring.StatusData Rule(string id) =>
        WitchRules.All().Single(s => s.Id == id);
}
