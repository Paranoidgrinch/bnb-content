using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Curator of Misplaced Hours (Act II boss, 278 HP) ──────────────────────────────────────────────────
//
// "The past is evidence. The future is already filed. The present is temporary."
//
// The Curator stands in a clock with three sectors, and its Dial turns after every one of its turns:
//
//   PAST    — it acts out of the record of your LAST turn: how much you played, what you opened with,
//             whether you answered it, whether you hit it hard.
//   PRESENT — it simply acts, here, now. These are the only ordinary attacks in the fight.
//   FUTURE  — it files an attack for later. Up to three sit on the timeline at once, each with a visible
//             countdown, and you can see every one of them coming.
//
// The timeline is the fight. BORROW ONE MINUTE lets you push one filed hour back a turn for 1 Energy — free
// if you have answered a citation — and the Curator spends whole actions pulling them forward again. At
// 139 HP it removes the PRESENT from its own clock and there is nothing left but evidence and schedule.
//
// Scheduled hours are authored as timed statuses on the Curator: the engine ticks a duration down at its
// bearer's turn end and announces the expiry, which is exactly "in N enemy turns" with a number the player
// can read and both sides can move. Deviations: ADAPTATIONS.md.
public static class CuratorOfMisplacedHours
{
    public const string EnemyId = "curator_of_misplaced_hours";

    // On the Curator.
    public const string TheCuratorId = "the_curator_of_misplaced_hours";
    public const string CuratorStateId = "the_archival_clock";
    public const string PresentRemovedPendingId = "the_present_is_removed_called";
    public const string PresentRemovedId = "the_present_is_removed";

    // The filed hours. Each is a countdown the player can read and either side can move.
    public const string ScheduledCollapseId = "scheduled_the_collapse";
    public const string ScheduledFailureId = "scheduled_a_later_failure";
    public const string ScheduledFirstBookingId = "scheduled_first_booking";
    public const string ScheduledSecondBookingId = "scheduled_second_booking";
    public const string ScheduledAuthorityId = "scheduled_tomorrows_authority";
    public const string ScheduledPassageId = "scheduled_future_without_passage";
    public const string ScheduledNearId = "scheduled_the_first_hour";
    public const string ScheduledMiddleId = "scheduled_the_second_hour";
    public const string ScheduledFarId = "scheduled_the_third_hour";

    public const string NoInterventionId = "no_present_intervention";

    // On the player.
    public const string CuratorRulesId = "curator_rules";
    public const string CuratorReferenceId = "curator_citation";
    public const string CuratorReferenceMark = "referenced_by_the_curator";
    public const string FreeAdjustmentId = "free_adjustment";
    public const string BorrowCardId = "borrow_one_minute";

    public static readonly TagId BorrowTag = new("borrowed_minute");

    // The Turn Record, kept on the player: it is the player's own turn that becomes the evidence.
    private static CounterId ActivityCounter => new("curator_activity");
    private static CounterId OpeningCounter => new("curator_opening");         // 0 none 1 Deed 2 Working 3 other
    private static CounterId ComplianceCounter => new("curator_compliance");
    private static CounterId ForceCounter => new("curator_force");

    private static CounterId ReferencesMetCounter => new("curator_references_met");
    private static CounterId LiveOpeningCounter => new("curator_live_opening");
    private static CounterId ReferenceDueCounter => new("curator_reference_due");
    private static CounterId BorrowedCounter => new("curator_borrowed");

    // On the Curator: where the Dial stands, and what has already been spent.
    private static CounterId DialCounter => new("curator_dial");               // 0 PRESENT 1 FUTURE 2 PAST
    private static CounterId ResolvedThisWindowCounter => new("curator_resolved_window");
    private static CounterId LastPastDamageCounter => new("curator_last_past_damage");
    private static CounterId TransitionSpentCounter => new("curator_transition_spent");
    private static CounterId SignatureSpentCounter => new("curator_signature_spent");

    public const int ActivityCeiling = 5;
    public const int ForceThreshold = 20;
    public const int TimelineCapacity = 3;
    public const int MaximumCountdown = 3;
    public const int TransitionHealth = 139;
    public const int SignatureHealth = 70;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // Every hour that can sit on the timeline, in the order the design lists them.
    public static readonly string[] Scheduled =
    [
        ScheduledCollapseId, ScheduledFailureId, ScheduledFirstBookingId, ScheduledSecondBookingId,
        ScheduledAuthorityId, ScheduledPassageId, ScheduledNearId, ScheduledMiddleId, ScheduledFarId,
    ];

    // ── Content ───────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheCuratorId, "The Curator of Misplaced Hours",
            "Its dial shows which hour it is working in."),
        Marker(PresentRemovedPendingId, "The Present Is Removed",
            "Its next action takes the present hour off the clock."),
        Marker(PresentRemovedId, "Past and Future Only",
            "There is no present left to act in."),
        Marker(NoInterventionId, "No Present Intervention",
            "The nearest filed hour cannot be borrowed against."),

        Filed(ScheduledCollapseId, "Scheduled: The Collapse", "22 damage, when this runs out."),
        Filed(ScheduledFailureId, "Appointed: A Later Failure", "14 damage and 1 Paperwork, when this runs out."),
        Filed(ScheduledFirstBookingId, "Double-Booked: First", "10 damage, when this runs out."),
        Filed(ScheduledSecondBookingId, "Double-Booked: Second", "10 damage, when this runs out."),
        Filed(ScheduledAuthorityId, "Reserved: Tomorrow's Authority", "The Curator gains 3 Strength."),
        Filed(ScheduledPassageId, "Filed: Future Without Passage", "24 damage and 1 Paperwork."),
        Filed(ScheduledNearId, "All Hours Belong Elsewhere: the first", "10 damage."),
        Filed(ScheduledMiddleId, "All Hours Belong Elsewhere: the second", "17 damage."),
        Filed(ScheduledFarId, "All Hours Belong Elsewhere: the third", "25 damage."),

        FreeAdjustment(),

        // "When a Curator Reference is fulfilled, the player gains Free Adjustment (maximum 1)." Answering the
        // archive is what buys you a minute — the fight's one renewable currency.
        ActTwo.Reference(CuratorReferenceId, "Curator Citation", CuratorReferenceMark,
            "The Curator has cited this card. Play it and a minute is properly accounted for.",
            cite: CiteWhatIsDue(),
            onFulfilled: OnCitationAnswered()),

        Rules(),
        ArchivalClock(),
    ];

    public static IEnumerable<CardData> Cards() => [BorrowOneMinute()];

    // ── The Turn Record ───────────────────────────────────────────────────────────────────────────────────
    //
    // Written at the END of the player's turn — the only moment that knows the whole turn — and read by every
    // PAST action. The engine's own per-turn stats survive it: they reset on the combatant's TURN START, so
    // both the count of cards played and the damage dealt are still standing when the turn ends.
    private static StatusData Rules()
    {
        var onDraw = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                // One minute to spend each turn, and only ever one: a fresh card only where none survives.
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        LiveMinutes<CardsDrawnTriggeredEffectContext>(), ComparisonOperator.Equal,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CreateCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self, new CardDefinitionId(BorrowCardId), CardZone.Hand,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))));

        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                RecordTheOpening()));

        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                IsTheApplicant<TurnStartedTriggeredEffectContext>(),
                SetOn<TurnStartedTriggeredEffectContext>(Self, BorrowedCounter, 0)));

        var onTurnEnded = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                IsTheApplicant<TurnEndedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    // Activity, capped at 5 as the design records it.
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        Self, ActivityCounter,
                        new MinExpression<TurnEndedTriggeredEffectContext>(
                            new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Self),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(ActivityCeiling)),
                        relative: false),
                    Copy(OpeningCounter, LiveOpeningCounter),
                    Copy(ComplianceCounter, ReferencesMetCounter),
                    // Force: "at least 20 direct card damage dealt to the Curator" this turn.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new DamageDealtThisTurnExpression<TurnEndedTriggeredEffectContext>(Self),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(ForceThreshold)),
                        SetOn<TurnEndedTriggeredEffectContext>(Self, ForceCounter, 1),
                        @else: SetOn<TurnEndedTriggeredEffectContext>(Self, ForceCounter, 0)),
                    SetOn<TurnEndedTriggeredEffectContext>(Self, LiveOpeningCounter, 0),
                    SetOn<TurnEndedTriggeredEffectContext>(Self, ReferencesMetCounter, 0),
                ])));

        return Rule(CuratorRulesId, "Three Hours Exist",
            "What you did last turn is evidence. What it has filed is already coming. Only now is temporary.",
            [
                Watch("CardsDrawn", onDraw),
                Watch("CardPlayed", onPlay),
                Watch("TurnStarted", onTurnStarted),
                Watch("TurnEnded", onTurnEnded),
            ]);
    }

    // "The type of the first non-Junk card." As with every Act-II record, the taxonomy is this game's:
    // Deed / Working / Rite-and-anything-else, and Junk fills no slot.
    private static IEffectNode<CardPlayedTriggeredEffectContext> RecordTheOpening()
    {
        IEffectNode<CardPlayedTriggeredEffectContext> Note(string tag, int value) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                PlayedCardHasTag(tag),
                SetOn<CardPlayedTriggeredEffectContext>(Self, LiveOpeningCounter, value));

        return new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                IsZero<CardPlayedTriggeredEffectContext>(Self, LiveOpeningCounter),
                new NotExpression<CardPlayedTriggeredEffectContext>(PlayedCardHasTag(CardAuthoring.JunkTag))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                SetOn<CardPlayedTriggeredEffectContext>(Self, LiveOpeningCounter, 3),
                Note(CardAuthoring.DeedTag, 1),
                Note(CardAuthoring.WorkingTag, 2),
            ]));
    }

    // ── Borrow One Minute ─────────────────────────────────────────────────────────────────────────────────
    //
    // "Once per player turn: pay 1 Energy; choose one Scheduled Intent and delay it by +1 enemy turn. If a
    // Free Adjustment exists it costs 0 and is consumed instead. Maximum countdown 3."
    //
    // ADAPTATION: the choice is over the hours the Curator can actually have filed, offered as a list. The
    // engine has no picker over a set of statuses, and an option list is what every other Act-II choice uses.
    // An option naming an hour that is not filed resolves to nothing — the same answer a card played into an
    // empty board gets everywhere in this act.
    private static CardData BorrowOneMinute() => new()
    {
        Id = BorrowCardId,
        NameKey = "Borrow One Minute",
        DescriptionKey =
            "Once per turn: push one filed hour back by 1 turn, to a maximum countdown of 3. "
            + "Free while you hold a Free Adjustment.",
        Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
        Tags = [BorrowTag, new TagId(CardAuthoring.WorkingTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                IsZero<CardPlayContext>(CombatantTargetSelectors.Source, BorrowedCounter),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    SetOn<CardPlayContext>(CombatantTargetSelectors.Source, BorrowedCounter, 1),
                    // The Free Adjustment pays for it where there is one; the Energy has been taken either
                    // way, so what the Adjustment really buys is the NEXT minute.
                    new ConditionalEffectNode<CardPlayContext>(
                        new TargetHasStatusExpression<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(FreeAdjustmentId)),
                        new CausalSequenceEffectNode<CardPlayContext>(
                        [
                            new RemoveStatusNode<CardPlayContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(FreeAdjustmentId)),
                            new ModifyResourceNode<CardPlayContext>(
                                CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                                new ConstantExpression<CardPlayContext>(1)),
                        ])),
                    new ChooseOptionsNode<CardPlayContext>(
                        [.. Scheduled.Select(Delay)],
                        [.. Scheduled.Select(HourLabel)],
                        count: 1, purpose: "which hour do you push back"),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    // "+1 enemy turn, and Borrow One Minute cannot create countdown 4+." A filed hour under No Present
    // Intervention refuses to move at all, which is what that intent is for.
    private static IEffectNode<CardPlayContext> Delay(string hour)
    {
        var curator = CombatantTargetSelectors.LowestHealthEnemyOfSource;

        return new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusDurationExpression<CardPlayContext>(
                        curator, new StatusDefinitionId(hour)),
                    ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                new AndExpression<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantStatusDurationExpression<CardPlayContext>(
                            curator, new StatusDefinitionId(hour)),
                        ComparisonOperator.Less, new ConstantExpression<CardPlayContext>(MaximumCountdown)),
                    new NotExpression<CardPlayContext>(Protected(hour)))),
            new ModifyStatusDurationNode<CardPlayContext>(
                curator, new StatusDefinitionId(hour), new ConstantExpression<CardPlayContext>(1)));
    }

    // The hour the Curator has put beyond reach: the one it named while it was the nearest.
    private static ICombatExpression<CardPlayContext, bool> Protected(string hour) =>
        new AndExpression<CardPlayContext>(
            new TargetHasStatusExpression<CardPlayContext>(
                CombatantTargetSelectors.LowestHealthEnemyOfSource, new StatusDefinitionId(NoInterventionId)),
            new ComparisonExpression<CardPlayContext>(
                new CombatantStatusDurationExpression<CardPlayContext>(
                    CombatantTargetSelectors.LowestHealthEnemyOfSource, new StatusDefinitionId(hour)),
                ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(1)));

    private static string HourLabel(string hour) => hour switch
    {
        ScheduledCollapseId => "push back the Collapse",
        ScheduledFailureId => "push back the Later Failure",
        ScheduledFirstBookingId => "push back the first Double-Booking",
        ScheduledSecondBookingId => "push back the second Double-Booking",
        ScheduledAuthorityId => "push back Tomorrow's Authority",
        ScheduledPassageId => "push back the Future Without Passage",
        ScheduledNearId => "push back the first of the Last Hours",
        ScheduledMiddleId => "push back the second of the Last Hours",
        _ => "push back the third of the Last Hours",
    };

    private static ICombatExpression<TContext, int> LiveMinutes<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new CombatantZoneCardCountExpression<TContext>(Self, CardZone.Hand, BorrowTag),
            new AddExpression<TContext>(
                new CombatantZoneCardCountExpression<TContext>(Self, CardZone.DrawPile, BorrowTag),
                new CombatantZoneCardCountExpression<TContext>(Self, CardZone.DiscardPile, BorrowTag)));

    // ── The clock ─────────────────────────────────────────────────────────────────────────────────────────
    //
    // The Dial turns after every Curator turn, the filed hours come due at its turn end, and the two
    // thresholds are checked where the Curator can see its own body.
    private static StatusData ArchivalClock()
    {
        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                SetOn<TurnStartedTriggeredEffectContext>(Self, ResolvedThisWindowCounter, 0),
                // "Trigger: Curator reaches 139 HP or less."
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        IsZero<TurnStartedTriggeredEffectContext>(Self, TransitionSpentCounter),
                        Below<TurnStartedTriggeredEffectContext>(TransitionHealth)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(PresentRemovedPendingId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, TransitionSpentCounter, 1),
                    ])),
                // Final Signature — "All Hours Belong Elsewhere", at 70 HP, once per combat. It replaces what
                // is on the timeline rather than adding to it: no capacity above 3 is created.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        IsZero<TurnStartedTriggeredEffectContext>(Self, SignatureSpentCounter),
                        Below<TurnStartedTriggeredEffectContext>(SignatureHealth)),
                    AllHoursBelongElsewhere()),
            ]));

        // A filed hour has run out. Which one decides what happens, and two in one window cost a Paperwork.
        var onExpired = new EffectProgram<StatusExpiredTriggeredEffectContext>(
            new CausalSequenceEffectNode<StatusExpiredTriggeredEffectContext>(
            [
                Due(ScheduledCollapseId, Hit(22)),
                Due(ScheduledFailureId, new CausalSequenceEffectNode<StatusExpiredTriggeredEffectContext>(
                    [Hit(14), Paperwork(1)])),
                Due(ScheduledFirstBookingId, Hit(10)),
                Due(ScheduledSecondBookingId, Hit(10)),
                Due(ScheduledAuthorityId, new ApplyStatusNode<StatusExpiredTriggeredEffectContext>(
                    Self, new StatusDefinitionId("strength"),
                    new ConstantExpression<StatusExpiredTriggeredEffectContext>(3))),
                Due(ScheduledPassageId, new CausalSequenceEffectNode<StatusExpiredTriggeredEffectContext>(
                    [Hit(24), Paperwork(1)])),
                Due(ScheduledNearId, Hit(10)),
                Due(ScheduledMiddleId, Hit(17)),
                Due(ScheduledFarId, Hit(25)),
                // Temporal Overlap: "if at least two resolve in the same window, apply 1 Paperwork after both
                // resolve; maximum once per window."
                new ConditionalEffectNode<StatusExpiredTriggeredEffectContext>(
                    CounterIs<StatusExpiredTriggeredEffectContext>(Self, ResolvedThisWindowCounter, 2),
                    Paperwork(1)),
            ]));

        var onDowned = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                // 7.8: nothing filed resolves after the combat ends, and no minute is owed to anyone.
                .. Scheduled.Select(h => (IEffectNode<CombatantDownedTriggeredEffectContext>)
                    new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(h))),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(Across,
                    new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(FreeAdjustmentId))),
            ]));

        return Rule(CuratorStateId, "The Archival Clock",
            "Its dial turns after every one of its turns, and what it has filed comes due whether you are "
            + "ready or not.",
            [
                Watch("TurnStarted", onTurnStarted),
                Watch("StatusExpired", onExpired),
                Watch("Downed", onDowned),
            ]);
    }

    private static IEffectNode<StatusExpiredTriggeredEffectContext> Due(
        string hour, IEffectNode<StatusExpiredTriggeredEffectContext> payload) =>
        new ConditionalEffectNode<StatusExpiredTriggeredEffectContext>(
            new TriggerEventStatusIsExpression<StatusExpiredTriggeredEffectContext>(new StatusDefinitionId(hour)),
            new CausalSequenceEffectNode<StatusExpiredTriggeredEffectContext>(
            [
                payload,
                Bump<StatusExpiredTriggeredEffectContext>(Self, ResolvedThisWindowCounter, 1),
            ]));

    // "Replace non-signature timeline entries as necessary, then schedule 10 / 17 / 25 at 1 / 2 / 3 turns.
    // The player gains 1 Free Adjustment."
    private static IEffectNode<TurnStartedTriggeredEffectContext> AllHoursBelongElsewhere() =>
        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            .. Scheduled.Take(6).Select(h => (IEffectNode<TurnStartedTriggeredEffectContext>)
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Self, new StatusDefinitionId(h))),
            File<TurnStartedTriggeredEffectContext>(ScheduledNearId, 1),
            File<TurnStartedTriggeredEffectContext>(ScheduledMiddleId, 2),
            File<TurnStartedTriggeredEffectContext>(ScheduledFarId, 3),
            GrantAdjustment<TurnStartedTriggeredEffectContext>(),
            SetOn<TurnStartedTriggeredEffectContext>(Self, SignatureSpentCounter, 1),
        ]);

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // Five slots, and the Dial decides what each one means. A slot reads as a PAST action, a PRESENT action or
    // a FUTURE filing depending on where the clock stands — which is the whole boss: the same five moves mean
    // three different things, and you can see which is coming.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "immediate_correction" => Hours(
            // PAST — Repeat the Recorded Effort: 8 + 2 per recorded Activity, at most 18.
            new DealDamageNode<EnemyActionContext>(Across,
                new MinExpression<EnemyActionContext>(
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(8),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(2), Recorded(ActivityCounter))),
                    new ConstantExpression<EnemyActionContext>(18))),
            // PRESENT — Immediate Correction.
            Strike(17),
            // FUTURE — Schedule the Collapse.
            File<EnemyActionContext>(ScheduledCollapseId, 2)),

        "seize_the_current_hour" => Hours(
            // PAST — Reopen the First Procedure, which answers what you opened with.
            ReopenTheFirstProcedure(),
            // PRESENT — Seize the Current Hour.
            new CausalSequenceEffectNode<EnemyActionContext>([Strike(12), ApplyToPlayer(Keywords.Paperwork, 1)]),
            // FUTURE — Appoint a Later Failure.
            File<EnemyActionContext>(ScheduledFailureId, 1)),

        "hold_the_present_open" => Hours(
            // PAST — File the Successful Method: what worked on it gets misfiled out of your way.
            FileTheSuccessfulMethod(),
            // PRESENT — Hold the Present Open.
            new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(20)),
            // FUTURE — Double-Book the Outcome, but only where two slots are free.
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    Filed(), ComparisonOperator.LessOrEqual,
                    new ConstantExpression<EnemyActionContext>(TimelineCapacity - 2)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    File<EnemyActionContext>(ScheduledFirstBookingId, 1),
                    File<EnemyActionContext>(ScheduledSecondBookingId, 2),
                ]),
                @else: File<EnemyActionContext>(ScheduledFirstBookingId, 1))),

        "the_only_moment_that_hurts" => Hours(
            // PAST — The Past Was Not Settled: it collects on an unanswered citation, or pays for a settled one.
            new ConditionalEffectNode<EnemyActionContext>(
                Recorded(ComplianceCounter, ComparisonOperator.Greater, 0),
                StripBlock(8),
                @else: ApplyToPlayer(ActTwo.OverdueId, 1)),
            // PRESENT — The Only Moment That Hurts.
            Strike(22),
            // FUTURE — Reserve Tomorrow's Authority.
            File<EnemyActionContext>(ScheduledAuthorityId, 2)),

        "schedule_the_collapse" => Hours(
            // PAST — Ask What You Are Doing Now belongs to the present; the past asks instead for the citation
            // it never got, which is the same beat one hour earlier.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(), Block(8)]),
            // PRESENT — Ask What You Are Doing Now.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(), Block(8)]),
            // FUTURE — Future Without Passage.
            File<EnemyActionContext>(ScheduledPassageId, 2)),

        _ => null,
    };

    // One slot, three hours — and the single action that takes the present off the clock. "Preserve the Turn
    // Record; preserve Scheduled Intents; gain 12 Block; no attack." The Dial turns after every action, which
    // is what makes the next hour readable a whole turn ahead.
    private static EffectProgram<EnemyActionContext> Hours(
        IEffectNode<EnemyActionContext> past,
        IEffectNode<EnemyActionContext> present,
        IEffectNode<EnemyActionContext> future) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                SelfHas<EnemyActionContext>(PresentRemovedPendingId),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Block(12),
                    new RemoveStatusNode<EnemyActionContext>(
                        Self, new StatusDefinitionId(PresentRemovedPendingId)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Self, new StatusDefinitionId(PresentRemovedId),
                        new ConstantExpression<EnemyActionContext>(1)),
                    // The clock is left standing on the PAST, so the very next action is already readable.
                    SetOn<EnemyActionContext>(Self, DialCounter, 2),
                ]),
                @else: new ConditionalEffectNode<EnemyActionContext>(
                    DialIs(0), present,
                    @else: new ConditionalEffectNode<EnemyActionContext>(DialIs(1), future, @else: past))),
            TurnTheDial(),
        ]));

    // PAST → PRESENT → FUTURE, and once the present is gone, PAST ↔ FUTURE.
    private static IEffectNode<EnemyActionContext> TurnTheDial() =>
        new ConditionalEffectNode<EnemyActionContext>(
            SelfHas<EnemyActionContext>(PresentRemovedId),
            new ConditionalEffectNode<EnemyActionContext>(
                DialIs(2), SetOn<EnemyActionContext>(Self, DialCounter, 1),
                @else: SetOn<EnemyActionContext>(Self, DialCounter, 2)),
            @else: new SetCombatantCounterNode<EnemyActionContext>(
                Self, DialCounter,
                new RemainderExpression<EnemyActionContext>(
                    new AddExpression<EnemyActionContext>(
                        new CombatantCounterExpression<EnemyActionContext>(Self, DialCounter),
                        new ConstantExpression<EnemyActionContext>(1)),
                    new ConstantExpression<EnemyActionContext>(3)),
                relative: false));

    private static ICombatExpression<EnemyActionContext, bool> DialIs(int sector) =>
        CounterIs<EnemyActionContext>(Self, DialCounter, sector);

    // "Based on the recorded Opening: Attack → 16 damage. Skill → 11 damage + 11 Block. Power/Other → 10
    // damage + 2 Strength." Read against this game's taxonomy: Deed, Working, Rite-and-anything-else.
    private static IEffectNode<EnemyActionContext> ReopenTheFirstProcedure() =>
        new ConditionalEffectNode<EnemyActionContext>(
            Recorded(OpeningCounter, ComparisonOperator.Equal, 1),
            Strike(16),
            @else: new ConditionalEffectNode<EnemyActionContext>(
                Recorded(OpeningCounter, ComparisonOperator.Equal, 2),
                new CausalSequenceEffectNode<EnemyActionContext>([Strike(11), Block(11)]),
                @else: new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Strike(10),
                    new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId("strength"),
                        new ConstantExpression<EnemyActionContext>(2)),
                ])));

    // "If recorded Force is true: after the next normal draw, misfile one card matching the previous Opening
    // type if possible, another otherwise. If Force is false: deal 11 damage instead."
    //
    // ADAPTATION: the misfiling goes on a card in hand NOW rather than after the next draw. The Curator acts
    // on its own turn, when the player's hand is already down — a mark written then would be written on cards
    // about to be discarded. The act's own misfiling beat puts the mark on the draw pile, which is where a
    // misfiling can still cost the player something, and that is the beat used here.
    private static IEffectNode<EnemyActionContext> FileTheSuccessfulMethod() =>
        new ConditionalEffectNode<EnemyActionContext>(
            Recorded(ForceCounter, ComparisonOperator.Greater, 0),
            ActTwo.MisfileOne(ActTwo.MisfiledMark),
            @else: Strike(11));

    // ── Filing an hour ────────────────────────────────────────────────────────────────────────────────────
    //
    // A filed hour is a timed status on the Curator: the engine counts it down at the Curator's own turn end
    // and announces the expiry, which is precisely "in N enemy turns" — with a number on the table that the
    // player can read and that Borrow One Minute and Curate the Outcome can both move.
    private static IEffectNode<TContext> File<TContext>(string hour, int turns) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                Filed<TContext>(), ComparisonOperator.Less,
                new ConstantExpression<TContext>(TimelineCapacity)),
            new ApplyStatusNode<TContext>(
                Self, new StatusDefinitionId(hour), new ConstantExpression<TContext>(1), durationTurns: turns));

    // How many hours are on the timeline. Never more than three.
    private static ICombatExpression<TContext, int> Filed<TContext>() where TContext : class =>
        Scheduled.Aggregate(
            (ICombatExpression<TContext, int>)new ConstantExpression<TContext>(0),
            (total, hour) => new AddExpression<TContext>(total,
                new MinExpression<TContext>(
                    new ConstantExpression<TContext>(1),
                    new CombatantStatusDurationExpression<TContext>(Self, new StatusDefinitionId(hour)))));

    private static ICombatExpression<EnemyActionContext, int> Filed() => Filed<EnemyActionContext>();

    // ── Citations ─────────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<EnemyActionContext> CiteLater() =>
        Bump<EnemyActionContext>(Across, ReferenceDueCounter, 1);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteWhatIsDue() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                Positive<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    Self, CardZone.Hand,
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                        new TagId(CuratorReferenceMark)),
                    takeFirst: 1),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter, 0),
            ]));

    // "A Minute Properly Accounted For": answering the archive is what buys the next minute.
    private static IEffectNode<CardPlayedTriggeredEffectContext> OnCitationAnswered() =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Bump<CardPlayedTriggeredEffectContext>(Self, ReferencesMetCounter, 1),
            GrantAdjustmentTo<CardPlayedTriggeredEffectContext>(Self),
        ]);

    private static StatusData FreeAdjustment() => new()
    {
        Id = FreeAdjustmentId,
        NameKey = "Free Adjustment",
        DescriptionKey = "One minute you do not have to pay for.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Maximum 1 — which a status that does not stack enforces by construction.
    private static IEffectNode<TContext> GrantAdjustment<TContext>() where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new ApplyStatusNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(FreeAdjustmentId),
                new ConstantExpression<TContext>(1)));

    private static IEffectNode<TContext> GrantAdjustmentTo<TContext>(ICombatantTargetSelector who)
        where TContext : class =>
        new ApplyStatusNode<TContext>(who, new StatusDefinitionId(FreeAdjustmentId),
            new ConstantExpression<TContext>(1));

    // ── Small shared pieces ───────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<EnemyActionContext, int> Recorded(CounterId counter) =>
        new CombatantCounterExpression<EnemyActionContext>(Across, counter);

    private static ICombatExpression<EnemyActionContext, bool> Recorded(
        CounterId counter, ComparisonOperator op, int value) =>
        new ComparisonExpression<EnemyActionContext>(
            Recorded(counter), op, new ConstantExpression<EnemyActionContext>(value));

    private static IEffectNode<EnemyActionContext> StripBlock(int amount) =>
        new ForEachTargetEffectNode<EnemyActionContext>(Self,
            new ModifyDefensivePoolNode<EnemyActionContext>(
                CombatantTargetSelectors.IterationTarget, StandardCombatIds.BlockDefensivePool,
                new NegateExpression<EnemyActionContext>(
                    new MinExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(amount),
                        new CombatantDefensivePoolExpression<EnemyActionContext>(
                            Self, StandardCombatIds.BlockDefensivePool)))));

    private static IEffectNode<EnemyActionContext> Strike(int amount) =>
        new DealDamageNode<EnemyActionContext>(Across, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<StatusExpiredTriggeredEffectContext> Hit(int amount) =>
        new DealDamageNode<StatusExpiredTriggeredEffectContext>(
            Across, new ConstantExpression<StatusExpiredTriggeredEffectContext>(amount));

    private static IEffectNode<StatusExpiredTriggeredEffectContext> Paperwork(int stacks) =>
        new ApplyStatusNode<StatusExpiredTriggeredEffectContext>(
            Across, new StatusDefinitionId(Keywords.Paperwork),
            new ConstantExpression<StatusExpiredTriggeredEffectContext>(stacks));

    private static IEffectNode<EnemyActionContext> Block(int amount) =>
        new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> ApplyToPlayer(string status, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(status),
            new ConstantExpression<EnemyActionContext>(stacks));

    private static ICombatExpression<TContext, bool> SelfHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> Below<TContext>(int health) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(Self),
            ComparisonOperator.LessOrEqual, new ConstantExpression<TContext>(health));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedCardHasTag(string tag) =>
        new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(tag));

    private static ICombatExpression<TContext, bool> Positive<TContext>(
        ICombatantTargetSelector on, CounterId counter) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(on, counter),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> CounterIs<TContext>(
        ICombatantTargetSelector on, CounterId counter, int value) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(on, counter),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(value));

    private static ICombatExpression<TContext, bool> IsZero<TContext>(
        ICombatantTargetSelector on, CounterId counter) where TContext : class =>
        CounterIs<TContext>(on, counter, 0);

    private static IEffectNode<TContext> SetOn<TContext>(
        ICombatantTargetSelector on, CounterId counter, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(value), relative: false);

    private static IEffectNode<TContext> Bump<TContext>(
        ICombatantTargetSelector on, CounterId counter, int delta) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(delta), relative: true);

    private static IEffectNode<TurnEndedTriggeredEffectContext> Copy(CounterId into, CounterId from) =>
        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
            Self, into, new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, from),
            relative: false);

    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    // A filed hour: it carries a duration, which is the countdown the whole fight negotiates over.
    private static StatusData Filed(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        UsesDuration = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData Marker(string id, string name, string description) =>
        Rule(id, name, description, []);

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
