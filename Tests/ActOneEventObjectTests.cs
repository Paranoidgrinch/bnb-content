using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// The shared Act-I event objects (BnB_Final_Events_Master_PostAudit.md). Fifteen events are written out of
// this vocabulary, so what it does has to be true before any of them is.
public class ActOneEventObjectTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    [Fact]
    public void The_five_temporary_cards_ship_and_three_of_them_bite_at_the_end_of_the_turn()
    {
        var compiled = ActOneEventObjects.Compile().ToDictionary(c => c.Id);

        Assert.Equal(5, compiled.Count);
        foreach (var id in new[] { "missing_signature", "notice_of_delay", "summons_to_appear" })
            Assert.True(compiled[id].LifecyclePrograms.ContainsKey(CardLifecycleTrigger.TurnEndInHand),
                $"'{id}' should do something if it is left in hand");
        // The other two act while they are held, not when the turn ends.
        Assert.Empty(compiled["fine_print"].LifecyclePrograms);
        Assert.Empty(compiled["wrong_form"].LifecyclePrograms);
    }

    // The point of the marking machinery: a card the RUN marked is dealt into the fight still marked, and the
    // marking rule then puts it where the marking says.
    [Fact]
    public void A_misfiled_card_starts_the_next_fight_in_the_discard_pile()
    {
        var (play, session) = FightWithMarkedCard(ActOneEventObjects.Misfiled);
        var combat = play.CombatDriver!.Current!;
        var zones = combat.State.GetCardZones(combat.HeroId);
        // The run really did write the marking, and the fight really did read it.
        Assert.All(session.Run.Deck.Where(c => c.DefinitionId.value == "permit_a38"),
            c => Assert.Contains(new RunCardTagId(ActOneEventObjects.Misfiled), c.Tags));

        Assert.Contains(zones.GetCardsInZone(CardZone.DiscardPile),
            card => card.Marks.Contains(new TagId(ActOneEventObjects.Misfiled)));
        Assert.DoesNotContain(combat.Hand, card => card.DefinitionId.value == "permit_a38");
        play.Dispose();
    }

    [Fact]
    public void A_fast_tracked_card_is_in_the_opening_hand()
    {
        var (play, _) = FightWithMarkedCard(ActOneEventObjects.FastTrack);

        Assert.Contains(play.CombatDriver!.Current!.Hand, card => card.DefinitionId.value == "permit_a38");
        play.Dispose();
    }

    [Fact]
    public void A_sealed_card_starts_outside_the_deck()
    {
        var (play, _) = FightWithMarkedCard(ActOneEventObjects.Sealed);
        var combat = play.CombatDriver!.Current!;
        var zones = combat.State.GetCardZones(combat.HeroId);

        Assert.Contains(zones.GetCardsInZone(CardZone.BanishedPile),
            card => card.DefinitionId.value == "permit_a38");
        play.Dispose();
    }

    // An unmarked deck plays exactly as it always did — the rule is inert when nothing carries a marking.
    [Fact]
    public void Without_a_marking_nothing_moves()
    {
        var (play, _) = FightWithMarkedCard(marking: null);
        var combat = play.CombatDriver!.Current!;
        var zones = combat.State.GetCardZones(combat.HeroId);

        Assert.Empty(zones.GetCardsInZone(CardZone.DiscardPile));
        Assert.Empty(zones.GetCardsInZone(CardZone.BanishedPile));
        play.Dispose();
    }

    // An event that writes the marking, and then the fight that has to honour it — the real path, because a
    // run under the replay model is rebuilt from its own answers: a card tagged from outside is tagged away
    // again on the next replay.
    private static (RunPlayback Play, InteractiveRunSession Session) FightWithMarkedCard(string? marking)
    {
        var probe = FightProbe.SoloAgainstHero(
            Quiet, QuietIntent, energy: 3, (ActOneEventObjects.MarkingsRule.Id, 1));
        var one = FightProbe.OneFight(probe, ["permit_a38", "paper_cut", "paper_cut", "paper_cut"]);

        var mark = marking is null
            ? new EventChoice("go", [])
            : new EventChoice("go",
            [
                new TagCardsRunEffect(
                    RunSelectors.DeckCards.OfKind(new CardDefinitionId("permit_a38")),
                    new RunCardTagId(marking), true),
            ]);

        var blueprint = one with
        {
            Characters = [],
            Events = new Dictionary<string, EventScript>
            {
                ["mark"] = new("start", [new EventSituation("start", "The clerk takes your permit.", [mark])]),
            },
            Map = new RunMap(
            [
                new Node(new NodeId("event"), StandardRunIds.EventNode, new EventRef(new EventId("mark"))),
                new Node(new NodeId("probe"), StandardRunIds.CombatNode, new EncounterRef(probe.Id)),
            ])
            {
                Edges = [new MapEdge(new NodeId("event"), new NodeId("probe"))],
            },
        };

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var session = play.Session!;

        for (var guard = 0; guard < 20 && play.CombatDriver?.Current is null; guard++)
        {
            if (session.IsAwaitingChoice)
                session.Pick(session.PendingSituation!.Choices[0].Id);
            else if (session.IsAwaitingNodeChoice)
                session.PickNode(session.PendingNodeChoices[0].Id.Value);
            else if (session.IsAwaitingInterlude)
                session.Continue();
            else
                break;
            Assert.Null(session.Error);
        }

        Assert.NotNull(play.CombatDriver?.Current);
        return (play, session);
    }
}
