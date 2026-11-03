using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 1 — **The Ombudsman of Root and Road** (342 HP).
//
// It hears two grounds of complaint, and they take turns:
//
//   The Road (odd rounds)  — two cards of the same cost in a row break its law.
//   The Root (even rounds) — playing more than 3 cards in a turn breaks its law.
//
// From half its health it hears both at once.
//
// REWORKED 2026-11-03 (user, Act III pass): the Claims paid in Gold and HP, the Counter-Petition and the Wergild
// are gone. "No Energy left over" was the Root's law until the user called it nonsense; it is the card count now.
public static partial class ActThree
{
    public const string OmbudsmanEnemyId = "ombudsman_of_root_and_road";
    public const string OmbudsmanId = "grounds_of_complaint";
    public const string RoadHeardId = "complaint_of_the_road";
    public const string RootHeardId = "complaint_of_the_root";
    public const string BothHeardId = "every_complaint_heard";

    public const int RootCardLimit = 3;

    private static ICombatantTargetSelector Ombudsman { get; } = Elite(OmbudsmanId);
    private static CounterId RoadMemory => CostMemory("ombudsman");

    private static IEnumerable<StatusData> OmbudsmanStatuses() =>
    [
        GroundsOfComplaint(),
        Marker(RoadHeardId, "The Road Is Heard", "Two cards of the same cost in a row break its law."),
        Marker(RootHeardId, "The Root Is Heard", "Playing more than 3 cards this turn breaks its law."),
        Marker(BothHeardId, "Every Complaint Heard", "From half its health both the Road and the Root are heard every round."),
    ];

    private static StatusData GroundsOfComplaint()
    {
        var player = CombatantTargetSelectors.Source;
        var odd = Is(new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new RoundNumberExpression<TurnStartedTriggeredEffectContext>(), new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
            ComparisonOperator.Equal, 1);
        IEffectNode<TurnStartedTriggeredEffectContext> Hear(string id, ICombatExpression<TurnStartedTriggeredEffectContext, bool> heard) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(heard,
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(Ombudsman, new StatusDefinitionId(id), new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                @else: new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Ombudsman, new StatusDefinitionId(id)));
        var both = Wearing<TurnStartedTriggeredEffectContext>(Ombudsman, BothHeardId);
        var turn = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(Ombudsman)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Hear(RoadHeardId, new OrExpression<TurnStartedTriggeredEffectContext>(odd, both)),
                Hear(RootHeardId, new OrExpression<TurnStartedTriggeredEffectContext>(new NotExpression<TurnStartedTriggeredEffectContext>(odd), both)),
                SetCounter<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.Source, RoadMemory, 0),
            ]));

        var costPlusOne = new AddExpression<CardPlayedTriggeredEffectContext>(PlayedCostValue, new ConstantExpression<CardPlayedTriggeredEffectContext>(1));
        var broken = new OrExpression<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wearing<CardPlayedTriggeredEffectContext>(Ombudsman, RoadHeardId),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    CounterOn<CardPlayedTriggeredEffectContext>(player, RoadMemory), ComparisonOperator.Equal, costPlusOne)),
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wearing<CardPlayedTriggeredEffectContext>(Ombudsman, RootHeardId),
                Is(PlayedThisTurn, ComparisonOperator.Greater, RootCardLimit)));
        var play = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            Alive<CardPlayedTriggeredEffectContext>(Ombudsman),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(broken, Breach<CardPlayedTriggeredEffectContext>(Ombudsman)),
                SetCounter<CardPlayedTriggeredEffectContext>(player, RoadMemory, costPlusOne),
            ]));

        var half = OnceBelow(50, BothHeardId, new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
        [
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                new StatusDefinitionId(RoadHeardId), new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                new StatusDefinitionId(RootHeardId), new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
        ]));

        return Rule(OmbudsmanId, "Grounds of Complaint",
            "Odd rounds: no two cards of one cost in a row. Even rounds: no more than 3 cards. From half health, both.",
            [On("TurnStarted", turn, StatusTriggerScope.Anywhere), On("CardPlayed", play, StatusTriggerScope.Anywhere), On("DamageTaken", half)]);
    }

    private static EffectProgram<EnemyActionContext>? OmbudsmanIntent(string enemyId, string intentId) => null;
}
