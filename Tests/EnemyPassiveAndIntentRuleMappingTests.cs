using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using Xunit;

namespace BnbContent.Tests;

// Step-2 foundation: reworked enemies carry passive statuses (their signature rule) and state-conditional
// intent rules. This proves the converter threads both onto the engine's EncounterEnemy — the plumbing every
// reworked identity relies on. Existing demo enemies (no passives/rules) stay unchanged.
public class EnemyPassiveAndIntentRuleMappingTests
{
    private static EncounterDefinition MapOne(BabEnemy enemy)
    {
        var encounter = new BabEncounter("enc", "Enc", 1, "normal", [enemy.Id], null, null);
        return EncounterMapper.Map(encounter, new Dictionary<string, BabEnemy> { [enemy.Id] = enemy }, 3);
    }

    private static BabEnemy Enemy(
        IReadOnlyList<BabEnemyStatus>? passives = null, IReadOnlyList<BabIntentRule>? rules = null) => new(
        "queue_crier", "Queue-Crier Homunculus", 31, "cycle",
        [
            new BabIntent("recite", "Recite", "attack", 6, null, null, null, null),
            new BabIntent("everyone_moves", "Everyone Moves at Once", "attack", 12, null, null, null, null),
        ],
        Tags: null, StartingStatuses: passives, IntentRules: rules);

    [Fact]
    public void An_enemy_with_no_extensions_maps_as_before()
    {
        var e = MapOne(Enemy());
        var member = Assert.Single(e.Enemies);
        Assert.Null(member.StartingStatuses);
        Assert.Null(member.IntentRules);
    }

    [Fact]
    public void Passive_statuses_become_starting_statuses()
    {
        var e = MapOne(Enemy(passives: [new BabEnemyStatus("lost_your_place", 1)]));
        var status = Assert.Single(e.Enemies[0].StartingStatuses!);
        Assert.Equal(new StatusDefinitionId("lost_your_place"), status.Status);
        Assert.Equal(1, status.Stacks);
    }

    [Fact]
    public void A_self_counter_intent_rule_maps_to_a_one_shot_override()
    {
        // "When Queue Position hits 3, replace the next intent with Everyone Moves at Once."
        var e = MapOne(Enemy(rules:
        [
            new BabIntentRule(
                new BabIntentCondition("self_counter", Counter: "queue_position", Status: null, Resource: null,
                    Op: "ge", Value: 3, Percent: null, MinStacks: null, LastTurn: null, Conditions: null),
                Action: "everyone_moves", Priority: 10),
        ]));

        var rule = Assert.Single(e.Enemies[0].IntentRules!);
        Assert.Equal(10, rule.Priority);
        Assert.Equal(new EnemyActionDefinitionId("queue_crier.everyone_moves"), rule.Action);
        var cond = Assert.IsType<SelfHasCounterCondition>(rule.Condition);
        Assert.Equal(new CounterId("queue_position"), cond.Counter);
        Assert.Equal(ComparisonOperator.GreaterOrEqual, cond.Op);
        Assert.Equal(3, cond.Value);
    }

    [Fact]
    public void Combinator_conditions_nest()
    {
        var e = MapOne(Enemy(rules:
        [
            new BabIntentRule(
                new BabIntentCondition("all_of", null, null, null, null, null, null, null, null,
                [
                    new BabIntentCondition("health_percent", null, null, null, "le", null, 50, null, null, null),
                    new BabIntentCondition("opponent_cards_played", null, null, null, "ge", 3, null, null, false, null),
                ]),
                Action: "everyone_moves", Priority: null),
        ]));

        var cond = Assert.IsType<AllOfCondition>(e.Enemies[0].IntentRules![0].Condition);
        Assert.Equal(2, cond.Conditions.Count);
        Assert.IsType<EnemyHealthPercentCondition>(cond.Conditions[0]);
        Assert.IsType<OpponentCardsPlayedCondition>(cond.Conditions[1]);
    }
}
