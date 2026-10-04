using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;
using Keywords = BnbContent.Converter.Cards.Keywords;

namespace BnbContent.Tests;

// The rares (§13): the cards that bend her own rules. Each test is red without the rule it names.
public partial class WitchCardTests
{
    private static IReadOnlyList<CardInstance> PotOf(RunPlayback play) =>
        Now(play).State.GetCardZones(Now(play).HeroId).SetAside;

    private static void Brew(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        play.CombatDriver!.UseAction(BrewAction, target);
        Assert.Null(session.Error);
    }

    [Fact]
    public void Twenty_five_rares_five_a_family_spread_over_four_acts()
    {
        var bases = WitchCards.Rares().Where(c => !c.Id.EndsWith('+')).ToList();
        Assert.Equal(25, bases.Count);
        Assert.Equal(50, WitchCards.Rares().Count);
        Assert.All(bases, c => Assert.Equal("rare", c.Rarity));
        Assert.All(WitchActions.Families, f => Assert.Equal(5, bases.Count(c => c.Tags!.Contains(f))));
        Assert.Equal([8, 6, 6, 5], [.. Enumerable.Range(1, 4).Select(act => bases.Count(c => c.Act == act))]);
    }

    // ── Fang ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Wolf_at_the_door_comes_cheaper_for_every_turn_it_waits()
    {
        var (play, session, enemy) = Fight(["wolf_at_the_door", "pot_lid", "pot_lid", "pot_lid", "pot_lid"]);
        EndTurn(play, session);
        EndTurn(play, session);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), WitchRules.WolfWaits));
        Play(play, session, "wolf_at_the_door", enemy);
        Assert.Equal(9, Now(play).HeroEnergy);   // two turns waited: it cost nothing
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.WolfWaits));
        play.Dispose();
    }

    [Fact]
    public void Carrion_flight_moves_on_once_its_target_falls()
    {
        var (play, session, enemy) = Pair(Of("carrion_flight"), firstHealth: 5);
        var other = Other(play, enemy).Id;
        var before = Standing(play, other);
        Play(play, session, "carrion_flight", enemy);
        Assert.Equal(before - 12, Standing(play, other));   // two strikes fell the first, four go on
        play.Dispose();
    }

    [Fact]
    public void Teeth_in_the_dark_and_full_pot_count_the_pot()
    {
        var (play, session, enemy) = Fight(["teeth_in_the_dark", "full_pot", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "pot_lid");
        Cook(play, session, "pot_lid");
        var before = Standing(play, enemy);
        Play(play, session, "teeth_in_the_dark", enemy);
        Assert.Equal(before - 14, Standing(play, enemy));
        Cook(play, session, "pot_lid");
        Play(play, session, "full_pot", enemy);
        Assert.Equal(4 + 9 + 6, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Turnskin_turns_a_husk_ingredient_into_fang()
    {
        var (play, session, enemy) = Fight(["turnskin", "pot_lid", "adders_nip", "pot_lid", "pot_lid"]);
        Cook(play, session, "pot_lid");
        Cook(play, session, "adders_nip");
        Play(play, session, "turnskin", enemy);
        Assert.Equal(["adders_nip", "adders_nip"], PotOf(play).Select(c => c.DefinitionId.value));
        play.Dispose();
    }

    [Fact]
    public void The_hare_runs_last_strikes_again_after_the_burst()
    {
        // A seal that guards nothing, so the hare's second bite is not lost in Block, and stout enough to live.
        var (play, session, enemy) = FightProbe.Start(FightProbe.Roster("hare", 9, (Biter, BiteIntent, 80)),
            ["crooked_finger", "the_hare_runs_last", "pot_lid", "pot_lid", "pot_lid"], character: WitchCharacter.Id);
        Play(play, session, "crooked_finger", enemy);   // 4 Hexed
        EndTurn(play, session);
        EndTurn(play, session);                          // its count stands at two
        Assert.Contains(Now(play).Hand, c => c.DefinitionId.value == "the_hare_runs_last");
        Play(play, session, "the_hare_runs_last", enemy);
        var health = Body(play, enemy).Health.Current;
        EndTurn(play, session);
        Assert.Equal(health - 12 - 8, Body(play, enemy).Health.Current);
        play.Dispose();
    }

    // ── Hex ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Call_the_third_night_bursts_now_and_name_written_backwards_doubles()
    {
        var (play, session, enemy) = Fight(["crooked_finger", "call_the_third_night", "name_written_backwards", "pot_lid", "pot_lid"]);
        Play(play, session, "crooked_finger", enemy);
        Play(play, session, "name_written_backwards", enemy);
        Assert.Equal(8, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        var health = Body(play, enemy).Health.Current;
        Play(play, session, "call_the_third_night", enemy);
        Assert.Equal(health - 24, Body(play, enemy).Health.Current);
        Assert.Equal(0, Body(play, enemy).GetCounter(WitchKeywords.ThreefoldStep));
        play.Dispose();
    }

    [Fact]
    public void Hex_in_the_rafters_spreads_the_first_burst_and_bane_root_regrows_it()
    {
        var (play, session, enemy) = Pair(["hex_in_the_rafters", "bane_root", "crooked_finger", "pot_lid", "pot_lid"]);
        Play(play, session, "hex_in_the_rafters", enemy);
        Play(play, session, "bane_root", enemy);
        Play(play, session, "crooked_finger", enemy);   // 4 Hexed
        for (var night = 1; night <= 3; night++)
            EndTurn(play, session);
        Assert.Equal(3, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Hexed));
        Assert.Equal(6, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Last_breath_hex_passes_on_all_its_hexed_and_the_count()
    {
        // The dying one guards nothing, so the second turn's nip still kills it.
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Roster("last_breath", 9, (Biter, BiteIntent, 5), ("municipal_gargoyle", "stonewall_ordinance", 40)),
            ["last_breath_hex", "adders_nip", "pot_lid", "pot_lid", "pot_lid",
             "adders_nip", "adders_nip", "adders_nip", "adders_nip", "adders_nip"], character: WitchCharacter.Id);
        Play(play, session, "last_breath_hex", enemy);
        EndTurn(play, session);                          // its count: 1
        Play(play, session, "adders_nip", enemy);
        var heir = Other(play, enemy);
        Assert.Equal(6, FightProbe.StacksOf(heir, WitchKeywords.Hexed));
        Assert.Equal(1, heir.GetCounter(WitchKeywords.ThreefoldStep));
        play.Dispose();
    }

    // ── Husk ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Shut_the_lid_shelters_a_full_pot_and_closes_it()
    {
        var (play, session, enemy) = Fight(["shut_the_lid", "adders_nip", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "adders_nip");
        Play(play, session, "shut_the_lid", enemy);
        Assert.False(Now(play).CanUse(AddAction));
        Play(play, session, "pot_lid", enemy);
        Assert.Equal(8 + 8, Block(Hero(play)));          // sheltering, though the pot holds a card
        play.Dispose();
    }

    [Fact]
    public void Scar_bark_answers_what_the_last_enemy_turn_took()
    {
        var (play, session, enemy) = Fight(Of("scar_bark"), Biter, BiteIntent);
        EndTurn(play, session);                           // bitten for 6
        Play(play, session, "scar_bark", enemy);
        Assert.Equal(5 + 6, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Winter_bark_keeps_unused_block_as_ward_wax_and_ironwood_pays_for_a_blow()
    {
        var (play, session, enemy) = Fight(Of("winter_bark"));
        Play(play, session, "winter_bark", enemy);
        EndTurn(play, session);
        Assert.Equal(6, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();

        (play, session, enemy) = Fight(Of("ironwood"), Biter, BiteIntent);
        Play(play, session, "ironwood", enemy);
        EndTurn(play, session);
        Assert.Equal(4 - 2, FightProbe.StacksOf(Hero(play), Keywords.WardWax));   // the blow got through: wax decays 2
        play.Dispose();
    }

    // ── Hearth ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Grannys_physic_treats_every_affliction()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.SoloAgainstHero(Quiet, QuietIntent, 9, ("poison", 3), ("panic", 1)), Of("grannys_physic"),
            character: WitchCharacter.Id);
        Play(play, session, "grannys_physic", enemy);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "poison"));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "panic"));
        play.Dispose();
    }

    [Fact]
    public void Stone_soup_turns_junk_into_the_named_family_and_cooks_it()
    {
        var (play, session, enemy) = Fight(["stone_soup", WitchCards.MirrorShardId, "pot_lid", "pot_lid", "pot_lid"]);
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "stone_soup").Id, enemy);
        play.CombatDriver.SupplyOptionChoice([1]);   // Hex
        Assert.Null(session.Error);
        Assert.Equal([WitchCards.SoupStoneId(WitchActions.Hex)], PotOf(play).Select(c => c.DefinitionId.value));
        play.Dispose();
    }

    [Fact]
    public void Keep_the_drippings_turns_wasted_healing_into_ward_wax()
    {
        var (play, session, enemy) = Fight(["keep_the_drippings", "mugwort_poultice", "pot_lid", "pot_lid", "pot_lid"]);
        Play(play, session, "keep_the_drippings", enemy);
        Play(play, session, "mugwort_poultice", enemy);   // 6 to heal, nothing lost: all 6 wasted
        Assert.Equal(6, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    [Fact]
    public void Never_wash_the_pot_keeps_one_ingredient_back()
    {
        var (play, session, enemy) = Fight(["never_wash_the_pot", "adders_nip", "adders_nip", "pot_lid", "pot_lid"]);
        Play(play, session, "never_wash_the_pot", enemy);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "pot_lid");
        play.CombatDriver!.UseAction(BrewAction, enemy);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(play.CombatDriver.PendingCardChoice);
        play.CombatDriver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == "pot_lid").Id]);
        Assert.Null(session.Error);
        Assert.Equal(["pot_lid"], PotOf(play).Select(c => c.DefinitionId.value));
        play.Dispose();
    }

    [Fact]
    public void For_what_ails_you_does_what_she_picks()
    {
        var (play, session, enemy) = Fight(Of("for_what_ails_you"));
        play.CombatDriver!.PlayCard(Now(play).Hand.First().Id, enemy);
        play.CombatDriver.SupplyOptionChoice([2]);   // draw 2
        Assert.Null(session.Error);
        Assert.Equal(6, Now(play).Hand.Count);
        play.Dispose();
    }

    // ── Fortune ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_black_cat_sat_down_spends_the_action_and_ninth_life_leaves_misfortune_behind()
    {
        var (play, session, enemy) = Fight(["the_black_cat_sat_down", "ninth_life", "pot_lid", "pot_lid", "pot_lid"],
            Biter, BiteIntent);
        Play(play, session, "ninth_life", enemy);
        Play(play, session, "the_black_cat_sat_down", enemy);
        Assert.Equal(66, Hero(play).Health.Current);        // the cat's price
        EndTurn(play, session);
        Assert.Equal(66, Hero(play).Health.Current);        // the seal's blow never came
        Assert.Equal(4, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Assert.Equal(2, Now(play).State.GetCardZones(Now(play).HeroId).AllCards
            .Count(c => c.DefinitionId.value == WitchCards.MirrorShardId));
        play.Dispose();
    }

    [Fact]
    public void Seven_years_leaves_half_behind_until_it_lands_and_loaded_bones_are_spent()
    {
        var (play, session, enemy) = Against(Biter, BiteIntent, Of("pot_lid"),
            (WitchKeywords.Misfortune, 1), (WitchRules.SevenYears, 1), (WitchRules.Loaded, 1));
        EndTurn(play, session);
        var landed = Body(play, enemy).GetCounter(new CounterId("misfortune_landed")) > 0;
        Assert.Equal(landed ? 0 : 1, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Assert.Equal(landed ? 0 : 1, FightProbe.StacksOf(Body(play, enemy), WitchRules.SevenYears));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, enemy), WitchRules.Loaded));
        play.Dispose();
    }

    [Fact]
    public void Borrowed_luck_cashes_misfortune_out()
    {
        var (play, session, enemy) = Against(Quiet, QuietIntent, Of("borrowed_luck"), (WitchKeywords.Misfortune, 8));
        Play(play, session, "borrowed_luck", enemy);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Assert.Equal(16, Block(Hero(play)));
        Assert.Equal(6, Now(play).Hand.Count);   // 5 − 1 + 2
        play.Dispose();
    }
}
