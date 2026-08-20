using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Enemy signatures whose whole point is a NUMBER, checked by letting the enemy actually act in a live fight.
public class EnemySignatureCombatTests
{
    // Blank-Line Leech, "Feed on the Filed Margin": for every 2 Paperwork ON THE LEECH its attack deals +2,
    // maximum +8, and the Paperwork is not spent. 5 Paperwork = two full groups → 8 + 4 = 12.
    [Theory]
    [InlineData(0, 8)]   // no Paperwork: the plain bite
    [InlineData(3, 10)]  // 3 Paperwork = one group of 2 (whole groups only), +2
    [InlineData(5, 12)]  // two groups, +4
    [InlineData(20, 16)] // capped at +8 however filed the Leech is
    public void The_leech_bites_harder_for_every_two_paperwork_it_carries(int paperwork, int expectedDamage)
    {
        var probe = FightProbe.Solo("blank_line_leech", "blank_space_bite", ("paperwork", paperwork));
        var (play, session, enemyId) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var heroBefore = combat.State.GetCombatant(combat.HeroId).Health.Current;

        play.CombatDriver.EndTurn(); // the Leech bites, then the hero's next turn begins
        Assert.Null(session.Error);

        var after = play.CombatDriver.Current!;
        Assert.Equal(heroBefore - expectedDamage, after.State.GetCombatant(after.HeroId).Health.Current);

        // Its own Paperwork is fuel, not ammunition — the tick spends it, the bite does not.
        var leech = after.State.GetCombatant(enemyId);
        Assert.Equal(Math.Max(0, paperwork), FightProbe.StacksOf(leech, "paperwork"));
    }

    // Unsigned Form Ghost, "Still Missing a Signature": below 3 Paperwork it takes 25% less direct damage; at
    // 3+ the reduction is off. A 6-damage Paper Cut therefore lands for 4 or for 6.
    [Theory]
    [InlineData(0, 4)]
    [InlineData(2, 4)]
    [InlineData(3, 6)]
    [InlineData(9, 6)]
    public void The_ghost_shrugs_off_card_damage_until_its_paperwork_piles_up(int paperwork, int expectedDamage)
    {
        var probe = FightProbe.Solo("unsigned_form_ghost", "spectral_initial", ("paperwork", paperwork));
        var (play, session, ghostId) = FightProbe.Start(probe);

        Assert.Equal(43, play.CombatDriver!.Current!.State.GetCombatant(ghostId).Health.Current);
        PaperCut(play, session, ghostId);

        Assert.Equal(43 - expectedDamage, play.CombatDriver.Current!.State.GetCombatant(ghostId).Health.Current);
    }

    // …and the reduction comes BACK when the Ghost's own Bookworm files the Paperwork away again.
    [Fact]
    public void The_ghosts_reduction_returns_once_bookworm_files_the_paperwork_away()
    {
        var probe = FightProbe.Solo("unsigned_form_ghost", "spectral_initial",
            ("paperwork", 3), ("bookworm", 2));
        var (play, session, ghostId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn(); // the Ghost's turn: Bookworm 2 erases 2 Paperwork → 1 left, then it ticks 1
        Assert.Null(session.Error);
        var ghost = play.CombatDriver.Current!.State.GetCombatant(ghostId);
        Assert.Equal(1, FightProbe.StacksOf(ghost, "paperwork"));

        var healthBefore = ghost.Health.Current;
        PaperCut(play, session, ghostId);

        // Back under the threshold → 25% less again: 4 instead of 6.
        Assert.Equal(healthBefore - 4, play.CombatDriver.Current!.State.GetCombatant(ghostId).Health.Current);
    }

    // Plays one Paper Cut (the Bureaucrat's 6-damage starter) at the given enemy, drawing new turns until the
    // opening hand offers one.
    private static void PaperCut(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        for (var turn = 0; turn < 4; turn++)
        {
            var combat = play.CombatDriver!.Current!;
            var card = combat.Hand.FirstOrDefault(c => c.DefinitionId.value == "paper_cut");
            if (card is not null)
            {
                play.CombatDriver.PlayCard(card.Id, enemyId);
                Assert.Null(session.Error);
                return;
            }
            play.CombatDriver.EndTurn();
            Assert.Null(session.Error);
        }
        Assert.Fail("no Paper Cut reached the hand");
    }

    // Duplicate Copy Mites, "Spread Through the Binding": every living enemy gains 1 Bookworm, the Mites 2 —
    // authored as a side-wide application plus one more on themselves, which is what makes the "instead" work
    // without a second effect kind.
    [Fact]
    public void The_mites_hand_out_bookworm_and_keep_the_extra_copy()
    {
        var probe = FightProbe.Solo("duplicate_copy_mite", "spread_through_the_binding");
        var (play, session, mitesId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        var mites = play.CombatDriver.Current!.State.GetCombatant(mitesId);
        Assert.Equal(2, FightProbe.StacksOf(mites, "bookworm"));
    }

    // Wax Notary, "Paper Seals Wax": the FIRST Paperwork it receives each player turn seals into 5 Block; the
    // Paperwork stays and further filings that turn give nothing. Form 12-B (0 cost, 1 Paperwork) files them.
    [Fact]
    public void The_notary_seals_the_first_paperwork_of_each_player_turn_into_block()
    {
        var probe = FightProbe.Solo("wax_notary", "notarial_mallet");
        var (play, session, notaryId) = FightProbe.Start(probe, Enumerable.Repeat("form_12_b", 10).ToList());

        Assert.Equal(0, BlockOf(play, notaryId));

        File(play, session, notaryId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, notaryId), "paperwork")); // the filing stays
        Assert.Equal(5, BlockOf(play, notaryId));

        File(play, session, notaryId); // same turn: the seal is already spent
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, notaryId), "paperwork"));
        Assert.Equal(5, BlockOf(play, notaryId));

        play.CombatDriver!.EndTurn(); // the Notary acts; its Block clears at its own turn start
        Assert.Null(session.Error);
        Assert.Equal(0, BlockOf(play, notaryId));

        File(play, session, notaryId); // a new player turn re-arms the seal
        Assert.Equal(5, BlockOf(play, notaryId));
    }

    // Sealed Door Ward, "One Remaining Seal": while the seal holds, the first card hit each player turn deals 4
    // less — and 18+ HP damage inside one player turn breaks it for good, with 6 direct damage as recoil.
    [Fact]
    public void The_wards_seal_dampens_the_first_hit_each_turn_until_a_big_turn_breaks_it()
    {
        var probe = FightProbe.Solo("sealed_door_ward", "barred_slam", energy: 9);
        var (play, session, wardId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        // 6-damage Paper Cuts: 2 (dampened), then 6, 6 — 14 banked, seal still intact.
        Cut(play, session, wardId);
        Assert.Equal(56 - 2, Enemy(play, wardId).Health.Current);
        Cut(play, session, wardId);
        Cut(play, session, wardId);
        Assert.Equal(56 - 14, Enemy(play, wardId).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, wardId), "one_remaining_seal"));

        // The fourth hit crosses 18 for the turn: the seal breaks and takes 6 with it.
        Cut(play, session, wardId);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, wardId), "one_remaining_seal"));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, wardId), "seal_intact"));
        Assert.Equal(56 - 20 - 6, Enemy(play, wardId).Health.Current);

        // Permanently: the next player turn opens with no dampener, so a full 6 lands.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        var before = Enemy(play, wardId).Health.Current;
        Cut(play, session, wardId);
        Assert.Equal(before - 6, Enemy(play, wardId).Health.Current);
    }

    // Below the threshold the seal survives the turn and re-arms for the next one.
    [Fact]
    public void The_wards_seal_re_arms_when_the_turn_stayed_small()
    {
        var probe = FightProbe.Solo("sealed_door_ward", "barred_slam", energy: 9);
        var (play, session, wardId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        Cut(play, session, wardId);
        Cut(play, session, wardId); // 2 + 6 = 8 banked, well under 18
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, wardId), "one_remaining_seal"));
        var before = Enemy(play, wardId).Health.Current;
        Cut(play, session, wardId);
        Assert.Equal(before - 2, Enemy(play, wardId).Health.Current); // dampened again
    }

    // Oath Candle, "Witness the Seal": the first time each round ANOTHER enemy gains Block, that enemy gains 3
    // more. Driven in the Candle's canonical duo — it is never a solo, because there would be nothing to witness.
    [Fact]
    public void The_candle_tops_up_the_first_block_another_enemy_gains_each_round()
    {
        // The Ward guards itself (14), then the Candle guards the whole side (5) — two gains in ONE round.
        var probe = FightProbe.Roster("witness",
            ("sealed_door_ward", "seven_wax_seals", 39),
            ("oath_candle", "hold_the_oath", 27));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var wardId = combat.State.Combatants.First(c => c.Id.value.StartsWith("sealed_door_ward")).Id;
        var candleId = combat.State.Combatants.First(c => c.Id.value.StartsWith("oath_candle")).Id;
        Assert.Equal(39, combat.State.GetCombatant(wardId).Health.Current);
        Assert.Equal(27, combat.State.GetCombatant(candleId).Health.Current);

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        // Ward: 14 witnessed up to 17, then +5 from Hold the Oath — the second gain of the round is not
        // witnessed. Candle: its own 5, never witnessed (no recursion).
        Assert.Equal(22, BlockOf(play, wardId));
        Assert.Equal(5, BlockOf(play, candleId));
    }

    // Contradictory Signpost, "Both Directions Mandatory": the first card of the player's turn picks the road —
    // an Attack means Dangerous Shortcut (15), anything else the Long Administrative Route (9 + 9 Block), and
    // playing nothing at all means No Route Listed (1 Doubt + 2 Paperwork).
    [Theory]
    [InlineData("paper_cut", 15, 0)]          // an attack card → the shortcut
    [InlineData("form_12_b", 9, 9)]           // a form → the long route
    public void The_signpost_takes_the_road_the_first_played_card_points_at(
        string cardId, int expectedDamage, int expectedBlock)
    {
        var probe = FightProbe.Solo("contradictory_signpost", "turn_in_place");
        var (play, session, signpostId) = FightProbe.Start(probe, Enumerable.Repeat(cardId, 10).ToList());

        var combat = play.CombatDriver!.Current!;
        var heroBefore = combat.State.GetCombatant(combat.HeroId).Health.Current;
        var card = combat.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, signpostId);
        Assert.Null(session.Error);

        var heroAfterCard = play.CombatDriver.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId).Health.Current;
        play.CombatDriver.EndTurn(); // the Signpost follows the road it was pointed down
        Assert.Null(session.Error);

        var after = play.CombatDriver.Current!;
        Assert.Equal(heroAfterCard - expectedDamage, after.State.GetCombatant(after.HeroId).Health.Current);
        Assert.Equal(expectedBlock, BlockOf(play, signpostId));
    }

    [Fact]
    public void The_signpost_posts_no_route_when_the_player_plays_nothing()
    {
        var probe = FightProbe.Solo("contradictory_signpost", "turn_in_place");
        var (play, session, signpostId) = FightProbe.Start(probe);

        var heroBefore = play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        var after = play.CombatDriver.Current!;
        var hero = after.State.GetCombatant(after.HeroId);
        // No Route Listed deals no damage, and the 2 Paperwork it files do not toll yet: Paperwork resolves
        // at the END of its bearer's turn, so the hero pays for these at the end of the turn that follows.
        Assert.Equal(heroBefore, hero.Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(hero, "doubt")); // …it files instead
        Assert.Equal(2, FightProbe.StacksOf(hero, "paperwork"));
        Assert.Equal(0, BlockOf(play, signpostId));
    }

    // Exception Imp, "Loophole": the first negative status the enemy side files on the player each round loses
    // one stack — a single-stack filing is voided outright — and the Imp gains 1 Strength for the trouble.
    [Fact]
    public void The_imp_strikes_one_stack_off_the_first_debuff_each_round()
    {
        // Loophole Prick would be 1 Doubt; the probe uses the intent that files 2 Paperwork so the difference
        // between "reduced" and "prevented" is visible.
        var probe = FightProbe.Roster("loophole",
            ("exception_imp", "technicality", null),
            ("filing_beetle", "mandatory_attachment", null));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var impId = combat.State.Combatants.First(c => c.Id.value.StartsWith("exception_imp")).Id;

        play.CombatDriver.EndTurn(); // the Beetle files 2 Paperwork → Loophole strikes one
        Assert.Null(session.Error);

        var after = play.CombatDriver.Current!;
        var hero = after.State.GetCombatant(after.HeroId);
        Assert.Equal(1, FightProbe.StacksOf(hero, "paperwork"));
        Assert.Equal(1, FightProbe.StacksOf(after.State.GetCombatant(impId), "strength"));

        // Second round: the exception is available again.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        var second = play.CombatDriver.Current!;
        Assert.Equal(2, FightProbe.StacksOf(second.State.GetCombatant(second.HeroId), "paperwork")); // 1 + (2−1)
        Assert.Equal(2, FightProbe.StacksOf(second.State.GetCombatant(impId), "strength"));
    }

    // A single-stack filing is prevented completely — and that is the round's exception spent.
    [Fact]
    public void The_imp_voids_a_single_stack_filing_completely()
    {
        var probe = FightProbe.Roster("loophole_single",
            ("exception_imp", "technicality", null),
            ("wax_notary", "drip_hot_wax", null)); // 7 damage + exactly 1 Paperwork
        var (play, session, _) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        var after = play.CombatDriver.Current!;
        Assert.Equal(0, FightProbe.StacksOf(after.State.GetCombatant(after.HeroId), "paperwork"));
    }

    // Old Statute Ghost, "Still in Force": the first time each round Panic / Doubt / Fatigue vanishes from the
    // player entirely, the Ghost banks a Precedent; the second one re-files a stack of whatever just went.
    [Fact]
    public void The_ghost_re_files_the_statute_on_its_second_precedent()
    {
        // The Queue-Crier supplies the Panic; it decays at the hero's own turn end, so every round one vanishes.
        var probe = FightProbe.Roster("statute",
            ("old_statute_ghost", "ancient_penalty", null),
            ("queue_crier_homunculus", "recite_the_waiting_order", null));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var ghostId = combat.State.Combatants.First(c => c.Id.value.StartsWith("old_statute_ghost")).Id;

        play.CombatDriver.EndTurn(); // nothing to lose yet; the Crier files the first Panic
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(0, Enemy(play, ghostId).GetCounter(PassiveStatuses.PrecedentCounter));

        play.CombatDriver.EndTurn(); // it decays to nothing → first precedent; the Crier files another
        Assert.Null(session.Error);
        Assert.Equal(1, Enemy(play, ghostId).GetCounter(PassiveStatuses.PrecedentCounter));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));

        play.CombatDriver.EndTurn(); // it vanishes again → second precedent → the statute is re-imposed
        Assert.Null(session.Error);
        Assert.Equal(0, Enemy(play, ghostId).GetCounter(PassiveStatuses.PrecedentCounter));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "panic")); // 1 re-filed by the Ghost + 1 from the Crier
    }

    // Inverted Hourglass, "Stolen Sand": every time Fatigue actually costs the player Energy the Hourglass
    // banks a grain (max 3), and Turn the Glass cashes them in for 8 + 4 each.
    [Fact]
    public void The_hourglass_banks_the_energy_fatigue_steals_and_cashes_it_in()
    {
        // The Token keeps filing Fatigue; each one fires at the hero's turn start, which is the moment the
        // Energy actually goes — and the moment the Hourglass banks a grain.
        var probe = FightProbe.Roster("sand",
            ("inverted_hourglass", "turn_the_glass", null),
            ("fading_number_token", "unreadable_digit", null));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var hourglassId = combat.State.Combatants.First(c => c.Id.value.StartsWith("inverted_hourglass")).Id;
        var heroBefore = combat.State.GetCombatant(combat.HeroId).Health.Current;

        // Round 1: nothing banked yet, so Turn the Glass is a plain 8. The Token then files the first Fatigue,
        // which fires at the hero's next turn start → one grain.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(heroBefore - 8, Hero(play).Health.Current);
        Assert.Equal(1, Enemy(play, hourglassId).GetCounter(PassiveStatuses.StolenSandCounter));

        // Round 2: 8 + 4 for the banked grain, and cashing in empties the glass.
        var beforeSecond = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(beforeSecond - 12, Hero(play).Health.Current);
        Assert.Equal(1, Enemy(play, hourglassId).GetCounter(PassiveStatuses.StolenSandCounter)); // spent, then refilled
    }

    // Fading Number Token, "Your Number Is Fading": it withers by 3 at the end of each of its own turns unless
    // the player is carrying Fatigue.
    [Theory]
    [InlineData("sharp_edge_token", 3)]  // an attack that files nothing → the Token fades
    [InlineData("number_fades", 0)]      // files 1 Fatigue → it holds together
    public void The_token_fades_unless_the_player_is_kept_tired(string intentId, int expectedLoss)
    {
        var probe = FightProbe.Solo("fading_number_token", intentId);
        var (play, session, tokenId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(43 - expectedLoss, Enemy(play, tokenId).Health.Current);
    }

    // Minute Moth, "Stolen Minute": a player turn that ends on exactly 0 Energy hands it a minute; at 2 it
    // swaps its next intent for Wingbeat Delay, which spends them.
    [Fact]
    public void The_moth_collects_empty_turns_and_cashes_them_for_a_wingbeat()
    {
        var probe = FightProbe.Solo("minute_moth", "dusty_wings");
        var (play, session, mothId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        Spend(play, session, mothId); // three 1-energy Paper Cuts empty the pool
        Assert.Equal(1, Enemy(play, mothId).GetCounter(PassiveStatuses.StolenMinuteCounter));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "fatigue"));

        // The second empty turn brings the Moth to 2 — its intent rule swaps in the Wingbeat straight away.
        var before = Hero(play).Health.Current;
        Spend(play, session, mothId);
        Assert.Equal(before - 8, Hero(play).Health.Current);
        Assert.Equal(0, Enemy(play, mothId).GetCounter(PassiveStatuses.StolenMinuteCounter));
        // The Wingbeat's Fatigue has already fired by now (it does so at the hero's turn start), which is
        // exactly what it costs: this turn opens on 2 Energy instead of 3.
        Assert.Equal(2, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
    }

    // Plays until the hero's energy is spent, then ends the turn.
    private static void Spend(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        while (true)
        {
            var combat = play.CombatDriver!.Current!;
            var energy = combat.State.GetCombatant(combat.HeroId).Resources[StandardCombatIds.EnergyResource].Current;
            var card = combat.Hand.FirstOrDefault(c => c.DefinitionId.value == "paper_cut");
            if (energy <= 0 || card is null)
                break;
            play.CombatDriver.PlayCard(card.Id, enemyId);
            Assert.Null(session.Error);
        }
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
    }

    // Counterclaim Imp, "Counterclaim": the first status the player files on it each turn is answered with 1
    // Paperwork; further filings that turn are not.
    [Fact]
    public void The_counterclaim_imp_answers_the_first_filing_of_each_turn()
    {
        var probe = FightProbe.Solo("counterclaim_imp", "countersuit");
        var (play, session, impId) = FightProbe.Start(probe, Enumerable.Repeat("form_12_b", 10).ToList());

        File(play, session, impId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "paperwork")); // answered
        File(play, session, impId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "paperwork")); // same turn: no second answer

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        var afterTick = FightProbe.StacksOf(Hero(play), "paperwork");

        File(play, session, impId); // a new player turn re-arms the counterclaim
        Assert.Equal(afterTick + 1, FightProbe.StacksOf(Hero(play), "paperwork"));
    }

    // Sustaining Gavel, "Sustained": the first Block another enemy gains each round is copied at half, rounded
    // down. The Gavel is never a solo — there would be nothing to sustain.
    [Fact]
    public void The_gavel_copies_half_of_an_allys_first_block_each_round()
    {
        // Roster order is turn order, and Block clears at its owner's turn start — so the Gavel is fielded
        // FIRST (as its encounter does), otherwise the copy it makes during the Imp's turn is wiped moments
        // later by its own.
        var probe = FightProbe.Roster("sustained",
            ("sustaining_gavel", "sustained_strike", 30),
            ("counterclaim_imp", "spiteful_filing", 33)); // 8 Block for itself
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var impId = combat.State.Combatants.First(c => c.Id.value.StartsWith("counterclaim_imp")).Id;
        var gavelId = combat.State.Combatants.First(c => c.Id.value.StartsWith("sustaining_gavel")).Id;
        Assert.Equal(33, combat.State.GetCombatant(impId).Health.Current);
        Assert.Equal(30, combat.State.GetCombatant(gavelId).Health.Current);

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(8, BlockOf(play, impId));
        Assert.Equal(4, BlockOf(play, gavelId)); // half of the Imp's 8, and none of its own to copy
    }

    // Self-Correcting Record, "Correct Against the Evidence": the first card to land 10+ on it each turn is
    // studied, and the NEXT card of that same type deals 4 less — once, then the correction is spent.
    [Fact]
    public void The_record_corrects_against_the_card_type_that_hurt_it()
    {
        // Approved for Disposal: a 12-damage FORM, so the study threshold is crossed and the type is unambiguous.
        var probe = FightProbe.Solo("self_correcting_record", "correct_against_you", energy: 9);
        var (play, session, recordId) = FightProbe.Start(probe,
            Enumerable.Repeat("approved_for_disposal", 10).ToList());

        Disposal(play, session, recordId);
        Assert.Equal(53 - 12, Enemy(play, recordId).Health.Current); // studied, but this one lands in full
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, recordId), "correction_form"));

        Disposal(play, session, recordId);
        Assert.Equal(53 - 12 - 8, Enemy(play, recordId).Health.Current); // corrected: 4 less
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, recordId), "correction_form")); // and spent

        Disposal(play, session, recordId);
        Assert.Equal(53 - 12 - 8 - 12, Enemy(play, recordId).Health.Current); // no correction left this turn
    }

    private static void Disposal(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "approved_for_disposal");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    // Warrant Bailiff, "Outstanding Warrant": while the player is 4 Paperwork deep its attacks hit for 5 more.
    // The Unsigned Form Ghost files 3 at a time, so the threshold is crossed in the second round.
    [Fact]
    public void The_bailiff_serves_the_warrant_once_the_player_is_four_deep_in_paperwork()
    {
        var probe = FightProbe.Roster("warrant",
            ("warrant_bailiff", "serve_warrant", null),
            ("unsigned_form_ghost", "missing_signature", null));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var bailiffId = combat.State.Combatants.First(c => c.Id.value.StartsWith("warrant_bailiff")).Id;

        play.CombatDriver.EndTurn(); // 13 damage, then 3 Paperwork filed — still under the threshold
        Assert.Null(session.Error);
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), "paperwork"));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, bailiffId), "warrant_served"));

        play.CombatDriver.EndTurn(); // another 3 → six deep: the warrant is served
        Assert.Null(session.Error);
        Assert.Equal(6, FightProbe.StacksOf(Hero(play), "paperwork"));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, bailiffId), "warrant_served"));

        // Now it hits for 13 + 5 — plus the six Paperwork the hero is already carrying, which toll as this
        // turn ENDS. The three the Ghost files on its way out are next turn's bill.
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(before - 18 - 6, Hero(play).Health.Current);
        Assert.Equal(9, FightProbe.StacksOf(Hero(play), "paperwork"));
    }

    // Threshold Seizure Ward, "Seize the Filing": the first Paperwork the player files on an enemy each round
    // is turned against them — that enemy gains 1 Bookworm and erases the filing at its turn start.
    [Fact]
    public void The_ward_turns_the_first_filing_of_each_round_into_bookworm()
    {
        var probe = FightProbe.Solo("threshold_seizure_ward", "lawful_hold");
        var (play, session, wardId) = FightProbe.Start(probe, Enumerable.Repeat("form_12_b", 10).ToList());

        File(play, session, wardId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, wardId), "paperwork"));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, wardId), "bookworm")); // seized

        File(play, session, wardId); // same round: the seizure is spent
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, wardId), "paperwork"));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, wardId), "bookworm"));

        play.CombatDriver!.EndTurn(); // its turn: Bookworm erases one filing, the rest ticks
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, wardId), "bookworm"));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, wardId), "paperwork"));
    }

    // Civic Battering Ram: Momentum builds to 4, Ram the Case cashes it at 11 + 4 each, and breaking its guard
    // costs it a Momentum — once per player turn.
    [Fact]
    public void The_rams_momentum_builds_cashes_and_can_be_broken()
    {
        var probe = FightProbe.Solo("civic_battering_ram", "build_momentum", energy: 9);
        var (play, session, ramId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        play.CombatDriver!.EndTurn(); // Build Momentum: +2 Momentum, 10 Block
        Assert.Null(session.Error);
        Assert.Equal(2, Enemy(play, ramId).GetCounter(PassiveStatuses.MomentumCounter));
        Assert.Equal(10, BlockOf(play, ramId));

        // Two Paper Cuts strip the 10 Block exactly; the second one breaks the approach.
        Cut(play, session, ramId);
        Assert.Equal(2, Enemy(play, ramId).GetCounter(PassiveStatuses.MomentumCounter));
        Cut(play, session, ramId);
        Assert.Equal(0, BlockOf(play, ramId));
        Assert.Equal(1, Enemy(play, ramId).GetCounter(PassiveStatuses.MomentumCounter)); // broken

        Cut(play, session, ramId); // once per player turn only
        Assert.Equal(1, Enemy(play, ramId).GetCounter(PassiveStatuses.MomentumCounter));
    }

    // Number-Ticket Wisp, "Your Number Came Up": it burns out with the Panic it hands out — every stack that
    // leaves the player through Panic's own decay costs the Wisp 4 HP.
    [Fact]
    public void The_wisp_burns_down_as_its_panic_decays()
    {
        var probe = FightProbe.Solo("number_ticket_wisp", "miscalled_number"); // 5 damage + 1 Panic

        var (play, session, wispId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn(); // it files a Panic; nothing has decayed yet
        Assert.Null(session.Error);
        Assert.Equal(25, Enemy(play, wispId).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));

        play.CombatDriver.EndTurn(); // that Panic decays at the hero's turn end → the Wisp loses 4
        Assert.Null(session.Error);
        Assert.Equal(21, Enemy(play, wispId).Health.Current);
    }

    // Duplicate Copy Mites, "Carbon Copies": the first time each round another enemy gains Bookworm, the Mites
    // guard themselves for 4. Their own Spread Through the Binding hands the Notary one, which is exactly the
    // point of their duo.
    [Fact]
    public void The_mites_guard_themselves_when_an_ally_gains_bookworm()
    {
        var probe = FightProbe.Roster("carbon",
            ("duplicate_copy_mite", "spread_through_the_binding", 26),
            ("wax_notary", "notarial_mallet", 34));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var mitesId = combat.State.Combatants.First(c => c.Id.value.StartsWith("duplicate_copy_mite")).Id;
        var notaryId = combat.State.Combatants.First(c => c.Id.value.StartsWith("wax_notary")).Id;

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, notaryId), "bookworm")); // the Notary is filed for
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, mitesId), "bookworm"));  // the Mites keep the extra copy
        Assert.Equal(4, BlockOf(play, mitesId));                                 // and guard themselves once
    }

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static void Cut(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static void File(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var form = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "form_12_b");
        play.CombatDriver.PlayCard(form.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static CombatantState Enemy(RunPlayback play, CombatantId enemyId) =>
        play.CombatDriver!.Current!.State.GetCombatant(enemyId);

    private static int BlockOf(RunPlayback play, CombatantId enemyId) =>
        Enemy(play, enemyId).DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    [Fact]
    public void The_leechs_telegraph_spells_out_the_margin_formula()
    {
        var leech = BabData.Load(TestData.Directory).Enemies.Single(e => e.Id == "blank_line_leech");
        var bite = Assert.Single(EnemyMapper.MapActions([leech]), a => a.Id == "blank_line_leech.blank_space_bite");

        Assert.Equal("Blank-Space Bite · 8 dmg +2 per 2 own Paperwork (max +8)", bite.Intent.Label);
    }
}
