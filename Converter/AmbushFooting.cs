using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.Statuses;
using D = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.DamageReceivedTriggeredEffectContext>;
using RE = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.RoundEndedTriggeredEffectContext>;

namespace BnbContent.Converter;

// NO AMBUSH OPENS WITH MORE THAN 20 DAMAGE (user, playtest 3, D1: "ambushes wo beide gegner im ersten zug angreifen
// sollten insgesamt nicht mehr als 20 dmg auf einen schlag verteilen, sonst kriegt man das einfach nicht geblockt").
// Every fight against two or more enemies (a boss excepted) opens with the player on 20 Ambush Footing: a ceiling on
// every blow that is the stacks still left, and every blow spends what it dealt, Block included. The first round
// can take 20 off the player in all and no more; at its end the footing is gone. Every enemy keeps its own opening
// move — the demand, the claim, the mechanic the room is about — and only the sum is held down.
// NEUTRAL, not a Buff: it is a rule of the room, and a room that refuses blessings (the Chisel) must not eat it.
public static class AmbushFooting
{
    public const string Id = "ambush_footing";
    public const string SpentId = "ambush_footing_spent";
    public const int Budget = 20;

    private static IEffectNode<RoundEndedTriggeredEffectContext> EveryoneLoses(string status) =>
        RE.Each(CombatantTargetSelectors.AllAliveCombatants, RE.Remove(RE.It, status));

    public static IReadOnlyList<StatusData> All() =>
    [
        Status(Id, "Ambush Footing", StatusPolarity.Neutral,
            $"This round the enemies can deal at most this much damage to you in total, Block included. Gone at the end of the round.",
            triggers:
            [
                // ⚠ The one who took the blow is the EVENT's target; the source here is whoever struck.
                On(TriggerEvent.DamageTaken, D.Seq(
                    D.Adjust(D.Event, Id, D.Minus(D.C(0), D.Plus(D.Amount, new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>()))),
                    D.If(D.Not(D.Wears(D.Event, Id)), D.Apply(D.Event, SpentId, 1)))),
                // …and a round's end is told from the side of whoever acted last, so the footing is taken off
                // whoever wears it.
                On(TriggerEvent.RoundEnded, EveryoneLoses(Id)),
            ],
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ClampMax, 0,
                    Priority: 1000, RestrictDamageKind: null, MagnitudeFromStacks: true),
            ]),
        // The footing spent to the last point: nothing more gets through this round.
        Status(SpentId, "Ambush Footing spent", StatusPolarity.Neutral,
            "The ambush has dealt all it may this round.",
            triggers: [On(TriggerEvent.RoundEnded, EveryoneLoses(SpentId))],
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ClampMax, 0,
                    Priority: 1000, RestrictDamageKind: null),
            ]),
    ];
}
