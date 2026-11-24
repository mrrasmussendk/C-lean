using System.Text;
using System.Text.Json;
using Gate.Model;

namespace Gate.Parse;

/// <summary>The gate's log: one JSON object per line, one line per check.</summary>
internal static class LogText
{
    public static string Line(CheckRun run)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteString("at", run.At);
            json.WriteString("mode", run.Mode);
            json.WriteString("change", run.Change);
            json.WriteNumber("seconds", Math.Round(run.Seconds, Decimals));
            json.WriteNumber("exit", run.ExitCode);
            json.WriteStartObject("findings");
            foreach (var rule in run.Findings.OrderBy(pair => pair.Key))
            {
                json.WriteNumber(rule.Key, rule.Value);
            }

            json.WriteEndObject();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    /// <summary>Every well-formed line of the log. A line that was cut off by a killed process is skipped.</summary>
    public static IReadOnlyList<CheckRun> Runs(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(Json.IsObject)
            .Select(Run)
            .ToList();

    private const int Decimals = 2;

    private static CheckRun Run(string line)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        var seconds = root.TryGetProperty("seconds", out var value) ? value.GetDouble() : 0;
        return new CheckRun(Json.Text(root, "at"), Json.Text(root, "mode"), Json.Text(root, "change"), seconds)
        {
            ExitCode = Json.Number(root, "exit"),
            Findings = Json.Members(root, "findings").ToDictionary(member => member.Name, member => member.Value.GetInt32()),
        };
    }
}
