using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-IV bosses since the rework of 2026-11-04 (user): one rule each, a second half from 50 %, every count a
// stack on a status. Numbers are the authored ones (FightProbe.Game is untuned).
public class ActFourBossesTests
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

    private static bool InHand(RunPlayback play, string cardId) =>
        play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == cardId);

    private const string Deed = "paper_cut";              // 1 Energy, Deed, 6 damage
    private const string Working = "cower_behind_a_desk"; // 1 Energy, Working, 5 Block
    private const string Rite = "ash_register";           // 1 Energy, Rite

    private static string[] Many(string card, int count = 10) => [.. Enumerable.Repeat(card, count)];

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Solo(
        string enemy, string intent, IReadOnlyList<string> deck, params (string Status, int Stacks)[] statuses) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, 9, statuses), deck, health: 400);

    // ── the Pharaoh ───────────────────────────────────────────────────────────────────────────────────────

    private static string? CommandOn(CombatantState pharaoh) =>
        ActFour.Commands.Select(c => c.Key).FirstOrDefault(k => Stacks(pharaoh, ActFour.CommandId(k)) > 0);

    [Fact]
    public void Each_round_he_gives_one_command_from_his_first_name()
    {
        var (play, _, pharaoh) = Solo(ActFour.PharaohEnemyId, "royal_decree", Many(Deed));
        var command = CommandOn(Body(play, pharaoh));
        Assert.Contains(command, new[] { "blow", "silence", "tribute" });
        Assert.Equal(1, Stacks(Body(play, pharaoh), ActFour.CartoucheWardId));
        play.Dispose();
    }

    [Fact]
    public void Silence_kept_is_the_third_crack_and_breaks_his_name()
    {
        var (play, _, pharaoh) = Solo(ActFour.PharaohEnemyId, "royal_decree", Many(Deed), (ActFour.NameCracksId, 2));
        for (var turn = 0; turn < 20 && CommandOn(Body(play, pharaoh)) != "silence"; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal("silence", CommandOn(Body(play, pharaoh)));
        // the Throne Name's other commands were refused, so the cracks are still 2
        Assert.Equal(2, Stacks(Body(play, pharaoh), ActFour.NameCracksId));

        play.CombatDriver!.EndTurn();
        var body = Body(play, pharaoh);
        Assert.Equal(1, Stacks(body, ActFour.TwoLandsNameId));
        Assert.Equal(0, Stacks(body, ActFour.ThroneNameId));
        Assert.Equal(0, Stacks(body, ActFour.NameCracksId));
        // the broken name cost him the turn: he reeled, and the reel is spent
        Assert.Equal(0, Stacks(body, ActFour.NameBrokenId));
        play.Dispose();
    }

    [Fact]
    public void A_refused_blow_is_answered_on_his_turn()
    {
        var (play, _, pharaoh) = Solo(ActFour.PharaohEnemyId, "royal_decree", Many(Deed));
        for (var turn = 0; turn < 20 && CommandOn(Body(play, pharaoh)) != "blow"; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal("blow", CommandOn(Body(play, pharaoh)));
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(before - ActFour.StrikeBack, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void His_last_name_breaking_takes_his_ward()
    {
        var (play, _, pharaoh) = Solo(ActFour.PharaohEnemyId, "royal_decree", Many(Deed),
            (ActFour.EternalNameId, 1), (ActFour.NameCracksId, 2));
        // playing nothing keeps every Eternal command but Slay the Traitor
        for (var turn = 0; turn < 20 && CommandOn(Body(play, pharaoh)) == "slay"; turn++)
            play.CombatDriver!.EndTurn();
        Assert.NotEqual("slay", CommandOn(Body(play, pharaoh)));
        play.CombatDriver!.EndTurn();
        var body = Body(play, pharaoh);
        Assert.Equal(1, Stacks(body, ActFour.NameExposedId));
        Assert.Equal(0, Stacks(body, ActFour.CartoucheWardId));
        play.Dispose();
    }

    [Fact]
    public void A_traitor_left_standing_strikes_every_round_and_falls_with_him()
    {
        var (play, session, pharaoh) = Solo(ActFour.PharaohEnemyId, "royal_decree", Many(Deed), (ActFour.TwoLandsNameId, 1));
        for (var turn = 0; turn < 20 && CommandOn(Body(play, pharaoh)) != "slay"; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal("slay", CommandOn(Body(play, pharaoh)));
        var traitor = Assert.Single(Wearing(play, ActFour.TraitorCondemnedId));
        Assert.Equal(ActFour.TraitorHealth, traitor.Health.Max);

        play.CombatDriver!.EndTurn();
        Assert.Single(Wearing(play, ActFour.TraitorId));
        Assert.Empty(Wearing(play, ActFour.TraitorCondemnedId));
        play.Dispose();
    }

    [Fact]
    public void A_second_slay_condemns_the_traitor_still_standing_rather_than_calling_another()
    {
        var (play, _, pharaoh) = Solo(ActFour.PharaohEnemyId, "royal_decree", Many(Deed), (ActFour.TwoLandsNameId, 1));
        var slays = 0;
        for (var turn = 0; turn < 40 && slays < 2; turn++)
        {
            if (CommandOn(Body(play, pharaoh)) == "slay")
                slays++;
            if (slays < 2)
                play.CombatDriver!.EndTurn();
        }
        Assert.Equal(2, slays);
        Assert.Single(Wearing(play, ActFour.TraitorId));
        Assert.Single(Wearing(play, ActFour.TraitorCondemnedId));
        play.Dispose();
    }

    // ── the Weigher ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_deed_tips_the_scale_and_the_crocodile_bites_on_his_turn()
    {
        var (play, session, weigher) = Solo(ActFour.WeigherEnemyId, "read_the_heart", Many(Deed));
        Play(play, session, Deed, weigher);
        Play(play, session, Deed, weigher);
        Assert.Equal(2, Stacks(Body(play, weigher), ActFour.TowardHeartId));
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        // 2 steps of 6 from the crocodile, then Read the Heart (19)
        Assert.Equal(before - 2 * ActFour.BitePerStep - 19, Hero(play).Health.Current);
        // the scale stays where you left it
        Assert.Equal(2, Stacks(Body(play, weigher), ActFour.TowardHeartId));
        play.Dispose();
    }

    [Fact]
    public void A_level_scale_is_a_feather_and_a_working_brings_it_back()
    {
        var (play, session, weigher) = Solo(ActFour.WeigherEnemyId, "read_the_heart", [Deed, Working, Deed, Working, Deed]);
        Play(play, session, Deed, weigher);
        Play(play, session, Working, null);
        Assert.Equal(0, Stacks(Body(play, weigher), ActFour.TowardHeartId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFour.ScaleFeatherId));
        play.Dispose();
    }

    [Fact]
    public void From_half_every_card_moves_the_scale_twice()
    {
        var (play, session, weigher) = Solo(ActFour.WeigherEnemyId, "read_the_heart", Many(Working), (ActFour.HeartRemembersId, 1));
        Play(play, session, Working, null);
        Assert.Equal(2, Stacks(Body(play, weigher), ActFour.TowardFeatherId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(2 * ActFour.BlockPerStep, Block(Body(play, weigher)));
        play.Dispose();
    }

    // ── the Architect ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void He_builds_two_courses_a_round_and_a_heavy_hit_tears_one_down()
    {
        var (play, session, architect) = FightProbe.Start(
            FightProbe.SoloAgainstHero(ActFour.ArchitectEnemyId, "stone_sledge", 9, ("strength", 6)), Many(Deed), health: 400);
        play.CombatDriver!.EndTurn();
        Assert.Equal(ActFour.CoursesPerRound, Stacks(Body(play, architect), ActFour.CourseId));
        Play(play, session, Deed, architect); // 6 + 6 Strength = 12
        Assert.Equal(ActFour.CoursesPerRound - 1, Stacks(Body(play, architect), ActFour.CourseId));
        play.Dispose();
    }

    [Fact]
    public void At_six_courses_the_capstone_comes_down_and_three_stand()
    {
        var (play, _, architect) = Solo(ActFour.ArchitectEnemyId, "raise_the_ramp", Many(Deed), (ActFour.CourseId, 6));
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(before - 36, Hero(play).Health.Current);
        Assert.Equal(1, Stacks(Hero(play), ActFour.EntombedId));
        // three stand after the capstone, and he builds two more as his turn ends
        Assert.Equal(ActFour.CoursesAfterCapstone + ActFour.CoursesPerRound, Stacks(Body(play, architect), ActFour.CourseId));
        play.Dispose();
    }

    // ── Renenutet ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Her_bounty_is_seven_hp_for_four_cards_and_a_grain()
    {
        var (play, session, ren) = Solo(ActFour.LadyEnemyId, "seal_the_granary", Many(Deed, 15));
        Assert.True(InHand(play, ActFour.BountyCardId));
        var hp = Hero(play).Health.Current;
        var hand = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, ActFour.BountyCardId, null);
        Assert.Equal(hp - ActFour.BountyPrice, Hero(play).Health.Current);
        Assert.Equal(hand - 1 + 4, play.CombatDriver.Current!.Hand.Count);
        Assert.Equal(1, Stacks(Body(play, ren), ActFour.GrainId));
        play.Dispose();
    }

    [Fact]
    public void At_seven_grain_she_harvests()
    {
        var (play, session, ren) = Solo(ActFour.LadyEnemyId, "seal_the_granary", Many(Deed, 15), (ActFour.GrainId, 6));
        Play(play, session, ActFour.BountyCardId, null);
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(before - 39, Hero(play).Health.Current);
        Assert.Equal(0, Stacks(Body(play, ren), ActFour.GrainId));
        play.Dispose();
    }

    [Fact]
    public void In_the_seventh_year_the_bounty_is_seven_cards_and_two_grain()
    {
        var (play, session, ren) = Solo(ActFour.LadyEnemyId, "seal_the_granary", Many(Deed, 20), (ActFour.SeventhYearId, 1));
        Assert.True(InHand(play, ActFour.SeventhBountyCardId));
        Play(play, session, ActFour.SeventhBountyCardId, null);
        Assert.Equal(2, Stacks(Body(play, ren), ActFour.GrainId));
        play.Dispose();
    }

    // ── the First Scribe ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Your_first_three_are_written_and_read_back()
    {
        var (play, session, scribe) = Solo(ActFour.ScribeEnemyId, "seal_the_scroll", [Deed, Working, Deed, Deed, Deed]);
        Play(play, session, Deed, scribe);
        Play(play, session, Working, null);
        Play(play, session, Deed, scribe);
        Play(play, session, Deed, scribe);
        var body = Body(play, scribe);
        Assert.Equal(1, Stacks(body, ActFour.EntryId(1, "deed")));
        Assert.Equal(1, Stacks(body, ActFour.EntryId(2, "working")));
        Assert.Equal(1, Stacks(body, ActFour.EntryId(3, "deed")));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        // your own 5 Block from the Working is still up when he reads
        Assert.Equal(before - (2 * ActFour.ReadDeed - 5), Hero(play).Health.Current);
        Assert.Equal(24 + ActFour.ReadWorking, Block(Body(play, scribe)));
        Assert.Equal(0, Stacks(Body(play, scribe), ActFour.EntryId(1, "deed")));
        play.Dispose();
    }

    [Fact]
    public void A_rite_erases_the_last_entry()
    {
        var (play, session, scribe) = Solo(ActFour.ScribeEnemyId, "seal_the_scroll", [Deed, Rite, Deed, Rite, Deed]);
        Play(play, session, Deed, scribe);
        Play(play, session, Rite, null);
        Assert.Equal(0, Stacks(Body(play, scribe), ActFour.EntryId(1, "deed")));
        play.Dispose();
    }

    [Fact]
    public void The_palimpsest_keeps_your_last_three()
    {
        var (play, session, scribe) = Solo(ActFour.ScribeEnemyId, "seal_the_scroll", [Deed, Working, Working, Deed, Deed],
            (ActFour.PalimpsestId, 1));
        Play(play, session, Deed, scribe);
        Play(play, session, Working, null);
        Play(play, session, Working, null);
        Play(play, session, Deed, scribe);
        var body = Body(play, scribe);
        Assert.Equal(1, Stacks(body, ActFour.EntryId(1, "working")));
        Assert.Equal(1, Stacks(body, ActFour.EntryId(2, "working")));
        Assert.Equal(1, Stacks(body, ActFour.EntryId(3, "deed")));
        play.Dispose();
    }

    // ── Selket ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Four_vessels_pour_on_you_in_turn()
    {
        var (play, _, selket) = Solo(ActFour.MotherEnemyId, "resin_over_the_mouth", Many(Deed));
        Assert.Equal(4, Wearing(play, ActFour.SelketVesselId).Count());
        Assert.Single(Wearing(play, ActFour.VesselId("weight")), v => Stacks(v, ActFour.PoursNextId) > 0);
        play.CombatDriver!.EndTurn();
        Assert.Equal(2, Stacks(Hero(play), ActFour.BurdenedId));
        Assert.Single(Wearing(play, ActFour.VesselId("venom")), v => Stacks(v, ActFour.PoursNextId) > 0);
        play.CombatDriver.EndTurn();
        Assert.True(Stacks(Hero(play), "poison") > 0);
        play.Dispose();
    }

    [Fact]
    public void From_half_she_is_wrapped_and_rises_once()
    {
        var (play, session, selket) = FightProbe.Start(
            FightProbe.Roster("selket", 9, (ActFour.MotherEnemyId, "resin_over_the_mouth", 20)), Many(Deed), health: 400);
        Play(play, session, Deed, selket);
        Play(play, session, Deed, selket);
        Assert.Equal(1, Stacks(Body(play, selket), ActFour.SelketWrappedId));
        Play(play, session, Deed, selket);
        Play(play, session, Deed, selket);
        var body = Body(play, selket);
        Assert.True(body.IsAlive);
        Assert.Equal(1, Stacks(body, ActFour.RisenDownedId));

        play.CombatDriver!.EndTurn();
        Assert.Equal(20 * ActFour.RisePercent / 100, Body(play, selket).Health.Current);
        Assert.Equal(0, Stacks(Body(play, selket), ActFour.RisenDownedId));
        play.Dispose();
    }

    // ── Anuket ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Energy_kept_lowers_the_nile_and_none_left_raises_it()
    {
        var (play, session, anuket) = FightProbe.Start(FightProbe.Solo(ActFour.QueenEnemyId, "close_the_sluice", 3), Many(Deed), health: 400);
        Assert.Equal(ActFour.NileStart, Stacks(Body(play, anuket), ActFour.NileLevelId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(2, Stacks(Body(play, anuket), ActFour.NileLevelId));
        Assert.Equal(1, Stacks(Body(play, anuket), "nile_low"));
        // Low Water: her 26 Block is 12 lower as your turn opens
        Assert.Equal(26 - ActFour.LowWaterBlock, Block(Body(play, anuket)));

        // the Ordered Flood of the first turn held 1 Energy, which arrives when the 3 run dry: spend it too
        for (var i = 0; i < 4; i++)
            Play(play, session, Deed, anuket);
        play.CombatDriver.EndTurn();
        Assert.Equal(3, Stacks(Body(play, anuket), ActFour.NileLevelId));
        play.Dispose();
    }

    [Fact]
    public void At_the_black_flood_the_river_takes()
    {
        // 3 + 2 is the top of the gauge; ending the turn on 0 Energy keeps it there, and her next action is the river
        var (play, session, anuket) = FightProbe.Start(
            FightProbe.Solo(ActFour.QueenEnemyId, "close_the_sluice", 3, (ActFour.NileLevelId, 2)), Many(Deed), health: 400);
        // the opening Ordered Flood holds 1 Energy: spend all 4
        for (var i = 0; i < 4; i++)
            Play(play, session, Deed, anuket);
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(before - 32, Hero(play).Health.Current);
        Assert.Equal(1, Stacks(Hero(play), ActFour.EntombedId));
        Assert.Equal(ActFour.NileStart, Stacks(Body(play, anuket), ActFour.NileLevelId));
        play.Dispose();
    }
}
