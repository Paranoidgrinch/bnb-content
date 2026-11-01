using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's fourth boss: a dragon that is also the city's fee office.
//
// REWORKED in the content cut of 2026-10-29. It hoards Permits — each one 3 Block a turn and 2 damage on its
// Assessment — and every second turn it serves you a Fee Notice: pay 15 Gold and the fee goes into the hoard
// (one more Permit), or refuse and it takes the fee out of you: 8 damage. (Until 2026-11-01 a refusal also
// cost 2 Paperwork and gave it Anger — three consequences where one says it; user: one clear sentence.) It is the one fight in which the run's Gold is on the table.
//
// The Gold is paid in the fight and collected after it: the fight cannot reach the purse, so paying writes a
// tally on the hero, and a run program installed when the city opens charges 15 Gold for each payment once
// the fight is over (never below nothing). The Notice says so.
public static class MunicipalDragon
{
    public const string DragonId = "the_municipal_dragon";
    public const string PermitId = "hoarded_permit";            // stacks = hoard size, on the Dragon
    public const string NoticeNextId = "fee_notice_next";       // telegraph, on the Dragon
    public const string NoticeId = "fee_notice";                // on the player while a fee is open
    public const string PaidId = "fee_paid";                    // on the player once it is paid

    public const string PayCardId = "pay_the_fee";
    public const string FeesProgramId = "city_dragon_fees";

    public static CounterId FeesPaidCounter => new("dragon_fees_paid");
    private static CounterId NoticeBeatCounter => new("notice_beat");

    public const int StartingPermits = 2;
    public const int PermitMaximum = 5;
    public const int FeeGold = 15;
    public const int RefusalDamage = 8;

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        PassiveStatuses.NamedMarker(DragonId, "The Municipal Dragon",
            $"Every second turn it demands {FeeGold} Gold: pay and it hoards a Permit (it gets stronger), refuse and you take "
            + $"{RefusalDamage} damage."),
        Stacked(PermitId, "Hoarded Permit", "3 Block a turn, and 2 damage on the Dragon's Assessment."),
        PassiveStatuses.NamedMarker(NoticeNextId, "Fee Notice Next",
            "Your next turn it serves a Fee Notice."),
        PassiveStatuses.NamedMarker(NoticeId, "Fee Notice",
            $"Play Pay the Fee this turn ({FeeGold} Gold, charged after the fight), or take {RefusalDamage} damage at your turn's end."),
        PassiveStatuses.NamedMarker(PaidId, "Fee Paid",
            "You paid. The fee goes into the hoard: one more Permit."),
    ];

    public static IEnumerable<CardData> Cards() => [PayTheFee()];

    public static IReadOnlyList<EncounterTriggerData> Triggers() => [TheHoardGuards(), TheFeeIsSettled()];

    // After the fight, each payment costs its Gold. Installed when the city opens; a fight without the Dragon
    // tallies nothing and charges nothing.
    public static ITriggeredRunEffectDefinition FeesProgram() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Multiply(RunEventValues.CombatCounter(FeesPaidCounter.ToString()), RunExpr.Const(-FeeGold))),
        ]);

    // ── The player's turn ─────────────────────────────────────────────────────

    // The hoard's Block goes up as the player's turn begins (Block is cleared at the Dragon's own turn start
    // after its triggers run, so the guard would wipe itself there), and on every second turn the Notice is
    // served with the card that pays it.
    private static EncounterTriggerData TheHoardGuards()
    {
        var player = CombatantTargetSelectors.Source;
        var dragon = Dragon;
        var beat = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(dragon, NoticeBeatCounter);

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new GainBlockNode<TurnStartedTriggeredEffectContext>(dragon,
                        new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                dragon, new StatusDefinitionId(PermitId)),
                            Const<TurnStartedTriggeredEffectContext>(3))),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new RemainderExpression<TurnStartedTriggeredEffectContext>(beat, Const<TurnStartedTriggeredEffectContext>(2)),
                            ComparisonOperator.Equal, Const<TurnStartedTriggeredEffectContext>(1)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(dragon, new StatusDefinitionId(NoticeNextId)),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                player, new StatusDefinitionId(NoticeId), Const<TurnStartedTriggeredEffectContext>(1)),
                            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                                player, new CardDefinitionId(PayCardId), CardZone.Hand,
                                Const<TurnStartedTriggeredEffectContext>(1)),
                        ]),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            dragon, new StatusDefinitionId(NoticeNextId), Const<TurnStartedTriggeredEffectContext>(1))),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        dragon, NoticeBeatCounter, Const<TurnStartedTriggeredEffectContext>(1), relative: true),
                ])));

        return Encounter("TurnStarted", program);
    }

    // At the player's turn end an open Notice is settled: paid, it goes into the hoard; still open, it is refused.
    private static EncounterTriggerData TheFeeIsSettled()
    {
        var player = CombatantTargetSelectors.Source;
        var dragon = Dragon;

        IEffectNode<TurnEndedTriggeredEffectContext> Apply(ICombatantTargetSelector who, string id, int stacks) =>
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(who, new StatusDefinitionId(id),
                Const<TurnEndedTriggeredEffectContext>(stacks));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(player, new StatusDefinitionId(PaidId)),
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(player, new StatusDefinitionId(PaidId)),
                            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                        dragon, new StatusDefinitionId(PermitId)),
                                    ComparisonOperator.Less, Const<TurnEndedTriggeredEffectContext>(PermitMaximum)),
                                Apply(dragon, PermitId, 1)),
                        ])),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(player, new StatusDefinitionId(NoticeId)),
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(player, new StatusDefinitionId(NoticeId)),
                            new DealDamageNode<TurnEndedTriggeredEffectContext>(
                                player, Const<TurnEndedTriggeredEffectContext>(RefusalDamage)),
                        ])),
                ])));

        return Encounter("TurnEnded", program);
    }

    // 0 Energy. The fee is paid now and charged after the fight; the card goes with the turn either way.
    private static CardData PayTheFee() => new()
    {
        Id = PayCardId,
        NameKey = "Pay the Fee",
        DescriptionKey = $"Pay {FeeGold} Gold after the fight; the Dragon hoards one more Permit.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new TargetHasStatusExpression<CardPlayContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(NoticeId)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new RemoveStatusNode<CardPlayContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(NoticeId)),
                    new ApplyStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PaidId), Const<CardPlayContext>(1)),
                    new SetCombatantCounterNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, FeesPaidCounter, Const<CardPlayContext>(1), relative: true),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // ── Raw intents ───────────────────────────────────────────────────────────

    // The Assessment is authored in the source data (12, +2 per Permit); nothing here is code-only any more.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

    // ── idioms ────────────────────────────────────────────────────────────────

    private static ICombatantTargetSelector Dragon { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(DragonId)));

    private static ICombatExpression<TContext, bool> PlayersTurn<TContext>() where TContext : class =>
        new AndExpression<TContext>(
            new TargetHasStatusExpression<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            new ComparisonExpression<TContext>(
                new CountTargetsExpression<TContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(DragonId))),
                ComparisonOperator.Greater, Const<TContext>(0)));

    private static EncounterTriggerData Encounter<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static ConstantExpression<TContext> Const<TContext>(int value) where TContext : class => new(value);

    private static StatusData Stacked(string id, string name, string? description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
