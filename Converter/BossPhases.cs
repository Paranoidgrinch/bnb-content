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
        LordSealkeeper.CrackingId,
        LordSealkeeper.UnsealedId,
        DeputyUndersecretary.ExecutiveId,
        MunicipalDragon.NoticeNextId,
        // Act I's elites whose next move is a state the player plans around (content cut 2026-10-29).
        Elites.WaitingRoom.NowServingId,
        Elites.WaitingRoom.CalledId,
        Elites.WaitingRoom.AtTheCounterId,
        PassiveStatuses.WindowClosedId,
        PassiveStatuses.WindowOpenId,
        // …and Act II's: the Obituary's ending, the Drawer's open drawer, the Testator's testament.
        Elites.ObituaryWithThreeEndings.RespectablePhaseId,
        Elites.ObituaryWithThreeEndings.HeroicPhaseId,
        Elites.ObituaryWithThreeEndings.AccuratePhaseId,
        Elites.DrawerOfInfiniteReturns.DrawerOpenId,
        Testator.HeirsId,
        Testator.ClaimantId,
        Testator.DebtsId,

        // ── Act II ────────────────────────────────────────────────────────────────────────────────────────

        // The Curator's dial is not a phase, but it is the same kind of fact and it is read at the same
        // moment: it says which of three meanings the telegraphed action has.

        // ── Act III ───────────────────────────────────────────────────────────────────────────────────────
        ActThree.BothHeardId,
        ActThree.GrandmotherHalfId,
        ActThree.HillFirstAnswerId,
        ActThree.CrownOfTheHillId,
        ActThree.CourtTightenedId,

        // ── Act IV ────────────────────────────────────────────────────────────────────────────────────────
        //
        // Since the rework of 2026-11-04: the Pharaoh's three names ARE his phases, and a broken name is the turn
        // he loses; every other boss turns at half its health.
        ActFour.ThroneNameId,
        ActFour.TwoLandsNameId,
        ActFour.EternalNameId,
        ActFour.NameExposedId,
        ActFour.NameBrokenId,
        ActFour.HeartRemembersId,
        ActFour.PlanAlwaysCorrectId,
        ActFour.SeventhYearId,
        ActFour.PalimpsestId,
        ActFour.SelketWrappedId,
        ActFour.FloodDriftsId,
        // Anuket's black flood is not a phase but an announcement: her next action is already decided.
        ActFour.WaterBlackId,

        // ── Act V ─────────────────────────────────────────────────────────────────────────────────────────
        //
        // Nisaba (rework of 2026-11-05): the Lapis Record and the Final Line are her phases, and Indelible beside
        // them is why the fight can no longer be ended by hitting her.
        Gods.Nisaba.LapisRecordId,
        Gods.Nisaba.FinalLineWrittenId,
        Gods.Nisaba.IndelibleId,
        Gods.Nisaba.NotYetWrittenId,

        // Inanna: the seventh gate passed, the Queen of Battle.
        Gods.Inanna.QueenOfBattleId,

        // Nanshe: the Lean Years and the Measure Made Equal, and beside them the day of her Distribution, which says
        // which of her three moves the telegraph is.
        Gods.Nanshe.LeanYearsId,
        Gods.Nanshe.MeasureEqualId,
        Gods.Nanshe.DayId,

        // Nanna-Suen: the phase the moon is in and the one it turns to next — the four rules his telegraph is read
        // against — and the Radiance his orbits gather.
        .. Gods.NannaSuen.Ring.SelectMany(p => new[] { Gods.NannaSuen.PhaseId(p.Key), Gods.NannaSuen.NextId(p.Key) }),
        Gods.NannaSuen.RadianceId,

        // Utu: No Shadow and the Judge of the Dead, and the Witness his every number is read with.
        Gods.Utu.NoShadowId,
        Gods.Utu.JudgeOfTheDeadId,
        Gods.Utu.WitnessId,
        Gods.Utu.DeadWitnessId,

        // Enlil: the Noise of Mankind and the Flood, and beside them the decree in force and the one announced —
        // for this god the chips ARE the fight.
        Gods.Enlil.NoiseOfMankindId,
        Gods.Enlil.FloodId,
        .. Gods.Enlil.Ring.SelectMany(d => new[] { Gods.Enlil.DecreeId(d.Key), Gods.Enlil.NextId(d.Key) }),
    ];
}
