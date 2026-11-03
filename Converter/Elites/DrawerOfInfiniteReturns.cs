using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Drawer of Infinite Returns (Act II elite, Returns) ─────────────────────────────────────────────────
//
// REWORKED in the content cut of 2026-10-29. Every turn the Drawer demands one card from your hand, and the card
// says so on its face. Play the demanded card and it works as always — and then it goes into the drawer: the
// drawer stands open, and an open drawer takes double damage until the end of your next turn. Keep it from the
// drawer instead and it costs you 6 health (2026-11-03; it was an Overdue). Every card it
// swallowed comes back to your hand when it dies.
//
// A shut drawer guards itself (10 Block each of its turns), so feeding it is how the fight is won quickly, and
// what it costs is the cards it keeps.
public static class DrawerOfInfiniteReturns
{
    public const string EnemyId = "drawer_of_infinite_returns";

    public const string TheDrawerId = "the_drawer";
    public const string DrawerOpenId = "drawer_open";
    public const string DemandedMark = "drawer_demand";
    public const string FedMark = "in_the_drawer";

    public const int OpenRounds = 2;      // the turn you feed it, and your next one
    public const int ShutBlock = 10;
    public const int DrawerHolds = 3;
    public const int RefusedDamage = 6;   // a demanded card kept (user, 2026-11-03: it was 1 Overdue)

    private static CounterId HeldCounter => new("drawer_holds");

    private static ICombatantTargetSelector Drawer { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(TheDrawerId)));

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheDrawer(),
        new StatusData
        {
            Id = DrawerOpenId,
            NameKey = "Drawer Open",
            DescriptionKey = "It holds a card you fed it: it takes double damage until the end of your next turn.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 200),
            ],
            Triggers = [],
        },
    ];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
        [TheDemand(), FeedingTheDrawer(), RefusingTheDrawer(), EverythingComesBack()];

    // A shut drawer guards itself at the start of its turn.
    private static StatusData TheDrawer()
    {
        var self = CombatantTargetSelectors.Source;
        var guard = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new NotExpression<TurnStartedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(DrawerOpenId))),
                new GainBlockNode<TurnStartedTriggeredEffectContext>(self, Const<TurnStartedTriggeredEffectContext>(ShutBlock))));

        return new StatusData
        {
            Id = TheDrawerId,
            NameKey = "The Drawer",
            DescriptionKey = "From your second turn it demands a card each turn. Play it and it is fed: the drawer opens and "
                + "takes double damage. Keep it: 1 Overdue. It holds three; a fourth pushes the oldest back out. It gives everything back when it dies.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    guard, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ],
        };
    }

    // After your draw, one card in hand is demanded — the first one, as every Act II mark reads "one card".
    private static EncounterTriggerData TheDemand()
    {
        var player = CombatantTargetSelectors.Source;
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        PlayersTurn<CardsDrawnTriggeredEffectContext>(),
                        // From the second round: the act files no Paperwork in a fight's first two rounds.
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                            ComparisonOperator.GreaterOrEqual, Const<CardsDrawnTriggeredEffectContext>(2))),
                    // Once a turn: no card already demanded.
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                            player, CardZone.Hand, new TagId(DemandedMark)),
                        ComparisonOperator.Equal, Const<CardsDrawnTriggeredEffectContext>(0))),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    player, CardZone.Hand,
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        player, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), new TagId(DemandedMark)),
                    takeFirst: 1)));
        return Encounter("CardsDrawn", program);
    }

    // The demanded card, played: it has done its work, and now it is the drawer's.
    private static EncounterTriggerData FeedingTheDrawer()
    {
        var player = CombatantTargetSelectors.Source;
        var card = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    PlayersTurn<CardPlayedTriggeredEffectContext>(),
                    new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(card, new TagId(DemandedMark))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(player, card, new TagId(DemandedMark), remove: true),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(player, card, new TagId(FedMark)),
                    new MoveCardToZoneNode<CardPlayedTriggeredEffectContext>(player, card, CardZone.ExhaustPile),
                    // The drawer holds three. A fourth pushes the oldest back out, into your discard pile —
                    // otherwise a long fight could swallow the whole deck and leave nothing to fight with.
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        player, HeldCounter, Const<CardPlayedTriggeredEffectContext>(1), relative: true),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, HeldCounter),
                            ComparisonOperator.Greater, Const<CardPlayedTriggeredEffectContext>(DrawerHolds)),
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                                player, CardZone.ExhaustPile,
                                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                                [
                                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                                        player, new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                                        new TagId(FedMark), remove: true),
                                    new MoveCardToZoneNode<CardPlayedTriggeredEffectContext>(
                                        player, new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                                        CardZone.DiscardPile),
                                ]),
                                markFilter: new TagId(FedMark), takeFirst: 1),
                            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                                player, HeldCounter, Const<CardPlayedTriggeredEffectContext>(-1), relative: true),
                        ])),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(Drawer, new StatusDefinitionId(DrawerOpenId)),
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        Drawer, new StatusDefinitionId(DrawerOpenId), Const<CardPlayedTriggeredEffectContext>(OpenRounds)),
                ])));
        return Encounter("CardPlayed", program);
    }

    // Your turn ends: a demanded card still in hand was refused (6 damage), and the drawer closes a round.
    private static EncounterTriggerData RefusingTheDrawer()
    {
        var player = CombatantTargetSelectors.Source;
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
                        player, CardZone.Hand,
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new MarkCardInstanceNode<TurnEndedTriggeredEffectContext>(
                                player, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                                new TagId(DemandedMark), remove: true),
                            new DealDamageNode<TurnEndedTriggeredEffectContext>(
                                player, Const<TurnEndedTriggeredEffectContext>(RefusedDamage), kind: DamageKind.DamageOverTime),
                        ]),
                        markFilter: new TagId(DemandedMark)),
                    new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                        Drawer, new StatusDefinitionId(DrawerOpenId), Const<TurnEndedTriggeredEffectContext>(-1)),
                ])));
        return Encounter("TurnEnded", program);
    }

    // When the Drawer falls, everything it swallowed comes back to your hand.
    private static EncounterTriggerData EverythingComesBack()
    {
        var player = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PassiveStatuses.ApplicantId));
        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(TheDrawerId)),
                new ForEachCardInZoneNode<CombatantDownedTriggeredEffectContext>(
                    player, CardZone.ExhaustPile,
                    new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CombatantDownedTriggeredEffectContext>(
                            player, new IteratedCardExpression<CombatantDownedTriggeredEffectContext>(),
                            new TagId(FedMark), remove: true),
                        new MoveCardToZoneNode<CombatantDownedTriggeredEffectContext>(
                            player, new IteratedCardExpression<CombatantDownedTriggeredEffectContext>(), CardZone.Hand),
                    ]),
                    markFilter: new TagId(FedMark))));
        return Encounter("Downed", program);
    }

    // The intents are authored in the source data; nothing here is code-only any more.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

    // ── idioms ─────────────────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, bool> PlayersTurn<TContext>() where TContext : class =>
        new AndExpression<TContext>(
            new TargetHasStatusExpression<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            new ComparisonExpression<TContext>(
                new CountTargetsExpression<TContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(TheDrawerId))),
                ComparisonOperator.Greater, Const<TContext>(0)));

    private static EncounterTriggerData Encounter<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static ConstantExpression<TContext> Const<TContext>(int value) where TContext : class => new(value);
}
