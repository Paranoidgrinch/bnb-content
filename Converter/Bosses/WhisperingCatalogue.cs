using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Whispering Catalogue (Act II boss, 258 HP) ────────────────────────────────────────────────────────
//
// "The Catalogue describes what the player is becoming."
//
// It mirrors you. Every Deed you play this turn it writes down as 3 damage, every Working as 4 Block, and on its
// turn it does exactly that — and if you played nothing at all, it hits you for 10. Both records stand on it as
// you play, so the mirror is always readable before it moves.
//
// REWORKED 2026-11-03 (user, Act II trial: "der letzte Boss hatte gefühlt keine spannende Identität"): the Turn
// Records, predictions, Authority, Contradictions, Established Entries and both later phases are gone.
public static class WhisperingCatalogue
{
    public const string EnemyId = "whispering_catalogue_boss";

    public const string TheCatalogueId = "the_whispering_catalogue";
    public const string MirroredBlowsId = "mirrored_blows";
    public const string MirroredGuardId = "mirrored_guard";

    public const int DamagePerDeed = 3;
    public const int BlockPerWorking = 4;
    public const int IdleDamage = 10;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    public static IEnumerable<StatusData> Statuses() =>
    [
        Status(TheCatalogueId, "The Whispering Catalogue",
            "It mirrors you: each Deed you play this turn is 3 damage to you, each Working 4 Block for it. Play nothing: 10 damage.",
            usesStacks: false, triggers: [TheCatalogueForgets()]),
        Status(MirroredBlowsId, "Mirrored Blows", "On its turn it deals you this much damage.", usesStacks: true, triggers: []),
        Status(MirroredGuardId, "Mirrored Guard", "On its turn it gains this much Block.", usesStacks: true, triggers: []),
    ];

    // Its record is of the turn just played: at the end of its own turn the page is blank again.
    private static StatusTriggerData TheCatalogueForgets()
    {
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(MirroredBlowsId)),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Self, new StatusDefinitionId(MirroredGuardId)),
        ]));
        return new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
            program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // Every Deed and every Working the player plays goes on the record, on every Catalogue standing.
    public static IReadOnlyList<EncounterTriggerData> Triggers()
    {
        var catalogues = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(TheCatalogueId));
        IEffectNode<CardPlayedTriggeredEffectContext> Record(string tag, string status, int amount) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(tag)),
                new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(catalogues,
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(status),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(amount))));
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Record(CardAuthoring.DeedTag, MirroredBlowsId, DamagePerDeed),
            Record(CardAuthoring.WorkingTag, MirroredGuardId, BlockPerWorking),
        ]));
        return [new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()))];
    }

    // One action, every turn: the mirror. A turn with nothing played is answered with 10.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "read_you_back" => Mirror(),
        _ => null,
    };

    private static EffectProgram<EnemyActionContext> Mirror()
    {
        var blows = new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(MirroredBlowsId));
        var guard = new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(MirroredGuardId));
        return new(new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new AddExpression<EnemyActionContext>(blows, guard),
                ComparisonOperator.Equal, new ConstantExpression<EnemyActionContext>(0)),
            new DealDamageNode<EnemyActionContext>(Across, new ConstantExpression<EnemyActionContext>(IdleDamage)),
            @else: new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(blows, ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
                    new DealDamageNode<EnemyActionContext>(Across, blows)),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(guard, ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
                    new GainBlockNode<EnemyActionContext>(Self, guard)),
            ])));
    }

    private static StatusData Status(
        string id, string name, string description, bool usesStacks, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = usesStacks,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
