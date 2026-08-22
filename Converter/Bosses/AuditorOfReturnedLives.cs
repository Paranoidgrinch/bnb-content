using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Auditor of Returned Lives (Act II boss, 288 HP) ───────────────────────────────────────────────────
//
// "The damage may be real. The record may still be insufficient."
//
// The Auditor does not argue about whether it is dying. It asks whether the death can be DOCUMENTED. Three
// times — at 216, 144 and 72 HP — an Account comes due, and the player gets one full turn to answer it:
//
//   SUBMIT DOCUMENTATION — spend 2 of the Supporting Documentation its own citations hand you. Account closed.
//   WITHHOLD             — spend nothing, take a Discrepancy, and live with what that Account does unclosed.
//
// Discrepancy makes it hit harder for the rest of the fight. But the Account of Life is the one that matters:
// leave it unreconciled and the Auditor's death is not final — it returns once at 72 HP into a Closing Audit,
// unless you are holding the two Documentation the final receipt costs when the lethal blow lands.
//
// The Auditor is therefore the one Act-II boss that hands EVERY deck a way to buy its own ending, and the
// whole fight is about whether you spend that currency early to be safe or hoard it to be finished.
// Deviations: ADAPTATIONS.md.
public static class AuditorOfReturnedLives
{
    public const string EnemyId = "auditor_of_returned_lives";

    // On the Auditor.
    public const string TheAuditorId = "the_auditor_of_returned_lives";
    public const string AuditorStateId = "the_sealed_accounts";
    public const string DiscrepancyId = "discrepancy";
    public const string AuditPendingId = "audit_pending";
    public const string AuditApproachingId = "audit_approaching";
    public const string FormalReconciliationId = "formal_reconciliation";
    public const string ClosingAuditId = "closing_audit";
    public const string DeathClauseId = "account_still_open";
    public const string SuspendedId = "the_account_is_suspended";

    // On the player.
    public const string AuditorRulesId = "auditor_rules";
    public const string DocumentationId = "supporting_documentation";
    public const string AuditorReferenceId = "auditor_citation";
    public const string AuditorReferenceMark = "referenced_by_the_auditor";

    // Which Account is on the table, and how far the audit has got.
    private static readonly CounterId AccountCounter = new("auditor_account");           // 0 none 1 Identity 2 Obligation 3 Life
    private static readonly CounterId AccountsQueuedCounter = new("auditor_accounts_queued");
    private static readonly CounterId AnsweredCounter = new("auditor_answered");         // 0 unanswered 1 submitted 2 withheld
    private static readonly CounterId LifeUnreconciledCounter = new("auditor_life_open");
    private static readonly CounterId ClauseSpentCounter = new("auditor_clause_spent");

    // On the player: the citation ledger.
    private static readonly CounterId ReferenceDueCounter = new("auditor_reference_due");
    private static readonly CounterId AnsweredLastTurnCounter = new("auditor_answered_last_turn");
    private static readonly CounterId AnsweredThisTurnCounter = new("auditor_answered_this_turn");
    private static readonly CounterId DelayedDocumentationCounter = new("auditor_delayed_documentation");
    private static readonly CounterId IdentityPenaltyCounter = new("auditor_identity_penalty");

    public const int DocumentationMaximum = 6;
    public const int DocumentationPerAccount = 2;
    public const int DiscrepancyMaximum = 3;
    public const int IdentityHealth = 216;
    public const int ObligationHealth = 144;
    public const int LifeHealth = 72;
    public const int AnnouncementMargin = 29;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // The three Accounts, each with the body-count that calls it.
    private static readonly (int Account, int Health)[] Accounts =
    [
        (1, IdentityHealth), (2, ObligationHealth), (3, LifeHealth),
    ];

    // ── Content ───────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheAuditorId, "The Auditor of Returned Lives",
            "It is not asking whether you can kill it."),
        Stacking(DocumentationId, "Supporting Documentation",
            "What its own citations have paid you. Two closes an Account."),
        Stacking(DiscrepancyId, "Discrepancy",
            "An Account left unreconciled. It makes the Auditor's case harder."),
        Marker(AuditPendingId, "Audit Pending",
            "An Account comes due at its next action. Answer it this turn."),
        Marker(AuditApproachingId, "Audit Approaching",
            "An Account is close."),
        Marker(FormalReconciliationId, "Formal Reconciliation",
            "The Account of Obligation has resolved."),
        Marker(ClosingAuditId, "Closing Audit",
            "It came back, and no life may remain unbalanced."),
        Marker(SuspendedId, "The Account Is Suspended",
            "Documentation you earn now is held until the Auditor's next turn. It is not lost."),

        DeathClause(),

        // 8.2: "Whenever the player fulfils a Reference issued by the Auditor, gain 1 Supporting
        // Documentation." This is the universal path the design promises every deck.
        ActTwo.Reference(AuditorReferenceId, "Auditor Citation", AuditorReferenceMark,
            "The Auditor has cited this card. Play it and the record grows by one line.",
            cite: CiteWhatIsDue(),
            onFulfilled: OnCitationAnswered()),

        Rules(),
        SealedAccounts(),
    ];

    // ── The player's side of the audit ────────────────────────────────────────────────────────────────────
    //
    // The Documentation is the player's, the citations are answered from the player's hand, and the answer to
    // an Account is the player's to give — so the player carries these rules.
    private static StatusData Rules()
    {
        var onDraw = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // A suspended gain was never lost — it simply waited for the Auditor's turn to begin.
                    ReleaseDelayedDocumentation(),
                    // 8.6: the Account of Identity's one penalty falls on the hand after the crossing.
                    ServeTheIdentityPenalty(),
                    // 8.4: the response window. One full player turn to close the Account or refuse to.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        AcrossHas<CardsDrawnTriggeredEffectContext>(AuditPendingId),
                        AnswerTheAccount()),
                ])));

        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                IsTheApplicant<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    Copy<TurnStartedTriggeredEffectContext>(AnsweredLastTurnCounter, AnsweredThisTurnCounter),
                    SetOn<TurnStartedTriggeredEffectContext>(Self, AnsweredThisTurnCounter, 0),
                ])));

        // 8.13: a prevention interceptor cannot ask a question, so the Death Clause's condition is expressed
        // by whether the clause is ON the Auditor at all — and the player's own record keeps it in step. Hold
        // the two Documentation the final receipt costs and the death takes; hold fewer and it comes back.
        var onStatusChanged = new EffectProgram<StatusStacksChangedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusStacksChangedTriggeredEffectContext>(
                IsTheApplicant<StatusStacksChangedTriggeredEffectContext>(),
                SyncTheClause<StatusStacksChangedTriggeredEffectContext>()));

        return Rule(AuditorRulesId, "The Record",
            "Its citations pay you in Documentation, and Documentation is what closes an Account — or buys "
            + "the receipt that makes its death final.",
            [
                Watch("CardsDrawn", onDraw),
                Watch("TurnStarted", onTurnStarted),
                Watch("StatusStacksChanged", onStatusChanged),
            ]);
    }

    // 8.4: "The choice is visible and voluntary if enough Documentation exists." With fewer than two there is
    // nothing to submit, so the Account goes unreconciled without asking.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> AnswerTheAccount() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                Documentation<CardsDrawnTriggeredEffectContext>(),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(DocumentationPerAccount)),
            new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                [Submit(), Withhold()],
                ["submit 2 Documentation and close the Account", "withhold — take the Discrepancy"],
                count: 1, purpose: "an Account of the Auditor comes due"),
            @else: Withhold());

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Submit() =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new ModifyStatusStacksNode<CardsDrawnTriggeredEffectContext>(
                Self, new StatusDefinitionId(DocumentationId),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(-DocumentationPerAccount)),
            SetOn<CardsDrawnTriggeredEffectContext>(Across, AnsweredCounter, 1),
        ]);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Withhold() =>
        SetOn<CardsDrawnTriggeredEffectContext>(Across, AnsweredCounter, 2);

    // ── The Auditor's own accounts ────────────────────────────────────────────────────────────────────────

    private static StatusData SealedAccounts()
    {
        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // A suspension lasts until the Auditor's turn begins, which is now.
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(SuspendedId)),
                // 8.3: each threshold is announced about 29 HP before it is reached.
                Announcement(),
                // 8.12: only the NEXT relevant Account is queued, however big the packet was — later
                // thresholds wait until this one has resolved, so one burst turn cannot skip the audit.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new NotExpression<TurnStartedTriggeredEffectContext>(
                        SelfHas<TurnStartedTriggeredEffectContext>(AuditPendingId)),
                    QueueTheNextAccount()),
            ]));

        return Rule(AuditorStateId, "The Sealed Accounts",
            "Three Accounts come due as its body goes, and each one is answered or it is not.",
            [Watch("TurnStarted", onTurnStarted)]);
    }

    private static IEffectNode<TurnStartedTriggeredEffectContext> Announcement() =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Accounts.Aggregate(
                (ICombatExpression<TurnStartedTriggeredEffectContext, bool>)
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                (any, a) => new OrExpression<TurnStartedTriggeredEffectContext>(any,
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        NotYetQueued<TurnStartedTriggeredEffectContext>(a.Account),
                        Below<TurnStartedTriggeredEffectContext>(a.Health + AnnouncementMargin)))),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Self, new StatusDefinitionId(AuditApproachingId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

    private static IEffectNode<TurnStartedTriggeredEffectContext> QueueTheNextAccount() =>
        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            .. Accounts.Select(a => (IEffectNode<TurnStartedTriggeredEffectContext>)
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        NotYetQueued<TurnStartedTriggeredEffectContext>(a.Account),
                        Below<TurnStartedTriggeredEffectContext>(a.Health)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        SetOn<TurnStartedTriggeredEffectContext>(Self, AccountCounter, a.Account),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, AccountsQueuedCounter, a.Account),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, AnsweredCounter, 0),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(AuditPendingId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(AuditApproachingId)),
                    ]))),
        ]);

    private static ICombatExpression<TContext, bool> NotYetQueued<TContext>(int account)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(Self, AccountsQueuedCounter),
            ComparisonOperator.Less, new ConstantExpression<TContext>(account));

    // 8.11: "When an Account resolves, the Auditor does not perform an additional damaging intent in that
    // same boss window. Audit resolution IS the boss-state action." So the resolution replaces whatever the
    // cycle would otherwise have done — no threshold ever costs the player a burst on top.
    private static IEffectNode<EnemyActionContext> ResolveTheAccount() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                CounterIs<EnemyActionContext>(Self, AnsweredCounter, 1),
                // Closed: no penalty at all, whichever Account it was.
                new NoOpEffectNode<EnemyActionContext>(),
                @else: Unreconciled()),
            new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(AuditPendingId)),
            // The Account of Obligation resolving is what opens Formal Reconciliation, closed or not.
            new ConditionalEffectNode<EnemyActionContext>(
                CounterIs<EnemyActionContext>(Self, AccountCounter, 2),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(Self,
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(6), Discrepancy())),
                    new ApplyStatusNode<EnemyActionContext>(
                        Self, new StatusDefinitionId(FormalReconciliationId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ])),
            SetOn<EnemyActionContext>(Self, AccountCounter, 0),
        ]);

    // 8.5–8.8: every unreconciled Account is a Discrepancy, and each one has its own single consequence.
    private static IEffectNode<EnemyActionContext> Unreconciled() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ApplyStatusNode<EnemyActionContext>(
                Self, new StatusDefinitionId(DiscrepancyId),
                new MinExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(1),
                    new SubtractExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(DiscrepancyMaximum), Discrepancy()))),
            // Identity: a card of yours is redacted and cited — which can still pay you back, because the
            // citation is Auditor-issued.
            new ConditionalEffectNode<EnemyActionContext>(
                CounterIs<EnemyActionContext>(Self, AccountCounter, 1),
                SetOn<EnemyActionContext>(Across, IdentityPenaltyCounter, 1)),
            // Obligation: two Auditor-issued Overdue, and the act's own Delinquency rule takes it from there.
            new ConditionalEffectNode<EnemyActionContext>(
                CounterIs<EnemyActionContext>(Self, AccountCounter, 2),
                new ApplyStatusNode<EnemyActionContext>(
                    Across, new StatusDefinitionId(ActTwo.OverdueId),
                    new ConstantExpression<EnemyActionContext>(2))),
            // Life: the death clause goes up, and from here its death is a question rather than a fact.
            new ConditionalEffectNode<EnemyActionContext>(
                CounterIs<EnemyActionContext>(Self, AccountCounter, 3),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    SetOn<EnemyActionContext>(Self, LifeUnreconciledCounter, 1),
                    SyncTheClause<EnemyActionContext>(),
                ])),
        ]);

    // ── The Death Clause ──────────────────────────────────────────────────────────────────────────────────
    //
    // The engine's data-driven one-shot pre-down interceptor, which is the only place to stand: a downed
    // combatant refuses healing and status application, so a death cannot be undone afterwards. It consumes
    // its own status, which is exactly "the Death Clause can trigger only once".
    private static StatusData DeathClause() => new()
    {
        Id = DeathClauseId,
        NameKey = "Account Still Open",
        DescriptionKey = "Its life is unreconciled. Kill it now and it comes back to close the book.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(LifeHealth,
        [
            // "Temporary Block and temporary intent states clear. Accounts, Discrepancy and Documentation
            // remain." Then the Closing Audit.
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                ClosingAuditId, 0, StatusPolarity.Neutral),
        ]),
    };

    // Kept in step by the player's own record: while the two Documentation a final receipt costs are on the
    // table, the clause comes off and the Auditor's death is final. Below that it goes back on.
    private static IEffectNode<TContext> SyncTheClause<TContext>() where TContext : class
    {
        var auditor = typeof(TContext) == typeof(EnemyActionContext) ? Self : Across;
        var player = typeof(TContext) == typeof(EnemyActionContext) ? Across : Self;

        return new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(auditor, LifeUnreconciledCounter),
                ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            player, new StatusDefinitionId(DocumentationId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TContext>(DocumentationPerAccount)),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(auditor, ClauseSpentCounter),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                new RemoveStatusNode<TContext>(auditor, new StatusDefinitionId(DeathClauseId)),
                @else: new ApplyStatusNode<TContext>(
                    auditor, new StatusDefinitionId(DeathClauseId), new ConstantExpression<TContext>(1))));
    }

    // ── Citations ─────────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<EnemyActionContext> CiteLater(int count) =>
        Bump<EnemyActionContext>(Across, ReferenceDueCounter, count);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteWhatIsDue() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                Positive<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                // As many as were asked for, and "never exceed 2 simultaneously open Auditor References".
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                    Cite(2), @else: Cite(1)),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter, 0),
            ]));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Cite(int cards) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                new TagId(AuditorReferenceMark)),
            takeFirst: cards);

    // 8.2: a fulfilled Auditor Reference pays one line of Documentation, to a ceiling of six — and while the
    // Account is suspended the line is HELD rather than lost.
    private static IEffectNode<CardPlayedTriggeredEffectContext> OnCitationAnswered() =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Bump<CardPlayedTriggeredEffectContext>(Self, AnsweredThisTurnCounter, 1),
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                AcrossHas<CardPlayedTriggeredEffectContext>(SuspendedId),
                Bump<CardPlayedTriggeredEffectContext>(Self, DelayedDocumentationCounter, 1),
                @else: FileOneLine<CardPlayedTriggeredEffectContext>()),
            SyncTheClause<CardPlayedTriggeredEffectContext>(),
        ]);

    private static IEffectNode<TContext> FileOneLine<TContext>() where TContext : class =>
        new ApplyStatusNode<TContext>(
            Self, new StatusDefinitionId(DocumentationId),
            new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new SubtractExpression<TContext>(
                    new ConstantExpression<TContext>(DocumentationMaximum),
                    new CombatantStatusStacksExpression<TContext>(Self, new StatusDefinitionId(DocumentationId)))));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> ReleaseDelayedDocumentation() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                Positive<CardsDrawnTriggeredEffectContext>(Self, DelayedDocumentationCounter),
                new NotExpression<CardsDrawnTriggeredEffectContext>(
                    AcrossHas<CardsDrawnTriggeredEffectContext>(SuspendedId))),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                FileOneLine<CardsDrawnTriggeredEffectContext>(),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, DelayedDocumentationCounter, 0),
                SyncTheClause<CardsDrawnTriggeredEffectContext>(),
            ]));

    // 8.6 unreconciled Identity: "after the next normal draw, redact 1 valid card and reference that same
    // card. This happens once." The redaction and the citation land together, so the card that was damaged is
    // the card that can pay it back.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> ServeTheIdentityPenalty() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Positive<CardsDrawnTriggeredEffectContext>(Self, IdentityPenaltyCounter),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    Self, CardZone.Hand,
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            new TagId(ActTwo.RedactedMark)),
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            new TagId(AuditorReferenceMark)),
                    ]),
                    takeFirst: 1),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, IdentityPenaltyCounter, 0),
            ]));

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // Five slots over three phases — Examination, Formal Reconciliation and the Closing Audit it only reaches
    // by coming back. A pending Account takes the slot outright, because the resolution IS the action.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "open_the_account" => Phases(
            // Request Supporting Documentation.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(1), Block(8)]),
            // Request Final Supporting Documentation: up to two citations at once.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(2), Block(8)]),
            // Request the Final Receipt.
            CiteLater(1)),

        "suspend_the_account" => Phases(
            // Suspend the Account: the next line you earn is held until its turn begins. It is not lost.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Block(18),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(SuspendedId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ]),
            // Balance the Unreturned Account: 12 + 3 per Discrepancy, and it gets nastier as the record does.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(Across, Scaled(12, 3, 21)),
                new ConditionalEffectNode<EnemyActionContext>(
                    Discrepant(2), ApplyToPlayer(Keywords.Doubt, 1)),
                new ConditionalEffectNode<EnemyActionContext>(
                    Discrepant(3), ApplyToPlayer(ActTwo.OverdueId, 1)),
            ]),
            // Return the Outstanding Balance.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Strike(13), ApplyToPlayer(Keywords.Paperwork, 1), ApplyToPlayer(Keywords.Doubt, 1),
            ])),

        "account_of_obligation" => Phases(
            // Examine the Previous Statement: it cites, then hits.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(1), Strike(10)]),
            // Reconcile by Force: 15 + 5 per Discrepancy, at most 30.
            new DealDamageNode<EnemyActionContext>(Across, Scaled(15, 5, 30)),
            // Close the Book by Force: 28, and the record goes with it.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Strike(28),
                new RemoveStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(DocumentationId)),
            ])),

        "final_reconciliation" => Phases(
            // Preliminary Balance: 14 + 4 per Discrepancy, at most 26.
            new DealDamageNode<EnemyActionContext>(Across, Scaled(14, 4, 26)),
            // Final Reconciliation: 18, and a line back if you answered it last turn — the design's own
            // stabilising route, so the fight is not only about losing the resource.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Strike(18),
                new ConditionalEffectNode<EnemyActionContext>(
                    Positive<EnemyActionContext>(Across, AnsweredLastTurnCounter),
                    GiveOneLine()),
            ]),
            // No Life May Remain Unbalanced: 16 + 4 per Discrepancy, at most 28.
            new DealDamageNode<EnemyActionContext>(Across, Scaled(16, 4, 28))),

        "unreconciled" => Phases(
            // Reject Incomplete Documentation: a line struck and paperwork filed, or a hit where there is
            // nothing to strike.
            RejectDocumentation(1, 13),
            // …and in Formal Reconciliation it takes two.
            RejectDocumentation(2, 14),
            // No Life May Remain Unbalanced.
            new DealDamageNode<EnemyActionContext>(Across, Scaled(16, 4, 28))),

        _ => null,
    };

    // One slot, three phases — and above all of them the pending Account, which takes the action outright.
    private static EffectProgram<EnemyActionContext> Phases(
        IEffectNode<EnemyActionContext> examination,
        IEffectNode<EnemyActionContext> reconciliation,
        IEffectNode<EnemyActionContext> closing) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                SelfHas<EnemyActionContext>(AuditPendingId),
                // Answered — either way — is what says the response turn has been had. The Account is queued
                // at the Auditor's turn start, so resolving in that same turn would take the window away.
                new NotExpression<EnemyActionContext>(
                    CounterIs<EnemyActionContext>(Self, AnsweredCounter, 0))),
            ResolveTheAccount(),
            @else: new ConditionalEffectNode<EnemyActionContext>(
                SelfHas<EnemyActionContext>(ClosingAuditId), closing,
                @else: new ConditionalEffectNode<EnemyActionContext>(
                    SelfHas<EnemyActionContext>(FormalReconciliationId), reconciliation,
                    @else: examination))));

    private static IEffectNode<EnemyActionContext> RejectDocumentation(int lines, int insteadDamage) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                Documentation<EnemyActionContext>(), ComparisonOperator.Greater,
                new ConstantExpression<EnemyActionContext>(0)),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ModifyStatusStacksNode<EnemyActionContext>(
                    Across, new StatusDefinitionId(DocumentationId), new NegateExpression<EnemyActionContext>(Struck(lines))),
                new ApplyStatusNode<EnemyActionContext>(
                    Across, new StatusDefinitionId(Keywords.Paperwork), Struck(lines)),
                SyncTheClause<EnemyActionContext>(),
            ]),
            @else: Strike(insteadDamage));

    // "Remove up to N Documentation. For each removed, apply 1 Paperwork."
    private static ICombatExpression<EnemyActionContext, int> Struck(int lines) =>
        new MinExpression<EnemyActionContext>(
            new ConstantExpression<EnemyActionContext>(lines), Documentation<EnemyActionContext>());

    private static IEffectNode<EnemyActionContext> GiveOneLine() =>
        new ApplyStatusNode<EnemyActionContext>(
            Across, new StatusDefinitionId(DocumentationId),
            new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1),
                new SubtractExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(DocumentationMaximum),
                    Documentation<EnemyActionContext>())));

    // ── Small shared pieces ───────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<EnemyActionContext, int> Discrepancy() =>
        new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(DiscrepancyId));

    private static ICombatExpression<EnemyActionContext, bool> Discrepant(int at) =>
        new ComparisonExpression<EnemyActionContext>(
            Discrepancy(), ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(at));

    private static ICombatExpression<EnemyActionContext, int> Scaled(int flat, int per, int ceiling) =>
        new MinExpression<EnemyActionContext>(
            new AddExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(flat),
                new MultiplyExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(per), Discrepancy())),
            new ConstantExpression<EnemyActionContext>(ceiling));

    // Read from whichever side the program is running on: the Documentation is always the player's.
    private static ICombatExpression<TContext, int> Documentation<TContext>() where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(
            typeof(TContext) == typeof(EnemyActionContext) ? Across : Self,
            new StatusDefinitionId(DocumentationId));

    private static IEffectNode<EnemyActionContext> Strike(int amount) =>
        new DealDamageNode<EnemyActionContext>(Across, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> Block(int amount) =>
        new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> ApplyToPlayer(string status, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(status),
            new ConstantExpression<EnemyActionContext>(stacks));

    private static ICombatExpression<TContext, bool> AcrossHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Across, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> SelfHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> Below<TContext>(int health) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(Self),
            ComparisonOperator.LessOrEqual, new ConstantExpression<TContext>(health));

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

    private static IEffectNode<TContext> SetOn<TContext>(
        ICombatantTargetSelector on, CounterId counter, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(value), relative: false);

    private static IEffectNode<TContext> Bump<TContext>(
        ICombatantTargetSelector on, CounterId counter, int delta) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(delta), relative: true);

    private static IEffectNode<TContext> Copy<TContext>(CounterId into, CounterId from) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            Self, into, new CombatantCounterExpression<TContext>(Self, from), relative: false);

    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static StatusData Marker(string id, string name, string description) =>
        Rule(id, name, description, []);

    private static StatusData Stacking(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

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
