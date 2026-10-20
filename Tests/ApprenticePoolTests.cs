using BnbContent.Converter;
using BnbContent.Converter.Apprentice;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// What the Sorcerer's Apprentice is offered: his own cards and the general ones, on every reward, transform, shelf
// and market; his relics on his relic draws; and nothing of his ever offered to the others.
public class ApprenticePoolTests
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

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void He_draws_only_his_own_and_the_general_cards(int act)
    {
        var pools = ConversionPools.Build(act);
        var his = pools.ApprenticeRewardCards.Select(c => c.Id).ToHashSet();
        var drawn = Drawn(pools.CardRewardSource(), ApprenticeCharacter.Id).ToList();
        Assert.All(drawn, id => Assert.Contains(id.TrimEnd('+'), his));
        Assert.Contains(drawn, id => ApprenticeCards.RewardPool(act).Any(c => c.Id == id.TrimEnd('+')));

        // …and none of his is ever drawn for the other two.
        var own = ApprenticeCharacter.Exclusive.ToHashSet();
        Assert.DoesNotContain(Drawn(pools.CardRewardSource(), ConversionPools.BureaucratId), id => own.Contains(id));
        Assert.DoesNotContain(Drawn(pools.CardRewardSource(), Converter.Witch.WitchCharacter.Id), id => own.Contains(id));
    }

    // Master §1: no rarity is locked behind an act — his rares can come from Act I on.
    [Fact]
    public void His_rares_are_offered_from_the_first_act()
    {
        var rares = Drawn(ConversionPools.Build(1).CardRewardSource("rare"), ApprenticeCharacter.Id).ToHashSet();
        Assert.Contains(rares, id => ApprenticeCards.Rares().Any(c => c.Id == id.TrimEnd('+')));
    }

    [Fact]
    public void His_relics_are_drawn_for_him_alone()
    {
        var pools = ConversionPools.Build(2);
        var source = pools.NormalRelicOfRarity("test", (Converter.Relics.RelicAuthoring.Rarity.Common, 1));
        IEnumerable<string> Relics(string character) => Enumerable.Range(1, 80)
            .SelectMany(seed => source.Generate(Run(character, seed)))
            .SelectMany(o => o.Grant.OfType<AddRelicByIdRunEffect>()).Select(a => a.Relic.Value);
        var own = ApprenticeRelics.All().Select(r => r.Id).ToHashSet();
        Assert.Contains(Relics(ApprenticeCharacter.Id), own.Contains);
        Assert.DoesNotContain(Relics(ConversionPools.BureaucratId), own.Contains);
    }

    [Fact]
    public void The_character_shelf_and_the_markets_carry_his_cards_for_him()
    {
        var shop = ShopTemplate.Build(ConversionPools.Build(1), new Random(1));
        var shelf = shop.Stock!.Single(g => g.Id == Converter.Relics.ShopRelics.CharacterCardShelf);
        Assert.Equal(10, shelf.Offers.Count(e => e.Tags?.Contains(ConversionPools.For(ApprenticeCharacter.Id)) == true));

        var stalls = FightProbe.Game.Events.Values
            .SelectMany(e => e.Situations.Values).SelectMany(s => s.Choices)
            .Where(c => c.Id.StartsWith("apprentice-card-", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(stalls);
        bool Shown(EventChoice choice, string character) =>
            choice.Requirement?.Evaluate(new RunEvalContext(Run(character))) ?? true;
        Assert.All(stalls, s => Assert.True(Shown(s, ApprenticeCharacter.Id)));
        Assert.All(stalls, s => Assert.False(Shown(s, ConversionPools.BureaucratId)));
    }
}
