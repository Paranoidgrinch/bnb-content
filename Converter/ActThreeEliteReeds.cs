using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 8 — **Three Reeds of Appeal** (78 / 84 / 90 HP).
//
// > Claims physically travel through a tribunal, and kill order rewrites the route of appeal.
//
// Three reeds stand in shallow black water: **Hearing → Remand → Refusal**. Standing does not sit where it
// is made here; at the end of every enemy turn one Claim moves one position further along the chain, and
// what waits at the end of it is a demand for three.
//
// The whole encounter is therefore about the ROUTE, and the player edits the route by choosing what to
// kill. Cut the Remand and Claims pass straight from Hearing to Refusal — faster. Cut the Refusal and a
// Claim reaching the end of the chain simply falls out of it and hands you a licence — but every surviving
// reed grows a Strength for the insult, and the last one standing grows another.
//
// A transfer is never a creation: nothing along this chain raises the announcement, which is the single
// rule that keeps a three-body tribunal from manufacturing standing out of its own procedure.
public static partial class ActThree
{
    public const string HearingReedId = "hearing_reed";
    public const string RemandReedId = "remand_reed";
    public const string RefusalReedId = "refusal_reed";

    public const string AppealChainId = "the_appeal_chain";
    public const string AppealHeldId = "appeal_held_under_review";
    public const string AppealMovedId = "appeal_moved_this_turn";
    public const string AtRefusalId = "awaiting_final_refusal";
    public const string ReedStrengthGivenId = "reeds_bereaved";
    public const string AppealOpenedId = "appeal_opened";
    public const string RemandFedId = "nothing_ends_here";

    public const int StateTheMatterLaw = 23;

    // The type of the last real card of the PREVIOUS player turn, and of this one: 1 Deed, 2 Working,
    // 3 Rite. Two cells, because the Hearing compares one turn against another and a single cell would be
    // overwritten by the very card it is meant to judge.
    public static CounterId LastTypeBeforeCounter => new("reed_last_type_before");
    public static CounterId LastTypeNowCounter => new("reed_last_type_now");

    private static readonly (string Marker, string Name)[] Reeds =
    [
        (HearingReedId, "Hearing Reed"),
        (RemandReedId, "Remand Reed"),
        (RefusalReedId, "Refusal Reed"),
    ];

    private static ICombatantTargetSelector Reed(string marker) => Lawgiver(marker);

    private static ICombatExpression<TContext, bool> Standing<TContext>(
        ICombatantTargetSelector reed)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(reed),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static IEnumerable<StatusData> ReedStatuses() =>
    [
        TheAppealChain(),
        Marker(HearingReedId, "Hearing Reed",
            "Where a matter is first stated. Standing made here begins its journey downstream."),
        Marker(RemandReedId, "Remand Reed",
            "Nothing ends here. It sends matters on, and is fed by other people keeping their word."),
        Marker(RefusalReedId, "Refusal Reed",
            "The end of the chain. Standing that reaches it is refused, at a price."),
        Marker(AppealHeldId, "Held Under Review",
            "The next automatic transfer along the chain waits one enemy turn."),
        Marker(AppealMovedId, "Appeal Moved",
            "One matter has already travelled the chain this enemy turn."),
        Marker(AtRefusalId, "Awaiting Final Refusal",
            "A matter has reached the Refusal Reed. Its next action refuses it, and names a price of 3."),
        RemandFed(),
        Marker(AppealOpenedId, "Tribunal Sitting",
            "The reeds have opened. From the next bell, one matter travels the chain each round."),
        Marker(ReedStrengthGivenId, "Bereaved",
            "The tribunal has already been paid for the reed it lost."),
    ];

    // "Nothing Ends Here — whenever Wergild from any Reed is fully paid, the Remand Reed gains 6 Block."
    //
    // ADAPTATION: a demand falls due as a turn ENDS, and Block granted there is swept away before the
    // player can meet it — so the settlement books the Block and the Remand puts it up at the next bell,
    // which is the turn it was ever meant to survive.
    private static StatusData RemandFed() => new()
    {
        Id = RemandFedId,
        NameKey = "Nothing Ends Here",
        DescriptionKey =
            "Somebody kept their word. The Remand Reed guards for 6 at the start of your next turn, once "
            + "for each promise kept.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the chain ─────────────────────────────────────────────────────────────────────────────────────────
    //
    // The whole tribunal is one rule and the PLAYER carries it, because it has to outlive any of the three
    // bodies it is about — a rule kept on a reed would stop working the moment that reed was cut.
    private static StatusData TheAppealChain()
    {
        var player = CombatantTargetSelectors.Source;

        // "State the Matter Clearly": the turn's first real card must differ in type from the last real
        // card of the turn before. The first turn is exempt, which falls out of the cell being empty.
        //
        // Written out one type at a time rather than as a number computed from the card, because a program
        // that ships has to be built from the vocabulary the document is written in — and "which of three
        // tags is on this card" is a question, not an arithmetic expression.
        (string Tag, int Type)[] types =
        [
            (Cards.CardAuthoring.DeedTag, 1),
            (Cards.CardAuthoring.WorkingTag, 2),
            (Cards.CardAuthoring.RiteTag, 3),
        ];

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> SaidAgain(string tag, int type) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                        player, LastTypeBeforeCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(type)));

        var stated = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new NotExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                        new TagId(Cards.CardAuthoring.JunkTag))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            Standing<CardPlayedTriggeredEffectContext>(Reed(HearingReedId)),
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                                    NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                                types.Select(t => SaidAgain(t.Tag, t.Type))
                                    .Aggregate((a, b) =>
                                        new OrExpression<CardPlayedTriggeredEffectContext>(a, b)))),
                        Violate<CardPlayedTriggeredEffectContext>(
                            Reed(HearingReedId), StateTheMatterLaw)),
                    .. types.Select(t => (IEffectNode<CardPlayedTriggeredEffectContext>)
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                                new TagId(t.Tag)),
                            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                                player, LastTypeNowCounter,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(t.Type),
                                relative: false))),
                ])));

        // The player's bell: what was said last turn is now what the matter is measured against.
        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, LastTypeBeforeCounter,
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                            Applicant, LastTypeNowCounter), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, LastTypeNowCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(AppealMovedId)),
                    // What the tribunal was fed while the player was settling goes up as Block now.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                Reed(RemandReedId), new StatusDefinitionId(RemandFedId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                Reed(RemandReedId),
                                new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(6),
                                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                        Reed(RemandReedId), new StatusDefinitionId(RemandFedId)))),
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                Reed(RemandReedId), new StatusDefinitionId(RemandFedId)),
                        ])),
                    // One matter travels — the design says "at the end of each enemy turn", and this is the
                    // same board seen from the side that has to answer it. Written at the player's bell
                    // because a turn ENDING happens once per body in the tribunal, and the chain moves once
                    // per round however many reeds are still standing.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(AppealOpenedId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        // The reeds are still opening: the first bell moves nothing.
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(AppealOpenedId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                    Applicant, new StatusDefinitionId(AppealHeldId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(AppealHeldId)),
                            MoveOneAppeal<TurnStartedTriggeredEffectContext>())),
                ])));

        // "Nothing Ends Here": a demand settled in full anywhere in the fight feeds the middle of the chain.
        // It is spliced into the act's one settlement, so it hears every creditor and not only its own.

        // The kill order rewrites the route, and the tribunal is paid for what it loses.
        var bereaved = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                // "Each surviving Reed gains 1 Strength when Refusal Reed dies."
                new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                    new AndExpression<CombatantDownedTriggeredEffectContext>(
                        new NotExpression<CombatantDownedTriggeredEffectContext>(
                            Standing<CombatantDownedTriggeredEffectContext>(Reed(RefusalReedId))),
                        new NotExpression<CombatantDownedTriggeredEffectContext>(
                            new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<CombatantDownedTriggeredEffectContext>(
                                    Applicant, new StatusDefinitionId(ReedStrengthGivenId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CombatantDownedTriggeredEffectContext>(0)))),
                    new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [
                        Strengthen(HearingReedId), Strengthen(RemandReedId),
                        new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(ReedStrengthGivenId),
                            new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)),
                    ])),
                // "Only one Reed remains: the survivor gains 1 additional Strength."
                new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                    new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                        LivingReeds<CombatantDownedTriggeredEffectContext>(),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)),
                    new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                        [.. Reeds.Select(r => Strengthen(r.Marker))])),
            ]));

        return Rule(AppealChainId, "The Appeal Chain",
            "Hearing, then Remand, then Refusal. At the end of every enemy turn one Claim travels one "
            + "living position further along, and a matter that reaches the Refusal Reed is refused for 3 "
            + "Wergild. Open a turn with the same kind of card you closed the last one with and you owe the "
            + "Hearing Reed 1 Trespass. Cut the Refusal and standing falls out of the chain and hands you a "
            + "licence — but the reeds that remain grow stronger for it.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    stated, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(FinalRefusalGuard()),
                    CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("Downed", JsonSerializer.SerializeToElement(
                    bereaved, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    private static IEffectNode<CombatantDownedTriggeredEffectContext> Strengthen(string marker) =>
        new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            Standing<CombatantDownedTriggeredEffectContext>(Reed(marker)),
            new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                Reed(marker), new StatusDefinitionId("strength"),
                new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)));

    private static ICombatExpression<TContext, int> LivingReeds<TContext>()
        where TContext : class =>
        Reeds.Select(r => (ICombatExpression<TContext, int>)new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantCurrentHealthExpression<TContext>(Reed(r.Marker))))
            .Aggregate((a, b) => new AddExpression<TContext>(a, b));

    // ── one matter travels ────────────────────────────────────────────────────────────────────────────────
    //
    // The oldest movable Claim is the one furthest along, because a matter that arrived earlier has already
    // travelled — so the Remand is asked before the Hearing, and only one moves per enemy turn.
    public static IEffectNode<TContext> MoveOneAppeal<TContext>()
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(Applicant, new StatusDefinitionId(AppealMovedId)),
            Advance<TContext>(1),
            Advance<TContext>(0),
            new RemoveStatusNode<TContext>(Applicant, new StatusDefinitionId(AppealMovedId)),
        ]);

    // The matter at position `from` moves to the next LIVING reed after it. With nothing living downstream
    // it falls out of the chain: refused without a price, and the traveller is handed a licence for the
    // trouble — which is the design's rule for a dead Refusal Reed, said once for every gap.
    private static IEffectNode<TContext> Advance<TContext>(int from)
        where TContext : class
    {
        var source = Reed(Reeds[from].Marker);

        var steps = new List<IEffectNode<TContext>>();
        for (var to = from + 1; to < Reeds.Length; to++)
        {
            var target = Reed(Reeds[to].Marker);
            var blockers = Enumerable.Range(from + 1, to - from - 1)
                .Select(i => (ICombatExpression<TContext, bool>)
                    new NotExpression<TContext>(Standing<TContext>(Reed(Reeds[i].Marker))))
                .DefaultIfEmpty(new ComparisonExpression<TContext>(
                    new ConstantExpression<TContext>(1), ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(1)))
                .Aggregate((a, b) => new AndExpression<TContext>(a, b));

            var arriving = to == Reeds.Length - 1
                ? new CausalSequenceEffectNode<TContext>(
                [
                    TransferClaim<TContext>(source, target, foreign: false),
                    new ApplyStatusNode<TContext>(
                        target, new StatusDefinitionId(AtRefusalId), new ConstantExpression<TContext>(1)),
                ])
                : (IEffectNode<TContext>)TransferClaim<TContext>(source, target, foreign: false);

            steps.Add(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Moved<TContext>()),
                    new AndExpression<TContext>(blockers, Standing<TContext>(target))),
                new CausalSequenceEffectNode<TContext>(
                [
                    arriving,
                    new ApplyStatusNode<TContext>(
                        Applicant, new StatusDefinitionId(AppealMovedId),
                        new ConstantExpression<TContext>(1)),
                ])));
        }

        // Nothing living downstream at all: the matter leaves the chain and the traveller is licensed.
        steps.Add(new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Moved<TContext>()),
                Enumerable.Range(from + 1, Reeds.Length - from - 1)
                    .Select(i => (ICombatExpression<TContext, bool>)
                        new NotExpression<TContext>(Standing<TContext>(Reed(Reeds[i].Marker))))
                    .Aggregate((a, b) => new AndExpression<TContext>(a, b))),
            new CausalSequenceEffectNode<TContext>(
            [
                new ModifySelectedStatusStacksNode<TContext>(
                    source,
                    new StatusSelectionSpec(StatusPolarityFilter.Any)
                    {
                        Definition = new StatusDefinitionId(ClaimId),
                    },
                    new ConstantExpression<TContext>(-1)),
                new ApplyStatusNode<TContext>(
                    Applicant, new StatusDefinitionId(SafeConductId), new ConstantExpression<TContext>(1),
                    sourceSelector: source),
                new ApplyStatusNode<TContext>(
                    Applicant, new StatusDefinitionId(AppealMovedId), new ConstantExpression<TContext>(1)),
            ])));

        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Moved<TContext>()),
                new AndExpression<TContext>(
                    Standing<TContext>(source),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(source, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.Greater, new ConstantExpression<TContext>(0)))),
            new CausalSequenceEffectNode<TContext>(steps));
    }

    private static ICombatExpression<TContext, bool> Moved<TContext>()
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Applicant, new StatusDefinitionId(AppealMovedId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // ── Nothing Ends Here ─────────────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> NothingEndsHere() =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Standing<TurnEndedTriggeredEffectContext>(Reed(RemandReedId)),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Reed(RemandReedId), new StatusDefinitionId(RemandFedId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)));

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? ReedIntent(string enemyId, string intentId)
    {
        var self = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext>? act = $"{enemyId}.{intentId}" switch
        {
            // Hearing Reed.
            "hearing_reed.hear_the_complaint" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                Blow(16), Blow(12)),
            "hearing_reed.take_the_testimony" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(14)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            ]),
            // Remand Reed.
            "remand_reed.send_it_upstream" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                MoveOneAppeal<EnemyActionContext>(),
                Blow(10),
            ]),
            "remand_reed.hold_under_review" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(AppealHeldId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ]),
            // Refusal Reed. Both of its actions begin by refusing whatever has reached it.
            "refusal_reed.no_further_appeal" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                TheFinalRefusal(),
                Blow(18),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        ConsumeClaim<EnemyActionContext>(self),
                        DemandWergild<EnemyActionContext>(self, 2),
                    ])),
            ]),
            // ADAPTATION: a written refusal is a violation on the record, so it files one. Without it the
            // design's "whenever Safe-Conduct prevents Refusal Reed Trespass" would never have anything to
            // answer — nothing else in the tribunal files in the Refusal's name.
            "refusal_reed.written_refusal" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                TheFinalRefusal(),
                Blow(13),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId("doubt"), new ConstantExpression<EnemyActionContext>(1)),
                FileTrespass<EnemyActionContext>(self),
            ]),
            _ => null,
        };

        return act is null ? null : new EffectProgram<EnemyActionContext>(act);
    }

    // "At the beginning of Refusal Reed's next action it may consume that Claim and create Wergild 3."
    private static IEffectNode<EnemyActionContext> TheFinalRefusal()
    {
        var self = CombatantTargetSelectors.Source;

        return new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(AtRefusalId)),
                    ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1))),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                ConsumeClaim<EnemyActionContext>(self),
                DemandWergild<EnemyActionContext>(self, 3),
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(AtRefusalId)),
            ]));
    }

    // "While Refusal Reed has a Claim, whenever Safe-Conduct prevents Refusal Reed Trespass: +6 Block."
    private static IEffectNode<StatusApplicationBlockedTriggeredEffectContext> FinalRefusalGuard() =>
        new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
            new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new StatusDefinitionId(TrespassId)),
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(RefusalReedId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0)),
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            Reed(RefusalReedId), new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1)))),
            new GainBlockNode<StatusApplicationBlockedTriggeredEffectContext>(
                Reed(RefusalReedId),
                new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(6)));
}
