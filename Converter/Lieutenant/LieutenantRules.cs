using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.LieutenantKeywords;
using static BnbContent.Converter.Lieutenant.Statuses;
using CP = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardPlayedTriggeredEffectContext>;
using CD = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardsDrawnTriggeredEffectContext>;
using RA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.RuleAnnouncedTriggeredEffectContext>;
using TE = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnEndedTriggeredEffectContext>;
using TS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// The marks the Lieutenant's cards leave on her when what they say outlasts them (lieutenant_master.md §3): a
// rule that outlives its card lives on a status, as the other characters' do.
public static partial class LieutenantRules
{
    public const string Witness = Obligations.Witness;
    public const string Buckle = "lt_buckle";
    public const string SecondRank = "lt_second_rank";
    public const string Headlong = "lt_headlong";

    public static IReadOnlyList<StatusData> All() =>
    [
        Status(Witness, "Witness", StatusPolarity.Buff,
            "Your next Deed hit on a Challenged enemy this turn counts twice toward its Challenge.",
            triggers: [On(TriggerEvent.TurnEnded, TE.Remove(TE.Self, Witness))]),
        BuckleMark(),
        Discount(SecondRank, "Second Rank", Cards.CardAuthoring.DeedTag, 99,
            "Your next Deed this turn costs 0."),
        Discount(Headlong, "Headlong", Cards.CardAuthoring.DeedTag, 1,
            "Your next Deed this turn costs 1 less, once per stack."),
        .. UncommonRules(),
        .. RareRules(),
        .. RelicRules(),
    ];

    // Buckle: an Order kept before the end of this turn takes back the Open Guard the card gave.
    private static StatusData BuckleMark() =>
        Status(Buckle, "Buckle", StatusPolarity.Neutral,
            "If you keep an Order before the end of this turn, lose this much Open Guard.",
            triggers:
            [
                On(TriggerEvent.RuleAnnounced, RA.If(RA.Announced(Obligations.OrderFulfilled),
                    RA.Seq(RA.Adjust(RA.Self, OpenGuard, RA.Minus(RA.C(0), RA.Min(RA.Stacks(RA.Self, Buckle), RA.Stacks(RA.Self, OpenGuard)))),
                        RA.Remove(RA.Self, Buckle)))),
                // Read through her turn's end, so a promise kept AT the end still counts.
                On(TriggerEvent.TurnStarted, TS.Remove(TS.Self, Buckle)),
            ]);

    // ── "your next Deed costs …" ─────────────────────────────────────────────────────────────────────────
    //
    // A price written on each card of the kind in her hand (the engine's per-copy price, which the play
    // spends) — by the card that grants it (CardShapes.Discount), since a status never hears its own arrival —
    // renewed on every card she draws while the discount stands; the first such card she plays uses
    // one stack, and when the last is used — or the turn ends — the prices come off again.

    private static StatusData Discount(string id, string name, string tag, int off, string description, bool intoNextTurn = false)
    {
        var aged = id + "_aged";
        var endOfTurn = intoNextTurn
            ? TE.If(TE.Is(TE.Counter(TE.Self, aged), 1),
                TE.Seq(MarkHand<TurnEndedTriggeredEffectContext>(tag, 0), TE.Set(TE.Self, aged, 0), TE.Remove(TE.Self, id)),
                // The hand goes to the discard pile now; the next turn's draw is priced again as it comes.
                TE.Seq(MarkHand<TurnEndedTriggeredEffectContext>(tag, 0), TE.Set(TE.Self, aged, 1)))
            : TE.Seq(MarkHand<TurnEndedTriggeredEffectContext>(tag, 0), TE.Remove(TE.Self, id));
        return Status(id, name, StatusPolarity.Buff, description,
            triggers:
            [
                OnEveryDraw(MarkHand<CardsDrawnTriggeredEffectContext>(tag, -off)),
                On(TriggerEvent.CardPlayed, CP.If(CP.CardTagged(tag), CP.Seq(
                    CP.Adjust(CP.Self, id, CP.C(-1)),
                    CP.If(CP.Not(CP.Wears(CP.Self, id)), MarkHand<CardPlayedTriggeredEffectContext>(tag, 0))))),
                On(TriggerEvent.TurnEnded, endOfTurn),
            ]);
    }

    internal static IEffectNode<T> MarkHand<T>(string tag, int delta) where T : class =>
        new ForEachCardInZoneNode<T>(Her, CardZone.Hand,
            new ConditionalEffectNode<T>(
                new CardInstanceHasTagExpression<T>(new IteratedCardExpression<T>(), new TagId(tag)),
                new SetCardInstanceMarkCounterNode<T>(Her, new IteratedCardExpression<T>(),
                    StandardCombatIds.CardCostDeltaCounter, new ConstantExpression<T>(delta), relative: false)),
            maxIterations: 20);
}
