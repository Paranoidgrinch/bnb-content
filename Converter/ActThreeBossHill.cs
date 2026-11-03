using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 4 — **The Answering Hill** (374 HP).
//
// The kind of card you played most last turn is forbidden this turn: every one of them breaks its law (the
// forbidden kind is a chip on the Hill). At two thirds and at one third of its health the Hill answers: it hits
// you for 5 for every Claim it holds, and the Claims are spent.
//
// REWORKED 2026-11-03 (user, Act III pass): the Buried Claims, the stirring slope, the Crown and the rezoning are
// gone.
public static partial class ActThree
{
    public const string HillEnemyId = "the_answering_hill";
    public const string HillId = "the_landscape_has_standing";
    public const string ForbiddenDeedId = "hill_forbids_deeds";
    public const string ForbiddenWorkingId = "hill_forbids_workings";
    public const string ForbiddenRiteId = "hill_forbids_rites";
    public const string HillFirstAnswerId = "hill_answered_once";
    public const string HillSecondAnswerId = "hill_answered_twice";
    // Kept for BossPhases: the Hill's second half.
    public const string CrownOfTheHillId = HillSecondAnswerId;

    public const int AnswerPerClaim = 5;

    private static ICombatantTargetSelector Hill { get; } = Elite(HillId);

    private static IEnumerable<StatusData> HillStatuses() =>
    [
        TheLandscapeHasStanding(),
        Marker(ForbiddenDeedId, "Forbidden: Deeds", "You played Deeds most last turn. Every Deed this turn breaks its law."),
        Marker(ForbiddenWorkingId, "Forbidden: Workings", "You played Workings most last turn. Every Working this turn breaks its law."),
        Marker(ForbiddenRiteId, "Forbidden: Rites", "You played Rites most last turn. Every Rite this turn breaks its law."),
        Marker(HillFirstAnswerId, "The Hill Has Answered", "At two thirds of its health it answered: 5 damage per Claim."),
        Marker(HillSecondAnswerId, "The Hill Answers Again", "At one third of its health it answered again: 5 damage per Claim."),
    ];

    private static StatusData TheLandscapeHasStanding()
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Forbidden(string marker, string tag) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(Wearing<CardPlayedTriggeredEffectContext>(Hill, marker), PlayedIs(tag));
        var footing = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new OrExpression<CardPlayedTriggeredEffectContext>(
                Forbidden(ForbiddenDeedId, Cards.CardAuthoring.DeedTag),
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    Forbidden(ForbiddenWorkingId, Cards.CardAuthoring.WorkingTag),
                    Forbidden(ForbiddenRiteId, Cards.CardAuthoring.RiteTag))),
            Breach<CardPlayedTriggeredEffectContext>(Hill));

        // The player's turn ends: whatever kind was played most is forbidden next turn (Deeds over Workings over
        // Rites on a tie).
        ICombatExpression<TurnEndedTriggeredEffectContext, int> Played(string tag) =>
            new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, new TagId(tag));
        ICombatExpression<TurnEndedTriggeredEffectContext, bool> AtLeast(
            ICombatExpression<TurnEndedTriggeredEffectContext, int> a, ICombatExpression<TurnEndedTriggeredEffectContext, int> b) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(a, ComparisonOperator.GreaterOrEqual, b);
        IEffectNode<TurnEndedTriggeredEffectContext> Forbid(string marker) =>
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Hill, new StatusDefinitionId(marker), new ConstantExpression<TurnEndedTriggeredEffectContext>(1));
        var one = new ConstantExpression<TurnEndedTriggeredEffectContext>(1);
        var deeds = Played(Cards.CardAuthoring.DeedTag);
        var workings = Played(Cards.CardAuthoring.WorkingTag);
        var rites = Played(Cards.CardAuthoring.RiteTag);
        var forbid = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            PlayersTurn<TurnEndedTriggeredEffectContext>(),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Hill, new StatusDefinitionId(ForbiddenDeedId)),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Hill, new StatusDefinitionId(ForbiddenWorkingId)),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Hill, new StatusDefinitionId(ForbiddenRiteId)),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        AtLeast(deeds, one),
                        new AndExpression<TurnEndedTriggeredEffectContext>(AtLeast(deeds, workings), AtLeast(deeds, rites))),
                    Forbid(ForbiddenDeedId),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new AndExpression<TurnEndedTriggeredEffectContext>(AtLeast(workings, one), AtLeast(workings, rites)),
                        Forbid(ForbiddenWorkingId),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(AtLeast(rites, one), Forbid(ForbiddenRiteId)))),
            ]));

        // The Hill answers: 5 a Claim, and the standing is spent.
        var hill = CombatantTargetSelectors.EventTarget;
        var claims = new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(hill, new StatusDefinitionId(ClaimId));
        var answer = new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                Is(claims, ComparisonOperator.Greater, 0),
                new DealDamageNode<DamageReceivedTriggeredEffectContext>(Applicant,
                    new MultiplyExpression<DamageReceivedTriggeredEffectContext>(claims,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(AnswerPerClaim)),
                    kind: DamageKind.DamageOverTime)),
            new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(hill, new StatusDefinitionId(ClaimId)),
        ]);

        return Rule(HillId, "The Landscape Has Standing",
            "What you played most last turn is forbidden: each one breaks its law. At 2/3 and 1/3 health it hits you 5 per Claim.",
            [
                On("CardPlayed", footing, StatusTriggerScope.Anywhere),
                On("TurnEnded", forbid, StatusTriggerScope.Anywhere),
                On("DamageTaken", new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    OnceBelow(66, HillFirstAnswerId, answer),
                    OnceBelow(33, HillSecondAnswerId, answer),
                ])),
            ]);
    }

    private static EffectProgram<EnemyActionContext>? HillIntent(string enemyId, string intentId) => null;
}
