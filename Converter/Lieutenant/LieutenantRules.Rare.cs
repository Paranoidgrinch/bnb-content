using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.LieutenantKeywords;
using static BnbContent.Converter.Lieutenant.Statuses;
using AR = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.ActionResolvedTriggeredEffectContext>;
using CD = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardsDrawnTriggeredEffectContext>;
using D = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.DamageReceivedTriggeredEffectContext>;
using RA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.RuleAnnouncedTriggeredEffectContext>;
using TE = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnEndedTriggeredEffectContext>;
using TS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// The rares' marks and Rites (§3.4, with the guardrails of §2.7). Each status's stacks carry its number where the
// upgrade changes it, so one status serves both twins.
public static partial class LieutenantRules
{
    public const string ChainOfCommand = Obligations.ChainOfCommand;
    public const string ChainBase = "lt_chain_base";
    public const string Brevet = "lt_brevet";
    public const string ParadeGround = "lt_parade_ground";
    public const string ParadeBase = "lt_parade_base";
    public const string ParadeCap = "lt_parade_cap";
    public const string DrumMajor = "lt_drum_major";
    public const string Succession = "lt_succession";
    public const string Riposte = "lt_riposte";
    public const string HiltKnot = "lt_hilt_knot";
    public const string ForcedMarch = "lt_forced_march";
    public const string PikeSquare = "lt_pike_square";
    public const string Armsmaster = "lt_armsmaster";
    public const string ArmsmasterSaving = "lt_armsmaster_saving";
    public const string BloodDebt = "lt_blood_debt";
    public const string OpenVisor = "lt_open_visor";
    public const string RisingStar = "lt_rising_star";
    public const string Watchword = "lt_watchword";
    public const string WatchwordOrders = "lt_watchword_orders";
    public const string WatchwordChallenges = "lt_watchword_challenges";
    public const string WatchSurgeon = "lt_watch_surgeon";

    private static IReadOnlyList<StatusData> RareRules() =>
    [
        ChainRite(),
        Status(Brevet, "Brevet", StatusPolarity.Buff, "The next Authority you spend this turn costs 1 less.",
            triggers: [On(TriggerEvent.TurnEnded, TE.Remove(TE.Self, Brevet))]),
        ParadeGroundMark(),
        DrumMajorMark(),
        SuccessionRite(),
        RiposteRite(),
        Status(HiltKnot, "Hilt Knot", StatusPolarity.Buff,
            "The first time each turn you spend Strength on a card, draw 1 card and gain this much Block.",
            triggers:
            [
                On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(StrengthPaid), RA.Fresh(Her, Latch(HiltKnot))),
                    RA.Seq(RA.Spend(Her, Latch(HiltKnot)), RA.Draw(Her, 1), RA.Block(Her, RA.Stacks(Her, HiltKnot)))), StatusTriggerScope.Anywhere),
            ]),
        Status(ForcedMarch, "Forced March", StatusPolarity.Neutral,
            "At the end of this turn, lose up to this much of the Strength you still have.",
            triggers:
            [
                On(TriggerEvent.TurnEnded, TE.Seq(
                    TE.Adjust(TE.Self, Strength, TE.Minus(TE.C(0), TE.Min(TE.Stacks(TE.Self, ForcedMarch), TE.Max(TE.C(0), TE.Stacks(TE.Self, Strength))))),
                    TE.Remove(TE.Self, ForcedMarch))),
            ]),
        PikeSquareMark(),
        ArmsmasterRite(),
        Status(ArmsmasterSaving, "Armsmaster's Saving", StatusPolarity.Buff,
            "Your next payment of 2 or more Strength this turn costs 1 less.",
            triggers: [On(TriggerEvent.TurnEnded, TE.Remove(TE.Self, ArmsmasterSaving))]),
        BloodDebtMark(),
        Status(GuardStriker, "Struck the Open Guard", StatusPolarity.Neutral,
            "This enemy was the last to strike the Lieutenant's Open Guard."),
        OpenVisorMark(),
        Status(RisingStar, "Rising Star", StatusPolarity.Buff,
            "The first time each turn spending Authority leaves you with exactly 0, gain 1 Strength and 4 Block.",
            triggers:
            [
                On(TriggerEvent.RuleAnnounced, RA.If(
                    RA.And(RA.Announced(AuthorityPaid), RA.And(RA.Is(RA.Stacks(Her, Authority), 0), RA.Fresh(Her, Latch(RisingStar)))),
                    RA.Seq(RA.Spend(Her, Latch(RisingStar)), RA.Apply(Her, Strength, 1), RA.Block(Her, 4))), StatusTriggerScope.Anywhere),
            ]),
        WatchwordMark(),
        WatchSurgeonRite(),
    ];

    // Chain of Command: a second Order place (Obligations.IssueOrder asks for this status); and when two Orders
    // were kept in one round, her next turn's hand comes with this many more. Judged when that hand is dealt.
    private static StatusData ChainRite() => Status(ChainOfCommand, "Chain of Command", StatusPolarity.Buff,
        "You may hold two Orders. If you kept two Orders last turn, draw this many more cards at the start of this one.",
        triggers:
        [
            On(TriggerEvent.CardsDrawn, CD.Seq(
                CD.If(CD.AtLeast(CD.Minus(CD.Counter(CD.Self, Obligations.OrdersKept), CD.Counter(CD.Self, ChainBase)), 2),
                    new DrawCardsNode<CardsDrawnTriggeredEffectContext>(CD.Self, CD.Stacks(CD.Self, ChainOfCommand))),
                CD.Set(CD.Self, ChainBase, CD.Counter(CD.Self, Obligations.OrdersKept)))),
        ]);

    // Parade Ground: an Order kept after it was played, by the end of the turn, pays Ward Wax for the Energy left
    // (at most 2). A promise kept AT the end is announced after this turn-end program has looked, so the mark
    // waits for it.
    private static StatusData ParadeGroundMark()
    {
        var waiting = ParadeGround + "_waiting";
        var energy = ParadeGround + "_energy";
        IEffectNode<T> Pay<T>() where T : class => N<T>.Seq(
            N<T>.Apply(Her, Cards.Keywords.WardWax, new MultiplyExpression<T>(N<T>.Counter(Her, energy), N<T>.Stacks(Her, ParadeGround))),
            N<T>.Set(Her, waiting, 0),
            N<T>.Remove(Her, ParadeGround));
        return Status(ParadeGround, "Parade Ground", StatusPolarity.Buff,
            "If you keep an Order before this turn ends, gain this much Ward Wax for each Energy you end it with (at most 2, or 3 upgraded).",
            triggers:
            [
                On(TriggerEvent.TurnEnded, TE.Seq(
                    TE.Set(TE.Self, energy, TE.Min(TE.Counter(TE.Self, ParadeCap), TE.Energy(TE.Self))),
                    TE.If(TE.Above(TE.Counter(TE.Self, Obligations.OrdersKept), TE.Counter(TE.Self, ParadeBase)),
                        Pay<TurnEndedTriggeredEffectContext>(), TE.Set(TE.Self, waiting, 1)))),
                On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.OrderFulfilled), RA.Is(RA.Counter(Her, waiting), 1)),
                    Pay<RuleAnnouncedTriggeredEffectContext>()), StatusTriggerScope.Anywhere),
                On(TriggerEvent.TurnStarted, TS.Seq(TS.Set(TS.Self, waiting, 0), TS.Remove(TS.Self, ParadeGround))),
            ]);
    }

    // Drum Major: the first Order kept before her next turn begins brings 1 Energy — now, in her turn; held for
    // the next one if it came at the end of it or after.
    private static StatusData DrumMajorMark()
    {
        var after = DrumMajor + "_after";
        return Status(DrumMajor, "Drum Major", StatusPolarity.Buff,
            "The first Order you keep before your next turn gives 1 Energy — at once in your turn, otherwise held for the next.",
            triggers:
            [
                On(TriggerEvent.RuleAnnounced, RA.If(RA.Announced(Obligations.OrderFulfilled), RA.Seq(
                    RA.If(RA.Is(RA.Counter(Her, after), 1), RA.Apply(Her, HeldEnergy.Id, 1), RA.GainEnergy(Her, 1)),
                    // Two stacks (upgraded): a card with it.
                    RA.If(RA.AtLeast(RA.Stacks(Her, DrumMajor), 2), RA.Draw(Her, 1)),
                    RA.Remove(Her, DrumMajor))), StatusTriggerScope.Anywhere),
                On(TriggerEvent.TurnEnded, TE.Set(TE.Self, after, 1)),
                On(TriggerEvent.TurnStarted, TS.Seq(TS.Set(TS.Self, after, 0), TS.Remove(TS.Self, DrumMajor))),
            ]);
    }

    // Succession: once a round, a Challenge kept lays a new one on another enemy — armed at once, since no card
    // of hers is closing to arm it. Alone with the last one, she draws a card more next turn instead.
    private static StatusData SuccessionRite()
    {
        var next = Promises.Successor;
        var standing = CombatantTargetSelectors.WithoutStatus(CombatantTargetSelectors.AllEnemiesOfSource,
            new StatusDefinitionId(Obligations.LastWon));
        var heir = CombatantTargetSelectors.FirstTarget(standing);
        return Status(Succession, "Succession", StatusPolarity.Buff,
            "Once each turn, when you keep a Challenge, Challenge another enemy: hit it with a Deed before it acts " +
            "(1 Authority). If there is none, draw 1 more card next turn.",
            triggers:
            [
                On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.ChallengeFulfilled), RA.Fresh(Her, Latch(Succession))),
                    RA.Seq(
                        RA.Spend(Her, Latch(Succession)),
                        RA.If(RA.AtLeast(RA.Count(standing), 1),
                            RA.Seq(
                                RA.Set(heir, Obligations.Armed(next.Id), 1),
                                RA.Set(heir, Obligations.ArmedRoundOf(next.Id), RA.Round),
                                RA.Set(heir, Obligations.Progress(next.Id), 0),
                                RA.Set(heir, Obligations.Grace(next.Id), 0),
                                RA.Set(heir, Obligations.Remanded(next.Id), 0),
                                RA.Apply(heir, next.Id, 1)),
                            RA.Apply(Her, Obligations.Reinforcements, 1)))), StatusTriggerScope.Anywhere),
            ]);
    }

    // Riposte: once each enemy phase, a Challenged enemy that has struck her is struck back — her Strength in it
    // once, and no Deed (§2.7).
    private static StatusData RiposteRite()
    {
        var challenged = AnyChallengeOn<ActionResolvedTriggeredEffectContext>(AR.Self);
        var struck = AR.And(AR.Not(AR.Wears(AR.Self, Command)),
            AR.And(new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(), challenged));
        return Status(Riposte, "Riposte", StatusPolarity.Buff,
            "Once each enemy phase, when a Challenged enemy has attacked you, deal it this much damage plus your Strength.",
            triggers:
            [
                On(TriggerEvent.ActionResolved, AR.If(AR.And(struck, AR.Fresh(Her, Latch(Riposte))),
                    AR.Seq(AR.Spend(Her, Latch(Riposte)),
                        AR.Wound(AR.Self, AR.Plus(AR.Stacks(Her, Riposte), AR.Max(AR.C(0), AR.Stacks(Her, Strength)))))),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // Pike Square: through the next enemy phase the first hit of every enemy action is this much smaller, and the
    // enemy is struck back for what was turned aside, at most 12 (not a Deed; no Strength).
    private static StatusData PikeSquareMark()
    {
        var struck = AR.And(AR.Not(AR.Wears(AR.Self, Command)), new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>());
        return Status(PikeSquare, "Pike Square", StatusPolarity.Buff,
            "The first hit of each enemy action on you this enemy phase deals this much less; that enemy takes as much back, at most 12.",
            triggers:
            [
                On(TriggerEvent.ActionResolved, AR.If(struck,
                    AR.Wound(AR.Self, AR.Min(AR.C(12), AR.Stacks(Her, PikeSquare)))), StatusTriggerScope.Anywhere),
                On(TriggerEvent.TurnStarted, TS.Remove(TS.Self, PikeSquare)),
            ],
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.AddPerStack, -1,
                    Priority: 50, OncePerAction: true),
            ]);
    }

    // Armsmaster: each turn's start asks — 1 Strength, or the next large payment cheaper (§2.7).
    private static StatusData ArmsmasterRite() => Status(Armsmaster, "Armsmaster", StatusPolarity.Buff,
        "At the start of your turn, choose: gain 1 Strength, or your next payment of 2 or more Strength this turn costs 1 less.",
        triggers:
        [
            On(TriggerEvent.TurnStarted, TS.Choose(
                ["Gain 1 Strength", "Your next payment of 2+ Strength costs 1 less"],
                TS.Apply(TS.Self, Strength, 1), TS.Apply(TS.Self, ArmsmasterSaving, 1))),
        ]);

    // Blood Debt: armed when her turn ends; the first time in the enemy phase Open Guard costs her HP, that much
    // (at most the stacks) is owed, and paid as her next turn begins to the enemy that struck — or, gone, another.
    private const string GuardStriker = Obligations.GuardStriker;

    private static StatusData BloodDebtMark()
    {
        var armed = BloodDebt + "_armed";
        var owed = BloodDebt + "_owed";
        var enemies = CombatantTargetSelectors.AllEnemiesOfSource;
        var striker = CombatantTargetSelectors.FirstTarget(CombatantTargetSelectors.WithStatus(enemies, new StatusDefinitionId(GuardStriker)));
        var anyone = CombatantTargetSelectors.FirstTarget(enemies);
        return Status(BloodDebt, "Blood Debt", StatusPolarity.Buff,
            "The first time this enemy phase Open Guard costs you HP, the enemy that struck takes that much at the start of your turn (at most this much).",
            triggers:
            [
                On(TriggerEvent.TurnEnded, TE.Seq(TE.Set(TE.Self, armed, 1), TE.Set(TE.Self, owed, 0))),
                On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(GuardHurt), RA.And(RA.Is(RA.Counter(Her, armed), 1), RA.Is(RA.Counter(Her, owed), 0))),
                    RA.Set(Her, owed, RA.Min(RA.Stacks(Her, BloodDebt), RA.Counter(Her, GuardExtra)))), StatusTriggerScope.Anywhere),
                On(TriggerEvent.TurnStarted, TS.Seq(
                    TS.If(TS.And(TS.Is(TS.Counter(TS.Self, armed), 1), TS.AtLeast(TS.Counter(TS.Self, owed), 1)),
                        TS.If(TS.AtLeast(TS.Count(CombatantTargetSelectors.WithStatus(enemies, new StatusDefinitionId(GuardStriker))), 1),
                            TS.Wound(striker, TS.Counter(TS.Self, owed)),
                            TS.Wound(anyone, TS.Counter(TS.Self, owed)))),
                    TS.Set(TS.Self, armed, 0), TS.Set(TS.Self, owed, 0),
                    TS.Remove(TS.Self, BloodDebt))),
            ]);
    }

    // Open Visor: what Block soaks of her next Deed is given back to the enemy as Block and dealt to it past
    // Block — the blow as if the Block were not there, and the Block still standing (§2.7).
    private static StatusData OpenVisorMark() => Status(OpenVisor, "Open Visor", StatusPolarity.Buff,
        "Your next Deed this turn ignores Block.",
        triggers:
        [
            On(TriggerEvent.DamageTaken, D.If(
                D.And(D.Wears(D.Self, OpenVisor), D.And(D.CardTagged(Cards.CardAuthoring.DeedTag),
                    D.AtLeast(new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>(), 1))),
                D.All(
                    new ModifyDefensivePoolNode<DamageReceivedTriggeredEffectContext>(D.Event, StandardCombatIds.BlockDefensivePool,
                        new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>()),
                    new DealDamageNode<DamageReceivedTriggeredEffectContext>(D.Event,
                        new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>(), ignoresBlock: true,
                        kind: DamageKind.DamageOverTime))), StatusTriggerScope.Anywhere),
            .. UntilNextDeedResolves(OpenVisor),
        ]);

    // Watchword: an Order and a Challenge both kept after it, before her next turn — paid as that turn begins.
    // Judged as her next hand is dealt — the moment a card more can still join it.
    private static StatusData WatchwordMark() => Status(Watchword, "Watchword", StatusPolarity.Buff,
        "If you keep both an Order and a Challenge before your next turn, it begins with 1 Strength, this much Authority and a card more.",
        triggers:
        [
            On(TriggerEvent.CardsDrawn, CD.Seq(
                CD.If(CD.And(
                        CD.Above(CD.Counter(CD.Self, Obligations.OrdersKept), CD.Counter(CD.Self, WatchwordOrders)),
                        CD.Above(CD.Counter(CD.Self, Obligations.ChallengesWon), CD.Counter(CD.Self, WatchwordChallenges))),
                    CD.Seq(CD.Apply(CD.Self, Strength, 1), GainAuthority<CardsDrawnTriggeredEffectContext>(CD.Self, CD.Stacks(CD.Self, Watchword)),
                        CD.Draw(CD.Self, 1))),
                CD.Remove(CD.Self, Watchword))),
        ]);

    // Watch Surgeon: once a round, a kept promise is paid for with 1 Authority as healing — if she is hurt and
    // has it. (The canon's "you may"; nobody declines healing they can pay for, so it is not asked.)
    private static StatusData WatchSurgeonRite() => Status(WatchSurgeon, "Watch Surgeon", StatusPolarity.Buff,
        "The first time each turn you keep an Order or a Challenge, spend 1 Authority to heal this much, if you are hurt.",
        triggers:
        [
            On(TriggerEvent.RuleAnnounced, RA.If(
                RA.And(RA.Or(RA.Announced(Obligations.OrderFulfilled), RA.Announced(Obligations.ChallengeFulfilled)),
                    RA.And(RA.Fresh(Her, Latch(WatchSurgeon)), RA.And(RA.AtLeast(RA.Stacks(Her, Authority), 1),
                        RA.AtLeast(new CombatantMissingHealthExpression<RuleAnnouncedTriggeredEffectContext>(Her), 1)))),
                RA.Seq(RA.Spend(Her, Latch(WatchSurgeon)), RA.Adjust(Her, Authority, RA.C(-1)), RA.Apply(Her, AuthorityPulse, 1),
                    new HealNode<RuleAnnouncedTriggeredEffectContext>(Her, RA.Stacks(Her, WatchSurgeon)))), StatusTriggerScope.Anywhere),
        ]);
}
