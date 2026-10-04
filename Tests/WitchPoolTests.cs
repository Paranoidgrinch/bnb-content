using BnbContent.Converter;
using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// What each character is offered (plan W4, pools): every card reward, transform and character shelf holds the
// Bureaucrat's entries and the Witch's side by side, each written for its character. She draws her own cards and the
// general ones; he draws exactly what he drew before she existed.
public class WitchPoolTests
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
    [InlineData(3)]
    public void Each_character_draws_only_its_own_and_the_general_cards(int act)
    {
        var pools = ConversionPools.Build(act);
        var hers = pools.WitchRewardCards.Select(c => c.Id).ToHashSet();
        var his = pools.RewardCards.Select(c => c.Id).ToHashSet();

        var witch = Drawn(pools.CardRewardSource(), WitchCharacter.Id).ToList();
        Assert.All(witch, id => Assert.Contains(id, hers));
        Assert.Contains(witch, id => WitchCards.RewardPool(act).Any(c => c.Id == id));

        var bureaucrat = Drawn(pools.CardRewardSource(), ConversionPools.BureaucratId).ToList();
        Assert.All(bureaucrat, id => Assert.Contains(id, his));
        Assert.All(Drawn(pools.CardRewardSource("rare"), WitchCharacter.Id), id => Assert.Contains(id, hers));
    }

    // The Bureaucrat's draws are byte-identical to a pool that holds his entries alone, untagged — which is what
    // every pool was before she existed.
    [Fact]
    public void The_bureaucrat_draws_exactly_as_before_she_existed()
    {
        var pools = ConversionPools.Build(2);
        var source = (PoolRewardSource)pools.CardRewardSource();
        var before = new PoolRewardSource(new RunPool<RewardOffer>(
            [.. source.Pool.Entries
                .Where(e => e.Value.Tags?.Contains(ConversionPools.For(ConversionPools.BureaucratId)) == true)
                .Select(e => e with { Value = e.Value with { Tags = null } })]),
            source.Count) { UpgradeChancePercent = source.UpgradeChancePercent };
        for (var seed = 1; seed <= 40; seed++)
            Assert.Equal(
                before.Generate(Run(ConversionPools.BureaucratId, seed)).Select(o => o.Id),
                source.Generate(Run(ConversionPools.BureaucratId, seed)).Select(o => o.Id));
    }

    [Fact]
    public void The_character_shelf_stocks_her_cards_for_her()
    {
        var shop = ShopTemplate.Build(ConversionPools.Build(1), new Random(1));
        var shelf = shop.Stock!.Single(g => g.Id == Converter.Relics.ShopRelics.CharacterCardShelf);
        var hers = shelf.Offers.Where(e => e.Tags!.Contains(ConversionPools.For(WitchCharacter.Id))).ToList();
        Assert.Equal(10, hers.Count);
        Assert.Equal(10, shelf.Offers.Count - hers.Count);
        Assert.All(hers, e => Assert.StartsWith("buy-", e.Id));
    }

    [Fact]
    public void The_bureaucrat_id_is_the_rosters()
    {
        Assert.Equal(ConversionPools.BureaucratId, FightProbe.Game.Characters![0].Id);
    }

    // The markets an event builds (the Licensed Vendor, the Act-III fairs): each card stall stands for one character,
    // and a character's own cards are never on the other's stall.
    [Fact]
    public void Market_stalls_show_each_character_its_own_cards()
    {
        var hers = WitchCharacter.Exclusive.ToHashSet();
        var his = Converter.Cards.FinalCards.CharacterPool(5).Select(c => c.Id).ToHashSet();
        var stalls = FightProbe.Game.Events.Values
            .SelectMany(e => e.Situations.Values).SelectMany(s => s.Choices)
            .Where(c => c.Id.StartsWith("card-", StringComparison.Ordinal) || c.Id.StartsWith("witch-card-", StringComparison.Ordinal))
            .Select(c => (Choice: c, Card: c.Effects.OfType<AddCardToDeckRunEffect>().Select(a => a.Card.value).FirstOrDefault()))
            .Where(s => s.Card is not null)
            .ToList();
        Assert.Contains(stalls, s => hers.Contains(s.Card!));

        bool Shown(EventChoice choice, string character) =>
            choice.Requirement?.Evaluate(new RunEvalContext(Run(character))) ?? true;
        foreach (var (choice, card) in stalls)
        {
            if (hers.Contains(card!))
                Assert.False(Shown(choice, ConversionPools.BureaucratId), $"{choice.Id} shows him {card}");
            if (his.Contains(card!))
                Assert.False(Shown(choice, WitchCharacter.Id), $"{choice.Id} shows her {card}");
        }
    }

    // Relic rewards the same way: she draws the general ones and her own; he draws exactly as before.
    [Fact]
    public void Relic_rewards_are_written_for_each_character()
    {
        var pools = ConversionPools.Build(2);
        var source = (PoolRewardSource)pools.NormalRelicOnTheCurve("test");
        var hers = WitchRelics.All().Select(r => "relic-" + r.Id).ToHashSet();
        var drawn = Enumerable.Range(1, 120).SelectMany(seed => source.Generate(Run(WitchCharacter.Id, seed)))
            .Select(o => o.Id).ToList();
        Assert.Contains(drawn, hers.Contains);
        Assert.All(Enumerable.Range(1, 120).SelectMany(seed => source.Generate(Run(ConversionPools.BureaucratId, seed))),
            o => Assert.DoesNotContain(o.Id, hers));

        var before = new PoolRewardSource(new RunPool<RewardOffer>(
            [.. source.Pool.Entries
                .Where(e => e.Value.Tags?.Contains(ConversionPools.For(ConversionPools.BureaucratId)) == true)
                .Select(e => e with { Value = e.Value with { Tags = null } })]), source.Count);
        for (var seed = 1; seed <= 40; seed++)
            Assert.Equal(before.Generate(Run(ConversionPools.BureaucratId, seed)).Select(o => o.Id),
                source.Generate(Run(ConversionPools.BureaucratId, seed)).Select(o => o.Id));
    }
}
