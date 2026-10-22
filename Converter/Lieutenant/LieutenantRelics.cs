using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Lieutenant;

// The Lieutenant's relics (lieutenant_master.md §4): three Common, five Uncommon and four Rare in her own pool, six
// from the shop — the others' shape. Offered to her alone (Eligibility.Lieutenant and her roster's Exclusive). Old
// municipal watch equipment, not heroic prizes. Numbers are the canon's BALANCE DRAFT.
public static class LieutenantRelics
{
    // Commission's reserve between fights (§2.2: the one exception to Authority ending with the fight).
    public static readonly RunCounterId CommissionReserve = new("lt_commission_reserve");
    public const int CommissionKeeps = 2;
    public const int BrandyCap = 6;

    public static IReadOnlyList<BnbRelic> All() =>
    [
        // ── Common ────────────────────────────────────────────────────────────────────────────────────────
        Normal("lt_relic_tin_fife", "Tin Fife", Rarity.Common,
            "The first time each turn you issue an Order, draw 1 card, then put a card from your hand on the bottom of your draw pile.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.TinFife)),
        Normal("lt_relic_chipped_buckler", "Chipped Buckler", Rarity.Common,
            "The first time each turn a Deed of yours hits a Challenged enemy, gain 4 Block.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.ChippedBuckler)),
        Normal("lt_relic_split_chinstrap", "Split Chinstrap", Rarity.Common,
            "Once each enemy phase, when an enemy hit spends your Open Guard and takes none of your HP, gain 1 Authority at the start of your next turn.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.SplitChinstrap)),

        // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────
        Normal("lt_relic_sword_knot", "Sword Knot", Rarity.Uncommon,
            "The first time each turn you spend Strength on a card, gain 1 Authority.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.SwordKnot)),
        Normal("lt_relic_warrant_cord", "Warrant Cord", Rarity.Uncommon,
            "Once each turn, when your Challenge ends because its target fell before you kept it, gain 1 Authority.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.WarrantCord)),
        Normal("lt_relic_parade_spur", "Parade Spur", Rarity.Uncommon,
            "At the end of your turn, if you have Open Guard, you may gain 1 more. If you do and an enemy attacks you before your next turn, gain 1 Authority when it comes.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.ParadeSpur)),
        Normal("lt_relic_pay_chest", "Pay Chest", Rarity.Uncommon,
            "At the end of your turn, if you have 3 or more Authority, gain 5 Block.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.PayChest)),
        Normal("lt_relic_dented_gorget", "Dented Gorget", Rarity.Uncommon,
            "Once each enemy phase, when an enemy hit spends your Open Guard, your first Deed next turn costs 1 less.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.DentedGorget)),

        // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────
        Normal("lt_relic_torn_standard", "Torn Standard", Rarity.Rare,
            "Once each turn, when an Order fails at its deadline and you have no Challenge out, Challenge the weakest enemy: hit it with a Deed by the end of your next turn (1 Authority).",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.TornStandard)),
        Normal("lt_relic_full_tang", "Full Tang", Rarity.Rare,
            "The first time each turn you spend Strength on a card, deal 6 damage to the weakest enemy for each Strength spent (at most 3).",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.FullTang)),
        Normal("lt_relic_scarlet_epaulette", "Scarlet Epaulette", Rarity.Rare,
            "The first time each turn you gain Open Guard, your next Deed this turn strikes its target once more for half of its blow.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.ScarletEpaulette)),
        Normal("lt_relic_commission", "Commission", Rarity.Rare,
            $"Begin each combat with 1 Authority. After a victory, keep up to {CommissionKeeps} Authority you have left for the start of the next combat.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.Commission),
            runPrograms:
            [
                Openings.EveryCombat(new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(1),
                    StatusId: LieutenantKeywords.Authority)),
                .. Enumerable.Range(1, CommissionKeeps).Select(kept => RunPrograms.When<NodeEnteredRunEvent>(
                    RunExpr.And(new EventBoolValueExpression(RunEventFields.NodeIsCombat),
                        RunExpr.Equal(RunExpr.Counter(CommissionReserve), RunExpr.Const(kept))),
                    Openings.NextCombat(new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(kept),
                        StatusId: LieutenantKeywords.Authority)))),
                // Overwritten, never added: the reserve is what the LAST victory left (§4.3).
                RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
                    RunEffectTemplates.ChangeCounter(CommissionReserve, RunExpr.Subtract(
                        RunExpr.Min(RunEventValues.CombatCounter(LieutenantRules.AuthorityLeft), RunExpr.Const(CommissionKeeps)),
                        RunExpr.Counter(CommissionReserve)))),
            ]),

        // ── Shop ──────────────────────────────────────────────────────────────────────────────────────────
        Shop("lt_relic_lead_knuckles", "Lead Knuckles",
            "Once each turn, when you play a Deed with Open Guard, you may Exhaust another card from your hand to deal 8 more damage to its target.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.LeadKnuckles)),
        Shop("lt_relic_iron_shackle", "Iron Shackle",
            "The first time each turn you issue a Challenge, you may gain 1 Open Guard to apply 2 Doubt to its target.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.IronShackle)),
        Shop("lt_relic_brandy_flask", "Brandy Flask",
            $"After a victorious combat, heal half the HP Open Guard cost you in it (at most {BrandyCap}).",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.BrandyFlask),
            runPrograms:
            [
                RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
                    RunEffectTemplates.Heal(RunExpr.Min(RunExpr.Const(BrandyCap),
                        RunExpr.Divide(RunEventValues.CombatCounter(LieutenantKeywords.GuardExtraTotal), RunExpr.Const(2))))),
            ]),
        Shop("lt_relic_badge_chain", "Badge Chain",
            "Once each turn, when you keep a Challenge, you may turn 1 of the Authority it gave you into 1 Strength.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.BadgeChain)),
        Shop("lt_relic_worn_epaulette", "Worn Epaulette",
            "The first time each turn you keep an Order, your next Working costs 1 less, this turn or the next.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.WornEpaulette)),
        Shop("lt_relic_spur_buckle", "Spur Buckle",
            "Once each turn, after a card spends your Strength, you may spend 1 more Strength to gain 1 Energy.",
            eligibility: Eligibility.Lieutenant, combatRule: Rule(LieutenantRules.SpurBuckle)),
    ];

    private static StatusData Rule(string id) => LieutenantRules.All().Single(s => s.Id == id);
}
