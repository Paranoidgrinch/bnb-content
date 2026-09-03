using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Keeper of the Thirty-Six Decans. The act's final examination: one watch a turn, each of
// the five words in its own hour, and a reckoning at the sixth.
//
//   WATCH I — MEASURE       1 Weighed.    Answered by meeting it.
//   WATCH II — NAME         1 Inscribed.  Answered by spending the register.
//   WATCH III — LABOR       1 Burdened.   Answered by paying the surcharge off.
//   WATCH IV — BURIAL       1 Entombed.   Answered by being buried and coming out the other side.
//   WATCH V — PRESERVATION  1 Embalmed.   Answered by spending the preservation on something.
//   WATCH VI — RECKONING    the stars are read, and the reading is worth more the more of the act you are
//                           still carrying.
//
// Every watch you clear costs the Keeper 6 HP, which is the whole examination: it is not asking you to
// survive the five words, it is asking you to have LEARNED them. And once the first full round of six is
// done the hours shorten — a minor reckoning after the third watch as well — so a player who is merely
// enduring runs out of turns before a player who is answering does.
public static partial class ActFour
{
    public const string DecanKeeperEnemyId = "keeper_of_the_thirty_six_decans";

    public const string ThirtySixDecansId = "the_thirty_six_decans";
    public const string HoursShortenedId = "the_hours_shorten";

    public const int Watches = 6;
    private const int CorrectObservationLoss = 6;
    private const int ReckoningBase = 16;
    private const int ReckoningPerKind = 4;
    private const int ReckoningCap = 36;
    private const int MinorBase = 10;
    private const int MinorPerKind = 2;
    private const int MinorCap = 20;

    // Which watch is standing, whether a reckoning is due, and the Keeper's bookmark in the met-measure tally.
    public static CounterId Watch => new("watch");
    public static CounterId ReckoningDue => new("reckoning_due");
    public static CounterId MinorDue => new("minor_reckoning_due");
    public static CounterId WatchRead => new("watch_read");

    // The five words, in the order the examination asks for them. Watch VI has no burden of its own.
    private static readonly string[] WatchStatuses =
        [WeighedId, InscribedId, BurdenedId, EntombedId, EmbalmedId];

    public static EffectProgram<EnemyActionContext>? DecanKeeperIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "keeper_of_the_thirty_six_decans.star_reckoning" =>
                Reckoning(ReckoningBase, ReckoningPerKind, ReckoningCap, ReckoningDue),
            "keeper_of_the_thirty_six_decans.minor_reckoning" =>
                Reckoning(MinorBase, MinorPerKind, MinorCap, MinorDue),
            _ => null,
        };

    public static IReadOnlyList<StatusData> DecanKeeperStatuses() => [TheThirtySixDecans(), HoursShorten()];

    // ── the examination ───────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheThirtySixDecans() => new()
    {
        Id = ThirtySixDecansId,
        NameKey = "The Thirty-Six Decans",
        DescriptionKey =
            "One watch a turn: measure, name, labour, burial, preservation, and then the stars are read. "
            + "Clear the watch's burden before your turn ends and the keeper loses 6 — it is asking whether "
            + "you have LEARNED the five words, not whether you can survive them.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(SetTheWatch(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(ObserveTheWatch(), nameof(TriggerEvent.TurnStarted)),
        ],
    };

    public static StatusData HoursShorten() => new()
    {
        Id = HoursShortenedId,
        NameKey = "The Hours Shorten",
        DescriptionKey = "The first full round of watches is done. The stars are now read at the third watch too.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The watch is set at the player's turn start, so the whole turn is theirs to answer it in.
    private static EffectProgram<TurnStartedTriggeredEffectContext> SetTheWatch()
    {
        var keeper = Bearer(ThirtySixDecansId);
        var watch = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(keeper, Watch),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(Watches));

        IEffectNode<TurnStartedTriggeredEffectContext> At(
            int index, IEffectNode<TurnStartedTriggeredEffectContext> hour) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    watch, ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                hour);

        var hours = new List<IEffectNode<TurnStartedTriggeredEffectContext>>();
        for (var i = 0; i < WatchStatuses.Length; i++)
            hours.Add(At(i, new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(WatchStatuses[i]),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: keeper)));

        // The sixth watch carries no burden of its own: it queues the reading, and from then on the hours
        // are permanently shorter.
        hours.Add(At(Watches - 1, new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                keeper, ReckoningDue, new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                relative: false),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                keeper, new StatusDefinitionId(HoursShortenedId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: keeper),
        ])));

        // …and once they have, the third watch queues a minor reading beside its own burden.
        hours.Add(At(2, new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                keeper, new StatusDefinitionId(HoursShortenedId)),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                keeper, MinorDue, new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                relative: false))));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(hours)));
    }

    // …and observed at the Keeper's own turn start, which is the first moment after the player's turn ended
    // and the act's ordering-free place to ask. A measure is the exception the master names: it removes
    // itself either way, so what counts is whether it was MET, read through a bookmark in the act's tally.
    private static EffectProgram<TurnStartedTriggeredEffectContext> ObserveTheWatch()
    {
        var keeper = CombatantTargetSelectors.Source;
        var watch = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(keeper, Watch),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(Watches));

        var correct = new DealDamageNode<TurnStartedTriggeredEffectContext>(
            keeper, new ConstantExpression<TurnStartedTriggeredEffectContext>(CorrectObservationLoss),
            ignoresBlock: true, kind: DamageKind.DamageOverTime);

        IEffectNode<TurnStartedTriggeredEffectContext> Cleared(int index, string statusId) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    watch, ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(index)),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    index == 0
                        // Watch I: a measure met, not a measure gone.
                        ? new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            SinceLastLooked<TurnStartedTriggeredEffectContext>(keeper, MeasuresMet, WatchRead),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0))
                        : new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(statusId))),
                    correct));

        var steps = new List<IEffectNode<TurnStartedTriggeredEffectContext>>();
        for (var i = 0; i < WatchStatuses.Length; i++)
            steps.Add(Cleared(i, WatchStatuses[i]));

        steps.Add(MoveTheBookmark<TurnStartedTriggeredEffectContext>(keeper, MeasuresMet, WatchRead));
        steps.Add(new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
            keeper, Watch, new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(steps));
    }

    // ── the readings ──────────────────────────────────────────────────────────────────────────────────────

    // The stars read what you are still carrying — by KIND, so the examination answers breadth and not depth
    // — and the reading is spent on being made.
    private static EffectProgram<EnemyActionContext> Reckoning(int start, int perKind, int cap, CounterId due) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MinExpression<EnemyActionContext>(
                    Const(cap),
                    new AddExpression<EnemyActionContext>(
                        Const(start),
                        new MultiplyExpression<EnemyActionContext>(
                            Const(perKind), NegativeKinds<EnemyActionContext>())))),

            new SetCombatantCounterNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, due, Const(0), relative: false),
        ]));
}
