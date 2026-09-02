using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stage 3 — The Granary Courts. The stage where the act's two economic words are pushed into each
// other on purpose: the measure asks for an exact expenditure, and the tax changes what an expenditure comes
// to. Meeting one is what stops you meeting the other.
//
//   The Crocodile of the Short Measure enforces a standard that is deliberately unfair — it demands the whole
//   turn (3), and its other jaw loads the scale with a burden, so the demand it made last turn is the one the
//   surcharge sabotages this turn. It snaps at whatever deficit is left.
//   The Jar-Seal Scarab Swarm attaches storage tags to anything it can reach: its swarm is three small hits,
//   and if any of them actually reaches flesh the player leaves the fight carrying one more thing.
//   The Hungry Grain Thief lives off the surcharge itself. Every card the player pays extra for is a ration
//   it collects when it comes round, and a fat thief eats.
//
// Which is why the Thief tallies at its OWN turn start rather than reacting to each payment: the count of
// surcharges paid is written down by the tax itself (`burden_paid`), and a body that reads a written number
// at a fixed moment needs no agreement with anybody about the order two rules fire in.
public static partial class ActFour
{
    public const string CrocodileEnemyId = "crocodile_of_the_short_measure";
    public const string ScarabSwarmEnemyId = "jar_seal_scarab_swarm";
    public const string GrainThiefEnemyId = "hungry_grain_thief";

    // The Thief's rule and its larder.
    public const string HungryForRationsId = "hungry_for_rations";
    public const string RationId = "ration";

    // What the short measure asks for: the whole turn. Three Energy is meetable with an unburdened hand and
    // awkward with a burdened one, which is the entire point of the stage — the Crocodile's own other jaw is
    // what makes its demand hard to meet.
    private const int ShortMeasure = 3;

    // How many rations the Thief eats at once, and what eating one is worth to it.
    public const int RationsPerFeast = 3;
    private const int FeastHealing = 5;

    // How many surcharges the Thief has already collected its cut from. The player's own `burden_paid` is the
    // running total; this is the Thief's bookmark in it, so a body that joins late or looks twice takes
    // exactly its share and no more.
    public static CounterId RationsCollected => new("rations_collected");

    // What the player's health was before the swarm struck, kept on the swarm: the only way a program can ask
    // "did any of those hits actually reach flesh?" is to look at the flesh before and after.
    public static CounterId FleshBeforeTheSwarm => new("flesh_before_the_swarm");

    public static EffectProgram<EnemyActionContext>? GranaryIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "crocodile_of_the_short_measure.short_measure" => SetTheMeasure(12, Const(ShortMeasure)),
            "crocodile_of_the_short_measure.snap_at_the_deficit" => SnapAtTheDeficit(19),
            "jar_seal_scarab_swarm.seal_swarm" => SealTheExcess(hits: 3, each: 4),
            "hungry_grain_thief.feast_on_rations" => FeastOnRations(18),
            _ => null,
        };

    // ── the Crocodile of the Short Measure ────────────────────────────────────────────────────────────────

    // A big bite, and then the deficit itself is added to what the player is carrying. The damage is flat and
    // always what the telegraph says; what the deficit changes is whether one more burden comes with it.
    private static EffectProgram<EnemyActionContext> SnapAtTheDeficit(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ConditionalEffectNode<EnemyActionContext>(
                // The record is 1 + the distance, so anything above 1 is a measure that was missed.
                AtLeast(2),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(BurdenedId), Const(1))),
        ]));

    // ── the Jar-Seal Scarab Swarm ─────────────────────────────────────────────────────────────────────────

    // Three small hits, and a seal attached to whatever got through. "Got through" means flesh: a swarm that
    // breaks itself on the player's block has tagged nothing, however many times it hit.
    //
    // The before-and-after reading is the honest one here. A rule that counted damage EVENTS would count the
    // hits that block ate as well, and one that watched the player's block would be wrong the moment
    // something else on the field spent it.
    private static EffectProgram<EnemyActionContext> SealTheExcess(int hits, int each)
    {
        var swarm = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new SetCombatantCounterNode<EnemyActionContext>(
                    swarm, FleshBeforeTheSwarm,
                    new CombatantCurrentHealthExpression<EnemyActionContext>(Applicant), relative: false),

                new RepeatEffectNode<EnemyActionContext>(
                    Const(hits),
                    new DealDamageNode<EnemyActionContext>(Applicant, Const(each))),

                // One seal per swarm, however many of the three got through.
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCurrentHealthExpression<EnemyActionContext>(Applicant),
                        ComparisonOperator.Less,
                        new CombatantCounterExpression<EnemyActionContext>(swarm, FleshBeforeTheSwarm)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(BurdenedId), Const(1))),
            ]));
    }

    // ── the Hungry Grain Thief ────────────────────────────────────────────────────────────────────────────

    // The larder. Visible, because a fattening thief is something the player is meant to see and answer —
    // either by killing it or by not paying surcharges in front of it.
    public static StatusData Ration() => new()
    {
        Id = RationId,
        NameKey = "Ration",
        DescriptionKey =
            "What the bureaucracy made somebody else carry. At 3 the Thief eats: it feeds on the surcharges "
            + "you have paid and heals.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The Thief's rule: at the start of its own turn it takes its cut of every surcharge the player has paid
    // since it last looked. One ration per taxed card, which is the master's "once per card played" — the tax
    // writes one payment down per card, so the arithmetic is the design.
    public static StatusData HungryForRations() => new()
    {
        Id = HungryForRationsId,
        NameKey = "Hungry for Rations",
        DescriptionKey =
            "This Thief lives off surcharges: at the start of its turn it takes 1 Ration for every card you "
            + "paid a Burdened surcharge on since it last looked.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TakeTheCut(), nameof(TriggerEvent.TurnStarted)),
        ],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> TakeTheCut()
    {
        var thief = CombatantTargetSelectors.Source;

        var paid = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, BurdenPaid);
        var collected = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(thief, RationsCollected);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(paid, ComparisonOperator.Greater, collected),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        thief, new StatusDefinitionId(RationId),
                        new SubtractExpression<TurnStartedTriggeredEffectContext>(paid, collected),
                        sourceSelector: thief),

                    // …and move the bookmark, so the same surcharge is never collected twice.
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        thief, RationsCollected, paid, relative: false),
                ])));
    }

    // The feast. The bite is always what the telegraph says; what the rations buy is the healing on top, and
    // three of them are eaten doing it.
    private static EffectProgram<EnemyActionContext> FeastOnRations(int damage)
    {
        var thief = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            thief, new StatusDefinitionId(RationId)),
                        ComparisonOperator.GreaterOrEqual,
                        Const(RationsPerFeast)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        new HealNode<EnemyActionContext>(thief, Const(FeastHealing)),
                        new ModifyStatusStacksNode<EnemyActionContext>(
                            thief, new StatusDefinitionId(RationId), Const(-RationsPerFeast)),
                    ])),
            ]));
    }
}
