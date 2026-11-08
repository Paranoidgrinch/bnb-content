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
using RA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.RuleAnnouncedTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// What a kept promise pays (§2.2): 1 Authority unless the card says more, then whatever else it names.
public sealed record Reward(int Authority = 1, int Block = 0, int Strength = 0, int Draw = 0, int NextTurnDraw = 0, int WardWax = 0)
{
    public string Text
    {
        get
        {
            var parts = new List<string> { $"{Authority} Authority" };
            if (Strength > 0) parts.Add($"{Strength} Strength");
            if (Block > 0) parts.Add($"{Block} Block");
            if (WardWax > 0) parts.Add($"{WardWax} Ward Wax");
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
        .. WardWax > 0 ? [N<T>.Apply(her, Cards.Keywords.WardWax, WardWax)] : Array.Empty<IEffectNode<T>>(),
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
    TwoEnemies,        // hit two different enemies with Deeds later this turn
}

public sealed record OrderSpec(string Id, string Name, OrderKind Kind, string Condition, Reward Reward, int N = 0)
{
    public string Text => $"Order: {Condition}. Success: {Reward.Text}.";
}

// What a Challenge asks about one living enemy (§2.4).
public enum ChallengeKind
{
    Hits,              // hit it with Deeds N times before it acts
    WithstandAttack,   // its next Attack action, within its next two actions, takes no HP from her
    Kill,              // down it by the end of her next turn
    BigHit,            // one later Deed hit takes at least N of its HP, before it acts
    Pursuit,           // a Deed hit later this turn, and another in her next turn
    KeepOrder,         // keep an Order before it acts
    KeepBlock,         // after its next Attack (within its next two actions) she still has Block
    TakeHit,           // its next Attack (within its next two actions) takes her HP, and she lives
    DistinctDeeds,     // hits from N different Deeds played later, before it acts
    SurviveThenHit,    // live through its next Attack (within its next two actions), then a Deed hit before it acts again
    HitByNextTurn,     // a Deed hit by the end of her next turn, whatever it does meanwhile
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

    // Witness (common): the next Deed (two, upgraded) this turn counts toward her Challenge whichever enemy it
    // hits, one stack each.
    public const string Witness = "lt_witness";

    // A promise's own counters. The issuing CARD clears them before it lays the promise down (CardShapes.Issue):
    // the events of one action are heard together, so a status trigger clearing them on issue would race the
    // arming that the same action's close sets off.
    public static string Armed(string id) => id + "_armed";
    public static string Progress(string id) => id + "_progress";
    // Remand: one deadline forgiven (grace), and whether this Challenge has had its one extension.
    public static string Grace(string id) => id + "_grace";
    public static string Remanded(string id) => id + "_remanded";
    // Redress: this bearer's Challenge is a copy, laid beside the first; its arrival withdraws nothing.
    public static string Copied(string id) => id + "_copy";
    // A Deed of hers struck the bearer during the action now closing (Second Blade): set, never added, so
    // however many hits one card makes it is one card.
    private static string Struck(string id) => id + "_struck";
    // Black Gauntlet's first stage done: the attack survived, the answer owed.
    private const int Survived = 10;
    // Double File: the enemies a Deed of hers has hit since the Order was armed.
    public const string DoubleFileMark = "lt_mark_double_file";
    // Remand's request, applied by the card and answered here.
    public const string RemandPulse = "lt_pulse_remand";
    // Field Pardon, Redress, Insubordination and Reprimand ask the same way.
    public const string PardonPulse = "lt_pulse_pardon";
    public const string RedressPulse = "lt_pulse_redress";
    public const string AbandonPulse = "lt_pulse_abandon";
    public const string FailPulse = "lt_pulse_fail";
    // The enemy whose Challenge was last kept (Succession looks past it), and the one that last struck her Open
    // Guard (Blood Debt pays it back).
    public const string LastWon = "lt_mark_last_won";
    public const string GuardStriker = "lt_mark_guard_striker";
    public static string ArmedRoundOf(string id) => ArmedRound(id);
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
            Status(DoubleFileMark, "Hit for Double File", StatusPolarity.Neutral,
                "A Deed of the Lieutenant's has hit this enemy for her Double File Order."),
            Status(RemandPulse, "Remand", StatusPolarity.Neutral, "Your Challenge is extended."),
            Status(PardonPulse, "Field Pardon", StatusPolarity.Neutral, "Every standing promise is declared kept."),
            Status(RedressPulse, "Redress", StatusPolarity.Neutral, "The Lieutenant's Challenge is copied onto every other enemy."),
            Status(AbandonPulse, "Insubordination", StatusPolarity.Neutral, "Your oldest Order is abandoned."),
            Status(FailPulse, "Reprimand", StatusPolarity.Neutral, "Your oldest Order fails."),
            Status(LastWon, "Challenge Kept", StatusPolarity.Neutral, "The Lieutenant's last Challenge was kept against this enemy."),
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
            D.All(D.Add(D.Event, PhaseHits, D.C(1)), D.Add(D.Event, PhaseHp, D.Amount)));

        // Anything applied anywhere: is it a promise being issued, or a pulse? Asked as one chain — this hears
        // every status applied in the fight (see N.FirstOf).
        var issued = SA.FirstOf(
        [
            (SA.StatusIs(StrengthPulse), SA.Seq(
                SA.Set(SA.Event, StrengthPaidLast, SA.Stacks(SA.Event, StrengthPulse)),
                SA.Set(SA.Event, StrengthPaidRound, SA.Round),
                SA.Remove(SA.Event, StrengthPulse),
                SA.Announce(SA.Event, StrengthPaid))),
            (SA.StatusIs(AuthorityPulse), SA.Seq(
                SA.Set(SA.Event, AuthorityPaidLast, SA.Stacks(SA.Event, AuthorityPulse)),
                SA.Set(SA.Event, AuthorityPaidRound, SA.Round),
                SA.Remove(SA.Event, AuthorityPulse),
                SA.Announce(SA.Event, AuthorityPaid))),
            (SA.StatusIs(RemandPulse), SA.Seq(
            [
                .. ChallengeList.Select(c => SA.Each(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                        new StatusDefinitionId(c.Id)),
                    SA.If(SA.Is(SA.Counter(SA.It, Remanded(c.Id)), 0),
                        SA.All(SA.Set(SA.It, Grace(c.Id), 1), SA.Set(SA.It, Remanded(c.Id), 1))))),
                SA.Remove(SA.Event, RemandPulse),
            ])),
            (SA.StatusIs(PardonPulse), Pardon()),
            (SA.StatusIs(RedressPulse), Redress()),
            (SA.StatusIs(AbandonPulse), SA.Seq(
            [
                .. OrderList.Select((o, i) => SA.If(SA.Is(SA.Counter(SA.Event, OlderOrder), i + 1),
                    SA.Seq(SA.Remove(SA.Event, o.Id), ReleasePlace<StatusAppliedTriggeredEffectContext>(SA.Event, i + 1)))),
                SA.Remove(SA.Event, AbandonPulse),
            ])),
            // Reprimand: the oldest Order fails, as at its deadline.
            (SA.StatusIs(FailPulse), SA.Seq(
                SA.FirstOf([.. OrderList.Select((o, i) => (SA.Is(SA.Counter(SA.Event, OlderOrder), i + 1),
                    Failed<StatusAppliedTriggeredEffectContext>(o, SA.Event)))]),
                SA.Remove(SA.Event, FailPulse))),
            .. OrderList.Select((o, i) => (SA.StatusIs(o.Id), IssueOrder(o, i + 1))),
            .. ChallengeList.Select(c => (SA.StatusIs(c.Id), IssueChallenge(c))),
        ]);

        return Status(Command, "Lieutenant", StatusPolarity.Neutral,
            "Orders are promises about your own actions, Challenges promises about one enemy. You hold one Order and " +
            "one Challenge at a time; a new one replaces the old. Keeping one earns Authority. The card that issues " +
            "a promise never counts toward it.",
            triggers:
            [
                On(TriggerEvent.TurnEnded, turnEnded),
                On(TriggerEvent.DamageTaken, struck),
                On(TriggerEvent.ActionStarting, AS.Set(Her, HpBeforeAction,
                    new CombatantCurrentHealthExpression<ActionStartingTriggeredEffectContext>(Her)), StatusTriggerScope.Anywhere),
                On(TriggerEvent.StatusApplied, issued, StatusTriggerScope.Anywhere),
            ]);
    }

    // Field Pardon (content review A8): every promise standing when the card was played is declared kept — each
    // with its normal reward and announcement. The older Order first (keeping it moves the newer one up into its
    // place, so the older place is asked twice), then every bearer of the Challenge, copies included.
    private static IEffectNode<StatusAppliedTriggeredEffectContext> Pardon()
    {
        var her = SA.Event;
        IEffectNode<StatusAppliedTriggeredEffectContext> KeepOlder() => SA.FirstOf(
            [.. OrderList.Select((o, i) => (SA.Is(SA.Counter(her, OlderOrder), i + 1), Kept<StatusAppliedTriggeredEffectContext>(o, her)))]);
        return SA.Seq(
        [
            KeepOlder(),
            KeepOlder(),
            .. ChallengeList.Select(c => SA.Each(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants,
                    new StatusDefinitionId(c.Id)),
                SA.If(SA.Is(SA.Counter(SA.It, Armed(c.Id)), 1), Won<StatusAppliedTriggeredEffectContext>(c, SA.It)))),
            SA.Remove(her, PardonPulse),
        ]);
    }

    // Redress (content review A9): her Challenge is copied onto every other enemy. Each copy is a Challenge of its
    // own — counted, judged and paid on its own bearer, its count and deadline fresh — armed at once, since no
    // card of hers is closing to arm it. A copy withdraws nothing as it arrives (IssueChallenge asks Copied).
    private static IEffectNode<StatusAppliedTriggeredEffectContext> Redress()
    {
        var her = SA.Event;
        IEffectNode<StatusAppliedTriggeredEffectContext> CopyOf(ChallengeSpec c)
        {
            var bearers = CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(c.Id));
            var others = CombatantTargetSelectors.WithoutStatus(CombatantTargetSelectors.AllEnemiesOfSource, new StatusDefinitionId(c.Id));
            return SA.If(SA.AtLeast(SA.Count(bearers), 1), SA.Each(others, SA.Seq(
                SA.Set(SA.It, Copied(c.Id), 1),
                SA.Set(SA.It, Armed(c.Id), 1),
                SA.Set(SA.It, ArmedRound(c.Id), SA.Round),
                SA.Set(SA.It, Progress(c.Id), 0),
                SA.Set(SA.It, Grace(c.Id), 0),
                SA.Set(SA.It, Remanded(c.Id), 0),
                SA.Set(SA.It, Struck(c.Id), 0),
                SA.Apply(SA.It, c.Id, 1))));
        }
        return SA.Seq([.. ChallengeList.Select(CopyOf), SA.Remove(her, RedressPulse)]);
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
        SA.FirstOf([.. OrderList.Select((o, i) => (SA.Is(SA.Counter(her, place), i + 1),
            SA.Seq(SA.Remove(her, o.Id), SA.Announce(her, OrderReplaced))))]);

    // An Order that ends leaves its place; the newer one moves up.
    private static IEffectNode<T> ReleasePlace<T>(ICombatantTargetSelector her, int number) where T : class =>
        N<T>.Seq(
            N<T>.If(N<T>.Is(N<T>.Counter(her, OlderOrder), number),
                N<T>.Seq(N<T>.Set(her, OlderOrder, N<T>.Counter(her, NewerOrder)), N<T>.Set(her, NewerOrder, 0))),
            N<T>.If(N<T>.Is(N<T>.Counter(her, NewerOrder), number), N<T>.Set(her, NewerOrder, 0)));

    // Everyone carrying `status`, the fallen included: what a "take it off everybody" walks over. ⚠ Not every
    // combatant: a fight that summons (Nisaba writes a body per sentence) keeps its gone bodies in the table, and a
    // walk over all of them passed the engine's 32 targets (golden run, 2026-11-07: Arrest against Nisaba).
    internal static ICombatantTargetSelector Bearers(string status) =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(status), includeFallen: true);

    // One Challenge in the whole fight: every other is withdrawn, without success or failure (§2.4) — unless this
    // one is a Redress copy, laid beside the first.
    private static IEffectNode<StatusAppliedTriggeredEffectContext> IssueChallenge(ChallengeSpec challenge) =>
        SA.If(SA.Is(SA.Counter(SA.Event, Copied(challenge.Id)), 0), SA.All(
        [
            .. ChallengeList.Where(c => c.Id != challenge.Id).Select(c => SA.Each(Bearers(c.Id), SA.Remove(SA.It, c.Id))),
            SA.Each(CombatantTargetSelectors.Except(Bearers(challenge.Id), SA.Event), SA.Remove(SA.It, challenge.Id)),
        ]));

    // ── Orders ───────────────────────────────────────────────────────────────────────────────────────────

    private static int NumberOf(OrderSpec order) => OrderList.ToList().IndexOf(order) + 1;

    private static IEffectNode<T> Cleanup<T>(OrderSpec order) where T : class =>
        order.Kind == OrderKind.TwoEnemies
            ? N<T>.Each(Bearers(DoubleFileMark), N<T>.Remove(N<T>.It, DoubleFileMark))
            : N<T>.Nothing;

    private static IEffectNode<T> Kept<T>(OrderSpec order, ICombatantTargetSelector her) where T : class =>
        N<T>.Seq(
            N<T>.Remove(her, order.Id),
            Cleanup<T>(order),
            ReleasePlace<T>(her, NumberOf(order)),
            order.Reward.Pay<T>(her),
            N<T>.Add(her, OrdersKept, N<T>.C(1)),
            N<T>.Set(her, OrderKeptRound, N<T>.Round),
            N<T>.Announce(her, OrderFulfilled));

    private static IEffectNode<T> Failed<T>(OrderSpec order, ICombatantTargetSelector her) where T : class =>
        N<T>.Seq(
            N<T>.Remove(her, order.Id),
            Cleanup<T>(order),
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
        if (order.Kind == OrderKind.TwoEnemies)
            triggers.Add(On(TriggerEvent.DamageTaken, D.If(
                D.And(D.Is(D.Counter(Her, Armed(order.Id)), 1), D.And(D.CardTagged(Cards.CardAuthoring.DeedTag),
                    D.And(D.Wears(D.Self, Command), D.Not(D.Wears(D.Event, Command))))),
                D.Apply(D.Event, DoubleFileMark, 1)), StatusTriggerScope.Anywhere));
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
            OrderKind.TwoEnemies => AR.If(AR.AtLeast(AR.Count(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(DoubleFileMark), includeFallen: true)), 2), Kept<ActionResolvedTriggeredEffectContext>(order, her)),
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
            OrderKind.PlayLater or OrderKind.DeedAndWorking or OrderKind.TwoEnemies => failed,
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
        // `enemy` finds the bearer BY this Challenge, so it is marked before the Challenge comes off.
        return N<T>.Seq(
            N<T>.Each(Bearers(LastWon), N<T>.Remove(N<T>.It, LastWon)),
            N<T>.Apply(enemy, LastWon, 1),
            N<T>.Remove(enemy, challenge.Id),
            challenge.Reward.Pay<T>(her),
            N<T>.Add(her, ChallengesWon, N<T>.C(1)),
            N<T>.Set(her, ChallengeWonRound, N<T>.Round),
            N<T>.Announce(her, ChallengeFulfilled));
    }

    private static IEffectNode<T> Ended<T>(ChallengeSpec challenge, ICombatantTargetSelector enemy, string how) where T : class =>
        N<T>.Seq(N<T>.Remove(enemy, challenge.Id), N<T>.Announce(Her, how));

    private static bool Counted(ChallengeKind kind) =>
        kind is ChallengeKind.Hits or ChallengeKind.BigHit or ChallengeKind.Pursuit or ChallengeKind.DistinctDeeds
            or ChallengeKind.SurviveThenHit or ChallengeKind.HitByNextTurn;

    private static bool AnswersAnAttack(ChallengeKind kind) =>
        kind is ChallengeKind.WithstandAttack or ChallengeKind.KeepBlock or ChallengeKind.TakeHit or ChallengeKind.SurviveThenHit;

    // ⚠ A CHALLENGE MAY HAVE SEVERAL BEARERS: Redress copies it onto every other enemy, and each copy is judged
    // and paid on its own. So every rule below reads the bearer it is about — the enemy hit, the actor, the
    // downed, or each bearer in turn — never "the" bearer of the status.
    private static StatusData ChallengeStatus(ChallengeSpec challenge)
    {
        var id = challenge.Id;
        var bearers = Bearers(id);

        // ⚠ ONE ACTION'S EVENTS ARE HEARD TOGETHER, their programs stepping side by side: two hits of one card
        // both see the count reached, and a judgement made beside the hit that feeds it reads the count before it
        // grew. So a hit only COUNTS (as the first step of its program), and the promise is judged once, when an
        // action closes — the single moment no other part of that action is still being heard. The same close
        // arms a promise laid during the action.
        var it = AR.It;
        var arm = AR.Each(bearers, AR.If(AR.Is(AR.Counter(it, Armed(id)), 1),
            Counted(challenge.Kind) ? JudgeCount(challenge, it) : AR.Nothing,
            AR.Seq(AR.Set(it, Armed(id), 1), AR.Set(it, ArmedRound(id), AR.Round))));

        var triggers = new List<StatusTriggerData> { On(TriggerEvent.ActionResolved, arm, StatusTriggerScope.Anywhere) };

        if (Counted(challenge.Kind))
        {
            triggers.Add(On(TriggerEvent.DamageTaken, Count(challenge)));
            triggers.Add(On(TriggerEvent.DamageTaken, Witnessed(challenge), StatusTriggerScope.Anywhere));
        }
        if (challenge.Kind is ChallengeKind.Hits or ChallengeKind.BigHit or ChallengeKind.DistinctDeeds or ChallengeKind.KeepOrder)
            triggers.Add(On(TriggerEvent.ActionStarting, BeforeItActs(challenge)));
        if (AnswersAnAttack(challenge.Kind))
            triggers.AddRange(AttackResponse(challenge));
        if (challenge.Kind is ChallengeKind.Kill or ChallengeKind.Pursuit or ChallengeKind.HitByNextTurn)
            triggers.Add(On(TriggerEvent.TurnEnded, ByHerNextTurn(challenge), StatusTriggerScope.Anywhere));
        if (challenge.Kind == ChallengeKind.KeepOrder)
            triggers.Add(On(TriggerEvent.RuleAnnounced, RA.If(RA.Announced(OrderFulfilled),
                RA.Each(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(id)),
                    RA.If(RA.Is(RA.Counter(RA.It, Armed(id)), 1), Won<RuleAnnouncedTriggeredEffectContext>(challenge, RA.It)))),
                StatusTriggerScope.Anywhere));

        // Down: a Kill Challenge is kept; any other has lost its target (§2.4), which is not a failure. A counted
        // one is left to the close of the action, which knows whether the downing hit reached the count.
        if (!Counted(challenge.Kind))
        {
            // The body just downed (wrapped as one body, as the fallen bearer always was).
            var downed = CombatantTargetSelectors.FirstTarget(CombatantTargetSelectors.SourceIncludingDowned);
            triggers.Add(On(TriggerEvent.Downed, challenge.Kind == ChallengeKind.Kill
                ? DN.If(DN.Is(DN.Counter(downed, Armed(id)), 1),
                    Won<CombatantDownedTriggeredEffectContext>(challenge, downed),
                    Ended<CombatantDownedTriggeredEffectContext>(challenge, downed, ChallengeLost))
                : Ended<CombatantDownedTriggeredEffectContext>(challenge, downed, ChallengeLost)));
        }

        return Status(id, "Challenge: " + challenge.Name, StatusPolarity.Neutral,
            $"The Lieutenant's Challenge: {challenge.Condition}. Success: {challenge.Reward.Text}.",
            triggers, tags: [ChallengeTag]);
    }

    // A Deed hit of hers on the bearer, once armed — counted, never judged here (see above).
    private static IEffectNode<DamageReceivedTriggeredEffectContext> Count(ChallengeSpec challenge) =>
        D.If(D.And(D.CardTagged(Cards.CardAuthoring.DeedTag), D.Wears(D.Self, Command)), CountOn(challenge, D.Event));

    // Witness: a Deed of hers that strikes an enemy without this Challenge counts on the Challenge's bearer, as if
    // it had struck there. (With copies on every enemy there is nobody without it, so "the bearer" is one body.)
    private static IEffectNode<DamageReceivedTriggeredEffectContext> Witnessed(ChallengeSpec challenge) =>
        D.If(D.And(D.Wears(Her, Witness), D.And(D.CardTagged(Cards.CardAuthoring.DeedTag), D.And(D.Wears(D.Self, Command),
                D.And(D.Not(D.Wears(D.Event, challenge.Id)), D.Not(D.Wears(D.Event, Command)))))),
            CountOn(challenge, BearerOf(challenge.Id)));

    private static IEffectNode<DamageReceivedTriggeredEffectContext> CountOn(ChallengeSpec challenge, ICombatantTargetSelector bearer)
    {
        var id = challenge.Id;
        var progress = D.Counter(bearer, Progress(id));
        var count = challenge.Kind switch
        {
            ChallengeKind.HitByNextTurn => D.Set(bearer, Progress(id), 1),
            ChallengeKind.Hits => D.Add(bearer, Progress(id), D.C(1)),
            ChallengeKind.BigHit => D.If(D.AtLeast(D.Amount, challenge.N), D.Set(bearer, Progress(id), 1)),
            // Later this turn, then again in a later round.
            ChallengeKind.Pursuit => D.If(D.Cmp(D.Round, ComparisonOperator.Equal, D.Counter(bearer, ArmedRound(id))),
                D.Set(bearer, Progress(id), D.Max(progress, D.C(1))),
                D.If(D.AtLeast(progress, 1), D.Set(bearer, Progress(id), 2))),
            ChallengeKind.DistinctDeeds => D.Set(bearer, Struck(id), 1),
            ChallengeKind.SurviveThenHit => D.If(D.AtLeast(progress, Survived), D.Set(bearer, Progress(id), Survived + 1)),
            _ => D.Nothing,
        };
        return D.If(D.Is(D.Counter(bearer, Armed(id)), 1), count);
    }

    private static IEffectNode<ActionResolvedTriggeredEffectContext> JudgeCount(ChallengeSpec challenge, ICombatantTargetSelector bearer)
    {
        var id = challenge.Id;
        var need = challenge.Kind switch
        {
            ChallengeKind.SurviveThenHit => Survived + 1,
            ChallengeKind.Pursuit => 2,
            ChallengeKind.BigHit or ChallengeKind.HitByNextTurn => 1,
            _ => challenge.N,
        };
        return AR.Seq(
            // One played Deed that struck it is one, however many hits it made.
            challenge.Kind == ChallengeKind.DistinctDeeds
                ? AR.If(AR.Is(AR.Counter(bearer, Struck(id)), 1),
                    AR.Seq(AR.Add(bearer, Progress(id), AR.C(1)), AR.Set(bearer, Struck(id), 0)))
                : AR.Nothing,
            AR.If(AR.AtLeast(AR.Counter(bearer, Progress(id)), need),
                Won<ActionResolvedTriggeredEffectContext>(challenge, bearer),
                AR.If(AR.Not(AR.Alive(bearer)), Ended<ActionResolvedTriggeredEffectContext>(challenge, bearer, ChallengeLost))));
    }

    // A deadline, unless Remand has forgiven one.
    private static IEffectNode<T> Deadline<T>(ChallengeSpec challenge, ICombatantTargetSelector bearer) where T : class =>
        N<T>.If(N<T>.Is(N<T>.Counter(bearer, Grace(challenge.Id)), 1),
            N<T>.Set(bearer, Grace(challenge.Id), 0),
            Ended<T>(challenge, bearer, ChallengeFailed));

    // "Before its next action" (§2.4): the bearer about to act, the promise unkept.
    private static IEffectNode<ActionStartingTriggeredEffectContext> BeforeItActs(ChallengeSpec challenge) =>
        AS.If(AS.Is(AS.Counter(AS.Self, Armed(challenge.Id)), 1), Deadline<ActionStartingTriggeredEffectContext>(challenge, AS.Self));

    // "By the end of your next turn": a turn of hers that ends in a later round than the arming. Pursuit also
    // fails at the end of the issuing turn if that turn's hit never came.
    private static IEffectNode<TurnEndedTriggeredEffectContext> ByHerNextTurn(ChallengeSpec challenge)
    {
        var id = challenge.Id;
        var bearer = TE.It;
        var later = TE.Above(TE.Round, TE.Counter(bearer, ArmedRound(id)));
        var due = challenge.Kind == ChallengeKind.Pursuit
            ? TE.Or(later, TE.Is(TE.Counter(bearer, Progress(id)), 0))
            : later;
        return TE.If(TE.Wears(TE.Self, Command),
            TE.Each(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(id)),
                TE.If(TE.And(TE.Is(TE.Counter(bearer, Armed(id)), 1), due), Deadline<TurnEndedTriggeredEffectContext>(challenge, bearer))));
    }

    // A promise about how the bearer's next Attack goes (§2.4): its next action that strikes her, within its next
    // two actions. Before each of its actions her health is written on it; when the action closes, an action that
    // struck her is judged, and two that did not are a failure (three, with Remand's grace). The bearer is the
    // actor.
    private static IEnumerable<StatusTriggerData> AttackResponse(ChallengeSpec challenge)
    {
        var id = challenge.Id;
        var armed = AR.Is(AR.Counter(AR.Self, Armed(id)), 1);
        // Her health itself, not a count kept by another rule: that count is written beside this judgement.
        var before = AS.If(AS.Is(AS.Counter(AS.Self, Armed(id)), 1),
            AS.Set(AS.Self, Mark(id, "hp"), new CombatantCurrentHealthExpression<ActionStartingTriggeredEffectContext>(Her)));
        var lost = AR.Minus(AR.Counter(AR.Self, Mark(id, "hp")), new CombatantCurrentHealthExpression<ActionResolvedTriggeredEffectContext>(Her));
        var kept = challenge.Kind switch
        {
            ChallengeKind.WithstandAttack => AR.Cmp(lost, ComparisonOperator.LessOrEqual, AR.C(0)),
            ChallengeKind.KeepBlock => AR.AtLeast(AR.BlockOf(Her), 1),
            _ => AR.AtLeast(lost, 1),
        };
        var judged = challenge.Kind == ChallengeKind.SurviveThenHit
            // She lived through it: now the answering blow, before its next action.
            ? AR.Set(AR.Self, Progress(id), Survived)
            : AR.If(kept,
                Won<ActionResolvedTriggeredEffectContext>(challenge, AR.Self),
                Ended<ActionResolvedTriggeredEffectContext>(challenge, AR.Self, ChallengeFailed));
        var stillWaiting = AR.Cmp(AR.Counter(AR.Self, Progress(id)), ComparisonOperator.Less, AR.C(Survived));
        var after = AR.If(AR.And(armed, stillWaiting), AR.If(new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(), judged,
            AR.Seq(
                AR.Add(AR.Self, Progress(id), AR.C(1)),
                AR.If(AR.AtLeast(AR.Counter(AR.Self, Progress(id)), 2),
                    Deadline<ActionResolvedTriggeredEffectContext>(challenge, AR.Self)))));
        // Black Gauntlet, once she has survived: the next action it starts is the deadline for her answer.
        var answerDue = AS.If(AS.And(AS.Is(AS.Counter(AS.Self, Armed(id)), 1), AS.AtLeast(AS.Counter(AS.Self, Progress(id)), Survived)),
            Deadline<ActionStartingTriggeredEffectContext>(challenge, AS.Self));
        yield return On(TriggerEvent.ActionStarting, challenge.Kind == ChallengeKind.SurviveThenHit ? AS.Seq(answerDue, before) : before);
        yield return On(TriggerEvent.ActionResolved, after);
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
