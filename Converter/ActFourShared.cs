using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV's intent dispatch and the few shapes every body shares. The bodies' own rules are in ActFourNormals,
// ActFourEliteBodies and ActFourBosses (reworked 2026-11-04). Weighed is no enemy's any more; only the act's
// events still hand it out.
public static partial class ActFour
{
    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            _ => NormalIntent(enemyId, intentId)
                 ?? EliteIntent(enemyId, intentId)
                 ?? BossIntent(enemyId, intentId),
        };

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // The player: the one combatant every fight marks as the applicant.
    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants,
                new StatusDefinitionId(PassiveStatuses.ApplicantId)));
}
