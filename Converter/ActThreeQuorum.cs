using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 6 — The Quorum Ring. No solo encounters: a quorum requires multiple parties, and that is
// the stage's whole argument. The Mushroom Circle owns a mandate somebody else may act under; the Bracken
// Moot hears every grievance whether or not anybody asked it to, and turns isolated claims into communal
// pressure.
public static partial class ActThree
{
    public const string QuorumRequiresDissentId = "quorum_requires_dissent";
    public const string QuorumFailedThisTurnId = "quorum_failed_this_turn";
    public const string CommonMandateId = "common_mandate";
    public const string CommonMandateGrantedId = "common_mandate_granted";
    public const string ClaimsAreHeardTogetherId = "claims_are_heard_together";
    public const string HearingId = "hearing";

    private const int HearingsPerFinding = 2;

    // ── Mandated Mushroom Circle ──────────────────────────────────────────────────────────────────────────

    // "If the player has played at least two non-Junk cards this turn and all of them share a card type,
    // apply 1 Trespass at the end of the turn." The Circle cannot legally act until its own procedure
    // recognises plurality, and it holds the player to the same standard.
    public static StatusData QuorumRequiresDissent()
    {
        var circle = Lawgiver(QuorumRequiresDissentId);

        ICombatExpression<TurnEndedTriggeredEffectContext, int> PlayedWithType(string tag) =>
            new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                Applicant, new TagId(tag));

        var realCards = new SubtractExpression<TurnEndedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant),
            PlayedWithType(Cards.CardAuthoring.JunkTag));

        // Every real card this turn was of one kind — whichever kind that turns out to be.
        ICombatExpression<TurnEndedTriggeredEffectContext, bool> AllOf(string tag) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                PlayedWithType(tag), ComparisonOperator.Equal, realCards);

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            realCards, ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                        new OrExpression<TurnEndedTriggeredEffectContext>(
                            AllOf(Cards.CardAuthoring.DeedTag),
                            new OrExpression<TurnEndedTriggeredEffectContext>(
                                AllOf(Cards.CardAuthoring.WorkingTag),
                                AllOf(Cards.CardAuthoring.RiteTag))))),
                Violate<TurnEndedTriggeredEffectContext>(circle, QuorumLaw, QuorumFailedThisTurnId)));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    circle, new StatusDefinitionId(QuorumFailedThisTurnId))));

        return Rule(QuorumRequiresDissentId, "Quorum Requires Dissent",
            "End a turn in which you played two or more real cards and every one of them was the same kind, "
            + "and you owe the Mushroom Circle 1 Trespass. A quorum requires disagreement.",
            [
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData QuorumFailedThisTurn() =>
        Marker(QuorumFailedThisTurnId, "Quorum Noted",
            "The Circle has already recorded a turn without dissent.");

    // "Another living enemy with no Claim may spend 1 Claim belonging to the Circle to pay the Claim cost of
    // its own ability. The Claim remains the Circle's until consumed. This is consumption, not transfer."
    //
    // The mandate is the licence, not the standing: nothing changes hands. The Circle spends one of its own
    // Claims and its neighbour is, for that moment, a party with standing — and whatever that neighbour's
    // standing lets it do, it does. Each identity that has such an ability answers this marker as well as
    // the Claim itself.
    public static StatusData CommonMandate()
    {
        var circle = Lawgiver(CommonMandateId);
        var mandated = CombatantTargetSelectors.LowestStatusStacks(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(CommonMandateId)),
            new StatusDefinitionId(ClaimId));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    // The Circle's own turn, and it has a mandate to lend.
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(CommonMandateId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                circle, new StatusDefinitionId(ClaimId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        // …and a neighbour with none of its own to act on.
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                mandated, new StatusDefinitionId(ClaimId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)))),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    ConsumeClaim<TurnStartedTriggeredEffectContext>(circle),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        mandated, new StatusDefinitionId(CommonMandateGrantedId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                ])));

        return Rule(CommonMandateId, "Common Mandate",
            "Once a turn the Mushroom Circle spends one of its own Claims so that a neighbour holding none "
            + "may act as a party with standing. The Claim is spent, not handed over.",
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    public static StatusData CommonMandateGranted() =>
        Marker(CommonMandateGrantedId, "Acting on the Mandate",
            "This party is acting under the Mushroom Circle's communal standing rather than its own.");

    // ── The Bracken Moot ──────────────────────────────────────────────────────────────────────────────────

    // "Whenever ANOTHER party is granted a Claim, the Moot gains 1 Hearing. At 2 Hearings, they are spent and
    // whichever party holds the MOST Claims is granted another. Transfers generate no Hearings."
    //
    // The Moot turns isolated grievances into communal political pressure, and it hears only what was
    // GRANTED — a Claim that merely changed hands is not a grievance anybody brought.
    public static StatusData ClaimsAreHeardTogether()
    {
        var moot = Lawgiver(ClaimsAreHeardTogetherId);

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                    // …to somebody else. The Moot does not hear its own grievances.
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(ClaimsAreHeardTogetherId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TContext>(0))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(
                        moot, new StatusDefinitionId(HearingId), new ConstantExpression<TContext>(1)),
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(
                                moot, new StatusDefinitionId(HearingId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TContext>(HearingsPerFinding)),
                        new CausalSequenceEffectNode<TContext>(
                        [
                            new RemoveStatusNode<TContext>(moot, new StatusDefinitionId(HearingId)),
                            CreateClaim<TContext>(CombatantTargetSelectors.HighestStatusStacks(
                                Parties, new StatusDefinitionId(ClaimId))),
                        ])),
                ])));

        return Rule(ClaimsAreHeardTogetherId, "Claims Are Heard Together",
            "Every Claim granted to somebody else is a Hearing for the Bracken Moot. At 2 Hearings it finds "
            + "for whichever party already holds the most, and grants them another.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData Hearing() => new()
    {
        Id = HearingId,
        NameKey = "Hearing",
        DescriptionKey = "A grievance the Moot has heard. At 2 it makes a finding.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
