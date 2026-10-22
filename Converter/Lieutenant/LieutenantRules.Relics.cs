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

// The rules her eighteen relics put into every fight (§4). A relic's rule is a status on her from the first turn,
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
    public const string WornEpaulette = "lt_relic_worn_epaulette";
    public const string WornEpauletteDiscount = "lt_worn_epaulette";
    public const string SpurBuckle = "lt_relic_spur_buckle";

    // Commission's bridge to the run: her Authority as it stands, written down whenever it changes, so the
    // fight's last state can be read when it resolves.
    public const string AuthorityLeft = "lt_authority_left";

    private static ICombatExpression<T, bool> AnyChallengeOn<T>(ICombatantTargetSelector who) where T : class
    {
        ICombatExpression<T, bool>? any = null;
        foreach (var c in Promises.Challenges)
        {
            var one = N<T>.Wears(who, c.Id);
            any = any is null ? one : N<T>.Or(any, one);
        }
        return any!;
    }

    private static ICombatExpression<T, bool> IsAnOrder<T>() where T : class
    {
        ICombatExpression<T, bool>? any = null;
        foreach (var o in Promises.Orders)
        {
            var one = N<T>.StatusIs(o.Id);
            any = any is null ? one : N<T>.Or(any, one);
        }
        return any!;
    }

    private static ICombatExpression<T, bool> IsAChallenge<T>() where T : class
    {
        ICombatExpression<T, bool>? any = null;
        foreach (var c in Promises.Challenges)
        {
            var one = N<T>.StatusIs(c.Id);
            any = any is null ? one : N<T>.Or(any, one);
        }
        return any!;
    }

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
        Relic(SplitChinstrap, "Split Chinstrap", "Once each enemy phase, when an enemy hit spends your Open Guard and takes none of your HP, gain 1 Authority at the start of your next turn.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(GuardHeld), RA.Fresh(Her, Latch(SplitChinstrap))),
                RA.Seq(RA.Spend(Her, Latch(SplitChinstrap)), RA.Set(Her, SplitChinstrap + "_owed", 1))), StatusTriggerScope.Anywhere),
            On(TriggerEvent.TurnStarted, TS.If(TS.Is(TS.Counter(TS.Self, SplitChinstrap + "_owed"), 1),
                TS.Seq(TS.Set(TS.Self, SplitChinstrap + "_owed", 0), GainAuthority<TurnStartedTriggeredEffectContext>(TS.Self, 1)))),
        ]),

        // ── Uncommon ─────────────────────────────────────────────────────────────────────────────────────
        Relic(SwordKnot, "Sword Knot", "The first time each turn you spend Strength on a card, gain 1 Authority.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(StrengthPaid), RA.Fresh(Her, Latch(SwordKnot))),
                RA.Seq(RA.Spend(Her, Latch(SwordKnot)), GainAuthority<RuleAnnouncedTriggeredEffectContext>(Her, 1))), StatusTriggerScope.Anywhere),
        ]),
        Relic(WarrantCord, "Warrant Cord", "Once each turn, when your Challenge ends because its target fell before you kept it, gain 1 Authority.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.ChallengeLost), RA.Fresh(Her, Latch(WarrantCord))),
                RA.Seq(RA.Spend(Her, Latch(WarrantCord)), GainAuthority<RuleAnnouncedTriggeredEffectContext>(Her, 1))), StatusTriggerScope.Anywhere),
        ]),
        // The wager is on the phase: an enemy struck her, and she is still standing when her turn comes.
        Relic(ParadeSpur, "Parade Spur", "At the end of your turn, if you have Open Guard, you may gain 1 more. If you do and an enemy attacks you before your next turn, gain 1 Authority when it comes.",
        [
            On(TriggerEvent.TurnEnded, TE.If(TE.Wears(TE.Self, OpenGuard),
                TE.Choose(["Gain 1 more Open Guard and wager", "Not this time"],
                    TE.Seq(TE.Apply(TE.Self, OpenGuard, 1), TE.Set(TE.Self, ParadeSpur + "_wager", 1)),
                    TE.Nothing))),
            On(TriggerEvent.TurnStarted, TS.If(TS.Is(TS.Counter(TS.Self, ParadeSpur + "_wager"), 1), TS.Seq(
                TS.Set(TS.Self, ParadeSpur + "_wager", 0),
                TS.If(TS.AtLeast(TS.Counter(TS.Self, PhaseHits), 1), GainAuthority<TurnStartedTriggeredEffectContext>(TS.Self, 1))))),
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
        // The canon's "you may Challenge": laid when no Challenge of hers is out, so it never displaces one.
        Relic(TornStandard, "Torn Standard", "Once each turn, when an Order fails at its deadline and you have no Challenge out, Challenge the weakest enemy: hit it with a Deed by the end of your next turn (1 Authority).",
            [On(TriggerEvent.RuleAnnounced, TornStandardAnswer(), StatusTriggerScope.Anywhere)]),
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
        // the count the run reads when the fight resolves.
        Relic(Commission, "Commission", "You begin each combat with 1 Authority, and with up to 2 more that you ended the last victory with.",
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
        Relic(WornEpaulette, "Worn Epaulette", "The first time each turn you keep an Order, your next Working costs 1 less, this turn or the next.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.OrderFulfilled),
                    RA.And(RA.Fresh(Her, Latch(WornEpaulette)), RA.Not(RA.Wears(Her, WornEpauletteDiscount)))),
                RA.Seq(RA.Spend(Her, Latch(WornEpaulette)), RA.Apply(Her, WornEpauletteDiscount, 1),
                    MarkHand<RuleAnnouncedTriggeredEffectContext>(Cards.CardAuthoring.WorkingTag, -1))), StatusTriggerScope.Anywhere),
        ]),
        Discount(WornEpauletteDiscount, "Worn Epaulette", Cards.CardAuthoring.WorkingTag, 1,
            "Your next Working costs 1 less, this turn or the next.", intoNextTurn: true),
        // The surcharge is no card's payment: nothing that answers a Strength payment hears it (§2.7).
        Relic(SpurBuckle, "Spur Buckle", "Once each turn, after a card spends your Strength, you may spend 1 more Strength to gain 1 Energy.",
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(StrengthPaid),
                    RA.And(RA.Fresh(Her, Latch(SpurBuckle)), RA.AtLeast(RA.Stacks(Her, Strength), 1))),
                RA.Seq(RA.Spend(Her, Latch(SpurBuckle)),
                    RA.Choose(["Spend 1 more Strength: gain 1 Energy", "Keep the Strength"],
                        RA.Seq(RA.Adjust(Her, Strength, RA.C(-1)), RA.GainEnergy(Her, 1)), RA.Nothing))), StatusTriggerScope.Anywhere),
        ]),
    ];

    private static IEffectNode<RuleAnnouncedTriggeredEffectContext> TornStandardAnswer()
    {
        var c = Promises.TornStandard;
        var target = CombatantTargetSelectors.LowestHealthEnemyOfSource;
        var anyOut = Promises.Challenges.Select(x => (ICombatExpression<RuleAnnouncedTriggeredEffectContext, bool>)RA.AtLeast(
                RA.Count(CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(x.Id))), 1))
            .Aggregate((a, b) => RA.Or(a, b));
        return RA.If(RA.And(RA.Announced(Obligations.OrderFailed), RA.And(RA.Not(anyOut), RA.Fresh(Her, Latch(TornStandard)))),
            RA.Seq(
                RA.Spend(Her, Latch(TornStandard)),
                RA.Set(target, Obligations.Armed(c.Id), 1),
                RA.Set(target, Obligations.ArmedRoundOf(c.Id), RA.Round),
                RA.Set(target, Obligations.Progress(c.Id), 0),
                RA.Set(target, Obligations.Grace(c.Id), 0),
                RA.Set(target, Obligations.Remanded(c.Id), 0),
                RA.Apply(target, c.Id, 1)));
    }

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
                AR.Each(CombatantTargetSelectors.AllCombatants, AR.Remove(AR.It, EpauletteTarget))))),
            On(TriggerEvent.TurnEnded, TE.Set(TE.Self, ready, 0)),
        ]);
    }

    public const string EpauletteTarget = "lt_mark_epaulette";
}
