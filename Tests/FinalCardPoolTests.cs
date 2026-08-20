using BnbContent.Converter.Cards;

namespace BnbContent.Tests;

// The pool as the design sheets count it. A card quietly missing, or authored at the wrong rarity or Act
// gate, changes what a reward can offer without breaking anything — so the counts are pinned here.
public class FinalCardPoolTests
{
    // bureaucrat_final_cards.md §1: Act I unlocks 15 Commons, 20 Uncommons and 11 Rares.
    [Theory]
    [InlineData("common", 15)]
    [InlineData("uncommon", 20)]
    [InlineData("rare", 11)]
    public void Act_one_offers_the_bureaucrat_cards_the_sheet_counts(string rarity, int expected)
    {
        var pool = FinalCards.RewardPool(act: 1).Where(c => c.Rarity == rarity).ToList();
        Assert.Equal(expected, pool.Count);
    }

    // "The Bureaucrat has 80 regular reward cards, each with a direct upgraded version." Starters and Junk
    // are separate from the reward pool and are never offered.
    [Fact]
    public void Every_reward_card_has_an_upgrade_and_no_starter_or_junk_is_offered()
    {
        var all = FinalCards.All().ToDictionary(c => c.Id);

        foreach (var card in FinalCards.RewardPool(act: 4))
        {
            Assert.True(all.ContainsKey(card.Id + "+"), $"'{card.Id}' has no upgraded version");
            Assert.NotEqual("starter", card.Rarity);
            Assert.NotEqual("junk", card.Rarity);
        }
    }

    // Ids are what everything else references — a duplicate would silently shadow a card.
    [Fact]
    public void No_two_cards_share_an_id()
    {
        var ids = FinalCards.All().Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // Every card carries exactly one PRIMARY type, because that is what the rules read it through: Ratified
    // adds its damage to Deeds, relics count Rites, and enemy passives sequence on the type.
    [Fact]
    public void Every_card_carries_exactly_one_primary_type()
    {
        string[] primary = [CardAuthoring.DeedTag, CardAuthoring.WorkingTag, CardAuthoring.RiteTag, CardAuthoring.JunkTag];

        foreach (var card in FinalCards.All())
            Assert.Equal(1, card.AllTags.Count(primary.Contains));
    }

    // A card's rules text is what both UIs show on a reward and in the hand; the engine renders none of it.
    [Fact]
    public void Every_card_says_what_it_does()
    {
        foreach (var card in FinalCards.All())
            Assert.False(string.IsNullOrWhiteSpace(card.Text), $"'{card.Id}' has no rules text");
    }

    // Every Rite installs a status that actually exists — a Rite whose status was never registered would
    // simply do nothing, and the fight would not complain.
    [Fact]
    public void Every_rite_installs_a_status_that_exists()
    {
        var statuses = FinalCards.Statuses().Select(s => s.Id).ToHashSet();

        foreach (var rite in FinalCards.All().Where(c => c.AllTags.Contains(CardAuthoring.RiteTag)))
        {
            var installs = CardIds.StatusesAppliedBy(rite);
            Assert.True(installs.Count > 0, $"Rite '{rite.Id}' installs nothing");
            foreach (var id in installs)
                Assert.True(statuses.Contains(id), $"Rite '{rite.Id}' installs unknown status '{id}'");
        }
    }
}

// Reads the statuses a card's authored program applies, so a test can check what a Rite actually installs.
internal static class CardIds
{
    public static IReadOnlyList<string> StatusesAppliedBy(CardAuthoring.BnbCard card)
    {
        var found = new List<string>();
        Walk(card.Program, found);
        return found;
    }

    private static void Walk(RogueDeck.Scenario.Authoring.CombatNodeModel node, List<string> found)
    {
        if (node.Kind == "applyStatus" && !string.IsNullOrEmpty(node.StatusId))
            found.Add(node.StatusId);
        foreach (var child in node.ChildrenOrEmpty)
            Walk(child, found);
    }
}
