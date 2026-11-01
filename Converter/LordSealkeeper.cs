using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's third boss: "Choose which protection disappears first, then decide how to spend the authority you
// broke loose."
//
// Three Great Seals stand as statuses on the Sealkeeper. Each player turn they raise a Seal Ward of Block, 6 per
// Seal standing; strip that Block and draw blood in one turn and a Seal breaks.
//
// SIMPLIFIED AGAIN on 2026-11-01 (user: every rule in one clear sentence): the three Seals used to do three
// different things, the player chose which to break from three cards laid into the hand, the break cracked the
// Keeper for a turn and left a Fragment card behind. Now the Seals are alike and one simply breaks.
//
// SIMPLIFIED in the content cut of 2026-10-29 (it had seventeen special moves): every broken Seal moves the
// Keeper into its next phase, and a phase is two or three moves in a loop. Three Seals: the Keeper presses its
// authority. One broken: it cracks and certifies. Two broken: it is unsealed and hits with what is left.
// Deviations: ADAPTATIONS.md.
public static class LordSealkeeper
{
    public const string SealkeeperId = "the_sealkeeper";
    public const string AccessSealId = "great_seal_of_access";
    public const string TestimonySealId = "great_seal_of_testimony";
    public const string ExecutionSealId = "great_seal_of_execution";
    public const string BreakUsedId = "a_seal_has_broken";           // one break per player turn, on the player
    public const string CrackingId = "the_keeper_cracks";            // Phase II (one Seal broken), on the boss
    public const string UnsealedId = "unsealed_authority";           // Phase III (two broken), on the boss

    // Where the Keeper is in its phase's loop: Phases II and III read it through their intent rules (Phase I
    // is the plain cycle). It goes back to the start whenever a Seal breaks.
    public static CounterId PhaseBeatCounter => new("phase_beat");
    public const int PhaseTwoMoves = 3;
    public const int PhaseThreeMoves = 2;

    public sealed record Seal(string SealId, string SealName, string SealText);

    public const int WardPerSeal = 6;
    private const string SealText = "Adds 6 Block to its Seal Ward each of your turns.";

    // The order they break in (the last first): alike in what they do, they differ only in name.
    public static readonly Seal[] Seals =
    [
        new(AccessSealId, "Great Seal of Access", SealText),
        new(TestimonySealId, "Great Seal of Testimony", SealText),
        new(ExecutionSealId, "Great Seal of Execution", SealText),
    ];

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheSealkeeper(),
        .. Seals.Select(seal => PassiveStatuses.NamedMarker(seal.SealId, seal.SealName, seal.SealText)),
        PassiveStatuses.NamedMarker(BreakUsedId, "A Seal Has Broken",
            "One Seal has already been broken this turn. The next has to wait."),
        PassiveStatuses.NamedMarker(CrackingId, "The Keeper Cracks",
            "Phase II: one Seal is broken. It certifies and denies: Seal the Record, Certified Denial, Unsealed Verdict."),
        PassiveStatuses.NamedMarker(UnsealedId, "Unsealed Authority",
            "Phase III: two Seals are broken. It hits with what is left: Stamp the Flesh, Authority Without Seal."),
    ];

    public static IEnumerable<CardData> Cards() => [];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheSealWardRises(),
        ASealMayBreak(),
    ];

    // ── Phase I: the Seals ────────────────────────────────────────────────────

    // The Seal Ward goes up at the start of every player turn: 6 Block per standing Seal. Raised HERE rather than at the boss's own turn start because Block is cleared
    // at a combatant's turn start after its triggers run — and because the Ward's whole job is to stand between
    // the player and the Keeper.
    private static EncounterTriggerData TheSealWardRises()
    {
        var player = CombatantTargetSelectors.Source;
        var keeper = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(SealkeeperId));

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Standing(string sealId) =>
            new IterationTargetStatusStacksExpression<TurnStartedTriggeredEffectContext>(new StatusDefinitionId(sealId));

        var ward = new MultiplyExpression<TurnStartedTriggeredEffectContext>(
            new AddExpression<TurnStartedTriggeredEffectContext>(
                Standing(AccessSealId),
                new AddExpression<TurnStartedTriggeredEffectContext>(
                    Standing(TestimonySealId), Standing(ExecutionSealId))),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(WardPerSeal));

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                {
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(keeper,
                        new SequenceEffectNode<TurnStartedTriggeredEffectContext>(new IEffectNode<TurnStartedTriggeredEffectContext>[]
                        {
                            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, ward),
                        })),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(BreakUsedId)),
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // Strip the Ward and draw blood in the same turn and a Seal breaks. One break per player turn.
    private static EncounterTriggerData ASealMayBreak()
    {
        var keeper = CombatantTargetSelectors.EventTarget;
        var applicant = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId));

        ICombatExpression<DamageReceivedTriggeredEffectContext, int> Stacks(
            ICombatantTargetSelector target, string statusId) =>
            target == keeper
                ? new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                    keeper, new StatusDefinitionId(statusId))
                : new CountTargetsExpression<DamageReceivedTriggeredEffectContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(statusId)));

        var sealsStanding = new AddExpression<DamageReceivedTriggeredEffectContext>(
            Stacks(keeper, AccessSealId),
            new AddExpression<DamageReceivedTriggeredEffectContext>(
                Stacks(keeper, TestimonySealId), Stacks(keeper, ExecutionSealId)));

        // One Seal breaks — the last still standing — and the Keeper moves into its next phase, from the top of its
        // loop.
        IEffectNode<DamageReceivedTriggeredEffectContext> Break(string sealId) =>
            new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(keeper, new StatusDefinitionId(sealId)),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(keeper, new StatusDefinitionId(CrackingId)),
                    new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(keeper, new StatusDefinitionId(CrackingId)),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            keeper, new StatusDefinitionId(UnsealedId), new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
                    ]),
                    new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                        new NotExpression<DamageReceivedTriggeredEffectContext>(
                            new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(keeper, new StatusDefinitionId(UnsealedId))),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            keeper, new StatusDefinitionId(CrackingId), new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)))),
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    keeper, PhaseBeatCounter, new ConstantExpression<DamageReceivedTriggeredEffectContext>(0), relative: false),
            ]);
        IEffectNode<DamageReceivedTriggeredEffectContext> BreakTheLast(int index) =>
            index < 0
                ? new SequenceEffectNode<DamageReceivedTriggeredEffectContext>([])
                : new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        Stacks(keeper, Seals[index].SealId), ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    Break(Seals[index].SealId),
                    BreakTheLast(index - 1));

        var offer = new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(
        [
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                applicant, new StatusDefinitionId(BreakUsedId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
            BreakTheLast(Seals.Length - 1),
        ]);

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        Stacks(keeper, SealkeeperId), ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        // The Ward has to be gone: the blow has to draw blood.
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        new AndExpression<DamageReceivedTriggeredEffectContext>(
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                sealsStanding, ComparisonOperator.Greater,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                            // …and only once per player turn.
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new CountTargetsExpression<DamageReceivedTriggeredEffectContext>(
                                    CombatantTargetSelectors.WithStatus(
                                        CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(BreakUsedId))),
                                ComparisonOperator.Equal,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0))))),
                offer));

        return new EncounterTriggerData("DamageTaken",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>()));
    }

    // ── The Keeper's own machinery ────────────────────────────────────────────

    private static StatusData TheSealkeeper()
    {
        var self = CombatantTargetSelectors.Source;
        var beat = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, PhaseBeatCounter);

        // The phase loop moves on one step per Keeper turn: three moves in Phase II, two in Phase III.
        IEffectNode<TurnEndedTriggeredEffectContext> Step(int length) =>
            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                self, PhaseBeatCounter,
                new RemainderExpression<TurnEndedTriggeredEffectContext>(
                    new AddExpression<TurnEndedTriggeredEffectContext>(
                        beat, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(length)),
                relative: false);

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(UnsealedId)),
                Step(PhaseThreeMoves),
                Step(PhaseTwoMoves)));

        return new StatusData
        {
            Id = SealkeeperId,
            NameKey = "The Lord Sealkeeper",
            DescriptionKey = "3 Seals give it 6 Block each turn. Break that Block and wound it in one turn: a Seal breaks, "
+ "and its next phase begins.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers =
            [
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ],
        };
    }
}
