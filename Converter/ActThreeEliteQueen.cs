using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 5 — **Ant Queen of the Proper Line** (Queen 160 HP, three Bearers of 27).
//
// Three Line-Bearers walk in front of her. Hitting a Bearer while one ahead of it still stands breaks her law;
// while any Bearer stands she takes half damage; and she raises each fallen Bearer once.
//
// REWORKED 2026-11-03 (user, Act III pass): the Reconstruction Charges and their Claims, the Permitted Exception,
// the Claim-scaled line and the Wergild of her enforcement are gone. "Directly targets a Bearer" is still the
// card's own target, so an area effect names nobody and breaks nothing.
public static partial class ActThree
{
    public const string AntQueenEnemyId = "ant_queen_of_the_proper_line";
    public const string AntQueenId = "the_proper_line";
    public const string LineShieldId = "shielded_by_the_line";

    private const int BearerRespawnHealth = 18;

    // The three positions, in the order the law and every return reads them.
    private static readonly (string EnemyId, string Marker, string Name)[] Bearers =
    [
        ("first_line_bearer", "line_bearer_first", "First Line-Bearer"),
        ("second_line_bearer", "line_bearer_second", "Second Line-Bearer"),
        ("third_line_bearer", "line_bearer_third", "Third Line-Bearer"),
    ];

    // Worn by the Queen once she has raised that Bearer: each comes back once.
    private static string ReturnedId(string marker) => $"{marker}_returned";

    private static ICombatantTargetSelector Queen { get; } = Elite(AntQueenId);

    // The Bearer standing in that position; a position with nobody in it resolves to nobody.
    private static ICombatantTargetSelector Bearer(string marker) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(marker)));

    // …and the body that fell there, which only her raising reaches.
    private static ICombatantTargetSelector FallenBearer(string marker) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(marker), includeFallen: true));

    private static ICombatExpression<TContext, bool> Standing<TContext>(string marker)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(Bearer(marker)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> AnyBearerStanding<TContext>() where TContext : class =>
        Bearers.Select(b => Standing<TContext>(b.Marker)).Aggregate((a, b) => new OrExpression<TContext>(a, b));

    private static IEnumerable<StatusData> QueenStatuses() =>
    [
        TheProperLine(),
        new StatusData
        {
            Id = LineShieldId,
            NameKey = "Shielded by the Line",
            DescriptionKey = "While a Line-Bearer stands, the Queen takes half damage.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 50),
            ],
            Triggers = [],
        },
        .. Bearers.Select(b => Marker(b.Marker, b.Name,
            $"{b.Name} of the procession. Hitting it while a Bearer ahead of it stands breaks the Queen's law.")),
        .. Bearers.Select(b => Marker(ReturnedId(b.Marker), $"{b.Name} Raised", "The Queen has raised this Bearer once already.")),
    ];

    // The shield is the line: on while anybody walks in front of her, off the moment nobody does.
    private static IEffectNode<TContext> KeepTheShield<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            AnyBearerStanding<TContext>(),
            new ApplyStatusNode<TContext>(Queen, new StatusDefinitionId(LineShieldId), new ConstantExpression<TContext>(1)),
            @else: new RemoveStatusNode<TContext>(Queen, new StatusDefinitionId(LineShieldId)));

    // ── Local Law — Do Not Break the Line ─────────────────────────────────────────────────────────────────
    private static StatusData TheProperLine()
    {
        // In a card-played trigger the event target is the card's OWN target.
        var struck = CombatantTargetSelectors.EventTarget;

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> OutOfOrder(int index) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wearing<CardPlayedTriggeredEffectContext>(struck, Bearers[index].Marker),
                Enumerable.Range(0, index)
                    .Select(i => Standing<CardPlayedTriggeredEffectContext>(Bearers[i].Marker))
                    .Aggregate((a, b) => new OrExpression<CardPlayedTriggeredEffectContext>(a, b)));

        var law = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new OrExpression<CardPlayedTriggeredEffectContext>(OutOfOrder(1), OutOfOrder(2)),
            Breach<CardPlayedTriggeredEffectContext>(Queen));

        // A Bearer falls (or the turn turns): the shield follows the line. The Queen falling takes the
        // procession with her.
        var fallen = new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(AntQueenId)),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [.. Bearers.Select(b => (IEffectNode<CombatantDownedTriggeredEffectContext>)
                        new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                            Standing<CombatantDownedTriggeredEffectContext>(b.Marker),
                            new SetHealthNode<CombatantDownedTriggeredEffectContext>(
                                Bearer(b.Marker), new ConstantExpression<CombatantDownedTriggeredEffectContext>(0))))]),
                @else: KeepTheShield<CombatantDownedTriggeredEffectContext>()),
        ]);

        return Rule(AntQueenId, "Do Not Break the Line",
            "Hitting a Bearer while one ahead of it stands breaks her law. While a Bearer stands she takes half damage. She raises each fallen Bearer once.",
            [
                On("CardPlayed", law, StatusTriggerScope.Anywhere),
                On("Downed", fallen, StatusTriggerScope.Anywhere),
                On("TurnStarted", KeepTheShield<TurnStartedTriggeredEffectContext>(), StatusTriggerScope.Anywhere),
            ]);
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? QueenIntent(string enemyId, string intentId)
    {
        if (enemyId == AntQueenEnemyId)
            return TheQueenActs(intentId);

        var index = Array.FindIndex(Bearers, b => b.EnemyId == enemyId);
        return index < 0 ? null : ABearerActs(index, intentId);
    }

    // "Each enemy turn only the frontmost living Line-Bearer acts."
    private static EffectProgram<EnemyActionContext>? ABearerActs(int index, string intentId)
    {
        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            "carry_forward" => Blow(9),
            "hold_the_line" => new GainBlockNode<EnemyActionContext>(Queen, new ConstantExpression<EnemyActionContext>(7)),
            // A Bearer with a gap in front of it bites into it.
            "bite_out_of_order" => index == 0
                ? Blow(9)
                : new ConditionalEffectNode<EnemyActionContext>(Standing<EnemyActionContext>(Bearers[index - 1].Marker), Blow(9), Blow(13)),
            _ => null,
        };
        if (act is null)
            return null;
        if (index == 0)
            return new EffectProgram<EnemyActionContext>(act);

        var somebodyAhead = Enumerable.Range(0, index)
            .Select(i => Standing<EnemyActionContext>(Bearers[i].Marker))
            .Aggregate((a, b) => new OrExpression<EnemyActionContext>(a, b));
        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(somebodyAhead, new NoOpEffectNode<EnemyActionContext>(), act));
    }

    private static EffectProgram<EnemyActionContext>? TheQueenActs(string intentId)
    {
        var self = CombatantTargetSelectors.Source;
        var livingBearers = Bearers
            .Select(b => (ICombatExpression<EnemyActionContext, int>)new MinExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(1), new CombatantCurrentHealthExpression<EnemyActionContext>(Bearer(b.Marker))))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            // "12 +4 per living Bearer, max 24."
            "count_the_proper_order" => new DealDamageNode<EnemyActionContext>(Applicant,
                new AddExpression<EnemyActionContext>(new ConstantExpression<EnemyActionContext>(12),
                    new MultiplyExpression<EnemyActionContext>(new ConstantExpression<EnemyActionContext>(4), livingBearers))),
            "collect_the_queens_claim" => Blow(14),
            "royal_survey_of_the_line" => new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(14)),
            // The lowest fallen Bearer she has not raised before; with nobody to raise, she counts the line.
            "replace_the_fallen" => RaiseTheLowestFallen(),
            "royal_enforcement" => Blow(20),
            _ => null,
        };
        return act is null ? null : new EffectProgram<EnemyActionContext>(act);
    }

    private static IEffectNode<EnemyActionContext> RaiseTheLowestFallen(int from = 0)
    {
        if (from == Bearers.Length)
            return Blow(12);
        var bearer = Bearers[from];
        return new ConditionalEffectNode<EnemyActionContext>(
            new AndExpression<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(Standing<EnemyActionContext>(bearer.Marker)),
                new NotExpression<EnemyActionContext>(Wearing<EnemyActionContext>(Queen, ReturnedId(bearer.Marker)))),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                // Standing up first, and only then given something to stand on: setting health is a living-only
                // operation.
                new SetCombatantLifecycleStateNode<EnemyActionContext>(FallenBearer(bearer.Marker), CombatantLifecycleState.Alive),
                new SetHealthNode<EnemyActionContext>(Bearer(bearer.Marker), new ConstantExpression<EnemyActionContext>(BearerRespawnHealth)),
                new ApplyStatusNode<EnemyActionContext>(Queen, new StatusDefinitionId(ReturnedId(bearer.Marker)),
                    new ConstantExpression<EnemyActionContext>(1)),
                KeepTheShield<EnemyActionContext>(),
            ]),
            @else: RaiseTheLowestFallen(from + 1));
    }
}
