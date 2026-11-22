using System;
using System.Linq;
using Gate.CSharp.Model;

namespace Gate.CSharp.Graph;

/// <summary>Assembles the dependency graph: nodes sorted by id, edges between nodes only, each once, no self
/// edges.</summary>
internal static class GraphBuilder
{
    public static CodeGraph Build(Solution solution)
    {
        var nodes = solution.Files.SelectMany(GraphNodes.Of)
            .GroupBy(node => node.Id).Select(same => same.First())
            .OrderBy(node => node.Id, StringComparer.Ordinal).ToList();
        var ids = nodes.Select(node => node.Id).ToHashSet();
        var edges = solution.Files.SelectMany(GraphEdges.Of)
            .Where(edge => edge.From != edge.To && ids.Contains(edge.From) && ids.Contains(edge.To))
            .Distinct()
            .OrderBy(edge => edge.From, StringComparer.Ordinal).ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind, StringComparer.Ordinal).ToList();
        return new CodeGraph(nodes, edges);
    }
}
