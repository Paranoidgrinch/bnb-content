using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// A run held still for a relic test: the whole authored game with its map replaced by a short straight road the test
// names — "shop", "rest", "event:<id>", "fight" — the relics worn from the start, and the hero standing on the given
// health. The test answers every question itself, so it can say exactly what was bought, rested or chosen.
internal sealed class RunBench : IDisposable
{
    public required RunPlayback Play { get; init; }
    public required InteractiveRunSession Session { get; init; }

    public RunState Run => Session.Run;
    public int Gold => Run.GetResource(StandardRunIds.Gold);
    public int Health => Run.Health.Current;
    public int MaxHealth => Run.Health.Max;
    public int Upgraded => Run.Deck.Count(c => c.UpgradeLevel > 0 || c.DefinitionId.value.EndsWith('+'));
    public string? Node => Run.CurrentNodeId?.Value;

    public void Dispose() => Play.Dispose();

    internal const string Fight = "fight";
    internal const string Shop = "shop";
    internal const string Rest = "rest";

    internal static RunBench Start(string[] road, string[]? relics = null, int? health = null, string[]? deck = null,
        string? character = null, EncounterDefinition? fight = null, int gold = 0)
    {
        fight ??= FightProbe.Solo(LieutenantTests.Quiet, LieutenantTests.QuietIntent, energy: 3);
        var ring = BnbContent.Converter.Playtest.SparringRing.OneFight(FightProbe.Game, fight,
            deck ?? [.. Enumerable.Repeat("paper_cut", 10)], health, relics: relics, character: character, gold: gold);
        var nodes = road.Select((stop, i) =>
        {
            var id = new NodeId($"stop{i}");
            return stop switch
            {
                Fight => new Node(id, StandardRunIds.CombatNode, new EncounterRef(fight.Id), [MapNodeTags.Combat]),
                Shop => new Node(id, StandardRunIds.ShopNode, new ShopRef(new ShopId("city-shop")), [MapNodeTags.Shop]),
                Rest => new Node(id, StandardRunIds.EventNode, new EventRef(new EventId("rest:city-waiting-room")), [MapNodeTags.Rest]),
                _ when stop.StartsWith("event:", StringComparison.Ordinal) =>
                    new Node(id, StandardRunIds.EventNode, new EventRef(new EventId(stop["event:".Length..])), [MapNodeTags.Event]),
                _ => throw new ArgumentException($"unknown stop '{stop}'"),
            };
        }).ToList();
        var blueprint = ring with { Map = new RunMap(nodes) };
        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        return new RunBench { Play = play, Session = play.Session! };
    }

    // Answer whatever is asked by the given policy until the run asks something the policy declines (returns false)
    // or the road ends. Interludes are always continued.
    public void Walk(Func<RunBench, bool> answer, int steps = 200)
    {
        for (var i = 0; i < steps && !Session.IsComplete; i++)
        {
            Assert.True(Session.Error is null, Session.Error);
            if (Session.IsAwaitingInterlude && Play.CombatDriver?.Current is null && !Session.IsAwaitingChoice && !Session.IsAwaitingEntities)
            {
                Session.Continue();
                continue;
            }
            if (!answer(this))
                return;
        }
        Assert.True(Session.Error is null, Session.Error);
    }

    public IReadOnlyList<string> Choices => Session.PendingChoices.Select(c => c.Id).ToList();
    public void Pick(string id) => Session.Pick(id);
    public void PickFirst(Func<string, bool> which) => Session.Pick(Session.PendingChoices.First(c => which(c.Id)).Id);

    // Win the probe fight by playing every card at the first enemy until it is over.
    public void WinFight()
    {
        for (var turn = 0; turn < 30 && Play.CombatDriver!.Current is { } combat && !combat.IsOver; turn++)
        {
            foreach (var card in combat.Hand.ToList())
            {
                if (Play.CombatDriver.Current is not { IsOver: false } now)
                    break;
                var target = now.State.Combatants.FirstOrDefault(c => c.Id != now.HeroId && c.Health.Current > 0);
                if (target is null)
                    break;
                if (now.CanPlay(card.Id))
                    Play.CombatDriver.PlayCard(card.Id, target.Id);
                while (Play.CombatDriver.PendingOptionChoice is not null)
                    Play.CombatDriver.SupplyOptionChoice([0]);
                while (Play.CombatDriver.PendingCardChoice is { Count: > 0 } offered)
                    Play.CombatDriver.SupplyCardChoice([offered[0].Id]);
            }
            if (Play.CombatDriver.Current is { IsOver: false })
                Play.CombatDriver.EndTurn();
        }
        while (Session.IsAwaitingEntities)
            Session.PickEntities([]);
        Assert.True(Session.Error is null, Session.Error);
    }
}
