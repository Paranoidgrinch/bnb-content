using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.ApprenticeKeywords;

namespace BnbContent.Converter.Apprentice;

// The statuses the Apprentice's cards and relics leave behind: rules that outlive the card that made them, and the
// marks his relics put on him at every fight's start, which the wand's own machinery reads (WandRules.Hooks).
public static partial class ApprenticeRules
{
    // Chalk Circle: "if you do not cast this turn, gain 1 Grit at the end of it" — one Grit per circle drawn.
    public const string QuietCircle = "chalk_circle_quiet";

    public static IReadOnlyDictionary<WandHook, StatusDefinitionId> Hooks => new Dictionary<WandHook, StatusDefinitionId>();

    public static IReadOnlyList<WandCastBoost> CastBoosts => [];

    public static IReadOnlyList<StatusData> All() => [QuietCircleStatus()];

    private static StatusData QuietCircleStatus()
    {
        var self = CombatantTargetSelectors.Source;
        var circles = new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(QuietCircle));
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new WandValueExpression<TurnEndedTriggeredEffectContext>(WandField.CastsThisTurn),
                    ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(Grit), circles)),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(QuietCircle)),
        ]));
        return Status(QuietCircle, "Chalk Circle", StatusPolarity.Neutral,
            "If you do not cast this turn, gain 1 Grit for each at the end of it.",
            [Trigger(program, nameof(TriggerEvent.TurnEnded))]);
    }
}
