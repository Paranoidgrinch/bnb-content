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
    // `negative` says whether the thing being held is an affliction. A preservation is written down only for
    // those, because the one body that lives off preservation — Stage 8's Hieroglyphic Complaint Wall — is
    // built on grievances staying legally active, and a player's own wax being held is not a grievance.
    public static IEffectNode<TContext> Fade<TContext>(
        ICombatantTargetSelector bearer, string statusId, int stacks = 1, bool negative = true)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(bearer, new StatusDefinitionId(EmbalmedId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TContext>(0)),
            negative
                ? new CausalSequenceEffectNode<TContext>(
                [
                    new ModifyStatusStacksNode<TContext>(
                        bearer, new StatusDefinitionId(EmbalmedId), new ConstantExpression<TContext>(-1)),
                    new SetCombatantCounterNode<TContext>(
                        bearer, DecaysPreserved, new ConstantExpression<TContext>(1), relative: true),
                ])
                : new ModifyStatusStacksNode<TContext>(
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
        // Stage 2 — the Gate of Counted Names
        Uncounted(),
        NoNumberInTheRegister(),
        StolenName(),
        ForgedEntry(),
        ChewedCredentials(),
        // Stage 3 — the Granary Courts
        Ration(),
        HungryForRations(),
        // Stage 4 — the Floodmark Basins
        HighWaterMark(),
        SiltedRecord(),
        SiltedRecordRule(),
        Flood(),
        RisingFlood(),
        // Stage 5 — the Tribute Causeway
        AdministrativeCostOfTribute(),
        Tally(),
        ThirdTally(),
        PresentedInFull(),
        NothingWasPresented(),
        // Stage 6 — the Corvée Yards
        WorkStrain(),
        LoseTheWorkRhythm(),
        Escape(),
        EscapePlan(),
        Stone(),
        StoneWork(),
        // Stages 7 and 8 — the Monument Works and the Hall of Reed and Ink
        Placement(),
        KeptOath(),
        BrokenOath(),
        FoundationOath(),
        FreshPigment(),
        FreshPigmentRule(),
        Complaint(),
        UndismissedComplaint(),
        // Stages 9 and 10 — the Courts of the Royal Seal and the Processional Galleries
        AuthorizedImpression(),
        CounterfeitAuthorization(),
        ProcessionalApproval(),
        // Stages 11 and 12 — the House of Linen and the Canopic Vaults
        DryWhatWouldDecay(),
        InstructionsForWrapping(),
        HooksStillAttached(),
        .. VesselOffices(),
        // Stages 13 and 14 — the Necropolis Warrens and the Chamber of Fixed Days
        NecropolisPassage(),
        EveryObjectRequiresAForm(),
        LunarReturn(),
        .. LastRites(),
        ApproachOfNoon(),
        // Stage 15 — the Cartouche Chambers
        ChiselSet(),
        EraseTheFavor(),
        RoyalFavor(),
        DynasticFavor(),
        // Stages 16 and 17 — the Hall of the Balance and the Sealed Court Before Eternity
        FeatherOfFinalMeasure(),
        BalanceOpen(),
        WaitsBeneathTheScale(),
        JawsOpen(),
        EntryDoesNotClose(),
        // The elite layer
        .. EliteStatuses(),
    ];

    // The standard roster, stage by stage.
    public static readonly IReadOnlySet<string> Identities = new HashSet<string>(StringComparer.Ordinal)
    {
        // Stage 1 — the Boundary Stelae
        "reed_cord_surveyor", "crooked_rod_bearer",
        // Stage 2 — the Gate of Counted Names
        "uncounted_pilgrim", "cobra_of_the_entry_mark", "name_eating_baboon",
        // Stage 3 — the Granary Courts
        "crocodile_of_the_short_measure", "jar_seal_scarab_swarm", "hungry_grain_thief",
        // Stage 4 — the Floodmark Basins
        "flood_mark_reader", "drowned_field_scribe", "silt_buried_farmer_shade",
        // Stage 5 — the Tribute Causeway
        "foreign_tribute_shade", "donkey_of_the_third_tally", "empty_handed_envoy",
        // Stage 6 — the Corvée Yards
        "rope_gang_wraith", "runaway_laborer", "stone_hauler_ushabti",
        // Stage 7 — the Monument Works
        "fallen_capstone_golem", "cornerstone_oath_stone",
        // Stage 8 — the Hall of Reed and Ink
        "palette_bearing_apprentice", "hieroglyphic_complaint_wall",
        // Stage 9 — the Courts of the Royal Seal
        "sun_seal_bearer", "false_seal_forger",
        // Stage 10 — the Processional Galleries
        "kneeling_petitioners",
        // Stage 11 — the House of Linen
        "natron_bearer", "linen_wrapped_embalmer", "unfinished_mummy",
        // Stage 12 — the Canopic Vaults
        "fourfold_vessel_guardian",
        // Stage 13 — the Necropolis Warrens
        "false_door_finder", "cursed_loot_bearer",
        // Stage 14 — the Chamber of Fixed Days
        "star_table_scribe", "moon_cycle_ibis", "eclipse_scarab",
        // Stage 15 — the Cartouche Chambers
        "name_erasing_chisel_spirit", "royal_genealogy_wall",
        // Stages 16 and 17 — the final forms. No new identities: each of these five is a body from an
        // earlier stage in the office the act promoted it into, and the roster counts it once, there.
        "feather_bearer", "crocodile_beneath_the_balance",
        "golden_ushabti_captain", "eternal_reed_scribe", "oathbound_gate",
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

    // A body whose STATE follows a number on the player: while the applicant's `watchedId` is at (or below)
    // `threshold`, the body wearing `ruleId` also wears `markerId`, and otherwise it does not.
    //
    // Two bodies in this act live on this — the Uncounted Pilgrim, legible only while the player is in the
    // register, and the Drowned Field Scribe, whose ink thickens once the player is deep enough in silt —
    // and both taught the same two lessons, which is why the rule is written once:
    //
    //   a status losing its LAST stack is reported as an EXPIRY, not as a removal or a stack change, and
    //   running out is the commonest way a number reaches zero. A watcher blind to expiry shows a stale
    //   state for the rest of the fight;
    //   and the OPENING state has no event at all: a player who walks in already carrying the number raises
    //   nothing to hear. So the state is also settled at every turn start — the TURN and not the round,
    //   because a fight's first round starts before its bodies are dressed, and a rule nobody wears yet
    //   does not fire.
    public static IReadOnlyList<StatusTriggerData> FollowTheApplicant(
        string ruleId, string markerId, string watchedId, int threshold, bool wornAtOrAbove)
    {
        EffectProgram<TContext> Settle<TContext>(bool gated) where TContext : class
        {
            var body = Bearer(ruleId);

            var atOrAbove = new ComparisonExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(Applicant, new StatusDefinitionId(watchedId)),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TContext>(threshold));

            IEffectNode<TContext> wear =
                new ConditionalEffectNode<TContext>(
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(body, new StatusDefinitionId(markerId))),
                    new ApplyStatusNode<TContext>(
                        body, new StatusDefinitionId(markerId), new ConstantExpression<TContext>(1)));

            IEffectNode<TContext> shed = new RemoveStatusNode<TContext>(body, new StatusDefinitionId(markerId));

            IEffectNode<TContext> settle = wornAtOrAbove
                ? new ConditionalEffectNode<TContext>(atOrAbove, wear, shed)
                : new ConditionalEffectNode<TContext>(atOrAbove, shed, wear);

            return new EffectProgram<TContext>(
                gated
                    // Only movements of the number being watched are worth settling for.
                    ? new ConditionalEffectNode<TContext>(
                        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(watchedId)), settle)
                    : settle);
        }

        return
        [
            Trigger(Settle<StatusAppliedTriggeredEffectContext>(gated: true),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(Settle<StatusMergedTriggeredEffectContext>(gated: true),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(Settle<StatusStacksChangedTriggeredEffectContext>(gated: true),
                nameof(TriggerEvent.StatusStacksChanged), StatusTriggerScope.Anywhere),
            Trigger(Settle<StatusRemovedTriggeredEffectContext>(gated: true),
                nameof(TriggerEvent.StatusRemoved), StatusTriggerScope.Anywhere),
            Trigger(Settle<StatusExpiredTriggeredEffectContext>(gated: true),
                nameof(TriggerEvent.StatusExpired), StatusTriggerScope.Anywhere),
            Trigger(Settle<TurnStartedTriggeredEffectContext>(gated: false),
                nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
        ];
    }

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
