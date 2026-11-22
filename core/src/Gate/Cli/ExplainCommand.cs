using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>`gate explain`: the rule's definition, default and fix guidance.</summary>
internal static class ExplainCommand
{
    public static int Run(IReadOnlyList<string> words)
    {
        var entry = RuleTable.Entries.FirstOrDefault(rule => words.Contains(rule.Id))
            ?? throw new GateFailure($"unknown rule '{string.Join(' ', words)}'");
        Surroundings.Print($"{entry.Id}: {entry.Definition}");
        Surroundings.Print($"default: {entry.Default}, severity: {entry.Severity.Name}");
        Surroundings.Print($"fix: {entry.Guidance}");
        return C6Hardening.Passed;
    }
}
