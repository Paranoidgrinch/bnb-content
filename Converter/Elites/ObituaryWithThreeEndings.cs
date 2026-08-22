using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── The Obituary with Three Endings (Act II elite) ────────────────────────────────────────────────────────
//
// A biography that will not be finished badly. Kill it while its record is unsettled and the death does not
// take — the account is rewritten and it comes back as something braver, then as something merely accurate.
// Each life has a condition under which its death is FINAL, and the whole fight is about meeting one:
//
//   A Respectable Life  (128) — final if you owe it nothing.
//   A Heroic Life        (46) — final if you have played a Redacted card this turn.
//   A Completely Accurate Account (32) — always final.
//
// The difficulty is not one long HP bar; it is the two endings you failed to give it.
public static class ObituaryWithThreeEndings
{
    public const string EnemyId = "obituary_with_three_endings";

    public const string TheObituaryId = "the_obituary";
    public const string ObituaryRulesId = "obituary_rules";
    public const string NoticeId = "proper_notice_of_passing";
    public const string NoticeMark = "proper_notice";

    // The two lives that can be rewritten, and the markers that say which one is being lived.
    public const string RespectableLifeId = "a_respectable_life";
    public const string HeroicLifeId = "a_heroic_life";
    public const string HeroicPhaseId = "living_a_heroic_life";
    public const string AccuratePhaseId = "a_completely_accurate_account";

    // "Only source-bound obligations created by this Obituary count." Kept as a counter on the player rather
    // than read off the Overdue instances: it is the one number both sides have to agree on, and a counter is
    // readable as a scalar from either end where a source-bound status is not.
    private static readonly CounterId DebtCounter = new("obituary_debt");
    private static readonly CounterId RedactedPlayedCounter = new("obituary_redacted_played");

    private const int HeroicHealth = 46;
    private const int AccurateHealth = 32;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Obituaries =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheObituaryId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheObituaryId, "The Obituary"),
        Marker(HeroicPhaseId, "A Heroic Life"),
        Marker(AccuratePhaseId, "A Completely Accurate Account"),
        RespectableLife(),
        HeroicLife(),
        Rules(),
        // 13.2: the single canonical Phase-I Reference source. Fulfilling it settles a debt; failing it makes
        // one — which is exactly the tool the design promises for settling the record before lethal damage.
        ActTwo.Reference(NoticeId, "Proper Notice of Passing", NoticeMark,
            "The Obituary requires notice. Play the cited card and a debt is struck; let it go and one is made.",
            cite: new NoOpEffectNode<CardsDrawnTriggeredEffectContext>(),
            onFulfilled: Settle<CardPlayedTriggeredEffectContext>(-1),
            onFailed: Settle<TurnStartedTriggeredEffectContext>(+1)),
    ];

    // The debt is the player's own counter either way — in the fulfilment hook the acting source is the
    // player, in the failure hook it is the Obituary, so the two write it from opposite sides.
    private static IEffectNode<TContext> Settle<TContext>(int delta) where TContext : class
    {
        var owner = typeof(TContext) == typeof(CardPlayedTriggeredEffectContext)
            ? CombatantTargetSelectors.Source
            : Opponent;

        return new SetCombatantCounterNode<TContext>(
            owner, DebtCounter,
            new MaxExpression<TContext>(
                new ConstantExpression<TContext>(0),
                new AddExpression<TContext>(
                    new CombatantCounterExpression<TContext>(owner, DebtCounter),
                    new ConstantExpression<TContext>(delta))),
            relative: false);
    }

    // ── 13.4 / 13.7 The rewrites ──────────────────────────────────────────────────────────────────────────
    //
    // Authored as the engine's one-shot death prevention rather than a revive: a downed combatant refuses
    // healing and status application, so the only place to stand is BEFORE the down. The prevention consumes
    // its own status, which is what makes each rewrite happen at most once.
    private static StatusData RespectableLife() => new()
    {
        Id = RespectableLifeId,
        NameKey = "A Respectable Life",
        DescriptionKey = "While the record is unsettled, this death does not take.",
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
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                HeroicPhaseId, 0, StatusPolarity.Neutral),
        ]),
    };

    private static StatusData HeroicLife() => new()
    {
        Id = HeroicLifeId,
        NameKey = "A Heroic Life",
        DescriptionKey = "Unless something of yours was redacted this turn, this death does not take either.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(AccurateHealth,
        [
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                AccuratePhaseId, 0, StatusPolarity.Neutral),
        ]),
    };

    // ── 13.1 / 13.5 The death conditions ──────────────────────────────────────────────────────────────────
    //
    // A prevention interceptor cannot ask a question — it fires whenever it is there. So the CONDITION is
    // expressed by whether the status is on the Obituary at all, and the player's own rules keep it in step:
    // a settled record takes the Respectable Life off, a Redacted card played takes the Heroic Life off.
    private static StatusData Rules()
    {
        var atTurnStart = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Self, RedactedPlayedCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                Sync<TurnStartedTriggeredEffectContext>(),
            ]));

        // Two things ride on a play: a Redacted card played is the heroic ending, and any play may have
        // settled the record.
        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(ActTwo.RedactedMark)),
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        Self, RedactedPlayedCounter,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false)),
                Sync<CardPlayedTriggeredEffectContext>(),
            ]));

        return Rule(ObituaryRulesId, "The Record",
            "Kill it while you still owe it notice and the death does not take. In its second life, only a "
            + "death you redacted something for is final.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    atTurnStart, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    onPlay, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    // Put each Obituary's death clause where its own life says it should be.
    private static IEffectNode<TContext> Sync<TContext>() where TContext : class
    {
        var it = CombatantTargetSelectors.IterationTarget;

        IEffectNode<TContext> Clause(string status, ICombatExpression<TContext, bool> conditionMet) =>
            new ConditionalEffectNode<TContext>(
                conditionMet,
                new RemoveStatusNode<TContext>(it, new StatusDefinitionId(status)),
                @else: new ApplyStatusNode<TContext>(
                    it, new StatusDefinitionId(status), new ConstantExpression<TContext>(1)));

        return new ForEachTargetEffectNode<TContext>(Obituaries,
            new ConditionalEffectNode<TContext>(
                // Phase III has no clause at all.
                new TargetHasStatusExpression<TContext>(it, new StatusDefinitionId(AccuratePhaseId)),
                new NoOpEffectNode<TContext>(),
                @else: new ConditionalEffectNode<TContext>(
                    new TargetHasStatusExpression<TContext>(it, new StatusDefinitionId(HeroicPhaseId)),
                    // 13.5: final once a Redacted card has been played this turn.
                    Clause(HeroicLifeId, Played<TContext>(RedactedPlayedCounter)),
                    // 13.1: final once nothing is owed.
                    @else: Clause(RespectableLifeId,
                        new NotExpression<TContext>(Played<TContext>(DebtCounter))))));
    }

    private static ICombatExpression<TContext, bool> Played<TContext>(CounterId counter) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, counter),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: the engine rotates ONE intent list, so each of the five slots carries all three lives and
    // reads differently depending on which is being lived. The telegraph shows the Phase-I name throughout;
    // the phase marker on the Obituary is what a player actually reads.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "record_a_respectable_career" => Lives(
            // P1: 13 damage and a Notice issued.
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(13), IssueNotice()]),
            // P2 Suppress the Witness: it redacts a card of yours and hits for 8 — which is also the tool
            // that lets you end it, since a Redacted card played is the heroic ending.
            new CausalSequenceEffectNode<EnemyActionContext>([RedactOne(), Damage(8)]),
            // P3 Final Corrected Edition.
            Damage(20)),
        "settled_accounts" => Lives(
            new CausalSequenceEffectNode<EnemyActionContext>(
                [new GainBlockNode<EnemyActionContext>(Self, Const(18)), IssueNotice()]),
            // P2 Add a Glorious Detail: 9 twice.
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(9), Damage(9)]),
            // P3 No Further Amendments.
            new CausalSequenceEffectNode<EnemyActionContext>(
                [Damage(15), ApplyToPlayer(Keywords.Paperwork, 1)])),
        "an_orderly_decline" => Lives(
            Damage(16),
            // P2 Correct the Cowardice: 13, and a Redacted card in hand is cited.
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(13), CiteRedacted()]),
            Damage(20)),
        "no_outstanding_matters" => Lives(
            // P1: the Obituary makes a debt of its own, without asking.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(10),
                ApplyToPlayer(ActTwo.OverdueId, 1),
                new SetCombatantCounterNode<EnemyActionContext>(
                    Opponent, DebtCounter, Const(1), relative: true),
            ]),
            // P2 Heroic Last Stand.
            Damage(20),
            // P3 No Further Amendments.
            new CausalSequenceEffectNode<EnemyActionContext>(
                [Damage(15), ApplyToPlayer(Keywords.Paperwork, 1)])),
        "a_third_ending" => Lives(
            // P1 Family Notified in Writing.
            new CausalSequenceEffectNode<EnemyActionContext>(
                [Damage(11), ApplyToPlayer(Keywords.Paperwork, 1)]),
            // P2 Embellish the Deed.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(15),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId("strength"), Const(1)),
            ]),
            Damage(20)),
        _ => null,
    };

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

    // 13.2: the Notice is announced now and cited on the player's next hand — the same beat every Act-II
    // citation uses, because a card cited during the enemy's turn is a card about to be discarded.
    private static IEffectNode<EnemyActionContext> IssueNotice() =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.Hand,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(), new TagId(NoticeMark)),
            takeFirst: 1);

    private static IEffectNode<EnemyActionContext> RedactOne() => ActTwo.RedactOne();

    private static IEffectNode<EnemyActionContext> CiteRedacted() =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.Hand,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(), new TagId(NoticeMark)),
            markFilter: new TagId(ActTwo.RedactedMark), takeFirst: 1);

    private static IEffectNode<EnemyActionContext> ApplyToPlayer(string status, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Opponent, new StatusDefinitionId(status), Const(stacks));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    private static StatusData Marker(string id, string name) => Rule(id, name, name, []);

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
