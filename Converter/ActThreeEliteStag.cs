using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 1 — **The Stag of Pre-Approved Violence** (138 HP).
//
// The hunting season turns every round. In an odd round violence is approved: your Deeds and its attacks both
// deal 50 % more. In an even round violence is forbidden: every Deed breaks its law. The season is on the Stag
// (and, while open, on you), so the round ahead can be read.
//
// REWORKED 2026-11-03 (user, Act III pass): the leave to resist, the Sanctions, the marked verge and the
// Authorized Charge are gone.
public static partial class ActThree
{
    public const string StagEnemyId = "stag_of_pre_approved_violence";
    public const string StagLawId = "pre_approved_violence";
    public const string SeasonOpenId = "season_open";
    public const string SeasonClosedId = "season_closed";
    public const string HuntersLicenceId = "hunters_licence";

    private static ICombatantTargetSelector Stag { get; } = Elite(StagLawId);

    private static IEnumerable<StatusData> StagStatuses() =>
    [
        TheHuntingSeason(),
        Sharpened(SeasonOpenId, "Season Open", "Violence is approved this round: its attacks deal 50% more."),
        Marker(SeasonClosedId, "Season Closed", "Violence is forbidden this round: every Deed you play breaks its law."),
        Sharpened(HuntersLicenceId, "Hunter's Licence", "Violence is approved this round: your attacks deal 50% more."),
    ];

    private static StatusData Sharpened(string id, string name, string text) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = text,
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, 150,
                RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    private static StatusData TheHuntingSeason()
    {
        var odd = Is(new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new RoundNumberExpression<TurnStartedTriggeredEffectContext>(), new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
            ComparisonOperator.Equal, 1);
        IEffectNode<TurnStartedTriggeredEffectContext> Put(ICombatantTargetSelector who, string id) =>
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(who, new StatusDefinitionId(id), new ConstantExpression<TurnStartedTriggeredEffectContext>(1));
        IEffectNode<TurnStartedTriggeredEffectContext> Take(ICombatantTargetSelector who, string id) =>
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(who, new StatusDefinitionId(id));

        // Read at the player's turn start, so the round's season stands for both turns of it.
        var turn = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(Stag)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(odd,
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [Take(Stag, SeasonClosedId), Put(Stag, SeasonOpenId), Put(Applicant, HuntersLicenceId)]),
                @else: new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [Take(Stag, SeasonOpenId), Take(Applicant, HuntersLicenceId), Put(Stag, SeasonClosedId)])));

        var deed = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wearing<CardPlayedTriggeredEffectContext>(Stag, SeasonClosedId), PlayedIs(Cards.CardAuthoring.DeedTag)),
            Breach<CardPlayedTriggeredEffectContext>(Stag));

        // With the Stag gone, the season closes on you too.
        var fallen = new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(StagLawId)),
            new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(Applicant, new StatusDefinitionId(HuntersLicenceId)));

        return Rule(StagLawId, "The Hunting Season",
            "Odd rounds: violence approved, your Deeds and its attacks deal 50% more. Even rounds: every Deed breaks its law.",
            [
                On("TurnStarted", turn, StatusTriggerScope.Anywhere),
                On("CardPlayed", deed, StatusTriggerScope.Anywhere),
                On("Downed", fallen, StatusTriggerScope.Anywhere),
            ]);
    }

    private static EffectProgram<EnemyActionContext>? StagIntent(string enemyId, string intentId) => null;

    private static IEffectNode<EnemyActionContext> Blow(int damage) =>
        new DealDamageNode<EnemyActionContext>(
            Applicant, new ConstantExpression<EnemyActionContext>(damage));
}
