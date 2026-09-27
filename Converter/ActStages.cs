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

    // The slice of the act's depth a stage owns, in percent — (floor of its start, ceiling of its end), so two
    // neighbouring stages touch at the boundary rather than leaving a row between them that belongs to neither.
    public static (int From, int To)? Band(int act, IReadOnlyList<string>? tags)
    {
        if (!Order.TryGetValue(act, out var stages) || tags is null)
            return null;
        var stage = tags.FirstOrDefault(t => t.StartsWith("stage_", StringComparison.Ordinal))?["stage_".Length..];
        var index = stage is null ? -1 : stages.ToList().IndexOf(stage);
        if (index < 0)
            return null;
        var count = stages.Count;
        return (index * 100 / count, ((index + 1) * 100 + count - 1) / count);
    }
}
