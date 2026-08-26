using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// One archive fight, entered through a real event door — the shape every Act-II event-object test needs.
//
// An inscription or a one-fight marking is written on ONE CARD by the run and read inside the fight, and a run
// is rebuilt from its own answers: a tag put on `session.Run` from outside is written away again on the next
// replay. So the tag is always written by an actual event choice, and the fight behind it is the real one.
internal sealed class ArchiveProbe : IDisposable
{
    // 47 HP, and its quietest intent — the fights here are about the player's own cards.
    public const string Ouroboros = "dead_letter_ouroboros";
    public const string Quiet = "self_addressed_notice";

    public RunPlayback Play { get; }
    public InteractiveRunSession Session { get; }

    private ArchiveProbe(RunPlayback play)
    {
        Play = play;
        Session = play.Session!;
    }

    public RunState Run => Session.Run;
    public InteractiveCombat Fight => Play.CombatDriver!.Current!;
    public CombatantState Hero => Fight.State.GetCombatant(Fight.HeroId);
    public CombatantId Enemy => Fight.State.Combatants.First(c => c.Id != Fight.HeroId).Id;

    public IReadOnlyList<CardInstance> Zone(CardZone zone) =>
        Fight.State.GetCardZones(Fight.HeroId).GetCardsInZone(zone).ToList();

    public CardInstance InHand(string definitionId) =>
        Zone(CardZone.Hand).First(c => c.DefinitionId.value == definitionId);

    // A combatant that has never gained Block has no Block pool at all — "none" reads as 0, not as a fault.
    public int Block =>
        Hero.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    public int CostOf(CardInstance card) =>
        Math.Max(0, Printed(card.DefinitionId)
            + (card.MarkCounters.TryGetValue(StandardCombatIds.CardCostDeltaCounter, out var d) ? d : 0));

    public static int Printed(CardDefinitionId definition) =>
        FightProbe.Game.Cards.First(c => c.Id == definition.value).Costs
            .Where(c => c.ResourceId == StandardCombatIds.EnergyResource).Sum(c => c.Amount);

    public void EndTurn()
    {
        Play.CombatDriver!.EndTurn();
        Assert.Null(Session.Error);
    }

    public void Play_(CardInstance card)
    {
        Play.CombatDriver!.PlayCard(card.Id, Enemy);
        Assert.Null(Session.Error);
    }

    public int EnemyHealth => Fight.State.GetCombatant(Enemy).Health.Current;

    public void Dispose() => Play.Dispose();

    // Write `inscriptions` on the named cards through a real event choice, then walk into a fight the given
    // rules are already running in.
    public static ArchiveProbe Enter(
        IReadOnlyList<string> deck,
        IReadOnlyList<(string Card, string Tag)> inscriptions,
        IReadOnlyList<string> rules,
        int energy = 3,
        int? drawnPerTurn = null)
    {
        var opening = FightProbe.SoloAgainstHero(
            Ouroboros, Quiet, energy, [.. rules.Select(r => (r, 1))]);
        // A smaller opening hand is how a test puts a card in the DRAW pile on purpose — a rule that fetches
        // one has to have somewhere to fetch it from.
        var probe = drawnPerTurn is not { } drawn
            ? opening
            : new EncounterDefinition(opening.Id, opening.Enemies, opening.HeroResources,
                opening.HeroStartingStatuses, opening.HeroDisplayName, drawn, opening.TriggeredEffects);
        var cards = deck.Select(id => new CardDefinitionId(id)).ToList();

        var write = new EventChoice("write",
            [.. inscriptions.Select(i => (IRunEffectRequest)new TagCardsRunEffect(
                RunSelectors.DeckCards.OfKind(new CardDefinitionId(i.Card)).Take(1),
                new RunCardTagId(i.Tag), true))]);

        var blueprint = FightProbe.Game with
        {
            Encounters = [probe],
            MapGeneration = null,
            Acts = null,
            Characters = [],
            Deck = cards,
            Start = FightProbe.Game.Start with { Deck = cards },
            Events = new Dictionary<string, EventScript>
            {
                ["write"] = new("start",
                    [new EventSituation("start", "The archive takes down your particulars.", [write])]),
            },
            Map = new RunMap(
            [
                new Node(new NodeId("desk"), StandardRunIds.EventNode, new EventRef(new EventId("write"))),
                new Node(new NodeId("fight"), StandardRunIds.CombatNode, new EncounterRef(probe.Id)),
            ])
            {
                Edges = [new MapEdge(new NodeId("desk"), new NodeId("fight"))],
            },
        };

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var story = new ArchiveProbe(play);

        for (var guard = 0; guard < 20 && play.CombatDriver?.Current is null; guard++)
        {
            if (story.Session.IsAwaitingChoice)
                story.Session.Pick(story.Session.PendingChoices[0].Id);
            else if (story.Session.IsAwaitingEntities)
                story.Session.PickEntities([0]);
            else if (story.Session.IsAwaitingInterlude)
                story.Session.Continue();
            else if (story.Session.IsAwaitingNodeChoice)
                story.Session.PickNode(story.Session.PendingNodeChoices[0].Id.Value);
            else
                break;
            Assert.Null(story.Session.Error);
        }

        Assert.NotNull(play.CombatDriver?.Current);
        return story;
    }
}
