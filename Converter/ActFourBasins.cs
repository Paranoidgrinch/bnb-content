using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stage 4 — The Floodmark Basins. The stage where a missed measure stops being an embarrassment and
// starts becoming a burial, and where the audit's §3.2 lands: a body may listen to a measure it does not own.
//
//   The Flood-Mark Reader owns its measure and answers the RESULT of any measure — one Entombed for every
//   resolution the player missed, wherever the demand came from.
//   The Drowned Field Scribe owns no measure at all. It reads how deep in silt the player already is, and
//   once they are deep enough its ink goes on twice as thick.
//   The Silt-Buried Farmer Shade keeps a clock instead of a consequence: every missed measure raises the
//   water one mark, and only when the water tops the bank does the field take you. Measuring correctly does
//   not undo the flood — it holds it where it is, which is the most this act ever offers.
//
// All three read the SAME running tallies the measure keeps (`measures_failed`, `measures_met`), each against
// its own bookmark. That is what "once per Weighed resolution" means in a game where several bodies may be
// listening: each takes the entries it has not answered yet, at its own turn start, in any order.
public static partial class ActFour
{
    public const string FarmerShadeEnemyId = "silt_buried_farmer_shade";

    public const string SiltedRecordId = "silted_record";
    public const string RisingFloodId = "rising_flood";
    public const string FloodId = "flood";

    // How deep in silt the player has to be before the Scribe's ink thickens (the master: "a visible
    // Entombed threshold", telegraphed). Three of the five that bury you.
    public const int SiltedRecordThreshold = 3;

    // How many marks the water climbs before it tops the bank. The Farmer arrives with the first mark already
    // standing — its own field was buried by this flood — so two missed measures finish what is already begun.
    public const int FloodSteps = 3;

    public static CounterId FailuresFlooded => new("failures_flooded");

    public static EffectProgram<EnemyActionContext>? BasinIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "silt_buried_farmer_shade.keep_the_furrow" => SetTheMeasure(11, Const(2)),
            _ => null,
        };

    // ── the Flood-Mark Reader ─────────────────────────────────────────────────────────────────────────────

    // ── the Drowned Field Scribe ──────────────────────────────────────────────────────────────────────────

    // ── the Silt-Buried Farmer Shade ──────────────────────────────────────────────────────────────────────

    // The water itself: a visible countdown that only ever rises on a missed measure. Keeping the furrow does
    // not lower it — nothing in this act gives anything back — it holds it where it stands, and that is what
    // the design means by "correct measurement can delay the burial".
    public static StatusData Flood() => new()
    {
        Id = FloodId,
        NameKey = "Flood",
        DescriptionKey =
            "How high the water stands on this field. Every measure you miss raises it 1; at 3 the field "
            + "takes you — 1 Entombed — and the water starts again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData RisingFlood() => new()
    {
        Id = RisingFloodId,
        NameKey = "Rising Flood",
        DescriptionKey =
            "This field is filling: each measure you miss raises the Flood by 1, and meeting one holds it "
            + "where it stands.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TheWaterRises(), nameof(TriggerEvent.TurnStarted)),
        ],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> TheWaterRises()
    {
        var farmer = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(farmer, MeasuresFailed, FailuresFlooded);

        // The water rises by ONE mark per cycle however many measures were missed in it: a flood is a clock,
        // not a tally.
        var rise = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            MoveTheBookmark<TurnStartedTriggeredEffectContext>(farmer, MeasuresFailed, FailuresFlooded),

            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                farmer, new StatusDefinitionId(FloodId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: farmer),

            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        farmer, new StatusDefinitionId(FloodId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(FloodSteps)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(EntombedId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: farmer),

                    // …and the water starts again from the bank, so the field can take you twice.
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        farmer, new StatusDefinitionId(FloodId)),
                ])),
        ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                rise));
    }
}
