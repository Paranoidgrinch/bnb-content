using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;
using K = BnbContent.Converter.Cards.Keywords;

namespace BnbContent.Converter.Apprentice;

// WHAT EACH GENERAL CARD DOES IN THE WAND (master §11). The fifty general cards keep their play text, their upgrade
// and everything the Bureaucrat and the Witch do with them; for the Apprentice each gets a small, hand-written wand
// part — never its full card text replayed, never a Rite installed. Its upgrade carries the same proxy (§11.2).
// "Copy" in the master is each atom's Copyable: what an Echo, a Keep or a Shape may take. Written onto the card at
// assembly; it means something only to a wand.
public static class ApprenticeProxies
{
    public static readonly IReadOnlyDictionary<string, WandImprint> General = new Dictionary<string, WandImprint>(StringComparer.Ordinal)
    {
        // ADAPTATION: "if Censure prevented a status this turn" — no record of that is kept, so the proxy asks
        // whether you carry Censure at all.
        ["absolute_interdict"] = Speak(Guard(2).When(Lacks(K.Censure)).Private())
            .GroundingWhen(2, Condition(HasStacks(K.Censure, 1, You))),
        ["black_tribunal"] = Speak(Hit(Amount(Plus(Const(7), Times(AtMost(DistinctStatuses(NegativeStatuses), 5), 2))))),
        ["blacklisted"] = Speak(Put(K.Censure, Amount(Plus(Const(2), Once(DistinctStatuses(PositiveStatuses)))))),
        ["blood_marginalia"] = Speak(Put(K.Citation, 2), Put(K.BloodInk, 1)),
        ["blood_redaction"] = Shape(WandRule.Convert, amount: 2, cap: 2, status: K.BloodInk),
        ["blood_tithe"] = Speak(Hit(Amount(Plus(Const(6), Times(Once(Stacks(K.BloodInk)), 3))))),
        ["borrowed_candle"] = Speak(Do(Draw(1), afterCast: true)),
        ["candle_cathedral"] = Speak(Wax(3)),
        ["compound_indictment"] = Speak(Do(Compound())),
        // ADAPTATION: "if an enemy lost HP to a status earlier in this cast" — no record of that is kept, so it asks
        // whether an enemy carries a status that costs it HP (Paperwork or Blood Ink).
        ["consecrated_testament"] = Speak(
            Wax(3).When(AnyEnemyBleeds).Private(),
            Guard(2).When(new NotExpression<CardPlayContext>(AnyEnemyBleeds)).Private()),
        ["contempt_finding"] = Speak(Do(CashCitation(2, blockEach: 3, hpEachToAll: 0))),
        ["countermanded_grace"] = Speak(Gain(K.Censure, 2).Private()),
        ["crossed_sigil"] = Speak(Do(Cleanse(1)), Put(K.Censure, 1).Private()),
        ["crown_repossession"] = Speak(Hit(10).Private(), Do(TakeBlock(8)), Put(K.Lien, 2).Private()),
        ["dawn_summons"] = Speak(Hit(8).Plus(4, WandPosition.FirstSpeak)),
        ["debt_ouroboros"] = Speak(Put(K.Lien, 3)),
        ["exemplary_sentence"] = Speak(Do(CashCitation(2, blockEach: 0, hpEachToAll: 3))),
        ["false_signature"] = Heighten(25, stacks: 0, filter: WandFilter.DamageOrDefense, strain: 1),
        ["foreclosure"] = Speak(Hit(5).Private(), Do(Cards.GeneralActI.ResolveLienNow(2))),
        ["forfeit_seal"] = Speak(Hit(5), Put(K.Lien, 2).When(Condition(TargetGuarded)).Private()),
        ["grand_citation"] = Speak(Hit(7, to: WandTargeting.AllEnemies), Do(CitedTakeMore(2))),
        ["grand_dispensation"] = Speak(Do(CombatNodeModel.ChooseOptions(1,
            ["Deal 10 damage", "Gain 10 Block"], [Damage(10), Block(10)], "Grand Dispensation"))),
        ["grave_lien"] = Speak(Hit(6), Put(K.Lien, 4)),
        ["hemal_audit"] = Speak(Hit(8).Private(), Do(CombatNodeModel.Repeat(CombatAmountSpec.FromConst(2), BiteBloodInk()))),
        ["last_office"] = Speak(Hit(Amount(Times(AtMost(Plus(Const(5), CombatAmountSpec.Unary("neg",
            DistinctStatuses([K.Paperwork, K.Doubt, K.Seal, K.Lien, K.Citation]))), 3), 4)))),
        ["malediction_review"] = Speak(Guard(5).Private(), Do(CombatNodeModel.ChooseOptions(1,
            ["Gain 1 Censure", "Apply 1 Censure to the target"],
            [Apply(K.Censure, 1, You), Apply(K.Censure, 1)], "Malediction Review"))),
        ["moonlit_counterfeit"] = Echo(50, WandFilter.DamageOrDefense),
        // ADAPTATION: the extra Lien for Block gained before its next turn is left to the card's play.
        ["mortgage_sigil"] = Speak(Put(K.Lien, 2).Private()),
        ["mortgaged_aegis"] = Speak(Guard(9).Private(), Gain(K.Lien, 2).Private()),
        ["notary_beetle"] = Heighten(0, stacks: 1, filter: WandFilter.NegativeStatus),
        ["oath_of_refusal"] = Speak(Gain(K.Censure, 2).Private(), Guard(3).Private()),
        ["proxy_curse"] = Speak(Do(ProxyCurse())),
        ["reciprocal_edict"] = Speak(Put(K.Censure, 2), Guard(2)),
        ["sanctioned_charm"] = Speak(Guard(5), Gain(K.Censure, 1).Private()),
        ["sanguine_errata"] = Speak(Put(K.BloodInk, 3).Private(), Do(RemoveOneOtherNegative())),
        // The card's own Ward Wax promise, one Wax rather than three.
        ["sealed_mantle"] = Speak(Guard(5).Private(), Do(Apply(Cards.GeneralRites.SealedMantle, 1, You))),
        ["seizure_writ"] = Speak(Hit(7).Private(), Do(SeizeBlock(8, perLien: 4, lienCap: 2))),
        ["silent_hearing"] = Speak(Put(K.Citation, 2), Guard(4).When(Condition(TargetIntendsAttack))),
        ["sovereign_prohibition"] = Speak(Gain(K.Censure, 2).Private(), Put(K.Censure, 1, WandTargeting.AllEnemies).Private()),
        ["standing_citation"] = Speak(Put(K.Citation, 3)),
        ["tallow_judgment"] = Speak(Do(Judgment(3, 5, 3))),
        ["tallow_reserve"] = Speak(Do(ReserveTallow(4, 2))),
        ["usurers_moon"] = Speak(Put(K.Lien, 2), Put(K.Citation, 1)),
        // ADAPTATION: "2 if another status lost a stack on it earlier this cast" — no record of that is kept: 1.
        ["vein_register"] = Speak(Put(K.BloodInk, 1).Private()),
        ["vital_census"] = Speak(Hit(6, to: WandTargeting.AllEnemies).Private(), Do(InkedLoseHp(2))),
        ["votive_covenant"] = Speak(Wax(4)),
        ["wax_indemnity"] = Speak(Wax(3).Private(), Do(new CombatNodeModel("heal", You, CombatAmountSpec.FromConst(1)))),
        // ADAPTATION: its decay protection is left to the card's play.
        ["wax_reliquary"] = Speak(Wax(3).Private()),
        ["waxen_surety"] = Speak(Wax(4)),
        ["witchmark_citation"] = Speak(Put(K.Citation, 3),
            Do(Draw(1), afterCast: true).When(new NotExpression<CardPlayContext>(Condition(TargetIntendsAttack)))),
    };

    // Each general card and its upgrade gets its proxy (master §11.2: the upgrade's wand part is the same).
    public static IReadOnlyList<CardData> Mark(IReadOnlyList<CardData> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        return [.. cards.Select(card =>
            card.Wand is null && General.TryGetValue(card.Id.TrimEnd('+'), out var proxy) ? card with { Wand = proxy } : card)];
    }

    // ── shorthand ──────────────────────────────────────────────────────────────────────────────────────────

    private static CombatAmountSpec Const(int value) => CombatAmountSpec.FromConst(value);

    private static ICombatExpression<CardPlayContext, bool> Lacks(string status) =>
        new NotExpression<CardPlayContext>(Condition(HasStacks(status, 1, You)));

    private static readonly CounterId Scratch = new("apprentice_proxy_scratch");
    private static CombatAmountSpec ScratchValue(string on = Target) => CombatAmountSpec.Counter(on, Scratch.value);
    private static CombatNodeModel SetScratch(CombatAmountSpec amount, string on = Target) =>
        new("setCombatantCounter", on, amount, CounterId: Scratch.value, Relative: false);
    private static CombatConditionSpec ScratchAtLeast(int value, string on = Target) =>
        new("compare", on, ValueKind: "counter", Op: ComparisonOperator.GreaterOrEqual, Right: value, Id: Scratch.value);

    private static CombatConditionSpec TargetIntendsAttack => new("intends", Target, Id: nameof(IntentKind.Attack));

    private static CombatConditionSpec TargetGuarded => new("compare", Target, ValueKind: "defensivePool",
        Op: ComparisonOperator.GreaterOrEqual, Right: 1, Id: StandardCombatIds.BlockDefensivePool.value);

    // "An enemy carries a status that costs it HP" — Paperwork or Blood Ink on any of them.
    private static ICombatExpression<CardPlayContext, bool> AnyEnemyBleeds =>
        new ComparisonExpression<CardPlayContext>(
            Amount(Plus(
                new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec("enemiesWithStatus", K.Paperwork)),
                new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec("enemiesWithStatus", K.BloodInk)))),
            ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0));

    private static CombatNodeModel Cleanse(int stacks) =>
        new("modifySelectedStatusStacks", You, CombatAmountSpec.FromConst(-stacks),
            Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff));

    // "Add a stack to the negative statuses it carries — when it carries 3 kinds or more." ADAPTATION: the master
    // caps it at 3 of them; a program cannot pick "the first three present", so each one it carries gets its stack.
    private static CombatNodeModel Compound() =>
        Seq(SetScratch(DistinctStatuses(NegativeStatuses)),
            If(ScratchAtLeast(3), Seq([.. NegativeStatuses.Select(status => If(HasStacks(status), Apply(status, 1)))])));

    // "Remove up to N Citation: gain B Block per stack; ALL enemies lose H HP per stack."
    private static CombatNodeModel CashCitation(int most, int blockEach, int hpEachToAll) =>
        Seq(
            SetScratch(AtMost(Stacks(K.Citation), most)),
            new CombatNodeModel("modifyStatusStacks", Target, CombatAmountSpec.Unary("neg", ScratchValue()), StatusId: K.Citation),
            blockEach > 0 ? Block(Times(ScratchValue(), blockEach)) : Seq(),
            hpEachToAll > 0
                ? new CombatNodeModel("dealDamage", AllEnemies, Times(ScratchValue(), hpEachToAll),
                    IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime)
                : Seq());

    // "Remove up to N of its Block."
    private static CombatNodeModel TakeBlock(int most) =>
        new("modifyDefensivePool", Target,
            CombatAmountSpec.Unary("neg", AtMost(new CombatAmountSpec("defensivePool", SelectorKey: Target,
                ReadId: StandardCombatIds.BlockDefensivePool.value), most)),
            PoolId: StandardCombatIds.BlockDefensivePool.value);

    // "Remove up to N of its Block; 1 Lien for every P removed, at most C."
    private static CombatNodeModel SeizeBlock(int most, int perLien, int lienCap) =>
        Seq(
            SetScratch(AtMost(new CombatAmountSpec("defensivePool", SelectorKey: Target,
                ReadId: StandardCombatIds.BlockDefensivePool.value), most)),
            new CombatNodeModel("modifyDefensivePool", Target, CombatAmountSpec.Unary("neg", ScratchValue()),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            Apply(K.Lien, AtMost(CombatAmountSpec.Binary("div", ScratchValue(), Const(perLien)), lienCap)));

    // Every enemy carrying Citation takes N more.
    private static CombatNodeModel CitedTakeMore(int more) =>
        CombatNodeModel.ForEach(AllEnemies,
            If(HasStacks(K.Citation, 1, "iterationTarget"), Damage(more, "iterationTarget")));

    private static CombatNodeModel BiteBloodInk() =>
        If(HasStacks(K.BloodInk),
            Seq(
                new CombatNodeModel("dealDamage", Target, Stacks(K.BloodInk), IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime),
                Remove(K.BloodInk, 1)));

    // "Remove 1 negative stack from yourself; if one was removed, apply 2 Blood Ink to the target."
    private static CombatNodeModel ProxyCurse() =>
        Seq(
            SetScratch(new CombatAmountSpec("stacksByPolarity", SelectorKey: You, Polarity: StatusPolarity.Debuff), You),
            If(ScratchAtLeast(1, You), Seq(Cleanse(1), Apply(K.BloodInk, 2))));

    // "Remove up to 1 other negative status stack from the target" — any of its debuffs but the Blood Ink just put on it.
    private static CombatNodeModel RemoveOneOtherNegative() =>
        NegativeStatuses.Where(s => s != K.BloodInk).Reverse()
            .Aggregate(Seq(), (otherwise, status) => If(HasStacks(status), Remove(status, 1), otherwise));

    private static CombatNodeModel Judgment(int most, int damage, int per) =>
        Seq(
            SetScratch(AtMost(Stacks(K.WardWax, You), most), You),
            new CombatNodeModel("modifyStatusStacks", You, CombatAmountSpec.Unary("neg", ScratchValue(You)), StatusId: K.WardWax),
            Damage(Plus(Const(damage), Times(ScratchValue(You), per))));

    // "Lose up to N Block; if all N were lost, gain W Ward Wax."
    private static CombatNodeModel ReserveTallow(int price, int wax) =>
        Seq(
            SetScratch(new CombatAmountSpec("defensivePool", SelectorKey: You, ReadId: StandardCombatIds.BlockDefensivePool.value), You),
            new CombatNodeModel("modifyDefensivePool", You, CombatAmountSpec.Unary("neg", AtMost(ScratchValue(You), price)),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            If(ScratchAtLeast(price, You), Apply(K.WardWax, wax, You)));

    private static CombatNodeModel InkedLoseHp(int hp) =>
        CombatNodeModel.ForEach(AllEnemies,
            If(HasStacks(K.BloodInk, 1, "iterationTarget"),
                new CombatNodeModel("dealDamage", "iterationTarget", Const(hp), IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime)));
}
