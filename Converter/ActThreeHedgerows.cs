using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 2 — The Hedgerows. Since the content cut of 2026-10-29: a sign that reads both ways, a tenant
// who owns your opening card, and a border that walks.
public static partial class ActThree
{
    public const string CurrentSurveyId = "current_survey";
    public const string SurveyedThisTurnId = "surveyed_this_turn";
    public const string SignSameCostId = "sign_same_cost";
    public const string SignDifferentCostId = "sign_different_cost";
    public const string WanderingTitleId = "wandering_title";
    public const string BeyondTheBorderId = "beyond_the_border";
    public const string BorderNextId = "border_next";
    public const string OccupiedPlotId = "respect_the_occupied_plot";

    // The Base Cost of the last card played this turn, plus one — so that zero can mean "no card yet" and a
    // free card can still be compared. Kept on the player.
    public static CounterId LastBaseCostCounter => CostMemory("survey");

    // ── Reckoning Hedge — the law sign ────────────────────────────────────────────────────────────────────
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

        // The sign the Hedge wears says which way the survey reads.
        var reversed = Wears<CardPlayedTriggeredEffectContext>(hedge, SignDifferentCostId);

        var violates = new OrExpression<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new NotExpression<CardPlayedTriggeredEffectContext>(reversed), sameAsLast),
            new AndExpression<CardPlayedTriggeredEffectContext>(
                reversed, new NotExpression<CardPlayedTriggeredEffectContext>(sameAsLast)));

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

        // Every Claim made for the Hedge turns its sign over. Bearer-scoped: the Claim lands on the Hedge.
        EffectProgram<TContext> Flip<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new ConditionalEffectNode<TContext>(
                    Wears<TContext>(CombatantTargetSelectors.EventTarget, SignDifferentCostId),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new RemoveStatusNode<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(SignDifferentCostId)),
                        new ApplyStatusNode<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(SignSameCostId),
                            new ConstantExpression<TContext>(1)),
                    ]),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new RemoveStatusNode<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(SignSameCostId)),
                        new ApplyStatusNode<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(SignDifferentCostId),
                            new ConstantExpression<TContext>(1)),
                    ]))));

        return Rule(CurrentSurveyId, "Current Survey",
            "The Hedge's sign names the law: two cards in a row of the same cost, or of different costs, owe "
            + "it 1 Trespass. Each Claim it gains flips the sign.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Flip<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Flip<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
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

    public static StatusData SignSameCost() =>
        Marker(SignSameCostId, "Sign: Same Cost",
            "Two cards in a row of the SAME cost owe the Hedge 1 Trespass.");

    public static StatusData SignDifferentCost() =>
        Marker(SignDifferentCostId, "Sign: Different Cost",
            "Two cards in a row of DIFFERENT cost owe the Hedge 1 Trespass.");

    // ── Hawthorn Tenant — the occupied plot ───────────────────────────────────────────────────────────────
    //
    // "Your first card each turn belongs to it; targeting anyone else is Trespass." A first card aimed at
    // nobody is not the Tenant's either, so it is the same breach.
    public static StatusData RespectTheOccupiedPlot()
    {
        var tenant = Lawgiver(OccupiedPlotId);
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        Alive<CardPlayedTriggeredEffectContext>(tenant),
                        new NotExpression<CardPlayedTriggeredEffectContext>(AimedAt(OccupiedPlotId)))),
                Violate<CardPlayedTriggeredEffectContext>(tenant, OccupiedPlotLaw)));

        return Rule(OccupiedPlotId, "The Occupied Plot",
            "Your first card each turn belongs to the Tenant. Aim it anywhere else, or at nobody, and you owe "
            + "it 1 Trespass.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // ── Errant Boundary Stone — the walking border ────────────────────────────────────────────────────────
    //
    // "Each turn it stands beside a different enemy, who is then beyond the border; attacking that enemy is
    // Trespass, but it takes double damage." The Stone counts itself among the neighbours, so in a pair the
    // border swings between the two of them.
    public static StatusData WanderingTitle()
    {
        var stone = Lawgiver(WanderingTitleId);
        var next = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(BeyondTheBorderId)));

        var walk = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    Alive<TurnStartedTriggeredEffectContext>(stone)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        next, new StatusDefinitionId(BorderNextId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    ClearFromParties<TurnStartedTriggeredEffectContext>(BeyondTheBorderId),
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.WithStatus(Parties, new StatusDefinitionId(BorderNextId)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BorderNextId)),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BeyondTheBorderId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        ])),
                ])));

        var cross = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    Alive<CardPlayedTriggeredEffectContext>(stone), AimedAt(BeyondTheBorderId)),
                Violate<CardPlayedTriggeredEffectContext>(stone, BorderLaw)));

        // With the Stone gone, nobody stands beyond any border.
        var fallen = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(WanderingTitleId)),
                ClearFromParties<CombatantDownedTriggeredEffectContext>(BeyondTheBorderId)));

        return Rule(WanderingTitleId, "The Walking Border",
            "Each turn the Stone moves the border to another party. Aiming at a party beyond the border: "
            + "1 Trespass, but it takes double damage.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    walk, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    cross, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("Downed", JsonSerializer.SerializeToElement(
                    fallen, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData BeyondTheBorder() => new()
    {
        Id = BeyondTheBorderId,
        NameKey = "Beyond the Border",
        DescriptionKey = "Takes double damage, but aiming a card at it owes the Boundary Stone 1 Trespass.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 200),
        ],
        Triggers = [],
    };

    public static StatusData BorderNext() =>
        Marker(BorderNextId, "Border Moving", "The border is about to move here.");
}
