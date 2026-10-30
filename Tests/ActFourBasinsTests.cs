using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 4 — The Floodmark Basins, proved in live fights.
//
// This is where a missed measure stops being an embarrassment and starts becoming a burial, and where the
// audit's §3.2 lands: a body may answer a measure it never demanded. The tests are therefore mostly about
// WHOSE measure it was, and about "once per resolution" holding when several bodies are listening.
public class ActFourBasinsTests
{
    private const string OneCost = "paper_cut";  // Deed, 1

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

    // ── the Silt-Buried Farmer Shade ──────────────────────────────────────────────────────────────────────

    // The water is already standing when you arrive, and every missed measure raises it one mark. At three
    // the field takes you, and the water starts again from the bank.
    [Fact]
    public void The_flood_rises_on_a_missed_measure_and_takes_the_field_at_three()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("silt_buried_farmer_shade", "keep_the_furrow"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 500);

        var farmer = Enemies(play)[0];
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.FloodId));

        play.CombatDriver!.EndTurn(); // the furrow is demanded
        play.CombatDriver.EndTurn();  // missed: the water rises to two
        Assert.Equal(2, FightProbe.StacksOf(Enemies(play)[0], ActFour.FloodId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        play.CombatDriver.EndTurn();  // missed again: the water tops the bank

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.FloodId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // Keeping the furrow does not lower the water — nothing in this act gives anything back — it holds it
    // where it stands, which is the most the design ever offers.
    [Fact]
    public void Keeping_the_furrow_holds_the_water_where_it_stands()
    {
        var (play, session, farmer) = FightProbe.Start(
            FightProbe.Solo("silt_buried_farmer_shade", "keep_the_furrow"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 500);

        play.CombatDriver!.EndTurn();  // the furrow is demanded: spend exactly 2
        Play(play, session, OneCost, farmer);
        Play(play, session, OneCost, farmer);
        play.CombatDriver.EndTurn();   // met

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.FloodId)); // held, not lowered
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }
}
