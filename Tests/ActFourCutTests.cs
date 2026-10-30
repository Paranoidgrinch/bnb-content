using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-IV content cut of 2026-10-30: the new bodies of the Necropolis and the Hall of Two Truths, and the
// gate that every law of the act sits on a chip the player can read.
public class ActFourCutTests
{
    private const string Deed = "paper_cut";              // 1 Energy, Deed: deal 6
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working

    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── every law on a chip ───────────────────────────────────────────────────────────────────────────────

    // The bodies whose whole identity is their visible intent cycle (a measure, a swarm, a clock that is shown
    // as it fills, four offices named in turn); everyone else opens the fight wearing its law.
    private static readonly HashSet<string> PatternBodies = new(StringComparer.Ordinal)
    {
        "crocodile_of_the_short_measure", "jar_seal_scarab_swarm", "fallen_capstone_golem",
        "fourfold_vessel_guardian", "eclipse_scarab",
    };

    [Fact]
    public void Every_law_of_the_act_is_a_chip_and_the_shared_law_is_told_once()
    {
        var logOnly = LogOnlyStatuses.Ids;
        foreach (var encounter in FightProbe.Shipped.Encounters.Where(e => e.Id.Value.StartsWith("labyrinth_", StringComparison.Ordinal)))
            foreach (var enemy in encounter.Enemies.Where(e => !PatternBodies.Contains(e.Id)))
                Assert.True(
                    (enemy.StartingStatuses ?? []).Any(s => !logOnly.Contains(s.Status.value)),
                    $"{enemy.Id} in {encounter.Id.Value} opens with no readable rule");

        Assert.Contains(ActFour.LabyrinthBodyId, logOnly);
        Assert.Contains(Tutorial.Steps, step => step.Moment == "act4.labyrinth");
    }

    // ── the Necropolis of Thieves ─────────────────────────────────────────────────────────────────────────

    // The Jackal guards its ally: a hit on the guarded one lands at half, and the Jackal takes the same.
    [Fact]
    public void The_jackal_takes_half_of_every_hit_on_the_ally_it_guards()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("jackal", energy: 9,
                ("jackal_of_the_western_gate", "bite_at_the_gate", 200), ("cursed_loot_bearer", "loot_swing", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 900);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        var jackal = Find(play, "jackal_of_the_western_gate");
        var loot = Find(play, "cursed_loot_bearer");
        Assert.Contains(Body(play, loot).Statuses, x => x.DefinitionId.value == ActFour.GuardedId);

        var (jackalBefore, lootBefore) = (Body(play, jackal).Health.Current, Body(play, loot).Health.Current);
        Play(play, session, Deed, loot);
        Assert.Equal(3, lootBefore - Body(play, loot).Health.Current);
        Assert.Equal(3, jackalBefore - Body(play, jackal).Health.Current);
        play.Dispose();
    }

    // Killing the Cursed Loot Bearer writes the claim down for the run: Gold and a curse after the fight.
    [Fact]
    public void Killing_the_loot_bearer_claims_the_loot()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("loot", energy: 9,
                ("cursed_loot_bearer", "loot_swing", 5), ("jackal_of_the_western_gate", "stand_the_watch", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 900);

        // Guarded by the Jackal, so it takes two cuts at half.
        var loot = Find(play, "cursed_loot_bearer");
        Play(play, session, Deed, loot);
        Assert.Equal(0, Hero(play).GetCounter(ActFour.LootClaimed));
        Play(play, session, Deed, loot);
        Assert.Equal(1, Hero(play).GetCounter(ActFour.LootClaimed));
        play.Dispose();
    }

    // ── the Hall of Two Truths ────────────────────────────────────────────────────────────────────────────

    // A clean turn before the Assessor is a Feather; playing the named sin is 1 Entombed and no Feather.
    [Fact]
    public void The_assessor_names_a_sin_and_a_clean_turn_is_a_feather()
    {
        var (play, session, assessor) = FightProbe.Start(
            FightProbe.Solo("assessor_of_the_forty_two", "weigh_the_answer", energy: 9),
            [.. Enumerable.Repeat(Deed, 5), .. Enumerable.Repeat(Working, 5)], health: 900);

        play.CombatDriver!.EndTurn();  // nothing played: clean
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.FeatherOfTruthId));

        // Walk to a turn whose sin is a Deed or a Working, and commit it.
        for (var guard = 0; guard < 3 && FightProbe.StacksOf(Body(play, assessor), ActFour.SinRitesId) > 0; guard++)
            play.CombatDriver.EndTurn();
        var feathers = FightProbe.StacksOf(Hero(play), ActFour.FeatherOfTruthId);
        var sin = FightProbe.StacksOf(Body(play, assessor), ActFour.SinDeedsId) > 0 ? Deed : Working;
        if (!play.CombatDriver.Current!.Hand.Any(c => c.DefinitionId.value == sin))
        {
            play.Dispose();
            return;
        }
        Play(play, session, sin, assessor);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.CombatDriver.EndTurn();
        Assert.Equal(feathers, FightProbe.StacksOf(Hero(play), ActFour.FeatherOfTruthId));
        play.Dispose();
    }
}
