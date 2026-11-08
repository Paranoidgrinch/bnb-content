using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// The engine's nodes under short names, for the enemy rules of Acts I and II that were reworked on 2026-11-08
// (CONTENT_REVIEW.md, E1-E9). Each line is the engine node it names; nothing here is a rule of its own.
internal static class E<T> where T : class
{
    public static ICombatantTargetSelector Self => CombatantTargetSelectors.Source;
    public static ICombatantTargetSelector It => CombatantTargetSelectors.IterationTarget;
    public static ICombatantTargetSelector Event => CombatantTargetSelectors.EventTarget;

    // Every living body wearing a marker, wherever the program runs from.
    public static ICombatantTargetSelector Wearers(string marker) =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(marker));

    public static IEffectNode<T> Seq(params IEffectNode<T>[] steps) => new CausalSequenceEffectNode<T>(steps);
    public static IEffectNode<T> If(ICombatExpression<T, bool> test, IEffectNode<T> then, IEffectNode<T>? otherwise = null) =>
        new ConditionalEffectNode<T>(test, then, otherwise);
    public static IEffectNode<T> Each(ICombatantTargetSelector who, IEffectNode<T> body) =>
        new ForEachTargetEffectNode<T>(who, body);

    // ── values ───────────────────────────────────────────────────────────────────────────────────────────
    public static ICombatExpression<T, int> C(int value) => new ConstantExpression<T>(value);
    public static ICombatExpression<T, int> Stacks(ICombatantTargetSelector who, string status) =>
        new CombatantStatusStacksExpression<T>(who, new StatusDefinitionId(status));
    public static ICombatExpression<T, int> Counter(ICombatantTargetSelector who, CounterId counter) =>
        new CombatantCounterExpression<T>(who, counter);
    public static ICombatExpression<T, int> Energy(ICombatantTargetSelector who) =>
        new CombatantCurrentResourceExpression<T>(who, StandardCombatIds.EnergyResource);
    public static ICombatExpression<T, int> CountOf(ICombatantTargetSelector who) => new CountTargetsExpression<T>(who);
    public static ICombatExpression<T, int> InZone(ICombatantTargetSelector who, CardZone zone, string? mark = null) =>
        new CombatantZoneCardCountExpression<T>(who, zone, mark: mark is null ? null : new TagId(mark));
    public static ICombatExpression<T, int> Plus(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new AddExpression<T>(a, b);
    public static ICombatExpression<T, int> Minus(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new SubtractExpression<T>(a, b);
    public static ICombatExpression<T, int> Times(ICombatExpression<T, int> a, int b) => new MultiplyExpression<T>(a, C(b));
    public static ICombatExpression<T, int> Min(ICombatExpression<T, int> a, ICombatExpression<T, int> b) => new MinExpression<T>(a, b);

    // ── cards ────────────────────────────────────────────────────────────────────────────────────────────
    public static ICardInstanceExpression<T> Played => new TriggerEventCardInstanceExpression<T>();
    public static ICardInstanceExpression<T> Iterated => new IteratedCardExpression<T>();
    public static ICombatExpression<T, int> CostOf(ICardInstanceExpression<T> card) =>
        new CardInstanceBaseCostExpression<T>(card, StandardCombatIds.EnergyResource);
    public static ICombatExpression<T, int> DeltaOf(ICardInstanceExpression<T> card) =>
        new CardInstanceMarkCounterExpression<T>(card, StandardCombatIds.CardCostDeltaCounter);
    public static ICombatExpression<T, bool> Marked(ICardInstanceExpression<T> card, string mark) =>
        new CardInstanceHasMarkExpression<T>(card, new TagId(mark));
    public static ICombatExpression<T, bool> Tagged(ICardInstanceExpression<T> card, string tag) =>
        new CardInstanceHasTagExpression<T>(card, new TagId(tag));
    public static IEffectNode<T> Mark(ICardInstanceExpression<T> card, string mark, bool remove = false) =>
        new MarkCardInstanceNode<T>(Self, card, new TagId(mark), remove);
    public static IEffectNode<T> Reprice(ICardInstanceExpression<T> card, ICombatExpression<T, int> delta) =>
        new SetCardInstanceMarkCounterNode<T>(Self, card, StandardCombatIds.CardCostDeltaCounter, delta, relative: true);
    public static IEffectNode<T> ForMarked(CardZone zone, string mark, IEffectNode<T> body) =>
        new ForEachCardInZoneNode<T>(Self, zone, body, markFilter: new TagId(mark));
    public static IEffectNode<T> Give(string cardId) =>
        new CreateCardInstanceNode<T>(Self, new CardDefinitionId(cardId), CardZone.Hand, C(1));

    // ── tests ────────────────────────────────────────────────────────────────────────────────────────────
    public static ICombatExpression<T, bool> Is(ICombatExpression<T, int> left, ComparisonOperator op, int right) =>
        new ComparisonExpression<T>(left, op, C(right));
    public static ICombatExpression<T, bool> Is(
        ICombatExpression<T, int> left, ComparisonOperator op, ICombatExpression<T, int> right) =>
        new ComparisonExpression<T>(left, op, right);
    public static ICombatExpression<T, bool> And(params ICombatExpression<T, bool>[] terms) =>
        terms.Skip(1).Aggregate(terms[0], (all, term) => new AndExpression<T>(all, term));
    public static ICombatExpression<T, bool> Not(ICombatExpression<T, bool> term) => new NotExpression<T>(term);
    public static ICombatExpression<T, bool> Wears(ICombatantTargetSelector who, string status) =>
        new TargetHasStatusExpression<T>(who, new StatusDefinitionId(status));
    public static ICombatExpression<T, bool> Present(string marker) => Is(CountOf(Wearers(marker)), ComparisonOperator.Greater, 0);
    // The acting combatant is the player: every fight marks the hero as the applicant.
    public static ICombatExpression<T, bool> Applicant =>
        Is(Stacks(Self, PassiveStatuses.ApplicantId), ComparisonOperator.Greater, 0);

    // ── effects ──────────────────────────────────────────────────────────────────────────────────────────
    public static IEffectNode<T> Apply(ICombatantTargetSelector who, string status, int stacks = 1) =>
        new ApplyStatusNode<T>(who, new StatusDefinitionId(status), C(stacks));
    public static IEffectNode<T> Apply(ICombatantTargetSelector who, string status, ICombatExpression<T, int> stacks) =>
        new ApplyStatusNode<T>(who, new StatusDefinitionId(status), stacks);
    public static IEffectNode<T> Remove(ICombatantTargetSelector who, string status) =>
        new RemoveStatusNode<T>(who, new StatusDefinitionId(status));
    public static IEffectNode<T> AddStacks(ICombatantTargetSelector who, string status, ICombatExpression<T, int> delta) =>
        new ModifyStatusStacksNode<T>(who, new StatusDefinitionId(status), delta);
    public static IEffectNode<T> Set(ICombatantTargetSelector who, CounterId counter, ICombatExpression<T, int> value) =>
        new SetCombatantCounterNode<T>(who, counter, value, relative: false);
    public static IEffectNode<T> Add(ICombatantTargetSelector who, CounterId counter, ICombatExpression<T, int> value) =>
        new SetCombatantCounterNode<T>(who, counter, value, relative: true);
    public static IEffectNode<T> Damage(ICombatantTargetSelector who, ICombatExpression<T, int> amount) =>
        new DealDamageNode<T>(who, amount);
    public static IEffectNode<T> Block(ICombatantTargetSelector who, ICombatExpression<T, int> amount) =>
        new GainBlockNode<T>(who, amount);
}

// Status and trigger shells shared by the reworked enemies.
internal static class EnemyRules
{
    public static StatusData Rule(string id, string name, string description,
        IReadOnlyList<StatusTriggerData>? triggers = null, StatusPolarity polarity = StatusPolarity.Neutral,
        bool stacks = false, IReadOnlyList<PassiveModifierData>? modifiers = null) => new()
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

    public static StatusTriggerData On<TContext>(string trigger, IEffectNode<TContext> root,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            TheOpeningHand.For(new EffectProgram<TContext>(root), trigger), CombatJson.CreateOptions<TContext>()), scope);

    // An encounter trigger. One hung on the draw means the turn's opening hand (TheOpeningHand).
    public static EncounterTriggerData Encounter<TContext>(string trigger, IEffectNode<TContext> root)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            TheOpeningHand.For(new EffectProgram<TContext>(root), trigger), CombatJson.CreateOptions<TContext>()));

    // A card a fight lays in the player's hand: free unless priced, used once, gone at the turn's end.
    public static CardData GivenCard(string id, string name, string text, IEffectNode<CardPlayContext> program, int cost = 0) =>
        new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = cost > 0 ? [new ResourceCost(StandardCombatIds.EnergyResource, cost)] : [],
            Tags = [new TagId("form")],
            Program = new EffectProgram<CardPlayContext>(program),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
}
