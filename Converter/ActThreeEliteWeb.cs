using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 2 — **The Old Web** (154 HP).
//
// Every card you play spins a thread (6 at most), and every Working you play cuts two. At the start of its turn
// the Web pulls them all tight: 3 damage a thread, one blow each, and the web is clear again. The threads are its
// own stacks, on its badge.
//
// REWORKED 2026-11-03 (user, Act III pass): the taut and slack Threads of law, the cutting by Safe-Conduct and the
// re-spinning are gone. "Hospitality Has Its Price" spins two threads instead of raising Wergild.
public static partial class ActThree
{
    public const string GrandmotherWebEnemyId = "grandmother_web";
    public const string GrandmotherWebId = "grandmother_web_threads";
    public const string WebThreadId = "web_thread";

    public const int WebThreadCeiling = 6;
    // A rule's damage, not an action's: the act's damage factor does not reach it.
    public const int WebThreadDamage = 3;
    public const int WorkingCuts = 2;

    private static ICombatantTargetSelector Web { get; } = Elite(GrandmotherWebId);

    private static IEnumerable<StatusData> WebStatuses() =>
    [
        GrandmotherWebThreads(),
        new StatusData
        {
            Id = WebThreadId,
            NameKey = "Threads",
            DescriptionKey = "At the start of its turn the Web pulls every thread tight: 3 damage each.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
        },
    ];

    private static ICombatExpression<TContext, int> Threads<TContext>() where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(Web, new StatusDefinitionId(WebThreadId));

    private static IEffectNode<TContext> Spin<TContext>(int threads) where TContext : class =>
        new RepeatEffectNode<TContext>(new ConstantExpression<TContext>(threads),
            new ConditionalEffectNode<TContext>(
                Is(Threads<TContext>(), ComparisonOperator.Less, WebThreadCeiling),
                new ApplyStatusNode<TContext>(Web, new StatusDefinitionId(WebThreadId), new ConstantExpression<TContext>(1))));

    private static StatusData GrandmotherWebThreads()
    {
        var play = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            Alive<CardPlayedTriggeredEffectContext>(Web),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                Spin<CardPlayedTriggeredEffectContext>(1),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    PlayedIs(Cards.CardAuthoring.WorkingTag),
                    new ModifyStatusStacksNode<CardPlayedTriggeredEffectContext>(
                        Web, new StatusDefinitionId(WebThreadId), new ConstantExpression<CardPlayedTriggeredEffectContext>(-WorkingCuts))),
            ]));

        // Its own turn start: every thread a blow, and the web is clear.
        var self = CombatantTargetSelectors.Source;
        var pull = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Is(new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(WebThreadId)),
                ComparisonOperator.Greater, 0),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RepeatEffectNode<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(WebThreadId)),
                    new DealDamageNode<TurnStartedTriggeredEffectContext>(Applicant,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(WebThreadDamage))),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(WebThreadId)),
            ]));

        return Rule(GrandmotherWebId, "The Old Web",
            "Every card you play spins a thread (6 at most); every Working cuts 2. On its turn each thread hits you for 3.",
            [On("CardPlayed", play, StatusTriggerScope.Anywhere), On("TurnStarted", pull)]);
    }

    private static EffectProgram<EnemyActionContext>? WebIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            $"{GrandmotherWebEnemyId}.hospitality_has_its_price" => new(Spin<EnemyActionContext>(2)),
            _ => null,
        };
}
