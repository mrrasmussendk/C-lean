using System.Text.Json;
using Gate.Model;

namespace Gate.Parse;

/// <summary>The trace file an instrumented run appends to: one JSON object per line, `{"from": ..., "to": ...}`.</summary>
internal static class TraceText
{
    /// <summary>The distinct component edges. A line that is not such an object is skipped.</summary>
    public static IReadOnlyList<ComponentEdge> Edges(string lines) =>
        lines.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith('{') && line.EndsWith('}'))
            .Select(Edge)
            .Where(edge => edge.From.Length > 0 && edge.To.Length > 0)
            .Distinct()
            .ToList();

    private static ComponentEdge Edge(string line)
    {
        using var document = JsonDocument.Parse(line);
        return new ComponentEdge(Json.Text(document.RootElement, "from"), Json.Text(document.RootElement, "to"));
    }
}
