using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, elites 4–6, proved in live fights. Between them they read the act's last three words at boss grade:
// the register, preservation, and what a turn is actually worth.
public class ActFourEliteReadingTests
{
    private const string OneCost = "paper_cut";   // Deed, 1: deal 6
    private const string TwoCost = "permit_a38";  // 2
    private const string Wax = "waxen_surety";    // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static bool Holds(RunPlayback play, string cardId) =>
        play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == cardId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    // ── the Keeper of the Living Cartouche ────────────────────────────────────────────────────────────────

    // The register enlarging an affliction is a BLACK glyph. The Keeper opens the cartouche, then writes a
    // burden into the enlarged space, and the glyph goes up.
    [Fact]
    public void An_amplified_affliction_writes_a_black_glyph()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle("keeper_of_the_living_cartouche",
                "open_the_cartouche", "write_the_burden"),
            health: 900);

        play.CombatDriver!.EndTurn();  // Inscribed 2
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));

        play.CombatDriver.EndTurn();   // …and a burden written into it: the register pays, the glyph lands
        var keeper = Body(play, "keeper_of_the_living_cartouche");
        Assert.Equal(1, FightProbe.StacksOf(keeper, ActFour.BlackGlyphId));
        Assert.Equal(1, keeper.GetCounter(ActFour.Glyphs));
        Assert.Equal(0, FightProbe.StacksOf(keeper, ActFour.GoldenGlyphId));
        play.Dispose();
    }

    // …and the register spent on a blessing of the player's own is a GOLDEN one, which is the whole decision
    // the body exists to put in front of them.
    [Fact]
    public void A_blessing_the_player_amplifies_writes_a_golden_glyph()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("keeper_of_the_living_cartouche", "open_the_cartouche"),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        play.CombatDriver!.EndTurn();  // Inscribed 2
        Play(play, session, Wax, null);  // 4 Ward Wax, made 5 by the register

        var keeper = Body(play, "keeper_of_the_living_cartouche");
        Assert.Equal(1, FightProbe.StacksOf(keeper, ActFour.GoldenGlyphId));
        Assert.Equal(0, FightProbe.StacksOf(keeper, ActFour.BlackGlyphId));
        Assert.Equal(5, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    // Three glyphs and the name is read back. Three black ones is 14 + 18 to the face, and the slots clear.
    [Fact]
    public void Three_black_glyphs_read_the_name_at_its_hardest()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle("keeper_of_the_living_cartouche",
                "open_the_cartouche", "write_the_burden", "write_misfortune"),
            health: 900);

        // Open, burden (glyph), misfortune (Doubt and Panic — the register is spent on the first of them).
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();

        var keeper = Body(play, "keeper_of_the_living_cartouche");
        var glyphs = keeper.GetCounter(ActFour.Glyphs);
        Assert.True(glyphs is 1 or 2, $"the cartouche recorded {glyphs} glyphs from two amplified writings");

        // Fill it the rest of the way and the name is read.
        while (Body(play, "keeper_of_the_living_cartouche").GetCounter(ActFour.Glyphs) < ActFour.GlyphsToRead)
            play.CombatDriver.EndTurn();

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();

        var read = Body(play, "keeper_of_the_living_cartouche");
        Assert.Equal(0, read.GetCounter(ActFour.Glyphs));
        Assert.Equal(0, FightProbe.StacksOf(read, ActFour.BlackGlyphId));
        Assert.True(before - Hero(play).Health.Current >= 14 + (3 * 6),
            "three black glyphs did not read at their full weight");
        play.Dispose();
    }

    // ── the Mummified Overseer of the Linen House ─────────────────────────────────────────────────────────

    // Preservation holding an affliction in place tightens the wrapping — at most twice a round, however
    // many were held.
    [Fact]
    public void Held_afflictions_tighten_the_wrapping_and_the_round_caps_it()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("mummified_overseer_of_the_linen_house", "natron_decree", energy: 3,
                ("panic", 3), ("fatigue", 2)),
            health: 900);

        play.CombatDriver!.EndTurn();  // Embalmed 2, and the player's turn ends holding what would fade
        play.CombatDriver.EndTurn();

        var wrapping = FightProbe.StacksOf(
            Body(play, "mummified_overseer_of_the_linen_house"), ActFour.WrappingId);
        Assert.InRange(wrapping, 1, 2);  // never more than two a round
        play.Dispose();
    }

    // …and at four, the second wrapping: the overseer braces, and two afflictions already on the player go
    // one deeper. It creates nothing to fill an empty slot.
    [Fact]
    public void The_second_wrapping_deepens_what_is_already_there_and_invents_nothing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("mummified_overseer_of_the_linen_house", "second_wrapping",
                energy: 3),
            health: 900);

        var before = FightProbe.StacksOf(Hero(play), ActFour.EntombedId);
        play.CombatDriver!.EndTurn();

        // A clean player has nothing to wrap tighter — the block lands and nothing else does.
        Assert.Equal(24, BlockOf(Body(play, "mummified_overseer_of_the_linen_house")));
        Assert.Equal(before, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Treasury of the Two Pans ──────────────────────────────────────────────────────────────────────

    // Two one-cost cards: two played, two Energy spent. The books agree, so the treasury bleeds and writes
    // a credit.
    [Fact]
    public void A_balanced_turn_bleeds_the_treasury_and_earns_a_credit()
    {
        var (play, session, treasury) = FightProbe.Start(
            FightProbe.Solo("treasury_of_the_two_pans", "receive_the_offering"),
            deck: [.. Enumerable.Repeat(OneCost, 10)], health: 900);

        var before = Body(play, "treasury_of_the_two_pans").Health.Current;
        Play(play, session, OneCost, treasury);
        Play(play, session, OneCost, treasury);
        var struck = before - Body(play, "treasury_of_the_two_pans").Health.Current;

        play.CombatDriver!.EndTurn();

        Assert.Equal(struck + 10, before - Body(play, "treasury_of_the_two_pans").Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.TreasuryCreditId));
        play.Dispose();
    }

    // One two-cost card: one played, two spent. That is waste, and waste is a burden.
    [Fact]
    public void An_overvalued_turn_is_a_burden()
    {
        var (play, session, treasury) = FightProbe.Start(
            FightProbe.Solo("treasury_of_the_two_pans", "receive_the_offering"),
            deck: [.. Enumerable.Repeat(TwoCost, 10)], health: 900);

        Play(play, session, TwoCost, treasury);
        play.CombatDriver!.EndTurn();

        // 1 from the reckoning, 1 from the offering it took on its own turn.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.TreasuryCreditId));
        play.Dispose();
    }

    // …and a credit is a line of credit against the treasury: once a turn it buys away a burden of yours.
    [Fact]
    public void A_credit_is_offered_and_settles_a_burden()
    {
        var (play, session, treasury) = FightProbe.Start(
            FightProbe.Solo("treasury_of_the_two_pans", "receive_the_offering"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 900);

        Assert.False(Holds(play, ActFour.SettleTheBurdenCardId), "a credit was offered before one was earned");

        // Two one-cost cards: two played, two spent. The books agree.
        Play(play, session, OneCost, treasury);
        Play(play, session, OneCost, treasury);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.TreasuryCreditId));

        Assert.True(Holds(play, ActFour.SettleTheBurdenCardId), "the credit was not offered");
        var burden = FightProbe.StacksOf(Hero(play), ActFour.BurdenedId);
        Play(play, session, ActFour.SettleTheBurdenCardId, null);

        Assert.Equal(burden - 1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.TreasuryCreditId));
        play.Dispose();
    }
}
