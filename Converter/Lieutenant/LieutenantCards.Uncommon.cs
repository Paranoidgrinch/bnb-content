using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Lieutenant.CardShapes;

namespace BnbContent.Converter.Lieutenant;

// The thirty-five uncommons (§3.3): the harder Orders and Challenges, the first Rites, and the cards that spend
// Authority and Strength for something. 23 from Act I, 12 — the spenders and the engines — from Act II (plan L2).
public static partial class LieutenantCards
{
    private static BnbCard Uncommon(string id, string name, string type, int cost, string text, CombatNodeModel program,
        int act = 1, IReadOnlyList<string>? tags = null) => Card(id, name, type, cost, text, program, "uncommon", act, tags);

    private static CombatNodeModel Rite(string status, int stacks = 1) => Apply(status, stacks, You);

    // ── Orders ───────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard AgainstTheWall = Uncommon("lt_against_the_wall", "Against the Wall", WorkingTag, 1,
        $"Gain 7 Block. {Promises.AgainstTheWall.Text}", Seq(Block(7), Issue(Promises.AgainstTheWall)));

    private static CombatNodeModel DoubleFileOut(int block) => Seq(
        Draw(1), Block(block),
        CombatNodeModel.ForEach(AllEnemies, new CombatNodeModel("removeStatus", "iterationTarget", StatusId: Obligations.DoubleFileMark)),
        Issue(Promises.DoubleFile));

    private static readonly BnbCard DoubleFile = Uncommon("lt_double_file", "Double File", WorkingTag, 1,
        $"Draw 1 card. Gain 4 Block. {Promises.DoubleFile.Text}", DoubleFileOut(4));

    private static readonly BnbCard MindTheGap = Uncommon("lt_mind_the_gap", "Mind the Gap", DeedTag, 1,
        $"Deal 8 damage. {Promises.MindTheGap.Text}", Seq(Damage(8), Issue(Promises.MindTheGap)));

    private static readonly BnbCard Cadence = Uncommon("lt_cadence", "Cadence", WorkingTag, 0,
        $"Draw 1 card. {Promises.Cadence.Text}", Seq(Draw(1), Issue(Promises.Cadence)));

    private static readonly BnbCard NoExcuses = Uncommon("lt_no_excuses", "No Excuses", DeedTag, 1,
        $"Deal 8 damage. {Promises.NoExcuses.Text}", Seq(Damage(8), Issue(Promises.NoExcuses)));

    private static readonly BnbCard NoSurrender = Uncommon("lt_no_surrender", "No Surrender", WorkingTag, 1,
        $"Gain 5 Block. {Promises.NoSurrender.Text}", Seq(Block(5), Issue(Promises.NoSurrender)));

    // ── Challenges ───────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard Gauntlet = Uncommon("lt_gauntlet", "Gauntlet", DeedTag, 1,
        $"Deal 7 damage. {Promises.Gauntlet.Text}", Seq(Damage(7), Issue(Promises.Gauntlet)));

    private static CombatConditionSpec IntendsToAttack => new("intends", Target, Id: nameof(IntentKind.Attack));

    private static readonly BnbCard OnYourFeet = Uncommon("lt_on_your_feet", "On Your Feet", WorkingTag, 1,
        $"Gain 8 Block. If the target intends to attack: {Promises.OnYourFeet.Text}",
        Seq(Block(8), If(IntendsToAttack, Issue(Promises.OnYourFeet))));

    private static readonly BnbCard Pursuit = Uncommon("lt_pursuit", "Pursuit", WorkingTag, 1,
        $"Draw 1 card. {Promises.Pursuit.Text}", Seq(Draw(1), Issue(Promises.Pursuit)));

    // The canon's fix: whether it has Block is asked before the blow, not after.
    private static CombatNodeModel NoRunningHit(int damage) => Seq(
        SetScratch(new CombatAmountSpec("defensivePool", SelectorKey: Target, ReadId: StandardCombatIds.BlockDefensivePool.value)),
        Damage(damage),
        If(ScratchAtLeast(1), Issue(Promises.NoRunning)));

    private static readonly BnbCard NoRunning = Uncommon("lt_no_running", "No Running", DeedTag, 1,
        $"Deal 9 damage. If the target had Block: {Promises.NoRunning.Text}", NoRunningHit(9));

    private static CombatNodeModel DareOut(int block) =>
        Seq(Block(block), Apply(OpenGuard, 1, You), If(IntendsToAttack, Issue(Promises.Dare)));

    private static readonly BnbCard Dare = Uncommon("lt_dare", "Dare", WorkingTag, 0,
        $"Gain 6 Block and 1 Open Guard. If the target intends to attack: {Promises.Dare.Text}", DareOut(6));

    private static readonly BnbCard SecondBlade = Uncommon("lt_second_blade", "Second Blade", DeedTag, 1,
        $"Deal 5 damage twice. {Promises.SecondBlade.Text}", Seq(Repeat(2, Damage(5)), Issue(Promises.SecondBlade)));

    private static readonly BnbCard Testimony = Uncommon("lt_testimony", "Testimony", WorkingTag, 1,
        $"Gain 6 Block. {Promises.Testimony.Text}", Seq(Block(6), Issue(Promises.Testimony)));

    // ── spending ─────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel WhistleOut(int block) => Seq(Block(block), MaySpendAuthority(1, "draw 2 cards", Draw(2)));

    private static readonly BnbCard Whistle = Uncommon("lt_whistle", "Whistle", WorkingTag, 0,
        "Gain 3 Block. You may spend 1 Authority to draw 2 cards.", WhistleOut(3));

    // A Deed back from the discard pile: the offer leaves out Workings, which is every Deed and nothing else she
    // plays (Rites exhaust).
    private static CombatNodeModel DispatchOut(int block) => Seq(Block(block),
        SetScratch(new CombatAmountSpec("zoneCards", SelectorKey: You, Zone: CardZone.DiscardPile)),
        If(ScratchAtLeast(1), MaySpendAuthority(2, "return a Deed from your discard pile to your hand",
            new CombatNodeModel("moveCardToZone", You,
                Card: new CombatCardSpec("chosen", CardZone.DiscardPile, Purpose: "choose a Deed to return", ExcludeTag: WorkingTag),
                ToZone: CardZone.Hand))));

    private static readonly BnbCard Dispatch = Uncommon("lt_dispatch", "Dispatch", WorkingTag, 1,
        "Gain 6 Block. You may spend 2 Authority to return a Deed from your discard pile to your hand.", DispatchOut(6), act: 2);

    // Half the HP the blow really took, to every other enemy, as damage that is no Deed's (§2.1).
    private static CombatNodeModel MakeAnExampleHit(int damage, bool free = false) => Seq(
        SetScratch(new CombatAmountSpec("currentHealth", SelectorKey: Target)),
        Damage(damage),
        SetScratch(CombatAmountSpec.Binary("sub", new CombatAmountSpec("counter", SelectorKey: You, CounterId: Scratch),
            new CombatAmountSpec("currentHealth", SelectorKey: Target))),
        new CombatNodeModel("setCombatantCounter", You,
            new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec("otherEnemies")), CounterId: Others),
        If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Greater, Right: 0, Id: Others),
            free ? Spread() : MaySpendAuthority(1, "half the HP it lost to every other enemy", Spread())));

    private static CombatNodeModel Spread() =>
        new("dealDamage", "otherEnemies",
            CombatAmountSpec.Binary("div", new CombatAmountSpec("counter", SelectorKey: You, CounterId: Scratch), CombatAmountSpec.FromConst(2)),
            DamageKind: DamageKind.DamageOverTime);

    private const string Others = "lt_scratch_others";

    private static readonly BnbCard MakeAnExample = Uncommon("lt_make_an_example", "Make an Example", DeedTag, 2,
        "Deal 14 damage. You may spend 1 Authority: every other enemy takes half the HP it lost.", MakeAnExampleHit(14), act: 2);

    private static CombatNodeModel FlankHit(int damage, int other, bool free = false)
    {
        var second = CombatNodeModel.RandomTargets("otherEnemies", CombatAmountSpec.FromConst(1), Damage(other, "iterationTarget"));
        return Seq(Damage(damage),
            SetScratch(new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec("otherEnemies"))),
            If(ScratchAtLeast(1), free ? second : MaySpendAuthority(1, $"deal {other} damage to another enemy", second)));
    }

    private static readonly BnbCard Flank = Uncommon("lt_flank", "Flank", DeedTag, 1,
        "Deal 9 damage. You may spend 1 Authority to deal 7 damage to another enemy.", FlankHit(9, 7));

    private static CombatNodeModel UltimatumHit(int damage, int price = 2) =>
        Seq(Damage(damage), MaySpendAuthority(price, "apply 2 Doubt", Apply(Cards.Keywords.Doubt, 2)));

    private static readonly BnbCard Ultimatum = Uncommon("lt_ultimatum", "Ultimatum", DeedTag, 1,
        "Deal 8 damage. You may spend 2 Authority to apply 2 Doubt.", UltimatumHit(8), act: 2);

    // "If you have a Challenge out" — any enemy carries one.
    private static CombatNodeModel IfChallengeOut(CombatNodeModel then)
    {
        CombatAmountSpec? total = null;
        foreach (var c in Promises.Challenges)
        {
            var one = new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec("withStatus", c.Id,
                [new CombatSelectorSpec("allCombatants")]));
            total = total is null ? one : Plus(total, one);
        }
        return Seq(SetScratch(total!), If(ScratchAtLeast(1), then));
    }

    private static readonly BnbCard Remand = Uncommon("lt_remand", "Remand", WorkingTag, 1,
        "Gain 7 Block. You may spend 1 Authority: your Challenge survives one more deadline (once per Challenge).",
        Seq(Block(7), IfChallengeOut(MaySpendAuthority(1, "extend your Challenge", Apply(Obligations.RemandPulse, 1, You)))), act: 2);

    private static CombatNodeModel AbandonChallenges() => CombatNodeModel.ForEach(AllEnemies,
        Seq([.. Promises.Challenges.Select(c => new CombatNodeModel("removeStatus", "iterationTarget", StatusId: c.Id))]));

    private static CombatNodeModel StandDownOut(int block) => Seq(Block(block),
        IfChallengeOut(MaySpendAuthority(1, "withdraw your Challenge and draw 3 cards", Seq(AbandonChallenges(), Draw(3)))));

    private static readonly BnbCard StandDown = Uncommon("lt_stand_down", "Stand Down", WorkingTag, 0,
        "Gain 4 Block. You may spend 1 Authority to withdraw your Challenge and draw 3 cards.", StandDownOut(4), act: 2);

    private static CombatNodeModel ArmsDrillHit(int damage) =>
        Seq(Repeat(2, Damage(damage)), MaySpendStrength(1, $"deal {damage} damage a third time", Damage(damage)));

    private static readonly BnbCard ArmsDrill = Uncommon("lt_arms_drill", "Arms Drill", DeedTag, 1,
        "Deal 4 damage twice. You may spend 1 Strength to deal 4 damage a third time.", ArmsDrillHit(4));

    // "Ignores up to 10 of its Block": the Block is set aside for the blow and what the blow left of it returns.
    private static CombatNodeModel OverhandHit(int damage, int strength = 3)
    {
        var block = StandardCombatIds.BlockDefensivePool.value;
        var setAside = new CombatAmountSpec("counter", SelectorKey: You, CounterId: Scratch);
        return If(Has(Strength, strength),
            Seq(
                SetScratch(AtMost(new CombatAmountSpec("defensivePool", SelectorKey: Target, ReadId: block), 10)),
                new CombatNodeModel("modifyDefensivePool", Target, CombatAmountSpec.Unary("neg", setAside), PoolId: block),
                Damage(damage),
                If(new CombatConditionSpec("isAlive", Target),
                    new CombatNodeModel("modifyDefensivePool", Target, setAside, PoolId: block))),
            Damage(damage));
    }

    private static readonly BnbCard Overhand = Uncommon("lt_overhand", "Overhand", DeedTag, 2,
        "Deal 14 damage. With 3 or more Strength, this ignores up to 10 of the target's Block.", OverhandHit(14), act: 2);

    private static CombatNodeModel HeaveHit(int damage, int price = 2) => Seq(Damage(damage),
        If(new CombatConditionSpec("isAlive", Target), MaySpendStrength(price, $"deal {damage} damage again", Damage(damage))));

    private static readonly BnbCard Heave = Uncommon("lt_heave", "Heave", DeedTag, 1,
        "Deal 8 damage. Then you may spend 2 Strength to deal 8 damage again.", HeaveHit(8), act: 2);

    private static CombatNodeModel GripOut(int perUnit, int none)
    {
        CombatNodeModel Pay(int n) => Seq(PayDeclared(n), Draw(n), Block(perUnit * n));
        var two = CombatNodeModel.ChooseOptions(1,
            [$"Spend 2 Strength: draw 2, gain {2 * perUnit} Block", $"Spend 1 Strength: draw 1, gain {perUnit} Block", $"Keep it: gain {none} Block"],
            [Pay(2), Pay(1), Block(none)], "Grip");
        var one = CombatNodeModel.ChooseOptions(1,
            [$"Spend 1 Strength: draw 1, gain {perUnit} Block", $"Keep it: gain {none} Block"], [Pay(1), Block(none)], "Grip");
        return If(Has(Strength, 2), two, If(Has(Strength, 1), one, Block(none)));
    }

    private static readonly BnbCard Grip = Uncommon("lt_grip", "Grip", WorkingTag, 0,
        "Spend up to 2 Strength: for each, draw 1 card and gain 4 Block. If you spend none, gain 2 Block.", GripOut(4, 2), act: 2);

    private static CombatNodeModel ShieldPunchHit(int damage, int block) =>
        Seq(Damage(damage), Block(Plus(CombatAmountSpec.FromConst(block), Stacks(Strength, You))));

    private static readonly BnbCard ShieldPunch = Uncommon("lt_shield_punch", "Shield Punch", DeedTag, 1,
        "Deal 5 damage. Gain 4 Block, plus 1 for each Strength.", ShieldPunchHit(5, 4));

    private static CombatNodeModel BrokenPikeHit(int damage, int splash, int price = 2) => Seq(Damage(damage),
        MaySpendStrength(price, $"{splash} damage to every other enemy",
            new CombatNodeModel("dealDamage", "otherEnemies", CombatAmountSpec.FromConst(splash), DamageKind: DamageKind.DamageOverTime)));

    private static readonly BnbCard BrokenPike = Uncommon("lt_broken_pike", "Broken Pike", DeedTag, 2,
        "Deal 12 damage. Then you may spend 2 Strength: every other enemy takes 7 damage.", BrokenPikeHit(12, 7), act: 2);

    // ── exposure ─────────────────────────────────────────────────────────────────────────────────────────

    private static CombatNodeModel FrontRankHit(int damage, int more) =>
        Seq(If(Has(OpenGuard), Damage(damage + more), Damage(damage)), Apply(OpenGuard, 1, You));

    private static readonly BnbCard FrontRank = Uncommon("lt_front_rank", "Front Rank", DeedTag, 1,
        "Deal 11 damage, 4 more if you have Open Guard. Then gain 1 Open Guard.", FrontRankHit(11, 4));

    private static readonly BnbCard Recoil = Uncommon("lt_recoil", "Recoil", WorkingTag, 1,
        "Gain 8 Block. If you go into the enemy phase with Open Guard and an enemy Attack takes your HP, draw 2 more cards next turn.",
        Seq(Block(8), Apply(LieutenantRules.Recoil, 1, You)), act: 2);

    // "If at least two different enemies are hit": the enemies standing when it lands.
    private static CombatNodeModel RuckusHit(int damage) => Seq(
        SetScratch(new CombatAmountSpec("countTargets", ReadSelector: new CombatSelectorSpec(AllEnemies))),
        Damage(damage, AllEnemies),
        If(ScratchAtLeast(2), Seq(Apply(OpenGuard, 1, You), Draw(1))));

    private static readonly BnbCard Ruckus = Uncommon("lt_ruckus", "Ruckus", DeedTag, 0,
        "Deal 3 damage to ALL enemies. If it hits two or more, gain 1 Open Guard and draw 1 card.", RuckusHit(3));

    private static CombatNodeModel PoiseOut(int block) =>
        Seq(Block(block), MaySpendAuthority(1, "no Open Guard until your next Deed has resolved", Apply(LieutenantRules.Poise, 1, You)));

    private static readonly BnbCard Poise = Uncommon("lt_poise", "Poise", WorkingTag, 1,
        "Gain 10 Block. You may spend 1 Authority: until your next Deed this turn has resolved, you gain no Open Guard.", PoiseOut(10), act: 2);

    private static CombatConditionSpec HurtInLastPhase =>
        new("compare", You, ValueKind: "counter", Op: ComparisonOperator.Greater, Right: 0, Id: LieutenantKeywords.PhaseHp);

    private static CombatNodeModel BloodyCobblesHit(int damage) =>
        Seq(If(HurtInLastPhase, Repeat(2, Damage(damage)), Damage(damage)), Apply(OpenGuard, 1, You));

    private static readonly BnbCard BloodyCobbles = Uncommon("lt_bloody_cobbles", "Bloody Cobbles", DeedTag, 1,
        "Deal 7 damage; twice if an enemy Attack took your HP last enemy phase. Gain 1 Open Guard.", BloodyCobblesHit(7));

    // ── Rites ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard UnderTheColours = Uncommon("lt_under_the_colours", "Under the Colours", RiteTag, 1,
        "The first time each turn you keep an Order, gain 1 Strength.", Rite(LieutenantRules.UnderTheColours));

    private static readonly BnbCard Scar = Uncommon("lt_scar", "Scar", RiteTag, 1,
        "The first time each enemy phase an enemy Attack takes your HP, gain 1 Strength.", Rite(LieutenantRules.Scar));

    private static readonly BnbCard Overreach = Uncommon("lt_overreach", "Overreach", RiteTag, 1,
        "The first time each turn you gain Open Guard, your next Deed this turn deals 4 more damage with its first hit.",
        Rite(LieutenantRules.Overreach, 4), act: 2);

    private static readonly BnbCard BrokenNose = Uncommon("lt_broken_nose", "Broken Nose", RiteTag, 1,
        "The first time each enemy phase Open Guard costs you HP, gain 1 Authority and 1 Strength.", Rite(LieutenantRules.BrokenNose));

    public static IReadOnlyList<BnbCard> Uncommons() =>
    [
        AgainstTheWall, AgainstTheWall.Upgraded($"Gain 10 Block. {Promises.AgainstTheWallPlus.Text}", Seq(Block(10), Issue(Promises.AgainstTheWallPlus))),
        DoubleFile, DoubleFile.Upgraded($"Draw 1 card. Gain 6 Block. {Promises.DoubleFile.Text}", DoubleFileOut(6)),
        MindTheGap, MindTheGap.Upgraded($"Deal 11 damage. {Promises.MindTheGapPlus.Text}", Seq(Damage(11), Issue(Promises.MindTheGapPlus))),
        Cadence, Cadence.Upgraded($"Draw 2 cards, then discard 1. {Promises.Cadence.Text}",
            Seq(DrawDiscard(Cadence.Id, 2, 1), Issue(Promises.Cadence))),
        NoExcuses, NoExcuses.Upgraded($"Deal 11 damage. {Promises.NoExcusesPlus.Text}", Seq(Damage(11), Issue(Promises.NoExcusesPlus))),
        NoSurrender, NoSurrender.Upgraded($"Gain 7 Block. {Promises.NoSurrenderPlus.Text}", Seq(Block(7), Issue(Promises.NoSurrenderPlus))),
        Gauntlet, Gauntlet.Upgraded($"Deal 10 damage. {Promises.GauntletPlus.Text}", Seq(Damage(10), Issue(Promises.GauntletPlus))),
        OnYourFeet, OnYourFeet.Upgraded($"Gain 10 Block. If the target intends to attack: {Promises.OnYourFeetPlus.Text}",
            Seq(Block(10), If(IntendsToAttack, Issue(Promises.OnYourFeetPlus)))),
        Pursuit, Pursuit.Upgraded($"Draw 2 cards, then discard 1. {Promises.Pursuit.Text}",
            Seq(DrawDiscard(Pursuit.Id, 2, 1), Issue(Promises.Pursuit))),
        NoRunning, NoRunning.Upgraded($"Deal 13 damage. If the target had Block: {Promises.NoRunning.Text}", NoRunningHit(13)),
        Dare, Dare.Upgraded($"Gain 8 Block and 1 Open Guard. If the target intends to attack: {Promises.DarePlus.Text}",
            Seq(Block(8), Apply(OpenGuard, 1, You), If(IntendsToAttack, Issue(Promises.DarePlus)))),
        SecondBlade, SecondBlade.Upgraded($"Deal 5 damage three times. {Promises.SecondBlade.Text}",
            Seq(Repeat(3, Damage(5)), Issue(Promises.SecondBlade))),
        Testimony, Testimony.Upgraded($"Gain 8 Block. {Promises.TestimonyPlus.Text}", Seq(Block(8), Issue(Promises.TestimonyPlus))),
        Whistle, Whistle.Upgraded("Gain 5 Block. You may spend 1 Authority to draw 2 cards.", WhistleOut(5)),
        Dispatch, Dispatch.Upgraded("Gain 9 Block. You may spend 2 Authority to return a Deed from your discard pile to your hand.",
            DispatchOut(9)),
        MakeAnExample, MakeAnExample.Upgraded("Deal 18 damage. Every other enemy takes half the HP it lost.",
            MakeAnExampleHit(18, free: true)),
        UnderTheColours, UnderTheColours.Upgraded("The first time each turn you keep an Order, gain 1 Strength and 3 Block.",
            Rite(LieutenantRules.UnderTheColoursPlus)),
        Flank, Flank.Upgraded("Deal 12 damage. Deal 8 damage to another enemy.", FlankHit(12, 8, free: true)),
        Ultimatum, Ultimatum.Upgraded("Deal 11 damage. You may spend 1 Authority to apply 2 Doubt.", UltimatumHit(11, price: 1)),
        Remand, Remand.Upgraded("Gain 11 Block. You may spend 1 Authority: your Challenge survives one more deadline (once per Challenge).",
            Seq(Block(11), IfChallengeOut(MaySpendAuthority(1, "extend your Challenge", Apply(Obligations.RemandPulse, 1, You))))),
        StandDown, StandDown.Upgraded("Gain 6 Block. You may spend 1 Authority to withdraw your Challenge and draw 3 cards.", StandDownOut(6)),
        ArmsDrill, ArmsDrill.Upgraded("Deal 5 damage three times.", Repeat(3, Damage(5))),
        Overhand, Overhand.Upgraded("Deal 18 damage. This ignores up to 10 of the target's Block.", OverhandHit(18, strength: 0)),
        Heave, Heave.Upgraded("Deal 10 damage. Then you may spend 1 Strength to deal 10 damage again.", HeaveHit(10, price: 1)),
        Grip, Grip.Upgraded("Spend up to 2 Strength: for each, draw 1 card and gain 6 Block. If you spend none, gain 3 Block.", GripOut(6, 3)),
        ShieldPunch, ShieldPunch.Upgraded("Deal 8 damage. Gain 6 Block, plus 1 for each Strength.", ShieldPunchHit(8, 6)),
        Scar, Scar.Upgraded("Gain 1 Strength. The first time each enemy phase an enemy Attack takes your HP, gain 1 Strength.",
            Seq(Apply(Strength, 1, You), Rite(LieutenantRules.Scar))),
        BrokenPike, BrokenPike.Upgraded("Deal 16 damage. Then you may spend 1 Strength: every other enemy takes 9 damage.", BrokenPikeHit(16, 9, price: 1)),
        FrontRank, FrontRank.Upgraded("Deal 16 damage, 4 more if you have Open Guard. Then gain 1 Open Guard.", FrontRankHit(16, 4)),
        Recoil, Recoil.Upgraded("Gain 12 Block. If you go into the enemy phase with Open Guard and an enemy Attack takes your HP, draw 2 more cards next turn.",
            Seq(Block(12), Apply(LieutenantRules.Recoil, 1, You))),
        Ruckus, Ruckus.Upgraded("Deal 5 damage to ALL enemies. If it hits two or more, gain 1 Open Guard and draw 1 card.", RuckusHit(5)),
        Poise, Poise.Upgraded("Gain 15 Block. You may spend 1 Authority: until your next Deed this turn has resolved, you gain no Open Guard.", PoiseOut(15)),
        Overreach, Overreach.Upgraded("The first time each turn you gain Open Guard, your next Deed this turn deals 6 more damage with its first hit.",
            Rite(LieutenantRules.Overreach, 6)),
        BloodyCobbles, BloodyCobbles.Upgraded("Deal 10 damage; twice if an enemy Attack took your HP last enemy phase. Gain 1 Open Guard.",
            BloodyCobblesHit(10)),
        BrokenNose, BrokenNose.Upgraded("Gain 1 Authority. The first time each enemy phase Open Guard costs you HP, gain 1 Authority and 1 Strength.",
            Seq(GainAuthority(1), Rite(LieutenantRules.BrokenNose))),
    ];
}
