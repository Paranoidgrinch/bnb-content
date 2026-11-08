using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// WHICH INGREDIENT EACH GENERAL CARD IS (hedge_witch_master.md §16.2; plan W6, table E4 approved 2026-10-04).
// Direct damage → Fang · curses, enemy status engines, delayed damage → Hex · Block, Ward Wax, protection → Husk ·
// recovery, cleansing, draw, plain usefulness → Hearth · denial, Censure, strange causality → Fortune.
// The general pool rebuilt on 2026-11-08 brings its families from GENERAL_POOL_PLAN.md §4 (last column).
// Written onto the card (and its upgrade) at assembly; it means something only to the Witch's cauldron.
public static class WitchFamilies
{
    public static readonly IReadOnlyDictionary<string, string> General = Table(
        (WitchActions.Fang,
        [
            "ashen_effigy", "beadles_cudgel", "blacklisted", "corpse_candle", "dawn_summons", "hailstones",
            "ill_wind", "midnight_toll", "poppet_pin", "pyre_of_old_records", "salamander_in_the_grate",
            "sifted_ashes", "smouldering_correspondence", "spiteful_draft", "tallow_judgment",
            "third_and_final_writ", "thrown_inkpot", "volley_of_quills", "weight_of_paper",
        ]),
        (WitchActions.Hex,
        [
            "bill_of_attainder", "compounded_grievance", "curdled_milk", "hares_mark", "lingering_malison",
            "named_in_the_roll", "overlooking", "seed_of_misgiving", "spreading_rumour", "whispering_gallery",
        ]),
        (WitchActions.Husk,
        [
            "ashen_receipt", "barred_door", "besom_guard", "bone_meal", "burnt_offering", "cold_hearth",
            "rowan_over_the_door", "salt_on_the_sill", "sealed_mantle", "standing_stones", "tallow_reserve",
            "unburied", "vigil_lamp", "wax_reliquary", "waxen_surety",
        ]),
        (WitchActions.Hearth,
        [
            "ash_remembers", "banked_embers", "borrowed_candle", "coin_under_the_tongue", "crossed_sigil",
            "dog_eared_almanac", "errand_rook", "gathered_gossip", "lamplit_study", "misers_coffer",
            "moonlit_counterfeit", "night_scrivener", "raked_from_the_ashes", "read_by_firelight",
            "sanctioned_charm", "sieve_and_shears", "stolen_hour", "tallow_stub", "wick_burnt_short",
        ]),
        (WitchActions.Fortune,
        [
            "false_signature", "hearth_kindling", "last_ember", "melted_down", "scattered_notes",
            "stay_of_proceedings", "wheat_from_chaff",
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
