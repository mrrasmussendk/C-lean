// C2 Architecture, variation points: a component declared as varying is reached through its abstraction only.
using Gate.Model;

namespace Gate.Contracts;

internal static class Variation
{
    private const string Model = "architecture.yaml";
    private const string Abstraction = "abstraction";

    public static IEnumerable<Finding> Check(CheckInput input)
    {
        var components = input.Architecture.Components;
        var varying = components.Where(component => component.Varies).Select(component => component.Name).ToHashSet();
        var wiring = components.Where(component => component.Wires).Select(component => component.Name).ToHashSet();
        var graph = input.Revisions.Head.Graph;
        var nodes = graph.Nodes.Where(node => !node.Test).GroupBy(node => node.Id).ToDictionary(group => group.Key, group => group.First());
        var abstractions = nodes.Values.Where(node => node.Kind == Abstraction).ToList();
        var crossing = graph.Edges
            .Where(edge => nodes.ContainsKey(edge.From) && nodes.ContainsKey(edge.To))
            .Select(edge => (edge, from: nodes[edge.From], to: nodes[edge.To]))
            .Where(use => varying.Contains(use.to.Component) && use.from.Component != use.to.Component);
        var direct = crossing
            .Where(use => use.edge.Kind == "call" && !Offered(abstractions, use.to))
            .Select(use => Direct(use.from, use.to));
        var built = crossing
            .Where(use => use.edge.Kind == "construct" && !wiring.Contains(use.from.Component))
            .Select(use => Built(use.from, use.to));
        return [.. Missing(varying, abstractions), .. direct, .. built];
    }

    /// <summary>A call reaches the component properly when its target is an abstraction or a member of one.</summary>
    private static bool Offered(IReadOnlyList<Node> abstractions, Node target) =>
        abstractions.Any(offered => offered.Component == target.Component
            && (offered.Id == target.Id || target.Id.StartsWith(offered.Id + ".") || target.Id.StartsWith(offered.Id + ":")));

    private static IEnumerable<Finding> Missing(IReadOnlySet<string> varying, IReadOnlyList<Node> abstractions) =>
        varying.Where(component => abstractions.All(offered => offered.Component != component))
            .Select(component => Blocking.At("C2.variation", new Location(Model, 0), $"component '{component}' is declared `varies: true` and defines no abstraction. Define an interface, protocol or abstract class in it for what it offers, so a second implementation can replace the first."));

    private static Finding Direct(Node from, Node to) =>
        Blocking.At("C2.variation", new Location(from.File, from.Line), $"{from.Id} calls {to.Id} directly, and component '{to.Component}' is declared as varying. Call it through the abstraction '{to.Component}' defines, and take that abstraction as a parameter, so another implementation can be passed in.");

    private static Finding Built(Node from, Node to) =>
        Blocking.At("C2.variation", new Location(from.File, from.Line), $"{from.Id} constructs {to.Id} of the varying component '{to.Component}', and component '{from.Component}' is not marked `wires: true`. Construct implementations where the program is put together and pass them in.");
}
