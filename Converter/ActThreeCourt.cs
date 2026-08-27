using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 10 — The Court Beneath the Hill. The act's last standard stage, and its argument closing on
// itself: no new universal mechanic appears, and the difficulty is that reciprocity has become
// self-sustaining. A repeated name is both the guilt and the payment; a coin counts every exchange more
// clearly than the parties who made it.
public static partial class ActThree
{
    public const string NamesOnceSpokenId = "names_once_spoken";
    public const string NameHeardThisTurnId = "name_heard_this_turn";
    public const string AllClaimsHaveValueId = "all_claims_have_value";
    public const string TallyId = "tally";

    // The mark a card carries once it has been played here. The Keeper's law is about a name having been
    // spoken before, and a copy that has been played is the only record of a name this fight keeps.
    public static readonly TagId SpokenMark = new("spoken");

    private const int TallyPerFavour = 3;
    private const int CoinPaidInKind = 4;

    // ── Keeper of Buried Names ────────────────────────────────────────────────────────────────────────────

    // "The first time each turn you play a card whose name has already been spoken in this combat, you owe
    // the Keeper 1 Trespass."
    public static StatusData NamesOnceSpoken()
    {
        var player = CombatantTargetSelectors.Source;
        var keeper = Lawgiver(NamesOnceSpokenId);
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, SpokenMark),
                    Violate<CardPlayedTriggeredEffectContext>(
                        keeper, BuriedNamesLaw, NameHeardThisTurnId)),
                // Spoken once is spoken forever; the mark rides on the card wherever it goes.
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(player, played, SpokenMark),
            ]));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    keeper, new StatusDefinitionId(NameHeardThisTurnId))));

        return Rule(NamesOnceSpokenId, "Names Once Spoken",
            "The first card you play each turn that you have played before in this combat owes the Keeper of "
            + "Buried Names 1 Trespass. The same name is also worth double as restitution to it.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData NameHeardThisTurn() =>
        Marker(NameHeardThisTurnId, "Name Heard",
            "The Keeper has already recognised a name this turn.");

    // "A card offered to the Keeper whose name has already been spoken pays 2 Wergild instead of 1." The same
    // repetition that makes the guilt makes the restitution worth more — which is the paradox the Keeper is
    // given a solo encounter to teach.
    public static IEffectNode<CardPlayContext> BuriedNamesAsPayment(ICardInstanceExpression<CardPlayContext> offering) =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new CardInstanceHasMarkExpression<CardPlayContext>(offering, SpokenMark),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksFromSourceExpression<CardPlayContext>(
                        Applicant, new StatusDefinitionId(WergildId), Lawgiver(NamesOnceSpokenId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0))),
            PayOneWergild<CardPlayContext>());

    // "Buried Demand": the Keeper spends recognised standing to name a price.
    private static EffectProgram<EnemyActionContext> BuriedDemand()
    {
        var keeper = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        keeper, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<EnemyActionContext>(0)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ConsumeClaim<EnemyActionContext>(keeper),
                    DemandWergild<EnemyActionContext>(keeper, 2),
                ])));
    }

    // ── Handworn Tally Coin ───────────────────────────────────────────────────────────────────────────────

    // "Whenever any party actually SPENDS a Claim, the Coin gains a Tally. At 3 it spends them, the player is
    // granted 1 Safe-Conduct, and whichever party holds the fewest Claims is granted one."
    //
    // Spending is not moving and not losing: the Coin counts exactly the announcement the act keeps for it,
    // and nothing else. This is the loop the whole stage is about — Claim, expenditure, protection, new
    // Claim, restitution — running without anybody having to want it to.
    public static StatusData AllClaimsHaveValue()
    {
        var coin = Lawgiver(AllClaimsHaveValueId);

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimConsumedId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(
                        coin, new StatusDefinitionId(TallyId), new ConstantExpression<TContext>(1)),
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(
                                coin, new StatusDefinitionId(TallyId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TContext>(TallyPerFavour)),
                        new CausalSequenceEffectNode<TContext>(
                        [
                            new RemoveStatusNode<TContext>(coin, new StatusDefinitionId(TallyId)),
                            new ApplyStatusNode<TContext>(
                                Applicant, new StatusDefinitionId(SafeConductId),
                                new ConstantExpression<TContext>(1), sourceSelector: coin),
                            CreateClaim<TContext>(CombatantTargetSelectors.LowestStatusStacks(
                                Parties, new StatusDefinitionId(ClaimId))),
                        ])),
                ])));

        return Rule(AllClaimsHaveValueId, "All Claims Have Value",
            "Every Claim anybody spends is a Tally for the Handworn Tally Coin. At 3 it pays out: you are "
            + "granted 1 Safe-Conduct, and whichever party holds the fewest Claims is granted one.",
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

    public static StatusData Tally() => new()
    {
        Id = TallyId,
        NameKey = "Tally",
        DescriptionKey = "A notch for an exchange the Coin has seen. At 3 it pays out.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "Paid in Kind": whenever a demand is settled in full, the Coin loses 4 HP. It cannot be paid off and it
    // cannot be argued with; it can only be worn down by other people keeping their word.
    public static IEffectNode<TurnEndedTriggeredEffectContext> PaidInKind()
    {
        var coin = Lawgiver(AllClaimsHaveValueId);

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(coin),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            // Direct HP loss, not damage: no Block and no reaction sees a coin wearing thin.
            new SetHealthNode<TurnEndedTriggeredEffectContext>(
                coin,
                new SubtractExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(coin),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(CoinPaidInKind))));
    }

    public static EffectProgram<EnemyActionContext>? CourtIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "keeper_of_buried_names.buried_demand" => BuriedDemand(),
            _ => null,
        };
}
