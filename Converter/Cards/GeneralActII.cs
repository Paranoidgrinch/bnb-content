using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// Act II of the general pool: copying, conversion and stronger status manipulation. Raw endgame damage is
// still held back — these cards move statuses around rather than simply adding more of them.
public static class GeneralActII
{
    private const int Act = 2;

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard Blacklisted = new(
        "blacklisted", "Blacklisted", WorkingTag, 1,
        "Apply 2 Censure. For each different positive Status already on the target, apply 1 additional " +
        "Censure, maximum +3.",
        Blacklist(2),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard SanguineErrata = new(
        "sanguine_errata", "Sanguine Errata", WorkingTag, 1,
        "Apply 2 Blood Ink. Then remove 1 stack of another negative Status from the target.",
        SanguineErrataBody(2),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard CountermandedGrace = Rite(
        "countermanded_grace", "Countermanded Grace", GeneralPrevention.CountermandedGrace, 1,
        "The first time each turn Censure prevents any Status stack, gain 2 Ward Wax. This may trigger from " +
        "Censure on you or on an enemy.");

    private static readonly BnbCard VeinRegister = Rite(
        "vein_register", "Vein Register", GeneralPrevention.VeinRegister, 1,
        "The first time each turn another Status on an enemy loses a stack, apply 1 Blood Ink to it.");

    private static readonly BnbCard CrossedSigil = new(
        "crossed_sigil", "Crossed Sigil", WorkingTag, 1,
        "Remove 1 stack of a negative Status from yourself. Then apply 1 Censure to an enemy. If you had no " +
        "negative Status to remove, gain 1 Censure instead.",
        CrossSigil(1),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard ProxyCurse = new(
        "proxy_curse", "Proxy Curse", WorkingTag, 1,
        "Remove up to 3 stacks of a negative Status from yourself. Apply 1 Blood Ink to an enemy per stack " +
        "removed.",
        Convert(from: You, to: Target, cap: 3, into: Keywords.BloodInk),
        Rarity: "uncommon", Act: Act);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BloodRedaction = new(
        "blood_redaction", "Blood Redaction", WorkingTag, 1,
        "Remove up to 6 stacks of a negative Status from an enemy. Apply the same number of Blood Ink. Exhaust.",
        Convert(from: Target, to: Target, cap: 6, into: Keywords.BloodInk),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    // "Choose a non-Rite card in your hand. Create a Temporary copy that costs 0 this turn. Exhaust the
    // original." The copy is free through one free play rather than a price on the card itself; the original
    // is a second prompt, and the upgrade simply spares it. See ADAPTATIONS.
    private static readonly BnbCard MoonlitCounterfeit = new(
        "moonlit_counterfeit", "Moonlit Counterfeit", WorkingTag, 1,
        "Create a Temporary copy of a card in your hand; your next card this turn is free. Exhaust the " +
        "original. Moonlit Counterfeit Exhausts.",
        Seq(Counterfeit(), ExhaustChosen()),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    private static readonly BnbCard SeizureWrit = new(
        "seizure_writ", "Seizure Writ", DeedTag, 2,
        "Deal 12 damage. Then remove all remaining Block from the target. For every 3 Block removed, apply 1 " +
        "Lien, maximum 6 Lien.",
        Seize(12, per: 3),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard StandingCitation = Rite(
        "standing_citation", "Standing Citation", GeneralPrevention.StandingCitation, 2,
        "The first time each turn Citation triggers on each enemy, that trigger does not remove a Citation " +
        "stack.",
        rarity: "rare");

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        Blacklisted, Blacklisted.Upgraded(
            "Apply 3 Censure. For each different positive Status already on the target, apply 1 additional " +
            "Censure, maximum +3.", Blacklist(3)),

        SanguineErrata, SanguineErrata.Upgraded(
            "Apply 3 Blood Ink. Then remove 1 stack of another negative Status from the target.",
            SanguineErrataBody(3)),

        CountermandedGrace, CountermandedGrace.UpgradedRite(GeneralPrevention.CountermandedGrace,
            "The first time each turn Censure prevents any Status stack, gain 3 Ward Wax."),

        VeinRegister, VeinRegister.UpgradedRite(GeneralPrevention.VeinRegister,
            "The first time each turn another Status on an enemy loses a stack, apply 1 Blood Ink to it.",
            cost: 0),

        CrossedSigil, CrossedSigil.Upgraded(
            "Remove 1 stack of a negative Status from yourself. Then apply 2 Censure to an enemy. If you had " +
            "no negative Status to remove, gain 2 Censure instead.", CrossSigil(2)),

        ProxyCurse, ProxyCurse.Upgraded(
            "Remove up to 4 stacks of a negative Status from yourself. Apply 1 Blood Ink to an enemy per " +
            "stack removed.", Convert(from: You, to: Target, cap: 4, into: Keywords.BloodInk)),

        BloodRedaction, BloodRedaction.Upgraded(
            "Remove up to 8 stacks of a negative Status from an enemy. Apply the same number of Blood Ink. " +
            "Exhaust.", Convert(from: Target, to: Target, cap: 8, into: Keywords.BloodInk)),

        MoonlitCounterfeit, MoonlitCounterfeit.Upgraded(
            "Create a Temporary copy of a card in your hand; your next card this turn is free. Moonlit " +
            "Counterfeit Exhausts.", Counterfeit()),

        SeizureWrit, SeizureWrit.Upgraded(
            "Deal 15 damage. Then remove all remaining Block from the target. For every 2 Block removed, " +
            "apply 1 Lien, maximum 6 Lien.", Seize(15, per: 2)),

        StandingCitation, StandingCitation.UpgradedRite(GeneralPrevention.StandingCitation,
            "The first time each turn Citation triggers on each enemy, that trigger does not remove a " +
            "Citation stack.", cost: 1),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity, Act: Act);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    // "For each DIFFERENT positive Status already on the target, +1 Censure, maximum +3." Distinct statuses
    // are counted by naming the ones the game files and asking each whether it is there.
    private static CombatNodeModel Blacklist(int baseCensure) =>
        Apply(Keywords.Censure,
            Plus(CombatAmountSpec.FromConst(baseCensure),
                AtMost(DistinctStatuses(PositiveStatuses), 3)));

    // "Then choose and remove 1 stack of another Status." One stack off a negative status the engine picks by
    // rule rather than by prompt — the selection is polarity-filtered, so Blood Ink itself can be the one
    // chosen. See ADAPTATIONS.
    private static CombatNodeModel SanguineErrataBody(int bloodInk) =>
        Seq(
            Apply(Keywords.BloodInk, bloodInk),
            new CombatNodeModel("modifySelectedStatusStacks", Target, CombatAmountSpec.FromConst(-1),
                Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff)));

    // "Remove up to N stacks of a negative Status; turn each removed stack into something else." How much can
    // be taken has to be known before it is taken, so it goes through a scratch counter.
    private static CounterId Converted => new("status_conversion");

    private static CombatNodeModel Convert(string from, string to, int cap, string into) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", from,
                AtMost(new CombatAmountSpec("stacksByPolarity", SelectorKey: from,
                    Polarity: StatusPolarity.Debuff), cap),
                CounterId: Converted.value, Relative: false),
            new CombatNodeModel("modifySelectedStatusStacks", from,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0), Taken(from)),
                Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff)),
            Apply(into, Taken(from), to));

    private static CombatAmountSpec Taken(string on) =>
        new("counter", SelectorKey: on, CounterId: Converted.value);

    // "Remove 1 stack of a negative Status from yourself. Then apply N Censure to an enemy. If you had none
    // to remove, gain N Censure instead." What you were carrying is read before anything is taken.
    private static CounterId Carried => new("crossed_sigil_carried");

    private static CombatNodeModel CrossSigil(int censure) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You,
                new CombatAmountSpec("stacksByPolarity", SelectorKey: You, Polarity: StatusPolarity.Debuff),
                CounterId: Carried.value, Relative: false),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.Greater, Right: 0, Id: Carried.value),
                Seq(
                    new CombatNodeModel("modifySelectedStatusStacks", You, CombatAmountSpec.FromConst(-1),
                        Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff)),
                    Apply(Keywords.Censure, censure)),
                Apply(Keywords.Censure, censure, You)));

    // "Remove all remaining Block. For every N removed, apply 1 Lien, max 6." What was there is read before
    // it is taken.
    private static CounterId Seized => new("seizure_writ_taken");

    private static CombatNodeModel Seize(int damage, int per) =>
        Seq(
            Damage(damage),
            new CombatNodeModel("setCombatantCounter", Target,
                new CombatAmountSpec("defensivePool", SelectorKey: Target,
                    ReadId: StandardCombatIds.BlockDefensivePool.value),
                CounterId: Seized.value, Relative: false),
            new CombatNodeModel("modifyDefensivePool", Target,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0),
                    new CombatAmountSpec("counter", SelectorKey: Target, CounterId: Seized.value)),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            Apply(Keywords.Lien,
                AtMost(CombatAmountSpec.Binary("div",
                    new CombatAmountSpec("counter", SelectorKey: Target, CounterId: Seized.value),
                    CombatAmountSpec.FromConst(per)), 6)));

    private static CombatNodeModel Counterfeit() =>
        Seq(
            new CombatNodeModel("createCardCopy", You, CombatAmountSpec.FromConst(1),
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to copy"),
                ToZone: CardZone.Hand),
            Apply(StandardCombatIds.FreeNextCardStatus.value, 1, You));

    private static CombatNodeModel ExhaustChosen() =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose the original to Exhaust"),
            ToZone: CardZone.ExhaustPile);
}
