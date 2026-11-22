// C3 Trace check, collection: the component edges an instrumented test run observed, as OpenTelemetry spans.
using System.Text;
using System.Text.Json;
using Gate.Model;

namespace Gate.Contracts;

internal static class Traces
{
    /// <summary>One parent span and one child span per edge, each carrying `gate.component` (OTLP JSON file export).</summary>
    public static string Otlp(IReadOnlyList<ComponentEdge> edges)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteStartArray("resourceSpans");
            json.WriteStartObject();
            json.WriteStartArray("scopeSpans");
            json.WriteStartObject();
            json.WriteStartArray("spans");
            foreach (var (edge, index) in edges.Select((edge, index) => (edge, index)))
            {
                WriteSpan(json, ($"p{index}", ""), edge.From);
                WriteSpan(json, ($"c{index}", $"p{index}"), edge.To);
            }

            json.WriteEndArray();
            json.WriteEndObject();
            json.WriteEndArray();
            json.WriteEndObject();
            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteSpan(Utf8JsonWriter json, (string Id, string Parent) span, string component)
    {
        json.WriteStartObject();
        json.WriteString("spanId", span.Id);
        json.WriteString("parentSpanId", span.Parent);
        json.WriteString("name", component);
        json.WriteStartArray("attributes");
        json.WriteStartObject();
        json.WriteString("key", "gate.component");
        json.WriteStartObject("value");
        json.WriteString("stringValue", component);
        json.WriteEndObject();
        json.WriteEndObject();
        json.WriteEndArray();
        json.WriteEndObject();
    }
}
