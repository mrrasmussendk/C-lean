using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gate.CSharp.Model;

namespace Gate.CSharp.Output;

/// <summary>findings.sarif: SARIF 2.1.0, one run, one result per finding.</summary>
internal static class Sarif
{
    public const string PackageVersion = "0.1.0";
    public const string SpecVersion = "1.0";
    private const string Schema = "https://json.schemastore.org/sarif-2.1.0.json";

    /// <summary>The severity of a rule a human switched off in gate.yaml: its findings are not written.</summary>
    private const string Off = "off";

    private static readonly IReadOnlyDictionary<string, string> Levels = new Dictionary<string, string>
    {
        ["block"] = "error",
        ["warn"] = "warning",
        ["report"] = "note",
    };

    public static string Render(IReadOnlyList<Finding> findings, Settings settings) =>
        Json.Render(writer => Document(writer, findings, settings));

    private static void Document(Utf8JsonWriter writer, IReadOnlyList<Finding> findings, Settings settings)
    {
        writer.WriteStartObject();
        writer.WriteString("$schema", Schema);
        writer.WriteString("version", "2.1.0");
        writer.WriteStartArray("runs");
        writer.WriteStartObject();
        writer.WriteStartObject("tool");
        writer.WriteStartObject("driver");
        writer.WriteString("name", "gate-csharp");
        writer.WriteString("version", PackageVersion);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteStartArray("results");
        foreach (var finding in findings.Where(finding => settings.Severities[finding.Rule] != Off))
        {
            Result(writer, finding, Levels[settings.Severities[finding.Rule]]);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Result(Utf8JsonWriter writer, Finding finding, string level)
    {
        writer.WriteStartObject();
        writer.WriteString("ruleId", finding.Rule);
        writer.WriteString("level", level);
        writer.WriteStartObject("message");
        writer.WriteString("text", finding.Message);
        writer.WriteEndObject();
        writer.WriteStartArray("locations");
        Location(writer, finding);
        writer.WriteEndArray();
        writer.WriteStartObject("partialFingerprints");
        writer.WriteString("gate/v1", finding.Key);
        writer.WriteEndObject();
        Suppression(writer, finding);
        writer.WriteEndObject();
    }

    private static void Location(Utf8JsonWriter writer, Finding finding)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("physicalLocation");
        writer.WriteStartObject("artifactLocation");
        writer.WriteString("uri", finding.File);
        writer.WriteEndObject();
        writer.WriteStartObject("region");
        writer.WriteNumber("startLine", finding.Line);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void Suppression(Utf8JsonWriter writer, Finding finding)
    {
        if (!finding.Suppressed)
        {
            return;
        }
        writer.WriteStartArray("suppressions");
        writer.WriteStartObject();
        writer.WriteString("kind", "inSource");
        writer.WriteEndObject();
        writer.WriteEndArray();
    }
}
