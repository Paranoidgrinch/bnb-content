using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The road's fifteen, played. Each test walks the door for real, takes one branch by name, and then looks at
// the fight the branch changed — and where the branch promised something for afterwards, wins that fight and
// asks the run.
//
// The fights behind these doors are Green Docket bodies wherever the branch is about the act's own vocabulary,
// because a Wergild is only settled by the act's customs, and the customs are what the act's opening hands
// the player.
public class ActThreeEventLiveTests
{
    private const string Road = "permit_hare";      // 66 HP; its third intent only raises Block
    private const string Quiet = "stamp_passage";   // …which is the intent every one of these fights is on

    private static readonly string[] Starter = ["permit_a38", "paper_cut", "paper_cut", "paper_cut"];

    private static readonly string[] Bigger =
        ["permit_a38", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"];

    private static int HeroStacks(EventStory story, string status) =>
        FightProbe.StacksOf(story.Fight.State.GetCombatant(story.Fight.HeroId), status);

    private static int EnemyStacks(EventStory story, string status) =>
        story.Fight.State.Combatants
            .Where(c => c.Id != story.Fight.HeroId)
            .Sum(c => FightProbe.StacksOf(c, status));

    private static bool Tagged(RunCardInstance card, string tag) =>
        card.Tags.Contains(new RunCardTagId(tag));

    private static int InDeck(EventStory story, string tag) =>
        story.Run.Deck.Count(c => Tagged(c, tag));

    private static EventStory OnTheRoad(
        string eventId, string choiceId, IReadOnlyList<string>? deck = null, int fights = 1,
        bool paying = false, int gold = 0, int? health = null) =>
        EventStory.Enter(eventId, choiceId, deck ?? Starter, fights, paying, gold,
            intent: Quiet, health: health, enemy: Road);

    // ── 1. A Clear Stream ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Washing_away_what_clings_takes_a_card_and_leaves_you_better()
    {
        using var story = OnTheRoad("a_clear_stream", "wash_away", health: 30);

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(30 + (70 * 5 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void Washing_one_thing_carefully_blesses_it_and_the_blessing_reaches_the_fight()
    {
        using var story = OnTheRoad("a_clear_stream", "wash_one", health: 30);

        Assert.Equal(1, InDeck(story, ActThreeEventObjects.RowanBlessed));
        Assert.Equal(30 + (70 * 10 + 99) / 100, story.Run.Health.Current);
        // The inscription is a rule of every fight from here on, so the rule is on the player in this one.
        Assert.Equal(1, HeroStacks(story, ActThreeEventObjects.RowanBlessed));
    }

    [Fact]
    public void The_bottled_water_rides_into_the_next_fight()
    {
        using var story = OnTheRoad("a_clear_stream", "bottle");

        Assert.Equal(1, HeroStacks(story, ActThreeEventObjects.BottledWaterId));
    }

    // ── 2. The Noticebound Hedge ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_lawful_gap_is_bought_and_paid_for()
    {
        using var story = OnTheRoad("the_noticebound_hedge", "lawful_gap", gold: 40);

        Assert.Equal(5, story.Run.Resources[StandardRunIds.Gold]);
        Assert.Equal(3, story.Run.Deck.Count);
    }

    [Fact]
    public void Crossing_first_pays_well_and_leaves_a_demand_on_the_road()
    {
        using var story = OnTheRoad("the_noticebound_hedge", "cross_first");

        Assert.Equal(90, story.Run.Resources[StandardRunIds.Gold]);
        Assert.Equal(2, HeroStacks(story, ActThreeEventObjects.EnvironmentalWergildId));
    }

    [Fact]
    public void The_hedge_knots_the_way()
    {
        using var story = OnTheRoad("the_noticebound_hedge", "mark_the_path");

        Assert.Equal(1, InDeck(story, ActThreeEventObjects.WayKnotted));
        Assert.Equal(1, HeroStacks(story, ActThreeEventObjects.WayKnotted));
    }

    // ── 3. The Witch at the Milestone ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_witchs_knot_is_tied_and_improved()
    {
        using var story = OnTheRoad("the_witch_at_the_milestone", "knot", Bigger);

        Assert.Equal(1, InDeck(story, ActThreeEventObjects.WayKnotted));
        Assert.Contains(story.Run.Deck, c => c.UpgradeLevel > 0);
    }

    [Fact]
    public void A_bad_memory_costs_a_card_and_some_of_you_and_pays_in_old_coin()
    {
        using var story = OnTheRoad("the_witch_at_the_milestone", "bad_memory");

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(66, story.Run.Health.Max);
        Assert.Equal(70, story.Run.Resources[StandardRunIds.Gold]);
    }

    [Fact]
    public void The_shortest_road_is_smaller_pays_nothing_and_ends_in_a_card()
    {
        using var story = OnTheRoad("the_witch_at_the_milestone", "shortest", Bigger, paying: true);

        // 20% of the Permit Hare's 66 is taken off at the opening bell.
        var hare = story.Fight.State.Combatants.Single(c => c.Id != story.Fight.HeroId);
        Assert.Equal(66 - 66 * 20 / 100, hare.Health.Current);

        story.WinTheFight();
        Assert.Equal(0, story.Run.Resources[StandardRunIds.Gold]); // the purse was garnished
    }

    // ── 4. The Public Footpath Dispute ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Declaring_a_public_right_covers_you_twice_and_pays_after_the_fight()
    {
        using var story = OnTheRoad("the_public_footpath_dispute", "declare", Bigger);

        Assert.Equal(3, HeroStacks(story, ActThree.SafeConductId)); // the act's one plus the door's two
        Assert.Equal(1, EnemyStacks(story, ActThree.ClaimId));

        story.WinTheFight();
        Assert.Equal(80, story.Run.Resources[StandardRunIds.Gold]);
    }

    [Fact]
    public void Recognizing_the_older_boundary_costs_a_card_and_five_of_you()
    {
        using var story = OnTheRoad("the_public_footpath_dispute", "older_boundary", health: 30);

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(25, story.Run.Health.Current);
    }

    [Fact]
    public void Mediating_costs_you_the_leave_you_came_with()
    {
        using var story = OnTheRoad("the_public_footpath_dispute", "mediate", Bigger);

        Assert.Equal(0, HeroStacks(story, ActThree.SafeConductId));
        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
    }

    // ── 5. Moonlit Mushrooms ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Stepping_inside_the_circle_costs_you_and_crowns_you()
    {
        using var story = OnTheRoad("moonlit_mushrooms", "step_inside", health: 60);

        Assert.Equal(60 - (70 * 8 + 99) / 100, story.Run.Health.Current);
        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "mootcap");
    }

    [Fact]
    public void Waiting_for_quorum_puts_you_under_a_count_that_only_ever_falls()
    {
        using var story = OnTheRoad("moonlit_mushrooms", "quorum", Bigger);

        Assert.Equal(1, HeroStacks(story, ActThreeEventObjects.QuorumVowId));
        var hero = story.Fight.State.GetCombatant(story.Fight.HeroId);
        Assert.Equal(1, hero.Counters[ActThreeEventObjects.QuorumHeld]);

        // Two real cards in one turn is neither one nor three, and the vow is broken for the fight.
        var hand = story.Zone(CardZone.Hand).Where(c => c.DefinitionId.value == "paper_cut").Take(2).ToList();
        var target = story.Fight.State.Combatants.First(c => c.Id != story.Fight.HeroId);
        foreach (var card in hand)
            story.Play.CombatDriver!.PlayCard(card.Id, target.Id);
        story.PassTurns(1);

        Assert.Equal(0, story.Fight.State.GetCombatant(story.Fight.HeroId).Counters[
            ActThreeEventObjects.QuorumHeld]);
    }

    // ── 6. A Spider's Clause ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reading_the_exception_costs_you_and_writes_an_older_right()
    {
        using var story = OnTheRoad("a_spiders_clause", "read_exception", health: 60);

        Assert.Equal(60 - (70 * 6 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(1, InDeck(story, ActThreeEventObjects.OldRightInscription));
    }

    [Fact]
    public void Cutting_through_the_clause_is_remembered_on_the_next_road()
    {
        using var story = OnTheRoad("a_spiders_clause", "cut_through");

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(1, HeroStacks(story, "doubt"));
    }

    [Fact]
    public void Signing_beneath_the_web_pays_a_hundred_and_gives_somebody_standing()
    {
        using var story = OnTheRoad("a_spiders_clause", "sign");

        Assert.Equal(100, story.Run.Resources[StandardRunIds.Gold]);
        Assert.Equal(1, EnemyStacks(story, ActThree.ClaimId));
    }

    // ── 7. The Ant Queue ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Waiting_your_turn_puts_two_things_in_order_and_rests_you()
    {
        using var story = OnTheRoad("the_ant_queue", "wait", Bigger, health: 30);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(30 + (70 * 10 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void Stepping_over_the_line_is_bitten_and_paid_for()
    {
        using var story = OnTheRoad("the_ant_queue", "step_over", health: 50);

        Assert.Equal(50 - (50 * 10 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(60, story.Run.Resources[StandardRunIds.Gold]);
    }

    [Fact]
    public void Walking_with_the_proper_line_puts_you_in_its_order()
    {
        using var story = OnTheRoad("the_ant_queue", "walk_with", Bigger);

        Assert.Equal(1, HeroStacks(story, ActThreeEventObjects.AntLineVowId));
    }

    // ── 8. The Conceptual Toll ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_toll_is_paid_at_the_door_and_the_stall_is_what_the_design_asks_for()
    {
        using var story = EventStory.AtTheDoor("the_conceptual_toll", Starter);
        story.Session.Pick("browse");
        Assert.Null(story.Session.Error);

        // Four cards, three relics, one removal and a way out — and nothing may be bought twice.
        var stock = FightProbe.Game.Events["the_conceptual_toll"].Situations["stock"].Choices;
        Assert.Equal(9, stock.Count);
        Assert.Equal(8, stock.Count(c => c.Requirement is not null));
    }

    [Fact]
    public void Disputing_the_crossing_pays_and_leaves_a_demand()
    {
        using var story = OnTheRoad("the_conceptual_toll", "dispute");

        Assert.Equal(85, story.Run.Resources[StandardRunIds.Gold]);
        Assert.Equal(2, HeroStacks(story, ActThreeEventObjects.EnvironmentalWergildId));
    }

    [Fact]
    public void Using_the_bridge_anyway_covers_you_twice_and_pays_in_cards()
    {
        using var story = OnTheRoad("the_conceptual_toll", "use_anyway", Bigger, paying: true);

        Assert.Equal(3, HeroStacks(story, ActThree.SafeConductId));
        story.WinTheFight();
        Assert.Equal(0, story.Run.Resources[StandardRunIds.Gold]);
    }

    // ── 9. Rain Beneath the Rowan ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Waiting_out_the_rain_is_the_best_rest_on_the_road()
    {
        using var story = OnTheRoad("rain_beneath_the_rowan", "wait", health: 20);

        Assert.Equal(20 + (70 * 30 + 99) / 100, story.Run.Health.Current);
    }

    [Fact]
    public void Walking_through_the_rain_costs_six_and_deals_one_more_for_two_fights()
    {
        using var story = OnTheRoad("rain_beneath_the_rowan", "keep_walking", Bigger, fights: 2, health: 30);

        Assert.Equal(24, story.Run.Health.Current);
        var first = story.Zone(CardZone.Hand).Count;
        story.WinTheFight();
        Assert.Equal(first, story.Zone(CardZone.Hand).Count);
    }

    // ── 10. The Buried Waystone ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Cleaning_the_inscription_sets_the_stones_watching()
    {
        using var story = OnTheRoad("the_buried_waystone", "clean");

        Assert.Equal(1, InDeck(story, ActThreeEventObjects.StoneWitnessed));
        Assert.Equal(1, HeroStacks(story, ActThreeEventObjects.StoneWitnessed));
    }

    [Fact]
    public void Burying_a_mark_beside_it_costs_a_card_and_five_and_pays_a_hundred()
    {
        using var story = OnTheRoad("the_buried_waystone", "bury");

        Assert.Equal(3, story.Run.Deck.Count);
        Assert.Equal(65, story.Run.Health.Max);
        Assert.Equal(100, story.Run.Resources[StandardRunIds.Gold]);
    }

    // ── 11. The Travelling Chandler ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_chandlers_cart_is_free_to_look_at_and_smaller_than_the_toll()
    {
        var stock = FightProbe.Game.Events["the_travelling_chandler"].Situations["stock"].Choices;

        Assert.Equal(6, stock.Count); // three cards, two relics, and a way out
        var browse = FightProbe.Game.Events["the_travelling_chandler"].Situations["start"].Choices
            .Single(c => c.Id == "browse");
        Assert.Null(browse.Costs);
    }

    [Fact]
    public void A_travellers_flame_opens_the_fight_wider()
    {
        using var story = OnTheRoad("the_travelling_chandler", "flame", Bigger, gold: 50);

        Assert.Equal(0, story.Run.Resources[StandardRunIds.Gold]);
        Assert.Equal(2, HeroStacks(story, ActThree.SafeConductId));
        // The pool is already full at the bell, so the flame's point is HELD and arrives when you run dry.
        Assert.Equal(1, HeroStacks(story, HeldEnergy.Id));
    }

    // ── 12. Stargazing ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_road_star_costs_four_and_covers_the_next_two_fights()
    {
        using var story = OnTheRoad("stargazing", "road_star", Bigger, fights: 2);

        Assert.Equal(66, story.Run.Health.Max);
        Assert.Equal(2, HeroStacks(story, ActThree.SafeConductId));
        story.WinTheFight();
        Assert.Equal(2, HeroStacks(story, ActThree.SafeConductId));
    }

    [Fact]
    public void The_root_star_improves_two_and_keeps_one_warm()
    {
        using var story = OnTheRoad("stargazing", "root_star", Bigger);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        Assert.Equal(1, InDeck(story, ActThreeEventObjects.HearthKept));
    }

    [Fact]
    public void The_hill_star_is_seen_looking()
    {
        using var story = OnTheRoad("stargazing", "hill_star");

        Assert.Equal(1, EnemyStacks(story, ActThree.ClaimId));
    }

    // ── 13. The Quiet Meadow ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Lying_in_the_grass_is_the_whole_of_what_the_meadow_offers()
    {
        using var story = OnTheRoad("the_quiet_meadow", "lie_down", health: 20);

        Assert.Equal(20 + (70 * 35 + 99) / 100, story.Run.Health.Current);
        Assert.Equal(4, story.Run.Deck.Count);
    }

    // ── 14. The Ombudsman's Warning ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_prepared_response_waits_through_the_ordinary_rooms()
    {
        using var story = OnTheRoad("the_ombudsmans_warning", "prepare", Bigger);

        Assert.Equal(2, story.Run.Deck.Count(c => c.UpgradeLevel > 0));
        // An ordinary fight is not what the leaf was about, so the licence is still waiting.
        Assert.Equal(1, HeroStacks(story, ActThree.SafeConductId));
    }

    [Fact]
    public void Your_own_complaint_buries_the_next_road_in_paperwork()
    {
        using var story = OnTheRoad("the_ombudsmans_warning", "complain", Bigger);

        Assert.Equal(1, EnemyStacks(story, "paperwork"));
        Assert.Equal(1, EnemyStacks(story, "doubt"));
        Assert.Equal(1, EnemyStacks(story, ActThree.ClaimId));

        story.WinTheFight();
        Assert.Equal(60, story.Run.Resources[StandardRunIds.Gold]);
    }

    [Fact]
    public void Keeping_the_leaf_costs_six_and_names_a_respondent()
    {
        using var story = OnTheRoad("the_ombudsmans_warning", "keep");

        Assert.Equal(64, story.Run.Health.Max);
        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "complaint_leaf");
    }

    // ── 15. The Kindly Procession ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Bowing_is_the_right_moment_and_lasts()
    {
        using var story = OnTheRoad("the_kindly_procession", "bow", health: 30);

        Assert.Equal(73, story.Run.Health.Max);
        // Healed a quarter of the seventy you had, and the three new points come with you.
        Assert.Equal(30 + (70 * 25 + 99) / 100 + 3, story.Run.Health.Current);
    }

    [Fact]
    public void Three_steps_is_guest_right_and_the_road_looks_after_its_guests()
    {
        using var story = OnTheRoad("the_kindly_procession", "three_steps");

        Assert.Equal(63, story.Run.Health.Max);
        Assert.Contains(story.Run.Relics, r => r.Definition.Id.Value == "guest_right_brooch");
        // The act's one, the door's two, and the brooch's own — it opens every fight with a licence.
        Assert.Equal(4, HeroStacks(story, ActThree.SafeConductId));
    }

    [Fact]
    public void Following_them_farther_is_paid_for_in_twelve_and_in_standing()
    {
        using var story = OnTheRoad("the_kindly_procession", "farther", Bigger, health: 30);

        Assert.Equal(58, story.Run.Health.Max);
        Assert.Equal(58, story.Run.Health.Current); // …and then made whole
        Assert.Equal(1, EnemyStacks(story, ActThree.ClaimId));
    }
}
