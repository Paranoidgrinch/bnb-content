using BnbContent.Converter.Apprentice;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The sharpening pass after the content audit (2026-10-21): each test pins the DIRECTION of one change, so it is
// red against the card or relic as it was before. The Hearth brew, the Iron Astrolabe and the Crown are pinned
// beside their kin (HedgeWitchTests, HeldEnergyTests, ActFourBossRelicTests).
public class SharpeningPassTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Fight(
        string[] deck, string? character = null, IReadOnlyList<string>? relics = null) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 9), deck, character: character, relics: relics);

    private static string Text(string cardId) =>
        FightProbe.Game.Cards.Single(c => c.Id == cardId).DescriptionKey;

    // ── Bureaucrat ─────────────────────────────────────────────────────────────────────────────────────────

    // Candle Tribunal+ hits for 7 (was 9): ratified, it was 108 for 2 Energy.
    [Fact]
    public void Candle_tribunal_plus_hits_for_seven()
    {
        var (play, session, enemy) = Fight(["candle_tribunal+", "paper_cut", "paper_cut", "paper_cut", "paper_cut"]);
        var before = Enemy(play, enemy).Health.Current;
        Play(play, session, "candle_tribunal+", enemy);
        Assert.Equal(before - 21, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    // Seal Dividend+ draws two (it used to cost 1 less and draw one). Two Paper Cuts go first, so the discard
    // pile has cards to give when the five-card deck has run dry.
    [Fact]
    public void Seal_dividend_plus_draws_two()
    {
        var (play, session, enemy) = Fight(["seal_dividend+", "seal_of_concern+", "seal_of_concern+", "paper_cut", "paper_cut"]);
        Play(play, session, "seal_dividend+", enemy);
        Play(play, session, "paper_cut", enemy);
        Play(play, session, "paper_cut", enemy);
        Play(play, session, "seal_of_concern+", enemy);
        Play(play, session, "seal_of_concern+", enemy);            // 4 Seal → Ratify, from an empty hand
        Assert.Equal(2, play.CombatDriver!.Current!.Hand.Count);
        play.Dispose();
    }

    // Dead Letter Office reads the discard pile too, at 2 Paperwork a Junk type. Off the Exhaust pile alone this
    // fight would have filed nothing.
    [Fact]
    public void Dead_letter_office_counts_the_junk_in_the_discard_pile()
    {
        var (play, session, enemy) = Fight(
            ["cauldron_copy", "deskward", "dead_letter_office", "paper_cut", "paper_cut"]);
        Play(play, session, "cauldron_copy", enemy);               // Duplicate Copy into the discard pile
        Play(play, session, "deskward", enemy);                    // Red Tape into the discard pile
        Play(play, session, "dead_letter_office", enemy);
        Assert.Equal(4, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Paperwork));
        play.Dispose();
    }

    // Wastepaper Bastion counts the Junk in the discard pile as well as the hand.
    [Fact]
    public void Wastepaper_bastion_counts_the_junk_in_the_discard_pile()
    {
        var (play, session, enemy) = Fight(["deskward", "wastepaper_bastion", "paper_cut", "paper_cut", "paper_cut"]);
        Play(play, session, "deskward", enemy);                    // 8 Block, Red Tape into the discard pile
        Play(play, session, "wastepaper_bastion", enemy);          // 5 + 2 for the one Red Tape
        Assert.Equal(8 + 7, Block(Hero(play)));
        play.Dispose();
    }

    // Presumption of Error costs nothing; its upgrade applies 2 Doubt instead of costing less.
    [Fact]
    public void Presumption_of_error_is_free_and_its_upgrade_doubts_twice()
    {
        var (play, session, enemy) = Fight(["presumption_of_error+", "paper_cut", "paper_cut", "paper_cut", "paper_cut"]);
        var energy = play.CombatDriver!.Current!.HeroEnergy;
        Play(play, session, "presumption_of_error+", enemy);
        Assert.Equal(energy, play.CombatDriver.Current!.HeroEnergy);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Doubt));
        Assert.Contains("Apply 2 Doubt", Text("presumption_of_error+"));
        play.Dispose();
    }

    // Form is printed on the cards the Form relics reward.
    [Theory]
    [InlineData("permit_a38")]
    [InlineData("form_of_ill_intent+")]
    [InlineData("broom_dispatch")]
    public void A_form_says_so(string cardId) =>
        Assert.StartsWith("Form. ", Text(cardId));

    // ── Apprentice ─────────────────────────────────────────────────────────────────────────────────────────

    // Final Draft pays 2 for each Rote and Poise (was 1).
    [Fact]
    public void Final_draft_pays_two_for_each_rote()
    {
        var (play, session, enemy) = Fight(
            ["ap_recitation", "ap_final_draft", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"],
            character: ApprenticeCharacter.Id);
        Play(play, session, "ap_recitation", enemy);               // 2 Rote
        var before = Enemy(play, enemy).Health.Current;
        Play(play, session, "ap_final_draft", enemy);
        Assert.Equal(before - (14 + 2 * 2), Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    // Lectern gives Poise as well as Rote, so it is no longer a dearer Recitation.
    [Fact]
    public void Lectern_gives_rote_and_poise()
    {
        var (play, session, enemy) = Fight(
            ["ap_lectern", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub", "ap_candle_stub"],
            character: ApprenticeCharacter.Id);
        Play(play, session, "ap_lectern", enemy);
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ApprenticeKeywords.Rote));
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ApprenticeKeywords.Poise));
        play.Dispose();
    }

    // His six cards with a lasting effect are Rites, so everything that reads Rites sees them.
    [Theory]
    [InlineData("ap_copybook")]
    [InlineData("ap_borrowed_grimoire")]
    [InlineData("ap_burning_glass")]
    [InlineData("ap_masters_hat")]
    [InlineData("ap_examiners_bell")]
    [InlineData("ap_apprentices_oath")]
    public void His_lasting_cards_are_rites(string cardId) =>
        Assert.Contains(FightProbe.Game.Cards.Single(c => c.Id == cardId).Tags, t => t.value == CardAuthoring.RiteTag);

    // ── Second round: O2–O10 of SHARPENING_PLAN.md ────────────────────────────────────────────────────────

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Against(
        string[] deck, string? character, params (string Status, int Stacks)[] enemyStatuses) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, 9, enemyStatuses), deck, character: character);

    // Last Office counts every negative status on its target, whoever put it there: a Hexed target takes one
    // share less, for the Witch as for anyone (it used to read only five named Bureaucrat and general statuses).
    [Fact]
    public void Last_office_counts_the_witchs_hexed_too()
    {
        var (play, session, enemy) = Against(
            ["last_office", "adders_nip", "adders_nip", "adders_nip", "adders_nip"],
            BnbContent.Converter.Witch.WitchCharacter.Id, (BnbContent.Converter.Witch.WitchKeywords.Hexed, 3));
        var before = Enemy(play, enemy).Health.Current;
        Play(play, session, "last_office", enemy);
        Assert.Equal(before - 32, Enemy(play, enemy).Health.Current);
        Assert.Equal(12, Block(Hero(play)));
        play.Dispose();
    }

    // Black Tribunal reads Hexed and Misfortune now: 14 + 8 for each of the two.
    [Fact]
    public void Black_tribunal_counts_the_witchs_statuses()
    {
        var (play, session, enemy) = Against(
            ["black_tribunal", "adders_nip", "adders_nip", "adders_nip", "adders_nip"],
            BnbContent.Converter.Witch.WitchCharacter.Id,
            (BnbContent.Converter.Witch.WitchKeywords.Hexed, 3), (BnbContent.Converter.Witch.WitchKeywords.Misfortune, 2));
        var before = Enemy(play, enemy).Health.Current;
        Play(play, session, "black_tribunal", enemy);
        Assert.Equal(before - 30, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    // Continuance+ keeps up to 20 Block (it used to keep 12 and cost less).
    [Fact]
    public void Continuance_plus_keeps_twenty()
    {
        var (play, session, enemy) = Fight(["continuance+", "deskward+", "deskward+", "deskward+", "paper_cut"]);
        Play(play, session, "continuance+", enemy);
        for (var i = 0; i < 3; i++)
            Play(play, session, "deskward+", enemy);
        play.CombatDriver!.EndTurn();
        Assert.Equal(20, Block(Hero(play)));
        play.Dispose();
    }

    // The general pool's new neutral tools.
    [Fact]
    public void Second_wind_pays_an_energy_and_a_card()
    {
        var (play, session, enemy) = Fight(["sanctioned_charm", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"]);
        Play(play, session, "paper_cut", enemy);                     // the probe's pool may not pass its max
        var energy = play.CombatDriver!.Current!.HeroEnergy;
        var hand = play.CombatDriver.Current.Hand.Count;
        Play(play, session, "sanctioned_charm", enemy);
        Assert.Equal(energy + 1, play.CombatDriver.Current!.HeroEnergy);
        Assert.Equal(hand, play.CombatDriver.Current.Hand.Count);   // played one, drew one
        Assert.Equal("Second Wind", FightProbe.Game.Cards.Single(c => c.Id == "sanctioned_charm").NameKey);
        play.Dispose();
    }

    [Fact]
    public void Blunt_instrument_hits_for_fourteen()
    {
        var (play, session, enemy) = Fight(["blacklisted", "paper_cut", "paper_cut", "paper_cut", "paper_cut"]);
        var before = Enemy(play, enemy).Health.Current;
        Play(play, session, "blacklisted", enemy);
        Assert.Equal(before - 14, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Steady_routine_guards_every_turn()
    {
        var (play, session, enemy) = Fight(["countermanded_grace", "paper_cut", "paper_cut", "paper_cut", "paper_cut"]);
        Play(play, session, "countermanded_grace", enemy);
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Fresh_ledger_exhausts_one_card_and_draws_two()
    {
        var (play, session, enemy) = Fight(
            ["crossed_sigil", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"]);
        var hand = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, "crossed_sigil", enemy);
        var offered = play.CombatDriver.PendingCardChoice;
        Assert.NotNull(offered);
        Assert.DoesNotContain(offered!, c => c.DefinitionId.value == "crossed_sigil");
        play.CombatDriver.SupplyCardChoice([offered![0].Id]);
        Assert.Null(session.Error);
        Assert.Equal(hand - 2 + 2, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    // The Bureaucrat's new shop relics bend his strands.
    [Fact]
    public void Second_opinion_adds_a_doubt()
    {
        var (play, session, enemy) = Fight(["strong_binder", "paper_cut", "paper_cut", "paper_cut", "paper_cut"],
            relics: ["departmental_purchase_order"]);
        Play(play, session, "strong_binder", enemy);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Doubt));
        play.Dispose();
    }

    [Fact]
    public void Assent_ribbon_files_against_a_ratified_enemy()
    {
        var (play, session, enemy) = Fight(["waxing_authority", "waxing_authority", "waxing_authority", "paper_cut", "paper_cut"],
            relics: ["archive_voucher_roll"]);
        for (var i = 0; i < 3; i++)
            Play(play, session, "waxing_authority", enemy);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Doubt));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Paperwork));
        play.Dispose();
    }

    [Fact]
    public void Priority_window_pass_files_when_the_queue_resolves()
    {
        var (play, session, enemy) = Fight(["protective_adjournment", "paper_cut", "paper_cut", "paper_cut", "paper_cut"],
            relics: ["priority_window_pass"]);
        Play(play, session, "protective_adjournment", enemy);
        play.CombatDriver!.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Paperwork));
        play.Dispose();
    }

    // The general shop's new relics: a rule with a price.
    [Fact]
    public void Glass_inkwell_sharpens_attacks()
    {
        var (play, session, enemy) = Fight(["paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"],
            relics: ["copper_receipt_roll"]);
        var before = Enemy(play, enemy).Health.Current;
        Play(play, session, "paper_cut", enemy);
        Assert.True(before - Enemy(play, enemy).Health.Current > 6);
        play.Dispose();
    }

    [Fact]
    public void Iron_thimble_takes_one_off_every_block()
    {
        var (play, session, enemy) = Fight(["cower_behind_a_desk", "paper_cut", "paper_cut", "paper_cut", "paper_cut"],
            relics: ["witchmarket_purse"]);
        Play(play, session, "cower_behind_a_desk", enemy);
        Assert.Equal(4, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Stale_loaf_costs_the_first_turn_and_pays_the_next()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent),
            ["paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut",
             "paper_cut", "paper_cut"], relics: ["merchant_punchcard"]);
        Assert.Equal(1, play.CombatDriver!.Current!.HeroEnergy);
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    [Fact]
    public void Pawned_wedding_ring_opens_with_strength()
    {
        var (play, _, _) = Fight(["paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"],
            relics: ["warranty_tag"]);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "strength"));
        play.Dispose();
    }

    // ── Rewards ────────────────────────────────────────────────────────────────────────────────────────────

    // General cards take about a quarter of an Act-I reward and a third of an Act-IV one, for every character —
    // the Witch's Act-IV offers were 60 % general before.
    [Theory]
    [InlineData(1, 0.22, 0.28)]
    [InlineData(4, 0.29, 0.34)]
    public void General_cards_take_a_bounded_share_of_every_reward(int act, double low, double high)
    {
        var pools = BnbContent.Converter.ConversionPools.Build(act);
        var source = Assert.IsType<RogueDeck.Run.PoolRewardSource>(pools.CardRewardSource());
        var generalIds = pools.GeneralCards.Select(c => "card-" + c.Id).ToHashSet();
        foreach (var who in new[] { BnbContent.Converter.ConversionPools.BureaucratId,
                     BnbContent.Converter.Witch.WitchCharacter.Id, ApprenticeCharacter.Id })
        {
            var entries = source.Pool.Entries
                .Where(e => e.Value.Tags?.Contains(BnbContent.Converter.ConversionPools.For(who)) == true).ToList();
            var share = entries.Where(e => generalIds.Contains(e.Value.Id)).Sum(e => (double)e.Weight)
                / entries.Sum(e => (double)e.Weight);
            Assert.InRange(share, low, high);
        }
    }

    // ── Relics ─────────────────────────────────────────────────────────────────────────────────────────────

    // The Brass Service Bell rings every second turn (was every third), so the boss relic stays above the Iron
    // Astrolabe, which now pays every second turn too.
    [Fact]
    public void The_bell_rings_on_the_second_turn()
    {
        var (play, _, _) = Fight(
            ["paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut",
             "paper_cut", "paper_cut"], relics: ["brass_service_bell"]);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // The mimic's tooth says what it gives.
    [Fact]
    public void Each_tooth_names_its_block()
    {
        var teeth = BnbContent.Converter.Relics.FinalRelics.All()
            .Where(r => r.Id.StartsWith("tooth_from_the_false_lid_", StringComparison.Ordinal)).ToList();
        Assert.Equal(4, teeth.Count);
        Assert.All(teeth, t => Assert.Contains(" Block", t.Text));
    }
}
