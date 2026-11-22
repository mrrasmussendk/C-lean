// C3 Trace check: an edge observed at run time that the model does not allow blocks.
using Gate.Model;

namespace Gate.Contracts;

internal static class C3TraceCheck
{
    private const string Model = "architecture.yaml";

    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var spans = input.Revisions.Head.Spans;
        var observed = Observed(spans);
        var allowed = input.Architecture.Edges.ToHashSet();
        var violations = observed.Except(allowed)
            .Select(edge => Blocking.At("C3.edge", new Location(Model, 0), $"a traced call goes {edge.From} -> {edge.To} and `edges` in {Model} does not allow it ({Example(spans, edge)}). The static graph misses calls through dependency injection, reflection and messaging; remove the dependency or have the model changed."));
        var unexercised = observed.Count == 0 ? [] : allowed.Except(observed).Select(Unexercised);
        return [.. violations, .. unexercised];
    }

    private static IReadOnlySet<ComponentEdge> Observed(IReadOnlyList<Span> spans)
    {
        var components = spans.GroupBy(span => span.Id).ToDictionary(group => group.Key, group => group.First().Component);
        return spans
            .Where(span => components.ContainsKey(span.Parent))
            .Select(span => new ComponentEdge(components[span.Parent], span.Component))
            .Where(edge => edge.From != edge.To && edge.From.Length > 0 && edge.To.Length > 0)
            .ToHashSet();
    }

    private static string Example(IReadOnlyList<Span> spans, ComponentEdge edge)
    {
        var components = spans.GroupBy(span => span.Id).ToDictionary(group => group.Key, group => group.First().Component);
        var child = spans.First(span => span.Component == edge.To && components.GetValueOrDefault(span.Parent, "") == edge.From);
        return $"span {child.Parent} in {edge.From} is the parent of span {child.Id} in {edge.To}";
    }

    private static Finding Unexercised(ComponentEdge edge) =>
        new("C3.unexercised", Severities.Report, new Location(Model, 0), $"C3.unexercised: no traced test exercises allowed edge {edge.From} -> {edge.To}. Add an integration test that crosses it.")
        {
            Fingerprint = $"C3.unexercised|{edge.From}|{edge.To}",
        };
}
