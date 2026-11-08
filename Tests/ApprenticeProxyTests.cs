using BnbContent.Converter;
using BnbContent.Converter.Apprentice;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.ApprenticeTests;

namespace BnbContent.Tests;

// The general cards in the Apprentice's wand (master §11, acceptance E36–E39): every one has a proxy, its upgrade has
// the same, and what the card does when PLAYED is untouched.
public class ApprenticeProxyTests
{
    private static readonly string[] GeneralIds = [.. BnbContent.Converter.Witch.WitchFamilies.General.Keys];

    [Fact]
    public void Every_general_card_and_its_upgrade_carries_its_proxy()
    {
        Assert.Equal(70, ApprenticeProxies.General.Count);
        Assert.Equal(GeneralIds.Order(StringComparer.Ordinal), ApprenticeProxies.General.Keys.Order(StringComparer.Ordinal));
        var cards = FightProbe.Shipped.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var id in GeneralIds)
        {
            Assert.True(cards.ContainsKey(id), id);
            Assert.NotNull(cards[id].Wand);
            Assert.Same(cards[id].Wand, cards[id + "+"].Wand);
            Assert.StartsWith(cards[id].Wand!.Operation.ToString(),
                FightProbe.Shipped.Presentation.Cards[id].Extra[ApprenticeProxies.WandTextKey]);
        }
    }

    private static readonly IReadOnlyDictionary<string, string> ShortTexts = BabData.Load(TestData.Directory).CardTexts;

    // Only the wand part was added: the card's play, its cost and its tags are what they were.
    [Fact]
    public void A_proxy_changes_nothing_about_playing_the_card()
    {
        var plain = BnbContent.Converter.Cards.FinalCards.Compile().ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var card in FightProbe.Shipped.Cards.Where(c => GeneralIds.Contains(c.Id.TrimEnd('+'))))
        {
            var before = plain[card.Id];
            Assert.Equal(before.Costs, card.Costs);
            // …in the card's own short words where it has them (source-data/cards/card_texts.json).
            Assert.Equal(ShortTexts.GetValueOrDefault(card.Id, before.DescriptionKey), card.DescriptionKey);
            var options = CombatJson.CreateOptions<CardPlayContext>();
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(before.Program, options),
                System.Text.Json.JsonSerializer.Serialize(card.Program, options));
        }
    }

}
