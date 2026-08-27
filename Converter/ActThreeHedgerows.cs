using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 2 — The Surveyed Hedgerows. Where Stage 1 taught that a law has an author, Stage 2
// teaches that standing CHANGES the law.
public static partial class ActThree
{
    // ── Stage 2 — The Surveyed Hedgerows ──────────────────────────────────────────────────────────────────
    //
    // Where Stage 1 taught that a law has an author, Stage 2 teaches that standing CHANGES the law: a Claim
    // reverses what the Hedge measures, moves where the Boundary Stone says it moves, and cannot be taken off
    // the Hawthorn Tenant at all.

    public const string CurrentSurveyId = "current_survey";
    public const string SurveyedThisTurnId = "surveyed_this_turn";
    public const string WanderingTitleId = "wandering_title";
    public const string PriorDisputeId = "prior_dispute";
    public const string PriorPossessionId = "prior_possession";
    public const string OccupiedPlotId = "respect_the_occupied_plot";
    public const string PlotEnforcedThisTurnId = "plot_enforced_this_turn";

    // The Base Cost of the last card played this turn, plus one — so that zero can mean "no card yet" and a
    // free card can still be compared. Kept on the player, the one combatant every part of the program can
    // address with a single selector.
    public static CounterId LastBaseCostCounter => new("last_base_cost");

    // "Playing two consecutive cards with the same Base Cost applies 1 Trespass. When the Hedge gains a
    // Claim, reverse the law; each new Claim flips it again. One Trespass from this law per player turn."
    //
    // Only a Claim that was MADE flips the survey. A Claim handed to the Hedge by the Boundary Stone standing
    // next to it does not, which is the whole of the design's Encounter 6: the transferred political
    // landscape and the Hedge's own later Claims are different legal facts.
    public static StatusData CurrentSurvey()
    {
        var player = CombatantTargetSelectors.Source;
        var hedge = Lawgiver(CurrentSurveyId);

        // Base Cost of the card just played, plus one.
        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var sameAsLast = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, LastBaseCostCounter),
            ComparisonOperator.Equal,
            ThisCost());

        // The survey is reversed on every ODD Claim the Hedge has been granted.
        var reversed = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
            new RemainderExpression<CardPlayedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                    hedge, new StatusDefinitionId(ClaimCreatedId)),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(2)),
            ComparisonOperator.Equal,
            new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var violates = new OrExpression<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new NotExpression<CardPlayedTriggeredEffectContext>(reversed), sameAsLast),
            new AndExpression<CardPlayedTriggeredEffectContext>(
                reversed, new NotExpression<CardPlayedTriggeredEffectContext>(sameAsLast)));

        // The BREACH is not capped; the Hedge's own answer to it is. A second same-cost pair in one turn is
        // still a violation of the survey, and the meadow's witnesses answer violations.
        var measure = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                // There has to BE a previous card for two of them to be consecutive.
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, LastBaseCostCounter),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                violates),
            Violate<CardPlayedTriggeredEffectContext>(hedge, CurrentSurveyLaw, SurveyedThisTurnId));

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                measure,
                // …and whatever the survey made of it, this card is what the next one is measured against.
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, LastBaseCostCounter, ThisCost(), relative: false),
            ]));

        // A new turn is a new survey: nothing was played before the first card of it.
        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, LastBaseCostCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Lawgiver(CurrentSurveyId), new StatusDefinitionId(SurveyedThisTurnId)),
                ])));

        return Rule(CurrentSurveyId, "Current Survey",
            "Two cards in a row of the same Base Cost are a Trespass owed to the Reckoning Hedge — until the "
            + "Hedge is granted a Claim, after which two of DIFFERENT Base Cost are. Once a turn.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData SurveyedThisTurn() =>
        Marker(SurveyedThisTurnId, "Surveyed",
            "The hedge has already measured against you this turn.");

    // "Whenever this Stone gains a newly created Claim, it may pass one of its Claims to an ally holding
    // fewer." The Stone is the event's target, so this one is BEARER-scoped: the rule is about what happened
    // to its own wearer, and it fires from the Stone's own side of the fight.
    public static StatusData WanderingTitle() =>
        Rule(WanderingTitleId, "Wandering Title",
            "When the Boundary Stone is granted a Claim it passes one on to whichever neighbour holds fewer. "
            + "A Claim that changes hands is not a new one.",
            [
                // A merged status raises StatusApplied the first time and StatusMerged every time after, and
                // the second Claim is the first one this rule can actually act on — so it must hear both.
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    WanderingTitleProgram<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    WanderingTitleProgram<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);

    private static EffectProgram<TContext> WanderingTitleProgram<TContext>()
        where TContext : class
    {
        var stone = CombatantTargetSelectors.EventTarget;
        var neighbour = CombatantTargetSelectors.LowestStatusStacks(
            CombatantTargetSelectors.WithoutStatus(
                CombatantTargetSelectors.AllAlliesOfSource, new StatusDefinitionId(WanderingTitleId)),
            new StatusDefinitionId(ClaimId));

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    // A Claim being MADE, not one arriving from somewhere else.
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                    // …and only downhill: a title wanders towards whoever holds fewer of them.
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            neighbour, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.Less,
                        new CombatantStatusStacksExpression<TContext>(
                            stone, new StatusDefinitionId(ClaimId)))),
                TransferClaim<TContext>(stone, neighbour)));
    }

    // Encounter scaffolding, not a passive: in the two fights where Claim transfer is being TAUGHT, the Stone
    // already holds a Claim when the player arrives, and passes it on before anybody has done anything. It
    // spends itself doing so, which is why later appearances get no free Claim.
    public static StatusData PriorDispute()
    {
        var stone = Lawgiver(PriorDisputeId);

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    CreateClaim<TurnStartedTriggeredEffectContext>(stone),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        stone, new StatusDefinitionId(PriorDisputeId)),
                ])));

        return Rule(PriorDisputeId, "Prior Dispute",
            "There was already an argument here before you arrived: the Boundary Stone opens holding a Claim.",
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // "The first time each player turn the player attacks the Tenant while another living enemy has lower
    // current HP: 1 Trespass." Attacking the occupier while somebody weaker is standing right there is what
    // makes it a tenancy dispute rather than a fight.
    public static StatusData RespectTheOccupiedPlot()
    {
        // In a damage-received trigger the receiver is the event target and the attacker is the source.
        var tenant = CombatantTargetSelectors.EventTarget;
        var attacker = CombatantTargetSelectors.Source;
        var weakestNeighbour = CombatantTargetSelectors.LowestHealth(
            CombatantTargetSelectors.WithoutStatus(
                CombatantTargetSelectors.AllEnemiesOfSource, new StatusDefinitionId(OccupiedPlotId)));

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    // It has to be the PLAYER doing the attacking …
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                            attacker, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    // …and somebody weaker has to be standing right there. With nobody else on the field the
                    // read is zero, which is below the Tenant's health and would wrongly pass, so the
                    // neighbour must actually be alive and holding something.
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(
                                weakestNeighbour),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(
                                weakestNeighbour),
                            ComparisonOperator.Less,
                            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(tenant)))),
                Violate<DamageReceivedTriggeredEffectContext>(
                    tenant, OccupiedPlotLaw, PlotEnforcedThisTurnId)));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Lawgiver(OccupiedPlotId), new StatusDefinitionId(PlotEnforcedThisTurnId))));

        return Rule(OccupiedPlotId, "Respect the Occupied Plot",
            "Strike the Hawthorn Tenant while a weaker party is standing beside it and you owe the Tenant 1 "
            + "Trespass. Once a turn.",
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData PlotEnforcedThisTurn() =>
        Marker(PlotEnforcedThisTurnId, "Plot Enforced",
            "The Tenant has already objected to being struck this turn.");

    // "Claims belonging to the Hawthorn Tenant cannot be transferred away, copied, or consumed as the cost of
    // another party's ability. Others may still give it Claims."
    //
    // A prohibition on what OTHER rules may do is not a rule of its own: nothing in the engine asks the
    // Tenant's permission. It is a mark, and every rule in the act that moves or spends somebody else's Claim
    // reads it — `ActThree.ClaimsOthersMayTake` is the one selector they all go through.
    public static StatusData PriorPossession() =>
        Marker(PriorPossessionId, "Prior Possession",
            "This party's Claims cannot be taken, copied or spent by anybody else. It may still be given more.");

    // The parties whose Claims another rule is allowed to move or spend: everybody except whoever the fight
    // has already recognised as the sitting occupier.
    public static ICombatantTargetSelector ClaimsOthersMayTake(ICombatantTargetSelector among) =>
        CombatantTargetSelectors.WithoutStatus(among, new StatusDefinitionId(PriorPossessionId));
}
