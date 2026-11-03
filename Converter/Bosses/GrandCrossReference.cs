using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Grand Cross-Reference (Act II boss, 68 + 72 + 76 HP) ──────────────────────────────────────────────
//
// "Nothing in the Archive means anything alone."
//
// Three monumental volumes, two of them linked at a time. The linked pair works together — whatever Block one of
// them raises, the other raises too — while the third stands Isolated: it does nothing, and it takes double
// damage. The isolation moves on to the next volume every 2 turns (Premise → Authority → Conclusion).
//
// REWORKED 2026-11-03 (user, Act II trial: "cooler Multi-Gegner, aber es gibt keine klare Synergie zwischen den
// Gegnern"): the untouchable Concordance engine and the final law chosen by kill order are gone, and with them
// the citations, the misfilings, the reversed passives and the isolated volume's growing Strength.
public static class GrandCrossReference
{
    public const string PremiseId = "gcr_premise";
    public const string AuthorityId = "gcr_authority";
    public const string ConclusionId = "gcr_conclusion";

    // Markers, so every rule can find the body it is about.
    public const string ThePremiseId = "the_premise";
    public const string TheAuthorityId = "the_authority";
    public const string TheConclusionId = "the_conclusion";
    public const string IsolatedId = "isolated_in_the_margin";

    public const int IsolatedTurns = 2;

    private const string LinkText =
        "Two volumes are linked and share their Block. The Isolated one does nothing and takes double damage.";

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // The order the isolation travels in.
    private static readonly string[] Order = [ThePremiseId, TheAuthorityId, TheConclusionId];

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(ThePremiseId, "The Premise"),
        Marker(TheAuthorityId, "The Authority"),
        Marker(TheConclusionId, "The Conclusion"),
        Isolated(),
    ];

    // ── Isolated in the Margin ────────────────────────────────────────────────────────────────────────────
    //
    // Its stacks are the turns it has left. At the end of its last one it passes to the next volume in the order
    // that is still standing (with only itself left, it stays).
    private static StatusData Isolated()
    {
        var isolated = new StatusDefinitionId(IsolatedId);
        IEffectNode<TurnEndedTriggeredEffectContext> PassTo(int from, int step)
        {
            if (step == Order.Length)
                return new NoOpEffectNode<TurnEndedTriggeredEffectContext>();
            var next = new StatusDefinitionId(Order[(from + step) % Order.Length]);
            var standing = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(next);
            return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CountTargetsExpression<TurnEndedTriggeredEffectContext>(standing),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Self, isolated),
                    new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.FirstTarget(standing),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, isolated,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(IsolatedTurns))),
                ]),
                @else: PassTo(from, step + 1));
        }

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(Self, isolated),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                Order.Select((marker, index) => (marker, index)).Aggregate(
                    (IEffectNode<TurnEndedTriggeredEffectContext>)new NoOpEffectNode<TurnEndedTriggeredEffectContext>(),
                    (rest, v) => new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(v.marker)),
                        PassTo(v.index, 1), @else: rest)),
                @else: new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    Self, isolated, new ConstantExpression<TurnEndedTriggeredEffectContext>(-1))));

        return new StatusData
        {
            Id = IsolatedId,
            NameKey = "Isolated",
            DescriptionKey = "Does nothing and takes double damage. Moves to the next volume when its turns run out.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 200),
            ],
            Triggers =
            [
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // An Isolated volume does nothing; a linked one acts, and the Block it raises its partner raises too.
    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            $"{PremiseId}.establish_the_initial_condition" => Linked(Block(6)),
            $"{PremiseId}.reject_the_unsupported_assumption" => Linked(Damage(14)),
            $"{PremiseId}.define_the_applicable_case" => Linked(Block(8)),
            $"{PremiseId}.restate_the_premise" => Linked(Damage(7)),

            $"{AuthorityId}.issue_the_binding_interpretation" => Linked(Damage(16)),
            $"{AuthorityId}.overrule_the_lesser_reading" => Linked(
                new ApplyStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(Keywords.Doubt), Const(2))),
            $"{AuthorityId}.cite_the_higher_authority" => Linked(Block(12)),
            $"{AuthorityId}.enforce_without_appeal" => Linked(Damage(21)),

            $"{ConclusionId}.draw_the_necessary_result" => Linked(Damage(9)),
            $"{ConclusionId}.conclude_from_missing_evidence" => Linked(Damage(13)),
            $"{ConclusionId}.preserve_the_result" => Linked(Block(14)),
            $"{ConclusionId}.file_the_result_elsewhere" => Linked(Damage(9)),
            _ => null,
        };

    private static EffectProgram<EnemyActionContext> Linked(IEffectNode<EnemyActionContext> body) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(Self, new StatusDefinitionId(IsolatedId)),
            new NoOpEffectNode<EnemyActionContext>(), @else: body));

    // Block for itself and for its linked partner: every volume that is not Isolated (the ally selector counts
    // the actor too).
    private static IEffectNode<EnemyActionContext> Block(int amount) =>
        new ForEachTargetEffectNode<EnemyActionContext>(CombatantTargetSelectors.AllAlliesOfSource,
            new ConditionalEffectNode<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(new TargetHasStatusExpression<EnemyActionContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(IsolatedId))),
                new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(amount))));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Across, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    private static StatusData Marker(string id, string name) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = LinkText,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
