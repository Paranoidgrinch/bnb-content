using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stages 9 and 10 — The Courts of the Royal Seal and The Processional Galleries, proved in live
// fights.
//
// Three bodies that do nothing to the player directly and change everything about what the others do: one
// authorises, one counterfeits, one legitimises. This is where §3.3 and §3.4 are tested — a copy may be
// answered like anything else, but it can never be the original a chain is measured from, and it can never
// feed the rule that made it.
public class ActFourSealTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    // ── the False-Seal Forger ─────────────────────────────────────────────────────────────────────────────

    // The forgery is convincing because it is the same thing again: the Cobra's one Poison becomes two, and
    // the second stack is the Forger's.
    [Fact]
    public void The_forger_adds_one_more_stack_of_the_same_thing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("counterfeit_venom", 3,
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("false_seal_forger", "counterfeit_seal", null)),
            health: 700);

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));
        play.Dispose();
    }

    // …and exactly one more: a forgery is not itself worth forging, which is the loop §3.4 exists to close.
    // The Forger's own Doubt is not foreign either, so nothing it does feeds it.
    [Fact]
    public void A_forgery_is_never_itself_forged()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("counterfeit_own", 3,
                ("false_seal_forger", "forgery_setup", null)),
            health: 700);

        play.CombatDriver!.EndTurn();

        // One Doubt, its own: no forger counterfeits its own paper.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "doubt"));
        play.Dispose();
    }

    // Once a round, however many afflictions land.
    [Fact]
    public void Only_the_first_affliction_of_a_round_is_counterfeited()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("counterfeit_twice", 3,
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("palette_bearing_apprentice", "fresh_pigment_entry", null),
                ("false_seal_forger", "counterfeit_seal", null)),
            health: 800);

        play.CombatDriver!.EndTurn(); // venom, then the scribe's entry — one round, two originals

        // The venom was forged (1 + 1); the register is only what the scribe wrote, plus its own pigment.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // ── the Sun-Seal Bearer ───────────────────────────────────────────────────────────────────────────────

    // The seal authorises its own side's first affliction — but only while the impression is intact, and
    // pressing it costs Block.
    [Fact]
    public void The_seal_authorizes_the_first_affliction_while_it_is_intact()
    {
        // The Bearer acts FIRST: a combatant's Block is cleared at its own turn start, so the impression is
        // only intact for what happens after it takes one. The authored encounters put it first for exactly
        // this reason.
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("authorized_venom", 3,
                ("sun_seal_bearer", "royal_impression", null),
                ("cobra_of_the_entry_mark", "entry_venom", null)),
            health: 700);

        play.CombatDriver!.EndTurn(); // 25 Block, then the venom — and the seal is pressed for it

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));
        Assert.Equal(25 - 6, Body(play, "sun_seal_bearer").DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        play.Dispose();
    }

    // …and a bearer whose impression is gone authorises nothing.
    [Fact]
    public void A_bearer_without_block_authorizes_nothing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("unsealed_venom", 3,
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("sun_seal_bearer", "seal_strike", null)),
            health: 700);

        play.CombatDriver!.EndTurn(); // no Royal Impression this turn: no Block, no authority

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "poison"));
        play.Dispose();
    }

    // Encounter 30's order, in one fight: the original lands, the seal authorises it, and the forger adds
    // exactly one more — and NOT one for the seal's copy as well, because a replicated application is never
    // the round's original (§3.3).
    [Fact]
    public void An_authorized_original_is_forged_once_and_no_further()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("authorized_counterfeit", 3,
                ("sun_seal_bearer", "royal_impression", null),
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("false_seal_forger", "counterfeit_seal", null)),
            health: 800);

        play.CombatDriver!.EndTurn();

        // 1 original + 1 authorised + 1 forged. Not four, and not a cascade.
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), "poison"));
        play.Dispose();
    }

    // ── the Kneeling Petitioners ──────────────────────────────────────────────────────────────────────────

    // The procession braces the moment anything official lands — every body still standing.
    [Fact]
    public void The_procession_braces_when_an_affliction_lands()
    {
        // The procession kneels first: a body's Block is cleared at ITS own turn start, so bracing a body
        // that has not acted yet would be swept away in the same breath.
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("processional_seal", 3,
                ("kneeling_petitioners", "kneel_in_unison", null),
                ("cobra_of_the_entry_mark", "entry_venom", null)),
            health: 700);

        play.CombatDriver!.EndTurn();

        foreach (var body in Enemies(play))
            Assert.True(body.DefensivePools[StandardCombatIds.BlockDefensivePool].Current >= 7,
                $"{body.DefinitionId.value} did not brace");
        play.Dispose();
    }

    // …and a FORGED affliction legitimises just as well: submission does not check paperwork, it only has to
    // look official (§3.3 — a replicated application may trigger this).
    //
    // The case is reachable exactly once: the procession's OWN chant is not foreign, so it wins no approval —
    // and the forgery of it, being the Forger's, is.
    [Fact]
    public void Even_a_forged_affliction_looks_official_enough()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("false_sealed_petition", 3,
                ("kneeling_petitioners", "petition_chant", null),
                ("false_seal_forger", "counterfeit_seal", null)),
            health: 700);

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "doubt")); // chanted once, forged once

        // …and the procession braced for the forgery. (The latch itself is not worth asserting from here:
        // it is cleared when the round turns, which happens before this line is reached.)
        Assert.True(
            Body(play, "kneeling_petitioners").DefensivePools[StandardCombatIds.BlockDefensivePool].Current >= 7,
            "the procession did not brace for the forgery");
        play.Dispose();
    }
}
