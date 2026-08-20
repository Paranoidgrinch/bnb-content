using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// Shared vocabulary and builders for the final card pool.
//
// BnB does not use Attack/Skill/Power. Its three primary types are Deed (a one-shot offensive action),
// Working (a one-shot defensive, manipulative or administrative action) and Rite (a persistent combat effect),
// plus Junk for the generated nuisance cards. Form, Argument and Permit are TAGS, not types.
//
// The primary type rides along as a combat tag, because that is what rules read it through: Ratified adds its
// damage only to cards tagged deed, relics count "the first Rite you play each turn", and the enemy passives
// that watch card TYPES (Wrong-Window Scribe, Triplicate Examiner) already read the same tag.
public static class CardAuthoring
{
    public const string DeedTag = "deed";
    public const string WorkingTag = "working";
    public const string RiteTag = "rite";
    public const string JunkTag = "junk";

    // Subtypes. Relics and enemies read these, so they are ordinary tags on top of the primary type.
    public const string FormTag = "form";
    public const string ArgumentTag = "argument";
    public const string PermitTag = "permit";

    // A combat-only generated instance. Nothing may copy, restore or record a card carrying this tag — the
    // anti-loop rule both card pools state.
    public const string TemporaryTag = "temporary";

    // The engine exhausts a played card carrying this tag; "Archive" is the Bureaucrat's own word for doing it
    // deliberately, and carries its own pulse so Rites can tell the two apart (see Keywords/ArchivePulse).
    public const string ExhaustTag = "exhaust";
}
