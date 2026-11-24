using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>`c-lean explain`: the rule's definition, default and fix guidance.</summary>
internal static class ExplainCommand
{
    public static int Run(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var own = RuleFiles.Load(Options.Value(arguments, Options.RootOption, Options.Here)).FirstOrDefault(rule => words.Contains(rule.Id));
        if (own is not null)
        {
            return Explain(own);
        }

        var entry = RuleTable.Entries.FirstOrDefault(rule => words.Contains(rule.Id))
            ?? throw new GateFailure($"explain {string.Join(' ', words)}: there is no such rule. Rule IDs are {string.Join(", ", RuleTable.Entries.Select(rule => rule.Id))}.");
        Surroundings.Print($"{entry.Id}: {entry.Definition}");
        Surroundings.Print($"default: {entry.Default}, severity: {entry.Severity.Name}");
        Surroundings.Print($"fix: {entry.Guidance}");
        return C6Hardening.Passed;
    }

    /// <summary>A rule of the repository, from its file under .gate/rules.</summary>
    private static int Explain(CustomRule rule)
    {
        Surroundings.Print($"{rule.Id}: {rule.Definition}");
        Surroundings.Print($"severity: {rule.Severity.Name}, applies to: {string.Join(", ", rule.Files)}, defined in: {Layout.Rules}/{rule.Id}.yaml");
        Surroundings.Print($"fix: {rule.Fix}");
        return C6Hardening.Passed;
    }
}
