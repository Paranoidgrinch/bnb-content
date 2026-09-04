using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// Act IV's Rites, from both pools. Five of the six change what an existing keyword DOES, so they are markers
// the keyword itself looks for — the same shape as Red Ink Doctrine inside the Paperwork tick. Only the
// Processional Calendar acts on its own.
public static class ActIVRites
{
    public const string TempleTally = "temple_tally";
    public const string ProcessionalCalendar = "processional_calendar";
    public const string HieraticMeasure = "hieratic_measure";
    public const string CandleCathedral = "candle_cathedral";
    public const string AbsoluteInterdict = "absolute_interdict";

    // The charge the Interdict lays. It is not the Rite — the Rite is a rule of the FIGHT and sits on
    // whoever played it, while this is laid on every combatant the rule applies to, which the master says is
    // each of them independently.
    public const string InterdictCharge = "interdict_charge";

    // Temple Tally remembers how many fives each enemy has already crossed, so a multiple is only ever
    // crossed once.
    public static CounterId FivesCrossed => new("temple_tally_fives");

    public static IReadOnlyList<StatusData> All() =>
    [
        Tally(TempleTally, "Temple Tally"),
        Tally(TempleTally + "+", "Temple Tally+"),
        Calendar(ProcessionalCalendar, "Processional Calendar"),
        Calendar(ProcessionalCalendar + "+", "Processional Calendar+"),

        // Read by the Seal→Ratify conversion (CardAuthoring.Ratify).
        Marker(HieraticMeasure, "Hieratic Measure",
            "Ratifying an enemy calls in its Paperwork on the spot, then takes 3 of it away."),
        Marker(HieraticMeasure + "+", "Hieratic Measure+",
            "Ratifying an enemy calls in its Paperwork on the spot, then takes 5 of it away."),
        // Read by Ward Wax.
        Marker(CandleCathedral, "Candle Cathedral",
            "Ward Wax pays half again, rounded up, and no longer decays faster for a hit that got through."),
        Marker(CandleCathedral + "+", "Candle Cathedral+",
            "Ward Wax pays half again, rounded up, and no longer decays faster for a hit that got through."),
        Interdict(AbsoluteInterdict, "Absolute Interdict"),
        Interdict(AbsoluteInterdict + "+", "Absolute Interdict+"),
        InterdictChargeStatus(),
    ];

    // ── Temple Tally ──────────────────────────────────────────────────────────────────────────────────────
    // "Whenever an enemy reaches a new multiple of 5 Paperwork for the first time this combat, apply 1 Seal
    // to it for each new multiple crossed."
    //
    // Watches Paperwork landing anywhere and compares how many fives the enemy is now worth against how many
    // it has been credited for. The difference is the Seal owed, and the credit is written back — so a pile
    // that shrinks and grows again crosses nothing twice, which is what "for the first time" means.
    private static StatusData Tally(string id, string name)
    {
        IEffectNode<TContext> Body<TContext>() where TContext : class
        {
            // ⚠ EventTarget, not Source: in a status-application event `Source` is whoever APPLIED it, and a
            // tally reading its own player's Paperwork counts nothing for the whole run. What "an enemy
            // reaches a new multiple" is about is the combatant the sheets landed ON — and it has to be an
            // enemy, so the applicant marker is what rules the player's own pile out.
            var enemy = CombatantTargetSelectors.EventTarget;
            var fivesNow = new DivideExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(enemy, new StatusDefinitionId(Keywords.Paperwork)),
                new ConstantExpression<TContext>(5));
            var credited = new CombatantCounterExpression<TContext>(enemy, FivesCrossed);

            return new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            enemy, new StatusDefinitionId(PassiveStatuses.ApplicantId))),
                    new AndExpression<TContext>(
                        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Paperwork)),
                        new ComparisonExpression<TContext>(fivesNow, ComparisonOperator.Greater, credited))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ApplyStatusNode<TContext>(enemy, new StatusDefinitionId(Keywords.Seal),
                        new SubtractExpression<TContext>(fivesNow, credited)),
                    new SetCombatantCounterNode<TContext>(enemy, FivesCrossed, fivesNow, relative: false),
                ]));
        }

        return Rite(id, name, "Every fifth Paperwork an enemy accumulates seals it.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                    Body<StatusAppliedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                    Body<StatusMergedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
        ]);
    }

    // ── Processional Calendar ─────────────────────────────────────────────────────────────────────────────
    // "At the end of your turn, if you have at least 2 Queued cards, resolve your oldest Queued card."
    private static StatusData Calendar(string id, string name) => Rite(id, name,
        "At the end of your turn, a backlog of two or more resolves its oldest card early.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.QueuePile),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                    new ResolveQueuedCardsNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // ── Absolute Interdict ────────────────────────────────────────────────────────────────────────────────
    // "The first time each turn Censure on a combatant would prevent Status stacks, 1 Censure prevents the
    // entire Status application instead, regardless of stack count, and only 1 Censure is consumed. This
    // applies independently to you and to each enemy."
    //
    // ⚠ This is the one Rite that could NOT be composed. It changes what Censure's own prohibition DOES, and
    // a prohibition is engine machinery read off the status definition — no program can reach inside it. What
    // it needed from the engine was two small, general things (both bought here, both proved in Core's
    // `PreventionPriorityTests`): a prohibition that refuses the WHOLE application for one stack — the charge
    // shape this genre has always had, which an absurdly large StacksPerStack only approximates — and a
    // PRIORITY, because the interceptor otherwise lets the oldest eligible prohibition answer, and a charge
    // laid beside a standing Censure would never once be the one to speak.
    //
    // So: the Rite lays a one-stack CHARGE on every combatant that is carrying Censure, at the top of that
    // combatant's own turn — which is what "the first time each turn, independently for each of them" comes
    // to — and when the charge is the thing that refused, one Censure is spent for it.
    private static StatusData Interdict(string id, string name) => Rite(id, name,
        "The first refusal each turn is total: 1 Censure turns away the whole application, however many "
        + "stacks it carried. This holds for you and for each enemy separately.",
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // Yesterday's charge goes first: a combatant that has run out of Censure must not keep a
                    // free refusal standing.
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(InterdictCharge)),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Censure)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(InterdictCharge),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                ])),
                nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),

            // "…and only 1 Censure is consumed." The charge pays itself; the Censure is spent here, and only
            // when the CHARGE was the thing that refused — somebody else's ward turning the same status away
            // must not cost the bearer a Censure.
            //
            // ⚠ `Source`, NOT `EventTarget`. A refusal is the one event family where the two read backwards
            // from the guess: `source` is the combatant that REFUSED — the one wearing the prohibition, and
            // the one whose Censure this is — while `eventTarget` is whoever was trying to apply the status.
            // Written the other way round this spends the ATTACKER's Censure, which it does not have, and
            // the rule is silently free.
            Trigger(new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventPreventerIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(InterdictCharge)),
                    new ModifyStatusStacksNode<StatusApplicationBlockedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Censure),
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(-1)))),
                nameof(TriggerEvent.StatusApplicationPrevented), StatusTriggerScope.Anywhere),
        ]);

    // The charge itself: neutral, so nothing that counts buffs counts it and no second prohibition eats it.
    private static StatusData InterdictChargeStatus() => new()
    {
        Id = InterdictCharge,
        NameKey = "Interdict",
        DescriptionKey =
            "The next unwanted status applied to this character is turned away entirely, however many stacks "
            + "it carried, and 1 Censure is spent doing it. One a turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Prevention = new StatusPreventionData(
            StatusPreventionScope.UnwantedByBearer, Priority: 10, RefusesWholeApplication: true),
    };

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Marker(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
    };

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

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
