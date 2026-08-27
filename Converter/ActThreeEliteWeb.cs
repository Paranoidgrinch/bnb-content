using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 2 — **Grandmother Web** (154 HP).
//
// > Local customs are physical threads: Safe-Conduct can cut one, but the web eventually mends.
//
// Three household courtesies hang in the white-thorn as visible Threads, and only a TAUT one is law. What
// makes the Web the act's cleverest use of Safe-Conduct is that a licence spent here does not buy the player
// a turn of safety — it buys the removal of a RULE, for the rest of this turn and the whole of the next.
// The player therefore chooses which custom is worth cutting, and the Web chooses which to knot.
//
// Four states per Thread, and each is a status the fight shows by name:
//
//   Taut    — the rule is law. (Slack is simply the absence of Taut.)
//   Cut     — refused by a licence; inert for two of the player's turns, and worth 7 Block when it mends.
//   Knotted — the Web has standing behind it; the Thread attempts 2 Trespass instead of 1.
//
// The one thing this needs that the act did not already have is knowing WHICH rule a refused Trespass came
// from — and the act already writes that down: `LawBeingFiledCounter` holds the law being filed for exactly
// as long as the filing lasts, and a refusal happens inside the filing.
public static partial class ActThree
{
    public const string GrandmotherWebEnemyId = "grandmother_web";
    public const string GrandmotherWebId = "grandmother_web_threads";

    // The three courtesies, in the order everything that has to pick "one of them" picks.
    public const int ThreadOfEntryLaw = 13;
    public const int ThreadOfMeasureLaw = 14;
    public const int ThreadOfDepartureLaw = 15;

    private static readonly (string Key, string Name, int Law)[] Threads =
    [
        ("entry", "Thread of Entry", ThreadOfEntryLaw),
        ("measure", "Thread of Measure", ThreadOfMeasureLaw),
        ("departure", "Thread of Departure", ThreadOfDepartureLaw),
    ];

    private static string TautId(string key) => $"web_thread_{key}_taut";
    private static string CutId(string key) => $"web_thread_{key}_cut";
    private static string KnotId(string key) => $"web_thread_{key}_knot";

    // Cut "for the rest of the current turn and the next full player turn": two of the player's turn starts
    // have to pass before it mends, and the start that takes it to nothing is the mending.
    private const int CutTurns = 2;
    private const int MendBlock = 7;

    public const string WebRotatedId = "web_rotated";
    public const string WebMeasuredThisTurnId = "web_measured_this_turn";
    public static CounterId WebActionsCounter => new("web_actions");

    private static ICombatantTargetSelector Web { get; } = Elite(GrandmotherWebId);

    private static IEnumerable<StatusData> WebStatuses() =>
    [
        GrandmotherWebThreads(),
        .. Threads.SelectMany(t => new[]
        {
            Marker(TautId(t.Key), $"{t.Name}: Taut",
                $"The {t.Name} is drawn tight, and while it is, it is law."),
            CutThread(t),
            Marker(KnotId(t.Key), $"{t.Name}: Knotted",
                $"An older promise has been tied into the {t.Name}. It attempts 2 Trespass rather than 1, "
                + "and a licence spent against it unties the knot as well as cutting the thread."),
        }),
        Marker(WebRotatedId, "Threads Turned",
            "The web has already shifted which courtesies are drawn tight."),
        Marker(WebMeasuredThisTurnId, "Measure Taken",
            "The Thread of Measure has already answered a matched pair this turn."),
    ];

    private static StatusData CutThread((string Key, string Name, int Law) thread) => new()
    {
        Id = CutId(thread.Key),
        NameKey = $"{thread.Name}: Cut",
        DescriptionKey =
            $"The {thread.Name} has been cut and is no law at all. It mends at the start of your second turn "
            + $"from now, and the Web gains {MendBlock} Block when it does.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── The three courtesies, and everything that happens to them ─────────────────────────────────────────
    private static StatusData GrandmotherWebThreads()
    {
        var player = CombatantTargetSelectors.Source;

        // Thread of Entry — the turn's first real card being a Deed.
        var entry = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Live<CardPlayedTriggeredEffectContext>("entry"),
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                        new TagId(Cards.CardAuthoring.DeedTag)))),
            Violate<CardPlayedTriggeredEffectContext>(
                Web, ThreadOfEntryLaw, stacks: Knotted<CardPlayedTriggeredEffectContext>("entry")));

        // Thread of Measure — two cards in a row of the same Base Cost, once a turn.
        var memory = CostMemory("web_measure");
        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var measure = new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    Live<CardPlayedTriggeredEffectContext>("measure"),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                        ComparisonOperator.Equal, ThisCost())),
                Violate<CardPlayedTriggeredEffectContext>(
                    Web, ThreadOfMeasureLaw, WebMeasuredThisTurnId,
                    stacks: Knotted<CardPlayedTriggeredEffectContext>("measure"))),
            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                player, memory, ThisCost(), relative: false),
        ]);

        var courtesies = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>([entry, measure]));

        // Thread of Departure — ending a turn with nothing real left to play.
        //
        // ADAPTATION: "no VALID non-Junk card" is read as "no non-Junk card". Whether a card in hand could
        // legally be played is not a question the rules layer can put to itself, and the difference only
        // shows on a hand of unaffordable cards — where the Thread is arguably right to fire anyway.
        var departure = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Live<TurnEndedTriggeredEffectContext>("departure"),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            RealCardsInHand<TurnEndedTriggeredEffectContext>(),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))),
                Violate<TurnEndedTriggeredEffectContext>(
                    Web, ThreadOfDepartureLaw,
                    stacks: Knotted<TurnEndedTriggeredEffectContext>("departure"))));

        // The player's turn opening: the measure forgets the last card, the latch clears, and any Thread
        // whose two turns have run mends — which the Web is paid 7 Block for.
        var mend = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        player, memory, new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                        relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Web, new StatusDefinitionId(WebMeasuredThisTurnId)),
                    .. Threads.Select(t => MendOne<TurnStartedTriggeredEffectContext>(t.Key)),
                ])));

        // A licence spent is a rule removed — and the act already wrote down which rule, because the counter
        // that says what is being filed is still standing while the filing is being refused.
        var cut = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(GrandmotherWebId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                    [.. Threads.Select(CutOne)])));

        // Standing is what lets the Web tie an older promise into a courtesy that is already law.
        EffectProgram<TContext> knot<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                KnotAThread<TContext>()));

        // "After every second Grandmother Web action, one Taut Thread becomes Slack and the Slack Thread
        // becomes Taut." The slack courtesy walks the three in order, so the next rotation is always visible.
        var rotate = new EffectProgram<ActionResolvedTriggeredEffectContext>(
            new CausalSequenceEffectNode<ActionResolvedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<ActionResolvedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, WebActionsCounter,
                    new ConstantExpression<ActionResolvedTriggeredEffectContext>(1), relative: true),
                new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                    new ComparisonExpression<ActionResolvedTriggeredEffectContext>(
                        new RemainderExpression<ActionResolvedTriggeredEffectContext>(
                            new CombatantCounterExpression<ActionResolvedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, WebActionsCounter),
                            new ConstantExpression<ActionResolvedTriggeredEffectContext>(2)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<ActionResolvedTriggeredEffectContext>(0)),
                    Rotate<ActionResolvedTriggeredEffectContext>()),
            ]));

        return Rule(GrandmotherWebId, "Three Courtesy Threads",
            "Three household customs hang in the hedge, and only the ones drawn Taut are law: Entry (open a "
            + "turn with a Deed), Measure (two cards in a row of the same Base Cost) and Departure (end a "
            + "turn with nothing real in hand). A licence spent against one CUTS it for two of your turns; "
            + "the Web mends it for 7 Block, and every Claim it is granted knots another tight.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    courtesies, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    departure, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    mend, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    cut, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    knot<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    knot<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
                new StatusTriggerData("ActionResolved", JsonSerializer.SerializeToElement(
                    rotate, CombatJson.CreateOptions<ActionResolvedTriggeredEffectContext>())),
            ]);
    }

    // ── reading a Thread ──────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, bool> Has<TContext>(string statusId)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Web, new StatusDefinitionId(statusId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // A Thread is law when it is drawn tight and has not been cut.
    private static ICombatExpression<TContext, bool> Live<TContext>(string key)
        where TContext : class =>
        new AndExpression<TContext>(
            Has<TContext>(TautId(key)),
            new NotExpression<TContext>(Has<TContext>(CutId(key))));

    // One Trespass, or two off a knotted Thread — one application either way, so a single licence refuses it.
    private static ICombatExpression<TContext, int> Knotted<TContext>(string key)
        where TContext : class =>
        new AddExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantStatusStacksExpression<TContext>(Web, new StatusDefinitionId(KnotId(key)))));

    private static ICombatExpression<TContext, int> RealCardsInHand<TContext>()
        where TContext : class =>
        new SubtractExpression<TContext>(
            new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand),
            new CombatantZoneCardCountExpression<TContext>(
                Applicant, CardZone.Hand, new TagId(Cards.CardAuthoring.JunkTag)));

    // ── cutting, mending, knotting, rotating ──────────────────────────────────────────────────────────────

    // The refused filing names its own law, so each Thread only has to ask whether the law was its.
    private static IEffectNode<StatusApplicationBlockedTriggeredEffectContext> CutOne(
        (string Key, string Name, int Law) thread) =>
        new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
            new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                new CombatantCounterExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, LawBeingFiledCounter),
                ComparisonOperator.Equal,
                new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(thread.Law)),
            new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
            [
                new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Web, new StatusDefinitionId(CutId(thread.Key)),
                    new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(CutTurns)),
                // "If Safe-Conduct prevents its Trespass: remove the Knot and Cut the Thread."
                new RemoveStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Web, new StatusDefinitionId(KnotId(thread.Key))),
            ]));

    private static IEffectNode<TContext> MendOne<TContext>(string key)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            Has<TContext>(CutId(key)),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(Web, new StatusDefinitionId(CutId(key))),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<TContext>(1)),
                Mend<TContext>(key),
                new ModifyStatusStacksNode<TContext>(
                    Web, new StatusDefinitionId(CutId(key)), new ConstantExpression<TContext>(-1))));

    // A courtesy coming back is worth something to the household that keeps it.
    private static IEffectNode<TContext> Mend<TContext>(string key)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(Web, new StatusDefinitionId(CutId(key))),
            new GainBlockNode<TContext>(Web, new ConstantExpression<TContext>(MendBlock)),
        ]);

    // "One Taut, non-Cut Thread becomes Knotted; maximum one Knot per Thread." First eligible in order, and
    // the latch is what makes "one" mean one across three conditionals in a row.
    private static IEffectNode<TContext> KnotAThread<TContext>()
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(Web, new StatusDefinitionId(WebRotatedId)),
            .. Threads.Select(t => (IEffectNode<TContext>)new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Has<TContext>(WebRotatedId)),
                    new AndExpression<TContext>(
                        Live<TContext>(t.Key),
                        new NotExpression<TContext>(Has<TContext>(KnotId(t.Key))))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(
                        Web, new StatusDefinitionId(KnotId(t.Key)), new ConstantExpression<TContext>(1)),
                    new ApplyStatusNode<TContext>(
                        Web, new StatusDefinitionId(WebRotatedId), new ConstantExpression<TContext>(1)),
                ]))),
            new RemoveStatusNode<TContext>(Web, new StatusDefinitionId(WebRotatedId)),
        ]);

    private static ICombatExpression<TContext, bool> AnyKnottable<TContext>()
        where TContext : class =>
        Threads.Select(t => (ICombatExpression<TContext, bool>)new AndExpression<TContext>(
                Live<TContext>(t.Key), new NotExpression<TContext>(Has<TContext>(KnotId(t.Key)))))
            .Aggregate((a, b) => new OrExpression<TContext>(a, b));

    // The slack courtesy walks the three in order — Departure, then Entry, then Measure — so that "next
    // rotation is visible" is true by construction rather than by a preview the fight has to draw.
    private static IEffectNode<TContext> Rotate<TContext>()
        where TContext : class
    {
        var steps = new List<IEffectNode<TContext>>
        {
            new RemoveStatusNode<TContext>(Web, new StatusDefinitionId(WebRotatedId)),
        };

        for (var i = 0; i < Threads.Length; i++)
        {
            var slack = Threads[i].Key;
            var next = Threads[(i + 1) % Threads.Length].Key;
            steps.Add(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Has<TContext>(WebRotatedId)),
                    new NotExpression<TContext>(Has<TContext>(TautId(slack)))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(
                        Web, new StatusDefinitionId(TautId(slack)), new ConstantExpression<TContext>(1)),
                    new RemoveStatusNode<TContext>(Web, new StatusDefinitionId(TautId(next))),
                    // The knot stays where it was tied. A Slack Thread is not law, so it does nothing there
                    // — but an older promise does not come untied merely because the web shifted.
                    new ApplyStatusNode<TContext>(
                        Web, new StatusDefinitionId(WebRotatedId), new ConstantExpression<TContext>(1)),
                ])));
        }

        steps.Add(new RemoveStatusNode<TContext>(Web, new StatusDefinitionId(WebRotatedId)));
        return new CausalSequenceEffectNode<TContext>(steps);
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? WebIntent(string enemyId, string intentId)
    {
        if (enemyId != GrandmotherWebEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext>? program = intentId switch
        {
            // "Mend one Cut Thread immediately and gain 14 Block; if none Cut, gain 20 Block." The mending
            // itself is worth its usual 7 on top — it is the same mending, however it comes about.
            "mend_the_household_law" => new ConditionalEffectNode<EnemyActionContext>(
                Threads.Select(t => (ICombatExpression<EnemyActionContext, bool>)Has<EnemyActionContext>(
                        CutId(t.Key)))
                    .Aggregate((a, b) => new OrExpression<EnemyActionContext>(a, b)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    MendFirstCut(),
                    new GainBlockNode<EnemyActionContext>(
                        self, new ConstantExpression<EnemyActionContext>(14)),
                ]),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(20))),
            // "Knot an eligible Thread +8 Block; if none, deal 16 instead."
            "knot_an_older_promise" => new ConditionalEffectNode<EnemyActionContext>(
                AnyKnottable<EnemyActionContext>(),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    KnotAThread<EnemyActionContext>(),
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(8)),
                ]),
                Blow(16)),
            "hospitality_has_its_price" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ConsumeClaim<EnemyActionContext>(self),
                    DemandWergild<EnemyActionContext>(self, 2),
                ])),
            // "18 +4 per Knotted Thread, max 26" — which is two knots' worth.
            "close_the_web_around_the_guest" => new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(18),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(4),
                        new MinExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(2), KnotCount<EnemyActionContext>())))),
            _ => null,
        };

        return program is null ? null : new EffectProgram<EnemyActionContext>(program);
    }

    private static ICombatExpression<TContext, int> KnotCount<TContext>()
        where TContext : class =>
        Threads.Select(t => (ICombatExpression<TContext, int>)new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantStatusStacksExpression<TContext>(Web, new StatusDefinitionId(KnotId(t.Key)))))
            .Aggregate((a, b) => new AddExpression<TContext>(a, b));

    private static IEffectNode<EnemyActionContext> MendFirstCut()
    {
        var steps = new List<IEffectNode<EnemyActionContext>>
        {
            new RemoveStatusNode<EnemyActionContext>(Web, new StatusDefinitionId(WebRotatedId)),
        };
        steps.AddRange(Threads.Select(t => (IEffectNode<EnemyActionContext>)new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(Has<EnemyActionContext>(WebRotatedId)),
                Has<EnemyActionContext>(CutId(t.Key))),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Mend<EnemyActionContext>(t.Key),
                new ApplyStatusNode<EnemyActionContext>(
                    Web, new StatusDefinitionId(WebRotatedId), new ConstantExpression<EnemyActionContext>(1)),
            ]))));
        steps.Add(new RemoveStatusNode<EnemyActionContext>(Web, new StatusDefinitionId(WebRotatedId)));
        return new CausalSequenceEffectNode<EnemyActionContext>(steps);
    }
}
