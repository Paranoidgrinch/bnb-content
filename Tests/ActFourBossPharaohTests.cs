using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Pharaoh of the Sealed Name, proved in live fights.
//
// Everything here is one question asked every turn: is THIS command worth less than the Authority refusing it
// hands over? So the tests follow the ward down through obedience, up through refusal, and out the far side
// into the exposure window — and then check that taking the next name wipes the ledger clean.
public class ActFourBossPharaohTests
{
    private const string OneCost = "paper_cut";   // Deed, 1: deal 6
    private const string Wax = "waxen_surety";    // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState King(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("pharaoh", StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Court(
        string intent = "those_not_counted_kneel", int energy = 3, IReadOnlyList<string>? deck = null) =>
        FightProbe.Start(
            FightProbe.Solo("pharaoh_of_the_sealed_name", intent, energy),
            deck: deck ?? [.. Enumerable.Repeat(OneCost, 20)], health: 900);

    // The ward opens whole, and the first command is standing before the player acts.
    [Fact]
    public void The_ward_opens_whole_and_a_command_is_already_standing()
    {
        var (play, _, _) = Court();

        Assert.Equal(ActFour.WardFull, FightProbe.StacksOf(King(play), ActFour.CartoucheWardId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.CommandMeasureId));
        play.Dispose();
    }

    // Obey and the legitimacy is stripped 18 — which no blow can do.
    [Fact]
    public void Obeying_a_command_strips_eighteen_of_the_ward()
    {
        var (play, session, king) = Court();

        Play(play, session, OneCost, king);
        Play(play, session, OneCost, king);  // exactly 2 Energy: Measure the Throne
        play.CombatDriver!.EndTurn();

        Assert.Equal(ActFour.WardFull - 18, FightProbe.StacksOf(King(play), ActFour.CartoucheWardId));
        Assert.Equal(0, FightProbe.StacksOf(King(play), ActFour.AuthorityId));
        play.Dispose();
    }

    // Refuse and it heals 9 while his Authority grows — and Authority is real damage on his own blows.
    [Fact]
    public void Refusing_heals_the_ward_and_builds_authority()
    {
        var (play, _, _) = Court();

        play.CombatDriver!.EndTurn();  // nothing spent: the command is refused
        Assert.Equal(ActFour.WardFull, FightProbe.StacksOf(King(play), ActFour.CartoucheWardId));
        Assert.Equal(1, FightProbe.StacksOf(King(play), ActFour.AuthorityId));

        for (var i = 0; i < 5; i++)
            play.CombatDriver.EndTurn();

        // Four is all the legitimacy one name can hold.
        Assert.Equal(ActFour.AuthorityCap, FightProbe.StacksOf(King(play), ActFour.AuthorityId));
        play.Dispose();
    }

    // Take the ward to nothing and the NAME IS EXPOSED for a whole player turn: nothing re-forms behind it,
    // and everything lands a quarter harder. Then the ward comes back at 18, not at 36.
    [Fact]
    public void Stripping_the_ward_exposes_the_name_for_one_whole_turn()
    {
        // A king who is only striking, so nothing he does changes what the player's cards cost — obeying two
        // commands in a row is the point of this test, and a surcharge would quietly break the second.
        var (play, session, king) = Court("royal_audience_ends");

        // Two obeyed commands take 36 down to nothing. Two one-cost cards a turn answers both of the throne
        // name's demands: exactly 2 spent, and exactly 1 left unspent.
        for (var turn = 0; turn < 2; turn++)
        {
            Play(play, session, OneCost, king);
            Play(play, session, OneCost, king);
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal(0, FightProbe.StacksOf(King(play), ActFour.CartoucheWardId));
        Assert.Equal(1, FightProbe.StacksOf(King(play), ActFour.NameExposedId));

        // Exposed: 6 becomes 7 (a quarter more), and no ward stands in the way of it.
        var standing = King(play).Health.Current;
        Play(play, session, OneCost, king);
        Assert.Equal(7, standing - King(play).Health.Current);

        // One whole turn of it, and then the ring re-forms at half.
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(King(play), ActFour.NameExposedId));
        Assert.Equal(ActFour.WardReformed, FightProbe.StacksOf(King(play), ActFour.CartoucheWardId));
        play.Dispose();
    }

    // …and taking the next name wipes the ledger: the ward whole again, the Authority gone, and something
    // handed to the player to carry into it. It is not an attack.
    [Fact]
    public void Taking_the_next_name_resets_the_ward_and_the_authority()
    {
        // Fielded one blow above the band, because the ward makes the real descent long and this test is
        // about what happens when it is crossed.
        var (play, session, king) = FightProbe.Start(
            FightProbe.Roster("pharaoh_band", energy: 9,
                ("pharaoh_of_the_sealed_name", "those_not_counted_kneel", 430)),
            deck: [.. Enumerable.Repeat(OneCost, 20)], health: 900);

        play.CombatDriver!.EndTurn();  // refuse once, so there is Authority to lose
        Assert.Equal(1, FightProbe.StacksOf(King(play), ActFour.AuthorityId));

        while (King(play).Health.Current > 420)
            Play(play, session, OneCost, king);

        var king2 = King(play);
        Assert.Equal(1, FightProbe.StacksOf(king2, ActFour.TwoLandsNameId));
        Assert.Equal(ActFour.WardFull, FightProbe.StacksOf(king2, ActFour.CartoucheWardId));
        Assert.Equal(0, FightProbe.StacksOf(king2, ActFour.AuthorityId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // The second name asks about ORDER rather than about amount: lead with a Deed, or lead with a Working.
    [Fact]
    public void The_second_name_asks_which_hand_you_lead_with()
    {
        var (play, session, king) = FightProbe.Start(
            FightProbe.Roster("pharaoh_second_name", energy: 9,
                ("pharaoh_of_the_sealed_name", "those_not_counted_kneel", 430)),
            deck: [.. Enumerable.Repeat(OneCost, 20)], health: 900);

        while (King(play).Health.Current > 420)
            Play(play, session, OneCost, king);
        play.CombatDriver!.EndTurn();

        var command = new[] { ActFour.CommandSouthId, ActFour.CommandNorthId }
            .Count(id => FightProbe.StacksOf(Hero(play), id) > 0);
        Assert.Equal(1, command);
        play.Dispose();
    }
}
