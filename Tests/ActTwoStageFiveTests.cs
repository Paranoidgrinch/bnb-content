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



    // "The first time each turn a Redacted card is fully played, it becomes Misfiled afterwards."
    //
    // Everything happens through real actions: a FightProbe fight is driven by DETERMINISTIC REPLAY, so state
    // written into the live combat between actions is gone on the next one. The redaction comes from the
    // Husk's own attack.
    [Fact]
    public void The_husk_files_away_what_it_wrote_over()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("palimpsest_husk", "scrape_the_surface", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();   // the Husk redacts the top of the draw pile
        play.CombatDriver.EndTurn();    // …and the next draw brings it into hand

        var redacted = play.CombatDriver.Current!.Hand
            .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.NotNull(redacted);

        play.CombatDriver.PlayCard(redacted!.Id, enemyId);
        Assert.Null(session.Error);

        // About THAT card, not about the table: the Husk redacts one every turn, so others are still marked.
        var after = Card(play, redacted.Id);
        Assert.False(after.HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.True(after.HasMark(new TagId(ActTwo.MisfiledMark)));
        play.Dispose();
    }

    // "The first time each turn the player plays a Redacted card, the Portrait loses 8 Block."
    [Fact]
    public void The_portraits_guard_opens_when_a_redacted_card_is_played()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("vacant_portrait", "erase_the_face", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();

        var redacted = play.CombatDriver.Current!.Hand
            .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.NotNull(redacted);

        // With no guard up, the whole 8 lands as damage — which is the half of the rule that makes the
        // absence felt when there is nothing left to strip.
        var plain = play.CombatDriver.Current!.Hand.First(c => c.Id != redacted!.Id);
        var before = Health(play, enemyId);
        play.CombatDriver.PlayCard(plain.Id, enemyId);
        var afterPlain = Health(play, enemyId);

        play.CombatDriver.PlayCard(redacted!.Id, enemyId);
        var afterRedacted = Health(play, enemyId);

        // A redacted card hits for HALF, so it would land for less than the plain one — and yet the Portrait
        // loses more, because the frame opens for 8 on top of it.
        Assert.True(afterPlain - afterRedacted > before - afterPlain,
            "playing the redacted card should have cost the Portrait more than a plain card did");
        play.Dispose();
    }

    private static CardInstance Card(RunPlayback play, CardInstanceId id) =>
        Enum.GetValues<CardZone>()
            .SelectMany(zone => Zones(play).GetCardsInZone(zone))
            .Single(card => card.Id == id);

    private static int Health(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id).Health.Current;
}
