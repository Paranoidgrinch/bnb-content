using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — the last three stages. A day that does not happen, a death that needs paperwork, and the archive
// settling everything it did to you back onto you.
public class ActTwoStagesEightToTenTests
{
    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static int Marked(RunPlayback play, string mark) =>
        Enum.GetValues<CardZone>().SelectMany(zone => Zones(play).GetCardsInZone(zone))
            .Count(card => card.HasMark(new TagId(mark)));

    // "Tuesday does not occur: it takes no action, and direct card damage against it lands 25% harder."
    [Fact]
    public void The_day_that_did_not_happen_takes_the_blows_harder()
    {
        var (missing, session, missingId) = FightProbe.Start(
            FightProbe.Solo("unoccurred_tuesday", "tuesday_does_not_occur", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);
        var (ordinary, plainSession, ordinaryId) = FightProbe.Start(
            FightProbe.Solo("unoccurred_tuesday", "calendar_residue", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        missing.CombatDriver!.EndTurn();   // the day that does not happen
        ordinary.CombatDriver!.EndTurn();  // an ordinary one

        var missingBefore = Enemy(missing, missingId).Health.Current;
        var ordinaryBefore = Enemy(ordinary, ordinaryId).Health.Current;
        missing.CombatDriver.PlayCard(missing.CombatDriver.Current!.Hand.First().Id, missingId);
        ordinary.CombatDriver.PlayCard(ordinary.CombatDriver.Current!.Hand.First().Id, ordinaryId);
        Assert.Null(session.Error);
        Assert.Null(plainSession.Error);

        Assert.True(
            missingBefore - Enemy(missing, missingId).Health.Current
            > ordinaryBefore - Enemy(ordinary, ordinaryId).Health.Current,
            "the same card should land harder on a day that did not happen");
        missing.Dispose();
        ordinary.Dispose();
    }

    // "At 4 Residue: one card in hand becomes Redacted and another becomes Misfiled."
    [Fact]
    public void Everything_else_settles_back_onto_you()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("miscellany_index", "cross_list", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        // Four redacted cards played is four Residue, and the fourth files everything else.
        for (var turn = 0; turn < 6; turn++)
        {
            var redacted = play.CombatDriver!.Current!.Hand
                .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.RedactedMark)));
            if (redacted is not null)
            {
                play.CombatDriver.PlayCard(redacted.Id, enemyId);
                Assert.Null(session.Error);
                if (Marked(play, ActTwo.MisfiledMark) > 0)
                {
                    play.Dispose();
                    return; // the archive settled: something in hand was misfiled
                }
            }
            play.CombatDriver!.EndTurn();
        }

        Assert.Fail("the Index never settled its residue");
    }
}
