using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 5 — The Wayside Covenants. The stage where the act starts being NICE to you, and where the
// player learns that hospitality is a debt engine. All three parties hand out Safe-Conduct, and all three
// want something back for it: the Witchling wants the gift used, the Bride wants the relationship to
// progress, and the Cup wants its generosity to create obligation somewhere in the social network.
public static partial class ActThree
{
    public const string CourtesySafeConductId = "courtesy_safe_conduct";
    public const string PromiseMustBePairedId = "a_promise_must_be_paired";
    public const string BetrothalClaimId = "betrothal_claim";
    public const string BetrothalGiftGivenId = "betrothal_gift_given";
    public const string DrinkBeforeChoosingId = "drink_before_choosing";
    public const string CupPouredThisTurnId = "cup_poured_this_turn";

    // How many of the Witchling's own stacks the player was carrying when this turn began. The rule is about
    // whether any of HERS was spent, and the difference between two readings answers that exactly — which is
    // why it needs no way to ask whose stack an interceptor happened to spend.
    public static CounterId CourtesyCarriedCounter => new("courtesy_carried");

    // Turns since the Cup last poured.
    public static CounterId CupPatienceCounter => new("cup_patience");

    private const int WitchlingHeal = 6;
    private const int CupPatience = 2;

    // ── Roadside Witchling — Courtesy Safe-Conduct ────────────────────────────────────────────────────────

    // "If the player carries Witchling-granted Safe-Conduct through a later full player turn without spending
    // any of it: 1 Trespass from the Witchling. If the player spends it: the Witchling heals. Only her own
    // granted stacks count."
    //
    // The gift is real, and ignoring it is rude. Whose stack the engine's interceptor happened to spend is
    // not a question anything can ask — so the rule does not ask it. It counts hers at the start of the turn
    // and counts them again at the end, and the difference is the whole answer.
    public static StatusData CourtesySafeConduct()
    {
        var witchling = Lawgiver(CourtesySafeConductId);

        ICombatExpression<TContext, int> Hers<TContext>() where TContext : class =>
            new CombatantStatusStacksFromSourceExpression<TContext>(
                Applicant, new StatusDefinitionId(SafeConductId), witchling);

        var remember = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Applicant, CourtesyCarriedCounter,
                    Hers<TurnStartedTriggeredEffectContext>(), relative: false)));

        var carried = new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
            Applicant, CourtesyCarriedCounter);

        var reckon = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    // Fewer of hers than the turn began with: the gift was used, and the bond is confirmed.
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        Hers<TurnEndedTriggeredEffectContext>(), ComparisonOperator.Less, carried),
                    new HealNode<TurnEndedTriggeredEffectContext>(
                        witchling, new ConstantExpression<TurnEndedTriggeredEffectContext>(WitchlingHeal)),
                    // Carried a whole turn and never used: rude.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                carried, ComparisonOperator.Greater,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                Hers<TurnEndedTriggeredEffectContext>(),
                                ComparisonOperator.GreaterOrEqual, carried)),
                        FileTrespass<TurnEndedTriggeredEffectContext>(witchling)))));

        return Rule(CourtesySafeConductId, "Courtesy Safe-Conduct",
            "The Witchling's own Safe-Conduct is a gift with strings. Spend it and she is pleased — she "
            + "recovers 6 HP. Carry it through a whole turn unspent and you owe her 1 Trespass.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    remember, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    reckon, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // ── The Blackthorn Bride ──────────────────────────────────────────────────────────────────────────────

    // "After a card of Base Cost 2 or more, the next card should cost 0 or 1. If it also costs 2 or more:
    // 1 Trespass." A promise must be paired.
    public static StatusData APromiseMustBePaired()
    {
        var player = CombatantTargetSelectors.Source;
        var bride = Lawgiver(PromiseMustBePairedId);
        var memory = CostMemory("betrothal");

        // The Base Cost of the card just played, plus one, so that "nothing yet" can be zero.
        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        var bothHeavy = new AndExpression<CardPlayedTriggeredEffectContext>(
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                ThisCost(), ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(3)));

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    bothHeavy,
                    Violate<CardPlayedTriggeredEffectContext>(bride, PairedPromiseLaw)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Applicant, memory,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)));

        return Rule(PromiseMustBePairedId, "A Promise Must Be Paired",
            "Follow a card of Base Cost 2 or more with another and you owe the Blackthorn Bride 1 Trespass. "
            + "Something cheap has to come between.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // "At 1 Claim the player gains 1 Safe-Conduct. At 2 Claims, create Wergild 2 from the Bride." Welcome,
    // then commitment, then obligation — the relationship progresses whether the player wants it to or not.
    public static StatusData BetrothalClaim()
    {
        var bride = CombatantTargetSelectors.EventTarget;

        EffectProgram<TContext> Program<TContext>() where TContext : class
        {
            var claims = new CombatantStatusStacksExpression<TContext>(
                bride, new StatusDefinitionId(ClaimId));

            return new EffectProgram<TContext>(
                new ConditionalEffectNode<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimId)),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        // The welcome, once.
                        new ConditionalEffectNode<TContext>(
                            new AndExpression<TContext>(
                                new ComparisonExpression<TContext>(
                                    claims, ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<TContext>(1)),
                                new ComparisonExpression<TContext>(
                                    new CombatantStatusStacksExpression<TContext>(
                                        bride, new StatusDefinitionId(BetrothalGiftGivenId)),
                                    ComparisonOperator.Equal,
                                    new ConstantExpression<TContext>(0))),
                            new CausalSequenceEffectNode<TContext>(
                            [
                                new ApplyStatusNode<TContext>(
                                    Applicant, new StatusDefinitionId(SafeConductId),
                                    new ConstantExpression<TContext>(1), sourceSelector: bride),
                                new ApplyStatusNode<TContext>(
                                    bride, new StatusDefinitionId(BetrothalGiftGivenId),
                                    new ConstantExpression<TContext>(1)),
                            ])),
                        // …and then the bill.
                        new ConditionalEffectNode<TContext>(
                            new ComparisonExpression<TContext>(
                                claims, ComparisonOperator.Equal, new ConstantExpression<TContext>(2)),
                            DemandWergild<TContext>(bride, 2)),
                    ])));
        }

        return Rule(BetrothalClaimId, "Betrothal Claim",
            "The Bride's first Claim is welcomed with 1 Safe-Conduct for you. Her second is a demand for "
            + "2 Wergild.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    public static StatusData BetrothalGiftGiven() =>
        Marker(BetrothalGiftGivenId, "Welcomed",
            "The Bride has already welcomed you once.");

    // ── Crossroads Cup — Drink Before Choosing ────────────────────────────────────────────────────────────

    // "Every two player turns the player gains 1 Safe-Conduct. The first time each player turn the player
    // spends Safe-Conduct, the living enemy with the fewest Claims gains 1 newly created Claim."
    //
    // The Cup helps. Its help also creates obligation somewhere in the social network, and that is the whole
    // identity: an unattended ceremonial cup at a crossroads is a polite debt engine.
    public static StatusData DrinkBeforeChoosing()
    {
        var cup = Lawgiver(DrinkBeforeChoosingId);

        var pour = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        cup, new StatusDefinitionId(CupPouredThisTurnId)),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, CupPatienceCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, CupPatienceCounter),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(CupPatience)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                Applicant, CupPatienceCounter,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(SafeConductId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                sourceSelector: cup),
                        ])),
                ])));

        // A licence being spent is a refusal happening: the engine says so when a Trespass is turned away.
        var obligation = new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new AndExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new ComparisonExpression<StatusApplicationBlockedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<StatusApplicationBlockedTriggeredEffectContext>(
                            cup, new StatusDefinitionId(CupPouredThisTurnId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                        cup, new StatusDefinitionId(CupPouredThisTurnId),
                        new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1)),
                    CreateClaim<StatusApplicationBlockedTriggeredEffectContext>(
                        CombatantTargetSelectors.LowestStatusStacks(Parties, new StatusDefinitionId(ClaimId))),
                ])));

        return Rule(DrinkBeforeChoosingId, "Drink Before Choosing",
            "Every second turn the Cup pours you 1 Safe-Conduct. The first licence you spend each turn is "
            + "recognised somewhere else: whichever party holds the fewest Claims gains one.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    pour, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    obligation, CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData CupPouredThisTurn() =>
        Marker(CupPouredThisTurnId, "Toast Made",
            "The Cup has already turned one of your licences into somebody's standing this turn.");
}
