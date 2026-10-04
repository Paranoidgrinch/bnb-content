using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// WHICH INGREDIENT EACH GENERAL CARD IS (hedge_witch_master.md §16.2; plan W6, table E4 approved 2026-10-04).
// Direct damage → Fang · curses, enemy status engines, delayed damage → Hex · Block, Ward Wax, protection → Husk ·
// recovery, cleansing, draw, plain usefulness → Hearth · denial, Censure, strange causality → Fortune.
// Written onto the card (and its upgrade) at assembly; it means something only to the Witch's cauldron.
public static class WitchFamilies
{
    public static readonly IReadOnlyDictionary<string, string> General = Table(
        (WitchActions.Fang,
        [
            "grave_lien", "foreclosure", "forfeit_seal", "dawn_summons", "seizure_writ", "blood_tithe",
            "vital_census", "exemplary_sentence", "black_tribunal", "grand_citation", "crown_repossession",
            "tallow_judgment", "hemal_audit", "last_office",
        ]),
        (WitchActions.Hex,
        [
            "witchmark_citation", "blood_marginalia", "mortgage_sigil", "silent_hearing", "notary_beetle",
            "usurers_moon", "sanguine_errata", "vein_register", "blood_redaction", "standing_citation",
            "debt_ouroboros", "compound_indictment",
        ]),
        (WitchActions.Husk,
        [
            "waxen_surety", "contempt_finding", "tallow_reserve", "sealed_mantle", "wax_reliquary",
            "consecrated_testament", "mortgaged_aegis", "votive_covenant", "candle_cathedral", "wax_indemnity",
        ]),
        (WitchActions.Hearth,
        [
            "borrowed_candle", "false_signature", "proxy_curse", "moonlit_counterfeit", "grand_dispensation",
        ]),
        (WitchActions.Fortune,
        [
            "malediction_review", "sanctioned_charm", "reciprocal_edict", "blacklisted", "countermanded_grace",
            "crossed_sigil", "oath_of_refusal", "sovereign_prohibition", "absolute_interdict",
        ]));

    public static IReadOnlyList<CardData> Mark(IReadOnlyList<CardData> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        return [.. cards.Select(card =>
            General.TryGetValue(card.Id.TrimEnd('+'), out var family) && card.Tags.All(t => t.value != family)
                ? card with { Tags = [.. card.Tags, new TagId(family)] }
                : card)];
    }

    private static Dictionary<string, string> Table(params (string Family, string[] Cards)[] rows) =>
        rows.SelectMany(r => r.Cards.Select(id => (id, r.Family))).ToDictionary(p => p.id, p => p.Family, StringComparer.Ordinal);
}
