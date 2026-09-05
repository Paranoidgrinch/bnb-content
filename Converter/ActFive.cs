using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V — THE DIVINE LEDGER, so far only as a place that speaks.
//
// The design's one SHARED rule for the whole act (boss master §Act V §4) is a UI rule: every god gets a
// prominent DIVINE RULE AREA in the combat screen, always in the same place, filled completely differently by
// each — the First Tablet, the Eanna Ledger, the Ration Tablet, the Lunar Calendar, the Oaths, the Decrees.
// The player should read it and know at once: this is where the god says what reality currently means.
//
// That area needs somewhere to get its words from, and the engine is the wrong place for them — a rule area is
// presentation, and the engine reads none of it. So it rides on the encounter's PRESENTATION (Extra), which is
// exactly what that dictionary is for: the frontend keys its panel off `divineRuleTitle` + `divineRule`, and an
// encounter without them shows no panel, which is every fight in Acts I–IV.
//
// At V-0 the six gods are still the ported placeholders and their lines say what each god IS. V-1 … V-6 replace
// the fights and rewrite these lines with the live state each area actually shows (the tablet's written rows,
// the ledger's claims, the moon's phase), which is the point at which the area stops being a caption.
public static partial class ActFive
{
    public const int Act = 5;

    // The Extra keys the frontend reads. Written down once here because they are a CONTRACT with the Godot
    // side rather than a name only this file uses.
    public const string RuleTitleKey = "divineRuleTitle";
    public const string RuleTextKey = "divineRule";

    public static IReadOnlyDictionary<string, (string Title, string Rule)> RuleAreas { get; } =
        new Dictionary<string, (string, string)>
        {
            ["act_5_nisaba_keeper_of_the_first_tablet"] = (
                "The First Tablet",
                "Three sentences about your future stand on the tablet, each with the turns left before it "
                + "becomes true. You cannot dispel one — you edit it. A Reed Mark buys one revision, and a "
                + "revised sentence still comes true, in smaller words."),
            ["act_5_inanna_mistress_of_the_eanna_ledger"] = (
                "The Eanna Ledger",
                "A card she claims is stamped PROPERTY OF EANNA. It stays yours, and its first play each turn "
                + "costs 1 Energy less — and every use of it writes 1 Temple Due. At the Procession what is "
                + "unpaid becomes Arrears, and Arrears do not go away."),
            [NansheEncounterId] = (
                "The Ration Tablet",
                "Three days to a Distribution, and all three portions are on the table before the first one "
                + "begins. Your natural Energy and your natural draw are your share — anything you take "
                + "beyond it comes out of a later day, and no day but the last falls below 1 and 1."),
            ["act_5_nanna_sin_moon_seal_of_ur"] = (
                "The Lunar Calendar",
                "The moon is a schedule. Whatever it is due on the day it arrives, it takes on that day and no "
                + "other."),
            ["act_5_utu_witness_of_every_oath"] = (
                "Oaths and Witness",
                "Utu witnesses every oath, including the ones you did not know you were swearing. Nothing done "
                + "here goes unrecorded."),
            ["act_5_enlil_voice_of_the_unalterable_decree"] = (
                "Decrees",
                "Enlil says a thing, and it is so. A decree is not an attack — it is the state the world is in "
                + "from now on."),
        };

    // The presentation hints one encounter carries, empty for anything that is not a god.
    public static IReadOnlyDictionary<string, string> Extra(string encounterId) =>
        RuleAreas.TryGetValue(encounterId, out var area)
            ? new Dictionary<string, string> { [RuleTitleKey] = area.Title, [RuleTextKey] = area.Rule }
            : new Dictionary<string, string>();

    // Everything the act's gods put into the document. One line per god as they are built.
    public static IReadOnlyList<StatusData> All() =>
        [.. NisabaStatuses(), .. InannaStatuses(), .. NansheStatuses()];

    public static IReadOnlyList<CardData> GivenCards() =>
        [.. NisabaReedCards(), .. InannaLedgerCards(), .. NansheRationCards()];

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        NisabaIntent(enemyId, intentId)
        ?? InannaIntent(enemyId, intentId)
        ?? NansheIntent(enemyId, intentId);
}
