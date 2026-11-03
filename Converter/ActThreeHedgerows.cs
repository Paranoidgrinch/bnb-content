using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 2 — The Hedgerows. Since 2026-11-03 (user): a hedge that minds your prices, a tenant who wants
// tending, and a border that walks.
public static partial class ActThree
{
    public const string CurrentSurveyId = "current_survey";
    public const string WanderingTitleId = "wandering_title";
    public const string BeyondTheBorderId = "beyond_the_border";
    public const string BorderNextId = "border_next";
    public const string OccupiedPlotId = "respect_the_occupied_plot";

    // The Base Cost of the last card played this turn, plus one — so that zero can mean "no card yet" and a
    // free card can still be compared. Kept on the player.
    public static CounterId LastBaseCostCounter => CostMemory("survey");

    // ── Reckoning Hedge — the same price twice ────────────────────────────────────────────────────────────
    //
    // Since 2026-11-03 (user): two cards of the same cost in a row break its law. The sign that flipped is gone.
    // The last cost is remembered on the player, plus one, so that zero means "nothing played yet this turn".
    public static StatusData CurrentSurvey()
    {
        var player = CombatantTargetSelectors.Source;
        return LawOnPlay(CurrentSurveyId, "The Same Price Twice",
            "Two cards of the same cost in a row break its law.",
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                CounterOn<CardPlayedTriggeredEffectContext>(player, LastBaseCostCounter), ComparisonOperator.Equal,
                new AddExpression<CardPlayedTriggeredEffectContext>(PlayedCostValue, new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
            also: SetCounter<CardPlayedTriggeredEffectContext>(player, LastBaseCostCounter,
                new AddExpression<CardPlayedTriggeredEffectContext>(PlayedCostValue, new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
            atTurnStart: SetCounter<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.Source, LastBaseCostCounter, 0));
    }

    // ── Hawthorn Tenant — the occupied plot ───────────────────────────────────────────────────────────────
    //
    // Since 2026-11-03 (user): the Tenant wants tending. A turn of yours that puts no status on it breaks its law
    // (it fights alone, so "aim your first card at it" was no law at all). A status put on it — new or added to —
    // marks it Tended for the turn.
    public const string TendedId = "tenant_tended";

    public static StatusData RespectTheOccupiedPlot()
    {
        var tenant = Lawgiver(OccupiedPlotId);
        IEffectNode<TContext> Tend<TContext>() where TContext : class =>
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    new NotExpression<TContext>(
                        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(TendedId)))),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.EventTarget, new StatusDefinitionId(TendedId),
                    new ConstantExpression<TContext>(1)));
        var reckon = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                Alive<TurnEndedTriggeredEffectContext>(tenant)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new NotExpression<TurnEndedTriggeredEffectContext>(Wearing<TurnEndedTriggeredEffectContext>(tenant, TendedId)),
                    Breach<TurnEndedTriggeredEffectContext>(tenant)),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(tenant, new StatusDefinitionId(TendedId)),
            ]));
        return Rule(OccupiedPlotId, "Tend the Plot",
            "End a turn without putting any status on it and you break its law.",
            [
                On("StatusApplied", Tend<StatusAppliedTriggeredEffectContext>()),
                On("StatusMerged", Tend<StatusMergedTriggeredEffectContext>()),
                On<TurnEndedTriggeredEffectContext>("TurnEnded", reckon, StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData Tended() =>
        Marker(TendedId, "Tended", "You put a status on the Tenant this turn: its law is kept.");

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
            "Each turn it moves the border to another enemy: that one takes double damage, but hitting it breaks the Stone's law.",
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
        DescriptionKey = "Takes double damage, but aiming a card at it breaks the Boundary Stone's law.",
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
