using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// Act II of the Bureaucrat pool: archival logic, recursion, redaction, indexing and deliberate deck
// pollution. Where Act I seeds Paperwork and Junk, Act II is about PROCESSING them.
public static class BureaucratActII
{
    private const int Act = 2;

    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BroomDispatch = new(
        "broom_dispatch", "Broom Dispatch", WorkingTag, 1,
        "Apply 2 Paperwork to ALL enemies.",
        Apply(Keywords.Paperwork, 2, AllEnemies),
        Rarity: "common", Act: Act, Tags: [FormTag]);

    private static readonly BnbCard ErrataFurnace = new(
        "errata_furnace", "Errata Furnace", WorkingTag, 1,
        "Archive a Junk card from your hand. Apply 4 Paperwork to a random enemy.",
        BurnJunkFor(4),
        Rarity: "common", Act: Act);

    // "You may move 2 of that newly applied Paperwork to it" — spread rather than moved, which is the same
    // arithmetic without asking the player to point at anything. See ADAPTATIONS.
    private static readonly BnbCard CrossFiling = new(
        "cross_filing", "Cross-Filing", WorkingTag, 1,
        "Apply 4 Paperwork to an enemy. If another enemy is present, move 2 of it to them.",
        CrossFile(4, 2),
        Rarity: "common", Act: Act, Tags: [FormTag]);

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard PalimpsestOrder = new(
        "palimpsest_order", "Palimpsest Order", WorkingTag, 1,
        "Archive a card from your hand. Return a non-Junk card from your discard pile to your hand. Exhaust.",
        Seq(
            ArchiveChosen(),
            new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a card to take back"),
                ToZone: CardZone.Hand)),
        Rarity: "uncommon", Act: Act, Tags: [ExhaustTag]);

    private static readonly BnbCard RedactionVeil = new(
        "redaction_veil", "Redaction Veil", WorkingTag, 1,
        "Remove up to 4 Paperwork from an enemy. Gain 3 Block for each Paperwork removed.",
        RedactPaperwork(4, 3),
        Rarity: "uncommon", Act: Act);

    // "Look at the top 4 cards of your draw pile. Archive one. Put the others back in any order." Reordering
    // a revealed few is a prompt the engine does not raise; the card Archives a chosen card off the draw
    // pile instead, which is the part that matters. See ADAPTATIONS.
    private static readonly BnbCard SmudgedIndex = new(
        "smudged_index", "Smudged Index", WorkingTag, 1,
        "Archive a card from your draw pile. Gain 4 Block.",
        Seq(Archive(new CombatCardSpec("chosen", CardZone.DrawPile, Purpose: "choose a card to Archive")), Block(4)),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard ClutterConcordance = new(
        "clutter_concordance", "Clutter Concordance", DeedTag, 1,
        "Deal 5 damage, plus 2 damage for each different Junk type currently present across your discard and " +
        "Exhaust piles.",
        Damage(Plus(CombatAmountSpec.FromConst(5), Times(JunkTypesLyingAround(), 2))),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard Marginalia = new(
        "marginalia", "Marginalia", WorkingTag, 1,
        "Choose a card from your Exhaust pile. Create a Temporary copy in your hand; it Exhausts when played. " +
        "Marginalia Exhausts.",
        CopyFrom(CardZone.ExhaustPile, "choose a card to copy"),
        Rarity: "uncommon", Act: Act, Tags: [ExhaustTag]);

    private static readonly BnbCard BindingFee = new(
        "binding_fee", "Binding Fee", WorkingTag, 1,
        "Archive a non-Junk card from your hand. Apply Paperwork equal to 3 plus its base Energy cost.",
        BindingFeePaperwork(3),
        Rarity: "uncommon", Act: Act);

    // "For each different Junk type you have ARCHIVED this combat" — read off the Exhaust pile, which is
    // where Archived cards go. Junk that exhausted itself counts too; see ADAPTATIONS.
    private static readonly BnbCard DeadLetterOffice = new(
        "dead_letter_office", "Dead Letter Office", WorkingTag, 1,
        "For each different Junk type in your Exhaust pile, apply 1 Paperwork to ALL enemies. Exhaust.",
        Apply(Keywords.Paperwork, JunkTypesIn(CardZone.ExhaustPile), AllEnemies),
        Rarity: "uncommon", Act: Act, Tags: [ExhaustTag]);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard GhostRegister = Rite(
        "ghost_register", "Ghost Register", BureaucratArchive.GhostRegister, 2,
        "The first card you Archive each turn is recorded. At the start of your next turn, a Temporary copy " +
        "of it is added to your hand; it costs 0 and Exhausts when played.",
        rarity: "rare");

    private static readonly BnbCard ArchivePyre = new(
        "archive_pyre", "Archive Pyre", DeedTag, 2,
        "Archive all Junk cards in your hand. Deal 9 damage to ALL enemies, plus 5 damage for each Junk " +
        "Archived this way.",
        PyreStrike(9, 5),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard FuneralIndex = new(
        "funeral_index", "Funeral Index", DeedTag, 2,
        "Deal 5 damage for each card you have Archived this combat. Count at most 8 cards. Exhaust.",
        Damage(Times(AtMost(ArchivedCount, 8), 5)),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    private static readonly BnbCard NullCatalogue = new(
        "null_catalogue", "Null Catalogue", WorkingTag, 1,
        "Choose up to 2 cards in your discard pile. Archive them. Draw 1 card for each card Archived this " +
        "way. Exhaust.",
        Seq(ArchiveFromDiscard(), ArchiveFromDiscard()),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        BroomDispatch, BroomDispatch.Upgraded("Apply 3 Paperwork to ALL enemies.",
            Apply(Keywords.Paperwork, 3, AllEnemies)),

        ErrataFurnace, ErrataFurnace.Upgraded(
            "Archive a Junk card from your hand. Apply 5 Paperwork to a random enemy.", BurnJunkFor(5)),

        CrossFiling, CrossFiling.Upgraded(
            "Apply 5 Paperwork to an enemy. If another enemy is present, move 3 of it to them.",
            CrossFile(5, 3)),

        PalimpsestOrder, PalimpsestOrder.Upgraded(
            "Archive a card from your hand. Return a non-Junk card from your discard pile to your hand. Exhaust.",
            cost: 0),

        RedactionVeil, RedactionVeil.Upgraded(
            "Remove up to 5 Paperwork from an enemy. Gain 3 Block for each Paperwork removed.",
            RedactPaperwork(5, 3)),

        SmudgedIndex, SmudgedIndex.Upgraded("Archive a card from your draw pile. Gain 6 Block.",
            Seq(Archive(new CombatCardSpec("chosen", CardZone.DrawPile, Purpose: "choose a card to Archive")),
                Block(6))),

        ClutterConcordance, ClutterConcordance.Upgraded(
            "Deal 7 damage, plus 2 damage for each different Junk type currently present across your discard " +
            "and Exhaust piles.",
            Damage(Plus(CombatAmountSpec.FromConst(7), Times(JunkTypesLyingAround(), 2)))),

        Marginalia, Marginalia.Upgraded(
            "Choose a card from your Exhaust pile. Create a Temporary copy in your hand; it Exhausts when " +
            "played. Marginalia Exhausts."),

        BindingFee, BindingFee.Upgraded(
            "Archive a non-Junk card from your hand. Apply Paperwork equal to 4 plus its base Energy cost.",
            BindingFeePaperwork(4)),

        DeadLetterOffice, DeadLetterOffice.Upgraded(
            "For each different Junk type in your Exhaust pile, apply 1 Paperwork to ALL enemies, plus 1 " +
            "more. Exhaust.",
            Apply(Keywords.Paperwork, Plus(JunkTypesIn(CardZone.ExhaustPile), CombatAmountSpec.FromConst(1)),
                AllEnemies)),

        GhostRegister, GhostRegister.UpgradedRite(BureaucratArchive.GhostRegister,
            "The first card you Archive each turn is recorded. At the start of your next turn, a Temporary " +
            "copy of it is added to your hand; it costs 0 and Exhausts when played.", cost: 1),

        ArchivePyre, ArchivePyre.Upgraded(
            "Archive all Junk cards in your hand. Deal 12 damage to ALL enemies, plus 5 damage for each Junk " +
            "Archived this way.", PyreStrike(12, 5)),

        FuneralIndex, FuneralIndex.Upgraded(
            "Deal 6 damage for each card you have Archived this combat. Count at most 8 cards. Exhaust.",
            Damage(Times(AtMost(ArchivedCount, 8), 6))),

        NullCatalogue, NullCatalogue.Upgraded(
            "Choose up to 2 cards in your discard pile. Archive them. Draw 1 card for each card Archived this " +
            "way. Exhaust.", cost: 0),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity, Act: Act);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    // "Archive a Junk card from your hand. Apply N Paperwork to a random enemy." No Junk, no filing: the loop
    // over the hand's Junk IS the condition, and its body is the whole card.
    private static CombatNodeModel BurnJunkFor(int paperwork) =>
        CombatNodeModel.ForEachCard(You, CardZone.Hand,
            Seq(
                Archive(new CombatCardSpec("iterated")),
                CombatNodeModel.RandomTargets(AllEnemies, CombatAmountSpec.FromConst(1),
                    Apply(Keywords.Paperwork, paperwork, "iterationTarget"))),
            tag: JunkTag, takeFirst: 1);

    // "Apply N Paperwork to an enemy. If another enemy is present, you may move M of it to them."
    //
    // Moved rather than offered: the card cannot ask the player to point at a second enemy, so with company
    // present the move simply happens. It is written as a spread over EVERY enemy and a double subtraction on
    // the original target, which nets to exactly "M off the target, M onto each other enemy" — and it is
    // skipped outright when the target is alone, which is what the card says. Counting the enemies goes
    // through a scratch counter, since a condition cannot compare a count.
    private static readonly CounterId EnemiesPresent = new("cross_filing_company");

    private static CombatNodeModel CrossFile(int paperwork, int moved) =>
        Seq(
            Apply(Keywords.Paperwork, paperwork),
            new CombatNodeModel("setCombatantCounter", You,
                new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec(AllEnemies)),
                CounterId: EnemiesPresent.value, Relative: false),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.GreaterOrEqual, Right: 2, Id: EnemiesPresent.value),
                Seq(
                    Apply(Keywords.Paperwork, moved, AllEnemies),
                    Remove(Keywords.Paperwork, moved * 2))));

    // "Remove up to N Paperwork from an enemy. Gain M Block for each removed." How much was there has to be
    // read before it is taken away, so it goes through a scratch counter.
    private static readonly CounterId Redacted = new("redaction_veil_taken");

    private static CombatNodeModel RedactPaperwork(int cap, int blockEach) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", Target,
                AtMost(Stacks(Keywords.Paperwork), cap), CounterId: Redacted.value, Relative: false),
            Block(Times(new CombatAmountSpec("counter", SelectorKey: Target, CounterId: Redacted.value), blockEach)),
            new CombatNodeModel("modifyStatusStacks", Target,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0),
                    new CombatAmountSpec("counter", SelectorKey: Target, CounterId: Redacted.value)),
                StatusId: Keywords.Paperwork));

    // "Archive a non-Junk card from your hand. Apply Paperwork equal to N plus its base Energy cost."
    private static readonly CounterId FeeCost = new("binding_fee_cost");

    private static CombatNodeModel BindingFeePaperwork(int baseAmount)
    {
        var chosen = new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to Archive");
        return Seq(
            new CombatNodeModel("setCombatantCounter", You,
                new CombatAmountSpec("cardCost", ReadId: Energy.value, ReadCard: chosen),
                CounterId: FeeCost.value, Relative: false),
            Archive(chosen),
            Apply(Keywords.Paperwork,
                Plus(CombatAmountSpec.FromConst(baseAmount),
                    new CombatAmountSpec("counter", SelectorKey: You, CounterId: FeeCost.value))));
    }

    // "Archive all Junk cards in your hand. Deal N damage to ALL enemies, plus M for each Junk Archived."
    // The tally is taken before the burning, because afterwards there is nothing left to count.
    private static readonly CounterId PyreFuel = new("archive_pyre_fuel");

    private static CombatNodeModel PyreStrike(int damage, int perJunk) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, CardsTagged(JunkTag),
                CounterId: PyreFuel.value, Relative: false),
            CombatNodeModel.ForEachCard(You, CardZone.Hand, Archive(new CombatCardSpec("iterated")), tag: JunkTag),
            Damage(Plus(CombatAmountSpec.FromConst(damage),
                Times(new CombatAmountSpec("counter", SelectorKey: You, CounterId: PyreFuel.value), perJunk)),
                AllEnemies));

    private static CombatNodeModel ArchiveFromDiscard() =>
        Seq(
            Archive(new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a card to Archive")),
            Draw(1));

    // A Temporary copy of a chosen card, which Exhausts when played and cannot itself be copied again.
    private static CombatNodeModel CopyFrom(CardZone zone, string purpose) =>
        new("createCardCopy", You, CombatAmountSpec.FromConst(1),
            Card: new CombatCardSpec("chosen", zone, Purpose: purpose), ToZone: CardZone.Hand);

    // How many DIFFERENT Junk types are lying in the discard and Exhaust piles. Each Junk card carries its own
    // id as a tag, so "a type is present" is a tag count clamped to one, and the four are added up.
    private static CombatAmountSpec JunkTypesLyingAround()
    {
        CombatAmountSpec? total = null;
        foreach (var type in BureaucratStarter.JunkTypes)
        {
            var present = Once(Plus(
                new CombatAmountSpec("zoneCards", SelectorKey: You, ReadId: type, Zone: CardZone.DiscardPile),
                new CombatAmountSpec("zoneCards", SelectorKey: You, ReadId: type, Zone: CardZone.ExhaustPile)));
            total = total is null ? present : Plus(total, present);
        }
        return total!;
    }

    private static CombatAmountSpec JunkTypesIn(CardZone zone)
    {
        CombatAmountSpec? total = null;
        foreach (var type in BureaucratStarter.JunkTypes)
        {
            var present = Once(new CombatAmountSpec("zoneCards", SelectorKey: You, ReadId: type, Zone: zone));
            total = total is null ? present : Plus(total, present);
        }
        return total!;
    }
}
