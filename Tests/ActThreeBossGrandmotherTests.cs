using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III boss 3 — Grandmother Clause. She forces nothing, which is the danger. Every gift is genuinely
// optional and declining is never a violation; accepting enough of them makes you a guest, and a guest is
// subject to household law.
public class ActThreeBossGrandmotherTests
{
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private const string AcceptedTea = "courtesy_tea";
    private const string AcceptedChair = "courtesy_chair";
    private const string AcceptedHoney = "courtesy_honey";
    private const string HouseRuleHoney = "house_rule_honey";
    private const string HouseRuleChair = "house_rule_chair";

    // Offer indices: 0 decline, then tea, chair, honey, slice.
    private const int Decline = 0;
    private const int Tea = 1;
    private const int Chair = 2;
    private const int Honey = 3;
    private const int Slice = 4;

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Granny(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int TrespassFrom(RunPlayback play, CombatantId filer) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)
                && s.SourceCombatantId == filer)
            .Sum(s => s.Stacks);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // The table is laid while the fight is being handed over, so she offers from the SECOND turn on.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Granny) Start(
        string intentId, IReadOnlyList<string> deck, int energy = 9, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ActThree.GrandmotherEnemyId, intentId, energy, statuses),
            deck: deck, health: 900);

    private static void NextTurnTaking(RunPlayback play, params int[] answers)
    {
        play.CombatDriver!.EndTurn();
        foreach (var answer in answers)
        {
            Assert.NotNull(play.CombatDriver.PendingOptionChoice);
            play.CombatDriver.SupplyOptionChoice([answer]);
        }
    }

    // ── the courtesies ────────────────────────────────────────────────────────────────────────────────────

    // Declining is never a violation, and costs nothing at all.
    [Fact]
    public void Declining_is_never_a_violation()
    {
        var (play, _, granny) = Start("ask_after_your_health", [.. Enumerable.Repeat(Working, 5)], energy: 0);

        NextTurnTaking(play, Decline);
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, TrespassFrom(play, granny));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // A courtesy is a gift and a promise in the same breath: the honey is Energy now, for a promise to leave
    // something in the purse at the bell.
    [Fact]
    public void A_courtesy_is_a_gift_and_a_promise()
    {
        var (play, _, _) = Start("ask_after_your_health", [.. Enumerable.Repeat(Working, 5)], energy: 3);

        NextTurnTaking(play, Honey);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), AcceptedHoney));
        // The purse is already full at the bell, so the sweetness comes as Block instead.
        Assert.Equal(3, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        Assert.True(Block(Hero(play)) >= 5);
        play.Dispose();
    }

    // A promise kept costs her 5 HP that no Block sees, and the courtesy is over.
    [Fact]
    public void A_promise_kept_costs_her_five()
    {
        var (play, _, _) = Start("ask_after_your_health", [.. Enumerable.Repeat(Working, 5)], energy: 3);

        NextTurnTaking(play, Honey);
        var health = Granny(play).Health.Current;

        play.CombatDriver!.EndTurn(); // the purse still has something in it

        Assert.Equal(health - 5, Granny(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), AcceptedHoney));
        play.Dispose();
    }

    // A promise broken is two violations at once — and one licence still refuses the whole of it, which is
    // exactly what a licence is for.
    [Fact]
    public void A_promise_broken_is_two_at_once()
    {
        var (play, session, granny) = Start("ask_after_your_health",
            [.. Enumerable.Repeat(Working, 5)], energy: 1);

        NextTurnTaking(play, Honey); // 1 Energy given, so 2 in the purse
        Play(play, session, Working, granny);
        Play(play, session, Working, granny); // …and both spent

        play.CombatDriver!.EndTurn();

        var refusals = play.CombatDriver.Current!.State.CombatLog
            .Where(e => e.Type == StandardCombatLogTypes.StatusApplicationBlocked)
            .Select(e => e.Message).ToList();
        Assert.Contains(refusals, m => m.Contains("prevented 2 stack(s)", StringComparison.Ordinal));
        Assert.Equal(0, TrespassFrom(play, granny));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), AcceptedHoney)); // and the courtesy is over
        play.Dispose();
    }

    // The chair asks you not to empty your hand, and gives 12 Block for it.
    [Fact]
    public void The_better_chair_guards_and_asks_you_to_keep_something()
    {
        var (play, session, granny) = Start("ask_after_your_health",
            [.. Enumerable.Repeat(Working, 5)], energy: 9);

        NextTurnTaking(play, Chair);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), AcceptedChair));
        Assert.True(Block(Hero(play)) >= 12);

        for (var i = 0; i < 5; i++)
            Play(play, session, Working, granny); // the hand is emptied

        var health = Granny(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(health, Granny(play).Health.Current); // no promise was kept
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Stay a Little Longer ──────────────────────────────────────────────────────────────────────────────

    // Three courtesies accepted and you are staying the night: her next action is the invitation, and it is
    // not a blow.
    [Fact]
    public void Three_courtesies_make_you_a_guest()
    {
        var (play, _, _) = Start("knitting_needle_precedent",
            [.. Enumerable.Repeat(Working, 5)], energy: 9);

        for (var turn = 0; turn < 3; turn++)
            NextTurnTaking(play, Chair);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before, Hero(play).Health.Current); // no direct attack occurs
        Assert.Equal(1, FightProbe.StacksOf(Granny(play), ActThree.HouseholdLawId));
        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.SafeConductId) >= 1);
        play.Dispose();
    }

    // Under household law she sets two places, and two promises kept at one table is a real evening.
    [Fact]
    public void Household_law_sets_two_places()
    {
        var (play, _, _) = Start("ask_after_your_health",
            [.. Enumerable.Repeat(Working, 5)], energy: 3,
            (ActThree.HouseholdLawId, 1));

        play.CombatDriver!.EndTurn();
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([Chair]);
        Assert.NotNull(play.CombatDriver.PendingOptionChoice); // a second place is set
        play.CombatDriver.SupplyOptionChoice([Honey]);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), AcceptedChair));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), AcceptedHoney));

        var health = Granny(play).Health.Current;
        var licences = FightProbe.StacksOf(Hero(play), ActThree.SafeConductId);
        play.CombatDriver.EndTurn(); // both promises kept: 5 each, and 4 more for the evening

        Assert.Equal(health - 14, Granny(play).Health.Current);
        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.SafeConductId) > licences);
        play.Dispose();
    }

    // ── Nothing Offered Is Forgotten ──────────────────────────────────────────────────────────────────────

    // "The Door Was Open for You — 16 +3 per Remembered Favour."
    [Fact]
    public void What_she_remembers_is_what_the_door_is_worth()
    {
        var (bare, _, _) = Start("the_door_was_open_for_you",
            [.. Enumerable.Repeat(Working, 5)], energy: 0);
        var before = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(before - 16, Hero(bare).Health.Current);
        bare.Dispose();

        var (play, _, _) = Start("the_door_was_open_for_you",
            [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.RememberedFavorId, 3));
        var start = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(start - 25, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── Because I Said So ─────────────────────────────────────────────────────────────────────────────────

    // Three remembered favours become a HOUSE RULE — a courtesy's condition with no gift attached, binding
    // whether anything was taken or not — and the favours are spent making it.
    [Fact]
    public void Three_favours_become_a_house_rule()
    {
        var (play, _, _) = Start("ask_after_your_health",
            [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.RememberedFavorId, 3));

        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Granny(play), ActThree.RememberedFavorId));
        Assert.Equal(2, FightProbe.StacksOf(Granny(play), HouseRuleChair));
        play.Dispose();
    }

    // A house rule binds you whether you took anything or not, and breaking it is two at once.
    [Fact]
    public void A_house_rule_binds_a_guest_who_took_nothing()
    {
        var (play, session, granny) = Start("ask_after_your_health",
            [.. Enumerable.Repeat(Working, 5)], energy: 5,
            (HouseRuleHoney, 2));

        NextTurnTaking(play, Decline); // nothing accepted at all
        for (var i = 0; i < 9; i++)
        {
            if (!play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Working))
                break;
            Play(play, session, Working, granny); // the purse is emptied
        }

        play.CombatDriver!.EndTurn();

        var refusals = play.CombatDriver.Current!.State.CombatLog
            .Where(e => e.Type == StandardCombatLogTypes.StatusApplicationBlocked)
            .Select(e => e.Message).ToList();
        Assert.Contains(refusals, m => m.Contains("prevented 2 stack(s)", StringComparison.Ordinal));
        play.Dispose();
    }

    // ── the signature ─────────────────────────────────────────────────────────────────────────────────────

    // "18 +3 per Claim +2 per Remembered Favour +1 per open Wergild point, to a maximum of 34."
    [Fact]
    public void The_signature_reads_everything_you_took()
    {
        var (play, _, _) = Start("you_accepted_the_hospitality",
            [.. Enumerable.Repeat(Working, 5)], energy: 0,
            (ActThree.ClaimId, 3), (ActThree.RememberedFavorId, 3));

        // 18 + 9 + 6 = 33, and the hospitality counter is still short of six, so the wound is what unlocks
        // it — the probe's Grandmother is at full health, so the slot is an ordinary blow instead.
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 16, Hero(play).Health.Current);
        play.Dispose();

        // Already a guest, so the invitation to stay is not queued in front of the signature.
        var probe = FightProbe.Solo(ActThree.GrandmotherEnemyId, "you_accepted_the_hospitality", 0,
            (ActThree.ClaimId, 3), (ActThree.RememberedFavorId, 3), (ActThree.HouseholdLawId, 1));
        var wounded = new EncounterDefinition(probe.Id,
            [probe.Enemies[0] with { MaxHealth = 80 }],
            probe.HeroResources, probe.HeroStartingStatuses, probe.HeroDisplayName,
            probe.CardsDrawnPerTurn, probe.TriggeredEffects);

        var (hurt, _, _) = FightProbe.Start(wounded,
            deck: [.. Enumerable.Repeat(Working, 5)], health: 900);
        var start = Hero(hurt).Health.Current;
        hurt.CombatDriver!.EndTurn();

        Assert.Equal(start - 33, Hero(hurt).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Granny(hurt), ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(Granny(hurt), ActThree.RememberedFavorId));
        hurt.Dispose();
    }
}
