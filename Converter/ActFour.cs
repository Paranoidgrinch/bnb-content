using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV — The Licensing Labyrinth: the five words the whole act is written in, and the Stage-1 identities
// that teach the first of them.
//
// Act III's pressure was source-bound STANDING. Act IV's is PROCEDURE — the act asks not what you did but
// whether you did it to the letter — and five words carry it. Their canonical definition was reconstructed
// from several hundred uses across three masters and ratified by the user on 2026-08-29; this file is that
// ratification in code, and every later Act-IV stage is a reading of it.
//
//   Weighed X   the MEASURE. A visible requirement for this turn: spend exactly X Energy. At the end of the
//               turn required and actual are compared, and it is the DISTANCE between them that the act
//               answers — an enemy can punish by error band rather than by pass/fail.
//   Burdened X  the TAX. Every card costs 1 more Energy, and paying that surcharge works one stack off. That
//               is why it collides with the measure: the tax changes what the turn actually cost, so paying
//               it and hitting the measure are one decision, not two.
//   Inscribed X the REGISTER, and the amplifier. The next status applied to you lands one stack larger —
//               whichever direction it was going — and one Inscribed is spent doing it. Hence the act's
//               central player-side decision: spend the register on a blessing of your own, or let it
//               magnify the next curse.
//   Entombed X  BURIAL PRESSURE. It accumulates; at five it buries you — the turn is lost — and five are
//               spent, so the cycle can start again.
//   Embalmed X  PRESERVATION. Whenever something on the bearer would fade of its own accord, one Embalmed is
//               spent instead and the value stays. A player preserves their own buffs with it; an enemy
//               holds a debuff in place with it.
//
// Two of the five needed the engine to learn something (both bought in this step, both proved in
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
    public const string InscribedId = "inscribed";
    public const string EntombedId = "entombed";
    public const string EmbalmedId = "embalmed";
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

    // How many times the bearer has worked a stack of Burdened off by paying its surcharge. The Colossus of
    // the Endless Procession (IV-15) asks exactly this, and "a stack is gone" is not the same question: a
    // cleanse takes stacks too.
    public static CounterId BurdenPaid => new("burden_paid");

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

    // The register. Neutral on purpose: being written down is neither a blessing nor a curse until the next
    // thing happens to you, and which of the two it turns out to be is the player's decision to make.
    //
    // The whole of its behaviour is the engine's amplification seam — it enlarges the next application to its
    // bearer by one stack, in either direction, and is spent doing it. It never enlarges an application of
    // itself, and one application is enlarged once however much register is held.
    public static StatusData Inscribed() => new()
    {
        Id = InscribedId,
        NameKey = "Inscribed",
        DescriptionKey =
            "You are written into the register. The next status applied to you — good or bad — lands with "
            + "1 more stack, and 1 Inscribed is spent doing it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        Amplification = new StatusAmplificationData(
            StatusAmplificationScope.Any, AddStacks: 1, StacksSpent: 1),
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

    // Preservation. It carries no rule of its own: what it does is written at the one place in this game where
    // a status fades of its own accord — `Fade` below — and every fading status in the port asks there.
    public static StatusData Embalmed() => new()
    {
        Id = EmbalmedId,
        NameKey = "Embalmed",
        DescriptionKey =
            "Preserved. Whenever a status on this character would fade of its own accord, 1 Embalmed is "
            + "spent instead and the status keeps its stack.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
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
    // Wax paying for the enemy turn. Every such loss in the port is written through here, because Embalmed is
    // defined against exactly this event and nothing else: a stack spent, cleansed or paid away is not a fade.
    //
    // When the bearer is preserved the fade does not happen and one Embalmed is spent in its place, which
    // makes Embalmed X read "the next X fades on this character do not happen" — one rule, no ordering
    // agreement between two turn-end triggers, and the same answer whichever status was about to shrink.
    public static IEffectNode<TContext> Fade<TContext>(
        ICombatantTargetSelector bearer, string statusId, int stacks = 1) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId(EmbalmedId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TContext>(0)),
            new ModifyStatusStacksNode<TContext>(
                bearer, new StatusDefinitionId(EmbalmedId), new ConstantExpression<TContext>(-1)),
            new ModifyStatusStacksNode<TContext>(
                bearer, new StatusDefinitionId(statusId), new ConstantExpression<TContext>(-stacks)));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> All() =>
    [
        Weighed(),
        Burdened(),
        Inscribed(),
        Entombed(),
        Embalmed(),
        LabyrinthBody(),
    ];

    // The standard roster, stage by stage.
    public static readonly IReadOnlySet<string> Identities = new HashSet<string>(StringComparer.Ordinal)
    {
        // Stage 1 — the Boundary Stelae
        "reed_cord_surveyor", "crooked_rod_bearer",
    };

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
