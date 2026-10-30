using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, elites 4–6, proved in live fights. Between them they read the act's last three words at boss grade:
// what a turn is actually worth, and what a free card costs.
public class ActFourEliteReadingTests
{
    private const string OneCost = "paper_cut";   // Deed, 1: deal 6
    private const string TwoCost = "permit_a38";  // 2
    private const string Wax = "waxen_surety";    // Working, 1: gain 7 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static bool Holds(RunPlayback play, string cardId) =>
        play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == cardId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    // ── the Mummified Overseer of the Linen House ─────────────────────────────────────────────────────────

    // Each turn, once the hand is drawn, two cards of it are wrapped in linen: they cost nothing.
    [Fact]
    public void Two_cards_of_the_hand_are_wrapped_and_cost_nothing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("mummified_overseer_of_the_linen_house", "dry_hand_of_authority", energy: 3),
            deck: [TwoCost, TwoCost, TwoCost, TwoCost, TwoCost], health: 900);

        var hand = play.CombatDriver!.Current!.Hand;
        Assert.Equal(2, hand.Count(c => c.Marks.Contains(new RogueDeck.Core.Combat.TagId(ActFour.LinenWrappedMark))));
        play.Dispose();
    }

    // Playing a wrapped card spends no Energy, buries the player by one, and takes the linen off.
    [Fact]
    public void A_wrapped_card_is_free_and_buries_you_by_one()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Solo("mummified_overseer_of_the_linen_house", "dry_hand_of_authority", energy: 3),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 900);

        var mark = new RogueDeck.Core.Combat.TagId(ActFour.LinenWrappedMark);
        var wrapped = play.CombatDriver!.Current!.Hand.First(c => c.Marks.Contains(mark));
        var energy = play.CombatDriver.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId)
            .Resources[StandardCombatIds.EnergyResource].Current;
        play.CombatDriver.PlayCard(wrapped.Id, enemy);
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(energy, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(1, play.CombatDriver.Current!.Hand.Count(c => c.Marks.Contains(mark)));
        play.Dispose();
    }

    // ── the Treasury of the Two Pans ──────────────────────────────────────────────────────────────────────

}
