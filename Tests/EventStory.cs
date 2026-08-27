using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// A tiny act made of one authored EVENT and the fights behind it, played through the real host path.
//
// An Act-I event is almost never finished when the player leaves the room: it writes a marking a fight has to
// honour, installs a rule the next fight opens with, or leaves a promise the run keeps once that fight is over.
// So testing one means walking the door AND the fight — and a run is rebuilt from its own answers under the
// replay model, which is why nothing here reaches into RunState to set things up.
internal sealed class EventStory : IDisposable
{
    // 11 HP, gnaws for 1 Paperwork a round: a fight two Paper Cuts end on the first turn.
    public const string Rat = "form_rat_a";
    public const string RatIntent = "gnaw_the_margins";

    // …and the archives' own quiet body, for the doors whose promise is about a real Act-II fight (a life the
    // Necrology Window lent out has to have somewhere to come back to).
    public const string Ouroboros = "dead_letter_ouroboros";
    public const string OuroborosIntent = "self_addressed_notice";

    public RunPlayback Play { get; }
    public InteractiveRunSession Session { get; }

    // The document this story is being played out of — what a resumed save has to be rebuilt against.
    public RunBlueprint Document { get; private init; } = null!;

    private EventStory(RunPlayback play)
    {
        Play = play;
        Session = play.Session!;
    }

    public RunState Run => Session.Run;
    public InteractiveCombat Fight =>
        Play.CombatDriver?.Current ?? throw new InvalidOperationException("no fight is on the table");

    // Stand in the doorway with the event's first situation on the table, having chosen nothing.
    public static EventStory AtTheDoor(
        string eventId, IReadOnlyList<string>? deck = null, int fights = 1,
        bool paying = false, int gold = 0, string intent = RatIntent, int? health = null,
        string enemy = Rat)
    {
        var document = Blueprint(eventId, fights, deck, paying, gold, intent, health, enemy);
        var play = new RunPlayback(() => { });
        play.Start(document, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var story = new EventStory(play) { Document = document };
        Assert.True(story.Session.IsAwaitingChoice, "the event's door did not open");
        return story;
    }

    // Walk into the event, take the named branch, read the outcome, then step to the first fight.
    public static EventStory Enter(
        string eventId, string choiceId, IReadOnlyList<string>? deck = null, int fights = 1,
        bool paying = false, int gold = 0, string intent = RatIntent, int? health = null,
        string enemy = Rat)
    {
        var story = AtTheDoor(eventId, deck, fights, paying, gold, intent, health, enemy);
        story.Session.Pick(choiceId);
        Assert.Null(story.Session.Error);
        story.Settle();
        return story;
    }

    // Resolve everything the run parks on — an interlude, a result screen, a reward pick, the next node — until
    // a fight is on the table or the run has nothing left to ask.
    public void Settle(int guard = 40)
    {
        for (var i = 0; i < guard && Play.CombatDriver?.Current is null; i++)
        {
            if (Session.IsAwaitingChoice)
                Session.Pick(Session.PendingChoices[0].Id);
            else if (Session.IsAwaitingEntities)
                // Take as many as the pick asks for, from the top — "upgrade 2 cards" is one request for two.
                Session.PickEntities([.. Enumerable.Range(0,
                    Math.Min(Session.PendingEntities!.Count, Session.PendingEntities.Displays.Count))]);
            else if (Session.IsAwaitingInterlude)
                Session.Continue();
            else if (Session.IsAwaitingNodeChoice)
                Session.PickNode(Session.PendingNodeChoices[0].Id.Value);
            else
                break;
            Assert.Null(Session.Error);
        }
    }

    // Walk the door, take the branch, and stop BETWEEN nodes — the one place a run may be written to disk.
    public static EventStory EnterAndPark(
        string eventId, string choiceId, IReadOnlyList<string>? deck = null)
    {
        var story = AtTheDoor(eventId, deck);
        story.Session.Pick(choiceId);
        Assert.Null(story.Session.Error);
        for (var i = 0; i < 20 && !story.Session.IsAwaitingInterlude; i++)
        {
            if (story.Session.IsAwaitingChoice)
                story.Session.Pick(story.Session.PendingChoices[0].Id);
            else if (story.Session.IsAwaitingEntities)
                story.Session.PickEntities([.. Enumerable.Range(0,
                    Math.Min(story.Session.PendingEntities!.Count,
                        story.Session.PendingEntities.Displays.Count))]);
            else
                break;
            Assert.Null(story.Session.Error);
        }
        Assert.True(story.Session.IsAwaitingInterlude, "the run should be parked between nodes");
        return story;
    }

    // Write the run out and read it back against the same document — what "Continue" does.
    public EventStory SaveAndResume()
    {
        var save = Play.SaveJson();
        Assert.True(save is not null, Play.Error);

        var resumed = new RunPlayback(() => { });
        resumed.Resume(Document, RunSaveJson.FromJson(save!), interactive: true);
        Assert.True(resumed.Error is null, resumed.Error);
        return new EventStory(resumed) { Document = Document };
    }

    // Hand the turn straight back, `turns` times — what a fight looks like when the point is a rule that only
    // speaks up on a later round.
    public void PassTurns(int turns)
    {
        for (var i = 0; i < turns && Play.CombatDriver?.Current is not null; i++)
        {
            Play.CombatDriver.EndTurn();
            Assert.Null(Session.Error);
        }
    }

    // Take the fight on the table apart with whatever is affordable, then settle whatever it paid out.
    public void WinTheFight()
    {
        Assert.NotNull(Play.CombatDriver?.Current);
        for (var turn = 0; turn < 20 && Play.CombatDriver!.Current is not null; turn++)
        {
            while (Play.CombatDriver.Current is { } current)
            {
                var hero = current.State.GetCombatant(current.HeroId);
                var energy = hero.Resources[StandardCombatIds.EnergyResource].Current;
                var playable = current.Hand.FirstOrDefault(c => CostOf(c.DefinitionId) <= energy && Attacks(c));
                var target = current.State.Combatants.FirstOrDefault(x => x.Id != current.HeroId && x.IsAlive);
                if (playable is null || target is null)
                    break;
                Play.CombatDriver.PlayCard(playable.Id, target.Id);
                Assert.Null(Session.Error);
            }
            if (Play.CombatDriver.Current is null)
                break;
            Play.CombatDriver.EndTurn();
            Assert.Null(Session.Error);
        }
        Assert.Null(Play.CombatDriver!.Current);
        Settle();
    }

    private static bool Attacks(CardInstance card) =>
        card.DefinitionId.value is "paper_cut" or "paper_cut+";

    public static int CostOf(CardDefinitionId definition) =>
        FightProbe.Game.Cards.FirstOrDefault(c => c.Id == definition.value)?.Costs
            .Where(c => c.ResourceId == StandardCombatIds.EnergyResource).Sum(c => c.Amount) ?? 0;

    // The per-instance price written on THIS copy (the stamp, the certified original) — added to the printed
    // cost and clamped at zero.
    public static int CostDeltaOf(CardInstance card) =>
        card.MarkCounters.TryGetValue(StandardCombatIds.CardCostDeltaCounter, out var delta) ? delta : 0;

    public IReadOnlyList<CardInstance> Zone(CardZone zone) =>
        Fight.State.GetCardZones(Fight.HeroId).GetCardsInZone(zone).ToList();

    public RunCardInstance Card(string definitionId) =>
        Run.Deck.First(c => c.DefinitionId.value == definitionId);

    public void Dispose() => Play.Dispose();

    // The act: the event's door, then `fights` identical fights behind it, each paying an ordinary purse so a
    // promise about "the Gold this fight pays" has something to be about.
    private static RunBlueprint Blueprint(
        string eventId, int fights, IReadOnlyList<string>? deck, bool paying, int gold, string intent,
        int? health, string enemy)
    {
        var probe = FightProbe.Solo(enemy, intent, energy: 3);
        var reward = paying
            ? new FixedRewardSource([new RewardOffer("spoils",
                [new ChangeResourceRunEffect(StandardRunIds.Gold, 30)])])
            : null;

        var nodes = new List<Node>
        {
            new(new NodeId("door"), StandardRunIds.EventNode, new EventRef(new EventId(eventId))),
        };
        var edges = new List<MapEdge>();
        for (var i = 1; i <= fights; i++)
        {
            // TAGGED as the ordinary fight it is: a promise that waits for "the next normal combat" reads the
            // node's role, and an untagged node would keep it waiting forever.
            nodes.Add(new Node(new NodeId($"fight{i}"), StandardRunIds.CombatNode,
                new EncounterRef(probe.Id, reward, new RewardId($"spoils{i}")),
                [MapNodeTags.Combat]));
            edges.Add(new MapEdge(new NodeId(i == 1 ? "door" : $"fight{i - 1}"), new NodeId($"fight{i}")));
        }

        var cards = (deck ?? ["paper_cut", "paper_cut", "paper_cut", "paper_cut"])
            .Select(id => new CardDefinitionId(id)).ToList();

        return FightProbe.Game with
        {
            Encounters = [probe],
            Map = new RunMap(nodes) { Edges = edges },
            MapGeneration = null,
            Acts = null,
            Deck = cards,
            Characters = [],
            Start = FightProbe.Game.Start with
            {
                Deck = cards,
                StartingHealth = health ?? FightProbe.Game.Start.StartingHealth,
                Resources = new Dictionary<string, int> { [StandardRunIds.Gold.Value] = gold },
            },
        };
    }
}
