using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stage 2 — The Gate of Counted Names. Where Stage 1 taught the measure, Stage 2 teaches the
// REGISTER: what being written down is worth, and to whom.
//
// The stage is built so the player cannot learn only half of Inscribed. Three bodies read it three ways:
//
//   the Uncounted Pilgrim reads it as a mere STATE — while you are in no register it is hard to hold to
//   account, and the moment you are inscribed it becomes legible and takes ordinary damage. Its own petition
//   is what registers you, so it makes itself hittable by asking you to be counted;
//   the Cobra of the Entry Mark reads it as the AMPLIFIER, and needs not a line of code to do it: it marks
//   you, then poisons you, and the register makes the venom land larger all by itself;
//   the Name-Eating Baboon reads the amplification itself — it watches the register actually magnify
//   somebody else's affliction and chews that into a forged authorization of its own.
//
// The stage is therefore also where the player learns that Inscribed is SPENDABLE: keep it and the Pilgrim
// stays legible but the Cobra's venom doubles down; spend it on a blessing of your own and the venom lands
// plain — but the Pilgrim goes back to being hard to hit. There is no right answer, which is the point.
public static partial class ActFour
{
    public const string PilgrimEnemyId = "uncounted_pilgrim";
    public const string CobraEnemyId = "cobra_of_the_entry_mark";
    public const string BaboonEnemyId = "name_eating_baboon";

    // The Pilgrim's two faces: the rule that keeps count, and the state the count produces.
    public const string NoNumberId = "no_number_in_the_register";
    public const string UncountedId = "uncounted";

    // The Baboon's rule, its resource, and the forgery it spends the resource on.
    public const string ChewedCredentialsId = "chewed_credentials";
    public const string StolenNameId = "stolen_name";
    public const string ForgedEntryId = "forged_entry";

    // How many stolen names buy one forged authorization (master §Stage 2: maximum 2).
    public const int StolenNamesPerForgery = 2;

    // "The first time each round" — a latch on the Baboon, cleared when the round turns.
    public static CounterId NameStolenThisRound => new("name_stolen_this_round");

    // ── the Uncounted Pilgrim ─────────────────────────────────────────────────────────────────────────────

    // What the register does to a body nobody has registered: while the player carries no Inscribed the
    // Pilgrim is Uncounted and hard to hold to account; the moment the player is in the register it becomes
    // legible and takes ordinary damage.
    //
    // The reduction is a passive on a VISIBLE marker rather than a condition inside the Pilgrim's intents,
    // for two reasons: the player has to be able to see the state they are being asked to change, and the
    // Pilgrim's telegraph must not lie — its shelter blocks what its telegraph says it blocks, in both states.
    public static StatusData Uncounted() => new()
    {
        Id = UncountedId,
        NameKey = "Uncounted",
        DescriptionKey =
            "In no register, and so hard to hold to account: this character takes 30% less attack damage. "
            + "While you are Inscribed it is Counted instead, and loses this.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 70),
        ],
        Triggers = [],
    };

    // The count itself. It answers every movement of the player's register — gained, merged, spent, gone —
    // and each time asks the one question the state depends on: is the player in the register at all?
    //
    // Spending is why all four events are watched rather than just the gain. The register is consumed by
    // amplifying, so a player who lets it magnify something drops back out of it mid-turn, and the Pilgrim
    // must go back to being Uncounted at that moment rather than at the next turn boundary — otherwise the
    // player would be attacking a body whose displayed state is a turn out of date.
    public static StatusData NoNumberInTheRegister() => new()
    {
        Id = NoNumberId,
        NameKey = "No Number in the Register",
        DescriptionKey =
            "This body is in no register. While you carry no Inscribed it is Uncounted; while you are "
            + "Inscribed it is Counted.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(Recount<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied),
                StatusTriggerScope.Anywhere),
            Trigger(Recount<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged),
                StatusTriggerScope.Anywhere),
            Trigger(Recount<StatusStacksChangedTriggeredEffectContext>(), nameof(TriggerEvent.StatusStacksChanged),
                StatusTriggerScope.Anywhere),
            Trigger(Recount<StatusRemovedTriggeredEffectContext>(), nameof(TriggerEvent.StatusRemoved),
                StatusTriggerScope.Anywhere),
            // The register running out is reported as an EXPIRY, not as a removal or a stack change — and
            // running out is the commonest way a player leaves the register, since the last stack goes by
            // being spent. A count that did not watch this one would show the Pilgrim as Counted for the
            // rest of the fight.
            Trigger(Recount<StatusExpiredTriggeredEffectContext>(), nameof(TriggerEvent.StatusExpired),
                StatusTriggerScope.Anywhere),
            // …and at every turn start from nothing at all, which is what settles the OPENING state: a
            // player who walks in already inscribed raises no status event for the count to hear. It is the
            // TURN and not the round, because a fight's first round starts before its bodies are dressed —
            // at that moment nothing wears this rule, and a rule nobody wears does not fire.
            Trigger(Recount<TurnStartedTriggeredEffectContext>(gated: false), nameof(TriggerEvent.TurnStarted),
                StatusTriggerScope.Anywhere),
        ],
    };

    // Read the register and set the state to match. Idempotent: it is safe to ask twice, which matters
    // because one application can raise two of the events it answers.
    //
    // `gated` is whether the program should first ask that the event was about the register at all. Every
    // status event does; a round turning has no status to ask about and recounts unconditionally.
    private static EffectProgram<TContext> Recount<TContext>(bool gated = true) where TContext : class
    {
        var pilgrim = Bearer<TContext>(NoNumberId);

        IEffectNode<TContext> count =
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            Applicant, new StatusDefinitionId(InscribedId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    // In the register: the Pilgrim is Counted, and takes what it is given.
                    new RemoveStatusNode<TContext>(pilgrim, new StatusDefinitionId(UncountedId)),
                    // Out of it: Uncounted again — but only once, or a marker would pile up.
                    new ConditionalEffectNode<TContext>(
                        new NotExpression<TContext>(
                            new TargetHasStatusExpression<TContext>(
                                pilgrim, new StatusDefinitionId(UncountedId))),
                        new ApplyStatusNode<TContext>(
                            pilgrim, new StatusDefinitionId(UncountedId), new ConstantExpression<TContext>(1))));

        return new EffectProgram<TContext>(
            gated
                ? new ConditionalEffectNode<TContext>(
                    // Only the register's own movements are worth a recount.
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(InscribedId)), count)
                : count);
    }

    // ── the Name-Eating Baboon ────────────────────────────────────────────────────────────────────────────

    // The names it has chewed. Visible, because the player is meant to see the forgery coming and decide
    // whether to stop feeding it — either by killing the Baboon or by spending the register themselves.
    public static StatusData StolenName() => new()
    {
        Id = StolenNameId,
        NameKey = "Stolen Name",
        DescriptionKey =
            "A name chewed off a tablet. At 2, the Baboon forges an authorization for the next affliction "
            + "and the names are spent.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The forgery: the Baboon's own amplifier, and the reason the engine's amplification is a spec on a
    // status rather than a rule inside the register. It enlarges the next NEGATIVE application only, which
    // is what distinguishes a forged authorization from being properly written into the register.
    public static StatusData ForgedEntry() => new()
    {
        Id = ForgedEntryId,
        NameKey = "Forged Entry",
        DescriptionKey =
            "A forged authorization rides on your file: the next negative status applied to you lands with "
            + "1 more stack, and the forgery is spent.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        Amplification = new StatusAmplificationData(
            StatusAmplificationScope.Debuffs, AddStacks: 1, StacksSpent: 1),
    };

    // What the Baboon is actually for: it does not read the register, it reads the register WORKING. The
    // first time each round Inscribed actually makes somebody else's affliction larger, that magnification is
    // what gets chewed into a name.
    //
    // Three gates, and each is a rule from the master rather than a convenience:
    //   the enlarged thing was NEGATIVE — the register spent on the player's own blessing feeds nothing;
    //   the enlarger was the register and not the Baboon's own forgery — §3.4's rule that a copy may never
    //   feed the copier, which is the whole reason the amplification event names what paid for it;
    //   the applier was somebody else — a Baboon that fed on its own Doubt would need no gate at all.
    public static StatusData ChewedCredentials() => new()
    {
        Id = ChewedCredentialsId,
        NameKey = "Chewed Credentials",
        DescriptionKey =
            "The first time each round the register makes another party's affliction larger, this Baboon "
            + "steals a name. At 2 stolen names it forges an authorization for the next affliction.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(ChewTheName(), nameof(TriggerEvent.StatusApplicationAmplified), StatusTriggerScope.Anywhere),
            Trigger(ClearTheLatch(), nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext> ChewTheName()
    {
        var baboon = Bearer<StatusApplicationAmplifiedTriggeredEffectContext>(ChewedCredentialsId);

        // "source" here is the combatant the enlarged status landed on, and "eventTarget" is whoever applied
        // it — so this asks: it happened to the player, and the applier was not a Baboon.
        var onThePlayer = new TargetHasStatusExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));

        var byAnotherParty = new NotExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
            new TargetHasStatusExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(ChewedCredentialsId)));

        var wasAnAffliction = new TriggerEventStatusPolarityIsExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
            StatusPolarity.Debuff);

        var byTheRegister = new TriggerEventAmplifierIsExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
            new StatusDefinitionId(InscribedId));

        var firstThisRound = new ComparisonExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
            new CombatantCounterExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                baboon, NameStolenThisRound),
            ComparisonOperator.Equal,
            new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(0));

        var chew = new CausalSequenceEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
        [
            new SetCombatantCounterNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                baboon, NameStolenThisRound,
                new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1), relative: false),

            new ApplyStatusNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                baboon, new StatusDefinitionId(StolenNameId),
                new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1),
                sourceSelector: baboon),

            // …and the second name is spent as soon as it is stolen: the forgery goes onto the file at once,
            // so the player sees what is coming rather than being told afterwards.
            new ConditionalEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                new ComparisonExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                        baboon, new StatusDefinitionId(StolenNameId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(StolenNamesPerForgery)),
                new CausalSequenceEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(ForgedEntryId),
                        new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1),
                        sourceSelector: baboon),

                    new RemoveStatusNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                        baboon, new StatusDefinitionId(StolenNameId)),
                ])),
        ]);

        return new EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                    new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(onThePlayer, wasAnAffliction),
                    new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                        new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(byTheRegister, byAnotherParty),
                        firstThisRound)),
                chew));
    }

    // A round turning is nobody's own event, so the rule finds every Baboon on the field itself.
    private static EffectProgram<RoundStartedTriggeredEffectContext> ClearTheLatch() =>
        new(new ForEachTargetEffectNode<RoundStartedTriggeredEffectContext>(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ChewedCredentialsId)),
            new SetCombatantCounterNode<RoundStartedTriggeredEffectContext>(
                CombatantTargetSelectors.IterationTarget, NameStolenThisRound,
                new ConstantExpression<RoundStartedTriggeredEffectContext>(0), relative: false)));

    // ── addressing the parties ────────────────────────────────────────────────────────────────────────────

    // "The body whose rule this is" — the living combatant carrying that rule. FirstTarget because a scalar
    // read needs one combatant; two bodies never carry the same rule in this stage.
    private static ICombatantTargetSelector Bearer<TContext>(string ruleId) where TContext : class =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)));
}
