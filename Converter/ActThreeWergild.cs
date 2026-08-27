using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III's fourth universal mechanic: **Wergild**, the demand. Trespass accumulates into somebody's
// standing; Wergild is standing already exercised — a party naming a price and giving you until the end of
// your next turn to pay it.
//
// Three things had to be decided, and all three are the same decision seen from different sides: a demand
// belongs to ONE creditor.
//
//   The clock is the creditor's. A demand raised is not yet due; it matures at the player's next turn start
//   and falls due at that turn's end. Two markers on the creditor say which it is, so a demand raised during
//   the player's own turn does not fall due before the player has had a turn to answer it.
//   The settlement is the creditor's. Each one reads what IT is owed and clears its OWN Wergild — which is
//   what the engine seam for naming whose instances a rule means was bought for.
//   The reward is the creditor's. Paid in full, most parties grant one Safe-Conduct; the Streamside
//   Oath-Fish grants two, and says so by wearing a marker the settlement reads.
//
// Payment is the card **Make Amends**, because a combat here has no free actions — only cards. The fight
// puts one in the player's hand when a demand is raised, it survives the turn boundary, and it returns to
// hand after each use for as long as anything is still owed.
public static partial class ActThree
{
    public const string WergildId = "wergild";
    public const string WergildDemandedId = "wergild_demanded";
    public const string WergildDueId = "wergild_due";
    public const string WergildFallsDueId = "wergild_falls_due";
    public const string OathAcceptedId = "oath_accepted";
    public const string MakeAmendsCardId = "make_amends";

    public static readonly TagId MakeAmendsTag = new("make_amends");

    // The mark that says which card in hand is being offered — the payment reads the card twice (what it
    // costs, and then where it goes) and a CHOSEN card cannot be read twice: each reading asks again.
    public static readonly TagId OfferingMark = new("offering");

    // What an unpaid point of Wergild costs, and what a demand settled in full grants.
    private const int UnpaidDamagePerPoint = 2;

    // A demand, one instance per creditor, carrying who it is owed to. Merging would lose exactly the thing
    // the whole mechanic is about.
    public static StatusData Wergild() => new()
    {
        Id = WergildId,
        NameKey = "Wergild",
        DescriptionKey =
            "A demand owed to whoever raised it, due by the end of your next turn. Make Amends pays a point; "
            + "each point still owed when it falls due costs you 2 HP and becomes that party's Claim.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.CreateSeparateInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData WergildDemanded() =>
        Marker(WergildDemandedId, "Demand Raised",
            "This party has named a price. It comes due at the end of your next turn.");

    public static StatusData WergildDue() =>
        Marker(WergildDueId, "Demand Due",
            "This party's demand is settled at the end of this turn, paid or not.");

    public static StatusData OathAccepted() =>
        Marker(OathAcceptedId, "Oath Accepted",
            "This party treats restitution as a sacred thing: settling with it in full grants 2 Safe-Conduct "
            + "instead of 1.");

    // ── raising a demand ──────────────────────────────────────────────────────────────────────────────────

    // Everything in the act that says "create Wergild N from X" goes through here: the demand itself, the
    // creditor's note that it has raised one, and the card the player answers it with.
    public static IEffectNode<TContext> DemandWergild<TContext>(ICombatantTargetSelector creditor, int points)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new ApplyStatusNode<TContext>(
                Applicant, new StatusDefinitionId(WergildId), new ConstantExpression<TContext>(points),
                sourceSelector: creditor),
            new ApplyStatusNode<TContext>(
                creditor, new StatusDefinitionId(WergildDemandedId), new ConstantExpression<TContext>(1)),
            OfferTheMeansToPay<TContext>(),
        ]);

    // One Make Amends at a time. It is not spent by being played — it comes back while anything is still
    // owed — so a second copy would only take up a hand slot.
    private static IEffectNode<TContext> OfferTheMeansToPay<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        Applicant, new StatusDefinitionId(WergildId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand, MakeAmendsTag),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0))),
            new CreateCardInstanceNode<TContext>(
                Applicant, new CardDefinitionId(MakeAmendsCardId), CardZone.Hand,
                new ConstantExpression<TContext>(1)));

    // The same offer, made from inside the card itself: the copy being played is still counted in hand.
    private static IEffectNode<CardPlayContext> AnotherMeansToPay() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        Applicant, new StatusDefinitionId(WergildId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0)),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantZoneCardCountExpression<CardPlayContext>(
                        Applicant, CardZone.Hand, MakeAmendsTag),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<CardPlayContext>(1))),
            new CreateCardInstanceNode<CardPlayContext>(
                Applicant, new CardDefinitionId(MakeAmendsCardId), CardZone.Hand,
                new ConstantExpression<CardPlayContext>(1)));

    // ── the demand falling due ────────────────────────────────────────────────────────────────────────────

    // The rule the player carries into every Green Docket fight beside the act's customs. It is bearer-scoped
    // on the player, so it fires on the PLAYER's turn boundaries and nobody else's — which is exactly the
    // clock the design gives a demand.
    public static StatusData WergildFallsDue()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        // At the player's turn start, every demand raised since the last one matures.
        var mature = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(WergildDemandedId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(WergildDueId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(WergildDemandedId)),
                    ]))));

        var settle = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource, Settlement()));

        return Rule(WergildFallsDueId, "Restitution",
            "A demand raised against you comes due at the end of your next turn. Settle it in full and you "
            + "are granted Safe-Conduct; leave any of it owing and it costs you 2 HP a point and becomes that "
            + "party's Claim.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    mature, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    settle, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ]);
    }

    // What one creditor's demand does when it falls due.
    private static IEffectNode<TurnEndedTriggeredEffectContext> Settlement()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        var owed = new CombatantStatusStacksFromSourceExpression<TurnEndedTriggeredEffectContext>(
            Applicant, new StatusDefinitionId(WergildId), creditor);

        // Paid in full. Most parties grant one Safe-Conduct; the Oath-Fish says otherwise by wearing a marker.
        //
        // And this is where the Sedge Bench's other half lives: an appeal does not erase ownership, it
        // suspends the Claim long enough for settlement to extinguish it. The Bench cannot do that itself —
        // only the moment a demand is settled knows it was settled — so the rule belongs here.
        var settled = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new AddExpression<TurnEndedTriggeredEffectContext>(
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1),
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        creditor, new StatusDefinitionId(OathAcceptedId))),
                sourceSelector: creditor),
            // A coin at the table is worn down by other people keeping their word.
            PaidInKind(),
            // …and the elites that wrote their own terms of settlement read them here.
            EliteSettlement(),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        creditor, new StatusDefinitionId(UnderReviewId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
                        creditor,
                        new StatusSelectionSpec(StatusPolarityFilter.Any)
                        {
                            Definition = new StatusDefinitionId(ClaimId),
                        },
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(-1)),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        creditor, new StatusDefinitionId(UnderReviewId)),
                ])),
        ]);

        // Left owing. Each creditor clears its OWN demand one point at a time and leaves every other
        // creditor's standing — which is what naming whose instances a rule means is for.
        var unpaid = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new DealDamageNode<TurnEndedTriggeredEffectContext>(
                Applicant,
                new MultiplyExpression<TurnEndedTriggeredEffectContext>(
                    owed, new ConstantExpression<TurnEndedTriggeredEffectContext>(UnpaidDamagePerPoint))),
            CreateClaim<TurnEndedTriggeredEffectContext>(creditor),
            new RepeatEffectNode<TurnEndedTriggeredEffectContext>(
                owed,
                new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    Applicant,
                    new StatusSelectionSpec(StatusPolarityFilter.Debuff)
                    {
                        Definition = new StatusDefinitionId(WergildId),
                        FromActingSource = true,
                    },
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1),
                    sourceSelector: creditor)),
        ]);

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(WergildDueId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        owed, ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    settled,
                    unpaid),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(WergildDueId)),
            ]));
    }

    // ── Make Amends ───────────────────────────────────────────────────────────────────────────────────────

    // "Pay one point of Wergild by either spending 1 Energy or discarding one eligible card." A combat here
    // has no free actions, only cards, so the free action is a card the fight puts in your hand. It costs
    // nothing, survives the turn boundary, and returns to hand after each use while anything is still owed.
    public static CardData MakeAmends() => new()
    {
        Id = MakeAmendsCardId,
        NameKey = "Make Amends",
        DescriptionKey =
            "Choose one: PAY IN COIN — spend 1 Energy. OFFER A CARD — discard a card from your hand. "
            + "Either settles 1 Wergild, oldest demand first.",
        Costs = [],
        Tags = [MakeAmendsTag, new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ChooseOptionsNode<CardPlayContext>(
                    [PayInCoin(), OfferACard()],
                    ["pay in coin", "offer a card"],
                    count: 1,
                    purpose: "make amends"),
                // The card exhausts when it is played, and comes back while anything is still owed — here
                // rather than inside the payment, because a payment that could not go through (the Juniper's
                // injunction, an empty purse) still has to leave the player a way to try again. The count is
                // ONE rather than none, because the copy being played is still in hand while its own program
                // runs and is about to leave.
                AnotherMeansToPay(),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    // The Juniper's injunction against coin closes this route while it stands. With the hedge on the field
    // its demand is the only one there is, so "the Juniper's Wergild cannot be paid with Energy" and "coin
    // pays nothing" are the same sentence — and the offering route is guaranteed open beside it.
    private static IEffectNode<CardPlayContext> PayInCoin() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new NotExpression<CardPlayContext>(
                    PaymentEnjoined<CardPlayContext>(InjunctionCoinId)),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantCurrentResourceExpression<CardPlayContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<CardPlayContext>(1))),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new LoseResourceNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                    new ConstantExpression<CardPlayContext>(1)),
                PayOneWergild<CardPlayContext>(),
            ]));

    // The offering is MARKED rather than read straight from the choice, because a chosen card cannot be
    // asked about twice — each reading asks the player again — and this needs to know both what the card
    // costs and where it is going.
    private static IEffectNode<CardPlayContext> OfferACard()
    {
        var player = CombatantTargetSelectors.Source;
        var offering = new IteratedCardExpression<CardPlayContext>();

        // "Cards with Base Cost 0 cannot be used as Offerings to pay Wergild owed to Charter-Shell Snail."
        // The charter is the Snail's, so it applies while the Snail is one of the parties owed anything.
        var acceptable = new AndExpression<CardPlayContext>(
            new NotExpression<CardPlayContext>(
                PaymentEnjoined<CardPlayContext>(InjunctionOfferingId)),
            new NotExpression<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksFromSourceExpression<CardPlayContext>(
                        player, new StatusDefinitionId(WergildId), Lawgiver(PaymentAccordingToCharterId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayContext>(0)),
                new ComparisonExpression<CardPlayContext>(
                    new CardInstanceBaseCostExpression<CardPlayContext>(
                        offering, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayContext>(0)))));

        return new CausalSequenceEffectNode<CardPlayContext>(
        [
            new MarkCardInstanceNode<CardPlayContext>(
                player,
                // Not itself: Make Amends is still in hand while its own program runs, and being a free card
                // it would be refused under the Snail's charter anyway.
                new ChosenCardInZoneExpression<CardPlayContext>(
                    CardZone.Hand, "offer a card", excludeTag: MakeAmendsTag),
                OfferingMark),
            new ForEachCardInZoneNode<CardPlayContext>(
                player, CardZone.Hand,
                new ConditionalEffectNode<CardPlayContext>(
                    new CardInstanceHasMarkExpression<CardPlayContext>(offering, OfferingMark),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new MarkCardInstanceNode<CardPlayContext>(player, offering, OfferingMark, remove: true),
                        new ConditionalEffectNode<CardPlayContext>(
                            acceptable,
                            new CausalSequenceEffectNode<CardPlayContext>(
                            [
                                PayOneWergild<CardPlayContext>(),
                                // A name already spoken is worth twice as much to the Keeper.
                                BuriedNamesAsPayment(offering),
                            ])),
                        new MoveCardToZoneNode<CardPlayContext>(player, offering, CardZone.DiscardPile),
                    ]))),
        ]);
    }

    // One point off the OLDEST demand — the design does not say which creditor a payment answers when
    // several are owed, and settling the oldest first is the reading a court would take. The card comes back
    // while anything is still owed.
    private static IEffectNode<TContext> PayOneWergild<TContext>()
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            // The Great Toll Frog swallows every point of ITS demand that is actually paid, so what it was
            // owed is written down before the payment and read again after: Make Amends settles the oldest
            // demand first and never says whose that was.
            RememberWhatTheFrogIsOwed<TContext>(),
            new ModifySelectedStatusStacksNode<TContext>(
                Applicant,
                new StatusSelectionSpec(StatusPolarityFilter.Debuff)
                {
                    Definition = new StatusDefinitionId(WergildId),
                },
                new ConstantExpression<TContext>(-1)),
            SwallowThePayment<TContext>(),
        ]);
}
