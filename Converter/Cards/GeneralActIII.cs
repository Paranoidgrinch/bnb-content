using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// Act III of the general pool: defensive engines and cash-out cards, meeting substantially more dangerous
// encounters than Act I's readable introductions.
public static class GeneralActIII
{
    private const int Act = 3;

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BloodTithe = new(
        "blood_tithe", "Blood Tithe", DeedTag, 1,
        "Deal 8 damage. If the target has Blood Ink, it loses HP equal to twice its Blood Ink, then loses 1 " +
        "Blood Ink.",
        Tithe(8),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard WaxReliquary = new(
        "wax_reliquary", "Wax Reliquary", WorkingTag, 1,
        "Gain 4 Ward Wax. Until your next turn, Ward Wax cannot suffer its additional decay.",
        Seq(Apply(Keywords.WardWax, 4, You), Apply(GeneralWax.WaxReliquary, 1, You)),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard ConsecratedTestament = Rite(
        "consecrated_testament", "Consecrated Testament", GeneralWax.ConsecratedTestament, 1,
        "The first 3 times each turn an enemy loses HP because of a Status effect, gain 1 Ward Wax.");

    private static readonly BnbCard MortgagedAegis = new(
        "mortgaged_aegis", "Mortgaged Aegis", WorkingTag, 1,
        "Gain 18 Block. At the start of your next turn, gain 8 Lien.",
        Seq(Block(18), Apply(GeneralWax.MortgagedAegis, 1, You)),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard VitalCensus = new(
        "vital_census", "Vital Census", DeedTag, 2,
        "Deal 8 damage to ALL enemies. Every enemy with Blood Ink loses HP equal to its Blood Ink, then loses 1.",
        Census(8),
        Rarity: "uncommon", Act: Act);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard VotiveCovenant = Rite(
        "votive_covenant", "Votive Covenant", GeneralWax.VotiveCovenant, 2,
        "If you take no unblocked Attack damage during an enemy turn, Ward Wax does not decay. If you do, it " +
        "loses 3 stacks instead of 2.",
        rarity: "rare");

    private static readonly BnbCard ExemplarySentence = new(
        "exemplary_sentence", "Exemplary Sentence", DeedTag, 2,
        "Remove up to 5 Citation from an enemy. For each removed, ALL enemies lose 4 HP. Then deal 12 damage " +
        "to it.",
        Sentence(12, 4),
        Rarity: "rare", Act: Act);

    // "Whenever you would take unblocked Attack damage, you may consume up to 4 Ward Wax; reduce that damage
    // by 3 per Wax consumed." A hit already landing cannot be softened, so the Wax buys the damage back after
    // the fact. See ADAPTATIONS.
    private static readonly BnbCard WaxIndemnity = new(
        "wax_indemnity", "Wax Indemnity", WorkingTag, 1,
        "Until your next turn, damage that gets through is answered by your Ward Wax: up to 4 Wax is spent, " +
        "healing 3 HP each.",
        Apply(GeneralWax.WaxIndemnity, 1, You),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard OathOfRefusal = Rite(
        "oath_of_refusal", "Oath of Refusal", GeneralWax.OathOfRefusal, 2,
        "The first 2 times each turn Censure prevents one or more Status stacks, record 1 Refusal. At the " +
        "start of your next turn, draw 1 card per Refusal, maximum 2, and gain 1 Energy. Then clear them.",
        rarity: "rare");

    private static readonly BnbCard DebtOuroboros = Rite(
        "debt_ouroboros", "Debt Ouroboros", GeneralWax.DebtOuroboros, 2,
        "Whenever Lien resolves, apply Lien equal to half the amount consumed, rounded down, maximum 4.",
        rarity: "rare");

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        BloodTithe, BloodTithe.Upgraded(
            "Deal 11 damage. If the target has Blood Ink, it loses HP equal to twice its Blood Ink, then " +
            "loses 1 Blood Ink.", Tithe(11)),

        WaxReliquary, WaxReliquary.Upgraded(
            "Gain 5 Ward Wax. Until your next turn, Ward Wax cannot suffer its additional decay.",
            Seq(Apply(Keywords.WardWax, 5, You), Apply(GeneralWax.WaxReliquary, 1, You))),

        ConsecratedTestament, ConsecratedTestament.UpgradedRite(GeneralWax.ConsecratedTestament,
            "The first 4 times each turn an enemy loses HP because of a Status effect, gain 1 Ward Wax."),

        MortgagedAegis, MortgagedAegis.Upgraded(
            "Gain 22 Block. At the start of your next turn, gain 8 Lien.",
            Seq(Block(22), Apply(GeneralWax.MortgagedAegis, 1, You))),

        VitalCensus, VitalCensus.Upgraded(
            "Deal 11 damage to ALL enemies. Every enemy with Blood Ink loses HP equal to its Blood Ink, then " +
            "loses 1.", Census(11)),

        VotiveCovenant, VotiveCovenant.UpgradedRite(GeneralWax.VotiveCovenant,
            "If you take no unblocked Attack damage during an enemy turn, Ward Wax does not decay. If you " +
            "do, it loses 3 stacks instead of 2.", cost: 1),

        ExemplarySentence, ExemplarySentence.Upgraded(
            "Remove up to 5 Citation from an enemy. For each removed, ALL enemies lose 5 HP. Then deal 15 " +
            "damage to it.", Sentence(15, 5)),

        WaxIndemnity, WaxIndemnity.Upgraded(
            "Until your next turn, damage that gets through is answered by your Ward Wax: up to 4 Wax is " +
            "spent, healing 4 HP each.", Apply(GeneralWax.WaxIndemnity, 1, You)),

        OathOfRefusal, OathOfRefusal.UpgradedRite(GeneralWax.OathOfRefusal,
            "The first 2 times each turn Censure prevents one or more Status stacks, record 1 Refusal. At " +
            "the start of your next turn, draw 1 card per Refusal, maximum 2, and gain 1 Energy.", cost: 1),

        DebtOuroboros, DebtOuroboros.UpgradedRite(GeneralWax.DebtOuroboros,
            "Whenever Lien resolves, apply Lien equal to half the amount consumed, rounded down, maximum 4.",
            cost: 1),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity, Act: Act);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    // Blood Ink's own bite, called in early and doubled.
    private static CombatNodeModel Tithe(int damage) =>
        Seq(
            Damage(damage),
            If(HasStacks(Keywords.BloodInk),
                Seq(
                    new CombatNodeModel("dealDamage", Target, Times(Stacks(Keywords.BloodInk), 2),
                        IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
                    Remove(Keywords.BloodInk, 1))));

    private static CombatNodeModel Census(int damage) =>
        Seq(
            Damage(damage, AllEnemies),
            new CombatNodeModel("forEachTarget",
                SelectorKey: "enemiesWithStatus", SelectorStatusId: Keywords.BloodInk,
                Children:
                [
                    Seq(
                        new CombatNodeModel("dealDamage", "iterationTarget",
                            Stacks(Keywords.BloodInk, "iterationTarget"),
                            IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
                        Remove(Keywords.BloodInk, 1, "iterationTarget")),
                ]));

    // "Remove up to 5 Citation. For each removed, ALL enemies lose N HP. Then deal M damage to it." Counted
    // before it is erased.
    private static readonly CounterId Sentenced = new("exemplary_sentence_count");

    private static CombatNodeModel Sentence(int damage, int perCitation) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, AtMost(Stacks(Keywords.Citation), 5),
                CounterId: Sentenced.value, Relative: false),
            Remove(Keywords.Citation, 5),
            new CombatNodeModel("dealDamage", AllEnemies,
                Times(new CombatAmountSpec("counter", SelectorKey: You, CounterId: Sentenced.value), perCitation),
                IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
            Damage(damage));
}
