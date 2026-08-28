using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 9 — The Moonlit Jurisdictions. Everyone agrees the inscription is law. The dispute is about
// what it says — and about which court has standing to say so. Two new bodies, and two old ones that come
// back changed: the Permit Hare from the very first room, now claimed by two legal systems, and the Errant
// Boundary Stone, which has acquired precedence over lesser border disputes.
public static partial class ActThree
{
    public const string ThreeReadingsId = "three_readings";
    public const string ReadingObservedId = "reading_observed";
    public const string DestinationRuleId = "destination";
    public const string DestinationId = "the_destination";
    public const string AttendedId = "attended";
    public const string AimedLastId = "aimed_last";
    public const string SuperiorJurisdictionId = "superior_jurisdiction";

    // Which of the Trail Marker's three interpretations is currently in force.
    public static CounterId ReadingCounter => new("current_reading");

    // How many times the player's attention has wandered to a new party this turn.
    public static CounterId WanderingCounter => new("wandering_attention");

    private const int Readings = 3;

    // ── the mark the fight leaves on whoever you looked at ────────────────────────────────────────────────

    // Two identities in this stage ask the same question — where did your attention actually go? — and
    // neither can be answered by a counter, because a counter holds a number and this is about WHO. So the
    // fight marks whoever a card was aimed at, and clears the marks when the turn starts again.
    public static StatusData Attended() =>
        Marker(AttendedId, "Attended To",
            "You have aimed something at this party this turn.");

    // The other half of the same question, and a different question: not "did you go here at all this turn"
    // but "is this where you were looking a moment ago". One mark accumulates and one moves, because the
    // Path asks whether the destination was ever reached and the Marker asks how often you changed your mind.
    //
    // Each identity writes ITS OWN mark, and neither reads the other's. Sharing one would race: the Marker
    // has to read where the eye was BEFORE the eye moves, and the order two CardPlayed rules fire in is not
    // decided.
    public static StatusData AimedLast() =>
        Marker(AimedLastId, "Last Aimed At",
            "The last thing you aimed at.");

    // ── The Untranslated Trail Marker — Three Readings ────────────────────────────────────────────────────

    // Reading I: two consecutive cards of the same Base Cost.
    // Reading II: the second time your attention wanders to a new party in one turn.
    // Reading III: ending your turn with no real card left in hand.
    //
    // The Marker advances to the next reading whenever it is granted a Claim, or whenever a Safe-Conduct is
    // spent against ITS Trespass specifically — arguing with the inscription is what changes what it says.
    public static StatusData ThreeReadings()
    {
        var player = CombatantTargetSelectors.Source;
        var marker = Lawgiver(ThreeReadingsId);
        var memory = CostMemory("inscription");

        ICombatExpression<CardPlayedTriggeredEffectContext, int> ThisCost() =>
            new AddExpression<CardPlayedTriggeredEffectContext>(
                new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    StandardCombatIds.EnergyResource),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1));

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Reading(int n) =>
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, ReadingCounter),
                ComparisonOperator.Equal,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(n));

        // READING I — the repeated measure.
        var repeatedMeasure = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Reading(1),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, memory),
                    ComparisonOperator.Equal, ThisCost())),
            Violate<CardPlayedTriggeredEffectContext>(marker, InscriptionLaw, ReadingObservedId));

        // READING II — the wandering attention. Where the attention went is not a number, so the fight marks
        // where it was and counts how often it moved somewhere else.
        var wanderingAttention = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId(AimedLastId)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                // …and you were looking somewhere a moment ago.
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                        Lawgiver(AimedLastId), new StatusDefinitionId(AimedLastId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, WanderingCounter,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        Reading(2),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                player, WanderingCounter),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(2))),
                    Violate<CardPlayedTriggeredEffectContext>(marker, InscriptionLaw, ReadingObservedId)),
            ]));

        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                repeatedMeasure,
                wanderingAttention,
                // The eye moves: only one party is where you are looking.
                new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(
                    Parties,
                    new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(AimedLastId))),
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget, new StatusDefinitionId(AimedLastId),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, memory, ThisCost(), relative: false),
            ]));

        // READING III — empty hands are unwitnessed.
        var emptyHands = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, ReadingCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(3)),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            RealCardsLeftInHand<TurnEndedTriggeredEffectContext>(),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))),
                Violate<TurnEndedTriggeredEffectContext>(marker, InscriptionLaw, ReadingObservedId)));

        // A new turn: the inscription still says whatever it says, but nothing has been attended to yet.
        var clear = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, memory,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, WanderingCounter,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Lawgiver(ThreeReadingsId), new StatusDefinitionId(ReadingObservedId)),
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                        Parties,
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(AimedLastId))),
                    // The reading starts at one; a fight that has not argued yet reads the plainest sense.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, ReadingCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            Applicant, ReadingCounter,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false)),
                ])));

        // Arguing with the inscription is what changes what it says: a Claim granted to it, or a licence
        // spent against its own Trespass.
        var advanceOnClaim = ArgueWithTheInscription<StatusAppliedTriggeredEffectContext>(
            new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(
                new StatusDefinitionId(ClaimId)),
            CombatantTargetSelectors.EventTarget);

        var advanceOnRefusal = ArgueWithTheInscription<StatusApplicationBlockedTriggeredEffectContext>(
            new TriggerEventStatusIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                new StatusDefinitionId(TrespassId)),
            CombatantTargetSelectors.EventTarget);

        return Rule(ThreeReadingsId, "Three Readings",
            "The inscription is authoritative and untranslated. Reading I: two cards in a row of the same "
            + "Base Cost. Reading II: the second time your attention moves to a new party in a turn. "
            + "Reading III: ending a turn with no real card in hand. Argue with it — grant it a Claim, or "
            + "spend a licence against it — and it advances to the next reading.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    onPlay, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    emptyHands, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    clear, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    advanceOnClaim, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplicationPrevented", JsonSerializer.SerializeToElement(
                    advanceOnRefusal,
                    CombatJson.CreateOptions<StatusApplicationBlockedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // The reading turns over: 1 → 2 → 3 → 1. `about` is whoever the event concerns, and it has to be the
    // Marker — a Claim granted to somebody else, or a licence spent against somebody else's Trespass, is not
    // an argument about this inscription.
    private static EffectProgram<TContext> ArgueWithTheInscription<TContext>(
        ICombatExpression<TContext, bool> theRightEvent, ICombatantTargetSelector about)
        where TContext : class =>
        new(new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                theRightEvent,
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        about, new StatusDefinitionId(ThreeReadingsId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TContext>(0))),
            new SetCombatantCounterNode<TContext>(
                Applicant, ReadingCounter,
                new AddExpression<TContext>(
                    new RemainderExpression<TContext>(
                        new CombatantCounterExpression<TContext>(Applicant, ReadingCounter),
                        new ConstantExpression<TContext>(Readings)),
                    new ConstantExpression<TContext>(1)),
                relative: false)));

    public static StatusData ReadingObserved() =>
        Marker(ReadingObservedId, "Read Once",
            "The inscription has already been read against you this turn.");

    // ── Elsewhere Path — Destination ──────────────────────────────────────────────────────────────────────

    // "At the start of each player turn, mark another living party as the Destination. End the turn without
    // ever aiming at it and you owe the Path 1 Trespass. If the Destination dies before you have, the Path is
    // granted a Claim."
    //
    // The law is not about what you did to the Destination. It is about whether you went where the path said
    // you were going.
    public static StatusData Destination()
    {
        var path = Lawgiver(DestinationRuleId);
        var destination = Lawgiver(DestinationId);
        var candidate = CombatantTargetSelectors.HighestStatusStacks(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(DestinationRuleId)),
            new StatusDefinitionId(ClaimId));

        // The Path's own bookkeeping: whatever a card is aimed at has been visited this turn.
        var visit = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(AttendedId),
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1)));

        var choose = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                        Parties,
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget,
                                new StatusDefinitionId(DestinationId)),
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(AttendedId)),
                        ])),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(candidate),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            candidate, new StatusDefinitionId(DestinationId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                ])));

        var reckon = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        // A destination was named and is still standing …
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(destination),
                            ComparisonOperator.Greater,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                        // …and you never went there.
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                destination, new StatusDefinitionId(AttendedId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))),
                Violate<TurnEndedTriggeredEffectContext>(path, DestinationLaw)));

        // The destination died before you ever aimed at it: the road was never travelled, and the Path has a
        // grievance of its own about it.
        var unreached = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new AndExpression<CombatantDownedTriggeredEffectContext>(
                    new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CombatantDownedTriggeredEffectContext>(
                            CombatantTargetSelectors.SourceIncludingDowned,
                            new StatusDefinitionId(DestinationId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(0)),
                    new ComparisonExpression<CombatantDownedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CombatantDownedTriggeredEffectContext>(
                            CombatantTargetSelectors.SourceIncludingDowned,
                            new StatusDefinitionId(AttendedId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(0))),
                CreateClaim<CombatantDownedTriggeredEffectContext>(path)));

        return Rule(DestinationRuleId, "Destination",
            "Each of your turns the Elsewhere Path names a party as the Destination. End the turn without "
            + "aiming anything at it and you owe the Path 1 Trespass; let it die unvisited and the Path is "
            + "granted a Claim.",
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
                new StatusTriggerData("Downed", JsonSerializer.SerializeToElement(
                    unreached, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData TheDestination() =>
        Marker(DestinationId, "Destination",
            "The path says you are going here. Aim something at it before the turn ends.");

    // ── Errant Boundary Stone — Superior Jurisdiction ─────────────────────────────────────────────────────

    // "Foreign Claim-transfer effects may not move a Claim from a party with more Claims to one with fewer.
    // The Stone itself ignores this when moving its own title."
    //
    // A prohibition on what other rules may do is a mark, as Prior Possession is: `ActThree.TransferClaim`
    // asks for it, and the Stone's own Wandering Title is the one transfer that does not.
    public static StatusData SuperiorJurisdiction() =>
        Marker(SuperiorJurisdictionId, "Superior Jurisdiction",
            "While this party stands, no foreign rule may move a Claim downhill — from a party holding more "
            + "to one holding fewer.");
}
