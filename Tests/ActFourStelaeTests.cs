using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV — the five words, and the Boundary Stelae that teach the first of them, proved in live fights.
//
// The vocabulary was reconstructed from usage and ratified on 2026-08-29; these tests are that ratification
// made falsifiable. Each of the five gets its own rule tested here — the measure's exact-spend comparison AND
// its error distance, the tax and its being worked off BY PAYING, the register amplifying in both directions,
// the burial at five and its reset, and preservation holding a value that would otherwise fade.
public class ActFourStelaeTests
{
    private const string OneCost = "paper_cut";   // Deed, 1
    private const string TwoCost = "permit_a38";  // 2
    private const string Wax = "waxen_surety";    // Working, 1: gain 4 Ward Wax

    // A bearer buried the moment the fight opens, so the very first turn is the lost one.
    private const int EntombedAtStart = ActFour.EntombedThreshold;

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Measure(RunPlayback play) => Hero(play).GetCounter(ActFour.MeasureResult);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── Weighed: the measure ──────────────────────────────────────────────────────────────────────────────

    // The Surveyor's measure is visible the moment it is raised — the requirement IS the stack count — and
    // spending exactly that much answers it: the record comes to 1, which is what "exact" reads as.
    [Fact]
    public void The_measure_is_visible_and_an_exact_spend_answers_it()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "set_the_measure"),
            deck: [.. Enumerable.Repeat(OneCost, 5)]);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        play.CombatDriver!.EndTurn(); // Set the Measure: 10 damage, and spend exactly 2 this turn

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        Play(play, session, OneCost, surveyor);
        Play(play, session, OneCost, surveyor);
        play.CombatDriver.EndTurn();

        Assert.Equal(1, Measure(play));  // 1 + 0: exact

        // A measure is taken ONCE: the one that resolved is gone, and what stands now is the fresh demand the
        // Surveyor raised on the turn that followed — two, not two added to the two already there.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        play.Dispose();
    }

    // …and what the act answers is the DISTANCE, not a verdict: spending nothing against a measure of two is
    // recorded as two away, spending one as one away. That difference is the whole of the Surveyor.
    [Fact]
    public void What_is_recorded_is_how_far_off_the_spend_was()
    {
        foreach (var (cardsPlayed, expected) in new[] { (0, 3), (1, 2), (2, 1) })
        {
            var (play, session, surveyor) = FightProbe.Start(
                FightProbe.Solo("reed_cord_surveyor", "set_the_measure"),
                deck: [.. Enumerable.Repeat(OneCost, 5)]);

            play.CombatDriver!.EndTurn();
            for (var i = 0; i < cardsPlayed; i++)
                Play(play, session, OneCost, surveyor);
            play.CombatDriver.EndTurn();

            Assert.Equal(expected, Measure(play));
            play.Dispose();
        }
    }

    // Overspending is an error in the same way underspending is: the measure asks for EXACTLY two, and three
    // is one away from it, exactly as one is.
    [Fact]
    public void Spending_too_much_misses_the_measure_by_just_as_much()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "set_the_measure", energy: 5),
            deck: [.. Enumerable.Repeat(OneCost, 6)]);

        play.CombatDriver!.EndTurn();
        Play(play, session, OneCost, surveyor);
        Play(play, session, OneCost, surveyor);
        Play(play, session, OneCost, surveyor);
        play.CombatDriver.EndTurn();

        Assert.Equal(2, Measure(play)); // 1 + |3 − 2|
        play.Dispose();
    }

    // ── Burdened: the tax ─────────────────────────────────────────────────────────────────────────────────

    // The tax is real Energy: a one-cost card costs two while it stands. And the stack is worked off by the
    // surcharge being PAID — the payment is written down as such, because a later enemy asks whether a burden
    // was paid off rather than merely lost.
    [Fact]
    public void The_tax_raises_what_a_card_costs_and_paying_it_works_a_stack_off()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.SoloAgainstHero("reed_cord_surveyor", "reed_lash", energy: 4,
                (ActFour.BurdenedId, 2)),
            deck: [.. Enumerable.Repeat(OneCost, 5)]);

        var before = Energy(play);
        Play(play, session, OneCost, surveyor);

        Assert.Equal(before - 2, Energy(play));                              // one Deed, two Energy
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId)); // …and one burden worked off
        Assert.Equal(1, Hero(play).GetCounter(ActFour.BurdenPaid));

        Play(play, session, OneCost, surveyor);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(2, Hero(play).GetCounter(ActFour.BurdenPaid));
        play.Dispose();
    }

    // The tax and the measure are ONE decision, not two: what the turn cost is what was actually paid, so a
    // burden standing over a measure of two turns "two one-cost cards" into an error of two.
    [Fact]
    public void The_tax_changes_what_the_turn_comes_to_and_therefore_the_measure()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.SoloAgainstHero("reed_cord_surveyor", "set_the_measure", energy: 5,
                (ActFour.BurdenedId, 2)),
            deck: [.. Enumerable.Repeat(OneCost, 5)]);

        play.CombatDriver!.EndTurn();          // spend exactly 2
        Play(play, session, OneCost, surveyor); // …but each Deed now costs 2
        Play(play, session, OneCost, surveyor);
        play.CombatDriver.EndTurn();

        Assert.Equal(3, Measure(play)); // 1 + |4 − 2|
        play.Dispose();
    }

    // ── Inscribed: the register ───────────────────────────────────────────────────────────────────────────

    // Being in the register makes the next thing that happens to you bigger — here the measure itself, which
    // arrives demanding three instead of two — and one Inscribed is spent doing it.
    [Fact]
    public void The_register_enlarges_the_next_application_and_is_spent()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("reed_cord_surveyor", "set_the_measure", energy: 3,
                (ActFour.InscribedId, 1)));

        play.CombatDriver!.EndTurn();

        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // …and it is polarity-blind, which is the act's central decision: spend the register on a blessing of
    // your own and the next curse arrives at its ordinary size.
    [Fact]
    public void The_register_enlarges_a_blessing_just_as_readily_and_is_then_gone()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.SoloAgainstHero("reed_cord_surveyor", "set_the_measure", energy: 3,
                (ActFour.InscribedId, 1)),
            deck: [.. Enumerable.Repeat(Wax, 5)]);

        // A blessing of the player's own reaches the register first: four wax become five.
        Play(play, session, Wax, surveyor);

        Assert.Equal(5, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));

        play.CombatDriver!.EndTurn();

        // …so the measure arrives at its ordinary size.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        play.Dispose();
    }

    // ── Entombed: burial ──────────────────────────────────────────────────────────────────────────────────

    // Four is pressure and nothing else. The fifth buries the turn — and five are spent doing it, so the
    // cycle can build again instead of ending the fight.
    [Fact]
    public void Five_entombed_takes_the_turn_and_is_spent()
    {
        var (play, session, surveyor) = FightProbe.Start(
            FightProbe.SoloAgainstHero("reed_cord_surveyor", "reed_lash", energy: 3,
                (ActFour.EntombedId, EntombedAtStart)),
            deck: [.. Enumerable.Repeat(OneCost, 5)]);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.True(Hero(play).Statuses.Any(s => s.DefinitionId == StandardCombatIds.StunStatus));

        // The turn is lost, which means the cards stay in hand: the play is refused, nothing is paid, and the
        // Surveyor is untouched. (The refusal is a step problem, not a session failure — a lost turn is a
        // legal thing to happen to a player, not a broken run.)
        var handBefore = play.CombatDriver!.Current!.Hand.Count;
        var energyBefore = Energy(play);
        var surveyorHealth = Enemy(play, surveyor).Health.Current;

        var card = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == OneCost);
        play.CombatDriver.PlayCard(card.Id, surveyor);

        Assert.True(session.Error is null, session.Error);
        Assert.Equal(handBefore, play.CombatDriver.Current!.Hand.Count);
        Assert.Equal(energyBefore, Energy(play));
        Assert.Equal(surveyorHealth, Enemy(play, surveyor).Health.Current);

        // …and the burial passes: the next turn is the player's own again.
        play.CombatDriver.EndTurn();
        Play(play, session, OneCost, surveyor);
        Assert.True(Enemy(play, surveyor).Health.Current < surveyorHealth);
        play.Dispose();
    }

    // ── Embalmed: preservation ────────────────────────────────────────────────────────────────────────────

    // Panic sheds a stack at every turn end. A preserved bearer sheds nothing: the Embalmed is spent instead,
    // and the fade happens again as soon as the preservation runs out.
    [Fact]
    public void Preservation_holds_a_value_that_would_otherwise_fade()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("reed_cord_surveyor", "reed_lash", energy: 3,
                ("panic", 2), (ActFour.EmbalmedId, 1)));

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "panic"));       // held
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId)); // …and paid for

        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));       // the fade returns
        play.Dispose();
    }

    // ── the Boundary Stelae ───────────────────────────────────────────────────────────────────────────────

    // The Surveyor answers by ERROR BAND: exact passes, one step away files a sheet, two or more files two.
    // That is the stage's lesson — precision, not compliance.
    [Fact]
    public void The_surveyor_answers_the_error_band_and_lets_an_exact_measure_pass()
    {
        foreach (var (cardsPlayed, sheets) in new[] { (2, 0), (1, 1), (0, 2) })
        {
            var (play, session, surveyor) = FightProbe.Start(
                FightProbe.Authored("labyrinth_stelae_01"),
                deck: [.. Enumerable.Repeat(OneCost, 12)], health: 400);

            var body = play.CombatDriver!.Current!.State.Combatants
                .First(c => c.Id != play.CombatDriver.Current!.HeroId).Id;

            play.CombatDriver.EndTurn();                    // Set the Measure
            for (var i = 0; i < cardsPlayed; i++)
                Play(play, session, OneCost, body);
            play.CombatDriver.EndTurn();                    // …the measure is taken
            play.CombatDriver.EndTurn();                    // Reed Lash
            play.CombatDriver.EndTurn();                    // Re-Tension Cord: the error is answered

            Assert.Equal(sheets, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
            play.Dispose();
        }
    }

    // The crooked standard is wrong but perfectly consistent: one, then three, then one again.
    [Fact]
    public void The_crooked_standard_alternates_one_and_three()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_stelae_02"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 400);

        var required = new List<int>();
        for (var cycle = 0; cycle < 2; cycle++)
        {
            play.CombatDriver!.EndTurn();  // Crooked Measure
            required.Add(FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
            play.CombatDriver.EndTurn();   // …the measure is taken, then Rod Strike
            play.CombatDriver.EndTurn();   // Brace the Standard
        }

        Assert.Equal([1, 3], required);
        play.Dispose();
    }

    // Encounter 3 is the audit's §3.1 in one room: the Bearer acts first and establishes the Primary Measure,
    // and the Surveyor does NOT raise a second, contradictory one — it answers the same measure's result.
    [Fact]
    public void Two_officials_raise_one_measure_between_them()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_stelae_duo_01"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 400);

        play.CombatDriver!.EndTurn(); // both act: the Bearer measures, the Surveyor merely strikes

        // One measure, and it is the Bearer's crooked standard of one — not one plus the Surveyor's two.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));
        play.Dispose();
    }

    private static int Energy(RunPlayback play) =>
        Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
}
