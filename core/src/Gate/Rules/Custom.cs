// The rules a repository adds under .gate/rules: each judged on the lines the change touched.
using System.Text.RegularExpressions;
using Gate.Model;

namespace Gate.Rules;

internal static class Custom
{
    private const double PatternSeconds = 2;

    public static IReadOnlyList<Finding> Check(CheckInput input) =>
        input.Custom.Where(evidence => evidence.Rule.Severity != Severities.Off)
            .SelectMany(Found)
            .Where(finding => input.Change.Lines.Any(range => Holds(range, finding.Where)))
            .ToList();

    /// <summary>Every violation in the evidence, wherever it is; the caller keeps those on touched lines.</summary>
    public static IEnumerable<Finding> Found(RuleEvidence evidence) =>
        evidence.Rule.Pattern.Length > 0 ? Matched(evidence) : Printed(evidence);

    private static IEnumerable<Finding> Matched(RuleEvidence evidence)
    {
        var pattern = new Regex(evidence.Rule.Pattern, RegexOptions.None, TimeSpan.FromSeconds(PatternSeconds));
        return from file in evidence.Files
               from line in file.Text.Split('\n').Select((text, index) => (text, number: index + 1))
               where pattern.IsMatch(line.text)
               select Finding(evidence.Rule, new Location(file.Path, line.number), evidence.Rule.Message);
    }

    private static IEnumerable<Finding> Printed(RuleEvidence evidence) =>
        evidence.Output.Split('\n').Select(line => Regex.Match(line.Trim(), CustomRule.OutputLine))
            .Where(match => match.Success)
            .Select(match => Finding(evidence.Rule, new Location(match.Groups["path"].Value, int.Parse(match.Groups["line"].Value)), match.Groups["message"].Value));

    private static Finding Finding(CustomRule rule, Location where, string message) =>
        new(rule.Id, rule.Severity, where, $"{rule.Id}: {message.TrimEnd('.')}. {rule.Fix}".TrimEnd())
        {
            Fingerprint = $"{rule.Id}|{where.File}|{where.Line}|{message}",
        };

    private static bool Holds(LineRange range, Location where) =>
        range.Path == where.File && range.Start <= where.Line && where.Line <= range.End;
}
