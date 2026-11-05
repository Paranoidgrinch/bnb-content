namespace BnbContent.Converter;

// STATUSES THE PLAYER READS IN THE LOG, NOT ON A BODY (playtest 3, B4 and B5). Two kinds:
//   • a character's own standing tag (The Applicant, the Lieutenant's command, the pot and the wand, which have
//     pictures of their own) — carried in every fight, it says nothing about this one;
//   • the engine's bookkeeping: marks that remember who struck whom last round, what was just spent, which enemy
//     already answered this turn. Cards and rules ask them; the player is told what they did, in the fight's log.
// The status keeps its name and text (the log names it); only its chip goes. The frontend reads the tag.
public static class LogOnlyStatuses
{
    public const string Tag = "log_only";

    public static readonly IReadOnlySet<string> Ids = new HashSet<string>(StringComparer.Ordinal)
    {
        // character tags
        "the_applicant", "lt_command", "hedge_witch_cauldron", "apprentice_wand",
        // the Bureaucrat's record of the past
        "attacked_this_round", "attacked_last_round", "queue_resolved", "junk_filed",
        // the Lieutenant's pulses and marks
        "lt_pulse_strength", "lt_pulse_authority", "lt_pulse_remand", "lt_pulse_pardon", "lt_pulse_redress",
        "lt_pulse_abandon", "lt_mark_double_file", "lt_mark_last_won", "lt_mark_guard_striker",
        // between-fight markings carried into a fight
        "act_one_markings", "act_two_markings",
        // Act III's shared law, explained once by the tutorial (moment act3.docket) instead of on every body
        "green_docket_body",
        // Act IV's shared law, explained once by the tutorial (moment act4.labyrinth)
        "labyrinth_body",
        // once-per-turn and once-per-fight guards of enemy rules
        "eternal_cartouche_spent", "border_next",
        // Act IV's machinery: which bodies weigh, a rise in progress, the jar under every jar's own name
        "weighs_your_turn", "rising", "canopic_jar", "natron_pending", "capstone_pending", "door_pending",
        // …and its elites': whose tool is whose, a tool just swung, a coil already given this turn
        "owns_pry_bar", "owns_knife", "owns_idol", "used_a_tool", "apep_loosened",
        // …and its bosses': the vessel under every vessel's own name
        "selket_vessel",
        // the Seizure Procession's machinery: the Lantern's rule says the whole of it in one sentence (2026-11-01)
        "lock_cart", "seizure_marshal",
        // the Sealed Door Ward's per-turn dampener: its One Remaining Seal says the whole rule (2026-11-01)
        "seal_intact",
        // the Fading Number Token's +3 while you are Fatigued: its rule says it (2026-11-01)
        "fading_edge",
        // the spent ambush footing (AmbushFooting): its chip would say only that the chip before it is gone
        "ambush_footing_spent",
    };
}
