using BnbContent.Converter.Apprentice;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using static BnbContent.Tests.ApprenticeTests;

namespace BnbContent.Tests;

// The Apprentice's cards, one rule each (master §6–§9): what the card does when PLAYED, and what it does in the
// WAND. Fights of the real document, against a tablet that only guards itself unless a test needs an attacker.
public class ApprenticeCardTests
{
    private const string Biter = "embossed_seal";
    private const string BiteIntent = "heavy_impression";

    private static int Stacks(CombatantState c, string status) => FightProbe.StacksOf(c, status);

    private static WandPlan Plan(RunPlayback play, CombatantId target)
    {
        var combat = Now(play);
        return WandCompiler.Compile(combat.State, combat.Registry, combat.HeroId, target);
    }

    // ── commons ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Wax_drop_singes_every_enemy_and_brands_whatever_the_next_speak_hits()
    {
        var (play, session, enemy) = Fight(["ap_wax_drop", "ap_wax_drop", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_wax_drop", enemy);
        Assert.Equal(2, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));

        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_wax_drop", 0);
        Inscribe(play, session, "ap_candle_stub", 1);
        Cast(play, session, enemy);
        // The Candle Stub's hit spent one Singe (2 → 1), and the Brand put one back on what it hit.
        Assert.Equal(2, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));
        play.Dispose();
    }

    [Fact]
    public void Snuffer_spends_the_singe_before_its_blow_and_quenches_the_wand()
    {
        var (play, session, enemy) = Fight(["ap_wax_drop", "ap_snuffer", "ap_snuffer", "ap_candle_stub", "ap_chalk_line"]);
        Play(play, session, "ap_wax_drop", enemy);
        var before = Standing(Body(play, enemy));
        Play(play, session, "ap_snuffer", enemy);
        Assert.Equal(before - 8, Standing(Body(play, enemy)));   // 2 Singe × 4 — and the blow itself found none
        Assert.Equal(0, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));
        play.Dispose();

        (play, session, enemy) = Fight(["ap_wax_drop", "ap_snuffer", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_wax_drop", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_snuffer", 0);
        Inscribe(play, session, "ap_candle_stub", 1);
        Assert.Equal(0, Plan(play, enemy).Exhaustion);             // 1 − 2 × 2
        Cast(play, session, enemy);
        Assert.Equal(0, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));
        play.Dispose();
    }

    [Fact]
    public void Ink_dish_takes_one_of_the_top_three_and_its_speak_brings_flow_after_the_cast()
    {
        var deck = new[] { "ap_ink_dish", "ap_chalk_line", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub",
            "ap_flyleaf", "ap_flyleaf", "ap_flyleaf" };
        var (play, session, enemy) = Fight(deck);
        var driver = play.CombatDriver!;
        var top = Now(play).State.GetCardZones(Now(play).HeroId).DrawPile.Take(3).Select(c => c.Id).ToList();
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_ink_dish").Id, enemy);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        Assert.Equal(top, offered.Select(c => c.Id));
        driver.SupplyCardChoice([offered[1].Id]);
        Assert.Null(session.Error);
        Assert.Contains(Now(play).Hand, c => c.Id == offered[1].Id);
        Assert.Equal(0, Stacks(Hero(play), ApprenticeKeywords.Flow)); // nothing here costs 2
        play.Dispose();

        (play, session, enemy) = Fight(["ap_ink_dish", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_ink_dish", 1);
        Play(play, session, "ap_chalk_line", enemy);
        Cast(play, session, enemy);
        Assert.Equal(1, Stacks(Hero(play), ApprenticeKeywords.Flow));
        play.Dispose();
    }

    [Fact]
    public void Folded_leaf_puts_a_discarded_card_on_top_of_the_draw_pile()
    {
        var (play, session, enemy) = Fight(["ap_folded_leaf", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub",
            "ap_flyleaf"]);
        Play(play, session, "ap_chalk_line", enemy);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_folded_leaf").Id, enemy);
        if (driver.PendingCardChoice is { } discard)
            driver.SupplyCardChoice([discard.First(c => c.DefinitionId.value == "ap_chalk_line").Id]);
        Assert.Null(session.Error);
        Assert.Equal("ap_chalk_line", Now(play).State.GetCardZones(Now(play).HeroId).DrawPile[0].DefinitionId.value);
        Assert.Equal(1, Stacks(Hero(play), ApprenticeKeywords.Gust));
        play.Dispose();
    }

    [Fact]
    public void Chalk_circle_gives_grit_at_the_end_of_a_turn_without_a_cast_and_grounds_the_wand()
    {
        var (play, session, enemy) = Fight(["ap_chalk_circle", "ap_chalk_circle", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_circle", enemy);
        Assert.Equal(9, Block(Hero(play)));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ApprenticeKeywords.Grit));
        Assert.Equal(0, Stacks(Hero(play), ApprenticeRules.QuietCircle));
        play.Dispose();

        (play, session, enemy) = Fight(["ap_chalk_circle", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_chalk_circle", 0);
        Inscribe(play, session, "ap_candle_stub", 1);
        Assert.Equal(0, Plan(play, enemy).Exhaustion);
        play.Dispose();
    }

    [Fact]
    public void Clay_weight_turns_spent_focus_into_block()
    {
        var (play, session, enemy) = Fight(["ap_clay_weight", "ap_chalk_line", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_chalk_line", enemy);   // Focus 2
        var driver = play.CombatDriver!;
        var block = Block(Hero(play));
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_clay_weight").Id, enemy);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([options.ToList().IndexOf("Spend 2 Focus")]);
        Assert.Null(session.Error);
        Assert.Equal(block + 10, Block(Hero(play)));
        Assert.Equal(1, Wand(play).Focus);             // 2 − 2, and the play itself gave 1
        play.Dispose();
    }

    [Fact]
    public void Masons_dust_makes_grit_or_turns_it_into_ward_wax()
    {
        var (play, session, enemy) = Fight(["ap_masons_dust", "ap_masons_dust", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_masons_dust", enemy);
        Assert.Equal(1, Stacks(Hero(play), ApprenticeKeywords.Grit));
        Play(play, session, "ap_masons_dust", enemy);
        Assert.Equal(0, Stacks(Hero(play), ApprenticeKeywords.Grit));
        Assert.Equal(5, Stacks(Hero(play), BnbContent.Converter.Cards.Keywords.WardWax));
        play.Dispose();
    }

    [Fact]
    public void Open_casement_swaps_two_wand_cards_for_free_and_draws()
    {
        var (play, session, enemy) = Fight(["ap_open_casement", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line",
            "ap_flyleaf"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_chalk_line", 1);
        var focus = Wand(play).Focus;
        var hand = Now(play).Hand.Count;
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_open_casement").Id, enemy);
        var installed = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        driver.SupplyCardChoice([.. installed.Select(c => c.Id)]);
        Assert.Null(session.Error);
        var zones = Now(play).State.GetCardZones(Now(play).HeroId);
        Assert.Equal("ap_chalk_line", zones.GetCard(Wand(play).Slots[0]!.Value).DefinitionId.value);
        Assert.Equal(focus + 1, Wand(play).Focus);     // the swap was free; the play gave 1
        Assert.Equal(hand, Now(play).Hand.Count);      // one played, one drawn
        play.Dispose();
    }

    [Fact]
    public void Penny_whistle_gives_more_focus_after_another_card_and_cuts_a_cast_short()
    {
        var (play, session, enemy) = Fight(["ap_penny_whistle", "ap_penny_whistle", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_penny_whistle", enemy);
        Assert.Equal(2, Wand(play).Focus);             // the first card this turn: only its own
        Play(play, session, "ap_penny_whistle", enemy);
        Assert.Equal(4, Wand(play).Focus);             // after another: one more
        play.Dispose();
    }

    [Fact]
    public void Wick_trimmer_exhausts_a_card_to_draw_or_guards()
    {
        var (play, session, enemy) = Fight(["ap_wick_trimmer", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub",
            "ap_flyleaf", "ap_flyleaf"]);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_wick_trimmer").Id, enemy);
        driver.SupplyOptionChoice([0]);
        var hand = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        Assert.DoesNotContain(hand, c => c.DefinitionId.value == "ap_wick_trimmer");
        driver.SupplyCardChoice([hand.First(c => c.DefinitionId.value == "ap_chalk_line").Id]);
        Assert.Null(session.Error);
        Assert.Contains(Now(play).State.GetCardZones(Now(play).HeroId).ExhaustPile, c => c.DefinitionId.value == "ap_chalk_line");
        Assert.Equal(5, Now(play).Hand.Count);         // 5 − 2 + 2
        play.Dispose();
    }

    [Fact]
    public void Spare_notch_lengthens_the_wand_and_exhausts()
    {
        var (play, session, enemy) = Fight(["ap_spare_notch", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_spare_notch", enemy);
        Assert.Equal(5, Wand(play).Capacity);
        Assert.Contains(Now(play).State.GetCardZones(Now(play).HeroId).ExhaustPile, c => c.DefinitionId.value == "ap_spare_notch");
        play.Dispose();
    }

    [Fact]
    public void Pinprick_guards_against_an_attacker()
    {
        var (play, session, enemy) = Fight(["ap_pinprick", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"],
            enemy: Biter, intent: BiteIntent);
        Play(play, session, "ap_pinprick", enemy);
        Assert.Equal(6, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Cup_of_water_makes_flow_or_drinks_it_for_cards_and_block()
    {
        var (play, session, enemy) = Fight(["ap_cup_of_water", "ap_cup_of_water", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub",
            "ap_flyleaf", "ap_flyleaf"]);
        Play(play, session, "ap_cup_of_water", enemy);
        Assert.Equal(2, Stacks(Hero(play), ApprenticeKeywords.Flow));
        var hand = Now(play).Hand.Count;
        Play(play, session, "ap_cup_of_water", enemy);
        Assert.Equal(0, Stacks(Hero(play), ApprenticeKeywords.Flow));
        Assert.Equal(6, Block(Hero(play)));
        Assert.Equal(hand - 1 + 2, Now(play).Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Dust_mote_rewards_inscribing_and_its_speak_grows_with_every_inscription_since_the_last_cast()
    {
        var (play, session, enemy) = Fight(["ap_dust_mote", "ap_dust_mote", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line",
            "ap_flyleaf"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_dust_mote", 0);
        var hand = Now(play).Hand.Count;
        Play(play, session, "ap_dust_mote", enemy);
        Assert.Equal(hand - 1 + 1, Now(play).Hand.Count);
        Inscribe(play, session, "ap_candle_stub", 1);
        Play(play, session, "ap_chalk_line", enemy);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        // Dust Mote: 4 + 3 × 2 inscriptions; Candle Stub 5.
        Assert.Equal(before - 15, Standing(Body(play, enemy)));
        play.Dispose();
    }

    // ── uncommons ──────────────────────────────────────────────────────────────────────────────────────────

    private static (RunPlayback, RogueDeck.Sandbox.Run.InteractiveRunSession, CombatantId) Bitten(string[] deck) =>
        Fight(deck, enemy: Biter, intent: BiteIntent, health: 70);

    [Fact]
    public void Split_wick_hits_twice_and_forks_the_next_speak()
    {
        var (play, session, enemy) = Fight(["ap_split_wick", "ap_wax_drop", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_wax_drop", enemy);
        var before = Standing(Body(play, enemy));
        Play(play, session, "ap_split_wick", enemy);
        Assert.Equal(before - (4 + 3) * 2, Standing(Body(play, enemy)));   // each hit met a Singe
        play.Dispose();

        (play, session, enemy) = Fight(["ap_split_wick", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_split_wick", 0);
        Inscribe(play, session, "ap_candle_stub", 1);
        Play(play, session, "ap_chalk_line", enemy);
        before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        Assert.Equal(before - 3 * 2, Standing(Body(play, enemy)));         // 4 → 3 + 3
        play.Dispose();
    }

    [Fact]
    public void Cracked_slate_may_spend_grit_for_a_heavier_blow()
    {
        var (play, session, enemy) = Fight(["ap_cracked_slate", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_chalk_line", enemy);                         // 1 Grit
        var before = Standing(Body(play, enemy));
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_cracked_slate").Id, enemy);
        driver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);
        Assert.Equal(before - 16, Standing(Body(play, enemy)));
        Assert.Equal(0, Stacks(Hero(play), ApprenticeKeywords.Grit));
        play.Dispose();
    }

    [Fact]
    public void Crosswind_moves_singe_to_another_enemy_or_becomes_gust_alone()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Roster("crosswind", 9, (Quiet, QuietIntent, null), (Biter, BiteIntent, null)),
            ["ap_crosswind", "ap_wax_drop", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"],
            character: ApprenticeCharacter.Id);
        var other = Now(play).State.Combatants.First(c => c.Id != Now(play).HeroId && c.Id != enemy).Id;
        Play(play, session, "ap_wax_drop", enemy);                          // 2 on each
        Play(play, session, "ap_crosswind", enemy);
        Assert.Equal(0, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));
        Assert.Equal(4, Stacks(Body(play, other), ApprenticeKeywords.Singe));
        play.Dispose();

        (play, session, enemy) = Fight(["ap_crosswind", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_crosswind", enemy);
        Assert.Equal(2, Stacks(Hero(play), ApprenticeKeywords.Gust));
        play.Dispose();
    }

    [Fact]
    public void Rubbing_copies_a_discarded_card_for_one_play()
    {
        var (play, session, enemy) = Fight(["ap_rubbing", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_candle_stub", enemy);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_rubbing").Id, enemy);
        if (driver.PendingCardChoice is { } discard)
            driver.SupplyCardChoice([discard.First(c => c.DefinitionId.value == "ap_candle_stub").Id]);
        Assert.Null(session.Error);
        var copy = Now(play).Hand.Single(c => c.DefinitionId.value == "ap_candle_stub");
        Play(play, session, "ap_candle_stub", enemy);
        Assert.Equal(CardZone.ExhaustPile, Now(play).State.GetCardZones(Now(play).HeroId).GetCard(copy.Id).Zone);
        play.Dispose();
    }

    [Fact]
    public void Watermark_and_footnote_trade_in_flow_and_exhaustion()
    {
        var (play, session, enemy) = Fight(["ap_watermark", "ap_footnote", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_watermark", enemy);                         // no Flow yet: just the Flow
        Assert.Equal(2, Stacks(Hero(play), ApprenticeKeywords.Flow));
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_footnote").Id, enemy);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        Assert.Equal(2, options.Count);
        driver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);
        Assert.Equal(0, Wand(play).Exhaustion);                              // never below zero
        play.Dispose();
    }

    [Fact]
    public void Torn_flyleaf_takes_a_card_back_and_slips_along_after_a_cast()
    {
        var (play, session, enemy) = Fight(["ap_torn_flyleaf", "ap_torn_flyleaf", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_torn_flyleaf", 1);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_torn_flyleaf", enemy);   // two Chalk Lines and a Torn Flyleaf: Focus 4
        var driver = play.CombatDriver!;
        if (driver.PendingCardChoice is { } installed)
            driver.SupplyCardChoice([installed.First(c => c.DefinitionId.value == "ap_candle_stub").Id]);
        Assert.Null(session.Error);
        Assert.Contains(Now(play).Hand, c => c.DefinitionId.value == "ap_candle_stub");
        Inscribe(play, session, "ap_candle_stub", 0);
        var focus = Wand(play).Focus;
        Cast(play, session, enemy);
        var zones = Now(play).State.GetCardZones(Now(play).HeroId);
        Assert.Equal("ap_torn_flyleaf", zones.GetCard(Wand(play).Slots[2]!.Value).DefinitionId.value);
        Assert.Equal(focus - 2 + 1, Wand(play).Focus);
        play.Dispose();
    }

    [Fact]
    public void Candle_at_both_ends_burns_after_repeating_the_last_blow()
    {
        var (play, session, enemy) = Fight(["ap_candle_both_ends", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_candle_both_ends", 1);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        // 5 and its Singe (first slot), then 150% of 5 rounded up that spends the Singe: 5 + 8 + 3.
        Assert.Equal(before - 16, Standing(Body(play, enemy)));
        Assert.Contains(Now(play).State.GetCardZones(Now(play).HeroId).ExhaustPile, c => c.DefinitionId.value == "ap_candle_both_ends");
        Assert.Equal(1, Wand(play).Occupied);
        play.Dispose();
    }

    [Fact]
    public void Firebreak_turns_singe_into_grit()
    {
        var (play, session, enemy) = Fight(["ap_firebreak", "ap_wax_drop", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_wax_drop", enemy);
        Play(play, session, "ap_firebreak", enemy);
        Assert.Equal(0, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));
        Assert.Equal(2, Stacks(Hero(play), ApprenticeKeywords.Grit));
        play.Dispose();
    }

    [Fact]
    public void Kiln_shelf_raises_the_cap()
    {
        var (play, session, enemy) = Fight(["ap_kiln_shelf", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_kiln_shelf", enemy);
        Assert.Equal(10, Wand(play).ExhaustionCap);
        play.Dispose();
    }

    [Fact]
    public void Borrowed_stone_spends_grit_for_energy_and_focus()
    {
        var (play, session, enemy) = Fight(["ap_borrowed_stone", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        Play(play, session, "ap_chalk_line", enemy);
        var energy = Now(play).HeroEnergy;
        var focus = Wand(play).Focus;
        Play(play, session, "ap_borrowed_stone", enemy);
        Assert.Equal(energy - 1 + 1, Now(play).HeroEnergy);
        Assert.Equal(focus + 1 + 2, Wand(play).Focus);
        play.Dispose();
    }

    [Fact]
    public void Cold_ash_softens_the_next_attack()
    {
        var (play, session, enemy) = Bitten(["ap_cold_ash", "ap_wax_drop", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        var hp = Hero(play).Health.Current;
        Play(play, session, "ap_wax_drop", enemy);
        Play(play, session, "ap_cold_ash", enemy);
        Assert.Equal(4, Stacks(Body(play, enemy), ApprenticeCards.Chilled));
        play.CombatDriver!.EndTurn();
        var softened = hp - Hero(play).Health.Current;
        Assert.Equal(0, Stacks(Body(play, enemy), ApprenticeCards.Chilled));
        play.Dispose();

        (play, session, enemy) = Bitten(["ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
        hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(hp - Hero(play).Health.Current - 4, softened);
        play.Dispose();
    }

    [Fact]
    public void Seal_press_keeps_a_ward_for_the_first_attack()
    {
        // Focus from Candle Stubs, so there is no other Block or Grit in the way of the bite.
        int Bite(bool cast)
        {
            var (play, session, enemy) = Bitten(["ap_seal_press", "ap_chalk_line", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"]);
            Play(play, session, "ap_candle_stub", enemy);
            Inscribe(play, session, "ap_seal_press", 0);
            Inscribe(play, session, "ap_chalk_line", 1);
            Play(play, session, "ap_candle_stub", enemy);
            if (cast)
            {
                Cast(play, session, enemy);
                Assert.Equal(WandStoreTrigger.NextEnemyAttack, Wand(play).StoredSpell!.Trigger);
                Assert.Equal(0, Block(Hero(play)));                    // kept, not spoken
            }
            var hp = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            Assert.Null(Wand(play).StoredSpell);
            var taken = hp - Hero(play).Health.Current;
            play.Dispose();
            return taken;
        }

        var unsealed = Bite(cast: false);
        Assert.True(unsealed >= 3, $"the bite must be at least the seal for this to say anything ({unsealed})");
        Assert.Equal(unsealed - 4, Bite(cast: true));                   // 75% of the Chalk Line's 6, rounded down
    }

    // ── rares ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Masters_correction_cheapens_a_card_or_inscribes_one_for_free()
    {
        var (play, session, enemy) = Fight(["ap_masters_correction", "ap_final_examination", "ap_candle_stub", "ap_chalk_line",
            "ap_chalk_line", "ap_flyleaf"]);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_masters_correction").Id, enemy);
        driver.SupplyOptionChoice([0]);
        var hand = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        var exam = hand.First(c => c.DefinitionId.value == "ap_final_examination");
        driver.SupplyCardChoice([exam.Id]);
        Assert.Null(session.Error);
        var energy = Now(play).HeroEnergy;
        driver.PlayCard(exam.Id, enemy);
        if (driver.PendingOptionChoice is { } spend)
            driver.SupplyOptionChoice([0]);
        Assert.Equal(energy - 1, Now(play).HeroEnergy);                     // 2 less 1
        play.Dispose();

        (play, session, enemy) = Fight(["ap_masters_correction", "ap_candle_stub", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Inscribe(play, session, "ap_candle_stub", 0);                        // the combat's free one
        driver = play.CombatDriver!;
        var focus = Wand(play).Focus;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_masters_correction").Id, enemy);
        driver.SupplyOptionChoice([1]);
        if (driver.PendingCardChoice is { } inscribable)
            driver.SupplyCardChoice([inscribable.First(c => c.DefinitionId.value == "ap_candle_stub").Id]);
        if (driver.PendingOptionChoice is { } slots)
            driver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);
        Assert.Equal(2, Wand(play).Occupied);
        Assert.Equal(focus + 1, Wand(play).Focus);                          // free, and the play gave 1
        play.Dispose();
    }

    [Fact]
    public void Final_examination_and_the_last_word_spend_focus_for_their_blows()
    {
        var (play, session, enemy) = Fight(["ap_final_examination", "ap_last_word", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);                         // Focus 3
        var before = Standing(Body(play, enemy));
        var block = Block(Hero(play));
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_final_examination").Id, enemy);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([options.ToList().IndexOf("Spend 3 Focus")]);
        Assert.Null(session.Error);
        Assert.Equal(before - (6 + 12), Standing(Body(play, enemy)));
        Assert.Equal(block + 4 + 9, Block(Hero(play)));
        Assert.Equal(1, Wand(play).Focus);                                   // 3 − 3 + the play's own 1
        play.Dispose();
    }

    [Fact]
    public void Lessons_in_ash_trades_cards_for_draws_and_focus()
    {
        var (play, session, enemy) = Fight(["ap_lessons_ash", "ap_candle_stub", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line",
            "ap_flyleaf", "ap_flyleaf"]);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_lessons_ash").Id, enemy);
        driver.SupplyOptionChoice([2]);
        for (var i = 0; i < 2; i++)
            if (driver.PendingCardChoice is { } hand)
                driver.SupplyCardChoice([hand.First(c => c.DefinitionId.value == "ap_candle_stub").Id]);
        Assert.Null(session.Error);
        Assert.Equal(2, Now(play).State.GetCardZones(Now(play).HeroId).ExhaustPile.Count);
        Assert.Equal(1 + 1 + 2, Wand(play).Focus);
        play.Dispose();
    }

    [Fact]
    public void Glass_rod_strengthens_and_strains_the_next_cast()
    {
        var (play, session, enemy) = Fight(["ap_glass_rod", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_glass_rod", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        Assert.Equal(before - 7, Standing(Body(play, enemy)));             // 5 × 1.5, rounded down (its Singe comes after)
        Assert.Equal(4, Wand(play).Exhaustion);
        Assert.Equal(0, Stacks(Hero(play), ApprenticeRules.GlassRodPrimed));
        play.Dispose();
    }

    [Fact]
    public void Second_hand_brings_back_an_exhausted_card_for_one_play()
    {
        var (play, session, enemy) = Fight(["ap_second_hand", "ap_spare_notch", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_spare_notch", enemy);                        // exhausts itself
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_second_hand").Id, enemy);
        if (driver.PendingCardChoice is { } exhausted)
            driver.SupplyCardChoice([exhausted.First(c => c.DefinitionId.value == "ap_spare_notch").Id]);
        Assert.Null(session.Error);
        var copy = Now(play).Hand.Single(c => c.DefinitionId.value == "ap_spare_notch");
        Assert.Contains(StandardCombatIds.TemporaryCopyMark, copy.Marks);
        Play(play, session, "ap_spare_notch", enemy);
        Assert.Equal(6, Wand(play).Capacity);
        play.Dispose();
    }

    [Fact]
    public void The_fourfold_lesson_gives_every_element_and_rewards_a_turn_that_had_them()
    {
        var (play, session, enemy) = Fight(["ap_fourfold_lesson", "ap_fourfold_lesson", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_fourfold_lesson", enemy);
        Assert.Equal((1, 1, 1), (Stacks(Hero(play), ApprenticeKeywords.Flow), Stacks(Hero(play), ApprenticeKeywords.Grit),
            Stacks(Hero(play), ApprenticeKeywords.Gust)));
        Assert.Equal(4, System.Numerics.BitOperations.PopCount((uint)Wand(play).ElementsThisTurn));
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_fourfold_lesson", 1);
        Play(play, session, "ap_chalk_line", enemy);
        var focus = Wand(play).Focus;
        var block = Block(Hero(play));
        Cast(play, session, enemy);
        Assert.Equal(block + 5, Block(Hero(play)));
        // 2 for the cast, 1 back for all four elements; and Flow from before refunds 1 of the cost.
        Assert.Equal(focus - 2 + 1 + 1, Wand(play).Focus);
        play.Dispose();
    }

    [Fact]
    public void A_perfect_copy_lays_the_wand_out_again()
    {
        var (play, session, enemy) = Fight(["ap_perfect_copy", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line",
            "ap_flyleaf", "ap_flyleaf"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_chalk_line", 2);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_perfect_copy").Id, enemy);
        var installed = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        driver.SupplyCardChoice([.. installed.Select(c => c.Id).Reverse()]);
        Assert.Null(session.Error);
        var zones = Now(play).State.GetCardZones(Now(play).HeroId);
        Assert.Equal("ap_chalk_line", zones.GetCard(Wand(play).Slots[0]!.Value).DefinitionId.value);
        Assert.Null(Wand(play).Slots[1]);
        play.Dispose();
    }
}
