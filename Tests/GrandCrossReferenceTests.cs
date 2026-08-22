using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// "Nothing in the Archive means anything alone." Three volumes hang around a concordance that cannot be
// touched, and only two of them are linked at a time. Kill all three and the engine becomes real — under the
// rule the LAST volume you killed writes. These tests kill it three ways and get three different bosses.
public class GrandCrossReferenceTests
{
    private const string Deed = "paper_cut";

    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState? Body(RunPlayback play, string marker) =>
        State(play).Combatants.FirstOrDefault(c =>
            c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(marker)));

    private static CombatantState Engine(RunPlayback play) =>
        State(play).Combatants.First(c => c.Id.value.Contains("grand_cross_reference"));

    private static bool EngineHas(RunPlayback play, string status) =>
        Engine(play).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    // The authored encounter exactly as the act fields it: four bodies, real HP, real intents.
    private static RunPlayback Fight()
    {
        var play = new RunPlayback(() => { });
        play.Start(
            FightProbe.OneFight(
                FightProbe.Authored("archives_boss_grand_cross_reference", energy: 9),
                deck: [.. Enumerable.Repeat(Deed, 24)], health: 900),
            seed: 1, interactive: true);
        var session = play.Session!;
        Assert.True(play.Error is null, play.Error);
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);
        return play;
    }

    private static void EndTurn(RunPlayback play, int option = 0)
    {
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
    }

    // Beat one named volume down until it is gone, ending turns as the hand runs out.
    private static void Break(RunPlayback play, string marker)
    {
        for (var turn = 0; turn < 30 && Body(play, marker) is { } volume && volume.Health.Current > 0; turn++)
        {
            while (play.CombatDriver?.Current is { } combat && combat.Hand.Count > 0
                   && Body(play, marker) is { Health.Current: > 0 } target)
            {
                play.CombatDriver.PlayCard(combat.Hand[0].Id, target.Id);
            }
            if (play.CombatDriver?.Current is null)
                return;
            EndTurn(play);
        }
    }

    // §9.1: the concordance is on the table from the first bell but is not part of the argument — nothing
    // the player does reaches it while the volumes stand.
    [Fact]
    public void The_concordance_cannot_be_touched_while_its_volumes_stand()
    {
        var play = Fight();
        var engine = Engine(play);

        Assert.True(EngineHas(play, GrandCrossReference.UntouchableId));

        var before = engine.Health.Current;
        while (play.CombatDriver!.Current is { } c && c.Hand.Count > 0)
            play.CombatDriver.PlayCard(c.Hand[0].Id, engine.Id);

        Assert.Equal(before, Engine(play).Health.Current);
    }

    // §9.1: three volumes at the HP the master fixes for them.
    [Fact]
    public void It_fields_three_volumes_around_a_central_body()
    {
        var play = Fight();

        Assert.Equal(68, Body(play, GrandCrossReference.ThePremiseId)!.Health.Max);
        Assert.Equal(72, Body(play, GrandCrossReference.TheAuthorityId)!.Health.Max);
        Assert.Equal(76, Body(play, GrandCrossReference.TheConclusionId)!.Health.Max);
        Assert.Equal(96, Engine(play).Health.Max);
    }

    // §9.7/§Transition: when all three are broken the concordance becomes real — and the LAST one broken
    // decides which rule it fights under. This is the whole boss: the player writes the final law.
    private static void BreakInOrder(RunPlayback play, params string[] order)
    {
        foreach (var marker in order)
            Break(play, marker);

        // One more enemy turn for the concordance to notice and take the field.
        for (var turn = 0; turn < 3 && EngineHas(play, GrandCrossReference.UntouchableId); turn++)
            EndTurn(play);
    }

    [Fact]
    public void Killing_the_premise_last_writes_the_premise_thesis()
    {
        var play = Fight();
        BreakInOrder(play,
            GrandCrossReference.TheConclusionId, GrandCrossReference.TheAuthorityId,
            GrandCrossReference.ThePremiseId);

        Assert.False(EngineHas(play, GrandCrossReference.UntouchableId),
            "the concordance never became part of the argument");
        Assert.True(EngineHas(play, GrandCrossReference.ThesisPremiseId));
        Assert.False(EngineHas(play, GrandCrossReference.ThesisAuthorityId));
        Assert.False(EngineHas(play, GrandCrossReference.ThesisConclusionId));
    }

    [Fact]
    public void Killing_the_authority_last_writes_the_authority_thesis()
    {
        var play = Fight();
        BreakInOrder(play,
            GrandCrossReference.ThePremiseId, GrandCrossReference.TheConclusionId,
            GrandCrossReference.TheAuthorityId);

        Assert.True(EngineHas(play, GrandCrossReference.ThesisAuthorityId));
        Assert.False(EngineHas(play, GrandCrossReference.ThesisPremiseId));
    }

    // §9.10: the Conclusion's thesis arrives already scheduled — a Final Result that is coming whatever you do.
    [Fact]
    public void Killing_the_conclusion_last_schedules_the_result()
    {
        var play = Fight();
        BreakInOrder(play,
            GrandCrossReference.ThePremiseId, GrandCrossReference.TheAuthorityId,
            GrandCrossReference.TheConclusionId);

        Assert.True(EngineHas(play, GrandCrossReference.ThesisConclusionId));
        Assert.True(EngineHas(play, GrandCrossReference.FinalResultId),
            "The Result Was Always Fixed, but nothing was actually filed");
    }

    // …and once the volumes are gone the concordance really can be fought.
    [Fact]
    public void Once_the_volumes_are_gone_the_concordance_can_be_fought()
    {
        var play = Fight();
        BreakInOrder(play,
            GrandCrossReference.TheConclusionId, GrandCrossReference.TheAuthorityId,
            GrandCrossReference.ThePremiseId);

        var engine = Engine(play);
        var before = engine.Health.Current;

        while (play.CombatDriver!.Current is { } c && c.Hand.Count > 0)
            play.CombatDriver.PlayCard(c.Hand[0].Id, engine.Id);

        Assert.True(play.CombatDriver.Current is null || Engine(play).Health.Current < before,
            "the concordance was still untouchable after all three volumes broke");
    }
}
