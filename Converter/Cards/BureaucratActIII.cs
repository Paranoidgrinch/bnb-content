using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// Act III of the Bureaucrat pool: custom, testimony, hospitality, restitution and grievances — effects that
// care about what happened on the turns before this one.
public static class BureaucratActIII
{
    private const int Act = 3;

    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard PriorityDocket = new(
        "priority_docket", "Priority Docket", WorkingTag, 1,
        "Choose another card in your hand and Queue it, paying 1 less Energy (minimum 0).",
        QueueForLess(1),
        Rarity: "common", Act: Act);

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard RestitutionWrit = new(
        "restitution_writ", "Restitution Writ", WorkingTag, 0,
        "Apply Paperwork equal to half the unblocked damage you took during the previous enemy turn, rounded " +
        "down. Maximum 6 Paperwork. Exhaust.",
        Apply(Keywords.Paperwork, AtMost(CombatAmountSpec.Binary("div", StruckLastRound(),
            CombatAmountSpec.FromConst(2)), 6)),
        Rarity: "uncommon", Act: Act, Tags: [ExhaustTag]);

    private static readonly BnbCard HedgeHospitality = new(
        "hedge_hospitality", "Hedge Hospitality", WorkingTag, 1,
        "Gain 7 Block. Until your next turn, the first enemy that deals unblocked damage to you gains 4 " +
        "Paperwork.",
        Seq(Block(7), Apply(BureaucratHistory.HedgeHospitality, 1, You)),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard WitnessKnot = new(
        "witness_knot", "Witness Knot", WorkingTag, 1,
        "Apply 1 Doubt to an enemy. If it attacks before your next turn, apply 2 Paperwork to all other enemies.",
        Seq(Apply(Keywords.Doubt, 1), Apply(BureaucratHistory.WitnessKnot, 1)),
        Rarity: "uncommon", Act: Act, Tags: [ArgumentTag]);

    private static readonly BnbCard GuestbookOath = Rite(
        "guestbook_oath", "Guestbook Oath", BureaucratHistory.GuestbookOath, 1,
        "At the end of your turn, if you have any Block, apply 1 Doubt to every enemy that intends to Attack.");

    // "Choose a card that resolved during your previous turn" is a memory the engine does not keep; the copy
    // is taken from the discard pile, which is where those cards are. See ADAPTATIONS.
    private static readonly BnbCard CustomaryDue = new(
        "customary_due", "Customary Due", WorkingTag, 0,
        "Create a Temporary copy of a card in your discard pile and Queue it. The copy Exhausts after " +
        "resolving. Customary Due Exhausts.",
        CopyAndQueue(),
        Rarity: "uncommon", Act: Act, Tags: [ExhaustTag]);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard HearthCompact = Rite(
        "hearth_compact", "Hearth Compact", BureaucratHistory.HearthCompact, 2,
        "Whenever an enemy with Doubt attacks and deals no unblocked damage, the Doubt stack that would " +
        "normally be consumed is retained.",
        rarity: "rare");

    private static readonly BnbCard DueRecompense = new(
        "due_recompense", "Due Recompense", DeedTag, 2,
        "Deal 14 damage, plus 5 damage for each Doubt on the target. Count at most 6 Doubt. Then remove all " +
        "Doubt from the target.",
        Recompense(14, 5),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard BloodTestimony = new(
        "blood_testimony", "Blood Testimony", DeedTag, 2,
        "Deal 9 damage to ALL enemies. Enemies that attacked during the previous enemy turn take 9 " +
        "additional damage.",
        Testimony(9, 9),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard HedgeCovenant = Rite(
        "hedge_covenant", "Hedge Covenant", BureaucratHistory.HedgeCovenant, 2,
        "Whenever Doubt reduces Attack damage, after that Attack has fully resolved, gain Block equal to half " +
        "the prevented damage, rounded up.",
        rarity: "rare");

    private static readonly BnbCard GuestRight = Rite(
        "guest_right", "Guest Right", BureaucratHistory.GuestRight, 2,
        "Once per turn, when an enemy with at least 3 Doubt would deal unblocked damage, remove 3 Doubt and " +
        "reduce that remaining damage to 0.",
        rarity: "rare");

    private static readonly BnbCard GrievanceLedger = new(
        "grievance_ledger", "Grievance Ledger", DeedTag, 2,
        "Deal 10 damage, plus 6 damage for each time this enemy has attacked during this combat. Count at " +
        "most 4 attacks.",
        Grievance(10, 6),
        Rarity: "rare", Act: Act);

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        PriorityDocket, PriorityDocket.Upgraded(
            "Choose another card in your hand and Queue it, paying 2 less Energy (minimum 0).",
            QueueForLess(2)),

        RestitutionWrit, RestitutionWrit.Upgraded(
            "Apply Paperwork equal to half the unblocked damage you took during the previous enemy turn, " +
            "rounded down. Maximum 9 Paperwork. Exhaust.",
            Apply(Keywords.Paperwork, AtMost(CombatAmountSpec.Binary("div", StruckLastRound(),
                CombatAmountSpec.FromConst(2)), 9))),

        HedgeHospitality, HedgeHospitality.Upgraded(
            "Gain 9 Block. Until your next turn, the first enemy that deals unblocked damage to you gains 5 " +
            "Paperwork.",
            Seq(Block(9), Apply(BureaucratHistory.HedgeHospitality + "+", 1, You))),

        WitnessKnot, WitnessKnot.Upgraded(
            "Apply 1 Doubt to an enemy. If it attacks before your next turn, apply 3 Paperwork to all other " +
            "enemies.",
            Seq(Apply(Keywords.Doubt, 1), Apply(BureaucratHistory.WitnessKnot + "+", 1))),

        GuestbookOath, GuestbookOath.UpgradedRite(BureaucratHistory.GuestbookOath,
            "At the end of your turn, if you have any Block, apply 1 Doubt to every enemy that intends to " +
            "Attack.", cost: 0),

        CustomaryDue, CustomaryDue.Upgraded(
            "Create a Temporary copy of a card in your discard pile and Queue it; your next card this turn " +
            "costs 1 less. The copy Exhausts after resolving. Customary Due Exhausts.",
            Seq(CopyAndQueue(), Apply(BureaucratRites.CounterWard, 1, You))),

        HearthCompact, HearthCompact.UpgradedRite(BureaucratHistory.HearthCompact,
            "Whenever an enemy with Doubt attacks and deals no unblocked damage, the Doubt stack that would " +
            "normally be consumed is retained.", cost: 1),

        DueRecompense, DueRecompense.Upgraded(
            "Deal 18 damage, plus 5 damage for each Doubt on the target. Count at most 6 Doubt. Then remove " +
            "all Doubt from the target.", Recompense(18, 5)),

        BloodTestimony, BloodTestimony.Upgraded(
            "Deal 12 damage to ALL enemies. Enemies that attacked during the previous enemy turn take 10 " +
            "additional damage.", Testimony(12, 10)),

        HedgeCovenant, HedgeCovenant.UpgradedRite(BureaucratHistory.HedgeCovenant,
            "Whenever Doubt reduces Attack damage, after that Attack has fully resolved, gain Block equal to " +
            "half the prevented damage, rounded up.", cost: 1),

        GuestRight, GuestRight.UpgradedRite(BureaucratHistory.GuestRight,
            "Once per turn, when an enemy with at least 3 Doubt would deal unblocked damage, remove 3 Doubt " +
            "and reduce that remaining damage to 0.", cost: 1),

        GrievanceLedger, GrievanceLedger.Upgraded(
            "Deal 10 damage, plus 8 damage for each time this enemy has attacked during this combat. Count " +
            "at most 4 attacks.", Grievance(10, 8)),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity, Act: Act);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    private static CombatAmountSpec StruckLastRound() =>
        new("counter", SelectorKey: You, CounterId: Keywords.StruckLastRoundCounter.value);

    // "Queue another card from your hand, paying N less Energy." Queueing itself is free (the queueCard node
    // pays nothing), so the card charges the price: what the chosen card costs, less the discount, floored.
    private static readonly CounterId DocketPrice = new("priority_docket_price");

    private static CombatNodeModel QueueForLess(int discount)
    {
        var chosen = new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to Queue");
        return Seq(
            new CombatNodeModel("setCombatantCounter", You,
                CombatAmountSpec.Binary("max",
                    CombatAmountSpec.Binary("sub",
                        new CombatAmountSpec("cardCost", ReadId: Energy.value, ReadCard: chosen),
                        CombatAmountSpec.FromConst(discount)),
                    CombatAmountSpec.FromConst(0)),
                CounterId: DocketPrice.value, Relative: false),
            new CombatNodeModel("loseResource", You,
                new CombatAmountSpec("counter", SelectorKey: You, CounterId: DocketPrice.value), Energy.value),
            new CombatNodeModel("queueCard", You, Card: chosen, HasCardTarget: true, ToSelectorKey: Target));
    }

    // A Temporary copy of something already spent, queued rather than played — the design's "resolved during
    // your previous turn" read as "in your discard pile", which is where such a card is.
    private static CombatNodeModel CopyAndQueue() =>
        Seq(
            new CombatNodeModel("createCardCopy", You, CombatAmountSpec.FromConst(1),
                Card: new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a card to repeat"),
                ToZone: CardZone.Hand),
            new CombatNodeModel("queueCard", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose the copy to Queue"),
                HasCardTarget: true, ToSelectorKey: Target));

    // "Deal N damage, plus M per Doubt (max 6). Then remove all Doubt." Counted before it is cleared.
    private static readonly CounterId DoubtCashed = new("due_recompense_doubt");

    private static CombatNodeModel Recompense(int damage, int per) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", Target, AtMost(Stacks(Keywords.Doubt), 6),
                CounterId: DoubtCashed.value, Relative: false),
            Damage(Plus(CombatAmountSpec.FromConst(damage),
                Times(new CombatAmountSpec("counter", SelectorKey: Target, CounterId: DoubtCashed.value), per))),
            new CombatNodeModel("removeStatus", Target, StatusId: Keywords.Doubt));

    // "Enemies that attacked during the previous enemy turn take N additional damage." Who attacked is
    // remembered on each enemy by the marker every encounter carries (BureaucratHistory.Attacked).
    private static CombatNodeModel Testimony(int damage, int extra) =>
        Seq(
            Damage(damage, AllEnemies),
            new CombatNodeModel("forEachTarget",
                SelectorKey: "enemiesWithStatus", SelectorStatusId: BureaucratHistory.AttackedLastRound,
                Children: [Damage(extra, "iterationTarget")]));

    // "Plus N for each time this enemy has attacked this combat, at most 4." The tally is kept by the same
    // marker; a counter on the enemy, so it is per enemy.
    private static CombatNodeModel Grievance(int damage, int per) =>
        Damage(Plus(CombatAmountSpec.FromConst(damage),
            Times(AtMost(new CombatAmountSpec("counter", SelectorKey: Target,
                CounterId: BureaucratHistory.AttacksCounter.value), 4), per)));
}
