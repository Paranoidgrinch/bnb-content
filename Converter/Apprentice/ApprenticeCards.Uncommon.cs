using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Apprentice;

// The fourteen uncommons (master §8): each a new relationship between the wand's instructions, not a larger number.
public static partial class ApprenticeCards
{
    // ADAPTATION (plan A7): the master's play text splits the largest hit of the NEXT damage card; the engine has no
    // hook between a card's hits, so the card is its own split: two hits that each meet a Singe of their own.
    private static readonly BnbCard SplitWick = Card(
        "ap_split_wick", "Split Wick", DeedTag, 1,
        "Deal 4 damage twice.", Repeat(2, Damage(4)),
        "Shape — Fork: the next damaging Speak becomes two hits of 60% each, each its own hit for Singe.",
        Shape(WandRule.Fork, percent: 60, amount: 2), "uncommon");

    private static readonly BnbCard CrackedSlate = Card(
        "ap_cracked_slate", "Cracked Slate", DeedTag, 1,
        "Deal 9 damage. You may spend 1 Grit to deal 7 more.", SlateBlow(9, 7),
        "Shape — Join: the next two damaging Speaks become one hit of both together, +3.",
        Shape(WandRule.Join, amount: 3), "uncommon");

    private static readonly BnbCard Crosswind = Card(
        "ap_crosswind", "Crosswind", WorkingTag, 1,
        "Move up to 3 Singe from the target to another enemy. With no other enemy, gain 2 Gust instead.", MoveSinge(3),
        "Shape — Crossfire: the next multi-hit damaging Speak alternates its hits between the target and another enemy.",
        Shape(WandRule.Crossfire), "uncommon");

    private static readonly BnbCard WrongWayRound = Card(
        "ap_wrong_way_round", "The Wrong Way Round", WorkingTag, 1,
        "Draw 2 cards, then two wand cards trade places for free; if they did, gain 1 Gust.", WrongWay(2),
        "Order: the rest of the cast runs backwards, once; with 2 or more left to turn, its Exhaustion is 1 lower.",
        Order(WandRule.Reverse, amount: 1), "uncommon");

    // ADAPTATION: "a non-Temporary Working other than Rubbing" — a choice can leave out one tag, so it offers every
    // card in the discard pile that is not itself a Temporary copy.
    private static readonly BnbCard Rubbing = Card(
        "ap_rubbing", "Rubbing", WorkingTag, 1,
        "Choose a card in your discard pile. Put a Temporary copy of it into your hand; it Exhausts when played.",
        TemporaryCopy(CardZone.DiscardPile),
        "Keep: the last Speak is stored at 50% and strikes at your next Burn Out. If a spell is already kept, gain 4 Block.",
        KeepLast(50, WandStoreTrigger.NextBurnOut, Guard(4)), "uncommon");

    private static readonly BnbCard Watermark = Card(
        "ap_watermark", "Watermark", WorkingTag, 1,
        "Choose: gain 2 Flow; or spend 1 Flow to lower the wand's Exhaustion by 3 and gain 1 Gust.", WatermarkChoice(2),
        "Heighten — Contrast: if the Speak before was of another kind (damage, defence, status) than the next, the " +
        "next gets 50% more; otherwise gain 1 Flow after the cast.",
        Contrast(50, 1), "uncommon");

    private static readonly BnbCard TornFlyleaf = Card(
        "ap_torn_flyleaf", "Torn Flyleaf", WorkingTag, 1,
        "Take a wand card back into your hand for free, then draw 1 card.",
        Seq(WandMove(WandAction.WithdrawToHand), Draw(1)),
        "Order — Slip: after the cast, this card moves into the nearest free slot to its right; if it moved, gain 1 Focus.",
        Order(WandRule.Slip), "uncommon");

    private static readonly BnbCard Footnote = Card(
        "ap_footnote", "Footnote", WorkingTag, 0,
        "Choose: draw 1 card; or lower the wand's Exhaustion by 2.", FootnoteChoice(1),
        "Clause — Final Breath: if paying for the cast left you no Focus, the next single-hit damaging Speak becomes " +
        "two hits of 60%.",
        Clause(WandRule.FinalBreath, percent: 60, amount: 2), "uncommon");

    private static readonly BnbCard CandleAtBothEnds = Card(
        "ap_candle_both_ends", "Candle at Both Ends", DeedTag, 1,
        "Exhaust another card from your hand: draw 3 cards and gain 1 Focus. With no other card, deal 9 damage instead.",
        BothEnds("ap_candle_both_ends", 3, 9),
        "Burn: repeat the last damaging Speak at 150%, then this card is exhausted. With none before it, deal 9 damage.",
        Echo(150, WandFilter.Damage, burns: true, Hit(9)), "uncommon");

    private static readonly BnbCard Firebreak = Card(
        "ap_firebreak", "Firebreak", WorkingTag, 1,
        "Remove up to 3 Singe from the target and gain 1 Grit for each. If none was removed, gain 6 Block.",
        FirebreakPlay(3, 0),
        "Ground: if a direct hit spent Singe earlier in this cast, its Exhaustion is 3 lower and you gain 1 Grit; " +
        "otherwise 1 lower.",
        Ground(WandRule.Firebreak, amount: 3, amount2: 1), "uncommon");

    private static readonly BnbCard KilnShelf = Card(
        "ap_kiln_shelf", "Kiln Shelf", WorkingTag, 2,
        "Raise the wand's Exhaustion cap by 2 this combat (12 at most). At 12, gain 12 Block instead. Exhaust.",
        RaiseCapOr(2, 12),
        "Ground — Cap: the heavy part of this cast's Exhaustion is at most 2; if it would have been more, the cast " +
        "costs 1 more Focus.",
        Ground(WandRule.HeavyCap, amount: 1, cap: 2), "uncommon", [ExhaustTag]);

    private static readonly BnbCard BorrowedStone = Card(
        "ap_borrowed_stone", "Borrowed Stone", WorkingTag, 1,
        "Spend 1 Grit to gain 1 Energy and 2 Focus. With no Grit, gain 8 Block.", BorrowStone(8),
        "Speak: gain 4 Block, +2 for each wand card exhausted this combat (+8 at most); with two or more, this " +
        "cast's Exhaustion is 1 lower.",
        StoneSpeak(4), "uncommon");

    private static readonly BnbCard ColdAsh = Card(
        "ap_cold_ash", "Cold Ash", WorkingTag, 1,
        "Spend up to 3 Singe on the target: its next attack deals 2 less for each.", ChillSinge(3, 2),
        "Speak: gain 1 Grit for every 2 Singe direct wand hits spent earlier in this cast (2 at most); if none was " +
        "spent, gain 5 Block.",
        AshSpeak(roundUp: false), "uncommon");

    private static readonly BnbCard SealPress = Card(
        "ap_seal_press", "Book Press", WorkingTag, 1,
        "Spend up to 2 Grit: gain 4 Ward Wax for each, and 4 Block. If you spend none, gain 7 Block.", PressGrit(2, 4),
        "Keep — Shield Seal: store the next defensive Speak at 75%. It rises before the first enemy attack this " +
        "enemy turn and fades at your next turn.",
        Keep(75, WandFilter.Defense, WandStoreTrigger.NextEnemyAttack), "uncommon");

    public static IReadOnlyList<BnbCard> Uncommons() =>
    [
        SplitWick, SplitWick.Up(
            "Deal 5 damage twice.", Repeat(2, Damage(5)),
            "Shape — Fork: the next damaging Speak becomes two hits of 70% each, each its own hit for Singe.",
            Shape(WandRule.Fork, percent: 70, amount: 2)),
        CrackedSlate, CrackedSlate.Up(
            "Deal 12 damage. You may spend 1 Grit to deal 9 more.", SlateBlow(12, 9),
            "Shape — Join: the next two damaging Speaks become one hit of both together, +5.",
            Shape(WandRule.Join, amount: 5)),
        Crosswind, Crosswind.Up(
            "Move up to 5 Singe from the target to another enemy. With no other enemy, gain 2 Gust instead.", MoveSinge(5),
            "Shape — Crossfire: the next multi-hit damaging Speak alternates its hits between the target and another " +
            "enemy; each it hits gains 1 Singe.",
            Shape(WandRule.Crossfire, amount: 1, status: ApprenticeKeywords.Singe)),
        WrongWayRound, WrongWayRound.Up(
            "Draw 3 cards, then two wand cards trade places for free; if they did, gain 1 Gust.", WrongWay(3),
            "Order: the rest of the cast runs backwards, once; with 2 or more left to turn, its Exhaustion is 2 lower.",
            Order(WandRule.Reverse, amount: 2)),
        // ADAPTATION: the master's upgrade lowers the copy's cost; a created copy carries no price mark, so the
        // upgrade draws a card instead.
        Rubbing, Rubbing.Up(
            "Choose a card in your discard pile. Put a Temporary copy of it into your hand; it Exhausts when played. " +
            "Draw 1 card.",
            Seq(TemporaryCopy(CardZone.DiscardPile), Draw(1)),
            "Keep: the last Speak is stored at 70% and strikes at your next Burn Out. If a spell is already kept, gain 4 Block.",
            KeepLast(70, WandStoreTrigger.NextBurnOut, Guard(4))),
        Watermark, Watermark.Up(
            "Choose: gain 3 Flow; or spend 1 Flow to lower the wand's Exhaustion by 3 and gain 1 Gust.", WatermarkChoice(3),
            "Heighten — Contrast: if the Speak before was of another kind (damage, defence, status) than the next, the " +
            "next gets 75% more; otherwise gain 1 Flow after the cast.",
            Contrast(75, 1)),
        TornFlyleaf, TornFlyleaf.Up(
            "Take a wand card back into your hand for free, then draw 2 cards.",
            Seq(WandMove(WandAction.WithdrawToHand), Draw(2)),
            "Order — Slip: after the cast, this card moves into the nearest free slot on either side; if it moved, " +
            "gain 1 Focus.",
            Order(WandRule.Slip, amount: 1)),
        Footnote, Footnote.Up(
            "Choose: draw 2 cards; or lower the wand's Exhaustion by 2.", FootnoteChoice(2),
            "Clause — Final Breath: if paying for the cast left you no Focus, the next single-hit damaging Speak " +
            "becomes two hits of 70%.",
            Clause(WandRule.FinalBreath, percent: 70, amount: 2)),
        CandleAtBothEnds, CandleAtBothEnds.Up(
            "Exhaust another card from your hand: draw 4 cards and gain 1 Focus. With no other card, deal 9 damage instead.",
            BothEnds("ap_candle_both_ends", 4, 9),
            "Burn: repeat the last damaging Speak at 175%, then this card is exhausted. With none before it, deal 12 damage.",
            Echo(175, WandFilter.Damage, burns: true, Hit(12))),
        Firebreak, Firebreak.Up(
            "Remove up to 3 Singe from the target and gain 1 Grit for each, then gain 3 Block. If none was removed, " +
            "gain 6 Block.",
            FirebreakPlay(3, 3),
            "Ground: if a direct hit spent Singe earlier in this cast, its Exhaustion is 4 lower and you gain 1 Grit; " +
            "otherwise 1 lower.",
            Ground(WandRule.Firebreak, amount: 4, amount2: 1)),
        KilnShelf, KilnShelf.Up(
            "Raise the wand's Exhaustion cap by 2 this combat (12 at most). At 12, gain 12 Block instead. Exhaust.",
            RaiseCapOr(2, 12),
            "Ground — Cap: the heavy part of this cast's Exhaustion is at most 1; if it would have been more, the cast " +
            "costs 1 more Focus.",
            Ground(WandRule.HeavyCap, amount: 1, cap: 1), cost: 1),
        BorrowedStone, BorrowedStone.Up(
            "Spend 1 Grit to gain 1 Energy and 2 Focus. With no Grit, gain 10 Block.", BorrowStone(10),
            "Speak: gain 6 Block, +2 for each wand card exhausted this combat (+8 at most); with two or more, this " +
            "cast's Exhaustion is 1 lower.",
            StoneSpeak(6)),
        ColdAsh, ColdAsh.Up(
            "Spend up to 3 Singe on the target: its next attack deals 3 less for each.", ChillSinge(3, 3),
            "Speak: gain 1 Grit for every 2 Singe direct wand hits spent earlier in this cast, rounded up (2 at most); " +
            "if none was spent, gain 5 Block.",
            AshSpeak(roundUp: true)),
        SealPress, SealPress.Up(
            "Spend up to 2 Grit: gain 5 Ward Wax for each, and 4 Block. If you spend none, gain 7 Block.", PressGrit(2, 5),
            "Keep — Shield Seal: store the next defensive Speak whole. It rises before the first enemy attack this " +
            "enemy turn and fades at your next turn.",
            Keep(100, WandFilter.Defense, WandStoreTrigger.NextEnemyAttack)),
    ];

    // ── the shapes the uncommons share ─────────────────────────────────────────────────────────────────────

    private static CombatNodeModel SlateBlow(int damage, int more) =>
        If(HasGrit,
            CombatNodeModel.ChooseOptions(1,
                [$"Spend 1 Grit: deal {damage + more}", $"Keep the Grit: deal {damage}"],
                [Seq(Remove(Grit, 1, You), Damage(damage + more)), Damage(damage)],
                "Cracked Slate"),
            Damage(damage));

    // The Singe comes off the target first, then goes to one other enemy — or, alone, becomes Gust.
    private static CombatNodeModel MoveSinge(int most) =>
        Seq(
            SetScratch(new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec("otherEnemies"))),
            If(ScratchAtLeast(1),
                Seq(
                    SetScratch(AtMost(Stacks(Singe), most)),
                    new CombatNodeModel("modifyStatusStacks", Target, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Singe),
                    CombatNodeModel.RandomTargets("otherEnemies", CombatAmountSpec.FromConst(1),
                        new CombatNodeModel("applyStatus", "iterationTarget", ScratchValue, StatusId: Singe))),
                Apply(Gust, 2, You)));

    private static CombatNodeModel WrongWay(int draw) =>
        Seq(Draw(draw), IfAtLeast(WandValue(WandField.Occupied), 2,
            Seq(WandMove(WandAction.Swap, free: true), Apply(Gust, 1, You))));

    private static CombatNodeModel TemporaryCopy(CardZone zone) =>
        new("createCardCopy", You, CombatAmountSpec.FromConst(1),
            Card: new CombatCardSpec("chosen", zone, Purpose: "choose a card to copy", ExcludeTag: TemporaryTag),
            ToZone: CardZone.Hand, Temporary: true);

    private static CombatNodeModel WatermarkChoice(int flow) =>
        If(HasStacks(Flow, 1, You),
            CombatNodeModel.ChooseOptions(1,
                [$"Gain {flow} Flow", "Spend 1 Flow: Exhaustion −3, gain 1 Gust"],
                [Apply(Flow, flow, You),
                 Seq(Remove(Flow, 1, You), WandMove(WandAction.Exhaustion, -3), Apply(Gust, 1, You))],
                "Watermark"),
            Apply(Flow, flow, You));

    private static CombatNodeModel FootnoteChoice(int draw) =>
        CombatNodeModel.ChooseOptions(1,
            [$"Draw {draw}", "Exhaustion −2"],
            [Draw(draw), WandMove(WandAction.Exhaustion, -2)],
            "Footnote");

    private static CombatNodeModel BothEnds(string self, int draw, int otherwise) =>
        IfAtLeast(CardsInZone(CardZone.Hand), 2,
            Seq(new CombatNodeModel("moveCardToZone", You,
                    Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to exhaust", ExcludeTag: Self(self)),
                    ToZone: CardZone.ExhaustPile),
                Draw(draw), WandMove(WandAction.Focus, 1)),
            Damage(otherwise));

    private static CombatNodeModel FirebreakPlay(int most, int blockAfter) =>
        Seq(
            SetScratch(AtMost(Stacks(Singe), most)),
            If(ScratchAtLeast(1),
                blockAfter > 0
                    ? Seq(
                        new CombatNodeModel("modifyStatusStacks", Target, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Singe),
                        Apply(Grit, ScratchValue, You),
                        Block(blockAfter))
                    : Seq(
                        new CombatNodeModel("modifyStatusStacks", Target, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Singe),
                        Apply(Grit, ScratchValue, You)),
                Block(6)));

    private static CombatNodeModel RaiseCapOr(int by, int block) =>
        IfAtLeast(WandValue(WandField.ExhaustionCap), 12, Block(block), WandMove(WandAction.RaiseCap, by));

    private static CombatNodeModel BorrowStone(int block) =>
        If(HasGrit,
            Seq(Remove(Grit, 1, You), Energy_(1), WandMove(WandAction.Focus, 2)),
            Block(block));

    private static WandImprint StoneSpeak(int baseBlock) =>
        Speak(Guard(Amount(Plus(CombatAmountSpec.FromConst(baseBlock), Times(AtMost(WandValue(WandField.WandCardsExhausted), 4), 2)))))
            .GroundingWhen(1, new ComparisonExpression<CardPlayContext>(Wand(WandField.WandCardsExhausted),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardPlayContext>(2)));

    public const string Chilled = "cold_ash_chilled";

    private static CombatNodeModel ChillSinge(int most, int perStack) =>
        Seq(
            SetScratch(AtMost(Stacks(Singe), most)),
            If(ScratchAtLeast(1),
                Seq(
                    new CombatNodeModel("modifyStatusStacks", Target, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Singe),
                    Apply(Chilled, Times(ScratchValue, perStack)))));

    // Grit for the Singe the cast's own hits spent so far; without any, Block.
    private static WandImprint AshSpeak(bool roundUp)
    {
        var spent = WandValue(WandField.SingeSpentThisCast);
        var grit = AtMost(CombatAmountSpec.Binary("div", roundUp ? Plus(spent, CombatAmountSpec.FromConst(1)) : spent,
            CombatAmountSpec.FromConst(2)), 2);
        var none = new ComparisonExpression<CardPlayContext>(Wand(WandField.SingeSpentThisCast),
            ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(0));
        return Speak(Put(Grit, Amount(grit), WandTargeting.Self).Private(), Guard(5).When(none));
    }

    private static CombatNodeModel PressGrit(int most, int waxPer) =>
        If(HasGrit,
            CombatNodeModel.ChooseOptions(1,
                [.. Enumerable.Range(0, most + 1).Select(n => n == 0 ? "Spend no Grit: 7 Block" : $"Spend {n} Grit")],
                [Block(7), .. Enumerable.Range(1, most).Select(n => Seq(
                    SetScratch(AtMost(Stacks(Grit, You), n)),
                    new CombatNodeModel("modifyStatusStacks", You, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Grit),
                    Apply(Cards.Keywords.WardWax, Times(ScratchValue, waxPer), You),
                    Block(4)))],
                "Book Press"),
            Block(7));
}
