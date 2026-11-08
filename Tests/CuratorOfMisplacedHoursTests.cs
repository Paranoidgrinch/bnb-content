using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Curator of Misplaced Hours (CONTENT_REVIEW E9, 2026-11-08): each turn it lends you an hour — a Borrowed Hour
// in hand gives 1 Energy and puts 6 on its next attack — and every Energy left unspent at the turn's end returns 3.
public class CuratorOfMisplacedHoursTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Curator(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Energy(RunPlayback play) => Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static int Kept(RunPlayback play) => FightProbe.StacksOf(Curator(play), CuratorOfMisplacedHours.KeptHoursId);

    private static RunPlayback Fight(string intent) =>
        FightProbe.Start(FightProbe.Solo(CuratorOfMisplacedHours.EnemyId, intent),
            deck: [.. Enumerable.Repeat("paper_cut", 24)], health: 600).Play;

    private static void Cut(RunPlayback play) =>
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut").Id,
            Curator(play).Id);

    private static void Borrow(RunPlayback play) =>
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == CuratorOfMisplacedHours.BorrowedHourId).Id, null);

    // Borrowed and spent: +1 Energy now, and the next attack is 6 heavier and spends the hours.
    [Fact]
    public void A_borrowed_hour_spent_in_full_is_six_on_its_next_attack()
    {
        using var play = Fight("immediate_correction");
        Assert.Contains(play.CombatDriver!.Current!.Hand, c => c.DefinitionId.value == CuratorOfMisplacedHours.BorrowedHourId);
        Cut(play);                                       // spend first: the probe caps the pool at its start value
        Borrow(play);
        Assert.Equal(3, Energy(play));
        Assert.Equal(CuratorOfMisplacedHours.LoanDamage, Kept(play));
        Cut(play); Cut(play); Cut(play);
        Assert.Equal(0, Energy(play));

        play.CombatDriver.EndTurn();
        Assert.Equal(600 - (17 + CuratorOfMisplacedHours.LoanDamage), Hero(play).Health.Current);
        Assert.Equal(0, Kept(play));
    }

    // Energy left at the turn's end returns hours: 3 for each, never below none.
    [Fact]
    public void Unspent_energy_returns_three_hours_each()
    {
        using var play = Fight("hold_the_present_open");
        Cut(play);
        Borrow(play);                                    // 3 Energy, 6 hours
        Cut(play); Cut(play);                            // 1 Energy left
        play.CombatDriver!.EndTurn();
        Assert.Equal(CuratorOfMisplacedHours.LoanDamage - CuratorOfMisplacedHours.ReturnPerEnergy, Kept(play));

        play.CombatDriver.EndTurn();                     // 3 unspent: the remaining 3 hours are all returned
        Assert.Equal(0, Kept(play));
    }

    // Not borrowing: the card is gone with the turn and the blow is as printed.
    [Fact]
    public void An_hour_not_borrowed_costs_nothing()
    {
        using var play = Fight("immediate_correction");
        play.CombatDriver!.EndTurn();
        Assert.Equal(600 - 17, Hero(play).Health.Current);
        Assert.Equal(1, play.CombatDriver.Current!.Hand.Count(c => c.DefinitionId.value == CuratorOfMisplacedHours.BorrowedHourId));
    }
}
