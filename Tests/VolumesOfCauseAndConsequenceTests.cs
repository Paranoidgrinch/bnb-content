using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Two volumes with a line drawn between them. Causes cites your cards; Consequences hits you with whatever
// the citation turned out to mean. Every test here is about that line — what fulfilling a citation does to
// the one that issued it, and what it does to the one that collects.
public class VolumesOfCauseAndConsequenceTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Volume(RunPlayback play, string statusId) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(statusId)));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static CardInstance? Cited(RunPlayback play) =>
        play.CombatDriver!.Current!.Hand
            .FirstOrDefault(c => c.HasMark(new TagId(VolumesOfCauseAndConsequence.CausesReferenceMark)));

    // Both volumes, each narrowed to one intent — the roster probe, so the Concordance has both ends.
    private static (RunPlayback Play, InteractiveRunSession Session) Pair(
        string causesIntent, string consequencesIntent) =>
        FightProbe.Start(
            FightProbe.Roster("probe.volumes", 9,
                (VolumesOfCauseAndConsequence.CausesId, causesIntent, null),
                (VolumesOfCauseAndConsequence.ConsequencesId, consequencesIntent, null)),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400) is var f
            ? (f.Play, f.Session)
            : default;

    // 10.5: Establish the Premise cites a card in hand and guards behind the citation.
    [Fact]
    public void Causes_cites_a_card_and_guards_behind_it()
    {
        var (play, _) = Pair("establish_the_premise", "return_to_the_premise");

        play.CombatDriver!.EndTurn();

        Assert.NotNull(Cited(play));
        Assert.Equal(8, Block(Volume(play, VolumesOfCauseAndConsequence.TheCausesId)));
        play.Dispose();
    }

    // 10.2 fulfilled: playing the cited card costs Causes 9 HP and up to 8 Block — and the HP loss is not a
    // Damage event, so the Block it is standing behind does not stop it.
    [Fact]
    public void Fulfilling_a_citation_wounds_causes_through_its_own_block()
    {
        var (play, session) = Pair("establish_the_premise", "return_to_the_premise");
        play.CombatDriver!.EndTurn();

        var causes = Volume(play, VolumesOfCauseAndConsequence.TheCausesId);
        var hp = causes.Health.Current;
        Assert.Equal(8, Block(causes));

        play.CombatDriver.PlayCard(Cited(play)!.Id, causes.Id);
        Assert.Null(session.Error);

        var after = Volume(play, VolumesOfCauseAndConsequence.TheCausesId);
        Assert.Equal(hp - 9, after.Health.Current);
        Assert.Equal(0, Block(after)); // 8 Block, 8 struck
        play.Dispose();
    }

    // …and the other end of the line: Consequences gains Supported Result.
    [Fact]
    public void Fulfilling_a_citation_supports_the_consequence()
    {
        var (play, session) = Pair("establish_the_premise", "return_to_the_premise");
        play.CombatDriver!.EndTurn();

        play.CombatDriver.PlayCard(
            Cited(play)!.Id, Volume(play, VolumesOfCauseAndConsequence.TheCausesId).Id);
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(
            Volume(play, VolumesOfCauseAndConsequence.TheConsequencesId),
            VolumesOfCauseAndConsequence.SupportedId));
        play.Dispose();
    }

    // 10.2 failed: letting the citation go owes Causes an Overdue AND leaves Consequences unsupported.
    [Fact]
    public void Failing_a_citation_owes_a_debt_and_unsupports_the_consequence()
    {
        var (play, _) = Pair("establish_the_premise", "return_to_the_premise");

        play.CombatDriver!.EndTurn(); // cited
        play.CombatDriver.EndTurn();  // the card was never played; Causes collects at its own turn start

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        Assert.Equal(1, FightProbe.StacksOf(
            Volume(play, VolumesOfCauseAndConsequence.TheConsequencesId),
            VolumesOfCauseAndConsequence.UnsupportedId));
        play.Dispose();
    }

    // 10.6 Enforce the Result: 17 neutral, 23 supported, 11 unsupported — and the result is spent by the blow.
    [Theory]
    [InlineData(null, 17)]
    [InlineData(VolumesOfCauseAndConsequence.SupportedId, 23)]
    [InlineData(VolumesOfCauseAndConsequence.UnsupportedId, 11)]
    public void The_result_is_enforced_at_what_the_concordance_says(string? result, int expected)
    {
        var statuses = result is null
            ? Array.Empty<(string, int)>()
            : [(result, 1)];
        var (play, _, consequences) = FightProbe.Start(
            FightProbe.Solo(VolumesOfCauseAndConsequence.ConsequencesId, "enforce_the_result", 9, statuses),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(expected, before - Hero(play).Health.Current);
        // "Then consume the active Concordance result."
        if (result is not null)
            Assert.Equal(0, FightProbe.StacksOf(Enemy(play, consequences), result));
        play.Dispose();
    }

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    // 10.6 Result Filed as Fact: BOTH volumes gain 14 Block, which is what makes killing either one slow.
    [Fact]
    public void The_result_filed_as_fact_guards_both_volumes()
    {
        var (play, _) = Pair("cause_without_warning", "result_filed_as_fact");

        play.CombatDriver!.EndTurn();

        Assert.Equal(14, Block(Volume(play, VolumesOfCauseAndConsequence.TheCausesId)));
        Assert.Equal(14, Block(Volume(play, VolumesOfCauseAndConsequence.TheConsequencesId)));
        play.Dispose();
    }

    // 10.8: when one volume dies the Concordance breaks — the survivor drops its result and reads alone,
    // one Strength the stronger.
    [Fact]
    public void The_survivor_reads_alone_and_reads_harder()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("probe.volumes.kill", 9,
                (VolumesOfCauseAndConsequence.CausesId, "cause_without_warning", 6),
                (VolumesOfCauseAndConsequence.ConsequencesId, "enforce_the_result", null)),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        var causes = Volume(play, VolumesOfCauseAndConsequence.TheCausesId);
        // paper_cut for 6: exactly enough to put a 6-HP Causes down.
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, causes.Id);
        Assert.Null(session.Error);

        Assert.True(
            play.CombatDriver.Current!.State.GetCombatant(causes.Id).Health.Current <= 0,
            "the Volume of Causes should be down");
        var survivor = Volume(play, VolumesOfCauseAndConsequence.TheConsequencesId);
        Assert.Equal(1, FightProbe.StacksOf(survivor, "strength"));
        play.Dispose();
    }
}
