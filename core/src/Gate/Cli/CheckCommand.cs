using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>`c-lean check`: collect, run the stages, decide the exit code.</summary>
internal static class CheckCommand
{
    public static int Run(IReadOnlyList<string> arguments)
    {
        if (Options.Value(arguments, FileCheck.Option, "").Length > 0)
        {
            return FileCheck.Run(arguments);
        }

        var stage = Options.Value(arguments, "--stage", Stages.All);
        if (stage != Stages.All && Stages.Named.All(named => named.Name != stage))
        {
            throw new GateFailure($"--stage {stage}: there is no such stage. Stages are {string.Join(", ", Stages.Named.Select(named => named.Name))}.");
        }

        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var started = Surroundings.Seconds();
        var input = Collect(arguments);
        var decision = C6Hardening.Decide(input, Settings.Enforced(input.Config, Stages.Run(input, stage)));
        Print(root, decision);
        if (!Options.Has(arguments, "--offline"))
        {
            CheckLog.Write(root, new Measured(Stats.Full, Surroundings.Seconds() - started, decision.ExitCode), decision.Findings);
        }

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
        var change = GitChange.Read(root, mergeBase) with { History = History(root, mergeBase) };
        var trees = (Base: GitChange.Tree(root, "base", mergeBase), Head: GitChange.Tree(root, "head", "HEAD"));
        var before = Task.Run(() => AnalyzeBase(new Bench(root, trees.Base, Path.Combine(root, Layout.BaseOut)), config));
        NativeRunner.AnalyzeHead(root, config);
        var head = SnapshotReader.Read(Path.Combine(root, Layout.HeadOut), config.Languages);
        TestBench.RunHead(new Bench(root, trees.Head, Path.Combine(root, Layout.HeadOut)), config, new MutationScope(change.Lines, head));
        before.GetAwaiter().GetResult();
        return InputReader.Read(root, change);
    }

    /// <summary>The base is analyzed and its suite run while the head is being worked on.</summary>
    private static void AnalyzeBase(Bench bench, Config config)
    {
        NativeRunner.AnalyzeBase(bench.Root, config, bench.Tree);
        TestBench.RunBase(bench, config);
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

    private static void Print(string root, Decision decision)
    {
        foreach (var line in Report.Lines(root, decision.Findings))
        {
            Surroundings.Print(line);
        }

        foreach (var line in Surroundings.Watched() ? Picture(decision.ExitCode) : [])
        {
            Surroundings.Print(line);
        }

        Surroundings.Print($"c-lean: exit {decision.ExitCode} ({Report.Counts(decision.Findings)}){Meaning(decision.ExitCode)}");
    }

    private static IReadOnlyList<string> Picture(int exitCode) =>
        exitCode == C6Hardening.Passed ? Art.Passed(Guide.Pen(), "the change may merge")
        : exitCode == C6Hardening.ConflictFiled ? Art.Stopped(Guide.Pen(), "a conflict waits for a human")
        : Art.Stopped(Guide.Pen(), "the change cannot merge yet");

    private static string Meaning(int exitCode) =>
        exitCode == C6Hardening.Blocked ? ": the change cannot merge until every block and unacknowledged warn above is resolved"
        : exitCode == C6Hardening.ConflictFiled ? ": a conflict is filed; the change waits for a human"
        : "";
}
