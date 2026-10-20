using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Apprentice;

// The eight rares (master §9): each changes the program's grammar, brings back what burned, or makes the cast's
// own shape its payoff — and each has a play worth taking without a wand built around it.
public static partial class ApprenticeCards
{
    private static readonly BnbCard MastersCorrection = Card(
        "ap_masters_correction", "Master's Correction", WorkingTag, 1,
        "Choose: another card in your hand costs 1 less the next time you play it; or inscribe a card from your hand " +
        "for free. Then draw 1 card.",
        Correction("ap_masters_correction", 1),
        "Order — Errata: the next Speak may speak as another installed card's Speak instead (you choose when casting).",
        Order(WandRule.Errata), "rare");

    private static readonly BnbCard FinalExamination = Card(
        "ap_final_examination", "Final Examination", DeedTag, 2,
        "Spend any amount of your Focus: deal 6 damage, +4 for each, and gain 4 Block, +3 for each.", Examination(4),
        "Order — Oral Exam: when casting, choose whether this section reads forward or backward; backward adds 1 Exhaustion.",
        Order(WandRule.OralExam, amount: 1), "rare");

    private static readonly BnbCard LessonsInAsh = Card(
        "ap_lessons_ash", "Lessons in Ash", WorkingTag, 1,
        "Exhaust up to 2 other cards from your hand: draw 1 card and gain 1 Focus for each.", AshLessons("ap_lessons_ash", 0),
        "Keep — Ash Trace: your next Burn Out this combat also deals 8 damage.",
        AshTrace(8), "rare");

    private static readonly BnbCard GlassRod = Card(
        "ap_glass_rod", "Glass Rod", DeedTag, 2,
        "Your next cast this combat deals and gives 50% more, and adds 3 Exhaustion. Draw 1 card. Exhaust.",
        Seq(Apply(ApprenticeRules.GlassRodPrimed, 1, You), Draw(1)),
        "Shape — Prism: the next damaging or defensive Speak resolves as 70% of itself and 70% of the same number in " +
        "the other form; then this card is exhausted. With nothing to shape, gain 8 Block and it still burns.",
        Shape(WandRule.Prism, percent: 70, burns: true, fallback: Guard(8)), "rare", [ExhaustTag]);

    private static readonly BnbCard SecondHand = Card(
        "ap_second_hand", "Second Hand", WorkingTag, 2,
        "Put a Temporary copy of a card from your Exhaust pile into your hand. Exhaust.",
        TemporaryCopy(CardZone.ExhaustPile),
        "Burn — Recall: while installed, the first Burn Out each combat may put an exhausted card into the slot it freed.",
        Recall(1), "rare", [ExhaustTag]);

    private static readonly BnbCard FourfoldLesson = Card(
        "ap_fourfold_lesson", "The Fourfold Lesson", WorkingTag, 2,
        "Deal 7 damage and apply 1 Singe. Gain 1 Flow, 1 Grit and 1 Gust.", Fourfold(7, 1),
        "Clause — Fourfold: at the end of the section, if 2 of Singe applied, Flow, Grit and Gust gained have happened " +
        "this turn, gain 5 Block; with 3, also Exhaustion 2 lower; with all 4, also gain 1 Focus after the cast.",
        Clause(WandRule.Fourfold, amount: 5, amount2: 2, cap: 1), "rare");

    private static readonly BnbCard PerfectCopy = Card(
        "ap_perfect_copy", "A Perfect Copy", WorkingTag, 1,
        "Put the wand's cards in any order you like, for free, then draw 2 cards.",
        Seq(WandMove(WandAction.Rearrange), Draw(2)),
        "Order — Palimpsest: after a full-wand cast, the installed cards move one occupied place to the right.",
        Order(WandRule.Rotate), "rare");

    private static readonly BnbCard LastWord = Card(
        "ap_last_word", "The Master's Last Word", DeedTag, 2,
        "Spend any amount of your Focus: deal 10 damage, +5 for each. With 4 or more spent, gain 3 Grit. Exhaust.",
        LastWordPlay(5),
        "Stop — Final Word: must run last. For each different kind of instruction before it (5 at most), a hit of 6; " +
        "every hit after the first costs 1 Focus left over from the cast. Adds 2 Exhaustion.",
        FinalWord(6, 5, 2), "rare", [ExhaustTag]);

    public static IReadOnlyList<BnbCard> Rares() =>
    [
        MastersCorrection, MastersCorrection.Up(
            "Choose: another card in your hand costs 2 less the next time you play it; or inscribe a card from your " +
            "hand for free. Then draw 1 card.",
            Correction("ap_masters_correction", 2),
            "Order — Errata: the next Speak may speak as another installed card's Speak instead, 25% stronger (you " +
            "choose when casting).",
            Order(WandRule.Errata, percent: 25)),
        FinalExamination, FinalExamination.Up(
            "Spend any amount of your Focus: deal 6 damage, +6 for each, and gain 4 Block, +3 for each.", Examination(6),
            "Order — Oral Exam: when casting, choose whether this section reads forward or backward, at no cost.",
            Order(WandRule.OralExam, amount: 0)),
        LessonsInAsh, LessonsInAsh.Up(
            "Exhaust up to 2 other cards from your hand: draw 1 card and gain 1 Focus for each, and 1 more card if you " +
            "exhausted any.",
            AshLessons("ap_lessons_ash", 1),
            "Keep — Ash Trace: your next Burn Out this combat also deals 12 damage.",
            AshTrace(12)),
        GlassRod, GlassRod.Up(
            "Your next cast this combat deals and gives 65% more, and adds 3 Exhaustion. Draw 1 card. Exhaust.",
            Seq(Apply(ApprenticeRules.GlassRodPrimedPlus, 1, You), Draw(1)),
            "Shape — Prism: the next damaging or defensive Speak resolves as 85% of itself and 85% of the same number in " +
            "the other form; then this card is exhausted. With nothing to shape, gain 8 Block and it still burns.",
            Shape(WandRule.Prism, percent: 85, burns: true, fallback: Guard(8))),
        SecondHand, SecondHand.Up(
            "Put a Temporary copy of a card from your Exhaust pile into your hand. Exhaust.",
            TemporaryCopy(CardZone.ExhaustPile),
            "Burn — Recall: while installed, the first two Burn Outs each combat may each put an exhausted card into " +
            "the slot it freed.",
            Recall(2), cost: 1),
        FourfoldLesson, FourfoldLesson.Up(
            "Deal 10 damage and apply 1 Singe. Gain 1 Flow, 1 Grit and 2 Gust.", Fourfold(10, 2),
            "Clause — Fourfold: at the end of the section, if 2 of Singe applied, Flow, Grit and Gust gained have " +
            "happened this turn, gain 8 Block; with 3, also Exhaustion 2 lower; with all 4, also gain 1 Focus after the cast.",
            Clause(WandRule.Fourfold, amount: 8, amount2: 2, cap: 1)),
        PerfectCopy, PerfectCopy.Up(
            "Put the wand's cards in any order you like, for free, then draw 3 cards.",
            Seq(WandMove(WandAction.Rearrange), Draw(3)),
            "Order — Palimpsest: after a full-wand cast, the installed cards move one occupied place to the right or " +
            "the left, as you choose.",
            Order(WandRule.Rotate, amount: 1)),
        LastWord, LastWord.Up(
            "Spend any amount of your Focus: deal 10 damage, +7 for each. With 4 or more spent, gain 3 Grit. Exhaust.",
            LastWordPlay(7),
            "Stop — Final Word: must run last. For each different kind of instruction before it (5 at most), a hit of " +
            "8; every hit after the first costs 1 Focus left over from the cast. Adds 2 Exhaustion.",
            FinalWord(8, 5, 2)),
    ];

    // ── the shapes the rares share ─────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel Correction(string self, int discount) =>
        Seq(
            CombatNodeModel.ChooseOptions(1,
                [$"Another card in hand costs {discount} less next time", "Inscribe a card for free"],
                [
                    new CombatNodeModel("setCardInstanceMarkCounter", You, CombatAmountSpec.FromConst(-discount),
                        Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to make cheaper", ExcludeTag: Self(self)),
                        CounterId: StandardCombatIds.CardCostDeltaCounter.value, Relative: false),
                    WandMove(WandAction.Inscribe, free: true),
                ],
                "Master's Correction"),
            Draw(1));

    private static CombatNodeModel Examination(int damagePer) =>
        Seq(
            WandMove(WandAction.SpendFocus, ApprenticeWand.MaxFocus),
            SetScratch(WandValue(WandField.FocusSpent)),
            Damage(Plus(CombatAmountSpec.FromConst(6), Times(ScratchValue, damagePer))),
            Block(Plus(CombatAmountSpec.FromConst(4), Times(ScratchValue, 3))));

    private static CombatNodeModel AshLessons(string self, int extraDraw)
    {
        CombatNodeModel Burn() => Seq(
            new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to exhaust", ExcludeTag: Self(self)),
                ToZone: CardZone.ExhaustPile),
            Draw(1), WandMove(WandAction.Focus, 1));
        var bonus = extraDraw > 0 ? Draw(extraDraw) : null;
        return CombatNodeModel.ChooseOptions(1,
            ["Exhaust none", "Exhaust 1", "Exhaust 2"],
            [
                Block(0),
                bonus is null ? Burn() : Seq(Burn(), bonus),
                bonus is null ? Seq(Burn(), Burn()) : Seq(Burn(), Burn(), bonus),
            ],
            "Lessons in Ash");
    }

    private static CombatNodeModel Fourfold(int damage, int gust) =>
        Seq(Damage(damage), Apply(Singe, 1), Apply(Flow, 1, You), Apply(Grit, 1, You), Apply(Gust, gust, You));

    private static CombatNodeModel LastWordPlay(int damagePer) =>
        Seq(
            WandMove(WandAction.SpendFocus, ApprenticeWand.MaxFocus),
            SetScratch(WandValue(WandField.FocusSpent)),
            Damage(Plus(CombatAmountSpec.FromConst(10), Times(ScratchValue, damagePer))),
            If(ScratchAtLeast(4), Apply(Grit, 3, You)));
}
