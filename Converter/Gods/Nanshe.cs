using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Gods.GodShapes;

namespace BnbContent.Converter.Gods;

// G3 — NANSHE, LADY OF THE JUST MEASURE, WHO READS THE DREAMS OF KINGS (rework of 2026-11-05).
//
// Every day has a visible portion: 3 Energy and 5 cards. Take From Tomorrow (0, Retain, as often as you like) gives
// 1 Energy and 1 card now and writes 1 Debt. Debt comes off the next day's portion, 1 Energy and 1 card a point, and a
// day never falls below 1 and 1; whatever cannot come off yet stays. At the end of each Distribution (three days)
// the Debt still open is collected, 10 a point, through Block. Her three days are on her tablet before Day I.
//
// The Lean Years (65 %): 4 cards a day, and Energy and draws your own cards make write Debt too; Judgment is 40.
// The Measure Made Equal (30 %): after each of her turns the side with the higher share of its health is lowered
// to the other's.
//
// How a day comes off: each pool loses what the Debt asks of it, down to 1; the points settled are the larger of
// the two takings (with 3 Energy and 5 cards a Debt of 4 makes a day of 1 and 1, and settles all four).
public static class Nanshe
{
    public const string EnemyId = "nanshe_keeper_of_the_just_ration";
    public const string EncounterId = "act_5_nanshe_keeper_of_the_just_ration";

    public const string RuleId = "nanshe_the_just_measure";
    public const string TabletId = "nanshe_her_tablet";
    public const string DayId = "nanshe_day";
    public const string DebtId = "nanshe_debt";
    public const string PortionEnergyId = "nanshe_portion_energy";
    public const string PortionCardsId = "nanshe_portion_cards";
    public const string LeanYearsId = "nanshe_the_lean_years";
    public const string MeasureEqualId = "nanshe_the_measure_made_equal";
    public const string TakeFromTomorrowId = "take_from_tomorrow";

    public const int MaxHealth = 900;
    public const int DaysToADistribution = 3;
    public const int PortionEnergy = 3;
    public const int PortionCards = 5;
    public const int LeanCards = 4;
    public const int DebtCollected = 10;
    public const int LeanPercent = 65;
    public const int EqualPercent = 30;

    public const int CanalsDamage = 22;
    public const int CanalsBlock = 20;
    public const int NetHits = 3;
    public const int NetDamage = 9;
    public const int NetWeak = 1;
    public const int JudgmentDamage = 30;
    public const int LeanJudgmentDamage = 40;
    public const int JudgmentPerDebt = 10;

    // On her: what of today's portion has been handed over and not yet drawn or gained (so the day's own draw and
    // Take From Tomorrow are never counted as made by a card).
    private static CounterId CardCredit => new("nanshe_card_credit");
    private static CounterId EnergyCredit => new("nanshe_energy_credit");

    private static ICombatantTargetSelector Her => Bearer(RuleId);

    private static ICombatExpression<TContext, int> Debt<TContext>() where TContext : class => StacksOn<TContext>(Applicant, DebtId);

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    // The player carries the tablet: it is what lets them read all three of her days before Day I.
    // Day I's portion is the full one (and its credit is written on her first rule, below).
    public static IReadOnlyList<StartingStatusSpec> HeroOpening() =>
    [
        new StartingStatusSpec(new StatusDefinitionId(TabletId), 1),
        new StartingStatusSpec(new StatusDefinitionId(PortionEnergyId), PortionEnergy),
        new StartingStatusSpec(new StatusDefinitionId(PortionCardsId), PortionCards),
    ];

    public static IEnumerable<StatusData> Statuses()
    {
        var her = Her;
        var self = CombatantTargetSelectors.Source;

        // Your turn opens: the day, and the Energy measured out at her dusk (the refill has already run, so what is
        // above the portion is taken back).
        var energyToday = StacksOn<TurnStartedTriggeredEffectContext>(Applicant, PortionEnergyId);
        var energyNow = new CombatantCurrentResourceExpression<TurnStartedTriggeredEffectContext>(Applicant, StandardCombatIds.EnergyResource);
        var morning = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(her), Do(
            SetStacks(her, DayId, Plus(new RemainderExpression<TurnStartedTriggeredEffectContext>(
                Minus(Round<TurnStartedTriggeredEffectContext>(), N<TurnStartedTriggeredEffectContext>(1)),
                N<TurnStartedTriggeredEffectContext>(DaysToADistribution)), N<TurnStartedTriggeredEffectContext>(1))),
            If(Less(energyToday, energyNow),
                new LoseResourceNode<TurnStartedTriggeredEffectContext>(Applicant, StandardCombatIds.EnergyResource, Minus(energyNow, energyToday))),
            Counter(her, EnergyCredit, N<TurnStartedTriggeredEffectContext>(0)),
            // the first morning: Take From Tomorrow, once, to keep
            If(Eq(Round<TurnStartedTriggeredEffectContext>(), 1), Give<TurnStartedTriggeredEffectContext>(TakeFromTomorrowId, CardZone.Hand))));

        // In the Lean Years what your own cards make is written down too: a draw beyond what she handed over, an
        // Energy point beyond it.
        IEffectNode<TContext> Beyond<TContext>(ICombatExpression<TContext, int> amount, CounterId credit) where TContext : class
        {
            var absorbed = Min(amount, CounterOf<TContext>(her, credit));
            return If(PlayersTurnAgainst<TContext>(her), Do(
                If(Wears<TContext>(her, LeanYearsId),
                    If(Above(Minus(amount, absorbed), 0), MarkBy(Applicant, DebtId, Minus(amount, absorbed)))),
                new SetCombatantCounterNode<TContext>(her, credit, new NegateExpression<TContext>(absorbed), relative: true)));
        }
        var drawn = Beyond(new EventAmountExpression<CardsDrawnTriggeredEffectContext>(), CardCredit);
        var gained = Beyond(new EventAmountExpression<ResourceGainedTriggeredEffectContext>(), EnergyCredit);

        // Her turn ends: the third day closes the Distribution and the Debt is collected; then, from 30 %, the
        // measure is made equal; and tomorrow's portion is measured, the Debt taken off it.
        var cardsPortion = Minus(N<TurnEndedTriggeredEffectContext>(PortionCards), StacksOn<TurnEndedTriggeredEffectContext>(self, LeanYearsId));
        var debt = Debt<TurnEndedTriggeredEffectContext>();
        var energyTaken = Min(debt, N<TurnEndedTriggeredEffectContext>(PortionEnergy - 1));
        var cardsTaken = Min(debt, Minus(cardsPortion, N<TurnEndedTriggeredEffectContext>(1)));
        var dusk = Do(
            If(AllOf(Eq(StacksOn<TurnEndedTriggeredEffectContext>(self, DayId), DaysToADistribution),
                    Above(Debt<TurnEndedTriggeredEffectContext>(), 0)),
                Do(HitPlayer(Times(Debt<TurnEndedTriggeredEffectContext>(), DebtCollected), ignoresBlock: true),
                    Unmark<TurnEndedTriggeredEffectContext>(Applicant, DebtId))),
            If(Wears<TurnEndedTriggeredEffectContext>(self, MeasureEqualId), MakeEqual(self)),
            SetStacks(Applicant, PortionEnergyId, Minus(N<TurnEndedTriggeredEffectContext>(PortionEnergy), energyTaken)),
            SetStacks(Applicant, PortionCardsId, Minus(cardsPortion, cardsTaken)),
            SetStacks(Applicant, DebtId, Minus(debt, Max(energyTaken, cardsTaken))),
            // the cards she hands over tomorrow are hers, not your cards'
            Counter(self, CardCredit, Minus(cardsPortion, cardsTaken)));

        var hurt = CombatantTargetSelectors.EventTarget;
        var phases = Do(
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, LeanYearsId)), AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, LeanPercent)),
                Mark<DamageReceivedTriggeredEffectContext>(hurt, LeanYearsId)),
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, MeasureEqualId)), AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, EqualPercent)),
                Mark<DamageReceivedTriggeredEffectContext>(hurt, MeasureEqualId)));

        yield return Face(RuleId, "The Just Measure",
            "Each day is a portion of 3 Energy and 5 cards. Debt comes off the next day, never below 1 and 1. After Day III, 10 a Debt.",
            triggers:
            [
                On("TurnStarted", morning),
                On("CardsDrawn", drawn),
                On("ResourceGained", gained),
                Own("TurnEnded", dusk),
                Own("DamageTaken", phases),
            ]);
        yield return Face(TabletId, "Her Tablet", "Her three days are written on it: you see all three of her moves before Day I.") with
        {
            Disclosure = new DisclosureData(0, DaysToADistribution - 1),
        };
        yield return Face(DayId, "Day of the Distribution", "Day I, II or III. At the end of Day III the Debt still open is collected.",
            stacks: true);
        yield return Face(DebtId, "Debt", "Comes off tomorrow's portion, 1 Energy and 1 card a point. At the end of Day III: 10 damage a point, through Block.",
            StatusPolarity.Debuff, stacks: true);
        yield return Face(PortionEnergyId, "Today's Energy", "Your Energy today, as she measured it.", stacks: true);
        yield return Face(PortionCardsId, "Today's Cards", "The cards you draw today, as she measured them.", stacks: true,
            modifiers: [new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.ClampMax, 0,
                RestrictDamageKind: null, MagnitudeFromStacks: true)]);
        yield return Face(LeanYearsId, "The Lean Years",
            "From 65 %: 4 cards a day, Energy and draws made by your cards write Debt, and her Judgment is 40.");
        yield return Face(MeasureEqualId, "The Measure Made Equal",
            "From 30 %: after each of her turns, whichever side has the higher share of its health is lowered to the other's.");
    }

    // The side with the higher share of its health comes down to the other's share (you never below 1).
    private static IEffectNode<TurnEndedTriggeredEffectContext> MakeEqual(ICombatantTargetSelector her)
    {
        var you = Applicant;
        var herNow = HealthOf<TurnEndedTriggeredEffectContext>(her);
        var herMax = MaxHealthOf<TurnEndedTriggeredEffectContext>(her);
        var youNow = HealthOf<TurnEndedTriggeredEffectContext>(you);
        var youMax = MaxHealthOf<TurnEndedTriggeredEffectContext>(you);
        var herShare = new MultiplyExpression<TurnEndedTriggeredEffectContext>(herNow, youMax);
        var yourShare = new MultiplyExpression<TurnEndedTriggeredEffectContext>(youNow, herMax);
        return If(Less(yourShare, herShare),
            new SetHealthNode<TurnEndedTriggeredEffectContext>(her,
                new DivideExpression<TurnEndedTriggeredEffectContext>(new MultiplyExpression<TurnEndedTriggeredEffectContext>(herMax, youNow), youMax)),
            If(Less(herShare, yourShare),
                new SetHealthNode<TurnEndedTriggeredEffectContext>(you, Max(N<TurnEndedTriggeredEffectContext>(1),
                    new DivideExpression<TurnEndedTriggeredEffectContext>(new MultiplyExpression<TurnEndedTriggeredEffectContext>(youMax, herNow), herMax)))));
    }

    public static IEnumerable<CardData> Cards()
    {
        var you = CombatantTargetSelectors.Source;
        yield return GodCard(TakeFromTomorrowId, "Take From Tomorrow",
            "Retain. Gain 1 Energy and draw 1 card. Write 1 Debt: it comes off tomorrow's portion. Returns to your hand.",
            Do(
                new SetCombatantCounterNode<CardPlayContext>(Her, EnergyCredit, N<CardPlayContext>(1), relative: true),
                new SetCombatantCounterNode<CardPlayContext>(Her, CardCredit, N<CardPlayContext>(1), relative: true),
                new GainResourceNode<CardPlayContext>(you, StandardCombatIds.EnergyResource, N<CardPlayContext>(1)),
                new DrawCardsNode<CardPlayContext>(you, N<CardPlayContext>(1)),
                Mark<CardPlayContext>(you, DebtId)),
            retain: true, returns: true, tags: BorrowTag);
    }

    // A card a walker leaves alone: playing it again and again is never wrong by its own lights (Take From Tomorrow).
    public const string BorrowTag = "borrow";

    // ── her days ──────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId)
    {
        if (enemyId != EnemyId)
            return null;
        var self = CombatantTargetSelectors.Source;
        return intentId switch
        {
            "open_the_canals" => new(Do(HitPlayer<EnemyActionContext>(CanalsDamage), Guard<EnemyActionContext>(self, CanalsBlock))),
            "the_net_of_nanshe" => new(Do(
            [
                .. Enumerable.Repeat(HitPlayer<EnemyActionContext>(NetDamage), NetHits),
                Afflict<EnemyActionContext>(WeakId, NetWeak),
            ])),
            "judgment_of_the_new_year" => new(HitPlayer(Plus(
                Plus(N<EnemyActionContext>(JudgmentDamage), Times(StacksOn<EnemyActionContext>(self, LeanYearsId), LeanJudgmentDamage - JudgmentDamage)),
                Times(Debt<EnemyActionContext>(), JudgmentPerDebt)))),
            _ => null,
        };
    }
}
