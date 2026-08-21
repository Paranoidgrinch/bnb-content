using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Redaction Galleries. Redacted is the act's fourth and last vocabulary: a card whose next play
// is halved. The halving is the ENGINE's (two reserved per-instance counters, consumed by the play); the mark
// beside it is the content's, and it is the only part a rule can see — an enemy cannot look at a scale factor.
public class ActTwoStageFiveTests
{
    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static IEnumerable<CardInstance> AllCards(RunPlayback play) =>
        Enum.GetValues<CardZone>().SelectMany(zone => Zones(play).GetCardsInZone(zone));

    private static int Marked(RunPlayback play, string mark) =>
        AllCards(play).Count(card => card.HasMark(new TagId(mark)));

    // The Husk's attack redacts a card, and the redaction is both a mark and a halving.
    [Fact]
    public void A_redacted_card_carries_both_the_mark_and_the_halving()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("palimpsest_husk", "scrape_the_surface"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();

        var redacted = Assert.Single(AllCards(play).Where(c => c.HasMark(new TagId(ActTwo.RedactedMark))));
        Assert.Equal(1, redacted.GetMarkCounter(StandardCombatIds.CardOutputScaleNumeratorCounter));
        Assert.Equal(2, redacted.GetMarkCounter(StandardCombatIds.CardOutputScaleDenominatorCounter));
        play.Dispose();
    }


}
