using System.Text.Json;
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

        // The pair it answers, and the turn start that opens its once-a-turn latch again.
        Assert.Equal(["CardPlayed", "TurnStarted"], mapped.TriggeredEffects.Select(t => t.Event).Order(StringComparer.Ordinal));
    }

    // Queue-Crier Homunculus ("Lost Your Place", Panic cash-out): the passive is baked into its one pure
    // ATTACK intent — 7 damage +3 per Panic, capped +9, Panic not consumed (see ADAPTATIONS.md).
    [Fact]
    public void The_crier_bakes_lost_your_place_into_its_attack_intent()
    {
        var crier = Data.Enemies.Single(e => e.Id == "queue_crier_homunculus");
        Assert.Equal(31, crier.MaxHp);
        // Its scaling lives in the intent; its own rule (Cutting the Line, 2026-11-01) is a status, not an intent rule.
        Assert.Equal(["cutting_the_line"], crier.StartingStatuses!.Select(s => s.Status));
        Assert.Null(crier.IntentRules);

        var actions = EnemyMapper.MapActions([crier]);
        var call = Assert.Single(actions, a => a.Id == "queue_crier_homunculus.call_a_number_that_is_not_yours");
        // The telegraph carries the whole formula — the player plans against base, bonus AND cap.
        Assert.Equal("Call a Number That Is Not Yours · 7 dmg +3 per Panic (max +9)", call.Intent.Label);

        var amount = CombatProgramModel.Classify(call.Program)!.Amount!;
        Assert.Equal(7, amount.LeftOrDefault.Const);                    // base 7
        Assert.Equal(9, amount.RightOrDefault.RightOrDefault.Const);    // cap +9
        Assert.Equal("panic", amount.RightOrDefault.LeftOrDefault.LeftOrDefault.ReadId);

        // The other two intents stay flat — the cash-out is one telegraphed hit per cycle.
        var recite = Assert.Single(actions, a => a.Id == "queue_crier_homunculus.recite_the_waiting_order");
        Assert.Equal("sequence", CombatProgramModel.Classify(recite.Program)!.Kind);
        Assert.DoesNotContain(actions, a => a.Id.Contains("monotone_rebuke")); // demo filler is gone
    }

    // Filing Beetle (Stage 3, the Bookworm tutorial): its signature is that it buffs ITSELF with Bookworm —
    // an enemy-side apply_status, the shape that silently breaks if "owner" ever maps to the hero.
    [Fact]
    public void The_beetle_applies_bookworm_to_itself()
    {
        var beetle = Data.Enemies.Single(e => e.Id == "filing_beetle");
        Assert.Equal(40, beetle.MaxHp);

        var folio = Assert.Single(EnemyMapper.MapActions([beetle]), a => a.Id == "filing_beetle.worm_eaten_folio");
        var nodes = CombatProgramModel.Classify(folio.Program)!.Children!;
        Assert.Equal("applyStatus", nodes[0].Kind);
        Assert.Equal("bookworm", nodes[0].StatusId);
        Assert.Equal("source", nodes[0].SelectorKey); // the beetle, not the hero
        Assert.Equal("gainBlock", nodes[1].Kind);
    }

    // Duos field REDUCED bodies ("Duo HP Scaling"): the encounter's per-roster health wins over the enemy's own
    // max_hp, positionally, so the same identity can appear at different HP in different encounters.
    [Fact]
    public void An_encounters_per_roster_health_overrides_the_enemys_own()
    {
        var enemies = Data.Enemies.ToDictionary(e => e.Id);
        var duo = Data.Encounters.Single(e => e.Id == "city_normal_seal_08");
        var mapped = EncounterMapper.Map(duo, enemies, Data.Bureaucrat.StartingEnergy);

        Assert.Equal(56, enemies["sealed_door_ward"].MaxHp); // solo HP, untouched
        Assert.Equal(39, mapped.Enemies.Single(e => e.Id == "sealed_door_ward").MaxHealth);
        Assert.Equal(27, mapped.Enemies.Single(e => e.Id == "oath_candle").MaxHealth);
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
