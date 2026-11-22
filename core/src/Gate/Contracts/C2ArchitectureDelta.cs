// C2 Architecture delta: the observed change in component edges must equal the declared one.
using Gate.Model;

namespace Gate.Contracts;

internal static class C2ArchitectureDelta
{
    private const string Model = "architecture.yaml";
    private const int ShownCauses = 3;

    public static IReadOnlyList<Finding> Check(CheckInput input) =>
        [.. Unmapped(input), .. DeltaMismatch(input), .. NotAllowed(input), .. OutOfOrder(input), .. Variation.Check(input)];

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
            .Select(node => Blocking.At("C2.unmapped", new Location(node.File, node.Line), $"type {node.Id} is in {node.File}, which matches the `paths` of no component in {Model}. Add a glob for it to the component it belongs to."));

    private static IEnumerable<Finding> DeltaMismatch(CheckInput input)
    {
        var revisions = input.Revisions;
        var declared = new Delta(input.Records.SelectMany(record => record.Delta.Add).ToList(), input.Records.SelectMany(record => record.Delta.Remove).ToList());
        var record = input.Records.Select(change => change.Path).FirstOrDefault(C1ChangeRecord.Directory);
        var added = ComponentEdges(revisions.Head.Graph).Except(ComponentEdges(revisions.Base.Graph)).ToHashSet();
        var removed = ComponentEdges(revisions.Base.Graph).Except(ComponentEdges(revisions.Head.Graph)).ToHashSet();
        return
        [
            .. added.Except(declared.Add).Select(edge => Undeclared(edge, Causes(revisions.Head.Graph, edge), "adds")),
            .. removed.Except(declared.Remove).Select(edge => Undeclared(edge, Causes(revisions.Base.Graph, edge), "removes")),
            .. declared.Add.Except(added).Select(edge => Unobserved(record, edge, "add")),
            .. declared.Remove.Except(removed).Select(edge => Unobserved(record, edge, "remove")),
        ];
    }

    /// <summary>The code-level edges behind one component edge, as (node it starts at, text naming both ends).</summary>
    private static IReadOnlyList<(Node From, string Text)> Causes(Graph graph, ComponentEdge edge)
    {
        var nodes = graph.Nodes.Where(node => !node.Test).GroupBy(node => node.Id).ToDictionary(group => group.Key, group => group.First());
        return graph.Edges
            .Where(code => nodes.ContainsKey(code.From) && nodes.ContainsKey(code.To))
            .Where(code => nodes[code.From].Component == edge.From && nodes[code.To].Component == edge.To)
            .Select(code => (nodes[code.From], $"{code.From} ({nodes[code.From].File}:{nodes[code.From].Line}) --{code.Kind}--> {code.To}"))
            .ToList();
    }

    private static Finding Undeclared(ComponentEdge edge, IReadOnlyList<(Node From, string Text)> causes, string verb)
    {
        var first = causes[0].From;
        var listed = string.Join("; ", causes.Take(ShownCauses).Select(cause => cause.Text));
        var more = causes.Count > ShownCauses ? $" and {causes.Count - ShownCauses} more" : "";
        var where = verb == "adds" ? "in" : "that was in";
        return Blocking.At("C2.undeclared", new Location(first.File, first.Line), $"the change {verb} component edge {Show(edge)} and the change record does not declare it. The dependency {where} the code: {listed}{more}. Undo it, or run `gate conflict` so a human can declare it.");
    }

    private static Finding Unobserved(string record, ComponentEdge edge, string key) =>
        Blocking.At("C2.unobserved", new Location(record, 0), $"`delta.{key}` declares edge {Show(edge)} and the code between base and head does not {key} it. The model may not drift ahead of the code: implement the change, or have the record corrected.");

    private static IEnumerable<Finding> NotAllowed(CheckInput input) =>
        input.Records.SelectMany(record => record.Delta.Add)
            .Except(input.Architecture.Edges)
            .Select(edge => Blocking.At("C2.model", new Location(Model, 0), $"the change record declares edge {Show(edge)} and `edges` in {Model} has no `{{from: {edge.From}, to: {edge.To}}}`. The human who approves the record adds it to the model in the same commit."));

    /// <summary>The commits that change the model and the record precede every implementation commit: every commit that touches an unprotected path.</summary>
    private static IEnumerable<Finding> OutOfOrder(CheckInput input)
    {
        var commits = input.Change.Commits;
        var guarded = input.Config.Protected;
        var firstImplementation = commits.TakeWhile(commit => commit.Paths.All(path => Glob.MatchesAny(guarded, path))).Count();
        return commits.Skip(firstImplementation)
            .Where(commit => commit.Paths.Any(IsDeclaration))
            .Select(commit => Blocking.At("C2.order", new Location(Model, 0), $"commit {commit.Id} by '{commit.Author}' changes {string.Join(", ", commit.Paths.Where(IsDeclaration))} after an earlier commit already changed implementation files. The commit that changes the model and the record must come first: declare, then implement."));
    }

    private static bool IsDeclaration(string path) =>
        path == Model || path.StartsWith(C1ChangeRecord.Directory);

    private static string Show(ComponentEdge edge) => $"{edge.From} -> {edge.To}";
}
