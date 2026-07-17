using System.Text.Json;
using System.Text.Json.Serialization;

namespace BnbContent.Converter;

// DTOs mirroring the original game's data files (snake_case JSON). Loading is strict: unknown JSON
// members abort the conversion (JsonUnmappedMemberHandling.Disallow via BabLoader), so a source-data
// construct the converter doesn't know cannot silently vanish from the port.

public sealed record BabClass(
    string Id,
    string Name,
    int MaxHp,
    int StartingEnergy,
    string? StartingRelic,
    IReadOnlyList<string> StartingDeck,
    IReadOnlyDictionary<string, int>? StartingResources);

public sealed record BabCard(
    string Id,
    string Name,
    [property: JsonPropertyName("class")] string? CardClass,
    string Type,
    int Cost,
    string? Rarity,
    string? Text,
    IReadOnlyList<BabEffect>? Effects,
    IReadOnlyList<string>? Tags,
    string? UpgradesTo);

public sealed record BabEffect(
    string Type,
    string? Target,
    int? Amount,
    string? Status,
    int? AmountPerStack,
    string? CardId,
    string? Destination,
    int? Copies,
    string? Resource,
    string? Tag);

public sealed record BabStatus(
    string Id,
    string Name,
    string Description,
    string Stacking,
    string Trigger,
    bool IsNegative,
    IReadOnlyList<string>? Tags);

public sealed record BabEnemy(
    string Id,
    string Name,
    int MaxHp,
    string? IntentPattern,
    IReadOnlyList<BabIntent> Intents,
    IReadOnlyList<string>? Tags);

// An intent carries exactly ONE payload shape in the source data: a bare attack (damage), a legacy
// effect list (effects), or the dominant action list (actions) — the latter two share the effect DSL.
public sealed record BabIntent(
    string Id,
    string Name,
    string IntentType,
    int? Damage,
    int? Block,
    IReadOnlyList<BabEffect>? Effects,
    IReadOnlyList<BabEffect>? Actions,
    double? Weight);

public sealed record BabEncounter(
    string Id,
    string Name,
    int Act,
    string Difficulty,
    IReadOnlyList<string> Enemies,
    double? Weight,
    IReadOnlyList<string>? Tags);

public sealed record BabEvent(
    string Id,
    string Name,
    int Act,
    string EventType,
    double? Weight,
    string Text,
    IReadOnlyList<BabEventChoice> Choices,
    IReadOnlyList<string>? Tags);

public sealed record BabEventChoice(
    string Id,
    string Text,
    string? ResultText,
    IReadOnlyList<BabEffect>? Effects,
    string? EncounterId);

public sealed record BabRelic(
    string Id,
    string Name,
    string? Rarity,
    string? Description,
    [property: JsonPropertyName("class")] string? RelicClass,
    IReadOnlyList<string>? AllowedClasses,
    IReadOnlyList<BabRelicEffect>? Effects,
    IReadOnlyList<string>? Tags);

public sealed record BabRelicEffect(
    string Type,
    int? Amount,
    string? Status,
    string? CardId,
    string? Destination,
    int? Copies,
    double? Factor);

public sealed record BabActManifest(
    string Id,
    int Act,
    string Name,
    IReadOnlyList<string> CharacterClassFiles,
    string DefaultCharacterClassId,
    IReadOnlyList<string> CardFiles,
    IReadOnlyList<string> EnemyFiles,
    IReadOnlyList<string> EncounterFiles,
    IReadOnlyList<string> StatusFiles,
    IReadOnlyList<string> EventFiles,
    IReadOnlyList<string> RelicFiles,
    BabMapSettings Map,
    BabTreasureSettings? Treasure,
    BabWaitingRoomSettings? WaitingRoom);

public sealed record BabMapSettings(
    int StepsBeforeBoss,
    int Width,
    string Layout,
    int MaxEvents,
    int MaxTreasures,
    int MaxElites,
    double EventCombatChance);

public sealed record BabTreasureSettings(double MimicChance, string? MimicEncounterId);

public sealed record BabWaitingRoomSettings(int HealPercent);

// A conversion problem with its source location — the converter's fail-loud currency.
public sealed class ConversionException : Exception
{
    public ConversionException(string where, string what)
        : base($"{where}: {what}") { }
}
