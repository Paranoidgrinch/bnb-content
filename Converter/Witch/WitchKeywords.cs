using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's keyword substrate (source-data/design/hedge_witch_master.md §2–§6): Hexed and its Threefold
// cycle, Misfortune, and the character's own standing status — the cauldron — which every fight of hers opens
// with (RunStart.CombatStatuses). Numbers marked BALANCE DRAFT in the canon are drafts here too.
public static class WitchKeywords
{
    public const string Hexed = "hexed";
    public const string Misfortune = "misfortune";
    public const string Cauldron = "hedge_witch_cauldron";   // her standing rules, worn every fight
    public const string FreeIngredient = "free_ingredient";  // "the first ingredient each turn costs nothing"

    // Hexed's Threefold step, kept on the hexed enemy.
    public static CounterId ThreefoldStep => new("threefold_step");

    // The HP she began the fight on — Hearth healing may restore up to it and no further (§3.4).
    public static CounterId CombatStartHp => new("combat_start_hp");

    // How many Brews this turn — read by rules that reward the first (Greasy Ladle).
    public static CounterId BrewsThisTurn => new("brews_this_turn");

    // §5: 1 stack = 5 %, normal cap 60 % — BALANCE DRAFT.
    public const int MisfortunePercentPerStack = 5;
    public const int MisfortuneCapStacks = 12;

    // §4: the burst is 3 × the current stacks every third enemy turn end — BALANCE DRAFT is the canon's own.
    public const int ThreefoldMultiplier = 3;

    public static IReadOnlyList<StatusData> All() =>
        [HexedStatus(), MisfortuneStatus(), CauldronStatus(), FreeIngredientStatus()];

    // ── Hexed: Threefold Hex (§4) ─────────────────────────────────────────────────────────────────────────
    //
    // "At the end of that enemy's turn: advance threefold_step by 1; at 3 the enemy loses HP equal to 3 × its
    // current Hexed; reset to 0. Hexed does not decrease. More Hexed does not reset the counter." Status HP loss,
    // not Attack damage: it ignores Block and is what "an enemy loses HP because of a Status" listens for.
    private static StatusData HexedStatus()
    {
        var self = CombatantTargetSelectors.Source;
        ICombatExpression<TurnEndedTriggeredEffectContext, int> step =
            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, ThreefoldStep);
        // CAUSAL: the step is read AFTER it has been advanced — a plain sequence asks the question before the
        // count it asks about has moved, and the third night never comes.
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, ThreefoldStep, new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        step, ComparisonOperator.GreaterOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(3)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new DealDamageNode<TurnEndedTriggeredEffectContext>(
                            self,
                            new MultiplyExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                    self, new StatusDefinitionId(Hexed)),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(ThreefoldMultiplier)),
                            ignoresBlock: true, kind: DamageKind.DamageOverTime),
                        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                            self, ThreefoldStep, new ConstantExpression<TurnEndedTriggeredEffectContext>(0),
                            relative: false),
                        WitchRules.OnBurst(),
                    ])),
                WitchRules.AfterTurn(),
            ]));

        return Status(Hexed, "Hexed", StatusPolarity.Debuff,
            $"At the end of every third turn of this character, it loses HP equal to {ThreefoldMultiplier} × its Hexed. " +
            "Hexed does not decay, and more Hexed does not reset the count.",
            [Trigger(program, nameof(TriggerEvent.TurnEnded))]);
    }

    // ── Misfortune (§5) ───────────────────────────────────────────────────────────────────────────────────
    //
    // "Immediately before that enemy's next scheduled action, roll once. Success: the entire action fails —
    // consumed, nothing resolves. After the roll, remove all Misfortune." 1 stack = 5 %, capped at 60 %.
    private static StatusData MisfortuneStatus()
    {
        var self = CombatantTargetSelectors.Source;
        ICombatExpression<ActionStartingTriggeredEffectContext, bool> Roll() =>
            new ComparisonExpression<ActionStartingTriggeredEffectContext>(
                new RandomBelowExpression<ActionStartingTriggeredEffectContext>(100),
                ComparisonOperator.Less,
                new MultiplyExpression<ActionStartingTriggeredEffectContext>(
                    new MinExpression<ActionStartingTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<ActionStartingTriggeredEffectContext>(
                            self, new StatusDefinitionId(Misfortune)),
                        new ConstantExpression<ActionStartingTriggeredEffectContext>(MisfortuneCapStacks)),
                    new ConstantExpression<ActionStartingTriggeredEffectContext>(MisfortunePercentPerStack)));
        IEffectNode<ActionStartingTriggeredEffectContext> Lands() =>
            new SequenceEffectNode<ActionStartingTriggeredEffectContext>(
            [
                new ApplyStatusNode<ActionStartingTriggeredEffectContext>(
                    self, StandardCombatIds.ActionFailsStatus,
                    new ConstantExpression<ActionStartingTriggeredEffectContext>(1)),
                WitchRules.RollLanded(true),
            ]);

        var program = new EffectProgram<ActionStartingTriggeredEffectContext>(
            new CausalSequenceEffectNode<ActionStartingTriggeredEffectContext>(
            [
                WitchRules.NoteRolled(),
                new ConditionalEffectNode<ActionStartingTriggeredEffectContext>(
                    // A sat-down black cat lands it outright; loaded knucklebones roll twice (WitchRules).
                    WitchRules.Lands(Roll),
                    Lands(),
                    // Lost — unless Black Cat's Collar throws the first lost roll of the fight again.
                    @else: new ConditionalEffectNode<ActionStartingTriggeredEffectContext>(WitchRules.CollarUnused,
                        new CausalSequenceEffectNode<ActionStartingTriggeredEffectContext>(
                        [
                            WitchRules.SpendCollar(),
                            new ConditionalEffectNode<ActionStartingTriggeredEffectContext>(Roll(), Lands(),
                                @else: WitchRules.RollLanded(false)),
                        ]),
                        @else: WitchRules.RollLanded(false))),
                new RemoveStatusNode<ActionStartingTriggeredEffectContext>(self, new StatusDefinitionId(Misfortune)),
                // …and what the cards riding on the roll make of it (WitchRules).
                WitchRules.AfterRoll(),
            ]));

        return Status(Misfortune, "Misfortune", StatusPolarity.Debuff,
            $"Before this character's next action, roll: {MisfortunePercentPerStack}% per stack (at most " +
            $"{MisfortuneCapStacks * MisfortunePercentPerStack}%) that the whole action fails. Then the Misfortune is gone.",
            [Trigger(program, nameof(TriggerEvent.ActionStarting))]);
    }

    // ── the cauldron: the Witch's own standing status (§2) ────────────────────────────────────────────────
    //
    // At the top of each of her turns: remember the HP the fight began on (once), clear this turn's tallies, and
    // make the first ingredient free.
    private static StatusData CauldronStatus()
    {
        var self = CombatantTargetSelectors.Source;
        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new SequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(self, CombatStartHp),
                        ComparisonOperator.Equal, new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        self, CombatStartHp, new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(self),
                        relative: false)),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    self, BrewsThisTurn, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new NotExpression<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            self, new StatusDefinitionId(FreeIngredient))),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        self, new StatusDefinitionId(FreeIngredient),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
            ]));

        return Status(Cauldron, "The Cauldron", StatusPolarity.Neutral,
            "Cards may go into the pot instead of being played; three in it can be Brewed. While it is empty, she " +
            "shelters behind it.",
            [Trigger(program, nameof(TriggerEvent.TurnStarted))]);
    }

    // "The first ingredient each turn costs 0 Energy" (§2.3, BALANCE DRAFT): a token worn until the first card
    // goes in. A price read off a STATUS rather than a once-per-turn claim, so asking what an ingredient costs —
    // which the screen does all the time — never spends the free one.
    private static StatusData FreeIngredientStatus() =>
        Status(FreeIngredient, "Free Ingredient", StatusPolarity.Buff,
            "The next card you put into the cauldron this turn costs no Energy.",
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.CardCost, PassiveModifierOperation.AddFlat, -1,
                    RestrictDamageKind: null, RestrictSourceCardTag: WitchActions.IngredientActionTag),
            ]);

    // ── shared ────────────────────────────────────────────────────────────────────────────────────────────

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static StatusData Status(
        string id, string name, StatusPolarity polarity, string description,
        IReadOnlyList<StatusTriggerData>? triggers = null,
        IReadOnlyList<PassiveModifierData>? passives = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = passives ?? [],
            Triggers = triggers ?? [],
        };
}
