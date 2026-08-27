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
        Marker(HieraticMeasure, "Hieratic Measure"),
        Marker(HieraticMeasure + "+", "Hieratic Measure+"),
        // Read by Ward Wax.
        Marker(CandleCathedral, "Candle Cathedral"),
        Marker(CandleCathedral + "+", "Candle Cathedral+"),
        // Read by the engine's prohibition through Censure's own spec.
        Marker(AbsoluteInterdict, "Absolute Interdict"),
        Marker(AbsoluteInterdict + "+", "Absolute Interdict+"),
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
            var enemy = CombatantTargetSelectors.Source;
            var fivesNow = new DivideExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(enemy, new StatusDefinitionId(Keywords.Paperwork)),
                new ConstantExpression<TContext>(5));
            var credited = new CombatantCounterExpression<TContext>(enemy, FivesCrossed);

            return new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Paperwork)),
                    new ComparisonExpression<TContext>(fivesNow, ComparisonOperator.Greater, credited)),
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

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Marker(string id, string name) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = "A standing rule of this fight.",
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
