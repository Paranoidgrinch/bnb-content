using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Lady of the Black Granaries. An ancient court official sitting between four immense
// black granaries. She carries no weapon. She decides what is counted as sufficient.
//
// Every turn she names a RATION — a number of cards, reachable from the state the turn actually opens with —
// and the whole fight is what happens around that number:
//
//   EXACT     her next blow lands 5 softer, and you break one of her four SEALS.
//   UNDER     Reserve intact: a Grain, and she eats 7 a head at the end of her turn.
//             Record intact: 2 Paperwork.
//   OVER      Labor intact: Burdened. Record intact: 2 Paperwork.
//
// The seals are four STATE FUNCTIONS, and breaking one takes that function away for good — which is the
// whole fight: the player is not grinding down four identical shields, they are deciding, in order, which of
// her powers to dismantle. Reserve is her healing. Labor is her burden. Record is her paperwork. Ration turns
// correct rationing into a damage window: with it broken, every further exact ration costs her 10 blood and
// 10 cover.
//
// All four broken and THE STORES STAND OPEN for two whole player turns — nothing to heal with, nothing to
// stand behind, and a quarter more from everything you land. Then FAMINE ACCOUNTING: the rations alternate
// 2 → 5 → 2 → 5, every miss is a step of Famine, and at three the Empty Storehouse answers.
public static partial class ActFour
{
    public const string LadyEnemyId = "lady_of_the_black_granaries";

    public const string BlackGranariesId = "the_black_granaries";
    public const string ReserveSealId = "granary_reserve_seal";
    public const string LaborSealId = "granary_labor_seal";
    public const string RecordSealId = "granary_record_seal";
    public const string LadyRationSealId = "granary_ration_seal";

    public const string LadyRationId = "the_ration";
    public const string GrainId = "grain";
    public const string ShortMeasureId = "short_measure";
    public const string GranariesOpenId = "the_granaries_stand_open";
    public const string FamineAccountingId = "famine_accounting";
    public const string FamineId = "famine";

    public const string ReserveCardId = "break_the_granary_reserve_seal";
    public const string LaborCardId = "break_the_granary_labor_seal";
    public const string RecordCardId = "break_the_granary_record_seal";
    public const string LadyRationCardId = "break_the_granary_ration_seal";

    public const string BlackSealTag = "black_granary_seal";

    public const int FamineLimit = 3;
    public const int OpenPlayerTurns = 2;
    private const int GrainPerHead = 7;
    private const int RationWindowLoss = 10;
    private const int EmergencyOpeningAt = 300;

    // What the player is owed, how long the stores have stood open, and where the ration table has got to.
    public static CounterId SealBreakOwed => new("seal_break_owed");
    public static CounterId OpenTurns => new("open_store_turns");
    public static CounterId RationStep => new("ration_step");

    private static readonly string[] LadySeals = [ReserveSealId, LaborSealId, RecordSealId, LadyRationSealId];

    public static EffectProgram<EnemyActionContext>? LadyIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "lady_of_the_black_granaries.seal_the_storehouse" => SealTheStorehouse(),
            "lady_of_the_black_granaries.nothing_leaves_uncounted" => NothingLeavesUncounted(),
            "lady_of_the_black_granaries.ration_the_living" => RationTheLiving(),
            "lady_of_the_black_granaries.the_hungry_crowd" => TheHungryCrowd(),
            "lady_of_the_black_granaries.open_one_jar" => OpenOneJar(),
            "lady_of_the_black_granaries.the_empty_storehouse" => TheEmptyStorehouse(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> LadyStatuses() =>
    [
        TheBlackGranaries(),
        GranarySeal(ReserveSealId, "Reserve Seal",
            "Intact, an under-ration feeds her: a Grain, and 7 healed a head at the end of her turn."),
        GranarySeal(LaborSealId, "Labor Seal",
            "Intact, an over-ration is answered with Burdened."),
        GranarySeal(RecordSealId, "Record Seal",
            "Intact, any failed ration is written up: 2 Paperwork."),
        GranarySeal(LadyRationSealId, "Ration Seal",
            "Intact, a correct ration is merely correct. Broken, every exact ration costs her 10 blood and "
            + "10 cover."),
        TheRation(),
        Grain(),
        MeasuredExactly(),
        GranariesStandOpen(),
        FamineAccounting(),
        Famine(),
    ];

    public static IReadOnlyList<CardData> LadySealCards() =>
    [
        GranarySealCard(ReserveCardId, "Break the Reserve Seal",
            "She can no longer take Grain, and no longer eats. You heal 5.",
            ReserveSealId, lady => [new HealNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(5))]),

        GranarySealCard(LaborCardId, "Break the Labor Seal",
            "An over-ration can no longer burden you. One Burdened comes off.",
            LaborSealId, lady => [new ModifyStatusStacksNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(BurdenedId),
                new ConstantExpression<CardPlayContext>(-1))]),

        GranarySealCard(RecordCardId, "Break the Record Seal",
            "A failed ration can no longer be written up. Up to 2 Paperwork comes off.",
            RecordSealId, lady => [new ModifyStatusStacksNode<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(Cards.Keywords.Paperwork),
                new ConstantExpression<CardPlayContext>(-2))]),

        GranarySealCard(LadyRationCardId, "Break the Ration Seal",
            "From now on every exact ration costs her 10 blood and 10 cover. She loses 10 cover now.",
            LadyRationSealId, lady => [new ModifyDefensivePoolNode<CardPlayContext>(
                lady, StandardCombatIds.BlockDefensivePool,
                new ConstantExpression<CardPlayContext>(-RationWindowLoss))]),
    ];

    // ── the four functions, and the numbers around them ───────────────────────────────────────────────────

    private static StatusData GranarySeal(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData TheRation() => new()
    {
        Id = LadyRationId,
        NameKey = "Ration",
        DescriptionKey =
            "What she has counted as sufficient for the turn ahead: play exactly this many cards, rubbish "
            + "and her own seals not counted.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData Grain() => new()
    {
        Id = GrainId,
        NameKey = "Grain",
        DescriptionKey = "Taken from an under-ration. She eats 7 a head at the end of her turn.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData MeasuredExactly() => new()
    {
        Id = ShortMeasureId,
        NameKey = "Measured Exactly",
        DescriptionKey = "Her next blow lands 5 softer.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.AddFlat, -5, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers =
        [
            Trigger(new EffectProgram<DamageDealtTriggeredEffectContext>(
                new RemoveStatusNode<DamageDealtTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(ShortMeasureId))),
                nameof(TriggerEvent.DamageDealt)),
        ],
    };

    public static StatusData GranariesStandOpen() => new()
    {
        Id = GranariesOpenId,
        NameKey = "The Stores Stand Open",
        DescriptionKey =
            "Two whole turns with nothing to eat and nothing to stand behind. Everything you land on her "
            + "goes 25% further.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 125, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    public static StatusData FamineAccounting() => new()
    {
        Id = FamineAccountingId,
        NameKey = "Famine Accounting",
        DescriptionKey =
            "The second half. The rations run 2, 5, 2, 5; every miss is a step of Famine and every exact "
            + "ration costs her 10 blood, 10 cover and a step back.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData Famine() => new()
    {
        Id = FamineId,
        NameKey = "Famine",
        DescriptionKey = "Every ration she has counted as failed. At three the Empty Storehouse answers.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheBlackGranaries() => new()
    {
        Id = BlackGranariesId,
        NameKey = "The Black Granaries",
        DescriptionKey =
            "Four seals, and a ration every turn. Play exactly what she counted as sufficient and you break "
            + "a seal of your choosing; play too few or too many and the seals still standing answer for it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(CountOutTheRation(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(JudgeTheRation(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(EmergencyOpening(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // ── the ration ────────────────────────────────────────────────────────────────────────────────────────

    // After the draw: the open-store window is counted, the next ration is announced against what the turn
    // can actually reach, and any seal-break the player earned is laid in their hand as a choice of four.
    //
    // The order matters: the ration is measured BEFORE her seals are handed over, because a seal card is not
    // one of the cards the ration counts.
    private static EffectProgram<TurnStartedTriggeredEffectContext> CountOutTheRation()
    {
        var lady = Bearer(BlackGranariesId);

        var energy = new CombatantCurrentResourceExpression<TurnStartedTriggeredEffectContext>(
            Applicant, StandardCombatIds.EnergyResource);

        var inHand = new SubtractExpression<TurnStartedTriggeredEffectContext>(
            new CombatantZoneCardCountExpression<TurnStartedTriggeredEffectContext>(Applicant, CardZone.Hand),
            new CombatantZoneCardCountExpression<TurnStartedTriggeredEffectContext>(
                Applicant, CardZone.Hand, new TagId(Cards.CardAuthoring.JunkTag)));

        // §5.2 for a number of CARDS rather than a number of Energy: what a turn can reach is bounded by what
        // it can pay and by what it is holding, and never falls below two.
        ICombatExpression<TurnStartedTriggeredEffectContext, int> Reachable(int preferred) =>
            new MaxExpression<TurnStartedTriggeredEffectContext>(
                new ConstantExpression<TurnStartedTriggeredEffectContext>(2),
                new MinExpression<TurnStartedTriggeredEffectContext>(
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(preferred),
                    new MinExpression<TurnStartedTriggeredEffectContext>(energy, inHand)));

        // The ration is a face on HER, not on the player — see ADAPTATIONS: a neutral rule-marker applied to
        // the player is an application like any other, so the register enlarges it and eats an Inscribed
        // doing it. An announcement is not a thing that happens to you.
        IEffectNode<TurnStartedTriggeredEffectContext> Announce(int preferred) =>
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                lady, new StatusDefinitionId(LadyRationId), Reachable(preferred), sourceSelector: lady);

        var everyOther = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new RemainderExpression<TurnStartedTriggeredEffectContext>(
                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(lady, RationStep),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
            ComparisonOperator.Equal, new ConstantExpression<TurnStartedTriggeredEffectContext>(0));

        IEffectNode<TurnStartedTriggeredEffectContext> Offer(string cardId, string sealId) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                    lady, new StatusDefinitionId(sealId)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(cardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // The open stores stand for two complete player turns, then the books are closed.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            lady, new StatusDefinitionId(GranariesOpenId)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    lady, OpenTurns),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(OpenPlayerTurns)),
                            FamineAccountingBegins<TurnStartedTriggeredEffectContext>(lady),
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                lady, OpenTurns,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                relative: true))),

                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        lady, new StatusDefinitionId(LadyRationId)),

                    // Phase I prefers 3 and 4; famine runs 2 and 5.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            lady, new StatusDefinitionId(FamineAccountingId)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            everyOther, Announce(2), Announce(5)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            everyOther, Announce(3), Announce(4))),

                    // …and what an exact ration earned: the intact seals, as a hand of choices.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                lady, SealBreakOwed),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            Offer(ReserveCardId, ReserveSealId),
                            Offer(LaborCardId, LaborSealId),
                            Offer(RecordCardId, RecordSealId),
                            Offer(LadyRationCardId, LadyRationSealId),
                        ])),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        lady, RationStep,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                ])));
    }

    // At the player's turn end — the one moment the number of cards played is final — and at hers, where the
    // grain is eaten.
    private static EffectProgram<TurnEndedTriggeredEffectContext> JudgeTheRation()
    {
        var lady = Bearer(BlackGranariesId);

        // What the player DID: rubbish is not a card played, and neither is one of her own seals.
        var played = new SubtractExpression<TurnEndedTriggeredEffectContext>(
            new SubtractExpression<TurnEndedTriggeredEffectContext>(
                new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant),
                new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                    Applicant, new TagId(Cards.CardAuthoring.JunkTag))),
            new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                Applicant, new TagId(BlackSealTag)));

        var required = new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
            lady, new StatusDefinitionId(LadyRationId));

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> Intact(string sealId) =>
            new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                lady, new StatusDefinitionId(sealId));

        var famine = new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
            lady, new StatusDefinitionId(FamineAccountingId));

        IEffectNode<TurnEndedTriggeredEffectContext> Give(string statusId, int stacks) =>
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(statusId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(stacks), sourceSelector: lady);

        var anySeal = LadySeals
            .Select(id => (ICombatExpression<TurnEndedTriggeredEffectContext, bool>)Intact(id))
            .Aggregate((a, b) => new OrExpression<TurnEndedTriggeredEffectContext>(a, b));

        var exact = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                lady, new StatusDefinitionId(ShortMeasureId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: lady),

            // The choice is owed, and taken next turn from the seals still standing.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                anySeal,
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    lady, SealBreakOwed, new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                    relative: false)),

            // With the Ration Seal broken, counting correctly is a weapon.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new NotExpression<TurnEndedTriggeredEffectContext>(Intact(LadyRationSealId)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new DealDamageNode<TurnEndedTriggeredEffectContext>(
                        lady, new ConstantExpression<TurnEndedTriggeredEffectContext>(RationWindowLoss),
                        ignoresBlock: true, kind: DamageKind.DamageOverTime),
                    new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                        lady, StandardCombatIds.BlockDefensivePool,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-RationWindowLoss)),
                ])),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                famine,
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    lady, new StatusDefinitionId(FamineId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1))),
        ]);

        var under = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Intact(ReserveSealId),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    lady, new StatusDefinitionId(GrainId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: lady)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Intact(RecordSealId), Give(Cards.Keywords.Paperwork, 2)),
        ]);

        var over = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Intact(LaborSealId), Give(BurdenedId, 1)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Intact(RecordSealId), Give(Cards.Keywords.Paperwork, 2)),
        ]);

        var missed = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    played, ComparisonOperator.Less, required),
                under, over),

            // In famine every miss is a step, and three of them empty the storehouse.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    famine,
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            lady, new StatusDefinitionId(FamineId)),
                        ComparisonOperator.Less,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(FamineLimit))),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    lady, new StatusDefinitionId(FamineId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: lady)),
        ]);

        // Her own turn end: the grain is eaten, unless the stores are standing open.
        var eat = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        lady, new StatusDefinitionId(GrainId)),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new NotExpression<TurnEndedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                        lady, new StatusDefinitionId(GranariesOpenId)))),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new HealNode<TurnEndedTriggeredEffectContext>(
                    lady,
                    new MultiplyExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(GrainPerHead),
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            lady, new StatusDefinitionId(GrainId)))),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    lady, new StatusDefinitionId(GrainId)),
            ]));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        PlayersTurn<TurnEndedTriggeredEffectContext>(),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            required, ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                played, ComparisonOperator.Equal, required),
                            exact, missed),

                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            lady, new StatusDefinitionId(LadyRationId)),
                    ])),

                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(BlackGranariesId)),
                    eat),
            ]));
    }

    // ── the stores ────────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<TContext> TheGranariesStandOpen<TContext>(ICombatantTargetSelector lady)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new ApplyStatusNode<TContext>(
                lady, new StatusDefinitionId(GranariesOpenId), new ConstantExpression<TContext>(1),
                sourceSelector: lady),
            new SetCombatantCounterNode<TContext>(
                lady, OpenTurns, new ConstantExpression<TContext>(0), relative: false),
        ]);

    // The transition, and it is not an attack: the grain goes, the cover goes, the seals stay broken.
    private static IEffectNode<TContext> FamineAccountingBegins<TContext>(ICombatantTargetSelector lady)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(lady, new StatusDefinitionId(GranariesOpenId)),
            new RemoveStatusNode<TContext>(lady, new StatusDefinitionId(GrainId)),
            new ModifyDefensivePoolNode<TContext>(
                lady, StandardCombatIds.BlockDefensivePool, new ConstantExpression<TContext>(-999)),
            new ApplyStatusNode<TContext>(
                lady, new StatusDefinitionId(FamineAccountingId), new ConstantExpression<TContext>(1),
                sourceSelector: lady),
        ]);

    // The failsafe: at 300 with seals still standing they are all struck off at once — and none of them pays
    // its breaking reward, because nobody chose.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> EmergencyOpening()
    {
        var lady = Bearer(BlackGranariesId);

        var anySeal = LadySeals
            .Select(id => (ICombatExpression<DamageReceivedTriggeredEffectContext, bool>)
                new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                    lady, new StatusDefinitionId(id)))
            .Aggregate((a, b) => new OrExpression<DamageReceivedTriggeredEffectContext>(a, b));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(lady),
                        ComparisonOperator.LessOrEqual,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(EmergencyOpeningAt)),
                    anySeal),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    .. LadySeals.Select(id =>
                        (IEffectNode<DamageReceivedTriggeredEffectContext>)
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                            lady, new StatusDefinitionId(id))),

                    TheGranariesStandOpen<DamageReceivedTriggeredEffectContext>(lady),
                ])));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // One slot, two meanings — the same shape the Pharaoh's names use, and the phase marker beside the
    // telegraph is what says which of them is standing (BossPhases).
    private static EffectProgram<EnemyActionContext> ByAccounting(
        IEffectNode<EnemyActionContext> plenty, IEffectNode<EnemyActionContext> famine) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FamineAccountingId)),
            famine, plenty));

    private static IEffectNode<EnemyActionContext> WhileIntact(
        string sealId, IEffectNode<EnemyActionContext> then, IEffectNode<EnemyActionContext> otherwise) =>
        new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(sealId)),
            then, otherwise);

    // Seal the Storehouse — and with nothing left to seal she has nothing to stand behind: the master's "only
    // while at least one Seal remains" is why an open storehouse cannot defend itself.
    private static EffectProgram<EnemyActionContext> SealTheStorehouse()
    {
        var anySeal = LadySeals
            .Select(id => (ICombatExpression<EnemyActionContext, bool>)
                new TargetHasStatusExpression<EnemyActionContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id)))
            .Aggregate((a, b) => new OrExpression<EnemyActionContext>(a, b));

        return ByAccounting(
            new ConditionalEffectNode<EnemyActionContext>(anySeal, Guard(28), Hit(24)),
            Seq(Guard(26), Debuff(BurdenedId, 1)));
    }

    private static EffectProgram<EnemyActionContext> NothingLeavesUncounted() =>
        ByAccounting(
            WhileIntact(RecordSealId, Debuff(Cards.Keywords.Paperwork, 3), Hit(20)),
            Seq(Hit(22), Debuff(Cards.Keywords.Paperwork, 2)));

    private static EffectProgram<EnemyActionContext> RationTheLiving() =>
        ByAccounting(
            WhileIntact(LaborSealId, Debuff(BurdenedId, 2), Hit(22)),
            Seq(Hit(24), Debuff(InscribedId, 1)));

    private static EffectProgram<EnemyActionContext> TheHungryCrowd() =>
        ByAccounting(
            Seq(Hit(8), Hit(8), Hit(8), Hit(8)),
            Seq(Hit(9), Hit(9), Hit(9), Hit(9)));

    // Open One Jar — "otherwise ineligible" in the master. An intent the engine has reached cannot step
    // aside, so an empty reserve is answered with the plainest thing she has left: a blow.
    private static EffectProgram<EnemyActionContext> OpenOneJar() =>
        ByAccounting(
            WhileIntact(ReserveSealId,
                new HealNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(18)), Hit(20)),
            Hit(31));

    private static EffectProgram<EnemyActionContext> TheEmptyStorehouse() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Hit(38),
            Debuff(BurdenedId, 1),
            Debuff(InscribedId, 1),
            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(FamineId)),
        ]));

    // ── the seals, as cards ───────────────────────────────────────────────────────────────────────────────

    // A seal is broken by PLAYING it — which is how a choice among four is put in front of the player. The
    // card costs nothing, is only ever in hand when a break is owed, and breaks exactly one.
    private static CardData GranarySealCard(
        string id, string name, string text, string sealId,
        Func<ICombatantTargetSelector, IReadOnlyList<IEffectNode<CardPlayContext>>> reward)
    {
        var lady = Bearer(BlackGranariesId);

        var anySealLeft = LadySeals
            .Select(other => (ICombatExpression<CardPlayContext, bool>)
                new TargetHasStatusExpression<CardPlayContext>(lady, new StatusDefinitionId(other)))
            .Aggregate((a, b) => new OrExpression<CardPlayContext>(a, b));

        var body = new List<IEffectNode<CardPlayContext>>
        {
            new RemoveStatusNode<CardPlayContext>(lady, new StatusDefinitionId(sealId)),
            new SetCombatantCounterNode<CardPlayContext>(
                lady, SealBreakOwed, new ConstantExpression<CardPlayContext>(0), relative: false),
        };
        body.AddRange(reward(lady));

        // Causal: the fourth seal has to SEE that it was the last one, or the stores never open on the break
        // that opened them.
        body.Add(new ConditionalEffectNode<CardPlayContext>(
            new NotExpression<CardPlayContext>(anySealLeft),
            TheGranariesStandOpen<CardPlayContext>(lady)));

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [new TagId(BlackSealTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantCounterExpression<CardPlayContext>(lady, SealBreakOwed),
                            ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                        new TargetHasStatusExpression<CardPlayContext>(
                            lady, new StatusDefinitionId(sealId))),
                    new CausalSequenceEffectNode<CardPlayContext>(body))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
