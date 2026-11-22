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
            throw new GateFailure($"unknown stage '{stage}'");
        }

        var input = Collect(arguments);
        var decision = C6Hardening.Decide(input, Stages.Run(input, stage));
        Print(decision);
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

    private static void Print(Decision decision)
    {
        foreach (var finding in decision.Findings)
        {
            Surroundings.Print($"{finding.Severity.Name} {finding.Rule} {finding.Where.File}:{finding.Where.Line} {finding.Message}");
        }

        var counts = string.Join(", ", new[] { Severities.Block, Severities.Warn, Severities.Report }
            .Select(severity => $"{decision.Findings.Count(finding => finding.Severity == severity)} {severity.Name}"));
        Surroundings.Print($"gate: exit {decision.ExitCode} ({counts})");
    }
}
