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
    public static IReadOnlyList<BnbRelic> All() => [.. ActI, .. ActII, .. ActIII, .. ActIV];

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

    // ── Act II ────────────────────────────────────────────────────────────────────────────────────────────
    //
    // The archives' five are all about ONE card: the one you did not get to play, the one you kept back, the
    // one that will not stay filed. Each marks a copy at the right moment and answers for it afterwards.

    public static readonly IReadOnlyList<BnbRelic> ActII =
    [
        Event("unreturned_library_card", "Unreturned Library Card",
            "The Perpetual Borrower — Pocket the borrower's library card.",
            "Once each combat, a card you end your turn still holding is waiting in your hand next turn, and "
            + "it is free.",
            combatRule: ActTwoEventRelicRules.UnreturnedLibraryCard),

        Event("reversible_shelf_label", "Reversible Shelf Label",
            "The Reciprocal Shelf — Take the loose shelf label.",
            "Once each combat, the label remembers a card you put down unplayed. When it next reaches your "
            + "hand you draw 1 and it costs 1 less.",
            combatRule: ActTwoEventRelicRules.ReversibleShelfLabel),

        Event("blank_cameo", "Blank Cameo",
            "The Redacted Portrait — Restore the missing face.",
            "At each combat's opening one card in your hand is kept, costs 1 less, and cannot be marked by "
            + "the archive.",
            combatRule: ActTwoEventRelicRules.BlankCameo),

        Event("vow_bead", "Vow Bead",
            "The Last Quiet Table — win without breaking the Vow.",
            "End a turn having played exactly 3 non-Junk cards and your next turn gains 1 Energy and 1 card.",
            combatRule: ActTwoEventRelicRules.VowBead),

        Event("inverted_sealstone", "Inverted Sealstone",
            "The Inward Seal — Break the seal outward.",
            "At each combat's opening one Deed or Working is sealed: the first time you play it, it comes "
            + "back to your hand.",
            combatRule: ActTwoEventRelicRules.InvertedSealstone),
    ];

    // ── Act III ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // The Green Docket's five are about the SHAPE of a turn — how many things you did, and in what order —
    // which is the question every Local Law in the act asks. Four of them are rules of a fight; the brooch
    // is a rule of the road.

    public static readonly IReadOnlyList<BnbRelic> ActIII =
    [
        Event("mootcap", "Mootcap",
            "Moonlit Mushrooms — Step inside the circle.",
            "The third real card you play each turn is put to the circle, and it answers: 10 Block, a card, "
            + "or 7 damage to everything standing.",
            combatRule: ActThreeEventRelicRules.Mootcap),

        Event("dissenting_spore", "Dissenting Spore",
            "Moonlit Mushrooms — Wait for quorum, and win without breaking it.",
            "An odd turn grows a spore and an even one costs you one. At three, the ring speaks: 1 Energy, "
            + "an extra card and 6 Block.",
            combatRule: ActThreeEventRelicRules.DissentingSpore),

        Event("antway_marker", "Antway Marker",
            "The Ant Queue — Walk with the proper line, and win cleanly.",
            "Three real cards in a row, none cheaper than the one before: the third is worth 1 Energy and a "
            + "card. Step out of order and the line is broken for the turn.",
            combatRule: ActThreeEventRelicRules.AntwayMarker),

        Event("complaint_leaf", "Complaint Leaf",
            "The Ombudsman's Warning — Keep the leaf.",
            "The first party to lay a hand on you is named the Respondent. While it is standing, one card in "
            + "your hand each turn costs 1 less.",
            combatRule: ActThreeEventRelicRules.ComplaintLeaf),

        // ADAPTATION: "once per Event, reduce one explicit Gold/HP option cost by 25%" has no engine face —
        // an event's costs are settled by the door, not by what the traveller is carrying. The brooch is
        // guest-right instead, which is what it IS: the road looks after somebody who has been welcomed, and
        // every fight opens a little kinder.
        Event("guest_right_brooch", "Guest-Right Brooch",
            "The Kindly Procession — Walk three steps with them.",
            "You have been welcomed. Every combat opens with 8 Block and 1 Safe-Conduct.",
            combatRule: ActThreeEventRelicRules.GuestRightBrooch),
    ];

    // ── Act IV ────────────────────────────────────────────────────────────────────────────────────────────
    //
    // The Licensing Labyrinth's five — the ones its first ten doors hand over. Every one of them is a reading
    // of one of the act's own five words: what a turn COST, what is held in place against its own fading,
    // what the register does to the first thing entered in it, what leaving completely is worth, and what a
    // missed measure is answered with. The other four arrive with events 11–20.

    public static readonly IReadOnlyList<BnbRelic> ActIV =
    [
        Event(ActFourEventRelicRules.CupId, "Cup of the Lowest Mark",
            "The Dry Nilometer — Accept the True Level.",
            "The first turn each fight you end with exactly one Energy unspent: heal 4, and one more card at "
            + "your next hand.",
            combatRule: ActFourEventRelicRules.CupOfTheLowestMark),

        Event(ActFourEventRelicRules.KnotId, "Red Linen Knot",
            "The Red Linen Procession — Follow Until the Last Gate.",
            "Every fight opens with 8 Block and one Embalmed, and the first time the linen holds something "
            + "in place you are wrapped again for 8.",
            combatRule: ActFourEventRelicRules.RedLinenKnot),

        Event(ActFourEventRelicRules.CartoucheId, "Blank Cartouche",
            "The Nameless Cartouche — Take the Fragment.",
            "An extra card in your first hand. The first Inscribed you gain each fight finds no name to be "
            + "written under and comes off again.",
            combatRule: ActFourEventRelicRules.BlankCartouche),

        Event(ActFourEventRelicRules.JarId, "Jar of Borrowed Breath",
            "The Four Canopic Jars — Jar of Breath.",
            "The first affliction to leave you completely each fight is breath given back: heal 3, and one "
            + "more card at your next hand.",
            combatRule: ActFourEventRelicRules.JarOfBorrowedBreath),

        Event(ActFourEventRelicRules.WeightId, "Broken Royal Weight",
            "The Chamber of False Measures — Break the Scale.",
            "Every fight opens with 10 Block. The first measure you miss is taken on the weight instead: 10 "
            + "Block at your next hand, and one Burdened.",
            combatRule: ActFourEventRelicRules.BrokenRoyalWeight),
    ];
}
