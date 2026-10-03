using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// The character-unspecific Act-I reward cards, from `source-data/design/general_final_cards.md`.
//
// The general pool has no Commons on purpose: these cards should bend or enrich a run, not replace a
// character's basics. Act I is the readable introduction to the five general statuses — Censure, Lien,
// Citation, Blood Ink, Ward Wax — plus a few flexible tools.
public static class GeneralActI
{
    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard MaledictionReview = new(
        "malediction_review", "Malediction Review", WorkingTag, 1,
        "Gain 6 Block. Choose one: gain 3 Censure; or apply 3 Censure to an enemy.",
        Seq(Block(6), CensureChoice()),
        Rarity: "uncommon");

    private static readonly BnbCard GraveLien = new(
        "grave_lien", "Grave Lien", DeedTag, 2,
        "Deal 13 damage. Apply 9 Lien.",
        Seq(Damage(13), Apply(Keywords.Lien, 9)),
        Rarity: "uncommon");

    // "If the target currently intends a non-damaging action" — everything the telegraph can say except an
    // Attack. Asked as the ELSE of "intends to Attack", which is the same question the other way round.
    private static readonly BnbCard WitchmarkCitation = new(
        "witchmark_citation", "Witchmark Citation", WorkingTag, 1,
        "Apply 5 Citation. If the target currently intends a non-damaging action, draw 1 card.",
        Seq(Apply(Keywords.Citation, 5), IfIntendsHarm(then: Seq(), otherwise: Draw(1))),
        Rarity: "uncommon");

    private static readonly BnbCard BloodMarginalia = new(
        "blood_marginalia", "Blood Marginalia", WorkingTag, 1,
        "Apply 5 Citation and 3 Blood Ink.",
        Seq(Apply(Keywords.Citation, 5), Apply(Keywords.BloodInk, 3)),
        Rarity: "uncommon");

    private static readonly BnbCard WaxenSurety = new(
        "waxen_surety", "Waxen Surety", WorkingTag, 1,
        "Gain 7 Ward Wax.",
        Apply(Keywords.WardWax, 7, You),
        Rarity: "uncommon");

    private static readonly BnbCard Foreclosure = new(
        "foreclosure", "Foreclosure", DeedTag, 1,
        "Deal 6 damage. Then immediately resolve up to 5 Lien on the target.",
        Seq(Damage(6), ResolveLienNow(5)),
        Rarity: "uncommon");

    private static readonly BnbCard ContemptFinding = new(
        "contempt_finding", "Contempt Finding", WorkingTag, 1,
        "Remove all Citation from an enemy. Gain 2 Block per Citation removed.",
        CashOutCitation(2),
        Rarity: "uncommon");

    private static readonly BnbCard TallowReserve = new(
        "tallow_reserve", "Tallow Reserve", WorkingTag, 0,
        "Requires at least 6 Block. Lose 6 Block. Gain 4 Ward Wax. Exhaust.",
        SpendBlockForWax(6, 4),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    private static readonly BnbCard MortgageSigil = new(
        "mortgage_sigil", "Mortgage Sigil", WorkingTag, 1,
        "Apply 3 Lien. The next time the target gains Block before the end of its next turn, apply 3 " +
        "additional Lien.",
        Seq(Apply(Keywords.Lien, 3), Apply(GeneralRites.MortgageSigil, 1)),
        Rarity: "uncommon");

    private static readonly BnbCard SilentHearing = new(
        "silent_hearing", "Silent Hearing", WorkingTag, 1,
        "Apply 3 Citation. Until your next turn, if the target performs a damaging action, gain 7 Block.",
        Seq(Apply(Keywords.Citation, 3), Apply(GeneralRites.SilentHearing, 1)),
        Rarity: "uncommon");

    private static readonly BnbCard SealedMantle = new(
        "sealed_mantle", "Sealed Mantle", WorkingTag, 1,
        "Gain 8 Block. If at least one enemy attacks during this enemy turn and you take no unblocked Attack " +
        "damage, gain 3 Ward Wax.",
        Seq(Block(8), Apply(GeneralRites.SealedMantle, 1, You)),
        Rarity: "uncommon");

    private static readonly BnbCard BorrowedCandle = new(
        "borrowed_candle", "Borrowed Candle", WorkingTag, 0,
        "Draw 2 cards. Put one card from your hand on top of your draw pile. Exhaust.",
        Seq(Draw(2), PutBack(ZonePlacement.Top)),
        Rarity: "uncommon", Tags: [ExhaustTag]);

    private static readonly BnbCard NotaryBeetle = Rite(
        "notary_beetle", "Notary Beetle", GeneralRites.NotaryBeetle, 1,
        "The first time each turn you apply a negative Status to an enemy that does not already have that " +
        "Status, apply 1 additional stack of it.");

    private static readonly BnbCard SanctionedCharm = new(
        "sanctioned_charm", "Sanctioned Charm", WorkingTag, 1,
        "Gain 6 Block. Until your next turn, the first time your Censure prevents a negative Status, the " +
        "Censure used to prevent it is not consumed.",
        Seq(Block(6), Apply(GeneralRites.SanctionedCharm, 1, You)),
        Rarity: "uncommon");

    private static readonly BnbCard ForfeitSeal = new(
        "forfeit_seal", "Forfeit Seal", DeedTag, 1,
        "Deal 7 damage. If the target still has Block after this attack, apply 4 Lien.",
        Seq(Damage(7), IfStillGuarded(Apply(Keywords.Lien, 4))),
        Rarity: "uncommon");

    // "Choose a card in your hand. It costs 1 less this turn." — narrowed to "your next card", because the
    // engine prices a card by what its owner is WEARING, not by a mark on one card in hand. See ADAPTATIONS.
    private static readonly BnbCard FalseSignature = new(
        "false_signature", "False Signature", WorkingTag, 0,
        "Your next card this turn costs 1 less Energy. After it is played, the next card you play this combat " +
        "costs 1 more. Exhaust.",
        Apply(GeneralForgery.Discount, 1, You),
        Rarity: "uncommon", Tags: [ExhaustTag, GeneralForgery.ForgeryTag]);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard DawnSummons = new(
        "dawn_summons", "Dawn Summons", DeedTag, 2,
        "Deal 16 damage. If this is the first card you play this turn, deal 10 additional damage.",
        DawnStrike(16),
        Rarity: "rare");

    private static readonly BnbCard ReciprocalEdict = Rite(
        "reciprocal_edict", "Reciprocal Edict", GeneralRites.ReciprocalEdict, 2,
        "The first time each turn your Censure prevents a negative Status applied by an enemy, apply 3 " +
        "Censure to that enemy. The first time each turn Censure prevents a positive Status on an enemy, " +
        "gain 2 Censure.",
        rarity: "rare");

    private static readonly BnbCard UsurersMoon = Rite(
        "usurers_moon", "Usurer's Moon", Keywords.UsurersMoon, 1,
        "Whenever Lien removes Block from an enemy, apply 1 Citation for every 2 Block removed, maximum 5 " +
        "Citation per Lien resolution.",
        rarity: "rare");

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        MaledictionReview, MaledictionReview.Upgraded(
            "Gain 9 Block. Gain 3 Censure and apply 3 Censure to an enemy.",
            Seq(Block(9), Apply(Keywords.Censure, 3, You), Apply(Keywords.Censure, 3))),

        GraveLien, GraveLien.Upgraded("Deal 15 damage and apply 9 Lien to ALL enemies.",
            Seq(Damage(15, AllEnemies), Apply(Keywords.Lien, 9, AllEnemies))),

        WitchmarkCitation, WitchmarkCitation.Upgraded(
            "Apply 8 Citation. If the target currently intends a non-damaging action, apply 3 more.",
            Seq(Apply(Keywords.Citation, 8), IfIntendsHarm(then: Seq(), otherwise: Apply(Keywords.Citation, 3)))),

        BloodMarginalia, BloodMarginalia.Upgraded("Apply 6 Citation and 5 Blood Ink.",
            Seq(Apply(Keywords.Citation, 6), Apply(Keywords.BloodInk, 5))),

        WaxenSurety, WaxenSurety.Upgraded("Gain 7 Ward Wax. Gain Block equal to your Ward Wax.",
            Seq(Apply(Keywords.WardWax, 7, You), Block(Stacks(Keywords.WardWax, You)))),

        Foreclosure, Foreclosure.Upgraded("Deal 9 damage. Then immediately resolve up to 8 Lien on the target.",
            Seq(Damage(9), ResolveLienNow(8))),

        ContemptFinding, ContemptFinding.Upgraded(
            "Remove all Citation from an enemy. Gain 3 Block per Citation removed; it takes 2 damage per Citation " +
            "removed.", CashOutCitation(3, damageEach: 2)),

        TallowReserve, TallowReserve.Upgraded(
            "Requires at least 6 Block. Lose 6 Block. Gain 7 Ward Wax. Exhaust.", SpendBlockForWax(6, 7)),

        MortgageSigil, MortgageSigil.Upgraded(
            "Apply 5 Lien. The next time the target gains Block before the end of its next turn, apply 5 " +
            "additional Lien.",
            Seq(Apply(Keywords.Lien, 5), Apply(GeneralRites.MortgageSigil + "+", 1))),

        SilentHearing, SilentHearing.Upgraded(
            "Apply 5 Citation. Until your next turn, if the target performs a damaging action, gain 10 Block.",
            Seq(Apply(Keywords.Citation, 5), Apply(GeneralRites.SilentHearing + "+", 1))),

        SealedMantle, SealedMantle.Upgraded(
            "Gain 12 Block. If at least one enemy attacks during this enemy turn and you take no unblocked " +
            "Attack damage, gain 4 Ward Wax.",
            Seq(Block(12), Apply(GeneralRites.SealedMantle + "+", 1, You))),

        BorrowedCandle, BorrowedCandle.Upgraded(
            "Draw 3 cards. Put one card from your hand on top of your draw pile. Exhaust.",
            Seq(Draw(3), PutBack(ZonePlacement.Top))),

        NotaryBeetle, NotaryBeetle.UpgradedRite(GeneralRites.NotaryBeetle,
            "The first time each turn you apply a negative Status to an enemy that does not already have " +
            "that Status, apply 1 additional stack of it.", cost: 0),

        SanctionedCharm, SanctionedCharm.Upgraded(
            "Gain 9 Block and 2 Censure. Until your next turn, the first time your Censure prevents a negative " +
            "Status, the Censure used to prevent it is not consumed.",
            Seq(Block(9), Apply(Keywords.Censure, 2, You), Apply(GeneralRites.SanctionedCharm, 1, You))),

        ForfeitSeal, ForfeitSeal.Upgraded(
            "Deal 11 damage. Apply 4 Lien; 8 if the target still has Block after this attack.",
            Seq(Damage(11), Apply(Keywords.Lien, 4), IfStillGuarded(Apply(Keywords.Lien, 4)))),

        FalseSignature, FalseSignature.Upgraded(
            "Your next card this turn costs 2 less Energy. After it is played, the next card you play this " +
            "combat costs 1 more. Exhaust.",
            Apply(GeneralForgery.DiscountPlus, 1, You)),

        DawnSummons, DawnSummons.Upgraded(
            "Deal 27 damage. If this is the first card you play this turn, deal 12 additional damage.",
            DawnStrike(27, bonus: 12)),

        ReciprocalEdict, ReciprocalEdict.UpgradedRite(GeneralRites.ReciprocalEdict,
            "The first time each turn your Censure prevents a negative Status applied by an enemy, apply 3 " +
            "Censure to that enemy. The first time each turn Censure prevents a positive Status on an enemy, " +
            "gain 2 Censure.", cost: 1),

        UsurersMoon, UsurersMoon.UpgradedRite(Keywords.UsurersMoon,
            "Whenever Lien removes Block from an enemy, apply 3 Citation for every 4 Block removed, maximum " +
            "5 Citation per Lien resolution."),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    // "Choose one: gain 3 Censure; or apply 3 Censure to an enemy." Both halves of Censure on one card, which
    // is how the general pool introduces a status that reads differently on each side of the fight.
    private static CombatNodeModel CensureChoice() =>
        CombatNodeModel.ChooseOptions(
            1, ["gain 3 Censure", "apply 3 Censure to an enemy"],
            [Apply(Keywords.Censure, 3, You), Apply(Keywords.Censure, 3)],
            "choose one");

    // "If the target intends a damaging action" as a branch with both arms, so the ELSE is the non-damaging
    // case the card actually asks about.
    private static CombatNodeModel IfIntendsHarm(CombatNodeModel then, CombatNodeModel otherwise) =>
        If(new CombatConditionSpec("intends", Target, Id: nameof(IntentKind.Attack)), then, otherwise);

    // "Resolve up to N Lien on the target immediately" — the same resolution the status runs at a turn's end,
    // capped, and paid out of the scratch counter that makes it possible at all (see Keywords.ResolveLien).
    // Like that resolution, it no longer spends the Lien: on an enemy, Lien stays (user, 2026-10-02).
    private static CombatNodeModel ResolveLienNow(int cap) =>
        Seq(
            SetCounterOn(Target, Keywords.LienResolvedCounter,
                CombatAmountSpec.Binary("min",
                    CombatAmountSpec.Binary("min",
                        new CombatAmountSpec("defensivePool", SelectorKey: Target,
                            ReadId: StandardCombatIds.BlockDefensivePool.value),
                        Stacks(Keywords.Lien)),
                    CombatAmountSpec.FromConst(cap))),
            new CombatNodeModel("modifyDefensivePool", Target,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0), Taken()),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            new CombatNodeModel("dealDamage", Target, Taken(),
                IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime));

    private static CombatAmountSpec Taken() =>
        new("counter", SelectorKey: Target, CounterId: Keywords.LienResolvedCounter.value);

    // "Remove all Citation from an enemy. Gain N Block per Citation removed." How much was there has to be
    // read before it is taken away, so it is written to a scratch counter first.
    private static CounterId CitationFound => new("contempt_finding_found");

    private static CombatNodeModel CashOutCitation(int blockEach, int damageEach = 0) =>
        Seq(
            SetCounterOn(Target, CitationFound, Stacks(Keywords.Citation)),
            Block(Times(new CombatAmountSpec("counter", SelectorKey: Target, CounterId: CitationFound.value), blockEach)),
            damageEach > 0
                ? Damage(Times(new CombatAmountSpec("counter", SelectorKey: Target, CounterId: CitationFound.value), damageEach))
                : Seq(),
            new CombatNodeModel("removeStatus", Target, StatusId: Keywords.Citation));

    // "Requires at least N Block. Lose N Block. Gain M Ward Wax." A condition compares a value read off a
    // combatant, and Block is not one of the values it can read — so the Block is written to a scratch counter
    // and the counter is what the card asks about.
    private static CounterId BlockOnHand => new("tallow_reserve_block");

    private static CombatNodeModel SpendBlockForWax(int price, int wax) =>
        Seq(
            SetCounterOn(You, BlockOnHand,
                new CombatAmountSpec("defensivePool", SelectorKey: You,
                    ReadId: StandardCombatIds.BlockDefensivePool.value)),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.GreaterOrEqual, Right: price, Id: BlockOnHand.value),
                Seq(
                    new CombatNodeModel("modifyDefensivePool", You, CombatAmountSpec.FromConst(-price),
                        PoolId: StandardCombatIds.BlockDefensivePool.value),
                    Apply(Keywords.WardWax, wax, You))));

    private static CombatNodeModel PutBack(ZonePlacement where) =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to put back"),
            ToZone: CardZone.DrawPile, Placement: where);

    // "If the target still has Block after this attack" — Block is not a value a condition can read, so it
    // goes through a scratch counter like everything else that asks about it.
    private static CounterId GuardLeft => new("forfeit_seal_guard");

    private static CombatNodeModel IfStillGuarded(CombatNodeModel then) =>
        Seq(
            SetCounterOn(Target, GuardLeft,
                new CombatAmountSpec("defensivePool", SelectorKey: Target,
                    ReadId: StandardCombatIds.BlockDefensivePool.value)),
            If(new CombatConditionSpec("compare", Target, ValueKind: "counter",
                    Op: ComparisonOperator.Greater, Right: 0, Id: GuardLeft.value),
                then));

    // "If this is the first card you play this turn" — asked before anything else the card does, when the
    // count still excludes the card in flight, so "first" means "none before it". How many were played is an
    // amount, not one of the values a condition can read, so it goes through a scratch counter.
    private static CounterId PlayedSoFar => new("dawn_summons_played");

    private static CombatNodeModel DawnStrike(int damage, int bonus = 10) =>
        Seq(
            SetCounterOn(You, PlayedSoFar, new CombatAmountSpec("cardsPlayedThisTurn", SelectorKey: You)),
            Damage(damage),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.Equal, Right: 0, Id: PlayedSoFar.value),
                Damage(bonus)));

    private static CombatNodeModel SetCounterOn(string who, CounterId counter, CombatAmountSpec amount) =>
        new("setCombatantCounter", who, amount, CounterId: counter.value, Relative: false);
}
