// C6 Hardening, single-responsibility report: distinct reasons and actors per component over a trailing window.
using Gate.Model;

namespace Gate.Contracts;

internal static class Responsibility
{
    private const string Model = "architecture.yaml";

    /// <summary>A report only: nothing validates it against defects yet.</summary>
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var components = input.Architecture.Components;
        var touches = input.Change.History
            .SelectMany(change => change.Paths.Select(path => Owner(components, path)).Distinct().Select(component => (component, change)))
            .Where(touch => touch.component.Length > 0)
            .GroupBy(touch => touch.component, touch => touch.change);
        return touches.Select(group => Finding(group.Key, group.ToList())).Where(finding => finding is not null).Select(finding => finding!).ToList();
    }

    /// <summary>For each commit of the window, oldest first: the change record it carries, or the one before it.</summary>
    public static IReadOnlyList<PastChange> History(IReadOnlyList<Commit> window, IReadOnlyDictionary<string, ChangeRecord> records)
    {
        var current = new ChangeRecord("", "", [], "") { Delta = new Delta([], []), DeclaredTests = [] };
        var history = new List<PastChange>();
        foreach (var commit in window)
        {
            current = commit.Paths.Where(records.ContainsKey).Select(path => records[path]).FirstOrDefault(current);
            history.Add(new PastChange(string.Join(",", current.Reasons), current.Actor, commit.Paths));
        }

        return history.Where(change => change.Reason.Length > 0).ToList();
    }

    private static string Owner(IReadOnlyList<Component> components, string path) =>
        components.Where(component => Glob.MatchesAny(component.Paths, path)).Select(component => component.Name).FirstOrDefault("");

    private static Finding? Finding(string component, IReadOnlyList<PastChange> changes)
    {
        var reasons = changes.Select(change => change.Reason).Distinct().Order().ToList();
        var actors = changes.Select(change => change.Actor).Where(actor => actor.Length > 0).Distinct().Order().ToList();
        if (reasons.Count <= 1 && actors.Count <= 1)
        {
            return null;
        }

        var message = $"C6.responsibility: component '{component}' changed for {reasons.Count} reasons ({string.Join(", ", reasons)}) asked for by {actors.Count} actors ({string.Join(", ", actors)}) in the trailing window. A component that changes for many reasons may hold more than one responsibility.";
        return new Finding("C6.responsibility", Severities.Report, new Location(Model, 0), message)
        {
            Fingerprint = $"C6.responsibility|{component}",
        };
    }
}
