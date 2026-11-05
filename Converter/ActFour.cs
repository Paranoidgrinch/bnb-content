using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV — The Licensing Labyrinth: the three words the whole act is written in, and the Stage-1 identities
// that teach the first of them.
//
// Act III's pressure was source-bound STANDING. Act IV's is PROCEDURE — the act asks not what you did but
// whether you did it to the letter — and three words carry it. Their canonical definition was reconstructed
// from several hundred uses across three masters and ratified by the user on 2026-08-29; this file is that
// ratification in code, and every later Act-IV stage is a reading of it.
//
//   Weighed X   the MEASURE. A visible requirement for this turn: spend exactly X Energy. At the end of the
//               turn required and actual are compared, and it is the DISTANCE between them that the act
//               answers — an enemy can punish by error band rather than by pass/fail.
//   Burdened X  the TAX. Every card costs 1 more Energy, and paying that surcharge works one stack off. That
//               is why it collides with the measure: the tax changes what the turn actually cost, so paying
//               it and hitting the measure are one decision, not two.
//   Entombed X  BURIAL PRESSURE. It accumulates; at five it buries you — the turn is lost — and five are
//               spent, so the cycle can start again.
//
// Since the content cut of 2026-10-30 the act speaks only these three. Inscribed and Embalmed are gone; the
// one good idea among them, preservation, is the Natron Bearer's own rule.
//
// Two of the original five needed the engine to learn something (both bought in this step, both proved in
// RogueDeck-Core's own tests): what a turn has actually COST (`resourceSpentThisTurn` — the measure has no
// meaning without it), and a status that ENLARGES the next application to its bearer and is spent doing it
// (a StatusAmplificationSpec, the mirror of the prohibition Act III's Safe-Conduct is built on). The other
// three are compositions of what was already there: a flat cost modifier plus a rule that hears the payment;
// the engine's Stun for one turn at a threshold; and a decay that asks, at the one place fading is written
// down, whether the bearer is preserved.
public static partial class ActFour
{
    // ── the vocabulary ────────────────────────────────────────────────────────────────────────────────────

    public const string WeighedId = "weighed";
    public const string BurdenedId = "burdened";
    public const string EntombedId = "entombed";
    public const string LabyrinthBodyId = "labyrinth_body";

    // What Entombed comes to before it buries its bearer, and what is spent when it does (elite master §6.3).
    public const int EntombedThreshold = 5;

    // What the last completed measure came to, kept on the player: 0 = no measure has ever been taken in this
    // fight, 1 = the last one was exact, 2 = it was off by one, and so on. ONE counter carries both facts
    // because "was there a measure?" and "how far off was it?" are always asked together, and a reader that
    // had to check two counters could read a distance belonging to no measure at all.
    //
    // It is a record and not a demand, which is why it stays a counter: what the player has to ACT on is the
    // requirement, and that is a status they can see (the marker rule from Act III's boss pass).
    public static CounterId MeasureResult => new("measure_result");

    // How many measures have been MET and how many MISSED in this fight — two tallies that only ever grow.
    // A record of the last measure cannot answer "once per resolution": the same failure would be answered
    // again every time a body looked. A body that cares keeps its own bookmark in one of these and takes the
    // difference, which is the same ordering-free idiom the Hungry Grain Thief eats by.
    public static CounterId MeasuresMet => new("measures_met");
    public static CounterId MeasuresFailed => new("measures_failed");

    // How many times the bearer has worked a stack of Burdened off by paying its surcharge. The Colossus of
    // the Endless Procession (IV-15) asks exactly this, and "a stack is gone" is not the same question: a
    // cleanse takes stacks too.
    public static CounterId BurdenPaid => new("burden_paid");

    // How many afflictions preservation has held in place on this character — written at the one fading
    // point, because "Embalmed prevented a decay" is a moment and not a state.
    public static CounterId DecaysPreserved => new("decays_preserved");

    // …and its mirror: how many afflictions faded on this character with nothing holding them. Written at the
    // same one place and for the same reason — the Mummified Overseer's wrapping tightens on the first and
    // loosens on the second, and neither is a state anything else could read off the character afterwards.
    public static CounterId DecaysUnpreserved => new("decays_unpreserved");

    // How much Energy Fatigue has actually taken out of the player's hands in this fight — written by
    // Fatigue itself at the one moment it takes any, because losing a resource raises nothing a rule can
    // hear, and "the player has Fatigue" is a different fact: a player at zero Energy loses nothing to it.
    public static CounterId EnergyTakenByFatigue => new("energy_taken_by_fatigue");

    // The Crooked Rod Bearer's alternation, on the Bearer itself: one crooked standard per body.
    public static CounterId CrookedStep => new("crooked_rod_step");

    // ── the five words ────────────────────────────────────────────────────────────────────────────────────

    // The measure. Stacks ARE the requirement, so the player reads what is demanded off the status itself.
    //
    // It resolves at the END of the bearer's turn, which is the only moment the question has an answer, and
    // it removes itself doing so: a measure is taken once. What it leaves behind is the record — and nothing
    // else, because the punishment belongs to whoever raised the measure (or to whoever is watching), not to
    // the measure itself. That is §3.2 exactly: an enemy may listen to a completed check without owning it.
    public static StatusData Weighed() => new()
    {
        Id = WeighedId,
        NameKey = "Weighed",
        DescriptionKey =
            "The measure: this turn you must spend exactly this much Energy. At the end of your turn the "
            + "measure is taken, and how far you were from it is what the labyrinth answers.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    // 1 + |spent − required|: 1 is an exact measure, 2 is off by one, 3 and up is a major
                    // error. The offset is what lets one counter say "a measure was taken" as well.
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, MeasureResult,
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                            new AbsExpression<TurnEndedTriggeredEffectContext>(
                                new SubtractExpression<TurnEndedTriggeredEffectContext>(
                                    new ResourceSpentThisTurnExpression<TurnEndedTriggeredEffectContext>(
                                        CombatantTargetSelectors.Source),
                                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                        CombatantTargetSelectors.Source, new StatusDefinitionId(WeighedId))))),
                        relative: false),

                    // …and the running tallies a body can keep a bookmark in. The record above says what the
                    // LAST measure came to; these say how many have been met and missed altogether, which is
                    // the only way a rule can answer "once per resolution" without agreeing with anybody
                    // about the order two turn-end rules fire in.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new ResourceSpentThisTurnExpression<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source),
                            ComparisonOperator.Equal,
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(WeighedId))),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, MeasuresMet,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, MeasuresFailed,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true)),

                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(WeighedId)),
                ])),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // The tax. A flat surcharge on every card — not a per-stack one, which at three stacks would price the
    // whole hand out of the turn — and the stack is worked off by the surcharge being PAID.
    //
    // "Paid" is the operative word: a card that ends up costing nothing (a free play) does not work the
    // burden off, because nothing was paid. The engine's cost-payment event reports what the play actually
    // came to, which is the same number the measure reads, so the two words meet on one figure.
    public static StatusData Burdened() => new()
    {
        Id = BurdenedId,
        NameKey = "Burdened",
        DescriptionKey =
            "Every card you play costs 1 more Energy. Paying that surcharge works one stack off.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost,
                PassiveModifierOperation.AddFlat, 1, RestrictDamageKind: null),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<CardCostPaidTriggeredEffectContext>(
                new ConditionalEffectNode<CardCostPaidTriggeredEffectContext>(
                    new ComparisonExpression<CardCostPaidTriggeredEffectContext>(
                        new EventAmountExpression<CardCostPaidTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardCostPaidTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardCostPaidTriggeredEffectContext>(
                    [
                        new ModifyStatusStacksNode<CardCostPaidTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(BurdenedId),
                            new ConstantExpression<CardCostPaidTriggeredEffectContext>(-1)),

                        // …and the payment itself is written down, because a later enemy asks whether a
                        // burden was worked off by paying rather than taken off by a cleanse.
                        new SetCombatantCounterNode<CardCostPaidTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, BurdenPaid,
                            new ConstantExpression<CardCostPaidTriggeredEffectContext>(1), relative: true),
                    ]))),
                nameof(TriggerEvent.CardCostPaid)),
        ],
    };

    // Burial pressure. It does nothing at all until it comes to five, and then it takes the turn: the engine's
    // Stun for one turn, which is exactly "the player loses the turn" — and five stacks are spent, so the
    // cycle can build again rather than the fight ending in a permanent burial.
    //
    // It is read at the bearer's TURN START, not the moment the fifth stack lands, because a turn can only be
    // lost before it is had. A stack applied during the player's own turn therefore waits for the next one.
    public static StatusData Entombed() => new()
    {
        Id = EntombedId,
        NameKey = "Entombed",
        DescriptionKey =
            "Burial pressure. At 5 it buries you: you lose that turn, and 5 Entombed are spent.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(EntombedId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(EntombedThreshold)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(EntombedId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(-EntombedThreshold)),

                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.StunStatus,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                            durationTurns: 1),
                    ]))),
                nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // Every Licensing Labyrinth body wears this, so a rule can say "the parties in this fight" without
    // knowing which side it is looking from — the same seam Act III's Green Docket body is.
    public static StatusData LabyrinthBody() => new()
    {
        Id = LabyrinthBodyId,
        NameKey = "Licensed Party",
        DescriptionKey = "A party under the procedure of the Licensing Labyrinth.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── fading, and what preservation does to it ──────────────────────────────────────────────────────────

    // A status LOSING a stack because a turn went by — Panic shedding one, Poison fading after its tick, Ward
    // Wax paying for the enemy turn. Every such loss in the port is written through here.
    //
    // Since the content cut of 2026-10-30 preservation is no longer an act word (Embalmed is gone): it is the
    // Natron Bearer's own rule. While a Natron Bearer stands, an AFFLICTION on the player does not fade at all,
    // and the moment is written down (DecaysPreserved), because the Bearer and the Linen House's Overseer both
    // answer it. Anything else fades as it always did, and is counted as an unpreserved fade if it hurt.
    public static IEffectNode<TContext> Fade<TContext>(
        ICombatantTargetSelector bearer, string statusId, int stacks = 1, bool negative = true)
        where TContext : class =>
        negative
            ? new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(bearer, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    new ComparisonExpression<TContext>(
                        new CountTargetsExpression<TContext>(
                            CombatantTargetSelectors.WithStatus(
                                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(DryWhatWouldDecayId))),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0))),
                new SetCombatantCounterNode<TContext>(
                    bearer, DecaysPreserved, new ConstantExpression<TContext>(1), relative: true),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ModifyStatusStacksNode<TContext>(
                        bearer, new StatusDefinitionId(statusId), new ConstantExpression<TContext>(-stacks)),
                    new SetCombatantCounterNode<TContext>(
                        bearer, DecaysUnpreserved, new ConstantExpression<TContext>(1), relative: true),
                ]))
            : new ModifyStatusStacksNode<TContext>(
                bearer, new StatusDefinitionId(statusId), new ConstantExpression<TContext>(-stacks));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> All() =>
    [
        Weighed(),
        Burdened(),
        Entombed(),
        LabyrinthBody(),
        // the normal bodies (reworked 2026-11-04) and the act's words: the Scale, Risen and the jars
        .. NormalStatuses(),
        .. LabyrinthStatuses(),
        // The elite layer
        .. EliteStatuses(),
        .. CutStatuses(),
        // …and the bosses
        .. BossStatuses(),
    ];

    // The standard roster, stage by stage.
    // The standard roster since the content cut of 2026-10-30, stage by stage.
    public static readonly IReadOnlySet<string> Identities = new HashSet<string>(StringComparer.Ordinal)
    {
        // Stage 1 — the Boundary Stelae
        "reed_cord_surveyor", "crooked_rod_bearer", "silt_buried_farmer_shade",
        // Stage 2 — the Granary Courts
        "crocodile_of_the_short_measure", "jar_seal_scarab_swarm", "hungry_grain_thief",
        // Stage 3 — the Pyramid Works
        "stone_hauler_ushabti", "runaway_laborer", "fallen_capstone_golem",
        // Stage 4 — the House of Linen
        "unfinished_mummy", "natron_bearer", "fourfold_vessel_guardian",
        // Stage 5 — the Necropolis of Thieves
        "cursed_loot_bearer", "false_door_finder", "jackal_of_the_western_gate",
        // Stage 6 — the Hall of Two Truths
        "assessor_of_the_forty_two", "moon_cycle_ibis", "name_erasing_chisel_spirit", "eclipse_scarab",
    };

    // ── shared idioms ─────────────────────────────────────────────────────────────────────────────────────

    // "The body whose rule this is" — the living combatant carrying that rule. FirstTarget because a scalar
    // read needs one combatant; no two bodies in this act carry the same rule.
    public static ICombatantTargetSelector Bearer(string ruleId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)));

    // The bookmark idiom, twice over.
    //
    // A body that lives off a running tally the player keeps — surcharges paid, measures missed — must not
    // answer the same entry twice, and must not depend on the order two rules fire in. So it keeps a
    // bookmark in the tally, reads the DIFFERENCE at a fixed moment of its own (its own turn start), and
    // then moves the bookmark up. A body that joins late, looks twice, or misses a turn takes exactly its
    // share and no more.
    public static ICombatExpression<TContext, int> SinceLastLooked<TContext>(
        ICombatantTargetSelector body, CounterId tally, CounterId bookmark) where TContext : class =>
        new SubtractExpression<TContext>(
            new CombatantCounterExpression<TContext>(Applicant, tally),
            new CombatantCounterExpression<TContext>(body, bookmark));

    // The same idiom over RESOLUTIONS rather than over one of the two tallies: a body that answers every
    // measure that resolves, met or missed, reads both and keeps one bookmark in their sum.
    public static ICombatExpression<TContext, int> ResolutionsSinceLastLooked<TContext>(
        ICombatantTargetSelector body, CounterId bookmark) where TContext : class =>
        new SubtractExpression<TContext>(Resolutions<TContext>(),
            new CombatantCounterExpression<TContext>(body, bookmark));

    public static IEffectNode<TContext> MoveTheResolutionBookmark<TContext>(
        ICombatantTargetSelector body, CounterId bookmark) where TContext : class =>
        new SetCombatantCounterNode<TContext>(body, bookmark, Resolutions<TContext>(), relative: false);

    private static ICombatExpression<TContext, int> Resolutions<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new CombatantCounterExpression<TContext>(Applicant, MeasuresMet),
            new CombatantCounterExpression<TContext>(Applicant, MeasuresFailed));

    public static IEffectNode<TContext> MoveTheBookmark<TContext>(
        ICombatantTargetSelector body, CounterId tally, CounterId bookmark) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            body, bookmark, new CombatantCounterExpression<TContext>(Applicant, tally), relative: false);

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);

    // ── the questions several bodies ask ──────────────────────────────────────────────────────────────────

    // An ORIGINAL affliction landing on the player: a debuff, on the applicant, that is not itself a copy.
    // In a status-application trigger "source" is whoever applied it and "eventTarget" is who received it.
    private static ICombatExpression<TContext, bool> OriginalAfflictionOnThePlayer<TContext>()
        where TContext : class =>
        new AndExpression<TContext>(
            new AndExpression<TContext>(
                new TargetHasStatusExpression<TContext>(
                    CombatantTargetSelectors.EventTarget, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff)),
            new NotExpression<TContext>(new TriggerEventIsReplicatedExpression<TContext>()));

    private static ICombatExpression<TContext, bool> NotYetThisRound<TContext>(
        ICombatantTargetSelector body, CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(body, latch),
            ComparisonOperator.Equal,
            new ConstantExpression<TContext>(0));

    // A round turning is nobody's own event, so a latch is cleared for every body that carries the rule.
    private static EffectProgram<TContext> ClearLatch<TContext>(string ruleId, CounterId latch)
        where TContext : class =>
        new(new ForEachTargetEffectNode<TContext>(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)),
            new SetCombatantCounterNode<TContext>(
                CombatantTargetSelectors.IterationTarget, latch,
                new ConstantExpression<TContext>(0), relative: false)));
}
