using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.ApprenticeKeywords;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Apprentice;

// What the expansion's lasting cards and relics put on him (sorcerers_apprentice_expansion.md §3–§4): a status per
// rule, each stack one more copy of it. They listen to his turn's start and end, to the fight's opening draw, and —
// what the wand itself raises — to each settled cast (TriggerEvent.WandCast).
public static partial class ApprenticeRules
{
    // ── cards ────────────────────────────────────────────────────────────────────────────────────────────────
    public const string CopybookRule = "copybook_rule";
    public const string GrimoireRule = "borrowed_grimoire_rule";
    public const string ExaminersBellRule = "examiners_bell_rule";
    public const string BurningGlassRule = "burning_glass_rule";
    public const string MastersHatRule = "masters_hat_rule";
    public const string OathRule = "apprentices_oath_rule";

    // ── relics ───────────────────────────────────────────────────────────────────────────────────────────────
    public const string PencilBox = "pencil_box_rule";
    public const string WireSpectacles = "wire_spectacles_rule";
    public const string SandGlass = "sand_glass_rule";
    public const string PewterInkwell = "pewter_inkwell_rule";
    public const string StarChart = "star_chart_rule";
    public const string TutorsCane = "tutors_cane_rule";
    public const string SpareWicks = "spare_wicks_rule";
    public const string SlateBoard = "slate_board_rule";
    public const string OilLamp = "oil_lamp_rule";

    private static CombatAmountSpec Each(string rule, int per = 1) =>
        per == 1 ? Stacks(rule, You) : Times(Stacks(rule, You), per);

    private static StatusTriggerData On<TContext>(string trigger, CombatNodeModel body) where TContext : class =>
        Trigger(CombatProgramModel.Build<TContext>(body), trigger);

    // Once a fight, at its opening draw — how a relic says "at the start of each combat".
    private static CombatNodeModel FirstDraw(string rule, CombatNodeModel body)
    {
        var done = rule + "_done";
        return If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Equal, Right: 0, Id: done),
            Seq(body, new CombatNodeModel("setCombatantCounter", You, CombatAmountSpec.FromConst(1), CounterId: done, Relative: false)));
    }

    // Once a turn: a latch that the turn's start opens again.
    private static CombatNodeModel OnceATurn(string rule, CombatNodeModel body)
    {
        var done = rule + "_turn";
        return If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: ComparisonOperator.Equal, Right: 0, Id: done),
            Seq(body, new CombatNodeModel("setCombatantCounter", You, CombatAmountSpec.FromConst(1), CounterId: done, Relative: false)));
    }

    private static CombatNodeModel OpenTheLatch(string rule) =>
        new("setCombatantCounter", You, CombatAmountSpec.FromConst(0), CounterId: rule + "_turn", Relative: false);

    private static CombatNodeModel GainFocus(CombatAmountSpec amount) =>
        new("wand", You, amount, WandMove: nameof(WandAction.Focus));

    private static IReadOnlyList<StatusData> Growth() =>
    [
        Status(CopybookRule, "Copybook", StatusPolarity.Buff,
            "At the start of each of your turns, gain 1 Rote and 1 Poise for each.",
            [On<TurnStartedTriggeredEffectContext>(nameof(TriggerEvent.TurnStarted),
                Seq(Apply(Rote, Each(CopybookRule), You), Apply(Poise, Each(CopybookRule), You)))]),
        Status(GrimoireRule, "The Borrowed Grimoire", StatusPolarity.Buff,
            "Whenever you cast, gain 1 Rote for each.",
            [On<WandCastTriggeredEffectContext>(nameof(TriggerEvent.WandCast), Apply(Rote, Each(GrimoireRule), You))]),
        Status(ExaminersBellRule, "The Examiner's Bell", StatusPolarity.Buff,
            "At the end of each of your turns in which you cast twice or more, gain 2 Rote for each.",
            [On<TurnEndedTriggeredEffectContext>(nameof(TriggerEvent.TurnEnded),
                ApprenticeCards.IfAtLeast(ApprenticeCards.WandValue(WandField.CastsThisTurn), 2,
                    Apply(Rote, Each(ExaminersBellRule, 2), You)))]),
        Status(BurningGlassRule, "Burning Glass", StatusPolarity.Buff,
            "Whenever you cast, apply 1 Singe to ALL enemies for each.",
            [On<WandCastTriggeredEffectContext>(nameof(TriggerEvent.WandCast), Apply(Singe, Each(BurningGlassRule), AllEnemies))]),
        Status(MastersHatRule, "The Master's Hat", StatusPolarity.Buff,
            "At the start of each of your turns, gain 1 Focus and 1 Rote for each.",
            [On<TurnStartedTriggeredEffectContext>(nameof(TriggerEvent.TurnStarted),
                Seq(GainFocus(Each(MastersHatRule)), Apply(Rote, Each(MastersHatRule), You)))]),
        Status(OathRule, "The Apprentice's Oath", StatusPolarity.Buff,
            "Whenever you cast, deal 3 damage to ALL enemies and gain 3 Block, for each.",
            [On<WandCastTriggeredEffectContext>(nameof(TriggerEvent.WandCast),
                Seq(Damage(Each(OathRule, 3), AllEnemies), Block(Each(OathRule, 3))))]),

        Status(PencilBox, "Pencil Box", StatusPolarity.Neutral,
            "At the start of each combat, gain 1 Rote.",
            [On<CardsDrawnTriggeredEffectContext>(nameof(TriggerEvent.CardsDrawn), FirstDraw(PencilBox, Apply(Rote, 1, You)))]),
        Status(WireSpectacles, "Wire Spectacles", StatusPolarity.Neutral,
            "At the start of each combat, gain 1 Poise and 1 Focus.",
            [On<CardsDrawnTriggeredEffectContext>(nameof(TriggerEvent.CardsDrawn),
                FirstDraw(WireSpectacles, Seq(Apply(Poise, 1, You), GainFocus(CombatAmountSpec.FromConst(1)))))]),
        Status(SandGlass, "Sand Glass", StatusPolarity.Neutral,
            "At the start of your turn, if the wand holds 3 or more cards, gain 1 Focus.",
            [On<TurnStartedTriggeredEffectContext>(nameof(TriggerEvent.TurnStarted),
                ApprenticeCards.IfAtLeast(ApprenticeCards.WandValue(WandField.Occupied), 3, GainFocus(CombatAmountSpec.FromConst(1))))]),
        Status(PewterInkwell, "Pewter Inkwell", StatusPolarity.Neutral,
            "Whenever you cast, gain 2 Block.",
            [On<WandCastTriggeredEffectContext>(nameof(TriggerEvent.WandCast), Block(2))]),
        Status(StarChart, "Star Chart", StatusPolarity.Neutral,
            "The first time you cast each turn, gain 1 Rote.",
            [
                On<WandCastTriggeredEffectContext>(nameof(TriggerEvent.WandCast), OnceATurn(StarChart, Apply(Rote, 1, You))),
                On<TurnStartedTriggeredEffectContext>(nameof(TriggerEvent.TurnStarted), OpenTheLatch(StarChart)),
            ]),
        Status(TutorsCane, "Tutor's Cane", StatusPolarity.Neutral,
            "The wand starts each combat with one more slot.",
            [On<CardsDrawnTriggeredEffectContext>(nameof(TriggerEvent.CardsDrawn),
                FirstDraw(TutorsCane, new CombatNodeModel("wand", You, CombatAmountSpec.FromConst(1), WandMove: nameof(WandAction.Extend))))]),
        Status(SpareWicks, "Spare Wicks", StatusPolarity.Neutral,
            "At the start of each combat, apply 2 Singe to ALL enemies.",
            [On<CardsDrawnTriggeredEffectContext>(nameof(TriggerEvent.CardsDrawn), FirstDraw(SpareWicks, Apply(Singe, 2, AllEnemies)))]),
        Status(SlateBoard, "Slate Board", StatusPolarity.Neutral,
            "Whenever you inscribe a card, gain 2 Block.",
            [Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    new TriggerEventCardZoneExpression<CardMovedToZoneTriggeredEffectContext>(CardZone.Wand),
                    new GainBlockNode<CardMovedToZoneTriggeredEffectContext>(CombatantTargetSelectors.Source,
                        new ConstantExpression<CardMovedToZoneTriggeredEffectContext>(2)))),
                nameof(TriggerEvent.CardMovedToZone))]),
        Status(OilLamp, "Oil Lamp", StatusPolarity.Neutral,
            "At the end of your turn, if you did not cast, gain 1 Focus.",
            [On<TurnEndedTriggeredEffectContext>(nameof(TriggerEvent.TurnEnded),
                ApprenticeCards.IfAtLeast(
                    CombatAmountSpec.Binary("sub", CombatAmountSpec.FromConst(1), Once(ApprenticeCards.WandValue(WandField.CastsThisTurn))),
                    1, GainFocus(CombatAmountSpec.FromConst(1))))]),
    ];
}
