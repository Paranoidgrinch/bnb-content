using System.Text.Json;
using System.Text.Json.Serialization;

namespace BnbContent.Converter;

// Loads the original game's data directory for the ported slice: Act I, Bureaucrat only.
// Strict JSON: snake_case member names, unknown members abort — nothing is silently dropped.
public sealed class BabData
{
    public required BabClass Bureaucrat { get; init; }
    public required IReadOnlyList<BabCard> Cards { get; init; }
    public required IReadOnlyList<BabStatus> Statuses { get; init; }
    public required IReadOnlyList<BabEnemy> Enemies { get; init; }
    public required IReadOnlyList<BabEncounter> Encounters { get; init; }
    public required IReadOnlyList<BabEvent> Events { get; init; }
    public required IReadOnlyList<BabRelic> Relics { get; init; }
    public required BabActManifest Act { get; init; }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static BabData Load(string dataDirectory)
    {
        T One<T>(string relative) => Read<T>(Path.Combine(dataDirectory, relative));
        List<T> Many<T>(params string[] relatives) =>
            relatives.SelectMany(r => Read<List<T>>(Path.Combine(dataDirectory, r))).ToList();

        return new BabData
        {
            Act = One<BabActManifest>("acts/act_1_city.json"),
            Bureaucrat = One<BabClass>("classes/bureaucrat.json"),
            Cards = Many<BabCard>("cards/bureaucrat_starter.json", "cards/bureaucrat_rewards.json"),
            Statuses = Many<BabStatus>("statuses/statuses.json"),
            Enemies = Many<BabEnemy>("enemies/city_enemies.json"),
            Encounters = Many<BabEncounter>("encounters/act_1_city.json"),
            Events = Many<BabEvent>("events/act_1_city_events.json"),
            Relics = Many<BabRelic>("relics/act_1_relics.json", "relics/bureaucrat_relics.json"),
        };
    }

    private static T Read<T>(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, JsonOptions)
                ?? throw new ConversionException(path, "file deserialized to null");
        }
        catch (JsonException ex)
        {
            throw new ConversionException(path, $"strict JSON load failed: {ex.Message}");
        }
    }
}
