using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 3 — **The Wrong Bridge in Person** (200 HP).
//
// Crossing costs a toll: your first hit each turn breaks its law, and it gains a Claim. At half its health the
// Bridge turns around — every Claim it holds comes down on it as 8 damage — and from then on your first hit each
// turn deals double.
//
// REWORKED 2026-11-03 (user, Act III pass): the two banks' Wergild, Return Standing and the settlements are gone.
// The turn is still one body passing its half rather than a second body, so the Claims it holds are there to fall.
public static partial class ActThree
{
    public const string WrongBridgeEnemyId = "the_wrong_bridge_in_person";
    public const string WrongBridgeId = "debt_before_passage";
    public const string TheOtherBankId = "the_other_bank";
    public const string OpenSpanId = "open_span";

    // Where the Bridge turns: half of its 200 as authored, scaled with whatever the act's health factor makes it.
    public const int BridgeTurnPercent = 50;
    public const int FallingClaimDamage = 8;

    private static ICombatantTargetSelector Bridge { get; } = Elite(WrongBridgeId);

    private static IEnumerable<StatusData> BridgeStatuses() =>
    [
        DebtBeforePassage(),
        Marker(TheOtherBankId, "The Other Bank", "The Bridge has turned: your first hit each turn deals double."),
        new StatusData
        {
            Id = OpenSpanId,
            NameKey = "Open Span",
            DescriptionKey = "Your next hit on the Bridge this turn deals double.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 200),
            ],
            Triggers = [],
        },
    ];

    private static StatusData DebtBeforePassage()
    {
        // In a damage program the struck body is the event's target, and the blow was the player's.
        var bridge = CombatantTargetSelectors.EventTarget;
        var byThePlayer = new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));
        var half = new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
            new CombatantMaxHealthExpression<DamageReceivedTriggeredEffectContext>(bridge),
            new ConstantExpression<DamageReceivedTriggeredEffectContext>(BridgeTurnPercent));
        var health = new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(bridge),
            new ConstantExpression<DamageReceivedTriggeredEffectContext>(100));
        var claims = new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(bridge, new StatusDefinitionId(ClaimId));

        var blow = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            byThePlayer,
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                // This bank: the first hit of a turn is the toll.
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new NotExpression<DamageReceivedTriggeredEffectContext>(
                        Wearing<DamageReceivedTriggeredEffectContext>(bridge, TheOtherBankId)),
                    Breach<DamageReceivedTriggeredEffectContext>(bridge)),
                // The other bank: the open span takes its double and closes.
                new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(bridge, new StatusDefinitionId(OpenSpanId)),
                // Half its health gone: it turns, and its standing falls on it.
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new NotExpression<DamageReceivedTriggeredEffectContext>(
                            Wearing<DamageReceivedTriggeredEffectContext>(bridge, TheOtherBankId)),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(health, ComparisonOperator.LessOrEqual, half)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            bridge, new StatusDefinitionId(TheOtherBankId), new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                        new SetHealthNode<DamageReceivedTriggeredEffectContext>(bridge,
                            new SubtractExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(bridge),
                                new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
                                    claims, new ConstantExpression<DamageReceivedTriggeredEffectContext>(FallingClaimDamage)))),
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(bridge, new StatusDefinitionId(ClaimId)),
                    ])),
            ]));

        // On the other bank, every turn of yours opens the span again.
        var open = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Wearing<TurnStartedTriggeredEffectContext>(Bridge, TheOtherBankId)),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Bridge, new StatusDefinitionId(OpenSpanId), new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

        return Rule(WrongBridgeId, "A Toll for Crossing",
            "Your first hit each turn breaks its law. At half health it turns: each Claim deals it 8, and your first hit each turn deals double.",
            [On("DamageTaken", blow), On("TurnStarted", open, StatusTriggerScope.Anywhere)]);
    }

    private static EffectProgram<EnemyActionContext>? BridgeIntent(string enemyId, string intentId) => null;
}
