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

    // Held back until the engine could read a telegraph; now it simply asks.
    private static readonly BnbCard FormOfIllIntent = new(
        "form_of_ill_intent", "Form of Ill Intent", WorkingTag, 1,
        "Apply 3 Paperwork. If the target intends to Attack, also apply 1 Doubt.",
        Seq(Apply(Keywords.Paperwork, 3), If(Intends(IntentKind.Attack), Apply(Keywords.Doubt, 1))),
        Rarity: "common", Tags: [FormTag]);

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

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    // The Rites: playing one puts a status on you, and that status carries the rule (BureaucratRites).
    private static readonly BnbCard BlackLedger = Rite(
        "black_ledger", "Black Ledger", BureaucratRites.BlackLedger, 1,
        "At the start of your turn, if any enemy has at least 8 Paperwork, draw 1 card.");

    private static readonly BnbCard AshRegister = Rite(
        "ash_register", "Ash Register", BureaucratRites.AshRegister, 1,
        "The first time each turn you Archive a card, draw 1 card.");

    private static readonly BnbCard SealDividend = Rite(
        "seal_dividend", "Seal Dividend", BureaucratRites.SealDividend, 1,
        "The first time each turn you Ratify an enemy, draw 1 card.");

    private static readonly BnbCard DubiousAuthority = Rite(
        "dubious_authority", "Dubious Authority", BureaucratRites.DubiousAuthority, 1,
        "Whenever Doubt is consumed after an enemy attacks, apply 2 Paperwork to that enemy.");

    private static readonly BnbCard ClerksFamiliar = Rite(
        "clerks_familiar", "Clerk's Familiar", BureaucratRites.ClerksFamiliar, 1,
        "The first time each turn you create a Junk card, gain 4 Block.");

    private static readonly BnbCard PendingMatters = Rite(
        "pending_matters", "Pending Matters", BureaucratRites.PendingMatters, 1,
        "The first time each turn a Queued card resolves, gain 3 Block.");

    private static readonly BnbCard NightDocket = new(
        "night_docket", "Night Docket", WorkingTag, 0,
        "Resolve your oldest Queued card immediately. Add 1 Red Tape to your discard pile. Exhaust.",
        Seq(ResolveQueued(1), AddJunk(BureaucratStarter.RedTape.Id, CardZone.DiscardPile)),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    private static readonly BnbCard CounterWard = new(
        "counter_ward", "Counter Ward", WorkingTag, 1,
        "Gain 6 Block. Your next card this turn costs 1 less Energy.",
        Seq(Block(6), Apply(BureaucratRites.CounterWard, 1, You)),
        Rarity: "uncommon");

    // "Deal 3 damage 3 times. If the target is Ratified, each hit also applies 1 Paperwork." Asked per hit,
    // because Ratified can end mid-card only if something else removes it — and per-hit is what the text says.
    private static readonly BnbCard ThreefoldInjunction = new(
        "threefold_injunction", "Threefold Injunction", DeedTag, 1,
        "Deal 3 damage 3 times. If the target is Ratified, each hit also applies 1 Paperwork.",
        Repeat(3, Seq(Damage(3), If(HasStacks(Keywords.Ratified), Apply(Keywords.Paperwork, 1)))),
        Rarity: "uncommon");

    private static readonly BnbCard CandleAllowance = new(
        "candle_allowance", "Candle Allowance", WorkingTag, 0,
        "Queue: Gain 1 Energy and draw 1 card. Exhaust.",
        Seq(Energy_(1), Draw(1)),
        Rarity: "uncommon", Tags: [ExhaustTag], Queued: true);

    // "You may Archive a Junk card from your hand; if you do, repeat this attack." Taken automatically when
    // there is Junk to take: Archiving Junk and striking twice is never the worse choice. See ADAPTATIONS.
    private static readonly BnbCard CinderWarrant = new(
        "cinder_warrant", "Cinder Warrant", DeedTag, 1,
        "Deal 7 damage. Archive a Junk card from your hand; if you do, repeat this attack.",
        CinderStrike(7),
        Rarity: "uncommon");

    private static readonly BnbCard PresumptionOfError = new(
        "presumption_of_error", "Presumption of Error", WorkingTag, 1,
        "Apply 1 Doubt. The next time that enemy consumes Doubt by attacking, apply 1 Doubt to it after the " +
        "Attack resolves. Exhaust.",
        Seq(Apply(Keywords.Doubt, 1), Apply(BureaucratRites.PresumptionOfError, 1)),
        Rarity: "uncommon", Tags: [ArgumentTag, ExhaustTag]);

    private static readonly BnbCard TallowBudget = new(
        "tallow_budget", "Tallow Budget", WorkingTag, 0,
        "Gain 1 Energy. Add 1 Red Tape to your hand. Exhaust.",
        Seq(Energy_(1), AddJunk(BureaucratStarter.RedTape.Id, CardZone.Hand)),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    // "If the target does not intend to Attack, apply 2 Seal; otherwise apply 1 Seal." — the telegraph read
    // straight off the enemy, which is what the engine's intent projection is for.
    private static readonly BnbCard ConditionalApproval = new(
        "conditional_approval", "Conditional Approval", DeedTag, 1,
        "Deal 6 damage. If the target does not intend to Attack, apply 2 Seal; otherwise apply 1 Seal.",
        Seq(Damage(6), If(Intends(IntentKind.Attack), ApplySeal(1), ApplySeal(2))),
        Rarity: "uncommon");

    private static readonly BnbCard WastepaperBastion = new(
        "wastepaper_bastion", "Wastepaper Bastion", WorkingTag, 1,
        "Gain 4 Block, plus 2 Block for each Junk card in your hand.",
        Block(Plus(CombatAmountSpec.FromConst(4), Times(CardsTagged(JunkTag), 2))),
        Rarity: "uncommon");

    private static readonly BnbCard FormalDissent = new(
        "formal_dissent", "Formal Dissent", WorkingTag, 0,
        "Remove 1 Doubt from an enemy. Gain 1 Energy. Exhaust.",
        Seq(Remove(Keywords.Doubt, 1), Energy_(1)),
        Rarity: "uncommon", Tags: [ArgumentTag, ExhaustTag]);

    private static readonly BnbCard HexCircular = new(
        "hex_circular", "Hex Circular", DeedTag, 2,
        "Deal 7 damage to ALL enemies. Apply 1 Doubt to ALL enemies.",
        Seq(Damage(7, AllEnemies), Apply(Keywords.Doubt, 1, AllEnemies)),
        Rarity: "uncommon");

    private static readonly BnbCard NotarysTithe = new(
        "notarys_tithe", "Notary's Tithe", WorkingTag, 0,
        "Remove 1 Seal from an enemy. Draw 2 cards. Exhaust.",
        Seq(Remove(Keywords.Seal, 1), Draw(2)),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    private static readonly BnbCard BacklogCharge = new(
        "backlog_charge", "Backlog Charge", DeedTag, 1,
        "Deal 6 damage, plus 3 damage for each card currently in your Queue. Count at most 3 Queued cards.",
        Damage(Plus(CombatAmountSpec.FromConst(6), Times(AtMost(CardsInZone(CardZone.QueuePile), 3), 3))),
        Rarity: "uncommon");

    private static readonly BnbCard ClericalDiscretion = new(
        "clerical_discretion", "Clerical Discretion", WorkingTag, 1,
        "Gain 5 Block. Choose one: apply 1 Doubt; or apply 1 Seal.",
        Seq(Block(5), DoubtOrSeal()),
        Rarity: "uncommon");

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard RedInkDoctrine = Rite(
        "red_ink_doctrine", "Red Ink Doctrine", Keywords.RedInkDoctrine, 2,
        "After an enemy takes HP loss from its Paperwork, if it survives, apply 2 Paperwork to it.",
        rarity: "rare");

    private static readonly BnbCard LicensedDisposal = Rite(
        "licensed_disposal", "Licensed Disposal", BureaucratRites.LicensedDisposal, 2,
        "The first Junk card you draw each turn is automatically Archived; then draw 1 card.",
        rarity: "rare");

    private static readonly BnbCard Continuance = Rite(
        "continuance", "Continuance", BureaucratRites.Continuance, 2,
        "At the end of your turn, retain up to 8 Block.",
        rarity: "rare");

    // The allowance is in force from the moment the Rite is played, so the card hands out the first one
    // itself; the Rite renews it at every turn start after that.
    private static readonly BnbCard ViolenceAllowance = new(
        "violence_allowance", "Violence Allowance", RiteTag, 2,
        "The first Deed you play each turn costs 1 less Energy.",
        Seq(InstallRite(BureaucratRites.ViolenceAllowance), Apply(BureaucratRites.AllowanceReady, 1, You)),
        Rarity: "rare");

    // "At the end of your turn, you may Queue one non-Rite card from your hand with base cost 2 or less for
    // 0 Energy." The Rite is the queueing itself, so it is authored as a card the player plays at will
    // rather than a lasting rule — see ADAPTATIONS.
    private static readonly BnbCard SkeletonStaff = new(
        "skeleton_staff", "Skeleton Staff", WorkingTag, 2,
        "Queue a card from your hand for free. Add 1 Red Tape to your discard pile.",
        Seq(
            new CombatNodeModel("queueCard", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to Queue"),
                HasCardTarget: true, ToSelectorKey: Target),
            AddJunk(BureaucratStarter.RedTape.Id, CardZone.DiscardPile)),
        Rarity: "rare");

    private static readonly BnbCard SummaryJudgment = new(
        "summary_judgment", "Summary Judgment", DeedTag, 2,
        "Deal 16 damage. If the target has at least 6 Paperwork, trigger its Paperwork immediately, then " +
        "remove 3 Paperwork.",
        Seq(Damage(16),
            If(HasStacks(Keywords.Paperwork, 6), Seq(TriggerPaperwork(), Remove(Keywords.Paperwork, 3)))),
        Rarity: "rare");

    // "Deal 5 damage 3 times. If the target is Ratified, repeat this attack." Ratified adds its +3 once for
    // the whole card, however many of these hits land — that is what once-per-action means.
    private static readonly BnbCard CandleTribunal = new(
        "candle_tribunal", "Candle Tribunal", DeedTag, 2,
        "Deal 5 damage 3 times. If the target is Ratified, repeat this attack.",
        TribunalStrike(5),
        Rarity: "rare");

    private static readonly BnbCard Rebuttal = new(
        "rebuttal", "Rebuttal", DeedTag, 1,
        "Deal 9 damage. Gain 4 Block per Doubt already on the target, maximum 12 Block. Then apply 1 Doubt.",
        Seq(
            Block(AtMost(Times(Stacks(Keywords.Doubt), 4), 12)),
            Damage(9),
            Apply(Keywords.Doubt, 1)),
        Rarity: "rare", Tags: [ArgumentTag]);

    // "Requires at least 1 Seal" is authored as a condition rather than a play restriction — the engine has
    // no data-authorable requirement — so the card is playable but does nothing without a Seal. See ADAPTATIONS.
    private static readonly BnbCard PrivySeal = new(
        "privy_seal", "Privy Seal", WorkingTag, 1,
        "Requires at least 1 Seal. Remove all Seals from an enemy and Ratify it immediately. Draw 1 card. Exhaust.",
        If(HasStacks(Keywords.Seal),
            Seq(
                new CombatNodeModel("removeStatus", Target, StatusId: Keywords.Seal),
                Apply(Keywords.Ratified, 1),
                Draw(1))),
        Rarity: "rare", Tags: [ExhaustTag]);

    private static readonly BnbCard BlankWarrant = new(
        "blank_warrant", "Blank Warrant", DeedTag, 2,
        "Deal 18 damage. If the target has no Paperwork, Doubt, or Seal, deal 5 additional damage.",
        Seq(Damage(18), If(Unmarked(), Damage(5))),
        Rarity: "rare");

    private static readonly BnbCard StayOfExecution = new(
        "stay_of_execution", "Stay of Execution", WorkingTag, 1,
        "Choose an enemy with Paperwork. Its Paperwork does not trigger at the end of its next turn. " +
        "Gain 2 Block per current Paperwork on that enemy, maximum 20 Block.",
        If(HasStacks(Keywords.Paperwork),
            Seq(
                Apply(Keywords.StayOfExecution, 1),
                Block(AtMost(Times(Stacks(Keywords.Paperwork), 2), 20)))),
        Rarity: "rare", Tags: [FormTag]);

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        WaxingAuthority, WaxingAuthority.Upgraded("Deal 7 damage. Apply 1 Seal.",
            Seq(Damage(7), ApplySeal(1))),

        FormOfIllIntent, FormOfIllIntent.Upgraded(
            "Apply 4 Paperwork. If the target intends to Attack, also apply 1 Doubt.",
            Seq(Apply(Keywords.Paperwork, 4), If(Intends(IntentKind.Attack), Apply(Keywords.Doubt, 1)))),

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

        // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────
        // A Rite's upgrade is a DIFFERENT status ("<id>+"), because the rule itself changes, not a number in
        // the card's own program.
        BlackLedger, BlackLedger.UpgradedRite(BureaucratRites.BlackLedger,
            "At the start of your turn, if any enemy has at least 6 Paperwork, draw 1 card."),
        AshRegister, AshRegister.UpgradedRite(BureaucratRites.AshRegister,
            "The first time each turn you Archive a card, draw 1 card.", cost: 0),
        SealDividend, SealDividend.UpgradedRite(BureaucratRites.SealDividend,
            "The first time each turn you Ratify an enemy, draw 1 card.", cost: 0),
        DubiousAuthority, DubiousAuthority.UpgradedRite(BureaucratRites.DubiousAuthority,
            "Whenever Doubt is consumed after an enemy attacks, apply 3 Paperwork to that enemy."),
        ClerksFamiliar, ClerksFamiliar.UpgradedRite(BureaucratRites.ClerksFamiliar,
            "The first time each turn you create a Junk card, gain 5 Block."),
        PendingMatters, PendingMatters.UpgradedRite(BureaucratRites.PendingMatters,
            "The first time each turn a Queued card resolves, gain 4 Block."),

        NightDocket, NightDocket.Upgraded("Resolve your oldest Queued card immediately. Exhaust.",
            ResolveQueued(1)),

        CounterWard, CounterWard.Upgraded("Gain 8 Block. Your next card this turn costs 1 less Energy.",
            Seq(Block(8), Apply(BureaucratRites.CounterWard, 1, You))),

        ThreefoldInjunction, ThreefoldInjunction.Upgraded("Deal 4 damage 3 times.", Repeat(3, Damage(4))),

        CandleAllowance, CandleAllowance.Upgraded("Queue: Gain 1 Energy and draw 2 cards. Exhaust.",
            Seq(Energy_(1), Draw(2))),

        CinderWarrant, CinderWarrant.Upgraded(
            "Deal 8 damage. Archive a Junk card from your hand; if you do, repeat this attack.",
            CinderStrike(8)),

        PresumptionOfError, PresumptionOfError.Upgraded(
            "Apply 1 Doubt. The next time that enemy consumes Doubt by attacking, apply 1 Doubt to it after " +
            "the Attack resolves. Exhaust.", cost: 0),

        TallowBudget, TallowBudget.Upgraded("Gain 1 Energy. Add 1 Red Tape to your discard pile. Exhaust.",
            Seq(Energy_(1), AddJunk(BureaucratStarter.RedTape.Id, CardZone.DiscardPile))),

        ConditionalApproval, ConditionalApproval.Upgraded(
            "Deal 8 damage. If the target does not intend to Attack, apply 2 Seal; otherwise apply 1 Seal.",
            Seq(Damage(8), If(Intends(IntentKind.Attack), ApplySeal(1), ApplySeal(2)))),

        WastepaperBastion, WastepaperBastion.Upgraded(
            "Gain 5 Block, plus 3 Block for each Junk card in your hand.",
            Block(Plus(CombatAmountSpec.FromConst(5), Times(CardsTagged(JunkTag), 3)))),

        FormalDissent, FormalDissent.Upgraded("Remove 1 Doubt from an enemy. Gain 1 Energy. Draw 1 card. Exhaust.",
            Seq(Remove(Keywords.Doubt, 1), Energy_(1), Draw(1))),

        HexCircular, HexCircular.Upgraded("Deal 9 damage to ALL enemies. Apply 1 Doubt to ALL enemies.",
            Seq(Damage(9, AllEnemies), Apply(Keywords.Doubt, 1, AllEnemies))),

        NotarysTithe, NotarysTithe.Upgraded("Remove 1 Seal from an enemy. Draw 3 cards. Exhaust.",
            Seq(Remove(Keywords.Seal, 1), Draw(3))),

        BacklogCharge, BacklogCharge.Upgraded(
            "Deal 8 damage, plus 3 damage for each card currently in your Queue. Count at most 3 Queued cards.",
            Damage(Plus(CombatAmountSpec.FromConst(8), Times(AtMost(CardsInZone(CardZone.QueuePile), 3), 3)))),

        ClericalDiscretion, ClericalDiscretion.Upgraded(
            "Gain 7 Block. Choose one: apply 1 Doubt; or apply 1 Seal.", Seq(Block(7), DoubtOrSeal())),

        // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────
        RedInkDoctrine, RedInkDoctrine.Upgraded(
            "After an enemy takes HP loss from its Paperwork, if it survives, apply 2 Paperwork to it.",
            cost: 1),
        LicensedDisposal, LicensedDisposal.UpgradedRite(BureaucratRites.LicensedDisposal,
            "The first Junk card you draw each turn is automatically Archived; then draw 1 card.", cost: 1),
        Continuance, Continuance.UpgradedRite(BureaucratRites.Continuance,
            "At the end of your turn, retain up to 12 Block."),
        ViolenceAllowance, ViolenceAllowance.Upgraded(
            "The first Deed you play each turn costs 1 less Energy.",
            Seq(InstallRite(BureaucratRites.ViolenceAllowance + "+"), Apply(BureaucratRites.AllowanceReady, 1, You)),
            cost: 1),

        SkeletonStaff, SkeletonStaff.Upgraded(
            "Queue a card from your hand for free. Add 1 Red Tape to your discard pile."),

        SummaryJudgment, SummaryJudgment.Upgraded(
            "Deal 19 damage. If the target has at least 6 Paperwork, trigger its Paperwork immediately, then " +
            "remove 3 Paperwork.",
            Seq(Damage(19),
                If(HasStacks(Keywords.Paperwork, 6), Seq(TriggerPaperwork(), Remove(Keywords.Paperwork, 3))))),

        CandleTribunal, CandleTribunal.Upgraded(
            "Deal 6 damage 3 times. If the target is Ratified, repeat this attack.", TribunalStrike(6)),

        Rebuttal, Rebuttal.Upgraded(
            "Deal 12 damage. Gain 4 Block per Doubt already on the target, maximum 12 Block. Then apply 1 Doubt.",
            Seq(Block(AtMost(Times(Stacks(Keywords.Doubt), 4), 12)), Damage(12), Apply(Keywords.Doubt, 1))),

        PrivySeal, PrivySeal.Upgraded(
            "Requires at least 1 Seal. Remove all Seals from an enemy and Ratify it immediately. Draw 1 card.",
            tags: []),

        BlankWarrant, BlankWarrant.Upgraded(
            "Deal 22 damage. If the target has no Paperwork, Doubt, or Seal, deal 5 additional damage.",
            Seq(Damage(22), If(Unmarked(), Damage(5)))),

        StayOfExecution, StayOfExecution.Upgraded(
            "Choose an enemy with Paperwork. Its Paperwork does not trigger at the end of its next turn. " +
            "Gain 2 Block per current Paperwork on that enemy, maximum 28 Block.",
            If(HasStacks(Keywords.Paperwork),
                Seq(Apply(Keywords.StayOfExecution, 1),
                    Block(AtMost(Times(Stacks(Keywords.Paperwork), 2), 28))))),
    ];

    // A Rite card: playing it installs the status that carries the rule. Its upgrade installs a DIFFERENT
    // status ("<id>+"), because what changes is the rule, not a number in the card's own program.
    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    private static CombatNodeModel ResolveQueued(int count) =>
        new("resolveQueuedCards", You, CombatAmountSpec.FromConst(count));

    // "If the target intends to Attack" — the enemy's telegraph, read straight off it.
    private static CombatConditionSpec Intends(IntentKind kind, string who = Target) =>
        new("intends", who, Id: kind.ToString());

    // "Choose one: apply 1 Doubt; or apply 1 Seal." The card raises its own prompt; with nobody to ask, the
    // first option is taken.
    private static CombatNodeModel DoubtOrSeal() =>
        CombatNodeModel.ChooseOptions(
            1, ["apply 1 Doubt", "apply 1 Seal"],
            [Apply(Keywords.Doubt, 1), ApplySeal(1)],
            "choose one");

    // "Deal N damage. Archive a Junk card from your hand; if you do, repeat this attack." Whether there was
    // Junk to take is remembered in a counter, because a marker status would have to be removed again — and a
    // status losing stacks is something the general pool's Blood Ink answers.
    private static readonly CounterId CinderFed = new("cinder_warrant_fed");

    private static CombatNodeModel CinderStrike(int damage) =>
        Seq(
            Damage(damage),
            SetCounter(CinderFed, 0),
            CombatNodeModel.ForEachCard(You, CardZone.Hand,
                Seq(Archive(new CombatCardSpec("iterated")), SetCounter(CinderFed, 1)),
                tag: JunkTag, takeFirst: 1),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.Equal, Right: 1, Id: CinderFed.value),
                Damage(damage)));

    // "Deal N damage 3 times. If the target is Ratified, repeat this attack." The question is asked once,
    // before the first volley, because the volley itself can end the Ratification by killing the enemy.
    private static CombatNodeModel TribunalStrike(int damage) =>
        If(HasStacks(Keywords.Ratified),
            Repeat(6, Damage(damage)),
            Repeat(3, Damage(damage)));

    // "If the target has no Paperwork, Doubt, or Seal" — three separate absences, since a condition compares
    // one value at a time.
    private static CombatConditionSpec Unmarked() =>
        new("compare", Target, ValueKind: "statusStacks",
            Op: ComparisonOperator.Equal, Right: 0, Id: Keywords.Paperwork);

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
