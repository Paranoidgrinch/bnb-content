using BnbContent.Converter;
using RogueDeck.Run;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// WHAT A FIGHT PAYS HAS TO SAY WHAT IT IS. A boss's spoils are one bundle that opens two more picks — the
// card reward and the boss's own relic — and a bundle can only be read through the words it puts on the
// screen. Every one of them said "a card reward", because that was the engine's guess for any reward that
// opens another reward; so a boss announced its relic as a card, twice, and the relic then arrived under the
// same heading as the card pick before it, with nothing naming it.
//
// The fix is not a nicer sentence: it is that the offer DECLARES its kind. These tests read the labels the
// frontends actually bake into the pick, so a reward that forgets to say what it is fails here.
public class BossSpoilsTests
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260829);

    private static readonly RunEntityLabeler Labeler = new(
        Game.Cards.ToDictionary(c => c.Id, c => c.NameKey ?? c.Id),
        Game.Relics.ToDictionary(r => r.Id, r => r.DisplayName),
        new Dictionary<string, string> { [StandardRunIds.Gold.Value] = "Gold" },
        new Dictionary<string, string>());

    private static IEnumerable<(string Encounter, RewardOffer Offer)> BossSpoils() =>
        from act in Game.Acts ?? []
        let spec = act.MapGeneration
        where spec is not null
        from entry in spec.VictoryRewardsByEncounter
        where entry.Value.Source is FixedRewardSource
        from offer in ((FixedRewardSource)entry.Value.Source).Offers
        select (entry.Key, offer);

    [Fact]
    public void Every_boss_pays_a_purse_a_card_and_a_relic_and_says_which_is_which()
    {
        var spoils = BossSpoils().ToList();
        Assert.NotEmpty(spoils);
        Assert.All(spoils, entry =>
        {
            var label = Labeler.Offer(entry.Offer);
            Assert.Contains("Gold", label);
            Assert.Contains("a card reward", label);
            Assert.Contains("a relic", label);
        });
    }

    // The failure this catches is not "a missing word" but "the same word twice": two different doors behind
    // one label is a bundle that cannot be read at all.
    [Fact]
    public void No_two_things_a_boss_pays_are_announced_with_the_same_words()
    {
        Assert.All(BossSpoils(), entry =>
        {
            var parts = Labeler.Offer(entry.Offer).Split(" + ");
            Assert.Equal(parts.Length, parts.Distinct(StringComparer.Ordinal).Count());
        });
    }

    // The elite and the mimic pay a relic too, off the role table rather than a named boss's.
    [Theory]
    [InlineData(MapNodeKind.Elite)]
    [InlineData(MapNodeKind.Boss)]
    [InlineData(MapNodeKind.Mimic)]
    public void Every_fight_that_pays_a_relic_calls_it_a_relic(MapNodeKind role)
    {
        var labels = (from act in Game.Acts ?? []
                      let spec = act.MapGeneration
                      where spec is not null && spec.VictoryRewards.ContainsKey(role)
                      let source = spec.VictoryRewards[role].Source as FixedRewardSource
                      where source is not null
                      from offer in source.Offers
                      select Labeler.Offer(offer)).ToList();

        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.Contains("a relic", label));
    }

    // The ordinary fights pay no relic, and must not claim one.
    [Fact]
    public void An_ordinary_fight_promises_a_card_and_nothing_else()
    {
        var labels = (from act in Game.Acts ?? []
                      let spec = act.MapGeneration
                      where spec is not null && spec.VictoryRewards.ContainsKey(MapNodeKind.Combat)
                      let source = spec.VictoryRewards[MapNodeKind.Combat].Source as FixedRewardSource
                      where source is not null
                      from offer in source.Offers
                      select Labeler.Offer(offer)).ToList();

        Assert.NotEmpty(labels);
        Assert.All(labels, label =>
        {
            Assert.Contains("a card reward", label);
            Assert.DoesNotContain("a relic", label);
        });
    }
}
