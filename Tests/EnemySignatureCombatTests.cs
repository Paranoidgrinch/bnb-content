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

    [Fact]
    public void The_leechs_telegraph_spells_out_the_margin_formula()
    {
        var leech = BabData.Load(TestData.Directory).Enemies.Single(e => e.Id == "blank_line_leech");
        var bite = Assert.Single(EnemyMapper.MapActions([leech]), a => a.Id == "blank_line_leech.blank_space_bite");

        Assert.Equal("Blank-Space Bite · 8 dmg +2 per 2 own Paperwork (max +8)", bite.Intent.Label);
    }
}
