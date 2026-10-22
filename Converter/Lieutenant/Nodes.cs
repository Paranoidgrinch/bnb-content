using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Lieutenant;

// The engine's effect nodes and expressions under short names, for the Lieutenant's rules: an Order or a
// Challenge reads several counters and statuses at once, and spelled out in full every one of them would be a
// screen of generics. Nothing here is a rule of its own — each line is the engine node it names.
internal static class N<T> where T : class
{
    public static ICombatantTargetSelector Self => CombatantTargetSelectors.Source;
    public static ICombatantTargetSelector It => CombatantTargetSelectors.IterationTarget;
    public static ICombatantTargetSelector Event => CombatantTargetSelectors.EventTarget;

    public static IEffectNode<T> Seq(params IEffectNode<T>[] steps) => new CausalSequenceEffectNode<T>(steps);
    public static IEffectNode<T> All(params IEffectNode<T>[] steps) => new SequenceEffectNode<T>(steps);
    public static IEffectNode<T> Nothing => new SequenceEffectNode<T>([]);

    public static IEffectNode<T> If(ICombatExpression<T, bool> test, IEffectNode<T> then, IEffectNode<T>? otherwise = null) =>
        new ConditionalEffectNode<T>(test, then, otherwise);

    // The first case that holds, and only it — an if-else chain. ⚠ A conditional that fails with nothing to do
    // still costs the fight a queue cycle (it hands its completion back as a continuation), so a rule that asks
    // thirty questions of every status application asks them as ONE chain, which costs one, not thirty.
    public static IEffectNode<T> FirstOf(IEnumerable<(ICombatExpression<T, bool> When, IEffectNode<T> Then)> cases)
    {
        var list = cases.ToList();
        if (list.Count == 0)
            return Nothing;
        // Halved rather than chained, so the depth grows with the logarithm of the cases: the game document is
        // JSON, and its reader stops at 64 levels.
        if (list.Count > 3)
        {
            var left = list.Take(list.Count / 2).ToList();
            var right = list.Skip(list.Count / 2).ToList();
            return new ConditionalEffectNode<T>(AnyOf(left.Select(c => c.When).ToList()), FirstOf(left), FirstOf(right));
        }
        IEffectNode<T>? chain = null;
        foreach (var (when, then) in Enumerable.Reverse(list))
            chain = new ConditionalEffectNode<T>(when, then, chain);
        return chain!;
    }

    // Any of these, as a balanced tree of ors.
    public static ICombatExpression<T, bool> AnyOf(IReadOnlyList<ICombatExpression<T, bool>> tests) =>
        tests.Count == 1 ? tests[0]
            : Or(AnyOf([.. tests.Take(tests.Count / 2)]), AnyOf([.. tests.Skip(tests.Count / 2)]));

    public static IEffectNode<T> Each(ICombatantTargetSelector who, IEffectNode<T> body) =>
        new ForEachTargetEffectNode<T>(who, body);

    // ── values ───────────────────────────────────────────────────────────────────────────────────────────

    public static ICombatExpression<T, int> C(int value) => new ConstantExpression<T>(value);
    public static ICombatExpression<T, int> Stacks(ICombatantTargetSelector who, string status) =>
        new CombatantStatusStacksExpression<T>(who, new StatusDefinitionId(status));
    public static ICombatExpression<T, int> Counter(ICombatantTargetSelector who, string counter) =>
        new CombatantCounterExpression<T>(who, new CounterId(counter));
    public static ICombatExpression<T, int> Count(ICombatantTargetSelector who) => new CountTargetsExpression<T>(who);
    public static ICombatExpression<T, int> Round => new RoundNumberExpression<T>();
    public static ICombatExpression<T, int> Amount => new EventAmountExpression<T>();
    public static ICombatExpression<T, int> Energy(ICombatantTargetSelector who) =>
        new CombatantCurrentResourceExpression<T>(who, StandardCombatIds.EnergyResource);
    public static ICombatExpression<T, int> BlockOf(ICombatantTargetSelector who) =>
        new CombatantDefensivePoolExpression<T>(who, StandardCombatIds.BlockDefensivePool);
    public static ICombatExpression<T, int> Played(ICombatantTargetSelector who) => new CardsPlayedThisTurnExpression<T>(who);
    public static ICombatExpression<T, int> PlayedTagged(ICombatantTargetSelector who, string tag) =>
        new CardsPlayedThisTurnWithTagExpression<T>(who, new TagId(tag));
    public static ICombatExpression<T, int> Plus(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new AddExpression<T>(a, b);
    public static ICombatExpression<T, int> Minus(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new SubtractExpression<T>(a, b);
    public static ICombatExpression<T, int> Times(ICombatExpression<T, int> a, int factor) => new MultiplyExpression<T>(a, C(factor));
    public static ICombatExpression<T, int> Min(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new MinExpression<T>(a, b);
    public static ICombatExpression<T, int> Max(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new MaxExpression<T>(a, b);
    public static ICombatExpression<T, int> Half(ICombatExpression<T, int> a) => new DivideExpression<T>(a, C(2));

    // ── tests ────────────────────────────────────────────────────────────────────────────────────────────

    public static ICombatExpression<T, bool> Cmp(ICombatExpression<T, int> a, ComparisonOperator op, ICombatExpression<T, int> b) =>
        new ComparisonExpression<T>(a, op, b);
    public static ICombatExpression<T, bool> AtLeast(ICombatExpression<T, int> a, int b) => Cmp(a, ComparisonOperator.GreaterOrEqual, C(b));
    public static ICombatExpression<T, bool> Is(ICombatExpression<T, int> a, int b) => Cmp(a, ComparisonOperator.Equal, C(b));
    public static ICombatExpression<T, bool> Above(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => Cmp(a, ComparisonOperator.Greater, b);
    public static ICombatExpression<T, bool> And(ICombatExpression<T, bool> a, ICombatExpression<T, bool> b) => new AndExpression<T>(a, b);
    public static ICombatExpression<T, bool> Or(ICombatExpression<T, bool> a, ICombatExpression<T, bool> b) => new OrExpression<T>(a, b);
    public static ICombatExpression<T, bool> Not(ICombatExpression<T, bool> a) => new NotExpression<T>(a);
    public static ICombatExpression<T, bool> Wears(ICombatantTargetSelector who, string status) =>
        new TargetHasStatusExpression<T>(who, new StatusDefinitionId(status));
    public static ICombatExpression<T, bool> Alive(ICombatantTargetSelector who) => new TargetIsAliveExpression<T>(who);
    public static ICombatExpression<T, bool> CardTagged(string tag) => new TriggerEventSourceCardHasTagExpression<T>(new TagId(tag));
    public static ICombatExpression<T, bool> Announced(string rule) => new AnnouncedRuleIsExpression<T>(rule);
    public static ICombatExpression<T, bool> StatusIs(string status) => new TriggerEventStatusIsExpression<T>(new StatusDefinitionId(status));
    public static ICombatExpression<T, bool> IntendsAttack(ICombatantTargetSelector who) =>
        new TargetIntendsExpression<T>(who, nameof(IntentKind.Attack));
    public static ICombatExpression<T, bool> OncePerAction(string key) => new ClaimOnceThisActionExpression<T>(key);

    // "Not yet this round": a latch is a counter holding the round it was last spent in.
    public static ICombatExpression<T, bool> Fresh(ICombatantTargetSelector who, string latch) =>
        Cmp(Counter(who, latch), ComparisonOperator.NotEqual, Round);
    public static IEffectNode<T> Spend(ICombatantTargetSelector who, string latch) => Set(who, latch, Round);

    // ── effects ──────────────────────────────────────────────────────────────────────────────────────────

    public static IEffectNode<T> Apply(ICombatantTargetSelector who, string status, ICombatExpression<T, int> stacks) =>
        new ApplyStatusNode<T>(who, new StatusDefinitionId(status), stacks);
    public static IEffectNode<T> Apply(ICombatantTargetSelector who, string status, int stacks) => Apply(who, status, C(stacks));
    public static IEffectNode<T> Remove(ICombatantTargetSelector who, string status) =>
        new RemoveStatusNode<T>(who, new StatusDefinitionId(status));
    public static IEffectNode<T> Adjust(ICombatantTargetSelector who, string status, ICombatExpression<T, int> delta) =>
        new ModifyStatusStacksNode<T>(who, new StatusDefinitionId(status), delta);
    public static IEffectNode<T> Set(ICombatantTargetSelector who, string counter, ICombatExpression<T, int> value) =>
        new SetCombatantCounterNode<T>(who, new CounterId(counter), value, relative: false);
    public static IEffectNode<T> Set(ICombatantTargetSelector who, string counter, int value) => Set(who, counter, C(value));
    public static IEffectNode<T> Add(ICombatantTargetSelector who, string counter, ICombatExpression<T, int> delta) =>
        new SetCombatantCounterNode<T>(who, new CounterId(counter), delta, relative: true);
    public static IEffectNode<T> Block(ICombatantTargetSelector who, ICombatExpression<T, int> amount) => new GainBlockNode<T>(who, amount);
    public static IEffectNode<T> Block(ICombatantTargetSelector who, int amount) => Block(who, C(amount));
    public static IEffectNode<T> Draw(ICombatantTargetSelector who, int cards) => new DrawCardsNode<T>(who, C(cards));
    public static IEffectNode<T> GainEnergy(ICombatantTargetSelector who, int amount) =>
        new GainResourceNode<T>(who, StandardCombatIds.EnergyResource, C(amount));
    public static IEffectNode<T> Hit(ICombatantTargetSelector who, ICombatExpression<T, int> amount) => new DealDamageNode<T>(who, amount);
    // Damage that is not a Deed's: no Strength, no Doubt, nothing that shapes attacks (canon §2.1).
    public static IEffectNode<T> Wound(ICombatantTargetSelector who, ICombatExpression<T, int> amount) =>
        new DealDamageNode<T>(who, amount, kind: DamageKind.DamageOverTime);
    public static IEffectNode<T> Heal(ICombatantTargetSelector who, int amount) => new HealNode<T>(who, C(amount));
    public static IEffectNode<T> Announce(ICombatantTargetSelector who, string rule) => new AnnounceRuleNode<T>(who, rule);
    public static IEffectNode<T> Choose(IReadOnlyList<string> labels, params IEffectNode<T>[] options) =>
        new ChooseOptionsNode<T>(options, labels, purpose: labels[0]);
}

internal static class Statuses
{
    public static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger, StatusTriggerScope scope = StatusTriggerScope.Bearer)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            TheOpeningHand.For(program, trigger), CombatJson.CreateOptions<TContext>()), scope);

    // A draw trigger that means EVERY draw, not only the turn's opening hand (TheOpeningHand explains the default).
    public static StatusTriggerData OnEveryDraw(IEffectNode<CardsDrawnTriggeredEffectContext> body) =>
        new(nameof(TriggerEvent.CardsDrawn), JsonSerializer.SerializeToElement(
            new EffectProgram<CardsDrawnTriggeredEffectContext>(body), CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()));

    // "Until your next Deed has resolved": a Deed played while the status stands marks it spent, and the close
    // of that action takes it away. Not on the card-played event itself — a card's program can be heard mid-way
    // (each causal step lets the events of the step before it through), so a status read by the Deed would be
    // gone before the Deed reached it.
    public static IReadOnlyList<StatusTriggerData> UntilNextDeedResolves(string id)
    {
        var spent = id + "_spent";
        return
        [
            On(TriggerEvent.CardPlayed, N<CardPlayedTriggeredEffectContext>.If(
                N<CardPlayedTriggeredEffectContext>.CardTagged(Cards.CardAuthoring.DeedTag),
                N<CardPlayedTriggeredEffectContext>.Set(N<CardPlayedTriggeredEffectContext>.Self, spent, 1))),
            On(TriggerEvent.ActionResolved, N<ActionResolvedTriggeredEffectContext>.If(
                N<ActionResolvedTriggeredEffectContext>.Is(N<ActionResolvedTriggeredEffectContext>.Counter(N<ActionResolvedTriggeredEffectContext>.Self, spent), 1),
                N<ActionResolvedTriggeredEffectContext>.Seq(
                    N<ActionResolvedTriggeredEffectContext>.Set(N<ActionResolvedTriggeredEffectContext>.Self, spent, 0),
                    N<ActionResolvedTriggeredEffectContext>.Remove(N<ActionResolvedTriggeredEffectContext>.Self, id)))),
            On(TriggerEvent.TurnEnded, N<TurnEndedTriggeredEffectContext>.Seq(
                N<TurnEndedTriggeredEffectContext>.Set(N<TurnEndedTriggeredEffectContext>.Self, spent, 0),
                N<TurnEndedTriggeredEffectContext>.Remove(N<TurnEndedTriggeredEffectContext>.Self, id))),
        ];
    }

    public static StatusTriggerData On<TContext>(TriggerEvent trigger, IEffectNode<TContext> body,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        Trigger(new EffectProgram<TContext>(body), trigger.ToString(), scope);

    public static StatusData Status(
        string id, string name, StatusPolarity polarity, string description,
        IReadOnlyList<StatusTriggerData>? triggers = null,
        IReadOnlyList<PassiveModifierData>? passives = null,
        IReadOnlyList<string>? tags = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = tags ?? [],
            PassiveModifiers = passives ?? [],
            Triggers = triggers ?? [],
        };
}
