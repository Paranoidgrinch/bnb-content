using BnbContent.Converter;
using BnbContent.Converter.Events;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act IV's twenty authored doors (BnB_Final_Events_Master_PostAudit.md §"ACT IV") — the shape of the set.
// What each one DOES is checked where it lands, in ActFourEventLiveTests.
public class ActFourEventTests
{
    private static readonly RunBlueprint Game = FightProbe.Game;

    internal static readonly string[] Twenty =
    [
        "the_dry_nilometer", "the_black_granary", "the_red_linen_procession", "the_nameless_cartouche",
        "the_forewritten_tablet", "the_tomb_robbers_fire", "the_triple_counted_donkey",
        "the_four_canopic_jars", "the_chamber_of_false_measures", "the_crocodile_at_the_weighing_place",
        "the_wall_of_old_complaints", "the_copper_tithe", "the_unnamed_throne", "the_fixed_day_festival",
        "the_broken_sluice", "the_unfinished_burial", "the_survey_of_the_dead",
        "the_house_of_life_at_night", "the_merciful_balance", "the_cartouche_repair_bench",
    ];

    // ── the shape of the set ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void All_twenty_ship_with_a_script_and_a_look()
    {
        Assert.Equal(20, Twenty.Length);
        foreach (var id in Twenty)
        {
            Assert.True(Game.Events.ContainsKey(id), $"'{id}' has no script");
            Assert.True(Game.Presentation.Events.ContainsKey(id), $"'{id}' has no look");
        }
    }

    // The seam this file used to pin from the other side: until IV-24 no map drew these doors at all, because
    // Act IV was a catalogue the document shipped rather than an act a run crossed. Now it is the fourth act,
    // and the twenty are exactly the doors ITS map opens — and no earlier act's.
    [Fact]
    public void The_fourth_acts_map_opens_these_twenty_doors_and_no_earlier_act_opens_any_of_them()
    {
        Assert.Equal(4, Game.Acts!.Count);

        var labyrinth = Game.Acts[3].MapGeneration!.NodeRefPools[MapNodeKind.Event];
        Assert.Equal([.. Twenty.Order()], [.. labyrinth.Order()]);

        foreach (var act in Game.Acts.Take(3))
            foreach (var id in Twenty)
                Assert.DoesNotContain(id, act.MapGeneration!.NodeRefPools[MapNodeKind.Event]);
    }

    [Fact]
    public void Every_branch_reads_its_outcome_back()
    {
        foreach (var id in Twenty)
        {
            var script = Game.Events[id];
            foreach (var choice in script.Situations.Values.SelectMany(s => s.Choices))
                if (choice.NextSituationId is { } next)
                    Assert.True(script.Situations.ContainsKey(next), $"'{id}' → '{choice.Id}' goes nowhere");
            foreach (var situation in script.Situations.Values)
            {
                Assert.False(string.IsNullOrWhiteSpace(situation.TextKey), $"'{id}' has a silent situation");
                Assert.NotEmpty(situation.Choices);
            }
        }
    }

    // Everything the ten name has to exist: the promises they install, the relics they hand over, and the
    // statuses their fights open with.
    [Fact]
    public void Everything_the_doors_name_is_authored()
    {
        var programs = Game.Programs!.Keys.ToHashSet();
        var relics = Game.Relics.Select(r => r.Id).ToHashSet();
        var statuses = Game.Statuses.Select(s => s.Id).ToHashSet();

        foreach (var id in Twenty)
            foreach (var effect in Effects(Game.Events[id]).SelectMany(Flatten))
                switch (effect)
                {
                    case InstallProgramByIdRunEffect install:
                        Assert.Contains(install.Source.Value, programs);
                        break;
                    case AddRelicByIdRunEffect relic:
                        Assert.Contains(relic.Relic.Value, relics);
                        break;
                    case InstallNextCombatOpeningRunEffect opening:
                        foreach (var node in Nodes(opening.Rule))
                            if (node.Kind is "applyStatus" or "modifyStatusStacks")
                                Assert.Contains(node.StatusId, statuses);
                        break;
                }
    }

    // Every Act-IV promise must be one a run can keep, and every one the document authors must be one a door
    // (or another promise) actually reaches. A stretch's later links are installed by the link before them.
    [Fact]
    public void Every_authored_promise_is_installed_by_someone()
    {
        var installed = Twenty
            .SelectMany(id => Effects(Game.Events[id]))
            .SelectMany(Flatten)
            .OfType<InstallProgramByIdRunEffect>()
            .Select(e => e.Source.Value)
            .ToHashSet();

        // …plus what the Labyrinth's own promises install: each stretch's next link, and the two prizes the
        // fight doors arm when the fight they set is actually walked into.
        installed.Add(ActFourEventPrograms.TabletPrize);
        installed.Add(ActFourEventPrograms.RobbersPrize);
        installed.Add(ActFourEventPrograms.TithePrize);
        installed.Add(ActFourEventPrograms.CountPrize);
        foreach (var name in Game.Programs!.Keys.Where(k => k.Contains("_again_", StringComparison.Ordinal)))
            installed.Add(name);

        var labyrinth = Game.Programs.Keys.Where(k => k.StartsWith("act_four_", StringComparison.Ordinal));
        Assert.Equal(labyrinth.OrderBy(k => k), installed.OrderBy(k => k));
    }

    // ── the nine relics the twenty hand over ──────────────────────────────────────────────────────────────

    // Act IV's nine Event relics, each granted by exactly one branch of exactly one door and reachable
    // nowhere else. A branch that grants nothing is a branch nobody can test (the IV-20 lesson), which is
    // why every relic ships with the door that hands it over.
    [Fact]
    public void The_nine_relics_these_doors_grant_are_authored_and_granted_exactly_once()
    {
        Assert.Equal(9, EventRelics.ActIV.Count);

        var granted = Twenty
            .SelectMany(id => Effects(Game.Events[id]))
            .OfType<AddRelicByIdRunEffect>()
            .Select(e => e.Relic.Value)
            .ToList();

        Assert.Equal(EventRelics.ActIV.Select(r => r.Id).OrderBy(id => id), granted.OrderBy(id => id));

        // Each is a rule of a fight, and each rule is in the document.
        var statuses = Game.Statuses.Select(s => s.Id).ToHashSet();
        foreach (var relic in EventRelics.ActIV)
        {
            Assert.NotNull(relic.CombatRule);
            Assert.Contains(relic.CombatRule!.Id, statuses);
        }
    }

    // An Event relic belongs to its door and to nothing else — not a shop shelf, not a chest, not a boss.
    [Fact]
    public void No_act_four_event_relic_reaches_another_pool()
    {
        foreach (var relic in EventRelics.ActIV)
        {
            Assert.Equal(RelicAuthoring.Pool.Event, relic.Pool);
            Assert.DoesNotContain(relic.Id, FinalRelics.Pool(RelicAuthoring.Pool.Shop).Select(r => r.Id));
            Assert.DoesNotContain(relic.Id, FinalRelics.Pool(RelicAuthoring.Pool.Normal).Select(r => r.Id));
            Assert.DoesNotContain(relic.Id, FinalRelics.Pool(RelicAuthoring.Pool.Boss).Select(r => r.Id));
        }
    }

    // ── the depth gate ────────────────────────────────────────────────────────────────────────────────────

    // The design gives Act IV's doors availability BANDS rather than stage numbers, and each door carries
    // its band as the depth the map will gate it by once the act is walkable.
    [Fact]
    public void The_labyrinth_gates_its_late_doors_by_depth()
    {
        var data = BabData.Load(TestData.Directory);
        var doors = ActFourEvents.All(
            ConversionPools.Build(data, [.. data.Relics.Select(RelicMapper.Map)], ActFourEvents.Act),
            new Random(1))
            .ToDictionary(e => e.Id, e => e.EarliestDepthPercent);

        Assert.Equal(55, doors["the_forewritten_tablet"]);              // Mid–Late
        Assert.Equal(55, doors["the_crocodile_at_the_weighing_place"]); // Mid–Late
        Assert.Equal(40, doors["the_chamber_of_false_measures"]);       // Mid
        Assert.Equal(0, doors["the_dry_nilometer"]);                    // Early–Mid
        Assert.Equal(0, doors["the_red_linen_procession"]);             // All
        Assert.Equal(75, doors["the_unnamed_throne"]);                  // Late · Rare
        Assert.Equal(75, doors["the_survey_of_the_dead"]);              // Late
        Assert.Equal(55, doors["the_unfinished_burial"]);               // Mid–Late
        Assert.Equal(40, doors["the_fixed_day_festival"]);              // Mid
        Assert.Equal(0, doors["the_broken_sluice"]);                    // Early–Mid
    }

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<IRunEffectRequest> Effects(EventScript script) =>
        script.Situations.Values
            .SelectMany(s => s.Choices)
            .SelectMany(c => c.Effects.Concat(c.Costs?.SelectMany(cost => cost.Pay) ?? []));

    private static IEnumerable<IRunEffectRequest> Flatten(IRunEffectRequest effect) =>
        effect is ConditionalRunEffect conditional
            ? conditional.WhenTrue.Concat(conditional.WhenFalse).SelectMany(Flatten).Append(effect)
            : [effect];

    private static IEnumerable<CombatNodeModel> Nodes(RelicCombatRule rule)
    {
        var stack = new Stack<CombatNodeModel>();
        if (RelicCombatTriggers.Get(rule.Trigger).ToModel(rule.Program) is not { } root)
            yield break;
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            foreach (var child in node.Children ?? [])
                stack.Push(child);
        }
    }
}
