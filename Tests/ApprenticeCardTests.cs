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
        Assert.Equal(7, Block(Hero(play)));
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
        Assert.Equal(5, Block(Hero(play)));
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
        // Dust Mote: 3 + 3 × 2 inscriptions; Candle Stub 4.
        Assert.Equal(before - 13, Standing(Body(play, enemy)));
        play.Dispose();
    }
}
