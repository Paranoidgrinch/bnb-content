using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Bosses;

// ── The Curator of Misplaced Hours (Act II boss, 278 HP) ──────────────────────────────────────────────────
//
// "The past is evidence. The future is already filed. The present is temporary."
//
// Every turn the Curator offers to lend you an hour: a Borrowed Hour in your hand gives 1 Energy now and puts 6 on
// its next attack (Kept Hours, on its badge, one stack a point of damage; the attack spends them all). Energy you
// leave unspent when your turn ends returns hours: 3 Kept Hours struck off for each.
//
// REWORKED 2026-11-03 (user, Act II trial: "irgendwas mit mehr dps für mehr gespielte Karten? eigentlich eine
// coole Idee aber kompliziert umgesetzt"): the dial of Past, Present and Future, the scheduled hours, Borrow One
// Minute and the citation are gone.
// REWORKED 2026-11-08 (CONTENT_REVIEW E9): "+2 per card played" was a flat tax on playing at all. The loan is a
// choice: tempo now against a heavier blow later, and Energy left over pays it back.
public static class CuratorOfMisplacedHours
{
    public const string EnemyId = "curator_of_misplaced_hours";

    public const string TheCuratorId = "the_curator_of_misplaced_hours";
    public const string KeptHoursId = "kept_hours";
    public const string BorrowedHourId = "borrowed_hour";

    public const int LoanDamage = 6;
    public const int ReturnPerEnergy = 3;

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    public static IEnumerable<StatusData> Statuses() =>
    [
        new StatusData
        {
            Id = TheCuratorId,
            NameKey = "The Curator of Misplaced Hours",
            DescriptionKey = "Each turn it lends you an hour: +1 Energy now, +6 on its next attack. Each Energy you leave "
                + "unspent at the end of your turn returns 3 of it.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
        },
        KeptHours(),
    ];

    // The hours, counted on the Curator: 1 damage each on its next blow, and that blow spends them.
    private static StatusData KeptHours()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new RemoveStatusNode<DamageDealtTriggeredEffectContext>(Self, new StatusDefinitionId(KeptHoursId)));
        return new StatusData
        {
            Id = KeptHoursId,
            NameKey = "Kept Hours",
            DescriptionKey = "Hours you borrowed: its next attack deals 1 more damage for each.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, 1,
                    RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // The loan is offered with the turn's hand; the Energy left when the turn ends pays it back.
    public static IReadOnlyList<EncounterTriggerData> Triggers()
    {
        var you = E<TurnEndedTriggeredEffectContext>.Self;
        var kept = new IterationTargetStatusStacksExpression<TurnEndedTriggeredEffectContext>(new StatusDefinitionId(KeptHoursId));
        return
        [
            EnemyRules.Encounter("CardsDrawn",
                E<CardsDrawnTriggeredEffectContext>.If(
                    E<CardsDrawnTriggeredEffectContext>.And(
                        E<CardsDrawnTriggeredEffectContext>.Applicant, E<CardsDrawnTriggeredEffectContext>.Present(TheCuratorId)),
                    E<CardsDrawnTriggeredEffectContext>.Give(BorrowedHourId))),
            EnemyRules.Encounter("TurnEnded",
                E<TurnEndedTriggeredEffectContext>.If(
                    E<TurnEndedTriggeredEffectContext>.And(
                        E<TurnEndedTriggeredEffectContext>.Applicant,
                        E<TurnEndedTriggeredEffectContext>.Is(
                            E<TurnEndedTriggeredEffectContext>.Energy(you), ComparisonOperator.Greater, 0)),
                    E<TurnEndedTriggeredEffectContext>.Each(E<TurnEndedTriggeredEffectContext>.Wearers(TheCuratorId),
                        E<TurnEndedTriggeredEffectContext>.If(
                            E<TurnEndedTriggeredEffectContext>.Is(kept, ComparisonOperator.Greater, 0),
                            E<TurnEndedTriggeredEffectContext>.AddStacks(E<TurnEndedTriggeredEffectContext>.It, KeptHoursId,
                                E<TurnEndedTriggeredEffectContext>.Minus(E<TurnEndedTriggeredEffectContext>.C(0),
                                    E<TurnEndedTriggeredEffectContext>.Min(kept,
                                        E<TurnEndedTriggeredEffectContext>.Times(
                                            E<TurnEndedTriggeredEffectContext>.Energy(you), ReturnPerEnergy)))))))),
        ];
    }

    // +1 Energy now; every Curator standing keeps 6 more of your future.
    public static CardData BorrowedHour() => EnemyRules.GivenCard(BorrowedHourId, "Borrowed Hour",
        "Gain 1 Energy. The Curator's next attack deals 6 more. Each Energy you leave unspent at the end of your turn "
        + "returns 3 of it.",
        E<CardPlayContext>.Seq(
            new GainResourceNode<CardPlayContext>(E<CardPlayContext>.Self, StandardCombatIds.EnergyResource, E<CardPlayContext>.C(1)),
            E<CardPlayContext>.Each(E<CardPlayContext>.Wearers(TheCuratorId),
                E<CardPlayContext>.Apply(E<CardPlayContext>.It, KeptHoursId, LoanDamage))));

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;
}
