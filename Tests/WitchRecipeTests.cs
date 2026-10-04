using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Hidden Recipes (hedge_witch_master.md §9): three exact cards, any order, any upgrade, REPLACE the ordinary
// brew — and the brew says which it was, for the Recipe Book.
public class WitchRecipeTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string Biter = "embossed_seal";
    private const string BiteIntent = "heavy_impression";

    private static readonly CardDefinitionId Add = new(WitchActions.AddIngredient);
    private static readonly CardDefinitionId Brew = new(WitchActions.Brew);

    private static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;
    private static CombatantState Hero(RunPlayback play) => Now(play).State.GetCombatant(Now(play).HeroId);
    private static CombatantState Body(RunPlayback play, CombatantId id) => Now(play).State.GetCombatant(id);
    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static string Card(string ingredient) =>
        ingredient == WitchHiddenRecipes.Dregs ? WitchCards.MirrorShardId : ingredient;

    // Into the pot, these three, and brew: what the pot was, and the fight after.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Brewed(
        IReadOnlyList<string> ingredients, string enemy = Quiet, string intent = QuietIntent, string[]? relics = null)
    {
        var deck = ingredients.Select(Card).Concat(["pot_lid", "pot_lid"]).ToArray();
        var (play, session, target) = FightProbe.Start(FightProbe.Roster("recipe", 9, (enemy, intent, 90)), deck,
            character: WitchCharacter.Id, relics: relics);
        var driver = play.CombatDriver!;
        foreach (var card in ingredients.Select(Card))
        {
            driver.UseAction(Add, null);
            Assert.Null(session.Error);
            var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
            driver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == card && !PotOf(play).Contains(c)).Id]);
            Assert.Null(session.Error);
        }
        driver.UseAction(Brew, target);
        if (driver.PendingCardChoice is IReadOnlyList<CardInstance> keep)   // Everpot: keep one
            driver.SupplyCardChoice([keep[0].Id]);
        Assert.Null(session.Error);
        return (play, session, target);
    }

    private static IReadOnlyList<CardInstance> PotOf(RunPlayback play) =>
        Now(play).State.GetCardZones(Now(play).HeroId).SetAside;

    public static TheoryData<int> Numbers() => [.. WitchHiddenRecipes.All.Select(r => r.Number)];

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Each_hidden_recipe_is_found_by_its_three_cards_and_says_which_it_was(int number)
    {
        var recipe = WitchHiddenRecipes.All.Single(r => r.Number == number);
        // In reverse order, to show the order does not matter.
        var (play, _, _) = Brewed([.. recipe.Ingredients.Reverse()]);
        Assert.Equal(number, Hero(play).GetCounter(WitchHiddenRecipes.Brewed));
        play.Dispose();
    }

    [Fact]
    public void Twenty_recipes_each_named_on_the_brew_for_the_recipe_book()
    {
        Assert.Equal(Enumerable.Range(1, 20), WitchHiddenRecipes.All.Select(r => r.Number));
        var said = FightProbe.Game.Presentation.Cards[WitchActions.Brew].Extra;
        Assert.All(WitchHiddenRecipes.All, r => Assert.StartsWith(r.Name + "|", said[$"hidden:{r.Number}"]));
        // A clue for each, and no clue names any of its own cards outright (§17: never the combination).
        var names = WitchCards.All().ToDictionary(c => c.Id, c => c.Name);
        Assert.All(WitchHiddenRecipes.All, r =>
        {
            var clue = said[$"clue:{r.Number}"];
            Assert.All(r.Ingredients.Where(names.ContainsKey), id => Assert.DoesNotContain(names[id], clue));
        });
        // Every named card exists as a card of hers.
        var hers = WitchCards.All().Select(c => c.Id).ToHashSet();
        Assert.All(WitchHiddenRecipes.All.SelectMany(r => r.Ingredients).Where(i => i != WitchHiddenRecipes.Dregs),
            id => Assert.Contains(id, hers));
    }

    [Fact]
    public void A_hidden_recipe_replaces_the_ordinary_brew()
    {
        // Adder's Kiss is three Fang cards: Red Teeth would be 18; the kiss is 5 + 5 + 5 + 12.
        var (play, _, enemy) = Brewed(["adders_nip", "adder_in_the_sleeve", "two_teeth"]);
        Assert.Equal(90 - 27, Body(play, enemy).Health.Current);
        play.Dispose();

        // An upgraded card is still that card.
        (play, _, enemy) = Brewed(["adders_nip+", "adder_in_the_sleeve", "two_teeth"]);
        Assert.Equal(10, Hero(play).GetCounter(WitchHiddenRecipes.Brewed));
        play.Dispose();
    }

    [Fact]
    public void Nine_lives_spends_the_action_and_leaves_misfortune_behind()
    {
        var (play, session, enemy) = Brewed(["black_cat", "ninth_life", "the_black_cat_sat_down"], Biter, BiteIntent);
        var health = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(health, Hero(play).Health.Current);
        Assert.Equal(6, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.Dispose();
    }

    [Fact]
    public void Black_spoon_pays_for_the_first_hidden_recipe_of_the_fight()
    {
        var (play, _, _) = Brewed(["adders_nip", "nettle_tea", "mugwort_poultice"], relics: ["black_spoon"]);
        Assert.Equal(1, Hero(play).GetCounter(WitchRules.BlackSpoonLatch));
        Assert.Equal(8, Block(Hero(play)));   // Snakebite Remedy's Block
        play.Dispose();
    }
}
