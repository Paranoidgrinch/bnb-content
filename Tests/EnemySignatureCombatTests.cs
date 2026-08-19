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
