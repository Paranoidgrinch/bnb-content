using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-I content cut of 2026-10-29: what the four stages ask of the player, and that every rule the player is
// asked about wears a chip.
public class ActOneCutTests
{
    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int BlockOf(RunPlayback play, CombatantId id) =>
        Body(play, id).DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    // The Wrong Window answers a second card of the turn's opening type with 5 Block — on the Scribe alone, and
    // the player can see the rule on it. In the duo the Line beside it is not a window.
    [Fact]
    public void The_scribe_wears_its_rule_and_alone_answers_the_second_card_of_the_type()
    {
        var probe = FightProbe.Roster("wrong_window",
            energy: 9,
            ("wrong_window_scribe", "tap_the_counter", null),
            ("a_very_official_line", "shuffle_forward", null));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 5).ToList());
        var scribe = Find(play, "wrong_window_scribe");
        var line = Find(play, "a_very_official_line");

        Assert.Equal(1, FightProbe.StacksOf(Body(play, scribe), PassiveStatuses.NotThisCounterId));

        Play(play, session, "paper_cut", line);
        Assert.Equal(0, BlockOf(play, scribe));
        Play(play, session, "paper_cut", line);
        Assert.Equal(5, BlockOf(play, scribe));
        Assert.Equal(0, BlockOf(play, line));
        play.Dispose();
    }

    private static int Health(RunPlayback play, CombatantId id) => Body(play, id).Health.Current;

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static int Patrons(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.Count(c => c.IsAlive
            && FightProbe.StacksOf(c, Converter.Elites.WaitingRoom.PatronId) > 0);

    // The Waiting Room's board asks for an exact number of cards. Meet it and your number is called: through the
    // next turn the Room stands at the counter — no Block, double damage.
    [Fact]
    public void Playing_the_boards_number_calls_you_to_the_counter()
    {
        var probe = FightProbe.Roster("waiting_called", energy: 9,
            ("devouring_waiting_room", "walls_hold_still", 88));
        var (play, session, room) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList());

        Assert.Equal(1, FightProbe.StacksOf(Body(play, room), Converter.Elites.WaitingRoom.RuleId));
        Assert.Equal(2, FightProbe.StacksOf(Body(play, room), Converter.Elites.WaitingRoom.NowServingId));
        Play(play, session, "paper_cut", room);
        Play(play, session, "paper_cut", room);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, Patrons(play));

        // At the counter: the Block it raised on its own turn is gone, and a Paper Cut lands twice over.
        Assert.Equal(1, FightProbe.StacksOf(Body(play, room), Converter.Elites.WaitingRoom.AtTheCounterId));
        Assert.Equal(0, BlockOf(play, room));
        Assert.Equal(3, FightProbe.StacksOf(Body(play, room), Converter.Elites.WaitingRoom.NowServingId));
        var before = Health(play, room);
        Play(play, session, "paper_cut", room);
        Assert.Equal(before - 12, Health(play, room));

        // One card against a 3: missed, and the counter closes again.
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, room), Converter.Elites.WaitingRoom.AtTheCounterId));
        Assert.Equal(1, Patrons(play));
        play.Dispose();
    }

    // Missing the number seats another Patron, three at most; each sighs 3 damage at you every enemy turn, and
    // the Room's own attack counts them.
    [Fact]
    public void Missing_the_number_seats_patrons_who_make_the_wait_hurt()
    {
        var probe = FightProbe.Roster("waiting_missed", energy: 9,
            ("devouring_waiting_room", "everyone_waits_longer", 88));
        var (play, session, _) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList(), health: 400);

        var health = new List<int> { Hero(play).Health.Current };
        for (var turn = 0; turn < 4; turn++)
        {
            play.CombatDriver!.EndTurn(); // nothing played: never the number on the board
            Assert.Null(session.Error);
            health.Add(Hero(play).Health.Current);
        }

        Assert.Equal(Converter.Elites.WaitingRoom.MaxPatrons, Patrons(play));
        // The wait grows with the room: every turn costs more than the one before, until the chairs are full.
        var losses = health.Zip(health.Skip(1), (a, b) => a - b).ToList();
        Assert.True(losses[0] >= 5, string.Join(",", losses) + $" patrons {Patrons(play)}");
        Assert.True(losses[2] > losses[0], string.Join(",", losses));
        play.Dispose();
    }

    // The fight is the Room, not its queue: when the Room falls, the patrons go home with it.
    [Fact]
    public void When_the_room_falls_the_patrons_go_home()
    {
        var probe = FightProbe.Roster("waiting_closed", energy: 9,
            ("devouring_waiting_room", "chairs_creep_closer", 6));
        var (play, session, room) = FightProbe.Start(probe, Enumerable.Repeat("paper_cut", 10).ToList(), health: 400);

        play.CombatDriver!.EndTurn(); // nothing played: a Patron sits down
        Assert.Null(session.Error);
        Assert.Equal(1, Patrons(play));

        Play(play, session, "paper_cut", room);
        Assert.True(play.CombatDriver.Current is null || Patrons(play) == 0, "the patrons should leave with the Room");
        play.Dispose();
    }
}
