// C4 Test admission: a test is admitted on what it adds, not on its existence.
using Gate.Model;

namespace Gate.Contracts;

internal static class C4TestAdmission
{
    private const string Survived = "Survived";
    private const string Uncovered = "NoCoverage";
    private const int ShownTests = 3;

    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var head = input.Revisions.Head;
        if (!head.HasMutationReport)
        {
            return [MissingReport(input.Config)];
        }

        var mutants = ChangedMutants(input);
        return [.. Failing(head), .. Survivors(input, mutants), .. Redundant(input, mutants), .. OverBudget(input), .. PropertyTests.Missing(input)];
    }

    /// <summary>Tests that kill nothing, or whose kills another test also makes (nightly pruning).</summary>
    public static IReadOnlyList<string> PruneCandidates(IReadOnlyList<Mutant> matrix, IReadOnlyList<string> tests)
    {
        var kills = tests.ToDictionary(test => test, test => matrix.Where(mutant => mutant.KilledBy.Contains(test)).Select(Key).ToHashSet());
        return tests.Where(test => kills[test].Count == 0 || tests.Any(other => Covers(kills, other, test))).ToList();
    }

    private static bool Covers(IReadOnlyDictionary<string, HashSet<string>> kills, string other, string test)
    {
        var larger = kills[other].Count > kills[test].Count;
        var tieBreak = kills[other].Count == kills[test].Count && string.CompareOrdinal(other, test) < 0;
        return other != test && kills[test].IsSubsetOf(kills[other]) && (larger || tieBreak);
    }

    private static IEnumerable<Finding> Failing(Snapshot head) =>
        head.Tests.Where(test => test.Failed)
            .Select(test => Blocking.At("C4.failing", new Location(test.Name, 0), $"test {test.Name} fails on the head of the change, before any mutant is applied. Make it pass; no test is admitted while the suite is red."));

    private static Finding MissingReport(Config config) =>
        new("C4.report", Severities.Named(config.Tests.MissingReport.Length > 0 ? config.Tests.MissingReport : Severities.Block.Name), new Location(".gate/out", 0), "C4.report: there is no .gate/out/<language>/mutation.json for any language in gate.yaml, so no test can be admitted. Set `tests.commands.<language>` in gate.yaml to the project's test command (it must write JUnit XML to $GATE_JUNIT); `c-lean check` then runs the suite once per mutant of the changed lines itself.")
        {
            Fingerprint = "C4.report",
        };

    private static IReadOnlyList<Mutant> ChangedMutants(CheckInput input)
    {
        var lines = input.Change.Lines;
        var ignored = input.Protected.MutationIgnore;
        return input.Revisions.Head.Mutants
            .Where(mutant => lines.Any(range => range.Path == mutant.File && range.Start <= mutant.Line && mutant.Line <= range.End))
            .Where(mutant => !Matches(ignored, mutant))
            .ToList();
    }

    private static IEnumerable<Finding> Survivors(CheckInput input, IReadOnlyList<Mutant> mutants)
    {
        var waivers = input.Protected.Waivers;
        var survivors = mutants
            .Where(mutant => mutant.Status == Survived && !Matches(waivers, mutant))
            .Select(mutant => Blocking.At("C4.survivor", new Location(mutant.File, mutant.Line), $"mutant {mutant.Mutator} on changed line {mutant.Line} of {mutant.File} is covered by tests and none of them fails for it. Add a test that fails for it, or have a human add it to .gate/mutation-waivers.yaml."));
        var uncovered = mutants.Where(mutant => mutant.Status == Uncovered).Select(Unreached);
        return survivors.Concat(uncovered);
    }

    private static Finding Unreached(Mutant mutant) =>
        new("C4.uncovered", Severities.Report, new Location(mutant.File, mutant.Line), $"C4.uncovered: no test covers mutant {mutant.Mutator} on a changed line. Cover the line.")
        {
            Fingerprint = $"C4.uncovered|{Key(mutant)}",
        };

    private static IEnumerable<Finding> Redundant(CheckInput input, IReadOnlyList<Mutant> mutants)
    {
        var existing = input.Revisions.Base.Tests.Select(test => test.Name).ToHashSet();
        var added = input.Revisions.Head.Tests.Select(test => test.Name).Where(name => !existing.Contains(name)).Distinct();
        return added
            .Where(test => !mutants.Any(mutant => mutant.KilledBy.Contains(test) && !mutant.KilledBy.Any(existing.Contains)))
            .Select(test => Blocking.At("C4.redundant", new Location(test, 0), $"new test {test} kills {mutants.Count(mutant => mutant.KilledBy.Contains(test))} of the {mutants.Count} mutants on the changed lines, and every one of those is already killed by an existing test. Delete it, or make it check something the existing tests do not."));
    }

    private static IEnumerable<Finding> OverBudget(CheckInput input)
    {
        var before = input.Revisions.Base.Tests.GroupBy(test => test.Name).ToDictionary(group => group.Key, group => group.Sum(test => test.Seconds));
        var slower = input.Revisions.Head.Tests
            .Select(test => (test.Name, Added: test.Seconds - before.GetValueOrDefault(test.Name, 0)))
            .Where(test => test.Added > 0)
            .OrderByDescending(test => test.Added)
            .ToList();
        var added = input.Revisions.Head.Tests.Sum(test => test.Seconds) - before.Values.Sum();
        var budget = input.Config.Tests.RuntimeBudgetSeconds;
        var costly = string.Join(", ", slower.Take(ShownTests).Select(test => $"{test.Name} +{test.Added:0.##} s"));
        return added > budget
            ? [Blocking.At("C4.budget", new Location(".gate/out", 0), $"the change adds {added:0.##} s of test time, budget {budget:0.##} s (`tests.runtime_budget_seconds`). Most added: {costly}. Make these faster or fewer.")]
            : [];
    }

    private static bool Matches(IReadOnlyList<MutantPattern> patterns, Mutant mutant) =>
        patterns.Any(pattern => Glob.Matches(pattern.Path, mutant.File)
            && (pattern.Mutator == "*" || pattern.Mutator == mutant.Mutator)
            && (pattern.Context.Length == 0 || pattern.Context == mutant.Context));

    private static string Key(Mutant mutant) => $"{mutant.File}:{mutant.Line}:{mutant.Mutator}";
}
