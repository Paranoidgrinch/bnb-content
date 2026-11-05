using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// Act IV's carried things: the Feathers the act's events hand out, Ammit's Shadow (an event fight), and the run's ledger —
// Tomb Gold played in a fight is paid after it, and killing a Tomb Robber (or all Three Robbers) puts Tomb Gold in
// your deck (since 2026-11-04; it was a curse). Ammit and the Restless King are in ActFourEliteBodies.
// The normal bodies that used to live here (the Jackal, the Assessor, the Loot Bearer, the False-Door Finder) are
// in ActFourNormals.
public static partial class ActFour
{
    // ── ids ───────────────────────────────────────────────────────────────────────────────────────────────


    public const string AssessorEnemyId = "assessor_of_the_forty_two";
    public const string FeatherOfTruthId = "feather_of_truth";

    public const string TombCurseCardId = "tomb_curse";



    public const string CurseEnemyId = "curse_of_the_opened_tomb";

    public const string AmmitEnemyId = "ammit_devourer_of_the_unworthy";
    public const string AmmitRuleId = "the_devourer_waits";
    public const string UnworthyId = "unworthy";
    public const string AmmitShadowId = "ammits_shadow";

    // Counters the fight leaves behind for the run (CombatCounter), and the run's own.
    public static CounterId FeathersEarned => new("feathers_earned");
    public static CounterId FeathersSpent => new("feathers_spent");
    public static CounterId LootClaimed => new("tomb_loot_claimed");

    public static readonly RunCounterId FeathersCarried = new("labyrinth_feathers_carried");

    public const int MaxCarriedFeathers = 6;
    public const int DevourDamage = 40;
    public const int FeatherAppeasement = 12;

    private static ICombatantTargetSelector Parties { get; } =
        CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(LabyrinthBodyId));

    private static ICombatExpression<TContext, bool> Has<TContext>(ICombatantTargetSelector who, string statusId)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(statusId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> Standing<TContext>(ICombatantTargetSelector who)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(who),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> IsPlayersTurn<TContext>() where TContext : class =>
        Has<TContext>(CombatantTargetSelectors.Source, PassiveStatuses.ApplicantId);

    private static StatusTriggerData On<TContext>(
        string trigger, IEffectNode<TContext> body, StatusTriggerScope scope = StatusTriggerScope.Anywhere)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            new EffectProgram<TContext>(body), CombatJson.CreateOptions<TContext>()), scope);

    private static StatusData Face(
        string id, string name, string description, StatusPolarity polarity = StatusPolarity.Neutral,
        bool stacks = false, IReadOnlyList<PassiveModifierData>? modifiers = null,
        IReadOnlyList<StatusTriggerData>? triggers = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = stacks,
            Tags = [],
            PassiveModifiers = modifiers ?? [],
            Triggers = triggers ?? [],
        };

    private static ConstantExpression<TContext> N<TContext>(int value) where TContext : class => new(value);

    public static IReadOnlyList<StatusData> CutStatuses() =>
    [
        Face(FeatherOfTruthId, "Feather", "A turn found true, carried through the act from its events.",
            StatusPolarity.Buff, stacks: true),
        AmmitsShadow(),
    ];

    public static IReadOnlyList<CardData> CutCards() => [.. TombCurse(), .. TombGoldCards()];

    // The curse the loot carries: unplayable, and it buries you a little whenever it is still in hand.
    private static readonly CardAuthoring.BnbCard TombCurseCard = new(
        TombCurseCardId, "Tomb Curse", CardAuthoring.JunkTag, 0,
        "Unplayable. Still in hand at the end of your turn: 1 Entombed.",
        new CombatNodeModel("gainBlock", "source", CombatAmountSpec.FromConst(0)),
        Rarity: "junk", Tags: [CardAuthoring.UnplayableTag, "curse", TombCurseCardId]);

    private static IReadOnlyList<CardData> TombCurse() =>
    [
        TombCurseCard.Compile() with
        {
            LifecyclePrograms = new Dictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>>
            {
                [CardLifecycleTrigger.TurnEndInHand] = new(new ApplyStatusNode<CardLifecycleContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(EntombedId),
                    new ConstantExpression<CardLifecycleContext>(1))),
            },
        },
    ];

    // Ammit's Shadow, the event fight: a body with no actions of its own, which bites at the start of each of
    // its turns and is counted like her.
    private static StatusData AmmitsShadow()
    {
        var bite = new DealDamageNode<TurnStartedTriggeredEffectContext>(
            Applicant, N<TurnStartedTriggeredEffectContext>(9));
        return Face(AmmitShadowId, "Ammit's Shadow",
            "It bites for 9 at the start of each of its turns.",
            triggers: [On("TurnStarted", bite, StatusTriggerScope.Bearer)]);
    }

    // ── the run's side ────────────────────────────────────────────────────────────────────────────────────

    public const string FeatherLedgerProgramId = "labyrinth_feather_ledger";
    public const string TombLedgerProgramId = "labyrinth_tomb_ledger";

    // Installed with the act: what the fights wrote down is carried, and every combat door opens with it.
    public static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition> CutPrograms() =>
        new Dictionary<string, ITriggeredRunEffectDefinition>
        {
            [FeatherLedgerProgramId] = RunPrograms.On<CombatResolvedRunEvent>(
            [
                RunEffectTemplates.ChangeCounter(FeathersCarried, RunExpr.Subtract(
                    RunEventValues.CombatCounter(FeathersEarned.ToString()),
                    RunEventValues.CombatCounter(FeathersSpent.ToString()))),
                // Tomb Gold played in the fight is paid now.
                RunEffectTemplates.GainResource(StandardRunIds.Gold,
                    RunExpr.Multiply(RunEventValues.CombatCounter(TombGoldPlayed.ToString()), RunExpr.Const(TombGoldPay))),
            ]),
            [TombLedgerProgramId + "_tomb_gold"] = RunPrograms.When<CombatResolvedRunEvent>(
                new RunComparisonExpression(
                    RunEventValues.CombatCounter(LootClaimed.ToString()), RunComparisonOperator.GreaterThan, RunExpr.Const(0)),
                RunEffectTemplates.Literal(new AddCardToDeckRunEffect(new CardDefinitionId(TombGoldCardId)))),
            [TombLedgerProgramId + "_doors"] = DoorsCarry(),
        };

    public static IEnumerable<string> CutProgramIds() =>
    [
        FeatherLedgerProgramId, TombLedgerProgramId + "_tomb_gold", TombLedgerProgramId + "_doors",
    ];

    // Every combat door opens with the Feathers you carry.
    private static ITriggeredRunEffectDefinition DoorsCarry()
    {
        var feathers = RunExpr.Min(RunExpr.Counter(FeathersCarried), RunExpr.Const(MaxCarriedFeathers));

        IRunEffectTemplate Carry(IRunExpression<int> amount, string status, int k) =>
            RunEffectTemplates.Literal(new ConditionalRunEffect(
                RunExpr.Equal(amount, RunExpr.Const(k)),
                [Openings.NextCombat(new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(k),
                    StatusId: status))],
                []));

        return RunPrograms.When<NodeEnteredRunEvent>(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            [
                .. Enumerable.Range(1, MaxCarriedFeathers).Select(k => Carry(feathers, FeatherOfTruthId, k)),
            ]);
    }
}
