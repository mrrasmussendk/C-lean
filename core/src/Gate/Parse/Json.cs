using System.Text.Json;

namespace Gate.Parse;

/// <summary>Reads values out of a JSON element. A missing property reads as empty, never as null.</summary>
internal static class Json
{
    /// <summary>Whether the text is one whole JSON object. A line cut off half way is not.</summary>
    public static bool IsObject(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string Text(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() ?? "" : "";

    public static int Number(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Number } value ? value.GetInt32() : 0;

    public static bool Flag(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.True };

    public static IReadOnlyList<JsonElement> Items(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Array } value ? value.EnumerateArray().ToList() : [];

    public static IReadOnlyList<string> Texts(JsonElement element, string name) =>
        Items(element, name).Select(item => item.GetString() ?? "").ToList();

    public static IReadOnlyList<JsonProperty> Members(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Object } value ? value.EnumerateObject().ToList() : [];

    public static JsonElement Object(JsonElement element, string name) =>
        Property(element, name) ?? default;

    private static JsonElement? Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;
}
