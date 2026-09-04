using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV's five Rites, in live fights out of the real converted game.
//
// A Rite is the one card shape that can be quietly inert: four of these five do not act at all, they change
// what an EXISTING keyword does, and the keyword has to be the thing that looks for them. A marker nobody
// reads installs cleanly, validates cleanly, and does nothing for the whole run — which is exactly the fault
// this file exists to catch.
public class ActFourCardTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    private const string Form = "form_of_ill_intent";      // Working, 1: apply 3 Paperwork
    private const string Seal = "seal_of_concern";         // Working, 1: apply 1 Seal
    private const string Hex = "deferred_hex";             // Deed, 1, Queue: deal 13
    private const string Wax = "waxen_surety";             // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // A Rite has to be DRAWN before it can be installed, and the deck is shuffled — so the tests that need
    // one on the table wait for it rather than assuming the opening hand. Ending a turn or two first changes
    // nothing about what is being proved.
    private static void Install(RunPlayback play, InteractiveRunSession session, string riteId, int maxTurns = 5)
    {
        for (var turn = 0; turn < maxTurns; turn++)
        {
            var card = play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == riteId);
            if (card is not null)
            {
                play.CombatDriver.PlayCard(card.Id, null);
                Assert.True(session.Error is null, session.Error);
                return;
            }

            play.CombatDriver.EndTurn();
        }

        Assert.Fail($"'{riteId}' never came up in {maxTurns} turns");
    }

    // A hand holds five, so a test that needs more plays than that has to let turns pass. Nothing the Rites
    // do is undone by a turn boundary, which is what makes this safe rather than convenient.
    private static void PlayTimes(
        RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target, int times,
        int maxTurns = 8)
    {
        var played = 0;

        for (var turn = 0; turn < maxTurns && played < times; turn++)
        {
            while (played < times &&
                   play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == cardId)
                       is { } card)
            {
                play.CombatDriver.PlayCard(card.Id, target);
                Assert.True(session.Error is null, session.Error);
                played++;
            }

            if (played < times)
                play.CombatDriver!.EndTurn();
        }

        Assert.Equal(times, played);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Fight(
        params string[] deck) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 9), deck.ToList(), health: 400);

    private static string[] Deck(params (string Card, int Count)[] cards) =>
        [.. cards.SelectMany(c => Enumerable.Repeat(c.Card, c.Count))];

    // ── Temple Tally ──────────────────────────────────────────────────────────────────────────────────────

    // "Whenever an enemy reaches a new multiple of 5 Paperwork for the first time this combat, apply 1 Seal
    // to it for each new multiple crossed."
    [Fact]
    public void Temple_tally_seals_an_enemy_at_every_fifth_sheet()
    {
        var (play, session, enemy) = Fight(Deck(("temple_tally", 2), (Form, 10)));

        Install(play, session, "temple_tally");

        Play(play, session, Form, enemy);                  // 3 sheets: no multiple crossed
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Seal));

        Play(play, session, Form, enemy);                  // 6 sheets: the first five
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Seal));

        Play(play, session, Form, enemy);                  // 9 sheets: still one multiple
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Seal));

        Play(play, session, Form, enemy);                  // 12 sheets: the second five
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Seal));
        play.Dispose();
    }

    // ── Processional Calendar ─────────────────────────────────────────────────────────────────────────────

    // "At the end of your turn, if you have at least 2 Queued cards, resolve your oldest Queued card." A
    // queued card ordinarily resolves at the START of the player's next turn; the Calendar spends the oldest
    // of a backlog one turn earlier, at the END of this one.
    [Fact]
    public void The_calendar_resolves_the_oldest_of_a_backlog_early()
    {
        // A body that raises no Block, so a hex resolving is 13 and nothing else.
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Roster("calendar", energy: 9, ("margin_note_gnawer", "add_a_correction", 200)),
            deck: Deck(("processional_calendar", 2), (Hex, 8)), health: 400);

        Install(play, session, "processional_calendar");

        Play(play, session, Hex, enemy);
        var queued = Enemy(play, enemy).Health.Current;
        play.CombatDriver!.EndTurn();                      // one queued card is not a backlog: nothing early
        Assert.Equal(queued - 13, Enemy(play, enemy).Health.Current);   // …it resolved on its own next turn

        Play(play, session, Hex, enemy);
        Play(play, session, Hex, enemy);
        var backlog = Enemy(play, enemy).Health.Current;
        play.CombatDriver.EndTurn();                       // two are: the oldest goes at the turn's end

        Assert.Equal(backlog - 13 - 13, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    // ── Hieratic Measure ──────────────────────────────────────────────────────────────────────────────────

    // "Whenever you Ratify an enemy, immediately trigger its current Paperwork once, then remove 3 Paperwork
    // from it." The toll is the Paperwork tick brought forward, so it ignores Block and is not an attack.
    [Fact]
    public void A_ratify_under_the_hieratic_measure_calls_in_the_paperwork_at_once()
    {
        var (play, session, enemy) = Fight(Deck(("hieratic_measure", 2), (Form, 6), (Seal, 8)));

        Install(play, session, "hieratic_measure");

        PlayTimes(play, session, Form, enemy, 2);          // 6 sheets standing
        PlayTimes(play, session, Seal, enemy, 2);          // two Seals: no conversion yet
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Ratified));

        var sheets = FightProbe.StacksOf(Enemy(play, enemy), Keywords.Paperwork);
        var before = Enemy(play, enemy).Health.Current;

        PlayTimes(play, session, Seal, enemy, 1);          // the third Seal Ratifies

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Ratified));
        Assert.Equal(sheets, before - Enemy(play, enemy).Health.Current);
        Assert.Equal(sheets - 3, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Paperwork));
        play.Dispose();
    }

    // ── Candle Cathedral ──────────────────────────────────────────────────────────────────────────────────

    // "Whenever Ward Wax grants Block, gain additional Block equal to half your Ward Wax, rounded up. Ward Wax
    // no longer suffers its additional decay."
    [Fact]
    public void The_cathedral_pays_the_wax_half_again_and_stops_the_extra_decay()
    {
        var (play, session, enemy) = Fight(Deck(("candle_cathedral", 2), (Wax, 8)));

        Install(play, session, "candle_cathedral");
        Play(play, session, Wax, enemy);                   // 4 Ward Wax
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));

        play.CombatDriver!.EndTurn();                      // the enemy hits: ordinarily 2 wax, here 1

        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        // …and the wax pays 3 + ceil(3 / 2) = 5 at the top of the turn.
        Assert.Equal(5, Block(Hero(play)));
        play.Dispose();
    }

    // ── Absolute Interdict ────────────────────────────────────────────────────────────────────────────────

    // "The first time each turn Censure on a combatant would prevent Status stacks, 1 Censure prevents the
    // entire Status application instead, regardless of stack count, and only 1 Censure is consumed."
    //
    // Two Censure against three sheets is the whole test: ordinarily two are refused, one lands and the
    // Censure is gone; under the Interdict none lands and one Censure is left standing.
    [Fact]
    public void Censure_alone_refuses_stack_for_stack()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("margin_note_gnawer", "add_a_correction", energy: 9,
                (Keywords.Censure, 2)),
            deck: [.. Enumerable.Repeat(Form, 6)], health: 400);

        play.CombatDriver!.EndTurn();                      // 3 sheets meet 2 Censure

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Censure));
        play.Dispose();
    }

    // The Rite is put on the table as a starting rule rather than played from the deck: the charge is laid at
    // the top of a combatant's OWN turn, and a Rite installed part-way through the player's first turn has
    // already missed it. What is being proved is the rule, and the card's install path is pinned by
    // FinalCardPoolTests.
    [Fact]
    public void The_interdict_makes_the_first_refusal_of_the_turn_total()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("margin_note_gnawer", "add_a_correction", energy: 9,
                (Keywords.Censure, 2), (ActIVRites.AbsoluteInterdict, 1)),
            deck: [.. Enumerable.Repeat(Form, 6)], health: 400);

        play.CombatDriver!.EndTurn();                      // 3 sheets meet the charge

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Censure));
        play.Dispose();
    }

    // …and it is ONE refusal a turn. The charge is spent, and the round's second application meets the
    // Censure that is left, stack for stack, exactly as it always did.
    [Fact]
    public void The_second_refusal_of_a_turn_is_an_ordinary_one()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("margin_note_gnawer", "add_a_correction", energy: 9,
                (Keywords.Censure, 4), (ActIVRites.AbsoluteInterdict, 1)),
            deck: [.. Enumerable.Repeat(Form, 6)], health: 400);

        play.CombatDriver!.EndTurn();                      // round 1: refused whole, 1 Censure spent
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Keywords.Censure));

        play.CombatDriver.EndTurn();                       // round 2: a new charge, and the same again
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Keywords.Censure));
        play.Dispose();
    }
}
