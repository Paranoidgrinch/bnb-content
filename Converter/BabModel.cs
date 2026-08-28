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
    string? Tag,
    // Reworked extensions: an optional cap on a per-stack scaling bonus (damage_per_status), and a counter id
    // + relative flag for set_counter (tracks like Queue Position). Optional → old effects are unchanged.
    int? Cap = null,
    string? Counter = null,
    bool? Relative = null,
    // damage_per_status only: whose status the scaling reads (a target key like "owner"/"player"; default: the
    // effect's own target), and how many stacks make one bonus step ("+2 for every 2 Paperwork" → 2).
    string? StatusOn = null,
    int? PerStacks = null);

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
    IReadOnlyList<string>? Tags,
    // Reworked-content extensions (absent in the original demo data): passive statuses the enemy starts a
    // fight with (its signature "rule", authored as a status with triggers), and state-conditional intent
    // rules (one-shot overrides / phase / orbit selection). Both optional → existing enemies are unchanged.
    IReadOnlyList<BabEnemyStatus>? StartingStatuses = null,
    IReadOnlyList<BabIntentRule>? IntentRules = null);

// A status the enemy carries from the start of the fight (a passive rule, or a standing buff).
public sealed record BabEnemyStatus(string Status, int? Stacks);

// A state-conditional intent: when Condition matches (highest Priority first), the enemy uses the intent
// named by Action instead of its plain cycle. Action is an intent id on the same enemy.
public sealed record BabIntentRule(BabIntentCondition Condition, string Action, int? Priority);

// A serializable predicate over live combat state, mapped to the engine's EnemyIntentCondition family.
// Kind picks the shape; only the fields that kind needs are read.
public sealed record BabIntentCondition(
    string Kind,
    string? Counter,
    string? Status,
    string? Resource,
    string? Op,
    int? Value,
    int? Percent,
    int? MinStacks,
    bool? LastTurn,
    IReadOnlyList<BabIntentCondition>? Conditions);

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
    double? Weight,
    // A SPECIAL intent is defined (so intent_rules can name it) but kept OUT of the round-robin cycle — it only
    // fires when an intent rule selects it (e.g. "Everyone Moves at Once" at Queue Position 3). Default false.
    bool? Special = null);

public sealed record BabEncounter(
    string Id,
    string Name,
    int Act,
    string Difficulty,
    IReadOnlyList<string> Enemies,
    double? Weight,
    IReadOnlyList<string>? Tags,
    // Per-ROSTER health, positionally parallel to Enemies: multi-enemy encounters field their bodies at
    // reduced HP ("Duo HP Scaling"), and the same identity appears at different HP in different encounters.
    // A null entry (or the whole list absent) keeps the enemy's own max_hp.
    IReadOnlyList<int?>? EnemyHealth = null,
    // Which map ROLE draws this template: combat / multi_combat / elite / boss / mimic. Only encounters that
    // carry a role are part of the act's curated pools — anything else is inert content the map never picks.
    string? Role = null,
    // Statuses one BODY carries in THIS fight and not in its others — encounter scaffolding rather than a
    // universal passive. Act III's design asks for it by name: the Boundary Stone begins its two teaching
    // encounters holding a Claim so that Claim transfer is actually demonstrated inside a standard fight,
    // "later appearances receive no free Claim". Indexed into Enemies, so the same identity can be scaffolded
    // in one encounter and bare in the next.
    IReadOnlyList<BabEncounterEnemyStatus>? EnemyStatuses = null);

// A status served on ONE of an encounter's bodies at the first bell. `Index` is the position in the
// encounter's own Enemies list, not an enemy id, because an encounter may field the same identity twice.
public sealed record BabEncounterEnemyStatus(int Index, string Status, int? Stacks);

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

// Only the length and the width are read (the rest were the ORIGINAL generator's knobs, and the acts authored
// since do not repeat them); everything after Width is optional so a later act's manifest can leave it out.
public sealed record BabMapSettings(
    int StepsBeforeBoss,
    int Width,
    string? Layout = null,
    int MaxEvents = 0,
    int MaxTreasures = 0,
    int MaxElites = 0,
    double EventCombatChance = 0,
    // …and Act III's manifest carries two more of the original's knobs. Both are answered by this port's own
    // map rules (ActRules.EarliestDepthPercent and the lane weights), so they are read and ignored rather
    // than left to abort a strict load.
    int FirstEliteDepth = 0,
    double EliteWeightMultiplier = 0);

public sealed record BabTreasureSettings(double MimicChance, string? MimicEncounterId);

public sealed record BabWaitingRoomSettings(int HealPercent);

// A conversion problem with its source location — the converter's fail-loud currency.
public sealed class ConversionException : Exception
{
    public ConversionException(string where, string what)
        : base($"{where}: {what}") { }
}
