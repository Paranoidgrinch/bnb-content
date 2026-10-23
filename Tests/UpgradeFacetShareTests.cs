using System.Text.RegularExpressions;
using BnbContent.Converter.Cards;

namespace BnbContent.Tests;

// The upgrade rule R1/U2 (bnb-godot PLAYTEST_FEEDBACK_2_PLAN F, LIEUTENANT_PLAN §1), read off the cards: at least
// 40 % of a character's upgrades change more than a number — a new clause, another cost, or Retain.
public class UpgradeFacetShareTests
{
    private static (int Faceted, int All) Share(IReadOnlyList<CardAuthoring.BnbCard> pool)
    {
        var cards = pool.Where(c => !c.Id.EndsWith('+') && c.Rarity is not "junk").ToList();
        var plus = pool.Where(c => c.Id.EndsWith('+')).ToDictionary(c => c.Id.TrimEnd('+'));
        static string Shape(string text) => Regex.Replace(text, @"\d+", "#");
        var faceted = cards.Count(c => Shape(c.RulesText) != Shape(plus[c.Id].RulesText) || c.Cost != plus[c.Id].Cost
            || c.RetainInHand != plus[c.Id].RetainInHand);
        return (faceted, cards.Count);
    }

    [Fact]
    public void The_apprentices_upgrades_bring_a_facet_two_in_five_times()
    {
        var (faceted, all) = Share(Converter.Apprentice.ApprenticeCards.All());
        Assert.True(faceted * 100 >= all * 40, $"{faceted} of {all} upgrades bring a facet");
    }

    [Fact]
    public void The_lieutenants_upgrades_bring_a_facet_two_in_five_times()
    {
        var (faceted, all) = Share(Converter.Lieutenant.LieutenantCards.All());
        Assert.True(faceted * 100 >= all * 40, $"{faceted} of {all} upgrades bring a facet");
    }

    // U5 / O9: a Lieutenant Rite's upgrade is a facet, never only "costs 1 less".
    [Fact]
    public void No_lieutenant_rite_upgrade_only_costs_less()
    {
        var cards = Converter.Lieutenant.LieutenantCards.All();
        foreach (var rite in cards.Where(c => c.Type == CardAuthoring.RiteTag && !c.Id.EndsWith('+')))
            Assert.Equal(rite.Cost, cards.Single(c => c.Id == rite.Id + "+").Cost);
    }
}
