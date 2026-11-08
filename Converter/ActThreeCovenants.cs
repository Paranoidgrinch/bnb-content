using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 4 — The Covenants. Since 2026-11-08 (content review E11/E12): a witch who plays the shell game
// with your hand, a bride who asks for one line of the old rhyme each turn, and (since 2026-11-03) a cup whose every
// drink is a debt. The Witchling's gifts went because they were the Grandmother Clause's law a stage early; the
// Bride's one-card-per-cost because it was the Reckoning Hedge's.
public static partial class ActThree
{
    public const string DrinkBeforeChoosingId = "drink_before_choosing";
    public const string CrossroadsDrinkCardId = "drink_from_the_cup";

    public const int CupClaims = 2;

    // ── Roadside Witchling — the Three Cups ───────────────────────────────────────────────────────────────
    //
    // Once your hand is dealt she puts three of its cards under her cups, and a hex under one of them. Play the hexed
    // one and it bites: 6 damage, and now you know. Leave all three cups unturned this turn and you break her law.
    // The cups are a visible mark on the cards; the hex is a second mark that the screen does not show.
    public const string ThreeCupsId = "the_three_cups";
    public const string UnderACupMark = "under_a_cup";
    public const string HexMark = "witchling_hex";
    public const string CupLiftedId = "cup_lifted";
    public const int CupCount = 3;
    public const int HexDamage = 6;
    private const string CupPickMark = "cup_pick";

    public static StatusData ThreeCups()
    {
        var witch = Lawgiver(ThreeCupsId);
        var hand = new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand);
        var cups = MarkedInZone<CardsDrawnTriggeredEffectContext>(CardZone.Hand, UnderACupMark);
        var pick = FirstMarked<CardsDrawnTriggeredEffectContext>(CardZone.Hand, CupPickMark);
        var random = new RandomCardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand);
        var deal = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            Unmark<CardsDrawnTriggeredEffectContext>(UnderACupMark, CardZone.Hand, CardZone.DiscardPile, CardZone.DrawPile),
            Unmark<CardsDrawnTriggeredEffectContext>(HexMark, CardZone.Hand, CardZone.DiscardPile, CardZone.DrawPile),
            new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(witch, new StatusDefinitionId(CupLiftedId)),
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Is(hand, ComparisonOperator.Greater, 0),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // The hexed cup first, then the other two around it.
                    MarkCard(random, CupPickMark),
                    MarkCard(pick, UnderACupMark),
                    MarkCard(pick, HexMark),
                    MarkCard(pick, CupPickMark, remove: true),
                    new RepeatUntilEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(cups, ComparisonOperator.GreaterOrEqual,
                            new MinExpression<CardsDrawnTriggeredEffectContext>(hand, new ConstantExpression<CardsDrawnTriggeredEffectContext>(CupCount))),
                        MarkCard(random, UnderACupMark), maxIterations: 32),
                ])),
        ]);

        var played = ThePlayedCard;
        var lift = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            CardHasMark(played, UnderACupMark),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    CardHasMark(played, HexMark),
                    new DealDamageNode<CardPlayedTriggeredEffectContext>(
                        Applicant, new ConstantExpression<CardPlayedTriggeredEffectContext>(HexDamage), kind: DamageKind.Reflected)),
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(witch, new StatusDefinitionId(CupLiftedId),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                MarkCard(played, HexMark, remove: true),
                MarkCard(played, UnderACupMark, remove: true),
            ]));

        var reckon = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new NotExpression<TurnEndedTriggeredEffectContext>(Wearing<TurnEndedTriggeredEffectContext>(witch, CupLiftedId)),
                    Is(MarkedInZone<TurnEndedTriggeredEffectContext>(CardZone.Hand, UnderACupMark), ComparisonOperator.Greater, 0)),
                Breach<TurnEndedTriggeredEffectContext>(witch)),
            Unmark<TurnEndedTriggeredEffectContext>(UnderACupMark, CardZone.Hand),
            Unmark<TurnEndedTriggeredEffectContext>(HexMark, CardZone.Hand),
        ]);

        return Rule(ThreeCupsId, "The Three Cups",
            $"Each turn she puts {CupCount} cards in your hand under her cups; one hides a hex that deals {HexDamage} damage to you when played. Play none of them and you break her law.",
            [AtTheOpeningHand(witch, deal), WhenYouPlay(witch, lift), AtYourTurnsEnd(witch, reckon)]);
    }

    public static StatusData CupLifted() =>
        Marker(CupLiftedId, "Cup Lifted", "You played a card from under her cups this turn: her law is kept.");

    // The gift-giving that only the Grandmother Clause does now (the Witchling gave gifts until 2026-11-08).
    // The player's turn opens: the gift of this round, in turn.
    private static IEffectNode<TurnStartedTriggeredEffectContext> GiveAGift(ICombatantTargetSelector giver, IReadOnlyList<string> gifts)
    {
        var round = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            new RoundNumberExpression<TurnStartedTriggeredEffectContext>(),
            new ConstantExpression<TurnStartedTriggeredEffectContext>(gifts.Count));
        IEffectNode<TurnStartedTriggeredEffectContext> Create(string gift) =>
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new CardDefinitionId(gift), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1));
        IEffectNode<TurnStartedTriggeredEffectContext> Give(int index) =>
            index == gifts.Count - 1
                ? Create(gifts[index])
                : new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    Is(round, ComparisonOperator.Equal, (index + 1) % gifts.Count), Create(gifts[index]), @else: Give(index + 1));
        return new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(giver)),
            Give(0));
    }

    // The player's turn ends with a gift still in hand: the giver's law is broken.
    private static IEffectNode<TurnEndedTriggeredEffectContext> AGiftRefused(ICombatantTargetSelector giver, string tag) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(), Alive<TurnEndedTriggeredEffectContext>(giver)),
                Is(new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand, new TagId(tag)), ComparisonOperator.Greater, 0)),
            Breach<TurnEndedTriggeredEffectContext>(giver));

    // A gift: free, gone when played, gone at the end of the turn if not.
    public static CardData Gift(string id, string name, string text, string tag, IEffectNode<CardPlayContext> body, int cost = 0) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = text,
        Costs = cost > 0 ? [new ResourceCost(StandardCombatIds.EnergyResource, cost)] : [],
        Tags = [new TagId("form"), new TagId(tag)],
        Program = new EffectProgram<CardPlayContext>(body),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    private static IEffectNode<CardPlayContext> LoseHealth(int amount) =>
        new SetHealthNode<CardPlayContext>(CombatantTargetSelectors.Source,
            new SubtractExpression<CardPlayContext>(
                new CombatantCurrentHealthExpression<CardPlayContext>(CombatantTargetSelectors.Source),
                new ConstantExpression<CardPlayContext>(amount)));

    private static IEffectNode<CardPlayContext> DiscardChosen(int count) =>
        new RepeatEffectNode<CardPlayContext>(new ConstantExpression<CardPlayContext>(count),
            new MoveCardToZoneNode<CardPlayContext>(CombatantTargetSelectors.Source,
                new ChosenCardInZoneExpression<CardPlayContext>(CardZone.Hand, "discard a card"), CardZone.DiscardPile));

    private static IEffectNode<CardPlayContext> Seq(params IEffectNode<CardPlayContext>[] steps) =>
        new CausalSequenceEffectNode<CardPlayContext>(steps);

    private static ConstantExpression<CardPlayContext> N(int value) => new(value);

    // ── The Blackthorn Bride — the Bridal Rhyme ───────────────────────────────────────────────────────────
    //
    // "Something old, something new, something borrowed, something blue": each turn of yours she asks for one line,
    // in that order, and her chip says which. Old is a card you held over from last turn, new a card of your deck
    // drawn this turn, borrowed a card the fight made for you (on her borrowed turns she lends you one), blue a
    // Working. Give her that line and she lets her Block fall; end the turn without it and you break her law. With
    // nothing held over she asks for something new instead.
    //
    // What a card is gets written on the copies, because the engine keeps no history of a card: once the first
    // hand is dealt every card of your deck is marked as your own, and every card the fight creates is marked as made
    // in it (either mark alone would miss a card that a rule created in the same instant); and at each turn's end
    // the cards of your own still in hand are marked as held over, the mark wiped again from every card that was
    // discarded after all. (A turn-start reading of the hand was tried first: the draw can land before it.)
    public const string BridalRhymeId = "the_bridal_rhyme";
    public const string RhymeKeptId = "rhyme_kept";
    public const string OwnCardMark = "of_your_own_deck";
    public const string MadeInTheFightMark = "made_in_the_fight";
    public const string HeldOverMark = "held_over";
    public const string BorrowedLaceId = "borrowed_lace";
    public const string BridalLoanTag = "bridal_loan";
    public const int BorrowedLaceBlock = 6;
    private static readonly string[] RhymeLines = ["something_old", "something_new", "something_borrowed", "something_blue"];
    public static string RhymeLineId(int line) => RhymeLines[line];

    public static StatusData BridalRhyme()
    {
        var bride = Lawgiver(BridalRhymeId);
        var round = new RoundNumberExpression<CardsDrawnTriggeredEffectContext>();
        var line = new RemainderExpression<CardsDrawnTriggeredEffectContext>(
            new SubtractExpression<CardsDrawnTriggeredEffectContext>(round, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
            new ConstantExpression<CardsDrawnTriggeredEffectContext>(RhymeLines.Length));
        IEffectNode<TContext> MarkAll<TContext>(CardZone zone, string mark, string? onlyMarked = null) where TContext : class =>
            new ForEachCardInZoneNode<TContext>(Applicant, zone,
                MarkCard(new IteratedCardExpression<TContext>(), mark),
                maxIterations: ZoneWalkLimit, markFilter: onlyMarked is null ? null : new TagId(onlyMarked));
        IEffectNode<CardsDrawnTriggeredEffectContext> Ask(int index) =>
            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(bride, new StatusDefinitionId(RhymeLineId(index)),
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1));
        var lend = new CreateCardInstanceNode<CardsDrawnTriggeredEffectContext>(
            Applicant, new CardDefinitionId(BorrowedLaceId), CardZone.Hand, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1));
        var ask = new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Is(round, ComparisonOperator.Equal, 1),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    MarkAll<CardsDrawnTriggeredEffectContext>(CardZone.Hand, OwnCardMark),
                    MarkAll<CardsDrawnTriggeredEffectContext>(CardZone.DrawPile, OwnCardMark),
                    MarkAll<CardsDrawnTriggeredEffectContext>(CardZone.DiscardPile, OwnCardMark),
                ])),
            .. Enumerable.Range(0, RhymeLines.Length).Select(index => (IEffectNode<CardsDrawnTriggeredEffectContext>)
                new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(bride, new StatusDefinitionId(RhymeLineId(index)))),
            new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(bride, new StatusDefinitionId(RhymeKeptId)),
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    Is(line, ComparisonOperator.Equal, 0),
                    Is(MarkedInZone<CardsDrawnTriggeredEffectContext>(CardZone.Hand, HeldOverMark), ComparisonOperator.Greater, 0)),
                Ask(0),
                @else: new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(Is(line, ComparisonOperator.LessOrEqual, 1), Ask(1),
                    @else: new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(Is(line, ComparisonOperator.Equal, 2),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>([Ask(2), lend]),
                        @else: Ask(3)))),
        ]);

        // Made in the fight: the first copy of whatever is created (a creation names a set; the rest are caught by
        // lacking the own-deck mark, unless they were made in the very first turn).
        var made = MarkCard(new TriggerEventCardInstanceExpression<CardInstanceCreatedTriggeredEffectContext>(), MadeInTheFightMark);

        // The turn's end: what of your own stays in hand is held over; whatever was discarded after all is not.
        var holdOver = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            Unmark<TurnEndedTriggeredEffectContext>(HeldOverMark, CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile),
            MarkAll<TurnEndedTriggeredEffectContext>(CardZone.Hand, HeldOverMark, onlyMarked: OwnCardMark),
        ]);
        var letGo = Unmark<HandDiscardedTriggeredEffectContext>(HeldOverMark, CardZone.DiscardPile, CardZone.ExhaustPile);

        var played = ThePlayedCard;
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Asked(int index, ICombatExpression<CardPlayedTriggeredEffectContext, bool> fits) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(Wearing<CardPlayedTriggeredEffectContext>(bride, RhymeLineId(index)), fits);
        var given = new OrExpression<CardPlayedTriggeredEffectContext>(
            new OrExpression<CardPlayedTriggeredEffectContext>(
                Asked(0, CardHasMark(played, HeldOverMark)),
                Asked(1, new AndExpression<CardPlayedTriggeredEffectContext>(
                    new AndExpression<CardPlayedTriggeredEffectContext>(CardHasMark(played, OwnCardMark),
                        new NotExpression<CardPlayedTriggeredEffectContext>(CardHasMark(played, MadeInTheFightMark))),
                    new NotExpression<CardPlayedTriggeredEffectContext>(CardHasMark(played, HeldOverMark))))),
            new OrExpression<CardPlayedTriggeredEffectContext>(
                Asked(2, new OrExpression<CardPlayedTriggeredEffectContext>(CardHasMark(played, MadeInTheFightMark),
                    new NotExpression<CardPlayedTriggeredEffectContext>(CardHasMark(played, OwnCardMark)))),
                Asked(3, PlayedIs(Cards.CardAuthoring.WorkingTag))));
        var keep = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new NotExpression<CardPlayedTriggeredEffectContext>(Wearing<CardPlayedTriggeredEffectContext>(bride, RhymeKeptId)), given),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(bride, new StatusDefinitionId(RhymeKeptId),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                new ModifyDefensivePoolNode<CardPlayedTriggeredEffectContext>(bride, StandardCombatIds.BlockDefensivePool,
                    new NegateExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantDefensivePoolExpression<CardPlayedTriggeredEffectContext>(bride, StandardCombatIds.BlockDefensivePool))),
            ]));

        var refused = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new NotExpression<TurnEndedTriggeredEffectContext>(Wearing<TurnEndedTriggeredEffectContext>(bride, RhymeKeptId)),
            Breach<TurnEndedTriggeredEffectContext>(bride));

        return Rule(BridalRhymeId, "The Bridal Rhyme",
            "Each turn she asks for something old (a card held over), new (drawn this turn), borrowed (made in the fight) or blue (a Working). Play it and she drops her Block; refuse and you break her law.",
            [
                AtTheOpeningHand(bride, ask),
                WhenYouPlay(bride, keep),
                AtYourTurnsEnd(bride, new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>([refused, holdOver])),
                On("HandDiscarded", new ConditionalEffectNode<HandDiscardedTriggeredEffectContext>(
                    PlayersTurn<HandDiscardedTriggeredEffectContext>(), letGo), StatusTriggerScope.Anywhere),
                On("CardInstanceCreated", new ConditionalEffectNode<CardInstanceCreatedTriggeredEffectContext>(
                    Alive<CardInstanceCreatedTriggeredEffectContext>(bride), made), StatusTriggerScope.Anywhere),
            ]);
    }

    public static IEnumerable<StatusData> BridalRhymeStatuses() =>
    [
        Marker(RhymeLineId(0), "Something Old", "Play a card you held over from last turn, or you break the Bride's law."),
        Marker(RhymeLineId(1), "Something New", "Play a card of your deck drawn this turn, or you break the Bride's law."),
        Marker(RhymeLineId(2), "Something Borrowed", "Play a card made in this fight (she lent you Borrowed Lace), or you break the Bride's law."),
        Marker(RhymeLineId(3), "Something Blue", "Play a Working this turn, or you break the Bride's law."),
        Marker(RhymeKeptId, "Rhyme Kept", "You gave her the line she asked for: her Block fell, and her law is kept this turn."),
    ];

    public static CardData BorrowedLace() =>
        Gift(BorrowedLaceId, "Borrowed Lace", $"Gain {BorrowedLaceBlock} Block. Lent by the Bride: gone at the end of the turn.", BridalLoanTag,
            new GainBlockNode<CardPlayContext>(CombatantTargetSelectors.Source, N(BorrowedLaceBlock)), cost: 1);

    // ── Crossroads Cup — Drink Before Choosing ────────────────────────────────────────────────────────────
    //
    // Each turn of yours a drink stands in your hand: +1 Energy, and the Cup gains 2 Claims (user, 2026-11-03).
    public static StatusData DrinkBeforeChoosing()
    {
        var cup = Lawgiver(DrinkBeforeChoosingId);
        var pour = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), Alive<TurnStartedTriggeredEffectContext>(cup)),
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new CardDefinitionId(CrossroadsDrinkCardId), CardZone.Hand,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));
        return Rule(DrinkBeforeChoosingId, "Drink Before Choosing",
            "Each turn the Cup offers you a drink: +1 Energy, but the Cup gains 2 Claims.",
            [On("TurnStarted", pour, StatusTriggerScope.Anywhere)]);
    }

    public static CardData DrinkFromTheCup() => new()
    {
        Id = CrossroadsDrinkCardId,
        NameKey = "Drink from the Cup",
        DescriptionKey = "Gain 1 Energy. The Cup gains 2 Claims. Unplayed, it is gone at the end of the turn.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new GainResourceNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                    new ConstantExpression<CardPlayContext>(1)),
                CreateClaim<CardPlayContext>(Lawgiver(DrinkBeforeChoosingId)),
                CreateClaim<CardPlayContext>(Lawgiver(DrinkBeforeChoosingId)),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };
}
