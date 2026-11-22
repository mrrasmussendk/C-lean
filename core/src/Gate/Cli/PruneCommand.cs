using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>`gate tests prune`: deletion candidates from the latest nightly kill matrix.</summary>
internal static class PruneCommand
{
    /// <summary>`gate tests nightly`: every mutant of the whole code against the whole suite; the full kill matrix.</summary>
    public static int Nightly(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var config = InputReader.Config(root);
        if (config.Tests.Commands.Count == 0)
        {
            throw new GateFailure($"{Path.Combine(root, Layout.Config)}: `tests.commands` names no test command, so there is nothing to run. Add one per language.");
        }

        var tree = GitChange.AddTree(root, "HEAD");
        try
        {
            TestBench.RunHead(new Bench(root, tree, Path.Combine(root, Layout.Nightly)), config, new MutationScope([], []));
        }
        finally
        {
            GitChange.RemoveTree(root, tree);
        }

        Surroundings.Print($"gate: wrote the kill matrix to {Layout.Nightly}; `gate tests prune` lists the deletion candidates");
        return C6Hardening.Passed;
    }

    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var reports = InputReader.Config(root).Languages
            .Select(language => Path.Combine(Path.Combine(root, Layout.Nightly), language, "mutation.json"))
            .ToList();
        if (!reports.Any(Files.Exists))
        {
            throw new GateFailure($"{string.Join(", ", reports)}: no nightly kill matrix. The nightly job runs the mutation tool on the full suite and writes the report there.");
        }

        var matrix = reports.SelectMany(path => Documents.Read(path, MutationText.Mutants)).ToList();
        var tests = reports.SelectMany(path => Documents.Read(path, MutationText.Tests)).ToList();
        var candidates = C4TestAdmission.PruneCandidates(matrix, tests);
        foreach (var test in candidates)
        {
            Surroundings.Print(test);
        }

        Surroundings.Print($"gate: {candidates.Count} of {tests.Count} tests kill nothing that another test does not kill");
        return C6Hardening.Passed;
    }
}
