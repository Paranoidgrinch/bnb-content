using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 1 — **The Stag of Pre-Approved Violence** (138 HP).
//
// > Protection from lawful violence is useful, but every use authorizes a stronger response.
//
// The Stag hands out the very licence its law makes you need, and counts every one you spend. Three spent
// licences are a Sanction apiece, and three Sanctions turn its next action into the Charge — which is paid
// for out of the standing you let it accumulate. Everything the player can do about it is Act III's own
// vocabulary read one notch differently:
//
//   take the Trespass instead of refusing it — the Sanction only counts REFUSALS;
//   settle its Wergild in full — a clean fight bleeds a Sanction back off and costs the Stag 7 HP;
//   keep its Claims low — the Charge is 22 flat without them, and 30 with two.
//
// The one thing the Stag never does is punish you for defending yourself. It authorizes it, and then bills
// you for the authorization.
public static partial class ActThree
{
    public const string StagEnemyId = "stag_of_pre_approved_violence";
    public const string StagLawId = "pre_approved_violence";
    public const string StagSanctionId = "stag_sanction";
    public const string StagVergeMarkedId = "stag_verge_marked";
    public const string StagViolenceNotedId = "stag_violence_noted";

    public const int StagSanctionsForCharge = 3;
    private const int StagChargeBase = 22;
    private const int StagChargePerClaim = 4;
    private const int StagChargeClaimCeiling = 2;
    private const int StagCleanFightHealth = 7;

    private static ICombatantTargetSelector Stag { get; } = Elite(StagLawId);

    private static IEnumerable<StatusData> StagStatuses() =>
    [
        ViolenceRequiresLeave(),
        StagSanction(),
        Marker(StagVergeMarkedId, "Verge Marked",
            "The Stag has staked the edge of the road. Its next Trespass is 2 rather than 1 — and one "
            + "Safe-Conduct still refuses the whole of it."),
        Marker(StagViolenceNotedId, "Leave Already Taken",
            "The Stag has already answered one act of violence this turn."),
    ];

    // Sanction is the Stag's own resource and exists only inside its encounter: not a damage bonus, not
    // standing, just a count of how many times you have used the leave it gave you.
    private static StatusData StagSanction() => new()
    {
        Id = StagSanctionId,
        NameKey = "Sanction",
        DescriptionKey =
            "How many of the Stag's own licences you have spent refusing it. At 3 its next action is the "
            + "Authorized Charge.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── Local Law — Violence Requires Leave ───────────────────────────────────────────────────────────────
    //
    // Three rules on one status, because they are one system: the law that makes you need a licence, the
    // grant that hands you one, and the count of the ones you spend.
    private static StatusData ViolenceRequiresLeave()
    {
        // The first Deed of the player's turn is the violence the road cares about. (BnB has no Attack type;
        // Deed is its one-shot offensive card, and every rule in the game that reads "Attack" reads `deed`.)
        var breach = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                    new TagId(Cards.CardAuthoring.DeedTag)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    Violate<CardPlayedTriggeredEffectContext>(
                        Stag, PreApprovedViolenceLaw, StagViolenceNotedId,
                        stacks: VergedTrespass<CardPlayedTriggeredEffectContext>()),
                    // A stake marks ONE crossing, whether the crossing was refused or not.
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        Stag, new StatusDefinitionId(StagVergeMarkedId)),
                ])));

        // "At the beginning of each player turn, if below the normal Safe-Conduct cap, gain 1 Safe-Conduct
        // from the Stag." Granted in the Stag's own name, so its Sanction can tell its licences from the
        // ones the fight opened you with.
        var grant = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Stag, new StatusDefinitionId(StagViolenceNotedId)),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(SafeConductId)),
                            ComparisonOperator.Less,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(SafeConductCeiling)),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(SafeConductId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                            sourceSelector: Stag)),
                ])));

        // "Whenever Safe-Conduct prevents Stag Trespass, the Stag gains 1 Sanction." A refusal is an event
        // the engine announces: on it, the SOURCE is whoever refused (the player, wearing the licence) and
        // the EVENT TARGET is whoever was refused — so "was it MY Trespass" is a question about the target.
        var sanction = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget,
                                new StatusDefinitionId(StagLawId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0)),
                        new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                Stag, new StatusDefinitionId(StagSanctionId)),
                            ComparisonOperator.Less,
                            new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                StagSanctionsForCharge)))),
                new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Stag, new StatusDefinitionId(StagSanctionId),
                    new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1))));

        return Rule(StagLawId, "Violence Requires Leave",
            "The first Deed you play each turn is 1 Trespass owed to the Stag — and each of your turns opens "
            + "with a Safe-Conduct from the Stag itself, up to three. Every licence you spend refusing it is "
            + "a Sanction, and at 3 Sanctions its next action is the Authorized Charge.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    breach, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    grant, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    sanction, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // One Trespass, or two off a marked verge. Written as a single application of two rather than two
    // applications of one, because §5.2 is explicit that one Safe-Conduct refuses the whole of a doubled
    // attempt — which is what a licence that pays for two incoming stacks at a time does.
    private static ICombatExpression<TContext, int> VergedTrespass<TContext>()
        where TContext : class =>
        new AddExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new MinExpression<TContext>(
                new ConstantExpression<TContext>(1),
                new CombatantStatusStacksExpression<TContext>(
                    Stag, new StatusDefinitionId(StagVergeMarkedId))));

    // ── A Clean Fight ─────────────────────────────────────────────────────────────────────────────────────
    //
    // "Whenever Stag Wergild is fully paid: normal Safe-Conduct; the Stag loses 7 HP; remove 1 Sanction."
    // The HP loss is direct, so it is a health SET that no Block and no damage reaction can see.
    private static IEffectNode<TurnEndedTriggeredEffectContext> ACleanFight()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(StagLawId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new SetHealthNode<TurnEndedTriggeredEffectContext>(
                    creditor,
                    new SubtractExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(creditor),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(StagCleanFightHealth))),
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(StagSanctionId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
            ]));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: "queue Authorized Charge as the Stag's next visible action" is written into EVERY slot
    // rather than into one of them, which is what "the next action" means — whichever move was coming up is
    // replaced by the Charge. The listed action stays the telegraph, as everywhere else in the port.
    //
    // The cooldowns fall out of the five-slot cycle: any one slot comes round again only every fifth action,
    // and the Charge cannot re-queue until three more licences have been spent.
    private static EffectProgram<EnemyActionContext>? StagIntent(string enemyId, string intentId)
    {
        if (enemyId != StagEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext>? ordinary = intentId switch
        {
            "stamp_the_hoof" => Blow(15),
            // The Stag grants leave in its own name, and braces behind the grant.
            "grant_leave_to_resist" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
            ]),
            // 18, and a bill on top of it once the Stag has standing to send one.
            "antlered_enforcement" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Blow(18),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(1)),
                    DemandWergild<EnemyActionContext>(self, 1)),
            ]),
            "mark_the_verge" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(16)),
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(StagVergeMarkedId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ]),
            "trample_the_boundary" => Blow(16),
            _ => null,
        };

        return ordinary is null
            ? null
            : new EffectProgram<EnemyActionContext>(
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(StagSanctionId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(StagSanctionsForCharge)),
                    AuthorizedCharge(),
                    ordinary));
    }

    private static IEffectNode<EnemyActionContext> Blow(int damage) =>
        new DealDamageNode<EnemyActionContext>(
            Applicant, new ConstantExpression<EnemyActionContext>(damage));

    // ── Authorized Charge ─────────────────────────────────────────────────────────────────────────────────
    //
    // "22 damage +4 per Claim consumed, up to 2 Claims. Maximum 30. Then Sanctions → 0."
    //
    // The blow is struck before the standing is spent, so the two readings of "per Claim consumed" agree:
    // what the Charge is worth is decided by what it is about to cash in.
    private static IEffectNode<EnemyActionContext> AuthorizedCharge()
    {
        var self = CombatantTargetSelectors.Source;

        var cashable = new MinExpression<EnemyActionContext>(
            new ConstantExpression<EnemyActionContext>(StagChargeClaimCeiling),
            new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)));

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(StagChargeBase),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(StagChargePerClaim), cashable))),
            new RepeatEffectNode<EnemyActionContext>(cashable, ConsumeClaim<EnemyActionContext>(self)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(StagSanctionId)),
        ]);
    }
}
