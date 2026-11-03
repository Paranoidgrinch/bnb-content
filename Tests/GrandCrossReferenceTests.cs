using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Grand Cross-Reference (user, 2026-11-03): two of the three volumes are linked and share their Block; the
// Isolated one does nothing and takes double damage; the isolation moves on every 2 turns.
public class GrandCrossReferenceTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Body(RunPlayback play, string marker) =>
        State(play).Combatants.First(c => c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(marker)));

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static bool Isolated(RunPlayback play, string marker) =>
        Body(play, marker).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(GrandCrossReference.IsolatedId));

    // The three volumes, each held to one intent.
    private static RunPlayback Fight(string premise, string authority, string conclusion) =>
        FightProbe.Start(
            FightProbe.Roster("probe.gcr", 9,
                (GrandCrossReference.PremiseId, premise, null),
                (GrandCrossReference.AuthorityId, authority, null),
                (GrandCrossReference.ConclusionId, conclusion, null)),
            deck: [.. Enumerable.Repeat("paper_cut", 24)], health: 900).Play;

    [Fact]
    public void The_conclusion_opens_isolated_and_the_isolation_moves_every_two_turns()
    {
        using var play = Fight("restate_the_premise", "issue_the_binding_interpretation", "draw_the_necessary_result");
        Assert.True(Isolated(play, GrandCrossReference.TheConclusionId));
        play.CombatDriver!.EndTurn();
        Assert.True(Isolated(play, GrandCrossReference.TheConclusionId));
        play.CombatDriver.EndTurn();
        Assert.True(Isolated(play, GrandCrossReference.ThePremiseId));
        Assert.False(Isolated(play, GrandCrossReference.TheConclusionId));
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.True(Isolated(play, GrandCrossReference.TheAuthorityId));
    }

    // Round one: the Premise (7) and the Authority (16) strike, the Isolated Conclusion (9) does nothing.
    [Fact]
    public void The_isolated_volume_does_nothing()
    {
        using var play = Fight("restate_the_premise", "issue_the_binding_interpretation", "draw_the_necessary_result");
        play.CombatDriver!.EndTurn();
        Assert.Equal(900 - 7 - 16, Hero(play).Health.Current);
    }

    [Fact]
    public void The_isolated_volume_takes_double_damage()
    {
        using var play = Fight("restate_the_premise", "issue_the_binding_interpretation", "draw_the_necessary_result");
        var conclusion = Body(play, GrandCrossReference.TheConclusionId);
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, conclusion.Id);
        Assert.Equal(conclusion.Health.Current - 12, Body(play, GrandCrossReference.TheConclusionId).Health.Current);
    }

    // The Premise guards for 6 and its linked partner, the Authority, guards with it; then the Authority guards for 12
    // and the Premise with it. Block still falls away at each body's own turn start, so the Authority, acting second,
    // has lost the Premise's 6 by the time it raises its own. The Isolated Conclusion gets nothing.
    [Fact]
    public void The_linked_pair_shares_its_block()
    {
        using var play = Fight("establish_the_initial_condition", "cite_the_higher_authority", "preserve_the_result");
        play.CombatDriver!.EndTurn();
        Assert.Equal(6 + 12, Block(Body(play, GrandCrossReference.ThePremiseId)));
        Assert.Equal(12, Block(Body(play, GrandCrossReference.TheAuthorityId)));
        Assert.Equal(0, Block(Body(play, GrandCrossReference.TheConclusionId)));
    }
}
