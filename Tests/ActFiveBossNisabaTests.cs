using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// ACT V, the first god — Nisaba, Keeper of the First Tablet, proved in live fights.
//
// The tests follow the tablet: what stands on it, what a count running out costs, what a Reed Mark buys, what
// a sealed line refuses, and what the Last Line does to both of them.
public class ActFiveBossNisabaTests
{
    private const string Cut = "paper_cut";      // Deed, 1: deal 6
    private const string Wax = "waxen_surety";   // Working, 1: gain 4 Ward Wax

    // The one intent of hers that costs the player no HP, so everything a fight loses is the tablet talking.
    private const string Quiet = "dry_the_clay";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Keeper(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("nisaba", StringComparison.Ordinal));

    private static int Stacks(CombatantState body, string status) => FightProbe.StacksOf(body, status);

    private static IReadOnlyList<string> InHand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand.Select(c => c.DefinitionId.value)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // A whole round: the player passes, she answers, and the tablet is read at the end of her window.
    private static void Round(RunPlayback play) => play.CombatDriver!.EndTurn();

    // The same probe with a smaller god in it, and her intent rules off: a fight that is ABOUT a phase must
    // not have the phase announcements walk in over the top of it just because 60 HP is under every band.
    private static EncounterDefinition Frail(EncounterDefinition probe, int maxHealth) =>
        new(probe.Id, [probe.Enemies[0] with { MaxHealth = maxHealth, IntentRules = null }],
            probe.HeroResources, probe.HeroStartingStatuses, probe.HeroDisplayName,
            probe.CardsDrawnPerTurn, probe.TriggeredEffects);

    // Play the whole hand at her, turn after turn, until `stop` says so or the rope runs out.
    private static void Grind(
        RunPlayback play, InteractiveRunSession session, CombatantId keeper, int turns, Func<bool> stop)
    {
        for (var turn = 0; turn < turns && !stop(); turn++)
        {
            while (!stop() && play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Cut))
                Play(play, session, Cut, keeper);
            if (stop())
                return;
            Round(play);
        }
    }

    private static bool Wears(RunPlayback play, string status) =>
        Keeper(play).Statuses.Any(s => s.DefinitionId.value == status);

    // The finale, put on the table by hand: the Last Line standing with its four dawns, the Indelible on her,
    // and marks enough in the player's hand to answer it.
    private static EncounterDefinition Finale(int marks)
    {
        var probe = FightProbe.Solo(ActFive.NisabaEnemyId, Quiet, 9,
            (ActFive.LastLineId, 4), (ActFive.IndelibleId, 1));
        return new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(ActFive.ReedMarksId), marks)],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);
    }

    // ── the tablet ────────────────────────────────────────────────────────────────────────────────────────

    // Three slots, three sentences, and one of them from each slot — never a duplicate, never two extremes
    // together, because a slot owns its pair and alternates between them.
    [Fact]
    public void The_tablet_opens_with_three_sentences_one_from_each_slot()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        var keeper = Keeper(play);
        Assert.Equal(2, Stacks(keeper, "nisaba_line_body_shall_bear"));
        Assert.Equal(1, Stacks(keeper, "nisaba_line_measures_withheld"));
        Assert.Equal(2, Stacks(keeper, "nisaba_line_hand_shall_hold_two"));
        // …and the other half of each pair is not on the tablet at all.
        Assert.Equal(0, Stacks(keeper, "nisaba_line_three_wounds"));
        Assert.Equal(0, Stacks(keeper, "nisaba_line_guard_counted_nothing"));
        Assert.Equal(0, Stacks(keeper, "nisaba_line_two_works_broken"));

        // The reed is cut, the margin is entered, and a sheet is in hand for every line that stands.
        Assert.Equal(1, Stacks(Hero(play), ActFive.ReedMarksId));
        Assert.InRange(Stacks(Hero(play), ActFive.CountedMarginId), 3, 5);
        var hand = InHand(play);
        Assert.Contains("revise_body_shall_bear", hand);
        Assert.Contains("revise_measures_withheld", hand);
        Assert.Contains("revise_hand_shall_hold_two", hand);
        play.Dispose();
    }

    // A count runs out at the end of HER window, and the sentence becomes true. The one-turn sentence is the
    // clock the player has least room against: written at the start of a turn, enacted at the end of it.
    [Fact]
    public void A_sentence_becomes_true_when_its_count_runs_out()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        Round(play);

        // Three Measures Shall Be Withheld: it is off the tablet, and it has been taken out of the pool of the
        // turn it was WRITTEN ABOUT rather than the one before it — a pool that is refilled at the turn's
        // start cannot be robbed the turn before, which is why the sentence waits on the player as a status
        // and takes what it is owed at that start. By the time the turn is in the player's hands the reading
        // has already happened and the status is gone, which is exactly what is asserted here.
        Assert.Equal(0, Stacks(Keeper(play), "nisaba_line_measures_withheld"));
        Assert.Equal(9 - 3, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(0, Stacks(Hero(play), ActFive.MeasuresWithheldId));

        // The slot that lost its line has already written the other half of its pair.
        Assert.Equal(2, Stacks(Keeper(play), "nisaba_line_guard_counted_nothing"));
        play.Dispose();
    }

    // The revision does not dispel anything. The sentence still becomes true; it says less.
    [Fact]
    public void A_revision_shrinks_the_sentence_rather_than_removing_it()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero(ActFive.NisabaEnemyId, Quiet, 9, (ActFive.ReedMarksId, 3)),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        // Four marks in hand (three carried, one cut this turn), and the sheet re-offers itself while any
        // remain — so one line can be walked back twice inside a turn.
        Assert.Equal(4, Stacks(Hero(play), ActFive.ReedMarksId));
        Play(play, session, "revise_body_shall_bear", null);
        Play(play, session, "revise_body_shall_bear", null);
        Assert.Equal(2, Stacks(Keeper(play), "nisaba_revised_body_shall_bear"));
        Assert.Equal(2, Stacks(Hero(play), ActFive.ReedMarksId));

        var whole = Hero(play).Health.Current;
        Round(play);   // the two-turn sentence counts down to one
        Round(play);   // …and is read out

        // Twice revised: a third of thirty-six, and Block never had anything to say about it.
        Assert.Equal(12, whole - Hero(play).Health.Current);
        play.Dispose();
    }

    // "The final revision may reverse subject or meaning." It is still written down. It is simply no longer
    // written about you.
    [Fact]
    public void The_fourth_revision_turns_the_sentence_against_her()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero(ActFive.NisabaEnemyId, Quiet, 9, (ActFive.ReedMarksId, 4)),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        for (var i = 0; i < 4; i++)
            Play(play, session, "revise_body_shall_bear", null);
        Assert.Equal(4, Stacks(Keeper(play), "nisaba_revised_body_shall_bear"));
        // Four revisions is as far as an ordinary sentence goes: nothing more is offered.
        Assert.DoesNotContain("revise_body_shall_bear", InHand(play));

        var hero = Hero(play).Health.Current;
        var hers = Keeper(play).Health.Current;
        Round(play);
        Round(play);

        Assert.Equal(hero, Hero(play).Health.Current);
        Assert.Equal(18, hers - Keeper(play).Health.Current);
        play.Dispose();
    }

    // The Counted Margin is always a bonus and never a punishment: hit the count and the reed is cut twice
    // next turn. Her own sheets are not cards you meant to play, and do not count towards it.
    [Fact]
    public void Meeting_the_counted_margin_pays_a_second_mark()
    {
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 20)], health: 900);

        var margin = Stacks(Hero(play), ActFive.CountedMarginId);

        // One reed sheet first — it is hers, so the count does not see it.
        Play(play, session, "revise_body_shall_bear", null);
        for (var i = 0; i < margin; i++)
            Play(play, session, Cut, keeper);

        Round(play);
        // The mark spent on the revision is back, and a second one with it.
        Assert.Equal(2, Stacks(Hero(play), ActFive.ReedMarksId));
        play.Dispose();
    }

    // …and missing it costs only the bonus.
    [Fact]
    public void Missing_the_margin_costs_only_the_bonus()
    {
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 20)], health: 900);

        Play(play, session, Cut, keeper);   // one card, and the margin is never one
        Round(play);
        Assert.Equal(2, Stacks(Hero(play), ActFive.ReedMarksId));   // the carried one plus this turn's
        play.Dispose();
    }

    // ── the lapis record ──────────────────────────────────────────────────────────────────────────────────

    // Her own blood announces both later phases, and the intent that carries one out is reached only by the
    // announcement — so a phase never lands in the middle of a turn that was planned without it.
    [Fact]
    public void Her_own_blood_announces_the_lapis_and_then_the_last_line()
    {
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, "set_the_reed", energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 200)], health: 4000);

        Assert.False(Wears(play, ActFive.LapisRecordId));

        // 620 → 403 is the lapis, and the announcement is spent by the intent it licenses.
        Grind(play, session, keeper, 40, () => Wears(play, ActFive.LapisRecordId));
        Assert.True(Wears(play, ActFive.LapisRecordId));
        Assert.False(Wears(play, ActFive.LapisAnnouncedId));

        // …and 186 is the Last Line, which closes every ordinary sentence behind it.
        Grind(play, session, keeper, 40, () => Wears(play, ActFive.LastLineId));
        Assert.True(Wears(play, ActFive.LastLineId));
        Assert.True(Wears(play, ActFive.IndelibleId));
        Assert.All(ActFive.LineFaces, face => Assert.False(Wears(play, face)));
        play.Dispose();
    }

    // The seal closes the line the player would otherwise have corrected NEXT turn, and closing it means
    // exactly one thing at the hand: there is no sheet for it.
    [Fact]
    public void A_sealed_line_cannot_be_revised_for_the_turn_it_covers()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, "impress_the_seal", 9, (ActFive.LapisRecordId, 1)),
            deck: [.. Enumerable.Repeat(Cut, 20)], health: 900);

        Round(play);

        // Two-turn sentences were the shortest that survive her window; the first of them was sealed.
        var sealed_ = ActFive.SealedFaces.Where(face => Wears(play, face)).ToList();
        var face = Assert.Single(sealed_);
        Assert.Equal("nisaba_sealed_body_shall_bear", face);
        Assert.DoesNotContain("revise_body_shall_bear", InHand(play));
        // …and the rest of the tablet is untouched: a seal closes one line, not the tablet.
        Assert.Contains("revise_hand_shall_hold_two", InHand(play));

        // It lifts at the end of the turn it covered.
        Round(play);
        Assert.DoesNotContain(face, Keeper(play).Statuses.Select(s => s.DefinitionId.value));
        play.Dispose();
    }

    // ── the last line ─────────────────────────────────────────────────────────────────────────────────────

    // While the lethal wording stands she cannot be brought below 1 — not once, but for as long as it is
    // unresolved, however many blows land. (This is the one rule Act V had to buy from the engine.)
    [Fact]
    public void While_the_last_line_stands_she_cannot_be_killed()
    {
        var (play, session, keeper) = FightProbe.Start(
            Frail(FightProbe.Solo(ActFive.NisabaEnemyId, "set_the_reed", 9,
                (ActFive.LastLineId, 4), (ActFive.IndelibleId, 1)), 60),
            deck: [.. Enumerable.Repeat(Cut, 40)], health: 900);

        Grind(play, session, keeper, 2, () => Keeper(play).Health.Current == 1);
        Assert.Equal(1, Keeper(play).Health.Current);
        Assert.True(Keeper(play).IsAlive);

        // …and five more blows in the same turn do not change it, which is what a prevention that is never
        // SPENT means: a one-shot charm re-arming itself would have died to the second hit of one action.
        while (play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Cut))
            Play(play, session, Cut, keeper);
        Assert.Equal(1, Keeper(play).Health.Current);
        Assert.True(Keeper(play).IsAlive);
        play.Dispose();
    }

    // Four revisions read SHALL REMAIN: the Indelible goes at once rather than when the countdown does, so a
    // player who has already solved the sentence is not made to stand through the rest of it.
    [Fact]
    public void The_fourth_revision_of_the_last_line_takes_the_indelible_off()
    {
        var (play, session, _) = FightProbe.Start(
            Finale(marks: 3), deck: [.. Enumerable.Repeat(Cut, 20)], health: 2000);

        // The Last Line is offered a sheet like any other sentence.
        Assert.Contains(ActFive.ReviseLastLineCardId, InHand(play));
        Assert.Equal(4, Stacks(Hero(play), ActFive.ReedMarksId));

        for (var i = 0; i < 4; i++)
            Play(play, session, ActFive.ReviseLastLineCardId, null);

        Assert.Equal(4, Stacks(Keeper(play), ActFive.LastLineRevisedId));
        Assert.False(Wears(play, ActFive.IndelibleId));
        // The sentence is still on the tablet — it simply no longer says what it said.
        Assert.True(Wears(play, ActFive.LastLineId));
        play.Dispose();
    }

    // …and the fifth turns the sentence round. The tablet is not particular about whose name is on it, and
    // it is read the moment it is written rather than at the next dawn.
    [Fact]
    public void The_fifth_revision_erases_the_keeper()
    {
        var (play, session, _) = FightProbe.Start(
            Finale(marks: 3), deck: [.. Enumerable.Repeat(Cut, 20)], health: 2000);

        for (var i = 0; i < 4; i++)
            Play(play, session, ActFive.ReviseLastLineCardId, null);
        Assert.Equal(0, Stacks(Hero(play), ActFive.ReedMarksId));

        Round(play);   // one more dawn, and one more mark
        Assert.Equal(1, Stacks(Hero(play), ActFive.ReedMarksId));

        Play(play, session, ActFive.ReviseLastLineCardId, null);

        // She loses everything she had left, whatever Block she was standing behind.
        Assert.True(play.CombatDriver!.Current is null
                    || !play.CombatDriver.Current.State.Combatants
                        .Any(c => c.DefinitionId.value.Contains("nisaba", StringComparison.Ordinal) && c.IsAlive));
        play.Dispose();
    }

    // An unrevised Last Line is not damage. It is the name coming off the record, and no Block answers that.
    [Fact]
    public void An_unrevised_last_line_erases_the_supplicant()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, Quiet, 3, (ActFive.LastLineId, 1)),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 200);

        Round(play);

        Assert.True(play.CombatDriver!.Current is null || !Hero(play).IsAlive
                    || Hero(play).Health.Current == 0);
        play.Dispose();
    }
}
