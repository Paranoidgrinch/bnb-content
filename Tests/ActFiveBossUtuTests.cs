using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// ACT V, the fifth god — Utu, Witness of Every Oath, proved in live fights.
//
// The tests follow the promise: that he asks, that he pays before he is owed anything, that he NEVER stops
// the card that breaks the vow, that what he saw stays seen, that the sky climbs off his own blood, and that
// the last promise is spoken once and answered once.
public class ActFiveBossUtuTests
{
    private const string Cut = "paper_cut";          // Deed, 1 Energy: deal 6
    private const string Binder = "strong_binder";   // Working, 1 Energy
    private const string Paper = "misfiled_paper";   // Junk, 1 Energy: draw 1, exhaust

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Witness(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("utu", StringComparison.Ordinal));

    private static int Stacks(CombatantState body, string status) => FightProbe.StacksOf(body, status);

    private static bool Wears(CombatantState body, string status) => Stacks(body, status) > 0;

    private static int Seen(RunPlayback play) => Stacks(Witness(play), ActFive.WitnessId);

    private static int Counter(CombatantState body, string counter) =>
        body.Counters.TryGetValue(new CounterId(counter), out var value) ? value : 0;

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    // One fight against him, as the game fields it — his rule, his intent rules, his real starting statuses —
    // with only two dials: how much of HIM there is (so a test can walk the sky down in a few plays) and what
    // the deck is made of (so the Oath Director can be asked a real question).
    private static (RunPlayback Play, CombatantId EnemyId) Court(
        int? bossHealth = null, int energy = 9, string card = Cut,
        string intentId = "rise_between_the_mountains", int? draw = null)
    {
        var probe = FightProbe.Solo(ActFive.UtuEnemyId, intentId, energy);
        var body = bossHealth is { } hp ? probe.Enemies[0] with { MaxHealth = hp } : probe.Enemies[0];
        var fight = new EncounterDefinition(
            probe.Id, [body], probe.HeroResources, probe.HeroStartingStatuses,
            probe.HeroDisplayName, draw ?? probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        var (play, _, enemy) = FightProbe.Start(
            fight, deck: [.. Enumerable.Repeat(card, 30)], health: 9000);
        return (play, enemy);
    }

    // "Speak." — the offer is standing whenever a turn begins with no vow on the table, and it is answered
    // by index: 0 restraint, 1 whatever the Director allowed into the middle slot, 2 the spending vow.
    private static void Speak(RunPlayback play, int option)
    {
        Assert.NotNull(play.CombatDriver!.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([option]);
    }

    // Ends the turn and leaves whatever he asks next STANDING, so a test can see that he asked. Pass an
    // answer only where the next morning is scenery rather than the thing under test.
    private static void EndTurn(RunPlayback play, int? answer = null)
    {
        play.CombatDriver!.EndTurn();
        if (answer is { } option && play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
    }

    private static int Energy(RunPlayback play) =>
        Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static bool CanPlay(RunPlayback play, string cardId = Cut) =>
        Energy(play) > 0 && Hand(play).Any(c => c.DefinitionId.value == cardId);

    private static void Play(RunPlayback play, CombatantId enemy, string cardId = Cut) =>
        play.CombatDriver!.PlayCard(Hand(play).First(c => c.DefinitionId.value == cardId).Id, enemy);

    // ── he asks, and he pays first ────────────────────────────────────────────────────────────────────────

    // §10.1: at the start of the turn the player chooses one of three Oaths. Three, named, every morning.
    [Fact]
    public void He_asks_for_a_promise_every_morning()
    {
        var (play, _) = Court();

        var offer = play.CombatDriver!.PendingOptionChoice;
        Assert.NotNull(offer);
        Assert.Equal(3, offer!.Count);
        Assert.All(offer, label => Assert.False(string.IsNullOrWhiteSpace(label)));

        Speak(play, 0);
        Assert.True(Wears(Hero(play), ActFive.RestraintId));
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        play.Dispose();
    }

    // "The Favor is granted immediately. THEN the player attempts to keep the Oath." Two cards, in hand,
    // before a single card of the turn has been played and whether or not the vow survives it.
    [Fact]
    public void The_favor_is_paid_before_the_promise_is_kept()
    {
        var (play, _) = Court();

        var before = Hand(play).Count;
        Speak(play, 0);
        Assert.Equal(before + 2, Hand(play).Count);
        Assert.Equal(0, Seen(play));
        play.Dispose();
    }

    // ⚠ CardsDrawn IS NOT "THE TURN BEGAN" — it is "cards arrived", and a card that draws cards fires it
    // again in the middle of the turn. His morning happens once a round, or a Favor is paid twice for one
    // promise and a turn whose Oath has just been broken is handed another one on the spot.
    [Fact]
    public void A_card_that_draws_does_not_buy_a_second_morning()
    {
        var (play, enemy) = Court(bossHealth: 900, card: Paper);
        Speak(play, 0);

        var hand = Hand(play).Count;
        Play(play, enemy, Paper);        // one card leaves, one card is drawn

        Assert.Equal(hand, Hand(play).Count);
        Assert.Null(play.CombatDriver!.PendingOptionChoice);
        play.Dispose();
    }

    // ── the sun does not forbid (§10.2) ───────────────────────────────────────────────────────────────────

    // The defining rule. The fifth card under an Oath of Restraint is legal, it resolves in full, and the
    // only thing that happens afterwards is that he has seen it.
    [Fact]
    public void The_fifth_card_is_legal_and_it_is_seen()
    {
        var (play, enemy) = Court(bossHealth: 900);
        Speak(play, 0);

        for (var i = 0; i < 4; i++)
            Play(play, enemy);
        Assert.Equal(0, Seen(play));

        var health = play.CombatDriver!.Current!.State.GetCombatant(enemy).Health.Current;
        Play(play, enemy);

        // It resolved: the damage landed, in full, exactly as it would have without the vow.
        Assert.Equal(health - 6, play.CombatDriver.Current!.State.GetCombatant(enemy).Health.Current);
        Assert.Equal(1, Seen(play));
        Assert.True(Wears(Hero(play), ActFive.SeenId));
        play.Dispose();
    }

    // One breach, one Witness. The vow is over the moment it is broken, so a sixth and a seventh card are
    // ordinary cards again — a fight that charged for each of them would be a fight that punishes the
    // player for a promise that no longer exists.
    [Fact]
    public void A_breach_is_seen_once()
    {
        var (play, enemy) = Court(bossHealth: 900);
        Speak(play, 0);

        for (var i = 0; i < 6; i++)
            Play(play, enemy);
        Assert.Equal(1, Seen(play));
        Assert.False(Wears(Hero(play), ActFive.RestraintId));
        play.Dispose();
    }

    // The warning §10.2 asks for, in the only place this game can write it: a chip that appears the moment
    // the allowance is spent and says the next card is the one he will see.
    [Fact]
    public void The_edge_of_the_oath_is_written_down_before_it_is_crossed()
    {
        var (play, enemy) = Court(bossHealth: 900);
        Speak(play, 0);

        for (var i = 0; i < 3; i++)
        {
            Play(play, enemy);
            Assert.False(Wears(Hero(play), ActFive.AtTheEdgeId));
        }

        Play(play, enemy);
        Assert.True(Wears(Hero(play), ActFive.AtTheEdgeId));
        Assert.Equal(0, Seen(play));
        play.Dispose();
    }

    // An Oath kept costs nothing and is not mentioned again: the Favor was the whole of the transaction.
    [Fact]
    public void An_oath_kept_leaves_nothing_behind()
    {
        var (play, enemy) = Court(bossHealth: 900);
        Speak(play, 0);

        for (var i = 0; i < 3; i++)
            Play(play, enemy);
        EndTurn(play);

        Assert.Equal(0, Seen(play));
        Assert.False(Wears(Hero(play), ActFive.RestraintId));
        Assert.NotNull(play.CombatDriver!.PendingOptionChoice);   // it is over, so he asks again
        play.Dispose();
    }

    // ── the vows that are judged at the end of the turn ───────────────────────────────────────────────────

    // "Play at least one Attack before ending the turn" — a Deed, here. Ending the turn without one is the
    // breach, and it is the ENDING that does it: nothing was ever refused.
    [Fact]
    public void The_oath_of_resolve_is_broken_by_a_turn_with_no_deed()
    {
        var (play, _) = Court(bossHealth: 900);
        Speak(play, 1);
        Assert.True(Wears(Hero(play), ActFive.ResolveId));
        Assert.True(Wears(Hero(play), ActFive.NotYetKeptId));   // …and he says so while it can still be kept

        EndTurn(play);
        Assert.Equal(1, Seen(play));
        play.Dispose();
    }

    // …and the same vow kept by one Deed is a vow that costs nothing.
    [Fact]
    public void The_oath_of_resolve_is_kept_by_one_deed()
    {
        var (play, enemy) = Court(bossHealth: 900);
        Speak(play, 1);

        Play(play, enemy);
        Assert.False(Wears(Hero(play), ActFive.NotYetKeptId));
        EndTurn(play);
        Assert.Equal(0, Seen(play));
        play.Dispose();
    }

    // "End the turn with exactly 0 Energy." He pays two Energy for it up front, which is what makes the vow
    // hard: there is more to spend than the turn wanted to spend.
    [Fact]
    public void The_open_hand_is_broken_by_the_energy_that_is_left()
    {
        var (play, _) = Court(bossHealth: 900, energy: 3);
        Speak(play, 2);
        Assert.True(Wears(Hero(play), ActFive.OathOpenHandId));

        EndTurn(play);
        Assert.Equal(1, Seen(play));
        play.Dispose();
    }

    // ── the Oath Director (§10.4) ─────────────────────────────────────────────────────────────────────────

    // "Oaths must be technically achievable." A hand with no Deed in it cannot promise a Deed, so he does not
    // ask for one — the middle slot becomes the Straight Path, which any hand can keep by not playing.
    [Fact]
    public void He_does_not_ask_for_a_deed_that_is_not_there()
    {
        var (play, _) = Court(bossHealth: 900, card: Binder);
        Assert.DoesNotContain(Hand(play), c => c.DefinitionId.value == Cut);

        Speak(play, 1);
        Assert.True(Wears(Hero(play), ActFive.StraightPathId));
        Assert.False(Wears(Hero(play), ActFive.ResolveId));
        play.Dispose();
    }

    // …and with a Deed in hand he asks for the deed.
    [Fact]
    public void With_a_deed_in_hand_he_asks_for_the_deed()
    {
        var (play, _) = Court(bossHealth: 900);

        Speak(play, 1);
        Assert.True(Wears(Hero(play), ActFive.ResolveId));
        play.Dispose();
    }

    // The Straight Path: the first card of the vow can never break it (nothing has been played under it),
    // and the second of the same kind always does.
    [Fact]
    public void The_straight_path_is_broken_by_the_same_kind_twice()
    {
        var (play, enemy) = Court(bossHealth: 900, card: Binder);
        Speak(play, 1);

        Play(play, enemy, Binder);
        Assert.Equal(0, Seen(play));

        Play(play, enemy, Binder);
        Assert.Equal(1, Seen(play));
        play.Dispose();
    }

    // ── the sky climbs (§10.9, §10.12) ────────────────────────────────────────────────────────────────────

    // Read off his own blood, once. The Zenith brings No Shadow with it — the planning material §10.11 asks
    // for — and it makes him see two more of his own accord, which is every number he deals going up.
    [Fact]
    public void The_zenith_is_crossed_once_and_it_brings_the_shadowless_light()
    {
        var (play, enemy) = Court(bossHealth: 40);
        Speak(play, 0);

        Play(play, enemy);
        Assert.True(Wears(Witness(play), ActFive.MorningSunId));

        Play(play, enemy);   // 28 of 40 — the sun is at the Zenith
        Assert.True(Wears(Witness(play), ActFive.ZenithId));
        Assert.False(Wears(Witness(play), ActFive.MorningSunId));
        Assert.True(Wears(Hero(play), ActFive.NoShadowId));
        Assert.Equal(2, Seen(play));

        // Crossed once, not once per hit: a marker re-applied while its condition holds reads a lie.
        Play(play, enemy);
        Assert.Equal(1, Stacks(Witness(play), ActFive.ZenithId));
        Assert.Equal(2, Seen(play));
        play.Dispose();
    }

    // The sky changing is an event on the table: the crossing queues his own move for it, and his intent
    // rules pick that move over the cycle on his very next action.
    [Fact]
    public void The_zenith_answers_with_a_move_of_its_own()
    {
        var (play, enemy) = Court(bossHealth: 40);
        Speak(play, 0);
        Play(play, enemy);
        Play(play, enemy);
        Assert.Equal(1, Counter(Witness(play), "utu_zenith_due"));

        EndTurn(play);
        // Cross the Open Land is the only move he has that files Doubt.
        Assert.True(Wears(Hero(play), Converter.Cards.Keywords.Doubt));
        Assert.Equal(0, Counter(Witness(play), "utu_zenith_due"));
        play.Dispose();
    }

    // Under the Zenith an Oath spans two turns, and the clock is on the table where the player can plan
    // against it.
    [Fact]
    public void An_oath_under_the_zenith_covers_two_turns()
    {
        var (play, enemy) = Court(bossHealth: 40);
        Speak(play, 0);
        Play(play, enemy);
        Play(play, enemy);
        EndTurn(play);                                        // into the Zenith

        Speak(play, 0);
        Assert.True(Wears(Hero(play), ActFive.LongRestraintId));
        Assert.Equal(2, Stacks(Hero(play), ActFive.OathStandsId));

        EndTurn(play);
        // No second offer: the vow it was sworn under is still standing, and its clock has moved on one.
        Assert.Null(play.CombatDriver!.PendingOptionChoice);
        Assert.True(Wears(Hero(play), ActFive.LongRestraintId));
        Assert.Equal(1, Stacks(Hero(play), ActFive.OathStandsId));
        play.Dispose();
    }

    // "Play no more than 8 cards ACROSS both turns" — the tally is the vow's, not the turn's, which is the
    // whole difference between an Oath of Restraint and an Oath of Long Restraint.
    [Fact]
    public void The_long_restraint_counts_across_both_turns()
    {
        var (play, enemy) = Court(bossHealth: 900);
        // Down to the Zenith on his own numbers first: 900 → 630 is 70 %.
        Speak(play, 0);
        while (!Wears(Witness(play), ActFive.ZenithId))
        {
            while (CanPlay(play))
                Play(play, enemy);
            EndTurn(play, 0);
        }

        // A fresh Zenith Oath, and the tally starts at the swearing.
        while (!Wears(Hero(play), ActFive.LongRestraintId))
            EndTurn(play, 0);

        var before = Seen(play);
        for (var i = 0; i < 5; i++)
            Play(play, enemy);
        EndTurn(play);
        Assert.Equal(before, Seen(play));       // five is not nine

        for (var i = 0; i < 3; i++)
            Play(play, enemy);
        Assert.Equal(before, Seen(play));       // eight is not nine either
        Play(play, enemy);
        Assert.Equal(before + 1, Seen(play));   // the ninth, across the two turns, is the one he sees
        play.Dispose();
    }

    // ── the Final Oath (§10.12–10.14) ─────────────────────────────────────────────────────────────────────

    // Beaten down to the last sky and no further: the walk stops on the play that crosses 35 %, so what is
    // left of him is a body the tests below can still hit without ending the fight by accident.
    private static (RunPlayback Play, CombatantId EnemyId) UnderTheFinalSky(int? draw = null)
    {
        var (play, enemy) = Court(bossHealth: 200, draw: draw);

        while (!Wears(Witness(play), ActFive.FinalOathId))
        {
            if (play.CombatDriver!.PendingOptionChoice is not null)
                Speak(play, 0);
            while (!Wears(Witness(play), ActFive.FinalOathId) && CanPlay(play))
                Play(play, enemy);
            if (!Wears(Witness(play), ActFive.FinalOathId))
                EndTurn(play, 0);
        }

        // A Zenith Oath sworn before the sky changed may still have a turn to run; the Great Oath is offered
        // on the first morning that finds the table empty.
        EndTurn(play);
        for (var guard = 0; play.CombatDriver!.PendingOptionChoice is null && guard < 4; guard++)
            EndTurn(play);

        return (play, enemy);
    }

    // "I have heard enough small promises. Speak once." The last sky offers Great Oaths, and their Favor is
    // paid every turn rather than once.
    [Fact]
    public void The_last_sky_asks_once_and_the_promise_does_not_expire()
    {
        var (play, enemy) = UnderTheFinalSky();
        Speak(play, 0);

        Assert.True(Wears(Hero(play), ActFive.ShallBeRestrainedId));
        Assert.False(Wears(Hero(play), ActFive.OathStandsId));   // no clock: it stands until he falls

        Play(play, enemy);
        EndTurn(play);
        Assert.Null(play.CombatDriver!.PendingOptionChoice);     // he does not ask again
        Assert.True(Wears(Hero(play), ActFive.ShallBeRestrainedId));
        play.Dispose();
    }

    // §10.14. The Great Oath may still be broken: the action resolves, the Favor ends, Witness rises, and his
    // next move becomes Pronounce What Was Witnessed — announced on a chip, a whole turn before it lands.
    [Fact]
    public void Breaking_the_final_oath_calls_the_judgment()
    {
        var (play, enemy) = UnderTheFinalSky();
        Speak(play, 0);

        var before = Seen(play);
        for (var i = 0; i < 6; i++)
            Play(play, enemy);

        Assert.Equal(before + 1, Seen(play));
        Assert.True(Wears(Witness(play), ActFive.JudgmentId));
        Assert.False(Wears(Hero(play), ActFive.ShallBeRestrainedId));
        Assert.Equal(1, Counter(Witness(play), "utu_judgment"));

        var health = Hero(play).Health.Current;
        EndTurn(play);
        // 25 + 10 per Witness, and nothing else he has comes anywhere near it.
        Assert.True(health - Hero(play).Health.Current >= 25 + 10 * before,
            $"judgment dealt {health - Hero(play).Health.Current} against a Witness of {before + 1}");
        Assert.False(Wears(Witness(play), ActFive.JudgmentId));

        // "Speak once." A broken Great Oath is not replaced: there is nothing left to promise.
        Assert.Null(play.CombatDriver!.PendingOptionChoice);
        play.Dispose();
    }

    // "Utu does not become invulnerable. The player may intentionally break the Final Oath and try to kill
    // him before Judgment resolves." Nothing in the fight says otherwise, and this is the test that says so.
    [Fact]
    public void He_is_not_invulnerable_while_the_judgment_is_coming()
    {
        // A hand wide enough to break the vow AND still hold a card: the point of the test is the card
        // AFTER the breach.
        var (play, enemy) = UnderTheFinalSky(draw: 9);
        Speak(play, 0);
        for (var i = 0; i < 6; i++)
            Play(play, enemy);
        Assert.True(Wears(Witness(play), ActFive.JudgmentId));

        Assert.False(play.CombatDriver!.Current!.State.GetCombatant(enemy).Health.Current <= 0);
        var health = play.CombatDriver.Current.State.GetCombatant(enemy).Health.Current;
        Play(play, enemy);
        Assert.True(play.CombatDriver.Current!.State.GetCombatant(enemy).Health.Current < health);
        play.Dispose();
    }

    // ── Witness (§10.3) ───────────────────────────────────────────────────────────────────────────────────

    // Every move he makes is read off what he has seen, and the telegraph beside it says the same sum. This
    // is what stops "take the biggest Favor and break it at once" from being the fight's answer.
    [Fact]
    public void Everything_he_does_is_read_off_the_witness()
    {
        int Suffered(int breaches)
        {
            var (play, enemy) = Court(bossHealth: 900);
            Speak(play, 0);
            for (var i = 0; i < 4 + breaches; i++)
                Play(play, enemy);
            Assert.Equal(breaches, Seen(play));

            var health = Hero(play).Health.Current;
            EndTurn(play);
            var dealt = health - Hero(play).Health.Current;
            play.Dispose();
            return dealt;
        }

        // Rise Between the Mountains is 24, and 2 more for each Witness.
        Assert.Equal(24, Suffered(0));
        Assert.Equal(26, Suffered(1));
    }

    // What he has seen is never given back. Not by a turn ending, not by a vow being kept afterwards.
    [Fact]
    public void What_was_witnessed_stays_witnessed()
    {
        var (play, enemy) = Court(bossHealth: 900);
        Speak(play, 0);
        for (var i = 0; i < 5; i++)
            Play(play, enemy);
        Assert.Equal(1, Seen(play));

        EndTurn(play, 0);
        Assert.Equal(1, Seen(play));
        Play(play, enemy);
        EndTurn(play, 0);
        Assert.Equal(1, Seen(play));   // a kept Oath does not buy it back
        play.Dispose();
    }
}
