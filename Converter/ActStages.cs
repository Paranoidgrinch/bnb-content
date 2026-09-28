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
        [1] = ["queue", "counter", "form", "seal", "ordinance", "delay", "appeal", "enforcement"],
        [2] = ["returns", "stacks", "catalogue", "reading_room", "redaction", "errata", "annex", "hours",
               "necrology", "concordance"],
        [3] = ["road", "hedgerows", "testimony", "tollwater", "covenants", "quorum", "appeals", "precedents",
               "jurisdictions", "court"],
        [4] = ["stelae", "gate", "granary", "basins", "causeway", "yards", "monument", "hall", "seal", "procession",
               "linen", "vaults", "warrens", "fixed_days", "cartouche", "balance", "sealed_court"],
    };

    // ── ELITES BELONG TO A STAGE TOO (user, 2026-09-28) ──────────────────────────────────────────────────────
    // "They always have a core mechanic that belongs to a stage, and should appear in that stage." Act I writes
    // the stage on the elite (`stage_delay` …). Acts II and III write it in the elite master as "after Standard
    // Stage N" — N is the stage whose mechanic the elite examines, so it stands in the stage right after the
    // lesson, N+1. Act IV writes an earliest depth (BabEncounter.EarliestDepthPercent); the elite stands in the
    // stage whose slice holds that depth. Source: source-data/design/…Master_Elite_Pool_Acts_I-IV_FINAL_AUDIT.md.
    public static readonly IReadOnlyDictionary<string, int> EliteStageAfterLesson = new Dictionary<string, int>
    {
        // Act II (§ Earliest recommended availability)
        ["archives_elite_after_hours_return_bell"] = 2,
        ["archives_elite_rolling_stacks_colossus"] = 3,
        ["archives_elite_catalogue_of_unwise_names"] = 3,
        ["archives_elite_silence_between_two_words"] = 4,
        ["archives_elite_black_ink_oracle"] = 5,
        ["archives_elite_volumes_of_cause_and_consequence"] = 6,
        ["archives_elite_drawer_of_infinite_returns"] = 6,
        ["archives_elite_presentless_clock"] = 7,
        ["archives_elite_obituary_with_three_endings"] = 7,
        // Act III (§ Earliest recommended appearance)
        ["green_docket_elite_stag_of_pre_approved_violence"] = 2,
        ["green_docket_elite_grandmother_web"] = 2,
        ["green_docket_elite_the_wrong_bridge_in_person"] = 3,
        ["green_docket_elite_great_toll_frog"] = 4,
        ["green_docket_elite_ant_queen_of_the_proper_line"] = 5,
        ["green_docket_elite_juniper_injunction"] = 6,
        ["green_docket_elite_surveyor_of_forgotten_paths"] = 7,
        ["green_docket_elite_three_reeds_of_appeal"] = 8,
        ["green_docket_elite_magistrate_of_thorns"] = 9,
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
