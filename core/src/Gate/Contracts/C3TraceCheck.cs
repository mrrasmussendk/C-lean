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
        var byId = spans.GroupBy(span => span.Id).ToDictionary(group => group.Key, group => group.First());
        return spans
            .Select(span => new ComponentEdge(Caller(byId, span), span.Component))
            .Where(edge => edge.From != edge.To && edge.From.Length > 0 && edge.To.Length > 0)
            .ToHashSet();
    }

    /// <summary>
    /// The component of the nearest span above this one that names its component; empty when there is none.
    /// A span without a component, as a library writes, must not hide the call that passes through it.
    /// </summary>
    private static string Caller(IReadOnlyDictionary<string, Span> byId, Span span)
    {
        var seen = new HashSet<string> { span.Id };
        for (var parent = byId.GetValueOrDefault(span.Parent); parent is not null && seen.Add(parent.Id); parent = byId.GetValueOrDefault(parent.Parent))
        {
            if (parent.Component.Length > 0)
            {
                return parent.Component;
            }
        }

        return "";
    }

    private static string Example(IReadOnlyList<Span> spans, ComponentEdge edge)
    {
        var byId = spans.GroupBy(span => span.Id).ToDictionary(group => group.Key, group => group.First());
        var child = spans.First(span => span.Component == edge.To && Caller(byId, span) == edge.From);
        return $"span {child.Parent} in {edge.From} is above span {child.Id} in {edge.To}";
    }

    private static Finding Unexercised(ComponentEdge edge) =>
        new("C3.unexercised", Severities.Report, new Location(Model, 0), $"C3.unexercised: no traced test exercises allowed edge {edge.From} -> {edge.To}. Add an integration test that crosses it.")
        {
            Fingerprint = $"C3.unexercised|{edge.From}|{edge.To}",
        };
}
