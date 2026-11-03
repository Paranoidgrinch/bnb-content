using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 4 — **Great Toll Frog** (176 HP).
//
// Nothing crosses for free. At the start of each of its turns the Frog swallows 5 of your Gold, and for every 10
// in its belly its attacks hit 1 harder. Deal it 25 in one turn and it spits the lot back and its weight is gone.
// What is still in its belly when the fight ends is gone for good.
//
// Gold is the run's, so the fight only keeps the count (a counter on the player, `frog_belly`) and a run program
// charges it once the fight is over (FrogSpoilsProgram) — the Municipal Dragon's idiom. The belly is also the
// Frog's own badge, so the price is always in sight.
//
// REWORKED 2026-11-03 (user, Act III pass): the Wergild it swallowed as Toll, and the Toll's payout, are gone.
public static partial class ActThree
{
    public const string GreatTollFrogEnemyId = "great_toll_frog";
    public const string GreatTollFrogId = "nothing_crosses_for_free";
    public const string SwallowedGoldId = "swallowed_gold";
    public const string TollWeightId = "toll_weight";

    public const int FrogMeal = 5;
    public const int GoldPerWeight = 10;
    public const int FrogSpitDamage = 25;
    public const string FrogSpoilsProgramId = "ford_frog_spoils";
    public static CounterId FrogBellyCounter => new("frog_belly");
    public static CounterId FrogBruiseCounter => new("frog_bruise");

    private static ICombatantTargetSelector Frog { get; } = Elite(GreatTollFrogId);

    // After the fight: what the Frog still holds is lost.
    public static ITriggeredRunEffectDefinition FrogSpoilsProgram() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Multiply(RunEventValues.CombatCounter(FrogBellyCounter.ToString()), RunExpr.Const(-1))),
        ]);

    private static IEnumerable<StatusData> FrogStatuses() =>
    [
        NothingCrossesForFree(),
        new StatusData
        {
            Id = SwallowedGoldId,
            NameKey = "Swallowed Gold",
            DescriptionKey = "Your Gold in its belly. Deal it 25 in one turn to get it back; what is left when the fight ends is lost.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
        },
        new StatusData
        {
            Id = TollWeightId,
            NameKey = "Heavy with Toll",
            DescriptionKey = "1 more damage on its attacks for every 10 Gold it has swallowed.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, 1,
                    RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers = [],
        },
    ];

    private static StatusData NothingCrossesForFree()
    {
        // Its own turn: a meal, and the weight of everything swallowed so far.
        var self = CombatantTargetSelectors.Source;
        var belly = CounterOn<TurnStartedTriggeredEffectContext>(Applicant, FrogBellyCounter);
        var meal = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Applicant, FrogBellyCounter, new ConstantExpression<TurnStartedTriggeredEffectContext>(FrogMeal), relative: true),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                self, new StatusDefinitionId(SwallowedGoldId), new ConstantExpression<TurnStartedTriggeredEffectContext>(FrogMeal)),
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(TollWeightId)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                Is(belly, ComparisonOperator.GreaterOrEqual, GoldPerWeight),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(self, new StatusDefinitionId(TollWeightId),
                    new DivideExpression<TurnStartedTriggeredEffectContext>(belly, new ConstantExpression<TurnStartedTriggeredEffectContext>(GoldPerWeight)))),
            SetCounter<TurnStartedTriggeredEffectContext>(self, FrogBruiseCounter, 0),
        ]);

        // A beating: 25 health from the player in one turn, and it spits everything back.
        var frog = CombatantTargetSelectors.EventTarget;
        var bruise = CounterOn<DamageReceivedTriggeredEffectContext>(frog, FrogBruiseCounter);
        var beaten = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    frog, FrogBruiseCounter, new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    Is(bruise, ComparisonOperator.GreaterOrEqual, FrogSpitDamage),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        SetCounter<DamageReceivedTriggeredEffectContext>(Applicant, FrogBellyCounter, 0),
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(frog, new StatusDefinitionId(SwallowedGoldId)),
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(frog, new StatusDefinitionId(TollWeightId)),
                    ])),
            ]));

        return Rule(GreatTollFrogId, "Nothing Crosses for Free",
            "Each turn it swallows 5 of your Gold; +1 damage per 10 swallowed. Deal it 25 in one turn and it spits it all back.",
            [On("TurnStarted", meal), On("DamageTaken", beaten)]);
    }

    private static EffectProgram<EnemyActionContext>? FrogIntent(string enemyId, string intentId) => null;
}
