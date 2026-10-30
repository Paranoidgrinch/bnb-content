using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stage 6 — The Corvée Yards. Compulsory labour, and the three things it does to the people in it.
//
//   The Rope-Gang Wraith is a dead work gang still pulling in step. It does not care that you are tired; it
//   cares that the RHYTHM broke — so the moment Fatigue actually takes Energy out of your hands, the gang
//   strains, and the next rope-snap carries that strain. Then the strain is spent.
//   The Runaway Laborer does not want to beat you. It wants out. It is the act's one non-lethal objective:
//   break the bracing that holds the gang together twice and it simply leaves, and the room is resolved.
//   The Stone-Hauler Ushabti performs its labour with perfect obedience and turns your surcharges into
//   building material. Every card you overpay for is a stone, and its blows carry them.
//
// The Wraith needed one thing this act had not yet written down: that Fatigue ACTUALLY took Energy. Losing a
// resource raises no event content can hear, and "the player has Fatigue" is not the same fact — a player at
// zero Energy loses nothing to it. So Fatigue itself writes the moment down (`energy_taken_by_fatigue`),
// exactly as the tax writes down its surcharges, and the gang keeps a bookmark in it.
public static partial class ActFour
{
    public const string RunawayEnemyId = "runaway_laborer";
    public const string UshabtiEnemyId = "stone_hauler_ushabti";

    public const string WorkStrainId = "work_strain";
    public const string EscapePlanId = "escape_plan";
    public const string EscapeId = "escape";
    public const string StoneWorkId = "stone_work";
    public const string StoneId = "stone";

    public const int EscapesToLeave = 2;

    public static CounterId StonesQuarried => new("stones_quarried");
    public static CounterId BraceAtTurnStart => new("brace_at_turn_start");

    public static EffectProgram<EnemyActionContext>? YardIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            _ => null,
        };

    // ── the Rope-Gang Wraith ──────────────────────────────────────────────────────────────────────────────

    // ── the Runaway Laborer ───────────────────────────────────────────────────────────────────────────────

    // How close the conscript is to being gone. Visible, because leaving is the objective the player is being
    // offered and they have to be able to see it coming.
    public static StatusData Escape() => new()
    {
        Id = EscapeId,
        NameKey = "Escape",
        DescriptionKey =
            "How far this conscript has got. At 2 it leaves the fight — and the room counts as resolved.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "The first time each player turn the player removes all Block from another enemy that had Block."
    //
    // Read as what it actually looks like from the yard: the gang was braced when your turn began, and it is
    // not braced now. That needs no damage bookkeeping at all — a combatant's Block is cleared at its OWN
    // turn start, so a brace that is gone by the end of YOUR turn is a brace you broke — and it is once per
    // player turn by construction rather than by a latch.
    public static StatusData EscapePlan() => new()
    {
        Id = EscapePlanId,
        NameKey = "Trying to Leave",
        DescriptionKey =
            "This conscript is not fighting you, it is leaving. Break the rest of the gang's Block during "
            + "your turn and it gets 1 Escape closer; at 2 it is gone.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(RememberTheBrace(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(BreakTheGang(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
        ],
    };

    // What the rest of the gang had standing when the player's turn began.
    private static EffectProgram<TurnStartedTriggeredEffectContext> RememberTheBrace() =>
        new(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            ItIsThePlayersTurn<TurnStartedTriggeredEffectContext>(),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Bearer(EscapePlanId), BraceAtTurnStart,
                new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                    TheRestOfTheGang<TurnStartedTriggeredEffectContext>(), StandardCombatIds.BlockDefensivePool),
                relative: false)));

    private static EffectProgram<TurnEndedTriggeredEffectContext> BreakTheGang()
    {
        var laborer = Bearer(EscapePlanId);
        var gang = TheRestOfTheGang<TurnEndedTriggeredEffectContext>();

        var wasBraced = new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(laborer, BraceAtTurnStart),
            ComparisonOperator.Greater,
            new ConstantExpression<TurnEndedTriggeredEffectContext>(0));

        var isBrokenNow = new AndExpression<TurnEndedTriggeredEffectContext>(
            // …and there is still somebody standing there to have been broken.
            new TargetExistsExpression<TurnEndedTriggeredEffectContext>(gang),
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                    gang, StandardCombatIds.BlockDefensivePool),
                ComparisonOperator.Equal,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    ItIsThePlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(wasBraced, isBrokenNow)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        laborer, new StatusDefinitionId(EscapeId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: laborer),

                    // …and at two, it is simply gone. Not killed — the room is resolved because the thing it
                    // was holding is no longer being held.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                laborer, new StatusDefinitionId(EscapeId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(EscapesToLeave)),
                        new SetCombatantLifecycleStateNode<TurnEndedTriggeredEffectContext>(
                            laborer, CombatantLifecycleState.Downed)),
                ])));
    }

    // ── the Stone-Hauler Ushabti ──────────────────────────────────────────────────────────────────────────

    // Building material, made out of your surcharges. Its blows carry the stones; it does not spend them,
    // which is what the appendix's cap is for.
    public static StatusData Stone() => new()
    {
        Id = StoneId,
        NameKey = "Stone",
        DescriptionKey =
            "Hauled out of what the bureaucracy made you overpay. This character's Stone Blow carries 3 more "
            + "damage per Stone, up to 9.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData StoneWork() => new()
    {
        Id = StoneWorkId,
        NameKey = "Compulsory Labour",
        DescriptionKey =
            "This worker takes 1 Stone for every card you paid a Burdened surcharge on since it last looked.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(QuarryTheSurcharge(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> QuarryTheSurcharge()
    {
        var ushabti = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(ushabti, BurdenPaid, StonesQuarried);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        ushabti, new StatusDefinitionId(StoneId), unread, sourceSelector: ushabti),

                    MoveTheBookmark<TurnStartedTriggeredEffectContext>(ushabti, BurdenPaid, StonesQuarried),
                ])));
    }

    // ── addressing the yard ───────────────────────────────────────────────────────────────────────────────

    // "The rest of the gang" — the Act-IV bodies on the Laborer's side that are not the Laborer. Read as a
    // scalar, because a brace is something one body is holding up.
    private static ICombatantTargetSelector TheRestOfTheGang<TContext>() where TContext : class =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithoutStatus(
                CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(LabyrinthBodyId)),
                new StatusDefinitionId(EscapePlanId)));

    // A turn rule that only means anything on the player's own turn — these two ride on TurnStarted and
    // TurnEnded for the whole field, and an enemy's turn boundary is not the window they describe.
    private static ICombatExpression<TContext, bool> ItIsThePlayersTurn<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));
}
