using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stages 13 and 14 — The Necropolis Warrens and The Chamber of Fixed Days. Two stages about what a
// procedure looks like when it is honest about being a calendar.
//
//   The False-Door Finder certifies the wrong entrance, and does it under LAW: Stage 13 brings Act III's
//   Safe-Conduct, Trespass and Claim back for exactly as long as the Finder is standing (§3.9). A passage
//   check met earns another licence; one missed is a violation owed to the Finder, and three of those are a
//   Claim — which is what makes its false threshold a threshold at all.
//   The Cursed Loot Bearer carries objects that generate their own paperwork as they get harder to lift.
//   Every card whose Burdened surcharge is actually PAID is one more form.
//   The Star-Table Scribe keeps the astronomical table: the measure for each appointed day, 1 → 2 → 3, in
//   that order and no other. A day measured wrong is a day written into the register.
//   The Moon-Cycle Ibis remembers the last rite it managed to perform and returns to it at its cycle point —
//   one stack, never the whole original amount (§3.7) — and sets the rite it has NOT lately done.
//   The Eclipse Scarab's procession contains a scheduled absence of noon. It is visible three turns out.
//
// Nothing here introduces a universal status. Stage 13's law is borrowed whole from Act III — the same
// statuses, the same customs, the same threshold — because a localized return that FORKED the vocabulary
// would be a sixth word for the act, and the point of §3.9 is that it is not one.
public static partial class ActFour
{
    public const string FalseDoorFinderEnemyId = "false_door_finder";
    public const string CursedLootBearerEnemyId = "cursed_loot_bearer";
    public const string MoonCycleIbisEnemyId = "moon_cycle_ibis";
    public const string EclipseScarabEnemyId = "eclipse_scarab";

    public const string LunarReturnId = "lunar_return";
    public const string LastRiteBurdenedId = "last_rite_burdened";
    public const string LastRiteEntombedId = "last_rite_entombed";
    public const string ApproachOfNoonId = "approach_of_noon";

    // How many approaches the Scarab makes before noon goes missing. Three of them plus the black turn itself
    // is the master's "every fourth own turn".
    public const int EclipseSteps = 3;

    public static EffectProgram<EnemyActionContext>? WarrenIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "moon_cycle_ibis.set_the_rite" => SetTheRite(15),
            "moon_cycle_ibis.wing_shelter" => WingShelter(25),
            "eclipse_scarab.black_noon" => BlackNoon(12),
            _ => null,
        };

    // ── the False-Door Finder ─────────────────────────────────────────────────────────────────────────────

    // ── the Cursed Loot Bearer ────────────────────────────────────────────────────────────────────────────

    // ── the Star-Table Scribe ─────────────────────────────────────────────────────────────────────────────

    // ── the Moon-Cycle Ibis ───────────────────────────────────────────────────────────────────────────────

    // The memory, and the two faces it wears. A program cannot hold a status id it only learns at fire time —
    // it can answer one in the same breath (that is what Replicated bought at IV-7), but it cannot put one in
    // its pocket for three turns — so the rite the Ibis last performed is remembered the way this act
    // remembers everything a player has to plan around: as a face on the body.
    public static StatusData LunarReturn() => new()
    {
        Id = LunarReturnId,
        NameKey = "Lunar Return",
        DescriptionKey =
            "This ibis repeats its rites by lunar return. Whatever affliction it last managed to lay on you "
            + "is remembered as the Last Rite — and the next rite it sets is the other one.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            // Applied and merged both, because a rite laid on a player who already carries it is a merge and
            // not an application — and it is just as much a rite for that.
            Trigger(RememberTheRite<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(RememberTheRite<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
        ],
    };

    public static IReadOnlyList<StatusData> LastRites() =>
    [
        LastRite(LastRiteBurdenedId, "Last Rite: Burdened",
            "The ibis last laid weight on you. Its cycle point repeats 1 Burdened — and its next rite is "
            + "burial."),
        LastRite(LastRiteEntombedId, "Last Rite: Entombed",
            "The ibis last packed you deeper. Its cycle point repeats 1 Entombed — and its next rite is "
            + "weight."),
    ];

    private static StatusData LastRite(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "Whenever the Ibis SUCCESSFULLY applies a negative status, remember its type." Successfully is the
    // operative word — a refused filing raises no application at all, so nothing is remembered — and the
    // gate is the act's own: an original affliction on the player, by this body.
    private static EffectProgram<TContext> RememberTheRite<TContext>() where TContext : class
    {
        var ibis = CombatantTargetSelectors.Source;

        // "That rite was just laid" → wear its face, and take the other one off.
        IEffectNode<TContext> Remember(string riteId, string faceId, string otherFaceId) =>
            new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(riteId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new RemoveStatusNode<TContext>(ibis, new StatusDefinitionId(otherFaceId)),
                    new ApplyStatusNode<TContext>(
                        ibis, new StatusDefinitionId(faceId), new ConstantExpression<TContext>(1),
                        sourceSelector: ibis),
                ]));

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    OriginalAfflictionOnThePlayer<TContext>(),
                    new TargetHasStatusExpression<TContext>(ibis, new StatusDefinitionId(LunarReturnId))),
                new CausalSequenceEffectNode<TContext>(
                [
                    Remember(BurdenedId, LastRiteBurdenedId, LastRiteEntombedId),
                    Remember(EntombedId, LastRiteEntombedId, LastRiteBurdenedId),
                ])));
    }

    // Set the rite the moon has NOT lately returned to: weight after burial, burial after weight, and weight
    // on a night with no record behind it. The memory decides, so the player reads what is coming off the
    // face the ibis is already wearing.
    private static EffectProgram<EnemyActionContext> SetTheRite(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(LastRiteBurdenedId)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(EntombedId), Const(1)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(BurdenedId), Const(1))),
        ]));

    // The cycle point: shelter, and one stack of the Last Rite. ONE — §3.7 is explicit that the return is not
    // a reproduction of the original amount, which is what keeps a predictable body from being a multiplying
    // one. A fight in which the ibis has never landed a rite repeats nothing.
    private static EffectProgram<EnemyActionContext> WingShelter(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(LastRiteBurdenedId)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(BurdenedId), Const(1)),
                new ConditionalEffectNode<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(LastRiteEntombedId)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(EntombedId), Const(1)))),
        ]));

    // ── the Eclipse Scarab ────────────────────────────────────────────────────────────────────────────────

    // How far the procession has got towards the hour that is not there. Visible from the first turn, which
    // is the entire point: the master calls the schedule "visible well in advance", and a catastrophe the
    // player cannot count down to is just a big number.
    public static StatusData ApproachOfNoon() => new()
    {
        Id = ApproachOfNoonId,
        NameKey = "Approach of Noon",
        DescriptionKey =
            "How far the scarab's procession has come. At 3 the next turn is Black Noon, and the count "
            + "starts again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The scheduled absence of noon: a fixed combined threat, and the procession begins again — a calendar
    // does not stop having fourth days.
    private static EffectProgram<EnemyActionContext> BlackNoon(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId("panic"), Const(2)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(EntombedId), Const(1)),

            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ApproachOfNoonId)),
        ]));
}
