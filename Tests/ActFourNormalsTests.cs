using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-IV rework of 2026-11-04 (user): the Scale and its Feather, Risen, the canopic jars, and each normal
// body's own rule.
public class ActFourNormalsTests
{
    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static IEnumerable<CombatantState> Jars(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.Where(c => c.IsAlive && FightProbe.StacksOf(c, ActFour.CanopicJarId) > 0);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // Gone: fallen, or the fight is over because it fell.
    private static bool Gone(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current is not { } combat || !combat.State.GetCombatant(id).IsAlive;

    private static int Stacks(CombatantState who, string status) => FightProbe.StacksOf(who, status);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private const string Deed = "paper_cut";              // 1 Energy, Deed, 6 damage
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working, 5 Block
    private const string Permit = "permit_a38";           // 2 Energy, Working, 5 Paperwork on the target
    private const string MultiHit = "threefold_injunction"; // 1 Energy, Deed, 3 damage 3 times

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Solo(
        string enemy, string intent, IReadOnlyList<string> deck, int hp = 200, int energy = 9) =>
        FightProbe.Start(FightProbe.Roster(enemy, energy, (enemy, intent, hp)), deck, health: 300);

    // ── the Scale ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_balanced_turn_is_a_feather_and_a_crooked_one_is_burdened()
    {
        var (play, session, rope) = Solo("reed_cord_surveyor", "re_tension_cord", [Deed, Working, Deed, Deed, Working]);
        Play(play, session, Deed, rope);
        Play(play, session, Working, null);
        Assert.Equal(1, Stacks(Body(play, rope), ActFour.DeedsPanId));
        Assert.Equal(1, Stacks(Body(play, rope), ActFour.WorkingsPanId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.ScaleFeatherId));
        Assert.Equal(0, Stacks(Hero(play), ActFour.BurdenedId));
        Assert.Equal(0, Stacks(Body(play, rope), ActFour.DeedsPanId));
        play.Dispose();

        var (play2, session2, rope2) = Solo("reed_cord_surveyor", "re_tension_cord", [.. Enumerable.Repeat(Deed, 5)]);
        Play(play2, session2, Deed, rope2);
        Play(play2, session2, Deed, rope2);
        play2.CombatDriver!.EndTurn();
        Assert.Equal(0, Stacks(Hero(play2), ActFour.ScaleFeatherId));
        Assert.Equal(1, Stacks(Hero(play2), ActFour.BurdenedId));
        play2.Dispose();
    }

    [Fact]
    public void An_empty_turn_is_not_balanced()
    {
        var (play, _, _) = Solo("reed_cord_surveyor", "re_tension_cord", [.. Enumerable.Repeat(Deed, 5)]);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    [Fact]
    public void A_feather_makes_the_next_attack_five_heavier_and_is_spent()
    {
        var (play, session, rope) = FightProbe.Start(
            FightProbe.RosterAgainstHero("feather", 9, [(ActFour.ScaleFeatherId, 1)], ("reed_cord_surveyor", "re_tension_cord", 200)),
            [.. Enumerable.Repeat(Deed, 5)], health: 300);
        var before = Body(play, rope).Health.Current;
        Play(play, session, Deed, rope);
        Assert.Equal(before - 6 - ActFour.FeatherDamage, Body(play, rope).Health.Current);
        Assert.Equal(0, Stacks(Hero(play), ActFour.ScaleFeatherId));
        var after = Body(play, rope).Health.Current;
        Play(play, session, Deed, rope);
        Assert.Equal(after - 6, Body(play, rope).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void The_crooked_cubit_wants_one_deed_more_in_odd_rounds()
    {
        var (play, session, cubit) = Solo("crooked_rod_bearer", "brace_the_standard", [.. Enumerable.Repeat(Deed, 5)]);
        Assert.Equal(1, Stacks(Body(play, cubit), ActFour.WantsDeedsId));
        Play(play, session, Deed, cubit);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.ScaleFeatherId));
        Assert.Equal(1, Stacks(Body(play, cubit), ActFour.WantsWorkingsId));
        Assert.Equal(0, Stacks(Body(play, cubit), ActFour.WantsDeedsId));
        play.Dispose();
    }

    [Fact]
    public void The_chiseller_counts_the_chiselled_pan_twice()
    {
        var (play, session, chisel) = Solo("name_erasing_chisel_spirit", "stone_dust", [Deed, Working, Working, Deed, Deed]);
        Play(play, session, Deed, chisel);
        Assert.Equal(2, Stacks(Body(play, chisel), ActFour.DeedsPanId));
        Play(play, session, Working, null);
        Play(play, session, Working, null);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.ScaleFeatherId));
        play.Dispose();
    }

    [Fact]
    public void Three_balanced_turns_and_the_judge_acquits_you()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("judge", 9, ("assessor_of_the_forty_two", "weigh_the_answer", 200), ("unfinished_mummy", "stillness", 200)),
            [Deed, Working, Deed, Working, Deed], health: 300);
        var judge = Find(play, "assessor_of_the_forty_two");
        for (var turn = 0; turn < ActFour.JudgeAcquits; turn++)
        {
            Assert.True(Body(play, judge).IsAlive);
            Play(play, session, Deed, judge);
            Play(play, session, Working, null);
            play.CombatDriver.EndTurn();
        }
        Assert.False(Body(play, judge).IsAlive);
        play.Dispose();
    }

    [Fact]
    public void A_crooked_turn_moves_the_eclipse()
    {
        var (play, _, beetle) = Solo("eclipse_scarab", "approach_noon", [.. Enumerable.Repeat(Deed, 5)]);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play, beetle), ActFour.ApproachOfNoonId));
        play.Dispose();
    }

    [Fact]
    public void A_crooked_turn_before_thoth_shelters_every_enemy()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("ibis", 9, ("moon_cycle_ibis", "moon_peck", 200), ("unfinished_mummy", "hook_drag", 200)),
            [.. Enumerable.Repeat(Deed, 5)], health: 300);
        play.CombatDriver!.EndTurn();
        // Sheltered at the end of their own turn, so the Block stands through yours.
        Assert.True(Block(Body(play, Find(play, "moon_cycle_ibis"))) >= 6);
        Assert.True(Block(Body(play, Find(play, "unfinished_mummy"))) >= 6);
        play.Dispose();
    }

    // ── Risen ─────────────────────────────────────────────────────────────────────────────────────────────

    // A lethal blow only Downs a Risen body: 1 HP, no damage taken. Its next turn it only rises (40 %, debuffs
    // gone) and strikes nobody; the turn after, it acts again.
    [Fact]
    public void A_risen_body_is_downed_then_rises_on_its_turn_and_only_then_acts()
    {
        var (play, session, golem) = Solo("fallen_capstone_golem", "falling_dust", [Permit, Deed, Deed, Deed, Deed], hp: 10);
        Play(play, session, Permit, golem);
        Play(play, session, Deed, golem);
        Play(play, session, Deed, golem);
        Assert.Equal(1, Body(play, golem).Health.Current);
        Assert.Equal(1, Stacks(Body(play, golem), ActFour.RisenDownedId));
        Play(play, session, Deed, golem);
        Assert.Equal(1, Body(play, golem).Health.Current);

        var hero = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(4, Body(play, golem).Health.Current);
        Assert.Equal(0, Stacks(Body(play, golem), ActFour.RisenDownedId));
        Assert.Equal(0, Stacks(Body(play, golem), "paperwork"));
        Assert.Equal(0, Stacks(Body(play, golem), ActFour.RisenGolemId));
        Assert.Equal(hero, Hero(play).Health.Current);

        play.CombatDriver.EndTurn();
        Assert.True(Hero(play).Health.Current < hero);
        play.Dispose();
    }

    [Fact]
    public void A_risen_body_dies_for_good_the_second_time()
    {
        var (play, session, golem) = Solo("fallen_capstone_golem", "set_support", [.. Enumerable.Repeat(Deed, 5)], hp: 10);
        Play(play, session, Deed, golem);
        Play(play, session, Deed, golem);
        play.CombatDriver!.EndTurn();
        Play(play, session, Deed, golem);
        Assert.True(Gone(play, golem));
        play.Dispose();
    }

    // Rise is the Necropolis' and the Works' word since the content review (E16): four normals, no more.
    [Fact]
    public void Only_four_normal_bodies_rise()
    {
        var risen = new[] { ActFour.RisenId, ActFour.RisenGolemId, ActFour.RisenDoorId, ActFour.RisenFarmerId, ActFour.RisenKeeperId };
        var rising = FightProbe.Game.Encounters.SelectMany(e => e.Enemies)
            .Where(e => ActFour.Identities.Contains(e.Id) && (e.StartingStatuses ?? []).Any(s => risen.Contains(s.Status.value)))
            .Select(e => e.Id).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
        Assert.Equal(["fallen_capstone_golem", "false_door_finder", "fourfold_vessel_guardian", "silt_buried_farmer_shade"], rising);
        var farmer = FightProbe.Game.Statuses.First(st => st.Id == ActFour.RisenFarmerId).NameKey;
        var king = FightProbe.Game.Statuses.First(st => st.Id == ActFour.KingRisenId).NameKey;
        Assert.NotEqual(king, farmer);
    }

    [Fact]
    public void The_drowned_farmer_rises_twice_at_a_quarter()
    {
        var (play, session, farmer) = Solo("silt_buried_farmer_shade", "raise_the_bank", [.. Enumerable.Repeat(Deed, 5)], hp: 12);
        Play(play, session, Deed, farmer);
        Play(play, session, Deed, farmer);
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Body(play, farmer).Health.Current);
        Play(play, session, Deed, farmer);
        Assert.Equal(1, Stacks(Body(play, farmer), ActFour.RisenDownedId));
        play.CombatDriver.EndTurn();
        Assert.Equal(3, Body(play, farmer).Health.Current);
        Play(play, session, Deed, farmer);
        Assert.True(Gone(play, farmer));
        play.Dispose();
    }

    [Fact]
    public void The_golem_rises_to_bring_the_capstone_next_and_the_ka_returns_armoured()
    {
        var (play, session, golem) = Solo("fallen_capstone_golem", "set_support", [.. Enumerable.Repeat(Deed, 5)], hp: 10);
        Play(play, session, Deed, golem);
        Play(play, session, Deed, golem);
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play, golem), ActFour.CapstoneDueId));
        var hero = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(0, Stacks(Body(play, golem), ActFour.CapstoneDueId));
        Assert.Equal(hero - 23, Hero(play).Health.Current);
        play.Dispose();

        var (play2, session2, ka) = Solo("false_door_finder", "stone_door", [.. Enumerable.Repeat(Deed, 5)], hp: 10);
        Play(play2, session2, Deed, ka);
        Play(play2, session2, Deed, ka);
        play2.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Body(play2, ka), ActFour.ThroughTheDoorId));
        Assert.Equal(20, Block(Body(play2, ka)));
        play2.Dispose();
    }

    // ── the House of Linen and the Necropolis (content review E16–E18) ──────────────────────────────────

    // Four layers; each swallows one hit whole. Fewer than three hits in a round and it wraps one more (at most
    // four); three hits and it does not.
    [Fact]
    public void The_mummys_linen_swallows_a_hit_per_layer_and_is_rewrapped_unless_hit_three_times()
    {
        var (play, session, mummy) = Solo("unfinished_mummy", "hook_drag", [.. Enumerable.Repeat(Deed, 15)], hp: 200);
        Assert.Equal(ActFour.LinenLayers, Stacks(Body(play, mummy), ActFour.LinenLayerId));
        Play(play, session, Deed, mummy);
        Play(play, session, Deed, mummy);
        Assert.Equal(200, Body(play, mummy).Health.Current);
        Assert.Equal(ActFour.LinenLayers - 2, Stacks(Body(play, mummy), ActFour.LinenLayerId));
        Assert.Equal(2, Stacks(Body(play, mummy), ActFour.UnwindingId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(ActFour.LinenLayers - 1, Stacks(Body(play, mummy), ActFour.LinenLayerId));
        Assert.Equal(0, Stacks(Body(play, mummy), ActFour.UnwindingId));

        // Three hits: three layers torn off, and no new one.
        Play(play, session, Deed, mummy);
        Play(play, session, Deed, mummy);
        Play(play, session, Deed, mummy);
        Assert.Equal(200, Body(play, mummy).Health.Current);
        Assert.Equal(0, Stacks(Body(play, mummy), ActFour.LinenLayerId));
        play.CombatDriver.EndTurn();
        Assert.Equal(0, Stacks(Body(play, mummy), ActFour.LinenLayerId));

        // Bare: a hit lands in full, and only now does a single hit a round let it wrap again.
        Play(play, session, Deed, mummy);
        Assert.Equal(194, Body(play, mummy).Health.Current);
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Stacks(Body(play, mummy), ActFour.LinenLayerId));
        Assert.Equal(0, Stacks(Body(play, mummy), ActFour.RisenId));
        play.Dispose();
    }

    // A multi-hit card spends one layer per hit, not one per card.
    [Fact]
    public void Every_hit_of_a_multi_hit_card_spends_its_own_layer()
    {
        var (play, session, mummy) = Solo("unfinished_mummy", "hook_drag", [.. Enumerable.Repeat(MultiHit, 5)], hp: 200);
        Play(play, session, MultiHit, mummy);
        Assert.Equal(ActFour.LinenLayers - 3, Stacks(Body(play, mummy), ActFour.LinenLayerId));
        Assert.Equal(3, Stacks(Body(play, mummy), ActFour.UnwindingId));
        Assert.Equal(200, Body(play, mummy).Health.Current);
        play.Dispose();
    }

    // The Block you still have when its turn ends becomes its own: block what it hits for, not more.
    [Fact]
    public void The_natron_bearer_dries_your_leftover_block_into_its_own()
    {
        var (play, session, bearer) = Solo("natron_bearer", "drying_rite", [.. Enumerable.Repeat(Working, 5)]);
        Play(play, session, Working, null);
        Play(play, session, Working, null);
        Play(play, session, Working, null);
        play.CombatDriver!.EndTurn();
        // 15 Block against its 13: 2 left over, and that is what it gains.
        Assert.Equal(2, Block(Body(play, bearer)));
        Assert.Equal(0, Stacks(Body(play, bearer), ActFour.RisenId));
        play.Dispose();

        var (play2, session2, bearer2) = Solo("natron_bearer", "pack_natron", [.. Enumerable.Repeat(Working, 5)]);
        Play(play2, session2, Working, null);
        Play(play2, session2, Working, null);
        play2.CombatDriver!.EndTurn();
        // It only packed natron (24): all 10 of yours dried into its own.
        Assert.Equal(24 + 10, Block(Body(play2, bearer2)));
        play2.Dispose();
    }

    // Every hit through your Block is Gold in its sack; after four of its turns it flees with them.
    [Fact]
    public void The_excavator_steals_gold_with_every_hit_and_flees_with_it_after_four_turns()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("excavator", 9, ("cursed_loot_bearer", "loot_swing", 200), ("false_door_finder", "stone_door", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);
        var robber = Find(play, "cursed_loot_bearer");
        Assert.Equal(ActFour.RobberTurns, Stacks(Body(play, robber), ActFour.EscapeShaftId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(ActFour.GoldPerHit, Stacks(Body(play, robber), ActFour.StolenGoldId));
        Assert.Equal(ActFour.RobberTurns - 1, Stacks(Body(play, robber), ActFour.EscapeShaftId));
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.True(Body(play, robber).IsAlive);
        play.CombatDriver.EndTurn();
        Assert.False(Body(play, robber).IsAlive);
        Assert.Equal(ActFour.RobberTurns * ActFour.GoldPerHit, Hero(play).Counters[ActFour.GoldCarriedOff]);
        Assert.False(Hero(play).Counters.TryGetValue(ActFour.LootClaimed, out var claimed) && claimed > 0);
        play.Dispose();
    }

    [Fact]
    public void A_blocked_hit_steals_nothing_and_killing_the_excavator_keeps_your_gold()
    {
        var (play, session, robber) = Solo("cursed_loot_bearer", "cursed_load", [Working, Working, Working, Working, Deed, Deed, Deed, Deed, Deed, Deed], hp: 12);
        var hand = play.CombatDriver!.Current!.Hand.Count(c => c.DefinitionId.value == Working);
        for (var i = 0; i < hand; i++)
            Play(play, session, Working, null);
        play.CombatDriver.EndTurn();
        if (hand * 5 >= 16)
            Assert.Equal(0, Stacks(Body(play, robber), ActFour.StolenGoldId));
        while (play.CombatDriver.Current is not null && Body(play, robber).IsAlive
               && play.CombatDriver.Current.Hand.Any(c => c.DefinitionId.value == Deed))
            Play(play, session, Deed, robber);
        Assert.True(Gone(play, robber));
        play.Dispose();
    }

    [Fact]
    public void The_excavators_kill_is_counted_for_tomb_gold_and_nothing_is_carried_off()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("excavator_kill", 9, ("cursed_loot_bearer", "loot_swing", 12), ("false_door_finder", "stone_door", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var robber = Find(play, "cursed_loot_bearer");
        play.CombatDriver!.EndTurn();
        Assert.Equal(ActFour.GoldPerHit, Stacks(Body(play, robber), ActFour.StolenGoldId));
        Play(play, session, Deed, robber);
        Play(play, session, Deed, robber);
        Assert.False(Body(play, robber).IsAlive);
        Assert.Equal(1, Hero(play).Counters[ActFour.LootClaimed]);
        Assert.False(Hero(play).Counters.TryGetValue(ActFour.GoldCarriedOff, out var gone) && gone > 0);
        play.Dispose();
    }

    // A fallen ally leaves a husk; on its next turn the Jackal guides it west and the ally stands again (40 %).
    [Fact]
    public void A_fallen_ally_leaves_a_husk_and_the_jackal_raises_it_on_its_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("jackal", 9, ("jackal_of_the_western_gate", "stand_the_watch", 200), ("cursed_loot_bearer", "loot_swing", 10)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var jackal = Find(play, "jackal_of_the_western_gate");
        var robber = Find(play, "cursed_loot_bearer");
        Play(play, session, Deed, robber);
        Play(play, session, Deed, robber);
        Assert.False(Body(play, robber).IsAlive);
        var husk = play.CombatDriver!.Current!.State.Combatants.Single(c => c.IsAlive && Stacks(c, ActFour.HuskId) > 0);
        Assert.Equal(ActFour.HuskHealth, husk.Health.Max);
        Assert.Equal(1, Stacks(Body(play, jackal), ActFour.GuidingWestId));
        var intent = FightProbe.Game.EnemyActions.First(a => a.Id == "jackal_of_the_western_gate.westward_guidance");
        Assert.StartsWith("Westward Guidance", intent.Intent.Label, StringComparison.Ordinal);

        play.CombatDriver.EndTurn();
        Assert.True(Body(play, robber).IsAlive);
        Assert.Equal(4, Body(play, robber).Health.Current);
        Assert.Equal(1, Stacks(Body(play, robber), ActFour.GuidedWestId));
        Assert.DoesNotContain(play.CombatDriver.Current!.State.Combatants, c => c.IsAlive && Stacks(c, ActFour.HuskId) > 0);
        Assert.Equal(0, Stacks(Body(play, jackal), ActFour.GuidingWestId));

        // Guided once: the second fall is for good, and leaves no husk.
        Play(play, session, Deed, robber);
        Assert.False(Body(play, robber).IsAlive);
        Assert.DoesNotContain(play.CombatDriver.Current!.State.Combatants, c => c.IsAlive && Stacks(c, ActFour.HuskId) > 0);
        play.Dispose();
    }

    [Fact]
    public void Destroying_the_husk_keeps_the_ally_down()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("jackal_husk", 9, ("jackal_of_the_western_gate", "stand_the_watch", 200), ("cursed_loot_bearer", "inventory_shield", 10)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var jackal = Find(play, "jackal_of_the_western_gate");
        var robber = Find(play, "cursed_loot_bearer");
        Play(play, session, Deed, robber);
        Play(play, session, Deed, robber);
        var husk = play.CombatDriver!.Current!.State.Combatants.Single(c => c.IsAlive && Stacks(c, ActFour.HuskId) > 0).Id;
        Play(play, session, Deed, husk);
        Play(play, session, Deed, husk);
        Assert.False(Body(play, husk).IsAlive);
        Assert.Equal(0, Stacks(Body(play, jackal), ActFour.GuidingWestId));
        play.CombatDriver.EndTurn();
        Assert.False(Body(play, robber).IsAlive);
        play.Dispose();
    }

    [Fact]
    public void Killing_the_jackal_crumbles_the_husk()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("jackal_falls", 9, ("jackal_of_the_western_gate", "stand_the_watch", 10), ("cursed_loot_bearer", "inventory_shield", 10)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var jackal = Find(play, "jackal_of_the_western_gate");
        var robber = Find(play, "cursed_loot_bearer");
        Play(play, session, Deed, robber);
        Play(play, session, Deed, robber);
        Assert.Contains(play.CombatDriver!.Current!.State.Combatants, c => c.IsAlive && Stacks(c, ActFour.HuskId) > 0);
        Play(play, session, Deed, jackal);
        Play(play, session, Deed, jackal);
        Assert.True(Gone(play, jackal));
        play.Dispose();
    }

    [Fact]
    public void In_a_duo_the_keeper_rises_while_its_ally_lives()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("keeper", 9, ("fourfold_vessel_guardian", "body_office", 10), ("fallen_capstone_golem", "set_support", 5)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var keeper = Find(play, "fourfold_vessel_guardian");
        var mummy = Find(play, "fallen_capstone_golem");
        Play(play, session, Deed, keeper);
        Play(play, session, Deed, keeper);
        Play(play, session, Deed, mummy);
        Assert.Equal(1, Stacks(Body(play, keeper), ActFour.RisenDownedId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(4, Body(play, keeper).Health.Current);
        Assert.Equal(1, Stacks(Body(play, keeper), ActFour.RisenKeeperId));
        // The Golem rose once already; now it goes for good, and the Keeper has nothing left to rise for.
        Play(play, session, Deed, mummy);
        Assert.False(Body(play, mummy).IsAlive);
        Assert.Equal(0, Stacks(Body(play, keeper), ActFour.RisenKeeperId));
        play.Dispose();
    }

    // ── the bodies' own rules ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_thief_steals_a_card_with_every_hit_that_lands_and_gives_them_back_when_it_dies()
    {
        var (play, session, thief) = Solo("hungry_grain_thief", "feast_on_rations", [.. Enumerable.Repeat(Deed, 10)], hp: 12);
        play.CombatDriver!.EndTurn();
        Assert.Equal(2, Stacks(Body(play, thief), ActFour.SackId));
        var combat = play.CombatDriver.Current!;
        Assert.Equal(2, combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.BanishedPile).Count);
        Assert.Equal(3, combat.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void The_sack_holds_three_cards_at_most()
    {
        var (play, _, thief) = Solo("hungry_grain_thief", "feast_on_rations", [.. Enumerable.Repeat(Deed, 10)], hp: 12);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(ActFour.SackSize, Stacks(Body(play, thief), ActFour.SackId));
        var combat = play.CombatDriver.Current!;
        Assert.Equal(ActFour.SackSize, combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.BanishedPile).Count);
        play.Dispose();
    }

    [Fact]
    public void Beside_sobek_the_sack_still_holds_three_at_most()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("thief_duo", 9, ("crocodile_of_the_short_measure", "load_the_scale", 60), ("hungry_grain_thief", "feast_on_rations", 60)),
            [.. Enumerable.Repeat(Deed, 10)], health: 3000);
        for (var turn = 0; turn < 12; turn++)
        {
            play.CombatDriver!.EndTurn();
            var combat = play.CombatDriver.Current!;
            Assert.True(combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.BanishedPile).Count <= ActFour.SackSize);
            Assert.NotEmpty(combat.Hand);
        }
        play.Dispose();
    }

    [Fact]
    public void Killing_the_thief_opens_its_sack()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("thief", 9, ("hungry_grain_thief", "feast_on_rations", 12), ("unfinished_mummy", "stillness", 200)),
            [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var thief = Find(play, "hungry_grain_thief");
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, play.CombatDriver.Current!.Hand.Count);
        Play(play, session, Deed, thief);
        Play(play, session, Deed, thief);
        Assert.False(Body(play, thief).IsAlive);
        var combat = play.CombatDriver.Current!;
        Assert.Empty(combat.State.GetCardZones(combat.HeroId).GetCardsInZone(CardZone.BanishedPile));
        Assert.Equal(3, combat.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Every_swarm_hit_through_your_block_is_a_burden()
    {
        var (play, _, _) = Solo("jar_seal_scarab_swarm", "seal_swarm", [.. Enumerable.Repeat(Deed, 5)]);
        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Stacks(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    [Fact]
    public void Sobeks_jaws_grow_with_your_burden()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("sobek", 9, [(ActFour.BurdenedId, 2)], ("crocodile_of_the_short_measure", "snap_at_the_deficit", 200)),
            [.. Enumerable.Repeat(Working, 5)], health: 300);
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        // Authored (untuned): 15 + 3 a Burdened.
        Assert.Equal(before - (15 + 2 * 3), Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void A_surcharge_is_a_stone_and_the_stone_blow_spends_them()
    {
        var (play, session, ushabti) = FightProbe.Start(
            FightProbe.RosterAgainstHero("ushabti", 9, [(ActFour.BurdenedId, 2)], ("stone_hauler_ushabti", "stone_blow", 200)),
            [.. Enumerable.Repeat(Working, 5)], health: 300);
        Play(play, session, Working, null);
        Play(play, session, Working, null);
        play.CombatDriver!.EndTurn();
        // Quarried at its turn start, spent by the blow that turn.
        Assert.Equal(0, Stacks(Body(play, ushabti), ActFour.StoneId));
        play.Dispose();
    }

    [Fact]
    public void The_runaway_flees_after_its_third_turn_and_its_ally_is_enraged()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("runaway", 9, ("runaway_laborer", "desperate_swing", 200), ("stone_hauler_ushabti", "brace_the_stone", 200)),
            [.. Enumerable.Repeat(Working, 10)], health: 300);
        var runaway = Find(play, "runaway_laborer");
        var hauler = Find(play, "stone_hauler_ushabti");
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.True(Body(play, runaway).IsAlive);
        play.CombatDriver.EndTurn();
        Assert.False(Body(play, runaway).IsAlive);
        Assert.Equal(ActFour.RunawayRage, Stacks(Body(play, hauler), "strength"));
        play.Dispose();
    }

    // ── the jars ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_single_normal_fight_opens_with_jars_and_a_multi_fight_does_not()
    {
        Assert.Contains(ActFour.HeroOpening(["unfinished_mummy"], "combat"), s => s.Status.value == ActFour.CanopicRiteId);
        Assert.DoesNotContain(ActFour.HeroOpening(["natron_bearer", "unfinished_mummy"], "multi_combat"), s => s.Status.value == ActFour.CanopicRiteId);
        Assert.DoesNotContain(ActFour.HeroOpening(["cursed_loot_bearer"], "mimic"), s => s.Status.value == ActFour.CanopicRiteId);
        var single = FightProbe.Game.Encounters.First(e => e.Id.Value == "labyrinth_linen_03");
        Assert.Contains(single.HeroStartingStatuses!, s => s.Status.value == ActFour.CanopicRiteId);
        var duo = FightProbe.Game.Encounters.First(e => e.Id.Value == "labyrinth_linen_duo_02");
        Assert.DoesNotContain(duo.HeroStartingStatuses!, s => s.Status.value == ActFour.CanopicRiteId);
    }

    [Fact]
    public void The_jars_stand_beside_their_master_give_their_powers_and_fall_with_it()
    {
        var (play, session, _) = FightProbe.Start(FightProbe.Authored("labyrinth_linen_03", energy: 9), [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var mummy = Find(play, "unfinished_mummy");
        var jars = Jars(play).ToList();
        Assert.InRange(jars.Count, 1, 2);
        var keys = ActFour.Jars.Where(j => jars.Any(b => Stacks(b, ActFour.JarBodyId(j.Key)) > 0)).Select(j => j.Key).ToList();
        Assert.Equal(jars.Count, keys.Count);
        foreach (var key in keys)
            Assert.True(Stacks(Body(play, mummy), ActFour.JarPowerId(key)) > 0, key);

        // Break the first jar: its power leaves the Mummy.
        var first = jars[0].Id;
        var firstKey = keys.First(k => Stacks(Body(play, first), ActFour.JarBodyId(k)) > 0);
        Play(play, session, Deed, first);
        Play(play, session, Deed, first);
        Play(play, session, Deed, first);
        Assert.False(Body(play, first).IsAlive);
        Assert.Equal(0, Stacks(Body(play, mummy), ActFour.JarPowerId(firstKey)));
        play.Dispose();
    }

    [Fact]
    public void The_keeper_always_takes_two_jars()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_vaults_01"), [.. Enumerable.Repeat(Deed, 5)], health: 300);
        Assert.Equal(2, Jars(play).Count());
        play.Dispose();
    }

    [Fact]
    public void A_master_that_falls_takes_its_jars_with_it()
    {
        var (play, session, _) = FightProbe.Start(FightProbe.Authored("labyrinth_stelae_01", energy: 9), [.. Enumerable.Repeat(Deed, 10)], health: 300);
        var rope = Find(play, "reed_cord_surveyor");
        Assert.NotEmpty(Jars(play));
        var risesAgain = Stacks(Body(play, rope), ActFour.RisenByKaId) > 0;
        while (Body(play, rope).IsAlive && play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Deed))
            Play(play, session, Deed, rope);
        if (!risesAgain && !Body(play, rope).IsAlive)
            Assert.Empty(Jars(play));
        play.Dispose();
    }

    [Fact]
    public void Jar_powers_do_what_they_say()
    {
        // Lung: 6 Block at the end of its turn.
        var (play, _, rope) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "reed_lash", energy: 9, (ActFour.JarPowerId("lung"), 1)), [.. Enumerable.Repeat(Deed, 5)], health: 300);
        play.CombatDriver!.EndTurn();
        Assert.Equal(6, Block(Body(play, rope)));
        play.Dispose();

        // Breath: the first hit each turn on it deals 5 less.
        var (play2, session2, rope2) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "reed_lash", energy: 9, (ActFour.JarPowerId("breath"), 1)), [.. Enumerable.Repeat(Deed, 5)], health: 300);
        var hp = Body(play2, rope2).Health.Current;
        Play(play2, session2, Deed, rope2);
        Assert.Equal(hp - 1, Body(play2, rope2).Health.Current);
        Play(play2, session2, Deed, rope2);
        Assert.Equal(hp - 7, Body(play2, rope2).Health.Current);
        play2.Dispose();

        // Blood: its attacks deal 3 more.
        var (play3, _, _) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "reed_lash", energy: 9, (ActFour.JarPowerId("blood"), 1)), [.. Enumerable.Repeat(Working, 5)], health: 300);
        var before = Hero(play3).Health.Current;
        play3.CombatDriver!.EndTurn();
        Assert.Equal(before - (12 + 3), Hero(play3).Health.Current);
        play3.Dispose();

        // Stomach: its attack gives you 1 Burdened (and the crooked scale one more).
        var (play4, session4, rope4) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "reed_lash", energy: 9, (ActFour.JarPowerId("stomach"), 1)), [Deed, Working, Deed, Working, Deed], health: 300);
        Play(play4, session4, Deed, rope4);
        Play(play4, session4, Working, null);
        play4.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play4), ActFour.BurdenedId));
        play4.Dispose();

        // Gut: a crooked scale is 2 Burdened.
        var (play5, _, _) = FightProbe.Start(
            FightProbe.Solo("reed_cord_surveyor", "re_tension_cord", energy: 9, (ActFour.JarPowerId("gut"), 1)), [.. Enumerable.Repeat(Deed, 5)], health: 300);
        play5.CombatDriver!.EndTurn();
        Assert.Equal(2, Stacks(Hero(play5), ActFour.BurdenedId));
        play5.Dispose();
    }

    // ── Tomb Gold ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tomb_gold_costs_nothing_and_counts_its_pay()
    {
        var card = FightProbe.Game.Cards.First(c => c.Id == ActFour.TombGoldCardId);
        Assert.Contains("25 Gold", card.DescriptionKey ?? "", StringComparison.Ordinal);
        var upgraded = FightProbe.Game.Cards.First(c => c.Id == ActFour.TombGoldCardId + "+");
        Assert.Contains("50 Gold", upgraded.DescriptionKey ?? "", StringComparison.Ordinal);

        var (play, session, _) = Solo("unfinished_mummy", "stillness", [.. Enumerable.Repeat(ActFour.TombGoldCardId, 5)]);
        Play(play, session, ActFour.TombGoldCardId, null);
        Play(play, session, ActFour.TombGoldCardId, null);
        Assert.Equal(2, Hero(play).Counters[ActFour.TombGoldPlayed]);
        Assert.True(play.CombatDriver!.Current!.Hand.Count >= 3);
        play.Dispose();
    }

    // No normal or multi body of the act files Paperwork, raises a measure or entombs you any more.
    [Fact]
    public void No_normal_body_files_paperwork_weighs_energy_or_entombs()
    {
        var bodies = ActFour.Identities;
        var actions = FightProbe.Game.EnemyActions.Where(a => bodies.Contains(a.Id.Split('.')[0])).ToList();
        Assert.NotEmpty(actions);
        foreach (var action in actions)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(action.Program, RogueDeck.Run.RunJson.CreateOptions());
            Assert.DoesNotContain("\"paperwork\"", json, StringComparison.Ordinal);
            Assert.DoesNotContain("\"weighed\"", json, StringComparison.Ordinal);
            Assert.DoesNotContain("\"entombed\"", json, StringComparison.Ordinal);
        }
    }
}
