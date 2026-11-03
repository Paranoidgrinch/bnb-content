using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Bosses;

// ── The Auditor of Returned Lives (Act II boss, 288 HP) ───────────────────────────────────────────────────
//
// "The damage may be real. The record may still be insufficient."
//
// Every second turn of yours is an Audit: each card still in your hand when that turn ends costs you 3 HP. The
// Audit is announced on you for the whole turn it covers, so the turn is spent emptying the hand.
//
// REWORKED 2026-11-03 (user, Act II trial: "hat wieder einen eigenen Status ... den hab ich garnicht
// verstanden"): Supporting Documentation, Discrepancy, the Accounts, the citation and the Closing Audit are gone.
public static class AuditorOfReturnedLives
{
    public const string EnemyId = "auditor_of_returned_lives";

    public const string TheAuditorId = "the_auditor_of_returned_lives";
    public const string AuditDueId = "audit_due";

    public const int CostPerCard = 3;

    // Whether the Auditor's last turn called an Audit: they alternate.
    private static CounterId CalledCounter => new("auditor_called");

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    public static IEnumerable<StatusData> Statuses() => [TheAuditor(), AuditDue()];

    // At the end of every other turn of its own, the Auditor calls an Audit on the turn you are about to play.
    private static StatusData TheAuditor()
    {
        var called = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, CalledCounter);
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    called, ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        Across, new StatusDefinitionId(AuditDueId), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        Self, CalledCounter, new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                ]),
                @else: new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, CalledCounter, new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false)));
        return Rule(TheAuditorId, "The Auditor of Returned Lives", UsesStacks: false,
            "Every second turn is an Audit: each card left in your hand at turn end costs you 3 HP.",
            [Watch("TurnEnded", program)]);
    }

    // The Audit, on the player, for the turn it covers: the hand left at its end is charged, health and not
    // damage (Block does not answer an audit), and the Audit is closed.
    private static StatusData AuditDue()
    {
        var self = Self;
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new SetHealthNode<TurnEndedTriggeredEffectContext>(self,
                new SubtractExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(self),
                    new MultiplyExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(CostPerCard),
                        new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(self, CardZone.Hand)))),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(AuditDueId)),
        ]));
        return Rule(AuditDueId, "Audit", UsesStacks: false,
            "Each card still in your hand at the end of this turn costs you 3 HP.",
            [Watch("TurnEnded", program)], polarity: StatusPolarity.Debuff);
    }

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static StatusData Rule(
        string id, string name, bool UsesStacks, string description, IReadOnlyList<StatusTriggerData> triggers,
        StatusPolarity polarity = StatusPolarity.Neutral) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = UsesStacks,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
