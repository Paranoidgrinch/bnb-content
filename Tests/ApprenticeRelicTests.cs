using BnbContent.Converter.Apprentice;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using static BnbContent.Tests.ApprenticeTests;

namespace BnbContent.Tests;

// The Apprentice's eighteen relics (master §10, expansion §4), each in a live fight of the real document: the mark it puts on him and
// the wand rule the mark switches on.
public class ApprenticeRelicTests
{
    private const string Biter = "embossed_seal";
    private const string BiteIntent = "heavy_impression";

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) With(
        string relic, string[] deck, string enemy = Quiet, string intent = QuietIntent) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, energy: 9), deck, health: 70,
            character: ApprenticeCharacter.Id, relics: [relic]);

    [Fact]
    public void Every_relic_is_his_alone_and_puts_its_mark_on_him()
    {
        Assert.Equal(18, ApprenticeRelics.All().Count);
        // The others' shape: 3 Common, 5 Uncommon, 4 Rare in his own pool, 6 from the shop.
        var shape = ApprenticeRelics.All().GroupBy(r => r.Rarity).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal((3, 5, 4, 6), (shape[BnbContent.Converter.Relics.RelicAuthoring.Rarity.Common],
            shape[BnbContent.Converter.Relics.RelicAuthoring.Rarity.Uncommon], shape[BnbContent.Converter.Relics.RelicAuthoring.Rarity.Rare],
            shape[BnbContent.Converter.Relics.RelicAuthoring.Rarity.Shop]));
        Assert.All(ApprenticeRelics.All(), r => Assert.Equal(BnbContent.Converter.Relics.RelicAuthoring.Eligibility.Apprentice, r.Eligibility));
        foreach (var relic in ApprenticeRelics.All())
        {
            var (play, _, _) = With(relic.Id, ["ap_candle_stub"]);
            Assert.Contains(Hero(play).Statuses, s => s.DefinitionId.value == relic.CombatRule!.Id);
            play.Dispose();
        }
    }

    [Fact]
    public void Sooty_ferrule_gives_focus_after_a_cast_whose_hit_spent_singe()
    {
        var (play, session, enemy) = With("ar_sooty_ferrule", ["ap_wax_drop", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_wax_drop", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        var focus = Wand(play).Focus;
        Cast(play, session, enemy);
        Assert.Equal(focus - 2 + 1, Wand(play).Focus);
        play.Dispose();
    }

    [Fact]
    public void Knotted_wristcord_hands_a_short_casts_card_back()
    {
        var (play, session, enemy) = With("ar_knotted_wristcord", ["ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        var driver = play.CombatDriver!;
        driver.UseAction(new CardDefinitionId(ApprenticeWand.Cast), enemy);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);
        Assert.Contains(Now(play).Hand, c => c.DefinitionId.value == "ap_candle_stub");
        Assert.Equal(0, Wand(play).Occupied);
        play.Dispose();
    }

    [Fact]
    public void Blue_inkstone_turns_a_flow_refund_into_relief()
    {
        var (play, session, enemy) = With("ar_blue_inkstone", ["ap_ink_blot", "ap_ink_blot", "ap_candle_stub", "ap_flyleaf", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);                          // Focus 2
        var driver = play.CombatDriver!;
        for (var i = 0; i < 2; i++)
        {
            driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_ink_blot").Id, enemy);
            if (driver.PendingCardChoice is { } discard)
                driver.SupplyCardChoice([discard.First(c => c.DefinitionId.value != "ap_candle_stub" && c.DefinitionId.value != "ap_flyleaf").Id]);
        }
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ApprenticeKeywords.Flow));
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_flyleaf", 1);
        var focus = Wand(play).Focus;
        driver.UseAction(new CardDefinitionId(ApprenticeWand.Cast), enemy);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([2]);                                        // both points to Exhaustion
        Assert.Null(session.Error);
        Assert.Equal(focus - 2, Wand(play).Focus);                            // no Focus back
        Assert.Equal(1, Wand(play).Exhaustion);                               // 3 − 2
        play.Dispose();
    }

    [Fact]
    public void Bent_nib_lets_a_new_card_trade_places_with_its_neighbour()
    {
        var (play, session, enemy) = With("ar_bent_nib", ["ap_candle_stub", "ap_ink_blot", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        var driver = play.CombatDriver!;
        driver.UseAction(new CardDefinitionId(ApprenticeWand.Inscribe), null);
        driver.SupplyCardChoice([driver.PendingCardChoice!.First(c => c.DefinitionId.value == "ap_ink_blot").Id]);
        driver.SupplyOptionChoice([driver.PendingOptionChoice!.ToList().IndexOf("Slot 2")]);
        var swap = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);
        var zones = Now(play).State.GetCardZones(Now(play).HeroId);
        Assert.Equal("ap_ink_blot", zones.GetCard(Wand(play).Slots[0]!.Value).DefinitionId.value);
        play.Dispose();
    }

    [Fact]
    public void Ash_bookcord_fills_a_burned_out_slot_from_the_hand()
    {
        var (play, session, enemy) = With("ar_ash_bookcord", ["ap_candle_stub", "ap_chalk_line", "ap_ink_blot", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Cast(play, session, enemy);                                            // Exhaustion to burn out
        var driver = play.CombatDriver!;
        driver.UseAction(new CardDefinitionId(ApprenticeWand.BurnOut), enemy);
        driver.SupplyCardChoice([driver.PendingCardChoice!.Single().Id]);
        var fill = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([fill.ToList().FindIndex(o => o.Contains("Ink Blot"))]);
        Assert.Null(session.Error);
        var zones = Now(play).State.GetCardZones(Now(play).HeroId);
        Assert.Equal("ap_ink_blot", zones.GetCard(Wand(play).Slots[0]!.Value).DefinitionId.value);
        play.Dispose();
    }

    [Fact]
    public void Chalked_thumbstall_turns_grit_into_ground_for_the_next_cast()
    {
        var (play, session, enemy) = With("ar_chalked_thumbstall", ["ap_chalk_line", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"],
            Biter, BiteIntent);
        Play(play, session, "ap_chalk_line", enemy);                          // 1 Grit
        play.CombatDriver!.EndTurn();
        Assert.Equal(2, Wand(play).GroundCredit);
        play.Dispose();
    }

    [Fact]
    public void Bone_spacer_heightens_the_speak_after_a_crossed_gap()
    {
        var (play, session, enemy) = With("ar_bone_spacer", ["ap_ink_blot", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_ink_blot", 0);
        Inscribe(play, session, "ap_candle_stub", 2);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        // Ink Blot then Bone Spacer: 4 × 1.25 = 5, × 1.5 = 7 (rounded down at each step).
        Assert.True(before - Standing(Body(play, enemy)) >= 7);
        Assert.Equal(2, Wand(play).Exhaustion);                               // base 1 + the spacer's 1
        play.Dispose();
    }

    [Fact]
    public void Feather_catcher_inscribes_a_card_the_gust_would_discard()
    {
        var (play, session, enemy) = With("ar_feather_catcher", ["ap_flyleaf", "ap_candle_stub", "ap_candle_stub", "ap_chalk_line",
            "ap_chalk_line", "ap_ink_blot", "ap_ink_blot"]);
        var driver = play.CombatDriver!;
        driver.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "ap_flyleaf").Id, enemy);
        if (driver.PendingCardChoice is { } discard)
            driver.SupplyCardChoice([discard[0].Id]);
        driver.EndTurn();
        var gust = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        var caught = gust.First(c => c.DefinitionId.value != "ap_flyleaf");
        driver.SupplyCardChoice([caught.Id]);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        driver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);
        Assert.Equal(caught.Id, Wand(play).Slots[0]);
        play.Dispose();
    }

    [Fact]
    public void Brass_dividers_offer_a_run_of_the_wand_once_a_turn()
    {
        var (play, session, enemy) = With("ar_brass_dividers", ["ap_ink_blot", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_ink_blot", 0);
        Inscribe(play, session, "ap_candle_stub", 1);
        var driver = play.CombatDriver!;
        var before = Standing(Body(play, enemy));
        driver.UseAction(new CardDefinitionId(ApprenticeWand.Cast), enemy);
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(driver.PendingOptionChoice);
        Assert.Equal("The whole wand", options[0]);
        driver.SupplyOptionChoice([options.ToList().IndexOf("Only slot 2")]);
        Assert.Null(session.Error);
        Assert.Equal(1, Wand(play).CastsThisTurn);
        Assert.Equal(before - 5, Standing(Body(play, enemy)));              // the Candle Stub alone, unheightened
        play.Dispose();
    }

    // ── the expansion's nine ─────────────────────────────────────────────────────────────────────────────────

    private static int Stacks(CombatantState c, string status) => FightProbe.StacksOf(c, status);

    [Fact]
    public void The_opening_relics_give_rote_poise_focus_singe_and_a_slot_as_the_fight_opens()
    {
        var (play, _, enemy) = With("ar_pencil_box", ["ap_candle_stub"]);
        Assert.Equal(1, Stacks(Hero(play), ApprenticeKeywords.Rote));
        play.Dispose();

        (play, _, _) = With("ar_wire_spectacles", ["ap_candle_stub"]);
        Assert.Equal((1, 2), (Stacks(Hero(play), ApprenticeKeywords.Poise), Wand(play).Focus));
        play.Dispose();

        (play, _, enemy) = With("ar_spare_wicks", ["ap_candle_stub"]);
        Assert.Equal(2, Stacks(Body(play, enemy), ApprenticeKeywords.Singe));
        play.Dispose();

        (play, _, _) = With("ar_tutors_cane", ["ap_candle_stub"]);
        Assert.Equal(5, Wand(play).Capacity);
        play.Dispose();
    }

    [Fact]
    public void Pewter_inkwell_guards_every_cast_and_star_chart_teaches_the_first_of_each_turn()
    {
        var deck = new[] { "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line" };
        var (play, session, enemy) = With("ar_pewter_inkwell", deck);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        var block = Block(Hero(play));
        Cast(play, session, enemy);
        Assert.Equal(block + 2, Block(Hero(play)));
        play.Dispose();

        (play, session, enemy) = With("ar_star_chart", deck);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Cast(play, session, enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Cast(play, session, enemy);
        Assert.Equal(1, Stacks(Hero(play), ApprenticeKeywords.Rote));   // once a turn
        play.Dispose();
    }

    [Fact]
    public void Slate_board_guards_an_inscription_oil_lamp_pays_a_quiet_turn_and_sand_glass_a_kept_wand()
    {
        var deck = new[] { "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_ink_blot", "ap_flyleaf" };
        var (play, session, _) = With("ar_slate_board", deck);
        var block = Block(Hero(play));
        Inscribe(play, session, "ap_candle_stub", 0);
        Assert.Equal(block + 2, Block(Hero(play)));
        play.Dispose();

        (play, _, _) = With("ar_oil_lamp", deck);
        var focus = Wand(play).Focus;
        play.CombatDriver!.EndTurn();
        Assert.Equal(focus + 1, Wand(play).Focus);
        play.Dispose();

        (play, session, var enemy) = With("ar_sand_glass", deck);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_ink_blot", 1);
        Inscribe(play, session, "ap_flyleaf", 2);
        focus = Wand(play).Focus;
        play.CombatDriver!.EndTurn();
        Assert.Equal(focus + 1, Wand(play).Focus);
        play.Dispose();
    }
}
