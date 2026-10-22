using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Lieutenant.CardShapes;

namespace BnbContent.Converter.Lieutenant;

// The twenty commons (§3.2): honest attacks and Block, the simple Orders and Challenges, and the first ways to
// spend what she earns. All from Act I (plan L2). Upgrades by the game's rules (plan §1, §4).
public static partial class LieutenantCards
{
    private static BnbCard Common(string id, string name, string type, int cost, string text, CombatNodeModel program,
        IReadOnlyList<string>? tags = null) => Card(id, name, type, cost, text, program, "common", 1, tags);

    private static readonly BnbCard PikeButt = Common("lt_pike_butt", "Pike Butt", DeedTag, 1,
        "Deal 4 damage twice.", Repeat(2, Damage(4)));

    private static CombatNodeModel TakeBlock(int most) =>
        new("modifyDefensivePool", Target,
            CombatAmountSpec.Unary("neg", AtMost(new CombatAmountSpec("defensivePool", SelectorKey: Target,
                ReadId: StandardCombatIds.BlockDefensivePool.value), most)),
            PoolId: StandardCombatIds.BlockDefensivePool.value);

    private static readonly BnbCard CutTheStrap = Common("lt_cut_the_strap", "Cut the Strap", DeedTag, 1,
        "Remove up to 8 Block from the target, then deal 6 damage.", Seq(TakeBlock(8), Damage(6)));

    private static CombatNodeModel HeelTurnHit(int damage) => If(Has(OpenGuard), Repeat(2, Damage(damage)), Damage(damage));

    private static readonly BnbCard HeelTurn = Common("lt_heel_turn", "Heel Turn", DeedTag, 0,
        "Deal 3 damage. If you have Open Guard, deal 3 damage twice instead.", HeelTurnHit(3));

    // One Open Guard for each enemy standing when the blow falls, at most 3.
    private static CombatNodeModel ClearTheSquareHit(int damage) => Seq(
        SetScratch(new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec(AllEnemies))),
        Damage(damage, AllEnemies),
        Apply(OpenGuard, AtMost(new CombatAmountSpec("counter", SelectorKey: You, CounterId: Scratch), 3), You));

    private static readonly BnbCard ClearTheSquare = Common("lt_clear_the_square", "Clear the Square", DeedTag, 2,
        "Deal 8 damage to ALL enemies. Gain 1 Open Guard for each enemy hit, at most 3.", ClearTheSquareHit(8));

    // "If you lost HP to an enemy Attack during the last enemy phase" — her phase count still holds it.
    private static CombatConditionSpec HurtLastPhase =>
        new("compare", You, ValueKind: "counter", Op: ComparisonOperator.Greater, Right: 0, Id: LieutenantKeywords.PhaseHp);

    private static readonly BnbCard ClenchedJaw = Common("lt_clenched_jaw", "Clenched Jaw", WorkingTag, 1,
        "Gain 6 Block. If an enemy Attack took your HP last enemy phase, gain 1 Strength.",
        Seq(Block(6), If(HurtLastPhase, Apply(Strength, 1, You))));

    private static CombatNodeModel BuckleUp(int block) =>
        Seq(Apply(Strength, 1, You), Block(block), Apply(OpenGuard, 1, You), Apply(LieutenantRules.Buckle, 1, You));

    private static readonly BnbCard Buckle = Common("lt_buckle", "Buckle", WorkingTag, 1,
        "Gain 1 Strength, 3 Block and 1 Open Guard. If you keep an Order this turn, lose that Open Guard.", BuckleUp(3));

    private static CombatNodeModel DrawDiscard(string self, int draw, int discard) =>
        Seq(Draw(draw), Repeat(discard, new CombatNodeModel("moveCardToZone", You,
            Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to discard", ExcludeTag: Self(self)),
            ToZone: CardZone.DiscardPile)));

    private static readonly BnbCard RollCall = Common("lt_roll_call", "Roll Call", WorkingTag, 0,
        $"Draw 1 card. {Promises.RollCall.Text}", Seq(Draw(1), Issue(Promises.RollCall)));

    private static readonly BnbCard Gatehouse = Common("lt_gatehouse", "Gatehouse", WorkingTag, 1,
        $"Gain 8 Block. {Promises.Gatehouse.Text}", Seq(Block(8), Issue(Promises.Gatehouse)));

    private static readonly BnbCard LastBell = Common("lt_last_bell", "Last Bell", DeedTag, 1,
        $"Deal 7 damage. {Promises.LastBell.Text}", Seq(Damage(7), Issue(Promises.LastBell)));

    private static readonly BnbCard Scabbard = Common("lt_scabbard", "Scabbard", WorkingTag, 0,
        $"Gain 4 Block. {Promises.Scabbard.Text}", Seq(Block(4), Issue(Promises.Scabbard)));

    private static readonly BnbCard MarkedMan = Common("lt_marked_man", "Marked Man", DeedTag, 1,
        $"Deal 5 damage. {Promises.MarkedMan.Text}", Seq(Damage(5), Issue(Promises.MarkedMan)));

    // "Choose an enemy intending to attack" — the card's target, if it does (plan L7).
    private static CombatConditionSpec IntendsAttack => new("intends", Target, Id: nameof(IntentKind.Attack));

    private static CombatNodeModel BracedPikeBrace(int block) =>
        Seq(Block(block), If(IntendsAttack, Issue(Promises.BracedPike)));

    private static readonly BnbCard BracedPike = Common("lt_braced_pike", "Braced Pike", WorkingTag, 1,
        $"Gain 7 Block. If the target intends to attack: {Promises.BracedPike.Text}", BracedPikeBrace(7));

    private static readonly BnbCard Arrest = Common("lt_arrest", "Arrest", DeedTag, 1,
        $"Deal 6 damage. {Promises.Arrest.Text}", Seq(Damage(6), Issue(Promises.Arrest)));

    private static readonly BnbCard Witness = Common("lt_witness", "Witness", WorkingTag, 0,
        "Gain 4 Block. Your next Deed hit on a Challenged enemy this turn counts twice toward its Challenge.",
        Seq(Block(4), Apply(LieutenantRules.Witness, 1, You)));

    private static CombatNodeModel SignalOut(int block) =>
        Seq(Block(block), MaySpendAuthority(1, "gain 1 Energy", Energy_(1)));

    private static readonly BnbCard Signal = Common("lt_signal", "Signal", WorkingTag, 0,
        "Gain 2 Block. You may spend 1 Authority to gain 1 Energy.", SignalOut(2));

    private static CombatNodeModel SecondRankOut(int block) =>
        Seq(Block(block), MaySpendAuthority(1, "your next Deed this turn costs 0",
            Discount(LieutenantRules.SecondRank, DeedTag, 99, 1)));

    private static readonly BnbCard SecondRank = Common("lt_second_rank", "Second Rank", WorkingTag, 1,
        "Gain 6 Block. You may spend 1 Authority: your next Deed this turn costs 0.", SecondRankOut(6));

    // "If you really spent Strength earlier this turn" — the round of her last payment is this one.
    private static CombatConditionSpec SpentStrengthThisTurn => new("compare", You, ValueKind: "counter",
        Op: ComparisonOperator.GreaterOrEqual, Right: 0, Id: Scratch);

    private static CombatNodeModel IfSpentStrength(CombatNodeModel then) => Seq(
        SetScratch(CombatAmountSpec.Binary("sub",
            new CombatAmountSpec("counter", SelectorKey: You, CounterId: LieutenantKeywords.StrengthPaidRound),
            new CombatAmountSpec("round"))),
        If(SpentStrengthThisTurn, then));

    private static readonly BnbCard Whetstone = Common("lt_whetstone", "Whetstone", WorkingTag, 1,
        "Gain 1 Strength. If you spent Strength earlier this turn, draw 1 card.",
        Seq(Apply(Strength, 1, You), IfSpentStrength(Draw(1))));

    private static CombatNodeModel ArmguardOut(int block, int wax) =>
        Seq(Block(block), MaySpendStrength(1, $"gain {wax} Ward Wax", Apply(Cards.Keywords.WardWax, wax, You)));

    private static readonly BnbCard Armguard = Common("lt_armguard", "Armguard", WorkingTag, 1,
        "Gain 8 Block. You may spend 1 Strength to gain 4 Ward Wax.", ArmguardOut(8, 4));

    private static CombatNodeModel HeadlongOut(int guard) =>
        Seq(Apply(OpenGuard, guard, You), Discount(LieutenantRules.Headlong, DeedTag, 1, 2));

    private static readonly BnbCard Headlong = Common("lt_headlong", "Headlong", WorkingTag, 1,
        "Gain 2 Open Guard. Your next two Deeds this turn cost 1 less.", HeadlongOut(2));

    private static CombatNodeModel SplitLipHit(int damage) =>
        Seq(Damage(damage), If(new CombatConditionSpec("isAlive", Target), Apply(OpenGuard, 1, You), Draw(1)));

    private static readonly BnbCard SplitLip = Common("lt_split_lip", "Split Lip", DeedTag, 1,
        "Deal 7 damage. If this kills the target, draw 1 card; otherwise gain 1 Open Guard.", SplitLipHit(7));

    public static IReadOnlyList<BnbCard> Commons() =>
    [
        PikeButt, PikeButt.Upgraded("Deal 4 damage three times.", Repeat(3, Damage(4))),
        CutTheStrap, CutTheStrap.Upgraded("Remove up to 12 Block from the target, then deal 9 damage.", Seq(TakeBlock(12), Damage(9))),
        HeelTurn, HeelTurn.Upgraded("Retain. Deal 4 damage. If you have Open Guard, deal 4 damage twice instead.",
            HeelTurnHit(4), retainInHand: true),
        ClearTheSquare, ClearTheSquare.Upgraded("Deal 12 damage to ALL enemies. Gain 1 Open Guard for each enemy hit, at most 3.",
            ClearTheSquareHit(12)),
        ClenchedJaw, ClenchedJaw.Upgraded("Gain 9 Block. If an enemy Attack took your HP last enemy phase, gain 1 Strength.",
            Seq(Block(9), If(HurtLastPhase, Apply(Strength, 1, You)))),
        Buckle, Buckle.Upgraded("Gain 1 Strength, 6 Block and 1 Open Guard. If you keep an Order this turn, lose that Open Guard.",
            BuckleUp(6)),
        RollCall, RollCall.Upgraded($"Draw 2 cards, then discard 1. {Promises.RollCall.Text}",
            Seq(DrawDiscard(RollCall.Id, 2, 1), Issue(Promises.RollCall))),
        Gatehouse, Gatehouse.Upgraded($"Gain 12 Block. {Promises.Gatehouse.Text}", Seq(Block(12), Issue(Promises.Gatehouse))),
        LastBell, LastBell.Upgraded($"Deal 10 damage. {Promises.LastBellPlus.Text}", Seq(Damage(10), Issue(Promises.LastBellPlus))),
        Scabbard, Scabbard.Upgraded($"Gain 6 Block. {Promises.Scabbard.Text}", Seq(Block(6), Issue(Promises.Scabbard))),
        MarkedMan, MarkedMan.Upgraded($"Deal 8 damage. {Promises.MarkedMan.Text}", Seq(Damage(8), Issue(Promises.MarkedMan))),
        BracedPike, BracedPike.Upgraded($"Gain 11 Block. If the target intends to attack: {Promises.BracedPike.Text}", BracedPikeBrace(11)),
        Arrest, Arrest.Upgraded($"Deal 9 damage. {Promises.Arrest.Text}", Seq(Damage(9), Issue(Promises.Arrest))),
        Witness, Witness.Upgraded("Gain 6 Block. Your next Deed hit on a Challenged enemy this turn counts twice toward its Challenge.",
            Seq(Block(6), Apply(LieutenantRules.Witness, 1, You))),
        Signal, Signal.Upgraded("Retain. Gain 3 Block. You may spend 1 Authority to gain 1 Energy.", SignalOut(3), retainInHand: true),
        SecondRank, SecondRank.Upgraded("Gain 9 Block. You may spend 1 Authority: your next Deed this turn costs 0.", SecondRankOut(9)),
        Whetstone, Whetstone.Upgraded("Gain 1 Strength. If you spent Strength earlier this turn, gain 1 more and draw 1 card.",
            Seq(Apply(Strength, 1, You), IfSpentStrength(Seq(Apply(Strength, 1, You), Draw(1))))),
        Armguard, Armguard.Upgraded("Gain 12 Block. You may spend 1 Strength to gain 4 Ward Wax.", ArmguardOut(12, 4)),
        Headlong, Headlong.Upgraded("Gain 1 Open Guard. Your next two Deeds this turn cost 1 less.", HeadlongOut(1)),
        SplitLip, SplitLip.Upgraded("Deal 11 damage. If this kills the target, draw 1 card; otherwise gain 1 Open Guard.", SplitLipHit(11)),
    ];
}
