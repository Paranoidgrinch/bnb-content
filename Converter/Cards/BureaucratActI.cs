using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// The Bureaucrat's Act-I reward cards, straight off `source-data/design/bureaucrat_final_cards.md`.
//
// Act I is the readable foundation: municipal absurdity, the first Paperwork / Doubt / Junk / Queue / Seal
// seeds, and reliable damage and Block. The numbers here are the design's; only combat simulation and live
// play are allowed to move them.
public static class BureaucratActI
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard WaxingAuthority = new(
        "waxing_authority", "Waxing Authority", DeedTag, 1,
        "Deal 5 damage. Apply 1 Seal.",
        Seq(Damage(5), ApplySeal(1)),
        Rarity: "common");

    private static readonly BnbCard SecureMisfiling = new(
        "secure_misfiling", "Secure Misfiling", WorkingTag, 0,
        "Add 1 Misfiled Paper to your discard pile. Draw 1 card.",
        Seq(AddCard(BureaucratStarter.MisfiledPaper.Id, CardZone.DiscardPile), Draw(1)),
        Rarity: "common", Tags: [FormTag]);

    private static readonly BnbCard CauldronCopy = new(
        "cauldron_copy", "Cauldron Copy", DeedTag, 1,
        "Deal 9 damage. Add 1 Duplicate Copy to your discard pile.",
        Seq(Damage(9), AddCard(BureaucratStarter.DuplicateCopy.Id, CardZone.DiscardPile)),
        Rarity: "common");

    // "If any enemy has Paperwork" reads across the whole enemy side, not just the card's target — the one
    // place a Common looks past what it is aimed at.
    private static readonly BnbCard OccultPrecedent = new(
        "occult_precedent", "Occult Precedent", WorkingTag, 1,
        "Gain 7 Block. If any enemy has Paperwork, gain 2 additional Block.",
        Block(BlockPlusIfAnyEnemyHas(7, Keywords.Paperwork, 2)),
        Rarity: "common", Tags: [ArgumentTag]);

    private static readonly BnbCard FinePrintHex = new(
        "fine_print_hex", "Fine-Print Hex", DeedTag, 1,
        "Deal 7 damage. If the target has Doubt, apply 1 Seal.",
        Seq(Damage(7), If(HasStacks(Keywords.Doubt), ApplySeal(1))),
        Rarity: "common");

    // "If this Ratifies the target, gain 5 Block": the Block is decided BEFORE the Seals are spent, because
    // afterwards there is nothing left to ask — so the Seal goes on, the question is put, and only then does
    // the Ratify conversion run.
    private static readonly BnbCard NotarialPress = new(
        "notarial_press", "Notarial Press", WorkingTag, 1,
        "Apply 2 Seal. If this Ratifies the target, gain 5 Block.",
        SealAndRatifyWithBonus(2, Block(5)),
        Rarity: "common", Tags: [FormTag]);

    private static readonly BnbCard InkblotVerdict = new(
        "inkblot_verdict", "Inkblot Verdict", DeedTag, 1,
        "Deal 8 damage. If the target has Paperwork, deal 2 additional damage.",
        Seq(Damage(8), If(HasStacks(Keywords.Paperwork), Damage(2))),
        Rarity: "common");

    private static readonly BnbCard Deskward = new(
        "deskward", "Deskward", WorkingTag, 1,
        "Gain 8 Block. Add 1 Red Tape to your discard pile.",
        Seq(Block(8), AddCard(BureaucratStarter.RedTape.Id, CardZone.DiscardPile)),
        Rarity: "common");

    private static readonly BnbCard SealOfConcern = new(
        "seal_of_concern", "Seal of Concern", WorkingTag, 1,
        "Apply 1 Seal and 1 Doubt.",
        Seq(ApplySeal(1), Apply(Keywords.Doubt, 1)),
        Rarity: "common", Tags: [FormTag]);

    private static readonly BnbCard PettyObjection = new(
        "petty_objection", "Petty Objection", WorkingTag, 1,
        "Gain 5 Block. Apply 1 Doubt.",
        Seq(Block(5), Apply(Keywords.Doubt, 1)),
        Rarity: "common", Tags: [ArgumentTag]);

    private static readonly BnbCard CursedAddendum = new(
        "cursed_addendum", "Cursed Addendum", DeedTag, 1,
        "Deal 6 damage. Apply 2 Paperwork.",
        Seq(Damage(6), Apply(Keywords.Paperwork, 2)),
        Rarity: "common");

    // The two Queue Commons: played now, felt next turn. Protective Adjournment pays far more Block than a
    // 1-Energy Working has any right to, and Deferred Hex far more damage, because both make the player spend
    // a turn without them.
    private static readonly BnbCard ProtectiveAdjournment = new(
        "protective_adjournment", "Protective Adjournment", WorkingTag, 1,
        "Queue: Gain 11 Block.",
        Block(11),
        Rarity: "common", Queued: true);

    private static readonly BnbCard DeferredHex = new(
        "deferred_hex", "Deferred Hex", DeedTag, 1,
        "Queue: Deal 13 damage.",
        Damage(13),
        Rarity: "common", Queued: true);

    // "Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional Block."
    private static readonly BnbCard CertifiedKindling = new(
        "certified_kindling", "Certified Kindling", WorkingTag, 1,
        "Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional Block.",
        Seq(Block(4), ArchiveJunkFirst()),
        Rarity: "common");

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        WaxingAuthority, WaxingAuthority.Upgraded("Deal 7 damage. Apply 1 Seal.",
            Seq(Damage(7), ApplySeal(1))),

        SecureMisfiling, SecureMisfiling.Upgraded(
            "Add 1 Misfiled Paper to your discard pile. Draw 2 cards.",
            Seq(AddCard(BureaucratStarter.MisfiledPaper.Id, CardZone.DiscardPile), Draw(2))),

        CauldronCopy, CauldronCopy.Upgraded("Deal 12 damage. Add 1 Duplicate Copy to your discard pile.",
            Seq(Damage(12), AddCard(BureaucratStarter.DuplicateCopy.Id, CardZone.DiscardPile))),

        OccultPrecedent, OccultPrecedent.Upgraded(
            "Gain 9 Block. If any enemy has Paperwork, gain 2 additional Block.",
            Block(BlockPlusIfAnyEnemyHas(9, Keywords.Paperwork, 2))),

        FinePrintHex, FinePrintHex.Upgraded("Deal 9 damage. If the target has Doubt, apply 1 Seal.",
            Seq(Damage(9), If(HasStacks(Keywords.Doubt), ApplySeal(1)))),

        NotarialPress, NotarialPress.Upgraded("Apply 2 Seal. If this Ratifies the target, gain 7 Block.",
            SealAndRatifyWithBonus(2, Block(7))),

        InkblotVerdict, InkblotVerdict.Upgraded(
            "Deal 10 damage. If the target has Paperwork, deal 2 additional damage.",
            Seq(Damage(10), If(HasStacks(Keywords.Paperwork), Damage(2)))),

        Deskward, Deskward.Upgraded("Gain 11 Block. Add 1 Red Tape to your discard pile.",
            Seq(Block(11), AddCard(BureaucratStarter.RedTape.Id, CardZone.DiscardPile))),

        SealOfConcern, SealOfConcern.Upgraded("Apply 2 Seal and 1 Doubt.",
            Seq(ApplySeal(2), Apply(Keywords.Doubt, 1))),

        PettyObjection, PettyObjection.Upgraded("Gain 7 Block. Apply 1 Doubt.",
            Seq(Block(7), Apply(Keywords.Doubt, 1))),

        CursedAddendum, CursedAddendum.Upgraded("Deal 8 damage. Apply 2 Paperwork.",
            Seq(Damage(8), Apply(Keywords.Paperwork, 2))),

        ProtectiveAdjournment, ProtectiveAdjournment.Upgraded("Queue: Gain 14 Block.", Block(14)),

        DeferredHex, DeferredHex.Upgraded("Queue: Deal 16 damage.", Damage(16)),

        CertifiedKindling, CertifiedKindling.Upgraded(
            "Archive a card from your hand. Gain 6 Block. If it was Junk, gain 4 additional Block.",
            Seq(Block(6), ArchiveJunkFirst())),
    ];

    // ── shapes shared by several cards ────────────────────────────────────────────────────────────────────

    // "Gain N Block. If any enemy has <status>, gain M more." Asked as an AMOUNT rather than a branch: the
    // curated condition shape names a selector but cannot parameterise it with a status, and counting the
    // enemies that carry it — clamped to one — is exactly "if any".
    private static CombatAmountSpec BlockPlusIfAnyEnemyHas(int baseBlock, string status, int bonus) =>
        CombatAmountSpec.Binary("add",
            CombatAmountSpec.FromConst(baseBlock),
            CombatAmountSpec.Binary("mul",
                Once(new CombatAmountSpec("countTargets",
                    ReadSelector: new CombatSelectorSpec("enemiesWithStatus", status))),
                CombatAmountSpec.FromConst(bonus)));

    // Apply Seal, and if the target crosses the Ratify threshold because of it, pay the bonus — asked BEFORE
    // the Seals are spent, since afterwards the evidence is gone.
    private static CombatNodeModel SealAndRatifyWithBonus(int stacks, CombatNodeModel bonus) =>
        Seq(
            Apply(Keywords.Seal, stacks),
            If(HasStacks(Keywords.Seal, RatifyThreshold), bonus),
            ConvertSeals());

    // "Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional Block."
    //
    // A card program cannot look at the card a player is ABOUT to choose, so the Junk case is settled before
    // the choice: while the hand holds Junk, the card takes the first of it and pays the bonus; with no Junk
    // in hand the player picks freely. Whether the taking happened is remembered in a COUNTER rather than a
    // marker status — a marker would have to be removed again, and a status losing stacks is something the
    // general pool's Blood Ink reacts to. The deviation (Junk is taken rather than offered) is recorded in
    // ADAPTATIONS; it is the choice a player after the bonus would make anyway.
    private static readonly CounterId TookJunk = new("certified_kindling_took_junk");

    private static CombatNodeModel ArchiveJunkFirst() =>
        Seq(
            SetCounter(TookJunk, 0),
            CombatNodeModel.ForEachCard(You, CardZone.Hand,
                Seq(
                    Archive(new CombatCardSpec("iterated")),
                    Block(4),
                    SetCounter(TookJunk, 1)),
                tag: JunkTag, takeFirst: 1),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.Equal, Right: 0, Id: TookJunk.value),
                ArchiveChosen()));

    private static CombatNodeModel SetCounter(CounterId counter, int value) =>
        new("setCombatantCounter", You, CombatAmountSpec.FromConst(value),
            CounterId: counter.value, Relative: false);
}
