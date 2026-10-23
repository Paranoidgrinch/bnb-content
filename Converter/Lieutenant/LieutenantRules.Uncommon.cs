using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.LieutenantKeywords;
using static BnbContent.Converter.Lieutenant.Statuses;
using AR = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.ActionResolvedTriggeredEffectContext>;
using CP = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.CardPlayedTriggeredEffectContext>;
using RA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.RuleAnnouncedTriggeredEffectContext>;
using SA = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.StatusAppliedTriggeredEffectContext>;
using SM = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.StatusMergedTriggeredEffectContext>;
using TE = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnEndedTriggeredEffectContext>;
using TS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// The uncommons' marks and Rites (§3.3).
public static partial class LieutenantRules
{
    public const string Recoil = "lt_recoil";
    public const string Poise = "lt_poise";
    public const string UnderTheColours = "lt_under_the_colours";
    public const string UnderTheColoursPlus = "lt_under_the_colours_plus";
    public const string Scar = "lt_scar";
    public const string Overreach = "lt_overreach";
    public const string OverreachCharge = "lt_overreach_charge";
    public const string BrokenNose = "lt_broken_nose";

    private static string Latch(string id) => id + "_round";

    private static IReadOnlyList<StatusData> UncommonRules() =>
    [
        RecoilMark(),
        PoiseMark(),
        OnceAKeptOrder(UnderTheColours, "Under the Colours", 0, "The first time each turn you keep an Order, gain 1 Strength."),
        OnceAKeptOrder(UnderTheColoursPlus, "Under the Colours+", 3,
            "The first time each turn you keep an Order, gain 1 Strength and 3 Block."),
        ScarRite(),
        OverreachRite(),
        Status(OverreachCharge, "Overreach", StatusPolarity.Buff,
            "Your next Deed this turn deals this much more damage with its first hit.",
            triggers: UntilNextDeedResolves(OverreachCharge),
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, 1,
                    RestrictSourceCardTag: Cards.CardAuthoring.DeedTag, OncePerAction: true),
            ]),
        BrokenNoseRite(),
    ];

    // Recoil: armed if her turn ends with Open Guard; the first enemy action of the phase that takes her HP pays
    // the draw, once. Judged when that action closes, against her health as it began.
    private static StatusData RecoilMark()
    {
        var armed = Recoil + "_armed";
        var hurt = AR.And(AR.Not(AR.Wears(AR.Self, Command)),
            AR.Above(AR.Counter(Her, HpBeforeAction), new CombatantCurrentHealthExpression<ActionResolvedTriggeredEffectContext>(Her)));
        return Status(Recoil, "Recoil", StatusPolarity.Buff,
            "If you go into the enemy phase with Open Guard and an enemy Attack takes your HP, draw 2 more cards next turn for each stack.",
            triggers:
            [
                On(TriggerEvent.TurnEnded, TE.If(TE.Wears(TE.Self, OpenGuard), TE.Set(TE.Self, armed, 1), TE.Remove(TE.Self, Recoil))),
                On(TriggerEvent.ActionResolved, AR.If(AR.And(hurt, AR.Is(AR.Counter(Her, armed), 1)),
                    AR.Seq(AR.Apply(Her, Obligations.Reinforcements, AR.Times(AR.Stacks(Her, Recoil), 2)),
                        AR.Set(Her, armed, 0), AR.Remove(Her, Recoil))), StatusTriggerScope.Anywhere),
                On(TriggerEvent.TurnStarted, TS.Seq(TS.Set(TS.Self, armed, 0), TS.Remove(TS.Self, Recoil))),
            ]);
    }

    // Poise: until her next Deed has resolved, Open Guard she gains is taken straight back — all of it if it is
    // new, what was added if it joined a standing Open Guard.
    private static StatusData PoiseMark() => Status(Poise, "Poise", StatusPolarity.Buff,
        "Until your next Deed this turn has resolved, you gain no Open Guard.",
        triggers:
        [
            On(TriggerEvent.StatusApplied, SA.If(SA.StatusIs(OpenGuard), SA.Remove(SA.Self, OpenGuard))),
            On(TriggerEvent.StatusMerged, SM.If(SM.StatusIs(OpenGuard), SM.Adjust(SM.Self, OpenGuard, SM.Minus(SM.C(0), SM.Amount)))),
            .. UntilNextDeedResolves(Poise),
        ]);

    private static StatusData OnceAKeptOrder(string id, string name, int block, string description) =>
        Status(id, name, StatusPolarity.Buff, description,
            triggers:
            [
                On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(Obligations.OrderFulfilled), RA.Fresh(Her, Latch(id))),
                    RA.Seq(
                    [
                        RA.Spend(Her, Latch(id)),
                        RA.Apply(Her, Strength, 1),
                        .. block > 0 ? [RA.Block(Her, block)] : Array.Empty<IEffectNode<RuleAnnouncedTriggeredEffectContext>>(),
                    ])), StatusTriggerScope.Anywhere),
            ]);

    // Scar: the first enemy action each round that takes her HP — read when it closes, against her health as it
    // began, so a blow of several hits is one blow.
    private static StatusData ScarRite()
    {
        var hurt = AR.And(AR.Not(AR.Wears(AR.Self, Command)),
            AR.Above(AR.Counter(Her, HpBeforeAction), new CombatantCurrentHealthExpression<ActionResolvedTriggeredEffectContext>(Her)));
        return Status(Scar, "Scar", StatusPolarity.Buff,
            "The first time each enemy phase an enemy Attack takes your HP, gain 2 Strength.",
            triggers:
            [
                On(TriggerEvent.ActionResolved, AR.If(AR.And(hurt, AR.Fresh(Her, Latch(Scar))),
                    AR.Seq(AR.Spend(Her, Latch(Scar)), AR.Apply(Her, Strength, 2))), StatusTriggerScope.Anywhere),
            ]);
    }

    // Overreach: the first Open Guard she gains each turn — new or added to — charges her next Deed's first hit.
    // The Rite's stacks are the bonus (6, or 9 upgraded).
    private static StatusData OverreachRite()
    {
        IEffectNode<T> Charge<T>() where T : class => N<T>.If(
            N<T>.And(N<T>.StatusIs(OpenGuard), N<T>.Fresh(Her, Latch(Overreach))),
            N<T>.Seq(N<T>.Spend(Her, Latch(Overreach)), N<T>.Apply(Her, OverreachCharge, N<T>.Stacks(Her, Overreach))));
        return Status(Overreach, "Overreach", StatusPolarity.Buff,
            "The first time each turn you gain Open Guard, your next Deed this turn deals this much more damage with its first hit.",
            triggers:
            [
                On(TriggerEvent.StatusApplied, Charge<StatusAppliedTriggeredEffectContext>()),
                On(TriggerEvent.StatusMerged, Charge<StatusMergedTriggeredEffectContext>()),
            ]);
    }

    // Broken Nose: the first time each enemy phase Open Guard costs her HP she would have kept (§2.5).
    private static StatusData BrokenNoseRite() => Status(BrokenNose, "Broken Nose", StatusPolarity.Buff,
        "The first time each enemy phase Open Guard costs you HP, gain 1 Authority and 2 Strength.",
        triggers:
        [
            On(TriggerEvent.RuleAnnounced, RA.If(RA.And(RA.Announced(GuardHurt), RA.Fresh(Her, Latch(BrokenNose))),
                RA.Seq(RA.Spend(Her, Latch(BrokenNose)), GainAuthority<RuleAnnouncedTriggeredEffectContext>(Her, 1),
                    RA.Apply(Her, Strength, 2))), StatusTriggerScope.Anywhere),
        ]);
}
