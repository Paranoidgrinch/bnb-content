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

        var trigger = Assert.Single(mapped.TriggeredEffects);
        Assert.Equal("CardPlayed", trigger.Event);
    }

    // Receipt-Eyed Clerk (Doubt cash-out): the design's fourth intent joins the cycle and IS the cash-out —
    // 6 damage +2 per current Doubt, capped at +8, Doubt not removed. No passive, no rule: pure intent.
    [Fact]
    public void The_clerk_cashes_doubt_out_through_a_capped_scaling_intent()
    {
        var clerk = Data.Enemies.Single(e => e.Id == "receipt_eyed_clerk");
        Assert.Equal(35, clerk.MaxHp);
        Assert.Equal(4, clerk.Intents.Count); // all four cycle (none is special)

        var discrepancy = Assert.Single(EnemyMapper.MapActions([clerk]),
            a => a.Id == "receipt_eyed_clerk.date_discrepancy");
        Assert.Equal(IntentKind.Attack, discrepancy.Intent.Kind);

        var amount = CombatProgramModel.Classify(discrepancy.Program)!.Amount!;
        Assert.Equal("add", amount.Kind);
        Assert.Equal(6, amount.LeftOrDefault.Const);                    // base 6
        Assert.Equal("min", amount.RightOrDefault.Kind);                // capped bonus
        Assert.Equal(8, amount.RightOrDefault.RightOrDefault.Const);    // cap +8
        Assert.Equal("doubt", amount.RightOrDefault.LeftOrDefault.LeftOrDefault.ReadId);
    }

    // Triplicate Examiner ("Three Copies Required"): reacts to the PLAYER's third card of the turn's opening
    // type → 8 Block for the Examiner, 1 Doubt for the player. Cross-combatant ⇒ a per-encounter CardPlayed
    // trigger, not an owner-scoped status.
    [Fact]
    public void The_examiner_contributes_a_third_copy_encounter_trigger()
    {
        var examiner = Data.Enemies.Single(e => e.Id == "triplicate_examiner");
        Assert.Equal(41, examiner.MaxHp);

        var enemies = Data.Enemies.ToDictionary(e => e.Id);
        var encounter = Data.Encounters.First(e => e.Enemies.Contains("triplicate_examiner"));
        var trigger = Assert.Single(EncounterMapper.Map(encounter, enemies, Data.Bureaucrat.StartingEnergy).TriggeredEffects);
        Assert.Equal("CardPlayed", trigger.Event);

        // The stored program is runnable (every node/expression discriminator registered) and pays out both halves.
        var options = CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>();
        var json = trigger.Program.GetRawText();
        var reloaded = JsonSerializer.Deserialize<EffectProgram<CardPlayedTriggeredEffectContext>>(json, options)!;
        Assert.Equal(json, JsonSerializer.Serialize(reloaded, options));

        var conditional = Assert.IsType<ConditionalEffectNode<CardPlayedTriggeredEffectContext>>(reloaded.Root);
        var sequence = Assert.IsType<SequenceEffectNode<CardPlayedTriggeredEffectContext>>(conditional.Children[0]);
        var block = Assert.IsType<GainBlockNode<CardPlayedTriggeredEffectContext>>(sequence.Children[0]);
        Assert.Equal(CombatantTargetSelectors.AllEnemiesOfSource.GetType(), block.TargetSelector.GetType());
        var doubt = Assert.IsType<ApplyStatusNode<CardPlayedTriggeredEffectContext>>(sequence.Children[1]);
        Assert.Equal(new StatusDefinitionId("doubt"), doubt.StatusDefinitionId);
        // The card's player takes it (Source, not the enemy side).
        Assert.Equal(CombatantTargetSelectors.Source.GetType(), doubt.TargetSelector.GetType());
    }

    // Queue-Crier Homunculus ("Lost Your Place", Panic cash-out): the passive is baked into its one pure
    // ATTACK intent — 7 damage +3 per Panic, capped +9, Panic not consumed (see ADAPTATIONS.md).
    [Fact]
    public void The_crier_bakes_lost_your_place_into_its_attack_intent()
    {
        var crier = Data.Enemies.Single(e => e.Id == "queue_crier_homunculus");
        Assert.Equal(31, crier.MaxHp);
        Assert.Null(crier.StartingStatuses); // pure scaling: no passive status, no intent rule
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
