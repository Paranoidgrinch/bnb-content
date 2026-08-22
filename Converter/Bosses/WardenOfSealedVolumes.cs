using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Warden of Sealed Volumes (Act II boss, 270 HP) ────────────────────────────────────────────────────
//
// "The card is still yours. The right to use it is not."
//
// The Warden announces a Seal type, then takes one of your cards into custody: it leaves every normal zone
// for the Banished pile, stays visible, and cannot be drawn or played. It comes back only when you meet that
// Seal's Release Condition — and when it does it comes back free and stays in your hand for the turn.
//
//   Seal of Restraint — end a turn on no more than 2 cards played; returns next turn.
//   Seal of Procedure — play 2 different card types in one turn; returns at once.
//   Seal of Evidence  — answer the Warden's citation; returns at once.
//
// At 135 HP it throws Total Lockdown: a second slot opens, and from then on every key you turn works against
// the lock — a correct release costs the Warden 8 HP and 8 Block. At 68 HP it either fills the empty slot or,
// with both full, puts them Under Final Review, where releasing costs it 6 HP more.
//
// The whole fight is a negotiation about which volume you can bear to lose and how fast you can buy it back.
// Deviations: ADAPTATIONS.md.
public static class WardenOfSealedVolumes
{
    public const string EnemyId = "warden_of_sealed_volumes";

    // On the Warden.
    public const string TheWardenId = "the_warden_of_sealed_volumes";
    public const string WardenStateId = "the_warden_keys";
    public const string CustodyId = "warden_custody";
    public const string LockdownPendingId = "total_lockdown_called";
    public const string TotalLockdownId = "total_lockdown";
    public const string ProvisionalPermissionId = "provisional_permission";
    public const string FinalReviewId = "under_final_review";

    // Which slots are occupied — one status per Seal, so the table shows what is held and under what terms.
    public const string SealOfRestraintId = "seal_of_restraint";
    public const string SealOfProcedureId = "seal_of_procedure";
    public const string SealOfEvidenceId = "seal_of_evidence";

    // On the player.
    public const string WardenRulesId = "warden_rules";
    public const string WardenReferenceId = "warden_citation";
    public const string WardenReferenceMark = "referenced_by_the_warden";

    // On the cards themselves.
    public const string RestraintMark = "sealed_by_restraint";
    public const string ProcedureMark = "sealed_by_procedure";
    public const string EvidenceMark = "sealed_by_evidence";

    // The announcement and the sealing are two different acts, as the design has them: "Inspect the Claim"
    // prepares the TYPE and seals nothing, and "Seal the Principal Instrument" is what actually reaches. Both
    // counters live on the PLAYER, because that is whose hand is reached into — one spelling serves both ends.
    private static readonly CounterId SealTypeCounter = new("warden_seal_type");
    private static readonly CounterId SealDueCounter = new("warden_seal_due");
    private static readonly CounterId RestraintDueCounter = new("warden_restraint_due");

    // The turn's play profile, which is what two of the three Release Conditions are about.
    private static readonly CounterId PlayedDeedCounter = new("warden_played_deed");
    private static readonly CounterId PlayedWorkingCounter = new("warden_played_working");
    private static readonly CounterId PlayedOtherCounter = new("warden_played_other");

    // Which key the Warden reaches for next. Kept on the Warden and advanced with every announcement, so a
    // fight sees all three locks rather than the same one over and over.
    private static readonly CounterId SealRotationCounter = new("warden_seal_rotation");

    private static readonly CounterId LockdownSpentCounter = new("warden_lockdown_spent");
    private static readonly CounterId FinalSignatureSpentCounter = new("warden_final_signature_spent");

    public const int CustodyMaximum = 2;
    public const int LockdownHealth = 135;
    public const int FinalSignatureHealth = 68;
    public const int RestraintTempo = 2;
    public const int KeysAgainstTheLockHealth = 8;
    public const int KeysAgainstTheLockBlock = 8;
    public const int FinalReviewHealth = 6;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // The three Seals, each with the mark it puts on a card and the number the announcement uses for it.
    private static readonly (string Status, string Mark, int Announcement)[] Seals =
    [
        (SealOfRestraintId, RestraintMark, 1),
        (SealOfProcedureId, ProcedureMark, 2),
        (SealOfEvidenceId, EvidenceMark, 3),
    ];

    // ── Content ───────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheWardenId, "The Warden of Sealed Volumes",
            "Its keys open access, memory, procedure and permission."),
        Stacking(CustodyId, "Custody",
            "How many of your volumes the Warden is holding."),
        Marker(LockdownPendingId, "Total Lockdown",
            "Its next action opens the second slot."),
        Marker(TotalLockdownId, "Total Lockdown",
            "Two slots, and every key you turn works against the lock."),
        Marker(ProvisionalPermissionId, "Provisional Permission",
            "The next volume you buy back comes with a card drawn."),
        Marker(FinalReviewId, "Under Final Review",
            "Releasing a volume now costs the Warden 6 HP more."),

        Marker(SealOfRestraintId, "Seal of Restraint",
            "Held until you end a turn having played no more than 2 cards."),
        Marker(SealOfProcedureId, "Seal of Procedure",
            "Held until you play two different kinds of card in one turn."),
        Marker(SealOfEvidenceId, "Seal of Evidence",
            "Held until you answer the Warden's citation."),

        // The citation the Seal of Evidence hangs on. It is issued only when that Seal is set, and the sealed
        // card cannot be its target because a sealed card is not in the hand to be cited.
        ActTwo.Reference(WardenReferenceId, "Warden Citation", WardenReferenceMark,
            "The Warden has cited this card. Play it and the volume it is holding comes back.",
            cite: new NoOpEffectNode<CardsDrawnTriggeredEffectContext>(),
            onFulfilled: OnCitationAnswered()),

        Rules(),
        WardenState(),
    ];

    // ── The player's side of the lock ─────────────────────────────────────────────────────────────────────
    //
    // The sealing reaches into the player's hand and two of the three Release Conditions are about the
    // player's own turn, so the player carries these rules and the Warden is read across the table.
    private static StatusData Rules()
    {
        var onDraw = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    SealWhatWasAnnounced(),
                    // The citation rides on the same beat, and only while the Seal of Evidence is set.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        AcrossHas<CardsDrawnTriggeredEffectContext>(SealOfEvidenceId),
                        CiteOneInHand<CardsDrawnTriggeredEffectContext>()),
                ])));

        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    NoteTheKind(),
                    // "Play at least 2 different card types in one player turn" — checked the moment the
                    // second kind lands, because that Seal returns its card immediately.
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            KindsPlayed<CardPlayedTriggeredEffectContext>(),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(2)),
                        Release<CardPlayedTriggeredEffectContext>(SealOfProcedureId, ProcedureMark)),
                ])));

        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                IsTheApplicant<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // A volume bought back last turn was retained for that turn only; from now on it is an
                    // ordinary card again. Cleared BEFORE this turn's release, so the fresh one keeps its own.
                    ClearRetention<TurnStartedTriggeredEffectContext>(),
                    // "When satisfied: return the sealed card at the start of the next player turn."
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Positive<TurnStartedTriggeredEffectContext>(Self, RestraintDueCounter),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            SetOn<TurnStartedTriggeredEffectContext>(Self, RestraintDueCounter, 0),
                            Release<TurnStartedTriggeredEffectContext>(SealOfRestraintId, RestraintMark),
                        ])),
                    ClearKinds<TurnStartedTriggeredEffectContext>(),
                ])));

        var onTurnEnded = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                IsTheApplicant<TurnEndedTriggeredEffectContext>(),
                // "End a player turn with no more than 2 cards played." The release itself waits for the turn
                // to come round, which is what the Seal of Restraint costs over the other two.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        AcrossHas<TurnEndedTriggeredEffectContext>(SealOfRestraintId),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Self),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(RestraintTempo))),
                    SetOn<TurnEndedTriggeredEffectContext>(Self, RestraintDueCounter, 1))));

        return Rule(WardenRulesId, "Conditional Access",
            "The Warden holds your volumes. Each is released only on its own terms, and the terms are "
            + "printed where you can read them.",
            [
                Watch("CardsDrawn", onDraw),
                Watch("CardPlayed", onPlay),
                Watch("TurnStarted", onTurnStarted),
                Watch("TurnEnded", onTurnEnded),
            ]);
    }

    // ── Sealing ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // "After normal draw on the sealing turn: identify up to 2 eligible candidate cards; the player chooses
    // which one enters custody. If only one exists, use it. If none, no card is sealed and the Warden gains
    // 10 Block instead."
    private static IEffectNode<CardsDrawnTriggeredEffectContext> SealWhatWasAnnounced()
    {
        // ADAPTATION: "if no valid Citation can be generated, convert Seal of Evidence to Seal of Procedure."
        // A citation needs a card left in hand once the volume is taken, so a hand of fewer than two cannot
        // carry one and the Warden turns the Procedure key instead. The question is asked HERE, at the
        // sealing, because it is the only moment the hand exists: during the Warden's own turn — when the
        // announcement was made — the player's hand has already been put down.
        IEffectNode<CardsDrawnTriggeredEffectContext> ForAnnouncement(int announcement, string status, string mark)
        {
            IEffectNode<CardsDrawnTriggeredEffectContext> body = announcement != 3
                ? Offer(status, mark)
                : new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                    Offer(status, mark),
                    @else: Offer(SealOfProcedureId, ProcedureMark));

            return new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                CounterIs<CardsDrawnTriggeredEffectContext>(Self, SealTypeCounter, announcement), body);
        }

        return new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                Positive<CardsDrawnTriggeredEffectContext>(Self, SealDueCounter),
                // No slot free, nothing sealed. Phase I has one, Total Lockdown has two.
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    AcrossStacks<CardsDrawnTriggeredEffectContext>(CustodyId),
                    ComparisonOperator.Less, SlotCount<CardsDrawnTriggeredEffectContext>())),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                .. Seals.Select(s => ForAnnouncement(s.Announcement, s.Status, s.Mark)),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, SealDueCounter, 0),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, SealTypeCounter, 0),
            ]));
    }

    private static IEffectNode<CardsDrawnTriggeredEffectContext> Offer(string status, string mark)
    {
        IEffectNode<CardsDrawnTriggeredEffectContext> Take(int index) =>
            Seal(new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand, index), status, mark);

        var hand = new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand);

        return new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                hand, ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
            // Two candidates, and the choice of which volume to surrender is the player's.
            new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                [Take(0), Take(1)],
                ["surrender the first volume in hand", "surrender the second volume in hand"],
                count: 1, purpose: "the Warden takes one volume into custody"),
            @else: new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    hand, ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                // "If only one eligible candidate exists: use it."
                Take(0),
                // "If none exist: no card is sealed; Warden gains 10 Block instead."
                @else: BlockAcross<CardsDrawnTriggeredEffectContext>(10)));
    }

    // The volume leaves every normal zone for the Banished pile — the one place nothing else reaches into,
    // which is what "leaves combat zones but stays visible" has to mean for the return to be the Warden's
    // alone. Its mark is what says which key opens it again.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Seal(
        ICardInstanceExpression<CardsDrawnTriggeredEffectContext> card, string status, string mark) =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Self, card, new TagId(mark)),
            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, Held<CardsDrawnTriggeredEffectContext>(CardZone.Hand, mark), CardZone.BanishedPile),
            new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Across,
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(status),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(CustodyId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                ])),
        ]);

    // ── Release ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // "When satisfied: return the sealed card. For that turn: Retain; Cost 0." Both halves are per-instance
    // marks — the whole printed cost taken off this one copy, and this one copy kept in hand at turn end.
    private static IEffectNode<TContext> Release<TContext>(string status, string mark) where TContext : class
    {
        var sealed_ = Held<TContext>(CardZone.BanishedPile, mark);
        var returned = Held<TContext>(CardZone.Hand, mark);

        return new ConditionalEffectNode<TContext>(
            AcrossHas<TContext>(status),
            new CausalSequenceEffectNode<TContext>(
            [
                new MoveCardToZoneNode<TContext>(Self, sealed_, CardZone.Hand),
                new SetCardInstanceMarkCounterNode<TContext>(
                    Self, returned, StandardCombatIds.CardCostDeltaCounter,
                    new NegateExpression<TContext>(
                        new CardInstanceBaseCostExpression<TContext>(returned, StandardCombatIds.EnergyResource)),
                    relative: false),
                new MarkCardInstanceNode<TContext>(Self, returned, StandardCombatIds.RetainedCardMark),
                new MarkCardInstanceNode<TContext>(Self, returned, new TagId(mark), remove: true),
                Across_Remove<TContext>(status),
                new ForEachTargetEffectNode<TContext>(Across,
                    new ModifyStatusStacksNode<TContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(CustodyId),
                        new ConstantExpression<TContext>(-1))),
                KeysTurnAgainstTheLock<TContext>(),
                // "Satisfying its condition grants normal release plus Draw 1."
                new ConditionalEffectNode<TContext>(
                    AcrossHas<TContext>(ProvisionalPermissionId),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new DrawCardsNode<TContext>(Self, new ConstantExpression<TContext>(1)),
                        Across_Remove<TContext>(ProvisionalPermissionId),
                    ])),
            ]));
    }

    // Phase II's passive: "whenever a sealed card is correctly released, the Warden loses 8 HP and up to 8
    // current Block." The HP loss is not a Damage event, so it is written as a health set — no Block, damage
    // modifier or damage-taken reaction can see it. Under Final Review it costs 6 more.
    private static IEffectNode<TContext> KeysTurnAgainstTheLock<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            AcrossHas<TContext>(TotalLockdownId),
            new CausalSequenceEffectNode<TContext>(
            [
                LoseHealthAcross<TContext>(KeysAgainstTheLockHealth),
                new ForEachTargetEffectNode<TContext>(Across,
                    new ModifyDefensivePoolNode<TContext>(
                        CombatantTargetSelectors.IterationTarget, StandardCombatIds.BlockDefensivePool,
                        new NegateExpression<TContext>(
                            new MinExpression<TContext>(
                                new ConstantExpression<TContext>(KeysAgainstTheLockBlock),
                                AcrossBlock<TContext>())))),
                new ConditionalEffectNode<TContext>(
                    AcrossHas<TContext>(FinalReviewId),
                    LoseHealthAcross<TContext>(FinalReviewHealth)),
            ]));

    // Answering a Warden Citation is the Seal of Evidence's key.
    private static IEffectNode<CardPlayedTriggeredEffectContext> OnCitationAnswered() =>
        Release<CardPlayedTriggeredEffectContext>(SealOfEvidenceId, EvidenceMark);

    // ── The Warden's own state ────────────────────────────────────────────────────────────────────────────

    private static StatusData WardenState()
    {
        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // "Trigger: Warden reaches 135 HP or less."
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        IsZero<TurnStartedTriggeredEffectContext>(Self, LockdownSpentCounter),
                        Below<TurnStartedTriggeredEffectContext>(LockdownHealth)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(LockdownPendingId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, LockdownSpentCounter, 1),
                    ])),
                // Final Signature — "Every Volume Is Restricted", at 68 HP, once per combat.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        IsZero<TurnStartedTriggeredEffectContext>(Self, FinalSignatureSpentCounter),
                        Below<TurnStartedTriggeredEffectContext>(FinalSignatureHealth)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                    Self, new StatusDefinitionId(CustodyId)),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(CustodyMaximum)),
                            // "If both are occupied: each occupied Seal becomes Under Final Review."
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                Self, new StatusDefinitionId(FinalReviewId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            // "If fewer than two Sealed Slots are occupied: immediately prepare a sealing."
                            @else: new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                            [
                                Announce<TurnStartedTriggeredEffectContext>(2),
                                SetOn<TurnStartedTriggeredEffectContext>(Across, SealDueCounter, 1),
                            ])),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, FinalSignatureSpentCounter, 1),
                    ])),
            ]));

        // 6.8 Death cleanup: every held volume comes back — to the discard, not the hand, because the fight
        // that would have used it is over.
        var onDowned = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                .. Seals.Select(s => ReturnToDiscard(s.Mark)),
            ]));

        return Rule(WardenStateId, "The Keys",
            "Half its body gone and the second slot opens; near the end every key you turn works against "
            + "the lock.",
            [
                Watch("TurnStarted", onTurnStarted),
                Watch("Downed", onDowned),
            ]);
    }

    // In a Downed program the acting Source is the FALLEN combatant — the Warden itself — so the volumes are
    // fetched from across the table, which is where their owner is standing.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> ReturnToDiscard(string mark) =>
        new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(Across,
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                new MoveCardToZoneNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget,
                    new FirstMarkedCardInOwnerZoneExpression<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, CardZone.BanishedPile, new TagId(mark)),
                    CardZone.DiscardPile),
            ]));

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // Five slots, each reading as its Phase-I move, its Phase-II move, or the one Total Lockdown action. The
    // design's cooldowns of 2 and 3 intents are satisfied by the cycle: five slots bring any one round again
    // only every fifth action.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "inspect_the_claim" => Phases(
            // Inspect the Claim: announce the next Seal, take 10 Block, seal nothing yet.
            new CausalSequenceEffectNode<EnemyActionContext>([AnnounceNext(), Block(10)]),
            // Inspect the Remaining Access — or, with both slots full, Hold in Custody instead.
            new ConditionalEffectNode<EnemyActionContext>(
                SlotsFull<EnemyActionContext>(),
                HoldInCustody(),
                @else: new CausalSequenceEffectNode<EnemyActionContext>([AnnounceNext(), Block(10)]))),

        "seal_the_principal_instrument" => Phases(
            // Seal the Principal Instrument: the announced procedure is called for now and resolves after the
            // player's next normal draw, which is when there is a hand to reach into.
            new CausalSequenceEffectNode<EnemyActionContext>([CallForTheSealing(), Block(8)]),
            // Seal the Remaining Access — ineligible with no free slot, and there the Warden reviews one of
            // the seals it already holds instead of reaching for a third that does not exist.
            new ConditionalEffectNode<EnemyActionContext>(
                SlotsFull<EnemyActionContext>(),
                // Review Provisional Permission: the next correct release also draws a card.
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        Self, new StatusDefinitionId(ProvisionalPermissionId),
                        new ConstantExpression<EnemyActionContext>(1)),
                    Block(8),
                ]),
                @else: new CausalSequenceEffectNode<EnemyActionContext>([CallForTheSealing(), Block(8)]))),

        "deny_immediate_access" => Phases(
            // Deny Immediate Access: 15, and 1 Doubt while it is holding anything.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(15),
                new ConditionalEffectNode<EnemyActionContext>(
                    Holding<EnemyActionContext>(1), ApplyToPlayer(Keywords.Doubt, 1)),
            ]),
            // Strike With the Master Key: 15 + 7 per Custody, 29 at both slots.
            new DealDamageNode<EnemyActionContext>(Across, ScaledByCustody(15, 7, 29))),

        "hold_in_custody" => Phases(
            // Hold in Custody: 12 + 7 per Custody Block.
            HoldInCustody(),
            // Inventory of Confiscated Means: 1 Paperwork per Custody, then a matching temporary Strength.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(Keywords.Paperwork), Custody()),
                new ApplyStatusNode<EnemyActionContext>(
                    Self, new StatusDefinitionId("strength"), Custody(), durationTurns: 1),
            ])),

        "final_denial" => Phases(
            // Denied by Authority: 14 + 6 per Custody, capped at 20 while only one slot exists.
            new DealDamageNode<EnemyActionContext>(Across, ScaledByCustody(14, 6, 20)),
            // Final Denial: 20, and at both slots the paperwork that comes with it.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(20),
                new ConditionalEffectNode<EnemyActionContext>(
                    Holding<EnemyActionContext>(CustodyMaximum),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        ApplyToPlayer(Keywords.Doubt, 1),
                        ApplyToPlayer(Keywords.Paperwork, 1),
                    ])),
            ])),

        _ => null,
    };

    // One slot, two phases, and the single action that turns one into the other: "unlock the second Sealed
    // Slot; preserve the currently sealed card; gain 16 Block; no attack."
    private static EffectProgram<EnemyActionContext> Phases(
        IEffectNode<EnemyActionContext> controlled, IEffectNode<EnemyActionContext> lockdown) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            SelfHas<EnemyActionContext>(LockdownPendingId),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Block(16),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(LockdownPendingId)),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(TotalLockdownId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ]),
            @else: new ConditionalEffectNode<EnemyActionContext>(
                SelfHas<EnemyActionContext>(TotalLockdownId), lockdown, @else: controlled)));

    // "Prepare the next Seal type." The rotation advances with every announcement so a fight meets all three
    // locks, and a key whose slot is already occupied is passed over — under Total Lockdown the Warden holds
    // two volumes at once and must not reach for the same one twice.
    private static IEffectNode<EnemyActionContext> AnnounceNext() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            // Chosen from where the rotation stands NOW, and only then advanced — so the first lock a fight
            // meets is the Seal of Restraint, the way the design lists them.
            Turn(0, [1, 2, 3]),
            Turn(1, [2, 3, 1]),
            Turn(2, [3, 1, 2]),
            Bump<EnemyActionContext>(Self, SealRotationCounter, 1),
        ]);

    // The rotation's current position, and the order it prefers its keys in from there.
    private static IEffectNode<EnemyActionContext> Turn(int position, int[] preference) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new RemainderExpression<EnemyActionContext>(
                    new CombatantCounterExpression<EnemyActionContext>(Self, SealRotationCounter),
                    new ConstantExpression<EnemyActionContext>(3)),
                ComparisonOperator.Equal, new ConstantExpression<EnemyActionContext>(position)),
            FirstFreeKey(preference));

    private static IEffectNode<EnemyActionContext> FirstFreeKey(int[] preference)
    {
        IEffectNode<EnemyActionContext> From(int index)
        {
            var key = Seals.First(s => s.Announcement == preference[index]);
            return index == preference.Length - 1
                ? Announce<EnemyActionContext>(key.Announcement)
                : new ConditionalEffectNode<EnemyActionContext>(
                    new NotExpression<EnemyActionContext>(AcrossHas<EnemyActionContext>(key.Status)),
                    Announce<EnemyActionContext>(key.Announcement),
                    @else: From(index + 1));
        }

        return From(0);
    }

    private static IEffectNode<TContext> Announce<TContext>(int announcement) where TContext : class =>
        SetOn<TContext>(Across, SealTypeCounter, announcement);

    // Nothing can be sealed under a key that was never named, so a sealing called for without a standing
    // announcement names one on the way past.
    private static IEffectNode<EnemyActionContext> CallForTheSealing() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                IsZero<EnemyActionContext>(Across, SealTypeCounter), AnnounceNext()),
            SetOn<EnemyActionContext>(Across, SealDueCounter, 1),
        ]);

    private static IEffectNode<EnemyActionContext> HoldInCustody() =>
        new GainBlockNode<EnemyActionContext>(Self,
            new AddExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(12),
                new MultiplyExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(7), Custody())));

    private static ICombatExpression<EnemyActionContext, int> ScaledByCustody(int flat, int per, int ceiling) =>
        new MinExpression<EnemyActionContext>(
            new AddExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(flat),
                new MultiplyExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(per), Custody())),
            new ConstantExpression<EnemyActionContext>(ceiling));

    private static ICombatExpression<EnemyActionContext, int> Custody() =>
        new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(CustodyId));

    private static ICombatExpression<TContext, bool> Holding<TContext>(int count) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Self, new StatusDefinitionId(CustodyId)),
            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(count));

    private static ICombatExpression<TContext, bool> SlotsFull<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Self, new StatusDefinitionId(CustodyId)),
            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(CustodyMaximum));

    // Phase I holds one volume; Total Lockdown holds two. No third slot exists.
    private static ICombatExpression<TContext, int> SlotCount<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                AcrossStacks<TContext>(TotalLockdownId)));

    // ── The turn's play profile ───────────────────────────────────────────────────────────────────────────

    private static IEffectNode<CardPlayedTriggeredEffectContext> NoteTheKind()
    {
        IEffectNode<CardPlayedTriggeredEffectContext> Note(string tag, CounterId counter) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(tag)),
                SetOn<CardPlayedTriggeredEffectContext>(Self, counter, 1));

        return new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            // Anything that is neither a Deed nor a Working counts as the third kind, so a card with no
            // taxonomy tag still tells the Procedure key something.
            SetOn<CardPlayedTriggeredEffectContext>(Self, PlayedOtherCounter, 1),
            Note(CardAuthoring.DeedTag, PlayedDeedCounter),
            Note(CardAuthoring.WorkingTag, PlayedWorkingCounter),
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    Positive<CardPlayedTriggeredEffectContext>(Self, PlayedDeedCounter),
                    Positive<CardPlayedTriggeredEffectContext>(Self, PlayedWorkingCounter)),
                SetOn<CardPlayedTriggeredEffectContext>(Self, PlayedOtherCounter, 0)),
        ]);
    }

    private static ICombatExpression<TContext, int> KindsPlayed<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new CombatantCounterExpression<TContext>(Self, PlayedDeedCounter),
            new AddExpression<TContext>(
                new CombatantCounterExpression<TContext>(Self, PlayedWorkingCounter),
                new CombatantCounterExpression<TContext>(Self, PlayedOtherCounter)));

    private static IEffectNode<TContext> ClearKinds<TContext>() where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            SetOn<TContext>(Self, PlayedDeedCounter, 0),
            SetOn<TContext>(Self, PlayedWorkingCounter, 0),
            SetOn<TContext>(Self, PlayedOtherCounter, 0),
        ]);

    // A volume bought back is retained for ONE turn. Clearing the mark puts every copy back under the
    // ordinary end-of-turn rule.
    private static IEffectNode<TContext> ClearRetention<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<TContext>(
                Self, new IteratedCardExpression<TContext>(), StandardCombatIds.RetainedCardMark, remove: true),
            markFilter: StandardCombatIds.RetainedCardMark);

    // ── Small shared pieces ───────────────────────────────────────────────────────────────────────────────

    private static ICardInstanceExpression<TContext> Held<TContext>(CardZone zone, string mark)
        where TContext : class =>
        new FirstMarkedCardInOwnerZoneExpression<TContext>(Self, zone, new TagId(mark));

    private static IEffectNode<TContext> CiteOneInHand<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<TContext>(
                Self, new IteratedCardExpression<TContext>(), new TagId(WardenReferenceMark)),
            takeFirst: 1);

    private static IEffectNode<TContext> LoseHealthAcross<TContext>(int amount) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new SetHealthNode<TContext>(
                CombatantTargetSelectors.IterationTarget,
                new SubtractExpression<TContext>(
                    new CombatantCurrentHealthExpression<TContext>(CombatantTargetSelectors.IterationTarget),
                    new ConstantExpression<TContext>(amount))));

    private static IEffectNode<TContext> BlockAcross<TContext>(int amount) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new GainBlockNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new ConstantExpression<TContext>(amount)));

    private static IEffectNode<EnemyActionContext> Block(int amount) =>
        new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Across, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> ApplyToPlayer(string status, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(status),
            new ConstantExpression<EnemyActionContext>(stacks));

    private static ICombatExpression<TContext, int> AcrossBlock<TContext>() where TContext : class =>
        new CombatantDefensivePoolExpression<TContext>(Across, StandardCombatIds.BlockDefensivePool);

    private static ICombatExpression<TContext, int> AcrossStacks<TContext>(string statusId) where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(Across, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> AcrossHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Across, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> SelfHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(statusId));

    private static IEffectNode<TContext> Across_Remove<TContext>(string statusId) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new RemoveStatusNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(statusId)));

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

    private static ICombatExpression<TContext, bool> IsZero<TContext>(
        ICombatantTargetSelector on, CounterId counter) where TContext : class =>
        CounterIs<TContext>(on, counter, 0);

    private static IEffectNode<TContext> Bump<TContext>(
        ICombatantTargetSelector on, CounterId counter, int delta) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(delta), relative: true);

    private static IEffectNode<TContext> SetOn<TContext>(
        ICombatantTargetSelector on, CounterId counter, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(value), relative: false);

    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    // Bearer scope throughout, for the same reason as every other Act-II boss: these programs read `Self`,
    // and under Anywhere each would fire on both turns and read the wrong body.
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
