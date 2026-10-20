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
        Assert.Equal(50, ApprenticeProxies.General.Count);
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

    // Only the wand part was added: the card's play, its cost and its tags are what they were.
    [Fact]
    public void A_proxy_changes_nothing_about_playing_the_card()
    {
        var plain = BnbContent.Converter.Cards.FinalCards.Compile().ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var card in FightProbe.Shipped.Cards.Where(c => GeneralIds.Contains(c.Id.TrimEnd('+'))))
        {
            var before = plain[card.Id];
            Assert.Equal(before.Costs, card.Costs);
            Assert.Equal(before.DescriptionKey, card.DescriptionKey);
            var options = CombatJson.CreateOptions<CardPlayContext>();
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(before.Program, options),
                System.Text.Json.JsonSerializer.Serialize(card.Program, options));
        }
    }

    [Fact]
    public void Grave_lien_in_the_wand_speaks_its_small_proxy()
    {
        var (play, session, enemy) = Fight(["grave_lien", "ap_chalk_line", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "grave_lien", 0);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        Assert.Equal(before - 6, Standing(Body(play, enemy)));
        Assert.Equal(4, FightProbe.StacksOf(Body(play, enemy), BnbContent.Converter.Cards.Keywords.Lien));
        play.Dispose();
    }

    // E38: Grand Dispensation's proxy is not repeatable — an Echo after it has nothing to copy.
    [Fact]
    public void Grand_dispensation_cannot_be_echoed()
    {
        var (play, session, enemy) = Fight(["grand_dispensation", "ap_flyleaf", "ap_chalk_line", "ap_chalk_line", "ap_candle_stub"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "grand_dispensation", 0);
        Inscribe(play, session, "ap_flyleaf", 1);
        var combat = Now(play);
        var plan = WandCompiler.Compile(combat.State, combat.Registry, combat.HeroId, enemy);
        Assert.True(plan.Steps[1].Fizzled);
        play.Dispose();
    }
}
