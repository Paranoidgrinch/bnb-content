using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The rares' marks and Rites (hedge_witch_master.md §13): the ones that bend her own rules — the third night, the
// roll, the pot, the heal cap. Like the uncommons', they answer from inside her keywords (OnBurst, AfterRoll) or
// from inside her cauldron (WitchActions), never by re-implementing them.
public static partial class WitchRules
{
    // ── enemy marks ───────────────────────────────────────────────────────────────────────────────────────
    public const string HareRunsLast = "hare_runs_last";       // its burst this turn is followed by this much damage
    public const string LastBreathHex = "last_breath_hex";     // dying, it passes on all its Hexed AND its count
    public const string Loaded = "loaded_knucklebones";        // its next roll is rolled twice
    public const string SevenYears = "seven_years_bad_luck";   // until a roll lands, each lost one leaves half
    public const string BlackCatSatDown = "black_cat_sat_down"; // its next roll cannot fail

    // ── her own marks and Rites ───────────────────────────────────────────────────────────────────────────
    public const string HexInTheRafters = "hex_in_the_rafters";
    public const string BaneRoot = "bane_root";
    public const string ShutTheLid = "shut_the_lid";
    public const string WinterBark = "winter_bark";
    public const string Ironwood = "ironwood";
    public const string KeepTheDrippings = "keep_the_drippings";
    public const string NeverWashThePot = "never_wash_the_pot";
    public const string NinthLife = "ninth_life";
    public const string WolfWaits = "wolf_waits";
    public const string WolfTag = "ing_wolf_at_the_door";

    public static CounterId Rolled => new("misfortune_rolled");
    private static CounterId RaftersLatch => new("hex_in_the_rafters_used");
    private static CounterId IronwoodLatch => new("ironwood_used");
    private static CounterId NinthLifeLatch => new("ninth_life_used");

    private static IReadOnlyList<StatusData> RareRules() =>
    [
        EnemyMark(HareRunsLast, "The Hare Runs Last", "When this character's Hex bursts this turn, it takes this much damage after."),
        LastBreathHexMark(),
        EnemyMark(Loaded, "Loaded Knucklebones", "This character's next Misfortune roll is rolled twice; either may land."),
        EnemyMark(SevenYears, "Seven Years' Bad Luck", "Until its Misfortune lands, every roll it wins leaves half the Misfortune behind."),
        EnemyMark(BlackCatSatDown, "The Black Cat Sat Down", "This character's next Misfortune roll cannot fail."),
        Rite(HexInTheRafters, "Hex in the Rafters",
            "The first Hex burst each enemy turn gives every other enemy this much Hexed.",
            [ClearLatchAtHerTurn(RaftersLatch)]),
        Rite(BaneRoot, "Bane-Root", "After a Hex bursts, that enemy gains this much Hexed.", []),
        Mark(ShutTheLid, "Shut the Lid",
            "Until your next turn the cauldron shelters you however full it is — and nothing goes in or comes out.",
            [Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Self, new StatusDefinitionId(ShutTheLid))),
                nameof(TriggerEvent.TurnStarted))]),
        WinterBarkMark(),
        IronwoodRite(),
        Rite(KeepTheDrippings, "Keep the Drippings",
            "Hearth healing that the combat-heal cap would waste becomes Ward Wax instead.", []),
        Rite(NeverWashThePot, "Never Wash the Pot", "After each Brew, keep one of its ingredients in the cauldron.", []),
        Rite(NinthLife, "Ninth Life",
            "After the first Misfortune that lands this combat, that enemy is left this much Misfortune.", []),
        WolfWaitsStatus(),
    ];

    // ── what her keywords call ────────────────────────────────────────────────────────────────────────────

    // Inside the burst, after the uncommons' answers. Self is the enemy that burst.
    private static IEffectNode<TurnEndedTriggeredEffectContext> OnBurstRare() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            IfWears<TurnEndedTriggeredEffectContext>(Self, HareRunsLast,
                new DealDamageNode<TurnEndedTriggeredEffectContext>(Self,
                    StacksOf<TurnEndedTriggeredEffectContext>(Self, HareRunsLast))),
            ForTheWitch<TurnEndedTriggeredEffectContext>(her =>
                IfWears<TurnEndedTriggeredEffectContext>(her, BaneRoot,
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(WitchKeywords.Hexed),
                        StacksOf<TurnEndedTriggeredEffectContext>(her, BaneRoot)))),
            ForTheWitch<TurnEndedTriggeredEffectContext>(her =>
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(her, new StatusDefinitionId(HexInTheRafters)),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(her, RaftersLatch),
                            ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            her, RaftersLatch, new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                        new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(OtherEnemies(Self),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(WitchKeywords.Hexed),
                                // Inside this loop "the one iterated" is the enemy, so she is named outright.
                                StacksOf<TurnEndedTriggeredEffectContext>(TheWitch, HexInTheRafters))),
                    ]))),
        ]);

    // Whether the roll lands at all: a sat-down cat, or the dice — once, or twice when loaded.
    public static ICombatExpression<ActionStartingTriggeredEffectContext, bool> Lands(
        Func<ICombatExpression<ActionStartingTriggeredEffectContext, bool>> roll) =>
        new OrExpression<ActionStartingTriggeredEffectContext>(
            new TargetHasStatusExpression<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(BlackCatSatDown)),
            new OrExpression<ActionStartingTriggeredEffectContext>(roll(),
                new AndExpression<ActionStartingTriggeredEffectContext>(
                    new TargetHasStatusExpression<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(Loaded)),
                    roll())));

    // How much Misfortune was rolled, written down before it is spent.
    public static IEffectNode<ActionStartingTriggeredEffectContext> NoteRolled() =>
        new SetCombatantCounterNode<ActionStartingTriggeredEffectContext>(Self, Rolled,
            StacksOf<ActionStartingTriggeredEffectContext>(Self, WitchKeywords.Misfortune), relative: false);

    private static IEffectNode<ActionStartingTriggeredEffectContext> LandedRare() =>
        new SequenceEffectNode<ActionStartingTriggeredEffectContext>(
        [
            new RemoveStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(SevenYears)),
            ForTheWitch<ActionStartingTriggeredEffectContext>(her =>
                new ConditionalEffectNode<ActionStartingTriggeredEffectContext>(
                    new AndExpression<ActionStartingTriggeredEffectContext>(
                        new TargetHasStatusExpression<ActionStartingTriggeredEffectContext>(her, new StatusDefinitionId(NinthLife)),
                        new ComparisonExpression<ActionStartingTriggeredEffectContext>(
                            new CombatantCounterExpression<ActionStartingTriggeredEffectContext>(her, NinthLifeLatch),
                            ComparisonOperator.Equal, new ConstantExpression<ActionStartingTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<ActionStartingTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<ActionStartingTriggeredEffectContext>(
                            her, NinthLifeLatch, new ConstantExpression<ActionStartingTriggeredEffectContext>(1), relative: false),
                        new ApplyStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(WitchKeywords.Misfortune),
                            StacksOf<ActionStartingTriggeredEffectContext>(her, NinthLife)),
                    ]))),
        ]);

    private static IEffectNode<ActionStartingTriggeredEffectContext> LostRare() =>
        IfWears<ActionStartingTriggeredEffectContext>(Self, SevenYears,
            new ApplyStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(WitchKeywords.Misfortune),
                new MaxExpression<ActionStartingTriggeredEffectContext>(new ConstantExpression<ActionStartingTriggeredEffectContext>(1),
                    new DivideExpression<ActionStartingTriggeredEffectContext>(
                        new CombatantCounterExpression<ActionStartingTriggeredEffectContext>(Self, Rolled),
                        new ConstantExpression<ActionStartingTriggeredEffectContext>(2)))));

    private static IReadOnlyList<IEffectNode<ActionStartingTriggeredEffectContext>> SpentAfterRollRare() =>
    [
        new RemoveStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(Loaded)),
        new RemoveStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(BlackCatSatDown)),
    ];

    // ── the statuses ──────────────────────────────────────────────────────────────────────────────────────

    // Dying, it hands ALL its Hexed and where its count stood to the healthiest enemy left.
    private static StatusData LastBreathHexMark()
    {
        var dead = CombatantTargetSelectors.SourceIncludingDowned;
        var heir = CombatantTargetSelectors.HighestHealth(OtherEnemies(dead));
        // A plain sequence: both steps read the dead at once, before it leaves the field.
        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new SequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(heir, WitchKeywords.ThreefoldStep,
                    new CombatantCounterExpression<CombatantDownedTriggeredEffectContext>(dead, WitchKeywords.ThreefoldStep),
                    relative: false),
                new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(heir, new StatusDefinitionId(WitchKeywords.Hexed),
                    StacksOf<CombatantDownedTriggeredEffectContext>(dead, WitchKeywords.Hexed)),
            ]));

        return EnemyMark(LastBreathHex, "Last-Breath Hex",
            "When this character dies, the healthiest other enemy takes on all its Hexed and its Threefold count.",
            [Trigger(program, nameof(TriggerEvent.Downed))]);
    }

    // When the round closes, what Block the enemy turn left her — up to this much — is kept as Ward Wax.
    private static StatusData WinterBarkMark()
    {
        var wearer = CombatantTargetSelectors.IterationTarget;
        var program = new EffectProgram<RoundEndedTriggeredEffectContext>(
            new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(WinterBark)),
                new CausalSequenceEffectNode<RoundEndedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<RoundEndedTriggeredEffectContext>(wearer, new StatusDefinitionId(Cards.Keywords.WardWax),
                        new MinExpression<RoundEndedTriggeredEffectContext>(
                            StacksOf<RoundEndedTriggeredEffectContext>(wearer, WinterBark),
                            new CombatantDefensivePoolExpression<RoundEndedTriggeredEffectContext>(
                                wearer, StandardCombatIds.BlockDefensivePool))),
                    new RemoveStatusNode<RoundEndedTriggeredEffectContext>(wearer, new StatusDefinitionId(WinterBark)),
                ])));

        return Mark(WinterBark, "Winter Bark", "After the enemy turn, up to this much of your unused Block becomes Ward Wax.",
            [Trigger(program, nameof(TriggerEvent.RoundEnded), StatusTriggerScope.Anywhere)]);
    }

    // The first blow each enemy turn that gets through her Block leaves her this much Ward Wax.
    private static StatusData IronwoodRite() =>
        Rite(Ironwood, "Ironwood", "The first time each turn an attack gets through your Block, gain this much Ward Wax.",
        [
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                            ComparisonOperator.Greater, new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget, IronwoodLatch),
                            ComparisonOperator.Equal, new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                            IronwoodLatch, new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(Cards.Keywords.WardWax),
                            StacksOf<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, Ironwood)),
                    ]))), nameof(TriggerEvent.DamageTaken)),
            ClearLatchAtHerTurn(IronwoodLatch),
        ]);

    // Wolf at the Door: each turn it waits in her hand it costs 1 less, to nothing; playing it starts again.
    private static StatusData WolfWaitsStatus() => new()
    {
        Id = WolfWaits,
        NameKey = "Wolf at the Door",
        DescriptionKey = "Wolf at the Door costs 1 less for each stack.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddPerStack, -1,
                RestrictDamageKind: null, RestrictSourceCardTag: WolfTag),
        ],
    };

    // The habit that feeds it: at the end of her turn, a wolf still in her hand has waited (at most twice — it
    // costs two).
    private static IEffectNode<TurnEndedTriggeredEffectContext> WolfWaited() =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(Self, CardZone.Hand, new TagId(WolfTag)),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    StacksOf<TurnEndedTriggeredEffectContext>(Self, WolfWaits),
                    ComparisonOperator.Less, new ConstantExpression<TurnEndedTriggeredEffectContext>(2))),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(WolfWaits),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)));

    // The Witch as one body, for a read made where a loop has taken over "the one iterated".
    private static ICombatantTargetSelector TheWitch => CombatantTargetSelectors.FirstTarget(Witch);

    private static StatusTriggerData ClearLatchAtHerTurn(CounterId latch) =>
        Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Self, latch, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
            nameof(TriggerEvent.TurnStarted));

    // ── what her cauldron and her healing ask (WitchActions, WitchCards) ──────────────────────────────────

    // Keep the Drippings: the part of a Hearth heal the cap would waste, as Ward Wax — asked BEFORE the heal.
    public static IEffectNode<TContext> Drippings<TContext>(ICombatantTargetSelector who, ICombatExpression<TContext, int> amount)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(KeepTheDrippings)),
            new ApplyStatusNode<TContext>(who, new StatusDefinitionId(Cards.Keywords.WardWax),
                new MaxExpression<TContext>(new ConstantExpression<TContext>(0),
                    new SubtractExpression<TContext>(amount,
                        new MaxExpression<TContext>(new ConstantExpression<TContext>(0),
                            new SubtractExpression<TContext>(
                                new CombatantCounterExpression<TContext>(who, WitchKeywords.CombatStartHp),
                                new CombatantCurrentHealthExpression<TContext>(who)))))));
}
