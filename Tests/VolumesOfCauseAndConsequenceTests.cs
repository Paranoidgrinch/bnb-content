using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Two volumes with a line drawn between them: whatever damage one takes, the other gains as Block (user,
// 2026-11-03).
public class VolumesOfCauseAndConsequenceTests
{
    private static CombatantState Volume(RunPlayback play, string statusId) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(statusId)));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static (RunPlayback Play, InteractiveRunSession Session) Pair(
        string causesIntent, string consequencesIntent) =>
        FightProbe.Start(
            FightProbe.Roster("probe.volumes", 9,
                (VolumesOfCauseAndConsequence.CausesId, causesIntent, null),
                (VolumesOfCauseAndConsequence.ConsequencesId, consequencesIntent, null)),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400) is var f
            ? (f.Play, f.Session)
            : default;

    [Theory]
    [InlineData(VolumesOfCauseAndConsequence.TheCausesId, VolumesOfCauseAndConsequence.TheConsequencesId)]
    [InlineData(VolumesOfCauseAndConsequence.TheConsequencesId, VolumesOfCauseAndConsequence.TheCausesId)]
    public void Damage_to_one_volume_is_block_on_the_other(string struck, string other)
    {
        var (play, session) = Pair("cause_without_warning", "enforce_the_result");
        var target = Volume(play, struck);
        Assert.Equal(0, Block(Volume(play, other)));

        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, target.Id);
        Assert.Null(session.Error);

        Assert.Equal(target.Health.Current - 6, Volume(play, struck).Health.Current);
        Assert.Equal(6, Block(Volume(play, other)));
        play.Dispose();
    }

    // Only health lost is handed over: a blow the volume's own Block stopped gives the other nothing.
    [Fact]
    public void A_blow_its_own_block_stopped_hands_nothing_over()
    {
        var (play, session) = Pair("establish_the_premise", "enforce_the_result");
        play.CombatDriver!.EndTurn();
        var causes = Volume(play, VolumesOfCauseAndConsequence.TheCausesId);
        Assert.Equal(8, Block(causes));

        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, causes.Id);
        Assert.Null(session.Error);

        Assert.Equal(2, Block(Volume(play, VolumesOfCauseAndConsequence.TheCausesId)));
        Assert.Equal(0, Block(Volume(play, VolumesOfCauseAndConsequence.TheConsequencesId)));
        play.Dispose();
    }

    [Fact]
    public void The_result_filed_as_fact_guards_both_volumes()
    {
        var (play, _) = Pair("establish_the_premise", "result_filed_as_fact");
        play.CombatDriver!.EndTurn();

        Assert.Equal(8 + 14, Block(Volume(play, VolumesOfCauseAndConsequence.TheCausesId)));
        Assert.Equal(14, Block(Volume(play, VolumesOfCauseAndConsequence.TheConsequencesId)));
        play.Dispose();
    }
}
