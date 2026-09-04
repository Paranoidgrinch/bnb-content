using System.Text.Json;
using System.Text.Json.Serialization;

namespace BnbContent.Converter;

// Loads the original game's data directory for the ported slice: Acts I–IV, Bureaucrat only.
// Strict JSON: snake_case member names, unknown members abort — nothing is silently dropped.
public sealed class BabData
{
    public required BabClass Bureaucrat { get; init; }
    public required IReadOnlyList<BabCard> Cards { get; init; }
    public required IReadOnlyList<BabStatus> Statuses { get; init; }
    public required IReadOnlyList<BabEnemy> Enemies { get; init; }
    public required IReadOnlyList<BabEncounter> Encounters { get; init; }
    public required IReadOnlyList<BabRelic> Relics { get; init; }
    // The acts the run walks, in order. Each manifest owns its own map settings, its treasure and its waiting
    // room; which ENCOUNTERS and EVENTS belong to it is decided by the act number the entries carry, not by the
    // file they came from (MapSpecBuilder filters on it).
    public required IReadOnlyList<BabActManifest> Acts { get; init; }

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
            Acts =
            [
                One<BabActManifest>("acts/act_1_city.json"),
                One<BabActManifest>("acts/act_2_archives.json"),
                One<BabActManifest>("acts/act_3_green_docket.json"),
                One<BabActManifest>("acts/act_4_licensing_labyrinth.json"),
            ],
            Bureaucrat = One<BabClass>("classes/bureaucrat.json"),
            Cards = Many<BabCard>("cards/bureaucrat_starter.json", "cards/bureaucrat_rewards.json"),
            Statuses = Many<BabStatus>("statuses/statuses.json"),
            // Act I's ported roster plus the acts authored since. Every act's bodies are ordinary enemy data;
            // what makes an act an act is its own map and its own vocabulary, not a separate catalogue. An act
            // joins the WALKED run (the Acts list above) once it has a roster, doors and bosses to end on;
            // Act IV did at IV-24, and Act V is the one still missing.
            Enemies = Many<BabEnemy>(
                "enemies/city_enemies.json", "enemies/act_2_archives_enemies.json",
                "enemies/act_3_green_docket_enemies.json",
                "enemies/act_4_licensing_labyrinth_enemies.json"),
            Encounters = Many<BabEncounter>(
                "encounters/act_1_city.json", "encounters/act_2_archives.json",
                "encounters/act_3_green_docket.json",
                "encounters/act_4_licensing_labyrinth.json"),
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
