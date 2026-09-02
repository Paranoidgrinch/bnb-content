using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stages 11 and 12 — The House of Linen and The Canopic Vaults. Preservation stops being a favour.
//
//   The Natron Bearer dries everything that would otherwise decay, and drying is burial: every affliction
//   preservation holds in place packs the player one deeper.
//   The Linen-Wrapped Embalmer writes the instructions first and wraps to them: an Embalmed application the
//   register made larger is a wrapping too tight to move in, and the player carries the weight.
//   The Unfinished Mummy still has its hooks in. While the player is preserved, the first blow they strike
//   catches on something.
//   The Fourfold Vessel Guardian is the whole canopic bureaucracy in one body, working one office at a time:
//   Body, Breath, Blood, Name, and round again. Which office is open is a face on the Guardian, never a
//   number — a player who has to plan around a rotation must be able to read where it stands.
//
// All three House-of-Linen bodies make their own Embalmed (§3.5), and every conversion here is capped at once
// a round, which is what keeps the Stage-37 chain — register, wrapping, weight, blow, burial — legible.
public static partial class ActFour
{
    public const string NatronBearerEnemyId = "natron_bearer";
    public const string EmbalmerEnemyId = "linen_wrapped_embalmer";
    public const string MummyEnemyId = "unfinished_mummy";
    public const string VesselGuardianEnemyId = "fourfold_vessel_guardian";

    public const string DryWhatWouldDecayId = "dry_what_would_decay";
    public const string WrappingInstructionsId = "instructions_for_wrapping";
    public const string HooksStillAttachedId = "hooks_still_attached";

    // The Guardian's four offices, in the order it works them.
    public const string OfficeOfTheBodyId = "office_of_the_body";
    public const string OfficeOfTheBreathId = "office_of_the_breath";
    public const string OfficeOfTheBloodId = "office_of_the_blood";
    public const string OfficeOfTheNameId = "office_of_the_name";

    private static readonly string[] Offices =
        [OfficeOfTheBodyId, OfficeOfTheBreathId, OfficeOfTheBloodId, OfficeOfTheNameId];

    public static CounterId DryingRead => new("drying_read");
    public static CounterId WrappedThisRound => new("wrapped_this_round");
    public static CounterId HookedThisTurn => new("hooked_this_turn");

    public static EffectProgram<EnemyActionContext>? LinenIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "fourfold_vessel_guardian.body_office" => Office(14, BurdenedId, 1, OfficeOfTheBodyId),
            "fourfold_vessel_guardian.breath_office" => Office(13, "panic", 1, OfficeOfTheBreathId),
            "fourfold_vessel_guardian.blood_office" => Office(14, "poison", 1, OfficeOfTheBloodId),
            "fourfold_vessel_guardian.name_office" => Office(11, InscribedId, 1, OfficeOfTheNameId),
            _ => null,
        };

    // ── the Natron Bearer ─────────────────────────────────────────────────────────────────────────────────

    // "Whenever Embalmed prevents a negative status from naturally decaying: apply 1 Entombed. Max once per
    // round." The same tally the Complaint Wall carves from, answered with burial instead of grievance — one
    // moment of preservation, one measure of natron, whatever else the round held.
    public static StatusData DryWhatWouldDecay() => new()
    {
        Id = DryWhatWouldDecayId,
        NameKey = "Dry What Would Decay",
        DescriptionKey =
            "Nothing on you is allowed to lapse, and drying is burial: once each round in which preservation "
            + "held an affliction in place, you are packed 1 Entombed deeper.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(PackTheNatron(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> PackTheNatron()
    {
        var bearer = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(bearer, DecaysPreserved, DryingRead);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // One measure of natron however many things were held: the cap is the master's.
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(EntombedId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: bearer),

                    MoveTheBookmark<TurnStartedTriggeredEffectContext>(bearer, DecaysPreserved, DryingRead),
                ])));
    }

    // ── the Linen-Wrapped Embalmer ────────────────────────────────────────────────────────────────────────

    // "When Inscribed strengthens an Embalmed application: apply 1 Burdened. Max once per round."
    //
    // Which is one question now that an amplification says what grew and what paid for it: was the enlarged
    // thing the wrapping, and was it the register that enlarged it? The Embalmer writes the instructions on
    // one turn and wraps to them on the next, so the chain is its own.
    public static StatusData InstructionsForWrapping() => new()
    {
        Id = WrappingInstructionsId,
        NameKey = "Instructions for Wrapping",
        DescriptionKey =
            "This embalmer wraps to what is written: once each round, an Embalmed the register made larger "
            + "leaves you carrying 1 Burdened.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(WrapToTheInstructions(), nameof(TriggerEvent.StatusApplicationAmplified),
                StatusTriggerScope.Anywhere),
            Trigger(ClearLatch<RoundStartedTriggeredEffectContext>(WrappingInstructionsId, WrappedThisRound),
                nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext> WrapToTheInstructions()
    {
        var embalmer = Bearer(WrappingInstructionsId);

        return new EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                    new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                        new TriggerEventStatusIsExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                            new StatusDefinitionId(EmbalmedId)),
                        new TriggerEventAmplifierIsExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                            new StatusDefinitionId(InscribedId))),
                    NotYetThisRound<StatusApplicationAmplifiedTriggeredEffectContext>(embalmer, WrappedThisRound)),
                new CausalSequenceEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                        embalmer, WrappedThisRound,
                        new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1),
                        relative: false),

                    new ApplyStatusNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(BurdenedId),
                        new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1),
                        sourceSelector: embalmer),
                ])));
    }

    // ── the Unfinished Mummy ──────────────────────────────────────────────────────────────────────────────

    // "While the player has Embalmed: the first Attack played each player turn adds 1 Entombed after
    // resolving." Preservation is not a state you move around in — the hooks are still attached, and the
    // first blow catches on them.
    public static StatusData HooksStillAttached() => new()
    {
        Id = HooksStillAttachedId,
        NameKey = "Hooks Still Attached",
        DescriptionKey =
            "This body is still in process. While you are Embalmed, the first Deed you play each turn "
            + "catches on its hooks: 1 Entombed.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(CatchOnTheHooks(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(ClearHookLatch(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<CardPlayedTriggeredEffectContext> CatchOnTheHooks()
    {
        var mummy = Bearer(HooksStillAttachedId);

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        // A Deed played by the player — this act's word for an attack.
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(Cards.CardAuthoring.DeedTag)),
                        new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId))),
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        // …while they are preserved, which is the state the hooks catch on.
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(EmbalmedId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        NotYetThisRound<CardPlayedTriggeredEffectContext>(mummy, HookedThisTurn))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        mummy, HookedThisTurn,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false),

                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(EntombedId),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), sourceSelector: mummy),
                ])));
    }

    // Once per PLAYER turn, so the latch is cleared when the player's turn begins and not when the round does.
    private static EffectProgram<TurnStartedTriggeredEffectContext> ClearHookLatch() =>
        new(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllAliveCombatants,
                    new StatusDefinitionId(HooksStillAttachedId)),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, HookedThisTurn,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false))));

    // ── the Fourfold Vessel Guardian ──────────────────────────────────────────────────────────────────────

    // One office at a time, and the office is a FACE. A rotation the player is meant to plan around has to be
    // readable off the body — "its dial shows which hour it is working in" was the lesson the Act-II bosses
    // paid for, and four named offices are that lesson applied to a standard.
    public static IReadOnlyList<StatusData> VesselOffices() =>
    [
        OfficeMarker(OfficeOfTheBodyId, "Office of the Body",
            "The Guardian is working the Body: it loads what you carry."),
        OfficeMarker(OfficeOfTheBreathId, "Office of the Breath",
            "The Guardian is working the Breath: it takes the air out of your hand."),
        OfficeMarker(OfficeOfTheBloodId, "Office of the Blood",
            "The Guardian is working the Blood: it leaves something in you."),
        OfficeMarker(OfficeOfTheNameId, "Office of the Name",
            "The Guardian is working the Name: it writes you into the register."),
    ];

    private static StatusData OfficeMarker(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // One office does its own work and puts its own face on: the vessel is opened, the package applied, and
    // every other office closed. Only the active one applies anything (§3.6).
    private static EffectProgram<EnemyActionContext> Office(
        int damage, string statusId, int stacks, string officeId)
    {
        var guardian = CombatantTargetSelectors.Source;

        var steps = new List<IEffectNode<EnemyActionContext>>
        {
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(statusId), Const(stacks)),
        };

        foreach (var closed in Offices.Where(o => o != officeId))
            steps.Add(new RemoveStatusNode<EnemyActionContext>(guardian, new StatusDefinitionId(closed)));

        steps.Add(new ApplyStatusNode<EnemyActionContext>(
            guardian, new StatusDefinitionId(officeId), Const(1), sourceSelector: guardian));

        return new EffectProgram<EnemyActionContext>(new CausalSequenceEffectNode<EnemyActionContext>(steps));
    }
}
