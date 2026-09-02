using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stages 7 and 8 — The Monument Works and The Hall of Reed and Ink, proved in live fights.
//
// Two stages of one idea: the building remembers, and so does the ink. The tests are about what each body
// records — a stone climbing where the player can count it, a foundation keeping score of compliance, a
// palette that is only fresh once a round, and a wall that is fed by nothing lapsing.
public class ActFourMonumentTests
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

    // ── the Fallen Capstone Golem ─────────────────────────────────────────────────────────────────────────

    // The installation climbs where the player can count it, and at three the stone comes down — then the
    // placement starts again, because a capstone that has already fallen can always be installed once more.
    [Fact]
    public void The_stone_climbs_where_it_can_be_counted_and_then_falls()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_monument_01"), health: 800);

        var placement = new List<int>();
        for (var turn = 0; turn < 4; turn++)
        {
            play.CombatDriver!.EndTurn();
            placement.Add(FightProbe.StacksOf(Enemies(play)[0], ActFour.PlacementId));
        }

        // 1, 2, 3 — and the fourth turn is the drop, which spends the whole placement.
        Assert.Equal([1, 2, 3, 0], placement);
        play.Dispose();
    }

    // …and what the stone weighs is the burial the player is already carrying.
    [Fact]
    public void The_capstone_falls_as_hard_as_the_burial_beneath_it()
    {
        int Falls(int entombed)
        {
            var (play, _, _) = FightProbe.Start(
                FightProbe.SoloAgainstHero("fallen_capstone_golem", "set_the_capstone", energy: 3,
                    entombed > 0 ? [(ActFour.EntombedId, entombed)] : []),
                health: 800);

            var before = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            var dealt = before - Hero(play).Health.Current;
            play.Dispose();
            return dealt;
        }

        Assert.Equal(25, Falls(0));
        Assert.Equal(25 + 8, Falls(2));
        // Four is where the cap bites: 16 would be too much, so it lands at 12. Five is not worth testing —
        // that much burial takes the player's turn before the stone can fall on it, and spends itself doing
        // so, which is the two Act-IV clocks meeting.
        Assert.Equal(25 + 12, Falls(4));
    }

    // ── the Cornerstone Oath-Stone ────────────────────────────────────────────────────────────────────────

    // A missed measure is written into the foundation, and the hammer swings by the record.
    [Fact]
    public void A_missed_measure_is_recorded_as_a_broken_oath()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("cornerstone_oath_stone", "foundation_measure"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 700);

        play.CombatDriver!.EndTurn();  // the measure is raised
        play.CombatDriver.EndTurn();   // …missed, and recorded

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.BrokenOathId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.KeptOathId));
        play.Dispose();
    }

    // A met measure is recorded too — and strikes a broken oath off the foundation, which is what compliance
    // is worth here: 4 less on the next smash.
    [Fact]
    public void A_kept_oath_strikes_a_broken_one_off_the_record()
    {
        var (play, session, stone) = FightProbe.Start(
            FightProbe.Solo("cornerstone_oath_stone", "foundation_measure"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 700);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();   // one broken oath
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.BrokenOathId));

        Play(play, session, OneCost, stone);
        Play(play, session, OneCost, stone);
        play.CombatDriver.EndTurn();   // …and a measure met

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.KeptOathId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.BrokenOathId));
        play.Dispose();
    }

    // The smash swings by the record: a broken oath is 4 more, and the record is what a missed measure wrote.
    [Fact]
    public void The_smash_swings_by_the_broken_record()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_monument_02"), health: 800);

        play.CombatDriver!.EndTurn();  // Foundation Measure: spend exactly 2
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();   // …missed, recorded as a broken oath, and the hammer follows

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.BrokenOathId));
        Assert.Equal(20 + 4, before - Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the Palette-Bearing Apprentice ────────────────────────────────────────────────────────────────────

    // The first entry of a round goes in heavier; the second is ordinary ink.
    [Fact]
    public void The_first_entry_of_a_round_goes_in_heavier()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("palette_bearing_apprentice", "fresh_pigment_entry"), health: 700);

        play.CombatDriver!.EndTurn();  // fresh pigment: 1 + 1
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));

        play.CombatDriver.EndTurn();   // a new round grinds a fresh palette: 2 more

        Assert.Equal(4, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // …and it is the scribe's OWN ink: another body writing into the register does not spend this palette.
    [Fact]
    public void Another_bodys_entry_does_not_spend_the_palette()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("hall_and_gate", 3,
                ("cobra_of_the_entry_mark", "entry_mark", null),
                ("palette_bearing_apprentice", "palette_guard", null)),
            health: 700);

        play.CombatDriver!.EndTurn(); // the Cobra marks the player; the Apprentice only guards

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId)); // plain ink
        Assert.Equal(1, FightProbe.StacksOf(
            Body(play, "palette_bearing_apprentice"), ActFour.FreshPigmentId)); // …and the palette is intact
        play.Dispose();
    }

    // ── the Hieroglyphic Complaint Wall ───────────────────────────────────────────────────────────────────

    // §3.5: the wall makes both halves of its own signature — the affliction that would fade, and the
    // preservation that stops it — so its solo fight feeds it without anybody else on the field.
    [Fact]
    public void The_wall_preserves_its_own_grievance_and_carves_it()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("hieroglyphic_complaint_wall", "preserve_the_complaint"), health: 700);

        play.CombatDriver!.EndTurn(); // 1 Panic and 1 Embalmed onto the player
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));

        play.CombatDriver.EndTurn();  // the player's turn ends: the Panic would fade, and does not

        Assert.Equal(1, Hero(play).GetCounter(ActFour.DecaysPreserved));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.ComplaintId));

        // The wall keeps its grievance alive by paying for it: one preservation, one Embalmed spent — and
        // the wall's own move puts another one on straight away, which is what makes the solo self-feeding.
        Assert.True(FightProbe.StacksOf(Hero(play), "panic") >= 1);
        play.Dispose();
    }

    // A grievance that lapses normally is no complaint at all: what the wall lives off is preservation, not
    // affliction.
    [Fact]
    public void A_grievance_that_lapses_feeds_the_wall_nothing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("hieroglyphic_complaint_wall", "carved_accusation", energy: 3,
                ("panic", 2)),
            health: 700);

        play.CombatDriver!.EndTurn(); // the Panic fades of its own accord — nothing was held

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(0, Hero(play).GetCounter(ActFour.DecaysPreserved));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.ComplaintId));
        play.Dispose();
    }

    // …and the accusation carries the complaints, 2 apiece.
    [Fact]
    public void The_accusation_carries_the_complaints()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_hall_02"), health: 900);

        play.CombatDriver!.EndTurn();  // Preserve the Complaint
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();   // …the preservation is carved, and the accusation follows

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.ComplaintId));
        Assert.Equal(18 + 2, before - Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }
}
