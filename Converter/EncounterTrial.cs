using System.Text.Json;
using BnbContent.Converter.Relics;
using RogueDeck.Run;

namespace BnbContent.Converter;

// ── THE ENCOUNTER TRIAL: EVERY FIGHT OF AN ACT, ONE AFTER ANOTHER, PLAYED BY HAND (user, 2026-10-31) ───────────
// The benches measured a planner, and the potential probe measured a deck that spends all of itself on one thing;
// neither is a person playing. The trial is the instrument for that: a fixed walk through EVERY encounter of an
// act, stage by stage (each stage's single fights, then its multi-fights, then its elites), and at the end every
// boss of the act — full health before each fight, so each fight is read on its own.
//
// What the deck is at each point is what an average run would have made of it by then. A walk of an act is about
// fourteen rooms, and the generated maps (`--map-report`) put roughly 1.9 fights, 0.6 relics, a third of a rest and
// a quarter of a shop into each stage of Act I. So the fights here pay NOTHING, and between the stages the player
// collects that stage's share at once, choosing as in a run:
//
//   after stage 1   two card offers, one improvement
//   after stage 2   two card offers, one relic
//   after stage 3   two card offers, one removal
//   after stage 4   two card offers, one relic, one improvement        → the deck the bosses are met with
//
// The Lieutenant walks it, from her own start (user). Her body is her own; the Infirmary, a relic only this walk
// hands out, mends her fully after every fight won.
//
// The walk rides in the game document as Presentation.Game.Extra["trial:<act>"] — a RunTutorial in RunJson, which
// the frontend walks the way it walks a lesson (map and start), on a save slot of its own.
public static class EncounterTrial
{
    public const string InfirmaryId = "trial_infirmary";
    public const string ExtraPrefix = "trial:";
    public static readonly int[] Acts = [1];

    public static string SpoilsId(int act, int stage) => $"trial_{act}_spoils_{stage}";

    // The relic that makes every fight start from full health. Event pool: no chest, shelf or boss ever offers it.
    public static RelicAuthoring.BnbRelic Infirmary => RelicAuthoring.Event(InfirmaryId, "The Infirmary",
        "The Encounter Trial — every fight is read on its own.",
        "Encounter Trial only: after every fight you win, heal to full.",
        runPrograms: [RelicAuthoring.AfterEveryVictory(new ComputedHealRunEffect(RunExpr.MissingHealth))]);

    // The stage packages, in order. Each is one stop with one choice; its effects ask one after another.
    private static IReadOnlyList<IRunEffectRequest> Spoils(ConversionPools pools, int act, int stage)
    {
        var where = SpoilsId(act, stage);
        IRunEffectRequest Card(int n) =>
            new OfferRewardRunEffect(new RewardId($"{where}:card{n}"), pools.CardRewardSource(), 1) { Kind = RewardKinds.Card };
        IRunEffectRequest Relic() =>
            new OfferRewardRunEffect(new RewardId($"{where}:relic"), pools.NormalRelicOnTheCurve(where), 1) { Kind = RewardKinds.Relic };
        IRunEffectRequest Improve() =>
            new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().ChooseByPlayer(1, "improve one card, permanently", allowSkip: true));
        IRunEffectRequest Remove() =>
            new RemoveCardsRunEffect(RunSelectors.DeckCards.ChooseByPlayer(1, "remove one card from your deck", allowSkip: true));
        return stage switch
        {
            1 => [Card(1), Card(2), Improve()],
            2 => [Card(1), Card(2), Relic()],
            3 => [Card(1), Card(2), Remove()],
            _ => [Card(1), Card(2), Relic(), Improve()],
        };
    }

    private static string SpoilsText(int stage) => stage switch
    {
        1 => "two card offers and one improvement",
        2 => "two card offers and one relic",
        3 => "two card offers and one card removed",
        _ => "two card offers, one relic and one improvement — the deck the bosses meet",
    };

    // The trial's own stops, for the blueprint's events.
    public static IEnumerable<(string Id, EventScript Script)> Events(IReadOnlyDictionary<int, ConversionPools> pools)
    {
        foreach (var act in Acts)
        {
            var stages = ActStages.Order[act].Count;
            for (var stage = 1; stage <= stages; stage++)
                yield return (SpoilsId(act, stage), new EventScript("start",
                [
                    new EventSituation("start",
                        $"Stage {stage} of {stages} is behind you. An average run would have gathered about this much by now.",
                    [
                        new EventChoice("collect", Spoils(pools[act], act, stage), TextKey: $"Collect: {SpoilsText(stage)}"),
                    ]),
                ]));
        }
    }

    // The encounters of one stage, as the act's own pools field them: single fights, multi-fights, elites.
    public static IReadOnlyList<(MapNodeKind Kind, string Encounter)> Stage(BabData data, MapGenerationSpec spec, int act, int stage)
    {
        var pooled = spec.Encounters!.ByRole.Values.SelectMany(p => p.Select(e => e.Encounter.Value)).ToHashSet(StringComparer.Ordinal);
        var ofAct = data.Encounters.Where(e => e.Act == act && pooled.Contains(e.Id)).ToList();
        IEnumerable<(MapNodeKind, string)> Of(string role, MapNodeKind kind) => ofAct
            .Where(e => e.Role == role)
            .Where(e => (kind == MapNodeKind.Elite
                ? ActStages.EliteStage(act, e.Id, e.Tags, e.EarliestDepthPercent)
                : ActStages.StageOf(act, e.Tags)) == stage)
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .Select(e => (kind, e.Id));
        return [.. Of("combat", MapNodeKind.Combat), .. Of("multi_combat", MapNodeKind.MultiCombat), .. Of("elite", MapNodeKind.Elite)];
    }

    public static RunTutorial Build(BabData data, MapGenerationSpec spec, int act, RunStart lieutenant)
    {
        var nodes = new List<Node>();
        void Fight(MapNodeKind kind, string encounter)
        {
            // The room as the act realises it, minus its pay: a trial fight is a reading, not a step up.
            var content = MapNodeRealizer.Realize(spec, kind, new EncounterId(encounter));
            var bare = content.Payload is EncounterRef found ? new EncounterRef(found.Id) : content.Payload;
            nodes.Add(new Node(new NodeId($"trial{nodes.Count}"), content.Type, bare, content.Tags));
        }
        var stages = ActStages.Order[act].Count;
        for (var stage = 1; stage <= stages; stage++)
        {
            foreach (var (kind, encounter) in Stage(data, spec, act, stage))
                Fight(kind, encounter);
            nodes.Add(new Node(new NodeId($"trial{nodes.Count}"), StandardRunIds.EventNode,
                new EventRef(new EventId(SpoilsId(act, stage))), [MapNodeTags.Treasure]));
        }
        foreach (var boss in spec.Encounters!.For(MapNodeKind.Boss).Select(e => e.Encounter.Value).Distinct().Order(StringComparer.Ordinal))
            Fight(MapNodeKind.Boss, boss);

        var start = lieutenant with { StartingRelics = [.. lieutenant.StartingRelics, InfirmaryId] };
        return new RunTutorial(new RunMap(nodes), start);
    }

    public static IEnumerable<KeyValuePair<string, string>> Extra(BabData data, IReadOnlyDictionary<int, MapGenerationSpec> specs, RunStart lieutenant)
    {
        var options = RunJson.CreateOptions();
        foreach (var act in Acts)
            yield return new($"{ExtraPrefix}{act}", JsonSerializer.Serialize(Build(data, specs[act], act, lieutenant), options));
    }
}
