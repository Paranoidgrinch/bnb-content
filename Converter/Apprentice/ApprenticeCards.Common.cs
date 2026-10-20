using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Apprentice;

// The sixteen commons (master §7). Each must work in Act I with nothing behind it; the reason to take each is its
// own decision, not a bigger number.
public static partial class ApprenticeCards
{
    private const string Singe = ApprenticeKeywords.Singe;
    private const string Flow = ApprenticeKeywords.Flow;
    private const string Grit = ApprenticeKeywords.Grit;
    private const string Gust = ApprenticeKeywords.Gust;

    private static readonly BnbCard WaxDrop = Card(
        "ap_wax_drop", "Wax Drop", WorkingTag, 1,
        "Apply 2 Singe to ALL enemies.", Apply(Singe, 2, AllEnemies),
        "Heighten — Brand: the next damaging Speak applies 1 Singe to each enemy it hits (3 in all at most).",
        Brand(Singe, 1, 3), "common");

    private static readonly BnbCard Snuffer = Card(
        "ap_snuffer", "Snuffer", DeedTag, 1,
        "Spend up to 3 Singe on the target; deal 4 damage for each.", SpendSinge(3, 4),
        "Ground — Quench: spend up to 2 Singe on the target; this cast's Exhaustion is 2 lower for each.",
        Ground(WandRule.Quench, amount: 2, percent: 2, cap: 4), "common");

    private static readonly BnbCard InkDish = Card(
        "ap_ink_dish", "Ink Dish", WorkingTag, 0,
        "Look at the top 3 cards of your draw pile and put 1 into your hand. If it costs 2 or more, gain 1 Flow.",
        TakeFromTop(3, 1),
        "Speak — Reservoir: after the cast, gain 1 Flow.",
        Speak(Do(Apply(Flow, 1, You), afterCast: true)), "common");

    private static readonly BnbCard FoldedLeaf = Card(
        "ap_folded_leaf", "Folded Leaf", WorkingTag, 1,
        "Put a card from your discard pile on top of your draw pile. Gain 1 Gust.",
        Seq(DiscardToTop(), Apply(Gust, 1, You)),
        "Keep: the next Speak is stored instead of spoken, and goes first in your next successful cast.",
        Keep(100), "common");

    private static readonly BnbCard BlottingSand = Card(
        "ap_blotting_sand", "Blotting Sand", WorkingTag, 1,
        "Remove 1 stack of a negative status from yourself. Gain 5 Block.", Seq(Cleanse(1), Block(5)),
        "Shape — Remedy: the next Speak that would put a negative status on an enemy puts none; instead remove up " +
        "to 2 negative stacks from yourself and gain 3 Block for each.",
        Shape(WandRule.Remedy, amount: 2, cap: 3), "common");

    private static readonly BnbCard ChalkCircle = Card(
        "ap_chalk_circle", "Chalk Circle", WorkingTag, 1,
        "Gain 7 Block. If you do not cast this turn, gain 1 Grit at the end of it.",
        Seq(Block(7), Apply(ApprenticeRules.QuietCircle, 1, You)),
        "Ground: this cast's Exhaustion is 2 lower.",
        Ground(2), "common");

    private static readonly BnbCard ClayWeight = Card(
        "ap_clay_weight", "Clay Weight", WorkingTag, 1,
        "Spend up to 2 Focus: gain 5 Block for each. If you spend none, gain 4 Block.", FocusIntoBlock(2, 5, 4),
        "Ground: this cast's Exhaustion is 1 lower for each heavy instruction before this one (3 at most).",
        Ground(WandRule.PerHeavy, cap: 3), "common");

    private static readonly BnbCard MasonsDust = Card(
        "ap_masons_dust", "Mason's Dust", WorkingTag, 1,
        "Spend 1 Grit to gain 5 Ward Wax. If you cannot, gain 1 Grit instead.", GritIntoWax(5),
        "Speak: spend up to 2 Grit and gain 3 Ward Wax for each; with no Grit, gain 4 Block.",
        Speak(Guard(4).When(NoGrit), Do(SpendGritForWax(2, 3))), "common");

    private static readonly BnbCard OpenCasement = Card(
        "ap_open_casement", "Open Casement", WorkingTag, 1,
        "Two wand cards trade places for free, then draw 1 card. With fewer than two in the wand, gain 1 Gust instead.",
        FreeSwapOr(draw: 1),
        "Order: the next two instructions trade places before they run.",
        Order(WandRule.SwapNext), "common");

    private static readonly BnbCard WindblownLeaf = Card(
        "ap_windblown_leaf", "Windblown Leaf", WorkingTag, 1,
        "Put a card from your hand at the bottom of your draw pile, then draw 2 cards. Gain 1 Gust.",
        Seq(HandToBottom("ap_windblown_leaf"), Draw(2), Apply(Gust, 1, You)),
        "Shape — Scatter: the next single-target damaging Speak hits ALL enemies at 60%.",
        Shape(WandRule.Scatter, percent: 60), "common");

    private static readonly BnbCard PennyWhistle = Card(
        "ap_penny_whistle", "Penny Whistle", DeedTag, 1,
        "Deal 5 damage. If you have played another card this turn, gain 1 more Focus.",
        Seq(Damage(5), IfAtLeast(PlayedThisTurn, 2, WandMove(WandAction.Focus, 1))),
        "Stop — Cut: the cast ends after the Speak before this one; if a Speak resolved, its Exhaustion is 2 lower.",
        Stop(2), "common");

    private static readonly BnbCard WickTrimmer = Card(
        "ap_wick_trimmer", "Wick Trimmer", WorkingTag, 1,
        "Choose: Exhaust another card from your hand and draw 2 cards; or gain 5 Block.",
        TrimOrGuard("ap_wick_trimmer", 2, 5),
        "Burn: gain 7 Block; this cast's Exhaustion is 2 lower; this card is exhausted after the cast.",
        Burn(2, Guard(7)), "common");

    private static readonly BnbCard SpareNotch = Card(
        "ap_spare_notch", "Spare Notch", WorkingTag, 1,
        "The wand has one more slot this combat (6 at most). At 6, gain 8 Block instead. Exhaust.",
        NotchOr(8),
        "Ground: this cast's Exhaustion is 1 lower, or 3 lower when 5 or more instructions run.",
        Ground(WandRule.ByLength, amount: 1, amount2: 3, cap: 5), "common", [ExhaustTag]);

    private static readonly BnbCard Pinprick = Card(
        "ap_pinprick", "Pinprick", DeedTag, 1,
        "Deal 5 damage. If the target intends to attack, gain 5 Block.",
        Seq(Damage(5), If(Intends, Block(5))),
        "Clause: if the target intends to attack, the next damaging Speak hits once more for 3.",
        Clause(WandRule.BonusHit, amount: 3), "common");

    private static readonly BnbCard CupOfWater = Card(
        "ap_cup_of_water", "Cup of Water", WorkingTag, 1,
        "Spend up to 2 Flow: draw 1 card and gain 3 Block for each. With no Flow, gain 2 Flow instead.",
        FlowIntoDraw(2, 3),
        "Shape — Soak: the next damage-only Speak gives Block equal to 80% of its damage instead of dealing it.",
        Shape(WandRule.Soak, percent: 80), "common");

    private static readonly BnbCard DustMote = Card(
        "ap_dust_mote", "Dust Mote", WorkingTag, 1,
        "Gain 5 Block. If you inscribed a card this turn, draw 1 card.",
        Seq(Block(5), IfAtLeast(WandValue(WandField.InscribedThisTurn), 1, Draw(1))),
        "Speak: deal 3 damage, +3 for each card inscribed since your last cast (+6 at most).",
        Speak(Hit(DustMoteDamage(3))), "common");

    public static IReadOnlyList<BnbCard> Commons() =>
    [
        WaxDrop, WaxDrop.Up(
            "Apply 3 Singe to ALL enemies.", Apply(Singe, 3, AllEnemies),
            "Heighten — Brand: the next damaging Speak applies 1 Singe to each enemy it hits (4 in all at most).",
            Brand(Singe, 1, 4)),
        Snuffer, Snuffer.Up(
            "Spend up to 3 Singe on the target; deal 5 damage for each.", SpendSinge(3, 5),
            "Ground — Quench: spend up to 3 Singe on the target; this cast's Exhaustion is 2 lower for each (4 at most).",
            Ground(WandRule.Quench, amount: 3, percent: 2, cap: 4)),
        InkDish, InkDish.Up(
            "Look at the top 4 cards of your draw pile and put 1 into your hand. If it costs 2 or more, gain 1 Flow.",
            TakeFromTop(4, 1),
            "Speak — Reservoir: after the cast, gain 2 Flow.",
            Speak(Do(Apply(Flow, 2, You), afterCast: true))),
        FoldedLeaf, FoldedLeaf.Up(
            "Put a card from your discard pile on top of your draw pile, then draw 1 card. Gain 1 Gust.",
            Seq(DiscardToTop(), Draw(1), Apply(Gust, 1, You)),
            "Keep: the next Speak is stored at 125% instead of spoken, and goes first in your next successful cast.",
            Keep(125)),
        BlottingSand, BlottingSand.Up(
            "Remove 2 stacks of negative statuses from yourself. Gain 5 Block.", Seq(Cleanse(2), Block(5)),
            "Shape — Remedy: the next Speak that would put a negative status on an enemy puts none; instead remove up " +
            "to 3 negative stacks from yourself and gain 3 Block for each.",
            Shape(WandRule.Remedy, amount: 3, cap: 3)),
        ChalkCircle, ChalkCircle.Up(
            "Gain 10 Block. If you do not cast this turn, gain 1 Grit at the end of it.",
            Seq(Block(10), Apply(ApprenticeRules.QuietCircle, 1, You)),
            "Ground: this cast's Exhaustion is 3 lower.",
            Ground(3)),
        ClayWeight, ClayWeight.Up(
            "Spend up to 2 Focus: gain 7 Block for each. If you spend none, gain 4 Block.", FocusIntoBlock(2, 7, 4),
            "Ground: this cast's Exhaustion is 1 lower for each heavy instruction before this one (4 at most).",
            Ground(WandRule.PerHeavy, cap: 4)),
        MasonsDust, MasonsDust.Up(
            "Spend 1 Grit to gain 7 Ward Wax. If you cannot, gain 1 Grit instead.", GritIntoWax(7),
            "Speak: spend up to 2 Grit and gain 4 Ward Wax for each; with no Grit, gain 4 Block.",
            Speak(Guard(4).When(NoGrit), Do(SpendGritForWax(2, 4)))),
        OpenCasement, OpenCasement.Up(
            "Two wand cards trade places for free, then draw 2 cards. With fewer than two in the wand, gain 1 Gust instead.",
            FreeSwapOr(draw: 2),
            "Order: the next two instructions trade places before they run; if that changes which Speak a Heighten " +
            "reaches, this cast's Exhaustion is 1 lower.",
            Order(WandRule.SwapNext, amount: 1)),
        WindblownLeaf, WindblownLeaf.Up(
            "Put a card from your hand at the bottom of your draw pile, then draw 3 cards. Gain 1 Gust.",
            Seq(HandToBottom("ap_windblown_leaf"), Draw(3), Apply(Gust, 1, You)),
            "Shape — Scatter: the next single-target damaging Speak hits ALL enemies at 75%.",
            Shape(WandRule.Scatter, percent: 75)),
        PennyWhistle, PennyWhistle.Up(
            "Deal 8 damage. If you have played another card this turn, gain 1 more Focus.",
            Seq(Damage(8), IfAtLeast(PlayedThisTurn, 2, WandMove(WandAction.Focus, 1))),
            "Stop — Cut: the cast ends after the Speak before this one; if a Speak resolved, its Exhaustion is 3 lower.",
            Stop(3)),
        WickTrimmer, WickTrimmer.Up(
            "Choose: Exhaust another card from your hand and draw 3 cards; or gain 5 Block.",
            TrimOrGuard("ap_wick_trimmer", 3, 5),
            "Burn: gain 10 Block; this cast's Exhaustion is 2 lower; this card is exhausted after the cast.",
            Burn(2, Guard(10))),
        SpareNotch, SpareNotch.Up(
            "The wand has one more slot this combat (6 at most). At 6, gain 8 Block instead. Exhaust.",
            NotchOr(8),
            "Ground: this cast's Exhaustion is 2 lower, or 4 lower when 5 or more instructions run.",
            Ground(WandRule.ByLength, amount: 2, amount2: 4, cap: 5), cost: 0),
        Pinprick, Pinprick.Up(
            "Deal 7 damage. If the target intends to attack, gain 5 Block.",
            Seq(Damage(7), If(Intends, Block(5))),
            "Clause: if the target intends to attack, the next damaging Speak hits once more for 5.",
            Clause(WandRule.BonusHit, amount: 5)),
        CupOfWater, CupOfWater.Up(
            "Spend up to 2 Flow: draw 1 card and gain 4 Block for each. With no Flow, gain 2 Flow instead.",
            FlowIntoDraw(2, 4),
            "Shape — Soak: the next damage-only Speak gives Block equal to its damage instead of dealing it.",
            Shape(WandRule.Soak, percent: 100)),
        DustMote, DustMote.Up(
            "Gain 7 Block. If you inscribed a card this turn, draw 1 card.",
            Seq(Block(7), IfAtLeast(WandValue(WandField.InscribedThisTurn), 1, Draw(1))),
            "Speak: deal 5 damage, +3 for each card inscribed since your last cast (+6 at most).",
            Speak(Hit(DustMoteDamage(5)))),
    ];

    // ── the shapes the commons share ───────────────────────────────────────────────────────────────────────

    private static readonly CounterId Scratch = new("apprentice_scratch");

    private static CombatAmountSpec ScratchValue => CombatAmountSpec.Counter(You, Scratch.value);

    private static CombatNodeModel SetScratch(CombatAmountSpec amount) =>
        new("setCombatantCounter", You, amount, CounterId: Scratch.value, Relative: false);

    private static CombatConditionSpec ScratchAtLeast(int value) =>
        new("compare", You, ValueKind: "counter", Op: ComparisonOperator.GreaterOrEqual, Right: value, Id: Scratch.value);

    // A wand move from a card's play text.
    public static CombatNodeModel WandMove(WandAction move, int? amount = null, bool free = false) =>
        new("wand", You, amount is { } n ? CombatAmountSpec.FromConst(n) : null, WandMove: move.ToString(), Free: free);

    public static CombatAmountSpec WandValue(WandField field) => new("wandValue", ReadId: field.ToString());

    // "If <value> is at least N": a condition compares a value read off a combatant, so the value goes through the
    // scratch counter first.
    internal static CombatNodeModel IfAtLeast(CombatAmountSpec value, int atLeast, CombatNodeModel then, CombatNodeModel? otherwise = null) =>
        Seq(SetScratch(value), If(ScratchAtLeast(atLeast), then, otherwise));

    // "Spend up to N Singe on the target; deal D for each" — the stacks are counted and taken BEFORE the blow, or the
    // blow would spend one of them itself.
    private static CombatNodeModel SpendSinge(int most, int perStack) =>
        Seq(
            SetScratch(AtMost(Stacks(Singe), most)),
            new CombatNodeModel("modifyStatusStacks", Target, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Singe),
            If(ScratchAtLeast(1), Damage(Times(ScratchValue, perStack))));

    // "Look at the top N of your draw pile and put M into your hand. If it costs 2 or more, gain 1 Flow."
    private static readonly string Picked = "ink_dish_pick";

    private static CombatNodeModel TakeFromTop(int top, int take) =>
        Repeat(take, Seq(
            new CombatNodeModel("markCardInstance", You,
                Card: new CombatCardSpec("chosen", CardZone.DrawPile, Purpose: "choose a card to take", Top: top), ToTag: Picked),
            CombatNodeModel.ForEachCard(You, CardZone.DrawPile, Seq(
                new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"), ToZone: CardZone.Hand),
                SetScratch(new CombatAmountSpec("cardCost", ReadId: Energy.value, ReadCard: new CombatCardSpec("iterated"))),
                If(ScratchAtLeast(2), Apply(Flow, 1, You)),
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"), ToTag: Picked, Relative: true)),
                mark: Picked)));

    private static CombatNodeModel DiscardToTop() =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a card to put on top of your draw pile"),
            ToZone: CardZone.DrawPile, Placement: ZonePlacement.Top);

    private static CombatNodeModel HandToBottom(string self) =>
        new("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card for the bottom of your draw pile", ExcludeTag: Self(self)),
            ToZone: CardZone.DrawPile, Placement: ZonePlacement.Bottom);

    private static CombatNodeModel Cleanse(int stacks) =>
        new("modifySelectedStatusStacks", You, CombatAmountSpec.FromConst(-stacks),
            Selection: new StatusSelectionSpec(StatusPolarityFilter.Debuff));

    // "Spend up to N Focus: gain B Block for each. If you spend none, gain F."
    private static CombatNodeModel FocusIntoBlock(int most, int perFocus, int none) =>
        Seq(
            WandMove(WandAction.SpendFocus, most),
            SetScratch(WandValue(WandField.FocusSpent)),
            If(ScratchAtLeast(1), Block(Times(ScratchValue, perFocus)), Block(none)));

    private static CombatConditionSpec HasGrit => HasStacks(Grit, 1, You);
    private static ICombatExpression<CardPlayContext, bool> NoGrit =>
        new NotExpression<CardPlayContext>(Condition(HasGrit));

    private static CombatNodeModel GritIntoWax(int wax) =>
        If(HasGrit,
            Seq(Remove(Grit, 1, You), Apply(Cards.Keywords.WardWax, wax, You)),
            Apply(Grit, 1, You));

    private static CombatNodeModel SpendGritForWax(int most, int perGrit) =>
        Seq(
            SetScratch(AtMost(Stacks(Grit, You), most)),
            new CombatNodeModel("modifyStatusStacks", You, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Grit),
            If(ScratchAtLeast(1), Apply(Cards.Keywords.WardWax, Times(ScratchValue, perGrit), You)));

    private static CombatNodeModel FreeSwapOr(int draw) =>
        IfAtLeast(WandValue(WandField.Occupied), 2,
            Seq(WandMove(WandAction.Swap, free: true), Draw(draw)),
            Apply(Gust, 1, You));

    // "If you have played another card this turn": this one is already counted when its program runs.
    private static CombatAmountSpec PlayedThisTurn => new("cardsPlayedThisTurn", SelectorKey: You);

    private static CombatNodeModel TrimOrGuard(string self, int draw, int block) =>
        CombatNodeModel.ChooseOptions(1,
            [$"Exhaust another card and draw {draw}", $"Gain {block} Block"],
            [
                Seq(new CombatNodeModel("moveCardToZone", You,
                        Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to exhaust", ExcludeTag: Self(self)),
                        ToZone: CardZone.ExhaustPile),
                    Draw(draw)),
                Block(block),
            ],
            "Wick Trimmer");

    private static CombatNodeModel NotchOr(int block) =>
        Seq(SetScratch(WandValue(WandField.Capacity)),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.GreaterOrEqual,
                    Right: RogueDeck.Core.Combat.WandState.HardSlotCap, Id: Scratch.value),
                Block(block),
                WandMove(WandAction.Extend, 1)));

    private static CombatConditionSpec Intends => new("intends", Target, Id: "Attack");

    private static CombatNodeModel FlowIntoDraw(int most, int blockPer) =>
        Seq(
            SetScratch(AtMost(Stacks(Flow, You), most)),
            If(ScratchAtLeast(1),
                Seq(
                    new CombatNodeModel("modifyStatusStacks", You, CombatAmountSpec.Unary("neg", ScratchValue), StatusId: Flow),
                    new CombatNodeModel("drawCards", You, ScratchValue),
                    Block(Times(ScratchValue, blockPer))),
                Apply(Flow, 2, You)));

    private static ICombatExpression<CardPlayContext, int> DustMoteDamage(int baseDamage) =>
        Amount(Plus(CombatAmountSpec.FromConst(baseDamage), Times(AtMost(WandValue(WandField.InscribedSinceCast), 2), 3)));
}
