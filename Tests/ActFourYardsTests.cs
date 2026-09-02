using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 6 — The Corvée Yards, proved in live fights.
//
// Compulsory labour, and the three things it does: a gang that answers the BREAKING of a rhythm rather than
// the tiredness itself, a conscript who wins by leaving, and a worker who turns the player's surcharges into
// stone. The tests are about the difference between a state and the moment it costs something.
public class ActFourYardsTests
{
    private const string OneCost = "paper_cut";  // Deed, 1: 6 damage

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Rope-Gang Wraith ──────────────────────────────────────────────────────────────────────────────

    // The gang answers Fatigue TAKING something, not the player being tired: a bearer with Energy loses it,
    // and the gang strains.
    [Fact]
    public void Fatigue_that_actually_takes_energy_strains_the_gang()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("rope_gang_wraith", "pull_together", energy: 3, ("fatigue", 1)),
            health: 400);

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.WorkStrainId));

        play.CombatDriver!.EndTurn(); // the player's next turn starts: Fatigue takes an Energy

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.WorkStrainId));
        Assert.Equal(1, Hero(play).GetCounter(ActFour.EnergyTakenByFatigue));
        play.Dispose();
    }

    // …and Fatigue that takes NOTHING leaves the rhythm intact. That is the distinction the identity is built
    // on, and it is a real one even though it is rare: Energy refills at the player's turn start BEFORE
    // Fatigue bites, so a player who merely spent everything last turn still has something to lose. What
    // there is nothing to take from is a turn with no Energy to refill into at all.
    [Fact]
    public void Fatigue_that_takes_nothing_leaves_the_rhythm_alone()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("rope_gang_wraith", "pull_together", energy: 0, ("fatigue", 2)),
            health: 400);

        play.CombatDriver!.EndTurn();

        Assert.Equal(0, Hero(play).GetCounter(ActFour.EnergyTakenByFatigue));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.WorkStrainId));
        play.Dispose();
    }

    // The snap carries the strain, and spends it doing so: the same rope-snap twice is not the same blow.
    [Fact]
    public void The_snap_carries_the_strain_and_spends_it()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("rope_gang_wraith", "rope_snap", energy: 3, ("fatigue", 1)),
            health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // Fatigue bites at the player's turn start; the gang strains and snaps

        var strained = before - Hero(play).Health.Current;
        Assert.Equal(26, strained);   // 20 + 6
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.WorkStrainId));

        var next = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();  // no Fatigue left to break the step: the plain snap

        Assert.Equal(20, next - Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the Stone-Hauler Ushabti ──────────────────────────────────────────────────────────────────────────

    // Every card the player overpaid for is a stone, collected when the worker comes round — and the blow
    // carries them, up to the cap.
    [Fact]
    public void Every_surcharge_paid_becomes_a_stone_and_the_blow_carries_them()
    {
        var (play, session, ushabti) = FightProbe.Start(
            FightProbe.SoloAgainstHero("stone_hauler_ushabti", "stone_blow", energy: 9,
                (ActFour.BurdenedId, 2)),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 500);

        Play(play, session, OneCost, ushabti); // taxed
        Play(play, session, OneCost, ushabti); // taxed

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();          // two stones hauled, and the blow carries them

        Assert.Equal(2, FightProbe.StacksOf(Enemies(play)[0], ActFour.StoneId));
        Assert.Equal(19 + 6, before - Hero(play).Health.Current);
        play.Dispose();
    }

    // An unburdened turn quarries nothing: the worker lives off the surcharge, not off the cards.
    [Fact]
    public void An_untaxed_turn_quarries_nothing()
    {
        var (play, session, ushabti) = FightProbe.Start(
            FightProbe.Solo("stone_hauler_ushabti", "stone_blow", energy: 3),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 500);

        Play(play, session, OneCost, ushabti);
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.StoneId));
        Assert.Equal(19, before - Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the Runaway Laborer ───────────────────────────────────────────────────────────────────────────────

    // Breaking the gang's bracing during your turn gets the conscript one step closer — and it is once per
    // turn, because what it reads is the brace that stood when your turn began.
    [Fact]
    public void Breaking_the_gangs_brace_gets_the_conscript_one_step_closer()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("break_the_gang", 9,
                ("runaway_laborer", "desperate_swing", null),
                ("stone_hauler_ushabti", "brace_the_stone", 40)),
            deck: [.. Enumerable.Repeat(OneCost, 30)], health: 600);

        play.CombatDriver!.EndTurn(); // the Ushabti braces: 25 Block

        var ushabti = Body(play, "stone_hauler_ushabti").Id;
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "runaway_laborer"), ActFour.EscapeId));

        // …and the player breaks all of it inside one turn.
        for (var i = 0; i < 5; i++)
            Play(play, session, OneCost, ushabti);
        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Body(play, "runaway_laborer"), ActFour.EscapeId));
        play.Dispose();
    }

    // A gang that was never braced cannot be broken: a quiet turn is not an escape.
    [Fact]
    public void An_unbraced_gang_is_no_escape_at_all()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("break_the_gang", 9,
                ("runaway_laborer", "desperate_swing", null),
                ("stone_hauler_ushabti", "haul_stone", 40)),
            deck: [.. Enumerable.Repeat(OneCost, 30)], health: 600);

        var ushabti = Body(play, "stone_hauler_ushabti").Id;
        Play(play, session, OneCost, ushabti);
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Body(play, "runaway_laborer"), ActFour.EscapeId));
        play.Dispose();
    }

    // Twice, and the conscript is simply gone — not killed. With nothing left holding the room, the fight is
    // resolved.
    [Fact]
    public void Two_broken_braces_and_the_conscript_leaves()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("break_the_gang", 9,
                ("runaway_laborer", "desperate_swing", null),
                ("stone_hauler_ushabti", "brace_the_stone", 200)),
            deck: [.. Enumerable.Repeat(OneCost, 40)], health: 800);

        for (var round = 0; round < 2; round++)
        {
            play.CombatDriver!.EndTurn(); // the Ushabti braces
            var ushabti = Body(play, "stone_hauler_ushabti").Id;
            for (var i = 0; i < 5; i++)
                Play(play, session, OneCost, ushabti);
            play.CombatDriver.EndTurn();  // …and the brace is gone by the end of the turn
        }

        var laborer = Enemies(play).First(c =>
            c.DefinitionId.value.Contains("runaway_laborer", StringComparison.Ordinal));
        Assert.False(laborer.IsAlive);
        play.Dispose();
    }
}
