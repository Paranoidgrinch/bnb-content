using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Presentless Clock does not schedule its own attacks — it changes when YOUR actions are considered to
// have happened. File a turn to the Past and your first effect happens now and echoes at half next turn; file
// it to the Future and half happens now and the rest arrives late. Either way it is holding a record of
// yours, and the two hands pull in opposite directions.
public class PresentlessClockTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // What an effect actually took off the Clock, counting the Block it ate on the way. The Clock guards
    // itself whenever it is holding a Past record, so measuring HP alone would credit its own reaction with
    // swallowing the echo.
    private static int Pool(RunPlayback play, CombatantId id)
    {
        var c = Enemy(play, id);
        return c.Health.Current + Block(c);
    }

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    // The offer comes after a normal draw, which is the second player turn — the opening hand is dealt before
    // any rule is watching. `file` picks the option; 0 = Past, 1 = Future, 2 = let it happen now.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Clock) Clock(
        string intent, int file)
    {
        var fight = FightProbe.Start(
            FightProbe.Solo(PresentlessClock.EnemyId, intent, energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);
        fight.Play.CombatDriver!.EndTurn();
        Assert.NotNull(fight.Play.CombatDriver.PendingOptionChoice);
        fight.Play.CombatDriver.SupplyOptionChoice([file]);
        return fight;
    }

    // 12.2: the offer names all three hands, and the Clock has none for the present.
    [Fact]
    public void The_clock_asks_which_hand_this_turn_belongs_to()
    {
        var (play, _, _) = Clock("chronology_closed", 2);

        Assert.Null(play.Session!.Error);
        play.Dispose();
    }

    // 12.3 Past: the effect resolves in full NOW — a 6-damage card takes 6 — and echoes at half next turn.
    [Fact]
    public void A_past_effect_lands_in_full_and_echoes_at_half()
    {
        var (play, session, clock) = Clock("second_hand_no_first", 0);

        var before = Pool(play, clock);
        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock);
        Assert.Null(session.Error);
        Assert.Equal(6, before - Pool(play, clock)); // in full, now

        // Next player turn: the echo resolves once, at half — into the 10 Block the Clock archived itself
        // behind, which is the two halves of the design working against each other on purpose.
        var afterPlay = Pool(play, clock);
        play.CombatDriver.EndTurn();

        Assert.Equal(3, afterPlay + 10 - Pool(play, clock));
        play.Dispose();
    }

    // 12.4 Future: half now, the rest at the start of the next player turn.
    [Fact]
    public void A_future_effect_is_split_across_two_turns()
    {
        var (play, session, clock) = Clock("second_hand_no_first", 1);

        var before = Pool(play, clock);
        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock);
        Assert.Null(session.Error);
        Assert.Equal(3, before - Pool(play, clock)); // 6 halved

        var afterPlay = Pool(play, clock);
        play.CombatDriver.EndTurn();

        // The Clock forecloses 6 on itself at its own turn start for holding a Future record…
        Assert.Equal(6, afterPlay - Pool(play, clock));

        // …and the remainder arrives with the player's next turn: 3 more, and no second foreclosure, because
        // the record is spent.
        var afterForeclosure = Pool(play, clock);
        play.CombatDriver.EndTurn();
        // Answering the new turn's offer is also what carries the turn's own start to its end — a turn-start
        // program is still draining when EndTurn hands control back.
        play.CombatDriver.SupplyOptionChoice([2]); // let this turn happen now
        Assert.Equal(3, afterForeclosure - Pool(play, clock));
        play.Dispose();
    }

    // 12.2: it is the FIRST eligible effect that is recorded, not every effect of the turn.
    [Fact]
    public void Only_the_first_effect_of_the_turn_is_recorded()
    {
        var (play, session, clock) = Clock("second_hand_no_first", 1);

        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock); // halved: 3
        var afterFirst = Pool(play, clock);
        play.CombatDriver.PlayCard(Hand(play)[0].Id, clock); // ordinary: 6
        Assert.Null(session.Error);

        Assert.Equal(6, afterFirst - Pool(play, clock));
        play.Dispose();
    }

    // 12.6 Archive the Past: an unresolved Past record guards the Clock by 10 at its own turn start.
    [Fact]
    public void An_unresolved_past_record_guards_the_clock()
    {
        var (play, _, clock) = Clock("second_hand_no_first", 0);

        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock); // the record is made
        play.CombatDriver.EndTurn();                          // …and the Clock archives it

        // Its intent gains no Block of its own, so the 10 is all reaction — and the echo that arrives at the
        // player's turn start immediately eats 3 of it, which is the two halves of the design meeting.
        Assert.Equal(7, Block(Enemy(play, clock)));
        play.Dispose();
    }

    // 12.6 Foreclose Tomorrow: an unresolved Future record costs it 6 Block — or 6 HP with no Block to give.
    [Fact]
    public void An_unresolved_future_record_forecloses_on_the_clock()
    {
        var (play, _, clock) = Clock("second_hand_no_first", 1);

        var before = Pool(play, clock);
        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock); // 3 of the 6 lands now
        var afterPlay = Pool(play, clock);
        Assert.True(before > afterPlay);

        play.CombatDriver.EndTurn();

        // No Block on the Clock, so the foreclosure is 6 HP straight off.
        Assert.Equal(6, afterPlay - Pool(play, clock));
        play.Dispose();
    }

    // 12.5: one unresolved record of each kind. Filing to an occupied hand never overwrites what is there.
    [Fact]
    public void A_full_slot_is_never_overwritten()
    {
        var (play, session, clock) = Clock("second_hand_no_first", 0);

        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock); // Past record: 3
        play.CombatDriver.EndTurn();
        // The record resolved at the turn start, so the slot is free again and the offer stands anew.
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);
        play.Dispose();
    }

    // 12.7 History Refuses Revision: 15 damage, and 10 Block on top while it is holding your past.
    [Fact]
    public void History_refuses_revision_harder_while_it_holds_your_past()
    {
        var (bare, _, bareClock) = Clock("history_refuses_revision", 2);
        bare.CombatDriver!.EndTurn();
        Assert.Equal(0, Block(Enemy(bare, bareClock)));
        bare.Dispose();

        var (held, _, heldClock) = Clock("history_refuses_revision", 0);
        held.CombatDriver!.PlayCard(Hand(held)[0].Id, heldClock);
        held.CombatDriver.EndTurn();

        // 10 from the reaction at its turn start and 10 more from the intent itself, less the 3 the echo
        // takes back the moment the player's turn opens.
        Assert.Equal(17, Block(Enemy(held, heldClock)));
        held.Dispose();
    }

    // Signature — Borrowed Tomorrow: your future is held back one further turn.
    [Fact]
    public void Borrowed_tomorrow_holds_your_future_back_a_turn()
    {
        var (play, _, clock) = Clock("borrowed_tomorrow", 1);

        play.CombatDriver!.PlayCard(Hand(play)[0].Id, clock); // 3 now, 3 owed
        var owed = Pool(play, clock);
        play.CombatDriver.EndTurn(); // the Clock borrows the tomorrow: 11 damage to you, nothing back to it

        // The remainder did NOT arrive this turn — only the foreclosure did.
        Assert.Equal(6, owed - Pool(play, clock)); // 6 foreclosed, no 3-damage remainder
        play.Dispose();
    }
}
