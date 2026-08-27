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

    // The gift carries her name, which is the whole point: only her own stacks count.
    [Fact]
    public void Her_courtesy_is_hers_and_says_so()
    {
        var (play, _, witchling) = FightProbe.Start(
            FightProbe.Solo("roadside_witchling", "courtesy_gift"));

        Assert.Equal(0, LicencesFrom(play, witchling));
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, LicencesFrom(play, witchling));
        play.Dispose();
    }

    // Carried through a whole turn unspent, the courtesy turns into a grievance — which the licence you were
    // saving then pays for. Refusing her own gift's grievance with the licence she gave you is exactly the
    // trap the identity is: the gift is real, and it is not free.
    [Fact]
    public void Carrying_her_gift_a_whole_turn_unspent_is_rude()
    {
        var (play, _, witchling) = FightProbe.Start(
            FightProbe.Solo("roadside_witchling", "courtesy_gift"), health: 200);

        play.CombatDriver!.EndTurn(); // she gives: the opening licence and hers
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver.EndTurn();  // a whole player turn passes unspent — and she files for it

        // One licence paid for the grievance, and she handed over another afterwards.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(2, LicencesFrom(play, witchling)); // both of the two are now hers
        play.Dispose();
    }

    // Spend it and she is pleased. The fight's own opening licence is spent first — the engine takes the
    // oldest — so this needs a turn where only hers is left to pay.
    [Fact]
    public void Spending_her_gift_pleases_her()
    {
        var (play, session, witchling) = FightProbe.Start(
            FightProbe.Roster("covenants", energy: 9,
                ("roadside_witchling", "courtesy_gift", null),
                ("permit_hare", "stamp_passage", null)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 300);

        var hare = Enemies(play)[1].Id;
        var witchlingId = Enemies(play)[0].Id;
        // Burn the fight's opening licence on the Hare's third-card rule — and hit HER while doing it, so
        // that there is room for her to recover afterwards.
        Play(play, session, OneCost, witchlingId);
        Play(play, session, OneCost, witchlingId);
        Play(play, session, OneCost, witchlingId);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn(); // she gives, and the Hare only blocks
        Assert.Equal(1, LicencesFrom(play, Enemies(play)[0].Id));

        var before = Enemies(play)[0].Health.Current;
        Play(play, session, OneCost, hare);
        Play(play, session, OneCost, hare);
        Play(play, session, OneCost, hare); // the third card again: HER licence pays for it
        Assert.Equal(0, LicencesFrom(play, Enemies(play)[0].Id));

        play.CombatDriver.EndTurn(); // the reckoning finds fewer of hers than the turn began with

        Assert.Equal(before + 6, Enemies(play)[0].Health.Current);
        play.Dispose();
    }

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

    [Fact]
    public void The_cup_pours_every_second_turn()
    {
        var (play, _, cup) = FightProbe.Start(
            FightProbe.Solo("crossroads_cup", "silver_rim"), health: 200);

        Assert.Equal(0, LicencesFrom(play, cup));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, LicencesFrom(play, cup)); // the second player turn begins
        play.CombatDriver.EndTurn();
        Assert.Equal(1, LicencesFrom(play, cup)); // …and the third does not
        play.CombatDriver.EndTurn();
        Assert.Equal(2, LicencesFrom(play, cup)); // …but the fourth does
        play.Dispose();
    }

    // The Cup's generosity is a debt engine: the first licence you spend each turn is recognised as somebody
    // else's standing.
    [Fact]
    public void Spending_a_licence_creates_standing_somewhere_else()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("covenants", energy: 9,
                ("crossroads_cup", "silver_rim", null),
                ("permit_hare", "stamp_passage", null)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 300);

        var hare = Enemies(play)[1].Id;
        Play(play, session, OneCost, hare);
        Play(play, session, OneCost, hare);
        Play(play, session, OneCost, hare); // the Hare's third-card rule, refused by the opening licence

        // Somebody was recognised for it — and the Cup holds none, so the Cup is the one with the fewest.
        var claims = Enemies(play).Sum(e => FightProbe.StacksOf(e, ActThree.ClaimId));
        Assert.Equal(1, claims);
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.CupPouredThisTurnId));
        play.Dispose();
    }

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
