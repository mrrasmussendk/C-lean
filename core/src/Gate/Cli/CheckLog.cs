using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Cli;

internal sealed record Measured(string Mode, double Seconds, int ExitCode);

/// <summary>Appends what one check cost and found to the gate's own log, which `c-lean stats` reads.</summary>
internal static class CheckLog
{
    public static void Write(string root, Measured measured, IReadOnlyList<Finding> findings)
    {
        var stopping = findings.Where(C6Hardening.Stops)
            .GroupBy(finding => finding.Rule)
            .ToDictionary(rule => rule.Key, rule => rule.Count());
        var run = new CheckRun(Surroundings.Now(), measured.Mode, GitChange.Branch(root), measured.Seconds)
        {
            ExitCode = measured.ExitCode,
            Findings = stopping,
        };
        Files.Append(Path.Combine(root, Layout.Log), LogText.Line(run));
    }
}
