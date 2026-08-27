using System.Text.Json;
using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// Every id in the shipped document has a name.
//
// This exists because of a bug that cost a whole run: a card wrote a combatant counter whose CounterId was
// null, and at the end of every fight that card was played in, the run died with an ArgumentNullException
// from a dictionary. The cause was C# static-field ORDER — a `static readonly CounterId` declared BELOW the
// card that used it is still `default` when the card's initializer runs, and `default` of an id struct is a
// null string. Nothing caught it because a null id serializes perfectly happily.
//
// So the test is written against the DOCUMENT rather than against any one author's discipline: whatever an
// id is called and wherever it sits, if it reaches the file without a name, this fails.
public class DocumentIdTests
{
    [Fact]
    public void No_id_in_the_shipped_document_is_nameless()
    {
        var blueprint = BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260827);
        var json = RunJson.ToJson(blueprint, RunJson.CreateOptions(indented: false));
        using var document = JsonDocument.Parse(json);

        var nameless = new List<string>();
        Scan(document.RootElement, "$", nameless);

        Assert.True(nameless.Count == 0,
            $"{nameless.Count} id(s) reached the document without a name: "
            + string.Join(", ", nameless.Take(10)));
    }

    // An id round-trips as a one-property wrapper ({"value": "…"}), so a wrapper whose only property is null
    // or empty is an id nobody named.
    private static void Scan(JsonElement element, string path, List<string> nameless)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (IsNamelessId(property.Value))
                        nameless.Add($"{path}/{property.Name}");
                    Scan(property.Value, $"{path}/{property.Name}", nameless);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    Scan(item, $"{path}[{index++}]", nameless);
                break;
        }
    }

    private static bool IsNamelessId(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
            return false;
        var wrapper = value.EnumerateObject().ToList();
        if (wrapper.Count != 1 || !string.Equals(wrapper[0].Name, "value", StringComparison.OrdinalIgnoreCase))
            return false;
        return wrapper[0].Value.ValueKind == JsonValueKind.Null
            || (wrapper[0].Value.ValueKind == JsonValueKind.String && wrapper[0].Value.GetString()?.Length == 0);
    }
}
