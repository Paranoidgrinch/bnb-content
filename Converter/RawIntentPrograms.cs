using RogueDeck.Core.Combat;

namespace BnbContent.Converter;

// A few elite intents reach past the flat effect DSL: they touch a SPECIFIC other body (the Moth Cloud feeding
// the Waiting Room's Lost Time), which needs a parameterised selector the curated CombatNodeModel has no key
// for. Those intents are authored here as RAW EffectPrograms — the same escape EncounterPassives uses for
// cross-combatant reactions — and EnemyMapper prefers this program when one exists for "<enemy>.<intent>".
public static class RawIntentPrograms
{
    public static EffectProgram<EnemyActionContext>? For(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "minute_moth_cloud.steal_a_minute" => StealAMinute(),
            _ => null,
        };

    // "Gain 1 Lost Time for the Waiting Room": the Moth writes the track its partner keeps, capped at 3, found
    // through the marker the Room carries — so killing the Room really does erase the resource with it.
    private static EffectProgram<EnemyActionContext> StealAMinute()
    {
        var room = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.LostTimeLedgerId));
        var ledger = CombatantTargetSelectors.IterationTarget;

        return new EffectProgram<EnemyActionContext>(
            new ForEachTargetEffectNode<EnemyActionContext>(room,
                new SetCombatantCounterNode<EnemyActionContext>(ledger, PassiveStatuses.LostTimeCounter,
                    new MinExpression<EnemyActionContext>(
                        new AddExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(ledger, PassiveStatuses.LostTimeCounter),
                            new ConstantExpression<EnemyActionContext>(1)),
                        new ConstantExpression<EnemyActionContext>(PassiveStatuses.LostTimeMaximum)),
                    relative: false)));
    }
}
