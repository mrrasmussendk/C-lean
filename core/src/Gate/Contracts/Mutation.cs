// C4 Test admission, the mutation run: apply one mutant, read which tests fail, write the kill matrix.
using System.Text;
using System.Text.Json;
using Gate.Model;

namespace Gate.Contracts;

internal static class Mutation
{
    public const string Killed = "Killed";
    public const string Survived = "Survived";
    public const string Timeout = "Timeout";
    public const string CompileError = "CompileError";

    private const int HighThreshold = 80;
    private const int LowThreshold = 60;

    /// <summary>The file's bytes with the mutant's byte range replaced.</summary>
    public static byte[] Apply(byte[] source, MutantPlan plan) =>
        [.. source.Take(plan.Start), .. Encoding.UTF8.GetBytes(plan.Replacement), .. source.Skip(plan.End)];

    /// <summary>A mutant is killed when a test fails for it. A run that reports no test at all did not compile.</summary>
    public static string Status(IReadOnlyList<TestRun> tests) =>
        tests.Count == 0 ? CompileError
        : tests.Any(test => test.Failed) ? Killed
        : Survived;

    /// <summary>The kill matrix in the Stryker mutation testing report schema. Test names are the test IDs.</summary>
    public static string Report(IReadOnlyList<MutantResult> results, IReadOnlyList<string> tests)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("schemaVersion", "1");
            json.WriteStartObject("thresholds");
            json.WriteNumber("high", HighThreshold);
            json.WriteNumber("low", LowThreshold);
            json.WriteEndObject();
            WriteFiles(json, results);
            WriteTests(json, tests);
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteFiles(Utf8JsonWriter json, IReadOnlyList<MutantResult> results)
    {
        json.WriteStartObject("files");
        foreach (var file in results.GroupBy(result => result.Plan.File))
        {
            json.WriteStartObject(file.Key);
            json.WriteString("language", Path.GetExtension(file.Key).TrimStart('.'));
            json.WriteString("source", "");
            json.WriteStartArray("mutants");
            foreach (var result in file)
            {
                WriteMutant(json, result);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void WriteMutant(Utf8JsonWriter json, MutantResult result)
    {
        var plan = result.Plan;
        json.WriteStartObject();
        json.WriteString("id", plan.Id);
        json.WriteString("mutatorName", plan.Mutator);
        json.WriteString("replacement", plan.Replacement);
        json.WriteString("description", plan.Context);
        json.WriteString("status", result.Status);
        json.WriteStartObject("location");
        WritePosition(json, "start", plan);
        WritePosition(json, "end", plan);
        json.WriteEndObject();
        json.WriteStartArray("killedBy");
        foreach (var test in result.KilledBy)
        {
            json.WriteStringValue(test);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WritePosition(Utf8JsonWriter json, string name, MutantPlan plan)
    {
        json.WriteStartObject(name);
        json.WriteNumber("line", plan.Line);
        json.WriteNumber("column", plan.Column);
        json.WriteEndObject();
    }

    private static void WriteTests(Utf8JsonWriter json, IReadOnlyList<string> tests)
    {
        json.WriteStartObject("testFiles");
        json.WriteStartObject("tests");
        json.WriteStartArray("tests");
        foreach (var test in tests)
        {
            json.WriteStartObject();
            json.WriteString("id", test);
            json.WriteString("name", test);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
        json.WriteEndObject();
    }
}
