using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Old-Growth Precedents. The standard does not change arbitrarily; it hardens because earlier
// disputes hardened it. A stump whose own law tightens with every dispute it has won, lichen that invents no
// law and only cites older authority, and a root that keeps what the forest remembers.
public class ActThreeStageEightTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static IReadOnlyList<StatusInstance> Trespasses(RunPlayback play) =>
        [.. Hero(play).Statuses.Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private const string OneCost = "paper_cut";

    // ── Sleeping Stump Auditor — The Old Measure ──────────────────────────────────────────────────────────

    // The stump sleeps through the first turn: it has no measure to compare against yet.
    [Fact]
    public void The_first_turn_sets_the_measure_and_costs_nothing()
    {
        var (play, session, stump) = FightProbe.Start(
            FightProbe.Solo("sleeping_stump_auditor", "rooted_stay", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 300);

        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Empty(Trespasses(play));
        play.Dispose();
    }

    [Fact]
    public void Playing_more_than_last_turn_owes_the_stump()
    {
        var (play, session, stump) = FightProbe.Start(
            FightProbe.Solo("sleeping_stump_auditor", "rooted_stay", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 300);

        Play(play, session, OneCost, stump); // one card in the first turn
        play.CombatDriver!.EndTurn();

        Play(play, session, OneCost, stump); // the same again is no more than last turn
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, OneCost, stump); // and this one is

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    // Rings of Precedent: the measure costs one Trespass, and one more for every dispute the Stump has won.
    [Fact]
    public void Every_dispute_the_stump_has_won_hardens_the_measure()
    {
        var (play, session, stump) = FightProbe.Start(
            FightProbe.Solo("sleeping_stump_auditor", "old_measure", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        // Its intent files one a turn: one refused, three that land, and it wins its first dispute.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));

        var before = Trespasses(play).Sum(t => t.Stacks);
        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump); // more cards than the turns of doing nothing before it

        // Two Trespass from one breach: the law itself, and the ring of precedent behind it.
        Assert.Equal(before + 2, Trespasses(play).Sum(t => t.Stacks));
        play.Dispose();
    }

    // ── Precedent Lichen — Cited Authority ────────────────────────────────────────────────────────────────

    // The Lichen invents no law. Granted standing, it cites the last one anybody was found to have broken —
    // and from then on that law is authority for two separate parties, which is what Encounter 31 is for.
    [Fact]
    public void The_lichen_cites_the_authority_the_fight_has_established()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("precedents", energy: 9,
                ("precedent_lichen", "cite_authority", null),
                ("sleeping_stump_auditor", "rooted_stay", null)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 600);

        var lichen = Enemies(play)[0].Id;
        var stump = Enemies(play)[1].Id;

        Play(play, session, OneCost, stump);      // one card: the measure is set
        play.CombatDriver!.EndTurn();             // the Lichen files; the opening licence refuses it

        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);      // two is more than one — the Stump's law is broken, and
        Assert.Contains(Trespasses(play), t => t.SourceCombatantId == stump); // the fight now has authority
        play.CombatDriver.EndTurn();

        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);      // two again: no breach
        play.CombatDriver.EndTurn();
        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);
        play.CombatDriver.EndTurn();              // the Lichen's third landed filing — and it is granted one

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));

        var before = Trespasses(play).Count(t => t.SourceCombatantId == lichen);
        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);
        Play(play, session, OneCost, stump);      // three: the same law broken again

        Assert.True(Trespasses(play).Count(t => t.SourceCombatantId == lichen) > before,
            "the cited authority is authority for the Lichen too");
        play.Dispose();
    }

    // ── Footfall Root — Deep Memory ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_granted_claim_becomes_a_memory()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("footfall_root", "remember_footstep"), health: 500);

        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.MemoryId));
        play.Dispose();
    }

    // Settlement may extinguish the Claim. It cannot extinguish what the forest remembers — which is why the
    // two are separate things rather than one number.
    [Fact]
    public void Memory_outlasts_the_claim_it_came_from()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("precedents",
                ("footfall_root", "remember_footstep", null),
                ("the_sedge_bench", "hold_under_review", null)),
            health: 600);

        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();
        var root = Enemies(play)[0];
        Assert.Equal(1, FightProbe.StacksOf(root, ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(root, ActThree.MemoryId));

        // The Bench takes the matter up; the Root's Claim can now be extinguished, and its Memory cannot.
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.UnderReviewId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.MemoryId));
        play.Dispose();
    }

    // Memory Crush hits harder for each thing the root remembers.
    [Fact]
    public void Memory_crush_hits_harder_for_what_is_remembered()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("footfall_root", "memory_crush", ("memory", 3)), health: 500);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - (18 + 9), Hero(play).Health.Current);
        play.Dispose();
    }
}
