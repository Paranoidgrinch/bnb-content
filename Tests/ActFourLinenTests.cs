using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stages 11 and 12 — The House of Linen and The Canopic Vaults, proved in live fights.
//
// Preservation stops being a favour here: everything the linen holds in place is one more thing packed
// around you. The tests follow the four conversions the stage is built out of — held decay into burial, a
// register-thickened wrapping into weight, a blow struck while preserved into burial, and the canopic
// bureaucracy's one office a turn.
public class ActFourLinenTests
{
    private const string OneCost = "paper_cut";  // Deed, 1 — this act's word for an attack

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Natron Bearer ─────────────────────────────────────────────────────────────────────────────────

    // Its own rite makes both halves — the Fatigue that would fade and the natron that stops it — and drying
    // is burial.
    [Fact]
    public void Drying_what_would_decay_packs_the_player_deeper()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("natron_bearer", "drying_rite"), health: 800);

        play.CombatDriver!.EndTurn(); // 1 Fatigue and 1 Embalmed
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        play.CombatDriver.EndTurn();  // the player's turn ends: the Fatigue is held, and the natron packs

        Assert.True(Hero(play).GetCounter(ActFour.DecaysPreserved) >= 1);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // A round in which nothing was held buries nothing: what it dries is preservation, not affliction.
    [Fact]
    public void A_round_that_held_nothing_buries_nothing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("natron_bearer", "natron_dust", energy: 3, ("fatigue", 1)),
            health: 800);

        play.CombatDriver!.EndTurn(); // the Fatigue fades of its own accord — nothing is dried

        Assert.Equal(0, Hero(play).GetCounter(ActFour.DecaysPreserved));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Linen-Wrapped Embalmer ────────────────────────────────────────────────────────────────────────

    // It writes the instructions on one turn and wraps to them on the next: the register thickens the
    // wrapping, and a wrapping that tight is weight.
    [Fact]
    public void A_wrapping_the_register_thickened_leaves_you_carrying_weight()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_linen_02"), health: 800);

        play.CombatDriver!.EndTurn(); // Write Instructions: 1 Inscribed
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        play.CombatDriver.EndTurn();  // Wrap Tight: the register makes it 2, and the weight follows

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId)); // spent doing it
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // …and a wrapping nobody wrote instructions for is just a wrapping.
    [Fact]
    public void An_unwritten_wrapping_costs_nothing_extra()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("linen_wrapped_embalmer", "wrap_tight"), health: 800);

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // ── the Unfinished Mummy ──────────────────────────────────────────────────────────────────────────────

    // Preservation is not a state you move around in: the first Deed struck while Embalmed catches on the
    // hooks, and only the first.
    [Fact]
    public void The_first_blow_struck_while_preserved_catches_on_the_hooks()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("unfinished_mummy", "stillness", energy: 5,
                (ActFour.EmbalmedId, 3)),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 800);

        var mummy = Enemies(play)[0].Id;
        Play(play, session, OneCost, mummy);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        Play(play, session, OneCost, mummy);  // the second blow is free of them
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        play.CombatDriver!.EndTurn();          // …until the next turn
        Play(play, session, OneCost, mummy);

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // An unpreserved player strikes freely: the hooks catch on the linen, not on the blow.
    [Fact]
    public void An_unpreserved_player_strikes_freely()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("unfinished_mummy", "stillness", energy: 3),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 800);

        Play(play, session, OneCost, Enemies(play)[0].Id);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Fourfold Vessel Guardian ──────────────────────────────────────────────────────────────────────

    // One office a turn, in order, and the office is a FACE: which vessel is open can be read off the body,
    // and only the open one does anything.
    [Fact]
    public void The_guardian_works_one_office_a_turn_and_wears_which()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_vaults_01"), health: 900);

        var offices = new[]
        {
            (ActFour.OfficeOfTheBodyId, ActFour.BurdenedId),
            (ActFour.OfficeOfTheBreathId, "panic"),
            (ActFour.OfficeOfTheBloodId, "poison"),
            (ActFour.OfficeOfTheNameId, ActFour.InscribedId),
        };

        foreach (var (office, applied) in offices)
        {
            play.CombatDriver!.EndTurn();

            var guardian = Enemies(play)[0];
            Assert.Equal(1, FightProbe.StacksOf(guardian, office));

            // …and no other office is open at the same time.
            foreach (var (other, _) in offices.Where(o => o.Item1 != office))
                Assert.Equal(0, FightProbe.StacksOf(guardian, other));

            Assert.True(FightProbe.StacksOf(Hero(play), applied) >= 1,
                $"the {office} office applied nothing");
        }

        play.Dispose();
    }

    // Encounter 40's chain in one turn pair: the Guardian's Name office writes the register, and the
    // Embalmer's wrapping is thickened by it.
    [Fact]
    public void The_name_office_feeds_the_embalmers_wrapping()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("vessel_of_the_name", 3,
                ("fourfold_vessel_guardian", "name_office", null),
                ("linen_wrapped_embalmer", "wrap_tight", null)),
            health: 900);

        play.CombatDriver!.EndTurn(); // the Name office writes 1 Inscribed, and the wrapping is thickened

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }
}
