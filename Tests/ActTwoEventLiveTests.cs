using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The archives' fifteen, played. Each test walks the door for real, takes one branch by name, and then looks at
// the fight the branch changed — and where the branch promised something for afterwards, wins that fight and
// asks the run. Nothing here writes state into the run: a run is rebuilt from its own answers under the replay
// model, so anything set from outside is set away again on the next replay.
public class ActTwoEventLiveTests
{
    private static readonly string[] Starter = ["permit_a38", "paper_cut", "paper_cut", "paper_cut"];

    // A deck that does not fit in one hand. The opening draw empties a four-card pile, and an empty pile is
    // reshuffled from the discard — so anything a door FILED somewhere is dealt straight back into the hand and
    // the test cannot see where it was put. Six cards leave the pile something to draw from.
    private static readonly string[] Bigger =
        ["permit_a38", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"];

    private static int HeroStacks(EventStory story, string status) =>
        FightProbe.StacksOf(story.Fight.State.GetCombatant(story.Fight.HeroId), status);

    private static bool Tagged(RunCardInstance card, string tag) =>
        card.Tags.Contains(new RunCardTagId(tag));

    private static int InDeck(EventStory story, string tag) =>
        story.Run.Deck.Count(c => Tagged(c, tag));

    // What rarity the authored pool says a card is — the archives' doors ask for Rare and Uncommon by name.
    private static string RarityOf(string cardId) =>
        Converter.Cards.FinalCards.All().First(c => c.Id == cardId).Rarity;

    // ── 1. Misfiled Prophecy ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Correcting_the_filing_code_refiles_one_card_and_misfiles_another()
    {
        using var story = EventStory.Enter("misfiled_prophecy", "correct", Bigger);

        Assert.DoesNotContain(story.Run.Deck, c => c.DefinitionId.value == "permit_a38"); // transformed away
        Assert.Equal(1, InDeck(story, ActTwo.MisfiledMark));
        // …and the fight it changed took that card straight back out of the opening hand, drawing a
        // replacement for it. The regulations clear the mark as they take it, so what is left is the CARD,
        // filed where nobody asked for it.
        Assert.Equal(1, HeroStacks(story, ActTwo.ArchiveRegulationsId));
        Assert.NotEmpty(story.Zone(CardZone.DiscardPile));
    }

    [Fact]
    public void Accepting_the_prophecy_authorizes_a_revision_and_files_an_unfinished_citation()
    {
        using var story = EventStory.Enter("misfiled_prophecy", "accept", Bigger);

        Assert.Equal(1, InDeck(story, ActTwoEventObjects.AuthorizedRevision));
        Assert.Contains(story.Zone(CardZone.DiscardPile),
            c => c.DefinitionId.value == "unfinished_citation");

        // The inscription is a rule of EVERY fight from now on, and it prices the copy it is written on: the
        // first play costs 1 more and is worth half again as much.
        var inscribed = story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile))
            .Single(c => c.Marks.Contains(new TagId(ActTwoEventObjects.AuthorizedRevision)));
        Assert.Equal(1, EventStory.CostDeltaOf(inscribed));
        Assert.Equal(3, inscribed.MarkCounters[StandardCombatIds.CardOutputScaleNumeratorCounter]);
        Assert.Equal(2, inscribed.MarkCounters[StandardCombatIds.CardOutputScaleDenominatorCounter]);
    }

    // ── 2. The Self-Correcting Index ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Allowing_the_correction_improves_two_cards_and_blacks_one_of_them_out()
    {
        using var story = EventStory.Enter("self_correcting_index", "allow", Starter);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(1, InDeck(story, ActTwo.RedactedMark));
        // A redaction written between fights arrives as a mark; the markings rule finishes it by halving the copy.
        var redacted = story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile))
            .Single(c => c.Marks.Contains(new TagId(ActTwo.RedactedMark)));
        Assert.Equal(1, redacted.MarkCounters[StandardCombatIds.CardOutputScaleNumeratorCounter]);
        Assert.Equal(2, redacted.MarkCounters[StandardCombatIds.CardOutputScaleDenominatorCounter]);
    }

    [Fact]
    public void Correcting_the_index_yourself_strikes_one_card_and_misfiles_two()
    {
        using var story = EventStory.Enter("self_correcting_index", "correct_yourself", Starter);

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(2, InDeck(story, ActTwo.MisfiledMark));
        Assert.Equal(1, HeroStacks(story, ActTwo.ArchiveRegulationsId));
    }

    // ── 3. The Locked Reading Room ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reading_under_supervision_pays_a_rare_card_and_is_read_over_your_shoulder()
    {
        using var story = EventStory.Enter("locked_reading_room", "supervised", Starter);

        var gained = story.Run.Deck.Single(c => !Starter.Contains(c.DefinitionId.value));
        Assert.Equal("rare", RarityOf(gained.DefinitionId.value));
        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.FourthCard));
    }

    [Fact]
    public void Copying_the_illuminated_passage_costs_forty_gold_and_illuminates_one_card()
    {
        using var story = EventStory.Enter("locked_reading_room", "copy", Starter, gold: 40);

        Assert.Equal(0, story.Run.GetResource(StandardRunIds.Gold));
        Assert.Equal(1, InDeck(story, ActTwoEventObjects.IlluminatedInitial));
    }

    [Fact]
    public void Waiting_outside_in_silence_heals_a_fifth()
    {
        using var story = EventStory.Enter("locked_reading_room", "wait", Starter, health: 30);

        Assert.Equal(30 + (70 * 20 + 99) / 100, story.Run.Health.Current);
    }

    // ── 4. The Perpetual Borrower ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_lent_volume_sits_out_the_fight_and_comes_back_improved()
    {
        using var story = EventStory.Enter("perpetual_borrower", "lend", Starter);

        // Lent means gone: the copy leaves the table at the opening bell…
        Assert.Contains(story.Zone(CardZone.BanishedPile), c => c.DefinitionId.value == "permit_a38");
        // …and comes back at round 2, retained and free for that turn.
        story.PassTurns(1);
        var returned = story.Zone(CardZone.Hand).Single(c => c.DefinitionId.value == "permit_a38");
        Assert.True(EventStory.CostOf(returned.DefinitionId) + EventStory.CostDeltaOf(returned) <= 0);

        story.WinTheFight();
        var permit = story.Card("permit_a38");
        Assert.Equal(1, permit.UpgradeLevel);
        Assert.False(Tagged(permit, ActTwoEventObjects.BorrowersKeeping));
    }

    [Fact]
    public void The_borrowers_notes_are_uncommon_and_come_with_a_claim_slip()
    {
        using var story = EventStory.Enter("perpetual_borrower", "notes", Starter);

        var gained = story.Run.Deck.Single(c => !Starter.Contains(c.DefinitionId.value));
        Assert.Equal("uncommon", RarityOf(gained.DefinitionId.value));
        Assert.Contains(story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "borrowers_claim");
    }

    [Fact]
    public void Pocketing_the_library_card_costs_maximum_health_and_lends_you_out_too()
    {
        using var story = EventStory.Enter("perpetual_borrower", "pocket", Starter);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "unreturned_library_card");
        Assert.Equal(64, story.Run.Health.Max); // 70 less eight per cent of it
    }

    // ── 5. The Reciprocal Shelf ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Arguing_with_the_classification_pays_a_card_and_misfiles_a_blacked_out_one()
    {
        using var story = EventStory.Enter("reciprocal_shelf", "argue", Starter);

        Assert.Equal(5, story.Run.Deck.Count);
        var caught = story.Run.Deck.Single(c => Tagged(c, ActTwo.MisfiledMark));
        Assert.True(Tagged(caught, ActTwo.RedactedMark), "the shelf blacks out what it reclassifies");
    }

    [Fact]
    public void The_shelf_label_misfiles_something_in_each_of_the_next_two_fights()
    {
        using var story = EventStory.Enter("reciprocal_shelf", "label", Starter, fights: 2);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "reversible_shelf_label");
        Assert.Equal(1, InDeck(story, ActTwo.MisfiledMark));

        story.WinTheFight();
        // The first misfiling was spent by the fight that honoured it; a second is written for the next one.
        Assert.Equal(1, InDeck(story, ActTwo.MisfiledMark));
        Assert.Equal(1, HeroStacks(story, ActTwo.ArchiveRegulationsId));

        story.WinTheFight();
        Assert.Equal(0, InDeck(story, ActTwo.MisfiledMark)); // …and no third
    }

    // ── 6. The Margin Notes ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Following_both_arguments_binds_two_cards_into_a_pair()
    {
        using var story = EventStory.Enter("margin_notes", "both", Starter);

        Assert.Equal(2, InDeck(story, ActTwoEventObjects.ConcordantPair));
        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.ConcordantPair));
    }

    [Fact]
    public void Scraping_the_margin_improves_a_card_and_sends_a_leaf_of_ink_after_you()
    {
        using var story = EventStory.Enter("margin_notes", "scrape", Starter);

        Assert.Contains(story.Run.Deck, c => c.UpgradeLevel > 0);
        Assert.Equal(1, HeroStacks(story, "redacted_leaf_rule"));
        // The leaf is read as it is HELD, so a deck small enough to be dealt whole reads it at the opening
        // bell: one card in hand comes out half-erased and the leaf itself is spent.
        Assert.Contains(story.Zone(CardZone.ExhaustPile), c => c.DefinitionId.value == "redacted_leaf");
        Assert.Contains(story.Zone(CardZone.Hand), c => c.Marks.Contains(new TagId(ActTwo.RedactedMark)));
    }

    // ── 7. Unclaimed Reservation ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_reserved_volume_is_not_on_the_table_until_round_three()
    {
        using var story = EventStory.Enter("unclaimed_reservation", "claim_volume", Starter);

        var claimed = story.Run.Deck.Single(c => Tagged(c, ActTwoEventObjects.Reservation));
        Assert.Contains(story.Zone(CardZone.BanishedPile),
            c => c.DefinitionId.value == claimed.DefinitionId.value);

        story.PassTurns(2);
        var arrived = story.Zone(CardZone.Hand).Single(c => c.DefinitionId.value == claimed.DefinitionId.value);
        Assert.True(EventStory.CostOf(arrived.DefinitionId) + EventStory.CostDeltaOf(arrived) <= 0);
    }

    [Fact]
    public void Entering_another_name_pays_seventy_gold_and_registers_a_card_to_somebody_else()
    {
        using var story = EventStory.Enter("unclaimed_reservation", "register", Starter);

        Assert.Equal(70, story.Run.GetResource(StandardRunIds.Gold));

        // One card in the opening hand is registered elsewhere, and nobody can pay what it now costs.
        var registered = story.Zone(CardZone.Hand)
            .Single(c => c.Marks.Contains(new TagId(ActTwoEventObjects.RegisteredMark)));
        Assert.Equal(9, EventStory.CostDeltaOf(registered));

        // Filing anything else releases the register.
        var other = story.Zone(CardZone.Hand).First(c => c.Id != registered.Id
            && c.DefinitionId.value == "paper_cut");
        story.Play.CombatDriver!.PlayCard(other.Id,
            story.Fight.State.Combatants.First(c => c.Id != story.Fight.HeroId).Id);
        Assert.Null(story.Session.Error);

        var freed = story.Zone(CardZone.Hand).Single(c => c.Id == registered.Id);
        Assert.Equal(0, EventStory.CostDeltaOf(freed));
        Assert.DoesNotContain(new TagId(ActTwoEventObjects.RegisteredMark), freed.Marks);
    }

    // ── 8. The Infinite Return Slot ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void Returning_a_bad_idea_pays_forty_gold_and_the_slot_remembers_it()
    {
        using var story = EventStory.Enter("infinite_return_slot", "return", Starter);

        Assert.Equal(40, story.Run.GetResource(StandardRunIds.Gold));
        Assert.Equal(3, story.Run.Deck.Count);
        var remembered = Assert.Single(story.Run.RemovedCards);
        Assert.Equal("permit_a38", remembered.Definition.value);
    }

    [Fact]
    public void Reaching_for_a_lost_page_with_nothing_lost_yet_still_hands_you_a_claim_slip()
    {
        using var story = EventStory.Enter("infinite_return_slot", "reach", Starter);

        Assert.Equal(4, story.Run.Deck.Count); // an empty history gives nothing back
        Assert.Contains(story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "borrowers_claim");
    }

    // ── 9. The Redacted Portrait ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Restoring_the_missing_face_costs_a_hundred_gold_and_buys_the_cameo()
    {
        using var story = EventStory.Enter("redacted_portrait", "restore", Starter, gold: 100);

        Assert.Equal(0, story.Run.GetResource(StandardRunIds.Gold));
        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "blank_cameo");
    }

    [Fact]
    public void The_absent_name_is_written_on_one_of_your_own_cards()
    {
        using var story = EventStory.Enter("redacted_portrait", "absent_name", Starter);

        Assert.Equal(1, InDeck(story, ActTwoEventObjects.TrueName));
        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.TrueName));
    }

    // ── 10. The Lost-Hour Bottle ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Drinking_the_lost_hour_holds_an_energy_now_and_takes_two_back_later()
    {
        using var story = EventStory.Enter("lost_hour_bottle", "drink", Starter);

        Assert.Equal(1, HeroStacks(story, "held_energy"));
        story.PassTurns(1);
        Assert.Equal(2, HeroStacks(story, "held_energy"));
        story.PassTurns(1);
        // The third round is when the hour is noticed missing, and the rule is done.
        Assert.Equal(0, HeroStacks(story, ActTwoEventObjects.LostHour));
    }

    [Fact]
    public void Binding_the_hour_makes_a_card_late_bound()
    {
        using var story = EventStory.Enter("lost_hour_bottle", "bind", Starter);

        Assert.Equal(1, InDeck(story, ActTwoEventObjects.LateBound));
        var bound = story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile))
            .Single(c => c.Marks.Contains(new TagId(ActTwoEventObjects.LateBound)));
        Assert.Contains(StandardCombatIds.RetainedCardMark, bound.Marks);
    }

    // ── 11. The Necrology Window ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_borrowed_life_waits_for_an_ordinary_fight_then_refuses_to_end()
    {
        using var story = EventStory.Enter(
            "necrology_window", "borrow", Starter, paying: true, health: 30);

        Assert.Equal(30 + (70 * 35 + 99) / 100, story.Run.Health.Current);

        // The window's loan landed on the fight that was walked into, on the largest body there.
        var body = story.Fight.State.Combatants.Single(c => c.Id != story.Fight.HeroId);
        Assert.Equal(1, FightProbe.StacksOf(body, ActTwoEventObjects.UnfinishedLife));

        story.WinTheFight();
        // 30 for the fight's own purse, 75 for the department's paperwork.
        Assert.Equal(105, story.Run.GetResource(StandardRunIds.Gold));
    }

    [Fact]
    public void Closing_an_abandoned_account_is_not_offered_when_it_would_be_lethal()
    {
        using var dying = EventStory.AtTheDoor("necrology_window", Starter, health: 8);
        Assert.DoesNotContain(dying.Session.PendingChoices, c => c.Id == "close");

        using var story = EventStory.Enter("necrology_window", "close", Starter, health: 30);
        Assert.Equal(22, story.Run.Health.Current);
        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Contains(story.Run.Deck, c => c.UpgradeLevel > 0);
    }

    // ── 12. The Almost-Helpful Clerk, Reassigned ──────────────────────────────────────────────────────────

    [Fact]
    public void The_whispered_amendment_sticks_only_if_it_is_filed_while_still_blacked_out()
    {
        using var story = EventStory.Enter("almost_helpful_clerk_reassigned", "amendment", Starter);

        var amended = story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile))
            .Single(c => c.Marks.Contains(new TagId(ActTwoEventObjects.WhisperedAmendment)));
        Assert.Contains(new TagId(ActTwo.RedactedMark), amended.Marks);

        story.WinTheFight();
        // The fight was won by playing Paper Cuts, so the amendment was never filed: nothing sticks, and the
        // redaction was one fight's business.
        var card = story.Run.Deck.Single(c => c.DefinitionId.value == "permit_a38");
        Assert.Equal(0, card.UpgradeLevel);
        Assert.False(Tagged(card, ActTwo.RedactedMark));
        Assert.False(Tagged(card, ActTwoEventObjects.WhisperedAmendment));
    }

    [Fact]
    public void The_readers_pass_undoes_the_first_thing_the_archive_writes_on_you()
    {
        using var story = EventStory.Enter("almost_helpful_clerk_reassigned", "pass", Starter);

        Assert.Equal(35, story.Run.GetResource(StandardRunIds.Gold));
        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.ReadersPass));
    }

    // ── 13. The Last Quiet Table ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Keeping_the_vow_through_a_won_fight_earns_the_bead()
    {
        using var story = EventStory.Enter("last_quiet_table", "vow", Starter);

        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.Vow));
        story.WinTheFight(); // the rat falls to two Paper Cuts — three is the limit, and two is under it
        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "vow_bead");
    }

    [Fact]
    public void The_forbidden_volume_is_rare_and_comes_with_ink_in_your_opening_hand()
    {
        using var story = EventStory.Enter("last_quiet_table", "forbidden", Starter);

        var gained = story.Run.Deck.Single(c => !Starter.Contains(c.DefinitionId.value));
        Assert.Equal("rare", RarityOf(gained.DefinitionId.value));
        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.FourthCard));
        // The leaf is read as it is held: one card in the opening hand comes out half-erased and the leaf is gone.
        Assert.DoesNotContain(story.Zone(CardZone.Hand), c => c.DefinitionId.value == "redacted_leaf");
        Assert.Contains(story.Zone(CardZone.Hand),
            c => c.MarkCounters.TryGetValue(StandardCombatIds.CardOutputScaleDenominatorCounter, out var d)
                && d == 2);
    }

    // ── 14. The Inward Seal ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Breaking_the_seal_outward_buys_the_sealstone_and_catches_two_cards()
    {
        using var story = EventStory.Enter("inward_seal", "outward", Starter);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "inverted_sealstone");
        Assert.Equal(2, InDeck(story, ActTwo.MisfiledMark));
        Assert.Equal(2, InDeck(story, ActTwo.RedactedMark));
    }

    [Fact]
    public void Pressing_the_seal_into_your_skin_adds_maximum_health_and_opens_in_paperwork()
    {
        using var story = EventStory.Enter("inward_seal", "skin", Starter);

        Assert.Equal(78, story.Run.Health.Max);
        Assert.Equal(2, HeroStacks(story, "paperwork"));
        Assert.Equal(1, HeroStacks(story, "doubt"));
    }

    // ── 15. The Librarian at the End of the Aisle ─────────────────────────────────────────────────────────

    [Fact]
    public void With_nothing_forgotten_the_librarian_hands_over_a_rare_card_instead()
    {
        using var story = EventStory.Enter("librarian_at_the_end_of_the_aisle", "forgotten_book", Starter);

        var gained = story.Run.Deck.Single(c => !Starter.Contains(c.DefinitionId.value));
        Assert.Equal("rare", RarityOf(gained.DefinitionId.value));
    }

    [Fact]
    public void Asking_the_librarian_to_forget_a_volume_strikes_it_and_heals()
    {
        using var story = EventStory.Enter(
            "librarian_at_the_end_of_the_aisle", "forget", Starter, health: 30);

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Single(story.Run.RemovedCards);
        Assert.Equal(30 + (70 * 15 + 99) / 100, story.Run.Health.Current);
    }

    // ── the promises survive being put down ───────────────────────────────────────────────────────────────

    // A promise that is still WAITING when the run is written to disk is the one most likely to be lost: the
    // shortest path has not chosen its fight yet, and the return slot's history is a list of cards nobody
    // holds. Both have to be there when the save is picked back up.
    [Fact]
    public void A_waiting_promise_and_the_removed_history_ride_through_a_save()
    {
        using var parked = EventStory.EnterAndPark(
            "librarian_at_the_end_of_the_aisle", "shortest", Starter);
        using var resumed = parked.SaveAndResume();
        resumed.Settle();

        var rat = resumed.Fight.State.Combatants.Single(c => c.Id != resumed.Fight.HeroId);
        Assert.Equal(9, rat.Health.Current);
        Assert.Equal(1, HeroStacks(resumed, ActTwoEventObjects.ShortestPath));

        using var forgotten = EventStory.EnterAndPark("infinite_return_slot", "return", Starter);
        using var back = forgotten.SaveAndResume();
        var remembered = Assert.Single(back.Run.RemovedCards);
        Assert.Equal("permit_a38", remembered.Definition.value);
    }

    [Fact]
    public void The_shortest_path_meets_a_diminished_enemy_pays_nothing_and_owes_you_a_card()
    {
        using var story = EventStory.Enter(
            "librarian_at_the_end_of_the_aisle", "shortest", Starter, paying: true);

        var rat = story.Fight.State.Combatants.Single(c => c.Id != story.Fight.HeroId);
        Assert.Equal(11, rat.Health.Max);
        Assert.Equal(9, rat.Health.Current); // a quarter of its own maximum, taken at the bell
        Assert.Equal(1, HeroStacks(story, ActTwoEventObjects.ShortestPath));

        story.WinTheFight();
        Assert.Equal(0, story.Run.GetResource(StandardRunIds.Gold)); // the purse was garnished on arrival
        Assert.Equal(5, story.Run.Deck.Count);                       // …and one procedure was waiting
    }
}
