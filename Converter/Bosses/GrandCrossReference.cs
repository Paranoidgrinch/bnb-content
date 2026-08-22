using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Grand Cross-Reference (Act II boss, 68 + 72 + 76 → 96 HP) ─────────────────────────────────────────
//
// "Nothing in the Archive means anything alone."
//
// Three monumental volumes hang around a concordance engine that cannot be touched. Exactly TWO of the
// volumes are linked at a time and only the linked pair acts; the third is isolated and silent. The engine
// itself spends every turn on one structural action that deals no damage — rotating the link, bracing the
// pair, reversing which way a passive reads, or feeding the volume nobody is talking to.
//
//   The Premise    (68) — conditions and citations. Its citations also redact.
//   The Authority  (72) — binding interpretation. A big hit on one linked volume is partly redirected to
//                         the other, so burst is redistributed rather than erased.
//   The Conclusion (76) — stored consequence. It banks a fifth of everything the linked pair takes and
//                         eventually draws the necessary result out of it.
//
// Kill all three and the engine becomes real: 96 HP, and the rule it fights under is decided by WHICH VOLUME
// YOU KILLED LAST. That is the whole boss — the player writes the final law by choosing a kill order.
//
//   Premise last    → Nothing Follows Without a Premise: answer its citations or Assumption becomes fact.
//   Authority last  → Only the Authorized Reading Counts: a Binding Guard you break with redacted cards.
//   Conclusion last → The Result Was Always Fixed: a Final Result that returns, harder, for ever.
//
// Deviations: ADAPTATIONS.md.
public static class GrandCrossReference
{
    public const string PremiseId = "gcr_premise";
    public const string AuthorityId = "gcr_authority";
    public const string ConclusionId = "gcr_conclusion";
    public const string EngineId = "grand_cross_reference";

    // Markers, so every rule can find the body it is about.
    public const string ThePremiseId = "the_premise";
    public const string TheAuthorityId = "the_authority";
    public const string TheConclusionId = "the_conclusion";
    public const string TheConcordanceId = "the_concordance";

    public const string EngineRulesId = "the_concordance_engine";
    public const string UntouchableId = "no_further_reference_yet";
    public const string IsolatedStrengthId = "isolated_in_the_margin";
    public const string ReversedId = "the_citation_is_reversed";
    public const string PreservedId = "the_result_is_preserved";

    // Phase-I passives, each on its own volume.
    public const string FoundationalCitationId = "foundational_citation";
    public const string BindingAuthorityId = "binding_authority";
    public const string StoredLedgerId = "stored_conclusion_ledger";

    // Phase II — the three Final Theses, one of which the player chooses by kill order.
    public const string ThesisPremiseId = "nothing_follows_without_a_premise";
    public const string ThesisAuthorityId = "only_the_authorized_reading_counts";
    public const string ThesisConclusionId = "the_result_was_always_fixed";
    public const string AssumptionId = "assumption";
    public const string BindingGuardId = "binding_guard";
    public const string FinalResultId = "final_result";
    public const string ConvergedId = "all_references_converge";

    // On the player.
    public const string CrossReferenceRulesId = "grand_cross_reference_rules";
    public const string CitationId = "grand_cross_reference_citation";
    public const string CitationMark = "referenced_by_the_concordance";

    // On the engine: the link rotation, the roll of who is still standing, and who fell last.
    private static readonly CounterId RotationCounter = new("gcr_rotation");
    private static readonly CounterId PremiseStandingCounter = new("gcr_premise_standing");
    private static readonly CounterId AuthorityStandingCounter = new("gcr_authority_standing");
    private static readonly CounterId ConclusionStandingCounter = new("gcr_conclusion_standing");
    private static readonly CounterId LastBrokenCounter = new("gcr_last_broken");   // 1 Premise 2 Authority 3 Conclusion
    private static readonly CounterId TransitionSpentCounter = new("gcr_transition_spent");
    private static readonly CounterId SignatureSpentCounter = new("gcr_signature_spent");
    private static readonly CounterId FinalResultDamageCounter = new("gcr_final_result_damage");

    // On the Conclusion: what the linked pair has paid it.
    private static readonly CounterId StoredCounter = new("gcr_stored_conclusion");

    // On the player: this fight's own bookkeeping.
    private static readonly CounterId CitationDueCounter = new("gcr_citation_due");
    private static readonly CounterId FoundationUsedCounter = new("gcr_foundation_used");
    private static readonly CounterId BindingUsedCounter = new("gcr_binding_used");
    private static readonly CounterId RedactedPlayedCounter = new("gcr_redacted_played");

    public const int StoredMaximum = 22;
    public const int StoredShare = 5;              // a fifth of what the linked pair takes
    public const int BindingThreshold = 14;
    public const int BindingRelief = 5;
    public const int AssumptionMaximum = 3;
    public const int BindingGuardAmount = 14;
    public const int FinalResultStart = 20;
    public const int FinalResultStep = 3;
    public const int FinalResultCeiling = 29;
    public const int ConvergenceHealth = 24;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static readonly (string Marker, int Index, CounterId Standing)[] Volumes =
    [
        (ThePremiseId, 1, PremiseStandingCounter),
        (TheAuthorityId, 2, AuthorityStandingCounter),
        (TheConclusionId, 3, ConclusionStandingCounter),
    ];

    // ── Content ───────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(ThePremiseId, "The Premise", "Conditions, and the citations that carry them."),
        Marker(TheAuthorityId, "The Authority", "The reading that binds, and redistributes."),
        Marker(TheConclusionId, "The Conclusion", "Everything the pair has paid, kept for later."),
        Marker(TheConcordanceId, "The Concordance", "It is not part of the argument yet."),
        Marker(IsolatedStrengthId, "Isolated in the Margin", "Nobody is citing it, so it is getting stronger."),
        Marker(ReversedId, "The Citation Is Reversed", "The linked passive reads the other way round."),
        Marker(PreservedId, "The Result Is Preserved", "What it has stored cannot be spent yet."),
        Marker(ConvergedId, "All References Converge", "Everything it has ever cited arrives at once."),

        // "The central body cannot initially be attacked." Written as complete immunity while the volumes
        // stand: the engine has no untargetability, and a body that cannot be hurt is what that means at the
        // table. It must be on the board from the first bell all the same — the combat ends the moment no
        // enemy is living, so a concordance that only appeared afterwards would never appear at all.
        Untouchable(),

        Stacking(AssumptionId, "Assumption", "What it has concluded without you. At 3 it becomes fact."),
        Stacking(BindingGuardId, "Binding Guard", "Only the authorized reading counts. Redact one and it goes."),
        Filed(FinalResultId, "Final Result", "The result was always fixed. This is how long you have."),

        Marker(ThesisPremiseId, "Nothing Follows Without a Premise",
            "Answer its citations, or what it assumed becomes fact."),
        Marker(ThesisAuthorityId, "Only the Authorized Reading Counts",
            "It guards itself with a reading you can only break with a redacted card."),
        Marker(ThesisConclusionId, "The Result Was Always Fixed",
            "The result is already scheduled, and it comes back harder."),

        FoundationalCitation(),
        BindingAuthority(),
        StoredLedger(),

        ActTwo.Reference(CitationId, "Concordance Citation", CitationMark,
            "The Cross-Reference has cited this card. Play it and the citation is answered.",
            cite: CiteWhatIsDue(),
            onFulfilled: OnCitationAnswered()),

        Rules(),
        ConcordanceEngine(),
    ];

    // ── The Premise's passive ─────────────────────────────────────────────────────────────────────────────
    //
    // "The first time each round either linked volume applies Referenced, that card also becomes Redacted."
    // The citing itself is done by the shared citation rule, so this hangs on the same beat and marks the
    // card it has just cited.
    private static StatusData FoundationalCitation() =>
        Marker(FoundationalCitationId, "Foundational Citation",
            "The first citation each round redacts the card it lands on.");

    // ── The Authority's passive ───────────────────────────────────────────────────────────────────────────
    //
    // "The first time each player turn a single card would deal at least 14 direct damage to either linked
    // volume: reduce that target's damage by 5 and deal 5 secondary damage to the linked partner."
    //
    // ADAPTATION: written as a reaction rather than an interception. A passive modifier is read from combatant
    // state and cannot see the size of the packet, and no once-per-turn gate exists on one. So the volume is
    // healed the 5 it should not have taken and the partner is dealt 5 — which leaves both bodies exactly
    // where the design puts them, and keeps the design's own point that the damage is REDISTRIBUTED rather
    // than erased. The secondary damage is dealt by the Authority, so it is not a card hit and cannot feed
    // this rule again.
    private static StatusData BindingAuthority()
    {
        var struck = CombatantTargetSelectors.EventTarget;

        var onDamage = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(BindingThreshold)),
                    IsZero<DamageReceivedTriggeredEffectContext>(Self, BindingUsedCounter)),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    Bump<DamageReceivedTriggeredEffectContext>(Self, BindingUsedCounter, 1),
                    new HealNode<DamageReceivedTriggeredEffectContext>(
                        struck, new ConstantExpression<DamageReceivedTriggeredEffectContext>(BindingRelief)),
                    // The partner pays what the struck volume did not.
                    HitThePartner(),
                ])));

        return Rule(BindingAuthorityId, "Binding Authority",
            "A heavy blow on one linked volume is read down by 5, and the 5 is read across to its partner.",
            [Watch("DamageTaken", onDamage, StatusTriggerScope.Anywhere)]);
    }

    // ADAPTATION: the partner is named rather than derived. Nothing selects "the other member of the current
    // pair" — a selector reaches allies or enemies, not one named half of a link — so the reading goes to the
    // Authority whenever something else was struck (it is half of the pair in two rotations out of three) and
    // the other way, to whichever volume still stands, when the Authority itself was struck.
    private static IEffectNode<DamageReceivedTriggeredEffectContext> HitThePartner()
    {
        IEffectNode<DamageReceivedTriggeredEffectContext> Read(string marker) =>
            new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(marker)),
                new DealDamageNode<DamageReceivedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(BindingRelief),
                    ignoresBlock: true));

        var struckTheAuthority = new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(TheAuthorityId));

        return new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            struckTheAuthority,
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                Alive<DamageReceivedTriggeredEffectContext>(TheConclusionId),
                Read(TheConclusionId),
                @else: Read(ThePremiseId)),
            @else: Read(TheAuthorityId));
    }

    // ── The Conclusion's ledger ───────────────────────────────────────────────────────────────────────────
    //
    // "Whenever either linked volume takes direct card damage, store 20% of the actual damage taken, rounded
    // down, to a maximum of 22."
    private static StatusData StoredLedger()
    {
        var onDamage = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                Self, StoredCounter,
                new MinExpression<DamageReceivedTriggeredEffectContext>(
                    new AddExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(Self, StoredCounter),
                        new DivideExpression<DamageReceivedTriggeredEffectContext>(
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(StoredShare))),
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(StoredMaximum)),
                relative: false));

        return Rule(StoredLedgerId, "Stored Conclusion",
            "A fifth of everything the linked pair takes is kept, and eventually drawn out of the ledger.",
            [Watch("DamageTaken", onDamage, StatusTriggerScope.Anywhere)]);
    }

    private static StatusData Untouchable() => new()
    {
        Id = UntouchableId,
        NameKey = "No Further Reference Yet",
        DescriptionKey = "The concordance is not part of the argument. Nothing you do reaches it.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(
                PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, Magnitude: 0),
        ],
        Triggers = [],
    };

    // ── The player's side ─────────────────────────────────────────────────────────────────────────────────

    private static StatusData Rules()
    {
        var onDraw = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // Only the Authorized Reading Counts: a guard put up every turn, and the redaction that
                    // hands the player the very tool for breaking it.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        AcrossHas<CardsDrawnTriggeredEffectContext>(ThesisAuthorityId),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            GuardUp<CardsDrawnTriggeredEffectContext>(BindingGuardAmount),
                            RedactTheTopCard<CardsDrawnTriggeredEffectContext>(),
                        ])),
                    // Nothing Follows Without a Premise cites every turn, whatever else is due.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        AcrossHas<CardsDrawnTriggeredEffectContext>(ThesisPremiseId),
                        Bump<CardsDrawnTriggeredEffectContext>(Self, CitationDueCounter, 1)),
                ])));

        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                // "The first time each turn the player plays a Redacted card: remove all Binding Guard, and
                // the Grand Cross-Reference loses 5 HP." The authorized reading is broken with the very thing
                // it did to your hand.
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                            new TagId(ActTwo.RedactedMark)),
                        IsZero<CardPlayedTriggeredEffectContext>(Self, RedactedPlayedCounter)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        Bump<CardPlayedTriggeredEffectContext>(Self, RedactedPlayedCounter, 1),
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            AcrossHas<CardPlayedTriggeredEffectContext>(BindingGuardId),
                            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                            [
                                Across_Remove<CardPlayedTriggeredEffectContext>(BindingGuardId),
                                LoseHealthAcross<CardPlayedTriggeredEffectContext>(5),
                            ])),
                    ]))));

        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                IsTheApplicant<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    SetOn<TurnStartedTriggeredEffectContext>(Self, BindingUsedCounter, 0),
                    SetOn<TurnStartedTriggeredEffectContext>(Self, FoundationUsedCounter, 0),
                    SetOn<TurnStartedTriggeredEffectContext>(Self, RedactedPlayedCounter, 0),
                ])));

        return Rule(CrossReferenceRulesId, "The Concordance",
            "Nothing here means anything alone: what one volume does is read against what another one did.",
            [
                Watch("CardsDrawn", onDraw),
                Watch("CardPlayed", onPlay),
                Watch("TurnStarted", onTurnStarted),
            ]);
    }

    // ── The concordance engine ────────────────────────────────────────────────────────────────────────────
    //
    // It keeps the roll of who is standing, and that roll is what decides everything: which pair is linked,
    // when the volumes are gone, and — the whole boss — WHICH ONE FELL LAST.
    private static StatusData ConcordanceEngine()
    {
        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // A volume that was standing and is not among the living any more fell since the last look,
                // and the last one to do that writes the final law.
                .. Volumes.Select(v => NoteIfBroken(v.Marker, v.Index, v.Standing)),
                // "When all three volumes are Broken: the central body becomes targetable and the last
                // destroyed volume determines the Final Thesis."
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        IsZero<TurnStartedTriggeredEffectContext>(Self, TransitionSpentCounter),
                        NoVolumesLeft()),
                    NoFurtherReference()),
                // Final Signature — All References Converge, at 24 HP, once per combat.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new AndExpression<TurnStartedTriggeredEffectContext>(
                            IsZero<TurnStartedTriggeredEffectContext>(Self, SignatureSpentCounter),
                            Positive<TurnStartedTriggeredEffectContext>(Self, TransitionSpentCounter)),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(Self),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(ConvergenceHealth))),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(ConvergedId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, SignatureSpentCounter, 1),
                    ])),
            ]));

        // The Result Was Always Fixed: the scheduled hour comes due, hits, and is filed again harder.
        var onExpired = new EffectProgram<StatusExpiredTriggeredEffectContext>(
            new ConditionalEffectNode<StatusExpiredTriggeredEffectContext>(
                new TriggerEventStatusIsExpression<StatusExpiredTriggeredEffectContext>(
                    new StatusDefinitionId(FinalResultId)),
                new CausalSequenceEffectNode<StatusExpiredTriggeredEffectContext>(
                [
                    new DealDamageNode<StatusExpiredTriggeredEffectContext>(
                        Across,
                        new CombatantCounterExpression<StatusExpiredTriggeredEffectContext>(
                            Self, FinalResultDamageCounter)),
                    // "Increase damage by 3, maximum 29" — 20 → 23 → 26 → 29 → 29…
                    new SetCombatantCounterNode<StatusExpiredTriggeredEffectContext>(
                        Self, FinalResultDamageCounter,
                        new MinExpression<StatusExpiredTriggeredEffectContext>(
                            new AddExpression<StatusExpiredTriggeredEffectContext>(
                                new CombatantCounterExpression<StatusExpiredTriggeredEffectContext>(
                                    Self, FinalResultDamageCounter),
                                new ConstantExpression<StatusExpiredTriggeredEffectContext>(FinalResultStep)),
                            new ConstantExpression<StatusExpiredTriggeredEffectContext>(FinalResultCeiling)),
                        relative: false),
                    // "Final Result cannot be permanently removed": it files itself again at once.
                    new ApplyStatusNode<StatusExpiredTriggeredEffectContext>(
                        Self, new StatusDefinitionId(FinalResultId),
                        new ConstantExpression<StatusExpiredTriggeredEffectContext>(1), durationTurns: 2),
                ])));

        return Rule(EngineRulesId, "The Concordance Engine",
            "It keeps the roll of what is still standing, and the last thing to fall decides what it becomes.",
            [
                Watch("TurnStarted", onTurnStarted),
                Watch("StatusExpired", onExpired),
            ]);
    }

    // A volume is "standing" until the moment it stops being among the living — a downed body is not selected
    // by a living-only selector, and a downed body's own statuses read as absent, so this is the one question
    // that can be asked about it from outside.
    private static IEffectNode<TurnStartedTriggeredEffectContext> NoteIfBroken(
        string marker, int index, CounterId standing) =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Alive<TurnStartedTriggeredEffectContext>(marker),
            SetOn<TurnStartedTriggeredEffectContext>(Self, standing, 1),
            @else: new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                Positive<TurnStartedTriggeredEffectContext>(Self, standing),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    SetOn<TurnStartedTriggeredEffectContext>(Self, standing, 0),
                    SetOn<TurnStartedTriggeredEffectContext>(Self, LastBrokenCounter, index),
                ])));

    private static ICombatExpression<TContext, bool> Alive<TContext>(string marker) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CountTargetsExpression<TContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(marker))),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TurnStartedTriggeredEffectContext, bool> NoVolumesLeft() =>
        Volumes.Aggregate(
            (ICombatExpression<TurnStartedTriggeredEffectContext, bool>)
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
            (all, v) => new AndExpression<TurnStartedTriggeredEffectContext>(all,
                new NotExpression<TurnStartedTriggeredEffectContext>(
                    Alive<TurnStartedTriggeredEffectContext>(v.Marker))));

    // "No Further Reference": the concordance stops being scenery and the kill order becomes the law.
    private static IEffectNode<TurnStartedTriggeredEffectContext> NoFurtherReference() =>
        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Self, new StatusDefinitionId(UntouchableId)),
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Self, new StatusDefinitionId(TheConcordanceId)),
            Thesis(1, ThesisPremiseId),
            Thesis(2, ThesisAuthorityId),
            Thesis(3, ThesisConclusionId),
            // The Conclusion's thesis arrives already scheduled.
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                CounterIs<TurnStartedTriggeredEffectContext>(Self, LastBrokenCounter, 3),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    SetOn<TurnStartedTriggeredEffectContext>(Self, FinalResultDamageCounter, FinalResultStart),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(FinalResultId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), durationTurns: 2),
                ])),
            SetOn<TurnStartedTriggeredEffectContext>(Self, TransitionSpentCounter, 1),
        ]);

    private static IEffectNode<TurnStartedTriggeredEffectContext> Thesis(int index, string thesis) =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            CounterIs<TurnStartedTriggeredEffectContext>(Self, LastBrokenCounter, index),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Self, new StatusDefinitionId(thesis),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

    // ── Citations ─────────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<EnemyActionContext> CiteLater(int count) =>
        Bump<EnemyActionContext>(Across, CitationDueCounter, count);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteWhatIsDue() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                Positive<CardsDrawnTriggeredEffectContext>(Self, CitationDueCounter)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    Self, CardZone.Hand,
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            new TagId(CitationMark)),
                        // 9.3 Foundational Citation: while the Premise stands, the first citation each round
                        // also redacts the card it lands on.
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            new AndExpression<CardsDrawnTriggeredEffectContext>(
                                AcrossHas<CardsDrawnTriggeredEffectContext>(ThePremiseId),
                                IsZero<CardsDrawnTriggeredEffectContext>(Self, FoundationUsedCounter)),
                            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                            [
                                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                    new TagId(ActTwo.RedactedMark)),
                                Bump<CardsDrawnTriggeredEffectContext>(Self, FoundationUsedCounter, 1),
                            ])),
                    ]),
                    takeFirst: 1),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, CitationDueCounter, 0),
            ]));

    // What answering a citation is worth depends on which law the kill order wrote.
    private static IEffectNode<CardPlayedTriggeredEffectContext> OnCitationAnswered() =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            // Premise thesis: "Fulfilled Reference — the Grand Cross-Reference loses 7 HP."
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                AcrossHas<CardPlayedTriggeredEffectContext>(ThesisPremiseId),
                LoseHealthAcross<CardPlayedTriggeredEffectContext>(7)),
            // Conclusion thesis: "Fulfilled Reference — delay Final Result by 1, to a maximum of 3; if it is
            // already at 3, the Grand Cross-Reference loses 4 HP instead."
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                AcrossHas<CardPlayedTriggeredEffectContext>(ThesisConclusionId),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantStatusDurationExpression<CardPlayedTriggeredEffectContext>(
                            Across, new StatusDefinitionId(FinalResultId)),
                        ComparisonOperator.Less, new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                    new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(Across,
                        new ModifyStatusDurationNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(FinalResultId),
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                    @else: LoseHealthAcross<CardPlayedTriggeredEffectContext>(4))),
        ]);

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) => enemyId switch
    {
        PremiseId => Premise(intentId),
        AuthorityId => Authority(intentId),
        ConclusionId => Conclusion(intentId),
        EngineId => Engine(intentId),
        _ => null,
    };

    // "The isolated volume does not act." Only the linked pair moves, which is what keeps three bodies from
    // becoming three times the action.
    private static EffectProgram<EnemyActionContext> WhileLinked(
        int index, IEffectNode<EnemyActionContext> body) =>
        new(new ConditionalEffectNode<EnemyActionContext>(Linked(index), body));

    // With all three standing the rotation names the pair; with two left they are permanently linked; the
    // last one standing has nobody to be linked to and acts alone.
    private static ICombatExpression<EnemyActionContext, bool> Linked(int index)
    {
        // The rotation lives on the engine, which every volume can see as an ally.
        var rotation = new SumOverTargetsExpression<EnemyActionContext>(
            CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(EngineRulesId)),
            new CombatantCounterExpression<EnemyActionContext>(
                CombatantTargetSelectors.IterationTarget, RotationCounter));

        // Pairs by rotation: 0 → Premise+Authority, 1 → Authority+Conclusion, 2 → Conclusion+Premise.
        var inPair = index switch
        {
            1 => new OrExpression<EnemyActionContext>(RotationIs(rotation, 0), RotationIs(rotation, 2)),
            2 => new OrExpression<EnemyActionContext>(RotationIs(rotation, 0), RotationIs(rotation, 1)),
            _ => new OrExpression<EnemyActionContext>(RotationIs(rotation, 1), RotationIs(rotation, 2)),
        };

        // Fewer than three standing and there is no isolated volume left to sit out.
        return new OrExpression<EnemyActionContext>(
            new NotExpression<EnemyActionContext>(AllThreeStanding()), inPair);
    }

    private static ICombatExpression<EnemyActionContext, bool> RotationIs(
        ICombatExpression<EnemyActionContext, int> rotation, int at) =>
        new ComparisonExpression<EnemyActionContext>(
            rotation, ComparisonOperator.Equal, new ConstantExpression<EnemyActionContext>(at));

    private static ICombatExpression<EnemyActionContext, bool> AllThreeStanding() =>
        Volumes.Aggregate(
            (ICombatExpression<EnemyActionContext, bool>)
                new ComparisonExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(0), ComparisonOperator.Equal,
                    new ConstantExpression<EnemyActionContext>(0)),
            (all, v) => new AndExpression<EnemyActionContext>(all, VolumeStands(v.Marker)));

    // A volume can see the other volumes as allies, and itself only through its own marker — so "standing"
    // is asked of allies and of self together.
    private static ICombatExpression<EnemyActionContext, bool> VolumeStands(string marker) =>
        new OrExpression<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(Self, new StatusDefinitionId(marker)),
            new ComparisonExpression<EnemyActionContext>(
                new CountTargetsExpression<EnemyActionContext>(
                    CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(marker))),
                ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)));

    private static EffectProgram<EnemyActionContext>? Premise(string intentId) => intentId switch
    {
        "establish_the_initial_condition" => WhileLinked(1,
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(1), Block(6)])),
        // "14 damage; +4 if the player carries Overdue from either linked volume, maximum 18."
        "reject_the_unsupported_assumption" => WhileLinked(1,
            new ConditionalEffectNode<EnemyActionContext>(
                AcrossHas<EnemyActionContext>(ActTwo.OverdueId), Strike(18), @else: Strike(14))),
        // "Define the Applicable Case": the next citation is the player's to place.
        "define_the_applicable_case" => WhileLinked(1,
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(1), Block(8)])),
        // "Restate the Premise": the previous move again at about half.
        "restate_the_premise" => WhileLinked(1, Strike(7)),
        _ => null,
    };

    private static EffectProgram<EnemyActionContext>? Authority(string intentId) => intentId switch
    {
        "issue_the_binding_interpretation" => WhileLinked(2, Strike(16)),
        // "Overrule the Lesser Reading": the heaviest card in the next hand is redacted.
        "overrule_the_lesser_reading" => WhileLinked(2, ActTwo.RedactOne()),
        // "Cite the Higher Authority": both linked volumes brace.
        "cite_the_higher_authority" => WhileLinked(2,
            new ForEachTargetEffectNode<EnemyActionContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(TheConclusionId)),
                new GainBlockNode<EnemyActionContext>(
                    CombatantTargetSelectors.IterationTarget, new ConstantExpression<EnemyActionContext>(12)))),
        "enforce_without_appeal" => WhileLinked(2, Strike(21)),
        _ => null,
    };

    private static EffectProgram<EnemyActionContext>? Conclusion(string intentId) => intentId switch
    {
        // "9 + Stored Conclusion, maximum 31. Then Stored Conclusion → 0." Preserved, it cannot be spent.
        "draw_the_necessary_result" => WhileLinked(3,
            new ConditionalEffectNode<EnemyActionContext>(
                SelfHas<EnemyActionContext>(PreservedId),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(PreservedId)),
                @else: new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new DealDamageNode<EnemyActionContext>(Across,
                        new MinExpression<EnemyActionContext>(
                            new AddExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(9),
                                new CombatantCounterExpression<EnemyActionContext>(Self, StoredCounter)),
                            new ConstantExpression<EnemyActionContext>(31))),
                    SetOn<EnemyActionContext>(Self, StoredCounter, 0),
                ]))),
        "conclude_from_missing_evidence" => WhileLinked(3,
            new CausalSequenceEffectNode<EnemyActionContext>([Strike(13), ApplyToPlayer(Keywords.Doubt, 1)])),
        "preserve_the_result" => WhileLinked(3,
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Block(14),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(PreservedId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ])),
        "file_the_result_elsewhere" => WhileLinked(3,
            new CausalSequenceEffectNode<EnemyActionContext>(
                [ActTwo.MisfileOne(ActTwo.MisfiledMark), Strike(9)])),
        _ => null,
    };

    // The engine's four structural actions in Phase I — none of which deals direct damage — and the shared
    // Phase-II moves once the volumes are gone and it is the only thing left to fight.
    private static EffectProgram<EnemyActionContext>? Engine(string intentId) => intentId switch
    {
        "reweave_the_references" => Structural(
            // Advance to the next valid linked pair.
            new SetCombatantCounterNode<EnemyActionContext>(
                Self, RotationCounter,
                new RemainderExpression<EnemyActionContext>(
                    new AddExpression<EnemyActionContext>(
                        new CombatantCounterExpression<EnemyActionContext>(Self, RotationCounter),
                        new ConstantExpression<EnemyActionContext>(1)),
                    new ConstantExpression<EnemyActionContext>(3)),
                relative: false),
            // No Further Reference.
            new CausalSequenceEffectNode<EnemyActionContext>([Strike(18), ApplyToPlayer(Keywords.Doubt, 1)])),

        "all_entries_support_the_finding" => Structural(
            // Both linked volumes gain 10 Block.
            new ForEachTargetEffectNode<EnemyActionContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(TheConclusionId)),
                new GainBlockNode<EnemyActionContext>(
                    CombatantTargetSelectors.IterationTarget, new ConstantExpression<EnemyActionContext>(10))),
            // Collapse the Index.
            new CausalSequenceEffectNode<EnemyActionContext>(
                [Strike(13), ActTwo.MisfileOne(ActTwo.MisfiledMark)])),

        "reverse_the_citation" => Structural(
            new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(ReversedId),
                new ConstantExpression<EnemyActionContext>(1)),
            // Bind the Remaining Record.
            new CausalSequenceEffectNode<EnemyActionContext>([Block(18), CiteLater(1)])),

        "isolate_the_margin" => Structural(
            // The volume nobody is citing gets stronger while it waits.
            new ForEachTargetEffectNode<EnemyActionContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(ThePremiseId)),
                new ApplyStatusNode<EnemyActionContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(IsolatedStrengthId),
                    new ConstantExpression<EnemyActionContext>(1))),
            // Every Entry Ends Here.
            new CausalSequenceEffectNode<EnemyActionContext>([Strike(11), Strike(11)])),

        _ => null,
    };

    // While the volumes stand the engine only ever structures; once they are gone it fights. The transition
    // itself is silent — "no attack occurs during transition" — which the untouchable marker still being on
    // at the moment of the action is what says.
    private static EffectProgram<EnemyActionContext> Structural(
        IEffectNode<EnemyActionContext> concordance, IEffectNode<EnemyActionContext> alone) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            SelfHas<EnemyActionContext>(UntouchableId),
            concordance,
            @else: new CausalSequenceEffectNode<EnemyActionContext>(
            [
                alone,
                // All References Converge: everything it ever cited arrives at once, and then it is spent.
                new ConditionalEffectNode<EnemyActionContext>(
                    SelfHas<EnemyActionContext>(ConvergedId),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        Strike(12),
                        ApplyToPlayer(Keywords.Paperwork, 2),
                        ApplyToPlayer(Keywords.Doubt, 1),
                        new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(ConvergedId)),
                    ])),
            ])));

    // ── Small shared pieces ───────────────────────────────────────────────────────────────────────────────

    // The same reading of randomness the act uses everywhere: the draw pile is already shuffled, so its
    // first card is the random one.
    private static IEffectNode<TContext> RedactTheTopCard<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.DrawPile,
            ActTwo.Redact<TContext>(Self, new IteratedCardExpression<TContext>()),
            takeFirst: 1);

    private static IEffectNode<TContext> GuardUp<TContext>(int amount) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new ApplyStatusNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BindingGuardId),
                new ConstantExpression<TContext>(amount)));

    private static IEffectNode<TContext> LoseHealthAcross<TContext>(int amount) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new SetHealthNode<TContext>(
                CombatantTargetSelectors.IterationTarget,
                new SubtractExpression<TContext>(
                    new CombatantCurrentHealthExpression<TContext>(CombatantTargetSelectors.IterationTarget),
                    new ConstantExpression<TContext>(amount))));

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

    private static IEffectNode<TContext> Across_Remove<TContext>(string statusId) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new RemoveStatusNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(statusId)));

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

    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    private static StatusTriggerData Watch<TContext>(
        string trigger, EffectProgram<TContext> program, StatusTriggerScope scope = StatusTriggerScope.Bearer)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);

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
