using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 1 — The Road of Permitted Turns. The three identities that teach the act's vocabulary:
// a law with an author, a custom that outlives the reason for it, and a bystander who remembers.
public static partial class ActThree
{
    // ── Stage 1 — The Road of Permitted Turns ─────────────────────────────────────────────────────────────
    //
    // Each identity's Local Law is ONE status, carried by the party whose law it is, scoped Anywhere — so the
    // status is not the event's subject but only the rule's licence, and the rule fires on what the PLAYER
    // does. The status is also how the program finds its own author again: the acting source of a rule that
    // answers a card play is the player, and a Trespass filed in the player's name would never mature into
    // anybody's Claim, so every application names its lawgiver explicitly.

    public const string NoHastyPassageId = "no_hasty_passage";
    public const string FirstUseBecameCustomId = "first_use_became_custom";
    public const string EveryDetourLeavesAStoneId = "every_detour_leaves_a_stone";
    public const string DetourStoneId = "detour_stone";
    public const string DetourLatchId = "detour_recorded_this_turn";

    // Which card type the combat's first non-Junk play made customary: 1 Deed, 2 Working, 3 Rite. Kept on the
    // player, the one combatant every part of the program can address with a single selector.
    public static CounterId CustomaryUseCounter => new("customary_use");

    private const int DetourStonesPerClaim = 2;

    // "If the player plays a third card during a player turn: 1 Trespass. Once per player turn." The count is
    // its own latch — a turn passes through exactly three played cards once.
    public static StatusData NoHastyPassage()
    {
        var player = CombatantTargetSelectors.Source;

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                Violate<CardPlayedTriggeredEffectContext>(Lawgiver(NoHastyPassageId), HastyPassageLaw)));

        return Rule(NoHastyPassageId, "No Hasty Passage",
            "The third card you play in a turn is a hasty passage: 1 Trespass owed to the Permit Hare.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // "The first non-Junk card played in the combat establishes its card type as Customary Use. From then on,
    // a turn whose first non-Junk card is of another type is a Trespass."
    //
    // Once per turn without a latch: the rule only ever asks on the turn's FIRST non-Junk card, and a turn has
    // one of those. The same reading is why no turn gate is needed for "beginning with the next player turn" —
    // the card that sets the custom is itself the first non-Junk card of its turn, and it takes the other
    // branch.
    public static StatusData FirstUseBecameCustom()
    {
        var player = CombatantTargetSelectors.Source;
        var clerk = Lawgiver(FirstUseBecameCustomId);

        // The custom has not been established yet: whatever type this card is becomes the procedure.
        IEffectNode<CardPlayedTriggeredEffectContext> Record(string tag, int type) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, CustomaryUseCounter,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(type), relative: false));

        // The custom stands and this card departs from it.
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Departs(string tag, int type) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, CustomaryUseCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(type)),
                new NotExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag))));

        var body = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, CustomaryUseCounter),
                ComparisonOperator.Equal,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                Record(Cards.CardAuthoring.DeedTag, 1),
                Record(Cards.CardAuthoring.WorkingTag, 2),
                Record(Cards.CardAuthoring.RiteTag, 3),
            ]),
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    Departs(Cards.CardAuthoring.DeedTag, 1),
                    new OrExpression<CardPlayedTriggeredEffectContext>(
                        Departs(Cards.CardAuthoring.WorkingTag, 2),
                        Departs(Cards.CardAuthoring.RiteTag, 3))),
                Violate<CardPlayedTriggeredEffectContext>(clerk, CustomaryUseLaw)));

        // …and none of it is asked of a Junk card, or of any card after the turn's first real one.
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(Cards.CardAuthoring.JunkTag))),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(player),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                body));

        return Rule(FirstUseBecameCustomId, "The First Use Became Custom",
            "However this combat's first real card was played, that is the procedure. Open a later turn with "
            + "another kind of card and you owe the Mossbound Clerk 1 Trespass.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // How many real cards have been played this turn — the Junk the fight hands you does not count as a use.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> NonJunkPlayedThisTurn(
        ICombatantTargetSelector player) =>
        new SubtractExpression<CardPlayedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
            new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                player, new TagId(Cards.CardAuthoring.JunkTag)));

    // The Cairn is a support identity and reads the fight rather than pressing it: the first Trespass the
    // player ACTUALLY receives each turn from somebody else leaves a stone, and two stones become somebody
    // else's standing. Prevented Trespass leaves no stone, which falls out of watching StatusApplied — a
    // refused application never lands.
    public static StatusData EveryDetourLeavesAStone()
    {
        // A Trespass application: the source is the party that filed it, the event target is the player.
        var filer = CombatantTargetSelectors.Source;
        var cairn = Lawgiver(EveryDetourLeavesAStoneId);
        var somebodyElse = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.Except(
                CombatantTargetSelectors.AllAlliesOfSource,
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
                    new StatusDefinitionId(EveryDetourLeavesAStoneId))));

        var record = new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
        [
            new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                cairn, new StatusDefinitionId(DetourLatchId),
                new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
            new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                cairn, new StatusDefinitionId(DetourStoneId),
                new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                        cairn, new StatusDefinitionId(DetourStoneId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(DetourStonesPerClaim)),
                new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<StatusAppliedTriggeredEffectContext>(
                        cairn, new StatusDefinitionId(DetourStoneId)),
                    CreateClaim<StatusAppliedTriggeredEffectContext>(somebodyElse),
                ])),
        ]);

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        // …filed by somebody OTHER than the Cairn…
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                                filer, new StatusDefinitionId(EveryDetourLeavesAStoneId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                        // …and not already recorded this turn.
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                                cairn, new StatusDefinitionId(DetourLatchId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)))),
                record));

        // The latch is released when the PLAYER's turn starts, so "the first time each player turn" means the
        // player's turns and not the Cairn's.
        var clear = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Lawgiver(EveryDetourLeavesAStoneId), new StatusDefinitionId(DetourLatchId))));

        return Rule(EveryDetourLeavesAStoneId, "Every Detour Leaves a Stone",
            "The first Trespass you actually take from somebody else each turn leaves the Cairn a stone. At 2 "
            + "stones the Cairn spends them, and another party gains a Claim.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    clear, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData DetourStone() => new()
    {
        Id = DetourStoneId,
        NameKey = "Detour Stone",
        DescriptionKey = "One remembered departure from the sanctioned path. Two of them become somebody's Claim.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData DetourLatch() => new()
    {
        Id = DetourLatchId,
        NameKey = "Detour Recorded",
        DescriptionKey = "This turn's departure has already been written down.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
