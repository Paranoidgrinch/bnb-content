using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stages 16 and 17 — the final forms, proved in live fights.
//
// No new vocabulary is introduced here, and that is what the tests are for: each of these five bodies is an
// earlier one promoted, and every word it uses — the measure and its distance, Stone, preservation, Kept and
// Broken Oaths — is one the player was already taught by the body now holding the office.
public class ActFourBalanceTests
{
    private const string OneCost = "paper_cut";  // Deed, 1: deal 6

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

    private static IReadOnlyList<string> Cuts => [.. Enumerable.Repeat(OneCost, 12)];

    // ── the Feather-Bearer ────────────────────────────────────────────────────────────────────────────────

    // The Crooked Rod Bearer spent the whole act measuring against a standard that was wrong. Its final form
    // measures true — and answers a miss by the DISTANCE, which is the Reed-Cord Surveyor's lesson delivered
    // by the body that had been cheating it. 16, and 5 more for every point out, up to 31.
    [Theory]
    [InlineData(0, 31)]  // three out, and the cap is exactly there
    [InlineData(1, 26)]
    [InlineData(2, 21)]
    public void A_missed_final_measure_is_answered_by_the_distance(int cardsPlayed, int expected)
    {
        var (play, session, bearer) = FightProbe.Start(
            FightProbe.Solo("feather_bearer", "true_balance"), deck: Cuts, health: 800);

        play.CombatDriver!.EndTurn();  // the feather is raised: spend exactly 3
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        for (var i = 0; i < cardsPlayed; i++)
            Play(play, session, OneCost, bearer);

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();

        Assert.Equal(expected, before - Hero(play).Health.Current);
        play.Dispose();
    }

    // Meet it exactly and the scale gives you a look at what is holding it: one player turn in which every
    // blow lands 8 deeper — and then the feather is raised again and the window closes.
    [Fact]
    public void An_exact_measure_opens_the_balance_for_exactly_one_turn()
    {
        var (play, session, bearer) = FightProbe.Start(
            FightProbe.Solo("feather_bearer", "true_balance"), deck: Cuts, health: 800);

        play.CombatDriver!.EndTurn();
        for (var i = 0; i < 3; i++)
            Play(play, session, OneCost, bearer);  // exactly 3 Energy

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();

        Assert.Equal(before, Hero(play).Health.Current);  // an exact measure is never answered with a blow
        Assert.Equal(1, FightProbe.StacksOf(Body(play, "feather_bearer"), ActFour.BalanceOpenId));

        // 6 damage a cut becomes 14 while the balance is open.
        var standing = Body(play, "feather_bearer").Health.Current;
        Play(play, session, OneCost, bearer);
        Assert.Equal(14, standing - Body(play, "feather_bearer").Health.Current);

        // …and the window is one turn wide: the next weighing closes it before it is answered.
        play.CombatDriver.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "feather_bearer"), ActFour.BalanceOpenId));
        play.Dispose();
    }

    // ── the Crocodile Beneath the Balance ─────────────────────────────────────────────────────────────────

    // The grain-measure crocodile waits under the final scale now, and one of its two known conditions is
    // burial: a player already three deep opens the jaws without any weighing at all.
    [Fact]
    public void Burial_alone_opens_the_jaws_and_the_bite_closes_them()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("crocodile_beneath_the_balance", "jaws_closed", energy: 3,
                (ActFour.EntombedId, 3)),
            health: 800);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // 29 + 3 per Entombed: the bite that counts, and then the jaws close again.
        Assert.Equal(29 + 9, before - Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "crocodile_beneath_the_balance"), ActFour.JawsOpenId));
        play.Dispose();
    }

    // A player carrying nothing gets the closed jaws — 20, and no signature at all.
    [Fact]
    public void Closed_jaws_are_an_ordinary_bite()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("crocodile_beneath_the_balance", "jaws_closed"), health: 800);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(20, before - Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the Golden Ushabti Captain ────────────────────────────────────────────────────────────────────────

    // The same Stone, quarried the same way out of what the bureaucracy made you overpay — and now spent on
    // the Court instead of swung. Every body of the labyrinth is braced, and the quarry is emptied doing it.
    [Fact]
    public void The_captain_braces_the_whole_court_with_its_stone_and_spends_it()
    {
        var (play, session, captain) = FightProbe.Start(
            FightProbe.RosterAgainstHero("eternal_shift", energy: 6, [(ActFour.BurdenedId, 2)],
                ("golden_ushabti_captain", "command_brace", null),
                ("eternal_reed_scribe", "reed_ward", null)),
            deck: Cuts, health: 800);

        Play(play, session, OneCost, captain);
        Play(play, session, OneCost, captain);  // two surcharges paid: two Stones to quarry
        play.CombatDriver!.EndTurn();

        var officer = Body(play, "golden_ushabti_captain");
        Assert.Equal(0, FightProbe.StacksOf(officer, ActFour.StoneId));  // the quarry is emptied

        // 12 to each body of the Court, and 4 more per Stone — the scribe is braced by the Captain's stone
        // as much as the Captain is.
        Assert.Equal(12 + 8, BlockOf(officer));
        Assert.True(BlockOf(Body(play, "eternal_reed_scribe")) >= 12 + 8,
            "the Court's other body was not braced by the Captain's command");
        play.Dispose();
    }

    // ── the Eternal Reed Scribe ───────────────────────────────────────────────────────────────────────────

    // "The first important negative status application by the enemy side each round receives Preserved
    // Entry." Which is Embalmed — the act's own preservation language, one stack, so exactly one fade is
    // held and nothing becomes permanent.
    [Fact]
    public void The_courts_first_entry_of_a_round_does_not_close()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle("eternal_reed_scribe", "unclosing_entry", "eternal_script", "reed_ward"),
            health: 800);

        play.CombatDriver!.EndTurn();  // 15 damage and 1 Panic — the entry, and the preservation with it
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));

        // The Panic that should have faded is held instead, and the preservation is spent holding it — one
        // stack, one fade. Nothing here becomes permanent.
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));

        // …and with the preservation spent, the entry finally closes.
        play.CombatDriver.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "panic"));
        play.Dispose();
    }

    // ── the Oathbound Gate ────────────────────────────────────────────────────────────────────────────────

    // The final door arrives with what it remembers of the route already carved into it — visible before the
    // first player action, and capped at the two tokens the audit allows.
    [Fact]
    public void The_door_arrives_remembering_two_broken_oaths()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_sealed_court_01"), health: 800);

        Assert.Equal(2, FightProbe.StacksOf(Body(play, "oathbound_gate"), ActFour.BrokenOathId));
        play.Dispose();
    }

    // …and it keeps recording, in exactly the Oath-Stone's language: a missed compliance check is another
    // Broken Oath on top of the two it walked in with, and the judgment swings 4 harder for each, to a cap.
    [Fact]
    public void The_door_keeps_recording_compliance_and_judges_by_the_whole_record()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle("oathbound_gate", "read_the_oath", "broken_oath_judgment"), health: 900);

        Assert.Equal(2, FightProbe.StacksOf(Body(play, "oathbound_gate"), ActFour.BrokenOathId));

        play.CombatDriver!.EndTurn();  // Read the Oath: 16 damage, spend exactly 2
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();  // nothing spent: a third Broken Oath, and then the judgment

        Assert.Equal(3, FightProbe.StacksOf(Body(play, "oathbound_gate"), ActFour.BrokenOathId));
        Assert.Equal(27 + 12, before - Hero(play).Health.Current);  // 4 apiece, and the cap is already met
        play.Dispose();
    }

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;
}
