using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act II's fifteen authored events (BnB_Final_Events_Master_PostAudit.md §"ACT II") — the shape of the set.
// What each one DOES is checked where it lands, in ActTwoEventLiveTests.
public class ActTwoEventTests
{
    private static readonly RunBlueprint Game = FightProbe.Game;

    internal static readonly string[] Fifteen =
    [
        "misfiled_prophecy", "self_correcting_index", "locked_reading_room", "perpetual_borrower",
        "reciprocal_shelf", "margin_notes", "unclaimed_reservation", "infinite_return_slot",
        "redacted_portrait", "lost_hour_bottle", "necrology_window", "almost_helpful_clerk_reassigned",
        "last_quiet_table", "inward_seal", "librarian_at_the_end_of_the_aisle",
    ];

    // ── the shape of the set ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void All_fifteen_ship_and_the_archives_draw_from_them()
    {
        var pool = Game.Acts![1].MapGeneration!.NodeRefPools[MapNodeKind.Event];

        Assert.Equal(15, Fifteen.Length);
        foreach (var id in Fifteen)
        {
            Assert.True(Game.Events.ContainsKey(id), $"'{id}' has no script");
            Assert.Contains(id, pool);
            Assert.True(Game.Presentation.Events.ContainsKey(id), $"'{id}' has no look");
        }
        // …and nothing else: the ported archives events are out of the loader entirely.
        Assert.Equal(15, pool.Count);
    }

    // Every branch offers its outcome before the door closes, and every outcome is somewhere to go.
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
    public void Everything_the_events_name_is_authored()
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
                        if (node.Kind == "applyStatus")
                            Assert.Contains(node.StatusId, statuses);
                        if (node.Kind == "createCardInstance")
                            Assert.Contains(node.ToDefinition, cards);
                    }
                    break;
                // The Librarian's first branch is a fork, and both sides of it are real effects.
                case ConditionalRunEffect conditional:
                    foreach (var inner in conditional.WhenTrue.Concat(conditional.WhenFalse))
                        Check(inner);
                    break;
            }
        }
    }

    // Every promise the fifteen can install must be one a run can actually keep — and, since it is installed by
    // NAME, one the document authored.
    [Fact]
    public void Every_authored_promise_is_installed_by_someone()
    {
        var installed = Fifteen
            .SelectMany(id => Effects(Game.Events[id]))
            .SelectMany(Flatten)
            .OfType<InstallProgramByIdRunEffect>()
            .Select(e => e.Source.Value)
            .ToHashSet();

        // …plus the four an archives promise installs on its own behalf: the bailiff, the extra reward and the
        // bounty are second beats of promises the doors DO hand out, and the shelf label's second misfiling is
        // what re-arms the shared expiry.
        installed.Add(ActTwoEventPrograms.GarnishThePurse);
        installed.Add(ActTwoEventPrograms.GarnishedReward);
        installed.Add(ActTwoEventPrograms.ExtraCardReward);
        installed.Add(ActTwoEventPrograms.NecrologyBounty);
        installed.Add(ActTwoEventPrograms.MarkingsExpire);

        var archives = Game.Programs!.Keys.Where(k => k.StartsWith("act_two_", StringComparison.Ordinal));
        Assert.Equal(archives.OrderBy(k => k), installed.OrderBy(k => k));
    }

    // ── the depth gate ────────────────────────────────────────────────────────────────────────────────────

    // Every Act-II event carries the design's "Earliest Stage N"; the map is told about it as a depth, and the
    // Librarian at the end of the aisle is the deepest door in the act.
    [Fact]
    public void The_archives_gate_their_deep_doors_by_depth()
    {
        var gates = Game.Acts![1].MapGeneration!.NodeRefMinimumDepthPercent;

        Assert.Equal(77, gates["librarian_at_the_end_of_the_aisle"]); // earliest stage 8 of 10
        Assert.Equal(88, gates["necrology_window"]);                  // earliest stage 9
        Assert.Equal(11, gates["misfiled_prophecy"]);                 // earliest stage 2
        // Stage 1 is the doorstep and is not a gate at all — the reassigned Clerk may open anywhere.
        Assert.DoesNotContain("almost_helpful_clerk_reassigned", gates.Keys);
        // The city gates nothing: Act I's fifteen name no stage.
        Assert.Empty(Game.Acts![0].MapGeneration!.NodeRefMinimumDepthPercent);
    }

    // …and a generated Act-II map actually honours it.
    [Fact]
    public void No_generated_archive_map_opens_the_librarian_early()
    {
        var spec = Game.Acts![1].MapGeneration!;
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
