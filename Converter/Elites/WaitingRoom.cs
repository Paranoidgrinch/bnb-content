using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── The Waiting Room Eats the Day (Act I elite, the Queue's examiner) ────────────────────────────────────────
//
// Reworked in the content cut of 2026-10-29. The Queue stage teaches that how MANY cards you play is watched;
// its elite asks for an exact number. A board above the door shows the number now being served, 1 to 4, from
// the start of each of your turns. End the turn having played exactly that many cards and your number is
// called: through your whole next turn the Room has no Block and takes double damage. Miss it and another
// Waiting Patron sits down (three at most) — a small body that sighs 3 damage at you every enemy turn, and
// that the Room's own attack counts.
//
// Everything is on chips: the number (Now Serving, as stacks), the call (Your Number Is Called, then At the
// Counter while it pays out) and each Patron's sigh.
public static class WaitingRoom
{
    public const string EnemyId = "devouring_waiting_room";
    public const string PatronEnemyId = "waiting_patron";

    public const string RuleId = "the_waiting_room";
    public const string NowServingId = "now_serving";
    public const string CalledId = "number_called";
    public const string AtTheCounterId = "at_the_counter";
    public const string PatronId = "waiting_patron";

    // The board's order, one number per player turn and then round again. It never asks for the same number
    // twice running, and a 1 follows a 3 so a turn spent on the big number is answered by an easy one.
    public static readonly int[] Board = [2, 3, 1, 4];

    public const int MaxPatrons = 3;
    public const int PatronHealth = 10;
    public const int PatronSigh = 3;
    private const int WaitBase = 5;
    private const int WaitPerPatron = 3;

    private static CounterId BoardStep => new("board_step");

    private static ICombatantTargetSelector Room { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(RuleId)));

    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId)));

    private static ICombatantTargetSelector Patrons { get; } =
        CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(PatronId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(RuleId, "The Waiting Room", false,
            "Play exactly the number on the board in a turn: your number is called. Miss it: another Waiting Patron sits down (max 3).")
            with { Triggers = [Closing()] },
        Marker(NowServingId, "Now Serving", true,
            "The number being served this turn. Play exactly this many cards and your number is called."),
        Marker(CalledId, "Your Number Is Called", false,
            "Your next turn, the Room is at the counter: no Block, and it takes double damage."),
        new StatusData
        {
            Id = AtTheCounterId,
            NameKey = "At the Counter",
            DescriptionKey = "Your number was called: this turn the Room has no Block and takes double damage.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 200),
            ],
            Triggers = [],
        },
        Patron(),
    ];

    // The two moments the Room watches are the PLAYER's turn, so they are encounter triggers: their source is
    // the combatant whose turn it is, and the applicant's marker says it was the player's.
    public static IReadOnlyList<EncounterTriggerData> Triggers() => [OpenTheBoard(), CallTheNumber()];

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "everyone_waits_longer" => EveryoneWaitsLonger(),
        _ => null,
    };

    // ── the start of your turn: a called number is paid out, and the board moves on ────────────────────────
    private static EncounterTriggerData OpenTheBoard()
    {
        var step = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Room, BoardStep);

        IEffectNode<TurnStartedTriggeredEffectContext> Show(int index) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(step, Const<TurnStartedTriggeredEffectContext>(Board.Length)),
                    ComparisonOperator.Equal,
                    Const<TurnStartedTriggeredEffectContext>(index)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Room, new StatusDefinitionId(NowServingId), Const<TurnStartedTriggeredEffectContext>(Board[index])));

        var body = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            // The call pays out now: the Room steps up to the counter for this whole turn.
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(Room, new StatusDefinitionId(CalledId)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Room, new StatusDefinitionId(CalledId)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Room, new StatusDefinitionId(AtTheCounterId), Const<TurnStartedTriggeredEffectContext>(1)),
                    new ModifyDefensivePoolNode<TurnStartedTriggeredEffectContext>(
                        Room, StandardCombatIds.BlockDefensivePool,
                        new SubtractExpression<TurnStartedTriggeredEffectContext>(
                            Const<TurnStartedTriggeredEffectContext>(0),
                            new CombatantDefensivePoolExpression<TurnStartedTriggeredEffectContext>(
                                Room, StandardCombatIds.BlockDefensivePool))),
                ])),
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Room, new StatusDefinitionId(NowServingId)),
            .. Enumerable.Range(0, Board.Length).Select(Show),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Room, BoardStep, Const<TurnStartedTriggeredEffectContext>(1), relative: true),
        ]);

        return Encounter("TurnStarted", PlayersTurn<TurnStartedTriggeredEffectContext>(body));
    }

    // ── the end of your turn: exactly the number, or another patron ────────────────────────────────────────
    private static EncounterTriggerData CallTheNumber()
    {
        var exact = new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source),
            ComparisonOperator.Equal,
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(Room, new StatusDefinitionId(NowServingId)));

        var sitDown = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CountTargetsExpression<TurnEndedTriggeredEffectContext>(Patrons),
                ComparisonOperator.Less,
                Const<TurnEndedTriggeredEffectContext>(MaxPatrons)),
            new SummonCombatantNode<TurnEndedTriggeredEffectContext>(
                StandardCombatIds.EnemyTeam,
                Const<TurnEndedTriggeredEffectContext>(PatronHealth),
                new CombatantDefinitionId(PatronEnemyId),
                "Waiting Patron",
                startingStatuses: [new StatusGrant(new StatusDefinitionId(PatronId), 1)]));

        var body = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            // The turn the Room stood at the counter is over.
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(Room, new StatusDefinitionId(AtTheCounterId)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                exact,
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Room, new StatusDefinitionId(CalledId), Const<TurnEndedTriggeredEffectContext>(1)),
                sitDown),
        ]);

        return Encounter("TurnEnded", PlayersTurn<TurnEndedTriggeredEffectContext>(body));
    }

    // ── the patrons ────────────────────────────────────────────────────────────────────────────────────────
    //
    // A summoned body has no action script (the intent selector only knows the roster the fight opened with), so
    // a Patron acts the way every summon in this engine acts: through its marker's own turn-start program.
    private static StatusData Patron() => new()
    {
        Id = PatronId,
        NameKey = "Waiting Patron",
        DescriptionKey = $"Waiting to be seen. Every enemy turn it sighs at you for {PatronSigh} damage.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            new StatusTriggerData(nameof(TriggerEvent.TurnStarted), JsonSerializer.SerializeToElement(
                new EffectProgram<TurnStartedTriggeredEffectContext>(
                    new DealDamageNode<TurnStartedTriggeredEffectContext>(
                        Applicant, Const<TurnStartedTriggeredEffectContext>(PatronSigh))),
                CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
        ],
    };

    // "Everyone Waits Longer": 5 damage, and 3 more for every Patron in the room.
    private static EffectProgram<EnemyActionContext> EveryoneWaitsLonger() =>
        new(new DealDamageNode<EnemyActionContext>(
            Applicant,
            new AddExpression<EnemyActionContext>(
                Const<EnemyActionContext>(WaitBase),
                new MultiplyExpression<EnemyActionContext>(
                    new CountTargetsExpression<EnemyActionContext>(Patrons),
                    Const<EnemyActionContext>(WaitPerPatron)))));

    // When the Room falls, the office closes and the patrons go home: the fight is the Room, not its queue.
    private static StatusTriggerData Closing() =>
        new(nameof(TriggerEvent.Downed), JsonSerializer.SerializeToElement(
            new EffectProgram<CombatantDownedTriggeredEffectContext>(
                new SetHealthNode<CombatantDownedTriggeredEffectContext>(
                    Patrons, Const<CombatantDownedTriggeredEffectContext>(0))),
            CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>()));

    // ── idioms ─────────────────────────────────────────────────────────────────────────────────────────────

    // The player's turn, while the Room still stands: a fallen Room calls nobody and seats nobody.
    private static EffectProgram<TContext> PlayersTurn<TContext>(IEffectNode<TContext> body) where TContext : class =>
        new(new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    Const<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CountTargetsExpression<TContext>(
                        CombatantTargetSelectors.WithStatus(
                            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(RuleId))),
                    ComparisonOperator.Greater,
                    Const<TContext>(0))),
            body));

    private static EncounterTriggerData Encounter<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static ConstantExpression<TContext> Const<TContext>(int value) where TContext : class => new(value);

    private static StatusData Marker(string id, string name, bool stacks, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = stacks,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
