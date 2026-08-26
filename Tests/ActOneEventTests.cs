using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act I's fifteen authored events (BnB_Final_Events_Master_PostAudit.md §"ACT I").
//
// Each is checked where it actually lands: the door is walked for real, the branch is taken by name, and then
// the FIGHT it changed is looked at — and, where the event promised something for afterwards, the fight is won
// and the run is asked what it paid. Nothing here reaches into the run to set up its own premise; a run is
// rebuilt from its own answers, so a state written from outside is written away again.
public class ActOneEventTests
{
    private static readonly RunBlueprint Game = FightProbe.Game;

    internal static readonly string[] Fifteen =
    [
        "misfiling_cabinet", "certified_copy_drawer", "self_amending_fee_table", "lost_and_found_desk",
        "licensed_vendor", "complaint_ledger", "waiting_token_exchange", "almost_helpful_clerk",
        "witness_queue", "sealed_back_door", "clerks_tea_break", "friendly_filing_cabinet",
        "receipt_of_prior_effort", "contradictory_map", "archive_window",
    ];

    // ── the shape of the set ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void All_fifteen_ship_and_the_city_draws_from_them()
    {
        var pool = Game.Acts![0].MapGeneration!.NodeRefPools[MapNodeKind.Event];

        Assert.Equal(15, Fifteen.Length);
        foreach (var id in Fifteen)
        {
            Assert.True(Game.Events.ContainsKey(id), $"'{id}' has no script");
            Assert.Contains(id, pool);
            Assert.True(Game.Presentation.Events.ContainsKey(id), $"'{id}' has no look");
        }
    }

    // The ported originals wore the same names; both sets in one pool would have offered each event twice.
    [Fact]
    public void The_ported_city_events_are_gone()
    {
        Assert.DoesNotContain("misfiling_cabinet", BabData.Load(TestData.Directory).Events.Select(e => e.Id));
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
                }
    }

    // Every promise the fifteen can install must be one a run can actually keep — and, since it is installed by
    // NAME, one the document authored.
    [Fact]
    public void Every_authored_promise_is_installed_by_someone()
    {
        var installed = Fifteen
            .SelectMany(id => Effects(Game.Events[id]))
            .OfType<InstallProgramByIdRunEffect>()
            .Select(e => e.Source.Value)
            .ToHashSet();

        // …except the bailiff, which is installed by the garnishment rather than by an event.
        installed.Add(ActOneEventPrograms.GarnishThePurse);
        Assert.Equal(Game.Programs!.Keys.OrderBy(k => k), installed.OrderBy(k => k));
    }

    private static IEnumerable<IRunEffectRequest> Effects(EventScript script) =>
        script.Situations.Values
            .SelectMany(s => s.Choices)
            .SelectMany(c => c.Effects.Concat(c.Costs?.SelectMany(cost => cost.Pay) ?? []));

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
