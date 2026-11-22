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
        Print(Options.Value(arguments, Options.RootOption, Options.Here), decision);
        return decision.ExitCode;
    }

    /// <summary>With --offline the interchange files and .gate/changeset.yaml are taken as they are.</summary>
    public static CheckInput Collect(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        if (Options.Has(arguments, "--offline"))
        {
            return InputReader.Read(root, InputReader.OfflineChange(root));
        }

        var config = InputReader.Config(root);
        var mergeBase = GitChange.MergeBase(root, Options.Value(arguments, "--base", "origin/main"));
        InTree(root, mergeBase, tree => AnalyzeBase(new Bench(root, tree, Path.Combine(root, Layout.BaseOut)), config));
        NativeRunner.AnalyzeHead(root, config);
        var change = GitChange.Read(root, mergeBase) with { History = History(root, mergeBase) };
        var head = SnapshotReader.Read(Path.Combine(root, Layout.HeadOut), config.Languages);
        var scope = new MutationScope(change.Lines, TestSelection.Files(head, change.Lines));
        InTree(root, "HEAD", tree => TestHead(new Bench(root, tree, Path.Combine(root, Layout.HeadOut)), config, scope));
        return InputReader.Read(root, change);
    }

    /// <summary>Runs an action in a throwaway checkout of one revision and removes the checkout whatever happens.</summary>
    public static void InTree(string root, string revision, Action<string> action)
    {
        var tree = GitChange.AddTree(root, revision);
        try
        {
            action(tree);
        }
        finally
        {
            GitChange.RemoveTree(root, tree);
        }
    }

    /// <summary>The suite, then one run per mutant, then one instrumented run. Skipped when gate.yaml names no test command.</summary>
    public static void TestHead(Bench bench, Config config, MutationScope scope)
    {
        if (config.Tests.Commands.Count == 0)
        {
            return;
        }

        TestBench.RunSuite(bench, config);
        TestBench.RunMutants(bench, config, scope);
        TestBench.RunTraced(bench, config);
    }

    private static void AnalyzeBase(Bench bench, Config config)
    {
        NativeRunner.AnalyzeBase(bench.Root, config, bench.Tree);
        TestBench.RunSuite(bench, config);
    }

    private static IReadOnlyList<PastChange> History(string root, string mergeBase)
    {
        var window = GitChange.Window(root, mergeBase);
        var records = window.SelectMany(commit => commit.Paths)
            .Where(path => path.StartsWith(C1ChangeRecord.Directory) && Files.Exists(Path.Combine(root, path)))
            .Distinct()
            .ToDictionary(path => path, path => Documents.Read(Path.Combine(root, path), text => Parse.ChangeText.Record(path, text)));
        return Responsibility.History(window, records);
    }

    /// <summary>Per finding: severity, rule and place on one line, then what is wrong and the offending source line.</summary>
    private static void Print(string root, Decision decision)
    {
        foreach (var finding in decision.Findings)
        {
            var where = finding.Where;
            Surroundings.Print($"{finding.Severity.Name} {finding.Rule} {(where.Line > 0 ? $"{where.File}:{where.Line}" : where.File)}");
            Surroundings.Print($"    {finding.Message}");
            Surroundings.Print(Excerpt(Files.Read(Path.Combine(root, where.File)), where.Line));
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
