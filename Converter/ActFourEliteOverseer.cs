using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Mummified Overseer of the Linen House. Preservation read at boss grade: the body that
// measures how much of you is already wrapped.
//
// Embalmed holds a fading thing in place, and Stage 11 taught the player that being preserved is not a
// favour. The Overseer counts it. Every affliction preservation holds tightens the wrapping; every one that
// fades with nothing holding it loosens it again — so the player's answer to this body is to let things
// lapse, which is exactly what the rest of the act has been teaching them not to be able to afford.
//
// At four the second wrapping goes on: the Overseer braces, and up to two of the afflictions already on the
// player each gain a stack. It creates nothing new to fill an empty slot — a body with nothing on it has
// nothing to wrap tighter, and that is the whole reward for having stayed clean.
public static partial class ActFour
{
    public const string LinenOverseerEnemyId = "mummified_overseer_of_the_linen_house";

    public const string LinenHouseId = "the_linen_house";
    public const string WrappingId = "wrapping";

    public const int WrappingLimit = 4;
    private const int WrappingPerRound = 2;
    private const int SecondWrappingBlock = 24;

    // The Overseer's bookmarks in the two tallies the act's one fading point keeps.
    public static CounterId HeldRead => new("held_read");
    public static CounterId LapsedRead => new("lapsed_read");

    public static EffectProgram<EnemyActionContext>? LinenOverseerIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "mummified_overseer_of_the_linen_house.second_wrapping" => SecondWrapping(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> LinenOverseerStatuses() => [TheLinenHouse(), Wrapping()];

    // ── the linen house ───────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheLinenHouse() => new()
    {
        Id = LinenHouseId,
        NameKey = "The Linen House",
        DescriptionKey =
            "This overseer measures how much of you is already wrapped. Every affliction preservation holds "
            + "in place tightens the wrapping — at most twice a round — and every one you let lapse loosens "
            + "it. At 4 the second wrapping goes on.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(MeasureTheWrapping(), nameof(TriggerEvent.TurnStarted))],
    };

    public static StatusData Wrapping() => new()
    {
        Id = WrappingId,
        NameKey = "Wrapping",
        DescriptionKey =
            "How much of you the linen already holds. At 4 the overseer wraps you a second time — and lets "
            + "the wrapping out again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Both halves at the Overseer's own turn start, through one bookmark in each of the act's two fading
    // tallies — held and lapsed. The per-round ceiling on the tightening is the master's, and reading a
    // whole turn's worth at one moment is what makes it enforceable at all: a rule that fired per fade could
    // only count, never cap.
    private static EffectProgram<TurnStartedTriggeredEffectContext> MeasureTheWrapping()
    {
        var overseer = CombatantTargetSelectors.Source;
        var held = SinceLastLooked<TurnStartedTriggeredEffectContext>(overseer, DecaysPreserved, HeldRead);
        var lapsed = SinceLastLooked<TurnStartedTriggeredEffectContext>(overseer, DecaysUnpreserved, LapsedRead);

        var wrapping = new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
            overseer, new StatusDefinitionId(WrappingId));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // Tighten — at most two a round, and never past four.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        held, ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        overseer, new StatusDefinitionId(WrappingId),
                        new MinExpression<TurnStartedTriggeredEffectContext>(
                            new MinExpression<TurnStartedTriggeredEffectContext>(
                                held,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(WrappingPerRound)),
                            new SubtractExpression<TurnStartedTriggeredEffectContext>(
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(WrappingLimit),
                                wrapping)),
                        sourceSelector: overseer)),

                // …and loosen, one for each affliction that faded with nothing holding it.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        lapsed, ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                        overseer, new StatusDefinitionId(WrappingId),
                        new SubtractExpression<TurnStartedTriggeredEffectContext>(
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0), lapsed))),

                MoveTheBookmark<TurnStartedTriggeredEffectContext>(overseer, DecaysPreserved, HeldRead),
                MoveTheBookmark<TurnStartedTriggeredEffectContext>(overseer, DecaysUnpreserved, LapsedRead),
            ]));
    }

    // The signature. Two of the afflictions already on the player go one deeper — chosen by position, so the
    // pick is deterministic and a replay reproduces it — and NOTHING is created to fill an empty slot: a
    // player carrying one affliction has one wrapped tighter, and a player carrying none walks away with the
    // Block and no more.
    private static EffectProgram<EnemyActionContext> SecondWrapping()
    {
        IEffectNode<EnemyActionContext> Tighten(int index) =>
            new ModifySelectedStatusStacksNode<EnemyActionContext>(
                Applicant,
                new StatusSelectionSpec(StatusPolarityFilter.Debuff, StatusPick.First, index),
                Const(1));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(
                    CombatantTargetSelectors.Source, Const(SecondWrappingBlock)),

                Tighten(0),
                Tighten(1),

                new RemoveStatusNode<EnemyActionContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(WrappingId)),
            ]));
    }
}
