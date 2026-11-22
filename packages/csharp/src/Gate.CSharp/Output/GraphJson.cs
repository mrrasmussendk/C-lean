using System.Text.Json;
using Gate.CSharp.Model;

namespace Gate.CSharp.Output;

/// <summary>graph.json, per spec/schemas/graph.schema.json.</summary>
internal static class GraphJson
{
    public static string Render(CodeGraph graph) => Json.Render(writer => Document(writer, graph));

    private static void Document(Utf8JsonWriter writer, CodeGraph graph)
    {
        writer.WriteStartObject();
        writer.WriteString("spec", Sarif.SpecVersion);
        writer.WriteString("language", "csharp");
        writer.WriteStartArray("nodes");
        foreach (var node in graph.Nodes)
        {
            Node(writer, node);
        }
        writer.WriteEndArray();
        writer.WriteStartArray("edges");
        foreach (var edge in graph.Edges)
        {
            Edge(writer, edge);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Node(Utf8JsonWriter writer, GraphNode node)
    {
        writer.WriteStartObject();
        writer.WriteString("id", node.Id);
        writer.WriteString("kind", node.Kind);
        writer.WriteString("component", node.Component);
        writer.WriteString("file", node.File);
        writer.WriteNumber("line", node.Line);
        writer.WriteBoolean("test", node.Test);
        writer.WriteBoolean("public", node.Public);
        writer.WriteBoolean("effect", node.Effect);
        writer.WriteEndObject();
    }

    private static void Edge(Utf8JsonWriter writer, GraphEdge edge)
    {
        writer.WriteStartObject();
        writer.WriteString("from", edge.From);
        writer.WriteString("to", edge.To);
        writer.WriteString("kind", edge.Kind);
        writer.WriteEndObject();
    }
}
