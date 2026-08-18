using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using Xunit;

namespace BnbContent.Tests;

// The first FINAL reworked Act-I identity, mapped from the real source-data: A Very Official Line. Proves the
// whole reworked-enemy pattern lands on the engine EncounterEnemy — a passive (queue_advances) carried from
// start, a state-conditional one-shot override (Queue Position ≥ 3 → Everyone Moves at Once), and that the
// SPECIAL intent is kept out of the round-robin cycle while still being a usable action definition.
public class FinalEnemyMappingTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    private static EncounterDefinition Encounter()
    {
        var enemies = Data.Enemies.ToDictionary(e => e.Id);
        var encounter = Data.Encounters.First(e => e.Enemies.Contains("a_very_official_line"));
        return EncounterMapper.Map(encounter, enemies, Data.Bureaucrat.StartingEnergy);
    }

    [Fact]
    public void The_line_carries_its_passive_and_keeps_the_special_intent_out_of_the_cycle()
    {
        var line = Encounter().Enemies.First(e => e.Id == "a_very_official_line");

        // Passive signature applied at combat start.
        Assert.Contains(line.StartingStatuses!, s => s.Status == new StatusDefinitionId("queue_advances"));

        // The cycle holds the three ordinary intents; "Everyone Moves at Once" is special → excluded from it.
        Assert.Equal(3, line.Actions.Count);
        Assert.DoesNotContain(line.Actions, a => a == new EnemyActionDefinitionId("a_very_official_line.everyone_moves"));
    }

    [Fact]
    public void The_queue_cash_out_is_a_self_counter_one_shot_override_of_the_special_intent()
    {
        var line = Encounter().Enemies.First(e => e.Id == "a_very_official_line");
        var rule = Assert.Single(line.IntentRules!);

        Assert.Equal(new EnemyActionDefinitionId("a_very_official_line.everyone_moves"), rule.Action);
        var cond = Assert.IsType<SelfHasCounterCondition>(rule.Condition);
        Assert.Equal(new CounterId("queue_position"), cond.Counter);
        Assert.Equal(3, cond.Value);
        Assert.Equal(ComparisonOperator.GreaterOrEqual, cond.Op);
    }

    [Fact]
    public void The_special_intent_still_has_an_action_definition_for_the_rule_to_name()
    {
        var actions = EnemyMapper.MapActions(Data.Enemies.Where(e => e.Id == "a_very_official_line").ToList());
        Assert.Contains(actions, a => a.Id == "a_very_official_line.everyone_moves");
    }

    // Cross-combatant passive: an encounter fielding the Wrong-Window Scribe carries its "Not This Counter"
    // reaction as a per-encounter CardPlayed triggered effect (reacts to the PLAYER playing a card).
    [Fact]
    public void The_scribe_contributes_a_card_played_encounter_trigger()
    {
        var enemies = Data.Enemies.ToDictionary(e => e.Id);
        var encounter = Data.Encounters.First(e => e.Enemies.Contains("wrong_window_scribe"));
        var mapped = EncounterMapper.Map(encounter, enemies, Data.Bureaucrat.StartingEnergy);

        var trigger = Assert.Single(mapped.TriggeredEffects);
        Assert.Equal("CardPlayed", trigger.Event);
    }

    // DSL: "N damage + X per <status>, capped at Y" → dealDamage(add(N, min(mul(statusStacks, X), Y))).
    // (Queue-Crier's "Lost Your Place": 7 + min(panic*3, 9).)
    [Fact]
    public void Damage_per_status_supports_a_base_and_a_cap()
    {
        var effect = new BabEffect("damage_per_status", "player", Amount: 7, Status: "panic",
            AmountPerStack: 3, null, null, null, null, null, Cap: 9);
        var node = EffectMapper.Map("test", effect, EffectMapper.EnemyTargets);

        Assert.Equal("dealDamage", node.Kind);
        var amount = node.Amount!;
        Assert.Equal("add", amount.Kind);                 // base + scaled
        Assert.Equal(7, amount.LeftOrDefault.Const);      // base 7
        Assert.Equal("min", amount.RightOrDefault.Kind);  // capped
        Assert.Equal(9, amount.RightOrDefault.RightOrDefault.Const); // cap 9
        Assert.Equal("mul", amount.RightOrDefault.LeftOrDefault.Kind); // stacks * per-stack
    }
}
