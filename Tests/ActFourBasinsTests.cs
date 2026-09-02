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

    // ── the Flood-Mark Reader ─────────────────────────────────────────────────────────────────────────────

    // A missed measure buries you one deeper; a measure met costs nothing at all.
    [Fact]
    public void Every_missed_measure_buries_the_player_one_deeper()
    {
        int BuriedAfter(bool meetTheMeasure)
        {
            var (play, session, reader) = FightProbe.Start(
                FightProbe.Solo("flood_mark_reader", "read_the_high_mark"),
                deck: [.. Enumerable.Repeat(OneCost, 10)], health: 400);

            play.CombatDriver!.EndTurn();  // Read the High Mark: spend exactly 2
            if (meetTheMeasure)
            {
                Play(play, session, OneCost, reader);
                Play(play, session, OneCost, reader);
            }
            play.CombatDriver.EndTurn();   // …the measure is taken, and the Reader reads it

            var buried = FightProbe.StacksOf(Hero(play), ActFour.EntombedId);
            play.Dispose();
            return buried;
        }

        Assert.Equal(1, BuriedAfter(meetTheMeasure: false));
        Assert.Equal(0, BuriedAfter(meetTheMeasure: true));
    }

    // …and it reads each resolution exactly once, however many turns pass with nothing new to read.
    [Fact]
    public void A_resolution_is_read_once_and_not_again()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("flood_mark_reader", "read_the_high_mark"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 400);

        play.CombatDriver!.EndTurn(); // the measure is raised
        play.CombatDriver.EndTurn();  // missed: 1 Entombed, and the demand is raised again
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        // A second missed measure is a second burial — but neither is ever counted twice.
        play.CombatDriver.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // §3.2: the Reader answers a measure it never demanded. The Crocodile's short measure, read by a body
    // from another stage entirely, buries just the same.
    [Fact]
    public void The_reader_answers_a_measure_somebody_else_demanded()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("basin_and_granary", 3,
                ("crocodile_of_the_short_measure", "short_measure", null),
                ("flood_mark_reader", "silt_lash", null)),
            health: 500);

        play.CombatDriver!.EndTurn(); // the Crocodile demands three; the Reader only lashes
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        play.CombatDriver.EndTurn();  // nothing spent: the measure is missed, and the Reader reads it

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Drowned Field Scribe ──────────────────────────────────────────────────────────────────────────

    // Shallow silt: the ledger is dry and the filing is one sheet.
    [Fact]
    public void A_dry_ledger_files_one_sheet()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("drowned_field_scribe", "silted_filing"), health: 400);

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.SiltedRecordId));

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }

    // Buried three deep, the record silts up — visibly, on the body — and the same filing goes on twice as
    // thick.
    [Fact]
    public void A_silted_ledger_files_two()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("drowned_field_scribe", "silted_filing", energy: 3,
                (ActFour.EntombedId, ActFour.SiltedRecordThreshold)),
            health: 400);

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.SiltedRecordId));

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }

    // …and the silt drains when the burial does: the threshold is a state, not a latch.
    [Fact]
    public void The_record_dries_again_when_the_burial_resolves()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("drowned_field_scribe", "mud_ledger", energy: 3,
                (ActFour.EntombedId, ActFour.EntombedThreshold)),
            health: 400);

        // Five Entombed bury the player at their turn start and are spent doing it — so the silt goes with
        // them, and the Scribe's ink thins again in the same breath.
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.SiltedRecordId));
        play.Dispose();
    }

    // Encounter 15 is the stage's own argument, in two bodies: a missed measure buries, and the burial is
    // what thickens the ink.
    [Fact]
    public void A_missed_measure_thickens_the_scribes_ink()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("faulty_flood_record", 3,
                [(ActFour.EntombedId, ActFour.SiltedRecordThreshold - 1)],
                ("flood_mark_reader", "read_the_high_mark", null),
                ("drowned_field_scribe", "silted_filing", null)),
            health: 500);

        // Two deep and the ink is still thin.
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "drowned_field_scribe"), ActFour.SiltedRecordId));
        play.CombatDriver!.EndTurn();  // the measure is demanded, one sheet filed
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));

        play.CombatDriver.EndTurn();   // missed: the Reader buries the third mark, and the ledger silts up

        Assert.Equal(ActFour.SiltedRecordThreshold, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, "drowned_field_scribe"), ActFour.SiltedRecordId));
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.Paperwork)); // 1 + a thickened 2
        play.Dispose();
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
