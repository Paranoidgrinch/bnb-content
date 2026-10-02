using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// "At the start of (the next / each) combat …" building blocks. A combat opening is the engine's
// serializable one-shot rule: fx.installNextCombatOpening queues it, the entered fight consumes it at
// its first turn start (after the energy refill, before the draw).
public static class Openings
{
    public static InstallNextCombatOpeningRunEffect NextCombat(params CombatNodeModel[] nodes) =>
        ForFights(1, nodes);

    // HOW MANY FIGHTS AN EVENT'S "NEXT COMBAT" LASTS (user, playtest feedback 2, G1: "im naechsten fight wird der
    // naechste debuff ignoriert — das ist schon sehr schwach … auf die naechsten x kaempfe"). Boons and burdens alike.
    public const int EventFights = 3;

    // The same opening, for the next `fights` fights in a row.
    public static InstallNextCombatOpeningRunEffect ForFights(int fights, params CombatNodeModel[] nodes) =>
        new(new RelicCombatRule
        {
            Trigger = "turnStarted",
            Program = CombatProgramModel.Build<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>(
                nodes.Length == 1 ? nodes[0] : CombatNodeModel.Sequence(nodes)),
        }, fights > 1 ? fights : null);

    // A relic run program: fire the opening for EVERY combat (gated on entering combat nodes).
    public static RogueDeck.Run.ITriggeredRunEffectDefinition EveryCombat(params CombatNodeModel[] nodes) =>
        RunPrograms.When<NodeEnteredRunEvent>(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            NextCombat(nodes));
}
