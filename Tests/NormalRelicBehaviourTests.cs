using RogueDeck.Core.Combat;
using static BnbContent.Tests.RelicBench;

namespace BnbContent.Tests;

// THE GENERAL NORMAL RELICS, EACH HELD TO ITS OWN TEXT (relic audit of 2026-11-20). Every test sets up the one
// situation the text names, does it, and reads what changed — and where the text has a limit ("the first time each
// turn", "once per combat", "an enemy") a second test does it again and expects nothing. A relic that installs and
// fires but does the wrong thing fails here; a relic that "passes the fight" without doing anything fails here too.
public class NormalRelicBehaviourTests
{
    private const string Block5 = "cower_behind_a_desk";   // 1: Gain 5 Block
    private const string Cut = "paper_cut";                // 1: Deal 6 damage (a Deed)
    private const string Pin = "poppet_pin";               // 0: Deal 4 damage (a Deed)
    private const string Stolen = "stolen_hour";           // 0: Gain 2 Energy. Exhaust.
    private const string Heavy = "weight_of_paper";        // 2: a Deed
    private const string Door = "barred_door";             // 2: Retain. Gain 14 Block.

    // ── Common ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sun_warmed_waystone_guards_the_turn_that_ends_with_energy_to_spare()
    {
        using (var d = Arm("sun_warmed_waystone", Times(Block5, 10), roster: [(Biter, BiteIntent, null)]))
        {
            var health = d.Health;
            d.End();                                   // 3 Energy unspent: 5 Block meets the bite of 6
            Assert.Equal(health - 1, d.Health);
        }
        using var e = Arm("sun_warmed_waystone", Times(Block5, 10), energy: 0, roster: [(Biter, BiteIntent, null)]);
        var before = e.Health;
        e.End();                                       // no Energy left: the whole bite
        Assert.Equal(before - 6, e.Health);
    }

    [Fact]
    public void Conservators_thread_guards_when_a_card_leaves_the_hand_unplayed()
    {
        // Discarded mid-turn by a card: the Block comes then, once.
        using (var d = Arm("conservators_thread", ["scattered_notes", "scattered_notes", Block5, Block5, Block5, .. Times(Block5, 10)]))
        {
            d.P("scattered_notes", pick: Block5);
            Assert.Equal(4, d.Block);
            d.P("scattered_notes", pick: Block5);
            Assert.Equal(4, d.Block);                  // the same turn: no second
        }
        // The hand thrown away at the turn's end: the Block is there for the enemy's blow.
        using (var d = Arm("conservators_thread", Times(Block5, 10), roster: [(Biter, BiteIntent, null)]))
        {
            var health = d.Health;
            d.End();
            Assert.Equal(health - 2, d.Health);
        }
        // Everything played: nothing left the hand unplayed.
        using var e = Arm("conservators_thread", Times(Block5, 10), energy: 5, roster: [(Biter, BiteIntent, null)]);
        for (var i = 0; i < 5; i++) e.P(Block5);
        Assert.Equal(25, e.Block);
        var hp = e.Health;
        e.End();
        Assert.Equal(hp, e.Health);
        Assert.Equal(0, e.Block);
    }

    [Fact]
    public void Witness_slate_adds_one_to_the_first_liable_of_each_turn()
    {
        using var d = Arm("witness_slate", Times("named_in_the_roll", 15), energy: 3);
        d.P("named_in_the_roll");
        Assert.Equal(3, d.StacksOn(d.Enemy, "liable"));
        d.P("named_in_the_roll");
        Assert.Equal(5, d.StacksOn(d.Enemy, "liable"));
        d.End();
        var before = d.StacksOn(d.Enemy, "liable");
        d.P("named_in_the_roll");
        Assert.Equal(before + 3, d.StacksOn(d.Enemy, "liable"));
    }

    [Fact]
    public void Lead_counterweight_keeps_a_card_costing_two_and_lets_the_rest_go()
    {
        // Every heavy card in hand stays and five more come; light cards go as always.
        using (var d = Arm("lead_counterweight", Times(Heavy, 20)))
        {
            d.End();
            Assert.Equal(10, d.Hand);
        }
        using var e = Arm("lead_counterweight", Times(Block5, 20));
        e.End();
        Assert.Equal(5, e.Hand);
    }

    [Fact]
    public void Hollow_wax_bead_counts_free_cards_only_and_pays_once_a_turn()
    {
        // Three cards costing 1: not a free card among them.
        using (var d = Arm("hollow_wax_bead", Times(Cut, 15), energy: 9))
        {
            d.P(Cut); d.P(Cut); d.P(Cut);
            Assert.Equal(2, d.Hand);
        }
        // Six free cards in one turn: one card drawn, not two.
        using var e = Arm("hollow_wax_bead", Times(Pin, 15), energy: 3, roster: [(Quiet, QuietIntent, 200)]);
        e.P(Pin); e.P(Pin);
        Assert.Equal(3, e.Hand);
        e.P(Pin);
        Assert.Equal(3, e.Hand);                       // the third free card drew one
        e.P(Pin); e.P(Pin); e.P(Pin);
        Assert.Equal(0, e.Hand);                       // the sixth: not again this turn
    }

    [Fact]
    public void Binders_awl_pays_an_energy_and_a_card_with_the_first_draw_only()
    {
        using var d = Arm("binders_awl", Times(Block5, 20));
        Assert.Equal(6, d.Hand);
        Assert.Equal(1, d.Stacks(HeldEnergyId));
        d.End();
        Assert.Equal(5, d.Hand);
        Assert.Equal(1, d.Stacks(HeldEnergyId));       // still the one, not a second
    }

    [Fact]
    public void Iron_prayer_bead_strengthens_the_first_deed_at_an_attacker_and_only_the_first()
    {
        using (var d = Arm("iron_prayer_bead", Times(Cut, 15), roster: [(Biter, BiteIntent, 200)]))
        {
            var health = d.HealthOf(d.Enemy);
            Assert.True(health == 200, "nothing should have struck before a card is played\n" + d.Log);
            d.P(Cut);
            Assert.Equal(health - 10, d.HealthOf(d.Enemy));
            d.P(Cut);
            Assert.Equal(health - 16, d.HealthOf(d.Enemy));
            d.End();
            health = d.HealthOf(d.Enemy);
            d.P(Cut);                                  // a new turn: the first Deed again
            Assert.Equal(health - 10, d.HealthOf(d.Enemy));
        }
        // An enemy that does not intend to attack: nothing.
        using var e = Arm("iron_prayer_bead", Times(Cut, 15), roster: [(Quiet, QuietIntent, 200)]);
        var before = e.HealthOf(e.Enemy);
        e.P(Cut);
        Assert.Equal(before - 6, e.HealthOf(e.Enemy));
    }

    [Fact]
    public void Ash_pan_pays_once_when_the_fifth_card_is_exhausted()
    {
        // Against the same five burnt without it: one card more, one Held Energy more — and the sixth adds nothing.
        (int Hand, int Held) Burn(string? relic, int burnt)
        {
            using var d = Arm(relic, Times(Stolen, 7), energy: 3);
            for (var i = 0; i < burnt; i++) d.P(Stolen);
            return (d.Hand, d.Stacks(HeldEnergyId));
        }
        Assert.Equal(Burn(null, 4), Burn("ash_pan", 4));
        var (hand, held) = Burn(null, 5);
        Assert.Equal((hand + 1, held + 1), Burn("ash_pan", 5));

    }

    [Fact]
    public void Tarnished_bell_strikes_your_first_debuff_each_turn_and_nothing_else()
    {
        using (var d = Arm("tarnished_bell", Times("seed_of_misgiving", 15), roster: [(Biter, BiteIntent, 200)]))
        {
            d.P("seed_of_misgiving");
            Assert.Equal(196, d.HealthOf(d.Enemy));
            d.P("seed_of_misgiving");
            Assert.Equal(196, d.HealthOf(d.Enemy));
            d.End();
            d.P("seed_of_misgiving");
            Assert.Equal(192, d.HealthOf(d.Enemy));
        }
        // A neutral mark that another relic puts on an attacking enemy is no debuff of yours.
        using var e = Arm("tarnished_bell", Times(Block5, 15), roster: [(Biter, BiteIntent, 200)], more: ["iron_prayer_bead"]);
        Assert.Equal(200, e.HealthOf(e.Enemy));
        e.End();
        Assert.Equal(200, e.HealthOf(e.Enemy));
    }

    [Fact]
    public void Bruise_cup_answers_the_first_enemy_blow_each_turn_and_not_your_own_price()
    {
        // The biter strikes first: 6, and the cup's 4 Block meets the constable's two blows of 5.
        using (var d = Arm("bruise_cup", Times(Block5, 15), roster: [(Biter, BiteIntent, null), (Twice, TwiceIntent, null)]))
        {
            var health = d.Health;
            d.End();
            Assert.True(health - 12 == d.Health, $"{health}->{d.Health}");
        }
        using (var d = Arm(null, Times(Block5, 15), roster: [(Biter, BiteIntent, null), (Twice, TwiceIntent, null)]))
        {
            var health = d.Health;
            d.End();
            Assert.Equal(health - 16, d.Health);
        }
        // Blood-Price Token's 3 HP are your own doing: no Block for them.
        using var e = Arm("bruise_cup", Times(Block5, 15), more: ["blood_price_token"]);
        Assert.Equal(0, e.Block);
    }

    // ── Uncommon ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Emergency_inkwell_refills_one_energy_once_a_combat()
    {
        using var d = Arm("emergency_inkwell", Times(Block5, 20), energy: 3);
        d.P(Block5); d.P(Block5); d.P(Block5);
        Assert.Equal(1, d.Energy);
        d.P(Block5);
        Assert.Equal(0, d.Energy);
        d.End();
        d.P(Block5); d.P(Block5); d.P(Block5);
        Assert.Equal(0, d.Energy);
    }

    [Fact]
    public void Ashen_wax_knife_draws_for_the_first_card_exhausted_each_turn()
    {
        using var d = Arm("ashen_wax_knife", Times(Stolen, 12));
        d.P(Stolen);
        Assert.Equal(5, d.Hand);
        d.P(Stolen);
        Assert.Equal(4, d.Hand);
        d.End();
        d.P(Stolen);
        Assert.Equal(5, d.Hand);
    }

    [Fact]
    public void Quiet_readers_cord_repays_a_quiet_turn_with_a_card()
    {
        using var d = Arm("quiet_readers_cord", Times(Block5, 30), energy: 9);
        d.P(Block5); d.P(Block5);
        d.End();
        Assert.Equal(6, d.Hand);
        d.P(Block5); d.P(Block5); d.P(Block5);
        d.End();
        Assert.Equal(5, d.Hand);
    }

    [Fact]
    public void Redaction_knife_trades_one_card_after_the_draw_once_a_turn()
    {
        // The player names the card: the Unread one, which then pays for being thrown away.
        using var d = Arm("redaction_knife", Times("ashen_receipt", 30));
        Assert.True(d.PendingPick(), "the knife should ask which card goes");
        var before = d.Pile(CardZone.DiscardPile);
        d.Answer(pick: "ashen_receipt");
        Assert.Equal(before + 1, d.Pile(CardZone.DiscardPile));
        Assert.Equal(6, d.Hand);                       // five, one out, one in, and the receipt's Unread: one more
        d.P("ashen_receipt");
        Assert.False(d.PendingPick());                 // a draw later in the turn is no second trade
    }

    [Fact]
    public void Remembrance_string_makes_a_retained_card_cheaper_until_it_is_played()
    {
        // Every Barred Door has Retain: kept, each costs 1 less next turn, and the next turn's keep makes it cheaper still.
        using var d = Arm("remembrance_string", Times(Door, 20), energy: 3);
        d.End();
        d.P(Door);
        Assert.Equal(2, d.Energy);
        d.End();
        var energy = d.Energy;
        d.P(Door);                                     // kept twice now: 2 less, free
        Assert.Equal(energy, d.Energy);
    }

    [Fact]
    public void Ember_box_draws_when_held_energy_arrives()
    {
        int Drawn(string? relic)
        {
            using var d = Arm(relic, Times("banked_embers", 12), energy: 3);
            d.P("banked_embers"); d.P("banked_embers"); d.P("banked_embers");   // the third empties: the held arrives
            Assert.True(d.Energy > 0, d.Log);
            return d.Hand;
        }
        Assert.Equal(Drawn(null) + 1, Drawn("ember_box"));
    }

    [Fact]
    public void Blood_price_token_costs_three_health_and_cheapens_the_next_card_each_turn()
    {
        using var d = Arm("blood_price_token", Times(Block5, 30), energy: 3);
        var max = d.Me.Health.Max;
        Assert.Equal(max - 3, d.Health);
        d.P(Block5);
        Assert.Equal(3, d.Energy);
        d.P(Block5);
        Assert.Equal(2, d.Energy);
        d.End();
        Assert.Equal(max - 6, d.Health);
    }

    [Fact]
    public void Blackthorn_brooch_answers_a_great_guard_with_a_blow_to_every_enemy_once_a_turn()
    {
        using var d = Arm("blackthorn_brooch", Times(Door, 15), energy: 9,
            roster: [(Quiet, QuietIntent, 100), (Biter, BiteIntent, 100)]);
        d.P(Door);
        Assert.Equal(94, d.HealthOf(d.Enemies[0]));
        Assert.Equal(94, d.HealthOf(d.Enemies[1]));
        d.P(Door);
        Assert.Equal(94, d.HealthOf(d.Enemies[0]));
    }

    [Fact]
    public void Sootglass_lens_draws_when_a_debuff_lands_on_an_enemy_already_carrying_one()
    {
        // Overlooking files Doubt on a clean enemy, then Liable on top of that Doubt: a card. Again: the same turn, none.
        using var d = Arm("sootglass_lens", Times("overlooking", 15), roster: [(Biter, BiteIntent, 200)]);
        d.P("overlooking");
        Assert.True(5 == d.Hand, d.Log);
        d.P("overlooking");
        Assert.Equal(4, d.Hand);
    }

    [Fact]
    public void Executioners_measure_counts_a_kill_that_wasted_nothing()
    {
        using (var d = Arm("executioners_measure", Times(Cut, 15), roster: [(Quiet, QuietIntent, 6), (Biter, BiteIntent, 100)]))
        {
            d.P(Cut, d.Enemies[0]);
            Assert.Equal(1, d.Counter("executioners_measure_kills"));
        }
        using var e = Arm("executioners_measure", Times(Cut, 15), roster: [(Quiet, QuietIntent, 5), (Biter, BiteIntent, 100)]);
        e.P(Cut, e.Enemies[0]);
        Assert.Equal(0, e.Counter("executioners_measure_kills"));
    }

    // ── Rare ─────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Charnel_ledger_makes_an_unread_card_happen_twice()
    {
        int Drawn(string? relic)
        {
            using var d = Arm(relic, ["scattered_notes", "ashen_receipt", Block5, Block5, Block5, .. Times(Block5, 10)]);
            d.P("scattered_notes", pick: "ashen_receipt");
            return d.Hand;
        }
        Assert.Equal(Drawn(null) + 1, Drawn("charnel_ledger"));
    }

    [Fact]
    public void Index_bone_draws_one_more_every_turn()
    {
        using var d = Arm("index_bone", Times(Block5, 30));
        Assert.Equal(6, d.Hand);
        d.End();
        Assert.Equal(6, d.Hand);
    }

    [Fact]
    public void Withheld_hourglass_frees_one_card_each_turn()
    {
        using var d = Arm("withheld_hourglass", Times(Heavy, 30), energy: 3, roster: [(Quiet, QuietIntent, 300)]);
        d.P(Heavy);
        Assert.Equal(3, d.Energy);
        d.P(Heavy);
        Assert.Equal(1, d.Energy);
        d.End();
        d.P(Heavy);
        Assert.Equal(3, d.Energy);
    }

    [Fact]
    public void Iron_astrolabe_pays_an_energy_every_second_turn()
    {
        using var d = Arm("iron_astrolabe", Times(Block5, 40));
        Assert.Equal(0, d.Stacks(HeldEnergyId));
        d.End();
        Assert.Equal(1, d.Stacks(HeldEnergyId));
        d.End();
        Assert.Equal(1, d.Stacks(HeldEnergyId));
        d.End();
        Assert.Equal(2, d.Stacks(HeldEnergyId));
    }

    [Fact]
    public void Rebinding_spindle_cheapens_two_cards_each_turn()
    {
        using var d = Arm("rebinding_spindle", Times(Heavy, 30), energy: 9, roster: [(Quiet, QuietIntent, 300)]);
        d.P(Heavy); d.P(Heavy);
        Assert.Equal(7, d.Energy);
        d.P(Heavy);
        Assert.Equal(5, d.Energy);
    }

    [Fact]
    public void Blood_stamped_bond_costs_three_and_pays_on_the_first_turn_only()
    {
        using var d = Arm("blood_stamped_bond", Times(Block5, 30));
        Assert.Equal(d.Me.Health.Max - 3, d.Health);
        Assert.Equal(6, d.Hand);
        Assert.Equal(1, d.Stacks(HeldEnergyId));
        d.End();
        Assert.Equal(d.Me.Health.Max - 3, d.Health);
        Assert.Equal(5, d.Hand);
    }

    [Fact]
    public void Thorn_crowned_reliquary_throws_a_quarter_of_each_guard_at_the_healthiest_enemy()
    {
        using var d = Arm("thorn_crowned_reliquary", Times(Door, 15), energy: 9,
            roster: [(Quiet, QuietIntent, 100), (Biter, BiteIntent, 150)]);
        d.P(Door);                                     // 14 Block: 3
        Assert.Equal(100, d.HealthOf(d.Enemies[0]));
        Assert.Equal(147, d.HealthOf(d.Enemies[1]));
    }

    // ── one card, many events: "the first time" means once, not once per event of the same action ─────────────

    [Fact]
    public void Tarnished_bell_rings_once_for_a_card_that_files_two_debuffs()
    {
        // Doubt and Liable in one card: one ring (4, and Liable's own 1 on top), not two.
        using var d = Arm("tarnished_bell", Times("overlooking", 15), roster: [(Biter, BiteIntent, 200)]);
        d.P("overlooking");
        Assert.InRange(d.HealthOf(d.Enemy), 195, 196);
    }

    [Fact]
    public void Witness_slate_adds_to_one_enemy_when_a_card_files_liable_on_all()
    {
        using var d = Arm("witness_slate", Times("ill_wind", 15), energy: 3,
            roster: [(Quiet, QuietIntent, 200), (Biter, BiteIntent, 200)]);
        d.P("ill_wind");
        Assert.Equal(3, d.StacksOn(d.Enemies[0], "liable") + d.StacksOn(d.Enemies[1], "liable"));
    }

    [Fact]
    public void Conservators_thread_guards_once_for_a_card_that_discards_many()
    {
        // Hearth Kindling throws away two cards in one play: one guard for them, not two.
        int Guard(string? relic)
        {
            using var d = Arm(relic, Times("hearth_kindling", 15), energy: 3);
            d.P("hearth_kindling");
            var driver = d.Play.CombatDriver!;
            for (var i = 0; i < 2 && driver.PendingOptionChoice is not null; i++)
            {
                driver.SupplyOptionChoice([0]);
                if (driver.PendingCardChoice is { Count: > 0 } offered)
                    driver.SupplyCardChoice([offered[0].Id]);
            }
            if (driver.PendingOptionChoice is not null)
                driver.SupplyOptionChoice([1]);
            Assert.Null(d.Session.Error);
            Assert.True(d.Pile(CardZone.DiscardPile) >= 3, d.Log);
            return d.Block;
        }
        Assert.Equal(Guard(null) + 4, Guard("conservators_thread"));
    }

    [Fact]
    public void Bruise_cup_guards_once_against_a_double_blow()
    {
        using var d = Arm("bruise_cup", Times(Block5, 15), roster: [(Twice, TwiceIntent, null), (Biter, BiteIntent, null)]);
        var health = d.Health;
        d.End();                                       // 5, 5: then 4 Block against the bite of 6
        Assert.Equal(health - 12, d.Health);
    }

    private const string HeldEnergyId = "held_energy";
}

internal static class RelicBenchExtensions
{
    public static bool PendingPick(this RelicBench d) => d.Play.CombatDriver!.PendingCardChoice is not null;
}
