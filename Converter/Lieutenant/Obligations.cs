using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.LieutenantKeywords;
using static BnbContent.Converter.Lieutenant.Statuses;
using AR = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.ActionResolvedTriggeredEffectContext>;
using SA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.StatusAppliedTriggeredEffectContext>;
using TE = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnEndedTriggeredEffectContext>;
using TS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>;
using D = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.DamageReceivedTriggeredEffectContext>;
using AS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.ActionStartingTriggeredEffectContext>;
using DN = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CombatantDownedTriggeredEffectContext>;
using CD = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardsDrawnTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// What a kept promise pays (§2.2): 1 Authority unless the card says more, then whatever else it names.
public sealed record Reward(int Authority = 1, int Block = 0, int Strength = 0, int Draw = 0, int NextTurnDraw = 0)
{
    public string Text
    {
        get
        {
            var parts = new List<string> { $"{Authority} Authority" };
            if (Strength > 0) parts.Add($"{Strength} Strength");
            if (Block > 0) parts.Add($"{Block} Block");
            if (Draw > 0) parts.Add(Draw == 1 ? "draw 1 card" : $"draw {Draw} cards");
            if (NextTurnDraw > 0) parts.Add($"draw {NextTurnDraw} more next turn");
            return parts.Count == 1 ? parts[0] : string.Join(", ", parts[..^1]) + " and " + parts[^1];
        }
    }

    internal IEffectNode<T> Pay<T>(ICombatantTargetSelector her) where T : class => N<T>.Seq(
    [
        GainAuthority<T>(her, Authority),
        .. Strength > 0 ? [N<T>.Apply(her, LieutenantKeywords.Strength, Strength)] : Array.Empty<IEffectNode<T>>(),
        .. Block > 0 ? [N<T>.Block(her, Block)] : Array.Empty<IEffectNode<T>>(),
        .. Draw > 0 ? [N<T>.Draw(her, Draw)] : Array.Empty<IEffectNode<T>>(),
        .. NextTurnDraw > 0 ? [N<T>.Apply(her, Obligations.Reinforcements, NextTurnDraw)] : Array.Empty<IEffectNode<T>>(),
    ]);
}

// What an Order asks of her own actions (§2.3). Each kind is one way of being judged.
public enum OrderKind
{
    PlayLater,         // play a card of the tag later this turn
    DeedAndWorking,    // play a Deed and a Working later this turn
    ExactlyMore,       // play exactly N more cards this turn (judged at the turn's end)
    EndWithNoEnergy,   // end the turn with exactly 0 Energy
    Restraint,         // no further Deed this turn, and end it with Energy left
    EndWithBlock,      // end the turn with at least N Block
    EndExposed,        // end the turn with Open Guard and at least N Block
    OpenNextTurn,      // the first card you play next turn is a Deed
    Withstand,         // in the next enemy phase an enemy attacks you and no Attack HP gets through
    WithstandExposed,  // …and you go into it with Open Guard
}

public sealed record OrderSpec(string Id, string Name, OrderKind Kind, string Condition, Reward Reward, int N = 0)
{
    public string Text => $"Order: {Condition}. Success: {Reward.Text}.";
}

// What a Challenge asks about one living enemy (§2.4).
public enum ChallengeKind
{
    Hits,              // hit it with Deeds N times before it acts
}

public sealed record ChallengeSpec(string Id, string Name, ChallengeKind Kind, string Condition, Reward Reward, int N = 1)
{
    public string Text => $"Challenge: {Condition}. Success: {Reward.Text}.";
}

// Orders and Challenges: issuing one, its places, how it is judged, and the one way it ends (§2.3–§2.6).
//
// An Order is a status on her, a Challenge a status on its enemy; each kind of promise is its own status, so its
// chip says what it asks. A card ISSUES one by applying it last; her standing status then gives it its place —
// one Order (two under Chain of Command, the oldest giving way) and one Challenge in the whole fight, the new one
// replacing the old without success or failure (L6/L7).
//
// The card that issued a promise can never keep it (§2.3: "only subsequent actions count"). The engine resolves a
// card's whole program before the events it raised, so the issuing card's own hits and its own card-played
// event are heard AFTER the promise exists. A promise therefore waits, unarmed, for the issuing action to close
// (ActionResolved) and only then starts listening; at that moment it also writes down what had been played.
public static class Obligations
{
    public const string OrderTag = "lt_order";
    public const string ChallengeTag = "lt_challenge";

    // Draw this many more cards at the start of next turn (a reward's "next turn" draw).
    public const string Reinforcements = "lt_reinforcements";

    public const string OrderFulfilled = "lt_order_fulfilled";
    public const string OrderFailed = "lt_order_failed";
    public const string OrderReplaced = "lt_order_replaced";
    public const string ChallengeFulfilled = "lt_challenge_fulfilled";
    public const string ChallengeFailed = "lt_challenge_failed";
    public const string ChallengeLost = "lt_challenge_lost";

    // Her counters. The two Order places hold an Order's number (its place in `Orders` + 1; 0 = empty), the
    // older first.
    public const string OlderOrder = "lt_order_older";
    public const string NewerOrder = "lt_order_newer";
    public const string OrdersKept = "lt_orders_kept";
    public const string ChallengesWon = "lt_challenges_won";
    public const string OrderFailedRound = "lt_order_failed_round";
    public const string OrderKeptRound = "lt_order_kept_round";
    public const string ChallengeWonRound = "lt_challenge_won_round";

    // Chain of Command (rare): a second Order place. The id is the Rite's; the Rite lives with the rares.
    public const string ChainOfCommand = "lt_chain_of_command";

    // A promise's own counters. The issuing CARD clears them before it lays the promise down (CardShapes.Issue):
    // the events of one action are heard together, so a status trigger clearing them on issue would race the
    // arming that the same action's close sets off.
    public static string Armed(string id) => id + "_armed";
    public static string Progress(string id) => id + "_progress";
    private static string Mark(string id, string what) => id + "_at_" + what;
    private static string ArmedRound(string id) => id + "_round";

    // ── the catalogue (Promises) ───────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<OrderSpec> OrderList => Promises.Orders;
    private static IReadOnlyList<ChallengeSpec> ChallengeList => Promises.Challenges;

    public static IReadOnlyList<StatusData> All()
    {
        return
        [
            CommandStatus(),
            Status(Reinforcements, "Reinforcements", StatusPolarity.Buff,
                "Draw this many more cards at the start of your next turn.",
                triggers: [On(TriggerEvent.CardsDrawn, CD.Remove(CD.Self, Reinforcements))],
                passives: [new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, 1)]),
            .. OrderList.Select(OrderStatus),
            .. ChallengeList.Select(ChallengeStatus),
        ];
    }

    // ── her standing status ──────────────────────────────────────────────────────────────────────────────

    private static StatusData CommandStatus()
    {
        // An enemy phase is counted from the end of her turn to the start of the next one.
        var turnEnded = TE.Seq(TE.Set(TE.Self, PhaseHits, 0), TE.Set(TE.Self, PhaseHp, 0));
        var struck = D.If(ByAnEnemy<DamageReceivedTriggeredEffectContext>(),
            D.Seq(D.Add(D.Event, PhaseHits, D.C(1)), D.Add(D.Event, PhaseHp, D.Amount)));

        // Anything applied anywhere: is it a promise being issued?
        var issued = SA.All(
        [
            SA.If(SA.StatusIs(StrengthPulse), SA.Seq(
                SA.Set(SA.Event, StrengthPaidLast, SA.Stacks(SA.Event, StrengthPulse)),
                SA.Set(SA.Event, StrengthPaidRound, SA.Round),
                SA.Remove(SA.Event, StrengthPulse),
                SA.Announce(SA.Event, StrengthPaid))),
            SA.If(SA.StatusIs(AuthorityPulse), SA.Seq(
                SA.Set(SA.Event, AuthorityPaidLast, SA.Stacks(SA.Event, AuthorityPulse)),
                SA.Remove(SA.Event, AuthorityPulse),
                SA.Announce(SA.Event, AuthorityPaid))),
            .. OrderList.Select((o, i) => SA.If(SA.StatusIs(o.Id), IssueOrder(o, i + 1))),
            .. ChallengeList.Select(c => SA.If(SA.StatusIs(c.Id), IssueChallenge(c))),
        ]);

        return Status(Command, "Lieutenant", StatusPolarity.Neutral,
            "Orders are promises about your own actions, Challenges promises about one enemy. You hold one Order and " +
            "one Challenge at a time; a new one replaces the old. Keeping one earns Authority. The card that issues " +
            "a promise never counts toward it.",
            triggers:
            [
                On(TriggerEvent.TurnEnded, turnEnded),
                On(TriggerEvent.DamageTaken, struck),
                On(TriggerEvent.StatusApplied, issued, StatusTriggerScope.Anywhere),
            ]);
    }

    // The Order takes a place: the newest place if there is room, otherwise the oldest Order gives way. A second
    // copy of the same Order replaces the first.
    private static IEffectNode<StatusAppliedTriggeredEffectContext> IssueOrder(OrderSpec order, int number)
    {
        var her = SA.Event;
        var older = SA.Counter(her, OlderOrder);
        var newer = SA.Counter(her, NewerOrder);
        return SA.Seq(
            ReleasePlace<StatusAppliedTriggeredEffectContext>(her, number),
            SA.If(SA.Wears(her, ChainOfCommand),
                SA.If(SA.Above(newer, SA.C(0)),
                    SA.Seq(GiveWay(her, OlderOrder), SA.Set(her, OlderOrder, newer), SA.Set(her, NewerOrder, number)),
                    SA.If(SA.Above(older, SA.C(0)), SA.Set(her, NewerOrder, number), SA.Set(her, OlderOrder, number))),
                SA.Seq(GiveWay(her, OlderOrder), GiveWay(her, NewerOrder),
                    SA.Set(her, OlderOrder, number), SA.Set(her, NewerOrder, 0))));
    }

    // The Order in this place is replaced: it ends without success or failure (§2.3).
    private static IEffectNode<StatusAppliedTriggeredEffectContext> GiveWay(ICombatantTargetSelector her, string place) =>
        SA.All([.. OrderList.Select((o, i) => SA.If(SA.Is(SA.Counter(her, place), i + 1),
            SA.Seq(SA.Remove(her, o.Id), SA.Announce(her, OrderReplaced))))]);

    // An Order that ends leaves its place; the newer one moves up.
    private static IEffectNode<T> ReleasePlace<T>(ICombatantTargetSelector her, int number) where T : class =>
        N<T>.Seq(
            N<T>.If(N<T>.Is(N<T>.Counter(her, OlderOrder), number),
                N<T>.Seq(N<T>.Set(her, OlderOrder, N<T>.Counter(her, NewerOrder)), N<T>.Set(her, NewerOrder, 0))),
            N<T>.If(N<T>.Is(N<T>.Counter(her, NewerOrder), number), N<T>.Set(her, NewerOrder, 0)));

    // One Challenge in the whole fight: every other is withdrawn, without success or failure (§2.4).
    private static IEffectNode<StatusAppliedTriggeredEffectContext> IssueChallenge(ChallengeSpec challenge)
    {
        var all = CombatantTargetSelectors.AllCombatants;
        return SA.Seq(
        [
            .. ChallengeList.Where(c => c.Id != challenge.Id).Select(c => SA.Each(all, SA.Remove(SA.It, c.Id))),
            SA.Each(CombatantTargetSelectors.Except(all, SA.Event), SA.Remove(SA.It, challenge.Id)),
        ]);
    }

    // ── Orders ───────────────────────────────────────────────────────────────────────────────────────────

    private static int NumberOf(OrderSpec order) => OrderList.ToList().IndexOf(order) + 1;

    private static IEffectNode<T> Kept<T>(OrderSpec order, ICombatantTargetSelector her) where T : class =>
        N<T>.Seq(
            N<T>.Remove(her, order.Id),
            ReleasePlace<T>(her, NumberOf(order)),
            order.Reward.Pay<T>(her),
            N<T>.Add(her, OrdersKept, N<T>.C(1)),
            N<T>.Set(her, OrderKeptRound, N<T>.Round),
            N<T>.Announce(her, OrderFulfilled));

    private static IEffectNode<T> Failed<T>(OrderSpec order, ICombatantTargetSelector her) where T : class =>
        N<T>.Seq(
            N<T>.Remove(her, order.Id),
            ReleasePlace<T>(her, NumberOf(order)),
            N<T>.Set(her, OrderFailedRound, N<T>.Round),
            N<T>.Announce(her, OrderFailed));

    private static StatusData OrderStatus(OrderSpec order)
    {
        var her = AR.Self;
        var armed = AR.Is(AR.Counter(her, Armed(order.Id)), 1);
        var arm = AR.Seq(
            AR.Set(her, Armed(order.Id), 1),
            AR.Set(her, ArmedRound(order.Id), AR.Round),
            AR.Set(her, Mark(order.Id, "played"), AR.Played(her)),
            AR.Set(her, Mark(order.Id, "deeds"), AR.PlayedTagged(her, Cards.CardAuthoring.DeedTag)),
            AR.Set(her, Mark(order.Id, "workings"), AR.PlayedTagged(her, Cards.CardAuthoring.WorkingTag)));

        var triggers = new List<StatusTriggerData>
        {
            On(TriggerEvent.ActionResolved, AR.If(armed, OnAction(order) ?? AR.Nothing, arm)),
        };
        if (OnTurnEnd(order) is { } end)
            triggers.Add(On(TriggerEvent.TurnEnded, TE.If(TE.Is(TE.Counter(TE.Self, Armed(order.Id)), 1), end)));
        if (OnTurnStart(order) is { } start)
            triggers.Add(On(TriggerEvent.TurnStarted, TS.If(TS.Is(TS.Counter(TS.Self, Armed(order.Id)), 1), start)));

        return Status(order.Id, "Order: " + order.Name, StatusPolarity.Neutral,
            $"{Capitalised(order.Condition)}. Success: {order.Reward.Text}.", triggers, tags: [OrderTag]);
    }

    private static IEffectNode<ActionResolvedTriggeredEffectContext>? OnAction(OrderSpec order)
    {
        var her = AR.Self;
        ICombatExpression<ActionResolvedTriggeredEffectContext, int> Since(string what, string? tag) =>
            AR.Minus(tag is null ? AR.Played(her) : AR.PlayedTagged(her, tag), AR.Counter(her, Mark(order.Id, what)));

        return order.Kind switch
        {
            OrderKind.PlayLater => AR.If(AR.AtLeast(Since("deeds", Cards.CardAuthoring.DeedTag), 1), Kept<ActionResolvedTriggeredEffectContext>(order, her)),
            OrderKind.DeedAndWorking => AR.If(
                AR.And(AR.AtLeast(Since("deeds", Cards.CardAuthoring.DeedTag), 1), AR.AtLeast(Since("workings", Cards.CardAuthoring.WorkingTag), 1)),
                Kept<ActionResolvedTriggeredEffectContext>(order, her)),
            // Next turn's first card: judged by the first action of a later round.
            OrderKind.OpenNextTurn => AR.If(AR.Above(AR.Round, AR.Counter(her, ArmedRound(order.Id))),
                AR.If(new FirstCardPlayedThisTurnHasTagExpression<ActionResolvedTriggeredEffectContext>(her, new TagId(Cards.CardAuthoring.DeedTag)),
                    Kept<ActionResolvedTriggeredEffectContext>(order, her), Failed<ActionResolvedTriggeredEffectContext>(order, her))),
            OrderKind.Restraint => AR.If(AR.AtLeast(Since("deeds", Cards.CardAuthoring.DeedTag), 1), Failed<ActionResolvedTriggeredEffectContext>(order, her)),
            _ => null,
        };
    }

    private static IEffectNode<TurnEndedTriggeredEffectContext>? OnTurnEnd(OrderSpec order)
    {
        var her = TE.Self;
        var kept = Kept<TurnEndedTriggeredEffectContext>(order, her);
        var failed = Failed<TurnEndedTriggeredEffectContext>(order, her);
        ICombatExpression<TurnEndedTriggeredEffectContext, int> Since(string what) =>
            TE.Minus(TE.Played(her), TE.Counter(her, Mark(order.Id, what)));

        return order.Kind switch
        {
            // Still standing when the turn ends: the deadline has passed.
            OrderKind.PlayLater or OrderKind.DeedAndWorking => failed,
            OrderKind.ExactlyMore => TE.If(TE.Is(Since("played"), order.N), kept, failed),
            OrderKind.EndWithNoEnergy => TE.If(TE.Is(TE.Energy(her), 0), kept, failed),
            OrderKind.Restraint => TE.If(TE.AtLeast(TE.Energy(her), 1), kept, failed),
            OrderKind.EndWithBlock => TE.If(TE.AtLeast(TE.BlockOf(her), order.N), kept, failed),
            OrderKind.EndExposed => TE.If(TE.And(TE.AtLeast(TE.BlockOf(her), order.N), TE.Wears(her, OpenGuard)), kept, failed),
            // Next turn's first card: a turn that ends in a later round without one has failed it.
            OrderKind.OpenNextTurn => TE.If(TE.Above(TE.Round, TE.Counter(her, ArmedRound(order.Id))), failed),
            // Into the enemy phase exposed, or not at all.
            OrderKind.WithstandExposed => TE.If(TE.Not(TE.Wears(her, OpenGuard)), failed),
            _ => null,
        };
    }

    private static IEffectNode<TurnStartedTriggeredEffectContext>? OnTurnStart(OrderSpec order)
    {
        var her = TS.Self;
        var withstood = TS.And(TS.AtLeast(TS.Counter(her, PhaseHits), 1), TS.Is(TS.Counter(her, PhaseHp), 0));
        return order.Kind switch
        {
            OrderKind.Withstand or OrderKind.WithstandExposed => TS.If(withstood,
                Kept<TurnStartedTriggeredEffectContext>(order, her), Failed<TurnStartedTriggeredEffectContext>(order, her)),
            _ => null,
        };
    }

    // ── Challenges ───────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<T> Won<T>(ChallengeSpec challenge, ICombatantTargetSelector enemy) where T : class
    {
        var her = Her;
        return N<T>.Seq(
            N<T>.Remove(enemy, challenge.Id),
            challenge.Reward.Pay<T>(her),
            N<T>.Add(her, ChallengesWon, N<T>.C(1)),
            N<T>.Set(her, ChallengeWonRound, N<T>.Round),
            N<T>.Announce(her, ChallengeFulfilled));
    }

    private static IEffectNode<T> Ended<T>(ChallengeSpec challenge, ICombatantTargetSelector enemy, string how) where T : class =>
        N<T>.Seq(N<T>.Remove(enemy, challenge.Id), N<T>.Announce(Her, how));

    private static StatusData ChallengeStatus(ChallengeSpec challenge)
    {
        var bearer = BearerOf(challenge.Id);
        var fallen = BearerOf(challenge.Id, fallen: true);

        // Armed once the issuing action has closed — any action closing after the Challenge was laid.
        var arm = AR.If(AR.Is(AR.Count(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                new StatusDefinitionId(challenge.Id))), 1),
            AR.If(AR.Is(AR.Counter(bearer, Armed(challenge.Id)), 0), AR.Set(bearer, Armed(challenge.Id), 1)));

        var armed = D.Is(D.Counter(D.Event, Armed(challenge.Id)), 1);
        var deedHit = D.And(armed, D.And(D.CardTagged(Cards.CardAuthoring.DeedTag), D.Wears(D.Self, Command)));
        var hit = challenge.Kind switch
        {
            ChallengeKind.Hits => D.If(deedHit, D.Seq(
                D.Add(D.Event, Progress(challenge.Id), D.C(1)),
                D.If(D.AtLeast(D.Counter(D.Event, Progress(challenge.Id)), challenge.N),
                    Won<DamageReceivedTriggeredEffectContext>(challenge, D.Event)))),
            _ => D.Nothing,
        };

        // Before it acts: the deadline (§2.4).
        var deadline = AS.If(AS.Is(AS.Counter(bearer, Armed(challenge.Id)), 1),
            Ended<ActionStartingTriggeredEffectContext>(challenge, bearer, ChallengeFailed));

        // Down before the promise was kept: the target is lost (§2.4), which is not a failure.
        var lost = Ended<CombatantDownedTriggeredEffectContext>(challenge, fallen, ChallengeLost);

        return Status(challenge.Id, "Challenge: " + challenge.Name, StatusPolarity.Neutral,
            $"The Lieutenant's Challenge: {challenge.Condition}. Success: {challenge.Reward.Text}.",
            triggers:
            [
                On(TriggerEvent.ActionResolved, arm, StatusTriggerScope.Anywhere),
                On(TriggerEvent.DamageTaken, hit),
                On(TriggerEvent.ActionStarting, deadline),
                On(TriggerEvent.Downed, lost),
            ],
            tags: [ChallengeTag]);
    }

    // "Is the target Challenged?" — for card programs: 1 when it carries any Challenge.
    public static CombatAmountSpec ChallengedAmount(string on = Cards.CardAuthoring.Target)
    {
        CombatAmountSpec? total = null;
        foreach (var c in ChallengeList)
        {
            var one = Cards.CardAuthoring.Stacks(c.Id, on);
            total = total is null ? one : Cards.CardAuthoring.Plus(total, one);
        }
        return Cards.CardAuthoring.Once(total ?? CombatAmountSpec.FromConst(0));
    }

    private static string Capitalised(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
