using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II's normal enemies, pulled tighter after the second trial (user, 2026-11-03): a collection is 7 damage instead
// of Paperwork, every citer collects what it is owed, the Fanged Alphabet bites harder once it has learned a price,
// and the Crabwise Shelf misfiles two.
public class ActTwoNormalBuffTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    // A citation left unanswered twice: the Entry collects at its next turn — 7 damage, and no Paperwork.
    [Fact]
    public void A_citer_collects_what_it_is_owed_as_seven_damage()
    {
        using var play = FightProbe.Start(FightProbe.Solo("second_person_entry", "cross_reference"),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400).Play;
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(400 - ActTwo.CollectionDamage, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "paperwork"));
    }

    // Two Paper Cuts in a row (both cost 1) teach the Alphabet a price, and its attack is 3 the harder.
    [Fact]
    public void The_alphabet_bites_harder_once_it_has_learned_a_price()
    {
        using var play = FightProbe.Start(FightProbe.Solo("fanged_alphabet", "learned_letter", 9),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400).Play;
        var alphabet = Enemy(play).Id;
        var before = Hero(play).Health.Current;
        play.CombatDriver!.PlayCard(play.CombatDriver.Current!.Hand[0].Id, alphabet);
        play.CombatDriver.PlayCard(play.CombatDriver.Current!.Hand[0].Id, alphabet);
        Assert.True(Enemy(play).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(ActTwo.LearnedBiteId)));
        var attack = FightProbe.Game.EnemyActions.First(a => a.Id == "fanged_alphabet.learned_letter").Intent.Label;
        var shown = int.Parse(System.Text.RegularExpressions.Regex.Match(attack, @"(\d+) dmg").Groups[1].Value);
        play.CombatDriver.EndTurn();
        Assert.Equal(before - (shown + 3), Hero(play).Health.Current);
    }

    // The marks are taken back the moment the cards are drawn, so the count is read off the action itself.
    [Fact]
    public void The_crabwise_shelf_misfiles_two()
    {
        var program = System.Text.Json.JsonSerializer.Serialize(
            FightProbe.Shipped.EnemyActions.First(a => a.Id == "crabwise_shelf.mis_shelve").Program,
            RogueDeck.Run.RunJson.CreateOptions());
        Assert.Contains("\"TakeFirst\":2", program.Replace(" ", ""), StringComparison.Ordinal);
    }
}
