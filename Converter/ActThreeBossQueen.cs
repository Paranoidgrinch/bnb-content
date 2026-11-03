using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 5 — **The Queen Under the Hill** (392 HP).
//
// The Hill Court has one law: the first card you play each turn must be a Working — anything else breaks it.
// Keep her law three turns running and she is satisfied: she sits out her next action. From half her health the
// court tightens: the first TWO cards each turn must be Workings.
//
// REWORKED 2026-11-03 (user, Act III pass): Favour, the Audiences, the Right of Audience, the Granted Name and the
// Wergild are gone.
public static partial class ActThree
{
    public const string QueenEnemyId = "queen_under_the_hill";
    public const string QueenId = "the_hill_court";
    public const string CourtKeptId = "court_kept";
    public const string AudienceGrantedId = "audience_granted";
    public const string CourtTightenedId = "the_court_tightens";

    public const int CourtTurnsForAudience = 3;

    private static ICombatantTargetSelector HillQueen { get; } = Elite(QueenId);

    private static IEnumerable<StatusData> HillQueenStatuses() =>
    [
        TheHillCourt(),
        new StatusData
        {
            Id = CourtKeptId,
            NameKey = "Court Kept",
            DescriptionKey = "Turns in a row you kept her law. At 3 she sits out her next action.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
        },
        Marker(AudienceGrantedId, "Satisfied", "You kept her law three turns running: she sits out her next action."),
        Marker(CourtTightenedId, "The Court Tightens", "From half her health your first two cards each turn must be Workings."),
    ];

    private static StatusData TheHillCourt()
    {
        var opening = new OrExpression<CardPlayedTriggeredEffectContext>(
            Is(PlayedThisTurn, ComparisonOperator.Equal, 1),
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Wearing<CardPlayedTriggeredEffectContext>(HillQueen, CourtTightenedId),
                Is(PlayedThisTurn, ComparisonOperator.Equal, 2)));
        var play = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Alive<CardPlayedTriggeredEffectContext>(HillQueen),
                new AndExpression<CardPlayedTriggeredEffectContext>(opening,
                    new NotExpression<CardPlayedTriggeredEffectContext>(PlayedIs(Cards.CardAuthoring.WorkingTag)))),
            Breach<CardPlayedTriggeredEffectContext>(HillQueen));

        // A turn kept is a mark; three in a row and she is satisfied. A turn broken wipes the marks.
        var kept = new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(HillQueen, new StatusDefinitionId(CourtKeptId));
        var reckon = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(), Alive<TurnEndedTriggeredEffectContext>(HillQueen)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Wearing<TurnEndedTriggeredEffectContext>(HillQueen, LawBrokenThisTurnId),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(HillQueen, new StatusDefinitionId(CourtKeptId)),
                @else: new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(HillQueen, new StatusDefinitionId(CourtKeptId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        Is(kept, ComparisonOperator.GreaterOrEqual, CourtTurnsForAudience),
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(HillQueen, new StatusDefinitionId(CourtKeptId)),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(HillQueen, new StatusDefinitionId(AudienceGrantedId),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        ])),
                ])));

        return Rule(QueenId, "The Hill Court",
            "Your first card each turn must be a Working, or you break her law. Keep it 3 turns running and she sits out a turn.",
            [
                On("CardPlayed", play, StatusTriggerScope.Anywhere),
                On("TurnEnded", reckon, StatusTriggerScope.Anywhere),
                On("DamageTaken", OnceBelow(50, CourtTightenedId, new NoOpEffectNode<DamageReceivedTriggeredEffectContext>())),
            ]);
    }

    // Her actions, each one sat out once if she is satisfied.
    private static EffectProgram<EnemyActionContext>? HillQueenIntent(string enemyId, string intentId)
    {
        if (enemyId != QueenEnemyId)
            return null;
        var self = CombatantTargetSelectors.Source;
        ConstantExpression<EnemyActionContext> N(int value) => new(value);
        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            "open_the_hill_registry" => Blow(16),
            "call_in_the_gift" => Blow(14),
            "royal_subterranean_correction" => new CausalSequenceEffectNode<EnemyActionContext>(
                [Blow(14), new ApplyStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(Cards.Keywords.Doubt), N(1))]),
            "count_every_buried_name" => new GainBlockNode<EnemyActionContext>(self, N(14)),
            "a_gift_in_return" => new ApplyStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(SafeConductId), N(1),
                sourceSelector: self),
            "no_further_gift" => new GainBlockNode<EnemyActionContext>(self, N(18)),
            "hill_court_final_order" => Blow(22),
            _ => null,
        };
        return act is null ? null : new(new ConditionalEffectNode<EnemyActionContext>(
            Wearing<EnemyActionContext>(self, AudienceGrantedId),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(AudienceGrantedId)),
            @else: act));
    }
}
