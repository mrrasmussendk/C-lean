// C4 Test admission: property-based tests are required for components the model marks as core logic.
using Gate.Model;

namespace Gate.Contracts;

internal static class PropertyTests
{
    private const string Model = "architecture.yaml";

    public static IEnumerable<Finding> Missing(CheckInput input)
    {
        var graph = input.Revisions.Head.Graph;
        var reached = Reached(graph);
        var present = graph.Nodes.Where(node => !node.Test).Select(node => node.Component).ToHashSet();
        return input.Architecture.Components
            .Where(component => component.CoreLogic && present.Contains(component.Name) && !reached.Contains(component.Name))
            .Select(component => Blocking.At("C4.property", new Location(Model, 0), $"component '{component.Name}' is marked `core_logic: true` and no property-based test reaches any of its code. Mutation testing does not catch code that special-cases the tested inputs; add a property-based test that calls into {string.Join(", ", component.Paths)}."));
    }

    /// <summary>Components with code that a property-based test reaches through calls and constructions.</summary>
    private static IReadOnlySet<string> Reached(Graph graph)
    {
        var uses = graph.Edges.Where(edge => edge.Kind is "call" or "construct").ToLookup(edge => edge.From, edge => edge.To);
        var frontier = graph.Nodes.Where(node => node.Test && node.Property).Select(node => node.Id).ToList();
        var seen = frontier.ToHashSet();
        while (frontier.Count > 0)
        {
            frontier = frontier.SelectMany(id => uses[id]).Where(seen.Add).ToList();
        }

        return graph.Nodes.Where(node => seen.Contains(node.Id) && !node.Test).Select(node => node.Component).ToHashSet();
    }
}
