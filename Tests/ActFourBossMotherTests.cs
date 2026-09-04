using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Mother of Natron and Resin, proved in live fights.
//
// She keeps what you shed. The tests follow one jar at a time: what fills the shelf, what washing a jar
// costs and buys, what a full shelf announces and how the announcement is taken back, and what is left of
// the shelf once three jars are enough.
public class ActFourBossMotherTests
{
    private const string Cut = "paper_cut";   // Deed, 1: deal 6

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Mother(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("mother", StringComparison.Ordinal));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Energy(CombatantState c) =>
        c.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool) ? pool.Current : 0;

    private static int Shelf(RunPlayback play) =>
        FightProbe.StacksOf(Mother(play), ActFour.VesselsFilledId);

    private static bool Wears(RunPlayback play, string status) =>
        Mother(play).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // Bind the Limbs hands the player a fresh Burdened every turn, which is how the shelf is filled on
    // purpose: a Burdened spent on a card is a Burdened that left, and what leaves her is kept.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Mother) AShelfBeingFilled(
        int? maxHealth = null)
    {
        (string, int)[] carried = [(ActFour.WeighedId, 1), (ActFour.BurdenedId, 1)];

        return FightProbe.Start(
            maxHealth is { } hp
                ? FightProbe.RosterAgainstHero("mother_shelf", energy: 9, carried,
                    (ActFour.MotherEnemyId, "bind_the_limbs", hp))
                : FightProbe.SoloAgainstHero(ActFour.MotherEnemyId, "bind_the_limbs", energy: 9, carried),
            deck: [.. Enumerable.Repeat(Cut, 12)], health: 900);
    }

    // Every affliction that leaves the player goes into a jar: one spent on a card, one taken at the end of
    // the turn it stood in.
    [Fact]
    public void What_leaves_you_is_kept()
    {
        var (play, session, mother) = AShelfBeingFilled();

        Assert.Equal(0, Shelf(play));

        Play(play, session, Cut, mother);   // the Burden is spent on it
        Assert.Equal(1, Shelf(play));
        Assert.Equal(1, FightProbe.StacksOf(Mother(play), "vessel_of_the_burdened"));

        play.CombatDriver!.EndTurn();       // …and the measure is taken at the turn's end

        Assert.Equal(2, Shelf(play));
        Assert.Equal(1, FightProbe.StacksOf(Mother(play), "vessel_of_the_weighed"));
        play.Dispose();
    }

    // Washing is offered as one sheet per occupied jar, costs an Energy and an Embalmed, and is spent for
    // the turn the moment it is used.
    [Fact]
    public void One_jar_a_turn_may_be_washed()
    {
        var (play, session, mother) = AShelfBeingFilled();

        Play(play, session, Cut, mother);
        play.CombatDriver!.EndTurn();       // shelf: a Burden and a Measure

        var hand = play.CombatDriver.Current!.Hand.Select(c => c.DefinitionId.value).ToList();
        Assert.Contains("wash_the_weighed_vessel", hand);
        Assert.Contains("wash_the_burdened_vessel", hand);

        var energy = Energy(Hero(play));
        Play(play, session, "wash_the_weighed_vessel", null);

        Assert.Equal(1, Shelf(play));
        Assert.Equal(0, FightProbe.StacksOf(Mother(play), "vessel_of_the_weighed"));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(energy - 1, Energy(Hero(play)));

        // One a turn — and a sheet that does nothing costs nothing either.
        Play(play, session, "wash_the_burdened_vessel", null);
        Assert.Equal(1, Shelf(play));
        Assert.Equal(energy - 1, Energy(Hero(play)));
        play.Dispose();
    }

    // A full shelf is announced at the start of the player's turn and answered at the end of it: everything
    // stored comes back, a stack per jar, with two Embalmed on top — and the first unsealing shortens the
    // shelf for good.
    [Fact]
    public void A_full_shelf_gives_everything_back()
    {
        var (play, session, mother) = AShelfBeingFilled();

        for (var turn = 0; turn < 3; turn++)
        {
            Play(play, session, Cut, mother);
            play.CombatDriver!.EndTurn();
        }

        // Three Burdens spent and one Measure taken.
        Assert.Equal(4, Shelf(play));
        Assert.True(Wears(play, ActFour.VesselsFullId));

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(0, Shelf(play));
        Assert.False(Wears(play, ActFour.VesselsFullId));

        // Three jars are enough from here on, and the fight pauses to say so.
        Assert.True(Wears(play, ActFour.ThreeJarsId));
        Assert.Equal(14, Block(Mother(play)));
        play.Dispose();
    }

    // …and the announcement is exactly what makes the response turn real: wash one jar and there is nothing
    // full to unseal.
    [Fact]
    public void Washing_a_jar_takes_the_unsealing_back()
    {
        var (play, session, mother) = AShelfBeingFilled();

        for (var turn = 0; turn < 3; turn++)
        {
            Play(play, session, Cut, mother);
            play.CombatDriver!.EndTurn();
        }

        Assert.True(Wears(play, ActFour.VesselsFullId));
        Play(play, session, "wash_the_burdened_vessel", null);
        Assert.Equal(3, Shelf(play));

        play.CombatDriver!.EndTurn();

        // Nothing was given back, and the shelf still holds what it held.
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        Assert.Equal(3, Shelf(play));
        Assert.False(Wears(play, ActFour.ThreeJarsId));
        play.Dispose();
    }

    // 305 is the failsafe on the short shelf. A shelf of four walking into a phase that holds three loses a
    // jar, and losing it costs the player nothing.
    [Fact]
    public void The_failsafe_shortens_the_shelf_and_spills_one_jar()
    {
        // Four Deeds at 6 apiece: the fourth is the one that crosses.
        var (play, session, mother) = AShelfBeingFilled(maxHealth: 305 + (4 * 6));

        for (var turn = 0; turn < 3; turn++)
        {
            Play(play, session, Cut, mother);
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal(4, Shelf(play));
        Assert.False(Wears(play, ActFour.ThreeJarsId));

        Play(play, session, Cut, mother);   // → 305

        Assert.True(Wears(play, ActFour.ThreeJarsId));
        Assert.Equal(14, Block(Mother(play)));
        Assert.Equal(3, Shelf(play));
        play.Dispose();
    }

    // Below 90 she announces the last preparation, and what it comes to is written on her shelf: 34, and 3
    // more for every jar still standing. The jars are not emptied by it.
    [Fact]
    public void The_last_preparation_is_the_size_of_the_shelf()
    {
        var (play, session, mother) = AShelfBeingFilled(maxHealth: 96);

        var whole = Hero(play).Health.Current;
        Play(play, session, Cut, mother);   // → 90

        Assert.True(Wears(play, ActFour.LastPreparationId));

        play.CombatDriver!.EndTurn();

        var jars = Shelf(play);
        Assert.True(jars >= 2, $"the shelf should have kept something: {jars}");
        Assert.Equal(34 + (3 * jars), whole - Hero(play).Health.Current);
        Assert.False(Wears(play, ActFour.LastPreparationId));
        play.Dispose();
    }
}
