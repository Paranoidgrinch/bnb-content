using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── The Drawer of Infinite Returns (Act II elite) ─────────────────────────────────────────────────────────
//
// Open a drawer and there is another drawer inside it. The Drawer never keeps anything — it returns it from
// somewhere deeper, and each return is worth more than the last. You may file one card away; it comes back
// cheaper next turn, and if you let it go past you it goes deeper still and comes back cheaper again. What it
// costs is time, and the Drawer counts that time as Depth Pressure.
//
// Refusing to play along is a real option: with nothing filed the Drawer simply guards itself, and you fight
// through 14 extra Block a turn.
public static class DrawerOfInfiniteReturns
{
    public const string EnemyId = "drawer_of_infinite_returns";

    public const string TheDrawerId = "the_drawer";
    public const string DrawerRulesId = "nested_return_rules";
    public const string DepthPressureId = "depth_pressure";
    public const string DrawerDelinquencyId = "drawer_delinquency";
    public const string NestedMark = "nested_card";

    // All of Nested Return's state, kept on the player: which depth the card is at (0 = the drawer is empty)
    // and how many player turns it still has to wait down there.
    private static readonly CounterId DepthCounter = new("nested_depth");
    private static readonly CounterId WaitCounter = new("nested_wait");

    private const int MaxDepth = 3;
    private const int MaxPressure = 3;
    private const int ClosedDrawerBlock = 14;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Drawers =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheDrawerId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheDrawer(),
        DepthPressure(),
        Rules(),
        ActTwo.Delinquency(DrawerDelinquencyId, "Returned to Sender",
            "The Drawer collects what it is owed."),
    ];

    // ── 11.2 Closed Drawer ────────────────────────────────────────────────────────────────────────────────
    //
    // "If no card is currently inside Nested Return, Drawer gains 14 Block at the beginning of its turn."
    // An engagement incentive, not an immunity — the Drawer reads the player's own depth counter to know
    // whether its drawer is empty.
    private static StatusData TheDrawer()
    {
        var guard = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Opponent, DepthCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new GainBlockNode<TurnStartedTriggeredEffectContext>(
                    Self, new ConstantExpression<TurnStartedTriggeredEffectContext>(ClosedDrawerBlock))));

        return Rule(TheDrawerId, "The Drawer",
            "An empty drawer is a closed drawer: it guards itself while it holds nothing of yours.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    guard, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ]);
    }

    private static StatusData DepthPressure() => new()
    {
        Id = DepthPressureId,
        NameKey = "Depth Pressure",
        DescriptionKey = "How long the Drawer has been holding something of yours. At 3 it slams shut.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── 11.3–11.6 Nested Return ───────────────────────────────────────────────────────────────────────────
    //
    // Three moments, all on the player, because it is the player's card and the player's turn that the whole
    // mechanism is about:
    //   after the normal draw — return what is due, or offer to file something;
    //   at the turn's end     — anything that came back and was not played goes deeper;
    //   on play               — the nesting ends, and from Depth 2 on it pays a card.
    private static StatusData Rules()
    {
        var afterDraw = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Nesting<CardsDrawnTriggeredEffectContext>(),
                // Something is down there: either it is still waiting, or it is due back.
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        Count<CardsDrawnTriggeredEffectContext>(WaitCounter),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Self, WaitCounter,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
                    @else: ReturnTheCard()),
                @else: Offer()));

        var atTurnEnd = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    Nesting<TurnEndedTriggeredEffectContext>(),
                    // It only goes deeper if it CAME BACK and was left unplayed. "Came back" is read as "not
                    // in the drawer" rather than "in hand": by the time a turn-end program reaches its
                    // card-touching nodes the hand has already been discarded, so the card is in the discard
                    // pile — and a card that was PLAYED has had its mark taken off, so the mark alone is
                    // enough to tell the two apart.
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                            Self, CardZone.BanishedPile, mark: new TagId(NestedMark)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                GoDeeper()));

        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    new TagId(NestedMark)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    // "Draw 1 after the card is played" — from Depth 2 on, and read before the depth is
                    // cleared, because clearing it is what ends the nesting.
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            Count<CardPlayedTriggeredEffectContext>(DepthCounter),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(2)),
                        new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                            Self, new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Self, new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(NestedMark), remove: true),
                    Set<CardPlayedTriggeredEffectContext>(DepthCounter, 0),
                    Set<CardPlayedTriggeredEffectContext>(WaitCounter, 0),
                ])));

        return Rule(DrawerRulesId, "Nested Return",
            "One card at a time may be filed away. It comes back cheaper the deeper it went — and the Drawer "
            + "counts every turn it holds it.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    afterDraw, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    atTurnEnd, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    onPlay, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    // 11.3: "the player MAY voluntarily choose one non-Junk card in hand and file it away. No Energy cost."
    // Voluntary, so it is a prompt with a real refusal — and no prompt at all when there is nothing worth
    // filing.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Offer() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new SubtractExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.Hand),
                    new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                        Self, CardZone.Hand, tag: new TagId(CardAuthoring.JunkTag))),
                ComparisonOperator.Greater,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
            new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                [FileItAway(), new NoOpEffectNode<CardsDrawnTriggeredEffectContext>()],
                ["file a card away in the Drawer", "keep your hand"],
                count: 1, purpose: "the Drawer stands open"));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> FileItAway() =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self,
                new ChosenCardInZoneExpression<CardsDrawnTriggeredEffectContext>(
                    CardZone.Hand, "file a card away"),
                new TagId(NestedMark)),
            // The card leaves the normal zones entirely — Banished is the one place nothing else reaches
            // into, which is what "temporarily leaves combat zones" has to mean for the return to be the
            // Drawer's alone.
            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, Nested<CardsDrawnTriggeredEffectContext>(CardZone.Hand), CardZone.BanishedPile),
            Set<CardsDrawnTriggeredEffectContext>(DepthCounter, 1),
            Set<CardsDrawnTriggeredEffectContext>(WaitCounter, 0),
        ]);

    // 11.4–11.6: it comes back cheaper the deeper it went. At Depth 3 it comes back free.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> ReturnTheCard()
    {
        var card = Nested<CardsDrawnTriggeredEffectContext>(CardZone.BanishedPile);

        return new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(Self, card, CardZone.Hand),
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    Count<CardsDrawnTriggeredEffectContext>(DepthCounter),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(MaxDepth)),
                // "Cost becomes 0 for that turn" — the whole printed cost taken off, not one off it.
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    Self, Nested<CardsDrawnTriggeredEffectContext>(CardZone.Hand),
                    StandardCombatIds.CardCostDeltaCounter,
                    new NegateExpression<CardsDrawnTriggeredEffectContext>(
                        new CardInstanceBaseCostExpression<CardsDrawnTriggeredEffectContext>(
                            Nested<CardsDrawnTriggeredEffectContext>(CardZone.Hand),
                            StandardCombatIds.EnergyResource)),
                    relative: false),
                @else: new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    Self, Nested<CardsDrawnTriggeredEffectContext>(CardZone.Hand),
                    StandardCombatIds.CardCostDeltaCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: false)),
        ]);
    }

    // The turn ended and it is still in your hand: down it goes. Depth Pressure counts entering Depth 2 and
    // every Depth-3 return you let pass — not the step from 2 to 3, which the design leaves free.
    private static IEffectNode<TurnEndedTriggeredEffectContext> GoDeeper()
    {
        var depth = Count<TurnEndedTriggeredEffectContext>(DepthCounter);

        return new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new OrExpression<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        depth, ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        depth, ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(MaxDepth))),
                Pressure<TurnEndedTriggeredEffectContext>()),
            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                Self, DepthCounter,
                new MinExpression<TurnEndedTriggeredEffectContext>(
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(MaxDepth),
                    new AddExpression<TurnEndedTriggeredEffectContext>(
                        depth, new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                relative: false),
            // A deeper drawer takes a full player turn to come back out of.
            Set<TurnEndedTriggeredEffectContext>(WaitCounter, 1),
            // Wherever the card ended up when the hand was put down — discard, or still in hand if something
            // retained it — the discount lapses and the drawer takes it back.
            .. new[] { CardZone.DiscardPile, CardZone.Hand }.SelectMany(zone =>
                new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new SetCardInstanceMarkCounterNode<TurnEndedTriggeredEffectContext>(
                        Self, Nested<TurnEndedTriggeredEffectContext>(zone),
                        StandardCombatIds.CardCostDeltaCounter,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new MoveCardToZoneNode<TurnEndedTriggeredEffectContext>(
                        Self, Nested<TurnEndedTriggeredEffectContext>(zone), CardZone.BanishedPile),
                }),
        ]);
    }

    private static IEffectNode<TContext> Pressure<TContext>() where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Drawers,
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(DepthPressureId)),
                    ComparisonOperator.Less, new ConstantExpression<TContext>(MaxPressure)),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(DepthPressureId),
                    new ConstantExpression<TContext>(1))));

    // ── 11.8 Intents ──────────────────────────────────────────────────────────────────────────────────────
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "open_another_drawer" => Normal(Damage(13)),
        "mahogany_runner" => Normal(Damage(17)),
        "index_the_contents" => Normal(new CausalSequenceEffectNode<EnemyActionContext>(
            [new GainBlockNode<EnemyActionContext>(Self, Const(20)), MisfileOne()])),
        "return_to_sender" => Normal(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(11),
            new ApplyStatusNode<EnemyActionContext>(
                Opponent, new StatusDefinitionId(ActTwo.OverdueId), Const(1)),
        ])),
        "inner_compartment" => Normal(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(Self, Const(12)),
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCounterExpression<EnemyActionContext>(Opponent, DepthCounter),
                    ComparisonOperator.Greater, Const(0)),
                Pressure<EnemyActionContext>()),
        ])),
        _ => null,
    };

    // Signature — Drawer Slams Shut: at Depth Pressure 3 whatever it was going to do becomes 24 damage and a
    // misfiling, and the pressure is spent.
    private static EffectProgram<EnemyActionContext> Normal(IEffectNode<EnemyActionContext> body) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    Self, new StatusDefinitionId(DepthPressureId)),
                ComparisonOperator.GreaterOrEqual, Const(MaxPressure)),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(24),
                MisfileOne(),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(DepthPressureId)),
            ]),
            @else: body));

    private static IEffectNode<EnemyActionContext> MisfileOne() =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(),
                new TagId(ActTwo.MisfiledMark)),
            takeFirst: 1);

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // ── shared shapes ─────────────────────────────────────────────────────────────────────────────────────

    private static ICardInstanceExpression<TContext> Nested<TContext>(CardZone zone) where TContext : class =>
        new FirstMarkedCardInOwnerZoneExpression<TContext>(
            CombatantTargetSelectors.Source, zone, new TagId(NestedMark));

    private static ICombatExpression<TContext, int> Count<TContext>(CounterId counter) where TContext : class =>
        new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter);

    private static ICombatExpression<TContext, bool> Nesting<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            Count<TContext>(DepthCounter), ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Set<TContext>(CounterId counter, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, counter, new ConstantExpression<TContext>(value), relative: false);

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
