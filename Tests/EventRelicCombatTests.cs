using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Event relics are one-off prizes from a single named branch, and almost all of them are rules of the
// FIGHT. A rule that never fires breaks no other test, so each one is proved in a real combat here.
public class EventRelicCombatTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // "Start each combat with 6 Block."
    [Fact]
    public void The_threshold_ward_guards_the_door_as_the_fight_opens()
    {
        var (play, _, _) = WithRelic("threshold_ward", "paper_cut", "paper_cut", "paper_cut");

        Assert.Equal(6, Block(Hero(play)));
        play.Dispose();
    }

    // "One card in your draw pile costs 1 less this fight." The mark rides on the INSTANCE, so it is the card
    // itself that got cheaper, not the hand it happened to be in.
    [Fact]
    public void The_property_tag_marks_one_card_as_cheaper()
    {
        var (play, _, _) = WithRelic("unclaimed_property_tag", Enumerable.Repeat("paper_cut", 10).ToArray());

        var combat = play.CombatDriver!.Current!;
        var zones = combat.State.GetCardZones(combat.HeroId);
        var marked = Enum.GetValues<CardZone>()
            .SelectMany(zone => zones.GetCardsInZone(zone))
            .Count(card => card.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter) == -1);

        Assert.Equal(1, marked);
        play.Dispose();
    }

    // The bone folder does the same and also finds you a card, so the opening hand is one bigger.
    [Fact]
    public void The_bone_folder_finds_you_an_extra_card()
    {
        var (withRelic, _, _) = WithRelic("inherited_bone_folder", Enumerable.Repeat("paper_cut", 12).ToArray());
        var (plain, _, _) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), [.. Enumerable.Repeat("paper_cut", 12)]);

        Assert.Equal(plain.CombatDriver!.Current!.Hand.Count + 1, withRelic.CombatDriver!.Current!.Hand.Count);
        withRelic.Dispose();
        plain.Dispose();
    }

    // "The first non-Junk card you play is copied into your hand." The hand shrinks by one when a card is
    // played and grows by one when the copy arrives, so it stays exactly the size it was.
    [Fact]
    public void The_originality_stamp_copies_the_first_card_you_play()
    {
        var (play, session, enemyId) = WithRelic("originality_stamp", Enumerable.Repeat("paper_cut", 10).ToArray());

        var before = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(before, play.CombatDriver.Current!.Hand.Count);

        // …and only the first: the second play leaves the hand one smaller.
        Play(play, session, "paper_cut", enemyId);
        Assert.Equal(before - 1, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Without_the_stamp_the_first_card_is_simply_played()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), [.. Enumerable.Repeat("paper_cut", 10)]);

        var before = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, "paper_cut", enemyId);

        Assert.Equal(before - 1, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    // "A card still in your hand at the end of your turn goes back on top of your draw pile; next turn you gain
    // 1 Energy and draw 1." Once — the ticket is spent the first time it is called.
    [Fact]
    public void The_uncalled_ticket_pays_out_on_the_turn_after_it_is_used()
    {
        var (withRelic, _, _) = WithRelic("uncalled_ticket", Enumerable.Repeat("paper_cut", 12).ToArray());
        var (plain, _, _) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), [.. Enumerable.Repeat("paper_cut", 12)]);

        withRelic.CombatDriver!.EndTurn();
        plain.CombatDriver!.EndTurn();

        // The ticket put a card back and then bought an extra draw, so the hand is one bigger than it would
        // otherwise have been, and the boon has removed itself again.
        Assert.Equal(plain.CombatDriver.Current!.Hand.Count + 1, withRelic.CombatDriver.Current!.Hand.Count);
        Assert.Equal(0, FightProbe.StacksOf(Hero(withRelic), "uncalled_ticket_boon"));
        withRelic.Dispose();
        plain.Dispose();
    }

    // Crossed-Out Map is the one Event relic that is a rule of the MAP, not of a fight: taking it hands over a
    // step that ignores the paths.
    [Fact]
    public void The_crossed_out_map_hands_over_a_step_off_the_paths()
    {
        var relic = EventRelics.All().Single(r => r.Id == "crossed_out_map");

        var step = Assert.Single(relic.Pickup!);
        Assert.IsType<GrantUnrestrictedStepRunEffect>(step);
        Assert.Null(relic.CombatRule);
    }

    // ── harness ────────────────────────────────────────────────────────────────

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

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
