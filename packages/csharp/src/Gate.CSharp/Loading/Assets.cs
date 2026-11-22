using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Gate.CSharp.Loading;

/// <summary>Reads obj/project.assets.json: the compile-time assemblies of the restored packages.</summary>
internal static class Assets
{
    public static IReadOnlyList<string> CompileAssemblies(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var folder = Members(Member(root, "packageFolders")).Select(member => member.Name).FirstOrDefault("");
        var target = Members(Member(root, "targets")).Select(member => member.Value).FirstOrDefault();
        return Members(target)
            .Where(library => Text(Member(library.Value, "type")) == "package")
            .SelectMany(library => Members(Member(library.Value, "compile"))
                .Where(file => !file.Name.EndsWith("_._"))
                .Select(file => $"{folder}{Text(Member(Member(Member(root, "libraries"), library.Name), "path"))}/{file.Name}"))
            .ToList();
    }

    private static JsonElement Member(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var member) ? member : default;

    private static IEnumerable<JsonProperty> Members(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object ? element.EnumerateObject().ToList() : [];

    private static string Text(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
}
