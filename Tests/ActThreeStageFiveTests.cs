using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Wayside Covenants, the stage where the act is NICE to you. All three parties hand out
// Safe-Conduct and all three want something back: the Witchling wants her gift used, the Bride wants the
// relationship to progress, the Cup wants its generosity to create obligation somewhere else.
public class ActThreeStageFiveTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static int LicencesFrom(RunPlayback play, CombatantId giver) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.SafeConductId)
                && s.SourceCombatantId == giver)
            .Sum(s => s.Stacks);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private const string OneCost = "paper_cut";
    private const string TwoCost = "permit_a38";

    // ── Roadside Witchling — Courtesy Safe-Conduct ────────────────────────────────────────────────────────

    // ── The Blackthorn Bride ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Two_heavy_promises_in_a_row_owe_the_bride()
    {
        var (play, session, bride) = FightProbe.Start(
            FightProbe.Solo("blackthorn_bride", "veil_of_thorns", energy: 9),
            deck: [TwoCost, TwoCost, OneCost, TwoCost, OneCost]);

        Play(play, session, TwoCost, bride);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, TwoCost, bride); // two of Base Cost 2 with nothing between them

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    [Fact]
    public void Something_cheap_between_them_keeps_the_promise()
    {
        var (play, session, bride) = FightProbe.Start(
            FightProbe.Solo("blackthorn_bride", "veil_of_thorns", energy: 9),
            deck: [TwoCost, OneCost, TwoCost, OneCost, OneCost]);

        Play(play, session, TwoCost, bride);
        Play(play, session, OneCost, bride);
        Play(play, session, TwoCost, bride);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // Welcome, then commitment, then obligation: her first Claim is a gift and her second is a bill.
    [Fact]
    public void The_brides_first_claim_is_a_welcome_and_her_second_is_a_bill()
    {
        var (play, _, bride) = FightProbe.Start(
            FightProbe.Solo("blackthorn_bride", "thorn_vow"), health: 400);

        // Thorn Vow files one Trespass a turn: the first is refused, the next three make her first Claim.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(1, LicencesFrom(play, bride)); // welcomed

        // The welcome's licence eats the next Trespass, so three more turns make the second Claim.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(2, Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == bride)
            .Sum(s => s.Stacks));
        play.Dispose();
    }

    // ── Crossroads Cup — Drink Before Choosing ────────────────────────────────────────────────────────────

    // Once a turn: the Cup makes one toast, not one per licence.
    [Fact]
    public void The_cup_makes_one_toast_a_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_covenants_duo_01", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 400);

        var witchling = Enemies(play)[1].Id;
        play.CombatDriver!.EndTurn(); // the Witchling gives, the Cup pours: two licences in hand

        // Two violations in one turn, and only one of them can be recognised.
        Play(play, session, OneCost, witchling);
        Play(play, session, OneCost, witchling);
        Play(play, session, OneCost, witchling);

        Assert.True(Enemies(play).Sum(e => FightProbe.StacksOf(e, ActThree.ClaimId)) <= 1);
        play.Dispose();
    }
}
