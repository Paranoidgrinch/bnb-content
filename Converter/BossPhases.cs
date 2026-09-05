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

        // ── Act IV ────────────────────────────────────────────────────────────────────────────────────────
        //
        // The Pharaoh's three names ARE his three phases, and the exposure is the transition telegraphed one
        // turn early — the ward is empty and everybody can see it.
        ActFour.TwoLandsNameId,
        ActFour.EternalNameId,
        ActFour.NameExposedId,

        // The Weigher's second half, and the window that leads to it.
        ActFour.HeartRemembersId,
        ActFour.HeartDeclaredLightId,

        // The Architect's second half — the schedule that can no longer be reversed.
        ActFour.PlanAlwaysCorrectId,

        // The Lady's, and the two open-store turns that are its telegraph: the seals are all broken and
        // everybody can see what is coming.
        ActFour.GranariesOpenId,
        ActFour.FamineAccountingId,
        ActFour.PalimpsestId,
        ActFour.TextIsCanonId,
        ActFour.VesselsFullId,
        ActFour.ThreeJarsId,
        ActFour.LastPreparationId,

        // The Vizier's second half, the turn it is announced on, and his signature. The acting office is
        // not a phase either — but like the Curator's dial it says which body the telegraphed turn belongs
        // to, and it is read at the same moment.
        ActFour.MouthOpensNextId,
        ActFour.MouthHasOpenedId,
        ActFour.KingNotHereId,
        ActFour.ActingOfficeId,

        // The Queen's second half, the turn it is announced on, her signature — and the two readings of the
        // gauge that are not levels but ANNOUNCEMENTS: the black flood queued for her next action, and the
        // drift she shows one turn before it moves.
        ActFour.FloodStirsId,
        ActFour.FloodDisobeysId,
        ActFour.FloodCountedId,
        ActFour.WaterBlackId,
        ActFour.FloodDriftsId,

        // ── Act V ─────────────────────────────────────────────────────────────────────────────────────────
        //
        // Nisaba's two later phases and the announcement that precedes each. The Last Line is not only a
        // phase but the ONLY sentence left on the tablet, and the Indelible beside it is why the fight can
        // no longer be ended by hitting her — both belong beside the telegraph rather than in the chip row,
        // because a player reading the intent without them is reading a fight that is no longer happening.
        ActFive.LapisAnnouncedId,
        ActFive.LapisRecordId,
        ActFive.LastLineAnnouncedId,
        ActFive.LastLineId,
        ActFive.IndelibleId,

        // Inanna's two later phases and their announcements — and the Procession beside them, which is not a
        // phase but the clock the whole ledger is read against: a player who cannot see how many rounds are
        // left before collection cannot decide whether to pay at all.
        ActFive.StorehouseAnnouncedId,
        ActFive.StorehouseId,
        ActFive.AllThingsAnnouncedId,
        ActFive.AllThingsId,
        ActFive.ProcessionCalledId,
        ActFive.ProcessionId,
    ];
}
