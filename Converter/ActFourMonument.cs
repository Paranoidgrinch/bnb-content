using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stages 7 and 8 — The Monument Works and The Hall of Reed and Ink. Two stages of one idea: the
// building remembers, and so does the ink.
//
//   The Fallen Capstone Golem is a stone that has ALREADY fallen and is still being officially installed.
//   Its Placement climbs where the player can see it, and when it tops out the capstone comes down — as hard
//   as the burial the player is already carrying makes it.
//   The Cornerstone Oath-Stone records whether the requirements were met while the monument was built. One
//   token a round: an oath kept, or an oath broken. Broken oaths are what its hammer swings with; a kept one
//   strikes a broken one off the record.
//   The Palette-Bearing Apprentice has fresh pigment, so the first entry of each round goes in heavier.
//   The Hieroglyphic Complaint Wall keeps grievances legally active for generations: whenever preservation
//   holds an affliction that should have faded, the wall has one more thing to carve.
//
// The Wall is also where §3.5 is binding — a solo body whose signature needs Embalmed must be able to create
// it itself — so `Preserve the Complaint` applies both halves: the affliction, and the preservation that
// stops it fading. Its complaint therefore never depends on another enemy being present.
public static partial class ActFour
{
    public const string CapstoneEnemyId = "fallen_capstone_golem";
    public const string OathStoneEnemyId = "cornerstone_oath_stone";
    public const string ApprenticeEnemyId = "palette_bearing_apprentice";
    public const string ComplaintWallEnemyId = "hieroglyphic_complaint_wall";

    public const string PlacementId = "placement";
    public const string FoundationOathId = "foundation_oath";
    public const string KeptOathId = "kept_oath";
    public const string BrokenOathId = "broken_oath";
    public const string FreshPigmentId = "fresh_pigment";
    public const string FreshPigmentRuleId = "fresh_pigment_rule";
    public const string ComplaintId = "complaint";
    public const string UndismissedComplaintId = "undismissed_complaint";

    // How high the stone climbs before it comes down (the master: "a visible Placement sequence").
    public const int PlacementSteps = 3;

    private const int CapstoneDamage = 25;
    private const int CapstonePerEntombed = 4;
    private const int CapstoneCap = 12;

    // The Oath-Stone's and the Wall's bookmarks.
    public static CounterId OathsRead => new("oaths_read");
    public static CounterId ComplaintsRead => new("complaints_read");

    public static EffectProgram<EnemyActionContext>? MonumentIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "fallen_capstone_golem.set_the_capstone" => SetTheCapstone(),
            "cornerstone_oath_stone.foundation_measure" => SetTheMeasure(13, Const(2)),
            _ => null,
        };

    // ── the Fallen Capstone Golem ─────────────────────────────────────────────────────────────────────────

    // How far the installation has got. Visible, and the whole point of the body: the player is meant to
    // count the stone down and decide what to do about the burial they are carrying before it lands.
    public static StatusData Placement() => new()
    {
        Id = PlacementId,
        NameKey = "Placement",
        DescriptionKey =
            "How far the capstone has been installed. At 3 it is set — and the stone falls as hard as the "
            + "burial you are already carrying makes it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The stone comes down, and the installation starts again — a capstone that has already fallen can
    // always be installed once more.
    private static EffectProgram<EnemyActionContext> SetTheCapstone() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    Const(CapstoneDamage),
                    new MinExpression<EnemyActionContext>(
                        new MultiplyExpression<EnemyActionContext>(
                            Const(CapstonePerEntombed),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                Applicant, new StatusDefinitionId(EntombedId))),
                        Const(CapstoneCap)))),

            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PlacementId)),
        ]));

    // ── the Cornerstone Oath-Stone ────────────────────────────────────────────────────────────────────────

    public static StatusData KeptOath() => new()
    {
        Id = KeptOathId,
        NameKey = "Kept Oath",
        DescriptionKey =
            "A requirement met while the monument was built. Each one recorded strikes a Broken Oath off the "
            + "foundation.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData BrokenOath() => new()
    {
        Id = BrokenOathId,
        NameKey = "Broken Oath",
        DescriptionKey =
            "A requirement missed while the monument was built. The foundation swings 4 harder for each of "
            + "them, up to 12.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // One token a round: the first compliance check the stone observes resolves as kept or broken. It reads
    // the same two tallies every other body in this act reads, through one bookmark in their sum — so a
    // measure raised by anybody at all is the one it records.
    public static StatusData FoundationOath() => new()
    {
        Id = FoundationOathId,
        NameKey = "Foundation Measure",
        DescriptionKey =
            "This stone records compliance: the first measure it sees resolve each round is written down as "
            + "a Kept Oath or a Broken Oath.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(RecordTheOath(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> RecordTheOath()
    {
        var stone = CombatantTargetSelectors.Source;
        var unread = ResolutionsSinceLastLooked<TurnStartedTriggeredEffectContext>(stone, OathsRead);

        // Which way the last resolution went: the record is 1 for an exact measure and more for a miss.
        var wasKept = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, MeasureResult),
            ComparisonOperator.Equal,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(1));

        var keep = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                stone, new StatusDefinitionId(KeptOathId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: stone),

            // …and compliance is recorded against the fault: one kept oath strikes one broken one off, which
            // is what "a Kept Oath may reduce a later hit" comes to on a hammer that swings by the record.
            new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                stone, new StatusDefinitionId(BrokenOathId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(-1)),
        ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    MoveTheResolutionBookmark<TurnStartedTriggeredEffectContext>(stone, OathsRead),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        wasKept,
                        keep,
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            stone, new StatusDefinitionId(BrokenOathId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: stone)),
                ])));
    }

    // ── the Palette-Bearing Apprentice ────────────────────────────────────────────────────────────────────

    // The pigment itself: a passive on the APPLYING side, so it thickens whatever this body writes into the
    // register and nothing else on the field.
    public static StatusData FreshPigment() => new()
    {
        Id = FreshPigmentId,
        NameKey = "Fresh Pigment",
        DescriptionKey = "The next entry this scribe makes in the register lands with 1 more stack.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.OutgoingStatusApplicationStacks,
                PassiveModifierOperation.AddFlat, 1, RestrictDamageKind: null,
                AppliesToStatusId: InscribedId),
        ],
        Triggers = [],
    };

    // …and the rule that grinds a fresh palette each round and spends it on the first entry this scribe
    // makes. Spent on ITS OWN entry: another body writing into the register does not use up this one's ink.
    public static StatusData FreshPigmentRule() => new()
    {
        Id = FreshPigmentRuleId,
        NameKey = "Grinds Fresh Pigment",
        DescriptionKey =
            "Each round this scribe grinds fresh pigment: its first Inscribed of the round lands with 1 more "
            + "stack.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(GrindPigment(), nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
            Trigger(SpendPigment<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied),
                StatusTriggerScope.Anywhere),
            Trigger(SpendPigment<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged),
                StatusTriggerScope.Anywhere),
        ],
    };

    // A round turning is nobody's own event, so the rule finds every scribe on the field itself.
    private static EffectProgram<RoundStartedTriggeredEffectContext> GrindPigment() =>
        new(new ForEachTargetEffectNode<RoundStartedTriggeredEffectContext>(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(FreshPigmentRuleId)),
            new ConditionalEffectNode<RoundStartedTriggeredEffectContext>(
                new NotExpression<RoundStartedTriggeredEffectContext>(
                    new IterationTargetHasStatusExpression<RoundStartedTriggeredEffectContext>(
                        new StatusDefinitionId(FreshPigmentId))),
                new ApplyStatusNode<RoundStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(FreshPigmentId),
                    new ConstantExpression<RoundStartedTriggeredEffectContext>(1),
                    sourceSelector: CombatantTargetSelectors.IterationTarget))));

    // On an application of the register, the applier's own pigment is spent. "source" in a status-application
    // trigger is whoever APPLIED it, which is exactly the body whose ink this is.
    private static EffectProgram<TContext> SpendPigment<TContext>() where TContext : class =>
        new(new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(InscribedId)),
                new TargetHasStatusExpression<TContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(FreshPigmentRuleId))),
            new RemoveStatusNode<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FreshPigmentId))));

    // ── the Hieroglyphic Complaint Wall ───────────────────────────────────────────────────────────────────

    public static StatusData Complaint() => new()
    {
        Id = ComplaintId,
        NameKey = "Complaint",
        DescriptionKey =
            "A grievance that has stayed legally active because it was never allowed to lapse. This wall's "
            + "accusations carry 2 more damage each, up to 8.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "Whenever Embalmed prevents a negative status from naturally decaying: gain 1 Complaint." Preservation
    // is a moment, not a state, so the fading point writes each one down and the wall keeps a bookmark in it.
    public static StatusData UndismissedComplaint() => new()
    {
        Id = UndismissedComplaintId,
        NameKey = "Undismissed",
        DescriptionKey =
            "Nothing on you is allowed to lapse: every affliction preservation holds in place gives this "
            + "wall one more Complaint.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(CarveTheComplaint(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> CarveTheComplaint()
    {
        var wall = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(wall, DecaysPreserved, ComplaintsRead);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        wall, new StatusDefinitionId(ComplaintId), unread, sourceSelector: wall),

                    MoveTheBookmark<TurnStartedTriggeredEffectContext>(wall, DecaysPreserved, ComplaintsRead),
                ])));
    }
}
