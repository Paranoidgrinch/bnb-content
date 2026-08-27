using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 5 — **Ant Queen of the Proper Line** (Queen 160 HP, three Bearers of 27).
//
// > The player may obey the formation, spend protection to break it, or create Claims that let the Queen
// > rebuild it.
//
// The Queen barely moves. Three Line-Bearers carry white strips of bark in a measured procession, and the
// whole encounter is one question: **may you choose your own target?** Striking out of order is a Trespass
// owed to the QUEEN — the Bearers hold no standing of their own — and the standing you hand her is what
// pays for the Bearers you killed to come back.
//
// Two readings had to be made explicit and both fall out of the engine rather than being written on top:
//
//   "Directly targets a Bearer" is the card's own target. A card played AT a Bearer names it; an area
//   effect names nobody, and a rule that asks who was named simply finds no one — which is the design's
//   "AoE without a specific Bearer target does not trigger the law", for free.
//   "Bearers never hold Claims" needs no rule at all: the violation is filed in the QUEEN's name, so the
//   three Trespass that mature into standing mature into hers.
public static partial class ActThree
{
    public const string AntQueenEnemyId = "ant_queen_of_the_proper_line";
    public const string AntQueenId = "the_proper_line";
    public const string ReconstructionChargeId = "reconstruction_charge";
    public const string PermittedExceptionId = "permitted_exception";
    public const string LinePendingId = "line_out_of_order";
    public const string LineBrokenThisTurnId = "line_broken_this_turn";

    public const int ProperLineLaw = 18;
    private const int BearerRespawnHealth = 18;
    private const int ReconstructionCharges = 2;
    private const int PermittedExceptionBlock = 6;

    // The three positions, in the order the law and every reconstruction reads them.
    private static readonly (string EnemyId, string Marker, string Name)[] Bearers =
    [
        ("first_line_bearer", "line_bearer_first", "First Line-Bearer"),
        ("second_line_bearer", "line_bearer_second", "Second Line-Bearer"),
        ("third_line_bearer", "line_bearer_third", "Third Line-Bearer"),
    ];

    private static ICombatantTargetSelector Queen { get; } = Elite(AntQueenId);

    // The Bearer standing in that position. Nearly everything means this one: a position with nobody in it
    // resolves to nobody, and a health read of nobody is zero — which is exactly "missing".
    private static ICombatantTargetSelector Bearer(string marker) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(marker)));

    // …and the body that fell there, which is a different address. A fallen Bearer still wears the mark that
    // says which position it was, and Replace the Fallen is the one rule that has to reach it — the engine
    // seam this elite bought.
    private static ICombatantTargetSelector FallenBearer(string marker) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(marker), includeFallen: true));

    private static ICombatExpression<TContext, bool> Standing<TContext>(string marker)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(Bearer(marker)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static IEnumerable<StatusData> QueenStatuses() =>
    [
        TheProperLine(),
        ReconstructionCharge(),
        Marker(PermittedExceptionId, "Permitted Exception",
            "The Queen's own licence was spent on this Bearer, so up to 6 of its Block was struck off with "
            + "it."),
        Marker(LinePendingId, "Out of Order",
            "The Bearer whose turn it was not. It is only this while the violation is being filed."),
        Marker(LineBrokenThisTurnId, "Order Noted",
            "The Queen has already answered one broken line this turn."),
        .. Bearers.Select(b => Marker(b.Marker, b.Name,
            $"{b.Name} of the procession. The line is walked in order, and striking out of it is the "
            + "Queen's business, not the Bearer's.")),
    ];

    private static StatusData ReconstructionCharge() => new()
    {
        Id = ReconstructionChargeId,
        NameKey = "Reconstruction Charge",
        DescriptionKey =
            "How many Bearers the Queen may still call back. Two a combat, and each one costs a Claim as "
            + "well.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── Local Law — Do Not Break the Line ─────────────────────────────────────────────────────────────────
    private static StatusData TheProperLine()
    {
        // In a card-played trigger the event target is the card's OWN target, which is exactly the design's
        // "directly targets" — and an untargeted card resolves to nobody, so an area effect asks nothing.
        var struck = CombatantTargetSelectors.EventTarget;

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> OutOfOrder(int index)
        {
            var isThisOne = new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                    struck, new StatusDefinitionId(Bearers[index].Marker)),
                ComparisonOperator.Greater,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(0));

            var somebodyAhead = Enumerable.Range(0, index)
                .Select(i => Standing<CardPlayedTriggeredEffectContext>(Bearers[i].Marker))
                .Aggregate((a, b) => new OrExpression<CardPlayedTriggeredEffectContext>(a, b));

            return new AndExpression<CardPlayedTriggeredEffectContext>(isThisOne, somebodyAhead);
        }

        var brokeTheLine = new OrExpression<CardPlayedTriggeredEffectContext>(OutOfOrder(1), OutOfOrder(2));

        // The Bearer that was struck is marked while the filing runs, because a refusal is announced without
        // the card that provoked it — and the Permitted Exception belongs to whoever was aimed at.
        var law = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                brokeTheLine,
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        struck, new StatusDefinitionId(LinePendingId),
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                    Violate<CardPlayedTriggeredEffectContext>(Queen, ProperLineLaw, LineBrokenThisTurnId),
                    .. Bearers.Select(b => (IEffectNode<CardPlayedTriggeredEffectContext>)
                        new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                            Bearer(b.Marker), new StatusDefinitionId(LinePendingId))),
                ])));

        // "If Safe-Conduct prevents this out-of-order Trespass, the targeted Bearer gains Permitted
        // Exception." ADAPTATION: "the first direct card hit after prevention ignores up to 6 Block" is
        // built as up to 6 Block struck off there and then — the Bearer's Block is only ever gained on the
        // Queen's own turn, so the next hit meets exactly the board the design describes.
        var exception = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantCounterExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, LawBeingFiledCounter),
                        ComparisonOperator.Equal,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(ProperLineLaw))),
                new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                    [.. Bearers.Select(b => PermitTheException(b.Marker))])));

        // The latch, the spent exceptions, and Closed Formation. The design puts the guarding at the end of
        // the enemy turn; it is written at the start of the PLAYER's, which is the same board seen from the
        // side that has to get through it — and, unlike a turn ending, that happens exactly once a round
        // however many bodies the procession is fielding.
        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Queen, new StatusDefinitionId(LineBrokenThisTurnId)),
                    .. Bearers.Select(b => (IEffectNode<TurnStartedTriggeredEffectContext>)
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            Bearer(b.Marker), new StatusDefinitionId(PermittedExceptionId))),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                Queen, new StatusDefinitionId(ClaimId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                            [.. Bearers.Select(b => (IEffectNode<TurnStartedTriggeredEffectContext>)
                                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                                    Standing<TurnStartedTriggeredEffectContext>(b.Marker),
                                    new GainBlockNode<TurnStartedTriggeredEffectContext>(
                                        Bearer(b.Marker),
                                        new ConstantExpression<TurnStartedTriggeredEffectContext>(4))))])),
                ])));

        // "Queen death: all surviving Bearers collapse and combat ends." The procession is the Queen's; it
        // has nothing to carry once she is gone.
        var collapse = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [.. Bearers.Select(b => (IEffectNode<CombatantDownedTriggeredEffectContext>)
                    new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                        Standing<CombatantDownedTriggeredEffectContext>(b.Marker),
                        new SetHealthNode<CombatantDownedTriggeredEffectContext>(
                            Bearer(b.Marker), new ConstantExpression<CombatantDownedTriggeredEffectContext>(0))))]));

        return Rule(AntQueenId, "Do Not Break the Line",
            "Aim a card at a Line-Bearer while one ahead of it is still standing and you owe the QUEEN 1 "
            + "Trespass — once a turn, and only where a card names its target. Spend a licence on it and "
            + "that Bearer loses up to 6 Block. Her standing tightens the line: at 1 Claim the acting "
            + "Bearer hits harder, at 2 every Bearer guards, at 3 she may enforce the order herself.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    law, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    exception, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("Downed", JsonSerializer.SerializeToElement(
                    collapse, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>())),
            ]);
    }

    private static IEffectNode<StatusApplicationBlockedTriggeredEffectContext> PermitTheException(string marker) =>
        new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
            new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    Bearer(marker), new StatusDefinitionId(LinePendingId)),
                ComparisonOperator.Greater,
                new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
            [
                new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Bearer(marker), new StatusDefinitionId(PermittedExceptionId),
                    new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1)),
                new ModifyDefensivePoolNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Bearer(marker), StandardCombatIds.BlockDefensivePool,
                    new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        -PermittedExceptionBlock)),
            ]));

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? QueenIntent(string enemyId, string intentId)
    {
        if (enemyId == AntQueenEnemyId)
            return TheQueenActs(intentId);

        var index = Array.FindIndex(Bearers, b => b.EnemyId == enemyId);
        return index < 0 ? null : ABearerActs(index, intentId);
    }

    // "Each enemy turn only the frontmost living Line-Bearer acts." The others are positional bodies, so
    // their whole program is a question about who is ahead of them.
    private static EffectProgram<EnemyActionContext>? ABearerActs(int index, string intentId)
    {
        var self = CombatantTargetSelectors.Source;

        // At 1 Claim the acting Bearer's direct attack is worth three more.
        IEffectNode<EnemyActionContext> CarryForward() =>
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        Queen, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                Blow(12), Blow(9));

        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            "carry_forward" => CarryForward(),
            "hold_the_line" => new GainBlockNode<EnemyActionContext>(
                Queen, new ConstantExpression<EnemyActionContext>(7)),
            // "If the previous position is dead, 13 damage; otherwise Carry Forward." A Bearer with nobody
            // in front of it has no gap to bite into.
            "bite_out_of_order" => index == 0
                ? CarryForward()
                : new ConditionalEffectNode<EnemyActionContext>(
                    Standing<EnemyActionContext>(Bearers[index - 1].Marker),
                    CarryForward(), Blow(13)),
            _ => null,
        };

        if (act is null)
            return null;

        if (index == 0)
            return new EffectProgram<EnemyActionContext>(act);

        var somebodyAhead = Enumerable.Range(0, index)
            .Select(i => Standing<EnemyActionContext>(Bearers[i].Marker))
            .Aggregate((a, b) => new OrExpression<EnemyActionContext>(a, b));

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                somebodyAhead, new NoOpEffectNode<EnemyActionContext>(), act));
    }

    private static EffectProgram<EnemyActionContext>? TheQueenActs(string intentId)
    {
        var self = CombatantTargetSelectors.Source;

        var livingBearers = Bearers
            .Select(b => LivingBearer<EnemyActionContext>(b.Marker))
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        IEffectNode<EnemyActionContext>? act = intentId switch
        {
            // "12 +4 per living Bearer, max 24."
            "count_the_proper_order" => new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(12),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(4), livingBearers))),
            "collect_the_queens_claim" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ConsumeClaim<EnemyActionContext>(self),
                    DemandWergild<EnemyActionContext>(self, 2),
                ])),
            // "Gain 14 Block; close one formation gap if possible." ADAPTATION: with three fixed positions
            // there is nowhere to march to, so the line closing up is the survivors tightening — 3 Block
            // each, and only while there is actually a gap to close.
            "royal_survey_of_the_line" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(14)),
                new ConditionalEffectNode<EnemyActionContext>(
                    AGapInTheLine<EnemyActionContext>(),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                        [.. Bearers.Select(b => (IEffectNode<EnemyActionContext>)
                            new ConditionalEffectNode<EnemyActionContext>(
                                Standing<EnemyActionContext>(b.Marker),
                                new GainBlockNode<EnemyActionContext>(
                                    Bearer(b.Marker), new ConstantExpression<EnemyActionContext>(3))))])),
            ]),
            "replace_the_fallen" => new ConditionalEffectNode<EnemyActionContext>(
                CanReconstruct<EnemyActionContext>(),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    ConsumeClaim<EnemyActionContext>(self),
                    ReplaceTheLowestFallen<EnemyActionContext>(),
                ]),
                // Nothing to rebuild, or nothing to rebuild it with: the Queen counts the line instead.
                Blow(12)),
            "royal_enforcement" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<EnemyActionContext>(ClaimCeiling)),
                RoyalEnforcement(),
                Blow(12)),
            _ => null,
        };

        return act is null ? null : new EffectProgram<EnemyActionContext>(act);
    }

    // A living Bearer counts one — a min against 1, so that a Bearer's health never doubles as a headcount.
    // (Written out of the vocabulary the document is serialized from: every expression that ships has to be
    // one the writer knows a kind for, so a hand-rolled one would not survive the export.)
    private static ICombatExpression<TContext, int> LivingBearer<TContext>(string marker)
        where TContext : class =>
        new MinExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new CombatantCurrentHealthExpression<TContext>(Bearer(marker)));

    private static ICombatExpression<TContext, bool> AGapInTheLine<TContext>()
        where TContext : class =>
        Bearers.Select(b => (ICombatExpression<TContext, bool>)
                new NotExpression<TContext>(Standing<TContext>(b.Marker)))
            .Aggregate((a, b) => new OrExpression<TContext>(a, b));

    private static ICombatExpression<TContext, bool> CanReconstruct<TContext>()
        where TContext : class =>
        new AndExpression<TContext>(
            AGapInTheLine<TContext>(),
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(Queen, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(1)),
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        Queen, new StatusDefinitionId(ReconstructionChargeId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(1))));

    // The lowest-numbered missing Bearer, and only it. The latch is what makes "one" mean one across three
    // conditionals in a row.
    private static IEffectNode<TContext> ReplaceTheLowestFallen<TContext>()
        where TContext : class
    {
        var steps = new List<IEffectNode<TContext>>
        {
            new RemoveStatusNode<TContext>(Queen, new StatusDefinitionId(LineBrokenThisTurnId)),
        };

        foreach (var bearer in Bearers)
        {
            steps.Add(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new NotExpression<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(
                                Queen, new StatusDefinitionId(LineBrokenThisTurnId)),
                            ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
                    new NotExpression<TContext>(Standing<TContext>(bearer.Marker))),
                new CausalSequenceEffectNode<TContext>(
                [
                    // Standing up first, and only then given something to stand on: setting health is a
                    // living-only operation, and until the lifecycle turns there is nobody there to heal.
                    new SetCombatantLifecycleStateNode<TContext>(
                        FallenBearer(bearer.Marker), CombatantLifecycleState.Alive),
                    new SetHealthNode<TContext>(
                        Bearer(bearer.Marker), new ConstantExpression<TContext>(BearerRespawnHealth)),
                    new ApplyStatusNode<TContext>(
                        Queen, new StatusDefinitionId(LineBrokenThisTurnId),
                        new ConstantExpression<TContext>(1)),
                ])));
        }

        steps.Add(new ModifyStatusStacksNode<TContext>(
            Queen, new StatusDefinitionId(ReconstructionChargeId), new ConstantExpression<TContext>(-1)));
        // The latch is the Queen's "one breach a turn" marker, borrowed for the length of this program: it
        // is put back exactly as it was found, because a reconstruction is not a violation.
        steps.Add(new RemoveStatusNode<TContext>(Queen, new StatusDefinitionId(LineBrokenThisTurnId)));
        return new CausalSequenceEffectNode<TContext>(steps);
    }

    // "Deal 27; consume all 3 Claims; create Wergild 2. If a charge remains and a Bearer is missing,
    // respawn only the lowest-numbered one." No free reconstruction — the charge is still spent.
    private static IEffectNode<EnemyActionContext> RoyalEnforcement()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Blow(27),
            new RepeatEffectNode<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(ClaimCeiling),
                ConsumeClaim<EnemyActionContext>(self)),
            DemandWergild<EnemyActionContext>(self, 2),
            new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    AGapInTheLine<EnemyActionContext>(),
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            Queen, new StatusDefinitionId(ReconstructionChargeId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1))),
                ReplaceTheLowestFallen<EnemyActionContext>()),
        ]);
    }
}
