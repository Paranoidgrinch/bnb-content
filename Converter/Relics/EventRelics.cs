using RogueDeck.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The Event-exclusive relics, from `source-data/design/BnB_Final_Relics_Master_PostAudit.md` §5. Each has
// exactly ONE source — a single named branch of a single event — and appears nowhere else: not in a shop, not
// in a chest, not after a boss. The `Source` line on each is that branch, and it is what Phase D (the events
// themselves) will wire the grant to.
//
// Unlike the Shop pool, these are almost all rules of the FIGHT: small one-off Rites won by making a choice
// once. The rules live in EventRelicRules.
public static class EventRelics
{
    public static IReadOnlyList<BnbRelic> All() => [.. ActI];

    // ── Act I ─────────────────────────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<BnbRelic> ActI =
    [
        Event("originality_stamp", "Originality Stamp",
            "The Certified Copy Drawer — Take the certified instrument.",
            "Once per combat, the first non-Junk card you play is copied into your hand, and your next card "
            + "costs 1 less.",
            combatRule: EventRelicRules.OriginalityStamp),

        Event("unclaimed_property_tag", "Unclaimed Property Tag",
            "The Lost-and-Found Desk — Claim an unlabelled parcel.",
            "At the start of each combat, one card in your draw pile costs 1 less this fight.",
            combatRule: EventRelicRules.UnclaimedPropertyTag),

        Event("uncalled_ticket", "Uncalled Ticket",
            "The Waiting Token Exchange — Exchange three hours of waiting.",
            "Once per combat, a card still in your hand at the end of your turn goes back on top of your draw "
            + "pile; next turn you gain 1 Energy and draw 1.",
            combatRule: EventRelicRules.UncalledTicket),

        Event("threshold_ward", "Threshold Ward",
            "The Sealed Back Door — Respect the seal.",
            "Start each combat with 6 Block. The first time an enemy gains a positive status each combat, gain "
            + "1 Energy and 6 Block.",
            combatRule: EventRelicRules.ThresholdWard),

        // The only Event relic that is not a rule of a fight: it is a rule of the MAP. One step that ignores
        // the paths, handed over when the relic is taken and kept until it is actually used.
        Event("crossed_out_map", "Crossed-Out Map",
            "The Contradictory Map — Fold the map incorrectly.",
            "Once, you may walk to any node in the next row, ignoring the paths.",
            pickup: [new GrantUnrestrictedStepRunEffect()]),

        Event("inherited_bone_folder", "Inherited Bone Folder",
            "The Archive Window — Take the old tool.",
            "At the start of each combat, one card in your draw pile costs 1 less this fight, and you draw 1 "
            + "extra card.",
            combatRule: EventRelicRules.InheritedBoneFolder),
    ];
}
