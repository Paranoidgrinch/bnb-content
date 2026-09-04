using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Queen of the Flood Reckoning, proved in live fights.
//
// The gauge is the fight and the player moves it, so the tests follow the river: what leftover Energy does
// to it, what the ordered middle pays for being held there, what an authority spent on it is worth and when
// it resolves, what the black mark costs, what the dry mark lends her — and what the second half does to
// all of it once the flood stops obeying.
public class ActFourBossQueenTests
{
    private const string Cut = "paper_cut";   // Deed, 1: deal 6

    private static readonly string[] Gauge =
    [
        ActFour.WaterDroughtId, ActFour.WaterExposedId, ActFour.WaterOrderedId,
        ActFour.WaterRisingId, ActFour.WaterBlackId,
    ];

    private static CombatantState Queen(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("queen_of_the_flood", StringComparison.Ordinal));

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static bool Wears(CombatantState c, string status) =>
        c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    // The reading, as a number, taken off the one mark she is standing at.
    private static int Water(RunPlayback play)
    {
        var queen = Queen(play);
        var marks = Gauge.Where(m => Wears(queen, m)).ToList();
        Assert.Single(marks);
        return Array.IndexOf(Gauge, marks[0]);
    }

    private static int Authorities(RunPlayback play) =>
        FightProbe.StacksOf(Hero(play), ActFour.SluiceAuthorityId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // Spend the whole pool on cuts, which is what tells the river to rise.
    private static void SpendItAll(RunPlayback play, InteractiveRunSession session, CombatantId queen)
    {
        while (true)
        {
            var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
            var card = play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == Cut);
            if (energy <= 0 || card is null)
                return;
            play.CombatDriver.PlayCard(card.Id, queen);
            Assert.True(session.Error is null, session.Error);
        }
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Queen) TheRiver(
        string intent = "record_the_lost_acreage", int? hp = null, int energy = 3) =>
        FightProbe.Start(
            FightProbe.Roster("flood_reckoning", energy, (ActFour.QueenEnemyId, intent, hp)),
            deck: [.. Enumerable.Repeat(Cut, 20)], health: 900);

    // §13.2: the river answers the player's own turn. Nothing left over and it rises; anything left over and
    // it falls. She opens at the Ordered Flood, and the reading is one mark and never two.
    [Fact]
    public void The_river_answers_what_you_did_not_spend()
    {
        var (play, session, queen) = TheRiver();

        Assert.Equal(2, Water(play));

        SpendItAll(play, session, queen);
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Water(play));         // nothing left: the water rises

        play.CombatDriver.EndTurn();          // a whole pool left: it falls again
        Assert.Equal(2, Water(play));

        play.CombatDriver.EndTurn();
        Assert.Equal(1, Water(play));
        play.Dispose();
    }

    // §13.3: the ordered middle costs her 12 of the Block the player is standing in front of and pays them
    // an authority — once a round. Since the river always moves at the end of a player turn, the middle can
    // only be REACHED, never held still: it is the 1 → 2 and the 3 → 2 that earn it, and here she steers the
    // river back onto it herself by closing the western sluice.
    [Fact]
    public void The_ordered_middle_strips_her_block_and_pays_an_authority()
    {
        var (play, session, queen) = TheRiver("close_the_western_sluice");

        Assert.Equal(1, Authorities(play));    // the fight opens at the middle, and it pays at once

        SpendItAll(play, session, queen);
        play.CombatDriver!.EndTurn();          // 2 → 3, then she raises 26 Block and closes the sluice: 3 → 2
        Assert.Equal(2, Water(play));

        Assert.Equal(2, Authorities(play));
        Assert.Equal(26 - 12, Block(Queen(play)));
        Assert.Contains("work_the_sluice",
            play.CombatDriver.Current!.Hand.Select(c => c.DefinitionId.value));
        play.Dispose();
    }

    // §13.4, and the whole reason the authority exists: it is DECLARED on the player's turn and resolves at
    // the end of it, AFTER the river has answered the Energy. A player standing at Rising Water who is going
    // to spend everything is one step from the black mark — and an authority spent before they know the
    // shift is what keeps them off it.
    [Fact]
    public void An_authority_spent_beats_the_energy_shift_and_calls_off_the_flood()
    {
        var (play, session, queen) = TheRiver();

        Assert.Equal(1, Authorities(play));

        SpendItAll(play, session, queen);
        play.CombatDriver!.EndTurn();          // 2 → 3: one step from the black mark
        Assert.Equal(3, Water(play));

        Play(play, session, "work_the_sluice", null);
        Assert.Equal(0, Authorities(play));

        SpendItAll(play, session, queen);
        play.CombatDriver.EndTurn();           // 3 → 4 by the Energy, then back to 3 by the sluice

        Assert.Equal(3, Water(play));
        Assert.False(Wears(Queen(play), ActFour.WaterBlackId), "the black flood was called off");
        play.Dispose();
    }

    // §13.3's Black Flood: the mark IS the queue. Her next action is the river taking the boundary, and it
    // puts the gauge back at the middle so the reading is a cycle and not a wall.
    [Fact]
    public void The_black_mark_is_the_queue_and_the_river_returns_to_the_middle()
    {
        var (play, session, queen) = TheRiver();

        SpendItAll(play, session, queen);
        play.CombatDriver!.EndTurn();
        SpendItAll(play, session, queen);

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();          // 3 → 4, and she answers it at once

        Assert.Equal(2, Water(play));
        Assert.True(before - Hero(play).Health.Current >= 38,
            $"the river took {before - Hero(play).Health.Current}");
        // One from standing at Rising Water when the turn opened, two from the river taking the boundary.
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // §13.3's Drought: the dry river lends her a Strength, and takes it back the moment the water returns.
    // It is the gauge's and not hers, which is why a second visit cannot stack a second one.
    [Fact]
    public void The_dry_river_lends_her_strength_and_takes_it_back()
    {
        var (play, session, queen) = TheRiver();

        play.CombatDriver!.EndTurn();         // 2 → 1
        play.CombatDriver.EndTurn();          // 1 → 0
        Assert.Equal(0, Water(play));
        Assert.Equal(1, FightProbe.StacksOf(Queen(play), "strength"));
        Assert.True(Wears(Queen(play), ActFour.DroughtStrengthId));

        // The dry mark is a floor: leaving Energy on the table cannot take the river below it, so climbing
        // out is the one thing a drought makes the player do — spend the pool, short as the Fatigue made it.
        play.CombatDriver.EndTurn();
        Assert.Equal(0, Water(play));

        SpendItAll(play, session, queen);
        play.CombatDriver.EndTurn();          // 0 → 1
        Assert.Equal(1, Water(play));
        Assert.Equal(0, FightProbe.StacksOf(Queen(play), "strength"));
        Assert.False(Wears(Queen(play), ActFour.DroughtStrengthId));
        play.Dispose();
    }

    // §13.6's PRIMARY trigger is not a health band: three authorities earned from the ordered middle, which
    // can only be done by moving the river onto it again and again.
    [Fact]
    public void Three_ordered_floods_stop_the_flood_obeying()
    {
        var (play, session, queen) = TheRiver();

        for (var round = 0; round < 8 && !Wears(Queen(play), ActFour.FloodDisobeysId); round++)
        {
            if (Water(play) == 1)
                SpendItAll(play, session, queen);   // 1 → 2, so her turn opens at the middle
            play.CombatDriver!.EndTurn();
        }

        Assert.True(Wears(Queen(play), ActFour.FloodDisobeysId), "the flood stopped obeying");
        Assert.False(Wears(Queen(play), ActFour.FloodStirsId), "and the telegraph is spent");
        Assert.Equal(3, Queen(play).GetCounter(ActFour.OrderedFloods));
        play.Dispose();
    }

    // §13.6's second half: the water drifts away from the middle at the end of every second turn of hers,
    // and the turn before it does she says so.
    [Fact]
    public void The_second_half_drifts_and_says_so_first()
    {
        var (play, session, queen) = TheRiver(hp: 400);

        for (var round = 0; round < 8 && !Wears(Queen(play), ActFour.FloodDisobeysId); round++)
        {
            if (Water(play) == 1)
                SpendItAll(play, session, queen);
            play.CombatDriver!.EndTurn();
        }

        // Her first turn in the second half only announces the drift …
        if (!Wears(Queen(play), ActFour.FloodDriftsId))
            play.CombatDriver!.EndTurn();
        Assert.True(Wears(Queen(play), ActFour.FloodDriftsId), "the drift is telegraphed a turn early");

        var before = Water(play);
        play.CombatDriver!.EndTurn();
        // … and the second one moves the river, on top of whatever the player's own turn did to it.
        Assert.False(Wears(Queen(play), ActFour.FloodDriftsId));
        Assert.NotEqual(before, Water(play));
        play.Dispose();
    }

    // The last count, below 90: 36, and the river moves one more step away from the middle whatever the
    // player did about it. It waits for the second half — the flood has to stop obeying first, which is why
    // the one blow that announces both is answered by the transition and not by the signature.
    [Fact]
    public void The_flood_is_counted_anyway_once_the_flood_no_longer_obeys()
    {
        var (play, session, queen) = TheRiver(hp: 89);

        Play(play, session, Cut, queen);
        Assert.True(Wears(Queen(play), ActFour.FloodStirsId));
        Assert.True(Wears(Queen(play), ActFour.FloodCountedId), "both are announced by the one blow");

        SpendItAll(play, session, queen);
        play.CombatDriver!.EndTurn();     // 2 → 3, and she answers with the transition: no attack
        Assert.True(Wears(Queen(play), ActFour.FloodDisobeysId));
        Assert.Equal(3, Water(play));

        // A turn spent standing still, so nothing of the player's own is ticking when the count lands.
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();      // 3 → 2 by the Energy left, then 36 and the river moves again

        Assert.Equal(36, before - Hero(play).Health.Current);
        Assert.NotEqual(2, Water(play));
        Assert.False(Wears(Queen(play), ActFour.FloodCountedId), "once per combat");
        play.Dispose();
    }
}
