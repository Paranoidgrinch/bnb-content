using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Hushed Reading Room. The room does not attack your deck, it attacks your HAND: what you are
// still holding, how much of it you spent, and what you never got to say.
//
// Three facts these rules stand on, all measured: the played-card count INCLUDES the card being played; that
// card is already out of the hand when a rule answering the play runs; and a conditional that goes FIRST in a
// trigger program loses its body (see ConditionalTriggerRootTortureTests in the engine).
public class ActTwoStageFourTests
{
    private static int Counter(RunPlayback play, string counter) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId)
            .Counters.TryGetValue(new CounterId(counter), out var value) ? value : 0;

    // "After the player's fourth played card, the oldest remaining card in hand goes straight to discard." The
    // hand loses two on that play: the card spent, and the place cleared.
    [Fact]
    public void The_table_clears_a_place_after_your_fourth_card()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("unclaimed_reading_table", "clear_the_table", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        for (var i = 0; i < 3; i++)
            Play(play, session, enemyId);
        var beforeFourth = play.CombatDriver!.Current!.Hand.Count;

        Play(play, session, enemyId);

        Assert.Equal(beforeFourth - 2, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void The_table_clears_only_one_place_a_turn()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("unclaimed_reading_table", "clear_the_table", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        for (var i = 0; i < 4; i++)
            Play(play, session, enemyId);
        Assert.Equal(0, play.CombatDriver!.Current!.Hand.Count); // 5 spent-and-cleared away

        play.Dispose();
    }

    // "A visible limit of 5" — introduced on the first draw, because a starting status can put a status on a
    // combatant but not a number.
    [Fact]
    public void The_margin_introduces_itself_at_five()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("mute_margin", "white_space", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Assert.Equal(5, Counter(play, "mute_margin"));
        play.Dispose();
    }

    // Writing past it narrows it, down to a floor of three.
    [Fact]
    public void Writing_past_the_margin_narrows_it()
    {
        var probe = FightProbe.Solo("mute_margin", "white_space", energy: 9);
        var wide = new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            probe.HeroStartingStatuses, probe.HeroDisplayName, cardsDrawnPerTurn: 8, probe.TriggeredEffects);
        var (play, session, enemyId) = FightProbe.Start(
            wide, deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        for (var i = 0; i < 6; i++) // one past the limit of five
            Play(play, session, enemyId);

        Assert.Equal(4, Counter(play, "mute_margin"));
        play.Dispose();
    }

    // A turn kept inside the margin never widens it past five.
    [Fact]
    public void A_turn_inside_the_margin_never_widens_it_past_five()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("mute_margin", "white_space", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn(); // a turn spent playing nothing at all

        Assert.Equal(5, Counter(play, "mute_margin"));
        play.Dispose();
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
