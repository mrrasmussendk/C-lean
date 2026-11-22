using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>How a finding is shown: severity, rule and place on one line, then what is wrong and the offending source line.</summary>
internal static class Report
{
    public static IReadOnlyList<string> Lines(string root, IReadOnlyList<Finding> findings) =>
        findings.SelectMany(finding => Lines(root, finding)).ToList();

    public static string Counts(IReadOnlyList<Finding> findings) =>
        string.Join(", ", new[] { Severities.Block, Severities.Warn, Severities.Report }
            .Select(severity => $"{findings.Count(finding => finding.Severity == severity)} {severity.Name}"));

    private static IEnumerable<string> Lines(string root, Finding finding)
    {
        var where = finding.Where;
        var place = where.Line > 0 ? $"{where.File}:{where.Line}" : where.File;
        var source = Files.Read(Path.Combine(root, where.File)).Split('\n');
        var text = where.Line > 0 && where.Line <= source.Length ? source[where.Line - 1].TrimEnd() : "";
        return text.Length > 0
            ? [$"{finding.Severity.Name} {finding.Rule} {place}", $"    {finding.Message}", $"    {where.Line} | {text}", ""]
            : [$"{finding.Severity.Name} {finding.Rule} {place}", $"    {finding.Message}", ""];
    }
}
