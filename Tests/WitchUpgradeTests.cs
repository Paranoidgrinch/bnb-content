using System.Text.RegularExpressions;
using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Witch's upgrades by the Bureaucrat's rules (HEDGE_WITCH_PLAN.md, Vorlage W8; user 2026-10-04): about half
// as much again in all, and a good share of them a new FACET instead of a bigger number — among them her own,
// the PINCH: an upgraded card that, in the cauldron, also gives one ingredient's worth of another family.
public partial class WitchUpgradeTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private static readonly CardDefinitionId Add = new(WitchActions.AddIngredient);
    private static readonly CardDefinitionId Brew = new(WitchActions.Brew);

    private static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;

    // Cooks exactly these three cards and brews them; what the hero and the enemy stood at afterwards.
    private static (int Block, int EnemyHealthLost, int WardWax) Brewed(params string[] three)
    {
        var (play, session, enemy) = FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 9),
            [.. three, "nettle_tea", "nettle_tea"], character: WitchCharacter.Id);
        foreach (var card in three)
        {
            play.CombatDriver!.UseAction(Add, enemy);
            var offered = play.CombatDriver.PendingCardChoice!;
            play.CombatDriver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == card).Id]);
            Assert.Null(session.Error);
        }
        var before = Now(play).State.GetCombatant(enemy).Health.Current;
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        var hero = Now(play).State.GetCombatant(Now(play).HeroId);
        var result = (
            hero.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0,
            before - Now(play).State.GetCombatant(enemy).Health.Current,
            FightProbe.StacksOf(hero, Converter.Cards.Keywords.WardWax));
        play.Dispose();
        return result;
    }

    // A pinch of Husk on Spilled Salt+: the same mixed brew gives 4 Block more than with the plain card.
    [Fact]
    public void A_pinch_adds_one_ingredient_of_its_family_to_whatever_is_brewed()
    {
        Assert.Equal(0, Brewed("adders_nip", "adders_nip", "spilled_salt").Block);
        Assert.Equal(4, Brewed("adders_nip", "adders_nip", "spilled_salt+").Block);
    }

    // …and it is NOT an ingredient: Thorn Hedge+ carries a pinch of Fang, and two Fangs beside it still brew a
    // mixed pot (2 × 5 damage, and 5 for the pinch) — not Red Teeth's 18.
    [Fact]
    public void A_pinch_never_makes_a_brew_concentrated()
    {
        Assert.Equal(10, Brewed("adders_nip", "adders_nip", "thorn_hedge").EnemyHealthLost);
        Assert.Equal(15, Brewed("adders_nip", "adders_nip", "thorn_hedge+").EnemyHealthLost);
    }

    [Fact]
    public void Horn_spoon_plus_makes_the_brew_give_five_ward_wax()
    {
        Assert.Equal(3, Brewed("adders_nip", "adders_nip", "horn_spoon").WardWax);
        Assert.Equal(5, Brewed("adders_nip", "adders_nip", "horn_spoon+").WardWax);
    }

    // The rules themselves, read off the cards: every Rite's upgrade costs one less (as the Bureaucrat's), and
    // at least 40 % of the upgrades change more than a number (R1).
    [Fact]
    public void Rites_upgrade_by_costing_one_less_and_two_upgrades_in_five_bring_a_facet()
    {
        var cards = WitchCards.All().Where(c => !c.Id.EndsWith('+') && c.Rarity is not "junk").ToList();
        var plus = WitchCards.All().Where(c => c.Id.EndsWith('+')).ToDictionary(c => c.Id.TrimEnd('+'));

        foreach (var rite in cards.Where(c => c.Type == Converter.Cards.CardAuthoring.RiteTag))
            Assert.True(plus[rite.Id].Cost == Math.Max(0, rite.Cost - 1), $"{rite.Id}+ costs {plus[rite.Id].Cost}");

        static string Shape(string text) => Regex.Replace(text, @"\d+", "#");
        var faceted = cards.Count(c => Shape(c.RulesText) != Shape(plus[c.Id].RulesText) || c.Cost != plus[c.Id].Cost
            || c.RetainInHand != plus[c.Id].RetainInHand);
        Assert.True(faceted * 100 >= cards.Count * 40, $"{faceted} of {cards.Count} upgrades bring a facet");
    }
}
