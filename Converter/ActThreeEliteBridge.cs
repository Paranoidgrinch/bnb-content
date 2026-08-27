using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 3 — **The Wrong Bridge in Person** (96 HP + 104 HP).
//
// > A debt changes meaning once the crossing is complete; the same payment that helps on one bank creates
// > standing on the other.
//
// A bridge built nowhere near a river has grown four stone legs and come to collect travellers itself. It
// is one fight with two banks, and the whole of it is a single joke told twice: **settling in full is good
// for you on This Bank and bad for you on the Other.**
//
//   This Bank   — an open demand makes hurting the Bridge a Trespass; every Claim it is granted is another
//                 demand; and paying in full strikes a Claim off and costs it 5 HP.
//   Other Bank  — refusing it earns you Return Standing, which its stonework later cashes; and paying in
//                 full now GRANTS it standing and spends the Standing you had built.
//
// ADAPTATION: the design spawns a second 104-HP body at Phase-I lethal, transferring the surviving Claims
// and preserving the open Wergild. It is built as ONE 200-HP body that turns around at 104 — the Act-II
// phase idiom — because that makes the two things the transition rule is FOR (Claims survive the crossing;
// the demand is not cancelled by it) true by construction rather than by a transfer that would have to be
// written, tested and kept from announcing itself as a grant.
public static partial class ActThree
{
    public const string WrongBridgeEnemyId = "the_wrong_bridge_in_person";
    public const string WrongBridgeId = "debt_before_passage";
    public const string TheOtherBankId = "the_other_bank";
    public const string ReturnStandingId = "return_standing";
    public const string BridgePassageNotedId = "bridge_passage_noted";
    public const string BridgeStandingNotedId = "bridge_standing_noted";

    public const int DebtBeforePassageLaw = 16;

    // Where This Bank ends: 96 of the Bridge's 200 spent, and the far side is 104.
    public const int OtherBankHealth = 104;
    private const int MaxReturnStanding = 2;
    private const int ThisBankSettlementHealth = 5;

    private static ICombatantTargetSelector Bridge { get; } = Elite(WrongBridgeId);

    private static IEnumerable<StatusData> BridgeStatuses() =>
    [
        DebtBeforePassage(),
        Marker(TheOtherBankId, "The Other Bank",
            "The crossing is complete. From here restitution is not something you buy your way out of — it "
            + "is something that gives the Bridge standing."),
        ReturnStanding(),
        Marker(BridgePassageNotedId, "Passage Noted",
            "The Bridge has already answered one blow this turn."),
        Marker(BridgeStandingNotedId, "Return Noted",
            "The Bridge has already recorded one refusal this turn."),
    ];

    private static StatusData ReturnStanding() => new()
    {
        Id = ReturnStandingId,
        NameKey = "Return Standing",
        DescriptionKey =
            "What refusing the Bridge on the far bank is worth to you — and what its stonework remembers. "
            + "At most 2.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, bool> OnTheOtherBank<TContext>()
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(Bridge, new StatusDefinitionId(TheOtherBankId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static StatusData DebtBeforePassage()
    {
        var bridge = CombatantTargetSelectors.EventTarget;   // in a damage trigger: whoever was struck
        var attacker = CombatantTargetSelectors.Source;

        // "While the Bridge has open Wergild, the first player-caused HP damage dealt to it each player turn
        // attempts 1 Trespass." The damage still resolves; the Trespass is what the debt adds to it. And the
        // same blow is what carries the Bridge over to the far bank.
        var struck = new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                                attacker, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        new AndExpression<DamageReceivedTriggeredEffectContext>(
                            // HP damage, not a blow the tollgate soaked.
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                                ComparisonOperator.Greater,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantStatusStacksFromSourceExpression<DamageReceivedTriggeredEffectContext>(
                                    Applicant, new StatusDefinitionId(WergildId), bridge),
                                ComparisonOperator.Greater,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)))),
                    Violate<DamageReceivedTriggeredEffectContext>(
                        bridge, DebtBeforePassageLaw, BridgePassageNotedId)),
                CrossOver<DamageReceivedTriggeredEffectContext>(),
            ]));

        // The latches, and the crossing again — a demand settled in full takes 5 HP off the Bridge without
        // being damage at all, and the far bank has to be reachable that way too.
        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            Bridge, new StatusDefinitionId(BridgePassageNotedId)),
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            Bridge, new StatusDefinitionId(BridgeStandingNotedId)),
                    ])),
                CrossOver<TurnStartedTriggeredEffectContext>(),
            ]));

        // "Toll Before Passage — whenever the Bridge gains a newly created Claim, create Wergild 2. The
        // Claim remains." This Bank only: on the far side a settled demand is what MAKES standing, and a
        // Claim that made another demand that made another Claim is the loop the act exists to refuse.
        EffectProgram<TContext> toll<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                    new NotExpression<TContext>(OnTheOtherBank<TContext>())),
                DemandWergild<TContext>(CombatantTargetSelectors.EventTarget, 2)));

        // "Return Passage Has Standing — the first time each player turn Safe-Conduct prevents Bridge
        // Trespass, +1 Return Standing, max 2." The far bank only.
        var standing = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(WrongBridgeId)),
                            ComparisonOperator.Greater,
                            new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0)),
                        new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            OnTheOtherBank<StatusApplicationBlockedTriggeredEffectContext>(),
                            new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                new NotExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                        new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                            Bridge, new StatusDefinitionId(BridgeStandingNotedId)),
                                        ComparisonOperator.Greater,
                                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0))),
                                new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                    new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                        Applicant, new StatusDefinitionId(ReturnStandingId)),
                                    ComparisonOperator.Less,
                                    new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(
                                        MaxReturnStanding)))))),
                new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(ReturnStandingId),
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1)),
                    new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                        Bridge, new StatusDefinitionId(BridgeStandingNotedId),
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1)),
                ])));

        return Rule(WrongBridgeId, "Debt Before Passage",
            "While you owe the Bridge anything, the first blow you land on it each turn is 1 Trespass — and "
            + "every Claim it is granted on this bank is another demand for 2. Settle in full here and a "
            + "Claim is struck off and the Bridge loses 5 HP. Once it turns around, the same settlement "
            + "GRANTS it standing instead, and refusing it earns you Return Standing its stonework "
            + "remembers.",
            [
                new StatusTriggerData("DamageTaken", JsonSerializer.SerializeToElement(
                    struck, CombatJson.CreateOptions<DamageReceivedTriggeredEffectContext>())),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    toll<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    toll<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    standing, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // The crossing. Nothing moves and nothing is transferred: the Bridge simply turns around, which is why
    // the Claims it holds and the demand it has raised are still exactly where they were.
    private static IEffectNode<TContext> CrossOver<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(OnTheOtherBank<TContext>()),
                new ComparisonExpression<TContext>(
                    new CombatantCurrentHealthExpression<TContext>(Bridge),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<TContext>(OtherBankHealth))),
            new ApplyStatusNode<TContext>(
                Bridge, new StatusDefinitionId(TheOtherBankId), new ConstantExpression<TContext>(1)));

    // ── settlement, read from whichever bank you are standing on ──────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> TheTollForCrossing()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        // This Bank: a Claim struck off, and 5 HP that no Block and no reaction sees.
        var thisBank = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
                creditor,
                new StatusSelectionSpec(StatusPolarityFilter.Any) { Definition = new StatusDefinitionId(ClaimId) },
                new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
            new SetHealthNode<TurnEndedTriggeredEffectContext>(
                creditor,
                new SubtractExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(creditor),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(ThisBankSettlementHealth))),
        ]);

        // The Other Bank: the same payment, and it makes the Bridge somebody instead of unmaking it.
        var otherBank = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            CreateClaim<TurnEndedTriggeredEffectContext>(creditor),
            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(ReturnStandingId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
        ]);

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(WrongBridgeId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                OnTheOtherBank<TurnEndedTriggeredEffectContext>(), otherBank, thisBank));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // Five slots, each reading as its This-Bank move or its Other-Bank one. The design's cooldowns of 2 and
    // 3 are the cycle itself: a slot comes round only every fifth action.
    private static EffectProgram<EnemyActionContext>? BridgeIntent(string enemyId, string intentId)
    {
        if (enemyId != WrongBridgeEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        return intentId switch
        {
            // Approaching Abutment → The Far Bank Rises.
            "approaching_abutment" => Banks(
                Blow(15),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(17),
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(12)),
                ])),
            // Raise the Tollgate → Move the Gap: it takes the licence if you have one, and files if you do not.
            "raise_the_tollgate" => Banks(
                new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ConditionalEffectNode<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                Applicant, new StatusDefinitionId(SafeConductId)),
                            ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
                        new ModifySelectedStatusStacksNode<EnemyActionContext>(
                            Applicant,
                            new StatusSelectionSpec(StatusPolarityFilter.Any)
                            {
                                Definition = new StatusDefinitionId(SafeConductId),
                            },
                            new ConstantExpression<EnemyActionContext>(-1)),
                        FileTrespass<EnemyActionContext>(self)),
                    Blow(10),
                ])),
            // Future Toll → Charge the Return Toll.
            "future_toll" => Banks(
                DemandWergild<EnemyActionContext>(self, 2),
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        ConsumeClaim<EnemyActionContext>(self),
                        DemandWergild<EnemyActionContext>(self, 2),
                    ]))),
            // Charge for the Crossing → Stonework Remembers the Crossing.
            "charge_for_the_crossing" => Banks(
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(17),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(3),
                                new CombatantStatusStacksExpression<EnemyActionContext>(
                                    self, new StatusDefinitionId(ClaimId)))))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new DealDamageNode<EnemyActionContext>(
                        Applicant,
                        new AddExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(14),
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(4),
                                new MinExpression<EnemyActionContext>(
                                    new ConstantExpression<EnemyActionContext>(MaxReturnStanding),
                                    new CombatantStatusStacksExpression<EnemyActionContext>(
                                        Applicant, new StatusDefinitionId(ReturnStandingId)))))),
                    new RemoveStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(ReturnStandingId)),
                ])),
            // The far bank's signature. On this bank the slot is an ordinary approach.
            "collapse_before_completion" => Banks(
                Blow(15),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(28),
                    new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    new RemoveStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(ReturnStandingId)),
                ])),
            _ => null,
        };
    }

    private static EffectProgram<EnemyActionContext> Banks(
        IEffectNode<EnemyActionContext> thisBank, IEffectNode<EnemyActionContext> otherBank) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            OnTheOtherBank<EnemyActionContext>(), otherBank, thisBank));
}
