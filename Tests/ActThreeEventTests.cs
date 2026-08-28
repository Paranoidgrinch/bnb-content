using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III's fifteen authored doors (BnB_Final_Events_Master_PostAudit.md §"ACT III") — the shape of the set.
// What each one DOES is checked where it lands, in ActThreeEventLiveTests.
public class ActThreeEventTests
{
    private static readonly RunBlueprint Game = FightProbe.Game;

    internal static readonly string[] Fifteen =
    [
        "a_clear_stream", "the_noticebound_hedge", "the_witch_at_the_milestone",
        "the_public_footpath_dispute", "moonlit_mushrooms", "a_spiders_clause", "the_ant_queue",
        "the_conceptual_toll", "rain_beneath_the_rowan", "the_buried_waystone",
        "the_travelling_chandler", "stargazing", "the_quiet_meadow", "the_ombudsmans_warning",
        "the_kindly_procession",
    ];

    // ── the shape of the set ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void All_fifteen_ship_and_the_road_draws_from_them()
    {
        var pool = Game.Acts![2].MapGeneration!.NodeRefPools[MapNodeKind.Event];

        Assert.Equal(15, Fifteen.Length);
        foreach (var id in Fifteen)
        {
            Assert.True(Game.Events.ContainsKey(id), $"'{id}' has no script");
            Assert.Contains(id, pool);
            Assert.True(Game.Presentation.Events.ContainsKey(id), $"'{id}' has no look");
        }
        // …and nothing else: the ported Green Docket events are out of the loader entirely.
        Assert.Equal(15, pool.Count);
    }

    [Fact]
    public void Every_branch_reads_its_outcome_back()
    {
        foreach (var id in Fifteen)
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

    // Everything the fifteen name has to exist: the promises they install, the relics they hand over, the
    // rules their fights open with and the cards they push into them.
    [Fact]
    public void Everything_the_doors_name_is_authored()
    {
        var programs = Game.Programs!.Keys.ToHashSet();
        var relics = Game.Relics.Select(r => r.Id).ToHashSet();
        var statuses = Game.Statuses.Select(s => s.Id).ToHashSet();
        var cards = Game.Cards.Select(c => c.Id).ToHashSet();

        foreach (var id in Fifteen)
            foreach (var effect in Effects(Game.Events[id]))
                Check(effect);

        void Check(IRunEffectRequest effect)
        {
            switch (effect)
            {
                case InstallProgramByIdRunEffect install:
                    Assert.Contains(install.Source.Value, programs);
                    break;
                case AddRelicByIdRunEffect relic:
                    Assert.Contains(relic.Relic.Value, relics);
                    break;
                case AddCardToDeckRunEffect card:
                    Assert.Contains(card.Card.value, cards);
                    break;
                case InstallNextCombatOpeningRunEffect opening:
                    foreach (var node in Nodes(opening.Rule))
                    {
                        if (node.Kind is "applyStatus" or "modifyStatusStacks")
                            Assert.Contains(node.StatusId, statuses);
                        if (node.Kind == "createCardInstance")
                            Assert.Contains(node.ToDefinition, cards);
                    }
                    break;
                case ConditionalRunEffect conditional:
                    foreach (var inner in conditional.WhenTrue.Concat(conditional.WhenFalse))
                        Check(inner);
                    break;
            }
        }
    }

    // Every promise the fifteen can install must be one a run can actually keep — and, since it is installed
    // by NAME, one the document authored.
    [Fact]
    public void Every_authored_promise_is_installed_by_someone()
    {
        var installed = Fifteen
            .SelectMany(id => Effects(Game.Events[id]))
            .SelectMany(Flatten)
            .OfType<InstallProgramByIdRunEffect>()
            .Select(e => e.Source.Value)
            .ToHashSet();

        // …plus the four a road's promise installs on its own behalf: the bailiff and its garnish, the two
        // card rewards the shortcuts pay, and the second halves of the two-fight promises.
        installed.Add(ActThreeEventPrograms.GarnishThePurse);
        installed.Add(ActThreeEventPrograms.GarnishedReward);
        installed.Add(ActThreeEventPrograms.ExtraCardReward);
        installed.Add(ActThreeEventPrograms.RareCardReward);

        var road = Game.Programs!.Keys.Where(k => k.StartsWith("act_three_", StringComparison.Ordinal));
        Assert.Equal(road.OrderBy(k => k), installed.OrderBy(k => k));
    }

    // The five inscriptions are permanent, so their rules have to reach every LATER fight — which they can
    // only do through the one promise every inscribing branch installs alongside the tag.
    [Fact]
    public void Every_inscription_carries_its_rules_forward()
    {
        var inscriptions = ActThreeEventObjects.Inscriptions().ToHashSet();

        foreach (var id in Fifteen)
            foreach (var choice in Game.Events[id].Situations.Values.SelectMany(s => s.Choices))
            {
                var writes = choice.Effects.OfType<TagCardsRunEffect>()
                    .Any(t => inscriptions.Contains(t.Tag.Value));
                if (!writes)
                    continue;
                Assert.Contains(choice.Effects.OfType<InstallProgramByIdRunEffect>(),
                    i => i.Source.Value == ActThreeEventPrograms.Inscriptions);
            }

        // …and all five rules are authored, so the promise has something to open a fight with. That it
        // REACHES a later fight is a thing only a played run can show, and ActThreeEventLiveTests shows it.
        var authored = Game.Statuses.Select(st => st.Id).ToHashSet();
        foreach (var inscription in inscriptions)
            Assert.Contains(inscription, authored);
    }

    // ── the depth gate ────────────────────────────────────────────────────────────────────────────────────

    // Every Act-III door carries the design's "Earliest Stage N"; the map is told about it as a depth.
    [Fact]
    public void The_road_gates_its_deep_doors_by_depth()
    {
        var gates = Game.Acts![2].MapGeneration!.NodeRefMinimumDepthPercent;

        Assert.Equal(77, gates["the_kindly_procession"]);      // earliest stage 8 of 10
        Assert.Equal(66, gates["the_ombudsmans_warning"]);     // earliest stage 7
        Assert.Equal(55, gates["the_buried_waystone"]);        // earliest stage 6
        Assert.Equal(11, gates["the_noticebound_hedge"]);      // earliest stage 2
        // Stage 1 is the doorstep and is not a gate at all — the stream and the meadow may open anywhere.
        Assert.DoesNotContain("a_clear_stream", gates.Keys);
        Assert.DoesNotContain("the_quiet_meadow", gates.Keys);
    }

    // …and a generated Act-III map actually honours it.
    [Fact]
    public void No_generated_road_opens_the_procession_early()
    {
        var spec = Game.Acts![2].MapGeneration!;
        var gates = spec.NodeRefMinimumDepthPercent;
        var seen = 0;

        for (var seed = 1; seed <= 25; seed++)
        {
            var generated = RuleBasedMapGenerator.Generate(
                spec, seed, startingLoadout: 0,
                new BalanceCalculator(Game.Balance, Game.Encounters),
                (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));
            var rows = generated.Map.Nodes.Max(n => Row(n.Id)) + 1;

            foreach (var node in generated.Map.Nodes)
            {
                if (generated.Roles[node.Id] != MapNodeKind.Event)
                    continue;
                if (node.Payload is not EventRef door
                    || !gates.TryGetValue(door.Id.Value, out var earliest))
                    continue;
                seen++;
                Assert.True(Row(node.Id) * 100 / (rows - 2) >= earliest,
                    $"'{door.Id}' opened at row {Row(node.Id)} of {rows}, but waits for {earliest}%");
            }
        }

        Assert.True(seen > 0, "no gated door was ever placed, so the gate proved nothing");
    }

    private static int Row(NodeId id) =>
        int.Parse(id.Value[1..id.Value.IndexOf('c')], System.Globalization.CultureInfo.InvariantCulture);

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
