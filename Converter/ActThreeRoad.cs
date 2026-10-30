using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 1 — The Road. Since the content cut of 2026-10-29 every law here is worn where it can be read:
// the Hare's quarry, the Path's arrow and the Cairn's stones are all chips on somebody.
public static partial class ActThree
{
    // ── Permit Hare — One Quarry per Turn ─────────────────────────────────────────────────────────────────
    //
    // "Changing target within a turn is Trespass; a full lawful turn gives a Safe-Conduct." The party your
    // first aimed card went to wears the Quarry mark; aiming at anybody else in the same turn is the breach.

    public const string NoHastyPassageId = "no_hasty_passage";
    public const string QuarryId = "the_quarry";
    public const string QuarrySwitchedId = "quarry_switched";

    public const string EveryDetourLeavesAStoneId = "every_detour_leaves_a_stone";
    public const string DetourStoneId = "detour_stone";

    public const string DestinationRuleId = "destination";
    public const string DestinationId = "the_destination";
    public const string AttendedId = "attended";
    public const string ArrowPointsElsewhereId = "arrow_points_elsewhere";

    // Stones the Cairn holds before it comes down, and how hard it comes down.
    public const int CairnStones = 3;
    public const int CairnCollapseDamage = 18;

    // Whether the card just played was aimed at a party to the fight, and at which one.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> AimedAtAParty() =>
        new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(GreenDocketBodyId));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> AimedAt(string statusId) =>
        new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> Alive<TContext>(ICombatantTargetSelector who)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(who),
            ComparisonOperator.Greater,
            new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> ClearFromParties<TContext>(string statusId) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(
            Parties,
            new RemoveStatusNode<TContext>(CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(statusId)));

    public static StatusData NoHastyPassage()
    {
        var hare = Lawgiver(NoHastyPassageId);
        var quarryStanding = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
            new CountTargetsExpression<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(Parties, new StatusDefinitionId(QuarryId))),
            ComparisonOperator.Greater,
            new ConstantExpression<CardPlayedTriggeredEffectContext>(0));

        var aim = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(Alive<CardPlayedTriggeredEffectContext>(hare), AimedAtAParty()),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new AndExpression<CardPlayedTriggeredEffectContext>(
                            quarryStanding,
                            new NotExpression<CardPlayedTriggeredEffectContext>(AimedAt(QuarryId))),
                        Violate<CardPlayedTriggeredEffectContext>(hare, HastyPassageLaw, QuarrySwitchedId)),
                    ClearFromParties<CardPlayedTriggeredEffectContext>(QuarryId),
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId(QuarryId),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                ])));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    ClearFromParties<TurnStartedTriggeredEffectContext>(QuarryId),
                    ClearFromParties<TurnStartedTriggeredEffectContext>(QuarrySwitchedId),
                ])));

        // A lawful turn: you chose a quarry and kept to it. The Hare stamps your permit.
        var stamp = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        PlayersTurn<TurnEndedTriggeredEffectContext>(),
                        Alive<TurnEndedTriggeredEffectContext>(hare)),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CountTargetsExpression<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.WithStatus(Parties, new StatusDefinitionId(QuarryId))),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new NotExpression<TurnEndedTriggeredEffectContext>(
                                Wears<TurnEndedTriggeredEffectContext>(hare, QuarrySwitchedId)),
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                    Applicant, new StatusDefinitionId(SafeConductId)),
                                ComparisonOperator.Less,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(SafeConductCeiling))))),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: hare)));

        return Rule(NoHastyPassageId, "One Quarry per Turn",
            "Your first aimed card picks this turn's Quarry. Aiming at anyone else: 1 Trespass. Keep to your "
            + "Quarry all turn: 1 Safe-Conduct.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    aim, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    stamp, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData Quarry() =>
        Marker(QuarryId, "Quarry",
            "The Permit Hare's law: this turn your cards go here. Aiming at anyone else is a Trespass.");

    public static StatusData QuarrySwitched() =>
        Marker(QuarrySwitchedId, "Permit Torn",
            "You changed your quarry this turn: no Safe-Conduct from the Hare.");

    // ── Elsewhere Path — the Arrow ────────────────────────────────────────────────────────────────────────
    //
    // "Each turn an arrow marks another enemy as Destination; ending the turn without hitting it is Trespass;
    // the Path is hard to hit while the arrow points elsewhere." Hitting the Destination turns the arrow back
    // to the Path, and the Path can be struck properly again until your next turn.
    public static StatusData Destination()
    {
        var path = Lawgiver(DestinationRuleId);
        var destination = Lawgiver(DestinationId);
        var candidate = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(DestinationRuleId)));

        var visit = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                AimedAt(DestinationId),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId(AttendedId),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        path, new StatusDefinitionId(ArrowPointsElsewhereId)),
                ])));

        var choose = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    Alive<TurnStartedTriggeredEffectContext>(path)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    ClearFromParties<TurnStartedTriggeredEffectContext>(DestinationId),
                    ClearFromParties<TurnStartedTriggeredEffectContext>(AttendedId),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Alive<TurnStartedTriggeredEffectContext>(candidate),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                candidate, new StatusDefinitionId(DestinationId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                path, new StatusDefinitionId(ArrowPointsElsewhereId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        ]),
                        // Nobody else left to point at: the arrow points at the Path itself.
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            path, new StatusDefinitionId(ArrowPointsElsewhereId))),
                ])));

        var reckon = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        PlayersTurn<TurnEndedTriggeredEffectContext>(),
                        Alive<TurnEndedTriggeredEffectContext>(path)),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Alive<TurnEndedTriggeredEffectContext>(destination),
                        new NotExpression<TurnEndedTriggeredEffectContext>(
                            Wears<TurnEndedTriggeredEffectContext>(destination, AttendedId)))),
                Violate<TurnEndedTriggeredEffectContext>(path, DestinationLaw)));

        return Rule(DestinationRuleId, "The Arrow",
            "Each turn the arrow names another party as Destination. Hit it before your turn ends or owe the "
            + "Path 1 Trespass. Until you do, the Path is hard to hit.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    visit, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    choose, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    reckon, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData TheDestination() =>
        Marker(DestinationId, "Destination",
            "The Elsewhere Path's arrow points here. Hit it this turn, or owe the Path 1 Trespass.");

    public static StatusData Attended() =>
        Marker(AttendedId, "Visited", "You went where the arrow pointed this turn.");

    public static StatusData ArrowPointsElsewhere() => new()
    {
        Id = ArrowPointsElsewhereId,
        NameKey = "The Arrow Points Elsewhere",
        DescriptionKey = "Takes half damage until you hit this turn's Destination.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 50),
        ],
        Triggers = [],
    };

    // ── Cairn of Stray Paths — Every Detour Leaves a Stone ───────────────────────────────────────────────
    //
    // "Every Trespass you take adds a stone; at 3 stones it collapses on you for a big hit; each hit on the
    // Cairn removes a stone." The stones are the Cairn's own stacks, so the count is always on screen.
    public static StatusData EveryDetourLeavesAStone()
    {
        var cairn = Lawgiver(EveryDetourLeavesAStoneId);

        var stack = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    Alive<StatusAppliedTriggeredEffectContext>(cairn)),
                new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                        cairn, new StatusDefinitionId(DetourStoneId),
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                                cairn, new StatusDefinitionId(DetourStoneId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(CairnStones)),
                        new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<StatusAppliedTriggeredEffectContext>(
                                cairn, new StatusDefinitionId(DetourStoneId)),
                            new DealDamageNode<StatusAppliedTriggeredEffectContext>(
                                Applicant,
                                new ConstantExpression<StatusAppliedTriggeredEffectContext>(CairnCollapseDamage)),
                        ])),
                ])));

        // A hit on the Cairn knocks a stone loose. Bearer-scoped: it is the Cairn being hit.
        var knock = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(DetourStoneId),
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)));

        return Rule(EveryDetourLeavesAStoneId, "Every Detour Leaves a Stone",
            "Every Trespass you take adds a stone to the Cairn. At 3 stones it collapses on you for 18. "
            + "Each hit on the Cairn knocks a stone loose.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    stack, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    knock, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
            ]);
    }

    public static StatusData DetourStone() => new()
    {
        Id = DetourStoneId,
        NameKey = "Stray Stone",
        DescriptionKey = "One stone per Trespass you took. At 3 the Cairn collapses on you for 18. Hit it to knock one loose.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // How many real cards have been played this turn — the Junk the fight hands you does not count as a use.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> NonJunkPlayedThisTurn(
        ICombatantTargetSelector player) =>
        new SubtractExpression<CardPlayedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
            new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                player, new TagId(Cards.CardAuthoring.JunkTag)));
}
