using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;

namespace BnbContent.Tests;

// "The first ingredient each turn is free" holds in EVERY fight of the game (player report 2026-10-04: the pot
// greyed out). Each encounter is fought for four turns; each turn the hand is played until the Energy is gone,
// and then the pot must still take a card for nothing — unless there is no card in hand it would take.
//
// Three things had broken it, each in its own corner of the game: Burdened ("every card you play costs 1 more")
// priced the pot as if it were a card; the Cartouche's wall, which stops buffs, ate the free-ingredient token; and
// a decree capping the cards of a turn (Nisaba's) refused the pot once the cap was reached. The pot is not a card
// played, and the token is a rule, not a buff.
public class WitchPotEverywhereTests
{
    private static readonly CardDefinitionId Add = new(WitchActions.AddIngredient);

    private static readonly HashSet<string> Uncookable = FightProbe.Game.Cards
        .Where(c => c.Tags.Any(t => t.value == WitchActions.Uncookable)).Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void After_the_energy_is_spent_the_first_ingredient_still_goes_in_for_nothing_in_every_fight()
    {
        var offenders = new System.Collections.Concurrent.ConcurrentBag<string>();
        var encounters = FightProbe.Game.Encounters.Where(e => !e.Id.Value.StartsWith("tutorial", StringComparison.Ordinal));
        Parallel.ForEach(encounters, new ParallelOptions { MaxDegreeOfParallelism = 4 }, encounter =>
        {
            var (play, session, _) = FightProbe.Start(
                FightProbe.Authored(encounter.Id.Value), health: 999, character: WitchCharacter.Id);
            var driver = play.CombatDriver!;
            for (var turn = 1; turn <= 4 && session.Error is null; turn++)
            {
                Answer(driver);
                if (driver.Current is not { IsHeroTurn: true })
                    break;
                SpendTheEnergy(driver);
                if (driver.Current is not { IsHeroTurn: true } now)
                    break;

                var hand = now.Hand.Count(c => !Uncookable.Contains(c.DefinitionId.value));
                var pot = now.State.GetCardZones(now.HeroId).SetAside.Count;
                var enemy = FirstEnemy(now);
                var cost = now.ActionCost(Add, enemy).Sum(c => c.Amount);
                if (hand > 0 && pot < WitchActions.Slots && (cost != 0 || !now.CanUse(Add, enemy)))
                    offenders.Add($"{encounter.Id.Value} turn {turn}: cost {cost}, usable {now.CanUse(Add, enemy)}, "
                        + $"energy {now.HeroEnergy}, statuses [{string.Join(",", now.State.GetCombatant(now.HeroId).Statuses.Select(s => s.DefinitionId.value))}]");
                driver.EndTurn();
            }
            play.Dispose();
        });
        Assert.True(offenders.IsEmpty, string.Join("\n", offenders.Order()));
    }

    private static CombatantId? FirstEnemy(RogueDeck.Scenario.Scripting.InteractiveCombat combat) =>
        combat.State.Combatants.FirstOrDefault(c => c.Id != combat.HeroId && c.IsAlive
            && c.TeamId == StandardCombatIds.EnemyTeam)?.Id;

    private static void Answer(RogueDeck.Sandbox.Run.InteractiveCombatDriver driver)
    {
        if (driver.PendingCardChoice is { } cards)
            driver.SupplyCardChoice([.. cards.Take(driver.PendingCardChoiceCount).Select(c => c.Id)]);
        if (driver.PendingOptionChoice is not null)
            driver.SupplyOptionChoice([0]);
    }

    // Plays whatever the hand allows until the Energy is gone or nothing more can be played. Bounded by the
    // Energy pool, not the hand: a card that refunds itself would otherwise loop.
    private static void SpendTheEnergy(RogueDeck.Sandbox.Run.InteractiveCombatDriver driver)
    {
        for (var guard = 0; guard < 10 && driver.Current is { IsHeroTurn: true, HeroEnergy: > 0 } now; guard++)
        {
            var (energy, hand) = (now.HeroEnergy, now.Hand.Count);
            if (now.Hand.FirstOrDefault(c => now.CanPlay(c.Id)) is not { } card)
                return;
            driver.PlayCard(card.Id, FirstEnemy(now));
            Answer(driver);
            if (driver.Current is null || (driver.Current.HeroEnergy == energy && driver.Current.Hand.Count == hand))
                return;
        }
    }
}
