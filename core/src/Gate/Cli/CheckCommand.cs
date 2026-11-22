using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>`gate check`: collect, run the stages, decide the exit code.</summary>
internal static class CheckCommand
{
    public static int Run(IReadOnlyList<string> arguments)
    {
        var stage = Options.Value(arguments, "--stage", Stages.All);
        if (stage != Stages.All && Stages.Named.All(named => named.Name != stage))
        {
            throw new GateFailure($"--stage {stage}: there is no such stage. Stages are {string.Join(", ", Stages.Named.Select(named => named.Name))}.");
        }

        var input = Collect(arguments);
        var decision = C6Hardening.Decide(input, Stages.Run(input, stage));
        Print(Options.Root(arguments), decision);
        return decision.ExitCode;
    }

    /// <summary>With --offline the interchange files and .gate/changeset.yaml are taken as they are.</summary>
    public static CheckInput Collect(IReadOnlyList<string> arguments)
    {
        var root = Options.Root(arguments);
        if (Options.Has(arguments, "--offline"))
        {
            return InputReader.Read(root, InputReader.OfflineChange(root));
        }

        var config = InputReader.Config(root);
        var mergeBase = GitChange.MergeBase(root, Options.Value(arguments, "--base", "origin/main"));
        NativeRunner.AnalyzeBase(root, config, mergeBase);
        NativeRunner.AnalyzeHead(root, config);
        return InputReader.Read(root, GitChange.Read(root, mergeBase));
    }

    /// <summary>Per finding: severity, rule and place on one line, then what is wrong and the offending source line.</summary>
    private static void Print(string root, Decision decision)
    {
        foreach (var finding in decision.Findings)
        {
            var where = finding.Where;
            Surroundings.Print($"{finding.Severity.Name} {finding.Rule} {(where.Line > 0 ? $"{where.File}:{where.Line}" : where.File)}");
            Surroundings.Print($"    {finding.Message}");
            Surroundings.Print(Excerpt(Files.Read(Layout.In(root, where.File)), where.Line));
        }

        var counts = string.Join(", ", new[] { Severities.Block, Severities.Warn, Severities.Report }
            .Select(severity => $"{decision.Findings.Count(finding => finding.Severity == severity)} {severity.Name}"));
        Surroundings.Print($"gate: exit {decision.ExitCode} ({counts}){Meaning(decision.ExitCode)}");
    }

    private static string Excerpt(string source, int line)
    {
        var lines = source.Split('\n');
        var text = line > 0 && line <= lines.Length ? lines[line - 1].TrimEnd() : "";
        return text.Length > 0 ? $"    {line} | {text}\n" : "";
    }

    private static string Meaning(int exitCode) =>
        exitCode == C6Hardening.Blocked ? ": the change cannot merge until every block and unacknowledged warn above is resolved"
        : exitCode == C6Hardening.ConflictFiled ? ": a conflict is filed; the change waits for a human"
        : "";
}
