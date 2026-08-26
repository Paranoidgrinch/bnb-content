using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The shared Act-II event objects (BnB_Final_Events_Master_PostAudit.md §ACT II). Fifteen events will be
// written out of this vocabulary, so what it does has to be true before any of them is.
public class ActTwoEventObjectTests
{
    private static readonly string[] Cut = ["paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"];

    private static readonly string[] Markings = [ActTwoEventObjects.ArchiveMarkings];

    [Fact]
    public void The_three_temporary_cards_ship_and_two_of_them_bite_at_the_end_of_the_turn()
    {
        var compiled = ActTwoEventObjects.Compile().ToDictionary(c => c.Id);

        Assert.Equal(3, compiled.Count);
        foreach (var id in new[] { "unfinished_citation", "borrowers_claim" })
            Assert.True(compiled[id].LifecyclePrograms.ContainsKey(CardLifecycleTrigger.TurnEndInHand),
                $"'{id}' should file something if it is left in hand");
        // The Leaf acts while it is held, not when the turn ends — its rule is a status, not a lifecycle.
        Assert.Empty(compiled["redacted_leaf"].LifecyclePrograms);
        Assert.All(compiled.Values, card => Assert.True(card.RetainInHandOnTurnEnd));
    }

    // ── what one fight has to honour ──────────────────────────────────────────────────────────────────────

    // A redaction is a MARK plus the halving beside it. Only the mark rides across from the run, so the rule
    // has to finish the job — otherwise a card the archive redacted between fights hits for full.
    [Fact]
    public void A_card_redacted_between_fights_lands_for_half()
    {
        using var full = ArchiveProbe.Enter(Cut, [], Markings);
        var before = full.EnemyHealth;
        full.Play_(full.InHand("paper_cut"));
        var whole = before - full.EnemyHealth;

        using var redacted = ArchiveProbe.Enter(Cut, [("paper_cut", ActTwo.RedactedMark)], Markings);
        var start = redacted.EnemyHealth;
        redacted.Play_(redacted.Zone(CardZone.Hand)
            .First(c => c.Marks.Contains(new TagId(ActTwo.RedactedMark))));

        Assert.Equal(6, whole);
        Assert.Equal(whole / 2, start - redacted.EnemyHealth);
    }

    // A lent volume is off the table for the fight's opening and handed back part-way through — Retaining, and
    // free for the turn it comes back on.
    [Fact]
    public void A_borrowed_volume_is_away_at_the_bell_and_back_at_round_two()
    {
        using var probe = ArchiveProbe.Enter(
            ["permit_a38", .. Cut], [("permit_a38", ActTwoEventObjects.BorrowersKeeping)], Markings);

        Assert.Contains(probe.Zone(CardZone.BanishedPile), c => c.DefinitionId.value == "permit_a38");
        Assert.DoesNotContain(probe.Zone(CardZone.Hand), c => c.DefinitionId.value == "permit_a38");

        probe.EndTurn();
        var lent = probe.InHand("permit_a38");
        Assert.Equal(0, probe.CostOf(lent));
        Assert.Contains(StandardCombatIds.RetainedCardMark, lent.Marks);
    }

    [Fact]
    public void A_reserved_volume_waits_one_round_longer()
    {
        using var probe = ArchiveProbe.Enter(
            ["permit_a38", .. Cut], [("permit_a38", ActTwoEventObjects.Reservation)], Markings);

        probe.EndTurn();
        Assert.DoesNotContain(probe.Zone(CardZone.Hand), c => c.DefinitionId.value == "permit_a38");
        probe.EndTurn();
        Assert.Equal(0, probe.CostOf(probe.InHand("permit_a38")));
    }

    // ── the five permanent inscriptions ───────────────────────────────────────────────────────────────────

    [Fact]
    public void An_authorized_revision_costs_one_more_and_does_half_again_as_much()
    {
        using var probe = ArchiveProbe.Enter(
            Cut, [("paper_cut", ActTwoEventObjects.AuthorizedRevision)],
            [ActTwoEventObjects.AuthorizedRevision], energy: 5);

        var revised = probe.Zone(CardZone.Hand)
            .First(c => c.Marks.Contains(new TagId(ActTwoEventObjects.AuthorizedRevision)));
        Assert.Equal(ArchiveProbe.Printed(revised.DefinitionId) + 1, probe.CostOf(revised));

        var before = probe.EnemyHealth;
        probe.Play_(revised);
        Assert.Equal(9, before - probe.EnemyHealth); // 6, and half again
    }

    [Fact]
    public void An_illuminated_initial_is_worth_a_card_and_three_block_the_first_time_it_is_played()
    {
        using var probe = ArchiveProbe.Enter(
            [.. Cut, .. Cut], [("paper_cut", ActTwoEventObjects.IlluminatedInitial)],
            [ActTwoEventObjects.IlluminatedInitial], energy: 5);

        var illuminated = probe.Zone(CardZone.Hand)
            .First(c => c.Marks.Contains(new TagId(ActTwoEventObjects.IlluminatedInitial)));
        var hand = probe.Zone(CardZone.Hand).Count;

        probe.Play_(illuminated);
        Assert.Equal(3, probe.Block);
        Assert.Equal(hand, probe.Zone(CardZone.Hand).Count); // one played, one drawn

        // Once each fight: an ordinary card played after it adds nothing.
        probe.Play_(probe.InHand("paper_cut"));
        Assert.Equal(3, probe.Block);
    }

    [Fact]
    public void A_concordant_pair_fetches_its_partner_to_the_top_of_the_draw_pile()
    {
        // Two cards carry the pair, and the hand is small enough that the partner is provably in the draw
        // pile — which is the only place the rule reaches into.
        using var probe = ArchiveProbe.Enter(
            [.. Cut, "permit_a38", "permit_a38", "permit_a38", "permit_a38", "permit_a38"],
            [("paper_cut", ActTwoEventObjects.ConcordantPair), ("permit_a38", ActTwoEventObjects.ConcordantPair)],
            [ActTwoEventObjects.ConcordantPair], energy: 5, drawnPerTurn: 2);

        var pair = new TagId(ActTwoEventObjects.ConcordantPair);
        var partnerInDraw = probe.Zone(CardZone.DrawPile).FirstOrDefault(c => c.Marks.Contains(pair));
        var played = probe.Zone(CardZone.Hand).FirstOrDefault(c => c.Marks.Contains(pair))
            ?? probe.Zone(CardZone.DrawPile).First(c => c.Marks.Contains(pair));
        if (partnerInDraw is null || !probe.Zone(CardZone.Hand).Contains(played))
        {
            // The seeded deal put both halves in the same place; draw until one is in hand.
            probe.EndTurn();
            partnerInDraw = probe.Zone(CardZone.DrawPile).FirstOrDefault(c => c.Marks.Contains(pair));
            played = probe.Zone(CardZone.Hand).First(c => c.Marks.Contains(pair));
        }
        Assert.NotNull(partnerInDraw);

        probe.Play_(played);
        Assert.Equal(partnerInDraw!.Id, probe.Zone(CardZone.DrawPile)[0].Id);
    }

    // "The first marker the archive puts on it is prevented" — undone at the next breath, once each fight.
    [Fact]
    public void A_card_that_knows_its_true_name_is_written_back_the_way_it_was()
    {
        using var probe = ArchiveProbe.Enter(
            Cut,
            [("paper_cut", ActTwoEventObjects.TrueName), ("paper_cut", ActTwo.RedactedMark)],
            [ActTwoEventObjects.TrueName, ActTwoEventObjects.ArchiveMarkings]);

        var named = new TagId(ActTwoEventObjects.TrueName);
        var redacted = new TagId(ActTwo.RedactedMark);

        // The archive wrote on it between fights; by the time the player looks at their hand it has been
        // written back — mark and halving both.
        var card = probe.Zone(CardZone.Hand).Concat(probe.Zone(CardZone.DrawPile))
            .Single(c => c.Marks.Contains(named));
        Assert.DoesNotContain(redacted, card.Marks);
        Assert.Equal(1, card.MarkCounters[StandardCombatIds.CardOutputScaleNumeratorCounter]);
        Assert.Equal(1, card.MarkCounters[StandardCombatIds.CardOutputScaleDenominatorCounter]);

        // …and it hits for its printed number, not for half of it.
        var before = probe.EnemyHealth;
        probe.Play_(probe.Zone(CardZone.Hand).First(c => c.Marks.Contains(named)));
        Assert.Equal(6, before - probe.EnemyHealth);
    }

    [Fact]
    public void A_late_bound_card_is_never_put_down_and_is_cheaper_the_second_turn_you_hold_it()
    {
        using var probe = ArchiveProbe.Enter(
            ["permit_a38", .. Cut], [("permit_a38", ActTwoEventObjects.LateBound)],
            [ActTwoEventObjects.LateBound], energy: 5);

        var bound = probe.InHand("permit_a38");
        Assert.Contains(StandardCombatIds.RetainedCardMark, bound.Marks);
        var printed = ArchiveProbe.Printed(bound.DefinitionId);
        Assert.Equal(printed, probe.CostOf(bound));

        probe.EndTurn();
        // Still held — and now it is worth waiting for.
        var held = probe.InHand("permit_a38");
        Assert.Equal(printed - 1, probe.CostOf(held));
    }

    // ── the Leaf ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_redacted_leaf_blacks_out_one_card_and_is_spent()
    {
        using var probe = ArchiveProbe.Enter(
            ["redacted_leaf", .. Cut], [], [ActTwoEventObjects.RedactedLeafRule.Id]);

        // The Leaf is drawn with the opening hand, so its work is done at the start of round 2.
        probe.EndTurn();

        Assert.Contains(probe.Zone(CardZone.ExhaustPile), c => c.DefinitionId.value == "redacted_leaf");
        Assert.Single(probe.Zone(CardZone.Hand), c => c.Marks.Contains(new TagId(ActTwo.RedactedMark)));
    }
}
