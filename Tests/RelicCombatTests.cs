using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// A relic that changes a FIGHT does it by handing the player a hidden status when the fight opens. These
// tests prove the whole chain — a relic in the inventory really does put its rule into the combat, and the
// rule really fires — because a relic that quietly does nothing breaks no test on its own.
public class RelicCombatTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // Starts the probe fight with the relic already in the inventory.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(
        string relicId, params string[] deck)
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 9);
        var blueprint = FightProbe.OneFight(probe, deck.ToList());
        blueprint = blueprint with
        {
            Start = blueprint.Start with { StartingRelics = [.. blueprint.Start.StartingRelics, relicId] },
            Characters = [],
        };

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }

    // "At combat start gain 4 Block." The plainest possible proof that a relic's rule reaches the fight.
    [Fact]
    public void A_relic_hands_its_rule_to_the_fight_when_the_fight_opens()
    {
        var (play, _, _) = WithRelic("black_salt_charm", "paper_cut", "paper_cut", "paper_cut");

        Assert.Equal(4, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "black_salt_charm"));
        play.Dispose();
    }

    // Without the relic, nothing happens — so the Block above is the relic's doing and not the encounter's.
    [Fact]
    public void Without_the_relic_the_fight_is_unchanged()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), ["paper_cut", "paper_cut", "paper_cut"]);

        Assert.Equal(0, Block(Hero(play)));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "black_salt_charm"));
        play.Dispose();
    }

    // "Every fifth card played deals 6 damage to the weakest living enemy."
    [Fact]
    public void Five_notch_bead_strikes_on_the_fifth_card_and_not_before()
    {
        var (play, session, enemyId) = WithRelic("five_notch_bead", Enumerable.Repeat("cower_behind_a_desk", 12).ToArray());

        var health = play.CombatDriver!.Current!.State.GetCombatant(enemyId).Health.Current;
        for (var i = 0; i < 4; i++)
            Play(play, session, "cower_behind_a_desk", enemyId);
        Assert.Equal(health, play.CombatDriver.Current!.State.GetCombatant(enemyId).Health.Current);

        Play(play, session, "cower_behind_a_desk", enemyId);
        Assert.Equal(health - 6, play.CombatDriver.Current!.State.GetCombatant(enemyId).Health.Current);
        play.Dispose();
    }

    // "The first time each turn you apply a negative status to an enemy, deal 4 damage to it" — once, and
    // only for what lands on the other side.
    [Fact]
    public void Tarnished_bell_answers_the_first_filing_of_a_turn_only()
    {
        var (play, session, enemyId) = WithRelic("tarnished_bell", Enumerable.Repeat("cursed_addendum", 12).ToArray());

        var health = play.CombatDriver!.Current!.State.GetCombatant(enemyId).Health.Current;
        Play(play, session, "cursed_addendum", enemyId);   // 6 damage + 2 Paperwork → the bell adds 4
        Assert.Equal(health - 10, play.CombatDriver.Current!.State.GetCombatant(enemyId).Health.Current);

        Play(play, session, "cursed_addendum", enemyId);   // the same turn: no bell
        Assert.Equal(health - 16, play.CombatDriver.Current!.State.GetCombatant(enemyId).Health.Current);
        play.Dispose();
    }

    // "One card in your opening hand costs 1 less the first time you play it." The promise is written on the
    // CARD, so it survives being held and is spent by that copy alone.
    [Fact]
    public void Index_volvelle_writes_its_discount_onto_a_card()
    {
        var (play, session, enemyId) = WithRelic("index_volvelle", Enumerable.Repeat("paper_cut", 12).ToArray());

        var hand = play.CombatDriver!.Current!.Hand;
        var marked = hand.Count(c => c.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter) != 0);
        Assert.Equal(1, marked);

        var discounted = hand.First(c => c.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter) != 0);
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        play.CombatDriver.PlayCard(discounted.Id, enemyId);
        Assert.Null(session.Error);
        Assert.Equal(energy, Hero(play).Resources[StandardCombatIds.EnergyResource].Current); // 1 − 1
        play.Dispose();
    }

    // "The first time each turn you Archive a card, gain 5 Block and draw 1." The Archive keyword announces
    // itself with a status on the player, so this also proves the relic hears the player's own filings — and
    // that the once-a-turn latch holds when the second Archive follows in the same turn.
    [Fact]
    public void Archive_key_pays_for_the_first_archive_of_a_turn_only()
    {
        var (play, session, enemyId) = WithRelic("archive_key", Enumerable.Repeat("smudged_index", 12).ToArray());

        ArchiveOne(play, session, enemyId);          // 4 Block from the card + 5 from the relic
        Assert.Equal(9, Block(Hero(play)));

        ArchiveOne(play, session, enemyId);          // the same turn: the card's 4 Block alone
        Assert.Equal(13, Block(Hero(play)));
        play.Dispose();
    }

    private static void ArchiveOne(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        Play(play, session, "smudged_index", target);
        var offered = play.CombatDriver!.PendingCardChoice;
        Assert.NotNull(offered);
        play.CombatDriver.SupplyCardChoice([offered![0].Id]);
        Assert.Null(session.Error);
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
