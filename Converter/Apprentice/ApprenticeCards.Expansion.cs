using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Apprentice;

// The expansion's 42 cards (sorcerers_apprentice_expansion.md §3): ten more for Act I, and his first cards for Acts
// II–IV. What is new is growth over a fight — Rote and Poise, the casts of the combat, "whenever you cast" — kept in
// the wand's grammar: nothing here ticks, brews or queues. Numbers are the expansion's BALANCE DRAFT.
public static partial class ApprenticeCards
{
    private const string Rote = ApprenticeKeywords.Rote;
    private const string Poise = ApprenticeKeywords.Poise;

    // ── what these cards read ─────────────────────────────────────────────────────────────────────────────

    private static CombatAmountSpec Const(int value) => CombatAmountSpec.FromConst(value);
    private static CombatAmountSpec Minus(CombatAmountSpec a, int b) => CombatAmountSpec.Binary("sub", a, Const(b));
    private static CombatAmountSpec AtLeast(CombatAmountSpec a, int floor) => CombatAmountSpec.Binary("max", a, Const(floor));

    // "N more if you have already cast this turn": during a cast its own cast is already counted, so the wand asks
    // for a second one and the hand for a first.
    private static CombatAmountSpec IfCastBefore(int bonus, bool inWand) =>
        Times(AtMost(inWand ? Minus(WandValue(WandField.CastsThisTurn), 1) : WandValue(WandField.CastsThisTurn), 1), bonus);

    // 1 when no slot is free.
    private static CombatAmountSpec WandFull => CombatAmountSpec.Binary("sub", Const(1), Once(WandValue(WandField.FreeSlots)));

    private static WandAtom WaxOf(CombatAmountSpec amount) =>
        new() { Kind = WandAtomKind.WardWax, Amount = Amount(amount) };

    private static WandAtom PutAll(string status, int stacks) => Put(status, stacks, WandTargeting.AllEnemies);

    private static WandAtom Later(CombatNodeModel program) => Do(program, afterCast: true);

    private static CombatNodeModel GainFocus(int amount) => WandMove(WandAction.Focus, amount);

    private static WandImprint Lesson(params WandAtom[] atoms) => Burn(0, atoms);

    // ── Act I: seven uncommons, three rares ──────────────────────────────────────────────────────────────────

    private static readonly BnbCard Recitation = Card(
        "ap_recitation", "Recitation", WorkingTag, 1,
        "Gain 2 Rote. Draw 1 card. Exhaust.", Seq(Apply(Rote, 2, You), Draw(1)),
        "Speak: after the cast, gain 1 Rote.",
        Speak(Later(Apply(Rote, 1, You))), "uncommon", [ExhaustTag]);

    private static readonly BnbCard SteadyHand = Card(
        "ap_steady_hand", "Steady Hand", WorkingTag, 1,
        "Gain 2 Poise and 5 Block. Exhaust.", Seq(Apply(Poise, 2, You), Block(5)),
        "Speak: gain 4 Block; after the cast, gain 1 Poise.",
        Speak(Guard(4), Later(Apply(Poise, 1, You))), "uncommon", [ExhaustTag]);

    private static readonly BnbCard Kindling = Card(
        "ap_kindling", "Kindling", DeedTag, 1,
        "Deal 4 damage twice; each hit then applies 1 Singe.", Repeat(2, Seq(Damage(4), Apply(Singe, 1))),
        "Speak: deal 3 damage twice.",
        Speak(Hit(3, 2)), "uncommon");

    private static readonly BnbCard EmberTongs = Card(
        "ap_ember_tongs", "Ember Tongs", DeedTag, 1,
        "Deal 4 damage, 3 more for each Singe on the target (at most 15 more).",
        Damage(Plus(Const(4), AtMost(Times(Stacks(Singe), 3), 15))),
        "Speak: deal 2 damage, 2 more for each Singe on the target (at most 10 more).",
        Speak(Hit(Amount(Plus(Const(2), AtMost(Times(Stacks(Singe), 2), 10))))), "uncommon");

    private static readonly BnbCard TracingPaper = Card(
        "ap_tracing_paper", "Tracing Paper", WorkingTag, 1,
        "Gain 6 Block. Draw 1 card.", Seq(Block(6), Draw(1)),
        "Echo: repeat the last damaging Speak at full strength; then this card burns.",
        Echo(100, WandFilter.Damage, burns: true), "uncommon");

    private static readonly BnbCard QuillKnife = Card(
        "ap_quill_knife", "Quill Knife", DeedTag, 0,
        "Deal 4 damage, 4 more if you inscribed a card this turn.",
        Damage(Plus(Const(4), Times(Once(WandValue(WandField.InscribedThisTurn)), 4))),
        "Speak: deal 3 damage, 3 more for each empty slot (at most 9 more).",
        Speak(Hit(Amount(Plus(Const(3), AtMost(Times(WandValue(WandField.FreeSlots), 3), 9))))), "uncommon");

    private static readonly BnbCard Inkhorn = Card(
        "ap_inkhorn", "Inkhorn", WorkingTag, 1,
        "Gain 2 Focus. Draw 1 card.", Seq(GainFocus(2), Draw(1)),
        "Speak: after the cast, gain 1 Focus.",
        Speak(Later(GainFocus(1))), "uncommon");

    private static readonly BnbCard Copybook = Card(
        "ap_copybook", "Copybook", WorkingTag, 2,
        "At the start of each of your turns, gain 1 Rote and 1 Poise. Exhaust.", Apply(ApprenticeRules.CopybookRule, 1, You),
        "Burn: after the cast, gain 2 Rote; then this card is exhausted.",
        Lesson(Later(Apply(Rote, 2, You))), "rare", [ExhaustTag]);

    // The wand's heat as a payoff: what Exhaustion has built up is spent at once, and the wand is cool for the next
    // cast. Its Speak strains the wand in turn.
    private static readonly BnbCard BankedFire = Card(
        "ap_banked_fire", "Banked Fire", DeedTag, 1,
        "Deal 5 damage to ALL enemies, 3 more for each Exhaustion the wand has. Then the wand's Exhaustion drops to 0.",
        Seq(Damage(Plus(Const(5), Times(WandValue(WandField.Exhaustion), 3)), AllEnemies), WandMove(WandAction.Exhaustion, -12)),
        "Heighten: the next damaging Speak deals 150% more; 2 more Exhaustion.",
        Heighten(150, filter: WandFilter.Damage, strain: 2), "rare");

    private static readonly BnbCard TuningFork = Card(
        "ap_tuning_fork", "Tuning Fork", WorkingTag, 1,
        "Gain 2 Focus. Draw 2 cards.", Seq(GainFocus(2), Draw(2)),
        "Echo: repeat the last Speak at 150%.",
        Echo(150), "rare");

    // ── Act II: two commons, nine uncommons, five rares ──────────────────────────────────────────────────────

    private static readonly BnbCard SealingWax = Card(
        "ap_sealing_wax", "Sealing Wax", WorkingTag, 1,
        "Gain 6 Block and 3 Ward Wax.", Seq(Block(6), Apply(Cards.Keywords.WardWax, 3, You)),
        "Speak: gain 3 Ward Wax, 1 more for each Grit you have (at most 3 more).",
        Speak(WaxOf(Plus(Const(3), AtMost(Stacks(Grit, You), 3)))), "common", act: 2);

    private static readonly BnbCard DrillBook = Card(
        "ap_drill_book", "Drill Book", DeedTag, 1,
        "Deal 6 damage, 2 more for each cast this combat (at most 10 more).",
        Damage(Plus(Const(6), AtMost(Times(WandValue(WandField.CastsThisCombat), 2), 10))),
        "Speak: deal 3 damage, 1 more for each cast this combat (at most 8 more).",
        Speak(Hit(Amount(Plus(Const(3), AtMost(WandValue(WandField.CastsThisCombat), 8))))), "common", act: 2);

    private static readonly BnbCard RedPencil = Card(
        "ap_red_pencil", "Red Pencil", DeedTag, 1,
        "Deal 9 damage.", Damage(9),
        "Heighten: the next damaging Speak deals 100% more; 1 more Exhaustion.",
        Heighten(100, filter: WandFilter.Damage, strain: 1), "uncommon", act: 2);

    private static readonly BnbCard Bookend = Card(
        "ap_bookend", "Bookend", WorkingTag, 1,
        "Gain 8 Block, 4 more if the wand is full.", Block(Plus(Const(8), Times(WandFull, 4))),
        "Speak: gain 4 Block, 2 more for each card in the wand.",
        Speak(Guard(Amount(Plus(Const(4), Times(WandValue(WandField.Occupied), 2))))), "uncommon", act: 2);

    private static readonly BnbCard PressedFlower = Card(
        "ap_pressed_flower", "Pressed Flower", WorkingTag, 1,
        "Gain 2 Flow. Draw 1 card.", Seq(Apply(Flow, 2, You), Draw(1)),
        "Keep: the next defensive Speak is stored at 125% and goes first in your next cast.",
        Keep(125, WandFilter.Defense), "uncommon", act: 2);

    private static readonly BnbCard SootMark = Card(
        "ap_soot_mark", "Soot Mark", DeedTag, 1,
        "Apply 4 Singe.", Apply(Singe, 4),
        "Speak: apply 2 Singe to ALL enemies.",
        Speak(PutAll(Singe, 2)), "uncommon", act: 2);

    private static readonly BnbCard StoneTablet = Card(
        "ap_stone_tablet", "Stone Tablet", WorkingTag, 1,
        "Gain 2 Grit and 5 Block.", Seq(Apply(Grit, 2, You), Block(5)),
        "Speak: gain 2 Grit.",
        Speak(Gain(Grit, 2)), "uncommon", act: 2);

    private static readonly BnbCard Weathervane = Card(
        "ap_weathervane", "Weathervane", WorkingTag, 1,
        "Gain 2 Gust. Draw 1 card.", Seq(Apply(Gust, 2, You), Draw(1)),
        "Order: the next two instructions trade places before they run; if that changes which Speak a Heighten " +
        "reaches, 2 less Exhaustion.",
        Order(WandRule.SwapNext, amount: 2), "uncommon", act: 2);

    private static readonly BnbCard Lectern = Card(
        "ap_lectern", "Lectern", WorkingTag, 2,
        "Gain 3 Rote. Exhaust.", Apply(Rote, 3, You),
        "Burn: after the cast, gain 2 Rote; then this card is exhausted.",
        Lesson(Later(Apply(Rote, 2, You))), "uncommon", [ExhaustTag], act: 2);

    private static readonly BnbCard LeadType = Card(
        "ap_lead_type", "Lead Type", WorkingTag, 1,
        "Gain 7 Block. Draw 1 card.", Seq(Block(7), Draw(1)),
        "Shape — Join: the next two damaging Speaks become one hit of both together, +6.",
        Shape(WandRule.Join, amount: 6), "uncommon", act: 2);

    private static readonly BnbCard ScorchMark = Card(
        "ap_scorch_mark", "Scorch Mark", DeedTag, 2,
        "Deal 8 damage to ALL enemies.", Damage(8, AllEnemies),
        "Speak: deal 4 damage to ALL enemies.",
        Speak(Hit(4, to: WandTargeting.AllEnemies)), "uncommon", act: 2);

    private static readonly BnbCard BorrowedGrimoire = Card(
        "ap_borrowed_grimoire", "The Borrowed Grimoire", WorkingTag, 2,
        "Whenever you cast this combat, gain 1 Rote. Exhaust.", Apply(ApprenticeRules.GrimoireRule, 1, You),
        "Burn: after the cast, gain 3 Rote; then this card is exhausted.",
        Lesson(Later(Apply(Rote, 3, You))), "rare", [ExhaustTag], act: 2);

    private static readonly BnbCard BellBook = Card(
        "ap_bell_book", "Bell, Book and Candle", DeedTag, 2,
        "Deal 6 damage 3 times; each hit then applies 1 Singe.", Repeat(3, Seq(Damage(6), Apply(Singe, 1))),
        "Speak: deal 4 damage 3 times.",
        Speak(Hit(4, 3)), "rare", act: 2);

    private static readonly BnbCard LongRecitation = Card(
        "ap_long_recitation", "The Long Recitation", WorkingTag, 2,
        "Deal 4 damage and gain 3 Block for each card in the wand.",
        Seq(Damage(Times(WandValue(WandField.Occupied), 4)), Block(Times(WandValue(WandField.Occupied), 3))),
        "Speak: deal 2 damage for each cast this combat (at most 20).",
        Speak(Hit(Amount(AtMost(Times(WandValue(WandField.CastsThisCombat), 2), 20)))), "rare", act: 2);

    private static readonly BnbCard WardingPrimer = Card(
        "ap_warding_primer", "Warding Primer", WorkingTag, 2,
        "Gain 3 Poise and 8 Block. Exhaust.", Seq(Apply(Poise, 3, You), Block(8)),
        "Keep — Shield Seal: the next defensive Speak is stored at 150% and raised just before the first enemy attack " +
        "this enemy turn.",
        Keep(150, WandFilter.Defense, WandStoreTrigger.NextEnemyAttack), "rare", [ExhaustTag], act: 2);

    private static readonly BnbCard ExaminersBell = Card(
        "ap_examiners_bell", "The Examiner's Bell", WorkingTag, 1,
        "At the end of each of your turns in which you cast twice or more, gain 2 Rote. Exhaust.",
        Apply(ApprenticeRules.ExaminersBellRule, 1, You),
        "Speak: deal 6 damage, 6 more if you have already cast this turn.",
        Speak(Hit(Amount(Plus(Const(6), IfCastBefore(6, inWand: true))))), "rare", [ExhaustTag], act: 2);

    // ── Act III: a common, three uncommons, five rares ───────────────────────────────────────────────────────

    private static readonly BnbCard MossPoultice = Card(
        "ap_moss_poultice", "Moss Poultice", WorkingTag, 1,
        "Gain 9 Block. Remove 1 stack of a negative status from yourself.", Seq(Block(9), Cleanse(1)),
        "Speak: gain 6 Block; in the first slot cast, also remove 1 negative stack from yourself.",
        Speak(Guard(6), Do(Cleanse(1)).Only(WandPosition.FirstExecuted)), "common", act: 3);

    private static readonly BnbCard Wildfire = Card(
        "ap_wildfire", "Wildfire", DeedTag, 1,
        "The target gains as much Singe again as it has (at most 8).", Apply(Singe, AtMost(Stacks(Singe), 8)),
        "Speak: the target gains as much Singe again as it has (at most 4).",
        Speak(Put(Singe, Amount(AtMost(Stacks(Singe), 4)))), "uncommon", act: 3);

    private static readonly BnbCard RainBarrel = Card(
        "ap_rain_barrel", "Rain Barrel", WorkingTag, 1,
        "Gain 4 Block, 3 more for each Flow you have.", Block(Plus(Const(4), Times(Stacks(Flow, You), 3))),
        "Speak: gain 4 Block; after the cast, gain 2 Flow.",
        Speak(Guard(4), Later(Apply(Flow, 2, You))), "uncommon", act: 3);

    private static readonly BnbCard StandingStone = Card(
        "ap_standing_stone", "Standing Stone", WorkingTag, 2,
        "Gain 14 Block and 2 Grit.", Seq(Block(14), Apply(Grit, 2, You)),
        "Speak: gain 8 Block, 2 more for each Grit you have.",
        Speak(Guard(Amount(Plus(Const(8), Times(Stacks(Grit, You), 2))))), "uncommon", act: 3);

    private static readonly BnbCard BurningGlass = Card(
        "ap_burning_glass", "Burning Glass", WorkingTag, 2,
        "Whenever you cast this combat, apply 1 Singe to ALL enemies. Exhaust.", Apply(ApprenticeRules.BurningGlassRule, 1, You),
        "Burn: apply 3 Singe; then this card is exhausted.",
        Lesson(Put(Singe, 3)), "rare", [ExhaustTag], act: 3);

    private static readonly BnbCard MastersHat = Card(
        "ap_masters_hat", "The Master's Hat", WorkingTag, 2,
        "At the start of each of your turns, gain 1 Focus and 1 Rote. Exhaust.", Apply(ApprenticeRules.MastersHatRule, 1, You),
        "Burn: after the cast, gain 2 Focus; then this card is exhausted.",
        Lesson(Later(GainFocus(2))), "rare", [ExhaustTag], act: 3);

    private static readonly BnbCard Thunderclap = Card(
        "ap_thunderclap", "Thunderclap", DeedTag, 2,
        "Deal 5 damage to ALL enemies, 5 more for each cast you made this turn.",
        Damage(Plus(Const(5), Times(WandValue(WandField.CastsThisTurn), 5)), AllEnemies),
        "Speak: deal 5 damage to ALL enemies, 5 more for each earlier cast this turn.",
        Speak(Hit(Amount(Times(WandValue(WandField.CastsThisTurn), 5)), WandTargeting.AllEnemies)), "rare", act: 3);

    private static readonly BnbCard Millstone = Card(
        "ap_millstone", "The Millstone", WorkingTag, 2,
        "Gain 6 Block for each Grit you have (at least 6), then lose all Grit.",
        Seq(Block(AtLeast(Times(Stacks(Grit, You), 6), 6)), new CombatNodeModel("removeStatus", You, StatusId: Grit)),
        "Speak: gain 3 Block for each cast this combat (at most 30).",
        Speak(Guard(Amount(AtMost(Times(WandValue(WandField.CastsThisCombat), 3), 30)))), "rare", act: 3);

    private static readonly BnbCard SpringTide = Card(
        "ap_spring_tide", "Spring Tide", WorkingTag, 1,
        "Gain 4 Flow and 2 Poise. Exhaust.", Seq(Apply(Flow, 4, You), Apply(Poise, 2, You)),
        "Speak: after the cast, gain 2 Flow and 1 Poise.",
        Speak(Later(Seq(Apply(Flow, 2, You), Apply(Poise, 1, You)))), "rare", [ExhaustTag], act: 3);

    // ── Act IV: a common, two uncommons, four rares ──────────────────────────────────────────────────────────

    private static readonly BnbCard SlideRule = Card(
        "ap_slide_rule", "Slide Rule", WorkingTag, 1,
        "Gain 8 Block. If the wand holds 4 or more cards, draw 1 card.",
        Seq(Block(8), IfAtLeast(WandValue(WandField.Occupied), 4, Draw(1))),
        "Ground: 2 less Exhaustion, or 4 less when 4 or more instructions run.",
        Ground(WandRule.ByLength, amount: 2, amount2: 4, cap: 4), "common", act: 4);

    private static readonly BnbCard Compendium = Card(
        "ap_compendium", "Compendium", WorkingTag, 1,
        "Gain 1 Rote and 1 Poise. Draw 2 cards.", Seq(Apply(Rote, 1, You), Apply(Poise, 1, You), Draw(2)),
        "Echo: repeat the last Speak at 75%.",
        Echo(75), "uncommon", act: 4);

    private static readonly BnbCard FinalDraft = Card(
        "ap_final_draft", "Final Draft", DeedTag, 2,
        "Deal 14 damage, 1 more for each Rote and each Poise you have.",
        Damage(Plus(Const(14), Plus(Stacks(Rote, You), Stacks(Poise, You)))),
        "Speak: deal 8 damage.",
        Speak(Hit(8)), "uncommon", act: 4);

    private static readonly BnbCard MagnumOpus = Card(
        "ap_magnum_opus", "The Magnum Opus", WorkingTag, 3,
        "Double your Rote and Poise. Exhaust.",
        Seq(Apply(Rote, Stacks(Rote, You), You), Apply(Poise, Stacks(Poise, You), You)),
        "Burn: after the cast, gain 1 Rote for each card in the wand; then this card is exhausted.",
        Lesson(Later(Apply(Rote, WandValue(WandField.Occupied), You))), "rare", [ExhaustTag], act: 4);

    private static readonly BnbCard SorcerersStaff = Card(
        "ap_sorcerers_staff", "The Sorcerer's Staff", WorkingTag, 2,
        "The wand has one more slot (6 at most) and a 2 higher Exhaustion cap this combat. Gain 2 Focus. Exhaust.",
        Seq(WandMove(WandAction.Extend, 1), WandMove(WandAction.RaiseCap, 2), GainFocus(2)),
        "Speak: deal 10 damage.",
        Speak(Hit(10)), "rare", [ExhaustTag], act: 4);

    private static readonly BnbCard UnfinishedSpell = Card(
        "ap_unfinished_spell", "The Unfinished Spell", DeedTag, 1,
        "Deal 6 damage, 6 more for each card in the wand.", Damage(Plus(Const(6), Times(WandValue(WandField.Occupied), 6))),
        "Speak: deal 3 damage for each Speak resolved this combat (at most 24).",
        Speak(Hit(Amount(AtMost(Times(WandValue(WandField.SpeaksThisCombat), 3), 24)))), "rare", act: 4);

    private static readonly BnbCard ApprenticesOath = Card(
        "ap_apprentices_oath", "The Apprentice's Oath", WorkingTag, 1,
        "Whenever you cast this combat, deal 3 damage to ALL enemies and gain 3 Block. Exhaust.",
        Apply(ApprenticeRules.OathRule, 1, You),
        "Speak: deal 3 damage to ALL enemies and gain 3 Block.",
        Speak(Hit(3, to: WandTargeting.AllEnemies), Guard(3)), "rare", [ExhaustTag], act: 4);

    // ── the list, with every upgrade ─────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<BnbCard> Expansion() =>
    [
        Recitation, Recitation.Up("Gain 3 Rote. Draw 1 card. Exhaust.", Seq(Apply(Rote, 3, You), Draw(1)),
            "Speak: gain 4 Block; after the cast, gain 1 Rote.",
            Speak(Guard(4), Later(Apply(Rote, 1, You)))),
        SteadyHand, SteadyHand.Up("Gain 3 Poise and 7 Block. Exhaust.", Seq(Apply(Poise, 3, You), Block(7)),
            "Speak: gain 6 Block; after the cast, gain 1 Poise.",
            Speak(Guard(6), Later(Apply(Poise, 1, You)))),
        Kindling, Kindling.Up("Deal 5 damage twice; each hit then applies 1 Singe.", Repeat(2, Seq(Damage(5), Apply(Singe, 1))),
            "Speak: deal 4 damage twice.", Speak(Hit(4, 2))),
        EmberTongs, EmberTongs.Up("Deal 4 damage, 4 more for each Singe on the target (at most 20 more).",
            Damage(Plus(Const(4), AtMost(Times(Stacks(Singe), 4), 20))),
            "Speak: deal 2 damage, 3 more for each Singe on the target (at most 15 more).",
            Speak(Hit(Amount(Plus(Const(2), AtMost(Times(Stacks(Singe), 3), 15)))))),
        TracingPaper, TracingPaper.Up("Gain 9 Block. Draw 1 card.", Seq(Block(9), Draw(1)),
            "Echo: repeat the last damaging Speak at 125%; then this card burns.",
            Echo(125, WandFilter.Damage, burns: true)),
        QuillKnife, QuillKnife.Up("Deal 6 damage, 6 more if you inscribed a card this turn.",
            Damage(Plus(Const(6), Times(Once(WandValue(WandField.InscribedThisTurn)), 6))),
            "Speak: deal 4 damage, 3 more for each empty slot (at most 12 more).",
            Speak(Hit(Amount(Plus(Const(4), AtMost(Times(WandValue(WandField.FreeSlots), 3), 12)))))),
        Inkhorn, Inkhorn.Up("Gain 3 Focus. Draw 1 card.", Seq(GainFocus(3), Draw(1)),
            "Speak: after the cast, gain 1 Focus.", Speak(Later(GainFocus(1)))),
        Copybook, Copybook.Up("At the start of each of your turns, gain 1 Rote and 1 Poise. Exhaust.",
            Apply(ApprenticeRules.CopybookRule, 1, You),
            "Burn: after the cast, gain 3 Rote; then this card is exhausted.",
            Lesson(Later(Apply(Rote, 3, You))), cost: 1),
        BankedFire, BankedFire.Up(
            "Deal 6 damage to ALL enemies, 4 more for each Exhaustion the wand has. Then the wand's Exhaustion drops to 0.",
            Seq(Damage(Plus(Const(6), Times(WandValue(WandField.Exhaustion), 4)), AllEnemies), WandMove(WandAction.Exhaustion, -12)),
            "Heighten: the next damaging Speak deals 200% more; 2 more Exhaustion.",
            Heighten(200, filter: WandFilter.Damage, strain: 2)),
        TuningFork, TuningFork.Up("Gain 3 Focus. Draw 2 cards.", Seq(GainFocus(3), Draw(2)),
            "Echo: repeat the last Speak at 175%.", Echo(175)),

        SealingWax, SealingWax.Up("Gain 8 Block and 4 Ward Wax.", Seq(Block(8), Apply(Cards.Keywords.WardWax, 4, You)),
            "Speak: gain 4 Ward Wax, 1 more for each Grit you have (at most 3 more).",
            Speak(WaxOf(Plus(Const(4), AtMost(Stacks(Grit, You), 3))))),
        DrillBook, DrillBook.Up("Deal 8 damage, 2 more for each cast this combat (at most 12 more).",
            Damage(Plus(Const(8), AtMost(Times(WandValue(WandField.CastsThisCombat), 2), 12))),
            "Speak: deal 5 damage, 1 more for each cast this combat (at most 8 more).",
            Speak(Hit(Amount(Plus(Const(5), AtMost(WandValue(WandField.CastsThisCombat), 8)))))),
        RedPencil, RedPencil.Up("Deal 12 damage.", Damage(12),
            "Heighten: the next damaging Speak deals 100% more.", Heighten(100, filter: WandFilter.Damage)),
        Bookend, Bookend.Up("Gain 11 Block, 4 more if the wand is full.", Block(Plus(Const(11), Times(WandFull, 4))),
            "Speak: gain 6 Block, 2 more for each card in the wand.",
            Speak(Guard(Amount(Plus(Const(6), Times(WandValue(WandField.Occupied), 2)))))),
        PressedFlower, PressedFlower.Up("Gain 3 Flow. Draw 1 card.", Seq(Apply(Flow, 3, You), Draw(1)),
            "Keep: the next defensive Speak is stored at 150% and goes first in your next cast.", Keep(150, WandFilter.Defense)),
        SootMark, SootMark.Up("Apply 6 Singe.", Apply(Singe, 6),
            "Speak: apply 3 Singe to ALL enemies.", Speak(PutAll(Singe, 3))),
        StoneTablet, StoneTablet.Up("Gain 3 Grit and 7 Block.", Seq(Apply(Grit, 3, You), Block(7)),
            "Speak: gain 3 Grit.", Speak(Gain(Grit, 3))),
        Weathervane, Weathervane.Up("Gain 3 Gust. Draw 1 card.", Seq(Apply(Gust, 3, You), Draw(1)),
            "Order: the next two instructions trade places before they run; if that changes which Speak a Heighten " +
            "reaches, 3 less Exhaustion.", Order(WandRule.SwapNext, amount: 3)),
        Lectern, Lectern.Up("Gain 3 Rote. Exhaust.", Apply(Rote, 3, You),
            "Burn: after the cast, gain 3 Rote; then this card is exhausted.", Lesson(Later(Apply(Rote, 3, You))), cost: 1),
        LeadType, LeadType.Up("Gain 10 Block. Draw 1 card.", Seq(Block(10), Draw(1)),
            "Shape — Join: the next two damaging Speaks become one hit of both together, +9.", Shape(WandRule.Join, amount: 9)),
        ScorchMark, ScorchMark.Up("Deal 11 damage to ALL enemies.", Damage(11, AllEnemies),
            "Speak: deal 6 damage to ALL enemies.", Speak(Hit(6, to: WandTargeting.AllEnemies))),
        BorrowedGrimoire, BorrowedGrimoire.Up("Whenever you cast this combat, gain 1 Rote. Exhaust.",
            Apply(ApprenticeRules.GrimoireRule, 1, You),
            "Burn: after the cast, gain 4 Rote; then this card is exhausted.", Lesson(Later(Apply(Rote, 4, You))), cost: 1),
        BellBook, BellBook.Up("Deal 8 damage 3 times; each hit then applies 1 Singe.", Repeat(3, Seq(Damage(8), Apply(Singe, 1))),
            "Speak: deal 5 damage 3 times.", Speak(Hit(5, 3))),
        LongRecitation, LongRecitation.Up("Deal 5 damage and gain 4 Block for each card in the wand.",
            Seq(Damage(Times(WandValue(WandField.Occupied), 5)), Block(Times(WandValue(WandField.Occupied), 4))),
            "Speak: deal 3 damage for each cast this combat (at most 30).",
            Speak(Hit(Amount(AtMost(Times(WandValue(WandField.CastsThisCombat), 3), 30))))),
        WardingPrimer, WardingPrimer.Up("Gain 3 Poise and 8 Block. Exhaust.", Seq(Apply(Poise, 3, You), Block(8)),
            "Keep — Shield Seal: the next defensive Speak is stored at 200% and raised just before the first enemy " +
            "attack this enemy turn.",
            Keep(200, WandFilter.Defense, WandStoreTrigger.NextEnemyAttack), cost: 1),
        ExaminersBell, ExaminersBell.Up("At the end of each of your turns in which you cast twice or more, gain 2 Rote. Exhaust.",
            Apply(ApprenticeRules.ExaminersBellRule, 1, You),
            "Speak: deal 9 damage, 6 more if you have already cast this turn.",
            Speak(Hit(Amount(Plus(Const(9), IfCastBefore(6, inWand: true))))), cost: 0),

        MossPoultice, MossPoultice.Up("Gain 12 Block. Remove 1 stack of a negative status from yourself.", Seq(Block(12), Cleanse(1)),
            "Speak: gain 8 Block; in the first slot cast, also remove 1 negative stack from yourself.",
            Speak(Guard(8), Do(Cleanse(1)).Only(WandPosition.FirstExecuted))),
        Wildfire, Wildfire.Up("The target gains as much Singe again as it has (at most 12).", Apply(Singe, AtMost(Stacks(Singe), 12)),
            "Speak: the target gains as much Singe again as it has (at most 6).",
            Speak(Put(Singe, Amount(AtMost(Stacks(Singe), 6))))),
        RainBarrel, RainBarrel.Up("Gain 6 Block, 3 more for each Flow you have.", Block(Plus(Const(6), Times(Stacks(Flow, You), 3))),
            "Speak: gain 4 Block; after the cast, gain 3 Flow.", Speak(Guard(4), Later(Apply(Flow, 3, You)))),
        StandingStone, StandingStone.Up("Gain 18 Block and 2 Grit.", Seq(Block(18), Apply(Grit, 2, You)),
            "Speak: gain 10 Block, 2 more for each Grit you have.",
            Speak(Guard(Amount(Plus(Const(10), Times(Stacks(Grit, You), 2)))))),
        BurningGlass, BurningGlass.Up("Whenever you cast this combat, apply 1 Singe to ALL enemies. Exhaust.",
            Apply(ApprenticeRules.BurningGlassRule, 1, You),
            "Burn: apply 5 Singe; then this card is exhausted.", Lesson(Put(Singe, 5)), cost: 1),
        MastersHat, MastersHat.Up("At the start of each of your turns, gain 1 Focus and 1 Rote. Exhaust.",
            Apply(ApprenticeRules.MastersHatRule, 1, You),
            "Burn: after the cast, gain 3 Focus; then this card is exhausted.", Lesson(Later(GainFocus(3))), cost: 1),
        Thunderclap, Thunderclap.Up("Deal 7 damage to ALL enemies, 7 more for each cast you made this turn.",
            Damage(Plus(Const(7), Times(WandValue(WandField.CastsThisTurn), 7)), AllEnemies),
            "Speak: deal 7 damage to ALL enemies, 7 more for each earlier cast this turn.",
            Speak(Hit(Amount(Times(WandValue(WandField.CastsThisTurn), 7)), WandTargeting.AllEnemies))),
        Millstone, Millstone.Up("Gain 8 Block for each Grit you have (at least 8), then lose all Grit.",
            Seq(Block(AtLeast(Times(Stacks(Grit, You), 8), 8)), new CombatNodeModel("removeStatus", You, StatusId: Grit)),
            "Speak: gain 4 Block for each cast this combat (at most 40).",
            Speak(Guard(Amount(AtMost(Times(WandValue(WandField.CastsThisCombat), 4), 40))))),
        SpringTide, SpringTide.Up("Gain 6 Flow and 2 Poise. Exhaust.", Seq(Apply(Flow, 6, You), Apply(Poise, 2, You)),
            "Speak: after the cast, gain 3 Flow and 1 Poise.", Speak(Later(Seq(Apply(Flow, 3, You), Apply(Poise, 1, You))))),

        SlideRule, SlideRule.Up("Gain 11 Block. If the wand holds 4 or more cards, draw 1 card.",
            Seq(Block(11), IfAtLeast(WandValue(WandField.Occupied), 4, Draw(1))),
            "Ground: 3 less Exhaustion, or 5 less when 4 or more instructions run.",
            Ground(WandRule.ByLength, amount: 3, amount2: 5, cap: 4)),
        Compendium, Compendium.Up("Gain 1 Rote and 1 Poise. Draw 3 cards.", Seq(Apply(Rote, 1, You), Apply(Poise, 1, You), Draw(3)),
            "Echo: repeat the last Speak at full strength.", Echo(100)),
        FinalDraft, FinalDraft.Up("Deal 18 damage, 1 more for each Rote and each Poise you have.",
            Damage(Plus(Const(18), Plus(Stacks(Rote, You), Stacks(Poise, You)))),
            "Speak: deal 11 damage.", Speak(Hit(11))),
        MagnumOpus, MagnumOpus.Up("Double your Rote and Poise. Exhaust.",
            Seq(Apply(Rote, Stacks(Rote, You), You), Apply(Poise, Stacks(Poise, You), You)),
            "Burn: after the cast, gain 1 Rote for each card in the wand; then this card is exhausted.",
            Lesson(Later(Apply(Rote, WandValue(WandField.Occupied), You))), cost: 2),
        SorcerersStaff, SorcerersStaff.Up("The wand has one more slot (6 at most) and a 2 higher Exhaustion cap this combat. " +
            "Gain 2 Focus. Exhaust.",
            Seq(WandMove(WandAction.Extend, 1), WandMove(WandAction.RaiseCap, 2), GainFocus(2)),
            "Speak: deal 14 damage.", Speak(Hit(14)), cost: 1),
        UnfinishedSpell, UnfinishedSpell.Up("Deal 8 damage, 8 more for each card in the wand.",
            Damage(Plus(Const(8), Times(WandValue(WandField.Occupied), 8))),
            "Speak: deal 4 damage for each Speak resolved this combat (at most 32).",
            Speak(Hit(Amount(AtMost(Times(WandValue(WandField.SpeaksThisCombat), 4), 32))))),
        ApprenticesOath, ApprenticesOath.Up("Whenever you cast this combat, deal 3 damage to ALL enemies and gain 3 Block. Exhaust.",
            Apply(ApprenticeRules.OathRule, 1, You),
            "Speak: deal 5 damage to ALL enemies and gain 5 Block.",
            Speak(Hit(5, to: WandTargeting.AllEnemies), Guard(5)), cost: 0),
    ];
}
