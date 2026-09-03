using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Colossus of the Endless Procession. A processional statue advancing one ceremonial step
// at a time, and the act's last word on discipline.
//
// The cycle is fixed and entirely visible, which is the whole design: nothing here is a surprise, and the
// damage is decided by what you did three turns ago.
//
//   STEP I   — the measure is set. Meet it or do not.
//   STEP II  — the burden advances, and the question is whether you WORK IT OFF by playing a taxed card
//              rather than having it cleansed or lost. That distinction is why the act writes down the
//              PAYMENT and not the stack.
//   STEP III — the foot descends: 26 if you did both, 34 if you did one, 40 and a burial if you did neither.
//
// Then a ceremonial pause — cover or a sweep of the causeway — and it begins again. Stone does not hurry: no
// outside effect can make this thing stronger, and the only way it ever gets heavier is your own burials.
public static partial class ActFour
{
    public const string ColossusEnemyId = "colossus_of_the_endless_procession";

    public const string EndlessProcessionId = "the_endless_procession";
    public const string StoneDoesNotHurryId = "stone_does_not_hurry";

    public const int FootBonusCap = 6;
    private const int FootBonusPerBurial = 2;
    private const int FootBoth = 26;
    private const int FootOne = 34;
    private const int FootNeither = 40;
    private const int ProcessionMeasure = 2;

    // What the procession has on the record, and how heavy your own burials have made its foot.
    public static CounterId MeasureFulfilled => new("measure_fulfilled");
    public static CounterId BurdenWorkedOff => new("burden_worked_off");
    public static CounterId FootBonus => new("foot_bonus");
    public static CounterId ProcessionMeasureRead => new("procession_measure_read");
    public static CounterId ProcessionBurdenRead => new("procession_burden_read");

    // Which ceremonial pause comes after this cycle, and whether it is due. The pause alternates between
    // cover and a sweep of the causeway, and the master asks for it to be visible one player turn before it
    // resolves — which it is, because the foot sets it and the intent is telegraphed like any other.
    public static CounterId CausewayTurn => new("causeway_turn");
    public static CounterId CausewayDue => new("causeway_due");

    public static EffectProgram<EnemyActionContext>? ColossusIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "colossus_of_the_endless_procession.processional_measure" =>
                SetTheMeasure(0, Const(ProcessionMeasure)),
            "colossus_of_the_endless_procession.the_burden_advances" => TheBurdenAdvances(18),
            "colossus_of_the_endless_procession.the_foot_descends" => TheFootDescends(),
            "colossus_of_the_endless_procession.clear_the_causeway" => ClearTheCauseway(14),
            _ => null,
        };

    public static IReadOnlyList<StatusData> ColossusStatuses() => [TheEndlessProcession(), StoneDoesNotHurry()];

    // ── the procession ────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheEndlessProcession() => new()
    {
        Id = EndlessProcessionId,
        NameKey = "The Endless Procession",
        DescriptionKey =
            "Three steps, fixed and visible: the measure, the burden, and the foot. Meet the measure and work "
            + "the burden off by PLAYING through it, and the foot comes down at 26. Fail one and it is 34; "
            + "fail both and it is 40 and a burial.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(KeepTheRecord(), nameof(TriggerEvent.TurnStarted)),
            Trigger(HeavierForEveryBurial(), nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(RaiseTheRefusalAgain(), nameof(TriggerEvent.StatusApplicationPrevented)),
        ],
    };

    // "The Colossus cannot gain ordinary Strength from external effects." A prohibition that refuses exactly
    // one status and nothing else — Act III's licence read backwards — and it puts itself straight back up
    // the moment it is spent, which is what makes a refusal permanent rather than a stock of two.
    //
    // That re-arming is only askable because a refusal can now say WHICH prohibition did it: without that,
    // the rule would restore itself on somebody else's ward turning something away.
    public static StatusData StoneDoesNotHurry() => new()
    {
        Id = StoneDoesNotHurryId,
        NameKey = "Stone Does Not Hurry",
        DescriptionKey = "Nothing from outside makes this thing stronger. Ever.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Prevention = new StatusPreventionData(
            StatusPreventionScope.Buffs, StacksPerStack: 99, Only: "strength"),
        Triggers = [],
    };

    // …and the rule that puts it back up, which lives on the BODY's own rule and not on the refusal itself.
    //
    // ⚠ A prohibition cannot answer its own last spend. The spend is synchronous — it happens inside the
    // interception, before the refusal event is handled — so by the time a trigger on the prohibition would
    // run, its final stack is gone and the status with it, and the bearer filter no longer matches anything.
    // The re-arm therefore has to be worn by something that is never spent, and it says which prohibition it
    // is answering, or it would restore the refusal every time somebody else's ward turned something away.
    private static EffectProgram<StatusApplicationBlockedTriggeredEffectContext> RaiseTheRefusalAgain() =>
        new(new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
            new TriggerEventPreventerIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                new StatusDefinitionId(StoneDoesNotHurryId)),
            new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(StoneDoesNotHurryId),
                new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1))));

    // Both records are kept at the Colossus's own turn start — which is before its own action resolves, so
    // Step III reads a record that is already complete, and neither reading has to agree with anything about
    // the order two turn-end rules fire in.
    private static EffectProgram<TurnStartedTriggeredEffectContext> KeepTheRecord()
    {
        var colossus = CombatantTargetSelectors.Source;

        IEffectNode<TurnStartedTriggeredEffectContext> Record(
            CounterId flag, CounterId tally, CounterId bookmark) =>
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        SinceLastLooked<TurnStartedTriggeredEffectContext>(colossus, tally, bookmark),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        colossus, flag, new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                        relative: false)),

                MoveTheBookmark<TurnStartedTriggeredEffectContext>(colossus, tally, bookmark),
            ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Record(MeasureFulfilled, MeasuresMet, ProcessionMeasureRead),
                Record(BurdenWorkedOff, BurdenPaid, ProcessionBurdenRead),
            ]));
    }

    // "Whenever Entombed reaches 5 and actually Stuns the player, future Foot Descends gains +2, max +6."
    // The burial's own rule is what applies the stun, so the stun landing on the player IS the moment — and
    // it is the only observable that says the burial actually cost a turn rather than merely accumulating.
    private static EffectProgram<StatusAppliedTriggeredEffectContext> HeavierForEveryBurial()
    {
        var colossus = Bearer(EndlessProcessionId);

        return new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(
                            StandardCombatIds.StunStatus),
                        new TargetHasStatusExpression<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId))),
                    new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                        new CombatantCounterExpression<StatusAppliedTriggeredEffectContext>(
                            colossus, FootBonus),
                        ComparisonOperator.Less,
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(FootBonusCap))),
                new SetCombatantCounterNode<StatusAppliedTriggeredEffectContext>(
                    colossus, FootBonus,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(FootBonusPerBurial),
                    relative: true)));
    }

    // ── the steps ─────────────────────────────────────────────────────────────────────────────────────────

    // The burden advances, and the record for it is opened: whatever was worked off before this moment
    // belongs to the last cycle.
    private static EffectProgram<EnemyActionContext> TheBurdenAdvances(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(BurdenedId), Const(2)),

            new SetCombatantCounterNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, BurdenWorkedOff, Const(0), relative: false),
        ]));

    // The foot. Its weight is the record, plus whatever your own burials have added to it — and then the
    // record is wiped and the procession starts again, because a procession is endless.
    private static EffectProgram<EnemyActionContext> TheFootDescends()
    {
        var colossus = CombatantTargetSelectors.Source;

        ICombatExpression<EnemyActionContext, bool> Kept(CounterId flag) =>
            new ComparisonExpression<EnemyActionContext>(
                new CombatantCounterExpression<EnemyActionContext>(colossus, flag),
                ComparisonOperator.Greater, Const(0));

        var bonus = new CombatantCounterExpression<EnemyActionContext>(colossus, FootBonus);

        IEffectNode<EnemyActionContext> Descend(int weight, bool bury) =>
            bury
                ? new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new DealDamageNode<EnemyActionContext>(
                        Applicant, new AddExpression<EnemyActionContext>(Const(weight), bonus)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(EntombedId), Const(1)),
                ])
                : new DealDamageNode<EnemyActionContext>(
                    Applicant, new AddExpression<EnemyActionContext>(Const(weight), bonus));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new AndExpression<EnemyActionContext>(Kept(MeasureFulfilled), Kept(BurdenWorkedOff)),
                    Descend(FootBoth, bury: false),
                    new ConditionalEffectNode<EnemyActionContext>(
                        new OrExpression<EnemyActionContext>(Kept(MeasureFulfilled), Kept(BurdenWorkedOff)),
                        Descend(FootOne, bury: false),
                        Descend(FootNeither, bury: true))),

                new SetCombatantCounterNode<EnemyActionContext>(
                    colossus, MeasureFulfilled, Const(0), relative: false),
                new SetCombatantCounterNode<EnemyActionContext>(
                    colossus, BurdenWorkedOff, Const(0), relative: false),

                // …and the pause that follows alternates. The flag is raised by the foot and taken down by
                // the sweep itself, so it can only ever stand during the one slot it is for.
                new SetCombatantCounterNode<EnemyActionContext>(
                    colossus, CausewayTurn,
                    new SubtractExpression<EnemyActionContext>(
                        Const(1),
                        new CombatantCounterExpression<EnemyActionContext>(colossus, CausewayTurn)),
                    relative: false),
                new SetCombatantCounterNode<EnemyActionContext>(
                    colossus, CausewayDue,
                    new CombatantCounterExpression<EnemyActionContext>(colossus, CausewayTurn),
                    relative: false),
            ]));
    }

    private static EffectProgram<EnemyActionContext> ClearTheCauseway(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),

            new SetCombatantCounterNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, CausewayDue, Const(0), relative: false),
        ]));
}
