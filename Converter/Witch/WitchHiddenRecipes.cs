using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// THE HIDDEN RECIPES (hedge_witch_master.md §9): three exact cards — whatever their upgrade, in any order — make a
// brew of their own that REPLACES the ordinary one. The pot knows a card by its own ingredient tag (WitchCards.
// Ingredient), so "Adder's Nip + Nettle Tea + Mugwort Poultice" is three counts. Which recipe was brewed is written
// on her (`hidden_recipe_brewed`, its number) for the frontend's Recipe Book: discovery is the player's knowledge,
// kept in the profile, never in the run. Numbers are drafts (§23): a recipe is about what its three cards and a
// Brew would be, bent towards its theme.
public static class WitchHiddenRecipes
{
    public static CounterId Brewed => new("hidden_recipe_brewed");

    public const string Dregs = "dregs";

    public sealed record HiddenRecipe(int Number, string Name, IReadOnlyList<string> Ingredients, string Text,
        Func<IEffectNode<CardPlayContext>> Effect);

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;
    private static ICombatantTargetSelector Target => CombatantTargetSelectors.EventTarget;
    private static ICombatExpression<CardPlayContext, int> Const(int value) => new ConstantExpression<CardPlayContext>(value);

    public static IReadOnlyList<HiddenRecipe> All { get; } =
    [
        new(1, "Snakebite Remedy", ["adders_nip", "nettle_tea", "mugwort_poultice"],
            "Heal 6 HP lost this combat, remove 2 stacks of negative Statuses from yourself and gain 8 Block.",
            () => Seq(Heal(6), Cleanse(2), Block(8))),
        new(2, "Everpot", ["taste_the_broth", "put_the_kettle_on", "never_wash_the_pot"],
            "Gain 8 Block. Keep one ingredient in the cauldron. Your next card into the cauldron is free.",
            () => Seq(Block(8), WitchActions.PickOneToKeep(), Status(WitchKeywords.FreeIngredient, 1, You))),
        new(3, "Thirteen Years", ["broken_mirror", "black_cat", "spilled_salt"],
            "Apply 13 Misfortune. If its roll fails, 6 Misfortune stays for its next action.",
            () => Seq(Status(WitchKeywords.Misfortune, 13, Target), Status(WitchRules.BadPenny, 6, Target))),
        new(4, "Knock Three Times", ["third_knock", "third_bell", "crooked_finger"],
            "Apply 9 Hexed. Its Threefold count moves on by 1.",
            () => Seq(Status(WitchKeywords.Hexed, 9, Target), Step(1))),
        new(5, "The Night Answers", ["third_knock", "thrice_spoken_name", "call_the_third_night"],
            "Apply 6 Hexed. Then its Hex bursts now, and its Threefold count starts again.",
            () => Seq(Status(WitchKeywords.Hexed, 6, Target), BurstNow())),
        new(6, "Grandam's Cure", ["honey_and_onion", "bitter_tea", "hot_broth"],
            "Heal 6 HP lost this combat, draw 2 cards and remove 2 stacks of negative Statuses from yourself.",
            () => Seq(Heal(6), new DrawCardsNode<CardPlayContext>(You, Const(2)), Cleanse(2))),
        new(7, "Bone-Mender", ["set_the_bone", "slough_off", "midwifes_hands"],
            "Heal 10 HP lost this combat, remove every negative Status from yourself and gain 5 Ward Wax.",
            () => Seq(Heal(10), new RemoveStatusesByPolarityNode<CardPlayContext>(You, StatusPolarity.Debuff),
                Status(Cards.Keywords.WardWax, 5, You))),
        new(8, "Biting Hedge", ["thorn_hedge", "hawthorn_wall", "hedge_thing"],
            "Gain 15 Block. Every enemy attack on you this turn costs the attacker 4 damage.",
            () => Seq(Block(15), Status(WitchRules.HedgehogCurl, 4, You))),
        new(9, "Lid Down", ["pot_lid", "clamp_the_lid", "shut_the_lid"],
            "Gain 12 Block. Until your next turn the cauldron shelters you however full it is, and nothing goes in or out.",
            () => Seq(Block(12), Status(WitchRules.ShutTheLid, 1, You))),
        new(10, "Adder's Kiss", ["adders_nip", "adder_in_the_sleeve", "two_teeth"],
            "Deal 5 damage three times, then 12.",
            () => Seq(Damage(5), Damage(5), Damage(5), Damage(12))),
        new(11, "Crow Breakfast", ["crows_peck", "murder_of_crows", "familiars_supper"],
            "Deal 3 damage to a random enemy 6 times. If an enemy falls, gain 1 Energy and your next card into the " +
            "cauldron is free.", CrowBreakfast),
        new(12, "Tell the Magpies", ["magpies_luck", "seven_magpies", "tell_the_bees"],
            "Apply 5 Misfortune to ALL enemies. Each whose action it makes fail lets you draw 1 more card next turn.",
            () => Seq(Status(WitchKeywords.Misfortune, 5, CombatantTargetSelectors.AllEnemiesOfSource),
                Status(WitchRules.MagpiesLuck, 1, CombatantTargetSelectors.AllEnemiesOfSource))),
        new(13, "Nine Lives", ["black_cat", "ninth_life", "the_black_cat_sat_down"],
            "The target's next action fails. After it does, the target is left 6 Misfortune.",
            () => Seq(Status(WitchRules.BlackCatSatDown, 1, Target), Status(WitchKeywords.Misfortune, 1, Target),
                Status(WitchRules.NineLivesLeft, 6, Target))),
        new(14, "Against the Evil Eye", ["evil_eye", "knock_on_wood", "horseshoe_over_the_door"],
            "Remove every negative Status from yourself. Apply 4 Misfortune, and 2 more for each stack removed.",
            EvilEye),
        new(15, "Sour Supper", ["sour_the_milk", "honey_and_onion", "proper_supper"],
            "Heal 5 HP lost this combat. Apply 8 Hexed.",
            () => Seq(Heal(5), Status(WitchKeywords.Hexed, 8, Target))),
        new(16, "Hedge Physic", ["nettle_tea", "comfrey_poultice", "honey_and_onion"],
            "Heal 12 HP lost this combat. Remove 3 stacks of negative Statuses from yourself.",
            () => Seq(Heal(12), Cleanse(3))),
        new(17, "Winter Coat", ["oakskin", "winter_bark", "birch_bark_wrap"],
            "Gain 20 Block. After the enemy turn, up to 10 of your unused Block becomes Ward Wax.",
            () => Seq(Block(20), Status(WitchRules.WinterBark, 10, You))),
        new(18, "Slow as Stone", ["snail_shell", "snails_patience", "horn_spoon"],
            "Gain 10 Block and 6 Ward Wax. Gain 8 Block next turn.",
            () => Seq(Block(10), Status(Cards.Keywords.WardWax, 6, You), Status(Cards.Keywords.PromisedBlock, 8, You))),
        new(19, "Grave Hex", ["old_grudge", "last_breath_hex", "bane_root"],
            "Apply 10 Hexed. When the target dies, the healthiest other enemy takes on all its Hexed and its Threefold count.",
            () => Seq(Status(WitchKeywords.Hexed, 10, Target), Status(WitchRules.LastBreathHex, 1, Target))),
        new(20, "Beggar's Pot", ["stone_soup", Dregs, Dregs],
            "Gain 10 Block. Heal 4 HP lost this combat. (The Dregs go back to your discard pile as ever.)",
            () => Seq(Block(10), Heal(4))),
    ];

    // NOTES IN THE MARGIN (§17): a clue for each recipe not yet found — in the canon's style, a riddle the names and
    // the folklore answer, never the list of three cards.
    public static IReadOnlyDictionary<int, string> Clues { get; } = new Dictionary<int, string>
    {
        [1] = "For a bite, something that stings and something that soothes.",
        [2] = "A pot that is never washed is never quite empty. Taste it; put the kettle on.",
        [3] = "A broken glass, a dark cat crossing, salt on the floor: count the years.",
        [4] = "Knock, and ring, and point.",
        [5] = "Knock at the door, say the name three times, and the night will answer.",
        [6] = "Honey, something bitter and something hot: what grandmother gave for everything.",
        [7] = "Set it, shed the old skin, and let careful hands do the rest.",
        [8] = "Every hedge bites back if it is thorny enough — and something lives in it.",
        [9] = "Down it goes, and clamped, and shut. Nothing more goes in.",
        [10] = "One bites, one waits in the sleeve, and teeth come in pairs.",
        [11] = "The crows eat early, and they eat together.",
        [12] = "One for sorrow, seven for a secret — and the bees must always be told.",
        [13] = "A black cat has nine of them, and somewhere it sits down.",
        [14] = "Against the eye: knock on wood and hang iron over the door.",
        [15] = "Soured milk, a sweet-sharp remedy and a proper meal.",
        [16] = "The hedge's own physic: a stinging leaf, a knitting root and honey.",
        [17] = "Oak, winter bark and birch: a coat for the cold.",
        [18] = "A shell, a slow patience, and a spoon cut from horn.",
        [19] = "An old grudge, a last breath, and a root that grows back from the grave.",
        [20] = "Soup from a stone and whatever was swept off the floor.",
    };

    // The pot holds this recipe's three cards (the reserve aside): each named card as often as it is named; Dregs
    // are anything without a family.
    public static ICombatExpression<CardPlayContext, bool> Holds(
        HiddenRecipe recipe, Func<string?, ICombatExpression<CardPlayContext, int>> inRecipe,
        ICombatExpression<CardPlayContext, int> dregs) =>
        recipe.Ingredients.GroupBy(i => i)
            .Select(g => (ICombatExpression<CardPlayContext, bool>)new ComparisonExpression<CardPlayContext>(
                g.Key == Dregs ? dregs : inRecipe(WitchCards.Ingredient(g.Key)),
                ComparisonOperator.GreaterOrEqual, Const(g.Count())))
            .Aggregate((a, b) => new AndExpression<CardPlayContext>(a, b));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<CardPlayContext> Seq(params IEffectNode<CardPlayContext>[] steps) =>
        new CausalSequenceEffectNode<CardPlayContext>(steps);

    private static IEffectNode<CardPlayContext> Heal(int amount) => WitchRules.Heal(You, Const(amount));

    private static IEffectNode<CardPlayContext> Block(int amount) => new GainBlockNode<CardPlayContext>(You, Const(amount));

    private static IEffectNode<CardPlayContext> Damage(int amount) => new DealDamageNode<CardPlayContext>(Target, Const(amount));

    private static IEffectNode<CardPlayContext> Status(string status, int stacks, ICombatantTargetSelector to) =>
        new ApplyStatusNode<CardPlayContext>(to, new StatusDefinitionId(status), Const(stacks));

    private static IEffectNode<CardPlayContext> Cleanse(int stacks) =>
        CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("modifySelectedStatusStacks", "source",
            CombatAmountSpec.FromConst(-stacks), Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff))).Root;

    private static IEffectNode<CardPlayContext> Step(int by) =>
        new SetCombatantCounterNode<CardPlayContext>(Target, WitchKeywords.ThreefoldStep, Const(by), relative: true);

    // The third night, called (as Call the Third Night calls it).
    private static IEffectNode<CardPlayContext> BurstNow() => Seq(
        new DealDamageNode<CardPlayContext>(Target,
            new MultiplyExpression<CardPlayContext>(
                new CombatantStatusStacksExpression<CardPlayContext>(Target, new StatusDefinitionId(WitchKeywords.Hexed)),
                Const(WitchKeywords.ThreefoldMultiplier)),
            ignoresBlock: true, kind: DamageKind.DamageOverTime),
        new SetCombatantCounterNode<CardPlayContext>(Target, WitchKeywords.ThreefoldStep, Const(0), relative: false));

    private static CounterId Standing => new("crow_breakfast_standing");

    private static IEffectNode<CardPlayContext> CrowBreakfast()
    {
        var standing = new CountTargetsExpression<CardPlayContext>(CombatantTargetSelectors.AllEnemiesOfSource);
        var fell = new ComparisonExpression<CardPlayContext>(
            new CombatantCounterExpression<CardPlayContext>(You, Standing), ComparisonOperator.Greater, standing);
        return Seq(
            new SetCombatantCounterNode<CardPlayContext>(You, Standing, standing, relative: false),
            CombatProgramModel.Build<CardPlayContext>(CombatNodeModel.RandomTargets("allEnemies",
                CombatAmountSpec.FromConst(6),
                new CombatNodeModel("dealDamage", "iterationTarget", CombatAmountSpec.FromConst(3)))).Root,
            new ConditionalEffectNode<CardPlayContext>(fell, Seq(
                new GainResourceNode<CardPlayContext>(You, StandardCombatIds.EnergyResource, Const(1)),
                Status(WitchKeywords.FreeIngredient, 1, You))));
    }

    private static CounterId Removed => new("against_the_evil_eye_removed");

    private static IEffectNode<CardPlayContext> EvilEye() => Seq(
        new SetCombatantCounterNode<CardPlayContext>(You, Removed,
            new CombatantStacksByPolarityExpression<CardPlayContext>(You, StatusPolarity.Debuff), relative: false),
        new RemoveStatusesByPolarityNode<CardPlayContext>(You, StatusPolarity.Debuff),
        new ApplyStatusNode<CardPlayContext>(Target, new StatusDefinitionId(WitchKeywords.Misfortune),
            new AddExpression<CardPlayContext>(Const(4),
                new MultiplyExpression<CardPlayContext>(new CombatantCounterExpression<CardPlayContext>(You, Removed), Const(2)))));
}
