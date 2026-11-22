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

    /// <summary>mutants.json, as a native package's `mutants` command writes it.</summary>
    public static MutantSheet Sheet(string json)
    {
        if (json.Length == 0)
        {
            return new MutantSheet(false, []);
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new MutantSheet(Json.Flag(root, "switched"), Json.Items(root, "mutants").Select(Plan).ToList());
    }

    private static MutantPlan Plan(JsonElement mutant) =>
        new(Json.Text(mutant, "id"), Json.Text(mutant, "file"), Json.Text(mutant, "mutator"), Json.Text(mutant, "replacement"))
        {
            Line = Json.Number(mutant, "line"),
            Column = Json.Number(mutant, "column"),
            Start = Json.Number(mutant, "start"),
            End = Json.Number(mutant, "end"),
            Context = Json.Text(mutant, "context"),
            Status = Json.Text(mutant, "status"),
        };

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
            Context = Json.Text(mutant, "description"),
        };
    }
}
