using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>`gate tests prune`: deletion candidates from the latest nightly kill matrix.</summary>
internal static class PruneCommand
{
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
