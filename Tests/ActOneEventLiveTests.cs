using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The fifteen, played. Each test walks the door for real, takes one branch by name, and then looks at the fight
// the branch changed — and where the branch promised something for afterwards, wins that fight and asks the run.
public class ActOneEventLiveTests
{
    private static readonly string[] Starter = ["permit_a38", "paper_cut", "paper_cut", "paper_cut"];

    private static int Marked(EventStory story, CardZone zone, string mark) =>
        story.Zone(zone).Count(card => card.Marks.Contains(new TagId(mark)));

    private static int HeroStacks(EventStory story, string status) =>
        FightProbe.StacksOf(story.Fight.State.GetCombatant(story.Fight.HeroId), status);

    // ── 1. The Misfiling Cabinet ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_cabinet_refiles_a_card_pays_fifty_gold_and_misfiles_another()
    {
        using var story = EventStory.Enter("misfiling_cabinet", "refile", Starter);

        Assert.Equal(50, story.Run.GetResource(StandardRunIds.Gold));
        Assert.DoesNotContain(story.Run.Deck, c => c.DefinitionId.value == "permit_a38"); // refiled away
        Assert.Single(story.Run.Deck, c => c.Tags.Contains(new RunCardTagId(ActOneEventObjects.Misfiled)));
        // …and the fight it changed opened with that card already filed in the wrong place.
        Assert.Equal(1, Marked(story, CardZone.DiscardPile, ActOneEventObjects.Misfiled));
    }

    [Fact]
    public void Pulling_the_file_free_removes_a_card_and_shuffles_two_forms_into_the_fight()
    {
        using var story = EventStory.Enter("misfiling_cabinet", "pull", Starter);

        Assert.Equal(3, story.Run.Deck.Count);
        var everywhere = story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)).ToList();
        Assert.Contains(everywhere, c => c.DefinitionId.value == "missing_signature");
        Assert.Contains(everywhere, c => c.DefinitionId.value == "wrong_form");
    }

    // ── 6. The Complaint Ledger ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_formal_complaint_strikes_a_card_and_buys_one_refusal()
    {
        using var story = EventStory.Enter("complaint_ledger", "complain", Starter);

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.AdministrativeExemption));
    }

    [Fact]
    public void Signing_as_a_witness_pays_a_card_and_puts_a_witness_in_the_fight()
    {
        using var story = EventStory.Enter("complaint_ledger", "witness", Starter);

        Assert.Equal(5, story.Run.Deck.Count); // the four it started with plus the reward pick
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.WitnessedProcedure));
    }

    // ── 7. The Waiting Token Exchange ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Trading_your_place_in_line_improves_a_card_and_calls_your_number_first()
    {
        using var story = EventStory.Enter("waiting_token_exchange", "place_in_line", Starter);

        Assert.Contains(story.Run.Deck, c => c.UpgradeLevel > 0);
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.PriorityNumber));
        // The notice is waiting in the opening hand, and the extra Energy is held rather than lost to a full
        // pool — the two extra cards are already drawn.
        Assert.Contains(story.Zone(CardZone.Hand), c => c.DefinitionId.value == "notice_of_delay");
        Assert.Equal(1, HeroStacks(story, "held_energy"));
    }

    [Fact]
    public void Trading_three_hours_of_waiting_buys_the_ticket_and_a_late_opening()
    {
        using var story = EventStory.Enter("waiting_token_exchange", "three_hours", Starter);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "uncalled_ticket");
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.RestrictedPublicHours));
    }

    // ── 8. The Almost-Helpful Clerk ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_helpful_stamp_puts_the_stamped_card_in_hand_and_makes_that_play_free()
    {
        using var story = EventStory.Enter("almost_helpful_clerk", "stamp", Starter);

        var stamped = story.Zone(CardZone.Hand)
            .Single(c => c.Marks.Contains(new TagId(ActOneEventObjects.Stamped)));
        Assert.True(EventStory.CostOf(stamped.DefinitionId) + EventStory.CostDeltaOf(stamped) <= 0,
            "the stamped card should cost nothing the first time it is played");
        Assert.Contains(story.Zone(CardZone.Hand), c => c.DefinitionId.value == "missing_signature");
    }

    // ── 8b / 14a. The Expedited Route ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_corrected_route_meets_a_diminished_enemy_and_is_never_paid_for_it()
    {
        using var story = EventStory.Enter("almost_helpful_clerk", "route", Starter, paying: true);

        var rat = story.Fight.State.Combatants.Single(c => c.Id != story.Fight.HeroId);
        Assert.Equal(11, rat.Health.Max);
        Assert.Equal(8, rat.Health.Current);           // 30% of its own maximum, taken at the bell
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.ExpeditedRoute));

        story.WinTheFight();
        Assert.Equal(0, story.Run.GetResource(StandardRunIds.Gold)); // the purse was garnished on arrival
    }

    // ── 4 / 15. Under Review, and what the archive stamps on the way back ─────────────────────────────────

    [Fact]
    public void A_card_left_for_identification_sits_out_the_fight_and_comes_back_improved()
    {
        using var story = EventStory.Enter("lost_and_found_desk", "identify", Starter);

        Assert.Contains(story.Zone(CardZone.BanishedPile), c => c.DefinitionId.value == "permit_a38");
        Assert.DoesNotContain(story.Zone(CardZone.Hand), c => c.DefinitionId.value == "permit_a38");

        story.WinTheFight();
        var permit = story.Card("permit_a38");
        Assert.Equal(1, permit.UpgradeLevel);
        Assert.DoesNotContain(new RunCardTagId(ActOneEventObjects.UnderReview), permit.Tags);
    }

    [Fact]
    public void A_method_submitted_for_preservation_comes_back_stamped_and_stays_cheaper()
    {
        using var story = EventStory.Enter("archive_window", "method", Starter, fights: 2);

        Assert.Contains(story.Zone(CardZone.BanishedPile), c => c.DefinitionId.value == "permit_a38");
        story.WinTheFight();

        var permit = story.Card("permit_a38");
        Assert.Equal(1, permit.UpgradeLevel);
        Assert.Contains(new RunCardTagId(ActOneEventObjects.CertifiedOriginal), permit.Tags);
        Assert.DoesNotContain(new RunCardTagId(ActOneEventObjects.UnderReview), permit.Tags);

        // …and the stamp is permanent: the NEXT fight prices it a point below what it prints.
        var certified = story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile))
            .Single(c => c.Marks.Contains(new TagId(ActOneEventObjects.CertifiedOriginal)));
        Assert.Equal(-1, EventStory.CostDeltaOf(certified));
    }

    [Fact]
    public void The_old_tool_comes_with_fine_print_that_taxes_the_first_card_of_the_turn()
    {
        using var story = EventStory.Enter("archive_window", "tool", Starter);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "inherited_bone_folder");
        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "fine_print");
    }

    // ── 3. The Self-Amending Fee Table ────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_fee_waiver_pays_now_and_the_audit_collects_afterwards()
    {
        // A fight the rat gets a bite in — an audit of a fight that cost nothing collects nothing.
        using var story = EventStory.Enter("self_amending_fee_table", "waiver",
            ["paper_cut", "cower_behind_a_desk"], intent: "many_small_bites");

        Assert.Equal(75, story.Run.GetResource(StandardRunIds.Gold));
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.AuditNotice));

        var health = story.Run.Health.Current;
        story.WinTheFight();
        var lost = story.Run.Health.Max - story.Run.Health.Current;
        Assert.True(lost > 0, "the rat should have got a bite in");
        Assert.Equal(Math.Max(0, 75 - Math.Min(4 * lost, 80)), story.Run.GetResource(StandardRunIds.Gold));
    }

    [Fact]
    public void The_comprehensive_fee_is_only_offered_to_someone_who_can_pay_it()
    {
        using (var poor = EventStory.AtTheDoor("self_amending_fee_table", Starter, gold: 149))
            Assert.DoesNotContain(poor.Session.PendingChoices, c => c.Id == "pay");

        using var rich = EventStory.Enter("self_amending_fee_table", "pay", Starter, gold: 150);
        Assert.Equal(0, rich.Run.GetResource(StandardRunIds.Gold));
        Assert.Equal(2, rich.Run.Deck.Count(c => c.UpgradeLevel > 0));
    }

    // ── 10. The Sealed Back Door ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Breaking_the_seal_costs_a_summons_and_pays_an_extra_card_after_the_fight()
    {
        using var story = EventStory.Enter("sealed_back_door", "break", Starter);

        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "summons_to_appear");

        var before = story.Run.Deck.Count;
        story.WinTheFight();
        Assert.Equal(before + 1, story.Run.Deck.Count);
    }

    [Fact]
    public void Respecting_the_seal_buys_a_ward_and_warns_the_opposition()
    {
        using var story = EventStory.Enter("sealed_back_door", "respect", Starter);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "threshold_ward");
        var rat = story.Fight.State.Combatants.Single(c => c.Id != story.Fight.HeroId);
        Assert.Equal(4, FightProbe.StacksOf(rat, "strength"));
    }

    // ── 13. Receipt of Prior Effort ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_performance_claim_pays_the_full_rate_for_a_fight_finished_early()
    {
        using var story = EventStory.Enter("receipt_of_prior_effort", "claim", Starter);

        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.ReceiptOfPriorEffort));
        story.WinTheFight();
        Assert.Equal(125, story.Run.GetResource(StandardRunIds.Gold));
    }

    // ── 14. The Contradictory Map ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Folding_the_map_wrong_buys_a_shortcut_and_two_wrong_forms()
    {
        using var story = EventStory.Enter("contradictory_map", "fold", Starter, fights: 2);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "crossed_out_map");
        Assert.True(story.Run.UnrestrictedSteps > 0, "the map should be worth one step off the paths");
        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "wrong_form");

        story.WinTheFight();
        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "wrong_form");
    }

    // ── the markings are spent where they are honoured ────────────────────────────────────────────────────

    [Fact]
    public void A_marking_is_read_by_one_fight_and_then_it_is_over()
    {
        using var story = EventStory.Enter("misfiling_cabinet", "refile", Starter, fights: 2);

        Assert.Equal(1, Marked(story, CardZone.DiscardPile, ActOneEventObjects.Misfiled));
        story.WinTheFight();

        Assert.DoesNotContain(story.Run.Deck,
            c => c.Tags.Contains(new RunCardTagId(ActOneEventObjects.Misfiled)));
        Assert.Equal(0, Marked(story, CardZone.DiscardPile, ActOneEventObjects.Misfiled));
    }

    // ── 2. The Certified Copy Drawer ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_certified_duplicate_is_worth_one_real_card_and_one_notice()
    {
        using var story = EventStory.Enter("certified_copy_drawer", "duplicate", Starter);

        Assert.Equal(6, story.Run.Deck.Count);                       // four, plus the copy, plus the notice
        Assert.Equal(2, story.Run.Deck.Count(c => c.DefinitionId.value == "permit_a38"));
        Assert.Contains(story.Run.Deck, c => c.DefinitionId.value == "duplicate_copy");
    }

    [Fact]
    public void The_certified_instrument_seals_a_card_away_until_the_third_round()
    {
        using var story = EventStory.Enter("certified_copy_drawer", "instrument",
            ["permit_a38", "cower_behind_a_desk", "cower_behind_a_desk", "cower_behind_a_desk"]);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "originality_stamp");
        Assert.Contains(story.Zone(CardZone.BanishedPile), c => c.DefinitionId.value == "permit_a38");

        // Rounds 1 and 2 pass without it; the seal opens at the third.
        story.PassTurns(2);
        Assert.Contains(story.Zone(CardZone.Hand), c => c.DefinitionId.value == "permit_a38");
    }

    // ── 9. The Witness Queue ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_first_witness_produces_a_relic_and_two_notices()
    {
        using var story = EventStory.Enter("witness_queue", "first", Starter);

        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(2, story.Run.Deck.Count(c => c.DefinitionId.value == "duplicate_copy"));
    }

    [Fact]
    public void The_second_witness_costs_a_statement_and_sends_a_summons()
    {
        using var story = EventStory.Enter("witness_queue", "second", Starter);

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Contains(story.Zone(CardZone.Hand), c => c.DefinitionId.value == "summons_to_appear");
    }

    [Fact]
    public void Cross_examining_all_three_buys_protection_and_a_watchful_witness()
    {
        using var story = EventStory.Enter("witness_queue", "cross_examine", Starter);

        Assert.Equal(5, story.Run.Deck.Count);
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.WitnessProtection));
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.WitnessedProcedure));
        Assert.Equal(10, story.Fight.State.GetCombatant(story.Fight.HeroId)
            .DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    // ── 11. The Clerk's Tea Break ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_lukewarm_tea_is_worth_a_fifth_of_what_you_have_left_to_lose()
    {
        using var story = EventStory.Enter("clerks_tea_break", "tea", Starter, health: 20);
        var max = story.Run.Health.Max;

        Assert.Equal(20 + (max * 20 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void The_abandoned_notes_improve_a_card_and_authorise_overtime()
    {
        using var story = EventStory.Enter("clerks_tea_break", "notes", Starter);

        Assert.Contains(story.Run.Deck, c => c.UpgradeLevel > 0);
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.AuthorizedOvertime));
    }

    // ── 12. The Friendly Filing Cabinet ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Alphabetizing_the_deck_files_one_card_away_for_good()
    {
        using var story = EventStory.Enter("friendly_filing_cabinet", "alphabetize", Starter);

        Assert.Equal(3, story.Run.Deck.Count);
    }

    [Fact]
    public void A_better_form_is_the_NEW_card_and_it_is_waiting_in_the_opening_hand()
    {
        using var story = EventStory.Enter("friendly_filing_cabinet", "better_form", Starter);

        Assert.Equal(4, story.Run.Deck.Count);
        Assert.DoesNotContain(story.Run.Deck, c => c.DefinitionId.value == "permit_a38");
        var fastTracked = Assert.Single(story.Run.Deck,
            c => c.Tags.Contains(new RunCardTagId(ActOneEventObjects.FastTrack)));
        Assert.Contains(story.Zone(CardZone.Hand), c => c.DefinitionId == fastTracked.DefinitionId);
    }

    // ── 5. The Licensed Vendor ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_vendor_sells_each_thing_once_and_only_to_someone_who_can_pay()
    {
        using var story = EventStory.AtTheDoor("licensed_vendor", Starter, gold: 200);
        story.Session.Pick("browse");

        var counter = story.Session.PendingChoices;
        Assert.Contains(counter, c => c.Id == "card-0");
        Assert.Contains(counter, c => c.Id == "removal");
        Assert.Contains(counter, c => c.Id == "leave");

        var deck = story.Run.Deck.Count;
        var purse = story.Run.GetResource(StandardRunIds.Gold);
        story.Session.Pick("card-0");
        Assert.Null(story.Session.Error);

        Assert.Equal(deck + 1, story.Run.Deck.Count);
        Assert.True(story.Run.GetResource(StandardRunIds.Gold) < purse, "the card should have been paid for");
        // Bought is sold out, and the counter stays open.
        Assert.DoesNotContain(story.Session.PendingChoices, c => c.Id == "card-0");
        Assert.Contains(story.Session.PendingChoices, c => c.Id == "leave");
    }

    [Fact]
    public void The_sealed_sample_is_paid_for_out_of_the_next_fights_purse()
    {
        using var story = EventStory.Enter("licensed_vendor", "sample", Starter, paying: true);

        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.GarnishedReward));
        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "fine_print");

        story.WinTheFight();
        Assert.Equal(0, story.Run.GetResource(StandardRunIds.Gold));
    }

    // ── the branches nothing above walks, and a net under all of them ─────────────────────────────────────

    [Fact]
    public void An_unlabelled_parcel_is_worth_a_tag_and_an_unsigned_form()
    {
        using var story = EventStory.Enter("lost_and_found_desk", "claim", Starter);

        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "unclaimed_property_tag");
        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "missing_signature");
    }

    [Fact]
    public void The_receipt_can_simply_be_redeemed()
    {
        using var story = EventStory.Enter("receipt_of_prior_effort", "redeem", Starter);

        Assert.Equal(75, story.Run.GetResource(StandardRunIds.Gold));
    }

    [Fact]
    public void The_annotated_corridor_pays_a_card_opens_one_counter_a_round_and_costs_a_wrong_form()
    {
        using var story = EventStory.Enter("contradictory_map", "annotated", Starter);

        Assert.Equal(5, story.Run.Deck.Count);
        Assert.Equal(1, HeroStacks(story, ActOneEventObjects.CorrectWindow));
        Assert.Contains(
            story.Zone(CardZone.Hand).Concat(story.Zone(CardZone.DrawPile)),
            c => c.DefinitionId.value == "wrong_form");
    }

    // Every branch of every one of the fifteen, walked: the door opens, the branch resolves, the run reaches
    // the fight behind it. A promise naming something the document does not hold fails HERE, at the moment it
    // is installed, rather than in a session nobody is watching.
    [Theory]
    [MemberData(nameof(EveryBranch))]
    public void Every_branch_of_every_event_can_actually_be_taken(string eventId, string choiceId)
    {
        using var story = EventStory.Enter(eventId, choiceId, Starter, gold: 400);

        Assert.Null(story.Session.Error);
        Assert.NotNull(story.Play.CombatDriver?.Current);
    }

    public static TheoryData<string, string> EveryBranch()
    {
        var data = new TheoryData<string, string>();
        foreach (var id in ActOneEventTests.Fifteen)
            foreach (var choice in FightProbe.Game.Events[id].Situations["start"].Choices)
                data.Add(id, choice.Id);
        return data;
    }

    [Fact]
    public void A_claim_submitted_and_then_dragged_out_pays_the_lower_rate()
    {
        using var story = EventStory.Enter("receipt_of_prior_effort", "claim",
            ["paper_cut", "cower_behind_a_desk"], intent: "many_small_bites");

        story.PassTurns(3);   // the third round goes by unfinished
        story.WinTheFight();
        Assert.Equal(25, story.Run.GetResource(StandardRunIds.Gold));
    }

    // A promise made at an event is part of the run, so it has to survive being written to disk and read back
    // — the "Continue" path. A program whose body the document does not hold fails loudly on restore.
    [Fact]
    public void A_pending_promise_survives_a_save_and_is_still_kept()
    {
        using var parked = EventStory.EnterAndPark("self_amending_fee_table", "waiver", Starter);
        Assert.Contains(parked.Run.InstalledPrograms,
            p => p.Id.Value == ActOneEventPrograms.AuditNotice);

        using var resumed = parked.SaveAndResume();
        Assert.Contains(resumed.Run.InstalledPrograms,
            p => p.Id.Value == ActOneEventPrograms.AuditNotice);

        resumed.Settle();
        Assert.Equal(1, HeroStacks(resumed, ActOneEventObjects.AuditNotice));
        resumed.WinTheFight();
        var lost = resumed.Run.Health.Max - resumed.Run.Health.Current;
        Assert.Equal(Math.Max(0, 75 - Math.Min(4 * lost, 80)),
            resumed.Run.GetResource(StandardRunIds.Gold));
    }
}
