using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act I's third boss: "Choose which protection disappears first, then decide how to spend the authority you
// broke loose."
//
// Three Great Seals stand as statuses on the Sealkeeper. Each player turn they raise a Seal Ward of Block;
// stripping that Block and drawing blood breaks one Seal of the player's choosing — and the broken Seal leaves
// a Fragment in the player's hand, a one-use piece of the boss's own authority. Once every Seal is gone (or the
// Keeper is down to 58 HP) it unseals itself, and in Phase II it tries to reclaim the Fragments still unspent.
//
// Every Fragment therefore exists twice: as a card the player holds, and as an "outstanding" marker on the
// Sealkeeper — that marker is how the boss's own programs know what is still out there, since a boss program
// can never read the player's hand or statuses. Deviations: ADAPTATIONS.md.
public static class LordSealkeeper
{
    public const string SealkeeperId = "the_sealkeeper";
    public const string AccessSealId = "great_seal_of_access";
    public const string TestimonySealId = "great_seal_of_testimony";
    public const string ExecutionSealId = "great_seal_of_execution";
    public const string TestimonyNotedId = "testimony_recorded";     // once-per-turn latch, on the boss
    public const string CrackedId = "cracked";                       // on the boss, until the turn ends
    public const string BreakReadyId = "a_seal_may_break";           // on the player
    public const string BreakUsedId = "a_seal_has_broken";           // one break per player turn, on the player
    public const string UnsealPendingId = "the_keeper_unsealed_next"; // telegraph, on the boss
    public const string UnsealedId = "unsealed_authority";           // Phase II, on the boss
    public const string BluntedId = "blunted_stamp";                 // Fragment of Execution, on the boss
    public const string ReclaimPendingId = "reclaiming_a_fragment";  // telegraph, on the boss

    public static readonly CounterId SealBeatCounter = new("seal_beat");
    public const int SealBeats = 6;
    public const int UnsealHealth = 58;

    // The three Seals, their break card, their Fragment card and the marker that says the Fragment is still
    // out there. Order is the order Reclaim works through them.
    public sealed record Seal(
        string SealId, string SealName, string SealText,
        string BreakCardId, string FragmentCardId, string OutstandingId, string FragmentName, string FragmentText);

    public static readonly Seal[] Seals =
    [
        new(AccessSealId, "Great Seal of Access", "The Seal Ward is 4 Block stronger.",
            "break_the_seal_of_access", "fragment_of_access", "fragment_of_access_outstanding",
            "Fragment of Access", "Remove up to 12 Block from the Lord Sealkeeper."),
        new(TestimonySealId, "Great Seal of Testimony",
            "The first status you land on the Sealkeeper each turn gives it 5 Block.",
            "break_the_seal_of_testimony", "fragment_of_testimony", "fragment_of_testimony_outstanding",
            "Fragment of Testimony", "Remove 2 stacks of Paperwork from yourself."),
        new(ExecutionSealId, "Great Seal of Execution", "The Sealkeeper's attacks deal 4 more.",
            "break_the_seal_of_execution", "fragment_of_execution", "fragment_of_execution_outstanding",
            "Fragment of Execution", "The Sealkeeper's next attack deals 8 less."),
    ];

    // ── Content ───────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        TheSealkeeper(),
        PassiveStatuses.NamedMarker(AccessSealId, Seals[0].SealName, Seals[0].SealText),
        PassiveStatuses.NamedMarker(TestimonySealId, Seals[1].SealName, Seals[1].SealText),
        GreatSealOfExecution(),
        PassiveStatuses.NamedMarker(TestimonyNotedId, "Testimony Recorded", null),
        PassiveStatuses.NamedMarker(BreakReadyId, "A Seal May Break",
            "Choose a Great Seal to break this turn."),
        PassiveStatuses.NamedMarker(BreakUsedId, "A Seal Has Broken", null),
        PassiveStatuses.NamedMarker(UnsealPendingId, "The Keeper Unseals Itself",
            "Its next action sheds what is left of the Seals."),
        PassiveStatuses.NamedMarker(UnsealedId, "Unsealed Authority", "Phase II."),
        PassiveStatuses.NamedMarker(ReclaimPendingId, "Reclaiming a Fragment",
            "Its next action takes back a Fragment you have not spent."),
        .. Seals.Select(s => PassiveStatuses.NamedMarker(s.OutstandingId, s.FragmentName + " (unspent)", null)),
        Cracked(),
        BluntedStamp(),
    ];

    public static IEnumerable<CardData> Cards() =>
        [.. Seals.Select(BreakCard), .. Seals.Select(FragmentCard)];

    public static IReadOnlyList<EncounterTriggerData> Triggers() =>
    [
        TheSealWardRises(),
        TheCrackCloses(),
        ASealMayBreak(),
        TestimonyIsRecorded(),
    ];

    // ── Phase I: the Seals ────────────────────────────────────────────────────

    // The Seal Ward goes up at the start of every player turn: 4 Block per standing Seal, and 4 more while the
    // Seal of Access still holds. Raised HERE rather than at the boss's own turn start because Block is cleared
    // at a combatant's turn start after its triggers run — and because the Ward's whole job is to stand between
    // the player and the Keeper.
    private static EncounterTriggerData TheSealWardRises()
    {
        var player = CombatantTargetSelectors.Source;
        var keeper = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(SealkeeperId));

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Standing(string sealId) =>
            new IterationTargetStatusStacksExpression<TurnStartedTriggeredEffectContext>(new StatusDefinitionId(sealId));

        var ward = new AddExpression<TurnStartedTriggeredEffectContext>(
            new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                new AddExpression<TurnStartedTriggeredEffectContext>(
                    Standing(AccessSealId),
                    new AddExpression<TurnStartedTriggeredEffectContext>(
                        Standing(TestimonySealId), Standing(ExecutionSealId))),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(4)),
            // The Seal of Access thickens the Ward by another 4.
            new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                Standing(AccessSealId), new ConstantExpression<TurnStartedTriggeredEffectContext>(4)));

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
                            // Both once-per-player-turn latches open again.
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(TestimonyNotedId)),
                        })),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        player, new StatusDefinitionId(BreakUsedId)),
                })));

        return new EncounterTriggerData("TurnStarted",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()));
    }

    // Cracked lasts until the end of the player's turn, and an unused break offer expires with it.
    private static EncounterTriggerData TheCrackCloses()
    {
        var player = CombatantTargetSelectors.Source;
        var keeper = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(SealkeeperId));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
                {
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(keeper, new StatusDefinitionId(CrackedId)),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        player, new StatusDefinitionId(BreakReadyId)),
                })));

        return new EncounterTriggerData("TurnEnded",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()));
    }

    // Strip the Ward and draw blood in the same turn and a Seal may be broken — the player picks which one from
    // three offers laid into their hand. One break per player turn.
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

        var offer = new SequenceEffectNode<DamageReceivedTriggeredEffectContext>(
        [
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                applicant, new StatusDefinitionId(BreakReadyId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                applicant, new StatusDefinitionId(BreakUsedId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
            // The Keeper is Cracked from this moment until the turn ends.
            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                keeper, new StatusDefinitionId(CrackedId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(1)),
            .. Seals.Select(s => (IEffectNode<DamageReceivedTriggeredEffectContext>)
                new CreateCardInstanceNode<DamageReceivedTriggeredEffectContext>(
                    applicant, new CardDefinitionId(s.BreakCardId), CardZone.Hand,
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(1))),
        ]);

        var program = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        Stacks(keeper, SealkeeperId), ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        // The Ward has to be gone: no Block left when the blow lands.
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantDefensivePoolExpression<DamageReceivedTriggeredEffectContext>(
                                keeper, StandardCombatIds.BlockDefensivePool),
                            ComparisonOperator.Equal,
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

    // The Great Seal of Testimony: the first status the player lands on the Keeper each turn is filed as Block.
    private static EncounterTriggerData TestimonyIsRecorded()
    {
        var keeper = CombatantTargetSelectors.EventTarget;
        var applier = CombatantTargetSelectors.Source;

        ICombatExpression<StatusAppliedTriggeredEffectContext, int> Stacks(
            ICombatantTargetSelector target, string statusId) =>
            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                target, new StatusDefinitionId(statusId));

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                        Stacks(keeper, TestimonySealId), ComparisonOperator.Greater,
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        // Only what the PLAYER files counts — never the Keeper's own paperwork.
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            Stacks(applier, PassiveStatuses.ApplicantId), ComparisonOperator.Greater,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            Stacks(keeper, TestimonyNotedId), ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)))),
                new SequenceEffectNode<StatusAppliedTriggeredEffectContext>(new IEffectNode<StatusAppliedTriggeredEffectContext>[]
                {
                    new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                        keeper, new StatusDefinitionId(TestimonyNotedId),
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    new GainBlockNode<StatusAppliedTriggeredEffectContext>(
                        keeper, new ConstantExpression<StatusAppliedTriggeredEffectContext>(5)),
                })));

        return new EncounterTriggerData("StatusApplied",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()));
    }

    // ── The Keeper's own machinery ────────────────────────────────────────────

    private static StatusData TheSealkeeper()
    {
        var self = CombatantTargetSelectors.Source;

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                self, new StatusDefinitionId(statusId));

        var beat = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(self, SealBeatCounter);

        var sealsStanding = new AddExpression<TurnEndedTriggeredEffectContext>(
            Stacks(AccessSealId),
            new AddExpression<TurnEndedTriggeredEffectContext>(Stacks(TestimonySealId), Stacks(ExecutionSealId)));

        var fragmentsOutstanding = new AddExpression<TurnEndedTriggeredEffectContext>(
            Stacks(Seals[0].OutstandingId),
            new AddExpression<TurnEndedTriggeredEffectContext>(
                Stacks(Seals[1].OutstandingId), Stacks(Seals[2].OutstandingId)));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(new IEffectNode<TurnEndedTriggeredEffectContext>[]
            {
                // Nothing left to certify, or too wounded to keep certifying: the unsealing is announced.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(UnsealedId), ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                Stacks(UnsealPendingId), ComparisonOperator.Equal,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                            new OrExpression<TurnEndedTriggeredEffectContext>(
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    sealsStanding, ComparisonOperator.Equal,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(self),
                                    ComparisonOperator.LessOrEqual,
                                    new ConstantExpression<TurnEndedTriggeredEffectContext>(UnsealHealth))))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(UnsealPendingId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),

                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    self, SealBeatCounter,
                    new RemainderExpression<TurnEndedTriggeredEffectContext>(
                        new AddExpression<TurnEndedTriggeredEffectContext>(
                            beat, new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(SealBeats)),
                    relative: false),

                // Reclaim is previewed a full player turn ahead: the beat the rotation is about to reach
                // decides whether the Keeper announces it.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(UnsealedId), ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                beat, ComparisonOperator.Equal,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                fragmentsOutstanding, ComparisonOperator.Greater,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))),
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(ReclaimPendingId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    @else: new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        self, new StatusDefinitionId(ReclaimPendingId))),
            }));

        return new StatusData
        {
            Id = SealkeeperId,
            NameKey = "The Lord Sealkeeper",
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

    // The Seal of Execution simply makes the Keeper's blows land harder. The design gives the bonus to the
    // first direct attack of each boss turn; the Keeper attacks at most once a turn, so the flat modifier is
    // the same thing without a per-turn latch.
    private static StatusData GreatSealOfExecution()
    {
        var seal = PassiveStatuses.NamedMarker(ExecutionSealId, Seals[2].SealName, Seals[2].SealText);
        return seal with
        {
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddFlat, 4, RestrictDamageKind: DamageKind.Direct),
            ],
        };
    }

    private static StatusData Cracked() => new()
    {
        Id = CrackedId,
        NameKey = "Cracked",
        DescriptionKey = "Takes 25 % more damage until the end of your turn.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 125, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    // The Fragment of Execution, once spent: the Keeper's next blow is blunted, and the blunting is used up
    // with it.
    private static StatusData BluntedStamp()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new RemoveStatusNode<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(BluntedId)));

        return new StatusData
        {
            Id = BluntedId,
            NameKey = "Blunted Stamp",
            DescriptionKey = "Its next attack deals 8 less.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddFlat, -8, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // ── The player's cards ────────────────────────────────────────────────────

    // One of three offers, live only for the turn the break was earned: it takes the Seal down and hands the
    // player its Fragment.
    private static CardData BreakCard(Seal seal) => new()
    {
        Id = seal.BreakCardId,
        NameKey = "Break the " + seal.SealName,
        DescriptionKey = $"Shatter the {seal.SealName} and take its Fragment.",
        Costs = [],
        Tags = [new TagId("form"), new TagId("seal")],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(BreakReadyId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0)),
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    new RemoveStatusNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(BreakReadyId)),
                    new ForEachTargetEffectNode<CardPlayContext>(
                        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(seal.SealId)),
                        new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                        {
                            new RemoveStatusNode<CardPlayContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(seal.SealId)),
                            // The Keeper itself remembers that this Fragment is out there.
                            new ApplyStatusNode<CardPlayContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(seal.OutstandingId),
                                new ConstantExpression<CardPlayContext>(1)),
                        })),
                    new CreateCardInstanceNode<CardPlayContext>(
                        CombatantTargetSelectors.Source, new CardDefinitionId(seal.FragmentCardId), CardZone.Hand,
                        new ConstantExpression<CardPlayContext>(1)),
                }))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // A Fragment stays in hand until it is spent — stolen authority does not expire with the turn.
    private static CardData FragmentCard(Seal seal)
    {
        var player = CombatantTargetSelectors.Source;
        var keeper = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(SealkeeperId));

        IEffectNode<CardPlayContext> Effect() => seal.SealId switch
        {
            AccessSealId => new ModifyDefensivePoolNode<CardPlayContext>(
                keeper, StandardCombatIds.BlockDefensivePool, new ConstantExpression<CardPlayContext>(-12)),
            // "Up to 2 stacks of one negative status" — the Keeper's own currency is Paperwork, so that is what
            // the Fragment scrubs off.
            TestimonySealId => new ModifyStatusStacksNode<CardPlayContext>(
                player, new StatusDefinitionId("paperwork"), new ConstantExpression<CardPlayContext>(-2)),
            _ => new ApplyStatusNode<CardPlayContext>(
                keeper, new StatusDefinitionId(BluntedId), new ConstantExpression<CardPlayContext>(1)),
        };

        return new CardData
        {
            Id = seal.FragmentCardId,
            NameKey = seal.FragmentName,
            DescriptionKey = seal.FragmentText,
            Costs = [],
            Tags = [new TagId("form"), new TagId("fragment")],
            Program = new EffectProgram<CardPlayContext>(
                new SequenceEffectNode<CardPlayContext>(new IEffectNode<CardPlayContext>[]
                {
                    Effect(),
                    // Spending it also takes it off the Keeper's list of things to reclaim.
                    new RemoveStatusNode<CardPlayContext>(keeper, new StatusDefinitionId(seal.OutstandingId)),
                })),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            // Kept in hand between turns: the Fragment is a boss-context action, not a card of the deck.
            TurnEndHandDestinationZone = CardZone.Hand,
        };
    }

    // ── Raw intents ───────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "the_keeper_unsealed" => TheKeeperUnsealed(),
        "reclaim_the_seal" => ReclaimTheSeal(),
        _ => null,
    };

    // The transition: whatever is left of the Seals shatters (granting nothing), the Ward falls, and the Keeper
    // stands up in its own authority.
    private static EffectProgram<EnemyActionContext> TheKeeperUnsealed()
    {
        var self = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(
            [
                .. Seals.Select(s => (IEffectNode<EnemyActionContext>)
                    new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(s.SealId))),
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(UnsealPendingId)),
                new ModifyDefensivePoolNode<EnemyActionContext>(
                    self, StandardCombatIds.BlockDefensivePool, new ConstantExpression<EnemyActionContext>(-999)),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(UnsealedId), new ConstantExpression<EnemyActionContext>(1)),
            ]));
    }

    // Reclaim: the first Fragment still unspent comes home, in the Seals' own order. A Fragment the player
    // already spent leaves the Keeper with nothing but a thin guard.
    private static EffectProgram<EnemyActionContext> ReclaimTheSeal()
    {
        var self = CombatantTargetSelectors.Source;

        ICombatExpression<EnemyActionContext, int> Stacks(string statusId) =>
            new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(statusId));

        IEffectNode<EnemyActionContext> Reclaimed(Seal seal) =>
            new SequenceEffectNode<EnemyActionContext>(
            [
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(seal.OutstandingId)),
                .. seal.SealId switch
                {
                    AccessSealId => new IEffectNode<EnemyActionContext>[]
                    {
                        new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(16)),
                    },
                    TestimonySealId =>
                    [
                        new RemoveStatusesByPolarityNode<EnemyActionContext>(self, StatusPolarity.Debuff),
                        new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(6)),
                    ],
                    _ =>
                    [
                        new ApplyStatusNode<EnemyActionContext>(
                            self, new StatusDefinitionId("strength"), new ConstantExpression<EnemyActionContext>(2)),
                    ],
                },
            ]);

        IEffectNode<EnemyActionContext> Chain(int index) =>
            index == Seals.Length
                // Nothing outstanding: the reclamation grasps at air.
                ? new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(5))
                : new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        Stacks(Seals[index].OutstandingId), ComparisonOperator.Greater,
                        new ConstantExpression<EnemyActionContext>(0)),
                    Reclaimed(Seals[index]),
                    @else: Chain(index + 1));

        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(ReclaimPendingId)),
                Chain(0),
            }));
    }
}
