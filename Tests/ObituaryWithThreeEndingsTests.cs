using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// A biography that will not be finished badly: it has to be killed three times, and each life fights its own way.
public class ObituaryWithThreeEndingsTests
{
    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static bool Has(RunPlayback play, CombatantId id, string status) =>
        Enemy(play, id).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    // The Obituary at its authored 128 HP. The rewrites set HP to a fixed number, and death prevention
    // clamps that number to the combatant's MAXIMUM — so a frail probe body would quietly turn "survives at
    // 46" into "survives at 12" and prove nothing.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        string intent, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ObituaryWithThreeEndings.EnemyId, intent, 9, statuses),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

    // Beat it down until it is dead or `until` appears on it, ending turns as the hand runs out. Stopping on
    // the phase marker matters: the next life's clause only goes on at the player's next turn start, so a
    // loop that kept swinging would kill the rewritten body before it had a clause to protect it.
    // Returns false if the combat ended — a final death takes the driver's combat away with it.
    private static bool GrindDown(RunPlayback play, CombatantId id, string? until = null)
    {
        for (var turn = 0; turn < 12; turn++)
        {
            while (play.CombatDriver?.Current is { } combat && combat.Hand.Count > 0)
            {
                var enemy = combat.State.Combatants.FirstOrDefault(c => c.Id == id);
                if (enemy is null || enemy.Health.Current <= 0)
                    return true;
                if (until is not null && Has(play, id, until))
                    return true;
                play.CombatDriver.PlayCard(combat.Hand.First().Id, id);
            }
            if (play.CombatDriver?.Current is null)
                return false;
            if (until is not null && Has(play, id, until))
                return true;
            play.CombatDriver.EndTurn();
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]);
        }
        return play.CombatDriver?.Current is not null;
    }

    // Every life comes back (user, 2026-11-03): the first death rewrites it at 46 as a Heroic Life, the second at
    // 32 as a Completely Accurate Account, and the third is final.
    [Fact]
    public void It_dies_three_times()
    {
        var (play, session, obituary) = Fight("settled_accounts");

        GrindDown(play, obituary, until: ObituaryWithThreeEndings.HeroicPhaseId);
        Assert.Null(session.Error);
        Assert.Equal(46, Enemy(play, obituary).Health.Current);
        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));

        GrindDown(play, obituary, until: ObituaryWithThreeEndings.AccuratePhaseId);
        Assert.Null(session.Error);
        Assert.Equal(32, Enemy(play, obituary).Health.Current);
        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.HeroicLifeId));

        GrindDown(play, obituary);
        Assert.Null(session.Error);
        Assert.True(play.CombatDriver?.Current is null || Enemy(play, obituary).Health.Current <= 0);
        play.Dispose();
    }

    // Each life fights its own way: the same slot is 13 in the first life and two blows of 8 in the second.
    [Fact]
    public void The_heroic_life_strikes_twice()
    {
        var (play, session, _) = Fight("record_a_respectable_career", (ObituaryWithThreeEndings.HeroicPhaseId, 1));
        var hero = play.CombatDriver!.Current!.HeroId;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(400 - 16, play.CombatDriver.Current!.State.GetCombatant(hero).Health.Current);
        play.Dispose();
    }
}
