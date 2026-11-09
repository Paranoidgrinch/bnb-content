using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 6: flexible damage (GENERAL_POOL_PLAN.md §4, approved 2026-11-08). Area damage, conditional
// damage, finishers and several hits — the last of which Strength and Liable both like.
public static class GeneralDamage
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ThrownInkpot = new(
        "thrown_inkpot", "Thrown Inkpot", DeedTag, 1,
        "Deal 8 damage. Apply 1 Doubt.",
        Seq(Damage(8), Apply(Keywords.Doubt, 1)));

    private static readonly BnbCard PoppetPin = new(
        "poppet_pin", "Poppet Pin", DeedTag, 0,
        "Deal 4 damage. If you have played 2 or more cards this turn, deal 4 more.",
        Pin(4));

    private static readonly BnbCard BeadlesCudgel = new(
        "beadles_cudgel", "Beadle's Cudgel", DeedTag, 1,
        "Deal 9 damage. If the enemy is Liable, draw 1 card.",
        Cudgel(9));

    private static readonly BnbCard Hailstones = new(
        "hailstones", "Hailstones", DeedTag, 1,
        "Deal 4 damage to ALL enemies twice.",
        Repeat(2, DamageAll(4)));

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard VolleyOfQuills = new(
        "volley_of_quills", "Volley of Quills", DeedTag, 1,
        "Deal 3 damage to ALL enemies, once for each card in your hand.",
        Volley(3),
        Rarity: "uncommon");

    // Kept (blacklisted). Content review (2026-11-08): no longer vanilla — a card thrown away buys a heavier blow,
    // and the discard feeds Unread. MTG: Lightning Axe.
    private static readonly BnbCard BluntInstrument = new(
        "blacklisted", "Blunt Instrument", DeedTag, 1,
        "Discard a card. Deal 18 damage.",
        Seq(GeneralDiscard.DiscardChosen(), Damage(18)),
        Rarity: "uncommon", Act: 2);

    // Kept; at most 6 Wax at +5 instead of 8 at +7, and an Uncommon now (2026-11-08).
    private static readonly BnbCard TallowJudgment = new(
        "tallow_judgment", "Tallow Judgment", DeedTag, 2,
        "Spend up to 6 Ward Wax. Deal 10 damage, +5 per Wax.",
        Judgment(6, 10, 5),
        Rarity: "uncommon", Act: 2);

    private static readonly BnbCard MidnightToll = new(
        "midnight_toll", "Midnight Toll", DeedTag, 2,
        "Deal 5 damage for each card you played this turn. Count at most 6.",
        Toll(5),
        Rarity: "uncommon");

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard ThirdAndFinalWrit = new(
        "third_and_final_writ", "Final Writ", DeedTag, 2,
        "Retain. Deal 12 damage. If the enemy has Doubt and Liable, deal it twice.",
        Writ(12),
        Rarity: "rare", Act: 3, RetainInHand: true);

    // Kept; the upgrade is 24 and +15 instead of 27 and +12 (2026-11-08).
    private static readonly BnbCard DawnSummons = new(
        "dawn_summons", "Dawn Summons", DeedTag, 2,
        "Deal 16 damage. +10 if it is your first card this turn.",
        Dawn(16, 10),
        Rarity: "rare", Act: 2);

    public static IReadOnlyList<BnbCard> All() =>
    [
        ThrownInkpot, ThrownInkpot.Upgraded("Deal 12 damage. Apply 1 Doubt.", Seq(Damage(12), Apply(Keywords.Doubt, 1))),
        PoppetPin, PoppetPin.Upgraded(
            "Deal 6 damage. If you have played 2 or more cards this turn, deal 6 more.", Pin(6)),
        BeadlesCudgel, BeadlesCudgel.Upgraded("Deal 13 damage. If the enemy is Liable, draw 1 card.", Cudgel(13)),
        Hailstones, Hailstones.Upgraded("Deal 4 damage to ALL enemies three times.", Repeat(3, DamageAll(4))),

        VolleyOfQuills, VolleyOfQuills.Upgraded(
            "Deal 4 damage to ALL enemies, once for each card in your hand.", Volley(4)),
        BluntInstrument, BluntInstrument.Upgraded("Discard a card. Deal 24 damage.",
            Seq(GeneralDiscard.DiscardChosen(), Damage(24))),
        TallowJudgment, TallowJudgment.Upgraded("Spend up to 6 Ward Wax. Deal 15 damage, +7 per Wax.",
            Judgment(6, 15, 7)),
        MidnightToll, MidnightToll.Upgraded(
            "Deal 7 damage for each card you played this turn. Count at most 6.", Toll(7)),

        ThirdAndFinalWrit, ThirdAndFinalWrit.Upgraded(
            "Retain. Deal 18 damage. If the enemy has Doubt and Liable, deal it twice.", Writ(18)),
        DawnSummons, DawnSummons.Upgraded("Deal 24 damage. +15 if it is your first card this turn.", Dawn(24, 15)),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // Asked before the blow, while the count still excludes the card in flight: "2 or more" means two cards
    // before this one.
    private static CombatNodeModel Pin(int damage) =>
        Seq(SetScratch(PlayedThisTurn), Damage(damage),
            IfScratch(ComparisonOperator.GreaterOrEqual, 2, Damage(damage)));

    private static CombatNodeModel Cudgel(int damage) =>
        Seq(Damage(damage), If(HasStacks(Keywords.Liable), Draw(1)));

    // The Volley is still in the hand while it resolves; it does not count itself. (Replayed from elsewhere — Last
    // Ember — it is not in the hand at all, hence the floor.)
    private static CombatNodeModel Volley(int damage) =>
        new("repeat", Amount: Max(Const(0), Minus(InZone(CardZone.Hand), Const(1))), Children: [DamageAll(damage)]);

    // "For each card you played this turn" — this one included, which the count does not yet hold.
    private static CombatNodeModel Toll(int each) =>
        Damage(Times(AtMost(Plus(PlayedThisTurn, Const(1)), 6), each));

    private static CombatNodeModel Writ(int damage) =>
        Seq(SetScratch(Plus(Once(Stacks(Keywords.Doubt)), Once(Stacks(Keywords.Liable)))),
            Damage(damage),
            IfScratch(ComparisonOperator.GreaterOrEqual, 2, Damage(damage)));

    private static CombatNodeModel Dawn(int damage, int bonus) =>
        Seq(SetScratch(PlayedThisTurn), Damage(damage),
            IfScratch(ComparisonOperator.Equal, 0, Damage(bonus)));

    // "Spend up to N Ward Wax. Deal D, +P per Wax." Counted and taken before the blow.
    private static CounterId WaxSpent => new("tallow_judgment_spent");

    private static CombatNodeModel Judgment(int cap, int damage, int per) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, AtMost(Stacks(Keywords.WardWax, You), cap),
                CounterId: WaxSpent.value, Relative: false),
            new CombatNodeModel("modifyStatusStacks", You,
                CombatAmountSpec.Unary("neg", new CombatAmountSpec("counter", SelectorKey: You, CounterId: WaxSpent.value)),
                StatusId: Keywords.WardWax),
            Damage(Plus(Const(damage),
                Times(new CombatAmountSpec("counter", SelectorKey: You, CounterId: WaxSpent.value), per))));
}
