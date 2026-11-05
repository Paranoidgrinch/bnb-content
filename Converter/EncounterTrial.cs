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
// A later act's trial opens where an average run would stand at that act's door: every earlier act's four packages,
// each followed by that act's boss spoils (a card offer and one of three of its bosses' relics), collected one stop
// after another before the first fight.
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
    public static readonly int[] Acts = [1, 2, 3, 4];

    public static string SpoilsId(int act, int stage) => $"trial_{act}_spoils_{stage}";

    // What an act's boss leaves behind, for the trial of the act after it: a card offer and a boss relic, one of
    // three drawn from every boss of the act (which boss a run meets is the map's draw).
    public static string BossSpoilsId(int act) => $"trial_{act}_boss_spoils";

    // The relic that makes every fight start from full health. Event pool: no chest, shelf or boss ever offers it.
    public static RelicAuthoring.BnbRelic Infirmary => RelicAuthoring.Event(InfirmaryId, "The Infirmary",
        "The Encounter Trial — every fight is read on its own.",
        "Encounter Trial only: after every fight you win, heal to full.",
        // A trial walks no act's opening, so the acts' after-fight ledgers (the Dragon's fees, the Signpost's
        // purse, the Toll Frog's belly, the Labyrinth's Tomb Gold and Feathers) ride here too.
        runPrograms:
        [
            RelicAuthoring.AfterEveryVictory(new ComputedHealRunEffect(RunExpr.MissingHealth)),
            MunicipalDragon.FeesProgram(),
            EncounterPassives.SignpostSpoilsProgram(),
            ActThree.FrogSpoilsProgram(),
            .. ActFour.CutPrograms().Values,
        ]);

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
        // An act of more than four stages (Act IV has six) repeats the first three packages until its last stage.
        var stages = ActStages.Order[act].Count;
        return (stage == stages ? 4 : (stage - 1) % 3 + 1) switch
        {
            1 => [Card(1), Card(2), Improve()],
            2 => [Card(1), Card(2), Relic()],
            3 => [Card(1), Card(2), Remove()],
            _ => [Card(1), Card(2), Relic(), Improve()],
        };
    }

    private static string SpoilsText(int act, int stage) => (stage == ActStages.Order[act].Count ? 4 : (stage - 1) % 3 + 1) switch
    {
        1 => "two card offers and one improvement",
        2 => "two card offers and one relic",
        3 => "two card offers and one card removed",
        _ => "two card offers, one relic and one improvement — the deck the bosses meet",
    };

    // The trial's own stops, for the blueprint's events.
    public static IEnumerable<(string Id, EventScript Script)> Events(BabData data, IReadOnlyDictionary<int, ConversionPools> pools)
    {
        foreach (var act in Acts)
        {
            var bossRelics = data.Encounters.Where(e => e.Act == act && e.Role == "boss")
                .SelectMany(e => BossRelics.For(e.Name)).DistinctBy(r => r.Id).ToList();
            yield return (BossSpoilsId(act), new EventScript("start",
            [
                new EventSituation("start",
                    $"The Act {Roman(act)} boss is behind you — what a boss leaves: a card, and one of three of its relics.",
                [
                    new EventChoice("collect",
                    [
                        new OfferRewardRunEffect(new RewardId($"{BossSpoilsId(act)}:card"), pools[act].CardRewardSource(), 1)
                            { Kind = RewardKinds.Card },
                        new OfferRewardRunEffect(
                            new RewardId($"{BossSpoilsId(act)}:relic"),
                            new PoolRewardSource(
                                new RunPool<RewardOffer>([.. bossRelics.Select(relic => new RunPool<RewardOffer>.Entry(
                                    new RewardOffer($"relic-{relic.Id}", [new AddRelicByIdRunEffect(new RelicId(relic.Id))]), 1))]),
                                3),
                            1)
                            { Kind = RewardKinds.Relic },
                    ], TextKey: "Collect: a card offer and a boss relic"),
                ]),
            ]));
            var stages = ActStages.Order[act].Count;
            for (var stage = 1; stage <= stages; stage++)
                yield return (SpoilsId(act, stage), new EventScript("start",
                [
                    new EventSituation("start",
                        $"Stage {stage} of {stages} is behind you. An average run would have gathered about this much by now.",
                    [
                        new EventChoice("collect", Spoils(pools[act], act, stage), TextKey: $"Collect: {SpoilsText(act, stage)}"),
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

    private static string Roman(int act) => act switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", _ => act.ToString() };

    public static RunTutorial Build(BabData data, MapGenerationSpec spec, int act, RunStart lieutenant)
    {
        var nodes = new List<Node>();
        void Stop(string eventId) => nodes.Add(new Node(new NodeId($"trial{nodes.Count}"), StandardRunIds.EventNode,
            new EventRef(new EventId(eventId)), [MapNodeTags.Treasure]));
        // The acts before this one, as an average run would have come out of them.
        for (var earlier = 1; earlier < act; earlier++)
        {
            for (var stage = 1; stage <= ActStages.Order[earlier].Count; stage++)
                Stop(SpoilsId(earlier, stage));
            Stop(BossSpoilsId(earlier));
        }
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
            Stop(SpoilsId(act, stage));
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
