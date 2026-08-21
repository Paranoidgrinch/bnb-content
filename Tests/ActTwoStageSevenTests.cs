using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — Restricted Annex. The Mnemonic Chain remembers one concrete card INSTANCE, not a card name: the
// mark rides that copy through every zone, so the Chain knows the book when it comes round again.
public class ActTwoStageSevenTests
{
    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static int Health(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id).Health.Current;

    // "The first eligible card played becomes a remembered concrete card instance."
    [Fact]
    public void The_chain_remembers_the_first_book_you_open()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("mnemonic_chain", "tighten_link", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        var first = play.CombatDriver!.Current!.Hand.First();
        play.CombatDriver.PlayCard(first.Id, enemyId);
        Assert.Null(session.Error);

        Assert.True(Zones(play).GetCard(first.Id).HasMark(new TagId(ActTwo.RememberedMark)));

        // …and only the first: a second play is not remembered too.
        var second = play.CombatDriver.Current!.Hand.First();
        play.CombatDriver.PlayCard(second.Id, enemyId);
        Assert.False(Zones(play).GetCard(second.Id).HasMark(new TagId(ActTwo.RememberedMark)));
        play.Dispose();
    }

    // "When that exact instance later re-enters the hand it is Referenced, and costs 1 more."
    [Fact]
    public void The_remembered_book_is_cited_and_dearer_when_it_returns()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("mnemonic_chain", "tighten_link", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 6)]);

        var first = play.CombatDriver!.Current!.Hand.First();
        play.CombatDriver.PlayCard(first.Id, enemyId);
        Assert.Null(session.Error);

        // Play the deck down so the discard pile is shuffled back and the remembered copy returns.
        for (var turn = 0; turn < 4; turn++)
        {
            while (play.CombatDriver.Current!.Hand.Count > 0)
                play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand.First().Id, enemyId);
            play.CombatDriver.EndTurn();

            var back = play.CombatDriver.Current!.Hand.FirstOrDefault(c => c.Id == first.Id);
            if (back is null)
                continue;

            Assert.True(Zones(play).GetCard(first.Id).HasMark(new TagId(ActTwo.ChainReferenceMark)));
            Assert.Equal(1, Zones(play).GetCard(first.Id)
                .GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
            play.Dispose();
            return;
        }

        Assert.Fail("the remembered card never came back round");
    }

    // "If the player plays it anyway: the Chain takes 8 direct damage."
    [Fact]
    public void Returning_the_remembered_book_costs_the_chain()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("mnemonic_chain", "tighten_link", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 6)]);

        var first = play.CombatDriver!.Current!.Hand.First();
        play.CombatDriver.PlayCard(first.Id, enemyId);
        Assert.Null(session.Error);

        for (var turn = 0; turn < 4; turn++)
        {
            while (play.CombatDriver.Current!.Hand.Count > 0)
            {
                var card = play.CombatDriver.Current!.Hand.First();
                if (card.Id == first.Id && card.HasMark(new TagId(ActTwo.ChainReferenceMark)))
                {
                    var before = Health(play, enemyId);
                    play.CombatDriver.PlayCard(card.Id, enemyId);
                    // The Chain guards itself, so the card's own damage is blocked — the 8 the citation costs
                    // it ignores Block and is exactly what comes through.
                    Assert.True(before - Health(play, enemyId) >= 8,
                        "playing the cited book should have cost the Chain 8 through its guard");
                    play.Dispose();
                    return;
                }
                play.CombatDriver.PlayCard(card.Id, enemyId);
            }
            play.CombatDriver.EndTurn();
        }

        Assert.Fail("the remembered card never came back round");
    }
}
