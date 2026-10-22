using BnbContent.Converter;
using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// What the Lieutenant is offered: her own cards and the general ones on every reward, transform, shelf and market;
// her relics on her relic draws; and nothing of hers ever offered to the others (LT-002, LT-046).
public class LieutenantPoolTests
{
    private static RunState Run(string? character, int seed = 7)
    {
        var run = new RunState(new RunId("run"), new HealthState(40, 40), new RunMap([]), randomSeed: seed);
        run.SetCharacter(character);
        return run;
    }

    private static IEnumerable<string> Drawn(IRewardSource source, string character) =>
        Enumerable.Range(1, 60).SelectMany(seed => source.Generate(Run(character, seed)))
            .SelectMany(o => o.Grant.OfType<AddCardToDeckRunEffect>()).Select(a => a.Card.value);

    [Fact]
    public void Her_pool_is_four_starters_and_twenty_thirty_five_and_twenty_five()
    {
        Assert.Equal(4, LieutenantCards.Starter().Count(c => !c.Id.EndsWith('+')));
        Assert.Equal(20, LieutenantCards.Commons().Count(c => !c.Id.EndsWith('+')));
        Assert.Equal(35, LieutenantCards.Uncommons().Count(c => !c.Id.EndsWith('+')));
        Assert.Equal(25, LieutenantCards.Rares().Count(c => !c.Id.EndsWith('+')));
        Assert.All(LieutenantCards.All().Where(c => !c.Id.EndsWith('+')),
            c => Assert.Contains(LieutenantCards.All(), u => u.Id == c.Id + "+"));
        // The canon's rarity swap (§1).
        Assert.Equal("common", LieutenantCards.All().Single(c => c.Id == "lt_whetstone").Rarity);
        Assert.Equal("uncommon", LieutenantCards.All().Single(c => c.Id == "lt_heave").Rarity);
    }

    // Plan L2: commons in Act I, uncommons 23 / 12, rares 8 / 6 / 6 / 5.
    [Fact]
    public void Her_cards_are_gated_by_act_like_the_others()
    {
        int Count(string rarity, int act) =>
            LieutenantCards.All().Count(c => !c.Id.EndsWith('+') && c.Rarity == rarity && c.Act == act);
        Assert.Equal(20, Count("common", 1));
        Assert.Equal(23, Count("uncommon", 1));
        Assert.Equal(12, Count("uncommon", 2));
        Assert.Equal([8, 6, 6, 5], [Count("rare", 1), Count("rare", 2), Count("rare", 3), Count("rare", 4)]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void She_draws_only_her_own_and_the_general_cards(int act)
    {
        var pools = ConversionPools.Build(act);
        var hers = pools.LieutenantRewardCards.Select(c => c.Id).ToHashSet();
        var drawn = Drawn(pools.CardRewardSource(), LieutenantCharacter.Id).ToList();
        Assert.All(drawn, id => Assert.Contains(id.TrimEnd('+'), hers));
        Assert.Contains(drawn, id => LieutenantCards.RewardPool(act).Any(c => c.Id == id.TrimEnd('+')));

        var own = LieutenantCharacter.Exclusive.ToHashSet();
        foreach (var other in new[] { ConversionPools.BureaucratId, Converter.Witch.WitchCharacter.Id, Converter.Apprentice.ApprenticeCharacter.Id })
            Assert.DoesNotContain(Drawn(pools.CardRewardSource(), other), id => own.Contains(id));
    }

    [Fact]
    public void Her_relics_are_drawn_for_her_alone()
    {
        var pools = ConversionPools.Build(2);
        var source = pools.NormalRelicOfRarity("test", (Converter.Relics.RelicAuthoring.Rarity.Common, 1));
        IEnumerable<string> Relics(string character) => Enumerable.Range(1, 80)
            .SelectMany(seed => source.Generate(Run(character, seed)))
            .SelectMany(o => o.Grant.OfType<AddRelicByIdRunEffect>()).Select(a => a.Relic.Value);
        var own = LieutenantRelics.All().Select(r => r.Id).ToHashSet();
        Assert.Contains(Relics(LieutenantCharacter.Id), own.Contains);
        Assert.DoesNotContain(Relics(ConversionPools.BureaucratId), own.Contains);
        Assert.DoesNotContain(Relics(Converter.Apprentice.ApprenticeCharacter.Id), own.Contains);
    }

    [Fact]
    public void The_character_shelf_and_the_markets_carry_her_cards_for_her()
    {
        var shop = ShopTemplate.Build(ConversionPools.Build(1), new Random(1));
        var shelf = shop.Stock!.Single(g => g.Id == Converter.Relics.ShopRelics.CharacterCardShelf);
        Assert.Equal(10, shelf.Offers.Count(e => e.Tags?.Contains(ConversionPools.For(LieutenantCharacter.Id)) == true));

        var stalls = FightProbe.Game.Events.Values
            .SelectMany(e => e.Situations.Values).SelectMany(s => s.Choices)
            .Where(c => c.Id.StartsWith("lieutenant-card-", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(stalls);
        bool Shown(EventChoice choice, string character) =>
            choice.Requirement?.Evaluate(new RunEvalContext(Run(character))) ?? true;
        Assert.All(stalls, s => Assert.True(Shown(s, LieutenantCharacter.Id)));
        Assert.All(stalls, s => Assert.False(Shown(s, ConversionPools.BureaucratId)));
    }

    [Fact]
    public void She_is_on_the_roster()
    {
        var roster = FightProbe.Game.Characters!.Single(c => c.Id == LieutenantCharacter.Id);
        Assert.Equal(80, roster.Start.MaxHealth);
        Assert.Contains("lt_relic_commission", roster.Exclusive!);
    }
}
