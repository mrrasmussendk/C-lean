using System.Text.Json;
using Gate.Model;

namespace Gate.Parse;

/// <summary>The Stryker mutation testing report schema. Test IDs are resolved to test names.</summary>
internal static class MutationText
{
    public static IReadOnlyList<Mutant> Mutants(string json)
    {
        if (json.Length == 0)
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        var names = TestNames(document.RootElement);
        return Json.Members(document.RootElement, "files")
            .SelectMany(file => Json.Items(file.Value, "mutants").Select(mutant => Mutant(file.Name, mutant, names)))
            .ToList();
    }

    public static IReadOnlyList<string> Tests(string json)
    {
        if (json.Length == 0)
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        return TestNames(document.RootElement).Values.Distinct().Order().ToList();
    }

    private static IReadOnlyDictionary<string, string> TestNames(JsonElement root) =>
        Json.Members(root, "testFiles")
            .SelectMany(file => Json.Items(file.Value, "tests"))
            .GroupBy(test => Json.Text(test, "id"))
            .ToDictionary(group => group.Key, group => Json.Text(group.First(), "name"));

    private static Mutant Mutant(string file, JsonElement mutant, IReadOnlyDictionary<string, string> names)
    {
        var line = Json.Number(Json.Object(Json.Object(mutant, "location"), "start"), "line");
        var killers = Json.Texts(mutant, "killedBy").Select(id => names.GetValueOrDefault(id, id)).ToList();
        return new Mutant(file.Replace('\\', '/'), line, Json.Text(mutant, "mutatorName"), Json.Text(mutant, "status"))
        {
            KilledBy = killers,
        };
    }
}
