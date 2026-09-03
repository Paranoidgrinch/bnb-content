using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Tombbreakers Three. Three experienced grave robbers who have opened the wrong royal
// tomb, and the only human beings in the act.
//
// They are the act's kill-order problem: all three act every round, so their individual bodies and blows are
// small, and which one you take down first decides what the rest of the fight is. Each death makes the
// survivors harder — the tomb preserves what is still inside it — and the last one standing is a different
// enemy from the one you started with.
//
//   The PRY-BAR VETERAN got here first and says so. It takes a Claim on the find, and standing makes it
//   stronger — which is Act III's word for exactly this, so the fight brings the law back with it.
//   The LAMP THIEF works in the dark it makes: it snuffs the lamp and then hits harder for the panic. Its
//   trespasses are filed in its own name, under the customs the Warrens already taught.
//   The CURSE-BEARER knows they should not have taken this. It is right, and the proof is what happens to
//   the others when one of them falls.
public static partial class ActFour
{
    public const string PryBarVeteranEnemyId = "pry_bar_veteran";
    public const string LampThiefEnemyId = "lamp_thief";
    public const string CurseBearerEnemyId = "curse_bearer";

    public const string TombbreakerId = "tombbreaker";
    public const string TombPreservedId = "tomb_preserved";

    public const int TombPreservedCap = 2;
    private const int TombPreservedBlock = 4;
    private const int DarknessBonus = 5;
    private const int VeteransClaimStrength = 2;
    private const int LastRobberStrength = 2;

    public static readonly IReadOnlySet<string> Tombbreakers = new HashSet<string>(StringComparer.Ordinal)
    {
        PryBarVeteranEnemyId, LampThiefEnemyId, CurseBearerEnemyId,
    };

    public static EffectProgram<EnemyActionContext>? TombbreakerIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "pry_bar_veteran.take_the_first_share" => TakeTheFirstShare(),
            "lamp_thief.knife_between_stones" => KnifeBetweenStones(10),
            "lamp_thief.find_another_passage" => FindAnotherPassage(10),
            _ => null,
        };

    public static IReadOnlyList<StatusData> TombbreakerStatuses() => [Tombbreaker(), TombPreserved()];

    // ── the tomb ──────────────────────────────────────────────────────────────────────────────────────────

    // The marker all three wear. It is where a robber's death is heard — the only place it can be — and the
    // tomb's answer to it. The Curse-Bearer is the one who says it out loud, but what preserves the survivors
    // is the opened tomb, so it keeps doing it when the Curse-Bearer is the one who falls.
    public static StatusData Tombbreaker() => new()
    {
        Id = TombbreakerId,
        NameKey = "Tombbreaker",
        DescriptionKey =
            "One of three who opened the wrong tomb. When one of them falls the survivors are preserved by "
            + "what they broke into — and the last one standing is a different enemy altogether.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(WeShouldNotHaveTakenThis(), nameof(TriggerEvent.Downed))],
    };

    // Deliberately NOT Embalmed. The act's preservation holds a fading thing in place on whoever wears it,
    // and a robber wearing that could prolong its own afflictions; what the tomb does to its intruders is
    // simpler and only ever good for them, so it is its own encounter-local word.
    public static StatusData TombPreserved() => new()
    {
        Id = TombPreservedId,
        NameKey = "Tomb-Preserved",
        DescriptionKey =
            "The opened tomb is keeping this one. 4 Block at the start of its turn for each, at most 2.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new GainBlockNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(TombPreservedBlock),
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(TombPreservedId))))),
                nameof(TriggerEvent.TurnStarted)),
        ],
    };

    // A robber falls, and the tomb closes a little tighter around the rest. Read on the marker the dying one
    // is still wearing, which is the only moment the question can be asked at all.
    private static EffectProgram<CombatantDownedTriggeredEffectContext> WeShouldNotHaveTakenThis()
    {
        var survivors = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(TombbreakerId));

        return new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                    survivors,
                    new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                            new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                                new IterationTargetStatusStacksExpression<CombatantDownedTriggeredEffectContext>(
                                    new StatusDefinitionId(TombPreservedId)),
                                ComparisonOperator.Less,
                                new ConstantExpression<CombatantDownedTriggeredEffectContext>(TombPreservedCap)),
                            new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget,
                                new StatusDefinitionId(TombPreservedId),
                                new ConstantExpression<CombatantDownedTriggeredEffectContext>(1))),

                        new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId("strength"),
                            new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)),
                    ])),

                // "The Tomb Was Closed for a Reason" — when one is left, the tomb has its answer and so does
                // the player: the survivor is a heavier enemy, and the room settles on you.
                new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                    new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                        new CountTargetsExpression<CombatantDownedTriggeredEffectContext>(survivors),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)),
                    new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [
                        new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                            survivors,
                            new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId("strength"),
                                new ConstantExpression<CombatantDownedTriggeredEffectContext>(
                                    LastRobberStrength))),

                        new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(EntombedId),
                            new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)),
                    ])),
            ]));
    }

    // ── the Pry-Bar Veteran ───────────────────────────────────────────────────────────────────────────────

    // "If it has no Claim, gain 1 Claim; otherwise 16 Block." The Claim is Act III's, made through the act's
    // own one making-point so that the standing is announced like any other — and the +2 Strength that comes
    // with holding it is granted with it, because nothing in this fight ever takes a Claim off a robber.
    private static EffectProgram<EnemyActionContext> TakeTheFirstShare()
    {
        var veteran = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        veteran, new StatusDefinitionId(ActThree.ClaimId)),
                    ComparisonOperator.Greater, Const(0)),
                new GainBlockNode<EnemyActionContext>(veteran, Const(16)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ActThree.CreateClaim<EnemyActionContext>(veteran),
                    new ApplyStatusNode<EnemyActionContext>(
                        veteran, new StatusDefinitionId("strength"), Const(VeteransClaimStrength)),
                ])));
    }

    // ── the Lamp Thief ────────────────────────────────────────────────────────────────────────────────────

    // "Work in Darkness: against a player with Panic, direct attacks +5 damage." Once per attack and not per
    // hit — a knife in the dark lands better, it is not two knives.
    private static ICombatExpression<EnemyActionContext, int> InTheDark(int damage) =>
        new AddExpression<EnemyActionContext>(
            Const(damage),
            new MultiplyExpression<EnemyActionContext>(
                Const(DarknessBonus),
                new MinExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        Applicant, new StatusDefinitionId("panic")),
                    Const(1))));

    private static EffectProgram<EnemyActionContext> KnifeBetweenStones(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, InTheDark(damage)),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
        ]));

    // …and its trespass is filed in its own name, through Act III's rule: three owed to this thief become
    // this thief's Claim, and the Safe-Conduct the tomb opens you with refuses the first one.
    private static EffectProgram<EnemyActionContext> FindAnotherPassage(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, InTheDark(damage)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(ActThree.TrespassId), Const(1),
                sourceSelector: CombatantTargetSelectors.Source),
        ]));
}
