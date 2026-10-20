using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Apprentice;

// The Apprentice's relics (master §10): two Common, two Uncommon and two Rare in his own pool, three from the shop.
// Offered to him alone (Eligibility.Apprentice, and his roster's Exclusive). Each puts a mark on him at the start of
// every fight (its rule status); what the mark does is the wand's own machinery (WandRules.Hooks, ApprenticeRules).
// Numbers are the master's BALANCE DRAFT.
public static class ApprenticeRelics
{
    public static IReadOnlyList<BnbRelic> All() =>
    [
        // ── Common ────────────────────────────────────────────────────────────────────────────────────────
        Normal("ar_sooty_ferrule", "Sooty Ferrule", Rarity.Common,
            "Once per turn, after a cast in which a direct wand hit spent Singe, gain 1 Focus.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.SootyFerrule)),
        Normal("ar_knotted_wristcord", "Knotted Wristcord", Rarity.Common,
            "After your first cast each turn that ran 1 or 2 wand cards, you may take one of them back into your hand for free.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.KnottedWristcord)),

        // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────
        Normal("ar_blue_inkstone", "Blue Inkstone", Rarity.Uncommon,
            "Once per turn, when Flow would return Focus after a cast, up to 2 of it may lower Exhaustion by 1 each instead.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.BlueInkstone)),
        Normal("ar_bent_nib", "Bent Nib", Rarity.Uncommon,
            "Once per turn, right after you inscribe a card, it may trade places with a neighbouring wand card for free — " +
            "with any wand card, if a Gust resolved this turn.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.BentNib)),

        // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────
        Normal("ar_brass_dividers", "Brass Dividers", Rarity.Rare,
            "Once per turn, you may cast any run of neighbouring slots instead of the whole wand. It costs Focus for what it runs.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.BrassDividers)),
        Normal("ar_ash_bookcord", "Ash Bookcord", Rarity.Rare,
            "Once per turn, after a wand card is exhausted by Burn or Burn Out, you may inscribe a card from your hand into " +
            "its slot for free.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.AshBookcord)),

        // ── Shop ──────────────────────────────────────────────────────────────────────────────────────────
        Shop("ar_feather_catcher", "Feather Catcher",
            "Once per turn, one of the cards your Gust would discard may be inscribed into a free slot instead, for free.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.FeatherCatcher)),
        Shop("ar_chalked_thumbstall", "Chalked Thumbstall",
            "The first time each enemy turn Grit softens an attack, your next cast's Exhaustion is 2 lower.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.ChalkedThumbstall)),
        Shop("ar_bone_spacer", "Bone Spacer",
            "Once per turn, the first cast that crosses an empty slot between two cards gives its next Speak 25% more, " +
            "and 1 more Exhaustion.",
            eligibility: Eligibility.Apprentice, combatRule: Rule(ApprenticeRules.BoneSpacer)),
    ];

    private static RogueDeck.Scenario.Authoring.StatusData Rule(string id) =>
        ApprenticeRules.All().Single(s => s.Id == id);
}
