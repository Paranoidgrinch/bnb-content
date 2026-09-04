using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Queen of the Flood Reckoning. An ancient queen beside an enormous flood gauge, whose
// monuments commemorate water levels, fields, harvests, and years survived.
//
// The gauge is the fight, and the PLAYER moves it. At the end of every one of their turns:
//
//   ENERGY LEFT OVER   the water falls a step;
//   NOTHING LEFT OVER  the water rises a step.
//
// Five readings, and each is a different fight. DROUGHT starves you and makes her stronger while it lasts;
// EXPOSED FIELDS files a sheet a turn; ORDERED FLOOD — the middle, where she starts — strips 12 of her own
// Block each round and pays you a SLUICE AUTHORITY for keeping her there; RISING WATER buries you a stack a
// turn; and BLACK FLOOD queues The River Takes the Boundary, 38 and the banks with it.
//
// Sluice Authority is the counterplay, and it is earned rather than given: spend one and the water moves a
// step back toward the ordered middle AFTER the turn's own shift resolves — which is what makes a black
// flood cancellable by a player who saw it coming a turn early.
//
// At three authorities earned (or 310) THE FLOOD NO LONGER OBEYS: the cap rises to three, and from then on
// the water DRIFTS away from the middle every second turn of hers, in a direction she shows you first.
// Below 90 she counts it anyway — 36, and the river moves one more step whatever you did about it.
public static partial class ActFour
{
    public const string QueenEnemyId = "queen_of_the_flood_reckoning";

    public const string FloodReckoningId = "the_flood_reckoning";

    public const string WaterDroughtId = "water_drought";
    public const string WaterExposedId = "water_exposed_fields";
    public const string WaterOrderedId = "water_ordered_flood";
    public const string WaterRisingId = "water_rising_water";
    public const string WaterBlackId = "water_black_flood";

    public const string SluiceAuthorityId = "sluice_authority";
    public const string DroughtStrengthId = "drought_strength";
    public const string FloodStirsId = "the_flood_stirs";
    public const string FloodDisobeysId = "the_flood_no_longer_obeys";
    public const string FloodDriftsId = "the_flood_drifts_next";
    public const string FloodCountedId = "the_flood_is_counted_anyway";

    public const string SluiceTag = "sluice_work";
    public const string SluiceCardId = "work_the_sluice";

    public const int SluiceCapPhaseOne = 2;
    public const int SluiceCapPhaseTwo = 3;
    public const int OrderedFloodsForTransition = 3;
    private const int OrderedFloodBlockStripped = 12;
    private const int FloodStirsAt = 310;
    private const int FloodDisobeysBlock = 14;
    private const int FloodCountedAt = 90;
    private const int FloodCountedBlow = 36;
    private const int BlackFloodBlow = 38;

    // The gauge, low to high. The reading IS these five markers and nothing else: one of them is always on
    // her, the intent rules key on them directly, and there is no second number anywhere that could come to
    // disagree with what the player is looking at.
    private static readonly string[] WaterMarks =
    [
        WaterDroughtId, WaterExposedId, WaterOrderedId, WaterRisingId, WaterBlackId,
    ];

    private const int OrderedFlood = 2;

    // How many authorities the ordered middle has paid out, whether this round's payment is made, whether
    // the sluice is declared for this turn, and how many of her turns the flood has disobeyed for.
    public static CounterId OrderedFloods => new("ordered_flood_count");
    public static CounterId OrderedFloodSpent => new("ordered_flood_spent");
    public static CounterId SluiceDeclared => new("sluice_declared");
    public static CounterId FloodTurns => new("flood_turns");
    public static CounterId FloodStirsTaken => new("flood_stirs_taken");
    public static CounterId FloodCountedTaken => new("flood_counted_taken");

    public static EffectProgram<EnemyActionContext>? QueenIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "queen_of_the_flood_reckoning.measure_the_fields_again" => new EffectProgram<EnemyActionContext>(
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(WeighedId), Achievable<EnemyActionContext>(2))),

            // Boss-caused water movement always resolves AFTER the blow, and is always previewed: the intent
            // says which way the river goes before the player spends the turn that answers it.
            "queen_of_the_flood_reckoning.open_the_eastern_sluice" => ByFlood(
                I: Seq(Hit(22), Debuff(InscribedId, 1), RiverRises()),
                II: Seq(Hit(24), Debuff(InscribedId, 1), RiverRises())),
            "queen_of_the_flood_reckoning.close_the_western_sluice" => ByFlood(
                I: Seq(Guard(26), Debuff(BurdenedId, 1), RiverFalls()),
                II: Seq(Guard(28), Debuff(BurdenedId, 1), RiverFalls())),
            "queen_of_the_flood_reckoning.record_the_lost_acreage" => ByFlood(
                I: Seq(Debuff(Cards.Keywords.Paperwork, 3)),
                II: Seq(Debuff(Cards.Keywords.Paperwork, 3))),
            "queen_of_the_flood_reckoning.harvest_under_authority" => ByFlood(
                I: Seq(Hit(16), Hit(16)),
                II: Seq(Hit(17), Hit(17))),
            "queen_of_the_flood_reckoning.the_banks_break" => ByFlood(
                I: Seq(Hit(30), Debuff(EntombedId, 1)),
                II: Seq(Hit(32), Debuff(EntombedId, 1))),

            // The two counts. In the first half a count is only a count; once the flood disobeys, counting a
            // dry year or a drowned field is worth what the gauge says it is.
            "queen_of_the_flood_reckoning.count_the_dry_years" => ByFlood(
                I: Seq(Hit(22)),
                II: new ConditionalEffectNode<EnemyActionContext>(
                    new OrExpression<EnemyActionContext>(
                        WaterIs<EnemyActionContext>(CombatantTargetSelectors.Source, 0),
                        WaterIs<EnemyActionContext>(CombatantTargetSelectors.Source, 1)),
                    Seq(Hit(30), Debuff("fatigue", 1)),
                    Seq(Hit(22)))),
            "queen_of_the_flood_reckoning.count_the_drowned_fields" => ByFlood(
                I: Seq(Hit(22)),
                II: new ConditionalEffectNode<EnemyActionContext>(
                    new OrExpression<EnemyActionContext>(
                        WaterIs<EnemyActionContext>(CombatantTargetSelectors.Source, 3),
                        WaterIs<EnemyActionContext>(CombatantTargetSelectors.Source, 4)),
                    Seq(Hit(30), Debuff(EntombedId, 1)),
                    Seq(Hit(22)))),

            "queen_of_the_flood_reckoning.the_river_takes_the_boundary" => TheRiverTakesTheBoundary(),
            "queen_of_the_flood_reckoning.the_flood_no_longer_obeys" => TheFloodNoLongerObeys(),
            "queen_of_the_flood_reckoning.the_flood_is_counted_anyway" => TheFloodIsCountedAnyway(),

            _ => null,
        };

    public static IReadOnlyList<StatusData> QueenStatuses() =>
    [
        TheFloodReckoning(),
        Gauge(WaterDroughtId, "Water: Drought",
            "The river is gone. Fatigue 1 at the start of your turn, and she is stronger for as long as it "
            + "lasts."),
        Gauge(WaterExposedId, "Water: Exposed Fields",
            "Too little. Paperwork 1 at the start of your turn."),
        Gauge(WaterOrderedId, "Water: Ordered Flood",
            "The reading the state was built for. Once a round she loses 12 Block and you are paid a Sluice "
            + "Authority for keeping her here."),
        Gauge(WaterRisingId, "Water: Rising Water",
            "Too much. Entombed 1 at the start of your turn."),
        Gauge(WaterBlackId, "Water: Black Flood",
            "The banks are gone. The River Takes the Boundary is her next action — unless the water is "
            + "brought back down before she acts."),
        SluiceAuthority(),
        DroughtStrength(),
        FloodStirs(),
        FloodDisobeys(),
        FloodDrifts(),
        FloodCounted(),
    ];

    public static IReadOnlyList<CardData> QueenSluiceCards() => [WorkTheSluice()];

    // ── the gauge ─────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Gauge(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description + " Spend all your Energy and the water rises a step at the end of your "
            + "turn; leave any and it falls one.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData SluiceAuthority() => new()
    {
        Id = SluiceAuthorityId,
        NameKey = "Sluice Authority",
        DescriptionKey =
            "Earned by holding her at the Ordered Flood. Spend one on your turn and the water moves a step "
            + "back toward the middle after the turn's own shift — which is how a black flood is called off.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The drought's Strength is the drought's, not hers: it is taken back the moment the river returns, and
    // a second visit to the dry mark does not stack a second one. The marker is what makes that possible —
    // it says the Strength on her belongs to the gauge and how much of it does.
    private static StatusData DroughtStrength() => new()
    {
        Id = DroughtStrengthId,
        NameKey = "Drought Strength",
        DescriptionKey = "1 Strength that belongs to the dry river. It goes the moment the water returns.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData FloodStirs() => new()
    {
        Id = FloodStirsId,
        NameKey = "The Flood No Longer Obeys",
        DescriptionKey = "Her next action is no attack: the gauge stops answering to her, you may hold one "
            + "more authority, and from then on the water drifts by itself.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData FloodDisobeys() => new()
    {
        Id = FloodDisobeysId,
        NameKey = "The Flood No Longer Obeys",
        DescriptionKey =
            "Every second turn of hers the water drifts one step AWAY from the ordered middle. You may hold "
            + "three authorities now, and the Ordered Flood still pays them.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData FloodDrifts() => new()
    {
        Id = FloodDriftsId,
        NameKey = "The Water Drifts Next",
        DescriptionKey = "At the end of her next turn the water moves one step further from the middle. "
            + "From the middle itself it rises.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData FloodCounted() => new()
    {
        Id = FloodCountedId,
        NameKey = "The Flood Is Counted Anyway",
        DescriptionKey = "36, and then the water moves one more step away from the middle whatever you did "
            + "about it. From the middle itself it rises.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheFloodReckoning() => new()
    {
        Id = FloodReckoningId,
        NameKey = "The Flood Reckoning",
        DescriptionKey =
            "The gauge answers to your Energy: spend it all and the water rises, leave any and it falls. "
            + "Hold her at the Ordered Flood and it pays you an authority to spend on the river.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TheGaugeIsRead(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(TheDayIsCounted(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheFloodFailsafes(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // What the reading DOES, and what the middle pays. The player's turn opens with the level's own pressure
    // and a sheet to spend an authority with; hers opens with the ordered flood's toll on her own Block.
    private static EffectProgram<TurnStartedTriggeredEffectContext> TheGaugeIsRead()
    {
        var queen = Bearer(FloodReckoningId);

        IEffectNode<TurnStartedTriggeredEffectContext> AtLevel(
            int level, IEffectNode<TurnStartedTriggeredEffectContext> what) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                WaterIs<TurnStartedTriggeredEffectContext>(queen, level), what);

        IEffectNode<TurnStartedTriggeredEffectContext> Suffer(string statusId, int stacks) =>
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(statusId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(stacks), sourceSelector: queen);

        var floods = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(queen, OrderedFloods);

        // ⚠ The ordered middle is read at the top of the PLAYER's turn and not of hers, and the reason is
        // the engine's one immovable fact about Block: it expires at the start of its OWNER's turn. Block she
        // gained on her turn is what the player has to get through, and it is gone before her next turn
        // begins — so "remove up to 12 Queen Block at the start of her turn" would strip a pool that is
        // already empty and mean nothing at all. Here it strips the wall the player is actually standing in
        // front of, and pays the authority into the turn that can spend it.
        var theMiddle = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                WaterIs<TurnStartedTriggeredEffectContext>(queen, OrderedFlood),
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(queen, OrderedFloodSpent),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ModifyDefensivePoolNode<TurnStartedTriggeredEffectContext>(
                    queen, StandardCombatIds.BlockDefensivePool,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(-OrderedFloodBlockStripped)),

                PayTheAuthority<TurnStartedTriggeredEffectContext>(queen),

                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    queen, OrderedFloodSpent,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    queen, OrderedFloods,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),

                // §13.6's PRIMARY trigger: three authorities earned from the middle, not a health band.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            floods, ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(
                                OrderedFloodsForTransition)),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                queen, FloodStirsTaken),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            queen, FloodStirsTaken,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            queen, new StatusDefinitionId(FloodStirsId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                            sourceSelector: queen),
                    ])),
            ]));

        var theirTurn = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                queen, SluiceDeclared,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

            AtLevel(0, Suffer("fatigue", 1)),
            AtLevel(1, Suffer(Cards.Keywords.Paperwork, 1)),
            AtLevel(3, Suffer(EntombedId, 1)),

            theMiddle,

            // An authority the player cannot reach is not a resource. The sheet is laid whenever they hold
            // one — after the middle has paid, so an authority earned this turn can be spent on this turn.
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(SluiceAuthorityId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(SluiceCardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
        ]);

        // Once a round: her own turn start is where the middle is allowed to pay again.
        var herTurn = new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
            queen, OrderedFloodSpent,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), theirTurn, herTurn));
    }

    // The end of a turn is where the gauge actually moves. The player's own turn ends with the two shifts in
    // the order §13.4 fixes — the Energy reading first, the declared sluice second — so a river pushed to
    // the black mark by the turn's own spending can still be pulled back off it by an authority spent
    // before the shift was known. Hers ends with the drift, once the flood stops obeying.
    private static EffectProgram<TurnEndedTriggeredEffectContext> TheDayIsCounted()
    {
        var queen = Bearer(FloodReckoningId);

        var unspent = new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
            Applicant, StandardCombatIds.EnergyResource);

        var theirTurn = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    unspent, ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                Rise<TurnEndedTriggeredEffectContext>(queen),
                Fall<TurnEndedTriggeredEffectContext>(queen)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(queen, SluiceDeclared),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                TowardTheMiddle<TurnEndedTriggeredEffectContext>(queen)),

            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                queen, SluiceDeclared,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
        ]);

        var turns = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(queen, FloodTurns);

        var herTurn = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                queen, new StatusDefinitionId(FloodDisobeysId)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    queen, FloodTurns,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),

                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new RemainderExpression<TurnEndedTriggeredEffectContext>(
                            turns, new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        FromTheMiddle<TurnEndedTriggeredEffectContext>(queen),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            queen, new StatusDefinitionId(FloodDriftsId)),
                    ]),

                    // The turn BEFORE a drift is where the warning belongs — a drift the player cannot see
                    // coming is the same problem one turn earlier.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new NotExpression<TurnEndedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                                queen, new StatusDefinitionId(FloodDriftsId))),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            queen, new StatusDefinitionId(FloodDriftsId),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                            sourceSelector: queen))),
            ]));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(), theirTurn, herTurn));
    }

    // 310 is the failsafe under §13.6's real trigger, and 90 announces the last count. Both are telegraphs
    // and neither is an action.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheFloodFailsafes()
    {
        var queen = Bearer(FloodReckoningId);
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(queen);

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> At(int band) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                health, ComparisonOperator.LessOrEqual,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(band));

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> NotYet(CounterId taken) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(queen, taken),
                ComparisonOperator.Equal,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0));

        IEffectNode<DamageReceivedTriggeredEffectContext> Announce(CounterId taken, string markerId) =>
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    queen, taken, new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                    relative: false),
                new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                    queen, new StatusDefinitionId(markerId),
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), sourceSelector: queen),
            ]);

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(FloodStirsAt), NotYet(FloodStirsTaken)),
                    Announce(FloodStirsTaken, FloodStirsId)),

                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(FloodCountedAt), NotYet(FloodCountedTaken)),
                    Announce(FloodCountedTaken, FloodCountedId)),
            ]));
    }

    // ── moving the river ──────────────────────────────────────────────────────────────────────────────────

    public static ICombatExpression<TContext, bool> WaterIs<TContext>(
        ICombatantTargetSelector queen, int level) where TContext : class =>
        new TargetHasStatusExpression<TContext>(queen, new StatusDefinitionId(WaterMarks[level]));

    // Setting the gauge is setting the ONE mark it stands at, and taking back or granting what the dry mark
    // lends her on the way past.
    private static IEffectNode<TContext> SetWater<TContext>(ICombatantTargetSelector queen, int level)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            .. WaterMarks.Select(m => (IEffectNode<TContext>)
                new RemoveStatusNode<TContext>(queen, new StatusDefinitionId(m))),
            new ApplyStatusNode<TContext>(
                queen, new StatusDefinitionId(WaterMarks[level]),
                new ConstantExpression<TContext>(1), sourceSelector: queen),

            level == 0
                ? new ConditionalEffectNode<TContext>(
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            queen, new StatusDefinitionId(DroughtStrengthId))),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new ApplyStatusNode<TContext>(
                            queen, new StatusDefinitionId("strength"),
                            new ConstantExpression<TContext>(1), sourceSelector: queen),
                        new ApplyStatusNode<TContext>(
                            queen, new StatusDefinitionId(DroughtStrengthId),
                            new ConstantExpression<TContext>(1), sourceSelector: queen),
                    ]))
                : new ConditionalEffectNode<TContext>(
                    new TargetHasStatusExpression<TContext>(
                        queen, new StatusDefinitionId(DroughtStrengthId)),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new ModifyStatusStacksNode<TContext>(
                            queen, new StatusDefinitionId("strength"),
                            new NegateExpression<TContext>(
                                new CombatantStatusStacksExpression<TContext>(
                                    queen, new StatusDefinitionId(DroughtStrengthId)))),
                        new RemoveStatusNode<TContext>(
                            queen, new StatusDefinitionId(DroughtStrengthId)),
                    ])),
        ]);

    // One shift, written as "where does each reading go?". Every movement in the fight is one of four
    // answers to that question, and a reading with nowhere to go simply stays where it is — which is the
    // clamp, and needs no separate rule.
    private static IEffectNode<TContext> Shift<TContext>(
        ICombatantTargetSelector queen, Func<int, int> destination, int level = 0) where TContext : class
    {
        if (level >= WaterMarks.Length)
            return new NoOpEffectNode<TContext>();

        var to = destination(level);
        var move = to == level
            ? (IEffectNode<TContext>)new NoOpEffectNode<TContext>()
            : SetWater<TContext>(queen, to);

        return new ConditionalEffectNode<TContext>(
            WaterIs<TContext>(queen, level), move, Shift<TContext>(queen, destination, level + 1));
    }

    private static IEffectNode<TContext> Rise<TContext>(ICombatantTargetSelector queen) where TContext : class =>
        Shift<TContext>(queen, l => Math.Min(WaterMarks.Length - 1, l + 1));

    private static IEffectNode<TContext> Fall<TContext>(ICombatantTargetSelector queen) where TContext : class =>
        Shift<TContext>(queen, l => Math.Max(0, l - 1));

    private static IEffectNode<TContext> TowardTheMiddle<TContext>(ICombatantTargetSelector queen)
        where TContext : class =>
        Shift<TContext>(queen, l => l < OrderedFlood ? l + 1 : l > OrderedFlood ? l - 1 : l);

    // Away from the middle — and from the middle itself the river RISES, which is the direction the marker
    // and the signature both name in advance so it is never a coin the player did not see flipped.
    private static IEffectNode<TContext> FromTheMiddle<TContext>(ICombatantTargetSelector queen)
        where TContext : class =>
        Shift<TContext>(queen, l => l < OrderedFlood
            ? Math.Max(0, l - 1)
            : Math.Min(WaterMarks.Length - 1, l + 1));

    // An intent moves the river from her own body, so the two readings an intent ever needs are named once.
    private static IEffectNode<EnemyActionContext> RiverRises() =>
        Rise<EnemyActionContext>(CombatantTargetSelectors.Source);

    private static IEffectNode<EnemyActionContext> RiverFalls() =>
        Fall<EnemyActionContext>(CombatantTargetSelectors.Source);

    // The ordered middle's payment, up to whatever the current cap is. A cap is not a branch the engine can
    // read as a number, so both readings are written out and one of them is true.
    private static IEffectNode<TContext> PayTheAuthority<TContext>(ICombatantTargetSelector queen)
        where TContext : class
    {
        var held = new CombatantStatusStacksExpression<TContext>(
            Applicant, new StatusDefinitionId(SluiceAuthorityId));

        IEffectNode<TContext> PayUpTo(int cap) =>
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    held, ComparisonOperator.Less, new ConstantExpression<TContext>(cap)),
                new ApplyStatusNode<TContext>(
                    Applicant, new StatusDefinitionId(SluiceAuthorityId),
                    new ConstantExpression<TContext>(1), sourceSelector: queen));

        return new ConditionalEffectNode<TContext>(
            new TargetHasStatusExpression<TContext>(queen, new StatusDefinitionId(FloodDisobeysId)),
            PayUpTo(SluiceCapPhaseTwo), PayUpTo(SluiceCapPhaseOne));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> ByFlood(
        IEffectNode<EnemyActionContext> I, IEffectNode<EnemyActionContext> II) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FloodDisobeysId)),
            II, I));

    // §13.3's Black Flood, and the one thing it always does afterwards: the river goes back to the middle,
    // so the reading the player has to manage is a cycle rather than a wall.
    private static EffectProgram<EnemyActionContext> TheRiverTakesTheBoundary() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Hit(BlackFloodBlow),
            Debuff(EntombedId, 2),
            Debuff(BurdenedId, 1),
            SetWater<EnemyActionContext>(CombatantTargetSelectors.Source, OrderedFlood),
        ]));

    private static EffectProgram<EnemyActionContext> TheFloodNoLongerObeys() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ApplyStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FloodDisobeysId), Const(1),
                sourceSelector: CombatantTargetSelectors.Source),
            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FloodStirsId)),
            Guard(FloodDisobeysBlock),
        ]));

    private static EffectProgram<EnemyActionContext> TheFloodIsCountedAnyway() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Hit(FloodCountedBlow),
            FromTheMiddle<EnemyActionContext>(CombatantTargetSelectors.Source),
            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FloodCountedId)),
        ]));

    // ── the sluice, as a card ─────────────────────────────────────────────────────────────────────────────

    // The authority is DECLARED on the player's turn and resolves at its end, after the Energy reading — the
    // order §13.4 fixes, and the only order this engine has a window for. Declaring it before knowing the
    // shift is the whole decision: an authority spent on a turn that was going to fall anyway is wasted.
    private static CardData WorkTheSluice()
    {
        var queen = Bearer(FloodReckoningId);

        return new CardData
        {
            Id = SluiceCardId,
            NameKey = "Work the Sluice",
            DescriptionKey =
                "Spend 1 Sluice Authority: at the end of this turn, after the river answers your Energy, it "
                + "moves one step back toward the Ordered Flood. Once a turn.",
            Costs = [],
            Tags = [new TagId(SluiceTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantCounterExpression<CardPlayContext>(queen, SluiceDeclared),
                            ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(0)),
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantStatusStacksExpression<CardPlayContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(SluiceAuthorityId)),
                            ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0))),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new ModifyStatusStacksNode<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(SluiceAuthorityId),
                            new ConstantExpression<CardPlayContext>(-1)),
                        new SetCombatantCounterNode<CardPlayContext>(
                            queen, SluiceDeclared, new ConstantExpression<CardPlayContext>(1),
                            relative: false),
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
