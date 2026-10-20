using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Apprentice;

// The statuses the Apprentice's cards and relics leave behind: rules that outlive the card that made them, and the
// marks his relics put on him at every fight's start, which the wand's own machinery reads (WandRules.Hooks).
public static partial class ApprenticeRules
{
    public static IReadOnlyDictionary<WandHook, StatusDefinitionId> Hooks => new Dictionary<WandHook, StatusDefinitionId>();

    public static IReadOnlyList<WandCastBoost> CastBoosts => [];

    public static IReadOnlyList<StatusData> All() => [];
}
