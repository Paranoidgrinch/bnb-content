using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Hedge Witch's reward cards (hedge_witch_master.md §11–§13; plan W4): the shape of the pools, and every card
// whose words carry a RULE beyond a plain number — each test is red without that rule. Numbers are the E7 drafts.
public class WitchCardTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero
    private const string Biter = "embossed_seal";
    private const string BiteIntent = "heavy_impression";

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Fight(
        string[] deck, string enemy = Quiet, string intent = QuietIntent, int energy = 9) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, energy: energy), deck, character: WitchCharacter.Id);

    private static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;
    private static CombatantState Hero(RunPlayback play) => Now(play).State.GetCombatant(Now(play).HeroId);
    private static CombatantState Body(RunPlayback play, CombatantId id) => Now(play).State.GetCombatant(id);
    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;
    private static int Standing(RunPlayback play, CombatantId id) =>
        Body(play, id).Health.Current + Block(Body(play, id));

    private static void Play(RunPlayback play, InteractiveRunSession session, string card, CombatantId target)
    {
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == card).Id, target);
        Assert.Null(session.Error);
    }

    private static void EndTurn(RunPlayback play, InteractiveRunSession session)
    {
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
    }

    private static string[] Of(string card, int copies = 10) => Enumerable.Repeat(card, copies).ToArray();

    // ── the pools ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Twenty_commons_four_a_family_each_with_its_upgrade_and_one_family_on_the_shipped_card()
    {
        var commons = WitchCards.Commons();
        Assert.Equal(40, commons.Count);
        var bases = commons.Where(c => !c.Id.EndsWith('+')).ToList();
        Assert.Equal(20, bases.Count);
        Assert.All(bases, c => Assert.Equal("common", c.Rarity));
        Assert.All(bases, c => Assert.Contains(commons, u => u.Id == c.Id + "+"));
        Assert.All(WitchActions.Families, f => Assert.Equal(4, bases.Count(c => c.Tags!.Contains(f))));

        var shipped = FightProbe.Game.Cards.ToDictionary(c => c.Id);
        foreach (var card in commons)
            Assert.Single(shipped[card.Id].Tags, t => WitchActions.Families.Contains(t.value));
    }

    // ── Fang ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Bramble_switch_bites_deeper_into_a_hexed_target()
    {
        var (play, session, enemy) = Fight(["bramble_switch", "bramble_switch", "crooked_finger", "pot_lid", "pot_lid"]);
        var before = Standing(play, enemy);
        Play(play, session, "bramble_switch", enemy);
        Assert.Equal(before - 7, Standing(play, enemy));
        Play(play, session, "crooked_finger", enemy);
        before = Standing(play, enemy);
        Play(play, session, "bramble_switch", enemy);
        Assert.Equal(before - 10, Standing(play, enemy));
        play.Dispose();
    }

    [Fact]
    public void Crows_peck_takes_more_from_a_target_at_half_or_less()
    {
        var (play, session, enemy) = Fight(Of("crows_peck"), energy: 0);
        var max = Body(play, enemy).Health.Max;
        var before = Standing(play, enemy);
        Play(play, session, "crows_peck", enemy);
        Assert.Equal(before - 3, Standing(play, enemy));

        // Peck it down to half, a turn at a time, then one more peck is the big one.
        while (Body(play, enemy).Health.Current * 2 > max)
        {
            if (!Now(play).Hand.Any(c => c.DefinitionId.value == "crows_peck"))
                EndTurn(play, session);
            Play(play, session, "crows_peck", enemy);
        }
        if (!Now(play).Hand.Any(c => c.DefinitionId.value == "crows_peck"))
            EndTurn(play, session);
        before = Standing(play, enemy);
        Play(play, session, "crows_peck", enemy);
        Assert.Equal(before - 6, Standing(play, enemy));
        play.Dispose();
    }

    // ── Hex ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Third_knock_knocks_again_only_on_the_night_before_the_burst()
    {
        var (play, session, enemy) = Fight(Of("third_knock"));
        Play(play, session, "third_knock", enemy);
        Assert.Equal(3, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        EndTurn(play, session);   // its step: 1
        Play(play, session, "third_knock", enemy);
        Assert.Equal(6, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        EndTurn(play, session);   // its step: 2 — the next end of its turn is the third night
        Play(play, session, "third_knock", enemy);
        Assert.Equal(12, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    // ── Husk ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Snail_shell_leaves_ward_wax_only_when_nothing_got_through()
    {
        var (play, session, enemy) = Fight(Of("snail_shell"));
        Play(play, session, "snail_shell", enemy);
        EndTurn(play, session);
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), Converter.Cards.Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.SnailShell));
        play.Dispose();

        // The shell's mark without its Block, so the seal's blow lands.
        (play, session, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero(Biter, BiteIntent, 9, (WitchRules.SnailShell, 3)), Of("pot_lid"),
            character: WitchCharacter.Id);
        var health = Hero(play).Health.Current;
        EndTurn(play, session);
        Assert.True(Hero(play).Health.Current < health, "the seal's blow got through the shell");
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Converter.Cards.Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.SnailShell));
        play.Dispose();
    }

    [Fact]
    public void Thorn_hedge_pricks_the_first_attacker_and_is_spent()
    {
        var (play, session, enemy) = Fight(Of("thorn_hedge"), Biter, BiteIntent);
        Play(play, session, "thorn_hedge", enemy);
        var health = Body(play, enemy).Health.Current;
        EndTurn(play, session);
        Assert.Equal(health - 4, Body(play, enemy).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.ThornHedge));
        play.Dispose();

        // Nobody attacks: nobody is pricked, and the hedge withers with her next turn.
        (play, session, enemy) = Fight(Of("thorn_hedge"));
        Play(play, session, "thorn_hedge", enemy);
        health = Body(play, enemy).Health.Current;
        EndTurn(play, session);
        Assert.Equal(health, Body(play, enemy).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.ThornHedge));
        play.Dispose();
    }

    [Fact]
    public void Shed_skin_sheds_one_stack_of_an_affliction()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.SoloAgainstHero(Quiet, QuietIntent, 9, ("panic", 2)), Of("shed_skin"), character: WitchCharacter.Id);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "panic"));
        Play(play, session, "shed_skin", enemy);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(5, Block(Hero(play)));
        play.Dispose();
    }

    // ── Hearth ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Mugwort_poultice_is_spent_when_used()
    {
        var (play, session, enemy) = Fight(Of("mugwort_poultice"));
        Play(play, session, "mugwort_poultice", enemy);
        var zones = Now(play).State.GetCardZones(Now(play).HeroId);
        Assert.Single(zones.ExhaustPile);
        Assert.Empty(zones.DiscardPile);
        play.Dispose();
    }

    // ── Fortune ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Knock_on_wood_braces_harder_once_an_enemy_is_ill_starred()
    {
        var (play, session, enemy) = Fight(["knock_on_wood", "knock_on_wood", "crooked_horseshoe", "pot_lid", "pot_lid"]);
        Play(play, session, "knock_on_wood", enemy);
        Assert.Equal(3, Block(Hero(play)));
        Play(play, session, "crooked_horseshoe", enemy);
        Assert.Equal(6, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Play(play, session, "knock_on_wood", enemy);
        Assert.Equal(9, Block(Hero(play)));
        play.Dispose();
    }
}
