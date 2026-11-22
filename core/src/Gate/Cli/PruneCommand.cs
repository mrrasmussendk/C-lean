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
        var root = Options.Root(arguments);
        var reports = InputReader.Config(root).Languages
            .Select(language => Files.Read(Path.Combine(Layout.In(root, Layout.Nightly), language, "mutation.json")))
            .ToList();
        var matrix = reports.SelectMany(MutationText.Mutants).ToList();
        var tests = reports.SelectMany(MutationText.Tests).ToList();
        var candidates = C4TestAdmission.PruneCandidates(matrix, tests);
        foreach (var test in candidates)
        {
            Surroundings.Print(test);
        }

        Surroundings.Print($"gate: {candidates.Count} of {tests.Count} tests kill nothing that another test does not kill");
        return C6Hardening.Passed;
    }
}
