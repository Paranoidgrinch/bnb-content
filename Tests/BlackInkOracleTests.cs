using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Black-Ink Oracle (CONTENT_REVIEW E8, 2026-11-08): every second turn it Clashes — you see your top card and
// may put it on the bottom, then it reveals a plate (cost 0-2). Your card costs more: you draw it. Less: 5 damage.
public class BlackInkOracleTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Oracle(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static IReadOnlyList<CardInstance> DrawPile(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(CardZone.DrawPile);

    private static RunPlayback Fight(params string[] deck) =>
        FightProbe.Start(
            FightProbe.Solo(BlackInkOracle.EnemyId, "pose_the_missing_question"),
            deck: deck.Length > 0 ? deck : [.. Enumerable.Repeat("paper_cut", 16)], health: 400).Play;

    [Fact]
    public void It_clashes_on_the_first_turn_and_every_second_one_after()
    {
        using var play = Fight();
        Assert.NotNull(play.CombatDriver!.PendingOptionChoice);
        Assert.Equal(2, play.CombatDriver.PendingOptionChoice!.Count);
        play.CombatDriver.SupplyOptionChoice([0]);
        play.CombatDriver.EndTurn();
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.EndTurn();
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
    }

    // The player sees the top card of the draw pile — the decision is made looking at it.
    [Fact]
    public void The_player_sees_the_top_card()
    {
        var rules = FightProbe.Game.Statuses.Single(s => s.Id == BlackInkOracle.OracleRulesId);
        Assert.Equal(1, rules.Disclosure!.DrawPileCards);
    }

    // Whatever the plate, the outcome follows the costs: higher draws the card, lower costs 5, equal is nothing.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void The_higher_cost_wins(int option)
    {
        // Paper Cut costs 1, Verdict 2: either can be the card that clashes.
        using var play = Fight([.. Enumerable.Repeat("paper_cut", 8), .. Enumerable.Repeat("summary_judgment", 8)]);
        var clashing = DrawPile(play)[option];           // kept on top, or the one under it
        var cost = clashing.DefinitionId.value == "summary_judgment" ? 2 : 1;
        var hand = Hand(play).Count;

        play.CombatDriver!.SupplyOptionChoice([option]);
        var plate = Hero(play).GetCounter(BlackInkOracle.PlateCounter);
        Assert.InRange(plate, 0, BlackInkOracle.Plates - 1);
        Assert.Equal(1, FightProbe.StacksOf(Oracle(play), BlackInkOracle.PlateId(plate)));

        var won = cost > plate;
        Assert.Equal(won ? hand + 1 : hand, Hand(play).Count);
        Assert.Equal(won, Hand(play).Any(c => c.Id == clashing.Id));
        Assert.Equal(cost < plate ? 400 - BlackInkOracle.WrongDamage : 400, Hero(play).Health.Current);
    }

    // Putting the top card on the bottom: the next one is what clashes.
    [Fact]
    public void Putting_it_on_the_bottom_brings_the_next_card_up()
    {
        using var play = Fight();
        var top = DrawPile(play)[0].Id;
        play.CombatDriver!.SupplyOptionChoice([1]);
        Assert.NotEqual(top, DrawPile(play)[0].Id);
        Assert.Equal(top, DrawPile(play)[^1].Id);
    }
}
