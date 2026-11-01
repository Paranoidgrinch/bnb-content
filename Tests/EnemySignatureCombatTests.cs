using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Enemy signatures whose whole point is a NUMBER, checked by letting the enemy actually act in a live fight.
public class EnemySignatureCombatTests
{

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

    // Wax Notary, "Paper Seals Wax": the FIRST Paperwork it receives each player turn seals into 5 Block; the
    // Paperwork stays and further filings that turn give nothing. Form of Ill Intent (1 Energy, 3 Paperwork)
    // files them (Broom Dispatch+ now) — the seal is about the FIRST filing of the turn, not about how thick it was.
    [Fact]
    public void The_notary_seals_the_first_paperwork_of_each_player_turn_into_block()
    {
        var probe = FightProbe.Solo("wax_notary", "notarial_mallet");
        var (play, session, notaryId) = FightProbe.Start(probe, Enumerable.Repeat(Filing, 10).ToList());

        Assert.Equal(0, BlockOf(play, notaryId));

        File(play, session, notaryId);
        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, notaryId), "paperwork")); // the filing stays
        Assert.Equal(5, BlockOf(play, notaryId));

        File(play, session, notaryId); // same turn: the seal is already spent
        Assert.Equal(6, FightProbe.StacksOf(Enemy(play, notaryId), "paperwork"));
        Assert.Equal(5, BlockOf(play, notaryId));

        play.CombatDriver!.EndTurn(); // the Notary acts; its Block clears at its own turn start
        Assert.Null(session.Error);
        Assert.Equal(0, BlockOf(play, notaryId));

        File(play, session, notaryId); // a new player turn re-arms the seal
        Assert.Equal(5, BlockOf(play, notaryId));
    }

    // Sealed Door Ward, "One Remaining Seal": while the seal holds, the first card hit each player turn deals 4
    // less — and 18+ HP damage inside one player turn breaks it for good (no recoil since 2026-11-01).
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

        // The fourth hit crosses 18 for the turn: the seal breaks.
        Cut(play, session, wardId);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, wardId), "one_remaining_seal"));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, wardId), "seal_intact"));
        Assert.Equal(56 - 20, Enemy(play, wardId).Health.Current);

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
    [InlineData(FreeForm, 9, 9)]              // a Working → the long route
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

    // Play nothing and it lists no route: a 1-damage shrug, nothing else (2026-11-01).
    [Fact]
    public void The_signpost_shrugs_when_the_player_plays_nothing()
    {
        var probe = FightProbe.Solo("contradictory_signpost", "turn_in_place");
        var (play, session, signpostId) = FightProbe.Start(probe);

        // Its answer to a player who plays nothing waits for its third turn, and it turns in place until then.
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        var heroBefore = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        var hero = Hero(play);
        Assert.Equal(heroBefore - 1, hero.Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(hero, "doubt"));
        Assert.Equal(0, FightProbe.StacksOf(hero, "paperwork"));
        Assert.Equal(0, BlockOf(play, signpostId));
    }

    // …and ten turns of nothing in a row confuse it for good: it falls over, the fight is won, and the hero carries
    // the counter the run pays its fourfold purse for.
    [Fact]
    public void Ten_idle_turns_leave_the_signpost_confused()
    {
        var probe = FightProbe.Solo("contradictory_signpost", "turn_in_place");
        var (play, session, signpostId) = FightProbe.Start(probe, health: 200);

        for (var turn = 1; turn < EncounterPassives.SignpostPatience; turn++)
        {
            play.CombatDriver!.EndTurn();
            Assert.Null(session.Error);
            Assert.True(Enemy(play, signpostId).IsAlive, $"fell after {turn} idle turns");
        }
        var fight = play.CombatDriver!.Current!;
        fight.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(RogueDeck.Core.Combat.CombatResult.Victory, fight.Result);
        Assert.Equal(1, fight.State.GetCombatant(fight.HeroId).GetCounter(EncounterPassives.SignpostConfusedCounter));
    }

    // Exception Imp, "Loophole": the first debuff the enemy side files on the player each round gives the Imp 1
    // Strength — the filing itself stands in full (since 2026-11-01).
    [Fact]
    public void The_imp_grows_on_the_first_debuff_each_round()
    {
        var probe = FightProbe.Roster("loophole",
            ("exception_imp", "technicality", null),
            ("filing_beetle", "mandatory_attachment", null));
        var (play, session, _) = FightProbe.Start(probe);

        var combat = play.CombatDriver!.Current!;
        var impId = combat.State.Combatants.First(c => c.Id.value.StartsWith("exception_imp")).Id;

        play.CombatDriver.EndTurn(); // the Beetle files 2 Paperwork
        Assert.Null(session.Error);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "paperwork"));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, impId), "strength"));

        // Second round: once more.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, impId), "strength"));
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

    // Fading Number Token, "Your Number Is Fading": while the player carries Fatigue its attacks deal 3 more.
    [Fact]
    public void The_token_hits_harder_while_the_player_is_tired()
    {
        var probe = FightProbe.Solo("fading_number_token", "sharp_edge_token");
        var (play, session, _) = FightProbe.Start(probe);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // rested: Sharp Edge as printed
        Assert.Null(session.Error);
        var rested = before - Hero(play).Health.Current;

        var tired = FightProbe.Start(FightProbe.SoloAgainstHero("fading_number_token", "sharp_edge_token", 3, ("fatigue", 2)));
        var tiredBefore = Hero(tired.Play).Health.Current;
        tired.Play.CombatDriver!.EndTurn();
        Assert.Null(tired.Session.Error);
        Assert.Equal(rested + 3, tiredBefore - Hero(tired.Play).Health.Current);
    }

    // Minute Moth, "Stolen Minute": every player turn that ends on exactly 0 Energy gives it 1 Strength and 3 Block
    // (the Block paid at its own turn end, so it stands through the player's next turn).
    [Fact]
    public void The_moth_grows_on_every_empty_turn()
    {
        var probe = FightProbe.Solo("minute_moth", "nibble_the_hour");
        var (play, session, mothId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        Spend(play, session, mothId); // three 1-energy Paper Cuts empty the pool
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, mothId), "strength"));
        Assert.Equal(3, BlockOf(play, mothId));

        // A turn that keeps an Energy back gives it nothing.
        var combat = play.CombatDriver!.Current!;
        play.CombatDriver.PlayCard(combat.Hand.First(c => c.DefinitionId.value == "paper_cut").Id, mothId);
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, mothId), "strength"));
        Assert.Equal(0, BlockOf(play, mothId));
    }

    // Queue-Crier Homunculus, "Cutting the Line": the fourth card of a turn cuts the line — 1 Panic, once a turn.
    [Fact]
    public void Playing_a_fourth_card_cuts_the_line()
    {
        var probe = FightProbe.Roster("cutting_the_line", energy: 9, ("queue_crier_homunculus", "move_everyone_back_one_place", 200));
        var (play, session, crierId) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        for (var i = 0; i < 3; i++)
            Cut(play, session, crierId);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "panic"));
        Cut(play, session, crierId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Cut(play, session, crierId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
    }

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

    // Self-Correcting Record, "Correct Against the Evidence": the first card to land 10+ on it each turn is
    // studied, and the NEXT card of that same type deals 4 less — once, then the correction is spent.
    [Fact]
    public void The_record_corrects_against_the_card_type_that_hurt_it()
    {
        // Summary Judgment: a 20-damage Deed, so the 10-damage study threshold is crossed and the type is
        // unambiguous. It was a 12-damage ported Working until the leftovers left; the Record reads the canon
        // taxonomy either way, which is why only the numbers and the type here moved.
        var probe = FightProbe.Solo("self_correcting_record", "correct_against_you", energy: 9);
        var (play, session, recordId) = FightProbe.Start(probe,
            Enumerable.Repeat(Judgment, 10).ToList());

        Disposal(play, session, recordId);
        Assert.Equal(53 - 20, Enemy(play, recordId).Health.Current); // studied, but this one lands in full
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, recordId), "correction_deed"));

        Disposal(play, session, recordId);
        Assert.Equal(53 - 20 - 16, Enemy(play, recordId).Health.Current); // corrected: 4 less
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, recordId), "correction_deed")); // and spent
        // (Summary Judgment deals 20 since playtest feedback 2: a third one would end the fight.)
    }

    private static void Disposal(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Judgment);
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
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

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static void Cut(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut");
        play.CombatDriver.PlayCard(card.Id, enemyId);
        Assert.Null(session.Error);
    }

    // The three cards these fights are conducted with, all canon: a 1-Energy Working that files 3 Paperwork,
    // a Working that is printed at 0, and a 2-Energy Deed that lands 16 — enough to cross the Record's
    // 10-damage study threshold. They replaced ported v2 cards when the leftovers left on 2026-09-10.
    private const string Filing = "broom_dispatch+";    // files 3 Paperwork and nothing else (Form of Ill Intent now files 6)
    private const string FreeForm = "secure_misfiling";
    private const string Judgment = "summary_judgment";

    private static void File(RunPlayback play, InteractiveRunSession session, CombatantId enemyId)
    {
        var form = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Filing);
        play.CombatDriver.PlayCard(form.Id, enemyId);
        Assert.Null(session.Error);
    }

    private static CombatantState Enemy(RunPlayback play, CombatantId enemyId) =>
        play.CombatDriver!.Current!.State.GetCombatant(enemyId);

    private static int BlockOf(RunPlayback play, CombatantId enemyId) =>
        Enemy(play, enemyId).DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

}
