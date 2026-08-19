using BnbContent.Converter;
using RogueDeck.Core.Combat;

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
        var probe = FightProbe.Solo("blank_line_leech", 45, "blank_space_bite", ("paperwork", paperwork));
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

    [Fact]
    public void The_leechs_telegraph_spells_out_the_margin_formula()
    {
        var leech = BabData.Load(TestData.Directory).Enemies.Single(e => e.Id == "blank_line_leech");
        var bite = Assert.Single(EnemyMapper.MapActions([leech]), a => a.Id == "blank_line_leech.blank_space_bite");

        Assert.Equal("Blank-Space Bite · 8 dmg +2 per 2 own Paperwork (max +8)", bite.Intent.Label);
    }
}
