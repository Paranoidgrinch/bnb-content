using BnbContent.Converter.Relics;
using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;
using Keywords = BnbContent.Converter.Cards.Keywords;

namespace BnbContent.Tests;

// The Hedge Witch's relics (hedge_witch_master.md §14; plan W5): each relic's rule in a live fight, red without it.
public class WitchRelicTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string Biter = "embossed_seal";
    private const string BiteIntent = "heavy_impression";   // 6 damage

    private static readonly CardDefinitionId Add = new(WitchActions.AddIngredient);
    private static readonly CardDefinitionId Brew = new(WitchActions.Brew);

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Fight(
        string relic, string[] deck, string enemy = Quiet, string intent = QuietIntent, int? health = null,
        params (string Status, int Stacks)[] enemyStatuses) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, 9, enemyStatuses), deck, health,
            character: WitchCharacter.Id, relics: [relic]);

    private static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;
    private static CombatantState Hero(RunPlayback play) => Now(play).State.GetCombatant(Now(play).HeroId);
    private static CombatantState Body(RunPlayback play, CombatantId id) => Now(play).State.GetCombatant(id);
    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;
    private static int Standing(RunPlayback play, CombatantId id) => Body(play, id).Health.Current + Block(Body(play, id));
    private static IReadOnlyList<CardInstance> Pot(RunPlayback play) => Now(play).State.GetCardZones(Now(play).HeroId).SetAside;

    private static void Play(RunPlayback play, InteractiveRunSession session, string card, CombatantId target)
    {
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == card).Id, target);
        Assert.Null(session.Error);
    }

    private static void Cook(RunPlayback play, InteractiveRunSession session, string card)
    {
        play.CombatDriver!.UseAction(Add, null);
        Assert.Null(session.Error);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(play.CombatDriver.PendingCardChoice);
        play.CombatDriver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == card).Id]);
        Assert.Null(session.Error);
    }

    private static void EndTurn(RunPlayback play, InteractiveRunSession session)
    {
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
    }

    [Fact]
    public void Eighteen_relics_hers_alone()
    {
        var relics = WitchRelics.All();
        Assert.Equal(18, relics.Count);
        Assert.All(relics, r => Assert.Equal(RelicAuthoring.Eligibility.HedgeWitch, r.Eligibility));
        Assert.Equal(3, relics.Count(r => r.Rarity == RelicAuthoring.Rarity.Common));
        Assert.Equal(5, relics.Count(r => r.Rarity == RelicAuthoring.Rarity.Uncommon));
        Assert.Equal(4, relics.Count(r => r.Rarity == RelicAuthoring.Rarity.Rare));
        Assert.Equal(6, relics.Count(r => r.Pool == RelicAuthoring.Pool.Shop));
        Assert.All(relics, r => Assert.Contains(r.Id, WitchCharacter.Exclusive));
        Assert.DoesNotContain(FinalRelics.Pool(RelicAuthoring.Pool.Normal), r => relics.Contains(r));
        Assert.All(relics, r => Assert.Contains(FightProbe.Game.Relics, shipped => shipped.Id == r.Id));
    }

    [Fact]
    public void Rowan_pin_pays_for_the_first_card_into_the_pot_each_turn()
    {
        var (play, session, _) = Fight("rowan_pin", ["adders_nip", "adders_nip", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "adders_nip");
        Assert.Equal(WitchRules.RowanPinBlock, Block(Hero(play)));
        Cook(play, session, "adders_nip");
        Assert.Equal(WitchRules.RowanPinBlock, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Three_knot_cord_leaves_hexed_after_the_first_burst()
    {
        var (play, session, enemy) = Fight("three_knot_cord", Enumerable.Repeat("crooked_finger", 10).ToArray());
        Play(play, session, "crooked_finger", enemy);
        for (var night = 1; night <= 3; night++)
            EndTurn(play, session);
        Assert.Equal(4 + WitchRules.ThreeKnotHexed, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Found_button_and_knucklebone_pair_answer_a_lost_roll()
    {
        var (play, session, enemy) = Fight("found_button", Enumerable.Repeat("pot_lid", 10).ToArray(), Biter, BiteIntent,
            enemyStatuses: (WitchKeywords.Misfortune, 1));
        EndTurn(play, session);
        Assert.Equal(70 - (6 - WitchRules.FoundButtonBlock), Hero(play).Health.Current);
        play.Dispose();

        (play, session, enemy) = Fight("knucklebone_pair", Enumerable.Repeat("pot_lid", 10).ToArray(), Biter, BiteIntent,
            enemyStatuses: (WitchKeywords.Misfortune, 1));
        EndTurn(play, session);
        Assert.Equal(WitchRules.KnucklebonePairStacks, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.Dispose();
    }

    [Fact]
    public void Greasy_ladle_draws_on_the_first_brew()
    {
        var (play, session, enemy) = Fight("greasy_ladle", [.. Enumerable.Repeat("adders_nip", 10)]);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(3, Now(play).Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Iron_trivet_guards_a_ready_pot_through_the_enemy_turn()
    {
        var (play, session, _) = Fight("iron_trivet", ["adders_nip", "adders_nip", "adders_nip", "nettle_tea", "nettle_tea"],
            Biter, BiteIntent);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        EndTurn(play, session);
        Assert.Equal(70, Hero(play).Health.Current);   // 6 Block against the seal's 6
        play.Dispose();
    }

    [Fact]
    public void Crows_toe_pecks_an_enemy_on_the_night_before_its_burst()
    {
        var (play, session, enemy) = FightProbe.Start(FightProbe.Roster("toe", 9, (Biter, BiteIntent, 80)),
            ["crooked_finger", "adders_nip", "pot_lid", "pot_lid", "pot_lid"], character: WitchCharacter.Id,
            relics: ["crows_toe"]);
        Play(play, session, "crooked_finger", enemy);
        EndTurn(play, session);
        EndTurn(play, session);   // its count stands at two
        var before = Standing(play, enemy);
        Play(play, session, "adders_nip", enemy);
        Assert.Equal(before - 6 - WitchRules.CrowsToeDamage, Standing(play, enemy));
        play.Dispose();
    }

    [Fact]
    public void Beeswax_cup_and_herb_wifes_rack_answer_healing()
    {
        var (play, session, enemy) = Fight("beeswax_cup", Enumerable.Repeat("nettle_tea", 10).ToArray(), Biter, BiteIntent);
        EndTurn(play, session);                               // bitten for 6
        Play(play, session, "nettle_tea", enemy);             // heals 1
        Assert.Equal(WitchRules.BeeswaxWax, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();

        (play, session, enemy) = Fight("herb_wifes_rack", ["nettle_tea", "adders_nip", "adders_nip", "pot_lid", "pot_lid"],
            Biter, BiteIntent);
        EndTurn(play, session);
        var bitten = Hero(play).Health.Current;
        Cook(play, session, "nettle_tea");
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(bitten + 1 + WitchRules.HerbWifeHeal, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void False_bottom_pot_keeps_a_fourth_card_out_of_the_brew_and_for_the_next()
    {
        var (play, session, enemy) = Fight("false_bottom_pot", ["adders_nip", "adders_nip", "adders_nip", "pot_lid", "pot_lid"]);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "pot_lid");
        Assert.Equal(4, Pot(play).Count);
        Assert.Contains(new TagId(WitchActions.ReserveMark), Pot(play)[^1].Marks);
        var before = Standing(play, enemy);
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(before - 18, Standing(play, enemy));   // three Fang: Red Teeth, the lid not counted
        Assert.Equal(["pot_lid"], Pot(play).Select(c => c.DefinitionId.value));
        play.Dispose();
    }

    [Fact]
    public void First_bell_starts_the_first_hexed_enemy_a_step_ahead()
    {
        var (play, session, enemy) = Fight("first_bell", Enumerable.Repeat("crooked_finger", 10).ToArray());
        Play(play, session, "crooked_finger", enemy);
        Assert.Equal(WitchRules.FirstBellSteps, Body(play, enemy).GetCounter(WitchKeywords.ThreefoldStep));
        play.Dispose();
    }

    [Fact]
    public void Black_cats_collar_rolls_the_first_lost_roll_again()
    {
        var (play, session, _) = Fight("black_cats_collar", Enumerable.Repeat("pot_lid", 10).ToArray(), Biter, BiteIntent,
            enemyStatuses: (WitchKeywords.Misfortune, 1));
        EndTurn(play, session);
        Assert.Equal(1, Hero(play).GetCounter(WitchRules.CollarLatch));
        play.Dispose();
    }

    [Fact]
    public void Grandams_ember_heals_once_past_the_fights_own_cap()
    {
        var (play, session, enemy) = Fight("grandams_ember", Enumerable.Repeat("mugwort_poultice", 10).ToArray(), health: 50);
        Play(play, session, "mugwort_poultice", enemy);
        Assert.Equal(56, Hero(play).Health.Current);
        Play(play, session, "mugwort_poultice", enemy);
        Assert.Equal(56, Hero(play).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Bone_strainer_lets_her_name_the_dregs()
    {
        var (play, session, enemy) = Fight("bone_strainer",
            [WitchCards.MirrorShardId, "adders_nip", "adders_nip", "pot_lid", "pot_lid"]);
        Cook(play, session, WitchCards.MirrorShardId);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        var before = Standing(play, enemy);
        play.CombatDriver!.UseAction(Brew, enemy);
        play.CombatDriver.SupplyOptionChoice([0]);   // Fang
        Assert.Null(session.Error);
        Assert.Equal(before - 15, Standing(play, enemy));   // three Fang in a MIXED brew, not Red Teeth
        play.Dispose();
    }

    [Fact]
    public void Apothecarys_scale_refunds_a_brew_of_three_families()
    {
        var (play, session, enemy) = Fight("apothecarys_scale", ["adders_nip", "crooked_finger", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "adders_nip");
        Cook(play, session, "crooked_finger");
        Cook(play, session, "pot_lid");
        var energy = Now(play).HeroEnergy;
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(energy, Now(play).HeroEnergy);
        play.Dispose();
    }

    [Fact]
    public void Yesterdays_jar_keeps_the_last_ingredient_after_the_first_brew()
    {
        var (play, session, enemy) = Fight("yesterdays_jar", ["adders_nip", "adders_nip", "pot_lid", "pot_lid", "pot_lid"]);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "pot_lid");
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(["pot_lid"], Pot(play).Select(c => c.DefinitionId.value));
        play.Dispose();
    }

    [Fact]
    public void Cats_eye_coin_counts_every_misfortune_that_lands()
    {
        var (play, session, enemy) = Fight("cats_eye_coin", ["the_black_cat_sat_down", "pot_lid", "pot_lid", "pot_lid", "pot_lid"],
            Biter, BiteIntent);
        Play(play, session, "the_black_cat_sat_down", enemy);
        EndTurn(play, session);
        Assert.Equal(1, Hero(play).GetCounter(WitchRules.CatsEyeTally));
        play.Dispose();
    }
}
