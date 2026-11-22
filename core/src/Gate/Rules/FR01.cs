// FR01: An abstraction with exactly one production implementation and exactly one production consumer must sit on a boundary declared in the architecture model
using Gate.Model;

namespace Gate.Rules;

internal static class FR01
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var graph = input.Revisions.Head.Graph;
        var production = graph.Nodes.Where(node => !node.Test).ToDictionary(node => node.Id);
        var boundaries = input.Architecture.Components.Where(component => component.Boundary || component.Varies).Select(component => component.Name).ToHashSet();
        var severity = Settings.Severity(input.Config, "FR01");
        return production.Values
            .Where(node => node.Kind == "abstraction" && !boundaries.Contains(node.Component))
            .Where(node => Implementations(graph, production, node).Count == 1 && Consumers(graph, production, node).Count == 1)
            .Select(node => Finding(node, severity))
            .ToList();
    }

    private static IReadOnlySet<string> Implementations(Graph graph, IReadOnlyDictionary<string, Node> production, Node abstraction) =>
        graph.Edges
            .Where(edge => edge.To == abstraction.Id && edge.Kind == "inherit" && production.ContainsKey(edge.From))
            .Select(edge => edge.From)
            .ToHashSet();

    /// <summary>A consumer is a production file, other than the abstraction's implementations, that refers to it.</summary>
    private static IReadOnlySet<string> Consumers(Graph graph, IReadOnlyDictionary<string, Node> production, Node abstraction)
    {
        var implementations = Implementations(graph, production, abstraction);
        var implementing = implementations.Select(id => production[id].File).Append(abstraction.File).ToHashSet();
        return graph.Edges
            .Where(edge => edge.To == abstraction.Id && edge.Kind != "inherit" && production.ContainsKey(edge.From))
            .Select(edge => production[edge.From].File)
            .Where(file => !implementing.Contains(file))
            .ToHashSet();
    }

    private static Finding Finding(Node abstraction, Severity severity)
    {
        var message = $"FR01: abstraction {abstraction.Id} has 1 implementation and 1 consumer and component '{abstraction.Component}' is not a declared boundary. Use the implementation directly, or declare the boundary in architecture.yaml.";
        return new Finding("FR01", severity, new Location(abstraction.File, abstraction.Line), message)
        {
            Fingerprint = $"FR01|{abstraction.File}|{abstraction.Id}",
        };
    }
}
