using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.LieutenantKeywords;
using static BnbContent.Converter.Lieutenant.Statuses;
using AR = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.ActionResolvedTriggeredEffectContext>;
using CD = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardsDrawnTriggeredEffectContext>;
using CP = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardPlayedTriggeredEffectContext>;
using D = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.DamageReceivedTriggeredEffectContext>;
using RA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.RuleAnnouncedTriggeredEffectContext>;
using SA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.StatusAppliedTriggeredEffectContext>;
using SM = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.StatusMergedTriggeredEffectContext>;
using TE = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnEndedTriggeredEffectContext>;
using TS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// The rules her nineteen relics put into every fight (§4). A relic's rule is a status on her from the first turn,
// as every relic's is; its id is the relic's own.
public static partial class LieutenantRules
{
    public const string TinFife = "lt_relic_tin_fife";
    public const string ChippedBuckler = "lt_relic_chipped_buckler";
    public const string SplitChinstrap = "lt_relic_split_chinstrap";
    public const string SwordKnot = "lt_relic_sword_knot";
    public const string WarrantCord = "lt_relic_warrant_cord";
    public const string ParadeSpur = "lt_relic_parade_spur";
    public const string PayChest = "lt_relic_pay_chest";
    public const string DentedGorget = "lt_relic_dented_gorget";
    public const string DentedGorgetDiscount = "lt_dented_gorget";
    public const string TornStandard = "lt_relic_torn_standard";
    public const string FullTang = "lt_relic_full_tang";
    public const string ScarletEpaulette = "lt_relic_scarlet_epaulette";
    public const string Commission = "lt_relic_commission";
    public const string LeadKnuckles = "lt_relic_lead_knuckles";
    public const string IronShackle = "lt_relic_iron_shackle";
    public const string BrandyFlask = "lt_relic_brandy_flask";
    public const string BadgeChain = "lt_relic_badge_chain";
    // Order of the Day and Promissory Note keep the ids they had as Worn Epaulette and Spur Buckle.
    public const string OrderOfTheDay = "lt_relic_worn_epaulette";
    public const string OrderOfTheDayDiscount = "lt_worn_epaulette";
    public const string PromissoryNote = "lt_relic_spur_buckle";
    public const string RegimentalDrum = "lt_relic_regimental_drum";

    // Commission's bridge to the run: her Authority as it stands, written down whenever it changes, so the
    // fight's last state can be read when it resolves.
    public const string AuthorityLeft = "lt_authority_left";

    internal static ICombatExpression<T, bool> AnyChallengeOn<T>(ICombatantTargetSelector who) where T : class =>
        N<T>.AnyOf([.. Promises.Challenges.Select(c => N<T>.Wears(who, c.Id))]);

    private static ICombatExpression<T, bool> IsAnOrder<T>() where T : class =>
        N<T>.AnyOf([.. Promises.Orders.Select(o => N<T>.StatusIs(o.Id))]);

    private static ICombatExpression<T, bool> IsAChallenge<T>() where T : class =>
        N<T>.AnyOf([.. Promises.Challenges.Select(c => N<T>.StatusIs(c.Id))]);

    private static StatusData Relic(string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers,
        IReadOnlyList<PassiveModifierData>? passives = null) =>
        Status(id, name, StatusPolarity.Neutral, description, triggers, passives);

    private static IReadOnlyList<StatusData> RelicRules() =>
    [
        // ── Common ───────────────────────────────────────────────────────────────────────────────────────
        // A fife, not a whistle: hand filtering, not a card more.
        Relic(TinFife, "Tin Fife", "The first time each turn you issue an Order, draw 1 card, then put a card from your hand on the bottom of your draw pile.",
        [
            On(TriggerEvent.StatusApplied, SA.If(SA.And(IsAnOrder<StatusAppliedTriggeredEffectContext>(), SA.Fresh(SA.Self, Latch(TinFife))),
                SA.Seq(SA.Spend(SA.Self, Latch(TinFife)), SA.Draw(SA.Self, 1),
                    new MoveCardToZoneNode<StatusAppliedTriggeredEffectContext>(SA.Self,
                        new ChosenCardInZoneExpression<StatusAppliedTriggeredEffectContext>(CardZone.Hand, "put a card on the bottom of your draw pile"),
                        CardZone.DrawPile)))),
        ]),
        // A hit of hers on the Challenged enemy notes the round; the action's close pays, once.
        Relic(ChippedBuckler, "Chipped Buckler", "The first time each turn a Deed of yours hits a Challenged enemy, gain 4 Block.",
        [
            On(TriggerEvent.DamageTaken, D.If(D.And(D.Wears(D.Self, Command), D.And(D.CardTagged(Cards.CardAuthoring.DeedTag),
                    AnyChallengeOn<DamageReceivedTriggeredEffectContext>(D.Event))),
                D.Set(Her, ChippedBuckler + "_hit", D.Round)), StatusTriggerScope.Anywhere),
            On(TriggerEvent.ActionResolved, AR.If(AR.And(AR.Is(AR.Minus(AR.Counter(AR.Self, ChippedBuckler + "_hit"), AR.Round), 0),
                    AR.Fresh(AR.Self, Latch(ChippedBuckler))),
                AR.Seq(AR.Spend(AR.Self, Latch(ChippedBuckler)), AR.Block(AR.Self, 4)))),
        ]),
        // Rewards reading the intents: Open Guard taken into a phase that never struck it. Armed when her turn ends
        // with Open Guard, disarmed when a hit spends it, paid with the next hand.
        Relic(SplitChinstrap, "Split Chinstrap", "At the start of your turn, if your Open Guard lapses unspent, draw 1 card.",
        [
            On(TriggerEvent.TurnEnded, TE.If(TE.Wears(TE.Self, OpenGuard),
                TE.Set(TE.Self, SplitChinstrap + "_owed", 1), TE.Set(TE.Self, SplitChinstrap + "_owed", 0))),
            On(TriggerEvent.RuleAnnounced, RA.If(RA.Announced(GuardSpent), RA.Set(Her, SplitChinstrap + "_owed", 0)), StatusTriggerScope.Anywhere),
            On(TriggerEvent.CardsDrawn, CD.If(CD.Is(CD.Counter(CD.Self, SplitChinstrap + "_owed"), 1),
                CD.Seq(CD.Set(CD.Self, SplitChinstrap + "_owed", 0), CD.Draw(CD.Self, 1)))),
        ]),

        // ── Uncommon ─────────────────────────────────────────────────────────────────────────────────────
        Relic(SwordKnot, "Sword Knot", "The first time each turn you spend Strength on a card, gain 1 Authority.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(StrengthPaid), RA.Fresh(Her, Latch(SwordKnot))),
                RA.Seq(RA.Spend(Her, Latch(SwordKnot)), GainAuthority<RuleAnnouncedTriggeredEffectContext>(Her, 1))), StatusTriggerScope.Anywhere),
        ]),
        // MTG: Raid — a kept Challenge is a card more.
        Relic(WarrantCord, "Warrant Cord", "The first time each turn you keep a Challenge, draw 1 card.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.ChallengeFulfilled), RA.Fresh(Her, Latch(WarrantCord))),
                RA.Seq(RA.Spend(Her, Latch(WarrantCord)), RA.Draw(Her, 1))), StatusTriggerScope.Anywhere),
        ]),
        // No question asked: the turn ends exposed, and the first enemy blow before her next turn pays as it lands.
        Relic(ParadeSpur, "Parade Spur", "At the end of your turn, if you have Open Guard, gain 1 Authority the next time an enemy attacks you before your next turn.",
        [
            On(TriggerEvent.TurnEnded, TE.If(TE.Wears(TE.Self, OpenGuard),
                TE.Set(TE.Self, ParadeSpur + "_wager", 1), TE.Set(TE.Self, ParadeSpur + "_wager", 0))),
            On(TriggerEvent.DamageTaken, D.If(D.And(ByAnEnemy<DamageReceivedTriggeredEffectContext>(), D.Is(D.Counter(D.Event, ParadeSpur + "_wager"), 1)),
                D.Seq(D.Set(D.Event, ParadeSpur + "_wager", 0), GainAuthority<DamageReceivedTriggeredEffectContext>(D.Event, 1)))),
            On(TriggerEvent.TurnStarted, TS.Set(TS.Self, ParadeSpur + "_wager", 0)),
        ]),
        // Read after the turn's own promises: one kept AT the end is announced after this has looked, so it waits.
        Relic(PayChest, "Pay Chest", "At the end of your turn, if you have 3 or more Authority, gain 5 Block.",
        [
            On(TriggerEvent.TurnEnded, TE.If(TE.AtLeast(TE.Stacks(TE.Self, Authority), 3),
                TE.Block(TE.Self, 5), TE.Set(TE.Self, PayChest + "_waiting", 1))),
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.OrderFulfilled),
                    RA.And(RA.Is(RA.Counter(Her, PayChest + "_waiting"), 1), RA.AtLeast(RA.Stacks(Her, Authority), 3))),
                RA.Seq(RA.Set(Her, PayChest + "_waiting", 0), RA.Block(Her, 5))), StatusTriggerScope.Anywhere),
            On(TriggerEvent.TurnStarted, TS.Set(TS.Self, PayChest + "_waiting", 0)),
        ]),
        Relic(DentedGorget, "Dented Gorget", "Once each enemy phase, when an enemy hit spends your Open Guard, your first Deed next turn costs 1 less.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.Announced(GuardSpent), RA.Set(Her, DentedGorget + "_owed", 1)), StatusTriggerScope.Anywhere),
            On(TriggerEvent.CardsDrawn, CD.If(CD.Is(CD.Counter(CD.Self, DentedGorget + "_owed"), 1), CD.Seq(
                CD.Set(CD.Self, DentedGorget + "_owed", 0),
                CD.Apply(CD.Self, DentedGorgetDiscount, 1),
                MarkHand<CardsDrawnTriggeredEffectContext>(Cards.CardAuthoring.DeedTag, -1)))),
        ]),
        Discount(DentedGorgetDiscount, "Dented Gorget", Cards.CardAuthoring.DeedTag, 1, "Your first Deed this turn costs 1 less."),

        // ── Rare ─────────────────────────────────────────────────────────────────────────────────────────
        // A failed Order hardens her; the first kept Order each turn pays a little more (relic audit L3).
        Relic(TornStandard, "Torn Standard", "Whenever an Order fails, gain 1 Strength. The first time each turn you keep an Order, gain 1 Authority.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.Announced(Obligations.OrderFailed), RA.Apply(Her, Strength, 1)), StatusTriggerScope.Anywhere),
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.OrderFulfilled), RA.Fresh(Her, Latch(TornStandard))),
                RA.Seq(RA.Spend(Her, Latch(TornStandard)), GainAuthority<RuleAnnouncedTriggeredEffectContext>(Her, 1))), StatusTriggerScope.Anywhere),
        ]),
        Relic(FullTang, "Full Tang", "The first time each turn you spend Strength on a card, deal 6 damage to the weakest enemy for each Strength spent (at most 3).",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(StrengthPaid), RA.Fresh(Her, Latch(FullTang))),
                RA.Seq(RA.Spend(Her, Latch(FullTang)),
                    RA.Wound(CombatantTargetSelectors.LowestHealthEnemyOfSource,
                        RA.Times(RA.Min(RA.C(3), RA.Counter(Her, StrengthPaidLast)), 6)))), StatusTriggerScope.Anywhere),
        ]),
        ScarletEpauletteRule(),
        Status(EpauletteTarget, "Scarlet Epaulette", StatusPolarity.Neutral, "The Lieutenant's next blow falls here once more."),
        // Commission's stored Authority arrives through the relic's own openings (LieutenantRelics); the rule keeps
        // the count the run reads when the fight resolves, and its presence raises her Authority cap to 8
        // (LieutenantKeywords.CapOf, CardShapes.GainAuthority).
        Relic(Commission, "Commission", "Your Authority cap is 8. You begin each combat with up to 3 Authority that you ended the last victory with.",
        [
            On(TriggerEvent.StatusApplied, SA.If(SA.StatusIs(Authority), SA.Set(SA.Event, AuthorityLeft, SA.Stacks(SA.Event, Authority)))),
            On(TriggerEvent.StatusMerged, SM.If(SM.StatusIs(Authority), SM.Set(SM.Event, AuthorityLeft, SM.Stacks(SM.Event, Authority)))),
            // A payment takes Authority off without an application; every action of hers and every turn's end
            // writes the count again.
            On(TriggerEvent.ActionResolved, AR.Set(AR.Self, AuthorityLeft, AR.Stacks(AR.Self, Authority))),
            On(TriggerEvent.TurnEnded, TE.Set(TE.Self, AuthorityLeft, TE.Stacks(TE.Self, Authority))),
        ]),

        // ── Shop ─────────────────────────────────────────────────────────────────────────────────────────
        // The Deed's own blow may be over by the time its play is heard, so the 8 come as a blow of their own on its
        // target — no Strength, no second Deed hit.
        Relic(LeadKnuckles, "Lead Knuckles", "Once each turn, when you play a Deed with Open Guard, you may Exhaust another card from your hand to deal 8 more damage to its target.",
        [
            On(TriggerEvent.CardPlayed, CP.If(CP.And(CP.CardTagged(Cards.CardAuthoring.DeedTag),
                    CP.And(CP.Wears(CP.Self, OpenGuard), CP.And(CP.Fresh(CP.Self, Latch(LeadKnuckles)),
                        CP.AtLeast(new CombatantZoneCardCountExpression<CardPlayedTriggeredEffectContext>(CP.Self, CardZone.Hand), 1)))),
                CP.Choose(["Exhaust a card from your hand: 8 more damage", "Keep your hand"],
                    CP.Seq(CP.Spend(CP.Self, Latch(LeadKnuckles)),
                        new MoveCardToZoneNode<CardPlayedTriggeredEffectContext>(CP.Self,
                            new ChosenCardInZoneExpression<CardPlayedTriggeredEffectContext>(CardZone.Hand, "choose a card to Exhaust"),
                            CardZone.ExhaustPile),
                        CP.Wound(CP.Event, CP.C(8))),
                    CP.Nothing))),
        ]),
        Relic(IronShackle, "Iron Shackle", "The first time each turn you issue a Challenge, you may gain 1 Open Guard to apply 2 Doubt to its target.",
        [
            On(TriggerEvent.StatusApplied, SA.If(SA.And(IsAChallenge<StatusAppliedTriggeredEffectContext>(), SA.Fresh(Her, Latch(IronShackle))),
                SA.Seq(SA.Spend(Her, Latch(IronShackle)),
                    SA.Choose(["Gain 1 Open Guard: 2 Doubt to the Challenged enemy", "Not this time"],
                        SA.Seq(SA.Apply(Her, OpenGuard, 1), SA.Apply(SA.Event, Cards.Keywords.Doubt, 2)), SA.Nothing))), StatusTriggerScope.Anywhere),
        ]),
        // The healing is the run's (LieutenantRelics); the fight only keeps Open Guard's count (GuardExtraTotal).
        Relic(BrandyFlask, "Brandy Flask", "After a victorious combat, heal half the HP Open Guard cost you in it (at most 6).", []),
        Relic(BadgeChain, "Badge Chain", "Once each turn, when you keep a Challenge, you may turn 1 of the Authority it gave you into 1 Strength.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.ChallengeFulfilled),
                    RA.And(RA.Fresh(Her, Latch(BadgeChain)), RA.AtLeast(RA.Stacks(Her, Authority), 1))),
                RA.Seq(RA.Spend(Her, Latch(BadgeChain)),
                    RA.Choose(["Turn 1 Authority into 1 Strength", "Keep the Authority"],
                        RA.Seq(RA.Adjust(Her, Authority, RA.C(-1)), RA.Apply(Her, Strength, 1)), RA.Nothing))), StatusTriggerScope.Anywhere),
        ]),
        Relic(OrderOfTheDay, "Order of the Day", "The first time each turn you keep an Order, your next Working costs 1 less, this turn or the next.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.OrderFulfilled),
                    RA.And(RA.Fresh(Her, Latch(OrderOfTheDay)), RA.Not(RA.Wears(Her, OrderOfTheDayDiscount)))),
                RA.Seq(RA.Spend(Her, Latch(OrderOfTheDay)), RA.Apply(Her, OrderOfTheDayDiscount, 1),
                    MarkHand<RuleAnnouncedTriggeredEffectContext>(Cards.CardAuthoring.WorkingTag, -1))), StatusTriggerScope.Anywhere),
        ]),
        Discount(OrderOfTheDayDiscount, "Order of the Day", Cards.CardAuthoring.WorkingTag, 1,
            "Your next Working costs 1 less, this turn or the next.", intoNextTurn: true),
        // The surcharge is no card's payment: nothing that answers a Strength payment hears it (§2.7).
        Relic(PromissoryNote, "Promissory Note", "Once each turn, after a card spends your Strength, you may spend 1 more Strength to gain 1 Energy.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(StrengthPaid),
                    RA.And(RA.Fresh(Her, Latch(PromissoryNote)), RA.AtLeast(RA.Stacks(Her, Strength), 1))),
                RA.Seq(RA.Spend(Her, Latch(PromissoryNote)),
                    RA.Choose(["Spend 1 more Strength: gain 1 Energy", "Keep the Strength"],
                        RA.Seq(RA.Adjust(Her, Strength, RA.C(-1)), RA.GainEnergy(Her, 1)), RA.Nothing))), StatusTriggerScope.Anywhere),
        ]),
        // The trade the shop's others make: more to work with, and the guard open from the first blow. Given with
        // the first hand, once a fight, so the Open Guard is not lapsed by the first turn's own start.
        Relic(RegimentalDrum, "Regimental Drum", "Start each combat with 1 Open Guard, 1 Authority and 1 Strength.",
        [
            On(TriggerEvent.CardsDrawn, CD.If(CD.Is(CD.Counter(CD.Self, RegimentalDrum + "_given"), 0), CD.Seq(
                CD.Set(CD.Self, RegimentalDrum + "_given", 1),
                CD.Apply(CD.Self, OpenGuard, 1),
                GainAuthority<CardsDrawnTriggeredEffectContext>(CD.Self, 1),
                CD.Apply(CD.Self, Strength, 1)))),
        ]),
    ];

    // Scarlet Epaulette: the first Open Guard each turn readies her next Deed to strike once more for half of what
    // its blow came to. Its hits only note the target and the size (set, not added: one card is one card); the
    // close of that Deed's action strikes the extra blow, with her Strength in it once.
    private static StatusData ScarletEpauletteRule()
    {
        var ready = ScarletEpaulette + "_ready";
        var size = ScarletEpaulette + "_size";
        var marked = ScarletEpaulette + "_target";
        var target = CombatantTargetSelectors.FirstTarget(CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(EpauletteTarget)));
        IEffectNode<T> Ready<T>() where T : class => N<T>.If(N<T>.And(N<T>.StatusIs(OpenGuard), N<T>.Fresh(Her, Latch(ScarletEpaulette))),
            N<T>.Seq(N<T>.Spend(Her, Latch(ScarletEpaulette)), N<T>.Set(Her, ready, 1)));
        return Relic(ScarletEpaulette, "Scarlet Epaulette",
            "The first time each turn you gain Open Guard, your next Deed this turn strikes its target once more for half of its blow.",
        [
            On(TriggerEvent.StatusApplied, Ready<StatusAppliedTriggeredEffectContext>()),
            On(TriggerEvent.StatusMerged, Ready<StatusMergedTriggeredEffectContext>()),
            On(TriggerEvent.DamageTaken, D.If(D.And(D.Is(D.Counter(Her, ready), 1),
                    D.And(D.Wears(D.Self, Command), D.CardTagged(Cards.CardAuthoring.DeedTag))),
                D.All(D.Set(Her, size, D.Plus(D.Amount, new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>())),
                    D.Apply(D.Event, EpauletteTarget, 1), D.Set(Her, marked, 1))), StatusTriggerScope.Anywhere),
            On(TriggerEvent.ActionResolved, AR.If(AR.Is(AR.Counter(AR.Self, marked), 1), AR.Seq(
                AR.Set(AR.Self, marked, 0), AR.Set(AR.Self, ready, 0),
                AR.If(AR.AtLeast(AR.Count(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants,
                        new StatusDefinitionId(EpauletteTarget))), 1),
                    AR.Hit(target, AR.Half(AR.Minus(AR.Counter(AR.Self, size), AR.Max(AR.C(0), AR.Stacks(AR.Self, Strength)))))),
                AR.Each(Obligations.Bearers(EpauletteTarget), AR.Remove(AR.It, EpauletteTarget))))),
            On(TriggerEvent.TurnEnded, TE.Set(TE.Self, ready, 0)),
        ]);
    }

    public const string EpauletteTarget = "lt_mark_epaulette";
}
