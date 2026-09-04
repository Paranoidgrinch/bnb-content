using BnbContent.Converter;
using BnbContent.Converter.Events;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The Labyrinth's twenty doors, played. Each test walks the door for real, takes one branch by name, and
// then looks at the fight the branch changed — and where the branch promised something for a stretch of
// road, wins the fight in between and asks the next one.
//
// The body behind these doors is an Ordinance Tablet on its quiet intent: it guards for ten and hits for
// nothing, so what a door wrote on the fight can be read off the opening without a corpse getting in the way.
public class ActFourEventLiveTests
{
    private const string Office = "ordinance_tablet";
    private const string Quiet = "stone_precedent";

    private static readonly string[] Papers = [.. Enumerable.Repeat("paper_cut", 6)];

    // `energy` is raised where a branch's own promise makes the fight expensive — Burdened puts a surcharge
    // on every card, and a probe that cannot afford a second card is measuring the surcharge, not the door.
    private static EventStory Door(
        string eventId, string choiceId, int fights = 1, int gold = 0, int? health = null, int energy = 3) =>
        EventStory.Enter(eventId, choiceId, Papers, fights, paying: false, gold,
            intent: Quiet, health: health, enemy: Office, energy: energy);

    private static int Hero(EventStory story, string status) =>
        FightProbe.StacksOf(story.Fight.State.GetCombatant(story.Fight.HeroId), status);

    private static int Enemies(EventStory story, string status) =>
        story.Fight.State.Combatants
            .Where(c => c.Id != story.Fight.HeroId)
            .Sum(c => FightProbe.StacksOf(c, status));

    private static bool Has(EventStory story, string relic) =>
        story.Run.Relics.Any(r => r.Definition.Id.Value == relic);

    private static int Gold(EventStory story) => story.Run.Resources[StandardRunIds.Gold];

    private static int Energy(EventStory story) =>
        story.Fight.State.GetCombatant(story.Fight.HeroId)
            .Resources[StandardCombatIds.EnergyResource].Current;

    // ── 1. The Dry Nilometer ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_true_level_costs_six_of_you_and_hands_over_the_cup()
    {
        using var story = Door("the_dry_nilometer", "accept_the_true_level");

        Assert.Equal(64, story.Run.Health.Max);
        Assert.True(Has(story, ActFourEventRelicRules.CupId));
        Assert.Equal(1, Hero(story, ActFourEventRelicRules.CupId));   // the rule is in the fight
    }

    [Fact]
    public void Moving_the_marker_pays_ninety_and_is_entered_for_two_rooms()
    {
        using var story = Door("the_dry_nilometer", "move_the_marker", fights: 2);

        Assert.Equal(90, Gold(story));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
    }

    [Fact]
    public void Leaving_unmeasured_heals_and_starts_the_next_office_from_the_beginning()
    {
        using var story = Door("the_dry_nilometer", "leave_unmeasured", health: 30);

        Assert.Equal(30 + (70 * 25 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(2, Hero(story, "paperwork"));
        Assert.Equal(3, Hero(story, ActFour.WeighedId));
    }

    // ── 2. The Black Granary ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Breaking_the_seal_pays_a_relic_and_is_carried_for_two_rooms()
    {
        using var story = Door("the_black_granary", "break_the_seal", fights: 2, energy: 5);

        Assert.Equal(130, Gold(story));
        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(2, Hero(story, ActFour.BurdenedId));
        story.WinTheFight();
        Assert.Equal(2, Hero(story, ActFour.BurdenedId));
    }

    [Fact]
    public void The_allotted_share_is_a_great_deal_of_food_and_a_little_less_of_you()
    {
        using var story = Door("the_black_granary", "accept_the_share", health: 20);

        Assert.Equal(65, story.Run.Health.Max);
        Assert.Equal(20 + (70 * 35 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void Restoring_the_record_improves_two_and_is_entered_once()
    {
        using var story = Door("the_black_granary", "restore_the_record");

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
    }

    // ── 3. The Red Linen Procession ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void Joining_the_procession_gives_one_card_to_the_wrapping()
    {
        using var story = Door("the_red_linen_procession", "join_the_procession", health: 30);

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(30 + (70 * 15 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(2, Hero(story, ActFour.EmbalmedId));
    }

    [Fact]
    public void Cutting_the_linen_corrects_two_and_closes_the_corridor()
    {
        using var story = Door("the_red_linen_procession", "cut_the_linen");

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(2, Hero(story, ActFour.EntombedId));
    }

    [Fact]
    public void Following_to_the_last_gate_costs_twelve_and_is_paid_in_linen()
    {
        using var story = Door("the_red_linen_procession", "follow_to_the_last_gate", health: 40);

        Assert.Equal(28, story.Run.Health.Current);
        Assert.True(Has(story, ActFourEventRelicRules.KnotId));
        Assert.Equal(1, Hero(story, ActFour.EmbalmedId));   // the knot's own opening
    }

    // ── 4. The Nameless Cartouche ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Inscribing_your_name_is_entered_for_three_rooms()
    {
        using var story = Door("the_nameless_cartouche", "inscribe_your_name", fights: 3);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
    }

    [Fact]
    public void Scraping_it_deeper_takes_a_card_and_seven_of_you()
    {
        using var story = Door("the_nameless_cartouche", "scrape_it_deeper");

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(63, story.Run.Health.Max);
    }

    [Fact]
    public void The_fragment_is_a_cartouche_with_nobody_in_it()
    {
        using var story = Door("the_nameless_cartouche", "take_the_fragment");

        Assert.True(Has(story, ActFourEventRelicRules.CartoucheId));
        Assert.Equal(1, Hero(story, ActFourEventRelicRules.CartoucheId));
    }

    // ── 5. The Forewritten Tablet ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Correcting_one_line_transforms_it_improves_it_and_pays_the_fee()
    {
        using var story = Door("the_forewritten_tablet", "correct_one_line");

        Assert.Equal(Papers.Length, story.Run.Deck.Count);
        Assert.Equal(1, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(50, Gold(story));
    }

    // ADAPTATION, proved: the door cannot open a fight, so it sets one on the road — the next ordinary
    // corridor is the three scribes', and the prize is paid only for winning it.
    [Fact]
    public void Demanding_the_tablet_sets_the_scribes_on_the_next_corridor()
    {
        using var story = Door("the_forewritten_tablet", "demand_the_tablet");

        Assert.Equal(2, Enemies(story, "strength"));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));

        var relics = story.Run.Relics.Count;
        story.WinTheFight();
        Assert.Equal(relics + 1, story.Run.Relics.Count);
    }

    [Fact]
    public void Signing_beneath_it_strikes_two_out_and_files_for_two_rooms()
    {
        using var story = Door("the_forewritten_tablet", "sign_beneath_it", fights: 2);

        Assert.Equal(Papers.Length - 2, story.Run.Deck.Count);
        Assert.Equal(3, Hero(story, "paperwork"));
        story.WinTheFight();
        Assert.Equal(3, Hero(story, "paperwork"));
    }

    // ── 6. The Tomb Robbers' Fire ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Trading_with_the_robbers_is_bought_and_paid_for()
    {
        using var story = Door("the_tomb_robbers_fire", "trade", gold: 100);

        Assert.Equal(30, Gold(story));
        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(1, Hero(story, ActThree.TrespassId));
    }

    [Fact]
    public void The_robbers_trade_is_not_offered_to_an_empty_purse()
    {
        using var story = EventStory.AtTheDoor("the_tomb_robbers_fire", Papers, enemy: Office, intent: Quiet);

        Assert.DoesNotContain(story.Session.PendingChoices, c => c.Id == "trade");
        Assert.Contains(story.Session.PendingChoices, c => c.Id == "join_the_opening");
    }

    [Fact]
    public void Joining_the_opening_sets_the_robbers_on_the_next_corridor_and_pays_the_take()
    {
        using var story = Door("the_tomb_robbers_fire", "join_the_opening");

        Assert.Equal(2, Enemies(story, "strength"));
        Assert.Equal(1, Hero(story, "panic"));

        var relics = story.Run.Relics.Count;
        story.WinTheFight();
        Assert.Equal(120, Gold(story));
        Assert.Equal(relics + 1, story.Run.Relics.Count);
    }

    [Fact]
    public void Stealing_from_the_thieves_is_looked_for_over_two_rooms()
    {
        using var story = Door("the_tomb_robbers_fire", "steal_from_the_thieves", fights: 2, energy: 5);

        Assert.Equal(100, Gold(story));
        Assert.Equal(1, Hero(story, "panic"));
        Assert.Equal(1, Hero(story, ActFour.BurdenedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, "panic"));
        Assert.Equal(1, Hero(story, ActFour.BurdenedId));
    }

    // ── 7. The Triple-Counted Donkey ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_first_tally_pays_seventy_five_and_the_other_tokens_are_carried()
    {
        using var story = Door("the_triple_counted_donkey", "honor_the_first_tally");

        Assert.Equal(75, Gold(story));
        Assert.Equal(1, Hero(story, ActFour.BurdenedId));
    }

    [Fact]
    public void Breaking_all_three_tokens_costs_blood_and_leaves_you_more_solid()
    {
        using var story = Door("the_triple_counted_donkey", "break_all_three", health: 40);

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(75, story.Run.Health.Max);
        Assert.Equal(40, story.Run.Health.Current);   // five out and five back: a bigger body, no better off
    }

    [Fact]
    public void Following_the_donkey_finds_water_and_something_left_behind()
    {
        using var story = Door("the_triple_counted_donkey", "follow_the_donkey", health: 30);

        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(30 + (70 * 10 + 99) / 100, story.Run.Health.Current);
    }

    // ── 8. The Four Canopic Jars ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_jar_of_breath_is_empty_and_that_is_the_point()
    {
        using var story = Door("the_four_canopic_jars", "jar_of_breath");

        Assert.True(Has(story, ActFourEventRelicRules.JarId));
        Assert.Equal(1, Hero(story, ActFourEventRelicRules.JarId));
    }

    [Fact]
    public void The_jar_of_blood_is_twelve_more_of_you_and_a_taste_of_it()
    {
        using var story = Door("the_four_canopic_jars", "jar_of_blood");

        Assert.Equal(82, story.Run.Health.Max);
        Assert.Equal(5, Hero(story, "poison"));
    }

    [Fact]
    public void The_jar_of_hunger_is_old_coin_that_wants_carrying()
    {
        using var story = Door("the_four_canopic_jars", "jar_of_hunger");

        Assert.Equal(150, Gold(story));
        Assert.Equal(2, Hero(story, ActFour.BurdenedId));
    }

    [Fact]
    public void The_jar_of_the_name_enters_three_under_somebody_elses()
    {
        using var story = Door("the_four_canopic_jars", "jar_of_the_name");

        Assert.Equal(3, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(2, Hero(story, ActFour.InscribedId));
    }

    // ── 9. The Chamber of False Measures ──────────────────────────────────────────────────────────────────

    [Fact]
    public void The_heavy_weight_makes_you_heavier_and_measures_you_twice()
    {
        using var story = Door("the_chamber_of_false_measures", "heavy_weight", fights: 2);

        Assert.Equal(80, story.Run.Health.Max);
        Assert.Equal(3, Hero(story, ActFour.WeighedId));
        story.WinTheFight();
        Assert.Equal(3, Hero(story, ActFour.WeighedId));
    }

    [Fact]
    public void The_light_weight_is_a_whole_body_and_eight_less_of_it()
    {
        using var story = Door("the_chamber_of_false_measures", "light_weight", health: 20);

        Assert.Equal(62, story.Run.Health.Max);
        Assert.Equal(62, story.Run.Health.Current);
    }

    [Fact]
    public void Breaking_the_scale_costs_fifteen_and_half_a_royal_weight_is_still_a_weight()
    {
        using var story = Door("the_chamber_of_false_measures", "break_the_scale", health: 40);

        Assert.Equal(25, story.Run.Health.Current);
        Assert.True(Has(story, ActFourEventRelicRules.WeightId));
    }

    // ── 10. The Crocodile at the Weighing Place ───────────────────────────────────────────────────────────

    [Fact]
    public void Offering_gold_is_an_arrangement_rather_than_a_weighing()
    {
        using var story = Door("the_crocodile_at_the_weighing_place", "offer_gold", gold: 80, health: 20);

        Assert.Equal(20, Gold(story));
        Assert.Equal(76, story.Run.Health.Max);
        Assert.Equal(20 + 6 + (76 * 15 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void Placing_yourself_on_the_scale_is_weighed_and_entered()
    {
        using var story = Door("the_crocodile_at_the_weighing_place", "place_yourself");

        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(3, Hero(story, ActFour.WeighedId));
        Assert.Equal(1, Hero(story, ActFour.EntombedId));
    }

    [Fact]
    public void Taking_the_offerings_is_noted_for_three_rooms()
    {
        using var story = Door("the_crocodile_at_the_weighing_place", "take_the_offerings", fights: 3);

        Assert.Equal(120, Gold(story));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
    }

    // ── 11. The Wall of Old Complaints ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Adding_your_own_complaint_corrects_two_and_is_answered_with_paperwork()
    {
        using var story = Door("the_wall_of_old_complaints", "add_your_own");

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(3, Hero(story, "paperwork"));
    }

    [Fact]
    public void Erasing_one_withdraws_it_and_costs_six_off_the_ceiling()
    {
        using var story = Door("the_wall_of_old_complaints", "erase_one");

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(64, story.Run.Health.Max);
    }

    [Fact]
    public void Reading_them_all_hands_over_the_chisel_and_two_rooms_of_doubt()
    {
        using var story = Door("the_wall_of_old_complaints", "read_them_all", fights: 2);

        Assert.True(Has(story, ActFourEventRelicRules.ChiselId));
        Assert.Equal(1, Hero(story, "doubt"));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, "doubt"));
    }

    // ── 12. The Copper Tithe ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Paying_the_tithe_takes_a_part_of_the_purse_and_stamps_two()
    {
        using var story = Door("the_copper_tithe", "pay_the_tithe", gold: 200);

        Assert.Equal(200 - (200 * 15 + 99) / 100, Gold(story));
        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
    }

    [Fact]
    public void Giving_more_than_required_takes_a_third_and_pays_out_of_the_bowl()
    {
        using var story = Door("the_copper_tithe", "give_more_than_required", gold: 200);

        Assert.Equal(200 - (200 * 35 + 99) / 100, Gold(story));
        Assert.NotEmpty(story.Run.Relics);
    }

    [Fact]
    public void Giving_nothing_keeps_the_purse_and_sends_the_bearer_down_the_corridor()
    {
        using var story = Door("the_copper_tithe", "give_nothing", gold: 200, energy: 5);

        Assert.Equal(200, Gold(story));                 // refusing costs nothing at the door
        Assert.Equal(2, Enemies(story, "strength"));
        Assert.Equal(1, Hero(story, ActFour.BurdenedId));

        story.WinTheFight();
        Assert.Equal(270, Gold(story));
    }

    // ── 13. The Unnamed Throne ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Restoring_the_name_costs_eight_and_hands_over_the_tablet()
    {
        using var story = Door("the_unnamed_throne", "restore_the_name");

        Assert.Equal(62, story.Run.Health.Max);
        Assert.True(Has(story, ActFourEventRelicRules.TabletId));
        Assert.Equal(1, Hero(story, ActFourEventRelicRules.TabletId));
    }

    [Fact]
    public void Erasing_it_completely_takes_two_of_yours_with_it()
    {
        using var story = Door("the_unnamed_throne", "erase_it_completely");

        Assert.Equal(Papers.Length - 2, story.Run.Deck.Count);
        Assert.Equal(2, Hero(story, "panic"));
    }

    [Fact]
    public void Taking_the_gold_leaf_is_paperwork_for_three_rooms()
    {
        using var story = Door("the_unnamed_throne", "take_the_gold_leaf", fights: 3);

        Assert.Equal(150, Gold(story));
        Assert.Equal(2, Hero(story, "paperwork"));
        story.WinTheFight();
        Assert.Equal(2, Hero(story, "paperwork"));
        story.WinTheFight();
        Assert.Equal(2, Hero(story, "paperwork"));
    }

    // ── 14. The Fixed-Day Festival ────────────────────────────────────────────────────────────────────────

    // The deck is all Deeds, so the Working half of the clause is the fallback the design writes in: with no
    // card of that category, another eligible one is used. Two cards come back improved either way.
    [Fact]
    public void Carrying_the_standard_honours_one_of_each_and_falls_back_when_a_category_is_absent()
    {
        using var story = Door("the_fixed_day_festival", "carry_the_standard", energy: 5);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(1, Hero(story, ActFour.BurdenedId));
    }

    // The drum's "+1 Energy" is a Spare Hand: a pool an opening gives into has just been refilled to its
    // maximum, so what the design means by the extra Energy is one more card out of the turn.
    [Fact]
    public void Beating_the_drum_opens_two_rooms_in_a_hurry()
    {
        using var story = Door("the_fixed_day_festival", "beat_the_drum", fights: 2);

        Assert.Equal(1, Hero(story, ActFourEventRelicRules.SpareId));
        Assert.Equal(1, Hero(story, "panic"));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFourEventRelicRules.SpareId));
        Assert.Equal(1, Hero(story, "panic"));
    }

    [Fact]
    public void Waiting_for_the_correct_star_is_rest_bought_with_forty_gold()
    {
        using var story = Door("the_fixed_day_festival", "wait_for_the_correct_star", gold: 100, health: 20);

        Assert.Equal(20 + (70 * 40 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(60, Gold(story));
    }

    // ── 15. The Broken Sluice ─────────────────────────────────────────────────────────────────────────────

    // "Lose up to 50 Gold" — a purse with less in it than that is emptied, not overdrawn.
    [Fact]
    public void Opening_the_sluice_washes_you_clean_and_the_purse_out()
    {
        using var story = Door("the_broken_sluice", "open_it", gold: 30, health: 20);

        Assert.Equal(20 + (70 * 25 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(0, Gold(story));
    }

    [Fact]
    public void Closing_it_properly_is_an_hour_on_your_knees_and_carried_into_the_next_room()
    {
        using var story = Door("the_broken_sluice", "close_it_properly", energy: 5);

        Assert.Equal(78, story.Run.Health.Max);
        Assert.Equal(1, Hero(story, ActFour.BurdenedId));
    }

    [Fact]
    public void Rerouting_the_channel_corrects_two_and_is_measured_for_two_rooms()
    {
        using var story = Door("the_broken_sluice", "reroute_the_channel", fights: 2);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(2, Hero(story, ActFour.WeighedId));
        story.WinTheFight();
        Assert.Equal(2, Hero(story, ActFour.WeighedId));
    }

    // ── 16. The Unfinished Burial ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Finishing_the_wrapping_takes_one_of_yours_and_leaves_the_spool()
    {
        using var story = Door("the_unfinished_burial", "finish_the_wrapping");

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.True(Has(story, ActFourEventRelicRules.CoilId));
    }

    [Fact]
    public void Taking_the_amulet_begins_the_next_fight_preserved_and_half_buried()
    {
        using var story = Door("the_unfinished_burial", "take_the_amulet");

        Assert.NotEmpty(story.Run.Relics);
        Assert.Equal(3, Hero(story, ActFour.EmbalmedId));
        Assert.Equal(1, Hero(story, ActFour.EntombedId));
    }

    [Fact]
    public void Unwrapping_the_name_turns_two_into_something_else_for_two_rooms()
    {
        using var story = Door("the_unfinished_burial", "unwrap_the_name", fights: 2);

        Assert.Equal(Papers.Length, story.Run.Deck.Count);
        Assert.Equal(Papers.Length - 2, story.Run.Deck.Count(c => c.DefinitionId.value == "paper_cut"));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.InscribedId));
    }

    // ── 17. The Survey of the Dead ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Being_counted_among_the_living_is_a_whole_body_with_a_lower_ceiling()
    {
        using var story = Door("the_survey_of_the_dead", "be_counted_among_the_living", health: 20);

        Assert.Equal(62, story.Run.Health.Max);
        Assert.Equal(62, story.Run.Health.Current);
    }

    [Fact]
    public void Being_counted_among_the_dead_is_wrapped_properly_for_three_rooms()
    {
        using var story = Door("the_survey_of_the_dead", "be_counted_among_the_dead", fights: 3);

        Assert.Equal(82, story.Run.Health.Max);
        Assert.Equal(1, Hero(story, ActFour.EmbalmedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.EmbalmedId));
        story.WinTheFight();
        Assert.Equal(1, Hero(story, ActFour.EmbalmedId));
    }

    [Fact]
    public void Refusing_the_count_puts_three_clerks_on_the_next_corridor_and_pays_for_beating_them()
    {
        using var story = Door("the_survey_of_the_dead", "refuse_the_count");

        Assert.Equal(3, Enemies(story, "strength"));
        Assert.Equal(1, Hero(story, ActFour.InscribedId));

        story.WinTheFight();
        Assert.Equal(90, Gold(story));
        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(1, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
    }

    // ── 18. The House of Life at Night ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Copying_a_formula_leaves_you_with_two_of_it_and_paperwork_about_the_second()
    {
        using var story = Door("the_house_of_life_at_night", "copy_a_formula");

        Assert.Equal(Papers.Length + 1, story.Run.Deck.Count);
        Assert.Equal(2, Hero(story, "paperwork"));
    }

    [Fact]
    public void Erasing_a_formula_takes_a_line_out_of_the_world()
    {
        using var story = Door("the_house_of_life_at_night", "erase_a_formula");

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(75, story.Run.Health.Max);
    }

    [Fact]
    public void Replacing_a_line_writes_two_others_in_the_better_hand()
    {
        using var story = Door("the_house_of_life_at_night", "replace_a_line");

        Assert.Equal(Papers.Length, story.Run.Deck.Count);
        Assert.Equal(Papers.Length - 2, story.Run.Deck.Count(c => c.DefinitionId.value == "paper_cut"));
        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(2, Hero(story, ActFour.InscribedId));
    }

    // ── 19. The Merciful Balance ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Gold_on_the_pan_buys_a_thing_struck_from_the_file()
    {
        using var story = Door("the_merciful_balance", "place_gold_on_the_pan", gold: 100);

        Assert.Equal(25, Gold(story));
        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
    }

    // A pan that cannot be paid is not offered at all — the branch's cost is its requirement.
    [Fact]
    public void An_empty_purse_is_not_shown_the_golden_pan()
    {
        using var door = EventStory.AtTheDoor("the_merciful_balance", Papers, gold: 10);

        Assert.DoesNotContain(door.Session.PendingChoices, c => c.Id == "place_gold_on_the_pan");
        Assert.Contains(door.Session.PendingChoices, c => c.Id == "place_blood_on_the_pan");
    }

    [Fact]
    public void Blood_on_the_pan_is_ten_off_the_ceiling_and_two_put_in_order()
    {
        using var story = Door("the_merciful_balance", "place_blood_on_the_pan");

        Assert.Equal(60, story.Run.Health.Max);
        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
    }

    [Fact]
    public void Your_burden_on_the_pan_buys_the_counterweight_and_a_heavier_room()
    {
        using var story = Door("the_merciful_balance", "place_your_burden_on_the_pan", energy: 5);

        Assert.True(Has(story, ActFourEventRelicRules.MercyId));
        Assert.Equal(2, Hero(story, ActFour.BurdenedId));
        Assert.Equal(1, Hero(story, ActFour.EntombedId));
    }

    // ── 20. Cartouche Repair Bench ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Restoring_a_name_at_the_bench_is_slow_work_and_a_restful_afternoon()
    {
        using var story = Door("the_cartouche_repair_bench", "restore_the_name", health: 20);

        Assert.Equal(1, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(20 + (70 * 15 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void Replacing_a_name_pays_fifty_out_of_the_drawer()
    {
        using var story = Door("the_cartouche_repair_bench", "replace_the_name");

        Assert.Equal(Papers.Length, story.Run.Deck.Count);
        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count(c => c.DefinitionId.value == "paper_cut"));
        Assert.Equal(50, Gold(story));
    }

    [Fact]
    public void Leaving_no_name_sets_the_register_looking_for_what_should_be_there()
    {
        using var story = Door("the_cartouche_repair_bench", "leave_no_name");

        Assert.Equal(Papers.Length - 1, story.Run.Deck.Count);
        Assert.Equal(2, Hero(story, ActFour.InscribedId));
    }

    // ── the stretch itself ────────────────────────────────────────────────────────────────────────────────

    // A promise about three rooms is three rooms and not four: the last link steps down after it is kept.
    [Fact]
    public void A_three_room_promise_stops_at_three()
    {
        using var story = Door("the_nameless_cartouche", "inscribe_your_name", fights: 4);

        story.WinTheFight();
        story.WinTheFight();
        story.WinTheFight();
        Assert.Equal(0, Hero(story, ActFour.InscribedId));
    }

    // …and it survives being written to disk between rooms, because what is still owed IS the name of the
    // program still installed.
    [Fact]
    public void A_stretch_survives_a_save_between_rooms()
    {
        using var parked = EventStory.EnterAndPark(
            "the_dry_nilometer", "move_the_marker", Papers, fights: 2, intent: Quiet, enemy: Office);

        using var resumed = parked.SaveAndResume();
        resumed.Settle();

        Assert.Equal(1, Hero(resumed, ActFour.InscribedId));
        resumed.WinTheFight();
        Assert.Equal(1, Hero(resumed, ActFour.InscribedId));   // the second link came back with the save
    }
}
