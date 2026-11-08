using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-IV elites since the rework of 2026-11-04 (user): each its own mechanic, each count a stack.
public class ActFourElitesTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Body(RunPlayback play, CombatantId id) => State(play).GetCombatant(id);

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static IEnumerable<CombatantState> Wearing(RunPlayback play, string status) =>
        State(play).Combatants.Where(c => c.IsAlive && FightProbe.StacksOf(c, status) > 0);

    private static int Stacks(CombatantState who, string status) => FightProbe.StacksOf(who, status);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static bool Over(RunPlayback play) => play.CombatDriver!.Current is null;

    private const string Deed = "paper_cut";              // 1 Energy, Deed, 6 damage
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working, 5 Block
    private const string Permit = "permit_a38";           // 2 Energy, Working

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Solo(
        string enemy, string intent, IReadOnlyList<string> deck, int? hp = null, int energy = 9) =>
        FightProbe.Start(FightProbe.Roster(enemy, energy, (enemy, intent, hp)), deck, health: 300);

    private static string[] Many(string card, int count = 10) => [.. Enumerable.Repeat(card, count)];

    // ── the Taskmaster ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Two_haulers_drag_the_obelisk_and_at_four_it_falls_for_forty()
    {
        var (play, _, master) = Solo(ActFour.RopeMasterEnemyId, "stand_over", Many(Deed));
        Assert.Equal(2, Wearing(play, ActFour.AtTheRopeId).Count());
        Assert.All(Wearing(play, ActFour.AtTheRopeId), h => Assert.Equal(ActFour.HaulerHealth, h.Health.Max));

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play, master), ActFour.ObeliskId));
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(before - ActFour.ObeliskDamage, Hero(play).Health.Current);
        Assert.Equal(0, Stacks(Body(play, master), ActFour.ObeliskId));
        play.Dispose();
    }

    [Fact]
    public void The_haulers_fall_with_the_taskmaster()
    {
        var (play, session, master) = Solo(ActFour.RopeMasterEnemyId, "stand_over", Many(Deed), hp: 6);
        Play(play, session, Deed, master);
        Assert.True(Over(play));
        play.Dispose();
    }

    // ── Colossus of Memnon ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void At_the_third_round_dawn_turns_up_to_two_burdened_into_entombed()
    {
        var (play, _, colossus) = FightProbe.Start(
            FightProbe.RosterAgainstHero("memnon", 3, [(ActFour.BurdenedId, 3)],
                (ActFour.ColossusEnemyId, "long_shadow", null)), Many(Deed), health: 300);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play, colossus), ActFour.UntilDawnId));
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Stacks(Body(play, colossus), ActFour.DawnBreaksId));
        play.CombatDriver.EndTurn();
        Assert.Equal(ActFour.DawnTakesAtMost, Stacks(Hero(play), ActFour.EntombedId));
        Assert.Equal(1, Stacks(Hero(play), ActFour.BurdenedId));
        Assert.Equal(0, Stacks(Body(play, colossus), ActFour.DawnBreaksId));
        play.Dispose();
    }

    // ── Coils of Apep ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_coil_is_a_card_fewer_and_twenty_damage_in_a_turn_loosens_it()
    {
        var (play, session, apep) = Solo(ActFour.DecanKeeperEnemyId, "strike", Many(Deed));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.CoilId));
        Assert.Equal(4, play.CombatDriver.Current!.Hand.Count);

        for (var i = 0; i < 4; i++)
            Play(play, session, Deed, apep);
        Assert.Equal(0, Stacks(Hero(play), ActFour.CoilId));
        Assert.Equal(1, Stacks(Body(play, apep), ActFour.LoosenedId));
        play.Dispose();
    }

    // ── the Sphinx ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_right_answer_is_a_sign_and_three_solve_her()
    {
        var (play, session, sphinx) = FightProbe.Start(
            FightProbe.Roster(ActFour.SphinxEnemyId, 9, (ActFour.SphinxEnemyId, "stone_riddle", null)),
            [Deed, Working, Deed, Working, Deed], health: 300);
        var body = Body(play, sphinx);
        var wantsDeeds = Stacks(body, ActFour.SphinxWantsDeedsId) > 0;
        Assert.True(wantsDeeds || Stacks(body, ActFour.SphinxWantsWorkingsId) > 0);
        Play(play, session, wantsDeeds ? Deed : Working, wantsDeeds ? sphinx : null);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play, sphinx), ActFour.SignId));
        Assert.Equal(0, Stacks(Hero(play), ActFour.EntombedId));

        // a wrong (empty) turn is 1 Entombed
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    [Fact]
    public void A_solved_sphinx_gains_no_block()
    {
        // two signs already given: the third solves her
        var (play, session, sphinx) = FightProbe.Start(FightProbe.Solo(ActFour.SphinxEnemyId, "stone_riddle", 9, (ActFour.SignId, 2)),
            [Deed, Working, Deed, Working, Deed], health: 300);
        var wantsDeeds = Stacks(Body(play, sphinx), ActFour.SphinxWantsDeedsId) > 0;
        Play(play, session, wantsDeeds ? Deed : Working, wantsDeeds ? sphinx : null);
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, Block(Body(play, sphinx)));
        Assert.True(Stacks(Body(play, sphinx), ActFour.SolvedId) > 0);
        play.Dispose();
    }

    // ── Ammit ─────────────────────────────────────────────────────────────────────────────────────────────

    // She devours the most expensive card left in your hand as your turn ends (+2 Strength) — unless you feed her a
    // Feather of Truth, which she lays down herself as the fight opens (content review E19, 2026-11-08).
    [Fact]
    public void Ammit_devours_the_costliest_card_you_hold_unless_you_feed_her_a_feather()
    {
        var (play, _, ammit) = Solo(ActFour.AmmitEnemyId, "crouch_by_the_scale", [Permit, Deed, Deed, Working, Deed]);
        Assert.Equal(1, Stacks(Hero(play), ActFour.FeatherOfTruthId));

        // With a Feather in hand she asks: feed her, and the hand is spared.
        play.CombatDriver!.EndTurn();
        var options = Assert.IsAssignableFrom<IReadOnlyList<string>>(play.CombatDriver.PendingOptionChoice);
        Assert.Contains("Feather", options[0], StringComparison.Ordinal);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Equal(0, Stacks(Hero(play), ActFour.FeatherOfTruthId));
        Assert.Equal(0, Stacks(Body(play, ammit), ActFour.DevouredId));
        Assert.Equal(0, Stacks(Body(play, ammit), "strength"));
        // Her own Feather went first: the run's carried Feathers are untouched.
        Assert.False(Hero(play).Counters.TryGetValue(ActFour.FeathersSpent, out var spent) && spent > 0);

        // No Feather left: the most expensive card in hand (Permit A38, 2 Energy) is gone for the fight.
        play.CombatDriver.EndTurn();
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        var combat = play.CombatDriver.Current!;
        var banished = combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.BanishedPile);
        Assert.Single(banished);
        Assert.DoesNotContain(combat.Hand, c => c.DefinitionId.value == Permit);
        Assert.Equal(1, Stacks(Body(play, ammit), ActFour.DevouredId));
        Assert.Equal(ActFour.DevourStrength, Stacks(Body(play, ammit), "strength"));
        play.Dispose();
    }

    // Play the card she is eyeing and her eye moves on to the next most expensive card you still hold.
    [Fact]
    public void Playing_the_eyed_card_moves_her_eye_to_the_next_most_expensive()
    {
        var (play, session, ammit) = Solo(ActFour.AmmitEnemyId, "crouch_by_the_scale", [Permit, Deed, Deed, Working, Deed]);
        Assert.Equal(1, Stacks(Hero(play), ActFour.AmmitsGazeId));
        Play(play, session, Permit, ammit);
        Assert.Equal(1, Stacks(Hero(play), ActFour.AmmitsGazeId));
        play.CombatDriver!.EndTurn();
        play.CombatDriver.SupplyOptionChoice([1]); // keep the Feather, let her eat
        var combat = play.CombatDriver.Current!;
        var banished = combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.BanishedPile);
        Assert.Single(banished);
        Assert.NotEqual(Permit, banished[0].DefinitionId.value);
        Assert.Equal(1, Stacks(Hero(play), ActFour.FeatherOfTruthId));
        Assert.Equal(ActFour.DevourStrength, Stacks(Body(play, ammit), "strength"));
        play.Dispose();
    }

    [Fact]
    public void Ammit_finds_nothing_in_an_empty_hand()
    {
        var (play, session, ammit) = Solo(ActFour.AmmitEnemyId, "crouch_by_the_scale", [Deed, Deed, Deed, Deed, Deed]);
        for (var i = 0; i < 5; i++)
            Play(play, session, Deed, ammit);
        play.CombatDriver!.EndTurn();
        Assert.Null(play.CombatDriver.PendingOptionChoice);
        Assert.Equal(1, Stacks(Hero(play), ActFour.FeatherOfTruthId));
        Assert.Equal(0, Stacks(Body(play, ammit), ActFour.DevouredId));
        play.Dispose();
    }

    // ── the Lioness ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_wound_is_rage_and_a_blow_blocked_whole_calms_her()
    {
        var (play, session, lioness) = Solo(ActFour.LionessEnemyId, "pounce", Many(Working));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play, lioness), ActFour.RageId));

        for (var i = 0; i < 5; i++)
            Play(play, session, Working, null);
        play.CombatDriver.EndTurn();
        Assert.Equal(0, Stacks(Body(play, lioness), ActFour.RageId));
        play.Dispose();
    }

    // ── Priest of the Four Sons ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Four_jars_stand_beside_the_priest_and_he_refills_a_broken_one()
    {
        var (play, session, priest) = Solo(ActFour.SurveyorEliteEnemyId, "refill", Many(Deed));
        Assert.Equal(4, Wearing(play, ActFour.CanopicJarId).Count());
        Assert.Equal(1, Stacks(Body(play, priest), ActFour.JarPowerId("lung")));
        Assert.Equal(1, Stacks(Body(play, priest), ActFour.RisenByKaId));

        var liver = Wearing(play, ActFour.JarBodyId("liver")).Single().Id;
        for (var i = 0; i < 4; i++)
            Play(play, session, Deed, liver);
        Assert.Empty(Wearing(play, ActFour.JarBodyId("liver")));
        Assert.Equal(0, Stacks(Body(play, priest), ActFour.JarPowerId("liver")));

        play.CombatDriver!.EndTurn();
        Assert.Single(Wearing(play, ActFour.JarBodyId("liver")));
        Assert.Equal(1, Stacks(Body(play, priest), ActFour.JarPowerId("liver")));
        play.Dispose();
    }

    // ── Brood Mother ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_second_round_each_jar_hatches_a_scarab_that_bites_three_times()
    {
        var (play, _, _) = Solo(ActFour.ScarabHostEnemyId, "brood_wall", Many(Deed));
        Assert.Equal(3, Wearing(play, ActFour.BroodJarId).Count());
        play.CombatDriver!.EndTurn();
        Assert.Empty(Wearing(play, ActFour.BroodScarabId));
        play.CombatDriver.EndTurn();
        Assert.Equal(3, Wearing(play, ActFour.BroodScarabId).Count());
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(before - 3 * ActFour.ScarabBites * ActFour.ScarabBite, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the Embalmer ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void One_card_a_turn_is_wrapped_free_and_playing_it_is_entombed()
    {
        var (play, session, embalmer) = Solo(ActFour.LinenOverseerEnemyId, "wrapped_beyond_recognition", Many(Permit), energy: 3);
        var wrapped = play.CombatDriver!.Current!.Hand.Where(c => c.Marks.Contains(new TagId(ActFour.LinenWrappedMark))).ToList();
        var card = Assert.Single(wrapped);
        play.CombatDriver.PlayCard(card.Id, embalmer);
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(1, Stacks(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // ── the Restless King ─────────────────────────────────────────────────────────────────────────────────

    // Deeds into a body until it is Downed (a rise's jar may have given it Block or a softer first hit).
    private static void StrikeUntil(RunPlayback play, InteractiveRunSession session, CombatantId id, Func<bool> done)
    {
        while (!done() && play.CombatDriver!.Current is { } fight && fight.Hand.Any(c => c.DefinitionId.value == Deed))
            Play(play, session, Deed, id);
    }

    [Fact]
    public void The_king_rises_twice_each_time_with_a_jar_power()
    {
        var (play, session, king) = Solo(ActFour.CurseEnemyId, "sealed", Many(Deed), hp: 10);
        StrikeUntil(play, session, king, () => Stacks(Body(play, king), ActFour.RisenDownedId) > 0);
        Assert.Equal(1, Body(play, king).Health.Current);
        Assert.Equal(1, Stacks(Body(play, king), ActFour.KingRisenLastId));

        play.CombatDriver!.EndTurn();
        var risen = Body(play, king);
        Assert.Equal(4, risen.Health.Current);
        Assert.Equal(0, Stacks(risen, ActFour.RisenDownedId));
        Assert.Single(ActFour.KingPowers, key => Stacks(risen, ActFour.JarPowerId(key)) > 0);

        StrikeUntil(play, session, king, () => Stacks(Body(play, king), ActFour.RisenDownedId) > 0);
        Assert.Equal(1, Stacks(Body(play, king), ActFour.RisenDownedId));
        play.CombatDriver.EndTurn();
        StrikeUntil(play, session, king, () => Over(play));
        Assert.True(Over(play));
        play.Dispose();
    }

    // ── the Three Robbers ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_robber_falls_the_others_rage_and_one_takes_up_its_tool()
    {
        var (play, session, _) = FightProbe.Start(FightProbe.Roster("robbers", 9,
            (ActFour.PryBarVeteranEnemyId, "bronze_pry_bar", 80),
            (ActFour.LampThiefEnemyId, "snuff_the_lamp", 6),
            (ActFour.CurseBearerEnemyId, "cursed_amulet", 80)), Many(Deed), health: 300);
        var lamp = State(play).Combatants.First(c => c.Id.value.StartsWith(ActFour.LampThiefEnemyId, StringComparison.Ordinal)).Id;
        Play(play, session, Deed, lamp);

        var survivors = Wearing(play, ActFour.TombbreakerId).ToList();
        Assert.Equal(2, survivors.Count);
        Assert.All(survivors, s => Assert.Equal(ActFour.RobberRage, Stacks(s, "strength")));
        var heir = Assert.Single(survivors, s => Stacks(s, ActFour.HoldsToolId("knife")) > 0);
        Assert.Equal(1, Stacks(heir, ActFour.ToolReadyId("knife")));
        play.Dispose();
    }
}
