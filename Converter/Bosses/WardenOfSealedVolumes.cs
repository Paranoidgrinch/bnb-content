using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Bosses;

// ── The Warden of Sealed Volumes (Act II boss, 270 HP) ────────────────────────────────────────────────────
//
// "The card is still yours. The right to use it is not."
//
// Every turn, once your hand is drawn, the Warden takes one card of it into custody (the Banished pile, in
// sight), three at most. Each card it holds makes its attacks 3 stronger. Deal it 20 damage in one turn and
// it lets every one of them go, back into your hand.
//
// REWORKED 2026-11-03 (user, Act II trial: "macht nichts außer eine Handkarte wegnehmen"): the three Seals and
// their release conditions, the announcements, Total Lockdown, Final Review and the citation are gone.
public static class WardenOfSealedVolumes
{
    public const string EnemyId = "warden_of_sealed_volumes";

    public const string TheWardenId = "the_warden_of_sealed_volumes";
    public const string CustodyId = "warden_custody";
    public const string WardenRulesId = "warden_rules";
    public const string SealedMark = "sealed_by_the_warden";

    public const int CustodyMaximum = 3;
    public const int StrengthPerCard = 3;
    public const int ReleaseDamage = 20;   // in one turn

    // The health it has lost since its last turn began.
    private static CounterId TakenCounter => new("warden_taken");

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static ICombatantTargetSelector Wardens =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(TheWardenId));

    public static IEnumerable<StatusData> Statuses() => [TheWarden(), Custody(), Rules()];

    // ── The player's side: the sealing ────────────────────────────────────────────────────────────────────
    //
    // Once the hand is drawn, one card of it — at random — goes into custody, while the Warden holds fewer
    // than three. The card is marked first and then moved by its mark, because a random pick read twice would
    // be two different cards.
    private static StatusData Rules()
    {
        var player = Self;
        var held = new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.BanishedPile, mark: new TagId(SealedMark));
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    new TargetHasStatusExpression<CardsDrawnTriggeredEffectContext>(player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CountTargetsExpression<CardsDrawnTriggeredEffectContext>(Wardens),
                            ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                held, ComparisonOperator.Less, new ConstantExpression<CardsDrawnTriggeredEffectContext>(CustodyMaximum)),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.Hand),
                                ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))))),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        player, new RandomCardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand), new TagId(SealedMark)),
                    new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                        player,
                        new FirstMarkedCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(player, CardZone.Hand, new TagId(SealedMark)),
                        CardZone.BanishedPile),
                    new ForEachTargetEffectNode<CardsDrawnTriggeredEffectContext>(Wardens,
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(CustodyId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                ])));
        return Rule(WardenRulesId, "Sealed Volumes",
            "Each turn the Warden seals a card from your hand (3 at most). Deal it 20 damage in one turn to free them all.",
            [Watch("CardsDrawn", TheOpeningHand.Only(program))]);
    }

    // ── The Warden's side: the release ────────────────────────────────────────────────────────────────────
    //
    // Health it loses is counted from its own turn start; reaching 20 while it holds anything frees every card
    // it holds, into the hand. In a damage program the struck body is the event's target.
    private static StatusData TheWarden()
    {
        var warden = CombatantTargetSelectors.EventTarget;
        var player = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId));
        var blow = new EffectProgram<DamageReceivedTriggeredEffectContext>(new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
        [
            new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                warden, TakenCounter, new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(warden, TakenCounter),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<DamageReceivedTriggeredEffectContext>(ReleaseDamage)),
                    new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(warden, new StatusDefinitionId(CustodyId))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(player,
                        new ForEachCardInZoneNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, CardZone.BanishedPile,
                            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                            [
                                new MarkCardInstanceNode<DamageReceivedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    new IteratedCardExpression<DamageReceivedTriggeredEffectContext>(), new TagId(SealedMark), remove: true),
                                new MoveCardToZoneNode<DamageReceivedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    new IteratedCardExpression<DamageReceivedTriggeredEffectContext>(), CardZone.Hand),
                            ]),
                            markFilter: new TagId(SealedMark))),
                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(warden, new StatusDefinitionId(CustodyId)),
                ])),
        ]));
        var turn = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Self, TakenCounter, new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false));
        return Rule(TheWardenId, "The Warden of Sealed Volumes",
            "Each turn it seals a card from your hand (3 at most). Deal it 20 damage in one turn to free them all.",
            [Watch("DamageTaken", blow), Watch("TurnStarted", turn)]);
    }

    // How many it holds, on its badge, and what that is worth to its attacks.
    private static StatusData Custody() => new()
    {
        Id = CustodyId,
        NameKey = "Custody",
        DescriptionKey = "Each card it holds adds 3 damage to its attacks.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, StrengthPerCard,
                RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
