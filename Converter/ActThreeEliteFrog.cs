using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III elite 4 — **Great Toll Frog** (176 HP).
//
// Nothing crosses for free. Once your hand is dealt the Frog eyes the dearest card in it (the mark is on the card)
// and puts its fare in your hand: pay 10 Gold and it lets the card be. Refuse — end the turn without paying — and it
// swallows that card for the fight and its law is broken. Deal it 25 in one turn and it spits every swallowed card
// back into your hand. Whatever it still holds when the fight ends is gone for that fight only; your deck keeps it.
//
// Gold is the run's, so the fight only keeps the count (a counter on the player, `frog_fare`) and a run program
// charges it once the fight is over (FrogSpoilsProgram) — the Municipal Dragon's idiom. The fight cannot read the
// purse, so a fare can be promised with an empty one; the charge never goes below nothing.
//
// REWORKED 2026-11-08 (content review E14): it swallowed 5 Gold a turn on its own, with no decision in it, and the
// Dragon already presses on Gold. Before that (2026-11-03) the Wergild it swallowed as Toll went.
public static partial class ActThree
{
    public const string GreatTollFrogEnemyId = "great_toll_frog";
    public const string GreatTollFrogId = "nothing_crosses_for_free";
    public const string SwallowedCardsId = "swallowed_cards";
    public const string EyedByTheFrogMark = "eyed_by_the_frog";
    public const string InTheFrogsBellyMark = "in_the_frogs_belly";
    public const string FrogsFareCardId = "the_frogs_fare";
    public const string FrogFareTag = "frog_fare";

    public const int FrogFare = 10;
    public const int FrogSpitDamage = 25;
    private const int FrogDearestLooked = 5;
    public const string FrogSpoilsProgramId = "ford_frog_spoils";
    public static CounterId FrogFareCounter => new("frog_fare");
    public static CounterId FrogBruiseCounter => new("frog_bruise");

    private static ICombatantTargetSelector Frog { get; } = Elite(GreatTollFrogId);

    // After the fight: every fare promised is paid.
    public static ITriggeredRunEffectDefinition FrogSpoilsProgram() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold,
                RunExpr.Multiply(RunEventValues.CombatCounter(FrogFareCounter.ToString()), RunExpr.Const(-FrogFare))),
        ]);

    private static IEnumerable<StatusData> FrogStatuses() =>
    [
        NothingCrossesForFree(),
        new StatusData
        {
            Id = SwallowedCardsId,
            NameKey = "In the Belly",
            DescriptionKey = "Cards of yours it has swallowed. Deal it 25 in one turn and it spits them all back into your hand.",
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers = [],
        },
    ];

    public static IEnumerable<CardData> FrogCards() =>
    [
        Gift(FrogsFareCardId, "The Frog's Fare",
            $"Pay {FrogFare} Gold after the fight. The Frog lets the card it eyed be this turn.", FrogFareTag,
            Seq(
                new SetCombatantCounterNode<CardPlayContext>(CombatantTargetSelectors.Source, FrogFareCounter, N(1), relative: true),
                Unmark<CardPlayContext>(EyedByTheFrogMark, CardZone.Hand, CardZone.DiscardPile, CardZone.DrawPile))),
    ];

    private static StatusData NothingCrossesForFree()
    {
        // The dearest card in hand, Junk aside: the first card found at 5+ Energy, else at 4, … down to 0.
        var looked = new IteratedCardExpression<CardsDrawnTriggeredEffectContext>();
        IEffectNode<CardsDrawnTriggeredEffectContext> EyeAt(int cost) =>
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand,
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        Is(MarkedInZone<CardsDrawnTriggeredEffectContext>(CardZone.Hand, EyedByTheFrogMark), ComparisonOperator.Equal, 0),
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            Is(CardCost(looked), ComparisonOperator.GreaterOrEqual, cost),
                            new NotExpression<CardsDrawnTriggeredEffectContext>(
                                new CardInstanceHasTagExpression<CardsDrawnTriggeredEffectContext>(looked, new TagId(Cards.CardAuthoring.JunkTag))))),
                    MarkCard(looked, EyedByTheFrogMark)),
                maxIterations: ZoneWalkLimit);
        var eye = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Unmark<CardsDrawnTriggeredEffectContext>(EyedByTheFrogMark, CardZone.Hand, CardZone.DiscardPile, CardZone.DrawPile),
            .. Enumerable.Range(0, FrogDearestLooked + 1).Reverse().Select(EyeAt),
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Is(MarkedInZone<CardsDrawnTriggeredEffectContext>(CardZone.Hand, EyedByTheFrogMark), ComparisonOperator.Greater, 0),
                new CreateCardInstanceNode<CardsDrawnTriggeredEffectContext>(Applicant, new CardDefinitionId(FrogsFareCardId), CardZone.Hand,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
        ]);

        // The turn ends unpaid: the eyed card is swallowed wherever it lies (played or not), and the law is broken.
        // It is walked for in the hand and then the discard pile, by its marks, because the hand may be discarded
        // while this runs.
        IEffectNode<TurnEndedTriggeredEffectContext> Walk(CardZone zone, string mark, IEffectNode<TurnEndedTriggeredEffectContext> body) =>
            new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(Applicant, zone, body,
                maxIterations: ZoneWalkLimit, markFilter: new TagId(mark));
        var it = new IteratedCardExpression<TurnEndedTriggeredEffectContext>();
        var intoTheBelly = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            MarkCard(it, InTheFrogsBellyMark),
            MarkCard(it, EyedByTheFrogMark, remove: true),
        ]);
        var gulp = new MoveCardToZoneNode<TurnEndedTriggeredEffectContext>(Applicant, it, CardZone.BanishedPile);
        var swallow = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Is(new AddExpression<TurnEndedTriggeredEffectContext>(
                    MarkedInZone<TurnEndedTriggeredEffectContext>(CardZone.Hand, EyedByTheFrogMark),
                    MarkedInZone<TurnEndedTriggeredEffectContext>(CardZone.DiscardPile, EyedByTheFrogMark)),
                ComparisonOperator.Greater, 0),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                Walk(CardZone.Hand, EyedByTheFrogMark, intoTheBelly),
                Walk(CardZone.DiscardPile, EyedByTheFrogMark, intoTheBelly),
                Walk(CardZone.Hand, InTheFrogsBellyMark, gulp),
                Walk(CardZone.DiscardPile, InTheFrogsBellyMark, gulp),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Frog, new StatusDefinitionId(SwallowedCardsId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                Breach<TurnEndedTriggeredEffectContext>(Frog),
            ]));

        // Its own turn: the bruise count starts over.
        var self = CombatantTargetSelectors.Source;
        var fresh = SetCounter<TurnStartedTriggeredEffectContext>(self, FrogBruiseCounter, 0);

        // A beating: 25 health from the player in one turn, and it spits every card it holds back into your hand.
        var frog = CombatantTargetSelectors.EventTarget;
        var bruise = CounterOn<DamageReceivedTriggeredEffectContext>(frog, FrogBruiseCounter);
        var beaten = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            new TargetHasStatusExpression<DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    frog, FrogBruiseCounter, new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        Is(bruise, ComparisonOperator.GreaterOrEqual, FrogSpitDamage),
                        Wearing<DamageReceivedTriggeredEffectContext>(frog, SwallowedCardsId)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new ForEachCardInZoneNode<DamageReceivedTriggeredEffectContext>(Applicant, CardZone.BanishedPile,
                            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                            [
                                MarkCard(new IteratedCardExpression<DamageReceivedTriggeredEffectContext>(), InTheFrogsBellyMark, remove: true),
                                new MoveCardToZoneNode<DamageReceivedTriggeredEffectContext>(Applicant,
                                    new IteratedCardExpression<DamageReceivedTriggeredEffectContext>(), CardZone.Hand),
                            ]),
                            maxIterations: ZoneWalkLimit, markFilter: new TagId(InTheFrogsBellyMark)),
                        new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(frog, new StatusDefinitionId(SwallowedCardsId)),
                    ])),
            ]));

        return Rule(GreatTollFrogId, "Nothing Crosses for Free",
            $"Eyes your dearest card each turn: pay {FrogFare} Gold or it swallows it and you break its law. {FrogSpitDamage} damage in a turn returns them.",
            [
                AtTheOpeningHand(Frog, eye),
                AtYourTurnsEnd(Frog, swallow),
                On("TurnStarted", fresh),
                On("DamageTaken", beaten),
            ]);
    }

    private static EffectProgram<EnemyActionContext>? FrogIntent(string enemyId, string intentId) => null;
}
