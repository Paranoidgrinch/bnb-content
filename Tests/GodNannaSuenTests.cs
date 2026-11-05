using BnbContent.Converter.Gods;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// G4, Nanna-Suen (ACT_V_REWORK_PLAN.md): a fixed ring of four phases, one a round from the New Moon, each one move
// and one rule; the phase now and the next on him; a Radiance for every orbit.
public class GodNannaSuenTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static CombatantState Him(RunPlayback play) =>
        State(play).Combatants.Single(c => c.DefinitionId.value == NannaSuen.EnemyId);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private const string Deed = "paper_cut";
    private const string Rite = "ash_register";

    private static (RunPlayback Play, InteractiveRunSession Session) Ring(string card = Rite)
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.SoloCycle(NannaSuen.EnemyId, [.. NannaSuen.Ring.Select(p => p.Move)]),
            [.. Enumerable.Repeat(card, 15)], health: 3000);
        return (play, session);
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, bool target = false)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target ? Him(play).Id : null);
        Assert.True(session.Error is null, session.Error);
    }

    private static string? Now(RunPlayback play) =>
        NannaSuen.Ring.SingleOrDefault(p => FightProbe.StacksOf(Him(play), NannaSuen.PhaseId(p.Key)) > 0)?.Key;

    private static string? Next(RunPlayback play) =>
        NannaSuen.Ring.SingleOrDefault(p => FightProbe.StacksOf(Him(play), NannaSuen.NextId(p.Key)) > 0)?.Key;

    [Fact]
    public void The_ring_turns_one_phase_a_round_from_the_new_moon_and_the_next_is_announced()
    {
        var (play, _) = Ring();
        string[] order = ["new", "waxing", "full", "waning"];
        for (var round = 0; round < 9; round++)
        {
            Assert.Equal(order[round % 4], Now(play));
            Assert.Equal(order[(round + 1) % 4], Next(play));
            Assert.StartsWith(NannaSuen.Ring[round % 4].Name, play.CombatDriver!.Current!.UpcomingIntentFor(Him(play).Id)!.Label);
            play.CombatDriver.EndTurn();
        }
        play.Dispose();
    }

    [Fact]
    public void Under_the_new_moon_he_takes_half_and_counts_the_unseen()
    {
        var (play, session) = Ring(Deed);
        Play(play, session, Deed, target: true);
        Assert.Equal(NannaSuen.MaxHealth - 3, Him(play).Health.Current); // 6, halved
        var before = State(play).GetCardZones(Hero(play).Id).AllCards.Count(c => c.DefinitionId.value == NannaSuen.MoonlessCardId);
        play.CombatDriver!.EndTurn();
        var moonless = State(play).GetCardZones(Hero(play).Id).AllCards.Count(c => c.DefinitionId.value == NannaSuen.MoonlessCardId);
        Assert.Equal(before + NannaSuen.Moonless, moonless);
        Assert.Equal(NannaSuen.NewMoonBlock, Block(Him(play)));
        play.Dispose();
    }

    [Fact]
    public void Moonless_cannot_be_played_and_leaves_at_the_end_of_the_turn()
    {
        var (play, _) = Ring();
        play.CombatDriver!.EndTurn();
        for (var turn = 0; turn < 4 && !play.CombatDriver.Current!.Hand.Any(c => c.DefinitionId.value == NannaSuen.MoonlessCardId); turn++)
            play.CombatDriver.EndTurn();
        var moonless = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == NannaSuen.MoonlessCardId);
        Assert.False(play.CombatDriver.Current.CanPlay(moonless.Id));
        play.CombatDriver.EndTurn();
        Assert.Equal(CardZone.ExhaustPile, State(play).GetCardZones(Hero(play).Id).GetCard(moonless.Id).Zone);
        play.Dispose();
    }

    [Fact]
    public void Waxing_raises_the_horns_for_two_fourteens_and_three_strength_for_good()
    {
        var (play, _) = Ring();
        play.CombatDriver!.EndTurn();
        var hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(NannaSuen.HornHits * NannaSuen.HornDamage, hp - Hero(play).Health.Current);
        Assert.Equal(NannaSuen.HornStrength, FightProbe.StacksOf(Him(play), "strength"));
        play.Dispose();
    }

    [Fact]
    public void Under_the_waning_moon_he_takes_double_and_an_orbit_ends_in_radiance()
    {
        var (play, session) = Ring(Deed);
        for (var turn = 0; turn < 3; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal("waning", Now(play));
        var hp = Him(play).Health.Current;
        Play(play, session, Deed, target: true);
        Assert.Equal(hp - 12, Him(play).Health.Current); // 6, doubled
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Him(play), NannaSuen.RadianceId));
        play.Dispose();
    }

    [Fact]
    public void Radiance_makes_everything_he_does_greater()
    {
        var (play, _) = Ring();
        for (var turn = 0; turn < 5; turn++)
            play.CombatDriver!.EndTurn();
        // round 6 is the Waxing of the second orbit: 14 + 3 Strength, and 15 % more
        Assert.Equal("waxing", Now(play));
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        var hit = (NannaSuen.HornDamage + NannaSuen.HornStrength) * 115 / 100;
        Assert.Equal(NannaSuen.HornHits * hit, hp - Hero(play).Health.Current);
        play.Dispose();
    }

    // The first card under the Full Moon is Counted; at the next Full Moon a free copy of it is in hand, and it
    // leaves at the end of that turn.
    [Fact]
    public void The_first_card_under_the_full_moon_returns_free_at_the_next_full_moon()
    {
        var (play, session) = Ring(Deed);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal("full", Now(play));
        Play(play, session, Deed, target: true);
        var counted = State(play).GetCardZones(Hero(play).Id).AllCards.Where(c => c.HasMark(new TagId(NannaSuen.CountedMark))).ToList();
        Assert.Single(counted);
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver.EndTurn();
        Assert.Equal("full", Now(play));
        var echo = play.CombatDriver.Current!.Hand.Single(c => c.HasMark(new TagId(NannaSuen.EchoMark)));
        Assert.Equal(Deed, echo.DefinitionId.value);
        var energy = play.CombatDriver.Current.HeroEnergy;
        var hp = Him(play).Health.Current;
        play.CombatDriver.PlayCard(echo.Id, Him(play).Id);
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(energy, play.CombatDriver.Current!.HeroEnergy);
        Assert.True(Him(play).Health.Current < hp);
        play.Dispose();
    }

    [Fact]
    public void An_unplayed_echo_leaves_at_the_end_of_its_night()
    {
        var (play, session) = Ring(Deed);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Play(play, session, Deed, target: true);
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver.EndTurn();
        var echo = play.CombatDriver.Current!.Hand.Single(c => c.HasMark(new TagId(NannaSuen.EchoMark)));
        play.CombatDriver.EndTurn();
        Assert.Equal(CardZone.ExhaustPile, State(play).GetCardZones(Hero(play).Id).GetCard(echo.Id).Zone);
        play.Dispose();
    }
}
