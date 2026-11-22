// The sanctioned exit, at the agent's elbow: a finding that survives several edits is a reason to file a conflict, not to contort.
namespace Gate.Contracts;

internal static class Attempts
{
    public const int Limit = 3;

    private const char Separator = '\t';

    /// <summary>How many consecutive per-file checks each finding has now survived. A finding that is gone is forgotten.</summary>
    public static IReadOnlyDictionary<string, int> Next(string state, IReadOnlyList<string> fingerprints)
    {
        var before = state.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(Separator, Fields))
            .Where(fields => fields.Length == Fields && int.TryParse(fields[0], out _))
            .GroupBy(fields => fields[1])
            .ToDictionary(group => group.Key, group => int.Parse(group.First()[0]));
        return fingerprints.Distinct().ToDictionary(fingerprint => fingerprint, fingerprint => before.GetValueOrDefault(fingerprint, 0) + 1);
    }

    /// <summary>The state to keep: findings of other files as they were, this file's as counted now.</summary>
    public static string State(string state, string file, IReadOnlyDictionary<string, int> counts)
    {
        var others = state.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(line => !line.Contains($"|{file}|"));
        var mine = counts.Select(count => $"{count.Value}{Separator}{count.Key}");
        return string.Concat(others.Concat(mine).Select(line => line + "\n"));
    }

    /// <summary>What to tell the implementer about findings that have survived the limit, or nothing.</summary>
    public static IReadOnlyList<string> Advice(IReadOnlyDictionary<string, int> counts)
    {
        var stuck = counts.Where(count => count.Value >= Limit).Select(count => count.Key.Split('|')[0]).Distinct().Order().ToList();
        return stuck.Count == 0
            ? []
            : [$"gate: still present after {Limit} edits of this file: {string.Join(", ", stuck)}. Do not work around a rule. If the rule and the task cannot both be satisfied, stop and run: gate conflict \"<which rule, and why the task needs to break it>\""];
    }

    private const int Fields = 2;
}
