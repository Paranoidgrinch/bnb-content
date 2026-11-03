using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── The Obituary with Three Endings (Act II elite) ────────────────────────────────────────────────────────
//
// A biography that will not be finished badly: it has to be killed three times. Each death is rewritten into a
// new life with its own way of fighting, and only the third is final.
//
//   A Respectable Life  (128) — steady blows and Block.
//   A Heroic Life        (46) — reckless double strikes.
//   A Completely Accurate Account (32) — heavy blows; this death is final.
//
// REWORKED 2026-11-03 (user, Act II trial: "man sieht nicht welche Karte redacted ist; grundsätzlich einen Gegner
// 3 mal töten müssen ganz cool"): the conditions on its deaths — the debt of Notice, the Redacted card — are gone;
// every life comes back.
public static class ObituaryWithThreeEndings
{
    public const string EnemyId = "obituary_with_three_endings";

    public const string TheObituaryId = "the_obituary";

    // The two lives that can be rewritten, and the markers that say which one is being lived.
    public const string RespectableLifeId = "a_respectable_life";
    public const string HeroicLifeId = "a_heroic_life";
    public const string HeroicPhaseId = "living_a_heroic_life";
    // The first ending's own chip (content cut 2026-10-29: every return shows its ending — one chip at a time).
    public const string RespectablePhaseId = "living_a_respectable_life";
    public const string AccuratePhaseId = "a_completely_accurate_account";

    private const int HeroicHealth = 46;
    private const int AccurateHealth = 32;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheObituaryId, "The Obituary",
            "It must be killed three times: each death returns it as a new life with new attacks."),
        Marker(RespectablePhaseId, "First Life: A Respectable Life",
            "Steady blows and Block. Killed, it returns as a Heroic Life."),
        Marker(HeroicPhaseId, "Second Life: A Heroic Life",
            "Reckless double strikes. Killed, it returns as a Completely Accurate Account."),
        Marker(AccuratePhaseId, "Last Life: A Completely Accurate Account",
            "Heavy blows. This death is final."),
        RespectableLife(),
        HeroicLife(),
    ];

    // ── 13.4 / 13.7 The rewrites ──────────────────────────────────────────────────────────────────────────
    //
    // Authored as the engine's one-shot death prevention rather than a revive: a downed combatant refuses
    // healing and status application, so the only place to stand is BEFORE the down. The prevention consumes
    // its own status, which is what makes each rewrite happen at most once.
    private static StatusData RespectableLife() => new()
    {
        Id = RespectableLifeId,
        NameKey = "A Respectable Life",
        DescriptionKey = "Its first death does not take: it returns as a Heroic Life.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(HeroicHealth,
        [
            // "…gain 8 Block; no attack occurs during the transition window."
            new InterceptorEffectData(nameof(EffectKind.GainBlock), nameof(EffectTarget.Self), 8, "", 0,
                StatusPolarity.Neutral),
            new InterceptorEffectData(nameof(EffectKind.RemoveStatus), nameof(EffectTarget.Self), 0,
                RespectablePhaseId, 0, StatusPolarity.Neutral),
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                HeroicPhaseId, 0, StatusPolarity.Neutral),
            // The second life's own rewrite is written in now: standing from the start, both would answer the
            // first death.
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                HeroicLifeId, 0, StatusPolarity.Neutral),
        ]),
    };

    private static StatusData HeroicLife() => new()
    {
        Id = HeroicLifeId,
        NameKey = "A Heroic Life",
        DescriptionKey = "Its second death does not take: it returns as a Completely Accurate Account.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(AccurateHealth,
        [
            new InterceptorEffectData(nameof(EffectKind.RemoveStatus), nameof(EffectTarget.Self), 0,
                HeroicPhaseId, 0, StatusPolarity.Neutral),
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                AccuratePhaseId, 0, StatusPolarity.Neutral),
        ]),
    };

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: the engine rotates ONE intent list, so each of the five slots carries all three lives and
    // reads differently depending on which is being lived. The telegraph shows the Phase-I name throughout;
    // the phase marker on the Obituary is what a player actually reads.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "record_a_respectable_career" => Lives(Damage(13), Twice(8), Damage(20)),
        "settled_accounts" => Lives(
            new GainBlockNode<EnemyActionContext>(Self, Const(18)),
            Twice(9),
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(15), ApplyToPlayer(Keywords.Paperwork, 1)])),
        "an_orderly_decline" => Lives(Damage(16), Twice(7), Damage(20)),
        "no_outstanding_matters" => Lives(
            Damage(10),
            Damage(20),
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(15), ApplyToPlayer(Keywords.Paperwork, 1)])),
        "a_third_ending" => Lives(
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(11), ApplyToPlayer(Keywords.Paperwork, 1)]),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Twice(8),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId("strength"), Const(1)),
            ]),
            Damage(20)),
        _ => null,
    };

    private static IEffectNode<EnemyActionContext> Twice(int amount) =>
        new CausalSequenceEffectNode<EnemyActionContext>([Damage(amount), Damage(amount)]);

    // One slot, three lives.
    private static EffectProgram<EnemyActionContext> Lives(
        IEffectNode<EnemyActionContext> respectable,
        IEffectNode<EnemyActionContext> heroic,
        IEffectNode<EnemyActionContext> accurate) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(Self, new StatusDefinitionId(AccuratePhaseId)),
            accurate,
            @else: new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(Self, new StatusDefinitionId(HeroicPhaseId)),
                heroic,
                @else: respectable)));

    private static IEffectNode<EnemyActionContext> ApplyToPlayer(string status, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Opponent, new StatusDefinitionId(status), Const(stacks));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // A marker still owes the player an explanation on hover: naming it twice explains nothing.
    private static StatusData Marker(string id, string name, string description) =>
        Rule(id, name, description, []);

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
