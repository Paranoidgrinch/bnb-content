using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// The lasting statuses behind the GENERAL pool's Act-I cards: two Rites, and the short-lived marks three
// Workings leave on an enemy or on you.
//
// Same shapes as the Bureaucrat's Rites (BureaucratRites): the rule lives on a status, "the first time each
// turn" is a counter latch cleared at turn start, and a rule that watches the other side of the fight is
// scoped to the whole fight rather than to its bearer.
public static class GeneralRites
{
    public const string NotaryBeetle = "notary_beetle";
    public const string ReciprocalEdict = "reciprocal_edict";
    public const string MortgageSigil = "mortgage_sigil";
    public const string SilentHearing = "silent_hearing";
    public const string SealedMantle = "sealed_mantle";
    public const string SanctionedCharm = "sanctioned_charm";

    // What the hearing owes the player. Block granted while the ENEMY is acting would be swept away at the
    // player's own turn start, so the debt is remembered and paid after their next draw — the same shape
    // Ward Wax uses for the same reason.
    public const string HearingOwed = "silent_hearing_owed";

    // The negative statuses the two pools file on enemies. Notary Beetle has to name them: it seeds "one more
    // of the status that was just applied", and no node can apply "whatever the event named" — so the rule is
    // written once per status it could be. A new negative status added later must appear here too.
    private static readonly string[] Fileable =
        [Keywords.Paperwork, Keywords.Doubt, Keywords.Seal, Keywords.Lien, Keywords.Citation, Keywords.BloodInk];

    public static IReadOnlyList<StatusData> All() =>
    [
        Beetle(NotaryBeetle, "Notary Beetle"),
        Beetle(NotaryBeetle + "+", "Notary Beetle+"),
        Edict(ReciprocalEdict, "Reciprocal Edict"),
        Edict(ReciprocalEdict + "+", "Reciprocal Edict+"),
        Sigil(MortgageSigil, "Mortgage Sigil", 3),
        Sigil(MortgageSigil + "+", "Mortgage Sigil+", 4),
        Hearing(SilentHearing, "Silent Hearing", 7),
        HearingOwedStatus(7),
        Mantle(),
        Charm(),
    ];

    // ── Notary Beetle ─────────────────────────────────────────────────────────────────────────────────────
    // "The first time each turn you apply a negative Status to an enemy that does not already have that
    // Status, apply 1 additional stack of it."
    //
    // "Does not already have it" is answered by the EVENT rather than by looking: a status arriving where
    // there was none raises StatusApplied, and one landing on top of itself raises StatusMerged. So watching
    // only StatusApplied IS the "new to that enemy" condition, and no state has to be remembered.
    private static CounterId BeetleFed => new("notary_beetle_fed");

    private static StatusData Beetle(string id, string name)
    {
        var enemy = CombatantTargetSelectors.EventTarget;
        var wearer = CombatantTargetSelectors.IterationTarget;

        IEffectNode<StatusAppliedTriggeredEffectContext> Seed(string status) =>
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(new StatusDefinitionId(status)),
                new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                        enemy, new StatusDefinitionId(status),
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    new SetCombatantCounterNode<StatusAppliedTriggeredEffectContext>(
                        wearer, BeetleFed, new ConstantExpression<StatusAppliedTriggeredEffectContext>(1),
                        relative: false),
                ]));

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ForEachTargetEffectNode<StatusAppliedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        // Not to the applicant: this seeds what the player files on the OTHER side.
                        new NotExpression<StatusAppliedTriggeredEffectContext>(
                            new TargetHasStatusExpression<StatusAppliedTriggeredEffectContext>(
                                enemy, new StatusDefinitionId(Keywords.ApplicantMarker))),
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantCounterExpression<StatusAppliedTriggeredEffectContext>(wearer, BeetleFed),
                            ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0))),
                    new SequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                        Fileable.Select(Seed).ToArray()))));

        return Rite(id, name,
            "The first time each turn you apply a negative status to an enemy that does not already have it, " +
            "apply 1 additional stack.",
            [Trigger(program, nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere), ClearLatch(BeetleFed)]);
    }

    // ── Reciprocal Edict ──────────────────────────────────────────────────────────────────────────────────
    // "The first time each turn your Censure prevents a negative Status applied by an enemy, apply 2 Censure
    // to that enemy. The first time each turn Censure prevents a positive Status on an enemy, gain 1 Censure."
    //
    // Both halves are the same event seen from either side of the fight: a refusal reports who refused
    // (source) and who was refused (eventTarget). Which half is which is decided by whether the refuser is
    // the applicant.
    private static CounterId EdictOnYou => new("reciprocal_edict_yours");
    private static CounterId EdictOnThem => new("reciprocal_edict_theirs");

    private static StatusData Edict(string id, string name)
    {
        var refuser = CombatantTargetSelectors.Source;
        var applier = CombatantTargetSelectors.EventTarget;
        var wearer = CombatantTargetSelectors.IterationTarget;

        IEffectNode<StatusApplicationBlockedTriggeredEffectContext> Half(
            CounterId latch, bool refuserIsApplicant, ICombatantTargetSelector gains, int censure) =>
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    Applicant<StatusApplicationBlockedTriggeredEffectContext>(refuser, refuserIsApplicant),
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantCounterExpression<StatusApplicationBlockedTriggeredEffectContext>(wearer, latch),
                        ComparisonOperator.Equal,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                        gains, new StatusDefinitionId(Keywords.Censure),
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(censure)),
                    new SetCombatantCounterNode<StatusApplicationBlockedTriggeredEffectContext>(
                        wearer, latch, new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1),
                        relative: false),
                ]));

        var program = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ForEachTargetEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                new SequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                [
                    // YOUR Censure refused something an enemy filed: that enemy is Censured back.
                    Half(EdictOnYou, refuserIsApplicant: true, applier, 2),
                    // An ENEMY's Censure refused a buff: you gain a Censure of your own.
                    Half(EdictOnThem, refuserIsApplicant: false, wearer, 1),
                ])));

        return Rite(id, name,
            "The first time each turn your Censure prevents a status an enemy filed, apply 2 Censure to that " +
            "enemy. The first time each turn Censure prevents a status on an enemy, gain 1 Censure.",
            [
                Trigger(program, nameof(TriggerEvent.StatusApplicationPrevented), StatusTriggerScope.Anywhere),
                ClearLatch(EdictOnYou),
                ClearLatch(EdictOnThem),
            ]);
    }

    private static ICombatExpression<TContext, bool> Applicant<TContext>(
        ICombatantTargetSelector who, bool shouldBe) where TContext : class
    {
        ICombatExpression<TContext, bool> wears =
            new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(Keywords.ApplicantMarker));
        return shouldBe ? wears : new NotExpression<TContext>(wears);
    }

    // ── Mortgage Sigil ────────────────────────────────────────────────────────────────────────────────────
    // "The next time the target gains Block before the end of its next turn, apply N additional Lien." A mark
    // ON THE ENEMY, spent by the Block it is waiting for, and gone when that turn ends either way.
    private static StatusData Sigil(string id, string name, int lien) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = $"The next Block this character gains costs it {lien} more Lien.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<BlockGainedTriggeredEffectContext>(
                new CausalSequenceEffectNode<BlockGainedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<BlockGainedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Lien),
                        new ConstantExpression<BlockGainedTriggeredEffectContext>(lien)),
                    new RemoveStatusNode<BlockGainedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(id)),
                ])), nameof(TriggerEvent.BlockGained)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── Silent Hearing ────────────────────────────────────────────────────────────────────────────────────
    // "Until your next turn, if the target performs a damaging action, gain N Block." The mark sits on the
    // ENEMY and watches its own actions; the Block goes to whoever is wearing the applicant marker, which is
    // how a rule on the other side of the fight reaches the player.
    private static StatusData Hearing(string id, string name, int block) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = $"If this character takes a damaging action, you gain {block} Block.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<ActionResolvedTriggeredEffectContext>(
                new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                    new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(),
                    new CausalSequenceEffectNode<ActionResolvedTriggeredEffectContext>(
                    [
                        new ForEachTargetEffectNode<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.WithStatus(
                                CombatantTargetSelectors.AllCombatants,
                                new StatusDefinitionId(Keywords.ApplicantMarker)),
                            new ApplyStatusNode<ActionResolvedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(HearingOwed),
                                new ConstantExpression<ActionResolvedTriggeredEffectContext>(1))),
                        new RemoveStatusNode<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(id)),
                    ]))), nameof(TriggerEvent.ActionResolved)),
        ],
    };

    // The debt, paid after the holder's next draw so the Block is there when they act rather than swept
    // away by their own turn start.
    private static StatusData HearingOwedStatus(int block) => new()
    {
        Id = HearingOwed,
        NameKey = "Heard",
        DescriptionKey = $"You gain {block} Block at the start of your next turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(block)),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(HearingOwed)),
                ])), nameof(TriggerEvent.CardsDrawn)),
        ],
    };

    // ── Sealed Mantle ─────────────────────────────────────────────────────────────────────────────────────
    // "If at least one enemy attacks during this enemy turn and you take no unblocked Attack damage, gain 2
    // Ward Wax."
    //
    // Both halves are already counted for us: an enemy action that struck announces itself when it closes
    // (whether or not Block soaked it), and Ward Wax already counts what got THROUGH. The mark waits for the
    // round to end and reads both.
    public const string MantleAttacked = "sealed_mantle_attacked";

    private static StatusData Mantle()
    {
        var wearer = CombatantTargetSelectors.IterationTarget;

        var watch = new EffectProgram<ActionResolvedTriggeredEffectContext>(
            new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                new AndExpression<ActionResolvedTriggeredEffectContext>(
                    new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(),
                    Applicant<ActionResolvedTriggeredEffectContext>(CombatantTargetSelectors.Source, shouldBe: false)),
                new ForEachTargetEffectNode<ActionResolvedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(SealedMantle)),
                    new SetCombatantCounterNode<ActionResolvedTriggeredEffectContext>(
                        wearer, new CounterId(MantleAttacked),
                        new ConstantExpression<ActionResolvedTriggeredEffectContext>(1), relative: false))));

        var settle = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(SealedMantle)),
                new CausalSequenceEffectNode<RoundEndedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<RoundEndedTriggeredEffectContext>(
                        new AndExpression<RoundEndedTriggeredEffectContext>(
                            new ComparisonExpression<RoundEndedTriggeredEffectContext>(
                                new CombatantCounterExpression<RoundEndedTriggeredEffectContext>(
                                    wearer, new CounterId(MantleAttacked)),
                                ComparisonOperator.Greater, new ConstantExpression<RoundEndedTriggeredEffectContext>(0)),
                            new ComparisonExpression<RoundEndedTriggeredEffectContext>(
                                new CombatantCounterExpression<RoundEndedTriggeredEffectContext>(
                                    wearer, Keywords.StruckThisRoundCounter),
                                ComparisonOperator.Equal, new ConstantExpression<RoundEndedTriggeredEffectContext>(0))),
                        new ApplyStatusNode<RoundEndedTriggeredEffectContext>(
                            wearer, new StatusDefinitionId(Keywords.WardWax),
                            new ConstantExpression<RoundEndedTriggeredEffectContext>(2))),
                    new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                        wearer, new CounterId(MantleAttacked),
                        new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<RoundEndedTriggeredEffectContext>(wearer, new StatusDefinitionId(SealedMantle)),
                ])));

        return Rite(SealedMantle, "Sealed Mantle",
            "If an enemy attacks this turn and nothing gets through, gain 2 Ward Wax.",
            [
                Trigger(watch, nameof(TriggerEvent.ActionResolved), StatusTriggerScope.Anywhere),
                Trigger(settle, nameof(TriggerEvent.RoundEnded), StatusTriggerScope.Anywhere),
            ]);
    }

    // ── Sanctioned Charm ──────────────────────────────────────────────────────────────────────────────────
    // "The first time your Censure prevents a negative Status, the Censure used to prevent it is not consumed."
    //
    // Handed back rather than held: the refusal has already been paid for by the time anything can answer it,
    // so the Charm refunds a Censure and spends itself. It refunds ONE — the event does not say how many
    // stacks the refusal cost. See ADAPTATIONS.
    private static StatusData Charm() => new()
    {
        Id = SanctionedCharm,
        NameKey = "Sanctioned Charm",
        DescriptionKey = "The next Censure you spend refusing a status is handed back.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
                new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Censure),
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1)),
                    new RemoveStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(SanctionedCharm)),
                ])), nameof(TriggerEvent.StatusApplicationPrevented)),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(SanctionedCharm))),
                nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Rite(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers = triggers,
        };

    private static StatusTriggerData ClearLatch(CounterId latch) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, latch,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
            nameof(TriggerEvent.TurnStarted));

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
