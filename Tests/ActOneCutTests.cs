using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-I content cut of 2026-10-29: what the four stages ask of the player, and that every rule the player is
// asked about wears a chip.
public class ActOneCutTests
{
    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int BlockOf(RunPlayback play, CombatantId id) =>
        Body(play, id).DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    // The Wrong Window answers a second card of the turn's opening type with 5 Block — on the Scribe alone, and
    // the player can see the rule on it. In the duo the Line beside it is not a window.
    [Fact]
    public void The_scribe_wears_its_rule_and_alone_answers_the_second_card_of_the_type()
    {
        var probe = FightProbe.Roster("wrong_window",
            energy: 9,
            ("wrong_window_scribe", "tap_the_counter", null),
            ("a_very_official_line", "shuffle_forward", null));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 5).ToList());
        var scribe = Find(play, "wrong_window_scribe");
        var line = Find(play, "a_very_official_line");

        Assert.Equal(1, FightProbe.StacksOf(Body(play, scribe), PassiveStatuses.NotThisCounterId));

        Play(play, session, "paper_cut", line);
        Assert.Equal(0, BlockOf(play, scribe));
        Play(play, session, "paper_cut", line);
        Assert.Equal(5, BlockOf(play, scribe));
        Assert.Equal(0, BlockOf(play, line));
        play.Dispose();
    }
}
