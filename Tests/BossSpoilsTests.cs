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

    // THE MIMIC IS THE ONLY ROLE THAT STILL PAYS A RELIC OFF THE ROLE TABLE. Elite and Boss each pay one of
    // their own, per encounter, since 2026-09-10 — a boss its forced 1-of-3, an elite the single relic
    // written for it — because the role table drew from the ported v2 list and handed out demo relics for
    // four acts. What is asserted here is the label, as before: a reward that opens another reward is
    // announced as a card unless it declares its kind.
    [Fact]
    public void The_mimic_pays_a_relic_and_calls_it_a_relic()
    {
        var labels = RoleLabels(MapNodeKind.Mimic);

        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.Contains("a relic", label));
    }

    [Theory]
    [InlineData(MapNodeKind.Elite)]
    [InlineData(MapNodeKind.Boss)]
    public void The_role_table_promises_no_relic_where_the_encounter_pays_its_own(MapNodeKind role)
    {
        var labels = RoleLabels(role);

        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.DoesNotContain("a relic", label));
    }

    [Fact]
    public void Every_elite_pays_its_own_relic_and_says_so()
    {
        var elites = (from act in Game.Acts ?? []
                      let spec = act.MapGeneration
                      where spec is not null
                      from entry in spec.VictoryRewardsByEncounter
                      where entry.Key.Contains("_elite_", StringComparison.Ordinal)
                      where entry.Value.Source is FixedRewardSource
                      from offer in ((FixedRewardSource)entry.Value.Source).Offers
                      select Labeler.Offer(offer)).ToList();

        Assert.Equal(38, elites.Count);
        Assert.All(elites, label =>
        {
            Assert.Contains("Gold", label);
            Assert.Contains("a card reward", label);
            Assert.Contains("a relic", label);
        });
    }

    private List<string> RoleLabels(MapNodeKind role) =>
        [.. from act in Game.Acts ?? []
            let spec = act.MapGeneration
            where spec is not null && spec.VictoryRewards.ContainsKey(role)
            let source = spec.VictoryRewards[role].Source as FixedRewardSource
            where source is not null
            from offer in source.Offers
            select Labeler.Offer(offer)];

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
