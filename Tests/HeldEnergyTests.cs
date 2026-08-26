using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Energy pool has a HARD ceiling: the engine clamps every gain to the pool's own max, and the turn's
// refill happens before the turn's triggers run. So every relic that promised "at the start of your turn,
// gain 1 Energy" was doing nothing at all, silently — four Normal relics, one Event relic and one Rite, on
// top of the boss relics this device was written for.
//
// These tests pin the fix from both ends: the promise is kept, and it is kept at the moment it can be.
public class HeldEnergyTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Energy(CombatantState combatant) =>
        combatant.Resources[StandardCombatIds.EnergyResource].Current;

    // Every relic that promises Energy up front now holds it — and holding it is visible on the table.
    [Theory]
    [InlineData("blood_stamped_bond")]
    [InlineData("rootbound_walking_staff")]
    [InlineData("iron_astrolabe")]
    public void A_relic_that_promises_energy_up_front_holds_it_instead(string relicId)
    {
        var (play, _, _) = WithRelic(relicId);

        Assert.Equal(3, Energy(Hero(play)));                                  // the pool was full…
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), HeldEnergy.Id));      // …so the point waits
        play.Dispose();
    }

    // …and the held point arrives on the card the holder could not otherwise have played.
    [Fact]
    public void Held_energy_arrives_the_moment_the_pool_runs_dry()
    {
        var (play, session, target) = WithRelic("iron_astrolabe");

        for (var i = 0; i < 3; i++)                                           // spend the pool
            Play(play, session, "paper_cut", target);

        Assert.Equal(1, Energy(Hero(play)));                                  // the held point landed
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), HeldEnergy.Id));      // and is spent

        Play(play, session, "paper_cut", target);                             // the fourth card, paid for
        Assert.Equal(0, Energy(Hero(play)));
        play.Dispose();
    }

    // The Conservator's Thread asked "is anything still in hand?" at the turn's end — where the answer is
    // always no, because the engine discards the hand before the turn-end triggers run. It reads what the
    // hand HELD now, and pays for it.
    [Fact]
    public void The_conservators_thread_pays_for_a_hand_it_can_no_longer_see()
    {
        var (play, session, _) = WithRelic("conservators_thread");

        play.CombatDriver!.EndTurn();      // cards left behind
        Assert.Null(session.Error);

        // The Block is granted after the NEXT draw, since Block gained at a turn's end is swept away.
        Assert.Equal(4, Block(Hero(play)));
        play.Dispose();
    }

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(string relicId)
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 3);
        var blueprint = FightProbe.OneFight(probe, Enumerable.Repeat("paper_cut", 12).ToList());
        blueprint = blueprint with
        {
            Start = blueprint.Start with { StartingRelics = [.. blueprint.Start.StartingRelics, relicId] },
            Characters = [],
        };

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }
}
