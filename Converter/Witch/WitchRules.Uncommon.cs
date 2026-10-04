using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The uncommons' marks and Rites (hedge_witch_master.md §12), and the places where her own keywords answer them:
// what a Hexed burst does besides the burst, and what a Misfortune roll does besides the roll. Those two hooks are
// called from WitchKeywords — the keyword asks, the card's mark answers — so a card never has to re-implement
// the third night or the roll to bend it.
public static partial class WitchRules
{
    // ── enemy marks: read by Hexed's burst and by Misfortune's roll ───────────────────────────────────────
    public const string ThirdBell = "third_bell";             // Third Bell: its burst spreads this much Hexed
    public const string DeadMansHex = "dead_mans_hex";        // Dead Man's Hex: dying, it passes its Hexed on
    public const string CrossedFingers = "crossed_fingers";   // Cross Your Fingers: a failed roll pays her Block
    public const string BadPenny = "bad_penny";               // Bad Penny: a failed roll leaves this behind
    public const string MagpiesLuck = "magpies_luck";         // Magpie's Luck: a roll that lands pays her cards

    // ── her own marks and Rites ───────────────────────────────────────────────────────────────────────────
    public const string HedgehogCurl = "hedgehog_curl";
    public const string HawthornWall = "hawthorn_wall";
    public const string NailInTheDoorpost = "nail_in_the_doorpost";
    public const string HorseshoeOverTheDoor = "horseshoe_over_the_door";
    public const string MagpieDraw = "magpie_draw";
    public const string ProperSupper = "proper_supper";
    public const string MidwifesHands = "midwifes_hands";
    public const string TellTheBees = "tell_the_bees";

    // Her standing habits, worn every fight with the cauldron: what waits in her hand is counted here.
    public const string Habits = "hedge_witch_habits";
    public static CounterId SleeveWaited => new("sleeve_waited");
    public const string SleeveTag = "ing_adder_in_the_sleeve";

    private static CounterId NailLatch => new("nail_in_the_doorpost_used");
    private static CounterId HorseshoeLatch => new("horseshoe_over_the_door_used");
    private static CounterId BeesLatch => new("tell_the_bees_used");
    private static CounterId Landed => new("misfortune_landed");

    private static IReadOnlyList<StatusData> UncommonRules() =>
    [
        EnemyMark(ThirdBell, "Third Bell", "When this character's Hex bursts this turn, every other enemy gains this much Hexed."),
        DeadMansHexMark(),
        EnemyMark(CrossedFingers, "Crossed Fingers", "If this character's Misfortune roll fails, the Witch gains this much Block."),
        EnemyMark(BadPenny, "Bad Penny", "If this character's Misfortune roll fails, this much Misfortune stays for its next action."),
        EnemyMark(MagpiesLuck, "Magpie's Luck", "If this character's Misfortune makes its action fail, the Witch draws this many cards next turn."),
        Retaliation(HedgehogCurl, "Hedgehog Curl", "Every enemy attack on you this turn costs the attacker this much damage.",
            (who, stacks) => new DealDamageNode<ActionResolvedTriggeredEffectContext>(who, stacks)),
        Retaliation(HawthornWall, "Hawthorn Wall", "Every enemy that attacks you this turn gains this much Hexed.",
            (who, stacks) => new ApplyStatusNode<ActionResolvedTriggeredEffectContext>(
                who, new StatusDefinitionId(WitchKeywords.Hexed), stacks)),
        FirstEachTurn(NailInTheDoorpost, "Nail in the Doorpost", WitchKeywords.Hexed, NailLatch, spread: true,
            "The first time each turn you apply Hexed to an enemy, another enemy gains this much Hexed."),
        FirstEachTurn(HorseshoeOverTheDoor, "Horseshoe Over the Door", WitchKeywords.Misfortune, HorseshoeLatch, spread: false,
            "The first time each turn you apply Misfortune to an enemy, it gains this much more."),
        MagpieDrawStatus(),
        ProperSupperStatus(),
        MidwifesHandsRite(),
        TellTheBeesRite(),
        HabitsStatus(),
    ];

    // ── the Hexed burst's answers (called by WitchKeywords' Hexed) ────────────────────────────────────────

    // Inside the burst, after the HP loss: Self is the hexed enemy.
    public static IEffectNode<TurnEndedTriggeredEffectContext> OnBurst() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            IfWears<TurnEndedTriggeredEffectContext>(Self, ThirdBell,
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(OtherEnemies(Self),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(WitchKeywords.Hexed),
                        StacksOf<TurnEndedTriggeredEffectContext>(Self, ThirdBell)))),
            OnBurstRare(),
        ]);

    // Every turn's end, burst or not: the marks that last "this turn" are gone.
    public static IEffectNode<TurnEndedTriggeredEffectContext> AfterTurn() =>
        new SequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(ThirdBell)),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(HareRunsLast)),
        ]);

    // ── the Misfortune roll's answers (called by WitchKeywords' Misfortune) ───────────────────────────────

    // Self is the enemy whose action was rolled for; the roll's outcome is written down for AfterRoll.
    public static IEffectNode<ActionStartingTriggeredEffectContext> RollLanded(bool landed) =>
        new SetCombatantCounterNode<ActionStartingTriggeredEffectContext>(Self, Landed,
            new ConstantExpression<ActionStartingTriggeredEffectContext>(landed ? 1 : 0), relative: false);

    public static ICombatExpression<ActionStartingTriggeredEffectContext, bool> DidLand =>
        new ComparisonExpression<ActionStartingTriggeredEffectContext>(
            new CombatantCounterExpression<ActionStartingTriggeredEffectContext>(Self, Landed),
            ComparisonOperator.Greater, new ConstantExpression<ActionStartingTriggeredEffectContext>(0));

    public static IEffectNode<ActionStartingTriggeredEffectContext> AfterRoll() =>
        new CausalSequenceEffectNode<ActionStartingTriggeredEffectContext>(
        [
            new ConditionalEffectNode<ActionStartingTriggeredEffectContext>(DidLand,
                // It failed its action: Magpie's Luck pays her, and the rares answer.
                new SequenceEffectNode<ActionStartingTriggeredEffectContext>(
                [
                    IfWears<ActionStartingTriggeredEffectContext>(Self, MagpiesLuck,
                        ForTheWitch<ActionStartingTriggeredEffectContext>(her =>
                            new ApplyStatusNode<ActionStartingTriggeredEffectContext>(
                                her, new StatusDefinitionId(MagpieDraw),
                                StacksOf<ActionStartingTriggeredEffectContext>(Self, MagpiesLuck)))),
                    LandedRare(),
                ]),
                @else: new CausalSequenceEffectNode<ActionStartingTriggeredEffectContext>(
                [
                    // The roll was lost: the consolations.
                    IfWears<ActionStartingTriggeredEffectContext>(Self, CrossedFingers,
                        ForTheWitch<ActionStartingTriggeredEffectContext>(her =>
                            new GainBlockNode<ActionStartingTriggeredEffectContext>(
                                her, StacksOf<ActionStartingTriggeredEffectContext>(Self, CrossedFingers)))),
                    IfWears<ActionStartingTriggeredEffectContext>(Self, BadPenny,
                        new ApplyStatusNode<ActionStartingTriggeredEffectContext>(
                            Self, new StatusDefinitionId(WitchKeywords.Misfortune),
                            StacksOf<ActionStartingTriggeredEffectContext>(Self, BadPenny))),
                    LostRare(),
                ])),
            new RemoveStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(CrossedFingers)),
            new RemoveStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(BadPenny)),
            new RemoveStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(MagpiesLuck)),
            .. SpentAfterRollRare(),
        ]);

    // ── the statuses ──────────────────────────────────────────────────────────────────────────────────────

    // Dying, it hands its Hexed on to the healthiest enemy left: half of it (one stack of the mark), or all of it
    // (two). The new bearer starts its own count.
    private static StatusData DeadMansHexMark()
    {
        var dead = CombatantTargetSelectors.SourceIncludingDowned;
        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.HighestHealth(OtherEnemies(dead)),
                new StatusDefinitionId(WitchKeywords.Hexed),
                new DivideExpression<CombatantDownedTriggeredEffectContext>(
                    new MultiplyExpression<CombatantDownedTriggeredEffectContext>(
                        StacksOf<CombatantDownedTriggeredEffectContext>(dead, WitchKeywords.Hexed),
                        new MinExpression<CombatantDownedTriggeredEffectContext>(
                            StacksOf<CombatantDownedTriggeredEffectContext>(dead, DeadMansHex),
                            new ConstantExpression<CombatantDownedTriggeredEffectContext>(2))),
                    new ConstantExpression<CombatantDownedTriggeredEffectContext>(2))));

        return EnemyMark(DeadMansHex, "Dead Man's Hex",
            "When this character dies, the healthiest other enemy takes on half its Hexed (all of it at 2).",
            [Trigger(program, nameof(TriggerEvent.Downed))]);
    }

    // Every enemy action that strikes her this turn is answered; her next turn takes the mark away.
    private static StatusData Retaliation(
        string id, string name, string description,
        Func<ICombatantTargetSelector, ICombatExpression<ActionResolvedTriggeredEffectContext, int>,
            IEffectNode<ActionResolvedTriggeredEffectContext>> answer)
    {
        var answerProgram = new EffectProgram<ActionResolvedTriggeredEffectContext>(
            new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                new AndExpression<ActionResolvedTriggeredEffectContext>(
                    new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(),
                    new NotExpression<ActionResolvedTriggeredEffectContext>(
                        new TargetHasStatusExpression<ActionResolvedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(WitchKeywords.Cauldron)))),
                new ForEachTargetEffectNode<ActionResolvedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                    answer(Self, StacksOf<ActionResolvedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, id)))));

        return Mark(id, name, description,
        [
            Trigger(answerProgram, nameof(TriggerEvent.ActionResolved), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Self, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnStarted)),
        ]);
    }

    // "The first time each turn you apply <status> to an enemy": heard on its arrival AND on its landing on top of
    // what was there, because "apply" means both. The latch is set before the answer, so an answer that applies
    // the same status is not heard as a second first time.
    private static StatusData FirstEachTurn(
        string id, string name, string status, CounterId latch, bool spread, string description)
    {
        EffectProgram<TContext> Program<TContext>() where TContext : class
        {
            var wearer = CombatantTargetSelectors.IterationTarget;
            var enemy = CombatantTargetSelectors.EventTarget;
            var amount = StacksOf<TContext>(wearer, id);
            IEffectNode<TContext> answer = spread
                ? new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.HighestHealth(CombatantTargetSelectors.Except(OtherEnemies(enemy), Witch)),
                    new StatusDefinitionId(status), amount)
                : new ApplyStatusNode<TContext>(enemy, new StatusDefinitionId(status), amount);
            return new EffectProgram<TContext>(
                new ForEachTargetEffectNode<TContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
                    new ConditionalEffectNode<TContext>(
                        new AndExpression<TContext>(
                            new AndExpression<TContext>(
                                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(status)),
                                new NotExpression<TContext>(
                                    new TargetHasStatusExpression<TContext>(enemy, new StatusDefinitionId(WitchKeywords.Cauldron)))),
                            new ComparisonExpression<TContext>(
                                new CombatantCounterExpression<TContext>(wearer, latch),
                                ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                        new CausalSequenceEffectNode<TContext>(
                        [
                            new SetCombatantCounterNode<TContext>(wearer, latch, new ConstantExpression<TContext>(1), relative: false),
                            answer,
                        ]))));
        }

        return Rite(id, name, description,
        [
            Trigger(Program<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(Program<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Self, latch, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
                nameof(TriggerEvent.TurnStarted)),
        ]);
    }

    // Magpie's Luck's pay: cards with the opening hand of her next turn.
    private static StatusData MagpieDrawStatus() =>
        Mark(MagpieDraw, "Magpie's Luck", "Draw this many more cards at the start of your next turn.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                        Self, StacksOf<CardsDrawnTriggeredEffectContext>(Self, MagpieDraw)),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(Self, new StatusDefinitionId(MagpieDraw)),
                ])), nameof(TriggerEvent.CardsDrawn)),
        ]);

    // Proper Supper: the meal is eaten at the top of her next turn — Hearth healing, so only what this fight took.
    private static StatusData ProperSupperStatus() =>
        Mark(ProperSupper, "Proper Supper", "At the start of your next turn, heal this much HP lost this combat.",
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    Heal(Self, StacksOf<TurnStartedTriggeredEffectContext>(Self, ProperSupper)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Self, new StatusDefinitionId(ProperSupper)),
                ])), nameof(TriggerEvent.TurnStarted)),
        ]);

    // Midwife's Hands: the first time a blow leaves her at or below a third of her HP, she is tended — once.
    private static StatusData MidwifesHandsRite() =>
        Rite(MidwifesHands, "Midwife's Hands",
            "The first time you fall to a third of your HP or below this combat, heal this much HP lost this combat.",
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new MultiplyExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(3)),
                        ComparisonOperator.LessOrEqual,
                        new CombatantMaxHealthExpression<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        Heal(CombatantTargetSelectors.EventTarget,
                            StacksOf<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, MidwifesHands)),
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(MidwifesHands)),
                    ]))), nameof(TriggerEvent.DamageTaken)),
        ]);

    // Tell the Bees: a death in the house is told, and she is a little mended — once a turn.
    private static StatusData TellTheBeesRite()
    {
        var wearer = CombatantTargetSelectors.IterationTarget;
        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new NotExpression<CombatantDownedTriggeredEffectContext>(
                    new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(WitchKeywords.Cauldron))),
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(TellTheBees)),
                    new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                        new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                            new CombatantCounterExpression<CombatantDownedTriggeredEffectContext>(wearer, BeesLatch),
                            ComparisonOperator.Equal, new ConstantExpression<CombatantDownedTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                        [
                            new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(
                                wearer, BeesLatch, new ConstantExpression<CombatantDownedTriggeredEffectContext>(1), relative: false),
                            Heal(wearer, StacksOf<CombatantDownedTriggeredEffectContext>(wearer, TellTheBees)),
                        ])))));

        return Rite(TellTheBees, "Tell the Bees",
            "Once each turn, when an enemy dies, heal this much HP lost this combat.",
        [
            Trigger(program, nameof(TriggerEvent.Downed), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Self, BeesLatch, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
                nameof(TriggerEvent.TurnStarted)),
        ]);
    }

    // Her habits: at the end of each of her turns, an Adder in the Sleeve still in her hand has waited a turn. One
    // count however many copies wait — the count is hers, not the card's, and playing one starts it again.
    private static StatusData HabitsStatus() => new()
    {
        Id = Habits,
        NameKey = "Hedge Habits",
        DescriptionKey = "What waits in her hand grows.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(new SequenceEffectNode<TurnEndedTriggeredEffectContext>([
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(Self, SleeveWaited,
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                            Self, CardZone.Hand, new TagId(SleeveTag)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)), relative: true),
                WolfWaited(),
            ])), nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    // The Witch, read from anywhere: the one who carries the cauldron.
    private static ICombatantTargetSelector Witch =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(WitchKeywords.Cauldron));

    // Something done to her from an enemy's own event: found by her cauldron and done inside the loop, because
    // a single-body effect will not take a selector that could in principle name several.
    private static IEffectNode<TContext> ForTheWitch<TContext>(Func<ICombatantTargetSelector, IEffectNode<TContext>> effect)
        where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Witch, effect(CombatantTargetSelectors.IterationTarget));

    // Every living enemy but this one.
    private static ICombatantTargetSelector OtherEnemies(ICombatantTargetSelector one) =>
        CombatantTargetSelectors.Except(
            CombatantTargetSelectors.Except(CombatantTargetSelectors.AllAliveCombatants, Witch), one);

    private static ICombatExpression<TContext, int> StacksOf<TContext>(ICombatantTargetSelector who, string status)
        where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(status));

    private static IEffectNode<TContext> IfWears<TContext>(
        ICombatantTargetSelector who, string status, IEffectNode<TContext> then) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(status)), then);

    // Hearth healing from a rule: HP lost this combat only (§3.4).
    private static IEffectNode<TContext> Heal<TContext>(ICombatantTargetSelector who, ICombatExpression<TContext, int> amount)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>([Drippings(who, amount), CappedHeal(who, amount)]);

    private static IEffectNode<TContext> CappedHeal<TContext>(ICombatantTargetSelector who, ICombatExpression<TContext, int> amount)
        where TContext : class =>
        new HealNode<TContext>(who,
            new MinExpression<TContext>(amount,
                new MaxExpression<TContext>(new ConstantExpression<TContext>(0),
                    new SubtractExpression<TContext>(
                        new CombatantCounterExpression<TContext>(who, WitchKeywords.CombatStartHp),
                        new CombatantCurrentHealthExpression<TContext>(who)))));

    private static StatusData EnemyMark(
        string id, string name, string description, IReadOnlyList<StatusTriggerData>? triggers = null) =>
        Mark(id, name, description, triggers ?? []) with { Polarity = StatusPolarity.Debuff };

    private static StatusData Rite(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) =>
        Mark(id, name, description, triggers) with { Polarity = StatusPolarity.Neutral };
}
