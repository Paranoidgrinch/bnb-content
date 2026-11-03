using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Black-Ink Oracle (user, 2026-11-03): every second turn it hides the cost of the first card in your hand and
// asks you for it — right, you draw a card; wrong, you take 5 damage.
public class BlackInkOracleTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static CardInstance? Queried(RunPlayback play) =>
        Hand(play).FirstOrDefault(c => c.HasMark(new TagId(BlackInkOracle.QueriedMark)));

    // paper_cut costs 1, so option 1 ("it costs 1") is right and the others are wrong.
    private static RunPlayback Fight() =>
        FightProbe.Start(
            FightProbe.Solo(BlackInkOracle.EnemyId, "pose_the_missing_question"),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400).Play;

    [Fact]
    public void It_asks_on_the_first_turn_and_every_second_one_after()
    {
        using var play = Fight();
        Assert.NotNull(play.CombatDriver!.PendingOptionChoice);
        Assert.Equal(4, play.CombatDriver.PendingOptionChoice!.Count);
        Assert.NotNull(Queried(play));
        play.CombatDriver.SupplyOptionChoice([1]);
        play.CombatDriver.EndTurn();
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.EndTurn();
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
    }

    [Fact]
    public void A_right_guess_draws_a_card_and_shows_the_cost_again()
    {
        using var play = Fight();
        var hand = Hand(play).Count;
        play.CombatDriver!.SupplyOptionChoice([1]);
        Assert.Equal(hand + 1, Hand(play).Count);
        Assert.Equal(400, Hero(play).Health.Current);
        Assert.Null(Queried(play));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void A_wrong_guess_costs_five(int option)
    {
        using var play = Fight();
        var hand = Hand(play).Count;
        play.CombatDriver!.SupplyOptionChoice([option]);
        Assert.Equal(400 - BlackInkOracle.WrongDamage, Hero(play).Health.Current);
        Assert.Equal(hand, Hand(play).Count);
        Assert.Null(Queried(play));
    }
}
