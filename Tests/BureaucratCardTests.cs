using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Bureaucrat's reward cards, in live fights out of the real converted game. The point of each test is a
// RULE, not a number: that a Seal converts at three, that a Queue card lands a turn later, that a conditional
// clause reads what it claims to read.
public class BureaucratCardTests
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
        params string[] deck) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 9), deck.ToList());

    // "Whenever an enemy reaches 3 Seal, remove exactly 3 Seal and trigger a Ratify event. Excess Seal
    // remains." Three applications of Seal of Concern get there; the third converts.
    [Fact]
    public void Three_seals_ratify_the_enemy_and_leave_the_excess()
    {
        var (play, session, enemyId) = Fight(Enumerable.Repeat("seal_of_concern", 10).ToArray());

        Play(play, session, "seal_of_concern", enemyId);
        Play(play, session, "seal_of_concern", enemyId);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));

        Play(play, session, "seal_of_concern", enemyId);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        play.Dispose();
    }

    // "Seal of Concern+ applies 2 Seal" — two of those is 4, which Ratifies and leaves 1 standing.
    [Fact]
    public void Seal_beyond_three_ratifies_and_the_rest_remains()
    {
        var (play, session, enemyId) = Fight(Enumerable.Repeat("seal_of_concern+", 10).ToArray());

        Play(play, session, "seal_of_concern+", enemyId);
        Play(play, session, "seal_of_concern+", enemyId);

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        play.Dispose();
    }

    // "Ratified: each Deed aimed at this enemy deals +3 total direct damage, until the end of your turn."
    [Fact]
    public void A_ratified_enemy_takes_three_more_from_every_deed_until_the_turn_ends()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9),
            ["seal_of_concern", "seal_of_concern", "seal_of_concern", "paper_cut", "paper_cut"]);

        for (var i = 0; i < 3; i++)
            Play(play, session, "seal_of_concern", enemyId);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));

        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(before - 9, Enemy(play, enemyId).Health.Current); // 6 + 3

        // The window closes when the player's turn does.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        play.Dispose();
    }

    // "Notarial Press: apply 2 Seal. If this Ratifies the target, gain 5 Block." The bonus is owed only when
    // the Press is what carries the target over.
    [Fact]
    public void The_press_pays_its_block_only_when_it_is_the_one_that_ratifies()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9),
            ["notarial_press", "notarial_press", "notarial_press"]);

        Play(play, session, "notarial_press", enemyId); // 2 Seal — not there yet
        Assert.Equal(0, Block(Hero(play)));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));

        Play(play, session, "notarial_press", enemyId); // 4 → Ratify, 1 left over
        Assert.Equal(5, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        play.Dispose();
    }

    // "Queue: Deal 13 damage." Played now, felt at the start of the next turn.
    [Fact]
    public void A_queued_hex_lands_at_the_start_of_the_next_turn()
    {
        var (play, session, enemyId) = Fight("deferred_hex", "paper_cut", "paper_cut");

        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "deferred_hex", enemyId);
        Assert.Equal(before, Enemy(play, enemyId).Health.Current);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // The Tablet spent its turn raising 10 Block, and that Block is still standing when the Queue
        // resolves at the start of the player's turn — so 13 lands as 10 absorbed and 3 through.
        Assert.Equal(before - 3, Enemy(play, enemyId).Health.Current);
        Assert.Equal(0, Block(Enemy(play, enemyId)));
        play.Dispose();
    }

    // "Occult Precedent: gain 7 Block. If ANY enemy has Paperwork, gain 2 additional Block." — once, not once
    // per such enemy, and only when there is one.
    [Fact]
    public void Occult_precedent_pays_its_bonus_once_and_only_with_paperwork_on_the_table()
    {
        var (play, session, enemyId) = Fight("occult_precedent", "permit_a38", "occult_precedent");

        Play(play, session, "occult_precedent", enemyId);
        Assert.Equal(7, Block(Hero(play)));

        Play(play, session, "permit_a38", enemyId); // 5 Paperwork onto the Tablet
        Play(play, session, "occult_precedent", enemyId);
        Assert.Equal(7 + 9, Block(Hero(play)));
        play.Dispose();
    }

    // "Certified Kindling: Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional."
    // The Archive is recorded, which is what separates it from an ordinary exhaust.
    [Fact]
    public void Certified_kindling_takes_the_junk_and_records_the_archiving()
    {
        var (play, session, enemyId) = Fight("certified_kindling", "deskward", "paper_cut");

        Play(play, session, "deskward", enemyId); // 8 Block, and a Red Tape into the discard pile
        var afterDeskward = Block(Hero(play));

        // No Junk in HAND (the Red Tape went to the discard pile), so the Kindling pays only its base and
        // asks the player which card to Archive.
        Play(play, session, "certified_kindling", enemyId);
        Assert.Equal(afterDeskward + 4, Block(Hero(play)));

        var offered = play.CombatDriver!.PendingCardChoice;
        Assert.NotNull(offered);
        play.CombatDriver.SupplyCardChoice([offered![0].Id]);
        Assert.Null(session.Error);

        // Archiving is RECORDED — that record is what separates it from an ordinary exhaust.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Archived));
        Assert.Contains(play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId)
            .ExhaustPile, c => c.Id == offered[0].Id);
        play.Dispose();
    }

    // ── Rites ─────────────────────────────────────────────────────────────────────────────────────────────
    // A Rite is a card that installs a lasting rule. The point of each test is that the rule fires ONCE per
    // turn and for the right event — not the number it pays.

    // "The first time each turn you Archive a card, draw 1 card."
    [Fact]
    public void Ash_register_answers_the_first_archiving_of_a_turn_and_no_more()
    {
        // Five cards, so the opening hand IS the deck and nothing depends on the shuffle.
        var (play, session, enemyId) = Fight(
            "ash_register", "certified_kindling", "certified_kindling", "paper_cut", "paper_cut");

        Play(play, session, "ash_register", enemyId);

        // First Archiving of the turn: the Kindling leaves the hand, the archived card leaves the hand, and
        // the Register hands one back — so the hand is one down, not two.
        var hand = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, "certified_kindling", enemyId);
        ArchiveA(play, session, "paper_cut");
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Archived));
        Assert.Equal(hand - 1, play.CombatDriver.Current!.Hand.Count);

        // Second Archiving of the SAME turn: no card comes back, so the hand is two down.
        var second = play.CombatDriver.Current!.Hand.Count;
        Play(play, session, "certified_kindling", enemyId);
        ArchiveA(play, session, "paper_cut");
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Keywords.Archived));
        Assert.Equal(second - 2, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    // "The first time each turn you Ratify an enemy, draw 1 card." The Ratify happens on the ENEMY, so the
    // rule has to watch the whole fight and reward the player who carries it.
    [Fact]
    public void Seal_dividend_answers_a_ratify_that_happens_on_the_enemy()
    {
        var (play, session, enemyId) = Fight(
            "seal_dividend", "seal_of_concern+", "seal_of_concern+", "paper_cut", "paper_cut");

        Play(play, session, "seal_dividend", enemyId);
        Play(play, session, "seal_of_concern+", enemyId); // 2 Seal — no Ratify yet

        var hand = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, "seal_of_concern+", enemyId); // 4 → Ratify
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Ratified));
        Assert.Equal(hand - 1 + 1, play.CombatDriver.Current!.Hand.Count); // played one, drew one
        play.Dispose();
    }

    // "The first Deed you play each turn costs 1 less Energy" — and only a Deed.
    [Fact]
    public void Violence_allowance_cheapens_the_first_deed_and_nothing_else()
    {
        var (play, session, enemyId) = Fight(
            "violence_allowance", "cower_behind_a_desk", "paper_cut", "paper_cut", "paper_cut");

        Play(play, session, "violence_allowance", enemyId);

        // A Working is not a Deed: it still costs its 1.
        var energy = Energy(play);
        Play(play, session, "cower_behind_a_desk", enemyId);
        Assert.Equal(energy - 1, Energy(play));

        // The first Deed is free (1 − 1); the next costs its 1 again.
        energy = Energy(play);
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(energy, Energy(play));

        energy = Energy(play);
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(energy - 1, Energy(play));
        play.Dispose();
    }

    // "At the end of your turn, retain up to 8 Block."
    [Fact]
    public void Continuance_keeps_up_to_its_ceiling_and_sheds_the_rest()
    {
        var (play, session, enemyId) = Fight(
            "continuance", "deskward", "deskward", "paper_cut", "paper_cut");

        Play(play, session, "continuance", enemyId);
        Play(play, session, "deskward", enemyId);
        Play(play, session, "deskward", enemyId);
        Assert.Equal(16, Block(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // 16 guarded, 8 kept — and the Ordinance Tablet's quiet turn took none of it.
        Assert.Equal(8, Block(Hero(play)));
        play.Dispose();
    }

    // "Deal 6 damage. If the target does not intend to Attack, apply 2 Seal; otherwise apply 1 Seal."
    [Fact]
    public void Conditional_approval_reads_the_enemys_telegraph()
    {
        // Stone Precedent is a guard, not an attack: the fuller Seal is owed.
        var (play, session, enemyId) = Fight("conditional_approval", "paper_cut", "paper_cut");
        Play(play, session, "conditional_approval", enemyId);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        play.Dispose();

        // The Embossed Seal's Heavy Impression IS an attack (the Tablet's own "mixed" intents are not):
        // only one Seal is owed.
        var attacking = FightProbe.Solo("embossed_seal", "heavy_impression", energy: 9);
        var (play2, session2, enemy2) = FightProbe.Start(attacking, ["conditional_approval", "paper_cut", "paper_cut"]);
        Play(play2, session2, "conditional_approval", enemy2);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play2, enemy2), Keywords.Seal));
        play2.Dispose();
    }

    // "Gain 5 Block. Choose one: apply 1 Doubt; or apply 1 Seal." The card raises its own prompt.
    [Fact]
    public void Clerical_discretion_lets_the_player_choose()
    {
        var (play, session, enemyId) = Fight("clerical_discretion", "paper_cut", "paper_cut");

        Play(play, session, "clerical_discretion", enemyId);
        Assert.Equal(["apply 1 Doubt", "apply 1 Seal"], play.CombatDriver!.PendingOptionChoice);

        play.CombatDriver.SupplyOptionChoice([1]); // the Seal
        Assert.Null(session.Error);

        Assert.Equal(5, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Seal));
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Doubt));
        play.Dispose();
    }

    // "Deal 6 damage, plus 3 damage for each card currently in your Queue."
    [Fact]
    public void Backlog_charge_counts_what_is_waiting()
    {
        var (play, session, enemyId) = Fight("backlog_charge", "deferred_hex", "deferred_hex", "paper_cut");

        Play(play, session, "deferred_hex", enemyId);
        Play(play, session, "deferred_hex", enemyId);

        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "backlog_charge", enemyId);
        Assert.Equal(before - (6 + 2 * 3), Enemy(play, enemyId).Health.Current);
        play.Dispose();
    }

    // "Deal 16 damage. If the target has at least 6 Paperwork, trigger its Paperwork immediately, then
    // remove 3 Paperwork." The toll ignores Block, because Paperwork always does.
    [Fact]
    public void Summary_judgment_calls_in_the_paperwork_it_finds()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("ordinance_tablet", "stone_precedent", energy: 9,
                (Keywords.Paperwork, 7)),
            ["summary_judgment", "paper_cut", "paper_cut"]);

        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "summary_judgment", enemyId);

        // 16 struck, then 7 tolled, and 3 Paperwork spent doing it.
        Assert.Equal(before - 16 - 7, Enemy(play, enemyId).Health.Current);
        Assert.Equal(4, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Paperwork));
        play.Dispose();
    }

    // "Its Paperwork does not trigger at the end of its next turn." Exactly one turn's reprieve.
    [Fact]
    public void Stay_of_execution_holds_the_paperwork_off_for_one_turn_only()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("ordinance_tablet", "stone_precedent", energy: 9, (Keywords.Paperwork, 5)),
            ["stay_of_execution", "paper_cut", "paper_cut"]);

        Play(play, session, "stay_of_execution", enemyId);
        Assert.Equal(10, Block(Hero(play))); // 2 per Paperwork

        var before = Enemy(play, enemyId).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(before, Enemy(play, enemyId).Health.Current); // the stay held

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(before - 5, Enemy(play, enemyId).Health.Current); // and is spent
        play.Dispose();
    }

    // "After an enemy takes HP loss from its Paperwork, if it survives, apply 2 Paperwork to it."
    [Fact]
    public void Red_ink_doctrine_writes_the_paperwork_deeper_every_time_it_bites()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("ordinance_tablet", "stone_precedent", energy: 9, (Keywords.Paperwork, 3)),
            ["red_ink_doctrine", "paper_cut", "paper_cut"]);

        Play(play, session, "red_ink_doctrine", enemyId);

        var before = Enemy(play, enemyId).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(before - 3, Enemy(play, enemyId).Health.Current);
        Assert.Equal(5, FightProbe.StacksOf(Enemy(play, enemyId), Keywords.Paperwork));
        play.Dispose();
    }

    // "Deal 7 damage. Archive a Junk card from your hand; if you do, repeat this attack."
    [Fact]
    public void Cinder_warrant_strikes_twice_only_when_there_is_junk_to_burn()
    {
        var (play, session, enemyId) = Fight("cinder_warrant", "tallow_budget", "cinder_warrant", "paper_cut");

        // No Junk in hand: one strike.
        var before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "cinder_warrant", enemyId);
        Assert.Equal(before - 7, Enemy(play, enemyId).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Archived));

        // Tallow Budget puts a Red Tape IN HAND; now there is something to burn.
        Play(play, session, "tallow_budget", enemyId);
        before = Enemy(play, enemyId).Health.Current;
        Play(play, session, "cinder_warrant", enemyId);
        Assert.Equal(before - 14, Enemy(play, enemyId).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Archived));
        play.Dispose();
    }

    private static int Energy(RunPlayback play) =>
        Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static void Archive(RunPlayback play, InteractiveRunSession session) =>
        ArchiveA(play, session, null);

    // Answers a pending Archive prompt, preferring a named card so a test keeps the cards it still needs.
    private static void ArchiveA(RunPlayback play, InteractiveRunSession session, string? definitionId)
    {
        var offered = play.CombatDriver!.PendingCardChoice;
        Assert.NotNull(offered);
        var pick = definitionId is null
            ? offered![0]
            : offered!.FirstOrDefault(c => c.DefinitionId.value == definitionId) ?? offered[0];
        play.CombatDriver.SupplyCardChoice([pick.Id]);
        Assert.Null(session.Error);
    }
}
