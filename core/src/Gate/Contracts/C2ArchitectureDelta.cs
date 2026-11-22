// C2 Architecture delta: the observed change in component edges must equal the declared one.
using Gate.Model;

namespace Gate.Contracts;

internal static class C2ArchitectureDelta
{
    private const string Model = "architecture.yaml";

    public static IReadOnlyList<Finding> Check(CheckInput input) =>
        [.. Unmapped(input), .. DeltaMismatch(input), .. NotAllowed(input), .. OutOfOrder(input)];

    public static IReadOnlySet<ComponentEdge> ComponentEdges(Graph graph)
    {
        var components = graph.Nodes.Where(node => !node.Test).GroupBy(node => node.Id).ToDictionary(group => group.Key, group => group.First().Component);
        return graph.Edges
            .Where(edge => components.ContainsKey(edge.From) && components.ContainsKey(edge.To))
            .Select(edge => new ComponentEdge(components[edge.From], components[edge.To]))
            .Where(edge => edge.From != edge.To && edge.From.Length > 0 && edge.To.Length > 0)
            .ToHashSet();
    }

    private static IEnumerable<Finding> Unmapped(CheckInput input) =>
        input.Revisions.Head.Graph.Nodes
            .Where(node => !node.Test && node.Kind != "function" && node.Component.Length == 0)
            .Select(node => Blocking.At("C2.unmapped", new Location(node.File, node.Line), $"type {node.Id} maps to no component. Add its path to a component in {Model}."));

    private static IEnumerable<Finding> DeltaMismatch(CheckInput input)
    {
        var before = ComponentEdges(input.Revisions.Base.Graph);
        var after = ComponentEdges(input.Revisions.Head.Graph);
        var added = Mismatch(after.Except(before), input.Records.SelectMany(record => record.Delta.Add), "adds");
        var removed = Mismatch(before.Except(after), input.Records.SelectMany(record => record.Delta.Remove), "removes");
        return added.Concat(removed);
    }

    private static IEnumerable<Finding> Mismatch(IEnumerable<ComponentEdge> observed, IEnumerable<ComponentEdge> declared, string verb)
    {
        var seen = observed.ToHashSet();
        var said = declared.ToHashSet();
        var undeclared = seen.Except(said).Select(edge => Blocking.At("C2.undeclared", new Location(Model, 0), $"the change {verb} edge {Show(edge)} and the change record does not declare it. Remove the dependency, or run `gate conflict`."));
        var unobserved = said.Except(seen).Select(edge => Blocking.At("C2.unobserved", new Location(Model, 0), $"the change record declares that the change {verb} edge {Show(edge)} and the code does not. The model may not drift ahead of the code."));
        return undeclared.Concat(unobserved);
    }

    private static IEnumerable<Finding> NotAllowed(CheckInput input) =>
        input.Records.SelectMany(record => record.Delta.Add)
            .Except(input.Architecture.Edges)
            .Select(edge => Blocking.At("C2.model", new Location(Model, 0), $"declared edge {Show(edge)} is not an allowed edge in {Model}. The commit that approves the record adds it to the model."));

    /// <summary>The commits that change the model and the record precede every implementation commit: every commit that touches an unprotected path.</summary>
    private static IEnumerable<Finding> OutOfOrder(CheckInput input)
    {
        var commits = input.Change.Commits;
        var guarded = input.Config.Protected;
        var firstImplementation = commits.TakeWhile(commit => commit.Paths.All(path => Glob.MatchesAny(guarded, path))).Count();
        return commits.Skip(firstImplementation)
            .Where(commit => commit.Paths.Any(IsDeclaration))
            .Select(commit => Blocking.At("C2.order", new Location(Model, 0), $"commit {commit.Id} changes the model or the change record after implementation started. Declare first, then implement."));
    }

    private static bool IsDeclaration(string path) =>
        path == Model || path.StartsWith(C1ChangeRecord.Directory);

    private static string Show(ComponentEdge edge) => $"{edge.From} -> {edge.To}";
}
