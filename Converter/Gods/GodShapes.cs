using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Gods;

// The shapes every god of Act V is written in (rework of 2026-11-05, ACT_V_REWORK_PLAN.md). Each god lives in a
// class of its own in this folder; ActFive is the registry that hands them to the rest of the converter.
//
// The idioms are Act IV's (ActFourBosses): every count is a stack on a status the player can read, a rule
// lives on the body it belongs to, and damage a god deals from its own turn is dealt from a rule on its OWN
// turn, so the player's Strength never swells it.
public static class GodShapes
{
    // Worn by a status that keeps its body at 1 HP while some other body stands (Nisaba's Indelible): a walker
    // reads it to know that hitting that body moves nothing.
    public const string CannotFallTag = "cannot_fall";

    // …and by a body the fight asks you to break before its countdown kills you (Nisaba's Final Line).
    public const string BreakFirstTag = "break_first";

    public static ConstantExpression<TContext> N<TContext>(int value) where TContext : class => new(value);

    public static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // The player: the one combatant every fight marks as the applicant.
    public static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId)));

    // "The body whose rule this is" — the living combatant carrying that rule.
    public static ICombatantTargetSelector Bearer(string ruleId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)));

    public static ICombatantTargetSelector Living(string status) =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(status));

    public static ICombatExpression<TContext, int> CountOf<TContext>(string status) where TContext : class =>
        new CountTargetsExpression<TContext>(Living(status));

    // ── effects ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEffectNode<TContext> Do<TContext>(params IEffectNode<TContext>[] steps) where TContext : class =>
        new CausalSequenceEffectNode<TContext>(steps);

    public static IEffectNode<TContext> If<TContext>(
        ICombatExpression<TContext, bool> condition, IEffectNode<TContext> then, IEffectNode<TContext>? @else = null)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(condition, then, @else: @else);

    public static IEffectNode<TContext> Nothing<TContext>() where TContext : class => new NoOpEffectNode<TContext>();

    public static IEffectNode<TContext> Mark<TContext>(ICombatantTargetSelector who, string status, int stacks = 1)
        where TContext : class =>
        new ApplyStatusNode<TContext>(who, new StatusDefinitionId(status), N<TContext>(stacks));

    public static IEffectNode<TContext> MarkBy<TContext>(ICombatantTargetSelector who, string status, ICombatExpression<TContext, int> stacks)
        where TContext : class =>
        new ApplyStatusNode<TContext>(who, new StatusDefinitionId(status), stacks);

    public static IEffectNode<TContext> Unmark<TContext>(ICombatantTargetSelector who, string status) where TContext : class =>
        new RemoveStatusNode<TContext>(who, new StatusDefinitionId(status));

    // A stack count set to exactly `value` (0 removes the status).
    public static IEffectNode<TContext> SetStacks<TContext>(ICombatantTargetSelector who, string status, ICombatExpression<TContext, int> value)
        where TContext : class =>
        Do(Unmark<TContext>(who, status),
            If(Above(value, 0), MarkBy(who, status, value)));

    public static IEffectNode<TContext> AddStacks<TContext>(ICombatantTargetSelector who, string status, int delta) where TContext : class =>
        delta >= 0
            ? Mark<TContext>(who, status, delta)
            : If(Above(StacksOn<TContext>(who, status), -delta),
                new ModifyStatusStacksNode<TContext>(who, new StatusDefinitionId(status), N<TContext>(delta)),
                Unmark<TContext>(who, status));

    // A countdown on a body: one fewer, and at the last one `then` and the count gone.
    public static IEffectNode<TContext> CountDown<TContext>(ICombatantTargetSelector who, string status, IEffectNode<TContext> then)
        where TContext : class =>
        If(new ComparisonExpression<TContext>(StacksOn<TContext>(who, status), ComparisonOperator.LessOrEqual, N<TContext>(1)),
            Do(Unmark<TContext>(who, status), then),
            new ModifyStatusStacksNode<TContext>(who, new StatusDefinitionId(status), N<TContext>(-1)));

    public static IEffectNode<TContext> Counter<TContext>(ICombatantTargetSelector who, CounterId counter, ICombatExpression<TContext, int> value)
        where TContext : class =>
        new SetCombatantCounterNode<TContext>(who, counter, value, relative: false);

    public static IEffectNode<TContext> Bump<TContext>(ICombatantTargetSelector who, CounterId counter, int by = 1) where TContext : class =>
        new SetCombatantCounterNode<TContext>(who, counter, N<TContext>(by), relative: true);

    public static ICombatExpression<TContext, int> CounterOf<TContext>(ICombatantTargetSelector who, CounterId counter) where TContext : class =>
        new CombatantCounterExpression<TContext>(who, counter);

    public static IEffectNode<TContext> KillAll<TContext>(string status) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Living(status),
            new SetHealthNode<TContext>(CombatantTargetSelectors.IterationTarget, N<TContext>(0)));

    // Health lost, not damage: Block does not apply and nothing modifies it. Kills at 0.
    public static IEffectNode<TContext> LoseHealth<TContext>(ICombatantTargetSelector who, ICombatExpression<TContext, int> amount)
        where TContext : class =>
        new SetHealthNode<TContext>(who, new MaxExpression<TContext>(N<TContext>(0),
            new SubtractExpression<TContext>(new CombatantCurrentHealthExpression<TContext>(who), amount)));

    public static IEffectNode<TContext> HitPlayer<TContext>(ICombatExpression<TContext, int> amount, bool ignoresBlock = false)
        where TContext : class =>
        new DealDamageNode<TContext>(Applicant, amount, ignoresBlock: ignoresBlock);

    public static IEffectNode<TContext> HitPlayer<TContext>(int amount, bool ignoresBlock = false) where TContext : class =>
        HitPlayer(N<TContext>(amount), ignoresBlock);

    public static IEffectNode<TContext> Guard<TContext>(ICombatantTargetSelector who, int block) where TContext : class =>
        new GainBlockNode<TContext>(who, N<TContext>(block));

    public static IEffectNode<TContext> Afflict<TContext>(string status, int stacks) where TContext : class =>
        new ApplyStatusNode<TContext>(Applicant, new StatusDefinitionId(status), N<TContext>(stacks));

    public static IEffectNode<TContext> Give<TContext>(string cardId, CardZone zone, int count = 1) where TContext : class =>
        new CreateCardInstanceNode<TContext>(Applicant, new CardDefinitionId(cardId), zone, N<TContext>(count));

    public static IEffectNode<TContext> ClearBlock<TContext>(ICombatantTargetSelector who) where TContext : class =>
        new ModifyDefensivePoolNode<TContext>(who, StandardCombatIds.BlockDefensivePool,
            new NegateExpression<TContext>(BlockOf<TContext>(who)));

    // ── expressions ──────────────────────────────────────────────────────────────────────────────────────

    public static ICombatExpression<TContext, int> StacksOn<TContext>(ICombatantTargetSelector who, string status) where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(status));

    public static ICombatExpression<TContext, bool> Wears<TContext>(ICombatantTargetSelector who, string status) where TContext : class =>
        new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(status));

    public static ICombatExpression<TContext, bool> Not<TContext>(ICombatExpression<TContext, bool> term) where TContext : class =>
        new NotExpression<TContext>(term);

    public static ICombatExpression<TContext, bool> AllOf<TContext>(params ICombatExpression<TContext, bool>[] terms) where TContext : class =>
        terms.Aggregate((a, b) => new AndExpression<TContext>(a, b));

    public static ICombatExpression<TContext, bool> AnyOf<TContext>(params ICombatExpression<TContext, bool>[] terms) where TContext : class =>
        terms.Aggregate((a, b) => new OrExpression<TContext>(a, b));

    public static ICombatExpression<TContext, bool> Eq<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Equal, N<TContext>(b));

    public static ICombatExpression<TContext, bool> Above<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Greater, N<TContext>(b));

    public static ICombatExpression<TContext, bool> Ge<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.GreaterOrEqual, N<TContext>(b));

    public static ICombatExpression<TContext, bool> Lt<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Less, N<TContext>(b));

    public static ICombatExpression<TContext, bool> Less<TContext>(ICombatExpression<TContext, int> a, ICombatExpression<TContext, int> b)
        where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Less, b);

    public static ICombatExpression<TContext, int> Plus<TContext>(ICombatExpression<TContext, int> a, ICombatExpression<TContext, int> b)
        where TContext : class => new AddExpression<TContext>(a, b);

    public static ICombatExpression<TContext, int> Minus<TContext>(ICombatExpression<TContext, int> a, ICombatExpression<TContext, int> b)
        where TContext : class => new SubtractExpression<TContext>(a, b);

    public static ICombatExpression<TContext, int> Times<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new MultiplyExpression<TContext>(a, N<TContext>(b));

    public static ICombatExpression<TContext, int> Over<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new DivideExpression<TContext>(a, N<TContext>(b));

    public static ICombatExpression<TContext, int> Min<TContext>(ICombatExpression<TContext, int> a, ICombatExpression<TContext, int> b)
        where TContext : class => new MinExpression<TContext>(a, b);

    public static ICombatExpression<TContext, int> Max<TContext>(ICombatExpression<TContext, int> a, ICombatExpression<TContext, int> b)
        where TContext : class => new MaxExpression<TContext>(a, b);

    public static ICombatExpression<TContext, int> HealthOf<TContext>(ICombatantTargetSelector who) where TContext : class =>
        new CombatantCurrentHealthExpression<TContext>(who);

    public static ICombatExpression<TContext, int> MaxHealthOf<TContext>(ICombatantTargetSelector who) where TContext : class =>
        new CombatantMaxHealthExpression<TContext>(who);

    public static ICombatExpression<TContext, int> BlockOf<TContext>(ICombatantTargetSelector who) where TContext : class =>
        new CombatantDefensivePoolExpression<TContext>(who, StandardCombatIds.BlockDefensivePool);

    public static ICombatExpression<TContext, int> Round<TContext>() where TContext : class => new RoundNumberExpression<TContext>();

    // At or below `percent` of its health.
    public static ICombatExpression<TContext, bool> AtOrBelow<TContext>(ICombatantTargetSelector body, int percent) where TContext : class =>
        new ComparisonExpression<TContext>(
            new MultiplyExpression<TContext>(HealthOf<TContext>(body), N<TContext>(100)),
            ComparisonOperator.LessOrEqual,
            new MultiplyExpression<TContext>(MaxHealthOf<TContext>(body), N<TContext>(percent)));

    public static ICombatExpression<TContext, bool> IsPlayersTurn<TContext>() where TContext : class =>
        Wears<TContext>(CombatantTargetSelectors.Source, PassiveStatuses.ApplicantId);

    public static ICombatExpression<TContext, bool> Standing<TContext>(ICombatantTargetSelector who) where TContext : class =>
        Above(HealthOf<TContext>(who), 0);

    // A turn of the player's that the god is alive for.
    public static ICombatExpression<TContext, bool> PlayersTurnAgainst<TContext>(ICombatantTargetSelector god) where TContext : class =>
        AllOf(IsPlayersTurn<TContext>(), Standing<TContext>(god));

    // The size of a single hit: what reached health and what the Block took.
    public static ICombatExpression<DamageReceivedTriggeredEffectContext, int> HitSize =>
        new AddExpression<DamageReceivedTriggeredEffectContext>(
            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
            new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>());

    // ── the act's two shared words ───────────────────────────────────────────────────────────────────────
    //
    // The plan asks for Weak and Frail (Inanna's Golden Ring, Enlil's plague). This game had neither, so Act V
    // brings them: one stack comes off at the end of each of your turns.
    public const string WeakId = "weak";
    public const string FrailId = "frail";

    public static IEnumerable<StatusData> Words()
    {
        var self = CombatantTargetSelectors.Source;
        yield return Face(WeakId, "Weak", "Your attacks deal 25 % less. One stack wears off at the end of your turn.",
            StatusPolarity.Debuff, stacks: true,
            modifiers: [Modifier(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, 75, DamageKind.Direct)],
            triggers: [Own("TurnEnded", AddStacks<TurnEndedTriggeredEffectContext>(self, WeakId, -1))]);
        yield return Face(FrailId, "Frail", "You gain 25 % less Block. One stack wears off at the end of your turn.",
            StatusPolarity.Debuff, stacks: true,
            modifiers: [Modifier(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.ScalePercent, 75)],
            triggers: [Own("TurnEnded", AddStacks<TurnEndedTriggeredEffectContext>(self, FrailId, -1))]);
    }

    // ── statuses ─────────────────────────────────────────────────────────────────────────────────────────

    public static StatusTriggerData On<TContext>(
        string trigger, IEffectNode<TContext> body, StatusTriggerScope scope = StatusTriggerScope.Anywhere)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            new EffectProgram<TContext>(body), CombatJson.CreateOptions<TContext>()), scope);

    public static StatusTriggerData Own<TContext>(string trigger, IEffectNode<TContext> body) where TContext : class =>
        On(trigger, body, StatusTriggerScope.Bearer);

    public static StatusData Face(
        string id, string name, string description, StatusPolarity polarity = StatusPolarity.Neutral,
        bool stacks = false, IReadOnlyList<PassiveModifierData>? modifiers = null,
        IReadOnlyList<StatusTriggerData>? triggers = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = stacks,
            Tags = [],
            PassiveModifiers = modifiers ?? [],
            Triggers = triggers ?? [],
        };

    public static PassiveModifierData Modifier(PassiveModifierPipeline pipeline, PassiveModifierOperation operation, int magnitude,
        DamageKind? kind = null) =>
        new(pipeline, operation, magnitude, RestrictDamageKind: kind);

    public static IReadOnlyList<EncounterTriggerData> Downed(IEffectNode<CombatantDownedTriggeredEffectContext> program) =>
    [
        new EncounterTriggerData("Downed", JsonSerializer.SerializeToElement(
            new EffectProgram<CombatantDownedTriggeredEffectContext>(program),
            CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>())),
    ];

    // A card a god puts in your hand: it costs what it says, and it is gone when played (Exhaust) unless
    // `returns`, and gone at the end of the turn unless `retain`.
    public static CardData GodCard(string id, string name, string text, IEffectNode<CardPlayContext> body, int cost = 0,
        bool retain = false, bool ethereal = true, bool returns = false, params string[] tags) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = cost > 0 ? [new ResourceCost(StandardCombatIds.EnergyResource, cost)] : [],
            Tags = [new TagId("form"), new TagId(id), .. tags.Select(t => new TagId(t))],
            Program = new EffectProgram<CardPlayContext>(body),
            PlayedCardDestinationZone = returns ? CardZone.Hand : CardZone.ExhaustPile,
            RetainInHandOnTurnEnd = retain,
            TurnEndHandDestinationZone = retain ? CardZone.Hand : ethereal ? CardZone.ExhaustPile : CardZone.DiscardPile,
        };
}
