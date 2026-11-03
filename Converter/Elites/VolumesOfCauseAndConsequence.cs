using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── Volumes of Cause and Consequence (Act II elite) ───────────────────────────────────────────────────────
//
// Two volumes on separate stands with a Concordance line drawn between them: whatever damage one of them takes,
// the other gains as Block. Spread your blows and you build their walls; pick one and finish it.
//
// REWORKED 2026-11-03 (user, Act II trial): the citations, Supported/Unsupported Result and the survivor's
// Strength are gone — "einfach nur kompliziert und nicht spannend".
public static class VolumesOfCauseAndConsequence
{
    public const string CausesId = "volume_of_causes";
    public const string ConsequencesId = "volume_of_consequences";

    public const string TheCausesId = "the_volume_of_causes";
    public const string TheConsequencesId = "the_volume_of_consequences";

    private const string Concordance = "Damage dealt to one Volume becomes Block on the other.";

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    private static ICombatantTargetSelector Bearing(string marker) =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(marker));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Volume(TheCausesId, "Volume of Causes", other: TheConsequencesId),
        Volume(TheConsequencesId, "Volume of Consequences", other: TheCausesId),
    ];

    // The line itself, carried by each volume: the health its bearer just lost goes up on the other as Block.
    // (A blow the bearer's own Block stopped cost it nothing, and hands over nothing.)
    private static StatusData Volume(string id, string name, string other)
    {
        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                    new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), ComparisonOperator.Greater,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(Bearing(other),
                    new GainBlockNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>()))));
        return new StatusData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = Concordance,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
            ],
        };
    }

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            $"{CausesId}.establish_the_premise" => Program(new GainBlockNode<EnemyActionContext>(Self, Const(8))),
            $"{CausesId}.cause_without_warning" => Program(Damage(14)),
            $"{CausesId}.insert_a_false_premise" => Program(
                new ApplyStatusNode<EnemyActionContext>(Opponent, new StatusDefinitionId(Keywords.Doubt), Const(2))),
            $"{CausesId}.repeat_the_argument" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(10),
                new ApplyStatusNode<EnemyActionContext>(
                    Opponent, new StatusDefinitionId(Keywords.Paperwork), Const(1)),
            ])),

            $"{ConsequencesId}.enforce_the_result" => Program(Damage(17)),
            $"{ConsequencesId}.consequence_without_cause" => Program(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Damage(12),
                    new ApplyStatusNode<EnemyActionContext>(
                        Opponent, new StatusDefinitionId(Keywords.Doubt), Const(1)),
                ])),
            $"{ConsequencesId}.return_to_the_premise" => Program(
                new GainBlockNode<EnemyActionContext>(Self, Const(8))),
            // "Result Filed as Fact: BOTH volumes gain 14 Block."
            $"{ConsequencesId}.result_filed_as_fact" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ForEachTargetEffectNode<EnemyActionContext>(Bearing(TheCausesId),
                    new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(14))),
                new GainBlockNode<EnemyActionContext>(Self, Const(14)),
            ])),
            _ => null,
        };

    private static EffectProgram<EnemyActionContext> Program(IEffectNode<EnemyActionContext> body) => new(body);

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);
}
