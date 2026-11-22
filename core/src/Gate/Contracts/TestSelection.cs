// C4 Test admission, selection: a change runs only the tests that cover its changed code.
using Gate.Model;

namespace Gate.Contracts;

internal static class TestSelection
{
    /// <summary>The test files whose tests reach a changed production function through calls. Empty means: run every test.</summary>
    public static IReadOnlyList<string> Files(Snapshot head, IReadOnlyList<LineRange> lines)
    {
        var changed = head.Units
            .Where(unit => !unit.Test && lines.Any(range => range.Path == unit.File && range.Start <= unit.EndLine && unit.StartLine <= range.End))
            .Select(unit => unit.Id)
            .ToList();
        var users = head.Graph.Edges.Where(edge => edge.Kind is "call" or "construct").ToLookup(edge => edge.To, edge => edge.From);
        var reaching = Closure(changed, users);
        return head.Graph.Nodes
            .Where(node => node.Test && reaching.Contains(node.Id))
            .Select(node => node.File)
            .Distinct().Order().ToList();
    }

    private static IReadOnlySet<string> Closure(IReadOnlyList<string> start, ILookup<string, string> next)
    {
        var frontier = start.ToList();
        var seen = start.ToHashSet();
        while (frontier.Count > 0)
        {
            frontier = frontier.SelectMany(id => next[id]).Where(seen.Add).ToList();
        }

        return seen;
    }
}
