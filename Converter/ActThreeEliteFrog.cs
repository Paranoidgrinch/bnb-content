using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 4 — **Great Toll Frog** (176 HP).
//
// > Restitution is necessary, but every payment becomes ammunition unless the player deliberately leaves
// > change behind.
//
// A colossal frog sits in a ford with coins, feathers, teeth and old Offerings lodged in its throat. It is
// the act's restitution system turned against itself: Wergild is the one pressure Act III lets you answer
// outright, and the Frog swallows every point you pay. Each swallowed point is a **Toll**, and the Toll is
// what its throat eventually throws back at you.
//
// The way out is the one thing nobody does by reflex — paying MORE than you owe. LEAVE THE CHANGE costs an
// extra card and takes a Toll back out of the Frog, which is the only route that settles a demand without
// arming the ford.
public static partial class ActThree
{
    public const string GreatTollFrogEnemyId = "great_toll_frog";
    public const string GreatTollFrogId = "nothing_crosses_for_free";
    public const string TollId = "frog_toll";
    public const string FrogCrossingNotedId = "frog_crossing_noted";

    public const int NothingCrossesForFreeLaw = 17;
    public const int TollCeiling = 5;

    // What the player owed the Frog when the current payment began, so that "a point ACTUALLY PAID to the
    // Frog" can be told from a point paid to somebody else — Make Amends settles the oldest demand first,
    // and never asks which creditor that was.
    public static CounterId FrogOwedBeforeCounter => new("frog_owed_before");

    private static ICombatantTargetSelector Frog { get; } = Elite(GreatTollFrogId);

    private static IEnumerable<StatusData> FrogStatuses() =>
    [
        NothingCrossesForFree(),
        Toll(),
        Marker(FrogCrossingNotedId, "Crossing Noted",
            "The Frog has already charged you for one crossing this turn."),
    ];

    private static StatusData Toll() => new()
    {
        Id = TollId,
        NameKey = "Toll",
        DescriptionKey =
            "What the Frog has swallowed: one for every point of its Wergild you have paid, up to 5. It is "
            + "not a debt — it is ammunition, and the ford gives it back all at once.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, int> OwedToTheFrog<TContext>()
        where TContext : class =>
        new CombatantStatusStacksFromSourceExpression<TContext>(
            Applicant, new StatusDefinitionId(WergildId), Frog);

    private static ICombatExpression<TContext, bool> FrogIsHere<TContext>()
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(Frog),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static StatusData NothingCrossesForFree()
    {
        var player = CombatantTargetSelectors.Source;

        // "The first time each player turn the player spends their final current Energy: 1 Trespass." The
        // card has to have COST something — a free card played on an empty purse is not a crossing paid for.
        var spent = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<CardPlayedTriggeredEffectContext>(
                            player, StandardCombatIds.EnergyResource),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                            StandardCombatIds.EnergyResource),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0))),
                Violate<CardPlayedTriggeredEffectContext>(
                    Frog, NothingCrossesForFreeLaw, FrogCrossingNotedId)));

        var reset = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Frog, new StatusDefinitionId(FrogCrossingNotedId))));

        // "The Toll Is Never Gone — whenever the Frog gains a newly created Claim, create Wergild 2." One
        // demand per grant, and grants stop at the Claim ceiling, so the ford cannot bill you forever.
        EffectProgram<TContext> billed<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                DemandWergild<TContext>(CombatantTargetSelectors.EventTarget, 2)));

        return Rule(GreatTollFrogId, "Nothing Crosses for Free",
            "The first time each turn you spend your last Energy, you owe the Frog 1 Trespass — and every "
            + "Claim it is granted becomes a demand for 2. Each point of that demand you pay is swallowed as "
            + "a Toll, up to 5, and the ford eventually gives all of it back at once. Pay in full and you "
            + "may LEAVE THE CHANGE: one more card discarded takes a Toll back out of its throat.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    spent, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    reset, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    billed<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    billed<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // ── Swallowed Payment, and the change you may leave ───────────────────────────────────────────────────
    //
    // Spliced into the act's one payment, on both sides of it: what the player owed the Frog is written down
    // first, and afterwards the difference says whether this point was the ford's.

    public static IEffectNode<TContext> RememberWhatTheFrogIsOwed<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            FrogIsHere<TContext>(),
            new SetCombatantCounterNode<TContext>(
                Applicant, FrogOwedBeforeCounter, OwedToTheFrog<TContext>(), relative: false));

    public static IEffectNode<TContext> SwallowThePayment<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                FrogIsHere<TContext>(),
                new ComparisonExpression<TContext>(
                    OwedToTheFrog<TContext>(), ComparisonOperator.Less,
                    new CombatantCounterExpression<TContext>(Applicant, FrogOwedBeforeCounter))),
            new CausalSequenceEffectNode<TContext>(
            [
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(Frog, new StatusDefinitionId(TollId)),
                        ComparisonOperator.Less, new ConstantExpression<TContext>(TollCeiling)),
                    new ApplyStatusNode<TContext>(
                        Frog, new StatusDefinitionId(TollId), new ConstantExpression<TContext>(1))),
                // The demand is settled in full, which is the one moment the ford will take change back.
                new ConditionalEffectNode<TContext>(
                    new AndExpression<TContext>(
                        new ComparisonExpression<TContext>(
                            OwedToTheFrog<TContext>(), ComparisonOperator.Equal,
                            new ConstantExpression<TContext>(0)),
                        new ComparisonExpression<TContext>(
                            new CombatantStatusStacksExpression<TContext>(Frog, new StatusDefinitionId(TollId)),
                            ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
                    LeaveTheChange<TContext>()),
            ]));

    // TAKE THE RECEIPT does nothing at all. LEAVE THE CHANGE costs one more card out of hand and pulls a
    // Toll back out of the Frog's throat — the only settlement in the fight that does not arm the ford.
    private static IEffectNode<TContext> LeaveTheChange<TContext>()
        where TContext : class
    {
        var player = CombatantTargetSelectors.Source;
        var change = new IteratedCardExpression<TContext>();

        return new ChooseOptionsNode<TContext>(
        [
            new NoOpEffectNode<TContext>(),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new SubtractExpression<TContext>(
                        new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand),
                        new CombatantZoneCardCountExpression<TContext>(
                            Applicant, CardZone.Hand, MakeAmendsTag)),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new MarkCardInstanceNode<TContext>(
                        player,
                        new ChosenCardInZoneExpression<TContext>(
                            CardZone.Hand, "leave the change", excludeTag: MakeAmendsTag),
                        OfferingMark),
                    new ForEachCardInZoneNode<TContext>(
                        player, CardZone.Hand,
                        new ConditionalEffectNode<TContext>(
                            new CardInstanceHasMarkExpression<TContext>(change, OfferingMark),
                            new CausalSequenceEffectNode<TContext>(
                            [
                                new MarkCardInstanceNode<TContext>(player, change, OfferingMark, remove: true),
                                new MoveCardToZoneNode<TContext>(player, change, CardZone.DiscardPile),
                                new ModifyStatusStacksNode<TContext>(
                                    Frog, new StatusDefinitionId(TollId),
                                    new ConstantExpression<TContext>(-1)),
                            ]))),
                ])),
        ], ["take the receipt", "leave the change"], count: 1, purpose: "the ford's change");
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // "At Toll 5 Regurgitate the Toll becomes the next eligible OFFENSIVE intent" — so the ford's two other
    // attacks read the throat before they open it, and its Block and its billing do not.
    private static EffectProgram<EnemyActionContext>? FrogIntent(string enemyId, string intentId)
    {
        if (enemyId != GreatTollFrogEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext>? offensive = intentId switch
        {
            // 15 +2 per open point, capped at +6.
            "tongue_of_collection" => new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(15),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(2),
                        new MinExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3),
                            OwedToTheFrog<EnemyActionContext>())))),
            "mud_bank_impact" => Blow(19),
            "regurgitate_the_toll" => RegurgitateTheToll(),
            _ => null,
        };

        if (offensive is { } attack)
            return new EffectProgram<EnemyActionContext>(
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(TollId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(TollCeiling)),
                    RegurgitateTheToll(), attack));

        IEffectNode<EnemyActionContext>? other = intentId switch
        {
            // "Consume 1 Claim; gain 10 +3 per Toll Block, max 25."
            "swallow_the_offering" => new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(1)),
                    ConsumeClaim<EnemyActionContext>(self)),
                new GainBlockNode<EnemyActionContext>(
                    self,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(10),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(3),
                            new MinExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(TollCeiling),
                                new CombatantStatusStacksExpression<EnemyActionContext>(
                                    self, new StatusDefinitionId(TollId)))))),
            ]),
            // "Wergild 2; at 2+ Claims Wergild 3 instead."
            "croak_the_amount_due" => new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(2)),
                DemandWergild<EnemyActionContext>(self, 3),
                DemandWergild<EnemyActionContext>(self, 2)),
            _ => null,
        };

        return other is null ? null : new EffectProgram<EnemyActionContext>(other);
    }

    // "12 +4 per Toll, maximum 32. Then Toll → 0 and the player gains 1 Safe-Conduct." The licence is the
    // ford's apology, and it is the only thing in the fight that hands one over for free.
    private static IEffectNode<EnemyActionContext> RegurgitateTheToll()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(12),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(4),
                        new MinExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(TollCeiling),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                self, new StatusDefinitionId(TollId)))))),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(TollId)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
        ]);
    }
}
