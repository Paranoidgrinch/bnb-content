using BnbContent.Converter.Bosses;

namespace BnbContent.Converter;

// WHICH BOSS THIS IS NOW.
//
// Every phased boss in this game rotates ONE intent list (the engine has no per-phase list, ADAPTATIONS), so a
// slot keeps its Phase-I name for the whole fight: the Warden still telegraphs "Inspect the Claim" while it is
// doing the Phase-II thing that slot means now. That reads as a bug, and the only thing that makes it read as
// the boss CHANGING instead is the phase marker — which until now was one chip among a dozen, filed after the
// stacks and the countdowns, nowhere near the telegraph it explains.
//
// So the phase says so about itself. These ids are tagged in the presentation manifest, and a frontend puts a
// tagged status where the phase belongs — beside what the boss is about to do — instead of in the chip row.
// Presentation is engine-ignored, so this changes no rule: it decides where a true thing is written.
//
// The list holds two sorts of marker, and both belong here: the phase a boss is IN, and the telegraph that it
// is about to change. A transition the player cannot see coming is the same problem one turn earlier.
public static class BossPhases
{
    public const string PhaseTag = "phase";

    public static readonly IReadOnlyList<string> Markers =
    [
        // ── Act I ─────────────────────────────────────────────────────────────────────────────────────────
        QueueCommissioner.FinalCounterId,          // the transition, telegraphed on the boss
        QueueCommissioner.PriorityQueueId,         // Phase II
        LordSealkeeper.UnsealPendingId,
        LordSealkeeper.UnsealedId,
        DeputyUndersecretary.ExecutiveId,
        MunicipalDragon.UnlicensedId,
        LivingCharter.ContradictoryId,

        // ── Act II ────────────────────────────────────────────────────────────────────────────────────────
        WhisperingCatalogue.SpeaksInFullId,        // Phase II: it predicts you twice a turn
        WhisperingCatalogue.FinalEntryId,          // the last phase
        WardenOfSealedVolumes.LockdownPendingId,
        WardenOfSealedVolumes.TotalLockdownId,
        WardenOfSealedVolumes.FinalReviewId,
        CuratorOfMisplacedHours.PresentRemovedPendingId,
        CuratorOfMisplacedHours.PresentRemovedId,
        AuditorOfReturnedLives.FormalReconciliationId,
        AuditorOfReturnedLives.ClosingAuditId,
        GrandCrossReference.ThesisPremiseId,       // Phase II is whichever Thesis the kill order chose
        GrandCrossReference.ThesisAuthorityId,
        GrandCrossReference.ThesisConclusionId,

        // The Curator's dial is not a phase, but it is the same kind of fact and it is read at the same
        // moment: it says which of three meanings the telegraphed action has.
        CuratorOfMisplacedHours.DialPresentId,
        CuratorOfMisplacedHours.DialFutureId,
        CuratorOfMisplacedHours.DialPastId,

        // ── Act III ───────────────────────────────────────────────────────────────────────────────────────
        ActThree.BoundaryPendingId,
        ActThree.CombinedJurisdictionId,
        ActThree.HeartwoodPendingId,
        ActThree.HeartwoodId,
        ActThree.StayLongerPendingId,
        ActThree.HouseholdLawId,
        ActThree.SlopeStirsPendingId,
        ActThree.SlopeAnswersPendingId,
        ActThree.SurveyedFaceId,
        ActThree.CrownStirsPendingId,
        ActThree.CrownBreaksPendingId,
        ActThree.CrownOfTheHillId,
        ActThree.CourtSessionPendingId,
        ActThree.CourtInSessionId,
        ActThree.GrantedNamePendingId,
        ActThree.SovereignReciprocityId,
    ];
}
