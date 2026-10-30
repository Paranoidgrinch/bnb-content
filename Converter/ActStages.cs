namespace BnbContent.Converter;

// ── THE STAGES OF EACH ACT, IN THE ORDER THE DESIGN WALKS THEM ───────────────────────────────────────────────
// Source: source-data/design/Bureaucrats_and_Broomsticks_Master_Standard_Encounter_Pools_Acts_I-IV_FINAL_AUDIT.md
// ("## STAGE N — …" per act). Every standard fight carries its stage as a `stage_<name>` tag; until 2026-09-27
// nothing read it, and a fight of an act's last stage could open its first room (playtest: Enforcement in room 0).
//
// A stage is a share of the act's depth: stage k of S owns the k-th of S equal slices, and a fight of that stage
// may only stand there (MapGenerationSpec.EncounterMinimum/MaximumDepthPercent). The generator falls back to the
// nearest slice when a row's own is empty, never further.
public static class ActStages
{
    public static readonly IReadOnlyDictionary<int, IReadOnlyList<string>> Order = new Dictionary<int, IReadOnlyList<string>>
    {
        [1] = ["queue", "ordinance", "seal", "delay"],
        [2] = ["returns", "stacks", "catalogue", "necrology"],
        [3] = ["road", "hedgerows", "tollwater", "covenants"],
        [4] = ["stelae", "granary", "works", "linen", "necropolis", "two_truths"],
    };

    // ── ELITES BELONG TO A STAGE TOO (user, 2026-09-28) ──────────────────────────────────────────────────────
    // "They always have a core mechanic that belongs to a stage, and should appear in that stage." Act I writes
    // the stage on the elite (`stage_delay` …). Acts II and III write it in the elite master as "after Standard
    // Stage N" — N is the stage whose mechanic the elite examines, so it stands in the stage right after the
    // lesson, N+1. Act IV writes an earliest depth (BabEncounter.EarliestDepthPercent); the elite stands in the
    // stage whose slice holds that depth. Source: source-data/design/…Master_Elite_Pool_Acts_I-IV_FINAL_AUDIT.md.
    public static readonly IReadOnlyDictionary<string, int> EliteStageAfterLesson = new Dictionary<string, int>
    {
        // Act II, after the content cut of 2026-10-29: four stages, and each elite stands in the stage after the
        // lesson it examines (the last stage takes the last two).
        ["archives_elite_after_hours_return_bell"] = 1,
        ["archives_elite_drawer_of_infinite_returns"] = 1,
        ["archives_elite_rolling_stacks_colossus"] = 2,
        ["archives_elite_catalogue_of_unwise_names"] = 3,
        ["archives_elite_volumes_of_cause_and_consequence"] = 3,
        ["archives_elite_black_ink_oracle"] = 3,
        ["archives_elite_obituary_with_three_endings"] = 4,
        // Act III, after the content cut of 2026-10-29: two elites stand behind each of the first three lessons,
        // and the Magistrate behind the last.
        ["green_docket_elite_stag_of_pre_approved_violence"] = 1,
        ["green_docket_elite_ant_queen_of_the_proper_line"] = 1,
        ["green_docket_elite_grandmother_web"] = 2,
        ["green_docket_elite_surveyor_of_forgotten_paths"] = 2,
        ["green_docket_elite_great_toll_frog"] = 3,
        ["green_docket_elite_the_wrong_bridge_in_person"] = 3,
        ["green_docket_elite_magistrate_of_thorns"] = 4,
        // Act IV, after the content cut of 2026-10-30: six stages, ten elites, each behind the lesson it examines.
        ["labyrinth_elite_surveyor_of_the_errant_cord"] = 1,
        ["labyrinth_elite_sphinx_of_the_processional_measure"] = 1,
        ["labyrinth_elite_scarab_host_of_the_sealed_granary"] = 2,
        ["labyrinth_elite_rope_master_of_the_corvee"] = 3,
        ["labyrinth_elite_colossus_of_the_endless_procession"] = 3,
        ["labyrinth_elite_mummified_overseer_of_the_linen_house"] = 4,
        ["labyrinth_elite_curse_of_the_opened_tomb"] = 4,
        ["labyrinth_elite_the_tombbreakers_three"] = 5,
        ["labyrinth_elite_keeper_of_the_thirty_six_decans"] = 5,
        ["labyrinth_elite_ammit_devourer_of_the_unworthy"] = 5,
    };

    // The 1-based stage an elite stands in, or null when nothing names one.
    public static int? EliteStage(int act, string id, IReadOnlyList<string>? tags, int? earliestDepthPercent)
    {
        if (!Order.TryGetValue(act, out var stages))
            return null;
        if (StageOf(act, tags) is { } tagged)
            return tagged;
        if (EliteStageAfterLesson.TryGetValue(id, out var lesson))
            return Math.Min(lesson + 1, stages.Count);
        if (earliestDepthPercent is { } depth)
            return Math.Min(depth * stages.Count / 100 + 1, stages.Count);
        return null;
    }

    // The 1-based stage a `stage_<name>` tag names in this act, or null.
    public static int? StageOf(int act, IReadOnlyList<string>? tags)
    {
        if (!Order.TryGetValue(act, out var stages) || tags is null)
            return null;
        var stage = tags.FirstOrDefault(t => t.StartsWith("stage_", StringComparison.Ordinal))?["stage_".Length..];
        var index = stage is null ? -1 : stages.ToList().IndexOf(stage);
        return index < 0 ? null : index + 1;
    }

    // The slice of the act's depth a stage owns, in percent — (floor of its start, ceiling of its end), so two
    // neighbouring stages touch at the boundary rather than leaving a row between them that belongs to neither.
    public static (int From, int To)? Band(int act, IReadOnlyList<string>? tags) =>
        StageOf(act, tags) is { } stage ? Band(act, stage) : null;

    public static (int From, int To)? Band(int act, int stage)
    {
        if (!Order.TryGetValue(act, out var stages) || stage < 1 || stage > stages.Count)
            return null;
        var count = stages.Count;
        var index = stage - 1;
        return (index * 100 / count, ((index + 1) * 100 + count - 1) / count);
    }
}
