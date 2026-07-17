using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// "At the start of (the next / each) combat …" building blocks. A combat opening is the engine's
// serializable one-shot rule: fx.installNextCombatOpening queues it, the entered fight consumes it at
// its first turn start (after the energy refill, before the draw).
public static class Openings
{
    public static InstallNextCombatOpeningRunEffect NextCombat(params CombatNodeModel[] nodes) =>
        new(new RelicCombatRule
        {
            Trigger = "turnStarted",
            Program = CombatProgramModel.Build<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>(
                nodes.Length == 1 ? nodes[0] : CombatNodeModel.Sequence(nodes)),
        });

    // A relic run program: fire the opening for EVERY combat (gated on entering combat nodes).
    public static RogueDeck.Run.ITriggeredRunEffectDefinition EveryCombat(params CombatNodeModel[] nodes) =>
        RunPrograms.When<NodeEnteredRunEvent>(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            NextCombat(nodes));
}
