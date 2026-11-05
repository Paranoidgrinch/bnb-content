using BnbContent.Converter.Gods;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// G6, Enlil (ACT_V_REWORK_PLAN.md): a ring of eight decrees, one in force a round on both sides and the next
// announced; the Noise of Mankind and Namtar's Plague; the Flood and Atrahasis' Boat.
public class GodEnlilTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static CombatantState Him(RunPlayback play) =>
        State(play).Combatants.Single(c => c.DefinitionId.value == Enlil.EnemyId);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private const string Deed = "paper_cut";              // 1, 6 damage
    private const string Working = "cower_behind_a_desk"; // 1, 5 Block
    private const string Charm = "sanctioned_charm";      // 0, 1 Energy and a card
    private const string Poultice = "comfrey_poultice";   // 1, heal 3 (7 at half or less)

    private static (RunPlayback Play, InteractiveRunSession Session) Start(
        string intent, string[] deck, int energy = 3, int strength = 0, int health = 3000)
    {
        var probe = strength > 0
            ? FightProbe.SoloAgainstHero(Enlil.EnemyId, intent, energy, ("strength", strength))
            : FightProbe.Solo(Enlil.EnemyId, intent, energy);
        var (play, session, _) = FightProbe.Start(probe, deck, health: health);
        return (play, session);
    }

    private static string[] Many(string card, int count = 20) => [.. Enumerable.Repeat(card, count)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, bool target = false)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target ? Him(play).Id : null);
        Assert.True(session.Error is null, session.Error);
    }

    private static string? InForce(CombatantState who) =>
        Enlil.Ring.SingleOrDefault(d => FightProbe.StacksOf(who, Enlil.DecreeId(d.Key)) > 0)?.Key;

    private static string? Announced(RunPlayback play) =>
        Enlil.Ring.SingleOrDefault(d => FightProbe.StacksOf(Him(play), Enlil.NextId(d.Key)) > 0)?.Key;

    private static void ToDecree(RunPlayback play, string key)
    {
        for (var round = 0; round < 8 && InForce(Him(play)) != key; round++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(key, InForce(Him(play)));
    }

    // One decree a round, the ring in its order, the next announced a round ahead, worn by both sides.
    [Fact]
    public void One_decree_a_round_turning_in_its_ring_and_announced_a_round_ahead()
    {
        var (play, _) = Start("raise_the_e_kur", Many(Working));
        for (var round = 0; round < 17; round++)
        {
            var now = Enlil.Ring[round % 8];
            Assert.Equal(now.Key, InForce(Him(play)));
            Assert.Equal(now.Key, InForce(Hero(play)));
            Assert.Equal(Enlil.Ring[(round + 1) % 8].Key, Announced(play));
            Assert.Single(Enlil.Ring, d => FightProbe.StacksOf(Hero(play), Enlil.DecreeId(d.Key)) > 0);
            play.CombatDriver!.EndTurn();
        }
        play.Dispose();
    }

    [Fact]
    public void No_wall_rises_above_twenty_on_either_side()
    {
        var (play, session) = Start("raise_the_e_kur", Many(Working), energy: 6);
        for (var i = 0; i < 5; i++)
            Play(play, session, Working);
        Assert.Equal(Enlil.WallCap, Block(Hero(play)));
        play.CombatDriver!.EndTurn();
        // his E-kur was raised under the first decree: 20, not 40 (the ring turned after it)
        Assert.Equal(Enlil.WallCap, Block(Him(play)));
        play.Dispose();
    }

    [Fact]
    public void The_fourth_work_is_the_last()
    {
        var (play, session) = Start("raise_the_e_kur", Many(Charm), energy: 9);
        ToDecree(play, "fourth");
        for (var i = 0; i < 4; i++)
            Play(play, session, Charm);
        var fifth = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Charm);
        Assert.False(play.CombatDriver.Current.CanPlay(fifth.Id));
        play.Dispose();
    }

    [Fact]
    public void No_word_is_without_price()
    {
        var (play, session) = Start("raise_the_e_kur", Many(Charm), energy: 3);
        ToDecree(play, "price");
        var energy = play.CombatDriver!.Current!.HeroEnergy;
        Play(play, session, Charm); // 0 printed, 1 under the decree; it gives 1 back
        Assert.Equal(energy, play.CombatDriver.Current!.HeroEnergy);
        play.Dispose();
    }

    [Fact]
    public void The_blow_returns_a_quarter_to_the_striker_both_ways()
    {
        var (play, session) = Start("the_command_goes_forth", Many(Deed), strength: 34, health: 3000);
        ToDecree(play, "blow");
        var hp = Hero(play).Health.Current;
        var his = Him(play).Health.Current;
        Play(play, session, Deed, target: true); // 40: you lose 10
        Assert.Equal(his - 40, Him(play).Health.Current);
        Assert.Equal(hp - 10, Hero(play).Health.Current);
        // his Command: 20 at you, 5 back to him
        his = Him(play).Health.Current;
        hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(hp - Enlil.CommandDamage, Hero(play).Health.Current);
        Assert.Equal(his - Enlil.CommandDamage / 4, Him(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Half_is_the_measure_of_every_wound()
    {
        var (play, session) = Start("the_command_goes_forth", Many(Deed));
        ToDecree(play, "half");
        var his = Him(play).Health.Current;
        Play(play, session, Deed, target: true);
        Assert.Equal(his - 3, Him(play).Health.Current);
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(hp - Enlil.CommandDamage / 2, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void No_wound_closes()
    {
        var (play, session) = Start("the_command_goes_forth", [.. Many(Poultice, 10), .. Many(Deed, 10)]);
        ToDecree(play, "wound");
        for (var turn = 0; turn < 4 && !play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Poultice); turn++)
            play.CombatDriver.EndTurn();
        Assert.Equal("wound", InForce(Him(play)));
        var hp = Hero(play).Health.Current;
        Assert.True(hp < Hero(play).Health.Max);
        Play(play, session, Poultice);
        Assert.Equal(hp, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_first_work_is_done_twice_and_his_first_hit_lands_twice()
    {
        var (play, session) = Start("the_command_goes_forth", Many(Deed));
        ToDecree(play, "first");
        var his = Him(play).Health.Current;
        Play(play, session, Deed, target: true);
        Assert.Equal(his - 12, Him(play).Health.Current);
        Play(play, session, Deed, target: true);
        Assert.Equal(his - 18, Him(play).Health.Current); // the second card once
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(hp - 2 * Enlil.CommandDamage, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_hand_holds_four()
    {
        var (play, _) = Start("raise_the_e_kur", Many(Working));
        ToDecree(play, "hand");
        Assert.Equal(Enlil.HandOfFour, play.CombatDriver!.Current!.Hand.Count);
        play.Dispose();
    }

    // ── the later phases ──────────────────────────────────────────────────────────────────────────────────

    private static void Wound(RunPlayback play, InteractiveRunSession session, int below)
    {
        for (var i = 0; i < 60 && Him(play).Health.Current * 100 > Him(play).Health.Max * below; i++)
        {
            var hand = play.CombatDriver!.Current!.Hand;
            if (!hand.Any(c => c.DefinitionId.value == Deed && play.CombatDriver.Current.CanPlay(c.Id)) || play.CombatDriver.Current.HeroEnergy == 0)
                play.CombatDriver.EndTurn();
            else
                Play(play, session, Deed, target: true);
        }
    }

    [Fact]
    public void In_the_noise_of_mankind_twelve_cards_bring_namtars_plague()
    {
        var (play, session) = Start("raise_the_e_kur", [.. Many(Deed, 10), .. Many(Charm, 30)], energy: 9, strength: 150);
        Wound(play, session, 60);
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Enlil.NoiseOfMankindId));
        // play Charms (and Deeds) until the Noise stands at 12, round by round
        for (var turn = 0; turn < 10 && FightProbe.StacksOf(Him(play), Enlil.NoiseId) < Enlil.NoiseLimit; turn++)
        {
            foreach (var card in play.CombatDriver!.Current!.Hand.Where(c => c.DefinitionId.value == Charm).ToList())
                if (play.CombatDriver.Current.CanPlay(card.Id) && FightProbe.StacksOf(Him(play), Enlil.NoiseId) < Enlil.NoiseLimit)
                {
                    play.CombatDriver.PlayCard(card.Id, null);
                    Assert.True(session.Error is null, session.Error);
                }
            if (FightProbe.StacksOf(Him(play), Enlil.NoiseId) < Enlil.NoiseLimit)
                play.CombatDriver.EndTurn();
        }
        Assert.True(FightProbe.StacksOf(Him(play), Enlil.NoiseId) >= Enlil.NoiseLimit);
        var hp = Hero(play).Health.Current;
        var block = Block(Hero(play));
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Him(play), Enlil.NoiseId));
        Assert.Equal(Enlil.PlagueWeak, FightProbe.StacksOf(Hero(play), GodShapes.WeakId));
        Assert.Equal(Enlil.PlagueFrail, FightProbe.StacksOf(Hero(play), GodShapes.FrailId));
        Assert.True(hp + block - Hero(play).Health.Current >= Enlil.PlagueDamage / 2);
        play.Dispose();
    }

    [Fact]
    public void In_the_flood_the_ring_stops_the_water_rises_and_the_boat_halves_your_share()
    {
        var (play, session) = Start("raise_the_e_kur", Many(Deed, 30), energy: 9, strength: 400, health: 5000);
        Wound(play, session, 30);
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Enlil.FloodId));
        var frozen = InForce(Him(play));
        Assert.Null(Announced(play));
        Assert.Single(play.CombatDriver!.Current!.Hand, c => c.DefinitionId.value == Enlil.BoatCardId);

        var hp = Hero(play).Health.Current;
        var his = Him(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Enlil.WaterId));
        Assert.Equal(frozen, InForce(Him(play)));
        Assert.Equal(frozen, InForce(Hero(play)));
        var first = Enlil.FloodPerWater * (frozen == "half" ? 1 : 2) / 2;
        Assert.Equal(hp - first, Hero(play).Health.Current);

        // the Boat: this round the flood deals you half (2 x 6 = 12, so 6)
        Play(play, session, Enlil.BoatCardId);
        Assert.Single(play.CombatDriver.Current!.Hand, c => c.DefinitionId.value == Enlil.BoatCardId);
        hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Him(play), Enlil.WaterId));
        var halved = 2 * Enlil.FloodPerWater / 2 * (frozen == "half" ? 1 : 2) / 2;
        Assert.Equal(hp - halved, Hero(play).Health.Current);
        Assert.True(Him(play).Health.Current < his);
        play.Dispose();
    }
}
