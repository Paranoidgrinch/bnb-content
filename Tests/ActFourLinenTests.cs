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

        play.CombatDriver!.EndTurn(); // 1 Fatigue
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        play.CombatDriver.EndTurn();  // the player's turn ends: the Fatigue is held, and the natron packs

        Assert.True(Hero(play).GetCounter(ActFour.DecaysPreserved) >= 1);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // A player carrying no affliction has nothing to hold in place, and nothing is packed.
    [Fact]
    public void A_round_that_held_nothing_buries_nothing()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Solo("natron_bearer", "pack_natron"), health: 800);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();

        Assert.Equal(0, Hero(play).GetCounter(ActFour.DecaysPreserved));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Linen-Wrapped Embalmer ────────────────────────────────────────────────────────────────────────

    // ── the Unfinished Mummy ──────────────────────────────────────────────────────────────────────────────

    // The first Deed of each turn catches on the hooks; the second goes through clean.
    [Fact]
    public void The_first_deed_of_a_turn_catches_on_the_hooks()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("unfinished_mummy", "stillness", energy: 3),
            deck: [.. Enumerable.Repeat(OneCost, 8)], health: 800);

        Play(play, session, OneCost, Enemies(play)[0].Id);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Play(play, session, OneCost, Enemies(play)[0].Id);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Fourfold Vessel Guardian ──────────────────────────────────────────────────────────────────────

}
