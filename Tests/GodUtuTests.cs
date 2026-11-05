using BnbContent.Converter.Gods;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// G5, Utu-Shamash (ACT_V_REWORK_PLAN.md): three oaths a turn, the Favor paid at once, a Witness for an oath not kept;
// the Saw returns a share of what you dealt him the turn just before; No Shadow; the Judge of the Dead.
public class GodUtuTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static CombatantState Him(RunPlayback play) =>
        State(play).Combatants.Single(c => c.DefinitionId.value == Utu.EnemyId);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private const string Deed = "paper_cut"; // 1 Energy, 6 damage

    private static (RunPlayback Play, InteractiveRunSession Session) Start(string intent, int strength = 0, int energy = 3)
    {
        var probe = strength > 0
            ? FightProbe.SoloAgainstHero(Utu.EnemyId, intent, energy, ("strength", strength))
            : FightProbe.Solo(Utu.EnemyId, intent, energy);
        var (play, session, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(Deed, 20)], health: 5000);
        return (play, session);
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, bool target = false)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target ? Him(play).Id : null);
        Assert.True(session.Error is null, session.Error);
    }

    // AddPercentPerStack: per stack, that percent of the amount, rounded down but at least 1.
    private static int Raised(int amount, int percent, int stacks) =>
        stacks == 0 ? amount : amount + stacks * Math.Max(1, amount * percent / 100);

    private static int Oaths(RunPlayback play) =>
        play.CombatDriver!.Current!.Hand.Count(c => Utu.Oaths.Any(o => c.DefinitionId.value == Utu.OathCardId(o.Sworn)));

    [Fact]
    public void Each_turn_three_oaths_are_in_your_hand()
    {
        var (play, _) = Start("rise_between_the_mountains");
        Assert.Equal(3, Oaths(play));
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Oaths(play));
        play.Dispose();
    }

    [Fact]
    public void Swearing_pays_the_favor_at_once_and_the_other_oaths_are_gone()
    {
        var (play, session) = Start("rise_between_the_mountains");
        Play(play, session, Utu.OathCardId(40));
        Assert.Equal(20, Block(Hero(play)));
        Assert.Equal(40, FightProbe.StacksOf(Him(play), Utu.SwornId));
        Assert.Equal(0, Oaths(play));
        play.Dispose();
    }

    [Fact]
    public void An_oath_not_kept_is_witnessed_and_one_kept_is_not()
    {
        var (play, session) = Start("rise_between_the_mountains");
        Play(play, session, Utu.OathCardId(20));
        Play(play, session, Deed, target: true);
        Play(play, session, Deed, target: true);
        Assert.Equal(12, FightProbe.StacksOf(Him(play), Utu.DealtId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Utu.WitnessId));
        // the slate is clean next morning
        Assert.Equal(0, FightProbe.StacksOf(Him(play), Utu.SwornId));
        Assert.Equal(0, FightProbe.StacksOf(Him(play), Utu.DealtId));
        play.Dispose();

        var (kept, keptSession) = Start("rise_between_the_mountains", strength: 20);
        Play(kept, keptSession, Utu.OathCardId(20));
        Play(kept, keptSession, Deed, target: true);
        kept.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Him(kept), Utu.WitnessId));
        kept.Dispose();
    }

    [Fact]
    public void A_turn_without_an_oath_is_not_witnessed_before_no_shadow()
    {
        var (play, _) = Start("rise_between_the_mountains");
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Him(play), Utu.WitnessId));
        play.Dispose();
    }

    // Retribution reads the turn just before it: 15 + half of the 12 you dealt him.
    [Fact]
    public void The_saw_returns_half_of_what_you_dealt_him_the_turn_before()
    {
        var (play, session) = Start("the_saw_of_judgment");
        Play(play, session, Deed, target: true);
        Play(play, session, Deed, target: true);
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(Utu.SawBase + 6, hp - Hero(play).Health.Current);
        // …and a turn of nothing is answered with the bare 15
        hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(Utu.SawBase, hp - Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Every_witness_makes_everything_he_does_ten_percent_greater()
    {
        var (play, session) = Start("the_saw_of_judgment");
        Play(play, session, Utu.OathCardId(60));
        play.CombatDriver!.EndTurn(); // the oath broken: 1 Witness; the Saw hits 15 into the Favor's 30 Block
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Utu.WitnessId));
        var hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(Raised(Utu.SawBase, Utu.WitnessPercent, 1), hp - Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Rays_that_lay_bare_remove_your_block_first()
    {
        var (play, session) = Start("rays_that_lay_bare");
        Play(play, session, Utu.OathCardId(60));
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        // the 30 Block is gone before the Rays land; the oath broken is a Witness, 10 % more
        Assert.Equal(Raised(Utu.RaysDamage, Utu.WitnessPercent, 1), hp - Hero(play).Health.Current);
        play.Dispose();
    }

    private static (RunPlayback Play, InteractiveRunSession Session) Wounded(int below)
    {
        var (play, session) = Start("the_saw_of_judgment", strength: 150, energy: 3);
        while (Him(play).Health.Current * 100 > Him(play).Health.Max * below)
        {
            if (!play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Deed) || play.CombatDriver.Current.HeroEnergy == 0)
                play.CombatDriver.EndTurn();
            else
                Play(play, session, Deed, target: true);
        }
        return (play, session);
    }

    [Fact]
    public void Under_no_shadow_the_saw_returns_three_quarters_and_a_turn_without_an_oath_is_witnessed()
    {
        var (play, session) = Wounded(60);
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Utu.NoShadowId));
        var witness = FightProbe.StacksOf(Him(play), Utu.WitnessId);
        var dealt = FightProbe.StacksOf(Him(play), Utu.DealtId);
        var hp = Hero(play).Health.Current;
        var block = Block(Hero(play));
        play.CombatDriver!.EndTurn();
        // no oath sworn this turn: witnessed
        Assert.Equal(witness + 1, FightProbe.StacksOf(Him(play), Utu.WitnessId));
        var saw = Raised(Utu.SawBase + dealt * Utu.NoShadowSawPercent / 100, Utu.WitnessPercent, witness + 1);
        Assert.Equal(saw, hp + block - Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_judge_of_the_dead_asks_no_more_oaths_and_counts_each_witness_double()
    {
        var (play, session) = Wounded(30);
        Assert.Equal(1, FightProbe.StacksOf(Him(play), Utu.JudgeOfTheDeadId));
        Assert.Equal(0, Oaths(play));
        Assert.Equal(0, FightProbe.StacksOf(Him(play), Utu.WitnessId));
        var below = FightProbe.StacksOf(Him(play), Utu.DeadWitnessId);
        var dealt = FightProbe.StacksOf(Him(play), Utu.DealtId);
        var hp = Hero(play).Health.Current;
        var block = Block(Hero(play));
        play.CombatDriver!.EndTurn();
        Assert.Equal(Raised(Utu.SawBase + dealt, Utu.DeadWitnessPercent, below), hp + block - Hero(play).Health.Current);
        Assert.Equal(0, Oaths(play));
        Assert.Equal(below, FightProbe.StacksOf(Him(play), Utu.DeadWitnessId));
        play.Dispose();
    }
}
