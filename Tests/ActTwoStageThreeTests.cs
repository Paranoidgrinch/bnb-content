using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Whispering Catalogue. A Reference is a debt with a name on it: play the cited card and it is
// fulfilled, leave it in the hand you put down and it costs you an Overdue owed to the one who cited you.
// That is why the whole rule lives on the CITING ENEMY — the Overdue has to come from it. Since 2026-11-08 the
// Second-Person Entry cites from the discard pile instead, and the Orphan Citation lets you pick what it cites.
public class ActTwoStageThreeTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Marked(RunPlayback play, string mark) =>
        Enum.GetValues<CardZone>()
            .SelectMany(zone => play.CombatDriver!.Current!.State
                .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(zone))
            .Count(card => card.HasMark(new TagId(mark)));

    // ── the Second-Person Entry's Back-Reference (CONTENT_REVIEW E5, 2026-11-08) ──────────────────────────

    private static IEnumerable<CardInstance> Zone(RunPlayback play, CardZone zone) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(zone);

    private static int Block(CombatantState body) =>
        body.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // The first hand has no discard to cite from; the second does, and a Requisition Slip comes with it.
    [Fact]
    public void The_entry_cites_a_card_in_your_discard_pile()
    {
        using var play = FightProbe.Start(FightProbe.Solo("second_person_entry", "cross_reference"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400).Play;
        Assert.Equal(0, Marked(play, ActTwo.EntryReferenceMark));
        Assert.DoesNotContain(play.CombatDriver!.Current!.Hand, c => c.DefinitionId.value == ActTwo.RequisitionSlipId);

        play.CombatDriver.EndTurn();
        Assert.Single(Zone(play, CardZone.DiscardPile), c => c.HasMark(new TagId(ActTwo.EntryReferenceMark)));
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.DefinitionId.value == ActTwo.RequisitionSlipId);
    }

    // 1 Energy brings the cited card back into the hand, and nothing is owed.
    [Fact]
    public void A_requisition_slip_takes_the_cited_card_back()
    {
        var (play, session, entry) = FightProbe.Start(FightProbe.Solo("second_person_entry", "cross_reference"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);
        play.CombatDriver!.EndTurn();
        var cited = Zone(play, CardZone.DiscardPile).Single(c => c.HasMark(new TagId(ActTwo.EntryReferenceMark)));
        var hand = play.CombatDriver.Current!.Hand.Count;

        play.CombatDriver.PlayCard(
            play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == ActTwo.RequisitionSlipId).Id, null);
        Assert.Null(session.Error);
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.Id == cited.Id);
        Assert.Equal(hand, play.CombatDriver.Current!.Hand.Count);    // the slip went, the card came
        Assert.Equal(0, Marked(play, ActTwo.EntryReferenceMark));
        Assert.Equal(2, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);

        play.CombatDriver.EndTurn();
        Assert.Empty(Zone(play, CardZone.ExhaustPile).Where(c => c.DefinitionId.value == "paper_cut"));
        Assert.Equal(12, Block(play.CombatDriver.Current!.State.GetCombatant(entry))); // Cross-Reference alone
        play.Dispose();
    }

    // Leave it and the card is gone for the fight, and the Entry guards on it: 4 Block per Energy it cost.
    [Fact]
    public void An_unanswered_citation_is_exhausted_and_the_entry_guards_on_it()
    {
        var (play, _, entry) = FightProbe.Start(FightProbe.Solo("second_person_entry", "you_are_here"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);
        play.CombatDriver!.EndTurn();
        var cited = Zone(play, CardZone.DiscardPile).Single(c => c.HasMark(new TagId(ActTwo.EntryReferenceMark)));

        play.CombatDriver.EndTurn();
        Assert.Contains(Zone(play, CardZone.ExhaustPile), c => c.Id == cited.Id);
        Assert.Equal(0, Marked(play, ActTwo.EntryReferenceMark));
        Assert.Equal(ActTwo.BackReferenceBlockPerCost, Block(play.CombatDriver.Current!.State.GetCombatant(entry)));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));  // it no longer files Overdue
        play.Dispose();
    }

    // ── the Orphan Citation's Source Reconstruction (CONTENT_REVIEW E5, 2026-11-08) ─────────────────────────

    // The top 3 of the draw pile are offered; the one chosen comes into the hand, cited.
    [Fact]
    public void The_orphan_citation_offers_the_top_three_and_cites_the_one_taken()
    {
        using var play = FightProbe.Start(FightProbe.Solo("orphan_citation", "missing_source"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400).Play;
        var offered = play.CombatDriver!.PendingCardChoice;
        Assert.NotNull(offered);
        Assert.Equal(ActTwo.OrphanShows, offered!.Count);
        var top = Zone(play, CardZone.DrawPile).Take(ActTwo.OrphanShows).Select(c => c.Id).ToList();
        Assert.Equal(top, offered.Select(c => c.Id).ToList());

        var hand = play.CombatDriver.Current!.Hand.Count;
        play.CombatDriver.SupplyCardChoice([offered[2].Id]);
        Assert.Equal(hand + 1, play.CombatDriver.Current!.Hand.Count);
        Assert.True(play.CombatDriver.Current!.Hand.Single(c => c.Id == offered[2].Id)
            .HasMark(new TagId(ActTwo.CitationReferenceMark)));
    }

    // Play it and nothing is owed; leave it and the Citation files an Overdue.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_card_taken_must_be_played_that_turn(bool played)
    {
        var (play, session, orphan) = FightProbe.Start(FightProbe.Solo("orphan_citation", "missing_source"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);
        var taken = play.CombatDriver!.PendingCardChoice![0].Id;
        play.CombatDriver.SupplyCardChoice([taken]);
        if (played)
        {
            play.CombatDriver.PlayCard(taken, orphan);
            Assert.Null(session.Error);
        }
        play.CombatDriver.EndTurn();
        Assert.Equal(played ? 0 : 1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        play.Dispose();
    }

    // "Two consecutive cards of the same Base Cost" — the alphabet learns the price.
    [Fact]
    public void The_alphabet_learns_a_price_paid_twice_in_a_row()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("fanged_alphabet", "re_index"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        Play(play, session, enemyId);
        Play(play, session, enemyId);

        var learned = Hero(play).Counters
            .Single(c => c.Key == new CounterId("alphabet_learned_cost")).Value;
        Assert.True(learned > 0, "the alphabet should have learned the price it saw twice");
        play.Dispose();
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
