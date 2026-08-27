using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// Act IV, both pools. The last tier before the boss gauntlet, and the only one the deck gets to answer Act V
// with — so these are deliberately large: tallies, measures, conversions and finishers.
public static class ActIVCards
{
    private const int Act = 4;

    // ── Bureaucrat ────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard FinalAttestation = new(
        "final_attestation", "Final Attestation", DeedTag, 1,
        "Deal 8 damage. If the target is Ratified, gain 1 Energy.",
        Seq(Damage(8), If(HasStacks(Keywords.Ratified), Energy_(1))),
        Rarity: "common", Act: Act);

    private static readonly BnbCard TempleTally = Rite(
        "temple_tally", "Temple Tally", ActIVRites.TempleTally, 1,
        "Whenever an enemy reaches a new multiple of 5 Paperwork for the first time this combat, apply 1 Seal " +
        "to it for each new multiple crossed.");

    private static readonly BnbCard ProcessionalCalendar = Rite(
        "processional_calendar", "Processional Calendar", ActIVRites.ProcessionalCalendar, 1,
        "At the end of your turn, if you have at least 2 Queued cards, resolve your oldest Queued card.");

    private static readonly BnbCard HieraticMeasure = Rite(
        "hieratic_measure", "Hieratic Measure", ActIVRites.HieraticMeasure, 2,
        "Whenever you Ratify an enemy, immediately trigger its current Paperwork once, then remove 3 " +
        "Paperwork from it.");

    private static readonly BnbCard CartoucheReckoning = new(
        "cartouche_reckoning", "Cartouche Reckoning", DeedTag, 3,
        "Deal 18 damage. Then, up to 3 times: if the target has at least 10 Paperwork, remove 10 Paperwork " +
        "and repeat this attack.",
        Reckoning(18),
        Rarity: "rare", Act: Act);

    // "Deal 12 additional damage for each other card that resolved from your Queue after this was queued." The
    // Queue resolves oldest first, so what resolved before it is what is left waiting behind it — counted at
    // resolution, capped at 3. See ADAPTATIONS.
    private static readonly BnbCard MonumentalWrit = new(
        "monumental_writ", "Monumental Writ", DeedTag, 3,
        "Queue: Deal 24 damage, plus 12 for each other card still in your Queue when this resolves. Count at " +
        "most 3.",
        Writ(24),
        Rarity: "rare", Act: Act, Queued: true);

    private static readonly BnbCard StoneLevy = new(
        "stone_levy", "Stone Levy", DeedTag, 2,
        "Remove up to 20 of your Block. Deal 10 damage plus 2 damage for each Block removed.",
        Levy(20, 10),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard FivefoldCompliance = new(
        "fivefold_compliance", "Fivefold Compliance", DeedTag, 3,
        "Deal 12 damage, then repeat once for each fulfilled clause: the target has at least 10 Paperwork; " +
        "at least 3 Doubt; is Ratified; you hold 2 different Junk types in your Exhaust pile; you have a " +
        "Queued card.",
        Fivefold(12),
        Rarity: "rare", Act: Act);

    // ── General ───────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BlackTribunal = new(
        "black_tribunal", "Black Tribunal", DeedTag, 2,
        "Deal 14 damage, plus 8 damage for each different negative Status on the target. Count at most 5.",
        Tribunal(14),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard SovereignProhibition = new(
        "sovereign_prohibition", "Sovereign Prohibition", WorkingTag, 2,
        "Gain 3 Censure. Apply 3 Censure to ALL enemies.",
        Seq(Apply(Keywords.Censure, 3, You), Apply(Keywords.Censure, 3, AllEnemies)),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard CandleCathedral = Rite(
        "candle_cathedral", "Candle Cathedral", ActIVRites.CandleCathedral, 2,
        "Whenever Ward Wax grants Block, gain additional Block equal to half your Ward Wax, rounded up. Ward " +
        "Wax no longer suffers its additional decay.");

    private static readonly BnbCard GrandCitation = new(
        "grand_citation", "Grand Citation", DeedTag, 2,
        "Deal 14 damage to ALL enemies. Each enemy with Citation additionally loses HP equal to 3 times its " +
        "Citation, then loses 1 Citation.",
        GrandCitationBody(14),
        Rarity: "uncommon", Act: Act);

    private static readonly BnbCard CrownRepossession = new(
        "crown_repossession", "Crown Repossession", DeedTag, 3,
        "Deal 22 damage. Remove all remaining Block from the target; it loses HP equal to the Block removed, " +
        "maximum 40. Apply 6 Lien.",
        Repossess(22, 40),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard AbsoluteInterdict = Rite(
        "absolute_interdict", "Absolute Interdict", ActIVRites.AbsoluteInterdict, 2,
        "The first time each turn Censure on a combatant would prevent Status stacks, 1 Censure prevents the " +
        "entire application instead, however many stacks it carried.",
        rarity: "rare");

    private static readonly BnbCard TallowJudgment = new(
        "tallow_judgment", "Tallow Judgment", DeedTag, 2,
        "Consume up to 8 Ward Wax. Deal 10 damage plus 7 damage per Ward Wax consumed.",
        Judgment(8, 10, 7),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard HemalAudit = new(
        "hemal_audit", "Hemal Audit", DeedTag, 2,
        "Deal 18 damage. Then trigger Blood Ink repeatedly, up to 6 times or until no Blood Ink remains.",
        Audit(18, 6),
        Rarity: "rare", Act: Act);

    private static readonly BnbCard CompoundIndictment = new(
        "compound_indictment", "Compound Indictment", WorkingTag, 1,
        "Requires at least 3 different negative Statuses on the target. Add 2 stacks to each negative Status " +
        "it carries, up to 5 of them. Exhaust.",
        Compound(2),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    private static readonly BnbCard GrandDispensation = new(
        "grand_dispensation", "Grand Dispensation", WorkingTag, 2,
        "Choose 2 different options: deal 24 damage to an enemy; gain 24 Block; draw 3 cards; gain 2 Energy. " +
        "Exhaust.",
        Dispensation(2),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    // "Count the different stackable Statuses that have reached 0 stacks on any combatant this combat." The
    // engine keeps no such history, so the card counts what is NOT on the table instead — the same five
    // statuses, absent rather than spent. See ADAPTATIONS.
    private static readonly BnbCard LastOffice = new(
        "last_office", "Last Office", WorkingTag, 2,
        "For each of Paperwork, Doubt, Seal, Lien and Citation the chosen enemy does not carry, deal 8 damage " +
        "to it and gain 3 Block. Exhaust.",
        LastOfficeBody(8, 3),
        Rarity: "rare", Act: Act, Tags: [ExhaustTag]);

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<BnbCard> All() =>
    [
        FinalAttestation, FinalAttestation.Upgraded("Deal 11 damage. If the target is Ratified, gain 1 Energy.",
            Seq(Damage(11), If(HasStacks(Keywords.Ratified), Energy_(1)))),

        TempleTally, TempleTally.UpgradedRite(ActIVRites.TempleTally,
            "Whenever an enemy reaches a new multiple of 5 Paperwork for the first time this combat, apply 1 " +
            "Seal to it for each new multiple crossed.", cost: 0),

        ProcessionalCalendar, ProcessionalCalendar.UpgradedRite(ActIVRites.ProcessionalCalendar,
            "At the end of your turn, if you have at least 2 Queued cards, resolve your oldest Queued card.",
            cost: 0),

        HieraticMeasure, HieraticMeasure.UpgradedRite(ActIVRites.HieraticMeasure,
            "Whenever you Ratify an enemy, immediately trigger its current Paperwork once, then remove 3 " +
            "Paperwork from it.", cost: 1),

        CartoucheReckoning, CartoucheReckoning.Upgraded(
            "Deal 21 damage. Then, up to 3 times: if the target has at least 10 Paperwork, remove 10 " +
            "Paperwork and repeat this attack.", Reckoning(21)),

        MonumentalWrit, MonumentalWrit.Upgraded(
            "Queue: Deal 30 damage, plus 12 for each other card still in your Queue when this resolves. " +
            "Count at most 3.", Writ(30)),

        StoneLevy, StoneLevy.Upgraded(
            "Remove up to 25 of your Block. Deal 10 damage plus 2 damage for each Block removed.",
            Levy(25, 10)),

        FivefoldCompliance, FivefoldCompliance.Upgraded(
            "Deal 15 damage, then repeat once for each fulfilled clause.", Fivefold(15)),

        BlackTribunal, BlackTribunal.Upgraded(
            "Deal 18 damage, plus 8 damage for each different negative Status on the target. Count at most 5.",
            Tribunal(18)),

        SovereignProhibition, SovereignProhibition.Upgraded(
            "Gain 3 Censure. Apply 3 Censure to ALL enemies.", cost: 1),

        CandleCathedral, CandleCathedral.UpgradedRite(ActIVRites.CandleCathedral,
            "Whenever Ward Wax grants Block, gain additional Block equal to half your Ward Wax, rounded up. " +
            "Ward Wax no longer suffers its additional decay.", cost: 1),

        GrandCitation, GrandCitation.Upgraded(
            "Deal 18 damage to ALL enemies. Each enemy with Citation additionally loses HP equal to 3 times " +
            "its Citation, then loses 1 Citation.", GrandCitationBody(18)),

        CrownRepossession, CrownRepossession.Upgraded(
            "Deal 27 damage. Remove all remaining Block from the target; it loses HP equal to the Block " +
            "removed, maximum 50. Apply 6 Lien.", Repossess(27, 50)),

        AbsoluteInterdict, AbsoluteInterdict.UpgradedRite(ActIVRites.AbsoluteInterdict,
            "The first time each turn Censure on a combatant would prevent Status stacks, 1 Censure prevents " +
            "the entire application instead, however many stacks it carried.", cost: 1),

        TallowJudgment, TallowJudgment.Upgraded(
            "Consume up to 8 Ward Wax. Deal 14 damage plus 8 damage per Ward Wax consumed.", Judgment(8, 14, 8)),

        HemalAudit, HemalAudit.Upgraded(
            "Deal 22 damage. Then trigger Blood Ink repeatedly, up to 8 times or until no Blood Ink remains.",
            Audit(22, 8)),

        CompoundIndictment, CompoundIndictment.Upgraded(
            "Requires at least 3 different negative Statuses on the target. Add 3 stacks to each negative " +
            "Status it carries, up to 5 of them. Exhaust.", Compound(3)),

        GrandDispensation, GrandDispensation.Upgraded(
            "Choose 3 different options: deal 24 damage to an enemy; gain 24 Block; draw 3 cards; gain 2 " +
            "Energy. Exhaust.", Dispensation(3)),

        LastOffice, LastOffice.Upgraded(
            "For each of Paperwork, Doubt, Seal, Lien and Citation the chosen enemy does not carry, deal 10 " +
            "damage to it and gain 4 Block. Exhaust.", LastOfficeBody(10, 4)),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbCard Rite(
        string id, string name, string riteStatusId, int cost, string text, string rarity = "uncommon") =>
        new(id, name, RiteTag, cost, text, InstallRite(riteStatusId), Rarity: rarity, Act: Act);

    private static BnbCard UpgradedRite(this BnbCard card, string riteStatusId, string text, int? cost = null) =>
        card.Upgraded(text, InstallRite(riteStatusId + "+"), cost);

    // "Up to 3 times: if the target has at least 10 Paperwork, remove 10 and strike again." Written out
    // rather than looped, because the engine's repeat-until asks its question only AFTER running the body.
    private static CombatNodeModel Reckoning(int damage)
    {
        var again = Seq(Remove(Keywords.Paperwork, 10), Damage(damage));
        return Seq(Damage(damage),
            If(HasStacks(Keywords.Paperwork, 10),
                Seq(again, If(HasStacks(Keywords.Paperwork, 10),
                    Seq(again, If(HasStacks(Keywords.Paperwork, 10), again))))));
    }

    private static CombatNodeModel Writ(int damage) =>
        Damage(Plus(CombatAmountSpec.FromConst(damage),
            Times(AtMost(CardsInZone(CardZone.QueuePile), 3), 12)));

    // "Remove up to N of your Block. Deal M damage plus 2 per Block removed." What is there is read before it
    // is spent.
    private static CounterId Levied => new("stone_levy_taken");

    private static CombatNodeModel Levy(int cap, int damage) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You,
                AtMost(new CombatAmountSpec("defensivePool", SelectorKey: You,
                    ReadId: StandardCombatIds.BlockDefensivePool.value), cap),
                CounterId: Levied.value, Relative: false),
            new CombatNodeModel("modifyDefensivePool", You,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0), Taken(Levied)),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            Damage(Plus(CombatAmountSpec.FromConst(damage), Times(Taken(Levied), 2))));

    // Five clauses, each worth another swing. Counted first, because striking changes several of them.
    private static CounterId Clauses => new("fivefold_clauses");

    private static CombatNodeModel Fivefold(int damage)
    {
        CombatAmountSpec Clause(CombatAmountSpec value) => Once(value);

        var count = Plus(
            Plus(
                Clause(CombatAmountSpec.Binary("div", Stacks(Keywords.Paperwork), CombatAmountSpec.FromConst(10))),
                Clause(CombatAmountSpec.Binary("div", Stacks(Keywords.Doubt), CombatAmountSpec.FromConst(3)))),
            Plus(
                Plus(
                    Clause(Stacks(Keywords.Ratified)),
                    Clause(CombatAmountSpec.Binary("div",
                        JunkTypesInExhaust(), CombatAmountSpec.FromConst(2)))),
                Clause(CardsInZone(CardZone.QueuePile))));

        return Seq(
            new CombatNodeModel("setCombatantCounter", You, count, CounterId: Clauses.value, Relative: false),
            Damage(damage),
            CombatNodeModel.Repeat(
                new CombatAmountSpec("counter", SelectorKey: You, CounterId: Clauses.value), Damage(damage)));
    }

    private static CombatAmountSpec JunkTypesInExhaust()
    {
        CombatAmountSpec? total = null;
        foreach (var type in BureaucratStarter.JunkTypes)
        {
            var present = Once(new CombatAmountSpec("zoneCards", SelectorKey: You, ReadId: type,
                Zone: CardZone.ExhaustPile));
            total = total is null ? present : Plus(total, present);
        }
        return total!;
    }

    private static CombatNodeModel Tribunal(int damage) =>
        Damage(Plus(CombatAmountSpec.FromConst(damage),
            Times(AtMost(DistinctStatuses(NegativeStatuses), 5), 8)));

    // "Each enemy with Citation loses HP equal to 3× its Citation, then loses 1." Per enemy, so the rule
    // walks them.
    private static CombatNodeModel GrandCitationBody(int damage) =>
        Seq(
            Damage(damage, AllEnemies),
            new CombatNodeModel("forEachTarget",
                SelectorKey: "enemiesWithStatus", SelectorStatusId: Keywords.Citation,
                Children:
                [
                    Seq(
                        new CombatNodeModel("dealDamage", "iterationTarget",
                            Times(Stacks(Keywords.Citation, "iterationTarget"), 3),
                            IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
                        Remove(Keywords.Citation, 1, "iterationTarget")),
                ]));

    private static CounterId Repossessed => new("crown_repossession_taken");

    private static CombatNodeModel Repossess(int damage, int cap) =>
        Seq(
            Damage(damage),
            new CombatNodeModel("setCombatantCounter", Target,
                AtMost(new CombatAmountSpec("defensivePool", SelectorKey: Target,
                    ReadId: StandardCombatIds.BlockDefensivePool.value), cap),
                CounterId: Repossessed.value, Relative: false),
            new CombatNodeModel("modifyDefensivePool", Target,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0), TakenOn(Target, Repossessed)),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            new CombatNodeModel("dealDamage", Target, TakenOn(Target, Repossessed),
                IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
            Apply(Keywords.Lien, 6));

    private static CounterId WaxSpent => new("tallow_judgment_wax");

    private static CombatNodeModel Judgment(int cap, int damage, int per) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, AtMost(Stacks(Keywords.WardWax, You), cap),
                CounterId: WaxSpent.value, Relative: false),
            new CombatNodeModel("modifyStatusStacks", You,
                CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(0), Taken(WaxSpent)),
                StatusId: Keywords.WardWax),
            Damage(Plus(CombatAmountSpec.FromConst(damage), Times(Taken(WaxSpent), per))));

    // "Trigger Blood Ink repeatedly, up to N times or until none remains." Each trigger is the status' own
    // bite: HP equal to its stacks, then one stack gone.
    private static CombatNodeModel Audit(int damage, int times) =>
        Seq(Damage(damage), CombatNodeModel.Repeat(CombatAmountSpec.FromConst(times), BiteBloodInk()));

    private static CombatNodeModel BiteBloodInk() =>
        If(HasStacks(Keywords.BloodInk),
            Seq(
                new CombatNodeModel("dealDamage", Target, Stacks(Keywords.BloodInk),
                    IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
                Remove(Keywords.BloodInk, 1)));

    // "Add N stacks to each negative Status it carries, up to 5 of them" — each named status topped up if it
    // is there, gated on the target carrying at least three of them.
    private static CombatNodeModel Compound(int stacks) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", Target, DistinctStatuses(NegativeStatuses),
                CounterId: Compounded.value, Relative: false),
            If(new CombatConditionSpec("compare", Target, ValueKind: "counter",
                    Op: ComparisonOperator.GreaterOrEqual, Right: 3, Id: Compounded.value),
                Seq(NegativeStatuses.Take(5)
                    .Select(status => If(HasStacks(status), Apply(status, stacks)))
                    .ToArray())));

    private static CounterId Compounded => new("compound_indictment_kinds");

    private static CombatNodeModel Dispensation(int picks) =>
        CombatNodeModel.ChooseOptions(picks,
            ["deal 24 damage", "gain 24 Block", "draw 3 cards", "gain 2 Energy"],
            [Damage(24), Block(24), Draw(3), Energy_(2)],
            $"choose {picks}");

    private static CombatNodeModel LastOfficeBody(int damage, int block)
    {
        string[] counted =
            [Keywords.Paperwork, Keywords.Doubt, Keywords.Seal, Keywords.Lien, Keywords.Citation];

        return Seq(counted
            .Select(status => If(HasStacks(status), Seq(), Seq(Damage(damage), Block(block))))
            .ToArray());
    }

    private static CombatAmountSpec Taken(CounterId counter) =>
        new("counter", SelectorKey: You, CounterId: counter.value);

    private static CombatAmountSpec TakenOn(string who, CounterId counter) =>
        new("counter", SelectorKey: who, CounterId: counter.value);
}
