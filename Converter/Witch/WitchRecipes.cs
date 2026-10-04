namespace BnbContent.Converter.Witch;

// The names and the plain-words effect of every family brew (hedge_witch_master.md §7, §8): what the cauldron
// says it is about to make before the Witch brews it, and what the Recipe Book's "Everyday Brewing" lists.
// Written onto the BREW action's presentation (Extra["recipe:<family>+<family>+<family>"] = "Name|effect"), so a
// frontend only reads; the rule itself is WitchActions.Brew. Hidden Recipes are not here — they are not
// previewed before they are found (§9).
public static class WitchRecipes
{
    private static readonly Dictionary<string, string> Short = new()
    {
        [WitchActions.Fang] = "Fang", [WitchActions.Hex] = "Hex", [WitchActions.Husk] = "Husk",
        [WitchActions.Hearth] = "Hearth", [WitchActions.Fortune] = "Fortune",
    };

    // §7 and §8, by the families in canonical order (Fang, Hex, Husk, Hearth, Fortune).
    private static readonly Dictionary<string, string> Names = new()
    {
        ["Fang+Fang+Fang"] = "Red Teeth",
        ["Hex+Hex+Hex"] = "Third Night",
        ["Husk+Husk+Husk"] = "Shell-Wax",
        ["Hearth+Hearth+Hearth"] = "Hearth Physic",
        ["Fortune+Fortune+Fortune"] = "Black Cat's Luck",

        ["Fang+Fang+Hex"] = "Snake-Curse",
        ["Fang+Fang+Husk"] = "Thornhide",
        ["Fang+Fang+Hearth"] = "Hunter's Stew",
        ["Fang+Fang+Fortune"] = "Hare's Luck",
        ["Fang+Hex+Hex"] = "Witch-Bite",
        ["Hex+Hex+Husk"] = "Black Bark",
        ["Hex+Hex+Hearth"] = "Bitter Charm",
        ["Hex+Hex+Fortune"] = "Crooked Moon",
        ["Fang+Husk+Husk"] = "Hedgehog Broth",
        ["Hex+Husk+Husk"] = "Bark and Bane",
        ["Husk+Husk+Hearth"] = "Bone Broth",
        ["Husk+Husk+Fortune"] = "Lucky Shell",
        ["Fang+Hearth+Hearth"] = "Red Broth",
        ["Hex+Hearth+Hearth"] = "Fever Tea",
        ["Husk+Hearth+Hearth"] = "Thick Stew",
        ["Hearth+Hearth+Fortune"] = "Lucky Supper",
        ["Fang+Fortune+Fortune"] = "Cat's Claw",
        ["Hex+Fortune+Fortune"] = "Ill Star",
        ["Husk+Fortune+Fortune"] = "Charm Against Harm",
        ["Hearth+Fortune+Fortune"] = "Lucky Tea",

        ["Fang+Hex+Husk"] = "Briar Hex",
        ["Fang+Hex+Hearth"] = "Hedge Broth",
        ["Fang+Hex+Fortune"] = "Black Adder",
        ["Fang+Husk+Hearth"] = "Hunter's Pot",
        ["Fang+Husk+Fortune"] = "Fox's Chance",
        ["Fang+Hearth+Fortune"] = "Lucky Hunt",
        ["Hex+Husk+Hearth"] = "Countercharm",
        ["Hex+Husk+Fortune"] = "Crossed Charm",
        ["Hex+Hearth+Fortune"] = "Fever Dream",
        ["Husk+Hearth+Fortune"] = "House Blessing",
    };

    // Every family brew: key = the three family tags in canonical order joined by '+', value = "Name|effect".
    public static IReadOnlyDictionary<string, string> Presentation()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var families = WitchActions.Families;
        for (var a = 0; a < families.Length; a++)
            for (var b = a; b < families.Length; b++)
                for (var c = b; c < families.Length; c++)
                {
                    string[] mix = [families[a], families[b], families[c]];
                    var name = Names[string.Join("+", mix.Select(f => Short[f]))];
                    result[$"recipe:{string.Join("+", mix)}"] = $"{name}|{Effect(mix)}";
                }
        // The Hidden Recipes, for the Recipe Book (never for the preview — an undiscovered one is not shown):
        // "hidden:<n>" = "Name|ingredient ids, '+'-joined|effect". A Dregs slot is written "dregs".
        foreach (var recipe in WitchHiddenRecipes.All)
        {
            result[$"hidden:{recipe.Number}"] = $"{recipe.Name}|{string.Join("+", recipe.Ingredients)}|{recipe.Text}";
            result[$"clue:{recipe.Number}"] = WitchHiddenRecipes.Clues[recipe.Number];
        }
        return result;
    }

    // In words, exactly what WitchActions.Brew will do with this mix.
    public static string Effect(IReadOnlyList<string> mix)
    {
        var count = WitchActions.Families.ToDictionary(f => f, f => mix.Count(m => m == f));
        if (count[WitchActions.Fang] == 3) return "Deal 18 damage.";
        if (count[WitchActions.Hex] == 3) return "Apply 7 Hexed.";
        if (count[WitchActions.Husk] == 3) return "Gain 8 Ward Wax.";
        if (count[WitchActions.Hearth] == 3) return "Heal 4 HP lost this combat.";
        if (count[WitchActions.Fortune] == 3) return "Apply 50% Misfortune.";
        var parts = new List<string>();
        if (count[WitchActions.Hex] > 0) parts.Add($"apply {2 * count[WitchActions.Hex]} Hexed");
        if (count[WitchActions.Fang] > 0) parts.Add($"deal {5 * count[WitchActions.Fang]} damage");
        if (count[WitchActions.Husk] > 0) parts.Add($"gain {4 * count[WitchActions.Husk]} Block");
        if (count[WitchActions.Hearth] > 0) parts.Add($"heal {count[WitchActions.Hearth]} HP lost this combat");
        if (count[WitchActions.Fortune] > 0) parts.Add($"apply {15 * count[WitchActions.Fortune]}% Misfortune");
        var text = string.Join(", ", parts);
        return text.Length == 0 ? "Nothing — only dregs." : char.ToUpperInvariant(text[0]) + text[1..] + ".";
    }
}
