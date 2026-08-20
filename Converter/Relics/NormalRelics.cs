using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The 50 Normal relics — the global Common/Uncommon/Rare pool, from
// `source-data/design/BnB_Final_Relics_Master_PostAudit.md` §3. 18 Common, 18 Uncommon, 14 Rare; 38 General
// and 12 Bureaucrat-specific. These are what Treasure, standard relic rewards and a shop's normal relic
// slots draw from — never the Shop, Event or Boss pools.
//
// A relic that changes a FIGHT does it through a hidden status handed over when the fight opens
// (RelicAuthoring.Rule); everything between fights is a run program.
public static class NormalRelics
{
    public static IReadOnlyList<BnbRelic> All() =>
    [
        // ── Common ────────────────────────────────────────────────────────────────────────────────────────
        Normal("levy_stamp", "Levy Stamp", Rarity.Common,
            "When picked up, gain 30 Gold. After every victorious combat, gain 4 additional Gold.",
            pickup: [Gold(30)],
            runPrograms: [AfterEveryVictory(Gold(4))]),

        // "Retain until the start of your next turn" for a card that arrived outside the draw step. Retention
        // is a property of a card DEFINITION, not of one copy, so this keeps the whole hand instead for one
        // turn — see ADAPTATIONS.
        Normal("brass_bookmark", "Brass Bookmark", Rarity.Common,
            "The first card that enters your hand outside the normal draw each turn is kept until your next turn.",
            combatRule: RelicRules.BrassBookmark),

        Normal("conservators_thread", "Conservator's Thread", Rarity.Common,
            "The first time each turn a card leaves your hand without being played, gain 4 Block.",
            combatRule: RelicRules.ConservatorsThread),

        Normal("sun_warmed_waystone", "Sun-Warmed Waystone", Rarity.Common,
            "If you end your turn with at least 1 unspent Energy, gain 5 Block.",
            combatRule: RelicRules.SunWarmedWaystone),

        Normal("five_notch_bead", "Five-Notch Bead", Rarity.Common,
            "Every fifth card played during combat deals 6 damage to the living enemy with the lowest HP.",
            combatRule: RelicRules.FiveNotchBead),

        Normal("formkeepers_signet", "Formkeeper's Signet", Rarity.Common,
            "The first time each turn you play a Form, gain 2 Block and apply 1 additional Paperwork to its target.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.FormkeepersSignet),

        Normal("moss_salve", "Moss Salve", Rarity.Common,
            "After winning a combat in which you lost HP, heal up to 2 HP.",
            runPrograms: [AfterEveryVictory(Heal(2))]),

        Normal("lead_counterweight", "Lead Counterweight", Rarity.Common,
            "The first time each turn you play a card with base cost 2 or more, gain 4 Block.",
            combatRule: RelicRules.LeadCounterweight),

        Normal("hollow_wax_bead", "Hollow Wax Bead", Rarity.Common,
            "Every third 0-cost card played during combat draws 1 card, at most once per turn.",
            combatRule: RelicRules.HollowWaxBead),

        Normal("binders_awl", "Binder's Awl", Rarity.Common,
            "The first time your draw pile is shuffled each combat, gain 1 Energy and draw 1 card.",
            combatRule: RelicRules.BindersAwl),

        Normal("carved_bone_buckle", "Carved Bone Buckle", Rarity.Common,
            "When picked up, gain 4 Max HP and heal 4 HP.",
            pickup: [MaxHealth(4), Heal(4)]),

        Normal("petitioners_token", "Petitioner's Token", Rarity.Common,
            "The first time each combat a Queued card resolves, gain 1 Energy and draw 1 card.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.PetitionersToken),

        Normal("iron_prayer_bead", "Iron Prayer Bead", Rarity.Common,
            "The first Deed you play each turn against an enemy that intends to Attack deals 4 additional damage.",
            combatRule: RelicRules.IronPrayerBead),

        Normal("black_salt_charm", "Black Salt Charm", Rarity.Common,
            "At combat start gain 4 Block.",
            combatRule: RelicRules.BlackSaltCharm),

        Normal("tarnished_bell", "Tarnished Bell", Rarity.Common,
            "The first time each turn you apply a negative status to an enemy, deal 4 damage to it.",
            combatRule: RelicRules.TarnishedBell),

        Normal("grave_coin", "Grave Coin", Rarity.Common,
            "Whenever an enemy dies while affected by a negative status, gain 4 Gold.",
            runPrograms: [AfterEveryVictory(Gold(4))]),

        Normal("bruise_cup", "Bruise Cup", Rarity.Common,
            "The first time each turn an enemy causes you to lose HP, gain 4 Block.",
            combatRule: RelicRules.BruiseCup),

        Normal("votive_candle", "Votive Candle", Rarity.Common,
            "The first Rite you play each combat costs 1 less Energy and grants 3 Block when played.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.VotiveCandle),

        // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────
        Normal("rootbound_walking_staff", "Rootbound Walking Staff", Rarity.Uncommon,
            "At the start of your next combat after a non-combat node, gain 1 Energy and 6 Block.",
            combatRule: RelicRules.RootboundStaff),

        Normal("counterfeit_toll_writ", "Counterfeit Toll Writ", Rarity.Uncommon,
            "When picked up, gain 30 Gold. Every shop purchase refunds 10 Gold.",
            pickup: [Gold(30)],
            runPrograms: [OnPurchase(Gold(10))]),

        Normal("emergency_inkwell", "Emergency Inkwell", Rarity.Uncommon,
            "Once per combat, after playing a card, if you have no Energy left, gain 1 Energy.",
            combatRule: RelicRules.EmergencyInkwell),

        Normal("ashen_wax_knife", "Ashen Wax Knife", Rarity.Uncommon,
            "The first time each turn you Exhaust a card, draw 1 card.",
            combatRule: RelicRules.AshenWaxKnife),

        Normal("quiet_readers_cord", "Quiet Reader's Cord", Rarity.Uncommon,
            "If you end your turn having played 2 or fewer cards, draw 1 additional card next turn.",
            combatRule: RelicRules.QuietReadersCord),

        Normal("archive_key", "Archive Key", Rarity.Uncommon,
            "The first time each turn you Archive a Junk card, gain 5 Block and draw 1 card.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.ArchiveKey),

        Normal("redaction_knife", "Redaction Knife", Rarity.Uncommon,
            "Once per turn, after your normal draw, discard 1 card and draw 1 card.",
            combatRule: RelicRules.RedactionKnife),

        Normal("alms_basin", "Alms Basin", Rarity.Uncommon,
            "Heal 8 HP the first time you make a purchase in each shop.",
            runPrograms: [OnPurchase(Heal(8))]),

        Normal("index_bone", "Index Bone", Rarity.Uncommon,
            "At the start of your turn, draw 1 additional card.",
            combatRule: RelicRules.IndexBone),

        // "Whenever you leave a card reward without taking a card, gain 10 Gold and heal 1 HP." Skipping a
        // reward is not something a run program can hear, so it pays after every victory instead — see
        // ADAPTATIONS.
        Normal("refusal_rosary", "Refusal Rosary", Rarity.Uncommon,
            "After every victorious combat, gain 10 Gold and heal 1 HP.",
            runPrograms: [AfterEveryVictory(Gold(10), Heal(1))]),

        Normal("archive_censer", "Archive Censer", Rarity.Uncommon,
            "The first time each turn you Archive a card, gain 1 Energy.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.ArchiveCenser),

        Normal("seal_makers_die", "Seal-Maker's Die", Rarity.Uncommon,
            "The first time each turn you Ratify, draw 1 card and gain 5 Block.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.SealMakersDie),

        Normal("blood_price_token", "Blood-Price Token", Rarity.Uncommon,
            "At the start of your turn, lose 3 HP; your next card that turn costs 1 less Energy.",
            combatRule: RelicRules.BloodPriceToken),

        Normal("blackthorn_brooch", "Blackthorn Brooch", Rarity.Uncommon,
            "The first time each turn a single card grants at least 10 Block, deal 6 damage to all enemies.",
            combatRule: RelicRules.BlackthornBrooch),

        Normal("executioners_measure", "Executioner's Measure", Rarity.Uncommon,
            "After every victorious combat, gain 15 Gold.",
            runPrograms: [AfterEveryVictory(Gold(15))]),

        Normal("sootglass_lens", "Sootglass Lens", Rarity.Uncommon,
            "The first time each turn you apply a negative status to an enemy that already had one, draw 1 card.",
            combatRule: RelicRules.SootglassLens),

        Normal("rubric_tablet", "Rubric Tablet", Rarity.Uncommon,
            "The first time each turn you play a Rite, your next card that turn costs 1 less Energy.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.RubricTablet),

        Normal("refuse_docket", "Refuse Docket", Rarity.Uncommon,
            "The first time each turn a Junk card enters your hand, it is Archived and an enemy gains 1 Seal.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.RefuseDocket),

        // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────
        Normal("index_volvelle", "Index Volvelle", Rarity.Rare,
            "At combat start, one card in your hand costs 1 less Energy the first time you play it.",
            combatRule: RelicRules.IndexVolvelle),

        Normal("withheld_hourglass", "Withheld Hourglass", Rarity.Rare,
            "At the start of your turn, one card in your hand costs 0 Energy the first time you play it.",
            combatRule: RelicRules.WithheldHourglass),

        Normal("road_claim_token", "Road-Claim Token", Rarity.Rare,
            "When picked up, upgrade 1 card. After every victorious combat, heal 5 HP.",
            pickup: [new UpgradeCardsRunEffect(
                RunSelectors.DeckCards.Upgradable().ChooseByPlayer(1, "upgrade a card"))],
            runPrograms: [AfterEveryVictory(Heal(5))]),

        Normal("concordance_medallion", "Concordance Medallion", Rarity.Rare,
            "The first time each turn you apply Paperwork or Doubt to a single enemy, apply half of it to " +
            "every other enemy, rounded down but never less than 1.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.ConcordanceMedallion),

        Normal("chancery_ribbon", "Chancery Ribbon", Rarity.Rare,
            "The first Form you play each turn costs 1 less Energy. Paperwork and Doubt it applies are " +
            "increased by 1.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.ChanceryRibbon),

        Normal("iron_astrolabe", "Iron Astrolabe", Rarity.Rare,
            "The first time each turn you draw cards, gain 1 Energy.",
            combatRule: RelicRules.IronAstrolabe),

        Normal("twin_ember_brazier", "Twin-Ember Brazier", Rarity.Rare,
            "Whenever you Rest at a Campfire, upgrade 1 random unupgraded card. Whenever you Smith, heal 7 HP.",
            runPrograms: [RelicRules.TwinEmberBrazier]),

        Normal("gilded_tithe_chain", "Gilded Tithe Chain", Rarity.Rare,
            "When picked up, gain 4 Max HP and heal 4. Every shop purchase grants 2 Max HP.",
            pickup: [MaxHealth(4), Heal(4)],
            runPrograms: [OnPurchase(MaxHealth(2))]),

        Normal("rebinding_spindle", "Rebinding Spindle", Rarity.Rare,
            "At the start of your turn, two cards in your hand cost 1 less Energy the first time they are played.",
            combatRule: RelicRules.RebindingSpindle),

        Normal("deferred_signet", "Deferred Signet", Rarity.Rare,
            "The first card you Queue each turn applies 1 Seal to its target when it resolves.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.DeferredSignet),

        Normal("blood_stamped_bond", "Blood-Stamped Bond", Rarity.Rare,
            "At combat start, lose 6 HP and gain 1 Energy and 1 additional card on turn 1.",
            combatRule: RelicRules.BloodStampedBond),

        Normal("thorn_crowned_reliquary", "Thorn-Crowned Reliquary", Rarity.Rare,
            "Whenever you gain Block, deal damage equal to a quarter of it to the enemy with the most HP, " +
            "at most 10 per gain.",
            combatRule: RelicRules.ThornCrownedReliquary),

        Normal("blank_folio", "Blank Folio", Rarity.Rare,
            "When picked up, remove 1 card from your deck.",
            pickup: [new RemoveCardsRunEffect(
                RunSelectors.DeckCards.ChooseByPlayer(1, "remove a card from your deck"))]),

        Normal("chancery_scale", "Chancery Scale", Rarity.Rare,
            "The first time each turn you apply Paperwork to an enemy already 5 deep, gain 1 Energy and draw 1 card.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: RelicRules.ChanceryScale),
    ];
}
