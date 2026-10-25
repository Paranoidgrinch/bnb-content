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
        // once-per-turn and once-per-fight guards of enemy rules
        "eternal_cartouche_spent", "detour_recorded_this_turn", "surveyed_this_turn", "plot_enforced_this_turn",
        "testified_this_turn", "contested_this_turn", "quorum_failed_this_turn", "name_heard_this_turn",
        "measured_this_turn", "cited_this_turn", "appeal_taken_this_turn", "cup_poured_this_turn",
        "web_measured_this_turn", "line_broken_this_turn", "appeal_moved_this_turn", "counter_petition_used",
        "credit_drawn_this_turn",
        // the spent ambush footing (AmbushFooting): its chip would say only that the chip before it is gone
        "ambush_footing_spent",
    };
}
