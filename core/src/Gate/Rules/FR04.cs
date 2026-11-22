// FR04: Indirection depth: in-solution call frames from an entry point to the first effect call
using Gate.Model;

namespace Gate.Rules;

internal static class FR04
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var graph = input.Revisions.Head.Graph;
        var functions = graph.Nodes.Where(node => node.Kind == "function" && !node.Test).ToDictionary(node => node.Id);
        var calls = graph.Edges.Where(edge => edge.Kind == "call" && functions.ContainsKey(edge.From) && functions.ContainsKey(edge.To)).ToLookup(edge => edge.From, edge => edge.To);
        var called = calls.SelectMany(targets => targets).ToHashSet();
        var limit = Settings.Limit(input.Config, "FR04");
        return functions.Values
            .Where(node => !called.Contains(node.Id))
            .Select(entry => (entry, depth: Depth(entry.Id, functions, calls)))
            .Where(measured => measured.depth > limit)
            .Select(measured => Finding(input.Config, measured.entry, (measured.depth, limit)))
            .ToList();
    }

    /// <summary>Frames on the shortest call path from the entry to a function that calls an effect API; 0 when there is none.</summary>
    private static int Depth(string entry, IReadOnlyDictionary<string, Node> functions, ILookup<string, string> calls)
    {
        var frontier = new List<string> { entry };
        var seen = new HashSet<string> { entry };
        for (var depth = 1; frontier.Count > 0; depth++)
        {
            if (frontier.Any(id => functions[id].Effect))
            {
                return depth;
            }

            frontier = frontier.SelectMany(id => calls[id]).Where(seen.Add).ToList();
        }

        return 0;
    }

    private static Finding Finding(Config config, Node entry, (int Depth, int Limit) measured)
    {
        var message = $"FR04: {entry.Id} reaches its first effect call after {measured.Depth} call frames, limit {measured.Limit}. Remove layers that only forward.";
        return new Finding("FR04", Settings.Severity(config, "FR04"), new Location(entry.File, entry.Line), message)
        {
            Fingerprint = $"FR04|{entry.File}|{entry.Id}",
        };
    }
}
