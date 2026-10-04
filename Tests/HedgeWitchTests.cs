using BnbContent.Converter.Witch;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Hedge Witch's rules in live fights (hedge_witch_master.md; plan W1–W3): the cauldron and its two actions,
// the recipes, Hexed's third night, Misfortune, Sheltering, and Hearth healing that stops at the HP the fight
// began on. Each test is about a RULE the canon states; the numbers are its BALANCE DRAFTS.
public class HedgeWitchTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero
    private const string Biter = "embossed_seal";
    private const string BiteIntent = "heavy_impression";

    private static readonly CardDefinitionId Add = new(WitchActions.AddIngredient);
    private static readonly CardDefinitionId Brew = new(WitchActions.Brew);

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Fight(
        string[] deck, string enemy = Quiet, string intent = QuietIntent, int energy = 9, int? health = null) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, energy: energy), deck, health, character: WitchCharacter.Id);

    private static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;
    private static CombatantState Hero(RunPlayback play) => Now(play).State.GetCombatant(Now(play).HeroId);
    private static CombatantState Body(RunPlayback play, CombatantId id) => Now(play).State.GetCombatant(id);
    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;
    private static IReadOnlyList<CardInstance> Pot(RunPlayback play) =>
        Now(play).State.GetCardZones(Now(play).HeroId).SetAside;

    private static void Play(RunPlayback play, InteractiveRunSession session, string card, CombatantId target)
    {
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == card).Id, target);
        Assert.Null(session.Error);
    }

    // "Into the pot": the card in hand of this kind.
    private static void Cook(RunPlayback play, InteractiveRunSession session, string card)
    {
        var driver = play.CombatDriver!;
        driver.UseAction(Add, null);
        Assert.Null(session.Error);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        driver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == card).Id]);
        Assert.Null(session.Error);
    }

    [Fact]
    public void She_starts_with_her_own_deck_her_two_actions_and_her_cauldron()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 3), character: WitchCharacter.Id);
        var combat = Now(play);
        Assert.Equal([WitchActions.AddIngredient, WitchActions.Brew], combat.Actions.Select(a => a.value));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), WitchKeywords.Cauldron));
        Assert.Equal(10, combat.State.GetCardZones(combat.HeroId).AllCards.Count);
        Assert.Equal(70, Hero(play).Health.Max);
        play.Dispose();

        // …and the Bureaucrat has none of it.
        var (bureaucrat, _, _) = FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 3));
        Assert.Empty(Now(bureaucrat).Actions);
        Assert.Equal(0, FightProbe.StacksOf(Hero(bureaucrat), WitchKeywords.Cauldron));
        bureaucrat.Dispose();
    }

    [Fact]
    public void The_first_ingredient_each_turn_is_free_the_next_costs_one_and_neither_is_a_card_played()
    {
        var (play, session, _) = Fight(["adders_nip", "adders_nip", "pot_lid", "crooked_finger", "nettle_tea"]);
        Assert.Equal(9, Now(play).HeroEnergy);

        Cook(play, session, "adders_nip");
        Assert.Equal(9, Now(play).HeroEnergy);
        Cook(play, session, "pot_lid");
        Assert.Equal(8, Now(play).HeroEnergy);
        Assert.Equal(["adders_nip", "pot_lid"], Pot(play).Select(c => c.DefinitionId.value));
        Assert.Equal(3, Now(play).Hand.Count);
        Assert.Equal(0, Now(play).State.GetCardPlayTurnStats(Now(play).HeroId).CardsPlayedThisTurn);
        play.Dispose();
    }

    [Fact]
    public void Three_of_one_family_brew_concentrated_any_other_mix_brews_what_each_gives()
    {
        // Fang ×3: Red Teeth, 18 damage — not 3 × 5.
        var (play, session, enemy) = Fight(["adders_nip", "adders_nip", "adders_nip", "pot_lid", "pot_lid"],
            health: 70);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        var before = Body(play, enemy).Health.Current + Block(Body(play, enemy));
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(before - 18, Body(play, enemy).Health.Current + Block(Body(play, enemy)));
        Assert.Empty(Pot(play));
        Assert.Equal(3, Now(play).State.GetCardZones(Now(play).HeroId).DiscardPile.Count);
        play.Dispose();

        // Fang + Hex + Husk: 5 damage, 2 Hexed, 4 Block.
        (play, session, enemy) = Fight(["adders_nip", "crooked_finger", "pot_lid", "nettle_tea", "nettle_tea"]);
        Cook(play, session, "adders_nip");
        Cook(play, session, "crooked_finger");
        Cook(play, session, "pot_lid");
        before = Body(play, enemy).Health.Current + Block(Body(play, enemy));
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Equal(before - 5, Body(play, enemy).Health.Current + Block(Body(play, enemy)));
        Assert.Equal(2, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        Assert.Equal(4, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Brewing_wants_three_in_the_pot()
    {
        var (play, session, enemy) = Fight(["adders_nip", "adders_nip", "pot_lid", "pot_lid", "nettle_tea"]);
        Cook(play, session, "adders_nip");
        Cook(play, session, "adders_nip");
        Assert.False(Now(play).CanUse(Brew, enemy));
        Cook(play, session, "pot_lid");
        Assert.True(Now(play).CanUse(Brew, enemy));
        Assert.False(Now(play).CanUse(Add));   // three is a full pot
        play.Dispose();
    }

    [Fact]
    public void Hexed_bursts_on_the_third_night_for_three_times_its_stacks_and_does_not_decay()
    {
        var (play, session, enemy) = Fight(Enumerable.Repeat("crooked_finger", 10).ToArray());
        Play(play, session, "crooked_finger", enemy); // 4 Hexed
        var health = Body(play, enemy).Health.Current;
        for (var night = 1; night <= 2; night++)
        {
            play.CombatDriver!.EndTurn();
            Assert.Null(session.Error);
            Assert.Equal(health, Body(play, enemy).Health.Current);
        }
        play.CombatDriver!.EndTurn();
        Assert.Equal(health - 12, Body(play, enemy).Health.Current);
        Assert.Equal(4, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Hexed));
        play.Dispose();
    }

    [Fact]
    public void Pot_lid_shelters_only_while_the_pot_is_empty()
    {
        var (play, session, enemy) = Fight(["pot_lid", "pot_lid", "adders_nip", "adders_nip", "adders_nip"]);
        Play(play, session, "pot_lid", enemy);
        Assert.Equal(8, Block(Hero(play)));     // 5 + 3 sheltering
        Cook(play, session, "adders_nip");
        Play(play, session, "pot_lid", enemy);
        Assert.Equal(13, Block(Hero(play)));    // + 5, the pot is brewing
        play.Dispose();
    }

    [Fact]
    public void Hearth_heals_only_what_this_fight_took()
    {
        // Begun hurt (60 of 70): Nettle Tea cannot heal what was lost before the fight.
        var (play, session, enemy) = Fight(Enumerable.Repeat("nettle_tea", 10).ToArray(), health: 60);
        Assert.Equal(60, Hero(play).Health.Current);
        Play(play, session, "nettle_tea", enemy);
        Assert.Equal(60, Hero(play).Health.Current);
        play.Dispose();

        // Bitten in the fight: it heals 1 of it.
        (play, session, enemy) = Fight(Enumerable.Repeat("nettle_tea", 10).ToArray(), Biter, BiteIntent, health: 60);
        play.CombatDriver!.EndTurn();
        var bitten = Hero(play).Health.Current;
        Assert.True(bitten < 60, "the seal struck");
        Play(play, session, "nettle_tea", enemy);
        Assert.Equal(bitten + 1, Hero(play).Health.Current);
        play.Dispose();
    }

    // §16.3: a fight's own cards do not go in the pot — the choice never offers them.
    [Fact]
    public void The_pot_will_not_take_a_fights_own_card()
    {
        var (play, session, _) = Fight(["acknowledge_service", "adders_nip", "pot_lid", "pot_lid", "pot_lid"]);
        Assert.Contains(Now(play).Hand, c => c.DefinitionId.value == "acknowledge_service");
        play.CombatDriver!.UseAction(Add, null);
        Assert.Null(session.Error);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(play.CombatDriver.PendingCardChoice);
        Assert.DoesNotContain(offered, c => c.DefinitionId.value == "acknowledge_service");
        play.Dispose();
    }

    // §5: rolled once before the next action and gone afterwards, whether it worked or not.
    [Fact]
    public void Misfortune_is_rolled_once_and_then_gone()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Solo(Biter, BiteIntent, energy: 3, (WitchKeywords.Misfortune, 6)), character: WitchCharacter.Id);
        Assert.Equal(6, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, enemy), WitchKeywords.Misfortune));
        play.Dispose();
    }
    // W6 (table E4): every general reward card is exactly one ingredient, its upgrade the same one — and the pot
    // cooks it as that family. Without the tag two Grave Liens and an Adder's Nip would be a Mixed brew (5
    // damage); with it they are three Fang, Red Teeth.
    [Fact]
    public void Every_general_card_is_one_ingredient_family_and_cooks_as_it()
    {
        var general = Converter.Cards.FinalCards.GeneralPool(5).Select(c => c.Id).ToHashSet();
        Assert.Equal(50, general.Count);
        Assert.Equal(general.Order(), WitchFamilies.General.Keys.Order());

        foreach (var card in FightProbe.Game.Cards.Where(c => general.Contains(c.Id.TrimEnd('+'))))
            Assert.Equal([WitchFamilies.General[card.Id.TrimEnd('+')]],
                card.Tags.Select(t => t.value).Where(WitchActions.Families.Contains));

        var (play, session, enemy) = Fight(["grave_lien", "grave_lien", "adders_nip", "pot_lid", "pot_lid"],
            health: 70);
        Cook(play, session, "grave_lien");
        Cook(play, session, "grave_lien");
        Cook(play, session, "adders_nip");
        var before = Body(play, enemy).Health.Current + Block(Body(play, enemy));
        play.CombatDriver!.UseAction(Brew, enemy);
        Assert.Null(session.Error);
        Assert.Equal(before - 18, Body(play, enemy).Health.Current + Block(Body(play, enemy)));
        play.Dispose();
    }
}
