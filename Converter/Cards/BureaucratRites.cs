using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// The statuses behind the Bureaucrat's RITES — "a persistent combat effect that remains active and changes
// rules, engines or recurring behavior".
//
// A Rite is a card that puts a status on the player; the status carries the rule. That is also how a Rite
// watches the ENEMIES: a status trigger can be scoped to the whole fight, in which case the status stops
// being the event's subject and becomes only the rule's licence.
//
// "The first time each turn…" is a counter latch on the wearer, cleared by a turn-start trigger. A counter
// rather than a marker status, because a marker would have to be REMOVED again, and a status losing stacks is
// something the general pool's Blood Ink answers.
public static class BureaucratRites
{
    public const string BlackLedger = "black_ledger";
    public const string AshRegister = "ash_register";
    public const string SealDividend = "seal_dividend";
    public const string DubiousAuthority = "dubious_authority";
    public const string ClerksFamiliar = "clerks_familiar";
    public const string PendingMatters = "pending_matters";
    public const string LicensedDisposal = "licensed_disposal";
    public const string Continuance = "continuance";
    public const string ViolenceAllowance = "violence_allowance";
    public const string PresumptionOfError = "presumption_of_error";
    public const string CounterWard = "counter_ward";

    public static IReadOnlyList<StatusData> All() =>
    [
        Ledger(BlackLedger, "Black Ledger", 8),
        Ledger(BlackLedger + "+", "Black Ledger+", 6),

        OnceEachTurn(AshRegister, "Ash Register",
            "The first time each turn you Archive a card, draw 1 card.",
            watches: Keywords.Archived, scope: StatusTriggerScope.Bearer, draw: 1),
        OnceEachTurn(AshRegister + "+", "Ash Register+",
            "The first time each turn you Archive a card, draw 1 card.",
            watches: Keywords.Archived, scope: StatusTriggerScope.Bearer, draw: 1),

        // A Ratify happens on an ENEMY, so the rule watches the whole fight and rewards its wearer.
        OnceEachTurn(SealDividend, "Seal Dividend",
            "The first time each turn you Ratify an enemy, draw 1 card.",
            watches: Keywords.Ratified, scope: StatusTriggerScope.Anywhere, draw: 1),
        OnceEachTurn(SealDividend + "+", "Seal Dividend+",
            "The first time each turn you Ratify an enemy, draw 1 card.",
            watches: Keywords.Ratified, scope: StatusTriggerScope.Anywhere, draw: 1),

        OnceEachTurn(ClerksFamiliar, "Clerk's Familiar",
            "The first time each turn you create a Junk card, gain 4 Block.",
            watches: Keywords.JunkFiled, scope: StatusTriggerScope.Bearer, block: 4),
        OnceEachTurn(ClerksFamiliar + "+", "Clerk's Familiar+",
            "The first time each turn you create a Junk card, gain 5 Block.",
            watches: Keywords.JunkFiled, scope: StatusTriggerScope.Bearer, block: 5),

        OnceEachTurn(PendingMatters, "Pending Matters",
            "The first time each turn a Queued card resolves, gain 3 Block.",
            watches: Keywords.QueueResolved, scope: StatusTriggerScope.Bearer, block: 3),
        OnceEachTurn(PendingMatters + "+", "Pending Matters+",
            "The first time each turn a Queued card resolves, gain 4 Block.",
            watches: Keywords.QueueResolved, scope: StatusTriggerScope.Bearer, block: 4),

        Doubtful(DubiousAuthority, "Dubious Authority", 2),
        Doubtful(DubiousAuthority + "+", "Dubious Authority+", 3),

        Disposal(LicensedDisposal, "Licensed Disposal"),
        Disposal(LicensedDisposal + "+", "Licensed Disposal+"),

        Retention(Continuance, "Continuance", 8),
        Retention(Continuance + "+", "Continuance+", 12),

        Allowance(ViolenceAllowance, "Violence Allowance"),
        Allowance(ViolenceAllowance + "+", "Violence Allowance+"),
        AllowanceReadyStatus(),

        Presumption(),
        CounterWardStatus(),
    ];

    // ── Black Ledger ──────────────────────────────────────────────────────────────────────────────────────
    // "At the start of your turn, if any enemy has at least 8 Paperwork, draw 1 card."
    //
    // "Any enemy with at least N" cannot be asked of a selector — a status-filtered selector only knows
    // presence, not depth. So the rule walks the enemies that carry Paperwork and asks each one; the first
    // that is deep enough draws the card and shuts the gate, which is what makes it one card and not one per
    // enemy. The gate is opened again at the top of the same program, so it is per turn by construction.
    private static readonly CounterId LedgerDrawn = new("black_ledger_drawn");

    private static StatusData Ledger(string id, string name, int threshold)
    {
        var enemy = CombatantTargetSelectors.IterationTarget;

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Set<TurnStartedTriggeredEffectContext>(LedgerDrawn, 0),
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(Keywords.Paperwork)),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new AndExpression<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                    enemy, new StatusDefinitionId(Keywords.Paperwork)),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(threshold)),
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source, LedgerDrawn),
                                ComparisonOperator.Equal,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            Draw<TurnStartedTriggeredEffectContext>(1),
                            Set<TurnStartedTriggeredEffectContext>(LedgerDrawn, 1),
                        ]))),
            ]));

        return Rite(id, name,
            $"At the start of your turn, if any enemy has at least {threshold} Paperwork, draw 1 card.",
            [Trigger(program, nameof(TriggerEvent.TurnStarted))]);
    }

    // ── "the first time each turn <status> moves" ─────────────────────────────────────────────────────────
    // Archiving, Ratifying, filing Junk and resolving a queued card all announce themselves the same way: a
    // count on somebody goes UP. One shape answers all four; only what it watches, whose event it is, and
    // what it pays differ.
    private static StatusData OnceEachTurn(
        string id, string name, string description, string watches, StatusTriggerScope scope,
        int draw = 0, int block = 0)
    {
        var latch = new CounterId($"{id}_paid");

        // The count going up reads as StatusApplied the first time and StatusMerged after that, so both say
        // the same thing. The wearer is found by the marker, which also works when the event is somebody
        // else's — a Ratify lands on an enemy, and the card is drawn by whoever holds the Rite.
        IEffectNode<TContext> Body<TContext>() where TContext : class
        {
            var wearer = CombatantTargetSelectors.IterationTarget;
            var pay = new List<IEffectNode<TContext>>();
            if (draw > 0)
                pay.Add(new DrawCardsNode<TContext>(wearer, new ConstantExpression<TContext>(draw)));
            if (block > 0)
                pay.Add(new GainBlockNode<TContext>(wearer, new ConstantExpression<TContext>(block)));
            pay.Add(new SetCombatantCounterNode<TContext>(
                wearer, latch, new ConstantExpression<TContext>(1), relative: false));

            return new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                new ConditionalEffectNode<TContext>(
                    new AndExpression<TContext>(
                        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(watches)),
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(wearer, latch),
                            ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                    new CausalSequenceEffectNode<TContext>(pay)));
        }

        return Rite(id, name, description,
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                Body<StatusAppliedTriggeredEffectContext>()), nameof(TriggerEvent.StatusApplied), scope),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                Body<StatusMergedTriggeredEffectContext>()), nameof(TriggerEvent.StatusMerged), scope),
            ClearLatch(latch),
        ]);
    }

    // ── Dubious Authority ─────────────────────────────────────────────────────────────────────────────────
    // "Whenever Doubt is consumed after an enemy attacks, apply 2 Paperwork to that enemy."
    //
    // Watching the whole fight for Doubt leaving an enemy. The "after an enemy attacks" half is asked as
    // "did that enemy deal damage this turn?" — which is what spending Doubt means here, and what keeps the
    // rule out of the way of a card that simply removes Doubt (Formal Dissent). See ADAPTATIONS.
    private static StatusData Doubtful(string id, string name, int paperwork)
    {
        IEffectNode<TContext> Body<TContext>() where TContext : class =>
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Doubt)),
                    new ComparisonExpression<TContext>(
                        new DamageDealtThisTurnExpression<TContext>(CombatantTargetSelectors.Source),
                        ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Paperwork),
                    new ConstantExpression<TContext>(paperwork)));

        return Rite(id, name,
            $"Whenever Doubt is consumed after an enemy attacks, apply {paperwork} Paperwork to that enemy.",
        [
            Anywhere<StatusStacksChangedTriggeredEffectContext>(
                nameof(TriggerEvent.StatusStacksChanged), Body<StatusStacksChangedTriggeredEffectContext>()),
            // A last stack being spent is an expiry, not a stack change.
            Anywhere<StatusExpiredTriggeredEffectContext>(
                nameof(TriggerEvent.StatusExpired), ExpiredBody<StatusExpiredTriggeredEffectContext>(paperwork)),
        ]);
    }

    // The expiry case addresses the bearer through the EVENT TARGET: a status expiry reports no source.
    private static IEffectNode<TContext> ExpiredBody<TContext>(int paperwork) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Doubt)),
                new ComparisonExpression<TContext>(
                    new DamageDealtThisTurnExpression<TContext>(CombatantTargetSelectors.EventTarget),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
            new ApplyStatusNode<TContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Paperwork),
                new ConstantExpression<TContext>(paperwork)));

    // ── Licensed Disposal ─────────────────────────────────────────────────────────────────────────────────
    // "The first Junk card you draw each turn is automatically Archived; then draw 1 card."
    //
    // Read after the draw, when the hand already holds what arrived. It takes the first Junk in HAND rather
    // than strictly the first Junk DRAWN — a distinction only visible when Junk was already being held; see
    // ADAPTATIONS. Archiving is recorded, so the Archive Rites answer it exactly as they would a card's own.
    private static readonly CounterId DisposalUsed = new("licensed_disposal_used");

    private static StatusData Disposal(string id, string name)
    {
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, DisposalUsed),
                    ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            CardZone.ExhaustPile),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Archived),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        Draw<CardsDrawnTriggeredEffectContext>(1),
                        Set<CardsDrawnTriggeredEffectContext>(DisposalUsed, 1),
                    ]),
                    tagFilter: new TagId(CardAuthoring.JunkTag), takeFirst: 1)));

        return Rite(id, name,
            "The first Junk card you draw each turn is Archived; then draw 1 card.",
            [Trigger(program, nameof(TriggerEvent.CardsDrawn)), ClearLatch(DisposalUsed)]);
    }

    // ── Continuance ───────────────────────────────────────────────────────────────────────────────────────
    // "At the end of your turn, retain up to N Block."
    //
    // Two halves: the retain-block tag stops the wearer's Block being swept at its own turn start at all, and
    // a turn-start trigger trims whatever survived back to the ceiling. The trim runs before the sweep would
    // have, so what the player keeps is exactly min(Block, N).
    private static StatusData Retention(string id, string name, int ceiling)
    {
        var block = new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool);

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ModifyDefensivePoolNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool,
                // min(ceiling - block, 0): nothing when under the ceiling, the excess removed when over it.
                new MinExpression<TurnStartedTriggeredEffectContext>(
                    new SubtractExpression<TurnStartedTriggeredEffectContext>(
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(ceiling), block),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0))));

        return Rite(id, name, $"At the end of your turn, retain up to {ceiling} Block.",
            [Trigger(program, nameof(TriggerEvent.TurnStarted))],
            tags: [StandardCombatIds.RetainBlockTag.value]);
    }

    // ── Violence Allowance ────────────────────────────────────────────────────────────────────────────────
    // "The first Deed you play each turn costs 1 less Energy."
    //
    // Two statuses, because a passive modifier cannot be conditional — its PRESENCE is the condition. The
    // Rite is permanent and carries the bookkeeping; the allowance itself is a second status that holds the
    // discount, is taken away by the Deed that spends it, and is handed back at the start of the next turn.
    // The discount is narrowed to Deeds by the card tag, so nothing else is quietly cheapened while it waits.
    public const string AllowanceReady = "allowance_ready";

    private static StatusData Allowance(string id, string name)
    {
        var spend = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                    new TagId(CardAuthoring.DeedTag)),
                new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(AllowanceReady))));

        IEffectNode<TContext> Renew<TContext>() where TContext : class =>
            new ApplyStatusNode<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(AllowanceReady),
                new ConstantExpression<TContext>(1));

        return Rite(id, name, "The first Deed you play each turn costs 1 less Energy.",
        [
            Trigger(spend, nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                Renew<TurnStartedTriggeredEffectContext>()), nameof(TriggerEvent.TurnStarted)),
            // In force from the moment the Rite is played, not only from the next turn.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                Renew<CardsDrawnTriggeredEffectContext>()), nameof(TriggerEvent.CardsDrawn)),
        ]);
    }

    // The discount itself. Restricted to Deeds, so while it waits it prices nothing else.
    private static StatusData AllowanceReadyStatus() => new()
    {
        Id = AllowanceReady,
        NameKey = "Violence Allowed",
        DescriptionKey = "Your next Deed this turn costs 1 less Energy.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, -1,
                RestrictDamageKind: null, RestrictSourceCardTag: CardAuthoring.DeedTag),
        ],
    };

    // ── Presumption of Error ──────────────────────────────────────────────────────────────────────────────
    // "The next time that enemy consumes Doubt by attacking, apply 1 Doubt to it after the Attack resolves."
    // A mark ON THE ENEMY, so it is per-enemy, and spent the first time it answers.
    private static StatusData Presumption() => new()
    {
        Id = PresumptionOfError,
        NameKey = "Presumption of Error",
        DescriptionKey = "The next time this character spends Doubt attacking, it gains 1 Doubt.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<StatusExpiredTriggeredEffectContext>(
                ExpiredPresumption<StatusExpiredTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusExpired), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusStacksChangedTriggeredEffectContext>(
                ChangedPresumption<StatusStacksChangedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusStacksChanged)),
        ],
    };

    private static IEffectNode<TContext> ChangedPresumption<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Doubt)),
                new ComparisonExpression<TContext>(
                    new DamageDealtThisTurnExpression<TContext>(CombatantTargetSelectors.Source),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
            new CausalSequenceEffectNode<TContext>(
            [
                new RemoveStatusNode<TContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(PresumptionOfError)),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Doubt),
                    new ConstantExpression<TContext>(1)),
            ]));

    private static IEffectNode<TContext> ExpiredPresumption<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(Keywords.Doubt)),
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId(PresumptionOfError))),
                new ComparisonExpression<TContext>(
                    new DamageDealtThisTurnExpression<TContext>(CombatantTargetSelectors.EventTarget),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
            new CausalSequenceEffectNode<TContext>(
            [
                new RemoveStatusNode<TContext>(CombatantTargetSelectors.EventTarget, new StatusDefinitionId(PresumptionOfError)),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Doubt),
                    new ConstantExpression<TContext>(1)),
            ]));

    // ── Counter Ward's rider ──────────────────────────────────────────────────────────────────────────────
    // "The next card you Queue this turn costs 1 less Energy." The discount is not narrowed to Queue cards —
    // the cost pipeline cannot be told to read a card's tags — so it is the next card, full stop, and the
    // player spends it on what they meant to. See ADAPTATIONS. Spent by the card that uses it.
    private static StatusData CounterWardStatus() => new()
    {
        Id = CounterWard,
        NameKey = "Counter Ward",
        DescriptionKey = "Your next card this turn costs 1 less Energy.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, -1),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(CounterWard))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(CounterWard))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Rite(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers,
        IReadOnlyList<string>? tags = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            // Neutral: a Rite is the player's own doing, not something done to them, so nothing that cleanses
            // or refuses statuses should touch it.
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = tags ?? [],
            Triggers = triggers,
        };

    private static StatusTriggerData ClearLatch(CounterId latch) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            Set<TurnStartedTriggeredEffectContext>(latch, 0)), nameof(TriggerEvent.TurnStarted));

    private static StatusTriggerData Anywhere<TContext>(string trigger, IEffectNode<TContext> body)
        where TContext : class =>
        Trigger(new EffectProgram<TContext>(body), trigger, StatusTriggerScope.Anywhere);

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);

    private static DrawCardsNode<TContext> Draw<TContext>(int cards) where TContext : class =>
        new(CombatantTargetSelectors.Source, new ConstantExpression<TContext>(cards));

    private static SetCombatantCounterNode<TContext> Set<TContext>(CounterId counter, int value)
        where TContext : class =>
        new(CombatantTargetSelectors.Source, counter, new ConstantExpression<TContext>(value), relative: false);
}
