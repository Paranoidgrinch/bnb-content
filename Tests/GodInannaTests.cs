using BnbContent.Converter.Gods;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// G2, Inanna (ACT_V_REWORK_PLAN.md): the seven regalia, one left at each gate (every 130 HP she loses), chosen by
// the player at a prompt that names them; several gates in one blow ask once each; then the Queen of Battle.
public class GodInannaTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static CombatantState Her(RunPlayback play) =>
        State(play).Combatants.Single(c => c.DefinitionId.value == Inanna.EnemyId);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Worn(RunPlayback play) =>
        Inanna.Regalia.Count(r => FightProbe.StacksOf(Her(play), Inanna.RegaliumId(r.Key)) > 0);

    private const string Deed = "paper_cut";              // 1 Energy, 6 damage
    private const string Rite = "ash_register";           // 1 Energy, no damage

    private static (RunPlayback Play, InteractiveRunSession Session) Start(string intent, int strength = 0, string card = Deed, int health = 2000)
    {
        var probe = strength > 0
            ? FightProbe.SoloAgainstHero(Inanna.EnemyId, intent, 3, ("strength", strength))
            : FightProbe.Solo(Inanna.EnemyId, intent, 3);
        var (play, session, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(card, 12)], health: health);
        return (play, session);
    }

    private static void Strike(RunPlayback play, InteractiveRunSession session)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Deed);
        play.CombatDriver.PlayCard(card.Id, Her(play).Id);
        Assert.True(session.Error is null, session.Error);
    }

    [Fact]
    public void She_comes_down_wearing_all_seven()
    {
        var (play, _) = Start("lioness_of_battle");
        Assert.Equal(7, Worn(play));
        Assert.Equal(Inanna.Thorns, FightProbe.StacksOf(Her(play), Inanna.RegaliumId("breastplate")));
        Assert.Equal(Inanna.MaxHealth, Her(play).Health.Max);
        play.Dispose();
    }

    // Every blow of yours here is cut twice: the Golden Ring's Weak (25 %) and her Robe (25 %).
    // 306 a blow → 229 → 171 lost: one gate.
    [Fact]
    public void At_a_gate_you_choose_which_regalium_she_leaves_and_the_prompt_names_each()
    {
        var (play, session) = Start("lioness_of_battle", strength: 300);
        Strike(play, session);
        var options = play.CombatDriver!.PendingOptionChoice;
        Assert.NotNull(options);
        Assert.Equal(Inanna.GatePurpose, play.CombatDriver.PendingOptionChoicePurpose);
        Assert.Equal(Inanna.Regalia.Select(Inanna.OptionLabel), options);
        Assert.All(Inanna.Regalia, r => Assert.Contains(options!, o => o.StartsWith(r.Name, StringComparison.Ordinal)));

        // She leaves the Robe (index 4) at the first gate.
        play.CombatDriver.SupplyOptionChoice([4]);
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        Assert.Equal(0, FightProbe.StacksOf(Her(play), Inanna.RegaliumId("robe")));
        Assert.Equal(6, Worn(play));
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Inanna.GatesPassedId));
        play.Dispose();
    }

    // 556 → 417 → 312 lost in one blow: two gates, two questions, and the second asks only about what is left.
    [Fact]
    public void Several_gates_passed_in_one_hit_ask_once_each()
    {
        var (play, session) = Start("lioness_of_battle", strength: 550);
        Strike(play, session);
        Assert.Equal(7, play.CombatDriver!.PendingOptionChoice!.Count);
        play.CombatDriver.SupplyOptionChoice([0]); // the Crown
        var second = play.CombatDriver.PendingOptionChoice;
        Assert.NotNull(second);
        Assert.Equal(6, second!.Count);
        Assert.DoesNotContain(second, o => o.StartsWith(Inanna.Regalia[0].Name, StringComparison.Ordinal));
        play.CombatDriver.SupplyOptionChoice([0]); // now the Rod
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        Assert.Equal(5, Worn(play));
        Assert.Equal(0, FightProbe.StacksOf(Her(play), Inanna.RegaliumId("crown")));
        Assert.Equal(0, FightProbe.StacksOf(Her(play), Inanna.RegaliumId("rod")));
        Assert.Equal(2, FightProbe.StacksOf(Her(play), Inanna.GatesPassedId));
        play.Dispose();
    }

    // A gate passed is passed: her Necklace heals her back up, and the next blow asks only for the gate it crosses.
    [Fact]
    public void A_gate_is_asked_once_even_when_she_heals_back_above_it()
    {
        var (play, session) = Start("splendor_of_eanna", strength: 300);
        Strike(play, session);
        play.CombatDriver!.SupplyOptionChoice([0]);
        play.CombatDriver.EndTurn();
        // 171 lost, healed 15 at her dawn: 156; this blow makes it 327, the second gate and only that one
        Strike(play, session);
        Assert.Equal(6, play.CombatDriver.PendingOptionChoice!.Count);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        Assert.Equal(2, FightProbe.StacksOf(Her(play), Inanna.GatesPassedId));
        play.Dispose();
    }

    [Fact]
    public void After_the_seventh_gate_she_is_the_queen_of_battle()
    {
        // 1706 → 1279 → 959 lost: seven gates in one blow, seven questions.
        var (play, session) = Start("lioness_of_battle", strength: 1700, health: 5000);
        Strike(play, session);
        for (var gate = 7; gate >= 1; gate--)
        {
            Assert.Equal(gate, play.CombatDriver!.PendingOptionChoice!.Count);
            play.CombatDriver.SupplyOptionChoice([0]);
        }
        Assert.Null(play.CombatDriver!.PendingOptionChoice);
        Assert.Equal(0, Worn(play));
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Inanna.QueenOfBattleId));

        // Every move twice (32 and 32), and 3 Strength at the end of her turn.
        var hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(2 * Inanna.LionessDamage, hp - Hero(play).Health.Current);
        Assert.Equal(Inanna.QueenStrength, FightProbe.StacksOf(Her(play), "strength"));
        play.Dispose();
    }

    // ── the seven, one by one ─────────────────────────────────────────────────────────────────────────────

    // The Crown (+5) and the Twin Beads (the first attack twice): Lioness lands 37 twice.
    [Fact]
    public void The_crown_and_the_twin_beads_make_her_lioness_land_37_twice()
    {
        var (play, _) = Start("lioness_of_battle", card: Rite);
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(2 * (Inanna.LionessDamage + Inanna.CrownStrength), hp - Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_twin_beads_double_only_the_first_arrow()
    {
        var (play, _) = Start("arrows_of_the_morning_star", card: Rite);
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal((Inanna.ArrowHits + 1) * (Inanna.ArrowDamage + Inanna.CrownStrength), hp - Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_rod_guards_her_at_the_end_of_her_turn_and_the_necklace_heals_her_at_its_start()
    {
        var (play, session) = Start("splendor_of_eanna", strength: 40);
        Strike(play, session);
        var hp = Her(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(Math.Min(Inanna.MaxHealth, hp + Inanna.NecklaceHeal), Her(play).Health.Current);
        Assert.Equal(Inanna.RodBlock + Inanna.SplendorBlock, Block(Her(play)));
        play.Dispose();
    }

    [Fact]
    public void The_robe_takes_a_quarter_off_and_the_breastplate_strikes_back_for_four()
    {
        var (play, session) = Start("lioness_of_battle", strength: 34);
        var hero = Hero(play).Health.Current;
        Strike(play, session);
        Assert.Equal(Inanna.MaxHealth - 22, Her(play).Health.Current); // 40, Weak 30, Robe 22
        Assert.Equal(hero - Inanna.Thorns, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_golden_ring_weakens_you_every_turn()
    {
        var (play, _) = Start("splendor_of_eanna", card: Rite);
        Assert.Equal(Inanna.RingWeak, FightProbe.StacksOf(Hero(play), GodShapes.WeakId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(Inanna.RingWeak, FightProbe.StacksOf(Hero(play), GodShapes.WeakId));
        play.Dispose();
    }

    [Fact]
    public void Infatuation_takes_a_quarter_off_your_attacks_for_two_turns_and_strengthens_her()
    {
        var (play, _) = Start("infatuation", card: Rite);
        play.CombatDriver!.EndTurn();
        Assert.Equal(Inanna.InfatuationTurns, FightProbe.StacksOf(Hero(play), Inanna.InfatuationId));
        Assert.Equal(Inanna.InfatuationStrength, FightProbe.StacksOf(Her(play), "strength"));
        play.CombatDriver.EndTurn();
        Assert.Equal(Inanna.InfatuationTurns + 1, FightProbe.StacksOf(Hero(play), Inanna.InfatuationId)); // 2 − 1 + 2
        play.Dispose();
    }
}
