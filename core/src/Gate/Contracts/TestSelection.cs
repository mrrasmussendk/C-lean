// C4 Test admission, selection: a change runs only the tests that cover its changed code.
using Gate.Model;

namespace Gate.Contracts;

/// <summary>The tests to run: their files and their fully qualified names. Both empty means every test.</summary>
internal sealed record Selection(IReadOnlyList<string> Files, IReadOnlyList<string> Names);

internal static class TestSelection
{
    public static readonly Selection Everything = new([], []);

    /// <summary>The tests that reach, through calls, a production function overlapping the given lines.</summary>
    public static Selection Reaching(Snapshot head, IReadOnlyList<LineRange> lines)
    {
        var changed = head.Units
            .Where(unit => !unit.Test && lines.Any(range => range.Path == unit.File && range.Start <= unit.EndLine && unit.StartLine <= range.End))
            .Select(unit => unit.Id)
            .ToList();
        var users = head.Graph.Edges.Where(edge => edge.Kind is "call" or "construct").ToLookup(edge => edge.To, edge => edge.From);
        var reaching = Closure(changed, users);
        var tests = head.Graph.Nodes.Where(node => node.Test && node.Kind == "function" && reaching.Contains(node.Id)).ToList();
        return new Selection(tests.Select(node => node.File).Distinct().Order().ToList(), tests.Select(node => Name(node.Id)).Distinct().Order().ToList());
    }

    /// <summary>A symbol ID without its language, package and parameters.</summary>
    private static string Name(string symbol) =>
        string.Join(':', symbol.Split('(')[0].Split(':').Skip(IdPrefixParts));

    private const int IdPrefixParts = 2;

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
