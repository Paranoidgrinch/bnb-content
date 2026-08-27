using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Rolling Stacks Colossus (Act II elite) ────────────────────────────────────────────────────────────
//
// Shelving walls on stone rollers. Every misfiling the Archive actually skips narrows the aisles by one —
// Compression — and at three the next thing the Colossus does is Shelf Collapse. The counterplay is the
// replacement card the skip hands you: it is not compensation, it is a temporary path through the shelves,
// and walking it (playing that exact card) pushes the walls back.
//
// Two of its rules live outside this file because they belong to moments the act owns: Compression is counted
// where a misfiling is skipped (ActTwo.TakeBack), and the Open Aisle rules ride on the player.
public static class RollingStacksColossus
{
    public const string EnemyId = "rolling_stacks_colossus";

    // The marker the Colossus carries: its presence is what turns the act's misfiling rule into a compression.
    public const string TheAislesNarrowId = "the_aisles_narrow";
    public const string CompressionId = "compression";
    public const string OpenAisleMark = "open_aisle";
    // "The last valid card instance played during the previous player turn": a card the player's own rule
    // re-points at with every play, so the Colossus can name it a turn later.
    public const string LastPlayedMark = "last_played";
    // The player-side half of Open Aisle: it walks the path, so the rule about walking it lives on the player.
    public const string OpenAisleRulesId = "open_aisle_rules";

    private const int MaxCompression = 3;
    private const int MaxPassageBlock = 27;

    // "Maximum once per player turn" for the Compression refund, and the Ladder's pending tax.
    private static CounterId AisleWalkedCounter => new("open_aisle_walked");
    private static CounterId LadderDisplacedCounter => new("ladder_displaced");

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    // Every Colossus on the field, seen from the PLAYER's side (its rules run in player-side contexts).
    private static ICombatantTargetSelector Colossi =>
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheAislesNarrowId));

    public static IEnumerable<StatusData> Statuses() => [TheAislesNarrow(), Compression(), OpenAisleRules()];

    // ── 6.2 Compression / 6.3 Open Aisle ──────────────────────────────────────────────────────────────────
    //
    // Called from ActTwo.TakeBack at the one moment the design names: a Misfiled card ACTUALLY skipped during
    // draw. Three things happen, all of them only when a Colossus is standing:
    //   the aisles narrow by one (to a ceiling of three);
    //   the replacement card is marked Open Aisle — the path the skip opened;
    //   and if the Ladder was displaced this turn, that first replacement costs an Energy more.
    //
    // ADAPTATION: "Status/Junk replacement cards do not receive Open Aisle" is not filtered — the mark is a
    // path, and the rule that spends it only pays out when the card is PLAYED, which is the thing an
    // unplayable card cannot do.
    public static IEffectNode<CardsDrawnTriggeredEffectContext> OnMisfilingSkipped(
        ICardInstanceExpression<CardsDrawnTriggeredEffectContext> replacement) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new CountTargetsExpression<CardsDrawnTriggeredEffectContext>(Colossi),
                ComparisonOperator.Greater,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Colossi,
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(CompressionId)),
                            ComparisonOperator.Less,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(MaxCompression)),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(CompressionId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, replacement, new TagId(OpenAisleMark)),
                // Displace the Ladder taxes the FIRST such replacement and then stops — the counter is the
                // "maximum one card" in the design, spent as it is used.
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, LadderDisplacedCounter),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, replacement,
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: true),
                        new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, LadderDisplacedCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                    ])),
            ]));

    private static StatusData TheAislesNarrow() =>
        Rule(TheAislesNarrowId, "The Aisles Narrow",
            "While the Colossus stands, every skipped misfiling compresses the Archive.", []);

    private static StatusData Compression() => new()
    {
        Id = CompressionId,
        NameKey = "Compression",
        DescriptionKey = "How far the shelves have closed in. At 3 the next thing the Colossus does is a collapse.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── The player's half of Open Aisle ───────────────────────────────────────────────────────────────────
    //
    // Two moments. At the player's turn start (before the turn's draw, which happens after turn-start
    // triggers) last turn's paths are swept away and the once-per-turn refund is re-armed. When a card
    // carrying the mark is PLAYED, the walls give by one.
    private static StatusData OpenAisleRules()
    {
        var sweep = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, AisleWalkedCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                .. new[] { CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile }
                    .Select(Unmark),
            ]));

        // The refund is read IMMEDIATELY — the played card is still in hand at the first instant of a
        // CardPlayed trigger, and the mark travels with the instance either way.
        var walk = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(OpenAisleMark)),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, AisleWalkedCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, AisleWalkedCounter,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false),
                    new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                            new StatusDefinitionId(TheAislesNarrowId)),
                        new ModifyStatusStacksNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(CompressionId),
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(-1))),
                ])));

        // "The last valid card instance played": each play clears the pointer and re-plants it. Read
        // immediately, while the card is still in hand — the mark travels with the instance wherever it goes
        // next, which is exactly what makes it findable a turn later.
        var remember = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                .. new[] { CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile }
                    .Select(ForgetLastPlayed),
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    new TagId(LastPlayedMark)),
            ]));

        return Rule(OpenAisleRulesId, "Open Aisle",
            "A replacement card drawn because of a skipped misfiling opens a path. Play it and the shelves "
            + "give by one — once per turn.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    sweep, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    walk, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    remember, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    private static IEffectNode<CardPlayedTriggeredEffectContext> ForgetLastPlayed(CardZone zone) =>
        new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                new TagId(LastPlayedMark), remove: true),
            markFilter: new TagId(LastPlayedMark));

    private static IEffectNode<TurnStartedTriggeredEffectContext> Unmark(CardZone zone) =>
        new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, zone,
            new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<TurnStartedTriggeredEffectContext>(),
                new TagId(OpenAisleMark), remove: true),
            markFilter: new TagId(OpenAisleMark));

    // ── 6.4 Intents ───────────────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: "At Compression 3 the next eligible normal intent becomes Shelf Collapse" is written into
    // every normal intent rather than into the intent order, because the engine rotates a fixed list. Each
    // normal intent asks the same question first — am I the next one, and are the aisles closed? — so
    // whichever comes up IS the collapse. What cannot follow is the telegraph: the intent label is fixed at
    // authoring time, so the player sees the ordinary intent's name on the turn it becomes a collapse. The
    // Compression counter is visible the whole time, which is the warning the design actually trades on.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "misfile_an_entire_section" => Normal(Misfile(2)),
        "roll_across_the_aisle" => Normal(new CausalSequenceEffectNode<EnemyActionContext>(
            [Damage(15), MisfileTheLastPlayed(), ])),
        "close_the_remaining_passage" => Normal(ClosePassage()),
        "displace_the_ladder" => Normal(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(11),
            new SetCombatantCounterNode<EnemyActionContext>(
                Opponent, LadderDisplacedCounter,
                new ConstantExpression<EnemyActionContext>(1), relative: false),
        ])),
        "stone_wheel_crush" => Normal(Damage(17)),
        _ => null,
    };

    // Every normal intent is really two: itself, and the collapse it becomes when the aisles have closed.
    private static EffectProgram<EnemyActionContext> Normal(IEffectNode<EnemyActionContext> body) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    Self, new StatusDefinitionId(CompressionId)),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<EnemyActionContext>(MaxCompression)),
            ShelfCollapse(),
            @else: body));

    // Signature — Shelf Collapse: 23 damage, two more misfilings, and the aisles open again. The cash-out is
    // what the player is buying off every time they walk an Open Aisle.
    private static IEffectNode<EnemyActionContext> ShelfCollapse() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(23),
            Misfile(2),
            new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(CompressionId)),
        ]);

    // 12 + 5 per Compression, never past 27.
    private static IEffectNode<EnemyActionContext> ClosePassage() =>
        new GainBlockNode<EnemyActionContext>(Self,
            new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(MaxPassageBlock),
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(12),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(5),
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            Self, new StatusDefinitionId(CompressionId))))));

    // "Mark the last valid card instance played during the previous player turn Misfiled, IF that instance
    // can still legally be tracked in the combat deck." The two zones searched are the deck: a card that
    // exhausted itself is exactly the one that can no longer be tracked, and no card at all (a turn where
    // nothing was played) resolves to null, which every card operation reads as "no card".
    private static IEffectNode<EnemyActionContext> MisfileTheLastPlayed() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent,
                new FirstMarkedCardInOwnerZoneExpression<EnemyActionContext>(
                    Opponent, CardZone.DiscardPile, new TagId(LastPlayedMark)),
                new TagId(ActTwo.MisfiledMark)),
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent,
                new FirstMarkedCardInOwnerZoneExpression<EnemyActionContext>(
                    Opponent, CardZone.Hand, new TagId(LastPlayedMark)),
                new TagId(ActTwo.MisfiledMark)),
        ]);

    // "Mark 2 DIFFERENT valid draw-pile cards." One loop taking the first N, not N loops each taking the
    // first — the latter marks the same card twice, because a marked card is still the top of the pile.
    private static IEffectNode<EnemyActionContext> Misfile(int count) =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(),
                new TagId(ActTwo.MisfiledMark)),
            takeFirst: count);

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, new ConstantExpression<EnemyActionContext>(amount));

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
