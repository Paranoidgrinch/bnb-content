using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;
using Keywords = BnbContent.Converter.Cards.Keywords;

namespace BnbContent.Tests;

// The uncommons (§12): every card whose words carry a rule beyond a number, each test red without that rule.
public partial class WitchCardTests
{
    private static readonly CardDefinitionId AddAction = new(WitchActions.AddIngredient);
    private static readonly CardDefinitionId BrewAction = new(WitchActions.Brew);

    private static void Cook(RunPlayback play, InteractiveRunSession session, string card)
    {
        var driver = play.CombatDriver!;
        driver.UseAction(AddAction, null);
        Assert.Null(session.Error);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        driver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == card).Id]);
        Assert.Null(session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Pair(
        string[] deck, int firstHealth = 40, int secondHealth = 40, params (string Status, int Stacks)[] hero) =>
        FightProbe.Start(
            FightProbe.RosterAgainstHero("witch_pair", 9, hero,
                (Quiet, QuietIntent, firstHealth), ("municipal_gargoyle", "stonewall_ordinance", secondHealth)),
            deck, character: WitchCharacter.Id);

    private static CombatantState Other(RunPlayback play, CombatantId than) =>
        Now(play).State.Combatants.First(c => c.Id != than && c.Id != Now(play).HeroId);

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Against(
        string enemy, string intent, string[] deck, params (string Status, int Stacks)[] enemyStatuses) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, 9, enemyStatuses), deck, character: WitchCharacter.Id);

    [Fact]
    public void Thirty_five_uncommons_seven_a_family_two_thirds_from_act_one()
    {
        var uncommons = WitchCards.Uncommons();
        var bases = uncommons.Where(c => !c.Id.EndsWith('+')).ToList();
        Assert.Equal(35, bases.Count);
        Assert.Equal(70, uncommons.Count);
        Assert.All(bases, c => Assert.Equal("uncommon", c.Rarity));
        Assert.All(WitchActions.Families, f => Assert.Equal(7, bases.Count(c => c.Tags!.Contains(f))));
        Assert.Equal(23, bases.Count(c => c.Act == 1));
        Assert.Equal(12, bases.Count(c => c.Act == 2));
    }

    // ── Fang ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Badgers_temper_bites_back_at_an_enemy_about_to_attack()
    {
        var (play, session, enemy) = Fight(Of("badgers_temper"), Biter, BiteIntent);
        var before = Standing(play, enemy);
        Play(play, session, "badgers_temper", enemy);
        Assert.Equal(before - 11, Standing(play, enemy));
        play.Dispose();

        (play, session, enemy) = Fight(Of("badgers_temper"));
        before = Standing(play, enemy);
        Play(play, session, "badgers_temper", enemy);
        Assert.Equal(before - 7, Standing(play, enemy));
        play.Dispose();
    }

    [Fact]
    public void Adder_in_the_sleeve_grows_for_each_turn_it_waits()
    {
        var (play, session, enemy) = Fight(["adder_in_the_sleeve", "pot_lid", "pot_lid", "pot_lid", "pot_lid"]);
        EndTurn(play, session);
        EndTurn(play, session);
        Assert.Contains(Now(play).Hand, c => c.DefinitionId.value == "adder_in_the_sleeve");
        var before = Standing(play, enemy);
        Play(play, session, "adder_in_the_sleeve", enemy);
        Assert.Equal(before - 11, Standing(play, enemy));   // 5 + 2 turns × 3
        Assert.Equal(0, Hero(play).GetCounter(WitchRules.SleeveWaited));
        play.Dispose();
    }

    [Fact]
    public void Hawthorn_switch_moves_a_hexed_targets_count_on()
    {
        var (play, session, enemy) = Fight(["hawthorn_switch", "hawthorn_switch", "crooked_finger", "pot_lid", "pot_lid"]);
        Play(play, session, "hawthorn_switch", enemy);
        Assert.Equal(0, Body(play, enemy).GetCounter(WitchKeywords.ThreefoldStep));
        Play(play, session, "crooked_finger", enemy);
        Play(play, session, "hawthorn_switch", enemy);
        Assert.Equal(1, Body(play, enemy).GetCounter(WitchKeywords.ThreefoldStep));
        play.Dispose();
    }

    [Fact]
    public void Bite_the_hand_bites_harder_into_a_strengthened_enemy()
    {
        var (play, session, enemy) = Against(Quiet, QuietIntent, Of("bite_the_hand"), ("strength", 1));
        var before = Standing(play, enemy);
        Play(play, session, "bite_the_hand", enemy);
        Assert.Equal(before - 12, Standing(play, enemy));
        play.Dispose();

        (play, session, enemy) = Fight(Of("bite_the_hand"));
        before = Standing(play, enemy);
        Play(play, session, "bite_the_hand", enemy);
        Assert.Equal(before - 6, Standing(play, enemy));
        play.Dispose();
    }

    [Fact]
    public void Familiars_supper_makes_the_next_ingredient_free_when_it_kills()
    {
        var (play, session, enemy) = Pair(
            ["familiars_supper", "adders_nip", "pot_lid", "pot_lid", "pot_lid"], firstHealth: 5);
        Cook(play, session, "adders_nip");               // the turn's free one
        Assert.Equal(9, Now(play).HeroEnergy);
        Play(play, session, "familiars_supper", enemy);  // kills: the next is free again
        Assert.Equal(8, Now(play).HeroEnergy);
        Cook(play, session, "pot_lid");
        Assert.Equal(8, Now(play).HeroEnergy);
        play.Dispose();
    }

    [Fact]
    public void Hedge_thing_is_fiercer_while_the_cauldron_shelters()
    {
        var (play, session, enemy) = Fight(["hedge_thing", "hedge_thing", "pot_lid", "pot_lid", "pot_lid"]);
        var before = Standing(play, enemy);
        Play(play, session, "hedge_thing", enemy);
        Assert.Equal(before - 11, Standing(play, enemy));
        Cook(play, session, "pot_lid");
        before = Standing(play, enemy);
        Play(play, session, "hedge_thing", enemy);
        Assert.Equal(before - 7, Standing(play, enemy));
        play.Dispose();
    }

    // ── Hex ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Charm_backwards_puts_the_third_night_off_by_one()
    {
        var (play, session, enemy) = Fight(Of("charm_backwards"));
        Play(play, session, "charm_backwards", enemy);    // 8 Hexed, count −1
        var health = Body(play, enemy).Health.Current;
        for (var night = 1; night <= 3; night++)
        {
            EndTurn(play, session);
            Assert.Equal(health, Body(play, enemy).Health.Current);
        }
        EndTurn(play, session);
        Assert.Equal(health - 24, Body(play, enemy).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Dead_mans_hex_passes_half_its_hexed_on_when_the_target_dies()
    {
        var (play, session, enemy) = Pair(["dead_mans_hex", "adders_nip", "pot_lid", "pot_lid", "pot_lid"], firstHealth: 5);
        Play(play, session, "dead_mans_hex", enemy);
        Play(play, session, "adders_nip", enemy);
        Assert.Equal(2, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Sour_the_milk_worsens_another_affliction()
    {
        var (play, session, enemy) = Against(Quiet, QuietIntent, Of("sour_the_milk"), (Keywords.Lien, 3));
        Play(play, session, "sour_the_milk", enemy);
        Assert.Equal(4, FightProbe.StacksOf(Body(play, enemy), Keywords.Lien));
        Assert.Equal(4, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Nail_in_the_doorpost_hexes_a_second_enemy_once_a_turn()
    {
        var (play, session, enemy) = Pair(
            ["nail_in_the_doorpost", "crooked_finger", "crooked_finger", "pot_lid", "pot_lid"], secondHealth: 50);
        Play(play, session, "nail_in_the_doorpost", enemy);
        Play(play, session, "crooked_finger", enemy);
        Assert.Equal(2, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Hexed));
        Play(play, session, "crooked_finger", enemy);
        Assert.Equal(2, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Hexed));
        Assert.Equal(8, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Third_bell_spreads_hexed_when_its_target_bursts()
    {
        var (play, session, enemy) = Pair(Of("third_bell"));
        Play(play, session, "third_bell", enemy);         // 3 Hexed; no burst this turn — the bell is spent
        EndTurn(play, session);
        Assert.Equal(0, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Hexed));
        EndTurn(play, session);                           // its count stands at two
        Play(play, session, "third_bell", enemy);
        EndTurn(play, session);                           // the third night
        Assert.Equal(3, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Knotted_cord_counts_as_two_hex_in_a_mixed_brew()
    {
        var (play, session, enemy) = Fight(["knotted_cord", "adders_nip", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "knotted_cord");
        Cook(play, session, "adders_nip");
        Cook(play, session, "pot_lid");
        play.CombatDriver!.UseAction(BrewAction, enemy);
        Assert.Null(session.Error);
        Assert.Equal(4, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    // ── Husk ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Horn_spoon_adds_ward_wax_to_whatever_is_brewed()
    {
        var (play, session, enemy) = Fight(["horn_spoon", "adders_nip", "adders_nip", "pot_lid", "pot_lid"]);
        Cook(play, session, "horn_spoon");
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        play.CombatDriver!.UseAction(BrewAction, enemy);
        Assert.Null(session.Error);
        Assert.Equal(WitchActions.WaxRiderStacks, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    [Fact]
    public void Oakskin_and_clamp_the_lid_read_the_pot()
    {
        var (play, session, enemy) = Fight(["oakskin", "oakskin", "clamp_the_lid", "clamp_the_lid", "pot_lid"]);
        Play(play, session, "oakskin", enemy);
        Assert.Equal(6, Block(Hero(play)));
        Play(play, session, "clamp_the_lid", enemy);
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Cook(play, session, "pot_lid");
        Play(play, session, "oakskin", enemy);
        Assert.Equal(6 + 7 + 10, Block(Hero(play)));
        Play(play, session, "clamp_the_lid", enemy);
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    [Fact]
    public void Hedgehog_curl_and_hawthorn_wall_answer_an_attack()
    {
        var (play, session, enemy) = Fight(Of("hedgehog_curl"), Biter, BiteIntent);
        Play(play, session, "hedgehog_curl", enemy);
        var health = Body(play, enemy).Health.Current;
        EndTurn(play, session);
        Assert.Equal(health - 2, Body(play, enemy).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.HedgehogCurl));
        play.Dispose();

        (play, session, enemy) = Fight(Of("hawthorn_wall"), Biter, BiteIntent);
        Play(play, session, "hawthorn_wall", enemy);
        EndTurn(play, session);
        Assert.Equal(2, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Slough_off_draws_only_when_something_was_shed()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.SoloAgainstHero(Quiet, QuietIntent, 9, ("poison", 1)), Of("slough_off"), character: WitchCharacter.Id);
        Play(play, session, "slough_off", enemy);
        Assert.Equal(5, Now(play).Hand.Count);   // shed the poison: drew one back
        Play(play, session, "slough_off", enemy);
        Assert.Equal(4, Now(play).Hand.Count);   // nothing left to shed
        play.Dispose();
    }

    [Fact]
    public void Snails_patience_promises_block_only_early_in_the_turn()
    {
        var (play, session, enemy) = Fight(Of("snails_patience"));
        Play(play, session, "snails_patience", enemy);
        Assert.Equal(6, FightProbe.StacksOf(Hero(play), Keywords.PromisedBlock));
        Play(play, session, "snails_patience", enemy);
        Assert.Equal(12, FightProbe.StacksOf(Hero(play), Keywords.PromisedBlock));
        Play(play, session, "snails_patience", enemy);
        Assert.Equal(12, FightProbe.StacksOf(Hero(play), Keywords.PromisedBlock));
        play.Dispose();
    }

    // ── Hearth ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Proper_supper_heals_at_the_top_of_the_next_turn()
    {
        // 5 Block against the seal's 6: one HP gets through, and the supper gives it back.
        var (play, session, enemy) = Fight(Of("proper_supper"), Biter, BiteIntent);
        Play(play, session, "proper_supper", enemy);
        EndTurn(play, session);
        Assert.Equal(70, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), WitchRules.ProperSupper));
        play.Dispose();
    }

    [Fact]
    public void Taste_the_broth_takes_an_ingredient_back_and_does_not_mind_an_empty_pot()
    {
        var (play, session, enemy) = Fight(["taste_the_broth", "taste_the_broth", "adders_nip", "pot_lid", "pot_lid"]);
        Play(play, session, "taste_the_broth", enemy);   // empty pot: nothing to take
        Cook(play, session, "adders_nip");
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == "taste_the_broth").Id, enemy);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(play.CombatDriver.PendingCardChoice);
        play.CombatDriver.SupplyCardChoice([offered.Single().Id]);
        Assert.Null(session.Error);
        Assert.Empty(Now(play).State.GetCardZones(Now(play).HeroId).SetAside);
        Assert.Contains(Now(play).Hand, c => c.DefinitionId.value == "adders_nip");
        play.Dispose();
    }

    [Fact]
    public void Put_the_kettle_on_frees_the_next_ingredient()
    {
        var (play, session, enemy) = Fight(["put_the_kettle_on", "adders_nip", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "adders_nip");       // free
        Play(play, session, "put_the_kettle_on", enemy);
        Assert.Equal(8, Now(play).HeroEnergy);
        Cook(play, session, "pot_lid");          // free again
        Assert.Equal(8, Now(play).HeroEnergy);
        play.Dispose();
    }

    // ── Fortune ───────────────────────────────────────────────────────────────────────────────────────────

    // The roll is the fight's own dice, so two fights that differ only in a mark roll the same: 5 % fails here.
    [Fact]
    public void Cross_your_fingers_and_bad_penny_pay_out_when_the_roll_fails()
    {
        var (control, controlSession, controlEnemy) = Against(Biter, BiteIntent, Of("pot_lid"), (WitchKeywords.Misfortune, 1));
        EndTurn(control, controlSession);
        Assert.Equal(0, Body(control, controlEnemy).GetCounter(new CounterId("misfortune_landed")));
        var bitten = Hero(control).Health.Current;
        control.Dispose();

        var (play, session, enemy) = Against(Biter, BiteIntent, Of("pot_lid"),
            (WitchKeywords.Misfortune, 1), (WitchRules.CrossedFingers, 6), (WitchRules.BadPenny, 3));
        EndTurn(play, session);
        Assert.True(Hero(play).Health.Current > bitten, "the crossed fingers' Block took part of the blow");
        Assert.Equal(3, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, enemy), WitchRules.CrossedFingers));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, enemy), WitchRules.BadPenny));
        play.Dispose();
    }

    [Fact]
    public void Magpies_luck_pays_cards_only_when_the_roll_lands()
    {
        var (play, session, enemy) = Against(Biter, BiteIntent, Of("pot_lid"),
            (WitchKeywords.Misfortune, 12), (WitchRules.MagpiesLuck, 2));
        EndTurn(play, session);
        var landed = Body(play, enemy).GetCounter(new CounterId("misfortune_landed")) > 0;
        Assert.Equal(landed ? 7 : 5, Now(play).Hand.Count);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, enemy), WitchRules.MagpiesLuck));
        play.Dispose();
    }

    [Fact]
    public void Cast_the_knucklebones_lets_her_choose_the_sure_throw()
    {
        var (play, session, enemy) = Fight(Of("cast_the_knucklebones"));
        play.CombatDriver!.PlayCard(Now(play).Hand.First().Id, enemy);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);
        Assert.Equal(5, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.CombatDriver.PlayCard(Now(play).Hand.First().Id, enemy);
        play.CombatDriver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);
        Assert.InRange(FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune), 5, 17);
        play.Dispose();
    }

    [Fact]
    public void Horseshoe_over_the_door_adds_to_the_first_misfortune_each_turn()
    {
        var (play, session, enemy) = Fight(
            ["horseshoe_over_the_door", "crooked_horseshoe", "crooked_horseshoe", "pot_lid", "pot_lid"]);
        Play(play, session, "horseshoe_over_the_door", enemy);
        Play(play, session, "crooked_horseshoe", enemy);
        Assert.Equal(8, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Play(play, session, "crooked_horseshoe", enemy);
        Assert.Equal(14, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.Dispose();
    }

    [Fact]
    public void Broken_mirror_leaves_a_shard_in_the_deck()
    {
        var (play, session, enemy) = Fight(Of("broken_mirror"));
        Play(play, session, "broken_mirror", enemy);
        Assert.Equal(10, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Assert.Contains(Now(play).State.GetCardZones(Now(play).HeroId).DrawPile,
            c => c.DefinitionId.value == WitchCards.MirrorShardId);
        play.Dispose();
    }

    [Fact]
    public void Seven_magpies_flock_onto_a_lone_enemy()
    {
        var (play, session, enemy) = Fight(Of("seven_magpies"));
        Play(play, session, "seven_magpies", enemy);
        Assert.Equal(6, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.Dispose();

        (play, session, enemy) = Pair(Of("seven_magpies"));
        Play(play, session, "seven_magpies", enemy);
        Assert.Equal(3, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        Assert.Equal(3, FightProbe.StacksOf(Other(play, enemy), WitchKeywords.Misfortune));
        play.Dispose();
    }
}
