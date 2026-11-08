using BnbContent.Converter.Cards;

namespace BnbContent.Tests;

// The pool as the design sheets count it. A card quietly missing, or authored at the wrong rarity or Act
// gate, changes what a reward can offer without breaking anything — so the counts are pinned here.
public class FinalCardPoolTests
{
    // bureaucrat_final_cards.md §1: Act I unlocks 15 Commons, 20 Uncommons and 11 Rares for the Bureaucrat.
    [Theory]
    [InlineData(1, "common", 15)]
    [InlineData(1, "uncommon", 20)]
    [InlineData(1, "rare", 11)]
    [InlineData(2, "common", 18)]
    [InlineData(2, "uncommon", 27)]
    [InlineData(2, "rare", 15)]
    [InlineData(3, "common", 19)]
    [InlineData(3, "uncommon", 32)]
    [InlineData(3, "rare", 21)]
    [InlineData(4, "common", 20)]
    [InlineData(4, "uncommon", 35)]
    [InlineData(4, "rare", 25)]
    public void Each_act_offers_the_bureaucrat_cards_the_sheets_count(int act, string rarity, int expected)
    {
        var pool = FinalCards.CharacterPool(act).Where(c => c.Rarity == rarity).ToList();
        Assert.Equal(expected, pool.Count);
    }

    // GENERAL_POOL_PLAN.md (2026-11-08): 26 Commons, all from Act I; the Uncommons about two thirds from Act I
    // and the rest from Act II; the Rares staggered over Acts II to IV.
    [Theory]
    [InlineData(1, "common", 26)]
    [InlineData(1, "uncommon", 19)]
    [InlineData(1, "rare", 0)]
    [InlineData(2, "uncommon", 29)]
    [InlineData(2, "rare", 6)]
    [InlineData(3, "rare", 12)]
    [InlineData(4, "common", 26)]
    [InlineData(4, "uncommon", 29)]
    [InlineData(4, "rare", 15)]
    public void Each_act_offers_the_general_cards_the_plan_counts(int act, string rarity, int expected)
    {
        var pool = FinalCards.GeneralPool(act).Where(c => c.Rarity == rarity).ToList();
        Assert.Equal(expected, pool.Count);
    }

    // bureaucrat_final_cards.md: 80 regular reward cards. GENERAL_POOL_PLAN.md: 70.
    [Fact]
    public void Both_pools_are_the_size_the_sheets_state()
    {
        var offerable = FinalCards.RewardPool(act: 4);
        Assert.Equal(80 + 70, offerable.Count);
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

    // The Bureaucrat's Act-III additions, card for card. The counts above say how many; this says WHICH — the
    // sheets' own Act-III themes (custom, testimony, hospitality, restitution, grievance).
    [Fact]
    public void Act_three_adds_exactly_the_cards_the_sheets_name()
    {
        var added = FinalCards.CharacterPool(act: 3)
            .Except(FinalCards.CharacterPool(act: 2))
            .Select(c => c.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        string[] expected =
        [
            "Blood Testimony", "Customary Due", "Due Recompense", "Grievance Ledger", "Guest Right",
            "Guestbook Oath", "Host's Compact", "Hedge Covenant", "Hedge Hospitality", "Priority Docket",
            "Restitution Writ", "Witness Knot",
        ];

        Assert.Equal(expected.OrderBy(n => n, StringComparer.Ordinal).ToList(), added);
    }

    // The Act-IV additions, card for card and rarity for rarity — the last tier the deck gets before the
    // boss gauntlet, so what it offers is what a run has to answer Act V with. The sheet's row is the
    // Bureaucrat's 1/3/4.
    [Fact]
    public void Act_four_adds_exactly_the_cards_the_sheets_name()
    {
        var added = FinalCards.CharacterPool(act: 4)
            .Except(FinalCards.CharacterPool(act: 3))
            .ToList();

        string[] expected =
        [
            "Cartouche Reckoning", "Final Attestation", "Fivefold Compliance", "Hieratic Measure",
            "Monumental Writ", "Processional Calendar", "Stone Levy", "Temple Tally",
        ];

        Assert.Equal(
            expected.OrderBy(n => n, StringComparer.Ordinal).ToList(),
            added.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ToList());

        Assert.Equal(1, added.Count(c => c.Rarity == "common"));
        Assert.Equal(3, added.Count(c => c.Rarity == "uncommon"));
        Assert.Equal(4, added.Count(c => c.Rarity == "rare"));
    }

    // ★★ An audit finding, pinned so it cannot drift back unnoticed: the Act-IV card tier speaks NONE of Act
    // IV's own five words. The sheets were written before Weighed, Burdened, Inscribed, Entombed and Embalmed
    // were ratified, so the act's cards are all in the older vocabulary — Paperwork, Doubt, Seal, Censure,
    // Lien, Citation, Ward Wax, Blood Ink — and the only card-side reader of the new five is Ward Wax's
    // decay asking whether its bearer is Embalmed.
    //
    // This is a DESIGN gap and not a bug, and it is deliberately left standing: closing it means authoring
    // cards the masters do not contain. The test states the fact so that the day somebody does author one,
    // the assertion is what tells them the tier's vocabulary has changed.
    [Fact]
    public void No_act_four_card_yet_speaks_the_acts_own_five_words()
    {
        string[] theFive = ["Weighed", "Burdened", "Inscribed", "Entombed", "Embalmed"];

        var speaking = FinalCards.RewardPool(act: 4)
            .Where(c => theFive.Any(w => c.Text.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .Select(c => c.Name)
            .ToList();

        Assert.Empty(speaking);
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
