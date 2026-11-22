// C6 Hardening: decides the exit code from the findings, the baseline, the acknowledgements and a filed conflict.
using Gate.Model;

namespace Gate.Contracts;

internal sealed record Decision(int ExitCode, IReadOnlyList<Finding> Findings);

internal static class C6Hardening
{
    public const int Passed = 0;
    public const int Blocked = 1;
    public const int GateFailed = 2;
    public const int ConflictFiled = 3;
    public const string ConflictDirectory = "conflicts/";

    public static Decision Decide(CheckInput input, IReadOnlyList<Finding> findings)
    {
        var weighed = findings.Select(finding => Weigh(input.Protected, finding)).OrderByDescending(finding => finding.Severity.Rank).ToList();
        if (input.Change.Commits.Any(commit => commit.Paths.Any(path => path.StartsWith(ConflictDirectory))))
        {
            return new Decision(ConflictFiled, weighed);
        }

        var stops = weighed.Any(finding => finding.Severity.Rank >= Severities.Warn.Rank);
        return new Decision(stops ? Blocked : Passed, weighed);
    }

    /// <summary>A baselined finding is reported, not enforced. An acknowledged warning no longer stops the merge.</summary>
    private static Finding Weigh(ProtectedData data, Finding finding)
    {
        var baselined = data.Baseline.Contains(finding.Fingerprint);
        var acknowledged = finding.Severity == Severities.Warn && data.Acknowledged.Contains(finding.Fingerprint);
        return baselined || acknowledged ? finding with { Severity = Severities.Report } : finding;
    }
}
