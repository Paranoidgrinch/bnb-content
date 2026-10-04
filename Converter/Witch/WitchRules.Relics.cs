using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The in-combat rules of her relics (hedge_witch_master.md §14; plan W5), each the status the relic puts on her at
// every fight's start (RelicAuthoring.Rule). Most are a mark her own keywords and her cauldron ask about — the
// third night (OnBurst), the roll (AfterRoll), the pot (WitchActions) — so the relic never re-implements them.
// Numbers are drafts (the canon gives none): small, a common relic about a third of a card.
public static partial class WitchRules
{
    // ── the rules, by relic ───────────────────────────────────────────────────────────────────────────────
    public const string RowanPin = "rowan_pin_rule";
    public const string ThreeKnotCord = "three_knot_cord_rule";
    public const string FoundButton = "found_button_rule";
    public const string GreasyLadle = "greasy_ladle_rule";
    public const string IronTrivet = "iron_trivet_rule";
    public const string CrowsToe = "crows_toe_rule";
    public const string BeeswaxCup = "beeswax_cup_rule";
    public const string KnucklebonePair = "knucklebone_pair_rule";
    public const string FalseBottomPot = "false_bottom_pot_rule";
    public const string FirstBell = "first_bell_rule";
    public const string BlackCatsCollar = "black_cats_collar_rule";
    public const string GrandamsEmber = "grandams_ember_rule";
    public const string BoneStrainer = "bone_strainer_rule";
    public const string ApothecarysScale = "apothecarys_scale_rule";
    public const string YesterdaysJar = "yesterdays_jar_rule";
    public const string CatsEyeCoin = "cats_eye_coin_rule";
    public const string HerbWifesRack = "herb_wifes_rack_rule";
    public const string BlackSpoon = "black_spoon_rule";

    public const int RowanPinBlock = 3, ThreeKnotHexed = 3, FoundButtonBlock = 5, IronTrivetBlock = 6,
        CrowsToeDamage = 5, BeeswaxWax = 2, KnucklebonePairStacks = 2, FirstBellSteps = 1, CatsEyeGold = 5,
        CatsEyeCap = 15, HerbWifeHeal = 3;

    // Latches: per turn ones are cleared at her turn's start, per combat ones never.
    public static CounterId RowanPinLatch => new("rowan_pin_used");
    private static CounterId ThreeKnotLatch => new("three_knot_cord_used");
    private static CounterId FoundButtonLatch => new("found_button_used");
    private static CounterId CrowsToeLatch => new("crows_toe_used");
    private static CounterId BeeswaxLatch => new("beeswax_cup_used");
    private static CounterId PairLatch => new("knucklebone_pair_used");
    private static CounterId FirstBellLatch => new("first_bell_used");
    public static CounterId CollarLatch => new("black_cats_collar_used");
    public static CounterId EmberLatch => new("grandams_ember_used");
    public static CounterId ScaleLatch => new("apothecarys_scale_used");
    public static CounterId JarLatch => new("yesterdays_jar_used");
    public static CounterId RackLatch => new("herb_wifes_rack_used");
    public static CounterId BlackSpoonLatch => new("black_spoon_used");

    // What the fight leaves for the run to collect (Cat's-Eye Coin): read after a victory as event.combatCounter.
    public static CounterId CatsEyeTally => new("cats_eye_coin_tally");

    private static IReadOnlyList<StatusData> RelicRules() =>
    [
        RelicRule(RowanPin, "Rowan Pin", $"The first card into the cauldron each turn gives {RowanPinBlock} Block.",
            [ClearLatchAtHerTurn(RowanPinLatch)]),
        RelicRule(ThreeKnotCord, "Three-Knot Cord", $"The first Hex burst this combat leaves {ThreeKnotHexed} Hexed behind."),
        RelicRule(FoundButton, "Found Button", $"The first Misfortune roll lost this combat gives you {FoundButtonBlock} Block."),
        RelicRule(GreasyLadle, "Greasy Ladle", "The first Brew each turn draws a card."),
        IronTrivetRule(),
        CrowsToeRule(),
        BeeswaxCupRule(),
        RelicRule(KnucklebonePair, "Knucklebone Pair",
            $"The first Misfortune roll lost each turn leaves {KnucklebonePairStacks} Misfortune behind.",
            [ClearLatchAtHerTurn(PairLatch)]),
        RelicRule(FalseBottomPot, "False-Bottom Pot",
            "The cauldron takes a fourth card in reserve: it is not brewed, and it stays for the next pot."),
        FirstBellRule(),
        RelicRule(BlackCatsCollar, "Black Cat's Collar", "The first Misfortune roll lost this combat is rolled again."),
        RelicRule(GrandamsEmber, "Grandam's Ember",
            "Once this combat, Hearth healing may restore HP lost before the fight."),
        RelicRule(BoneStrainer, "Bone Strainer", "Dregs in a mixed Brew count as a family you name."),
        RelicRule(ApothecarysScale, "Apothecary's Scale",
            "The first Brew this combat of three different families gives its Energy back."),
        RelicRule(YesterdaysJar, "Yesterday's Jar",
            "After the first Brew this combat, the last ingredient stays in the cauldron."),
        RelicRule(CatsEyeCoin, "Cat's-Eye Coin",
            $"Every Misfortune that lands is worth {CatsEyeGold} Gold after the fight, at most {CatsEyeCap}."),
        RelicRule(BlackSpoon, "Black Spoon",
            "The first Hidden Recipe you brew each combat gives 1 Energy and draws a card."),
        RelicRule(HerbWifesRack, "Herb-Wife's Rack",
            $"The first Brew this combat with a Hearth ingredient heals {HerbWifeHeal} more HP lost this combat."),
    ];

    // ── what her keywords call ────────────────────────────────────────────────────────────────────────────

    // After a burst (OnBurst): Three-Knot Cord.
    private static IEffectNode<TurnEndedTriggeredEffectContext> OnBurstRelics() =>
        OnceFor<TurnEndedTriggeredEffectContext>(ThreeKnotCord, ThreeKnotLatch,
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(WitchKeywords.Hexed),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(ThreeKnotHexed)));

    // A roll that landed (AfterRoll): Cat's-Eye Coin keeps count.
    private static IEffectNode<ActionStartingTriggeredEffectContext> LandedRelics() =>
        ForTheWitch<ActionStartingTriggeredEffectContext>(her =>
            IfWears<ActionStartingTriggeredEffectContext>(her, CatsEyeCoin,
                new SetCombatantCounterNode<ActionStartingTriggeredEffectContext>(her, CatsEyeTally,
                    new ConstantExpression<ActionStartingTriggeredEffectContext>(1), relative: true)));

    // A roll that was lost (AfterRoll): Found Button, Knucklebone Pair.
    private static IEffectNode<ActionStartingTriggeredEffectContext> LostRelics() =>
        new SequenceEffectNode<ActionStartingTriggeredEffectContext>(
        [
            OnceFor<ActionStartingTriggeredEffectContext>(FoundButton, FoundButtonLatch,
                new GainBlockNode<ActionStartingTriggeredEffectContext>(TheWitch,
                    new ConstantExpression<ActionStartingTriggeredEffectContext>(FoundButtonBlock))),
            OnceFor<ActionStartingTriggeredEffectContext>(KnucklebonePair, PairLatch,
                new ApplyStatusNode<ActionStartingTriggeredEffectContext>(Self, new StatusDefinitionId(WitchKeywords.Misfortune),
                    new ConstantExpression<ActionStartingTriggeredEffectContext>(KnucklebonePairStacks))),
        ]);

    // Black Cat's Collar, asked by the roll itself when it was lost: the first such roll this combat is thrown again.
    public static ICombatExpression<ActionStartingTriggeredEffectContext, bool> CollarUnused =>
        new AndExpression<ActionStartingTriggeredEffectContext>(
            new TargetHasStatusExpression<ActionStartingTriggeredEffectContext>(TheWitch, new StatusDefinitionId(BlackCatsCollar)),
            new ComparisonExpression<ActionStartingTriggeredEffectContext>(
                new CombatantCounterExpression<ActionStartingTriggeredEffectContext>(TheWitch, CollarLatch),
                ComparisonOperator.Equal, new ConstantExpression<ActionStartingTriggeredEffectContext>(0)));

    public static IEffectNode<ActionStartingTriggeredEffectContext> SpendCollar() =>
        new SetCombatantCounterNode<ActionStartingTriggeredEffectContext>(TheWitch, CollarLatch,
            new ConstantExpression<ActionStartingTriggeredEffectContext>(1), relative: false);

    // "Once this <span>, if she wears <rule>": the latch is hers, set before the effect.
    private static IEffectNode<TContext> OnceFor<TContext>(string rule, CounterId latch, IEffectNode<TContext> effect)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new TargetHasStatusExpression<TContext>(TheWitch, new StatusDefinitionId(rule)),
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(TheWitch, latch),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
            new CausalSequenceEffectNode<TContext>(
            [
                new SetCombatantCounterNode<TContext>(TheWitch, latch, new ConstantExpression<TContext>(1), relative: false),
                effect,
            ]));

    // ── the rules with triggers of their own ──────────────────────────────────────────────────────────────

    // Iron Trivet: a Ready pot at the end of her turn gives Block for the enemy turn.
    private static StatusData IronTrivetRule() =>
        RelicRule(IronTrivet, "Iron Trivet", $"End your turn with the cauldron Ready: gain {IronTrivetBlock} Block.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(Self, CardZone.SetAsidePile),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(WitchActions.Slots)),
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(Self,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(IronTrivetBlock)))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // Crow's Toe: the first blow each turn she lands on an enemy whose third night is next adds a peck. The peck
    // is a hit of its own; the latch is set first, so it is not heard as another first blow.
    private static StatusData CrowsToeRule() =>
        RelicRule(CrowsToe, "Crow's Toe",
            $"The first time each turn you hit an enemy whose Hex bursts at the end of its next turn, deal {CrowsToeDamage} more.",
        [
            Trigger(new EffectProgram<DamageDealtTriggeredEffectContext>(
                new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                    new AndExpression<DamageDealtTriggeredEffectContext>(
                        new ComparisonExpression<DamageDealtTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageDealtTriggeredEffectContext>(Self, CrowsToeLatch),
                            ComparisonOperator.Equal, new ConstantExpression<DamageDealtTriggeredEffectContext>(0)),
                        new ComparisonExpression<DamageDealtTriggeredEffectContext>(
                            new CombatantCounterExpression<DamageDealtTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget, WitchKeywords.ThreefoldStep),
                            ComparisonOperator.GreaterOrEqual, new ConstantExpression<DamageDealtTriggeredEffectContext>(2))),
                    new CausalSequenceEffectNode<DamageDealtTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<DamageDealtTriggeredEffectContext>(Self, CrowsToeLatch,
                            new ConstantExpression<DamageDealtTriggeredEffectContext>(1), relative: false),
                        new DealDamageNode<DamageDealtTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                            new ConstantExpression<DamageDealtTriggeredEffectContext>(CrowsToeDamage)),
                    ]))), nameof(TriggerEvent.DamageDealt)),
            ClearLatchAtHerTurn(CrowsToeLatch),
        ]);

    // Beeswax Cup: the first heal each turn that actually mends her leaves a little Ward Wax.
    private static StatusData BeeswaxCupRule() =>
        RelicRule(BeeswaxCup, "Beeswax Cup", $"The first time each turn you are healed, gain {BeeswaxWax} Ward Wax.",
        [
            Trigger(new EffectProgram<HealedTriggeredEffectContext>(
                new ConditionalEffectNode<HealedTriggeredEffectContext>(
                    new AndExpression<HealedTriggeredEffectContext>(
                        new ComparisonExpression<HealedTriggeredEffectContext>(
                            new EventAmountExpression<HealedTriggeredEffectContext>(),
                            ComparisonOperator.Greater, new ConstantExpression<HealedTriggeredEffectContext>(0)),
                        new ComparisonExpression<HealedTriggeredEffectContext>(
                            new CombatantCounterExpression<HealedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, BeeswaxLatch),
                            ComparisonOperator.Equal, new ConstantExpression<HealedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<HealedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<HealedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                            BeeswaxLatch, new ConstantExpression<HealedTriggeredEffectContext>(1), relative: false),
                        new ApplyStatusNode<HealedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(Cards.Keywords.WardWax),
                            new ConstantExpression<HealedTriggeredEffectContext>(BeeswaxWax)),
                    ]))), nameof(TriggerEvent.Healed)),
            ClearLatchAtHerTurn(BeeswaxLatch),
        ]);

    // First Bell: the first enemy she hexes this combat starts its count a step ahead — heard on the Hexed arriving.
    private static StatusData FirstBellRule()
    {
        var wearer = CombatantTargetSelectors.IterationTarget;
        var enemy = CombatantTargetSelectors.EventTarget;
        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ForEachTargetEffectNode<StatusAppliedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(FirstBell)),
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(new StatusDefinitionId(WitchKeywords.Hexed)),
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantCounterExpression<StatusAppliedTriggeredEffectContext>(wearer, FirstBellLatch),
                            ComparisonOperator.Equal, new ConstantExpression<StatusAppliedTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<StatusAppliedTriggeredEffectContext>(wearer, FirstBellLatch,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(1), relative: false),
                        new SetCombatantCounterNode<StatusAppliedTriggeredEffectContext>(enemy, WitchKeywords.ThreefoldStep,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(FirstBellSteps), relative: true),
                    ]))));

        return RelicRule(FirstBell, "First Bell", "The first enemy you Hex this combat starts its Threefold count a step ahead.",
            [Trigger(program, nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere)]);
    }

    private static StatusData RelicRule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData>? triggers = null) =>
        Relics.RelicAuthoring.Rule(id, name, description, triggers ?? []);
}
