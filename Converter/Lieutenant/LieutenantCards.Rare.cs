using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Lieutenant.CardShapes;

namespace BnbContent.Converter.Lieutenant;

// The twenty-five rares (§3.4, guardrails §2.7): 8 from Act I with a plain use of their own, then 6 / 6 / 5 over
// Acts II–IV for the engines (plan L2). Parade Ground, Blood Debt and Reprimand are active Workings, as the canon
// revised them.
public static partial class LieutenantCards
{
    private static BnbCard Rare(string id, string name, string type, int cost, string text, CombatNodeModel program,
        int act, IReadOnlyList<string>? tags = null) => Card(id, name, type, cost, text, program, "rare", act, tags);

    private static CombatNodeModel SetCounter(string counter, CombatAmountSpec amount) =>
        new("setCombatantCounter", You, amount, CounterId: counter, Relative: false);

    private static CombatAmountSpec MyCounter(string counter) => new("counter", SelectorKey: You, CounterId: counter);

    // ── Act I ────────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel BrevetOut(int authority, int block) =>
        Seq(GainAuthority(authority), Block(block), Apply(LieutenantRules.Brevet, 1, You));

    private static readonly BnbCard Brevet = Rare("lt_brevet", "Brevet", WorkingTag, 1,
        "Gain 1 Authority and 6 Block. The next Authority you spend this turn costs 1 less. Exhaust.", BrevetOut(1, 6), 1, [ExhaustTag]);

    // "You may abandon an Order": the oldest goes, without failing (§2.3).
    private static CombatConditionSpec HasAnOrder => new("compare", You, ValueKind: "counter",
        Op: ComparisonOperator.Greater, Right: 0, Id: Obligations.OlderOrder);

    private static CombatNodeModel InsubordinationHit(int damage) => Seq(Damage(damage),
        If(HasAnOrder, If(new CombatConditionSpec("isAlive", Target),
            CombatNodeModel.ChooseOptions(1, [$"Abandon your oldest Order: deal {damage} again and gain 1 Open Guard", "Keep your Order"],
                [Seq(Apply(Obligations.AbandonPulse, 1, You), Damage(damage), Apply(OpenGuard, 1, You)), Seq()], "Insubordination"))));

    private static readonly BnbCard Insubordination = Rare("lt_insubordination", "Insubordination", DeedTag, 1,
        "Deal 10 damage. You may abandon your oldest Order to deal 10 damage again and gain 1 Open Guard.", InsubordinationHit(10), 1);

    private static CombatNodeModel ParadeGroundOut(int block, int most = 2) => Seq(Block(block),
        SetCounter(LieutenantRules.ParadeBase, MyCounter(Obligations.OrdersKept)),
        SetCounter(LieutenantRules.ParadeCap, CombatAmountSpec.FromConst(most)),
        Apply(LieutenantRules.ParadeGround, 5, You));

    private static readonly BnbCard ParadeGround = Rare("lt_parade_ground", "Parade Ground", WorkingTag, 1,
        "Gain 10 Block. If you keep an Order before this turn ends, gain 5 Ward Wax for each Energy you end it with (at most 2).",
        ParadeGroundOut(10), 1);

    private static CombatNodeModel DrumMajorOut(int block, int stacks = 1) =>
        Seq(Block(block), Draw(1), new CombatNodeModel("removeStatus", You, StatusId: LieutenantRules.DrumMajor),
            Apply(LieutenantRules.DrumMajor, stacks, You));

    private static readonly BnbCard DrumMajor = Rare("lt_drum_major", "Drum Major", WorkingTag, 1,
        "Gain 6 Block and draw 1 card. The first Order you keep before your next turn gives 1 Energy.", DrumMajorOut(6), 1);

    private static CombatNodeModel ServiceRecordHit(int damage) => Damage(Plus(CombatAmountSpec.FromConst(damage),
        Times(AtMost(MyCounter(Obligations.ChallengesWon), 4), 4)));

    private static readonly BnbCard ServiceRecord = Rare("lt_service_record", "Service Record", DeedTag, 1,
        "Deal 8 damage, plus 4 for each Challenge you have kept this combat (at most 4).", ServiceRecordHit(8), 1);

    private static CombatNodeModel SplinteredShieldOut(int block, int again) => Seq(Block(block),
        CombatNodeModel.ChooseOptions(1, ["Break your Block: gain 1 Strength, 1 Authority and 2 Open Guard", "Keep the Block"],
        [
            Seq(
                new CombatNodeModel("modifyDefensivePool", You, CombatAmountSpec.Unary("neg",
                    new CombatAmountSpec("defensivePool", SelectorKey: You, ReadId: StandardCombatIds.BlockDefensivePool.value)),
                    PoolId: StandardCombatIds.BlockDefensivePool.value),
                Apply(Strength, 1, You), GainAuthority(1), Apply(OpenGuard, 2, You),
                again > 0 ? Block(again) : Seq()),
            Seq(),
        ], "Splintered Shield"));

    private static readonly BnbCard SplinteredShield = Rare("lt_splintered_shield", "Splintered Shield", WorkingTag, 1,
        "Gain 14 Block. You may lose all your Block to gain 1 Strength, 1 Authority and 2 Open Guard.", SplinteredShieldOut(14, 0), 1);

    // 1 more for every 3 HP she is missing, at most `most`; Open Guard doubles that part only (§3.4, corrected).
    private static CombatNodeModel LastDitchHit(int damage, int most)
    {
        var bonus = AtMost(CombatAmountSpec.Binary("div", new CombatAmountSpec("missingHealth", SelectorKey: You), CombatAmountSpec.FromConst(3)), most);
        return CombatNodeModel.ChooseOptions(1, ["Gain 1 Open Guard: double the missing-HP part", "Strike as you are"],
        [
            Seq(Apply(OpenGuard, 1, You), Damage(Plus(CombatAmountSpec.FromConst(damage), Times(bonus, 2)))),
            Damage(Plus(CombatAmountSpec.FromConst(damage), bonus)),
        ], "Last Ditch");
    }

    private static readonly BnbCard LastDitch = Rare("lt_last_ditch", "Last Ditch", DeedTag, 1,
        "Deal 12 damage, plus 1 for every 3 HP you are missing (at most 12). You may gain 1 Open Guard to double that bonus.",
        LastDitchHit(12, 12), 1);

    private static CombatNodeModel OpenVisorOut(int block) => Seq(Block(block),
        If(Has(OpenGuard), Seq(),
            CombatNodeModel.ChooseOptions(1, ["Gain 1 Open Guard: your next Deed ignores Block", "Keep your guard up"],
                [Apply(OpenGuard, 1, You), Seq()], "Open Visor")),
        If(Has(OpenGuard), Apply(LieutenantRules.OpenVisor, 1, You)));

    private static readonly BnbCard OpenVisor = Rare("lt_open_visor", "Open Visor", WorkingTag, 1,
        "Gain 7 Block. If you have no Open Guard, you may gain 1. With Open Guard, your next Deed this turn ignores Block.",
        OpenVisorOut(7), 1);

    // ── Act II ───────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard BlackGauntlet = Rare("lt_black_gauntlet", "Black Gauntlet", DeedTag, 1,
        $"Deal 11 damage. {Promises.BlackGauntlet.Text}", Seq(Damage(11), Issue(Promises.BlackGauntlet)), 2);

    private static CombatNodeModel RedressOut(int block) => Seq(Block(block),
        If(new CombatConditionSpec("isAlive", Target),
            CombatNodeModel.ChooseOptions(1, ["Move your Challenge to the target", "Leave it where it is"],
                [Apply(Obligations.RedressPulse, 1, Target), Seq()], "Redress")));

    private static readonly BnbCard Redress = Rare("lt_redress", "Redress", WorkingTag, 1,
        "Gain 10 Block. You may move your Challenge to the target, if it fits it; its count starts again, its deadline stays. " +
        "Once per Challenge; not Pursuit.", RedressOut(10), 2);

    private static readonly BnbCard Riposte = Rare("lt_riposte", "Riposte", RiteTag, 1,
        "Gain 6 Block. Once each enemy phase, when a Challenged enemy has attacked you, deal it 5 damage plus your Strength.",
        Seq(Block(6), Apply(LieutenantRules.Riposte, 5, You)), 2);

    private static CombatNodeModel CounterchargeHit(int damage, int most, int block = 0)
    {
        CombatNodeModel Spend(int n) => Seq(PayDeclared(n), Apply(Cards.Keywords.Doubt, n), GainAuthority(n),
            block > 0 ? Block(block * n) : Seq());
        var options = Enumerable.Range(1, most).Reverse()
            .Select(n => (Label: $"Spend {n} Strength: {n} Doubt and {n} Authority", Node: Spend(n), Need: n)).ToList();
        CombatNodeModel Offer(int have) => CombatNodeModel.ChooseOptions(1,
            [.. options.Where(o => o.Need <= have).Select(o => o.Label), "Keep the Strength"],
            [.. options.Where(o => o.Need <= have).Select(o => o.Node), Seq()], "Countercharge");
        CombatNodeModel ByStrength(int have) => have == 0 ? Seq() : If(Has(Strength, have), Offer(have), ByStrength(have - 1));
        return Seq(Damage(damage), ByStrength(most));
    }

    private static readonly BnbCard Countercharge = Rare("lt_countercharge", "Countercharge", DeedTag, 1,
        "Deal 9 damage. Then spend up to 2 Strength: for each, apply 1 Doubt and gain 1 Authority.", CounterchargeHit(9, 2), 2);

    private static CombatNodeModel ForcedMarchOut(int strength) =>
        Seq(Apply(Strength, strength, You), Apply(OpenGuard, 1, You), Apply(LieutenantRules.ForcedMarch, strength, You));

    private static readonly BnbCard ForcedMarch = Rare("lt_forced_march", "Forced March", WorkingTag, 1,
        "Gain 3 Strength and 1 Open Guard. At the end of this turn, lose up to 3 of the Strength you still have.", ForcedMarchOut(3), 2);

    // "If an Order failed at its deadline in your last turn or the enemy phase after it" — the round before this one.
    private static CombatNodeModel ReprimandOut(int block, int strength) => Seq(Block(block),
        SetScratch(CombatAmountSpec.Binary("sub", new CombatAmountSpec("round"), MyCounter(Obligations.OrderFailedRound))),
        If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Equal, Right: 1, Id: Scratch),
            Seq(Apply(Strength, strength, You), Apply(OpenGuard, 1, You))));

    private static readonly BnbCard Reprimand = Rare("lt_reprimand", "Reprimand", WorkingTag, 1,
        "Gain 9 Block. If an Order failed at its deadline last turn, gain 2 Strength and 1 Open Guard.", ReprimandOut(9, 2), 2);

    // ── Act III ──────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel ChainOut(int block, int draw) => Seq(Block(block),
        SetCounter(LieutenantRules.ChainBase, MyCounter(Obligations.OrdersKept)),
        Apply(LieutenantRules.ChainOfCommand, draw, You));

    private static readonly BnbCard ChainOfCommand = Rare("lt_chain_of_command", "Chain of Command", RiteTag, 1,
        "Gain 7 Block. You may hold two Orders. If you keep two Orders in a turn, draw 2 more cards next turn.", ChainOut(7, 2), 3);

    private static CombatNodeModel SuccessionOut(int strength) =>
        Seq(Apply(Strength, strength, You), Block(5), Apply(LieutenantRules.Succession, 1, You));

    private static readonly BnbCard Succession = Rare("lt_succession", "Succession", RiteTag, 2,
        "Gain 1 Strength and 5 Block. Once each turn, when you keep a Challenge, Challenge another enemy: hit it with a Deed " +
        "before it acts (1 Authority). If there is none, draw 1 more card next turn.", SuccessionOut(1), 3);

    private static CombatNodeModel HiltKnotOut(int block) => Seq(Apply(Strength, 1, You), Apply(LieutenantRules.HiltKnot, block, You));

    private static readonly BnbCard HiltKnot = Rare("lt_hilt_knot", "Hilt Knot", RiteTag, 1,
        "Gain 1 Strength. The first time each turn you spend Strength on a card, draw 1 card and gain 5 Block.", HiltKnotOut(5), 3);

    // The ward is set as the card is played: 4 and 2 (or 3) for each Strength she has then.
    private static CombatNodeModel PikeSquareOut(int block, int perStrength) => Seq(Block(block),
        Apply(LieutenantRules.PikeSquare, Plus(CombatAmountSpec.FromConst(4), Times(Stacks(Strength, You), perStrength)), You));

    private static readonly BnbCard PikeSquare = Rare("lt_pike_square", "Pike Square", WorkingTag, 2,
        "Gain 12 Block. This enemy phase, the first hit of each enemy action on you deals 4 less, and 2 less for each Strength " +
        "you have now; that enemy takes as much back (at most 12).", PikeSquareOut(12, 2), 3);

    private static CombatNodeModel RisingStarOut(int authority) => Seq(GainAuthority(authority), Apply(LieutenantRules.RisingStar, 1, You));

    private static readonly BnbCard RisingStar = Rare("lt_rising_star", "Rising Star", RiteTag, 1,
        "Gain 1 Authority. The first time each turn spending Authority leaves you with exactly 0, gain 1 Strength and 4 Block.",
        RisingStarOut(1), 3);

    private static CombatNodeModel WatchwordOut(int block, int authority) => Seq(Block(block),
        SetCounter(LieutenantRules.WatchwordOrders, MyCounter(Obligations.OrdersKept)),
        SetCounter(LieutenantRules.WatchwordChallenges, MyCounter(Obligations.ChallengesWon)),
        new CombatNodeModel("removeStatus", You, StatusId: LieutenantRules.Watchword),
        Apply(LieutenantRules.Watchword, authority, You));

    private static readonly BnbCard Watchword = Rare("lt_watchword", "Watchword", WorkingTag, 0,
        "Gain 5 Block. If you keep both an Order and a Challenge before your next turn, it begins with 1 Strength, 1 Authority " +
        "and a card more.", WatchwordOut(5, 1), 3);

    // ── Act IV ───────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel ArmsmasterOut(int strength) =>
        Seq(Apply(Strength, strength, You), Block(8), Apply(LieutenantRules.Armsmaster, 1, You));

    private static readonly BnbCard Armsmaster = Rare("lt_armsmaster", "Armsmaster", RiteTag, 2,
        "Gain 1 Strength and 8 Block. At the start of your turn, choose: gain 1 Strength, or your next payment of 2 or more " +
        "Strength this turn costs 1 less.", ArmsmasterOut(1), 4);

    // 0, 1 or 2 more Energy for as many more blows, each with 1 Open Guard.
    private static CombatNodeModel FullTiltHit(int damage)
    {
        CombatConditionSpec HasEnergy(int n) => new("compare", You, ValueKind: "currentResource", Op: ComparisonOperator.GreaterOrEqual,
            Right: n, Id: StandardCombatIds.EnergyResource.value);
        CombatNodeModel More(int n) => Seq(
            new CombatNodeModel("loseResource", You, CombatAmountSpec.FromConst(n), StandardCombatIds.EnergyResource.value),
            Repeat(n, Seq(If(new CombatConditionSpec("isAlive", Target), Damage(damage)), Apply(OpenGuard, 1, You))));
        var two = CombatNodeModel.ChooseOptions(1, ["2 more Energy: two more blows", "1 more Energy: one more blow", "No more"],
            [More(2), More(1), Seq()], "Full Tilt");
        var one = CombatNodeModel.ChooseOptions(1, ["1 more Energy: one more blow", "No more"], [More(1), Seq()], "Full Tilt");
        return Seq(Damage(damage), If(new CombatConditionSpec("isAlive", Target), If(HasEnergy(2), two, If(HasEnergy(1), one))));
    }

    private static readonly BnbCard FullTilt = Rare("lt_full_tilt", "Full Tilt", DeedTag, 1,
        "Deal 9 damage. You may spend 1 or 2 more Energy: for each, deal 9 damage again and gain 1 Open Guard.", FullTiltHit(9), 4);

    private static CombatNodeModel BloodDebtOut(int block, int most) => Seq(Block(block), Apply(LieutenantRules.BloodDebt, most, You));

    private static readonly BnbCard BloodDebt = Rare("lt_blood_debt", "Blood Debt", WorkingTag, 1,
        "Gain 6 Block. The first time next enemy phase Open Guard costs you HP, the enemy that struck takes that much at the " +
        "start of your turn (at most 18).", BloodDebtOut(6, 18), 4);

    private static readonly BnbCard WatchSurgeon = Rare("lt_watch_surgeon", "Watch Surgeon", RiteTag, 1,
        "Gain 8 Block. The first time each turn you keep an Order or a Challenge, spend 1 Authority to heal 2 HP, if you are hurt.",
        Seq(Block(8), Apply(LieutenantRules.WatchSurgeon, 2, You)), 4);

    private static CombatNodeModel FieldPardonOut(int block) => Seq(Block(block), GainAuthority(1),
        MaySpendAuthority(3, "a standing Order or Challenge is kept", Apply(Obligations.PardonPulse, 1, You)));

    private static readonly BnbCard FieldPardon = Rare("lt_field_pardon", "Field Pardon", WorkingTag, 1,
        "Gain 10 Block and 1 Authority. You may spend 3 Authority: one Order or Challenge you already had is kept. Exhaust.",
        FieldPardonOut(10), 4, [ExhaustTag]);

    public static IReadOnlyList<BnbCard> Rares() =>
    [
        Brevet, Brevet.Upgraded("Retain. Gain 1 Authority and 8 Block. The next Authority you spend this turn costs 1 less. Exhaust.", BrevetOut(1, 8),
            retainInHand: true),
        Insubordination, Insubordination.Upgraded("Deal 15 damage. You may abandon your oldest Order to deal 15 damage again and gain 1 Open Guard.",
            InsubordinationHit(15)),
        ParadeGround, ParadeGround.Upgraded("Gain 12 Block. If you keep an Order before this turn ends, gain 5 Ward Wax for each Energy you end it with (at most 3).",
            ParadeGroundOut(12, most: 3)),
        DrumMajor, DrumMajor.Upgraded("Gain 9 Block and draw 1 card. The first Order you keep before your next turn gives 1 Energy and draws 1 card.",
            DrumMajorOut(9, stacks: 2)),
        ServiceRecord, ServiceRecord.Upgraded("Deal 10 damage, plus 4 for each Challenge you have kept this combat (at most 4). If this kills the target, gain 1 Authority.",
            Seq(ServiceRecordHit(10), If(new CombatConditionSpec("isAlive", Target), Seq(), GainAuthority(1)))),
        SplinteredShield, SplinteredShield.Upgraded("Gain 16 Block. You may lose all your Block to gain 1 Strength, 1 Authority and 2 Open Guard, then 4 Block.",
            SplinteredShieldOut(16, 4)),
        LastDitch, LastDitch.Upgraded("Deal 18 damage, plus 1 for every 3 HP you are missing (at most 16). You may gain 1 Open Guard to double that bonus.",
            LastDitchHit(18, 16)),
        OpenVisor, OpenVisor.Upgraded("Gain 11 Block. If you have no Open Guard, you may gain 1. With Open Guard, your next Deed this turn ignores Block.",
            OpenVisorOut(11)),
        BlackGauntlet, BlackGauntlet.Upgraded($"Deal 16 damage. {Promises.BlackGauntlet.Text}", Seq(Damage(16), Issue(Promises.BlackGauntlet))),
        Redress, Redress.Upgraded("Gain 15 Block. You may move your Challenge to the target, if it fits it; its count starts again, its deadline stays. " +
            "Once per Challenge; not Pursuit.", RedressOut(15)),
        Riposte, Riposte.Upgraded("Gain 6 Block. Once each enemy phase, when a Challenged enemy has attacked you, deal it 8 damage plus your Strength.",
            Seq(Block(6), Apply(LieutenantRules.Riposte, 8, You))),
        Countercharge, Countercharge.Upgraded("Deal 11 damage. Then spend up to 2 Strength: for each, apply 1 Doubt and gain 1 Authority and 3 Block.",
            CounterchargeHit(11, 2, block: 3)),
        ForcedMarch, ForcedMarch.Upgraded("Gain 4 Strength and 1 Open Guard. At the end of this turn, lose up to 4 of the Strength you still have.",
            ForcedMarchOut(4)),
        Reprimand, Reprimand.Upgraded("Gain 13 Block. If an Order failed at its deadline last turn, gain 3 Strength and 1 Open Guard.", ReprimandOut(13, 3)),
        ChainOfCommand, ChainOfCommand.Upgraded("Gain 10 Block. You may hold two Orders. If you keep two Orders in a turn, draw 3 more cards next turn.",
            ChainOut(10, 3)),
        Succession, Succession.Upgraded("Gain 2 Strength and 5 Block. Once each turn, when you keep a Challenge, Challenge another enemy: hit it with a Deed " +
            "before it acts (1 Authority). If there is none, draw 1 more card next turn.", SuccessionOut(2)),
        HiltKnot, HiltKnot.Upgraded("Gain 1 Strength. The first time each turn you spend Strength on a card, draw 1 card and gain 8 Block.", HiltKnotOut(8)),
        PikeSquare, PikeSquare.Upgraded("Gain 15 Block. This enemy phase, the first hit of each enemy action on you deals 4 less, and 3 less for each Strength " +
            "you have now; that enemy takes as much back (at most 12).", PikeSquareOut(15, 3)),
        RisingStar, RisingStar.Upgraded("Gain 2 Authority. The first time each turn spending Authority leaves you with exactly 0, gain 1 Strength and 4 Block.",
            RisingStarOut(2)),
        Watchword, Watchword.Upgraded("Gain 8 Block. If you keep both an Order and a Challenge before your next turn, it begins with 1 Strength, 2 Authority " +
            "and a card more.", WatchwordOut(8, 2)),
        Armsmaster, Armsmaster.Upgraded("Gain 2 Strength and 8 Block. At the start of your turn, choose: gain 1 Strength, or your next payment of 2 or more " +
            "Strength this turn costs 1 less.", ArmsmasterOut(2)),
        FullTilt, FullTilt.Upgraded("Deal 13 damage. You may spend 1 or 2 more Energy: for each, deal 13 damage again and gain 1 Open Guard.", FullTiltHit(13)),
        BloodDebt, BloodDebt.Upgraded("Gain 9 Block. The first time next enemy phase Open Guard costs you HP, the enemy that struck takes that much at the " +
            "start of your turn (at most 24).", BloodDebtOut(9, 24)),
        WatchSurgeon, WatchSurgeon.Upgraded("Gain 8 Block. The first time each turn you keep an Order or a Challenge, spend 1 Authority to heal 3 HP, if you are hurt.",
            Seq(Block(8), Apply(LieutenantRules.WatchSurgeon, 3, You))),
        FieldPardon, FieldPardon.Upgraded("Gain 15 Block and 1 Authority. You may spend 3 Authority: one Order or Challenge you already had is kept. Exhaust.",
            FieldPardonOut(15)),
    ];
}
