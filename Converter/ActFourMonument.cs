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

    public const string PlacementId = "placement";
    public const string KeptOathId = "kept_oath";
    public const string BrokenOathId = "broken_oath";
    public const string FreshPigmentId = "fresh_pigment";
    public const string ComplaintId = "complaint";

    // How high the stone climbs before it comes down (the master: "a visible Placement sequence").
    public const int PlacementSteps = 3;

    private const int CapstoneDamage = 25;
    private const int CapstonePerEntombed = 4;
    private const int CapstoneCap = 12;

    public static EffectProgram<EnemyActionContext>? MonumentIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "fallen_capstone_golem.set_the_capstone" => SetTheCapstone(),
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

    // ── the Palette-Bearing Apprentice ────────────────────────────────────────────────────────────────────

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

}
