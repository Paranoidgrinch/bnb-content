using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Rope-Master of the Corvée. The one encounter in which Burdened does what a corvée
// actually does: it conscripts.
//
// Every card whose surcharge you actually PAY is one more piece of labour owed. At three, the Rope-Master
// calls the missing hands and a Stone-Hauler Ushabti walks on — the Stage-6 body, at 72 HP, on the rope. Two
// at most, and two in the whole fight; once the roll is used up, labour stops being owed at all, which is
// what keeps a tax on your own hand from becoming an unbounded add-spawner.
//
// While a Hauler lives the Rope-Master braces behind it, and cutting one down cuts the rope: a burden comes
// off you and the Rope-Master's cover goes with it. The gang is therefore both the pressure and the answer.
public static partial class ActFour
{
    public const string RopeMasterEnemyId = "rope_master_of_the_corvee";
    public const string StoneHaulerSummonEnemyId = "stone_hauler_of_the_corvee";

    public const string TheCorveeId = "the_corvee";
    public const string AtTheRopeId = "at_the_rope";
    public const string WorkedLastId = "worked_last";

    public const int LaborForOneHand = 3;
    public const int HaulerHealth = 72;
    public const int SummonsPerCombat = 2;

    private const int WorkRopeBlock = 8;
    private const int CutRopeBlock = 8;

    // The Rope-Master's books: labour owed, its bookmark in the player's surcharge tally, how many calls are
    // left, whether a hand has already worked this enemy turn, and where the gang is in its rhythm.
    public static CounterId LaborOwed => new("labor_owed");
    public static CounterId LaborRead => new("labor_read");
    public static CounterId CallsMade => new("calls_made");
    public static CounterId HandsUsed => new("hands_used");
    public static CounterId SledgeStep => new("sledge_step");

    public static EffectProgram<EnemyActionContext>? RopeMasterIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "rope_master_of_the_corvee.rope_across_the_back" => RopeAcrossTheBack(8),
            "rope_master_of_the_corvee.call_the_missing_hands" => CallTheMissingHands(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> RopeMasterStatuses() => [TheCorvee(), AtTheRope(), WorkedLast()];

    // ── the corvée ────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheCorvee() => new()
    {
        Id = TheCorveeId,
        NameKey = "The Corvée",
        DescriptionKey =
            "Every card whose Burdened surcharge you actually pay is labour owed. At 3 the missing hands are "
            + "called — twice in a fight, two on the rope at once — and while any of them lives this "
            + "rope-master braces behind them for 8.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(CountTheLabour(), nameof(TriggerEvent.TurnStarted))],
    };

    // The Rope-Master's own turn start does four things, in this order: the roll is opened for the round, the
    // rope is braced if anyone is on it, and the surcharges paid since it last looked become labour — queued
    // rather than resolved, because §6.5 says a signature never interrupts a card or a measure, and reading
    // the tally at a fixed moment of the Rope-Master's own is exactly that.
    private static EffectProgram<TurnStartedTriggeredEffectContext> CountTheLabour()
    {
        var master = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(master, BurdenPaid, LaborRead);

        var owed = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(master, LaborOwed);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // A fresh enemy round: one hand works it.
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    master, HandsUsed,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

                // Work Rope — while at least one hand is on it, and never once per hand.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        LivingHands<TurnStartedTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new GainBlockNode<TurnStartedTriggeredEffectContext>(
                        master, new ConstantExpression<TurnStartedTriggeredEffectContext>(WorkRopeBlock))),

                // Labour is only owed while there is a call left to make: an exhausted roll stops the tally
                // rather than banking it, which is the master's own bound. Counted up from nothing rather
                // than down from two, because a counter nobody has written yet reads zero, and "none left"
                // and "not started" must not be the same answer.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(master, CallsMade),
                        ComparisonOperator.Less,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(SummonsPerCombat)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        master, LaborOwed,
                        new MinExpression<TurnStartedTriggeredEffectContext>(
                            new AddExpression<TurnStartedTriggeredEffectContext>(owed, unread),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(LaborForOneHand)),
                        relative: false)),

                MoveTheBookmark<TurnStartedTriggeredEffectContext>(master, BurdenPaid, LaborRead),
            ]));
    }

    // ── the hands ─────────────────────────────────────────────────────────────────────────────────────────

    // A summoned body carries no action script of its own — the engine's intent selector only knows the
    // roster the fight opened with — so a Hauler acts the way every summon in this engine acts: through a
    // marker status with a turn-start program. That marker is also where its death is heard.
    public static StatusData AtTheRope() => new()
    {
        Id = AtTheRopeId,
        NameKey = "At the Rope",
        DescriptionKey =
            "Conscripted labour. One hand works per enemy turn, taking it in turns if two are on the rope — "
            + "and cutting one down takes a burden off you and 8 Block off the rope-master.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(WorkTheRope(), nameof(TriggerEvent.TurnStarted)),
            Trigger(CutTheRope(), nameof(TriggerEvent.Downed)),
        ],
    };

    // Which hand worked last, so two of them take it in turns rather than one doing everything.
    public static StatusData WorkedLast() => new()
    {
        Id = WorkedLastId,
        NameKey = "Worked Last",
        DescriptionKey = "This hand worked the rope last turn; the other one takes the next.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> WorkTheRope()
    {
        var hand = CombatantTargetSelectors.Source;
        var master = Bearer(TheCorveeId);
        var step = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(master, SledgeStep);

        IEffectNode<TurnStartedTriggeredEffectContext> Job(
            int index, IEffectNode<TurnStartedTriggeredEffectContext> work) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(
                        step, new ConstantExpression<TurnStartedTriggeredEffectContext>(3)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                work);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    // One hand works per enemy turn …
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(master, HandsUsed),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    // … and not the one that worked last, unless it is the only hand left on the rope.
                    new OrExpression<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                hand, new StatusDefinitionId(WorkedLastId))),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            LivingHands<TurnStartedTriggeredEffectContext>(),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)))),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        master, HandsUsed,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false),

                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                        Hands(),
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget,
                            new StatusDefinitionId(WorkedLastId))),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        hand, new StatusDefinitionId(WorkedLastId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),

                    // Drag the Sledge, Shoulder the Stone, Pull the Rope Tight — the gang keeps one rhythm
                    // between them, which is what a gang is.
                    Job(0, new DealDamageNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new ConstantExpression<TurnStartedTriggeredEffectContext>(13))),
                    Job(1, new GainBlockNode<TurnStartedTriggeredEffectContext>(
                        master, new ConstantExpression<TurnStartedTriggeredEffectContext>(8))),
                    Job(2, new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(BurdenedId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: hand)),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        master, SledgeStep,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                ])));
    }

    // Cut the Rope: a hand down is a burden off you and cover off the rope-master. Read on the marker the
    // hand is still wearing as it falls, which is the only place this is askable.
    private static EffectProgram<CombatantDownedTriggeredEffectContext> CutTheRope()
    {
        var master = Bearer(TheCorveeId);

        return new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                new ModifyStatusStacksNode<CombatantDownedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(BurdenedId),
                    new ConstantExpression<CombatantDownedTriggeredEffectContext>(-1)),

                new ModifyDefensivePoolNode<CombatantDownedTriggeredEffectContext>(
                    master, StandardCombatIds.BlockDefensivePool,
                    new ConstantExpression<CombatantDownedTriggeredEffectContext>(-CutRopeBlock)),
            ]));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> RopeAcrossTheBack(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
        ]));

    // The call. A hand walks on wearing the rope — and if the rope is already full, the call is spent on
    // bracing instead, so the roll runs out either way and the gang can never be three.
    private static EffectProgram<EnemyActionContext> CallTheMissingHands()
    {
        var master = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        LivingHands<EnemyActionContext>(), ComparisonOperator.GreaterOrEqual, Const(2)),
                    new GainBlockNode<EnemyActionContext>(master, Const(20)),
                    new SummonCombatantNode<EnemyActionContext>(
                        StandardCombatIds.EnemyTeam,
                        Const(HaulerHealth),
                        new CombatantDefinitionId(StoneHaulerSummonEnemyId),
                        "Stone-Hauler Ushabti",
                        startingStatuses:
                        [
                            new StatusGrant(new StatusDefinitionId(LabyrinthBodyId), 1),
                            new StatusGrant(new StatusDefinitionId(AtTheRopeId), 1),
                        ])),

                new SetCombatantCounterNode<EnemyActionContext>(master, CallsMade, Const(1), relative: true),
                new SetCombatantCounterNode<EnemyActionContext>(master, LaborOwed, Const(0), relative: false),
            ]));
    }

    // Everyone on the rope — the summoned hands, and never the rope-master itself.
    private static ICombatantTargetSelector Hands() =>
        CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(AtTheRopeId));

    private static ICombatExpression<TContext, int> LivingHands<TContext>() where TContext : class =>
        new CountTargetsExpression<TContext>(Hands());
}
