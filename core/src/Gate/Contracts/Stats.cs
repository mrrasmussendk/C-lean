// What the gate costs: checks, time and findings per rule, from the gate's own log.
using Gate.Model;

namespace Gate.Contracts;

internal static class Stats
{
    public const string Full = "check";
    public const string PerFile = "file";

    private const int Half = 2;
    private const int NameWidth = 32;
    private const int CountWidth = 16;
    private const int RuleWidth = 20;
    private const int StoppedWidth = 20;

    public static IReadOnlyList<string> Summary(IReadOnlyList<CheckRun> runs, int conflicts) =>
    [
        Time(runs, Full, "full checks"),
        Time(runs, PerFile, "per-file checks"),
        $"conflicts filed: {conflicts}",
        "",
        Row(RuleWidth, "rule") + Row(StoppedWidth, "checks it stopped") + "findings",
        .. Rules(runs),
        "",
        Row(NameWidth, "change") + Row(CountWidth, "full checks") + Row(CountWidth, "edits checked") + Row(CountWidth, "edits stopped") + "passed",
        .. Changes(runs),
    ];

    private static string Time(IReadOnlyList<CheckRun> runs, string mode, string name)
    {
        var seconds = runs.Where(run => run.Mode == mode).Select(run => run.Seconds).Order().ToList();
        var median = seconds.Count == 0 ? 0 : seconds[seconds.Count / Half];
        return $"{name}: {seconds.Count}, median {median:0.0} s, total {seconds.Sum():0} s";
    }

    /// <summary>The rules that stop the most checks first: those cost the most iterations.</summary>
    private static IEnumerable<string> Rules(IReadOnlyList<CheckRun> runs) =>
        runs.SelectMany(run => run.Findings)
            .GroupBy(found => found.Key)
            .OrderByDescending(rule => rule.Count())
            .ThenBy(rule => rule.Key)
            .Select(rule => Row(RuleWidth, rule.Key) + Row(StoppedWidth, rule.Count()) + rule.Sum(found => found.Value));

    private static IEnumerable<string> Changes(IReadOnlyList<CheckRun> runs) =>
        runs.GroupBy(run => run.Change)
            .Select(change => (change.Key, Full: change.Where(run => run.Mode == Full).ToList(), Edits: change.Where(run => run.Mode == PerFile).ToList()))
            .Select(change => Row(NameWidth, change.Key) + Row(CountWidth, change.Full.Count) + Row(CountWidth, change.Edits.Count)
                + Row(CountWidth, change.Edits.Count(run => run.ExitCode != C6Hardening.Passed))
                + (change.Full.Any(run => run.ExitCode == C6Hardening.Passed) ? "yes" : "no"));

    private static string Row(int width, object cell) => $"{cell}".PadRight(width);
}
